using System;
using System.Collections.Generic;

/// <summary>Passive per-NPC witnesses for installed InventoryRuntime owners.</summary>
public static class NpcInventoryCensusProvider
{
    public const string SectionPrefix = "p12f.inventory/";
    public const int SchemaVersion = 1;

    internal interface INpcInventorySectionCensusProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        NpcRuntime NpcOwner { get; }
        InventoryRuntime InventoryOwner { get; }
    }

    private sealed class SectionProvider : INpcInventorySectionCensusProvider
    {
        private readonly NpcRuntime npc;
        private readonly InventoryRuntime owner;
        private readonly string sectionId;

        public SectionProvider(NpcRuntime npc, InventoryRuntime owner)
        {
            this.npc = npc;
            this.owner = owner;
            sectionId = SectionPrefix + npc.RuntimeId;
        }

        public string RuntimeId => npc.RuntimeId;
        public NpcRuntime NpcOwner => npc;
        public InventoryRuntime InventoryOwner => owner;
        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (!owner.TryGetCensusCardinality(out int cardinality))
                throw new InvalidOperationException("Inventory census cannot materialize missing item storage.");
            return new OwnerSectionCensusWitness(sectionId, SchemaVersion, owner, cardinality, owner.Revision);
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(IReadOnlyList<NpcRuntime> roster)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));
        List<NpcRuntime> ordered = new List<NpcRuntime>(roster.Count);
        foreach (NpcRuntime npc in roster)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId))
                throw new InvalidOperationException("Inventory census requires installed NPCs with valid RuntimeIds.");
            InventoryRuntime owner = npc.ExistingInventory;
            if (owner == null) throw new InvalidOperationException("Inventory census requires an existing inventory owner.");
            ordered.Add(npc);
        }
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        string previous = null;
        for (int i = 0; i < ordered.Count; i++)
        {
            NpcRuntime npc = ordered[i];
            if (string.Equals(previous, npc.RuntimeId, StringComparison.Ordinal))
                throw new InvalidOperationException("Inventory census requires unique NPC RuntimeIds.");
            providers[i] = new SectionProvider(npc, npc.ExistingInventory);
            previous = npc.RuntimeId;
        }
        return Array.AsReadOnly(providers);
    }
}
