using NUnit.Framework;

public sealed class CrimeSocialAppraisalIntegrationTests
{
    [Test]
    public void VictimKnowsLossWithoutPerpetratorAndLaterAttributionSupersedesReaction()
    {
        PersonStore persons = CreatePersons(out PersonId maria, out PersonId joao, out _);
        TheftOutcomeStore outcomes = new TheftOutcomeStore(persons);
        TheftOutcome outcome = CreateOutcome(maria, joao, "theft.maria.1");
        Assert.That(outcomes.TryRecord(outcome, out CrimeOutcomeStoreFailure outcomeFailure), Is.True, outcomeFailure.ToString());
        CrimeKnowledgeStore knowledge = new CrimeKnowledgeStore(persons, outcomes);
        SocialReactionStore reactions = new SocialReactionStore(persons);
        CrimeSocialAppraisalIntegration integration = new CrimeSocialAppraisalIntegration(outcomes, knowledge, reactions);

        CrimeKnowledgeObservation lossOnly = CrimeKnowledgeObservation.VictimKnowsLoss(
            outcome,
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectExperience, "loss"),
            100L);
        Assert.That(integration.TryRecordKnowledgeAndAppraise(lossOnly, out SocialReactionStoreFailure firstFailure), Is.True, firstFailure.ToString());
        SocialReaction first = reactions.GetCurrentReactions()[0];
        Assert.That(first.PerceivedAttribution.Kind, Is.EqualTo(SocialPerceivedAttributionKind.Unknown));
        Assert.That(first.Valence, Is.EqualTo(SocialReactionValence.Negative));

