using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public sealed class LegacySpatialAnchorBindingStoreCoreTests
{
    [Test]
    public void CoreAnchorAuthorityPreservesOneToOneLocationBindingAndRevision()
    {
        LocationLookup locations = new LocationLookup("loc-a", "loc-b");
        LegacySpatialAnchorBindingStore anchors = new LegacySpatialAnchorBindingStore(locations);

        Assert.That(anchors.TryBindCity("city-a", new LocationId("loc-a"), out SpatialAnchorBindingFailure firstFailure), Is.True);
        Assert.That(firstFailure.Code, Is.EqualTo(SpatialAnchorBindingFailureCode.None));
        Assert.That(anchors.Revision, Is.EqualTo(1));
        Assert.That(anchors.TryBindCity("city-a", new LocationId("loc-a"), out SpatialAnchorBindingFailure retryFailure), Is.True);
        Assert.That(retryFailure.Code, Is.EqualTo(SpatialAnchorBindingFailureCode.None));
        Assert.That(anchors.Revision, Is.EqualTo(1), "An idempotent binding must not publish a second revision.");

        Assert.That(anchors.TryBindCity("city-a", new LocationId("loc-b"), out SpatialAnchorBindingFailure ownerConflict), Is.False);
        Assert.That(ownerConflict.Code, Is.EqualTo(SpatialAnchorBindingFailureCode.OwnerAlreadyBound));
        Assert.That(anchors.TryBindCity("city-b", new LocationId("loc-a"), out SpatialAnchorBindingFailure locationConflict), Is.False);
        Assert.That(locationConflict.Code, Is.EqualTo(SpatialAnchorBindingFailureCode.LocationAlreadyBound));
        Assert.That(anchors.TryBindCity("city-b", new LocationId("missing"), out SpatialAnchorBindingFailure missingLocation), Is.False);
        Assert.That(missingLocation.Code, Is.EqualTo(SpatialAnchorBindingFailureCode.LocationNotRegistered));
        Assert.That(anchors.Count, Is.EqualTo(1));
        Assert.That(anchors.Revision, Is.EqualTo(1));
        Assert.That(anchors.ValidateInvariants().IsValid, Is.True);

        LegacySpatialAnchorBindingStore restored = anchors.Clone(locations);
        Assert.That(restored.Revision, Is.EqualTo(anchors.Revision));
        Assert.That(restored.Count, Is.EqualTo(anchors.Count));
        Assert.That(restored.TryGet(new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.City, "city-a"), out LocationId restoredLocation), Is.True);
        Assert.That(restoredLocation, Is.EqualTo(new LocationId("loc-a")));
        Assert.That(restored.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void CoreAnchorAuthorityRetainsComposedOwnerValidation()
    {
        LocationLookup locations = new LocationLookup("loc-a");
        LegacySpatialAnchorBindingStore anchors = new LegacySpatialAnchorBindingStore(locations, _ => false);

        Assert.That(anchors.TryBindCity("city-a", new LocationId("loc-a"), out SpatialAnchorBindingFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAnchorBindingFailureCode.OwnerNotRegistered));
        Assert.That(anchors.Count, Is.Zero);
        Assert.That(anchors.Revision, Is.Zero);
    }

    [Test]
    public void CoreSpatialAuthorityComposesAuthoredLocationAndOwnsItsAnchorBinding()
    {
        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        SpatialWorldScaleContext scale = new SpatialWorldScaleContext(
            "scale-v1", "test-content", "revision-1", 1m, "km");
        HexRecord hex = new HexRecord(
            new HexId("hex-a"),
            new HexCoordinate(0, 0),
            new TerrainReference(new TerrainDefinitionId("terrain-a"), "terrain-revision-1"));
        LocationRecord location = new LocationRecord(new LocationId("location-a"), new HexId("hex-a"));

        Assert.That(spatial.TryComposeGeography(
            new SpatialGeographyDefinition(scale, new[] { hex }, new[] { location }),
            out SpatialAuthorityFailure compositionFailure), Is.True);
        Assert.That(compositionFailure.Code, Is.EqualTo(SpatialAuthorityFailureCode.None));
        Assert.That(spatial.Revision, Is.EqualTo(1));
        Assert.That(spatial.TryGet(new LocationId("location-a"), out LocationRecord registered), Is.True);
        Assert.That(registered.AnchorHexId, Is.EqualTo(new HexId("hex-a")));

        LegacySpatialAnchorBindingStore anchors = new LegacySpatialAnchorBindingStore(spatial);
        Assert.That(anchors.TryBindCity("city-a", registered.Id, out SpatialAnchorBindingFailure anchorFailure), Is.True);
        Assert.That(anchorFailure.Code, Is.EqualTo(SpatialAnchorBindingFailureCode.None));
        Assert.That(anchors.ValidateInvariants().IsValid, Is.True);
        Assert.That(spatial.ValidateInvariants().IsValid, Is.True);
        Assert.That(spatial.LocalTopologyBindingCount, Is.Zero,
            "Daily-v1 location composition must not manufacture a P10 topology binding.");
    }

    private sealed class LocationLookup : ISpatialLocationLookup
    {
        private readonly HashSet<string> locationIds = new HashSet<string>();

        internal LocationLookup(params string[] ids)
        {
            foreach (string id in ids) locationIds.Add(id);
        }

        public bool TryGet(LocationId id, out LocationRecord location)
        {
            if (id != null && locationIds.Contains(id.Value))
            {
                location = new LocationRecord(new LocationId(id.Value), new HexId("anchor-hex"));
                return true;
            }

            location = null;
            return false;
        }
    }
}
