using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;

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
    private long censusRevision;

    internal long CensusRevision => censusRevision;

    public RuntimeIdentityRegistry(SimulationLogger logger = null)
    {
        this.logger = logger ?? UnitySimulationLoggerAdapter.Create(null);
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

        if (CanAdvanceCensusRevision("NPC") == false)
        {
            return false;
        }

        npcsByRuntimeId.Add(npcRuntime.RuntimeId, npcRuntime);
        AdvanceCensusRevision();
        return true;
    }

    internal bool TryGetNpcForMembership(string runtimeId, out NpcRuntime npcRuntime)
    {
        if (!string.IsNullOrWhiteSpace(runtimeId)
            && npcsByRuntimeId.TryGetValue(runtimeId, out npcRuntime))
            return true;

        npcRuntime = null;
        return false;
    }

    internal bool CanRegisterNpcForMembership(NpcRuntime npcRuntime, out bool duplicateIdentity)
    {
        duplicateIdentity = false;
        if (npcRuntime == null || string.IsNullOrWhiteSpace(npcRuntime.RuntimeId))
            return false;
        if (TryGetRegisteredType(npcRuntime.RuntimeId, out _))
        {
            duplicateIdentity = true;
            return false;
        }
        return censusRevision < long.MaxValue;
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

        if (CanAdvanceCensusRevision("City") == false)
        {
            return false;
        }

        citiesByRuntimeId.Add(cityRuntime.RuntimeId, cityRuntime);
        AdvanceCensusRevision();
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

        if (CanAdvanceCensusRevision("Location") == false)
        {
            return false;
        }

        bool indexAdded = false;
        bool revisionAdvanced = false;
        try
        {
            locationsByRuntimeId.Add(location.RuntimeId, location);
            indexAdded = true;
            completedMutation?.Invoke("RuntimeIdentity.Location.Index");
            AdvanceCensusRevision();
            revisionAdvanced = true;
            completedMutation?.Invoke("RuntimeIdentity.Location.Revision");
            return true;
        }
        catch
        {
            if (revisionAdvanced) censusRevision--;
            if (indexAdded) locationsByRuntimeId.Remove(location.RuntimeId);
            throw;
        }
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

        if (CanAdvanceCensusRevision("Route") == false)
        {
            return false;
        }

        routesByRuntimeId.Add(route.RuntimeId, route);
        AdvanceCensusRevision();
        return true;
    }

    public bool RegisterExplorableSite(ExplorableSiteRuntime site)
    {
        return RegisterExplorableSiteCore(site, null);
    }

    internal bool RegisterExplorableSiteForP10Genesis(ExplorableSiteRuntime site, Action<string> completedMutation)
    {
        return RegisterExplorableSiteCore(site, completedMutation);
    }

    private bool RegisterExplorableSiteCore(ExplorableSiteRuntime site, Action<string> completedMutation)
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

        if (CanAdvanceCensusRevision("ExplorableSite") == false)
        {
            return false;
        }

        bool indexAdded = false;
        bool revisionAdvanced = false;
        try
        {
            explorableSitesByRuntimeId.Add(site.RuntimeId, site);
            indexAdded = true;
            completedMutation?.Invoke("RuntimeIdentity.ExplorableSite.Index");
            AdvanceCensusRevision();
            revisionAdvanced = true;
            completedMutation?.Invoke("RuntimeIdentity.ExplorableSite.Revision");
            return true;
        }
        catch
        {
            if (revisionAdvanced) censusRevision--;
            if (indexAdded) explorableSitesByRuntimeId.Remove(site.RuntimeId);
            throw;
        }
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

        if (CanAdvanceCensusRevision("LocalPlace") == false)
        {
            return false;
        }

        localPlacesByRuntimeId.Add(localPlace.RuntimeId, localPlace);
        AdvanceCensusRevision();
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

        if (CanAdvanceCensusRevision("LocalConnection") == false)
        {
            return false;
        }

        localConnectionsByRuntimeId.Add(localConnection.RuntimeId, localConnection);
        AdvanceCensusRevision();
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

        if (CanAdvanceCensusRevision("NotableItem") == false)
        {
            return false;
        }

        notableItemsByRuntimeId.Add(notableItem.RuntimeId, notableItem);
        AdvanceCensusRevision();
        return true;
    }

    internal bool TryRegisterLocalTopologyMembers(
        IReadOnlyList<LocalPlaceRuntime> localPlaces,
        IReadOnlyList<LocalTopologyConnectionRuntime> localConnections,
        out string diagnostic)
    {
        return TryRegisterLocalTopologyMembersCore(localPlaces, localConnections, null, out diagnostic);
    }

    internal bool TryRegisterLocalTopologyMembersForP10Genesis(
        IReadOnlyList<LocalPlaceRuntime> localPlaces,
        IReadOnlyList<LocalTopologyConnectionRuntime> localConnections,
        Action<string> completedMutation,
        out string diagnostic)
    {
        return TryRegisterLocalTopologyMembersCore(localPlaces, localConnections, completedMutation, out diagnostic);
    }

    private bool TryRegisterLocalTopologyMembersCore(
        IReadOnlyList<LocalPlaceRuntime> localPlaces,
        IReadOnlyList<LocalTopologyConnectionRuntime> localConnections,
        Action<string> completedMutation,
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

        if (runtimeIds.Count > 0 && CanAdvanceCensusRevision("local topology batch") == false)
        {
            diagnostic = "Runtime identity census revision is exhausted.";
            return false;
        }

        int addedPlaces = 0;
        int addedConnections = 0;
        bool revisionAdvanced = false;
        try
        {
            for (int i = 0; i < localPlaces.Count; i++)
            {
                LocalPlaceRuntime localPlace = localPlaces[i];
                localPlacesByRuntimeId.Add(localPlace.RuntimeId, localPlace);
                addedPlaces++;
                completedMutation?.Invoke("RuntimeIdentity.LocalPlace.Index." + i.ToString(CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < localConnections.Count; i++)
            {
                LocalTopologyConnectionRuntime localConnection = localConnections[i];
                localConnectionsByRuntimeId.Add(localConnection.RuntimeId, localConnection);
                addedConnections++;
                completedMutation?.Invoke("RuntimeIdentity.LocalConnection.Index." + i.ToString(CultureInfo.InvariantCulture));
            }

            if (runtimeIds.Count > 0)
            {
                AdvanceCensusRevision();
                revisionAdvanced = true;
                completedMutation?.Invoke("RuntimeIdentity.LocalTopology.Revision");
            }

            return true;
        }
        catch
        {
            if (revisionAdvanced) censusRevision--;
            for (int i = addedConnections - 1; i >= 0; i--)
                localConnectionsByRuntimeId.Remove(localConnections[i].RuntimeId);
            for (int i = addedPlaces - 1; i >= 0; i--)
                localPlacesByRuntimeId.Remove(localPlaces[i].RuntimeId);
            throw;
        }
    }

    internal int GetCensusCardinality(RuntimeIdentityCensusIndex index)
    {
        switch (index)
        {
            case RuntimeIdentityCensusIndex.Npcs: return npcsByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.Cities: return citiesByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.Locations: return locationsByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.Routes: return routesByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.ExplorableSites: return explorableSitesByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.LocalPlaces: return localPlacesByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.LocalConnections: return localConnectionsByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.NotableItems: return notableItemsByRuntimeId.Count;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    internal IReadOnlyList<KeyValuePair<string, string>> CaptureRegisteredRuntimeIdentityValues()
    {
        List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
        AddIdentityValues(values, "npc", npcsByRuntimeId);
        AddIdentityValues(values, "city", citiesByRuntimeId);
        AddIdentityValues(values, "location", locationsByRuntimeId);
        AddIdentityValues(values, "route", routesByRuntimeId);
        AddIdentityValues(values, "site", explorableSitesByRuntimeId);
        AddIdentityValues(values, "local-place", localPlacesByRuntimeId);
        AddIdentityValues(values, "local-connection", localConnectionsByRuntimeId);
        AddIdentityValues(values, "notable-item", notableItemsByRuntimeId);
        values.Sort((left, right) =>
        {
            int familyOrder = string.Compare(left.Key, right.Key, StringComparison.Ordinal);
            return familyOrder != 0
                ? familyOrder
                : string.Compare(left.Value, right.Value, StringComparison.Ordinal);
        });
        return values.AsReadOnly();
    }

    private static void AddIdentityValues<TValue>(
        List<KeyValuePair<string, string>> values,
        string familyId,
        Dictionary<string, TValue> identities)
    {
        foreach (KeyValuePair<string, TValue> identity in identities)
            values.Add(new KeyValuePair<string, string>(familyId, identity.Key));
    }

    private bool CanAdvanceCensusRevision(string identityKind)
    {
        if (censusRevision < long.MaxValue)
        {
            return true;
        }

        logger.LogError($"Cannot register {identityKind} runtime identity: census revision is exhausted.");
        return false;
    }

    private void AdvanceCensusRevision()
    {
        censusRevision++;
    }

    internal void RollbackGenesisExplorableSite(ExplorableSiteRuntime site)
    {
        if (site == null || !explorableSitesByRuntimeId.TryGetValue(site.RuntimeId, out ExplorableSiteRuntime current)
            || !ReferenceEquals(site, current) || censusRevision <= 0)
            throw new InvalidOperationException("Cannot roll back the P10-B ExplorableSite identity insertion.");
        explorableSitesByRuntimeId.Remove(site.RuntimeId);
        censusRevision--;
    }

    internal void RollbackGenesisLocation(SpatialLocationRuntime location)
    {
        if (location == null || !locationsByRuntimeId.TryGetValue(location.RuntimeId, out SpatialLocationRuntime current)
            || !ReferenceEquals(location, current) || censusRevision <= 0)
            throw new InvalidOperationException("Cannot roll back the P10-B Location identity insertion.");
        locationsByRuntimeId.Remove(location.RuntimeId);
        censusRevision--;
    }

    internal void RollbackGenesisLocalTopologyMembers(
        IReadOnlyList<LocalPlaceRuntime> places, IReadOnlyList<LocalTopologyConnectionRuntime> connections)
    {
        if (places == null || connections == null || censusRevision <= 0)
            throw new InvalidOperationException("Cannot roll back the P10-B LocalTopology identity batch.");
        foreach (LocalPlaceRuntime place in places)
            if (place == null || !localPlacesByRuntimeId.TryGetValue(place.RuntimeId, out LocalPlaceRuntime currentPlace)
                || !ReferenceEquals(place, currentPlace))
                throw new InvalidOperationException("P10-B LocalPlace identity rollback did not match the inserted object.");
        foreach (LocalTopologyConnectionRuntime connection in connections)
            if (connection == null || !localConnectionsByRuntimeId.TryGetValue(connection.RuntimeId, out LocalTopologyConnectionRuntime currentConnection)
                || !ReferenceEquals(connection, currentConnection))
                throw new InvalidOperationException("P10-B LocalConnection identity rollback did not match the inserted object.");
        foreach (LocalPlaceRuntime place in places) localPlacesByRuntimeId.Remove(place.RuntimeId);
        foreach (LocalTopologyConnectionRuntime connection in connections) localConnectionsByRuntimeId.Remove(connection.RuntimeId);
        censusRevision--;
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
