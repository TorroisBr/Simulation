/// <summary>Passive census for retained P11 actor-choice inputs.</summary>
public sealed class ActorChoiceP11CensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p12f.actor-choice-inputs";
    public const int SchemaVersion = 1;

    private readonly ActorChoiceStore store;

    public ActorChoiceP11CensusProvider(ActorChoiceStore store)
    {
        this.store = store ?? throw new System.ArgumentNullException(nameof(store));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(
            SectionId,
            SchemaVersion,
            store.CensusOwnerIdentity,
            store.P11InputCount,
            store.CensusRevision);
    }
}

/// <summary>Passive census for P18 temporal inputs excluded from the P12 daily profile.</summary>
public sealed class ActorChoiceTemporalCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p12f.actor-choice-temporal-inputs";
    public const int SchemaVersion = 1;

    private readonly ActorChoiceStore store;

    public ActorChoiceTemporalCensusProvider(ActorChoiceStore store)
    {
        this.store = store ?? throw new System.ArgumentNullException(nameof(store));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(
            SectionId,
            SchemaVersion,
            store.CensusOwnerIdentity,
            store.TemporalInputCount,
            store.CensusRevision);
    }
}
