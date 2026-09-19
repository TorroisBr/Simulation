using System;
using System.Reflection;
using NUnit.Framework;

public sealed class PersonNamedBirthLifecycleTests
{
    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void ProposalIsPureAndCapturesWorldDayAndPopulationState()
    {
        CityRuntime city = CreateCity("birth-proposal", 10);
        SimulationRuntime world = CreateWorld(new SimulationTime(7L), city);
        int populationBefore = city.CurrentPopulation;
        long revisionBefore = city.Population.Revision;

        Assert.That(world.TryProposeNamedBirth(
            city,
            new PersonId("person-proposal"),
            out PersonBirthTransition transition,
            out PersonBirthLifecycleFailure failure), Is.True, failure.ToString());

        Assert.That(transition.BirthAbsoluteDay, Is.EqualTo(7L));
        Assert.That(transition.ExpectedAbsoluteDay, Is.EqualTo(7L));
        Assert.That(transition.ExpectedPopulationRevision, Is.EqualTo(revisionBefore));
        Assert.That(transition.PopulationBefore, Is.EqualTo(populationBefore));
        Assert.That(transition.PopulationAfter, Is.EqualTo(populationBefore + 1));
        Assert.That(world.PersonStore.CountResidents(city.RuntimeId), Is.EqualTo(0));
        Assert.That(world.PersonStore.Persons.Count, Is.EqualTo(0));
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
    }

