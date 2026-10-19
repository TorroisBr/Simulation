using System;

internal enum DailyV1SelectedOpenMarketPurchaseFailure
{
    None,
    InvalidInput,
    InvalidQuantity,
    InsufficientStock,
    InvalidPrice,
    InsufficientFunds,
    CounterpartyCreditCapacity,
    InventoryCapacity,
    AdmissionRejected,
    StalePlan,
    TransactionCommitFailed,
    PartialCommit
}

internal sealed class DailyV1SelectedOpenMarketPurchaseCommitResult
{
    internal bool Succeeded { get; }
    internal DailyV1SelectedOpenMarketPurchaseFailure Failure { get; }
    internal bool PartialCommit { get; }
    internal int BuyerAccountMutationCount { get; }
    internal int CounterpartyAccountMutationCount { get; }
    internal int InventoryMutationCount { get; }
    internal int MarketMutationCount { get; }

    internal DailyV1SelectedOpenMarketPurchaseCommitResult(
        bool succeeded, DailyV1SelectedOpenMarketPurchaseFailure failure,
        bool partialCommit = false, int buyerAccountMutationCount = 0,
        int counterpartyAccountMutationCount = 0, int inventoryMutationCount = 0,
        int marketMutationCount = 0)
    {
        Succeeded = succeeded;
        Failure = failure;
        PartialCommit = partialCommit;
        BuyerAccountMutationCount = buyerAccountMutationCount;
        CounterpartyAccountMutationCount = counterpartyAccountMutationCount;
        InventoryMutationCount = inventoryMutationCount;
        MarketMutationCount = marketMutationCount;
    }
}

internal interface IDailyV1SelectedOpenMarketPurchaseBoundary
{
    bool CanUseAccountBacked { get; }
    bool TryBegin();
    void Complete(DailyV1SelectedOpenMarketPurchaseCommitResult result);
    void Dispose();
}

internal sealed class DailyV1SelectedOpenMarketPurchaseExecution
{
    internal DailyV1SelectedOpenMarketPurchasePlan Plan { get; }
    internal DailyV1SelectedOpenMarketPurchaseCommitResult Commit { get; }
    internal DailyV1SelectedOpenMarketPurchaseFailure PreparationFailure { get; }

    private DailyV1SelectedOpenMarketPurchaseExecution(
        DailyV1SelectedOpenMarketPurchasePlan plan,
        DailyV1SelectedOpenMarketPurchaseCommitResult commit,
        DailyV1SelectedOpenMarketPurchaseFailure preparationFailure)
    { Plan = plan; Commit = commit; PreparationFailure = preparationFailure; }

    internal static DailyV1SelectedOpenMarketPurchaseExecution TryExecute(
        DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners npc,
        DailyV1MarketOwner market, DailyV1ItemDefinition item, int requestedQuantity,
        IDailyV1SelectedOpenMarketPurchaseBoundary boundary)
    {
        DailyV1SelectedOpenMarketPurchaseFailure failure = DailyV1SelectedOpenMarketPurchaseFailure.InvalidInput;
        if (graph == null || !graph.TryPrepareOpenMarketPurchase(npc, market, item,
                requestedQuantity, boundary?.CanUseAccountBacked ?? true,
                out DailyV1SelectedOpenMarketPurchasePlan plan, out failure))
            return new DailyV1SelectedOpenMarketPurchaseExecution(null, null, failure);
        DailyV1SelectedOpenMarketPurchaseCommitResult commit = null;
        try
        {
            commit = plan.TryCommit(boundary == null ? null : (Func<bool>)boundary.TryBegin);
            boundary?.Complete(commit);
        }
        finally { boundary?.Dispose(); }
        return new DailyV1SelectedOpenMarketPurchaseExecution(plan, commit,
            DailyV1SelectedOpenMarketPurchaseFailure.None);
    }
}

internal sealed class DailyV1SelectedOpenMarketPurchasePlan
{
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;
    private readonly DailyV1NpcRuntimeOwners npc;
    private readonly DailyV1CityRuntimeOwners city;
    private readonly DailyV1MarketOwner market;
    private readonly DailyV1ItemDefinition item;
    private readonly MarketLiquidityMode liquidityMode;
    private readonly long accountRevision;
    private readonly long counterpartyRevision;
    private readonly long inventoryRevision;
    private readonly long marketRevision;
    internal int Quantity { get; }
    internal float UnitPrice { get; }
    internal float TotalPrice { get; }
    internal string ActorRuntimeId => npc.RuntimeId;
    internal string MarketRuntimeId => city.Counterparty.CounterpartyRuntimeId;
    internal string ItemDefinitionId => item.DefinitionId;

