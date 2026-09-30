using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Passive P12-B evidence for the installed Person registry membership.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class PersonMembershipCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p12d.person.membership";
    public const int SchemaVersion = 1;

    private readonly PersonStore owner;

    public PersonMembershipCensusProvider(PersonStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(
            SectionId,
            SchemaVersion,
            owner,
            owner.Persons.Count,
            owner.Revision);
    }
}

/// <summary>
/// Passive P12-B evidence for the installed Person-to-NpcRuntime bindings.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class PersonMaterializationBindingCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p12d.person.materialization-binding";
    public const int SchemaVersion = 1;

    private readonly PersonStore owner;

    public PersonMaterializationBindingCensusProvider(PersonStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(
            SectionId,
            SchemaVersion,
            owner,
            owner.MaterializedBindingCount,
            owner.Revision);
    }
}

/// <summary>Creates the fixed census provider set for one installed PersonStore.</summary>
public static class PersonStoreCensusProvider
{
    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(PersonStore owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        IOwnerSectionCensusProvider[] providers =
        {
            new PersonMembershipCensusProvider(owner),
            new PersonMaterializationBindingCensusProvider(owner)
        };
        return new ReadOnlyCollection<IOwnerSectionCensusProvider>(providers);
    }
}
