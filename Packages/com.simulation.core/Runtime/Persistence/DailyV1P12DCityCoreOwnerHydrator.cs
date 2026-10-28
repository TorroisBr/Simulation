using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Detached values required to hydrate the selected P12-D City owner vector.
/// The Unity adapter resolves host objects to stable location IDs, then Core
/// resolves canonical definition nodes and creates the sole live owners.
/// </summary>
internal sealed class DailyV1P12DCityCoreOwnerHydrationInput
{
    internal int SchemaVersion { get; }
    internal string CityRuntimeId { get; }
    internal string CityDefinitionId { get; }
    internal string LegacyLocationRuntimeId { get; }
    internal string ResolvedLocationRuntimeId { get; }
    internal long ImportantNpcRevision { get; }
    internal IReadOnlyList<string> ImportantNpcRuntimeIds { get; }
    internal string MarketCounterpartyRuntimeId { get; }
    internal MarketLiquidityMode MarketLiquidityMode { get; }
    internal bool MarketCounterpartyHasAccount { get; }
    internal long MarketRevision { get; }
    internal IReadOnlyList<P12DCityMarketItemSnapshot> MarketItems { get; }
    internal string PopulationEconomicRuntimeId { get; }
    internal ConsumptionPaymentMode PopulationPaymentMode { get; }
    internal bool PopulationEconomyHasAccount { get; }
    internal string PopulationRuntimeId { get; }
    internal int CurrentPopulation { get; }
    internal long PopulationRevision { get; }
    internal long PopulationReceiptRevision { get; }
    internal IReadOnlyList<PopulationOperationReceiptSnapshot> PopulationReceipts { get; }

    internal DailyV1P12DCityCoreOwnerHydrationInput(
        string cityRuntimeId,
        string cityDefinitionId,
        string legacyLocationRuntimeId,
        string resolvedLocationRuntimeId,
        long importantNpcRevision,
        IEnumerable<string> importantNpcRuntimeIds,
        string marketCounterpartyRuntimeId,
        MarketLiquidityMode marketLiquidityMode,
        bool marketCounterpartyHasAccount,
        long marketRevision,
        IEnumerable<P12DCityMarketItemSnapshot> marketItems,
        string populationEconomicRuntimeId,
        ConsumptionPaymentMode populationPaymentMode,
        bool populationEconomyHasAccount,
        string populationRuntimeId,
        int currentPopulation,
        long populationRevision,
        long populationReceiptRevision,
        IEnumerable<PopulationOperationReceiptSnapshot> populationReceipts,
        int schemaVersion = 1)
    {
        SchemaVersion = schemaVersion;
        CityRuntimeId = cityRuntimeId;
        CityDefinitionId = cityDefinitionId;
        LegacyLocationRuntimeId = legacyLocationRuntimeId;
        ResolvedLocationRuntimeId = resolvedLocationRuntimeId;
        ImportantNpcRevision = importantNpcRevision;
        ImportantNpcRuntimeIds = Copy(importantNpcRuntimeIds);
        MarketCounterpartyRuntimeId = marketCounterpartyRuntimeId;
        MarketLiquidityMode = marketLiquidityMode;
        MarketCounterpartyHasAccount = marketCounterpartyHasAccount;
        MarketRevision = marketRevision;
        MarketItems = Copy(marketItems);
        PopulationEconomicRuntimeId = populationEconomicRuntimeId;
        PopulationPaymentMode = populationPaymentMode;
        PopulationEconomyHasAccount = populationEconomyHasAccount;
        PopulationRuntimeId = populationRuntimeId;
        CurrentPopulation = currentPopulation;
        PopulationRevision = populationRevision;
        PopulationReceiptRevision = populationReceiptRevision;
        PopulationReceipts = Copy(populationReceipts);
    }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source) => source == null
        ? null
        : new ReadOnlyCollection<T>(new List<T>(source));
}

internal enum DailyV1P12DCityCoreOwnerHydrationFailure
{
    None = 0,
    InvalidInput,
    MissingCityDefinition,
    MissingItemDefinition,
    InvalidMarketItem,
    InvalidOwnerValue
}

