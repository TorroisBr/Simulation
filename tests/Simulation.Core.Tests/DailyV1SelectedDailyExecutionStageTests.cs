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

        List<string> expectedCalls = new List<string>
        {
            "logger:23", "begin-day",
            "economy-preflight", "knowledge-preflight", "actor-context"
        };
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc.Lifecycle.LifeState == NpcLifeState.Alive)
            {
                expectedCalls.Add("actor-merchant:" + npc.RuntimeId);
                if (ReferenceEquals(npc, graph.Npcs[0])) expectedCalls.Add("directive");
            }
        }
        expectedCalls.AddRange(new[] { "travel-context", "travel-results", "travel-observation" });
        Assert.That(boundary.Calls, Is.EqualTo(expectedCalls));
        Assert.That(boundary.Calls, Does.Not.Contain("place-content"));
        Assert.That(boundary.Calls, Does.Not.Contain("expeditions"));
        Assert.That(boundary.Calls, Does.Not.Contain("autonomous-expeditions"),
            "The accepted profile has no place-content, explorable-site, or autonomy producer to invoke.");
        Assert.That(directives.TryTakeDirective(graph.Npcs[0].RuntimeId,
            out _), Is.False, "The Core actor-turn stage consumes its prepared selected directive.");
        Assert.That(graph.Cities.Select(city => city.Market.Revision).ToArray(),
            Is.Not.EqualTo(marketRevisionsBefore),
            "The Core phase owns and commits the selected daily Economy writes.");
        Assert.That(graph.Npcs.Select(npc => npc.SpatialKnowledge.Revision).ToArray(),
            Is.Not.EqualTo(spatialKnowledgeRevisionsBefore),
            "The Core phase owns and commits the selected local Knowledge writes.");
    }

    [Test]
    public void AdvanceRunsSelectedCrimeJusticeScopesAndMerchantUrgencyInCoreDayStartPosition()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(),
            new DailyV1JusticeRecordsOwner());
        Assert.That(graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled, Is.True,
            "The positive plan-urgency case is admitted by the immutable selected configuration.");
        DailyV1NpcRuntimeOwners localMerchant = graph.Npcs.First(owner =>
            DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            && DailyV1NpcDefinitionRules.HasMerchantBehavior(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue));
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, localMerchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        DailyV1CityRuntimeOwners target = graph.Cities.First(city => !ReferenceEquals(city, origin));
        DailyV1ItemDefinition item = graph.Definitions.Items.Values.First();
        Assert.That(graph.TryInstallMerchantTradePlan(localMerchant, item,
            origin.RuntimeId, target.RuntimeId, 2, 1f, "decision/day-start-plan"), Is.True);
        long planRevision = localMerchant.MerchantTradePlan.Revision;

        FakeBoundary boundary = new FakeBoundary
        {
            UseSelectedDayStartContext = true,
            CrimeSystemPresent = true,
            JusticeSystemPresent = true
        };

        DailyV1SelectedDailyExecutionStage.Advance(23L, graph, null, boundary);

        Assert.That(boundary.Calls, Does.Not.Contain("begin-day"),
            "The admitted selected graph runs the Core day-start stages directly.");
        Assert.That(boundary.DayStartPhases, Is.EqualTo(new[]
        {
            DailyV1SelectedCrimeJusticeDailyPhase.ClearArrestedToday,
            DailyV1SelectedCrimeJusticeDailyPhase.AdvanceHiddenStatuses,
            DailyV1SelectedCrimeJusticeDailyPhase.AdvanceSentences,
            DailyV1SelectedCrimeJusticeDailyPhase.SyncWantedStatuses
        }), "The four existing P12 daily scopes retain their order.");
        Assert.That(boundary.Calls.IndexOf("merchant-plan"), Is.GreaterThan(
            boundary.Calls.IndexOf("crime:" + DailyV1SelectedCrimeJusticeDailyPhase.SyncWantedStatuses)));
        Assert.That(boundary.Calls.IndexOf("merchant-plan"), Is.LessThan(
            boundary.Calls.IndexOf("economy-preflight")));
        Assert.That(boundary.Calls.IndexOf("merchant-plan"), Is.LessThan(
            boundary.Calls.IndexOf("actor-context")));
        Assert.That(localMerchant.MerchantTradePlan.HasData, Is.True,
            "The day-start urgency phase preserves an admitted plan; actor-turn Merchant eligibility handles local-plan cleanup later.");
        Assert.That(localMerchant.MerchantTradePlan.PendingTravelDays, Is.EqualTo(1),
            "The day-start urgency phase advances the exact active plan's pending-travel count.");
        Assert.That(localMerchant.MerchantTradePlan.Revision, Is.EqualTo(planRevision + 1L));
    }

    [Test]
    public void StateBackedDayStartUsesCoreBoundRosterInsteadOfHostProjection()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = true,
            HasUnselectedLiveRuntimeMember = true,
            CrimeSystemPresent = true,
            JusticeSystemPresent = true,
            DayStartRuntimeRosterOverride = Array.Empty<DailyV1ActorTurnRosterProjection>()
        };

        DailyV1SelectedDailyExecutionStage.Advance(
            composition.ExecutionBoundaryState, composition.OwnerGraph, boundary);

        Assert.That(boundary.DayStartPhases, Is.EqualTo(new[]
        {
            DailyV1SelectedCrimeJusticeDailyPhase.ClearArrestedToday,
            DailyV1SelectedCrimeJusticeDailyPhase.AdvanceHiddenStatuses,
            DailyV1SelectedCrimeJusticeDailyPhase.AdvanceSentences,
            DailyV1SelectedCrimeJusticeDailyPhase.SyncWantedStatuses
        }), "The admitted state-backed day-start stages use the roster already bound to Core state.");
        Assert.That(boundary.Calls, Does.Not.Contain("begin-day"));
    }

    [Test]
    public void StateBackedDayStartLetsCoreSelectCompatibilityForUnselectedLiveMember()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        IReadOnlyList<DailyV1ActorTurnRosterProjection> priorRoster =
            state.SelectedRuntimeActorRoster;
        Assert.That(state.TryCreateSelectedRuntimeActorRosterWithAppendedMember(
                new DailyV1ActorTurnRosterProjection("runtime/unselected-dynamic", null),
                composition.OwnerGraph,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> rosterWithUnselectedMember), Is.True,
            "The Core roster can retain an unselected runtime identity without assigning it an owner.");
        Assert.That(state.TryCommitSelectedRuntimeActorRoster(priorRoster, rosterWithUnselectedMember), Is.True);

        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            HasUnselectedLiveRuntimeMember = false,
            CrimeSystemPresent = true,
            JusticeSystemPresent = true,
            DayStartRuntimeRosterOverride = Array.Empty<DailyV1ActorTurnRosterProjection>()
        };

        DailyV1SelectedDailyExecutionStage.Advance(
            composition.ExecutionBoundaryState, composition.OwnerGraph, boundary);

        Assert.That(boundary.Calls.Count(call => call == "begin-day"), Is.EqualTo(1),
            "Core selects the compatibility callback from its exact roster even when the host cannot provide a selected day-start context.");
        Assert.That(boundary.DayStartPhases, Is.Empty,
            "The Core-selected day-start scopes do not run alongside the whole-stage fallback.");
        Assert.That(boundary.Calls, Does.Not.Contain("merchant-plan"));
    }

    [Test]
    public void AdmittedStageRejectsMissingSelectedDayStartContextInsteadOfDelegatingToUnityWholeDay()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = false
        };

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(
                composition.ExecutionBoundaryState, composition.OwnerGraph, boundary));

        Assert.That(failure.Message, Does.Contain(
            "The admitted Daily-v1 host could not provide its selected day-start boundary."));
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "actor-context", "logger:0" }),
            "A selected execution cannot fall back to Unity's whole-day authority when its exact owner boundary is missing.");
        Assert.That(boundary.Calls, Does.Not.Contain("begin-day"));
        Assert.That(boundary.Calls, Does.Not.Contain("economy-preflight"));
        Assert.That(boundary.Calls, Does.Not.Contain("knowledge-preflight"));
        Assert.That(boundary.Calls, Does.Not.Contain("travel-context"));
    }

    [Test]
    public void AdmittedDailyStageRejectsHostOwnedActionFallbackBeforeAnyDailyCallback()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = false
        };
        long marketRevision = composition.OwnerGraph.Cities[0].Market.Revision;
        long spatialKnowledgeRevision = composition.OwnerGraph.Npcs[0].SpatialKnowledge.Revision;
        DailyCaptureEligibilityToken captureToken =
            composition.ExecutionBoundaryState.CurrentCaptureToken;

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(
                composition.ExecutionBoundaryState, composition.OwnerGraph, boundary));

        Assert.That(failure.Message, Does.Contain("host-owned action execution"));
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "actor-context" }),
            "The admitted Core-owned action contract is proven before logger, daily, owner, or capture effects.");
        Assert.That(composition.OwnerGraph.Cities[0].Market.Revision, Is.EqualTo(marketRevision));
        Assert.That(composition.OwnerGraph.Npcs[0].SpatialKnowledge.Revision,
            Is.EqualTo(spatialKnowledgeRevision));
        Assert.That(composition.ExecutionBoundaryState.CurrentCaptureToken, Is.SameAs(captureToken),
            "A preflight rejection does not invalidate or replace the prior completed-boundary token.");
    }

    [Test]
    public void AdvanceRejectsDayStartBoundaryBoundToAnotherJusticeOwner()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(),
            new DailyV1JusticeRecordsOwner());
        FakeBoundary boundary = new FakeBoundary
        {
            UseSelectedDayStartContext = true,
            JusticeSystemPresent = true,
            SubstituteJusticeRecordsOwner = new DailyV1JusticeRecordsOwner()
        };

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(23L, graph, null, boundary));
        Assert.That(boundary.DayStartPhases, Is.Empty,
            "Core must reject a substituted Justice owner before admitting any Crime/Justice scope.");
        Assert.That(boundary.Calls, Does.Not.Contain("begin-day"));
    }

    [Test]
    public void AdvanceUsesAdmittedMerchantConfigurationForPlanUrgency()
    {
        DailyV1EffectiveDefinitions definitions = CopyWithMerchantTradeEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), false);
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        Assert.That(graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled, Is.False,
            "The graph carries the selected immutable configuration used by the Core day stage.");
        DailyV1NpcRuntimeOwners localMerchant = graph.Npcs.First(owner =>
            DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            && DailyV1NpcDefinitionRules.HasMerchantBehavior(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue));
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, localMerchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        DailyV1CityRuntimeOwners target = graph.Cities.First(city => !ReferenceEquals(city, origin));
        DailyV1ItemDefinition item = graph.Definitions.Items.Values.First();
        Assert.That(graph.TryInstallMerchantTradePlan(localMerchant, item,
            origin.RuntimeId, target.RuntimeId, 2, 1f, "decision/disabled-plan"), Is.True);
        long planRevision = localMerchant.MerchantTradePlan.Revision;
        FakeBoundary boundary = new FakeBoundary { UseSelectedDayStartContext = true };

        DailyV1SelectedDailyExecutionStage.Advance(23L, graph, null, boundary);

        Assert.That(boundary.Calls, Does.Not.Contain("merchant-plan"));
        Assert.That(boundary.Calls, Does.Not.Contain("actor-merchant:" + localMerchant.RuntimeId),
            "Core actor-turn eligibility follows the same admitted Merchant setting.");
        Assert.That(localMerchant.MerchantTradePlan.HasData, Is.True);
        Assert.That(localMerchant.MerchantTradePlan.Revision, Is.EqualTo(planRevision));
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
        Assert.That(boundary.Calls.IndexOf("knowledge-preflight"), Is.EqualTo(
            boundary.Calls.IndexOf("begin-day") + 1),
            "The selected local-knowledge stage remains immediately after day start when Economy is disabled.");
        int actorContext = boundary.Calls.IndexOf("actor-context");
        int merchantPass = boundary.Calls.FindIndex(call =>
            call.StartsWith("actor-merchant:", StringComparison.Ordinal));
        int travelContext = boundary.Calls.IndexOf("travel-context");
        int travelResults = boundary.Calls.IndexOf("travel-results");
        int travelObservation = boundary.Calls.IndexOf("travel-observation");
        Assert.That(actorContext, Is.GreaterThanOrEqualTo(0));
        Assert.That(merchantPass, Is.GreaterThan(actorContext));
        Assert.That(travelContext, Is.GreaterThan(merchantPass));
        Assert.That(travelResults, Is.GreaterThan(travelContext));
        Assert.That(travelObservation, Is.GreaterThan(travelResults));
    }

    [Test]
    public void AdvanceStopsAtTheFirstFailingDomainPhase()
    {
        FakeBoundary boundary = new FakeBoundary
        {
            ThrowOnDemography = true
        };
        DailyV1EffectiveDefinitions definitions = CopyWithNaturalMortalityEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), true);
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(
                9L, graph, null, boundary));

        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "logger:9", "demography"
        }));
    }

    [Test]
    public void AdvanceStopsAtDayStartFailureAfterDisabledDemography()
    {
        FakeBoundary boundary = new FakeBoundary { ThrowOnBeginDay = true };
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions());

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(
                9L, graph, null, boundary));

        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "logger:9", "begin-day"
        }));
        Assert.That(boundary.PublishedDemographyResult.AbsoluteDay, Is.EqualTo(9L));
    }

    [Test]
    public void AdvanceDoesNotRequestDisabledDemographyWriterFromTheHost()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(definitions.EffectiveConfiguration.NaturalMortality.Enabled, Is.False);
        Assert.That(definitions.EffectiveConfiguration.AggregateDemography.Enabled, Is.False);
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        FakeBoundary boundary = new FakeBoundary { ThrowOnDemography = true };

        Assert.DoesNotThrow(() => DailyV1SelectedDailyExecutionStage.Advance(
            23L, graph, null, boundary));

        Assert.That(boundary.Calls, Does.Not.Contain("demography"),
            "The exact admitted profile has no demographic writer or resident-floor operation to perform.");
        Assert.That(boundary.Calls[0], Is.EqualTo("logger:23"));
        Assert.That(boundary.Calls, Does.Contain("actor-context"));
        Assert.That(boundary.Calls, Does.Contain("travel-context"));
        Assert.That(boundary.PublishedDemographyResult.AbsoluteDay, Is.EqualTo(23L));
        Assert.That(boundary.PublishedDemographyResult.NamedDeathsApplied, Is.Zero);
        Assert.That(boundary.PublishedDemographyResult.AggregateBirthsApplied, Is.Zero);
    }

    [Test]
    public void AdvanceRequestsDemographyContextWhenNaturalMortalityIsEnabled()
    {
        DailyV1EffectiveDefinitions definitions = CopyWithNaturalMortalityEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), true);
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        FakeBoundary boundary = new FakeBoundary();

        DailyV1SelectedDailyExecutionStage.Advance(23L, graph, null, boundary);

        Assert.That(boundary.Calls, Does.Contain("demography"),
            "An enabled policy still uses the established host writer context.");
        Assert.That(boundary.PublishedDemographyResult.AbsoluteDay, Is.EqualTo(23L));
        Assert.That(boundary.PublishedDemographyResult.NamedPersonsEvaluated, Is.Zero,
            "The empty fixture requests the enabled-policy boundary without inventing Persons.");
    }

    [Test]
    public void RuntimeAdvanceStageInvokesSelectedDailyCoreBeforeCompletionAndTokenPublication()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        long absoluteDay = composition.SimulationTime.AbsoluteDay + 1L;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        Assert.That(state.SimulationTime.AbsoluteDay, Is.EqualTo(absoluteDay - 1L));
        Assert.That(state.HasAdmittedSelectedDailyExecution, Is.True);
        ScheduledDirectiveStore directives = CreateDirectiveForFirstNpc(graph, absoluteDay);
        FakeBoundary dailyBoundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = true
        };
        FakeRuntimeAdvanceBoundary advanceBoundary = new FakeRuntimeAdvanceBoundary(
            state, dailyBoundary);

        Assert.That(state.TryAdvanceDay(
            advanceBoundary, out SimulationRuntimeAdvanceFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.None));
        Assert.That(advanceBoundary.Calls, Is.EqualTo(new[]
        {
            "operation", "execution-boundary", "capture-evidence"
        }));
        Assert.That(directives.TryTakeDirective(graph.Npcs[0].RuntimeId, out _), Is.False,
            "The state-backed Core actor-turn stage consumes its exact graph-bound directive.");
        Assert.That(dailyBoundary.Calls, Does.Not.Contain("directive"),
            "Selected directives execute through Core rather than the legacy host callback.");
        Assert.That(dailyBoundary.Calls, Does.Not.Contain("begin-day"),
            "The admitted selected path runs the Core day-start stages instead of delegating to Unity's whole-day callback.");
        Assert.That(dailyBoundary.Calls, Does.Contain("actor-context"));
        Assert.That(dailyBoundary.Calls, Does.Contain("travel-context"));
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(1L));
        Assert.That(state.SimulationTime.AbsoluteDay, Is.EqualTo(absoluteDay));
        Assert.That(state.CurrentCaptureToken, Is.Not.Null);
        Assert.That(state.CurrentCaptureToken.AbsoluteDay, Is.EqualTo(absoluteDay));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void AdvanceUsesStateBoundDecisionRootsWhenHostOwnerLookupFails()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = true,
            FailDecisionRecordOwnerResolution = true
        };

        FakeRuntimeAdvanceBoundary advanceBoundary = new FakeRuntimeAdvanceBoundary(state, boundary);
        Assert.That(state.TryAdvanceDay(
            advanceBoundary, out SimulationRuntimeAdvanceFailure failure), Is.True, failure.ToString());

        Assert.That(boundary.Calls, Does.Contain("actor-context"));
        Assert.That(boundary.DecisionOwnerLookupCount, Is.Zero,
            "The state-bound Core roots are authoritative; the host's failing owner lookup is not consulted.");
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(1L));
        Assert.That(state.CurrentCaptureToken, Is.Not.Null);
    }

    [Test]
    public void RuntimeAdvanceRejectsPerActorChoiceBoundaryThatFallsBackBeforeDailyWritesAndCapturePublication()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        long initialDay = state.SimulationTime.AbsoluteDay;
        FakeBoundary dailyBoundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            ActorChoiceSelectedActionExecutionOverride = false
        };
        // The returned per-actor boundary has the same exact roots and Core
        // contract type, but it would use the legacy host action callback.
        FakeRuntimeAdvanceBoundary advanceBoundary = new FakeRuntimeAdvanceBoundary(state, dailyBoundary);

        Assert.Throws<InvalidOperationException>(() =>
            state.TryAdvanceDay(advanceBoundary, out _));

        Assert.That(dailyBoundary.Calls, Is.EqualTo(new[] { "actor-context" }),
            "Per-actor action-mode proof completes before logger, daily-stage, owner, or capture callbacks.");
        Assert.That(dailyBoundary.SelectedActionDispatchLookupCount, Is.Zero);
        Assert.That(advanceBoundary.Calls, Does.Contain("fault"));
        Assert.That(advanceBoundary.Calls, Does.Not.Contain("capture-evidence"));
        Assert.That(state.SimulationTime.AbsoluteDay, Is.EqualTo(initialDay + 1L),
            "The failed admission is detected after the existing temporal advance, then faults before any daily owner write.");
        Assert.That(state.CompletedCoreSequence, Is.Zero);
        Assert.That(state.CurrentCaptureToken, Is.Null);
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void RuntimeAdvanceUsesStateBoundRootsWhenHostOwnerLookupFails()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        FakeBoundary dailyBoundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = true,
            FailDecisionRecordOwnerResolution = true
        };
        FakeRuntimeAdvanceBoundary advanceBoundary = new FakeRuntimeAdvanceBoundary(state, dailyBoundary);

        Assert.That(state.TryAdvanceDay(
            advanceBoundary, out SimulationRuntimeAdvanceFailure failure), Is.True, failure.ToString());

        Assert.That(dailyBoundary.Calls, Does.Contain("actor-context"));
        Assert.That(dailyBoundary.DecisionOwnerLookupCount, Is.Zero,
            "A runtime advance consumes only the roots bound to its execution state.");
        Assert.That(advanceBoundary.Calls, Does.Not.Contain("fault"));
        Assert.That(advanceBoundary.Calls, Does.Contain("capture-evidence"));
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(1L));
        Assert.That(state.CurrentCaptureToken, Is.Not.Null);
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void SelectedDailyContextFailureAfterClockAdvanceFaultsAndCannotPublishBoundary()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        Assert.That(state.HasAdmittedSelectedDailyExecution, Is.True);
        FakeRuntimeAdvanceBoundary advanceBoundary = new FakeRuntimeAdvanceBoundary(
            state, new FakeBoundary())
        {
            FailSelectedContext = true
        };

        Assert.That(state.TryAdvanceDay(
            advanceBoundary, out SimulationRuntimeAdvanceFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(advanceBoundary.Calls, Does.Contain("fault"));
        Assert.That(advanceBoundary.Calls, Does.Not.Contain("completed"));
        Assert.That(advanceBoundary.Calls, Does.Not.Contain("capture-evidence"));
        Assert.That(state.CompletedCoreSequence, Is.Zero);
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void AdvanceRequiresTheAdmittedDefinitionGraph()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(0L, null, null, new FakeBoundary()));
    }

    [Test]
    public void AdvanceRejectsASubstitutedScheduledDirectiveStore()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions());
        ScheduledDirectiveStore substituted = new ScheduledDirectiveStore(new SimulationTime(4L));
        FakeBoundary boundary = new FakeBoundary();

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(0L, graph, substituted, boundary));
        Assert.That(boundary.Calls, Is.Empty,
            "Owner identity preflight must reject a substitute before any daily callback or write.");
    }

    [Test]
    public void AdvanceRejectsActorContextWithDifferentPersonStoreInstance()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions());
        FakeBoundary boundary = new FakeBoundary { SubstituteActorPersonStore = true };

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(0L, graph, null, boundary));
        Assert.That(boundary.Calls, Does.Contain("actor-context"));
        Assert.That(boundary.Calls, Does.Not.Contain("directive"),
            "The Core actor operation stage must not run with a substituted PersonStore.");
    }

    [Test]
    public void AdmittedActorTurnContextUsesOnlyCoreBoundRosterAndOwnerRoots()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore);

        Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
            state, graph, boundary,
            out IDailyV1SelectedActorTurnExecutionContext context), Is.True);

        Assert.That(context.RuntimeRoster, Is.SameAs(state.SelectedRuntimeActorRoster));
        Assert.That(context.PersonStore, Is.SameAs(graph.PersonStore));
        Assert.That(context.ActorChoiceStore, Is.SameAs(state.SelectedActorChoiceStore));
        Assert.That(context.ActorChoiceStore.PersonStore, Is.SameAs(context.PersonStore));
        Assert.That(context.CoreRoots.RandomSource, Is.SameAs(state.SelectedRandomSource));
        Assert.That(context.CoreRoots.IdAllocator, Is.SameAs(state.SelectedRuntimeIdAllocator));
        Assert.That(context.CoreRoots.RecordSequence, Is.SameAs(state.SelectedRecordSequence));
        Assert.That(context.CoreRoots.DecisionStore, Is.SameAs(state.SelectedDecisionStore));
        Assert.That(state.SelectedRuntimeActorTurnCoreRoots, Is.SameAs(context.CoreRoots));
        Assert.That(context.CoreRoots.IsBoundTo(
            state, graph, context.PersonStore, context.ActorChoiceStore), Is.True);

        FakeBoundary nextBoundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore);
        Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
            state, graph, nextBoundary,
            out IDailyV1SelectedActorTurnExecutionContext nextContext), Is.True);
        Assert.That(nextContext.CoreRoots, Is.SameAs(context.CoreRoots),
            "Subsequent daily contexts must reuse the exact Core session-owned roots.");
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "actor-context" }));
    }

    [Test]
    public void ActorTurnContextFactoryRejectsUnboundStateBeforeHostBoundary()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        DailyV1ExecutionBoundaryState unboundState = new DailyV1ExecutionBoundaryState(
            new SimulationTime(),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()));
        FakeBoundary boundary = new FakeBoundary();

        Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
            unboundState, graph, boundary,
            out IDailyV1SelectedActorTurnExecutionContext context), Is.False);

        Assert.That(context, Is.Null);
        Assert.That(boundary.Calls, Is.Empty,
            "Core must reject incomplete bound roots without asking the host to create a projection boundary.");
    }

    private static ScheduledDirectiveStore CreateDirectiveForFirstNpc(
        DailyV1SelectedRuntimeOwnerGraph graph,
        long absoluteDay)
    {
        Assert.That(graph.Npcs, Is.Not.Empty);
        ScheduledDirectiveStore directives = graph.ScheduledDirectives
            ?? new ScheduledDirectiveStore(new SimulationTime(absoluteDay));
        Assert.That(directives.Add(new ScheduledDirective(
            "directive/daily-stage", absoluteDay,
            ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison,
            graph.Npcs[0].RuntimeId,
            "action/escape",
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue)), Is.True);
        Assert.That(graph.TryBindScheduledDirectives(directives), Is.True);
        return directives;
    }

    private static DailyV1GenesisComposition CreateSelectedGenesis()
    {
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition, out string diagnostic), Is.True, diagnostic);
        Assert.That(composition.ExecutionBoundaryState.TryBindOrValidateSelectedRuntimeActorRoster(
            composition.OwnerGraph.Npcs.Select(owner =>
                new DailyV1ActorTurnRosterProjection(owner.RuntimeId, null))), Is.True);
        return composition;
    }

    private static DailyV1ExecutionBoundaryState CreateCaptureState(
        long absoluteDay,
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        Assert.That(graph?.Definitions?.EffectiveConfiguration, Is.Not.Null);
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        DailyV1CalendarDefinition calendarDefinition = graph.Definitions.Calendar;
        CalendarDefinition calendar = calendarDefinition.monthLengths != null
            && calendarDefinition.monthLengths.Count > 0
            ? new CalendarDefinition(
                new List<int>(calendarDefinition.monthLengths), calendarDefinition.daysPerWeek)
            : new CalendarDefinition(
                calendarDefinition.monthsPerYear,
                calendarDefinition.weeksPerMonth,
                calendarDefinition.daysPerWeek);
        SimulationCalendar runtimeCalendar = new SimulationCalendar(calendar);
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState(
            new SimulationTime(absoluteDay), admission, SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()));
        Assert.That(state.TryBindRuntimeConfiguration(
            graph.Definitions.EffectiveConfiguration, runtimeCalendar), Is.True);
        Assert.That(state.TryBindSelectedRuntimeOwnerGraph(graph), Is.True);
        Assert.That(state.TryBindSelectedRuntimeActorRoster(graph.Npcs.Select(owner =>
            new DailyV1ActorTurnRosterProjection(owner.RuntimeId, null))), Is.True);
        return state;
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        DailyV1EffectiveDefinitions definitions)
    {
        return CreateGraph(definitions, null);
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        DailyV1EffectiveDefinitions definitions,
        DailyV1JusticeRecordsOwner justiceRecordsOwner)
    {
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(justiceRecordsOwner, out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.TryBindPersonStore(new PersonStore()), Is.True);
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

    private static DailyV1EffectiveDefinitions CopyWithNaturalMortalityEnabled(
        DailyV1EffectiveDefinitions definitions,
        bool enabled)
    {
        EffectiveSimulationConfiguration current = definitions.EffectiveConfiguration;
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            current.Population,
            current.Economy,
            current.Travel,
            current.Crime,
            current.GuardCrime,
            new EffectiveNaturalMortalityConfiguration(enabled, 0d),
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

    private static DailyV1EffectiveDefinitions CopyWithMerchantTradeEnabled(
        DailyV1EffectiveDefinitions definitions,
        bool enabled)
    {
        EffectiveSimulationConfiguration current = definitions.EffectiveConfiguration;
        EffectiveMerchantTradeConfiguration currentMerchant = current.MerchantTrade;
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            current.Population,
            current.Economy,
            current.Travel,
            current.Crime,
            current.GuardCrime,
            current.NaturalMortality,
            current.AggregateDemography,
            new EffectiveMerchantTradeConfiguration(
                enabled,
                currentMerchant.AllowAutonomousTradeRepositioning,
                currentMerchant.MaxTradeAmount,
                currentMerchant.LocalWholesalePriceMultiplier,
                currentMerchant.LocalReserveRatio,
                currentMerchant.MaxUnprofitablePlanWaitDays),
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
        private readonly RuntimeIdAllocator runtimeIdAllocator;
        private readonly IAuthoritativeRandomSource randomSource;
        private readonly SimulationRecordSequence recordSequence;
        private readonly NpcDecisionStore decisionStore;
        private readonly ActorChoiceStore actorChoiceStore;

        internal FakeBoundary() { }

        internal FakeBoundary(
            RuntimeIdAllocator runtimeIdAllocator,
            IAuthoritativeRandomSource randomSource,
            SimulationRecordSequence recordSequence,
            NpcDecisionStore decisionStore,
            ActorChoiceStore actorChoiceStore)
        {
            this.runtimeIdAllocator = runtimeIdAllocator;
            this.randomSource = randomSource;
            this.recordSequence = recordSequence;
            this.decisionStore = decisionStore;
            this.actorChoiceStore = actorChoiceStore;
        }

        internal List<string> Calls { get; } = new List<string>();
        internal List<DailyV1SelectedCrimeJusticeDailyPhase> DayStartPhases { get; } =
            new List<DailyV1SelectedCrimeJusticeDailyPhase>();
        internal DailyV1SelectedDemographyResult PublishedDemographyResult { get; private set; }
        internal bool ThrowOnDemography { get; set; }
        internal bool ThrowOnBeginDay { get; set; }
        internal bool SubstituteActorPersonStore { get; set; }
        internal bool UseSelectedDayStartContext { get; set; }
        internal bool HasUnselectedLiveRuntimeMember { get; set; }
        internal bool CrimeSystemPresent { get; set; }
        internal bool JusticeSystemPresent { get; set; }
        internal IReadOnlyList<DailyV1ActorTurnRosterProjection> DayStartRuntimeRosterOverride { get; set; }
        internal bool UseSelectedActionExecution { get; set; }
        internal bool? ActorChoiceSelectedActionExecutionOverride { get; set; }
        internal bool FailDecisionRecordOwnerResolution { get; set; }
        internal int DecisionOwnerLookupCount { get; private set; }
        internal int SelectedActionDispatchLookupCount { get; private set; }
        internal DailyV1JusticeRecordsOwner SubstituteJusticeRecordsOwner { get; set; }
        public void BeginLoggerDay(long absoluteDay) => Calls.Add("logger:" + absoluteDay);
        public bool TryCreateSelectedDemographyContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDemographyExecutionContext context)
        {
            Calls.Add("demography");
            if (ThrowOnDemography) throw new InvalidOperationException("demography failed");
            context = new FakeDemographyContext(graph);
            return true;
        }
        public void PublishSelectedDemographyResult(DailyV1SelectedDemographyResult result)
        {
            PublishedDemographyResult = result ?? throw new ArgumentNullException(nameof(result));
        }
        public bool TryCreateSelectedDayStartContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDayStartExecutionContext context)
        {
            if (!UseSelectedDayStartContext)
            {
                context = null;
                return false;
            }
            Calls.Add("day-start-context");
            context = new FakeDayStartContext(this, graph, DayStartRuntimeRosterOverride);
            return true;
        }
        public void BeginSimulationDay()
        {
            Calls.Add("begin-day");
            if (ThrowOnBeginDay) throw new InvalidOperationException("day start failed");
        }
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
        public bool TryCreateSelectedActorTurnContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedActorTurnExecutionContext context)
        {
            CurrentGraph = graph;
            Calls.Add("actor-context");
            List<DailyV1ActorTurnRosterProjection> runtimeRoster = graph.Npcs
                .Select(npc => new DailyV1ActorTurnRosterProjection(
                    npc.RuntimeId, null))
                .ToList();
            PersonStore people = SubstituteActorPersonStore
                ? new PersonStore()
                : graph.PersonStore;
            ActorChoiceStore choices = !SubstituteActorPersonStore && actorChoiceStore != null
                ? actorChoiceStore
                : new ActorChoiceStore(people);
            context = new FakeActorTurnContext(
                runtimeRoster, people, choices, new FakeActorTurnBoundary(this));
            return true;
        }
        public bool TryCreateSelectedActorTurnBoundary(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1ActorTurnExecutionBoundary boundary)
        {
            Assert.That(graph, Is.Not.Null);
            CurrentGraph = graph;
            Calls.Add("actor-context");
            boundary = new FakeActorTurnBoundary(this);
            return true;
        }
        internal DailyV1SelectedRuntimeOwnerGraph CurrentGraph { get; private set; }
        public bool TryCreateSelectedTravelDayContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedTravelDayExecutionContext context)
        {
            Assert.That(graph, Is.SameAs(CurrentGraph));
            Calls.Add("travel-context");
            context = new FakeTravelContext(this);
            return true;
        }

        private sealed class FakeDemographyContext : IDailyV1SelectedDemographyExecutionContext
        {
            internal FakeDemographyContext(DailyV1SelectedRuntimeOwnerGraph graph)
            {
                DailyV1CalendarDefinition definition = graph.Definitions.Calendar;
                Calendar = definition.monthLengths != null && definition.monthLengths.Count > 0
                    ? new SimulationCalendar(new CalendarDefinition(
                        new List<int>(definition.monthLengths), definition.daysPerWeek))
                    : new SimulationCalendar(new CalendarDefinition(
                        definition.monthsPerYear, definition.weeksPerMonth, definition.daysPerWeek));
                Boundary = new FakeDemographyBoundary(graph);
            }

            public SimulationCalendar Calendar { get; }
            public IPersonNaturalMortalitySampleProvider MortalitySamples => null;
            public IAggregateDemographyProvider AggregateProvider => null;
            public IDailyV1SelectedDemographyBoundary Boundary { get; }
        }

        private sealed class FakeDemographyBoundary : IDailyV1SelectedDemographyBoundary
        {
            private readonly IReadOnlyDictionary<string, int> representedFloors;

            internal FakeDemographyBoundary(DailyV1SelectedRuntimeOwnerGraph graph)
            {
                Dictionary<string, int> floors = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (DailyV1CityRuntimeOwners city in graph.Cities)
                    floors[city.RuntimeId] = city.Population.CurrentPopulation;
                representedFloors = floors;
            }

            public bool TryApplyPersonDeath(PersonId personId, out string failure)
            {
                failure = "No mortality is expected in this fixture.";
                return false;
            }

            public IReadOnlyDictionary<string, int> BuildResidentFloorSupplement() => representedFloors;
        }

        private sealed class FakeTravelContext : IDailyV1SelectedTravelDayExecutionContext
        {
            private readonly FakeBoundary owner;
            internal FakeTravelContext(FakeBoundary owner) => this.owner = owner;
            public IDailyV1SelectedTravelDayBoundary Boundary => EmptyTravelBoundary.Instance;
            public bool AdvanceSoloTravel => false;
            public bool ExecuteWithinPartyOwnerOperation(Func<bool> operation) => operation?.Invoke() == true;
            public void PublishCompletedTravelResults(
                IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
                IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults)
            {
                Assert.That(partyArrivals, Is.Empty);
                Assert.That(soloResults, Is.Empty);
                owner.Calls.Add("travel-results");
            }
            public void ObserveCompletedTravelResults() => owner.Calls.Add("travel-observation");
        }

        private sealed class EmptyTravelBoundary : IDailyV1SelectedTravelDayBoundary
        {
            internal static readonly EmptyTravelBoundary Instance = new EmptyTravelBoundary();
            public bool TryPrepareTransition(DailyV1NpcRuntimeOwners npc,
                DailyV1SelectedTravelTransitionPlan plan, bool arriving, out object projectionReceipt)
            { projectionReceipt = null; return false; }
            public void PublishCommittedTransition(DailyV1NpcRuntimeOwners npc,
                DailyV1SelectedTravelTransitionPlan plan, bool arriving, object projectionReceipt) =>
                throw new AssertionException("No travel transition is expected in this fixture.");
            public bool PublishTravelPartyArrival(TravelPartyRuntime party) => false;
            public void PublishSoloTravelResult(DailyV1SelectedSoloTravelDayResult result) =>
                throw new AssertionException("No solo travel result is expected in this fixture.");
        }

        private sealed class FakeActorTurnContext : IDailyV1SelectedActorTurnExecutionContext
        {
            internal FakeActorTurnContext(
                IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRoster,
                PersonStore personStore,
                ActorChoiceStore actorChoiceStore,
                IDailyV1ActorTurnExecutionBoundary boundary)
            {
                RuntimeRoster = runtimeRoster;
                PersonStore = personStore;
                ActorChoiceStore = actorChoiceStore;
                Boundary = boundary;
            }

            public IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
            public PersonStore PersonStore { get; }
            public ActorChoiceStore ActorChoiceStore { get; }
            public DailyV1ActorTurnCoreRoots CoreRoots => null;
            public IDailyV1ActorTurnExecutionBoundary Boundary { get; }
        }

        private sealed class FakeDayStartContext : IDailyV1SelectedDayStartExecutionContext
        {
            internal FakeDayStartContext(
                FakeBoundary owner,
                DailyV1SelectedRuntimeOwnerGraph graph,
                IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRosterOverride)
            {
                RuntimeRoster = runtimeRosterOverride ?? graph.Npcs.Select(npc =>
                    new DailyV1ActorTurnRosterProjection(npc.RuntimeId, null)).ToList();
                CrimeSystemPresent = owner.CrimeSystemPresent;
                JusticeSystemPresent = owner.JusticeSystemPresent;
                HasUnselectedLiveRuntimeMember = owner.HasUnselectedLiveRuntimeMember;
                FakeDayStartOperationBoundary operationBoundary =
                    new FakeDayStartOperationBoundary(owner, graph);
                CrimeJusticeBoundary = operationBoundary;
                MerchantTradeBoundary = operationBoundary;
            }

            public IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
            public bool HasUnselectedLiveRuntimeMember { get; }
            public bool CrimeSystemPresent { get; }
            public bool JusticeSystemPresent { get; }
            public IDailyV1SelectedCrimeJusticeDailyBoundary CrimeJusticeBoundary { get; }
            public IDailyV1SelectedMerchantTradeStateBoundary MerchantTradeBoundary { get; }
        }

        private sealed class FakeDayStartOperationBoundary
            : IDailyV1SelectedCrimeJusticeDailyBoundary,
                IDailyV1SelectedMerchantTradeStateBoundary
        {
            private readonly FakeBoundary owner;
            private readonly DailyV1SelectedRuntimeOwnerGraph graph;

            internal FakeDayStartOperationBoundary(
                FakeBoundary owner,
                DailyV1SelectedRuntimeOwnerGraph graph)
            {
                this.owner = owner;
                this.graph = graph;
            }

            public DailyV1JusticeRecordsOwner JusticeRecordsOwner =>
                owner.SubstituteJusticeRecordsOwner ?? graph.JusticeRecordsOwner;

            public void ExecuteWithinDailyMutation(
                DailyV1SelectedCrimeJusticeDailyPhase phase,
                Action mutation)
            {
                owner.DayStartPhases.Add(phase);
                owner.Calls.Add("crime:" + phase);
                mutation();
            }

            public bool CanProjectStatusChanges(
                DailyV1NpcRuntimeOwners actor,
                IReadOnlyList<DailyV1StatusDefinition> statusToAdd) =>
                graph.ContainsExactNpcOwner(actor);

            public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners actor)
            {
                if (!graph.ContainsExactNpcOwner(actor)
                    || !actor.CanAdvanceCrimeJusticeRevision(1))
                    throw new InvalidOperationException("The fake day-start owner is not admitted.");
            }

            public void NotifyStatusMutationCommitted(
                DailyV1NpcRuntimeOwners actor,
                DailyV1StatusDefinition status,
                bool added) => actor.NotifyCrimeJusticeMutationCommitted();

            public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners actor) =>
                actor.NotifyCrimeJusticeMutationCommitted();

            public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
                graph.JusticeRecordsOwner.ContainsSentence(sentence);

            public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence) { }
            public void Log(SimulationLogCategory category, string message) { }
            public void LogWarning(string message) { }

            public bool TryInstallPlanState(
                DailyV1NpcRuntimeOwners actor,
                DailyV1ItemDefinition item,
                string originCityRuntimeId,
                string targetCityRuntimeId,
                int plannedAmount,
                int remainingAmount,
                float purchasePricePerItem,
                int waitDaysAtDestination,
                int pendingTravelDays,
                string originDecisionId)
            {
                owner.Calls.Add("merchant-plan");
                return actor.MerchantTradePlan.TryInstallState(item,
                    originCityRuntimeId, targetCityRuntimeId, plannedAmount,
                    remainingAmount, purchasePricePerItem, waitDaysAtDestination,
                    pendingTravelDays, originDecisionId);
            }

            public bool TrySetTradeTravelPlan(
                DailyV1NpcRuntimeOwners actor,
                string targetCityRuntimeId,
                float utility,
                string originDecisionId) => false;

            public void ClearTradeTravelPlan(DailyV1NpcRuntimeOwners actor) { }
            public NpcDecisionRecord RecordTradeRedirect(
                DailyV1NpcRuntimeOwners actor,
                string targetLocationRuntimeId,
                CommercialDecisionEvidence evidence) => null;
            public void LogTradeDecisionFailure(string message) { }
            public void LogTrade(string message) { }
        }

        private sealed class FakeActorTurnBoundary : IDailyV1ActorTurnExecutionBoundary,
            IDailyV1SelectedActorChoiceExecutionContext,
            IDailyV1CoreActorChoiceExecutionBoundary,
            IDailyV1SelectedMerchantTradeStateBoundary,
            IDailyV1SelectedMerchantTradeStateAdmissionBoundary
        {
            private readonly FakeBoundary owner;
            private readonly bool? selectedActionExecution;
            internal FakeActorTurnBoundary(
                FakeBoundary owner,
                bool? selectedActionExecution = null)
            {
                this.owner = owner;
                this.selectedActionExecution = selectedActionExecution;
            }
            public bool IsReservedToday(DailyV1NpcRuntimeOwners actor) => false;
            public void ProcessScheduledDirective(DailyV1NpcRuntimeOwners actor, ScheduledDirective directive) =>
                owner.Calls.Add("directive");
            public bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal) => false;
            public void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor) =>
                owner.Calls.Add("actor-merchant:" + actor.RuntimeId);
            public void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor) { }

            public bool TryGetActorChoiceExecutionContext(
                DailyV1NpcRuntimeOwners actor,
                out IDailyV1ActorChoiceExecutionBoundary executionBoundary)
            {
                executionBoundary = owner.ActorChoiceSelectedActionExecutionOverride.HasValue
                    ? new FakeActorTurnBoundary(
                        owner, owner.ActorChoiceSelectedActionExecutionOverride.Value)
                    : this;
                return true;
            }

            public void ClearCurrentAction(DailyV1NpcRuntimeOwners actor) { }
            public bool TryPrepareRequestedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool TryRefreshRequestedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool CanCommitRequestedActionSlot(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool TryPublishRequestedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public void CancelPreparedRequestedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) { }
            public void LogRequestedActionDecisionFailure(string message) { }
            public NpcActionResultType? ExecuteCurrentAction(DailyV1NpcRuntimeOwners actor) => null;

            public bool TryInstallPlanState(
                DailyV1NpcRuntimeOwners actor,
                DailyV1ItemDefinition item,
                string originCityRuntimeId,
                string targetCityRuntimeId,
                int plannedAmount,
                int remainingAmount,
                float purchasePricePerItem,
                int waitDaysAtDestination,
                int pendingTravelDays,
                string originDecisionId) => true;
            public bool TrySetTradeTravelPlan(
                DailyV1NpcRuntimeOwners actor,
                string targetCityRuntimeId,
                float utility,
                string originDecisionId) => true;
            public void ClearTradeTravelPlan(DailyV1NpcRuntimeOwners actor) { }
            public NpcDecisionRecord RecordTradeRedirect(
                DailyV1NpcRuntimeOwners actor,
                string targetLocationRuntimeId,
                CommercialDecisionEvidence evidence) => null;
            public void LogTradeDecisionFailure(string message) { }
            public void LogTrade(string message) { }
            public bool ExecuteWithinSelectedMerchantDailyOperation(
                DailyV1NpcRuntimeOwners actor,
                Func<bool> operation) => operation != null && operation();

            public IAuthoritativeRandomSource RandomSource => owner.randomSource;
            public bool UsesSelectedDailyV1ActionExecution(DailyV1NpcRuntimeOwners actor) =>
                selectedActionExecution ?? owner.UseSelectedActionExecution;
            public void LogSkippedDirective(ScheduledDirective directive, string reason) { }
            public bool TryCreateActionProjectionBoundary(
                DailyV1NpcRuntimeOwners actor,
                out IDailyV1SelectedActionProjectionBoundary projectionBoundary)
            { projectionBoundary = null; return false; }
            public bool TryGetDecisionRecordOwners(
                DailyV1NpcRuntimeOwners actor,
                out RuntimeIdAllocator idAllocator,
                out SimulationRecordSequence recordSequence,
                out NpcDecisionStore decisionStore)
            {
                owner.DecisionOwnerLookupCount++;
                idAllocator = owner.runtimeIdAllocator;
                recordSequence = owner.recordSequence;
                decisionStore = owner.decisionStore;
                return !owner.FailDecisionRecordOwnerResolution
                    && idAllocator != null && recordSequence != null && decisionStore != null;
            }
            public bool TryPrepareSelectedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionCandidate selected,
                IDailyV1SelectedActionProjectionBoundary decisionBoundary,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool CanCommitSelectedActionSlot(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool TryPublishSelectedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public void CancelPreparedSelectedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) { }
            public void LogSelectedActionDecisionFailure(DailyV1NpcRuntimeOwners actor, string message) { }
            public bool TryPrepareCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) => false;
            public bool CanCommitCurrentActionClear(DailyV1NpcRuntimeOwners actor) => false;
            public bool TryPublishCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) => false;
            public void CancelPreparedCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) { }
            public void LogChosenActionTarget(DailyV1NpcRuntimeOwners actor) { }
            public bool TryGetDispatchBoundary(
                DailyV1NpcRuntimeOwners actor,
                out IDailyV1SelectedActionDispatchBoundary dispatchBoundary)
            {
                owner.SelectedActionDispatchLookupCount++;
                dispatchBoundary = null;
                return false;
            }
            public bool TryCompleteDispatch(
                DailyV1NpcRuntimeOwners actor,
                DailyV1SelectedActionDispatchResult dispatch,
                out bool actionSucceeded,
                out NpcActionResultType? resultType,
                out string resultMessage)
            { actionSucceeded = false; resultType = null; resultMessage = null; return false; }
            public bool TryGetActionStatusBoundary(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcRuntimeOwners target,
                bool reuseExistingOperation,
                out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary boundary)
            { boundary = null; return false; }
            public bool TryPrepareForcedOutcomeActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1ActionDefinition action,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool TryGetForcedEscapeBoundary(
                DailyV1NpcRuntimeOwners actor,
                DailyV1ActionDefinition action,
                out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
            { boundary = null; return false; }
        }
    }

    private sealed class FakeRuntimeAdvanceBoundary : IDailyV1RuntimeAdvanceBoundary
    {
        private readonly DailyV1ExecutionBoundaryState state;
        private readonly IDailyV1SelectedDailyExecutionBoundary executionBoundary;
        internal List<string> Calls { get; } = new List<string>();
        internal bool FailSelectedContext { get; set; }

        internal FakeRuntimeAdvanceBoundary(
            DailyV1ExecutionBoundaryState state,
            IDailyV1SelectedDailyExecutionBoundary executionBoundary)
        {
            this.state = state;
            this.executionBoundary = executionBoundary;
        }

        public DailyV1ExecutionBoundaryState ExecutionState => state;
        public bool HasIntradayTimeline => false;
        public bool TryEnterDailyAdvanceOperation(out IDisposable operationScope)
        {
            Calls.Add("operation");
            operationScope = new FakeOperationScope();
            return true;
        }
        public bool TryCreateSelectedDailyExecutionBoundary(
            out IDailyV1SelectedDailyExecutionBoundary boundary)
        {
            Calls.Add("execution-boundary");
            boundary = FailSelectedContext ? null : executionBoundary;
            return !FailSelectedContext;
        }
        public bool TryAdvanceDayCore(out SimulationRuntimeAdvanceFailure failure)
        {
            failure = SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed;
            return false;
        }
        public bool TryAdvanceIntradayToNextDayBoundary(out SimulationRuntimeAdvanceFailure failure)
        {
            failure = SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed;
            return false;
        }
        public bool TryCaptureCompletedDailyCaptureEvidence(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure)
        {
            Calls.Add("capture-evidence");
            ownerSections = DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
            mutationEpoch = 1L;
            failure = DailyCaptureEligibilityFailure.None;
            return true;
        }

        public bool TryCaptureDailyBoundaryAdmissionEvidence(
            bool sessionReplacement,
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure)
        {
            Calls.Add("admission-evidence");
            ownerSections = DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
            mutationEpoch = 1L;
            failure = DailyCaptureEligibilityFailure.None;
            return true;
        }

        public bool TryCaptureContinuationSaveEvidence(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure) =>
            TryCaptureDailyBoundaryAdmissionEvidence(
                sessionReplacement: false, out ownerSections, out mutationEpoch, out failure);

        public void FaultRuntimeAdmission() => Calls.Add("fault");
    }

    private sealed class FakeOperationScope : IDisposable
    {
        public void Dispose() { }
    }
}
