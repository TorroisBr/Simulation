using System;
using UnityEngine;

[Serializable]
public class NpcActionRuntime
{
    [SerializeField] private NpcActionData action;
    [NonSerialized] private NpcRuntime targetNpc;
    [NonSerialized] private CityRuntime targetCity;
    [SerializeField] private ItemData targetItem;
    [SerializeField] private int amount;
    [SerializeField] private float expectedUnitPrice;
    [SerializeField] private float successChanceMultiplier = 1f;
    [SerializeField] private NpcTravelReason travelReason;
    [SerializeField] private float expectedNetValue;
    [SerializeField] private string originDecisionId;
    [SerializeField] private string stableOccurrenceKey;
    [NonSerialized] private CommercialDecisionEvidence commercialDecisionEvidence;
    [NonSerialized] private CommercialScoutingEvidence commercialScoutingEvidence;
    [NonSerialized] private NpcRuntime p12Owner;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;

    public NpcActionData Action => action;
    public NpcRuntime TargetNpc => targetNpc;
    public CityRuntime TargetCity => targetCity;
    public ItemData TargetItem => targetItem;
    public int Amount => amount;
    public float ExpectedUnitPrice => expectedUnitPrice;
    public float ExpectedTravelCost => expectedUnitPrice;
    public float SuccessChanceMultiplier => successChanceMultiplier;
    public NpcTravelReason TravelReason => travelReason;
    public float ExpectedNetValue => expectedNetValue;
    public string OriginDecisionId => originDecisionId;
    public string StableOccurrenceKey => stableOccurrenceKey;
    public CommercialDecisionEvidence CommercialDecisionEvidence => commercialDecisionEvidence;
    public CommercialScoutingEvidence CommercialScoutingEvidence => commercialScoutingEvidence;

    public NpcActionRuntime(NpcActionData action)
    {
        this.action = action;
    }

    public NpcActionRuntime(NpcActionData action, NpcRuntime targetNpc)
    {
        this.action = action;
        this.targetNpc = targetNpc;
    }

    public NpcActionRuntime(NpcActionData action, NpcRuntime targetNpc, int amount)
    {
        this.action = action;
        this.targetNpc = targetNpc;
        this.amount = Mathf.Max(0, amount);
    }

    public NpcActionRuntime(NpcActionData action, NpcRuntime targetNpc, ItemData targetItem, int amount, float expectedUnitPrice)
    {
        this.action = action;
        this.targetNpc = targetNpc;
        this.targetItem = targetItem;
        this.amount = Mathf.Max(0, amount);
        this.expectedUnitPrice = Mathf.Max(0f, expectedUnitPrice);
    }

    /// <summary>Creates an unpublished action payload without replaying its setters.</summary>
    internal static bool TryCreateFromOwnerSnapshot(
        NpcActionData action,
        NpcRuntime targetNpc,
        CityRuntime targetCity,
        ItemData targetItem,
        int amount,
        float expectedUnitPrice,
        float successChanceMultiplier,
        NpcTravelReason travelReason,
        float expectedNetValue,
        string originDecisionId,
        string stableOccurrenceKey,
        CommercialDecisionEvidence commercialDecisionEvidence,
        CommercialScoutingEvidence commercialScoutingEvidence,
        out NpcActionRuntime staged)
    {
        staged = null;
        if (action == null || amount < 0
            || !IsFiniteNonNegative(expectedUnitPrice)
            || !IsFiniteNonNegative(successChanceMultiplier)
            || !Enum.IsDefined(typeof(NpcTravelReason), travelReason)
            || float.IsNaN(expectedNetValue) || float.IsInfinity(expectedNetValue))
            return false;
        NpcActionRuntime candidate = new NpcActionRuntime(action);
        candidate.targetNpc = targetNpc;
        candidate.targetCity = targetCity;
        candidate.targetItem = targetItem;
        candidate.amount = amount;
        candidate.expectedUnitPrice = expectedUnitPrice;
        candidate.successChanceMultiplier = successChanceMultiplier;
        candidate.travelReason = travelReason;
        candidate.expectedNetValue = expectedNetValue;
        candidate.originDecisionId = originDecisionId;
        candidate.stableOccurrenceKey = stableOccurrenceKey;
        candidate.commercialDecisionEvidence = commercialDecisionEvidence;
        candidate.commercialScoutingEvidence = commercialScoutingEvidence;
        staged = candidate;
        return true;
    }

