using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>A copied inventory row whose item handle is the canonical Daily-v1 graph node.</summary>
public sealed class DailyV1InventoryItemState
{
    public DailyV1ItemDefinition Item { get; }
    public int Amount { get; }
    public float AverageUnitCost { get; }

    internal DailyV1InventoryItemState(DailyV1ItemDefinition item, int amount, float averageUnitCost)
    { Item = item; Amount = amount; AverageUnitCost = averageUnitCost; }
}

/// <summary>Prepared replacement for an inventory owner; installation is a single reference replacement.</summary>
public sealed class DailyV1InventoryPreparedState
{
    internal readonly List<DailyV1InventoryItemState> Rows;
    internal readonly ReadOnlyCollection<DailyV1InventoryItemState> ReadOnlyRows;
    internal DailyV1InventoryPreparedState(List<DailyV1InventoryItemState> rows)
    { Rows = rows; ReadOnlyRows = rows.AsReadOnly(); }
}

/// <summary>Authoritative, Unity-independent Daily-v1 inventory owner.</summary>
public sealed class DailyV1InventoryOwner
{
    private List<DailyV1InventoryItemState> rows = new List<DailyV1InventoryItemState>();
    private ReadOnlyCollection<DailyV1InventoryItemState> readOnlyRows;
    private readonly DailyV1EffectiveDefinitions definitions;
    private long revision;
    private Func<bool> mutationAdmission;
    private Action mutationCommitted;

    public IReadOnlyList<DailyV1InventoryItemState> Items => readOnlyRows ?? (readOnlyRows = EnsureRows().AsReadOnly());
    public long Revision => revision;
    public DailyV1EffectiveDefinitions Definitions => definitions;

