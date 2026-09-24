using System;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The scale values and terrain IDs below are explicitly authored test-fixture
/// content. They are not defaults for production worlds.
/// </summary>
public sealed class SpatialGeographyTests
{
    [Test]
    public void AxialCoordinateUsesPairEqualityLexicographicOrderAndCheckedOffsets()
    {
        HexCoordinate first = new HexCoordinate(2, -3);
        HexCoordinate samePair = new HexCoordinate(2, -3);
        HexCoordinate nextQ = new HexCoordinate(3, -100);

        Assert.That(first, Is.EqualTo(samePair));
        Assert.That(first.GetHashCode(), Is.EqualTo(samePair.GetHashCode()));
        Assert.That(first.CompareTo(new HexCoordinate(2, -2)), Is.LessThan(0));
        Assert.That(first.CompareTo(nextQ), Is.LessThan(0));
        Assert.That(new HexCoordinate(int.MaxValue, 4).TryOffset(new HexCoordinate(1, 0), out _), Is.False);
        Assert.That(new HexCoordinate(4, int.MinValue).TryOffset(new HexCoordinate(0, -1), out _), Is.False);
    }

    [Test]
    public void FiniteGeographyComposesAtomicallyAndValidatesCoordinatesAndAnchors()
    {
        SpatialAuthorityStore store = new SpatialAuthorityStore();
        SpatialGeographyDefinition duplicateCoordinates = new SpatialGeographyDefinition(
            CreateFixtureScale(false),
            new[]
            {
                GeographicHex("hex-a", 0, 0, "terrain.fixture.plains"),
                GeographicHex("hex-b", 0, 0, "terrain.fixture.forest")
            },
            new[] { new LocationRecord(new LocationId("location-a"), new HexId("hex-a")) });

        Assert.That(store.TryComposeGeography(duplicateCoordinates, out SpatialAuthorityFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure.Code, Is.EqualTo(SpatialAuthorityFailureCode.DuplicateHexCoordinate));
        Assert.That(store.HexCount, Is.Zero);
        Assert.That(store.LocationCount, Is.Zero);
        Assert.That(store.HasGeography, Is.False);
        Assert.That(store.Revision, Is.Zero);
        Assert.That(store.ValidateInvariants().IsValid, Is.True);

        SpatialGeographyDefinition missingAnchor = new SpatialGeographyDefinition(
            CreateFixtureScale(false),
            new[] { GeographicHex("hex-a", 0, 0, "terrain.fixture.plains") },
            new[] { new LocationRecord(new LocationId("location-a"), new HexId("missing-hex")) });
        Assert.That(store.TryComposeGeography(missingAnchor, out SpatialAuthorityFailure anchorFailure), Is.False);
        Assert.That(anchorFailure.Code, Is.EqualTo(SpatialAuthorityFailureCode.AnchorHexNotRegistered));
        Assert.That(store.HexCount, Is.Zero);
        Assert.That(store.LocationCount, Is.Zero);
        Assert.That(store.Revision, Is.Zero);

        SpatialAuthorityStore authored = CreateAuthoredFixture();
        Assert.That(authored.HasGeography, Is.True);
        Assert.That(authored.Revision, Is.EqualTo(1L), "Initial finite geography composes as one atomic authority mutation.");
        Assert.That(authored.ScaleContext.DistancePerNeighborStep, Is.EqualTo(2m));
        Assert.That(authored.ScaleContext.Unit, Is.EqualTo("kilometers"));
        Assert.That(authored.ScaleContext.SourceIdentity, Is.EqualTo("authored-fixture:p8a-small-world"));
        Assert.That(authored.ScaleContext.SourceVersion, Is.EqualTo("fixture-v1"));
        Assert.That(authored.TryGet(new LocationId("location.fixture.center"), out LocationRecord location), Is.True);
        Assert.That(location.AnchorHexId, Is.EqualTo(new HexId("hex.fixture.center")));
        Assert.That(authored.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void NeighborQueryUsesOnlyTheSixExistingAxialCellsAndReturnsCoordinateOrder()
    {
        SpatialAuthorityStore store = CreateAuthoredFixture(reverseRegistrationOrder: true);

        Assert.That(store.TryGetGeometricNeighbors(
            new HexId("hex.fixture.center"),
            out var neighbors,
            out SpatialAuthorityFailure failure), Is.True, failure.ToString());
        Assert.That(neighbors.Select(hex => hex.Id.Value), Is.EqualTo(new[]
        {
            "hex.fixture.left-lower",
            "hex.fixture.left-upper",
            "hex.fixture.down",
            "hex.fixture.up",
            "hex.fixture.right-lower",
            "hex.fixture.right-upper"
        }));

        SpatialAuthorityStore withMissingCell = CreateAuthoredFixture(includeRightLower: false);
        Assert.That(withMissingCell.TryGetGeometricNeighbors(
            new HexId("hex.fixture.center"),
            out var finiteNeighbors,
            out failure), Is.True, failure.ToString());
        Assert.That(finiteNeighbors.Select(hex => hex.Id.Value), Does.Not.Contain("hex.fixture.right-lower"));
        Assert.That(finiteNeighbors, Has.Count.EqualTo(5));
        Assert.That(withMissingCell.HexCount, Is.EqualTo(6));
    }

    [Test]
    public void IdentityOnlyPhaseSevenStoresRemainScaleFreeAndCannotPretendToHaveAdjacency()
    {
        SpatialAuthorityStore identityOnly = new SpatialAuthorityStore();
        Assert.That(identityOnly.TryRegisterHex(new HexRecord(new HexId("hex.legacy")), out _), Is.True);
        Assert.That(identityOnly.HasGeography, Is.False);
        Assert.That(identityOnly.ScaleContext, Is.Null);

        Assert.That(identityOnly.TryGetGeometricNeighbors(
            new HexId("hex.legacy"), out var neighbors, out SpatialAuthorityFailure noGeography), Is.False);
        Assert.That(noGeography.Code, Is.EqualTo(SpatialAuthorityFailureCode.GeographyNotPresent));
        Assert.That(neighbors, Is.Empty);

        long revision = identityOnly.Revision;
        Assert.That(identityOnly.TryRegisterHex(
            GeographicHex("hex.uncomposed", 1, 0, "terrain.fixture.plains"),
            out SpatialAuthorityFailure compositionRequired), Is.False);
        Assert.That(compositionRequired.Code, Is.EqualTo(SpatialAuthorityFailureCode.GeographicHexRequiresComposition));
        Assert.That(identityOnly.Revision, Is.EqualTo(revision));
        Assert.That(identityOnly.HexCount, Is.EqualTo(1));
        Assert.That(identityOnly.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void RuntimeClonesAuthoredGeographyBeforeTheFirstBoundary()
    {
        SpatialAuthorityStore source = CreateAuthoredFixture();
        long sourceRevision = source.Revision;
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(0L),
            Array.Empty<CityRuntime>(),
            Array.Empty<NpcRuntime>(),
            spatialAuthorityStore: source);

        Assert.That(runtime.SpatialAuthorityStore, Is.Not.SameAs(source));
        Assert.That(runtime.SpatialAuthorityStore.HasGeography, Is.True);
        Assert.That(runtime.SpatialAuthorityStore.HexCount, Is.EqualTo(source.HexCount));
        Assert.That(runtime.SpatialAuthorityStore.ScaleContext, Is.EqualTo(source.ScaleContext));
        Assert.That(runtime.SpatialAuthorityStore.ScaleContext, Is.Not.SameAs(source.ScaleContext));
        Assert.That(runtime.SpatialAuthorityStore.TryGetGeometricNeighbors(
            new HexId("hex.fixture.center"), out var neighbors, out _), Is.True);
        Assert.That(neighbors, Has.Count.EqualTo(6));

        Assert.That(source.TryRegisterLocation(
            new LocationRecord(new LocationId("location.source-only"), new HexId("hex.fixture.center")), out _), Is.True);
        Assert.That(runtime.SpatialAuthorityStore.TryGet(new LocationId("location.source-only"), out _), Is.False);
        Assert.That(source.Revision, Is.EqualTo(sourceRevision + 1));
        Assert.That(runtime.SpatialAuthorityStore.Revision, Is.EqualTo(sourceRevision));
    }

    [Test]
    public void GeographicSnapshotsCanonicalOutputFormatterAndDiffPreserveFactsAndProvenance()
    {
        SpatialAuthorityStore firstAuthority = CreateAuthoredFixture();
        SpatialAuthorityStore sameFactsDifferentOrder = CreateAuthoredFixture(reverseRegistrationOrder: true);
        WorldStateSnapshot first = Snapshot(firstAuthority);
        WorldStateSnapshot same = Snapshot(sameFactsDifferentOrder);
        string canonical = WorldStateCanonicalWriter.Write(first);

        Assert.That(WorldStateCanonicalWriter.Write(same), Is.EqualTo(canonical));
        Assert.That(WorldStateDiff.Compare(first, same).IsEmpty, Is.True);
        Assert.That(canonical, Does.Contain("SPATIAL_COORDINATE_CONVENTION|axial-hex-v1|q-then-r"));
        Assert.That(canonical, Does.Contain("SPATIAL_HEX|hex.fixture.center|0|0|terrain.fixture.plains"));
        Assert.That(canonical, Does.Contain("SPATIAL_WORLD_SCALE|scale.fixture.kilometers|authored-fixture:p8a-small-world|fixture-v1|2|kilometers"));
        Assert.That(WorldStateSnapshotFormatter.Format(first), Does.Contain("World scale: scale.fixture.kilometers"));
        Assert.That(WorldStateSnapshotFormatter.Format(first), Does.Contain("HEX hex.fixture.center axial 0,0 terrain terrain.fixture.plains"));
        Assert.That(WorldStateInvariantValidator.Validate(first).IsValid, Is.True);

        WorldStateSnapshot changed = Snapshot(CreateAuthoredFixture(variant: true));
        var differences = WorldStateDiff.Compare(first, changed).Differences;
        Assert.That(differences.Any(value => value.Section == "SpatialHex" && value.Identity == "hex.fixture.center" && value.Field == "CoordinateQ"), Is.True);
        Assert.That(differences.Any(value => value.Section == "SpatialHex" && value.Identity == "hex.fixture.center" && value.Field == "CoordinateR"), Is.True);
        Assert.That(differences.Any(value => value.Section == "SpatialHex" && value.Identity == "hex.fixture.center" && value.Field == "TerrainDefinitionId"), Is.True);
        Assert.That(differences.Any(value => value.Section == "SpatialWorldScale" && value.Field == "ResolvedConventionId"), Is.True);
        Assert.That(differences.Any(value => value.Section == "SpatialWorldScale" && value.Field == "SourceIdentity"), Is.True);
        Assert.That(differences.Any(value => value.Section == "SpatialWorldScale" && value.Field == "SourceVersion"), Is.True);
        Assert.That(differences.Any(value => value.Section == "SpatialWorldScale" && value.Field == "DistancePerNeighborStep"), Is.True);
        Assert.That(differences.Any(value => value.Section == "SpatialWorldScale" && value.Field == "Unit"), Is.True);
    }

    [Test]
    public void SpatialSnapshotInvariantsRejectIncompleteGeographyAndScaleProvenance()
    {
        WorldStateSpatialSnapshot missingScale = new WorldStateSpatialSnapshot(
            hexes: new[] { new WorldStateHexSnapshot("hex-a", 0, 0, "terrain.fixture.plains") },
            authorityRevision: 1L,
            coordinateConventionVersion: HexCoordinate.ConventionVersion,
            coordinateCanonicalOrder: HexCoordinate.CanonicalOrder);
        WorldStateInvariantReport missingScaleReport = WorldStateInvariantValidator.Validate(
            new WorldStateSnapshot(0L, spatial: missingScale));
        Assert.That(missingScaleReport.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "SpatialGeographyScaleMissing"));

        WorldStateSpatialSnapshot malformedScale = new WorldStateSpatialSnapshot(
            hexes: new[]
            {
                new WorldStateHexSnapshot("hex-a", 0, 0, "terrain.fixture.plains"),
                new WorldStateHexSnapshot("hex-b", 0, 0, "terrain.fixture.forest")
            },
            authorityRevision: 1L,
            coordinateConventionVersion: "unsupported",
            coordinateCanonicalOrder: "r-then-q",
            scaleContext: new WorldStateSpatialScaleContextSnapshot("", "", "", 0m, ""));
        WorldStateInvariantReport malformedReport = WorldStateInvariantValidator.Validate(
            new WorldStateSnapshot(0L, spatial: malformedScale));
        Assert.That(malformedReport.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "DuplicateSpatialHexCoordinate"));
        Assert.That(malformedReport.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "SpatialScaleIdentityMissing"));
        Assert.That(malformedReport.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "SpatialScaleProvenanceMissing"));
        Assert.That(malformedReport.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "SpatialScaleDistanceInvalid"));
        Assert.That(malformedReport.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "SpatialScaleUnitMissing"));
        Assert.That(malformedReport.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "SpatialCoordinateConventionUnsupported"));
        Assert.That(malformedReport.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "SpatialCoordinateOrderUnsupported"));
    }

    private static SpatialAuthorityStore CreateAuthoredFixture(
        bool reverseRegistrationOrder = false,
        bool includeRightLower = true,
        bool variant = false)
    {
        HexRecord[] authoredHexes =
        {
            GeographicHex("hex.fixture.center", variant ? 3 : 0, variant ? 2 : 0,
                variant ? "terrain.fixture.highland" : "terrain.fixture.plains"),
            GeographicHex("hex.fixture.right-upper", 1, 0, "terrain.fixture.forest"),
            GeographicHex("hex.fixture.right-lower", 1, -1, "terrain.fixture.hills"),
            GeographicHex("hex.fixture.down", 0, -1, "terrain.fixture.marsh"),
            GeographicHex("hex.fixture.left-lower", -1, 0, "terrain.fixture.coast"),
            GeographicHex("hex.fixture.left-upper", -1, 1, "terrain.fixture.forest"),
            GeographicHex("hex.fixture.up", 0, 1, "terrain.fixture.hills")
        };
        HexRecord[] selectedHexes = includeRightLower
            ? authoredHexes
            : authoredHexes.Where(hex => hex.Id.Value != "hex.fixture.right-lower").ToArray();
        if (reverseRegistrationOrder)
        {
            Array.Reverse(selectedHexes);
        }

        SpatialWorldScaleContext scaleContext = CreateFixtureScale(variant);
        SpatialGeographyDefinition definition = new SpatialGeographyDefinition(
            scaleContext,
            selectedHexes,
            new[] { new LocationRecord(new LocationId("location.fixture.center"), new HexId("hex.fixture.center")) });
        SpatialAuthorityStore store = new SpatialAuthorityStore();
        Assert.That(store.TryComposeGeography(definition, out SpatialAuthorityFailure failure), Is.True, failure.ToString());
        return store;
    }

    private static SpatialWorldScaleContext CreateFixtureScale(bool variant)
    {
        // These explicit values describe this test fixture only.
        return variant
            ? new SpatialWorldScaleContext(
                "scale.fixture.miles",
                "authored-fixture:p8a-small-world-variant",
                "fixture-v2",
                3m,
                "miles")
            : new SpatialWorldScaleContext(
                "scale.fixture.kilometers",
                "authored-fixture:p8a-small-world",
                "fixture-v1",
                2m,
                "kilometers");
    }

    private static HexRecord GeographicHex(string id, int q, int r, string terrainDefinitionId)
    {
        return new HexRecord(
            new HexId(id),
            new HexCoordinate(q, r),
            new TerrainDefinitionId(terrainDefinitionId));
    }

    private static WorldStateSnapshot Snapshot(SpatialAuthorityStore authority)
    {
        return WorldStateSnapshotBuilder.BuildSnapshot(
            new WorldStateSnapshotContext(spatialAuthorityStore: authority));
    }
}
