using System;

/// <summary>Passive exact-count witness for the runtime-installed War owner.</summary>
public sealed class PersistentWarCensusProvider : IOwnerSectionCensusProvider
{
    public const int SchemaVersion = 1;
    public const string SectionId = "p12e.wars";

    private readonly PersistentWarStore owner;

    public PersistentWarCensusProvider(PersistentWarStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.Count, owner.Revision);
    }
}
