using System.Text;
using WorldAtlas.Observations.Models;
using WorldAtlas.Observations.Navigation;
using WorldAtlas.Observations.Parsing;

namespace WorldAtlas.Tests;

[TestFixture]
public sealed class ObservationFileReaderTests
{
    private readonly ObservationFileReader _reader = new();

    [Test]
    public void LoadsBothDeterministicDemoFixturesAndTheirRawByteChain()
    {
        var firstBytes = ObservationFixture.ReadBytes("observation-day-1.json");
        var first = _reader.Read(firstBytes);
        var second = _reader.Read(ObservationFixture.ReadBytes("observation-day-2.json"));

        Assert.That(first.SourceKind, Is.EqualTo(ObservationSourceKind.Demo));
        Assert.That(first.Profile, Is.EqualTo("atlas.demo/v1"));
        Assert.That(first.ObservationIndex, Is.Zero);
        Assert.That(first.PreviousDigest, Is.Null);
        Assert.That(first.WorldId, Is.EqualTo("demo:world"));
        Assert.That(first.Cities.Rows.Select(city => city.CityId), Is.EqualTo(new[] { "city-1", "city-2" }));
        Assert.That(first.TryGetStock("city-1", "wheat", out var before), Is.True);
        Assert.That(before!.Quantity, Is.EqualTo(20));
        Assert.That(second.ObservationIndex, Is.EqualTo(1));
        Assert.That(second.Boundary.AbsoluteDay, Is.EqualTo(2));
        Assert.That(second.Boundary.CompletedSequence, Is.EqualTo(2));
        Assert.That(second.PreviousDigest, Is.EqualTo(first.RawDigest));
        Assert.That(second.PreviousDigest, Is.EqualTo(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(firstBytes)).ToLowerInvariant()));
        Assert.That(second.TryGetStock("city-1", "wheat", out var after), Is.True);
        Assert.That(after!.Quantity, Is.EqualTo(25));
    }

    [Test]
    public void FixtureInterpretationIsDeterministicAndDoesNotTreatNullBindingAsAJoin()
    {
        var bytes = ObservationFixture.ReadBytes("observation-day-1.json");
        var left = _reader.Read(bytes);
        var right = _reader.Read(bytes);

        Assert.That(left.RawDigest, Is.EqualTo(right.RawDigest));
        Assert.That(WorldNavigator.List(left, AtlasEntityKind.City).Select(item => item.Key),
            Is.EqualTo(WorldNavigator.List(right, AtlasEntityKind.City).Select(item => item.Key)));
        Assert.That(WorldNavigator.ResolveAnchorHexId(left, new AtlasEntityKey(AtlasEntityKind.City, "city-1")), Is.EqualTo("hex-1"));
        Assert.That(WorldNavigator.ResolveAnchorHexId(left, new AtlasEntityKey(AtlasEntityKind.City, "city-2")), Is.Null);
    }

    [Test]
    public void ValidationFixtureKeepsUnplacedHexAccessibleWithoutMapCoordinates()
    {
        var observation = _reader.Read(ObservationFixture.ReadBytes("unplaced-hex-validation.json"));
        var listed = WorldNavigator.List(observation, AtlasEntityKind.Hex);
        var projection = new WorldAtlas.Observations.Geometry.HexProjection(observation.Geography.Hexes);
        var facts = WorldInspector.BuildFacts(observation, new AtlasEntityKey(AtlasEntityKind.Hex, "legacy-unplaced"));

        Assert.That(observation.UnplacedHexes.Select(hex => hex.HexId), Is.EqualTo(new[] { "legacy-unplaced" }));
        Assert.That(listed.Any(item => item.Key.SourceId == "legacy-unplaced" && item.Detail.Contains("unplaced", StringComparison.Ordinal)), Is.True);
        Assert.That(projection.PlacedHexes.Length, Is.EqualTo(2));
        Assert.That(projection.PlacedHexes.Any(hex => hex.HexId == "legacy-unplaced"), Is.False);
        Assert.That(facts.Single(fact => fact.Name == "Axial coordinates").Availability, Is.EqualTo(FactAvailability.Unknown));
    }

    [Test]
    public void RejectsUnknownVersionAndDuplicateJsonProperties()
    {
        var json = Encoding.UTF8.GetString(ObservationFixture.ReadBytes("observation-day-1.json"));
        var unknownVersion = json.Replace("\"version\":1", "\"version\":2", StringComparison.Ordinal);
        var duplicateProperty = json.Replace("\"schema\":\"simulation.lab.observation\"",
            "\"schema\":\"simulation.lab.observation\",\"schema\":\"simulation.lab.observation\"", StringComparison.Ordinal);

        Assert.That(() => _reader.Read(Encoding.UTF8.GetBytes(unknownVersion)),
            Throws.TypeOf<ObservationValidationException>().With.Property(nameof(ObservationValidationException.Code)).EqualTo("unknown-version"));
        Assert.That(() => _reader.Read(Encoding.UTF8.GetBytes(duplicateProperty)),
            Throws.TypeOf<ObservationValidationException>().With.Property(nameof(ObservationValidationException.Code)).EqualTo("duplicate-property"));
    }

    [Test]
    public void RejectsInvalidUtf8BomPartialCoordinatesDanglingReferencesAndOversizedStrings()
    {
        var bytes = ObservationFixture.ReadBytes("observation-day-1.json");
        var bomBytes = new byte[] { 0xef, 0xbb, 0xbf }.Concat(bytes).ToArray();
        var json = Encoding.UTF8.GetString(bytes);
        var partialCoordinates = json.Replace("\"q\":-1", "\"q\":null", StringComparison.Ordinal);
        var danglingLocation = json.Replace("\"anchorHexId\":\"hex-1\"", "\"anchorHexId\":\"missing-hex\"", StringComparison.Ordinal);
        var oversizedLabel = json.Replace("Sample Town", new string('x', ObservationFileReader.MaximumStringLength + 1), StringComparison.Ordinal);
        var malformedUtf8 = new byte[] { 0xc3, 0x28 };

        AssertCode(bomBytes, "utf8-bom");
        AssertCode(malformedUtf8, "invalid-utf8");
        AssertCode(Encoding.UTF8.GetBytes(partialCoordinates), "partial-hex-coordinate");
        AssertCode(Encoding.UTF8.GetBytes(danglingLocation), "dangling-location-anchor");
        AssertCode(Encoding.UTF8.GetBytes(oversizedLabel), "string-too-long");
    }

    [Test]
    public void RejectsWrongSourceProfileAndUnknownCapabilities()
    {
        var json = Encoding.UTF8.GetString(ObservationFixture.ReadBytes("observation-day-1.json"));
        AssertCode(Encoding.UTF8.GetBytes(json.Replace("atlas.demo/v1", "atlas.daily-map-stock/v1", StringComparison.Ordinal)), "unsupported-profile");
        AssertCode(Encoding.UTF8.GetBytes(json.Replace("\"sourceKind\":\"demo\"", "\"sourceKind\":\"core\"", StringComparison.Ordinal)), "invalid-source-kind");
        AssertCode(Encoding.UTF8.GetBytes(json.Replace("\"cities/v1\":", "\"cities/v1\":", StringComparison.Ordinal)
            .Replace("\"city-market-stock/v1\":", "\"unknown/v1\":{},\"city-market-stock/v1\":", StringComparison.Ordinal)), "unknown-property");
    }

    private void AssertCode(byte[] bytes, string code)
    {
        var exception = Assert.Throws<ObservationValidationException>(() => _reader.Read(bytes));
        Assert.That(exception!.Code, Is.EqualTo(code));
    }
}
