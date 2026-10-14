using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum MarketStockRowKind
{
    NullRow,
    NullItem,
    Item
}

/// <summary>A copied, ordered Market row value. It never owns or references a live Unity object.</summary>
public sealed class MarketStockRow
{
    public MarketStockRowKind Kind { get; }
    public MarketItemDefinition ItemDefinition { get; }
    public int Amount { get; }
    public int DesiredAmount { get; }
    public float CurrentPrice { get; }

    private MarketStockRow(MarketStockRowKind kind, MarketItemDefinition itemDefinition,
        int amount, int desiredAmount, float currentPrice)
    {
        Kind = kind;
        ItemDefinition = itemDefinition;
        Amount = amount;
        DesiredAmount = desiredAmount;
        CurrentPrice = currentPrice;
    }

    public static MarketStockRow NullRow() => new MarketStockRow(MarketStockRowKind.NullRow, null, 0, 0, 0f);

    public static MarketStockRow NullItem(int amount, int desiredAmount, float currentPrice) =>
        new MarketStockRow(MarketStockRowKind.NullItem, null, amount, desiredAmount, currentPrice);

    public static MarketStockRow Item(MarketItemDefinition definition, int amount, int desiredAmount,
        float currentPrice)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        return new MarketStockRow(MarketStockRowKind.Item, definition, amount, desiredAmount, currentPrice);
    }
}

/// <summary>A detached value snapshot of the finite source live-owned by Unity.</summary>
public sealed class FiniteSourceSnapshot
{
    public string ProductionSourceId { get; }
    public string SettlementSemanticId { get; }
    public string MarketStoreSemanticId { get; }
    public string ItemDefinitionId { get; }
    public string ContentRevision { get; }
    public int DailyOutputLimit { get; }
    public int InitialReserve { get; }
    public int RemainingReserve { get; }
    public long Revision { get; }
    public long LastProcessedDay { get; }

    public FiniteSourceSnapshot(string productionSourceId, string settlementSemanticId,
        string marketStoreSemanticId, string itemDefinitionId, string contentRevision,
        int dailyOutputLimit, int initialReserve, int remainingReserve, long revision, long lastProcessedDay)
    {
        ProductionSourceId = productionSourceId;
        SettlementSemanticId = settlementSemanticId;
        MarketStoreSemanticId = marketStoreSemanticId;
        ItemDefinitionId = itemDefinitionId;
        ContentRevision = contentRevision;
        DailyOutputLimit = dailyOutputLimit;
        InitialReserve = initialReserve;
        RemainingReserve = remainingReserve;
        Revision = revision;
        LastProcessedDay = lastProcessedDay;
    }
}

/// <summary>An immutable request for one admitted finite-source daily operation.</summary>
public sealed class FiniteSourceOperationRequest
{
    private readonly ReadOnlyCollection<MarketStockRow> marketRows;

    public FiniteSourceSnapshot Source { get; }
    public long ExpectedSourceRevision { get; }
    public long MarketRevision { get; }
    public long ExpectedMarketRevision { get; }
    public long AdmittedAbsoluteDay { get; }
    public int RequestedQuantity { get; }
    public IReadOnlyList<MarketStockRow> MarketRows => marketRows;

    public FiniteSourceOperationRequest(FiniteSourceSnapshot source, long expectedSourceRevision,
        long marketRevision, long expectedMarketRevision, long admittedAbsoluteDay, int requestedQuantity,
        IReadOnlyList<MarketStockRow> orderedMarketRows)
    {
        Source = source;
        ExpectedSourceRevision = expectedSourceRevision;
        MarketRevision = marketRevision;
        ExpectedMarketRevision = expectedMarketRevision;
        AdmittedAbsoluteDay = admittedAbsoluteDay;
        RequestedQuantity = requestedQuantity;
        List<MarketStockRow> copy = new List<MarketStockRow>();
        if (orderedMarketRows != null)
        {
            for (int i = 0; i < orderedMarketRows.Count; i++)
                copy.Add(orderedMarketRows[i] ?? MarketStockRow.NullRow());
        }
        marketRows = copy.AsReadOnly();
    }

    public FiniteSourceOperationRequest WithMarketRows(IReadOnlyList<MarketStockRow> orderedMarketRows) =>
        new FiniteSourceOperationRequest(Source, ExpectedSourceRevision, MarketRevision,
            ExpectedMarketRevision, AdmittedAbsoluteDay, RequestedQuantity, orderedMarketRows);
}

public enum FiniteSourceOperationStatus
{
    Applied,
    Exhausted
}

/// <summary>A detached prepared transition. It does not install either live owner.</summary>
public sealed class FiniteSourceOperationResult
{
    private readonly ReadOnlyCollection<MarketStockRow> marketRows;

    public FiniteSourceOperationStatus Status { get; }
    public int Quantity { get; }
    public int ReserveBefore { get; }
    public int ReserveAfter { get; }
    public long SourceRevisionBefore { get; }
    public long SourceRevisionAfter { get; }
    public long MarketRevisionBefore { get; }
    public long MarketRevisionAfter { get; }
    public int StockBefore { get; }
    public int StockAfter { get; }
    public int TargetRowIndex { get; }
    public IReadOnlyList<MarketStockRow> PreparedMarketRows => marketRows;

