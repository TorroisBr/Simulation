using System;

/// <summary>Passive fixed-owner witness for active expeditions.</summary>
public sealed class ExpeditionCensusProvider : IOwnerSectionCensusProvider
{
    public const int SchemaVersion = 1;
    public const string SectionId = "p12f.expeditions";
    private readonly ExpeditionStore owner;

    public ExpeditionCensusProvider(ExpeditionStore owner)
    { this.owner = owner ?? throw new ArgumentNullException(nameof(owner)); }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        using (owner.EnterReadWindow())
        {
            if (!owner.ValidateCensus(out int count, out long revision))
                throw new InvalidOperationException("Expedition owner census is inconsistent.");
            return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, count, revision);
        }
    }
}
