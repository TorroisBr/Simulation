using System;

internal enum DailyV1SelectedNpcTradeMutation
{
    BuyerAccount,
    SellerAccount,
    BuyerInventory,
    SellerInventory
}

internal enum DailyV1SelectedNpcTradeFailure
{
    None,
    InvalidInput,
    SameAccount,
    InvalidQuantity,
    InvalidPrice,
    InsufficientInventory,
    InventoryCapacity,
    InsufficientFunds,
    CreditRejected,
    AdmissionRejected,
    TransactionCommitFailed,
    PartialCommit
}

internal sealed class DailyV1SelectedNpcTradeCommitResult
{
    internal bool Succeeded { get; }
    internal bool PartialCommit { get; }
    internal DailyV1SelectedNpcTradeFailure Failure { get; }
    internal int BuyerAccountMutationCount { get; }
    internal int SellerAccountMutationCount { get; }
    internal int BuyerInventoryMutationCount { get; }
    internal int SellerInventoryMutationCount { get; }

    internal DailyV1SelectedNpcTradeCommitResult(bool succeeded,
        DailyV1SelectedNpcTradeFailure failure, bool partialCommit = false,
        int buyerAccountMutationCount = 0, int sellerAccountMutationCount = 0,
        int buyerInventoryMutationCount = 0, int sellerInventoryMutationCount = 0)
    {
        Succeeded = succeeded; Failure = failure; PartialCommit = partialCommit;
        BuyerAccountMutationCount = buyerAccountMutationCount;
        SellerAccountMutationCount = sellerAccountMutationCount;
        BuyerInventoryMutationCount = buyerInventoryMutationCount;
        SellerInventoryMutationCount = sellerInventoryMutationCount;
    }
}

/// <summary>Unity P12 census admission and per-section mutation publication only.</summary>
internal interface IDailyV1SelectedNpcTradeBoundary
{
    bool TryBegin();
    void NotifyMutation(DailyV1SelectedNpcTradeMutation mutation);
    void Dispose();
}

/// <summary>Executes the established NPC-to-NPC owner write and compensation order.</summary>
internal static class DailyV1SelectedNpcTradeExecution
{
    internal static DailyV1SelectedNpcTradeCommitResult TryExecute(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners buyer,
        DailyV1NpcRuntimeOwners seller,
        DailyV1ItemDefinition item,
        int quantity,
        float unitPrice,
        IDailyV1SelectedNpcTradeBoundary boundary)
    {
        DailyV1SelectedNpcTradeFailure failure = DailyV1SelectedNpcTradeFailure.InvalidInput;
        if (graph == null || buyer == null || seller == null || item == null
            || !graph.ContainsExactNpcOwner(buyer) || !graph.ContainsExactNpcOwner(seller)
            || buyer.Lifecycle?.LifeState != NpcLifeState.Alive
            || seller.Lifecycle?.LifeState != NpcLifeState.Alive
            || !ReferenceEquals(buyer.Definitions, graph.Definitions)
            || !ReferenceEquals(seller.Definitions, graph.Definitions)
            || !graph.Definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admitted)
            || !ReferenceEquals(admitted, item) || buyer.Inventory == null || seller.Inventory == null
            || !ReferenceEquals(buyer.Inventory.Definitions, graph.Definitions)
            || !ReferenceEquals(seller.Inventory.Definitions, graph.Definitions)
            || buyer.MoneyAccount == null || seller.MoneyAccount == null)
            return Result(false, failure);
        if (ReferenceEquals(buyer, seller))
            return Result(false, DailyV1SelectedNpcTradeFailure.SameAccount);
        if (quantity <= 0) return Result(false, DailyV1SelectedNpcTradeFailure.InvalidQuantity);
        if (!IsFiniteNonNegative(unitPrice)) return Result(false, DailyV1SelectedNpcTradeFailure.InvalidPrice);
        float totalPrice = unitPrice * quantity;
        if (!IsFiniteNonNegative(totalPrice)) return Result(false, DailyV1SelectedNpcTradeFailure.InvalidPrice);
        if (seller.Inventory.GetAmount(item) < quantity)
            return Result(false, DailyV1SelectedNpcTradeFailure.InsufficientInventory);
        if (!buyer.Inventory.CanAddItem(item, quantity, unitPrice))
            return Result(false, DailyV1SelectedNpcTradeFailure.InventoryCapacity);
        if (!buyer.MoneyAccount.CanDebit(totalPrice))
            return Result(false, DailyV1SelectedNpcTradeFailure.InsufficientFunds);
        if (!seller.MoneyAccount.CanCredit(totalPrice))
            return Result(false, DailyV1SelectedNpcTradeFailure.CreditRejected);

