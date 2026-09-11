using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NpcRuntime
{
	[SerializeField]private string runtimeId;
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
    [SerializeField]private int hiddenDaysRemaining;
    [SerializeField]private MerchantTradePlanRuntime merchantTradePlan = new MerchantTradePlanRuntime();
    [SerializeField]private NpcTravelPlanRuntime travelPlan = new NpcTravelPlanRuntime();
    [SerializeField]private CommercialKnowledgeRuntime commercialKnowledge = new CommercialKnowledgeRuntime();

    public string RuntimeId => runtimeId;
    public NpcData NpcData => npcData;
    public string DefinitionId => npcData != null ? npcData.DefinitionId : string.Empty;
    public List<NpcStatusData> CurrentStatus => currentStatus ?? (currentStatus = new List<NpcStatusData>());
    public NpcActionData CurrentAction => currentAction;
    public NpcActionRuntime CurrentActionRuntime => currentActionRuntime;
    public InventoryRuntime Inventory => inventory ?? (inventory = new InventoryRuntime());
    public float Money => money;
    public CityRuntime CurrentCity => currentCity;
    public CityRuntime DestinationCity => destinationCity;
    public int TravelDaysRemaining => travelDaysRemaining;
    public bool TravelStartedToday => travelStartedToday;
    public bool IsTraveling => destinationCity != null && travelDaysRemaining > 0;
    public int HiddenDaysRemaining => hiddenDaysRemaining;
    public bool IsHidden => hiddenDaysRemaining > 0;
    public MerchantTradePlanRuntime MerchantTradePlan => merchantTradePlan ?? (merchantTradePlan = new MerchantTradePlanRuntime());
    public NpcTravelPlanRuntime TravelPlan => travelPlan ?? (travelPlan = new NpcTravelPlanRuntime());
    public CommercialKnowledgeRuntime CommercialKnowledge => commercialKnowledge ?? (commercialKnowledge = new CommercialKnowledgeRuntime());
    public string NpcName => npcData != null ? npcData.name : "NPC desconhecido";

	public NpcRuntime(string runtimeId, NpcData npcData)
		: this(runtimeId, npcData, null, 0f)
	{
	}

	public NpcRuntime(string runtimeId, NpcData npcData, CityRuntime startingCity, float initialMoney)
	{
		if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("NpcRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

		this.runtimeId = runtimeId;
		this.npcData = npcData;
        money = Mathf.Max(0f, initialMoney);

        if (npcData != null && npcData.statusPadrao != null)
        {
		    CurrentStatus.AddRange(npcData.statusPadrao);
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

        if (CurrentStatus.Contains(status) == false)
        {
            CurrentStatus.Add(status);
        }
    }

    public void RemoveStatus(NpcStatusData status)
    {
        CurrentStatus.Remove(status);
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

    public void SetTravelPlan(CityRuntime targetCity, NpcTravelReason reason, float utility, float expectedCost)
    {
        TravelPlan.Set(targetCity, reason, utility, expectedCost);
    }

    public void ClearTravelPlan()
    {
        TravelPlan.Clear();
    }

    public void HideForDays(int days)
    {
        int followingFullDays = Mathf.Max(1, days);
        int durationIncludingCurrentDay = followingFullDays + 1;
        hiddenDaysRemaining = Mathf.Max(hiddenDaysRemaining, durationIncludingCurrentDay);
    }

    public bool AdvanceHiddenDay()
    {
        if (hiddenDaysRemaining <= 0)
        {
            return false;
        }

        hiddenDaysRemaining = Mathf.Max(0, hiddenDaysRemaining - 1);
        return hiddenDaysRemaining <= 0;
    }

    public void ClearHidden()
    {
        hiddenDaysRemaining = 0;
    }

    public void SetMerchantTradePlan(ItemData item, CityRuntime originCity, CityRuntime targetCity, int plannedAmount, float purchasePricePerItem)
    {
        MerchantTradePlan.Set(item, originCity, targetCity, plannedAmount, purchasePricePerItem);
    }

    public void ClearMerchantTradePlan()
    {
        MerchantTradePlan.Clear();

        if (TravelPlan.Reason == NpcTravelReason.Trade)
        {
            ClearTravelPlan();
        }
    }
}
