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
    public bool SellerWasAlive { get; }
    public bool SellerCouldReceive { get; }
    public bool CounterpartyCouldPay { get; }
    public bool MarketCouldAcceptStock { get; }
    public long InventoryRevision { get; }
    public long MarketRevision { get; }
    public long SellerMoneyRevision { get; }
    public long CounterpartyMoneyRevision { get; }
    internal KeyedSaleSnapshot(string input, string request, string actor, string proposal, string action, int version, string profile, string tick, string site, string counterparty, string item, MarketLiquidityMode mode, float price, int stock, int inventory, float sellerBalance, float counterpartyBalance, int requested, int effective, bool alive, bool canReceive, bool counterpartyCanPay, bool marketCanAccept, long inventoryRevision, long marketRevision, long sellerMoneyRevision, long counterpartyMoneyRevision)
    { ActorChoiceInputId=input; RequestId=request; ActorPersonId=actor; ProposalId=proposal; ActionSemanticId=action; ActionVersion=version; ProfileId=profile; LogicalTick=tick; MarketSiteId=site; CounterpartyId=counterparty; ItemDefinitionId=item; LiquidityMode=mode; UnitPrice=price; MarketStock=stock; SellerInventory=inventory; SellerBalance=sellerBalance; CounterpartyBalance=counterpartyBalance; RequestedQuantity=requested; EffectiveQuantity=effective; SellerWasAlive=alive; SellerCouldReceive=canReceive; CounterpartyCouldPay=counterpartyCanPay; MarketCouldAcceptStock=marketCanAccept; InventoryRevision=inventoryRevision; MarketRevision=marketRevision; SellerMoneyRevision=sellerMoneyRevision; CounterpartyMoneyRevision=counterpartyMoneyRevision; }
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

public sealed class EconomyTransactionService
{
    private readonly List<KeyedSaleReceipt> keyedSaleReceipts = new List<KeyedSaleReceipt>();

