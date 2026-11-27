using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;

internal sealed class DailyV1ObservationEnvelope
{
    internal const string Schema = "simulation.lab.observation";
    internal const string Profile = "atlas.daily-map-stock/v1";
    internal const string CompositionProfile = "UnityBootstrap-Daily-v1";
    internal const string ScenarioId = "selected-daily-stock";
    internal const string ItemDefinitionId = "TRIGO";

    internal DailyV1ObservationEnvelope(
        DailyV1AtlasObservation observation,
        string runId,
        long observationIndex,
        string previousDigest,
        string buildDigest)
    {
        Observation = observation;
        RunId = runId;
        ObservationIndex = observationIndex;
        PreviousDigest = previousDigest;
        BuildDigest = buildDigest;
    }

    internal DailyV1AtlasObservation Observation { get; }
    internal string RunId { get; }
    internal long ObservationIndex { get; }
    internal string PreviousDigest { get; }
    internal string BuildDigest { get; }
}

internal static class DailyV1ObservationBuildIdentity
{
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static bool TryCompute(
        DailyV1HostEnvironmentIdentity identity,
        out string digest)
    {
        digest = null;
        if (identity == null || identity.Assemblies == null
            || identity.Assemblies.Count < 1 || identity.Assemblies.Count > 4096)
            return false;

        try
        {
            List<P12AAssemblyIdentityV2> assemblies =
                new List<P12AAssemblyIdentityV2>(identity.Assemblies);
            assemblies.Sort(CompareAssembly);

            List<string> fields = new List<string>(10 + assemblies.Count * 3)
            {
                "lab-observation-build/v1",
                identity.HostKind.ToString(CultureInfo.InvariantCulture),
                identity.RuntimeFrameworkDescription,
                identity.RuntimeVersion,
                identity.OperatingSystemDescription,
                identity.RuntimeIdentifier,
                identity.ProcessArchitecture,
                identity.TargetFramework,
                identity.NumericProfileId,
                identity.NumericProfileVersion.ToString(CultureInfo.InvariantCulture),
                assemblies.Count.ToString(CultureInfo.InvariantCulture)
            };

            foreach (P12AAssemblyIdentityV2 assembly in assemblies)
            {
                if (assembly == null
                    || string.IsNullOrWhiteSpace(assembly.SimpleName)
                    || !IsCanonicalVersion(assembly.Version)
                    || !Guid.TryParseExact(assembly.Mvid, "D", out Guid mvid)
                    || !string.Equals(mvid.ToString("D").ToLowerInvariant(),
                        assembly.Mvid, StringComparison.Ordinal))
                    return false;

                fields.Add(assembly.SimpleName);
                fields.Add(assembly.Version);
                fields.Add(assembly.Mvid);
            }

            using (MemoryStream framed = new MemoryStream())
            {
                foreach (string field in fields)
                {
                    if (field == null)
                        return false;
                    byte[] bytes = StrictUtf8.GetBytes(field);
                    uint length = checked((uint)bytes.Length);
                    framed.WriteByte((byte)(length >> 24));
                    framed.WriteByte((byte)(length >> 16));
                    framed.WriteByte((byte)(length >> 8));
                    framed.WriteByte((byte)length);
                    framed.Write(bytes, 0, bytes.Length);
                }

                using (SHA256 sha = SHA256.Create())
                    digest = ToLowerHex(sha.ComputeHash(framed.ToArray()));
            }

            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is EncoderFallbackException
            || exception is OverflowException
            || exception is CryptographicException)
        {
            digest = null;
            return false;
        }
    }

    private static int CompareAssembly(P12AAssemblyIdentityV2 left, P12AAssemblyIdentityV2 right)
    {
        if (left == null) return right == null ? 0 : -1;
        if (right == null) return 1;
        int result = StringComparer.Ordinal.Compare(left.SimpleName, right.SimpleName);
        if (result != 0) return result;
        result = StringComparer.Ordinal.Compare(left.Version, right.Version);
        return result != 0
            ? result
            : StringComparer.Ordinal.Compare(left.Mvid, right.Mvid);
    }

    private static bool IsCanonicalVersion(string value)
    {
        if (string.IsNullOrEmpty(value) || !Version.TryParse(value, out Version version))
            return false;
        return string.Equals(version.ToString(), value, StringComparison.Ordinal);
    }

    internal static string ToLowerHex(byte[] bytes)
    {
        StringBuilder result = new StringBuilder(bytes.Length * 2);
        foreach (byte value in bytes)
            result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return result.ToString();
    }
}

