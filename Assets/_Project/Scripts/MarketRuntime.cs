using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MarketItemRuntime
{
    [SerializeField] private ItemData item;
    [SerializeField] private int amount;
    [SerializeField] private int desiredAmount;
    [SerializeField] private float currentPrice;

    public ItemData Item => item;
    public int Amount => amount;
    public int DesiredAmount => desiredAmount;
    public float CurrentPrice => currentPrice;

    public MarketItemRuntime(ItemData item, int amount, int desiredAmount)
    {
        this.item = item;
        this.amount = Mathf.Max(0, amount);
        this.desiredAmount = Mathf.Max(1, desiredAmount);
        UpdatePrice();
    }

    public void AddAmount(int amountToAdd)
    {
        if (amountToAdd <= 0)
        {
            return;
        }

        amount += amountToAdd;
    }

    public bool RemoveAmount(int amountToRemove)
    {
        if (amountToRemove <= 0 || amount < amountToRemove)
        {
            return false;
        }

        amount -= amountToRemove;
        return true;
    }

    public void UpdatePrice()
    {
        if (item == null)
        {
            currentPrice = 0f;
            return;
        }

        float basePrice = Mathf.Max(0.01f, item.basePrice);
        float stockForRatio = Mathf.Max(1, amount);
        float multiplier = desiredAmount / stockForRatio;

        currentPrice = basePrice * Mathf.Clamp(multiplier, 0.5f, 3f);
    }
}

[Serializable]
public class MarketRuntime
{
    [SerializeField] private List<MarketItemRuntime> items = new List<MarketItemRuntime>();

    public List<MarketItemRuntime> Items => items ?? (items = new List<MarketItemRuntime>());

    public MarketRuntime()
    {
    }

    public MarketRuntime(List<MarketItemConfig> marketItems)
    {
        if (marketItems == null)
        {
            return;
        }

        foreach (MarketItemConfig config in marketItems)
        {
            if (config.item == null)
            {
                continue;
            }

            Items.Add(new MarketItemRuntime(config.item, config.initialAmount, config.desiredAmount));
        }
    }

    public MarketItemRuntime GetItem(ItemData item)
    {
        return Items.Find(x => x.Item == item);
    }

    public int GetAmount(ItemData item)
    {
        MarketItemRuntime marketItem = GetItem(item);
        return marketItem != null ? marketItem.Amount : 0;
    }

    public float GetPrice(ItemData item)
    {
        MarketItemRuntime marketItem = GetItem(item);

        if (marketItem != null)
        {
            return marketItem.CurrentPrice;
        }

        return item != null ? Mathf.Max(0.01f, item.basePrice) : 0f;
    }

    public void AddStock(ItemData item, int amount, int desiredAmount = 100)
    {
        if (item == null || amount <= 0)
        {
            return;
        }

        MarketItemRuntime marketItem = GetOrCreateItem(item, desiredAmount);
        marketItem.AddAmount(amount);
        marketItem.UpdatePrice();
    }

    public int RemoveStockUpTo(ItemData item, int amount)
    {
        MarketItemRuntime marketItem = GetItem(item);

        if (marketItem == null || amount <= 0)
        {
            return 0;
        }

        int amountToRemove = Mathf.Min(amount, marketItem.Amount);

        if (amountToRemove <= 0)
        {
            return 0;
        }

        marketItem.RemoveAmount(amountToRemove);
        marketItem.UpdatePrice();
        return amountToRemove;
    }

    public bool BuyItem(NpcRuntime npc, ItemData item, int requestedAmount, out int amountBought, out float unitPrice, out float totalPrice)
    {
        return ExecuteNpcTrade(npc, item, requestedAmount, true, out amountBought, out unitPrice, out totalPrice, out _);
    }

    public bool SellItem(NpcRuntime npc, ItemData item, int requestedAmount, out int amountSold, out float unitPrice, out float totalPrice, out float approximateProfit)
    {
        return ExecuteNpcTrade(npc, item, requestedAmount, false, out amountSold, out unitPrice, out totalPrice, out approximateProfit);
    }

    public void UpdatePrices()
    {
        foreach (MarketItemRuntime item in Items)
        {
            item.UpdatePrice();
        }
    }

    private bool ExecuteNpcTrade(NpcRuntime npc, ItemData item, int requestedAmount, bool npcBuys, out int amountTraded, out float unitPrice, out float totalPrice, out float approximateProfit)
    {
        amountTraded = 0;
        unitPrice = 0f;
        totalPrice = 0f;
        approximateProfit = 0f;

        if (npc == null || item == null || requestedAmount <= 0)
        {
            return false;
        }

        MarketItemRuntime marketItem = npcBuys == true ? GetItem(item) : GetOrCreateItem(item, 100);

        if (marketItem == null)
        {
            return false;
        }

        unitPrice = Mathf.Max(0.01f, marketItem.CurrentPrice);

        if (npcBuys == true)
        {
            int affordableAmount = Mathf.FloorToInt(npc.Money / unitPrice);
            amountTraded = Mathf.Min(requestedAmount, marketItem.Amount, affordableAmount);

            if (amountTraded <= 0)
            {
                return false;
            }

            totalPrice = unitPrice * amountTraded;

            if (npc.TrySpendMoney(totalPrice) == false)
            {
                return false;
            }

            marketItem.RemoveAmount(amountTraded);
            npc.Inventory.AddItem(item, amountTraded, unitPrice);
        }
        else
        {
            amountTraded = Mathf.Min(requestedAmount, npc.Inventory.GetAmount(item));

            if (amountTraded <= 0)
            {
                return false;
            }

            float averageUnitCost = npc.Inventory.GetAverageUnitCost(item);
            totalPrice = unitPrice * amountTraded;

            if (npc.Inventory.RemoveItem(item, amountTraded) == false)
            {
                return false;
            }

            marketItem.AddAmount(amountTraded);
            npc.AddMoney(totalPrice);
            approximateProfit = (unitPrice - averageUnitCost) * amountTraded;
        }

        marketItem.UpdatePrice();
        return true;
    }

    private MarketItemRuntime GetOrCreateItem(ItemData item, int desiredAmount)
    {
        MarketItemRuntime marketItem = GetItem(item);

        if (marketItem != null)
        {
            return marketItem;
        }

        marketItem = new MarketItemRuntime(item, 0, Mathf.Max(1, desiredAmount));
        Items.Add(marketItem);
        return marketItem;
    }
}
