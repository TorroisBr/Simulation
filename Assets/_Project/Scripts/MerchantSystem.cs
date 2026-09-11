using System;
using System.Collections.Generic;
using UnityEngine;

public class MerchantSystem : INpcActionProvider
{
    private const int MaxUnprofitablePlanWaitDays = 3;
    private const float LocalMerchantWholesalePriceMultiplier = 0.70f;
    private const float LocalMerchantReserveRatio = 0.25f;

    private readonly int maxMerchantTradeAmount;
    private readonly float minimumProfitPerItem;
    private readonly bool allowTradeRepositioning;
    private readonly TravelSystem travelSystem;
    private readonly SimulationTime simulationTime;
    private readonly CommercialKnowledgePolicy knowledgePolicy;
    private readonly NpcDecisionRecorder decisionRecorder;
    private readonly SimulationLogger logger;

    public MerchantSystem(
        int maxMerchantTradeAmount,
        float minimumProfitPerItem,
        bool allowTradeRepositioning,
        TravelSystem travelSystem,
        SimulationTime simulationTime,
        CommercialKnowledgeSettings knowledgeSettings,
        NpcDecisionRecorder decisionRecorder,
        SimulationLogger logger = null)
    {
        this.maxMerchantTradeAmount = Mathf.Max(1, maxMerchantTradeAmount);
        this.minimumProfitPerItem = Mathf.Max(0f, minimumProfitPerItem);
        this.allowTradeRepositioning = allowTradeRepositioning;
        this.travelSystem = travelSystem;
        this.simulationTime = simulationTime ?? throw new System.ArgumentNullException(nameof(simulationTime));
        knowledgePolicy = new CommercialKnowledgePolicy(knowledgeSettings);
        this.decisionRecorder = decisionRecorder;
        this.logger = logger ?? new SimulationLogger(null);
    }

    public void AdvanceNpcTradeState(NpcRuntime npcRuntime)
    {
        if (IsMerchant(npcRuntime) == false || npcRuntime.CurrentCity == null || npcRuntime.IsTraveling == true)
        {
            return;
        }

        ObserveCurrentMarket(npcRuntime);
        NormalizeTradePlan(npcRuntime);
        MerchantTradePlanRuntime plan = npcRuntime.MerchantTradePlan;

        if (plan.IsActive == false)
        {
            return;
        }

        if (plan.TargetCity != npcRuntime.CurrentCity)
        {
            if (CanStartTradeTravel(npcRuntime, plan.TargetCity) == true)
            {
                SetTradeTravelPlan(npcRuntime, plan.TargetCity);
                return;
            }

            ClearTradeTravelPlan(npcRuntime);
            plan.RedirectTo(npcRuntime.CurrentCity);
            logger.Log(SimulationLogCategory.Trade, $"{npcRuntime.NpcName} nao consegue viajar para cumprir o plano comercial e vai reavaliar venda local.");
        }

        int amount = GetPlannedTradeAmount(npcRuntime, plan);

        if (amount <= 0)
        {
            npcRuntime.ClearMerchantTradePlan();
            return;
        }

        if (TryGetUsefulObservation(npcRuntime, npcRuntime.CurrentCity, plan.Item, out CommercialMarketObservation localObservation, out _) == false)
        {
            plan.IncrementWaitDayAtDestination();
            return;
        }

        float localPrice = localObservation.ObservedPrice;
        float profitPerItem = localPrice - plan.PurchasePricePerItem;
        MerchantTradeOpportunity localBuyer = FindBestLocalMerchantBuyer(npcRuntime, plan.Item, amount, plan.PurchasePricePerItem);

        if (localBuyer != null || profitPerItem >= minimumProfitPerItem)
        {
            plan.ResetWaitDaysAtDestination();
            return;
        }

        MerchantTradeOpportunity redirectOpportunity = FindBestTradeDestinationForPlan(npcRuntime, plan);

        if (redirectOpportunity != null)
        {
            NpcDecisionRecord redirectDecision = decisionRecorder?.Record(
                npcRuntime.RuntimeId,
                NpcDecisionType.TradeRedirect,
                NpcDecisionOrigin.Autonomous,
                null,
                null,
                redirectOpportunity.TargetCity?.Location?.RuntimeId,
                redirectOpportunity.Evidence);
            plan.RedirectTo(redirectOpportunity.TargetCity, redirectDecision?.DecisionId);
            SetTradeTravelPlan(npcRuntime, redirectOpportunity.TargetCity);
            logger.Log(SimulationLogCategory.Trade, $"{npcRuntime.NpcName} reavaliou o plano comercial e mudou o destino para {redirectOpportunity.TargetCity.CityName}.");
            return;
        }

        plan.IncrementWaitDayAtDestination();
    }

    public void BootstrapInitialKnowledge(NpcRuntime npcRuntime)
    {
        if (simulationTime.AbsoluteDay != 0L
            || IsTravelingMerchant(npcRuntime) == false
            || npcRuntime.CurrentCity == null)
        {
            return;
        }

        if (npcRuntime.SpatialKnowledge.KnowsLocation(npcRuntime.CurrentCity.Location?.RuntimeId) == true)
        {
            ObserveMarket(npcRuntime, npcRuntime.CurrentCity, CommercialKnowledgeSource.InitialScenarioKnowledge);
        }

        if (travelSystem == null)
        {
            return;
        }

        foreach (CityRuntime connectedCity in travelSystem.GetKnownDirectDestinationCities(npcRuntime, npcRuntime.CurrentCity))
        {
            ObserveMarket(npcRuntime, connectedCity, CommercialKnowledgeSource.InitialScenarioKnowledge);
        }
    }

