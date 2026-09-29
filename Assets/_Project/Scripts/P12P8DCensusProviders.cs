using System;

/// <summary>
/// Passive P12-B evidence for the installed P8-D route-observation owner.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class SpatialRouteObservationCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p8d.spatial-route-observations";
    public const int SchemaVersion = 1;

    private readonly SpatialRouteKnowledgeStore owner;

    public SpatialRouteObservationCensusProvider(SpatialRouteKnowledgeStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.ObservationCount, owner.Revision);
    }
}

/// <summary>
/// Passive P12-B evidence for the installed P8-D route-plan history owner.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public sealed class PersonRoutePlanHistoryCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p8d.person-route-plan-history";
    public const int SchemaVersion = 1;

    private readonly PersonRoutePlanStore owner;

    public PersonRoutePlanHistoryCensusProvider(PersonRoutePlanStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.PlanCount, owner.Revision);
    }
}
