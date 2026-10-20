using System;
using System.Collections.Generic;
using UnityEngine;

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
    private readonly struct TradeItem
    {
        internal readonly ItemData AuthoringItem;
        internal readonly DailyV1ItemDefinition Definition;
        internal string DefinitionId => Definition != null ? Definition.DefinitionId : AuthoringItem != null ? AuthoringItem.DefinitionId : null;
        internal bool IsValid => (AuthoringItem != null) != (Definition != null);

        internal TradeItem(ItemData item) { AuthoringItem = item; Definition = null; }
        internal TradeItem(DailyV1ItemDefinition definition) { AuthoringItem = null; Definition = definition; }
    }

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
        return ExecuteKeyedMarketSale(proposalId, fingerprint, actorChoiceInputId, requestId,
            actorPersonId, actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            seller, market, new TradeItem(item), requestedQuantity);
    }

    internal KeyedSaleReceipt TryExecuteKeyedMarketSale(
        string proposalId, string fingerprint, string actorChoiceInputId, string requestId,
        string actorPersonId, string actionSemanticId, int actionVersion, string profileId,
        string logicalTick, string marketSiteId, NpcRuntime seller, MarketRuntime market,
        DailyV1ItemDefinition item, int requestedQuantity)
    {
        return ExecuteKeyedMarketSale(proposalId, fingerprint, actorChoiceInputId, requestId,
            actorPersonId, actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            seller, market, new TradeItem(item), requestedQuantity);
    }

    private KeyedSaleReceipt ExecuteKeyedMarketSale(
        string proposalId, string fingerprint, string actorChoiceInputId, string requestId,
        string actorPersonId, string actionSemanticId, int actionVersion, string profileId,
        string logicalTick, string marketSiteId, NpcRuntime seller, MarketRuntime market,
        TradeItem item, int requestedQuantity)
    {
        if (string.IsNullOrWhiteSpace(proposalId) || string.IsNullOrWhiteSpace(fingerprint))
            return KeyedSaleReceipt.Rejected(proposalId, fingerprint, null, EconomyTransactionFailureReason.InvalidInput);

        string canonicalFingerprint = CreateRequestFingerprint(
            proposalId, fingerprint, actorChoiceInputId, requestId, actorPersonId,
            actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            item.IsValid ? item.DefinitionId : null, requestedQuantity);

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
        float observedPrice = market != null && item.IsValid ? GetMarketSalePrice(market, item) : 0f;
        int observedStock = market != null && item.IsValid ? GetMarketAmount(market, item) : 0;
        int observedInventory = seller != null && item.IsValid ? GetInventoryAmount(seller.Inventory, item) : 0;
        float observedSellerBalance = sellerAccount != null ? sellerAccount.Balance : 0f;
        float observedCounterpartyBalance = counterpartyAccount != null ? counterpartyAccount.Balance : 0f;
        EconomyTransactionResult result = ValidateSale(seller, market, item, requestedQuantity, mode);
        int snapshotQuantity = result.Success ? result.Quantity : UnityMathfCompatibility.Min(UnityMathfCompatibility.Max(0, requestedQuantity), observedInventory);
        float normalizedSalePrice = IsValidNonNegativeFiniteAmount(observedPrice) ? UnityMathfCompatibility.Max(0.01f, observedPrice) : observedPrice;
        if (!result.Success && mode == MarketLiquidityMode.AccountBacked && counterpartyAccount != null
            && IsValidNonNegativeFiniteAmount(normalizedSalePrice))
        {
            snapshotQuantity = UnityMathfCompatibility.Min(snapshotQuantity, GetAffordableQuantity(observedCounterpartyBalance, normalizedSalePrice));
        }
        float snapshotTotal = normalizedSalePrice * snapshotQuantity;
        KeyedSaleSnapshot snapshot = new KeyedSaleSnapshot(actorChoiceInputId, requestId, actorPersonId, proposalId,
            actionSemanticId, actionVersion, profileId, logicalTick, marketSiteId,
            seller != null ? seller.RuntimeId : null,
            seller != null && seller.CurrentCity != null ? seller.CurrentCity.RuntimeId : null,
            market != null ? market.StockOwnerRuntimeId : null, item.IsValid ? item.DefinitionId : null,
            mode, observedPrice, observedStock, observedInventory, observedSellerBalance, observedCounterpartyBalance,
            requestedQuantity, snapshotQuantity, seller != null && seller.IsAlive,
            snapshotQuantity > 0 && sellerAccount != null && sellerAccount.CanCredit(snapshotTotal),
            snapshotQuantity > 0 && mode == MarketLiquidityMode.AccountBacked && counterpartyAccount != null && counterpartyAccount.CanDebit(snapshotTotal),
            snapshotQuantity > 0 && market != null && item.IsValid && CanAddMarketStock(market, item, snapshotQuantity),
            invRev, marketRev, sellerMoneyRev, counterpartyMoneyRev);
        if (!result.Success)
        {
            KeyedSaleReceipt rejected = KeyedSaleReceipt.Terminal(proposalId, canonicalFingerprint, fingerprint, snapshot, result);
            StoreReceipt(rejected);
            return rejected;
        }

        int quantity = result.Quantity;
        float total = result.TotalPrice;
        PreparedInventoryState inventoryReplacement = PrepareInventoryReplacement(
            seller.Inventory, item, quantity, 0f, false, 0);
        PreparedMarketState marketReplacement = PrepareMarketReplacement(market, item, quantity, 100);
        float sellerBalanceReplacement = sellerAccount.Balance + total;
        float counterpartyBalanceReplacement = counterpartyAccount != null ? counterpartyAccount.Balance - total : 0f;
        KeyedSaleReceipt committed = KeyedSaleReceipt.Committed(proposalId, canonicalFingerprint, fingerprint, snapshot, result);
        KeyedSaleReceipt unresolved = KeyedSaleReceipt.Unresolved(proposalId, canonicalFingerprint, fingerprint, snapshot, result);
        if (keyedSaleReceipts.Capacity < keyedSaleReceipts.Count + 1) keyedSaleReceipts.Capacity = keyedSaleReceipts.Count + 1;
        bool ready = seller.Inventory.CanInstall(invRev) && market.CanInstall(marketRev)
            && MoneyAccountOwnerBridge.CanInstall(sellerAccount, sellerMoneyRev, sellerBalanceReplacement)
            && (counterpartyAccount == null || MoneyAccountOwnerBridge.CanInstall(counterpartyAccount, counterpartyMoneyRev, counterpartyBalanceReplacement));
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
            MoneyAccountOwnerBridge.InstallPrepared(sellerAccount, sellerMoneyRev, sellerBalanceReplacement);
            if (counterpartyAccount != null) MoneyAccountOwnerBridge.InstallPrepared(counterpartyAccount, counterpartyMoneyRev, counterpartyBalanceReplacement);
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

    private static EconomyTransactionResult ValidateSale(NpcRuntime npc, MarketRuntime market, TradeItem item, int requested, MarketLiquidityMode mode)
    {
        // Reuse the established sale preconditions and calculations without applying its mutations.
        if (npc == null || !npc.IsAlive || market == null || !item.IsValid || !CanUseTradeItem(npc, market, item))
            return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, mode == MarketLiquidityMode.AccountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.InvalidInput);
        if (requested <= 0) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.InvalidQuantity);
        int quantity = UnityMathfCompatibility.Min(requested, GetInventoryAmount(npc.Inventory, item));
        float price = GetMarketSalePrice(market, item);
        if (quantity <= 0) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.InsufficientInventory);
        if (float.IsNaN(price) || float.IsInfinity(price) || price < 0f) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.InvalidPrice);
        price = UnityMathfCompatibility.Max(.01f, price);
        MarketCounterpartyRuntime cp = market.Counterparty;
        if (mode == MarketLiquidityMode.AccountBacked)
        {
            if (cp.MoneyAccount == null) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.Transfer, EconomyTransactionFailureReason.InvalidInput);
            quantity = UnityMathfCompatibility.Min(quantity, GetAffordableQuantity(cp.MoneyAccount.Balance, price));
            if (quantity <= 0) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.Transfer, EconomyTransactionFailureReason.InsufficientCounterpartyFunds);
        }
        float total = quantity * price;
        if (float.IsNaN(total) || float.IsInfinity(total)) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.InvalidPrice);
        if (mode == MarketLiquidityMode.AccountBacked && !cp.MoneyAccount.CanDebit(total)) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.Transfer, EconomyTransactionFailureReason.InsufficientCounterpartyFunds);
        if (!npc.MoneyAccount.CanCredit(total)) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, mode == MarketLiquidityMode.AccountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.CreditRejected);
        if (!CanAddMarketStock(market, item, quantity)) return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale, MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.MarketCapacity);
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
        if (p12CensusRuntime != null)
        {
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.MoneyTransfer,
                MoneyEffect.Transfer,
                EconomyTransactionFailureReason.TransactionCommitFailed);
        }

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

        if (p12CensusRuntime != null)
        {
            MoneyAccountRuntime sourceAccount = source.MoneyAccount;
            MoneyAccountRuntime destinationAccount = destination.MoneyAccount;
            EconomyTransactionResult preflightFailure = ValidateMoneyTransfer(
                sourceAccount,
                destinationAccount,
                amount,
                source.RuntimeId,
                destination.RuntimeId);
            if (preflightFailure != null) return preflightFailure;

            if (!p12CensusRuntime.HasNpcTradeCensusAdapter
                || !p12CensusRuntime.TryBeginNpcMoneyTransferCensusOperation(
                    source,
                    destination,
                    out SimulationOperationScope operationScope,
                    out _,
                    out _))
            {
                return EconomyTransactionResult.CreateFailure(
                    EconomyTransactionType.MoneyTransfer,
                    MoneyEffect.Transfer,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    source.RuntimeId,
                    destination.RuntimeId);
            }

            try
            {
                return TryTransferMoney(
                    sourceAccount,
                    destinationAccount,
                    amount,
                    source.RuntimeId,
                    destination.RuntimeId);
            }
            finally
            {
                operationScope.Dispose();
            }
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
        return ExecuteNpcTrade(buyer, seller, new TradeItem(item), quantity, unitPrice);
    }

    internal EconomyTransactionResult TryExecuteNpcTrade(
        NpcRuntime buyer,
        NpcRuntime seller,
        DailyV1ItemDefinition item,
        int quantity,
        float unitPrice)
    {
        return ExecuteNpcTrade(buyer, seller, new TradeItem(item), quantity, unitPrice);
    }

    private EconomyTransactionResult ExecuteNpcTrade(
        NpcRuntime buyer,
        NpcRuntime seller,
        TradeItem item,
        int quantity,
        float unitPrice)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.NpcToNpcTrade;
        MoneyEffect moneyEffect = MoneyEffect.Transfer;

        if (buyer == null || seller == null || !item.IsValid || buyer.IsAlive == false || seller.IsAlive == false
            || !CanUseTradeItem(buyer, seller, item))
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                buyerRuntimeId: buyer != null ? buyer.RuntimeId : null,
                sellerRuntimeId: seller != null ? seller.RuntimeId : null);
        }

        DailyV1SelectedRuntimeOwnerGraph buyerSelectedGraph = buyer.SelectedRuntimeOwnerGraph;
        if (item.Definition != null
            && buyer.SelectedRuntimeOwners != null
            && seller.SelectedRuntimeOwners != null
            && buyerSelectedGraph != null
            && ReferenceEquals(buyerSelectedGraph, seller.SelectedRuntimeOwnerGraph)
            && buyerSelectedGraph.ContainsExactNpcOwner(buyer.SelectedRuntimeOwners)
            && buyerSelectedGraph.ContainsExactNpcOwner(seller.SelectedRuntimeOwners))
            return ExecuteSelectedNpcTrade(buyer, seller, item.Definition, quantity, unitPrice);

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

        if (GetInventoryAmount(seller.Inventory, item) < quantity)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientInventory,
                buyerRuntimeId: buyer.RuntimeId,
                sellerRuntimeId: seller.RuntimeId);
        }

        if (CanAddInventoryItem(buyer.Inventory, item, quantity, unitPrice) == false)
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
                NotifyNpcTradeOwnerMutation(censusTracking, buyerAccountSectionId, MoneyAccountOwnerBridge.HasP12MutationBoundary(buyer.MoneyAccount));
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
                NotifyNpcTradeOwnerMutation(censusTracking, sellerAccountSectionId, MoneyAccountOwnerBridge.HasP12MutationBoundary(seller.MoneyAccount));
            if (sellerCreditSucceeded == false)
            {
                long buyerCompensationRevision = buyer.MoneyAccount.Revision;
                buyer.MoneyAccount.TryCredit(totalPrice);
                if (buyer.MoneyAccount.Revision != buyerCompensationRevision)
                    NotifyNpcTradeOwnerMutation(censusTracking, buyerAccountSectionId, MoneyAccountOwnerBridge.HasP12MutationBoundary(buyer.MoneyAccount));
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    buyerRuntimeId: buyer.RuntimeId,
                    sellerRuntimeId: seller.RuntimeId);
            }

            long sellerInventoryRevision = seller.Inventory.Revision;
            bool sellerInventoryRemoved = RemoveInventoryItem(seller.Inventory, item, quantity);
            if (seller.Inventory.Revision != sellerInventoryRevision)
                NotifyNpcTradeOwnerMutation(censusTracking, sellerInventorySectionId, seller.Inventory.HasP12MutationBoundary);
            if (sellerInventoryRemoved == false)
            {
                long sellerCompensationRevision = seller.MoneyAccount.Revision;
                seller.MoneyAccount.TryDebit(totalPrice);
                if (seller.MoneyAccount.Revision != sellerCompensationRevision)
                    NotifyNpcTradeOwnerMutation(censusTracking, sellerAccountSectionId, MoneyAccountOwnerBridge.HasP12MutationBoundary(seller.MoneyAccount));

                long buyerCompensationRevision = buyer.MoneyAccount.Revision;
                buyer.MoneyAccount.TryCredit(totalPrice);
                if (buyer.MoneyAccount.Revision != buyerCompensationRevision)
                    NotifyNpcTradeOwnerMutation(censusTracking, buyerAccountSectionId, MoneyAccountOwnerBridge.HasP12MutationBoundary(buyer.MoneyAccount));
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    buyerRuntimeId: buyer.RuntimeId,
                    sellerRuntimeId: seller.RuntimeId);
            }

            long buyerInventoryRevision = buyer.Inventory.Revision;
            bool buyerInventoryAdded = TryAddInventoryItem(buyer.Inventory, item, quantity, unitPrice);
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

    private EconomyTransactionResult ExecuteSelectedNpcTrade(
        NpcRuntime buyer, NpcRuntime seller, DailyV1ItemDefinition item,
        int quantity, float unitPrice)
    {
        DailyV1SelectedRuntimeOwnerGraph graph = buyer.SelectedRuntimeOwnerGraph;
        bool tracking = p12CensusRuntime != null && p12CensusRuntime.HasNpcTradeCensusAdapter;
        DailyV1SelectedNpcTradeBoundary boundary = tracking
            ? new DailyV1SelectedNpcTradeBoundary(this, buyer, seller)
            : null;
        DailyV1SelectedNpcTradeCommitResult result = DailyV1SelectedNpcTradeExecution.TryExecute(
            graph, buyer.SelectedRuntimeOwners, seller.SelectedRuntimeOwners,
            item, quantity, unitPrice, boundary);
        if (!result.Succeeded)
            return EconomyTransactionResult.CreateFailure(EconomyTransactionType.NpcToNpcTrade,
                MoneyEffect.Transfer, result.Failure switch
                {
                    DailyV1SelectedNpcTradeFailure.SameAccount => EconomyTransactionFailureReason.SameAccount,
                    DailyV1SelectedNpcTradeFailure.InvalidQuantity => EconomyTransactionFailureReason.InvalidQuantity,
                    DailyV1SelectedNpcTradeFailure.InvalidPrice => EconomyTransactionFailureReason.InvalidPrice,
                    DailyV1SelectedNpcTradeFailure.InsufficientInventory => EconomyTransactionFailureReason.InsufficientInventory,
                    DailyV1SelectedNpcTradeFailure.InventoryCapacity => EconomyTransactionFailureReason.InventoryCapacity,
                    DailyV1SelectedNpcTradeFailure.InsufficientFunds => EconomyTransactionFailureReason.InsufficientFunds,
                    DailyV1SelectedNpcTradeFailure.CreditRejected => EconomyTransactionFailureReason.CreditRejected,
                    _ => EconomyTransactionFailureReason.TransactionCommitFailed
                }, buyerRuntimeId: buyer.RuntimeId, sellerRuntimeId: seller.RuntimeId);

        float total = unitPrice * quantity;
        return EconomyTransactionResult.CreateSuccess(EconomyTransactionType.NpcToNpcTrade,
            MoneyEffect.Transfer, total, item.DefinitionId, quantity, quantity,
            unitPrice, total, sourceRuntimeId: buyer.RuntimeId,
            destinationRuntimeId: seller.RuntimeId, buyerRuntimeId: buyer.RuntimeId,
            sellerRuntimeId: seller.RuntimeId, itemSourceRuntimeId: seller.RuntimeId,
            itemDestinationRuntimeId: buyer.RuntimeId);
    }

    private sealed class DailyV1SelectedNpcTradeBoundary : IDailyV1SelectedNpcTradeBoundary
    {
        private readonly EconomyTransactionService service;
        private readonly NpcRuntime buyer;
        private readonly NpcRuntime seller;
        private SimulationOperationScope scope;
        private string buyerAccount;
        private string sellerAccount;
        private string buyerInventory;
        private string sellerInventory;

        internal DailyV1SelectedNpcTradeBoundary(
            EconomyTransactionService service, NpcRuntime buyer, NpcRuntime seller)
        { this.service = service; this.buyer = buyer; this.seller = seller; }

        public bool TryBegin() => service.p12CensusRuntime.TryBeginNpcTradeCensusOperation(
            buyer, seller, out scope, out buyerAccount, out sellerAccount,
            out buyerInventory, out sellerInventory);

        public void NotifyMutation(DailyV1SelectedNpcTradeMutation mutation)
        {
            switch (mutation)
            {
                case DailyV1SelectedNpcTradeMutation.BuyerAccount:
                    service.NotifyNpcTradeOwnerMutation(true, buyerAccount,
                        MoneyAccountOwnerBridge.HasP12MutationBoundary(buyer.MoneyAccount));
                    break;
                case DailyV1SelectedNpcTradeMutation.SellerAccount:
                    service.NotifyNpcTradeOwnerMutation(true, sellerAccount,
                        MoneyAccountOwnerBridge.HasP12MutationBoundary(seller.MoneyAccount));
                    break;
                case DailyV1SelectedNpcTradeMutation.BuyerInventory:
                    service.NotifyNpcTradeOwnerMutation(true, buyerInventory,
                        buyer.Inventory.HasP12MutationBoundary);
                    break;
                case DailyV1SelectedNpcTradeMutation.SellerInventory:
                    service.NotifyNpcTradeOwnerMutation(true, sellerInventory,
                        seller.Inventory.HasP12MutationBoundary);
                    break;
            }
        }

        public void Dispose() => scope?.Dispose();
    }

    public EconomyTransactionResult TryExecuteOpenMarketPurchase(
        NpcRuntime npc,
        MarketRuntime market,
        ItemData item,
        int requestedQuantity)
    {
        return ExecuteMarketPurchase(npc, market, new TradeItem(item), requestedQuantity, MarketLiquidityMode.Open);
    }

    public EconomyTransactionResult TryExecuteOpenMarketSale(
        NpcRuntime npc,
        MarketRuntime market,
        ItemData item,
        int requestedQuantity)
    {
        return ExecuteMarketSale(npc, market, new TradeItem(item), requestedQuantity, MarketLiquidityMode.Open);
    }

    internal bool TryExecuteSelectedPlannedNpcSale(
        NpcRuntime seller,
        NpcRuntime buyer,
        out DailyV1SelectedActionExecutionStage.PlannedNpcSaleResult result)
    {
        result = null;
        DailyV1SelectedRuntimeOwnerGraph graph = seller?.SelectedRuntimeOwnerGraph;
        if (seller == null || buyer == null || seller.CurrentCity == null
            || graph == null || !ReferenceEquals(graph, buyer.SelectedRuntimeOwnerGraph))
            return false;

        bool trackTrade = p12CensusRuntime != null && p12CensusRuntime.HasNpcTradeCensusAdapter;
        IDailyV1SelectedNpcTradeBoundary tradeBoundary = trackTrade
            ? new DailyV1SelectedNpcTradeBoundary(this, buyer, seller)
            : null;
        bool trackMarket = p12CensusRuntime != null && p12CensusRuntime.HasP12MarketOperationAdapter;
        IDailyV1SelectedOpenMarketSaleBoundary marketBoundary = trackMarket
            ? new DailyV1SelectedOpenMarketSaleBoundary(this, seller, seller.CurrentCity.Market)
            : null;

        bool executed = DailyV1SelectedActionExecutionStage.TryExecutePlannedNpcSaleSlot(
            graph, seller.SelectedRuntimeOwners, buyer.SelectedRuntimeOwners,
            tradeBoundary, marketBoundary,
            seller.MerchantTradePlan.CanAdmitSelectedPlanOwnerMutation,
            seller.MerchantTradePlan.PublishSelectedPlanOwnerMutation,
            out result);
        FaultSelectedPlannedNpcSaleIfPartial(result);
        return executed;
    }

    /// <summary>Rolls and executes an exact selected planned NPC sale through
    /// Core while retaining this service's existing P12 transaction adapters.</summary>
    internal bool TryExecuteSelectedPlannedNpcSaleActionSlot(
        NpcRuntime seller,
        NpcRuntime buyer,
        DailyV1ActionDefinition expectedAction,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        out bool actionRollSucceeded,
        out DailyV1SelectedActionExecutionStage.PlannedNpcSaleResult result)
    {
        actionRollSucceeded = false;
        result = null;
        DailyV1SelectedRuntimeOwnerGraph graph = seller?.SelectedRuntimeOwnerGraph;
        DailyV1NpcRuntimeOwners actor = seller?.SelectedRuntimeOwners;
        DailyV1NpcRuntimeOwners target = buyer?.SelectedRuntimeOwners;
        DailyV1CityRuntimeOwners cityOwner = null;
        if (graph != null && !string.IsNullOrWhiteSpace(actor?.Travel?.CurrentCityRuntimeId))
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
                if (string.Equals(candidate.RuntimeId, actor.Travel.CurrentCityRuntimeId,
                    StringComparison.Ordinal))
                { cityOwner = candidate; break; }

        bool trackTrade = p12CensusRuntime != null
            && p12CensusRuntime.HasNpcTradeCensusAdapter;
        bool trackMarket = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;
        if (seller == null || buyer == null || expectedAction == null || graph == null
            || seller.MerchantTradePlan == null
            || !ReferenceEquals(graph, buyer.SelectedRuntimeOwnerGraph)
            || actor == null || target == null || cityOwner == null
            || !graph.ContainsExactNpcOwner(actor) || !graph.ContainsExactNpcOwner(target)
            || !graph.ContainsExactCityOwner(cityOwner)
            || (trackTrade
                && !ReferenceEquals(seller.MerchantTradePlan?.SelectedOwner, actor.MerchantTradePlan))
            || !ReferenceEquals(seller.CurrentCity?.SelectedRuntimeOwners, cityOwner)
            || !ReferenceEquals(buyer.CurrentCity, seller.CurrentCity)
            || !ReferenceEquals(seller.CurrentCity?.Market?.SelectedDailyV1Owner, cityOwner.Market))
            return false;

        IDailyV1SelectedNpcTradeBoundary tradeBoundary = trackTrade
            ? new DailyV1SelectedNpcTradeBoundary(this, buyer, seller)
            : null;
        IDailyV1SelectedOpenMarketSaleBoundary marketBoundary = trackMarket
            ? new DailyV1SelectedOpenMarketSaleBoundary(this, seller, seller.CurrentCity.Market)
            : null;
        if (!DailyV1SelectedActionExecutionStage.TryExecuteSelectedPlannedNpcSaleActionSlot(
            graph, actor, target, expectedAction, randomSource, currentAbsoluteDay,
            tradeBoundary, marketBoundary,
            seller.MerchantTradePlan.CanAdmitSelectedPlanOwnerMutation,
            seller.MerchantTradePlan.PublishSelectedPlanOwnerMutation,
            out DailyV1SelectedActionExecutionStage.SelectedPlannedNpcSaleActionResult execution))
            return false;

        actionRollSucceeded = execution.ActionRollSucceeded;
        result = execution.Sale;
        FaultSelectedPlannedNpcSaleIfPartial(result);
        return true;
    }

    internal void FaultSelectedPlannedNpcSaleIfPartial(
        DailyV1SelectedActionExecutionStage.PlannedNpcSaleResult result)
    {
        if (result?.NpcTrade?.PartialCommit == true)
            p12CensusRuntime?.FaultRuntimeAdmission();
    }

    /// <summary>Creates the existing Unity P12 market scopes for a Core-owned
    /// selected-action dispatcher. These are host admission adapters only; the
    /// returned Core operations still own preparation and commit semantics.</summary>
    internal bool TryCreateSelectedActionMarketBoundaries(
        NpcRuntime npc,
        out IDailyV1SelectedOpenMarketSaleBoundary saleBoundary,
        out IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary)
    {
        saleBoundary = null;
        purchaseBoundary = null;
        DailyV1SelectedRuntimeOwnerGraph graph = npc?.SelectedRuntimeOwnerGraph;
        DailyV1NpcRuntimeOwners actor = npc?.SelectedRuntimeOwners;
        DailyV1CityRuntimeOwners cityOwner = null;
        if (graph != null && !string.IsNullOrWhiteSpace(actor?.Travel?.CurrentCityRuntimeId))
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
                if (string.Equals(candidate.RuntimeId, actor.Travel.CurrentCityRuntimeId,
                    StringComparison.Ordinal))
                { cityOwner = candidate; break; }

        if (npc == null || graph == null || actor == null || cityOwner == null
            || !graph.ContainsExactNpcOwner(actor)
            || !graph.ContainsExactCityOwner(cityOwner)
            || !ReferenceEquals(npc.CurrentCity?.SelectedRuntimeOwners, cityOwner)
            || !ReferenceEquals(npc.CurrentCity?.Market?.SelectedDailyV1Owner, cityOwner.Market))
            return false;

        bool trackMarket = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;
        if (trackMarket)
        {
            saleBoundary = new DailyV1SelectedOpenMarketSaleBoundary(
                this, npc, npc.CurrentCity.Market);
            purchaseBoundary = new DailyV1SelectedOpenMarketPurchaseBoundary(
                this, npc, npc.CurrentCity.Market);
        }
        return true;
    }

    /// <summary>Creates the existing Unity P12 NPC-trade scope when the exact
    /// selected Core participants are represented by the host roster.</summary>
    internal bool TryCreateSelectedActionNpcTradeBoundary(
        NpcRuntime seller,
        NpcRuntime buyer,
        out IDailyV1SelectedNpcTradeBoundary boundary)
    {
        boundary = null;
        DailyV1SelectedRuntimeOwnerGraph graph = seller?.SelectedRuntimeOwnerGraph;
        if (seller == null || buyer == null || graph == null
            || !ReferenceEquals(graph, buyer.SelectedRuntimeOwnerGraph)
            || !graph.ContainsExactNpcOwner(seller.SelectedRuntimeOwners)
            || !graph.ContainsExactNpcOwner(buyer.SelectedRuntimeOwners)
            || !ReferenceEquals(seller.CurrentCity, buyer.CurrentCity)
            || !ReferenceEquals(seller.CurrentCity?.SelectedRuntimeOwners,
                seller.SelectedRuntimeOwners?.Travel == null
                    ? null
                    : FindCityOwner(graph, seller.SelectedRuntimeOwners.Travel.CurrentCityRuntimeId)))
            return false;

        bool trackTrade = p12CensusRuntime != null
            && p12CensusRuntime.HasNpcTradeCensusAdapter;
        if (trackTrade)
            boundary = new DailyV1SelectedNpcTradeBoundary(this, buyer, seller);
        return true;
    }

    private static DailyV1CityRuntimeOwners FindCityOwner(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string runtimeId)
    {
        if (graph == null || string.IsNullOrWhiteSpace(runtimeId)) return null;
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            if (city != null && string.Equals(city.RuntimeId, runtimeId, StringComparison.Ordinal))
                return graph.ContainsExactCityOwner(city) ? city : null;
        return null;
    }

    internal bool TryExecuteSelectedPlannedMarketSale(
        NpcRuntime seller,
        out DailyV1SelectedActionExecutionStage.PlannedMarketSaleResult result)
    {
        result = null;
        DailyV1SelectedRuntimeOwnerGraph graph = seller?.SelectedRuntimeOwnerGraph;
        DailyV1NpcRuntimeOwners actor = seller?.SelectedRuntimeOwners;
        DailyV1CityRuntimeOwners cityOwner = null;
        if (graph != null && !string.IsNullOrWhiteSpace(actor?.Travel?.CurrentCityRuntimeId))
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
                if (string.Equals(candidate.RuntimeId, actor.Travel.CurrentCityRuntimeId,
                    StringComparison.Ordinal))
                { cityOwner = candidate; break; }

        bool trackMarket = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;

        if (seller == null || graph == null || actor == null || cityOwner == null
            || !graph.ContainsExactNpcOwner(actor)
            || !graph.ContainsExactCityOwner(cityOwner)
            || (trackMarket
                && !ReferenceEquals(seller.MerchantTradePlan.SelectedOwner, actor.MerchantTradePlan))
            || !ReferenceEquals(seller.CurrentCity?.SelectedRuntimeOwners, cityOwner)
            || !ReferenceEquals(seller.CurrentCity?.Market?.SelectedDailyV1Owner, cityOwner.Market))
            return false;

        IDailyV1SelectedOpenMarketSaleBoundary marketBoundary = trackMarket
            ? new DailyV1SelectedOpenMarketSaleBoundary(this, seller, seller.CurrentCity.Market)
            : null;
        bool executed = DailyV1SelectedActionExecutionStage.TryExecutePlannedMarketSaleSlot(
            graph, actor, marketBoundary,
            seller.MerchantTradePlan.CanAdmitSelectedPlanOwnerMutation,
            seller.MerchantTradePlan.PublishSelectedPlanOwnerMutation,
            out result);
        FaultSelectedPlannedMarketSaleIfPartial(result);
        return executed;
    }

    internal bool TryExecuteSelectedPlannedMarketSaleActionSlot(
        NpcRuntime seller,
        DailyV1ActionDefinition expectedAction,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        out bool actionRollSucceeded,
        out DailyV1SelectedActionExecutionStage.PlannedMarketSaleResult result)
    {
        actionRollSucceeded = false;
        result = null;
        DailyV1SelectedRuntimeOwnerGraph graph = seller?.SelectedRuntimeOwnerGraph;
        DailyV1NpcRuntimeOwners actor = seller?.SelectedRuntimeOwners;
        DailyV1CityRuntimeOwners cityOwner = null;
        if (graph != null && !string.IsNullOrWhiteSpace(actor?.Travel?.CurrentCityRuntimeId))
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
                if (string.Equals(candidate.RuntimeId, actor.Travel.CurrentCityRuntimeId,
                    StringComparison.Ordinal))
                { cityOwner = candidate; break; }

        bool trackMarket = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;
        if (seller == null || expectedAction == null || graph == null || actor == null
            || cityOwner == null || seller.CurrentCity?.Market == null
            || !graph.ContainsExactNpcOwner(actor)
            || !graph.ContainsExactCityOwner(cityOwner)
            || (trackMarket
                && !ReferenceEquals(seller.MerchantTradePlan?.SelectedOwner, actor.MerchantTradePlan))
            || !ReferenceEquals(seller.CurrentCity.SelectedRuntimeOwners, cityOwner)
            || !ReferenceEquals(seller.CurrentCity.Market.SelectedDailyV1Owner, cityOwner.Market))
            return false;

        IDailyV1SelectedOpenMarketSaleBoundary marketBoundary = trackMarket
            ? new DailyV1SelectedOpenMarketSaleBoundary(this, seller, seller.CurrentCity.Market)
            : null;
        if (!DailyV1SelectedActionExecutionStage.TryExecuteSelectedPlannedMarketSaleActionSlot(
            graph, actor, expectedAction, randomSource, currentAbsoluteDay, marketBoundary,
            seller.MerchantTradePlan.CanAdmitSelectedPlanOwnerMutation,
            seller.MerchantTradePlan.PublishSelectedPlanOwnerMutation,
            out DailyV1SelectedActionExecutionStage.SelectedPlannedMarketSaleActionResult execution))
            return false;

        actionRollSucceeded = execution.ActionRollSucceeded;
        result = execution.Sale;
        FaultSelectedPlannedMarketSaleIfPartial(result);
        return true;
    }

    internal void FaultSelectedPlannedMarketSaleIfPartial(
        DailyV1SelectedActionExecutionStage.PlannedMarketSaleResult result)
    {
        if (result?.PartialCommit == true)
            p12CensusRuntime?.FaultRuntimeAdmission();
    }

    /// <summary>Executes a selected local market action from its exact Core payload.</summary>
    internal EconomyTransactionResult TryExecuteSelectedOpenMarketAction(
        NpcRuntime npc,
        DailyV1NpcActionPayloadOwner payload)
    {
        DailyV1SelectedRuntimeOwnerGraph graph = npc?.SelectedRuntimeOwnerGraph;
        if (npc == null || graph == null || payload == null || npc.CurrentCity == null
            || !ReferenceEquals(npc.SelectedRuntimeOwners?.ActionSlot?.CurrentPayload, payload))
        {
            return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale,
                MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc?.RuntimeId);
        }

        bool p12Tracking = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;
        DailyV1SelectedOpenMarketSaleBoundary saleBoundary = p12Tracking
            ? new DailyV1SelectedOpenMarketSaleBoundary(this, npc, npc.CurrentCity.Market)
            : null;
        DailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary = p12Tracking
            ? new DailyV1SelectedOpenMarketPurchaseBoundary(this, npc, npc.CurrentCity.Market)
            : null;
        bool succeeded = DailyV1SelectedActionExecutionStage.TryExecuteOpenMarketSlot(
            graph, npc.SelectedRuntimeOwners, saleBoundary, purchaseBoundary,
            out DailyV1SelectedOpenMarketSaleExecution sale,
            out DailyV1SelectedOpenMarketPurchaseExecution purchase);

        if (sale != null)
        {
            if (!succeeded)
                return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale,
                    MoneyEffect.ExplicitSource, MapSelectedSaleFailure(
                        sale.Commit?.Failure ?? sale.PreparationFailure), actorRuntimeId: npc.RuntimeId);
            DailyV1SelectedOpenMarketSalePlan plan = sale.Plan;
            return EconomyTransactionResult.CreateSuccess(EconomyTransactionType.OpenMarketSale,
                MoneyEffect.ExplicitSource, plan.TotalPrice, plan.ItemDefinitionId,
                plan.RequestedQuantity, plan.Quantity, plan.UnitPrice, plan.TotalPrice,
                actorRuntimeId: npc.RuntimeId, itemSourceRuntimeId: npc.RuntimeId,
                itemDestinationRuntimeId: plan.MarketRuntimeId);
        }
        if (purchase != null)
        {
            MarketLiquidityMode mode = npc.CurrentCity.Market.Counterparty.LiquidityMode;
            MoneyEffect effect = mode == MarketLiquidityMode.AccountBacked
                ? MoneyEffect.Transfer : MoneyEffect.ExplicitSink;
            if (!succeeded)
                return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketPurchase,
                    effect, MapSelectedPurchaseFailure(purchase.Commit?.Failure
                        ?? purchase.PreparationFailure), actorRuntimeId: npc.RuntimeId);
            DailyV1SelectedOpenMarketPurchasePlan plan = purchase.Plan;
            return EconomyTransactionResult.CreateSuccess(EconomyTransactionType.OpenMarketPurchase,
                effect, plan.TotalPrice, plan.ItemDefinitionId, payload.Amount, plan.Quantity,
                plan.UnitPrice, plan.TotalPrice, sourceRuntimeId: plan.MarketRuntimeId,
                destinationRuntimeId: npc.RuntimeId, buyerRuntimeId: npc.RuntimeId,
                sellerRuntimeId: plan.MarketRuntimeId, actorRuntimeId: npc.RuntimeId,
                itemSourceRuntimeId: plan.MarketRuntimeId, itemDestinationRuntimeId: npc.RuntimeId);
        }
        return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale,
            MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.TransactionCommitFailed,
            actorRuntimeId: npc.RuntimeId);
    }

    /// <summary>Rolls and executes the exact selected local market slot through
    /// Core while retaining this service's existing P12 transaction adapters.</summary>
    internal bool TryExecuteSelectedOpenMarketActionSlot(
        NpcRuntime npc,
        DailyV1ActionDefinition expectedAction,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        out bool actionRollSucceeded,
        out EconomyTransactionResult transaction)
    {
        actionRollSucceeded = false;
        transaction = null;
        DailyV1SelectedRuntimeOwnerGraph graph = npc?.SelectedRuntimeOwnerGraph;
        DailyV1NpcRuntimeOwners actor = npc?.SelectedRuntimeOwners;
        DailyV1CityRuntimeOwners cityOwner = null;
        if (graph != null && !string.IsNullOrWhiteSpace(actor?.Travel?.CurrentCityRuntimeId))
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
                if (string.Equals(candidate.RuntimeId, actor.Travel.CurrentCityRuntimeId,
                    StringComparison.Ordinal))
                { cityOwner = candidate; break; }

        if (npc == null || expectedAction == null || graph == null || actor == null
            || npc.CurrentCity?.Market == null || cityOwner == null
            || !graph.ContainsExactNpcOwner(actor)
            || !graph.ContainsExactCityOwner(cityOwner)
            || !ReferenceEquals(npc.CurrentCity.SelectedRuntimeOwners, cityOwner)
            || !ReferenceEquals(npc.CurrentCity.Market.SelectedDailyV1Owner, cityOwner.Market))
            return false;

        bool trackMarket = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;
        DailyV1SelectedOpenMarketSaleBoundary saleBoundary = trackMarket
            ? new DailyV1SelectedOpenMarketSaleBoundary(this, npc, npc.CurrentCity.Market)
            : null;
        DailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary = trackMarket
            ? new DailyV1SelectedOpenMarketPurchaseBoundary(this, npc, npc.CurrentCity.Market)
            : null;

        if (!DailyV1SelectedActionExecutionStage.TryExecuteSelectedOpenMarketActionSlot(
            graph, actor, expectedAction, randomSource, currentAbsoluteDay,
            saleBoundary, purchaseBoundary,
            out DailyV1SelectedActionExecutionStage.SelectedOpenMarketActionResult execution))
            return false;

        actionRollSucceeded = execution.ActionRollSucceeded;
        if (!actionRollSucceeded)
            return true;

        DailyV1NpcActionPayloadOwner payload = actor.ActionSlot.CurrentPayload;
        transaction = CreateSelectedOpenMarketActionResult(npc, payload,
            execution.Succeeded, execution.Sale, execution.Purchase);
        return true;
    }

    private static EconomyTransactionResult CreateSelectedOpenMarketActionResult(
        NpcRuntime npc,
        DailyV1NpcActionPayloadOwner payload,
        bool succeeded,
        DailyV1SelectedOpenMarketSaleExecution sale,
        DailyV1SelectedOpenMarketPurchaseExecution purchase)
    {
        if (sale != null)
        {
            if (!succeeded)
                return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale,
                    MoneyEffect.ExplicitSource, MapSelectedSaleFailure(
                        sale.Commit?.Failure ?? sale.PreparationFailure), actorRuntimeId: npc.RuntimeId);
            DailyV1SelectedOpenMarketSalePlan plan = sale.Plan;
            return EconomyTransactionResult.CreateSuccess(EconomyTransactionType.OpenMarketSale,
                MoneyEffect.ExplicitSource, plan.TotalPrice, plan.ItemDefinitionId,
                plan.RequestedQuantity, plan.Quantity, plan.UnitPrice, plan.TotalPrice,
                actorRuntimeId: npc.RuntimeId, itemSourceRuntimeId: npc.RuntimeId,
                itemDestinationRuntimeId: plan.MarketRuntimeId);
        }
        if (purchase != null)
        {
            MarketLiquidityMode mode = npc.CurrentCity.Market.Counterparty.LiquidityMode;
            MoneyEffect effect = mode == MarketLiquidityMode.AccountBacked
                ? MoneyEffect.Transfer : MoneyEffect.ExplicitSink;
            if (!succeeded)
                return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketPurchase,
                    effect, MapSelectedPurchaseFailure(purchase.Commit?.Failure
                        ?? purchase.PreparationFailure), actorRuntimeId: npc.RuntimeId);
            DailyV1SelectedOpenMarketPurchasePlan plan = purchase.Plan;
            return EconomyTransactionResult.CreateSuccess(EconomyTransactionType.OpenMarketPurchase,
                effect, plan.TotalPrice, plan.ItemDefinitionId, payload.Amount, plan.Quantity,
                plan.UnitPrice, plan.TotalPrice, sourceRuntimeId: plan.MarketRuntimeId,
                destinationRuntimeId: npc.RuntimeId, buyerRuntimeId: npc.RuntimeId,
                sellerRuntimeId: plan.MarketRuntimeId, actorRuntimeId: npc.RuntimeId,
                itemSourceRuntimeId: plan.MarketRuntimeId, itemDestinationRuntimeId: npc.RuntimeId);
        }
        return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketSale,
            MoneyEffect.ExplicitSource, EconomyTransactionFailureReason.TransactionCommitFailed,
            actorRuntimeId: npc.RuntimeId);
    }

    /// <summary>Executes one selected cross-city BuyGoods purchase and both resulting owner plans in Core.</summary>
    internal EconomyTransactionResult TryExecuteSelectedCrossCityBuy(
        NpcRuntime npc,
        DailyV1NpcActionPayloadOwner payload,
        CityRuntime targetCityProjection)
    {
        if (!TryResolveSelectedCrossCityBuy(npc, payload, targetCityProjection,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor,
            out SpatialLocationRuntime targetLocationProjection))
            return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketPurchase,
                MoneyEffect.ExplicitSink, EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc?.RuntimeId);

        DailyV1SelectedCrossCityBuyResult execution = ExecuteSelectedCrossCityBuyCore(
            npc, actor, graph, payload);
        return CompleteSelectedCrossCityBuy(npc, actor, payload,
            targetCityProjection, targetLocationProjection, execution);
    }

    internal bool TryExecuteSelectedCrossCityBuyActionSlot(
        NpcRuntime npc,
        DailyV1ActionDefinition expectedAction,
        DailyV1NpcActionPayloadOwner payload,
        CityRuntime targetCityProjection,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        out bool actionRollSucceeded,
        out EconomyTransactionResult result)
    {
        actionRollSucceeded = false;
        result = null;
        if (!TryResolveSelectedCrossCityBuy(npc, payload, targetCityProjection,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor,
            out SpatialLocationRuntime targetLocationProjection))
            return false;

        bool trackMarket = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;
        DailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary = trackMarket
            ? new DailyV1SelectedOpenMarketPurchaseBoundary(this, npc, npc.CurrentCity.Market)
            : null;
        if (!DailyV1SelectedActionExecutionStage.TryExecuteSelectedCrossCityBuyActionSlot(
            graph, actor, expectedAction, randomSource, currentAbsoluteDay, purchaseBoundary,
            npc.MerchantTradePlan.CanAdmitSelectedPlanOwnerMutation,
            npc.MerchantTradePlan.PublishSelectedPlanOwnerMutation,
            npc.TravelPlan.CanAdmitSelectedTradeTravelPlanMutation,
            npc.TravelPlan.PublishSelectedTradeTravelPlanMutation,
            out DailyV1SelectedActionExecutionStage.SelectedCrossCityBuyActionResult execution))
            return false;

        actionRollSucceeded = execution.ActionRollSucceeded;
        if (!actionRollSucceeded)
        {
            MarketLiquidityMode liquidity = npc.CurrentCity.Market.Counterparty.LiquidityMode;
            MoneyEffect effect = liquidity == MarketLiquidityMode.AccountBacked
                ? MoneyEffect.Transfer : MoneyEffect.ExplicitSink;
            result = EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketPurchase,
                effect, EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
            return true;
        }

        result = CompleteSelectedCrossCityBuy(npc, actor, payload,
            targetCityProjection, targetLocationProjection, execution.Purchase);
        return true;
    }

    private bool TryResolveSelectedCrossCityBuy(
        NpcRuntime npc,
        DailyV1NpcActionPayloadOwner payload,
        CityRuntime targetCityProjection,
        out DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1NpcRuntimeOwners actor,
        out SpatialLocationRuntime targetLocationProjection)
    {
        graph = npc?.SelectedRuntimeOwnerGraph;
        actor = npc?.SelectedRuntimeOwners;
        DailyV1CityRuntimeOwners originOwner = null;
        DailyV1CityRuntimeOwners targetOwner = null;
        targetLocationProjection = null;
        if (graph != null && targetCityProjection != null)
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            {
                if (string.Equals(candidate.RuntimeId, actor?.Travel?.CurrentCityRuntimeId,
                    StringComparison.Ordinal))
                    originOwner = candidate;
                if (string.Equals(candidate.RuntimeId, targetCityProjection.RuntimeId,
                    StringComparison.Ordinal))
                    targetOwner = candidate;
            }
        if (graph?.SpatialNetwork != null && targetOwner != null)
            graph.SpatialNetwork.TryGetLocation(targetOwner.LocationRuntimeId,
                out targetLocationProjection);
        return npc != null && actor != null && graph != null && payload != null
            && npc.CurrentCity?.Market != null
            && npc.MerchantTradePlan != null && npc.TravelPlan != null
            && originOwner != null && targetOwner != null
            && graph.ContainsExactNpcOwner(actor)
            && graph.ContainsExactCityOwner(originOwner)
            && graph.ContainsExactCityOwner(targetOwner)
            && ReferenceEquals(npc.CurrentCity.SelectedRuntimeOwners, originOwner)
            && ReferenceEquals(npc.CurrentCity.Market.SelectedDailyV1Owner, originOwner.Market)
            && ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
            && ReferenceEquals(npc.MerchantTradePlan.SelectedOwner, actor.MerchantTradePlan)
            && ReferenceEquals(npc.TravelPlan.SelectedOwner, actor.TravelPlan)
            && npc.TravelPlan.CanProjectSelectedTradePlanTarget(actor.TravelPlan,
                targetOwner, targetOwner.RuntimeId, targetOwner.LocationRuntimeId,
                targetLocationProjection, targetCityProjection);
    }

    /// <summary>Validates the existing exact Unity target projection before the Core dispatcher runs.</summary>
    internal bool TryResolveSelectedCrossCityBuyTarget(
        NpcRuntime npc,
        DailyV1NpcActionPayloadOwner payload,
        CityRuntime targetCityProjection,
        out SpatialLocationRuntime targetLocationProjection)
    {
        return TryResolveSelectedCrossCityBuy(npc, payload, targetCityProjection,
            out _, out _, out targetLocationProjection);
    }

    /// <summary>Preserves the existing P12 fault and Unity travel-plan projection after Core dispatch.</summary>
    internal void CompleteSelectedCrossCityBuyDispatch(
        NpcRuntime npc,
        DailyV1SelectedCrossCityBuyResult result,
        SpatialLocationRuntime targetLocationProjection,
        CityRuntime targetCityProjection)
    {
        FaultSelectedCrossCityBuyAdmissionIfPartial(result);
        if (result?.Succeeded != true) return;

        DailyV1NpcRuntimeOwners actor = npc?.SelectedRuntimeOwners;
        if (actor == null || targetLocationProjection == null || targetCityProjection == null)
            throw new InvalidOperationException(
                "The committed selected cross-city buy has no exact Unity travel-plan projection.");
        npc.TravelPlan.ProjectSelectedTradePlan(
            actor.TravelPlan, targetLocationProjection, targetCityProjection);
    }

    private DailyV1SelectedCrossCityBuyResult ExecuteSelectedCrossCityBuyCore(
        NpcRuntime npc,
        DailyV1NpcRuntimeOwners actor,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcActionPayloadOwner payload)
    {
        bool trackMarket = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;
        DailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary = trackMarket
            ? new DailyV1SelectedOpenMarketPurchaseBoundary(this, npc, npc.CurrentCity.Market)
            : null;
        return DailyV1SelectedCrossCityBuyExecution.TryExecute(
            graph, actor, payload, purchaseBoundary,
            npc.MerchantTradePlan.CanAdmitSelectedPlanOwnerMutation,
            npc.MerchantTradePlan.PublishSelectedPlanOwnerMutation,
            npc.TravelPlan.CanAdmitSelectedTradeTravelPlanMutation,
            npc.TravelPlan.PublishSelectedTradeTravelPlanMutation);
    }

    private EconomyTransactionResult CompleteSelectedCrossCityBuy(
        NpcRuntime npc,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload,
        CityRuntime targetCityProjection,
        SpatialLocationRuntime targetLocationProjection,
        DailyV1SelectedCrossCityBuyResult execution)
    {
        MarketLiquidityMode liquidity = npc.CurrentCity.Market.Counterparty.LiquidityMode;
        MoneyEffect effect = liquidity == MarketLiquidityMode.AccountBacked
            ? MoneyEffect.Transfer : MoneyEffect.ExplicitSink;
        if (!execution.Succeeded)
        {
            FaultSelectedCrossCityBuyAdmissionIfPartial(execution);
            EconomyTransactionFailureReason reason = execution.Failure
                == DailyV1SelectedCrossCityBuyFailure.PurchasePreparation
                ? MapSelectedPurchaseFailure(execution.PurchaseCommit?.Failure
                    ?? DailyV1SelectedOpenMarketPurchaseFailure.TransactionCommitFailed)
                : EconomyTransactionFailureReason.TransactionCommitFailed;
            return EconomyTransactionResult.CreateFailure(EconomyTransactionType.OpenMarketPurchase,
                effect, reason, actorRuntimeId: npc.RuntimeId);
        }

        npc.TravelPlan.ProjectSelectedTradePlan(actor.TravelPlan,
            targetLocationProjection, targetCityProjection);
        DailyV1SelectedOpenMarketPurchasePlan plan = execution.PurchasePlan;
        return EconomyTransactionResult.CreateSuccess(EconomyTransactionType.OpenMarketPurchase,
            effect, plan.TotalPrice, plan.ItemDefinitionId,
            payload.Amount, execution.Quantity, execution.UnitPrice, plan.TotalPrice,
            sourceRuntimeId: plan.MarketRuntimeId, destinationRuntimeId: npc.RuntimeId,
            buyerRuntimeId: npc.RuntimeId, sellerRuntimeId: plan.MarketRuntimeId,
            actorRuntimeId: npc.RuntimeId, itemSourceRuntimeId: plan.MarketRuntimeId,
            itemDestinationRuntimeId: npc.RuntimeId);
    }

    internal void FaultSelectedCrossCityBuyAdmissionIfPartial(
        DailyV1SelectedCrossCityBuyResult result)
    {
        if (result?.PartialCommit == true) p12CensusRuntime?.FaultRuntimeAdmission();
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
            new TradeItem(item),
            requestedQuantity,
            market != null ? market.Counterparty.LiquidityMode : MarketLiquidityMode.Open);
    }

    internal EconomyTransactionResult TryExecuteMarketPurchase(
        NpcRuntime npc, MarketRuntime market, DailyV1ItemDefinition item, int requestedQuantity)
    {
        return ExecuteMarketPurchase(npc, market, new TradeItem(item), requestedQuantity,
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
            new TradeItem(item),
            requestedQuantity,
            market != null ? market.Counterparty.LiquidityMode : MarketLiquidityMode.Open);
    }

    internal EconomyTransactionResult TryExecuteMarketSale(
        NpcRuntime npc, MarketRuntime market, DailyV1ItemDefinition item, int requestedQuantity)
    {
        return ExecuteMarketSale(npc, market, new TradeItem(item), requestedQuantity,
            market != null ? market.Counterparty.LiquidityMode : MarketLiquidityMode.Open);
    }

    private EconomyTransactionResult ExecuteMarketPurchase(
        NpcRuntime npc,
        MarketRuntime market,
        TradeItem item,
        int requestedQuantity,
        MarketLiquidityMode liquidityMode)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.OpenMarketPurchase;
        bool accountBacked = liquidityMode == MarketLiquidityMode.AccountBacked;
        MoneyEffect moneyEffect = accountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSink;

        if (npc == null || npc.IsAlive == false || market == null || !item.IsValid
            || !CanUseTradeItem(npc, market, item))
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidInput,
                actorRuntimeId: npc != null ? npc.RuntimeId : null);
        }

        DailyV1SelectedRuntimeOwnerGraph selectedGraph = npc.SelectedRuntimeOwnerGraph;
        if (item.Definition != null && selectedGraph != null)
            return ExecuteSelectedOpenMarketPurchase(
                npc, market, item, requestedQuantity, selectedGraph);

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

        if (!TryGetMarketStock(market, item, out int marketAmount, out float currentPrice)
            || marketAmount <= 0)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientStock,
                actorRuntimeId: npc.RuntimeId);
        }

        if (IsValidNonNegativeFiniteAmount(currentPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                actorRuntimeId: npc.RuntimeId);
        }

        float unitPrice = UnityMathfCompatibility.Max(0.01f, currentPrice);

        if (IsValidNonNegativeFiniteAmount(unitPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                actorRuntimeId: npc.RuntimeId);
        }

        int affordableQuantity = GetAffordableQuantity(npc.MoneyAccount.Balance, unitPrice);
        int quantity = UnityMathfCompatibility.Min(requestedQuantity, marketAmount, affordableQuantity);

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

        if (CanAddInventoryItem(npc.Inventory, item, quantity, unitPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InventoryCapacity,
                actorRuntimeId: npc.RuntimeId);
        }

        if (!market.CanInstall(market.Revision))
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        bool p12Tracking = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;
        if (p12Tracking && accountBacked)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        SimulationOperationScope operationScope = null;
        string accountSectionId = null;
        string inventorySectionId = null;
        string marketSectionId = null;
        if (p12Tracking
            && !p12CensusRuntime.TryBeginP12MarketOperation(
                npc,
                market,
                transactionType,
                out operationScope,
                out accountSectionId,
                out inventorySectionId,
                out marketSectionId))
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        try
        {
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
            NotifyP12MarketOwnerMutation(p12Tracking, accountSectionId, MoneyAccountOwnerBridge.HasP12MutationBoundary(npc.MoneyAccount));

            if (RemoveMarketStock(market, item, quantity) != quantity)
            {
                bool refunded = accountBacked == true
                    ? TryCommitMoneyTransfer(counterparty.MoneyAccount, npc.MoneyAccount, totalPrice)
                    : npc.MoneyAccount.TryCredit(totalPrice);
                if (refunded) NotifyP12MarketOwnerMutation(p12Tracking, accountSectionId, MoneyAccountOwnerBridge.HasP12MutationBoundary(npc.MoneyAccount));

                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    actorRuntimeId: npc.RuntimeId);
            }

            long inventoryRevisionBefore = npc.Inventory.Revision;
            bool inventoryAdded = TryAddInventoryItem(npc.Inventory, item, quantity, unitPrice);
            if (npc.Inventory.Revision != inventoryRevisionBefore)
            {
                NotifyP12MarketOwnerMutation(p12Tracking, inventorySectionId, npc.Inventory.HasP12MutationBoundary);
            }
            if (!inventoryAdded)
            {
                if (p12Tracking) p12CensusRuntime.FaultRuntimeAdmission();
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    actorRuntimeId: npc.RuntimeId);
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
                sourceRuntimeId: accountBacked == true ? npc.RuntimeId : null,
                destinationRuntimeId: accountBacked == true ? counterparty.CounterpartyRuntimeId : null,
                actorRuntimeId: npc.RuntimeId,
                itemSourceRuntimeId: counterparty.CounterpartyRuntimeId,
                itemDestinationRuntimeId: npc.RuntimeId);
        }
        finally
        {
            operationScope?.Dispose();
        }
    }

    private EconomyTransactionResult ExecuteMarketSale(
        NpcRuntime npc,
        MarketRuntime market,
        TradeItem item,
        int requestedQuantity,
        MarketLiquidityMode liquidityMode)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.OpenMarketSale;
        bool accountBacked = liquidityMode == MarketLiquidityMode.AccountBacked;
        MoneyEffect moneyEffect = accountBacked ? MoneyEffect.Transfer : MoneyEffect.ExplicitSource;

        if (npc == null || npc.IsAlive == false || market == null || !item.IsValid
            || !CanUseTradeItem(npc, market, item))
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

        DailyV1SelectedRuntimeOwnerGraph selectedGraph = item.Definition != null
            ? npc.SelectedRuntimeOwnerGraph
            : null;
        if (!accountBacked && selectedGraph != null)
        {
            return ExecuteSelectedOpenMarketSale(
                npc, market, item, requestedQuantity, selectedGraph);
        }

        if (requestedQuantity <= 0)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidQuantity,
                actorRuntimeId: npc.RuntimeId);
        }

        int availableQuantity = GetInventoryAmount(npc.Inventory, item);
        int quantity = UnityMathfCompatibility.Min(requestedQuantity, availableQuantity);

        if (quantity <= 0)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientInventory,
                actorRuntimeId: npc.RuntimeId);
        }

        float salePrice = GetMarketSalePrice(market, item);

        if (IsValidNonNegativeFiniteAmount(salePrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                actorRuntimeId: npc.RuntimeId);
        }

        float unitPrice = UnityMathfCompatibility.Max(0.01f, salePrice);

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
            quantity = UnityMathfCompatibility.Min(quantity, affordableQuantity);

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

        if (CanAddMarketStock(market, item, quantity) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.MarketCapacity,
                actorRuntimeId: npc.RuntimeId);
        }

        if (!market.CanInstall(market.Revision))
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        bool p12Tracking = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;
        if (p12Tracking && accountBacked)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        SimulationOperationScope operationScope = null;
        string accountSectionId = null;
        string inventorySectionId = null;
        string marketSectionId = null;
        if (p12Tracking
            && !p12CensusRuntime.TryBeginP12MarketOperation(
                npc,
                market,
                transactionType,
                out operationScope,
                out accountSectionId,
                out inventorySectionId,
                out marketSectionId))
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        try
        {
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
            NotifyP12MarketOwnerMutation(p12Tracking, accountSectionId, MoneyAccountOwnerBridge.HasP12MutationBoundary(npc.MoneyAccount));

            if (RemoveInventoryItem(npc.Inventory, item, quantity) == false)
            {
                bool refunded = accountBacked == true
                    ? TryCommitMoneyTransfer(npc.MoneyAccount, counterparty.MoneyAccount, totalPrice)
                    : npc.MoneyAccount.TryDebit(totalPrice);
                if (refunded) NotifyP12MarketOwnerMutation(p12Tracking, accountSectionId, MoneyAccountOwnerBridge.HasP12MutationBoundary(npc.MoneyAccount));

                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    actorRuntimeId: npc.RuntimeId);
            }
            NotifyP12MarketOwnerMutation(p12Tracking, inventorySectionId, npc.Inventory.HasP12MutationBoundary);

            int addedToMarket = AddMarketStock(market, item, quantity);
            if (addedToMarket != quantity)
            {
                if (p12Tracking) p12CensusRuntime.FaultRuntimeAdmission();
                return EconomyTransactionResult.CreateFailure(
                    transactionType,
                    moneyEffect,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    actorRuntimeId: npc.RuntimeId);
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
                sourceRuntimeId: accountBacked == true ? counterparty.CounterpartyRuntimeId : null,
                destinationRuntimeId: accountBacked == true ? npc.RuntimeId : null,
                actorRuntimeId: npc.RuntimeId,
                itemSourceRuntimeId: npc.RuntimeId,
                itemDestinationRuntimeId: counterparty.CounterpartyRuntimeId);
        }
        finally
        {
            operationScope?.Dispose();
        }
    }

    private EconomyTransactionResult ExecuteSelectedOpenMarketSale(
        NpcRuntime npc,
        MarketRuntime market,
        TradeItem item,
        int requestedQuantity,
        DailyV1SelectedRuntimeOwnerGraph selectedGraph)
    {
        if (selectedGraph == null)
        {
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.OpenMarketSale,
                MoneyEffect.ExplicitSource,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                actorRuntimeId: npc.RuntimeId);
        }

        bool p12Tracking = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;
        DailyV1SelectedOpenMarketSaleBoundary boundary = p12Tracking
            ? new DailyV1SelectedOpenMarketSaleBoundary(this, npc, market)
            : null;
        DailyV1SelectedOpenMarketSaleExecution execution =
            DailyV1SelectedOpenMarketSaleExecution.TryExecute(
                selectedGraph,
                npc.SelectedRuntimeOwners,
                market.SelectedDailyV1Owner,
                item.Definition,
                requestedQuantity,
                boundary);

        if (!execution.Prepared)
        {
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.OpenMarketSale,
                MoneyEffect.ExplicitSource,
                MapSelectedSaleFailure(execution.PreparationFailure),
                actorRuntimeId: npc.RuntimeId);
        }

        DailyV1SelectedOpenMarketSalePlan plan = execution.Plan;
        DailyV1SelectedOpenMarketSaleCommitResult commit = execution.Commit;

        if (!commit.Succeeded)
        {
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.OpenMarketSale,
                MoneyEffect.ExplicitSource,
                MapSelectedSaleFailure(commit.Failure),
                actorRuntimeId: npc.RuntimeId);
        }

        return EconomyTransactionResult.CreateSuccess(
            EconomyTransactionType.OpenMarketSale,
            MoneyEffect.ExplicitSource,
            plan.TotalPrice,
            plan.ItemDefinitionId,
            plan.RequestedQuantity,
            plan.Quantity,
            plan.UnitPrice,
            plan.TotalPrice,
            actorRuntimeId: npc.RuntimeId,
            itemSourceRuntimeId: npc.RuntimeId,
            itemDestinationRuntimeId: plan.MarketRuntimeId);
    }

    private EconomyTransactionResult ExecuteSelectedOpenMarketPurchase(
        NpcRuntime npc,
        MarketRuntime market,
        TradeItem item,
        int requestedQuantity,
        DailyV1SelectedRuntimeOwnerGraph selectedGraph)
    {
        bool p12Tracking = p12CensusRuntime != null
            && p12CensusRuntime.HasP12MarketOperationAdapter;
        DailyV1SelectedOpenMarketPurchaseBoundary boundary = p12Tracking
            ? new DailyV1SelectedOpenMarketPurchaseBoundary(this, npc, market)
            : null;
        DailyV1SelectedOpenMarketPurchaseExecution execution =
            DailyV1SelectedOpenMarketPurchaseExecution.TryExecute(
                selectedGraph, npc.SelectedRuntimeOwners, market.SelectedDailyV1Owner,
                item.Definition, requestedQuantity, boundary);
        MarketLiquidityMode liquidityMode = market.Counterparty.LiquidityMode;
        MoneyEffect moneyEffect = liquidityMode == MarketLiquidityMode.AccountBacked
            ? MoneyEffect.Transfer : MoneyEffect.ExplicitSink;
        if (execution.Plan == null)
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.OpenMarketPurchase, moneyEffect,
                MapSelectedPurchaseFailure(execution.PreparationFailure),
                actorRuntimeId: npc.RuntimeId);
        if (execution.Commit == null || !execution.Commit.Succeeded)
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.OpenMarketPurchase, moneyEffect,
                MapSelectedPurchaseFailure(execution.Commit?.Failure
                    ?? DailyV1SelectedOpenMarketPurchaseFailure.TransactionCommitFailed),
                actorRuntimeId: npc.RuntimeId);

        DailyV1SelectedOpenMarketPurchasePlan plan = execution.Plan;
        return EconomyTransactionResult.CreateSuccess(
            EconomyTransactionType.OpenMarketPurchase, moneyEffect, plan.TotalPrice,
            plan.ItemDefinitionId, requestedQuantity, plan.Quantity, plan.UnitPrice,
            plan.TotalPrice, sourceRuntimeId: plan.MarketRuntimeId,
            destinationRuntimeId: npc.RuntimeId, buyerRuntimeId: npc.RuntimeId,
            sellerRuntimeId: plan.MarketRuntimeId, actorRuntimeId: npc.RuntimeId,
            itemSourceRuntimeId: plan.MarketRuntimeId,
            itemDestinationRuntimeId: npc.RuntimeId);
    }

    private static EconomyTransactionFailureReason MapSelectedPurchaseFailure(
        DailyV1SelectedOpenMarketPurchaseFailure failure) => failure switch
        {
            DailyV1SelectedOpenMarketPurchaseFailure.InvalidQuantity => EconomyTransactionFailureReason.InvalidQuantity,
            DailyV1SelectedOpenMarketPurchaseFailure.InsufficientStock => EconomyTransactionFailureReason.InsufficientStock,
            DailyV1SelectedOpenMarketPurchaseFailure.InvalidPrice => EconomyTransactionFailureReason.InvalidPrice,
            DailyV1SelectedOpenMarketPurchaseFailure.InsufficientFunds => EconomyTransactionFailureReason.InsufficientFunds,
            DailyV1SelectedOpenMarketPurchaseFailure.CounterpartyCreditCapacity => EconomyTransactionFailureReason.CounterpartyCreditCapacity,
            DailyV1SelectedOpenMarketPurchaseFailure.InventoryCapacity => EconomyTransactionFailureReason.InventoryCapacity,
            DailyV1SelectedOpenMarketPurchaseFailure.None => EconomyTransactionFailureReason.None,
            _ => EconomyTransactionFailureReason.TransactionCommitFailed
        };

    /// <summary>Unity P12 scope and invalidation adapter for one Core purchase.</summary>
    private sealed class DailyV1SelectedOpenMarketPurchaseBoundary : IDailyV1SelectedOpenMarketPurchaseBoundary
    {
        private readonly EconomyTransactionService service;
        private readonly NpcRuntime npc;
        private readonly MarketRuntime market;
        private SimulationOperationScope operationScope;
        private string accountSectionId;
        private string inventorySectionId;
        private string marketSectionId;

        internal DailyV1SelectedOpenMarketPurchaseBoundary(
            EconomyTransactionService service, NpcRuntime npc, MarketRuntime market)
        { this.service = service; this.npc = npc; this.market = market; }

        public bool CanUseAccountBacked => false;

        public bool TryBegin() => service.p12CensusRuntime.TryBeginP12MarketOperation(
            npc, market, EconomyTransactionType.OpenMarketPurchase,
            out operationScope, out accountSectionId,
            out inventorySectionId, out marketSectionId);

        public void Complete(DailyV1SelectedOpenMarketPurchaseCommitResult result)
        {
            if (result == null) return;
            if (result.BuyerAccountMutationCount > 0)
            {
                bool ownerNotifiesCommit = MoneyAccountOwnerBridge.HasP12MutationBoundary(npc.MoneyAccount);
                for (int i = 0; i < result.BuyerAccountMutationCount; i++)
                    service.NotifyP12MarketOwnerMutation(true, accountSectionId, ownerNotifiesCommit);
            }
            if (result.InventoryMutationCount > 0)
            {
                bool ownerNotifiesCommit = npc.Inventory.HasP12MutationBoundary;
                for (int i = 0; i < result.InventoryMutationCount; i++)
                    service.NotifyP12MarketOwnerMutation(true, inventorySectionId, ownerNotifiesCommit);
            }
            if (result.MarketMutationCount > 0)
            {
                bool ownerNotifiesCommit = market.SelectedDailyV1Owner.HasMutationBoundary;
                for (int i = 0; i < result.MarketMutationCount; i++)
                    service.NotifyP12MarketOwnerMutation(true, marketSectionId, ownerNotifiesCommit);
            }
            if (result.PartialCommit) service.p12CensusRuntime.FaultRuntimeAdmission();
        }

        public void Dispose() => operationScope?.Dispose();
    }

    private static EconomyTransactionFailureReason MapSelectedSaleFailure(
        DailyV1SelectedOpenMarketSaleFailure failure) => failure switch
        {
            DailyV1SelectedOpenMarketSaleFailure.InvalidQuantity => EconomyTransactionFailureReason.InvalidQuantity,
            DailyV1SelectedOpenMarketSaleFailure.InsufficientInventory => EconomyTransactionFailureReason.InsufficientInventory,
            DailyV1SelectedOpenMarketSaleFailure.InvalidPrice => EconomyTransactionFailureReason.InvalidPrice,
            DailyV1SelectedOpenMarketSaleFailure.CreditRejected => EconomyTransactionFailureReason.CreditRejected,
            DailyV1SelectedOpenMarketSaleFailure.MarketCapacity => EconomyTransactionFailureReason.MarketCapacity,
            DailyV1SelectedOpenMarketSaleFailure.None => EconomyTransactionFailureReason.None,
            _ => EconomyTransactionFailureReason.TransactionCommitFailed
        };

    private void NotifyP12MarketOwnerMutation(bool tracking, string sectionId, bool ownerNotifiesCommit)
    {
        if (!tracking || ownerNotifiesCommit) return;
        p12CensusRuntime.NotifyP12MarketOperationOwnerMutation(sectionId);
    }

    /// <summary>Unity-facing P12 scope and invalidation adapter for one Core sale.</summary>
    private sealed class DailyV1SelectedOpenMarketSaleBoundary : IDailyV1SelectedOpenMarketSaleBoundary
    {
        private readonly EconomyTransactionService service;
        private readonly NpcRuntime npc;
        private readonly MarketRuntime market;
        private SimulationOperationScope operationScope;
        private string accountSectionId;
        private string inventorySectionId;
        private string marketSectionId;

        internal DailyV1SelectedOpenMarketSaleBoundary(
            EconomyTransactionService service,
            NpcRuntime npc,
            MarketRuntime market)
        {
            this.service = service;
            this.npc = npc;
            this.market = market;
        }

        public bool TryBegin() => service.p12CensusRuntime.TryBeginP12MarketOperation(
            npc,
            market,
            EconomyTransactionType.OpenMarketSale,
            out operationScope,
            out accountSectionId,
            out inventorySectionId,
            out marketSectionId);

        public void Complete(DailyV1SelectedOpenMarketSaleCommitResult result)
        {
            if (result == null) return;
            if (result.AccountMutationCount > 0)
            {
                bool ownerNotifiesCommit = MoneyAccountOwnerBridge.HasP12MutationBoundary(npc.MoneyAccount);
                for (int i = 0; i < result.AccountMutationCount; i++)
                    service.NotifyP12MarketOwnerMutation(true, accountSectionId, ownerNotifiesCommit);
            }
            if (result.InventoryMutationCount > 0)
            {
                bool ownerNotifiesCommit = npc.Inventory.HasP12MutationBoundary;
                for (int i = 0; i < result.InventoryMutationCount; i++)
                    service.NotifyP12MarketOwnerMutation(true, inventorySectionId, ownerNotifiesCommit);
            }
            if (result.Succeeded)
            {
                bool ownerNotifiesCommit = market.SelectedDailyV1Owner.HasMutationBoundary;
                service.NotifyP12MarketOwnerMutation(true, marketSectionId, ownerNotifiesCommit);
            }
            if (result.PartialCommit)
                service.p12CensusRuntime.FaultRuntimeAdmission();
        }

        public void Dispose() => operationScope?.Dispose();
    }

    private static bool CanUseTradeItem(NpcRuntime npc, MarketRuntime market, TradeItem item)
    {
        if (item.Definition == null) return true;
        DailyV1InventoryOwner inventoryOwner = npc.Inventory?.SelectedDailyV1Owner;
        DailyV1MarketOwner marketOwner = market.SelectedDailyV1Owner;
        DailyV1EffectiveDefinitions definitions = inventoryOwner?.Definitions;
        return inventoryOwner != null && marketOwner != null
            && ReferenceEquals(definitions, marketOwner.Definitions)
            && definitions.Items.TryGetValue(item.Definition.DefinitionId, out DailyV1ItemDefinition admitted)
            && ReferenceEquals(item.Definition, admitted);
    }

    private static bool CanUseTradeItem(NpcRuntime buyer, NpcRuntime seller, TradeItem item)
    {
        if (item.Definition == null) return true;
        DailyV1EffectiveDefinitions buyerDefinitions = buyer?.Inventory?.SelectedDailyV1Owner?.Definitions;
        DailyV1EffectiveDefinitions sellerDefinitions = seller?.Inventory?.SelectedDailyV1Owner?.Definitions;
        return buyerDefinitions != null && ReferenceEquals(buyerDefinitions, sellerDefinitions)
            && buyerDefinitions.Items.TryGetValue(item.Definition.DefinitionId, out DailyV1ItemDefinition admitted)
            && ReferenceEquals(item.Definition, admitted);
    }

    private static bool TryGetMarketStock(MarketRuntime market, TradeItem item, out int amount, out float price)
    {
        amount = 0;
        price = 0f;
        if (item.Definition != null)
        {
            DailyV1MarketItemState row = market.SelectedDailyV1Owner?.GetItem(item.Definition);
            if (row == null) return false;
            amount = row.Amount;
            price = row.CurrentPrice;
            return true;
        }

        MarketItemRuntime legacy = market.GetItem(item.AuthoringItem);
        if (legacy == null) return false;
        amount = legacy.Amount;
        price = legacy.CurrentPrice;
        return true;
    }

    private static int GetInventoryAmount(InventoryRuntime inventory, TradeItem item) =>
        item.Definition != null
            ? inventory.GetSelectedAmount(item.Definition)
            : inventory.GetAmount(item.AuthoringItem);

    private static int GetMarketAmount(MarketRuntime market, TradeItem item) =>
        item.Definition != null
            ? market.GetSelectedAmount(item.Definition)
            : market.GetAmount(item.AuthoringItem);

    private static bool CanAddInventoryItem(InventoryRuntime inventory, TradeItem item, int amount, float unitCost) =>
        item.Definition != null
            ? inventory.CanAddSelectedItem(item.Definition, amount, unitCost)
            : inventory.CanAddItem(item.AuthoringItem, amount, unitCost);

    private static bool TryAddInventoryItem(InventoryRuntime inventory, TradeItem item, int amount, float unitCost) =>
        item.Definition != null
            ? inventory.TryAddSelectedItem(item.Definition, amount, unitCost)
            : inventory.TryAddItem(item.AuthoringItem, amount, unitCost);

    private static bool RemoveInventoryItem(InventoryRuntime inventory, TradeItem item, int amount) =>
        item.Definition != null
            ? inventory.RemoveSelectedItem(item.Definition, amount)
            : inventory.RemoveItem(item.AuthoringItem, amount);

    private static float GetMarketSalePrice(MarketRuntime market, TradeItem item) =>
        item.Definition != null
            ? market.GetSelectedPriceForSale(item.Definition)
            : market.GetPriceForSale(item.AuthoringItem);

    private static bool CanAddMarketStock(MarketRuntime market, TradeItem item, int amount) =>
        item.Definition != null
            ? market.CanAddSelectedStock(item.Definition, amount)
            : market.CanAddStock(item.AuthoringItem, amount);

    private static int AddMarketStock(MarketRuntime market, TradeItem item, int amount) =>
        item.Definition != null
            ? market.AddSelectedStock(item.Definition, amount)
            : market.AddStock(item.AuthoringItem, amount);

    private static int RemoveMarketStock(MarketRuntime market, TradeItem item, int amount) =>
        item.Definition != null
            ? market.RemoveSelectedStock(item.Definition, amount)
            : market.RemoveStockUpTo(item.AuthoringItem, amount);

    private static PreparedInventoryState PrepareInventoryReplacement(
        InventoryRuntime inventory, TradeItem item, int removed, float addedUnitCost, bool add, int added) =>
        item.Definition != null
            ? inventory.PrepareSelectedReplacement(item.Definition, removed, addedUnitCost, add, added)
            : inventory.PrepareReplacement(item.AuthoringItem, removed, addedUnitCost, add, added);

    private static PreparedMarketState PrepareMarketReplacement(
        MarketRuntime market, TradeItem item, int delta, int desiredAmount) =>
        item.Definition != null
            ? market.PrepareSelectedReplacement(item.Definition, delta, desiredAmount)
            : market.PrepareReplacement(item.AuthoringItem, delta, desiredAmount);

    public EconomyTransactionResult TryExecutePopulationConsumption(
        PopulationEconomyRuntime population,
        MarketRuntime market,
        ItemData item,
        int requestedQuantity)
    {
        return ExecutePopulationConsumption(population, market, new TradeItem(item), requestedQuantity);
    }

    internal EconomyTransactionResult TryExecutePopulationConsumption(
        PopulationEconomyRuntime population,
        MarketRuntime market,
        DailyV1ItemDefinition item,
        int requestedQuantity)
    {
        if (market == null)
        {
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.PopulationConsumption,
                MoneyEffect.Transfer,
                EconomyTransactionFailureReason.InvalidInput,
                actorRuntimeId: population != null ? population.PopulationEconomicRuntimeId : null);
        }

        DailyV1PopulationConsumptionOutcome outcome = DailyV1PopulationConsumptionOperation.Execute(
            population, market.Counterparty, market.SelectedDailyV1Owner, item, requestedQuantity);
        if (outcome.Success)
        {
            return EconomyTransactionResult.CreateSuccess(
                EconomyTransactionType.PopulationConsumption,
                MoneyEffect.Transfer,
                outcome.TotalPrice,
                outcome.ItemDefinitionId,
                outcome.RequestedQuantity,
                outcome.Quantity,
                outcome.UnitPrice,
                outcome.TotalPrice,
                outcome.SourceRuntimeId,
                outcome.DestinationRuntimeId,
                actorRuntimeId: outcome.ActorRuntimeId,
                itemSourceRuntimeId: outcome.ItemSourceRuntimeId,
                itemDestinationRuntimeId: outcome.ItemDestinationRuntimeId);
        }

        return EconomyTransactionResult.CreateFailure(
            EconomyTransactionType.PopulationConsumption,
            MoneyEffect.Transfer,
            MapPopulationConsumptionFailure(outcome.Failure),
            actorRuntimeId: outcome.ActorRuntimeId);
    }

    private static EconomyTransactionFailureReason MapPopulationConsumptionFailure(
        DailyV1PopulationConsumptionFailure failure)
    {
        switch (failure)
        {
            case DailyV1PopulationConsumptionFailure.InvalidInput:
                return EconomyTransactionFailureReason.InvalidInput;
            case DailyV1PopulationConsumptionFailure.InvalidQuantity:
                return EconomyTransactionFailureReason.InvalidQuantity;
            case DailyV1PopulationConsumptionFailure.InvalidPrice:
                return EconomyTransactionFailureReason.InvalidPrice;
            case DailyV1PopulationConsumptionFailure.InsufficientFunds:
                return EconomyTransactionFailureReason.InsufficientFunds;
            case DailyV1PopulationConsumptionFailure.InsufficientStock:
                return EconomyTransactionFailureReason.InsufficientStock;
            case DailyV1PopulationConsumptionFailure.CounterpartyCreditCapacity:
                return EconomyTransactionFailureReason.CounterpartyCreditCapacity;
            default:
                return EconomyTransactionFailureReason.TransactionCommitFailed;
        }
    }

    private EconomyTransactionResult ExecutePopulationConsumption(
        PopulationEconomyRuntime population,
        MarketRuntime market,
        TradeItem item,
        int requestedQuantity)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.PopulationConsumption;
        MoneyEffect moneyEffect = MoneyEffect.Transfer;

        if (population == null || market == null || !item.IsValid)
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
        DailyV1MarketItemState selectedMarketItem = item.Definition != null
            ? market.GetSelectedItem(item.Definition)
            : null;
        MarketItemRuntime legacyMarketItem = item.AuthoringItem != null
            ? market.GetItem(item.AuthoringItem)
            : null;
        int available = selectedMarketItem?.Amount ?? legacyMarketItem?.Amount ?? 0;
        float currentPrice = selectedMarketItem?.CurrentPrice ?? legacyMarketItem?.CurrentPrice ?? 0f;

        if (available <= 0 || (item.Definition != null && selectedMarketItem == null)
            || (item.AuthoringItem != null && legacyMarketItem == null))
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InsufficientStock,
                actorRuntimeId: population.PopulationEconomicRuntimeId);
        }

        float unitPrice = currentPrice;

        if (IsValidNonNegativeFiniteAmount(unitPrice) == false)
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType,
                moneyEffect,
                EconomyTransactionFailureReason.InvalidPrice,
                actorRuntimeId: population.PopulationEconomicRuntimeId);
        }

        unitPrice = UnityMathfCompatibility.Max(0.01f, unitPrice);
        int affordableQuantity = GetAffordableQuantity(population.MoneyAccount.Balance, unitPrice);
        int quantity = UnityMathfCompatibility.Min(requestedQuantity, available, affordableQuantity);

        if (quantity <= 0)
        {
            EconomyTransactionFailureReason reason = available <= 0
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

        int removed = item.Definition != null
            ? market.RemoveSelectedStock(item.Definition, quantity)
            : market.RemoveStockUpTo(item.AuthoringItem, quantity);
        if (removed != quantity)
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
        return DailyV1MoneyTransferOperation.TryExecute(
            source, destination, amount, sourceRuntimeId, destinationRuntimeId);
    }

    private static EconomyTransactionResult ValidateMoneyTransfer(
        MoneyAccountRuntime source,
        MoneyAccountRuntime destination,
        float amount,
        string sourceRuntimeId,
        string destinationRuntimeId)
        => DailyV1MoneyTransferOperation.Validate(
            source, destination, amount, sourceRuntimeId, destinationRuntimeId);

    private static bool TryCommitMoneyTransfer(
        MoneyAccountRuntime source,
        MoneyAccountRuntime destination,
        float amount)
        => DailyV1MoneyTransferOperation.TryCommit(source, destination, amount);

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

        return UnityMathfCompatibility.Max(0, UnityMathfCompatibility.FloorToInt(affordableAmount));
    }

    private static bool IsValidNonNegativeFiniteAmount(float amount)
    {
        return amount >= 0f
            && float.IsNaN(amount) == false
            && float.IsInfinity(amount) == false;
    }
}
