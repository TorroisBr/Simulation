using System;

/// <summary>
/// Holds the exact selected City owner instances consumed by the Unity runtime
/// adapter. This is an owner join, not a second copy of their mutable state.
/// </summary>
internal sealed class DailyV1CityRuntimeOwners
{
    internal string RuntimeId { get; }
    internal DailyV1EffectiveDefinitions Definitions { get; }
    internal DailyV1CityDefinition Definition { get; }
    internal SettlementPopulationRuntime Population { get; }
    internal DailyV1MarketOwner Market { get; }
    internal MarketCounterpartyRuntime Counterparty { get; }
    internal PopulationEconomyRuntime PopulationEconomy { get; }

    private DailyV1CityRuntimeOwners(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        SettlementPopulationRuntime population,
        DailyV1MarketOwner market,
        MarketCounterpartyRuntime counterparty,
        PopulationEconomyRuntime populationEconomy)
    {
        RuntimeId = runtimeId;
        Definitions = definitions;
        Definition = definition;
        Population = population;
        Market = market;
        Counterparty = counterparty;
        PopulationEconomy = populationEconomy;
    }

    internal static bool TryCreateInitial(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        MarketCounterpartyRuntime suppliedCounterparty,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (string.IsNullOrWhiteSpace(runtimeId) || !IsAdmitted(definitions, definition)) return false;

        DailyV1CityData values = definition.Values;
        MarketCounterpartyRuntime counterparty = suppliedCounterparty == null
            ? CreateConfiguredCounterparty(runtimeId, values)
            : NormalizeCounterparty(suppliedCounterparty, runtimeId);
        SettlementPopulationRuntime population = new SettlementPopulationRuntime(
            runtimeId, Math.Max(0, values.initialPopulation));
        DailyV1MarketOwner market = new DailyV1MarketOwner(definitions, counterparty);
        if (!market.TryInitializeFromCity(definition)) return false;

        ConsumptionPaymentMode paymentMode = (ConsumptionPaymentMode)(values.populationConsumption?.paymentMode?.Value
            ?? (int)ConsumptionPaymentMode.Free);
        float purchasingPower = values.populationConsumption?.initialPurchasingPower ?? 0f;
        PopulationEconomyRuntime populationEconomy = new PopulationEconomyRuntime(
            runtimeId, paymentMode, purchasingPower, counterparty.LiquidityMode);

        owners = new DailyV1CityRuntimeOwners(
            runtimeId, definitions, definition, population, market, counterparty, populationEconomy);
        return true;
    }

    internal static bool TryCreateBound(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        SettlementPopulationRuntime population,
        DailyV1MarketOwner market,
        MarketCounterpartyRuntime counterparty,
        PopulationEconomyRuntime populationEconomy,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (string.IsNullOrWhiteSpace(runtimeId)
            || definitions == null
            || definition == null
            || !definitions.Cities.TryGetValue(definition.DefinitionId, out DailyV1CityDefinition admitted)
            || !ReferenceEquals(definition, admitted)
            || population == null
            || !string.Equals(population.SettlementRuntimeId, runtimeId, StringComparison.Ordinal)
            || market == null
            || !ReferenceEquals(market.Definitions, definitions)
            || counterparty == null
            || !ReferenceEquals(market.Counterparty, counterparty)
            || !string.Equals(counterparty.CounterpartyRuntimeId, runtimeId, StringComparison.Ordinal)
            || populationEconomy == null
            || !string.Equals(populationEconomy.CityRuntimeId, runtimeId, StringComparison.Ordinal))
            return false;

        owners = new DailyV1CityRuntimeOwners(
            runtimeId, definitions, definition, population, market, counterparty, populationEconomy);
        return true;
    }

    private static bool IsAdmitted(
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition) =>
        definitions != null && definition != null
        && definitions.Cities.TryGetValue(definition.DefinitionId, out DailyV1CityDefinition admitted)
        && ReferenceEquals(definition, admitted);

