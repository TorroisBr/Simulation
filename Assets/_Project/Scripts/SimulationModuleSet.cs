using System.Collections.Generic;

public class SimulationModuleSet
{
    private readonly HashSet<SimulationModule> enabledModules = new HashSet<SimulationModule>();
    private readonly SimulationLogger logger;

    public SimulationModuleSet(SimulationConfigData config, SimulationLogger logger = null)
    {
        this.logger = logger ?? new SimulationLogger(null);

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
            logger.LogWarning("Modulo Merchant desabilitado porque Economy nao esta ativo.");
        }
    }
}
