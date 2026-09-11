using System;
using System.Collections.Generic;

public sealed class CommercialKnowledgeSharingSystem
{
    private const int DefaultMaxSharedObservationsPerInteraction = 2;

    private readonly SimulationTime simulationTime;
    private readonly int maxSharedObservationsPerInteraction;

    public CommercialKnowledgeSharingSystem(SimulationTime simulationTime, CommercialKnowledgeSettings settings)
    {
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        int configuredLimit = settings != null ? settings.maxSharedObservationsPerInteraction : DefaultMaxSharedObservationsPerInteraction;
        maxSharedObservationsPerInteraction = Math.Max(1, configuredLimit);
    }

    public void ShareAmongPresentMerchants(IReadOnlyList<NpcRuntime> npcRuntimes)
    {
        if (npcRuntimes == null)
        {
            return;
        }

        List<NpcRuntime> merchants = new List<NpcRuntime>();

        foreach (NpcRuntime npcRuntime in npcRuntimes)
        {
            if (IsEligibleMerchant(npcRuntime) == true)
            {
                merchants.Add(npcRuntime);
            }
        }

        merchants.Sort(CompareMerchants);
        Dictionary<string, IReadOnlyList<CommercialMarketObservation>> phaseSnapshots = CapturePhaseSnapshots(merchants);

        int groupStart = 0;

        while (groupStart < merchants.Count)
        {
            CityRuntime city = merchants[groupStart].CurrentCity;
            int groupEnd = groupStart + 1;

            while (groupEnd < merchants.Count && merchants[groupEnd].CurrentCity == city)
            {
                groupEnd++;
            }

            int groupCount = groupEnd - groupStart;
            // Day rotation keeps pairing deterministic without permanently excluding the last merchant in odd groups.
            int rotation = groupCount > 0 ? (int)(simulationTime.AbsoluteDay % groupCount) : 0;

            for (int pairOffset = 0; pairOffset + 1 < groupCount; pairOffset += 2)
            {
                int firstIndex = groupStart + (rotation + pairOffset) % groupCount;
                int secondIndex = groupStart + (rotation + pairOffset + 1) % groupCount;
                NpcRuntime first = merchants[firstIndex];
                NpcRuntime second = merchants[secondIndex];
                ShareSnapshot(first, second, phaseSnapshots[first.RuntimeId]);
                ShareSnapshot(second, first, phaseSnapshots[second.RuntimeId]);
            }

            groupStart = groupEnd;
        }
    }

    private Dictionary<string, IReadOnlyList<CommercialMarketObservation>> CapturePhaseSnapshots(List<NpcRuntime> merchants)
    {
        Dictionary<string, IReadOnlyList<CommercialMarketObservation>> snapshots = new Dictionary<string, IReadOnlyList<CommercialMarketObservation>>(StringComparer.Ordinal);

        foreach (NpcRuntime merchant in merchants)
        {
            List<CommercialMarketObservation> observations = new List<CommercialMarketObservation>();

            foreach (CommercialMarketObservation observation in merchant.CommercialKnowledge.Observations)
            {
                if (observation == null
                    || observation.ItemDefinition == null
                    || (observation.Source == CommercialKnowledgeSource.SharedByNpc
                        && observation.ReceivedDay >= simulationTime.AbsoluteDay))
                {
                    continue;
                }

                observations.Add(observation);
            }

            observations.Sort(CompareObservations);
            snapshots.Add(merchant.RuntimeId, observations.AsReadOnly());
        }

        return snapshots;
    }

    private void ShareSnapshot(NpcRuntime sender, NpcRuntime receiver, IReadOnlyList<CommercialMarketObservation> observations)
    {
        int sharedCount = 0;

        foreach (CommercialMarketObservation observation in observations)
        {
            CommercialMarketObservation sharedObservation = new CommercialMarketObservation(
                observation.LocationRuntimeId,
                observation.ItemDefinition,
                observation.ObservedPrice,
                observation.ObservedStock,
                observation.ObservedDay,
                simulationTime.AbsoluteDay,
                CommercialKnowledgeSource.SharedByNpc,
                sender.RuntimeId);

            if (receiver.CommercialKnowledge.CanImproveWith(sharedObservation) == false)
            {
                continue;
            }

            if (receiver.CommercialKnowledge.RecordObservation(sharedObservation) == true)
            {
                sharedCount++;
            }

            if (sharedCount >= maxSharedObservationsPerInteraction)
            {
                return;
            }
        }
    }

    private static int CompareMerchants(NpcRuntime left, NpcRuntime right)
    {
        string leftLocationId = left.CurrentCity?.Location?.RuntimeId ?? string.Empty;
        string rightLocationId = right.CurrentCity?.Location?.RuntimeId ?? string.Empty;
        int locationComparison = string.CompareOrdinal(leftLocationId, rightLocationId);
        return locationComparison != 0 ? locationComparison : string.CompareOrdinal(left.RuntimeId, right.RuntimeId);
    }

    private static int CompareObservations(CommercialMarketObservation left, CommercialMarketObservation right)
    {
        int dayComparison = right.ObservedDay.CompareTo(left.ObservedDay);

        if (dayComparison != 0)
        {
            return dayComparison;
        }

        int sourceComparison = CommercialKnowledgeRuntime.GetSourcePriority(right.Source)
            .CompareTo(CommercialKnowledgeRuntime.GetSourcePriority(left.Source));

        if (sourceComparison != 0)
        {
            return sourceComparison;
        }

        int locationComparison = string.CompareOrdinal(left.LocationRuntimeId, right.LocationRuntimeId);
        return locationComparison != 0
            ? locationComparison
            : string.CompareOrdinal(left.ItemDefinitionId, right.ItemDefinitionId);
    }

    private static bool IsEligibleMerchant(NpcRuntime npcRuntime)
    {
        return npcRuntime != null
            && npcRuntime.IsTraveling == false
            && npcRuntime.CurrentCity != null
            && npcRuntime.NpcData != null
            && npcRuntime.NpcData.job != null
            && npcRuntime.NpcData.job.jobType == NpcJobType.Merchant;
    }
}
