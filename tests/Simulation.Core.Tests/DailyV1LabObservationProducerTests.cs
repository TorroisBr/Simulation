using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1LabObservationProducerTests
{
    private static readonly string[] RootKeys =
    {
        "schema", "version", "profile", "sourceKind", "worldId", "runId",
        "observationIndex", "previousDigest", "boundary", "producer", "capabilities"
    };

    [Test]
    public void ObserveDailyPublishesTwoRealAdjacentCanonicalArtifacts()
    {
        string parent = CreateOwnedDirectory("producer-e2e");
        string output = Path.Combine(parent, "observations");
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;
        StringWriter stdout = new StringWriter();
        StringWriter stderr = new StringWriter();
        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            int exitCode = DailyV1ObservationCommand.Run(output);
            Assert.That(exitCode, Is.Zero, stderr.ToString());

            string firstPath = Path.Combine(output, "0000.atlas-observation.json");
            string secondPath = Path.Combine(output, "0001.atlas-observation.json");
            Assert.That(File.Exists(firstPath), Is.True);
            Assert.That(File.Exists(secondPath), Is.True);
            Assert.That(Directory.GetFileSystemEntries(output).Length, Is.EqualTo(2));
            byte[] firstBytes = File.ReadAllBytes(firstPath);
            byte[] secondBytes = File.ReadAllBytes(secondPath);
            AssertCanonicalJsonFile(firstBytes);
            AssertCanonicalJsonFile(secondBytes);

            using (JsonDocument firstDocument = JsonDocument.Parse(firstBytes,
                new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 24 }))
            using (JsonDocument secondDocument = JsonDocument.Parse(secondBytes,
                new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 24 }))
            {
                JsonElement first = firstDocument.RootElement;
                JsonElement second = secondDocument.RootElement;
                AssertPropertyNames(first, RootKeys);
                AssertPropertyNames(second, RootKeys);
                Assert.That(first.GetProperty("schema").GetString(),
                    Is.EqualTo("simulation.lab.observation"));
                Assert.That(first.GetProperty("version").GetInt32(), Is.EqualTo(1));
                Assert.That(first.GetProperty("profile").GetString(),
                    Is.EqualTo("atlas.daily-map-stock/v1"));
                Assert.That(first.GetProperty("sourceKind").GetString(), Is.EqualTo("core"));
                Assert.That(second.GetProperty("worldId").GetString(),
                    Is.EqualTo(first.GetProperty("worldId").GetString()));
                Assert.That(first.GetProperty("worldId").GetString(),
                    Does.StartWith("world:"));
                Assert.That(WorldId.TryParse(first.GetProperty("worldId").GetString(), out _), Is.True);

                string runId = first.GetProperty("runId").GetString();
                Assert.That(runId, Does.Match("^run:[0-9a-f]{32}$"));
                Assert.That(second.GetProperty("runId").GetString(), Is.EqualTo(runId));
                Assert.That(first.GetProperty("observationIndex").GetString(), Is.EqualTo("0"));
                Assert.That(second.GetProperty("observationIndex").GetString(), Is.EqualTo("1"));
                Assert.That(first.GetProperty("previousDigest").ValueKind, Is.EqualTo(JsonValueKind.Null));
                Assert.That(second.GetProperty("previousDigest").GetString(),
                    Is.EqualTo(Sha256(firstBytes)));

                JsonElement firstBoundary = first.GetProperty("boundary");
                JsonElement secondBoundary = second.GetProperty("boundary");
                AssertPropertyNames(firstBoundary, "absoluteDay", "completedSequence");
                AssertPropertyNames(secondBoundary, "absoluteDay", "completedSequence");
                Assert.That(firstBoundary.GetProperty("absoluteDay").GetString(), Is.EqualTo("1"));
                Assert.That(firstBoundary.GetProperty("completedSequence").GetString(), Is.EqualTo("1"));
                Assert.That(secondBoundary.GetProperty("absoluteDay").GetString(), Is.EqualTo("2"));
                Assert.That(secondBoundary.GetProperty("completedSequence").GetString(), Is.EqualTo("2"));

                JsonElement firstProducer = first.GetProperty("producer");
                JsonElement secondProducer = second.GetProperty("producer");
                AssertPropertyNames(firstProducer, "id", "version", "build", "scenarioId",
                    "scenarioRevision", "compositionProfile", "effectiveInputFingerprint");
                AssertPropertyNames(secondProducer, "id", "version", "build", "scenarioId",
                    "scenarioRevision", "compositionProfile", "effectiveInputFingerprint");
                Assert.That(firstProducer.GetProperty("id").GetString(), Is.EqualTo("simulation.lab"));
                Assert.That(firstProducer.GetProperty("version").GetInt32(), Is.EqualTo(1));
                Assert.That(firstProducer.GetProperty("scenarioId").GetString(),
                    Is.EqualTo("selected-daily-stock"));
                Assert.That(firstProducer.GetProperty("scenarioRevision").GetString(), Is.EqualTo("1"));
                Assert.That(firstProducer.GetProperty("compositionProfile").GetString(),
                    Is.EqualTo("UnityBootstrap-Daily-v1"));
                Assert.That(firstProducer.GetProperty("build").GetString(),
                    Does.Match("^[0-9a-f]{64}$"));
                Assert.That(secondProducer.GetProperty("build").GetString(),
                    Is.EqualTo(firstProducer.GetProperty("build").GetString()));
                Assert.That(secondProducer.GetProperty("effectiveInputFingerprint").GetString(),
                    Is.EqualTo(firstProducer.GetProperty("effectiveInputFingerprint").GetString()));

                JsonElement firstCapabilities = first.GetProperty("capabilities");
                JsonElement secondCapabilities = second.GetProperty("capabilities");
                AssertPropertyNames(firstCapabilities,
                    "geography/v1", "cities/v1", "city-market-stock/v1");
                AssertPropertyNames(secondCapabilities,
                    "geography/v1", "cities/v1", "city-market-stock/v1");
                AssertCapabilityShape(firstCapabilities);
                AssertCapabilityShape(secondCapabilities);

                JsonElement firstCities = firstCapabilities.GetProperty("cities/v1")
                    .GetProperty("rows");
                JsonElement secondCities = secondCapabilities.GetProperty("cities/v1")
                    .GetProperty("rows");
                Assert.That(firstCities.GetArrayLength(), Is.EqualTo(2));
                Assert.That(secondCities.GetArrayLength(), Is.EqualTo(2));
                foreach (JsonElement city in firstCities.EnumerateArray())
                    Assert.That(city.GetProperty("p8LocationId").ValueKind,
                        Is.EqualTo(JsonValueKind.Null));
                foreach (JsonElement city in secondCities.EnumerateArray())
                    Assert.That(city.GetProperty("p8LocationId").ValueKind,
                        Is.EqualTo(JsonValueKind.Null));

                JsonElement firstScope = firstCapabilities.GetProperty("city-market-stock/v1")
                    .GetProperty("scope");
                JsonElement secondScope = secondCapabilities.GetProperty("city-market-stock/v1")
                    .GetProperty("scope");
                AssertPropertyNames(firstScope, "cityId", "itemDefinitionId");
                AssertPropertyNames(secondScope, "cityId", "itemDefinitionId");
                Assert.That(secondScope.GetProperty("cityId").GetString(),
                    Is.EqualTo(firstScope.GetProperty("cityId").GetString()));
                Assert.That(firstScope.GetProperty("itemDefinitionId").GetString(),
                    Is.EqualTo("TRIGO"));
                Assert.That(secondScope.GetProperty("itemDefinitionId").GetString(),
                    Is.EqualTo("TRIGO"));

                JsonElement firstStock = firstCapabilities.GetProperty("city-market-stock/v1")
                    .GetProperty("rows");
                JsonElement secondStock = secondCapabilities.GetProperty("city-market-stock/v1")
                    .GetProperty("rows");
                Assert.That(firstStock.GetArrayLength(), Is.EqualTo(1));
                Assert.That(secondStock.GetArrayLength(), Is.EqualTo(1));
                JsonElement firstRow = firstStock[0];
                JsonElement secondRow = secondStock[0];
                AssertPropertyNames(firstRow, "cityId", "itemDefinitionId", "quantity");
                AssertPropertyNames(secondRow, "cityId", "itemDefinitionId", "quantity");
                Assert.That(firstRow.GetProperty("cityId").GetString(),
                    Is.EqualTo(firstScope.GetProperty("cityId").GetString()));
                Assert.That(secondRow.GetProperty("cityId").GetString(),
                    Is.EqualTo(secondScope.GetProperty("cityId").GetString()));
                Assert.That(firstRow.GetProperty("itemDefinitionId").GetString(), Is.EqualTo("TRIGO"));
                Assert.That(secondRow.GetProperty("itemDefinitionId").GetString(), Is.EqualTo("TRIGO"));
                int firstQuantity = firstRow.GetProperty("quantity").GetInt32();
                int secondQuantity = secondRow.GetProperty("quantity").GetInt32();
                Assert.That(firstQuantity, Is.GreaterThanOrEqualTo(0));
                Assert.That(secondQuantity, Is.GreaterThanOrEqualTo(0));
                Assert.That((long)secondQuantity - (long)firstQuantity, Is.Not.EqualTo(0L));
            }

            string[] reports = stdout.ToString().Split(new[] { Environment.NewLine },
                StringSplitOptions.RemoveEmptyEntries);
            Assert.That(reports.Length, Is.EqualTo(2));
            Assert.That(reports[0], Does.Match("^file=0000\\.atlas-observation\\.json sha256=[0-9a-f]{64}$"));
            Assert.That(reports[1], Does.Match("^file=0001\\.atlas-observation\\.json sha256=[0-9a-f]{64}$"));

            string firstHash = Sha256(firstBytes);
            string secondHash = Sha256(secondBytes);
            stdout.GetStringBuilder().Clear();
            stderr.GetStringBuilder().Clear();
            Assert.That(DailyV1ObservationCommand.Run(output), Is.EqualTo(1));
            Assert.That(File.ReadAllBytes(firstPath), Is.EqualTo(firstBytes));
            Assert.That(File.ReadAllBytes(secondPath), Is.EqualTo(secondBytes));
            Assert.That(Sha256(File.ReadAllBytes(firstPath)), Is.EqualTo(firstHash));
            Assert.That(Sha256(File.ReadAllBytes(secondPath)), Is.EqualTo(secondHash));
            Assert.That(Directory.GetFileSystemEntries(output).Length, Is.EqualTo(2));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            DeleteOwnedDirectory(parent);
        }
    }

    [Test]
    public void EncoderPreservesAbsenceVersusZeroAndEscapesWithoutNormalizingText()
    {
        DailyV1AtlasObservation zero = SyntheticObservation(
            new List<DailyV1AtlasMarketStockObservation>
            {
                new DailyV1AtlasMarketStockObservation("city-1", "TRIGO", 0)
            });
        DailyV1ObservationEnvelope zeroEnvelope = Envelope(zero, 0, null);
        Assert.That(DailyV1ObservationV1Encoder.TryEncode(zeroEnvelope,
            out byte[] zeroBytes, out string diagnostic), Is.True, diagnostic);

        DailyV1AtlasObservation absent = SyntheticObservation(
            new List<DailyV1AtlasMarketStockObservation>());
        Assert.That(DailyV1ObservationV1Encoder.TryEncode(Envelope(absent, 0, null),
            out byte[] absentBytes, out diagnostic), Is.True, diagnostic);
        Assert.That(zeroBytes, Is.Not.EqualTo(absentBytes));
        using (JsonDocument zeroDocument = JsonDocument.Parse(zeroBytes))
        using (JsonDocument absentDocument = JsonDocument.Parse(absentBytes))
        {
            JsonElement zeroRows = zeroDocument.RootElement.GetProperty("capabilities")
                .GetProperty("city-market-stock/v1").GetProperty("rows");
            JsonElement absentRows = absentDocument.RootElement.GetProperty("capabilities")
                .GetProperty("city-market-stock/v1").GetProperty("rows");
            Assert.That(zeroRows.GetArrayLength(), Is.EqualTo(1));
            Assert.That(zeroRows[0].GetProperty("quantity").GetInt32(), Is.Zero);
            Assert.That(absentRows.GetArrayLength(), Is.Zero);
        }

        DailyV1AtlasObservation repeated = SyntheticObservation(
            new List<DailyV1AtlasMarketStockObservation>
            {
                new DailyV1AtlasMarketStockObservation("city-1", "TRIGO", 0)
            }, "Campo \"Verde\" \\ 🗺️");
        Assert.That(DailyV1ObservationV1Encoder.TryEncode(Envelope(repeated, 0, null),
            out byte[] encodedOnce, out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1ObservationV1Encoder.TryEncode(Envelope(repeated, 0, null),
            out byte[] encodedTwice, out diagnostic), Is.True, diagnostic);
        Assert.That(encodedTwice, Is.EqualTo(encodedOnce));
        Assert.That(Encoding.UTF8.GetString(encodedOnce), Does.Contain("Campo \\\"Verde\\\" \\\\ 🗺️"));
        using (JsonDocument decoded = JsonDocument.Parse(encodedOnce))
            Assert.That(decoded.RootElement.GetProperty("capabilities").GetProperty("cities/v1")
                .GetProperty("rows")[0].GetProperty("label").GetString(),
                Is.EqualTo("Campo \"Verde\" \\ 🗺️"));

        DailyV1AtlasObservation invalidLabel = SyntheticObservation(
            new List<DailyV1AtlasMarketStockObservation>(), "bad\nlabel");
        Assert.That(DailyV1ObservationV1Encoder.TryEncode(Envelope(invalidLabel, 0, null),
            out _, out diagnostic), Is.False);
        Assert.That(diagnostic, Is.EqualTo("SOURCE_FACTS_INVALID"));
    }

    [Test]
    public void EncoderRejectsDanglingRelationshipsDuplicateIdsAndForeignStockRows()
    {
        DailyV1ObservationEnvelope valid = Envelope(SyntheticObservation(
            new List<DailyV1AtlasMarketStockObservation>()), 0, null);
        Assert.That(DailyV1ObservationV1Encoder.TryEncode(valid,
            out _, out string diagnostic), Is.True, diagnostic);

        DailyV1AtlasObservation duplicateHex = SyntheticObservation(
            new List<DailyV1AtlasMarketStockObservation>(),
            hexes: new List<DailyV1AtlasHexObservation>
            {
                new DailyV1AtlasHexObservation("hex-1", 0, 0, "terrain-1", "sample-world-v1"),
                new DailyV1AtlasHexObservation("hex-1", 1, 0, "terrain-1", "sample-world-v1")
            });
        Assert.That(DailyV1ObservationV1Encoder.TryEncode(Envelope(duplicateHex, 0, null),
            out _, out diagnostic), Is.False);

        DailyV1AtlasObservation danglingLocation = SyntheticObservation(
            new List<DailyV1AtlasMarketStockObservation>(),
            locations: new List<DailyV1AtlasLocationObservation>
            {
                new DailyV1AtlasLocationObservation("location-1", "hex-missing")
            });
        Assert.That(DailyV1ObservationV1Encoder.TryEncode(Envelope(danglingLocation, 0, null),
            out _, out diagnostic), Is.False);

        DailyV1AtlasObservation danglingCity = SyntheticObservation(
            new List<DailyV1AtlasMarketStockObservation>(),
            cities: new List<DailyV1AtlasCityObservation>
            {
                new DailyV1AtlasCityObservation("city-1", "Campo Verde", "location-missing")
            });
        Assert.That(DailyV1ObservationV1Encoder.TryEncode(Envelope(danglingCity, 0, null),
            out _, out diagnostic), Is.False);

        DailyV1AtlasObservation foreignRow = SyntheticObservation(
            new List<DailyV1AtlasMarketStockObservation>
            {
                new DailyV1AtlasMarketStockObservation("city-foreign", "TRIGO", 1)
            });
        Assert.That(DailyV1ObservationV1Encoder.TryEncode(Envelope(foreignRow, 0, null),
            out _, out diagnostic), Is.False);

        DailyV1ObservationEnvelope wrongPredecessor = Envelope(
            SyntheticObservation(new List<DailyV1AtlasMarketStockObservation>()), 1,
            new string('A', 64));
        Assert.That(DailyV1ObservationV1Encoder.TryEncode(wrongPredecessor,
            out _, out diagnostic), Is.False);
        Assert.That(diagnostic, Is.EqualTo("PROVENANCE_INVALID"));
    }

    [Test]
    public void CommandAcceptsOnlyTheSingleObserveDailyOutputOption()
    {
        Assert.That(DailyV1ObservationCommand.TryParseArguments(
            new[] { "observe-daily", "--out", "observations" }, out string output), Is.True);
        Assert.That(output, Is.EqualTo("observations"));
        Assert.That(DailyV1ObservationCommand.TryParseArguments(
            new[] { "observe-daily", "--out", "observations", "--seed", "5" }, out _), Is.False);
        Assert.That(DailyV1ObservationCommand.TryParseArguments(
            new[] { "observe-daily", "--output", "observations" }, out _), Is.False);
        Assert.That(DailyV1ObservationCommand.TryParseArguments(
            Array.Empty<string>(), out _), Is.False);
    }

    [Test]
    public void BuildIdentityUsesSpecifiedLengthFramingAndExplicitAssemblyAnchors()
    {
        DailyV1HostEnvironmentIdentity identity =
            DailyV1DotNetHostEnvironmentIdentityFactory.Capture(typeof(Program));
        Assert.That(DailyV1ObservationBuildIdentity.TryCompute(identity,
            out string actual), Is.True);
        Assert.That(actual, Does.Match("^[0-9a-f]{64}$"));
        Assert.That(actual, Is.EqualTo(IndependentlyComputeBuildDigest(identity)));
        Assert.That(DailyV1ObservationBuildIdentity.TryCompute(identity,
            out string repeated), Is.True);
        Assert.That(repeated, Is.EqualTo(actual));
    }

    [TestCase(FakeFailure.Create)]
    [TestCase(FakeFailure.Write)]
    [TestCase(FakeFailure.Flush)]
    [TestCase(FakeFailure.ReadBack)]
    [TestCase(FakeFailure.Rename)]
    public void PublisherFailuresRemoveOnlyTemporaryFileAndDoNotPublishFinal(
        FakeFailure failure)
    {
        string parent = CreateOwnedDirectory("publisher-failure");
        string output = Path.Combine(parent, "out");
        Directory.CreateDirectory(output);
        try
        {
            FakeObservationFileOperations operations =
                new FakeObservationFileOperations { Failure = failure };
            DailyV1ObservationArtifactPublisher publisher =
                new DailyV1ObservationArtifactPublisher(operations);
            Assert.That(publisher.TryPublish(output, 0, Encoding.UTF8.GetBytes("{}\n"),
                out _, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Is.Not.EqualTo("NONE"));
            Assert.That(operations.Files, Is.Empty);
        }
        finally
        {
            DeleteOwnedDirectory(parent);
        }
    }

    [Test]
    public void SecondPublishFailurePreservesFirstCommittedArtifact()
    {
        string parent = CreateOwnedDirectory("publisher-second-failure");
        string output = Path.Combine(parent, "out");
        Directory.CreateDirectory(output);
        try
        {
            FakeObservationFileOperations operations = new FakeObservationFileOperations();
            DailyV1ObservationArtifactPublisher publisher =
                new DailyV1ObservationArtifactPublisher(operations);
            byte[] first = Encoding.UTF8.GetBytes("{\"index\":0}\n");
            Assert.That(publisher.TryPublish(output, 0, first,
                out string name, out string digest, out string diagnostic), Is.True, diagnostic);
            Assert.That(name, Is.EqualTo("0000.atlas-observation.json"));
            Assert.That(digest, Is.EqualTo(Sha256(first)));
            operations.Failure = FakeFailure.Write;
            Assert.That(publisher.TryPublish(output, 1, Encoding.UTF8.GetBytes("{\"index\":1}\n"),
                out _, out _, out diagnostic), Is.False);
            Assert.That(operations.Files.Count, Is.EqualTo(1));
            Assert.That(operations.Files.Values.Single(), Is.EqualTo(first));
        }
        finally
        {
            DeleteOwnedDirectory(parent);
        }
    }

    [Test]
    public void CommandDoesNotRetractCommittedFirstArtifactWhenReportingFails()
    {
        string parent = CreateOwnedDirectory("postcommit-report-failure");
        string output = Path.Combine(parent, "observations");
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;
        try
        {
            Console.SetOut(new ThrowOnWriteLineTextWriter());
            Console.SetError(new StringWriter());
            Assert.That(DailyV1ObservationCommand.Run(output), Is.EqualTo(1));
            Assert.That(Directory.GetFiles(output, "*.atlas-observation.json").Length, Is.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(output, "0000.atlas-observation.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(output, "0001.atlas-observation.json")), Is.False);
            AssertCanonicalJsonFile(File.ReadAllBytes(
                Path.Combine(output, "0000.atlas-observation.json")));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            DeleteOwnedDirectory(parent);
        }
    }

    private static void AssertCapabilityShape(JsonElement capabilities)
    {
        JsonElement geography = capabilities.GetProperty("geography/v1");
        AssertPropertyNames(geography, "scope", "coverage", "sourceStamp", "hexes", "locations");
        Assert.That(geography.GetProperty("scope").GetString(), Is.EqualTo("registered-geography"));
        Assert.That(geography.GetProperty("coverage").GetString(), Is.EqualTo("complete"));
        JsonElement geoStamp = geography.GetProperty("sourceStamp");
        AssertPropertyNames(geoStamp, "spatialRevision");
        AssertCanonicalNonnegative(geoStamp.GetProperty("spatialRevision").GetString());
        JsonElement hexes = geography.GetProperty("hexes");
        JsonElement locations = geography.GetProperty("locations");
        Assert.That(hexes.GetArrayLength(), Is.GreaterThan(0));
        Assert.That(locations.GetArrayLength(), Is.GreaterThan(0));
        HashSet<string> hexIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement hex in hexes.EnumerateArray())
        {
            AssertPropertyNames(hex, "hexId", "q", "r", "terrainId", "terrainRevision");
            Assert.That(hexIds.Add(hex.GetProperty("hexId").GetString()), Is.True);
            Assert.That(hex.GetProperty("q").TryGetInt32(out _), Is.True);
            Assert.That(hex.GetProperty("r").TryGetInt32(out _), Is.True);
            Assert.That(hex.GetProperty("terrainId").GetString(), Is.Not.Empty);
            Assert.That(hex.GetProperty("terrainRevision").GetString(), Is.EqualTo("sample-world-v1"));
        }
        HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement location in locations.EnumerateArray())
        {
            AssertPropertyNames(location, "locationId", "anchorHexId");
            Assert.That(locationIds.Add(location.GetProperty("locationId").GetString()), Is.True);
            Assert.That(hexIds.Contains(location.GetProperty("anchorHexId").GetString()), Is.True);
        }

        JsonElement cities = capabilities.GetProperty("cities/v1");
        AssertPropertyNames(cities, "scope", "coverage", "sourceStamp", "bindingCoverage", "rows");
        Assert.That(cities.GetProperty("scope").GetString(), Is.EqualTo("world-city-roster"));
        Assert.That(cities.GetProperty("coverage").GetString(), Is.EqualTo("complete"));
        Assert.That(cities.GetProperty("bindingCoverage").GetString(), Is.EqualTo("complete"));
        JsonElement cityStamp = cities.GetProperty("sourceStamp");
        AssertPropertyNames(cityStamp, "identityIndexRevision", "anchorRevision");
        AssertCanonicalNonnegative(cityStamp.GetProperty("identityIndexRevision").GetString());
        AssertCanonicalNonnegative(cityStamp.GetProperty("anchorRevision").GetString());
        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement city in cities.GetProperty("rows").EnumerateArray())
        {
            AssertPropertyNames(city, "cityId", "label", "p8LocationId");
            Assert.That(cityIds.Add(city.GetProperty("cityId").GetString()), Is.True);
            Assert.That(city.GetProperty("p8LocationId").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }

        JsonElement market = capabilities.GetProperty("city-market-stock/v1");
        AssertPropertyNames(market, "scope", "coverage", "sourceStamp", "rows");
        Assert.That(market.GetProperty("coverage").GetString(), Is.EqualTo("complete"));
        JsonElement marketScope = market.GetProperty("scope");
        AssertPropertyNames(marketScope, "cityId", "itemDefinitionId");
        Assert.That(cityIds.Contains(marketScope.GetProperty("cityId").GetString()), Is.True);
        Assert.That(marketScope.GetProperty("itemDefinitionId").GetString(), Is.EqualTo("TRIGO"));
        JsonElement marketStamp = market.GetProperty("sourceStamp");
        AssertPropertyNames(marketStamp, "marketRevision");
        AssertCanonicalNonnegative(marketStamp.GetProperty("marketRevision").GetString());
    }

    private static void AssertCanonicalJsonFile(byte[] bytes)
    {
        Assert.That(bytes.Length, Is.LessThanOrEqualTo(DailyV1ObservationV1Encoder.MaximumArtifactBytes));
        Assert.That(bytes.Length, Is.GreaterThan(2));
        Assert.That(bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf, Is.False,
            "Canonical observation files are UTF-8 without a BOM.");
        Assert.That(bytes[bytes.Length - 1], Is.EqualTo((byte)'\n'));
        Assert.That(bytes[bytes.Length - 2], Is.Not.EqualTo((byte)'\n'));
    }

    private static void AssertPropertyNames(JsonElement element, params string[] expected)
    {
        string[] actual = element.EnumerateObject().Select(property => property.Name).ToArray();
        CollectionAssert.AreEqual(expected, actual);
    }

    private static void AssertCanonicalNonnegative(string value)
    {
        Assert.That(value, Is.Not.Null);
        Assert.That(long.TryParse(value, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out long parsed), Is.True);
        Assert.That(parsed, Is.GreaterThanOrEqualTo(0));
        Assert.That(parsed.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Is.EqualTo(value));
    }

    private static DailyV1ObservationEnvelope Envelope(
        DailyV1AtlasObservation observation, long index, string previousDigest) =>
        new DailyV1ObservationEnvelope(observation, "run:0123456789abcdef0123456789abcdef",
            index, previousDigest, new string('a', 64));

    private static DailyV1AtlasObservation SyntheticObservation(
        IList<DailyV1AtlasMarketStockObservation> stockRows,
        string label = "Campo Verde",
        IList<DailyV1AtlasHexObservation> hexes = null,
        IList<DailyV1AtlasLocationObservation> locations = null,
        IList<DailyV1AtlasCityObservation> cities = null)
    {
        return new DailyV1AtlasObservation(
            "world:0123456789abcdef0123456789abcdef",
            1L, 1L, new string('b', 64), 1L,
            hexes ?? new List<DailyV1AtlasHexObservation>
            {
                new DailyV1AtlasHexObservation("hex-1", 0, 0, "terrain-1", "sample-world-v1")
            },
            locations ?? new List<DailyV1AtlasLocationObservation>
            {
                new DailyV1AtlasLocationObservation("location-1", "hex-1")
            },
            2L, 0L,
            cities ?? new List<DailyV1AtlasCityObservation>
            {
                new DailyV1AtlasCityObservation("city-1", label, null)
            },
            "city-1", "TRIGO", 3L, stockRows);
    }

    private static string IndependentlyComputeBuildDigest(
        DailyV1HostEnvironmentIdentity identity)
    {
        List<P12AAssemblyIdentityV2> assemblies = identity.Assemblies
            .OrderBy(value => value.SimpleName, StringComparer.Ordinal)
            .ThenBy(value => value.Version, StringComparer.Ordinal)
            .ThenBy(value => value.Mvid, StringComparer.Ordinal)
            .ToList();
        List<string> fields = new List<string>
        {
            "lab-observation-build/v1",
            identity.HostKind.ToString(System.Globalization.CultureInfo.InvariantCulture),
            identity.RuntimeFrameworkDescription,
            identity.RuntimeVersion,
            identity.OperatingSystemDescription,
            identity.RuntimeIdentifier,
            identity.ProcessArchitecture,
            identity.TargetFramework,
            identity.NumericProfileId,
            identity.NumericProfileVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            assemblies.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        foreach (P12AAssemblyIdentityV2 assembly in assemblies)
        {
            fields.Add(assembly.SimpleName);
            fields.Add(assembly.Version);
            fields.Add(assembly.Mvid.ToLowerInvariant());
        }

        using (MemoryStream stream = new MemoryStream())
        {
            foreach (string field in fields)
            {
                byte[] encoded = new UTF8Encoding(false, true).GetBytes(field);
                uint length = (uint)encoded.Length;
                stream.WriteByte((byte)(length >> 24));
                stream.WriteByte((byte)(length >> 16));
                stream.WriteByte((byte)(length >> 8));
                stream.WriteByte((byte)length);
                stream.Write(encoded, 0, encoded.Length);
            }
            using (SHA256 sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream.ToArray())
                    .Select(value => value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
        }
    }

    private static string CreateOwnedDirectory(string prefix)
    {
        string path = Path.Combine(Path.GetTempPath(),
            "simulation-lab-producer-tests-" + prefix + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteOwnedDirectory(string path)
    {
        if (Directory.Exists(path)
            && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0)
            Directory.Delete(path, true);
    }

    private static string Sha256(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return string.Concat(sha.ComputeHash(bytes)
                .Select(value => value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
    }

    private sealed class ThrowOnWriteLineTextWriter : StringWriter
    {
        public override void WriteLine(string value)
        {
            throw new IOException("Injected output failure after commit.");
        }
    }

    public enum FakeFailure
    {
        None,
        Create,
        Write,
        Flush,
        ReadBack,
        Rename
    }

    private sealed class FakeObservationFileOperations : IDailyV1ObservationFileOperations
    {
        internal readonly Dictionary<string, byte[]> Files =
            new Dictionary<string, byte[]>(StringComparer.Ordinal);
        internal FakeFailure Failure { get; set; }

        public IDailyV1ObservationTempFile CreateNew(string path)
        {
            if (Failure == FakeFailure.Create)
                throw new IOException("Injected create failure.");
            if (Files.ContainsKey(path))
                throw new IOException("Create-new collision.");
            Files.Add(path, Array.Empty<byte>());
            return new FakeTempFile(this, path);
        }

        public byte[] ReadAllBytes(string path)
        {
            if (Failure == FakeFailure.ReadBack)
                throw new IOException("Injected readback failure.");
            if (!Files.TryGetValue(path, out byte[] bytes))
                throw new IOException("Missing temp artifact.");
            return bytes.ToArray();
        }

        public void MoveNoOverwrite(string source, string destination)
        {
            if (Failure == FakeFailure.Rename || Files.ContainsKey(destination))
                throw new IOException("Injected or colliding rename failure.");
            Files.Add(destination, Files[source]);
            Files.Remove(source);
        }

        public void DeleteFileIfExists(string path)
        {
            Files.Remove(path);
        }

        private sealed class FakeTempFile : IDailyV1ObservationTempFile
        {
            private readonly FakeObservationFileOperations owner;
            private readonly string path;

            internal FakeTempFile(FakeObservationFileOperations owner, string path)
            {
                this.owner = owner;
                this.path = path;
            }

            public void Write(byte[] bytes)
            {
                if (owner.Failure == FakeFailure.Write)
                    throw new IOException("Injected write failure.");
                owner.Files[path] = bytes.ToArray();
            }

            public void FlushToDisk()
            {
                if (owner.Failure == FakeFailure.Flush)
                    throw new IOException("Injected flush failure.");
            }

            public void Dispose()
            {
            }
        }
    }
}
