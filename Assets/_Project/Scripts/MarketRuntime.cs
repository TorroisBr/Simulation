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
    [NonSerialized] private DailyV1ItemDefinition selectedDefinition;

    public ItemData Item => item;
    public int Amount => amount;
    public int DesiredAmount => desiredAmount;
    public float CurrentPrice => currentPrice;
    internal DailyV1ItemDefinition SelectedDefinition => selectedDefinition;

    public MarketItemRuntime(ItemData item, int amount, int desiredAmount)
    {
        this.item = item;
        this.amount = UnityMathfCompatibility.Max(0, amount);
        this.desiredAmount = UnityMathfCompatibility.Max(1, desiredAmount);
        UpdatePrice();
    }

    private MarketItemRuntime(ItemData item, int amount, int desiredAmount, float currentPrice,
        DailyV1ItemDefinition selectedDefinition = null)
    {
        this.item = item;
        this.amount = amount;
        this.desiredAmount = desiredAmount;
        this.currentPrice = currentPrice;
        this.selectedDefinition = selectedDefinition;
    }

    internal static MarketItemRuntime CreatePreparedFiniteStockRow(
        ItemData item, int amount, int desiredAmount, float currentPrice,
        DailyV1ItemDefinition selectedDefinition = null) =>
        new MarketItemRuntime(item, amount, desiredAmount, currentPrice, selectedDefinition);

    internal static bool TryCreateFromOwnerSnapshot(
        ItemData item,
        int amount,
        int desiredAmount,
        float currentPrice,
        out MarketItemRuntime snapshotRow)
    {
        snapshotRow = null;
        if (item == null
            || string.IsNullOrWhiteSpace(item.DefinitionId)
            || amount < 0
            || desiredAmount <= 0
            || currentPrice <= 0f
            || float.IsNaN(currentPrice)
            || float.IsInfinity(currentPrice))
        {
            return false;
        }

        snapshotRow = new MarketItemRuntime(item, amount, desiredAmount, currentPrice);
        return true;
    }

    internal MarketItemRuntime(MarketItemRuntime source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        item = source.item;
        amount = source.amount;
        desiredAmount = source.desiredAmount;
        currentPrice = source.currentPrice;
        selectedDefinition = source.selectedDefinition;
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
        float basePrice = selectedDefinition != null ? selectedDefinition.Values.basePrice : item.basePrice;
        return MarketPriceCalculator.CalculateCurrentPrice(basePrice, desiredAmount, amount);
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
    [NonSerialized] private DailyV1MarketOwner selectedOwner;
    [NonSerialized] private DailyV1EffectiveDefinitions selectedDefinitions;
    [NonSerialized] private DailyV1DefinitionAdapter.SelectedBindings selectedBindings;

    public IReadOnlyList<MarketItemRuntime> Items
    {
        get
        {
            if (selectedOwner == null) return readOnlyItems ?? (readOnlyItems = (items ?? (items = new List<MarketItemRuntime>())).AsReadOnly());
            List<MarketItemRuntime> projection = new List<MarketItemRuntime>(selectedOwner.Items.Count);
            if (selectedBindings == null) return projection.AsReadOnly();
            foreach (DailyV1MarketItemState row in selectedOwner.Items)
            {
                if (row == null || !selectedBindings.TryGetAsset(row.Item, out ItemData asset)) return Array.Empty<MarketItemRuntime>();
                projection.Add(MarketItemRuntime.CreatePreparedFiniteStockRow(
                    asset, row.Amount, row.DesiredAmount, row.CurrentPrice, row.Item));
            }
            return projection.AsReadOnly();
        }
    }
    public long Revision => selectedOwner != null ? selectedOwner.Revision : revision;
    public MarketCounterpartyRuntime Counterparty => selectedOwner != null ? selectedOwner.Counterparty : counterparty ?? (counterparty = MarketCounterpartyRuntime.CreateOpen());
    internal MarketCounterpartyRuntime InstalledCounterparty => counterparty;
    public string StockOwnerRuntimeId => Counterparty.CounterpartyRuntimeId;
    internal DailyV1MarketOwner SelectedDailyV1Owner => selectedOwner;

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

    internal MarketRuntime(DailyV1MarketOwner owner)
    {
        selectedOwner = owner ?? throw new ArgumentNullException(nameof(owner));
        selectedDefinitions = owner.Definitions;
        counterparty = owner.Counterparty;
    }

    /// <summary>Creates selected owner state directly from admitted Core definitions, without resolving Unity authoring assets.</summary>
    internal MarketRuntime(DailyV1CityDefinition city, DailyV1EffectiveDefinitions definitions,
        MarketCounterpartyRuntime counterparty)
        : this(CreateSelectedMarketOwner(city, definitions, counterparty, null, requireBindings: false))
    {
    }

    /// <summary>Unity compatibility constructor. The selected owner still consumes the canonical Core graph; bindings are used only by legacy ItemData projections.</summary>
    internal MarketRuntime(DailyV1CityDefinition city, DailyV1EffectiveDefinitions definitions,
        MarketCounterpartyRuntime counterparty, DailyV1DefinitionAdapter.SelectedBindings bindings)
        : this(CreateSelectedMarketOwner(city, definitions, counterparty, bindings, requireBindings: true))
    {
        selectedBindings = bindings;
    }

    private static DailyV1MarketOwner CreateSelectedMarketOwner(DailyV1CityDefinition city,
        DailyV1EffectiveDefinitions definitions, MarketCounterpartyRuntime counterparty,
        DailyV1DefinitionAdapter.SelectedBindings bindings, bool requireBindings)
    {
        if (city == null || definitions == null || (requireBindings && bindings == null)
            || !definitions.Cities.TryGetValue(city.DefinitionId, out DailyV1CityDefinition admitted)
            || !ReferenceEquals(city, admitted))
            throw new ArgumentException("A selected Market requires the admitted Daily-v1 City node and graph.");
        DailyV1MarketOwner owner = new DailyV1MarketOwner(definitions, counterparty);
        if (!owner.TryInitializeFromCity(city))
            throw new ArgumentException("The selected Market rows could not be created from the canonical definition graph.");
        return owner;
    }

    internal static bool TryCreateSelectedFromOwnerSnapshot(DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<DailyV1MarketItemState> rows, long ownerRevision,
        MarketCounterpartyRuntime snapshotCounterparty, out MarketRuntime market)
    {
        market = null;
        if (definitions == null) return false;
        DailyV1MarketOwner owner = new DailyV1MarketOwner(definitions, snapshotCounterparty);
        if (!owner.TryCreateFromOwnerSnapshot(rows, ownerRevision, snapshotCounterparty, out DailyV1MarketOwner restored)) return false;
        market = new MarketRuntime(restored);
        return true;
    }

    internal bool TryBindSelectedDailyV1Owner(DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings)
    {
        if (definitions == null || bindings == null || selectedOwner == null
            || !ReferenceEquals(selectedOwner.Definitions, definitions)
            || (selectedDefinitions != null && !ReferenceEquals(selectedDefinitions, definitions))) return false;
        selectedDefinitions = definitions; selectedBindings = bindings; return true;
    }

    private bool TryGetSelectedNode(ItemData item, out DailyV1ItemDefinition node)
    {
        node = null;
        return selectedOwner != null && selectedBindings != null && item != null
            && selectedBindings.TryGet(item, out node)
            && selectedDefinitions.Items.TryGetValue(node.DefinitionId, out DailyV1ItemDefinition admitted)
            && ReferenceEquals(node, admitted);
    }

    private MarketItemRuntime Project(DailyV1MarketItemState row)
    {
        return row != null && selectedBindings != null && selectedBindings.TryGetAsset(row.Item, out ItemData asset)
            ? MarketItemRuntime.CreatePreparedFiniteStockRow(asset, row.Amount, row.DesiredAmount, row.CurrentPrice, row.Item) : null;
    }

    internal DailyV1MarketItemState GetSelectedItem(DailyV1ItemDefinition item) => selectedOwner?.GetItem(item);
    internal int GetSelectedAmount(DailyV1ItemDefinition item) => selectedOwner?.GetAmount(item) ?? 0;
    internal float GetSelectedPrice(DailyV1ItemDefinition item) => selectedOwner?.GetPrice(item) ?? 0f;
    internal bool CanAddSelectedStock(DailyV1ItemDefinition item, int amount) => selectedOwner != null && selectedOwner.CanAddStock(item, amount);
    internal int AddSelectedStock(DailyV1ItemDefinition item, int amount, int desiredAmount = 100) =>
        selectedOwner != null ? selectedOwner.AddStock(item, amount, desiredAmount) : 0;
    internal int RemoveSelectedStock(DailyV1ItemDefinition item, int amount) =>
        selectedOwner != null ? selectedOwner.RemoveStockUpTo(item, amount) : 0;

    internal float GetSelectedPriceForSale(DailyV1ItemDefinition item)
    {
        if (selectedOwner == null || item == null) return 0f;
        DailyV1MarketItemState row = selectedOwner.GetItem(item);
        return row != null
            ? row.CurrentPrice
            : selectedOwner.Definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admitted)
                && ReferenceEquals(item, admitted)
                    ? MarketPriceCalculator.CalculateCurrentPrice(item.Values.basePrice, 100, 0)
                    : 0f;
    }

    /// <summary>Prepares selected market rows from the canonical item node, without projecting through Unity ItemData.</summary>
    internal PreparedMarketState PrepareSelectedReplacement(
        DailyV1ItemDefinition item, int delta, int desiredAmount)
    {
        if (selectedOwner == null) return new PreparedMarketState((DailyV1MarketPreparedState)null);
        return new PreparedMarketState(selectedOwner.PrepareReplacement(item, delta, desiredAmount));
    }

    internal static bool TryCreateFromOwnerSnapshot(
        IReadOnlyList<MarketItemRuntime> snapshotRows,
        long snapshotRevision,
        MarketCounterpartyRuntime snapshotCounterparty,
        out MarketRuntime market)
    {
        market = null;
        if (snapshotRows == null || snapshotRevision < 0L || snapshotCounterparty == null)
        {
            return false;
        }

        List<MarketItemRuntime> copiedRows = new List<MarketItemRuntime>(snapshotRows.Count);
        for (int i = 0; i < snapshotRows.Count; i++)
        {
            MarketItemRuntime row = snapshotRows[i];
            if (row == null
                || row.Item == null
                || string.IsNullOrWhiteSpace(row.Item.DefinitionId)
                || row.Amount < 0
                || row.DesiredAmount <= 0
                || row.CurrentPrice <= 0f
                || float.IsNaN(row.CurrentPrice)
                || float.IsInfinity(row.CurrentPrice))
            {
                return false;
            }

            copiedRows.Add(new MarketItemRuntime(row));
        }

        MarketRuntime staged = new MarketRuntime();
        staged.items = copiedRows;
        staged.readOnlyItems = copiedRows.AsReadOnly();
        staged.counterparty = snapshotCounterparty;
        staged.revision = snapshotRevision;
        market = staged;
        return true;
    }

    public MarketItemRuntime GetItem(ItemData item)
    {
        if (selectedOwner != null) return TryGetSelectedNode(item, out DailyV1ItemDefinition node) ? Project(GetSelectedItem(node)) : null;
        MarketItemRuntime value = (items ?? (items = new List<MarketItemRuntime>())).Find(x => x != null && x.Item == item);
        return value == null ? null : new MarketItemRuntime(value);
    }

    public int GetAmount(ItemData item)
    {
        if (selectedOwner != null) return TryGetSelectedNode(item, out DailyV1ItemDefinition node) ? GetSelectedAmount(node) : 0;
        MarketItemRuntime marketItem = (items ?? (items = new List<MarketItemRuntime>())).Find(x => x != null && x.Item == item);
        return marketItem != null ? marketItem.Amount : 0;
    }

    public float GetPrice(ItemData item)
    {
        if (selectedOwner != null) return TryGetSelectedNode(item, out DailyV1ItemDefinition node) ? GetSelectedPrice(node) : 0f;
        MarketItemRuntime marketItem = (items ?? (items = new List<MarketItemRuntime>())).Find(x => x != null && x.Item == item);

        if (marketItem != null)
        {
            return marketItem.CurrentPrice;
        }

        return item != null ? UnityMathfCompatibility.Max(0.01f, item.basePrice) : 0f;
    }

    internal float GetPriceForSale(ItemData item)
    {
        if (selectedOwner != null) return TryGetSelectedNode(item, out DailyV1ItemDefinition node) ? GetSelectedPrice(node) : 0f;
        MarketItemRuntime marketItem = GetItem(item);

        if (marketItem != null)
        {
            return marketItem.CurrentPrice;
        }

        return item != null ? new MarketItemRuntime(item, 0, 100).CurrentPrice : 0f;
    }

    public int AddStock(ItemData item, int amount, int desiredAmount = 100)
    {
        if (selectedOwner != null) return TryGetSelectedNode(item, out DailyV1ItemDefinition node)
            ? AddSelectedStock(node, amount, desiredAmount) : 0;
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
        if (selectedOwner != null) return TryGetSelectedNode(item, out DailyV1ItemDefinition node)
            && CanAddSelectedStock(node, amount);
        if (item == null || amount <= 0)
        {
            return false;
        }

        MarketItemRuntime marketItem = (items ?? (items = new List<MarketItemRuntime>())).Find(x => x != null && x.Item == item);
        return marketItem == null || marketItem.Amount <= int.MaxValue - amount;
    }

    public int RemoveStockUpTo(ItemData item, int amount)
    {
        if (selectedOwner != null) return TryGetSelectedNode(item, out DailyV1ItemDefinition node)
            ? RemoveSelectedStock(node, amount) : 0;
        MarketItemRuntime marketItem = (items ?? (items = new List<MarketItemRuntime>())).Find(x => x != null && x.Item == item);

        if (marketItem == null || amount <= 0 || revision == long.MaxValue)
        {
            return 0;
        }

        int amountToRemove = UnityMathfCompatibility.Min(amount, marketItem.Amount);

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

    internal EconomyTransactionResult BuySelectedItem(NpcRuntime npc, DailyV1ItemDefinition item, int requestedAmount)
    {
        return p12TransactionService != null
            ? p12TransactionService.TryExecuteMarketPurchase(npc, this, item, requestedAmount)
            : new EconomyTransactionService().TryExecuteMarketPurchase(npc, this, item, requestedAmount);
    }

    internal EconomyTransactionResult SellSelectedItem(NpcRuntime npc, DailyV1ItemDefinition item, int requestedAmount)
    {
        return p12TransactionService != null
            ? p12TransactionService.TryExecuteMarketSale(npc, this, item, requestedAmount)
            : new EconomyTransactionService().TryExecuteMarketSale(npc, this, item, requestedAmount);
    }

    public void UpdatePrices()
    {
        if (selectedOwner != null) { selectedOwner.UpdatePrices(); return; }
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
        if (selectedOwner != null)
        {
            if (!selectedOwner.TryBindMutationBoundary(admission, committed))
                throw new InvalidOperationException("Selected Daily-v1 Market owner is already bound to a P12 mutation boundary.");
            p12MutationAdmission = admission; p12MutationCommitted = committed; return;
        }
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

        marketItem = new MarketItemRuntime(item, 0, UnityMathfCompatibility.Max(1, desiredAmount));
        EnsureItems().Add(marketItem);
        return marketItem;
    }

    internal bool CanInstall(long expectedRevision) => selectedOwner != null
        ? selectedOwner.CanInstall(expectedRevision) : revision == expectedRevision && revision < long.MaxValue;

    internal bool CanInstall(long expectedRevision, long revisionIncrements) => selectedOwner != null
        ? selectedOwner.CanInstall(expectedRevision, revisionIncrements) :
        revision == expectedRevision && revisionIncrements >= 0
        && revisionIncrements <= long.MaxValue - expectedRevision;

    internal bool CanInstallMaterialFlow(long expectedRevision, long revisionIncrements) =>
        selectedOwner != null ? selectedOwner.CanInstallMaterialFlow(expectedRevision, revisionIncrements)
            : CanInstall(expectedRevision, revisionIncrements) && CanCommitP12OwnerMutation();

    internal void NotifyMaterialFlowInstalled() => NotifyP12OwnerMutation();

    internal void InstallPrepared(long expectedRevision, PreparedMarketState replacement)
    {
        if (selectedOwner != null)
        {
            if (replacement?.SelectedState == null || !selectedOwner.TryInstall(expectedRevision, 1L, replacement.SelectedState))
                throw new InvalidOperationException("Selected Daily-v1 Market prepared install was stale or invalid.");
            return;
        }
        items = replacement.Items;
        readOnlyItems = replacement.ReadOnlyItems;
        revision = expectedRevision + 1;
    }

    internal void InstallPrepared(long expectedRevision, long revisionIncrements, PreparedMarketState replacement)
    {
        if (selectedOwner != null)
        {
            DailyV1MarketPreparedState selectedReplacement = replacement?.SelectedState;
            if (selectedReplacement == null
                && !TryCreateSelectedPreparedState(replacement?.Items, out selectedReplacement))
                throw new InvalidOperationException("Selected Daily-v1 Market replacement did not map to its admitted Core definition graph.");
            if (!selectedOwner.TryInstall(expectedRevision, revisionIncrements, selectedReplacement))
                throw new InvalidOperationException("Selected Daily-v1 Market prepared install was stale or invalid.");
            return;
        }
        items = replacement.Items;
        readOnlyItems = replacement.ReadOnlyItems;
        revision = expectedRevision + revisionIncrements;
    }

    internal PreparedMarketState CreatePreparedSnapshot()
    {
        List<MarketItemRuntime> replacement = new List<MarketItemRuntime>();
        if (selectedOwner != null)
        {
            if (!selectedOwner.TryCreatePreparedSnapshot(out DailyV1MarketPreparedState selectedSnapshot))
                throw new InvalidOperationException("Selected Daily-v1 Market owner could not create a valid typed prepared snapshot.");
            return new PreparedMarketState(selectedSnapshot);
        }
        foreach (MarketItemRuntime item in items ?? (items = new List<MarketItemRuntime>()))
            replacement.Add(item == null ? null : new MarketItemRuntime(item));
        return new PreparedMarketState(replacement);
    }

    internal bool TryGetPreparedItem(PreparedMarketState state, DailyV1ItemDefinition item,
        out DailyV1MarketItemState preparedItem)
    {
        preparedItem = null;
        return selectedOwner != null && state?.SelectedState != null
            && selectedOwner.TryGetPreparedItem(state.SelectedState, item, out preparedItem);
    }

    internal bool TryCountPreparedItems(PreparedMarketState state, DailyV1ItemDefinition item,
        out int count)
    {
        count = 0;
        return selectedOwner != null && state?.SelectedState != null
            && selectedOwner.TryCountPreparedItems(state.SelectedState, item, out count);
    }

    internal bool TryApplyPreparedStockDelta(PreparedMarketState state,
        DailyV1ItemDefinition item, int delta, int desiredAmount,
        out PreparedMarketState replacement, out DailyV1MarketItemState updatedItem)
    {
        replacement = null;
        updatedItem = null;
        if (selectedOwner == null || state?.SelectedState == null
            || !selectedOwner.TryApplyPreparedStockDelta(state.SelectedState, item, delta,
                desiredAmount, out DailyV1MarketPreparedState selectedReplacement,
                out updatedItem)) return false;
        replacement = new PreparedMarketState(selectedReplacement);
        return true;
    }

    internal bool TryPreparePriceRefresh(PreparedMarketState state,
        out PreparedMarketState replacement, out bool changed)
    {
        replacement = null;
        changed = false;
        if (selectedOwner == null || state?.SelectedState == null
            || !selectedOwner.TryPreparePriceRefresh(state.SelectedState,
                out DailyV1MarketPreparedState selectedReplacement, out changed)) return false;
        replacement = new PreparedMarketState(selectedReplacement);
        return true;
    }

    private bool TryCreateSelectedPreparedState(IReadOnlyList<MarketItemRuntime> projectedRows,
        out DailyV1MarketPreparedState prepared)
    {
        prepared = null;
        if (selectedOwner == null || selectedBindings == null || selectedDefinitions == null || projectedRows == null)
            return false;
        List<DailyV1MarketItemState> rows = new List<DailyV1MarketItemState>(projectedRows.Count);
        foreach (MarketItemRuntime row in projectedRows)
        {
            if (row == null || row.Item == null || row.Amount < 0 || row.DesiredAmount <= 0
                || row.CurrentPrice <= 0f || float.IsNaN(row.CurrentPrice) || float.IsInfinity(row.CurrentPrice))
                return false;
            DailyV1ItemDefinition node = row.SelectedDefinition;
            if (node == null && !selectedBindings.TryGet(row.Item, out node)) return false;
            if (!selectedDefinitions.Items.TryGetValue(node.DefinitionId, out DailyV1ItemDefinition admitted)
                || !ReferenceEquals(admitted, node)
                || !DailyV1MarketItemState.TryCreate(node, row.Amount, row.DesiredAmount,
                    row.CurrentPrice, out DailyV1MarketItemState typedRow)) return false;
            rows.Add(typedRow);
        }
        return selectedOwner.TryCreatePreparedState(rows, out prepared);
    }

    internal PreparedMarketState PrepareReplacement(ItemData item, int delta, int desiredAmount)
    {
        if (selectedOwner != null)
        {
            if (!TryGetSelectedNode(item, out DailyV1ItemDefinition node)) return new PreparedMarketState((DailyV1MarketPreparedState)null);
            return PrepareSelectedReplacement(node, delta, desiredAmount);
        }
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

    internal bool TryCreateFiniteSourceScenario(string locationId, FiniteProductionSourceState source,
        out FiniteSourceScenarioRuntime runtime, out FiniteSourceScenarioSnapshot snapshot,
        out string rejectionReason)
    {
        runtime = null;
        snapshot = null;
        rejectionReason = "IdentityMismatch";
        if (source == null || string.IsNullOrWhiteSpace(locationId)) return false;
        if (selectedOwner != null)
        {
            FiniteSourceSnapshot selectedSource = new FiniteSourceSnapshot(source.ProductionSourceId,
                source.SettlementSemanticId, source.MarketStoreSemanticId, source.ItemDefinitionId,
                source.ContentRevision, source.DailyOutputLimit, source.InitialReserve,
                source.RemainingReserve, source.Revision, source.LastProcessedDay);
            return selectedOwner.TryCreateFiniteSourceScenario(locationId, selectedSource,
                out runtime, out snapshot, out rejectionReason);
        }

        FiniteSourceSnapshot coreSource = new FiniteSourceSnapshot(
            source.ProductionSourceId, source.SettlementSemanticId, source.MarketStoreSemanticId,
            source.ItemDefinitionId, source.ContentRevision, source.DailyOutputLimit,
            source.InitialReserve, source.RemainingReserve, source.Revision, source.LastProcessedDay);
        List<MarketStockRow> rows = CaptureFiniteSourceRows();
        if (!FiniteSourceScenarioDefinition.TryCreate(locationId, coreSource, revision, rows,
                out FiniteSourceScenarioDefinition definition, out rejectionReason))
            return false;
        FiniteSourceScenarioSnapshot initial = new FiniteSourceScenarioSnapshot(coreSource, revision, rows);
        if (!FiniteSourceScenarioRuntime.TryCreate(definition, initial, out runtime, out rejectionReason))
            return false;
        snapshot = initial;
        return true;
    }

    internal bool TryPrepareFiniteStockIncrease(
        FiniteSourceScenarioSnapshot currentSnapshot,
        PreparedFiniteSourceAdvance preparedAdvance,
        FiniteSourceScenarioSnapshot nextSnapshot,
        out PreparedMarketState replacement,
        out string rejectionReason)
    {
        if (selectedOwner != null)
        {
            if (!selectedOwner.TryPrepareFiniteStockIncrease(currentSnapshot, preparedAdvance, nextSnapshot,
                    out DailyV1MarketPreparedState selectedReplacement, out rejectionReason))
            { replacement = null; return false; }
            replacement = new PreparedMarketState(selectedReplacement); return true;
        }
        replacement = null;
        rejectionReason = "InvalidMarketInput";
        if (currentSnapshot == null || preparedAdvance == null || nextSnapshot == null
            || preparedAdvance.Status != FiniteSourceOperationStatus.Applied
            || preparedAdvance.Quantity <= 0) return false;
        if (revision != currentSnapshot.MarketRevision)
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
        if (preparedAdvance.MarketRevisionBefore != revision
            || preparedAdvance.MarketRevisionAfter != revision + 1
            || nextSnapshot.MarketRevision != revision + 1
            || preparedAdvance.StockBefore < 0
            || preparedAdvance.StockAfter != (long)preparedAdvance.StockBefore + preparedAdvance.Quantity)
        {
            rejectionReason = "MarketRowMapping";
            return false;
        }

        List<MarketItemRuntime> currentItems = items ?? (items = new List<MarketItemRuntime>());
        List<MarketStockRow> liveRows = CaptureFiniteSourceRows();
        if (!SameMarketRows(currentSnapshot.MarketRows, liveRows)
            || nextSnapshot.MarketRows.Count != currentItems.Count
            || preparedAdvance.TargetRowIndex < 0
            || preparedAdvance.TargetRowIndex >= currentItems.Count)
        {
            rejectionReason = "MarketRowMapping";
            return false;
        }

        List<MarketItemRuntime> nextItems = new List<MarketItemRuntime>(currentItems.Count);
        for (int i = 0; i < currentItems.Count; i++)
        {
            MarketItemRuntime current = currentItems[i];
            MarketStockRow prepared = nextSnapshot.MarketRows[i];
            if (current == null)
            {
                if (prepared == null || prepared.Kind != MarketStockRowKind.NullRow)
                    return RejectMarketRowMapping(out replacement, out rejectionReason);
                nextItems.Add(null);
                continue;
            }
            if (current.Item == null)
            {
                if (prepared == null || prepared.Kind != MarketStockRowKind.NullItem
                    || prepared.Amount != current.Amount || prepared.DesiredAmount != current.DesiredAmount
                    || !SameFloatBits(prepared.CurrentPrice, current.CurrentPrice))
                    return RejectMarketRowMapping(out replacement, out rejectionReason);
                nextItems.Add(new MarketItemRuntime(current));
                continue;
            }

            if (prepared == null || prepared.Kind != MarketStockRowKind.Item
                || prepared.ItemDefinition == null
                || !string.Equals(prepared.ItemDefinition.DefinitionId, current.Item.DefinitionId, StringComparison.Ordinal)
                || !SameFloatBits(prepared.ItemDefinition.BasePrice, current.Item.basePrice)
                || prepared.DesiredAmount != current.DesiredAmount)
                return RejectMarketRowMapping(out replacement, out rejectionReason);

            if (i == preparedAdvance.TargetRowIndex)
            {
                if ((long)current.Amount + preparedAdvance.Quantity != prepared.Amount
                    || current.Amount != preparedAdvance.StockBefore
                    || prepared.Amount != preparedAdvance.StockAfter)
                    return RejectMarketRowMapping(out replacement, out rejectionReason);
                nextItems.Add(MarketItemRuntime.CreatePreparedFiniteStockRow(
                    current.Item, prepared.Amount, prepared.DesiredAmount, prepared.CurrentPrice));
            }
            else
            {
                if (prepared.Amount != current.Amount
                    || !SameFloatBits(prepared.CurrentPrice, current.CurrentPrice))
                    return RejectMarketRowMapping(out replacement, out rejectionReason);
                nextItems.Add(new MarketItemRuntime(current));
            }
        }

        replacement = new PreparedMarketState(nextItems);
        rejectionReason = string.Empty;
        return true;
    }

    private List<MarketStockRow> CaptureFiniteSourceRows()
    {
        List<MarketItemRuntime> currentItems = EnsureItems();
        List<MarketStockRow> rows = new List<MarketStockRow>(currentItems.Count);
        foreach (MarketItemRuntime current in currentItems)
        {
            if (current == null) rows.Add(MarketStockRow.NullRow());
            else if (current.Item == null)
                rows.Add(MarketStockRow.NullItem(current.Amount, current.DesiredAmount, current.CurrentPrice));
            else rows.Add(MarketStockRow.Item(
                new MarketItemDefinition(current.Item.DefinitionId, current.Item.basePrice),
                current.Amount, current.DesiredAmount, current.CurrentPrice));
        }
        return rows;
    }

    private static bool SameMarketRows(IReadOnlyList<MarketStockRow> left, IReadOnlyList<MarketStockRow> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        for (int i = 0; i < left.Count; i++)
        {
            MarketStockRow a = left[i], b = right[i];
            if (a == null || b == null || a.Kind != b.Kind || a.Amount != b.Amount
                || a.DesiredAmount != b.DesiredAmount || !SameFloatBits(a.CurrentPrice, b.CurrentPrice)) return false;
            if (a.ItemDefinition == null || b.ItemDefinition == null)
            {
                if (a.ItemDefinition != b.ItemDefinition) return false;
            }
            else if (!string.Equals(a.ItemDefinition.DefinitionId, b.ItemDefinition.DefinitionId, StringComparison.Ordinal)
                || !SameFloatBits(a.ItemDefinition.BasePrice, b.ItemDefinition.BasePrice)) return false;
        }
        return true;
    }

    private static bool RejectMarketRowMapping(out PreparedMarketState replacement, out string rejectionReason)
    {
        replacement = null;
        rejectionReason = "MarketRowMapping";
        return false;
    }

    private static bool SameFloatBits(float left, float right) =>
        BitConverter.ToInt32(BitConverter.GetBytes(left), 0)
        == BitConverter.ToInt32(BitConverter.GetBytes(right), 0);

    internal bool CanInstallFiniteStock(long expectedRevision, PreparedMarketState replacement) =>
        replacement != null && CanInstall(expectedRevision) && (selectedOwner != null
            ? selectedOwner.HasMutationBoundary ? CanInstallMaterialFlow(expectedRevision, 1L) : true
            : CanCommitP12OwnerMutation());

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
    internal readonly DailyV1MarketPreparedState SelectedState;
    internal PreparedMarketState(List<MarketItemRuntime> items) { Items = items; ReadOnlyItems = items.AsReadOnly(); }
    internal PreparedMarketState(DailyV1MarketPreparedState selectedState) { SelectedState = selectedState; }
}
