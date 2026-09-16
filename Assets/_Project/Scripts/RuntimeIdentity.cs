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
    private long nextTravelPartySequence = 1;
    private long nextOrganizationSequence = 1;
    private long nextExplorableSiteSequence = 1;
    private long nextExpeditionSequence = 1;
    private long nextLocalPlaceSequence = 1;
    private long nextLocalConnectionSequence = 1;
    private long nextNotableItemSequence = 1;

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

    public string AllocateTravelPartyId()
    {
        return Allocate("travel-party", ref nextTravelPartySequence);
    }

    public string AllocateOrganizationId()
    {
        return Allocate("organization", ref nextOrganizationSequence);
    }

    public string AllocateExplorableSiteId()
    {
        return Allocate("site", ref nextExplorableSiteSequence);
    }

    public string AllocateExpeditionId()
    {
        return Allocate("expedition", ref nextExpeditionSequence);
    }

    public string AllocateLocalPlaceId()
    {
        return Allocate("local-place", ref nextLocalPlaceSequence);
    }

    public string AllocateLocalConnectionId()
    {
        return Allocate("local-connection", ref nextLocalConnectionSequence);
    }

    public string AllocateNotableItemId()
    {
        return Allocate("notable-item", ref nextNotableItemSequence);
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
    private readonly Dictionary<string, ExplorableSiteRuntime> explorableSitesByRuntimeId = new Dictionary<string, ExplorableSiteRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalPlaceRuntime> localPlacesByRuntimeId = new Dictionary<string, LocalPlaceRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalTopologyConnectionRuntime> localConnectionsByRuntimeId = new Dictionary<string, LocalTopologyConnectionRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, NotableItemRuntime> notableItemsByRuntimeId = new Dictionary<string, NotableItemRuntime>(StringComparer.Ordinal);
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

    public bool RegisterExplorableSite(ExplorableSiteRuntime site)
    {
        if (site == null)
        {
            logger.LogError("Cannot register ExplorableSite runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(site.RuntimeId) == true)
        {
            logger.LogError("Cannot register ExplorableSite runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(site.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{site.RuntimeId}' while registering ExplorableSite; it is already registered as {registeredType}.");
            return false;
        }

        explorableSitesByRuntimeId.Add(site.RuntimeId, site);
        return true;
    }

    public bool RegisterLocalPlace(LocalPlaceRuntime localPlace)
    {
        if (localPlace == null)
        {
            logger.LogError("Cannot register LocalPlace runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(localPlace.RuntimeId) == true)
        {
            logger.LogError("Cannot register LocalPlace runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(localPlace.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{localPlace.RuntimeId}' while registering LocalPlace; it is already registered as {registeredType}.");
            return false;
        }

        localPlacesByRuntimeId.Add(localPlace.RuntimeId, localPlace);
        return true;
    }

    public bool RegisterLocalConnection(LocalTopologyConnectionRuntime localConnection)
    {
        if (localConnection == null)
        {
            logger.LogError("Cannot register LocalConnection runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(localConnection.RuntimeId) == true)
        {
            logger.LogError("Cannot register LocalConnection runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(localConnection.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{localConnection.RuntimeId}' while registering LocalConnection; it is already registered as {registeredType}.");
            return false;
        }

        localConnectionsByRuntimeId.Add(localConnection.RuntimeId, localConnection);
        return true;
    }

    public bool RegisterNotableItem(NotableItemRuntime notableItem)
    {
        if (notableItem == null)
        {
            logger.LogError("Cannot register NotableItem runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(notableItem.RuntimeId) == true)
        {
            logger.LogError("Cannot register NotableItem runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(notableItem.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{notableItem.RuntimeId}' while registering NotableItem; it is already registered as {registeredType}.");
            return false;
        }

        notableItemsByRuntimeId.Add(notableItem.RuntimeId, notableItem);
        return true;
    }

    internal bool TryRegisterLocalTopologyMembers(
        IReadOnlyList<LocalPlaceRuntime> localPlaces,
        IReadOnlyList<LocalTopologyConnectionRuntime> localConnections,
        out string diagnostic)
    {
        diagnostic = null;

        if (localPlaces == null || localConnections == null)
        {
            diagnostic = "Local topology member collections cannot be null.";
            return false;
        }

        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (LocalPlaceRuntime localPlace in localPlaces)
        {
            if (localPlace == null)
            {
                diagnostic = "Local topology contains a null LocalPlace.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(localPlace.RuntimeId) == true)
            {
                diagnostic = "Local topology contains a LocalPlace with an empty RuntimeId.";
                return false;
            }

            if (runtimeIds.Add(localPlace.RuntimeId) == false)
            {
                diagnostic = $"Local topology contains duplicate RuntimeId '{localPlace.RuntimeId}'.";
                return false;
            }

            if (IsRuntimeIdAvailable(localPlace.RuntimeId) == false)
            {
                diagnostic = $"LocalPlace RuntimeId '{localPlace.RuntimeId}' is already registered.";
                return false;
            }
        }

        foreach (LocalTopologyConnectionRuntime localConnection in localConnections)
        {
            if (localConnection == null)
            {
                diagnostic = "Local topology contains a null LocalConnection.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(localConnection.RuntimeId) == true)
            {
                diagnostic = "Local topology contains a LocalConnection with an empty RuntimeId.";
                return false;
            }

            if (runtimeIds.Add(localConnection.RuntimeId) == false)
            {
                diagnostic = $"Local topology contains duplicate RuntimeId '{localConnection.RuntimeId}'.";
                return false;
            }

            if (IsRuntimeIdAvailable(localConnection.RuntimeId) == false)
            {
                diagnostic = $"LocalConnection RuntimeId '{localConnection.RuntimeId}' is already registered.";
                return false;
            }
        }

        foreach (LocalPlaceRuntime localPlace in localPlaces)
        {
            localPlacesByRuntimeId.Add(localPlace.RuntimeId, localPlace);
        }

        foreach (LocalTopologyConnectionRuntime localConnection in localConnections)
        {
            localConnectionsByRuntimeId.Add(localConnection.RuntimeId, localConnection);
        }

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

    public bool TryFindRouteBetweenLocations(
        string originLocationRuntimeId,
        string destinationLocationRuntimeId,
        out SpatialRouteRuntime route)
    {
        route = null;
        if (string.IsNullOrWhiteSpace(originLocationRuntimeId) == true
            || string.IsNullOrWhiteSpace(destinationLocationRuntimeId) == true)
        {
            return false;
        }

        foreach (SpatialRouteRuntime candidate in routesByRuntimeId.Values)
        {
            if (candidate != null
                && candidate.Origin != null
                && candidate.Destination != null
                && string.Equals(candidate.Origin.RuntimeId, originLocationRuntimeId, StringComparison.Ordinal) == true
                && string.Equals(candidate.Destination.RuntimeId, destinationLocationRuntimeId, StringComparison.Ordinal) == true)
            {
                if (route != null)
                {
                    route = null;
                    return false;
                }

                route = candidate;
            }
        }

        return route != null;
    }

    public bool TryGetExplorableSite(string runtimeId, out ExplorableSiteRuntime site)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && explorableSitesByRuntimeId.TryGetValue(runtimeId, out site) == true)
        {
            return true;
        }

        site = null;

        LogResolutionFailure("ExplorableSite", runtimeId);
        return false;
    }

    public bool TryGetLocalPlace(string runtimeId, out LocalPlaceRuntime localPlace)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && localPlacesByRuntimeId.TryGetValue(runtimeId, out localPlace) == true)
        {
            return true;
        }

        localPlace = null;

        LogResolutionFailure("LocalPlace", runtimeId);
        return false;
    }

    public bool TryGetLocalConnection(string runtimeId, out LocalTopologyConnectionRuntime localConnection)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && localConnectionsByRuntimeId.TryGetValue(runtimeId, out localConnection) == true)
        {
            return true;
        }

        localConnection = null;

        LogResolutionFailure("LocalConnection", runtimeId);
        return false;
    }

    public bool TryGetNotableItem(string runtimeId, out NotableItemRuntime notableItem)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && notableItemsByRuntimeId.TryGetValue(runtimeId, out notableItem) == true)
        {
            return true;
        }

        notableItem = null;
        LogResolutionFailure("NotableItem", runtimeId);
        return false;
    }

    public bool IsRuntimeIdAvailable(string runtimeId)
    {
        return string.IsNullOrWhiteSpace(runtimeId) == false
            && TryGetRegisteredType(runtimeId, out _) == false;
    }

    internal bool TryGetCityWithoutLogging(string runtimeId, out CityRuntime cityRuntime)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && citiesByRuntimeId.TryGetValue(runtimeId, out cityRuntime) == true)
        {
            return true;
        }

        cityRuntime = null;
        return false;
    }

    internal bool TryGetNpcWithoutLogging(string runtimeId, out NpcRuntime npcRuntime)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && npcsByRuntimeId.TryGetValue(runtimeId, out npcRuntime) == true)
        {
            return true;
        }

        npcRuntime = null;
        return false;
    }

    internal bool TryGetExplorableSiteWithoutLogging(string runtimeId, out ExplorableSiteRuntime site)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && explorableSitesByRuntimeId.TryGetValue(runtimeId, out site) == true)
        {
            return true;
        }

        site = null;
        return false;
    }

    internal bool TryGetNotableItemWithoutLogging(string runtimeId, out NotableItemRuntime notableItem)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && notableItemsByRuntimeId.TryGetValue(runtimeId, out notableItem) == true)
        {
            return true;
        }

        notableItem = null;
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

        if (explorableSitesByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "ExplorableSite";
            return true;
        }

        if (localPlacesByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "LocalPlace";
            return true;
        }

        if (localConnectionsByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "LocalConnection";
            return true;
        }

        if (notableItemsByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "NotableItem";
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
