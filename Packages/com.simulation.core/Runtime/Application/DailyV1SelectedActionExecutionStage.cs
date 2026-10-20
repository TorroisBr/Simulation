using System;

/// <summary>Core-owned dispatch for selected actions already admitted to the
/// exact graph-owned current-action slot. P12 scopes are supplied only to the
/// existing owner operations; this stage never accepts a projected payload.</summary>
internal static class DailyV1SelectedActionExecutionStage
{
    internal static bool TryApplyActionStatusPlan(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        bool targetExpected,
        DailyV1NpcRuntimeOwners target,
        DailyV1SelectedActionStatusPlan.ApplyOwnerStatusChanges applyActor,
        DailyV1SelectedActionStatusPlan.ApplyOwnerStatusChanges applyTarget,
        out DailyV1SelectedActionStatusPlanResult result)
    {
        result = DailyV1SelectedActionStatusPlanResult.ActorRejected;
        if (graph == null
            || !graph.TryCreateActionStatusPlan(
                actor, action, targetExpected, target,
                out DailyV1SelectedActionStatusPlan plan))
            return false;

        result = plan.Apply(applyActor, applyTarget);
        return true;
    }

    internal interface ISelectedSoloTravelActionBoundary
    {
        /// <summary>Runs the complete travel operation inside the existing host P12 scope.</summary>
        bool ExecuteWithinOperation(Func<bool> operation);
        bool CanStartTradeReposition();
        void LogTradeReposition();
        /// <summary>Preserves host guards/projection checks before Core charges the owner.</summary>
        bool CanCommitPreparedTransition(DailyV1SelectedTravelTransitionPlan plan,
            SpatialRouteRuntime route, float travelCost);
        bool CommitPreparedTransition(DailyV1SelectedTravelTransitionPlan plan);
        /// <summary>Publishes the event and travel/cost logs after Core discoveries.</summary>
        void PublishTravelStarted(SoloTravelResult result);
        void LogCommercialScouting();
        /// <summary>The existing Unity owner bridge commits/reflects the exact Core plan clear.</summary>
        void ClearTravelPlan();
    }

    internal sealed class SelectedSoloTravelActionResult
    {
        internal bool Succeeded { get; }
        internal bool ActionRollSucceeded { get; }
        internal SoloTravelResult Travel { get; }

        internal SelectedSoloTravelActionResult(bool succeeded, bool actionRollSucceeded,
            SoloTravelResult travel)
        { Succeeded = succeeded; ActionRollSucceeded = actionRollSucceeded; Travel = travel; }
    }

    internal sealed class SelectedOpenMarketActionResult
    {
        internal bool Succeeded { get; }
        internal bool ActionRollSucceeded { get; }
        internal DailyV1SelectedOpenMarketSaleExecution Sale { get; }
        internal DailyV1SelectedOpenMarketPurchaseExecution Purchase { get; }

        internal SelectedOpenMarketActionResult(bool succeeded, bool actionRollSucceeded,
            DailyV1SelectedOpenMarketSaleExecution sale,
            DailyV1SelectedOpenMarketPurchaseExecution purchase)
        {
            Succeeded = succeeded;
            ActionRollSucceeded = actionRollSucceeded;
            Sale = sale;
            Purchase = purchase;
        }
    }

    internal sealed class SelectedPlannedMarketSaleActionResult
    {
        internal bool Succeeded { get; }
        internal bool ActionRollSucceeded { get; }
        internal PlannedMarketSaleResult Sale { get; }

        internal SelectedPlannedMarketSaleActionResult(bool succeeded,
            bool actionRollSucceeded, PlannedMarketSaleResult sale)
        {
            Succeeded = succeeded;
            ActionRollSucceeded = actionRollSucceeded;
            Sale = sale;
        }
    }

    internal sealed class SelectedPlannedNpcSaleActionResult
    {
        internal bool Succeeded { get; }
        internal bool ActionRollSucceeded { get; }
        internal PlannedNpcSaleResult Sale { get; }

        internal SelectedPlannedNpcSaleActionResult(bool succeeded,
            bool actionRollSucceeded, PlannedNpcSaleResult sale)
        {
            Succeeded = succeeded;
            ActionRollSucceeded = actionRollSucceeded;
            Sale = sale;
        }
    }

    internal sealed class SelectedCrossCityBuyActionResult
    {
        internal bool ActionRollSucceeded { get; }
        internal DailyV1SelectedCrossCityBuyResult Purchase { get; }

        internal SelectedCrossCityBuyActionResult(bool actionRollSucceeded,
            DailyV1SelectedCrossCityBuyResult purchase)
        {
            ActionRollSucceeded = actionRollSucceeded;
            Purchase = purchase;
        }
    }

    internal sealed class PlannedNpcSaleResult
    {
        internal bool Succeeded { get; }
        internal bool SoldToNpc { get; }
        internal int Quantity { get; }
        internal float UnitPrice { get; }
        internal bool PlanCompleted { get; }
        internal bool PlanTransitioned { get; }
        internal DailyV1SelectedNpcTradeCommitResult NpcTrade { get; }
        internal DailyV1SelectedOpenMarketSaleExecution MarketSale { get; }

        internal PlannedNpcSaleResult(bool succeeded, bool soldToNpc, int quantity, float unitPrice,
            bool planCompleted, bool planTransitioned, DailyV1SelectedNpcTradeCommitResult npcTrade,
            DailyV1SelectedOpenMarketSaleExecution marketSale)
        { Succeeded = succeeded; SoldToNpc = soldToNpc; Quantity = quantity; UnitPrice = unitPrice;
          PlanCompleted = planCompleted; PlanTransitioned = planTransitioned;
          NpcTrade = npcTrade; MarketSale = marketSale; }
    }

    internal sealed class PlannedMarketSaleResult
    {
        internal bool Succeeded { get; }
        internal bool PartialCommit { get; }
        internal int Quantity { get; }
        internal float UnitPrice { get; }
        internal bool PlanCompleted { get; }
        internal bool PlanTransitioned { get; }
        internal DailyV1SelectedOpenMarketSaleExecution MarketSale { get; }