internal static class DailyV1ObservationV1Encoder
{
    internal const int MaximumArtifactBytes = 4 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static bool TryEncode(
        DailyV1ObservationEnvelope envelope,
        out byte[] bytes,
        out string diagnostic)
    {
        bytes = null;
        diagnostic = "OBSERVATION_INVALID";
        if (!TryValidate(envelope, out diagnostic))
            return false;

        try
        {
            StringBuilder json = new StringBuilder(8192);
            DailyV1AtlasObservation observation = envelope.Observation;

            json.Append('{');
            StringProperty(json, "schema", DailyV1ObservationEnvelope.Schema);
            NumberProperty(json, "version", 1);
            StringProperty(json, "profile", DailyV1ObservationEnvelope.Profile);
            StringProperty(json, "sourceKind", "core");
            StringProperty(json, "worldId", observation.WorldId);
            StringProperty(json, "runId", envelope.RunId);
            StringProperty(json, "observationIndex",
                envelope.ObservationIndex.ToString(CultureInfo.InvariantCulture));
            PropertyName(json, "previousDigest");
            if (envelope.PreviousDigest == null) json.Append("null,");
            else { Quoted(json, envelope.PreviousDigest); json.Append(','); }

            PropertyName(json, "boundary");
            json.Append('{');
            StringProperty(json, "absoluteDay",
                observation.AbsoluteDay.ToString(CultureInfo.InvariantCulture));
            StringProperty(json, "completedSequence",
                observation.CompletedSequence.ToString(CultureInfo.InvariantCulture), false);
            json.Append("},");

            PropertyName(json, "producer");
            json.Append('{');
            StringProperty(json, "id", "simulation.lab");
            NumberProperty(json, "version", 1);
            StringProperty(json, "build", envelope.BuildDigest);
            StringProperty(json, "scenarioId", DailyV1ObservationEnvelope.ScenarioId);
            StringProperty(json, "scenarioRevision", "1");
            StringProperty(json, "compositionProfile", DailyV1ObservationEnvelope.CompositionProfile);
            StringProperty(json, "effectiveInputFingerprint",
                observation.EffectiveInputFingerprint, false);
            json.Append("},");

            PropertyName(json, "capabilities");
            json.Append('{');
            WriteGeography(json, observation);
            WriteCities(json, observation);
            WriteMarket(json, observation);
            json.Append("}}");
            json.Append('\n');

            byte[] candidate = StrictUtf8.GetBytes(json.ToString());
            if (candidate.Length == 0 || candidate.Length > MaximumArtifactBytes)
            {
                diagnostic = "ARTIFACT_TOO_LARGE";
                return false;
            }

            bytes = candidate;
            diagnostic = "NONE";
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is EncoderFallbackException
            || exception is OverflowException)
        {
            diagnostic = "ENCODING_FAILED";
            return false;
        }
    }

    private static bool TryValidate(
        DailyV1ObservationEnvelope envelope,
        out string diagnostic)
    {
        diagnostic = "OBSERVATION_INVALID";
        if (envelope == null || envelope.Observation == null
            || envelope.ObservationIndex < 0
            || !IsPrefixedGuid(envelope.RunId, "run:")
            || !IsLowerHexDigest(envelope.BuildDigest)
            || (envelope.ObservationIndex == 0
                ? envelope.PreviousDigest != null
                : !IsLowerHexDigest(envelope.PreviousDigest)))
        {
            diagnostic = "PROVENANCE_INVALID";
            return false;
        }

        diagnostic = "SOURCE_FACTS_INVALID";
        DailyV1AtlasObservation observation = envelope.Observation;
        if (!WorldId.TryParse(observation.WorldId, out _)
            || !IsLowerHexDigest(observation.EffectiveInputFingerprint)
            || observation.AbsoluteDay <= 0
            || observation.CompletedSequence <= 0
            || observation.SpatialRevision < 0
            || observation.IdentityIndexRevision < 0
            || observation.AnchorRevision < 0
            || observation.MarketRevision < 0
            || observation.Hexes == null || observation.Locations == null
            || observation.Cities == null || observation.MarketStockRows == null
            || observation.Hexes.Count == 0 || observation.Locations.Count == 0
            || observation.Cities.Count == 0
            || observation.Hexes.Count > 4096 || observation.Locations.Count > 4096
            || observation.Cities.Count > 1024 || observation.MarketStockRows.Count > 1)
        {
            diagnostic = "SOURCE_FACTS_INVALID";
            return false;
        }

        HashSet<string> hexIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> coordinates = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1AtlasHexObservation hex in observation.Hexes)
        {
            if (hex == null || !IsIdentifier(hex.HexId)
                || !hexIds.Add(hex.HexId)
                || hex.Q.HasValue != hex.R.HasValue)
                return false;
            if (hex.Q.HasValue)
            {
                if (!IsIdentifier(hex.TerrainId) || !IsIdentifier(hex.TerrainRevision)
                    || !coordinates.Add(hex.Q.Value.ToString(CultureInfo.InvariantCulture)
                        + "," + hex.R.Value.ToString(CultureInfo.InvariantCulture)))
                    return false;
            }
            else if (hex.TerrainId != null || hex.TerrainRevision != null)
                return false;
        }

        HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1AtlasLocationObservation location in observation.Locations)
        {
            if (location == null || !IsIdentifier(location.LocationId)
                || !IsIdentifier(location.AnchorHexId)
                || !locationIds.Add(location.LocationId)
                || !hexIds.Contains(location.AnchorHexId))
                return false;
        }

        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> boundLocationIds = new HashSet<string>(StringComparer.Ordinal);
        bool scopeCityFound = false;
        foreach (DailyV1AtlasCityObservation city in observation.Cities)
        {
            if (city == null || !IsIdentifier(city.CityId) || !cityIds.Add(city.CityId)
                || (city.Label != null && !IsText(city.Label, true))
                || (city.P8LocationId != null
                    && (!IsIdentifier(city.P8LocationId)
                        || !locationIds.Contains(city.P8LocationId)
                        || !boundLocationIds.Add(city.P8LocationId))))
                return false;
            if (string.Equals(city.CityId, observation.ScopedCityId, StringComparison.Ordinal))
                scopeCityFound = true;
        }

        if (!scopeCityFound
            || !IsIdentifier(observation.ScopedCityId)
            || !string.Equals(observation.ScopedItemDefinitionId,
                DailyV1ObservationEnvelope.ItemDefinitionId, StringComparison.Ordinal))
            return false;

        foreach (DailyV1AtlasMarketStockObservation row in observation.MarketStockRows)
        {
            if (row == null || !IsIdentifier(row.CityId)
                || !IsIdentifier(row.ItemDefinitionId)
                || row.Quantity < 0
                || !string.Equals(row.CityId, observation.ScopedCityId, StringComparison.Ordinal)
                || !string.Equals(row.ItemDefinitionId,
                    observation.ScopedItemDefinitionId, StringComparison.Ordinal))
                return false;
        }

        diagnostic = "NONE";
        return true;
    }

    private static void WriteGeography(StringBuilder json, DailyV1AtlasObservation observation)
    {
        PropertyName(json, "geography/v1");
        json.Append('{');
        StringProperty(json, "scope", "registered-geography");
        StringProperty(json, "coverage", "complete");
        PropertyName(json, "sourceStamp");
        json.Append('{');
        StringProperty(json, "spatialRevision",
            observation.SpatialRevision.ToString(CultureInfo.InvariantCulture), false);
        json.Append("},");
        PropertyName(json, "hexes");
        json.Append('[');
        List<DailyV1AtlasHexObservation> hexes =
            new List<DailyV1AtlasHexObservation>(observation.Hexes);
        hexes.Sort((left, right) => StringComparer.Ordinal.Compare(left.HexId, right.HexId));
        for (int index = 0; index < hexes.Count; index++)
        {
            if (index != 0) json.Append(',');
            DailyV1AtlasHexObservation hex = hexes[index];
            json.Append('{');
            StringProperty(json, "hexId", hex.HexId);
            NullableNumberProperty(json, "q", hex.Q);
            NullableNumberProperty(json, "r", hex.R);
            NullableStringProperty(json, "terrainId", hex.TerrainId);
            NullableStringProperty(json, "terrainRevision", hex.TerrainRevision, false);
            json.Append('}');
        }
        json.Append("],");
        PropertyName(json, "locations");
        json.Append('[');
        List<DailyV1AtlasLocationObservation> locations =
            new List<DailyV1AtlasLocationObservation>(observation.Locations);
        locations.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.LocationId, right.LocationId));
        for (int index = 0; index < locations.Count; index++)
        {
            if (index != 0) json.Append(',');
            DailyV1AtlasLocationObservation location = locations[index];
            json.Append('{');
            StringProperty(json, "locationId", location.LocationId);
            StringProperty(json, "anchorHexId", location.AnchorHexId, false);
            json.Append('}');
        }
        json.Append("]},");
    }

    private static void WriteCities(StringBuilder json, DailyV1AtlasObservation observation)
    {
        PropertyName(json, "cities/v1");
        json.Append('{');
        StringProperty(json, "scope", "world-city-roster");
        StringProperty(json, "coverage", "complete");
        PropertyName(json, "sourceStamp");
        json.Append('{');
        StringProperty(json, "identityIndexRevision",
            observation.IdentityIndexRevision.ToString(CultureInfo.InvariantCulture));
        StringProperty(json, "anchorRevision",
            observation.AnchorRevision.ToString(CultureInfo.InvariantCulture), false);
        json.Append("},");
        StringProperty(json, "bindingCoverage", "complete");
        PropertyName(json, "rows");
        json.Append('[');
        List<DailyV1AtlasCityObservation> cities =
            new List<DailyV1AtlasCityObservation>(observation.Cities);
        cities.Sort((left, right) => StringComparer.Ordinal.Compare(left.CityId, right.CityId));
        for (int index = 0; index < cities.Count; index++)
        {
            if (index != 0) json.Append(',');
            DailyV1AtlasCityObservation city = cities[index];
            json.Append('{');
            StringProperty(json, "cityId", city.CityId);
            NullableStringProperty(json, "label", city.Label);
            NullableStringProperty(json, "p8LocationId", city.P8LocationId, false);
            json.Append('}');
        }
        json.Append("]},");
    }

    private static void WriteMarket(StringBuilder json, DailyV1AtlasObservation observation)
    {
        PropertyName(json, "city-market-stock/v1");
        json.Append('{');
        PropertyName(json, "scope");
        json.Append('{');
        StringProperty(json, "cityId", observation.ScopedCityId);
        StringProperty(json, "itemDefinitionId", observation.ScopedItemDefinitionId, false);
        json.Append("},");
        StringProperty(json, "coverage", "complete");
        PropertyName(json, "sourceStamp");
        json.Append('{');
        StringProperty(json, "marketRevision",
            observation.MarketRevision.ToString(CultureInfo.InvariantCulture), false);
        json.Append("},");
        PropertyName(json, "rows");
        json.Append('[');
        for (int index = 0; index < observation.MarketStockRows.Count; index++)
        {
            if (index != 0) json.Append(',');
            DailyV1AtlasMarketStockObservation row = observation.MarketStockRows[index];
            json.Append('{');
            StringProperty(json, "cityId", row.CityId);
            StringProperty(json, "itemDefinitionId", row.ItemDefinitionId);
            NumberProperty(json, "quantity", row.Quantity, false);
            json.Append('}');
        }
        json.Append("]}");
    }

    private static bool IsIdentifier(string value)
    {
        if (!IsText(value, true))
            return false;
        if (value[0] == '/' || value[0] == '\\'
            || (value.Length >= 2 && value[1] == ':' && char.IsLetter(value[0])))
            return false;
        return true;
    }

    private static bool IsText(string value, bool requireNonWhitespace)
    {
        if (value == null || value.Length > 512
            || (requireNonWhitespace && string.IsNullOrWhiteSpace(value)))
            return false;
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (char.IsControl(current))
                return false;
            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                    return false;
                index++;
            }
            else if (char.IsLowSurrogate(current))
                return false;
        }
        return true;
    }

    private static bool IsPrefixedGuid(string value, string prefix)
    {
        if (value == null || value.Length != prefix.Length + 32
            || !value.StartsWith(prefix, StringComparison.Ordinal))
            return false;
        string digits = value.Substring(prefix.Length);
        if (!Guid.TryParseExact(digits, "N", out Guid parsed) || parsed == Guid.Empty)
            return false;
        for (int index = 0; index < digits.Length; index++)
        {
            char current = digits[index];
            if (!((current >= '0' && current <= '9') || (current >= 'a' && current <= 'f')))
                return false;
        }
        return true;
    }

    internal static bool IsLowerHexDigest(string value)
    {
        if (value == null || value.Length != 64)
            return false;
        foreach (char current in value)
            if (!((current >= '0' && current <= '9') || (current >= 'a' && current <= 'f')))
                return false;
        return true;
    }

    private static void PropertyName(StringBuilder json, string name)
    {
        Quoted(json, name);
        json.Append(':');
    }

    private static void StringProperty(
        StringBuilder json, string name, string value, bool comma = true)
    {
        PropertyName(json, name);
        Quoted(json, value);
        if (comma) json.Append(',');
    }

    private static void NullableStringProperty(
        StringBuilder json, string name, string value, bool comma = true)
    {
        PropertyName(json, name);
        if (value == null) json.Append("null");
        else Quoted(json, value);
        if (comma) json.Append(',');
    }

    private static void NumberProperty(
        StringBuilder json, string name, int value, bool comma = true)
    {
        PropertyName(json, name);
        json.Append(value.ToString(CultureInfo.InvariantCulture));
        if (comma) json.Append(',');
    }

    private static void NullableNumberProperty(
        StringBuilder json, string name, int? value, bool comma = true)
    {
        PropertyName(json, name);
        if (value.HasValue) json.Append(value.Value.ToString(CultureInfo.InvariantCulture));
        else json.Append("null");
        if (comma) json.Append(',');
    }

    private static void Quoted(StringBuilder json, string value)
    {
        json.Append('"');
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            switch (current)
            {
                case '"': json.Append("\\\""); break;
                case '\\': json.Append("\\\\"); break;
                default:
                    if (current <= 0x1f)
                    {
                        json.Append("\\u");
                        json.Append(((int)current).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        json.Append(current);
                        if (char.IsHighSurrogate(current))
                            json.Append(value[++index]);
                    }
                    break;
            }
        }
        json.Append('"');
    }
}

