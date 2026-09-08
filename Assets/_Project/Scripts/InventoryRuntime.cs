using System;
using System.Collections.Generic;
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

    public void Add(int amountToAdd, float unitCost = 0f)
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

    public bool Remove(int amountToRemove)
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

    public List<InventoryItemRuntime> Items => items;

    public InventoryItemRuntime GetItem(ItemData item)
    {
        return items.Find(x => x.Item == item);
    }

    public int GetAmount(ItemData item)
    {
        InventoryItemRuntime inventoryItem = GetItem(item);
        return inventoryItem != null ? inventoryItem.Amount : 0;
    }

    public float GetAverageUnitCost(ItemData item)
    {
        InventoryItemRuntime inventoryItem = GetItem(item);
        return inventoryItem != null ? inventoryItem.AverageUnitCost : 0f;
    }

    public bool HasItem(ItemData item, int amount)
    {
        return GetAmount(item) >= amount;
    }

    public void AddItem(ItemData item, int amount, float unitCost = 0f)
    {
        if (item == null || amount <= 0)
        {
            return;
        }

        InventoryItemRuntime inventoryItem = GetItem(item);

        if (inventoryItem == null)
        {
            items.Add(new InventoryItemRuntime(item, amount, unitCost));
            return;
        }

        inventoryItem.Add(amount, unitCost);
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        InventoryItemRuntime inventoryItem = GetItem(item);

        if (inventoryItem == null || inventoryItem.Remove(amount) == false)
        {
            return false;
        }

        if (inventoryItem.Amount <= 0)
        {
            items.Remove(inventoryItem);
        }

        return true;
    }

    public bool IsEmpty()
    {
        foreach (InventoryItemRuntime item in items)
        {
            if (item.Amount > 0)
            {
                return false;
            }
        }

        return true;
    }
}
