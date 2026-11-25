using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Current market facts paired with an exact admitted Daily-v1 item node.</summary>
internal sealed class DailyV1MerchantLocalSaleFact
{
    internal DailyV1ItemDefinition Item { get; }
    internal float ObservedPrice { get; }
    internal float Freshness { get; }
    internal int AvailableAmount { get; }

    internal DailyV1MerchantLocalSaleFact(
        DailyV1ItemDefinition item,
        float observedPrice,
        float freshness,
        int availableAmount)
    {
        Item = item;
        ObservedPrice = observedPrice;
        Freshness = freshness;
        AvailableAmount = availableAmount;
    }
}

/// <summary>The existing local SellGoods choice, retaining its market-fact index and graph identity.</summary>
internal sealed class DailyV1MerchantLocalSalePlan
{
    internal int MarketFactIndex { get; }
    internal DailyV1ItemDefinition Item { get; }
    internal int Amount { get; }
    internal float ReferencePrice { get; }
    internal float LocalPrice { get; }
    internal float ProfitPerItem { get; }
    internal float Score { get; }
    internal CommercialDecisionEvidence Evidence { get; }
    internal float Utility { get; }
    internal DailyV1NpcActionPayloadOwner SelectedPayload { get; }
    internal string TargetNpcRuntimeId { get; }

    internal DailyV1MerchantLocalSalePlan(
        int marketFactIndex,
        DailyV1ItemDefinition item,
        int amount,
        float referencePrice,
        float localPrice,
        float profitPerItem,
        float score,
        float utility = 0f,
        CommercialDecisionEvidence evidence = null,
        DailyV1NpcActionPayloadOwner selectedPayload = null,
        string targetNpcRuntimeId = null)
    {
        MarketFactIndex = marketFactIndex;
        Item = item;
        Amount = amount;
        ReferencePrice = referencePrice;
        LocalPrice = localPrice;
        ProfitPerItem = profitPerItem;
        Score = score;
        Utility = utility;
        Evidence = evidence;
        SelectedPayload = selectedPayload;
        TargetNpcRuntimeId = targetNpcRuntimeId;
    }
}

/// <summary>Unity-read inputs for a selected merchant's trade-destination candidate.</summary>
internal sealed class DailyV1MerchantTradeCandidate
{
    internal DailyV1ItemDefinition Item { get; }
    internal string TargetRuntimeId { get; }
    internal int Amount { get; }
    internal float BuyPrice { get; }
    internal float SellPrice { get; }
    internal float TotalTravelCost { get; }
    internal int TotalTravelDays { get; }
    internal float OriginFreshness { get; }
    internal float DestinationFreshness { get; }

    internal DailyV1MerchantTradeCandidate(
        DailyV1ItemDefinition item,
        string targetRuntimeId,
        int amount,
        float buyPrice,
        float sellPrice,
        float totalTravelCost,
        int totalTravelDays,
        float originFreshness,
        float destinationFreshness)
    {
        Item = item;
        TargetRuntimeId = targetRuntimeId;
        Amount = amount;
        BuyPrice = buyPrice;
        SellPrice = sellPrice;
        TotalTravelCost = totalTravelCost;
        TotalTravelDays = totalTravelDays;
        OriginFreshness = originFreshness;
        DestinationFreshness = destinationFreshness;
    }
}

/// <summary>The selected merchant trade candidate with the existing score semantics.</summary>
internal sealed class DailyV1MerchantTradePlan
{
    internal int CandidateIndex { get; }
    internal DailyV1ItemDefinition Item { get; }
    internal string TargetRuntimeId { get; }
    internal int Amount { get; }
    internal float BuyPrice { get; }
    internal float SellPrice { get; }
    internal float ProfitPerItem { get; }
    internal float NetProfit { get; }
    internal float Score { get; }
    internal CommercialDecisionEvidence Evidence { get; }
    internal DailyV1NpcActionPayloadOwner SelectedPayload { get; }

    internal DailyV1MerchantTradePlan(
        int candidateIndex,
        DailyV1ItemDefinition item,
        string targetRuntimeId,
        int amount,
        float buyPrice,
        float sellPrice,
        float profitPerItem,
        float netProfit,
        float score,
        CommercialDecisionEvidence evidence = null,
        DailyV1NpcActionPayloadOwner selectedPayload = null)
    {
        CandidateIndex = candidateIndex;
        Item = item;
        TargetRuntimeId = targetRuntimeId;
        Amount = amount;
        BuyPrice = buyPrice;
        SellPrice = sellPrice;
        ProfitPerItem = profitPerItem;
        NetProfit = netProfit;
        Score = score;
        Evidence = evidence;
        SelectedPayload = selectedPayload;
    }
}