        internal PlannedMarketSaleResult(bool succeeded, bool partialCommit, int quantity,
            float unitPrice, bool planCompleted, bool planTransitioned,
            DailyV1SelectedOpenMarketSaleExecution marketSale)
        {
            Succeeded = succeeded;
            PartialCommit = partialCommit;
            Quantity = quantity;
            UnitPrice = unitPrice;
            PlanCompleted = planCompleted;
            PlanTransitioned = planTransitioned;
            MarketSale = marketSale;
        }
    }

    internal sealed class SoloTravelResult
    {
        internal DailyV1SelectedTravelTransitionPlan Transition { get; }
        internal SpatialRouteRuntime Route { get; }
        internal float TravelCost { get; }
        internal DailyV1SelectedSoloTravelResult Execution { get; }
        internal bool Succeeded => Execution?.Succeeded == true;

        internal SoloTravelResult(DailyV1SelectedTravelTransitionPlan transition,
            SpatialRouteRuntime route, float travelCost, DailyV1SelectedSoloTravelResult execution)
        { Transition = transition; Route = route; TravelCost = travelCost; Execution = execution; }
    }

    internal static bool TryRollCurrentActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        out bool succeeded) =>
        DailyV1NpcActionDecision.TryRollCurrentActionSlot(
            graph, actor, randomSource, currentAbsoluteDay, out succeeded);

    /// <summary>Prepares and charges the exact selected Travel slot using Core
    /// route/cost/travel owners, then asks the host only to commit the already
    /// prepared transition through its existing P12 projection boundary.</summary>
    internal static bool TryExecuteSoloTravelSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        string destinationCityRuntimeId,
        TravelPartyStore partyStore,
        Func<DailyV1SelectedTravelTransitionPlan, bool> commitTransition,
        out SoloTravelResult result)
        => TryExecuteSoloTravelSlot(graph, actor, destinationCityRuntimeId, partyStore,
            null, commitTransition, out result);

    private static bool TryExecuteSoloTravelSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        string destinationCityRuntimeId,
        TravelPartyStore partyStore,
        Func<DailyV1SelectedTravelTransitionPlan, SpatialRouteRuntime, float, bool> validatePreparedTransition,
        Func<DailyV1SelectedTravelTransitionPlan, bool> commitTransition,
        out SoloTravelResult result)
    {
        result = null;
        DailyV1NpcActionSlotOwner slot = actor?.ActionSlot;
        DailyV1NpcActionPayloadOwner payload = slot?.CurrentPayload;
        if (graph == null || actor == null || !graph.ContainsExactNpcOwner(actor)
            || slot?.CurrentAction == null || payload == null
            || !ReferenceEquals(payload.Action, slot.CurrentAction)
            || slot.CurrentAction.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.TravelActionTypeValue
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, slot.CurrentAction, actor.Status?.Statuses)
            || string.IsNullOrWhiteSpace(destinationCityRuntimeId)
            || !string.Equals(payload.TargetCityRuntimeId, destinationCityRuntimeId, StringComparison.Ordinal)
            || actor.Lifecycle?.LifeState != NpcLifeState.Alive
            || actor.Travel == null || actor.Travel.IsTraveling
            || graph.IsManagedByTravelParty(actor, partyStore)
            || commitTransition == null)
            return false;

        DailyV1CityRuntimeOwners originCity = null;
        DailyV1CityRuntimeOwners destinationCity = null;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
        {
            if (string.Equals(candidate.RuntimeId, actor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal))
                originCity = candidate;
            if (string.Equals(candidate.RuntimeId, destinationCityRuntimeId, StringComparison.Ordinal))
                destinationCity = candidate;
        }
        if (originCity == null || destinationCity == null
            || !graph.ContainsExactCityOwner(originCity)
            || !graph.ContainsExactCityOwner(destinationCity)
            || !graph.TryGetCityLocationRuntimeId(destinationCity.RuntimeId, out string destinationLocationRuntimeId)
            || !graph.TryPlanSelectedSoloTravel(actor, actor.Travel.CurrentLocationRuntimeId,
                destinationLocationRuntimeId, destinationCity.RuntimeId,
                out SpatialRouteRuntime route, out float travelCost)
            || !DailyV1SelectedTravelTransitionPlan.TryStartTravel(
                actor, originCity, actor.Travel.CurrentLocationRuntimeId,
                destinationLocationRuntimeId, destinationCity, route.TravelDays,
                payload.OriginDecisionId, route.RuntimeId,
                out DailyV1SelectedTravelTransitionPlan transition))
            return false;

        if (validatePreparedTransition != null
            && !validatePreparedTransition(transition, route, travelCost))
            return false;

        DailyV1SelectedSoloTravelResult execution = DailyV1SelectedSoloTravelExecution.TryExecute(
            actor, transition, travelCost, () => commitTransition(transition));
        result = new SoloTravelResult(transition, route, travelCost, execution);
        return true;
    }

    /// <summary>
    /// Dispatches the exact selected Travel action slot through Core travel owners.
    /// The host supplies only the existing P12 operation wrapper and projection,
    /// event, and notification callbacks.
    /// </summary>
    internal static bool TryExecuteSelectedSoloTravelActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition expectedAction,
        TravelPartyStore partyStore,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        ISelectedSoloTravelActionBoundary boundary,
        out SelectedSoloTravelActionResult result)
    {
        result = null;
        DailyV1NpcActionSlotOwner slot = actor?.ActionSlot;
        if (graph == null || actor == null || expectedAction == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(slot?.CurrentAction, expectedAction)
            || expectedAction.Values.actionType?.Value != DailyV1NpcDefinitionRules.TravelActionTypeValue)
            return false;

        DailyV1NpcActionPayloadOwner payload = slot.CurrentPayload;
        if (payload == null || !ReferenceEquals(payload.Action, expectedAction)
            || payload.TargetNpcRuntimeId != null
            || payload.TargetItem != null
            || string.IsNullOrWhiteSpace(payload.TargetCityRuntimeId)
            || !Enum.IsDefined(typeof(NpcTravelReason), payload.TravelReason)
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, expectedAction, actor.Status?.Statuses)
            || boundary == null)
            return false;

        // From this point the action is an exact supported selected slot. Its
        // existing deterministic roll must precede the host P12 wrapper and all
        // travel callbacks, just as it did before selected Travel dispatch.
        if (!TryRollCurrentActionSlot(graph, actor, randomSource, currentAbsoluteDay,
                out bool actionSucceeded))
        {
            result = new SelectedSoloTravelActionResult(false, false, null);
            return true;
        }
        if (!actionSucceeded)
        {
            result = new SelectedSoloTravelActionResult(false, false, null);
            return true;
        }

        SoloTravelResult completedTravel = null;
        bool succeeded = boundary.ExecuteWithinOperation(() =>
        {
            if (payload.TravelReason == NpcTravelReason.TradeReposition)
            {
                if (!boundary.CanStartTradeReposition()) return false;
                boundary.LogTradeReposition();
            }

            string destinationCityRuntimeId = payload.TargetCityRuntimeId;
            if (string.IsNullOrWhiteSpace(destinationCityRuntimeId)
                || !TryExecuteSoloTravelSlot(graph, actor, destinationCityRuntimeId,
                    partyStore,
                    (plan, route, cost) => boundary.CanCommitPreparedTransition(plan, route, cost),
                    boundary.CommitPreparedTransition,
                    out SoloTravelResult travel)
                || travel == null || !travel.Succeeded)
                return false;

            // Discoveries are authoritative Core owner mutations and retain
            // their existing P12-bound owner callbacks.
            actor.SpatialKnowledge?.DiscoverLocation(travel.Route.Origin.RuntimeId);
            actor.SpatialKnowledge?.DiscoverRoute(travel.Route.RuntimeId);
            boundary.PublishTravelStarted(travel);
            if (payload.TravelReason == NpcTravelReason.CommercialScout)
                boundary.LogCommercialScouting();
            boundary.ClearTravelPlan();
            completedTravel = travel;
            return true;
        });

        result = new SelectedSoloTravelActionResult(succeeded, true, completedTravel);
        return true;
    }

    internal static bool TryExecuteOpenMarketSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IDailyV1SelectedOpenMarketSaleBoundary saleBoundary,
        IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary,
        out DailyV1SelectedOpenMarketSaleExecution sale,
        out DailyV1SelectedOpenMarketPurchaseExecution purchase)
    {
        sale = null;
        purchase = null;
        if (graph == null || actor == null || !graph.ContainsExactNpcOwner(actor)
            || actor.ActionSlot?.CurrentAction == null
            || actor.ActionSlot.CurrentPayload == null
            || !ReferenceEquals(actor.ActionSlot.CurrentPayload.Action, actor.ActionSlot.CurrentAction))
            return false;

        int actionType = actor.ActionSlot.CurrentAction.Values.actionType?.Value ?? int.MinValue;
        if (actionType != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            && actionType != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue)
            return false;

        return DailyV1SelectedOpenMarketActionExecution.TryExecute(
            graph, actor, actor.ActionSlot.CurrentPayload, saleBoundary, purchaseBoundary,
            out sale, out purchase);
    }

    /// <summary>Rolls and executes an exact selected local BuyGoods/SellGoods slot.
    /// The host supplies only the existing P12 transaction boundaries.</summary>
    internal static bool TryExecuteSelectedOpenMarketActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition expectedAction,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        IDailyV1SelectedOpenMarketSaleBoundary saleBoundary,
        IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary,
        out SelectedOpenMarketActionResult result)
    {
        result = null;
        DailyV1NpcActionSlotOwner slot = actor?.ActionSlot;
        DailyV1NpcActionPayloadOwner payload = slot?.CurrentPayload;
        int actionType = slot?.CurrentAction?.Values.actionType?.Value ?? int.MinValue;
        if (graph == null || actor == null || expectedAction == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || !ReferenceEquals(slot?.CurrentAction, expectedAction)
            || payload == null || !ReferenceEquals(payload.Action, expectedAction)
            || (actionType != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
                && actionType != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue)
            || payload.TargetNpcRuntimeId != null
            || string.IsNullOrWhiteSpace(actor.Travel?.CurrentCityRuntimeId)
            || !string.Equals(payload.TargetCityRuntimeId,
                actor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)
            || actor.Travel.IsTraveling
            || (actionType == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
                && actor.MerchantTradePlan?.IsActive == true))
            return false;

        if (!TryRollCurrentActionSlot(graph, actor, randomSource, currentAbsoluteDay,
            out bool actionRollSucceeded))
            return false;

        if (!actionRollSucceeded)
        {
            result = new SelectedOpenMarketActionResult(false, false, null, null);
            return true;
        }

        bool succeeded = TryExecuteOpenMarketSlot(graph, actor, saleBoundary,
            purchaseBoundary, out DailyV1SelectedOpenMarketSaleExecution sale,
            out DailyV1SelectedOpenMarketPurchaseExecution purchase);
        result = new SelectedOpenMarketActionResult(succeeded, true, sale, purchase);
        return true;
    }

    /// <summary>Rolls and executes the exact selected active-plan local sale.
    /// Existing P12 plan admission, market transaction, and plan publication
    /// callbacks retain their established ordering.</summary>
    internal static bool TryExecuteSelectedPlannedMarketSaleActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners seller,
        DailyV1ActionDefinition expectedAction,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        IDailyV1SelectedOpenMarketSaleBoundary marketBoundary,
        Func<bool> planAdmission,
        Action planCommitted,
        out SelectedPlannedMarketSaleActionResult result)
    {
        result = null;
        DailyV1NpcActionSlotOwner slot = seller?.ActionSlot;
        DailyV1NpcActionPayloadOwner payload = slot?.CurrentPayload;
        if (graph == null || seller == null || expectedAction == null
            || !graph.ContainsExactNpcOwner(seller)
            || !ReferenceEquals(seller.Definitions, graph.Definitions)
            || !ReferenceEquals(slot?.CurrentAction, expectedAction)
            || payload == null || !ReferenceEquals(payload.Action, expectedAction)
            || expectedAction.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            || payload.TargetNpcRuntimeId != null
            || string.IsNullOrWhiteSpace(seller.Travel?.CurrentCityRuntimeId)
            || !string.Equals(payload.TargetCityRuntimeId,
                seller.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)
            || seller.Travel.IsTraveling
            || seller.MerchantTradePlan?.IsActive != true)
            return false;

        if (!TryRollCurrentActionSlot(graph, seller, randomSource, currentAbsoluteDay,
            out bool actionRollSucceeded))
            return false;

        if (!actionRollSucceeded)
        {
            result = new SelectedPlannedMarketSaleActionResult(false, false, null);
            return true;
        }

        bool succeeded = TryExecutePlannedMarketSaleSlot(graph, seller, marketBoundary,
            planAdmission, planCommitted, out PlannedMarketSaleResult sale);
        result = new SelectedPlannedMarketSaleActionResult(succeeded && sale?.Succeeded == true,
            true, sale);
        return true;
    }

    /// <summary>Executes the selected active-plan SellGoods payload against its
    /// exact buyer/seller owners, falls back to the same local market sale on
    /// transaction rejection, and advances the Core plan after a committed
    /// sale. The callbacks are limited to existing P12 plan admission and receipt.</summary>
    internal static bool TryExecutePlannedNpcSaleSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners seller,
        DailyV1NpcRuntimeOwners buyer,
        IDailyV1SelectedNpcTradeBoundary tradeBoundary,
        IDailyV1SelectedOpenMarketSaleBoundary marketBoundary,
        Func<bool> planAdmission,
        Action planCommitted,
        out PlannedNpcSaleResult result)
    {
        result = null;
        DailyV1NpcActionSlotOwner slot = seller?.ActionSlot;
        DailyV1NpcActionPayloadOwner payload = slot?.CurrentPayload;
        DailyV1CityRuntimeOwners city = null;
        if (graph != null && !string.IsNullOrWhiteSpace(seller?.Travel?.CurrentCityRuntimeId))
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
                if (string.Equals(candidate.RuntimeId, seller.Travel.CurrentCityRuntimeId, StringComparison.Ordinal))
                { city = candidate; break; }
        if (graph == null || seller == null || buyer == null
            || !graph.ContainsExactNpcOwner(seller) || !graph.ContainsExactNpcOwner(buyer)
            || !ReferenceEquals(seller.Definitions, graph.Definitions)
            || !ReferenceEquals(buyer.Definitions, graph.Definitions)
            || slot?.CurrentAction == null || payload == null
            || !ReferenceEquals(payload.Action, slot.CurrentAction)
            || slot.CurrentAction.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, slot.CurrentAction, seller.Status?.Statuses)
            || !string.Equals(payload.TargetNpcRuntimeId, buyer.RuntimeId, StringComparison.Ordinal)
            || payload.TargetItem == null || payload.Amount <= 0
            || !seller.MerchantTradePlan.IsActive
            || !ReferenceEquals(payload.TargetItem, seller.MerchantTradePlan.ItemDefinition)
            || seller.Lifecycle?.LifeState != NpcLifeState.Alive
            || buyer.Lifecycle?.LifeState != NpcLifeState.Alive
            || seller.Travel == null || buyer.Travel == null
            || seller.Travel.IsTraveling || buyer.Travel.IsTraveling
            || string.IsNullOrWhiteSpace(seller.Travel.CurrentCityRuntimeId)
            || (payload.TargetCityRuntimeId != null
                && !string.Equals(payload.TargetCityRuntimeId, seller.Travel.CurrentCityRuntimeId, StringComparison.Ordinal))
            || !string.Equals(seller.Travel.CurrentCityRuntimeId, buyer.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)
            || city == null || !graph.ContainsExactCityOwner(city)
            || !DailyV1NpcDefinitionRules.HasJobType(buyer.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            || !DailyV1NpcDefinitionRules.HasMerchantBehavior(buyer.Definition, graph.Definitions, 1))
            return false;

        EffectiveMerchantTradeConfiguration tradeConfig = graph.Definitions.EffectiveConfiguration?.MerchantTrade
            ?? new EffectiveMerchantTradeConfiguration(enabled: true);
        float retailPrice = city.Market.GetPrice(payload.TargetItem);
        float preference = DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(
            buyer.Definition, graph.Definitions, payload.TargetItem, out float configuredPreference)
            ? UnityMathfCompatibility.Max(0.01f, configuredPreference) : 1f;
        float expectedUnitPrice = UnityMathfCompatibility.Max(0.01f, retailPrice)
            * UnityMathfCompatibility.Clamp(
                tradeConfig.LocalWholesalePriceMultiplier
                + UnityMathfCompatibility.Min(0.15f,
                    UnityMathfCompatibility.Max(0f, preference - 1f) * 0.1f), 0.5f, 0.85f);
        float spendable = UnityMathfCompatibility.Max(0f,
            buyer.MoneyAccount.Balance * (1f - tradeConfig.LocalReserveRatio));
        if (!expectedUnitPrice.Equals(payload.ExpectedUnitPrice)
            || spendable < payload.ExpectedUnitPrice * payload.Amount)
            return false;

        if (!seller.MerchantTradePlan.CanRegisterSale(payload.Amount))
        {
            result = new PlannedNpcSaleResult(false, false, 0, 0f,
                false, false, null, null);
            return true;
        }

        // The merchant-plan P12 mutation is the second owner transition after
        // the trade. Reserve its existing admission before any counterparty
        // writes so a rejected plan mutation cannot strand a completed sale.
        bool planAdmitted;
        try { planAdmitted = planAdmission == null || planAdmission(); }
        catch { planAdmitted = false; }
        if (!planAdmitted)
        {
            result = new PlannedNpcSaleResult(false, false, 0, 0f,
                false, false, null, null);
            return true;
        }

        DailyV1SelectedNpcTradeCommitResult trade = DailyV1SelectedNpcTradeExecution.TryExecute(
            graph, buyer, seller, payload.TargetItem, payload.Amount,
            payload.ExpectedUnitPrice, tradeBoundary);
        if (trade.Succeeded)
        {
            bool transitioned = seller.MerchantTradePlan.TryRegisterSale(
                payload.Amount, null, planCommitted, out bool completed);
            result = new PlannedNpcSaleResult(true, true, payload.Amount,
                payload.ExpectedUnitPrice, transitioned && completed, transitioned, trade, null);
            return true;
        }

        DailyV1SelectedOpenMarketSaleExecution market =
            DailyV1SelectedOpenMarketSaleExecution.TryExecute(
                graph, seller, city.Market, payload.TargetItem, payload.Amount, marketBoundary);
        if (!market.Prepared || market.Commit?.Succeeded != true || market.Plan.Quantity <= 0)
        {
            result = new PlannedNpcSaleResult(false, false, 0, 0f,
                false, false, trade, market);
            return true;
        }
        bool marketTransitioned = seller.MerchantTradePlan.TryRegisterSale(
            market.Plan.Quantity, null, planCommitted, out bool marketCompleted);
        result = new PlannedNpcSaleResult(true, false, market.Plan.Quantity,
            market.Plan.UnitPrice, marketTransitioned && marketCompleted,
            marketTransitioned, trade, market);
        return true;
    }

    internal static bool TryExecuteSelectedPlannedNpcSaleActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners seller,
        DailyV1NpcRuntimeOwners buyer,
        DailyV1ActionDefinition expectedAction,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        IDailyV1SelectedNpcTradeBoundary tradeBoundary,
        IDailyV1SelectedOpenMarketSaleBoundary marketBoundary,
        Func<bool> planAdmission,
        Action planCommitted,
        out SelectedPlannedNpcSaleActionResult result)
    {
        result = null;
        DailyV1NpcActionSlotOwner slot = seller?.ActionSlot;
        DailyV1NpcActionPayloadOwner payload = slot?.CurrentPayload;
        DailyV1NpcMerchantTradePlanOwner plan = seller?.MerchantTradePlan;
        DailyV1CityRuntimeOwners city = null;
        if (graph != null && !string.IsNullOrWhiteSpace(seller?.Travel?.CurrentCityRuntimeId))
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
                if (string.Equals(candidate.RuntimeId, seller.Travel.CurrentCityRuntimeId,
                    StringComparison.Ordinal))
                { city = candidate; break; }

        if (graph == null || seller == null || buyer == null || expectedAction == null
            || !graph.ContainsExactNpcOwner(seller) || !graph.ContainsExactNpcOwner(buyer)
            || !ReferenceEquals(seller.Definitions, graph.Definitions)
            || !ReferenceEquals(buyer.Definitions, graph.Definitions)
            || !graph.Definitions.Actions.TryGetValue(expectedAction.DefinitionId,
                out DailyV1ActionDefinition admittedAction)
            || !ReferenceEquals(admittedAction, expectedAction)
            || !ReferenceEquals(slot?.CurrentAction, expectedAction)
            || payload == null || !ReferenceEquals(payload.Action, expectedAction)
            || expectedAction.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            || !string.Equals(payload.TargetNpcRuntimeId, buyer.RuntimeId, StringComparison.Ordinal)
            || payload.TargetItem == null || payload.Amount <= 0
            || !graph.Definitions.Items.TryGetValue(payload.TargetItem.DefinitionId,
                out DailyV1ItemDefinition admittedItem)
            || !ReferenceEquals(admittedItem, payload.TargetItem)
            || plan == null || !plan.IsActive
            || !ReferenceEquals(payload.TargetItem, plan.ItemDefinition)
            || (payload.TargetCityRuntimeId != null
                && !string.Equals(payload.TargetCityRuntimeId,
                    seller.Travel?.CurrentCityRuntimeId, StringComparison.Ordinal))
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, expectedAction, seller.Status?.Statuses)
            || seller.Lifecycle?.LifeState != NpcLifeState.Alive
            || buyer.Lifecycle?.LifeState != NpcLifeState.Alive
            || seller.Travel == null || buyer.Travel == null
            || seller.Travel.IsTraveling || buyer.Travel.IsTraveling
            || string.IsNullOrWhiteSpace(seller.Travel.CurrentCityRuntimeId)
            || !string.Equals(seller.Travel.CurrentCityRuntimeId,
                buyer.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)
            || city == null || !graph.ContainsExactCityOwner(city)
            || !DailyV1NpcDefinitionRules.HasJobType(buyer.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            || !DailyV1NpcDefinitionRules.HasMerchantBehavior(buyer.Definition,
                graph.Definitions, DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue))
            return false;

        if (!TryRollCurrentActionSlot(graph, seller, randomSource, currentAbsoluteDay,
            out bool actionRollSucceeded))
        {
            result = new SelectedPlannedNpcSaleActionResult(false, false, null);
            return true;
        }

        if (!actionRollSucceeded)
        {
            result = new SelectedPlannedNpcSaleActionResult(false, false, null);
            return true;
        }

        bool executed = TryExecutePlannedNpcSaleSlot(graph, seller, buyer,
            tradeBoundary, marketBoundary, planAdmission, planCommitted,
            out PlannedNpcSaleResult sale);
        result = new SelectedPlannedNpcSaleActionResult(
            executed && sale?.Succeeded == true, true, sale);
        return true;
    }

    /// <summary>Rolls and executes an exact selected cross-city BuyGoods slot in
    /// Core. The existing purchase boundary and owner admission callbacks retain
    /// their established P12 and publication order.</summary>
    internal static bool TryExecuteSelectedCrossCityBuyActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition expectedAction,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary,
        Func<bool> merchantPlanAdmission,
        Action merchantPlanCommitted,
        Func<bool> travelPlanAdmission,
        Action travelPlanCommitted,
        out SelectedCrossCityBuyActionResult result)
    {
        result = null;
        DailyV1NpcActionSlotOwner slot = actor?.ActionSlot;
        DailyV1NpcActionPayloadOwner payload = slot?.CurrentPayload;
        if (graph == null || actor == null || expectedAction == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || !graph.Definitions.Actions.TryGetValue(expectedAction.DefinitionId,
                out DailyV1ActionDefinition admittedAction)
            || !ReferenceEquals(admittedAction, expectedAction)
            || !ReferenceEquals(slot?.CurrentAction, expectedAction)
            || payload == null || !ReferenceEquals(payload.Action, expectedAction)
            || expectedAction.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue
            || payload.TargetItem == null
            || !graph.Definitions.Items.TryGetValue(payload.TargetItem.DefinitionId,
                out DailyV1ItemDefinition admittedItem)
            || !ReferenceEquals(admittedItem, payload.TargetItem)
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, expectedAction, actor.Status?.Statuses)
            || !DailyV1SelectedCrossCityBuyExecution.IsValidSelectedBuy(
                graph, actor, payload, out _, out _, out _))
            return false;

        if (!TryRollCurrentActionSlot(graph, actor, randomSource, currentAbsoluteDay,
            out bool actionRollSucceeded))
            return false;
        if (!actionRollSucceeded)
        {
            result = new SelectedCrossCityBuyActionResult(false, null);
            return true;
        }

        DailyV1SelectedCrossCityBuyResult purchase = DailyV1SelectedCrossCityBuyExecution.TryExecute(
            graph, actor, payload, purchaseBoundary, merchantPlanAdmission,
            merchantPlanCommitted, travelPlanAdmission, travelPlanCommitted);
        result = new SelectedCrossCityBuyActionResult(true, purchase);
        return true;
    }

    /// <summary>Executes an active selected merchant plan's market liquidation
    /// from its exact action slot. Plan revision/admission is checked before the
    /// sale; a post-sale plan rejection is reported as a partial commit for the
    /// host admission adapter to fault.</summary>
    internal static bool TryExecutePlannedMarketSaleSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners seller,
        IDailyV1SelectedOpenMarketSaleBoundary marketBoundary,
        Func<bool> planAdmission,
        Action planCommitted,
        out PlannedMarketSaleResult result)
    {
        result = null;
        DailyV1NpcActionSlotOwner slot = seller?.ActionSlot;
        DailyV1NpcActionPayloadOwner payload = slot?.CurrentPayload;
        DailyV1NpcMerchantTradePlanOwner plan = seller?.MerchantTradePlan;
        DailyV1CityRuntimeOwners city = null;
        if (graph != null && !string.IsNullOrWhiteSpace(seller?.Travel?.CurrentCityRuntimeId))
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
                if (string.Equals(candidate.RuntimeId, seller.Travel.CurrentCityRuntimeId,
                    StringComparison.Ordinal))
                { city = candidate; break; }

        if (graph == null || seller == null || !graph.ContainsExactNpcOwner(seller)
            || !ReferenceEquals(seller.Definitions, graph.Definitions)
            || slot?.CurrentAction == null || payload == null
            || !ReferenceEquals(payload.Action, slot.CurrentAction)
            || slot.CurrentAction.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, slot.CurrentAction, seller.Status?.Statuses)
            || payload.TargetNpcRuntimeId != null
            || payload.TargetItem == null
            || payload.Amount <= 0
            || plan == null || !plan.IsActive
            || !ReferenceEquals(payload.TargetItem, plan.ItemDefinition)
            || payload.Amount > plan.RemainingAmount
            || seller.Lifecycle?.LifeState != NpcLifeState.Alive
            || seller.Travel == null || seller.Travel.IsTraveling
            || city == null || !graph.ContainsExactCityOwner(city)
            || !string.Equals(payload.TargetCityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(plan.TargetCityRuntimeId, city.RuntimeId, StringComparison.Ordinal))
            return false;

        if (!plan.CanRegisterSale(payload.Amount))
        {
            result = new PlannedMarketSaleResult(false, false, 0, 0f,
                false, false, null);
            return true;
        }

        bool admitted;
        try { admitted = planAdmission == null || planAdmission(); }
        catch { admitted = false; }
        if (!admitted)
        {
            result = new PlannedMarketSaleResult(false, false, 0, 0f,
                false, false, null);
            return true;
        }

        DailyV1SelectedOpenMarketSaleExecution marketSale =
            DailyV1SelectedOpenMarketSaleExecution.TryExecute(
                graph, seller, city.Market, payload.TargetItem, payload.Amount,
                marketBoundary);
        if (!marketSale.Prepared || marketSale.Commit?.Succeeded != true
            || marketSale.Plan.Quantity <= 0)
        {
            result = new PlannedMarketSaleResult(false,
                marketSale.Commit?.PartialCommit == true, 0, 0f,
                false, false, marketSale);
            return true;
        }

        bool transitioned = plan.TryRegisterSale(
            marketSale.Plan.Quantity, planAdmission, planCommitted,
            out bool completed);
        result = new PlannedMarketSaleResult(transitioned, !transitioned,
            marketSale.Plan.Quantity, marketSale.Plan.UnitPrice,
            transitioned && completed, transitioned, marketSale);
        return true;
    }
}