    private static bool IsFiniteNonNegative(float value) =>
        value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);

    public void SetSuccessChanceMultiplier(float multiplier)
    {
        float normalized = Mathf.Max(0f, multiplier);
        if (successChanceMultiplier.Equals(normalized)) return;
        if (!CanCommitP12Mutation()) return;
        successChanceMultiplier = normalized;
        NotifyP12MutationCommitted();
    }

    public void SetOriginDecisionId(string decisionId)
    {
        string normalized = string.IsNullOrWhiteSpace(decisionId) ? null : decisionId;
        if (string.Equals(originDecisionId, normalized, StringComparison.Ordinal)) return;
        if (!CanCommitP12Mutation()) return;
        originDecisionId = normalized;
        NotifyP12MutationCommitted();
    }

    /// <summary>
    /// Supplies semantic occurrence identity for domain outcomes. This is
    /// intentionally separate from the decision history id.
    /// </summary>
    public void SetStableOccurrenceKey(string occurrenceKey)
    {
        string normalized = string.IsNullOrWhiteSpace(occurrenceKey) == true
            ? null
            : occurrenceKey;
        if (string.Equals(stableOccurrenceKey, normalized, StringComparison.Ordinal)) return;
        if (!CanCommitP12Mutation()) return;
        stableOccurrenceKey = normalized;
        NotifyP12MutationCommitted();
    }

    public void SetCommercialDecisionEvidence(CommercialDecisionEvidence evidence)
    {
        if (ReferenceEquals(commercialDecisionEvidence, evidence)) return;
        if (!CanCommitP12Mutation()) return;
        commercialDecisionEvidence = evidence;
        NotifyP12MutationCommitted();
    }

    public void SetCommercialScoutingEvidence(CommercialScoutingEvidence evidence)
    {
        if (ReferenceEquals(commercialScoutingEvidence, evidence)) return;
        if (!CanCommitP12Mutation()) return;
        commercialScoutingEvidence = evidence;
        NotifyP12MutationCommitted();
    }

    internal bool IsP12Bound => p12Owner != null;

    internal bool CanBindToP12Owner(NpcRuntime owner) => owner != null
        && (p12Owner == null || ReferenceEquals(p12Owner, owner));

    internal bool TryBindP12Owner(NpcRuntime owner)
    {
        if (owner == null) return false;
        if (ReferenceEquals(p12Owner, owner)) return true;
        if (p12Owner != null) return false;
        p12Owner = owner;
        p12MutationAdmission = () => owner.CanCommitInstalledP12ActionMutation(this);
        p12MutationCommitted = () => owner.NotifyInstalledP12ActionMutationCommitted(this);
        return true;
    }

    internal bool UnbindP12Owner(NpcRuntime owner)
    {
        if (p12Owner == null) return true;
        if (!ReferenceEquals(p12Owner, owner)) return false;
        p12Owner = null;
        p12MutationAdmission = null;
        p12MutationCommitted = null;
        return true;
    }

    private bool CanCommitP12Mutation()
    {
        if (p12MutationAdmission == null) return true;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12MutationCommitted()
    {
        if (p12MutationCommitted == null) return;
        try { p12MutationCommitted(); }
        catch { }
    }

    public NpcActionRuntime(NpcActionData action, CityRuntime targetCity, ItemData targetItem, int amount, float expectedUnitPrice)
    {
        this.action = action;
        this.targetCity = targetCity;
        this.targetItem = targetItem;
        this.amount = amount;
        this.expectedUnitPrice = expectedUnitPrice;
    }

    public NpcActionRuntime(NpcActionData action, CityRuntime targetCity, ItemData targetItem, NpcTravelReason travelReason, float expectedCost, float expectedNetValue)
    {
        this.action = action;
        this.targetCity = targetCity;
        this.targetItem = targetItem;
        this.travelReason = travelReason;
        expectedUnitPrice = Mathf.Max(0f, expectedCost);
        this.expectedNetValue = expectedNetValue;
    }
}

