using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class TesteSimulacao : MonoBehaviour
{
    [SerializeField] private int daysToSimulate = 1;
    [SerializeField] private int maxMerchantTradeAmount = 5;
    [SerializeField] private float minimumProfitPerItem = 1f;
    
    [SerializeField] private List<NpcData> npcList = new List<NpcData>();
    [SerializeField] private List<NpcStatusData> npcStatusList = new List<NpcStatusData>();
    [SerializeField] private List<NpcActionData> npcActionList = new List<NpcActionData>();
    [SerializeField] private List<CityData> cityList = new List<CityData>();
    [SerializeField] private List<NpcStartingCityConfig> npcStartingCities = new List<NpcStartingCityConfig>();

    [SerializeField] private List<NpcRuntime> npcRuntimeList = new List<NpcRuntime>();
    [SerializeField] private List<CityRuntime> cityRuntimeList = new List<CityRuntime>();

    private Dictionary<CityData, CityRuntime> cityRuntimeByData = new Dictionary<CityData, CityRuntime>();
    private int currentDay;

    public void Start()
    {
        InitializeSimulation();
    }

    public void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame == true)
        {
            Simulate(Mathf.Max(1, daysToSimulate));
        }
    }

    private void InitializeSimulation()
    {
        currentDay = 0;
        cityRuntimeList.Clear();
        cityRuntimeByData.Clear();

        foreach (CityData cityData in cityList)
        {
            if (cityData == null)
            {
                continue;
            }

            CityRuntime cityRuntime = new CityRuntime(cityData);
            cityRuntimeList.Add(cityRuntime);
            cityRuntimeByData[cityData] = cityRuntime;
        }

        npcRuntimeList.Clear();

        foreach (NpcData npcData in npcList)
        {
            if (npcData == null)
            {
                continue;
            }

            NpcStartingCityConfig startingConfig = npcStartingCities.Find(x => x.npc == npcData);
            CityRuntime startingCity = null;
            float initialMoney = 0f;

            if (startingConfig != null)
            {
                startingCity = GetCityRuntime(startingConfig.startingCity);
                initialMoney = startingConfig.initialMoney;
            }
            else if (cityRuntimeList.Count > 0)
            {
                startingCity = cityRuntimeList[0];
            }

            npcRuntimeList.Add(new NpcRuntime(npcData, startingCity, initialMoney));
        }
    }

    private void Simulate(int daysToSimulate)
    {
        for (int i = 0; i < daysToSimulate; i++)
        {
            currentDay++;
            Debug.Log($"Dia {currentDay}");

            foreach (CityRuntime cityRuntime in cityRuntimeList)
            {
                cityRuntime.SimulateProductionDay();
            }

            foreach (CityRuntime cityRuntime in cityRuntimeList)
            {
                cityRuntime.SimulateConsumptionDay();
                cityRuntime.UpdateMarketPrices();
            }

            foreach (NpcRuntime npcRuntime in npcRuntimeList)
            {
                if (npcRuntime.IsTraveling == true)
                {
                    continue;
                }

                EvaluateStatus(npcRuntime);
                EvaluateAction(npcRuntime);
                ExecuteAction(npcRuntime);
            }

            AdvanceTravels();
        }
    }

    private void EvaluateStatus(NpcRuntime npcRuntime)
    {
        
    }

    private void EvaluateAction(NpcRuntime npcRuntime)
    {
        List<NpcActionData> validActions = GetAllValidActions(npcRuntime.CurrentStatus);
        Dictionary<NpcActionRuntime, float> utilities = CalculateActionUtilities(npcRuntime, validActions);
        NpcActionRuntime chosenAction = ChooseAction(utilities);
        npcRuntime.SetCurrentActionRuntime(chosenAction);
    }

    private void ExecuteAction(NpcRuntime npcRuntime)
    {
        NpcActionRuntime actionRuntime = npcRuntime.CurrentActionRuntime;
        NpcActionData action = actionRuntime != null ? actionRuntime.Action : npcRuntime.CurrentAction;

        if (action == null)
        {
            return;
        }

        bool actionSucceeded = true;

        if (IsActionType(action, NpcActionType.BuyGoods) == true)
        {
            actionSucceeded = ExecuteBuyGoods(npcRuntime, actionRuntime);
        }
        else if (IsActionType(action, NpcActionType.SellGoods) == true)
        {
            actionSucceeded = ExecuteSellGoods(npcRuntime, actionRuntime);
        }
        else if (IsActionType(action, NpcActionType.Travel) == true)
        {
            actionSucceeded = ExecuteTravel(npcRuntime, actionRuntime);
        }

        if (actionSucceeded == true)
        {
            ApplyActionStatusChanges(npcRuntime, action);
        }
    }

    private void ApplyActionStatusChanges(NpcRuntime npcRuntime, NpcActionData action)
    {
        if (action.statusToRemove != null)
        {
            foreach (NpcStatusData status in action.statusToRemove)
            {
                npcRuntime.RemoveStatus(status);
            }
        }

        if (action.statusToAdd != null)
        {
            foreach (NpcStatusData status in action.statusToAdd)
            {
                npcRuntime.AddStatus(status);
            }
        }
    }
    
    private Dictionary<NpcActionRuntime, float> CalculateActionUtilities(NpcRuntime npcRuntime, List<NpcActionData> validActions)
    {
        Dictionary<NpcActionRuntime, float> utilities = new Dictionary<NpcActionRuntime, float>();

        foreach (NpcActionData action in validActions)
        {
            float utility = CalculateBaseActionUtility(npcRuntime, action);
            NpcActionRuntime actionRuntime = CreateRuntimeAction(npcRuntime, action, ref utility);

            if (actionRuntime == null)
            {
                continue;
            }

            utility = Mathf.Max(0f, utility);

            utilities.Add(actionRuntime, utility);
        }

        return utilities;
    }

    private float CalculateBaseActionUtility(NpcRuntime npcRuntime, NpcActionData action)
    {
        float utility = action.baseUtility;

        if (npcRuntime.NpcData != null && npcRuntime.NpcData.acoesPadrao != null)
        {
            NPCDefaultAction defaultAction = npcRuntime.NpcData.acoesPadrao.Find(x => x.action == action);

            if (defaultAction != null)
            {
                utility = defaultAction.baseUtility;
            }
        }

        if (npcRuntime.NpcData != null && npcRuntime.NpcData.job != null && npcRuntime.NpcData.job.workAction == action)
        {
            utility = npcRuntime.NpcData.job.workUtility;
        }

        if (action.statusModifiers != null)
        {
            foreach (StatusWeightModifier modifier in action.statusModifiers)
            {
                if (modifier.status != null && npcRuntime.CurrentStatus.Contains(modifier.status))
                {
                    utility *= modifier.multiplier;
                }
            }
        }

        return utility;
    }

    private NpcActionRuntime CreateRuntimeAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (IsActionType(action, NpcActionType.BuyGoods) == true)
        {
            return CreateBuyGoodsAction(npcRuntime, action, ref utility);
        }

        if (IsActionType(action, NpcActionType.SellGoods) == true)
        {
            return CreateSellGoodsAction(npcRuntime, action, ref utility);
        }

        if (IsActionType(action, NpcActionType.Travel) == true)
        {
            return CreateTravelAction(npcRuntime, action, ref utility);
        }

        return new NpcActionRuntime(action);
    }

    private List<NpcActionData> GetAllValidActions(List<NpcStatusData> npcCurrentStatus)
    {
        List<NpcActionData> validActions = new List<NpcActionData>();

        foreach (NpcActionData action in npcActionList)
        {
            if (action == null)
            {
                continue;
            }

            bool hasAllRequiredStatus = action.statusNecessariosParaFazerAcao == null || action.statusNecessariosParaFazerAcao.All(requiredStatus => requiredStatus == null || npcCurrentStatus.Contains(requiredStatus));

            if (hasAllRequiredStatus == true)
            {
                validActions.Add(action);
            }
        }

        return validActions;
    }
    
    private NpcActionRuntime ChooseAction(Dictionary<NpcActionRuntime, float> utilities)
    {
        float totalWeight = 0f;

        foreach (KeyValuePair<NpcActionRuntime, float> pair in utilities)
        {
            if (pair.Value > 0f)
            {
                totalWeight += pair.Value;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);

        float currentWeight = 0f;

        foreach (KeyValuePair<NpcActionRuntime, float> pair in utilities)
        {
            if (pair.Value <= 0f)
            {
                continue;
            }

            currentWeight += pair.Value;

            if (randomValue <= currentWeight)
            {
                return pair.Key;
            }
        }

        return null;
    }

    private NpcActionRuntime CreateBuyGoodsAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (IsMerchant(npcRuntime) == false || npcRuntime.CurrentCity == null || npcRuntime.MerchantTradePlan.IsActive == true)
        {
            utility = 0f;
            return null;
        }

        MerchantTradeOpportunity opportunity = FindBestTradeOpportunity(npcRuntime);

        if (opportunity == null)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(utility, 30f + opportunity.ProfitPerItem * 5f);
        return new NpcActionRuntime(action, opportunity.TargetCity, opportunity.Item, opportunity.Amount, opportunity.BuyPrice);
    }

    private NpcActionRuntime CreateSellGoodsAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (IsMerchant(npcRuntime) == false || npcRuntime.CurrentCity == null)
        {
            utility = 0f;
            return null;
        }

        MerchantTradePlanRuntime plan = npcRuntime.MerchantTradePlan;

        if (plan.IsActive == true && plan.TargetCity == npcRuntime.CurrentCity && npcRuntime.Inventory.HasItem(plan.Item, 1) == true)
        {
            float localPrice = npcRuntime.CurrentCity.Market.GetPrice(plan.Item);

            if (localPrice > plan.PurchasePricePerItem)
            {
                int amount = Mathf.Min(plan.PlannedAmount, npcRuntime.Inventory.GetAmount(plan.Item));
                utility = Mathf.Max(utility, 80f + (localPrice - plan.PurchasePricePerItem) * 5f);
                return new NpcActionRuntime(action, npcRuntime.CurrentCity, plan.Item, amount, localPrice);
            }
        }

        MerchantTradeOpportunity localSale = FindBestLocalSale(npcRuntime);

        if (localSale == null)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(utility, 25f + localSale.ProfitPerItem * 5f);
        return new NpcActionRuntime(action, npcRuntime.CurrentCity, localSale.Item, localSale.Amount, localSale.SellPrice);
    }

    private NpcActionRuntime CreateTravelAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (IsMerchant(npcRuntime) == false || npcRuntime.CurrentCity == null)
        {
            utility = 0f;
            return null;
        }

        MerchantTradePlanRuntime plan = npcRuntime.MerchantTradePlan;

        if (plan.IsActive == false || plan.TargetCity == null || plan.TargetCity == npcRuntime.CurrentCity)
        {
            utility = 0f;
            return null;
        }

        int travelDays = GetTravelDays(npcRuntime.CurrentCity, plan.TargetCity);

        if (travelDays <= 0)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(utility, 70f);
        return new NpcActionRuntime(action, plan.TargetCity, plan.Item, plan.PlannedAmount, 0f);
    }

    private bool ExecuteBuyGoods(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (npcRuntime.CurrentCity == null || actionRuntime == null || actionRuntime.TargetItem == null)
        {
            return false;
        }

        bool bought = npcRuntime.CurrentCity.Market.BuyItem(npcRuntime, actionRuntime.TargetItem, actionRuntime.Amount, out int amountBought, out float unitPrice, out _);

        if (bought == false)
        {
            return false;
        }

        npcRuntime.SetMerchantTradePlan(actionRuntime.TargetItem, npcRuntime.CurrentCity, actionRuntime.TargetCity, amountBought, unitPrice);
        Debug.Log($"{npcRuntime.NpcName} comprou {amountBought} {actionRuntime.TargetItem.itemName} em {npcRuntime.CurrentCity.CityName} por {unitPrice:0.##} cada");
        return true;
    }

    private bool ExecuteSellGoods(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (npcRuntime.CurrentCity == null || actionRuntime == null || actionRuntime.TargetItem == null)
        {
            return false;
        }

        bool sold = npcRuntime.CurrentCity.Market.SellItem(npcRuntime, actionRuntime.TargetItem, actionRuntime.Amount, out int amountSold, out float unitPrice, out _, out float approximateProfit);

        if (sold == false)
        {
            return false;
        }

        Debug.Log($"{npcRuntime.NpcName} vendeu {amountSold} {actionRuntime.TargetItem.itemName} em {npcRuntime.CurrentCity.CityName} por {unitPrice:0.##} cada");
        Debug.Log($"Lucro aproximado: {approximateProfit:0.##}");

        if (npcRuntime.MerchantTradePlan.Item == actionRuntime.TargetItem && npcRuntime.Inventory.HasItem(actionRuntime.TargetItem, 1) == false)
        {
            npcRuntime.ClearMerchantTradePlan();
        }

        return true;
    }

    private bool ExecuteTravel(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (npcRuntime.CurrentCity == null || actionRuntime == null || actionRuntime.TargetCity == null)
        {
            return false;
        }

        int travelDays = GetTravelDays(npcRuntime.CurrentCity, actionRuntime.TargetCity);

        if (travelDays <= 0)
        {
            return false;
        }

        CityRuntime originCity = npcRuntime.CurrentCity;

        if (npcRuntime.StartTravel(actionRuntime.TargetCity, travelDays) == false)
        {
            return false;
        }

        Debug.Log($"{npcRuntime.NpcName} iniciou viagem de {originCity.CityName} para {actionRuntime.TargetCity.CityName}");
        return true;
    }

    private void AdvanceTravels()
    {
        foreach (NpcRuntime npcRuntime in npcRuntimeList)
        {
            if (npcRuntime.IsTraveling == false)
            {
                continue;
            }

            if (npcRuntime.TravelStartedToday == true)
            {
                npcRuntime.ClearTravelStartedToday();
                continue;
            }

            bool arrived = npcRuntime.AdvanceTravelDay(out CityRuntime arrivedCity);

            if (arrived == true)
            {
                Debug.Log($"{npcRuntime.NpcName} chegou em {arrivedCity.CityName}");
                continue;
            }

            string verb = npcRuntime.TravelDaysRemaining == 1 ? "Resta" : "Restam";
            string dayText = npcRuntime.TravelDaysRemaining == 1 ? "dia" : "dias";
            Debug.Log($"{npcRuntime.NpcName} esta viajando. {verb} {npcRuntime.TravelDaysRemaining} {dayText}.");
        }
    }

    private MerchantTradeOpportunity FindBestTradeOpportunity(NpcRuntime npcRuntime)
    {
        CityRuntime currentCity = npcRuntime.CurrentCity;

        if (currentCity == null || currentCity.CityData == null || currentCity.CityData.connections == null)
        {
            return null;
        }

        MerchantTradeOpportunity bestOpportunity = null;

        foreach (MarketItemRuntime localItem in currentCity.Market.Items)
        {
            if (localItem.Item == null || localItem.Amount <= 0)
            {
                continue;
            }

            float buyPrice = currentCity.Market.GetPrice(localItem.Item);
            int affordableAmount = Mathf.FloorToInt(npcRuntime.Money / buyPrice);
            int amount = Mathf.Min(maxMerchantTradeAmount, localItem.Amount, affordableAmount);

            if (amount <= 0)
            {
                continue;
            }

            foreach (CityConnection connection in currentCity.CityData.connections)
            {
                CityRuntime targetCity = GetCityRuntime(connection.destination);

                if (targetCity == null || targetCity == currentCity)
                {
                    continue;
                }

                float sellPrice = targetCity.Market.GetPrice(localItem.Item);
                float profitPerItem = sellPrice - buyPrice;

                if (profitPerItem < minimumProfitPerItem)
                {
                    continue;
                }

                float score = profitPerItem * amount;

                if (bestOpportunity == null || score > bestOpportunity.Score)
                {
                    bestOpportunity = new MerchantTradeOpportunity(localItem.Item, targetCity, amount, buyPrice, sellPrice, profitPerItem, score);
                }
            }
        }

        return bestOpportunity;
    }

    private MerchantTradeOpportunity FindBestLocalSale(NpcRuntime npcRuntime)
    {
        if (npcRuntime.CurrentCity == null)
        {
            return null;
        }

        MerchantTradeOpportunity bestSale = null;

        foreach (InventoryItemRuntime inventoryItem in npcRuntime.Inventory.Items)
        {
            if (inventoryItem.Item == null || inventoryItem.Amount <= 0)
            {
                continue;
            }

            float localPrice = npcRuntime.CurrentCity.Market.GetPrice(inventoryItem.Item);
            float referencePrice = inventoryItem.AverageUnitCost > 0f ? inventoryItem.AverageUnitCost : inventoryItem.Item.basePrice;
            float profitPerItem = localPrice - referencePrice;

            if (profitPerItem < minimumProfitPerItem)
            {
                continue;
            }

            int amount = Mathf.Min(maxMerchantTradeAmount, inventoryItem.Amount);
            float score = profitPerItem * amount;

            if (bestSale == null || score > bestSale.Score)
            {
                bestSale = new MerchantTradeOpportunity(inventoryItem.Item, npcRuntime.CurrentCity, amount, referencePrice, localPrice, profitPerItem, score);
            }
        }

        return bestSale;
    }

    private int GetTravelDays(CityRuntime originCity, CityRuntime targetCity)
    {
        if (originCity == null || targetCity == null || originCity.CityData == null || originCity.CityData.connections == null)
        {
            return -1;
        }

        foreach (CityConnection connection in originCity.CityData.connections)
        {
            if (connection.destination == targetCity.CityData)
            {
                return Mathf.Max(1, connection.travelDays);
            }
        }

        return -1;
    }

    private CityRuntime GetCityRuntime(CityData cityData)
    {
        if (cityData == null)
        {
            return null;
        }

        cityRuntimeByData.TryGetValue(cityData, out CityRuntime cityRuntime);
        return cityRuntime;
    }

    private bool IsMerchant(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null || npcRuntime.NpcData == null || npcRuntime.NpcData.job == null)
        {
            return false;
        }

        return npcRuntime.NpcData.job.jobType == NpcJobType.Merchant || string.Equals(npcRuntime.NpcData.job.jobName, "Mercador", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsActionType(NpcActionData action, NpcActionType actionType)
    {
        if (action == null)
        {
            return false;
        }

        if (action.actionType == actionType)
        {
            return true;
        }

        if (string.IsNullOrEmpty(action.actionName) == true)
        {
            return false;
        }

        if (actionType == NpcActionType.BuyGoods)
        {
            return string.Equals(action.actionName, "COMPRAR_MERCADORIA", StringComparison.OrdinalIgnoreCase);
        }

        if (actionType == NpcActionType.SellGoods)
        {
            return string.Equals(action.actionName, "VENDER_MERCADORIA", StringComparison.OrdinalIgnoreCase);
        }

        if (actionType == NpcActionType.Travel)
        {
            return string.Equals(action.actionName, "VIAJAR", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private class MerchantTradeOpportunity
    {
        public ItemData Item { get; }
        public CityRuntime TargetCity { get; }
        public int Amount { get; }
        public float BuyPrice { get; }
        public float SellPrice { get; }
        public float ProfitPerItem { get; }
        public float Score { get; }

        public MerchantTradeOpportunity(ItemData item, CityRuntime targetCity, int amount, float buyPrice, float sellPrice, float profitPerItem, float score)
        {
            Item = item;
            TargetCity = targetCity;
            Amount = amount;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            ProfitPerItem = profitPerItem;
            Score = score;
        }
    }
}

[Serializable]
public class NpcStartingCityConfig
{
    public NpcData npc;
    public CityData startingCity;
    public float initialMoney = 100f;
}