/// <summary>
/// Host-owned ports needed to admit and publish selected-action operations.
/// The host resolves the exact Unity projections and P12 operation adapters;
/// action routing and domain execution stay in Simulation.Core.
/// </summary>
internal interface IDailyV1SelectedActionDispatchBoundary
{
    bool TryGetSoloTravelBoundary(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        out TravelPartyStore partyStore,
        out DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary boundary);

    bool TryGetMarketBoundaries(
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1SelectedOpenMarketSaleBoundary saleBoundary,
        out IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary);

    bool TryGetNpcTradeBoundary(
        DailyV1NpcRuntimeOwners seller,
        DailyV1NpcRuntimeOwners buyer,
        out IDailyV1SelectedNpcTradeBoundary boundary);

    bool CanAdmitMerchantPlanMutation(DailyV1NpcRuntimeOwners actor);
    void PublishMerchantPlanMutation(DailyV1NpcRuntimeOwners actor);
    bool CanAdmitTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor);
    void PublishTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor);

    /// <summary>Completes the existing host projection after Core commits a cross-city buy.</summary>
    void CompleteCrossCityBuy(
        DailyV1NpcRuntimeOwners actor,
        DailyV1SelectedCrossCityBuyResult result);
}

internal enum DailyV1SelectedActionDispatchKind
{
    PlannedNpcSale,
    PlannedMarketSale,
    OpenMarket,
    CrossCityBuy,
    SoloTravel,
    Normal
}

