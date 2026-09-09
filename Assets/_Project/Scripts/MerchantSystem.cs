using UnityEngine;

public class MerchantSystem : INpcActionProvider
{
    private const int MaxUnprofitablePlanWaitDays = 3;

    private readonly int maxMerchantTradeAmount;
    private readonly float minimumProfitPerItem;
    private readonly TravelSystem travelSystem;

    public MerchantSystem(int maxMerchantTradeAmount, float minimumProfitPerItem, TravelSystem travelSystem)
    {
        this.maxMerchantTradeAmount = Mathf.Max(1, maxMerchantTradeAmount);
        this.minimumProfitPerItem = Mathf.Max(0f, minimumProfitPerItem);
        this.travelSystem = travelSystem;
    }

    public bool HandlesAction(NpcActionData action)
    {
        if (action == null)
        {
            return false;
        }

        return action.actionType == NpcActionType.BuyGoods
            || action.actionType == NpcActionType.SellGoods;
    }

    public NpcActionRuntime CreateAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (action == null)
        {
            utility = 0f;
            return null;
        }

        if (action.actionType == NpcActionType.BuyGoods)
        {
            return CreateBuyGoodsAction(npcRuntime, action, ref utility);
        }

        if (action.actionType == NpcActionType.SellGoods)
        {
            return CreateSellGoodsAction(npcRuntime, action, ref utility);
        }

