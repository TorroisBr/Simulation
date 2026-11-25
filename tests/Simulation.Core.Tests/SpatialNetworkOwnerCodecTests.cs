using System.Linq;
using NUnit.Framework;

public sealed class SpatialNetworkOwnerCodecTests
{
    [Test]
    public void RoundTripsSortedSpatialOwnerValuesThroughCoreCodec()
    {
        SpatialNetworkOwnerSnapshot source = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
            3L,
            new[] { "location-b", "location-a" },
            new[]
            {
                new SpatialRouteOwnerSnapshotRecord(
                    "route-a", "location-a", "location-b", 2)
            });

        Assert.That(P12ASpatialNetworkOwnerCodec.TryEncode(source,
            out P12AOwnerSection locations, out P12AOwnerSection routes, out P12AResult encodeError),
            Is.True, encodeError?.SectionCode);
        Assert.That(locations.Id, Is.EqualTo(P12ASpatialNetworkOwnerCodec.LocationsSectionId));
        Assert.That(routes.Id, Is.EqualTo(P12ASpatialNetworkOwnerCodec.RoutesSectionId));
        Assert.That(locations.Rows.Select(row => row.OwnerKey),
            Is.EqualTo(new[] { "location-a", "location-b" }));
        Assert.That(routes.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[] { "route-a" }));

        Assert.That(P12ASpatialNetworkOwnerCodec.TryDecode(locations, routes,
            out SpatialNetworkOwnerSnapshot decoded, out P12AResult decodeError),
            Is.True, decodeError?.SectionCode);
        Assert.That(decoded.Revision, Is.EqualTo(3L));
        Assert.That(decoded.LocationRuntimeIds, Is.EqualTo(new[] { "location-a", "location-b" }));
        Assert.That(decoded.Routes, Has.Count.EqualTo(1));
        Assert.That(decoded.Routes[0].RuntimeId, Is.EqualTo("route-a"));
        Assert.That(decoded.Routes[0].OriginLocationRuntimeId, Is.EqualTo("location-a"));
        Assert.That(decoded.Routes[0].DestinationLocationRuntimeId, Is.EqualTo("location-b"));
        Assert.That(decoded.Routes[0].TravelDays, Is.EqualTo(2));
    }

    [Test]
    public void RejectsRouteWithUnresolvedSpatialEndpointBeforeProducingSections()
    {
        SpatialNetworkOwnerSnapshot source = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
            3L,
            new[] { "location-a", "location-b" },
            new[]
            {
                new SpatialRouteOwnerSnapshotRecord(
                    "route-a", "location-a", "missing-location", 2)
            });

        Assert.That(P12ASpatialNetworkOwnerCodec.TryEncode(source,
            out P12AOwnerSection locations, out P12AOwnerSection routes, out _), Is.False);
        Assert.That(locations, Is.Null);
        Assert.That(routes, Is.Null);
    }

    [Test]
    public void DecodeRejectsMismatchedSectionRevision()
    {
        SpatialNetworkOwnerSnapshot source = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
            3L,
            new[] { "location-a", "location-b" },
            new[]
            {
                new SpatialRouteOwnerSnapshotRecord(
                    "route-a", "location-a", "location-b", 2)
            });
        Assert.That(P12ASpatialNetworkOwnerCodec.TryEncode(source,
            out P12AOwnerSection locations, out P12AOwnerSection routes, out _), Is.True);
        P12AOwnerSection mismatchedRoutes = new P12AOwnerSection(
            routes.Id, routes.Major, routes.Minor, routes.Role, routes.AliasGroup,
            routes.Revision + 1L, routes.Rows);

        Assert.That(P12ASpatialNetworkOwnerCodec.TryDecode(locations, mismatchedRoutes,
            out SpatialNetworkOwnerSnapshot decoded, out _), Is.False);
        Assert.That(decoded, Is.Null);
    }
}