/// <summary>Result of routing one exact selected owner action slot.</summary>
internal sealed class DailyV1SelectedActionDispatchResult
{
    internal DailyV1SelectedActionDispatchKind Kind { get; }
    internal bool ActionRollSucceeded { get; }
    internal bool Succeeded { get; }
    internal float ReferencePrice { get; }
    internal float ReferenceUnitCost { get; }
    internal string NormalActionName { get; }
    internal string NormalActionLogText { get; }
    internal DailyV1SelectedActionExecutionStage.SelectedPlannedNpcSaleActionResult PlannedNpcSale { get; }
    internal DailyV1SelectedActionExecutionStage.SelectedPlannedMarketSaleActionResult PlannedMarketSale { get; }
    internal DailyV1SelectedActionExecutionStage.SelectedOpenMarketActionResult OpenMarket { get; }
    internal DailyV1SelectedActionExecutionStage.SelectedCrossCityBuyActionResult CrossCityBuy { get; }
    internal DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult SoloTravel { get; }

    internal DailyV1SelectedActionDispatchResult(
        DailyV1SelectedActionDispatchKind kind,
        bool actionRollSucceeded,
        bool succeeded,
        float referencePrice = 0f,
        float referenceUnitCost = 0f,
        DailyV1SelectedActionExecutionStage.SelectedPlannedNpcSaleActionResult plannedNpcSale = null,
        DailyV1SelectedActionExecutionStage.SelectedPlannedMarketSaleActionResult plannedMarketSale = null,
        DailyV1SelectedActionExecutionStage.SelectedOpenMarketActionResult openMarket = null,
        DailyV1SelectedActionExecutionStage.SelectedCrossCityBuyActionResult crossCityBuy = null,
        DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult soloTravel = null,
        string normalActionName = null,
        string normalActionLogText = null)
    {
        Kind = kind;
        ActionRollSucceeded = actionRollSucceeded;
        Succeeded = succeeded;
        ReferencePrice = referencePrice;
        ReferenceUnitCost = referenceUnitCost;
        NormalActionName = normalActionName;
        NormalActionLogText = normalActionLogText;
        PlannedNpcSale = plannedNpcSale;
        PlannedMarketSale = plannedMarketSale;
        OpenMarket = openMarket;
        CrossCityBuy = crossCityBuy;
        SoloTravel = soloTravel;
    }
}

