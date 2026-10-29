using System;
using System.Collections.Generic;

/// <summary>
/// Authoritative location and route membership shared by the Unity adapter and
/// the detached Daily-v1 host. RuntimeIdentityRegistry remains a host-side
/// cross-family lookup index; it does not own spatial topology or revision.
/// </summary>
internal sealed class SpatialNetworkOwner
{
    private readonly HashSet<SpatialLocationRuntime> locations = new HashSet<SpatialLocationRuntime>();
    private readonly List<SpatialRouteRuntime> routes = new List<SpatialRouteRuntime>();
    private readonly Dictionary<string, SpatialLocationRuntime> locationsById =
        new Dictionary<string, SpatialLocationRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, SpatialRouteRuntime> routesById =
        new Dictionary<string, SpatialRouteRuntime>(StringComparer.Ordinal);
    private readonly HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<SpatialLocationRuntime, List<SpatialRouteRuntime>> outgoingRoutes =
        new Dictionary<SpatialLocationRuntime, List<SpatialRouteRuntime>>();
    private long revision;

    internal int LocationCount => locations.Count;
    internal int RouteCount => routes.Count;
    internal long Revision => revision;
    internal IEnumerable<SpatialLocationRuntime> Locations
    {
        get
        {
            SpatialLocationRuntime[] snapshot = new SpatialLocationRuntime[locations.Count];
            locations.CopyTo(snapshot);
            return Array.AsReadOnly(snapshot);
        }
    }
    internal IReadOnlyList<SpatialRouteRuntime> Routes => Array.AsReadOnly(routes.ToArray());

