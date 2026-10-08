using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

[Serializable]
public sealed class SpatialLocationRuntime
{
    private readonly string runtimeId;

    public string RuntimeId => runtimeId;

    public SpatialLocationRuntime(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("SpatialLocationRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

        this.runtimeId = runtimeId;
    }
}

[Serializable]
public sealed class SpatialRouteRuntime
{
    private readonly string runtimeId;
    private readonly SpatialLocationRuntime origin;
    private readonly SpatialLocationRuntime destination;
    private readonly int travelDays;

    public string RuntimeId => runtimeId;
    public SpatialLocationRuntime Origin => origin;
    public SpatialLocationRuntime Destination => destination;
    public int TravelDays => travelDays;

    public SpatialRouteRuntime(
        string runtimeId,
        SpatialLocationRuntime origin,
        SpatialLocationRuntime destination,
        int travelDays)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("SpatialRouteRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

        if (origin == null)
        {
            throw new ArgumentNullException(nameof(origin));
        }

        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        if (origin == destination)
        {
            throw new ArgumentException("SpatialRouteRuntime cannot connect a location to itself.", nameof(destination));
        }

        this.runtimeId = runtimeId;
        this.origin = origin;
        this.destination = destination;
        this.travelDays = Math.Max(1, travelDays);
    }
}

/// <summary>
/// Detached schema-v1 owner export for the legacy spatial network. This is
/// owner data only; capture-boundary authority and cross-owner validation are
/// supplied by the surrounding P12 composition.
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

public sealed class SpatialNetworkRuntime
{
    private readonly RuntimeIdentityRegistry identityRegistry;
    private readonly SimulationLogger logger;
    private readonly HashSet<SpatialLocationRuntime> locations = new HashSet<SpatialLocationRuntime>();
    private readonly List<SpatialRouteRuntime> routes = new List<SpatialRouteRuntime>();
    private readonly Dictionary<SpatialLocationRuntime, List<SpatialRouteRuntime>> outgoingRoutes = new Dictionary<SpatialLocationRuntime, List<SpatialRouteRuntime>>();
    private long revision;

    public IEnumerable<SpatialLocationRuntime> Locations
    {
        get
        {
            SpatialLocationRuntime[] snapshot = new SpatialLocationRuntime[locations.Count];
            locations.CopyTo(snapshot);
            return Array.AsReadOnly(snapshot);
        }
    }

    public IReadOnlyList<SpatialRouteRuntime> Routes => Array.AsReadOnly(routes.ToArray());
    public int LocationCount => locations.Count;
    public int RouteCount => routes.Count;
    public long Revision => revision;

    public SpatialNetworkRuntime(RuntimeIdentityRegistry identityRegistry, SimulationLogger logger = null)
    {
        this.identityRegistry = identityRegistry ?? throw new ArgumentNullException(nameof(identityRegistry));
        this.logger = logger ?? new SimulationLogger(null);
    }

    internal bool UsesIdentityRegistry(RuntimeIdentityRegistry candidate)
    {
        return ReferenceEquals(identityRegistry, candidate);
    }

    /// <summary>
    /// Copies exact legacy location and route identities/order and the local
    /// revision. Callers bind this operation to the P12-B capture authority.
    /// </summary>
    internal SpatialNetworkOwnerSnapshot CaptureOwnerSnapshot()
    {
        SpatialLocationRuntime[] locationSnapshot = new SpatialLocationRuntime[locations.Count];
        locations.CopyTo(locationSnapshot);

        List<string> locationIds = new List<string>(locationSnapshot.Length);
        foreach (SpatialLocationRuntime location in locationSnapshot)
        {
            locationIds.Add(location.RuntimeId);
        }

        List<SpatialRouteOwnerSnapshotRecord> routeRecords = new List<SpatialRouteOwnerSnapshotRecord>(routes.Count);
        foreach (SpatialRouteRuntime route in routes)
        {
            routeRecords.Add(new SpatialRouteOwnerSnapshotRecord(
                route.RuntimeId,
                route.Origin.RuntimeId,
                route.Destination.RuntimeId,
                route.TravelDays));
        }

        return new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
            revision,
            locationIds,
            routeRecords);
    }

