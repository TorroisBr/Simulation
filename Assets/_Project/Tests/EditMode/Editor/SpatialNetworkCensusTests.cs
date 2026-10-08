using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SpatialNetworkCensusTests
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
    public void ProvidersExposeFixedOwnerSectionsAndZeroWitnesses()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime owner = new SpatialNetworkRuntime(registry);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = SpatialNetworkCensusProvider.CreateProviders(owner);

        Assert.That(providers.Select(provider => provider.GetCurrentCensus().SectionId), Is.EqualTo(new[]
        {
            SpatialNetworkCensusProvider.LocationsSectionId,
            SpatialNetworkCensusProvider.RoutesSectionId
        }));

        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            Assert.That(witness.SchemaVersion, Is.EqualTo(SpatialNetworkCensusProvider.SchemaVersion));
            Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
            Assert.That(witness.Cardinality, Is.Zero);
            Assert.That(witness.Revision, Is.Zero);
        }
    }

    [Test]
    public void SuccessfulLocationAndRouteRegistrationAdvanceSharedRevisionOnce()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime owner = new SpatialNetworkRuntime(registry);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = SpatialNetworkCensusProvider.CreateProviders(owner);
        IReadOnlyList<IOwnerSectionCensusProvider> identityProviders = RuntimeIdentityRegistryCensusProvider.CreateProviders(registry);
        SpatialLocationRuntime first = new SpatialLocationRuntime("location-a");
        SpatialLocationRuntime second = new SpatialLocationRuntime("location-b");

        Assert.That(owner.RegisterLocation(first), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(1L));
        Assert.That(owner.RegisterLocation(second), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(2L));
        Assert.That(owner.RegisterRoute(new SpatialRouteRuntime("route-a", first, second, 3)), Is.True);

        OwnerSectionCensusWitness locations = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness routes = providers[1].GetCurrentCensus();
        Assert.That(locations.Cardinality, Is.EqualTo(2));
        Assert.That(routes.Cardinality, Is.EqualTo(1));
        Assert.That(locations.Revision, Is.EqualTo(3L));
        Assert.That(routes.Revision, Is.EqualTo(locations.Revision));

        OwnerSectionCensusWitness identityLocations = FindWitness(
            identityProviders,
            RuntimeIdentityRegistryCensusProvider.LocationsSectionId);
        OwnerSectionCensusWitness identityRoutes = FindWitness(
            identityProviders,
            RuntimeIdentityRegistryCensusProvider.RoutesSectionId);
        Assert.That(identityLocations.Cardinality, Is.EqualTo(2));
        Assert.That(identityRoutes.Cardinality, Is.EqualTo(1));
        Assert.That(identityLocations.Revision, Is.EqualTo(3L));
        Assert.That(identityRoutes.Revision, Is.EqualTo(identityLocations.Revision));
    }

    [Test]
    public void RejectedRegistrationsLeaveSpatialOwnerWitnessesUnchanged()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime owner = new SpatialNetworkRuntime(registry);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = SpatialNetworkCensusProvider.CreateProviders(owner);
        SpatialLocationRuntime registered = new SpatialLocationRuntime("location-a");
        Assert.That(owner.RegisterLocation(registered), Is.True);
        OwnerSectionCensusWitness beforeLocations = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness beforeRoutes = providers[1].GetCurrentCensus();

        LogAssert.Expect(LogType.Error, "Cannot register spatial location: runtime instance is null.");
        LogAssert.Expect(LogType.Error, "Spatial location 'location-a' is already registered in this network.");
        LogAssert.Expect(LogType.Error, "Cannot register spatial route: runtime instance is null.");
        LogAssert.Expect(LogType.Error, "Cannot register spatial route 'route-unbound': origin and destination must both be registered locations.");
        Assert.That(owner.RegisterLocation(null), Is.False);
        Assert.That(owner.RegisterLocation(registered), Is.False);
        Assert.That(owner.RegisterRoute(null), Is.False);
        Assert.That(owner.RegisterRoute(new SpatialRouteRuntime(
            "route-unbound",
            registered,
            new SpatialLocationRuntime("location-unregistered"),
            1)), Is.False);

        Assert.That(providers[0].GetCurrentCensus().Cardinality, Is.EqualTo(beforeLocations.Cardinality));
        Assert.That(providers[0].GetCurrentCensus().Revision, Is.EqualTo(beforeLocations.Revision));
        Assert.That(providers[1].GetCurrentCensus().Cardinality, Is.EqualTo(beforeRoutes.Cardinality));
        Assert.That(providers[1].GetCurrentCensus().Revision, Is.EqualTo(beforeRoutes.Revision));
    }

    [Test]
    public void CrossTypeIdentityCollisionAndRegistrySaturationDoNotPartiallyRegister()
    {
        RuntimeIdentityRegistry collisionRegistry = new RuntimeIdentityRegistry();
        Assert.That(collisionRegistry.RegisterCity(SimulationTestFactory.CreateCity("shared-id", "city-location")), Is.True);
        SpatialNetworkRuntime collisionOwner = new SpatialNetworkRuntime(collisionRegistry);
        IReadOnlyList<IOwnerSectionCensusProvider> collisionProviders = SpatialNetworkCensusProvider.CreateProviders(collisionOwner);
        IReadOnlyList<IOwnerSectionCensusProvider> identityProviders = RuntimeIdentityRegistryCensusProvider.CreateProviders(collisionRegistry);
        OwnerSectionCensusWitness identityLocationsBefore = identityProviders.Single(
            provider => provider.GetCurrentCensus().SectionId == RuntimeIdentityRegistryCensusProvider.LocationsSectionId)
            .GetCurrentCensus();

        LogAssert.Expect(LogType.Error, "Duplicate RuntimeId 'shared-id' while registering Location; it is already registered as City.");
        Assert.That(collisionOwner.RegisterLocation(new SpatialLocationRuntime("shared-id")), Is.False);
        Assert.That(collisionOwner.LocationCount, Is.Zero);
        Assert.That(collisionOwner.Revision, Is.Zero);
        Assert.That(collisionProviders[0].GetCurrentCensus().Cardinality, Is.Zero);
        OwnerSectionCensusWitness identityLocationsAfter = identityProviders.Single(
            provider => provider.GetCurrentCensus().SectionId == RuntimeIdentityRegistryCensusProvider.LocationsSectionId)
            .GetCurrentCensus();
        Assert.That(identityLocationsAfter.Cardinality, Is.EqualTo(identityLocationsBefore.Cardinality));
        Assert.That(identityLocationsAfter.Revision, Is.EqualTo(identityLocationsBefore.Revision));

        RuntimeIdentityRegistry saturatedRegistry = new RuntimeIdentityRegistry();
        typeof(RuntimeIdentityRegistry).GetField("censusRevision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(saturatedRegistry, long.MaxValue);
        SpatialNetworkRuntime saturatedOwner = new SpatialNetworkRuntime(saturatedRegistry);
        LogAssert.Expect(LogType.Error, "Cannot register Location runtime identity: census revision is exhausted.");
        Assert.That(saturatedOwner.RegisterLocation(new SpatialLocationRuntime("saturated-location")), Is.False);
        Assert.That(saturatedOwner.LocationCount, Is.Zero);
        Assert.That(saturatedOwner.Revision, Is.Zero);
        Assert.That(saturatedOwner.TryGetLocation("saturated-location", out _), Is.False);
    }

    [Test]
    public void RejectedRouteIdentityCollisionLeavesBothOwnersUnchanged()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterCity(SimulationTestFactory.CreateCity("shared-route-id", "city-location")), Is.True);
        SpatialNetworkRuntime owner = new SpatialNetworkRuntime(registry);
        SpatialLocationRuntime origin = new SpatialLocationRuntime("route-origin");
        SpatialLocationRuntime destination = new SpatialLocationRuntime("route-destination");
        Assert.That(owner.RegisterLocation(origin), Is.True);
        Assert.That(owner.RegisterLocation(destination), Is.True);

        IReadOnlyList<IOwnerSectionCensusProvider> networkProviders = SpatialNetworkCensusProvider.CreateProviders(owner);
        IReadOnlyList<IOwnerSectionCensusProvider> identityProviders = RuntimeIdentityRegistryCensusProvider.CreateProviders(registry);
        OwnerSectionCensusWitness networkLocationsBefore = networkProviders[0].GetCurrentCensus();
        OwnerSectionCensusWitness networkRoutesBefore = networkProviders[1].GetCurrentCensus();
        OwnerSectionCensusWitness identityRoutesBefore = FindWitness(
            identityProviders,
            RuntimeIdentityRegistryCensusProvider.RoutesSectionId);

        LogAssert.Expect(LogType.Error,
            "Duplicate RuntimeId 'shared-route-id' while registering Route; it is already registered as City.");
        Assert.That(owner.RegisterRoute(new SpatialRouteRuntime(
            "shared-route-id",
            origin,
            destination,
            1)), Is.False);

        OwnerSectionCensusWitness networkLocationsAfter = networkProviders[0].GetCurrentCensus();
        OwnerSectionCensusWitness networkRoutesAfter = networkProviders[1].GetCurrentCensus();
        OwnerSectionCensusWitness identityRoutesAfter = FindWitness(
            identityProviders,
            RuntimeIdentityRegistryCensusProvider.RoutesSectionId);
        Assert.That(networkLocationsAfter.Cardinality, Is.EqualTo(networkLocationsBefore.Cardinality));
        Assert.That(networkLocationsAfter.Revision, Is.EqualTo(networkLocationsBefore.Revision));
        Assert.That(networkRoutesAfter.Cardinality, Is.EqualTo(networkRoutesBefore.Cardinality));
        Assert.That(networkRoutesAfter.Revision, Is.EqualTo(networkRoutesBefore.Revision));
        Assert.That(identityRoutesAfter.Cardinality, Is.EqualTo(identityRoutesBefore.Cardinality));
        Assert.That(identityRoutesAfter.Revision, Is.EqualTo(identityRoutesBefore.Revision));
    }

    [Test]
    public void SpatialNetworkRevisionSaturationPreflightsIdentityRegistration()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime owner = new SpatialNetworkRuntime(registry);
        typeof(SpatialNetworkRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(owner, long.MaxValue);

        LogAssert.Expect(LogType.Error, "Cannot register spatial location: spatial network census revision is exhausted.");
        Assert.That(owner.RegisterLocation(new SpatialLocationRuntime("saturated-location")), Is.False);
        Assert.That(owner.LocationCount, Is.Zero);
        Assert.That(owner.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(owner.TryGetLocation("saturated-location", out _), Is.False);
    }

    [Test]
    public void SpatialCollectionsAreDetachedReadOnlySnapshotsAndPreserveAdjacency()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime owner = new SpatialNetworkRuntime(registry);
        SpatialLocationRuntime origin = new SpatialLocationRuntime("location-origin");
        SpatialLocationRuntime firstDestination = new SpatialLocationRuntime("location-first");
        SpatialLocationRuntime secondDestination = new SpatialLocationRuntime("location-second");
        Assert.That(owner.RegisterLocation(origin), Is.True);
        Assert.That(owner.RegisterLocation(firstDestination), Is.True);
        SpatialRouteRuntime firstRoute = new SpatialRouteRuntime("route-first", origin, firstDestination, 2);
        Assert.That(owner.RegisterRoute(firstRoute), Is.True);

        IEnumerable<SpatialLocationRuntime> locationSnapshot = owner.Locations;
        IReadOnlyList<SpatialRouteRuntime> routeSnapshot = owner.Routes;
        IReadOnlyList<SpatialRouteRuntime> outgoingSnapshot = owner.GetOutgoingRoutes(origin);
        Assert.Throws<System.NotSupportedException>(() => ((IList<SpatialLocationRuntime>)locationSnapshot).Add(secondDestination));
        Assert.Throws<System.NotSupportedException>(() => ((IList<SpatialRouteRuntime>)routeSnapshot).Add(
            new SpatialRouteRuntime("route-illegal", origin, firstDestination, 1)));
        Assert.Throws<System.NotSupportedException>(() => ((IList<SpatialRouteRuntime>)outgoingSnapshot).Clear());

        Assert.That(owner.RegisterLocation(secondDestination), Is.True);
        SpatialRouteRuntime secondRoute = new SpatialRouteRuntime("route-second", origin, secondDestination, 4);
        Assert.That(owner.RegisterRoute(secondRoute), Is.True);

        Assert.That(locationSnapshot.Count(), Is.EqualTo(2));
        Assert.That(routeSnapshot, Has.Count.EqualTo(1));
        Assert.That(outgoingSnapshot, Has.Count.EqualTo(1));
        Assert.That(owner.GetOutgoingRoutes(origin), Is.EqualTo(new[] { firstRoute, secondRoute }));
    }

    [Test]
    public void OwnerSnapshot_RoundTripsEmptyAndPopulatedNetworkWithParallelRouteIdentityAndOrder()
    {
        SpatialNetworkRuntime empty = new SpatialNetworkRuntime(new RuntimeIdentityRegistry());
        SpatialNetworkOwnerSnapshot emptySnapshot = empty.CaptureOwnerSnapshot();
        Assert.That(emptySnapshot.SchemaVersion, Is.EqualTo(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion));
        Assert.That(emptySnapshot.Revision, Is.Zero);
        Assert.That(emptySnapshot.LocationRuntimeIds, Is.Empty);
        Assert.That(emptySnapshot.Routes, Is.Empty);
        Assert.That(SpatialNetworkRuntime.TryCreateFromOwnerSnapshot(
            emptySnapshot,
            new RuntimeIdentityRegistry(),
            out SpatialNetworkRuntime restoredEmpty,
            out SpatialNetworkSnapshotFailure emptyFailure), Is.True, emptyFailure.Message);
        Assert.That(emptyFailure.Code, Is.EqualTo(SpatialNetworkSnapshotFailureCode.None));
        Assert.That(restoredEmpty.LocationCount, Is.Zero);
        Assert.That(restoredEmpty.RouteCount, Is.Zero);
        Assert.That(restoredEmpty.Revision, Is.Zero);

        RuntimeIdentityRegistry sourceRegistry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime source = new SpatialNetworkRuntime(sourceRegistry);
        SpatialLocationRuntime origin = new SpatialLocationRuntime("legacy-origin");
        SpatialLocationRuntime destination = new SpatialLocationRuntime("legacy-destination");
        Assert.That(source.RegisterLocation(origin), Is.True);
        Assert.That(source.RegisterLocation(destination), Is.True);
        Assert.That(source.RegisterRoute(new SpatialRouteRuntime("legacy-route-first", origin, destination, 4)), Is.True);
        Assert.That(source.RegisterRoute(new SpatialRouteRuntime("legacy-route-parallel", origin, destination, 9)), Is.True);
        Assert.That(source.RegisterRoute(new SpatialRouteRuntime("legacy-route-reverse", destination, origin, 2)), Is.True);

        SpatialNetworkOwnerSnapshot snapshot = source.CaptureOwnerSnapshot();
        string[] expectedLocationOrder = source.Locations.Select(location => location.RuntimeId).ToArray();
        Assert.That(snapshot.LocationRuntimeIds, Is.EqualTo(expectedLocationOrder));
        Assert.That(snapshot.Routes.Select(route => route.RuntimeId), Is.EqualTo(new[]
        {
            "legacy-route-first",
            "legacy-route-parallel",
            "legacy-route-reverse"
        }));
        Assert.That(snapshot.Routes[0].OriginLocationRuntimeId, Is.EqualTo("legacy-origin"));
        Assert.That(snapshot.Routes[0].DestinationLocationRuntimeId, Is.EqualTo("legacy-destination"));
        Assert.That(snapshot.Routes[0].TravelDays, Is.EqualTo(4));
        Assert.That(snapshot.Routes[1].OriginLocationRuntimeId, Is.EqualTo("legacy-origin"));
        Assert.That(snapshot.Routes[1].DestinationLocationRuntimeId, Is.EqualTo("legacy-destination"));
        Assert.That(snapshot.Routes[1].TravelDays, Is.EqualTo(9));

        Assert.That(SpatialNetworkRuntime.TryCreateFromOwnerSnapshot(
            snapshot,
            new RuntimeIdentityRegistry(),
            out SpatialNetworkRuntime restored,
            out SpatialNetworkSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(failure.Code, Is.EqualTo(SpatialNetworkSnapshotFailureCode.None));
        Assert.That(restored.Revision, Is.EqualTo(source.Revision));
        Assert.That(restored.Locations.Select(location => location.RuntimeId), Is.EqualTo(expectedLocationOrder));
        Assert.That(restored.Routes.Select(route => route.RuntimeId), Is.EqualTo(new[]
        {
            "legacy-route-first",
            "legacy-route-parallel",
            "legacy-route-reverse"
        }));
        Assert.That(restored.GetOutgoingRoutes(restored.Locations.First(location => location.RuntimeId == "legacy-origin")
            ).Select(route => route.RuntimeId), Is.EqualTo(new[] { "legacy-route-first", "legacy-route-parallel" }));
        Assert.That(restored.Routes[0].TravelDays, Is.EqualTo(4));
        Assert.That(restored.Routes[1].TravelDays, Is.EqualTo(9));
    }

    [Test]
    public void OwnerSnapshot_IsDetachedReadOnlyAndRestoresExactRevisionGaps()
    {
        RuntimeIdentityRegistry sourceRegistry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime source = new SpatialNetworkRuntime(sourceRegistry);
        SpatialLocationRuntime origin = new SpatialLocationRuntime("snapshot-origin");
        SpatialLocationRuntime destination = new SpatialLocationRuntime("snapshot-destination");
        Assert.That(source.RegisterLocation(origin), Is.True);
        Assert.That(source.RegisterLocation(destination), Is.True);
        Assert.That(source.RegisterRoute(new SpatialRouteRuntime("snapshot-route", origin, destination, 1)), Is.True);

        SpatialNetworkOwnerSnapshot detached = source.CaptureOwnerSnapshot();
        IList<string> locationRows = detached.LocationRuntimeIds as IList<string>;
        IList<SpatialRouteOwnerSnapshotRecord> routeRows = detached.Routes as IList<SpatialRouteOwnerSnapshotRecord>;
        Assert.That(locationRows, Is.Not.Null);
        Assert.That(routeRows, Is.Not.Null);
        Assert.That(locationRows.IsReadOnly, Is.True);
        Assert.That(routeRows.IsReadOnly, Is.True);
        Assert.Throws<System.NotSupportedException>(() => locationRows.Add("illegal-location"));
        Assert.Throws<System.NotSupportedException>(() => routeRows.Clear());

        Assert.That(source.RegisterLocation(new SpatialLocationRuntime("snapshot-later-location")), Is.True);
        Assert.That(detached.LocationRuntimeIds, Has.Count.EqualTo(2));
        Assert.That(detached.Routes, Has.Count.EqualTo(1));
        Assert.That(detached.Revision, Is.EqualTo(3L));

        SpatialNetworkOwnerSnapshot withRevisionGap = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
            19L,
            detached.LocationRuntimeIds,
            detached.Routes);
        Assert.That(SpatialNetworkRuntime.TryCreateFromOwnerSnapshot(
            withRevisionGap,
            new RuntimeIdentityRegistry(),
            out SpatialNetworkRuntime restored,
            out SpatialNetworkSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(restored.Revision, Is.EqualTo(19L));
        Assert.That(restored.RegisterLocation(new SpatialLocationRuntime("after-gap-location")), Is.True);
        Assert.That(restored.Revision, Is.EqualTo(20L));
    }

    [Test]
    public void OwnerSnapshot_RejectsMalformedAndImpossibleStateWithoutChangingActiveOwner()
    {
        SpatialRouteOwnerSnapshotRecord validRoute = new SpatialRouteOwnerSnapshotRecord(
            "route-valid", "location-origin", "location-destination", 2);
        SpatialNetworkOwnerSnapshot[] invalidSnapshots =
        {
            null,
            new SpatialNetworkOwnerSnapshot(2, 0, new string[0], new SpatialRouteOwnerSnapshotRecord[0]),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, -1, new string[0], new SpatialRouteOwnerSnapshotRecord[0]),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 0, null, new SpatialRouteOwnerSnapshotRecord[0]),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 0, new string[0], null),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 0, new[] { " " }, new SpatialRouteOwnerSnapshotRecord[0]),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 1, new string[] { null }, new SpatialRouteOwnerSnapshotRecord[0]),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 1, new[] { "duplicate", "duplicate" }, new SpatialRouteOwnerSnapshotRecord[0]),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 0, new[] { "location-origin" }, new[] { validRoute }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 3, new[] { "location-origin", "location-destination" }, new SpatialRouteOwnerSnapshotRecord[] { null }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 3, new[] { "location-origin", "location-destination" }, new[]
            {
                new SpatialRouteOwnerSnapshotRecord(" ", "location-origin", "location-destination", 2)
            }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 3, new[] { "location-origin", "location-destination" }, new[]
            {
                new SpatialRouteOwnerSnapshotRecord(null, "location-origin", "location-destination", 2)
            }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 3, new[] { "location-origin", "location-destination" }, new[]
            {
                new SpatialRouteOwnerSnapshotRecord("route-valid", null, "location-destination", 2)
            }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 3, new[] { "location-origin", "location-destination" }, new[]
            {
                new SpatialRouteOwnerSnapshotRecord("route-valid", "location-origin", null, 2)
            }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 3, new[] { "location-origin", "location-destination" }, new[]
            {
                new SpatialRouteOwnerSnapshotRecord("route-valid", "missing-origin", "location-destination", 2)
            }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 3, new[] { "location-origin", "location-destination" }, new[]
            {
                new SpatialRouteOwnerSnapshotRecord("route-valid", "location-origin", "missing-destination", 2)
            }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 3, new[] { "location-origin" }, new[]
            {
                new SpatialRouteOwnerSnapshotRecord("route-valid", "location-origin", "location-origin", 2)
            }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 3, new[] { "location-origin", "location-destination" }, new[]
            {
                new SpatialRouteOwnerSnapshotRecord("route-valid", "location-origin", "location-destination", 0)
            }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 3, new[] { "location-origin", "location-destination" }, new[]
            {
                new SpatialRouteOwnerSnapshotRecord("route-valid", "location-origin", "location-destination", -1)
            }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 4, new[] { "location-origin", "location-destination" }, new[]
            {
                validRoute,
                new SpatialRouteOwnerSnapshotRecord("route-valid", "location-origin", "location-destination", 3)
            }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 3, new[] { "location-origin", "location-destination" }, new[]
            {
                validRoute,
                new SpatialRouteOwnerSnapshotRecord("location-origin", "location-destination", "location-origin", 3)
            })
        };

        RuntimeIdentityRegistry activeRegistry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime active = new SpatialNetworkRuntime(activeRegistry);
        SpatialLocationRuntime activeOrigin = new SpatialLocationRuntime("active-origin");
        SpatialLocationRuntime activeDestination = new SpatialLocationRuntime("active-destination");
        Assert.That(active.RegisterLocation(activeOrigin), Is.True);
        Assert.That(active.RegisterLocation(activeDestination), Is.True);
        Assert.That(active.RegisterRoute(new SpatialRouteRuntime("active-route", activeOrigin, activeDestination, 5)), Is.True);
        long activeRevision = active.Revision;
        long activeIdentityRevision = activeRegistry.CensusRevision;

        foreach (SpatialNetworkOwnerSnapshot invalid in invalidSnapshots)
        {
            Assert.That(SpatialNetworkRuntime.TryCreateFromOwnerSnapshot(
                invalid,
                activeRegistry,
                out SpatialNetworkRuntime rejected,
                out SpatialNetworkSnapshotFailure failure), Is.False);
            Assert.That(rejected, Is.Null);
            Assert.That(failure.Code, Is.Not.EqualTo(SpatialNetworkSnapshotFailureCode.None));
            Assert.That(active.LocationCount, Is.EqualTo(2));
            Assert.That(active.RouteCount, Is.EqualTo(1));
            Assert.That(active.Revision, Is.EqualTo(activeRevision));
            Assert.That(activeRegistry.CensusRevision, Is.EqualTo(activeIdentityRevision));
            Assert.That(active.TryGetLocation("active-origin", out SpatialLocationRuntime stillOrigin), Is.True);
            Assert.That(stillOrigin, Is.SameAs(activeOrigin));
            Assert.That(active.TryGetRoute("active-route", out SpatialRouteRuntime stillRoute), Is.True);
            Assert.That(stillRoute.TravelDays, Is.EqualTo(5));
        }
    }

    [Test]
    public void OwnerSnapshot_RejectsRegistryIdentityCollisionAndCapacityBeforeRegistration()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime existing = new SpatialNetworkRuntime(registry);
        Assert.That(existing.RegisterLocation(new SpatialLocationRuntime("occupied")), Is.True);
        long revisionBefore = registry.CensusRevision;
        SpatialNetworkOwnerSnapshot collision = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
            1,
            new[] { "occupied" },
            new SpatialRouteOwnerSnapshotRecord[0]);

        Assert.That(SpatialNetworkRuntime.TryCreateFromOwnerSnapshot(
            collision,
            registry,
            out SpatialNetworkRuntime rejectedCollision,
            out SpatialNetworkSnapshotFailure collisionFailure), Is.False);
        Assert.That(rejectedCollision, Is.Null);
        Assert.That(collisionFailure.Code, Is.EqualTo(SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision));
        Assert.That(registry.CensusRevision, Is.EqualTo(revisionBefore));
        Assert.That(existing.LocationCount, Is.EqualTo(1));

        RuntimeIdentityRegistry saturated = new RuntimeIdentityRegistry();
        typeof(RuntimeIdentityRegistry).GetField("censusRevision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(saturated, long.MaxValue);
        SpatialNetworkOwnerSnapshot capacity = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
            1,
            new[] { "fresh-location" },
            new SpatialRouteOwnerSnapshotRecord[0]);
        Assert.That(SpatialNetworkRuntime.TryCreateFromOwnerSnapshot(
            capacity,
            saturated,
            out SpatialNetworkRuntime rejectedCapacity,
            out SpatialNetworkSnapshotFailure capacityFailure), Is.False);
        Assert.That(rejectedCapacity, Is.Null);
        Assert.That(capacityFailure.Code, Is.EqualTo(SpatialNetworkSnapshotFailureCode.RuntimeIdentityRevisionExhausted));
        Assert.That(saturated.CensusRevision, Is.EqualTo(long.MaxValue));
        Assert.That(saturated.TryGetLocation("fresh-location", out _), Is.False);
    }

    private static OwnerSectionCensusWitness FindWitness(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        string sectionId)
    {
        return providers.Single(provider => provider.GetCurrentCensus().SectionId == sectionId).GetCurrentCensus();
    }
}
