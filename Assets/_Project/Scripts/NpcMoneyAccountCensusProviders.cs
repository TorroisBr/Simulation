using System;
using System.Collections.Generic;

/// <summary>Passive per-NPC witnesses for installed MoneyAccountRuntime owners.</summary>
public static class NpcMoneyAccountCensusProvider
{
    public const string SectionPrefix = "p12e.npc-money-account/";
    public const int SchemaVersion = 1;

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
        private readonly string sectionId;

        public SectionProvider(NpcRuntime npc, MoneyAccountRuntime owner)
        {
            this.npc = npc;
            this.owner = owner;
            sectionId = SectionPrefix + npc.RuntimeId;
        }

        public string RuntimeId => npc.RuntimeId;
        public NpcRuntime NpcOwner => npc;
        public MoneyAccountRuntime MoneyAccountOwner => owner;

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
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
            ordered.Add(npc);
        }

        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
        {
            NpcRuntime npc = ordered[i];
            providers[i] = new SectionProvider(npc, npc.MoneyAccount);
        }

        return Array.AsReadOnly(providers);
    }
}
