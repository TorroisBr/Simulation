using System;
using System.Collections.Generic;

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
