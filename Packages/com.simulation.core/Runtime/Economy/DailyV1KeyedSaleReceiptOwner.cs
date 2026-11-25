using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public enum KeyedSaleOutcome
{
    Committed,
    TerminalRejection,
    ProvenNoInstall,
    Unresolved,
    FingerprintCollision
}

public sealed class KeyedSaleSnapshot
{
    public string ActorChoiceInputId { get; }
    public string RequestId { get; }
    public string ActorPersonId { get; }
    public string ProposalId { get; }
    public string ActionSemanticId { get; }
    public int ActionVersion { get; }
    public string ProfileId { get; }
    public string LogicalTick { get; }
    public string MarketSiteId { get; }
    public string CounterpartyId { get; }
    public string ItemDefinitionId { get; }
    public MarketLiquidityMode LiquidityMode { get; }
    public float UnitPrice { get; }
    public int MarketStock { get; }
    public int SellerInventory { get; }
    public float SellerBalance { get; }
    public float CounterpartyBalance { get; }
    public int RequestedQuantity { get; }
    public int EffectiveQuantity { get; }
    public string SellerRuntimeId { get; }
    public string SellerCityRuntimeId { get; }
    public bool SellerWasAlive { get; }
    public bool SellerCouldReceive { get; }
    public bool CounterpartyCouldPay { get; }
    public bool MarketCouldAcceptStock { get; }
    public long InventoryRevision { get; }
    public long MarketRevision { get; }
    public long SellerMoneyRevision { get; }
    public long CounterpartyMoneyRevision { get; }

    internal KeyedSaleSnapshot(
        string input, string request, string actor, string proposal, string action,
        int version, string profile, string tick, string site, string sellerRuntime,
        string sellerCityRuntime, string counterparty, string item,
        MarketLiquidityMode mode, float price, int stock, int inventory,
        float sellerBalance, float counterpartyBalance, int requested, int effective,
        bool alive, bool canReceive, bool counterpartyCanPay, bool marketCanAccept,
        long inventoryRevision, long marketRevision, long sellerMoneyRevision,
        long counterpartyMoneyRevision)
    {
        ActorChoiceInputId = input;
        RequestId = request;
        ActorPersonId = actor;
        ProposalId = proposal;
        ActionSemanticId = action;
        ActionVersion = version;
        ProfileId = profile;
        LogicalTick = tick;
        MarketSiteId = site;
        SellerRuntimeId = sellerRuntime;
        SellerCityRuntimeId = sellerCityRuntime;
        CounterpartyId = counterparty;
        ItemDefinitionId = item;
        LiquidityMode = mode;
        UnitPrice = price;
        MarketStock = stock;
        SellerInventory = inventory;
        SellerBalance = sellerBalance;
        CounterpartyBalance = counterpartyBalance;
        RequestedQuantity = requested;
        EffectiveQuantity = effective;
        SellerWasAlive = alive;
        SellerCouldReceive = canReceive;
        CounterpartyCouldPay = counterpartyCanPay;
        MarketCouldAcceptStock = marketCanAccept;
        InventoryRevision = inventoryRevision;
        MarketRevision = marketRevision;
        SellerMoneyRevision = sellerMoneyRevision;
        CounterpartyMoneyRevision = counterpartyMoneyRevision;
    }
}

public sealed class KeyedSaleReceipt
{
    public string ProposalId { get; }
    public string Fingerprint { get; }
    internal string CallerFingerprint { get; }
    public KeyedSaleOutcome Outcome { get; }
    public KeyedSaleSnapshot Snapshot { get; }
    public EconomyTransactionResult Result { get; }

    private KeyedSaleReceipt(string id, string fingerprint, string callerFingerprint,
        KeyedSaleOutcome outcome, KeyedSaleSnapshot snapshot,
        EconomyTransactionResult result)
    {
        ProposalId = id;
        Fingerprint = fingerprint;
        CallerFingerprint = callerFingerprint;
        Outcome = outcome;
        Snapshot = snapshot;
        Result = result;
    }

    internal static KeyedSaleReceipt Committed(string id, string fingerprint,
        string callerFingerprint, KeyedSaleSnapshot snapshot, EconomyTransactionResult result) =>
        new KeyedSaleReceipt(id, fingerprint, callerFingerprint,
            KeyedSaleOutcome.Committed, snapshot, result);

