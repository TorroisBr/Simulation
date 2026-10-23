using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Builds the .NET host's P12-v2 compatibility identity from the same admitted
/// typed Daily-v1 definitions consumed by Core genesis. Host-specific facts are
/// supplied separately and the P8 identity uses the promoted snapshot framing.
/// </summary>
internal static class DailyV1CompatibilityDescriptorFactory
{
    private const string ProfileId = "UnityBootstrap-Daily-v1";
    private const int ProfileVersion = 1;
    private const string GenesisProfileContract = "unity-authored-bootstrap/genesis-v1";
    private const string GeographyProfileContract = "unity-authored-bootstrap/authored-geography-v1";
    private const string GeographyStage = "p9.genesis.authored-geography/v1";
    private const string GeographySchemaId = "p12c-spatial-authority-geography";
    private const int GeographySchemaVersion = 1;
    private const int P9GeographySchemaVersion = 2;

    internal static bool TryCreateDotNetV2(
        DailyV1EffectiveDefinitions definitions,
        DailyV1HostEnvironmentIdentity hostIdentity,
        out P12ACompatibilityDescriptorV2 descriptor,
        out string diagnostic)
    {
        descriptor = null;
        diagnostic = "The selected Daily-v1 definitions do not match the admitted authored-geography profile.";
        if (definitions == null || hostIdentity == null) return false;

        try
        {
            if (!IsExactSelectedProfile(definitions, out diagnostic)
                || !TryComputeP8Fingerprint(definitions, out string p8Fingerprint))
                return false;

            string p9Fingerprint = DailyV1DefinitionIdentity.CreateP9Fingerprint(
                definitions, out _);
            if (!string.Equals(definitions.ContentFingerprint,
                    definitions.ContentProjection.Fingerprint, StringComparison.Ordinal)
                || !DailyV1CompatibilityFingerprint.TryCreate(
                    definitions,
                    p9Fingerprint,
                    DailyV1SelectedOwnerSectionCensusContract.FixedRequired,
                    DailyV1SelectedOwnerSectionCensusContract.FixedEmpty,
                    DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes,
                    DailyV1SelectedOwnerSectionCensusContract.CityPrefixes,
                    p9Fingerprint,
                    p8Fingerprint,
                    out DailyV1CompatibilityFingerprint semanticIdentity))
            {
                diagnostic = "The selected Daily-v1 semantic identity is incomplete.";
                return false;
            }

            descriptor = semanticIdentity.CreateDotNetV2Descriptor(hostIdentity);
            if (descriptor.HostKind != hostIdentity.HostKind
                || descriptor.ProfileId != ProfileId
                || descriptor.ProfileVersion != ProfileVersion)
            {
                descriptor = null;
                diagnostic = "The generated P12-v2 descriptor does not match the fixed Daily-v1 host profile.";
                return false;
            }

            diagnostic = null;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException
            || exception is FormatException
            || exception is P12ALimitException
            || exception is CryptographicException
            || exception is IOException
            || exception is EncoderFallbackException)
        {
            descriptor = null;
            diagnostic = "The selected Daily-v1 compatibility identity is invalid.";
            return false;
        }
    }

    private static bool IsExactSelectedProfile(
        DailyV1EffectiveDefinitions definitions,
        out string diagnostic)
    {
        diagnostic = "The selected Daily-v1 definitions do not match the admitted authored-geography profile.";
        DailyV1SimulationConfigData config = definitions.Configuration;
        if (config == null || definitions.Calendar == null || definitions.EffectiveConfiguration == null
            || definitions.ContentProjection == null
            || string.IsNullOrWhiteSpace(definitions.ContentFingerprint)
            || !config.useAuthoredGeographyProfile
            || definitions.ProfileContractIdentity != GenesisProfileContract
            || definitions.GeographyProfileContractIdentity != GeographyProfileContract
            || definitions.GeographyStageIdentity != GeographyStage
            || definitions.HexCoordinateConvention != HexCoordinate.ConventionVersion
            || definitions.HexCoordinateCanonicalOrder != HexCoordinate.CanonicalOrder
            || (config.explorableSites?.Count ?? 0) != 0
            || definitions.ExplorableSites.Count != 0
            || !string.IsNullOrWhiteSpace(config.authoredP10RuinSite)
            || !string.IsNullOrWhiteSpace(config.p10bStableSiteKey))
            return false;

        string[] expectedStages =
        {
            "p9.genesis.resolve-profile/v1",
            "p9.genesis.authored-world/v1",
            GeographyStage,
            "p9.genesis.authored-actors/v1",
            "p9.genesis.validate-profile/v1",
            "p9.genesis.publish/v1"
        };
        if (definitions.StageOrder == null || definitions.StageOrder.Count != expectedStages.Length)
            return false;
        for (int i = 0; i < expectedStages.Length; i++)
            if (!string.Equals(definitions.StageOrder[i], expectedStages[i], StringComparison.Ordinal))
                return false;

        if (string.IsNullOrWhiteSpace(config.authoredHexId)
            || string.IsNullOrWhiteSpace(config.authoredTerrainDefinitionId)
            || string.IsNullOrWhiteSpace(config.authoredTerrainRevisionToken)
            || string.IsNullOrWhiteSpace(config.authoredLocationId)
            || string.IsNullOrWhiteSpace(config.authoredScaleConventionId)
            || string.IsNullOrWhiteSpace(config.authoredScaleSourceIdentity)
            || string.IsNullOrWhiteSpace(config.authoredScaleSourceVersion)
            || string.IsNullOrWhiteSpace(config.authoredDistancePerNeighborStep)
            || string.IsNullOrWhiteSpace(config.authoredScaleUnit))
            return false;

        diagnostic = null;
        return true;
    }

