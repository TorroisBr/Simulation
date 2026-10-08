using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[Serializable]
public class InventoryItemRuntime
{
    [SerializeField] private ItemData item;
    [SerializeField] private int amount;
    [SerializeField] private float averageUnitCost;

    public ItemData Item => item;
    public int Amount => amount;
    public float AverageUnitCost => averageUnitCost;

    public InventoryItemRuntime(ItemData item, int amount, float averageUnitCost = 0f)
    {
        this.item = item;
        this.amount = Mathf.Max(0, amount);
        this.averageUnitCost = Mathf.Max(0f, averageUnitCost);
    }

    internal void Add(int amountToAdd, float unitCost = 0f)
    {
        if (amountToAdd <= 0)
        {
            return;
        }

        if (amount <= 0)
        {
            averageUnitCost = Mathf.Max(0f, unitCost);
            amount = amountToAdd;
            return;
        }

        if (unitCost > 0f)
        {
            float currentTotalCost = averageUnitCost * amount;
            float addedTotalCost = unitCost * amountToAdd;
            averageUnitCost = (currentTotalCost + addedTotalCost) / (amount + amountToAdd);
        }

        amount += amountToAdd;
    }

    internal bool Remove(int amountToRemove)
    {
        if (amountToRemove <= 0 || amount < amountToRemove)
        {
            return false;
        }

        amount -= amountToRemove;

        if (amount <= 0)
        {
            averageUnitCost = 0f;
        }

        return true;
    }
}

[Serializable]
public class InventoryRuntime
{
    [SerializeField] private List<InventoryItemRuntime> items = new List<InventoryItemRuntime>();
    [NonSerialized] private ReadOnlyCollection<InventoryItemRuntime> readOnlyItems;
    [NonSerialized] private long revision;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;

    public IReadOnlyList<InventoryItemRuntime> Items => readOnlyItems ?? (readOnlyItems = (items ?? (items = new List<InventoryItemRuntime>())).AsReadOnly());
    public long Revision => revision;

    internal bool TryGetCensusCardinality(out int cardinality)
    {
        cardinality = 0;
        if (items == null) return false;
        cardinality = items.Count;
        return true;
    }

    public InventoryItemRuntime GetItem(ItemData item)
    {
        InventoryItemRuntime value = (items ?? (items = new List<InventoryItemRuntime>())).Find(x => x != null && x.Item == item);
        return value == null ? null : new InventoryItemRuntime(value.Item, value.Amount, value.AverageUnitCost);
    }

    public int GetAmount(ItemData item)
    {
        InventoryItemRuntime inventoryItem = (items ?? (items = new List<InventoryItemRuntime>())).Find(x => x != null && x.Item == item);
        return inventoryItem != null ? inventoryItem.Amount : 0;
    }

    public float GetAverageUnitCost(ItemData item)
    {
        InventoryItemRuntime inventoryItem = (items ?? (items = new List<InventoryItemRuntime>())).Find(x => x != null && x.Item == item);
        return inventoryItem != null ? inventoryItem.AverageUnitCost : 0f;
    }

    public bool HasItem(ItemData item, int amount)
    {
        return GetAmount(item) >= amount;
    }

    /// <summary>Creates an unpublished owner from exact rows without replaying inventory writes.</summary>
    internal static bool TryCreateFromOwnerSnapshot(
        IReadOnlyList<InventoryItemRuntime> rows,
        long ownerRevision,
        out InventoryRuntime staged)
    {
        staged = null;
        if (rows == null || ownerRevision < 0L) return false;
        InventoryRuntime candidate = new InventoryRuntime();
        HashSet<ItemData> uniqueItems = new HashSet<ItemData>();
        foreach (InventoryItemRuntime row in rows)
        {
            if (row == null || row.Item == null || string.IsNullOrWhiteSpace(row.Item.DefinitionId)
                || row.Amount < 0 || float.IsNaN(row.AverageUnitCost)
                || float.IsInfinity(row.AverageUnitCost) || row.AverageUnitCost < 0f
                || !uniqueItems.Add(row.Item))
                return false;
            candidate.items.Add(new InventoryItemRuntime(row.Item, row.Amount, row.AverageUnitCost));
        }
        candidate.revision = ownerRevision;
        candidate.readOnlyItems = candidate.items.AsReadOnly();
        staged = candidate;
        return true;
    }

