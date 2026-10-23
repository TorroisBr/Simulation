using System;
using System.Collections.Generic;

/// <summary>
/// Host hooks for publishing the existing P12 owner admission and Unity-facing
/// projections. Merchant decisions and exact owner state transitions remain
/// in the shared Core operation.
/// </summary>
internal interface IDailyV1SelectedMerchantTradeStateBoundary
{
    bool TryInstallPlanState(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ItemDefinition item,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        int remainingAmount,
        float purchasePricePerItem,
        int waitDaysAtDestination,
        int pendingTravelDays,
        string originDecisionId);

    bool TrySetTradeTravelPlan(
        DailyV1NpcRuntimeOwners actor,
        string targetCityRuntimeId,
        float utility,
        string originDecisionId);

    void ClearTradeTravelPlan(DailyV1NpcRuntimeOwners actor);
    NpcDecisionRecord RecordTradeRedirect(
        DailyV1NpcRuntimeOwners actor,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence evidence);
    void LogTrade(string message);
}

/// <summary>
/// Existing Unity/P12 admission wrapper for one selected actor's daily
/// Merchant state operation. Core owns invoking the operation and its owner
/// mutations; the host supplies only the established admission lifetime.
/// </summary>
internal interface IDailyV1SelectedMerchantTradeStateAdmissionBoundary
{
    bool ExecuteWithinSelectedMerchantDailyOperation(
        DailyV1NpcRuntimeOwners actor,
        Func<bool> operation);
}

/// <summary>
/// Advances the selected Daily-v1 merchant plan from the same Core owner graph
/// used by selected actions and continuation capture. This is the existing
/// once-per-actor daily trade-state pass, not an additional trade operation.
/// </summary>
internal static class DailyV1SelectedMerchantTradeStateOperation
{
    private sealed class TradeOpportunity
    {
        internal DailyV1CityRuntimeOwners TargetCity;
        internal int Amount;
        internal float SellPrice;
        internal float ProfitPerItem;
        internal float NetProfit;
        internal float Score;
        internal float TravelCost;
        internal float Freshness;
        internal DailyV1CommercialMarketObservation Observation;
        internal CommercialDecisionEvidence Evidence;
    }

    internal static bool TryAdvance(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        long absoluteDay,
        IDailyV1SelectedMerchantTradeStateBoundary boundary)
    {
        if (graph == null || actor == null || boundary == null || absoluteDay < 0L
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)) return false;

