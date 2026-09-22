using System;
using System.Linq;
using NUnit.Framework;

public sealed class SpatialAuthorityTests
{
    [Test]
    public void StableIdsAreDistinctAndLookupIsIndependentOfObjectInstance()
    {
        SpatialAuthorityStore store = new SpatialAuthorityStore();
        Assert.That(store.TryRegisterHex(new HexRecord(new HexId("hex-b")), out _), Is.True);
        Assert.That(store.TryRegisterHex(new HexRecord(new HexId("hex-a")), out _), Is.True);
        Assert.That(store.TryRegisterLocation(
            new LocationRecord(new LocationId("location-a"), new HexId("hex-a")), out _), Is.True);

        Assert.That(store.TryGet(new HexId("hex-a"), out HexRecord hex), Is.True);
        Assert.That(hex.Id, Is.EqualTo(new HexId("hex-a")));
        Assert.That(store.TryGet(new LocationId("location-a"), out LocationRecord location), Is.True);
        Assert.That(location.AnchorHexId, Is.EqualTo(new HexId("hex-a")));
        Assert.That(store.Hexes.Select(value => value.Id.Value), Is.EqualTo(new[] { "hex-a", "hex-b" }));
    }

    [Test]
    public void LocationRequiresRegisteredAnchorAndAnchorIsStable()
    {
        SpatialAuthorityStore store = new SpatialAuthorityStore();
        SpatialAuthorityFailure failure;
        Assert.That(store.TryRegisterLocation(
            new LocationRecord(new LocationId("location-a"), new HexId("missing")),
            out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.AnchorHexNotRegistered));

        Assert.That(store.TryRegisterHex(new HexRecord(new HexId("hex-a")), out _), Is.True);
        Assert.That(store.TryRegisterLocation(
            new LocationRecord(new LocationId("location-a"), new HexId("hex-a")), out _), Is.True);
        Assert.That(store.TryRegisterLocation(
            new LocationRecord(new LocationId("location-a"), new HexId("hex-a")), out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.DuplicateLocationId));
    }

    [Test]
    public void SpatialReferencesResolveHexAndLocationWithoutRuntimeObjects()
    {
        SpatialAuthorityStore store = CreateAuthority("hex-a", "location-a");

        Assert.That(store.TryResolve(
            SpatialReference.ForHex(new HexId("hex-a")), null, out SpatialResolution hexResolution, out _), Is.True);
        Assert.That(hexResolution.Hex.Id.Value, Is.EqualTo("hex-a"));
        Assert.That(store.TryResolve(
            SpatialReference.ForLocation(new LocationId("location-a")), null, out SpatialResolution locationResolution, out _), Is.True);
        Assert.That(locationResolution.Location.Id.Value, Is.EqualTo("location-a"));
        Assert.That(locationResolution.Hex.Id.Value, Is.EqualTo("hex-a"));
    }

