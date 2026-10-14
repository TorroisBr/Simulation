using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Effective, Unity-free identity and admission values for one finite-source scenario.</summary>
public sealed class FiniteSourceScenarioDefinition
{
    private readonly ReadOnlyCollection<MarketStockRow> initialMarketRows;
    private readonly FiniteSourceSnapshot initialSource;

    public string LocationId { get; }
    public string ProductionSourceId { get; }
    public string SettlementSemanticId { get; }
    public string MarketStoreSemanticId { get; }
    public string ItemDefinitionId { get; }
    public string ContentRevision { get; }
    public int DailyOutputLimit { get; }
    public int InitialReserve { get; }
    public long InitialMarketRevision { get; }
    public IReadOnlyList<MarketStockRow> InitialMarketRows => initialMarketRows;
    public FiniteSourceSnapshot InitialSource => initialSource;

    private FiniteSourceScenarioDefinition(string locationId, FiniteSourceSnapshot source,
        long marketRevision, IReadOnlyList<MarketStockRow> rows)
    {
        LocationId = locationId;
        ProductionSourceId = source.ProductionSourceId;
        SettlementSemanticId = source.SettlementSemanticId;
        MarketStoreSemanticId = source.MarketStoreSemanticId;
        ItemDefinitionId = source.ItemDefinitionId;
        ContentRevision = source.ContentRevision;
        DailyOutputLimit = source.DailyOutputLimit;
        InitialReserve = source.InitialReserve;
        InitialMarketRevision = marketRevision;
        initialSource = source;
        initialMarketRows = CopyRows(rows);
    }

    public static bool TryCreate(string locationId, FiniteSourceSnapshot source,
        long marketRevision, IReadOnlyList<MarketStockRow> orderedMarketRows,
        out FiniteSourceScenarioDefinition definition, out string rejectionReason)
    {
        definition = null;
        rejectionReason = "InvalidScenarioDefinition";
        if (string.IsNullOrWhiteSpace(locationId) || source == null
            || string.IsNullOrWhiteSpace(source.ProductionSourceId)
            || string.IsNullOrWhiteSpace(source.SettlementSemanticId)
            || string.IsNullOrWhiteSpace(source.MarketStoreSemanticId)
            || string.IsNullOrWhiteSpace(source.ItemDefinitionId)
            || string.IsNullOrWhiteSpace(source.ContentRevision)
            || source.DailyOutputLimit <= 0
            || source.InitialReserve < 0
            || source.RemainingReserve < 0
            || source.RemainingReserve > source.InitialReserve
            || source.Revision < 0
            || marketRevision < 0
            || orderedMarketRows == null)
            return false;

        int targetCount = 0;
        for (int i = 0; i < orderedMarketRows.Count; i++)
        {
            MarketStockRow row = orderedMarketRows[i];
            if (row != null && row.Kind == MarketStockRowKind.Item
                && row.ItemDefinition != null
                && string.Equals(row.ItemDefinition.DefinitionId, source.ItemDefinitionId, StringComparison.Ordinal))
                targetCount++;
        }
        if (targetCount != 1)
        {
            rejectionReason = "MarketRowCardinality";
            return false;
        }

        definition = new FiniteSourceScenarioDefinition(locationId, source, marketRevision, orderedMarketRows);
        rejectionReason = string.Empty;
        return true;
    }

    private static ReadOnlyCollection<MarketStockRow> CopyRows(IReadOnlyList<MarketStockRow> rows)
    {
        List<MarketStockRow> copy = new List<MarketStockRow>(rows.Count);
        for (int i = 0; i < rows.Count; i++) copy.Add(rows[i] ?? MarketStockRow.NullRow());
        return copy.AsReadOnly();
    }
}

/// <summary>An immutable, detached view of both live P14-B owners.</summary>
public sealed class FiniteSourceScenarioSnapshot
{
    private readonly ReadOnlyCollection<MarketStockRow> marketRows;

    public FiniteSourceSnapshot Source { get; }
    public long MarketRevision { get; }
    public IReadOnlyList<MarketStockRow> MarketRows => marketRows;

