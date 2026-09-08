using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CityRuntime
{
    [SerializeField] private CityData cityData;
    [SerializeField] private int currentPopulation;
    [SerializeField] private MarketRuntime market;
    [NonSerialized] private List<NpcRuntime> importantNpcs = new List<NpcRuntime>();

    public CityData CityData => cityData;
    public int CurrentPopulation => currentPopulation;
    public MarketRuntime Market => market ?? (market = new MarketRuntime());
    public List<NpcRuntime> ImportantNpcs => importantNpcs ?? (importantNpcs = new List<NpcRuntime>());
    public string CityName => cityData != null ? cityData.cityName : "Cidade desconhecida";

    public CityRuntime(CityData cityData)
    {
        this.cityData = cityData;
        currentPopulation = cityData != null ? Mathf.Max(0, cityData.initialPopulation) : 0;
        market = cityData != null ? new MarketRuntime(cityData.marketItems) : new MarketRuntime();
    }

    public void SimulateProductionDay()
    {
        if (cityData == null || cityData.productionConfigs == null)
        {
            return;
        }

        foreach (CityProductionConfig production in cityData.productionConfigs)
        {
            if (production.item == null || production.amountPerDay <= 0)
            {
                continue;
            }

            Market.AddStock(production.item, production.amountPerDay);
            Debug.Log($"{CityName} produziu {production.amountPerDay} {production.item.itemName}");
        }
    }

    public void SimulateConsumptionDay()
    {
        if (cityData == null || cityData.marketItems == null)
        {
            return;
        }

        foreach (MarketItemConfig config in cityData.marketItems)
        {
            if (config.item == null || config.consumptionPer1000Population <= 0f)
            {
                continue;
            }

            int desiredConsumption = Mathf.RoundToInt(currentPopulation / 1000f * config.consumptionPer1000Population);
            int consumed = Market.RemoveStockUpTo(config.item, desiredConsumption);

            if (consumed > 0)
            {
                Debug.Log($"{CityName} consumiu {consumed} {config.item.itemName}");
            }
        }
    }

    public void UpdateMarketPrices()
    {
        Market.UpdatePrices();
    }

    public void AddImportantNpc(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return;
        }

        if (ImportantNpcs.Contains(npcRuntime) == false)
        {
            ImportantNpcs.Add(npcRuntime);
        }

        npcRuntime.SetCurrentCity(this);
    }

    public void RemoveImportantNpc(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return;
        }

        ImportantNpcs.Remove(npcRuntime);

        if (npcRuntime.CurrentCity == this)
        {
            npcRuntime.SetCurrentCity(null);
        }
    }
}
