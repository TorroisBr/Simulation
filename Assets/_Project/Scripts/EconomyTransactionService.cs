using System;
using System.Collections.Generic;
using UnityEngine;

public enum EconomyTransactionType
{
    MoneyTransfer,
    NpcToNpcTrade,
    OpenMarketPurchase,
    OpenMarketSale,
    TravelCharge,
    PopulationConsumption
}

public enum MoneyEffect
{
    Transfer,
    ExplicitSource,
    ExplicitSink
}

public enum EconomyTransactionFailureReason
{
    None,
    InvalidInput,
    InvalidQuantity,
    InvalidPrice,
    SameAccount,
    InsufficientFunds,
    InsufficientStock,
    InsufficientInventory,
    InventoryCapacity,
    MarketCapacity,
    CounterpartyCreditCapacity,
    InsufficientCounterpartyFunds,
    CreditRejected,
    TransactionCommitFailed
}

public sealed class EconomyTransactionResult
{
    private readonly bool success;
    private readonly EconomyTransactionType transactionType;
    private readonly MoneyEffect moneyEffect;
    private readonly EconomyTransactionFailureReason failureReason;
    private readonly float amount;
    private readonly string itemDefinitionId;
    private readonly int requestedQuantity;
    private readonly int quantity;
    private readonly float unitPrice;
    private readonly float totalPrice;
    private readonly string sourceRuntimeId;
    private readonly string destinationRuntimeId;
    private readonly string itemSourceRuntimeId;
    private readonly string itemDestinationRuntimeId;
    private readonly string buyerRuntimeId;
    private readonly string sellerRuntimeId;
    private readonly string actorRuntimeId;
    private readonly IReadOnlyList<string> participantRuntimeIds;

    public bool Success => success;
    public EconomyTransactionType TransactionType => transactionType;
    public MoneyEffect MoneyEffect => moneyEffect;
    public EconomyTransactionFailureReason FailureReason => failureReason;
    public float Amount => amount;
    public string ItemDefinitionId => itemDefinitionId;
    public string ItemId => itemDefinitionId;
    public int RequestedQuantity => requestedQuantity;
    public int Quantity => quantity;
    public int EffectiveQuantity => quantity;
    public float UnitPrice => unitPrice;
    public float PricePerItem => unitPrice;
    public float TotalPrice => totalPrice;
    public string SourceRuntimeId => sourceRuntimeId;
    public string DestinationRuntimeId => destinationRuntimeId;
    public string ItemSourceRuntimeId => itemSourceRuntimeId;
    public string ItemDestinationRuntimeId => itemDestinationRuntimeId;
    public string BuyerRuntimeId => buyerRuntimeId;
    public string SellerRuntimeId => sellerRuntimeId;
    public string ActorRuntimeId => actorRuntimeId;
    public IReadOnlyList<string> ParticipantRuntimeIds => participantRuntimeIds;

    private EconomyTransactionResult(
        bool success,
        EconomyTransactionType transactionType,
        MoneyEffect moneyEffect,
        EconomyTransactionFailureReason failureReason,
        float amount,
        string itemDefinitionId,
        int requestedQuantity,
        int quantity,
        float unitPrice,
        float totalPrice,
        string sourceRuntimeId,
        string destinationRuntimeId,
        string buyerRuntimeId,
        string sellerRuntimeId,
        string actorRuntimeId,
        IEnumerable<string> participantRuntimeIds,
        string itemSourceRuntimeId,
        string itemDestinationRuntimeId)
    {
        this.success = success;
        this.transactionType = transactionType;
        this.moneyEffect = moneyEffect;
        this.failureReason = failureReason;
        this.amount = amount;
        this.itemDefinitionId = itemDefinitionId;
        this.requestedQuantity = requestedQuantity;
        this.quantity = quantity;
        this.unitPrice = unitPrice;
        this.totalPrice = totalPrice;
        this.sourceRuntimeId = sourceRuntimeId;
        this.destinationRuntimeId = destinationRuntimeId;
        this.itemSourceRuntimeId = itemSourceRuntimeId;
        this.itemDestinationRuntimeId = itemDestinationRuntimeId;
        this.buyerRuntimeId = buyerRuntimeId;
        this.sellerRuntimeId = sellerRuntimeId;
        this.actorRuntimeId = actorRuntimeId;

        List<string> ids = new List<string>();

        if (participantRuntimeIds != null)
        {
            foreach (string runtimeId in participantRuntimeIds)
            {
                if (string.IsNullOrWhiteSpace(runtimeId) == false)
                {
                    ids.Add(runtimeId);
                }
            }
        }

        this.participantRuntimeIds = ids.AsReadOnly();
    }

    internal static EconomyTransactionResult CreateSuccess(
        EconomyTransactionType transactionType,
        MoneyEffect moneyEffect,
        float amount,
        string itemDefinitionId = null,
        int requestedQuantity = 0,
        int quantity = 0,
        float unitPrice = 0f,
        float totalPrice = 0f,
        string sourceRuntimeId = null,
        string destinationRuntimeId = null,
        string buyerRuntimeId = null,
        string sellerRuntimeId = null,
        string actorRuntimeId = null,
        IEnumerable<string> participantRuntimeIds = null,
        string itemSourceRuntimeId = null,
        string itemDestinationRuntimeId = null)
    {
        return new EconomyTransactionResult(
            true,
            transactionType,
            moneyEffect,
            EconomyTransactionFailureReason.None,
            amount,
            itemDefinitionId,
            requestedQuantity,
            quantity,
            unitPrice,
            totalPrice,
            sourceRuntimeId,
            destinationRuntimeId,
            buyerRuntimeId,
            sellerRuntimeId,
            actorRuntimeId,
            participantRuntimeIds,
            itemSourceRuntimeId,
            itemDestinationRuntimeId);
    }

    internal static EconomyTransactionResult CreateFailure(
        EconomyTransactionType transactionType,
        MoneyEffect moneyEffect,
        EconomyTransactionFailureReason failureReason,
        string sourceRuntimeId = null,
        string destinationRuntimeId = null,
        string buyerRuntimeId = null,
        string sellerRuntimeId = null,
        string actorRuntimeId = null,
        IEnumerable<string> participantRuntimeIds = null,
        string itemSourceRuntimeId = null,
        string itemDestinationRuntimeId = null)
    {
        return new EconomyTransactionResult(
            false,
            transactionType,
            moneyEffect,
            failureReason,
            0f,
            null,
            0,
            0,
            0f,
            0f,
            sourceRuntimeId,
            destinationRuntimeId,
            buyerRuntimeId,
            sellerRuntimeId,
            actorRuntimeId,
            participantRuntimeIds,
            itemSourceRuntimeId,
            itemDestinationRuntimeId);
    }
}

public enum KeyedSaleOutcome { Committed, TerminalRejection, ProvenNoInstall, Unresolved, FingerprintCollision }

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
    internal KeyedSaleSnapshot(string input, string request, string actor, string proposal, string action, int version, string profile, string tick, string site, string sellerRuntime, string sellerCityRuntime, string counterparty, string item, MarketLiquidityMode mode, float price, int stock, int inventory, float sellerBalance, float counterpartyBalance, int requested, int effective, bool alive, bool canReceive, bool counterpartyCanPay, bool marketCanAccept, long inventoryRevision, long marketRevision, long sellerMoneyRevision, long counterpartyMoneyRevision)
    { ActorChoiceInputId=input; RequestId=request; ActorPersonId=actor; ProposalId=proposal; ActionSemanticId=action; ActionVersion=version; ProfileId=profile; LogicalTick=tick; MarketSiteId=site; SellerRuntimeId=sellerRuntime; SellerCityRuntimeId=sellerCityRuntime; CounterpartyId=counterparty; ItemDefinitionId=item; LiquidityMode=mode; UnitPrice=price; MarketStock=stock; SellerInventory=inventory; SellerBalance=sellerBalance; CounterpartyBalance=counterpartyBalance; RequestedQuantity=requested; EffectiveQuantity=effective; SellerWasAlive=alive; SellerCouldReceive=canReceive; CounterpartyCouldPay=counterpartyCanPay; MarketCouldAcceptStock=marketCanAccept; InventoryRevision=inventoryRevision; MarketRevision=marketRevision; SellerMoneyRevision=sellerMoneyRevision; CounterpartyMoneyRevision=counterpartyMoneyRevision; }
}

public sealed class KeyedSaleReceipt
{
    public string ProposalId { get; }
    public string Fingerprint { get; }
    internal string CallerFingerprint { get; }
    public KeyedSaleOutcome Outcome { get; }
    public KeyedSaleSnapshot Snapshot { get; }
    public EconomyTransactionResult Result { get; }
    private KeyedSaleReceipt(string id, string fingerprint, string callerFingerprint, KeyedSaleOutcome outcome, KeyedSaleSnapshot snapshot, EconomyTransactionResult result) { ProposalId=id; Fingerprint=fingerprint; CallerFingerprint=callerFingerprint; Outcome=outcome; Snapshot=snapshot; Result=result; }
    internal static KeyedSaleReceipt Committed(string id,string fp,string callerFp,KeyedSaleSnapshot s,EconomyTransactionResult r) => new KeyedSaleReceipt(id,fp,callerFp,KeyedSaleOutcome.Committed,s,r);
    internal static KeyedSaleReceipt Terminal(string id,string fp,string callerFp,KeyedSaleSnapshot s,EconomyTransactionResult r) => new KeyedSaleReceipt(id,fp,callerFp,KeyedSaleOutcome.TerminalRejection,s,r);
    internal static KeyedSaleReceipt ProvenNoInstall(string id,string fp,string callerFp,KeyedSaleSnapshot s,EconomyTransactionResult r) => new KeyedSaleReceipt(id,fp,callerFp,KeyedSaleOutcome.ProvenNoInstall,s,r);
    internal static KeyedSaleReceipt Unresolved(string id,string fp,string callerFp,KeyedSaleSnapshot s,EconomyTransactionResult r) => new KeyedSaleReceipt(id,fp,callerFp,KeyedSaleOutcome.Unresolved,s,r);
    internal static KeyedSaleReceipt Rejected(string id,string fp,KeyedSaleSnapshot s,EconomyTransactionFailureReason reason) => Terminal(id,fp,fp,s,EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale,MoneyEffect.ExplicitSource,reason));
    internal static KeyedSaleReceipt Collision(string id,string fp,string callerFp = null) => new KeyedSaleReceipt(id,fp,callerFp,KeyedSaleOutcome.FingerprintCollision,null,EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale,MoneyEffect.ExplicitSource,EconomyTransactionFailureReason.InvalidInput));
}

public sealed class EconomyTransactionService : IOwnerSectionCensusProvider
{
    public const string KeyedSaleReceiptSectionId = "p12e.economy-keyed-sale-receipts";
    public const int KeyedSaleReceiptSectionSchemaVersion = 1;

    private readonly List<KeyedSaleReceipt> keyedSaleReceipts = new List<KeyedSaleReceipt>();
    private readonly object keyedSaleReceiptsOwnerIdentity = new object();
    private long keyedSaleReceiptsRevision;
    private SimulationRuntime p12CensusRuntime;

    /// <summary>
    /// Reports the exact live cardinality and revision of the keyed-sale receipt
    /// ledger. This is census evidence only, not a receipt export.
    /// </summary>
    public OwnerSectionCensusWitness GetKeyedSaleReceiptCensus()
    {
        return new OwnerSectionCensusWitness(
            KeyedSaleReceiptSectionId,
            KeyedSaleReceiptSectionSchemaVersion,
            keyedSaleReceiptsOwnerIdentity,
            keyedSaleReceipts.Count,
            keyedSaleReceiptsRevision);
    }

    OwnerSectionCensusWitness IOwnerSectionCensusProvider.GetCurrentCensus()
    {
        return GetKeyedSaleReceiptCensus();
    }

    internal void BindP12CensusRuntime(SimulationRuntime runtime)
    {
        if (runtime == null) throw new ArgumentNullException(nameof(runtime));
        if (p12CensusRuntime != null && !ReferenceEquals(p12CensusRuntime, runtime))
            throw new InvalidOperationException("EconomyTransactionService cannot be rebound to a different P12 runtime.");
        p12CensusRuntime = runtime;
    }

    private void NotifyNpcTradeOwnerMutation(bool tracking, string sectionId, bool ownerNotifiesCommit)
    {
        if (tracking && !ownerNotifiesCommit) p12CensusRuntime.NotifyNpcTradeOwnerMutation(sectionId);
    }

