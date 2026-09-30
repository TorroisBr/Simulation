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

    private static OwnerSectionCensusWitness FindWitness(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        string sectionId)
    {
        return providers.Single(provider => provider.GetCurrentCensus().SectionId == sectionId).GetCurrentCensus();
    }
}
