using System;
using System.Collections.Generic;
using UnityEngine;

public class TravelSystem : IAuthoritativeMutationGuardBindable
{
    private readonly SpatialNetworkRuntime spatialNetwork;
    private readonly Func<SpatialLocationRuntime, CityRuntime> getCityRuntimeByLocation;
    private readonly EffectiveTravelConfiguration travelConfiguration;
    private readonly DomainEventRecorder domainEventRecorder;
    private readonly SimulationLogger logger;
    private readonly EconomyTransactionService transactionService;
    private TravelPartyStore travelPartyStore;
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();

    public TravelSystem(
        SpatialNetworkRuntime spatialNetwork,
        Func<SpatialLocationRuntime, CityRuntime> getCityRuntimeByLocation,
        float travelCostPerDay = 0f,
        SimulationLogger logger = null,
        EconomyTransactionService transactionService = null)
        : this(
            spatialNetwork,
            getCityRuntimeByLocation,
            new EffectiveTravelConfiguration(travelCostPerDay),
            null,
            logger,
            transactionService)
    {
    }

    public TravelSystem(
        SpatialNetworkRuntime spatialNetwork,
        Func<SpatialLocationRuntime, CityRuntime> getCityRuntimeByLocation,
        EffectiveTravelConfiguration travelConfiguration,
        DomainEventRecorder domainEventRecorder,
        SimulationLogger logger,
        EconomyTransactionService transactionService = null)
    {
        this.spatialNetwork = spatialNetwork;
        this.getCityRuntimeByLocation = getCityRuntimeByLocation;
        this.travelConfiguration = travelConfiguration ?? new EffectiveTravelConfiguration(0f);
        this.domainEventRecorder = domainEventRecorder;
        this.logger = logger ?? new SimulationLogger(null);
        this.transactionService = transactionService ?? new EconomyTransactionService();
    }

    public TravelSystem(
        SpatialNetworkRuntime spatialNetwork,
        Func<SpatialLocationRuntime, CityRuntime> getCityRuntimeByLocation,
        float travelCostPerDay,
        DomainEventRecorder domainEventRecorder,
        SimulationLogger logger,
        EconomyTransactionService transactionService = null)
        : this(
            spatialNetwork,
            getCityRuntimeByLocation,
            new EffectiveTravelConfiguration(travelCostPerDay),
            domainEventRecorder,
            logger,
            transactionService)
    {
    }

    public void AttachTravelPartyStore(TravelPartyStore store)
    {
        if (mutationGuardBinding.BoundGuard != null
            && !ReferenceEquals(travelPartyStore, store))
        {
            throw new InvalidOperationException(
                "TravelSystem cannot replace its attached TravelPartyStore after runtime binding.");
        }

        if (store != null && mutationGuardBinding.BoundGuard != null)
        {
            if (store.CanBindMutationGuard(mutationGuardBinding.BoundGuard) == false
                || store.TryBindMutationGuard(mutationGuardBinding.BoundGuard) == false)
            {
                throw new InvalidOperationException("TravelSystem cannot attach a TravelPartyStore owned by another runtime.");
            }
        }

        travelPartyStore = store;
    }

    public bool TryStartTravel(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            return false;
        }

        if (npcRuntime == null
            || actionRuntime == null
            || actionRuntime.TargetCity == null)
        {
            return false;
        }

