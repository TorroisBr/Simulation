using System;
using UnityEngine;

/// <summary>Unity-only bindings for the shared SimulationLogger host ports.</summary>
internal static class UnitySimulationLoggerAdapter
{
    internal static SimulationLogger Create(SimulationLogSettings settings)
    {
        return new SimulationLogger(settings, Application.persistentDataPath, Write);
    }

    internal static SimulationLogger CreateSelected(DailyV1SimulationLogSettings settings)
    {
        if (settings == null)
            return Create((SimulationLogSettings)null);
        return Create(new SimulationLogSettings
        {
            showDay = settings.showDay,
            npcActions = settings.npcActions,
            trade = settings.trade,
            travel = settings.travel,
            crime = settings.crime,
            justice = settings.justice,
            economyProduction = settings.economyProduction,
            economyConsumption = settings.economyConsumption,
            market = settings.market
        });
    }

    private static void Write(DailyV1LogSeverity severity, string message)
    {
        switch (severity)
        {
            case DailyV1LogSeverity.Warning:
                Debug.LogWarning(message);
                break;
            case DailyV1LogSeverity.Error:
                Debug.LogError(message);
                break;
            default:
                Debug.Log(message);
                break;
        }
    }
}
