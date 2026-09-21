using System.Collections.Generic;

public class SimulationModuleSet
{
    private readonly HashSet<SimulationModule> requestedModules = new HashSet<SimulationModule>();

    public SimulationModuleSet(SimulationConfigData config, SimulationLogger logger = null)
    {
        if (config != null)
        {
            foreach (SimulationModule module in config.EnabledModules)
            {
                requestedModules.Add(module);
            }
        }
    }

    // This is a bootstrap compatibility view only. EffectiveSimulationConfiguration
    // is the semantic authority after composition.
    public bool IsEnabled(SimulationModule module)
    {
        return requestedModules.Contains(module);
    }
}