/// <summary>
/// Owns dispatch for selected Daily-v1 Normal, Travel and Merchant action
/// slots already supported by Core. Unmigrated provider families remain
/// explicitly unhandled for the existing host-provider path.
/// </summary>
internal static class DailyV1SelectedActionDispatchStage
{
    internal static bool TryDispatch(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        IDailyV1SelectedActionDispatchBoundary boundary,
        out DailyV1SelectedActionDispatchResult result)
    {
        result = null;
        if (graph == null || actor == null || randomSource == null || boundary == null
            || currentAbsoluteDay < 0L
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions))
            return false;

        DailyV1NpcActionSlotOwner slot = actor.ActionSlot;
        DailyV1ActionDefinition action = slot?.CurrentAction;
        DailyV1NpcActionPayloadOwner payload = slot?.CurrentPayload;
        int actionType = action?.Values.actionType?.Value ?? int.MinValue;
        if (action == null || payload == null || !ReferenceEquals(payload.Action, action))
            return false;

        if (actionType == DailyV1NpcDefinitionRules.NormalActionTypeValue)
        {
            result = new DailyV1SelectedActionDispatchResult(
                DailyV1SelectedActionDispatchKind.Normal,
                true,
                true,
                normalActionName: DailyV1NpcDefinitionRules.GetActionName(graph.Definitions, action),
                normalActionLogText: DailyV1NpcDefinitionRules.GetNormalActionLogText(graph.Definitions, action));
            return true;
        }

