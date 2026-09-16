using System;
using System.Collections.Generic;

public sealed class WorldObserverReadModel
{
    public IReadOnlyList<WorldObserverLocationReadModel> Locations { get; }
    public IReadOnlyList<WorldObserverLocationReadModel> MacroLocations => Locations;
    public IReadOnlyList<WorldObserverRouteReadModel> Routes { get; }
    public IReadOnlyList<WorldObserverSiteReadModel> Sites { get; }
    public IReadOnlyList<WorldObserverTravelerReadModel> Travelers { get; }
    public IReadOnlyList<WorldObserverExpeditionReadModel> Expeditions { get; }
    public WorldObserverPlaceReadModel SelectedPlace { get; }
    public IReadOnlyList<WorldObserverActivityReadModel> ActivityFeed { get; }

    public WorldObserverReadModel(
        IReadOnlyList<WorldObserverLocationReadModel> locations,
        IReadOnlyList<WorldObserverRouteReadModel> routes,
        IReadOnlyList<WorldObserverSiteReadModel> sites,
        IReadOnlyList<WorldObserverTravelerReadModel> travelers,
        IReadOnlyList<WorldObserverExpeditionReadModel> expeditions,
        WorldObserverPlaceReadModel selectedPlace,
        IReadOnlyList<WorldObserverActivityReadModel> activityFeed)
    {
        Locations = locations ?? Array.Empty<WorldObserverLocationReadModel>();
        Routes = routes ?? Array.Empty<WorldObserverRouteReadModel>();
        Sites = sites ?? Array.Empty<WorldObserverSiteReadModel>();
        Travelers = travelers ?? Array.Empty<WorldObserverTravelerReadModel>();
        Expeditions = expeditions ?? Array.Empty<WorldObserverExpeditionReadModel>();
        SelectedPlace = selectedPlace;
        ActivityFeed = activityFeed ?? Array.Empty<WorldObserverActivityReadModel>();
    }
}

public sealed class WorldObserverLocationReadModel
{
    public string RuntimeId { get; }
    public string DisplayName { get; }
    public bool IsCity { get; }
    public IReadOnlyList<string> SiteRuntimeIds { get; }

    public WorldObserverLocationReadModel(
        string runtimeId,
        string displayName,
        bool isCity,
        IReadOnlyList<string> siteRuntimeIds)
    {
        RuntimeId = runtimeId;
        DisplayName = displayName;
        IsCity = isCity;
        SiteRuntimeIds = siteRuntimeIds ?? Array.Empty<string>();
    }
}

public sealed class WorldObserverRouteReadModel
{
    public string RuntimeId { get; }
    public string OriginLocationRuntimeId { get; }
    public string DestinationLocationRuntimeId { get; }
    public int TravelDays { get; }

    public WorldObserverRouteReadModel(SpatialRouteRuntime route)
    {
        RuntimeId = route.RuntimeId;
        OriginLocationRuntimeId = route.Origin.RuntimeId;
        DestinationLocationRuntimeId = route.Destination.RuntimeId;
        TravelDays = route.TravelDays;
    }
}

public sealed class WorldObserverSiteReadModel
{
    public string RuntimeId { get; }
    public string DisplayName { get; }
    public string LocationRuntimeId { get; }
    public bool HasState { get; }
    public PlaceSiteState SiteState { get; }
    public PlaceAccessState AccessState { get; }

    public WorldObserverSiteReadModel(
        ExplorableSiteRuntime site,
        bool hasState,
        PlaceSiteState siteState,
        PlaceAccessState accessState)
    {
        RuntimeId = site.RuntimeId;
        DisplayName = string.IsNullOrWhiteSpace(site.Definition.DisplayName) == true
            ? site.RuntimeId
            : site.Definition.DisplayName;
        LocationRuntimeId = site.Location.RuntimeId;
        HasState = hasState;
        SiteState = siteState;
        AccessState = accessState;
    }
}

public sealed class WorldObserverTravelerReadModel
{
    public string RuntimeId { get; }
    public string DisplayName { get; }
    public bool IsTraveling { get; }
    public string CurrentLocationRuntimeId { get; }
    public string DestinationLocationRuntimeId { get; }
    public string RouteRuntimeId { get; }
    public int RemainingTravelDays { get; }
    public string ExpeditionId { get; }
    public ExpeditionState? ExpeditionState { get; }
    public int ExplorationProgress { get; }
    public int ExplorationProgressRequired { get; }

