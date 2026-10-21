/// <summary>Passive count/revision witness for one concrete directive store.</summary>
public sealed class ScheduledDirectiveCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = ScheduledDirectiveStore.SectionId;
    public const int SchemaVersion = ScheduledDirectiveStore.SchemaVersion;

    private readonly ScheduledDirectiveStore owner;

    public ScheduledDirectiveCensusProvider(ScheduledDirectiveStore owner)
    {
        this.owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus() => owner.GetCurrentCensus();
}
