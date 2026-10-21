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
    private readonly DailyV1RuntimeIdentityIndex identityIndex = new DailyV1RuntimeIdentityIndex();

    internal long CensusRevision => identityIndex.CensusRevision;
    internal object CensusOwnerIdentity => identityIndex.CensusOwnerIdentity;
    internal DailyV1RuntimeIdentityIndex CoreIdentityIndex => identityIndex;

    public RuntimeIdentityRegistry(SimulationLogger logger = null)
        : this(logger, null)
    {
    }

    internal RuntimeIdentityRegistry(
        SimulationLogger logger,
        DailyV1RuntimeIdentityIndex sharedIdentityIndex)
    {
        this.logger = logger ?? UnitySimulationLoggerAdapter.Create(null);
        identityIndex = sharedIdentityIndex ?? new DailyV1RuntimeIdentityIndex();
    }

    internal bool BindSelectedNpcProjection(NpcRuntime npcRuntime) =>
        npcRuntime != null && TryBindProjection(npcRuntime.RuntimeId,
            DailyV1RuntimeIdentityKind.Npc, npcsByRuntimeId, npcRuntime);

    internal bool BindSelectedCityProjection(CityRuntime cityRuntime) =>
        cityRuntime != null && TryBindProjection(cityRuntime.RuntimeId,
            DailyV1RuntimeIdentityKind.City, citiesByRuntimeId, cityRuntime);

    internal bool BindSelectedLocationProjection(SpatialLocationRuntime location) =>
        location != null && TryBindProjection(location.RuntimeId,
            DailyV1RuntimeIdentityKind.Location, locationsByRuntimeId, location);

    internal bool TryBindSelectedLocationProjection(
        SpatialLocationRuntime location,
        IDictionary<string, SpatialLocationRuntime> selectedLocationsByRuntimeId)
    {
        if (location == null || selectedLocationsByRuntimeId == null
            || !BindSelectedLocationProjection(location)) return false;

        bool selectedLookupAdded = false;
        try
        {
            selectedLocationsByRuntimeId.Add(location.RuntimeId, location);
            selectedLookupAdded = true;
            return true;
        }
        catch
        {
            if (selectedLookupAdded)
            {
                if (!selectedLocationsByRuntimeId.TryGetValue(location.RuntimeId,
                        out SpatialLocationRuntime current)
                    || !ReferenceEquals(current, location)
                    || !selectedLocationsByRuntimeId.Remove(location.RuntimeId))
                    throw new InvalidOperationException(
                        "A failed selected Location projection could not roll back its staged runtime lookup.");
            }
            RollbackSelectedLocationProjection(location);
            throw;
        }
    }

    internal void RollbackSelectedLocationProjection(SpatialLocationRuntime location)
    {
        if (location == null || !locationsByRuntimeId.TryGetValue(location.RuntimeId, out SpatialLocationRuntime current)
            || !ReferenceEquals(location, current))
            throw new InvalidOperationException("Cannot roll back a selected Location projection that does not match its Core identity.");
        locationsByRuntimeId.Remove(location.RuntimeId);
    }

    internal void RollbackSelectedLocationProjection(
        SpatialLocationRuntime location,
        IDictionary<string, SpatialLocationRuntime> selectedLocationsByRuntimeId)
    {
        if (location == null || selectedLocationsByRuntimeId == null
            || !selectedLocationsByRuntimeId.TryGetValue(location.RuntimeId,
                out SpatialLocationRuntime current)
            || !ReferenceEquals(location, current)
            || !selectedLocationsByRuntimeId.Remove(location.RuntimeId))
            throw new InvalidOperationException(
                "Cannot roll back a selected Location lookup that does not match its Core identity.");

        RollbackSelectedLocationProjection(location);
    }

    private bool TryBindProjection<TValue>(
        string runtimeId,
        DailyV1RuntimeIdentityKind kind,
        Dictionary<string, TValue> projections,
        TValue value)
    {
        return identityIndex.HasIdentity(runtimeId, kind)
            && !projections.ContainsKey(runtimeId)
            && projections.TryAdd(runtimeId, value);
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

        if (!TryStageIdentity(npcRuntime.RuntimeId, DailyV1RuntimeIdentityKind.Npc, "NPC")) return false;
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
        return identityIndex.CensusRevision < long.MaxValue;
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

        if (!TryStageIdentity(cityRuntime.RuntimeId, DailyV1RuntimeIdentityKind.City, "City")) return false;
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
        bool identityAdded = false;
        bool revisionAdvanced = false;
        try
        {
            if (!TryStageIdentity(location.RuntimeId, DailyV1RuntimeIdentityKind.Location, "Location")) return false;
            identityAdded = true;
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
            if (revisionAdvanced) RollbackCensusRevision();
            if (indexAdded) locationsByRuntimeId.Remove(location.RuntimeId);
            if (identityAdded) identityIndex.TryRemoveStaged(location.RuntimeId, DailyV1RuntimeIdentityKind.Location);
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

        if (!TryStageIdentity(route.RuntimeId, DailyV1RuntimeIdentityKind.Route, "Route")) return false;
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
        bool identityAdded = false;
        bool revisionAdvanced = false;
        try
        {
            if (!TryStageIdentity(site.RuntimeId, DailyV1RuntimeIdentityKind.ExplorableSite, "ExplorableSite")) return false;
            identityAdded = true;
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
            if (revisionAdvanced) RollbackCensusRevision();
            if (indexAdded) explorableSitesByRuntimeId.Remove(site.RuntimeId);
            if (identityAdded) identityIndex.TryRemoveStaged(site.RuntimeId, DailyV1RuntimeIdentityKind.ExplorableSite);
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

        if (!TryStageIdentity(localPlace.RuntimeId, DailyV1RuntimeIdentityKind.LocalPlace, "LocalPlace")) return false;
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

        if (!TryStageIdentity(localConnection.RuntimeId, DailyV1RuntimeIdentityKind.LocalConnection, "LocalConnection")) return false;
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

        if (!TryStageIdentity(notableItem.RuntimeId, DailyV1RuntimeIdentityKind.NotableItem, "NotableItem")) return false;
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
        int stagedPlaces = 0;
        int stagedConnections = 0;
        bool revisionAdvanced = false;
        try
        {
            for (int i = 0; i < localPlaces.Count; i++)
            {
                LocalPlaceRuntime localPlace = localPlaces[i];
                if (!TryStageIdentity(localPlace.RuntimeId, DailyV1RuntimeIdentityKind.LocalPlace, "LocalPlace"))
                    throw new InvalidOperationException("The Core runtime identity index rejected a prevalidated LocalPlace id.");
                stagedPlaces++;
                localPlacesByRuntimeId.Add(localPlace.RuntimeId, localPlace);
                addedPlaces++;
                completedMutation?.Invoke("RuntimeIdentity.LocalPlace.Index." + i.ToString(CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < localConnections.Count; i++)
            {
                LocalTopologyConnectionRuntime localConnection = localConnections[i];
                if (!TryStageIdentity(localConnection.RuntimeId, DailyV1RuntimeIdentityKind.LocalConnection, "LocalConnection"))
                    throw new InvalidOperationException("The Core runtime identity index rejected a prevalidated LocalConnection id.");
                stagedConnections++;
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
            if (revisionAdvanced) RollbackCensusRevision();
            for (int i = addedConnections - 1; i >= 0; i--)
                localConnectionsByRuntimeId.Remove(localConnections[i].RuntimeId);
            for (int i = addedPlaces - 1; i >= 0; i--)
                localPlacesByRuntimeId.Remove(localPlaces[i].RuntimeId);
            for (int i = stagedConnections - 1; i >= 0; i--)
                identityIndex.TryRemoveStaged(localConnections[i].RuntimeId, DailyV1RuntimeIdentityKind.LocalConnection);
            for (int i = stagedPlaces - 1; i >= 0; i--)
                identityIndex.TryRemoveStaged(localPlaces[i].RuntimeId, DailyV1RuntimeIdentityKind.LocalPlace);
            throw;
        }
    }

    internal int GetCensusCardinality(RuntimeIdentityCensusIndex index)
    {
        switch (index)
        {
            case RuntimeIdentityCensusIndex.Npcs: return identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Npc);
            case RuntimeIdentityCensusIndex.Cities: return identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.City);
            case RuntimeIdentityCensusIndex.Locations: return identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Location);
            case RuntimeIdentityCensusIndex.Routes: return identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Route);
            case RuntimeIdentityCensusIndex.ExplorableSites: return identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.ExplorableSite);
            case RuntimeIdentityCensusIndex.LocalPlaces: return identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.LocalPlace);
            case RuntimeIdentityCensusIndex.LocalConnections: return identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.LocalConnection);
            case RuntimeIdentityCensusIndex.NotableItems: return identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.NotableItem);
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    internal IReadOnlyList<KeyValuePair<string, string>> CaptureRegisteredRuntimeIdentityValues()
    {
        return identityIndex.CaptureRegisteredValues();
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
        if (identityIndex.CensusRevision < long.MaxValue)
        {
            return true;
        }

        logger.LogError($"Cannot register {identityKind} runtime identity: census revision is exhausted.");
        return false;
    }

    private void AdvanceCensusRevision()
    {
        if (!identityIndex.TryAdvanceRevision())
            throw new InvalidOperationException("Runtime identity census revision is exhausted.");
    }

    private bool TryStageIdentity(string runtimeId, DailyV1RuntimeIdentityKind kind, string identityKind)
    {
        if (!identityIndex.TryStageRegister(runtimeId, kind))
        {
            if (identityIndex.CensusRevision == long.MaxValue)
                logger.LogError($"Cannot register {identityKind} runtime identity: census revision is exhausted.");
            else
                logger.LogError($"Cannot register {identityKind} runtime identity: Core rejected duplicate or invalid RuntimeId '{runtimeId}'.");
            return false;
        }
        return true;
    }

    private void RollbackCensusRevision()
    {
        if (!identityIndex.TryRollbackRevision())
            throw new InvalidOperationException("Runtime identity census revision rollback failed.");
    }

    internal void RollbackGenesisExplorableSite(ExplorableSiteRuntime site)
    {
        if (site == null || !explorableSitesByRuntimeId.TryGetValue(site.RuntimeId, out ExplorableSiteRuntime current)
            || !ReferenceEquals(site, current) || CensusRevision <= 0)
            throw new InvalidOperationException("Cannot roll back the P10-B ExplorableSite identity insertion.");
        explorableSitesByRuntimeId.Remove(site.RuntimeId);
        if (!identityIndex.TryRemoveStaged(site.RuntimeId, DailyV1RuntimeIdentityKind.ExplorableSite)
            || !identityIndex.TryRollbackRevision())
            throw new InvalidOperationException("Core ExplorableSite identity rollback failed.");
    }

    internal void RollbackGenesisLocation(SpatialLocationRuntime location)
    {
        if (location == null || !locationsByRuntimeId.TryGetValue(location.RuntimeId, out SpatialLocationRuntime current)
            || !ReferenceEquals(location, current) || CensusRevision <= 0)
            throw new InvalidOperationException("Cannot roll back the P10-B Location identity insertion.");
        locationsByRuntimeId.Remove(location.RuntimeId);
        if (!identityIndex.TryRemoveStaged(location.RuntimeId, DailyV1RuntimeIdentityKind.Location)
            || !identityIndex.TryRollbackRevision())
            throw new InvalidOperationException("Core Location identity rollback failed.");
    }

    internal void RollbackGenesisLocalTopologyMembers(
        IReadOnlyList<LocalPlaceRuntime> places, IReadOnlyList<LocalTopologyConnectionRuntime> connections)
    {
        if (places == null || connections == null || CensusRevision <= 0)
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
        foreach (LocalPlaceRuntime place in places)
            if (!identityIndex.TryRemoveStaged(place.RuntimeId, DailyV1RuntimeIdentityKind.LocalPlace))
                throw new InvalidOperationException("Core LocalPlace identity rollback failed.");
        foreach (LocalTopologyConnectionRuntime connection in connections)
            if (!identityIndex.TryRemoveStaged(connection.RuntimeId, DailyV1RuntimeIdentityKind.LocalConnection))
                throw new InvalidOperationException("Core LocalConnection identity rollback failed.");
        if (!identityIndex.TryRollbackRevision())
            throw new InvalidOperationException("Core LocalTopology revision rollback failed.");
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
        if (identityIndex.TryGetKind(runtimeId, out DailyV1RuntimeIdentityKind kind))
        {
            registeredType = kind switch
            {
                DailyV1RuntimeIdentityKind.Npc => "NPC",
                DailyV1RuntimeIdentityKind.City => "City",
                DailyV1RuntimeIdentityKind.Location => "Location",
                DailyV1RuntimeIdentityKind.Route => "Route",
                DailyV1RuntimeIdentityKind.ExplorableSite => "ExplorableSite",
                DailyV1RuntimeIdentityKind.LocalPlace => "LocalPlace",
                DailyV1RuntimeIdentityKind.LocalConnection => "LocalConnection",
                DailyV1RuntimeIdentityKind.NotableItem => "NotableItem",
                _ => null
            };
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
