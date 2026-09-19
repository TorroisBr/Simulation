using System;

public enum NaturalMortalityPolicy
{
    Disabled = 0,
    ConfiguredAnnualProbability = 1
}

public enum AggregateDemographyPolicy
{
    Disabled = 0,
    ConfiguredAnnualRates = 1
}

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
    public long MaturityAgeYears { get; }

    public EffectivePopulationConfiguration(
        PopulationRepresentationMode representationMode,
        NpcDecisionSimulationScope decisionScope,
        long maturityAgeYears = SimulationConfigurationDefaults.DefaultMaturityAgeYears)
    {
        RepresentationMode = representationMode;
        DecisionScope = decisionScope;
        MaturityAgeYears = maturityAgeYears;
    }

    public bool Equals(EffectivePopulationConfiguration other)
    {
        return other != null
            && RepresentationMode == other.RepresentationMode
            && DecisionScope == other.DecisionScope
            && MaturityAgeYears == other.MaturityAgeYears;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EffectivePopulationConfiguration);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ((int)RepresentationMode * 397) ^ (int)DecisionScope;
            return (hash * 397) ^ MaturityAgeYears.GetHashCode();
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

public sealed class EffectiveNaturalMortalityConfiguration : IEquatable<EffectiveNaturalMortalityConfiguration>
{
    public NaturalMortalityPolicy Policy { get; }
    public bool Enabled => Policy != NaturalMortalityPolicy.Disabled;
    public double AnnualProbability { get; }

    public EffectiveNaturalMortalityConfiguration(
        bool enabled = false,
        double annualProbability = 0d)
        : this(
            enabled ? NaturalMortalityPolicy.ConfiguredAnnualProbability : NaturalMortalityPolicy.Disabled,
            annualProbability)
    {
    }

    public EffectiveNaturalMortalityConfiguration(
        NaturalMortalityPolicy policy,
        double annualProbability = 0d)
    {
        Policy = policy;
        AnnualProbability = annualProbability;
    }

    public bool Equals(EffectiveNaturalMortalityConfiguration other)
    {
        return other != null
            && Policy == other.Policy
            && AnnualProbability.Equals(other.AnnualProbability);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EffectiveNaturalMortalityConfiguration);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Policy * 397) ^ AnnualProbability.GetHashCode();
        }
    }
}

public sealed class EffectiveAggregateDemographyConfiguration : IEquatable<EffectiveAggregateDemographyConfiguration>
{
    public AggregateDemographyPolicy Policy { get; }
    public bool Enabled => Policy != AggregateDemographyPolicy.Disabled;
    public double AnnualBirthRate { get; }
    public double AnnualDeathRate { get; }
    public double AnnualBirthRatePerYear => AnnualBirthRate;
    public double AnnualDeathRatePerYear => AnnualDeathRate;

    public EffectiveAggregateDemographyConfiguration(
        bool enabled = false,
        double annualBirthRate = 0d,
        double annualDeathRate = 0d)
        : this(
            enabled ? AggregateDemographyPolicy.ConfiguredAnnualRates : AggregateDemographyPolicy.Disabled,
            annualBirthRate,
            annualDeathRate)
    {
    }

    public EffectiveAggregateDemographyConfiguration(
        AggregateDemographyPolicy policy,
        double annualBirthRate = 0d,
        double annualDeathRate = 0d)
    {
        Policy = policy;
        AnnualBirthRate = annualBirthRate;
        AnnualDeathRate = annualDeathRate;
    }

    public bool Equals(EffectiveAggregateDemographyConfiguration other)
    {
        return other != null
            && Policy == other.Policy
            && AnnualBirthRate.Equals(other.AnnualBirthRate)
            && AnnualDeathRate.Equals(other.AnnualDeathRate);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EffectiveAggregateDemographyConfiguration);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ((int)Policy * 397) ^ AnnualBirthRate.GetHashCode();
            return (hash * 397) ^ AnnualDeathRate.GetHashCode();
        }
    }
}

public sealed class EffectiveSimulationConfiguration : IEquatable<EffectiveSimulationConfiguration>
{
    public EffectivePopulationConfiguration Population { get; }
    public EffectiveEconomyConfiguration Economy { get; }
    public EffectiveTravelConfiguration Travel { get; }
    public EffectiveCrimeConfiguration Crime { get; }
    public EffectiveGuardCrimeConfiguration GuardCrime { get; }
    public EffectiveNaturalMortalityConfiguration NaturalMortality { get; }
    public EffectiveAggregateDemographyConfiguration AggregateDemography { get; }

