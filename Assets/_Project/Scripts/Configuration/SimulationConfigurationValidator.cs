using System;
using System.Collections.Generic;

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

        ValidatePopulation(configuration.Population, errors);
        ValidateEconomy(configuration.Economy, errors);
        ValidateTravel(configuration.Travel, errors);
        ValidateCrime(configuration.Crime, errors);
        ValidateGuardCrime(configuration.GuardCrime, errors);
        return new SimulationConfigurationValidationResult(errors);
    }

    private static void ValidatePopulation(
        EffectivePopulationConfiguration population,
        List<string> errors)
    {
        if (population == null)
        {
            errors.Add("Population configuration is required.");
            return;
        }

        if (population.RepresentationMode != PopulationRepresentationMode.Aggregate
            && population.RepresentationMode != PopulationRepresentationMode.Hybrid
            && population.RepresentationMode != PopulationRepresentationMode.FullyIndividualized)
        {
            errors.Add("Population representation mode has an unknown value.");
        }

        if (population.DecisionScope != NpcDecisionSimulationScope.RelevantOnly
            && population.DecisionScope != NpcDecisionSimulationScope.AllMaterialized)
        {
            errors.Add("NPC decision simulation scope has an unknown value.");
        }

        if (population.MaturityAgeYears < 0L)
        {
            errors.Add("Maturity age in years cannot be negative.");
        }
    }

    private static void ValidateEconomy(
        EffectiveEconomyConfiguration economy,
        List<string> errors)
    {
        if (economy == null)
        {
            errors.Add("Economy configuration is required.");
        }
    }

    private static void ValidateTravel(
        EffectiveTravelConfiguration travel,
        List<string> errors)
    {
        if (travel == null)
        {
            errors.Add("Travel configuration is required.");
            return;
        }

        if (float.IsNaN(travel.TravelCostPerDay) || float.IsInfinity(travel.TravelCostPerDay))
        {
            errors.Add("Travel cost per day must be finite.");
        }
        else if (travel.TravelCostPerDay < 0f)
        {
            errors.Add("Travel cost per day cannot be negative.");
        }
    }

    private static void ValidateCrime(
        EffectiveCrimeConfiguration crime,
        List<string> errors)
    {
        if (crime == null)
        {
            errors.Add("Crime configuration is required.");
            return;
        }

        if (crime.AutonomousEnabled && !crime.Enabled)
        {
            errors.Add("Crime cannot be autonomous when it is disabled.");
        }
    }

    private static void ValidateGuardCrime(
        EffectiveGuardCrimeConfiguration guardCrime,
        List<string> errors)
    {
        if (guardCrime == null)
        {
            errors.Add("Guard crime configuration is required.");
        }
    }
}
