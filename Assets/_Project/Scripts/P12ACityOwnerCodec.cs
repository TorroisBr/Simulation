using System;
using System.Collections.Generic;

/// <summary>
/// Unity projection adapter for the Core-owned P12-D City image codec.
/// CityRuntime capture and projection remain host responsibilities.
/// </summary>
internal static class P12ACityOwnerCodec
{
    internal static bool TryEncode(P12DCityRootOwnerSnapshot snapshot,
        out IReadOnlyList<P12AOwnerSection> sections, out P12AResult result)
        => P12DCityOwnerImageSectionCodec.TryEncode(ToCoreValue(snapshot), out sections, out result);

    internal static bool TryDecode(P12AOwnerSection presence, P12AOwnerSection market,
        P12AOwnerSection population, P12AOwnerSection receipts,
        out P12DCityRootOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        if (!P12DCityOwnerImageSectionCodec.TryDecode(presence, market, population, receipts,
                out DailyV1P12DCityCoreOwnerHydrationInput value, out result))
            return false;

        snapshot = FromCoreValue(value);
        result = null;
        return snapshot != null;
    }

    internal static string EncodeCityKeyForSection(string cityRuntimeId)
        => P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(cityRuntimeId);

    internal static P12DCityRootOwnerSnapshot FromCoreValue(
        DailyV1P12DCityCoreOwnerHydrationInput value)
    {
        if (value == null) return null;
        return new P12DCityRootOwnerSnapshot(
            value.CityRuntimeId,
            value.CityDefinitionId,
            value.LegacyLocationRuntimeId,
            value.ImportantNpcRevision,
            value.ImportantNpcRuntimeIds,
            value.MarketCounterpartyRuntimeId,
            value.MarketLiquidityMode,
            value.MarketRevision,
            value.MarketItems,
            value.PopulationEconomicRuntimeId,
            value.PopulationPaymentMode,
            value.PopulationRuntimeId,
            value.CurrentPopulation,
            value.PopulationRevision,
            value.PopulationReceiptRevision,
            value.PopulationReceipts);
    }

    private static DailyV1P12DCityCoreOwnerHydrationInput ToCoreValue(
        P12DCityRootOwnerSnapshot snapshot)
    {
        if (snapshot == null) return null;
        return new DailyV1P12DCityCoreOwnerHydrationInput(
            snapshot.CityRuntimeId,
            snapshot.CityDefinitionId,
            snapshot.LegacyLocationRuntimeId,
            snapshot.LegacyLocationRuntimeId,
            snapshot.ImportantNpcRevision,
            snapshot.ImportantNpcRuntimeIds,
            snapshot.MarketCounterpartyRuntimeId,
            snapshot.MarketLiquidityMode,
            snapshot.MarketCounterpartyHasAccount,
            snapshot.MarketRevision,
            snapshot.MarketItems,
            snapshot.PopulationEconomicRuntimeId,
            snapshot.PopulationPaymentMode,
            snapshot.PopulationEconomyHasAccount,
            snapshot.PopulationRuntimeId,
            snapshot.CurrentPopulation,
            snapshot.PopulationRevision,
            snapshot.PopulationReceiptRevision,
            snapshot.PopulationReceipts,
            snapshot.SchemaVersion);
    }
}
