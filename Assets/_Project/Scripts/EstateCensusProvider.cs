using System;

/// <summary>Passive exact-count witness for the runtime-installed Estate owner.</summary>
public sealed class EstateCensusProvider : IOwnerSectionCensusProvider
{
    public const int SchemaVersion = 1;
    public const string SectionId = "p12e.estate.records";

    private readonly EstateStore owner;

    public EstateCensusProvider(EstateStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.Count, owner.Revision);
    }
}