    public KeyedSaleReceipt TryExecuteKeyedMarketSale(
        string proposalId, string fingerprint, string actorChoiceInputId, string requestId,
        string actorPersonId, string actionSemanticId, int actionVersion, string profileId,
        string logicalTick, string marketSiteId, NpcRuntime seller, MarketRuntime market,
        ItemData item, int requestedQuantity)
    {
        if (string.IsNullOrWhiteSpace(proposalId) || string.IsNullOrWhiteSpace(fingerprint))
            return KeyedSaleReceipt.Rejected(proposalId, fingerprint, null, EconomyTransactionFailureReason.InvalidInput);

        string canonicalFingerprint = CreateRequestFingerprint(
            proposalId, fingerprint, actorChoiceInputId, requestId, actorPersonId,
            actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            item != null ? item.DefinitionId : null, requestedQuantity);

        for (int i = 0; i < keyedSaleReceipts.Count; i++)
        {
            KeyedSaleReceipt prior = keyedSaleReceipts[i];
            if (!string.Equals(prior.ProposalId, proposalId, StringComparison.Ordinal)) continue;
            if (!string.Equals(prior.Fingerprint, canonicalFingerprint, StringComparison.Ordinal))
                return KeyedSaleReceipt.Collision(proposalId, canonicalFingerprint, fingerprint);
            if (prior.Outcome != KeyedSaleOutcome.ProvenNoInstall) return prior;
            break;
        }

        // The receipt revision is part of the census contract. Refuse new
        // installs if it cannot advance without wrapping or reusing a stamp.
        if (keyedSaleReceiptsRevision == long.MaxValue)
        {
            return KeyedSaleReceipt.Rejected(
                proposalId,
                fingerprint,
                null,
                EconomyTransactionFailureReason.TransactionCommitFailed);
        }

        MarketLiquidityMode mode = market != null ? market.Counterparty.LiquidityMode : MarketLiquidityMode.Open;
        MoneyAccountRuntime sellerAccount = seller != null ? seller.MoneyAccount : null;
        MoneyAccountRuntime counterpartyAccount = market != null && mode == MarketLiquidityMode.AccountBacked ? market.Counterparty.MoneyAccount : null;
        long invRev = seller != null ? seller.Inventory.Revision : -1;
        long marketRev = market != null ? market.Revision : -1;
        long sellerMoneyRev = sellerAccount != null ? sellerAccount.Revision : -1;
        long counterpartyMoneyRev = counterpartyAccount != null ? counterpartyAccount.Revision : -1;
        float observedPrice = market != null && item != null ? market.GetPriceForSale(item) : 0f;
        int observedStock = market != null && item != null ? market.GetAmount(item) : 0;
        int observedInventory = seller != null && item != null ? seller.Inventory.GetAmount(item) : 0;
        float observedSellerBalance = sellerAccount != null ? sellerAccount.Balance : 0f;
        float observedCounterpartyBalance = counterpartyAccount != null ? counterpartyAccount.Balance : 0f;
        EconomyTransactionResult result = ValidateSale(seller, market, item, requestedQuantity, mode);
        int snapshotQuantity = result.Success ? result.Quantity : Mathf.Min(Mathf.Max(0, requestedQuantity), observedInventory);
        float normalizedSalePrice = IsValidNonNegativeFiniteAmount(observedPrice) ? Mathf.Max(0.01f, observedPrice) : observedPrice;
        if (!result.Success && mode == MarketLiquidityMode.AccountBacked && counterpartyAccount != null
            && IsValidNonNegativeFiniteAmount(normalizedSalePrice))
        {
            snapshotQuantity = Mathf.Min(snapshotQuantity, GetAffordableQuantity(observedCounterpartyBalance, normalizedSalePrice));
        }
        float snapshotTotal = normalizedSalePrice * snapshotQuantity;
        KeyedSaleSnapshot snapshot = new KeyedSaleSnapshot(actorChoiceInputId, requestId, actorPersonId, proposalId,
            actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            seller != null ? seller.RuntimeId : null,
            seller != null && seller.CurrentCity != null ? seller.CurrentCity.RuntimeId : null,
            market != null ? market.StockOwnerRuntimeId : null, item != null ? item.DefinitionId : null,
            mode, observedPrice, observedStock, observedInventory, observedSellerBalance, observedCounterpartyBalance,
            requestedQuantity, snapshotQuantity, seller != null && seller.IsAlive,
            snapshotQuantity > 0 && sellerAccount != null && sellerAccount.CanCredit(snapshotTotal),
            snapshotQuantity > 0 && mode == MarketLiquidityMode.AccountBacked && counterpartyAccount != null && counterpartyAccount.CanDebit(snapshotTotal),
            snapshotQuantity > 0 && market != null && item != null && market.CanAddStock(item, snapshotQuantity),
            invRev, marketRev, sellerMoneyRev, counterpartyMoneyRev);
        if (!result.Success)
        {
            KeyedSaleReceipt rejected = KeyedSaleReceipt.Terminal(proposalId, canonicalFingerprint, fingerprint, snapshot, result);
            StoreReceipt(rejected);
            return rejected;
        }

        int quantity = result.Quantity;
        float total = result.TotalPrice;
        PreparedInventoryState inventoryReplacement = seller.Inventory.PrepareReplacement(item, quantity, 0f, false, 0);
        PreparedMarketState marketReplacement = market.PrepareReplacement(item, quantity, 100);
        float sellerBalanceReplacement = sellerAccount.Balance + total;
        float counterpartyBalanceReplacement = counterpartyAccount != null ? counterpartyAccount.Balance - total : 0f;
        KeyedSaleReceipt committed = KeyedSaleReceipt.Committed(proposalId, canonicalFingerprint, fingerprint, snapshot, result);
        KeyedSaleReceipt unresolved = KeyedSaleReceipt.Unresolved(proposalId, canonicalFingerprint, fingerprint, snapshot, result);
        if (keyedSaleReceipts.Capacity < keyedSaleReceipts.Count + 1) keyedSaleReceipts.Capacity = keyedSaleReceipts.Count + 1;
        bool ready = seller.Inventory.CanInstall(invRev) && market.CanInstall(marketRev)
            && sellerAccount.CanInstall(sellerMoneyRev, sellerBalanceReplacement)
            && (counterpartyAccount == null || counterpartyAccount.CanInstall(counterpartyMoneyRev, counterpartyBalanceReplacement));
        if (!ready)
        {
            EconomyTransactionResult notInstalled = EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.OpenMarketSale,
                mode == MarketLiquidityMode.AccountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSource,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: seller.RuntimeId);
            KeyedSaleReceipt stale = KeyedSaleReceipt.ProvenNoInstall(proposalId, canonicalFingerprint, fingerprint, snapshot, notInstalled);
            StoreReceipt(stale);
            return stale;
        }

