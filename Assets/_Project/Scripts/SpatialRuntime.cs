using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
    private readonly SpatialNetworkOwner owner;

    public IEnumerable<SpatialLocationRuntime> Locations => owner.Locations;

    public IReadOnlyList<SpatialRouteRuntime> Routes => owner.Routes;
    public int LocationCount => owner.LocationCount;
    public int RouteCount => owner.RouteCount;
    public long Revision => owner.Revision;
    internal SpatialNetworkOwner SelectedOwner => owner;

    public SpatialNetworkRuntime(RuntimeIdentityRegistry identityRegistry, SimulationLogger logger = null)
        : this(identityRegistry, logger, null)
    {
    }

    internal SpatialNetworkRuntime(
        RuntimeIdentityRegistry identityRegistry,
        SimulationLogger logger,
        SpatialNetworkOwner selectedOwner)
    {
        this.identityRegistry = identityRegistry ?? throw new ArgumentNullException(nameof(identityRegistry));
        this.logger = logger ?? UnitySimulationLoggerAdapter.Create(null);
        owner = selectedOwner ?? new SpatialNetworkOwner();
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
        List<string> locationIds = new List<string>(owner.LocationCount);
        foreach (SpatialLocationRuntime location in owner.Locations)
        {
            locationIds.Add(location.RuntimeId);
        }

        List<SpatialRouteOwnerSnapshotRecord> routeRecords = new List<SpatialRouteOwnerSnapshotRecord>(owner.RouteCount);
        foreach (SpatialRouteRuntime route in owner.Routes)
        {
            routeRecords.Add(new SpatialRouteOwnerSnapshotRecord(
                route.RuntimeId,
                route.Origin.RuntimeId,
                route.Destination.RuntimeId,
                route.TravelDays));
        }

        return new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
            owner.Revision,
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
            if (!staged.owner.TryInstallRestoredLocation(
                    location, () => stagedIdentityRegistry.RegisterLocation(location)))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision,
                    "The staged spatial owner rejected a validated legacy location identity.");
                return false;
            }
            stagedLocations.Add(locationId, location);
        }

        foreach (SpatialRouteOwnerSnapshotRecord routeRecord in snapshot.Routes)
        {
            SpatialRouteRuntime route = new SpatialRouteRuntime(
                routeRecord.RuntimeId,
                stagedLocations[routeRecord.OriginLocationRuntimeId],
                stagedLocations[routeRecord.DestinationLocationRuntimeId],
                routeRecord.TravelDays);
            if (!staged.owner.TryInstallRestoredRoute(
                    route, () => stagedIdentityRegistry.RegisterRoute(route)))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision,
                    "The staged identity registry rejected a validated legacy route identity.");
                return false;
            }

        }

        if (!staged.owner.TryInstallRestoredRevision(snapshot.Revision))
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.InvalidSnapshot,
                "The spatial owner snapshot revision is below its staged registration count.");
            return false;
        }
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

        if (owner.ContainsLocation(location))
        {
            logger.LogError($"Spatial location '{location.RuntimeId}' is already registered in this network.");
            return false;
        }

        if (!owner.CanAdvanceRevision)
        {
            logger.LogError("Cannot register spatial location: spatial network census revision is exhausted.");
            return false;
        }

        if (owner.ContainsLocationId(location.RuntimeId))
        {
            if (!identityRegistry.IsRuntimeIdAvailable(location.RuntimeId))
                identityRegistry.RegisterLocation(location);
            else
                logger.LogError($"Duplicate RuntimeId '{location.RuntimeId}' while registering Location.");
            return false;
        }

        return owner.TryRegisterLocation(
            location,
            () => completedMutation == null
                ? identityRegistry.RegisterLocation(location)
                : identityRegistry.RegisterLocationForP10Genesis(location, completedMutation),
            () => identityRegistry.RollbackGenesisLocation(location),
            completedMutation);
    }

    internal void RollbackGenesisLocation(SpatialLocationRuntime location)
    {
        if (location == null
            || !owner.ContainsLocation(location)
            || !identityRegistry.TryGetLocation(location.RuntimeId, out SpatialLocationRuntime current)
            || !ReferenceEquals(location, current))
            throw new InvalidOperationException("Cannot roll back the P10-B spatial Location insertion.");
        if (!owner.TryRollbackGenesisLocation(location, () => identityRegistry.RollbackGenesisLocation(location)))
            throw new InvalidOperationException("Cannot roll back the P10-B spatial Location insertion.");
    }

    public bool RegisterRoute(SpatialRouteRuntime route)
    {
        if (route == null)
        {
            logger.LogError("Cannot register spatial route: runtime instance is null.");
            return false;
        }

        if (!owner.ContainsLocation(route.Origin) || !owner.ContainsLocation(route.Destination))
        {
            logger.LogError($"Cannot register spatial route '{route.RuntimeId}': origin and destination must both be registered locations.");
            return false;
        }

        if (!owner.CanAdvanceRevision)
        {
            logger.LogError("Cannot register spatial route: spatial network census revision is exhausted.");
            return false;
        }

        if (owner.ContainsLocationId(route.RuntimeId) || owner.ContainsRouteId(route.RuntimeId))
        {
            if (!identityRegistry.IsRuntimeIdAvailable(route.RuntimeId))
                identityRegistry.RegisterRoute(route);
            else
                logger.LogError($"Duplicate RuntimeId '{route.RuntimeId}' while registering Route.");
            return false;
        }

        return owner.TryRegisterRoute(route, () => identityRegistry.RegisterRoute(route));
    }

    public bool TryGetLocation(string runtimeId, out SpatialLocationRuntime location)
    {
        if (owner.TryGetLocation(runtimeId, out location)) return true;
        return identityRegistry.TryGetLocation(runtimeId, out location);
    }

    public bool TryGetRoute(string runtimeId, out SpatialRouteRuntime route)
    {
        if (owner.TryGetRoute(runtimeId, out route)) return true;
        return identityRegistry.TryGetRoute(runtimeId, out route);
    }

    public IReadOnlyList<SpatialRouteRuntime> GetOutgoingRoutes(SpatialLocationRuntime origin)
    {
        return owner.GetOutgoingRoutes(origin);
    }

    public bool TryGetSingleDirectRoute(
        SpatialLocationRuntime origin,
        SpatialLocationRuntime destination,
        out SpatialRouteRuntime route)
    {
        bool found = owner.TryGetSingleDirectRoute(origin, destination, out route, out int directRouteCount);
        if (!found && directRouteCount > 1)
        {
            logger.LogError($"Direct route from location '{origin.RuntimeId}' to '{destination.RuntimeId}' is ambiguous: {directRouteCount} routes exist.");
        }
        return found;
    }
}
