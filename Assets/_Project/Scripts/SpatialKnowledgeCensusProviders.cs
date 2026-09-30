using System;
using System.Collections.Generic;

/// <summary>Passive per-NPC witnesses for selected-profile spatial Knowledge counts.</summary>
public static class SpatialKnowledgeCensusProvider
{
    public const string LocationsSectionPrefix = "p12f.spatial-knowledge.locations/";
    public const string RoutesSectionPrefix = "p12f.spatial-knowledge.routes/";
    public const int SchemaVersion = 1;

    private enum Section
    {
        Locations,
        Routes
    }

    private sealed class SectionProvider : IOwnerSectionCensusProvider
    {
        private readonly SpatialKnowledgeRuntime owner;
        private readonly Section section;
        private readonly string sectionId;

        public SectionProvider(SpatialKnowledgeRuntime owner, Section section, string sectionId)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.section = section;
            this.sectionId = sectionId;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            int cardinality = section == Section.Locations
                ? owner.KnownLocationCount
                : owner.KnownRouteCount;
            return new OwnerSectionCensusWitness(sectionId, SchemaVersion, owner, cardinality, owner.Revision);
        }
    }

    /// <summary>
    /// Creates two passive section providers per installed NPC, ordered by
    /// RuntimeId. Each pair names the exact same SpatialKnowledge owner and
    /// revision while reporting separate location and route cardinalities.
    /// </summary>
    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        IReadOnlyList<NpcRuntime> npcRuntimes)
    {
        if (npcRuntimes == null) throw new ArgumentNullException(nameof(npcRuntimes));

        List<NpcRuntime> orderedNpcs = new List<NpcRuntime>(npcRuntimes.Count);
        for (int i = 0; i < npcRuntimes.Count; i++)
        {
            NpcRuntime npc = npcRuntimes[i];
            if (npc == null)
            {
                throw new ArgumentException("Spatial Knowledge census requires every installed NPC.", nameof(npcRuntimes));
            }

            if (string.IsNullOrWhiteSpace(npc.RuntimeId))
            {
                throw new InvalidOperationException("Spatial Knowledge census requires each NPC RuntimeId.");
            }

            SpatialKnowledgeRuntime owner = npc.SpatialKnowledge;
            if (owner == null || !string.Equals(owner.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A SpatialKnowledge owner must use the RuntimeId of its owning NPC.");
            }

            orderedNpcs.Add(npc);
        }

        orderedNpcs.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        IOwnerSectionCensusProvider[] providers =
            new IOwnerSectionCensusProvider[orderedNpcs.Count * 2];
        string previousRuntimeId = null;
        int providerIndex = 0;
        foreach (NpcRuntime npc in orderedNpcs)
        {
            if (string.Equals(previousRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Spatial Knowledge census requires unique NPC RuntimeIds.",
                    nameof(npcRuntimes));
            }

            SpatialKnowledgeRuntime owner = npc.SpatialKnowledge;
            providers[providerIndex++] = new SectionProvider(
                owner,
                Section.Locations,
                LocationsSectionPrefix + npc.RuntimeId);
            providers[providerIndex++] = new SectionProvider(
                owner,
                Section.Routes,
                RoutesSectionPrefix + npc.RuntimeId);
            previousRuntimeId = npc.RuntimeId;
        }

        return Array.AsReadOnly(providers);
    }
}
