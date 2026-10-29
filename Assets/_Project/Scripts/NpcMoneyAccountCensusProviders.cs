using System;
using System.Collections.Generic;

/// <summary>Passive per-NPC witnesses for installed MoneyAccountRuntime owners.</summary>
public static class NpcMoneyAccountCensusProvider
{
    public const string SectionPrefix = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix;
    public const int SchemaVersion = DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion;

    internal interface INpcMoneyAccountSectionCensusProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        NpcRuntime NpcOwner { get; }
        MoneyAccountRuntime MoneyAccountOwner { get; }
    }

    private sealed class SectionProvider : INpcMoneyAccountSectionCensusProvider
    {
        private readonly NpcRuntime npc;
        private readonly MoneyAccountRuntime owner;
        private readonly IOwnerSectionCensusProvider selectedCoreOwnerProvider;
        private readonly string sectionId;

        public SectionProvider(
            NpcRuntime npc,
            MoneyAccountRuntime owner,
            IOwnerSectionCensusProvider selectedCoreOwnerProvider = null)
        {
            this.npc = npc;
            this.owner = owner;
            this.selectedCoreOwnerProvider = selectedCoreOwnerProvider;
            sectionId = SectionPrefix + npc.RuntimeId;
        }

        public string RuntimeId => npc.RuntimeId;
        public NpcRuntime NpcOwner => npc;
        public MoneyAccountRuntime MoneyAccountOwner => owner;

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (selectedCoreOwnerProvider != null)
                return selectedCoreOwnerProvider.GetCurrentCensus();
            if (owner == null)
                throw new InvalidOperationException("MoneyAccount census requires an existing account owner.");
            return new OwnerSectionCensusWitness(sectionId, SchemaVersion, owner, 1, owner.Revision);
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(IReadOnlyList<NpcRuntime> roster)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));

        List<NpcRuntime> ordered = new List<NpcRuntime>(roster.Count);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<MoneyAccountRuntime> accountOwners = new HashSet<MoneyAccountRuntime>();
        bool allSelected = true;
        foreach (NpcRuntime npc in roster)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId))
                throw new InvalidOperationException("MoneyAccount census requires installed NPCs with valid RuntimeIds.");
            if (!runtimeIds.Add(npc.RuntimeId))
                throw new InvalidOperationException("MoneyAccount census requires unique NPC RuntimeIds.");

            MoneyAccountRuntime owner = npc.MoneyAccount;
            if (owner == null)
                throw new InvalidOperationException("MoneyAccount census requires an existing account owner.");
            if (!accountOwners.Add(owner))
                throw new InvalidOperationException("MoneyAccount census requires a distinct account owner for each NPC.");
            allSelected &= npc.SelectedRuntimeOwners != null;
            ordered.Add(npc);
        }

        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        Dictionary<string, DailyV1NpcOwnerCensusProviders.IMoneyAccountProvider> selectedProviders = null;
        if (allSelected)
        {
            DailyV1NpcRuntimeOwners[] selectedOwners = new DailyV1NpcRuntimeOwners[ordered.Count];
            for (int i = 0; i < ordered.Count; i++) selectedOwners[i] = ordered[i].SelectedRuntimeOwners;
            IReadOnlyList<IOwnerSectionCensusProvider> coreProviders =
                DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(selectedOwners);
            selectedProviders = new Dictionary<string, DailyV1NpcOwnerCensusProviders.IMoneyAccountProvider>(StringComparer.Ordinal);
            foreach (IOwnerSectionCensusProvider coreProvider in coreProviders)
            {
                if (!(coreProvider is DailyV1NpcOwnerCensusProviders.IMoneyAccountProvider selectedProvider)
                    || !selectedProviders.TryAdd(selectedProvider.RuntimeId, selectedProvider))
                    throw new InvalidOperationException("Selected MoneyAccount census providers must have unique Core runtime identities.");
            }
        }
        for (int i = 0; i < ordered.Count; i++)
        {
            NpcRuntime npc = ordered[i];
            IOwnerSectionCensusProvider selectedProvider = null;
            if (allSelected)
            {
                if (!selectedProviders.TryGetValue(npc.RuntimeId, out DailyV1NpcOwnerCensusProviders.IMoneyAccountProvider coreProvider)
                    || !ReferenceEquals(coreProvider.NpcOwner, npc.SelectedRuntimeOwners)
                    || !ReferenceEquals(coreProvider.MoneyAccountOwner, npc.SelectedRuntimeOwners.MoneyAccount)
                    || !ReferenceEquals(coreProvider.MoneyAccountOwner, npc.MoneyAccount))
                    throw new InvalidOperationException("Selected MoneyAccount census must use the exact Core owner bound to the NPC runtime.");
                selectedProvider = coreProvider;
            }
            providers[i] = new SectionProvider(npc, npc.MoneyAccount, selectedProvider);
        }

        return Array.AsReadOnly(providers);
    }
}