internal interface IDailyV1ObservationTempFile : IDisposable
{
    void Write(byte[] bytes);
    void FlushToDisk();
}

internal interface IDailyV1ObservationFileOperations
{
    IDailyV1ObservationTempFile CreateNew(string path);
    byte[] ReadAllBytes(string path);
    void MoveNoOverwrite(string source, string destination);
    void DeleteFileIfExists(string path);
}

internal sealed class SystemDailyV1ObservationFileOperations :
    IDailyV1ObservationFileOperations
{
    internal static readonly SystemDailyV1ObservationFileOperations Instance =
        new SystemDailyV1ObservationFileOperations();

    private SystemDailyV1ObservationFileOperations() { }

    public IDailyV1ObservationTempFile CreateNew(string path) =>
        new DailyV1ObservationTempFile(
            new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.WriteThrough));

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
    public void MoveNoOverwrite(string source, string destination) =>
        File.Move(source, destination, false);
    public void DeleteFileIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private sealed class DailyV1ObservationTempFile : IDailyV1ObservationTempFile
    {
        private FileStream stream;
        internal DailyV1ObservationTempFile(FileStream stream) { this.stream = stream; }
        public void Write(byte[] bytes) => stream.Write(bytes, 0, bytes.Length);
        public void FlushToDisk() => stream.Flush(true);
        public void Dispose()
        {
            FileStream current = stream;
            stream = null;
            current?.Dispose();
        }
    }
}