        if (actor.Lifecycle?.LifeState != NpcLifeState.Alive
            || !DailyV1NpcDefinitionRules.HasJobType(actor.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            || actor.Travel == null || actor.Travel.IsTraveling
            || !TryGetCity(graph, actor.Travel.CurrentCityRuntimeId, out DailyV1CityRuntimeOwners currentCity))
            return true;

        EffectiveSimulationConfiguration effective = graph.Definitions.EffectiveConfiguration;
        EffectiveMerchantTradeConfiguration trade = effective?.MerchantTrade
            ?? new EffectiveMerchantTradeConfiguration(enabled: true);
        EffectiveCommercialKnowledgeConfiguration knowledge = effective?.CommercialKnowledge
            ?? new EffectiveCommercialKnowledgeConfiguration();

        ObserveCurrentMarket(actor, currentCity, absoluteDay);

        DailyV1NpcMerchantTradePlanOwner plan = actor.MerchantTradePlan;
        if (plan == null) return false;
        bool localMerchant = DailyV1NpcDefinitionRules.HasMerchantBehavior(actor.Definition,
            graph.Definitions, DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue);
        if ((localMerchant && plan.HasData)
            || (plan.HasData && !plan.IsActive)
            || (plan.IsActive && GetInventoryAmount(actor, plan.ItemDefinition) < 1))
        {
            ClearPlan(actor, boundary);
            return true;
        }

        if (!plan.IsActive) return true;

        if (!string.Equals(plan.TargetCityRuntimeId, currentCity.RuntimeId, StringComparison.Ordinal))
        {
            if (CanPlanKnownTravel(graph, actor, currentCity, plan.TargetCityRuntimeId))
            {
                if (DailyV1NpcDefinitionRules.HasMerchantBehavior(actor.Definition, graph.Definitions,
                    DailyV1NpcDefinitionRules.TravelingMerchantBehaviorValue))
                    boundary.TrySetTradeTravelPlan(actor, plan.TargetCityRuntimeId, 70f,
                        plan.OriginDecisionId);
                return true;
            }

            ClearTradeTravelPlan(actor, boundary);
            TryInstallPlan(actor, boundary, plan.ItemDefinition, plan.OriginCityRuntimeId,
                currentCity.RuntimeId, plan.PlannedAmount, plan.RemainingAmount,
                plan.PurchasePricePerItem, 0, plan.PendingTravelDays, plan.OriginDecisionId);
            boundary.LogTrade(actor.Definition?.Values?.name
                + " nao consegue viajar para cumprir o plano comercial e vai reavaliar venda local.");
        }

        int amount = GetPlannedTradeAmount(actor, plan, trade);
        if (amount <= 0)
        {
            ClearPlan(actor, boundary);
            return true;
        }

        if (!TryGetUsefulObservation(actor, currentCity.LocationRuntimeId, plan.ItemDefinition,
                absoluteDay, knowledge, out DailyV1CommercialMarketObservation localObservation,
                out _))
        {
            IncrementWait(actor, boundary, plan);
            return true;
        }

        float localPrice = localObservation.ObservedPrice;
        float profitPerItem = localPrice - plan.PurchasePricePerItem;
        bool hasLocalBuyer = HasBestLocalMerchantBuyer(graph, actor, currentCity,
            plan.ItemDefinition, amount, plan.PurchasePricePerItem, localObservation,
            absoluteDay, trade, knowledge);
        int marketAmount = LimitMarketSaleAmountFromKnowledge(actor, currentCity.LocationRuntimeId,
            localPrice, amount, absoluteDay, knowledge);
        float minimumProfit = GetMinimumProfit(actor, graph.Definitions);

        if (hasLocalBuyer || (marketAmount > 0 && profitPerItem >= minimumProfit))
        {
            if (plan.WaitDaysAtDestination > 0)
                TryInstallPlan(actor, boundary, plan.ItemDefinition, plan.OriginCityRuntimeId,
                    plan.TargetCityRuntimeId, plan.PlannedAmount, plan.RemainingAmount,
                    plan.PurchasePricePerItem, 0, plan.PendingTravelDays, plan.OriginDecisionId);
            return true;
        }

        TradeOpportunity redirect = FindBestTradeDestination(graph, actor, currentCity,
            plan, absoluteDay, trade, knowledge);
        if (redirect != null)
        {
            NpcDecisionRecord decision = boundary.RecordTradeRedirect(actor,
                redirect.TargetCity.LocationRuntimeId, redirect.Evidence);
            int nextWaitDays = string.Equals(plan.TargetCityRuntimeId,
                redirect.TargetCity.RuntimeId, StringComparison.Ordinal)
                ? plan.WaitDaysAtDestination : 0;
            TryInstallPlan(actor, boundary, plan.ItemDefinition, plan.OriginCityRuntimeId,
                redirect.TargetCity.RuntimeId, plan.PlannedAmount, plan.RemainingAmount,
                plan.PurchasePricePerItem, nextWaitDays, plan.PendingTravelDays,
                decision?.DecisionId ?? plan.OriginDecisionId);
            boundary.TrySetTradeTravelPlan(actor, redirect.TargetCity.RuntimeId, 70f,
                decision?.DecisionId ?? plan.OriginDecisionId);
            boundary.LogTrade(actor.Definition?.Values?.name
                + " reavaliou o plano comercial e mudou o destino para "
                + redirect.TargetCity.Definition?.Values?.cityName + ".");
            return true;
        }

        IncrementWait(actor, boundary, plan);
        return true;
    }

