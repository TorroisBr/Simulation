using System;
using NUnit.Framework;

public sealed class PopulationLifecycleDiagnosticsIntegrationTests
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
    public void FatalResidentConflictAppearsInDiagnosticsSnapshotAndDiff()
    {
        CityRuntime city = CreateCity("diagnostics-fatal-city", 5);
        NpcRuntime resident = CreateNpc("diagnostics-fatal-resident");
        NpcRuntime opponent = CreateNpc("diagnostics-fatal-opponent");
        SimulationRuntime world = CreateWorld(city, resident, opponent);

        Assert.That(
            SettlementPopulationMembershipSystem.TryBindExistingResident(
                city,
                resident,
                world.GetAuthoritativeNpcRoster(),
                out PopulationMembershipFailure membershipFailure),
            Is.True,
            membershipFailure.ToString());

        WorldStateSnapshot before = Capture(world);
        WorldStateNpcSnapshot beforeNpc = FindNpc(before, resident.RuntimeId);
        WorldStateCitySnapshot beforeCity = FindCity(before, city.RuntimeId);
        Assert.That(beforeNpc.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(beforeNpc.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(beforeCity.CurrentPopulation, Is.EqualTo(5));
        Assert.That(beforeCity.PopulationRevision, Is.EqualTo(0));
        Assert.That(beforeCity.NamedResidentCount, Is.EqualTo(1));

        Conflict conflict = CreateConflict(resident, opponent);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints();
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(resident.RuntimeId)
        {
            ForceDeath = true,
            ForcedInjurySeverity = NpcInjurySeverity.SeriouslyInjured
        });
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(opponent.RuntimeId)
        {
            ForceAlive = true
        });

        ConflictResolutionService service = new ConflictResolutionService(
            new ConflictResolver(
                new FixedCapabilityModel(),
                new SequenceConflictRandomSource(0.5f, 0.5f, 0.5f, 0.5f)));
        NpcConflictConsequenceSystem applier = new NpcConflictConsequenceSystem(world, service);

        Assert.That(
            applier.TryResolveAndApply(conflict, constraints, out _, out string reason),
            Is.True,
            reason);

        WorldStateSnapshot after = Capture(world);
        WorldStateNpcSnapshot afterNpc = FindNpc(after, resident.RuntimeId);
        WorldStateCitySnapshot afterCity = FindCity(after, city.RuntimeId);
        Assert.That(after.KnownNpcCount, Is.EqualTo(2));
        Assert.That(afterNpc.LifeState, Is.EqualTo(NpcLifeState.Dead));
        Assert.That(afterNpc.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(afterNpc.InjurySeverity, Is.EqualTo(NpcInjurySeverity.SeriouslyInjured));
        Assert.That(afterCity.CurrentPopulation, Is.EqualTo(4));
        Assert.That(afterCity.PopulationRevision, Is.EqualTo(1));
        Assert.That(afterCity.NamedResidentCount, Is.EqualTo(0));
        Assert.That(WorldStateDiagnostics.Validate(after).HasErrors, Is.False);

        WorldStateDiff diff = WorldStateDiagnostics.Compare(before, after);
        Assert.That(Contains(diff, "NPC", "LifeState"), Is.True);
        Assert.That(Contains(diff, "NPC", "ResidenceSettlementRuntimeId"), Is.True);
        Assert.That(Contains(diff, "NPC", "InjurySeverity"), Is.True);
        Assert.That(Contains(diff, "City", "CurrentPopulation"), Is.True);
        Assert.That(Contains(diff, "City", "PopulationRevision"), Is.True);
        Assert.That(Contains(diff, "City", "NamedResidentCount"), Is.True);
    }

    [Test]
    public void ImmigrationAndEmigrationRemainVisibleInDiagnostics()
    {
        CityRuntime city = CreateCity("diagnostics-membership-city", 5);
        NpcRuntime npc = CreateNpc("diagnostics-membership-npc");
        SimulationRuntime world = CreateWorld(city, npc);

        WorldStateSnapshot before = Capture(world);
        Assert.That(FindNpc(before, npc.RuntimeId).ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(FindCity(before, city.RuntimeId).CurrentPopulation, Is.EqualTo(5));

        Assert.That(
            world.TryApplyImmigration(
                npc,
                city,
                out NpcPopulationLifecycleTransition immigration,
                out NpcPopulationLifecycleFailure immigrationFailure),
            Is.True,
            immigrationFailure.ToString());
        Assert.That(immigration.Operation, Is.EqualTo(NpcPopulationLifecycleOperation.Immigration));

        WorldStateSnapshot afterImmigration = Capture(world);
        WorldStateNpcSnapshot immigrant = FindNpc(afterImmigration, npc.RuntimeId);
        WorldStateCitySnapshot immigrantCity = FindCity(afterImmigration, city.RuntimeId);
        Assert.That(immigrant.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(immigrantCity.CurrentPopulation, Is.EqualTo(6));
        Assert.That(immigrantCity.PopulationRevision, Is.EqualTo(1));
        Assert.That(immigrantCity.NamedResidentCount, Is.EqualTo(1));
        Assert.That(WorldStateDiagnostics.Validate(afterImmigration).HasErrors, Is.False);
        Assert.That(Contains(WorldStateDiagnostics.Compare(before, afterImmigration), "City", "NamedResidentCount"), Is.True);

        Assert.That(
            world.TryApplyEmigration(
                npc,
                city,
                out NpcPopulationLifecycleTransition emigration,
                out NpcPopulationLifecycleFailure emigrationFailure),
            Is.True,
            emigrationFailure.ToString());
        Assert.That(emigration.Operation, Is.EqualTo(NpcPopulationLifecycleOperation.Emigration));

        WorldStateSnapshot afterEmigration = Capture(world);
        WorldStateNpcSnapshot emigrant = FindNpc(afterEmigration, npc.RuntimeId);
        WorldStateCitySnapshot emigrantCity = FindCity(afterEmigration, city.RuntimeId);
        Assert.That(emigrant.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(emigrant.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(emigrantCity.CurrentPopulation, Is.EqualTo(5));
        Assert.That(emigrantCity.PopulationRevision, Is.EqualTo(2));
        Assert.That(emigrantCity.NamedResidentCount, Is.EqualTo(0));
        Assert.That(WorldStateDiagnostics.Validate(afterEmigration).HasErrors, Is.False);
        Assert.That(Contains(WorldStateDiagnostics.Compare(afterImmigration, afterEmigration), "City", "CurrentPopulation"), Is.True);
    }

    private static WorldStateSnapshot Capture(SimulationRuntime world)
    {
        return WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            world.SimulationTime,
            world.NpcRuntimes,
            world.Cities));
    }

    private static Conflict CreateConflict(NpcRuntime resident, NpcRuntime opponent)
    {
        Conflict conflict = new Conflict("diagnostics-fatal-conflict");
        conflict.AddSide("resident-side", ConflictObjectiveType.Defeat, ConflictStakes.Existential).AddNpc(resident);
        conflict.AddSide("opponent-side", ConflictObjectiveType.Defeat, ConflictStakes.Existential).AddNpc(opponent);
        return conflict;
    }

    private static SimulationRuntime CreateWorld(CityRuntime city, params NpcRuntime[] npcs)
    {
        return new SimulationRuntime(
            new SimulationTime(12L),
            new[] { city },
            npcs,
            economyEnabled: false);
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

    private static WorldStateNpcSnapshot FindNpc(WorldStateSnapshot snapshot, string runtimeId)
    {
        foreach (WorldStateNpcSnapshot npc in snapshot.Npcs)
        {
            if (string.Equals(npc.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                return npc;
            }
        }

        Assert.Fail("NPC snapshot not found: " + runtimeId);
        return null;
    }

    private static WorldStateCitySnapshot FindCity(WorldStateSnapshot snapshot, string runtimeId)
    {
        foreach (WorldStateCitySnapshot city in snapshot.Cities)
        {
            if (string.Equals(city.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                return city;
            }
        }

        Assert.Fail("City snapshot not found: " + runtimeId);
        return null;
    }

    private static bool Contains(WorldStateDiff diff, string section, string field)
    {
        foreach (WorldStateDifference difference in diff.Differences)
        {
            if (difference.Section == section && difference.Field == field)
            {
                return true;
            }
        }

        return false;
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