[Serializable]
public class NpcTravelPlanRuntime
{
    [NonSerialized] private SpatialLocationRuntime targetLocation;
    [NonSerialized] private CityRuntime targetCity;
    [SerializeField] private NpcTravelReason reason;
    [SerializeField] private float utility;
    [SerializeField] private float expectedCost;
    [SerializeField] private string originDecisionId;
    [SerializeField] private long revision;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;

    public SpatialLocationRuntime TargetLocation => targetLocation;
    public CityRuntime TargetCity => targetCity;
    public NpcTravelReason Reason => reason;
    public float Utility => utility;
    public float ExpectedCost => expectedCost;
    public string OriginDecisionId => originDecisionId;
    public long Revision => revision;
    public bool IsActive => targetLocation != null && reason != NpcTravelReason.None;

    internal NpcTravelPlanState CaptureOwnerState() => new NpcTravelPlanState(
        targetLocation, targetCity, reason, utility, expectedCost, originDecisionId);

    internal bool MatchesOwnerState(NpcTravelPlanState state) => state != null
        && ReferenceEquals(targetLocation, state.TargetLocation)
        && ReferenceEquals(targetCity, state.TargetCity)
        && reason == state.Reason
        && utility.Equals(state.Utility)
        && expectedCost.Equals(state.ExpectedCost)
        && string.Equals(originDecisionId, state.OriginDecisionId, StringComparison.Ordinal);

    internal bool CanInstallOwnerState(NpcTravelPlanState state) => state != null
        && (MatchesOwnerState(state)
            || (revision < long.MaxValue && CanCommitP12Mutation()));

    internal bool InstallOwnerState(NpcTravelPlanState state)
    {
        if (state == null) return false;
        if (MatchesOwnerState(state)) return true;
        if (revision == long.MaxValue || !CanCommitP12Mutation()) return false;
        targetLocation = state.TargetLocation;
        targetCity = state.TargetCity;
        reason = state.Reason;
        utility = state.Utility;
        expectedCost = state.ExpectedCost;
        originDecisionId = state.OriginDecisionId;
        revision++;
        NotifyP12MutationCommitted();
        return true;
    }

    /// <summary>Creates an unpublished exact plan without replaying a plan mutation.</summary>
    internal static bool TryCreateFromOwnerSnapshot(
        SpatialLocationRuntime targetLocation,
        CityRuntime targetCity,
        NpcTravelReason reason,
        float utility,
        float expectedCost,
        string originDecisionId,
        long exactRevision,
        out NpcTravelPlanRuntime staged)
    {
        staged = null;
        if (exactRevision < 0L || !Enum.IsDefined(typeof(NpcTravelReason), reason)
            || float.IsNaN(utility) || float.IsInfinity(utility) || utility < 0f
            || float.IsNaN(expectedCost) || float.IsInfinity(expectedCost) || expectedCost < 0f
            || (targetCity != null && !ReferenceEquals(targetCity.Location, targetLocation))
            || (targetLocation == null && (targetCity != null || reason != NpcTravelReason.None)))
            return false;
        NpcTravelPlanRuntime candidate = new NpcTravelPlanRuntime();
        candidate.targetLocation = targetLocation;
        candidate.targetCity = targetCity;
        candidate.reason = reason;
        candidate.utility = utility;
        candidate.expectedCost = expectedCost;
        candidate.originDecisionId = originDecisionId;
        candidate.revision = exactRevision;
        staged = candidate;
        return true;
    }

    public void Set(CityRuntime targetCity, NpcTravelReason reason, float utility, float expectedCost, string originDecisionId = null)
    {
        Set(targetCity?.Location, targetCity, reason, utility, expectedCost, originDecisionId);
    }

    public void Set(
        SpatialLocationRuntime targetLocation,
        CityRuntime targetCityProjection,
        NpcTravelReason reason,
        float utility,
        float expectedCost,
        string originDecisionId = null)
    {
        if (targetLocation == null
            || (targetCityProjection != null && targetCityProjection.Location != targetLocation))
        {
            Clear();
            return;
        }

        NpcTravelPlanState next = new NpcTravelPlanState(
            targetLocation,
            targetCityProjection,
            reason,
            Mathf.Max(0f, utility),
            Mathf.Max(0f, expectedCost),
            string.IsNullOrWhiteSpace(originDecisionId) == true ? null : originDecisionId);
        InstallOwnerState(next);
    }

