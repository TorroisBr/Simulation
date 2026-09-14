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

    public bool AddAmount(int amountToAdd)
    {
        if (amountToAdd <= 0)
        {
            return false;
        }

        if (amount > int.MaxValue - amountToAdd)
        {
            return false;
        }

        amount += amountToAdd;
        return true;
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
    [NonSerialized] private MarketCounterpartyRuntime counterparty;

    public List<MarketItemRuntime> Items => items ?? (items = new List<MarketItemRuntime>());
    public MarketCounterpartyRuntime Counterparty => counterparty ?? (counterparty = MarketCounterpartyRuntime.CreateOpen());
    public string StockOwnerRuntimeId => Counterparty.CounterpartyRuntimeId;

    public MarketRuntime()
    {
        counterparty = MarketCounterpartyRuntime.CreateOpen();
    }

    public MarketRuntime(List<MarketItemConfig> marketItems)
        : this(marketItems, null)
    {
    }

    public MarketRuntime(
        List<MarketItemConfig> marketItems,
        MarketCounterpartyRuntime counterparty)
    {
        this.counterparty = counterparty ?? MarketCounterpartyRuntime.CreateOpen();

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
        return Items.Find(x => x != null && x.Item == item);
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

    internal float GetPriceForSale(ItemData item)
    {
        MarketItemRuntime marketItem = GetItem(item);

        if (marketItem != null)
        {
            return marketItem.CurrentPrice;
        }

        return item != null ? new MarketItemRuntime(item, 0, 100).CurrentPrice : 0f;
    }

    public int AddStock(ItemData item, int amount, int desiredAmount = 100)
    {
        if (item == null || amount <= 0)
        {
            return 0;
        }

        if (CanAddStock(item, amount) == false)
        {
            return 0;
        }

        MarketItemRuntime marketItem = GetOrCreateItem(item, desiredAmount);
        if (marketItem.AddAmount(amount) == false)
        {
            return 0;
        }

        marketItem.UpdatePrice();
        return amount;
    }

    public bool CanAddStock(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return false;
        }

        MarketItemRuntime marketItem = GetItem(item);
        return marketItem == null || marketItem.Amount <= int.MaxValue - amount;
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
        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteMarketPurchase(npc, this, item, requestedAmount);
        amountBought = result.Quantity;
        unitPrice = result.UnitPrice;
        totalPrice = result.TotalPrice;
        return result.Success;
    }

    public bool SellItem(NpcRuntime npc, ItemData item, int requestedAmount, out int amountSold, out float unitPrice, out float totalPrice, out float approximateProfit)
    {
        float averageUnitCost = npc != null && item != null ? npc.Inventory.GetAverageUnitCost(item) : 0f;
        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteMarketSale(npc, this, item, requestedAmount);
        amountSold = result.Quantity;
        unitPrice = result.UnitPrice;
        totalPrice = result.TotalPrice;
        approximateProfit = result.Success ? (unitPrice - averageUnitCost) * amountSold : 0f;
        return result.Success;
    }

    public void UpdatePrices()
    {
        foreach (MarketItemRuntime item in Items)
        {
            if (item != null)
            {
                item.UpdatePrice();
            }
        }
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