    private static bool TryComputeP8Fingerprint(
        DailyV1EffectiveDefinitions definitions,
        out string fingerprint)
    {
        fingerprint = null;
        DailyV1SimulationConfigData config = definitions.Configuration;
        if (!decimal.TryParse(config.authoredDistancePerNeighborStep,
                NumberStyles.Number, CultureInfo.InvariantCulture, out decimal distance)
            || distance <= 0m)
            return false;
        byte[][] payloads =
        {
            EncodeHex(config, definitions),
            EncodeLocation(config, definitions),
            EncodeScale(config, definitions, distance)
        };
        string[] ids = { "p8a.hexes", "p8a.locations", "p8a.scale-context" };
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
        {
            ContinuationWire.WriteString(writer, "p8a-selected-geography/v1");
            writer.Write(ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                ContinuationWire.WriteString(writer, ids[i]);
                writer.Write(payloads[i].Length);
                writer.Write(payloads[i]);
            }
            writer.Flush();
            using (SHA256 sha = SHA256.Create())
                fingerprint = Hex(sha.ComputeHash(stream.ToArray()));
        }
        return true;
    }

    private static byte[] EncodeHex(DailyV1SimulationConfigData config,
        DailyV1EffectiveDefinitions definitions)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
        {
            WriteGeographyHeader(writer, definitions);
            writer.Write(1);
            ContinuationWire.WriteString(writer, config.authoredHexId);
            writer.Write(config.authoredHexQ);
            writer.Write(config.authoredHexR);
            ContinuationWire.WriteString(writer, config.authoredTerrainDefinitionId);
            ContinuationWire.WriteString(writer, config.authoredTerrainRevisionToken);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static byte[] EncodeLocation(DailyV1SimulationConfigData config,
        DailyV1EffectiveDefinitions definitions)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
        {
            WriteGeographyHeader(writer, definitions);
            writer.Write(1);
            ContinuationWire.WriteString(writer, config.authoredLocationId);
            ContinuationWire.WriteString(writer, config.authoredHexId);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static byte[] EncodeScale(DailyV1SimulationConfigData config,
        DailyV1EffectiveDefinitions definitions, decimal distance)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
        {
            WriteGeographyHeader(writer, definitions);
            writer.Write(1);
            ContinuationWire.WriteString(writer, config.authoredScaleConventionId);
            ContinuationWire.WriteString(writer, config.authoredScaleSourceIdentity);
            ContinuationWire.WriteString(writer, config.authoredScaleSourceVersion);
            foreach (int part in decimal.GetBits(distance)) writer.Write(part);
            ContinuationWire.WriteString(writer, config.authoredScaleUnit);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static void WriteGeographyHeader(BinaryWriter writer,
        DailyV1EffectiveDefinitions definitions)
    {
        ContinuationWire.WriteString(writer, GeographySchemaId);
        writer.Write(GeographySchemaVersion);
        writer.Write((ushort)SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        ContinuationWire.WriteString(writer, definitions.GeographyProfileContractIdentity);
        writer.Write(P9GeographySchemaVersion);
        writer.Write(1L);
        ContinuationWire.WriteString(writer, definitions.HexCoordinateConvention);
        ContinuationWire.WriteString(writer, definitions.HexCoordinateCanonicalOrder);
    }

    private static string Hex(byte[] bytes)
    {
        StringBuilder result = new StringBuilder(bytes.Length * 2);
        foreach (byte value in bytes)
            result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return result.ToString();
    }
}
