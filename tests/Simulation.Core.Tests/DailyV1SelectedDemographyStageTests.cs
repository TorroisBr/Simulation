using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedDemographyStageTests
{
    [Test]
    public void AggregatePopulationWritesUseExactSelectedCityOwnersInStableIdentityOrder()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph();
        PersonStore people = new PersonStore();
        RecordingBoundary boundary = new RecordingBoundary(graph.Cities);
        RecordingAggregateProvider provider = new RecordingAggregateProvider(boundary.Events);
        int[] initialPopulations = graph.Cities.Select(city => city.Population.CurrentPopulation).ToArray();

        DailyV1SelectedDemographyResult result = DailyV1SelectedDemographyStage.Advance(
            graph,
            people,
            1L,
            new SimulationCalendar(new CalendarDefinition(2, 1, 2)),
            new EffectiveNaturalMortalityConfiguration(),
            new EffectiveAggregateDemographyConfiguration(true, 1d, 0d),
            null,
            provider,
            boundary);

        Assert.That(graph.Cities.Count, Is.GreaterThan(0));
        Assert.That(result.AggregateSettlementsProcessed, Is.EqualTo(graph.Cities.Count));
        Assert.That(result.AggregateBirthsApplied, Is.EqualTo(graph.Cities.Count));
        Assert.That(result.AggregateDeathsApplied, Is.Zero);
        string[] expectedCityOrder = graph.Cities.Select(city => city.RuntimeId)
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();
        Assert.That(provider.Contexts.Select(context => context.SettlementRuntimeId),
            Is.EqualTo(expectedCityOrder));
        for (int index = 0; index < graph.Cities.Count; index++)
        {
            Assert.That(graph.Cities[index].Population.CurrentPopulation,
                Is.EqualTo(initialPopulations[index] + 1));
            Assert.That(graph.Cities[index].Population.Revision, Is.EqualTo(1L));
        }
        Assert.That(boundary.Events.First(), Is.EqualTo("floors"));
    }

    [Test]
    public void NamedMortalityUsesPersonStoreBeforeFloorSnapshotAndAggregatePolicy()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph();
        PersonStore people = new PersonStore();
        PersonId personId = new PersonId("daily-demography-person");
        Assert.That(people.TryRegister(new PersonRuntime(personId, 0L), out PersonStoreFailure failure),
            Is.True, failure.ToString());

        RecordingBoundary boundary = new RecordingBoundary(graph.Cities);
        boundary.DeathToApply = personId;
        RecordingAggregateProvider provider = new RecordingAggregateProvider(boundary.Events);

        DailyV1SelectedDemographyResult result = DailyV1SelectedDemographyStage.Advance(
            graph,
            people,
            1L,
            new SimulationCalendar(new CalendarDefinition(2, 1, 2)),
            new EffectiveNaturalMortalityConfiguration(true, 1d),
            new EffectiveAggregateDemographyConfiguration(true, 1d, 0d),
            new CertainMortalitySamples(),
            provider,
            boundary);

        Assert.That(result.NamedPersonsEvaluated, Is.EqualTo(1));
        Assert.That(result.NamedDeathsApplied, Is.EqualTo(1));
        Assert.That(result.AggregateBirthsApplied, Is.EqualTo(graph.Cities.Count));
        Assert.That(boundary.Events[0], Is.EqualTo("death:" + personId.Value));
        Assert.That(boundary.Events[1], Is.EqualTo("floors"));
        Assert.That(boundary.Events[2], Does.StartWith("policy:"));
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder = new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        return graph;
    }

    private sealed class RecordingBoundary : IDailyV1SelectedDemographyBoundary
    {
        private readonly IReadOnlyDictionary<string, int> floors;
        internal readonly List<string> Events = new List<string>();
        internal PersonId DeathToApply;

        internal RecordingBoundary(IReadOnlyList<DailyV1CityRuntimeOwners> cities)
        {
            Dictionary<string, int> values = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (DailyV1CityRuntimeOwners city in cities)
                values.Add(city.RuntimeId, 0);
            floors = values;
        }

        public bool TryApplyPersonDeath(PersonId personId, out string failure)
        {
            Events.Add("death:" + personId.Value);
            failure = null;
            return DeathToApply != null && DeathToApply.Equals(personId);
        }

        public IReadOnlyDictionary<string, int> BuildRepresentedResidentFloors()
        {
            Events.Add("floors");
            return floors;
        }
    }

    private sealed class RecordingAggregateProvider : IAggregateDemographyProvider
    {
        private readonly List<string> events;
        internal readonly List<AggregateDemographyContext> Contexts =
            new List<AggregateDemographyContext>();

        internal RecordingAggregateProvider(List<string> events)
        {
            this.events = events;
        }

        public AggregateDemographyChange GetChange(AggregateDemographyContext context)
        {
            Contexts.Add(context);
            events.Add("policy:" + context.SettlementRuntimeId);
            return new AggregateDemographyChange(1, 0);
        }
    }

    private sealed class CertainMortalitySamples : IPersonNaturalMortalitySampleProvider
    {
        public double GetSample(PersonId personId, long currentAbsoluteDay) => 0d;
    }
}