    public FiniteSourceScenarioSnapshot(FiniteSourceSnapshot source, long marketRevision,
        IReadOnlyList<MarketStockRow> orderedMarketRows)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (orderedMarketRows == null) throw new ArgumentNullException(nameof(orderedMarketRows));
        Source = source;
        MarketRevision = marketRevision;
        List<MarketStockRow> copy = new List<MarketStockRow>(orderedMarketRows.Count);
        for (int i = 0; i < orderedMarketRows.Count; i++)
            copy.Add(orderedMarketRows[i] ?? MarketStockRow.NullRow());
        marketRows = copy.AsReadOnly();
    }
}

/// <summary>An immutable transition proposal; it has no owner-install capability.</summary>
public sealed class PreparedFiniteSourceAdvance
{
    internal readonly FiniteSourceScenarioRuntime Runtime;
    internal readonly FiniteSourceScenarioSnapshot Basis;
    internal readonly FiniteSourceOperationResult Operation;
    public long AdmittedAbsoluteDay { get; }
    public FiniteSourceOperationStatus Status => Operation.Status;
    public int Quantity => Operation.Quantity;
    public int ReserveBefore => Operation.ReserveBefore;
    public int ReserveAfter => Operation.ReserveAfter;
    public long SourceRevisionBefore => Operation.SourceRevisionBefore;
    public long SourceRevisionAfter => Operation.SourceRevisionAfter;
    public long MarketRevisionBefore => Operation.MarketRevisionBefore;
    public long MarketRevisionAfter => Operation.MarketRevisionAfter;
    public int StockBefore => Operation.StockBefore;
    public int StockAfter => Operation.StockAfter;
    public int TargetRowIndex => Operation.TargetRowIndex;

    internal PreparedFiniteSourceAdvance(FiniteSourceScenarioRuntime runtime,
        FiniteSourceScenarioSnapshot basis, long day, FiniteSourceOperationResult operation)
    {
        Runtime = runtime;
        Basis = basis;
        AdmittedAbsoluteDay = day;
        Operation = operation;
    }
}

/// <summary>
/// Pure finite-source transition coordinator. The host supplies detached owner values and remains
/// responsible for atomically installing a returned snapshot through its existing owner boundary.
/// </summary>
public sealed class FiniteSourceScenarioRuntime
{
    private readonly FiniteSourceScenarioDefinition definition;

    private FiniteSourceScenarioRuntime(FiniteSourceScenarioDefinition definition) => this.definition = definition;

    public static bool TryCreate(FiniteSourceScenarioDefinition definition,
        FiniteSourceScenarioSnapshot initialState, out FiniteSourceScenarioRuntime runtime,
        out string rejectionReason)
    {
        runtime = null;
        rejectionReason = "InvalidInitialSnapshot";
        if (definition == null || initialState == null
            || !SameSource(definition.InitialSource, initialState.Source)
            || !HasDefinitionSource(definition, initialState.Source)
            || initialState.MarketRevision != definition.InitialMarketRevision
            || !SameRows(definition.InitialMarketRows, initialState.MarketRows))
            return false;
        runtime = new FiniteSourceScenarioRuntime(definition);
        rejectionReason = string.Empty;
        return true;
    }

    public bool TryPrepareAdvance(FiniteSourceScenarioSnapshot currentSnapshot,
        long admittedAbsoluteDay, out PreparedFiniteSourceAdvance prepared, out string rejectionReason)
    {
        prepared = null;
        rejectionReason = "InvalidCurrentSnapshot";
        if (!IsValidSnapshot(currentSnapshot)) return false;

        FiniteSourceSnapshot source = currentSnapshot.Source;
        int quantity = Math.Min(source.DailyOutputLimit, source.RemainingReserve);
        FiniteSourceOperationRequest request = new FiniteSourceOperationRequest(
            source, source.Revision, currentSnapshot.MarketRevision, currentSnapshot.MarketRevision,
            admittedAbsoluteDay, quantity, currentSnapshot.MarketRows);
        if (!FiniteSourceProductionKernel.TryPrepare(request,
                out FiniteSourceOperationResult operation, out rejectionReason))
            return false;

        prepared = new PreparedFiniteSourceAdvance(this, currentSnapshot, admittedAbsoluteDay, operation);
        return true;
    }

