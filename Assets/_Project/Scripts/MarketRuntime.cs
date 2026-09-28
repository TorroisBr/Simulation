using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    internal bool AddAmount(int amountToAdd)
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

    internal bool RemoveAmount(int amountToRemove)
    {
        if (amountToRemove <= 0 || amount < amountToRemove)
        {
            return false;
        }

        amount -= amountToRemove;
        return true;
    }

    internal void UpdatePrice()
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
    [NonSerialized] private ReadOnlyCollection<MarketItemRuntime> readOnlyItems;
    [NonSerialized] private long revision;

    public IReadOnlyList<MarketItemRuntime> Items => readOnlyItems ?? (readOnlyItems = (items ?? (items = new List<MarketItemRuntime>())).AsReadOnly());
    public long Revision => revision;
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

            EnsureItems().Add(new MarketItemRuntime(config.item, config.initialAmount, config.desiredAmount));
        }
    }

    public MarketItemRuntime GetItem(ItemData item)
    {
        MarketItemRuntime value = (items ?? (items = new List<MarketItemRuntime>())).Find(x => x != null && x.Item == item);
        return value == null ? null : new MarketItemRuntime(value.Item, value.Amount, value.DesiredAmount);
    }

    public int GetAmount(ItemData item)
    {
        MarketItemRuntime marketItem = (items ?? (items = new List<MarketItemRuntime>())).Find(x => x != null && x.Item == item);
        return marketItem != null ? marketItem.Amount : 0;
    }

    public float GetPrice(ItemData item)
    {
        MarketItemRuntime marketItem = (items ?? (items = new List<MarketItemRuntime>())).Find(x => x != null && x.Item == item);

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
        if (item == null || amount <= 0 || revision == long.MaxValue)
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
        revision++;
        return amount;
    }

    public bool CanAddStock(ItemData item, int amount)
    {
        if (item == null || amount <= 0 || revision == long.MaxValue)
        {
            return false;
        }

        MarketItemRuntime marketItem = (items ?? (items = new List<MarketItemRuntime>())).Find(x => x != null && x.Item == item);
        return marketItem == null || marketItem.Amount <= int.MaxValue - amount;
    }

    public int RemoveStockUpTo(ItemData item, int amount)
    {
        MarketItemRuntime marketItem = (items ?? (items = new List<MarketItemRuntime>())).Find(x => x != null && x.Item == item);

        if (marketItem == null || amount <= 0 || revision == long.MaxValue)
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
        revision++;
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
        bool changed = false;
        foreach (MarketItemRuntime item in items ?? (items = new List<MarketItemRuntime>()))
        {
            if (item != null)
            {
                float previous = item.CurrentPrice;
                item.UpdatePrice();
                changed |= previous != item.CurrentPrice;
            }
        }
        if (changed && revision < long.MaxValue) revision++;
    }

    private MarketItemRuntime GetOrCreateItem(ItemData item, int desiredAmount)
    {
        MarketItemRuntime marketItem = (items ?? (items = new List<MarketItemRuntime>())).Find(x => x != null && x.Item == item);

        if (marketItem != null)
        {
            return marketItem;
        }

        marketItem = new MarketItemRuntime(item, 0, Mathf.Max(1, desiredAmount));
        EnsureItems().Add(marketItem);
        return marketItem;
    }

    internal bool CanInstall(long expectedRevision) => revision == expectedRevision && revision < long.MaxValue;

    internal void InstallPrepared(long expectedRevision, PreparedMarketState replacement)
    {
        items = replacement.Items;
        readOnlyItems = replacement.ReadOnlyItems;
        revision = expectedRevision + 1;
    }

    internal PreparedMarketState PrepareReplacement(ItemData item, int delta, int desiredAmount)
    {
        List<MarketItemRuntime> replacement = new List<MarketItemRuntime>((items ?? (items = new List<MarketItemRuntime>())).Count + 1);
        bool found = false;
        foreach (MarketItemRuntime existing in items)
        {
            if (existing == null) continue;
            if (existing.Item != item) { replacement.Add(new MarketItemRuntime(existing.Item, existing.Amount, existing.DesiredAmount)); continue; }
            found = true;
            MarketItemRuntime updated = new MarketItemRuntime(item, existing.Amount + delta, existing.DesiredAmount);
            replacement.Add(updated);
        }
        if (!found) replacement.Add(new MarketItemRuntime(item, delta, desiredAmount));
        return new PreparedMarketState(replacement);
    }

    private List<MarketItemRuntime> EnsureItems()
    {
        if (items == null) items = new List<MarketItemRuntime>();
        readOnlyItems = null;
        return items;
    }
}

internal sealed class PreparedMarketState
{
    internal readonly List<MarketItemRuntime> Items;
    internal readonly ReadOnlyCollection<MarketItemRuntime> ReadOnlyItems;
    internal PreparedMarketState(List<MarketItemRuntime> items) { Items = items; ReadOnlyItems = items.AsReadOnly(); }
}