    public WorldObserverTravelerReadModel(
        NpcRuntime npc,
        ExpeditionRuntime expedition,
        TravelPartyRuntime travelParty = null)
    {
        RuntimeId = npc.RuntimeId;
        DisplayName = npc.NpcData != null && string.IsNullOrWhiteSpace(npc.NpcData.name) == false
            ? npc.NpcData.name
            : npc.RuntimeId;
        IsTraveling = npc.IsTraveling;
        CurrentLocationRuntimeId = npc.CurrentLocation?.RuntimeId;
        DestinationLocationRuntimeId = npc.DestinationLocation?.RuntimeId;
        RouteRuntimeId = travelParty?.RouteRuntimeId;
        RemainingTravelDays = npc.TravelDaysRemaining;
        ExpeditionId = expedition?.ExpeditionId;
        ExpeditionState = expedition?.State;
        ExplorationProgress = expedition != null ? expedition.ExplorationProgress : 0;
        ExplorationProgressRequired = expedition != null ? expedition.ExplorationProgressRequired : 0;
    }
}

public sealed class WorldObserverExpeditionReadModel
{
    public string ExpeditionId { get; }
    public string TargetSiteRuntimeId { get; }
    public ExpeditionState State { get; }
    public ExpeditionObjectiveType ObjectiveType { get; }
    public int Progress { get; }
    public int RequiredProgress { get; }
    public bool IsActive { get; }
    public string CurrentLocalPlaceRuntimeId { get; }

    public WorldObserverExpeditionReadModel(ExpeditionRuntime expedition)
    {
        ExpeditionId = expedition.ExpeditionId;
        TargetSiteRuntimeId = expedition.TargetSiteRuntimeId;
        State = expedition.State;
        ObjectiveType = expedition.Objective.ObjectiveType;
        Progress = expedition.ExplorationProgress;
        RequiredProgress = expedition.ExplorationProgressRequired;
        IsActive = expedition.IsActive;
        CurrentLocalPlaceRuntimeId = expedition.CurrentLocalPlaceRuntimeId;
    }
}

public sealed class WorldObserverPlaceReadModel
{
    public string RuntimeId { get; }
    public string DisplayName { get; }
    public PlaceContentOwnerKind OwnerKind { get; }
    public string MacroLocationRuntimeId { get; }
    public IReadOnlyList<string> PresentNpcRuntimeIds { get; }
    public IReadOnlyList<WorldObserverExpeditionReadModel> Expeditions { get; }
    public bool HasSiteState { get; }
    public PlaceSiteState SiteState { get; }
    public PlaceAccessState AccessState { get; }
    public string ControllerRuntimeId { get; }
    public IReadOnlyList<WorldObserverOppositionReadModel> Oppositions { get; }
    public IReadOnlyList<WorldObserverContentReadModel> Content { get; }
    public IReadOnlyList<WorldObserverTopologyNodeReadModel> TopologyNodes { get; }
    public IReadOnlyList<WorldObserverTopologyConnectionReadModel> TopologyConnections { get; }

    public WorldObserverPlaceReadModel(
        string runtimeId,
        string displayName,
        PlaceContentOwnerKind ownerKind,
        string macroLocationRuntimeId,
        IReadOnlyList<string> presentNpcRuntimeIds,
        IReadOnlyList<WorldObserverExpeditionReadModel> expeditions,
        bool hasSiteState,
        PlaceSiteState siteState,
        PlaceAccessState accessState,
        string controllerRuntimeId,
        IReadOnlyList<WorldObserverOppositionReadModel> oppositions,
        IReadOnlyList<WorldObserverContentReadModel> content,
        IReadOnlyList<WorldObserverTopologyNodeReadModel> topologyNodes,
        IReadOnlyList<WorldObserverTopologyConnectionReadModel> topologyConnections)
    {
        RuntimeId = runtimeId;
        DisplayName = displayName;
        OwnerKind = ownerKind;
        MacroLocationRuntimeId = macroLocationRuntimeId;
        PresentNpcRuntimeIds = presentNpcRuntimeIds ?? Array.Empty<string>();
        Expeditions = expeditions ?? Array.Empty<WorldObserverExpeditionReadModel>();
        HasSiteState = hasSiteState;
        SiteState = siteState;
        AccessState = accessState;
        ControllerRuntimeId = controllerRuntimeId;
        Oppositions = oppositions ?? Array.Empty<WorldObserverOppositionReadModel>();
        Content = content ?? Array.Empty<WorldObserverContentReadModel>();
        TopologyNodes = topologyNodes ?? Array.Empty<WorldObserverTopologyNodeReadModel>();
        TopologyConnections = topologyConnections ?? Array.Empty<WorldObserverTopologyConnectionReadModel>();
    }
}

