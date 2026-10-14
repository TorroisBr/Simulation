using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class FiniteSourceScenarioRuntimeTests
{
    [Test]
    public void FixedP14BScenarioAdvancesAndExhaustsWithExactSharedParityValues()
    {
        FiniteSourceScenarioDefinition definition = FiniteSourceScenarioParityVectors.CreateDefinition(
            out FiniteSourceScenarioSnapshot snapshot);
        Assert.That(FiniteSourceScenarioRuntime.TryCreate(definition, snapshot,
            out FiniteSourceScenarioRuntime runtime, out string rejection), Is.True, rejection);

        long[] days = FiniteSourceScenarioParityVectors.AdmittedDays;
        int[] expectedReserve = { 2, 0, 0 };
        int[] expectedStock = { 5, 7, 7 };
        long[] expectedSourceRevision = { 1, 2, 2 };
        long[] expectedMarketRevision = { 1, 2, 2 };
        FiniteSourceOperationStatus[] expectedStatus =
        {
            FiniteSourceOperationStatus.Applied,
            FiniteSourceOperationStatus.Applied,
            FiniteSourceOperationStatus.Exhausted
        };

        for (int i = 0; i < days.Length; i++)
        {
            Assert.That(runtime.TryPrepareAdvance(snapshot, days[i], out PreparedFiniteSourceAdvance prepared,
                out rejection), Is.True, rejection);
            Assert.That(prepared.Status, Is.EqualTo(expectedStatus[i]));
            Assert.That(runtime.TryApplyPrepared(snapshot, prepared,
                out FiniteSourceScenarioSnapshot next, out rejection), Is.True, rejection);
            Assert.That(next.Source.RemainingReserve, Is.EqualTo(expectedReserve[i]));
            Assert.That(next.Source.Revision, Is.EqualTo(expectedSourceRevision[i]));
            Assert.That(next.MarketRevision, Is.EqualTo(expectedMarketRevision[i]));
            Assert.That(next.MarketRows[0].Amount, Is.EqualTo(expectedStock[i]));
            Assert.That(FloatBits(next.MarketRows[0].CurrentPrice), Is.EqualTo(FloatBits(
                MarketPriceCalculator.CalculateCurrentPrice(2f, 8, expectedStock[i]))));
            Assert.That(next.Source.LastProcessedDay, Is.EqualTo(i < 2 ? days[i] : days[1]));
            snapshot = next;
        }
    }

    [Test]
    public void RecreatedScenarioUsesCapturedRemainingReserveAsItsStockDeltaBaseline()
    {
        FiniteSourceScenarioParityVectors.CreateDefinition(out FiniteSourceScenarioSnapshot fixture);
        FiniteSourceSnapshot afterPriorProduction = new FiniteSourceSnapshot(
            fixture.Source.ProductionSourceId, fixture.Source.SettlementSemanticId,
            fixture.Source.MarketStoreSemanticId, fixture.Source.ItemDefinitionId,
            fixture.Source.ContentRevision, fixture.Source.DailyOutputLimit,
            fixture.Source.InitialReserve, 2, 1, 1);
        List<MarketStockRow> currentRows = new List<MarketStockRow>
        {
            MarketStockRow.Item(new MarketItemDefinition(FiniteSourceScenarioParityVectors.ItemId, 2f),
                5, 8, MarketPriceCalculator.CalculateCurrentPrice(2f, 8, 5))
        };
        Assert.That(FiniteSourceScenarioDefinition.TryCreate(FiniteSourceScenarioParityVectors.LocationId,
            afterPriorProduction, 1, currentRows,
            out FiniteSourceScenarioDefinition definition, out string rejection), Is.True, rejection);
        FiniteSourceScenarioSnapshot snapshot = new FiniteSourceScenarioSnapshot(
            afterPriorProduction, 1, currentRows);
        Assert.That(FiniteSourceScenarioRuntime.TryCreate(definition, snapshot,
            out FiniteSourceScenarioRuntime runtime, out rejection), Is.True, rejection);

        Assert.That(runtime.TryPrepareAdvance(snapshot, 2,
            out PreparedFiniteSourceAdvance prepared, out rejection), Is.True, rejection);
        Assert.That(prepared.Status, Is.EqualTo(FiniteSourceOperationStatus.Applied));
        Assert.That(prepared.Quantity, Is.EqualTo(2));
        Assert.That(runtime.TryApplyPrepared(snapshot, prepared,
            out FiniteSourceScenarioSnapshot next, out rejection), Is.True, rejection);
        Assert.That(next.Source.RemainingReserve, Is.Zero);
        Assert.That(next.Source.Revision, Is.EqualTo(2));
        Assert.That(next.MarketRevision, Is.EqualTo(2));
        Assert.That(next.MarketRows[0].Amount, Is.EqualTo(7));
        Assert.That(FloatBits(next.MarketRows[0].CurrentPrice), Is.EqualTo(FloatBits(
            MarketPriceCalculator.CalculateCurrentPrice(2f, 8, 7))));
    }

    [Test]
    public void DefinitionRejectsInvalidIdentityAndAmbiguousTargetRows()
    {
        FiniteSourceScenarioParityVectors.CreateDefinition(out FiniteSourceScenarioSnapshot snapshot);
        FiniteSourceSnapshot source = snapshot.Source;
        Assert.That(FiniteSourceScenarioDefinition.TryCreate(" ", source, 0,
            snapshot.MarketRows, out _, out _), Is.False);
        Assert.That(FiniteSourceScenarioDefinition.TryCreate(FiniteSourceScenarioParityVectors.LocationId,
            source, 0, new List<MarketStockRow>(), out _, out string missing), Is.False);
        Assert.That(missing, Is.EqualTo("MarketRowCardinality"));

        List<MarketStockRow> duplicates = new List<MarketStockRow>(snapshot.MarketRows)
        {
            snapshot.MarketRows[0]
        };
        Assert.That(FiniteSourceScenarioDefinition.TryCreate(FiniteSourceScenarioParityVectors.LocationId,
            source, 0, duplicates, out _, out string duplicate), Is.False);
        Assert.That(duplicate, Is.EqualTo("MarketRowCardinality"));

        FiniteSourceScenarioDefinition valid = FiniteSourceScenarioParityVectors.CreateDefinition(
            out FiniteSourceScenarioSnapshot validInitial);
        FiniteSourceSnapshot mismatchedInitialSource = new FiniteSourceSnapshot(
            source.ProductionSourceId, source.SettlementSemanticId, source.MarketStoreSemanticId,
            source.ItemDefinitionId, source.ContentRevision, source.DailyOutputLimit,
            source.InitialReserve, source.RemainingReserve - 1, source.Revision, source.LastProcessedDay);
        FiniteSourceScenarioSnapshot mismatchedInitial = new FiniteSourceScenarioSnapshot(
            mismatchedInitialSource, validInitial.MarketRevision, validInitial.MarketRows);
        Assert.That(FiniteSourceScenarioRuntime.TryCreate(valid, mismatchedInitial,
            out _, out string initialMismatch), Is.False);
        Assert.That(initialMismatch, Is.EqualTo("InvalidInitialSnapshot"));
    }

    [Test]
    public void StaleDuplicateAndOlderDaysRejectWithoutChangingTheSnapshot()
    {
        FiniteSourceScenarioDefinition definition = FiniteSourceScenarioParityVectors.CreateDefinition(
            out FiniteSourceScenarioSnapshot snapshot);
        Assert.That(FiniteSourceScenarioRuntime.TryCreate(definition, snapshot,
            out FiniteSourceScenarioRuntime runtime, out _), Is.True);
        Assert.That(runtime.TryPrepareAdvance(snapshot, 1, out PreparedFiniteSourceAdvance prepared, out _), Is.True);
        Assert.That(runtime.TryApplyPrepared(snapshot, prepared, out FiniteSourceScenarioSnapshot next, out _), Is.True);

        Assert.That(runtime.TryPrepareAdvance(next, 1, out _, out string duplicate), Is.False);
        Assert.That(duplicate, Is.EqualTo("BoundaryAlreadyProcessed"));
        Assert.That(runtime.TryPrepareAdvance(next, 0, out _, out string older), Is.False);
        Assert.That(older, Is.EqualTo("BoundaryAlreadyProcessed"));
        Assert.That(runtime.TryApplyPrepared(next, prepared, out _, out string stale), Is.False);
        Assert.That(stale, Is.EqualTo("InvalidPreparedAdvance"));
        Assert.That(snapshot.Source.RemainingReserve, Is.EqualTo(5));
        Assert.That(snapshot.MarketRows[0].Amount, Is.EqualTo(2));
    }

    [Test]
    public void PreparedAdvanceCannotCrossRuntimeAndSnapshotsAreDetachedAndReadOnly()
    {
        FiniteSourceScenarioDefinition definition = FiniteSourceScenarioParityVectors.CreateDefinition(
            out FiniteSourceScenarioSnapshot snapshot);
        Assert.That(FiniteSourceScenarioRuntime.TryCreate(definition, snapshot,
            out FiniteSourceScenarioRuntime first, out _), Is.True);
        Assert.That(FiniteSourceScenarioRuntime.TryCreate(definition, snapshot,
            out FiniteSourceScenarioRuntime second, out _), Is.True);
        Assert.That(first.TryPrepareAdvance(snapshot, 1, out PreparedFiniteSourceAdvance prepared, out _), Is.True);
        Assert.That(second.TryApplyPrepared(snapshot, prepared, out _, out _), Is.False);

        List<MarketStockRow> mutableRows = new List<MarketStockRow>(snapshot.MarketRows);
        FiniteSourceScenarioSnapshot detached = new FiniteSourceScenarioSnapshot(snapshot.Source,
            snapshot.MarketRevision, mutableRows);
        mutableRows.Clear();
        Assert.That(detached.MarketRows.Count, Is.EqualTo(1));
        Assert.Throws<NotSupportedException>(() => ((IList<MarketStockRow>)detached.MarketRows).Add(
            MarketStockRow.NullRow()));
        Assert.That(first.ReadSnapshot(detached), Is.SameAs(detached));
    }

    [Test]
    public void InvalidCurrentOwnerStateAndRevisionOverflowAreRejectedAtomically()
    {
        FiniteSourceScenarioDefinition definition = FiniteSourceScenarioParityVectors.CreateDefinition(
            out FiniteSourceScenarioSnapshot snapshot);
        Assert.That(FiniteSourceScenarioRuntime.TryCreate(definition, snapshot,
            out FiniteSourceScenarioRuntime runtime, out _), Is.True);
        FiniteSourceSnapshot otherSource = new FiniteSourceSnapshot(
            snapshot.Source.ProductionSourceId, snapshot.Source.SettlementSemanticId,
            snapshot.Source.MarketStoreSemanticId, snapshot.Source.ItemDefinitionId,
            snapshot.Source.ContentRevision, snapshot.Source.DailyOutputLimit,
            snapshot.Source.InitialReserve, snapshot.Source.RemainingReserve,
            snapshot.Source.Revision, snapshot.Source.LastProcessedDay);
        FiniteSourceScenarioSnapshot changedRows = new FiniteSourceScenarioSnapshot(otherSource, 0,
            new[] { MarketStockRow.Item(new MarketItemDefinition(FiniteSourceScenarioParityVectors.ItemId,
                3f), 2, 8, 2f) });
        Assert.That(runtime.TryPrepareAdvance(changedRows, 1, out _, out string invalid), Is.False);
        Assert.That(invalid, Is.EqualTo("InvalidCurrentSnapshot"));

        FiniteSourceSnapshot maxRevision = new FiniteSourceSnapshot(
            snapshot.Source.ProductionSourceId, snapshot.Source.SettlementSemanticId,
            snapshot.Source.MarketStoreSemanticId, snapshot.Source.ItemDefinitionId,
            snapshot.Source.ContentRevision, snapshot.Source.DailyOutputLimit,
            snapshot.Source.InitialReserve, snapshot.Source.RemainingReserve,
            long.MaxValue, snapshot.Source.LastProcessedDay);
        FiniteSourceScenarioSnapshot exhaustedRevision = new FiniteSourceScenarioSnapshot(maxRevision,
            long.MaxValue, snapshot.MarketRows);
        Assert.That(runtime.TryPrepareAdvance(exhaustedRevision, 1, out _, out string overflow), Is.False);
        Assert.That(overflow, Is.EqualTo("SourceRevisionExhausted"));
        Assert.That(snapshot.Source.RemainingReserve, Is.EqualTo(5));
        Assert.That(snapshot.MarketRows[0].Amount, Is.EqualTo(2));
    }

    [Test]
    public void OrderedMarketRowsRemainStableAndStockOverflowDoesNotProduceANextSnapshot()
    {
        FiniteSourceScenarioParityVectors.CreateDefinition(out FiniteSourceScenarioSnapshot fixture);
        List<MarketStockRow> rows = new List<MarketStockRow>
        {
            MarketStockRow.NullRow(),
            MarketStockRow.NullItem(-2, 0, float.NaN),
            MarketStockRow.Item(new MarketItemDefinition("item.other", 7f), 3, 20, 9f),
            MarketStockRow.Item(new MarketItemDefinition(FiniteSourceScenarioParityVectors.ItemId, 2f),
                int.MaxValue - 2, 8, MarketPriceCalculator.CalculateCurrentPrice(2f, 8, int.MaxValue - 2))
        };
        FiniteSourceSnapshot source = new FiniteSourceSnapshot(
            fixture.Source.ProductionSourceId, fixture.Source.SettlementSemanticId,
            fixture.Source.MarketStoreSemanticId, fixture.Source.ItemDefinitionId,
            fixture.Source.ContentRevision, 3, 5, 5, 0, long.MinValue);
        Assert.That(FiniteSourceScenarioDefinition.TryCreate(FiniteSourceScenarioParityVectors.LocationId,
            source, 0, rows, out FiniteSourceScenarioDefinition definition, out string rejection), Is.True, rejection);
        FiniteSourceScenarioSnapshot snapshot = new FiniteSourceScenarioSnapshot(source, 0, rows);
        Assert.That(FiniteSourceScenarioRuntime.TryCreate(definition, snapshot,
            out FiniteSourceScenarioRuntime runtime, out rejection), Is.True, rejection);
        Assert.That(runtime.TryPrepareAdvance(snapshot, 1, out _, out string overflow), Is.False);
        Assert.That(overflow, Is.EqualTo("MarketStockOverflow"));
        Assert.That(snapshot.MarketRows[0].Kind, Is.EqualTo(MarketStockRowKind.NullRow));
        Assert.That(snapshot.MarketRows[1].Amount, Is.EqualTo(-2));
        Assert.That(snapshot.MarketRows[2].ItemDefinition.DefinitionId, Is.EqualTo("item.other"));
        Assert.That(snapshot.MarketRows[2].Amount, Is.EqualTo(3));
        Assert.That(snapshot.MarketRows[3].Amount, Is.EqualTo(int.MaxValue - 2));
        Assert.That(snapshot.Source.RemainingReserve, Is.EqualTo(5));
        Assert.That(snapshot.Source.Revision, Is.Zero);
        Assert.That(snapshot.MarketRevision, Is.Zero);
    }

    [Test]
    public void AppliedTransitionPreservesOrderedUnrelatedMarketRowsExactly()
    {
        FiniteSourceScenarioParityVectors.CreateDefinition(out FiniteSourceScenarioSnapshot fixture);
        List<MarketStockRow> rows = new List<MarketStockRow>
        {
            MarketStockRow.NullRow(),
            MarketStockRow.NullItem(-4, 0, float.NaN),
            MarketStockRow.Item(new MarketItemDefinition("item.other", 7f), 3, 20, 9f),
            fixture.MarketRows[0]
        };
        Assert.That(FiniteSourceScenarioDefinition.TryCreate(FiniteSourceScenarioParityVectors.LocationId,
            fixture.Source, fixture.MarketRevision, rows,
            out FiniteSourceScenarioDefinition definition, out string rejection), Is.True, rejection);
        FiniteSourceScenarioSnapshot snapshot = new FiniteSourceScenarioSnapshot(
            fixture.Source, fixture.MarketRevision, rows);
        Assert.That(FiniteSourceScenarioRuntime.TryCreate(definition, snapshot,
            out FiniteSourceScenarioRuntime runtime, out rejection), Is.True, rejection);
        Assert.That(runtime.TryPrepareAdvance(snapshot, 1,
            out PreparedFiniteSourceAdvance prepared, out rejection), Is.True, rejection);
        Assert.That(runtime.TryApplyPrepared(snapshot, prepared,
            out FiniteSourceScenarioSnapshot next, out rejection), Is.True, rejection);

        Assert.That(next.MarketRows.Count, Is.EqualTo(4));
        Assert.That(next.MarketRows[0].Kind, Is.EqualTo(MarketStockRowKind.NullRow));
        Assert.That(next.MarketRows[1].Kind, Is.EqualTo(MarketStockRowKind.NullItem));
        Assert.That(next.MarketRows[1].Amount, Is.EqualTo(-4));
        Assert.That(float.IsNaN(next.MarketRows[1].CurrentPrice), Is.True);
        Assert.That(next.MarketRows[2].ItemDefinition.DefinitionId, Is.EqualTo("item.other"));
        Assert.That(next.MarketRows[2].Amount, Is.EqualTo(3));
        Assert.That(next.MarketRows[2].DesiredAmount, Is.EqualTo(20));
        Assert.That(FloatBits(next.MarketRows[2].CurrentPrice), Is.EqualTo(FloatBits(9f)));
        Assert.That(next.MarketRows[3].Amount, Is.EqualTo(5));
    }

    [Test]
    public void MarketRevisionExhaustionAndWrongSourceIdentityRejectBeforeAnyNextState()
    {
        FiniteSourceScenarioParityVectors.CreateDefinition(out FiniteSourceScenarioSnapshot fixture);
        FiniteSourceSnapshot source = fixture.Source;
        Assert.That(FiniteSourceScenarioDefinition.TryCreate(FiniteSourceScenarioParityVectors.LocationId,
            source, long.MaxValue, fixture.MarketRows,
            out FiniteSourceScenarioDefinition maxMarketDefinition, out string rejection), Is.True, rejection);
        FiniteSourceScenarioSnapshot maxMarket = new FiniteSourceScenarioSnapshot(source,
            long.MaxValue, fixture.MarketRows);
        Assert.That(FiniteSourceScenarioRuntime.TryCreate(maxMarketDefinition, maxMarket,
            out FiniteSourceScenarioRuntime runtime, out rejection), Is.True, rejection);
        Assert.That(runtime.TryPrepareAdvance(maxMarket, 1, out _, out string exhausted), Is.False);
        Assert.That(exhausted, Is.EqualTo("MarketRevisionExhausted"));

        FiniteSourceSnapshot wrongIdentity = new FiniteSourceSnapshot(
            "source.other", source.SettlementSemanticId, source.MarketStoreSemanticId,
            source.ItemDefinitionId, source.ContentRevision, source.DailyOutputLimit,
            source.InitialReserve, source.RemainingReserve, source.Revision, source.LastProcessedDay);
        FiniteSourceScenarioSnapshot wrongOwner = new FiniteSourceScenarioSnapshot(wrongIdentity,
            fixture.MarketRevision, fixture.MarketRows);
        Assert.That(runtime.TryPrepareAdvance(wrongOwner, 1, out _, out string identity), Is.False);
        Assert.That(identity, Is.EqualTo("InvalidCurrentSnapshot"));
        Assert.That(fixture.Source.RemainingReserve, Is.EqualTo(5));
        Assert.That(fixture.MarketRows[0].Amount, Is.EqualTo(2));
    }

    private static int FloatBits(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
}
