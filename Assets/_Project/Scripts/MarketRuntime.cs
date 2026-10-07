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

    internal MarketItemRuntime(MarketItemRuntime source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        item = source.item;
        amount = source.amount;
        desiredAmount = source.desiredAmount;
        currentPrice = source.currentPrice;
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
        currentPrice = CalculatePrice();
    }

    internal bool WouldPriceChange() => currentPrice != CalculatePrice();

    private float CalculatePrice()
    {
        if (item == null) return 0f;

        float basePrice = Mathf.Max(0.01f, item.basePrice);
        float stockForRatio = Mathf.Max(1, amount);
        float multiplier = desiredAmount / stockForRatio;
        return basePrice * Mathf.Clamp(multiplier, 0.5f, 3f);
    }
}

[Serializable]
public class MarketRuntime
{
    [SerializeField] private List<MarketItemRuntime> items = new List<MarketItemRuntime>();
    [NonSerialized] private MarketCounterpartyRuntime counterparty;
    [NonSerialized] private ReadOnlyCollection<MarketItemRuntime> readOnlyItems;
    [NonSerialized] private long revision;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;
    [NonSerialized] private EconomyTransactionService p12TransactionService;

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
        return value == null ? null : new MarketItemRuntime(value);
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

        if (!CanCommitP12OwnerMutation()) return 0;

        MarketItemRuntime marketItem = GetOrCreateItem(item, desiredAmount);
        if (marketItem.AddAmount(amount) == false)
        {
            return 0;
        }

