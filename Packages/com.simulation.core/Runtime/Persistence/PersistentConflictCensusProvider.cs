using System;

/// <summary>Passive exact-count witness for the runtime-installed conflict owner.</summary>
public sealed class PersistentConflictCensusProvider : IOwnerSectionCensusProvider
{
    public const int SchemaVersion = 1;
    public const string SectionId = "p12e.conflicts";

    private readonly PersistentConflictStore owner;

    public PersistentConflictCensusProvider(PersistentConflictStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.Count, owner.Revision);
    }
}