/// <summary>Core decision portion of selected Daily-v1 merchant local-sale planning.</summary>
internal static class DailyV1MerchantActionPlanning
{
    /// <summary>Builds selected BuyGoods candidates from the graph's exact commercial and spatial owners.</summary>
    internal static bool TryPlanSelectedTradeOpportunity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners merchant,
        DailyV1ActionDefinition buyAction,
        string originCityRuntimeId,
        long currentAbsoluteDay,
        float additionalTravelCost,
        int additionalTravelDays,
        out DailyV1MerchantTradePlan plan)
    {
        plan = null;
        if (graph == null || merchant == null || !graph.ContainsExactNpcOwner(merchant)
            || !DailyV1NpcDefinitionRules.IsAdmittedNpc(graph.Definitions, merchant.Definition)
            || (buyAction != null
                && (!DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, buyAction)
                    || buyAction.Values.actionType?.Value != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue
                    || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                        graph.Definitions, buyAction, merchant.Status?.Statuses)))
            || merchant.Lifecycle?.LifeState != NpcLifeState.Alive
            || merchant.Travel == null || merchant.Travel.IsTraveling
            || merchant.CommercialKnowledge == null
            || !ReferenceEquals(merchant.CommercialKnowledge.Definitions, graph.Definitions)
            || merchant.MoneyAccount == null || merchant.SpatialKnowledge == null
            || graph.SpatialNetwork == null
            || additionalTravelCost < 0f || float.IsNaN(additionalTravelCost)
            || float.IsInfinity(additionalTravelCost) || additionalTravelDays < 0)
            return false;

        DailyV1CityRuntimeOwners origin = null;
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            if (string.Equals(city.RuntimeId, originCityRuntimeId, StringComparison.Ordinal))
            { origin = city; break; }
        if (origin == null || !graph.ContainsExactCityOwner(origin)
            || !string.Equals(origin.RuntimeId, merchant.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)
            || !string.Equals(origin.LocationRuntimeId, merchant.Travel.CurrentLocationRuntimeId, StringComparison.Ordinal)
            || !graph.SpatialNetwork.TryGetLocation(origin.LocationRuntimeId, out SpatialLocationRuntime originLocation)
            || !merchant.SpatialKnowledge.KnowsLocation(originLocation.RuntimeId))
            return false;

        EffectiveSimulationConfiguration effective = graph.Definitions.EffectiveConfiguration;
        EffectiveMerchantTradeConfiguration trade = effective?.MerchantTrade
            ?? new EffectiveMerchantTradeConfiguration(enabled: true);
        if (trade.MaxTradeAmount <= 0) return false;
        EffectiveCommercialKnowledgeConfiguration knowledge = effective?.CommercialKnowledge
            ?? new EffectiveCommercialKnowledgeConfiguration();
        float travelCostPerDay = effective?.Travel?.TravelCostPerDay ?? 1f;
        List<SpatialRouteRuntime> routes = new List<SpatialRouteRuntime>(
            graph.SpatialNetwork.GetOutgoingRoutes(originLocation));
        routes.Sort((left, right) => string.CompareOrdinal(left?.RuntimeId ?? string.Empty,
            right?.RuntimeId ?? string.Empty));

        List<DailyV1MerchantTradeCandidate> candidates = new List<DailyV1MerchantTradeCandidate>();
        List<DailyV1CommercialMarketObservation> originObservations = new List<DailyV1CommercialMarketObservation>();
        List<DailyV1CommercialMarketObservation> destinationObservations = new List<DailyV1CommercialMarketObservation>();
        List<DailyV1CommercialLiquidityObservation> liquidityObservations = new List<DailyV1CommercialLiquidityObservation>();
        List<float> originFreshness = new List<float>();
        List<float> destinationFreshness = new List<float>();
        List<float> liquidityFreshness = new List<float>();
        foreach (DailyV1CommercialMarketObservation observedOrigin in merchant.CommercialKnowledge.Observations)
        {
            if (observedOrigin == null
                || !string.Equals(observedOrigin.LocationRuntimeId, origin.LocationRuntimeId, StringComparison.Ordinal)
                || observedOrigin.ObservedStock <= 0
                || !graph.Definitions.Items.TryGetValue(observedOrigin.ItemDefinitionId, out DailyV1ItemDefinition item)
                || !ReferenceEquals(item, observedOrigin.ItemDefinition)) continue;
            float originFresh = DailyV1CommercialKnowledgeFreshness.GetFreshness(
                observedOrigin.ObservedDay, currentAbsoluteDay, knowledge);
            float buyPrice = observedOrigin.ObservedPrice;
            if (originFresh <= 0f || buyPrice <= 0f) continue;

            foreach (SpatialRouteRuntime route in routes)
            {
                if (route == null || !merchant.SpatialKnowledge.KnowsRoute(route.RuntimeId)
                    || !merchant.SpatialKnowledge.KnowsLocation(route.Destination.RuntimeId)
                    || !graph.SpatialNetwork.TryGetSingleDirectRoute(
                        originLocation, route.Destination, out SpatialRouteRuntime direct, out _)
                    || !ReferenceEquals(direct, route)) continue;
                DailyV1CityRuntimeOwners target = null;
                foreach (DailyV1CityRuntimeOwners city in graph.Cities)
                    if (string.Equals(city.LocationRuntimeId, route.Destination.RuntimeId, StringComparison.Ordinal))
                    { target = city; break; }
                if (target == null || ReferenceEquals(target, origin)) continue;
                int travelDays = route.TravelDays;
                if (travelDays <= 0) continue;
                float travelCost = UnityMathfCompatibility.Max(1, travelDays) * travelCostPerDay;
                float totalCost = additionalTravelCost + travelCost;
                float spendable = merchant.MoneyAccount.Balance - totalCost;
                if (spendable <= 0f) continue;
                int available = UnityMathfCompatibility.Min(trade.MaxTradeAmount,
                    observedOrigin.ObservedStock,
                    UnityMathfCompatibility.FloorToInt(spendable / buyPrice));
                if (available <= 0
                    || !merchant.CommercialKnowledge.TryGetObservation(
                        target.LocationRuntimeId, item.DefinitionId,
                        out DailyV1CommercialMarketObservation observedDestination)
                    || !ReferenceEquals(observedDestination.ItemDefinition, item)) continue;
                float destinationFresh = DailyV1CommercialKnowledgeFreshness.GetFreshness(
                    observedDestination.ObservedDay, currentAbsoluteDay, knowledge);
                if (destinationFresh <= 0f) continue;
                int amount = LimitAmountFromKnowledge(merchant.CommercialKnowledge,
                    target.LocationRuntimeId, observedDestination.ObservedPrice, available,
                    currentAbsoluteDay, knowledge);
                if (amount <= 0) continue;

                candidates.Add(new DailyV1MerchantTradeCandidate(item, target.RuntimeId,
                    amount, buyPrice, observedDestination.ObservedPrice, totalCost,
                    additionalTravelDays + travelDays, originFresh, destinationFresh));
                originObservations.Add(observedOrigin);
                destinationObservations.Add(observedDestination);
                originFreshness.Add(originFresh);
                destinationFreshness.Add(destinationFresh);
                if (merchant.CommercialKnowledge.TryGetLiquidityObservation(
                        target.LocationRuntimeId, out DailyV1CommercialLiquidityObservation liquidity))
                {
                    liquidityObservations.Add(liquidity);
                    liquidityFreshness.Add(DailyV1CommercialKnowledgeFreshness.GetFreshness(
                        liquidity.ObservedDay, currentAbsoluteDay, knowledge));
                }
                else
                {
                    liquidityObservations.Add(null);
                    liquidityFreshness.Add(0f);
                }
            }
        }

        if (!TryPlanTradeOpportunity(graph.Definitions, merchant.Definition, candidates,
                DailyV1NpcDefinitionRules.MinimumProfitPerItem(merchant.Definition, graph.Definitions),
                out DailyV1MerchantTradePlan selected) || selected == null) return true;

        DailyV1CommercialMarketObservation originFact = originObservations[selected.CandidateIndex];
        DailyV1CommercialMarketObservation destinationFact = destinationObservations[selected.CandidateIndex];
        DailyV1CommercialLiquidityObservation liquidityFact = liquidityObservations[selected.CandidateIndex];
        float gross = selected.ProfitPerItem * selected.Amount;
        CommercialDecisionEvidence evidence = new CommercialDecisionEvidence(
            selected.Item.DefinitionId, origin.LocationRuntimeId, origin.LocationRuntimeId,
            graph.Cities.First(city => string.Equals(city.RuntimeId, selected.TargetRuntimeId, StringComparison.Ordinal)).LocationRuntimeId,
            null, CommercialObservationEvidence.Capture(originFact.LocationRuntimeId,
                originFact.ObservedPrice, originFact.ObservedStock, originFact.ObservedDay,
                originFact.ReceivedDay, originFreshness[selected.CandidateIndex],
                (CommercialKnowledgeSource)(int)originFact.Source, originFact.SourceRuntimeId),
            CommercialObservationEvidence.Capture(destinationFact.LocationRuntimeId,
                destinationFact.ObservedPrice, destinationFact.ObservedStock, destinationFact.ObservedDay,
                destinationFact.ReceivedDay, destinationFreshness[selected.CandidateIndex],
                (CommercialKnowledgeSource)(int)destinationFact.Source, destinationFact.SourceRuntimeId),
            selected.Amount, selected.BuyPrice, selected.SellPrice,
            candidates[selected.CandidateIndex].TotalTravelCost, gross, selected.NetProfit,
            selected.Score, liquidityFact == null ? null : CommercialLiquidityEvidence.Capture(
                liquidityFact.LocationRuntimeId, liquidityFact.LiquidityMode,
                liquidityFact.ObservedPurchasingPower, liquidityFact.ObservedDay,
                liquidityFact.ReceivedDay, liquidityFreshness[selected.CandidateIndex],
                (CommercialKnowledgeSource)(int)liquidityFact.Source, liquidityFact.SourceRuntimeId));
        DailyV1NpcActionPayloadOwner selectedPayload = null;
        if (buyAction != null && !DailyV1NpcActionPayloadOwner.TryCreate(
                graph.Definitions, buyAction, null, selected.TargetRuntimeId,
                selected.Item, selected.Amount, selected.BuyPrice, 1f,
                NpcTravelReason.None, 0f, null, null, evidence, null,
                out selectedPayload)) return true;
        plan = new DailyV1MerchantTradePlan(selected.CandidateIndex, selected.Item,
            selected.TargetRuntimeId, selected.Amount, selected.BuyPrice, selected.SellPrice,
            selected.ProfitPerItem, selected.NetProfit, selected.Score, evidence, selectedPayload);
        return true;
    }

    /// <summary>Plans the existing selected planned-sale target from ordered Core city presence.</summary>
    internal static bool TryPlanSelectedPlannedLocalBuyer(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners seller,
        DailyV1ActionDefinition action,
        long currentAbsoluteDay,
        float currentUtility,
        out DailyV1MerchantLocalSalePlan plan)
    {
        plan = null;
        if (graph == null || seller == null || action == null
            || !graph.ContainsExactNpcOwner(seller)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
            || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(graph.Definitions, action, seller.Status?.Statuses)
            || seller.Lifecycle?.LifeState != NpcLifeState.Alive
            || seller.Travel == null || seller.Travel.IsTraveling
            || seller.MerchantTradePlan == null || !seller.MerchantTradePlan.IsActive
            || seller.Inventory == null || seller.CommercialKnowledge == null)
            return false;

        DailyV1CityRuntimeOwners city = null;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            if (string.Equals(candidate.RuntimeId, seller.Travel.CurrentCityRuntimeId, StringComparison.Ordinal))
            { city = candidate; break; }
        DailyV1ItemDefinition item = seller.MerchantTradePlan.ItemDefinition;
        if (city == null || !graph.ContainsExactCityOwner(city) || city.NpcPresence == null
            || item == null
            || !seller.CommercialKnowledge.TryGetObservation(city.LocationRuntimeId,
                item.DefinitionId, out DailyV1CommercialMarketObservation observation)
            || !ReferenceEquals(observation.ItemDefinition, item)) return false;

        EffectiveSimulationConfiguration effective = graph.Definitions.EffectiveConfiguration;
        EffectiveMerchantTradeConfiguration trade = effective?.MerchantTrade
            ?? new EffectiveMerchantTradeConfiguration(enabled: true);
        EffectiveCommercialKnowledgeConfiguration knowledge = effective?.CommercialKnowledge
            ?? new EffectiveCommercialKnowledgeConfiguration();
        float freshness = DailyV1CommercialKnowledgeFreshness.GetFreshness(
            observation.ObservedDay, currentAbsoluteDay, knowledge);
        if (freshness <= 0f || trade.MaxTradeAmount <= 0) return true;
        int requestedAmount = UnityMathfCompatibility.Min(trade.MaxTradeAmount,
            seller.MerchantTradePlan.RemainingAmount, seller.Inventory.GetAmount(item));
        if (requestedAmount <= 0) return true;

        float minimumProfit = UnityMathfCompatibility.Max(0f,
            DailyV1NpcDefinitionRules.MinimumProfitPerItem(seller.Definition, graph.Definitions));
        DailyV1NpcRuntimeOwners bestBuyer = null;
        int bestAmount = 0;
        float bestPrice = 0f;
        float bestProfit = 0f;
        float bestScore = 0f;
        foreach (string buyerRuntimeId in city.NpcPresence.OrderedNpcRuntimeIds)
        {
            if (!graph.TryGetNpcOwner(buyerRuntimeId, out DailyV1NpcRuntimeOwners buyer)
                || ReferenceEquals(buyer, seller)
                || buyer.Lifecycle?.LifeState != NpcLifeState.Alive
                || buyer.Travel == null || buyer.Travel.IsTraveling
                || !string.Equals(buyer.Travel.CurrentCityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
                || !DailyV1NpcDefinitionRules.HasJobType(buyer.Definition, graph.Definitions,
                    DailyV1NpcDefinitionRules.MerchantJobTypeValue)
                || !DailyV1NpcDefinitionRules.HasMerchantBehavior(buyer.Definition, graph.Definitions,
                    DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue)
                || buyer.MoneyAccount == null) continue;

            float preference = DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(
                buyer.Definition, graph.Definitions, item, out float configuredPreference)
                ? UnityMathfCompatibility.Max(0.01f, configuredPreference) : 1f;
            float preferredPriceBonus = UnityMathfCompatibility.Min(0.15f,
                UnityMathfCompatibility.Max(0f, preference - 1f) * 0.1f);
            float unitPrice = UnityMathfCompatibility.Max(0.01f, observation.ObservedPrice)
                * UnityMathfCompatibility.Clamp(trade.LocalWholesalePriceMultiplier + preferredPriceBonus,
                    0.5f, 0.85f);
            float profit = unitPrice - seller.MerchantTradePlan.PurchasePricePerItem;
            if (profit < minimumProfit) continue;
            float spendable = UnityMathfCompatibility.Max(0f,
                buyer.MoneyAccount.Balance * (1f - trade.LocalReserveRatio));
            int amount = UnityMathfCompatibility.Min(requestedAmount,
                UnityMathfCompatibility.FloorToInt(spendable / unitPrice));
            if (amount <= 0) continue;
            float score = profit * amount * freshness * preference;
            bool prefer = bestBuyer == null || score > bestScore;
            if (!prefer && UnityMathfCompatibility.Approximately(score, bestScore))
                prefer = string.CompareOrdinal(buyer.RuntimeId, bestBuyer.RuntimeId) < 0;
            if (!prefer) continue;
            bestBuyer = buyer; bestAmount = amount; bestPrice = unitPrice;
            bestProfit = profit; bestScore = score;
        }

        if (bestBuyer == null) return true;
        float grossProfit = bestProfit * bestAmount;
        CommercialDecisionEvidence evidence = new CommercialDecisionEvidence(
            item.DefinitionId, city.LocationRuntimeId, city.LocationRuntimeId,
            city.LocationRuntimeId, null, null,
            CommercialObservationEvidence.Capture(observation.LocationRuntimeId,
                observation.ObservedPrice, observation.ObservedStock, observation.ObservedDay,
                observation.ReceivedDay, freshness,
                (CommercialKnowledgeSource)(int)observation.Source, observation.SourceRuntimeId),
            bestAmount, seller.MerchantTradePlan.PurchasePricePerItem, bestPrice,
            0f, grossProfit, grossProfit, bestScore);
        if (!DailyV1NpcActionPayloadOwner.TryCreate(graph.Definitions, action,
                bestBuyer.RuntimeId, null, item, bestAmount, bestPrice, 1f,
                NpcTravelReason.None, 0f, null, null, evidence, null,
                out DailyV1NpcActionPayloadOwner payload)) return false;
        plan = new DailyV1MerchantLocalSalePlan(0, item, bestAmount,
            seller.MerchantTradePlan.PurchasePricePerItem, bestPrice, bestProfit,
            bestScore, UnityMathfCompatibility.Max(currentUtility, 85f + bestScore),
            evidence, payload, bestBuyer.RuntimeId);
        return true;
    }

    /// <summary>Plans the established destination-market fallback for an active
    /// selected trade plan when no eligible local NPC buyer is selected.</summary>
    internal static bool TryPlanSelectedPlannedMarketSale(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners seller,
        DailyV1ActionDefinition action,
        long currentAbsoluteDay,
        float currentUtility,
        out DailyV1MerchantLocalSalePlan plan)
    {
        plan = null;
        if (graph == null || seller == null || action == null
            || !graph.ContainsExactNpcOwner(seller)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
            || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(graph.Definitions, action, seller.Status?.Statuses)
            || seller.Lifecycle?.LifeState != NpcLifeState.Alive
            || seller.Travel == null || seller.Travel.IsTraveling
            || seller.MerchantTradePlan == null || !seller.MerchantTradePlan.IsActive
            || seller.Inventory == null || seller.CommercialKnowledge == null)
            return false;

        DailyV1CityRuntimeOwners city = null;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            if (string.Equals(candidate.RuntimeId, seller.Travel.CurrentCityRuntimeId, StringComparison.Ordinal))
            { city = candidate; break; }
        DailyV1ItemDefinition item = seller.MerchantTradePlan.ItemDefinition;
        if (city == null || !graph.ContainsExactCityOwner(city) || item == null
            || !string.Equals(seller.MerchantTradePlan.TargetCityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || !seller.CommercialKnowledge.TryGetObservation(city.LocationRuntimeId,
                item.DefinitionId, out DailyV1CommercialMarketObservation observation)
            || !ReferenceEquals(observation.ItemDefinition, item)) return false;

        EffectiveSimulationConfiguration effective = graph.Definitions.EffectiveConfiguration;
        EffectiveMerchantTradeConfiguration trade = effective?.MerchantTrade
            ?? new EffectiveMerchantTradeConfiguration(enabled: true);
        EffectiveCommercialKnowledgeConfiguration knowledge = effective?.CommercialKnowledge
            ?? new EffectiveCommercialKnowledgeConfiguration();
        float freshness = DailyV1CommercialKnowledgeFreshness.GetFreshness(
            observation.ObservedDay, currentAbsoluteDay, knowledge);
        int amount = Math.Min(trade.MaxTradeAmount, Math.Min(seller.MerchantTradePlan.RemainingAmount,
            seller.Inventory.GetAmount(item)));
        if (freshness <= 0f || amount <= 0) return true;

        float localPrice = observation.ObservedPrice;
        if (localPrice <= 0f) return true;
        if (seller.CommercialKnowledge.TryGetLiquidityObservation(city.LocationRuntimeId,
                out DailyV1CommercialLiquidityObservation liquidity)
            && DailyV1CommercialKnowledgeFreshness.GetFreshness(
                liquidity.ObservedDay, currentAbsoluteDay, knowledge) > 0f
            && liquidity.LiquidityMode != MarketLiquidityMode.Open)
            amount = Math.Min(amount, (int)Math.Floor(liquidity.ObservedPurchasingPower / localPrice));
        if (amount <= 0) return true;

        float profit = localPrice - seller.MerchantTradePlan.PurchasePricePerItem;
        float minimumProfit = Math.Max(0f,
            DailyV1NpcDefinitionRules.MinimumProfitPerItem(seller.Definition, graph.Definitions));
        bool profitable = profit >= minimumProfit;
        if (!profitable && seller.MerchantTradePlan.WaitDaysAtDestination < trade.MaxUnprofitablePlanWaitDays)
            return true;

        float gross = profit * amount;
        CommercialLiquidityEvidence liquidityEvidence = null;
        if (liquidity != null)
            liquidityEvidence = CommercialLiquidityEvidence.Capture(
                liquidity.LocationRuntimeId, liquidity.LiquidityMode,
                liquidity.ObservedPurchasingPower, liquidity.ObservedDay,
                liquidity.ReceivedDay,
                DailyV1CommercialKnowledgeFreshness.GetFreshness(
                    liquidity.ObservedDay, currentAbsoluteDay, knowledge),
                (CommercialKnowledgeSource)(int)liquidity.Source, liquidity.SourceRuntimeId);
        CommercialDecisionEvidence evidence = new CommercialDecisionEvidence(
            item.DefinitionId, city.LocationRuntimeId,
            seller.MerchantTradePlan.OriginCityRuntimeId == null
                ? city.LocationRuntimeId
                : FindCityLocation(graph, seller.MerchantTradePlan.OriginCityRuntimeId) ?? city.LocationRuntimeId,
            city.LocationRuntimeId, null, null,
            CommercialObservationEvidence.Capture(observation.LocationRuntimeId,
                observation.ObservedPrice, observation.ObservedStock, observation.ObservedDay,
                observation.ReceivedDay, freshness,
                (CommercialKnowledgeSource)(int)observation.Source, observation.SourceRuntimeId),
            amount, seller.MerchantTradePlan.PurchasePricePerItem, localPrice,
            0f, gross, gross, profitable ? profit * 5f : 0f, liquidityEvidence);
        if (!DailyV1NpcActionPayloadOwner.TryCreate(graph.Definitions, action,
                null, city.RuntimeId, item, amount, localPrice, 1f,
                NpcTravelReason.None, 0f, null, null, evidence, null,
                out DailyV1NpcActionPayloadOwner payload)) return false;

        float utility = profitable
            ? Math.Max(currentUtility, 80f + profit * 5f)
            : Math.Max(currentUtility, 35f);
        plan = new DailyV1MerchantLocalSalePlan(0, item, amount,
            seller.MerchantTradePlan.PurchasePricePerItem, localPrice, profit,
            profitable ? profit * 5f : 0f, utility, evidence, payload);
        return true;
    }

    private static string FindCityLocation(DailyV1SelectedRuntimeOwnerGraph graph, string cityRuntimeId)
    {
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            if (string.Equals(city.RuntimeId, cityRuntimeId, StringComparison.Ordinal))
                return city.LocationRuntimeId;
        return null;
    }

    /// <summary>
    /// Builds and selects the existing autonomous selected-definition local
    /// SellGoods action from the exact runtime owners. Unity only projects the
    /// returned target to its existing action wrapper.
    /// </summary>
    internal static bool TryPlanSelectedLocalSale(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        DailyV1ActionDefinition action,
        long currentAbsoluteDay,
        float currentUtility,
        out DailyV1MerchantLocalSalePlan plan)
    {
        plan = null;
        if (graph == null || npc == null || action == null
            || !graph.ContainsExactNpcOwner(npc)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
            || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, action, npc.Status?.Statuses)
            || !DailyV1NpcDefinitionRules.HasJobType(
                npc.Definition, graph.Definitions, DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            || npc.Lifecycle?.LifeState != NpcLifeState.Alive
            || npc.Travel == null || npc.Travel.IsTraveling
            || npc.MerchantTradePlan == null || npc.MerchantTradePlan.IsActive
            || npc.Inventory == null || !ReferenceEquals(npc.Inventory.Definitions, graph.Definitions)
            || npc.CommercialKnowledge == null
            || !ReferenceEquals(npc.CommercialKnowledge.Definitions, graph.Definitions))
            return false;

        DailyV1CityRuntimeOwners city = null;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            if (string.Equals(candidate.RuntimeId, npc.Travel.CurrentCityRuntimeId, StringComparison.Ordinal))
            {
                city = candidate;
                break;
            }
        if (city == null || !graph.ContainsExactCityOwner(city)
            || string.IsNullOrWhiteSpace(city.LocationRuntimeId)
            || city.Market == null || !ReferenceEquals(city.Market.Definitions, graph.Definitions))
            return false;

        EffectiveSimulationConfiguration effective = graph.Definitions.EffectiveConfiguration;
        EffectiveMerchantTradeConfiguration trade = effective?.MerchantTrade
            ?? new EffectiveMerchantTradeConfiguration(enabled: true);
        EffectiveCommercialKnowledgeConfiguration knowledge = effective?.CommercialKnowledge
            ?? new EffectiveCommercialKnowledgeConfiguration();
        int maximumTradeAmount = trade.MaxTradeAmount;
        if (maximumTradeAmount <= 0) return false;

        List<DailyV1MerchantLocalSaleFact> facts = new List<DailyV1MerchantLocalSaleFact>();
        List<DailyV1CommercialMarketObservation> observations =
            new List<DailyV1CommercialMarketObservation>();
        List<float> freshnessValues = new List<float>();
        foreach (DailyV1ItemDefinition item in graph.Definitions.Items.Values)
        {
            if (item == null
                || !npc.CommercialKnowledge.TryGetObservation(
                    city.LocationRuntimeId, item.DefinitionId,
                    out DailyV1CommercialMarketObservation observation)
                || !ReferenceEquals(observation.ItemDefinition, item))
                continue;

            float freshness = DailyV1CommercialKnowledgeFreshness.GetFreshness(
                observation.ObservedDay, currentAbsoluteDay, knowledge);
            if (freshness <= 0f) continue;

            int availableAmount = LimitAmountFromKnowledge(
                npc.CommercialKnowledge, city.LocationRuntimeId,
                observation.ObservedPrice, maximumTradeAmount,
                currentAbsoluteDay, knowledge);
            facts.Add(new DailyV1MerchantLocalSaleFact(
                item, observation.ObservedPrice, freshness, availableAmount));
            observations.Add(observation);
            freshnessValues.Add(freshness);
        }

        float minimumProfit = UnityMathfCompatibility.Max(
            0f, DailyV1NpcDefinitionRules.MinimumProfitPerItem(npc.Definition, graph.Definitions));
        if (!TryPlanLocalSale(graph.Definitions, npc.Definition, npc.Inventory,
                facts, maximumTradeAmount, minimumProfit, out DailyV1MerchantLocalSalePlan selected)
            || selected == null)
            return true;

        DailyV1CommercialMarketObservation marketObservation = observations[selected.MarketFactIndex];
        float selectedFreshness = freshnessValues[selected.MarketFactIndex];
        DailyV1CommercialLiquidityObservation liquidityObservation = null;
        float liquidityFreshness = 0f;
        if (npc.CommercialKnowledge.TryGetLiquidityObservation(
                city.LocationRuntimeId, out DailyV1CommercialLiquidityObservation liquidity))
        {
            liquidityObservation = liquidity;
            liquidityFreshness = DailyV1CommercialKnowledgeFreshness.GetFreshness(
                liquidity.ObservedDay, currentAbsoluteDay, knowledge);
        }

        float grossProfit = selected.ProfitPerItem * selected.Amount;
        CommercialDecisionEvidence evidence = new CommercialDecisionEvidence(
            selected.Item.DefinitionId,
            city.LocationRuntimeId,
            city.LocationRuntimeId,
            city.LocationRuntimeId,
            null,
            null,
            CommercialObservationEvidence.Capture(
                marketObservation.LocationRuntimeId,
                marketObservation.ObservedPrice,
                marketObservation.ObservedStock,
                marketObservation.ObservedDay,
                marketObservation.ReceivedDay,
                selectedFreshness,
                (CommercialKnowledgeSource)(int)marketObservation.Source,
                marketObservation.SourceRuntimeId),
            selected.Amount,
            selected.ReferencePrice,
            selected.LocalPrice,
            0f,
            grossProfit,
            grossProfit,
            selected.Score,
            liquidityObservation == null ? null : CommercialLiquidityEvidence.Capture(
                liquidityObservation.LocationRuntimeId,
                liquidityObservation.LiquidityMode,
                liquidityObservation.ObservedPurchasingPower,
                liquidityObservation.ObservedDay,
                liquidityObservation.ReceivedDay,
                liquidityFreshness,
                (CommercialKnowledgeSource)(int)liquidityObservation.Source,
                liquidityObservation.SourceRuntimeId));

        if (!DailyV1NpcActionPayloadOwner.TryCreate(
                graph.Definitions, action, null, city.RuntimeId, selected.Item,
                selected.Amount, selected.LocalPrice, 1f, NpcTravelReason.None,
                0f, null, null, evidence, null,
                out DailyV1NpcActionPayloadOwner selectedPayload))
            return true;

        plan = new DailyV1MerchantLocalSalePlan(
            selected.MarketFactIndex, selected.Item, selected.Amount,
            selected.ReferencePrice, selected.LocalPrice, selected.ProfitPerItem,
            selected.Score, UnityMathfCompatibility.Max(currentUtility, 25f + selected.Score),
            evidence, selectedPayload);
        return true;
    }

    private static int LimitAmountFromKnowledge(
        DailyV1CommercialKnowledgeOwner knowledgeOwner,
        string locationRuntimeId,
        float expectedUnitPrice,
        int requestedAmount,
        long currentAbsoluteDay,
        EffectiveCommercialKnowledgeConfiguration knowledgeConfiguration)
    {
        if (requestedAmount <= 0 || expectedUnitPrice <= 0f)
            return Math.Max(0, requestedAmount);
        if (!knowledgeOwner.TryGetLiquidityObservation(
                locationRuntimeId, out DailyV1CommercialLiquidityObservation observation)
            || DailyV1CommercialKnowledgeFreshness.GetFreshness(
                observation.ObservedDay, currentAbsoluteDay, knowledgeConfiguration) <= 0f
            || observation.LiquidityMode == MarketLiquidityMode.Open)
            return requestedAmount;
        return UnityMathfCompatibility.Min(
            requestedAmount,
            UnityMathfCompatibility.FloorToInt(observation.ObservedPurchasingPower / expectedUnitPrice));
    }

    internal static bool TryPlanLocalSale(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition merchant,
        DailyV1InventoryOwner inventory,
        IReadOnlyList<DailyV1MerchantLocalSaleFact> marketFacts,
        int maximumTradeAmount,
        float minimumProfitPerItem,
        out DailyV1MerchantLocalSalePlan plan)
    {
        plan = null;
        if (!DailyV1NpcDefinitionRules.IsAdmittedNpc(definitions, merchant)
            || inventory == null || !ReferenceEquals(inventory.Definitions, definitions)
            || marketFacts == null || maximumTradeAmount <= 0)
            return false;

        for (int inventoryIndex = 0; inventoryIndex < inventory.Items.Count; inventoryIndex++)
        {
            DailyV1InventoryItemState inventoryRow = inventory.Items[inventoryIndex];
            if (inventoryRow == null)
                return false;

            if (inventoryRow.Amount <= 0)
                continue;

            if (inventoryRow.Item == null
                || !definitions.Items.TryGetValue(inventoryRow.Item.DefinitionId, out DailyV1ItemDefinition admittedItem)
                || !ReferenceEquals(inventoryRow.Item, admittedItem))
                return false;

            int factIndex = FindExactMarketFact(marketFacts, inventoryRow.Item);
            if (factIndex == -2)
                return false;
            if (factIndex < 0)
                continue;
            DailyV1MerchantLocalSaleFact fact = marketFacts[factIndex];
            if (fact.Freshness <= 0f || fact.AvailableAmount <= 0)
                continue;

            int amount = UnityMathfCompatibility.Min(
                maximumTradeAmount,
                UnityMathfCompatibility.Min(inventoryRow.Amount, fact.AvailableAmount));
            if (amount <= 0)
                continue;

            float referencePrice = inventoryRow.AverageUnitCost > 0f
                ? inventoryRow.AverageUnitCost
                : inventoryRow.Item.Values.basePrice;
            float profitPerItem = fact.ObservedPrice - referencePrice;
            if (profitPerItem < UnityMathfCompatibility.Max(0f, minimumProfitPerItem))
                continue;

            float preferenceMultiplier = DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(
                merchant, definitions, inventoryRow.Item, out float configuredMultiplier)
                ? UnityMathfCompatibility.Max(0.01f, configuredMultiplier)
                : 1f;
            float score = profitPerItem * amount
                * fact.Freshness
                * preferenceMultiplier;

            // For local sales all candidates share the same destination and have no target NPC.
            // This is the remaining ordering of MerchantSystem.ShouldPreferOpportunity.
            if (plan == null
                || score > plan.Score
                || (UnityMathfCompatibility.Approximately(score, plan.Score)
                    && string.CompareOrdinal(inventoryRow.Item.DefinitionId, plan.Item.DefinitionId) < 0))
            {
                plan = new DailyV1MerchantLocalSalePlan(
                    factIndex,
                    inventoryRow.Item,
                    amount,
                    referencePrice,
                    fact.ObservedPrice,
                    profitPerItem,
                    score);
            }
        }

        return true;
    }

    private static int FindExactMarketFact(
        IReadOnlyList<DailyV1MerchantLocalSaleFact> marketFacts,
        DailyV1ItemDefinition item)
    {
        for (int index = 0; index < marketFacts.Count; index++)
        {
            DailyV1MerchantLocalSaleFact fact = marketFacts[index];
            if (fact == null)
                continue;
            if (ReferenceEquals(fact.Item, item))
                return index;
            if (fact.Item != null && string.Equals(fact.Item.DefinitionId, item.DefinitionId, StringComparison.Ordinal))
                return -2;
        }
        return -1;
    }

    internal static bool TryPlanTradeOpportunity(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition merchant,
        IReadOnlyList<DailyV1MerchantTradeCandidate> candidates,
        float minimumProfitPerItem,
        out DailyV1MerchantTradePlan plan)
    {
        plan = null;
        if (!DailyV1NpcDefinitionRules.IsAdmittedNpc(definitions, merchant) || candidates == null)
            return false;

        for (int index = 0; index < candidates.Count; index++)
        {
            DailyV1MerchantTradeCandidate candidate = candidates[index];
            if (candidate == null)
                continue;

            if (candidate.Item == null
                || !definitions.Items.TryGetValue(candidate.Item.DefinitionId, out DailyV1ItemDefinition admittedItem)
                || !ReferenceEquals(candidate.Item, admittedItem))
                return false;

            if (candidate.Amount <= 0 || candidate.BuyPrice <= 0f
                || candidate.OriginFreshness <= 0f || candidate.DestinationFreshness <= 0f
                || candidate.TotalTravelDays <= 0)
                continue;

            float profitPerItem = candidate.SellPrice - candidate.BuyPrice;
            float netProfit = profitPerItem * candidate.Amount - candidate.TotalTravelCost;
            if (profitPerItem < UnityMathfCompatibility.Max(0f, minimumProfitPerItem) || netProfit <= 0f)
                continue;

            float freshness = UnityMathfCompatibility.Min(
                candidate.OriginFreshness, candidate.DestinationFreshness);
            float preferenceMultiplier = DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(
                merchant, definitions, candidate.Item, out float configuredMultiplier)
                ? UnityMathfCompatibility.Max(0.01f, configuredMultiplier)
                : 1f;
            float score = netProfit / UnityMathfCompatibility.Max(1, candidate.TotalTravelDays)
                * freshness
                * preferenceMultiplier;
            string candidateTargetRuntimeId = candidate.TargetRuntimeId ?? string.Empty;
            string currentTargetRuntimeId = plan?.TargetRuntimeId ?? string.Empty;
            int targetComparison = plan == null
                ? -1
                : string.CompareOrdinal(candidateTargetRuntimeId, currentTargetRuntimeId);

            // MerchantSystem.ShouldPreferOpportunity orders trade choices by score,
            // then target City RuntimeId, then selected item DefinitionId. The
            // selected BuyGoods path has no target NPC.
            if (plan == null
                || score > plan.Score
                || (UnityMathfCompatibility.Approximately(score, plan.Score)
                    && (targetComparison < 0
                        || (targetComparison == 0
                            && string.CompareOrdinal(candidate.Item.DefinitionId, plan.Item.DefinitionId) < 0))))
            {
                plan = new DailyV1MerchantTradePlan(
                    index,
                    candidate.Item,
                    candidate.TargetRuntimeId,
                    candidate.Amount,
                    candidate.BuyPrice,
                    candidate.SellPrice,
                    profitPerItem,
                    netProfit,
                    score);
            }
        }

        return true;
    }
}

/// <summary>Selected-owner commercial freshness with the existing Unity policy bounds.</summary>
internal static class DailyV1CommercialKnowledgeFreshness
{
    internal static float GetFreshness(
        long observedDay,
        long currentAbsoluteDay,
        EffectiveCommercialKnowledgeConfiguration configuration)
    {
        int configuredFreshDays = configuration?.FreshForDays ?? 7;
        int configuredMaxAge = configuration?.MaxUsefulAgeDays ?? 30;
        int freshForDays = Math.Min(Math.Max(0, configuredFreshDays), int.MaxValue - 1);
        if (configuredMaxAge <= 0) configuredMaxAge = 30;
        int maxUsefulAgeDays = configuredMaxAge > freshForDays
            ? configuredMaxAge
            : freshForDays + 1;
        long effectiveDay = Math.Max(0L, currentAbsoluteDay);
        long ageDays = effectiveDay <= observedDay ? 0L : effectiveDay - observedDay;
        if (ageDays <= freshForDays) return 1f;
        if (ageDays >= maxUsefulAgeDays) return 0f;
        float decayRange = maxUsefulAgeDays - freshForDays;
        return 1f - (ageDays - freshForDays) / decayRange;
    }
}