    internal static KeyedSaleReceipt Terminal(string id, string fingerprint,
        string callerFingerprint, KeyedSaleSnapshot snapshot, EconomyTransactionResult result) =>
        new KeyedSaleReceipt(id, fingerprint, callerFingerprint,
            KeyedSaleOutcome.TerminalRejection, snapshot, result);

    internal static KeyedSaleReceipt ProvenNoInstall(string id, string fingerprint,
        string callerFingerprint, KeyedSaleSnapshot snapshot, EconomyTransactionResult result) =>
        new KeyedSaleReceipt(id, fingerprint, callerFingerprint,
            KeyedSaleOutcome.ProvenNoInstall, snapshot, result);

    internal static KeyedSaleReceipt Unresolved(string id, string fingerprint,
        string callerFingerprint, KeyedSaleSnapshot snapshot, EconomyTransactionResult result) =>
        new KeyedSaleReceipt(id, fingerprint, callerFingerprint,
            KeyedSaleOutcome.Unresolved, snapshot, result);

    internal static KeyedSaleReceipt Rejected(string id, string fingerprint,
        KeyedSaleSnapshot snapshot, EconomyTransactionFailureReason reason) =>
        Terminal(id, fingerprint, fingerprint, snapshot,
            EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale,
                MoneyEffect.ExplicitSource, reason));

    internal static KeyedSaleReceipt Collision(string id, string fingerprint,
        string callerFingerprint = null) =>
        new KeyedSaleReceipt(id, fingerprint, callerFingerprint,
            KeyedSaleOutcome.FingerprintCollision, null,
            EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale,
                MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.InvalidInput));
}

/// <summary>
/// Core-owned keyed-sale receipt authority. The selected Daily-v1 P12 profile
/// witnesses this exact owner as ExplicitlyEmpty/0 and has no receipt payload;
/// a P18-D receipt makes that profile fail closed until a separately admitted
/// persistence contract exists.
/// </summary>
internal sealed class DailyV1KeyedSaleReceiptOwner : IOwnerSectionCensusProvider
{
    internal const string SectionId = "p12e.economy-keyed-sale-receipts";
    internal const int SchemaVersion = 1;

    private readonly List<KeyedSaleReceipt> receipts = new List<KeyedSaleReceipt>();
    private long revision;

    internal int Count => receipts.Count;
    internal KeyedSaleReceipt this[int index] => receipts[index];
    internal long Revision => revision;

    public OwnerSectionCensusWitness GetCurrentCensus() =>
        new OwnerSectionCensusWitness(SectionId, SchemaVersion, this, receipts.Count, revision);

    internal KeyedSaleReceipt FindByProposalId(string proposalId)
    {
        if (proposalId == null) return null;
        for (int i = 0; i < receipts.Count; i++)
            if (string.Equals(receipts[i].ProposalId, proposalId, StringComparison.Ordinal))
                return receipts[i];
        return null;
    }

    internal bool CanStore(string proposalId)
    {
        if (revision == long.MaxValue) return false;
        return FindByProposalId(proposalId) != null || receipts.Count < int.MaxValue;
    }

    /// <summary>Reserves list capacity before any transaction owner is installed.</summary>
    internal bool PrepareStore(string proposalId)
    {
        if (!CanStore(proposalId)) return false;
        if (FindByProposalId(proposalId) == null && receipts.Capacity < receipts.Count + 1)
            receipts.Capacity = receipts.Count + 1;
        return true;
    }

    internal bool TryStore(KeyedSaleReceipt receipt)
    {
        if (receipt == null || !CanStore(receipt.ProposalId)) return false;
        long nextRevision = revision + 1L;
        for (int i = 0; i < receipts.Count; i++)
        {
            if (!string.Equals(receipts[i].ProposalId, receipt.ProposalId, StringComparison.Ordinal))
                continue;
            receipts[i] = receipt;
            revision = nextRevision;
            return true;
        }

        if (receipts.Capacity < receipts.Count + 1)
            receipts.Capacity = receipts.Count + 1;
        receipts.Add(receipt);
        revision = nextRevision;
        return true;
    }

