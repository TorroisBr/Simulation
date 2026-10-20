using System;

/// <summary>
/// Dispatches a selected action payload to its exact Core-owned local market
/// transaction. The host supplies only P12 admission and completion adapters.
/// </summary>
internal static class DailyV1SelectedOpenMarketActionExecution
{
    internal static bool TryExecute(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload,
        IDailyV1SelectedOpenMarketSaleBoundary saleBoundary,
        IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary,
        out DailyV1SelectedOpenMarketSaleExecution sale,
        out DailyV1SelectedOpenMarketPurchaseExecution purchase)
    {
        sale = null;
        purchase = null;
        if (graph == null || actor == null || payload == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, payload.Action)
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, payload.Action, actor.Status?.Statuses)
            || actor.Travel == null || actor.Travel.IsTraveling
            || string.IsNullOrWhiteSpace(actor.Travel.CurrentCityRuntimeId)
            || !string.Equals(payload.TargetCityRuntimeId,
                actor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)
            || payload.TargetNpcRuntimeId != null
            || payload.TargetItem == null || payload.Amount <= 0
            || (payload.Action.Values.actionType?.Value != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
                && payload.Action.Values.actionType?.Value != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue))
            return false;

        DailyV1CityRuntimeOwners city = null;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            if (string.Equals(candidate.RuntimeId, actor.Travel.CurrentCityRuntimeId,
                    StringComparison.Ordinal))
            { city = candidate; break; }
        if (city == null || !graph.ContainsExactCityOwner(city)) return false;

        if (payload.Action.Values.actionType.Value == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue)
        {
            sale = DailyV1SelectedOpenMarketSaleExecution.TryExecute(
                graph, actor, city.Market, payload.TargetItem, payload.Amount, saleBoundary);
            return sale.Prepared && sale.Commit?.Succeeded == true;
        }

        purchase = DailyV1SelectedOpenMarketPurchaseExecution.TryExecute(
            graph, actor, city.Market, payload.TargetItem, payload.Amount, purchaseBoundary);
        return purchase.Plan != null && purchase.Commit?.Succeeded == true;
    }
}
