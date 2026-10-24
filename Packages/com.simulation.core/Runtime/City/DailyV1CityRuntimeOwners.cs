using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Holds the exact selected City owner instances consumed by the Unity runtime
/// adapter. This is an owner join, not a second copy of their mutable state.
/// </summary>
internal sealed partial class DailyV1CityRuntimeOwners
{
    internal string RuntimeId { get; }
    internal string LocationRuntimeId { get; }
    internal DailyV1EffectiveDefinitions Definitions { get; }
    internal DailyV1CityDefinition Definition { get; }
    internal SettlementPopulationRuntime Population { get; }
    internal DailyV1MarketOwner Market { get; }
    internal MarketCounterpartyRuntime Counterparty { get; }
    internal PopulationEconomyRuntime PopulationEconomy { get; }
    internal DailyV1CityNpcPresenceOwner NpcPresence { get; }
    internal DailyV1FiniteProductionSourceOwner FiniteSource { get; }
    // Latest deterministic material-flow receipt is a read-only diagnostic
    // projection of the authoritative Core operation. It is not a persisted
    // owner value and does not participate in mutation revision accounting.
    internal LocalDailyMaterialFlowResult LastMaterialFlow { get; private set; }

    private DailyV1CityRuntimeOwners(
        string runtimeId,
        string locationRuntimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        SettlementPopulationRuntime population,
        DailyV1MarketOwner market,
        MarketCounterpartyRuntime counterparty,
        PopulationEconomyRuntime populationEconomy,
        DailyV1CityNpcPresenceOwner npcPresence,
        DailyV1FiniteProductionSourceOwner finiteSource)
    {
        RuntimeId = runtimeId;
        LocationRuntimeId = locationRuntimeId;
        Definitions = definitions;
        Definition = definition;
        Population = population;
        Market = market;
        Counterparty = counterparty;
        PopulationEconomy = populationEconomy;
        NpcPresence = npcPresence;
        FiniteSource = finiteSource;
    }

    internal static bool TryCreateInitial(
        string runtimeId,
        string locationRuntimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        MarketCounterpartyRuntime suppliedCounterparty,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (string.IsNullOrWhiteSpace(runtimeId) || string.IsNullOrWhiteSpace(locationRuntimeId)
            || !IsAdmitted(definitions, definition)) return false;

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
        if (!DailyV1CityNpcPresenceOwner.TryCreateInitial(
                runtimeId, out DailyV1CityNpcPresenceOwner npcPresence))
            return false;
        if (!DailyV1FiniteProductionSourceOwner.TryCreate(
                definitions, definition, out DailyV1FiniteProductionSourceOwner finiteSource))
            return false;

        owners = new DailyV1CityRuntimeOwners(
            runtimeId, locationRuntimeId, definitions, definition, population, market, counterparty,
            populationEconomy, npcPresence, finiteSource);
        return true;
    }

    internal static bool TryCreateBound(
        string runtimeId,
        string locationRuntimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        SettlementPopulationRuntime population,
        DailyV1MarketOwner market,
        MarketCounterpartyRuntime counterparty,
        PopulationEconomyRuntime populationEconomy,
        DailyV1CityNpcPresenceOwner npcPresence,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (string.IsNullOrWhiteSpace(runtimeId)
            || string.IsNullOrWhiteSpace(locationRuntimeId)
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
            || !string.Equals(populationEconomy.CityRuntimeId, runtimeId, StringComparison.Ordinal)
            || npcPresence == null
            || !string.Equals(npcPresence.CityRuntimeId, runtimeId, StringComparison.Ordinal)
            || !DailyV1FiniteProductionSourceOwner.TryCreate(
                definitions, definition, out DailyV1FiniteProductionSourceOwner finiteSource))
            return false;

        owners = new DailyV1CityRuntimeOwners(
            runtimeId, locationRuntimeId, definitions, definition, population, market, counterparty,
            populationEconomy, npcPresence, finiteSource);
        return true;
    }