    internal FiniteSourceOperationResult(FiniteSourceOperationStatus status, int quantity,
        int reserveBefore, int reserveAfter, long sourceRevisionBefore, long sourceRevisionAfter,
        long marketRevisionBefore, long marketRevisionAfter, int stockBefore, int stockAfter,
        int targetRowIndex, IReadOnlyList<MarketStockRow> rows)
    {
        Status = status;
        Quantity = quantity;
        ReserveBefore = reserveBefore;
        ReserveAfter = reserveAfter;
        SourceRevisionBefore = sourceRevisionBefore;
        SourceRevisionAfter = sourceRevisionAfter;
        MarketRevisionBefore = marketRevisionBefore;
        MarketRevisionAfter = marketRevisionAfter;
        StockBefore = stockBefore;
        StockAfter = stockAfter;
        TargetRowIndex = targetRowIndex;
        List<MarketStockRow> copy = new List<MarketStockRow>();
        if (rows != null)
        {
            for (int i = 0; i < rows.Count; i++) copy.Add(rows[i] ?? MarketStockRow.NullRow());
        }
        marketRows = copy.AsReadOnly();
    }
}

/// <summary>Prepares the P14-B reserve debit and Market stock credit without mutating either owner.</summary>
public static class FiniteSourceProductionKernel
{
    public static bool TryPrepare(FiniteSourceOperationRequest request,
        out FiniteSourceOperationResult result, out string rejectionReason)
    {
        result = null;
        rejectionReason = "IdentityMismatch";
        if (request == null || request.Source == null) return false;

        FiniteSourceSnapshot source = request.Source;
        if (source.Revision != request.ExpectedSourceRevision)
        {
            rejectionReason = "StaleSourceRevision";
            return false;
        }
        if (request.MarketRevision != request.ExpectedMarketRevision)
        {
            rejectionReason = "StaleMarketRevision";
            return false;
        }
        if (request.AdmittedAbsoluteDay <= source.LastProcessedDay)
        {
            rejectionReason = "BoundaryAlreadyProcessed";
            return false;
        }
        if (source.RemainingReserve < 0 || source.RemainingReserve > source.InitialReserve
            || source.DailyOutputLimit <= 0)
        {
            rejectionReason = "InvalidSourceState";
            return false;
        }

        int quantity = Math.Min(source.DailyOutputLimit, source.RemainingReserve);
        if (request.RequestedQuantity != quantity)
        {
            rejectionReason = "InvalidMarketInput";
            return false;
        }
        if (quantity == 0)
        {
            result = new FiniteSourceOperationResult(FiniteSourceOperationStatus.Exhausted, 0,
                source.RemainingReserve, source.RemainingReserve, source.Revision, source.Revision,
                request.MarketRevision, request.MarketRevision, 0, 0, -1, null);
            rejectionReason = string.Empty;
            return true;
        }
        if (source.Revision == long.MaxValue)
        {
            rejectionReason = "SourceRevisionExhausted";
            return false;
        }
        if (request.MarketRevision == long.MaxValue)
        {
            rejectionReason = "MarketRevisionExhausted";
            return false;
        }
        if (string.IsNullOrEmpty(source.ItemDefinitionId))
        {
            rejectionReason = "IdentityMismatch";
            return false;
        }

        int matchCount = 0;
        int targetRowIndex = -1;
        for (int i = 0; i < request.MarketRows.Count; i++)
        {
            MarketStockRow row = request.MarketRows[i];
            if (row != null && row.Kind == MarketStockRowKind.Item
                && string.Equals(row.ItemDefinition.DefinitionId, source.ItemDefinitionId, StringComparison.Ordinal))
            {
                matchCount++;
                targetRowIndex = i;
            }
        }
        if (matchCount != 1)
        {
            rejectionReason = "MarketRowCardinality";
            return false;
        }

        MarketStockRow target = request.MarketRows[targetRowIndex];
        if (target.Amount > int.MaxValue - quantity)
        {
            rejectionReason = "MarketStockOverflow";
            return false;
        }

        int nextAmount = target.Amount + quantity;
        List<MarketStockRow> replacement = new List<MarketStockRow>(request.MarketRows.Count);
        for (int i = 0; i < request.MarketRows.Count; i++)
        {
            MarketStockRow row = request.MarketRows[i];
            if (i != targetRowIndex)
            {
                replacement.Add(row);
                continue;
            }

            float nextPrice = MarketPriceCalculator.CalculateCurrentPrice(
                row.ItemDefinition.BasePrice, row.DesiredAmount, nextAmount);
            replacement.Add(MarketStockRow.Item(row.ItemDefinition, nextAmount, row.DesiredAmount, nextPrice));
        }

        result = new FiniteSourceOperationResult(FiniteSourceOperationStatus.Applied, quantity,
            source.RemainingReserve, source.RemainingReserve - quantity, source.Revision,
            source.Revision + 1, request.MarketRevision, request.MarketRevision + 1,
            target.Amount, nextAmount, targetRowIndex, replacement);
        rejectionReason = string.Empty;
        return true;
    }
}
