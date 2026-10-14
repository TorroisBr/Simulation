using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class FiniteSourceProductionKernelTests
{
    [Test]
    public void AppliedOperationPreparesBothOwnersAndPreservesOrderedUnrelatedRows()
    {
        List<MarketStockRow> rows = new List<MarketStockRow>
        {
            MarketStockRow.NullRow(),
            MarketStockRow.NullItem(-4, 0, float.NaN),
            MarketStockRow.Item(new MarketItemDefinition("item.other", 7f), 3, 20, 9f),
            MarketStockRow.Item(new MarketItemDefinition("item.target", 2f), 4, 8, 1f)
        };
        FiniteSourceOperationRequest request = CreateRequest(rows);

        Assert.That(FiniteSourceProductionKernel.TryPrepare(request,
            out FiniteSourceOperationResult result, out string rejection), Is.True, rejection);

        Assert.That(result.Status, Is.EqualTo(FiniteSourceOperationStatus.Applied));
        Assert.That(result.Quantity, Is.EqualTo(5));
        Assert.That(result.ReserveBefore, Is.EqualTo(7));
        Assert.That(result.ReserveAfter, Is.EqualTo(2));
        Assert.That(result.SourceRevisionBefore, Is.EqualTo(2));
        Assert.That(result.SourceRevisionAfter, Is.EqualTo(3));
        Assert.That(result.MarketRevisionBefore, Is.EqualTo(3));
        Assert.That(result.MarketRevisionAfter, Is.EqualTo(4));
        Assert.That(result.StockBefore, Is.EqualTo(4));
        Assert.That(result.StockAfter, Is.EqualTo(9));
        Assert.That(result.TargetRowIndex, Is.EqualTo(3));
        Assert.That(result.PreparedMarketRows.Count, Is.EqualTo(rows.Count));
        Assert.That(result.PreparedMarketRows[0].Kind, Is.EqualTo(MarketStockRowKind.NullRow));
        Assert.That(result.PreparedMarketRows[1].Kind, Is.EqualTo(MarketStockRowKind.NullItem));
        Assert.That(result.PreparedMarketRows[1].Amount, Is.EqualTo(-4));
        Assert.That(float.IsNaN(result.PreparedMarketRows[1].CurrentPrice), Is.True);
        Assert.That(result.PreparedMarketRows[2].ItemDefinition.DefinitionId, Is.EqualTo("item.other"));
        Assert.That(result.PreparedMarketRows[2].Amount, Is.EqualTo(3));
        Assert.That(result.PreparedMarketRows[2].CurrentPrice, Is.EqualTo(9f));
        Assert.That(result.PreparedMarketRows[3].Amount, Is.EqualTo(9));
        Assert.That(result.PreparedMarketRows[3].DesiredAmount, Is.EqualTo(8));
        Assert.That(result.PreparedMarketRows[3].CurrentPrice,
            Is.EqualTo(MarketPriceCalculator.CalculateCurrentPrice(2f, 8, 9)));

        Assert.That(request.Source.RemainingReserve, Is.EqualTo(7));
        Assert.That(request.Source.Revision, Is.EqualTo(2));
        Assert.That(rows[3].Amount, Is.EqualTo(4));
        Assert.That(request.MarketRevision, Is.EqualTo(3));
    }

    [Test]
    public void DuplicateDefinitionIdsRejectAsAmbiguousWithoutMutatingInputs()
    {
        FiniteSourceOperationRequest request = CreateRequest(new List<MarketStockRow>
        {
            MarketStockRow.Item(new MarketItemDefinition("item.target", 2f), 4, 8, 1f),
            MarketStockRow.Item(new MarketItemDefinition("item.target", 3f), 4, 8, 1f)
        });

        AssertRejected(request, "MarketRowCardinality");
        Assert.That(request.MarketRows[0].Amount, Is.EqualTo(4));
        Assert.That(request.MarketRows[1].Amount, Is.EqualTo(4));
    }

    [Test]
    public void MissingTargetOrStockOverflowRejectsWithoutMutation()
    {
        AssertRejected(CreateRequest(new List<MarketStockRow>
        {
            MarketStockRow.Item(new MarketItemDefinition("item.other", 2f), 4, 8, 1f)
        }), "MarketRowCardinality");

        AssertRejected(CreateRequest(new List<MarketStockRow>
        {
            MarketStockRow.Item(new MarketItemDefinition("item.target", 2f), int.MaxValue - 4, 8, 1f)
        }), "MarketStockOverflow");
    }

    [Test]
    public void SourceMarketDayAndRevisionBoundariesKeepTheirRejectionOrder()
    {
        FiniteSourceOperationRequest sourceStale = CreateRequest(TargetRows(), expectedSourceRevision: 1);
        AssertRejected(sourceStale, "StaleSourceRevision");

        FiniteSourceOperationRequest marketStale = CreateRequest(TargetRows(), expectedMarketRevision: 2);
        AssertRejected(marketStale, "StaleMarketRevision");

        FiniteSourceOperationRequest dayStale = CreateRequest(TargetRows(), day: 5);
        AssertRejected(dayStale, "BoundaryAlreadyProcessed");

        FiniteSourceSnapshot invalidReserve = CreateSource(remainingReserve: 11);
        AssertRejected(CreateRequest(invalidReserve, TargetRows()), "InvalidSourceState");

        FiniteSourceSnapshot invalidLimit = CreateSource(dailyLimit: 0);
        AssertRejected(CreateRequest(invalidLimit, TargetRows()), "InvalidSourceState");

        FiniteSourceSnapshot sourceExhaustedRevision = CreateSource(revision: long.MaxValue);
        AssertRejected(CreateRequest(sourceExhaustedRevision, TargetRows(),
            expectedSourceRevision: long.MaxValue), "SourceRevisionExhausted");

        FiniteSourceOperationRequest marketExhaustedRevision = CreateRequest(TargetRows(), marketRevision: long.MaxValue,
            expectedMarketRevision: long.MaxValue);
        AssertRejected(marketExhaustedRevision, "MarketRevisionExhausted");

        FiniteSourceOperationRequest invalidQuantity = CreateRequest(TargetRows(), quantity: 4);
        AssertRejected(invalidQuantity, "InvalidMarketInput");
    }

    [Test]
    public void EmptyReserveIsExhaustedWithoutPreparingMarketRowsOrAdvancingRevisions()
    {
        FiniteSourceOperationRequest request = CreateRequest(
            CreateSource(remainingReserve: 0), new List<MarketStockRow>(), quantity: 0);

        Assert.That(FiniteSourceProductionKernel.TryPrepare(request,
            out FiniteSourceOperationResult result, out string rejection), Is.True, rejection);
        Assert.That(result.Status, Is.EqualTo(FiniteSourceOperationStatus.Exhausted));
        Assert.That(result.Quantity, Is.Zero);
        Assert.That(result.ReserveAfter, Is.Zero);
        Assert.That(result.SourceRevisionAfter, Is.EqualTo(result.SourceRevisionBefore));
        Assert.That(result.MarketRevisionAfter, Is.EqualTo(result.MarketRevisionBefore));
        Assert.That(result.PreparedMarketRows, Is.Empty);
    }

    [Test]
    public void RequestCopiesTheOrderedRowListAndExposesItAsReadOnly()
    {
        List<MarketStockRow> rows = new List<MarketStockRow>(TargetRows());
        FiniteSourceOperationRequest request = CreateRequest(rows);
        rows.Clear();

        Assert.That(request.MarketRows.Count, Is.EqualTo(1));
        Assert.Throws<NotSupportedException>(() => ((IList<MarketStockRow>)request.MarketRows).Add(
            MarketStockRow.NullRow()));
    }

    private static void AssertRejected(FiniteSourceOperationRequest request, string expectedReason)
    {
        int reserveBefore = request.Source.RemainingReserve;
        long sourceRevisionBefore = request.Source.Revision;
        long marketRevisionBefore = request.MarketRevision;
        Assert.That(FiniteSourceProductionKernel.TryPrepare(request,
            out FiniteSourceOperationResult result, out string rejection), Is.False);
        Assert.That(result, Is.Null);
        Assert.That(rejection, Is.EqualTo(expectedReason));
        Assert.That(request.Source.RemainingReserve, Is.EqualTo(reserveBefore));
        Assert.That(request.Source.Revision, Is.EqualTo(sourceRevisionBefore));
        Assert.That(request.MarketRevision, Is.EqualTo(marketRevisionBefore));
    }

    private static List<MarketStockRow> TargetRows() => new List<MarketStockRow>
    {
        MarketStockRow.Item(new MarketItemDefinition("item.target", 2f), 4, 8, 1f)
    };

    private static FiniteSourceOperationRequest CreateRequest(List<MarketStockRow> rows,
        long expectedSourceRevision = 2, long marketRevision = 3, long expectedMarketRevision = 3,
        long day = 6, int quantity = 5) => CreateRequest(CreateSource(), rows, expectedSourceRevision,
            marketRevision, expectedMarketRevision, day, quantity);

    private static FiniteSourceOperationRequest CreateRequest(FiniteSourceSnapshot source,
        List<MarketStockRow> rows, long expectedSourceRevision = 2, long marketRevision = 3,
        long expectedMarketRevision = 3, long day = 6, int quantity = 5) =>
        new FiniteSourceOperationRequest(source, expectedSourceRevision, marketRevision,
            expectedMarketRevision, day, quantity, rows);

    private static FiniteSourceSnapshot CreateSource(int dailyLimit = 5, int initialReserve = 10,
        int remainingReserve = 7, long revision = 2) => new FiniteSourceSnapshot(
            "source.target", "settlement.target", "store.target", "item.target", "content-v1",
            dailyLimit, initialReserve, remainingReserve, revision, 5);
}