    /// <summary>
    /// Hydrates the admitted P12-D City owner bundle directly from detached Core
    /// values. Unity may build a compatibility projection from the returned
    /// owners, but it must not construct a second population, market, or
    /// membership authority for the selected profile.
    /// </summary>
    internal static bool TryCreateRestoredP12DOwners(
        string runtimeId,
        string locationRuntimeId,
        string populationRuntimeId,
        string populationEconomicRuntimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        int currentPopulation,
        long populationRevision,
        IReadOnlyList<PopulationOperationReceiptSnapshot> populationReceipts,
        long populationReceiptRevision,
        IReadOnlyList<DailyV1MarketItemState> marketRows,
        long marketRevision,
        IReadOnlyList<string> orderedNpcRuntimeIds,
        long importantNpcRevision,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (string.IsNullOrWhiteSpace(runtimeId)
            || string.IsNullOrWhiteSpace(locationRuntimeId)
            || !string.Equals(populationRuntimeId, runtimeId, StringComparison.Ordinal)
            || !string.Equals(populationEconomicRuntimeId, "population-" + runtimeId, StringComparison.Ordinal)
            || marketRows == null
            || orderedNpcRuntimeIds == null
            || marketRevision < 0L
            || importantNpcRevision < 0L
            || !IsAdmitted(definitions, definition)
            || !SettlementPopulationRuntime.TryCreateFromOwnerSnapshot(
                populationRuntimeId,
                currentPopulation,
                populationRevision,
                populationReceipts,
                populationReceiptRevision,
                out SettlementPopulationRuntime population))
            return false;

        MarketCounterpartyRuntime counterparty = MarketCounterpartyRuntime.CreateOpen(runtimeId);
        DailyV1MarketOwner market = new DailyV1MarketOwner(definitions, counterparty);
        if (!market.TryCreateFromOwnerSnapshot(
                marketRows, marketRevision, counterparty, out DailyV1MarketOwner restoredMarket)
            || !DailyV1CityNpcPresenceOwner.TryCreateBound(
                runtimeId, orderedNpcRuntimeIds, importantNpcRevision,
                out DailyV1CityNpcPresenceOwner npcPresence))
            return false;

        PopulationEconomyRuntime populationEconomy = new PopulationEconomyRuntime(
            runtimeId,
            ConsumptionPaymentMode.Free,
            0f,
            MarketLiquidityMode.Open);
        if (!TryCreateBound(
                runtimeId,
                locationRuntimeId,
                definitions,
                definition,
                population,
                restoredMarket,
                counterparty,
                populationEconomy,
                npcPresence,
                out DailyV1CityRuntimeOwners restoredOwners)
            || restoredOwners.HasLocalDailyMaterialFlow)
            return false;

        owners = restoredOwners;
        return true;
    }

    private static bool IsAdmitted(
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition) =>
        definitions != null && definition != null
        && definitions.Cities.TryGetValue(definition.DefinitionId, out DailyV1CityDefinition admitted)
        && ReferenceEquals(definition, admitted);

    /// <summary>
    /// Mirrors the selected City adapter's local-flow classification using
    /// only the admitted immutable definition and its Core-owned source.
    /// </summary>
    internal bool HasLocalDailyMaterialFlow
    {
        get
        {
            DailyV1CityData city = Definition?.Values;
            if (city == null) return false;
            LocalMaterialFlowProfile profile = (LocalMaterialFlowProfile)(city.materialFlowProfile?.Value
                ?? (int)LocalMaterialFlowProfile.ExogenousDaily);
            return FiniteSource != null
                || profile != LocalMaterialFlowProfile.ExogenousDaily
                || !string.IsNullOrWhiteSpace(city.settlementSemanticId)
                || !string.IsNullOrWhiteSpace(city.materialFlowLocationId)
                || !string.IsNullOrWhiteSpace(city.marketStoreSemanticId)
                || (city.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>()).Any(source =>
                    source != null && (!string.IsNullOrWhiteSpace(source.productionSourceId)
                        || source.initialReserve != 0));
        }
    }