/// <summary>
/// Resolves selected P12-D City row identities against the admitted Core graph
/// and creates the exact owner bundle later projected by the Unity adapter.
/// This method stages privately and publishes no runtime or owner collection.
/// </summary>
internal static class DailyV1P12DCityCoreOwnerHydrator
{
    internal static bool TryCreate(
        DailyV1P12DCityCoreOwnerHydrationInput input,
        DailyV1EffectiveDefinitions definitions,
        out DailyV1CityRuntimeOwners owners,
        out DailyV1P12DCityCoreOwnerHydrationFailure failure)
    {
        owners = null;
        failure = DailyV1P12DCityCoreOwnerHydrationFailure.InvalidInput;
        if (input == null || definitions == null || input.SchemaVersion != P12DCityOwnerImageSectionCodec.SchemaVersion
            || input.ImportantNpcRuntimeIds == null
            || input.MarketItems == null
            || input.PopulationReceipts == null
            || string.IsNullOrWhiteSpace(input.CityRuntimeId)
            || string.IsNullOrWhiteSpace(input.CityDefinitionId)
            || string.IsNullOrWhiteSpace(input.LegacyLocationRuntimeId)
            || !string.Equals(input.LegacyLocationRuntimeId,
                input.ResolvedLocationRuntimeId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(input.ResolvedLocationRuntimeId)
            || input.ImportantNpcRevision < 0L
            || !string.Equals(input.MarketCounterpartyRuntimeId,
                input.CityRuntimeId, StringComparison.Ordinal)
            || input.MarketLiquidityMode != MarketLiquidityMode.Open
            || input.MarketCounterpartyHasAccount
            || input.MarketRevision < 0L
            || !string.Equals(input.PopulationRuntimeId,
                input.CityRuntimeId, StringComparison.Ordinal)
            || !string.Equals(input.PopulationEconomicRuntimeId,
                "population-" + input.CityRuntimeId, StringComparison.Ordinal)
            || input.PopulationPaymentMode != ConsumptionPaymentMode.Free
            || input.PopulationEconomyHasAccount
            || input.CurrentPopulation < 0
            || input.PopulationRevision < 0L
            || input.PopulationReceiptRevision < 0L
            || input.PopulationReceiptRevision < input.PopulationReceipts.Count)
            return false;

        if (!definitions.Cities.TryGetValue(input.CityDefinitionId,
                out DailyV1CityDefinition cityDefinition)
            || cityDefinition == null
            || !string.Equals(cityDefinition.DefinitionId,
                input.CityDefinitionId, StringComparison.Ordinal))
        {
            failure = DailyV1P12DCityCoreOwnerHydrationFailure.MissingCityDefinition;
            return false;
        }

        List<DailyV1MarketItemState> marketRows =
            new List<DailyV1MarketItemState>(input.MarketItems.Count);
        foreach (P12DCityMarketItemSnapshot row in input.MarketItems)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ItemDefinitionId))
            {
                failure = DailyV1P12DCityCoreOwnerHydrationFailure.MissingItemDefinition;
                return false;
            }
            if (!definitions.Items.TryGetValue(row.ItemDefinitionId,
                    out DailyV1ItemDefinition itemDefinition)
                || itemDefinition == null
                || !string.Equals(itemDefinition.DefinitionId,
                    row.ItemDefinitionId, StringComparison.Ordinal))
            {
                failure = DailyV1P12DCityCoreOwnerHydrationFailure.MissingItemDefinition;
                return false;
            }
            if (!DailyV1MarketItemState.TryCreate(itemDefinition,
                    row.Amount, row.DesiredAmount, row.CurrentPrice,
                    out DailyV1MarketItemState marketRow))
            {
                failure = DailyV1P12DCityCoreOwnerHydrationFailure.InvalidMarketItem;
                return false;
            }
            marketRows.Add(marketRow);
        }

        if (!DailyV1CityRuntimeOwners.TryCreateRestoredP12DOwners(
                input.CityRuntimeId,
                input.ResolvedLocationRuntimeId,
                input.PopulationRuntimeId,
                input.PopulationEconomicRuntimeId,
                definitions,
                cityDefinition,
                input.CurrentPopulation,
                input.PopulationRevision,
                input.PopulationReceipts,
                input.PopulationReceiptRevision,
                marketRows,
                input.MarketRevision,
                input.ImportantNpcRuntimeIds,
                input.ImportantNpcRevision,
                out owners))
        {
            owners = null;
            failure = DailyV1P12DCityCoreOwnerHydrationFailure.InvalidOwnerValue;
            return false;
        }

        failure = DailyV1P12DCityCoreOwnerHydrationFailure.None;
        return true;
    }
}
