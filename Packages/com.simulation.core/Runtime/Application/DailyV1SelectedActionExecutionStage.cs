using System;

/// <summary>Core-owned dispatch for selected actions already admitted to the
/// exact graph-owned current-action slot. P12 scopes are supplied only to the
/// existing owner operations; this stage never accepts a projected payload.</summary>
internal static class DailyV1SelectedActionExecutionStage
{
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
