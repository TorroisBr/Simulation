using System;
using System.Collections.Generic;

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

    public bool RegisterLocation(SpatialLocationRuntime location)
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

        if (identityRegistry.RegisterLocation(location) == false)
        {
            return false;
        }

        locations.Add(location);
        outgoingRoutes.Add(location, new List<SpatialRouteRuntime>());
        revision++;
        return true;
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
