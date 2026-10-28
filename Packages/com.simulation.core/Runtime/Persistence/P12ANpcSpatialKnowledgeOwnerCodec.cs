using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Detached values from both sections owned by one NPC SpatialKnowledgeRuntime.</summary>
internal sealed class P12ANpcSpatialKnowledgeOwnerValue
{
    private readonly IReadOnlyList<string> locationIds;
    private readonly IReadOnlyList<string> routeIds;

    internal P12ANpcSpatialKnowledgeOwnerValue(string runtimeId,
        IEnumerable<string> knownLocationIds, IEnumerable<string> knownRouteIds, long revision)
    {
        RuntimeId = runtimeId;
        if (knownLocationIds == null || knownRouteIds == null)
            throw new ArgumentNullException(knownLocationIds == null ? nameof(knownLocationIds) : nameof(knownRouteIds));
        locationIds = Copy(knownLocationIds);
        routeIds = Copy(knownRouteIds);
        Revision = revision;
    }

    internal string RuntimeId { get; }
    internal IReadOnlyList<string> LocationIds => locationIds;
    internal IReadOnlyList<string> RouteIds => routeIds;
    internal long Revision { get; }

    private static IReadOnlyList<string> Copy(IEnumerable<string> source)
    {
        List<string> values = new List<string>();
        foreach (string value in source)
        {
            if (values.Count == P12AContinuationCodec.MaxRows)
                throw new ArgumentException("Spatial Knowledge row count exceeds the format bound.");
            values.Add(value);
        }
        return values.AsReadOnly();
    }
}

/// <summary>Explicit Daily-v1 codecs for the two NPC SpatialKnowledge census sections.</summary>
internal static class P12ANpcSpatialKnowledgeOwnerCodec
{
    private const string PayloadContract = "p12a.npc-spatial-knowledge/v1";

