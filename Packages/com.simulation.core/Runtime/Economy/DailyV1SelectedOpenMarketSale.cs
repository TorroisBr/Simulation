using System;

internal enum DailyV1SelectedOpenMarketSaleFailure
{
    None,
    InvalidInput,
    InvalidQuantity,
    InsufficientInventory,
    InvalidPrice,
    CreditRejected,
    MarketCapacity,
    AdmissionRejected,
    StalePlan,
    TransactionCommitFailed,
    PartialCommit
}

internal sealed class DailyV1SelectedOpenMarketSaleCommitResult
{
    internal bool Succeeded { get; }
    internal DailyV1SelectedOpenMarketSaleFailure Failure { get; }
    internal bool PartialCommit { get; }
    internal int AccountMutationCount { get; }
    internal int InventoryMutationCount { get; }

    internal DailyV1SelectedOpenMarketSaleCommitResult(
        bool succeeded,
        DailyV1SelectedOpenMarketSaleFailure failure,
        bool partialCommit = false,
        int accountMutationCount = 0,
        int inventoryMutationCount = 0)
    {
        Succeeded = succeeded;
        Failure = failure;
        PartialCommit = partialCommit;
        AccountMutationCount = accountMutationCount;
        InventoryMutationCount = inventoryMutationCount;
    }
}

/// <summary>
/// The existing Unity P12 adapter for the selected open-market sale. It only
/// admits and records the already-defined market operation; Core owns action
/// validation, plan creation, mutation, and partial-commit classification.
/// </summary>
internal interface IDailyV1SelectedOpenMarketSaleBoundary
{
    bool TryBegin();
    void Complete(DailyV1SelectedOpenMarketSaleCommitResult result);
    void Dispose();
}

/// <summary>The complete selected Daily-v1 open-market sale owner operation.</summary>
internal sealed class DailyV1SelectedOpenMarketSaleExecution
{
    internal DailyV1SelectedOpenMarketSalePlan Plan { get; }
    internal DailyV1SelectedOpenMarketSaleCommitResult Commit { get; }
    internal DailyV1SelectedOpenMarketSaleFailure PreparationFailure { get; }

    internal bool Prepared => Plan != null;

    private DailyV1SelectedOpenMarketSaleExecution(
        DailyV1SelectedOpenMarketSalePlan plan,
        DailyV1SelectedOpenMarketSaleCommitResult commit,
        DailyV1SelectedOpenMarketSaleFailure preparationFailure)
    {
        Plan = plan;
        Commit = commit;
        PreparationFailure = preparationFailure;
    }

    internal static DailyV1SelectedOpenMarketSaleExecution TryExecute(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        DailyV1MarketOwner market,
        DailyV1ItemDefinition item,
        int requestedQuantity,
        IDailyV1SelectedOpenMarketSaleBoundary boundary)
    {
        DailyV1SelectedOpenMarketSaleFailure preparationFailure =
            DailyV1SelectedOpenMarketSaleFailure.InvalidInput;
        if (graph == null
            || !graph.TryPrepareOpenMarketSale(npc, market, item, requestedQuantity,
                out DailyV1SelectedOpenMarketSalePlan plan,
                out preparationFailure))
            return new DailyV1SelectedOpenMarketSaleExecution(null, null,
                preparationFailure);

        DailyV1SelectedOpenMarketSaleCommitResult commit = null;
        try
        {
            commit = plan.TryCommit(boundary == null ? null : (Func<bool>)boundary.TryBegin);
            boundary?.Complete(commit);
        }
        finally
        {
            boundary?.Dispose();
        }

        return new DailyV1SelectedOpenMarketSaleExecution(plan, commit,
            DailyV1SelectedOpenMarketSaleFailure.None);
    }
}

/// <summary>
/// Prepared ordinary Daily-v1 open-market sale over the exact selected Core
/// owners. This is deliberately separate from P18-D keyed receipt execution.
/// </summary>
internal sealed class DailyV1SelectedOpenMarketSalePlan
{
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;
    private readonly DailyV1NpcRuntimeOwners npc;
    private readonly DailyV1CityRuntimeOwners city;
    private readonly DailyV1MarketOwner market;
    private readonly DailyV1ItemDefinition item;
    private readonly long expectedAccountRevision;
    private readonly long expectedInventoryRevision;
    private readonly long expectedMarketRevision;