    public DailyV1InventoryOwner(DailyV1EffectiveDefinitions definitions)
    { this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions)); }

    public bool TryGetCensusCardinality(out int cardinality)
    { cardinality = 0; if (rows == null) return false; cardinality = rows.Count; return true; }

    public DailyV1InventoryItemState GetItem(DailyV1ItemDefinition item)
    {
        DailyV1InventoryItemState value = Find(item);
        return value == null ? null : Copy(value);
    }
    public int GetAmount(DailyV1ItemDefinition item) => Find(item)?.Amount ?? 0;
    public float GetAverageUnitCost(DailyV1ItemDefinition item) => Find(item)?.AverageUnitCost ?? 0f;
    public bool HasItem(DailyV1ItemDefinition item, int amount) => GetAmount(item) >= amount;

    public bool CanAddItem(DailyV1ItemDefinition item, int amount, float unitCost = 0f)
    {
        if (!IsAdmitted(item) || amount <= 0 || revision == long.MaxValue
            || float.IsNaN(unitCost) || float.IsInfinity(unitCost) || unitCost < 0f) return false;
        DailyV1InventoryItemState current = Find(item);
        if (current == null || current.Amount > int.MaxValue - amount) return current == null;
        if (unitCost <= 0f || current.Amount <= 0) return true;
        int nextAmount = current.Amount + amount;
        float average = (current.AverageUnitCost * current.Amount + unitCost * amount) / nextAmount;
        return !float.IsNaN(average) && !float.IsInfinity(average) && average >= 0f;
    }

    public bool TryAddItem(DailyV1ItemDefinition item, int amount, float unitCost = 0f)
    {
        if (!CanAddItem(item, amount, unitCost) || !CanCommit()) return false;
        DailyV1InventoryItemState current = Find(item);
        List<DailyV1InventoryItemState> next = CopyRows(rows);
        if (current == null) next.Add(new DailyV1InventoryItemState(item, amount, Math.Max(0f, unitCost)));
        else
        {
            int index = FindIndex(item);
            int nextAmount = current.Amount + amount;
            float average = current.Amount <= 0 ? Math.Max(0f, unitCost)
                : unitCost > 0f ? (current.AverageUnitCost * current.Amount + unitCost * amount) / nextAmount
                : current.AverageUnitCost;
            next[index] = new DailyV1InventoryItemState(item, nextAmount, average);
        }
        rows = next; readOnlyRows = rows.AsReadOnly(); revision++; NotifyCommitted(); return true;
    }

    public bool RemoveItem(DailyV1ItemDefinition item, int amount)
    {
        DailyV1InventoryItemState current = Find(item);
        if (current == null || amount <= 0 || current.Amount < amount || revision == long.MaxValue || !CanCommit()) return false;
        List<DailyV1InventoryItemState> next = CopyRows(rows);
        int index = FindIndex(item);
        int remaining = current.Amount - amount;
        if (remaining == 0) next.RemoveAt(index);
        else next[index] = new DailyV1InventoryItemState(item, remaining, current.AverageUnitCost);
        rows = next; readOnlyRows = rows.AsReadOnly(); revision++; NotifyCommitted(); return true;
    }

    public bool IsEmpty()
    { foreach (DailyV1InventoryItemState row in Items) if (row != null && row.Amount > 0) return false; return true; }

    public bool TryBindMutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null || committed == null || mutationAdmission != null || mutationCommitted != null) return false;
        mutationAdmission = admission; mutationCommitted = committed; return true;
    }
    public bool UnbindMutationBoundary(Func<bool> admission, Action committed)
    {
        if (!ReferenceEquals(mutationAdmission, admission) || !ReferenceEquals(mutationCommitted, committed)) return false;
        mutationAdmission = null; mutationCommitted = null; return true;
    }
    public bool HasMutationBoundary => mutationAdmission != null || mutationCommitted != null;

    public bool CanInstall(long expectedRevision) => revision == expectedRevision && revision < long.MaxValue;
    public bool TryInstall(long expectedRevision, DailyV1InventoryPreparedState replacement)
    {
        if (replacement == null || !CanInstall(expectedRevision) || !IsValidPreparedState(replacement)) return false;
        rows = replacement.Rows; readOnlyRows = replacement.ReadOnlyRows; revision = expectedRevision + 1; return true;
    }

    public DailyV1InventoryPreparedState PrepareReplacement(DailyV1ItemDefinition item, int removed, float addCost, bool add, int added)
    {
        if (!IsAdmitted(item) || removed < 0 || added < 0
            || float.IsNaN(addCost) || float.IsInfinity(addCost) || addCost < 0f
            || (add && added <= 0) || (!add && added != 0)) return null;
        DailyV1InventoryItemState existingItem = Find(item);
        if (removed > 0 && (existingItem == null || removed > existingItem.Amount)) return null;
        long nextAmount = (long)(existingItem?.Amount ?? 0) - removed + added;
        if (nextAmount < 0 || nextAmount > int.MaxValue) return null;
        List<DailyV1InventoryItemState> next = new List<DailyV1InventoryItemState>((rows ?? (rows = new List<DailyV1InventoryItemState>())).Count + (add ? 1 : 0));
        bool found = false;
        foreach (DailyV1InventoryItemState existing in rows)
        {
            if (existing == null) return null;
            if (found || !ReferenceEquals(existing.Item, item)) { next.Add(Copy(existing)); continue; }
            found = true;
            int amount = (int)nextAmount;
            if (amount > 0)
            {
                float cost = add ? (existing.AverageUnitCost * existing.Amount + addCost * added) / amount : existing.AverageUnitCost;
                if (float.IsNaN(cost) || float.IsInfinity(cost) || cost < 0f) return null;
                next.Add(new DailyV1InventoryItemState(item, amount, cost));
            }
        }
        if (add && !found)
        {
            if (nextAmount <= 0) return null;
            next.Add(new DailyV1InventoryItemState(item, (int)nextAmount, addCost));
        }
        return new DailyV1InventoryPreparedState(next);
    }

    public bool TryCreatePreparedFromOwnerSnapshot(IReadOnlyList<DailyV1InventoryItemState> source, long ownerRevision,
        out DailyV1InventoryOwner staged)
    {
        staged = null;
        if (source == null || ownerRevision < 0) return false;
        DailyV1InventoryOwner candidate = new DailyV1InventoryOwner(definitions);
        HashSet<DailyV1ItemDefinition> unique = new HashSet<DailyV1ItemDefinition>(ReferenceEqualityComparerForDefinitions.Instance);
        foreach (DailyV1InventoryItemState row in source)
        {
            if (row == null || !candidate.IsAdmitted(row.Item) || row.Amount < 0
                || float.IsNaN(row.AverageUnitCost) || float.IsInfinity(row.AverageUnitCost)
                || row.AverageUnitCost < 0f || !unique.Add(row.Item)) return false;
            candidate.rows.Add(Copy(row));
        }
        candidate.revision = ownerRevision; candidate.readOnlyRows = candidate.rows.AsReadOnly(); staged = candidate; return true;
    }

    private bool IsAdmitted(DailyV1ItemDefinition item) => item != null && definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admitted) && ReferenceEquals(admitted, item);
    private bool IsValidPreparedState(DailyV1InventoryPreparedState prepared)
    {
        if (prepared.Rows == null || prepared.ReadOnlyRows == null || prepared.Rows.Count != prepared.ReadOnlyRows.Count) return false;
        for (int i = 0; i < prepared.Rows.Count; i++)
        {
            DailyV1InventoryItemState row = prepared.Rows[i];
            if (row == null || !ReferenceEquals(row, prepared.ReadOnlyRows[i]) || !IsAdmitted(row.Item)
                || row.Amount < 0 || float.IsNaN(row.AverageUnitCost) || float.IsInfinity(row.AverageUnitCost)
                || row.AverageUnitCost < 0f) return false;
            for (int earlier = 0; earlier < i; earlier++)
                if (ReferenceEquals(prepared.Rows[earlier].Item, row.Item)) return false;
        }
        return true;
    }
    private DailyV1InventoryItemState Find(DailyV1ItemDefinition item) { if (!IsAdmitted(item)) return null; foreach (DailyV1InventoryItemState row in rows ?? (rows = new List<DailyV1InventoryItemState>())) if (row != null && ReferenceEquals(row.Item, item)) return row; return null; }
    private int FindIndex(DailyV1ItemDefinition item) { for (int i = 0; i < (rows?.Count ?? 0); i++) if (rows[i] != null && ReferenceEquals(rows[i].Item, item)) return i; return -1; }
    private List<DailyV1InventoryItemState> EnsureRows() { if (rows == null) rows = new List<DailyV1InventoryItemState>(); return rows; }
    private bool CanCommit() { if (mutationAdmission == null) return true; try { return mutationAdmission(); } catch { return false; } }
    private void NotifyCommitted() { if (mutationCommitted == null) return; try { mutationCommitted(); } catch { } }
    private static DailyV1InventoryItemState Copy(DailyV1InventoryItemState row) => row == null ? null : new DailyV1InventoryItemState(row.Item, row.Amount, row.AverageUnitCost);
    private static List<DailyV1InventoryItemState> CopyRows(List<DailyV1InventoryItemState> source) { List<DailyV1InventoryItemState> copy = new List<DailyV1InventoryItemState>(source?.Count ?? 0); if (source != null) foreach (DailyV1InventoryItemState row in source) copy.Add(Copy(row)); return copy; }

    private sealed class ReferenceEqualityComparerForDefinitions : IEqualityComparer<DailyV1ItemDefinition>
    {
        internal static readonly ReferenceEqualityComparerForDefinitions Instance = new ReferenceEqualityComparerForDefinitions();
        public bool Equals(DailyV1ItemDefinition x, DailyV1ItemDefinition y) => ReferenceEquals(x, y);
        public int GetHashCode(DailyV1ItemDefinition value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
}

/// <summary>A copied market row retaining the canonical Daily-v1 item definition handle.</summary>
public sealed class DailyV1MarketItemState
{
    public DailyV1ItemDefinition Item { get; }
    public int Amount { get; }
    public int DesiredAmount { get; }
    public float CurrentPrice { get; }
    internal DailyV1MarketItemState(DailyV1ItemDefinition item, int amount, int desiredAmount, float price)
    { Item = item; Amount = amount; DesiredAmount = desiredAmount; CurrentPrice = price; }

    /// <summary>Creates a detached candidate row; the owner validates graph identity before installation.</summary>
    public static bool TryCreate(DailyV1ItemDefinition item, int amount, int desiredAmount, float currentPrice,
        out DailyV1MarketItemState state)
    {
        state = null;
        if (item == null || amount < 0 || desiredAmount <= 0 || currentPrice <= 0f
            || float.IsNaN(currentPrice) || float.IsInfinity(currentPrice)) return false;
        state = new DailyV1MarketItemState(item, amount, desiredAmount, currentPrice);
        return true;
    }
}

public sealed class DailyV1MarketPreparedState
{
    internal readonly List<DailyV1MarketItemState> Rows;
    internal readonly ReadOnlyCollection<DailyV1MarketItemState> ReadOnlyRows;
    internal DailyV1MarketPreparedState(List<DailyV1MarketItemState> rows) { Rows = rows; ReadOnlyRows = rows.AsReadOnly(); }
}

/// <summary>Authoritative, Unity-independent Daily-v1 Market stock owner.</summary>
public sealed class DailyV1MarketOwner
{
    private List<DailyV1MarketItemState> rows = new List<DailyV1MarketItemState>();
    private ReadOnlyCollection<DailyV1MarketItemState> readOnlyRows;
    private readonly DailyV1EffectiveDefinitions definitions;
    private long revision;
    private Func<bool> mutationAdmission;
    private Action mutationCommitted;
    private MarketCounterpartyRuntime counterparty;
    public IReadOnlyList<DailyV1MarketItemState> Items => readOnlyRows ?? (readOnlyRows = EnsureRows().AsReadOnly());
    public long Revision => revision;
    public DailyV1EffectiveDefinitions Definitions => definitions;
    public MarketCounterpartyRuntime Counterparty => counterparty ?? (counterparty = MarketCounterpartyRuntime.CreateOpen());

    public DailyV1MarketOwner(DailyV1EffectiveDefinitions definitions, MarketCounterpartyRuntime counterparty = null)
    { this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions)); this.counterparty = counterparty ?? MarketCounterpartyRuntime.CreateOpen(); }

    public bool TryInitializeFromCity(DailyV1CityDefinition city)
    {
        if (city == null || !definitions.Cities.TryGetValue(city.DefinitionId, out DailyV1CityDefinition admitted) || !ReferenceEquals(city, admitted) || rows.Count != 0) return false;
        List<DailyV1MarketItemState> next = new List<DailyV1MarketItemState>();
        foreach (DailyV1MarketItemConfig config in city.Values.marketItems ?? Array.Empty<DailyV1MarketItemConfig>())
        {
            if (config == null || string.IsNullOrWhiteSpace(config.item) || !definitions.Items.TryGetValue(config.item, out DailyV1ItemDefinition item)) continue;
            next.Add(new DailyV1MarketItemState(item, Math.Max(0, config.initialAmount), Math.Max(1, config.desiredAmount), CalculatePrice(item, config.initialAmount, config.desiredAmount)));
        }
        rows = next; readOnlyRows = rows.AsReadOnly(); return true;
    }

    public DailyV1MarketItemState GetItem(DailyV1ItemDefinition item) { DailyV1MarketItemState row = Find(item); return row == null ? null : Copy(row); }
    public int GetAmount(DailyV1ItemDefinition item) => Find(item)?.Amount ?? 0;
    public float GetPrice(DailyV1ItemDefinition item) { DailyV1MarketItemState row = Find(item); return row != null ? row.CurrentPrice : IsAdmitted(item) ? UnityMathfCompatibility.Max(0.01f, item.Values.basePrice) : 0f; }
    public bool CanAddStock(DailyV1ItemDefinition item, int amount) { if (!IsAdmitted(item) || amount <= 0) return false; DailyV1MarketItemState row = Find(item); return row == null || row.Amount <= int.MaxValue - amount; }

    public int AddStock(DailyV1ItemDefinition item, int amount, int desiredAmount = 100)
    {
        if (!CanAddStock(item, amount) || revision == long.MaxValue || !CanCommit()) return 0;
        List<DailyV1MarketItemState> next = CopyRows(rows); int index = FindIndex(item);
        if (index < 0) next.Add(CreateRow(item, amount, desiredAmount));
        else { DailyV1MarketItemState old = next[index]; next[index] = CreateRow(item, old.Amount + amount, old.DesiredAmount); }
        rows = next; readOnlyRows = rows.AsReadOnly(); revision++; NotifyCommitted(); return amount;
    }
    public int RemoveStockUpTo(DailyV1ItemDefinition item, int amount)
    {
        int index = FindIndex(item); if (index < 0 || amount <= 0 || revision == long.MaxValue) return 0;
        int remove = UnityMathfCompatibility.Min(amount, rows[index].Amount); if (remove <= 0 || !CanCommit()) return 0;
        List<DailyV1MarketItemState> next = CopyRows(rows); DailyV1MarketItemState old = next[index];
        next[index] = CreateRow(item, old.Amount - remove, old.DesiredAmount);
        rows = next; readOnlyRows = rows.AsReadOnly(); revision++; NotifyCommitted(); return remove;
    }
    public void UpdatePrices()
    {
        bool changed = false; foreach (DailyV1MarketItemState row in Items) changed |= row != null && row.CurrentPrice != CalculatePrice(row.Item, row.Amount, row.DesiredAmount);
        if (!changed || revision == long.MaxValue || !CanCommit()) return;
        List<DailyV1MarketItemState> next = new List<DailyV1MarketItemState>(rows.Count);
        foreach (DailyV1MarketItemState row in rows) next.Add(row == null ? null : CreateRow(row.Item, row.Amount, row.DesiredAmount));
        rows = next; readOnlyRows = rows.AsReadOnly(); revision++; NotifyCommitted();
    }
    public bool TryBindMutationBoundary(Func<bool> admission, Action committed)
    { if (admission == null || committed == null || mutationAdmission != null || mutationCommitted != null) return false; mutationAdmission = admission; mutationCommitted = committed; return true; }
    public bool UnbindMutationBoundary(Func<bool> admission, Action committed)
    { if (!ReferenceEquals(mutationAdmission, admission) || !ReferenceEquals(mutationCommitted, committed)) return false; mutationAdmission = null; mutationCommitted = null; return true; }
    public bool HasMutationBoundary => mutationAdmission != null || mutationCommitted != null;
    public bool CanInstall(long expectedRevision) => revision == expectedRevision && revision < long.MaxValue;
    public bool CanInstall(long expectedRevision, long increments) => revision == expectedRevision && increments >= 0 && increments <= long.MaxValue - expectedRevision;
    public bool CanInstallMaterialFlow(long expectedRevision, long increments) => CanInstall(expectedRevision, increments) && CanCommit();
    public bool TryInstall(long expectedRevision, long increments, DailyV1MarketPreparedState replacement)
    { if (replacement == null || increments <= 0 || !CanInstall(expectedRevision, increments) || !IsValidPreparedState(replacement)) return false; rows = replacement.Rows; readOnlyRows = replacement.ReadOnlyRows; revision = expectedRevision + increments; return true; }
    public void NotifyMaterialFlowInstalled() => NotifyCommitted();

    /// <summary>Validates and snapshots a detached replacement while preserving selected row order.</summary>
    public bool TryCreatePreparedState(IReadOnlyList<DailyV1MarketItemState> source,
        out DailyV1MarketPreparedState prepared)
    {
        prepared = null;
        if (source == null) return false;
        List<DailyV1MarketItemState> next = new List<DailyV1MarketItemState>(source.Count);
        HashSet<DailyV1ItemDefinition> unique = new HashSet<DailyV1ItemDefinition>(ReferenceEqualityComparerForMarketDefinitions.Instance);
        foreach (DailyV1MarketItemState row in source)
        {
            if (!IsValid(row) || !unique.Add(row.Item)) return false;
            next.Add(Copy(row));
        }
        prepared = new DailyV1MarketPreparedState(next);
        return true;
    }

    /// <summary>Creates a detached typed working copy without projecting through Unity-facing rows.</summary>
    public bool TryCreatePreparedSnapshot(out DailyV1MarketPreparedState prepared) =>
        TryCreatePreparedState(Items, out prepared);

    /// <summary>Reads one exact graph-node row from a validated prepared copy.</summary>
    public bool TryGetPreparedItem(DailyV1MarketPreparedState prepared, DailyV1ItemDefinition item,
        out DailyV1MarketItemState row)
    {
        row = null;
        if (!IsAdmitted(item) || !IsValidPreparedState(prepared)) return false;
        foreach (DailyV1MarketItemState candidate in prepared.Rows)
            if (ReferenceEquals(candidate.Item, item))
            { row = Copy(candidate); return true; }
        return false;
    }

    /// <summary>Counts rows by exact graph-node identity in a validated prepared copy.</summary>
    public bool TryCountPreparedItems(DailyV1MarketPreparedState prepared, DailyV1ItemDefinition item,
        out int count)
    {
        count = 0;
        if (!IsAdmitted(item) || !IsValidPreparedState(prepared)) return false;
        foreach (DailyV1MarketItemState candidate in prepared.Rows)
            if (ReferenceEquals(candidate.Item, item)) count++;
        return true;
    }

    /// <summary>Prepares a checked stock adjustment while preserving row order and the exact item node.</summary>
    public bool TryApplyPreparedStockDelta(DailyV1MarketPreparedState prepared,
        DailyV1ItemDefinition item, int delta, int desiredAmount,
        out DailyV1MarketPreparedState replacement, out DailyV1MarketItemState updatedItem)
    {
        replacement = null;
        updatedItem = null;
        if (!IsAdmitted(item) || delta == 0 || !IsValidPreparedState(prepared)) return false;
        int index = -1;
        for (int i = 0; i < prepared.Rows.Count; i++)
            if (ReferenceEquals(prepared.Rows[i].Item, item)) { index = i; break; }

        int currentAmount = index >= 0 ? prepared.Rows[index].Amount : 0;
        long nextAmount = (long)currentAmount + delta;
        if (nextAmount < 0 || nextAmount > int.MaxValue
            || (index < 0 && (delta < 0 || desiredAmount <= 0))) return false;
        int nextDesired = index >= 0 ? prepared.Rows[index].DesiredAmount : Math.Max(1, desiredAmount);
        float nextPrice = CalculatePrice(item, (int)nextAmount, nextDesired);
        if (float.IsNaN(nextPrice) || float.IsInfinity(nextPrice) || nextPrice <= 0f) return false;

        List<DailyV1MarketItemState> next = new List<DailyV1MarketItemState>(prepared.Rows.Count + (index < 0 ? 1 : 0));
        for (int i = 0; i < prepared.Rows.Count; i++)
        {
            DailyV1MarketItemState row = prepared.Rows[i];
            next.Add(i == index
                ? new DailyV1MarketItemState(item, (int)nextAmount, nextDesired, nextPrice)
                : Copy(row));
        }
        if (index < 0) next.Add(new DailyV1MarketItemState(item, (int)nextAmount, nextDesired, nextPrice));
        replacement = new DailyV1MarketPreparedState(next);
        updatedItem = Copy(next[index >= 0 ? index : next.Count - 1]);
        return true;
    }

    /// <summary>Prepares all price changes as one detached replacement.</summary>
    public bool TryPreparePriceRefresh(DailyV1MarketPreparedState prepared,
        out DailyV1MarketPreparedState replacement, out bool changed)
    {
        replacement = null;
        changed = false;
        if (!IsValidPreparedState(prepared)) return false;
        List<DailyV1MarketItemState> next = new List<DailyV1MarketItemState>(prepared.Rows.Count);
        foreach (DailyV1MarketItemState row in prepared.Rows)
        {
            float price = CalculatePrice(row.Item, row.Amount, row.DesiredAmount);
            if (float.IsNaN(price) || float.IsInfinity(price) || price <= 0f) return false;
            if (price != row.CurrentPrice)
            {
                changed = true;
                next.Add(new DailyV1MarketItemState(row.Item, row.Amount, row.DesiredAmount, price));
            }
            else next.Add(Copy(row));
        }
        replacement = new DailyV1MarketPreparedState(next);
        return true;
    }

    public DailyV1MarketPreparedState PrepareReplacement(DailyV1ItemDefinition item, int delta, int desiredAmount)
    {
        if (!IsAdmitted(item)) return null;
        List<DailyV1MarketItemState> next = new List<DailyV1MarketItemState>((rows ?? (rows = new List<DailyV1MarketItemState>())).Count + 1); bool found = false;
        foreach (DailyV1MarketItemState old in rows)
        { if (old == null) { next.Add(null); continue; } if (!found && ReferenceEquals(old.Item, item)) { found = true; next.Add(CreateRow(item, old.Amount + delta, old.DesiredAmount)); } else next.Add(Copy(old)); }
        if (!found) next.Add(CreateRow(item, delta, desiredAmount)); return new DailyV1MarketPreparedState(next);
    }

    public bool TryCreateFromOwnerSnapshot(IReadOnlyList<DailyV1MarketItemState> source, long ownerRevision,
        MarketCounterpartyRuntime snapshotCounterparty, out DailyV1MarketOwner staged)
    {
        staged = null; if (source == null || ownerRevision < 0 || snapshotCounterparty == null) return false;
        DailyV1MarketOwner candidate = new DailyV1MarketOwner(definitions, snapshotCounterparty);
        foreach (DailyV1MarketItemState row in source)
        { if (!IsValid(row)) return false; candidate.rows.Add(Copy(row)); }
        candidate.revision = ownerRevision; candidate.readOnlyRows = candidate.rows.AsReadOnly(); staged = candidate; return true;
    }

    public bool TryCreateFiniteSourceScenario(string locationId, FiniteSourceSnapshot source,
        out FiniteSourceScenarioRuntime runtime, out FiniteSourceScenarioSnapshot snapshot, out string rejectionReason)
    {
        runtime = null; snapshot = null; rejectionReason = "IdentityMismatch";
        if (source == null || string.IsNullOrWhiteSpace(locationId)) return false;
        List<MarketStockRow> stock = CaptureFiniteSourceRows();
        if (!FiniteSourceScenarioDefinition.TryCreate(locationId, source, revision, stock, out FiniteSourceScenarioDefinition definition, out rejectionReason)) return false;
        FiniteSourceScenarioSnapshot initial = new FiniteSourceScenarioSnapshot(source, revision, stock);
        if (!FiniteSourceScenarioRuntime.TryCreate(definition, initial, out runtime, out rejectionReason)) return false;
        snapshot = initial; return true;
    }

    public bool TryPrepareFiniteStockIncrease(FiniteSourceScenarioSnapshot currentSnapshot,
        PreparedFiniteSourceAdvance preparedAdvance, FiniteSourceScenarioSnapshot nextSnapshot,
        out DailyV1MarketPreparedState replacement, out string rejectionReason)
    {
        replacement = null; rejectionReason = "InvalidMarketInput";
        if (currentSnapshot == null || preparedAdvance == null || nextSnapshot == null || preparedAdvance.Status != FiniteSourceOperationStatus.Applied || preparedAdvance.Quantity <= 0) return false;
        if (revision != currentSnapshot.MarketRevision) { rejectionReason = "StaleMarketRevision"; return false; }
        if (revision == long.MaxValue) { rejectionReason = "MarketRevisionExhausted"; return false; }
        if (!CanCommit()) { rejectionReason = "MarketMutationRejected"; return false; }
        if (preparedAdvance.MarketRevisionBefore != revision || preparedAdvance.MarketRevisionAfter != revision + 1
            || nextSnapshot.MarketRevision != revision + 1 || preparedAdvance.StockBefore < 0
            || preparedAdvance.StockAfter != (long)preparedAdvance.StockBefore + preparedAdvance.Quantity)
        { rejectionReason = "MarketRowMapping"; return false; }
        List<MarketStockRow> live = CaptureFiniteSourceRows();
        if (!SameMarketRows(currentSnapshot.MarketRows, live) || nextSnapshot.MarketRows.Count != rows.Count
            || preparedAdvance.TargetRowIndex < 0 || preparedAdvance.TargetRowIndex >= rows.Count)
        { rejectionReason = "MarketRowMapping"; return false; }
        List<DailyV1MarketItemState> next = new List<DailyV1MarketItemState>(rows.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            DailyV1MarketItemState current = rows[i]; MarketStockRow prepared = nextSnapshot.MarketRows[i];
            if (current == null) { if (prepared == null || prepared.Kind != MarketStockRowKind.NullRow) return RejectMapping(out replacement, out rejectionReason); next.Add(null); continue; }
            if (current.Item == null) { if (prepared == null || prepared.Kind != MarketStockRowKind.NullItem || prepared.Amount != current.Amount || prepared.DesiredAmount != current.DesiredAmount || !SameFloatBits(prepared.CurrentPrice, current.CurrentPrice)) return RejectMapping(out replacement, out rejectionReason); next.Add(Copy(current)); continue; }
            if (prepared == null || prepared.Kind != MarketStockRowKind.Item || prepared.ItemDefinition == null
                || !string.Equals(prepared.ItemDefinition.DefinitionId, current.Item.DefinitionId, StringComparison.Ordinal)
                || !SameFloatBits(prepared.ItemDefinition.BasePrice, current.Item.Values.basePrice) || prepared.DesiredAmount != current.DesiredAmount)
                return RejectMapping(out replacement, out rejectionReason);
            if (i == preparedAdvance.TargetRowIndex)
            { if ((long)current.Amount + preparedAdvance.Quantity != prepared.Amount || current.Amount != preparedAdvance.StockBefore || prepared.Amount != preparedAdvance.StockAfter) return RejectMapping(out replacement, out rejectionReason); next.Add(new DailyV1MarketItemState(current.Item, prepared.Amount, prepared.DesiredAmount, prepared.CurrentPrice)); }
            else { if (prepared.Amount != current.Amount || !SameFloatBits(prepared.CurrentPrice, current.CurrentPrice)) return RejectMapping(out replacement, out rejectionReason); next.Add(Copy(current)); }
        }
        replacement = new DailyV1MarketPreparedState(next); rejectionReason = string.Empty; return true;
    }

    private bool IsAdmitted(DailyV1ItemDefinition item) => item != null && definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admitted) && ReferenceEquals(item, admitted);
    private DailyV1MarketItemState Find(DailyV1ItemDefinition item) { if (!IsAdmitted(item)) return null; foreach (DailyV1MarketItemState row in rows ?? (rows = new List<DailyV1MarketItemState>())) if (row != null && ReferenceEquals(row.Item, item)) return row; return null; }
    private int FindIndex(DailyV1ItemDefinition item) { for (int i = 0; i < (rows?.Count ?? 0); i++) if (rows[i] != null && ReferenceEquals(rows[i].Item, item)) return i; return -1; }
    private List<DailyV1MarketItemState> EnsureRows() { if (rows == null) rows = new List<DailyV1MarketItemState>(); return rows; }
    private bool CanCommit() { if (mutationAdmission == null) return true; try { return mutationAdmission(); } catch { return false; } }
    private void NotifyCommitted() { if (mutationCommitted == null) return; try { mutationCommitted(); } catch { } }
    private DailyV1MarketItemState CreateRow(DailyV1ItemDefinition item, int amount, int desired) => new DailyV1MarketItemState(item, Math.Max(0, amount), Math.Max(1, desired), CalculatePrice(item, amount, desired));
    private static float CalculatePrice(DailyV1ItemDefinition item, int amount, int desired) => item == null ? 0f : MarketPriceCalculator.CalculateCurrentPrice(item.Values.basePrice, Math.Max(1, desired), Math.Max(0, amount));
    private bool IsValid(DailyV1MarketItemState row) => row != null && IsAdmitted(row.Item) && row.Amount >= 0 && row.DesiredAmount > 0 && row.CurrentPrice > 0f && !float.IsNaN(row.CurrentPrice) && !float.IsInfinity(row.CurrentPrice);
    private bool IsValidPreparedState(DailyV1MarketPreparedState prepared)
    {
        if (prepared?.Rows == null || prepared.ReadOnlyRows == null
            || prepared.Rows.Count != prepared.ReadOnlyRows.Count) return false;
        HashSet<DailyV1ItemDefinition> unique = new HashSet<DailyV1ItemDefinition>(ReferenceEqualityComparerForMarketDefinitions.Instance);
        for (int i = 0; i < prepared.Rows.Count; i++)
        {
            DailyV1MarketItemState row = prepared.Rows[i];
            if (!IsValid(row) || !ReferenceEquals(row, prepared.ReadOnlyRows[i]) || !unique.Add(row.Item)) return false;
        }
        return true;
    }
    private List<MarketStockRow> CaptureFiniteSourceRows() { List<MarketStockRow> stock = new List<MarketStockRow>(Items.Count); foreach (DailyV1MarketItemState row in Items) { if (row == null) stock.Add(MarketStockRow.NullRow()); else if (row.Item == null) stock.Add(MarketStockRow.NullItem(row.Amount,row.DesiredAmount,row.CurrentPrice)); else stock.Add(MarketStockRow.Item(new MarketItemDefinition(row.Item.DefinitionId,row.Item.Values.basePrice),row.Amount,row.DesiredAmount,row.CurrentPrice)); } return stock; }
    private static bool SameMarketRows(IReadOnlyList<MarketStockRow> left,IReadOnlyList<MarketStockRow> right) { if(left==null||right==null||left.Count!=right.Count)return false; for(int i=0;i<left.Count;i++){MarketStockRow a=left[i],b=right[i]; if(a==null||b==null||a.Kind!=b.Kind||a.Amount!=b.Amount||a.DesiredAmount!=b.DesiredAmount||!SameFloatBits(a.CurrentPrice,b.CurrentPrice))return false; if(a.ItemDefinition==null||b.ItemDefinition==null){if(a.ItemDefinition!=b.ItemDefinition)return false;} else if(!string.Equals(a.ItemDefinition.DefinitionId,b.ItemDefinition.DefinitionId,StringComparison.Ordinal)||!SameFloatBits(a.ItemDefinition.BasePrice,b.ItemDefinition.BasePrice))return false;} return true; }
    private static bool SameFloatBits(float a,float b)=>BitConverter.ToInt32(BitConverter.GetBytes(a),0)==BitConverter.ToInt32(BitConverter.GetBytes(b),0);
    private static bool RejectMapping(out DailyV1MarketPreparedState replacement,out string reason){replacement=null;reason="MarketRowMapping";return false;}
    private static List<DailyV1MarketItemState> CopyRows(List<DailyV1MarketItemState> source){List<DailyV1MarketItemState> result=new List<DailyV1MarketItemState>(source?.Count??0);if(source!=null)foreach(DailyV1MarketItemState row in source)result.Add(Copy(row));return result;}
    private static DailyV1MarketItemState Copy(DailyV1MarketItemState row)=>row==null?null:new DailyV1MarketItemState(row.Item,row.Amount,row.DesiredAmount,row.CurrentPrice);

    private sealed class ReferenceEqualityComparerForMarketDefinitions : IEqualityComparer<DailyV1ItemDefinition>
    {
        internal static readonly ReferenceEqualityComparerForMarketDefinitions Instance = new ReferenceEqualityComparerForMarketDefinitions();
        public bool Equals(DailyV1ItemDefinition x, DailyV1ItemDefinition y) => ReferenceEquals(x, y);
        public int GetHashCode(DailyV1ItemDefinition value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
}
