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
        if (string.IsNullOrWhiteSpace(settlementRuntimeId) == true)
        {
            throw new ArgumentException(
                "CityProductionResult requires a non-empty settlement RuntimeId.",
                nameof(settlementRuntimeId));
        }

        if (string.IsNullOrWhiteSpace(stockOwnerRuntimeId) == true)
        {
            throw new ArgumentException(
                "CityProductionResult requires a non-empty stock owner RuntimeId.",
                nameof(stockOwnerRuntimeId));
        }

        if (string.IsNullOrWhiteSpace(itemDefinitionId) == true)
        {
            throw new ArgumentException(
                "CityProductionResult requires a non-empty item DefinitionId.",
                nameof(itemDefinitionId));
        }

        if (quantityProduced <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantityProduced),
                quantityProduced,
                "CityProductionResult requires a positive produced quantity.");
        }

        SettlementRuntimeId = settlementRuntimeId;
        StockOwnerRuntimeId = stockOwnerRuntimeId;
        ItemDefinitionId = itemDefinitionId;
        QuantityProduced = quantityProduced;
    }
}
