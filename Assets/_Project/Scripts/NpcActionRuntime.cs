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

    public void SetSuccessChanceMultiplier(float multiplier)
    {
        successChanceMultiplier = Mathf.Max(0f, multiplier);
    }

    public void SetOriginDecisionId(string decisionId)
    {
        originDecisionId = string.IsNullOrWhiteSpace(decisionId) == true ? null : decisionId;
    }

    /// <summary>
    /// Supplies semantic occurrence identity for domain outcomes. This is
    /// intentionally separate from the decision history id.
    /// </summary>
    public void SetStableOccurrenceKey(string occurrenceKey)
    {
        stableOccurrenceKey = string.IsNullOrWhiteSpace(occurrenceKey) == true
            ? null
            : occurrenceKey;
    }

    public void SetCommercialDecisionEvidence(CommercialDecisionEvidence evidence)
    {
        commercialDecisionEvidence = evidence;
    }

    public void SetCommercialScoutingEvidence(CommercialScoutingEvidence evidence)
    {
        commercialScoutingEvidence = evidence;
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

    public SpatialLocationRuntime TargetLocation => targetLocation;
    public CityRuntime TargetCity => targetCity;
    public NpcTravelReason Reason => reason;
    public float Utility => utility;
    public float ExpectedCost => expectedCost;
    public string OriginDecisionId => originDecisionId;
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

    internal void InstallOwnerState(NpcTravelPlanState state)
    {
        targetLocation = state.TargetLocation;
        targetCity = state.TargetCity;
        reason = state.Reason;
        utility = state.Utility;
        expectedCost = state.ExpectedCost;
        originDecisionId = state.OriginDecisionId;
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

        this.targetLocation = targetLocation;
        targetCity = targetCityProjection;
        this.reason = reason;
        this.utility = Mathf.Max(0f, utility);
        this.expectedCost = Mathf.Max(0f, expectedCost);
        this.originDecisionId = string.IsNullOrWhiteSpace(originDecisionId) == true ? null : originDecisionId;
    }

    public void Clear()
    {
        targetLocation = null;
        targetCity = null;
        reason = NpcTravelReason.None;
        utility = 0f;
        expectedCost = 0f;
        originDecisionId = null;
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

    public ItemData Item => item;
    public CityRuntime OriginCity => originCity;
    public CityRuntime TargetCity => targetCity;
    public int PlannedAmount => plannedAmount;
    public int RemainingAmount => remainingAmount > 0 ? remainingAmount : plannedAmount;
    public float PurchasePricePerItem => purchasePricePerItem;
    public int WaitDaysAtDestination => waitDaysAtDestination;
    public int PendingTravelDays => pendingTravelDays;
    public string OriginDecisionId => originDecisionId;
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

    internal void InstallOwnerState(MerchantTradePlanState state)
    {
        item = state.Item;
        originCity = state.OriginCity;
        targetCity = state.TargetCity;
        plannedAmount = state.PlannedAmount;
        remainingAmount = state.RawRemainingAmount;
        purchasePricePerItem = state.PurchasePricePerItem;
        waitDaysAtDestination = state.WaitDaysAtDestination;
        pendingTravelDays = state.PendingTravelDays;
        originDecisionId = state.OriginDecisionId;
    }

    public void Set(ItemData item, CityRuntime originCity, CityRuntime targetCity, int plannedAmount, float purchasePricePerItem, string originDecisionId = null)
    {
        this.item = item;
        this.originCity = originCity;
        this.targetCity = targetCity;
        this.plannedAmount = Mathf.Max(0, plannedAmount);
        remainingAmount = this.plannedAmount;
        this.purchasePricePerItem = Mathf.Max(0f, purchasePricePerItem);
        waitDaysAtDestination = 0;
        pendingTravelDays = 0;
        this.originDecisionId = string.IsNullOrWhiteSpace(originDecisionId) == true ? null : originDecisionId;
    }

    public void RedirectTo(CityRuntime targetCity, string originDecisionId = null)
    {
        if (this.targetCity != targetCity)
        {
            waitDaysAtDestination = 0;
        }

        this.targetCity = targetCity;

        if (string.IsNullOrWhiteSpace(originDecisionId) == false)
        {
            this.originDecisionId = originDecisionId;
        }
    }

    public void IncrementWaitDayAtDestination()
    {
        waitDaysAtDestination++;
    }

    public void ResetWaitDaysAtDestination()
    {
        waitDaysAtDestination = 0;
    }

    public void IncrementPendingTravelDay()
    {
        pendingTravelDays++;
    }

    public bool RegisterSale(int amountSold)
    {
        if (amountSold <= 0)
        {
            return false;
        }

        remainingAmount = Mathf.Max(0, RemainingAmount - amountSold);
        waitDaysAtDestination = 0;

        if (remainingAmount > 0)
        {
            return false;
        }

        Clear();
        return true;
    }

    public void Clear()
    {
        item = null;
        originCity = null;
        targetCity = null;
        plannedAmount = 0;
        remainingAmount = 0;
        purchasePricePerItem = 0f;
        waitDaysAtDestination = 0;
        pendingTravelDays = 0;
        originDecisionId = null;
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