    /// <summary>
    /// Advances pending-travel days for selected active plans before the daily
    /// actor pass. The compatibility adapter remains responsible for the
    /// existing P12 plan-owner admission and publication callback.
    /// </summary>
    internal static bool TryAdvancePendingTravelDays(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1NpcRuntimeOwners> exactNpcOwners,
        IDailyV1SelectedMerchantTradeStateBoundary boundary)
    {
        if (graph == null || exactNpcOwners == null || boundary == null
            || !graph.MatchesExactOwnerInstances(graph.Cities, exactNpcOwners))
            return false;

        foreach (DailyV1NpcRuntimeOwners actor in exactNpcOwners)
        {
            if (actor?.Lifecycle?.LifeState != NpcLifeState.Alive
                || actor.Travel == null || actor.Travel.IsTraveling
                || !TryGetCity(graph, actor.Travel.CurrentCityRuntimeId,
                    out DailyV1CityRuntimeOwners currentCity))
                continue;

            DailyV1NpcMerchantTradePlanOwner plan = actor.MerchantTradePlan;
            if (plan == null || !plan.IsActive
                || plan.PendingTravelDays == int.MaxValue
                || !TryGetCity(graph, plan.TargetCityRuntimeId,
                    out DailyV1CityRuntimeOwners targetCity)
                || ReferenceEquals(targetCity, currentCity))
                continue;

            // The Unity adapter intentionally treats a rejected projection/P12
            // mutation as a no-op for this legacy void maintenance operation.
            boundary.TryInstallPlanState(actor, plan.ItemDefinition,
                plan.OriginCityRuntimeId, plan.TargetCityRuntimeId,
                plan.PlannedAmount, plan.RemainingAmount,
                plan.PurchasePricePerItem, plan.WaitDaysAtDestination,
                plan.PendingTravelDays + 1, plan.OriginDecisionId);
        }

        return true;
    }