    public bool CanAddItem(ItemData item, int amount, float unitCost = 0f)
    {
        if (item == null
            || amount <= 0
            || revision == long.MaxValue
            || float.IsNaN(unitCost) == true
            || float.IsInfinity(unitCost) == true
            || unitCost < 0f)
        {
            return false;
        }

        InventoryItemRuntime inventoryItem = items != null
            ? items.Find(x => x != null && x.Item == item)
            : null;

        if (inventoryItem == null || inventoryItem.Amount > int.MaxValue - amount)
        {
            return inventoryItem == null;
        }

        if (unitCost <= 0f || inventoryItem.Amount <= 0)
        {
            return true;
        }

        float currentTotalCost = inventoryItem.AverageUnitCost * inventoryItem.Amount;
        float addedTotalCost = unitCost * amount;
        float nextAverageUnitCost = (currentTotalCost + addedTotalCost) / (inventoryItem.Amount + amount);

        return float.IsNaN(nextAverageUnitCost) == false
            && float.IsInfinity(nextAverageUnitCost) == false
            && nextAverageUnitCost >= 0f;
    }

    public void AddItem(ItemData item, int amount, float unitCost = 0f)
    {
        TryAddItem(item, amount, unitCost);
    }

    public bool TryAddItem(ItemData item, int amount, float unitCost = 0f)
    {
        if (CanAddItem(item, amount, unitCost) == false || CanCommitP12Mutation() == false)
            return false;

        InventoryItemRuntime inventoryItem = items != null
            ? items.Find(x => x != null && x.Item == item)
            : null;

        if (inventoryItem == null)
        {
            EnsureItems().Add(new InventoryItemRuntime(item, amount, unitCost));
            revision++;
            NotifyP12MutationCommitted();
            return true;
        }

        inventoryItem.Add(amount, unitCost);
        revision++;
        NotifyP12MutationCommitted();
        return true;
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        InventoryItemRuntime inventoryItem = items != null
            ? items.Find(x => x != null && x.Item == item)
            : null;

        if (revision == long.MaxValue || inventoryItem == null || amount <= 0
            || inventoryItem.Amount < amount || CanCommitP12Mutation() == false)
        {
            return false;
        }

        if (inventoryItem.Remove(amount) == false) return false;

        if (inventoryItem.Amount <= 0)
        {
            EnsureItems().Remove(inventoryItem);
        }

        revision++;
        NotifyP12MutationCommitted();

        return true;
    }

    internal void BindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12MutationAdmission != null || p12MutationCommitted != null)
            throw new InvalidOperationException("InventoryRuntime is already bound to a P12 mutation boundary.");

        p12MutationAdmission = admission;
        p12MutationCommitted = committed;
    }

    internal bool UnbindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (!ReferenceEquals(p12MutationAdmission, admission)
            || !ReferenceEquals(p12MutationCommitted, committed)) return false;

        p12MutationAdmission = null;
        p12MutationCommitted = null;
        return true;
    }

    internal bool HasP12MutationBoundary => p12MutationAdmission != null || p12MutationCommitted != null;

    private bool CanCommitP12Mutation()
    {
        if (p12MutationAdmission == null) return true;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12MutationCommitted()
    {
        if (p12MutationCommitted == null) return;
        try { p12MutationCommitted(); }
        catch { }
    }

    public bool IsEmpty()
    {
        foreach (InventoryItemRuntime item in Items)
        {
            if (item != null && item.Amount > 0)
            {
                return false;
            }
        }

        return true;
    }

    internal bool CanInstall(long expectedRevision) => revision == expectedRevision && revision < long.MaxValue;

    internal void InstallPrepared(long expectedRevision, PreparedInventoryState replacement)
    {
        items = replacement.Items;
        readOnlyItems = replacement.ReadOnlyItems;
        revision = expectedRevision + 1;
    }

    internal PreparedInventoryState PrepareReplacement(ItemData item, int removed, float addCost, bool add, int added)
    {
        List<InventoryItemRuntime> replacement = new List<InventoryItemRuntime>((items ?? (items = new List<InventoryItemRuntime>())).Count + (add ? 1 : 0));
        bool found = false;
        foreach (InventoryItemRuntime existing in items)
        {
            if (existing == null)
            {
                replacement.Add(null);
                continue;
            }
            if (found || existing.Item != item)
            {
                replacement.Add(new InventoryItemRuntime(existing.Item, existing.Amount, existing.AverageUnitCost));
                continue;
            }
            found = true;
            int next = existing.Amount - removed + added;
            if (next > 0)
            {
                float cost = add ? (existing.AverageUnitCost * existing.Amount + addCost * added) / next : existing.AverageUnitCost;
                replacement.Add(new InventoryItemRuntime(item, next, cost));
            }
        }
        if (add && !found) replacement.Add(new InventoryItemRuntime(item, added, addCost));
        return new PreparedInventoryState(replacement);
    }

    private List<InventoryItemRuntime> EnsureItems()
    {
        if (items == null) items = new List<InventoryItemRuntime>();
        readOnlyItems = null;
        return items;
    }
}

internal sealed class PreparedInventoryState
{
    internal readonly List<InventoryItemRuntime> Items;
    internal readonly ReadOnlyCollection<InventoryItemRuntime> ReadOnlyItems;
    internal PreparedInventoryState(List<InventoryItemRuntime> items) { Items = items; ReadOnlyItems = items.AsReadOnly(); }
}
