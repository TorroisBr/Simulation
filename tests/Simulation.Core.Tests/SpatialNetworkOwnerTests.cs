using System;
using NUnit.Framework;

public sealed class SpatialNetworkOwnerTests
{
    [Test]
    public void RegisteringLocationAndRouteOwnsTopologyAndLocalRevision()
    {
        SpatialNetworkOwner owner = new SpatialNetworkOwner();
        SpatialLocationRuntime origin = new SpatialLocationRuntime("location:1");
        SpatialLocationRuntime destination = new SpatialLocationRuntime("location:2");
        bool originIdentity = false;
        bool destinationIdentity = false;

        Assert.That(owner.TryRegisterLocation(origin,
            () => { originIdentity = true; return true; },
            () => originIdentity = false,
            null), Is.True);
        Assert.That(owner.TryRegisterLocation(destination,
            () => { destinationIdentity = true; return true; },
            () => destinationIdentity = false,
            null), Is.True);

        SpatialRouteRuntime route = new SpatialRouteRuntime("route:1", origin, destination, 3);
        bool routeIdentity = false;
        Assert.That(owner.TryRegisterRoute(route, () => { routeIdentity = true; return true; }), Is.True);

        Assert.That(originIdentity, Is.True);
        Assert.That(destinationIdentity, Is.True);
        Assert.That(routeIdentity, Is.True);
        Assert.That(owner.LocationCount, Is.EqualTo(2));
        Assert.That(owner.RouteCount, Is.EqualTo(1));
        Assert.That(owner.Revision, Is.EqualTo(3));
        Assert.That(owner.TryGetLocation("location:1", out SpatialLocationRuntime foundOrigin), Is.True);
        Assert.That(foundOrigin, Is.SameAs(origin));
        Assert.That(owner.TryGetRoute("route:1", out SpatialRouteRuntime foundRoute), Is.True);
        Assert.That(foundRoute, Is.SameAs(route));
        Assert.That(owner.GetOutgoingRoutes(origin), Is.EqualTo(new[] { route }));
        Assert.That(owner.TryGetSingleDirectRoute(origin, destination,
            out SpatialRouteRuntime direct, out int matching), Is.True);
        Assert.That(direct, Is.SameAs(route));
        Assert.That(matching, Is.EqualTo(1));
    }

    [Test]
    public void FailedLocationPublicationRollsBackIdentityIndexesAndRevision()
    {
        SpatialNetworkOwner owner = new SpatialNetworkOwner();
        SpatialLocationRuntime location = new SpatialLocationRuntime("location:failed");
        bool identityRegistered = false;

        Assert.Throws<InvalidOperationException>(() => owner.TryRegisterLocation(location,
            () => { identityRegistered = true; return true; },
            () => identityRegistered = false,
            stage =>
            {
                if (stage == "SpatialNetwork.Location.OutgoingIndex")
                    throw new InvalidOperationException("injected publication failure");
            }));

        Assert.That(identityRegistered, Is.False);
        Assert.That(owner.LocationCount, Is.Zero);
        Assert.That(owner.RouteCount, Is.Zero);
        Assert.That(owner.Revision, Is.Zero);
        Assert.That(owner.TryGetLocation("location:failed", out _), Is.False);
    }

    [Test]
    public void DuplicateIdsAndNonmemberRouteEndpointsAreRejectedBeforeIdentityRegistration()
    {
        SpatialNetworkOwner owner = new SpatialNetworkOwner();
        SpatialLocationRuntime registered = new SpatialLocationRuntime("location:1");
        Assert.That(owner.TryRegisterLocation(registered, () => true, () => { }, null), Is.True);

        int identityAttempts = 0;
        Assert.That(owner.TryRegisterLocation(new SpatialLocationRuntime("location:1"),
            () => { identityAttempts++; return true; }, () => { }, null), Is.False);
        SpatialRouteRuntime route = new SpatialRouteRuntime(
            "route:missing", registered, new SpatialLocationRuntime("location:outside"), 1);
        Assert.That(owner.TryRegisterRoute(route, () => { identityAttempts++; return true; }), Is.False);

        Assert.That(identityAttempts, Is.Zero);
        Assert.That(owner.LocationCount, Is.EqualTo(1));
        Assert.That(owner.RouteCount, Is.Zero);
        Assert.That(owner.Revision, Is.EqualTo(1));
    }

    [Test]
    public void GenesisRollbackRequiresExactUnconnectedLocation()
    {
        SpatialNetworkOwner owner = new SpatialNetworkOwner();
        SpatialLocationRuntime first = new SpatialLocationRuntime("location:1");
        SpatialLocationRuntime second = new SpatialLocationRuntime("location:2");
        Assert.That(owner.TryRegisterLocation(first, () => true, () => { }, null), Is.True);
        Assert.That(owner.TryRegisterLocation(second, () => true, () => { }, null), Is.True);
        SpatialRouteRuntime route = new SpatialRouteRuntime("route:1", first, second, 1);
        Assert.That(owner.TryRegisterRoute(route, () => true), Is.True);

        Assert.That(owner.TryRollbackGenesisLocation(first, () => { }), Is.False);
        Assert.That(owner.TryRollbackGenesisLocation(new SpatialLocationRuntime("location:2"), () => { }), Is.False);
        Assert.That(owner.TryRollbackGenesisLocation(second, () => { }), Is.True);
        Assert.That(owner.TryGetLocation("location:2", out _), Is.False);
        Assert.That(owner.LocationCount, Is.EqualTo(1));
        Assert.That(owner.RouteCount, Is.EqualTo(1));
        Assert.That(owner.Revision, Is.EqualTo(2));
    }
}
