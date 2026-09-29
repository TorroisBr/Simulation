using System;

/// <summary>
/// Passive P12-B evidence for the installed P8-A Hex collection.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class SpatialHexCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p8a.hexes";
    public const int SchemaVersion = 1;

    private readonly SpatialAuthorityStore owner;

    public SpatialHexCensusProvider(SpatialAuthorityStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.HexCount, owner.Revision);
    }
}

/// <summary>
/// Passive P12-B evidence for the installed P8-A Location collection.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class SpatialLocationCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p8a.locations";
    public const int SchemaVersion = 1;

    private readonly SpatialAuthorityStore owner;

    public SpatialLocationCensusProvider(SpatialAuthorityStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.LocationCount, owner.Revision);
    }
}

/// <summary>
/// Passive P12-B evidence for the installed P8-A scale-context presence.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class SpatialScaleContextCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p8a.scale-context";
    public const int SchemaVersion = 1;

    private readonly SpatialAuthorityStore owner;

    public SpatialScaleContextCensusProvider(SpatialAuthorityStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.HasGeography ? 1 : 0, owner.Revision);
    }
}
