using System;
using System.Collections.Generic;

/// <summary>Passive per-NPC census witnesses for the selected P12 Knowledge owners.</summary>
public static class NpcKnowledgeCensusProvider
{
    public const int SchemaVersion = DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion;
    private static readonly string[] Prefixes =
    {
        DailyV1SelectedOwnerSectionCensusContract.NpcExplorableSiteKnowledgePrefix,
        DailyV1SelectedOwnerSectionCensusContract.NpcLocalTopologyPlacesPrefix,
        DailyV1SelectedOwnerSectionCensusContract.NpcLocalTopologyConnectionsPrefix,
        DailyV1SelectedOwnerSectionCensusContract.NpcAdventureOppositionPrefix,
        DailyV1SelectedOwnerSectionCensusContract.NpcAdventureNotableItemsPrefix,
        DailyV1SelectedOwnerSectionCensusContract.NpcAdventureCommonResourcesPrefix,
        DailyV1SelectedOwnerSectionCensusContract.NpcAdventureAccessPrefix,
        DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix,
        DailyV1SelectedOwnerSectionCensusContract.NpcCommercialLiquidityPrefix,
        DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix
    };

    internal static string SectionIdFor(int kind, string runtimeId) =>
        DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(kind, runtimeId);

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
        public object TypedOwner
        {
            get
            {
                DailyV1NpcRuntimeOwners selected = npc.SelectedRuntimeOwners;
                if (selected == null) return kind == 0 ? (object)explorable : kind <= 2 ? local : kind <= 6 ? adventure : commercial;
                return kind == 0 ? (object)selected.ExplorableSiteKnowledge
                    : kind <= 2 ? selected.LocalTopologyKnowledgeEmptyOwner
                    : kind <= 6 ? selected.AdventureSiteIntelKnowledge
                    : selected.CommercialKnowledge;
            }
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            int cardinality;
            long revision;
            DailyV1NpcRuntimeOwners selected = npc.SelectedRuntimeOwners;
            switch (kind)
            {
                case 0:
                    cardinality = selected == null ? explorable.Observations.Count : selected.ExplorableSiteKnowledge.Observations.Count;
                    revision = selected == null ? explorable.Revision : selected.ExplorableSiteKnowledge.Revision;
                    break;
                case 1:
                case 2:
                    if (selected == null)
                    {
                        cardinality = kind == 1 ? local.PlaceObservations.Count : local.ConnectionObservations.Count;
                        revision = local.Revision;
                    }
                    else
                    {
                        DailyV1NpcLocalTopologyKnowledgeEmptyOwner emptyOwner = selected.LocalTopologyKnowledgeEmptyOwner;
                        if (emptyOwner == null || !emptyOwner.MatchesProjection(
                                npc.RuntimeId, local.OwnerRuntimeId,
                                local.PlaceObservations.Count, local.ConnectionObservations.Count, local.Revision))
                            throw new InvalidOperationException(
                                "Selected Daily-v1 LocalTopology census requires the exact empty Core owner projection.");
                        cardinality = kind == 1
                            ? emptyOwner.PlaceObservationCount
                            : emptyOwner.ConnectionObservationCount;
                        revision = emptyOwner.Revision;
                    }
                    break;
                case 3:
                    cardinality = selected == null ? adventure.OppositionObservations.Count : selected.AdventureSiteIntelKnowledge.OppositionObservations.Count;
                    revision = selected == null ? adventure.Revision : selected.AdventureSiteIntelKnowledge.Revision;
                    break;
                case 4:
                    cardinality = selected == null ? adventure.NotableItemObservations.Count : selected.AdventureSiteIntelKnowledge.NotableItemObservations.Count;
                    revision = selected == null ? adventure.Revision : selected.AdventureSiteIntelKnowledge.Revision;
                    break;
                case 5:
                    cardinality = selected == null ? adventure.CommonResourceObservations.Count : selected.AdventureSiteIntelKnowledge.CommonResourceObservations.Count;
                    revision = selected == null ? adventure.Revision : selected.AdventureSiteIntelKnowledge.Revision;
                    break;
                case 6:
                    cardinality = selected == null ? adventure.AccessObservations.Count : selected.AdventureSiteIntelKnowledge.AccessObservations.Count;
                    revision = selected == null ? adventure.Revision : selected.AdventureSiteIntelKnowledge.Revision;
                    break;
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
            DailyV1NpcRuntimeOwners selected = npc.SelectedRuntimeOwners;
            if (explorable == null || local == null || adventure == null || commercial == null
                || !string.Equals(explorable.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(local.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(adventure.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                || (selected != null
                    && (!ReferenceEquals(explorable.SelectedOwner, selected.ExplorableSiteKnowledge)
                        || !ReferenceEquals(adventure.SelectedOwner, selected.AdventureSiteIntelKnowledge)
                        || !ReferenceEquals(commercial.SelectedOwner, selected.CommercialKnowledge)
                        || selected.LocalTopologyKnowledgeEmptyOwner == null
                        || !selected.LocalTopologyKnowledgeEmptyOwner.MatchesProjection(
                            npc.RuntimeId, local.OwnerRuntimeId,
                            local.PlaceObservations.Count, local.ConnectionObservations.Count, local.Revision))))
                throw new InvalidOperationException("Knowledge census requires existing exact NPC-owned Knowledge runtimes and their bound Core owners.");
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