    internal bool HasConfiguredAccountBackedConsumption =>
        PopulationEconomy?.PaymentMode == ConsumptionPaymentMode.AccountBacked
        && (Definition?.Values?.marketItems ?? Array.Empty<DailyV1MarketItemConfig>())
            .Any(row => row != null && row.consumptionPer1000Population > 0f);

    internal void RecordSelectedMaterialFlow(LocalDailyMaterialFlowResult result)
    {
        LastMaterialFlow = result;
    }

    /// <summary>
    /// Applies the selected City's existing ordinary production pass through
    /// its admitted definition nodes and canonical Market owner.
    /// </summary>
    internal void SimulateSelectedProductionDay(Action<SimulationLogCategory, string> logOutput)
    {
        DailyV1CityData city = Definition.Values;
        foreach (DailyV1CityProductionConfig production in
            city.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>())
        {
            if (production == null || production.amountPerDay <= 0
                || !Definitions.Items.TryGetValue(production.item ?? string.Empty,
                    out DailyV1ItemDefinition item))
                continue;

            int produced = Market.AddStock(item, production.amountPerDay);
            if (produced <= 0) continue;
            logOutput?.Invoke(SimulationLogCategory.EconomyProduction,
                $"{city.cityName} produziu {produced} {item.Values.itemName}");
        }
    }

    /// <summary>
    /// Applies the selected City's existing ordinary consumption pass. Free
    /// consumption and account-backed transactions write directly to the same
    /// Core Market and MoneyAccount owners used by capture and hydration.
    /// </summary>
    internal void SimulateSelectedConsumptionDay(
        Action<SimulationLogCategory, string> logOutput)
    {
        DailyV1CityData city = Definition.Values;
        foreach (DailyV1MarketItemConfig config in city.marketItems ?? Array.Empty<DailyV1MarketItemConfig>())
        {
            if (config == null || config.consumptionPer1000Population <= 0f
                || !Definitions.Items.TryGetValue(config.item ?? string.Empty,
                    out DailyV1ItemDefinition item))
                continue;

            int desiredConsumption = UnityMathfCompatibility.RoundToInt(
                Population.CurrentPopulation / 1000f * config.consumptionPer1000Population);
            if (desiredConsumption <= 0) continue;

            int consumed = PopulationEconomy.PaymentMode == ConsumptionPaymentMode.Free
                ? Market.RemoveStockUpTo(item, desiredConsumption)
                : DailyV1PopulationConsumptionOperation.Execute(
                    PopulationEconomy, Counterparty, Market, item, desiredConsumption).Quantity;
            if (consumed > 0)
                logOutput?.Invoke(SimulationLogCategory.EconomyConsumption,
                    $"{city.cityName} consumiu {consumed} {item.Values.itemName}");
        }
    }

    internal void UpdateSelectedMarketPrices(Action<SimulationLogCategory, string> logOutput)
    {
        Market.UpdatePrices();
        logOutput?.Invoke(SimulationLogCategory.Market,
            $"{Definition.Values.cityName} atualizou os precos do mercado.");
    }

    /// <summary>
    /// Executes the selected City's admitted daily material-flow policy through
    /// the same Core owners used by bootstrap, capture, and hydration. The
    /// Unity CityRuntime supplies only the host location projection and log sink.
    /// </summary>
    internal LocalDailyMaterialFlowResult SimulateSelectedLocalDailyMaterialFlow(
        string locationId,
        long absoluteDay,
        string calendarVersion,
        Action<SimulationLogCategory, string> logOutput)
    {
        LocalMaterialFlowProfile profile = (LocalMaterialFlowProfile)(Definition.Values.materialFlowProfile?.Value
            ?? (int)LocalMaterialFlowProfile.ExogenousDaily);
        switch (profile)
        {
            case LocalMaterialFlowProfile.FiniteReserveDaily:
                return SimulateSelectedFiniteReserveDailyMaterialFlow(
                    locationId, absoluteDay, calendarVersion, logOutput);
            case LocalMaterialFlowProfile.MixedSourcesDaily:
                return SimulateSelectedMixedSourceDailyMaterialFlow(
                    absoluteDay, calendarVersion, logOutput);
            default:
                // Keep the selected runtime's prior fallback for an unrecognized
                // profile value; valid admitted profiles use the explicit cases.
                return SimulateSelectedExogenousDailyMaterialFlow(
                    locationId, absoluteDay, calendarVersion, logOutput);
        }
    }

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