    public void Clear()
    {
        InstallOwnerState(new NpcTravelPlanState(
            null, null, NpcTravelReason.None, 0f, 0f, null));
    }

    internal void BindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12MutationAdmission != null || p12MutationCommitted != null)
            throw new InvalidOperationException("NpcTravelPlanRuntime is already bound to a P12 mutation boundary.");
        p12MutationAdmission = admission;
        p12MutationCommitted = committed;
    }

    internal bool UnbindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (!ReferenceEquals(p12MutationAdmission, admission)
            || !ReferenceEquals(p12MutationCommitted, committed)) return false;
        p12MutationAdmission = null;
        p12MutationCommitted = null;
        return true;
    }

    private bool CanCommitP12Mutation()
    {
        if (p12MutationAdmission == null) return true;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12MutationCommitted()
    {
        if (p12MutationCommitted == null) return;
        try { p12MutationCommitted(); }
        catch { }
    }
}

internal sealed class NpcTravelPlanState
{
    internal SpatialLocationRuntime TargetLocation { get; }
    internal CityRuntime TargetCity { get; }
    internal NpcTravelReason Reason { get; }
    internal float Utility { get; }
    internal float ExpectedCost { get; }
    internal string OriginDecisionId { get; }

    internal NpcTravelPlanState(SpatialLocationRuntime targetLocation, CityRuntime targetCity,
        NpcTravelReason reason, float utility, float expectedCost, string originDecisionId)
    {
        TargetLocation = targetLocation;
        TargetCity = targetCity;
        Reason = reason;
        Utility = utility;
        ExpectedCost = expectedCost;
        OriginDecisionId = originDecisionId;
    }
}

public enum NpcTravelReason
{
    None,
    Trade,
    Flee,
    TradeReposition,
    CommercialScout
}

public enum NpcActionResultType
{
    Success,
    Failure
}

public class NpcActionResult
{
    public NpcActionResultType ResultType { get; }
    public string Message { get; }
    public bool Success => ResultType == NpcActionResultType.Success;

    private NpcActionResult(NpcActionResultType resultType, string message)
    {
        ResultType = resultType;
        Message = message;
    }

    public static NpcActionResult Succeeded(string message = null)
    {
        return new NpcActionResult(NpcActionResultType.Success, message);
    }

    public static NpcActionResult Failed(string message = null)
    {
        return new NpcActionResult(NpcActionResultType.Failure, message);
    }
}

[Serializable]
public class MerchantTradePlanRuntime
{
    [SerializeField] private ItemData item;
    [NonSerialized] private CityRuntime originCity;
    [NonSerialized] private CityRuntime targetCity;
    [SerializeField] private int plannedAmount;
    [SerializeField] private int remainingAmount;
    [SerializeField] private float purchasePricePerItem;
    [SerializeField] private int waitDaysAtDestination;
    [SerializeField] private int pendingTravelDays;
    [SerializeField] private string originDecisionId;
    [SerializeField] private long revision;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;

    public ItemData Item => item;
    public CityRuntime OriginCity => originCity;
    public CityRuntime TargetCity => targetCity;
    public int PlannedAmount => plannedAmount;
    public int RemainingAmount => remainingAmount > 0 ? remainingAmount : plannedAmount;
    public float PurchasePricePerItem => purchasePricePerItem;
    public int WaitDaysAtDestination => waitDaysAtDestination;
    public int PendingTravelDays => pendingTravelDays;
    public string OriginDecisionId => originDecisionId;
    public long Revision => revision;
    public bool HasData => item != null || originCity != null || targetCity != null || plannedAmount > 0 || remainingAmount > 0;
    public bool IsActive => item != null && targetCity != null && RemainingAmount > 0;

    internal MerchantTradePlanState CaptureOwnerState() => new MerchantTradePlanState(
        item, originCity, targetCity, plannedAmount, remainingAmount,
        purchasePricePerItem, waitDaysAtDestination, pendingTravelDays, originDecisionId);