        if (boundary != null)
        {
            bool admittedOperation;
            try { admittedOperation = boundary.TryBegin(); }
            catch { admittedOperation = false; }
            if (!admittedOperation) return Result(false, DailyV1SelectedNpcTradeFailure.AdmissionRejected);
        }

        int buyerAccounts = 0, sellerAccounts = 0, buyerInventories = 0, sellerInventories = 0;
        try
        {
            if (!MutateAccount(buyer.MoneyAccount, totalPrice, debit: true,
                    ref buyerAccounts, () => boundary?.NotifyMutation(DailyV1SelectedNpcTradeMutation.BuyerAccount)))
                return Result(false, DailyV1SelectedNpcTradeFailure.TransactionCommitFailed,
                    buyerAccountMutationCount: buyerAccounts);

            if (!MutateAccount(seller.MoneyAccount, totalPrice, debit: false,
                    ref sellerAccounts, () => boundary?.NotifyMutation(DailyV1SelectedNpcTradeMutation.SellerAccount)))
            {
                CompensateAccount(buyer.MoneyAccount, totalPrice, debit: false,
                    ref buyerAccounts, () => boundary?.NotifyMutation(DailyV1SelectedNpcTradeMutation.BuyerAccount));
                return Result(false, DailyV1SelectedNpcTradeFailure.TransactionCommitFailed,
                    buyerAccountMutationCount: buyerAccounts, sellerAccountMutationCount: sellerAccounts);
            }

            long sellerInventoryRevision = seller.Inventory.Revision;
            bool removed = seller.Inventory.RemoveItem(item, quantity);
            if (seller.Inventory.Revision != sellerInventoryRevision)
            {
                sellerInventories++;
                boundary?.NotifyMutation(DailyV1SelectedNpcTradeMutation.SellerInventory);
            }
            if (!removed)
            {
                CompensateAccount(seller.MoneyAccount, totalPrice, debit: true,
                    ref sellerAccounts, () => boundary?.NotifyMutation(DailyV1SelectedNpcTradeMutation.SellerAccount));
                CompensateAccount(buyer.MoneyAccount, totalPrice, debit: false,
                    ref buyerAccounts, () => boundary?.NotifyMutation(DailyV1SelectedNpcTradeMutation.BuyerAccount));
                return Result(false, DailyV1SelectedNpcTradeFailure.TransactionCommitFailed,
                    buyerAccountMutationCount: buyerAccounts, sellerAccountMutationCount: sellerAccounts,
                    sellerInventoryMutationCount: sellerInventories);
            }

            long buyerInventoryRevision = buyer.Inventory.Revision;
            bool added = buyer.Inventory.TryAddItem(item, quantity, unitPrice);
            if (buyer.Inventory.Revision != buyerInventoryRevision)
            {
                buyerInventories++;
                boundary?.NotifyMutation(DailyV1SelectedNpcTradeMutation.BuyerInventory);
            }
            if (!added)
                return Result(false, DailyV1SelectedNpcTradeFailure.PartialCommit, partialCommit: true,
                    buyerAccounts, sellerAccounts, buyerInventories, sellerInventories);

            return Result(true, DailyV1SelectedNpcTradeFailure.None,
                buyerAccountMutationCount: buyerAccounts, sellerAccountMutationCount: sellerAccounts,
                buyerInventoryMutationCount: buyerInventories, sellerInventoryMutationCount: sellerInventories);
        }
        finally { boundary?.Dispose(); }
    }

    private static bool MutateAccount(MoneyAccountRuntime account, float amount, bool debit,
        ref int mutationCount, Action notify)
    {
        long revision = account.Revision;
        bool succeeded = debit ? account.TryDebit(amount) : account.TryCredit(amount);
        if (account.Revision != revision) { mutationCount++; notify?.Invoke(); }
        return succeeded;
    }

    private static void CompensateAccount(MoneyAccountRuntime account, float amount, bool debit,
        ref int mutationCount, Action notify)
    {
        MutateAccount(account, amount, debit, ref mutationCount, notify);
    }

    private static bool IsFiniteNonNegative(float value) =>
        value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);

    private static DailyV1SelectedNpcTradeCommitResult Result(bool succeeded,
        DailyV1SelectedNpcTradeFailure failure, bool partialCommit = false,
        int buyerAccountMutationCount = 0, int sellerAccountMutationCount = 0,
        int buyerInventoryMutationCount = 0, int sellerInventoryMutationCount = 0) =>
        new DailyV1SelectedNpcTradeCommitResult(succeeded, failure, partialCommit,
            buyerAccountMutationCount, sellerAccountMutationCount,
            buyerInventoryMutationCount, sellerInventoryMutationCount);
}
