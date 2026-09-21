using NUnit.Framework;

public sealed class CrimeSocialAppraisalIntegrationTests
{
    [Test]
    public void VictimKnowsLossWithoutPerpetratorAndLaterAttributionSupersedesReaction()
    {
        PersonStore persons = CreatePersons(out PersonId maria, out PersonId joao, out _);
        SimulationTime time = new SimulationTime(200L);
        TheftOutcomeStore outcomes = new TheftOutcomeStore(persons, time);
        TheftOutcome outcome = CreateOutcome(maria, joao, "theft.maria.1");
        Assert.That(outcomes.TryRecord(outcome, out CrimeOutcomeStoreFailure outcomeFailure), Is.True, outcomeFailure.ToString());
        CrimeKnowledgeStore knowledge = new CrimeKnowledgeStore(persons, outcomes, time, new InstitutionStore());
        SocialReactionStore reactions = new SocialReactionStore(persons, time);
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
        SimulationTime time = new SimulationTime(200L);
        TheftOutcomeStore outcomes = new TheftOutcomeStore(persons, time);
        TheftOutcome outcome = CreateOutcome(maria, joao, "theft.false-belief");
        outcomes.TryRecord(outcome, out _);
        CrimeKnowledgeStore knowledge = new CrimeKnowledgeStore(persons, outcomes, time, new InstitutionStore());
        SocialReactionStore reactions = new SocialReactionStore(persons, time);
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
        SimulationTime time = new SimulationTime(200L);
        TheftOutcomeStore outcomes = new TheftOutcomeStore(persons, time);
        TheftOutcome outcome = CreateOutcome(maria, joao, "theft.investigation");
        outcomes.TryRecord(outcome, out _);
        CrimeKnowledgeStore knowledge = new CrimeKnowledgeStore(persons, outcomes, time, new InstitutionStore());
        SocialReactionStore reactions = new SocialReactionStore(persons, time);
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
        SimulationTime time = new SimulationTime();
        TheftOutcomeStore outcomes = new TheftOutcomeStore(persons, time);
        CrimeSocialAppraisalIntegration integration = new CrimeSocialAppraisalIntegration(
            outcomes,
            new CrimeKnowledgeStore(persons, outcomes, time, new InstitutionStore()),
            new SocialReactionStore(persons, time));
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
        SimulationTime crimeTime = new SimulationTime();
        JusticeSystem justice = new JusticeSystem(null, null, null, null);
        CrimeSystem crime = new CrimeSystem(justice, null, null, theftOutcomeSink: integration, simulationTime: crimeTime);
        NpcActionData action = SimulationTestFactory.CreateAction("steal-social", NpcActionType.Steal, NpcActionCategory.Crime);
        action.crimeSettings.amount = 20;

        NpcActionRuntime theftAction = new NpcActionRuntime(action, victim, 20);
        theftAction.SetStableOccurrenceKey("explicit-theft-1");
        Assert.That(crime.TryExecuteAction(thief, theftAction), Is.Not.Null);
        Assert.That(outcomes.Count, Is.EqualTo(1));
        Assert.That(outcomes.Outcomes[0].PerpetratorPersonId, Is.EqualTo(joao));
        Assert.That(outcomes.Outcomes[0].VictimPersonId, Is.EqualTo(maria));
        Assert.That(outcomes.Outcomes[0].OutcomeId.Value, Does.Not.Contain(thief.RuntimeId));
        Assert.That(outcomes.Outcomes[0].OutcomeId.Value, Does.Not.Contain(victim.RuntimeId));
    }

    [Test]
    public void BoundCrimeSystemRejectsTheftWithoutSemanticOccurrenceKeyBeforeMutation()
    {
        PersonStore persons = CreatePersons(out PersonId maria, out PersonId joao, out _);
        SimulationTime time = new SimulationTime();
        CrimeSocialAppraisalWorldState state = new CrimeSocialAppraisalWorldState(
            persons,
            new InstitutionStore(),
            time);
        CityRuntime city = SimulationTestFactory.CreateCity("crime-key-city", "crime-key-location");
        SimulationRuntime world = new SimulationRuntime(
            time,
            new[] { city },
            null,
            personStore: persons);
        Assert.That(world.TryMaterializePerson(
            joao,
            SimulationTestFactory.CreateNpc("key-thief"),
            "runtime-key-joao",
            city,
            0f,
            out NpcRuntime thief,
            out PersonMaterializationFailure thiefFailure), Is.True, thiefFailure.ToString());
        Assert.That(world.TryMaterializePerson(
            maria,
            SimulationTestFactory.CreateNpc("key-victim"),
            "runtime-key-maria",
            city,
            50f,
            out NpcRuntime victim,
            out PersonMaterializationFailure victimFailure), Is.True, victimFailure.ToString());

        CrimeSystem crime = new CrimeSystem(
            new JusticeSystem(null, null, null, null),
            null,
            null,
            simulationTime: time,
            theftOutcomeSink: state.Integration);
        NpcActionData action = SimulationTestFactory.CreateAction("steal-without-key", NpcActionType.Steal, NpcActionCategory.Crime);
        NpcActionResult result = crime.TryExecuteAction(thief, new NpcActionRuntime(action, victim, 20));

        Assert.That(result.Success, Is.False);
        Assert.That(thief.Money, Is.EqualTo(0f));
        Assert.That(victim.Money, Is.EqualTo(50f));
        Assert.That(state.TheftOutcomes.Count, Is.EqualTo(0));
    }

