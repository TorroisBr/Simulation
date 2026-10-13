using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12ADRootOwnerCodecTests
{
    [Test]
    public void GenealogyCodecPreservesExactAcyclicEdgesAndRejectsCycles()
    {
        GenealogyOwnerSnapshot source = new GenealogyOwnerSnapshot(
            GenealogyOwnerSnapshot.CurrentSchemaVersion, 4L,
            new[]
            {
                Edge("person-z", "person-child"),
                Edge("person-a", "person-child"),
                Edge("person-child", "person-grandchild")
            });
        Assert.That(P12AGenealogyOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.SectionCode + ":" + encodeError?.Code);
        Assert.That(section.Id, Is.EqualTo(GenealogyCensusProvider.SectionId));
        Assert.That(section.Rows.Select(row => row.OwnerKey), Is.Ordered);
        Assert.That(P12AGenealogyOwnerCodec.TryDecode(section,
            out GenealogyOwnerSnapshot decoded, out P12AResult decodeError),
            Is.True, decodeError?.SectionCode + ":" + decodeError?.Code);
        Assert.That(decoded.Revision, Is.EqualTo(source.Revision));
        Assert.That(decoded.Records.Select(EdgeKey), Is.EquivalentTo(source.Records.Select(EdgeKey)));

        GenealogyOwnerSnapshot cyclic = new GenealogyOwnerSnapshot(
            GenealogyOwnerSnapshot.CurrentSchemaVersion, 2L,
            new[] { Edge("person-a", "person-b"), Edge("person-b", "person-a") });
        Assert.That(P12AGenealogyOwnerCodec.TryEncode(cyclic, out _, out _), Is.False);
    }

    [Test]
    public void SpatialNetworkCodecSortsSetIdentityAndPreservesRouteOrder()
    {
        SpatialNetworkOwnerSnapshot source = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 4L,
            new[] { "location-z", "location-a" },
            new[]
            {
                new SpatialRouteOwnerSnapshotRecord("route-z", "location-z", "location-a", 3),
                new SpatialRouteOwnerSnapshotRecord("route-a", "location-a", "location-z", 2)
            });
        Assert.That(P12ASpatialNetworkOwnerCodec.TryEncode(source,
            out P12AOwnerSection locations, out P12AOwnerSection routes, out P12AResult encodeError),
            Is.True, encodeError?.SectionCode + ":" + encodeError?.Code);
        Assert.That(locations.Rows.Select(row => row.OwnerKey),
            Is.EqualTo(new[] { "location-a", "location-z" }));
        Assert.That(routes.Rows.Select(row => row.OwnerKey),
            Is.EqualTo(new[] { "route-z", "route-a" }), "route list order is owner-defined");
        Assert.That(P12ASpatialNetworkOwnerCodec.TryDecode(locations, routes,
            out SpatialNetworkOwnerSnapshot decoded, out P12AResult decodeError),
            Is.True, decodeError?.SectionCode + ":" + decodeError?.Code);
        Assert.That(decoded.LocationRuntimeIds, Is.EqualTo(new[] { "location-a", "location-z" }));
        Assert.That(decoded.Routes.Select(route => route.RuntimeId), Is.EqualTo(new[] { "route-z", "route-a" }));

        SpatialNetworkOwnerSnapshot invalid = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 2L,
            new[] { "location-a" },
            new[] { new SpatialRouteOwnerSnapshotRecord("route-a", "location-a", "missing", 1) });
        Assert.That(P12ASpatialNetworkOwnerCodec.TryEncode(invalid, out _, out _, out _), Is.False);
    }

    private static ParentageRecord Edge(string parent, string child)
        => new ParentageRecord(new PersonId(parent), new PersonId(child));

    private static string EdgeKey(ParentageRecord record)
        => record.ParentId.Value + "->" + record.ChildId.Value;
}
