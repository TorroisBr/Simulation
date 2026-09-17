using System;

/// <summary>
/// Immutable, Unity-independent configuration consumed by future simulation runtime integrations.
/// </summary>
public sealed class EffectiveSimulationConfiguration : IEquatable<EffectiveSimulationConfiguration>
{
    public EffectivePopulationConfiguration Population { get; }
    public SimulationDomainPolicy MobilityPolicy { get; }
    public SimulationDomainPolicy CrimePolicy { get; }
    public SimulationDomainPolicy EconomyPolicy { get; }
    public SimulationDomainPolicy AdventurePolicy { get; }

    public EffectiveSimulationConfiguration(
        EffectivePopulationConfiguration population,
        SimulationDomainPolicy mobilityPolicy,
        SimulationDomainPolicy crimePolicy,
        SimulationDomainPolicy economyPolicy,
        SimulationDomainPolicy adventurePolicy)
    {
        Population = population;
        MobilityPolicy = mobilityPolicy;
        CrimePolicy = crimePolicy;
        EconomyPolicy = economyPolicy;
        AdventurePolicy = adventurePolicy;
    }

    public bool Equals(EffectiveSimulationConfiguration other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        return Equals(Population, other.Population)
            && Equals(MobilityPolicy, other.MobilityPolicy)
            && Equals(CrimePolicy, other.CrimePolicy)
            && Equals(EconomyPolicy, other.EconomyPolicy)
            && Equals(AdventurePolicy, other.AdventurePolicy);
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
            hash = (hash * 397) ^ (MobilityPolicy == null ? 0 : MobilityPolicy.GetHashCode());
            hash = (hash * 397) ^ (CrimePolicy == null ? 0 : CrimePolicy.GetHashCode());
            hash = (hash * 397) ^ (EconomyPolicy == null ? 0 : EconomyPolicy.GetHashCode());
            return (hash * 397) ^ (AdventurePolicy == null ? 0 : AdventurePolicy.GetHashCode());
        }
    }

    /// <summary>
    /// Creates the explicit baseline used by future configuration resolution.
    /// </summary>
    public static EffectiveSimulationConfiguration CreateDefault()
    {
        EffectivePopulationConfiguration population = new EffectivePopulationConfiguration(
            PopulationRepresentationMode.Aggregate,
            NpcDecisionSimulationScope.RelevantOnly);

        SimulationDomainPolicy availableAutonomous = new SimulationDomainPolicy(
            SimulationFeatureAvailability.Available,
            SimulationAutonomyMode.Autonomous);

        return new EffectiveSimulationConfiguration(
            population,
            availableAutonomous,
            availableAutonomous,
            availableAutonomous,
            availableAutonomous);
    }
}
