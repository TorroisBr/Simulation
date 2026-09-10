using System;
using System.Collections.Generic;
using UnityEngine;

public class TravelSystem
{
    private readonly SpatialNetworkRuntime spatialNetwork;
    private readonly Func<SpatialLocationRuntime, CityRuntime> getCityRuntimeByLocation;
    private readonly float travelCostPerDay;
    private readonly SimulationLogger logger;

    public TravelSystem(
        SpatialNetworkRuntime spatialNetwork,
        Func<SpatialLocationRuntime, CityRuntime> getCityRuntimeByLocation,
        float travelCostPerDay = 0f,
        SimulationLogger logger = null)
    {
        this.spatialNetwork = spatialNetwork;
        this.getCityRuntimeByLocation = getCityRuntimeByLocation;
        this.travelCostPerDay = Mathf.Max(0f, travelCostPerDay);
        this.logger = logger ?? new SimulationLogger(null);
    }

    public bool TryStartTravel(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (npcRuntime == null || npcRuntime.CurrentCity == null || actionRuntime == null || actionRuntime.TargetCity == null)
        {
            return false;
        }

        int travelDays = GetTravelDays(npcRuntime.CurrentCity, actionRuntime.TargetCity);

        if (travelDays <= 0)
        {
            return false;
        }

        float travelCost = GetTravelCost(npcRuntime.CurrentCity, actionRuntime.TargetCity);

        if (travelCost < 0f || npcRuntime.Money < travelCost)
        {
            return false;
        }

        CityRuntime originCity = npcRuntime.CurrentCity;

        if (npcRuntime.StartTravel(actionRuntime.TargetCity, travelDays) == false)
        {
            return false;
        }

        npcRuntime.TrySpendMoney(travelCost);
        logger.Log(SimulationLogCategory.Travel, $"{npcRuntime.NpcName} iniciou viagem de {originCity.CityName} para {actionRuntime.TargetCity.CityName}");

        if (travelCost > 0f)
        {
            logger.Log(SimulationLogCategory.Travel, $"Custo de viagem: {travelCost:0.##}");
        }

        return true;
    }

    public bool CanStartTravel(NpcRuntime npcRuntime, CityRuntime targetCity, out int travelDays, out float travelCost)
    {
        travelDays = -1;
        travelCost = -1f;

        if (npcRuntime == null || npcRuntime.CurrentCity == null || targetCity == null || npcRuntime.IsTraveling == true)
        {
            return false;
        }

        travelDays = GetTravelDays(npcRuntime.CurrentCity, targetCity);

        if (travelDays <= 0)
        {
            return false;
        }

        travelCost = GetTravelCost(travelDays);
        return npcRuntime.Money >= travelCost;
    }

    public void AdvanceTravels(List<NpcRuntime> npcRuntimeList)
    {
        if (npcRuntimeList == null)
        {
            return;
        }

        foreach (NpcRuntime npcRuntime in npcRuntimeList)
        {
            if (npcRuntime == null || npcRuntime.IsTraveling == false)
            {
                continue;
            }

            if (npcRuntime.TravelStartedToday == true)
            {
                npcRuntime.ClearTravelStartedToday();
                continue;
            }

            bool arrived = npcRuntime.AdvanceTravelDay(out CityRuntime arrivedCity);

            if (arrived == true)
            {
                string cityName = arrivedCity != null ? arrivedCity.CityName : "destino desconhecido";
                logger.Log(SimulationLogCategory.Travel, $"{npcRuntime.NpcName} chegou em {cityName}");
                continue;
            }

            string verb = npcRuntime.TravelDaysRemaining == 1 ? "Resta" : "Restam";
            string dayText = npcRuntime.TravelDaysRemaining == 1 ? "dia" : "dias";
            logger.Log(SimulationLogCategory.Travel, $"{npcRuntime.NpcName} esta viajando. {verb} {npcRuntime.TravelDaysRemaining} {dayText}.");
        }
    }

    public int GetTravelDays(CityRuntime originCity, CityRuntime targetCity)
    {
        if (originCity == null || targetCity == null || originCity.Location == null || targetCity.Location == null || spatialNetwork == null)
        {
            return -1;
        }

        return spatialNetwork.TryGetSingleDirectRoute(originCity.Location, targetCity.Location, out SpatialRouteRuntime route) == true
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

        return destinationCities;
    }

    public float GetTravelCost(CityRuntime originCity, CityRuntime targetCity)
    {
        int travelDays = GetTravelDays(originCity, targetCity);

        if (travelDays <= 0)
        {
            return -1f;
        }

        return GetTravelCost(travelDays);
    }

    public float GetTravelCost(int travelDays)
    {
        return Mathf.Max(1, travelDays) * travelCostPerDay;
    }

}