    public KeyedSaleReceipt TryExecuteKeyedMarketSale(
        string proposalId, string fingerprint, string actorChoiceInputId, string requestId,
        string actorPersonId, string actionSemanticId, int actionVersion, string profileId,
        string logicalTick, string marketSiteId, NpcRuntime seller, MarketRuntime market,
        ItemData item, int requestedQuantity)
    {
        if (string.IsNullOrWhiteSpace(proposalId) || string.IsNullOrWhiteSpace(fingerprint))
            return KeyedSaleReceipt.Rejected(proposalId, fingerprint, null, EconomyTransactionFailureReason.InvalidInput);

        MarketLiquidityMode mode = market != null ? market.Counterparty.LiquidityMode : MarketLiquidityMode.Open;
        string canonicalFingerprint = CreateCanonicalFingerprint(fingerprint, actorChoiceInputId, requestId,
            actorPersonId, seller != null && seller.PersonId != null ? seller.PersonId.ToString() : null,
            proposalId, actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            seller != null ? seller.RuntimeId : null, seller != null && seller.CurrentCity != null ? seller.CurrentCity.RuntimeId : null,
            market != null ? market.StockOwnerRuntimeId : null, mode,
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
        if (string.IsNullOrWhiteSpace(proposalId) || string.IsNullOrWhiteSpace(fingerprint)) return null;
        MarketLiquidityMode mode = market != null ? market.Counterparty.LiquidityMode : MarketLiquidityMode.Open;
        string canonicalFingerprint = CreateCanonicalFingerprint(fingerprint, actorChoiceInputId, requestId,
            actorPersonId, seller != null && seller.PersonId != null ? seller.PersonId.ToString() : null,
            proposalId, actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            seller != null ? seller.RuntimeId : null, seller != null && seller.CurrentCity != null ? seller.CurrentCity.RuntimeId : null,
            market != null ? market.StockOwnerRuntimeId : null, mode,
            item != null ? item.DefinitionId : null, requestedQuantity);
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
        for (int i = 0; i < keyedSaleReceipts.Count; i++)
            if (string.Equals(keyedSaleReceipts[i].ProposalId, receipt.ProposalId, StringComparison.Ordinal)) { keyedSaleReceipts[i] = receipt; return; }
        if (keyedSaleReceipts.Capacity < keyedSaleReceipts.Count + 1) keyedSaleReceipts.Capacity = keyedSaleReceipts.Count + 1;
        keyedSaleReceipts.Add(receipt);
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

        if (buyer.MoneyAccount.TryDebit(totalPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        if (seller.MoneyAccount.TryCredit(totalPrice) == false)
        {
            buyer.MoneyAccount.TryCredit(totalPrice);
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        if (seller.Inventory.RemoveItem(item, quantity) == false)
        {
            seller.MoneyAccount.TryDebit(totalPrice);
            buyer.MoneyAccount.TryCredit(totalPrice);
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        buyer.Inventory.AddItem(item, quantity, unitPrice);

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
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                actorRuntimeId: npc.RuntimeId);
        }

        int affordableQuantity = GetAffordableQuantity(npc.MoneyAccount.Balance, unitPrice);
        int quantity = Mathf.Min(requestedQuantity, marketItem.Amount, affordableQuantity);

        if (quantity <= 0)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientFunds,
                actorRuntimeId: npc.RuntimeId);
        }

        float totalPrice = unitPrice * quantity;

        if (IsValidNonNegativeFiniteAmount(totalPrice) == false
            || npc.MoneyAccount.CanDebit(totalPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientFunds,
                actorRuntimeId: npc.RuntimeId);
        }

        if (accountBacked == true && counterparty.MoneyAccount.CanCredit(totalPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.CounterpartyCreditCapacity,
                actorRuntimeId: npc.RuntimeId);
        }

        if (npc.Inventory.CanAddItem(item, quantity, unitPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InventoryCapacity,
                actorRuntimeId: npc.RuntimeId);
        }

        bool moneyCommitted = accountBacked == true
            ? TryCommitMoneyTransfer(npc.MoneyAccount, counterparty.MoneyAccount, totalPrice)
            : npc.MoneyAccount.TryDebit(totalPrice);

        if (moneyCommitted == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        if (market.RemoveStockUpTo(item, quantity) != quantity)
        {
            if (accountBacked == true)
            {
                TryCommitMoneyTransfer(counterparty.MoneyAccount, npc.MoneyAccount, totalPrice);
            }
            else
            {
                npc.MoneyAccount.TryCredit(totalPrice);
            }

            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        npc.Inventory.AddItem(item, quantity, unitPrice);

        return EconomyTransactionResult.CreateSuccess(
            transactionType,
            moneyEffect,
            totalPrice,
            item.DefinitionId,
            requestedQuantity,
            quantity,
            unitPrice,
            totalPrice,
            sourceRuntimeId: accountBacked == true ? npc.RuntimeId : null,
            destinationRuntimeId: accountBacked == true ? counterparty.CounterpartyRuntimeId : null,
            actorRuntimeId: npc.RuntimeId,
            itemSourceRuntimeId: counterparty.CounterpartyRuntimeId,
            itemDestinationRuntimeId: npc.RuntimeId);
    }

    private EconomyTransactionResult ExecuteMarketSale(
        NpcRuntime npc,
        MarketRuntime market,
        ItemData item,
        int requestedQuantity,
        MarketLiquidityMode liquidityMode)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.OpenMarketSale;
        bool accountBacked = liquidityMode == MarketLiquidityMode.AccountBacked;
        MoneyEffect moneyEffect = accountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSource;

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

        int availableQuantity = npc.Inventory.GetAmount(item);
        int quantity = Mathf.Min(requestedQuantity, availableQuantity);

        if (quantity <= 0)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientInventory,
                actorRuntimeId: npc.RuntimeId);
        }

        float salePrice = market.GetPriceForSale(item);

        if (IsValidNonNegativeFiniteAmount(salePrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                actorRuntimeId: npc.RuntimeId);
        }

        float unitPrice = Mathf.Max(0.01f, salePrice);

        if (IsValidNonNegativeFiniteAmount(unitPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                actorRuntimeId: npc.RuntimeId);
        }

        if (accountBacked == true)
        {
            int affordableQuantity = GetAffordableQuantity(counterparty.MoneyAccount.Balance, unitPrice);
            quantity = Mathf.Min(quantity, affordableQuantity);

            if (quantity <= 0)
            {
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.InsufficientCounterpartyFunds,
                    actorRuntimeId: npc.RuntimeId);
            }
        }

