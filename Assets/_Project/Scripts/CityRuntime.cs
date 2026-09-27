using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CityRuntime
{
    [NonSerialized] private MutationGuardBinding runtimeMutationGuardBinding = new MutationGuardBinding();
    [SerializeField] private string runtimeId;
    [SerializeField] private CityData cityData;
    [NonSerialized] private SettlementPopulationRuntime population;
    [SerializeField] private MarketRuntime market;
    [NonSerialized] private MarketCounterpartyRuntime marketCounterparty;
    [NonSerialized] private PopulationEconomyRuntime populationEconomy;
    [NonSerialized] private SpatialLocationRuntime location;
    [NonSerialized] private List<NpcRuntime> importantNpcs = new List<NpcRuntime>();
    [NonSerialized] private SimulationLogger logger;
    [NonSerialized] private LocalDailyMaterialFlowResult lastMaterialFlow;

    public string RuntimeId => runtimeId;
    public CityData CityData => cityData;
    public string DefinitionId => cityData != null ? cityData.DefinitionId : string.Empty;
    public SpatialLocationRuntime Location => location;
    public SettlementPopulationRuntime Population
    {
        get
        {
            if (population == null)
            {
                population = CreatePopulationRuntime();
            }

            return population;
        }
    }
    public int CurrentPopulation => Population.CurrentPopulation;
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
    public bool HasLocalDailyMaterialFlow => cityData != null
        && (!string.IsNullOrWhiteSpace(cityData.settlementSemanticId)
            || !string.IsNullOrWhiteSpace(cityData.materialFlowLocationId)
            || !string.IsNullOrWhiteSpace(cityData.marketStoreSemanticId)
            || (cityData.productionConfigs != null && cityData.productionConfigs.Exists(source =>
                source != null && !string.IsNullOrWhiteSpace(source.productionSourceId))));
    public LocalDailyMaterialFlowResult LastMaterialFlow => lastMaterialFlow;

    internal void ValidateLocalDailyMaterialFlowAnchor(LegacySpatialAnchorBindingStore bindings, SpatialAuthorityStore spatial)
    {
        if (!HasLocalDailyMaterialFlow) return;
        if (string.IsNullOrWhiteSpace(cityData.settlementSemanticId)
            || string.IsNullOrWhiteSpace(cityData.materialFlowLocationId)
            || string.IsNullOrWhiteSpace(cityData.marketStoreSemanticId)
            || cityData.productionConfigs == null || cityData.productionConfigs.Count != 1)
            throw new LocalDailyMaterialFlowRejectedException("P14-A requires settlement, LocationId, market store, and exactly one authored source.");
        CityProductionConfig source = cityData.productionConfigs[0];
        if (source == null || source.item == null || source.amountPerDay <= 0
            || string.IsNullOrWhiteSpace(source.productionSourceId) || string.IsNullOrWhiteSpace(source.contentRevision))
            throw new LocalDailyMaterialFlowRejectedException("P14-A source identity, item, positive quantity, and content revision are required.");
        if (PopulationEconomy.PaymentMode != ConsumptionPaymentMode.Free
            || cityData.marketItems == null || cityData.marketItems.Count != 1
            || cityData.marketItems[0] == null || cityData.marketItems[0].item == null
            || !string.Equals(cityData.marketItems[0].item.DefinitionId, source.item.DefinitionId, StringComparison.Ordinal))
            throw new LocalDailyMaterialFlowRejectedException("P14-A requires exactly one market item row matching its source item and free population consumption.");
        SpatialAnchorOwnerId owner = new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.City, RuntimeId);
        if (bindings == null || !bindings.TryGet(owner, out LocationId bound)
            || !string.Equals(bound?.Value, cityData.materialFlowLocationId, StringComparison.Ordinal)
            || spatial == null || !spatial.TryGet(bound, out _))
            throw new LocalDailyMaterialFlowRejectedException("P14-A City anchor must resolve and match its authored stable LocationId.");
    }

    internal void SimulateLocalDailyMaterialFlow(long absoluteDay, string calendarVersion)
    {
        CityProductionConfig source = cityData.productionConfigs[0];
        MarketItemConfig itemConfig = cityData.marketItems?.Find(candidate => candidate != null
            && candidate.item != null && string.Equals(candidate.item.DefinitionId, source.item.DefinitionId, StringComparison.Ordinal));
        if (itemConfig == null || PopulationEconomy.PaymentMode != ConsumptionPaymentMode.Free)
            throw new LocalDailyMaterialFlowRejectedException("P14-A requires a market item and free population consumption for the source item.");

        int opening = Market.GetAmount(source.item);
        int applied = Market.AddStock(source.item, source.amountPerDay);
        string rejection = applied == source.amountPerDay ? string.Empty : "AggregateStockOverflow";
        int requested = Math.Max(0, Mathf.RoundToInt(Population.CurrentPopulation / 1000f * itemConfig.consumptionPer1000Population));
        int actual = Market.RemoveStockUpTo(source.item, requested);
        int closing = Market.GetAmount(source.item);
        lastMaterialFlow = new LocalDailyMaterialFlowResult(cityData.settlementSemanticId, source.productionSourceId,
            cityData.marketStoreSemanticId, cityData.materialFlowLocationId, source.item.DefinitionId,
            source.contentRevision, "Economy.Enabled=true;PaymentMode=Free", "simulation-calendar", calendarVersion,
            true, Population.CurrentPopulation, itemConfig.consumptionPer1000Population, absoluteDay, opening,
            source.amountPerDay, applied, rejection, requested, actual, closing);
        logger?.Log(SimulationLogCategory.EconomyProduction, $"{CityName} source {source.productionSourceId}: {applied}/{source.amountPerDay}");
        if (actual > 0) logger?.Log(SimulationLogCategory.EconomyConsumption, $"{CityName} consumed {actual} {source.item.itemName}");
    }

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
        population = CreatePopulationRuntime();
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

            int desiredConsumption = Mathf.RoundToInt(Population.CurrentPopulation / 1000f * config.consumptionPer1000Population);
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

    internal bool CanBindRuntimeMutationGuard(AuthoritativeMutationGuard guard)
    {
        EnsureRuntimeMutationGuardBinding();
        return runtimeMutationGuardBinding.CanBindTo(guard)
            && Population.CanBindMutationGuard(guard);
    }

    internal bool TryBindRuntimeMutationGuard(AuthoritativeMutationGuard guard)
    {
        EnsureRuntimeMutationGuardBinding();
        return CanBindRuntimeMutationGuard(guard)
            && Population.TryBindMutationGuard(guard)
            && runtimeMutationGuardBinding.TryBindTo(guard);
    }

    private void EnsureRuntimeMutationGuardBinding()
    {
        if (runtimeMutationGuardBinding == null)
        {
            runtimeMutationGuardBinding = new MutationGuardBinding();
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

    private SettlementPopulationRuntime CreatePopulationRuntime()
    {
        int initialPopulation = cityData != null ? Mathf.Max(0, cityData.initialPopulation) : 0;
        return new SettlementPopulationRuntime(runtimeId, initialPopulation);
    }
}
