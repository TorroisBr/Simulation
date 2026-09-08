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

    public NpcActionData Action => action;
    public NpcRuntime TargetNpc => targetNpc;
    public CityRuntime TargetCity => targetCity;
    public ItemData TargetItem => targetItem;
    public int Amount => amount;
    public float ExpectedUnitPrice => expectedUnitPrice;

    public NpcActionRuntime(NpcActionData action)
    {
        this.action = action;
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
public class MerchantTradePlanRuntime
{
    [SerializeField] private ItemData item;
    [NonSerialized] private CityRuntime originCity;
    [NonSerialized] private CityRuntime targetCity;
    [SerializeField] private int plannedAmount;
    [SerializeField] private float purchasePricePerItem;

    public ItemData Item => item;
    public CityRuntime OriginCity => originCity;
    public CityRuntime TargetCity => targetCity;
    public int PlannedAmount => plannedAmount;
    public float PurchasePricePerItem => purchasePricePerItem;
    public bool IsActive => item != null && targetCity != null && plannedAmount > 0;

    public void Set(ItemData item, CityRuntime originCity, CityRuntime targetCity, int plannedAmount, float purchasePricePerItem)
    {
        this.item = item;
        this.originCity = originCity;
        this.targetCity = targetCity;
        this.plannedAmount = Mathf.Max(0, plannedAmount);
        this.purchasePricePerItem = Mathf.Max(0f, purchasePricePerItem);
    }

    public void Clear()
    {
        item = null;
        originCity = null;
        targetCity = null;
        plannedAmount = 0;
        purchasePricePerItem = 0f;
    }
}
