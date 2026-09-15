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
    [NonSerialized] private PopulationEconomyRuntime populationEconomy;
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
    public PopulationEconomyRuntime PopulationEconomy
    {
        get
        {
            if (populationEconomy == null)
            {
                populationEconomy = CreateConfiguredPopulationEconomy();
            }

            return populationEconomy;
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
        populationEconomy = CreateConfiguredPopulationEconomy();
    }

    public IReadOnlyList<CityProductionResult> SimulateProductionDay()
    {
        List<CityProductionResult> results = new List<CityProductionResult>();

        if (cityData == null || cityData.productionConfigs == null)
        {
            return results.AsReadOnly();
        }

        foreach (CityProductionConfig production in cityData.productionConfigs)
        {
            if (production == null || production.item == null || production.amountPerDay <= 0)
            {
                continue;
            }

            int produced = Market.AddStock(production.item, production.amountPerDay);
            if (produced <= 0)
            {
                continue;
            }

            results.Add(new CityProductionResult(
                RuntimeId,
                production.item.DefinitionId,
                produced,
                Market.StockOwnerRuntimeId));
            logger?.Log(SimulationLogCategory.EconomyProduction, $"{CityName} produziu {produced} {production.item.itemName}");
        }

        return results.AsReadOnly();
    }

    public IReadOnlyList<CityConsumptionResult> SimulateConsumptionDay()
    {
        List<CityConsumptionResult> results = new List<CityConsumptionResult>();

        if (cityData == null || cityData.marketItems == null)
        {
            return results.AsReadOnly();
        }

        foreach (MarketItemConfig config in cityData.marketItems)
        {
            if (config == null || config.item == null || config.consumptionPer1000Population <= 0f)
            {
                continue;
            }

            int desiredConsumption = Mathf.RoundToInt(currentPopulation / 1000f * config.consumptionPer1000Population);
            if (desiredConsumption <= 0)
            {
                results.Add(new CityConsumptionResult(
                    RuntimeId,
                    PopulationEconomy.PopulationEconomicRuntimeId,
                    config.item.DefinitionId,
                    desiredConsumption,
                    0,
                    PopulationEconomy.PaymentMode,
                    0f,
                    0f));
                continue;
            }

            int consumed;
            float unitPrice = 0f;
            float totalPaid = 0f;

            if (PopulationEconomy.PaymentMode == ConsumptionPaymentMode.Free)
            {
                consumed = Market.RemoveStockUpTo(config.item, desiredConsumption);
            }
            else
            {
                EconomyTransactionResult receipt = new EconomyTransactionService().TryExecutePopulationConsumption(
                    PopulationEconomy,
                    Market,
                    config.item,
                    desiredConsumption);
                consumed = receipt.Quantity;
                unitPrice = receipt.UnitPrice;
                totalPaid = receipt.TotalPrice;
            }

            results.Add(new CityConsumptionResult(
                RuntimeId,
                PopulationEconomy.PopulationEconomicRuntimeId,
                config.item.DefinitionId,
                desiredConsumption,
                consumed,
                PopulationEconomy.PaymentMode,
                unitPrice,
                totalPaid));

            if (consumed > 0)
            {
                logger?.Log(SimulationLogCategory.EconomyConsumption, $"{CityName} consumiu {consumed} {config.item.itemName}");
            }
        }

        return results.AsReadOnly();
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

        if (npcRuntime.IsAlive == false)
        {
            return;
        }

        if (npcRuntime.IsTraveling == true)
        {
            return;
        }

        if (npcRuntime.CurrentCity != this || npcRuntime.CurrentLocation != Location)
        {
            if (npcRuntime.CurrentCity != null)
            {
                npcRuntime.CurrentCity.RemoveImportantNpc(npcRuntime);
            }

            if (npcRuntime.SetCurrentPresence(Location, this) == false)
            {
                return;
            }
        }

        if (ImportantNpcs.Contains(npcRuntime) == false)
        {
            ImportantNpcs.Add(npcRuntime);
        }
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
            npcRuntime.ClearCurrentPresenceFromCity(this);
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

    private PopulationEconomyRuntime CreateConfiguredPopulationEconomy()
    {
        PopulationConsumptionConfig consumption = cityData != null
            ? cityData.PopulationConsumption
            : new PopulationConsumptionConfig();
        return new PopulationEconomyRuntime(
            runtimeId,
            consumption,
            MarketCounterparty.LiquidityMode);
    }
}
