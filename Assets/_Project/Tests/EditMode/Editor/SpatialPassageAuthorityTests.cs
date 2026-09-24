using System;
using NUnit.Framework;

public sealed class SpatialPassageAuthorityTests
{
    [Test]
    public void BoundaryIdentityIsCanonicalUnorderedAndRequiresDistinctEndpoints()
    {
        HexBoundaryKey forward = new HexBoundaryKey(new HexId("hex.z"), new HexId("hex.a"));
        HexBoundaryKey reverse = new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.z"));

        Assert.That(forward.FirstHexId.Value, Is.EqualTo("hex.a"));
        Assert.That(forward.SecondHexId.Value, Is.EqualTo("hex.z"));
        Assert.That(forward, Is.EqualTo(reverse));
        Assert.That(forward.GetHashCode(), Is.EqualTo(reverse.GetHashCode()));
        Assert.Throws<ArgumentException>(() => new HexBoundaryKey(new HexId("same"), new HexId("same")));
    }

    [Test]
    public void TraversalOptionIdentityIsTypedAndIncludesEveryStableComponent()
    {
        TraversalOptionRef crossing = TraversalOptionRef.ForCrossing(new CrossingId("option-1"));
        TraversalOptionRef connection = TraversalOptionRef.ForConnection(new ConnectionId("option-1"));
        TraversalOptionRef ruleV1 = TraversalOptionRef.ForWildernessRule("rule.basic", "v1");
        TraversalOptionRef ruleV2 = TraversalOptionRef.ForWildernessRule("rule.basic", "v2");

        Assert.That(crossing, Is.Not.EqualTo(connection));
        Assert.That(ruleV1, Is.Not.EqualTo(ruleV2));
        Assert.That(TraversalOptionRef.ForCrossing(new CrossingId("option-1")), Is.EqualTo(crossing));
        Assert.That(TraversalOptionRef.ForConnection(new ConnectionId("option-1")), Is.Not.EqualTo(crossing));
        Assert.That(TraversalOptionRef.ForConnection(new ConnectionId("z")).CompareTo(crossing), Is.LessThan(0));
        Assert.That(ruleV1.CompareTo(ruleV2), Is.LessThan(0));
    }

    [Test]
    public void SpatialAuthorityResolvesCrossingReferenceToRegisteredCrossingAndRegionalHex()
    {
        SpatialAuthorityStore authority = CreateAdjacentGeography();
        Assert.That(authority.TryGetGeometricBoundary(
            new HexId("hex.z"), new HexId("hex.a"), out HexBoundaryKey boundary, out SpatialAuthorityFailure failure),
            Is.True, failure.ToString());
        Assert.That(boundary.FirstHexId.Value, Is.EqualTo("hex.a"));
        Assert.That(boundary.SecondHexId.Value, Is.EqualTo("hex.z"));
        Assert.That(authority.TryRegisterCrossing(
            new CrossingRecord(new CrossingId("crossing.bridge"), boundary, new HexId("hex.z")), out failure),
            Is.True, failure.ToString());

        SpatialReference reference = SpatialReference.ForCrossing(new CrossingId("crossing.bridge"));
        Assert.That(reference.Kind, Is.EqualTo(SpatialReferenceKind.Crossing));
        Assert.That(reference.StableKey, Is.EqualTo("crossing:crossing.bridge"));
        Assert.That(authority.TryResolve(reference, null, out SpatialResolution resolution, out failure), Is.True, failure.ToString());
        Assert.That(resolution.Reference, Is.EqualTo(reference));
        Assert.That(resolution.Crossing.Id, Is.EqualTo(new CrossingId("crossing.bridge")));
        Assert.That(resolution.Crossing.Boundary, Is.EqualTo(boundary));
        Assert.That(resolution.Hex.Id, Is.EqualTo(new HexId("hex.z")));
        Assert.That(resolution.Location, Is.Null);

        Assert.That(authority.TryResolve(
            SpatialReference.ForCrossing(new CrossingId("crossing.missing")), null, out _, out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.CrossingNotRegistered));
    }

    [Test]
    public void CrossingRegistrationRejectsNonAdjacentBoundaryWithoutMutatingRevision()
    {
        SpatialAuthorityStore authority = CreateAdjacentGeography();
        HexBoundaryKey nonAdjacent = new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.missing"));
        long revision = authority.Revision;

        Assert.That(authority.TryRegisterCrossing(
            new CrossingRecord(new CrossingId("crossing.invalid"), nonAdjacent, new HexId("hex.a")),
            out SpatialAuthorityFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.CrossingBoundaryNotAdjacent));
        Assert.That(authority.Revision, Is.EqualTo(revision));
        Assert.That(authority.TryGet(new CrossingId("crossing.invalid"), out _), Is.False);
    }

    private static SpatialAuthorityStore CreateAdjacentGeography()
    {
        HexRecord[] hexes =
        {
            CreateHex("hex.z", 0, 0),
            CreateHex("hex.a", 1, 0),
            CreateHex("hex.isolated", 9, 9)
        };
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("scale.test", "fixture", "v1", 1m, "step"),
            hexes);
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        Assert.That(authority.TryComposeGeography(geography, out SpatialAuthorityFailure failure), Is.True, failure.ToString());
        return authority;
    }

    private static HexRecord CreateHex(string id, int q, int r)
    {
        return new HexRecord(
            new HexId(id),
            new HexCoordinate(q, r),
            new TerrainReference(new TerrainDefinitionId("terrain.test"), "terrain-v1"));
    }
}
