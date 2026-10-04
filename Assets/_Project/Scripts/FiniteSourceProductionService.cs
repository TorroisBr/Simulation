using System;

public enum FiniteSourceProductionStatus
{
    Applied,
    Exhausted,
    Rejected
}

public sealed class FiniteSourceProductionResult
{
    public FiniteSourceProductionStatus Status { get; }
    public string RejectionReason { get; }
    public int Quantity { get; }
    public int ReserveBefore { get; }
    public int ReserveAfter { get; }
    public int StockBefore { get; }
    public int StockAfter { get; }
    public long SourceRevision { get; }
    public long MarketRevision { get; }

    internal FiniteSourceProductionResult(
        FiniteSourceProductionStatus status,
        string reason,
        int quantity,
        int reserveBefore,
        int reserveAfter,
        int stockBefore,
        int stockAfter,
        long sourceRevision,
        long marketRevision)
    {
        Status = status;
        RejectionReason = reason ?? string.Empty;
        Quantity = quantity;
        ReserveBefore = reserveBefore;
        ReserveAfter = reserveAfter;
        StockBefore = stockBefore;
        StockAfter = stockAfter;
        SourceRevision = sourceRevision;
        MarketRevision = marketRevision;
    }
}

/// <summary>Coordinates one prevalidated reserve debit and Market stock credit.</summary>
public sealed class FiniteSourceProductionService
{
    public FiniteSourceProductionResult TryProduceDaily(
        FiniteProductionSourceStore sourceStore,
        MarketRuntime market,
        string settlementSemanticId,
        string marketStoreSemanticId,
        ItemData item,
        long absoluteDay,
        long expectedSourceRevision,
        long expectedMarketRevision)
    {
        FiniteProductionSourceState source = sourceStore != null ? sourceStore.Source : null;
        if (source == null || market == null || item == null
            || !string.Equals(source.SettlementSemanticId, settlementSemanticId, StringComparison.Ordinal)
            || !string.Equals(source.MarketStoreSemanticId, marketStoreSemanticId, StringComparison.Ordinal)
            || !string.Equals(source.ItemDefinitionId, item.DefinitionId, StringComparison.Ordinal))
        {
            return Rejected("IdentityMismatch", source, market, item);
        }

        if (source.Revision != expectedSourceRevision)
            return Rejected("StaleSourceRevision", source, market, item);
        if (market.Revision != expectedMarketRevision)
            return Rejected("StaleMarketRevision", source, market, item);
        if (absoluteDay <= source.LastProcessedDay)
            return Rejected("BoundaryAlreadyProcessed", source, market, item);
        if (source.RemainingReserve < 0 || source.RemainingReserve > source.InitialReserve
            || source.DailyOutputLimit <= 0)
            return Rejected("InvalidSourceState", source, market, item);

        int quantity = Math.Min(source.DailyOutputLimit, source.RemainingReserve);
        if (quantity == 0)
        {
            return new FiniteSourceProductionResult(
                FiniteSourceProductionStatus.Exhausted,
                "ReserveExhausted",
                0,
                source.RemainingReserve,
                source.RemainingReserve,
                market.GetAmount(item),
                market.GetAmount(item),
                source.Revision,
                market.Revision);
        }

        if (source.Revision == long.MaxValue)
            return Rejected("SourceRevisionExhausted", source, market, item);

        if (!market.TryPrepareFiniteStockIncrease(item, quantity, expectedMarketRevision,
                out PreparedMarketState preparedMarket, out int stockBefore, out string marketRejection))
        {
            return Rejected(marketRejection, source, market, item);
        }

        if (!sourceStore.CanInstall(expectedSourceRevision, absoluteDay))
            return Rejected(source.Revision != expectedSourceRevision
                ? "StaleSourceRevision" : "SourceRevisionExhausted", source, market, item);
        if (!market.CanInstallFiniteStock(expectedMarketRevision, preparedMarket))
            return Rejected(market.Revision != expectedMarketRevision
                ? "StaleMarketRevision" : "MarketMutationRejected", source, market, item);

        int reserveBefore = source.RemainingReserve;
        int stockAfter = stockBefore + quantity;

        // Both owners were validated above. Install the two roots synchronously, then
        // publish the P12 owner notification only after both values are authoritative.
        sourceStore.Install(expectedSourceRevision, absoluteDay, quantity);
        market.InstallFiniteStock(expectedMarketRevision, preparedMarket);
        market.NotifyFiniteStockInstalled();

        return new FiniteSourceProductionResult(
            FiniteSourceProductionStatus.Applied,
            string.Empty,
            quantity,
            reserveBefore,
            source.RemainingReserve,
            stockBefore,
            stockAfter,
            source.Revision,
            market.Revision);
    }

    private static FiniteSourceProductionResult Rejected(
        string reason,
        FiniteProductionSourceState source,
        MarketRuntime market,
        ItemData item)
    {
        int reserve = source != null ? source.RemainingReserve : 0;
        int stock = market != null && item != null ? market.GetAmount(item) : 0;
        return new FiniteSourceProductionResult(
            FiniteSourceProductionStatus.Rejected,
            reason,
            0,
            reserve,
            reserve,
            stock,
            stock,
            source != null ? source.Revision : 0,
            market != null ? market.Revision : 0);
    }
}