internal sealed class DailyV1ObservationArtifactPublisher
{
    private readonly IDailyV1ObservationFileOperations operations;

    internal DailyV1ObservationArtifactPublisher(
        IDailyV1ObservationFileOperations operations)
    {
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));
    }

    internal bool TryPublish(
        string outputDirectory,
        int index,
        byte[] bytes,
        out string fileName,
        out string digest,
        out string diagnostic)
    {
        fileName = null;
        digest = null;
        diagnostic = "PUBLISH_INVALID";
        if (string.IsNullOrWhiteSpace(outputDirectory)
            || index < 0 || index > 1
            || bytes == null || bytes.Length == 0
            || bytes.Length > DailyV1ObservationV1Encoder.MaximumArtifactBytes)
            return false;

        string name = index.ToString("D4", CultureInfo.InvariantCulture)
            + ".atlas-observation.json";
        string finalPath;
        string temporaryPath;
        try
        {
            string fullOutput = Path.GetFullPath(outputDirectory);
            if (!Directory.Exists(fullOutput))
            {
                diagnostic = "OUTPUT_DIRECTORY_INVALID";
                return false;
            }
            finalPath = Path.Combine(fullOutput, name);
            temporaryPath = Path.Combine(fullOutput,
                "." + name + "." + Guid.NewGuid().ToString("N") + ".tmp");
            if (File.Exists(finalPath) || Directory.Exists(finalPath))
            {
                diagnostic = "OUTPUT_EXISTS";
                return false;
            }
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is IOException || exception is UnauthorizedAccessException
            || exception is NotSupportedException)
        {
            diagnostic = "OUTPUT_PATH_INVALID";
            return false;
        }

        string stage = "TEMP_CREATE_FAILED";
        bool committed = false;
        IDailyV1ObservationTempFile temporary = null;
        string candidateDigest;
        using (SHA256 sha = SHA256.Create())
            candidateDigest = DailyV1ObservationBuildIdentity.ToLowerHex(sha.ComputeHash(bytes));

        try
        {
            temporary = operations.CreateNew(temporaryPath);
            stage = "TEMP_WRITE_FAILED";
            temporary.Write(bytes);
            stage = "TEMP_FLUSH_FAILED";
            temporary.FlushToDisk();
            temporary.Dispose();
            temporary = null;

            stage = "TEMP_READBACK_FAILED";
            byte[] readBack = operations.ReadAllBytes(temporaryPath);
            if (readBack == null || !BytesEqual(bytes, readBack)
                || !string.Equals(candidateDigest,
                    ComputeSha256(readBack), StringComparison.Ordinal))
                throw new IOException("Temporary artifact bytes changed before publication.");

            stage = "RENAME_FAILED";
            operations.MoveNoOverwrite(temporaryPath, finalPath);
            committed = true;
            fileName = name;
            digest = candidateDigest;
            diagnostic = "NONE";
            return true;
        }
        catch (Exception exception) when (exception is IOException
            || exception is UnauthorizedAccessException || exception is ArgumentException
            || exception is NotSupportedException || exception is CryptographicException)
        {
            diagnostic = stage;
            return false;
        }
        finally
        {
            if (!committed)
            {
                try { temporary?.Dispose(); }
                catch (Exception) { }
                try { operations.DeleteFileIfExists(temporaryPath); }
                catch (Exception) { }
            }
        }
    }

    private static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length) return false;
        for (int index = 0; index < left.Length; index++)
            if (left[index] != right[index]) return false;
        return true;
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return DailyV1ObservationBuildIdentity.ToLowerHex(sha.ComputeHash(bytes));
    }
}