    /// <summary>Captures the exact selected spatial owner as detached P12 values.</summary>
    internal SpatialNetworkOwnerSnapshot CaptureOwnerSnapshot()
    {
        List<string> locationIds = new List<string>(locationsById.Keys);
        locationIds.Sort(StringComparer.Ordinal);

        List<SpatialRouteOwnerSnapshotRecord> routeRecords =
            new List<SpatialRouteOwnerSnapshotRecord>(routes.Count);
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

    internal bool ContainsLocation(SpatialLocationRuntime location) =>
        location != null && locations.Contains(location);

    internal bool ContainsLocationId(string runtimeId) =>
        runtimeId != null && locationsById.ContainsKey(runtimeId);

    internal bool ContainsRouteId(string runtimeId) =>
        runtimeId != null && routesById.ContainsKey(runtimeId);

    internal bool CanAdvanceRevision => revision < long.MaxValue;

    internal bool TryGetLocation(string runtimeId, out SpatialLocationRuntime location) =>
        !string.IsNullOrWhiteSpace(runtimeId) && locationsById.TryGetValue(runtimeId, out location)
            ? true
            : Fail(out location);

    internal bool TryGetRoute(string runtimeId, out SpatialRouteRuntime route) =>
        !string.IsNullOrWhiteSpace(runtimeId) && routesById.TryGetValue(runtimeId, out route)
            ? true
            : Fail(out route);

    internal bool TryRegisterLocation(
        SpatialLocationRuntime location,
        Func<bool> registerIdentity,
        Action rollbackIdentity,
        Action<string> completedMutation)
    {
        if (location == null || registerIdentity == null || rollbackIdentity == null
            || locations.Contains(location) || runtimeIds.Contains(location.RuntimeId)
            || !CanAdvanceRevision)
            return false;

        bool identityRegistered = false;
        bool networkLocationAdded = false;
        bool locationIdAdded = false;
        bool outgoingRoutesAdded = false;
        bool revisionAdvanced = false;
        try
        {
            if (!registerIdentity()) return false;
            identityRegistered = true;
            completedMutation?.Invoke("SpatialNetwork.Location.IdentityRegistered");

            if (!locations.Add(location))
                throw new InvalidOperationException("Spatial location became registered during genesis publication.");
            networkLocationAdded = true;
            locationIdAdded = locationsById.TryAdd(location.RuntimeId, location);
            if (!locationIdAdded || !runtimeIds.Add(location.RuntimeId))
                throw new InvalidOperationException("Spatial location identity became occupied during genesis publication.");
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
            if (locationIdAdded)
            {
                locationsById.Remove(location.RuntimeId);
                runtimeIds.Remove(location.RuntimeId);
            }
            if (networkLocationAdded) locations.Remove(location);
            if (identityRegistered) rollbackIdentity();
            throw;
        }
    }

    internal bool TryRegisterRoute(SpatialRouteRuntime route, Func<bool> registerIdentity)
    {
        if (route == null || registerIdentity == null
            || !locations.Contains(route.Origin) || !locations.Contains(route.Destination)
            || runtimeIds.Contains(route.RuntimeId) || !CanAdvanceRevision
            || !outgoingRoutes.TryGetValue(route.Origin, out List<SpatialRouteRuntime> outgoing))
            return false;

        if (!registerIdentity()) return false;
        routes.Add(route);
        routesById.Add(route.RuntimeId, route);
        runtimeIds.Add(route.RuntimeId);
        outgoing.Add(route);
        revision++;
        return true;
    }

    internal bool TryInstallRestoredLocation(SpatialLocationRuntime location, Func<bool> registerIdentity)
    {
        if (location == null || registerIdentity == null || runtimeIds.Contains(location.RuntimeId)
            || !registerIdentity())
            return false;

        if (!locations.Add(location)
            || !locationsById.TryAdd(location.RuntimeId, location)
            || !runtimeIds.Add(location.RuntimeId))
            return false;
        outgoingRoutes.Add(location, new List<SpatialRouteRuntime>());
        revision++;
        return true;
    }

    internal bool TryInstallRestoredRoute(SpatialRouteRuntime route, Func<bool> registerIdentity)
    {
        if (route == null || registerIdentity == null
            || !locations.Contains(route.Origin) || !locations.Contains(route.Destination)
            || runtimeIds.Contains(route.RuntimeId)
            || !outgoingRoutes.TryGetValue(route.Origin, out List<SpatialRouteRuntime> outgoing)
            || !registerIdentity())
            return false;

        routes.Add(route);
        routesById.Add(route.RuntimeId, route);
        runtimeIds.Add(route.RuntimeId);
        outgoing.Add(route);
        revision++;
        return true;
    }

    internal bool TryInstallRestoredRevision(long value)
    {
        if (value < revision || value < 0L) return false;
        revision = value;
        return true;
    }

    internal bool TryRollbackGenesisLocation(SpatialLocationRuntime location, Action rollbackIdentity)
    {
        if (location == null || rollbackIdentity == null || !locations.Contains(location) || revision <= 0
            || (outgoingRoutes.TryGetValue(location, out List<SpatialRouteRuntime> outgoing) && outgoing.Count != 0)
            || !locationsById.TryGetValue(location.RuntimeId, out SpatialLocationRuntime current)
            || !ReferenceEquals(location, current))
            return false;

        locations.Remove(location);
        locationsById.Remove(location.RuntimeId);
        runtimeIds.Remove(location.RuntimeId);
        outgoingRoutes.Remove(location);
        rollbackIdentity();
        revision--;
        return true;
    }

    internal IReadOnlyList<SpatialRouteRuntime> GetOutgoingRoutes(SpatialLocationRuntime origin)
    {
        return origin != null && outgoingRoutes.TryGetValue(origin, out List<SpatialRouteRuntime> foundRoutes)
            ? Array.AsReadOnly(foundRoutes.ToArray())
            : Array.Empty<SpatialRouteRuntime>();
    }

    internal bool TryGetSingleDirectRoute(
        SpatialLocationRuntime origin,
        SpatialLocationRuntime destination,
        out SpatialRouteRuntime route,
        out int matchingRouteCount)
    {
        route = null;
        matchingRouteCount = 0;
        if (origin == null || destination == null
            || !outgoingRoutes.TryGetValue(origin, out List<SpatialRouteRuntime> foundRoutes))
            return false;

        foreach (SpatialRouteRuntime candidate in foundRoutes)
        {
            if (candidate == null || candidate.Destination != destination) continue;
            matchingRouteCount++;
            route = candidate;
        }

        if (matchingRouteCount == 1) return true;
        route = null;
        return false;
    }

    private static bool Fail<T>(out T value)
    {
        value = default(T);
        return false;
    }
}
