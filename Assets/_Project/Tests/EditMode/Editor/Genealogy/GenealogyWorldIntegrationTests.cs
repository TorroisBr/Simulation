using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class GenealogyWorldIntegrationTests
{
    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void WorldOwnsIndependentGenealogyAndExposesOnlyReadQueries()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime parent = Register(persons, "parent");
        PersonRuntime child = Register(persons, "child");
        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(parent.PersonId, child.PersonId, out _), Is.True);

        SimulationRuntime world = CreateWorld(
            new[] { CreateCity("world-owner", 1) },
            persons,
            genealogy);

        Assert.That(world.GenealogyRecords, Has.Count.EqualTo(1));
        Assert.That(world.ContainsParentage(parent.PersonId, child.PersonId), Is.True);
        Assert.That(world.GetGenealogyParents(child.PersonId), Has.Count.EqualTo(1));
        Assert.That(typeof(SimulationRuntime).GetProperty("GenealogyStore"), Is.Null);
    }

    [Test]
    public void InjectedGenealogyWithMissingEndpointRejectsWorldConstruction()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime parent = Register(persons, "parent-present");
        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(
            parent.PersonId,
            new PersonId("child-missing"),
            out _), Is.True);

        Assert.Throws<ArgumentException>(() => CreateWorld(
            new[] { CreateCity("invalid-world", 1) },
            persons,
            genealogy));
    }

    [Test]
    public void WorldParentageValidatesMembershipAndDelegatesGraphRules()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime parent = Register(persons, "parent");
        PersonRuntime child = Register(persons, "child");
        SimulationRuntime world = CreateWorld(new[] { CreateCity("membership", 2) }, persons);

        Assert.That(world.TryAddParentage(
            parent.PersonId,
            child.PersonId,
            out PersonGenealogyFailure addFailure), Is.True, addFailure.ToString());
        Assert.That(world.TryAddParentage(
            new PersonId("parent"),
            new PersonId("child"),
            out PersonGenealogyFailure duplicate), Is.False);
        Assert.That(duplicate, Is.EqualTo(PersonGenealogyFailure.DuplicateParentage));

        Assert.That(world.TryRemoveParentage(
            parent.PersonId,
            child.PersonId,
            out PersonGenealogyFailure removeFailure), Is.True, removeFailure.ToString());
        Assert.That(world.PersonStore.TryGet(parent.PersonId, out _), Is.True);
        Assert.That(world.PersonStore.TryGet(child.PersonId, out _), Is.True);
    }

    [Test]
    public void UnknownWorldParentAndChildAreRejectedWithoutStoreMutation()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime registered = Register(persons, "registered");
        SimulationRuntime world = CreateWorld(new[] { CreateCity("unknown-endpoints", 2) }, persons);

        Assert.That(world.TryAddParentage(
            new PersonId("missing-parent"),
            registered.PersonId,
            out PersonGenealogyFailure parentFailure), Is.False);
        Assert.That(parentFailure, Is.EqualTo(PersonGenealogyFailure.ParentNotRegistered));
        Assert.That(world.TryAddParentage(
            registered.PersonId,
            new PersonId("missing-child"),
            out PersonGenealogyFailure childFailure), Is.False);
        Assert.That(childFailure, Is.EqualTo(PersonGenealogyFailure.ChildNotRegistered));
        Assert.That(world.GenealogyRecords, Is.Empty);
    }

    [Test]
    public void WorldCycleIsRejectedAndExistingEdgesRemain()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime first = Register(persons, "a");
        PersonRuntime second = Register(persons, "b");
        PersonRuntime third = Register(persons, "c");
        SimulationRuntime world = CreateWorld(new[] { CreateCity("cycle", 3) }, persons);

        Assert.That(world.TryAddParentage(first.PersonId, second.PersonId, out _), Is.True);
        Assert.That(world.TryAddParentage(second.PersonId, third.PersonId, out _), Is.True);
        Assert.That(world.TryAddParentage(
            third.PersonId,
            first.PersonId,
            out PersonGenealogyFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(PersonGenealogyFailure.WouldCreateCycle));
        Assert.That(world.GenealogyRecords, Has.Count.EqualTo(2));
    }

    [Test]
    public void WorldsWithEqualPersonIdsRemainIsolated()
    {
        PersonStore firstPersons = new PersonStore();
        PersonStore secondPersons = new PersonStore();
        PersonRuntime firstParent = Register(firstPersons, "same-parent");
        PersonRuntime firstChild = Register(firstPersons, "same-child");
        Register(secondPersons, "same-parent");
        Register(secondPersons, "same-child");
        SimulationRuntime first = CreateWorld(new[] { CreateCity("world-a", 2) }, firstPersons);
        SimulationRuntime second = CreateWorld(new[] { CreateCity("world-b", 2) }, secondPersons);

        Assert.That(first.TryAddParentage(firstParent.PersonId, firstChild.PersonId, out _), Is.True);
        Assert.That(first.GenealogyRecords, Has.Count.EqualTo(1));
        Assert.That(second.GenealogyRecords, Is.Empty);
    }

    [Test]
    public void ParentAwareProposalIsPureDeterministicAndSnapshotsInput()
    {
        CityRuntime city = CreateCity("proposal-parents", 10);
        PersonStore persons = new PersonStore();
        Register(persons, "parent-b");
        Register(persons, "parent-a");
        SimulationRuntime world = CreateWorld(new[] { city }, persons);
        List<PersonId> input = new List<PersonId>
        {
            new PersonId("parent-b"),
            new PersonId("parent-a")
        };
        int populationBefore = city.CurrentPopulation;

        Assert.That(world.TryProposeNamedBirth(
            city,
            new PersonId("child-proposed"),
            input,
            out PersonBirthTransition transition,
            out PersonBirthLifecycleFailure failure), Is.True, failure.ToString());
        input.Clear();

        Assert.That(transition.ParentIds, Has.Count.EqualTo(2));
        Assert.That(transition.ParentIds[0].Value, Is.EqualTo("parent-a"));
        Assert.That(transition.ParentIds[1].Value, Is.EqualTo("parent-b"));
        Assert.That(world.GenealogyRecords, Is.Empty);
        Assert.That(world.PersonStore.TryGet(new PersonId("child-proposed"), out _), Is.False);
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void ParentAwareBirthSupportsThreeParentsAndCreatesNoNpc()
    {
        CityRuntime city = CreateCity("three-parent-birth", 10);
        PersonStore persons = new PersonStore();
        Register(persons, "parent-a");
        Register(persons, "parent-b");
        Register(persons, "parent-c");
        SimulationRuntime world = CreateWorld(new[] { city }, persons);

        Assert.That(world.TryApplyNamedBirth(
            city,
            new PersonId("child-three"),
            new[]
            {
                new PersonId("parent-c"),
                new PersonId("parent-a"),
                new PersonId("parent-b")
            },
            out PersonBirthTransition transition,
            out PersonBirthLifecycleFailure failure), Is.True, failure.ToString());

        Assert.That(transition.ParentIds, Has.Count.EqualTo(3));
        Assert.That(world.GetGenealogyParents(new PersonId("child-three")), Has.Count.EqualTo(3));
        Assert.That(world.GetGenealogyParents(new PersonId("child-three"))[0].Value, Is.EqualTo("parent-a"));
        Assert.That(world.PersonStore.TryGet(new PersonId("child-three"), out PersonRuntime child), Is.True);
        Assert.That(child.BirthAbsoluteDay, Is.EqualTo(world.CurrentDay));
        Assert.That(child.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(child.IsMaterialized, Is.False);
        Assert.That(world.NpcRuntimes, Is.Empty);
        Assert.That(city.CurrentPopulation, Is.EqualTo(11));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void DuplicateOrNullParentInputFailsWithoutMutation()
    {
        CityRuntime city = CreateCity("duplicate-parent-input", 4);
        PersonStore persons = new PersonStore();
        Register(persons, "parent-a");
        Register(persons, "parent-b");
        SimulationRuntime world = CreateWorld(new[] { city }, persons);

        Assert.That(world.TryApplyNamedBirth(
            city,
            new PersonId("child-duplicate"),
            new[] { new PersonId("parent-a"), new PersonId("parent-b"), new PersonId("parent-a") },
            out _,
            out PersonBirthLifecycleFailure duplicate), Is.False);
        Assert.That(duplicate, Is.EqualTo(PersonBirthLifecycleFailure.DuplicateParentInput));

        Assert.That(world.TryApplyNamedBirth(
            city,
            new PersonId("child-null"),
            null,
            out _,
            out PersonBirthLifecycleFailure nullFailure), Is.False);
        Assert.That(nullFailure, Is.EqualTo(PersonBirthLifecycleFailure.InvalidParentCollection));
        Assert.That(world.PersonStore.Persons, Has.Count.EqualTo(2));
        Assert.That(world.GenealogyRecords, Is.Empty);
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
    }

    [Test]
    public void UnknownParentBirthFailsBeforePersonPopulationOrGenealogyMutation()
    {
        CityRuntime city = CreateCity("unknown-parent-birth", 4);
        PersonStore persons = new PersonStore();
        Register(persons, "parent-known");
        SimulationRuntime world = CreateWorld(new[] { city }, persons);
        int populationBefore = city.CurrentPopulation;

        Assert.That(world.TryApplyNamedBirth(
            city,
            new PersonId("child-unknown-parent"),
            new[] { new PersonId("parent-known"), new PersonId("parent-missing") },
            out _,
            out PersonBirthLifecycleFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(PersonBirthLifecycleFailure.ParentNotRegistered));
        Assert.That(world.PersonStore.TryGet(new PersonId("child-unknown-parent"), out _), Is.False);
        Assert.That(world.GenealogyRecords, Is.Empty);
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void ZeroParentOverloadRemainsNamedBirthWithoutEdges()
    {
        CityRuntime city = CreateCity("zero-parent-birth", 4);
        SimulationRuntime world = CreateWorld(new[] { city });

        Assert.That(world.TryApplyNamedBirth(
            city,
            new PersonId("child-zero"),
            Array.Empty<PersonId>(),
            out _,
            out PersonBirthLifecycleFailure failure), Is.True, failure.ToString());
        Assert.That(world.PersonStore.TryGet(new PersonId("child-zero"), out _), Is.True);
        Assert.That(world.GenealogyRecords, Is.Empty);
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
    }

    [Test]
    public void StaleParentAwareBirthLeavesNoPersonOrEdges()
    {
        CityRuntime city = CreateCity("stale-parent-birth", 4);
        PersonStore persons = new PersonStore();
        Register(persons, "parent-a");
        Register(persons, "parent-b");
        SimulationRuntime world = CreateWorld(new[] { city }, persons);
        Assert.That(world.TryProposeNamedBirth(
            city,
            new PersonId("child-stale"),
            new[] { new PersonId("parent-a"), new PersonId("parent-b") },
            out PersonBirthTransition transition,
            out _), Is.True);
        world.SimulationTime.AdvanceDay();

        Assert.That(world.TryApplyNamedBirth(
            transition,
            out PersonBirthLifecycleFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PersonBirthLifecycleFailure.StaleWorldDay));
        Assert.That(world.GenealogyRecords, Is.Empty);
        Assert.That(world.PersonStore.TryGet(new PersonId("child-stale"), out _), Is.False);
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
    }

    [Test]
    public void StalePopulationParentAwareBirthLeavesNoPersonOrEdges()
    {
        CityRuntime city = CreateCity("stale-parent-population", 4);
        PersonStore persons = new PersonStore();
        Register(persons, "parent-a");
        Register(persons, "parent-b");
        SimulationRuntime world = CreateWorld(new[] { city }, persons);
        Assert.That(world.TryProposeNamedBirth(
            city,
            new PersonId("child-stale-population"),
            new[] { new PersonId("parent-a"), new PersonId("parent-b") },
            out PersonBirthTransition transition,
            out _), Is.True);
        Assert.That(SettlementPopulationSystem.TryPropose(
            city.Population,
            new PopulationChangeSet(1, 0, 0, 0),
            out SettlementPopulationTransition aggregateTransition,
            out _), Is.True);
        Assert.That(SettlementPopulationSystem.TryApply(city.Population, aggregateTransition, out _), Is.True);

        Assert.That(world.TryApplyNamedBirth(
            transition,
            out PersonBirthLifecycleFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PersonBirthLifecycleFailure.StalePopulation));
        Assert.That(world.GenealogyRecords, Is.Empty);
        Assert.That(world.PersonStore.TryGet(new PersonId("child-stale-population"), out _), Is.False);
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void WorldGenealogyDoesNotAliasInjectedStoreAfterConstruction()
    {
        CityRuntime city = CreateCity("genealogy-alias-isolation", 4);
        PersonStore persons = new PersonStore();
        PersonRuntime parent = Register(persons, "alias-parent");
        PersonRuntime firstChild = Register(persons, "alias-child-a");
        PersonRuntime secondChild = Register(persons, "alias-child-b");
        GenealogyStore injected = new GenealogyStore();
        Assert.That(injected.TryAddParentage(parent.PersonId, firstChild.PersonId, out _), Is.True);

        SimulationRuntime world = CreateWorld(new[] { city }, persons, injected);

        Assert.That(world.ContainsParentage(parent.PersonId, firstChild.PersonId), Is.True);
        Assert.That(injected.TryRemoveParentage(parent.PersonId, firstChild.PersonId, out _), Is.True);
        Assert.That(world.ContainsParentage(parent.PersonId, firstChild.PersonId), Is.True);

        Assert.That(injected.TryAddParentage(parent.PersonId, secondChild.PersonId, out _), Is.True);
        Assert.That(injected.ContainsParentage(parent.PersonId, secondChild.PersonId), Is.True);
        Assert.That(world.ContainsParentage(parent.PersonId, secondChild.PersonId), Is.False);
        Assert.That(world.GenealogyRecords, Has.Count.EqualTo(1));
    }

    [Test]
    public void ParentageRemovalDoesNotRemovePersons()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime parent = Register(persons, "remove-parent");
        PersonRuntime child = Register(persons, "remove-child");
        SimulationRuntime world = CreateWorld(new[] { CreateCity("remove-edge", 2) }, persons);
        Assert.That(world.TryAddParentage(parent.PersonId, child.PersonId, out _), Is.True);

        Assert.That(world.TryRemoveParentage(
            parent.PersonId,
            child.PersonId,
            out PersonGenealogyFailure failure), Is.True, failure.ToString());
        Assert.That(world.GenealogyRecords, Is.Empty);
        Assert.That(world.PersonStore.Persons, Has.Count.EqualTo(2));
    }

    [Test]
    public void ParentAwareBirthAppearsInSnapshotCanonicalFormatterAndDiff()
    {
        CityRuntime city = CreateCity("diagnostic-parent-birth", 4);
        PersonStore persons = new PersonStore();
        Register(persons, "parent-a");
        Register(persons, "parent-b");
        SimulationRuntime world = CreateWorld(new[] { city }, persons);
        WorldStateSnapshot before = Snapshot(world, city);

        Assert.That(world.TryApplyNamedBirth(
            city,
            new PersonId("child-diagnostic"),
            new[] { new PersonId("parent-b"), new PersonId("parent-a") },
            out _,
            out PersonBirthLifecycleFailure failure), Is.True, failure.ToString());

        WorldStateSnapshot after = Snapshot(world, city);
        string canonical = WorldStateCanonicalWriter.Write(after);
        string formatted = WorldStateSnapshotFormatter.Format(after);
        WorldStateDiff added = WorldStateDiff.Compare(before, after);
        Assert.That(after.Parentages, Has.Count.EqualTo(2));
        Assert.That(canonical, Does.Contain("PARENTAGE|parent-a|child-diagnostic"));
        Assert.That(formatted, Does.Contain("PARENTAGE parent-a -> child-diagnostic"));
        Assert.That(HasDifference(added, "Parentage", "parent-a -> child-diagnostic", WorldStateDifferenceChangeKind.Added), Is.True);
        Assert.That(HasDifference(added, "Parentage", "parent-b -> child-diagnostic", WorldStateDifferenceChangeKind.Added), Is.True);

        Assert.That(world.TryRemoveParentage(
            new PersonId("parent-a"),
            new PersonId("child-diagnostic"),
            out _), Is.True);
        WorldStateDiff removed = WorldStateDiff.Compare(after, Snapshot(world, city));
        Assert.That(HasDifference(removed, "Parentage", "parent-a -> child-diagnostic", WorldStateDifferenceChangeKind.Removed), Is.True);
    }

    [Test]
    public void DiagnosticsValidatorDetectsMissingDuplicateSelfAndCycleRelations()
    {
        WorldStateSnapshot invalid = new WorldStateSnapshot(
            0L,
            persons: new[]
            {
                new WorldStatePersonSnapshot("a", null),
                new WorldStatePersonSnapshot("b", null)
            },
            parentages: new[]
            {
                new WorldStateParentageSnapshot("missing", "a"),
                new WorldStateParentageSnapshot("a", "missing"),
                new WorldStateParentageSnapshot("a", "a"),
                new WorldStateParentageSnapshot("a", "b"),
                new WorldStateParentageSnapshot("a", "b"),
                new WorldStateParentageSnapshot("b", "a")
            });

        WorldStateInvariantReport report = WorldStateInvariantValidator.Validate(invalid);
        Assert.That(report.HasErrors, Is.True);
        Assert.That(ContainsIssue(report, "GenealogyParentMissing"), Is.True);
        Assert.That(ContainsIssue(report, "GenealogyChildMissing"), Is.True);
        Assert.That(ContainsIssue(report, "GenealogySelfParent"), Is.True);
        Assert.That(ContainsIssue(report, "GenealogyDuplicateParentage"), Is.True);
        Assert.That(ContainsIssue(report, "GenealogyCycle"), Is.True);
    }

    [Test]
    public void AggregateOnlyBirthDoesNotCreatePersonOrGenealogy()
    {
        CityRuntime city = CreateCity("aggregate-only-genealogy", 4);
        SimulationRuntime world = CreateWorld(new[] { city });
        Assert.That(SettlementPopulationSystem.TryPropose(
            city.Population,
            new PopulationChangeSet(1, 0, 0, 0),
            out SettlementPopulationTransition transition,
            out _), Is.True);
        Assert.That(SettlementPopulationSystem.TryApply(city.Population, transition, out _), Is.True);

        Assert.That(world.PersonStore.Persons, Is.Empty);
        Assert.That(world.GenealogyRecords, Is.Empty);
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
    }

    private static SimulationRuntime CreateWorld(
        CityRuntime[] cities,
        PersonStore personStore = null,
        GenealogyStore genealogyStore = null)
    {
        return new SimulationRuntime(
            new SimulationTime(),
            cities,
            null,
            economyEnabled: false,
            personStore: personStore,
            genealogyStore: genealogyStore);
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

    private static PersonRuntime Register(PersonStore store, string personId)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(personId));
        Assert.That(store.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }

    private static WorldStateSnapshot Snapshot(SimulationRuntime world, CityRuntime city)
    {
        return WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: world.SimulationTime,
            cities: new[] { city },
            personStore: world.PersonStore,
            parentages: world.GenealogyRecords));
    }

    private static bool HasDifference(
        WorldStateDiff diff,
        string section,
        string identity,
        WorldStateDifferenceChangeKind kind)
    {
        foreach (WorldStateDifference difference in diff.Differences)
        {
            if (difference.Section == section
                && difference.Identity == identity
                && difference.ChangeKind == kind)
            {
                return true;
            }
        }

        return false;
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