    /// <summary>
    /// Executes the selected finite-reserve City source through the canonical
    /// Core item, Market, and reserve owners. The Unity City adapter only
    /// projects the resulting value and forwards log output.
    /// </summary>
    internal LocalDailyMaterialFlowResult SimulateSelectedFiniteReserveDailyMaterialFlow(
        string locationId,
        long absoluteDay,
        string calendarVersion,
        Action<SimulationLogCategory, string> logOutput)
    {
        DailyV1CityData city = Definition.Values;
        if ((LocalMaterialFlowProfile)(city.materialFlowProfile?.Value
                ?? (int)LocalMaterialFlowProfile.ExogenousDaily) != LocalMaterialFlowProfile.FiniteReserveDaily
            || city.productionConfigs == null || city.productionConfigs.Count != 1
            || city.productionConfigs[0] == null)
            throw new LocalDailyMaterialFlowRejectedException(
                "Selected finite-reserve material flow requires one authored source.");

        DailyV1CityProductionConfig source = city.productionConfigs[0];
        if (string.IsNullOrWhiteSpace(source.item)
            || !Definitions.Items.TryGetValue(source.item, out DailyV1ItemDefinition item)
            || item == null || FiniteSource == null || !ReferenceEquals(FiniteSource.Item, item)
            || !string.Equals(FiniteSource.ProductionSourceId, source.productionSourceId, StringComparison.Ordinal)
            || !string.Equals(FiniteSource.ContentRevision, source.contentRevision, StringComparison.Ordinal)
            || source.amountPerDay != FiniteSource.DailyOutputLimit
            || source.initialReserve != FiniteSource.InitialReserve
            || !string.Equals(FiniteSource.SettlementSemanticId, city.settlementSemanticId, StringComparison.Ordinal)
            || !string.Equals(FiniteSource.MarketStoreSemanticId, city.marketStoreSemanticId, StringComparison.Ordinal))
            throw new LocalDailyMaterialFlowRejectedException(
                "Finite source configuration must match its constructed Core owner.");

        DailyV1MarketItemConfig itemConfig = null;
        foreach (DailyV1MarketItemConfig candidate in city.marketItems ?? Array.Empty<DailyV1MarketItemConfig>())
        {
            if (candidate != null && string.Equals(candidate.item, item.DefinitionId, StringComparison.Ordinal))
            {
                itemConfig = candidate;
                break;
            }
        }
        if (itemConfig == null
            || (ConsumptionPaymentMode)(city.populationConsumption?.paymentMode?.Value
                ?? (int)ConsumptionPaymentMode.Free) != ConsumptionPaymentMode.Free
            || string.IsNullOrWhiteSpace(locationId))
            throw new LocalDailyMaterialFlowRejectedException(
                "Selected finite-reserve material flow requires its admitted item, location, and free population consumption.");

        int opening = Market.GetAmount(item);
        int applied = 0;
        string rejection = string.Empty;
        FiniteSourceSnapshot sourceSnapshot = FiniteSource.CaptureSnapshot();
        long expectedSourceRevision = sourceSnapshot.Revision;
        long expectedMarketRevision = Market.Revision;
        int requestedProduction = Math.Min(sourceSnapshot.DailyOutputLimit, sourceSnapshot.RemainingReserve);

        if (requestedProduction > 0 && expectedSourceRevision == long.MaxValue)
        {
            rejection = "SourceRevisionExhausted";
        }
        else if (requestedProduction > 0 && expectedMarketRevision == long.MaxValue)
        {
            rejection = "MarketRevisionExhausted";
        }
        else if (!Market.TryCreateFiniteSourceScenario(locationId, sourceSnapshot,
                     out FiniteSourceScenarioRuntime finiteRuntime,
                     out FiniteSourceScenarioSnapshot currentSnapshot,
                     out rejection))
        {
            // Preserve the production service's fail-closed rejection result;
            // the existing free-consumption stage still runs below.
        }
        else if (!finiteRuntime.TryPrepareAdvance(currentSnapshot, absoluteDay,
                     out PreparedFiniteSourceAdvance preparedAdvance, out rejection)
            || preparedAdvance.Quantity != requestedProduction)
        {
            if (string.IsNullOrEmpty(rejection)) rejection = "InvalidMarketInput";
        }
        else if (preparedAdvance.Status == FiniteSourceOperationStatus.Exhausted)
        {
            rejection = "ReserveExhausted";
        }
        else if (preparedAdvance.Status != FiniteSourceOperationStatus.Applied
            || !finiteRuntime.TryApplyPrepared(currentSnapshot, preparedAdvance,
                out FiniteSourceScenarioSnapshot nextSnapshot, out rejection)
            || !Market.TryPrepareFiniteStockIncrease(currentSnapshot, preparedAdvance, nextSnapshot,
                out DailyV1MarketPreparedState preparedMarket, out rejection))
        {
            if (string.IsNullOrEmpty(rejection)) rejection = "InvalidMarketInput";
        }
        else if (!FiniteSource.CanInstall(expectedSourceRevision, absoluteDay))
        {
            rejection = FiniteSource.Revision != expectedSourceRevision
                ? "StaleSourceRevision" : "SourceRevisionExhausted";
        }
        else if (!Market.CanInstallMaterialFlow(expectedMarketRevision, 1))
        {
            rejection = Market.Revision != expectedMarketRevision
                ? "StaleMarketRevision" : "MarketMutationRejected";
        }
        else if (!FiniteSource.CanInstall(expectedSourceRevision, absoluteDay))
        {
            rejection = FiniteSource.Revision != expectedSourceRevision
                ? "StaleSourceRevision" : "SourceRevisionExhausted";
        }
        else if (!Market.CanInstallMaterialFlow(expectedMarketRevision, 1))
        {
            // The legacy prepared-install path checks admission once while
            // preparing the paired Market replacement, once before returning
            // the preparation, and once again at install. Keep the final
            // recheck here so P12 mutation admission has the same boundary.
            rejection = Market.Revision != expectedMarketRevision
                ? "StaleMarketRevision" : "MarketMutationRejected";
        }
        else if (!FiniteSource.TryInstall(expectedSourceRevision, absoluteDay, preparedAdvance.Quantity))
        {
            rejection = FiniteSource.Revision != expectedSourceRevision
                ? "StaleSourceRevision" : "SourceRevisionExhausted";
        }
        else if (!Market.TryInstall(expectedMarketRevision, 1, preparedMarket))
        {
            // Both owners were preflighted above against this single-threaded
            // operation. This branch is retained as a fail-closed invariant.
            rejection = Market.Revision != expectedMarketRevision
                ? "StaleMarketRevision" : "MarketMutationRejected";
        }
        else
        {
            applied = preparedAdvance.Quantity;
            Market.NotifyMaterialFlowInstalled();
        }

        int requestedConsumption = Math.Max(0, UnityMathfCompatibility.RoundToInt(
            Population.CurrentPopulation / 1000f * itemConfig.consumptionPer1000Population));
        int actualConsumption = Market.RemoveStockUpTo(item, requestedConsumption);
        int closing = Market.GetAmount(item);
        LocalDailyMaterialFlowResult result = new LocalDailyMaterialFlowResult(
            city.settlementSemanticId,
            source.productionSourceId,
            city.marketStoreSemanticId,
            locationId,
            item.DefinitionId,
            source.contentRevision,
            "Economy.Enabled=true;PaymentMode=Free",
            "simulation-calendar",
            calendarVersion,
            true,
            Population.CurrentPopulation,
            itemConfig.consumptionPer1000Population,
            absoluteDay,
            opening,
            source.amountPerDay,
            applied,
            rejection,
            requestedConsumption,
            actualConsumption,
            closing);

        logOutput?.Invoke(SimulationLogCategory.EconomyProduction,
            city.cityName + " source " + source.productionSourceId + ": "
            + applied + "/" + source.amountPerDay);
        if (actualConsumption > 0)
            logOutput?.Invoke(SimulationLogCategory.EconomyConsumption,
                city.cityName + " consumed " + actualConsumption + " " + item.Values.itemName);
        return result;
    }