    internal string ActorRuntimeId => npc.RuntimeId;
    internal string MarketRuntimeId => city.Counterparty.CounterpartyRuntimeId;
    internal string ItemDefinitionId => item.DefinitionId;
    internal int RequestedQuantity { get; }
    internal int Quantity { get; }
    internal float UnitPrice { get; }
    internal float TotalPrice { get; }

    private DailyV1SelectedOpenMarketSalePlan(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners city,
        DailyV1MarketOwner market,
        DailyV1ItemDefinition item,
        int requestedQuantity,
        int quantity,
        float unitPrice,
        float totalPrice)
    {
        this.graph = graph;
        this.npc = npc;
        this.city = city;
        this.market = market;
        this.item = item;
        RequestedQuantity = requestedQuantity;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalPrice = totalPrice;
        expectedAccountRevision = npc.MoneyAccount.Revision;
        expectedInventoryRevision = npc.Inventory.Revision;
        expectedMarketRevision = market.Revision;
    }

    internal static bool TryPrepare(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners city,
        DailyV1MarketOwner market,
        DailyV1ItemDefinition item,
        int requestedQuantity,
        out DailyV1SelectedOpenMarketSalePlan plan,
        out DailyV1SelectedOpenMarketSaleFailure failure)
    {
        plan = null;
        failure = DailyV1SelectedOpenMarketSaleFailure.InvalidInput;
        if (graph == null || npc == null || city == null || market == null || item == null
            || !graph.ContainsExactNpcOwner(npc)
            || !graph.ContainsExactCityOwner(city)
            || !ReferenceEquals(city.Market, market)
            || !ReferenceEquals(npc.Definitions, graph.Definitions)
            || !ReferenceEquals(city.Definitions, graph.Definitions)
            || !ReferenceEquals(market.Definitions, graph.Definitions)
            || !graph.Definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admittedItem)
            || !ReferenceEquals(item, admittedItem)
            || npc.Inventory == null
            || !ReferenceEquals(npc.Inventory.Definitions, graph.Definitions)
            || npc.MoneyAccount == null
            || npc.Lifecycle?.LifeState != NpcLifeState.Alive
            || npc.Travel == null
            || npc.Travel.IsTraveling
            || !string.Equals(npc.Travel.CurrentCityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(npc.Travel.CurrentLocationRuntimeId, city.LocationRuntimeId, StringComparison.Ordinal)
            || city.Counterparty == null
            || city.Counterparty.LiquidityMode != MarketLiquidityMode.Open)
            return false;

        if (requestedQuantity <= 0)
        {
            failure = DailyV1SelectedOpenMarketSaleFailure.InvalidQuantity;
            return false;
        }

        int quantity = UnityMathfCompatibility.Min(requestedQuantity, npc.Inventory.GetAmount(item));
        if (quantity <= 0)
        {
            failure = DailyV1SelectedOpenMarketSaleFailure.InsufficientInventory;
            return false;
        }

        float salePrice = market.GetPrice(item);
        if (!IsValidNonNegativeFiniteAmount(salePrice))
        {
            failure = DailyV1SelectedOpenMarketSaleFailure.InvalidPrice;
            return false;
        }

        float unitPrice = UnityMathfCompatibility.Max(0.01f, salePrice);
        float totalPrice = unitPrice * quantity;
        if (!IsValidNonNegativeFiniteAmount(unitPrice) || !IsValidNonNegativeFiniteAmount(totalPrice))
        {
            failure = DailyV1SelectedOpenMarketSaleFailure.InvalidPrice;
            return false;
        }

        if (!npc.MoneyAccount.CanCredit(totalPrice))
        {
            failure = DailyV1SelectedOpenMarketSaleFailure.CreditRejected;
            return false;
        }

        // The ordinary sale preserves the established credit → inventory
        // removal → market add order. If inventory removal is rejected after
        // credit, one further account revision must remain available for its
        // compensation write; otherwise reject before changing any owner.
        if (npc.MoneyAccount.Revision > long.MaxValue - 2)
        {
            failure = DailyV1SelectedOpenMarketSaleFailure.TransactionCommitFailed;
            return false;
        }

        if (!market.CanAddStock(item, quantity))
        {
            failure = DailyV1SelectedOpenMarketSaleFailure.MarketCapacity;
            return false;
        }

        if (!market.CanInstall(market.Revision))
        {
            failure = DailyV1SelectedOpenMarketSaleFailure.TransactionCommitFailed;
            return false;
        }

        plan = new DailyV1SelectedOpenMarketSalePlan(
            graph, npc, city, market, item, requestedQuantity, quantity, unitPrice, totalPrice);
        failure = DailyV1SelectedOpenMarketSaleFailure.None;
        return true;
    }

