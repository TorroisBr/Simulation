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

public sealed class EconomyTransactionService
{
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

        if (buyer == null || seller == null || item == null)
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

        if (npc == null || market == null || item == null)
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

        if (npc == null || market == null || item == null)
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