        float totalPrice = unitPrice * quantity;

        if (IsValidNonNegativeFiniteAmount(totalPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                actorRuntimeId: npc.RuntimeId);
        }

        if (accountBacked == true)
        {
            if (counterparty.MoneyAccount.CanDebit(totalPrice) == false)
            {
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.InsufficientCounterpartyFunds,
                    actorRuntimeId: npc.RuntimeId);
            }

            if (npc.MoneyAccount.CanCredit(totalPrice) == false)
            {
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.CreditRejected,
                    actorRuntimeId: npc.RuntimeId);
            }
        }
        else if (npc.MoneyAccount.CanCredit(totalPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.CreditRejected,
                actorRuntimeId: npc.RuntimeId);
        }

        if (market.CanAddStock(item, quantity) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.MarketCapacity,
                actorRuntimeId: npc.RuntimeId);
        }

        bool moneyCommitted = accountBacked == true
            ? TryCommitMoneyTransfer(counterparty.MoneyAccount, npc.MoneyAccount, totalPrice)
            : npc.MoneyAccount.TryCredit(totalPrice);

        if (moneyCommitted == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        if (npc.Inventory.RemoveItem(item, quantity) == false)
        {
            if (accountBacked == true)
            {
                TryCommitMoneyTransfer(npc.MoneyAccount, counterparty.MoneyAccount, totalPrice);
            }
            else
            {
                npc.MoneyAccount.TryDebit(totalPrice);
            }

            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        market.AddStock(item, quantity);

        return EconomyTransactionResult.CreateSuccess(
            transactionType,
            moneyEffect,
            totalPrice,
            item.DefinitionId,
            requestedQuantity,
            quantity,
            unitPrice,
            totalPrice,
            sourceRuntimeId: accountBacked == true ? counterparty.CounterpartyRuntimeId : null,
            destinationRuntimeId: accountBacked == true ? npc.RuntimeId : null,
            actorRuntimeId: npc.RuntimeId,
            itemSourceRuntimeId: npc.RuntimeId,
            itemDestinationRuntimeId: counterparty.CounterpartyRuntimeId);
    }

    public EconomyTransactionResult TryExecutePopulationConsumption(
        PopulationEconomyRuntime population,
        MarketRuntime market,
        ItemData item,
        int requestedQuantity)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.PopulationConsumption;
        MoneyEffect moneyEffect = MoneyEffect.Transfer;

        if (population == null || market == null || item == null)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                actorRuntimeId: population != null ? population.PopulationEconomicRuntimeId : null);
        }

        if (population.PaymentMode != ConsumptionPaymentMode.AccountBacked
            || population.MoneyAccount == null
            || market.Counterparty == null
            || market.Counterparty.LiquidityMode != MarketLiquidityMode.AccountBacked
            || market.Counterparty.MoneyAccount == null)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                actorRuntimeId: population.PopulationEconomicRuntimeId);
        }

        if (requestedQuantity <= 0)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidQuantity,
                actorRuntimeId: population.PopulationEconomicRuntimeId);
        }

        MarketCounterpartyRuntime settlement = market.Counterparty;
        MarketItemRuntime marketItem = market.GetItem(item);

        if (marketItem == null || marketItem.Amount <= 0)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientStock,
                actorRuntimeId: population.PopulationEconomicRuntimeId);
        }

        float unitPrice = marketItem.CurrentPrice;

        if (IsValidNonNegativeFiniteAmount(unitPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                actorRuntimeId: population.PopulationEconomicRuntimeId);
        }

        unitPrice = Mathf.Max(0.01f, unitPrice);
        int affordableQuantity = GetAffordableQuantity(population.MoneyAccount.Balance, unitPrice);
        int quantity = Mathf.Min(requestedQuantity, marketItem.Amount, affordableQuantity);

        if (quantity <= 0)
        {
            EconomyTransactionFailureReason reason = marketItem.Amount <= 0
                ? EconomyTransactionFailureReason.InsufficientStock
                : EconomyTransactionFailureReason.InsufficientFunds;
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                reason,
                actorRuntimeId: population.PopulationEconomicRuntimeId);
        }

        float totalPrice = unitPrice * quantity;

        if (IsValidNonNegativeFiniteAmount(totalPrice) == false
            || population.MoneyAccount.CanDebit(totalPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientFunds,
                actorRuntimeId: population.PopulationEconomicRuntimeId);
        }

        if (settlement.MoneyAccount.CanCredit(totalPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.CounterpartyCreditCapacity,
                actorRuntimeId: population.PopulationEconomicRuntimeId);
        }

        if (TryCommitMoneyTransfer(population.MoneyAccount, settlement.MoneyAccount, totalPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: population.PopulationEconomicRuntimeId);
        }

        if (market.RemoveStockUpTo(item, quantity) != quantity)
        {
            TryCommitMoneyTransfer(settlement.MoneyAccount, population.MoneyAccount, totalPrice);
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: population.PopulationEconomicRuntimeId);
        }

        return EconomyTransactionResult.CreateSuccess(
            transactionType,
            moneyEffect,
            totalPrice,
            item.DefinitionId,
            requestedQuantity,
            quantity,
            unitPrice,
            totalPrice,
            sourceRuntimeId: population.PopulationEconomicRuntimeId,
            destinationRuntimeId: settlement.CounterpartyRuntimeId,
            actorRuntimeId: population.PopulationEconomicRuntimeId,
            itemSourceRuntimeId: market.StockOwnerRuntimeId,
            itemDestinationRuntimeId: population.PopulationEconomicRuntimeId);
    }

    public bool CanChargeTravel(NpcRuntime npc, float amount)
    {
        return npc != null
            && IsValidNonNegativeFiniteAmount(amount)
            && npc.MoneyAccount.CanDebit(amount);
    }

    public EconomyTransactionResult TryChargeTravelGroup(
        IReadOnlyList<NpcRuntime> members,
        float amountPerMember)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.TravelCharge;
        MoneyEffect moneyEffect = MoneyEffect.ExplicitSink;
        List<string> participantIds = new List<string>();
        HashSet<NpcRuntime> uniqueMembers = new HashSet<NpcRuntime>();

        if (members == null || members.Count == 0 || IsValidNonNegativeFiniteAmount(amountPerMember) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                participantRuntimeIds: participantIds);
        }

        foreach (NpcRuntime member in members)
        {
            if (member == null || uniqueMembers.Add(member) == false)
            {
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.InvalidInput,
                    participantRuntimeIds: participantIds);
            }

            participantIds.Add(member.RuntimeId);

            if (CanChargeTravel(member, amountPerMember) == false)
            {
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.InsufficientFunds,
                    actorRuntimeId: member.RuntimeId,
                    participantRuntimeIds: participantIds);
            }
        }

        float totalAmount = amountPerMember * members.Count;

        if (IsValidNonNegativeFiniteAmount(totalAmount) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                participantRuntimeIds: participantIds);
        }

        int chargedCount = 0;

        foreach (NpcRuntime member in members)
        {
            if (member.MoneyAccount.TryDebit(amountPerMember) == false)
            {
                for (int i = 0; i < chargedCount; i++)
                {
                    members[i].MoneyAccount.TryCredit(amountPerMember);
                }

                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    participantRuntimeIds: participantIds);
            }

            chargedCount++;
        }

        return EconomyTransactionResult.CreateSuccess(
            transactionType,
            moneyEffect,
            totalAmount,
            totalPrice: totalAmount,
            participantRuntimeIds: participantIds);
    }

    public EconomyTransactionResult TryChargeTravel(NpcRuntime npc, float amount)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.TravelCharge;
        MoneyEffect moneyEffect = MoneyEffect.ExplicitSink;

        if (npc == null || IsValidNonNegativeFiniteAmount(amount) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                actorRuntimeId: npc != null ? npc.RuntimeId : null);
        }

        if (npc.MoneyAccount.CanDebit(amount) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientFunds,
                actorRuntimeId: npc.RuntimeId);
        }

        if (npc.MoneyAccount.TryDebit(amount) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        return EconomyTransactionResult.CreateSuccess(
            transactionType,
            moneyEffect,
            amount,
            totalPrice: amount,
            actorRuntimeId: npc.RuntimeId);
    }

    public EconomyTransactionResult TryRestoreTravelCharge(NpcRuntime npc, float amount)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.TravelCharge;
        MoneyEffect moneyEffect = MoneyEffect.ExplicitSource;

        if (npc == null || IsValidNonNegativeFiniteAmount(amount) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                actorRuntimeId: npc != null ? npc.RuntimeId : null);
        }

        if (npc.MoneyAccount.CanCredit(amount) == false || npc.MoneyAccount.TryCredit(amount) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.CreditRejected,
                actorRuntimeId: npc.RuntimeId);
        }

        return EconomyTransactionResult.CreateSuccess(
            transactionType,
            moneyEffect,
            amount,
            totalPrice: amount,
            actorRuntimeId: npc.RuntimeId);
    }

    private EconomyTransactionResult TryTransferMoney(
        MoneyAccountRuntime source,
        MoneyAccountRuntime destination,
        float amount,
        string sourceRuntimeId,
        string destinationRuntimeId)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.MoneyTransfer;
        MoneyEffect moneyEffect = MoneyEffect.Transfer;

        if (source == null || destination == null || IsValidNonNegativeFiniteAmount(amount) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                sourceRuntimeId,
                destinationRuntimeId);
        }

        if (ReferenceEquals(source, destination))
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.SameAccount,
                sourceRuntimeId,
                destinationRuntimeId);
        }

        if (source.CanDebit(amount) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientFunds,
                sourceRuntimeId,
                destinationRuntimeId);
        }

        if (destination.CanCredit(amount) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.CreditRejected,
                sourceRuntimeId,
                destinationRuntimeId);
        }

        if (TryCommitMoneyTransfer(source, destination, amount) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                sourceRuntimeId,
                destinationRuntimeId);
        }

        return EconomyTransactionResult.CreateSuccess(
            transactionType,
            moneyEffect,
            amount,
            totalPrice: amount,
            sourceRuntimeId: sourceRuntimeId,
            destinationRuntimeId: destinationRuntimeId);
    }

    private static bool TryCommitMoneyTransfer(
        MoneyAccountRuntime source,
        MoneyAccountRuntime destination,
        float amount)
    {
        if (source == null
            || destination == null
            || ReferenceEquals(source, destination)
            || IsValidNonNegativeFiniteAmount(amount) == false
            || source.CanDebit(amount) == false
            || destination.CanCredit(amount) == false)
        {
            return false;
        }

        if (source.TryDebit(amount) == false)
        {
            return false;
        }

        if (destination.TryCredit(amount) == true)
        {
            return true;
        }

        source.TryCredit(amount);
        return false;
    }

    private static int GetAffordableQuantity(float balance, float unitPrice)
    {
        if (balance < unitPrice)
        {
            return 0;
        }

        float affordableAmount = balance / unitPrice;

        if (float.IsInfinity(affordableAmount) == true || affordableAmount >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return Mathf.Max(0, Mathf.FloorToInt(affordableAmount));
    }

    private static bool IsValidNonNegativeFiniteAmount(float amount)
    {
        return amount >= 0f
            && float.IsNaN(amount) == false
            && float.IsInfinity(amount) == false;
    }
}