        return TryStartTravel(
            npcRuntime,
            actionRuntime.TargetCity.Location,
            actionRuntime.TargetCity,
            actionRuntime.OriginDecisionId);
    }

    public bool TryStartTravel(
        NpcRuntime npcRuntime,
        SpatialLocationRuntime targetLocation,
        CityRuntime targetCityProjection,
        string originDecisionId = null)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            return false;
        }

        CityRuntime resolvedTargetCity = targetCityProjection ?? GetCityRuntime(targetLocation);

        if (npcRuntime == null
            || npcRuntime.IsAlive == false
            || targetLocation == null
            || (targetCityProjection != null && targetCityProjection.Location != targetLocation)
            || IsManagedByTravelParty(npcRuntime)
            || TryGetDirectRoute(npcRuntime.CurrentLocation, targetLocation, out SpatialRouteRuntime route) == false)
        {
            return false;
        }

        int travelDays = route.TravelDays;
        float travelCost = GetTravelCost(travelDays);

        if (travelCost < 0f || transactionService.CanChargeTravel(npcRuntime, travelCost) == false)
        {
            return false;
        }

        SpatialLocationRuntime originLocation = npcRuntime.CurrentLocation;

        EconomyTransactionResult charge = transactionService.TryChargeTravel(npcRuntime, travelCost);

        if (charge.Success == false)
        {
            return false;
        }

        if (npcRuntime.StartTravel(
            targetLocation,
            resolvedTargetCity,
            travelDays,
            originDecisionId,
            route.RuntimeId) == false)
        {
            transactionService.TryRestoreTravelCharge(npcRuntime, travelCost);
            return false;
        }

        // Direct discovery happens only after execution has entered the real route.
        npcRuntime.SpatialKnowledge.DiscoverLocation(originLocation.RuntimeId);
        npcRuntime.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
        domainEventRecorder?.Record((eventId, absoluteDay, recordSequence) => new NpcTravelStartedEvent(
            eventId,
            absoluteDay,
            recordSequence,
            npcRuntime.RuntimeId,
            originLocation.RuntimeId,
            targetLocation.RuntimeId,
            route.RuntimeId,
            originDecisionId));
        string originName = GetCityRuntime(originLocation)?.CityName ?? originLocation.RuntimeId;
        string destinationName = resolvedTargetCity != null ? resolvedTargetCity.CityName : targetLocation.RuntimeId;
        logger.Log(SimulationLogCategory.Travel, $"{npcRuntime.NpcName} iniciou viagem de {originName} para {destinationName}");

        if (travelCost > 0f)
        {
            logger.Log(SimulationLogCategory.Travel, $"Custo de viagem: {travelCost:0.##}");
        }

        return true;
    }

    public bool TryStartTravel(
        NpcRuntime npcRuntime,
        SpatialLocationRuntime targetLocation,
        int travelDays,
        string originDecisionId = null)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            return false;
        }

        if (travelDays <= 0 || TryGetDirectRoute(npcRuntime?.CurrentLocation, targetLocation, out SpatialRouteRuntime route) == false)
        {
            return false;
        }

        if (route.TravelDays != travelDays)
        {
            return false;
        }

        return TryStartTravel(npcRuntime, targetLocation, GetCityRuntime(targetLocation), originDecisionId);
    }

    public bool CanStartTravel(NpcRuntime npcRuntime, CityRuntime targetCity, out int travelDays, out float travelCost)
    {
        return CanStartTravel(npcRuntime, targetCity?.Location, out travelDays, out travelCost);
    }

    public bool CanStartTravel(
        NpcRuntime npcRuntime,
        SpatialLocationRuntime targetLocation,
        out int travelDays,
        out float travelCost)
    {
        travelDays = -1;
        travelCost = -1f;

        if (npcRuntime == null
            || npcRuntime.IsAlive == false
            || npcRuntime.CurrentLocation == null
            || targetLocation == null
            || npcRuntime.IsTraveling == true
            || IsManagedByTravelParty(npcRuntime))
        {
            return false;
        }

        travelDays = GetTravelDays(npcRuntime.CurrentLocation, targetLocation);

        if (travelDays <= 0)
        {
            return false;
        }

        travelCost = GetTravelCost(travelDays);
        return transactionService.CanChargeTravel(npcRuntime, travelCost);
    }

    public IReadOnlyList<NpcRuntime> AdvanceTravels(List<NpcRuntime> npcRuntimeList)
    {
        ThrowIfFaulted();

        if (npcRuntimeList == null)
        {
            return Array.Empty<NpcRuntime>();
        }

        List<NpcRuntime> arrivedNpcs = new List<NpcRuntime>();
        List<NpcRuntime> orderedNpcs = new List<NpcRuntime>(npcRuntimeList);
        orderedNpcs.Sort((left, right) => string.CompareOrdinal(
            left?.RuntimeId ?? string.Empty,
            right?.RuntimeId ?? string.Empty));

        foreach (NpcRuntime npcRuntime in orderedNpcs)
        {
            if (npcRuntime == null || npcRuntime.IsAlive == false || npcRuntime.IsTraveling == false || IsManagedByTravelParty(npcRuntime))
            {
                continue;
            }

            if (npcRuntime.TravelStartedToday == true)
            {
                npcRuntime.ClearTravelStartedToday();
                continue;
            }

            string originDecisionId = npcRuntime.TravelOriginDecisionId;
            bool arrived = npcRuntime.AdvanceTravelDay(out CityRuntime arrivedCity);

            if (arrived == true)
            {
                npcRuntime.SpatialKnowledge.DiscoverLocation(npcRuntime.CurrentLocation?.RuntimeId);
                arrivedNpcs.Add(npcRuntime);
                domainEventRecorder?.Record((eventId, absoluteDay, recordSequence) => new NpcArrivedEvent(
                    eventId,
                    absoluteDay,
                    recordSequence,
                    npcRuntime.RuntimeId,
                    npcRuntime.CurrentLocation?.RuntimeId,
                    originDecisionId));
                string destinationName = arrivedCity != null
                    ? arrivedCity.CityName
                    : npcRuntime.CurrentLocation?.RuntimeId ?? "destino desconhecido";
                logger.Log(SimulationLogCategory.Travel, $"{npcRuntime.NpcName} chegou em {destinationName}");
                continue;
            }

            string verb = npcRuntime.TravelDaysRemaining == 1 ? "Resta" : "Restam";
            string dayText = npcRuntime.TravelDaysRemaining == 1 ? "dia" : "dias";
            logger.Log(SimulationLogCategory.Travel, $"{npcRuntime.NpcName} esta viajando. {verb} {npcRuntime.TravelDaysRemaining} {dayText}.");
        }

        return arrivedNpcs.AsReadOnly();
    }

    public int GetTravelDays(CityRuntime originCity, CityRuntime targetCity)
    {
        return GetTravelDays(originCity?.Location, targetCity?.Location);
    }

    public int GetTravelDays(
        SpatialLocationRuntime originLocation,
        SpatialLocationRuntime targetLocation)
    {
        return TryGetDirectRoute(originLocation, targetLocation, out SpatialRouteRuntime route) == true
            ? route.TravelDays
            : -1;
    }

    public List<CityRuntime> GetDirectDestinationCities(CityRuntime originCity)
    {
        List<CityRuntime> destinationCities = new List<CityRuntime>();

        if (originCity == null || originCity.Location == null || spatialNetwork == null || getCityRuntimeByLocation == null)
        {
            return destinationCities;
        }

        HashSet<SpatialLocationRuntime> visitedDestinations = new HashSet<SpatialLocationRuntime>();

        foreach (SpatialRouteRuntime route in spatialNetwork.GetOutgoingRoutes(originCity.Location))
        {
            if (route == null || visitedDestinations.Add(route.Destination) == false)
            {
                continue;
            }

            if (spatialNetwork.TryGetSingleDirectRoute(originCity.Location, route.Destination, out _) == false)
            {
                continue;
            }

            CityRuntime destinationCity = getCityRuntimeByLocation(route.Destination);

            if (destinationCity != null)
            {
                destinationCities.Add(destinationCity);
            }
            else
            {
                logger.LogWarning($"Direct route '{route.RuntimeId}' points to location '{route.Destination.RuntimeId}', which is not associated with a CityRuntime.");
            }
        }

        destinationCities.Sort((left, right) => string.CompareOrdinal(
            left?.RuntimeId ?? string.Empty,
            right?.RuntimeId ?? string.Empty));

        return destinationCities;
    }

    public IReadOnlyList<SpatialRouteRuntime> GetKnownDirectRoutes(NpcRuntime npcRuntime, CityRuntime originCity)
    {
        return GetKnownDirectRoutes(npcRuntime, originCity?.Location);
    }

    public IReadOnlyList<SpatialRouteRuntime> GetKnownDirectRoutes(
        NpcRuntime npcRuntime,
        SpatialLocationRuntime originLocation)
    {
        List<SpatialRouteRuntime> knownRoutes = new List<SpatialRouteRuntime>();

        if (npcRuntime == null
            || originLocation == null
            || spatialNetwork == null
            || npcRuntime.SpatialKnowledge.KnowsLocation(originLocation.RuntimeId) == false)
        {
            return knownRoutes;
        }

        foreach (SpatialRouteRuntime route in spatialNetwork.GetOutgoingRoutes(originLocation))
        {
            if (route == null
                || npcRuntime.SpatialKnowledge.KnowsRoute(route.RuntimeId) == false
                || npcRuntime.SpatialKnowledge.KnowsLocation(route.Destination.RuntimeId) == false
                || spatialNetwork.TryGetSingleDirectRoute(originLocation, route.Destination, out SpatialRouteRuntime directRoute) == false
                || directRoute != route)
            {
                continue;
            }

            knownRoutes.Add(route);
        }

        knownRoutes.Sort((left, right) => string.CompareOrdinal(
            left?.RuntimeId ?? string.Empty,
            right?.RuntimeId ?? string.Empty));

        return knownRoutes.AsReadOnly();
    }

    public List<CityRuntime> GetKnownDirectDestinationCities(NpcRuntime npcRuntime, CityRuntime originCity)
    {
        List<CityRuntime> destinationCities = new List<CityRuntime>();

        foreach (SpatialRouteRuntime route in GetKnownDirectRoutes(npcRuntime, originCity))
        {
            CityRuntime destinationCity = GetCityRuntime(route.Destination);

            if (destinationCity != null)
            {
                destinationCities.Add(destinationCity);
            }
        }

        return destinationCities;
    }

    public bool CanPlanKnownTravel(NpcRuntime npcRuntime, CityRuntime targetCity, out int travelDays, out float travelCost)
    {
        return CanPlanKnownTravel(npcRuntime, targetCity?.Location, out travelDays, out travelCost);
    }

    public bool CanPlanKnownTravel(
        NpcRuntime npcRuntime,
        SpatialLocationRuntime targetLocation,
        out int travelDays,
        out float travelCost)
    {
        travelDays = -1;
        travelCost = -1f;

        if (npcRuntime == null
            || npcRuntime.CurrentLocation == null
            || targetLocation == null
            || npcRuntime.IsTraveling == true
            || TryGetKnownDirectRoute(npcRuntime, npcRuntime.CurrentLocation, targetLocation, out SpatialRouteRuntime route) == false)
        {
            return false;
        }

        travelDays = route.TravelDays;
        travelCost = GetTravelCost(travelDays);
        return npcRuntime.Money >= travelCost;
    }

    public bool TryGetKnownDirectRoute(
        NpcRuntime npcRuntime,
        CityRuntime originCity,
        CityRuntime targetCity,
        out SpatialRouteRuntime route)
    {
        return TryGetKnownDirectRoute(npcRuntime, originCity?.Location, targetCity?.Location, out route);
    }

    public bool TryGetKnownDirectRoute(
        NpcRuntime npcRuntime,
        SpatialLocationRuntime originLocation,
        SpatialLocationRuntime targetLocation,
        out SpatialRouteRuntime route)
    {
        route = null;

        if (npcRuntime == null
            || originLocation == null
            || targetLocation == null
            || npcRuntime.SpatialKnowledge.KnowsLocation(originLocation.RuntimeId) == false
            || npcRuntime.SpatialKnowledge.KnowsLocation(targetLocation.RuntimeId) == false
            || TryGetDirectRoute(originLocation, targetLocation, out route) == false)
        {
            route = null;
            return false;
        }

        if (npcRuntime.SpatialKnowledge.KnowsRoute(route.RuntimeId) == true)
        {
            return true;
        }

        route = null;
        return false;
    }

    public CityRuntime GetCityRuntime(SpatialLocationRuntime location)
    {
        return location != null && getCityRuntimeByLocation != null
            ? getCityRuntimeByLocation(location)
            : null;
    }

    public float GetTravelCost(CityRuntime originCity, CityRuntime targetCity)
    {
        return GetTravelCost(originCity?.Location, targetCity?.Location);
    }

    public float GetTravelCost(
        SpatialLocationRuntime originLocation,
        SpatialLocationRuntime targetLocation)
    {
        int travelDays = GetTravelDays(originLocation, targetLocation);

        if (travelDays <= 0)
        {
            return -1f;
        }

        return GetTravelCost(travelDays);
    }

    public float GetTravelCost(int travelDays)
    {
        return Mathf.Max(1, travelDays) * travelConfiguration.TravelCostPerDay;
    }

    public bool TryGetDirectRoute(
        SpatialLocationRuntime originLocation,
        SpatialLocationRuntime targetLocation,
        out SpatialRouteRuntime route)
    {
        route = null;

        if (originLocation == null || targetLocation == null || spatialNetwork == null)
        {
            return false;
        }

        return spatialNetwork.TryGetSingleDirectRoute(originLocation, targetLocation, out route);
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard)
            && (travelPartyStore == null || travelPartyStore.CanBindMutationGuard(guard));
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        if (!CanBindMutationGuard(guard))
        {
            return false;
        }

        if (!mutationGuardBinding.TryBindTo(guard))
        {
            return false;
        }

        return travelPartyStore == null || travelPartyStore.TryBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }

    private void ThrowIfFaulted()
    {
        if (!mutationGuardBinding.CanMutate)
        {
            throw new InvalidOperationException("A faulted SimulationRuntime cannot advance travel.");
        }
    }

    private bool IsManagedByTravelParty(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcRuntime.ActiveTravelPartyId) == false)
        {
            return true;
        }

        return travelPartyStore != null
            && travelPartyStore.TryGetPartyForNpc(npcRuntime.RuntimeId, out _);
    }

}
