using System;
using NUnit.Framework;

public sealed class PersonNaturalMortalityFoundationTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void DeathDayIsNullableWriteOnceTruthAndLifeIsDerivedForRequestedDay()
    {
        PersonRuntime historical = new PersonRuntime(
            new PersonId("historical-death"),
            3L,
            12L);

        Assert.That(historical.DeathAbsoluteDay, Is.EqualTo(12L));
        Assert.That(historical.IsDeadAt(11L), Is.False);
        Assert.That(historical.IsDeadAt(12L), Is.True);
        Assert.That(historical.IsDeadAt(100L), Is.True);
        Assert.That(typeof(PersonRuntime).GetProperty("LifeState"), Is.Null);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PersonRuntime(new PersonId("negative-death"), 0L, -1L));
        Assert.Throws<ArgumentException>(() =>
            new PersonRuntime(new PersonId("death-before-birth"), 5L, 4L));
    }

    [Test]
    public void ProposalIsPureAndApplyKeepsPersonRegisteredAndHistoricalRelations()
    {
        SimulationRuntime world = CreateWorld(20L);
        PersonRuntime parent = Register(world, "mortality-parent", 0L);
        PersonRuntime child = Register(world, "mortality-child", 10L);
        Assert.That(world.TryAddParentage(parent.PersonId, child.PersonId, out _), Is.True);

        Assert.That(world.TryProposePersonDeath(
            parent.PersonId,
            out PersonDeathTransition transition,
            out PersonDeathLifecycleFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(parent.DeathAbsoluteDay, Is.Null);
        Assert.That(world.ContainsParentage(parent.PersonId, child.PersonId), Is.True);

        Assert.That(world.TryApplyPersonDeath(
            transition,
            out PersonDeathLifecycleFailure applyFailure), Is.True, applyFailure.ToString());
        Assert.That(parent.DeathAbsoluteDay, Is.EqualTo(20L));
        Assert.That(parent.IsDeadAt(20L), Is.True);
        Assert.That(world.PersonStore.TryGet(parent.PersonId, out PersonRuntime registered), Is.True);
        Assert.That(registered, Is.SameAs(parent));
        Assert.That(world.ContainsParentage(parent.PersonId, child.PersonId), Is.True);

        Assert.That(world.TryApplyPersonDeath(parent.PersonId, out _, out PersonDeathLifecycleFailure repeatedFailure), Is.False);
        Assert.That(repeatedFailure, Is.EqualTo(PersonDeathLifecycleFailure.PersonAlreadyDead));
        Assert.That(parent.DeathAbsoluteDay, Is.EqualTo(20L));
    }

    [Test]
    public void MaterializedDeathAtomicallyUpdatesPersonAndExecutionMirrorOnly()
    {
        SimulationRuntime world = CreateWorld(30L);
        PersonRuntime person = Register(world, "materialized-death", 0L);
        Assert.That(world.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc("materialized-death-definition"),
            "materialized-death-npc",
            null,
            0f,
            out NpcRuntime npc,
            out _), Is.True);
        npc.SetCurrentAction(SimulationTestFactory.CreateAction(
            "death-action",
            NpcActionType.Normal));

        Assert.That(npc.TryApplyDeath(), Is.False);
        Assert.That(world.TryApplyPersonDeath(
            person.PersonId,
            out PersonDeathTransition transition,
            out PersonDeathLifecycleFailure failure), Is.True, failure.ToString());

        Assert.That(transition.ExpectedMaterializedNpcRuntimeId, Is.EqualTo(npc.RuntimeId));
        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(30L));
        Assert.That(npc.LifeState, Is.EqualTo(NpcLifeState.Dead));
        Assert.That(npc.CurrentAction, Is.Null);
        Assert.That(person.MaterializedNpcRuntimeId, Is.EqualTo(npc.RuntimeId));
        Assert.That(world.NpcRuntimes, Does.Contain(npc));
    }

    [Test]
    public void ResidentPersonDeathRequiresWorldAuthorityAndKeepsAggregateAtomic()
    {
        CityData data = SimulationTestFactory.CreateCityData("resident-death-city-definition");
        data.initialPopulation = 5;
        CityRuntime city = new CityRuntime(
            "resident-death-city",
            data,
            new SpatialLocationRuntime("resident-death-location"));
        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(12L),
            new[] { city },
            null);
        PersonRuntime person = Register(world, "resident-death-person", 0L);
        Assert.That(world.TryBindExistingPersonResident(person.PersonId, city, out _), Is.True);
        Assert.That(world.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc("resident-death-definition"),
            "resident-death-npc",
            null,
            0f,
            out NpcRuntime npc,
            out _), Is.True);
        int populationBefore = city.CurrentPopulation;
        long revisionBefore = city.Population.Revision;

        Assert.That(NpcPopulationLifecycleSystem.TryApplyResidentDeath(
            npc,
            city,
            world.GetAuthoritativeNpcRoster(),
            out _,
            out NpcPopulationLifecycleFailure directFailure), Is.False);
        Assert.That(directFailure, Is.EqualTo(NpcPopulationLifecycleFailure.PersonDeathAuthorityRequired));
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(npc.IsAlive, Is.True);
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));

        Assert.That(world.TryApplyResidentDeath(
            npc,
            city,
            out _,
            out NpcPopulationLifecycleFailure worldFailure), Is.True, worldFailure.ToString());
        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(12L));
        Assert.That(npc.IsDead, Is.True);
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore - 1));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore + 1L));
    }

    [Test]
    public void DormantResidentPersonDeathClearsResidenceAndDecrementsAggregateAtomically()
    {
        CityData data = SimulationTestFactory.CreateCityData("dormant-resident-death-city-definition");
        data.initialPopulation = 5;
        CityRuntime city = new CityRuntime(
            "dormant-resident-death-city",
            data,
            new SpatialLocationRuntime("dormant-resident-death-location"));
        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(12L),
            new[] { city },
            null);
        PersonRuntime person = Register(world, "dormant-resident-death-person", 0L);
        Assert.That(world.TryBindExistingPersonResident(person.PersonId, city, out _), Is.True);

        Assert.That(world.TryApplyPersonDeath(
            person.PersonId,
            out PersonDeathTransition transition,
            out PersonDeathLifecycleFailure failure), Is.True, failure.ToString());

        Assert.That(transition.ExpectsResident, Is.True);
        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(12L));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void DeadPersonCannotBeAssignedResidence()
    {
        CityData data = SimulationTestFactory.CreateCityData("dead-residence-city-definition");
        data.initialPopulation = 2;
        CityRuntime city = new CityRuntime(
            "dead-residence-city",
            data,
            new SpatialLocationRuntime("dead-residence-location"));
        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(10L),
            new[] { city },
            null);
        PersonRuntime person = new PersonRuntime(new PersonId("dead-residence-person"), 0L, 9L);
        Assert.That(world.TryRegisterPerson(person, out _), Is.True);

        Assert.That(world.TryBindExistingPersonResident(
            person.PersonId,
            city,
            out PersonResidenceMembershipFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PersonResidenceMembershipFailure.PersonDead));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(2));
    }

    [Test]
    public void StaleDayAndMaterializationAreRejectedWithoutPartialMutation()
    {
        SimulationRuntime dayWorld = CreateWorld(5L);
        PersonRuntime dayPerson = Register(dayWorld, "stale-day", 0L);
        Assert.That(dayWorld.TryProposePersonDeath(dayPerson.PersonId, out PersonDeathTransition staleDay, out _), Is.True);
        dayWorld.SimulationTime.AdvanceDay();

        Assert.That(dayWorld.TryApplyPersonDeath(staleDay, out PersonDeathLifecycleFailure dayFailure), Is.False);
        Assert.That(dayFailure, Is.EqualTo(PersonDeathLifecycleFailure.StaleWorldDay));
        Assert.That(dayPerson.DeathAbsoluteDay, Is.Null);

        SimulationRuntime bindingWorld = CreateWorld(5L);
        PersonRuntime bindingPerson = Register(bindingWorld, "stale-binding", 0L);
        Assert.That(bindingWorld.TryProposePersonDeath(bindingPerson.PersonId, out PersonDeathTransition staleBinding, out _), Is.True);
        Assert.That(bindingWorld.TryMaterializePerson(
            bindingPerson.PersonId,
            SimulationTestFactory.CreateNpc("stale-binding-definition"),
            "stale-binding-npc",
            null,
            0f,
            out NpcRuntime npc,
            out _), Is.True);

        Assert.That(bindingWorld.TryApplyPersonDeath(staleBinding, out PersonDeathLifecycleFailure bindingFailure), Is.False);
        Assert.That(bindingFailure, Is.EqualTo(PersonDeathLifecycleFailure.StaleMaterialization));
        Assert.That(bindingPerson.DeathAbsoluteDay, Is.Null);
        Assert.That(npc.IsAlive, Is.True);

        Assert.That(bindingWorld.TryApplyPersonDeath(
            (PersonDeathTransition)null,
            out PersonDeathLifecycleFailure invalidFailure), Is.False);
        Assert.That(invalidFailure, Is.EqualTo(PersonDeathLifecycleFailure.InvalidTransition));
    }

    [Test]
    public void ProposalCannotCrossWorldsWithEqualPersonIdentityAndDay()
    {
        SimulationRuntime first = CreateWorld(5L);
        SimulationRuntime second = CreateWorld(5L);
        PersonRuntime firstPerson = Register(first, "shared-death-id", 0L);
        PersonRuntime secondPerson = Register(second, "shared-death-id", 0L);
        Assert.That(first.TryProposePersonDeath(firstPerson.PersonId, out PersonDeathTransition transition, out _), Is.True);

        Assert.That(second.TryApplyPersonDeath(transition, out PersonDeathLifecycleFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PersonDeathLifecycleFailure.StalePersonRegistration));
        Assert.That(firstPerson.DeathAbsoluteDay, Is.Null);
        Assert.That(secondPerson.DeathAbsoluteDay, Is.Null);
    }

    [Test]
    public void DeathDoesNotVacateOfficeOrMutateInstitutionalRecognition()
    {
        SimulationRuntime world = CreateWorld(40L);
        PersonRuntime person = Register(world, "dead-incumbent", 0L);
        InstitutionId institutionId = new InstitutionId("mortality-institution");
        OfficeId officeId = new OfficeId("mortality-office");
        Assert.That(world.TryRegisterInstitution(new InstitutionRecord(institutionId), out _), Is.True);
        Assert.That(world.TryRegisterOffice(new OfficeRecord(officeId, institutionId), out _), Is.True);
        Assert.That(world.TryAssignIncumbent(officeId, person.PersonId, out _), Is.True);

        Assert.That(world.TryApplyPersonDeath(person.PersonId, out _, out _), Is.True);

        Assert.That(world.IsOfficeVacant(officeId), Is.False);
        Assert.That(world.TryGetCurrentOfficeIncumbent(officeId, out PersonId incumbent), Is.True);
        Assert.That(incumbent, Is.EqualTo(person.PersonId));
    }

    [Test]
    public void DeadPersonCannotMaterializeAndLegacyBindingRequiresMatchingLifeState()
    {
        SimulationRuntime world = CreateWorld(10L);
        PersonRuntime dead = new PersonRuntime(new PersonId("dead-materialization"), 0L, 9L);
        Assert.That(world.TryRegisterPerson(dead, out _), Is.True);

        Assert.That(world.TryMaterializePerson(
            dead.PersonId,
            SimulationTestFactory.CreateNpc("dead-materialization-definition"),
            "dead-materialization-npc",
            null,
            0f,
            out _,
            out PersonMaterializationFailure materializationFailure), Is.False);
        Assert.That(materializationFailure, Is.EqualTo(PersonMaterializationFailure.PersonDead));

        NpcRuntime livingLegacy = new NpcRuntime(
            "living-legacy",
            SimulationTestFactory.CreateNpc("living-legacy-definition"));
        SimulationRuntime adoptionWorld = new SimulationRuntime(
            new SimulationTime(10L),
            null,
            new[] { livingLegacy });
        PersonRuntime historicalDead = new PersonRuntime(new PersonId("historical-dead"), 0L, 8L);
        Assert.That(adoptionWorld.TryRegisterPerson(historicalDead, out _), Is.True);
        Assert.That(adoptionWorld.TryBindExistingNpcToPerson(
            historicalDead.PersonId,
            livingLegacy.RuntimeId,
            out PersonMaterializationFailure bindingFailure), Is.False);
        Assert.That(bindingFailure, Is.EqualTo(PersonMaterializationFailure.LifeStateConflict));
    }

    [Test]
    public void NaturalMortalityEvaluationUsesCustomCalendarAndExplicitDeterministicSample()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("custom-calendar-mortality"), 0L);
        SimulationCalendar calendar = new SimulationCalendar(
            new CalendarDefinition(new[] { 2, 3 }, 1));
        const double annualProbability = 0.25d;
        double expectedDailyProbability = 1d - Math.Pow(0.75d, 1d / 5d);

        Assert.That(PersonNaturalMortalityQuery.TryEvaluate(
            person,
            7L,
            calendar,
            annualProbability,
            0d,
            out PersonNaturalMortalityEvaluation dies,
            out PersonNaturalMortalityQueryFailure failure), Is.True, failure.ToString());
        Assert.That(dies.CompletedYears, Is.EqualTo(1L));
        Assert.That(dies.DailyProbability, Is.EqualTo(expectedDailyProbability).Within(1e-12d));
        Assert.That(dies.ShouldDie, Is.True);

        Assert.That(PersonNaturalMortalityQuery.TryEvaluate(
            person,
            7L,
            calendar,
            annualProbability,
            0.99d,
            out PersonNaturalMortalityEvaluation survives,
            out _), Is.True);
        Assert.That(survives.ShouldDie, Is.False);
        Assert.That(person.DeathAbsoluteDay, Is.Null);
    }

    [Test]
    public void NaturalMortalityRejectsUnknownBirthRecordedDeathAndInvalidInputs()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(3, 2, 4));
        PersonRuntime unknownBirth = new PersonRuntime(new PersonId("unknown-birth"));
        Assert.That(PersonNaturalMortalityQuery.TryEvaluate(
            unknownBirth,
            10L,
            calendar,
            0.2d,
            0.1d,
            out _,
            out PersonNaturalMortalityQueryFailure unknownFailure), Is.False);
        Assert.That(unknownFailure, Is.EqualTo(PersonNaturalMortalityQueryFailure.BirthDateUnknown));

        PersonRuntime dead = new PersonRuntime(new PersonId("already-dead"), 0L, 5L);
        Assert.That(PersonNaturalMortalityQuery.TryEvaluate(
            dead,
            10L,
            calendar,
            0.2d,
            0.1d,
            out _,
            out PersonNaturalMortalityQueryFailure deadFailure), Is.False);
        Assert.That(deadFailure, Is.EqualTo(PersonNaturalMortalityQueryFailure.DeathAlreadyRecorded));

        PersonRuntime living = new PersonRuntime(new PersonId("invalid-input"), 0L);
        Assert.That(PersonNaturalMortalityQuery.TryEvaluate(
            living,
            10L,
            calendar,
            -0.1d,
            0.1d,
            out _,
            out PersonNaturalMortalityQueryFailure probabilityFailure), Is.False);
        Assert.That(probabilityFailure, Is.EqualTo(PersonNaturalMortalityQueryFailure.InvalidAnnualProbability));
        Assert.That(PersonNaturalMortalityQuery.TryEvaluate(
            living,
            10L,
            calendar,
            0.2d,
            1d,
            out _,
            out PersonNaturalMortalityQueryFailure sampleFailure), Is.False);
        Assert.That(sampleFailure, Is.EqualTo(PersonNaturalMortalityQueryFailure.InvalidDeterministicSample));
    }

    [Test]
    public void DiagnosticsCaptureDeathAndBoundNpcConsistency()
    {
        SimulationRuntime world = CreateWorld(15L);
        PersonRuntime person = Register(world, "diagnostic-death", 0L);
        Assert.That(world.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc("diagnostic-death-definition"),
            "diagnostic-death-npc",
            null,
            0f,
            out _,
            out _), Is.True);
        Assert.That(world.TryApplyPersonDeath(person.PersonId, out _, out _), Is.True);

        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(
            new WorldStateSnapshotContext(
                simulationTime: world.SimulationTime,
                npcs: world.NpcRuntimes,
                personStore: world.PersonStore));

        Assert.That(snapshot.Persons[0].DeathAbsoluteDay, Is.EqualTo(15L));
        Assert.That(
            WorldStateCanonicalWriter.Write(snapshot),
            Does.Contain("PERSON|diagnostic-death|diagnostic-death-npc|true|0|~|~|15\n"));
        Assert.That(WorldStateInvariantValidator.Validate(snapshot).IsValid, Is.True);
    }

    private static SimulationRuntime CreateWorld(long absoluteDay)
    {
        return new SimulationRuntime(new SimulationTime(absoluteDay), null, null);
    }

    private static PersonRuntime Register(
        SimulationRuntime world,
        string personId,
        long? birthAbsoluteDay)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(personId), birthAbsoluteDay);
        Assert.That(world.TryRegisterPerson(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }
}
