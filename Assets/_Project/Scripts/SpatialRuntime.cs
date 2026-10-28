using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

        if (!P12DSpatialNetworkOwnerHydrator.TryCreateOwner(
            snapshot,
            out SpatialNetworkOwner stagedOwner,
            out failure))
        {
            return false;
        }

        return TryCreateFromStagedOwner(
            stagedOwner, stagedIdentityRegistry, false,
            out stagedNetwork, out failure);
    }

    /// <summary>
    /// Creates the Unity spatial projection over an already staged exact Core
    /// owner. The private identity registry receives the same location and
    /// route instances; no second spatial authority is hydrated.
    /// </summary>
    internal static bool TryCreateFromStagedOwner(
        SpatialNetworkOwner stagedOwner,
        RuntimeIdentityRegistry stagedIdentityRegistry,
        bool identitiesAlreadyStaged,
        out SpatialNetworkRuntime stagedNetwork,
        out SpatialNetworkSnapshotFailure failure)
    {
        stagedNetwork = null;
        if (stagedOwner == null || stagedIdentityRegistry == null)
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.InvalidSnapshot,
                "A Core-staged spatial owner and unpublished identity registry are required.");
            return false;
        }

        long identityCount = (long)stagedOwner.LocationCount + stagedOwner.RouteCount;
        foreach (SpatialLocationRuntime location in stagedOwner.Locations)
        {
            bool identityMatches = identitiesAlreadyStaged
                ? stagedIdentityRegistry.CoreIdentityIndex.HasIdentity(
                    location.RuntimeId, DailyV1RuntimeIdentityKind.Location)
                : stagedIdentityRegistry.IsRuntimeIdAvailable(location.RuntimeId);
            if (!identityMatches)
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision,
                    "A spatial runtime identity is already occupied in the staged identity registry.");
                return false;
            }
        }
        foreach (SpatialRouteRuntime route in stagedOwner.Routes)
        {
            bool identityMatches = identitiesAlreadyStaged
                ? stagedIdentityRegistry.CoreIdentityIndex.HasIdentity(
                    route.RuntimeId, DailyV1RuntimeIdentityKind.Route)
                : stagedIdentityRegistry.IsRuntimeIdAvailable(route.RuntimeId);
            if (!identityMatches)
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision,
                    "A spatial runtime identity is already occupied in the staged identity registry.");
                return false;
            }
        }

        if (!identitiesAlreadyStaged
            && stagedIdentityRegistry.CensusRevision > long.MaxValue - identityCount)
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.RuntimeIdentityRevisionExhausted,
                "The staged runtime identity registry cannot represent all spatial owner identities.");
            return false;
        }

        foreach (SpatialLocationRuntime location in stagedOwner.Locations)
        {
            bool bound = identitiesAlreadyStaged
                ? stagedIdentityRegistry.BindSelectedLocationProjection(location)
                : stagedIdentityRegistry.RegisterLocation(location);
            if (!bound)
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision,
                    "The staged identity registry rejected a validated legacy location identity.");
                return false;
            }
        }

        foreach (SpatialRouteRuntime route in stagedOwner.Routes)
        {
            bool bound = identitiesAlreadyStaged
                ? stagedIdentityRegistry.BindSelectedRouteProjection(route)
                : stagedIdentityRegistry.RegisterRoute(route);
            if (!bound)
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision,
                    "The staged identity registry rejected a validated legacy route identity.");
                return false;
            }
        }

        stagedNetwork = new SpatialNetworkRuntime(stagedIdentityRegistry, null, stagedOwner);
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
