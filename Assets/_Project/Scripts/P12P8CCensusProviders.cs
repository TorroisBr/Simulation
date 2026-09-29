using System;

/// <summary>
/// Passive P12-B evidence for the installed P8-C City/Site anchor owner.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class LegacySpatialAnchorBindingCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p8c.city-site-location-bindings";
    public const int SchemaVersion = 1;

    private readonly LegacySpatialAnchorBindingStore owner;

    public LegacySpatialAnchorBindingCensusProvider(LegacySpatialAnchorBindingStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.Count, owner.Revision);
    }
}

/// <summary>
/// Passive P12-B evidence for the installed P8-C Person position owner.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class PersonSpatialPositionCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p8c.person-positions";
    public const int SchemaVersion = 1;

    private readonly PersonSpatialPositionStore owner;

    public PersonSpatialPositionCensusProvider(PersonSpatialPositionStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.Count, owner.Revision);
    }
}
