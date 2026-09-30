using System;

/// <summary>Passive exact-count witness for the installed TravelPartyStore.</summary>
public sealed class TravelPartyCensusProvider : IOwnerSectionCensusProvider
{
    public const int SchemaVersion = 1;
    public const string SectionId = "p12f.travel-parties";

    private readonly TravelPartyStore owner;

    public TravelPartyCensusProvider(TravelPartyStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.ActiveParties.Count, owner.Revision);
    }
}