    internal static string CreateRequestFingerprint(
        string proposalId, string callerFingerprint, string actorChoiceInputId,
        string requestId, string actorPersonId, string actionSemanticId,
        int actionVersion, string profileId, string logicalTick, string marketSiteId,
        string itemDefinitionId, int requestedQuantity) =>
        CreateCanonicalFingerprint(proposalId, callerFingerprint, actorChoiceInputId,
            requestId, actorPersonId, actionSemanticId, actionVersion, profileId,
            logicalTick, marketSiteId, itemDefinitionId, requestedQuantity);

    private static string CreateCanonicalFingerprint(params object[] values)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            string value = values[i] == null
                ? null
                : values[i] is IFormattable formattable
                    ? formattable.ToString(null, CultureInfo.InvariantCulture)
                    : values[i].ToString();
            if (value == null) builder.Append("-1:");
            else builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value);
            builder.Append('|');
        }
        return builder.ToString();
    }
}

/// <summary>
/// Executes a keyed sale against the exact selected Core owner graph. Unity
/// supplies the already admitted request identity; all mutable execution truth,
/// transaction preparation, installation, and receipt mutation remain here.
/// </summary>
internal static class DailyV1SelectedKeyedMarketSaleOperation
{
    internal static KeyedSaleReceipt TryExecute(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners seller,
        DailyV1CityRuntimeOwners city,
        DailyV1ItemDefinition item,
        string proposalId,
        string fingerprint,
        string actorChoiceInputId,
        string requestId,
        string actorPersonId,
        string actionSemanticId,
        int actionVersion,
        string profileId,
        string logicalTick,
        string marketSiteId,
        int requestedQuantity)
    {
        if (string.IsNullOrWhiteSpace(proposalId) || string.IsNullOrWhiteSpace(fingerprint))
            return KeyedSaleReceipt.Rejected(proposalId, fingerprint, null,
                EconomyTransactionFailureReason.InvalidInput);

        string canonicalFingerprint = DailyV1KeyedSaleReceiptOwner.CreateRequestFingerprint(
            proposalId, fingerprint, actorChoiceInputId, requestId, actorPersonId,
            actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            item != null ? item.DefinitionId : null, requestedQuantity);
        DailyV1KeyedSaleReceiptOwner receiptOwner = graph?.KeyedSaleReceipts;
        if (receiptOwner == null)
            return KeyedSaleReceipt.Rejected(proposalId, fingerprint, null,
                EconomyTransactionFailureReason.InvalidInput);

        KeyedSaleReceipt prior = receiptOwner.FindByProposalId(proposalId);
        if (prior != null)
        {
            if (!string.Equals(prior.Fingerprint, canonicalFingerprint, StringComparison.Ordinal))
                return KeyedSaleReceipt.Collision(proposalId, canonicalFingerprint, fingerprint);
            if (prior.Outcome != KeyedSaleOutcome.ProvenNoInstall) return prior;
        }

        if (!receiptOwner.CanStore(proposalId))
            return KeyedSaleReceipt.Rejected(proposalId, fingerprint, null,
                EconomyTransactionFailureReason.TransactionCommitFailed);

        MarketLiquidityMode mode = city?.Counterparty?.LiquidityMode ?? MarketLiquidityMode.Open;
        DailyV1MarketOwner market = city?.Market;
        MoneyAccountRuntime sellerAccount = seller?.MoneyAccount;
        MoneyAccountRuntime counterpartyAccount = mode == MarketLiquidityMode.AccountBacked
            ? city?.Counterparty?.MoneyAccount
            : null;
        long inventoryRevision = seller?.Inventory?.Revision ?? -1L;
        long marketRevision = market?.Revision ?? -1L;
        long sellerMoneyRevision = sellerAccount?.Revision ?? -1L;
        long counterpartyMoneyRevision = counterpartyAccount?.Revision ?? -1L;
        bool itemIsAdmitted = IsAdmittedItem(graph, item);
        float observedPrice = market != null && itemIsAdmitted ? market.GetPrice(item) : 0f;
        int observedStock = market != null && itemIsAdmitted ? market.GetAmount(item) : 0;
        int observedInventory = seller?.Inventory != null && itemIsAdmitted
            ? seller.Inventory.GetAmount(item)
            : 0;
        float observedSellerBalance = sellerAccount?.Balance ?? 0f;
        float observedCounterpartyBalance = counterpartyAccount?.Balance ?? 0f;

        EconomyTransactionResult result = ValidateSale(
            graph, seller, city, item, actorPersonId, actionSemanticId,
            marketSiteId, requestedQuantity, mode);
        int snapshotQuantity = result.Success
            ? result.Quantity
            : UnityMathfCompatibility.Min(UnityMathfCompatibility.Max(0, requestedQuantity), observedInventory);
        float normalizedPrice = IsValidNonNegativeFiniteAmount(observedPrice)
            ? UnityMathfCompatibility.Max(0.01f, observedPrice)
            : observedPrice;
        if (!result.Success && mode == MarketLiquidityMode.AccountBacked
            && counterpartyAccount != null && IsValidNonNegativeFiniteAmount(normalizedPrice))
            snapshotQuantity = UnityMathfCompatibility.Min(snapshotQuantity,
                GetAffordableQuantity(observedCounterpartyBalance, normalizedPrice));
        float snapshotTotal = normalizedPrice * snapshotQuantity;
        KeyedSaleSnapshot snapshot = new KeyedSaleSnapshot(
            actorChoiceInputId, requestId, actorPersonId, proposalId, actionSemanticId,
            actionVersion, profileId, logicalTick, marketSiteId,
            seller?.RuntimeId,
            seller?.Travel?.CurrentCityRuntimeId,
            city?.Counterparty?.CounterpartyRuntimeId,
            itemIsAdmitted ? item.DefinitionId : null,
            mode, observedPrice, observedStock, observedInventory, observedSellerBalance,
            observedCounterpartyBalance, requestedQuantity, snapshotQuantity,
            seller?.Lifecycle?.LifeState == NpcLifeState.Alive,
            snapshotQuantity > 0 && sellerAccount != null && sellerAccount.CanCredit(snapshotTotal),
            snapshotQuantity > 0 && mode == MarketLiquidityMode.AccountBacked
                && counterpartyAccount != null && counterpartyAccount.CanDebit(snapshotTotal),
            snapshotQuantity > 0 && market != null && itemIsAdmitted
                && market.CanAddStock(item, snapshotQuantity),
            inventoryRevision, marketRevision, sellerMoneyRevision, counterpartyMoneyRevision);

        if (!result.Success)
        {
            if (!receiptOwner.PrepareStore(proposalId))
                return KeyedSaleReceipt.Rejected(proposalId, fingerprint, snapshot,
                    EconomyTransactionFailureReason.TransactionCommitFailed);
            KeyedSaleReceipt terminal = KeyedSaleReceipt.Terminal(
                proposalId, canonicalFingerprint, fingerprint, snapshot, result);
            if (!receiptOwner.TryStore(terminal))
                return KeyedSaleReceipt.Rejected(proposalId, fingerprint, snapshot,
                    EconomyTransactionFailureReason.TransactionCommitFailed);
            return terminal;
        }

        int quantity = result.Quantity;
        float total = result.TotalPrice;
        DailyV1InventoryPreparedState inventoryReplacement = seller.Inventory.PrepareReplacement(
            item, quantity, 0f, false, 0);
        DailyV1MarketPreparedState marketReplacement = market.PrepareReplacement(item, quantity, 100);
        float sellerBalanceReplacement = sellerAccount.Balance + total;
        float counterpartyBalanceReplacement = counterpartyAccount != null
            ? counterpartyAccount.Balance - total
            : 0f;
        KeyedSaleReceipt committed = KeyedSaleReceipt.Committed(
            proposalId, canonicalFingerprint, fingerprint, snapshot, result);
        KeyedSaleReceipt unresolved = KeyedSaleReceipt.Unresolved(
            proposalId, canonicalFingerprint, fingerprint, snapshot,
            Failure(mode, EconomyTransactionFailureReason.TransactionCommitFailed, seller.RuntimeId));
        if (!receiptOwner.PrepareStore(proposalId))
            return KeyedSaleReceipt.Rejected(proposalId, fingerprint, snapshot,
                EconomyTransactionFailureReason.TransactionCommitFailed);

        bool ready = inventoryReplacement != null && marketReplacement != null
            && seller.Inventory.CanInstall(inventoryRevision)
            && market.CanInstall(marketRevision)
            && sellerAccount.CanInstall(sellerMoneyRevision, sellerBalanceReplacement)
            && (counterpartyAccount == null
                || counterpartyAccount.CanInstall(counterpartyMoneyRevision, counterpartyBalanceReplacement));
        if (!ready)
        {
            KeyedSaleReceipt notInstalled = KeyedSaleReceipt.ProvenNoInstall(
                proposalId, canonicalFingerprint, fingerprint, snapshot,
                Failure(mode, EconomyTransactionFailureReason.TransactionCommitFailed, seller.RuntimeId));
            if (!receiptOwner.TryStore(notInstalled))
                return KeyedSaleReceipt.Rejected(proposalId, fingerprint, snapshot,
                    EconomyTransactionFailureReason.TransactionCommitFailed);
            return notInstalled;
        }

        // The same four precomputed owners are installed in the established
        // inventory → market → seller account → counterparty account order.
        try
        {
            if (!seller.Inventory.TryInstall(inventoryRevision, inventoryReplacement)
                || !market.TryInstall(marketRevision, 1L, marketReplacement))
                throw new InvalidOperationException("A prepared selected-sale owner rejected its preflighted replacement.");
            sellerAccount.InstallPrepared(sellerMoneyRevision, sellerBalanceReplacement);
            if (counterpartyAccount != null)
                counterpartyAccount.InstallPrepared(counterpartyMoneyRevision, counterpartyBalanceReplacement);
        }
        catch (Exception)
        {
            if (!receiptOwner.TryStore(unresolved)) return unresolved;
            return unresolved;
        }

        if (!receiptOwner.TryStore(committed))
        {
            receiptOwner.TryStore(unresolved);
            return unresolved;
        }
        return committed;
    }

