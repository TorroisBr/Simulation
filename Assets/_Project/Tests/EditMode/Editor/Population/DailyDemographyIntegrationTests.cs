using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyDemographyIntegrationTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void DailyPhaseUsesInjectedCalendarAndRunsNamedDeathBeforeAggregateBirth()
    {
        CityRuntime city = CreateCity("daily-order-city", 1);
        PersonRuntime person = new PersonRuntime(new PersonId("daily-order-person"), 0L);
        AggregateContextProvider provider = new AggregateContextProvider(1, 0);
        SimulationRuntime world = CreateWorld(
            city,
            person,
            new CalendarDefinition(2, 1, 2),
            provider,
            naturalMortalityEnabled: true,
            naturalMortalityAnnualProbability: 1d,
            aggregateDemographyEnabled: true);

        world.AdvanceDay();

        Assert.That(world.Calendar.DaysPerYear, Is.EqualTo(4L));
        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(1L));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(1));
        Assert.That(world.LastDailyDemographyReport.NamedDeathsApplied, Is.EqualTo(1));
        Assert.That(world.LastDailyDemographyReport.AggregateBirthsApplied, Is.EqualTo(1));
        Assert.That(provider.Contexts, Has.Count.EqualTo(1));
        Assert.That(provider.Contexts[0].CurrentAbsoluteDay, Is.EqualTo(1L));
        Assert.That(provider.Contexts[0].RepresentedResidentFloor, Is.EqualTo(0));
        Assert.That(provider.Contexts[0].CurrentPopulation, Is.EqualTo(0));
    }

    [Test]
    public void AggregateDailyPhaseDoesNotCreatePersonsAndIsDeterministicAcrossRuns()
    {
        SimulationRuntime first = CreateAggregateOnlyWorld("replay-city");
        SimulationRuntime second = CreateAggregateOnlyWorld("replay-city");

        first.AdvanceDays(3);
        second.AdvanceDays(3);

        Assert.That(first.Cities[0].CurrentPopulation, Is.EqualTo(13));
        Assert.That(second.Cities[0].CurrentPopulation, Is.EqualTo(13));
        Assert.That(first.Cities[0].CurrentPopulation, Is.EqualTo(second.Cities[0].CurrentPopulation));
        Assert.That(first.PersonStore.Persons, Is.Empty);
        Assert.That(second.PersonStore.Persons, Is.Empty);
    }

    [Test]
    public void DemographyConfigurationUsesPrecedenceAndRejectsInvalidProbability()
    {
        EffectiveSimulationConfiguration resolved = SimulationConfigurationResolver.ResolveOrThrow(
            new SimulationConfigurationPreset(
                "preset",
                new SimulationConfigurationOverrides(
                    naturalMortality: new NaturalMortalityConfigurationOverrides(
                        enabled: true,
                        annualProbability: 0.1d))),
            new SimulationConfigurationOverrides(
                naturalMortality: new NaturalMortalityConfigurationOverrides(
                    enabled: false,
                    annualProbability: 0.2d)),
            new SimulationConfigurationOverrides(
                naturalMortality: new NaturalMortalityConfigurationOverrides(
                    enabled: true,
                    annualProbability: 0.3d)));

        Assert.That(resolved.NaturalMortality.Enabled, Is.True);
        Assert.That(resolved.NaturalMortality.AnnualProbability, Is.EqualTo(0.3d));

        SimulationConfigurationResolutionResult invalid = SimulationConfigurationResolver.Resolve(
            contentOverrides: new SimulationConfigurationOverrides(
                naturalMortality: new NaturalMortalityConfigurationOverrides(
                    enabled: true,
                    annualProbability: 1.1d)));
        Assert.That(invalid.IsValid, Is.False);
    }

    [Test]
    public void ProviderMutationIsRestoredAndRuntimeCalendarIsDetachedFromDefinition()
    {
        CityRuntime city = CreateCity("provider-rollback-city", 10);
        CalendarDefinition definition = new CalendarDefinition(2, 1, 2);
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                aggregateDemography: new AggregateDemographyConfigurationOverrides(
                    enabled: true,
                    annualBirthRate: 1d,
                    annualDeathRate: 0d)));
        MutatingProvider provider = new MutatingProvider(city.Population);
        SimulationRuntime world = new SimulationRuntime(
            simulationTime: new SimulationTime(),
            cities: new[] { city },
            npcRuntimes: null,
            configuration: configuration,
            calendarDefinition: definition,
            aggregateDemographyProvider: provider);

        definition.daysPerWeek = 9;
        world.AdvanceDay();

        Assert.That(world.Calendar.DaysPerYear, Is.EqualTo(4L));
        Assert.That(city.CurrentPopulation, Is.EqualTo(10));
        Assert.That(city.Population.Revision, Is.EqualTo(0L));
        Assert.That(world.LastDailyDemographyReport.HasErrors, Is.True);
    }

    private static SimulationRuntime CreateWorld(
        CityRuntime city,
        PersonRuntime person,
        CalendarDefinition calendar,
        IAggregateDemographyProvider provider,
        bool naturalMortalityEnabled,
        double naturalMortalityAnnualProbability,
        bool aggregateDemographyEnabled)
    {
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                naturalMortality: new NaturalMortalityConfigurationOverrides(
                    enabled: naturalMortalityEnabled,
                    annualProbability: naturalMortalityAnnualProbability),
                aggregateDemography: new AggregateDemographyConfigurationOverrides(
                    enabled: aggregateDemographyEnabled)));
        SimulationRuntime world = new SimulationRuntime(
            simulationTime: new SimulationTime(),
            cities: new[] { city },
            npcRuntimes: null,
            configuration: configuration,
            calendarDefinition: calendar,
            aggregateDemographyProvider: provider);
        Assert.That(world.TryRegisterPerson(person, out _), Is.True);
        Assert.That(world.TryBindExistingPersonResident(
            person.PersonId,
            city,
            out PersonResidenceMembershipFailure failure), Is.True, failure.ToString());
        return world;
    }

    private static SimulationRuntime CreateAggregateOnlyWorld(string runtimeId)
    {
        CityRuntime city = CreateCity(runtimeId, 10);
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                aggregateDemography: new AggregateDemographyConfigurationOverrides(
                    enabled: true,
                    annualBirthRate: 4d,
                    annualDeathRate: 0d)));
        return new SimulationRuntime(
            simulationTime: new SimulationTime(),
            cities: new[] { city },
            npcRuntimes: null,
            configuration: configuration,
            calendarDefinition: new CalendarDefinition(2, 1, 2));
    }

    private static CityRuntime CreateCity(string runtimeId, int population)
    {
        CityData data = SimulationTestFactory.CreateCityData(runtimeId + "-definition");
        data.initialPopulation = population;
        return new CityRuntime(
            runtimeId,
            data,
            new SpatialLocationRuntime(runtimeId + "-location"));
    }

    private sealed class AggregateContextProvider : IAggregateDemographyProvider
    {
        private readonly int births;
        private readonly int deaths;

        public List<AggregateDemographyContext> Contexts { get; } =
            new List<AggregateDemographyContext>();

        public AggregateContextProvider(int births, int deaths)
        {
            this.births = births;
            this.deaths = deaths;
        }

        public AggregateDemographyChange GetChange(AggregateDemographyContext context)
        {
            Contexts.Add(context);
            return new AggregateDemographyChange(births, deaths);
        }
    }

    private sealed class MutatingProvider : IAggregateDemographyProvider
    {
        private readonly SettlementPopulationRuntime population;

        public MutatingProvider(SettlementPopulationRuntime population)
        {
            this.population = population;
        }

        public AggregateDemographyChange GetChange(AggregateDemographyContext context)
        {
            SettlementPopulationSystem.TryPropose(
                population,
                new PopulationChangeSet(1, 0, 0, 0),
                out SettlementPopulationTransition transition,
                out _);
            SettlementPopulationSystem.TryApply(population, transition, out _);
            return new AggregateDemographyChange(0, 0);
        }
    }
}
