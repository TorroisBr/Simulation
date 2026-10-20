using System;

internal enum DailyV1SelectedCrossCityBuyFailure
{
    None,
    InvalidInput,
    PurchasePreparation,
    MerchantPlanPreflight,
    MerchantPlanAdmission,
    TravelPlanPreflight,
    TravelPlanAdmission,
    PurchaseCommit,
    PostPurchaseOwnerCommit
}

internal sealed class DailyV1SelectedCrossCityBuyResult
{
    internal bool Succeeded { get; }
    internal bool PartialCommit { get; }
    internal DailyV1SelectedCrossCityBuyFailure Failure { get; }
    internal DailyV1SelectedOpenMarketPurchasePlan PurchasePlan { get; }
    internal DailyV1SelectedOpenMarketPurchaseCommitResult PurchaseCommit { get; }
    internal string OriginDecisionId { get; }
    internal int Quantity => PurchasePlan?.Quantity ?? 0;
    internal float UnitPrice => PurchasePlan?.UnitPrice ?? 0f;
    internal float TravelCost { get; }

    internal DailyV1SelectedCrossCityBuyResult(
        bool succeeded, DailyV1SelectedCrossCityBuyFailure failure,
        DailyV1SelectedOpenMarketPurchasePlan purchasePlan = null,
        DailyV1SelectedOpenMarketPurchaseCommitResult purchaseCommit = null,
        string originDecisionId = null, float travelCost = 0f,
        bool partialCommit = false)
    {
        Succeeded = succeeded;
        Failure = failure;
        PurchasePlan = purchasePlan;
        PurchaseCommit = purchaseCommit;
        OriginDecisionId = originDecisionId;
        TravelCost = travelCost;
        PartialCommit = partialCommit;
    }
}

/// <summary>
/// Prepares and commits the selected Merchant BuyGoods purchase together with
/// its exact merchant and direct-route travel plans. Host callbacks provide
/// existing P12 admission and post-commit publication only.
/// </summary>
internal static class DailyV1SelectedCrossCityBuyExecution
{
    private const float TradeTravelUtility = 70f;

    internal static DailyV1SelectedCrossCityBuyResult TryExecute(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload,
        IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary,
        Func<bool> merchantPlanAdmission,
        Action merchantPlanCommitted,
        Func<bool> travelPlanAdmission,
        Action travelPlanCommitted)
    {
        if (!IsValidSelectedBuy(graph, actor, payload, out DailyV1CityRuntimeOwners origin,
                out DailyV1CityRuntimeOwners destination,
                out SpatialLocationRuntime targetLocation))
            return Failed(DailyV1SelectedCrossCityBuyFailure.InvalidInput);

        if (!graph.TryPrepareOpenMarketPurchase(actor, origin.Market, payload.TargetItem,
                payload.Amount, purchaseBoundary?.CanUseAccountBacked ?? true,
                out DailyV1SelectedOpenMarketPurchasePlan purchasePlan,
                out DailyV1SelectedOpenMarketPurchaseFailure purchaseFailure))
            return Failed(DailyV1SelectedCrossCityBuyFailure.PurchasePreparation, purchaseFailure);

        string originDecisionId = NormalizeOptionalId(payload.OriginDecisionId);
        DailyV1NpcMerchantTradePlanOwner merchantPlan = actor.MerchantTradePlan;
        if (merchantPlan == null || !merchantPlan.CanInstallState(
                payload.TargetItem, origin.RuntimeId, destination.RuntimeId,
                purchasePlan.Quantity, purchasePlan.Quantity, purchasePlan.UnitPrice,
                0, 0, originDecisionId))
            return Failed(DailyV1SelectedCrossCityBuyFailure.MerchantPlanPreflight, plan: purchasePlan);
        bool merchantPlanChanges = !merchantPlan.MatchesState(payload.TargetItem,
            origin.RuntimeId, destination.RuntimeId, purchasePlan.Quantity,
            purchasePlan.Quantity, purchasePlan.UnitPrice, 0, 0, originDecisionId);
        if (merchantPlanChanges && !Admitted(merchantPlanAdmission))
            return Failed(DailyV1SelectedCrossCityBuyFailure.MerchantPlanAdmission, plan: purchasePlan);

        if (actor.TravelPlan == null
            || !graph.SpatialNetwork.TryGetLocation(actor.Travel.CurrentLocationRuntimeId,
                out SpatialLocationRuntime currentLocation)
            || !graph.SpatialNetwork.TryGetSingleDirectRoute(currentLocation, targetLocation,
                out SpatialRouteRuntime route, out _))
            return Failed(DailyV1SelectedCrossCityBuyFailure.TravelPlanPreflight, plan: purchasePlan);
        float travelCost = Math.Max(1, route.TravelDays)
            * (graph.Definitions.EffectiveConfiguration?.Travel?.TravelCostPerDay ?? 1f);
        if (float.IsNaN(travelCost) || float.IsInfinity(travelCost) || travelCost < 0f
            || !actor.TravelPlan.CanCommit(targetLocation.RuntimeId, destination.RuntimeId,
                NpcTravelReason.Trade, TradeTravelUtility, travelCost, originDecisionId))
            return Failed(DailyV1SelectedCrossCityBuyFailure.TravelPlanPreflight,
                plan: purchasePlan, travelCost: travelCost);
        bool travelPlanChanges = !actor.TravelPlan.Matches(targetLocation.RuntimeId,
            destination.RuntimeId, NpcTravelReason.Trade, TradeTravelUtility,
            travelCost, originDecisionId);
        if (travelPlanChanges && !Admitted(travelPlanAdmission))
            return Failed(DailyV1SelectedCrossCityBuyFailure.TravelPlanAdmission,
                plan: purchasePlan, travelCost: travelCost);

        DailyV1SelectedOpenMarketPurchaseCommitResult purchaseCommit;
        try
        {
            purchaseCommit = purchasePlan.TryCommit(
                purchaseBoundary == null ? null : (Func<bool>)purchaseBoundary.TryBegin);
            purchaseBoundary?.Complete(purchaseCommit);
        }
        finally
        {
            purchaseBoundary?.Dispose();
        }
        if (purchaseCommit == null || !purchaseCommit.Succeeded)
            return new DailyV1SelectedCrossCityBuyResult(false,
                DailyV1SelectedCrossCityBuyFailure.PurchaseCommit, purchasePlan,
                purchaseCommit, originDecisionId, travelCost, purchaseCommit?.PartialCommit == true);

        bool merchantInstalled = merchantPlan.TryInstallState(payload.TargetItem,
            origin.RuntimeId, destination.RuntimeId, purchasePlan.Quantity,
            purchasePlan.Quantity, purchasePlan.UnitPrice, 0, 0, originDecisionId);
        if (!merchantInstalled)
            return new DailyV1SelectedCrossCityBuyResult(false,
                DailyV1SelectedCrossCityBuyFailure.PostPurchaseOwnerCommit,
                purchasePlan, purchaseCommit, originDecisionId, travelCost, partialCommit: true);
        if (merchantPlanChanges) merchantPlanCommitted?.Invoke();

        bool travelInstalled = graph.TryCommitMerchantTradeTravelPlan(actor,
            destination.RuntimeId, TradeTravelUtility, originDecisionId, () => true,
            out float committedCost)
            && committedCost.Equals(travelCost);
        if (!travelInstalled)
            return new DailyV1SelectedCrossCityBuyResult(false,
                DailyV1SelectedCrossCityBuyFailure.PostPurchaseOwnerCommit,
                purchasePlan, purchaseCommit, originDecisionId, travelCost, partialCommit: true);
        if (travelPlanChanges) travelPlanCommitted?.Invoke();

        return new DailyV1SelectedCrossCityBuyResult(true,
            DailyV1SelectedCrossCityBuyFailure.None, purchasePlan, purchaseCommit,
            originDecisionId, travelCost);
    }

