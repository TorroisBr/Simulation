using System.Linq;
using NUnit.Framework;

public sealed class SpatialNetworkOwnerHydratorTests
{
    [Test]
    public void HydratesExactLocationsRoutesEndpointsAndRevision()
    {
        SpatialNetworkOwnerSnapshot snapshot = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
            11L,
            new[] { "location-z", "location-a" },
            new[]
            {
                new SpatialRouteOwnerSnapshotRecord(
                    "route-z-a", "location-z", "location-a", 3),
                new SpatialRouteOwnerSnapshotRecord(
                    "route-a-z", "location-a", "location-z", 5)
            });

        Assert.That(P12DSpatialNetworkOwnerHydrator.TryCreateOwner(
            snapshot,
            out SpatialNetworkOwner owner,
            out SpatialNetworkSnapshotFailure failure), Is.True, failure?.Message);

        SpatialLocationRuntime[] locations = owner.Locations.ToArray();
        Assert.That(locations.Select(location => location.RuntimeId),
            Is.EqualTo(new[] { "location-z", "location-a" }));
        Assert.That(owner.Routes.Select(route => route.RuntimeId),
            Is.EqualTo(new[] { "route-z-a", "route-a-z" }));
        Assert.That(owner.Routes[0].Origin, Is.SameAs(locations[0]));
        Assert.That(owner.Routes[0].Destination, Is.SameAs(locations[1]));
        Assert.That(owner.Routes[0].TravelDays, Is.EqualTo(3));
        Assert.That(owner.Routes[1].Origin, Is.SameAs(locations[1]));
        Assert.That(owner.Routes[1].Destination, Is.SameAs(locations[0]));
        Assert.That(owner.Routes[1].TravelDays, Is.EqualTo(5));
        Assert.That(owner.Revision, Is.EqualTo(11L));
        Assert.That(owner.GetOutgoingRoutes(locations[0]).Select(route => route.RuntimeId),
            Is.EqualTo(new[] { "route-z-a" }));
    }

    [Test]
    public void HydratedOwnerContinuesFromRestoredRevision()
    {
        SpatialNetworkOwnerSnapshot snapshot = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
            23L,
            new[] { "location-a" },
            new SpatialRouteOwnerSnapshotRecord[0]);

        Assert.That(P12DSpatialNetworkOwnerHydrator.TryCreateOwner(
            snapshot, out SpatialNetworkOwner owner, out _), Is.True);
        Assert.That(owner.TryRegisterLocation(
            new SpatialLocationRuntime("location-next"),
            () => true,
            () => { },
            null), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(24L));
    }

    [Test]
    public void RejectsMalformedSnapshotWithoutReturningPartialOwner()
    {
        SpatialRouteOwnerSnapshotRecord validRoute = new SpatialRouteOwnerSnapshotRecord(
            "route-a", "location-a", "location-b", 2);
        SpatialNetworkOwnerSnapshot[] invalidSnapshots =
        {
            null,
            new SpatialNetworkOwnerSnapshot(2, 0L, new string[0], new SpatialRouteOwnerSnapshotRecord[0]),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                0L, null, new SpatialRouteOwnerSnapshotRecord[0]),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                0L, new string[0], null),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                -1L, new string[0], new SpatialRouteOwnerSnapshotRecord[0]),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                0L, new[] { " " }, new SpatialRouteOwnerSnapshotRecord[0]),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                1L, new[] { "location-a", "location-a" }, new SpatialRouteOwnerSnapshotRecord[0]),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                3L, new[] { "location-a", "location-b" },
                new[] { new SpatialRouteOwnerSnapshotRecord("location-a", "location-a", "location-b", 2) }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                3L, new[] { "location-a", "location-b" },
                new SpatialRouteOwnerSnapshotRecord[] { null }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                1L, new[] { "location-a" }, new[] { validRoute }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                2L, new[] { "location-a", "location-b" },
                new[] { new SpatialRouteOwnerSnapshotRecord("route-a", "location-a", "location-b", 2) }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                3L, new[] { "location-a", "location-b" },
                new[] { new SpatialRouteOwnerSnapshotRecord("route", "location-a", "missing", 2) }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                3L, new[] { "location-a" },
                new[] { new SpatialRouteOwnerSnapshotRecord("route", "location-a", "location-a", 2) }),
            new SpatialNetworkOwnerSnapshot(SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
                2L, new[] { "location-a", "location-b" },
                new[] { new SpatialRouteOwnerSnapshotRecord("route", "location-a", "location-b", 0) })
        };

        foreach (SpatialNetworkOwnerSnapshot invalid in invalidSnapshots)
        {
            Assert.That(P12DSpatialNetworkOwnerHydrator.TryCreateOwner(
                invalid, out SpatialNetworkOwner owner, out SpatialNetworkSnapshotFailure failure), Is.False);
            Assert.That(owner, Is.Null);
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.Code, Is.Not.EqualTo(SpatialNetworkSnapshotFailureCode.None));
        }
    }
}
