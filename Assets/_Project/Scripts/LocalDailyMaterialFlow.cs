using System;
using System.Collections.Generic;

/// <summary>Immutable outcome for one stable-ID daily production contributor.</summary>
[Serializable]
public sealed class CityDailyMaterialFlowSourceOutcome
{
    public string ProductionSourceId { get; }
    public CityProductionSourceKind Kind { get; }
    public int ConfiguredQuantity { get; }
    public int PlannedQuantity { get; }
    public int AppliedQuantity { get; }
    public string RejectionReason { get; }

    internal CityDailyMaterialFlowSourceOutcome(
        string productionSourceId,
        CityProductionSourceKind kind,
        int configuredQuantity,
        int plannedQuantity,
        int appliedQuantity,
        string rejectionReason)
    {
        ProductionSourceId = productionSourceId ?? string.Empty;
        Kind = kind;
        ConfiguredQuantity = configuredQuantity;
        PlannedQuantity = plannedQuantity;
        AppliedQuantity = appliedQuantity;
        RejectionReason = rejectionReason ?? string.Empty;
    }
}

/// <summary>Deterministic projection of one authored settlement's daily source and free sink.</summary>
[Serializable]
public sealed class LocalDailyMaterialFlowResult
{
    public string SettlementSemanticId { get; }
    public string ProductionSourceId { get; }
    public string MarketStoreSemanticId { get; }
    public string LocationId { get; }
    public string ItemDefinitionId { get; }
    public string ContentRevision { get; }
    public string EffectiveConfiguration { get; }
    public string CalendarIdentity { get; }
    public string CalendarVersion { get; }
    public bool EconomyEnabled { get; }
    public int PopulationCount { get; }
    public float ConsumptionPer1000Population { get; }
    public long AbsoluteDay { get; }
    public int OpeningStock { get; }
    public long ConfiguredSourceQuantity { get; }
    public int AppliedSourceQuantity { get; }
    public string SourceRejectionReason { get; }
    public int RequestedFreeConsumption { get; }
    public int ActualFreeConsumption { get; }
    public int ClosingStock { get; }
    public IReadOnlyList<CityDailyMaterialFlowSourceOutcome> SourceOutcomes { get; }
    public string TitleOwnerSemanticId => SettlementSemanticId;
    public string StockCustodianSemanticId => MarketStoreSemanticId;

    public LocalDailyMaterialFlowResult(string settlementId, string sourceId, string storeId, string locationId,
        string itemId, string contentRevision, string effectiveConfiguration, string calendarIdentity,
        string calendarVersion, bool economyEnabled, int populationCount, float consumptionPer1000Population,
        long day, int opening, long configuredSource, int appliedSource, string rejection, int requested, int actual, int closing,
        IReadOnlyList<CityDailyMaterialFlowSourceOutcome> sourceOutcomes = null)
    {
        SettlementSemanticId = settlementId;
        ProductionSourceId = sourceId;
        MarketStoreSemanticId = storeId;
        LocationId = locationId;
        ItemDefinitionId = itemId;
        ContentRevision = contentRevision;
        EffectiveConfiguration = effectiveConfiguration;
        CalendarIdentity = calendarIdentity;
        CalendarVersion = calendarVersion;
        EconomyEnabled = economyEnabled;
        PopulationCount = populationCount;
        ConsumptionPer1000Population = consumptionPer1000Population;
        AbsoluteDay = day;
        OpeningStock = opening;
        ConfiguredSourceQuantity = configuredSource;
        AppliedSourceQuantity = appliedSource;
        SourceRejectionReason = rejection ?? string.Empty;
        RequestedFreeConsumption = requested;
        ActualFreeConsumption = actual;
        ClosingStock = closing;
        SourceOutcomes = sourceOutcomes ?? Array.Empty<CityDailyMaterialFlowSourceOutcome>();
    }
}

public sealed class LocalDailyMaterialFlowRejectedException : InvalidOperationException
{
    public LocalDailyMaterialFlowRejectedException(string message) : base(message) { }
}
