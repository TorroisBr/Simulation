using System;
using System.Collections.Generic;

public sealed class SimulationConfigurationResolutionResult
{
    public EffectiveSimulationConfiguration Configuration { get; }
    public SimulationConfigurationValidationResult Validation { get; }
    public bool IsValid => Validation != null && Validation.IsValid;

    internal SimulationConfigurationResolutionResult(
        EffectiveSimulationConfiguration configuration,
        SimulationConfigurationValidationResult validation)
    {
        Configuration = configuration;
        Validation = validation;
    }
}

public static class SimulationConfigurationResolver
{
    public static SimulationConfigurationResolutionResult Resolve(
        SimulationConfigurationPreset preset = null,
        SimulationConfigurationOverrides worldOverrides = null,
        SimulationConfigurationOverrides contentOverrides = null)
    {
        EffectiveSimulationConfiguration resolved = SimulationConfigurationDefaults.Create();
        resolved = Apply(resolved, preset != null ? preset.Overrides : null);
        resolved = Apply(resolved, worldOverrides);
        resolved = Apply(resolved, contentOverrides);

        SimulationConfigurationValidationResult validation =
            SimulationConfigurationValidator.Validate(resolved);

        return new SimulationConfigurationResolutionResult(
            validation.IsValid ? resolved : null,
            validation);
    }

    public static bool TryResolve(
        SimulationConfigurationPreset preset,
        SimulationConfigurationOverrides worldOverrides,
        SimulationConfigurationOverrides contentOverrides,
        out EffectiveSimulationConfiguration configuration,
        out SimulationConfigurationValidationResult validation)
    {
        SimulationConfigurationResolutionResult result = Resolve(
            preset,
            worldOverrides,
            contentOverrides);
        configuration = result.Configuration;
        validation = result.Validation;
        return result.IsValid;
    }

    public static EffectiveSimulationConfiguration ResolveOrThrow(
        SimulationConfigurationPreset preset = null,
        SimulationConfigurationOverrides worldOverrides = null,
        SimulationConfigurationOverrides contentOverrides = null)
    {
        SimulationConfigurationResolutionResult result = Resolve(
            preset,
            worldOverrides,
            contentOverrides);
        if (!result.IsValid)
        {
            throw new ArgumentException(
                "Simulation configuration is invalid: " + string.Join("; ", result.Validation.Errors));
        }

        return result.Configuration;
    }

    private static EffectiveSimulationConfiguration Apply(
        EffectiveSimulationConfiguration current,
        SimulationConfigurationOverrides overrides)
    {
        if (overrides == null)
        {
            return current;
        }

        PopulationRepresentationMode representationMode = current.Population.RepresentationMode;
        if (overrides.Population.RepresentationMode.HasValue)
        {
            representationMode = overrides.Population.RepresentationMode.Value;
        }

        NpcDecisionSimulationScope decisionScope = current.Population.DecisionScope;
        if (overrides.Population.DecisionScope.HasValue)
        {
            decisionScope = overrides.Population.DecisionScope.Value;
        }

        long maturityAgeYears = current.Population.MaturityAgeYears;
        if (overrides.Population.MaturityAgeYears.HasValue)
        {
            maturityAgeYears = overrides.Population.MaturityAgeYears.Value;
        }

        bool economyEnabled = current.Economy.Enabled;
        if (overrides.Economy.Enabled.HasValue)
        {
            economyEnabled = overrides.Economy.Enabled.Value;
        }

        float travelCostPerDay = current.Travel.TravelCostPerDay;
        if (overrides.Travel.TravelCostPerDay.HasValue)
        {
            travelCostPerDay = overrides.Travel.TravelCostPerDay.Value;
        }

        bool crimeEnabled = current.Crime.Enabled;
        if (overrides.Crime.Enabled.HasValue)
        {
            crimeEnabled = overrides.Crime.Enabled.Value;
        }

        bool autonomousCrimeEnabled = current.Crime.AutonomousEnabled;
        if (overrides.Crime.AutonomousEnabled.HasValue)
        {
            autonomousCrimeEnabled = overrides.Crime.AutonomousEnabled.Value;
        }

        bool guardCrimeEnabled = current.GuardCrime.Enabled;
        if (overrides.GuardCrime.Enabled.HasValue)
        {
            guardCrimeEnabled = overrides.GuardCrime.Enabled.Value;
        }

        return new EffectiveSimulationConfiguration(
            new EffectivePopulationConfiguration(representationMode, decisionScope, maturityAgeYears),
            new EffectiveEconomyConfiguration(economyEnabled),
            new EffectiveTravelConfiguration(travelCostPerDay),
            new EffectiveCrimeConfiguration(crimeEnabled, autonomousCrimeEnabled),
            new EffectiveGuardCrimeConfiguration(guardCrimeEnabled));
    }
}