        utility = 0f;
        return null;
    }

    public NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (actionRuntime == null || actionRuntime.Action == null)
        {
            return NpcActionResult.Failed();
        }

        if (actionRuntime.Action.actionType == NpcActionType.BuyGoods)
        {
            return TryExecuteBuyGoods(npcRuntime, actionRuntime) == true ? NpcActionResult.Succeeded() : NpcActionResult.Failed();
        }

        if (actionRuntime.Action.actionType == NpcActionType.SellGoods)
        {
            return TryExecuteSellGoods(npcRuntime, actionRuntime) == true ? NpcActionResult.Succeeded() : NpcActionResult.Failed();
        }

        return NpcActionResult.Failed();
    }

    private bool TryExecuteBuyGoods(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (npcRuntime == null || npcRuntime.CurrentCity == null || actionRuntime == null || actionRuntime.TargetItem == null || actionRuntime.TargetCity == null)
        {
            return false;
        }

        bool bought = npcRuntime.CurrentCity.Market.BuyItem(npcRuntime, actionRuntime.TargetItem, actionRuntime.Amount, out int amountBought, out float unitPrice, out _);

        if (bought == false || amountBought <= 0)
        {
            return false;
        }

        npcRuntime.SetMerchantTradePlan(actionRuntime.TargetItem, npcRuntime.CurrentCity, actionRuntime.TargetCity, amountBought, unitPrice);
        SetTradeTravelPlan(npcRuntime, actionRuntime.TargetCity);
        Debug.Log($"{npcRuntime.NpcName} comprou {amountBought} {actionRuntime.TargetItem.itemName} em {npcRuntime.CurrentCity.CityName} por {unitPrice:0.##} cada");
        return true;
    }

    private bool TryExecuteSellGoods(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (npcRuntime == null || npcRuntime.CurrentCity == null || actionRuntime == null || actionRuntime.TargetItem == null)
        {
            return false;
        }

        MerchantTradePlanRuntime plan = npcRuntime.MerchantTradePlan;
        bool saleBelongsToPlan = plan.IsActive == true && plan.Item == actionRuntime.TargetItem;
        float planPurchasePrice = saleBelongsToPlan == true ? plan.PurchasePricePerItem : 0f;

        bool sold = npcRuntime.CurrentCity.Market.SellItem(npcRuntime, actionRuntime.TargetItem, actionRuntime.Amount, out int amountSold, out float unitPrice, out _, out float approximateProfit);

        if (sold == false || amountSold <= 0)
        {
            return false;
        }

        if (saleBelongsToPlan == true)
        {
            approximateProfit = (unitPrice - planPurchasePrice) * amountSold;
        }

        Debug.Log($"{npcRuntime.NpcName} vendeu {amountSold} {actionRuntime.TargetItem.itemName} em {npcRuntime.CurrentCity.CityName} por {unitPrice:0.##} cada");
        Debug.Log($"Lucro aproximado: {approximateProfit:0.##}");

        if (saleBelongsToPlan == true && plan.RegisterSale(amountSold) == true)
        {
            Debug.Log($"{npcRuntime.NpcName} concluiu o plano comercial.");
        }

        return true;
    }

    private NpcActionRuntime CreateBuyGoodsAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (IsMerchant(npcRuntime) == false || npcRuntime.CurrentCity == null)
        {
            utility = 0f;
            return null;
        }

        NormalizeTradePlan(npcRuntime);

        if (npcRuntime.MerchantTradePlan.IsActive == true)
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

        utility = Mathf.Max(utility, 30f + opportunity.Score);
        return new NpcActionRuntime(action, opportunity.TargetCity, opportunity.Item, opportunity.Amount, opportunity.BuyPrice);
    }

    private NpcActionRuntime CreateSellGoodsAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (IsMerchant(npcRuntime) == false || npcRuntime.CurrentCity == null)
        {
            utility = 0f;
            return null;
        }

        NormalizeTradePlan(npcRuntime);

        MerchantTradePlanRuntime plan = npcRuntime.MerchantTradePlan;

        if (plan.IsActive == true)
        {
            if (plan.TargetCity == npcRuntime.CurrentCity)
            {
                return CreatePlannedSellGoodsAction(npcRuntime, action, ref utility);
            }

            if (CanStartTradeTravel(npcRuntime, plan.TargetCity) == false)
            {
                plan.RedirectTo(npcRuntime.CurrentCity);
                Debug.Log($"{npcRuntime.NpcName} nao consegue viajar para cumprir o plano comercial e vai reavaliar venda local.");
                return CreatePlannedSellGoodsAction(npcRuntime, action, ref utility);
            }

            SetTradeTravelPlan(npcRuntime, plan.TargetCity);
            utility = 0f;
            return null;
        }

        MerchantTradeOpportunity localSale = FindBestLocalSale(npcRuntime);

        if (localSale == null)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(utility, 25f + localSale.Score);
        return new NpcActionRuntime(action, npcRuntime.CurrentCity, localSale.Item, localSale.Amount, localSale.SellPrice);
    }

    private NpcActionRuntime CreatePlannedSellGoodsAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        MerchantTradePlanRuntime plan = npcRuntime.MerchantTradePlan;
        int amount = GetPlannedTradeAmount(npcRuntime, plan);

        if (amount <= 0)
        {
            npcRuntime.ClearMerchantTradePlan();
            utility = 0f;
            return null;
        }

        float localPrice = npcRuntime.CurrentCity.Market.GetPrice(plan.Item);
        float profitPerItem = localPrice - plan.PurchasePricePerItem;

        if (profitPerItem >= minimumProfitPerItem)
        {
            plan.ResetWaitDaysAtDestination();
            utility = Mathf.Max(utility, 80f + profitPerItem * 5f);
            return new NpcActionRuntime(action, npcRuntime.CurrentCity, plan.Item, amount, localPrice);
        }

        if (TryRedirectPlanToConnectedDestination(npcRuntime, plan) == true)
        {
            utility = 0f;
            return null;
        }

        plan.IncrementWaitDayAtDestination();

        if (plan.WaitDaysAtDestination >= MaxUnprofitablePlanWaitDays)
        {
            utility = Mathf.Max(utility, 35f);
            return new NpcActionRuntime(action, npcRuntime.CurrentCity, plan.Item, amount, localPrice);
        }

        utility = 0f;
        return null;
    }

    private void NormalizeTradePlan(NpcRuntime npcRuntime)
    {
        MerchantTradePlanRuntime plan = npcRuntime.MerchantTradePlan;

        if (plan.HasData == true && plan.IsActive == false)
        {
            npcRuntime.ClearMerchantTradePlan();
            return;
        }

        if (plan.IsActive == true && npcRuntime.Inventory.HasItem(plan.Item, 1) == false)
        {
            npcRuntime.ClearMerchantTradePlan();
        }
    }

    private void SetTradeTravelPlan(NpcRuntime npcRuntime, CityRuntime targetCity)
    {
        if (npcRuntime == null || targetCity == null || npcRuntime.CurrentCity == null || targetCity == npcRuntime.CurrentCity || travelSystem == null)
        {
            return;
        }

        float travelCost = travelSystem.GetTravelCost(npcRuntime.CurrentCity, targetCity);

        if (travelCost < 0f)
        {
            return;
        }

        npcRuntime.SetTravelPlan(targetCity, NpcTravelReason.Trade, 70f, travelCost);
    }

    private bool CanStartTradeTravel(NpcRuntime npcRuntime, CityRuntime targetCity)
    {
        return travelSystem != null
            && travelSystem.CanStartTravel(npcRuntime, targetCity, out _, out _);
    }

    private bool TryRedirectPlanToConnectedDestination(NpcRuntime npcRuntime, MerchantTradePlanRuntime plan)
    {
        MerchantTradeOpportunity opportunity = FindBestTradeDestinationForPlan(npcRuntime, plan);

        if (opportunity == null)
        {
            return false;
        }

        plan.RedirectTo(opportunity.TargetCity);
        SetTradeTravelPlan(npcRuntime, opportunity.TargetCity);
        Debug.Log($"{npcRuntime.NpcName} reavaliou o plano comercial e mudou o destino para {opportunity.TargetCity.CityName}.");
        return true;
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
            if (localItem == null || localItem.Item == null || localItem.Amount <= 0)
            {
                continue;
            }

            float buyPrice = currentCity.Market.GetPrice(localItem.Item);
            int affordableAmount = Mathf.FloorToInt(npcRuntime.Money / buyPrice);
            int baseAmount = Mathf.Min(maxMerchantTradeAmount, localItem.Amount, affordableAmount);

            if (baseAmount <= 0)
            {
                continue;
            }

            foreach (CityConnection connection in currentCity.CityData.connections)
            {
                CityRuntime targetCity = GetConnectedCity(connection);

                if (targetCity == null || targetCity == currentCity)
                {
                    continue;
                }

                int travelDays = travelSystem != null ? travelSystem.GetTravelDays(currentCity, targetCity) : -1;

                if (travelDays <= 0)
                {
                    continue;
                }

                float travelCost = travelSystem != null ? travelSystem.GetTravelCost(travelDays) : 0f;
                float moneyAvailableForGoods = npcRuntime.Money - travelCost;

                if (moneyAvailableForGoods <= 0f)
                {
                    continue;
                }

                int amount = Mathf.Min(baseAmount, Mathf.FloorToInt(moneyAvailableForGoods / buyPrice));

                if (amount <= 0)
                {
                    continue;
                }

                float sellPrice = targetCity.Market.GetPrice(localItem.Item);
                float profitPerItem = sellPrice - buyPrice;
                float netProfit = profitPerItem * amount - travelCost;

                if (profitPerItem < minimumProfitPerItem || netProfit <= 0f)
                {
                    continue;
                }

                float score = CalculateTradeScore(netProfit, travelDays) * GetTradePreferenceMultiplier(npcRuntime, localItem.Item);

                if (bestOpportunity == null || score > bestOpportunity.Score)
                {
                    bestOpportunity = new MerchantTradeOpportunity(localItem.Item, targetCity, amount, buyPrice, sellPrice, profitPerItem, netProfit, score);
                }
            }
        }

        return bestOpportunity;
    }

    private MerchantTradeOpportunity FindBestTradeDestinationForPlan(NpcRuntime npcRuntime, MerchantTradePlanRuntime plan)
    {
        CityRuntime currentCity = npcRuntime.CurrentCity;

        if (currentCity == null || currentCity.CityData == null || currentCity.CityData.connections == null || plan == null || plan.Item == null)
        {
            return null;
        }

        int amount = GetPlannedTradeAmount(npcRuntime, plan);

        if (amount <= 0)
        {
            return null;
        }

        MerchantTradeOpportunity bestOpportunity = null;

        foreach (CityConnection connection in currentCity.CityData.connections)
        {
            CityRuntime targetCity = GetConnectedCity(connection);

            if (targetCity == null || targetCity == currentCity)
            {
                continue;
            }

            int travelDays = travelSystem != null ? travelSystem.GetTravelDays(currentCity, targetCity) : -1;

            if (travelDays <= 0)
            {
                continue;
            }

            float travelCost = travelSystem != null ? travelSystem.GetTravelCost(travelDays) : 0f;

            if (npcRuntime.Money < travelCost)
            {
                continue;
            }

            float sellPrice = targetCity.Market.GetPrice(plan.Item);
            float profitPerItem = sellPrice - plan.PurchasePricePerItem;
            float netProfit = profitPerItem * amount - travelCost;

            if (profitPerItem < minimumProfitPerItem || netProfit <= 0f)
            {
                continue;
            }

            float score = CalculateTradeScore(netProfit, travelDays) * GetTradePreferenceMultiplier(npcRuntime, plan.Item);

            if (bestOpportunity == null || score > bestOpportunity.Score)
            {
                bestOpportunity = new MerchantTradeOpportunity(plan.Item, targetCity, amount, plan.PurchasePricePerItem, sellPrice, profitPerItem, netProfit, score);
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
            if (inventoryItem == null || inventoryItem.Item == null || inventoryItem.Amount <= 0)
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
            float score = CalculateTradeScore(profitPerItem * amount, 1) * GetTradePreferenceMultiplier(npcRuntime, inventoryItem.Item);

            if (bestSale == null || score > bestSale.Score)
            {
                bestSale = new MerchantTradeOpportunity(inventoryItem.Item, npcRuntime.CurrentCity, amount, referencePrice, localPrice, profitPerItem, profitPerItem * amount, score);
            }
        }

        return bestSale;
    }

    private CityRuntime GetConnectedCity(CityConnection connection)
    {
        if (connection == null || connection.destination == null)
        {
            return null;
        }

        return travelSystem != null ? travelSystem.GetCityRuntime(connection.destination) : null;
    }

    private int GetPlannedTradeAmount(NpcRuntime npcRuntime, MerchantTradePlanRuntime plan)
    {
        if (npcRuntime == null || plan == null || plan.Item == null)
        {
            return 0;
        }

        return Mathf.Min(maxMerchantTradeAmount, plan.RemainingAmount, npcRuntime.Inventory.GetAmount(plan.Item));
    }

    private float CalculateTradeScore(float netProfit, int travelDays)
    {
        return netProfit / Mathf.Max(1, travelDays);
    }

    private float GetTradePreferenceMultiplier(NpcRuntime npcRuntime, ItemData item)
    {
        if (npcRuntime == null || npcRuntime.NpcData == null || npcRuntime.NpcData.job == null || item == null)
        {
            return 1f;
        }

        foreach (TradeItemPreference preference in npcRuntime.NpcData.job.PreferredTradeItems)
        {
            if (preference != null && preference.item == item)
            {
                return Mathf.Max(0.01f, preference.utilityMultiplier);
            }
        }

        return 1f;
    }

    private bool IsMerchant(NpcRuntime npcRuntime)
    {
        return npcRuntime != null
            && npcRuntime.NpcData != null
            && npcRuntime.NpcData.job != null
            && npcRuntime.NpcData.job.jobType == NpcJobType.Merchant;
    }

    private class MerchantTradeOpportunity
    {
        public ItemData Item { get; }
        public CityRuntime TargetCity { get; }
        public int Amount { get; }
        public float BuyPrice { get; }
        public float SellPrice { get; }
        public float ProfitPerItem { get; }
        public float NetProfit { get; }
        public float Score { get; }

        public MerchantTradeOpportunity(ItemData item, CityRuntime targetCity, int amount, float buyPrice, float sellPrice, float profitPerItem, float netProfit, float score)
        {
            Item = item;
            TargetCity = targetCity;
            Amount = amount;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            ProfitPerItem = profitPerItem;
            NetProfit = netProfit;
            Score = score;
        }
    }
}
