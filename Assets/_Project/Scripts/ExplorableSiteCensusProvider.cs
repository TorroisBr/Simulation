using System;

/// <summary>
/// Passive P12-B evidence for the exact installed ExplorableSiteStore.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class ExplorableSiteCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p12d.explorable-sites";
    public const int SchemaVersion = 1;

    private readonly ExplorableSiteStore owner;

    public ExplorableSiteCensusProvider(ExplorableSiteStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.Count, owner.Revision);
    }
}