    /// <summary>
    /// Builds an unpublished exact-value legacy spatial owner. The supplied
    /// identity registry must itself be part of an unpublished staged
    /// composition and must be discarded if staging fails.
    /// </summary>
    internal static bool TryCreateFromOwnerSnapshot(
        SpatialNetworkOwnerSnapshot snapshot,
        RuntimeIdentityRegistry stagedIdentityRegistry,
        out SpatialNetworkRuntime stagedNetwork,
        out SpatialNetworkSnapshotFailure failure)
    {
        stagedNetwork = null;
        if (snapshot == null || stagedIdentityRegistry == null)
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.InvalidSnapshot,
                "A spatial owner snapshot and unpublished identity registry are required.");
            return false;
        }

        if (snapshot.SchemaVersion != SpatialNetworkOwnerSnapshot.CurrentSchemaVersion)
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.UnsupportedSnapshotSchema,
                "The spatial owner snapshot schema is not supported.");
            return false;
        }

        if (snapshot.LocationRuntimeIds == null || snapshot.Routes == null || snapshot.Revision < 0)
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.InvalidSnapshot,
                "The spatial owner snapshot is missing required collections or has a negative revision.");
            return false;
        }

        long minimumRevision = (long)snapshot.LocationRuntimeIds.Count + snapshot.Routes.Count;
        if (snapshot.Revision < minimumRevision)
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.InvalidSnapshot,
                "The spatial owner revision cannot account for its registered locations and routes.");
            return false;
        }

        HashSet<string> allRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < snapshot.LocationRuntimeIds.Count; index++)
        {
            string locationId = snapshot.LocationRuntimeIds[index];
            if (string.IsNullOrWhiteSpace(locationId))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.InvalidLocationId,
                    "The spatial owner snapshot contains an empty location identity.");
                return false;
            }

            if (!allRuntimeIds.Add(locationId) || !locationIds.Add(locationId))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.DuplicateRuntimeId,
                    "The spatial owner snapshot contains a duplicate runtime identity.");
                return false;
            }
        }

        for (int index = 0; index < snapshot.Routes.Count; index++)
        {
            SpatialRouteOwnerSnapshotRecord route = snapshot.Routes[index];
            if (route == null
                || string.IsNullOrWhiteSpace(route.RuntimeId)
                || string.IsNullOrWhiteSpace(route.OriginLocationRuntimeId)
                || string.IsNullOrWhiteSpace(route.DestinationLocationRuntimeId))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.InvalidRoute,
                    "The spatial owner snapshot contains a null route or empty route identity/reference.");
                return false;
            }

            if (!allRuntimeIds.Add(route.RuntimeId))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.DuplicateRuntimeId,
                    "The spatial owner snapshot contains a duplicate runtime identity.");
                return false;
            }

            if (!locationIds.Contains(route.OriginLocationRuntimeId)
                || !locationIds.Contains(route.DestinationLocationRuntimeId))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.MissingRouteEndpoint,
                    "Every legacy route endpoint must refer to a registered snapshot location.");
                return false;
            }

            if (string.Equals(route.OriginLocationRuntimeId, route.DestinationLocationRuntimeId, StringComparison.Ordinal))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.SelfRoute,
                    "A legacy route cannot connect a location to itself.");
                return false;
            }

            if (route.TravelDays < 1)
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.InvalidTravelDays,
                    "A legacy route must preserve a positive normalized travel duration.");
                return false;
            }
        }

        foreach (string runtimeId in allRuntimeIds)
        {
            if (!stagedIdentityRegistry.IsRuntimeIdAvailable(runtimeId))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision,
                    "A spatial runtime identity is already occupied in the staged identity registry.");
                return false;
            }
        }

        long identityCount = allRuntimeIds.Count;
        if (stagedIdentityRegistry.CensusRevision > long.MaxValue - identityCount)
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.RuntimeIdentityRevisionExhausted,
                "The staged runtime identity registry cannot represent all spatial owner identities.");
            return false;
        }

        SpatialNetworkRuntime staged = new SpatialNetworkRuntime(stagedIdentityRegistry);
        Dictionary<string, SpatialLocationRuntime> stagedLocations =
            new Dictionary<string, SpatialLocationRuntime>(StringComparer.Ordinal);
        foreach (string locationId in snapshot.LocationRuntimeIds)
        {
            SpatialLocationRuntime location = new SpatialLocationRuntime(locationId);
            if (!stagedIdentityRegistry.RegisterLocation(location))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision,
                    "The staged identity registry rejected a validated legacy location identity.");
                return false;
            }

            staged.locations.Add(location);
            staged.outgoingRoutes.Add(location, new List<SpatialRouteRuntime>());
            stagedLocations.Add(locationId, location);
        }

        foreach (SpatialRouteOwnerSnapshotRecord routeRecord in snapshot.Routes)
        {
            SpatialRouteRuntime route = new SpatialRouteRuntime(
                routeRecord.RuntimeId,
                stagedLocations[routeRecord.OriginLocationRuntimeId],
                stagedLocations[routeRecord.DestinationLocationRuntimeId],
                routeRecord.TravelDays);
            if (!stagedIdentityRegistry.RegisterRoute(route))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision,
                    "The staged identity registry rejected a validated legacy route identity.");
                return false;
            }

            staged.routes.Add(route);
            staged.outgoingRoutes[route.Origin].Add(route);
        }

        staged.revision = snapshot.Revision;
        stagedNetwork = staged;
        failure = SpatialNetworkSnapshotFailure.None;
        return true;
    }

    public bool RegisterLocation(SpatialLocationRuntime location)
    {
        return RegisterLocationCore(location, null);
    }

    internal bool RegisterLocationForP10Genesis(SpatialLocationRuntime location, Action<string> completedMutation)
    {
        return RegisterLocationCore(location, completedMutation);
    }

    private bool RegisterLocationCore(SpatialLocationRuntime location, Action<string> completedMutation)
    {
        if (location == null)
        {
            logger.LogError("Cannot register spatial location: runtime instance is null.");
            return false;
        }

        if (locations.Contains(location) == true)
        {
            logger.LogError($"Spatial location '{location.RuntimeId}' is already registered in this network.");
            return false;
        }

        if (CanAdvanceRevision("spatial location") == false)
        {
            return false;
        }

        bool identityRegistered = false;
        bool networkLocationAdded = false;
        bool outgoingRoutesAdded = false;
        bool revisionAdvanced = false;
        try
        {
            bool identityAdded = completedMutation == null
                ? identityRegistry.RegisterLocation(location)
                : identityRegistry.RegisterLocationForP10Genesis(location, completedMutation);
            if (!identityAdded) return false;
            identityRegistered = true;
            completedMutation?.Invoke("SpatialNetwork.Location.IdentityRegistered");

            if (!locations.Add(location))
                throw new InvalidOperationException("Spatial location became registered during genesis publication.");
            networkLocationAdded = true;
            completedMutation?.Invoke("SpatialNetwork.Location.NetworkIndex");

            outgoingRoutes.Add(location, new List<SpatialRouteRuntime>());
            outgoingRoutesAdded = true;
            completedMutation?.Invoke("SpatialNetwork.Location.OutgoingIndex");

            revision++;
            revisionAdvanced = true;
            completedMutation?.Invoke("SpatialNetwork.Location.Revision");
            return true;
        }
        catch
        {
            if (revisionAdvanced) revision--;
            if (outgoingRoutesAdded) outgoingRoutes.Remove(location);
            if (networkLocationAdded) locations.Remove(location);
            if (identityRegistered) identityRegistry.RollbackGenesisLocation(location);
            throw;
        }
    }

    internal void RollbackGenesisLocation(SpatialLocationRuntime location)
    {
        if (location == null || !locations.Contains(location) || revision <= 0
            || (outgoingRoutes.TryGetValue(location, out List<SpatialRouteRuntime> outgoing) && outgoing.Count != 0)
            || !identityRegistry.TryGetLocation(location.RuntimeId, out SpatialLocationRuntime current)
            || !ReferenceEquals(location, current))
            throw new InvalidOperationException("Cannot roll back the P10-B spatial Location insertion.");
        locations.Remove(location);
        outgoingRoutes.Remove(location);
        identityRegistry.RollbackGenesisLocation(location);
        revision--;
    }

    public bool RegisterRoute(SpatialRouteRuntime route)
    {
        if (route == null)
        {
            logger.LogError("Cannot register spatial route: runtime instance is null.");
            return false;
        }

        if (locations.Contains(route.Origin) == false || locations.Contains(route.Destination) == false)
        {
            logger.LogError($"Cannot register spatial route '{route.RuntimeId}': origin and destination must both be registered locations.");
            return false;
        }

        if (CanAdvanceRevision("spatial route") == false)
        {
            return false;
        }

        if (identityRegistry.RegisterRoute(route) == false)
        {
            return false;
        }

        routes.Add(route);
        outgoingRoutes[route.Origin].Add(route);
        revision++;
        return true;
    }

    public bool TryGetLocation(string runtimeId, out SpatialLocationRuntime location)
    {
        return identityRegistry.TryGetLocation(runtimeId, out location);
    }

    public bool TryGetRoute(string runtimeId, out SpatialRouteRuntime route)
    {
        return identityRegistry.TryGetRoute(runtimeId, out route);
    }

    public IReadOnlyList<SpatialRouteRuntime> GetOutgoingRoutes(SpatialLocationRuntime origin)
    {
        if (origin != null && outgoingRoutes.TryGetValue(origin, out List<SpatialRouteRuntime> foundRoutes) == true)
        {
            return Array.AsReadOnly(foundRoutes.ToArray());
        }

        return Array.Empty<SpatialRouteRuntime>();
    }

    public bool TryGetSingleDirectRoute(
        SpatialLocationRuntime origin,
        SpatialLocationRuntime destination,
        out SpatialRouteRuntime route)
    {
        route = null;

        if (origin == null || destination == null || outgoingRoutes.TryGetValue(origin, out List<SpatialRouteRuntime> foundRoutes) == false)
        {
            return false;
        }

        int directRouteCount = 0;

        foreach (SpatialRouteRuntime candidate in foundRoutes)
        {
            if (candidate == null || candidate.Destination != destination)
            {
                continue;
            }

            directRouteCount++;
            route = candidate;
        }

        if (directRouteCount == 1)
        {
            return true;
        }

        route = null;

        if (directRouteCount > 1)
        {
            logger.LogError($"Direct route from location '{origin.RuntimeId}' to '{destination.RuntimeId}' is ambiguous: {directRouteCount} routes exist.");
        }

        return false;
    }

    private bool CanAdvanceRevision(string registrationKind)
    {
        if (revision < long.MaxValue)
        {
            return true;
        }

        logger.LogError($"Cannot register {registrationKind}: spatial network census revision is exhausted.");
        return false;
    }
}
