using System;
using System.Collections.Generic;

/// <summary>
/// Executes the selected Daily-v1 local merchant knowledge-sharing stage over
/// the exact Core owner graph. Unity keeps only the roster/runtime adapter;
/// observations, freshness, ordering, pairing, and committed owner writes use
/// the same authorities consumed by persistence and merchant planning.
/// </summary>
internal static class DailyV1CommercialKnowledgeSharingStage
{
    private sealed class Merchant
    {
        internal DailyV1NpcRuntimeOwners Owner { get; }
        internal DailyV1CityRuntimeOwners City { get; }

        internal Merchant(DailyV1NpcRuntimeOwners owner, DailyV1CityRuntimeOwners city)
        {
            Owner = owner;
            City = city;
        }
    }

    internal static bool TryShareAmongPresentMerchants(
        DailyV1SelectedRuntimeOwnerGraph graph,
        long absoluteDay,
        EffectiveCommercialKnowledgeConfiguration configuration,
        int maxSharedObservationsPerInteraction)
    {
        if (graph == null || absoluteDay < 0L || maxSharedObservationsPerInteraction < 1)
            return false;

        List<Merchant> merchants = new List<Merchant>();
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (!IsEligibleMerchant(graph, npc, out DailyV1CityRuntimeOwners city)) continue;
            merchants.Add(new Merchant(npc, city));
        }

        merchants.Sort(CompareMerchants);
        Dictionary<string, IReadOnlyList<DailyV1CommercialKnowledgeShareValue>> phaseSnapshots =
            CapturePhaseSnapshots(graph, merchants, absoluteDay, configuration);

        int groupStart = 0;
        while (groupStart < merchants.Count)
        {
            string cityRuntimeId = merchants[groupStart].City.RuntimeId;
            int groupEnd = groupStart + 1;
            while (groupEnd < merchants.Count
                && string.Equals(merchants[groupEnd].City.RuntimeId, cityRuntimeId,
                    StringComparison.Ordinal))
                groupEnd++;

            int groupCount = groupEnd - groupStart;
            int rotation = (int)(absoluteDay % groupCount);
            for (int pairOffset = 0; pairOffset + 1 < groupCount; pairOffset += 2)
            {
                int firstIndex = groupStart + (rotation + pairOffset) % groupCount;
                int secondIndex = groupStart + (rotation + pairOffset + 1) % groupCount;
                Merchant first = merchants[firstIndex];
                Merchant second = merchants[secondIndex];
                if (!phaseSnapshots.TryGetValue(first.Owner.RuntimeId,
                        out IReadOnlyList<DailyV1CommercialKnowledgeShareValue> firstSnapshot)
                    || !phaseSnapshots.TryGetValue(second.Owner.RuntimeId,
                        out IReadOnlyList<DailyV1CommercialKnowledgeShareValue> secondSnapshot))
                    return false;

                ShareSnapshot(graph, first.Owner, second.Owner, firstSnapshot,
                    absoluteDay, maxSharedObservationsPerInteraction);
                ShareSnapshot(graph, second.Owner, first.Owner, secondSnapshot,
                    absoluteDay, maxSharedObservationsPerInteraction);
            }

            groupStart = groupEnd;
        }

