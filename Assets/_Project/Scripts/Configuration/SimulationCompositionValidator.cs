using System.Collections.Generic;

public sealed class SimulationCompositionCapabilities
{
    public bool MerchantTradeAvailable { get; }
    public bool CrimeAvailable { get; }
    public bool GuardCrimeAvailable { get; }

    public SimulationCompositionCapabilities(
        bool merchantTradeAvailable,
        bool crimeAvailable,
        bool guardCrimeAvailable)
    {
        MerchantTradeAvailable = merchantTradeAvailable;
        CrimeAvailable = crimeAvailable;
        GuardCrimeAvailable = guardCrimeAvailable;
    }
}

public static class SimulationCompositionValidator
{
    public static IReadOnlyList<string> Validate(
        EffectiveSimulationConfiguration configuration,
        SimulationCompositionCapabilities capabilities)
    {
        List<string> errors = new List<string>();

        if (configuration == null)
        {
            errors.Add("Effective simulation configuration is required for composition.");
            return errors.AsReadOnly();
        }

        if (capabilities == null)
        {
            errors.Add("Simulation composition capabilities are required.");
            return errors.AsReadOnly();
        }

        if (configuration.MerchantTrade.Enabled && !capabilities.MerchantTradeAvailable)
        {
            errors.Add("Merchant trade is enabled but no compatible merchant capability is available.");
        }

        if (configuration.Crime.Enabled && !capabilities.CrimeAvailable)
        {
            errors.Add("Crime is enabled but no compatible crime capability is available.");
        }

        if (configuration.GuardCrime.Enabled && !capabilities.GuardCrimeAvailable)
        {
            errors.Add("Guard crime is enabled but no compatible guard-crime capability is available.");
        }

        return errors.AsReadOnly();
    }
}
