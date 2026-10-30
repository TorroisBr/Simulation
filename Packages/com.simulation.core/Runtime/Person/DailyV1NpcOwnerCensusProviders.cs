using System;
using System.Collections.Generic;

/// <summary>
/// Creates selected-profile Inventory, MoneyAccount, and SpatialKnowledge
/// census witnesses directly from the exact Core NPC owners. Unity projections
/// may alias these objects, but they do not select or read persisted owners.
/// </summary>
internal static class DailyV1NpcOwnerCensusProviders
{
    internal interface IInventoryProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        DailyV1NpcRuntimeOwners NpcOwner { get; }
        DailyV1InventoryOwner InventoryOwner { get; }
    }

    internal interface IMoneyAccountProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        DailyV1NpcRuntimeOwners NpcOwner { get; }
        MoneyAccountRuntime MoneyAccountOwner { get; }
    }

    internal interface ISpatialKnowledgeProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        DailyV1NpcRuntimeOwners NpcOwner { get; }
        SpatialKnowledgeRuntime SpatialKnowledgeOwner { get; }
    }

    private enum SpatialKnowledgeSection
    {
        Locations,
        Routes
    }

    private sealed class SpatialKnowledgeSectionProvider : ISpatialKnowledgeProvider
    {
        private readonly SpatialKnowledgeSection section;
        private readonly string sectionId;

        internal SpatialKnowledgeSectionProvider(
            DailyV1NpcRuntimeOwners npcOwner,
            SpatialKnowledgeRuntime owner,
            SpatialKnowledgeSection section,
            string sectionId)
        {
            NpcOwner = npcOwner;
            SpatialKnowledgeOwner = owner;
            this.section = section;
            this.sectionId = sectionId;
        }

        public string RuntimeId => NpcOwner.RuntimeId;
        public DailyV1NpcRuntimeOwners NpcOwner { get; }
        public SpatialKnowledgeRuntime SpatialKnowledgeOwner { get; }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            int cardinality = section == SpatialKnowledgeSection.Locations
                ? SpatialKnowledgeOwner.KnownLocationCount
                : SpatialKnowledgeOwner.KnownRouteCount;
            return new OwnerSectionCensusWitness(
                sectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                SpatialKnowledgeOwner,
                cardinality,
                SpatialKnowledgeOwner.Revision);
        }
    }

    private sealed class InventorySectionProvider : IInventoryProvider
    {
        private readonly string sectionId;

        internal InventorySectionProvider(DailyV1NpcRuntimeOwners owner)
        {
            NpcOwner = owner;
            sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + owner.RuntimeId;
        }

        public string RuntimeId => NpcOwner.RuntimeId;
        public DailyV1NpcRuntimeOwners NpcOwner { get; }
        public DailyV1InventoryOwner InventoryOwner => NpcOwner.Inventory;

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (!InventoryOwner.TryGetCensusCardinality(out int cardinality))
                throw new InvalidOperationException("The Core Inventory owner cannot provide its census cardinality.");
            return new OwnerSectionCensusWitness(
                sectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                InventoryOwner,
                cardinality,
                InventoryOwner.Revision);
        }
    }

    private sealed class MoneyAccountSectionProvider : IMoneyAccountProvider
    {
        private readonly string sectionId;

        internal MoneyAccountSectionProvider(DailyV1NpcRuntimeOwners owner)
        {
            NpcOwner = owner;
            sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + owner.RuntimeId;
        }

        public string RuntimeId => NpcOwner.RuntimeId;
        public DailyV1NpcRuntimeOwners NpcOwner { get; }
        public MoneyAccountRuntime MoneyAccountOwner => NpcOwner.MoneyAccount;

        public OwnerSectionCensusWitness GetCurrentCensus() => new OwnerSectionCensusWitness(
            sectionId,
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            MoneyAccountOwner,
            1,
            MoneyAccountOwner.Revision);
    }

    internal static IReadOnlyList<IOwnerSectionCensusProvider> CreateInventoryProviders(
        IReadOnlyList<DailyV1NpcRuntimeOwners> roster)
    {
        ValidateRoster(roster);
        List<DailyV1NpcRuntimeOwners> ordered = OrderByRuntimeId(roster);
        HashSet<DailyV1InventoryOwner> owners = new HashSet<DailyV1InventoryOwner>();
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
        {
            DailyV1NpcRuntimeOwners npc = ordered[i];
            if (npc.Inventory == null || !ReferenceEquals(npc.Inventory.Definitions, npc.Definitions)
                || !owners.Add(npc.Inventory))
                throw new InvalidOperationException("Selected Inventory census requires one distinct admitted Core owner per NPC.");
            providers[i] = new InventorySectionProvider(npc);
        }
        return Array.AsReadOnly(providers);
    }

    internal static IReadOnlyList<IOwnerSectionCensusProvider> CreateMoneyAccountProviders(
        IReadOnlyList<DailyV1NpcRuntimeOwners> roster)
    {
        ValidateRoster(roster);
        List<DailyV1NpcRuntimeOwners> ordered = OrderByRuntimeId(roster);
        HashSet<MoneyAccountRuntime> owners = new HashSet<MoneyAccountRuntime>();
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
        {
            DailyV1NpcRuntimeOwners npc = ordered[i];
            if (npc.MoneyAccount == null || !owners.Add(npc.MoneyAccount))
                throw new InvalidOperationException("Selected MoneyAccount census requires one distinct Core owner per NPC.");
            providers[i] = new MoneyAccountSectionProvider(npc);
        }
        return Array.AsReadOnly(providers);
    }

    internal static IReadOnlyList<IOwnerSectionCensusProvider> CreateSpatialKnowledgeProviders(
        IReadOnlyList<DailyV1NpcRuntimeOwners> roster)
    {
        ValidateRoster(roster);
        List<DailyV1NpcRuntimeOwners> ordered = OrderByRuntimeId(roster);
        HashSet<SpatialKnowledgeRuntime> owners = new HashSet<SpatialKnowledgeRuntime>();
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count * 2];
        int providerIndex = 0;
        foreach (DailyV1NpcRuntimeOwners npc in ordered)
        {
            SpatialKnowledgeRuntime owner = npc.SpatialKnowledge;
            if (owner == null
                || !string.Equals(owner.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                || !owners.Add(owner))
                throw new InvalidOperationException(
                    "Selected SpatialKnowledge census requires one distinct owner bound to each NPC RuntimeId.");

            providers[providerIndex++] = new SpatialKnowledgeSectionProvider(
                npc,
                owner,
                SpatialKnowledgeSection.Locations,
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + npc.RuntimeId);
            providers[providerIndex++] = new SpatialKnowledgeSectionProvider(
                npc,
                owner,
                SpatialKnowledgeSection.Routes,
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + npc.RuntimeId);
        }
        return Array.AsReadOnly(providers);
    }

    private static void ValidateRoster(IReadOnlyList<DailyV1NpcRuntimeOwners> roster)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in roster)
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) || !runtimeIds.Add(npc.RuntimeId))
                throw new InvalidOperationException("Selected owner census requires unique NPC runtime identities.");
    }

    private static List<DailyV1NpcRuntimeOwners> OrderByRuntimeId(
        IReadOnlyList<DailyV1NpcRuntimeOwners> roster)
    {
        List<DailyV1NpcRuntimeOwners> ordered = new List<DailyV1NpcRuntimeOwners>(roster);
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        return ordered;
    }
}