    public EffectiveSimulationConfiguration(
        EffectivePopulationConfiguration population,
        EffectiveEconomyConfiguration economy,
        EffectiveTravelConfiguration travel,
        EffectiveCrimeConfiguration crime,
        EffectiveGuardCrimeConfiguration guardCrime,
        EffectiveNaturalMortalityConfiguration naturalMortality = null,
        EffectiveAggregateDemographyConfiguration aggregateDemography = null)
    {
        Population = population;
        Economy = economy;
        Travel = travel;
        Crime = crime;
        GuardCrime = guardCrime;
        NaturalMortality = naturalMortality ?? new EffectiveNaturalMortalityConfiguration();
        AggregateDemography = aggregateDemography ?? new EffectiveAggregateDemographyConfiguration();
    }

    public bool Equals(EffectiveSimulationConfiguration other)
    {
        return other != null
            && Equals(Population, other.Population)
            && Equals(Economy, other.Economy)
            && Equals(Travel, other.Travel)
            && Equals(Crime, other.Crime)
            && Equals(GuardCrime, other.GuardCrime)
            && Equals(NaturalMortality, other.NaturalMortality)
            && Equals(AggregateDemography, other.AggregateDemography);
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
            hash = (hash * 397) ^ (GuardCrime == null ? 0 : GuardCrime.GetHashCode());
            hash = (hash * 397) ^ (NaturalMortality == null ? 0 : NaturalMortality.GetHashCode());
            return (hash * 397) ^ (AggregateDemography == null ? 0 : AggregateDemography.GetHashCode());
        }
    }
}

public sealed class PopulationConfigurationOverrides
{
    public PopulationRepresentationMode? RepresentationMode { get; }
    public NpcDecisionSimulationScope? DecisionScope { get; }
    public long? MaturityAgeYears { get; }

    public PopulationConfigurationOverrides(
        PopulationRepresentationMode? representationMode = null,
        NpcDecisionSimulationScope? decisionScope = null,
        long? maturityAgeYears = null)
    {
        RepresentationMode = representationMode;
        DecisionScope = decisionScope;
        MaturityAgeYears = maturityAgeYears;
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

public sealed class NaturalMortalityConfigurationOverrides
{
    public NaturalMortalityPolicy? Policy { get; }
    public bool? Enabled { get; }
    public double? AnnualProbability { get; }

    public NaturalMortalityConfigurationOverrides(
        bool? enabled = null,
        double? annualProbability = null,
        NaturalMortalityPolicy? policy = null)
    {
        Enabled = enabled;
        AnnualProbability = annualProbability;
        Policy = policy;
    }
}

public sealed class AggregateDemographyConfigurationOverrides
{
    public AggregateDemographyPolicy? Policy { get; }
    public bool? Enabled { get; }
    public double? AnnualBirthRate { get; }
    public double? AnnualDeathRate { get; }
    public double? AnnualBirthRatePerYear => AnnualBirthRate;
    public double? AnnualDeathRatePerYear => AnnualDeathRate;

    public AggregateDemographyConfigurationOverrides(
        bool? enabled = null,
        double? annualBirthRate = null,
        double? annualDeathRate = null,
        AggregateDemographyPolicy? policy = null)
    {
        Enabled = enabled;
        AnnualBirthRate = annualBirthRate;
        AnnualDeathRate = annualDeathRate;
        Policy = policy;
    }
}

public sealed class SimulationConfigurationOverrides
{
    public PopulationConfigurationOverrides Population { get; }
    public EconomyConfigurationOverrides Economy { get; }
    public TravelConfigurationOverrides Travel { get; }
    public CrimeConfigurationOverrides Crime { get; }
    public GuardCrimeConfigurationOverrides GuardCrime { get; }
    public NaturalMortalityConfigurationOverrides NaturalMortality { get; }
    public AggregateDemographyConfigurationOverrides AggregateDemography { get; }

    public SimulationConfigurationOverrides(
        PopulationConfigurationOverrides population = null,
        EconomyConfigurationOverrides economy = null,
        TravelConfigurationOverrides travel = null,
        CrimeConfigurationOverrides crime = null,
        GuardCrimeConfigurationOverrides guardCrime = null,
        NaturalMortalityConfigurationOverrides naturalMortality = null,
        AggregateDemographyConfigurationOverrides aggregateDemography = null)
    {
        Population = population ?? new PopulationConfigurationOverrides();
        Economy = economy ?? new EconomyConfigurationOverrides();
        Travel = travel ?? new TravelConfigurationOverrides();
        Crime = crime ?? new CrimeConfigurationOverrides();
        GuardCrime = guardCrime ?? new GuardCrimeConfigurationOverrides();
        NaturalMortality = naturalMortality ?? new NaturalMortalityConfigurationOverrides();
        AggregateDemography = aggregateDemography ?? new AggregateDemographyConfigurationOverrides();
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