    internal bool MatchesOwnerState(MerchantTradePlanState state) => state != null
        && ReferenceEquals(item, state.Item)
        && ReferenceEquals(originCity, state.OriginCity)
        && ReferenceEquals(targetCity, state.TargetCity)
        && plannedAmount == state.PlannedAmount
        && remainingAmount == state.RawRemainingAmount
        && purchasePricePerItem.Equals(state.PurchasePricePerItem)
        && waitDaysAtDestination == state.WaitDaysAtDestination
        && pendingTravelDays == state.PendingTravelDays
        && string.Equals(originDecisionId, state.OriginDecisionId, StringComparison.Ordinal);

    internal bool CanInstallOwnerState(MerchantTradePlanState state) => state != null
        && (MatchesOwnerState(state)
            || (revision < long.MaxValue && CanCommitP12Mutation()));

    internal bool InstallOwnerState(MerchantTradePlanState state)
    {
        if (state == null) return false;
        if (MatchesOwnerState(state)) return true;
        if (revision == long.MaxValue || !CanCommitP12Mutation()) return false;
        item = state.Item;
        originCity = state.OriginCity;
        targetCity = state.TargetCity;
        plannedAmount = state.PlannedAmount;
        remainingAmount = state.RawRemainingAmount;
        purchasePricePerItem = state.PurchasePricePerItem;
        waitDaysAtDestination = state.WaitDaysAtDestination;
        pendingTravelDays = state.PendingTravelDays;
        originDecisionId = state.OriginDecisionId;
        revision++;
        NotifyP12MutationCommitted();
        return true;
    }

    /// <summary>Creates an unpublished exact plan without replaying trade-plan mutations.</summary>
    internal static bool TryCreateFromOwnerSnapshot(
        ItemData item,
        CityRuntime originCity,
        CityRuntime targetCity,
        int plannedAmount,
        int rawRemainingAmount,
        float purchasePricePerItem,
        int waitDaysAtDestination,
        int pendingTravelDays,
        string originDecisionId,
        long exactRevision,
        out MerchantTradePlanRuntime staged)
    {
        staged = null;
        if (exactRevision < 0L || plannedAmount < 0 || rawRemainingAmount < 0
            || waitDaysAtDestination < 0 || pendingTravelDays < 0
            || float.IsNaN(purchasePricePerItem) || float.IsInfinity(purchasePricePerItem)
            || purchasePricePerItem < 0f
            || (item == null && (plannedAmount != 0 || rawRemainingAmount != 0
                || originCity != null || targetCity != null))
            || (item != null && string.IsNullOrWhiteSpace(item.DefinitionId))
            || (targetCity != null && targetCity.Location == null))
            return false;
        MerchantTradePlanRuntime candidate = new MerchantTradePlanRuntime();
        candidate.item = item;
        candidate.originCity = originCity;
        candidate.targetCity = targetCity;
        candidate.plannedAmount = plannedAmount;
        candidate.remainingAmount = rawRemainingAmount;
        candidate.purchasePricePerItem = purchasePricePerItem;
        candidate.waitDaysAtDestination = waitDaysAtDestination;
        candidate.pendingTravelDays = pendingTravelDays;
        candidate.originDecisionId = originDecisionId;
        candidate.revision = exactRevision;
        staged = candidate;
        return true;
    }

    public void Set(ItemData item, CityRuntime originCity, CityRuntime targetCity, int plannedAmount, float purchasePricePerItem, string originDecisionId = null)
    {
        int normalizedAmount = Mathf.Max(0, plannedAmount);
        InstallOwnerState(new MerchantTradePlanState(
            item,
            originCity,
            targetCity,
            normalizedAmount,
            normalizedAmount,
            Mathf.Max(0f, purchasePricePerItem),
            0,
            0,
            string.IsNullOrWhiteSpace(originDecisionId) == true ? null : originDecisionId));
    }

    public void RedirectTo(CityRuntime targetCity, string originDecisionId = null)
    {
        int nextWaitDays = this.targetCity != targetCity ? 0 : waitDaysAtDestination;
        string nextDecisionId = string.IsNullOrWhiteSpace(originDecisionId) == false
            ? originDecisionId
            : this.originDecisionId;
        InstallOwnerState(new MerchantTradePlanState(
            item, originCity, targetCity, plannedAmount, remainingAmount,
            purchasePricePerItem, nextWaitDays, pendingTravelDays, nextDecisionId));
    }

