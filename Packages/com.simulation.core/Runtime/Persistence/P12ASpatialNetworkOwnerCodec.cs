using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>Explicit codecs for the selected Daily-v1 legacy spatial owner.</summary>
internal static class P12ASpatialNetworkOwnerCodec
{
    internal const string LocationsSectionId = "p12d.legacy-spatial-network.locations";
    internal const string RoutesSectionId = "p12d.legacy-spatial-network.routes";
    internal const int SchemaVersion = 1;
    private const string RoutePayloadContract = "p12a.legacy-spatial-route/v1";

    internal static bool TryEncode(SpatialNetworkOwnerSnapshot snapshot,
        out P12AOwnerSection locations, out P12AOwnerSection routes, out P12AResult result)
    {
        locations = null;
        routes = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            LocationsSectionId);
        if (!TryValidate(snapshot, out List<string> locationIds, out result)) return false;

        try
        {
            List<P12AOwnerRow> locationRows = new List<P12AOwnerRow>(locationIds.Count);
            foreach (string id in locationIds)
                locationRows.Add(new P12AOwnerRow(id, snapshot.Revision, Array.Empty<byte>()));

            List<P12AOwnerRow> routeRows = new List<P12AOwnerRow>(snapshot.Routes.Count);
            foreach (SpatialRouteOwnerSnapshotRecord route in snapshot.Routes)
            {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, RoutePayloadContract);
                    P12AWire.WriteString(writer, route.RuntimeId);
                    P12AWire.WriteString(writer, route.OriginLocationRuntimeId);
                    P12AWire.WriteString(writer, route.DestinationLocationRuntimeId);
                    writer.Write(route.TravelDays);
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            RoutesSectionId, route.RuntimeId);
                        return false;
                    }
                    routeRows.Add(new P12AOwnerRow(route.RuntimeId, snapshot.Revision, stream.ToArray()));
                }
            }

            locations = new P12AOwnerSection(LocationsSectionId,
                SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(LocationsSectionId),
                snapshot.Revision, locationRows);
            routes = new P12AOwnerSection(RoutesSectionId,
                SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(RoutesSectionId),
                snapshot.Revision, routeRows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            locations = routes = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                LocationsSectionId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection locations, P12AOwnerSection routes,
        out SpatialNetworkOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            LocationsSectionId);
        if (!IsSection(locations, LocationsSectionId)
            || !IsSection(routes, RoutesSectionId)
            || locations.Revision != routes.Revision
            || !RowsStrictlyAscending(locations)) return false;

        List<string> locationIds = new List<string>(locations.Rows.Count);
        HashSet<string> locationSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerRow row in locations.Rows)
        {
            if (row == null || row.Revision != locations.Revision || row.PayloadLength != 0
                || string.IsNullOrWhiteSpace(row.OwnerKey)) return false;
            try { P12AWire.GetEncodedStringByteCount(row.OwnerKey); }
            catch (Exception exception) when (IsWireException(exception))
            { result = new P12AResult(P12AResultCode.InvalidOwnerValue, locations.Id, row.OwnerKey); return false; }
            if (!locationSet.Add(row.OwnerKey)) return false;
            locationIds.Add(row.OwnerKey);
        }

        List<SpatialRouteOwnerSnapshotRecord> routeRecords = new List<SpatialRouteOwnerSnapshotRecord>(routes.Rows.Count);
        HashSet<string> routeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerRow row in routes.Rows)
        {
            if (row == null || row.Revision != routes.Revision || row.PayloadLength < 0
                || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes
                || !TryReadRoute(row, out SpatialRouteOwnerSnapshotRecord route)
                || row.OwnerKey != route.RuntimeId || !routeIds.Add(route.RuntimeId)
                || !locationSet.Contains(route.OriginLocationRuntimeId)
                || !locationSet.Contains(route.DestinationLocationRuntimeId)
                || route.OriginLocationRuntimeId == route.DestinationLocationRuntimeId
                || route.TravelDays < 1)
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                    routes.Id, row?.OwnerKey);
                return false;
            }
            routeRecords.Add(route);
        }
        foreach (string routeId in routeIds)
            if (locationSet.Contains(routeId))
            { result = new P12AResult(P12AResultCode.InvalidOwnerValue, routes.Id, routeId); return false; }

        SpatialNetworkOwnerSnapshot candidate = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, locations.Revision,
            locationIds, routeRecords);
        if (!TryValidate(candidate, out _, out result)) return false;
        snapshot = candidate;
        result = null;
        return true;
    }

    private static bool TryValidate(SpatialNetworkOwnerSnapshot snapshot,
        out List<string> orderedLocations, out P12AResult result)
    {
        orderedLocations = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            LocationsSectionId);
        if (snapshot == null || snapshot.SchemaVersion != SpatialNetworkOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0 || snapshot.LocationRuntimeIds == null || snapshot.Routes == null
            || snapshot.LocationRuntimeIds.Count > P12AContinuationCodec.MaxRows
            || snapshot.Routes.Count > P12AContinuationCodec.MaxRows
            || snapshot.Revision < snapshot.LocationRuntimeIds.Count + (long)snapshot.Routes.Count)
            return false;

        HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
        List<string> locations = new List<string>(snapshot.LocationRuntimeIds.Count);
        foreach (string id in snapshot.LocationRuntimeIds)
        {
            if (string.IsNullOrWhiteSpace(id) || !identities.Add(id)) return false;
            try { P12AWire.GetEncodedStringByteCount(id); }
            catch (Exception exception) when (IsWireException(exception))
            { result = new P12AResult(P12AResultCode.LimitExceeded, LocationsSectionId); return false; }
            locations.Add(id);
        }
        locations.Sort(StringComparer.Ordinal);
        HashSet<string> locationSet = new HashSet<string>(locations, StringComparer.Ordinal);
        foreach (SpatialRouteOwnerSnapshotRecord route in snapshot.Routes)
        {
            if (route == null || string.IsNullOrWhiteSpace(route.RuntimeId)
                || string.IsNullOrWhiteSpace(route.OriginLocationRuntimeId)
                || string.IsNullOrWhiteSpace(route.DestinationLocationRuntimeId)
                || route.OriginLocationRuntimeId == route.DestinationLocationRuntimeId
                || route.TravelDays < 1 || !identities.Add(route.RuntimeId)
                || !locationSet.Contains(route.OriginLocationRuntimeId)
                || !locationSet.Contains(route.DestinationLocationRuntimeId))
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                    RoutesSectionId, route?.RuntimeId);
                return false;
            }
            try
            {
                P12AWire.GetEncodedStringByteCount(route.RuntimeId);
                P12AWire.GetEncodedStringByteCount(route.OriginLocationRuntimeId);
                P12AWire.GetEncodedStringByteCount(route.DestinationLocationRuntimeId);
            }
            catch (Exception exception) when (IsWireException(exception))
            {
                result = new P12AResult(P12AResultCode.LimitExceeded,
                    RoutesSectionId, route.RuntimeId);
                return false;
            }
        }
        orderedLocations = locations;
        result = null;
        return true;
    }

    private static bool IsSection(P12AOwnerSection section, string id)
        => section != null && section.Id == id
            && section.Major == SchemaVersion && section.Minor == 0
            && section.Role == OwnerSectionRole.Required
            && section.AliasGroup == P12ADailyV1Census.ExpectedAliasGroupForSection(id)
            && section.Revision >= 0 && section.Rows.Count <= P12AContinuationCodec.MaxRows;

    private static bool RowsStrictlyAscending(P12AOwnerSection section)
    {
        string prior = null;
        foreach (P12AOwnerRow row in section.Rows)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.OwnerKey)
                || (prior != null && StringComparer.Ordinal.Compare(prior, row.OwnerKey) >= 0)) return false;
            prior = row.OwnerKey;
        }
        return true;
    }

    private static bool TryReadRoute(P12AOwnerRow row, out SpatialRouteOwnerSnapshotRecord route)
    {
        route = null;
        byte[] payload = row.Payload;
        if (payload == null || payload.Length != row.PayloadLength) return false;
        using (MemoryStream stream = new MemoryStream(payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!ReadString(reader, out string contract) || contract != RoutePayloadContract
                || !ReadString(reader, out string runtimeId)
                || !ReadString(reader, out string origin)
                || !ReadString(reader, out string destination)) return false;
            int travelDays = reader.ReadInt32();
            if (stream.Position != stream.Length) return false;
            route = new SpatialRouteOwnerSnapshotRecord(runtimeId, origin, destination, travelDays);
            return true;
        }
    }

    private static bool ReadString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is EncoderFallbackException || exception is DecoderFallbackException;
}