    private DailyV1SelectedOpenMarketPurchasePlan(
        DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners city, DailyV1MarketOwner market,
        DailyV1ItemDefinition item, MarketLiquidityMode liquidityMode,
        int quantity, float unitPrice, float totalPrice)
    {
        this.graph = graph; this.npc = npc; this.city = city; this.market = market;
        this.item = item; this.liquidityMode = liquidityMode;
        Quantity = quantity; UnitPrice = unitPrice; TotalPrice = totalPrice;
        accountRevision = npc.MoneyAccount.Revision;
        counterpartyRevision = city.Counterparty.MoneyAccount?.Revision ?? 0L;
        inventoryRevision = npc.Inventory.Revision;
        marketRevision = market.Revision;
    }

    internal static bool TryPrepare(
        DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners city, DailyV1MarketOwner market,
        DailyV1ItemDefinition item, int requestedQuantity, bool allowAccountBacked,
        out DailyV1SelectedOpenMarketPurchasePlan plan,
        out DailyV1SelectedOpenMarketPurchaseFailure failure)
    {
        plan = null; failure = DailyV1SelectedOpenMarketPurchaseFailure.InvalidInput;
        if (graph == null || npc == null || city == null || market == null || item == null
            || !graph.ContainsExactNpcOwner(npc) || !graph.ContainsExactCityOwner(city)
            || !ReferenceEquals(city.Market, market)
            || !ReferenceEquals(npc.Definitions, graph.Definitions)
            || !ReferenceEquals(market.Definitions, graph.Definitions)
            || !graph.Definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admitted)
            || !ReferenceEquals(item, admitted) || npc.MoneyAccount == null || npc.Inventory == null
            || !ReferenceEquals(npc.Inventory.Definitions, graph.Definitions)
            || npc.Lifecycle?.LifeState != NpcLifeState.Alive
            || npc.Travel == null || npc.Travel.IsTraveling
            || !string.Equals(npc.Travel.CurrentCityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(npc.Travel.CurrentLocationRuntimeId, city.LocationRuntimeId, StringComparison.Ordinal)
            || city.Counterparty == null) return false;

        if (requestedQuantity <= 0)
        { failure = DailyV1SelectedOpenMarketPurchaseFailure.InvalidQuantity; return false; }
        MarketLiquidityMode mode = city.Counterparty.LiquidityMode;
        if (mode == MarketLiquidityMode.AccountBacked
            && (!allowAccountBacked || city.Counterparty.MoneyAccount == null))
        {
            failure = DailyV1SelectedOpenMarketPurchaseFailure.TransactionCommitFailed;
            return false;
        }
        int stock = market.GetAmount(item);
        if (stock <= 0)
        { failure = DailyV1SelectedOpenMarketPurchaseFailure.InsufficientStock; return false; }
        float price = market.GetPrice(item);
        if (!IsFiniteNonNegative(price))
        { failure = DailyV1SelectedOpenMarketPurchaseFailure.InvalidPrice; return false; }
        float unitPrice = UnityMathfCompatibility.Max(0.01f, price);
        int affordable = UnityMathfCompatibility.FloorToInt(npc.MoneyAccount.Balance / unitPrice);
        int quantity = UnityMathfCompatibility.Min(requestedQuantity, stock, affordable);
        if (quantity <= 0)
        { failure = DailyV1SelectedOpenMarketPurchaseFailure.InsufficientFunds; return false; }
        float total = unitPrice * quantity;
        if (!IsFiniteNonNegative(total) || !npc.MoneyAccount.CanDebit(total))
        { failure = DailyV1SelectedOpenMarketPurchaseFailure.InsufficientFunds; return false; }
        if (mode == MarketLiquidityMode.AccountBacked
            && !city.Counterparty.MoneyAccount.CanCredit(total))
        { failure = DailyV1SelectedOpenMarketPurchaseFailure.CounterpartyCreditCapacity; return false; }
        if (!market.CanInstall(market.Revision)
            || !npc.Inventory.CanAddItem(item, quantity, unitPrice))
        { failure = DailyV1SelectedOpenMarketPurchaseFailure.InventoryCapacity; return false; }
        if (npc.MoneyAccount.Revision > long.MaxValue - (mode == MarketLiquidityMode.AccountBacked ? 2 : 2)
            || npc.Inventory.Revision == long.MaxValue || market.Revision == long.MaxValue
            || (mode == MarketLiquidityMode.AccountBacked
                && city.Counterparty.MoneyAccount.Revision > long.MaxValue - 2))
        { failure = DailyV1SelectedOpenMarketPurchaseFailure.TransactionCommitFailed; return false; }
        plan = new DailyV1SelectedOpenMarketPurchasePlan(graph, npc, city, market,
            item, mode, quantity, unitPrice, total);
        failure = DailyV1SelectedOpenMarketPurchaseFailure.None;
        return true;
    }