    [Test]
    public void TheftOutcomeIdentityUsesExplicitSemanticOccurrenceAndIsNotDecisionSequenceBased()
    {
        PersonId perpetrator = new PersonId("person.joao");
        PersonId victim = new PersonId("person.maria");
        TheftOutcomeId first = TheftOutcomeId.Create(perpetrator, victim, 100L, "theft-slot-a");
        TheftOutcomeId same = TheftOutcomeId.Create(perpetrator, victim, 100L, "theft-slot-a");
        TheftOutcomeId second = TheftOutcomeId.Create(perpetrator, victim, 100L, "theft-slot-b");

        Assert.That(first, Is.EqualTo(same));
        Assert.That(first, Is.Not.EqualTo(second));
        Assert.That(first.Value, Does.Not.Contain("decision-"));
        Assert.That(first.Value, Does.Not.Contain("npc-"));
        Assert.Throws<System.ArgumentException>(() => new TheftOutcome(
            first,
            perpetrator,
            victim,
            20,
            100L,
            "different-semantic-occurrence"));
    }

    [Test]
    public void CrimeKnowledgeRejectsFutureBeforeOutcomeAndMismatchedRoleWithoutMutation()
    {
        PersonStore persons = CreatePersons(out PersonId maria, out PersonId joao, out _);
        SimulationTime time = new SimulationTime(200L);
        TheftOutcomeStore outcomes = new TheftOutcomeStore(persons, time);
        TheftOutcome outcome = CreateOutcome(maria, joao, "theft-guards");
        outcomes.TryRecord(outcome, out _);
        CrimeKnowledgeStore knowledge = new CrimeKnowledgeStore(persons, outcomes, time, new InstitutionStore());

        CrimeKnowledgeObservation beforeOutcome = new CrimeKnowledgeObservation(
            maria,
            outcome.OutcomeId,
            CrimeKnowledgeRole.Victim,
            true,
            SocialPerceivedAttribution.Unknown(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectExperience, "too-early"),
            99L);
        Assert.That(knowledge.TryRecord(beforeOutcome, out CrimeKnowledgeStoreFailure beforeFailure), Is.False);
        Assert.That(beforeFailure.Code, Is.EqualTo(CrimeKnowledgeStoreFailureCode.BeforeOutcome));

        CrimeKnowledgeObservation wrongRole = new CrimeKnowledgeObservation(
            maria,
            outcome.OutcomeId,
            CrimeKnowledgeRole.Perpetrator,
            false,
            SocialPerceivedAttribution.NotApplicable(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectObservation, "wrong-role"),
            100L);
        Assert.That(knowledge.TryRecord(wrongRole, out CrimeKnowledgeStoreFailure roleFailure), Is.False);
        Assert.That(roleFailure.Code, Is.EqualTo(CrimeKnowledgeStoreFailureCode.RoleEndpointMismatch));
        Assert.That(knowledge.CurrentObservations, Is.Empty);
    }

    [Test]
    public void SimulationRuntimeOwnsOneSharedCrimeAppraisalWorldBoundary()
    {
        PersonStore persons = new PersonStore();
        SimulationTime time = new SimulationTime();
        SimulationRuntime world = new SimulationRuntime(
            time,
            null,
            null,
            personStore: persons);

        Assert.That(world.CrimeSocialAppraisal, Is.Not.Null);
        Assert.That(world.CrimeSocialAppraisal.PersonStore, Is.SameAs(world.PersonStore));
        Assert.That(world.CrimeSocialAppraisal.SimulationTime, Is.SameAs(world.SimulationTime));
        Assert.That(world.CrimeSocialAppraisal.TheftOutcomes.PersonStore, Is.SameAs(world.PersonStore));
        Assert.That(world.CrimeSocialAppraisal.CrimeKnowledge.OutcomeStore, Is.SameAs(world.CrimeSocialAppraisal.TheftOutcomes));
        Assert.That(world.CrimeSocialAppraisal.SocialReactions.PersonStore, Is.SameAs(world.PersonStore));
    }