        if (actionType == DailyV1NpcDefinitionRules.TravelActionTypeValue)
        {
            if (payload.TargetNpcRuntimeId != null || payload.TargetItem != null
                || string.IsNullOrWhiteSpace(payload.TargetCityRuntimeId)
                || !boundary.TryGetSoloTravelBoundary(actor, action,
                    out TravelPartyStore partyStore,
                    out DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary travelBoundary)
                || travelBoundary == null)
                return false;

            if (!DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
                    graph, actor, action, partyStore, randomSource, currentAbsoluteDay,
                    travelBoundary,
                    out DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult travel))
                return false;

            result = new DailyV1SelectedActionDispatchResult(
                DailyV1SelectedActionDispatchKind.SoloTravel,
                travel?.ActionRollSucceeded == true,
                travel?.Succeeded == true,
                soloTravel: travel);
            return true;
        }

        if (actionType != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue
            && actionType != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue)
            return false;

        if (payload.TargetItem == null || payload.Amount <= 0
            || string.IsNullOrWhiteSpace(payload.TargetCityRuntimeId)
            || actor.Travel == null || actor.Travel.IsTraveling
            || string.IsNullOrWhiteSpace(actor.Travel.CurrentCityRuntimeId))
            return false;

        bool activeMerchantPlan = actor.MerchantTradePlan?.IsActive == true;
        if (actionType == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            && activeMerchantPlan && payload.TargetNpcRuntimeId != null)
        {
            DailyV1NpcRuntimeOwners buyer = FindExactNpc(graph, payload.TargetNpcRuntimeId);
            if (buyer == null
                || !boundary.TryGetNpcTradeBoundary(actor, buyer,
                    out IDailyV1SelectedNpcTradeBoundary tradeBoundary)
                || !boundary.TryGetMarketBoundaries(actor,
                    out IDailyV1SelectedOpenMarketSaleBoundary marketBoundary, out _))
                return false;

            float referencePrice = actor.MerchantTradePlan.PurchasePricePerItem;
            if (!DailyV1SelectedActionExecutionStage.TryExecuteSelectedPlannedNpcSaleActionSlot(
                    graph, actor, buyer, action, randomSource, currentAbsoluteDay,
                    tradeBoundary, marketBoundary,
                    () => boundary.CanAdmitMerchantPlanMutation(actor),
                    () => boundary.PublishMerchantPlanMutation(actor),
                    out DailyV1SelectedActionExecutionStage.SelectedPlannedNpcSaleActionResult sale))
                return false;

            result = new DailyV1SelectedActionDispatchResult(
                DailyV1SelectedActionDispatchKind.PlannedNpcSale,
                sale?.ActionRollSucceeded == true,
                sale?.ActionRollSucceeded == true && sale.Succeeded,
                referencePrice: referencePrice,
                plannedNpcSale: sale);
            return true;
        }

        if (actionType == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            && activeMerchantPlan)
        {
            if (payload.TargetNpcRuntimeId != null
                || !string.Equals(payload.TargetCityRuntimeId,
                    actor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)
                || !boundary.TryGetMarketBoundaries(actor,
                    out IDailyV1SelectedOpenMarketSaleBoundary marketBoundary, out _))
                return false;

            float referencePrice = actor.MerchantTradePlan.PurchasePricePerItem;
            if (!DailyV1SelectedActionExecutionStage.TryExecuteSelectedPlannedMarketSaleActionSlot(
                    graph, actor, action, randomSource, currentAbsoluteDay,
                    marketBoundary,
                    () => boundary.CanAdmitMerchantPlanMutation(actor),
                    () => boundary.PublishMerchantPlanMutation(actor),
                    out DailyV1SelectedActionExecutionStage.SelectedPlannedMarketSaleActionResult sale))
                return false;

            result = new DailyV1SelectedActionDispatchResult(
                DailyV1SelectedActionDispatchKind.PlannedMarketSale,
                sale?.ActionRollSucceeded == true,
                sale?.ActionRollSucceeded == true && sale.Succeeded,
                referencePrice: referencePrice,
                plannedMarketSale: sale);
            return true;
        }

        if (payload.TargetNpcRuntimeId != null) return false;
        if (!boundary.TryGetMarketBoundaries(actor,
            out IDailyV1SelectedOpenMarketSaleBoundary saleBoundary,
            out IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary))
            return false;

        if (string.Equals(payload.TargetCityRuntimeId,
                actor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal))
        {
            float referenceUnitCost = actionType == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
                ? actor.Inventory?.GetAverageUnitCost(payload.TargetItem) ?? 0f
                : 0f;
            if (!DailyV1SelectedActionExecutionStage.TryExecuteSelectedOpenMarketActionSlot(
                    graph, actor, action, randomSource, currentAbsoluteDay,
                    saleBoundary, purchaseBoundary,
                    out DailyV1SelectedActionExecutionStage.SelectedOpenMarketActionResult market))
                return false;

            result = new DailyV1SelectedActionDispatchResult(
                DailyV1SelectedActionDispatchKind.OpenMarket,
                market?.ActionRollSucceeded == true,
                market?.ActionRollSucceeded == true && market.Succeeded,
                referenceUnitCost: referenceUnitCost,
                openMarket: market);
            return true;
        }

        if (actionType != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue)
            return false;

        if (!DailyV1SelectedActionExecutionStage.TryExecuteSelectedCrossCityBuyActionSlot(
                graph, actor, action, randomSource, currentAbsoluteDay,
                purchaseBoundary,
                () => boundary.CanAdmitMerchantPlanMutation(actor),
                () => boundary.PublishMerchantPlanMutation(actor),
                () => boundary.CanAdmitTradeTravelPlanMutation(actor),
                () => boundary.PublishTradeTravelPlanMutation(actor),
                out DailyV1SelectedActionExecutionStage.SelectedCrossCityBuyActionResult crossCityBuy))
            return false;

        result = new DailyV1SelectedActionDispatchResult(
            DailyV1SelectedActionDispatchKind.CrossCityBuy,
            crossCityBuy?.ActionRollSucceeded == true,
            crossCityBuy?.ActionRollSucceeded == true && crossCityBuy.Purchase?.Succeeded == true,
            crossCityBuy: crossCityBuy);
        boundary.CompleteCrossCityBuy(actor, crossCityBuy?.Purchase);
        return true;
    }

    private static DailyV1NpcRuntimeOwners FindExactNpc(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string runtimeId)
    {
        foreach (DailyV1NpcRuntimeOwners candidate in graph.Npcs)
            if (candidate != null
                && string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
                return graph.ContainsExactNpcOwner(candidate) ? candidate : null;
        return null;
    }
}