public sealed class WorldObserverContentReadModel
{
    public string ItemDefinitionId { get; }
    public int Amount { get; }
    public PlaceContentPersistencePolicy PersistencePolicy { get; }
    public string NotableRuntimeId { get; }

    public WorldObserverContentReadModel(PlaceContentStackRuntime stack)
    {
        ItemDefinitionId = stack.ItemDefinitionId;
        Amount = stack.Amount;
        PersistencePolicy = stack.PersistencePolicy;
        NotableRuntimeId = null;
    }

    public WorldObserverContentReadModel(NotableItemRuntime notable)
    {
        ItemDefinitionId = notable.DefinitionId;
        Amount = 1;
        PersistencePolicy = PlaceContentPersistencePolicy.Notable;
        NotableRuntimeId = notable.RuntimeId;
    }
}

public sealed class WorldObserverOppositionReadModel
{
    public string RuntimeId { get; }
    public string DisplayName { get; }
    public bool IsActive { get; }
    public int NamedParticipantCount { get; }
    public int AggregateParticipantCount { get; }

    public WorldObserverOppositionReadModel(PlaceOppositionRuntime opposition)
    {
        RuntimeId = opposition.RuntimeId;
        DisplayName = opposition.DisplayName;
        IsActive = opposition.IsActive;
        NamedParticipantCount = opposition.NamedParticipants.Count;
        AggregateParticipantCount = opposition.AggregateParticipants.Count;
    }
}

public sealed class WorldObserverTopologyNodeReadModel
{
    public string RuntimeId { get; }
    public string DisplayName { get; }
    public string ParentRuntimeId { get; }
    public int Depth { get; }
    public bool IsEntryPoint { get; }

    public WorldObserverTopologyNodeReadModel(LocalTopologyRuntime topology, LocalPlaceRuntime place)
    {
        RuntimeId = place.RuntimeId;
        DisplayName = place.DisplayName;
        ParentRuntimeId = place.Parent?.RuntimeId;
        Depth = GetDepth(place);
        IsEntryPoint = topology.IsEntryPoint(place);
    }

    private static int GetDepth(LocalPlaceRuntime place)
    {
        int depth = 0;
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        LocalPlaceRuntime current = place?.Parent;
        while (current != null && visited.Add(current.RuntimeId) == true)
        {
            depth++;
            current = current.Parent;
        }

        return depth;
    }
}

public sealed class WorldObserverTopologyConnectionReadModel
{
    public string RuntimeId { get; }
    public string OriginRuntimeId { get; }
    public string DestinationRuntimeId { get; }
    public float TraversalCost { get; }

    public WorldObserverTopologyConnectionReadModel(LocalTopologyConnectionRuntime connection)
    {
        RuntimeId = connection.RuntimeId;
        OriginRuntimeId = connection.Origin.RuntimeId;
        DestinationRuntimeId = connection.Destination.RuntimeId;
        TraversalCost = connection.TraversalCost;
    }
}

public sealed class WorldObserverActivityReadModel
{
    public string EventId { get; }
    public long AbsoluteDay { get; }
    public DomainEventType EventType { get; }
    public string Summary { get; }

    public WorldObserverActivityReadModel(DomainEvent domainEvent)
    {
        EventId = domainEvent.EventId;
        AbsoluteDay = domainEvent.AbsoluteDay;
        EventType = domainEvent.EventType;
        Summary = domainEvent.EventType + " · day " + domainEvent.AbsoluteDay;
    }
}

public sealed class WorldObserverQueryService
{
    private readonly IReadOnlyList<CityRuntime> cities;
    private readonly IReadOnlyList<NpcRuntime> npcs;
    private readonly SpatialNetworkRuntime network;
    private readonly ExplorableSiteStore siteStore;
    private readonly ExpeditionStore expeditionStore;
    private readonly TravelPartyStore travelPartyStore;
    private readonly LocalTopologyStore topologyStore;
    private readonly PlaceContentStore contentStore;
    private readonly DomainEventStore eventStore;