    public bool TryApplyPrepared(FiniteSourceScenarioSnapshot currentSnapshot,
        PreparedFiniteSourceAdvance prepared, out FiniteSourceScenarioSnapshot nextSnapshot,
        out string rejectionReason)
    {
        nextSnapshot = null;
        rejectionReason = "InvalidPreparedAdvance";
        if (prepared == null || !ReferenceEquals(prepared.Runtime, this)
            || !IsValidSnapshot(currentSnapshot)
            || !SameSnapshot(prepared.Basis, currentSnapshot))
            return false;

        if (prepared.Operation.Status == FiniteSourceOperationStatus.Exhausted)
        {
            nextSnapshot = currentSnapshot;
            rejectionReason = string.Empty;
            return true;
        }

        FiniteSourceSnapshot source = currentSnapshot.Source;
        FiniteSourceSnapshot nextSource = new FiniteSourceSnapshot(
            source.ProductionSourceId, source.SettlementSemanticId, source.MarketStoreSemanticId,
            source.ItemDefinitionId, source.ContentRevision, source.DailyOutputLimit,
            source.InitialReserve, prepared.Operation.ReserveAfter,
            prepared.Operation.SourceRevisionAfter, prepared.AdmittedAbsoluteDay);
        nextSnapshot = new FiniteSourceScenarioSnapshot(nextSource,
            prepared.Operation.MarketRevisionAfter, prepared.Operation.PreparedMarketRows);
        if (!IsValidSnapshot(nextSnapshot))
        {
            nextSnapshot = null;
            rejectionReason = "InvalidPreparedAdvance";
            return false;
        }

        rejectionReason = string.Empty;
        return true;
    }

    public FiniteSourceScenarioSnapshot ReadSnapshot(FiniteSourceScenarioSnapshot currentSnapshot) =>
        IsValidSnapshot(currentSnapshot) ? currentSnapshot : null;

    private bool IsValidSnapshot(FiniteSourceScenarioSnapshot snapshot)
    {
        if (snapshot == null || !HasDefinitionSource(definition, snapshot.Source)
            || snapshot.Source.Revision < definition.InitialSource.Revision
            || snapshot.Source.LastProcessedDay < definition.InitialSource.LastProcessedDay
            || snapshot.MarketRevision < definition.InitialMarketRevision)
            return false;
        long sourceRevisionDelta = snapshot.Source.Revision - definition.InitialSource.Revision;
        long marketRevisionDelta = snapshot.MarketRevision - definition.InitialMarketRevision;
        MarketStockRow currentTarget = FindTarget(snapshot.MarketRows, definition.ItemDefinitionId);
        MarketStockRow initialTarget = FindTarget(definition.InitialMarketRows, definition.ItemDefinitionId);
        if (sourceRevisionDelta != marketRevisionDelta || currentTarget == null || initialTarget == null
            || (long)currentTarget.Amount - initialTarget.Amount
                != (long)definition.InitialSource.RemainingReserve - snapshot.Source.RemainingReserve)
            return false;
        return HasCompatibleRows(snapshot.MarketRows, sourceRevisionDelta > 0);
    }

    private bool HasCompatibleRows(IReadOnlyList<MarketStockRow> rows, bool productionWasApplied)
    {
        if (rows == null || rows.Count != definition.InitialMarketRows.Count) return false;
        int targetCount = 0;
        MarketStockRow initialTarget = FindTarget(definition.InitialMarketRows, definition.ItemDefinitionId);
        for (int i = 0; i < rows.Count; i++)
        {
            MarketStockRow row = rows[i];
            MarketStockRow initial = definition.InitialMarketRows[i];
            if (row == null || initial == null || row.Kind != initial.Kind) return false;
            if (row.Kind == MarketStockRowKind.Item
                && row.ItemDefinition != null && initial.ItemDefinition != null
                && string.Equals(row.ItemDefinition.DefinitionId, definition.ItemDefinitionId, StringComparison.Ordinal))
            {
                targetCount++;
                if (initialTarget == null || row.DesiredAmount != initialTarget.DesiredAmount
                    || !SameFloatBits(row.ItemDefinition.BasePrice, initialTarget.ItemDefinition.BasePrice)
                    || row.Amount < initialTarget.Amount
                    || (productionWasApplied && !SameFloatBits(row.CurrentPrice,
                        MarketPriceCalculator.CalculateCurrentPrice(row.ItemDefinition.BasePrice,
                            row.DesiredAmount, row.Amount))))
                    return false;
                continue;
            }
            if (!SameRows(new[] { row }, new[] { initial })) return false;
        }
        return targetCount == 1;
    }