        return true;
    }

    private static bool IsEligibleMerchant(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        if (npc == null || npc.Lifecycle == null || npc.Lifecycle.LifeState != NpcLifeState.Alive
            || npc.Travel == null || npc.Travel.IsTraveling
            || string.IsNullOrWhiteSpace(npc.Travel.CurrentCityRuntimeId)
            || npc.CommercialKnowledge == null
            || !ReferenceEquals(npc.Definitions, graph.Definitions)
            || !DailyV1NpcDefinitionRules.HasJobType(npc.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue))
            return false;

        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
        {
            if (candidate != null && string.Equals(candidate.RuntimeId,
                    npc.Travel.CurrentCityRuntimeId, StringComparison.Ordinal))
            {
                city = candidate;
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, IReadOnlyList<DailyV1CommercialKnowledgeShareValue>>
        CapturePhaseSnapshots(
            DailyV1SelectedRuntimeOwnerGraph graph,
            IReadOnlyList<Merchant> merchants,
            long absoluteDay,
            EffectiveCommercialKnowledgeConfiguration configuration)
    {
        Dictionary<string, IReadOnlyList<DailyV1CommercialKnowledgeShareValue>> snapshots =
            new Dictionary<string, IReadOnlyList<DailyV1CommercialKnowledgeShareValue>>(
                StringComparer.Ordinal);

        foreach (Merchant merchant in merchants)
        {
            List<DailyV1CommercialKnowledgeShareValue> observations =
                new List<DailyV1CommercialKnowledgeShareValue>();
            foreach (DailyV1CommercialMarketObservation observation
                in merchant.Owner.CommercialKnowledge.Observations)
            {
                if (observation == null || observation.ItemDefinition == null
                    || !graph.Definitions.Items.TryGetValue(observation.ItemDefinitionId,
                        out DailyV1ItemDefinition admitted)
                    || !ReferenceEquals(admitted, observation.ItemDefinition)
                    || DailyV1CommercialKnowledgeFreshness.GetFreshness(
                        observation.ObservedDay, absoluteDay, configuration) <= 0f
                    || (observation.Source == DailyV1CommercialKnowledgeSource.SharedByNpc
                        && observation.ReceivedDay >= absoluteDay))
                    continue;

                observations.Add(DailyV1CommercialKnowledgeShareValue.Capture(observation));
            }

            foreach (DailyV1CommercialLiquidityObservation observation
                in merchant.Owner.CommercialKnowledge.LiquidityObservations)
            {
                if (observation == null
                    || DailyV1CommercialKnowledgeFreshness.GetFreshness(
                        observation.ObservedDay, absoluteDay, configuration) <= 0f
                    || (observation.Source == DailyV1CommercialKnowledgeSource.SharedByNpc
                        && observation.ReceivedDay >= absoluteDay))
                    continue;

                observations.Add(DailyV1CommercialKnowledgeShareValue.Capture(observation));
            }

            observations.Sort(CompareObservations);
            snapshots.Add(merchant.Owner.RuntimeId, observations.AsReadOnly());
        }

        return snapshots;
    }

    private static void ShareSnapshot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners sender,
        DailyV1NpcRuntimeOwners receiver,
        IReadOnlyList<DailyV1CommercialKnowledgeShareValue> observations,
        long absoluteDay,
        int maximumSuccessfulUpdates)
    {
        int sharedCount = 0;
        foreach (DailyV1CommercialKnowledgeShareValue value in observations)
        {
            if (!value.TryCreateShared(graph.Definitions, sender.RuntimeId, absoluteDay,
                    out DailyV1CommercialMarketObservation market,
                    out DailyV1CommercialLiquidityObservation liquidity))
                continue;

            DailyV1CommercialKnowledgeOwner knowledge = receiver.CommercialKnowledge;
            bool recorded = market != null
                ? knowledge.CanImproveWith(market) && knowledge.RecordObservation(market)
                : liquidity != null && knowledge.CanImproveWith(liquidity)
                    && knowledge.RecordLiquidityObservation(liquidity);
            if (!recorded) continue;
            sharedCount++;
            if (sharedCount >= maximumSuccessfulUpdates) return;
        }
    }

    private static int CompareMerchants(Merchant left, Merchant right)
    {
        int location = string.CompareOrdinal(left.City.LocationRuntimeId,
            right.City.LocationRuntimeId);
        return location != 0 ? location
            : string.CompareOrdinal(left.Owner.RuntimeId, right.Owner.RuntimeId);
    }

    private static int CompareObservations(
        DailyV1CommercialKnowledgeShareValue left,
        DailyV1CommercialKnowledgeShareValue right)
    {
        int day = right.ObservedDay.CompareTo(left.ObservedDay);
        if (day != 0) return day;
        int source = DailyV1CommercialKnowledgeOwner.GetSourcePriority(right.Source)
            .CompareTo(DailyV1CommercialKnowledgeOwner.GetSourcePriority(left.Source));
        if (source != 0) return source;
        int location = string.CompareOrdinal(left.LocationRuntimeId, right.LocationRuntimeId);
        if (location != 0) return location;
        return string.CompareOrdinal(SortKey(left), SortKey(right));
    }

    private static string SortKey(DailyV1CommercialKnowledgeShareValue value) =>
        value.IsMarketObservation ? "item:" + value.ItemDefinitionId : "liquidity";
}