    /// <summary>
    /// Executes the admitted selected mixed-source City operation against the
    /// exact Core Market and finite-source owners. Production is installed as
    /// one prepared replacement before the existing free-consumption write.
    /// </summary>
    internal LocalDailyMaterialFlowResult SimulateSelectedMixedSourceDailyMaterialFlow(
        long absoluteDay,
        string calendarVersion,
        Action<SimulationLogCategory, string> logOutput)
    {
        DailyV1CityData city = Definition.Values;
        if (!DailyV1MixedSourceProfileAdmission.TryValidate(city, out string rejection))
            throw PrepareMixedSourceRejected(rejection);
        bool finiteConfigMatches = false;
        if (FiniteSource != null)
            foreach (DailyV1CityProductionConfig source in city.productionConfigs)
                if (source != null
                    && (CityProductionSourceKind)(source.sourceKind?.Value ?? 0)
                        == CityProductionSourceKind.FiniteReserveDaily
                    && ReferenceEquals(FiniteSource.Item, Definitions.Items.TryGetValue(
                        source.item, out DailyV1ItemDefinition item) ? item : null)
                    && string.Equals(source.productionSourceId, FiniteSource.ProductionSourceId, StringComparison.Ordinal)
                    && string.Equals(source.contentRevision, FiniteSource.ContentRevision, StringComparison.Ordinal)
                    && source.amountPerDay == FiniteSource.DailyOutputLimit
                    && source.initialReserve == FiniteSource.InitialReserve
                    && string.Equals(city.settlementSemanticId, FiniteSource.SettlementSemanticId, StringComparison.Ordinal)
                    && string.Equals(city.marketStoreSemanticId, FiniteSource.MarketStoreSemanticId, StringComparison.Ordinal))
                    finiteConfigMatches = true;
        if (!finiteConfigMatches)
            throw PrepareMixedSourceRejected("FiniteSourceOwnerMismatch");

        DailyV1FiniteProductionSourceOwner finite = FiniteSource;
        if (finite.RemainingReserve < 0 || finite.RemainingReserve > finite.InitialReserve
            || finite.DailyOutputLimit <= 0)
            throw PrepareMixedSourceRejected("InvalidFiniteSourceState");

        long marketRevision = Market.Revision;
        if (!Market.TryCreatePreparedSnapshot(out DailyV1MarketPreparedState marketState)
            || !Market.TryCountPreparedItems(marketState, finite.Item, out int matchingCount)
            || matchingCount != 1
            || !Market.TryGetPreparedItem(marketState, finite.Item, out DailyV1MarketItemState preparedItem)
            || preparedItem == null)
            throw PrepareMixedSourceRejected("MarketRowCardinality");

        int openingStock = preparedItem.Amount;
        long marketIncrements = 0;
        long totalApplied = 0;
        int finiteApplied = 0;
        List<CityDailyMaterialFlowSourceOutcome> outcomes = new List<CityDailyMaterialFlowSourceOutcome>(
            city.productionConfigs.Count);
        List<DailyV1CityProductionConfig> ordered = new List<DailyV1CityProductionConfig>(city.productionConfigs);
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.productionSourceId ?? string.Empty, right?.productionSourceId ?? string.Empty));

        foreach (DailyV1CityProductionConfig source in ordered)
        {
            CityProductionSourceKind kind = (CityProductionSourceKind)(source.sourceKind?.Value ?? 0);
            bool finiteSource = kind == CityProductionSourceKind.FiniteReserveDaily;
            int planned = finiteSource
                ? Math.Min(source.amountPerDay, finite.RemainingReserve)
                : source.amountPerDay;
            string sourceRejection = planned == 0 ? "ReserveExhausted" : string.Empty;
            int applied = 0;
            if (planned > 0)
            {
                if (!Market.TryApplyPreparedStockDelta(marketState, finite.Item, planned,
                        preparedItem.DesiredAmount, out DailyV1MarketPreparedState updatedState,
                        out DailyV1MarketItemState updatedItem))
                    sourceRejection = "AggregateStockOverflow";
                else
                {
                    marketState = updatedState;
                    preparedItem = updatedItem;
                    applied = planned;
                    marketIncrements++;
                    totalApplied += applied;
                    if (finiteSource) finiteApplied = applied;
                }
            }

            outcomes.Add(new CityDailyMaterialFlowSourceOutcome(
                source.productionSourceId, kind, source.amountPerDay, planned, applied, sourceRejection));
        }

        if (marketIncrements > 0 && !Market.CanInstallMaterialFlow(marketRevision, marketIncrements))
            throw PrepareMixedSourceRejected(Market.Revision == marketRevision
                ? "MarketMutationRejected" : "StaleMarketRevision");
        long finiteRevision = finite.Revision;
        if (finiteApplied > 0 && !finite.CanInstall(finiteRevision, absoluteDay))
            throw PrepareMixedSourceRejected(finite.Revision == long.MaxValue
                ? "SourceRevisionExhausted"
                : absoluteDay <= finite.LastProcessedDay ? "BoundaryAlreadyProcessed" : "StaleSourceRevision");

        if (marketIncrements > 0 && !Market.TryInstall(marketRevision, marketIncrements, marketState))
            throw new LocalDailyMaterialFlowRejectedException(
                "P14-C daily production became stale or was rejected before installation.");
        if (finiteApplied > 0 && !finite.TryInstall(finiteRevision, absoluteDay, finiteApplied))
            throw new LocalDailyMaterialFlowRejectedException(
                "P14-C daily production became stale or was rejected before installation.");
        if (marketIncrements > 0) Market.NotifyMaterialFlowInstalled();

        DailyV1CityProductionConfig representative = null;
        foreach (DailyV1CityProductionConfig row in ordered)
            if ((CityProductionSourceKind)(row.sourceKind?.Value ?? 0) == CityProductionSourceKind.ExogenousDaily)
            { representative = row; break; }
        DailyV1MarketItemConfig marketItem = city.marketItems[0];
        int requested = Math.Max(0, UnityMathfCompatibility.RoundToInt(
            Population.CurrentPopulation / 1000f * marketItem.consumptionPer1000Population));
        int actual = Market.RemoveStockUpTo(finite.Item, requested);
        int closingStock = Market.GetAmount(finite.Item);
        string[] rejected = outcomes
            .Where(outcome => !string.IsNullOrEmpty(outcome.RejectionReason)
                && outcome.RejectionReason != "ReserveExhausted")
            .Select(outcome => outcome.ProductionSourceId + ":" + outcome.RejectionReason)
            .ToArray();
        LocalDailyMaterialFlowResult result = new LocalDailyMaterialFlowResult(
            city.settlementSemanticId,
            representative?.productionSourceId ?? string.Empty,
            city.marketStoreSemanticId,
            city.materialFlowLocationId,
            finite.Item.DefinitionId,
            representative?.contentRevision ?? finite.ContentRevision,
            "Economy.Enabled=true;PaymentMode=Free",
            "simulation-calendar",
            calendarVersion,
            true,
            Population.CurrentPopulation,
            marketItem.consumptionPer1000Population,
            absoluteDay,
            openingStock,
            city.productionConfigs.Aggregate(0L, (total, source) => total + source.amountPerDay),
            checked((int)totalApplied),
            string.Join(";", rejected),
            requested,
            actual,
            closingStock,
            outcomes.AsReadOnly());

        foreach (CityDailyMaterialFlowSourceOutcome outcome in outcomes)
            if (outcome.AppliedQuantity > 0)
                logOutput?.Invoke(SimulationLogCategory.EconomyProduction,
                    city.cityName + " source " + outcome.ProductionSourceId + ": "
                    + outcome.AppliedQuantity + "/" + outcome.ConfiguredQuantity);
        if (actual > 0)
            logOutput?.Invoke(SimulationLogCategory.EconomyConsumption,
                city.cityName + " consumed " + actual + " " + finite.Item.Values.itemName);
        return result;
    }

    private static LocalDailyMaterialFlowRejectedException PrepareMixedSourceRejected(string reason) =>
        new LocalDailyMaterialFlowRejectedException(
            "P14-C daily production could not be prepared: " + (reason ?? string.Empty));
}

