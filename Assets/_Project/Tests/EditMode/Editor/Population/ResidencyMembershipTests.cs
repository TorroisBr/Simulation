using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class ResidencyMembershipTests
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
    public void ExistingNamedResidentCanBindWithoutPopulationChange()
    {
        CityRuntime city = CreateCity("residency-bind", 3);
        NpcRuntime npc = CreateNpc("residency-bind-npc");

        BindExistingResident(city, npc, new[] { npc });

        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(city.CurrentPopulation, Is.EqualTo(3));
    }

    [Test]
    public void BindingExistingResidentDoesNotIncrementPopulationRevision()
    {
        CityRuntime city = CreateCity("residency-bind-revision", 3);
        NpcRuntime npc = CreateNpc("residency-bind-revision-npc");
        long revisionBefore = city.Population.Revision;

        BindExistingResident(city, npc, new[] { npc });

        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
    }

    [Test]
    public void ResidentIdentityUsesRuntimeId()
    {
        CityRuntime city = CreateCity("residency-runtime-id", 3);
        NpcRuntime npc = CreateNpc("residency-runtime-id-npc");

        BindExistingResident(city, npc, new[] { npc });

        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.Not.EqualTo(city.CityName));
    }

    [Test]
    public void NamedResidentCountIncludesPresentResident()
    {
        CityRuntime city = CreateCity("residency-present-resident", 10);
        NpcRuntime npc = CreateNpc("present-resident");
        BindExistingResident(city, npc, new[] { npc });
        city.AddImportantNpc(npc);

        SettlementPopulationPresenceSummary summary = Query(city, new[] { npc });

        Assert.That(summary.NamedResidentCount, Is.EqualTo(1));
        Assert.That(summary.NamedPresentCount, Is.EqualTo(1));
    }

    [Test]
    public void NamedResidentCountIncludesTravelingResident()
    {
        CityRuntime origin = CreateCity("residency-travel-origin", 10);
        CityRuntime destination = CreateCity("residency-travel-destination", 10);
        NpcRuntime npc = CreateNpc("traveling-resident");
        BindExistingResident(origin, npc, new[] { npc });
        origin.AddImportantNpc(npc);
        Assert.That(npc.StartTravel(destination, 2), Is.True);

        SettlementPopulationPresenceSummary summary = Query(origin, new[] { npc });

        Assert.That(summary.NamedResidentCount, Is.EqualTo(1));
    }

    [Test]
    public void NamedPresentCountExcludesTravelingResident()
    {
        CityRuntime origin = CreateCity("residency-travel-present-origin", 10);
        CityRuntime destination = CreateCity("residency-travel-present-destination", 10);
        NpcRuntime npc = CreateNpc("traveling-present-resident");
        BindExistingResident(origin, npc, new[] { npc });
        origin.AddImportantNpc(npc);
        Assert.That(npc.StartTravel(destination, 2), Is.True);

        SettlementPopulationPresenceSummary summary = Query(origin, new[] { npc });

        Assert.That(summary.NamedPresentCount, Is.EqualTo(0));
    }

    [Test]
    public void NamedPresentCountIncludesVisitor()
    {
        CityRuntime city = CreateCity("residency-visitor-present", 10);
        NpcRuntime visitor = CreateNpc("visitor-present");
        city.AddImportantNpc(visitor);

        SettlementPopulationPresenceSummary summary = Query(city, new[] { visitor });

        Assert.That(summary.NamedResidentCount, Is.EqualTo(0));
        Assert.That(summary.NamedPresentCount, Is.EqualTo(1));
    }

    [Test]
    public void VisitorDoesNotBecomeResident()
    {
        CityRuntime city = CreateCity("residency-visitor-no-bind", 10);
        NpcRuntime visitor = CreateNpc("visitor-no-bind");
        city.AddImportantNpc(visitor);

        Assert.That(visitor.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(Query(city, new[] { visitor }).NamedResidentCount, Is.EqualTo(0));
    }

    [Test]
    public void NamedResidentCountCanDifferFromNamedPresentCount()
    {
        CityRuntime city = CreateCity("residency-count-difference", 10);
        NpcRuntime resident = CreateNpc("count-difference-resident");
        BindExistingResident(city, resident, new[] { resident });
        city.AddImportantNpc(resident);
        Assert.That(resident.StartTravel(CreateCity("count-difference-destination", 10), 2), Is.True);

        SettlementPopulationPresenceSummary summary = Query(city, new[] { resident });

        Assert.That(summary.NamedResidentCount, Is.EqualTo(1));
        Assert.That(summary.NamedPresentCount, Is.EqualTo(0));
    }

    [Test]
    public void NamedPresentCountCanExceedNamedResidentCount()
    {
        CityRuntime city = CreateCity("residency-present-exceeds", 10);
        NpcRuntime resident = CreateNpc("present-exceeds-resident");
        NpcRuntime firstVisitor = CreateNpc("present-exceeds-visitor-a");
        NpcRuntime secondVisitor = CreateNpc("present-exceeds-visitor-b");
        BindExistingResident(city, resident, new[] { resident, firstVisitor, secondVisitor });
        city.AddImportantNpc(resident);
        city.AddImportantNpc(firstVisitor);
        city.AddImportantNpc(secondVisitor);

        SettlementPopulationPresenceSummary summary = Query(
            city,
            new[] { resident, firstVisitor, secondVisitor });

        Assert.That(summary.NamedResidentCount, Is.EqualTo(1));
        Assert.That(summary.NamedPresentCount, Is.EqualTo(3));
    }

    [Test]
    public void DeadNpcExcludedFromNamedResidentCount()
    {
        CityRuntime city = CreateCity("residency-dead-resident", 10);
        NpcRuntime npc = CreateNpc("dead-resident");
        BindExistingResident(city, npc, new[] { npc });
        Assert.That(
            NpcPopulationLifecycleSystem.TryApplyResidentDeath(
                npc,
                city,
                SimulationTestFactory.CreateAuthoritativeNpcRoster(new[] { npc }),
                out _,
                out NpcPopulationLifecycleFailure failure),
            Is.True,
            failure.ToString());

        Assert.That(Query(city, new[] { npc }).NamedResidentCount, Is.EqualTo(0));
    }

    [Test]
    public void DeadNpcExcludedFromNamedPresentCount()
    {
        CityRuntime city = CreateCity("residency-dead-present", 10);
        NpcRuntime npc = CreateNpc("dead-present");
        city.AddImportantNpc(npc);
        Assert.That(npc.TryApplyDeath(), Is.True);

        Assert.That(Query(city, new[] { npc }).NamedPresentCount, Is.EqualTo(0));
    }

    [Test]
    public void QueryDoesNotMutateWorld()
    {
        CityRuntime city = CreateCity("residency-query-pure", 10);
        NpcRuntime npc = CreateNpc("query-pure");
        BindExistingResident(city, npc, new[] { npc });
        city.AddImportantNpc(npc);
        int populationBefore = city.CurrentPopulation;
        long revisionBefore = city.Population.Revision;
        string residenceBefore = npc.ResidenceSettlementRuntimeId;
        CityRuntime currentCityBefore = npc.CurrentCity;

        SettlementPopulationPresenceSummary first = Query(city, new[] { npc });
        SettlementPopulationPresenceSummary second = Query(city, new[] { npc });

        Assert.That(first.NamedResidentCount, Is.EqualTo(second.NamedResidentCount));
        Assert.That(first.NamedPresentCount, Is.EqualTo(second.NamedPresentCount));
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(residenceBefore));
        Assert.That(npc.CurrentCity, Is.SameAs(currentCityBefore));
    }

    [Test]
    public void QueryDoesNotConsumeRng()
    {
        CityRuntime city = CreateCity("residency-query-rng", 10);
        NpcRuntime npc = CreateNpc("query-rng");
        Random random = new Random(1234);
        int first = random.Next();

        Query(city, new[] { npc });

        Random expectedRandom = new Random(1234);
        Assert.That(first, Is.EqualTo(expectedRandom.Next()));
        Assert.That(random.Next(), Is.EqualTo(expectedRandom.Next()));
    }

    [Test]
    public void QueryDoesNotAdvanceTime()
    {
        CityRuntime city = CreateCity("residency-query-time", 10);
        NpcRuntime npc = CreateNpc("query-time");
        SimulationTime time = new SimulationTime(17L);

        Query(city, new[] { npc });

        Assert.That(time.AbsoluteDay, Is.EqualTo(17L));
    }

    [Test]
    public void CannotBindExistingResidentBeyondAggregatePopulation()
    {
        CityRuntime city = CreateCity("residency-capacity", 1);
        NpcRuntime first = CreateNpc("capacity-first");
        NpcRuntime second = CreateNpc("capacity-second");
        BindExistingResident(city, first, new[] { first });

        bool bound = SettlementPopulationMembershipSystem.TryBindExistingResident(
            city,
            second,
            SimulationTestFactory.CreateAuthoritativeNpcRoster(new[] { first, second }),
            out PopulationMembershipFailure failure);

        Assert.That(bound, Is.False);
        Assert.That(failure, Is.EqualTo(PopulationMembershipFailure.AggregateCapacityExceeded));
        Assert.That(second.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(1));
    }

    [Test]
    public void NullAuthoritativeRosterFailsClosedWithoutPopulationChange()
    {
        CityRuntime city = CreateCity("residency-null-roster", 1);
        NpcRuntime npc = CreateNpc("residency-null-roster-npc");
        long revisionBefore = city.Population.Revision;

        bool bound = SettlementPopulationMembershipSystem.TryBindExistingResident(
            city,
            npc,
            (AuthoritativeNpcRoster)null,
            out PopulationMembershipFailure failure);

        Assert.That(bound, Is.False);
        Assert.That(failure, Is.EqualTo(PopulationMembershipFailure.AuthoritativeRosterRequired));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(1));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
    }

    [Test]
    public void IncompleteRosterCannotSilentlyOverbookAggregatePopulation()
    {
        CityRuntime city = CreateCity("residency-incomplete-roster", 1);
        NpcRuntime first = CreateNpc("incomplete-roster-first");
        NpcRuntime second = CreateNpc("incomplete-roster-second");
        BindExistingResident(city, first, new[] { first, second });

        bool bound = SettlementPopulationMembershipSystem.TryBindExistingResident(
            city,
            second,
            new[] { second },
            out PopulationMembershipFailure failure);

        Assert.That(bound, Is.False);
        Assert.That(failure, Is.EqualTo(PopulationMembershipFailure.AuthoritativeRosterRequired));
        Assert.That(second.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(1));
    }

    [Test]
    public void RebindingSameResidenceIsIdempotentOrExplicitlySafe()
    {
        CityRuntime city = CreateCity("residency-rebind", 2);
        NpcRuntime npc = CreateNpc("rebind");
        BindExistingResident(city, npc, new[] { npc });
        long revisionBefore = city.Population.Revision;

        bool rebound = SettlementPopulationMembershipSystem.TryBindExistingResident(
            city,
            npc,
            SimulationTestFactory.CreateAuthoritativeNpcRoster(new[] { npc }),
            out PopulationMembershipFailure failure);

        Assert.That(rebound, Is.True);
        Assert.That(failure, Is.EqualTo(PopulationMembershipFailure.None));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
    }

    [Test]
    public void CannotSilentlyReplaceResidenceWithoutMigration()
    {
        CityRuntime origin = CreateCity("residency-origin", 2);
        CityRuntime other = CreateCity("residency-other", 2);
        NpcRuntime npc = CreateNpc("residence-replacement");
        BindExistingResident(origin, npc, new[] { npc });

        bool rebound = SettlementPopulationMembershipSystem.TryBindExistingResident(
            other,
            npc,
            SimulationTestFactory.CreateAuthoritativeNpcRoster(new[] { npc }),
            out PopulationMembershipFailure failure);

        Assert.That(rebound, Is.False);
        Assert.That(failure, Is.EqualTo(PopulationMembershipFailure.ResidenceAlreadyAssigned));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(origin.RuntimeId));
    }

    [Test]
    public void ResidenceAppearsInWorldStateSnapshot()
    {
        CityRuntime city = CreateCity("residency-snapshot", 10);
        NpcRuntime npc = CreateNpc("snapshot-resident");
        BindExistingResident(city, npc, new[] { npc });

        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(
            new WorldStateSnapshotContext(npcs: new[] { npc }));
        string canonical = WorldStateCanonicalWriter.Write(snapshot);

        Assert.That(snapshot.Npcs[0].ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(canonical, Does.Contain(city.RuntimeId));
    }

    [Test]
    public void ResidenceChangeChangesWorldStateDigest()
    {
        CityRuntime city = CreateCity("residency-digest", 10);
        NpcRuntime npc = CreateNpc("digest-resident");
        WorldStateSnapshotContext context = new WorldStateSnapshotContext(npcs: new[] { npc });
        WorldStateSnapshot beforeSnapshot = WorldStateSnapshotBuilder.BuildSnapshot(context);
        string before = WorldStateSnapshotDigest.Compute(beforeSnapshot);

        BindExistingResident(city, npc, new[] { npc });

        WorldStateSnapshot afterSnapshot = WorldStateSnapshotBuilder.BuildSnapshot(context);
        string after = WorldStateSnapshotDigest.Compute(afterSnapshot);
        WorldStateDiff diff = WorldStateDiff.Compare(beforeSnapshot, afterSnapshot);

        Assert.That(after, Is.Not.EqualTo(before));
        Assert.That(diff.Differences, Has.Some.Property("Field").EqualTo("ResidenceSettlementRuntimeId"));
    }

    [Test]
    public void DerivedCountsDoNotNeedPersistentMutableCounters()
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        Assert.That(typeof(CityRuntime).GetField("namedResidentCount", flags), Is.Null);
        Assert.That(typeof(CityRuntime).GetField("namedPresentCount", flags), Is.Null);
        Assert.That(typeof(SettlementPopulationPresenceSummary).GetProperty("NamedResidentCount"), Is.Not.Null);
        Assert.That(typeof(SettlementPopulationPresenceSummary).GetProperty("NamedPresentCount"), Is.Not.Null);
    }

    [Test]
    public void AuthoritativeRosterHasNoPublicArbitraryConstructionApi()
    {
        BindingFlags publicInstance = BindingFlags.Instance | BindingFlags.Public;
        Assert.That(typeof(AuthoritativeNpcRoster).GetConstructors(publicInstance), Is.Empty);

        BindingFlags publicStatic = BindingFlags.Static | BindingFlags.Public;
        foreach (MethodInfo method in typeof(AuthoritativeNpcRoster).GetMethods(publicStatic))
        {
            if (method.ReturnType != typeof(AuthoritativeNpcRoster))
            {
                continue;
            }

            foreach (ParameterInfo parameter in method.GetParameters())
            {
                Assert.That(
                    typeof(IEnumerable<NpcRuntime>).IsAssignableFrom(parameter.ParameterType),
                    Is.False,
                    method.Name + " must not accept an arbitrary NPC enumerable.");
            }
        }
    }

    private static SettlementPopulationPresenceSummary Query(
        CityRuntime city,
        IEnumerable<NpcRuntime> npcs)
    {
        return SettlementPopulationPresenceQuery.BuildSummary(city, npcs);
    }

    private static void BindExistingResident(
        CityRuntime city,
        NpcRuntime npc,
        IEnumerable<NpcRuntime> knownNpcs)
    {
        bool bound = SettlementPopulationMembershipSystem.TryBindExistingResident(
            city,
            npc,
            SimulationTestFactory.CreateAuthoritativeNpcRoster(knownNpcs),
            out PopulationMembershipFailure failure);
        Assert.That(bound, Is.True, failure.ToString());
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
        return new NpcRuntime(
            runtimeId,
            SimulationTestFactory.CreateNpc(runtimeId));
    }
}