    [Test]
    public void NamedBirthCreatesPersonResidenceAndAggregateWithoutNpc()
    {
        CityRuntime city = CreateCity("birth-success", 10);
        SimulationRuntime world = CreateWorld(new SimulationTime(), city);

        Assert.That(world.TryApplyNamedBirth(
            city,
            new PersonId("person-success"),
            out PersonBirthTransition transition,
            out PersonBirthLifecycleFailure failure), Is.True, failure.ToString());

        Assert.That(world.PersonStore.TryGet(transition.PersonId, out PersonRuntime person), Is.True);
        Assert.That(person.BirthAbsoluteDay, Is.EqualTo(0L));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(person.IsMaterialized, Is.False);
        Assert.That(world.NpcRuntimes.Count, Is.EqualTo(0));
        Assert.That(city.CurrentPopulation, Is.EqualTo(11));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));

        SettlementPopulationPresenceSummary summary = Summary(world, city);
        Assert.That(summary.RepresentedResidentCount, Is.EqualTo(1));
        Assert.That(summary.NamedResidentCount, Is.EqualTo(1));
    }

    [Test]
    public void BirthAtLongAbsoluteDayAndAgeQueryRemainValid()
    {
        const long birthDay = 1_000_000_000_000L;
        CityRuntime city = CreateCity("birth-long-day", 1);
        SimulationRuntime world = CreateWorld(new SimulationTime(birthDay), city);

        Assert.That(world.TryApplyNamedBirth(
            city,
            new PersonId("person-long-day"),
            out _,
            out PersonBirthLifecycleFailure failure), Is.True, failure.ToString());
        Assert.That(world.PersonStore.TryGet(new PersonId("person-long-day"), out PersonRuntime person), Is.True);

        Assert.That(PersonAgeQuery.TryCalculate(
            person,
            birthDay,
            new CalendarDefinition(12, 1, 30),
            out PersonAgeSnapshot age,
            out PersonAgeQueryFailure ageFailure), Is.True, ageFailure.ToString());
        Assert.That(age.AgeInDays, Is.EqualTo(0L));
        Assert.That(age.CompletedYears, Is.EqualTo(0L));
    }

    [Test]
    public void DuplicateBirthIsRejectedWithoutMutation()
    {
        CityRuntime city = CreateCity("birth-duplicate", 5);
        SimulationRuntime world = CreateWorld(new SimulationTime(), city);
        PersonId personId = new PersonId("person-duplicate");
        Assert.That(world.TryApplyNamedBirth(city, personId, out _, out _), Is.True);
        int population = city.CurrentPopulation;
        long revision = city.Population.Revision;

        Assert.That(world.TryApplyNamedBirth(
            city,
            personId,
            out PersonBirthTransition transition,
            out PersonBirthLifecycleFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(PersonBirthLifecycleFailure.PersonAlreadyExists));
        Assert.That(world.PersonStore.Persons.Count, Is.EqualTo(1));
        Assert.That(city.CurrentPopulation, Is.EqualTo(population));
        Assert.That(city.Population.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void ForeignSettlementIsRejectedAtomically()
    {
        CityRuntime owned = CreateCity("birth-owned", 5);
        CityRuntime foreign = CreateCity("birth-foreign", 8);
        SimulationRuntime world = CreateWorld(new SimulationTime(), owned);
        int foreignPopulation = foreign.CurrentPopulation;

        Assert.That(world.TryApplyNamedBirth(
            foreign,
            new PersonId("person-foreign"),
            out PersonBirthTransition transition,
            out PersonBirthLifecycleFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(PersonBirthLifecycleFailure.SettlementNotInWorld));
        Assert.That(world.PersonStore.Persons.Count, Is.EqualTo(0));
        Assert.That(owned.CurrentPopulation, Is.EqualTo(5));
        Assert.That(foreign.CurrentPopulation, Is.EqualTo(foreignPopulation));
    }

    [Test]
    public void PopulationOverflowIsRejectedAtomically()
    {
        CityRuntime city = CreateCity("birth-overflow", int.MaxValue);
        SimulationRuntime world = CreateWorld(new SimulationTime(), city);

        Assert.That(world.TryApplyNamedBirth(
            city,
            new PersonId("person-overflow"),
            out PersonBirthTransition transition,
            out PersonBirthLifecycleFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(PersonBirthLifecycleFailure.PopulationOverflow));
        Assert.That(world.PersonStore.Persons.Count, Is.EqualTo(0));
        Assert.That(city.CurrentPopulation, Is.EqualTo(int.MaxValue));
        Assert.That(city.Population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void RevisionOverflowIsRejectedAtomically()
    {
        CityRuntime city = CreateCity("birth-revision-overflow", 5);
        SimulationRuntime world = CreateWorld(new SimulationTime(), city);
        SetPrivateField(city.Population, "revision", long.MaxValue);

        Assert.That(world.TryApplyNamedBirth(
            city,
            new PersonId("person-revision-overflow"),
            out PersonBirthTransition transition,
            out PersonBirthLifecycleFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(PersonBirthLifecycleFailure.RevisionOverflow));
        Assert.That(world.PersonStore.Persons.Count, Is.EqualTo(0));
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
        Assert.That(city.Population.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void StaleDayProposalCannotApply()
    {
        CityRuntime city = CreateCity("birth-stale-day", 5);
        SimulationRuntime world = CreateWorld(new SimulationTime(), city);
        Assert.That(world.TryProposeNamedBirth(
            city,
            new PersonId("person-stale-day"),
            out PersonBirthTransition transition,
            out _), Is.True);
        world.SimulationTime.AdvanceDay();

        Assert.That(world.TryApplyNamedBirth(
            transition,
            out PersonBirthLifecycleFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(PersonBirthLifecycleFailure.StaleWorldDay));
        Assert.That(world.PersonStore.Persons.Count, Is.EqualTo(0));
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
        Assert.That(city.Population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void StalePopulationProposalCannotApply()
    {
        CityRuntime city = CreateCity("birth-stale-population", 5);
        SimulationRuntime world = CreateWorld(new SimulationTime(), city);
        Assert.That(world.TryProposeNamedBirth(
            city,
            new PersonId("person-stale-population"),
            out PersonBirthTransition transition,
            out _), Is.True);
        Assert.That(SettlementPopulationSystem.TryPropose(
            city.Population,
            new PopulationChangeSet(1, 0, 0, 0),
            out SettlementPopulationTransition aggregateTransition,
            out _), Is.True);
        Assert.That(SettlementPopulationSystem.TryApply(
            city.Population,
            aggregateTransition,
            out _), Is.True);

        Assert.That(world.TryApplyNamedBirth(
            transition,
            out PersonBirthLifecycleFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(PersonBirthLifecycleFailure.StalePopulation));
        Assert.That(world.PersonStore.Persons.Count, Is.EqualTo(0));
        Assert.That(city.CurrentPopulation, Is.EqualTo(6));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void InvalidRepresentedPopulationBlocksBirth()
    {
        CityRuntime city = CreateCity("birth-invalid-representation", 0);
        SimulationRuntime world = CreateWorld(new SimulationTime(), city);
        PersonRuntime existing = new PersonRuntime(new PersonId("existing-invalid-resident"));
        Assert.That(world.TryRegisterPerson(existing, out _), Is.True);
        SetPrivateField(existing, "residenceSettlementRuntimeId", city.RuntimeId);

        Assert.That(world.TryProposeNamedBirth(
            city,
            new PersonId("person-invalid-representation"),
            out PersonBirthTransition transition,
            out PersonBirthLifecycleFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(PersonBirthLifecycleFailure.RepresentedPopulationInvalid));
        Assert.That(world.PersonStore.Persons.Count, Is.EqualTo(1));
        Assert.That(city.CurrentPopulation, Is.EqualTo(0));
        Assert.That(city.Population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void AggregateOnlyBirthDoesNotCreatePerson()
    {
        CityRuntime city = CreateCity("birth-aggregate-only", 10);
        SettlementPopulationTransition transition;
        Assert.That(SettlementPopulationSystem.TryPropose(
            city.Population,
            new PopulationChangeSet(1, 0, 0, 0),
            out transition,
            out PopulationTransitionFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(SettlementPopulationSystem.TryApply(
            city.Population,
            transition,
            out PopulationTransitionFailure applyFailure), Is.True, applyFailure.ToString());

        Assert.That(city.CurrentPopulation, Is.EqualTo(11));
    }

    [Test]
    public void BirthThenMaterializationDoesNotDoubleCountOrChangePopulation()
    {
        CityRuntime city = CreateCity("birth-materialization", 5);
        SimulationRuntime world = CreateWorld(new SimulationTime(), city);
        PersonId personId = new PersonId("person-materialization");
        Assert.That(world.TryApplyNamedBirth(city, personId, out _, out _), Is.True);
        int populationBefore = city.CurrentPopulation;
        long revisionBefore = city.Population.Revision;

        Assert.That(world.TryMaterializePerson(
            personId,
            SimulationTestFactory.CreateNpc("birth-materialization-definition"),
            "npc-birth-materialization",
            null,
            0f,
            out NpcRuntime npc,
            out PersonMaterializationFailure failure), Is.True, failure.ToString());

        Assert.That(npc, Is.Not.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
        Assert.That(Summary(world, city).RepresentedResidentCount, Is.EqualTo(1));
        Assert.That(Summary(world, city).MaterializedResidentCount, Is.EqualTo(1));
    }

    [Test]
    public void BirthDiagnosticsExposePersonAndAggregateDelta()
    {
        CityRuntime city = CreateCity("birth-diagnostics", 5);
        SimulationRuntime world = CreateWorld(new SimulationTime(), city);
        WorldStateSnapshot before = Snapshot(world);

        Assert.That(world.TryApplyNamedBirth(
            city,
            new PersonId("person-diagnostics"),
            out _,
            out PersonBirthLifecycleFailure failure), Is.True, failure.ToString());

        WorldStateSnapshot after = Snapshot(world);
        WorldStateDiff diff = WorldStateDiff.Compare(before, after);
        Assert.That(HasDifference(diff, "Person", "person-diagnostics", "Entity", WorldStateDifferenceChangeKind.Added), Is.True);
        Assert.That(HasDifference(diff, "City", city.RuntimeId, "CurrentPopulation", WorldStateDifferenceChangeKind.Changed), Is.True);
        Assert.That(HasDifference(diff, "City", city.RuntimeId, "PopulationRevision", WorldStateDifferenceChangeKind.Changed), Is.True);
        Assert.That(after.PersonCount, Is.EqualTo(1));
        Assert.That(after.Persons[0].BirthAbsoluteDay, Is.EqualTo(0L));
        Assert.That(after.Persons[0].ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
    }

    [Test]
    public void BirthTransitionFactsAreReadOnlyAndCannotBePubliclyConstructed()
    {
        Assert.That(typeof(PersonBirthTransition).GetConstructor(
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(PersonId), typeof(string), typeof(long), typeof(long), typeof(int), typeof(int) },
            null), Is.Null);
        Assert.That(typeof(PersonBirthTransition).GetProperty("PopulationBefore").CanWrite, Is.False);
        Assert.That(typeof(PersonBirthTransition).GetProperty("PopulationAfter").CanWrite, Is.False);
        Assert.That(typeof(PersonBirthTransition).GetProperty("BirthAbsoluteDay").CanWrite, Is.False);
    }

    private static SimulationRuntime CreateWorld(SimulationTime time, params CityRuntime[] cities)
    {
        return new SimulationRuntime(time, cities, null, economyEnabled: false);
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

    private static SettlementPopulationPresenceSummary Summary(
        SimulationRuntime world,
        CityRuntime city)
    {
        return SettlementPopulationPresenceQuery.BuildSummary(
            city,
            world.NpcRuntimes,
            world.PersonStore.Persons);
    }

    private static WorldStateSnapshot Snapshot(SimulationRuntime world)
    {
        return WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: world.SimulationTime,
            cities: world.Cities,
            npcs: world.NpcRuntimes,
            personStore: world.PersonStore));
    }

    private static bool HasDifference(
        WorldStateDiff diff,
        string section,
        string identity,
        string field,
        WorldStateDifferenceChangeKind kind)
    {
        foreach (WorldStateDifference difference in diff.Differences)
        {
            if (difference.Section == section
                && difference.Identity == identity
                && difference.Field == field
                && difference.ChangeKind == kind)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(instance, value);
    }
}
