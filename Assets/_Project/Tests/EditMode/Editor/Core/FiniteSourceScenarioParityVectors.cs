using System.Collections.Generic;

/// <summary>Shared fixed P14-B operation inputs for the .NET Lab and Unity parity proof.</summary>
public static class FiniteSourceScenarioParityVectors
{
    public const string LocationId = "location.p14b-lab";
    public const string ProductionSourceId = "source.p14b-lab";
    public const string SettlementId = "settlement.p14b-lab";
    public const string MarketStoreId = "market.p14b-lab";
    public const string ItemId = "item.p14b-lab";
    public const string ContentRevision = "p14b-fixture-v1";

    public static FiniteSourceScenarioDefinition CreateDefinition(
        out FiniteSourceScenarioSnapshot initialSnapshot)
    {
        FiniteSourceSnapshot source = new FiniteSourceSnapshot(
            ProductionSourceId, SettlementId, MarketStoreId, ItemId, ContentRevision,
            dailyOutputLimit: 3, initialReserve: 5, remainingReserve: 5,
            revision: 0, lastProcessedDay: long.MinValue);
        List<MarketStockRow> rows = new List<MarketStockRow>
        {
            MarketStockRow.Item(new MarketItemDefinition(ItemId, 2f), 2, 8,
                MarketPriceCalculator.CalculateCurrentPrice(2f, 8, 2))
        };
        if (!FiniteSourceScenarioDefinition.TryCreate(LocationId, source, 0, rows,
                out FiniteSourceScenarioDefinition definition, out string rejection))
            throw new System.InvalidOperationException("Shared P14-B fixture is invalid: " + rejection);
        initialSnapshot = new FiniteSourceScenarioSnapshot(source, 0, rows);
        return definition;
    }

    public static long[] AdmittedDays => new long[] { 1, 2, 3 };
}