    public void IncrementWaitDayAtDestination()
    {
        InstallOwnerState(new MerchantTradePlanState(
            item, originCity, targetCity, plannedAmount, remainingAmount,
            purchasePricePerItem, waitDaysAtDestination + 1, pendingTravelDays,
            originDecisionId));
    }

    public void ResetWaitDaysAtDestination()
    {
        InstallOwnerState(new MerchantTradePlanState(
            item, originCity, targetCity, plannedAmount, remainingAmount,
            purchasePricePerItem, 0, pendingTravelDays, originDecisionId));
    }

    public void IncrementPendingTravelDay()
    {
        InstallOwnerState(new MerchantTradePlanState(
            item, originCity, targetCity, plannedAmount, remainingAmount,
            purchasePricePerItem, waitDaysAtDestination, pendingTravelDays + 1,
            originDecisionId));
    }

    public bool RegisterSale(int amountSold)
    {
        if (amountSold <= 0)
        {
            return false;
        }

        int nextRemainingAmount = Mathf.Max(0, RemainingAmount - amountSold);
        bool completed = nextRemainingAmount <= 0;
        MerchantTradePlanState next = completed
            ? new MerchantTradePlanState(null, null, null, 0, 0, 0f, 0, 0, null)
            : new MerchantTradePlanState(item, originCity, targetCity,
                plannedAmount, nextRemainingAmount, purchasePricePerItem, 0,
                pendingTravelDays, originDecisionId);
        return InstallOwnerState(next) && completed;
    }

    public void Clear()
    {
        InstallOwnerState(new MerchantTradePlanState(null, null, null,
            0, 0, 0f, 0, 0, null));
    }

    internal void BindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12MutationAdmission != null || p12MutationCommitted != null)
            throw new InvalidOperationException("MerchantTradePlanRuntime is already bound to a P12 mutation boundary.");
        p12MutationAdmission = admission;
        p12MutationCommitted = committed;
    }

    internal bool UnbindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (!ReferenceEquals(p12MutationAdmission, admission)
            || !ReferenceEquals(p12MutationCommitted, committed)) return false;
        p12MutationAdmission = null;
        p12MutationCommitted = null;
        return true;
    }

    private bool CanCommitP12Mutation()
    {
        if (p12MutationAdmission == null) return true;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12MutationCommitted()
    {
        if (p12MutationCommitted == null) return;
        try { p12MutationCommitted(); }
        catch { }
    }
}

internal sealed class MerchantTradePlanState
{
    internal ItemData Item { get; }
    internal CityRuntime OriginCity { get; }
    internal CityRuntime TargetCity { get; }
    internal int PlannedAmount { get; }
    internal int RawRemainingAmount { get; }
    internal int RemainingAmount => RawRemainingAmount > 0 ? RawRemainingAmount : PlannedAmount;
    internal float PurchasePricePerItem { get; }
    internal int WaitDaysAtDestination { get; }
    internal int PendingTravelDays { get; }
    internal string OriginDecisionId { get; }
    internal bool HasData => Item != null || OriginCity != null || TargetCity != null
        || PlannedAmount > 0 || RawRemainingAmount > 0;
    internal bool IsActive => Item != null && TargetCity != null && RemainingAmount > 0;

    internal MerchantTradePlanState(ItemData item, CityRuntime originCity, CityRuntime targetCity,
        int plannedAmount, int rawRemainingAmount, float purchasePricePerItem,
        int waitDaysAtDestination, int pendingTravelDays, string originDecisionId)
    {
        Item = item;
        OriginCity = originCity;
        TargetCity = targetCity;
        PlannedAmount = plannedAmount;
        RawRemainingAmount = rawRemainingAmount;
        PurchasePricePerItem = purchasePricePerItem;
        WaitDaysAtDestination = waitDaysAtDestination;
        PendingTravelDays = pendingTravelDays;
        OriginDecisionId = originDecisionId;
    }
}