    public WorldObserverQueryService(
        IEnumerable<CityRuntime> cities,
        IEnumerable<NpcRuntime> npcs,
        SpatialNetworkRuntime network,
        ExplorableSiteStore siteStore,
        ExpeditionStore expeditionStore = null,
        TravelPartyStore travelPartyStore = null,
        LocalTopologyStore topologyStore = null,
        PlaceContentStore contentStore = null,
        DomainEventStore eventStore = null)
    {
        this.cities = new List<CityRuntime>(cities ?? Array.Empty<CityRuntime>()).AsReadOnly();
        this.npcs = new List<NpcRuntime>(npcs ?? Array.Empty<NpcRuntime>()).AsReadOnly();
        this.network = network;
        this.siteStore = siteStore;
        this.expeditionStore = expeditionStore;
        this.travelPartyStore = travelPartyStore;
        this.topologyStore = topologyStore;
        this.contentStore = contentStore;
        this.eventStore = eventStore;
    }

    public WorldObserverReadModel BuildReadModel(string selectedPlaceRuntimeId = null)
    {
        List<WorldObserverLocationReadModel> locations = BuildLocations();
        List<WorldObserverRouteReadModel> routes = BuildRoutes();
        List<WorldObserverSiteReadModel> sites = BuildSites();
        List<WorldObserverExpeditionReadModel> expeditions = BuildExpeditions();
        List<WorldObserverTravelerReadModel> travelers = BuildTravelers();
        WorldObserverPlaceReadModel selectedPlace = BuildSelectedPlace(selectedPlaceRuntimeId, expeditions);
        List<WorldObserverActivityReadModel> activityFeed = BuildActivityFeed();

        return new WorldObserverReadModel(
            locations.AsReadOnly(),
            routes.AsReadOnly(),
            sites.AsReadOnly(),
            travelers.AsReadOnly(),
            expeditions.AsReadOnly(),
            selectedPlace,
            activityFeed.AsReadOnly());
    }

    public WorldObserverReadModel Query(string selectedPlaceRuntimeId = null)
    {
        return BuildReadModel(selectedPlaceRuntimeId);
    }

    private List<WorldObserverLocationReadModel> BuildLocations()
    {
        List<WorldObserverLocationReadModel> result = new List<WorldObserverLocationReadModel>();
        if (network == null)
        {
            return result;
        }

        List<SpatialLocationRuntime> orderedLocations = new List<SpatialLocationRuntime>(network.Locations);
        orderedLocations.Sort((left, right) => string.CompareOrdinal(left?.RuntimeId, right?.RuntimeId));
        foreach (SpatialLocationRuntime location in orderedLocations)
        {
            if (location == null)
            {
                continue;
            }

            CityRuntime city = FindCityForLocation(location.RuntimeId);
            List<string> siteIds = new List<string>();
            if (siteStore != null)
            {
                foreach (ExplorableSiteRuntime site in siteStore.GetForLocationRuntimeId(location.RuntimeId))
                {
                    if (site != null)
                    {
                        siteIds.Add(site.RuntimeId);
                    }
                }
            }

            result.Add(new WorldObserverLocationReadModel(
                location.RuntimeId,
                city != null ? city.CityName : location.RuntimeId,
                city != null,
                siteIds.AsReadOnly()));
        }

        return result;
    }

    private List<WorldObserverRouteReadModel> BuildRoutes()
    {
        List<WorldObserverRouteReadModel> result = new List<WorldObserverRouteReadModel>();
        if (network == null)
        {
            return result;
        }

        List<SpatialRouteRuntime> orderedRoutes = new List<SpatialRouteRuntime>(network.Routes);
        orderedRoutes.Sort((left, right) => string.CompareOrdinal(left?.RuntimeId, right?.RuntimeId));
        foreach (SpatialRouteRuntime route in orderedRoutes)
        {
            if (route != null)
            {
                result.Add(new WorldObserverRouteReadModel(route));
            }
        }

        return result;
    }

    private List<WorldObserverSiteReadModel> BuildSites()
    {
        List<WorldObserverSiteReadModel> result = new List<WorldObserverSiteReadModel>();
        if (siteStore == null)
        {
            return result;
        }

        foreach (ExplorableSiteRuntime site in siteStore.Sites)
        {
            if (site == null)
            {
                continue;
            }

            PlaceContentRuntime content = null;
            bool hasState = contentStore != null && contentStore.TryGet(site, out content);
            result.Add(new WorldObserverSiteReadModel(
                site,
                hasState,
                hasState ? content.SiteState : default(PlaceSiteState),
                hasState ? content.AccessState : default(PlaceAccessState)));
        }

        result.Sort((left, right) => string.CompareOrdinal(left.RuntimeId, right.RuntimeId));
        return result;
    }

