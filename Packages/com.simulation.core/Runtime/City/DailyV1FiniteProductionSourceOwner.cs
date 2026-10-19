using System;
using System.Collections.Generic;

/// <summary>
/// Core authority for one selected City's finite production reserve. The Unity
/// runtime may expose a compatibility view, but reserve, revision and processed
/// day are owned here and keyed by the admitted definition graph.
/// </summary>
internal sealed class DailyV1FiniteProductionSourceOwner
{
    internal string ProductionSourceId { get; }
    internal string SettlementSemanticId { get; }
    internal string MarketStoreSemanticId { get; }
    internal DailyV1ItemDefinition Item { get; }
    internal string ContentRevision { get; }
    internal int DailyOutputLimit { get; }
    internal int InitialReserve { get; }
    internal int RemainingReserve { get; private set; }
    internal long Revision { get; private set; }
    internal long LastProcessedDay { get; private set; } = long.MinValue;

    private DailyV1FiniteProductionSourceOwner(
        DailyV1CityProductionConfig source,
        DailyV1ItemDefinition item,
        string settlementSemanticId,
        string marketStoreSemanticId)
    {
        ProductionSourceId = source.productionSourceId;
        SettlementSemanticId = settlementSemanticId;
        MarketStoreSemanticId = marketStoreSemanticId;
        Item = item;
        ContentRevision = source.contentRevision;
        DailyOutputLimit = source.amountPerDay;
        InitialReserve = source.initialReserve;
        RemainingReserve = source.initialReserve;
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition city,
        out DailyV1FiniteProductionSourceOwner owner)
    {
        owner = null;
        if (definitions == null || city == null
            || !definitions.Cities.TryGetValue(city.DefinitionId, out DailyV1CityDefinition admittedCity)
            || !ReferenceEquals(city, admittedCity)) return false;

        DailyV1CityData values = city.Values;
        LocalMaterialFlowProfile profile = (LocalMaterialFlowProfile)(values.materialFlowProfile?.Value
            ?? (int)LocalMaterialFlowProfile.ExogenousDaily);
        if (profile != LocalMaterialFlowProfile.FiniteReserveDaily
            && profile != LocalMaterialFlowProfile.MixedSourcesDaily)
            return true;

        if (string.IsNullOrWhiteSpace(values.settlementSemanticId)
            || string.IsNullOrWhiteSpace(values.marketStoreSemanticId)
            || values.productionConfigs == null) return false;

        DailyV1CityProductionConfig source = null;
        if (profile == LocalMaterialFlowProfile.MixedSourcesDaily)
        {
            if (!DailyV1MixedSourceProfileAdmission.TryValidate(values, out _)) return false;
            foreach (DailyV1CityProductionConfig row in values.productionConfigs)
                if (row != null && (CityProductionSourceKind)(row.sourceKind?.Value ?? 0)
                        == CityProductionSourceKind.FiniteReserveDaily)
                {
                    if (source != null) return false;
                    source = row;
                }
        }
        else
        {
            if (values.productionConfigs.Count != 1) return false;
            source = values.productionConfigs[0];
        }

        if (source == null || string.IsNullOrWhiteSpace(source.item)
            || !definitions.Items.TryGetValue(source.item, out DailyV1ItemDefinition item)
            || item == null || string.IsNullOrWhiteSpace(source.productionSourceId)
            || string.IsNullOrWhiteSpace(source.contentRevision) || source.amountPerDay <= 0
            || source.initialReserve < 0) return false;

        owner = new DailyV1FiniteProductionSourceOwner(
            source, item, values.settlementSemanticId, values.marketStoreSemanticId);
        return true;
    }

    internal FiniteSourceSnapshot CaptureSnapshot() => new FiniteSourceSnapshot(
        ProductionSourceId,
        SettlementSemanticId,
        MarketStoreSemanticId,
        Item.DefinitionId,
        ContentRevision,
        DailyOutputLimit,
        InitialReserve,
        RemainingReserve,
        Revision,
        LastProcessedDay);

    internal bool CanInstall(long expectedRevision, long day) =>
        expectedRevision == Revision && Revision < long.MaxValue && day > LastProcessedDay;

    internal bool TryInstall(long expectedRevision, long day, int produced)
    {
        if (!CanInstall(expectedRevision, day) || produced <= 0 || produced > RemainingReserve
            || produced > DailyOutputLimit) return false;
        RemainingReserve -= produced;
        Revision = expectedRevision + 1;
        LastProcessedDay = day;
        return true;
    }
}