    [Test]
    public void CrimeAppraisalIsRepresentedInDeterministicDiagnostics()
    {
        PersonStore persons = CreatePersons(out PersonId maria, out PersonId joao, out _);
        InstitutionStore institutions = new InstitutionStore();
        SimulationTime time = new SimulationTime(200L);
        CrimeSocialAppraisalWorldState state = new CrimeSocialAppraisalWorldState(
            persons,
            institutions,
            time);
        TheftOutcome outcome = CreateOutcome(maria, joao, "theft-diagnostics");

        Assert.That(state.Integration.TryAcceptTheftOutcome(outcome), Is.True);
        WorldStateSnapshot snapshot = WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            simulationTime: time,
            personStore: persons,
            institutionStore: institutions,
            crimeSocialAppraisal: state));

        Assert.That(snapshot.TheftOutcomeCount, Is.EqualTo(1));
        Assert.That(snapshot.CrimeKnowledgeCount, Is.EqualTo(1));
        Assert.That(snapshot.SocialReactionCount, Is.EqualTo(1));
        Assert.That(WorldStateDiagnostics.Export(snapshot), Does.Contain("THEFT_OUTCOME|"));
        Assert.That(WorldStateDiagnostics.Export(snapshot), Does.Contain("CRIME_KNOWLEDGE|"));
        Assert.That(WorldStateDiagnostics.Export(snapshot), Does.Contain("SOCIAL_REACTION|"));
        Assert.That(WorldStateDiagnostics.Validate(snapshot).HasErrors, Is.False);

        Assert.That(state.Integration.TryAcceptTheftOutcome(
            CreateOutcome(maria, joao, "theft-diagnostics-2")), Is.True);
        WorldStateSnapshot after = WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            simulationTime: time,
            personStore: persons,
            institutionStore: institutions,
            crimeSocialAppraisal: state));
        bool foundOutcomeChange = false;
        foreach (WorldStateDifference difference in WorldStateDiagnostics.Compare(snapshot, after).Differences)
        {
            if (difference.Section == "TheftOutcome")
            {
                foundOutcomeChange = true;
                break;
            }
        }

        Assert.That(foundOutcomeChange, Is.True);
    }

    [Test]
    public void SimulationRuntimeRejectsCrimeSystemBoundToForeignAppraisalWorld()
    {
        PersonStore foreignPersons = CreatePersons(out _, out _, out _);
        SimulationTime foreignTime = new SimulationTime();
        CrimeSocialAppraisalWorldState foreignState = new CrimeSocialAppraisalWorldState(
            foreignPersons,
            new InstitutionStore(),
            foreignTime);
        CrimeSystem crime = new CrimeSystem(
            new JusticeSystem(null, null, null, null),
            null,
            null,
            simulationTime: foreignTime,
            theftOutcomeSink: foreignState.Integration);

        Assert.Throws<System.ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(),
            null,
            null,
            personStore: new PersonStore(),
            crimeSystem: crime));
    }

    [Test]
    public void SimulationRuntimeBindsCrimeSystemToOneSimulationTimeBoundary()
    {
        SimulationTime time = new SimulationTime(17L);
        CrimeSystem unboundCrime = new CrimeSystem(null, null, null);
        SimulationRuntime world = new SimulationRuntime(
            time,
            null,
            null,
            crimeSystem: unboundCrime);

        Assert.That(unboundCrime.SimulationTime, Is.SameAs(world.SimulationTime));

        CrimeSystem foreignCrime = new CrimeSystem(
            null,
            null,
            null,
            simulationTime: new SimulationTime(17L));
        Assert.Throws<System.ArgumentException>(() => new SimulationRuntime(
            time,
            null,
            null,
            crimeSystem: foreignCrime));
    }

    [Test]
    public void CrimeAppraisalIntegrationRejectsUnboundReactionStore()
    {
        PersonStore persons = CreatePersons(out PersonId maria, out PersonId joao, out _);
        SimulationTime time = new SimulationTime(200L);
        TheftOutcomeStore outcomes = new TheftOutcomeStore(persons, time);
        Assert.Throws<System.ArgumentException>(() => new CrimeSocialAppraisalIntegration(
            outcomes,
            new CrimeKnowledgeStore(persons, outcomes, time, new InstitutionStore()),
            new SocialReactionStore()));
    }

    private static TheftOutcome CreateOutcome(PersonId victim, PersonId perpetrator, string key)
    {
        return new TheftOutcome(
            TheftOutcomeId.Create(perpetrator, victim, 100L, key),
            perpetrator,
            victim,
            20,
            100L,
            key);
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