    private List<WorldObserverTravelerReadModel> BuildTravelers()
    {
        List<WorldObserverTravelerReadModel> result = new List<WorldObserverTravelerReadModel>();
        List<NpcRuntime> orderedNpcs = new List<NpcRuntime>(npcs);
        orderedNpcs.Sort((left, right) => string.CompareOrdinal(left?.RuntimeId, right?.RuntimeId));
        foreach (NpcRuntime npc in orderedNpcs)
        {
            if (npc == null)
            {
                continue;
            }

            ExpeditionRuntime expedition = null;
            TravelPartyRuntime party = null;
            expeditionStore?.TryGetExpeditionForNpc(npc.RuntimeId, out expedition);
            travelPartyStore?.TryGetPartyForNpc(npc.RuntimeId, out party);
            result.Add(new WorldObserverTravelerReadModel(npc, expedition, party));
        }

        return result;
    }

    private List<WorldObserverExpeditionReadModel> BuildExpeditions()
    {
        List<WorldObserverExpeditionReadModel> result = new List<WorldObserverExpeditionReadModel>();
        if (expeditionStore == null)
        {
            return result;
        }

        foreach (ExpeditionRuntime expedition in expeditionStore.ActiveExpeditions)
        {
            if (expedition != null)
            {
                result.Add(new WorldObserverExpeditionReadModel(expedition));
            }
        }

        return result;
    }

    private WorldObserverPlaceReadModel BuildSelectedPlace(
        string selectedPlaceRuntimeId,
        IReadOnlyList<WorldObserverExpeditionReadModel> allExpeditions)
    {
        if (string.IsNullOrWhiteSpace(selectedPlaceRuntimeId) == true)
        {
            return null;
        }

        CityRuntime city = FindCity(selectedPlaceRuntimeId);
        if (city != null)
        {
            return BuildPlace(
                city.RuntimeId,
                city.CityName,
                PlaceContentOwnerKind.City,
                city.Location.RuntimeId,
                PlaceContentOwnerReference.ForCity(city),
                allExpeditions,
                null);
        }

        ExplorableSiteRuntime site = siteStore?.GetByRuntimeId(selectedPlaceRuntimeId);
        if (site != null)
        {
            return BuildPlace(
                site.RuntimeId,
                site.Definition.DisplayName,
                PlaceContentOwnerKind.ExplorableSite,
                site.Location.RuntimeId,
                PlaceContentOwnerReference.ForExplorableSite(site),
                allExpeditions,
                site.RuntimeId);
        }

        if (topologyStore != null && topologyStore.TryGetLocalPlace(selectedPlaceRuntimeId, out LocalPlaceRuntime localPlace) == true)
        {
            LocalTopologyRuntime topology = localPlace.OwningTopology;
            PlaceContentOwnerReference owner = PlaceContentOwnerReference.ForLocalPlace(localPlace);
            return BuildPlace(
                localPlace.RuntimeId,
                localPlace.DisplayName,
                PlaceContentOwnerKind.LocalPlace,
                owner.MacroLocationRuntimeId,
                owner,
                allExpeditions,
                topology?.Owner.OwnerRuntimeId);
        }

        return null;
    }