    internal DailyV1SelectedOpenMarketPurchaseCommitResult TryCommit(Func<bool> preCommitAdmission)
    {
        if (!IsCurrent()) return Result(false, DailyV1SelectedOpenMarketPurchaseFailure.StalePlan);
        if (preCommitAdmission != null)
        {
            try { if (!preCommitAdmission()) return Result(false, DailyV1SelectedOpenMarketPurchaseFailure.AdmissionRejected); }
            catch { return Result(false, DailyV1SelectedOpenMarketPurchaseFailure.AdmissionRejected); }
        }
        if (!npc.MoneyAccount.TryDebit(TotalPrice))
            return Result(false, DailyV1SelectedOpenMarketPurchaseFailure.TransactionCommitFailed);
        int buyerAccountMutations = 1;
        int counterpartyMutations = 0;
        if (liquidityMode == MarketLiquidityMode.AccountBacked
            && !city.Counterparty.MoneyAccount.TryCredit(TotalPrice))
        {
            bool refunded = npc.MoneyAccount.TryCredit(TotalPrice);
            return new DailyV1SelectedOpenMarketPurchaseCommitResult(false,
                DailyV1SelectedOpenMarketPurchaseFailure.TransactionCommitFailed,
                !refunded, refunded ? 2 : 1);
        }
        if (liquidityMode == MarketLiquidityMode.AccountBacked) counterpartyMutations = 1;

        if (market.RemoveStockUpTo(item, Quantity) != Quantity)
        {
            bool refunded = liquidityMode == MarketLiquidityMode.AccountBacked
                ? city.Counterparty.MoneyAccount.TryDebit(TotalPrice) && npc.MoneyAccount.TryCredit(TotalPrice)
                : npc.MoneyAccount.TryCredit(TotalPrice);
            return new DailyV1SelectedOpenMarketPurchaseCommitResult(false,
                DailyV1SelectedOpenMarketPurchaseFailure.TransactionCommitFailed,
                !refunded, refunded ? 2 : 1,
                liquidityMode == MarketLiquidityMode.AccountBacked ? 2 : 0,
                marketMutationCount: market.Revision > marketRevision ? 1 : 0);
        }
        if (!npc.Inventory.TryAddItem(item, Quantity, UnitPrice))
            return new DailyV1SelectedOpenMarketPurchaseCommitResult(false,
                DailyV1SelectedOpenMarketPurchaseFailure.PartialCommit, true,
                buyerAccountMutations, counterpartyMutations, 0, 1);
        return new DailyV1SelectedOpenMarketPurchaseCommitResult(true,
            DailyV1SelectedOpenMarketPurchaseFailure.None, false,
            buyerAccountMutations, counterpartyMutations, 1, 1);
    }

    private bool IsCurrent() => graph.ContainsExactNpcOwner(npc)
        && graph.ContainsExactCityOwner(city) && ReferenceEquals(city.Market, market)
        && npc.MoneyAccount.Revision == accountRevision
        && (city.Counterparty.MoneyAccount?.Revision ?? 0L) == counterpartyRevision
        && npc.Inventory.Revision == inventoryRevision && market.Revision == marketRevision
        && market.GetAmount(item) >= Quantity
        && npc.MoneyAccount.CanDebit(TotalPrice)
        && (liquidityMode != MarketLiquidityMode.AccountBacked
            || city.Counterparty.MoneyAccount.CanCredit(TotalPrice))
        && npc.Inventory.CanAddItem(item, Quantity, UnitPrice)
        && market.CanInstall(marketRevision);

    private static bool IsFiniteNonNegative(float value) =>
        value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);

    private DailyV1SelectedOpenMarketPurchaseCommitResult Result(
        bool succeeded, DailyV1SelectedOpenMarketPurchaseFailure failure) =>
        new DailyV1SelectedOpenMarketPurchaseCommitResult(succeeded, failure);
}
