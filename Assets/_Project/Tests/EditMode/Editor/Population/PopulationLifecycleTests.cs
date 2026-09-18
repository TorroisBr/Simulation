using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;

public sealed class PopulationLifecycleTests
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
    public void SimulationRuntimeOwnsFreshAuthoritativeRosterSnapshots()
    {
        SimulationRuntime world = CreateWorld();
        NpcRuntime first = CreateNpc("lifecycle-owner-first");
        NpcRuntime second = CreateNpc("lifecycle-owner-second");

        Assert.That(world.TryRegisterNpc(first, out WorldNpcRegistryFailure firstFailure), Is.True, firstFailure.ToString());
        AuthoritativeNpcRoster firstSnapshot = world.GetAuthoritativeNpcRoster();
        Assert.That(world.TryRegisterNpc(second, out WorldNpcRegistryFailure secondFailure), Is.True, secondFailure.ToString());
        AuthoritativeNpcRoster secondSnapshot = world.GetAuthoritativeNpcRoster();

        Assert.That(firstSnapshot.Npcs.Count, Is.EqualTo(1));
        Assert.That(secondSnapshot.Npcs.Count, Is.EqualTo(2));
        Assert.That(world.NpcRuntimes.Count, Is.EqualTo(2));
        Assert.That(world.NpcRuntimes, Is.TypeOf<ReadOnlyCollection<NpcRuntime>>());
        Assert.Throws<NotSupportedException>(() => ((IList<NpcRuntime>)world.NpcRuntimes).Add(CreateNpc("not-registered")));
    }

    [Test]
    public void SimulationRuntimeRejectsNullAndDuplicateRegistrations()
    {
        SimulationRuntime world = CreateWorld();
        NpcRuntime npc = CreateNpc("lifecycle-duplicate");

        Assert.That(world.TryRegisterNpc(null, out WorldNpcRegistryFailure nullFailure), Is.False);
        Assert.That(nullFailure, Is.EqualTo(WorldNpcRegistryFailure.InvalidNpc));
        Assert.That(world.TryRegisterNpc(npc, out WorldNpcRegistryFailure firstFailure), Is.True, firstFailure.ToString());
        Assert.That(world.TryRegisterNpc(npc, out WorldNpcRegistryFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure, Is.EqualTo(WorldNpcRegistryFailure.DuplicateRuntimeId));
        Assert.That(world.TryRegisterNpc(CreateNpc("lifecycle-duplicate"), out WorldNpcRegistryFailure duplicateIdFailure), Is.False);
        Assert.That(duplicateIdFailure, Is.EqualTo(WorldNpcRegistryFailure.DuplicateRuntimeId));
    }

    [Test]
    public void NullAuthoritativeRosterIsRejected()
    {
        CityRuntime city = CreateCity("lifecycle-null-roster-city", 5);
        NpcRuntime npc = CreateNpc("lifecycle-null-roster-npc");

        bool proposed = NpcPopulationLifecycleSystem.TryProposeImmigration(
            npc,
            city,
            null,
            out _,
            out NpcPopulationLifecycleFailure failure);

        Assert.That(proposed, Is.False);
        Assert.That(failure, Is.EqualTo(NpcPopulationLifecycleFailure.AuthoritativeRosterRequired));
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.Null);
    }

    [Test]
    public void ImmigrationIncrementsAggregateOnceAndAssignsResidence()
    {
        CityRuntime city = CreateCity("lifecycle-immigration-city", 5);
        NpcRuntime npc = CreateNpc("lifecycle-immigration-npc");
        SimulationRuntime world = CreateWorld(city, npc);

        bool applied = world.TryApplyImmigration(
            npc,
            city,
            out NpcPopulationLifecycleTransition transition,
            out NpcPopulationLifecycleFailure failure);

        Assert.That(applied, Is.True, failure.ToString());
        Assert.That(transition.Operation, Is.EqualTo(NpcPopulationLifecycleOperation.Immigration));
        Assert.That(transition.PopulationBefore, Is.EqualTo(5));
        Assert.That(transition.PopulationAfter, Is.EqualTo(6));
        Assert.That(city.CurrentPopulation, Is.EqualTo(6));
        Assert.That(city.Population.Revision, Is.EqualTo(1));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(world.GetAuthoritativeNpcRoster().Npcs.Count, Is.EqualTo(1));
    }

    [Test]
    public void ImmigrationOverflowIsRejectedWithoutMutation()
    {
        CityRuntime city = CreateCity("lifecycle-immigration-overflow-city", int.MaxValue);
        NpcRuntime npc = CreateNpc("lifecycle-immigration-overflow-npc");
        SimulationRuntime world = CreateWorld(city, npc);

        bool applied = world.TryApplyImmigration(
            npc,
            city,
            out _,
            out NpcPopulationLifecycleFailure failure);

        Assert.That(applied, Is.False);
        Assert.That(failure, Is.EqualTo(NpcPopulationLifecycleFailure.PopulationOverflow));
        Assert.That(city.CurrentPopulation, Is.EqualTo(int.MaxValue));
        Assert.That(city.Population.Revision, Is.EqualTo(0));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.Null);
    }

    [Test]
    public void EmigrationDecrementsAggregateAndKeepsNpcWorldRegistered()
    {
        CityRuntime city = CreateCity("lifecycle-emigration-city", 5);
        NpcRuntime npc = CreateNpc("lifecycle-emigration-npc");
        BindExistingResident(city, npc, new[] { npc });
        SimulationRuntime world = CreateWorld(city, npc);

        bool applied = world.TryApplyEmigration(
            npc,
            city,
            out NpcPopulationLifecycleTransition transition,
            out NpcPopulationLifecycleFailure failure);

        Assert.That(applied, Is.True, failure.ToString());
        Assert.That(transition.Operation, Is.EqualTo(NpcPopulationLifecycleOperation.Emigration));
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
        Assert.That(city.Population.Revision, Is.EqualTo(1));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(world.GetAuthoritativeNpcRoster().Npcs, Has.Count.EqualTo(1));
        Assert.That(world.TryUnregisterNpc(npc.RuntimeId, out WorldNpcRegistryFailure unregisterFailure), Is.True, unregisterFailure.ToString());
    }

    [Test]
    public void ResidentCannotBeUnregisteredBeforeEmigration()
    {
        CityRuntime city = CreateCity("lifecycle-registered-resident-city", 5);
        NpcRuntime npc = CreateNpc("lifecycle-registered-resident-npc");
        BindExistingResident(city, npc, new[] { npc });
        SimulationRuntime world = CreateWorld(city, npc);

        Assert.That(world.TryUnregisterNpc(npc.RuntimeId, out WorldNpcRegistryFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(WorldNpcRegistryFailure.NpcHasResidence));
        Assert.That(world.GetAuthoritativeNpcRoster().Npcs, Has.Count.EqualTo(1));
    }

    [Test]
    public void ResidentDeathDecrementsOnceClearsMembershipAndKeepsWorldEntity()
    {
        CityRuntime city = CreateCity("lifecycle-death-city", 5);
        NpcRuntime npc = CreateNpc("lifecycle-death-npc");
        BindExistingResident(city, npc, new[] { npc });
        SimulationRuntime world = CreateWorld(city, npc);

        bool applied = world.TryApplyResidentDeath(
            npc,
            city,
            out NpcPopulationLifecycleTransition transition,
            out NpcPopulationLifecycleFailure failure);

        Assert.That(applied, Is.True, failure.ToString());
        Assert.That(transition.Operation, Is.EqualTo(NpcPopulationLifecycleOperation.ResidentDeath));
        Assert.That(npc.IsDead, Is.True);
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
        Assert.That(city.Population.Revision, Is.EqualTo(1));
        Assert.That(SettlementPopulationPresenceQuery.BuildSummary(city, world.NpcRuntimes).NamedResidentCount, Is.EqualTo(0));
        Assert.That(world.GetAuthoritativeNpcRoster().Npcs, Has.Count.EqualTo(1));

        bool repeated = world.TryApplyResidentDeath(
            npc,
            city,
            out _,
            out NpcPopulationLifecycleFailure repeatedFailure);

        Assert.That(repeated, Is.False);
        Assert.That(repeatedFailure, Is.EqualTo(NpcPopulationLifecycleFailure.DeadNpc));
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
        Assert.That(city.Population.Revision, Is.EqualTo(1));
    }

    [Test]
    public void DirectResidentDeathCannotBypassPopulationLifecycle()
    {
        CityRuntime city = CreateCity("lifecycle-direct-death-city", 5);
        NpcRuntime npc = CreateNpc("lifecycle-direct-death-npc");
        BindExistingResident(city, npc, new[] { npc });

        Assert.That(npc.TryApplyDeath(), Is.False);
        Assert.That(npc.IsAlive, Is.True);
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
        Assert.That(city.Population.Revision, Is.EqualTo(0));
    }

    [Test]
    public void StaleLifecycleApplyIsAtomic()
    {
        CityRuntime city = CreateCity("lifecycle-stale-city", 5);
        NpcRuntime npc = CreateNpc("lifecycle-stale-npc");
        SimulationRuntime world = CreateWorld(city, npc);
        AuthoritativeNpcRoster roster = world.GetAuthoritativeNpcRoster();

        Assert.That(
            NpcPopulationLifecycleSystem.TryProposeImmigration(
                npc,
                city,
                roster,
                out NpcPopulationLifecycleTransition transition,
                out NpcPopulationLifecycleFailure proposalFailure),
            Is.True,
            proposalFailure.ToString());

        Assert.That(
            SettlementPopulationSystem.TryPropose(
                city.Population,
                new PopulationChangeSet(0, 0, 1, 0),
                out SettlementPopulationTransition unrelatedTransition,
                out PopulationTransitionFailure unrelatedFailure),
            Is.True,
            unrelatedFailure.ToString());
        Assert.That(SettlementPopulationSystem.TryApply(city.Population, unrelatedTransition, out unrelatedFailure), Is.True);

        bool applied = NpcPopulationLifecycleSystem.TryApply(
            npc,
            city,
            roster,
            transition,
            out NpcPopulationLifecycleFailure failure);

        Assert.That(applied, Is.False);
        Assert.That(failure, Is.EqualTo(NpcPopulationLifecycleFailure.StaleState));
        Assert.That(city.CurrentPopulation, Is.EqualTo(6));
        Assert.That(city.Population.Revision, Is.EqualTo(1));
        Assert.That(npc.IsAlive, Is.True);
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.Null);
    }

    [Test]
    public void FatalConflictConsequenceCannotKillResidentOutsideLifecycle()
    {
        CityRuntime city = CreateCity("lifecycle-conflict-city", 5);
        NpcRuntime npc = CreateNpc("lifecycle-conflict-npc");
        BindExistingResident(city, npc, new[] { npc });
        Assert.That(npc.CanApplyConflictConsequence(NpcInjurySeverity.Hurt, true), Is.False);
        Assert.That(npc.ApplyConflictConsequence(NpcInjurySeverity.Hurt, true), Is.False);
        Assert.That(npc.IsAlive, Is.True);
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
        Assert.That(city.Population.Revision, Is.EqualTo(0));
    }

    private static SimulationRuntime CreateWorld(params object[] values)
    {
        List<CityRuntime> cities = new List<CityRuntime>();
        List<NpcRuntime> npcs = new List<NpcRuntime>();

        foreach (object value in values)
        {
            if (value is CityRuntime city)
            {
                cities.Add(city);
            }
            else if (value is NpcRuntime npc)
            {
                npcs.Add(npc);
            }
        }

        return new SimulationRuntime(new SimulationTime(), cities, npcs, economyEnabled: false);
    }

    private static CityRuntime CreateCity(string runtimeId, int population)
    {
        CityData cityData = SimulationTestFactory.CreateCityData("definition-" + runtimeId);
        cityData.initialPopulation = population;
        return new CityRuntime(
            runtimeId,
            cityData,
            new SpatialLocationRuntime("location-" + runtimeId));
    }

    private static NpcRuntime CreateNpc(string runtimeId)
    {
        return new NpcRuntime(runtimeId, SimulationTestFactory.CreateNpc(runtimeId));
    }

    private static void BindExistingResident(CityRuntime city, NpcRuntime npc, IEnumerable<NpcRuntime> roster)
    {
        bool bound = SettlementPopulationMembershipSystem.TryBindExistingResident(
            city,
            npc,
            SimulationTestFactory.CreateAuthoritativeNpcRoster(roster),
            out PopulationMembershipFailure failure);
        Assert.That(bound, Is.True, failure.ToString());
    }

}