        marketItem.UpdatePrice();
        revision++;
        NotifyP12OwnerMutation();
        return amount;
    }

    public bool CanAddStock(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
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

        if (!CanCommitP12OwnerMutation()) return 0;

        marketItem.RemoveAmount(amountToRemove);
        marketItem.UpdatePrice();
        revision++;
        NotifyP12OwnerMutation();
        return amountToRemove;
    }

    public bool BuyItem(NpcRuntime npc, ItemData item, int requestedAmount, out int amountBought, out float unitPrice, out float totalPrice)
    {
        EconomyTransactionResult service = p12TransactionService != null
            ? p12TransactionService.TryExecuteMarketPurchase(npc, this, item, requestedAmount)
            : new EconomyTransactionService().TryExecuteMarketPurchase(npc, this, item, requestedAmount);
        amountBought = service.Quantity;
        unitPrice = service.UnitPrice;
        totalPrice = service.TotalPrice;
        return service.Success;
    }

    public bool SellItem(NpcRuntime npc, ItemData item, int requestedAmount, out int amountSold, out float unitPrice, out float totalPrice, out float approximateProfit)
    {
        float averageUnitCost = npc != null && item != null ? npc.Inventory.GetAverageUnitCost(item) : 0f;
        EconomyTransactionResult transaction = p12TransactionService != null
            ? p12TransactionService.TryExecuteMarketSale(npc, this, item, requestedAmount)
            : new EconomyTransactionService().TryExecuteMarketSale(npc, this, item, requestedAmount);
        amountSold = transaction.Quantity;
        unitPrice = transaction.UnitPrice;
        totalPrice = transaction.TotalPrice;
        approximateProfit = transaction.Success ? (unitPrice - averageUnitCost) * amountSold : 0f;
        return transaction.Success;
    }

    public void UpdatePrices()
    {
        List<MarketItemRuntime> currentItems = items ?? (items = new List<MarketItemRuntime>());
        bool changed = false;
        foreach (MarketItemRuntime item in currentItems)
        {
            changed |= item != null && item.WouldPriceChange();
        }
        if (!changed || revision == long.MaxValue || !CanCommitP12OwnerMutation()) return;

        foreach (MarketItemRuntime item in currentItems)
            if (item != null) item.UpdatePrice();

        revision++;
        NotifyP12OwnerMutation();
    }

    internal void BindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12MutationAdmission != null || p12MutationCommitted != null)
            throw new InvalidOperationException("MarketRuntime is already bound to a P12 mutation boundary.");

        p12MutationAdmission = admission;
        p12MutationCommitted = committed;
    }

    internal void BindP12TransactionService(EconomyTransactionService service)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        if (p12TransactionService != null && !ReferenceEquals(p12TransactionService, service))
            throw new InvalidOperationException("MarketRuntime cannot be bound to a different P12 transaction service.");
        p12TransactionService = service;
    }

    private bool CanCommitP12OwnerMutation()
    {
        if (p12MutationAdmission == null) return true;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12OwnerMutation()
    {
        if (p12MutationCommitted == null) return;
        try { p12MutationCommitted(); }
        catch { }
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

    internal bool CanInstall(long expectedRevision, long revisionIncrements) =>
        revision == expectedRevision && revisionIncrements >= 0
        && revisionIncrements <= long.MaxValue - expectedRevision;

    internal bool CanInstallMaterialFlow(long expectedRevision, long revisionIncrements) =>
        CanInstall(expectedRevision, revisionIncrements) && CanCommitP12OwnerMutation();

    internal void NotifyMaterialFlowInstalled() => NotifyP12OwnerMutation();

    internal void InstallPrepared(long expectedRevision, PreparedMarketState replacement)
    {
        items = replacement.Items;
        readOnlyItems = replacement.ReadOnlyItems;
        revision = expectedRevision + 1;
    }

    internal void InstallPrepared(long expectedRevision, long revisionIncrements, PreparedMarketState replacement)
    {
        items = replacement.Items;
        readOnlyItems = replacement.ReadOnlyItems;
        revision = expectedRevision + revisionIncrements;
    }

    internal PreparedMarketState CreatePreparedSnapshot()
    {
        List<MarketItemRuntime> replacement = new List<MarketItemRuntime>();
        foreach (MarketItemRuntime item in items ?? (items = new List<MarketItemRuntime>()))
            replacement.Add(item == null ? null : new MarketItemRuntime(item));
        return new PreparedMarketState(replacement);
    }

    internal PreparedMarketState PrepareReplacement(ItemData item, int delta, int desiredAmount)
    {
        List<MarketItemRuntime> replacement = new List<MarketItemRuntime>((items ?? (items = new List<MarketItemRuntime>())).Count + 1);
        bool found = false;
        foreach (MarketItemRuntime existing in items)
        {
            if (existing == null) { replacement.Add(null); continue; }
            if (!found && existing.Item == item)
            {
                found = true;
                replacement.Add(new MarketItemRuntime(item, existing.Amount + delta, existing.DesiredAmount));
                continue;
            }
            replacement.Add(new MarketItemRuntime(existing));
        }
        if (!found) replacement.Add(new MarketItemRuntime(item, delta, desiredAmount));
        return new PreparedMarketState(replacement);
    }

    internal bool TryPrepareFiniteStockIncrease(
        ItemData item,
        int quantity,
        long expectedRevision,
        out PreparedMarketState replacement,
        out int stockBefore,
        out string rejectionReason)
    {
        replacement = null;
        stockBefore = 0;
        rejectionReason = "InvalidMarketInput";
        if (item == null || quantity <= 0) return false;
        if (revision != expectedRevision)
        {
            rejectionReason = "StaleMarketRevision";
            return false;
        }
        if (revision == long.MaxValue)
        {
            rejectionReason = "MarketRevisionExhausted";
            return false;
        }
        if (!CanCommitP12OwnerMutation())
        {
            rejectionReason = "MarketMutationRejected";
            return false;
        }

        List<MarketItemRuntime> currentItems = items ?? (items = new List<MarketItemRuntime>());
        int matchCount = 0;
        MarketItemRuntime matchingItem = null;
        foreach (MarketItemRuntime current in currentItems)
        {
            if (current == null || current.Item == null
                || !string.Equals(current.Item.DefinitionId, item.DefinitionId, StringComparison.Ordinal)) continue;
            matchCount++;
            matchingItem = current;
        }
        if (matchCount != 1)
        {
            rejectionReason = "MarketRowCardinality";
            return false;
        }
        if (matchingItem.Amount > int.MaxValue - quantity)
        {
            rejectionReason = "MarketStockOverflow";
            return false;
        }

        List<MarketItemRuntime> nextItems = new List<MarketItemRuntime>(currentItems.Count);
        foreach (MarketItemRuntime current in currentItems)
        {
            if (ReferenceEquals(current, matchingItem))
            {
                MarketItemRuntime increased = new MarketItemRuntime(current);
                if (!increased.AddAmount(quantity))
                {
                    rejectionReason = "MarketStockOverflow";
                    return false;
                }
                increased.UpdatePrice();
                nextItems.Add(increased);
            }
            else
            {
                nextItems.Add(current == null ? null : new MarketItemRuntime(current));
            }
        }

        stockBefore = matchingItem.Amount;
        replacement = new PreparedMarketState(nextItems);
        rejectionReason = string.Empty;
        return true;
    }

    internal bool CanInstallFiniteStock(long expectedRevision, PreparedMarketState replacement) =>
        replacement != null && CanInstall(expectedRevision) && CanCommitP12OwnerMutation();

    internal void InstallFiniteStock(long expectedRevision, PreparedMarketState replacement) =>
        InstallPrepared(expectedRevision, replacement);

    internal void NotifyFiniteStockInstalled() => NotifyP12OwnerMutation();

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
