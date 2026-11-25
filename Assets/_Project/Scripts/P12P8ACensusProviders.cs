using System;

/// <summary>
/// Passive P12-B evidence for the installed P8-A Hex collection.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class SpatialHexCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = P12P8AImageSectionContract.HexSectionId;
    public const int SchemaVersion = P12P8AImageSectionContract.SchemaVersion;

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
    public const string SectionId = P12P8AImageSectionContract.LocationSectionId;
    public const int SchemaVersion = P12P8AImageSectionContract.SchemaVersion;

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
    public const string SectionId = P12P8AImageSectionContract.ScaleContextSectionId;
    public const int SchemaVersion = P12P8AImageSectionContract.SchemaVersion;

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
