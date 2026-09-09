using System;
using UnityEngine;

public class SimulationLogger
{
    private readonly SimulationLogSettings settings;

    public SimulationLogger(SimulationLogSettings settings)
    {
        this.settings = settings ?? new SimulationLogSettings();
    }

    public void Log(SimulationLogCategory category, string message)
    {
        if (string.IsNullOrEmpty(message) == true || IsEnabled(category) == false)
        {
            return;
        }

        Debug.Log(message);
    }

    public void LogWarning(string message)
    {
        if (string.IsNullOrEmpty(message) == false)
        {
            Debug.LogWarning(message);
        }
    }

    public void LogError(string message)
    {
        if (string.IsNullOrEmpty(message) == false)
        {
            Debug.LogError(message);
        }
    }

    public bool IsEnabled(SimulationLogCategory category)
    {
        switch (category)
        {
            case SimulationLogCategory.Day:
                return settings.showDay;
            case SimulationLogCategory.NpcAction:
                return settings.npcActions;
            case SimulationLogCategory.EconomyProduction:
                return settings.economyProduction;
            case SimulationLogCategory.EconomyConsumption:
                return settings.economyConsumption;
            case SimulationLogCategory.Market:
                return settings.market;
            case SimulationLogCategory.Trade:
                return settings.trade;
            case SimulationLogCategory.Travel:
                return settings.travel;
            case SimulationLogCategory.Crime:
                return settings.crime;
            case SimulationLogCategory.Justice:
                return settings.justice;
            default:
                return true;
        }
    }
}

[Serializable]
public class SimulationLogSettings
{
    public bool showDay = true;
    public bool npcActions = true;
    public bool trade = true;
    public bool travel = true;
    public bool crime = true;
    public bool justice = true;
    public bool economyProduction = true;
    public bool economyConsumption = true;
    public bool market = true;
}

public enum SimulationLogCategory
{
    Day,
    NpcAction,
    EconomyProduction,
    EconomyConsumption,
    Market,
    Trade,
    Travel,
    Crime,
    Justice
}
