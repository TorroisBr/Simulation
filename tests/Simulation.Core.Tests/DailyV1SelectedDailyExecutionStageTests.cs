using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1SelectedDailyExecutionStageTests
{
    [Test]
    public void AdvanceRunsSelectedDailyPhasesInTheEstablishedOrder()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        FakeBoundary boundary = new FakeBoundary();
        ScheduledDirectiveStore directives = CreateDirectiveForFirstNpc(graph, 23L);
        long[] marketRevisionsBefore = graph.Cities.Select(city => city.Market.Revision).ToArray();
        long[] spatialKnowledgeRevisionsBefore = graph.Npcs
            .Select(npc => npc.SpatialKnowledge.Revision).ToArray();

        DailyV1SelectedDailyExecutionStage.Advance(23L, graph, directives, boundary);

        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "place-content", "logger:23", "demography", "begin-day",
            "economy-preflight", "knowledge-preflight", "expeditions",
            "autonomous-expeditions", "actor-turns", "travel-and-observe"
        }));
        Assert.That(directives.TryTakeDirective(graph.Npcs[0].RuntimeId,
            out ScheduledDirective preparedDirective), Is.True);
        Assert.That(preparedDirective.ActorRuntimeId, Is.EqualTo(graph.Npcs[0].RuntimeId));
        Assert.That(graph.Cities.Select(city => city.Market.Revision).ToArray(),
            Is.Not.EqualTo(marketRevisionsBefore),
            "The Core phase owns and commits the selected daily Economy writes.");
        Assert.That(graph.Npcs.Select(npc => npc.SpatialKnowledge.Revision).ToArray(),
            Is.Not.EqualTo(spatialKnowledgeRevisionsBefore),
            "The Core phase owns and commits the selected local Knowledge writes.");
    }

    [Test]
    public void AdvanceDerivesUniformAndCustomCalendarIdentityFromAdmittedDefinitions()
    {
        DailyV1CalendarDefinition[] calendars =
        {
            new DailyV1CalendarDefinition(12, 4, 7, null),
            new DailyV1CalendarDefinition(3, 4, 5, new[] { 10, 11, 12 })
        };

        foreach (DailyV1CalendarDefinition calendar in calendars)
        {
            DailyV1EffectiveDefinitions definitions = CreateFiniteFlowDefinitions(calendar);
            DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);

            DailyV1SelectedDailyExecutionStage.Advance(23L, graph, null, new FakeBoundary());

            string expected = calendar.monthLengths != null && calendar.monthLengths.Count > 0
                ? new SimulationCalendar(new CalendarDefinition(
                    new List<int>(calendar.monthLengths), calendar.daysPerWeek)).SemanticVersion
                : new SimulationCalendar(new CalendarDefinition(
                    calendar.monthsPerYear, calendar.weeksPerMonth, calendar.daysPerWeek)).SemanticVersion;
            Assert.That(graph.Cities, Has.Count.EqualTo(1));
            Assert.That(graph.Cities[0].LastMaterialFlow.CalendarVersion, Is.EqualTo(expected));
        }
    }

    [Test]
    public void AdvanceOmitsEconomyWhenTheSelectedConfigurationDisablesIt()
    {
        DailyV1EffectiveDefinitions definitions = CopyWithEconomyEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), false);
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        FakeBoundary boundary = new FakeBoundary();

        DailyV1SelectedDailyExecutionStage.Advance(0L, graph, null, boundary);

        Assert.That(boundary.Calls, Does.Not.Contain("economy-preflight"));
        Assert.That(boundary.Calls[4], Is.EqualTo("knowledge-preflight"));
        Assert.That(boundary.Calls[^1], Is.EqualTo("travel-and-observe"));
    }

    [Test]
    public void AdvanceStopsAtTheFirstFailingDomainPhase()
    {
        FakeBoundary boundary = new FakeBoundary
        {
            ThrowOnDemography = true
        };
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions());

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(
                9L, graph, null, boundary));

        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "place-content", "logger:9", "demography"
        }));
    }

    [Test]
    public void AdvanceRequiresTheAdmittedDefinitionGraph()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(0L, null, null, new FakeBoundary()));
    }

    private static ScheduledDirectiveStore CreateDirectiveForFirstNpc(
        DailyV1SelectedRuntimeOwnerGraph graph,
        long absoluteDay)
    {
        Assert.That(graph.Npcs, Is.Not.Empty);
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(new SimulationTime(absoluteDay));
        Assert.That(directives.Add(new ScheduledDirective(
            "directive/daily-stage", absoluteDay,
            ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison,
            graph.Npcs[0].RuntimeId,
            "action/escape",
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue)), Is.True);
        return directives;
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        DailyV1EffectiveDefinitions definitions)
    {
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        return graph;
    }

    private static DailyV1EffectiveDefinitions CopyWithEconomyEnabled(
        DailyV1EffectiveDefinitions definitions,
        bool enabled)
    {
        EffectiveSimulationConfiguration current = definitions.EffectiveConfiguration;
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            current.Population,
            new EffectiveEconomyConfiguration(enabled),
            current.Travel,
            current.Crime,
            current.GuardCrime,
            current.NaturalMortality,
            current.AggregateDemography,
            current.MerchantTrade,
            current.CommercialKnowledge);
        return new DailyV1EffectiveDefinitions(
            definitions.Configuration,
            configuration,
            definitions.Calendar,
            null,
            definitions.Cities.Values,
            definitions.Items.Values,
            definitions.Npcs.Values,
            definitions.Jobs.Values,
            definitions.Actions.Values,
            definitions.Statuses.Values,
            null,
            definitions.StageOrder,
            definitions.ProfileContractIdentity,
            definitions.GeographyProfileContractIdentity,
            definitions.GeographyStageIdentity,
            definitions.HexCoordinateConvention,
            definitions.HexCoordinateCanonicalOrder,
            null,
            definitions.Traits.Values,
            definitions.CapabilityAttributes.Values,
            definitions.ExplorableSites.Values);
    }

    private static DailyV1EffectiveDefinitions CreateFiniteFlowDefinitions(
        DailyV1CalendarDefinition calendar)
    {
        DailyV1CityData city = new DailyV1CityData(
            "city/finite", "Finite City", "settlement/finite", "location/finite", "market/finite",
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.FiniteReserveDaily, "FiniteReserveDaily"),
            1000, null,
            new DailyV1PopulationConsumptionConfig(
                new DailyV1EnumValue((int)ConsumptionPaymentMode.Free, "Free"), 0f),
            new[] { new DailyV1MarketItemConfig("item/finite", 5, 100, 0f) },
            new[] { new DailyV1CityProductionConfig("item/finite", 3, 7,
                "source/finite", "content-v1",
                new DailyV1EnumValue((int)CityProductionSourceKind.FiniteReserveDaily, "FiniteReserveDaily")) },
            Array.Empty<DailyV1CityConnection>());
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            DailyV1TypedDefinitionGraphTests.CreateConfiguration(1f, new[] { "city/finite" }),
            SimulationConfigurationDefaults.Create(),
            calendar,
            new[] { city },
            new[] { new DailyV1ItemData("item/finite", "Iron", 4f, null) },
            Array.Empty<DailyV1NpcData>(),
            Array.Empty<DailyV1NpcJobData>(),
            Array.Empty<DailyV1NpcActionData>(),
            Array.Empty<DailyV1NpcStatusData>(),
            Array.Empty<string>(),
            "test.daily-v1", "test.geography-v1", "test.geography-stage/v1", "hex-v1", "q-r-s");
        return CopyWithEconomyEnabled(definitions, true);
    }

    private sealed class FakeBoundary : IDailyV1SelectedDailyExecutionBoundary
    {
        internal List<string> Calls { get; } = new List<string>();
        internal bool ThrowOnDemography { get; set; }
        public void AdvancePlaceContent() => Calls.Add("place-content");
        public void BeginLoggerDay(long absoluteDay) => Calls.Add("logger:" + absoluteDay);
        public void AdvanceSelectedDemography()
        {
            Calls.Add("demography");
            if (ThrowOnDemography) throw new InvalidOperationException("demography failed");
        }
        public void BeginSimulationDay() => Calls.Add("begin-day");
        public void EnsureSelectedEconomyCanRun(DailyV1SelectedRuntimeOwnerGraph graph)
        {
            Assert.That(graph, Is.Not.Null);
            Calls.Add("economy-preflight");
        }
        public void LogSelectedDailyEconomy(
            DailyV1CityRuntimeOwners owner,
            SimulationLogCategory category,
            string message)
        {
            Assert.That(owner, Is.Not.Null);
        }
        public void EnsureSelectedLocalKnowledgeCanRun(DailyV1SelectedRuntimeOwnerGraph graph)
        {
            Assert.That(graph, Is.Not.Null);
            Calls.Add("knowledge-preflight");
        }
        public void AdvanceActiveExpeditions() => Calls.Add("expeditions");
        public void StartAutonomousExpeditions() => Calls.Add("autonomous-expeditions");
        public void AdvanceActorTurns() => Calls.Add("actor-turns");
        public void AdvanceTravelAndObserve() => Calls.Add("travel-and-observe");
    }
}
