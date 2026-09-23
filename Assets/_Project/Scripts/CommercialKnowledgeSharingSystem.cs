using System;
using System.Collections.Generic;

public sealed class CommercialKnowledgeSharingSystem : IAuthoritativeMutationGuardBindable
{
    private readonly SimulationTime simulationTime;
    private readonly int maxSharedObservationsPerInteraction;
    private readonly CommercialKnowledgePolicy knowledgePolicy;
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();

    public CommercialKnowledgeSharingSystem(
        SimulationTime simulationTime,
        EffectiveCommercialKnowledgeConfiguration configuration)
    {
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        knowledgePolicy = new CommercialKnowledgePolicy(configuration);
        maxSharedObservationsPerInteraction = configuration != null
            ? Math.Max(1, configuration.MaxSharedObservationsPerInteraction)
            : 2;
    }

    public void ShareAmongPresentMerchants(IReadOnlyList<NpcRuntime> npcRuntimes)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            throw new InvalidOperationException("A faulted SimulationRuntime cannot share commercial knowledge.");
        }

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
        Dictionary<string, IReadOnlyList<ShareableObservation>> phaseSnapshots = CapturePhaseSnapshots(merchants);

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

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard) && simulationTime.CanBindMutationGuard(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        if (!CanBindMutationGuard(guard) || !mutationGuardBinding.TryBindTo(guard))
        {
            return false;
        }

        return simulationTime.TryBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }

    private Dictionary<string, IReadOnlyList<ShareableObservation>> CapturePhaseSnapshots(List<NpcRuntime> merchants)
    {
        Dictionary<string, IReadOnlyList<ShareableObservation>> snapshots = new Dictionary<string, IReadOnlyList<ShareableObservation>>(StringComparer.Ordinal);

        foreach (NpcRuntime merchant in merchants)
        {
            List<ShareableObservation> observations = new List<ShareableObservation>();

            foreach (CommercialMarketObservation observation in merchant.CommercialKnowledge.Observations)
            {
                if (observation == null
                    || observation.ItemDefinition == null
                    || knowledgePolicy.GetFreshness(observation, simulationTime.AbsoluteDay) <= 0f
                    || (observation.Source == CommercialKnowledgeSource.SharedByNpc
                        && observation.ReceivedDay >= simulationTime.AbsoluteDay))
                {
                    continue;
                }

                observations.Add(new ShareableObservation(observation));
            }

            foreach (CommercialLiquidityObservation observation in merchant.CommercialKnowledge.LiquidityObservations)
            {
                if (observation == null
                    || knowledgePolicy.GetFreshness(observation, simulationTime.AbsoluteDay) <= 0f
                    || (observation.Source == CommercialKnowledgeSource.SharedByNpc
                        && observation.ReceivedDay >= simulationTime.AbsoluteDay))
                {
                    continue;
                }

                observations.Add(new ShareableObservation(observation));
            }

            observations.Sort(CompareObservations);
            snapshots.Add(merchant.RuntimeId, observations.AsReadOnly());
        }

        return snapshots;
    }

    private void ShareSnapshot(NpcRuntime sender, NpcRuntime receiver, IReadOnlyList<ShareableObservation> observations)
    {
        int sharedCount = 0;

        foreach (ShareableObservation observation in observations)
        {
            bool recorded = observation.MarketObservation != null
                ? ShareMarketObservation(sender, receiver, observation.MarketObservation)
                : ShareLiquidityObservation(sender, receiver, observation.LiquidityObservation);

            if (recorded == true)
            {
                sharedCount++;
            }

            if (sharedCount >= maxSharedObservationsPerInteraction)
            {
                return;
            }
        }
    }

    private bool ShareMarketObservation(NpcRuntime sender, NpcRuntime receiver, CommercialMarketObservation observation)
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

        return receiver.CommercialKnowledge.CanImproveWith(sharedObservation)
            && receiver.CommercialKnowledge.RecordObservation(sharedObservation);
    }

    private bool ShareLiquidityObservation(NpcRuntime sender, NpcRuntime receiver, CommercialLiquidityObservation observation)
    {
        CommercialLiquidityObservation sharedObservation = new CommercialLiquidityObservation(
            observation.LocationRuntimeId,
            observation.LiquidityMode,
            observation.ObservedPurchasingPower,
            observation.ObservedDay,
            simulationTime.AbsoluteDay,
            CommercialKnowledgeSource.SharedByNpc,
            sender.RuntimeId);

        return receiver.CommercialKnowledge.CanImproveWith(sharedObservation)
            && receiver.CommercialKnowledge.RecordLiquidityObservation(sharedObservation);
    }

    private static int CompareMerchants(NpcRuntime left, NpcRuntime right)
    {
        string leftLocationId = left.CurrentCity?.Location?.RuntimeId ?? string.Empty;
        string rightLocationId = right.CurrentCity?.Location?.RuntimeId ?? string.Empty;
        int locationComparison = string.CompareOrdinal(leftLocationId, rightLocationId);
        return locationComparison != 0 ? locationComparison : string.CompareOrdinal(left.RuntimeId, right.RuntimeId);
    }

    private static int CompareObservations(ShareableObservation left, ShareableObservation right)
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
        if (locationComparison != 0)
        {
            return locationComparison;
        }

        return string.CompareOrdinal(left.SortKey, right.SortKey);
    }

    private static bool IsEligibleMerchant(NpcRuntime npcRuntime)
    {
        return npcRuntime != null
            && npcRuntime.IsAlive == true
            && npcRuntime.IsTraveling == false
            && npcRuntime.CurrentCity != null
            && npcRuntime.NpcData != null
            && npcRuntime.NpcData.job != null
            && npcRuntime.NpcData.job.jobType == NpcJobType.Merchant;
    }

    private sealed class ShareableObservation
    {
        public CommercialMarketObservation MarketObservation { get; }
        public CommercialLiquidityObservation LiquidityObservation { get; }
        public long ObservedDay => MarketObservation != null ? MarketObservation.ObservedDay : LiquidityObservation.ObservedDay;
        public CommercialKnowledgeSource Source => MarketObservation != null ? MarketObservation.Source : LiquidityObservation.Source;
        public string LocationRuntimeId => MarketObservation != null ? MarketObservation.LocationRuntimeId : LiquidityObservation.LocationRuntimeId;
        public string SortKey => MarketObservation != null ? "item:" + MarketObservation.ItemDefinitionId : "liquidity";

        public ShareableObservation(CommercialMarketObservation observation)
        {
            MarketObservation = observation;
        }

        public ShareableObservation(CommercialLiquidityObservation observation)
        {
            LiquidityObservation = observation;
        }
    }
}
