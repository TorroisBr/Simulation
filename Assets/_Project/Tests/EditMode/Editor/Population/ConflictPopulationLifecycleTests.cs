using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class ConflictPopulationLifecycleTests
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
    public void FatalResidentConflictUsesLifecycleAndKeepsNpcWorldRegistered()
    {
        CityRuntime city = CreateCity("fatal-resident-city", 5);
        NpcRuntime resident = CreateNpc("fatal-resident");
        NpcRuntime opponent = CreateNpc("fatal-resident-opponent");
        BindExistingResident(city, resident, new[] { resident, opponent });
        SimulationRuntime world = CreateWorld(new[] { city }, resident, opponent);
        NpcConflictConsequenceSystem applier = CreateApplier(world);
        Conflict conflict = CreateConflict(resident, opponent, ConflictStakes.Existential);
        ConflictResolutionConstraints constraints = ForceDeathFor(resident, NpcInjurySeverity.SeriouslyInjured);
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(opponent.RuntimeId)
        {
            ForceAlive = true
        });

        bool applied = applier.TryResolveAndApply(
            conflict,
            constraints,
            out ConflictResolutionResult result,
            out string reason);

        Assert.That(applied, Is.True, reason);
        Assert.That(result.NpcConsequences.FindByRuntimeId(resident.RuntimeId).IsDead, Is.True);
        Assert.That(resident.IsDead, Is.True);
        Assert.That(resident.InjurySeverity, Is.EqualTo(NpcInjurySeverity.SeriouslyInjured));
        Assert.That(resident.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
        Assert.That(city.Population.Revision, Is.EqualTo(1));
        Assert.That(world.GetAuthoritativeNpcRoster().Npcs, Does.Contain(resident));
    }

    [Test]
    public void FatalResidentConflictRecordsPersonDeathThroughWorldAuthority()
    {
        CityRuntime city = CreateCity("fatal-person-resident-city", 5);
        NpcRuntime opponent = CreateNpc("fatal-person-opponent");
        SimulationRuntime world = CreateWorld(new[] { city }, opponent);
        PersonId personId = new PersonId("fatal-person-resident");
        Assert.That(world.TryRegisterPerson(new PersonRuntime(personId, 0L), out _), Is.True);
        Assert.That(world.TryBindExistingPersonResident(personId, city, out _), Is.True);
        Assert.That(world.TryMaterializePerson(
            personId,
            SimulationTestFactory.CreateNpc("fatal-person-definition"),
            "fatal-person-npc",
            null,
            0f,
            out NpcRuntime resident,
            out _), Is.True);
        NpcConflictConsequenceSystem applier = CreateApplier(world);
        Conflict conflict = CreateConflict(resident, opponent, ConflictStakes.Existential);
        ConflictResolutionConstraints constraints = ForceDeathFor(
            resident,
            NpcInjurySeverity.SeriouslyInjured);
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(opponent.RuntimeId)
        {
            ForceAlive = true
        });

        Assert.That(applier.TryResolveAndApply(
            conflict,
            constraints,
            out _,
            out string reason), Is.True, reason);
        Assert.That(world.PersonStore.TryGet(personId, out PersonRuntime person), Is.True);
        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(world.CurrentDay));
        Assert.That(resident.IsDead, Is.True);
        Assert.That(resident.InjurySeverity, Is.EqualTo(NpcInjurySeverity.SeriouslyInjured));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
        Assert.That(world.GetAuthoritativeNpcRoster().Npcs, Does.Contain(resident));
    }

    [Test]
    public void FatalResidentConflictCannotDecrementPopulationTwice()
    {
        CityRuntime city = CreateCity("fatal-once-city", 5);
        NpcRuntime resident = CreateNpc("fatal-once-resident");
        NpcRuntime opponent = CreateNpc("fatal-once-opponent");
        BindExistingResident(city, resident, new[] { resident, opponent });
        SimulationRuntime world = CreateWorld(new[] { city }, resident, opponent);
        NpcConflictConsequenceSystem applier = CreateApplier(world);
        Conflict conflict = CreateConflict(resident, opponent, ConflictStakes.Existential);
        ConflictResolutionConstraints constraints = ForceDeathFor(resident, NpcInjurySeverity.Injured);
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(opponent.RuntimeId)
        {
            ForceAlive = true
        });

        Assert.That(applier.TryResolveAndApply(conflict, constraints, out ConflictResolutionResult result, out string reason), Is.True, reason);
        int populationAfterFirstApply = city.CurrentPopulation;
        long revisionAfterFirstApply = city.Population.Revision;

        bool repeated = applier.TryApply(conflict, result, out string repeatedReason);

        Assert.That(repeated, Is.False, repeatedReason);
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationAfterFirstApply));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionAfterFirstApply));
        Assert.That(resident.IsDead, Is.True);
    }

    [Test]
    public void MissingResidenceSettlementRejectsFatalResidentConflictAtomically()
    {
        CityRuntime residenceCity = CreateCity("missing-residence-city", 5);
        NpcRuntime resident = CreateNpc("missing-residence-resident");
        NpcRuntime opponent = CreateNpc("missing-residence-opponent");
        BindExistingResident(residenceCity, resident, new[] { resident, opponent });
        SimulationRuntime world = CreateWorld(Array.Empty<CityRuntime>(), resident, opponent);
        NpcConflictConsequenceSystem applier = CreateApplier(world);
        Conflict conflict = CreateConflict(resident, opponent, ConflictStakes.Existential);
        ConflictResolutionConstraints constraints = ForceDeathFor(resident, NpcInjurySeverity.SeriouslyInjured);
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(opponent.RuntimeId)
        {
            ForceAlive = true
        });

        bool applied = applier.TryResolveAndApply(conflict, constraints, out _, out string reason);

        Assert.That(applied, Is.False);
        Assert.That(reason, Does.Contain("missing from the authoritative world"));
        Assert.That(resident.IsAlive, Is.True);
        Assert.That(resident.InjurySeverity, Is.EqualTo(NpcInjurySeverity.None));
        Assert.That(resident.ResidenceSettlementRuntimeId, Is.EqualTo(residenceCity.RuntimeId));
        Assert.That(residenceCity.CurrentPopulation, Is.EqualTo(5));
        Assert.That(residenceCity.Population.Revision, Is.EqualTo(0));
    }

    [Test]
    public void NonresidentFatalConflictStillKillsWithoutPopulationDelta()
    {
        CityRuntime unrelatedCity = CreateCity("nonresident-city", 5);
        NpcRuntime nonresident = CreateNpc("nonresident-fatal");
        NpcRuntime opponent = CreateNpc("nonresident-opponent");
        SimulationRuntime world = CreateWorld(new[] { unrelatedCity }, nonresident, opponent);
        NpcConflictConsequenceSystem applier = CreateApplier(world);
        Conflict conflict = CreateConflict(nonresident, opponent, ConflictStakes.Existential);
        ConflictResolutionConstraints constraints = ForceDeathFor(nonresident, NpcInjurySeverity.Injured);
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(opponent.RuntimeId)
        {
            ForceAlive = true
        });

        bool applied = applier.TryResolveAndApply(conflict, constraints, out _, out string reason);

        Assert.That(applied, Is.True, reason);
        Assert.That(nonresident.IsDead, Is.True);
        Assert.That(nonresident.InjurySeverity, Is.EqualTo(NpcInjurySeverity.Injured));
        Assert.That(unrelatedCity.CurrentPopulation, Is.EqualTo(5));
        Assert.That(unrelatedCity.Population.Revision, Is.EqualTo(0));
    }

    [Test]
    public void NonfatalResidentConflictPreservesMembershipAndPopulation()
    {
        CityRuntime city = CreateCity("nonfatal-resident-city", 5);
        NpcRuntime resident = CreateNpc("nonfatal-resident");
        NpcRuntime opponent = CreateNpc("nonfatal-resident-opponent");
        BindExistingResident(city, resident, new[] { resident, opponent });
        SimulationRuntime world = CreateWorld(new[] { city }, resident, opponent);
        NpcConflictConsequenceSystem applier = CreateApplier(world);
        Conflict conflict = CreateConflict(resident, opponent, ConflictStakes.Low);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints();
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(resident.RuntimeId)
        {
            ForceAlive = true,
            ForcedInjurySeverity = NpcInjurySeverity.Hurt
        });
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(opponent.RuntimeId)
        {
            ForceAlive = true
        });

        bool applied = applier.TryResolveAndApply(conflict, constraints, out ConflictResolutionResult result, out string reason);

        Assert.That(applied, Is.True, reason);
        Assert.That(result.NpcConsequences.FindByRuntimeId(resident.RuntimeId).IsDead, Is.False);
        Assert.That(resident.IsAlive, Is.True);
        Assert.That(resident.InjurySeverity, Is.EqualTo(NpcInjurySeverity.Hurt));
        Assert.That(resident.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
        Assert.That(city.Population.Revision, Is.EqualTo(0));
    }

    [Test]
    public void StaleResidentDeathWithConflictInjuryIsAtomic()
    {
        CityRuntime city = CreateCity("stale-conflict-city", 5);
        NpcRuntime resident = CreateNpc("stale-conflict-resident");
        BindExistingResident(city, resident, new[] { resident });
        SimulationRuntime world = CreateWorld(new[] { city }, resident);
        AuthoritativeNpcRoster roster = world.GetAuthoritativeNpcRoster();

        Assert.That(
            NpcPopulationLifecycleSystem.TryProposeResidentDeath(
                resident,
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

        bool applied = NpcPopulationLifecycleSystem.TryApplyResidentDeathWithConflictInjury(
            resident,
            city,
            roster,
            NpcInjurySeverity.SeriouslyInjured,
            transition,
            out NpcPopulationLifecycleFailure failure);

        Assert.That(applied, Is.False);
        Assert.That(failure, Is.EqualTo(NpcPopulationLifecycleFailure.StaleState));
        Assert.That(resident.IsAlive, Is.True);
        Assert.That(resident.InjurySeverity, Is.EqualTo(NpcInjurySeverity.None));
        Assert.That(resident.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(city.CurrentPopulation, Is.EqualTo(6));
        Assert.That(city.Population.Revision, Is.EqualTo(1));
    }

    private static NpcConflictConsequenceSystem CreateApplier(SimulationRuntime world)
    {
        ConflictResolver resolver = new ConflictResolver(
            new FixedCapabilityModel(),
            new SequenceConflictRandomSource(0.5f, 0.5f, 0.5f, 0.5f));
        return new NpcConflictConsequenceSystem(world, new ConflictResolutionService(resolver));
    }

    private static Conflict CreateConflict(NpcRuntime first, NpcRuntime second, ConflictStakes stakes)
    {
        Conflict conflict = new Conflict("conflict-" + first.RuntimeId);
        ConflictSide firstSide = conflict.AddSide("first", ConflictObjectiveType.Defeat, stakes);
        ConflictSide secondSide = conflict.AddSide("second", ConflictObjectiveType.Defeat, stakes);
        firstSide.AddNpc(first);
        secondSide.AddNpc(second);
        return conflict;
    }

    private static ConflictResolutionConstraints ForceDeathFor(NpcRuntime npc, NpcInjurySeverity severity)
    {
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints();
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(npc.RuntimeId)
        {
            ForceDeath = true,
            ForcedInjurySeverity = severity
        });
        return constraints;
    }

    private static SimulationRuntime CreateWorld(
        IReadOnlyList<CityRuntime> cities,
        params NpcRuntime[] npcs)
    {
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

    private static void BindExistingResident(
        CityRuntime city,
        NpcRuntime npc,
        IEnumerable<NpcRuntime> roster)
    {
        bool bound = SettlementPopulationMembershipSystem.TryBindExistingResident(
            city,
            npc,
            SimulationTestFactory.CreateAuthoritativeNpcRoster(roster),
            out PopulationMembershipFailure failure);
        Assert.That(bound, Is.True, failure.ToString());
    }

    private sealed class FixedCapabilityModel : ICapabilityModel
    {
        public CapabilityEvaluationResult Evaluate(
            NpcRuntime participant,
            CapabilityEvaluationContext context = null)
        {
            return new CapabilityEvaluationResult(100f, 100f, null);
        }
    }
}
