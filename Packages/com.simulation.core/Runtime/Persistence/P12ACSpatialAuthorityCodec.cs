using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>Explicit detached-value codec for the selected P8-A geography owner.</summary>
internal static class P12ACSpatialAuthorityCodec
{
    private const string HexOwnerKey = "p8a-hexes";
    private const string LocationOwnerKey = "p8a-locations";
    private const string ScaleOwnerKey = "p8a-scale-context";

    internal static bool TryEncode(P12CSpatialAuthoritySnapshot snapshot, string aliasGroup,
        out P12AOwnerSection hexSection, out P12AOwnerSection locationSection,
        out P12AOwnerSection scaleSection, out P12AResult result)
    {
        hexSection = null; locationSection = null; scaleSection = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, P12P8AImageSectionContract.HexSectionId);
        if (!IsSupported(snapshot)) return false;
        try
        {
            byte[] hexPayload = EncodeHexes(snapshot);
            byte[] locationPayload = EncodeLocations(snapshot);
            byte[] scalePayload = EncodeScale(snapshot);
            hexSection = Section(P12P8AImageSectionContract.HexSectionId, HexOwnerKey, aliasGroup, snapshot.OwnerRevision,
                P12P8AImageSectionContract.SchemaVersion, hexPayload);
            locationSection = Section(P12P8AImageSectionContract.LocationSectionId, LocationOwnerKey, aliasGroup, snapshot.OwnerRevision,
                P12P8AImageSectionContract.SchemaVersion, locationPayload);
            scaleSection = Section(P12P8AImageSectionContract.ScaleContextSectionId, ScaleOwnerKey, aliasGroup, snapshot.OwnerRevision,
                P12P8AImageSectionContract.SchemaVersion, scalePayload);
            result = null; return true;
        }
        catch (Exception exception) when (exception is P12ALimitException || exception is IOException
            || exception is ArgumentException || exception is OverflowException || exception is EncoderFallbackException)
        { hexSection = null; locationSection = null; scaleSection = null;
          result = new P12AResult(P12AResultCode.InvalidOwnerValue, P12P8AImageSectionContract.HexSectionId); return false; }
    }

    internal static bool TryDecode(P12AOwnerSection hexSection, P12AOwnerSection locationSection,
        P12AOwnerSection scaleSection, out P12CSpatialAuthoritySnapshot snapshot, out P12AResult result)
    {
        snapshot = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue, P12P8AImageSectionContract.HexSectionId);
        if (!IsSection(hexSection, P12P8AImageSectionContract.HexSectionId, HexOwnerKey, P12P8AImageSectionContract.SchemaVersion)
            || !IsSection(locationSection, P12P8AImageSectionContract.LocationSectionId, LocationOwnerKey, P12P8AImageSectionContract.SchemaVersion)
            || !IsSection(scaleSection, P12P8AImageSectionContract.ScaleContextSectionId, ScaleOwnerKey, P12P8AImageSectionContract.SchemaVersion)
            || hexSection.AliasGroup != locationSection.AliasGroup
            || hexSection.AliasGroup != scaleSection.AliasGroup
            || hexSection.Revision != locationSection.Revision || hexSection.Revision != scaleSection.Revision)
            return false;
        try
        {
            if (!TryReadCommon(hexSection, out SpatialHeader hexHeader)
                || !TryReadCommon(locationSection, out SpatialHeader locationHeader)
                || !TryReadCommon(scaleSection, out SpatialHeader scaleHeader)
                || !hexHeader.Equals(locationHeader) || !hexHeader.Equals(scaleHeader)) return false;
            snapshot = new P12CSpatialAuthoritySnapshot(hexHeader.SchemaId, hexHeader.SchemaVersion,
                hexHeader.Profile, hexHeader.P9ProfileIdentity, hexHeader.P9ProfileSchemaVersion,
                hexHeader.OwnerRevision, hexHeader.CoordinateConventionVersion,
                hexHeader.CoordinateCanonicalOrder, hexHeader.Hexes, locationHeader.Locations, scaleHeader.Scale);
            if (!IsSupported(snapshot)) { snapshot = null; return false; }
            result = null; return true;
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is P12ALimitException
            || exception is IOException || exception is ArgumentException || exception is DecoderFallbackException)
        { snapshot = null; return false; }
    }

    private static bool IsSupported(P12CSpatialAuthoritySnapshot snapshot) => snapshot != null
        && snapshot.SchemaId == P12CSpatialAuthoritySnapshot.CurrentSchemaId
        && snapshot.SchemaVersion == P12CSpatialAuthoritySnapshot.CurrentSchemaVersion
        && snapshot.AdmissionProfile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
        && snapshot.P9ProfileContractIdentity == P12CP9GenesisContract.GeographyProfileContractIdentity
        && snapshot.P9ProfileSchemaVersion == 2 && snapshot.OwnerRevision == 1L
        && snapshot.CoordinateConventionVersion == HexCoordinate.ConventionVersion
        && snapshot.CoordinateCanonicalOrder == HexCoordinate.CanonicalOrder
        && snapshot.Hexes != null && snapshot.Hexes.Count == 1
        && snapshot.Locations != null && snapshot.Locations.Count == 1
        && snapshot.Hexes[0] != null && !string.IsNullOrWhiteSpace(snapshot.Hexes[0].Id)
        && !string.IsNullOrWhiteSpace(snapshot.Hexes[0].TerrainDefinitionId)
        && !string.IsNullOrWhiteSpace(snapshot.Hexes[0].AuthoredRevisionToken)
        && snapshot.Locations[0] != null && !string.IsNullOrWhiteSpace(snapshot.Locations[0].Id)
        && snapshot.Locations[0].AnchorHexId == snapshot.Hexes[0].Id
        && snapshot.ScaleContext != null && !string.IsNullOrWhiteSpace(snapshot.ScaleContext.ResolvedConventionId)
        && !string.IsNullOrWhiteSpace(snapshot.ScaleContext.SourceIdentity)
        && !string.IsNullOrWhiteSpace(snapshot.ScaleContext.SourceVersion)
        && snapshot.ScaleContext.DistancePerNeighborStep > 0m
        && !string.IsNullOrWhiteSpace(snapshot.ScaleContext.Unit);

    private static P12AOwnerSection Section(string id, string ownerKey, string aliasGroup,
        long revision, int schema, byte[] payload)
    {
        P12AOwnerRow row = new P12AOwnerRow(ownerKey, revision, payload);
        return new P12AOwnerSection(id, schema, 0, OwnerSectionRole.Required,
            aliasGroup, revision, new[] { row });
    }

    private static bool IsSection(P12AOwnerSection section, string id, string ownerKey, int schema)
        => section != null && section.Id == id && section.Major == schema && section.Minor == 0
            && section.Role == OwnerSectionRole.Required && section.Rows.Count == 1
            && section.Rows[0].OwnerKey == ownerKey && section.Revision >= 0
            && section.Rows[0].Revision == section.Revision && section.Rows[0].Payload != null;

    private static byte[] EncodeHexes(P12CSpatialAuthoritySnapshot snapshot)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            WriteHeader(writer, snapshot); writer.Write(snapshot.Hexes.Count);
            foreach (P12CSpatialHexSnapshot hex in snapshot.Hexes)
            {
                if (hex == null) throw new ArgumentException("Missing P8-A Hex.");
                P12AWire.WriteString(writer, hex.Id); writer.Write(hex.Q); writer.Write(hex.R);
                P12AWire.WriteString(writer, hex.TerrainDefinitionId); P12AWire.WriteString(writer, hex.AuthoredRevisionToken);
            }
            writer.Flush(); return stream.ToArray();
        }
    }

    /// <summary>
    /// Computes the compatibility identity for the exact selected P8-A value
    /// snapshot, using the same explicit owner payload encodings as the image.
    /// </summary>
    internal static bool TryComputeFingerprint(P12CSpatialAuthoritySnapshot snapshot,
        out string fingerprint)
    {
        fingerprint = null;
        if (!IsSupported(snapshot)) return false;
        try
        {
            byte[][] payloads = { EncodeHexes(snapshot), EncodeLocations(snapshot), EncodeScale(snapshot) };
            string[] ids = { P12P8AImageSectionContract.HexSectionId, P12P8AImageSectionContract.LocationSectionId, P12P8AImageSectionContract.ScaleContextSectionId };
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                P12AWire.WriteString(writer, "p8a-selected-geography/v1");
                writer.Write(ids.Length);
                for (int i = 0; i < ids.Length; i++)
                {
                    P12AWire.WriteString(writer, ids[i]);
                    writer.Write(payloads[i].Length);
                    writer.Write(payloads[i]);
                }
                writer.Flush();
                byte[] digest;
                using (SHA256 sha = SHA256.Create()) digest = sha.ComputeHash(stream.ToArray());
                StringBuilder hex = new StringBuilder(digest.Length * 2);
                foreach (byte value in digest) hex.Append(value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                fingerprint = hex.ToString();
                return true;
            }
        }
        catch (Exception exception) when (exception is P12ALimitException || exception is IOException
            || exception is ArgumentException || exception is OverflowException || exception is EncoderFallbackException)
        {
            fingerprint = null;
            return false;
        }
    }

    private static byte[] EncodeLocations(P12CSpatialAuthoritySnapshot snapshot)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            WriteHeader(writer, snapshot); writer.Write(snapshot.Locations.Count);
            foreach (P12CSpatialLocationSnapshot location in snapshot.Locations)
            {
                if (location == null) throw new ArgumentException("Missing P8-A Location.");
                P12AWire.WriteString(writer, location.Id); P12AWire.WriteString(writer, location.AnchorHexId);
            }
            writer.Flush(); return stream.ToArray();
        }
    }

    private static byte[] EncodeScale(P12CSpatialAuthoritySnapshot snapshot)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            WriteHeader(writer, snapshot); writer.Write(1); P12CSpatialScaleSnapshot scale = snapshot.ScaleContext;
            P12AWire.WriteString(writer, scale.ResolvedConventionId); P12AWire.WriteString(writer, scale.SourceIdentity);
            P12AWire.WriteString(writer, scale.SourceVersion);
            foreach (int part in decimal.GetBits(scale.DistancePerNeighborStep)) writer.Write(part);
            P12AWire.WriteString(writer, scale.Unit); writer.Flush(); return stream.ToArray();
        }
    }

    private static void WriteHeader(BinaryWriter writer, P12CSpatialAuthoritySnapshot snapshot)
    {
        P12AWire.WriteString(writer, snapshot.SchemaId); writer.Write(snapshot.SchemaVersion);
        writer.Write((ushort)snapshot.AdmissionProfile); P12AWire.WriteString(writer, snapshot.P9ProfileContractIdentity);
        writer.Write(snapshot.P9ProfileSchemaVersion); writer.Write(snapshot.OwnerRevision);
        P12AWire.WriteString(writer, snapshot.CoordinateConventionVersion);
        P12AWire.WriteString(writer, snapshot.CoordinateCanonicalOrder);
    }

    private static bool TryReadCommon(P12AOwnerSection section, out SpatialHeader header)
    {
        header = null;
        using (MemoryStream stream = new MemoryStream(section.Rows[0].Payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!ReadHeader(reader, out header)) return false;
            int count = reader.ReadInt32(); if (count != 1) return false;
            if (section.Id == P12P8AImageSectionContract.HexSectionId)
            {
                string id = Required(reader);
                int q = reader.ReadInt32(), r = reader.ReadInt32();
                string terrain = Required(reader), authoredRevision = Required(reader);
                if (stream.Position != stream.Length || string.IsNullOrWhiteSpace(id)) return false;
                header.Hexes = new[] { new P12CSpatialHexSnapshot(id, q, r, terrain, authoredRevision) };
                return true;
            }
            if (section.Id == P12P8AImageSectionContract.LocationSectionId)
            {
                string id = Required(reader), anchor = Required(reader);
                if (stream.Position != stream.Length || string.IsNullOrWhiteSpace(id)) return false;
                header.Locations = new[] { new P12CSpatialLocationSnapshot(id, anchor) }; return true;
            }
            string convention = Required(reader), source = Required(reader), version = Required(reader);
            int[] bits = { reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32() };
            decimal distance = new decimal(bits); string unit = Required(reader);
            if (stream.Position != stream.Length) return false;
            header.Scale = new P12CSpatialScaleSnapshot(convention, source, version, distance, unit);
            return true;
        }
    }

    private static bool ReadHeader(BinaryReader reader, out SpatialHeader header)
    {
        header = null;
        string schema = Required(reader); int schemaVersion = reader.ReadInt32();
        ushort profileValue = reader.ReadUInt16();
        if (profileValue != (ushort)SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1) return false;
        string p9 = Required(reader); int p9Version = reader.ReadInt32(); long ownerRevision = reader.ReadInt64();
        string coordinate = Required(reader), canonicalOrder = Required(reader);
        header = new SpatialHeader(schema, schemaVersion, (SimulationRuntimeAdmissionProfile)profileValue,
            p9, p9Version, ownerRevision, coordinate, canonicalOrder);
        return schema == P12CSpatialAuthoritySnapshot.CurrentSchemaId
            && schemaVersion == P12CSpatialAuthoritySnapshot.CurrentSchemaVersion
            && p9 == P12CP9GenesisContract.GeographyProfileContractIdentity && p9Version == 2
            && ownerRevision == 1L && coordinate == HexCoordinate.ConventionVersion
            && canonicalOrder == HexCoordinate.CanonicalOrder;
    }

    private static string Required(BinaryReader reader)
    { if (!P12AWire.ReadString(reader, out string value) || string.IsNullOrWhiteSpace(value)) throw new InvalidDataException(); return value; }

    private sealed class SpatialHeader
    {
        internal SpatialHeader(string schemaId, int schemaVersion, SimulationRuntimeAdmissionProfile profile,
            string p9, int p9Version, long revision, string coordinate, string order)
        { SchemaId = schemaId; SchemaVersion = schemaVersion; Profile = profile; P9ProfileIdentity = p9;
          P9ProfileSchemaVersion = p9Version; OwnerRevision = revision; CoordinateConventionVersion = coordinate;
          CoordinateCanonicalOrder = order; }
        internal string SchemaId { get; }
        internal int SchemaVersion { get; }
        internal SimulationRuntimeAdmissionProfile Profile { get; }
        internal string P9ProfileIdentity { get; }
        internal int P9ProfileSchemaVersion { get; }
        internal long OwnerRevision { get; }
        internal string CoordinateConventionVersion { get; }
        internal string CoordinateCanonicalOrder { get; }
        internal IReadOnlyList<P12CSpatialHexSnapshot> Hexes { get; set; }
        internal IReadOnlyList<P12CSpatialLocationSnapshot> Locations { get; set; }
        internal P12CSpatialScaleSnapshot Scale { get; set; }
        internal bool Equals(SpatialHeader other) => other != null && SchemaId == other.SchemaId
            && SchemaVersion == other.SchemaVersion && Profile == other.Profile
            && P9ProfileIdentity == other.P9ProfileIdentity && P9ProfileSchemaVersion == other.P9ProfileSchemaVersion
            && OwnerRevision == other.OwnerRevision && CoordinateConventionVersion == other.CoordinateConventionVersion
            && CoordinateCanonicalOrder == other.CoordinateCanonicalOrder;
    }
}