    private WorldObserverPlaceReadModel BuildPlace(
        string runtimeId,
        string displayName,
        PlaceContentOwnerKind ownerKind,
        string macroLocationRuntimeId,
        PlaceContentOwnerReference owner,
        IReadOnlyList<WorldObserverExpeditionReadModel> allExpeditions,
        string expeditionTargetSiteRuntimeId)
    {
        List<string> presentNpcs = new List<string>();
        foreach (NpcRuntime npc in npcs)
        {
            if (npc != null && npc.CurrentLocation != null
                && string.Equals(npc.CurrentLocation.RuntimeId, macroLocationRuntimeId, StringComparison.Ordinal) == true)
            {
                presentNpcs.Add(npc.RuntimeId);
            }
        }
        presentNpcs.Sort(StringComparer.Ordinal);

        List<WorldObserverExpeditionReadModel> expeditions = new List<WorldObserverExpeditionReadModel>();
        foreach (WorldObserverExpeditionReadModel expedition in allExpeditions)
        {
            if (expedition != null && string.Equals(expedition.TargetSiteRuntimeId, expeditionTargetSiteRuntimeId, StringComparison.Ordinal) == true)
            {
                expeditions.Add(expedition);
            }
        }

        PlaceContentRuntime content = null;
        bool hasContent = contentStore != null && contentStore.TryGet(owner, out content);
        List<WorldObserverOppositionReadModel> oppositions = new List<WorldObserverOppositionReadModel>();
        List<WorldObserverContentReadModel> contentReadModels = new List<WorldObserverContentReadModel>();
        bool hasState = false;
        PlaceSiteState siteState = default(PlaceSiteState);
        PlaceAccessState accessState = default(PlaceAccessState);
        string controllerRuntimeId = null;
        if (hasContent == true)
        {
            hasState = true;
            siteState = content.SiteState;
            accessState = content.AccessState;
            controllerRuntimeId = content.ControllerRuntimeId;
            foreach (PlaceOppositionRuntime opposition in content.Oppositions)
            {
                if (opposition != null)
                {
                    oppositions.Add(new WorldObserverOppositionReadModel(opposition));
                }
            }

            foreach (PlaceContentStackRuntime stack in content.StackedContent)
            {
                if (stack != null && stack.Amount > 0)
                {
                    contentReadModels.Add(new WorldObserverContentReadModel(stack));
                }
            }

            foreach (NotableItemRuntime notable in content.NotableContent)
            {
                if (notable != null)
                {
                    contentReadModels.Add(new WorldObserverContentReadModel(notable));
                }
            }
        }

        List<WorldObserverTopologyNodeReadModel> nodes = new List<WorldObserverTopologyNodeReadModel>();
        List<WorldObserverTopologyConnectionReadModel> connections = new List<WorldObserverTopologyConnectionReadModel>();
        LocalTopologyRuntime topology = FindTopology(ownerKind, owner);
        if (topology != null && topology.IsPublished == true)
        {
            foreach (LocalPlaceRuntime place in topology.Places)
            {
                if (place != null)
                {
                    nodes.Add(new WorldObserverTopologyNodeReadModel(topology, place));
                }
            }

            foreach (LocalTopologyConnectionRuntime connection in topology.Connections)
            {
                if (connection != null)
                {
                    connections.Add(new WorldObserverTopologyConnectionReadModel(connection));
                }
            }
        }

        nodes.Sort((left, right) => string.CompareOrdinal(left.RuntimeId, right.RuntimeId));
        connections.Sort((left, right) => string.CompareOrdinal(left.RuntimeId, right.RuntimeId));
        return new WorldObserverPlaceReadModel(
            runtimeId,
            displayName,
            ownerKind,
            macroLocationRuntimeId,
            presentNpcs.AsReadOnly(),
            expeditions.AsReadOnly(),
            hasState,
            siteState,
            accessState,
            controllerRuntimeId,
            oppositions.AsReadOnly(),
            contentReadModels.AsReadOnly(),
            nodes.AsReadOnly(),
            connections.AsReadOnly());
    }

    private LocalTopologyRuntime FindTopology(
        PlaceContentOwnerKind ownerKind,
        PlaceContentOwnerReference owner)
    {
        if (topologyStore == null || owner == null)
        {
            return null;
        }

        string topologyOwnerRuntimeId = ownerKind == PlaceContentOwnerKind.LocalPlace
            && string.IsNullOrWhiteSpace(owner.TopologyOwnerRuntimeId) == false
            ? owner.TopologyOwnerRuntimeId
            : owner.OwnerRuntimeId;
        return topologyStore.TryGetTopologyForOwner(topologyOwnerRuntimeId, out LocalTopologyRuntime topology) == true
            ? topology
            : null;
    }

    private List<WorldObserverActivityReadModel> BuildActivityFeed()
    {
        List<WorldObserverActivityReadModel> result = new List<WorldObserverActivityReadModel>();
        if (eventStore == null)
        {
            return result;
        }

        for (int i = eventStore.Events.Count - 1; i >= 0 && result.Count < 30; i--)
        {
            DomainEvent domainEvent = eventStore.Events[i];
            if (domainEvent != null)
            {
                result.Add(new WorldObserverActivityReadModel(domainEvent));
            }
        }

        return result;
    }

    private CityRuntime FindCity(string runtimeId)
    {
        foreach (CityRuntime city in cities)
        {
            if (city != null && string.Equals(city.RuntimeId, runtimeId, StringComparison.Ordinal) == true)
            {
                return city;
            }
        }

        return null;
    }

    private CityRuntime FindCityForLocation(string locationRuntimeId)
    {
        foreach (CityRuntime city in cities)
        {
            if (city != null && city.Location != null
                && string.Equals(city.Location.RuntimeId, locationRuntimeId, StringComparison.Ordinal) == true)
            {
                return city;
            }
        }

        return null;
    }
}
