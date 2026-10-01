using System;
using System.Collections.Generic;

/// <summary>Passive per-NPC census witnesses for the selected P12 Knowledge owners.</summary>
public static class NpcKnowledgeCensusProvider
{
    public const int SchemaVersion = 1;
    private static readonly string[] Prefixes =
    {
        "p12f.explorable-site-knowledge/",
        "p12f.local-topology-knowledge.places/",
        "p12f.local-topology-knowledge.connections/",
        "p12f.adventure-intel.opposition/",
        "p12f.adventure-intel.notable-items/",
        "p12f.adventure-intel.common-resources/",
        "p12f.adventure-intel.access/",
        "p12f.commercial-knowledge.markets/",
        "p12f.commercial-knowledge.liquidity/",
        "p12f.commercial-knowledge.share-receipts/"
    };

    internal static string SectionIdFor(int kind, string runtimeId) => Prefixes[kind] + runtimeId;

    internal interface INpcKnowledgeSectionCensusProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        NpcRuntime NpcOwner { get; }
        int SectionKind { get; }
        object TypedOwner { get; }
    }

    private sealed class SectionProvider : INpcKnowledgeSectionCensusProvider
    {
        private readonly NpcRuntime npc;
        private readonly ExplorableSiteKnowledgeRuntime explorable;
        private readonly LocalTopologyKnowledgeRuntime local;
        private readonly AdventureSiteIntelKnowledgeRuntime adventure;
        private readonly CommercialKnowledgeRuntime commercial;
        private readonly int kind;
        private readonly string sectionId;

        public SectionProvider(NpcRuntime npc, ExplorableSiteKnowledgeRuntime explorable,
            LocalTopologyKnowledgeRuntime local, AdventureSiteIntelKnowledgeRuntime adventure,
            CommercialKnowledgeRuntime commercial, int kind)
        {
            this.npc = npc; this.explorable = explorable; this.local = local;
            this.adventure = adventure; this.commercial = commercial; this.kind = kind;
            sectionId = Prefixes[kind] + npc.RuntimeId;
        }

        public string RuntimeId => npc.RuntimeId;
        public NpcRuntime NpcOwner => npc;
        public int SectionKind => kind;
        public object TypedOwner => kind == 0 ? (object)explorable : kind <= 2 ? local : kind <= 6 ? adventure : commercial;

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            int cardinality;
            long revision;
            switch (kind)
            {
                case 0: cardinality = explorable.Observations.Count; revision = explorable.Revision; break;
                case 1: cardinality = local.PlaceObservations.Count; revision = local.Revision; break;
                case 2: cardinality = local.ConnectionObservations.Count; revision = local.Revision; break;
                case 3: cardinality = adventure.OppositionObservations.Count; revision = adventure.Revision; break;
                case 4: cardinality = adventure.NotableItemObservations.Count; revision = adventure.Revision; break;
                case 5: cardinality = adventure.CommonResourceObservations.Count; revision = adventure.Revision; break;
                case 6: cardinality = adventure.AccessObservations.Count; revision = adventure.Revision; break;
                default:
                    if (!commercial.TryReadCensus(out int markets, out int liquidity, out int receipts, out revision))
                        throw new InvalidOperationException("Commercial Knowledge census requires all backing lists to exist.");
                    cardinality = kind == 7 ? markets : kind == 8 ? liquidity : receipts;
                    break;
            }
            return new OwnerSectionCensusWitness(sectionId, SchemaVersion, TypedOwner, cardinality, revision);
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(IReadOnlyList<NpcRuntime> roster)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));
        List<NpcRuntime> ordered = new List<NpcRuntime>(roster.Count);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcRuntime npc in roster)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) || !ids.Add(npc.RuntimeId))
                throw new InvalidOperationException("Knowledge census requires non-null NPCs with unique RuntimeIds.");
            ExplorableSiteKnowledgeRuntime explorable = npc.ExistingExplorableSiteKnowledge;
            LocalTopologyKnowledgeRuntime local = npc.ExistingLocalTopologyKnowledge;
            AdventureSiteIntelKnowledgeRuntime adventure = npc.ExistingAdventureSiteIntelKnowledge;
            CommercialKnowledgeRuntime commercial = npc.ExistingCommercialKnowledge;
            if (explorable == null || local == null || adventure == null || commercial == null
                || !string.Equals(explorable.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(local.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(adventure.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
                throw new InvalidOperationException("Knowledge census requires existing exact NPC-owned Knowledge runtimes.");
            ordered.Add(npc);
        }
        ordered.Sort((a, b) => StringComparer.Ordinal.Compare(a.RuntimeId, b.RuntimeId));
        IOwnerSectionCensusProvider[] result = new IOwnerSectionCensusProvider[ordered.Count * 10];
        int index = 0;
        foreach (NpcRuntime npc in ordered)
        {
            for (int kind = 0; kind < Prefixes.Length; kind++)
                result[index++] = new SectionProvider(npc, npc.ExistingExplorableSiteKnowledge,
                    npc.ExistingLocalTopologyKnowledge, npc.ExistingAdventureSiteIntelKnowledge,
                    npc.ExistingCommercialKnowledge, kind);
        }
        return Array.AsReadOnly(result);
    }
}
