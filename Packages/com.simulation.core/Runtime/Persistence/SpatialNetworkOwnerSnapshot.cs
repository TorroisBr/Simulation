using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Detached schema-v1 owner export for the legacy spatial network. Capture-
/// boundary authority and cross-owner validation remain with P12 composition.
/// </summary>
internal sealed class SpatialNetworkOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;

    internal int SchemaVersion { get; }
    internal long Revision { get; }
    internal IReadOnlyList<string> LocationRuntimeIds { get; }
    internal IReadOnlyList<SpatialRouteOwnerSnapshotRecord> Routes { get; }

    internal SpatialNetworkOwnerSnapshot(
        int schemaVersion,
        long revision,
        IEnumerable<string> locationRuntimeIds,
        IEnumerable<SpatialRouteOwnerSnapshotRecord> routes)
    {
        SchemaVersion = schemaVersion;
        Revision = revision;
        LocationRuntimeIds = locationRuntimeIds == null
            ? null
            : new ReadOnlyCollection<string>(new List<string>(locationRuntimeIds));

        if (routes == null)
        {
            Routes = null;
            return;
        }

        List<SpatialRouteOwnerSnapshotRecord> routeCopy = new List<SpatialRouteOwnerSnapshotRecord>();
        foreach (SpatialRouteOwnerSnapshotRecord route in routes)
        {
            routeCopy.Add(route == null
                ? null
                : new SpatialRouteOwnerSnapshotRecord(
                    route.RuntimeId,
                    route.OriginLocationRuntimeId,
                    route.DestinationLocationRuntimeId,
                    route.TravelDays));
        }

        Routes = new ReadOnlyCollection<SpatialRouteOwnerSnapshotRecord>(routeCopy);
    }
}

/// <summary>
/// Exact legacy route values. Validation belongs to the staged owner factory
/// so malformed persisted input is rejected as data instead of throwing here.
/// </summary>
internal sealed class SpatialRouteOwnerSnapshotRecord
{
    internal string RuntimeId { get; }
    internal string OriginLocationRuntimeId { get; }
    internal string DestinationLocationRuntimeId { get; }
    internal int TravelDays { get; }

    internal SpatialRouteOwnerSnapshotRecord(
        string runtimeId,
        string originLocationRuntimeId,
        string destinationLocationRuntimeId,
        int travelDays)
    {
        RuntimeId = runtimeId;
        OriginLocationRuntimeId = originLocationRuntimeId;
        DestinationLocationRuntimeId = destinationLocationRuntimeId;
        TravelDays = travelDays;
    }
}

internal enum SpatialNetworkSnapshotFailureCode
{
    None = 0,
    InvalidSnapshot,
    UnsupportedSnapshotSchema,
    InvalidLocationId,
    DuplicateRuntimeId,
    InvalidRoute,
    MissingRouteEndpoint,
    SelfRoute,
    InvalidTravelDays,
    RuntimeIdentityCollision,
    RuntimeIdentityRevisionExhausted
}

internal sealed class SpatialNetworkSnapshotFailure
{
    internal static readonly SpatialNetworkSnapshotFailure None =
        new SpatialNetworkSnapshotFailure(SpatialNetworkSnapshotFailureCode.None, string.Empty);

    internal SpatialNetworkSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private SpatialNetworkSnapshotFailure(SpatialNetworkSnapshotFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static SpatialNetworkSnapshotFailure Create(
        SpatialNetworkSnapshotFailureCode code,
        string message)
    {
        return code == SpatialNetworkSnapshotFailureCode.None
            ? None
            : new SpatialNetworkSnapshotFailure(code, message);
    }
}
