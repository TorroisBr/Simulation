using System;

[Serializable]
public sealed class CityProductionResult
{
    public string SettlementRuntimeId { get; }
    public string StockOwnerRuntimeId { get; }
    public string ItemDefinitionId { get; }
    public int QuantityProduced { get; }

    public CityProductionResult(
        string settlementRuntimeId,
        string itemDefinitionId,
        int quantityProduced,
        string stockOwnerRuntimeId = null)
    {
        SettlementRuntimeId = settlementRuntimeId;
        StockOwnerRuntimeId = stockOwnerRuntimeId;
        ItemDefinitionId = itemDefinitionId;
        QuantityProduced = Math.Max(0, quantityProduced);
    }
}
