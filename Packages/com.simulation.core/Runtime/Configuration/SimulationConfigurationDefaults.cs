public static class SimulationConfigurationDefaults
{
    public const long DefaultMaturityAgeYears = 18L;

    public static EffectiveSimulationConfiguration Create()
    {
        return new EffectiveSimulationConfiguration(
            new EffectivePopulationConfiguration(
                PopulationRepresentationMode.Aggregate,
                NpcDecisionSimulationScope.RelevantOnly,
                DefaultMaturityAgeYears),
            new EffectiveEconomyConfiguration(true),
            new EffectiveTravelConfiguration(0f),
            new EffectiveCrimeConfiguration(false, false),
            new EffectiveGuardCrimeConfiguration(false),
            new EffectiveNaturalMortalityConfiguration(),
            new EffectiveAggregateDemographyConfiguration(),
            new EffectiveMerchantTradeConfiguration(),
            new EffectiveCommercialKnowledgeConfiguration());
    }

    public static EffectiveSimulationConfiguration CreateForRuntime(
        bool economyEnabled,
        bool guardCrimeEnabled)
    {
        EffectiveSimulationConfiguration defaults = Create();
        return new EffectiveSimulationConfiguration(
            defaults.Population,
            new EffectiveEconomyConfiguration(economyEnabled),
            defaults.Travel,
            defaults.Crime,
            new EffectiveGuardCrimeConfiguration(guardCrimeEnabled),
            defaults.NaturalMortality,
            defaults.AggregateDemography,
            defaults.MerchantTrade,
            defaults.CommercialKnowledge);
    }
}
