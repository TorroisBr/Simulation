using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>Exact singleton witness for one registered Person's life and residence facts.</summary>
public sealed class PersonLifeResidenceCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionPrefix = "p12b.person-life-residence/";
    public const int SchemaVersion = 1;

    private readonly PersonRuntime owner;

    public PersonLifeResidenceCensusProvider(PersonRuntime owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public PersonRuntime PersonOwner => owner;
    public string SectionId => SectionIdFor(owner.PersonId);

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(
            SectionId,
            SchemaVersion,
            owner,
            1,
            owner.LifeResidenceRevision);
    }

    public static string SectionIdFor(PersonId personId)
    {
        if (personId == null) throw new ArgumentNullException(nameof(personId));
        return SectionPrefix
            + personId.Value.Length.ToString(CultureInfo.InvariantCulture)
            + ":" + personId.Value;
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        IReadOnlyList<PersonRuntime> roster)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));
        List<PersonRuntime> ordered = new List<PersonRuntime>(roster.Count);
        HashSet<string> personIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<PersonRuntime> owners = new HashSet<PersonRuntime>();
        for (int i = 0; i < roster.Count; i++)
        {
            PersonRuntime person = roster[i];
            if (person == null
                || person.PersonId == null
                || !personIds.Add(person.PersonId.Value)
                || !owners.Add(person))
                throw new ArgumentException("Person life/residence census requires unique exact Person owners and IDs.", nameof(roster));
            ordered.Add(person);
        }
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.PersonId.Value, right.PersonId.Value));
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        for (int i = 0; i < ordered.Count; i++) providers[i] = new PersonLifeResidenceCensusProvider(ordered[i]);
        return Array.AsReadOnly(providers);
    }
}

/// <summary>Exact singleton witness for one NPC's lifecycle owner revision.</summary>
public sealed class NpcLifecycleCensusProvider : IOwnerSectionCensusProvider
{
    public const string LifeStateSectionPrefix = "p12b.npc-life-state/";
    public const string ResidenceSectionPrefix = "p12b.npc-residence/";
    public const int SchemaVersion = 1;

    private readonly NpcRuntime owner;
    private readonly bool residence;

    public NpcLifecycleCensusProvider(NpcRuntime owner, bool residence)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        if (string.IsNullOrWhiteSpace(owner.RuntimeId))
            throw new ArgumentException("NPC lifecycle census requires a RuntimeId.", nameof(owner));
        if (residence && owner.BoundPersonRuntime != null)
            throw new ArgumentException("Person-backed NPC residence belongs to its Person owner.", nameof(owner));
        this.residence = residence;
    }

    public NpcRuntime NpcOwner => owner;
    public bool IsResidence => residence;
    public string SectionId => SectionIdFor(owner.RuntimeId, residence);

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(
            SectionId,
            SchemaVersion,
            owner,
            1,
            residence ? owner.ResidenceRevision : owner.LifeStateRevision);
    }

    public static string SectionIdFor(string runtimeId, bool residence)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            throw new ArgumentException("NPC lifecycle section requires a RuntimeId.", nameof(runtimeId));
        return (residence ? ResidenceSectionPrefix : LifeStateSectionPrefix) + runtimeId;
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
                throw new ArgumentException("NPC lifecycle census requires unique exact NPC owners and RuntimeIds.", nameof(roster));
            ordered.Add(npc);
        }
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        List<IOwnerSectionCensusProvider> providers = new List<IOwnerSectionCensusProvider>(ordered.Count * 2);
        for (int i = 0; i < ordered.Count; i++)
        {
            NpcRuntime npc = ordered[i];
            providers.Add(new NpcLifecycleCensusProvider(npc, false));
            if (npc.BoundPersonRuntime == null)
                providers.Add(new NpcLifecycleCensusProvider(npc, true));
        }
        return providers.AsReadOnly();
    }
}