    private static EconomyTransactionResult ValidateSale(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners seller,
        DailyV1CityRuntimeOwners city,
        DailyV1ItemDefinition item,
        string actorPersonId,
        string actionSemanticId,
        string marketSiteId,
        int requested,
        MarketLiquidityMode mode)
    {
        MoneyEffect effect = mode == MarketLiquidityMode.AccountBacked
            ? MoneyEffect.Transfer
            : MoneyEffect.ExplicitSource;
        DailyV1MarketOwner market = city?.Market;
        DailyV1ItemDefinition admittedItem;
        bool selectedContext = graph != null && seller != null && city != null && market != null
            && graph.ContainsExactNpcOwner(seller)
            && graph.ContainsExactCityOwner(city)
            && ReferenceEquals(city.Market, market)
            && ReferenceEquals(seller.Definitions, graph.Definitions)
            && ReferenceEquals(city.Definitions, graph.Definitions)
            && ReferenceEquals(market.Definitions, graph.Definitions)
            && IsAdmittedItem(graph, item)
            && seller.Inventory != null && seller.MoneyAccount != null
            && seller.Lifecycle?.LifeState == NpcLifeState.Alive
            && seller.Travel != null && !seller.Travel.IsTraveling
            && string.Equals(seller.Travel.CurrentCityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            && string.Equals(seller.Travel.CurrentLocationRuntimeId, city.LocationRuntimeId, StringComparison.Ordinal)
            && string.Equals(marketSiteId, city.LocationRuntimeId, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(actorPersonId)
            && graph.Definitions.Actions.TryGetValue(actionSemanticId ?? string.Empty,
                out DailyV1ActionDefinition action)
            && action.Values.actionType?.Value == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue;
        // P18-D receives ActorPersonId from its already-admitted P11 input. This
        // owner records that causal identity without duplicating actor-resolution
        // authority with a second PersonStore join.
        if (!selectedContext || !graph.Definitions.Items.TryGetValue(item.DefinitionId, out admittedItem)
            || !ReferenceEquals(admittedItem, item) || seller.Inventory == null || market == null
            || sellerAccountMissing(seller) || city.Counterparty == null)
            return Failure(mode, EconomyTransactionFailureReason.InvalidInput);

        if (requested <= 0)
            return Failure(MarketLiquidityMode.Open,
                EconomyTransactionFailureReason.InvalidQuantity);
        int quantity = UnityMathfCompatibility.Min(requested, seller.Inventory.GetAmount(item));
        float price = market.GetPrice(item);
        if (quantity <= 0)
            return Failure(MarketLiquidityMode.Open,
                EconomyTransactionFailureReason.InsufficientInventory);
        if (float.IsNaN(price) || float.IsInfinity(price) || price < 0f)
            return Failure(MarketLiquidityMode.Open,
                EconomyTransactionFailureReason.InvalidPrice);
        price = UnityMathfCompatibility.Max(.01f, price);
        MarketCounterpartyRuntime counterparty = city.Counterparty;
        MoneyAccountRuntime counterpartyAccount = counterparty.LiquidityMode == MarketLiquidityMode.AccountBacked
            ? counterparty.MoneyAccount
            : null;
        if (counterparty.LiquidityMode == MarketLiquidityMode.AccountBacked)
        {
            if (counterpartyAccount == null)
                return Failure(MarketLiquidityMode.AccountBacked,
                    EconomyTransactionFailureReason.InvalidInput);
            quantity = UnityMathfCompatibility.Min(quantity,
                GetAffordableQuantity(counterpartyAccount.Balance, price));
            if (quantity <= 0)
                return Failure(MarketLiquidityMode.AccountBacked,
                    EconomyTransactionFailureReason.InsufficientCounterpartyFunds);
        }

        float total = quantity * price;
        if (float.IsNaN(total) || float.IsInfinity(total))
            return Failure(MarketLiquidityMode.Open,
                EconomyTransactionFailureReason.InvalidPrice);
        if (counterpartyAccount != null && !counterpartyAccount.CanDebit(total))
            return Failure(MarketLiquidityMode.AccountBacked,
                EconomyTransactionFailureReason.InsufficientCounterpartyFunds);
        if (!seller.MoneyAccount.CanCredit(total))
            return Failure(counterpartyAccount != null ? MarketLiquidityMode.AccountBacked : MarketLiquidityMode.Open,
                EconomyTransactionFailureReason.CreditRejected);
        if (!market.CanAddStock(item, quantity))
            return Failure(MarketLiquidityMode.Open,
                EconomyTransactionFailureReason.MarketCapacity);

        return EconomyTransactionResult.CreateSuccess(
            EconomyTransactionType.OpenMarketSale,
            counterpartyAccount != null ? MoneyEffect.Transfer : MoneyEffect.ExplicitSource,
            total, item.DefinitionId, requested, quantity, price, total,
            sourceRuntimeId: counterpartyAccount != null ? counterparty.CounterpartyRuntimeId : null,
            destinationRuntimeId: counterpartyAccount != null ? seller.RuntimeId : null,
            actorRuntimeId: seller.RuntimeId,
            itemSourceRuntimeId: seller.RuntimeId,
            itemDestinationRuntimeId: counterparty.CounterpartyRuntimeId);
    }

    private static EconomyTransactionResult Failure(
        MarketLiquidityMode mode,
        EconomyTransactionFailureReason reason, string actorRuntimeId = null) =>
        EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale,
            mode == MarketLiquidityMode.AccountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSource,
            reason, actorRuntimeId: actorRuntimeId);

    private static bool IsAdmittedItem(DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1ItemDefinition item) => graph != null && item != null
        && graph.Definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admitted)
        && ReferenceEquals(admitted, item);

    private static bool sellerAccountMissing(DailyV1NpcRuntimeOwners seller) => seller?.MoneyAccount == null;

    private static int GetAffordableQuantity(float balance, float unitPrice)
    {
        if (balance < unitPrice) return 0;
        float affordableAmount = balance / unitPrice;
        if (float.IsInfinity(affordableAmount) || affordableAmount >= int.MaxValue)
            return int.MaxValue;
        return UnityMathfCompatibility.Max(0, UnityMathfCompatibility.FloorToInt(affordableAmount));
    }

    private static bool IsValidNonNegativeFiniteAmount(float amount) =>
        amount >= 0f && !float.IsNaN(amount) && !float.IsInfinity(amount);
}