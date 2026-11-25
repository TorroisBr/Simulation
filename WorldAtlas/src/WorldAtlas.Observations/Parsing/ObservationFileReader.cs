using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WorldAtlas.Observations.Models;

namespace WorldAtlas.Observations.Parsing;

/// <summary>Reads the closed, demo-only ATLAS-A observation v1 contract.</summary>
public sealed class ObservationFileReader
{
    public const int MaximumFileBytes = 4 * 1024 * 1024;
    public const int MaximumDepth = 24;
    public const int MaximumStringLength = 512;
    public const int MaximumHexes = 4096;
    public const int MaximumLocations = 4096;
    public const int MaximumCities = 1024;

    private const string ExpectedSchema = "simulation.lab.observation";
    private const string ExpectedProfile = "atlas.demo/v1";
    private const string ExpectedInputFingerprint = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static readonly byte[] Utf8Bom = [0xef, 0xbb, 0xbf];
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public ObservationDocument Read(ReadOnlySpan<byte> utf8Bytes, string? sourcePath = null)
    {
        if (utf8Bytes.Length == 0)
        {
            throw Invalid("empty-file", "The observation file is empty.");
        }

        if (utf8Bytes.Length > MaximumFileBytes)
        {
            throw Invalid("file-too-large", $"The observation file exceeds the {MaximumFileBytes:N0} byte limit.");
        }

        if (utf8Bytes.StartsWith(Utf8Bom))
        {
            throw Invalid("utf8-bom", "Observation JSON must be UTF-8 without a byte order mark.");
        }

        string json;
        try
        {
            json = StrictUtf8.GetString(utf8Bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw Invalid("invalid-utf8", "The observation file is not valid UTF-8.", exception);
        }

        using var document = ParseJson(json);
        var root = document.RootElement;
        RequireObject(root, "$");
        RequireExactProperties(root, "$", "schema", "version", "profile", "sourceKind", "worldId", "runId",
            "observationIndex", "previousDigest", "boundary", "producer", "capabilities");

        var schema = RequiredString(root, "schema", "$.schema");
        if (!StringComparer.Ordinal.Equals(schema, ExpectedSchema))
        {
            throw Invalid("unknown-schema", $"Unsupported schema '{schema}'.");
        }

        var version = RequiredInt32(root, "version", "$.version");
        if (version != 1)
        {
            throw Invalid("unknown-version", $"Unsupported observation version {version}.");
        }

        var profile = RequiredString(root, "profile", "$.profile");
        if (!StringComparer.Ordinal.Equals(profile, ExpectedProfile))
        {
            throw Invalid("unsupported-profile", $"ATLAS-A accepts only the explicit Demo profile '{ExpectedProfile}'.");
        }

        var sourceKindText = RequiredString(root, "sourceKind", "$.sourceKind");
        if (!StringComparer.Ordinal.Equals(sourceKindText, "demo"))
        {
            throw Invalid("invalid-source-kind", "The atlas.demo/v1 profile must declare sourceKind 'demo'.");
        }

        var worldId = RequiredIdentity(root, "worldId", "$.worldId");
        var runId = RequiredIdentity(root, "runId", "$.runId");
        if (!StringComparer.Ordinal.Equals(worldId, "demo:world") || !StringComparer.Ordinal.Equals(runId, "demo:run"))
        {
            throw Invalid("foreign-demo-identity", "The frozen ATLAS-A fixture identity must be demo:world / demo:run.");
        }

        var index = RequiredCanonicalNonNegativeInt64(root, "observationIndex", "$.observationIndex");
        var previousDigest = NullableString(root, "previousDigest", "$.previousDigest", allowEmpty: false);
        if (index == 0 && previousDigest is not null)
        {
            throw Invalid("first-observation-predecessor", "Observation index 0 must not declare a predecessor digest.");
        }

        if (index > 0 && !IsLowerSha256(previousDigest))
        {
            throw Invalid("invalid-predecessor-digest", "Observations after index 0 require a lower-case SHA-256 predecessor digest.");
        }

        var boundaryElement = Required(root, "boundary", "$.boundary");
        RequireExactProperties(boundaryElement, "$.boundary", "absoluteDay", "completedSequence");
        var boundary = new ObservationBoundary(
            RequiredCanonicalNonNegativeInt64(boundaryElement, "absoluteDay", "$.boundary.absoluteDay"),
            RequiredCanonicalNonNegativeInt64(boundaryElement, "completedSequence", "$.boundary.completedSequence"));
        if (boundary.CompletedSequence <= 0)
        {
            throw Invalid("invalid-completed-sequence", "A completed observation sequence must be positive.");
        }

        var producer = ReadProducer(Required(root, "producer", "$.producer"));
        var capabilities = Required(root, "capabilities", "$.capabilities");
        RequireExactProperties(capabilities, "$.capabilities", "geography/v1", "cities/v1", "city-market-stock/v1");

        var geography = ReadGeography(Required(capabilities, "geography/v1", "$.capabilities.geography/v1"));
        var cities = ReadCities(Required(capabilities, "cities/v1", "$.capabilities.cities/v1"));
        var stock = ReadStock(Required(capabilities, "city-market-stock/v1", "$.capabilities.city-market-stock/v1"));
        ValidateReferences(geography, cities, stock);

        var digest = Convert.ToHexString(SHA256.HashData(utf8Bytes)).ToLowerInvariant();
        return new ObservationDocument(
            schema,
            version,
            profile,
            ObservationSourceKind.Demo,
            worldId,
            runId,
            index,
            previousDigest,
            boundary,
            producer,
            geography,
            cities,
            stock,
            digest,
            sourcePath is null ? null : Path.GetFullPath(sourcePath));
    }

    public async Task<ObservationDocument> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 16 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var buffer = new MemoryStream(capacity: 64 * 1024);
        var chunk = new byte[16 * 1024];
        while (true)
        {
            var bytesRead = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            if (buffer.Length + bytesRead > MaximumFileBytes)
            {
                throw Invalid("file-too-large", $"The observation file exceeds the {MaximumFileBytes:N0} byte limit.");
            }

            buffer.Write(chunk, 0, bytesRead);
        }

        return Read(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length)), path);
    }

    private static JsonDocument ParseJson(string json)
    {
        try
        {
            return JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = MaximumDepth
            });
        }
        catch (JsonException exception)
        {
            throw Invalid("malformed-json", "The observation file is not valid bounded JSON.", exception);
        }
    }

    private static ProducerMetadata ReadProducer(JsonElement element)
    {
        RequireExactProperties(element, "$.producer", "id", "version", "build", "scenarioId", "scenarioRevision",
            "compositionProfile", "effectiveInputFingerprint");
        var metadata = new ProducerMetadata(
            RequiredIdentity(element, "id", "$.producer.id"),
            RequiredInt32(element, "version", "$.producer.version"),
            RequiredIdentity(element, "build", "$.producer.build"),
            RequiredIdentity(element, "scenarioId", "$.producer.scenarioId"),
            RequiredIdentity(element, "scenarioRevision", "$.producer.scenarioRevision"),
            RequiredIdentity(element, "compositionProfile", "$.producer.compositionProfile"),
            RequiredIdentity(element, "effectiveInputFingerprint", "$.producer.effectiveInputFingerprint"));

        if (metadata.Id != "atlas.demo" || metadata.Version != 1 || metadata.Build != "fixture-v1" ||
            metadata.ScenarioId != "demo-map" || metadata.ScenarioRevision != "1" ||
            metadata.CompositionProfile != "demo" || metadata.EffectiveInputFingerprint != ExpectedInputFingerprint)
        {
            throw Invalid("unsupported-demo-producer", "The producer identity does not match the frozen ATLAS-A Demo fixture.");
        }

        return metadata;
    }

    private static GeographyCapability ReadGeography(JsonElement element)
    {
        const string path = "$.capabilities.geography/v1";
        RequireExactProperties(element, path, "scope", "coverage", "sourceStamp", "hexes", "locations");
        var scope = RequiredIdentity(element, "scope", path + ".scope");
        var coverage = RequiredCoverage(element, "coverage", path + ".coverage");
        var sourceStamp = NullableString(element, "sourceStamp", path + ".sourceStamp", allowEmpty: false);
        if (scope != "registered-geography" || coverage != CapabilityCoverage.Complete || sourceStamp is not null)
        {
            throw Invalid("invalid-geography-scope", "Demo geography must be complete registered-geography with a null source stamp.");
        }

        var hexElements = RequiredArray(element, "hexes", path + ".hexes", MaximumHexes);
        var hexes = ImmutableArray.CreateBuilder<HexFact>(hexElements.Count);
        var hexIds = new HashSet<string>(StringComparer.Ordinal);
        var coordinates = new HashSet<(int Q, int R)>();
        for (var index = 0; index < hexElements.Count; index++)
        {
            var itemPath = $"{path}.hexes[{index}]";
            var item = hexElements[index];
            RequireExactProperties(item, itemPath, "hexId", "q", "r", "terrainId", "terrainRevision");
            var hexId = RequiredIdentity(item, "hexId", itemPath + ".hexId");
            if (!hexIds.Add(hexId))
            {
                throw Invalid("duplicate-hex-id", $"Duplicate Hex identity '{hexId}'.");
            }

            var q = NullableInt32(item, "q", itemPath + ".q");
            var r = NullableInt32(item, "r", itemPath + ".r");
            var terrainId = NullableString(item, "terrainId", itemPath + ".terrainId", allowEmpty: false);
            var terrainRevision = NullableString(item, "terrainRevision", itemPath + ".terrainRevision", allowEmpty: false);
            var allNull = q is null && r is null && terrainId is null && terrainRevision is null;
            var allKnown = q is not null && r is not null && terrainId is not null && terrainRevision is not null;
            if (!allNull && !allKnown)
            {
                throw Invalid("partial-hex-coordinate", $"{itemPath} must contain all coordinate/terrain fields or all null fields.");
            }

            if (q is not null && r is not null && !coordinates.Add((q.Value, r.Value)))
            {
                throw Invalid("duplicate-hex-coordinate", $"Duplicate axial coordinate ({q},{r}).");
            }

            hexes.Add(new HexFact(hexId, q, r, terrainId, terrainRevision));
        }

        var locationElements = RequiredArray(element, "locations", path + ".locations", MaximumLocations);
        var locations = ImmutableArray.CreateBuilder<LocationFact>(locationElements.Count);
        var locationIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < locationElements.Count; index++)
        {
            var itemPath = $"{path}.locations[{index}]";
            var item = locationElements[index];
            RequireExactProperties(item, itemPath, "locationId", "anchorHexId");
            var id = RequiredIdentity(item, "locationId", itemPath + ".locationId");
            if (!locationIds.Add(id))
            {
                throw Invalid("duplicate-location-id", $"Duplicate Location identity '{id}'.");
            }

            locations.Add(new LocationFact(id, RequiredIdentity(item, "anchorHexId", itemPath + ".anchorHexId")));
        }

        return new GeographyCapability(scope, coverage, sourceStamp, hexes.MoveToImmutable(), locations.MoveToImmutable());
    }

    private static CitiesCapability ReadCities(JsonElement element)
    {
        const string path = "$.capabilities.cities/v1";
        RequireExactProperties(element, path, "scope", "coverage", "sourceStamp", "bindingCoverage", "rows");
        var scope = RequiredIdentity(element, "scope", path + ".scope");
        var coverage = RequiredCoverage(element, "coverage", path + ".coverage");
        var sourceStamp = NullableString(element, "sourceStamp", path + ".sourceStamp", allowEmpty: false);
        var bindingCoverage = RequiredCoverage(element, "bindingCoverage", path + ".bindingCoverage");
        if (scope != "world-city-roster" || coverage != CapabilityCoverage.Complete || sourceStamp is not null ||
            bindingCoverage != CapabilityCoverage.Complete)
        {
            throw Invalid("invalid-city-scope", "Demo Cities must be a complete world-city-roster with complete binding coverage and a null source stamp.");
        }

        var items = RequiredArray(element, "rows", path + ".rows", MaximumCities);
        var rows = ImmutableArray.CreateBuilder<CityFact>(items.Count);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < items.Count; index++)
        {
            var itemPath = $"{path}.rows[{index}]";
            var item = items[index];
            RequireExactProperties(item, itemPath, "cityId", "label", "p8LocationId");
            var id = RequiredIdentity(item, "cityId", itemPath + ".cityId");
            if (!ids.Add(id))
            {
                throw Invalid("duplicate-city-id", $"Duplicate City identity '{id}'.");
            }

            rows.Add(new CityFact(
                id,
                NullableString(item, "label", itemPath + ".label", allowEmpty: true),
                NullableString(item, "p8LocationId", itemPath + ".p8LocationId", allowEmpty: false)));
        }

        return new CitiesCapability(scope, coverage, sourceStamp, bindingCoverage, rows.MoveToImmutable());
    }

    private static CityMarketStockCapability ReadStock(JsonElement element)
    {
        const string path = "$.capabilities.city-market-stock/v1";
        RequireExactProperties(element, path, "scope", "coverage", "sourceStamp", "rows");
        var scopeElement = Required(element, "scope", path + ".scope");
        RequireExactProperties(scopeElement, path + ".scope", "cityId", "itemDefinitionId");
        var scope = new StockScope(
            RequiredIdentity(scopeElement, "cityId", path + ".scope.cityId"),
            RequiredIdentity(scopeElement, "itemDefinitionId", path + ".scope.itemDefinitionId"));
        var coverage = RequiredCoverage(element, "coverage", path + ".coverage");
        var sourceStamp = NullableString(element, "sourceStamp", path + ".sourceStamp", allowEmpty: false);
        if (coverage != CapabilityCoverage.Complete || sourceStamp is not null)
        {
            throw Invalid("invalid-stock-scope", "Demo city-market-stock must be complete within its named scope and have a null source stamp.");
        }

        var items = RequiredArray(element, "rows", path + ".rows", 1);
        var rows = ImmutableArray.CreateBuilder<StockRow>(items.Count);
        var keys = new HashSet<(string CityId, string ItemDefinitionId)>();
        for (var index = 0; index < items.Count; index++)
        {
            var itemPath = $"{path}.rows[{index}]";
            var item = items[index];
            RequireExactProperties(item, itemPath, "cityId", "itemDefinitionId", "quantity");
            var cityId = RequiredIdentity(item, "cityId", itemPath + ".cityId");
            var itemId = RequiredIdentity(item, "itemDefinitionId", itemPath + ".itemDefinitionId");
            if (!StringComparer.Ordinal.Equals(cityId, scope.CityId) || !StringComparer.Ordinal.Equals(itemId, scope.ItemDefinitionId))
            {
                throw Invalid("stock-row-outside-scope", $"{itemPath} does not match the declared stock scope.");
            }

            if (!keys.Add((cityId, itemId)))
            {
                throw Invalid("duplicate-stock-row", "The stock scope contains a duplicate row.");
            }

            rows.Add(new StockRow(cityId, itemId, RequiredNonNegativeInt64(item, "quantity", itemPath + ".quantity")));
        }

        return new CityMarketStockCapability(scope, coverage, sourceStamp, rows.MoveToImmutable());
    }

    private static void ValidateReferences(GeographyCapability geography, CitiesCapability cities, CityMarketStockCapability stock)
    {
        var hexIds = geography.Hexes.Select(hex => hex.HexId).ToHashSet(StringComparer.Ordinal);
        var locationIds = geography.Locations.Select(location => location.LocationId).ToHashSet(StringComparer.Ordinal);
        var cityIds = cities.Rows.Select(city => city.CityId).ToHashSet(StringComparer.Ordinal);

        foreach (var location in geography.Locations)
        {
            if (!hexIds.Contains(location.AnchorHexId))
            {
                throw Invalid("dangling-location-anchor", $"Location '{location.LocationId}' refers to missing Hex '{location.AnchorHexId}'.");
            }
        }

        foreach (var city in cities.Rows)
        {
            if (city.P8LocationId is not null && !locationIds.Contains(city.P8LocationId))
            {
                throw Invalid("dangling-city-location", $"City '{city.CityId}' refers to missing Location '{city.P8LocationId}'.");
            }
        }

        if (!cityIds.Contains(stock.Scope.CityId))
        {
            throw Invalid("dangling-stock-city", $"Stock scope refers to missing City '{stock.Scope.CityId}'.");
        }
    }

    private static void RequireExactProperties(JsonElement element, string path, params string[] expected)
    {
        RequireObject(element, path);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!names.Add(property.Name))
            {
                throw Invalid("duplicate-property", $"{path} contains duplicate property '{property.Name}'.");
            }
        }

        var expectedSet = expected.ToHashSet(StringComparer.Ordinal);
        var unknown = names.Except(expectedSet, StringComparer.Ordinal).FirstOrDefault();
        if (unknown is not null)
        {
            throw Invalid("unknown-property", $"{path} contains unsupported property '{unknown}'.");
        }

        var missing = expectedSet.Except(names, StringComparer.Ordinal).FirstOrDefault();
        if (missing is not null)
        {
            throw Invalid("missing-property", $"{path} is missing required property '{missing}'.");
        }
    }

    private static JsonElement Required(JsonElement parent, string name, string path)
    {
        if (!parent.TryGetProperty(name, out var value))
        {
            throw Invalid("missing-property", $"{path} is required.");
        }

        return value;
    }

    private static List<JsonElement> RequiredArray(JsonElement parent, string name, string path, int maximumCount)
    {
        var element = Required(parent, name, path);
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw Invalid("invalid-array", $"{path} must be an array.");
        }

        var values = element.EnumerateArray().ToList();
        if (values.Count > maximumCount)
        {
            throw Invalid("too-many-rows", $"{path} contains more than {maximumCount:N0} rows.");
        }

        return values;
    }

    private static string RequiredIdentity(JsonElement parent, string name, string path)
    {
        var value = RequiredString(parent, name, path);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid("blank-identity", $"{path} must not be blank.");
        }

        return value;
    }

    private static string RequiredString(JsonElement parent, string name, string path)
    {
        var value = Required(parent, name, path);
        if (value.ValueKind != JsonValueKind.String)
        {
            throw Invalid("invalid-string", $"{path} must be a string.");
        }

        var text = value.GetString()!;
        if (text.Length > MaximumStringLength)
        {
            throw Invalid("string-too-long", $"{path} exceeds {MaximumStringLength} UTF-16 code units.");
        }

        return text;
    }

    private static string? NullableString(JsonElement parent, string name, string path, bool allowEmpty)
    {
        var value = Required(parent, name, path);
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw Invalid("invalid-string", $"{path} must be a string or null.");
        }

        var text = value.GetString()!;
        if (text.Length > MaximumStringLength)
        {
            throw Invalid("string-too-long", $"{path} exceeds {MaximumStringLength} UTF-16 code units.");
        }

        if (!allowEmpty && string.IsNullOrWhiteSpace(text))
        {
            throw Invalid("blank-identity", $"{path} must not be blank when present.");
        }

        return text;
    }

    private static int RequiredInt32(JsonElement parent, string name, string path)
    {
        var value = Required(parent, name, path);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
        {
            throw Invalid("invalid-integer", $"{path} must be an Int32 JSON number.");
        }

        return number;
    }

    private static int? NullableInt32(JsonElement parent, string name, string path)
    {
        var value = Required(parent, name, path);
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
        {
            throw Invalid("invalid-integer", $"{path} must be an Int32 JSON number or null.");
        }

        return number;
    }

    private static long RequiredNonNegativeInt64(JsonElement parent, string name, string path)
    {
        var value = Required(parent, name, path);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var number) || number < 0)
        {
            throw Invalid("invalid-quantity", $"{path} must be a non-negative Int64 JSON number.");
        }

        return number;
    }

    private static long RequiredCanonicalNonNegativeInt64(JsonElement parent, string name, string path)
    {
        var value = RequiredString(parent, name, path);
        if (value.Length == 0 || (value.Length > 1 && value[0] == '0') ||
            !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 0)
        {
            throw Invalid("invalid-decimal-string", $"{path} must be a canonical non-negative Int64 decimal string.");
        }

        return number;
    }

    private static CapabilityCoverage RequiredCoverage(JsonElement parent, string name, string path)
    {
        return RequiredString(parent, name, path) switch
        {
            "complete" => CapabilityCoverage.Complete,
            "unsupported" => CapabilityCoverage.Unsupported,
            "not-included" => CapabilityCoverage.NotIncluded,
            var value => throw Invalid("invalid-coverage", $"{path} has unknown coverage '{value}'.")
        };
    }

    private static bool IsLowerSha256(string? value)
    {
        if (value is null || value.Length != 64)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
            {
                return false;
            }
        }

        return true;
    }

    private static void RequireObject(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Invalid("invalid-object", $"{path} must be an object.");
        }
    }

    private static ObservationValidationException Invalid(string code, string message, Exception? inner = null) =>
        inner is null
            ? new ObservationValidationException(code, message)
            : new ObservationValidationException(code, message, inner);
}