internal static class DailyV1ObservationCommand
{
    private static readonly DailyV1ObservationArtifactPublisher Publisher =
        new DailyV1ObservationArtifactPublisher(SystemDailyV1ObservationFileOperations.Instance);

    internal static bool TryParseArguments(string[] args, out string outputDirectory)
    {
        outputDirectory = null;
        if (args == null || args.Length != 3
            || !string.Equals(args[0], "observe-daily", StringComparison.Ordinal)
            || !string.Equals(args[1], "--out", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(args[2]))
            return false;
        outputDirectory = args[2];
        return true;
    }

    internal static int Run(string requestedOutputDirectory)
    {
        if (!TryCreateOutputDirectory(requestedOutputDirectory, out string outputDirectory))
        {
            Report("OUTPUT_REJECTED");
            return 1;
        }

        int exitCode = 1;
        string scratchRoot = null;
        bool ownsScratchRoot = false;
        DailyV1LabObservationRun run = null;
        try
        {
            if (!TryCreateScratchRoot(outputDirectory, out scratchRoot))
            {
                Report("SCRATCH_REJECTED");
                return exitCode;
            }
            ownsScratchRoot = true;

            DailyV1HostEnvironmentIdentity identity =
                DailyV1DotNetHostEnvironmentIdentityFactory.Capture(typeof(Program));
            if (!DailyV1ObservationBuildIdentity.TryCompute(identity, out string buildDigest))
            {
                Report("BUILD_IDENTITY_REJECTED");
                return exitCode;
            }
            string runId = "run:" + NewNonzeroGuid();
            if (!DailyV1LabObservationRun.TryStart(scratchRoot, identity,
                    out run, out DailyV1LabObservationStartFailure startFailure))
            {
                Report("START_REJECTED_" + startFailure.ToString().ToUpperInvariant());
                return exitCode;
            }

            DailyV1LabObservationAdvanceResult firstResult = run.AdvanceAndObserve();
            if (!TryGetExpectedCut(firstResult, 1L, 1L,
                    out DailyV1AtlasObservation first, out string firstFailure))
            {
                Report(firstFailure);
                return exitCode;
            }

            DailyV1ObservationEnvelope firstEnvelope =
                new DailyV1ObservationEnvelope(first, runId, 0L, null, buildDigest);
            if (!DailyV1ObservationV1Encoder.TryEncode(firstEnvelope,
                    out byte[] firstBytes, out string encodeFailure))
            {
                Report(encodeFailure);
                return exitCode;
            }
            if (!Publisher.TryPublish(outputDirectory, 0, firstBytes,
                    out string firstName, out string firstDigest, out string publishFailure))
            {
                Report(publishFailure);
                return exitCode;
            }
            ReportArtifact(firstName, firstDigest);

            DailyV1LabObservationAdvanceResult secondResult = run.AdvanceAndObserve();
            if (!TryGetExpectedCut(secondResult, 2L, 2L,
                    out DailyV1AtlasObservation second, out string secondFailure))
            {
                Report(secondFailure);
                return exitCode;
            }
            if (!SameRunPair(first, second, out long delta))
            {
                Report("OBSERVATION_PAIR_REJECTED");
                return exitCode;
            }
            if (delta == 0)
            {
                Report("STOCK_DELTA_ZERO");
                return exitCode;
            }

            DailyV1ObservationEnvelope secondEnvelope =
                new DailyV1ObservationEnvelope(second, runId, 1L, firstDigest, buildDigest);
            if (!DailyV1ObservationV1Encoder.TryEncode(secondEnvelope,
                    out byte[] secondBytes, out encodeFailure))
            {
                Report(encodeFailure);
                return exitCode;
            }
            if (!Publisher.TryPublish(outputDirectory, 1, secondBytes,
                    out string secondName, out string secondDigest, out publishFailure))
            {
                Report(publishFailure);
                return exitCode;
            }
            ReportArtifact(secondName, secondDigest);
            exitCode = 0;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is IOException
            || exception is UnauthorizedAccessException || exception is SecurityException
            || exception is NotSupportedException || exception is OverflowException
            || exception is CryptographicException)
        {
            Report("PRODUCER_FAILED");
        }
        finally
        {
            if (run != null)
            {
                try { run.Dispose(); }
                catch (Exception) { exitCode = 1; Report("RUN_RELEASE_FAILED"); }
            }

            if (ownsScratchRoot && scratchRoot != null)
            {
                try
                {
                    if (Directory.Exists(scratchRoot)
                        && (File.GetAttributes(scratchRoot) & FileAttributes.ReparsePoint) == 0)
                        Directory.Delete(scratchRoot, true);
                }
                catch (Exception) { exitCode = 1; Report("SCRATCH_CLEANUP_FAILED"); }
            }
        }

        return exitCode;
    }

    private static bool TryCreateOutputDirectory(
        string requested, out string outputDirectory)
    {
        outputDirectory = null;
        if (string.IsNullOrWhiteSpace(requested)) return false;
        try
        {
            string full = Path.GetFullPath(requested);
            string parent = Path.GetDirectoryName(full);
            if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent)
                || !IsSupportedLocalPath(full)
                || Directory.Exists(full) || File.Exists(full)
                || IsReparsePoint(parent))
                return false;

            Directory.CreateDirectory(full);
            if (!Directory.Exists(full) || IsReparsePoint(full)
                || Directory.GetFileSystemEntries(full).Length != 0)
                return false;
            outputDirectory = full;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is IOException || exception is UnauthorizedAccessException
            || exception is NotSupportedException || exception is SecurityException)
        {
            return false;
        }
    }

    private static bool TryCreateScratchRoot(
        string outputDirectory, out string scratchRoot)
    {
        scratchRoot = null;
        try
        {
            string parent = Path.GetFullPath(Path.GetTempPath());
            if (!Directory.Exists(parent) || !IsSupportedLocalPath(parent)
                || IsReparsePoint(parent))
                return false;

            for (int attempt = 0; attempt < 8; attempt++)
            {
                string candidate = Path.Combine(parent,
                    "simulation-lab-observation-" + Guid.NewGuid().ToString("N"));
                if (Directory.Exists(candidate) || File.Exists(candidate))
                    continue;

                if (SamePath(candidate, outputDirectory))
                    continue;
                Directory.CreateDirectory(candidate);
                if (IsReparsePoint(candidate)
                    || Directory.GetFileSystemEntries(candidate).Length != 0)
                {
                    scratchRoot = candidate;
                    return false;
                }
                scratchRoot = candidate;
                return true;
            }
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is IOException || exception is UnauthorizedAccessException
            || exception is NotSupportedException || exception is SecurityException)
        {
            return false;
        }
        return false;
    }

    private static bool TryGetExpectedCut(
        DailyV1LabObservationAdvanceResult result,
        long expectedDay,
        long expectedSequence,
        out DailyV1AtlasObservation observation,
        out string diagnostic)
    {
        observation = null;
        diagnostic = "ADVANCE_REJECTED";
        if (result == null || result.Disposition != DailyV1LabObservationDisposition.Captured
            || result.Observation == null)
        {
            if (result != null)
            {
                diagnostic = result.Disposition == DailyV1LabObservationDisposition.AdvanceUnsuccessful
                    ? "ADVANCE_UNSUCCESSFUL"
                    : result.Disposition == DailyV1LabObservationDisposition.AdvancedObservationUnavailable
                        ? "CAPTURE_UNAVAILABLE"
                        : "ADVANCE_REJECTED";
            }
            return false;
        }

        DailyV1AtlasObservation candidate = result.Observation;
        if (candidate.AbsoluteDay != expectedDay
            || candidate.CompletedSequence != expectedSequence
            || !WorldId.TryParse(candidate.WorldId, out _)
            || !DailyV1ObservationV1Encoder.IsLowerHexDigest(
                candidate.EffectiveInputFingerprint)
            || candidate.Hexes.Count == 0
            || !candidate.Hexes.Any(hex => hex.Q.HasValue && hex.R.HasValue
                && !string.IsNullOrWhiteSpace(hex.TerrainId)
                && !string.IsNullOrWhiteSpace(hex.TerrainRevision))
            || candidate.Locations.Count == 0
            || candidate.Cities.Count != 2
            || candidate.Cities.Any(city => city.P8LocationId != null)
            || candidate.MarketStockRows.Count != 1
            || !string.Equals(candidate.ScopedItemDefinitionId,
                DailyV1ObservationEnvelope.ItemDefinitionId, StringComparison.Ordinal)
            || !string.Equals(candidate.MarketStockRows[0].CityId,
                candidate.ScopedCityId, StringComparison.Ordinal)
            || !string.Equals(candidate.MarketStockRows[0].ItemDefinitionId,
                DailyV1ObservationEnvelope.ItemDefinitionId, StringComparison.Ordinal)
            || candidate.MarketStockRows[0].Quantity < 0
            || !candidate.Cities.Any(city => string.Equals(city.CityId,
                candidate.ScopedCityId, StringComparison.Ordinal)))
        {
            diagnostic = "PROFILE_CUT_REJECTED";
            return false;
        }

        observation = candidate;
        diagnostic = "NONE";
        return true;
    }    private static bool SameRunPair(
        DailyV1AtlasObservation first,
        DailyV1AtlasObservation second,
        out long delta)
    {
        delta = 0;
        if (first == null || second == null
            || !string.Equals(first.WorldId, second.WorldId, StringComparison.Ordinal)
            || !string.Equals(first.EffectiveInputFingerprint,
                second.EffectiveInputFingerprint, StringComparison.Ordinal)
            || !string.Equals(first.ScopedCityId, second.ScopedCityId, StringComparison.Ordinal)
            || !string.Equals(first.ScopedItemDefinitionId,
                second.ScopedItemDefinitionId, StringComparison.Ordinal)
            || first.AbsoluteDay == long.MaxValue
            || first.CompletedSequence == long.MaxValue
            || second.AbsoluteDay != first.AbsoluteDay + 1
            || second.CompletedSequence != first.CompletedSequence + 1
            || first.MarketStockRows.Count != 1 || second.MarketStockRows.Count != 1
            || !string.Equals(first.MarketStockRows[0].CityId,
                second.MarketStockRows[0].CityId, StringComparison.Ordinal)
            || !string.Equals(first.MarketStockRows[0].ItemDefinitionId,
                second.MarketStockRows[0].ItemDefinitionId, StringComparison.Ordinal))
            return false;
        delta = (long)second.MarketStockRows[0].Quantity
            - (long)first.MarketStockRows[0].Quantity;
        return true;
    }

    private static string NewNonzeroGuid()
    {
        Guid value;
        do { value = Guid.NewGuid(); } while (value == Guid.Empty);
        return value.ToString("N").ToLowerInvariant();
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static bool IsSupportedLocalPath(string path)
    {
        if (path.StartsWith(@"\\", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal))
            return false;
        string root = Path.GetPathRoot(path);
        if (string.IsNullOrWhiteSpace(root))
            return false;
        DriveInfo drive = new DriveInfo(root);
        return drive.IsReady
            && drive.DriveType != DriveType.Network
            && drive.DriveType != DriveType.CDRom
            && drive.DriveType != DriveType.NoRootDirectory
            && drive.DriveType != DriveType.Unknown;
    }

    private static bool SamePath(string left, string right) =>
        string.Equals(Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

    private static void ReportArtifact(string fileName, string digest) =>
        Console.WriteLine("file={0} sha256={1}", fileName, digest);

    private static void Report(string diagnostic)
    {
        try { Console.Error.WriteLine(diagnostic); }
        catch (Exception) { }
    }
}