    internal DailyV1SelectedOpenMarketSaleCommitResult TryCommit(Func<bool> preCommitAdmission)
    {
        if (!IsCurrent())
            return new DailyV1SelectedOpenMarketSaleCommitResult(
                false, DailyV1SelectedOpenMarketSaleFailure.StalePlan);

        if (preCommitAdmission != null)
        {
            try
            {
                if (!preCommitAdmission())
                    return new DailyV1SelectedOpenMarketSaleCommitResult(
                        false, DailyV1SelectedOpenMarketSaleFailure.AdmissionRejected);
            }
            catch
            {
                return new DailyV1SelectedOpenMarketSaleCommitResult(
                    false, DailyV1SelectedOpenMarketSaleFailure.AdmissionRejected);
            }
        }

        if (!npc.MoneyAccount.TryCredit(TotalPrice))
            return new DailyV1SelectedOpenMarketSaleCommitResult(
                false, DailyV1SelectedOpenMarketSaleFailure.TransactionCommitFailed);

        if (!npc.Inventory.RemoveItem(item, Quantity))
        {
            bool refunded = npc.MoneyAccount.TryDebit(TotalPrice);
            return new DailyV1SelectedOpenMarketSaleCommitResult(
                false,
                DailyV1SelectedOpenMarketSaleFailure.TransactionCommitFailed,
                accountMutationCount: refunded ? 2 : 1);
        }

        if (market.AddStock(item, Quantity) != Quantity)
        {
            return new DailyV1SelectedOpenMarketSaleCommitResult(
                false,
                DailyV1SelectedOpenMarketSaleFailure.PartialCommit,
                partialCommit: true,
                accountMutationCount: 1,
                inventoryMutationCount: 1);
        }

        return new DailyV1SelectedOpenMarketSaleCommitResult(
            true,
            DailyV1SelectedOpenMarketSaleFailure.None,
            accountMutationCount: 1,
            inventoryMutationCount: 1);
    }

    private bool IsCurrent() => graph.ContainsExactNpcOwner(npc)
        && graph.ContainsExactCityOwner(city)
        && ReferenceEquals(city.Market, market)
        && ReferenceEquals(npc.Definitions, graph.Definitions)
        && ReferenceEquals(city.Definitions, graph.Definitions)
        && ReferenceEquals(market.Definitions, graph.Definitions)
        && graph.Definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admittedItem)
        && ReferenceEquals(item, admittedItem)
        && npc.Lifecycle?.LifeState == NpcLifeState.Alive
        && npc.Travel != null
        && !npc.Travel.IsTraveling
        && string.Equals(npc.Travel.CurrentCityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
        && string.Equals(npc.Travel.CurrentLocationRuntimeId, city.LocationRuntimeId, StringComparison.Ordinal)
        && npc.MoneyAccount.Revision == expectedAccountRevision
        && npc.Inventory.Revision == expectedInventoryRevision
        && market.Revision == expectedMarketRevision
        && npc.Inventory.GetAmount(item) >= Quantity
        && npc.MoneyAccount.CanCredit(TotalPrice)
        && market.CanAddStock(item, Quantity)
        && market.CanInstall(expectedMarketRevision);

    private static bool IsValidNonNegativeFiniteAmount(float amount) =>
        amount >= 0f && !float.IsNaN(amount) && !float.IsInfinity(amount);
}
