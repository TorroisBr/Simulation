using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CityRuntime
{
    [SerializeField] private string runtimeId;
    [SerializeField] private CityData cityData;
    [SerializeField] private int currentPopulation;
    [SerializeField] private MarketRuntime market;
    [NonSerialized] private MarketCounterpartyRuntime marketCounterparty;
    [NonSerialized] private SpatialLocationRuntime location;
    [NonSerialized] private List<NpcRuntime> importantNpcs = new List<NpcRuntime>();
    [NonSerialized] private SimulationLogger logger;

    public string RuntimeId => runtimeId;
    public CityData CityData => cityData;
    public string DefinitionId => cityData != null ? cityData.DefinitionId : string.Empty;
    public SpatialLocationRuntime Location => location;
    public int CurrentPopulation => currentPopulation;
    public MarketRuntime Market
    {
        get
        {
            if (market == null)
            {
                marketCounterparty = NormalizeMarketCounterparty(marketCounterparty, runtimeId);
                market = new MarketRuntime(new List<MarketItemConfig>(), marketCounterparty);
            }

            return market;
        }
    }

    public MarketCounterpartyRuntime MarketCounterparty
    {
        get
        {
            if (marketCounterparty == null)
            {
                marketCounterparty = NormalizeMarketCounterparty(market != null ? market.Counterparty : null, runtimeId);
            }

            return marketCounterparty;
        }
    }
    public List<NpcRuntime> ImportantNpcs => importantNpcs ?? (importantNpcs = new List<NpcRuntime>());
    public string CityName => cityData != null ? cityData.cityName : "Cidade desconhecida";

    public CityRuntime(
        string runtimeId,
        CityData cityData,
        SpatialLocationRuntime location,
        SimulationLogger logger = null,
        MarketCounterpartyRuntime marketCounterparty = null)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("CityRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

        if (location == null)
        {
            throw new ArgumentNullException(nameof(location));
        }

        this.runtimeId = runtimeId;
        this.cityData = cityData;
        this.location = location;
        this.logger = logger ?? new SimulationLogger(null);
        this.marketCounterparty = marketCounterparty != null
            ? NormalizeMarketCounterparty(marketCounterparty, runtimeId)
            : CreateConfiguredMarketCounterparty(runtimeId, cityData);
        currentPopulation = cityData != null ? Mathf.Max(0, cityData.initialPopulation) : 0;
        market = cityData != null
            ? new MarketRuntime(cityData.marketItems, this.marketCounterparty)
            : new MarketRuntime(new List<MarketItemConfig>(), this.marketCounterparty);
    }

    public void SimulateProductionDay()
    {
        if (cityData == null || cityData.productionConfigs == null)
        {
            return;
        }

        foreach (CityProductionConfig production in cityData.productionConfigs)
        {
            if (production == null || production.item == null || production.amountPerDay <= 0)
            {
                continue;
            }

            Market.AddStock(production.item, production.amountPerDay);
            logger?.Log(SimulationLogCategory.EconomyProduction, $"{CityName} produziu {production.amountPerDay} {production.item.itemName}");
        }
    }

    public void SimulateConsumptionDay()
    {
        if (cityData == null || cityData.marketItems == null)
        {
            return;
        }

        foreach (MarketItemConfig config in cityData.marketItems)
        {
            if (config == null || config.item == null || config.consumptionPer1000Population <= 0f)
            {
                continue;
            }

            int desiredConsumption = Mathf.RoundToInt(currentPopulation / 1000f * config.consumptionPer1000Population);
            int consumed = Market.RemoveStockUpTo(config.item, desiredConsumption);

            if (consumed > 0)
            {
                logger?.Log(SimulationLogCategory.EconomyConsumption, $"{CityName} consumiu {consumed} {config.item.itemName}");
            }
        }
    }

    public void UpdateMarketPrices()
    {
        Market.UpdatePrices();
        logger?.Log(SimulationLogCategory.Market, $"{CityName} atualizou os precos do mercado.");
    }

    public void AddImportantNpc(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return;
        }

        if (ImportantNpcs.Contains(npcRuntime) == false)
        {
            ImportantNpcs.Add(npcRuntime);
        }

        npcRuntime.SetCurrentCity(this);
    }

    public void RemoveImportantNpc(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return;
        }

        ImportantNpcs.Remove(npcRuntime);

        if (npcRuntime.CurrentCity == this)
        {
            npcRuntime.SetCurrentCity(null);
        }
    }

    private static MarketCounterpartyRuntime NormalizeMarketCounterparty(
        MarketCounterpartyRuntime counterparty,
        string cityRuntimeId)
    {
        if (counterparty == null)
        {
            return MarketCounterpartyRuntime.CreateOpen(cityRuntimeId);
        }

        if (counterparty.LiquidityMode == MarketLiquidityMode.Open
            && string.IsNullOrWhiteSpace(counterparty.CounterpartyRuntimeId) == true)
        {
            return MarketCounterpartyRuntime.CreateOpen(cityRuntimeId);
        }

        if (string.Equals(counterparty.CounterpartyRuntimeId, cityRuntimeId, StringComparison.Ordinal) == false)
        {
            throw new ArgumentException(
                "A CityRuntime market counterparty must use the CityRuntime RuntimeId.",
                nameof(counterparty));
        }

        return counterparty;
    }

    private static MarketCounterpartyRuntime CreateConfiguredMarketCounterparty(
        string cityRuntimeId,
        CityData configuredCity)
    {
        MarketLiquidityConfig liquidity = configuredCity != null
            ? configuredCity.MarketLiquidity
            : null;

        if (liquidity == null || liquidity.liquidityMode == MarketLiquidityMode.Open)
        {
            return MarketCounterpartyRuntime.CreateOpen(cityRuntimeId);
        }

        if (liquidity.liquidityMode != MarketLiquidityMode.AccountBacked)
        {
            throw new ArgumentOutOfRangeException(
                nameof(configuredCity),
                liquidity.liquidityMode,
                "CityData contains an unsupported MarketLiquidityMode.");
        }

        MoneyAccountRuntime account = new MoneyAccountRuntime(liquidity.initialPurchasingPower);
        return MarketCounterpartyRuntime.CreateAccountBacked(cityRuntimeId, account);
    }
}
