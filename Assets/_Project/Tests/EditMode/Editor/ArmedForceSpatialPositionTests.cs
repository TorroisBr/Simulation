using System;
using System.Linq;
using NUnit.Framework;

public sealed class ArmedForceSpatialPositionTests
{
    [Test]
    public void PositionIdentityIsStableOptionalAndStoreOwned()
    {
        WorldParts parts = CreateWorld(false);
        ArmedForceSpatialStateStore spatial = parts.Spatial;

        Assert.That(spatial.TryGetPosition(parts.ForceId, out _), Is.False);
        Assert.That(spatial.TrySetPosition(
            parts.ForceId,
            SpatialReference.ForHex(new HexId("hex-a")),
            out ArmedForceSpatialFailure failure), Is.True, failure.ToString());
        Assert.That(spatial.TryGetPosition(new ArmedForceId("force-main"), out SpatialReference position), Is.True);
        Assert.That(position.StableKey, Is.EqualTo("hex:hex-a"));
        Assert.That(spatial.Positions[0].ForceId, Is.EqualTo(parts.ForceId));
        Assert.That(spatial.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void PositionUpdatesRejectInvalidOrInactiveForcesAtomically()
    {
        WorldParts parts = CreateWorld(false);
        long revision = parts.Spatial.Revision;
        Assert.That(parts.Spatial.TrySetPosition(
            parts.ForceId,
            SpatialReference.ForHex(new HexId("missing")),
            out ArmedForceSpatialFailure invalid), Is.False);
        Assert.That(invalid.Code, Is.EqualTo(ArmedForceSpatialFailureCode.SpatialReferenceNotRegistered));
        Assert.That(parts.Spatial.Revision, Is.EqualTo(revision));
        Assert.That(parts.Spatial.TryGetPosition(parts.ForceId, out _), Is.False);

        Assert.That(parts.Forces.TryTerminate(parts.ForceId, 1L, out _), Is.True);
        Assert.That(parts.Spatial.TrySetPosition(
            parts.ForceId,
            SpatialReference.ForHex(new HexId("hex-a")),
            out ArmedForceSpatialFailure terminated), Is.False);
        Assert.That(terminated.Code, Is.EqualTo(ArmedForceSpatialFailureCode.ForceTerminated));
    }

    [Test]
    public void SamePositionAndMissingClearAreNoOpRevisions()
    {
        WorldParts parts = CreateWorld(false);
        SpatialReference position = SpatialReference.ForHex(new HexId("hex-a"));
        Assert.That(parts.Spatial.TrySetPosition(parts.ForceId, position, out _), Is.True);
        long revision = parts.Spatial.Revision;
        Assert.That(parts.Spatial.TrySetPosition(parts.ForceId, SpatialReference.ForHex(new HexId("hex-a")), out _), Is.True);
        Assert.That(parts.Spatial.Revision, Is.EqualTo(revision));
        Assert.That(parts.Spatial.TryClearPosition(parts.ForceId, out _), Is.True);
        Assert.That(parts.Spatial.Revision, Is.EqualTo(revision + 1L));
        Assert.That(parts.Spatial.TryClearPosition(parts.ForceId, out _), Is.True);
        Assert.That(parts.Spatial.Revision, Is.EqualTo(revision + 1L));
    }

    [Test]
    public void ParentAndChildPositionsAreIndependentOfHierarchyAndDetachment()
    {
        WorldParts parts = CreateWorld(true);
        Assert.That(parts.Spatial.TrySetPosition(parts.ForceId, SpatialReference.ForHex(new HexId("hex-a")), out _), Is.True);
        Assert.That(parts.Spatial.TrySetPosition(parts.ChildId, SpatialReference.ForLocation(new LocationId("location-b")), out _), Is.True);

        Assert.That(parts.Forces.TryDetach(parts.ChildId, out _), Is.True);
        Assert.That(parts.Spatial.TryGetPosition(parts.ChildId, out SpatialReference detachedPosition), Is.True);
        Assert.That(detachedPosition.StableKey, Is.EqualTo("location:location-b"));
        Assert.That(parts.Forces.TryReattach(parts.ChildId, out _), Is.True);
        Assert.That(parts.Spatial.TryGetPosition(parts.ChildId, out SpatialReference reattachedPosition), Is.True);
        Assert.That(reattachedPosition.StableKey, Is.EqualTo("location:location-b"));

        Assert.That(parts.Spatial.TryClearPosition(parts.ForceId, out _), Is.True);
        Assert.That(parts.Spatial.TryGetPosition(parts.ChildId, out _), Is.True);
        Assert.That(parts.Spatial.TryCheckCompatibility(
            parts.ForceId,
            SpatialReference.ForHex(new HexId("hex-a")),
            out bool compatible,
            out ArmedForceSpatialFailure failure), Is.True, failure.ToString());
        Assert.That(compatible, Is.False);
    }

    [Test]
    public void CompatibilityIsDirectionalAndUsesTypedAreaGranularity()
    {
        WorldParts parts = CreateWorld(true);
        SpatialReference hexA = SpatialReference.ForHex(new HexId("hex-a"));
        SpatialReference locationA = SpatialReference.ForLocation(new LocationId("location-a"));
        SpatialReference locationB = SpatialReference.ForLocation(new LocationId("location-b"));

        Assert.That(parts.Spatial.TrySetPosition(parts.ForceId, hexA, out _), Is.True);
        AssertCompatibility(parts.Spatial, parts.ForceId, hexA, true);
        AssertCompatibility(parts.Spatial, parts.ForceId, locationA, false);

        Assert.That(parts.Spatial.TrySetPosition(parts.ForceId, locationA, out _), Is.True);
        AssertCompatibility(parts.Spatial, parts.ForceId, hexA, true);
        AssertCompatibility(parts.Spatial, parts.ForceId, locationA, true);
        AssertCompatibility(parts.Spatial, parts.ForceId, locationB, false);

        Assert.That(parts.Spatial.TrySetPosition(parts.ForceId, parts.SubLocation, out _), Is.True);
        AssertCompatibility(parts.Spatial, parts.ForceId, locationA, true);
        AssertCompatibility(parts.Spatial, parts.ForceId, parts.SubLocation, true);
        AssertCompatibility(parts.Spatial, parts.ForceId, SpatialReference.ForSubLocation(
            LocalTopologyOwnerKind.City, parts.City.RuntimeId, "place-b"), false);
    }

    [Test]
    public void CompatibilityReportsInvalidReferencesInsteadOfFalse()
    {
        WorldParts parts = CreateWorld(false);
        Assert.That(parts.Spatial.TryCheckCompatibility(
            parts.ForceId,
            SpatialReference.ForHex(new HexId("missing")),
            out bool compatible,
            out ArmedForceSpatialFailure failure), Is.False);
        Assert.That(compatible, Is.False);
        Assert.That(failure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.SpatialReferenceNotRegistered));
    }

    [Test]
    public void LegacyOperationalLocationNeverPopulatesTypedPosition()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId forceId = new ArmedForceId("force-legacy");
        Assert.That(forces.TryRegister(new ArmedForceRecord(
            forceId, "Legacy", 0L, operationalLocationReference: "opaque-origin"), out _), Is.True);
        SpatialAuthorityStore authority = CreateAuthority(false);
        ArmedForceSpatialStateStore spatial = new ArmedForceSpatialStateStore(forces, authority);

        Assert.That(spatial.TryGetPosition(forceId, out _), Is.False);
        WorldStateSnapshot snapshot = WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(0L),
            armedForceStore: forces,
            spatialAuthorityStore: authority,
            armedForceSpatialStateStore: spatial));
        string canonical = WorldStateCanonicalWriter.Write(snapshot);
        Assert.That(canonical, Does.Not.Contain("ARMED_FORCE_POSITION|force-legacy|"));
        Assert.That(canonical, Does.Contain("ARMED_FORCE_LEGACY_OPERATIONAL_REFERENCE|force-legacy|opaque-origin"));
    }

    [Test]
    public void SnapshotDiffAndInvariantDiagnosticsExposeAuthoritativePosition()
    {
        WorldParts parts = CreateWorld(false);
        Assert.That(parts.Spatial.TrySetPosition(parts.ForceId, SpatialReference.ForHex(new HexId("hex-a")), out _), Is.True);
        WorldStateSnapshot before = WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(0L),
            armedForceStore: parts.Forces,
            spatialAuthorityStore: parts.Authority,
            armedForceSpatialStateStore: parts.Spatial));

        Assert.That(parts.Spatial.TrySetPosition(parts.ForceId, SpatialReference.ForHex(new HexId("hex-b")), out _), Is.True);
        WorldStateSnapshot after = WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(0L),
            armedForceStore: parts.Forces,
            spatialAuthorityStore: parts.Authority,
            armedForceSpatialStateStore: parts.Spatial));

        Assert.That(before.ArmedForcePositions.Single().CurrentPositionStableKey, Is.EqualTo("hex:hex-a"));
        Assert.That(WorldStateCanonicalWriter.Write(after), Does.Contain("ARMED_FORCE_POSITION|force-main|hex:hex-b"));
        Assert.That(WorldStateDiagnostics.Compare(before, after).Differences, Has.Some.Matches<WorldStateDifference>(difference =>
            difference.Section == "ArmedForcePosition"
            && difference.Identity == "force-main"
            && difference.Field == "SpatialReference"));
        Assert.That(WorldStateDiagnostics.Validate(after).IsValid, Is.True);
    }

    [Test]
    public void PositionSnapshotIsIndependentOfInsertionOrder()
    {
        WorldParts first = CreateWorld(true, false);
        WorldParts second = CreateWorld(true, true);
        Assert.That(first.Spatial.TrySetPosition(first.ChildId, SpatialReference.ForLocation(new LocationId("location-b")), out _), Is.True);
        Assert.That(first.Spatial.TrySetPosition(first.ForceId, SpatialReference.ForHex(new HexId("hex-a")), out _), Is.True);
        Assert.That(second.Spatial.TrySetPosition(second.ForceId, SpatialReference.ForHex(new HexId("hex-a")), out _), Is.True);
        Assert.That(second.Spatial.TrySetPosition(second.ChildId, SpatialReference.ForLocation(new LocationId("location-b")), out _), Is.True);

        WorldStateSnapshot firstSnapshot = Snapshot(first);
        WorldStateSnapshot secondSnapshot = Snapshot(second);
        Assert.That(WorldStateCanonicalWriter.Write(firstSnapshot), Is.EqualTo(WorldStateCanonicalWriter.Write(secondSnapshot)));
        Assert.That(WorldStateDiagnostics.Compare(firstSnapshot, secondSnapshot).IsEmpty, Is.True);
    }

    [Test]
    public void SimulationRuntimeClonesSpatialPositionAgainstClonedForceAndAuthority()
    {
        WorldParts parts = CreateWorld(false);
        Assert.That(parts.Spatial.TrySetPosition(
            parts.ForceId,
            SpatialReference.ForHex(new HexId("hex-a")),
            out _), Is.True);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(0L),
            Array.Empty<CityRuntime>(),
            Array.Empty<NpcRuntime>(),
            armedForceStore: parts.Forces,
            spatialAuthorityStore: parts.Authority,
            armedForceSpatialStateStore: parts.Spatial);

        Assert.That(runtime.ArmedForceSpatialStateStore, Is.Not.SameAs(parts.Spatial));
        Assert.That(runtime.ArmedForceSpatialStateStore.ArmedForceStore, Is.SameAs(runtime.ArmedForceStore));
        Assert.That(runtime.ArmedForceSpatialStateStore.SpatialAuthorityStore, Is.SameAs(runtime.SpatialAuthorityStore));
        Assert.That(runtime.ArmedForceSpatialStateStore.TryGetPosition(parts.ForceId, out SpatialReference position), Is.True);
        Assert.That(position.StableKey, Is.EqualTo("hex:hex-a"));
        Assert.That(parts.Spatial.TrySetPosition(parts.ForceId, SpatialReference.ForHex(new HexId("hex-b")), out _), Is.True);
        Assert.That(runtime.ArmedForceSpatialStateStore.TryGetPosition(parts.ForceId, out position), Is.True);
        Assert.That(position.StableKey, Is.EqualTo("hex:hex-a"));
    }

    [Test]
    public void TerminationRetainsHistoricalPositionButBlocksActiveQueriesAndDoesNotReuseIdentity()
    {
        WorldParts parts = CreateWorld(false);
        Assert.That(parts.Spatial.TrySetPosition(parts.ForceId, SpatialReference.ForHex(new HexId("hex-a")), out _), Is.True);
        Assert.That(parts.Forces.TryTerminate(parts.ForceId, 2L, out _), Is.True);
        Assert.That(parts.Spatial.TryGetPosition(parts.ForceId, out SpatialReference retained), Is.True);
        Assert.That(retained.StableKey, Is.EqualTo("hex:hex-a"));
        Assert.That(parts.Spatial.TryCheckCompatibility(
            parts.ForceId,
            SpatialReference.ForHex(new HexId("hex-a")),
            out _,
            out ArmedForceSpatialFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.ForceTerminated));
        Assert.That(parts.Spatial.ValidateInvariants().IsValid, Is.True);
    }

    private static void AssertCompatibility(
        ArmedForceSpatialStateStore spatial,
        ArmedForceId forceId,
        SpatialReference required,
        bool expected)
    {
        Assert.That(spatial.TryCheckCompatibility(
            forceId,
            required,
            out bool compatible,
            out ArmedForceSpatialFailure failure), Is.True, failure.ToString());
        Assert.That(compatible, Is.EqualTo(expected));
    }

    private static WorldStateSnapshot Snapshot(WorldParts parts)
    {
        return WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(0L),
            armedForceStore: parts.Forces,
            spatialAuthorityStore: parts.Authority,
            localTopologyStore: parts.Topology,
            armedForceSpatialStateStore: parts.Spatial));
    }

    private static WorldParts CreateWorld(bool withChild, bool reverseInsertion = false)
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId forceId = new ArmedForceId("force-main");
        ArmedForceId childId = new ArmedForceId("force-child");
        ArmedForceRecord root = new ArmedForceRecord(forceId, "Main", 0L);
        ArmedForceRecord child = new ArmedForceRecord(childId, "Child", 0L, forceId);
        if (reverseInsertion && withChild)
        {
            Assert.That(forces.TryRegister(child, out _), Is.False);
        }

        Assert.That(forces.TryRegister(root, out _), Is.True);
        if (withChild)
        {
            Assert.That(forces.TryRegister(child, out _), Is.True);
        }

        SpatialAuthorityStore authority = CreateAuthority(withChild);
        LocalTopologyStore topology = CreateTopology(authority, out CityRuntime city, out LocalPlaceRuntime place);
        ArmedForceSpatialStateStore spatial = new ArmedForceSpatialStateStore(forces, authority, topology);
        return new WorldParts(forces, authority, topology, spatial, forceId, childId, city, SpatialReference.ForSubLocation(
            LocalTopologyOwnerKind.City, city.RuntimeId, place.RuntimeId));
    }

    private static SpatialAuthorityStore CreateAuthority(bool includeSecondLocation)
    {
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-b")), out _), Is.True);
        Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-a")), out _), Is.True);
        Assert.That(authority.TryRegisterLocation(new LocationRecord(new LocationId("location-a"), new HexId("hex-a")), out _), Is.True);
        if (includeSecondLocation)
        {
            Assert.That(authority.TryRegisterLocation(new LocationRecord(new LocationId("location-b"), new HexId("hex-b")), out _), Is.True);
        }
        return authority;
    }

    private static LocalTopologyStore CreateTopology(
        SpatialAuthorityStore authority,
        out CityRuntime city,
        out LocalPlaceRuntime place)
    {
        city = SimulationTestFactory.CreateCity("city-spatial", "legacy-spatial");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterCity(city), Is.True);
        LocalTopologyRuntime runtime = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForCity(city), registry);
        place = new LocalPlaceRuntime("place-a");
        Assert.That(runtime.AddPlace(place, null, true), Is.True);
        Assert.That(runtime.AddPlace(new LocalPlaceRuntime("place-b")), Is.True);
        LocalTopologyStore topology = new LocalTopologyStore(registry);
        Assert.That(topology.Add(runtime), Is.True);
        Assert.That(authority.TryBindLocalTopology(runtime, new LocationId("location-a"), out _), Is.True);
        return topology;
    }

    private sealed class WorldParts
    {
        public ArmedForceStore Forces { get; }
        public SpatialAuthorityStore Authority { get; }
        public LocalTopologyStore Topology { get; }
        public ArmedForceSpatialStateStore Spatial { get; }
        public ArmedForceId ForceId { get; }
        public ArmedForceId ChildId { get; }
        public CityRuntime City { get; }
        public SpatialReference SubLocation { get; }

        public WorldParts(
            ArmedForceStore forces,
            SpatialAuthorityStore authority,
            LocalTopologyStore topology,
            ArmedForceSpatialStateStore spatial,
            ArmedForceId forceId,
            ArmedForceId childId,
            CityRuntime city,
            SpatialReference subLocation)
        {
            Forces = forces;
            Authority = authority;
            Topology = topology;
            Spatial = spatial;
            ForceId = forceId;
            ChildId = childId;
            City = city;
            SubLocation = subLocation;
        }
    }
}
