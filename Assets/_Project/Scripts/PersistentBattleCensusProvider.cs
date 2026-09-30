using System;

/// <summary>Passive exact-count witness for the runtime-installed Battle owner.</summary>
public sealed class PersistentBattleCensusProvider : IOwnerSectionCensusProvider
{
    public const int SchemaVersion = 1;
    public const string SectionId = "p12e.battles";

    private readonly PersistentBattleStore owner;

    public PersistentBattleCensusProvider(PersistentBattleStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.Count, owner.Revision);
    }
}
