using System.Collections.Generic;

/// <summary>
/// Deterministic, Unity-independent validator for effective simulation configuration.
/// </summary>
public static class SimulationConfigurationValidator
{
    public static SimulationConfigurationValidationResult Validate(
        EffectiveSimulationConfiguration configuration)
    {
        List<string> errors = new List<string>();

        if (configuration == null)
        {
            errors.Add("Configuration is required.");
            return new SimulationConfigurationValidationResult(errors);
        }

        AddPopulationErrors(errors, configuration.Population);
        AddDomainPolicyErrors(errors, "MobilityPolicy", configuration.MobilityPolicy);
        AddDomainPolicyErrors(errors, "CrimePolicy", configuration.CrimePolicy);
        AddDomainPolicyErrors(errors, "EconomyPolicy", configuration.EconomyPolicy);
        AddDomainPolicyErrors(errors, "AdventurePolicy", configuration.AdventurePolicy);

        return new SimulationConfigurationValidationResult(errors);
    }

    public static SimulationConfigurationValidationResult ValidateDomainPolicy(
        SimulationDomainPolicy policy)
    {
        List<string> errors = new List<string>();
        AddDomainPolicyErrors(errors, "DomainPolicy", policy);
        return new SimulationConfigurationValidationResult(errors);
    }

    public static SimulationConfigurationValidationResult ValidatePopulation(
        EffectivePopulationConfiguration population)
    {
        List<string> errors = new List<string>();
        AddPopulationErrors(errors, population);
        return new SimulationConfigurationValidationResult(errors);
    }

    private static void AddDomainPolicyErrors(
        List<string> errors,
        string propertyName,
        SimulationDomainPolicy policy)
    {
        if (policy == null)
        {
            errors.Add(propertyName + " is required.");
            return;
        }

        foreach (string error in policy.GetValidationErrors())
        {
            errors.Add(propertyName + ": " + error);
        }
    }

    private static void AddPopulationErrors(
        List<string> errors,
        EffectivePopulationConfiguration population)
    {
        if (population == null)
        {
            errors.Add("Population is required.");
            return;
        }

        if (IsKnownRepresentationMode(population.RepresentationMode) == false)
        {
            errors.Add("Population representation mode has an unknown value.");
        }

        if (IsKnownDecisionScope(population.DecisionScope) == false)
        {
            errors.Add("NPC decision simulation scope has an unknown value.");
        }
    }

    private static bool IsKnownRepresentationMode(PopulationRepresentationMode mode)
    {
        return mode == PopulationRepresentationMode.Aggregate
            || mode == PopulationRepresentationMode.Hybrid
            || mode == PopulationRepresentationMode.FullyIndividualized;
    }

    private static bool IsKnownDecisionScope(NpcDecisionSimulationScope scope)
    {
        return scope == NpcDecisionSimulationScope.RelevantOnly
            || scope == NpcDecisionSimulationScope.AllMaterialized;
    }
}