    internal static void ObserveCurrentMarket(
        DailyV1NpcRuntimeOwners actor,
        DailyV1CityRuntimeOwners city,
        long absoluteDay)
    {
        actor.SpatialKnowledge?.DiscoverLocation(city.LocationRuntimeId);
        if (city.Market == null || actor.CommercialKnowledge == null) return;

        foreach (DailyV1MarketItemState row in city.Market.Items)
        {
            if (row?.Item == null || string.IsNullOrWhiteSpace(row.Item.DefinitionId)) continue;
            actor.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
                city.LocationRuntimeId, row.Item, row.CurrentPrice, row.Amount,
                absoluteDay, absoluteDay, DailyV1CommercialKnowledgeSource.DirectObservation));
        }

        MarketCounterpartyRuntime counterparty = city.Market.Counterparty;
        float purchasingPower = counterparty.LiquidityMode == MarketLiquidityMode.AccountBacked
            ? counterparty.MoneyAccount.Balance : 0f;
        actor.CommercialKnowledge.RecordLiquidityObservation(
            new DailyV1CommercialLiquidityObservation(city.LocationRuntimeId,
                counterparty.LiquidityMode, purchasingPower, absoluteDay, absoluteDay,
                DailyV1CommercialKnowledgeSource.DirectObservation));
    }

    /// <summary>
    /// Applies the established post-travel market observation to the exact
    /// selected owners. Travel transitions and arrival event publication have
    /// already completed; this preserves the prior party-first, then solo
    /// roster order while keeping market knowledge writes in Core.
    /// </summary>
    internal static bool ObserveCompletedTravelResults(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
        IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults,
        long absoluteDay)
    {
        if (graph == null || partyArrivals == null || soloResults == null || absoluteDay < 0L)
            return false;

        if (graph.Definitions?.EffectiveConfiguration?.MerchantTrade?.Enabled != true)
            return true;

        foreach (DailyV1SelectedTravelPartyArrivalResult arrival in partyArrivals)
        {
            if (arrival?.Members == null) return false;
            foreach (DailyV1NpcRuntimeOwners member in arrival.Members)
                if (!ObserveCurrentMarketForExactOwner(graph, member, absoluteDay)) return false;
        }

        foreach (DailyV1SelectedSoloTravelDayResult result in soloResults)
        {
            if (result?.Npc == null) return false;
            if (result.Arrived
                && !ObserveCurrentMarketForExactOwner(graph, result.Npc, absoluteDay))
                return false;
        }

        return true;
    }

    private static bool ObserveCurrentMarketForExactOwner(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        long absoluteDay)
    {
        if (actor == null || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions))
            return false;
        if (actor.Lifecycle?.LifeState != NpcLifeState.Alive
            || !DailyV1NpcDefinitionRules.HasJobType(
                actor.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue))
            return true;

        string cityRuntimeId = actor.Travel?.CurrentCityRuntimeId;
        if (string.IsNullOrWhiteSpace(cityRuntimeId)) return true;
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (!string.Equals(city?.RuntimeId, cityRuntimeId, StringComparison.Ordinal)) continue;
            ObserveCurrentMarket(actor, city, absoluteDay);
            return true;
        }

        // A valid location can be outside the selected City roster. The old
        // Unity projection had no CurrentCity in that case and observed nothing.
        return true;
    }

    private static bool HasBestLocalMerchantBuyer(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners seller,
        DailyV1CityRuntimeOwners city,
        DailyV1ItemDefinition item,
        int requestedAmount,
        float referencePrice,
        DailyV1CommercialMarketObservation observation,
        long absoluteDay,
        EffectiveMerchantTradeConfiguration trade,
        EffectiveCommercialKnowledgeConfiguration knowledge)
    {
        if (seller.Inventory == null || item == null || requestedAmount <= 0) return false;
        int maxAmount = Math.Min(requestedAmount, seller.Inventory.GetAmount(item));
        if (maxAmount <= 0 || !TryGetUsefulObservation(seller, city.LocationRuntimeId,
                item, absoluteDay, knowledge, out _, out float freshness)) return false;
        float minimumProfit = GetMinimumProfit(seller, graph.Definitions);

        foreach (string buyerRuntimeId in city.NpcPresence?.OrderedNpcRuntimeIds
            ?? Array.Empty<string>())
        {
            if (!graph.TryGetNpcOwner(buyerRuntimeId, out DailyV1NpcRuntimeOwners buyer)
                || ReferenceEquals(buyer, seller)
                || buyer.Lifecycle?.LifeState != NpcLifeState.Alive
                || buyer.Travel == null || buyer.Travel.IsTraveling
                || !string.Equals(buyer.Travel.CurrentCityRuntimeId, city.RuntimeId,
                    StringComparison.Ordinal)
                || !DailyV1NpcDefinitionRules.HasJobType(buyer.Definition, graph.Definitions,
                    DailyV1NpcDefinitionRules.MerchantJobTypeValue)
                || !DailyV1NpcDefinitionRules.HasMerchantBehavior(buyer.Definition,
                    graph.Definitions, DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue)
                || buyer.MoneyAccount == null) continue;

            float preference = GetTradePreference(buyer.Definition, graph.Definitions, item);
            float preferredBonus = Math.Min(0.15f, Math.Max(0f, preference - 1f) * 0.1f);
            float unitPrice = Math.Max(0.01f, observation.ObservedPrice)
                * UnityMathfCompatibility.Clamp(trade.LocalWholesalePriceMultiplier + preferredBonus,
                    0.5f, 0.85f);
            float profit = unitPrice - referencePrice;
            if (profit < minimumProfit) continue;
            float spendable = Math.Max(0f, buyer.MoneyAccount.Balance * (1f - trade.LocalReserveRatio));
            if (Math.Min(maxAmount, UnityMathfCompatibility.FloorToInt(spendable / unitPrice)) > 0)
                return true;
        }
        return false;
    }

    private static TradeOpportunity FindBestTradeDestination(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1CityRuntimeOwners currentCity,
        DailyV1NpcMerchantTradePlanOwner plan,
        long absoluteDay,
        EffectiveMerchantTradeConfiguration trade,
        EffectiveCommercialKnowledgeConfiguration knowledge)
    {
        if (graph.SpatialNetwork == null || actor.SpatialKnowledge == null
            || actor.MoneyAccount == null || plan.ItemDefinition == null) return null;
        int availableAmount = GetPlannedTradeAmount(actor, plan, trade);
        if (availableAmount <= 0) return null;
        TradeOpportunity best = null;

        List<SpatialRouteRuntime> routes = new List<SpatialRouteRuntime>(
            graph.SpatialNetwork.GetOutgoingRoutes(
                GetLocation(graph, currentCity.LocationRuntimeId)));
        routes.Sort((left, right) => string.CompareOrdinal(left?.RuntimeId, right?.RuntimeId));
        foreach (SpatialRouteRuntime route in routes)
        {
            if (route == null || !actor.SpatialKnowledge.KnowsLocation(currentCity.LocationRuntimeId)
                || !actor.SpatialKnowledge.KnowsRoute(route.RuntimeId)
                || !actor.SpatialKnowledge.KnowsLocation(route.Destination.RuntimeId)
                || !graph.SpatialNetwork.TryGetSingleDirectRoute(route.Origin,
                    route.Destination, out SpatialRouteRuntime direct, out _)
                || !ReferenceEquals(direct, route)
                || !TryGetCityByLocation(graph, route.Destination.RuntimeId,
                    out DailyV1CityRuntimeOwners targetCity)
                || ReferenceEquals(targetCity, currentCity)) continue;

            int travelDays = route.TravelDays;
            if (travelDays <= 0) continue;
            if (!graph.TryGetMerchantTradeTravelCost(route, out float travelCost)) continue;
            if (actor.MoneyAccount.Balance < travelCost
                || !TryGetUsefulObservation(actor, targetCity.LocationRuntimeId,
                    plan.ItemDefinition, absoluteDay, knowledge,
                    out DailyV1CommercialMarketObservation destinationObservation,
                    out float freshness)) continue;

            float sellPrice = destinationObservation.ObservedPrice;
            int amount = LimitMarketSaleAmountFromKnowledge(actor, targetCity.LocationRuntimeId,
                sellPrice, availableAmount, absoluteDay, knowledge);
            if (amount <= 0) continue;
            float profit = sellPrice - plan.PurchasePricePerItem;
            float net = profit * amount - travelCost;
            if (profit < GetMinimumProfit(actor, graph.Definitions) || net <= 0f) continue;
            float score = net / Math.Max(1, travelDays) * freshness
                * GetTradePreference(actor.Definition, graph.Definitions, plan.ItemDefinition);
            if (!ShouldPrefer(score, targetCity, best)) continue;

            float gross = profit * amount;
            best = new TradeOpportunity
            {
                TargetCity = targetCity,
                Amount = amount,
                SellPrice = sellPrice,
                ProfitPerItem = profit,
                NetProfit = net,
                Score = score,
                TravelCost = travelCost,
                Freshness = freshness,
                Observation = destinationObservation,
                Evidence = CreateEvidence(graph, actor, currentCity, targetCity, plan,
                    destinationObservation, freshness, amount, sellPrice,
                    travelCost, gross, net, score, absoluteDay, knowledge)
            };
        }
        return best;
    }

    private static CommercialDecisionEvidence CreateEvidence(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1CityRuntimeOwners currentCity,
        DailyV1CityRuntimeOwners targetCity,
        DailyV1NpcMerchantTradePlanOwner plan,
        DailyV1CommercialMarketObservation observation,
        float freshness,
        int amount,
        float sellPrice,
        float travelCost,
        float gross,
        float net,
        float score,
        long absoluteDay,
        EffectiveCommercialKnowledgeConfiguration knowledge)
    {
        CommercialLiquidityEvidence liquidityEvidence = null;
        if (actor.CommercialKnowledge.TryGetLiquidityObservation(targetCity.LocationRuntimeId,
                out DailyV1CommercialLiquidityObservation liquidity))
            liquidityEvidence = CommercialLiquidityEvidence.Capture(liquidity.LocationRuntimeId,
                liquidity.LiquidityMode, liquidity.ObservedPurchasingPower,
                liquidity.ObservedDay, liquidity.ReceivedDay,
                DailyV1CommercialKnowledgeFreshness.GetFreshness(liquidity.ObservedDay,
                    absoluteDay, knowledge), (CommercialKnowledgeSource)(int)liquidity.Source,
                liquidity.SourceRuntimeId);

        TryGetCity(graph, plan.TargetCityRuntimeId, out DailyV1CityRuntimeOwners previousCity);
        return new CommercialDecisionEvidence(plan.ItemDefinition.DefinitionId,
            currentCity.LocationRuntimeId, currentCity.LocationRuntimeId,
            targetCity.LocationRuntimeId, previousCity?.LocationRuntimeId,
            null,
            CommercialObservationEvidence.Capture(observation.LocationRuntimeId,
                observation.ObservedPrice, observation.ObservedStock, observation.ObservedDay,
                observation.ReceivedDay, freshness,
                (CommercialKnowledgeSource)(int)observation.Source, observation.SourceRuntimeId),
            amount, plan.PurchasePricePerItem, sellPrice, travelCost, gross, net, score,
            liquidityEvidence);
    }

    private static bool TryGetUsefulObservation(
        DailyV1NpcRuntimeOwners actor,
        string locationRuntimeId,
        DailyV1ItemDefinition item,
        long absoluteDay,
        EffectiveCommercialKnowledgeConfiguration configuration,
        out DailyV1CommercialMarketObservation observation,
        out float freshness)
    {
        observation = null;
        freshness = 0f;
        if (actor?.CommercialKnowledge == null || item == null
            || !actor.CommercialKnowledge.TryGetObservation(locationRuntimeId,
                item.DefinitionId, out observation)
            || !ReferenceEquals(observation.ItemDefinition, item))
        { observation = null; return false; }
        freshness = DailyV1CommercialKnowledgeFreshness.GetFreshness(
            observation.ObservedDay, absoluteDay, configuration);
        return freshness > 0f;
    }

    private static int LimitMarketSaleAmountFromKnowledge(
        DailyV1NpcRuntimeOwners actor,
        string locationRuntimeId,
        float unitPrice,
        int requestedAmount,
        long absoluteDay,
        EffectiveCommercialKnowledgeConfiguration configuration)
    {
        if (requestedAmount <= 0 || unitPrice <= 0f) return Math.Max(0, requestedAmount);
        if (actor?.CommercialKnowledge == null
            || !actor.CommercialKnowledge.TryGetLiquidityObservation(locationRuntimeId,
                out DailyV1CommercialLiquidityObservation observation)
            || DailyV1CommercialKnowledgeFreshness.GetFreshness(observation.ObservedDay,
                absoluteDay, configuration) <= 0f
            || observation.LiquidityMode == MarketLiquidityMode.Open)
            return requestedAmount;
        return Math.Min(requestedAmount,
            UnityMathfCompatibility.FloorToInt(observation.ObservedPurchasingPower / unitPrice));
    }

    private static int GetPlannedTradeAmount(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcMerchantTradePlanOwner plan,
        EffectiveMerchantTradeConfiguration trade) => actor?.Inventory == null || plan?.ItemDefinition == null
        ? 0 : Math.Min(trade.MaxTradeAmount,
            Math.Min(plan.RemainingAmount, actor.Inventory.GetAmount(plan.ItemDefinition)));

    private static int GetInventoryAmount(DailyV1NpcRuntimeOwners actor, DailyV1ItemDefinition item) =>
        actor?.Inventory == null || item == null ? 0 : actor.Inventory.GetAmount(item);

    private static float GetMinimumProfit(
        DailyV1NpcRuntimeOwners actor,
        DailyV1EffectiveDefinitions definitions) => Math.Max(0f,
        DailyV1NpcDefinitionRules.MinimumProfitPerItem(actor?.Definition, definitions));

    private static float GetTradePreference(
        DailyV1NpcDefinition actor,
        DailyV1EffectiveDefinitions definitions,
        DailyV1ItemDefinition item) =>
        DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(actor, definitions,
            item, out float multiplier) ? Math.Max(0.01f, multiplier) : 1f;

    private static bool ShouldPrefer(float score, DailyV1CityRuntimeOwners city,
        TradeOpportunity current)
    {
        if (current == null || score > current.Score) return true;
        if (!UnityMathfCompatibility.Approximately(score, current.Score)) return false;
        return string.CompareOrdinal(city?.RuntimeId ?? string.Empty,
            current.TargetCity?.RuntimeId ?? string.Empty) < 0;
    }

    private static bool CanPlanKnownTravel(DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor, DailyV1CityRuntimeOwners currentCity,
        string targetCityRuntimeId)
    {
        if (graph.SpatialNetwork == null || actor.SpatialKnowledge == null
            || !TryGetCity(graph, targetCityRuntimeId, out DailyV1CityRuntimeOwners targetCity)
            || !graph.SpatialNetwork.TryGetLocation(currentCity.LocationRuntimeId,
                out SpatialLocationRuntime origin)
            || !graph.SpatialNetwork.TryGetLocation(targetCity.LocationRuntimeId,
                out SpatialLocationRuntime destination)
            || !actor.SpatialKnowledge.KnowsLocation(origin.RuntimeId)
            || !actor.SpatialKnowledge.KnowsLocation(destination.RuntimeId)
            || !graph.SpatialNetwork.TryGetSingleDirectRoute(origin, destination,
                out SpatialRouteRuntime route, out _)) return false;
        if (!actor.SpatialKnowledge.KnowsRoute(route.RuntimeId)) return false;
        if (!graph.TryGetMerchantTradeTravelCost(route, out float travelCost)) return false;
        return actor.MoneyAccount != null && actor.MoneyAccount.Balance >= travelCost;
    }

    private static bool TryGetCity(DailyV1SelectedRuntimeOwnerGraph graph,
        string runtimeId, out DailyV1CityRuntimeOwners city)
    {
        city = null;
        if (string.IsNullOrWhiteSpace(runtimeId)) return false;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            if (string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
            { city = candidate; return true; }
        return false;
    }

    private static bool TryGetCityByLocation(DailyV1SelectedRuntimeOwnerGraph graph,
        string locationRuntimeId, out DailyV1CityRuntimeOwners city)
    {
        city = null;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            if (string.Equals(candidate.LocationRuntimeId, locationRuntimeId, StringComparison.Ordinal))
            { city = candidate; return true; }
        return false;
    }

    private static SpatialLocationRuntime GetLocation(
        DailyV1SelectedRuntimeOwnerGraph graph, string locationRuntimeId) =>
        graph?.SpatialNetwork != null
            && graph.SpatialNetwork.TryGetLocation(locationRuntimeId, out SpatialLocationRuntime location)
                ? location : null;

    private static void TryInstallPlan(DailyV1NpcRuntimeOwners actor,
        IDailyV1SelectedMerchantTradeStateBoundary boundary,
        DailyV1ItemDefinition item, string origin, string target, int plannedAmount,
        int remainingAmount, float purchasePrice, int waitDays, int pendingDays,
        string originDecisionId) => boundary.TryInstallPlanState(actor, item, origin, target,
            plannedAmount, remainingAmount, purchasePrice, waitDays, pendingDays,
            originDecisionId);

    private static void IncrementWait(DailyV1NpcRuntimeOwners actor,
        IDailyV1SelectedMerchantTradeStateBoundary boundary,
        DailyV1NpcMerchantTradePlanOwner plan) => TryInstallPlan(actor, boundary,
            plan.ItemDefinition, plan.OriginCityRuntimeId, plan.TargetCityRuntimeId,
            plan.PlannedAmount, plan.RemainingAmount, plan.PurchasePricePerItem,
            plan.WaitDaysAtDestination + 1, plan.PendingTravelDays, plan.OriginDecisionId);

    private static void ClearPlan(DailyV1NpcRuntimeOwners actor,
        IDailyV1SelectedMerchantTradeStateBoundary boundary)
    {
        TryInstallPlan(actor, boundary, null, null, null, 0, 0, 0f, 0, 0, null);
        ClearTradeTravelPlan(actor, boundary);
    }

    private static void ClearTradeTravelPlan(DailyV1NpcRuntimeOwners actor,
        IDailyV1SelectedMerchantTradeStateBoundary boundary)
    {
        if (actor.TravelPlan?.Reason == NpcTravelReason.Trade)
            boundary.ClearTradeTravelPlan(actor);
    }
}