    public void ObserveCurrentMarket(NpcRuntime npcRuntime)
    {
        if (IsMerchant(npcRuntime) == false || npcRuntime.CurrentCity == null || npcRuntime.IsTraveling == true)
        {
            return;
        }

        npcRuntime.SpatialKnowledge.DiscoverLocation(npcRuntime.CurrentCity.Location?.RuntimeId);
        ObserveMarket(npcRuntime, npcRuntime.CurrentCity, CommercialKnowledgeSource.DirectObservation);
    }

    private void ObserveMarket(
        NpcRuntime npcRuntime,
        CityRuntime cityRuntime,
        CommercialKnowledgeSource source)
    {
        if (npcRuntime == null || cityRuntime == null || cityRuntime.Location == null)
        {
            return;
        }

        foreach (MarketItemRuntime marketItem in cityRuntime.Market.Items)
        {
            if (marketItem == null
                || marketItem.Item == null
                || string.IsNullOrWhiteSpace(marketItem.Item.DefinitionId) == true)
            {
                continue;
            }

            CommercialMarketObservation observation = new CommercialMarketObservation(
                cityRuntime.Location.RuntimeId,
                marketItem.Item,
                marketItem.CurrentPrice,
                marketItem.Amount,
                simulationTime.AbsoluteDay,
                simulationTime.AbsoluteDay,
                source);
            npcRuntime.CommercialKnowledge.RecordObservation(observation);
        }
    }

    public NpcActionRuntime CreateMerchantTravelAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (action == null
            || IsTravelingMerchant(npcRuntime) == false
            || npcRuntime.CurrentCity == null
            || npcRuntime.IsTraveling == true
            || npcRuntime.MerchantTradePlan.IsActive == true
            || npcRuntime.TravelPlan.IsActive == true)
        {
            utility = 0f;
            return null;
        }

        if (FindBestTradeOpportunityFrom(npcRuntime, npcRuntime.CurrentCity, 0f, 0) != null
            || FindBestLocalSale(npcRuntime) != null)
        {
            utility = 0f;
            return null;
        }

        if (allowTradeRepositioning == true)
        {
            MerchantTradeRepositionOpportunity reposition = FindBestTradeRepositionOpportunity(npcRuntime);

            if (reposition != null)
            {
                utility = Mathf.Max(0f, reposition.TradeOpportunity.Score);
                NpcActionRuntime repositionAction = new NpcActionRuntime(
                    action,
                    reposition.OriginCity,
                    reposition.TradeOpportunity.Item,
                    NpcTravelReason.TradeReposition,
                    reposition.RepositionCost,
                    reposition.TradeOpportunity.NetProfit);
                repositionAction.SetCommercialDecisionEvidence(reposition.TradeOpportunity.Evidence);
                return repositionAction;
            }
        }

        MerchantCommercialScoutingOpportunity scouting = FindBestCommercialScoutingOpportunity(npcRuntime);

