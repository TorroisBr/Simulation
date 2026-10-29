using System;
using System.Collections.Generic;

/// <summary>Passive per-NPC witnesses for installed InventoryRuntime owners.</summary>
public static class NpcInventoryCensusProvider
{
    public const string SectionPrefix = DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix;
    public const int SchemaVersion = DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion;

    internal interface INpcInventorySectionCensusProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        NpcRuntime NpcOwner { get; }
        InventoryRuntime InventoryOwner { get; }
        bool UsesSelectedCoreOwner { get; }
    }

    private sealed class SectionProvider : INpcInventorySectionCensusProvider
    {
        private readonly NpcRuntime npc;
        private readonly InventoryRuntime owner;
        private readonly IOwnerSectionCensusProvider selectedCoreOwnerProvider;
        private readonly string sectionId;

        public SectionProvider(
            NpcRuntime npc,
            InventoryRuntime owner,
            IOwnerSectionCensusProvider selectedCoreOwnerProvider = null)
        {
            this.npc = npc;
            this.owner = owner;
            this.selectedCoreOwnerProvider = selectedCoreOwnerProvider;
            sectionId = SectionPrefix + npc.RuntimeId;
        }

        public string RuntimeId => npc.RuntimeId;
        public NpcRuntime NpcOwner => npc;
        public InventoryRuntime InventoryOwner => owner;
        public bool UsesSelectedCoreOwner => selectedCoreOwnerProvider != null;
        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (selectedCoreOwnerProvider != null)
                return selectedCoreOwnerProvider.GetCurrentCensus();
            if (!owner.TryGetCensusCardinality(out int cardinality))
                throw new InvalidOperationException("Inventory census cannot materialize missing item storage.");
            return new OwnerSectionCensusWitness(sectionId, SchemaVersion, owner, cardinality, owner.Revision);
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(IReadOnlyList<NpcRuntime> roster)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));
        List<NpcRuntime> ordered = new List<NpcRuntime>(roster.Count);
        bool allSelected = true;
        foreach (NpcRuntime npc in roster)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId))
                throw new InvalidOperationException("Inventory census requires installed NPCs with valid RuntimeIds.");
            InventoryRuntime owner = npc.ExistingInventory;
            if (owner == null) throw new InvalidOperationException("Inventory census requires an existing inventory owner.");
            allSelected &= npc.SelectedRuntimeOwners != null;
            ordered.Add(npc);
        }
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        Dictionary<string, DailyV1NpcOwnerCensusProviders.IInventoryProvider> selectedProviders = null;
        if (allSelected)
        {
            DailyV1NpcRuntimeOwners[] selectedOwners = new DailyV1NpcRuntimeOwners[ordered.Count];
            for (int i = 0; i < ordered.Count; i++) selectedOwners[i] = ordered[i].SelectedRuntimeOwners;
            IReadOnlyList<IOwnerSectionCensusProvider> coreProviders =
                DailyV1NpcOwnerCensusProviders.CreateInventoryProviders(selectedOwners);
            selectedProviders = new Dictionary<string, DailyV1NpcOwnerCensusProviders.IInventoryProvider>(StringComparer.Ordinal);
            foreach (IOwnerSectionCensusProvider coreProvider in coreProviders)
            {
                if (!(coreProvider is DailyV1NpcOwnerCensusProviders.IInventoryProvider selectedProvider)
                    || !selectedProviders.TryAdd(selectedProvider.RuntimeId, selectedProvider))
                    throw new InvalidOperationException("Selected Inventory census providers must have unique Core runtime identities.");
            }
        }
        string previous = null;
        for (int i = 0; i < ordered.Count; i++)
        {
            NpcRuntime npc = ordered[i];
            if (string.Equals(previous, npc.RuntimeId, StringComparison.Ordinal))
                throw new InvalidOperationException("Inventory census requires unique NPC RuntimeIds.");
            IOwnerSectionCensusProvider selectedProvider = null;
            if (allSelected)
            {
                if (!selectedProviders.TryGetValue(npc.RuntimeId, out DailyV1NpcOwnerCensusProviders.IInventoryProvider coreProvider)
                    || !ReferenceEquals(coreProvider.NpcOwner, npc.SelectedRuntimeOwners)
                    || !ReferenceEquals(coreProvider.InventoryOwner, npc.ExistingInventory.SelectedDailyV1Owner))
                    throw new InvalidOperationException("Selected Inventory census must use the exact Core owner bound to the Unity projection.");
                selectedProvider = coreProvider;
            }
            providers[i] = new SectionProvider(npc, npc.ExistingInventory, selectedProvider);
            previous = npc.RuntimeId;
        }
        return Array.AsReadOnly(providers);
    }
}
