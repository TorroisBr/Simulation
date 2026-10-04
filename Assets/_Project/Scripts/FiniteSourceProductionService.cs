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

internal sealed class FiniteSourceProductionPreparation
{
    private readonly FiniteProductionSourceStore sourceStore;
    private readonly MarketRuntime market;
    internal readonly PreparedMarketState MarketState;
    internal readonly long SourceRevision, MarketRevision, AbsoluteDay;
    internal readonly int Quantity, ReserveBefore, StockBefore;

    internal FiniteSourceProductionPreparation(
        FiniteProductionSourceStore sourceStore,
        MarketRuntime market,
        PreparedMarketState marketState,
        long sourceRevision,
        long marketRevision,
        long absoluteDay,
        int quantity,
        int reserveBefore,
        int stockBefore)
    {
        this.sourceStore = sourceStore;
        this.market = market;
        MarketState = marketState;
        SourceRevision = sourceRevision;
        MarketRevision = marketRevision;
        AbsoluteDay = absoluteDay;
        Quantity = quantity;
        ReserveBefore = reserveBefore;
        StockBefore = stockBefore;
    }

    internal bool CanInstall() => Quantity > 0
        && sourceStore.CanInstall(SourceRevision, AbsoluteDay)
        && market.CanInstallFiniteStock(MarketRevision, MarketState);

    internal bool Install()
    {
        if (!CanInstall()) return false;
        sourceStore.Install(SourceRevision, AbsoluteDay, Quantity);
        market.InstallFiniteStock(MarketRevision, MarketState);
        market.NotifyFiniteStockInstalled();
        return true;
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
        FiniteProductionSourceState expectedSource = sourceStore != null ? sourceStore.Source : null;
        if (!TryPrepareDaily(sourceStore, market, settlementSemanticId, marketStoreSemanticId,
                expectedSource != null ? expectedSource.ProductionSourceId : null,
                expectedSource != null ? expectedSource.ContentRevision : null,
                item, absoluteDay, expectedSourceRevision, expectedMarketRevision,
                out FiniteSourceProductionPreparation preparation, out string rejection))
            return Rejected(rejection, sourceStore != null ? sourceStore.Source : null, market, item);

        FiniteProductionSourceState source = sourceStore.Source;
        if (preparation.Quantity == 0)
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

        if (!preparation.Install())
            return Rejected(source.Revision != expectedSourceRevision
                ? "StaleSourceRevision" : market.Revision != expectedMarketRevision
                    ? "StaleMarketRevision" : "MarketMutationRejected", source, market, item);

        return new FiniteSourceProductionResult(
            FiniteSourceProductionStatus.Applied,
            string.Empty,
            preparation.Quantity,
            preparation.ReserveBefore,
            source.RemainingReserve,
            preparation.StockBefore,
            preparation.StockBefore + preparation.Quantity,
            source.Revision,
            market.Revision);
    }

    internal bool TryPrepareDaily(
        FiniteProductionSourceStore sourceStore,
        MarketRuntime market,
        string settlementSemanticId,
        string marketStoreSemanticId,
        string productionSourceId,
        string contentRevision,
        ItemData item,
        long absoluteDay,
        long expectedSourceRevision,
        long expectedMarketRevision,
        out FiniteSourceProductionPreparation preparation,
        out string rejectionReason)
    {
        preparation = null;
        rejectionReason = "IdentityMismatch";
        FiniteProductionSourceState source = sourceStore != null ? sourceStore.Source : null;
        if (source == null || market == null || item == null
            || !string.Equals(source.SettlementSemanticId, settlementSemanticId, StringComparison.Ordinal)
            || !string.Equals(source.MarketStoreSemanticId, marketStoreSemanticId, StringComparison.Ordinal)
            || !string.Equals(source.ProductionSourceId, productionSourceId, StringComparison.Ordinal)
            || !string.Equals(source.ContentRevision, contentRevision, StringComparison.Ordinal)
            || !string.Equals(source.ItemDefinitionId, item.DefinitionId, StringComparison.Ordinal))
            return false;

        if (source.Revision != expectedSourceRevision) { rejectionReason = "StaleSourceRevision"; return false; }
        if (market.Revision != expectedMarketRevision) { rejectionReason = "StaleMarketRevision"; return false; }
        if (absoluteDay <= source.LastProcessedDay) { rejectionReason = "BoundaryAlreadyProcessed"; return false; }
        if (source.RemainingReserve < 0 || source.RemainingReserve > source.InitialReserve
            || source.DailyOutputLimit <= 0)
        { rejectionReason = "InvalidSourceState"; return false; }

        int quantity = Math.Min(source.DailyOutputLimit, source.RemainingReserve);
        if (quantity == 0)
        {
            preparation = new FiniteSourceProductionPreparation(sourceStore, market, null,
                expectedSourceRevision, expectedMarketRevision, absoluteDay, 0,
                source.RemainingReserve, market.GetAmount(item));
            rejectionReason = string.Empty;
            return true;
        }
        if (source.Revision == long.MaxValue) { rejectionReason = "SourceRevisionExhausted"; return false; }
        if (!market.TryPrepareFiniteStockIncrease(item, quantity, expectedMarketRevision,
                out PreparedMarketState preparedMarket, out int stockBefore, out rejectionReason))
            return false;
        if (!sourceStore.CanInstall(expectedSourceRevision, absoluteDay))
        {
            rejectionReason = source.Revision != expectedSourceRevision
                ? "StaleSourceRevision" : "SourceRevisionExhausted";
            return false;
        }
        if (!market.CanInstallFiniteStock(expectedMarketRevision, preparedMarket))
        {
            rejectionReason = market.Revision != expectedMarketRevision
                ? "StaleMarketRevision" : "MarketMutationRejected";
            return false;
        }

        preparation = new FiniteSourceProductionPreparation(sourceStore, market, preparedMarket,
            expectedSourceRevision, expectedMarketRevision, absoluteDay, quantity,
            source.RemainingReserve, stockBefore);
        rejectionReason = string.Empty;
        return true;
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
