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

    public NpcActionData Action => action;
    public NpcRuntime TargetNpc => targetNpc;
    public CityRuntime TargetCity => targetCity;
    public ItemData TargetItem => targetItem;
    public int Amount => amount;
    public float ExpectedUnitPrice => expectedUnitPrice;
    public float SuccessChanceMultiplier => successChanceMultiplier;

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

    public NpcActionRuntime(NpcActionData action, CityRuntime targetCity, ItemData targetItem, int amount, float expectedUnitPrice)
    {
        this.action = action;
        this.targetCity = targetCity;
        this.targetItem = targetItem;
        this.amount = amount;
        this.expectedUnitPrice = expectedUnitPrice;
    }
}

[Serializable]
public class NpcTravelPlanRuntime
{
    [NonSerialized] private CityRuntime targetCity;
    [SerializeField] private NpcTravelReason reason;
    [SerializeField] private float utility;
    [SerializeField] private float expectedCost;

    public CityRuntime TargetCity => targetCity;
    public NpcTravelReason Reason => reason;
    public float Utility => utility;
    public float ExpectedCost => expectedCost;
    public bool IsActive => targetCity != null && reason != NpcTravelReason.None;

    public void Set(CityRuntime targetCity, NpcTravelReason reason, float utility, float expectedCost)
    {
        this.targetCity = targetCity;
        this.reason = reason;
        this.utility = Mathf.Max(0f, utility);
        this.expectedCost = Mathf.Max(0f, expectedCost);
    }

    public void Clear()
    {
        targetCity = null;
        reason = NpcTravelReason.None;
        utility = 0f;
        expectedCost = 0f;
    }
}

public enum NpcTravelReason
{
    None,
    Trade,
    Flee
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

    public ItemData Item => item;
    public CityRuntime OriginCity => originCity;
    public CityRuntime TargetCity => targetCity;
    public int PlannedAmount => plannedAmount;
    public int RemainingAmount => remainingAmount > 0 ? remainingAmount : plannedAmount;
    public float PurchasePricePerItem => purchasePricePerItem;
    public int WaitDaysAtDestination => waitDaysAtDestination;
    public int PendingTravelDays => pendingTravelDays;
    public bool HasData => item != null || originCity != null || targetCity != null || plannedAmount > 0 || remainingAmount > 0;
    public bool IsActive => item != null && targetCity != null && RemainingAmount > 0;

    public void Set(ItemData item, CityRuntime originCity, CityRuntime targetCity, int plannedAmount, float purchasePricePerItem)
    {
        this.item = item;
        this.originCity = originCity;
        this.targetCity = targetCity;
        this.plannedAmount = Mathf.Max(0, plannedAmount);
        remainingAmount = this.plannedAmount;
        this.purchasePricePerItem = Mathf.Max(0f, purchasePricePerItem);
        waitDaysAtDestination = 0;
        pendingTravelDays = 0;
    }

    public void RedirectTo(CityRuntime targetCity)
    {
        if (this.targetCity != targetCity)
        {
            waitDaysAtDestination = 0;
        }

        this.targetCity = targetCity;
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
    }
}