    [Test]
    public void LocalTopologyBridgeResolvesSubLocationToLocationAndAnchorHex()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-a", "legacy-location-a");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterCity(city), Is.True);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForCity(city),
            registry);
        LocalPlaceRuntime place = new LocalPlaceRuntime("market-square");
        Assert.That(topology.AddPlace(place, null, true), Is.True);
        LocalTopologyStore topologyStore = new LocalTopologyStore(registry);
        Assert.That(topologyStore.Add(topology), Is.True);

        SpatialAuthorityStore authority = CreateAuthority("hex-a", "location-a");
        Assert.That(authority.TryBindLocalTopology(topology, new LocationId("location-a"), out _), Is.True);

        SpatialReference reference = SpatialReference.ForSubLocation(
            LocalTopologyOwnerKind.City,
            city.RuntimeId,
            place.RuntimeId);
        Assert.That(authority.ValidateInvariants(topologyStore).IsValid, Is.True);
        Assert.That(authority.TryResolve(reference, topologyStore, out SpatialResolution resolution, out _), Is.True);
        Assert.That(resolution.LocalTopology, Is.SameAs(topology));
        Assert.That(resolution.SubLocation, Is.SameAs(place));
        Assert.That(resolution.Location.Id.Value, Is.EqualTo("location-a"));
        Assert.That(resolution.Hex.Id.Value, Is.EqualTo("hex-a"));
    }

    [Test]
    public void TopologyBridgeRejectsDuplicateOwnerAndMissingLocation()
    {
        LocalTopologyRuntime topology = new LocalTopologyRuntime(
            new LocalTopologyOwnerReference("owner-a", LocalTopologyOwnerKind.City, "legacy-location-a"));
        Assert.That(topology.AddPlace(new LocalPlaceRuntime("place-a")), Is.True);
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-a")), out _), Is.True);
        SpatialAuthorityFailure failure;
        Assert.That(authority.TryBindLocalTopology(topology, new LocationId("missing"), out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.TopologyLocationNotRegistered));
        Assert.That(authority.TryRegisterLocation(
            new LocationRecord(new LocationId("location-a"), new HexId("hex-a")), out _), Is.True);
        Assert.That(authority.TryBindLocalTopology(topology, new LocationId("location-a"), out _), Is.True);
        Assert.That(authority.TryBindLocalTopology(topology, new LocationId("location-a"), out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.DuplicateTopologyBinding));
    }

    [Test]
    public void InsertionOrderDoesNotChangeAuthoritativeProjectionOrCanonicalSnapshot()
    {
        SpatialAuthorityStore first = CreateAuthority("hex-b", "location-b");
        Assert.That(first.TryRegisterHex(new HexRecord(new HexId("hex-a")), out _), Is.True);
        Assert.That(first.TryRegisterLocation(
            new LocationRecord(new LocationId("location-a"), new HexId("hex-a")), out _), Is.True);

        SpatialAuthorityStore second = new SpatialAuthorityStore();
        Assert.That(second.TryRegisterHex(new HexRecord(new HexId("hex-a")), out _), Is.True);
        Assert.That(second.TryRegisterLocation(
            new LocationRecord(new LocationId("location-a"), new HexId("hex-a")), out _), Is.True);
        Assert.That(second.TryRegisterHex(new HexRecord(new HexId("hex-b")), out _), Is.True);
        Assert.That(second.TryRegisterLocation(
            new LocationRecord(new LocationId("location-b"), new HexId("hex-b")), out _), Is.True);

        WorldStateSnapshot firstSnapshot = WorldStateSnapshotBuilder.BuildSnapshot(
            new WorldStateSnapshotContext(spatialAuthorityStore: first));
        WorldStateSnapshot secondSnapshot = WorldStateSnapshotBuilder.BuildSnapshot(
            new WorldStateSnapshotContext(spatialAuthorityStore: second));
        Assert.That(WorldStateCanonicalWriter.Write(firstSnapshot), Is.EqualTo(WorldStateCanonicalWriter.Write(secondSnapshot)));
        Assert.That(WorldStateDiff.Compare(firstSnapshot, secondSnapshot).IsEmpty, Is.True);
    }

    [Test]
    public void SimulationRuntimeClonesSpatialAuthorityWithoutChangingAdvanceDayContract()
    {
        SpatialAuthorityStore source = CreateAuthority("hex-a", "location-a");
        long revision = source.Revision;
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(0L),
            Array.Empty<CityRuntime>(),
            Array.Empty<NpcRuntime>(),
            spatialAuthorityStore: source);

        Assert.That(runtime.SpatialAuthorityStore, Is.Not.SameAs(source));
        Assert.That(runtime.SpatialAuthorityStore.Revision, Is.EqualTo(revision));
        Assert.That(runtime.SpatialAuthorityStore.TryGet(new LocationId("location-a"), out _), Is.True);
        runtime.AdvanceDay();
        Assert.That(runtime.SpatialAuthorityStore.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void SnapshotAndInvariantDiagnosticsExposeSpatialAuthorityDeterministically()
    {
        SpatialAuthorityStore authority = CreateAuthority("hex-a", "location-a");
        WorldStateSnapshot snapshot = WorldStateDiagnostics.Capture(
            new WorldStateSnapshotContext(spatialAuthorityStore: authority));

        Assert.That(snapshot.Spatial.AuthorityRevision, Is.EqualTo(authority.Revision));
        Assert.That(snapshot.Spatial.Hexes.Single().HexId, Is.EqualTo("hex-a"));
        Assert.That(snapshot.Spatial.AnchoredLocations.Single().AnchorHexId, Is.EqualTo("hex-a"));
        Assert.That(WorldStateInvariantValidator.Validate(snapshot).IsValid, Is.True);
        Assert.That(WorldStateCanonicalWriter.Write(snapshot), Does.Contain("SPATIAL_LOCATION|location-a|hex-a"));
    }

    private static SpatialAuthorityStore CreateAuthority(string hexId, string locationId)
    {
        SpatialAuthorityStore store = new SpatialAuthorityStore();
        Assert.That(store.TryRegisterHex(new HexRecord(new HexId(hexId)), out _), Is.True);
        Assert.That(store.TryRegisterLocation(
            new LocationRecord(new LocationId(locationId), new HexId(hexId)), out _), Is.True);
        return store;
    }
}
