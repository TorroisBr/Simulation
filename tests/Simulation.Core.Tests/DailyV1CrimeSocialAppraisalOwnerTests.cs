using NUnit.Framework;

public sealed class DailyV1CrimeSocialAppraisalOwnerTests
{
    [Test]
    public void TheftOutcomeStorePreservesSemanticIdentityAndP12RevisionBoundary()
    {
        PersonStore persons = CreatePersons(out PersonId perpetrator, out PersonId victim);
        SimulationTime time = new SimulationTime(20L);
        TheftOutcomeStore store = new TheftOutcomeStore(persons, time);
        RecordingMutationBoundary boundary = new RecordingMutationBoundary();
        Assert.That(store.TryBindP12MutationBoundary(boundary), Is.True);

        TheftOutcome outcome = CreateOutcome(perpetrator, victim, 20L, "stable-occurrence");
        Assert.That(outcome.OutcomeId, Is.EqualTo(TheftOutcomeId.Create(
            perpetrator, victim, 20L, "stable-occurrence")));
        Assert.That(outcome.OutcomeId.Value, Does.Not.Contain("runtime-thief"));

        Assert.That(store.TryRecord(outcome, out CrimeOutcomeStoreFailure failure), Is.True, failure.ToString());
        Assert.That(store.P12CensusRevision, Is.EqualTo(1L));
        Assert.That(boundary.CommittedCount, Is.EqualTo(1));
        Assert.That(store.TryRecord(outcome, out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(CrimeOutcomeStoreFailureCode.DuplicateOutcomeId));
        Assert.That(store.Count, Is.EqualTo(1));
        Assert.That(store.P12CensusRevision, Is.EqualTo(1L));

        Assert.That(store.TryRemove(outcome.OutcomeId), Is.True);
        Assert.That(store.Count, Is.Zero);
        Assert.That(store.P12CensusRevision, Is.EqualTo(2L));
        Assert.That(boundary.CommittedCount, Is.EqualTo(2));
    }

    [Test]
    public void CrimeKnowledgeAndReactionOperateOnTheExactSharedOutcomeOwner()
    {
        PersonStore persons = CreatePersons(out PersonId perpetrator, out PersonId victim);
        SimulationTime time = new SimulationTime(20L);
        TheftOutcomeStore outcomes = new TheftOutcomeStore(persons, time);
        TheftOutcome outcome = CreateOutcome(perpetrator, victim, 19L, "theft-source");
        Assert.That(outcomes.TryRecord(outcome, out CrimeOutcomeStoreFailure outcomeFailure),
            Is.True, outcomeFailure.ToString());

        CrimeKnowledgeStore knowledge = new CrimeKnowledgeStore(
            persons, outcomes, time, new InstitutionStore());
        SocialReactionStore reactions = new SocialReactionStore(persons, time);
        CrimeSocialAppraisalIntegration integration = new CrimeSocialAppraisalIntegration(
            outcomes, knowledge, reactions);
        CrimeKnowledgeObservation observation = CrimeKnowledgeObservation.VictimKnowsLoss(
            outcome,
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectExperience, "loss"),
            20L);

        Assert.That(integration.TryRecordKnowledgeAndAppraise(
            observation, out SocialReactionStoreFailure failure), Is.True, failure.ToString());
        Assert.That(knowledge.TryGet(victim, outcome.OutcomeId, out CrimeKnowledgeObservation stored), Is.True);
        Assert.That(stored.OutcomeId, Is.SameAs(outcome.OutcomeId));
        Assert.That(reactions.GetCurrentReactions(), Has.Count.EqualTo(1));
        Assert.That(reactions.GetCurrentReactions()[0].Target.Kind,
            Is.EqualTo(SocialReactionTargetKind.TheftOutcome));
    }

    private static PersonStore CreatePersons(out PersonId perpetrator, out PersonId victim)
    {
        perpetrator = new PersonId("person:perpetrator");
        victim = new PersonId("person:victim");
        PersonStore store = new PersonStore();
        Assert.That(store.TryRegister(new PersonRuntime(perpetrator), out PersonStoreFailure perpetratorFailure),
            Is.True, perpetratorFailure.ToString());
        Assert.That(store.TryRegister(new PersonRuntime(victim), out PersonStoreFailure victimFailure),
            Is.True, victimFailure.ToString());
        return store;
    }

    private static TheftOutcome CreateOutcome(
        PersonId perpetrator,
        PersonId victim,
        long day,
        string occurrenceKey)
    {
        TheftOutcomeId id = TheftOutcomeId.Create(perpetrator, victim, day, occurrenceKey);
        return new TheftOutcome(id, perpetrator, victim, 17, day, occurrenceKey);
    }

    private sealed class RecordingMutationBoundary : IP12CrimeSocialAppraisalOwnerMutationBoundary
    {
        internal int CommittedCount { get; private set; }

        public bool CanCommit(object owner) => owner != null;
        public void Committed(object owner) => CommittedCount++;
        public void Fault() => throw new AssertionException("The owner mutation boundary faulted unexpectedly.");
    }
}
