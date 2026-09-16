using System;
using UnityEngine;

public sealed class WorldObserverTimeController : MonoBehaviour
{
    private SimulationRuntime simulationRuntime;
    private Action<int> advanceDaysAction;

    public void Bind(SimulationRuntime runtime)
    {
        simulationRuntime = runtime;
        advanceDaysAction = null;
    }

    public void SetAdvanceDaysCallback(Action<int> callback)
    {
        advanceDaysAction = callback;
        simulationRuntime = null;
    }

    public bool AdvanceOneDay()
    {
        if (advanceDaysAction != null)
        {
            advanceDaysAction(1);
            return true;
        }

        if (simulationRuntime == null)
        {
            return false;
        }

        simulationRuntime.AdvanceDay();
        return true;
    }
}