    private static MarketStockRow FindTarget(IReadOnlyList<MarketStockRow> rows, string itemId)
    {
        if (rows == null) return null;
        for (int i = 0; i < rows.Count; i++)
        {
            MarketStockRow row = rows[i];
            if (row != null && row.Kind == MarketStockRowKind.Item && row.ItemDefinition != null
                && string.Equals(row.ItemDefinition.DefinitionId, itemId, StringComparison.Ordinal))
                return row;
        }
        return null;
    }

    private static bool HasDefinitionSource(FiniteSourceScenarioDefinition definition, FiniteSourceSnapshot source) =>
        source != null
        && string.Equals(source.ProductionSourceId, definition.ProductionSourceId, StringComparison.Ordinal)
        && string.Equals(source.SettlementSemanticId, definition.SettlementSemanticId, StringComparison.Ordinal)
        && string.Equals(source.MarketStoreSemanticId, definition.MarketStoreSemanticId, StringComparison.Ordinal)
        && string.Equals(source.ItemDefinitionId, definition.ItemDefinitionId, StringComparison.Ordinal)
        && string.Equals(source.ContentRevision, definition.ContentRevision, StringComparison.Ordinal)
        && source.DailyOutputLimit == definition.DailyOutputLimit
        && source.InitialReserve == definition.InitialReserve
        && source.RemainingReserve >= 0 && source.RemainingReserve <= source.InitialReserve
        && source.Revision >= 0;

    private static bool SameSnapshot(FiniteSourceScenarioSnapshot left, FiniteSourceScenarioSnapshot right) =>
        left != null && right != null && SameSource(left.Source, right.Source)
        && left.MarketRevision == right.MarketRevision && SameRows(left.MarketRows, right.MarketRows);

    private static bool SameSource(FiniteSourceSnapshot left, FiniteSourceSnapshot right) =>
        left != null && right != null
        && string.Equals(left.ProductionSourceId, right.ProductionSourceId, StringComparison.Ordinal)
        && string.Equals(left.SettlementSemanticId, right.SettlementSemanticId, StringComparison.Ordinal)
        && string.Equals(left.MarketStoreSemanticId, right.MarketStoreSemanticId, StringComparison.Ordinal)
        && string.Equals(left.ItemDefinitionId, right.ItemDefinitionId, StringComparison.Ordinal)
        && string.Equals(left.ContentRevision, right.ContentRevision, StringComparison.Ordinal)
        && left.DailyOutputLimit == right.DailyOutputLimit && left.InitialReserve == right.InitialReserve
        && left.RemainingReserve == right.RemainingReserve && left.Revision == right.Revision
        && left.LastProcessedDay == right.LastProcessedDay;

    private static bool SameRows(IReadOnlyList<MarketStockRow> left, IReadOnlyList<MarketStockRow> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        for (int i = 0; i < left.Count; i++)
        {
            MarketStockRow a = left[i];
            MarketStockRow b = right[i];
            if (a == null || b == null || a.Kind != b.Kind || a.Amount != b.Amount
                || a.DesiredAmount != b.DesiredAmount || !SameFloatBits(a.CurrentPrice, b.CurrentPrice))
                return false;
            if (a.ItemDefinition == null || b.ItemDefinition == null)
            {
                if (a.ItemDefinition != b.ItemDefinition) return false;
            }
            else if (!string.Equals(a.ItemDefinition.DefinitionId, b.ItemDefinition.DefinitionId, StringComparison.Ordinal)
                || !SameFloatBits(a.ItemDefinition.BasePrice, b.ItemDefinition.BasePrice)) return false;
        }
        return true;
    }

    private static bool SameFloatBits(float left, float right) =>
        BitConverter.ToInt32(BitConverter.GetBytes(left), 0) == BitConverter.ToInt32(BitConverter.GetBytes(right), 0);
}
