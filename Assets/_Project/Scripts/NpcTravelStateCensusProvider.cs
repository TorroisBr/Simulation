using System;
using System.Collections.Generic;

/// <summary>Passive per-NPC witness for travel commitment identity and revision.</summary>
public sealed class NpcTravelStateCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionPrefix = "p12f.npc-travel-state/";
    public const int SchemaVersion = 1;

    private readonly NpcRuntime owner;

    public NpcTravelStateCensusProvider(NpcRuntime owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        if (string.IsNullOrWhiteSpace(owner.RuntimeId))
            throw new ArgumentException("NPC travel-state census requires a RuntimeId.", nameof(owner));
    }

    public string RuntimeId => owner.RuntimeId;
    public NpcRuntime NpcOwner => owner;
    public string SectionId => SectionIdFor(owner.RuntimeId);

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(
            SectionId,
            SchemaVersion,
            owner,
            1,
            owner.TravelStateRevision);
    }

    public static string SectionIdFor(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            throw new ArgumentException("NPC travel-state section requires a RuntimeId.", nameof(runtimeId));
        return SectionPrefix + runtimeId;
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        IReadOnlyList<NpcRuntime> roster)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));

        List<NpcRuntime> ordered = new List<NpcRuntime>(roster.Count);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<NpcRuntime> owners = new HashSet<NpcRuntime>();
        for (int i = 0; i < roster.Count; i++)
        {
            NpcRuntime npc = roster[i];
            if (npc == null
                || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !runtimeIds.Add(npc.RuntimeId)
                || !owners.Add(npc))
            {
                throw new ArgumentException(
                    "NPC travel-state census requires unique exact NPC owners and RuntimeIds.",
                    nameof(roster));
            }
            ordered.Add(npc);
        }

        ordered.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
            providers[i] = new NpcTravelStateCensusProvider(ordered[i]);
        return Array.AsReadOnly(providers);
    }
}
