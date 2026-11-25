internal static class DailyV1SelectedMarketSaleProfit
{
    /// <summary>Retains the existing unplanned sale's pre-mutation inventory
    /// average-cost calculation for the Unity Trade-category projection.</summary>
    internal static float Calculate(float preSaleAverageUnitCost, float realizedUnitPrice, int quantity) =>
        (realizedUnitPrice - preSaleAverageUnitCost) * quantity;
}
