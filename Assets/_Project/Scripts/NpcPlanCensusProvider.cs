using System;
using System.Collections.Generic;

/// <summary>Exact per-rostered-NPC witnesses for embedded daily plan owners.</summary>
public static class NpcPlanCensusProvider
{
    public const string MerchantTradePlanSectionPrefix = "p12b.merchant-trade-plan/";
    public const string TravelPlanSectionPrefix = "p12b.npc-travel-plan/";
    public const int SchemaVersion = 1;

    public const int MerchantTradePlanKind = 0;
    public const int TravelPlanKind = 1;

    internal interface INpcPlanSectionCensusProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        NpcRuntime NpcOwner { get; }
        object PlanOwner { get; }
        int Kind { get; }
    }

    private sealed class SectionProvider : INpcPlanSectionCensusProvider
    {
        private readonly NpcRuntime npcOwner;
        private readonly object planOwner;
        private readonly int kind;
        private readonly string sectionId;

        public SectionProvider(NpcRuntime npcOwner, object planOwner, int kind, string sectionId)
        {
            this.npcOwner = npcOwner ?? throw new ArgumentNullException(nameof(npcOwner));
            this.planOwner = planOwner ?? throw new ArgumentNullException(nameof(planOwner));
            this.kind = kind;
            this.sectionId = sectionId;
        }

        public string RuntimeId => npcOwner.RuntimeId;
        public NpcRuntime NpcOwner => npcOwner;
        public object PlanOwner => planOwner;
        public int Kind => kind;

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            long revision = kind == MerchantTradePlanKind
                ? ((MerchantTradePlanRuntime)planOwner).Revision
                : ((NpcTravelPlanRuntime)planOwner).Revision;
            return new OwnerSectionCensusWitness(sectionId, SchemaVersion, planOwner, 1, revision);
        }
    }

    public static string SectionIdFor(int kind, string runtimeId)
    {
        if (kind != MerchantTradePlanKind && kind != TravelPlanKind)
            throw new ArgumentOutOfRangeException(nameof(kind));
        string prefix = kind == MerchantTradePlanKind
            ? MerchantTradePlanSectionPrefix
            : TravelPlanSectionPrefix;
        return prefix + runtimeId;
    }

    /// <summary>
    /// Creates two singleton sections per exact installed NPC, ordered by
    /// ordinal RuntimeId. Empty and populated plans both have cardinality one.
    /// </summary>
    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        IReadOnlyList<NpcRuntime> roster)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));

        List<NpcRuntime> ordered = new List<NpcRuntime>(roster.Count);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcRuntime npc in roster)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !runtimeIds.Add(npc.RuntimeId))
            {
                throw new InvalidOperationException(
                    "NPC plan census requires non-null roster actors with unique RuntimeIds.");
            }

            MerchantTradePlanRuntime merchantPlan = npc.ExistingMerchantTradePlan;
            NpcTravelPlanRuntime travelPlan = npc.ExistingTravelPlan;
            if (merchantPlan == null || travelPlan == null)
            {
                throw new InvalidOperationException(
                    "NPC plan census requires both embedded plan owners to be installed.");
            }

            ordered.Add(npc);
        }

        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        IOwnerSectionCensusProvider[] providers =
            new IOwnerSectionCensusProvider[ordered.Count * 2];
        int index = 0;
        foreach (NpcRuntime npc in ordered)
        {
            providers[index++] = new SectionProvider(
                npc,
                npc.ExistingMerchantTradePlan,
                MerchantTradePlanKind,
                SectionIdFor(MerchantTradePlanKind, npc.RuntimeId));
            providers[index++] = new SectionProvider(
                npc,
                npc.ExistingTravelPlan,
                TravelPlanKind,
                SectionIdFor(TravelPlanKind, npc.RuntimeId));
        }

        return Array.AsReadOnly(providers);
    }
}
