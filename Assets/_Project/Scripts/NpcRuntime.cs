using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NpcRuntime
{
	[SerializeField]private NpcData npcData;
	[SerializeField]private List<NpcStatusData> currentStatus = new List<NpcStatusData>();
	[SerializeField]private NpcActionData currentAction;
    [NonSerialized]private NpcActionRuntime currentActionRuntime;
    [SerializeField]private InventoryRuntime inventory = new InventoryRuntime();
    [SerializeField]private float money;
    [NonSerialized]private CityRuntime currentCity;
    [NonSerialized]private CityRuntime destinationCity;
    [SerializeField]private int travelDaysRemaining;
    [SerializeField]private bool travelStartedToday;
    [SerializeField]private MerchantTradePlanRuntime merchantTradePlan = new MerchantTradePlanRuntime();

    public NpcData NpcData => npcData;
    public List<NpcStatusData> CurrentStatus => currentStatus;
    public NpcActionData CurrentAction => currentAction;
    public NpcActionRuntime CurrentActionRuntime => currentActionRuntime;
    public InventoryRuntime Inventory => inventory;
    public float Money => money;
    public CityRuntime CurrentCity => currentCity;
    public CityRuntime DestinationCity => destinationCity;
    public int TravelDaysRemaining => travelDaysRemaining;
    public bool TravelStartedToday => travelStartedToday;
    public bool IsTraveling => destinationCity != null && travelDaysRemaining > 0;
    public MerchantTradePlanRuntime MerchantTradePlan => merchantTradePlan;
    public string NpcName => npcData != null ? npcData.name : "NPC desconhecido";

	public NpcRuntime(NpcData npcData)
        : this(npcData, null, 0f)
	{
	}

	public NpcRuntime(NpcData npcData, CityRuntime startingCity, float initialMoney)
	{
		this.npcData = npcData;
        money = Mathf.Max(0f, initialMoney);

        if (npcData != null && npcData.statusPadrao != null)
        {
		    currentStatus.AddRange(npcData.statusPadrao);
        }

        if (startingCity != null)
        {
            startingCity.AddImportantNpc(this);
        }
	}

    public void SetCurrentAction(NpcActionData action)
    {
        currentAction = action;
        currentActionRuntime = action != null ? new NpcActionRuntime(action) : null;
    }

    public void SetCurrentActionRuntime(NpcActionRuntime actionRuntime)
    {
        currentActionRuntime = actionRuntime;
        currentAction = actionRuntime != null ? actionRuntime.Action : null;
    }

    public void AddStatus(NpcStatusData status)
    {
        if (status == null)
        {
            return;
        }

        if (currentStatus.Contains(status) == false)
        {
            currentStatus.Add(status);
        }
    }

    public void RemoveStatus(NpcStatusData status)
    {
        currentStatus.Remove(status);
    }

    public void SetCurrentCity(CityRuntime city)
    {
        currentCity = city;
    }

    public void AddMoney(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        money += amount;
    }

    public bool TrySpendMoney(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (money < amount)
        {
            return false;
        }

        money -= amount;
        return true;
    }

    public bool StartTravel(CityRuntime destination, int travelDays)
    {
        if (destination == null || IsTraveling == true)
        {
            return false;
        }

        if (currentCity != null)
        {
            currentCity.RemoveImportantNpc(this);
        }

        destinationCity = destination;
        travelDaysRemaining = Mathf.Max(1, travelDays);
        travelStartedToday = true;
        return true;
    }

    public void ClearTravelStartedToday()
    {
        travelStartedToday = false;
    }

    public bool AdvanceTravelDay(out CityRuntime arrivedCity)
    {
        arrivedCity = null;

        if (IsTraveling == false)
        {
            return false;
        }

        travelDaysRemaining = Mathf.Max(0, travelDaysRemaining - 1);

        if (travelDaysRemaining > 0)
        {
            return false;
        }

        arrivedCity = destinationCity;
        destinationCity = null;

        if (arrivedCity != null)
        {
            arrivedCity.AddImportantNpc(this);
        }

        return true;
    }

    public void SetMerchantTradePlan(ItemData item, CityRuntime originCity, CityRuntime targetCity, int plannedAmount, float purchasePricePerItem)
    {
        merchantTradePlan.Set(item, originCity, targetCity, plannedAmount, purchasePricePerItem);
    }

    public void ClearMerchantTradePlan()
    {
        merchantTradePlan.Clear();
    }
}
