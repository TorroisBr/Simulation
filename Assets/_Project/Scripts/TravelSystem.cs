using System;
using System.Collections.Generic;
using UnityEngine;

public class TravelSystem
{
    private readonly Func<CityData, CityRuntime> getCityRuntime;

    public TravelSystem(Func<CityData, CityRuntime> getCityRuntime)
    {
        this.getCityRuntime = getCityRuntime;
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

        CityRuntime originCity = npcRuntime.CurrentCity;

        if (npcRuntime.StartTravel(actionRuntime.TargetCity, travelDays) == false)
        {
            return false;
        }

        Debug.Log($"{npcRuntime.NpcName} iniciou viagem de {originCity.CityName} para {actionRuntime.TargetCity.CityName}");
        return true;
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
                Debug.Log($"{npcRuntime.NpcName} chegou em {cityName}");
                continue;
            }

            string verb = npcRuntime.TravelDaysRemaining == 1 ? "Resta" : "Restam";
            string dayText = npcRuntime.TravelDaysRemaining == 1 ? "dia" : "dias";
            Debug.Log($"{npcRuntime.NpcName} esta viajando. {verb} {npcRuntime.TravelDaysRemaining} {dayText}.");
        }
    }

    public int GetTravelDays(CityRuntime originCity, CityRuntime targetCity)
    {
        if (originCity == null || targetCity == null || originCity.CityData == null || originCity.CityData.connections == null)
        {
            return -1;
        }

        foreach (CityConnection connection in originCity.CityData.connections)
        {
            if (connection == null || connection.destination == null)
            {
                continue;
            }

            if (connection.destination == targetCity.CityData)
            {
                return Mathf.Max(1, connection.travelDays);
            }
        }

        return -1;
    }

    public CityRuntime GetCityRuntime(CityData cityData)
    {
        if (cityData == null || getCityRuntime == null)
        {
            return null;
        }

        return getCityRuntime(cityData);
    }
}
