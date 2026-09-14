using System;

public sealed class CityConsumptionResult
{
    private readonly string settlementRuntimeId;
    private readonly string populationRuntimeId;
    private readonly string itemDefinitionId;
    private readonly int requestedQuantity;
    private readonly int consumedQuantity;
    private readonly ConsumptionPaymentMode paymentMode;
    private readonly float unitPrice;
    private readonly float totalPaid;

    public string SettlementRuntimeId => settlementRuntimeId;
    public string PopulationRuntimeId => populationRuntimeId;
    public string PopulationEconomicRuntimeId => populationRuntimeId;
    public string ItemDefinitionId => itemDefinitionId;
    public int RequestedQuantity => requestedQuantity;
    public int ConsumedQuantity => consumedQuantity;
    public int EffectiveQuantity => consumedQuantity;
    public ConsumptionPaymentMode PaymentMode => paymentMode;
    public float UnitPrice => unitPrice;
    public float TotalPaid => totalPaid;

    public CityConsumptionResult(
        string settlementRuntimeId,
        string populationRuntimeId,
        string itemDefinitionId,
        int requestedQuantity,
        int consumedQuantity,
        ConsumptionPaymentMode paymentMode,
        float unitPrice,
        float totalPaid)
    {
        this.settlementRuntimeId = settlementRuntimeId;
        this.populationRuntimeId = populationRuntimeId;
        this.itemDefinitionId = itemDefinitionId;
        this.requestedQuantity = Math.Max(0, requestedQuantity);
        this.consumedQuantity = Math.Max(0, consumedQuantity);
        this.paymentMode = paymentMode;
        this.unitPrice = unitPrice;
        this.totalPaid = totalPaid;
    }
}
