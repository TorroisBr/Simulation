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

    public IReadOnlyList<InventoryItemRuntime> Items => readOnlyItems ?? (readOnlyItems = (items ?? (items = new List<InventoryItemRuntime>())).AsReadOnly());
    public long Revision => revision;

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

        InventoryItemRuntime inventoryItem = (items ?? (items = new List<InventoryItemRuntime>())).Find(x => x != null && x.Item == item);

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
        if (item == null || amount <= 0 || revision == long.MaxValue)
        {
            return;
        }

        InventoryItemRuntime inventoryItem = (items ?? (items = new List<InventoryItemRuntime>())).Find(x => x != null && x.Item == item);

        if (inventoryItem == null)
        {
            EnsureItems().Add(new InventoryItemRuntime(item, amount, unitCost));
            revision++;
            return;
        }

        inventoryItem.Add(amount, unitCost);
        revision++;
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        InventoryItemRuntime inventoryItem = (items ?? (items = new List<InventoryItemRuntime>())).Find(x => x != null && x.Item == item);

        if (revision == long.MaxValue || inventoryItem == null || inventoryItem.Remove(amount) == false)
        {
            return false;
        }

        if (inventoryItem.Amount <= 0)
        {
            EnsureItems().Remove(inventoryItem);
        }

        revision++;

        return true;
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