/// <summary>
/// Owns the ordered stable runtime identities for a selected City's NPC
/// presence relation and the P12 revision that records membership changes.
/// Unity keeps the exact object-reference projection needed by the existing
/// runtime and persistence adapters.
/// </summary>
internal sealed class DailyV1CityNpcPresenceOwner
{
    private readonly List<string> orderedNpcRuntimeIds;
    private readonly System.Collections.ObjectModel.ReadOnlyCollection<string> readOnlyNpcRuntimeIds;

    internal string CityRuntimeId { get; }
    internal System.Collections.Generic.IReadOnlyList<string> OrderedNpcRuntimeIds => readOnlyNpcRuntimeIds;
    internal long Revision { get; private set; }

    private DailyV1CityNpcPresenceOwner(string cityRuntimeId, List<string> orderedNpcRuntimeIds, long revision)
    {
        CityRuntimeId = cityRuntimeId;
        this.orderedNpcRuntimeIds = orderedNpcRuntimeIds;
        readOnlyNpcRuntimeIds = orderedNpcRuntimeIds.AsReadOnly();
        Revision = revision;
    }

    internal static bool TryCreateInitial(string cityRuntimeId, out DailyV1CityNpcPresenceOwner owner)
    {
        owner = null;
        if (string.IsNullOrWhiteSpace(cityRuntimeId)) return false;
        owner = new DailyV1CityNpcPresenceOwner(cityRuntimeId, new List<string>(), 0L);
        return true;
    }