    private static bool IsValidSelectedBuy(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload,
        out DailyV1CityRuntimeOwners origin,
        out DailyV1CityRuntimeOwners destination,
        out SpatialLocationRuntime targetLocation)
    {
        origin = null;
        destination = null;
        targetLocation = null;
        if (graph == null || actor == null || payload == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.ActionSlot?.CurrentAction, payload.Action)
            || !ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, payload.Action)
            || payload.Action.Values.actionType?.Value != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue
            || !DailyV1NpcDefinitionRules.HasJobType(actor.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            || DailyV1NpcDefinitionRules.HasMerchantBehavior(actor.Definition,
                graph.Definitions, DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue)
            || !DailyV1NpcDefinitionRules.HasMerchantBehavior(actor.Definition,
                graph.Definitions, DailyV1NpcDefinitionRules.TravelingMerchantBehaviorValue)
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(graph.Definitions,
                payload.Action, actor.Status?.Statuses)
            || payload.TargetNpcRuntimeId != null || payload.TargetItem == null
            || payload.Amount <= 0 || string.IsNullOrWhiteSpace(payload.TargetCityRuntimeId)
            || actor.Travel == null || actor.Travel.IsTraveling
            || string.IsNullOrWhiteSpace(actor.Travel.CurrentCityRuntimeId)
            || string.Equals(payload.TargetCityRuntimeId,
                actor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)) return false;

        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (string.Equals(city.RuntimeId, actor.Travel.CurrentCityRuntimeId,
                    StringComparison.Ordinal)) origin = city;
            if (string.Equals(city.RuntimeId, payload.TargetCityRuntimeId,
                    StringComparison.Ordinal)) destination = city;
        }
        if (origin == null || destination == null || ReferenceEquals(origin, destination)
            || !graph.ContainsExactCityOwner(origin) || !graph.ContainsExactCityOwner(destination)
            || graph.SpatialNetwork == null
            || !graph.SpatialNetwork.TryGetLocation(destination.LocationRuntimeId,
                out targetLocation)) return false;
        return true;
    }

    private static string NormalizeOptionalId(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool Admitted(Func<bool> admission)
    {
        if (admission == null) return true;
        try { return admission(); }
        catch { return false; }
    }

    private static DailyV1SelectedCrossCityBuyResult Failed(
        DailyV1SelectedCrossCityBuyFailure failure,
        DailyV1SelectedOpenMarketPurchaseFailure purchaseFailure = DailyV1SelectedOpenMarketPurchaseFailure.None,
        DailyV1SelectedOpenMarketPurchasePlan plan = null,
        float travelCost = 0f) => new DailyV1SelectedCrossCityBuyResult(
            false, failure, plan, new DailyV1SelectedOpenMarketPurchaseCommitResult(
                false, purchaseFailure), travelCost: travelCost);
}
