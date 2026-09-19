using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class PersonFoundationTests
{
    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void PersonId_IsOrdinalImmutableAndRejectsEmptyValues()
    {
        PersonId first = new PersonId("person-1");
        PersonId equal = new PersonId("person-1");
        PersonId differentCase = new PersonId("PERSON-1");

        Assert.That(first.Equals(equal), Is.True);
        Assert.That(first == equal, Is.True);
        Assert.That(first != differentCase, Is.True);
        Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
        Assert.That(first.ToString(), Is.EqualTo("person-1"));
        Assert.Throws<ArgumentException>(() => new PersonId("  "));
    }

    [Test]
    public void PersonStore_RegistersOnceAndExposesReadOnlySnapshot()
    {
        PersonStore store = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("person-store"));

        Assert.That(store.TryRegister(person, out PersonStoreFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(PersonStoreFailure.None));
        Assert.That(store.TryRegister(new PersonRuntime(new PersonId("person-store")), out failure), Is.False);
        Assert.That(failure, Is.EqualTo(PersonStoreFailure.DuplicatePersonId));
        Assert.That(store.TryGet(new PersonId("person-store"), out PersonRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(person));

        PersonStore independentStore = new PersonStore();
        Assert.That(independentStore.TryRegister(new PersonRuntime(new PersonId("person-store")), out _), Is.True);

        IList<PersonRuntime> readOnly = store.Persons as IList<PersonRuntime>;
        Assert.That(readOnly, Is.Not.Null);
        Assert.That(readOnly.IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => readOnly.Add(new PersonRuntime(new PersonId("other"))));
    }

    [Test]
    public void PersonStore_CanCoexistWithAllPopulationRepresentationModes()
    {
        PopulationRepresentationMode[] modes =
        {
            PopulationRepresentationMode.Aggregate,
            PopulationRepresentationMode.Hybrid,
            PopulationRepresentationMode.FullyIndividualized
        };

        foreach (PopulationRepresentationMode mode in modes)
        {
            EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
                new EffectivePopulationConfiguration(mode, NpcDecisionSimulationScope.RelevantOnly),
                new EffectiveEconomyConfiguration(true),
                new EffectiveTravelConfiguration(0f),
                new EffectiveCrimeConfiguration(false, false),
                new EffectiveGuardCrimeConfiguration(false));
            SimulationRuntime world = new SimulationRuntime(
                new SimulationTime(),
                null,
                null,
                configuration: configuration);

            Assert.That(world.Configuration.Population.RepresentationMode, Is.EqualTo(mode));
            Assert.That(world.TryRegisterPerson(new PersonRuntime(new PersonId("person-" + mode)), out _), Is.True);
            Assert.That(world.PersonStore.Persons, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void Materialization_BindsPersonAndDoesNotChangePopulationAggregate()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("person-city", "person-location");
        SimulationRuntime world = CreateWorld();
        PersonId personId = new PersonId("person-materialized");
        Assert.That(world.TryRegisterPerson(new PersonRuntime(personId), out _), Is.True);
        int populationBefore = city.CurrentPopulation;
        long revisionBefore = city.Population.Revision;

        Assert.That(world.TryMaterializePerson(
            personId,
            SimulationTestFactory.CreateNpc("person-definition"),
            "npc-materialized",
            city,
            75f,
            out NpcRuntime npc,
            out PersonMaterializationFailure failure), Is.True);

        Assert.That(failure, Is.EqualTo(PersonMaterializationFailure.None));
        Assert.That(npc.PersonId, Is.EqualTo(personId));
        Assert.That(world.NpcRuntimes, Has.Count.EqualTo(1));
        Assert.That(world.PersonStore.TryGetByMaterializedNpcRuntimeId("npc-materialized", out PersonRuntime bound), Is.True);
        Assert.That(bound.PersonId, Is.EqualTo(personId));
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
    }

    [Test]
    public void Materialization_IsAtomicAcrossDuplicatePersonAndRuntimeFailures()
    {
        SimulationRuntime world = CreateWorld();
        PersonId firstId = new PersonId("person-first");
        PersonId secondId = new PersonId("person-second");
        Assert.That(world.TryRegisterPerson(new PersonRuntime(firstId), out _), Is.True);
        Assert.That(world.TryRegisterPerson(new PersonRuntime(secondId), out _), Is.True);

        Assert.That(world.TryMaterializePerson(
            firstId,
            SimulationTestFactory.CreateNpc("definition-first"),
            "npc-shared",
            null,
            0f,
            out _,
            out _), Is.True);

        Assert.That(world.TryMaterializePerson(
            firstId,
            SimulationTestFactory.CreateNpc("definition-duplicate-person"),
            "npc-other",
            null,
            0f,
            out _,
            out PersonMaterializationFailure duplicatePersonFailure), Is.False);
        Assert.That(duplicatePersonFailure, Is.EqualTo(PersonMaterializationFailure.AlreadyMaterialized));

        Assert.That(world.TryMaterializePerson(
            secondId,
            SimulationTestFactory.CreateNpc("definition-duplicate-runtime"),
            "npc-shared",
            null,
            0f,
            out _,
            out PersonMaterializationFailure duplicateRuntimeFailure), Is.False);
        Assert.That(duplicateRuntimeFailure, Is.EqualTo(PersonMaterializationFailure.DuplicateNpcRuntimeId));
        Assert.That(world.NpcRuntimes, Has.Count.EqualTo(1));
        Assert.That(world.PersonStore.TryGet(secondId, out PersonRuntime second), Is.True);
        Assert.That(second.IsMaterialized, Is.False);
    }

    [Test]
    public void Materialization_RejectsMissingPersonWithoutRegisteringNpc()
    {
        SimulationRuntime world = CreateWorld();

        Assert.That(world.TryMaterializePerson(
            new PersonId("not-registered"),
            SimulationTestFactory.CreateNpc("missing-person-definition"),
            "npc-missing-person",
            null,
            0f,
            out NpcRuntime npc,
            out PersonMaterializationFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(PersonMaterializationFailure.PersonNotRegistered));
        Assert.That(npc, Is.Null);
        Assert.That(world.NpcRuntimes, Is.Empty);
    }

    [Test]
    public void ExistingLegacyNpc_CanBeExplicitlyAdoptedWithoutChangingPopulation()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("legacy-city", "legacy-location");
        NpcRuntime legacy = new NpcRuntime("npc-legacy", SimulationTestFactory.CreateNpc("legacy-definition"));
        SimulationRuntime world = CreateWorld(legacy);
        PersonId personId = new PersonId("person-legacy");
        Assert.That(world.TryRegisterPerson(new PersonRuntime(personId), out _), Is.True);
        int populationBefore = city.CurrentPopulation;
        long revisionBefore = city.Population.Revision;

        Assert.That(world.TryBindExistingNpcToPerson(
            personId,
            legacy.RuntimeId,
            out PersonMaterializationFailure failure), Is.True);

        Assert.That(failure, Is.EqualTo(PersonMaterializationFailure.None));
        Assert.That(legacy.PersonId, Is.EqualTo(personId));
        Assert.That(world.NpcRuntimes, Has.Count.EqualTo(1));
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
    }

    [Test]
    public void BoundNpc_CannotBeReassignedOrUnregistered()
    {
        SimulationRuntime world = CreateWorld();
        PersonId firstId = new PersonId("person-bound");
        PersonId secondId = new PersonId("person-other");
        Assert.That(world.TryRegisterPerson(new PersonRuntime(firstId), out _), Is.True);
        Assert.That(world.TryRegisterPerson(new PersonRuntime(secondId), out _), Is.True);
        Assert.That(world.TryMaterializePerson(
            firstId,
            SimulationTestFactory.CreateNpc("bound-definition"),
            "npc-bound",
            null,
            0f,
            out _,
            out _), Is.True);

        Assert.That(world.TryBindExistingNpcToPerson(secondId, "npc-bound", out PersonMaterializationFailure bindFailure), Is.False);
        Assert.That(bindFailure, Is.EqualTo(PersonMaterializationFailure.NpcAlreadyBoundToDifferentPerson));
        Assert.That(world.TryUnregisterNpc("npc-bound", out WorldNpcRegistryFailure unregisterFailure), Is.False);
        Assert.That(unregisterFailure, Is.EqualTo(WorldNpcRegistryFailure.NpcBoundToPerson));
        Assert.That(world.NpcRuntimes, Has.Count.EqualTo(1));
        Assert.That(world.PersonStore.TryGetByMaterializedNpcRuntimeId("npc-bound", out _), Is.True);
    }

    [Test]
    public void PersonBinding_SurvivesDeathAndEmigrationTransitions()
    {
        SimulationRuntime deathWorld = CreateWorld();
        PersonId deadPersonId = new PersonId("person-dead");
        Assert.That(deathWorld.TryRegisterPerson(new PersonRuntime(deadPersonId), out _), Is.True);
        Assert.That(deathWorld.TryMaterializePerson(
            deadPersonId,
            SimulationTestFactory.CreateNpc("death-definition"),
            "npc-dead",
            null,
            0f,
            out NpcRuntime deadNpc,
            out _), Is.True);
        Assert.That(deathWorld.TryApplyPersonDeath(
            deadPersonId,
            out _,
            out PersonDeathLifecycleFailure deathFailure), Is.True, deathFailure.ToString());
        Assert.That(deadNpc.PersonId, Is.EqualTo(deadPersonId));
        Assert.That(deathWorld.PersonStore.TryGet(deadPersonId, out PersonRuntime deadPerson), Is.True);
        Assert.That(deadPerson.IsMaterialized, Is.True);
        Assert.That(deadPerson.DeathAbsoluteDay, Is.EqualTo(deathWorld.CurrentDay));

        CityRuntime city = SimulationTestFactory.CreateCity("emigration-city", "emigration-location");
        SimulationRuntime travelWorld = CreateWorld();
        PersonId travelerId = new PersonId("person-emigrant");
        Assert.That(travelWorld.TryRegisterPerson(new PersonRuntime(travelerId), out _), Is.True);
        Assert.That(travelWorld.TryMaterializePerson(
            travelerId,
            SimulationTestFactory.CreateNpc("emigration-definition"),
            "npc-emigrant",
            null,
            0f,
            out NpcRuntime traveler,
            out _), Is.True);
        Assert.That(travelWorld.TryApplyImmigration(traveler, city, out _, out NpcPopulationLifecycleFailure immigrationFailure), Is.True);
        Assert.That(immigrationFailure, Is.EqualTo(NpcPopulationLifecycleFailure.None));
        Assert.That(travelWorld.TryApplyEmigration(traveler, city, out _, out NpcPopulationLifecycleFailure emigrationFailure), Is.True);
        Assert.That(emigrationFailure, Is.EqualTo(NpcPopulationLifecycleFailure.None));
        Assert.That(traveler.PersonId, Is.EqualTo(travelerId));
        Assert.That(travelWorld.PersonStore.TryGet(travelerId, out PersonRuntime travelerPerson), Is.True);
        Assert.That(travelerPerson.IsMaterialized, Is.True);
    }

    [Test]
    public void Diagnostics_IncludePersonsAndBindingsButLegacyContextsRemainValid()
    {
        SimulationRuntime world = CreateWorld();
        PersonId personId = new PersonId("person-diagnostics");
        Assert.That(world.TryRegisterPerson(new PersonRuntime(personId), out _), Is.True);
        WorldStateSnapshot before = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: world.SimulationTime,
            npcs: world.NpcRuntimes,
            personStore: world.PersonStore));

        Assert.That(before.PersonCount, Is.EqualTo(1));
        Assert.That(before.Persons[0].IsMaterialized, Is.False);
        Assert.That(WorldStateCanonicalWriter.Write(before), Does.Contain("PERSON|person-diagnostics|"));
        Assert.That(WorldStateInvariantValidator.Validate(before).IsValid, Is.True);
        Assert.That(WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext()).PersonCount, Is.EqualTo(0));

        Assert.That(world.TryMaterializePerson(
            personId,
            SimulationTestFactory.CreateNpc("diagnostics-definition"),
            "npc-diagnostics",
            null,
            0f,
            out _,
            out _), Is.True);
        WorldStateSnapshot after = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: world.SimulationTime,
            npcs: world.NpcRuntimes,
            personStore: world.PersonStore));
        WorldStateDiff diff = WorldStateDiff.Compare(before, after);
        bool hasPersonDifference = false;
        foreach (WorldStateDifference difference in diff.Differences)
        {
            if (difference.Section == "Person")
            {
                hasPersonDifference = true;
                break;
            }
        }

        Assert.That(after.Persons[0].MaterializedNpcRuntimeId, Is.EqualTo("npc-diagnostics"));
        Assert.That(after.Npcs[0].PersonId, Is.EqualTo("person-diagnostics"));
        Assert.That(WorldStateCanonicalWriter.Write(after), Does.Contain("NPC_PERSON|npc-diagnostics|person-diagnostics"));
        Assert.That(hasPersonDifference, Is.True);
        Assert.That(WorldStateInvariantValidator.Validate(after).IsValid, Is.True);
    }

    [Test]
    public void Diagnostics_RejectDanglingMaterializedPersonBinding()
    {
        WorldStateSnapshot snapshot = new WorldStateSnapshot(
            0L,
            persons: new[] { new WorldStatePersonSnapshot("person-dangling", "npc-missing") });

        WorldStateInvariantReport report = WorldStateInvariantValidator.Validate(snapshot);
        Assert.That(report.HasErrors, Is.True);
        Assert.That(ContainsIssue(report, "PersonMaterializedNpcMissing"), Is.True);
    }

    private static SimulationRuntime CreateWorld(params NpcRuntime[] npcs)
    {
        return new SimulationRuntime(new SimulationTime(), null, npcs);
    }

    private static bool ContainsIssue(WorldStateInvariantReport report, string code)
    {
        foreach (WorldStateInvariantIssue issue in report.Issues)
        {
            if (issue != null && issue.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