        // All allocations, validation, and expected-revision checks precede the install boundary.
        try
        {
            seller.Inventory.InstallPrepared(invRev, inventoryReplacement);
            market.InstallPrepared(marketRev, marketReplacement);
            sellerAccount.InstallPrepared(sellerMoneyRev, sellerBalanceReplacement);
            if (counterpartyAccount != null) counterpartyAccount.InstallPrepared(counterpartyMoneyRev, counterpartyBalanceReplacement);
        }
        catch (Exception)
        {
            // Once the boundary is crossed, never guess which assignments took effect.
            EconomyTransactionResult uncertain = EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.OpenMarketSale,
                mode == MarketLiquidityMode.AccountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSource,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: seller.RuntimeId);
            KeyedSaleReceipt unresolvedResult = KeyedSaleReceipt.Unresolved(proposalId, canonicalFingerprint, fingerprint, snapshot, uncertain);
            StoreReceipt(unresolvedResult);
            return unresolvedResult;
        }
        StoreReceipt(committed);
        return committed;
    }

    public KeyedSaleReceipt FindKeyedSaleReceipt(
        string proposalId, string fingerprint, string actorChoiceInputId, string requestId,
        string actorPersonId, string actionSemanticId, int actionVersion, string profileId,
        string logicalTick, string marketSiteId, NpcRuntime seller, MarketRuntime market,
        ItemData item, int requestedQuantity)
    {
        return FindKeyedSaleReceipt(
            proposalId, fingerprint, actorChoiceInputId, requestId, actorPersonId,
            actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            item != null ? item.DefinitionId : null, requestedQuantity);
    }

    /// <summary>
    /// Looks up a receipt using only immutable request correlation. Execution-time
    /// owner state such as actor materialization/location and current market mapping
    /// belongs in the original receipt snapshot, not in replay identity.
    /// </summary>
    public KeyedSaleReceipt FindKeyedSaleReceipt(
        string proposalId, string fingerprint, string actorChoiceInputId, string requestId,
        string actorPersonId, string actionSemanticId, int actionVersion, string profileId,
        string logicalTick, string marketSiteId, string itemDefinitionId, int requestedQuantity)
    {
        if (string.IsNullOrWhiteSpace(proposalId) || string.IsNullOrWhiteSpace(fingerprint)) return null;
        string canonicalFingerprint = CreateRequestFingerprint(
            proposalId, fingerprint, actorChoiceInputId, requestId, actorPersonId,
            actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            itemDefinitionId, requestedQuantity);
        for (int i = 0; i < keyedSaleReceipts.Count; i++)
        {
            KeyedSaleReceipt receipt = keyedSaleReceipts[i];
            if (!string.Equals(receipt.ProposalId, proposalId, StringComparison.Ordinal)) continue;
            return string.Equals(receipt.Fingerprint, canonicalFingerprint, StringComparison.Ordinal)
                ? receipt
                : KeyedSaleReceipt.Collision(proposalId, canonicalFingerprint, fingerprint);
        }
        return null;
    }

    private static string CreateRequestFingerprint(
        string proposalId, string callerFingerprint, string actorChoiceInputId,
        string requestId, string actorPersonId, string actionSemanticId,
        int actionVersion, string profileId, string logicalTick, string marketSiteId,
        string itemDefinitionId, int requestedQuantity)
    {
        // Mutable execution-time truth belongs in KeyedSaleSnapshot. This key is
        // causal request identity and must survive actor rematerialization, movement,
        // and changes to market state after a committed operation.
        return CreateCanonicalFingerprint(
            proposalId, callerFingerprint, actorChoiceInputId, requestId, actorPersonId,
            actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            itemDefinitionId, requestedQuantity);
    }

    private static string CreateCanonicalFingerprint(params object[] values)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            string value = values[i] == null
                ? null
                : values[i] is IFormattable formattable
                    ? formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
                    : values[i].ToString();
            if (value == null) builder.Append("-1:");
            else builder.Append(value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(':').Append(value);
            builder.Append('|');
        }
        return builder.ToString();
    }

    private void StoreReceipt(KeyedSaleReceipt receipt)
    {
        long nextRevision = keyedSaleReceiptsRevision + 1L;
        for (int i = 0; i < keyedSaleReceipts.Count; i++)
            if (string.Equals(keyedSaleReceipts[i].ProposalId, receipt.ProposalId, StringComparison.Ordinal))
            {
                keyedSaleReceipts[i] = receipt;
                keyedSaleReceiptsRevision = nextRevision;
                return;
            }
        if (keyedSaleReceipts.Capacity < keyedSaleReceipts.Count + 1) keyedSaleReceipts.Capacity = keyedSaleReceipts.Count + 1;
        keyedSaleReceipts.Add(receipt);
        keyedSaleReceiptsRevision = nextRevision;
    }

    private static EconomyTransactionResult ValidateSale(NpcRuntime npc, MarketRuntime market, ItemData item, int requested, MarketLiquidityMode mode)
    {
        // Reuse the established sale preconditions and calculations without applying its mutations.
        if (npc == null || !npc.IsAlive || market == null || item == null)
            return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, mode == MarketLiquidityMode.AccountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.InvalidInput);
        if (requested <= 0) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.InvalidQuantity);
        int quantity = Mathf.Min(requested, npc.Inventory.GetAmount(item));
        float price = market.GetPriceForSale(item);
        if (quantity <= 0) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.InsufficientInventory);
        if (float.IsNaN(price) || float.IsInfinity(price) || price < 0f) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.InvalidPrice);
        price = Mathf.Max(.01f, price);
        MarketCounterpartyRuntime cp = market.Counterparty;
        if (mode == MarketLiquidityMode.AccountBacked)
        {
            if (cp.MoneyAccount == null) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.Transfer, EconomyTransactionFailureReason.InvalidInput);
            quantity = Mathf.Min(quantity, GetAffordableQuantity(cp.MoneyAccount.Balance, price));
            if (quantity <= 0) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.Transfer, EconomyTransactionFailureReason.InsufficientCounterpartyFunds);
        }
        float total = quantity * price;
        if (float.IsNaN(total) || float.IsInfinity(total)) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.InvalidPrice);
        if (mode == MarketLiquidityMode.AccountBacked && !cp.MoneyAccount.CanDebit(total)) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.Transfer, EconomyTransactionFailureReason.InsufficientCounterpartyFunds);
        if (!npc.MoneyAccount.CanCredit(total)) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, mode == MarketLiquidityMode.AccountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.CreditRejected);
        if (!market.CanAddStock(item, quantity)) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.MarketCapacity);
        return EconomyTransactionResult.CreateSuccess(EconomyTransactionType.OpenMarketSale, mode == MarketLiquidityMode.AccountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSource, total, item.DefinitionId, requested, quantity, price, total,
            sourceRuntimeId: mode == MarketLiquidityMode.AccountBacked ? cp.CounterpartyRuntimeId : null,
            destinationRuntimeId: mode == MarketLiquidityMode.AccountBacked ? npc.RuntimeId : null,
            actorRuntimeId: npc.RuntimeId, itemSourceRuntimeId: npc.RuntimeId, itemDestinationRuntimeId: cp.CounterpartyRuntimeId);
    }
    public EconomyTransactionResult TryTransferMoney(
        MoneyAccountRuntime source,
        MoneyAccountRuntime destination,
        float amount)
    {
        return TryTransferMoney(source, destination, amount, null, null);
    }

    public EconomyTransactionResult TryTransferMoney(
        NpcRuntime source,
        NpcRuntime destination,
        float amount)
    {
        if (source == null || destination == null)
        {
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.MoneyTransfer,
                MoneyEffect.Transfer,
                EconomyTransactionFailureReason.InvalidInput,
                source != null ? source.RuntimeId : null,
                destination != null ? destination.RuntimeId : null);
        }

        return TryTransferMoney(source.MoneyAccount, destination.MoneyAccount, amount, source.RuntimeId, destination.RuntimeId);
    }

    public EconomyTransactionResult TryExecuteNpcTrade(
        NpcRuntime buyer,
        NpcRuntime seller,
        ItemData item,
        int quantity,
        float unitPrice)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.NpcToNpcTrade;
        MoneyEffect moneyEffect = MoneyEffect.Transfer;

        if (buyer == null || seller == null || item == null || buyer.IsAlive == false || seller.IsAlive == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                buyerRuntimeId: buyer != null ? buyer.RuntimeId : null,
                sellerRuntimeId: seller != null ? seller.RuntimeId : null);
        }

        if (buyer == seller)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.SameAccount,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        if (quantity <= 0)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidQuantity,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        if (IsValidNonNegativeFiniteAmount(unitPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        float totalPrice = unitPrice * quantity;

        if (IsValidNonNegativeFiniteAmount(totalPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        if (seller.Inventory.GetAmount(item) < quantity)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientInventory,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        if (buyer.Inventory.CanAddItem(item, quantity, unitPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InventoryCapacity,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        if (buyer.MoneyAccount.CanDebit(totalPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientFunds,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        if (seller.MoneyAccount.CanCredit(totalPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.CreditRejected,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        SimulationOperationScope censusScope = null;
        string buyerAccountSectionId = null;
        string sellerAccountSectionId = null;
        string buyerInventorySectionId = null;
        string sellerInventorySectionId = null;
        bool censusTracking = p12CensusRuntime != null && p12CensusRuntime.HasNpcTradeCensusAdapter;
        if (censusTracking)
        {
            censusTracking = p12CensusRuntime.TryBeginNpcTradeCensusOperation(
                buyer,
                seller,
                out censusScope,
                out buyerAccountSectionId,
                out sellerAccountSectionId,
                out buyerInventorySectionId,
                out sellerInventorySectionId);
            if (!censusTracking)
            {
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    buyerRuntimeId: buyer.RuntimeId,
                    sellerRuntimeId: seller.RuntimeId);
            }
        }

        try
        {
            long buyerAccountRevision = buyer.MoneyAccount.Revision;
            bool buyerDebitSucceeded = buyer.MoneyAccount.TryDebit(totalPrice);
            if (buyer.MoneyAccount.Revision != buyerAccountRevision)
                NotifyNpcTradeOwnerMutation(censusTracking, buyerAccountSectionId, buyer.MoneyAccount.HasP12MutationBoundary);
            if (buyerDebitSucceeded == false)
            {
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    buyerRuntimeId: buyer.RuntimeId,
                    sellerRuntimeId: seller.RuntimeId);
            }

            long sellerAccountRevision = seller.MoneyAccount.Revision;
            bool sellerCreditSucceeded = seller.MoneyAccount.TryCredit(totalPrice);
            if (seller.MoneyAccount.Revision != sellerAccountRevision)
                NotifyNpcTradeOwnerMutation(censusTracking, sellerAccountSectionId, seller.MoneyAccount.HasP12MutationBoundary);
            if (sellerCreditSucceeded == false)
            {
                long buyerCompensationRevision = buyer.MoneyAccount.Revision;
                buyer.MoneyAccount.TryCredit(totalPrice);
                if (buyer.MoneyAccount.Revision != buyerCompensationRevision)
                    NotifyNpcTradeOwnerMutation(censusTracking, buyerAccountSectionId, buyer.MoneyAccount.HasP12MutationBoundary);
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    buyerRuntimeId: buyer.RuntimeId,
                    sellerRuntimeId: seller.RuntimeId);
            }

            long sellerInventoryRevision = seller.Inventory.Revision;
            bool sellerInventoryRemoved = seller.Inventory.RemoveItem(item, quantity);
            if (seller.Inventory.Revision != sellerInventoryRevision)
                NotifyNpcTradeOwnerMutation(censusTracking, sellerInventorySectionId, seller.Inventory.HasP12MutationBoundary);
            if (sellerInventoryRemoved == false)
            {
                long sellerCompensationRevision = seller.MoneyAccount.Revision;
                seller.MoneyAccount.TryDebit(totalPrice);
                if (seller.MoneyAccount.Revision != sellerCompensationRevision)
                    NotifyNpcTradeOwnerMutation(censusTracking, sellerAccountSectionId, seller.MoneyAccount.HasP12MutationBoundary);

                long buyerCompensationRevision = buyer.MoneyAccount.Revision;
                buyer.MoneyAccount.TryCredit(totalPrice);
                if (buyer.MoneyAccount.Revision != buyerCompensationRevision)
                    NotifyNpcTradeOwnerMutation(censusTracking, buyerAccountSectionId, buyer.MoneyAccount.HasP12MutationBoundary);
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    buyerRuntimeId: buyer.RuntimeId,
                    sellerRuntimeId: seller.RuntimeId);
            }

            long buyerInventoryRevision = buyer.Inventory.Revision;
            bool buyerInventoryAdded = buyer.Inventory.TryAddItem(item, quantity, unitPrice);
            if (buyer.Inventory.Revision != buyerInventoryRevision)
                NotifyNpcTradeOwnerMutation(censusTracking, buyerInventorySectionId, buyer.Inventory.HasP12MutationBoundary);
            if (!buyerInventoryAdded)
            {
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    buyerRuntimeId: buyer.RuntimeId,
                    sellerRuntimeId: seller.RuntimeId);
            }

            return EconomyTransactionResult.CreateSuccess(
                transactionType,
                moneyEffect,
                totalPrice,
                item.DefinitionId,
                quantity,
                quantity,
                unitPrice,
                totalPrice,
                sourceRuntimeId: buyer.RuntimeId,
                destinationRuntimeId: seller.RuntimeId,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId,
                itemSourceRuntimeId: seller.RuntimeId,
                itemDestinationRuntimeId: buyer.RuntimeId);
        }
        finally
        {
            censusScope?.Dispose();
        }
    }

    public EconomyTransactionResult TryExecuteOpenMarketPurchase(
        NpcRuntime npc,
        MarketRuntime market,
        ItemData item,
        int requestedQuantity)
    {
        return ExecuteMarketPurchase(npc, market, item, requestedQuantity, MarketLiquidityMode.Open);
    }

    public EconomyTransactionResult TryExecuteOpenMarketSale(
        NpcRuntime npc,
        MarketRuntime market,
        ItemData item,
        int requestedQuantity)
    {
        return ExecuteMarketSale(npc, market, item, requestedQuantity, MarketLiquidityMode.Open);
    }

    public EconomyTransactionResult TryExecuteMarketPurchase(
        NpcRuntime npc,
        MarketRuntime market,
        ItemData item,
        int requestedQuantity)
    {
        return ExecuteMarketPurchase(
            npc,
            market,
            item,
            requestedQuantity,
            market != null ? market.Counterparty.LiquidityMode : MarketLiquidityMode.Open);
    }

    public EconomyTransactionResult TryExecuteMarketSale(
        NpcRuntime npc,
        MarketRuntime market,
        ItemData item,
        int requestedQuantity)
    {
        return ExecuteMarketSale(
            npc,
            market,
            item,
            requestedQuantity,
            market != null ? market.Counterparty.LiquidityMode : MarketLiquidityMode.Open);
    }

    private EconomyTransactionResult ExecuteMarketPurchase(
        NpcRuntime npc,
        MarketRuntime market,
        ItemData item,
        int requestedQuantity,
        MarketLiquidityMode liquidityMode)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.OpenMarketPurchase;
        bool accountBacked = liquidityMode == MarketLiquidityMode.AccountBacked;
        MoneyEffect moneyEffect = accountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSink;

        if (npc == null || npc.IsAlive == false || market == null || item == null)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                actorRuntimeId: npc != null ? npc.RuntimeId : null);
        }

        MarketCounterpartyRuntime counterparty = market.Counterparty;

        if (accountBacked == true && (counterparty == null || counterparty.MoneyAccount == null))
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                actorRuntimeId: npc.RuntimeId);
        }

        if (requestedQuantity <= 0)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidQuantity,
                actorRuntimeId: npc.RuntimeId);
        }

        MarketItemRuntime marketItem = market.GetItem(item);

        if (marketItem == null || marketItem.Amount <= 0)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientStock,
                actorRuntimeId: npc.RuntimeId);
        }

        if (IsValidNonNegativeFiniteAmount(marketItem.CurrentPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                actorRuntimeId: npc.RuntimeId);
        }

        float unitPrice = Mathf.Max(0.01f, marketItem.CurrentPrice);

        if (IsValidNonNegativeFiniteAmount(unitPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
    #]´ó­m¢G§²ÚîÆ­yÐ4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÍ…±”µ½µÁ•¹Í…Ñ¥½¸µ¹ÁŒˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰ÀÄÈµµ…É­•ÐµÍ…±”µ½µÁ•¹Í…Ñ¥½¸µ¹ÁŒˆ¤°4(€€€€€€€€€€€¥Ñä°4(€€€€€€€€€€€€ÈÁ˜¤ì4(€€€€€€€¹ÁŒ¹%¹Ù•¹Ñ½Éä¹‘‘%Ñ•´¡¥Ñ•´°€È°€Ñ˜¤ì4(€€€€€€€ÑåÁ•½˜¡%¹Ù•¹Ñ½ÉåIÕ¹Ñ¥µ”¤¹•Ñ¥•± ‰É•Ù¥Í¥½¸ˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤4(€€€€€€€€€€€€¹M•ÑY…±Õ”¡¹ÁŒ¹%¹Ù•¹Ñ½Éä°±½¹œ¹5…áY…±Õ”¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹•Ýmtì¥Ñäô°4(€€€€€€€€€€€¹•Ýmtì¹ÁŒô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ‰•™½É”°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”‰•™½É•…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°‰•™½É•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹I•ÍÕ±ÐÉ•ÍÕ±Ð€ôÍ•ÉÙ¥”¹QÉåá•ÕÑ•5…É­•ÑM…±”¡¹ÁŒ°¥Ñä¹5…É­•Ð°¥Ñ•´°€Ä¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹MÕ•ÍÌ°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹…¥±ÕÉ•I•…Í½¸°%Ì¹ÅÕ…±Q¼¡½¹½µåQÉ…¹Í…Ñ¥½¹…¥±ÕÉ•I•…Í½¸¹QÉ…¹Í…Ñ¥½¹½µµ¥Ñ…¥±•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¹ÁŒ¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÈÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¹ÁŒ¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ È¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ…™Ñ•È°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…™Ñ•É…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°…™Ñ•É…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…™Ñ•È°%Ì¹ÅÕ…±Q¼¡‰•™½É”€¬€È¤°€‰Ñ¡”ÍÕ•ÍÍ™Õ°É•‘¥Ð…¹…½Õ¹ÐÉ•Ù•ÉÍ…°‰½Ñ …‘Ù…¹”Ñ¡”•Á½ ˆ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…ÍÍ•ÍÍµ•¹Ð¹Q½MÑÉ¥¹œ ¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ5…É­•ÑM…±•}á¡…ÕÍÑ•‘5…É­•ÑI•Ù¥Í¥½¹I•©•ÑÍ	•™½É•¹å=Ý¹•É½µµ¥Ð ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµµ…É­•ÐµÉ•Ù¥Í¥½¸µ±¥µ¥Ðˆ°€ÄÁ˜¤ì4(€€€€€€€¥ÑåIÕ¹Ñ¥µ”¥Ñä€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•¥Ñä 4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÉ•Ù¥Í¥½¸µ±¥µ¥Ðµ¥Ñäˆ°4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÉ•Ù¥Í¥½¸µ±¥µ¥Ðµ±½…Ñ¥½¸ˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•5…É­•Ñ%Ñ•´¡¥Ñ•´°€À°€ÄÀ¤¤ì4(€€€€€€€ÑåÁ•½˜¡5…É­•ÑIÕ¹Ñ¥µ”¤¹•Ñ¥•± ‰É•Ù¥Í¥½¸ˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤4(€€€€€€€€€€€€¹M•ÑY…±Õ”¡¥Ñä¹5…É­•Ð°±½¹œ¹5…áY…±Õ”¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”¹ÁŒ€ô¹•Ü9ÁIÕ¹Ñ¥µ” 4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÉ•Ù¥Í¥½¸µ±¥µ¥Ðµ¹ÁŒˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰ÀÄÈµµ…É­•ÐµÉ•Ù¥Í¥½¸µ±¥µ¥Ðµ¹ÁŒˆ¤°4(€€€€€€€€€€€¥Ñä°4(€€€€€€€€€€€€ÈÁ˜¤ì4(€€€€€€€¹ÁŒ¹%¹Ù•¹Ñ½Éä¹‘‘%Ñ•´¡¥Ñ•´°€È°€Ñ˜¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹•Ýmtì¥Ñäô°4(€€€€€€€€€€€¹•Ýmtì¹ÁŒô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¥¹¥Ñ¥…±ÍÍ•ÍÍµ•¹Ð¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€¥¹¥Ñ¥…±ÍÍ•ÍÍµ•¹Ð¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ‰•™½É”°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”‰•™½É•…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°‰•™½É•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹I•ÍÕ±ÐÉ•ÍÕ±Ð€ôÍ•ÉÙ¥”¹QÉåá•ÕÑ•5…É­•ÑM…±”¡¹ÁŒ°¥Ñä¹5…É­•Ð°¥Ñ•´°€Ä¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹MÕ•ÍÌ°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹…¥±ÕÉ•I•…Í½¸°%Ì¹ÅÕ…±Q¼¡½¹½µåQÉ…¹Í…Ñ¥½¹…¥±ÕÉ•I•…Í½¸¹QÉ…¹Í…Ñ¥½¹½µµ¥Ñ…¥±•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¹ÁŒ¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÈÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¹ÁŒ¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ È¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ…™Ñ•È°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…™Ñ•É…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°…™Ñ•É…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…™Ñ•È°%Ì¹ÅÕ…±Q¼¡‰•™½É”¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…ÍÍ•ÍÍµ•¹Ð¹Q½MÑÉ¥¹œ ¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ5…É­•ÑAÕÉ¡…Í•}á¡…ÕÍÑ•‘5…É­•ÑI•Ù¥Í¥½¹I•©•ÑÍ	•™½É•¹å=Ý¹•É½µµ¥Ð ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµµ…É­•ÐµÁÕÉ¡…Í”µÉ•Ù¥Í¥½¸µ±¥µ¥Ðˆ°€ÄÁ˜¤ì4(€€€€€€€¥ÑåIÕ¹Ñ¥µ”¥Ñä€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•¥Ñä 4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÁÕÉ¡…Í”µÉ•Ù¥Í¥½¸µ±¥µ¥Ðµ¥Ñäˆ°4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÁÕÉ¡…Í”µÉ•Ù¥Í¥½¸µ±¥µ¥Ðµ±½…Ñ¥½¸ˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•5…É­•Ñ%Ñ•´¡¥Ñ•´°€Ô°€Ô¤¤ì4(€€€€€€€ÑåÁ•½˜¡5…É­•ÑIÕ¹Ñ¥µ”¤¹•Ñ¥•± ‰É•Ù¥Í¥½¸ˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤4(€€€€€€€€€€€€¹M•ÑY…±Õ”¡¥Ñä¹5…É­•Ð°±½¹œ¹5…áY…±Õ”¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”¹ÁŒ€ô¹•Ü9ÁIÕ¹Ñ¥µ” 4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÁÕÉ¡…Í”µÉ•Ù¥Í¥½¸µ±¥µ¥Ðµ¹ÁŒˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰ÀÄÈµµ…É­•ÐµÁÕÉ¡…Í”µÉ•Ù¥Í¥½¸µ±¥µ¥Ðµ¹ÁŒˆ¤°4(€€€€€€€€€€€¥Ñä°4(€€€€€€€€€€€€ÔÁ˜¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹•Ýmtì¥Ñäô°4(€€€€€€€€€€€¹•Ýmtì¹ÁŒô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¥¹¥Ñ¥…±ÍÍ•ÍÍµ•¹Ð¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€¥¹¥Ñ¥…±ÍÍ•ÍÍµ•¹Ð¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ‰•™½É”°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”‰•™½É•…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°‰•™½É•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹I•ÍÕ±ÐÉ•ÍÕ±Ð€ôÍ•ÉÙ¥”¹QÉåá•ÕÑ•5…É­•ÑAÕÉ¡…Í”¡¹ÁŒ°¥Ñä¹5…É­•Ð°¥Ñ•´°€Ä¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹MÕ•ÍÌ°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹…¥±ÕÉ•I•…Í½¸°%Ì¹ÅÕ…±Q¼¡½¹½µåQÉ…¹Í…Ñ¥½¹…¥±ÕÉ•I•…Í½¸¹QÉ…¹Í…Ñ¥½¹½µµ¥Ñ…¥±•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¹ÁŒ¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÔÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¹ÁŒ¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ô¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ…™Ñ•È°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…™Ñ•É…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°…™Ñ•É…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…™Ñ•È°%Ì¹ÅÕ…±Q¼¡‰•™½É”¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…ÍÍ•ÍÍµ•¹Ð¹Q½MÑÉ¥¹œ ¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ5…É­•ÑAÕÉ¡…Í•}MÑ…±•5…É­•ÑI•Ù¥Í¥½¹I•©•ÑÍ	•™½É•¹å=Ý¹•É½µµ¥Ð ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµµ…É­•ÐµÍÑ…±”µÉ•Ù¥Í¥½¸ˆ°€ÄÁ˜¤ì4(€€€€€€€¥ÑåIÕ¹Ñ¥µ”¥Ñä€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•¥Ñä 4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÍÑ…±”µÉ•Ù¥Í¥½¸µ¥Ñäˆ°4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÍÑ…±”µÉ•Ù¥Í¥½¸µ±½…Ñ¥½¸ˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•5…É­•Ñ%Ñ•´¡¥Ñ•´°€Ô°€Ô¤¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”¹ÁŒ€ô¹•Ü9ÁIÕ¹Ñ¥µ” 4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÍÑ…±”µÉ•Ù¥Í¥½¸µ¹ÁŒˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰ÀÄÈµµ…É­•ÐµÍÑ…±”µÉ•Ù¥Í¥½¸µ¹ÁŒˆ¤°4(€€€€€€€€€€€¥Ñä°4(€€€€€€€€€€€€ÔÁ˜¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹•Ýmtì¥Ñäô°4(€€€€€€€€€€€¹•Ýmtì¹ÁŒô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(€€€€€€€ÑåÁ•½˜¡5…É­•ÑIÕ¹Ñ¥µ”¤¹•Ñ¥•± ‰É•Ù¥Í¥½¸ˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤4(€€€€€€€€€€€€¹M•ÑY…±Õ”¡¥Ñä¹5…É­•Ð°¥Ñä¹5…É­•Ð¹I•Ù¥Í¥½¸€¬€Ä¤ì4(4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹I•ÍÕ±ÐÉ•ÍÕ±Ð€ôÍ•ÉÙ¥”¹QÉåá•ÕÑ•5…É­•ÑAÕÉ¡…Í”¡¹ÁŒ°¥Ñä¹5…É­•Ð°¥Ñ•´°€Ä¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹MÕ•ÍÌ°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹…¥±ÕÉ•I•…Í½¸°%Ì¹ÅÕ…±Q¼¡½¹½µåQÉ…¹Í…Ñ¥½¹…¥±ÕÉ•I•…Í½¸¹QÉ…¹Í…Ñ¥½¹½µµ¥Ñ…¥±•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¹ÁŒ¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÔÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¹ÁŒ¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ô¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…ÍÍ•ÍÍµ•¹Ð°%Ì¹ÅÕ…±Q¼¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹AÉ½Ñ½½±…Õ±Ñ•¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ5…É­•ÑAÕÉ¡…Í•}]É½¹Q¡É•…‘I•©•ÑÍ	•™½É•¹å=Ý¹•É½µµ¥Ð ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµµ…É­•ÐµÝÉ½¹œµÑ¡É•…ˆ°€ÄÁ˜¤ì4(€€€€€€€¥ÑåIÕ¹Ñ¥µ”¥Ñä€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•¥Ñä 4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÝÉ½¹œµÑ¡É•…µ¥Ñäˆ°4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÝÉ½¹œµÑ¡É•…µ±½…Ñ¥½¸ˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•5…É­•Ñ%Ñ•´¡¥Ñ•´°€Ô°€Ô¤¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”¹ÁŒ€ô¹•Ü9ÁIÕ¹Ñ¥µ” 4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•ÐµÝÉ½¹œµÑ¡É•…µ¹ÁŒˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰ÀÄÈµµ…É­•ÐµÝÉ½¹œµÑ¡É•…µ¹ÁŒˆ¤°4(€€€€€€€€€€€¥Ñä°4(€€€€€€€€€€€€ÔÁ˜¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹•Ýmtì¥Ñäô°4(€€€€€€€€€€€¹•Ýmtì¹ÁŒô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹I•ÍÕ±ÐÉ•ÍÕ±Ð€ô¹Õ±°ì4(€€€€€€€Q¡É•…½™™Q¡É•…€ô¹•ÜQ¡É•…  ¤€ôøÉ•ÍÕ±Ð€ôÍ•ÉÙ¥”¹QÉåá•ÕÑ•5…É­•ÑAÕÉ¡…Í”¡¹ÁŒ°¥Ñä¹5…É­•Ð°¥Ñ•´°€Ä¤¤ì4(€€€€€€€½™™Q¡É•…¹MÑ…ÉÐ ¤ì4(€€€€€€€½™™Q¡É•…¹)½¥¸ ¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð°%Ì¹9½Ð¹9Õ±°¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹MÕ•ÍÌ°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹…¥±ÕÉ•I•…Í½¸°%Ì¹ÅÕ…±Q¼¡½¹½µåQÉ…¹Í…Ñ¥½¹…¥±ÕÉ•I•…Í½¸¹QÉ…¹Í…Ñ¥½¹½µµ¥Ñ…¥±•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¹ÁŒ¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÔÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¹ÁŒ¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ô¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…ÍÍ•ÍÍµ•¹Ð°%Ì¹ÅÕ…±Q¼¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹AÉ½Ñ½½±…Õ±Ñ•¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ5…É­•Ñ¥É•Ñ5ÕÑ…Ñ¥½¹}UÁ‘…Ñ•Í1¥Ù•5…É­•Ñ]¥Ñ¹•ÍÍ¹‘M­¥ÁÍ9½=ÁÌ ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥¹¥Ñ¥…±%Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµµ…É­•Ðµ‘¥É•Ðµ¥¹¥Ñ¥…°ˆ°€Õ˜¤ì4(€€€€€€€%Ñ•µ…Ñ„…‘‘•‘%Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµµ…É­•Ðµ‘¥É•Ðµ…‘‘•ˆ°€Ý˜¤ì4(€€€€€€€¥ÑåIÕ¹Ñ¥µ”¥Ñä€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•¥Ñä 4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•Ðµ‘¥É•Ðµ¥Ñäˆ°4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•Ðµ‘¥É•Ðµ±½…Ñ¥½¸ˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•5…É­•Ñ%Ñ•´¡¥¹¥Ñ¥…±%Ñ•´°€Ð°€Ð¤¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”¹ÁŒ€ô¹•Ü9ÁIÕ¹Ñ¥µ” 4(€€€€€€€€€€€€‰ÀÄÈµµ…É­•Ðµ‘¥É•Ðµ¹ÁŒˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰ÀÄÈµµ…É­•Ðµ‘¥É•Ðµ¹ÁŒˆ¤°4(€€€€€€€€€€€¥Ñä°4(€€€€€€€€€€€€ÄÁ˜¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹•Ýmtì¥Ñäô°4(€€€€€€€€€€€¹•Ýmtì¹ÁŒô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ‰•™½É”°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”‰•™½É•…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°‰•™½É•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€±½¹œ¥¹¥Ñ¥…±5…É­•ÑI•Ù¥Í¥½¸€ô¥Ñä¹5…É­•Ð¹I•Ù¥Í¥½¸ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹‘‘MÑ½¬¡…‘‘•‘%Ñ•´°€Ì¤°%Ì¹ÅÕ…±Q¼ Ì¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹‘‘MÑ½¬¡¥¹¥Ñ¥…±%Ñ•´°€À¤°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹I•µ½Ù•MÑ½­UÁQ¼¡¥¹¥Ñ¥…±%Ñ•´°€À¤°%Ì¹i•É¼¤ì4(€€€€€€€¥Ñä¹5…É­•Ð¹UÁ‘…Ñ•AÉ¥•Ì ¤ì4(4(€€€€€€€¥¹¥Ñ¥…±%Ñ•´¹‰…Í•AÉ¥”€ô€Ù˜ì4(€€€€€€€¥Ñä¹5…É­•Ð¹UÁ‘…Ñ•AÉ¥•Ì ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹•ÑAÉ¥”¡¥¹¥Ñ¥…±%Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ù˜¤¤ì4(€€€€€€€¥Ñä¹5…É­•Ð¹UÁ‘…Ñ•AÉ¥•Ì ¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹%Ñ•µÌ¹½Õ¹Ð°%Ì¹ÅÕ…±Q¼ È¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹I•Ù¥Í¥½¸°%Ì¹ÅÕ…±Q¼¡¥¹¥Ñ¥…±5…É­•ÑI•Ù¥Í¥½¸€¬€È¤°4(€€€€€€€€€€€€‰Ñ¡”É½Ü…‘‘¥Ñ¥½¸…¹¡…¹•µÁÉ¥”É•™É•Í •… ½µµ¥Ð½¹”5…É­•ÐÉ•Ù¥Í¥½¸ˆ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ…™Ñ•È°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…™Ñ•É…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°…™Ñ•É…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…™Ñ•È°%Ì¹ÅÕ…±Q¼¡‰•™½É”€¬€È¤°€‰Ñ¡”É½Ü…‘‘¥Ñ¥½¸…¹¡…¹•µÁÉ¥”É•™É•Í …É”É•Á½ÉÑ•ì¹¼µ½ÁÌ…É”¹½Ðˆ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…ÍÍ•ÍÍµ•¹Ð¹Q½MÑÉ¥¹œ ¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ…¥±å5…É­•ÑAÉ½‘ÕÑ¥½¹}I•Á½ÉÑÍ5…É­•Ñ½µµ¥Ñ%¹Í¥‘•‘Ù…¹” ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµ‘…¥±äµµ…É­•ÐµÁÉ½‘ÕÑ¥½¸ˆ°€Õ˜¤ì4(€€€€€€€¥Ñå…Ñ„¥Ñå…Ñ„€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•¥Ñå…Ñ„ 4(€€€€€€€€€€€€‰ÀÄÈµ‘…¥±äµµ…É­•ÐµÁÉ½‘ÕÑ¥½¸µ¥Ñäµ‘…Ñ„ˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•5…É­•Ñ%Ñ•´¡¥Ñ•´°€ÄÀ°€ÄÀ¤¤ì4(€€€€€€€¥Ñå…Ñ„¹ÁÉ½‘ÕÑ¥½¹½¹™¥Ì¹‘¡¹•Ü¥ÑåAÉ½‘ÕÑ¥½¹½¹™¥œì¥Ñ•´€ô¥Ñ•´°…µ½Õ¹ÑA•É…ä€ô€Èô¤ì4(€€€€€€€¥ÑåIÕ¹Ñ¥µ”¥Ñä€ô¹•Ü¥ÑåIÕ¹Ñ¥µ” 4(€€€€€€€€€€€€‰ÀÄÈµ‘…¥±äµµ…É­•ÐµÁÉ½‘ÕÑ¥½¸µ¥Ñäˆ°4(€€€€€€€€€€€¥Ñå…Ñ„°4(€€€€€€€€€€€¹•ÜMÁ…Ñ¥…±1½…Ñ¥½¹IÕ¹Ñ¥µ” ‰ÀÄÈµ‘…¥±äµµ…É­•ÐµÁÉ½‘ÕÑ¥½¸µ±½…Ñ¥½¸ˆ¤¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹•Ýmtì¥Ñäô°4(€€€€€€€€€€€¹•Ü9ÁIÕ¹Ñ¥µ•lÁt°4(€€€€€€€€€€€•½¹½µå¹…‰±•èÑÉÕ”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(€€€€€€€±½¹œ¥¹¥Ñ¥…±5…É­•ÑI•Ù¥Í¥½¸€ô¥Ñä¹5…É­•Ð¹I•Ù¥Í¥½¸ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ‰•™½É”°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”‰•™½É•…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°‰•™½É•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉå‘Ù…¹•…ä¡½ÕÐM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘Ù…¹•…¥±ÕÉ”…‘Ù…¹•…¥±ÕÉ”¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…‘Ù…¹•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹I•Ù¥Í¥½¸°%Ì¹ÅÕ…±Q¼¡¥¹¥Ñ¥…±5…É­•ÑI•Ù¥Í¥½¸€¬€Ä¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ ÄÈ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ…™Ñ•È°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…™Ñ•É…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°…™Ñ•É…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…™Ñ•È°%Ì¹ÅÕ…±Q¼¡‰•™½É”€¬€Ä¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…ÍÍ•ÍÍµ•¹Ð¹Q½MÑÉ¥¹œ ¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ…¥±åÉ••½¹ÍÕµÁÑ¥½¹}I•Á½ÉÑÍ5…É­•Ñ½µµ¥Ñ%¹Í¥‘•‘Ù…¹” ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµ‘…¥±äµ™É•”µ½¹ÍÕµÁÑ¥½¸ˆ°€Õ˜¤ì4(€€€€€€€¥Ñå…Ñ„¥Ñå…Ñ„€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•¥Ñå…Ñ„ 4(€€€€€€€€€€€€‰ÀÄÈµ‘…¥±äµ™É•”µ½¹ÍÕµÁÑ¥½¸µ¥Ñäˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•5…É­•Ñ%Ñ•´¡¥Ñ•´°€Ð°€Ð¤¤ì4(€€€€€€€¥Ñå…Ñ„¹µ…É­•Ñ%Ñ•µÍlÁt¹½¹ÍÕµÁÑ¥½¹A•ÈÄÀÀÁA½ÁÕ±…Ñ¥½¸€ô€Å˜ì4(€€€€€€€¥ÑåIÕ¹Ñ¥µ”¥Ñä€ô¹•Ü¥ÑåIÕ¹Ñ¥µ” 4(€€€€€€€€€€€€‰ÀÄÈµ‘…¥±äµ™É•”µ½¹ÍÕµÁÑ¥½¸µ¥Ñäˆ°4(€€€€€€€€€€€¥Ñå…Ñ„°4(€€€€€€€€€€€¹•ÜMÁ…Ñ¥…±1½…Ñ¥½¹IÕ¹Ñ¥µ” ‰ÀÄÈµ‘…¥±äµ™É•”µ½¹ÍÕµÁÑ¥½¸µ±½…Ñ¥½¸ˆ¤¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹•Ýmtì¥Ñäô°4(€€€€€€€€€€€¹•Ü9ÁIÕ¹Ñ¥µ•lÁt°4(€€€€€€€€€€€•½¹½µå¹…‰±•èÑÉÕ”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(€€€€€€€±½¹œ¥¹¥Ñ¥…±5…É­•ÑI•Ù¥Í¥½¸€ô¥Ñä¹5…É­•Ð¹I•Ù¥Í¥½¸ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ‰•™½É”°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”‰•™½É•…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°‰•™½É•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉå‘Ù…¹•…ä¡½ÕÐM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘Ù…¹•…¥±ÕÉ”…‘Ù…¹•…¥±ÕÉ”¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…‘Ù…¹•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ì¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹I•Ù¥Í¥½¸°%Ì¹ÅÕ…±Q¼¡¥¹¥Ñ¥…±5…É­•ÑI•Ù¥Í¥½¸€¬€Ä¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ…™Ñ•È°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…™Ñ•É…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°…™Ñ•É…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…™Ñ•È°%Ì¹ÅÕ…±Q¼¡‰•™½É”€¬€Ä¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…ÍÍ•ÍÍµ•¹Ð¹Q½MÑÉ¥¹œ ¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ…¥±å¡…¹•‘AÉ¥•I•™É•Í¡}I•Á½ÉÑÍ5…É­•Ñ½µµ¥Ñ%¹Í¥‘•‘Ù…¹” ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµ‘…¥±äµÁÉ¥”µÉ•™É•Í ˆ°€Õ˜¤ì4(€€€€€€€¥Ñå…Ñ„¥Ñå…Ñ„€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•¥Ñå…Ñ„ 4(€€€€€€€€€€€€‰ÀÄÈµ‘…¥±äµÁÉ¥”µÉ•™É•Í µ¥Ñäˆ°4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•5…É­•Ñ%Ñ•´¡¥Ñ•´°€ÄÀ°€ÄÀ¤¤ì4(€€€€€€€¥ÑåIÕ¹Ñ¥µ”¥Ñä€ô¹•Ü¥ÑåIÕ¹Ñ¥µ” 4(€€€€€€€€€€€€‰ÀÄÈµ‘…¥±äµÁÉ¥”µÉ•™É•Í µ¥Ñäˆ°4(€€€€€€€€€€€¥Ñå…Ñ„°4(€€€€€€€€€€€¹•ÜMÁ…Ñ¥…±1½…Ñ¥½¹IÕ¹Ñ¥µ” ‰ÀÄÈµ‘…¥±äµÁÉ¥”µÉ•™É•Í µ±½…Ñ¥½¸ˆ¤¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹•Ýmtì¥Ñäô°4(€€€€€€€€€€€¹•Ü9ÁIÕ¹Ñ¥µ•lÁt°4(€€€€€€€€€€€•½¹½µå¹…‰±•èÑÉÕ”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(€€€€€€€±½¹œ¥¹¥Ñ¥…±5…É­•ÑI•Ù¥Í¥½¸€ô¥Ñä¹5…É­•Ð¹I•Ù¥Í¥½¸ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹•ÑAÉ¥”¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Õ˜¤¤ì4(€€€€€€€¥Ñ•´¹‰…Í•AÉ¥”€ô€Ù˜ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ‰•™½É”°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”‰•™½É•…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°‰•™½É•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉå‘Ù…¹•…ä¡½ÕÐM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘Ù…¹•…¥±ÕÉ”…‘Ù…¹•…¥±ÕÉ”¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…‘Ù…¹•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹•ÑAÉ¥”¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ù˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥Ñä¹5…É­•Ð¹I•Ù¥Í¥½¸°%Ì¹ÅÕ…±Q¼¡¥¹¥Ñ¥…±5…É­•ÑI•Ù¥Í¥½¸€¬€Ä¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ…™Ñ•È°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…™Ñ•É…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°…™Ñ•É…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…™Ñ•È°%Ì¹ÅÕ…±Q¼¡‰•™½É”€¬€Ä¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…ÍÍ•ÍÍµ•¹Ð¹Q½MÑÉ¥¹œ ¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ9ÁQÉ…‘•}I•Á½ÉÑÍ	Õå•É•‰¥Ñ¹‘MÕ•ÍÍ™Õ±½µÁ•¹Í…Ñ¥½¹]¡•¹M•±±•ÉÉ•‘¥Ñ…¹¹½Ñ½µµ¥Ð ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµ¹ÁŒµÑÉ…‘”µ½µÁ•¹Í…Ñ¥½¸ˆ¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”‰Õå•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰‰Õå•Èµ½µÁ•¹Í…Ñ¥½¸ˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰‰Õå•Èµ½µÁ•¹Í…Ñ¥½¸ˆ¤°¹Õ±°°€ÄÀÁ˜¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”Í•±±•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰Í•±±•Èµ½µÁ•¹Í…Ñ¥½¸ˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰Í•±±•Èµ½µÁ•¹Í…Ñ¥½¸ˆ¤°¹Õ±°°€ÈÁ˜¤ì4(€€€€€€€Í•±±•È¹%¹Ù•¹Ñ½Éä¹‘‘%Ñ•´¡¥Ñ•´°€Ì°€Ñ˜¤ì4(€€€€€€€ÑåÁ•½˜¡5½¹•å½Õ¹ÑIÕ¹Ñ¥µ”¤¹•Ñ¥•± ‰É•Ù¥Í¥½¸ˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤4(€€€€€€€€€€€€¹M•ÑY…±Õ”¡Í•±±•È¹5½¹•å½Õ¹Ð°±½¹œ¹5…áY…±Õ”¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹Õ±°°4(€€€€€€€€€€€¹•Ýmtì‰Õå•È°Í•±±•Èô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ‰•™½É”°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”‰•™½É•…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°‰•™½É•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹I•ÍÕ±ÐÉ•ÍÕ±Ð€ôÍ•ÉÙ¥”¹QÉåá•ÕÑ•9ÁQÉ…‘”¡‰Õå•È°Í•±±•È°¥Ñ•´°€È°€ÄÁ˜¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹MÕ•ÍÌ°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹…¥±ÕÉ•I•…Í½¸°%Ì¹ÅÕ…±Q¼¡½¹½µåQÉ…¹Í…Ñ¥½¹…¥±ÕÉ•I•…Í½¸¹QÉ…¹Í…Ñ¥½¹½µµ¥Ñ…¥±•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰Õå•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÄÀÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÈÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ì¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ…™Ñ•È°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…™Ñ•É…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°…™Ñ•É…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…™Ñ•È°%Ì¹ÅÕ…±Q¼¡‰•™½É”€¬€È¤°€‰Ñ¡”‘•‰¥Ð…¹¥ÑÌ½µµ¥ÑÑ•½µÁ•¹Í…Ñ¥½¸…É”‰½Ñ É•Á½ÉÑ•ˆ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…ÍÍ•ÍÍµ•¹Ð¹Q½MÑÉ¥¹œ ¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ9ÁQÉ…‘•}I•Á½ÉÑÍ	½Ñ¡MÕ•ÍÍ™Õ±½Õ¹Ñ½µÁ•¹Í…Ñ¥½¹Í]¡•¹M•±±•É%¹Ù•¹Ñ½Éå…¹¹½Ñ½µµ¥Ð ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµ¹ÁŒµÑÉ…‘”µ¥¹Ù•¹Ñ½Éäµ½µÁ•¹Í…Ñ¥½¸ˆ¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”‰Õå•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰‰Õå•Èµ¥¹Ù•¹Ñ½Éäµ½µÁ•¹Í…Ñ¥½¸ˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰‰Õå•Èµ¥¹Ù•¹Ñ½Éäµ½µÁ•¹Í…Ñ¥½¸ˆ¤°¹Õ±°°€ÄÀÁ˜¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”Í•±±•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰Í•±±•Èµ¥¹Ù•¹Ñ½Éäµ½µÁ•¹Í…Ñ¥½¸ˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰Í•±±•Èµ¥¹Ù•¹Ñ½Éäµ½µÁ•¹Í…Ñ¥½¸ˆ¤°¹Õ±°°€ÈÁ˜¤ì4(€€€€€€€Í•±±•È¹%¹Ù•¹Ñ½Éä¹‘‘%Ñ•´¡¥Ñ•´°€Ì°€Ñ˜¤ì4(€€€€€€€ÑåÁ•½˜¡%¹Ù•¹Ñ½ÉåIÕ¹Ñ¥µ”¤¹•Ñ¥•± ‰É•Ù¥Í¥½¸ˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤4(€€€€€€€€€€€€¹M•ÑY…±Õ”¡Í•±±•È¹%¹Ù•¹Ñ½Éä°±½¹œ¹5…áY…±Õ”¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹Õ±°°4(€€€€€€€€€€€¹•Ýmtì‰Õå•È°Í•±±•Èô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ‰•™½É”°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”‰•™½É•…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°‰•™½É•…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹I•ÍÕ±ÐÉ•ÍÕ±Ð€ôÍ•ÉÙ¥”¹QÉåá•ÕÑ•9ÁQÉ…‘”¡‰Õå•È°Í•±±•È°¥Ñ•´°€È°€ÄÁ˜¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹MÕ•ÍÌ°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹…¥±ÕÉ•I•…Í½¸°%Ì¹ÅÕ…±Q¼¡½¹½µåQÉ…¹Í…Ñ¥½¹…¥±ÕÉ•I•…Í½¸¹QÉ…¹Í…Ñ¥½¹½µµ¥Ñ…¥±•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰Õå•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÄÀÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÈÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ì¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ…™Ñ•È°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…™Ñ•É…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹QÉÕ”°…™Ñ•É…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…™Ñ•È°%Ì¹ÅÕ…±Q¼¡‰•™½É”€¬€Ð¤°€‰‘•‰¥Ð°É•‘¥Ð°…¹‰½Ñ ½µµ¥ÑÑ•…½Õ¹Ð½µÁ•¹Í…Ñ¥½¹Ì…É”É•Á½ÉÑ•ˆ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€…ÍÍ•ÍÍµ•¹Ð¹Q½MÑÉ¥¹œ ¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ9ÁQÉ…‘•}AÉ••á¥ÍÑ¥¹U¹¹½Ñ¥™¥•‘=Ý¹•ÉÉ¥™ÑI•©•ÑÍ	•™½É•¹åQÉ…‘•½µµ¥Ð ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµ¹ÁŒµÑÉ…‘”µ‘É¥™Ðˆ¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”‰Õå•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰‰Õå•Èµ‘É¥™Ðˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰‰Õå•Èµ‘É¥™Ðˆ¤°¹Õ±°°€ÄÀÁ˜¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”Í•±±•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰Í•±±•Èµ‘É¥™Ðˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰Í•±±•Èµ‘É¥™Ðˆ¤°¹Õ±°°€ÈÁ˜¤ì4(€€€€€€€Í•±±•È¹%¹Ù•¹Ñ½Éä¹‘‘%Ñ•´¡¥Ñ•´°€Ì°€Ñ˜¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹Õ±°°4(€€€€€€€€€€€¹•Ýmtì‰Õå•È°Í•±±•Èô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ‰•™½É”°½ÕÐ|¤°%Ì¹QÉÕ”¤ì(€€€€€€€U¹‰¥¹‘ÕÉÉ•¹Ñ@ÄÉ=Ý¹•É	½Õ¹‘…Éä¡‰Õå•È¹5½¹•å½Õ¹Ð¤ì(€€€€€€€‰Õå•È¹5½¹•å½Õ¹Ð¹QÉåÉ•‘¥Ð Õ˜¤ì(4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹I•ÍÕ±ÐÉ•ÍÕ±Ð€ôÍ•ÉÙ¥”¹QÉåá•ÕÑ•9ÁQÉ…‘”¡‰Õå•È°Í•±±•È°¥Ñ•´°€È°€ÄÁ˜¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹MÕ•ÍÌ°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹…¥±ÕÉ•I•…Í½¸°%Ì¹ÅÕ…±Q¼¡½¹½µåQÉ…¹Í…Ñ¥½¹…¥±ÕÉ•I•…Í½¸¹QÉ…¹Í…Ñ¥½¹½µµ¥Ñ…¥±•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰Õå•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÄÀÕ˜¤°€‰Ñ¡”ÁÉ••á¥ÍÑ¥¹œ•áÑ•É¹…°É•‘¥ÐÉ•µ…¥¹Ì°‰ÕÐÑ¡”ÑÉ…‘”‘½•Ì¹½Ð‰•¥¸ˆ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÈÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰Õå•È¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ì¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåI•…‘9ÁI½ÍÑ•É•¹ÍÕÍ5ÕÑ…Ñ¥½¹Á½ ¡½ÕÐ±½¹œ…™Ñ•È°½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”•Á½¡…¥±ÕÉ”¤°4(€€€€€€€€€€€%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•Á½¡…¥±ÕÉ”°%Ì¹ÅÕ…±Q¼¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹AÉ½Ñ½½±…Õ±Ñ•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…™Ñ•È°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…ÍÍ•ÍÍµ•¹Ð°%Ì¹ÅÕ…±Q¼¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹AÉ½Ñ½½±…Õ±Ñ•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰•™½É”°%Ì¹i•É¼¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ9ÁQÉ…‘•}U¹É•¥ÍÑ•É•‘A…ÉÑ¥¥Á…¹Ñ%ÍI•©•Ñ•‘	•™½É•¹å=Ý¹•É½µµ¥Ð ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµ¹ÁŒµÑÉ…‘”µÕ¹É•¥ÍÑ•É•ˆ¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”‰Õå•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰‰Õå•ÈµÕ¹É•¥ÍÑ•É•ˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰‰Õå•ÈµÕ¹É•¥ÍÑ•É•ˆ¤°¹Õ±°°€ÄÀÁ˜¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”Í•±±•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰Í•±±•ÈµÕ¹É•¥ÍÑ•É•ˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰Í•±±•ÈµÕ¹É•¥ÍÑ•É•ˆ¤°¹Õ±°°€ÈÁ˜¤ì4(€€€€€€€Í•±±•È¹%¹Ù•¹Ñ½Éä¹‘‘%Ñ•´¡¥Ñ•´°€Ì°€Ñ˜¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹Õ±°°4(€€€€€€€€€€€¹•ÝmtìÍ•±±•Èô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹I•ÍÕ±ÐÉ•ÍÕ±Ð€ôÍ•ÉÙ¥”¹QÉåá•ÕÑ•9ÁQÉ…‘”¡‰Õå•È°Í•±±•È°¥Ñ•´°€È°€ÄÁ˜¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹MÕ•ÍÌ°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹…¥±ÕÉ•I•…Í½¸°%Ì¹ÅÕ…±Q¼¡½¹½µåQÉ…¹Í…Ñ¥½¹…¥±ÕÉ•I•…Í½¸¹QÉ…¹Í…Ñ¥½¹½µµ¥Ñ…¥±•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰Õå•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÄÀÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÈÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰Õå•È¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ì¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…ÍÍ•ÍÍµ•¹Ð°%Ì¹ÅÕ…±Q¼¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹AÉ½Ñ½½±…Õ±Ñ•¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ9ÁQÉ…‘•}]É½¹Q¡É•…‘‘µ¥ÍÍ¥½¹I•©•ÑÍ	•™½É•¹å=Ý¹•É½µµ¥Ð ¤4(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµ¹ÁŒµÑÉ…‘”µÝÉ½¹œµÑ¡É•…ˆ¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”‰Õå•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰‰Õå•ÈµÝÉ½¹œµÑ¡É•…ˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰‰Õå•ÈµÝÉ½¹œµÑ¡É•…ˆ¤°¹Õ±°°€ÄÀÁ˜¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”Í•±±•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰Í•±±•ÈµÝÉ½¹œµÑ¡É•…ˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰Í•±±•ÈµÝÉ½¹œµÑ¡É•…ˆ¤°¹Õ±°°€ÈÁ˜¤ì4(€€€€€€€Í•±±•È¹%¹Ù•¹Ñ½Éä¹‘‘%Ñ•´¡¥Ñ•´°€Ì°€Ñ˜¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹Õ±°°4(€€€€€€€€€€€¹•Ýmtì‰Õå•È°Í•±±•Èô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹I•ÍÕ±ÐÉ•ÍÕ±Ð€ô¹Õ±°ì4(€€€€€€€Q¡É•…½™™Q¡É•…€ô¹•ÜQ¡É•…  ¤€ôøÉ•ÍÕ±Ð€ôÍ•ÉÙ¥”¹QÉåá•ÕÑ•9ÁQÉ…‘”¡‰Õå•È°Í•±±•È°¥Ñ•´°€È°€ÄÁ˜¤¤ì4(€€€€€€€½™™Q¡É•…¹MÑ…ÉÐ ¤ì4(€€€€€€€½™™Q¡É•…¹)½¥¸ ¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð°%Ì¹9½Ð¹9Õ±°¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹MÕ•ÍÌ°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹…¥±ÕÉ•I•…Í½¸°%Ì¹ÅÕ…±Q¼¡½¹½µåQÉ…¹Í…Ñ¥½¹…¥±ÕÉ•I•…Í½¸¹QÉ…¹Í…Ñ¥½¹½µµ¥Ñ…¥±•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰Õå•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÄÀÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÈÁ˜¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰Õå•È¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ì¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…ÍÍ•ÍÍµ•¹Ð°%Ì¹ÅÕ…±Q¼¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹AÉ½Ñ½½±…Õ±Ñ•¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥@ÄÉ9ÁQÉ…‘•}M…ÑÕÉ…Ñ¥½¹™Ñ•É¥ÉÍÑ½µµ¥ÑAÉ•Í•ÉÙ•Í½µµ¥ÑÑ•‘1•…™¹‘…Õ±ÑÍAÉ½Ñ½½° ¤(€€€ì4(€€€€€€€%Ñ•µ…Ñ„¥Ñ•´€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•%Ñ•´ ‰ÀÄÈµ¹ÁŒµÑÉ…‘”µÁ½ÍÐµ½µµ¥Ðµ™…Õ±Ðˆ¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”‰Õå•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰‰Õå•ÈµÁ½ÍÐµ½µµ¥Ðµ™…Õ±Ðˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰‰Õå•ÈµÁ½ÍÐµ½µµ¥Ðµ™…Õ±Ðˆ¤°¹Õ±°°€ÄÀÁ˜¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”Í•±±•È€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰Í•±±•ÈµÁ½ÍÐµ½µµ¥Ðµ™…Õ±Ðˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰Í•±±•ÈµÁ½ÍÐµ½µµ¥Ðµ™…Õ±Ðˆ¤°¹Õ±°°€ÈÁ˜¤ì4(€€€€€€€Í•±±•È¹%¹Ù•¹Ñ½Éä¹‘‘%Ñ•´¡¥Ñ•´°€Ì°€Ñ˜¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹Õ±°°4(€€€€€€€€€€€¹•Ýmtì‰Õå•È°Í•±±•Èô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”€ô¹•Ü½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥” ¤ì4(€€€€€€€	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡Í•ÉÙ¥”°ÉÕ¹Ñ¥µ”¤ì4(€€€€€€€½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍAÉ½Ñ½½°ÁÉ½Ñ½½°€ô€¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍAÉ½Ñ½½°¥ÑåÁ•½˜¡M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”¤4(€€€€€€€€€€€€¹•Ñ¥•± ‰¹ÁI½ÍÑ•É•¹ÍÕÍAÉ½Ñ½½°ˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤4(€€€€€€€€€€€€¹•ÑY…±Õ”¡ÉÕ¹Ñ¥µ”¤ì4(€€€€€€€ÑåÁ•½˜¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍAÉ½Ñ½½°¤¹•Ñ¥•± ‰µÕÑ…Ñ¥½¹Á½ ˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤4(€€€€€€€€€€€€¹M•ÑY…±Õ”¡ÁÉ½Ñ½½°°±½¹œ¹5…áY…±Õ”¤ì4(4(€€€€€€€½¹½µåQÉ…¹Í…Ñ¥½¹I•ÍÕ±ÐÉ•ÍÕ±Ð€ôÍ•ÉÙ¥”¹QÉåá•ÕÑ•9ÁQÉ…‘”¡‰Õå•È°Í•±±•È°¥Ñ•´°€È°€ÄÁ˜¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹MÕ•ÍÌ°%Ì¹…±Í”°(€€€€€€€€€€€€‰Q¡”™¥ÉÍÐ±•…˜½µµ¥Ð¥ÌÉ•Ñ…¥¹•ì±…Ñ•ÈÝÉ¥Ñ•Ì…É”É•©•Ñ•…™Ñ•ÈÑ¡”ÁÉ½Ñ½½°™…Õ±ÑÌ°…¹Ñ¡¥Ì½¹ÑÉ…Ð…‘‘Ì¹¼ÑÉ…‘”É½±±‰…¬¸ˆ¤ì(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÍÕ±Ð¹…¥±ÕÉ•I•…Í½¸°%Ì¹ÅÕ…±Q¼¡½¹½µåQÉ…¹Í…Ñ¥½¹…¥±ÕÉ•I•…Í½¸¹QÉ…¹Í…Ñ¥½¹½µµ¥Ñ…¥±•¤¤ì(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰Õå•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ àÁ˜¤¤ì(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰Õå•È¹5½¹•å½Õ¹Ð¹I•Ù¥Í¥½¸°%Ì¹ÅÕ…±Q¼ Ä¤¤ì(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹5½¹•ä°%Ì¹ÅÕ…±Q¼ ÈÁ˜¤¤ì(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰Õå•È¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹i•É¼¤ì(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í•±±•È¹%¹Ù•¹Ñ½Éä¹•Ñµ½Õ¹Ð¡¥Ñ•´¤°%Ì¹ÅÕ…±Q¼ Ì¤¤ì(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”…ÍÍ•ÍÍµ•¹Ð¤°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…ÍÍ•ÍÍµ•¹Ð°%Ì¹ÅÕ…±Q¼¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹AÉ½Ñ½½±…Õ±Ñ•¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥…¥±åAÉ½™¥±•I•©•ÑÍ]É½¹Q¡É•…‘]¥Ñ¡½ÕÑ‘Ù…¹¥¹¹‘…Õ±ÑÍA…ÉÑ¥…±AÉ½Ñ½½° ¤4(€€€ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹Õ±°°4(€€€€€€€€€€€¹Õ±°°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(4(€€€€€€€‰½½°…‘Ù…¹•€ôÑÉÕ”ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘Ù…¹•…¥±ÕÉ”…‘Ù…¹•…¥±ÕÉ”€ôM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘Ù…¹•…¥±ÕÉ”¹9½¹”ì4(€€€€€€€Q¡É•…½™™Q¡É•…€ô¹•ÜQ¡É•…  ¤€ôø…‘Ù…¹•€ôÉÕ¹Ñ¥µ”¹QÉå‘Ù…¹•…ä¡½ÕÐ…‘Ù…¹•…¥±ÕÉ”¤¤ì4(€€€€€€€½™™Q¡É•…¹MÑ…ÉÐ ¤ì4(€€€€€€€½™™Q¡É•…¹)½¥¸ ¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…‘Ù…¹•°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…‘Ù…¹•…¥±ÕÉ”°%Ì¹ÅÕ…±Q¼¡M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘Ù…¹•…¥±ÕÉ”¹IÕ¹Ñ¥µ•…Õ±Ñ•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹ÕÉÉ•¹Ñ…ä°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”•¹ÍÕÍ…¥±ÕÉ”¤°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹ÍÕÍ…¥±ÕÉ”°%Ì¹ÅÕ…±Q¼¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹AÉ½Ñ½½±…Õ±Ñ•¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥IÕ¹Ñ¥µ•=Ý¹•‘±½­Á¥ÍI•©•Ñ]É½¹Q¡É•…‘]¥Ñ¡½ÕÑ‘Ù…¹¥¹œ ¤4(€€€ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°4(€€€€€€€€€€€¹Õ±°°4(€€€€€€€€€€€¹Õ±°°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(4(€€€€€€€á•ÁÑ¥½¸…‘Ù…¹•á•ÁÑ¥½¸€ô¹Õ±°ì4(€€€€€€€‰½½°ÑÉå‘Ù…¹•I•ÍÕ±Ð€ôÑÉÕ”ì4(€€€€€€€M¥µÕ±…Ñ¥½¹Q¥µ•‘Ù…¹•…¥±ÕÉ”ÑÉå‘Ù…¹•…¥±ÕÉ”€ôM¥µÕ±…Ñ¥½¹Q¥µ•‘Ù…¹•…¥±ÕÉ”¹9½¹”ì4(€€€€€€€Q¡É•…½™™Q¡É•…€ô¹•ÜQ¡É•…  ¤€ôø4(€€€€€€€ì4(€€€€€€€€€€€ÑÉä4(€€€€€€€€€€€ì4(€€€€€€€€€€€€€€€ÉÕ¹Ñ¥µ”¹M¥µÕ±…Ñ¥½¹Q¥µ”¹‘Ù…¹•…ä ¤ì4(€€€€€€€€€€€ô4(€€€€€€€€€€€…Ñ €¡á•ÁÑ¥½¸•á•ÁÑ¥½¸¤4(€€€€€€€€€€€ì4(€€€€€€€€€€€€€€€…‘Ù…¹•á•ÁÑ¥½¸€ô•á•ÁÑ¥½¸ì4(€€€€€€€€€€€ô4(4(€€€€€€€€€€€ÑÉå‘Ù…¹•I•ÍÕ±Ð€ôÉÕ¹Ñ¥µ”¹M¥µÕ±…Ñ¥½¹Q¥µ”¹QÉå‘Ù…¹•…ä¡½ÕÐÑÉå‘Ù…¹•…¥±ÕÉ”¤ì4(€€€€€€€ô¤ì4(€€€€€€€½™™Q¡É•…¹MÑ…ÉÐ ¤ì4(€€€€€€€½™™Q¡É•…¹)½¥¸ ¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…‘Ù…¹•á•ÁÑ¥½¸°%Ì¹QåÁ•=˜ñ%¹Ù…±¥‘=Á•É…Ñ¥½¹á•ÁÑ¥½¸ø ¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÑÉå‘Ù…¹•I•ÍÕ±Ð°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÑÉå‘Ù…¹•…¥±ÕÉ”°%Ì¹ÅÕ…±Q¼¡M¥µÕ±…Ñ¥½¹Q¥µ•‘Ù…¹•…¥±ÕÉ”¹IÕ¹Ñ¥µ•…Õ±Ñ•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹ÕÉÉ•¹Ñ…ä°%Ì¹i•É¼¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”•¹ÍÕÍ…¥±ÕÉ”¤°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹ÍÕÍ…¥±ÕÉ”°%Ì¹ÅÕ…±Q¼¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹AÉ½Ñ½½±…Õ±Ñ•¤¤ì4(€€€ô4(4(€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÙ½¥	¥¹‘@ÄÉ•¹ÍÕÍIÕ¹Ñ¥µ”¡½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”Í•ÉÙ¥”°M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”¤(€€€ì4(€€€€€€€ÑåÁ•½˜¡M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”¤4(€€€€€€€€€€€€¹•Ñ5•Ñ¡½ ‰	¥¹‘@ÄÉ½¹½µåQÉ…¹Í…Ñ¥½¹M•ÉÙ¥”ˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤4(€€€€€€€€€€€€¹%¹Ù½­”¡ÉÕ¹Ñ¥µ”°¹•Ü½‰©•ÑmtìÍ•ÉÙ¥”ô¤ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÙ½¥U¹‰¥¹‘ÕÉÉ•¹Ñ@ÄÉ=Ý¹•É	½Õ¹‘…Éä¡½‰©•Ð½Ý¹•È¤(€€€ì(€€€€€€€Õ¹Œñ‰½½°ø…‘µ¥ÍÍ¥½¸€ô€¡Õ¹Œñ‰½½°ø¥½Ý¹•È¹•ÑQåÁ” ¤¹•Ñ¥•± (€€€€€€€€€€€€‰ÀÄÉ5ÕÑ…Ñ¥½¹‘µ¥ÍÍ¥½¸ˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤¹•ÑY…±Õ”¡½Ý¹•È¤ì(€€€€€€€Ñ¥½¸½µµ¥ÑÑ•€ô€¡Ñ¥½¸¥½Ý¹•È¹•ÑQåÁ” ¤¹•Ñ¥•± (€€€€€€€€€€€€‰ÀÄÉ5ÕÑ…Ñ¥½¹½µµ¥ÑÑ•ˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤¹•ÑY…±Õ”¡½Ý¹•È¤ì(€€€€€€€5•Ñ¡½‘%¹™¼µ•Ñ¡½€ô½Ý¹•È¹•ÑQåÁ” ¤¹•Ñ5•Ñ¡½ (€€€€€€€€€€€€‰U¹‰¥¹‘@ÄÉ5ÕÑ…Ñ¥½¹	½Õ¹‘…Éäˆ°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤ì(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡µ•Ñ¡½°%Ì¹9½Ð¹9Õ±°¤ì(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡µ•Ñ¡½¹%¹Ù½­”¡½Ý¹•È°¹•Ü½‰©•Ñmtì…‘µ¥ÍÍ¥½¸°½µµ¥ÑÑ•ô¤°%Ì¹QÉÕ”¤ì(€€€ô(4(€€€mQ•ÍÑ…Í”¡™…±Í”¥t4(€€€mQ•ÍÑ…Í”¡ÑÉÕ”¥t4(€€€ÁÕ‰±¥ŒÙ½¥…¥±åAÉ½™¥±•…Õ±ÑÍ‘µ¥ÍÍ¥½¹™Ñ•É%¹Ñ•ÉÉÕÁÑ•‘‘Ù…¹”¡‰½½°ÕÍ•	…Ñ ¤4(€€€ì4(€€€€€€€I•½É‘¥áÑÕÉ”É•½É‘Ì€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•I•½É‘¥áÑÕÉ” ¤ì4(€€€€€€€9ÁIÕ¹Ñ¥µ”¹ÁŒ€ô¹•Ü9ÁIÕ¹Ñ¥µ” ‰¹ÁŒµ…‘µ¥ÍÍ¥½¸µ™…¥±ÕÉ”ˆ°M¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•9ÁŒ ‰…‘µ¥ÍÍ¥½¸µ™…¥±ÕÉ”ˆ¤¤ì4(€€€€€€€9ÁÑ¥½¹…Ñ„…Ñ¥½¸€ôM¥µÕ±…Ñ¥½¹Q•ÍÑ…Ñ½Éä¹É•…Ñ•Ñ¥½¸ ‰…‘µ¥ÍÍ¥½¸µ™…¥±ÕÉ”µÑÉ…Ù•°ˆ°9ÁÑ¥½¹QåÁ”¹QÉ…Ù•°¤ì4(€€€€€€€…Ñ¥½¸¹‰…Í•UÑ¥±¥Ñä€ô€Å˜ì4(€€€€€€€¹ÁŒ¹9Á…Ñ„¹…½•ÍA…‘É…¼¹‘¡¹•Ü9A•™…Õ±ÑÑ¥½¸ì…Ñ¥½¸€ô…Ñ¥½¸°‰…Í•UÑ¥±¥Ñä€ô€Å˜ô¤ì4(€€€€€€€‘µ¥ÍÍ¥½¹AÉ½‰•Ñ¥½¹AÉ½Ù¥‘•ÈÁÉ½Ù¥‘•È€ô¹•Ü‘µ¥ÍÍ¥½¹AÉ½‰•Ñ¥½¹AÉ½Ù¥‘•È 4(€€€€€€€€€€€…Ñ¥½¸°4(€€€€€€€€€€€€ ¤€ôøÑ¡É½Ü¹•Ü%¹Ù…±¥‘=Á•É…Ñ¥½¹á•ÁÑ¥½¸ ‰¥¹©•Ñ•¥¹½µÁ±•Ñ”‘…¥±ä•á•ÕÑ¥½¸ˆ¤¤ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ô¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€É•½É‘Ì¹Q¥µ”°4(€€€€€€€€€€€¹Õ±°°4(€€€€€€€€€€€¹•Ýmtì¹ÁŒô°4(€€€€€€€€€€€•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€½¹™¥ÕÉ•‘Ñ¥½¹Ìè¹•Ýmtì…Ñ¥½¸ô°4(€€€€€€€€€€€¹Á•¥Í¥½¹MåÍÑ•´è¹•Ü9Á•¥Í¥½¹MåÍÑ•´¡¹•ÜMåÍÑ•´¹½±±•Ñ¥½¹Ì¹•¹•É¥Œ¹1¥ÍÐñ%9ÁÑ¥½¹AÉ½Ù¥‘•ÈøìÁÉ½Ù¥‘•Èô¤°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤ì4(4(€€€€€€€¥˜€¡ÕÍ•	…Ñ ¤4(€€€€€€€ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡É½ÝÌñ%¹Ù…±¥‘=Á•É…Ñ¥½¹á•ÁÑ¥½¸ø  ¤€ôøÉÕ¹Ñ¥µ”¹QÉå‘Ù…¹•…åÌ 4(€€€€€€€€€€€€€€€€È°4(€€€€€€€€€€€€€€€½ÕÐ|°4(€€€€€€€€€€€€€€€½ÕÐ|¤¤ì4(€€€€€€€ô4(€€€€€€€•±Í”4(€€€€€€€ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡É½ÝÌñ%¹Ù…±¥‘=Á•É…Ñ¥½¹á•ÁÑ¥½¸ø  ¤€ôøÉÕ¹Ñ¥µ”¹QÉå‘Ù…¹•…ä¡½ÕÐ|¤¤ì4(€€€€€€€ô4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹ÕÉÉ•¹Ñ…ä°%Ì¹ÅÕ…±Q¼ Å0¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”™…¥±ÕÉ”¤°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡™…¥±ÕÉ”°%Ì¹ÅÕ…±Q¼¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹AÉ½Ñ½½±…Õ±Ñ•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹QÉå‘Ù…¹•…ä¡½ÕÐM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘Ù…¹•…¥±ÕÉ”É•ÑÉå…¥±ÕÉ”¤°%Ì¹…±Í”¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡É•ÑÉå…¥±ÕÉ”°%Ì¹ÅÕ…±Q¼¡M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘Ù…¹•…¥±ÕÉ”¹IÕ¹Ñ¥µ•…Õ±Ñ•¤¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÉÕ¹Ñ¥µ”¹ÕÉÉ•¹Ñ…ä°%Ì¹ÅÕ…±Q¼ Å0¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥…¥±åAÉ½™¥±•I•©•ÑÍ5¥Íµ…Ñ¡•‘½¹ÍÑÉÕÑ¥½¹Q¡É•…‘¹‘@Äá½µÁ½Í¥Ñ¥½¸ ¤4(€€€ì4(€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐµ…¥¹Q¡É•…‘½¹Ñ•áÐ€ô4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤ì4(€€€€€€€á•ÁÑ¥½¸½¹ÍÑÉÕÑ¥½¹…¥±ÕÉ”€ô¹Õ±°ì4(€€€€€€€Q¡É•…½™™Q¡É•…€ô¹•ÜQ¡É•…  ¤€ôø4(€€€€€€€ì4(€€€€€€€€€€€ÑÉä4(€€€€€€€€€€€ì4(€€€€€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°¹Õ±°°¹Õ±°°•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèµ…¥¹Q¡É•…‘½¹Ñ•áÐ¤ì4(€€€€€€€€€€€ô4(€€€€€€€€€€€…Ñ €¡á•ÁÑ¥½¸•á•ÁÑ¥½¸¤4(€€€€€€€€€€€ì4(€€€€€€€€€€€€€€€½¹ÍÑÉÕÑ¥½¹…¥±ÕÉ”€ô•á•ÁÑ¥½¸ì4(€€€€€€€€€€€ô4(€€€€€€€ô¤ì4(€€€€€€€½™™Q¡É•…¹MÑ…ÉÐ ¤ì4(€€€€€€€½™™Q¡É•…¹)½¥¸ ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡½¹ÍÑÉÕÑ¥½¹…¥±ÕÉ”°%Ì¹QåÁ•=˜ñÉÕµ•¹Ñá•ÁÑ¥½¸ø ¤¤ì4(4(€€€€€€€ÍÍ•ÉÐ¹Q¡É½ÝÌñÉÕµ•¹Ñá•ÁÑ¥½¸ø  ¤€ôø¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ” 4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹Q¥µ” ¤°¹Õ±°°¹Õ±°°•½¹½µå¹…‰±•è™…±Í”°4(€€€€€€€€€€€ÀÄá‘%¹ÑÉ…‘…åAÉ½™¥±”è¹•Ü@Äá%¹ÑÉ…‘…åAÉ½™¥±” ‰Ý½É±ˆ°€‰ÁÉ½™¥±”ˆ°€‰½¹™¥œˆ°€‰½¹Ñ•¹Ðˆ¤°4(€€€€€€€€€€€ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐèM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ¹…ÁÑÕÉ•U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ ¤¤¤ì4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥	½½ÑÍÑÉ…ÁM½Á•ÍY…±¥‘…Ñ¥½¹Q¡É½Õ¡AÕ‰±¥…Ñ¥½¹¹‘I•Ù½­•Í…¥±•‘AÕ‰±¥…Ñ¥½¸ ¤4(€€€ì4(€€€€€€€M¥µÕ±…Ñ¥½¹½¹™¥…Ñ„½¹™¥œ€ôÍÍ•Ñ…Ñ…‰…Í”¹1½…‘ÍÍ•ÑÑA…Ñ ñM¥µÕ±…Ñ¥½¹½¹™¥…Ñ„ø 4(€€€€€€€€€€€€‰ÍÍ•ÑÌ½}AÉ½©•Ð½…Ñ„½M¥µÕ±…Ñ¥½¹Ì½M¥µÕ±…Ñ¥½¸µ•¹•É…±Q•ÍÐ¹…ÍÍ•Ðˆ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡½¹™¥œ°%Ì¹9½Ð¹9Õ±°¤ì4(4(€€€€€€€…µ•=‰©•ÐÍÕ•ÍÍ™Õ±=‰©•Ð€ô¹•Ü…µ•=‰©•Ð ‰@ÄÈ‰½½ÑÍÑÉ…À…‘µ¥ÍÍ¥½¸ÍÕ•ÍÌˆ¤ì4(€€€€€€€…µ•=‰©•Ð™…¥±•‘=‰©•Ð€ô¹•Ü…µ•=‰©•Ð ‰@ÄÈ‰½½ÑÍÑÉ…À…‘µ¥ÍÍ¥½¸™…¥±ÕÉ”ˆ¤ì4(€€€€€€€ÑÉä4(€€€€€€€ì4(€€€€€€€€€€€Q•ÍÑ•M¥µÕ±……¼ÍÕ•ÍÍ™Õ±	½½ÑÍÑÉ…À€ôÍÕ•ÍÍ™Õ±=‰©•Ð¹‘‘½µÁ½¹•¹ÐñQ•ÍÑ•M¥µÕ±……¼ø ¤ì4(€€€€€€€€€€€½¹™¥ÕÉ•M•±•Ñ•‘	½½ÑÍÑÉ…À¡ÍÕ•ÍÍ™Õ±	½½ÑÍÑÉ…À°½¹™¥œ¤ì4(€€€€€€€€€€€‰½½°Ù…±¥‘…Ñ¥½¹Q…¥±]…Í	ÕÍä€ô™…±Í”ì4(€€€€€€€€€€€%¹Ù½­•%¹¥Ñ¥…±¥é•M¥µÕ±…Ñ¥½¸¡ÍÕ•ÍÍ™Õ±	½½ÑÍÑÉ…À°ÍÑ…•%€ôø4(€€€€€€€€€€€ì4(€€€€€€€€€€€€€€€¥˜€¡ÍÑ…•%€ôô€‰Àä¹•¹•Í¥Ì¹Ù…±¥‘…Ñ”µÁÉ½™¥±”½ØÄˆ¤4(€€€€€€€€€€€€€€€ì4(€€€€€€€€€€€€€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ÉÕ¹Ñ¥µ”€ôI•…‘AÉ¥Ù…Ñ•¥•±ñM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ø¡ÍÕ•ÍÍ™Õ±	½½ÑÍÑÉ…À°€‰Í¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ˆ¤ì4(€€€€€€€€€€€€€€€€€€€Ù…±¥‘…Ñ¥½¹Q…¥±]…Í	ÕÍä€ô€…ÉÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”™…¥±ÕÉ”¤4(€€€€€€€€€€€€€€€€€€€€€€€€˜˜™…¥±ÕÉ”€ôô½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹=Á•É…Ñ¥½¹%¹AÉ½É•ÍÌì4(€€€€€€€€€€€€€€€ô4(€€€€€€€€€€€ô¤ì4(4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Ù…±¥‘…Ñ¥½¹Q…¥±]…Í	ÕÍä°%Ì¹QÉÕ”¤ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÍÕ•ÍÍ™Õ±	½½ÑÍÑÉ…À¹	½½ÑÍÑÉ…À°%Ì¹9½Ð¹9Õ±°¤ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡ÍÕ•ÍÍ™Õ±	½½ÑÍÑÉ…À¹IÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¥‘±”¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€€€€€¥‘±”¹Q½MÑÉ¥¹œ ¤¤ì4(4(€€€€€€€€€€€Q•ÍÑ•M¥µÕ±……¼™…¥±•‘	½½ÑÍÑÉ…À€ô™…¥±•‘=‰©•Ð¹‘‘½µÁ½¹•¹ÐñQ•ÍÑ•M¥µÕ±……¼ø ¤ì4(€€€€€€€€€€€½¹™¥ÕÉ•M•±•Ñ•‘	½½ÑÍÑÉ…À¡™…¥±•‘	½½ÑÍÑÉ…À°½¹™¥œ¤ì4(€€€€€€€€€€€Q…É•Ñ%¹Ù½…Ñ¥½¹á•ÁÑ¥½¸Ñ¡É½Ý¸€ôÍÍ•ÉÐ¹Q¡É½ÝÌñQ…É•Ñ%¹Ù½…Ñ¥½¹á•ÁÑ¥½¸ø  ¤€ôø4(€€€€€€€€€€€€€€€%¹Ù½­•%¹¥Ñ¥…±¥é•M¥µÕ±…Ñ¥½¸¡™…¥±•‘	½½ÑÍÑÉ…À°ÍÑ…•%€ôø4(€€€€€€€€€€€€€€€ì4(€€€€€€€€€€€€€€€€€€€¥˜€¡ÍÑ…•%€ôô€‰Àä¹•¹•Í¥Ì¹ÁÕ‰±¥Í ½ØÄˆ¤4(€€€€€€€€€€€€€€€€€€€€€€€Ñ¡É½Ü¹•Ü%¹Ù…±¥‘=Á•É…Ñ¥½¹á•ÁÑ¥½¸ ‰¥¹©•Ñ•Á½ÍÐµÁÕ‰±¥…Ñ¥½¸…±±‰…¬™…¥±ÕÉ”ˆ¤ì4(€€€€€€€€€€€€€€€ô¤¤ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Ñ¡É½Ý¸¹%¹¹•Éá•ÁÑ¥½¸°%Ì¹QåÁ•=˜ñ%¹Ù…±¥‘=Á•É…Ñ¥½¹á•ÁÑ¥½¸ø ¤¤ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡™…¥±•‘	½½ÑÍÑÉ…À¹	½½ÑÍÑÉ…À°%Ì¹9Õ±°¤ì4(€€€€€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”™…¥±•‘IÕ¹Ñ¥µ”€ôI•…‘AÉ¥Ù…Ñ•¥•±ñM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ø¡™…¥±•‘	½½ÑÍÑÉ…À°€‰Í¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ”ˆ¤ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡™…¥±•‘IÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”™…Õ±Ñ•¤°%Ì¹…±Í”¤ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡™…Õ±Ñ•°%Ì¹ÅÕ…±Q¼¡½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”¹AÉ½Ñ½½±…Õ±Ñ•¤¤ì4(4(€€€€€€€€€€€™…¥±•‘	½½ÑÍÑÉ…À¹MÑ…ÉÐ ¤ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡™…¥±•‘	½½ÑÍÑÉ…À¹	½½ÑÍÑÉ…À°%Ì¹9Õ±°°€‰„™…¥±•MÑ…ÉÐ…ÑÑ•µÁÐ¥ÌÁ•Éµ…¹•¹Ñ±ä±…Ñ¡•ˆ¤ì4(€€€€€€€ô4(€€€€€€€™¥¹…±±ä4(€€€€€€€ì4(€€€€€€€€€€€U¹¥Ñå¹¥¹”¹=‰©•Ð¹•ÍÑÉ½å%µµ•‘¥…Ñ”¡ÍÕ•ÍÍ™Õ±=‰©•Ð¤ì4(€€€€€€€€€€€U¹¥Ñå¹¥¹”¹=‰©•Ð¹•ÍÑÉ½å%µµ•‘¥…Ñ”¡™…¥±•‘=‰©•Ð¤ì4(€€€€€€€ô4(€€€ô4(4(€€€mQ•ÍÑt4(€€€ÁÕ‰±¥ŒÙ½¥MÑ…ÉÑ…ÁÑÕÉ•Í¹‘A…ÍÍ•ÍQ¡•M•±•Ñ•‘AÉ½™¥±•=Ý¹•ÉQ¡É•… ¤4(€€€ì4(€€€€€€€M¥µÕ±…Ñ¥½¹½¹™¥…Ñ„½¹™¥œ€ôÍÍ•Ñ…Ñ…‰…Í”¹1½…‘ÍÍ•ÑÑA…Ñ ñM¥µÕ±…Ñ¥½¹½¹™¥…Ñ„ø 4(€€€€€€€€€€€€‰ÍÍ•ÑÌ½}AÉ½©•Ð½…Ñ„½M¥µÕ±…Ñ¥½¹Ì½M¥µÕ±…Ñ¥½¸µ•¹•É…±Q•ÍÐ¹…ÍÍ•Ðˆ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡½¹™¥œ°%Ì¹9½Ð¹9Õ±°¤ì4(4(€€€€€€€…µ•=‰©•Ð‰½½ÑÍÑÉ…Á=‰©•Ð€ô¹•Ü…µ•=‰©•Ð ‰@ÄÈMÑ…ÉÐ½Ý¹•È‰¥¹‘¥¹œˆ¤ì4(€€€€€€€ÑÉä4(€€€€€€€ì4(€€€€€€€€€€€Q•ÍÑ•M¥µÕ±……¼‰½½ÑÍÑÉ…À€ô‰½½ÑÍÑÉ…Á=‰©•Ð¹‘‘½µÁ½¹•¹ÐñQ•ÍÑ•M¥µÕ±……¼ø ¤ì4(€€€€€€€€€€€]É¥Ñ•AÉ¥Ù…Ñ•¥•±¡‰½½ÑÍÑÉ…À°€‰Í¥µÕ±…Ñ¥½¹½¹™¥œˆ°½¹™¥œ¤ì4(€€€€€€€€€€€]É¥Ñ•AÉ¥Ù…Ñ•¥•±¡‰½½ÑÍÑÉ…À°€‰ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹AÉ½™¥±”ˆ°M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹AÉ½™¥±”¹U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ¤ì4(4(€€€€€€€€€€€‰½½ÑÍÑÉ…À¹MÑ…ÉÐ ¤ì4(4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰½½ÑÍÑÉ…À¹	½½ÑÍÑÉ…À°%Ì¹9½Ð¹9Õ±°¤ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡I•…‘AÉ¥Ù…Ñ•¥•±ñQ¡É•…ø¡‰½½ÑÍÑÉ…À°€‰‰½½ÑÍÑÉ…ÁMÑ…ÉÑQ¡É•…ˆ¤°%Ì¹M…µ•Ì¡Q¡É•…¹ÕÉÉ•¹ÑQ¡É•…¤¤ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡I•…‘AÉ¥Ù…Ñ•¥•±ñ¥¹Ðø¡‰½½ÑÍÑÉ…À°€‰‰½½ÑÍÑÉ…ÁMÑ…ÉÑQ¡É•…‘%ˆ¤°%Ì¹ÅÕ…±Q¼¡Q¡É•…¹ÕÉÉ•¹ÑQ¡É•…¹5…¹…•‘Q¡É•…‘%¤¤ì4(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡‰½½ÑÍÑÉ…À¹IÕ¹Ñ¥µ”¹QÉåÍÍ•ÍÍ9ÁI½ÍÑ•É•¹ÍÕÌ¡½ÕÐ½¹Ñ¥¹Õ…Ñ¥½¹•¹ÍÕÍ…¥±ÕÉ”™…¥±ÕÉ”¤°%Ì¹QÉÕ”°4(€€€€€€€€€€€€€€€™…¥±ÕÉ”¹Q½MÑÉ¥¹œ ¤¤ì4(€€€€€€€ô4(€€€€€€€™¥¹…±±ä4(€€€€€€€ì4(€€€€€€€€€€€U¹¥Ñå¹¥¹”¹=‰©•Ð¹•ÍÑÉ½å%µµ•‘¥…Ñ”¡‰½½ÑÍÑÉ…Á=‰©•Ð¤ì4(€€€€€€€ô4(€€€ô4(4(€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÙ½¥½¹™¥ÕÉ•M•±•Ñ•‘	½½ÑÍÑÉ…À¡Q•ÍÑ•M¥µÕ±……¼‰½½ÑÍÑÉ…À°M¥µÕ±…Ñ¥½¹½¹™¥…Ñ„½¹™¥œ¤4(€€€ì4(€€€€€€€Q¡É•…½Ý¹•ÉQ¡É•…€ôQ¡É•…¹ÕÉÉ•¹ÑQ¡É•…ì4(€€€€€€€]É¥Ñ•AÉ¥Ù…Ñ•¥•±¡‰½½ÑÍÑÉ…À°€‰Í¥µÕ±…Ñ¥½¹½¹™¥œˆ°½¹™¥œ¤ì4(€€€€€€€]É¥Ñ•AÉ¥Ù…Ñ•¥•±¡‰½½ÑÍÑÉ…À°€‰ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹AÉ½™¥±”ˆ°M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹AÉ½™¥±”¹U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ¤ì4(€€€€€€€]É¥Ñ•AÉ¥Ù…Ñ•¥•±¡‰½½ÑÍÑÉ…À°€‰‰½½ÑÍÑÉ…ÁMÑ…ÉÑQ¡É•…ˆ°½Ý¹•ÉQ¡É•…¤ì4(€€€€€€€]É¥Ñ•AÉ¥Ù…Ñ•¥•±¡‰½½ÑÍÑÉ…À°€‰‰½½ÑÍÑÉ…ÁMÑ…ÉÑQ¡É•…‘%ˆ°½Ý¹•ÉQ¡É•…¹5…¹…•‘Q¡É•…‘%¤ì4(€€€€€€€]É¥Ñ•AÉ¥Ù…Ñ•¥•±¡‰½½ÑÍÑÉ…À°€‰ÉÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐˆ°4(€€€€€€€€€€€¹•ÜM¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹½¹Ñ•áÐ 4(€€€€€€€€€€€€€€€M¥µÕ±…Ñ¥½¹IÕ¹Ñ¥µ•‘µ¥ÍÍ¥½¹AÉ½™¥±”¹U¹¥Ñå	½½ÑÍÑÉ…Á…¥±åXÄ°4(€€€€€€€€€€€€€€€½Ý¹•ÉQ¡É•…°4(€€€€€€€€€€€€€€€½Ý¹•ÉQ¡É•…¹5…¹…•‘Q¡É•…‘%¤¤ì4(€€€€€€€]É¥Ñ•AÉ¥Ù…Ñ•¥•±¡‰½½ÑÍÑÉ…À°€‰‰½½ÑÍÑÉ…ÁMÑ…ÉÑÑÑ•µÁÑ•ˆ°ÑÉÕ”¤ì4(€€€ô4(4(€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÙ½¥%¹Ù½­•%¹¥Ñ¥…±¥é•M¥µÕ±…Ñ¥½¸¡Q•ÍÑ•M¥µÕ±……¼‰½½ÑÍÑÉ…À°Ñ¥½¸ñÍÑÉ¥¹œøÍÑ…•½µÁ±•Ñ•¤4(€€€ì4(€€€€€€€5•Ñ¡½‘%¹™¼µ•Ñ¡½€ôÑåÁ•½˜¡Q•ÍÑ•M¥µÕ±……¼¤¹•Ñ5•Ñ¡½ 4(€€€€€€€€€€€€‰%¹¥Ñ¥…±¥é•M¥µÕ±…Ñ¥½¸ˆ°4(€€€€€€€€€€€	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡µ•Ñ¡½°%Ì¹9½Ð¹9Õ±°¤ì4(€€€€€€€µ•Ñ¡½¹%¹Ù½­”¡‰½½ÑÍÑÉ…À°¹•Ü½‰©•ÑmtìÍÑ…•½µÁ±•Ñ•ô¤ì4(€€€ô4(4(€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒPI•…‘AÉ¥Ù…Ñ•¥•±ñPø¡½‰©•ÐÑ…É•Ð°ÍÑÉ¥¹œ¹…µ”¤4(€€€ì4(€€€€€€€¥•±‘%¹™¼™¥•±€ôÑ…É•Ð¹•ÑQåÁ” ¤¹•Ñ¥•±¡¹…µ”°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡™¥•±°%Ì¹9½Ð¹9Õ±°¤ì4(€€€€€€€É•ÑÕÉ¸€¡P¥™¥•±¹•ÑY…±Õ”¡Ñ…É•Ð¤ì4(€€€ô4(4(€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÙ½¥]É¥Ñ•AÉ¥Ù…Ñ•¥•±ñPø¡½‰©•ÐÑ…É•Ð°ÍÑÉ¥¹œ¹…µ”°PÙ…±Õ”¤4(€€€ì4(€€€€€€€¥•±‘%¹™¼™¥•±€ôÑ…É•Ð¹•ÑQåÁ” ¤¹•Ñ¥•±¡¹…µ”°	¥¹‘¥¹±…Ì¹%¹ÍÑ…¹”ð	¥¹‘¥¹±…Ì¹9½¹AÕ‰±¥Œ¤ì4(€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡™¥•±°%Ì¹9½Ð¹9Õ±°¤ì4(€€€€€€€™¥•±¹M•ÑY…±Õ”¡Ñ…É•Ð°Ù…±Õ”¤ì4(€€€ô4(4(€€€ÁÉ¥Ù…Ñ”Í•…±•±…ÍÌ‘µ¥ÍÍ¥½¹AÉ½‰•Ñ¥½¹AÉ½Ù¥‘•È€è%9ÁÑ¥½¹AÉ½Ù¥‘•È4(€€€ì4(€€€€€€€ÁÉ¥Ù…Ñ”É•…‘½¹±ä9ÁÑ¥½¹…Ñ„…Ñ¥½¸ì4(€€€€€€€ÁÉ¥Ù…Ñ”É•…‘½¹±äÑ¥½¸½‰Í•ÉÙ•‘µ¥ÍÍ¥½¸ì4(4(€€€€€€€ÁÕ‰±¥Œ‘µ¥ÍÍ¥½¹AÉ½‰•Ñ¥½¹AÉ½Ù¥‘•È¡9ÁÑ¥½¹…Ñ„…Ñ¥½¸°Ñ¥½¸½‰Í•ÉÙ•‘µ¥ÍÍ¥½¸¤4(€€€€€€€ì4(€€€€€€€€€€€Ñ¡¥Ì¹…Ñ¥½¸€ô…Ñ¥½¸ì4(€€€€€€€€€€€Ñ¡¥Ì¹½‰Í•ÉÙ•‘µ¥ÍÍ¥½¸€ô½‰Í•ÉÙ•‘µ¥ÍÍ¥½¸ì4(€€€€€€€ô4(4(€€€€€€€ÁÕ‰±¥Œ‰½½°!…¹‘±•ÍÑ¥½¸¡9ÁÑ¥½¹…Ñ„…¹‘¥‘…Ñ”¤€ôøI•™•É•¹•ÅÕ…±Ì¡…¹‘¥‘…Ñ”°…Ñ¥½¸¤ì4(4(€€€€€€€ÁÕ‰±¥Œ9ÁÑ¥½¹IÕ¹Ñ¥µ”É•…Ñ•Ñ¥½¸¡9ÁIÕ¹Ñ¥µ”¹ÁIÕ¹Ñ¥µ”°9ÁÑ¥½¹…Ñ„…¹‘¥‘…Ñ”°É•˜™±½…ÐÕÑ¥±¥Ñä¤4(€€€€€€€ì4(€€€€€€€€€€€½‰Í•ÉÙ•‘µ¥ÍÍ¥½¸ü¹%¹Ù½­” ¤ì4(€€€€€€€€€€€É•ÑÕÉ¸¹•Ü9ÁÑ¥½¹IÕ¹Ñ¥µ”¡…¹‘¥‘…Ñ”¤ì4(€€€€€€€ô4(4(€€€€€€€ÁÕ‰±¥Œ9ÁÑ¥½¹I•ÍÕ±ÐQÉåá•ÕÑ•Ñ¥½¸¡9ÁIÕ¹Ñ¥µ”¹ÁIÕ¹Ñ¥µ”°9ÁÑ¥½¹IÕ¹Ñ¥µ”…Ñ¥½¹IÕ¹Ñ¥µ”¤4(€€€€€€€ì4(€€€€€€€€€€€É•ÑÕÉ¸9ÁÑ¥½¹I•ÍÕ±Ð¹MÕ••‘• ¤ì4(€€€€€€€ô4(€€€ô4)ô4(