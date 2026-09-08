using System.Collections.Generic;
using UnityEngine;

public class SimulationModuleSet
{
    private readonly HashSet<SimulationModule> enabledModules = new HashSet<SimulationModule>();

    public SimulationModuleSet(SimulationConfigData config)
    {
        if (config != null)
        {
            foreach (SimulationModule module in config.EnabledModules)
            {
                enabledModules.Add(module);
            }
        }

        NormalizeDependencies();
    }

    public bool IsEnabled(SimulationModule module)
    {
        return enabledModules.Contains(module);
    }

    private void NormalizeDependencies()
    {
        if (enabledModules.Contains(SimulationModule.Merchant) == true && enabledModules.Contains(SimulationModule.Economy) == false)
        {
            enabledModules.Remove(SimulationModule.Merchant);
            Debug.LogWarning("Modulo Merchant desabilitado porque Economy nao esta ativo.");
        }
    }
}
