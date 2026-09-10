using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "World Simulation/City")]
public class CityData : ScriptableObject
{
    public string id;
    public string cityName;
    public int initialPopulation = 1000;

    public string DefinitionId => id;

    public List<MarketItemConfig> marketItems = new List<MarketItemConfig>();
    public List<CityProductionConfig> productionConfigs = new List<CityProductionConfig>();
    public List<CityConnection> connections = new List<CityConnection>();
}

[Serializable]
public class MarketItemConfig
{
    public ItemData item;
    public int initialAmount = 100;
    public int desiredAmount = 100;
    public float consumptionPer1000Population;
}

[Serializable]
public class CityProductionConfig
{
    public ItemData item;
    public int amountPerDay;
}

[Serializable]
public class CityConnection
{
    public CityData destination;
    public int travelDays = 1;
}
