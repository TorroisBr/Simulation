using System;

public enum PopulationRepresentationMode
{
    Aggregate = 0,
    Hybrid = 1,
    FullyIndividualized = 2
}

public enum NpcDecisionSimulationScope
{
    RelevantOnly = 0,
    AllMaterialized = 1
}

public sealed class EffectivePopulationConfiguration : IEquatable<EffectivePopulationConfiguration>
{
    public PopulationRepresentationMode RepresentationMode { get; }
    public NpcDecisionSimulationScope DecisionScope { get; }

    public EffectivePopulationConfiguration(
        PopulationRepresentationMode representationMode,
        NpcDecisionSimulationScope decisionScope)
    {
        RepresentationMode = representationMode;
        DecisionScope = decisionScope;
    }

    public bool Equals(EffectivePopulationConfiguration other)
    {
        return other != null
            && RepresentationMode == other.RepresentationMode
            && DecisionScope == other.DecisionScope;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EffectivePopulationConfiguration);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)RepresentationMode * 397) ^ (int)DecisionScope;
        }
    }
}

public sealed class EffectiveEconomyConfiguration : IEquatable<EffectiveEconomyConfiguration>
{
    public bool Enabled { get; }

    public EffectiveEconomyConfiguration(bool enabled)
    {
        Enabled = enabled;
    }

    public bool Equals(EffectiveEconomyConfiguration other)
    {
        return other != null && Enabled == other.Enabled;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EffectiveEconomyConfiguration);
    }

    public override int GetHashCode()
    {
        return Enabled ? 1 : 0;
    }
}

public sealed class EffectiveTravelConfiguration : IEquatable<EffectiveTravelConfiguration>
{
    public float TravelCostPerDay { get; }

    public EffectiveTravelConfiguration(float travelCostPerDay)
    {
        TravelCostPerDay = travelCostPerDay;
    }

    public bool Equals(EffectiveTravelConfiguration other)
    {
        return other != null && TravelCostPerDay.Equals(other.TravelCostPerDay);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EffectiveTravelConfiguration);
    }

    public override int GetHashCode()
    {
        return TravelCostPerDay.GetHashCode();
    }
}

public sealed class EffectiveCrimeConfiguration : IEquatable<EffectiveCrimeConfiguration>
{
    public bool Enabled { get; }
    public bool AutonomousEnabled { get; }

    public EffectiveCrimeConfiguration(bool enabled, bool autonomousEnabled)
    {
        Enabled = enabled;
        AutonomousEnabled = autonomousEnabled;
    }

    public bool Equals(EffectiveCrimeConfiguration other)
    {
        return other != null
            && Enabled == other.Enabled
            && AutonomousEnabled == other.AutonomousEnabled;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EffectiveCrimeConfiguration);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Enabled ? 1 : 0) * 397 ^ (AutonomousEnabled ? 1 : 0);
        }
    }
}

public sealed class EffectiveGuardCrimeConfiguration : IEquatable<EffectiveGuardCrimeConfiguration>
{
    public bool Enabled { get; }

    public EffectiveGuardCrimeConfiguration(bool enabled)
    {
        Enabled = enabled;
    }

    public bool Equals(EffectiveGuardCrimeConfiguration other)
    {
        return other != null && Enabled == other.Enabled;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EffectiveGuardCrimeConfiguration);
    }

    public override int GetHashCode()
    {
        return Enabled ? 1 : 0;
    }
}

public sealed class EffectiveSimulationConfiguration : IEquatable<EffectiveSimulationConfiguration>
{
    public EffectivePopulationConfiguration Population { get; }
    public EffectiveEconomyConfiguration Economy { get; }
    public EffectiveTravelConfiguration Travel { get; }
    public EffectiveCrimeConfiguration Crime { get; }
    public EffectiveGuardCrimeConfiguration GuardCrime { get; }

