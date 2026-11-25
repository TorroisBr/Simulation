using System;

/// <summary>
/// Runs the selected Daily-v1 pre-actor knowledge stage against the same
/// authoritative owners used by actor turns and persistence. It discovers the
/// current location, records the selected merchant's current City facts, and
/// then performs the existing local merchant-sharing operation.
/// </summary>
internal static class DailyV1SelectedLocalKnowledgeRefreshStage
{
    internal static bool TryRun(
        DailyV1SelectedRuntimeOwnerGraph graph,
        long absoluteDay)
    {
        if (graph == null || absoluteDay < 0L)
            return false;

        EffectiveSimulationConfiguration configuration = graph.Definitions?.EffectiveConfiguration;
        bool merchantTradeEnabled = configuration?.MerchantTrade?.Enabled == true;

        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc?.Lifecycle?.LifeState != NpcLifeState.Alive
                || npc.Travel == null || npc.Travel.IsTraveling
                || string.IsNullOrWhiteSpace(npc.Travel.CurrentLocationRuntimeId))
                continue;

            if (npc.SpatialKnowledge == null) return false;
            npc.SpatialKnowledge.DiscoverLocation(npc.Travel.CurrentLocationRuntimeId);

            if (!merchantTradeEnabled
                || string.IsNullOrWhiteSpace(npc.Travel.CurrentCityRuntimeId)
                || !DailyV1NpcDefinitionRules.HasJobType(npc.Definition, graph.Definitions,
                    DailyV1NpcDefinitionRules.MerchantJobTypeValue))
                continue;

            if (!TryFindCity(graph, npc.Travel.CurrentCityRuntimeId,
                    out DailyV1CityRuntimeOwners city))
                return false;

            DailyV1SelectedMerchantTradeStateOperation.ObserveCurrentMarket(
                npc, city, absoluteDay);
        }

        if (!merchantTradeEnabled) return true;
        int maximum = Math.Max(1,
            configuration?.CommercialKnowledge?.MaxSharedObservationsPerInteraction ?? 2);
        return DailyV1CommercialKnowledgeSharingStage.TryShareAmongPresentMerchants(
            graph, absoluteDay, configuration?.CommercialKnowledge, maximum);
    }

    private static bool TryFindCity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string cityRuntimeId,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
        {
            if (candidate != null && string.Equals(candidate.RuntimeId, cityRuntimeId,
                    StringComparison.Ordinal))
            {
                city = candidate;
                return true;
            }
        }

        return false;
    }
}