    internal static bool TryEncode(P12DNpcFRow source,
        out P12AOwnerSection locations, out P12AOwnerSection routes, out P12AResult result)
    {
        locations = null;
        routes = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix, source?.RuntimeId);
        if (source == null || source.KnownLocationRuntimeIds == null || source.KnownRouteRuntimeIds == null)
            return false;
        if (source.KnownLocationRuntimeIds.Count > P12AContinuationCodec.MaxRows
            || source.KnownRouteRuntimeIds.Count > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix, source.RuntimeId);
            return false;
        }
        return TryEncode(new P12ANpcSpatialKnowledgeOwnerValue(source.RuntimeId,
            source.KnownLocationRuntimeIds, source.KnownRouteRuntimeIds,
            source.SpatialKnowledgeRevision), out locations, out routes, out result);
    }

    internal static bool TryEncode(P12ANpcSpatialKnowledgeOwnerValue value,
        out P12AOwnerSection locations, out P12AOwnerSection routes, out P12AResult result)
    {
        locations = null;
        routes = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix, value?.RuntimeId);
        if (!TryValidate(value, out string locationsId, out string routesId,
            out string locationsAlias, out string routesAlias)) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(locationsId);
            P12AWire.GetEncodedStringByteCount(routesId);
            if (locationsAlias != null) P12AWire.GetEncodedStringByteCount(locationsAlias);
            if (routesAlias != null) P12AWire.GetEncodedStringByteCount(routesAlias);
            if (!TryEncodeRows(value.RuntimeId, value.LocationIds, value.Revision,
                "location", locationsId, out List<P12AOwnerRow> locationRows, out result)
                || !TryEncodeRows(value.RuntimeId, value.RouteIds, value.Revision,
                    "route", routesId, out List<P12AOwnerRow> routeRows, out result)) return false;
            locations = new P12AOwnerSection(locationsId, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
                OwnerSectionRole.Required, locationsAlias, value.Revision, locationRows);
            routes = new P12AOwnerSection(routesId, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
                OwnerSectionRole.Required, routesAlias, value.Revision, routeRows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            locations = null;
            routes = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                locationsId, value.RuntimeId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection locations, P12AOwnerSection routes,
        out P12ANpcSpatialKnowledgeOwnerValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            locations?.Id ?? DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix);
        if (!TryDecodeSection(locations, DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix,
                "location", out string locationRuntimeId, out IReadOnlyList<string> locationIds,
                out long locationRevision, out result)
            || !TryDecodeSection(routes, DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix,
                "route", out string routeRuntimeId, out IReadOnlyList<string> routeIds,
                out long routeRevision, out result)) return false;
        if (locationRuntimeId != routeRuntimeId || locationRevision != routeRevision)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, locations.Id, locationRuntimeId);
            return false;
        }
        // Both sections share one monotonic revision. Each newly discovered ID
        // advances it once, so the combined paired cardinality must be reachable.
        if ((long)locationIds.Count + routeIds.Count > locationRevision)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, locations.Id, locationRuntimeId);
            return false;
        }
        value = new P12ANpcSpatialKnowledgeOwnerValue(locationRuntimeId,
            locationIds, routeIds, locationRevision);
        result = null;
        return true;
    }

    private static bool TryEncodeRows(string runtimeId, IReadOnlyList<string> ids,
        long revision, string keyKind, string sectionId, out List<P12AOwnerRow> rows,
        out P12AResult result)
    {
        rows = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, sectionId, runtimeId);
        if (ids == null || ids.Count > P12AContinuationCodec.MaxRows)
        {
            if (ids?.Count > P12AContinuationCodec.MaxRows)
                result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, runtimeId);
            return false;
        }
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        List<P12AOwnerRow> values = new List<P12AOwnerRow>(ids.Count);
        long encodedRowsLength = sizeof(int);
        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) return false;
            P12AWire.GetEncodedStringByteCount(id);
            byte[] payload = EncodeRow(runtimeId, id, revision, keyKind);
            values.Add(new P12AOwnerRow(id, revision, payload));
            encodedRowsLength = checked(encodedRowsLength + EncodedRowLength(id, payload.Length));
            if (encodedRowsLength > P12AContinuationCodec.MaxSectionBytes)
            {
                result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, runtimeId);
                return false;
            }
        }
        rows = values;
        result = null;
        return true;
    }

    private static bool TryDecodeSection(P12AOwnerSection section, string prefix, string keyKind,
        out string runtimeId, out IReadOnlyList<string> ids, out long revision, out P12AResult result)
    {
        runtimeId = null;
        ids = null;
        revision = 0L;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, section?.Id ?? prefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id)
            || !section.Id.StartsWith(prefix, StringComparison.Ordinal)
            || section.Major != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.Revision < 0L
            || section.Rows.Count > P12AContinuationCodec.MaxRows
            || section.Rows.Count > section.Revision)
        {
            if (section?.Rows.Count > P12AContinuationCodec.MaxRows)
                result = new P12AResult(P12AResultCode.LimitExceeded, section.Id);
            return false;
        }
        runtimeId = section.Id.Substring(prefix.Length);
        if (!IsRuntimeId(runtimeId)
            || section.Id != prefix + runtimeId
            || section.AliasGroup != DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(section.Id))
        {
            runtimeId = null;
            return false;
        }
        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            if (section.AliasGroup != null) P12AWire.GetEncodedStringByteCount(section.AliasGroup);
            List<string> values = new List<string>(section.Rows.Count);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            long encodedRowsLength = sizeof(int);
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != section.Revision
                    || string.IsNullOrWhiteSpace(row.OwnerKey) || !seen.Add(row.OwnerKey)
                    || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes)
                    return false;
                P12AWire.GetEncodedStringByteCount(row.OwnerKey);
                byte[] payload = row.Payload;
                if (payload == null || payload.Length != row.PayloadLength) return false;
                encodedRowsLength = checked(encodedRowsLength + EncodedRowLength(row.OwnerKey, payload.Length));
                if (encodedRowsLength > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, section.Id, runtimeId);
                    return false;
                }
                if (!TryDecodeRow(payload, runtimeId, row.OwnerKey, row.Revision, keyKind)) return false;
                values.Add(row.OwnerKey);
            }
            ids = values.AsReadOnly();
            revision = section.Revision;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            runtimeId = null;
            ids = null;
            revision = 0L;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                section.Id);
            return false;
        }
    }

    private static byte[] EncodeRow(string runtimeId, string id, long revision, string keyKind)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, keyKind);
            P12AWire.WriteString(writer, id);
            writer.Write(revision);
            writer.Flush();
            if (stream.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return stream.ToArray();
        }
    }

    private static bool TryDecodeRow(byte[] payload, string runtimeId, string ownerKey,
        long expectedRevision, string keyKind)
    {
        using (MemoryStream stream = new MemoryStream(payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract
                || !P12AWire.ReadString(reader, out string payloadRuntimeId) || payloadRuntimeId != runtimeId
                || !P12AWire.ReadString(reader, out string payloadKind) || payloadKind != keyKind
                || !P12AWire.ReadString(reader, out string id) || id != ownerKey) return false;
            long revision = reader.ReadInt64();
            return stream.Position == stream.Length && revision == expectedRevision;
        }
    }

    private static bool TryValidate(P12ANpcSpatialKnowledgeOwnerValue value,
        out string locationsId, out string routesId, out string locationsAlias, out string routesAlias)
    {
        locationsId = null;
        routesId = null;
        locationsAlias = null;
        routesAlias = null;
        if (value == null || !IsRuntimeId(value.RuntimeId) || value.Revision < 0L
            || value.LocationIds == null || value.RouteIds == null
            || value.LocationIds.Count > P12AContinuationCodec.MaxRows
            || value.RouteIds.Count > P12AContinuationCodec.MaxRows
            || (long)value.LocationIds.Count + value.RouteIds.Count > value.Revision) return false;
        locationsId = DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + value.RuntimeId;
        routesId = DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + value.RuntimeId;
        locationsAlias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(locationsId);
        routesAlias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(routesId);
        return !string.IsNullOrWhiteSpace(locationsAlias) && !string.IsNullOrWhiteSpace(routesAlias);
    }

    private static long EncodedRowLength(string ownerKey, int payloadLength)
        => sizeof(int) + (long)P12AWire.GetEncodedStringByteCount(ownerKey)
            + sizeof(long) + sizeof(int) + payloadLength;

    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value) && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is DecoderFallbackException;
}
