using NUnit.Framework;

public sealed class PersonResidenceFoundationTests
{
    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void UnmaterializedPerson_CanBindResidenceWithoutPopulationMutation()
    {
        CityRuntime city = CreateCity("residence-unmaterialized", 5);
        SimulationRuntime world = CreateWorld(city);
        PersonRuntime person = new PersonRuntime(new PersonId("person-unmaterialized"));
        Assert.That(world.TryRegisterPerson(person, out _), Is.True);
        int populationBefore = city.CurrentPopulation;
        long revisionBefore = city.Population.Revision;

        Assert.That(world.TryBindExistingPersonResident(
            person.PersonId,
            city,
            out PersonResidenceMembershipFailure failure), Is.True, failure.ToString());

        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
        SettlementPopulationPresenceSummary summary = Summary(world, city);
        Assert.That(summary.IndividualizedResidentCount, Is.EqualTo(1));
        Assert.That(summary.MaterializedResidentCount, Is.EqualTo(0));
        Assert.That(summary.LegacyResidentNpcCount, Is.EqualTo(0));
        Assert.That(summary.RepresentedResidentCount, Is.EqualTo(1));
    }

    [Test]
    public void Materialization_PreservesPersonResidenceAndDoesNotDoubleCount()
    {
        CityRuntime city = CreateCity("residence-materialization", 5);
        SimulationRuntime world = CreateWorld(city);
        PersonRuntime person = RegisterPerson(world, "person-materialization");
        Assert.That(world.TryBindExistingPersonResident(person.PersonId, city, out _), Is.True);
        int populationBefore = city.CurrentPopulation;
        long revisionBefore = city.Population.Revision;

        Assert.That(world.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc("residence-materialization-definition"),
            "npc-materialization",
            city,
            0f,
            out NpcRuntime npc,
            out PersonMaterializationFailure materializationFailure), Is.True, materializationFailure.ToString());

        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
        SettlementPopulationPresenceSummary summary = Summary(world, city);
        Assert.That(summary.IndividualizedResidentCount, Is.EqualTo(1));
        Assert.That(summary.MaterializedResidentCount, Is.EqualTo(1));
        Assert.That(summary.LegacyResidentNpcCount, Is.EqualTo(0));
        Assert.That(summary.RepresentedResidentCount, Is.EqualTo(1));
    }

    [Test]
    public void LegacyAdoption_TransfersNpcResidenceToPersonAuthority()
    {
        CityRuntime city = CreateCity("residence-adoption", 5);
        NpcRuntime legacy = CreateLegacyResident(city, "npc-adoption");
        SimulationRuntime world = CreateWorldWithNpcs(new[] { legacy }, city);
        PersonRuntime person = RegisterPerson(world, "person-adoption");

        Assert.That(world.TryBindExistingNpcToPerson(
            person.PersonId,
            legacy.RuntimeId,
            out PersonMaterializationFailure failure), Is.True, failure.ToString());

        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(legacy.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        SettlementPopulationPresenceSummary summary = Summary(world, city);
        Assert.That(summary.IndividualizedResidentCount, Is.EqualTo(1));
        Assert.That(summary.LegacyResidentNpcCount, Is.EqualTo(0));
        Assert.That(summary.RepresentedResidentCount, Is.EqualTo(1));
    }

    [Test]
    public void LegacyAdoption_ResidenceConflictIsAtomic()
    {
        CityRuntime origin = CreateCity("residence-conflict-origin", 5);
        CityRuntime personResidence = CreateCity("residence-conflict-person", 5);
        NpcRuntime legacy = CreateLegacyResident(origin, "npc-residence-conflict");
        SimulationRuntime world = CreateWorldWithNpcs(new[] { legacy }, origin, personResidence);
        PersonRuntime person = RegisterPerson(world, "person-residence-conflict");
        Assert.That(world.TryBindExistingPersonResident(person.PersonId, personResidence, out _), Is.True);
        int originPopulation = origin.CurrentPopulation;
        int personPopulation = personResidence.CurrentPopulation;

        Assert.That(world.TryBindExistingNpcToPerson(
            person.PersonId,
            legacy.RuntimeId,
            out PersonMaterializationFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(PersonMaterializationFailure.ResidenceConflict));
        Assert.That(legacy.PersonId, Is.Null);
        Assert.That(legacy.ResidenceSettlementRuntimeId, Is.EqualTo(origin.RuntimeId));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(personResidence.RuntimeId));
        Assert.That(person.IsMaterialized, Is.False);
        Assert.That(origin.CurrentPopulation, Is.EqualTo(originPopulation));
        Assert.That(personResidence.CurrentPopulation, Is.EqualTo(personPopulation));
    }

    [Test]
    public void Adoption_ExistingPersonResidenceIsObservedWhenNpcHasNone()
    {
        CityRuntime city = CreateCity("residence-adoption-person-first", 5);
        NpcRuntime legacy = new NpcRuntime("npc-adoption-person-first", SimulationTestFactory.CreateNpc("adoption-person-first"));
        SimulationRuntime world = CreateWorldWithNpcs(new[] { legacy }, city);
        PersonRuntime person = RegisterPerson(world, "person-adoption-person-first");
        Assert.That(world.TryBindExistingPersonResident(person.PersonId, city, out _), Is.True);

        Assert.That(world.TryBindExistingNpcToPerson(person.PersonId, legacy.RuntimeId, out _), Is.True);
        Assert.That(legacy.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(Summary(world, city).RepresentedResidentCount, Is.EqualTo(1));
    }

    [Test]
    public void PersonBackedNpc_ImmigrationEmigrationAndDeathUpdatePersonResidence()
    {
        CityRuntime city = CreateCity("residence-lifecycle", 5);
        SimulationRuntime world = CreateWorld(city);
        PersonRuntime person = RegisterPerson(world, "person-lifecycle");
        Assert.That(world.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc("residence-lifecycle-definition"),
            "npc-lifecycle",
            null,
            0f,
            out NpcRuntime npc,
            out _), Is.True);

        int populationBefore = city.CurrentPopulation;
        Assert.That(world.TryApplyImmigration(npc, city, out _, out NpcPopulationLifecycleFailure immigrationFailure), Is.True, immigrationFailure.ToString());
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore + 1));

        Assert.That(world.TryApplyEmigration(npc, city, out _, out NpcPopulationLifecycleFailure emigrationFailure), Is.True, emigrationFailure.ToString());
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));

        Assert.That(world.TryApplyImmigration(npc, city, out _, out _), Is.True);
        Assert.That(world.TryApplyResidentDeath(npc, city, out _, out NpcPopulationLifecycleFailure deathFailure), Is.True, deathFailure.ToString());
        Assert.That(npc.IsDead, Is.True);
        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(world.CurrentDay));
        Assert.That(person.IsDeadAt(world.CurrentDay), Is.True);
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
    }

    [Test]
    public void PersonBackedNpc_MigrationUpdatesPersonResidenceAndBothAggregates()
    {
        CityRuntime origin = CreateCity("residence-migration-origin", 5);
        CityRuntime destination = CreateCity("residence-migration-destination", 5);
        SimulationRuntime world = CreateWorld(origin, destination);
        PersonRuntime person = RegisterPerson(world, "person-migration");
        Assert.That(world.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc("residence-migration-definition"),
            "npc-migration",
            null,
            0f,
            out NpcRuntime npc,
            out _), Is.True);
        Assert.That(world.TryApplyImmigration(npc, origin, out _, out _), Is.True);
        int originBefore = origin.CurrentPopulation;
        int destinationBefore = destination.CurrentPopulation;

        Assert.That(NpcResidenceMigrationSystem.TryPropose(
            npc,
            origin,
            destination,
            world.GetAuthoritativeNpcRoster(),
            out NpcResidenceMigrationTransition transition,
            out NpcResidenceMigrationFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(NpcResidenceMigrationSystem.TryApply(
            npc,
            origin,
            destination,
            world.GetAuthoritativeNpcRoster(),
            transition,
            out NpcResidenceMigrationFailure applyFailure), Is.True, applyFailure.ToString());

        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(origin.CurrentPopulation, Is.EqualTo(originBefore - 1));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(destinationBefore + 1));
    }

    [Test]
    public void ResidenceAndPresenceAreDistinctForPhysicallyPresentNpc()
    {
        CityRuntime residence = CreateCity("residence-physical-home", 5);
        CityRuntime presence = CreateCity("residence-physical-visitor", 5);
        SimulationRuntime world = CreateWorld(residence, presence);
        PersonRuntime person = RegisterPerson(world, "person-physical");
        Assert.That(world.TryBindExistingPersonResident(person.PersonId, residence, out _), Is.True);
        Assert.That(world.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc("residence-physical-definition"),
            "npc-physical",
            presence,
            0f,
            out NpcRuntime npc,
            out _), Is.True);

        SettlementPopulationPresenceSummary home = Summary(world, residence);
        SettlementPopulationPresenceSummary visitor = Summary(world, presence);
        Assert.That(home.RepresentedResidentCount, Is.EqualTo(1));
        Assert.That(home.NamedPresentCount, Is.EqualTo(0));
        Assert.That(visitor.RepresentedResidentCount, Is.EqualTo(0));
        Assert.That(visitor.NamedPresentCount, Is.EqualTo(1));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(residence.RuntimeId));
    }

    [Test]
    public void PersonResidenceCapacityCountsIndividualizedResidentsWithoutDoubleCountingMaterialization()
    {
        CityRuntime city = CreateCity("residence-capacity", 1);
        SimulationRuntime world = CreateWorld(city);
        PersonRuntime first = RegisterPerson(world, "person-capacity-first");
        PersonRuntime second = RegisterPerson(world, "person-capacity-second");
        Assert.That(world.TryBindExistingPersonResident(first.PersonId, city, out _), Is.True);
        Assert.That(world.TryMaterializePerson(
            first.PersonId,
            SimulationTestFactory.CreateNpc("capacity-definition"),
            "npc-capacity",
            null,
            0f,
            out _,
            out _), Is.True);

        Assert.That(world.TryBindExistingPersonResident(
            second.PersonId,
            city,
            out PersonResidenceMembershipFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PersonResidenceMembershipFailure.AggregateCapacityExceeded));
        Assert.That(Summary(world, city).RepresentedResidentCount, Is.EqualTo(1));
    }

    [Test]
    public void PersonResidenceDiagnosticsExposeAuthority()
    {
        CityRuntime city = CreateCity("residence-diagnostics", 5);
        SimulationRuntime world = CreateWorld(city);
        PersonRuntime person = RegisterPerson(world, "person-diagnostics-residence");
        Assert.That(world.TryBindExistingPersonResident(person.PersonId, city, out _), Is.True);
        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: world.SimulationTime,
            cities: world.Cities,
            npcs: world.NpcRuntimes,
            personStore: world.PersonStore));

        Assert.That(snapshot.Persons[0].ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(WorldStateCanonicalWriter.Write(snapshot), Does.Contain("residence-diagnostics"));
        Assert.That(WorldStateInvariantValidator.Validate(snapshot).IsValid, Is.True);
    }

    [Test]
    public void DiagnosticsRejectPersonNpcResidenceDivergence()
    {
        WorldStateSnapshot snapshot = new WorldStateSnapshot(
            0L,
            npcs: new[]
            {
                new WorldStateNpcSnapshot(
                    "npc-divergent",
                    "definition-divergent",
                    "npc divergent",
                    null,
                    NpcLifeState.Alive,
                    NpcInjurySeverity.None,
                    null,
                    null,
                    null,
                    null,
                    false,
                    null,
                    0,
                    null,
                    0f,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "person-divergent")
            },
            cities: new[]
            {
                new WorldStateCitySnapshot(
                    "city-divergent",
                    "definition-city-divergent",
                    "location-city-divergent",
                    1,
                    null,
                    MarketLiquidityMode.Open,
                    0f,
                    null,
                    null)
            },
            persons: new[]
            {
                new WorldStatePersonSnapshot(
                    "person-divergent",
                    null,
                    null,
                    null,
                    "city-divergent",
                    "npc-divergent")
            });

        WorldStateInvariantReport report = WorldStateInvariantValidator.Validate(snapshot);
        Assert.That(report.HasErrors, Is.True);
        Assert.That(ContainsIssue(report, "PersonNpcResidenceMismatch"), Is.True);
    }

    [Test]
    public void PersonResidence_IsolatedBetweenWorldsWithEqualIds()
    {
        CityRuntime firstCity = CreateCity("shared-city-id", 2);
        CityRuntime secondCity = CreateCity("shared-city-id", 2);
        SimulationRuntime firstWorld = CreateWorld(firstCity);
        SimulationRuntime secondWorld = CreateWorld(secondCity);
        PersonRuntime firstPerson = RegisterPerson(firstWorld, "shared-person-id");
        PersonRuntime secondPerson = RegisterPerson(secondWorld, "shared-person-id");

        Assert.That(firstWorld.TryBindExistingPersonResident(firstPerson.PersonId, firstCity, out _), Is.True);
        Assert.That(firstPerson.ResidenceSettlementRuntimeId, Is.EqualTo(firstCity.RuntimeId));
        Assert.That(secondPerson.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(secondWorld.TryBindExistingPersonResident(secondPerson.PersonId, secondCity, out _), Is.True);
        Assert.That(Summary(firstWorld, firstCity).RepresentedResidentCount, Is.EqualTo(1));
        Assert.That(Summary(secondWorld, secondCity).RepresentedResidentCount, Is.EqualTo(1));
    }

    private static SimulationRuntime CreateWorld(params CityRuntime[] cities)
    {
        return new SimulationRuntime(new SimulationTime(), cities, null);
    }

    private static SimulationRuntime CreateWorldWithNpcs(NpcRuntime[] npcs, params CityRuntime[] cities)
    {
        return new SimulationRuntime(new SimulationTime(), cities, npcs);
    }

    private static PersonRuntime RegisterPerson(SimulationRuntime world, string personId)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(personId));
        Assert.That(world.TryRegisterPerson(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }

    private static NpcRuntime CreateLegacyResident(CityRuntime city, string runtimeId)
    {
        NpcRuntime npc = new NpcRuntime(runtimeId, SimulationTestFactory.CreateNpc(runtimeId));
        Assert.That(SettlementPopulationMembershipSystem.TryBindExistingResident(
            city,
            npc,
            SimulationTestFactory.CreateAuthoritativeNpcRoster(new[] { npc }),
            out PopulationMembershipFailure failure), Is.True, failure.ToString());
        city.AddImportantNpc(npc);
        return npc;
    }

    private static SettlementPopulationPresenceSummary Summary(SimulationRuntime world, CityRuntime city)
    {
        return SettlementPopulationPresenceQuery.BuildSummary(
            city,
            world.NpcRuntimes,
            world.PersonStore.Persons);
    }

    private static CityRuntime CreateCity(string runtimeId, int population)
    {
        CityData data = SimulationTestFactory.CreateCityData("definition-" + runtimeId);
        data.initialPopulation = population;
        return new CityRuntime(
            runtimeId,
            data,
            new SpatialLocationRuntime("location-" + runtimeId));
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