        CrimeKnowledgeObservation attributed = new CrimeKnowledgeObservation(
            maria,
            outcome.OutcomeId,
            CrimeKnowledgeRole.Victim,
            true,
            SocialPerceivedAttribution.BelievedPerson(joao),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.ReceivedInformation, "witness"),
            103L);
        Assert.That(integration.TryRecordKnowledgeAndAppraise(attributed, out SocialReactionStoreFailure secondFailure), Is.True, secondFailure.ToString());

        Assert.That(reactions.HistoricalReactions, Has.Count.EqualTo(2));
        Assert.That(reactions.GetCurrentReactions(), Has.Count.EqualTo(1));
        Assert.That(reactions.GetCurrentReactions()[0].PerceivedAttribution.PersonId, Is.EqualTo(joao));
        Assert.That(reactions.GetCurrentReactions()[0].SupersedesReactionId, Is.EqualTo(first.ReactionId));
        Assert.That(outcome.PerpetratorPersonId, Is.EqualTo(joao));
    }

    [Test]
    public void FalseAttributionIsRecordedAsBeliefWithoutChangingFactualPerpetrator()
    {
        PersonStore persons = CreatePersons(out PersonId maria, out PersonId joao, out PersonId pedro);
        TheftOutcomeStore outcomes = new TheftOutcomeStore(persons);
        TheftOutcome outcome = CreateOutcome(maria, joao, "theft.false-belief");
        outcomes.TryRecord(outcome, out _);
        CrimeKnowledgeStore knowledge = new CrimeKnowledgeStore(persons, outcomes);
        SocialReactionStore reactions = new SocialReactionStore(persons);
        CrimeSocialAppraisalIntegration integration = new CrimeSocialAppraisalIntegration(outcomes, knowledge, reactions);

        CrimeKnowledgeObservation falseBelief = new CrimeKnowledgeObservation(
            maria,
            outcome.OutcomeId,
            CrimeKnowledgeRole.Victim,
            true,
            SocialPerceivedAttribution.BelievedPerson(pedro),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.ReceivedInformation, "false-witness"),
            105L);
        Assert.That(integration.TryRecordKnowledgeAndAppraise(falseBelief, out SocialReactionStoreFailure failure), Is.True, failure.ToString());

        Assert.That(reactions.GetCurrentReactions()[0].PerceivedAttribution.PersonId, Is.EqualTo(pedro));
        Assert.That(outcome.PerpetratorPersonId, Is.EqualTo(joao));
    }

    [Test]
    public void InvestigatorReactionRequiresExplicitKnowledgeAndUsesEvaluatorRole()
    {
        PersonStore persons = CreatePersons(out PersonId maria, out PersonId joao, out PersonId investigator);
        TheftOutcomeStore outcomes = new TheftOutcomeStore(persons);
        TheftOutcome outcome = CreateOutcome(maria, joao, "theft.investigation");
        outcomes.TryRecord(outcome, out _);
        CrimeKnowledgeStore knowledge = new CrimeKnowledgeStore(persons, outcomes);
        SocialReactionStore reactions = new SocialReactionStore(persons);
        CrimeSocialAppraisalIntegration integration = new CrimeSocialAppraisalIntegration(outcomes, knowledge, reactions);

        CrimeKnowledgeObservation unawareVictim = CrimeKnowledgeObservation.VictimKnowsLoss(
            outcome,
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectExperience, "loss"),
            100L);
        integration.TryRecordKnowledgeAndAppraise(unawareVictim, out _);
        Assert.That(reactions.GetCurrentReactions(), Has.Count.EqualTo(1));

        CrimeKnowledgeObservation awareVictim = new CrimeKnowledgeObservation(
            maria,
            outcome.OutcomeId,
            CrimeKnowledgeRole.Victim,
            true,
            SocialPerceivedAttribution.Unknown(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectObservation, "investigator-seen"),
            101L,
            knownInvestigatorPersonId: investigator);
        Assert.That(integration.TryRecordKnowledgeAndAppraise(awareVictim, out SocialReactionStoreFailure victimFailure), Is.True, victimFailure.ToString());
        Assert.That(reactions.GetCurrentReactions(), Has.Count.EqualTo(2));

        CrimeKnowledgeObservation unawareCriminal = new CrimeKnowledgeObservation(
            joao,
            outcome.OutcomeId,
            CrimeKnowledgeRole.Perpetrator,
            false,
            SocialPerceivedAttribution.NotApplicable(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectObservation, "no-known-investigation"),
            101L);
        Assert.That(integration.TryRecordKnowledgeAndAppraise(unawareCriminal, out SocialReactionStoreFailure criminalUnawareFailure), Is.True, criminalUnawareFailure.ToString());
        Assert.That(reactions.GetCurrentReactions(), Has.Count.EqualTo(2));

        CrimeKnowledgeObservation awareCriminal = new CrimeKnowledgeObservation(
            joao,
            outcome.OutcomeId,
            CrimeKnowledgeRole.Perpetrator,
            false,
            SocialPerceivedAttribution.NotApplicable(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectObservation, "investigator-seen"),
            102L,
            knownInvestigatorPersonId: investigator);
        Assert.That(integration.TryRecordKnowledgeAndAppraise(awareCriminal, out SocialReactionStoreFailure criminalFailure), Is.True, criminalFailure.ToString());

        SocialReaction criminalReaction = null;
        foreach (SocialReaction reaction in reactions.GetCurrentReactions())
        {
            if (reaction.EvaluatorPersonId == joao && reaction.Target.Kind == SocialReactionTargetKind.Person)
            {
                criminalReaction = reaction;
            }
        }

        Assert.That(criminalReaction, Is.Not.Null);
        Assert.That(criminalReaction.Valence, Is.EqualTo(SocialReactionValence.Negative));
    }

    [Test]
    public void CrimeSystemEmitsPersonBasedOutcomeWithoutUsingRuntimeIdentity()
    {
        PersonStore persons = CreatePersons(out PersonId maria, out PersonId joao, out _);
        TheftOutcomeStore outcomes = new TheftOutcomeStore(persons);
        CrimeSocialAppraisalIntegration integration = new CrimeSocialAppraisalIntegration(
            outcomes,
            new CrimeKnowledgeStore(persons, outcomes),
            new SocialReactionStore(persons));
        CityRuntime city = SimulationTestFactory.CreateCity("crime-social-city", "crime-social-location");
        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            null,
            personStore: persons);
        Assert.That(world.TryMaterializePerson(
            joao,
            SimulationTestFactory.CreateNpc("thief"),
            "runtime-joao",
            city,
            0f,
            out NpcRuntime thief,
            out PersonMaterializationFailure thiefFailure), Is.True, thiefFailure.ToString());
        Assert.That(world.TryMaterializePerson(
            maria,
            SimulationTestFactory.CreateNpc("victim"),
            "runtime-maria",
            city,
            50f,
            out NpcRuntime victim,
            out PersonMaterializationFailure victimFailure), Is.True, victimFailure.ToString());
        SimulationTime time = world.SimulationTime;
        JusticeSystem justice = new JusticeSystem(null, null, null, null);
        CrimeSystem crime = new CrimeSystem(justice, null, null, theftOutcomeSink: integration, simulationTime: time);
        NpcActionData action = SimulationTestFactory.CreateAction("steal-social", NpcActionType.Steal, NpcActionCategory.Crime);
        action.crimeSettings.amount = 20;

        Assert.That(crime.TryExecuteAction(thief, new NpcActionRuntime(action, victim, 20)), Is.Not.Null);
        Assert.That(outcomes.Count, Is.EqualTo(1));
        Assert.That(outcomes.Outcomes[0].PerpetratorPersonId, Is.EqualTo(joao));
        Assert.That(outcomes.Outcomes[0].VictimPersonId, Is.EqualTo(maria));
        Assert.That(outcomes.Outcomes[0].OutcomeId.Value, Does.Not.Contain(thief.RuntimeId));
        Assert.That(outcomes.Outcomes[0].OutcomeId.Value, Does.Not.Contain(victim.RuntimeId));
    }

    private static TheftOutcome CreateOutcome(PersonId victim, PersonId perpetrator, string key)
    {
        return new TheftOutcome(
            TheftOutcomeId.Create(perpetrator, victim, 100L, key),
            perpetrator,
            victim,
            20,
            100L);
    }

    private static PersonStore CreatePersons(
        out PersonId maria,
        out PersonId joao,
        out PersonId pedro)
    {
        PersonStore persons = new PersonStore();
        maria = new PersonId("person.maria");
        joao = new PersonId("person.joao");
        pedro = new PersonId("person.pedro");
        persons.TryRegister(new PersonRuntime(maria), out _);
        persons.TryRegister(new PersonRuntime(joao), out _);
        persons.TryRegister(new PersonRuntime(pedro), out _);
        return persons;
    }

}
