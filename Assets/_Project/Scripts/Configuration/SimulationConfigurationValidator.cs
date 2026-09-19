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
        ValidateNaturalMortality(configuration.NaturalMortality, errors);
        ValidateAggregateDemography(configuration.AggregateDemography, errors);
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

    private static void ValidateNaturalMortality(
        EffectiveNaturalMortalityConfiguration naturalMortality,
        List<string> errors)
    {
        if (naturalMortality == null)
        {
            errors.Add("Natural mortality configuration is required.");
            return;
        }

        if (naturalMortality.Policy != NaturalMortalityPolicy.Disabled
            && naturalMortality.Policy != NaturalMortalityPolicy.ConfiguredAnnualProbability)
        {
            errors.Add("Natural mortality policy has an unknown value.");
        }

        if (double.IsNaN(naturalMortality.AnnualProbability)
            || double.IsInfinity(naturalMortality.AnnualProbability))
        {
            errors.Add("Natural mortality annual probability must be finite.");
        }
        else if (naturalMortality.AnnualProbability < 0d
            || naturalMortality.AnnualProbability > 1d)
        {
            errors.Add("Natural mortality annual probability must be between zero and one.");
        }
    }

    private static void ValidateAggregateDemography(
        EffectiveAggregateDemographyConfiguration aggregateDemography,
        List<string> errors)
    {
        if (aggregateDemography == null)
        {
            errors.Add("Aggregate demography configuration is required.");
            return;
        }

        if (aggregateDemography.Policy != AggregateDemographyPolicy.Disabled
            && aggregateDemography.Policy != AggregateDemographyPolicy.ConfiguredAnnualRates)
        {
            errors.Add("Aggregate demography policy has an unknown value.");
        }

        ValidateNonNegativeFinite(
            aggregateDemography.AnnualBirthRate,
            "Aggregate demography annual birth rate",
            errors);
        ValidateNonNegativeFinite(
            aggregateDemography.AnnualDeathRate,
            "Aggregate demography annual death rate",
            errors);
    }

    private static void ValidateNonNegativeFinite(
        double value,
        string label,
        List<string> errors)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            errors.Add(label + " must be finite.");
        }
        else if (value < 0d)
        {
            errors.Add(label + " cannot be negative.");
        }
    }
}
