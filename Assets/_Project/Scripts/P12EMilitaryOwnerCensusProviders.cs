using System;

/// <summary>Passive exact-count witness for one installed manpower-state owner.</summary>
public sealed class ContingentManpowerCensusProvider : IOwnerSectionCensusProvider
{
    public const int SchemaVersion = 1;
    public const string SectionId = "p12e.contingent-manpower.states";

    private readonly ContingentManpowerStateStore owner;

    public ContingentManpowerCensusProvider(ContingentManpowerStateStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.States.Count, owner.Revision);
    }
}

/// <summary>Passive exact-count witness for one installed armed-force position owner.</summary>
public sealed class ArmedForceSpatialCensusProvider : IOwnerSectionCensusProvider
{
    public const int SchemaVersion = 1;
    public const string SectionId = "p12e.armed-force-spatial.positions";

    private readonly ArmedForceSpatialStateStore owner;

    public ArmedForceSpatialCensusProvider(ArmedForceSpatialStateStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.Count, owner.Revision);
    }
}
