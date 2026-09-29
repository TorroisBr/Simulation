using System;
using System.Collections.Generic;

/// <summary>
/// Passive P12-B evidence for the installed P8-B passage child. Reads are
/// unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class SpatialPassageStateCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p8b.passage-option-barrier-state";
    public const int SchemaVersion = 1;

    private readonly SpatialAuthorityStore parent;
    private readonly SpatialPassageAuthority owner;

    public SpatialPassageStateCensusProvider(SpatialAuthorityStore parent)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        owner = parent.PassageAuthority
            ?? throw new ArgumentException("The installed spatial authority must have a passage child.", nameof(parent));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        if (ReferenceEquals(parent.PassageAuthority, owner) == false)
        {
            throw new InvalidOperationException("The installed passage owner changed after census provider construction.");
        }

        IReadOnlyList<PassageOptionRecord> options = owner.Options;
        IReadOnlyList<BarrierRecord> barriers = owner.Barriers;
        int cardinality;
        checked
        {
            cardinality = options.Count + barriers.Count;
        }

        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, cardinality, parent.Revision);
    }
}

/// <summary>
/// Passive P12-B evidence for the installed P8-B crossing owner. Reads are
/// unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class SpatialCrossingCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p8b.crossings";
    public const int SchemaVersion = 1;

    private readonly SpatialAuthorityStore owner;

    public SpatialCrossingCensusProvider(SpatialAuthorityStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.CrossingCount, owner.Revision);
    }
}