    private static MarketCounterpartyRuntime CreateConfiguredCounterparty(
        string runtimeId,
        DailyV1CityData values)
    {
        MarketLiquidityMode liquidityMode = (MarketLiquidityMode)(values.marketLiquidity?.liquidityMode?.Value
            ?? (int)MarketLiquidityMode.Open);
        if (liquidityMode == MarketLiquidityMode.Open)
            return MarketCounterpartyRuntime.CreateOpen(runtimeId);
        if (liquidityMode != MarketLiquidityMode.AccountBacked)
            throw new ArgumentOutOfRangeException(nameof(values), liquidityMode,
                "CityData contains an unsupported MarketLiquidityMode.");
        return MarketCounterpartyRuntime.CreateAccountBacked(
            runtimeId,
            new MoneyAccountRuntime(values.marketLiquidity?.initialPurchasingPower ?? 0f));
    }

    private static MarketCounterpartyRuntime NormalizeCounterparty(
        MarketCounterpartyRuntime counterparty,
        string runtimeId)
    {
        if (counterparty.LiquidityMode == MarketLiquidityMode.Open
            && string.IsNullOrWhiteSpace(counterparty.CounterpartyRuntimeId))
            return MarketCounterpartyRuntime.CreateOpen(runtimeId);
        if (!string.Equals(counterparty.CounterpartyRuntimeId, runtimeId, StringComparison.Ordinal))
            throw new ArgumentException(
                "A CityRuntime market counterparty must use the CityRuntime RuntimeId.",
                nameof(counterparty));
        return counterparty;
    }

    /// <summary>
    /// Applies the selected exogenous daily source and free population sink using the exact
    /// admitted definition nodes and the already-installed Core owners.
    /// </summary>
    internal LocalDailyMaterialFlowResult SimulateSelectedExogenousDailyMaterialFlow(
        string locationId,
        long absoluteDay,
        string calendarVersion,
        Action<SimulationLogCategory, string> logOutput)
    {
        DailyV1CityData city = Definition.Values;
        if (city.productionConfigs == null || city.productionConfigs.Count == 0
            || city.productionConfigs[0] == null)
            throw new LocalDailyMaterialFlowRejectedException("Selected local material flow requires an authored source.");

        DailyV1CityProductionConfig source = city.productionConfigs[0];
        if (string.IsNullOrWhiteSpace(source.item)
            || !Definitions.Items.TryGetValue(source.item, out DailyV1ItemDefinition item))
            throw new LocalDailyMaterialFlowRejectedException("Selected local material flow source has no admitted item definition.");

        DailyV1MarketItemConfig itemConfig = null;
        foreach (DailyV1MarketItemConfig candidate in city.marketItems ?? Array.Empty<DailyV1MarketItemConfig>())
        {
            if (candidate != null && string.Equals(candidate.item, item.DefinitionId, StringComparison.Ordinal))
            {
                itemConfig = candidate;
                break;
            }
        }

        if (itemConfig == null || (ConsumptionPaymentMode)(city.populationConsumption?.paymentMode?.Value ?? 0)
                != ConsumptionPaymentMode.Free)
            throw new LocalDailyMaterialFlowRejectedException("Selected local material flow requires a matching market item and free population consumption.");

        int opening = Market.GetAmount(item);
        int applied = Market.AddStock(item, source.amountPerDay);
        string rejection = applied == source.amountPerDay ? string.Empty : "AggregateStockOverflow";
        int requested = Math.Max(0, UnityMathfCompatibility.RoundToInt(
            Population.CurrentPopulation / 1000f * itemConfig.consumptionPer1000Population));
        int actual = Market.RemoveStockUpTo(item, requested);
        int closing = Market.GetAmount(item);

        LocalDailyMaterialFlowResult result = new LocalDailyMaterialFlowResult(
            city.settlementSemanticId, source.productionSourceId, city.marketStoreSemanticId, locationId,
            item.DefinitionId, source.contentRevision, "Economy.Enabled=true;PaymentMode=Free",
            "simulation-calendar", calendarVersion, true, Population.CurrentPopulation,
            itemConfig.consumptionPer1000Population, absoluteDay, opening,
            source.amountPerDay, applied, rejection, requested, actual, closing);

        logOutput?.Invoke(SimulationLogCategory.EconomyProduction,
            $"{city.cityName} source {source.productionSourceId}: {applied}/{source.amountPerDay}");
        if (actual > 0)
            logOutput?.Invoke(SimulationLogCategory.EconomyConsumption,
                $"{city.cityName} consumed {actual} {item.Values.itemName}");
        return result;
    }
}
