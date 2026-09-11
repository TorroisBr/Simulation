using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class RuntimeIdAllocator
{
    private long nextNpcSequence = 1;
    private long nextCitySequence = 1;
    private long nextLocationSequence = 1;
    private long nextRouteSequence = 1;
    private long nextEventSequence = 1;
    private long nextDirectiveSequence = 1;
    private long nextDecisionSequence = 1;

    public string AllocateNpcId()
    {
        return Allocate("npc", ref nextNpcSequence);
    }

    public string AllocateCityId()
    {
        return Allocate("city", ref nextCitySequence);
    }

    public string AllocateLocationId()
    {
        return Allocate("location", ref nextLocationSequence);
    }

    public string AllocateRouteId()
    {
        return Allocate("route", ref nextRouteSequence);
    }

    public string AllocateEventId()
    {
        return Allocate("event", ref nextEventSequence);
    }

    public string AllocateDirectiveId()
    {
        return Allocate("directive", ref nextDirectiveSequence);
    }

    public string AllocateDecisionId()
    {
        return Allocate("decision", ref nextDecisionSequence);
    }

    private static string Allocate(string prefix, ref long nextSequence)
    {
        if (nextSequence == long.MaxValue)
        {
            throw new InvalidOperationException($"RuntimeId sequence exhausted for type '{prefix}'.");
        }

        string runtimeId = prefix + "-" + nextSequence.ToString("D6", CultureInfo.InvariantCulture);
        nextSequence++;
        return runtimeId;
    }
}

public sealed class RuntimeIdentityRegistry
{
    private readonly Dictionary<string, NpcRuntime> npcsByRuntimeId = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, CityRuntime> citiesByRuntimeId = new Dictionary<string, CityRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, SpatialLocationRuntime> locationsByRuntimeId = new Dictionary<string, SpatialLocationRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, SpatialRouteRuntime> routesByRuntimeId = new Dictionary<string, SpatialRouteRuntime>(StringComparer.Ordinal);
    private readonly SimulationLogger logger;

    public RuntimeIdentityRegistry(SimulationLogger logger = null)
    {
        this.logger = logger ?? new SimulationLogger(null);
    }

    public bool RegisterNpc(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            logger.LogError("Cannot register NPC runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcRuntime.RuntimeId) == true)
        {
            logger.LogError("Cannot register NPC runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(npcRuntime.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{npcRuntime.RuntimeId}' while registering NPC; it is already registered as {registeredType}.");
            return false;
        }

        npcsByRuntimeId.Add(npcRuntime.RuntimeId, npcRuntime);
        return true;
    }

    public bool RegisterCity(CityRuntime cityRuntime)
    {
        if (cityRuntime == null)
        {
            logger.LogError("Cannot register City runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(cityRuntime.RuntimeId) == true)
        {
            logger.LogError("Cannot register City runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(cityRuntime.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{cityRuntime.RuntimeId}' while registering City; it is already registered as {registeredType}.");
            return false;
        }

        citiesByRuntimeId.Add(cityRuntime.RuntimeId, cityRuntime);
        return true;
    }

    public bool RegisterLocation(SpatialLocationRuntime location)
    {
        if (location == null)
        {
            logger.LogError("Cannot register Location runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(location.RuntimeId) == true)
        {
            logger.LogError("Cannot register Location runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(location.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{location.RuntimeId}' while registering Location; it is already registered as {registeredType}.");
            return false;
        }

        locationsByRuntimeId.Add(location.RuntimeId, location);
        return true;
    }

    public bool RegisterRoute(SpatialRouteRuntime route)
    {
        if (route == null)
        {
            logger.LogError("Cannot register Route runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(route.RuntimeId) == true)
        {
            logger.LogError("Cannot register Route runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(route.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{route.RuntimeId}' while registering Route; it is already registered as {registeredType}.");
            return false;
        }

        routesByRuntimeId.Add(route.RuntimeId, route);
        return true;
    }

    public bool TryGetNpc(string runtimeId, out NpcRuntime npcRuntime)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && npcsByRuntimeId.TryGetValue(runtimeId, out npcRuntime) == true)
        {
            return true;
        }

        npcRuntime = null;

        LogResolutionFailure("NPC", runtimeId);
        return false;
    }

    public bool TryGetCity(string runtimeId, out CityRuntime cityRuntime)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && citiesByRuntimeId.TryGetValue(runtimeId, out cityRuntime) == true)
        {
            return true;
        }

        cityRuntime = null;

        LogResolutionFailure("City", runtimeId);
        return false;
    }

    public bool TryGetLocation(string runtimeId, out SpatialLocationRuntime location)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && locationsByRuntimeId.TryGetValue(runtimeId, out location) == true)
        {
            return true;
        }

        location = null;
        LogResolutionFailure("Location", runtimeId);
        return false;
    }

    public bool TryGetRoute(string runtimeId, out SpatialRouteRuntime route)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && routesByRuntimeId.TryGetValue(runtimeId, out route) == true)
        {
            return true;
        }

        route = null;
        LogResolutionFailure("Route", runtimeId);
        return false;
    }

    private bool TryGetRegisteredType(string runtimeId, out string registeredType)
    {
        if (npcsByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "NPC";
            return true;
        }

        if (citiesByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "City";
            return true;
        }

        if (locationsByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "Location";
            return true;
        }

        if (routesByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "Route";
            return true;
        }

        registeredType = null;
        return false;
    }

    private void LogResolutionFailure(string requestedType, string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && TryGetRegisteredType(runtimeId, out string registeredType) == true)
        {
            logger.LogWarning($"{requestedType} runtime resolution failed: RuntimeId '{runtimeId}' is registered as {registeredType}, not {requestedType}.");
            return;
        }

        logger.LogWarning($"{requestedType} runtime resolution failed: RuntimeId '{FormatRuntimeId(runtimeId)}' is not registered.");
    }

    private static string FormatRuntimeId(string runtimeId)
    {
        return string.IsNullOrWhiteSpace(runtimeId) == true ? "<empty>" : runtimeId;
    }
}
