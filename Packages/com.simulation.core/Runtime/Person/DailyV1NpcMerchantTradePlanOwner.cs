using System;

/// <summary>
/// Core authority for the selected NPC merchant plan. It retains admitted
/// item identity and stable City ids; Unity's MerchantTradePlanRuntime is only
/// the compatibility projection used by existing gameplay and P12 adapters.
/// </summary>
internal sealed class DailyV1NpcMerchantTradePlanOwner
{
    internal DailyV1EffectiveDefinitions Definitions { get; }
    internal DailyV1ItemDefinition ItemDefinition { get; private set; }
    internal string OriginCityRuntimeId { get; private set; }
    internal string TargetCityRuntimeId { get; private set; }
    internal int PlannedAmount { get; private set; }
    internal int RawRemainingAmount { get; private set; }
    internal float PurchasePricePerItem { get; private set; }
    internal int WaitDaysAtDestination { get; private set; }
    internal int PendingTravelDays { get; private set; }
    internal string OriginDecisionId { get; private set; }
    internal long Revision { get; private set; }
    internal int RemainingAmount => RawRemainingAmount > 0 ? RawRemainingAmount : PlannedAmount;
    internal bool HasData => ItemDefinition != null || OriginCityRuntimeId != null
        || TargetCityRuntimeId != null || PlannedAmount > 0 || RawRemainingAmount > 0;
    internal bool IsActive => ItemDefinition != null && TargetCityRuntimeId != null
        && RemainingAmount > 0;

    private DailyV1NpcMerchantTradePlanOwner(DailyV1EffectiveDefinitions definitions)
    {
        Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    internal static bool TryCreateInitial(
        DailyV1EffectiveDefinitions definitions,
        out DailyV1NpcMerchantTradePlanOwner owner)
    {
        owner = null;
        if (definitions == null) return false;
        owner = new DailyV1NpcMerchantTradePlanOwner(definitions);
        return true;
    }

    internal static bool TryCreateFromOwnerSnapshot(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ItemDefinition itemDefinition,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        int rawRemainingAmount,
        float purchasePricePerItem,
        int waitDaysAtDestination,
        int pendingTravelDays,
        string originDecisionId,
        long exactRevision,
        out DailyV1NpcMerchantTradePlanOwner owner)
    {
        owner = null;
        if (!IsValidState(definitions, itemDefinition, originCityRuntimeId,
                targetCityRuntimeId, plannedAmount, rawRemainingAmount,
                purchasePricePerItem, waitDaysAtDestination, pendingTravelDays,
                exactRevision)) return false;

        owner = new DailyV1NpcMerchantTradePlanOwner(definitions);
        owner.InstallFields(itemDefinition, originCityRuntimeId, targetCityRuntimeId,
            plannedAmount, rawRemainingAmount, purchasePricePerItem,
            waitDaysAtDestination, pendingTravelDays, originDecisionId, exactRevision);
        return true;
    }

    internal bool MatchesState(
        DailyV1ItemDefinition itemDefinition,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        int rawRemainingAmount,
        float purchasePricePerItem,
        int waitDaysAtDestination,
        int pendingTravelDays,
        string originDecisionId)
    {
        return ReferenceEquals(ItemDefinition, itemDefinition)
            && string.Equals(OriginCityRuntimeId, originCityRuntimeId, StringComparison.Ordinal)
            && string.Equals(TargetCityRuntimeId, targetCityRuntimeId, StringComparison.Ordinal)
            && PlannedAmount == plannedAmount
            && RawRemainingAmount == rawRemainingAmount
            && PurchasePricePerItem.Equals(purchasePricePerItem)
            && WaitDaysAtDestination == waitDaysAtDestination
            && PendingTravelDays == pendingTravelDays
            && string.Equals(OriginDecisionId, originDecisionId, StringComparison.Ordinal);
    }

    internal bool CanInstallState(
        DailyV1ItemDefinition itemDefinition,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        int rawRemainingAmount,
        float purchasePricePerItem,
        int waitDaysAtDestination,
        int pendingTravelDays,
        string originDecisionId)
    {
        if (!IsValidState(Definitions, itemDefinition, originCityRuntimeId,
                targetCityRuntimeId, plannedAmount, rawRemainingAmount,
                purchasePricePerItem, waitDaysAtDestination, pendingTravelDays,
                Revision)) return false;
        return MatchesState(itemDefinition, originCityRuntimeId, targetCityRuntimeId,
                plannedAmount, rawRemainingAmount, purchasePricePerItem,
                waitDaysAtDestination, pendingTravelDays, originDecisionId)
            || Revision < long.MaxValue;
    }

    /// <summary>Installs one already-prepared state and advances its owner revision once.</summary>
    internal bool TryInstallState(
        DailyV1ItemDefinition itemDefinition,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        int rawRemainingAmount,
        float purchasePricePerItem,
        int waitDaysAtDestination,
        int pendingTravelDays,
        string originDecisionId)
    {
        if (!CanInstallState(itemDefinition, originCityRuntimeId, targetCityRuntimeId,
                plannedAmount, rawRemainingAmount, purchasePricePerItem,
                waitDaysAtDestination, pendingTravelDays, originDecisionId)) return false;
        if (MatchesState(itemDefinition, originCityRuntimeId, targetCityRuntimeId,
                plannedAmount, rawRemainingAmount, purchasePricePerItem,
                waitDaysAtDestination, pendingTravelDays, originDecisionId)) return true;

        InstallFields(itemDefinition, originCityRuntimeId, targetCityRuntimeId,
            plannedAmount, rawRemainingAmount, purchasePricePerItem,
            waitDaysAtDestination, pendingTravelDays, originDecisionId, Revision + 1L);
        return true;
    }

    private void InstallFields(
        DailyV1ItemDefinition itemDefinition,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        int rawRemainingAmount,
        float purchasePricePerItem,
        int waitDaysAtDestination,
        int pendingTravelDays,
        string originDecisionId,
        long revision)
    {
        ItemDefinition = itemDefinition;
        OriginCityRuntimeId = originCityRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        PlannedAmount = plannedAmount;
        RawRemainingAmount = rawRemainingAmount;
        PurchasePricePerItem = purchasePricePerItem;
        WaitDaysAtDestination = waitDaysAtDestination;
        PendingTravelDays = pendingTravelDays;
        OriginDecisionId = originDecisionId;
        Revision = revision;
    }

    private static bool IsValidState(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ItemDefinition itemDefinition,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        int rawRemainingAmount,
        float purchasePricePerItem,
        int waitDaysAtDestination,
        int pendingTravelDays,
        long revision)
    {
        return definitions != null
            && (itemDefinition == null
                || (definitions.Items.TryGetValue(itemDefinition.DefinitionId,
                        out DailyV1ItemDefinition admittedItem)
                    && ReferenceEquals(admittedItem, itemDefinition)))
            && (originCityRuntimeId == null || !string.IsNullOrWhiteSpace(originCityRuntimeId))
            && (targetCityRuntimeId == null || !string.IsNullOrWhiteSpace(targetCityRuntimeId))
            && revision >= 0L
            && plannedAmount >= 0
            && rawRemainingAmount >= 0
            && waitDaysAtDestination >= 0
            && pendingTravelDays >= 0
            && !float.IsNaN(purchasePricePerItem)
            && !float.IsInfinity(purchasePricePerItem)
            && purchasePricePerItem >= 0f
            && (itemDefinition != null || (plannedAmount == 0 && rawRemainingAmount == 0
                && originCityRuntimeId == null && targetCityRuntimeId == null));
    }
}