    public EffectiveSimulationConfiguration(
        EffectivePopulationConfiguration population,
        EffectiveEconomyConfiguration economy,
        EffectiveTravelConfiguration travel,
        EffectiveCrimeConfiguration crime,
        EffectiveGuardCrimeConfiguration guardCrime)
    {
        Population = population;
        Economy = economy;
        Travel = travel;
        Crime = crime;
        GuardCrime = guardCrime;
    }

    public bool Equals(EffectiveSimulationConfiguration other)
    {
        return other != null
            && Equals(Population, other.Population)
            && Equals(Economy, other.Economy)
            && Equals(Travel, other.Travel)
            && Equals(Crime, other.Crime)
            && Equals(GuardCrime, other.GuardCrime);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EffectiveSimulationConfiguration);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Population == null ? 0 : Population.GetHashCode();
            hash = (hash * 397) ^ (Economy == null ? 0 : Economy.GetHashCode());
            hash = (hash * 397) ^ (Travel == null ? 0 : Travel.GetHashCode());
            hash = (hash * 397) ^ (Crime == null ? 0 : Crime.GetHashCode());
            return (hash * 397) ^ (GuardCrime == null ? 0 : GuardCrime.GetHashCode());
        }
    }
}

public sealed class PopulationConfigurationOverrides
{
    public PopulationRepresentationMode? RepresentationMode { get; }
    public NpcDecisionSimulationScope? DecisionScope { get; }

    public PopulationConfigurationOverrides(
        PopulationRepresentationMode? representationMode = null,
        NpcDecisionSimulationScope? decisionScope = null)
    {
        RepresentationMode = representationMode;
        DecisionScope = decisionScope;
    }
}

public sealed class EconomyConfigurationOverrides
{
    public bool? Enabled { get; }

    public EconomyConfigurationOverrides(bool? enabled = null)
    {
        Enabled = enabled;
    }
}

public sealed class TravelConfigurationOverrides
{
    public float? TravelCostPerDay { get; }

    public TravelConfigurationOverrides(float? travelCostPerDay = null)
    {
        TravelCostPerDay = travelCostPerDay;
    }
}

public sealed class CrimeConfigurationOverrides
{
    public bool? Enabled { get; }
    public bool? AutonomousEnabled { get; }

    public CrimeConfigurationOverrides(
        bool? enabled = null,
        bool? autonomousEnabled = null)
    {
        Enabled = enabled;
        AutonomousEnabled = autonomousEnabled;
    }
}

public sealed class GuardCrimeConfigurationOverrides
{
    public bool? Enabled { get; }

    public GuardCrimeConfigurationOverrides(bool? enabled = null)
    {
        Enabled = enabled;
    }
}

public sealed class SimulationConfigurationOverrides
{
    public PopulationConfigurationOverrides Population { get; }
    public EconomyConfigurationOverrides Economy { get; }
    public TravelConfigurationOverrides Travel { get; }
    public CrimeConfigurationOverrides Crime { get; }
    public GuardCrimeConfigurationOverrides GuardCrime { get; }

    public SimulationConfigurationOverrides(
        PopulationConfigurationOverrides population = null,
        EconomyConfigurationOverrides economy = null,
        TravelConfigurationOverrides travel = null,
        CrimeConfigurationOverrides crime = null,
        GuardCrimeConfigurationOverrides guardCrime = null)
    {
        Population = population ?? new PopulationConfigurationOverrides();
        Economy = economy ?? new EconomyConfigurationOverrides();
        Travel = travel ?? new TravelConfigurationOverrides();
        Crime = crime ?? new CrimeConfigurationOverrides();
        GuardCrime = guardCrime ?? new GuardCrimeConfigurationOverrides();
    }
}

public sealed class SimulationConfigurationPreset
{
    public string Name { get; }
    public SimulationConfigurationOverrides Overrides { get; }

    public SimulationConfigurationPreset(
        string name,
        SimulationConfigurationOverrides overrides = null)
    {
        Name = name ?? string.Empty;
        Overrides = overrides ?? new SimulationConfigurationOverrides();
    }
}