    internal static bool TryCreateBound(
        string cityRuntimeId,
        System.Collections.Generic.IEnumerable<string> orderedIds,
        long revision,
        out DailyV1CityNpcPresenceOwner owner)
    {
        owner = null;
        if (string.IsNullOrWhiteSpace(cityRuntimeId) || orderedIds == null || revision < 0L) return false;

        List<string> candidate = new List<string>();
        System.Collections.Generic.HashSet<string> unique =
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (string npcRuntimeId in orderedIds)
        {
            if (string.IsNullOrWhiteSpace(npcRuntimeId) || !unique.Add(npcRuntimeId)) return false;
            candidate.Add(npcRuntimeId);
        }

        owner = new DailyV1CityNpcPresenceOwner(cityRuntimeId, candidate, revision);
        return true;
    }

    internal bool Contains(string npcRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId)) return false;
        return orderedNpcRuntimeIds.Contains(npcRuntimeId);
    }

    internal bool CanApplyRevisionIncrements(long increments) =>
        increments >= 0L && Revision <= long.MaxValue - increments;

    internal bool TryAdd(string npcRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId)) return false;
        if (orderedNpcRuntimeIds.Contains(npcRuntimeId)) return true;
        if (Revision == long.MaxValue) return false;
        orderedNpcRuntimeIds.Add(npcRuntimeId);
        Revision++;
        return true;
    }

    internal bool TryRemove(string npcRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId)) return false;
        int index = orderedNpcRuntimeIds.IndexOf(npcRuntimeId);
        if (index < 0) return true;
        if (Revision == long.MaxValue) return false;
        orderedNpcRuntimeIds.RemoveAt(index);
        Revision++;
        return true;
    }
}