        if (scouting == null)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(utility, 20f + scouting.Score);
        NpcActionRuntime scoutingAction = new NpcActionRuntime(
            action,
            scouting.TargetCity,
            null,
            NpcTravelReason.CommercialScout,
            scouting.TravelCost,
            0f);
        scoutingAction.SetCommercialScoutingEvidence(scouting.Evidence);
        return scoutingAction;
    }

    public void LogTradeRepositionDecision(NpcRuntime npcRuntime, CityRuntime originCity, NpcActionRuntime actionRuntime)
    {
        if (npcRuntime == null || originCity == null || actionRuntime == null || actionRuntime.TargetCity == null)
        {
            return;
        }

        string opportunity = actionRuntime.TargetItem != null ? actionRuntime.TargetItem.itemName : "desconhecida";
        logger.Log(
            SimulationLogCategory.Trade,
            $"{npcRuntime.NpcName} decidiu se reposicionar comercialmente de {originCity.CityName} para {actionRuntime.TargetCity.CityName}. Custo: {actionRuntime.ExpectedTravelCost:0.##}; oportunidade esperada: {opportunity}; lucro liquido esperado do ciclo: {actionRuntime.ExpectedNetValue:0.##}.");
    }

    public void LogCommercialScoutingDecision(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        CommercialScoutingEvidence evidence = actionRuntime?.CommercialScoutingEvidence;

        if (npcRuntime == null || actionRuntime?.TargetCity == null || evidence == null)
        {
            return;
        }

        logger.Log(
            SimulationLogCategory.Trade,
            $"{npcRuntime.NpcName} iniciou pesquisa comercial em {actionRuntime.TargetCity.CityName}. Informacoes ausentes: {evidence.UnknownObservationCount}; desatualizadas: {evidence.StaleObservationCount}.");
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

        if (IsTravelingMerchant(npcRuntime) == true && actionRuntime.TargetCity != npcRuntime.CurrentCity)
        {
            npcRuntime.SetMerchantTradePlan(
                actionRuntime.TargetItem,
                npcRuntime.CurrentCity,
                actionRuntime.TargetCity,
                amountBought,
                unitPrice,
                actionRuntime.OriginDecisionId);
            SetTradeTravelPlan(npcRuntime, actionRuntime.TargetCity);
            logger.Log(SimulationLogCategory.Trade, $"{npcRuntime.NpcName} comprou {amountBought} {actionRuntime.TargetItem.itemName} em {npcRuntime.CurrentCity.CityName} por {unitPrice:0.##} cada para vender em {actionRuntime.TargetCity.CityName}");
            return true;
        }

        logger.Log(SimulationLogCategory.Trade, $"{npcRuntime.NpcName} comprou {amountBought} {actionRuntime.TargetItem.itemName} em {npcRuntime.CurrentCity.CityName} por {unitPrice:0.##} cada para estoque local");
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
        float referencePrice = saleBelongsToPlan == true ? planPurchasePrice : npcRuntime.Inventory.GetAverageUnitCost(actionRuntime.TargetItem);

        if (actionRuntime.TargetNpc != null && TryExecuteSellGoodsToNpc(npcRuntime, actionRuntime, referencePrice, out int amountSoldToNpc) == true)
        {
            if (saleBelongsToPlan == true && plan.RegisterSale(amountSoldToNpc) == true)
            {
                logger.Log(SimulationLogCategory.Trade, $"{npcRuntime.NpcName} concluiu o plano comercial.");
            }

            return true;
        }

        return TryExecuteSellGoodsToMarket(npcRuntime, actionRuntime, saleBelongsToPlan, plan, planPurchasePrice);
    }

    private bool TryExecuteSellGoodsToNpc(NpcRuntime sellerRuntime, NpcActionRuntime actionRuntime, float referencePrice, out int amountSold)
    {
        amountSold = 0;

        if (sellerRuntime == null || sellerRuntime.CurrentCity == null || actionRuntime == null || actionRuntime.TargetNpc == null || actionRuntime.TargetItem == null)
        {
            return false;
        }

        NpcRuntime buyerRuntime = actionRuntime.TargetNpc;

        if (buyerRuntime == sellerRuntime || IsLocalMerchant(buyerRuntime) == false || buyerRuntime.CurrentCity != sellerRuntime.CurrentCity || buyerRuntime.IsTraveling == true)
        {
            return false;
        }

        float currentRetailPrice = sellerRuntime.CurrentCity.Market.GetPrice(actionRuntime.TargetItem);
        float unitPrice = CalculateLocalMerchantUnitPrice(buyerRuntime, actionRuntime.TargetItem, currentRetailPrice);

        if (unitPrice - referencePrice < minimumProfitPerItem)
        {
            return false;
        }

        float reserveAmount = buyerRuntime.Money * LocalMerchantReserveRatio;
        float spendableMoney = Mathf.Max(0f, buyerRuntime.Money - reserveAmount);
        int affordableAmount = Mathf.FloorToInt(spendableMoney / unitPrice);
        amountSold = Mathf.Min(actionRuntime.Amount, sellerRuntime.Inventory.GetAmount(actionRuntime.TargetItem), affordableAmount);

        if (amountSold <= 0)
        {
            return false;
        }

        float totalPrice = unitPrice * amountSold;

        if (buyerRuntime.TrySpendMoney(totalPrice) == false)
        {
            return false;
        }

        if (sellerRuntime.Inventory.RemoveItem(actionRuntime.TargetItem, amountSold) == false)
        {
            buyerRuntime.AddMoney(totalPrice);
            amountSold = 0;
            return false;
        }

        sellerRuntime.AddMoney(totalPrice);
        buyerRuntime.Inventory.AddItem(actionRuntime.TargetItem, amountSold, unitPrice);
        float approximateProfit = (unitPrice - referencePrice) * amountSold;
        logger.Log(SimulationLogCategory.Trade, $"{sellerRuntime.NpcName} vendeu {amountSold} {actionRuntime.TargetItem.itemName} para {buyerRuntime.NpcName} em {sellerRuntime.CurrentCity.CityName} por {unitPrice:0.##} cada");
        logger.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {approximateProfit:0.##}");
        return true;
    }

    private bool TryExecuteSellGoodsToMarket(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, bool saleBelongsToPlan, MerchantTradePlanRuntime plan, float planPurchasePrice)
    {
        bool sold = npcRuntime.CurrentCity.Market.SellItem(npcRuntime, actionRuntime.TargetItem, actionRuntime.Amount, out int amountSold, out float unitPrice, out _, out float approximateProfit);

        if (sold == false || amountSold <= 0)
        {
            return false;
        }

        if (saleBelongsToPlan == true)
        {
            approximateProfit = (unitPrice - planPurchasePrice) * amountSold;
        }

        logger.Log(SimulationLogCategory.Trade, $"{npcRuntime.NpcName} vendeu {amountSold} {actionRuntime.TargetItem.itemName} ao mercado de {npcRuntime.CurrentCity.CityName} por {unitPrice:0.##} cada");
        logger.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {approximateProfit:0.##}");

        if (saleBelongsToPlan == true && plan.RegisterSale(amountSold) == true)
        {
            logger.Log(SimulationLogCategory.Trade, $"{npcRuntime.NpcName} concluiu o plano comercial.");
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

        if (npcRuntime.MerchantTradePlan.IsActive == true)
        {
            utility = 0f;
            return null;
        }

        if (IsLocalMerchant(npcRuntime) == true)
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

        float baseUtility = 30f;
        utility = Mathf.Max(utility, baseUtility + opportunity.Score);
        NpcActionRuntime buyAction = new NpcActionRuntime(action, opportunity.TargetCity, opportunity.Item, opportunity.Amount, opportunity.BuyPrice);
        buyAction.SetCommercialDecisionEvidence(opportunity.Evidence);
        return buyAction;
    }

    private NpcActionRuntime CreateSellGoodsAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (IsMerchant(npcRuntime) == false || npcRuntime.CurrentCity == null)
        {
            utility = 0f;
            return null;
        }

        MerchantTradePlanRuntime plan = npcRuntime.MerchantTradePlan;

        if (plan.IsActive == true)
        {
            if (plan.TargetCity == npcRuntime.CurrentCity)
            {
                return CreatePlannedSellGoodsAction(npcRuntime, action, ref utility);
            }

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
        NpcActionRuntime sellAction = new NpcActionRuntime(action, npcRuntime.CurrentCity, localSale.Item, localSale.Amount, localSale.SellPrice);
        sellAction.SetCommercialDecisionEvidence(localSale.Evidence);
        return sellAction;
    }

    private NpcActionRuntime CreatePlannedSellGoodsAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        MerchantTradePlanRuntime plan = npcRuntime.MerchantTradePlan;
        int amount = GetPlannedTradeAmount(npcRuntime, plan);

        if (amount <= 0)
        {
            utility = 0f;
            return null;
        }

        if (TryGetUsefulObservation(npcRuntime, npcRuntime.CurrentCity, plan.Item, out CommercialMarketObservation localObservation, out float localFreshness) == false)
        {
            utility = 0f;
            return null;
        }

        float localPrice = localObservation.ObservedPrice;
        float profitPerItem = localPrice - plan.PurchasePricePerItem;
        MerchantTradeOpportunity localBuyer = FindBestLocalMerchantBuyer(npcRuntime, plan.Item, amount, plan.PurchasePricePerItem);

        if (localBuyer != null)
        {
            utility = Mathf.Max(utility, 85f + localBuyer.Score);
            NpcActionRuntime buyerSaleAction = new NpcActionRuntime(action, localBuyer.TargetNpc, plan.Item, localBuyer.Amount, localBuyer.SellPrice);
            buyerSaleAction.SetCommercialDecisionEvidence(localBuyer.Evidence);
            return buyerSaleAction;
        }

        float expectedGrossProfit = profitPerItem * amount;
        CommercialDecisionEvidence saleEvidence = CreateCommercialEvidence(
            npcRuntime,
            plan.OriginCity ?? npcRuntime.CurrentCity,
            npcRuntime.CurrentCity,
            null,
            plan.Item,
            null,
            0f,
            localObservation,
            localFreshness,
            amount,
            plan.PurchasePricePerItem,
            localPrice,
            0f,
            expectedGrossProfit,
            expectedGrossProfit,
            profitPerItem >= minimumProfitPerItem ? profitPerItem * 5f : 0f);

        if (profitPerItem >= minimumProfitPerItem)
        {
            utility = Mathf.Max(utility, 80f + profitPerItem * 5f);
            NpcActionRuntime plannedSaleAction = new NpcActionRuntime(action, npcRuntime.CurrentCity, plan.Item, amount, localPrice);
            plannedSaleAction.SetCommercialDecisionEvidence(saleEvidence);
            return plannedSaleAction;
        }

        if (plan.WaitDaysAtDestination >= MaxUnprofitablePlanWaitDays)
        {
            utility = Mathf.Max(utility, 35f);
            NpcActionRuntime liquidationAction = new NpcActionRuntime(action, npcRuntime.CurrentCity, plan.Item, amount, localPrice);
            liquidationAction.SetCommercialDecisionEvidence(saleEvidence);
            return liquidationAction;
        }

        utility = 0f;
        return null;
    }

    private void NormalizeTradePlan(NpcRuntime npcRuntime)
    {
        MerchantTradePlanRuntime plan = npcRuntime.MerchantTradePlan;

        if (IsLocalMerchant(npcRuntime) == true && plan.HasData == true)
        {
            npcRuntime.ClearMerchantTradePlan();
            return;
        }

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
        if (npcRuntime == null || IsTravelingMerchant(npcRuntime) == false || targetCity == null || npcRuntime.CurrentCity == null || targetCity == npcRuntime.CurrentCity || travelSystem == null)
        {
            return;
        }

        float travelCost = travelSystem.GetTravelCost(npcRuntime.CurrentCity, targetCity);

        if (travelCost < 0f)
        {
            return;
        }

        npcRuntime.SetTravelPlan(targetCity, NpcTravelReason.Trade, 70f, travelCost, npcRuntime.MerchantTradePlan.OriginDecisionId);
    }

    private void ClearTradeTravelPlan(NpcRuntime npcRuntime)
    {
        if (npcRuntime != null && npcRuntime.TravelPlan.Reason == NpcTravelReason.Trade)
        {
            npcRuntime.ClearTravelPlan();
        }
    }

    private bool CanStartTradeTravel(NpcRuntime npcRuntime, CityRuntime targetCity)
    {
        return travelSystem != null
            && travelSystem.CanPlanKnownTravel(npcRuntime, targetCity, out _, out _);
    }

    private MerchantTradeOpportunity FindBestTradeOpportunity(NpcRuntime npcRuntime)
    {
        return FindBestTradeOpportunityFrom(npcRuntime, npcRuntime.CurrentCity, 0f, 0);
    }

    private MerchantTradeOpportunity FindBestTradeOpportunityFrom(NpcRuntime npcRuntime, CityRuntime originCity, float additionalCost, int additionalTravelDays)
    {
        if (npcRuntime == null || travelSystem == null || originCity == null || originCity.Location == null)
        {
            return null;
        }

        MerchantTradeOpportunity bestOpportunity = null;
        List<CityRuntime> destinationCities = travelSystem.GetKnownDirectDestinationCities(npcRuntime, originCity);

        foreach (CommercialMarketObservation originObservation in npcRuntime.CommercialKnowledge.Observations)
        {
            if (originObservation == null
                || originObservation.LocationRuntimeId != originCity.Location.RuntimeId
                || originObservation.ItemDefinition == null
                || originObservation.ObservedStock <= 0)
            {
                continue;
            }

            float originFreshness = knowledgePolicy.GetFreshness(originObservation, simulationTime.AbsoluteDay);
            float buyPrice = originObservation.ObservedPrice;

            if (originFreshness <= 0f || buyPrice <= 0f)
            {
                continue;
            }

            foreach (CityRuntime targetCity in destinationCities)
            {
                if (targetCity == null || targetCity == originCity)
                {
                    continue;
                }

                int travelDays = travelSystem.GetTravelDays(originCity, targetCity);

                if (travelDays <= 0)
                {
                    continue;
                }

                float travelCost = travelSystem.GetTravelCost(travelDays);
                float totalTravelCost = additionalCost + travelCost;
                float moneyAvailableForGoods = npcRuntime.Money - totalTravelCost;

                if (moneyAvailableForGoods <= 0f)
                {
                    continue;
                }

                int amount = Mathf.Min(maxMerchantTradeAmount, originObservation.ObservedStock, Mathf.FloorToInt(moneyAvailableForGoods / buyPrice));

                if (amount <= 0)
                {
                    continue;
                }

                if (TryGetUsefulObservation(npcRuntime, targetCity, originObservation.ItemDefinition, out CommercialMarketObservation targetObservation, out float targetFreshness) == false)
                {
                    continue;
                }

                float sellPrice = targetObservation.ObservedPrice;
                float profitPerItem = sellPrice - buyPrice;
                float netProfit = profitPerItem * amount - totalTravelCost;

                if (profitPerItem < minimumProfitPerItem || netProfit <= 0f)
                {
                    continue;
                }

                int totalTravelDays = additionalTravelDays + travelDays;
                float freshness = Mathf.Min(originFreshness, targetFreshness);
                float score = CalculateTradeScore(netProfit, totalTravelDays)
                    * freshness
                    * GetTradePreferenceMultiplier(npcRuntime, originObservation.ItemDefinition);

                if (bestOpportunity == null || score > bestOpportunity.Score)
                {
                    CommercialDecisionEvidence evidence = CreateCommercialEvidence(
                        npcRuntime,
                        originCity,
                        targetCity,
                        null,
                        originObservation.ItemDefinition,
                        originObservation,
                        originFreshness,
                        targetObservation,
                        targetFreshness,
                        amount,
                        buyPrice,
                        sellPrice,
                        totalTravelCost,
                        profitPerItem * amount,
                        netProfit,
                        score);
                    bestOpportunity = new MerchantTradeOpportunity(originObservation.ItemDefinition, targetCity, amount, buyPrice, sellPrice, profitPerItem, netProfit, score, evidence);
                }
            }
        }

        return bestOpportunity;
    }

    private MerchantTradeRepositionOpportunity FindBestTradeRepositionOpportunity(NpcRuntime npcRuntime)
    {
        CityRuntime currentCity = npcRuntime != null ? npcRuntime.CurrentCity : null;

        if (currentCity == null || travelSystem == null)
        {
            return null;
        }

        MerchantTradeRepositionOpportunity bestReposition = null;

        foreach (CityRuntime repositionCity in travelSystem.GetKnownDirectDestinationCities(npcRuntime, currentCity))
        {
            if (repositionCity == null || repositionCity == currentCity)
            {
                continue;
            }

            int repositionDays = travelSystem.GetTravelDays(currentCity, repositionCity);

            if (repositionDays <= 0)
            {
                continue;
            }

            float repositionCost = travelSystem.GetTravelCost(repositionDays);
            MerchantTradeOpportunity tradeOpportunity = FindBestTradeOpportunityFrom(
                npcRuntime,
                repositionCity,
                repositionCost,
                repositionDays);

            if (tradeOpportunity == null)
            {
                continue;
            }

            if (bestReposition == null || tradeOpportunity.Score > bestReposition.TradeOpportunity.Score)
            {
                bestReposition = new MerchantTradeRepositionOpportunity(repositionCity, repositionCost, tradeOpportunity);
            }
        }

        return bestReposition;
    }

    private MerchantTradeOpportunity FindBestTradeDestinationForPlan(NpcRuntime npcRuntime, MerchantTradePlanRuntime plan)
    {
        CityRuntime currentCity = npcRuntime.CurrentCity;

        if (currentCity == null || plan == null || plan.Item == null || travelSystem == null)
        {
            return null;
        }

        int amount = GetPlannedTradeAmount(npcRuntime, plan);

        if (amount <= 0)
        {
            return null;
        }

        MerchantTradeOpportunity bestOpportunity = null;

        foreach (CityRuntime targetCity in travelSystem.GetKnownDirectDestinationCities(npcRuntime, currentCity))
        {
            if (targetCity == null || targetCity == currentCity)
            {
                continue;
            }

            int travelDays = travelSystem.GetTravelDays(currentCity, targetCity);

            if (travelDays <= 0)
            {
                continue;
            }

            float travelCost = travelSystem.GetTravelCost(travelDays);

            if (npcRuntime.Money < travelCost)
            {
                continue;
            }

            if (TryGetUsefulObservation(npcRuntime, targetCity, plan.Item, out CommercialMarketObservation targetObservation, out float freshness) == false)
            {
                continue;
            }

            float sellPrice = targetObservation.ObservedPrice;
            float profitPerItem = sellPrice - plan.PurchasePricePerItem;
            float netProfit = profitPerItem * amount - travelCost;

            if (profitPerItem < minimumProfitPerItem || netProfit <= 0f)
            {
                continue;
            }

            float score = CalculateTradeScore(netProfit, travelDays)
                * freshness
                * GetTradePreferenceMultiplier(npcRuntime, plan.Item);

            if (bestOpportunity == null || score > bestOpportunity.Score)
            {
                CommercialDecisionEvidence evidence = CreateCommercialEvidence(
                    npcRuntime,
                    currentCity,
                    targetCity,
                    plan.TargetCity,
                    plan.Item,
                    null,
                    0f,
                    targetObservation,
                    freshness,
                    amount,
                    plan.PurchasePricePerItem,
                    sellPrice,
                    travelCost,
                    profitPerItem * amount,
                    netProfit,
                    score);
                bestOpportunity = new MerchantTradeOpportunity(plan.Item, targetCity, amount, plan.PurchasePricePerItem, sellPrice, profitPerItem, netProfit, score, evidence);
            }
        }

        return bestOpportunity;
    }

    private MerchantTradeOpportunity FindBestLocalMerchantBuyer(NpcRuntime sellerRuntime, ItemData item, int requestedAmount, float referencePrice)
    {
        if (sellerRuntime == null || sellerRuntime.CurrentCity == null || item == null || requestedAmount <= 0)
        {
            return null;
        }

        int sellerAmount = sellerRuntime.Inventory.GetAmount(item);
        int maxAmount = Mathf.Min(requestedAmount, sellerAmount);

        if (maxAmount <= 0)
        {
            return null;
        }

        MerchantTradeOpportunity bestBuyer = null;

        if (TryGetUsefulObservation(sellerRuntime, sellerRuntime.CurrentCity, item, out CommercialMarketObservation localObservation, out float freshness) == false)
        {
            return null;
        }

        float retailPrice = localObservation.ObservedPrice;

        foreach (NpcRuntime candidate in sellerRuntime.CurrentCity.ImportantNpcs)
        {
            if (candidate == null || candidate == sellerRuntime || candidate.CurrentCity != sellerRuntime.CurrentCity || candidate.IsTraveling == true || IsLocalMerchant(candidate) == false)
            {
                continue;
            }

            float preferenceMultiplier = GetTradePreferenceMultiplier(candidate, item);
            float unitPrice = CalculateLocalMerchantUnitPrice(candidate, item, retailPrice);
            float profitPerItem = unitPrice - referencePrice;

            if (profitPerItem < minimumProfitPerItem)
            {
                continue;
            }

            float reserveAmount = candidate.Money * LocalMerchantReserveRatio;
            float spendableMoney = Mathf.Max(0f, candidate.Money - reserveAmount);
            int affordableAmount = Mathf.FloorToInt(spendableMoney / unitPrice);
            int amount = Mathf.Min(maxAmount, affordableAmount);

            if (amount <= 0)
            {
                continue;
            }

            float score = CalculateTradeScore(profitPerItem * amount, 1) * freshness * preferenceMultiplier;

            if (bestBuyer == null || score > bestBuyer.Score)
            {
                CommercialDecisionEvidence evidence = CreateCommercialEvidence(
                    sellerRuntime,
                    sellerRuntime.CurrentCity,
                    sellerRuntime.CurrentCity,
                    null,
                    item,
                    null,
                    0f,
                    localObservation,
                    freshness,
                    amount,
                    referencePrice,
                    unitPrice,
                    0f,
                    profitPerItem * amount,
                    profitPerItem * amount,
                    score);
                bestBuyer = new MerchantTradeOpportunity(item, sellerRuntime.CurrentCity, amount, referencePrice, unitPrice, profitPerItem, profitPerItem * amount, score, evidence, candidate);
            }
        }

        return bestBuyer;
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

            if (TryGetUsefulObservation(npcRuntime, npcRuntime.CurrentCity, inventoryItem.Item, out CommercialMarketObservation localObservation, out float freshness) == false)
            {
                continue;
            }

            float localPrice = localObservation.ObservedPrice;
            float referencePrice = inventoryItem.AverageUnitCost > 0f ? inventoryItem.AverageUnitCost : inventoryItem.Item.basePrice;
            float profitPerItem = localPrice - referencePrice;

            if (profitPerItem < minimumProfitPerItem)
            {
                continue;
            }

            int amount = Mathf.Min(maxMerchantTradeAmount, inventoryItem.Amount);
            float score = CalculateTradeScore(profitPerItem * amount, 1)
                * freshness
                * GetTradePreferenceMultiplier(npcRuntime, inventoryItem.Item);

            if (bestSale == null || score > bestSale.Score)
            {
                CommercialDecisionEvidence evidence = CreateCommercialEvidence(
                    npcRuntime,
                    npcRuntime.CurrentCity,
                    npcRuntime.CurrentCity,
                    null,
                    inventoryItem.Item,
                    null,
                    0f,
                    localObservation,
                    freshness,
                    amount,
                    referencePrice,
                    localPrice,
                    0f,
                    profitPerItem * amount,
                    profitPerItem * amount,
                    score);
                bestSale = new MerchantTradeOpportunity(inventoryItem.Item, npcRuntime.CurrentCity, amount, referencePrice, localPrice, profitPerItem, profitPerItem * amount, score, evidence);
            }
        }

        return bestSale;
    }

    private MerchantCommercialScoutingOpportunity FindBestCommercialScoutingOpportunity(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null || npcRuntime.CurrentCity == null || travelSystem == null)
        {
            return null;
        }

        List<string> relevantItemDefinitionIds = GetRelevantCommercialItemDefinitionIds(npcRuntime);
        MerchantCommercialScoutingOpportunity bestOpportunity = null;

        foreach (SpatialRouteRuntime route in travelSystem.GetKnownDirectRoutes(npcRuntime, npcRuntime.CurrentCity))
        {
            CityRuntime targetCity = travelSystem.GetCityRuntime(route.Destination);

            if (targetCity == null || targetCity == npcRuntime.CurrentCity)
            {
                continue;
            }

            float travelCost = travelSystem.GetTravelCost(route.TravelDays);

            if (travelCost < 0f || npcRuntime.Money < travelCost)
            {
                continue;
            }

            int unknownObservationCount = 0;
            int staleObservationCount = 0;
            bool hasOldestObservation = false;
            long oldestObservationDay = 0L;

            if (relevantItemDefinitionIds.Count == 0)
            {
                unknownObservationCount = 1;
            }
            else
            {
                foreach (string itemDefinitionId in relevantItemDefinitionIds)
                {
                    if (npcRuntime.CommercialKnowledge.TryGetObservation(
                        targetCity.Location.RuntimeId,
                        itemDefinitionId,
                        out CommercialMarketObservation observation) == false)
                    {
                        unknownObservationCount++;
                        continue;
                    }

                    if (knowledgePolicy.GetFreshness(observation, simulationTime.AbsoluteDay) > 0f)
                    {
                        continue;
                    }

                    staleObservationCount++;

                    if (hasOldestObservation == false || observation.ObservedDay < oldestObservationDay)
                    {
                        hasOldestObservation = true;
                        oldestObservationDay = observation.ObservedDay;
                    }
                }
            }

            if (unknownObservationCount == 0 && staleObservationCount == 0)
            {
                continue;
            }

            long oldestAgeDays = hasOldestObservation == true
                ? Math.Max(0L, simulationTime.AbsoluteDay - oldestObservationDay)
                : 0L;
            float informationNeed = unknownObservationCount * 20f
                + staleObservationCount * 12f
                + Mathf.Min(30f, (float)oldestAgeDays) * 0.5f;
            float score = informationNeed / Mathf.Max(1f, route.TravelDays + travelCost / 10f);

            if (score <= 0f)
            {
                continue;
            }

            CommercialScoutingEvidence evidence = new CommercialScoutingEvidence(
                targetCity.Location.RuntimeId,
                route.RuntimeId,
                unknownObservationCount,
                staleObservationCount,
                hasOldestObservation,
                oldestObservationDay,
                travelCost,
                route.TravelDays,
                score);
            MerchantCommercialScoutingOpportunity opportunity = new MerchantCommercialScoutingOpportunity(
                targetCity,
                travelCost,
                score,
                evidence);

            if (bestOpportunity == null
                || opportunity.Score > bestOpportunity.Score
                || (Mathf.Approximately(opportunity.Score, bestOpportunity.Score) == true
                    && string.CompareOrdinal(opportunity.Evidence.KnownRouteRuntimeId, bestOpportunity.Evidence.KnownRouteRuntimeId) < 0))
            {
                bestOpportunity = opportunity;
            }
        }

        return bestOpportunity;
    }

    private List<string> GetRelevantCommercialItemDefinitionIds(NpcRuntime npcRuntime)
    {
        List<string> definitionIds = new List<string>();
        HashSet<string> uniqueDefinitionIds = new HashSet<string>(StringComparer.Ordinal);

        if (npcRuntime?.NpcData?.job != null)
        {
            foreach (TradeItemPreference preference in npcRuntime.NpcData.job.PreferredTradeItems)
            {
                AddRelevantItemDefinitionId(definitionIds, uniqueDefinitionIds, preference?.item?.DefinitionId);
            }
        }

        foreach (CommercialMarketObservation observation in npcRuntime.CommercialKnowledge.Observations)
        {
            AddRelevantItemDefinitionId(definitionIds, uniqueDefinitionIds, observation?.ItemDefinitionId);
        }

        definitionIds.Sort(StringComparer.Ordinal);
        return definitionIds;
    }

    private static void AddRelevantItemDefinitionId(
        List<string> definitionIds,
        HashSet<string> uniqueDefinitionIds,
        string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId) == false && uniqueDefinitionIds.Add(definitionId) == true)
        {
            definitionIds.Add(definitionId);
        }
    }

    private bool TryGetUsefulObservation(
        NpcRuntime npcRuntime,
        CityRuntime cityRuntime,
        ItemData item,
        out CommercialMarketObservation observation,
        out float freshness)
    {
        observation = null;
        freshness = 0f;

        if (npcRuntime == null
            || cityRuntime == null
            || cityRuntime.Location == null
            || item == null
            || string.IsNullOrWhiteSpace(item.DefinitionId) == true
            || npcRuntime.CommercialKnowledge.TryGetObservation(cityRuntime.Location.RuntimeId, item.DefinitionId, out observation) == false)
        {
            return false;
        }

        freshness = knowledgePolicy.GetFreshness(observation, simulationTime.AbsoluteDay);
        return freshness > 0f;
    }

    private CommercialDecisionEvidence CreateCommercialEvidence(
        NpcRuntime npcRuntime,
        CityRuntime tradeOrigin,
        CityRuntime tradeDestination,
        CityRuntime previousDestination,
        ItemData item,
        CommercialMarketObservation originObservation,
        float originFreshness,
        CommercialMarketObservation destinationObservation,
        float destinationFreshness,
        int expectedQuantity,
        float expectedPurchaseUnitPrice,
        float expectedSaleUnitPrice,
        float expectedTravelCost,
        float expectedGrossProfit,
        float expectedNetProfit,
        float expectedScore)
    {
        if (npcRuntime?.CurrentCity?.Location == null
            || tradeOrigin?.Location == null
            || tradeDestination?.Location == null
            || item == null
            || string.IsNullOrWhiteSpace(item.DefinitionId) == true)
        {
            return null;
        }

        return new CommercialDecisionEvidence(
            item.DefinitionId,
            npcRuntime.CurrentCity.Location.RuntimeId,
            tradeOrigin.Location.RuntimeId,
            tradeDestination.Location.RuntimeId,
            previousDestination?.Location?.RuntimeId,
            CommercialObservationEvidence.Capture(originObservation, originFreshness),
            CommercialObservationEvidence.Capture(destinationObservation, destinationFreshness),
            expectedQuantity,
            expectedPurchaseUnitPrice,
            expectedSaleUnitPrice,
            expectedTravelCost,
            expectedGrossProfit,
            expectedNetProfit,
            expectedScore);
    }

    private float CalculateLocalMerchantUnitPrice(NpcRuntime buyerRuntime, ItemData item, float retailPrice)
    {
        float preferenceMultiplier = GetTradePreferenceMultiplier(buyerRuntime, item);
        float preferredPriceBonus = Mathf.Min(0.15f, Mathf.Max(0f, preferenceMultiplier - 1f) * 0.1f);
        return Mathf.Max(0.01f, retailPrice)
            * Mathf.Clamp(LocalMerchantWholesalePriceMultiplier + preferredPriceBonus, 0.5f, 0.85f);
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

    private bool IsTravelingMerchant(NpcRuntime npcRuntime)
    {
        return IsMerchant(npcRuntime) == true
            && npcRuntime.NpcData.job.merchantBehavior == MerchantBehavior.Traveling;
    }

    private bool IsLocalMerchant(NpcRuntime npcRuntime)
    {
        return IsMerchant(npcRuntime) == true
            && npcRuntime.NpcData.job.merchantBehavior == MerchantBehavior.Local;
    }

    private class MerchantTradeOpportunity
    {
        public ItemData Item { get; }
        public NpcRuntime TargetNpc { get; }
        public CityRuntime TargetCity { get; }
        public int Amount { get; }
        public float BuyPrice { get; }
        public float SellPrice { get; }
        public float ProfitPerItem { get; }
        public float NetProfit { get; }
        public float Score { get; }
        public CommercialDecisionEvidence Evidence { get; }

        public MerchantTradeOpportunity(ItemData item, CityRuntime targetCity, int amount, float buyPrice, float sellPrice, float profitPerItem, float netProfit, float score, CommercialDecisionEvidence evidence, NpcRuntime targetNpc = null)
        {
            Item = item;
            TargetNpc = targetNpc;
            TargetCity = targetCity;
            Amount = amount;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            ProfitPerItem = profitPerItem;
            NetProfit = netProfit;
            Score = score;
            Evidence = evidence;
        }
    }

    private class MerchantTradeRepositionOpportunity
    {
        public CityRuntime OriginCity { get; }
        public float RepositionCost { get; }
        public MerchantTradeOpportunity TradeOpportunity { get; }

        public MerchantTradeRepositionOpportunity(CityRuntime originCity, float repositionCost, MerchantTradeOpportunity tradeOpportunity)
        {
            OriginCity = originCity;
            RepositionCost = repositionCost;
            TradeOpportunity = tradeOpportunity;
        }
    }

    private sealed class MerchantCommercialScoutingOpportunity
    {
        public CityRuntime TargetCity { get; }
        public float TravelCost { get; }
        public float Score { get; }
        public CommercialScoutingEvidence Evidence { get; }

        public MerchantCommercialScoutingOpportunity(
            CityRuntime targetCity,
            float travelCost,
            float score,
            CommercialScoutingEvidence evidence)
        {
            TargetCity = targetCity;
            TravelCost = travelCost;
            Score = score;
            Evidence = evidence;
        }
    }
}
