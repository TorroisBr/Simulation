using System;
using System.Collections.Generic;

/// <summary>
/// Host ports for the selected Daily-v1 stage. Core owns the selected
/// Crime/Justice day-start, Merchant plan urgency, economy, local-knowledge,
/// ordered actor-turn, directive-preparation, and travel algorithms, along
/// with demographic ordering and aggregate-owner writes.
/// The host contributes the existing person-death writer, represented-resident
/// facts, P12 admission, and Unity projection. Place content and autonomous
/// expedition production are outside this accepted profile.
/// </summary>
internal interface IDailyV1SelectedDailyExecutionBoundary
{
    void BeginLoggerDay(long absoluteDay);
    bool TryCreateSelectedDemographyContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDemographyExecutionContext context);
    void PublishSelectedDemographyResult(DailyV1SelectedDemographyResult result);
    bool TryCreateSelectedDayStartContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDayStartExecutionContext context);
    /// <summary>Legacy whole-stage path used only when selected owner coverage is mixed.</summary>
    void BeginSimulationDay();
    void EnsureSelectedEconomyCanRun(DailyV1SelectedRuntimeOwnerGraph graph);
    void LogSelectedDailyEconomy(
        DailyV1CityRuntimeOwners owner,
        SimulationLogCategory category,
        string message);
    void EnsureSelectedLocalKnowledgeCanRun(DailyV1SelectedRuntimeOwnerGraph graph);
    bool TryCreateSelectedActorTurnContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedActorTurnExecutionContext context);
    bool TryCreateSelectedTravelDayContext(
        out IDailyV1SelectedTravelDayExecutionContext context);
}

/// <summary>
/// Exact selected runtime roster and compatibility ports for the established
/// start-of-day Crime/Justice and Merchant-plan operations. Core owns invoking
/// these operations; Unity supplies only their existing admission and
/// projection boundaries. A mixed roster uses the legacy whole-stage path.
/// </summary>
internal interface IDailyV1SelectedDayStartExecutionContext
{
    IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
    bool CrimeSystemPresent { get; }
    bool JusticeSystemPresent { get; }
    IDailyV1SelectedCrimeJusticeDailyBoundary CrimeJusticeBoundary { get; }
    IDailyV1SelectedMerchantTradeStateBoundary MerchantTradeBoundary { get; }
}

internal interface IDailyV1SelectedDemographyExecutionContext
{
    PersonStore PersonStore { get; }
    SimulationCalendar Calendar { get; }
    IPersonNaturalMortalitySampleProvider MortalitySamples { get; }
    IAggregateDemographyProvider AggregateProvider { get; }
    IDailyV1SelectedDemographyBoundary Boundary { get; }
}

/// <summary>
/// Exact owner inputs and host ports for Core's existing ordered actor-turn
/// stage. The host contributes runtime identity projections and P8/P12/action
/// adapters; Core builds the roster and invokes the operation stage.
/// </summary>
internal interface IDailyV1SelectedActorTurnExecutionContext
{
    IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
    PersonStore PersonStore { get; }
    ActorChoiceStore ActorChoiceStore { get; }
    IDailyV1ActorTurnExecutionBoundary Boundary { get; }
}

/// <summary>
/// Host facts and P12/projection ports for the existing Core-selected travel
/// operation. Core invokes the travel algorithm and owns party-before-solo
/// ordering; the host only supplies exact adapters and publishes its results.
/// </summary>
internal interface IDailyV1SelectedTravelDayExecutionContext
{
    TravelPartyStore PartyStore { get; }
    IReadOnlyCollection<string> ManagedTravelPartyNpcRuntimeIds { get; }
    IDailyV1SelectedTravelDayBoundary Boundary { get; }
    bool AdvanceSoloTravel { get; }
    bool ExecuteWithinPartyOwnerOperation(Func<bool> operation);
    void PublishCompletedTravelResults(
        IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
        IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults);
    void ObserveCompletedTravelResults();
}

/// <summary>
/// Owns the order of the selected Daily-v1 runtime phases. Exact-empty
/// place-content and autonomous-expedition capabilities are not invoked for
/// this admitted profile; Core-owned graph and operation stages remain in Core.
/// </summary>
internal static class DailyV1SelectedDailyExecutionStage
{
    internal static void Advance(
        DailyV1ExecutionBoundaryState executionState,
        long absoluteDay,
        DailyV1SelectedRuntimeOwnerGraph graph,
        ScheduledDirectiveStore scheduledDirectives,
        IDailyV1SelectedDailyExecutionBoundary boundary)
    {
        if (executionState == null) throw new ArgumentNullException(nameof(executionState));
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (!executionState.SelectedRuntimeExecutionRootsBound
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph))
            throw new InvalidOperationException(
                "The selected Daily-v1 stage requires the exact runtime execution roots bound to its Core state.");

        AdvanceCore(executionState, absoluteDay, graph, scheduledDirectives, boundary);
    }

    internal static void Advance(
        long absoluteDay,
        DailyV1SelectedRuntimeOwnerGraph graph,
        ScheduledDirectiveStore scheduledDirectives,
        IDailyV1SelectedDailyExecutionBoundary boundary)
    {
        AdvanceCore(null, absoluteDay, graph, scheduledDirectives, boundary);
    }

    private static void AdvanceCore(
        DailyV1ExecutionBoundaryState executionState,
        long absoluteDay,
        DailyV1SelectedRuntimeOwnerGraph graph,
        ScheduledDirectiveStore scheduledDirectives,
        IDailyV1SelectedDailyExecutionBoundary boundary)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (boundary == null) throw new ArgumentNullException(nameof(boundary));
        if (absoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(absoluteDay));
        if (!ReferenceEquals(graph.ScheduledDirectives, scheduledDirectives))
            throw new InvalidOperationException(
                "The selected Daily-v1 stage requires the exact directive store bound to its Core owner graph.");

        EffectiveSimulationConfiguration selectedConfiguration =
            graph.Definitions?.EffectiveConfiguration;
        if (selectedConfiguration == null)
            throw new InvalidOperationException(
                "The selected Daily-v1 graph has no admitted effective configuration.");

        IDailyV1SelectedActorTurnExecutionContext actorTurnContext = null;
        IReadOnlyList<DailyV1ActorTurnRosterEntry> actorRoster = null;
        if (executionState != null
            && (!boundary.TryCreateSelectedActorTurnContext(
                    graph, out actorTurnContext)
                || actorTurnContext == null
                || actorTurnContext.RuntimeRoster == null
                || actorTurnContext.PersonStore == null
                || !ReferenceEquals(graph.PersonStore, actorTurnContext.PersonStore)
                || actorTurnContext.ActorChoiceStore == null
                || actorTurnContext.Boundary == null
                || !DailyV1ActorTurnExecutionStage.TryBuildRoster(
                    graph, actorTurnContext.RuntimeRoster, out actorRoster)
                || !HasExactSelectedExecutionRoots(
                    executionState, graph, actorTurnContext, actorRoster)))
            throw new InvalidOperationException(
                "The selected Daily-v1 host could not prove the exact actor, choice, random, allocator, sequence, and decision roots before daily mutations.");

        boundary.BeginLoggerDay(absoluteDay);

        // The accepted Daily-v1 proving profile disables both demographic
        // writers. In that profile the shared Core stage must not require a
        // Unity person-death writer or represented-resident projection simply
        // to perform an empty phase. Enabled demographic policies continue
        // through the exact existing owner/admission boundary below.
        DailyV1SelectedDemographyResult demographyResult;
        if (selectedConfiguration.NaturalMortality.Enabled
            || selectedConfiguration.AggregateDemography.Enabled)
        {
            if (!boundary.TryCreateSelectedDemographyContext(
                    graph, out IDailyV1SelectedDemographyExecutionContext demographyContext)
                || demographyContext == null
                || demographyContext.PersonStore == null
                || !ReferenceEquals(graph.PersonStore, demographyContext.PersonStore)
                || demographyContext.Calendar == null
                || demographyContext.Boundary == null)
                throw new InvalidOperationException(
                    "The selected Daily-v1 host could not provide the exact demographic owners and writer boundary.");

            demographyResult = DailyV1SelectedDemographyStage.Advance(
                graph,
                demographyContext.PersonStore,
                absoluteDay,
                demographyContext.Calendar,
                selectedConfiguration.NaturalMortality,
                selectedConfiguration.AggregateDemography,
                demographyContext.MortalitySamples,
                demographyContext.AggregateProvider,
                demographyContext.Boundary);
        }
        else
        {
            demographyResult = new DailyV1SelectedDemographyResult(absoluteDay);
        }
        boundary.PublishSelectedDemographyResult(demographyResult);
        if (boundary.TryCreateSelectedDayStartContext(
                graph, out IDailyV1SelectedDayStartExecutionContext dayStartContext))
        {
            if (dayStartContext == null
                || dayStartContext.RuntimeRoster == null
                || dayStartContext.CrimeJusticeBoundary == null
                || dayStartContext.MerchantTradeBoundary == null
                || !ReferenceEquals(graph.JusticeRecordsOwner,
                    dayStartContext.CrimeJusticeBoundary.JusticeRecordsOwner)
                || graph.Definitions.EffectiveConfiguration?.MerchantTrade == null
                || !DailyV1ActorTurnExecutionStage.TryBuildRoster(
                    graph, dayStartContext.RuntimeRoster,
                    out IReadOnlyList<DailyV1ActorTurnRosterEntry> dayStartRoster))
                throw new InvalidOperationException(
                    "The selected Daily-v1 host could not provide its exact day-start owner roster and boundaries.");

            List<DailyV1NpcRuntimeOwners> exactNpcOwners = new List<DailyV1NpcRuntimeOwners>();
            foreach (DailyV1ActorTurnRosterEntry entry in dayStartRoster)
                if (entry?.ActorOwner != null)
                    exactNpcOwners.Add(entry.ActorOwner);
            if (!graph.MatchesExactOwnerInstances(graph.Cities, exactNpcOwners))
                throw new InvalidOperationException(
                    "The selected Daily-v1 day-start roster does not match the exact Core NPC owners.");

            if (!DailyV1SelectedCrimeJusticeDailyStage.TryRun(
                    graph,
                    dayStartRoster,
                    dayStartContext.CrimeSystemPresent,
                    dayStartContext.JusticeSystemPresent,
                    dayStartContext.CrimeJusticeBoundary))
                throw new InvalidOperationException(
                    "The selected Core Crime/Justice day-start stage rejected its exact owner graph.");

            if (graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled
                && !DailyV1SelectedMerchantTradeStateOperation.TryAdvancePendingTravelDays(
                    graph, exactNpcOwners, dayStartContext.MerchantTradeBoundary))
                throw new InvalidOperationException(
                    "The selected Core Merchant plan-urgency stage rejected its exact owner graph.");
        }
        else
        {
            // Dynamic or otherwise unselected members retain the established
            // whole-stage Unity lifecycle until their owner closure is admitted.
            boundary.BeginSimulationDay();
        }
        if (scheduledDirectives != null)
        {
            if (graph.RuntimeIdentityIndex == null)
                throw new InvalidOperationException(
                    "Selected Daily-v1 directive preparation requires the Core runtime identity index.");
            scheduledDirectives.PrepareSelectedDailyV1Day(absoluteDay, graph.RuntimeIdentityIndex);
        }

        if (graph.Definitions.EffectiveConfiguration?.Economy?.Enabled == true)
        {
            boundary.EnsureSelectedEconomyCanRun(graph);
            if (!graph.TrySimulateDailyEconomyDay(
                    graph.Cities,
                    absoluteDay,
                    GetCalendarSemanticVersion(graph.Definitions.Calendar),
                    boundary.LogSelectedDailyEconomy))
                throw new InvalidOperationException(
                    "The selected Core economy stage could not complete against its exact runtime owner graph.");
        }

        boundary.EnsureSelectedLocalKnowledgeCanRun(graph);
        if (!DailyV1SelectedLocalKnowledgeRefreshStage.TryRun(graph, absoluteDay))
            throw new InvalidOperationException(
                "The selected Core local-knowledge stage could not complete against its exact runtime owner graph.");

        if (executionState == null
            && (!boundary.TryCreateSelectedActorTurnContext(
                    graph, out actorTurnContext)
                || actorTurnContext == null
                || actorTurnContext.RuntimeRoster == null
                || actorTurnContext.PersonStore == null
                || !ReferenceEquals(graph.PersonStore, actorTurnContext.PersonStore)
                || actorTurnContext.ActorChoiceStore == null
                || actorTurnContext.Boundary == null
                || !DailyV1ActorTurnExecutionStage.TryBuildRoster(
                    graph, actorTurnContext.RuntimeRoster, out actorRoster)))
            throw new InvalidOperationException(
                "The selected Core actor-turn stage could not build the exact admitted graph and runtime roster.");
        if (graph.Definitions.EffectiveConfiguration?.MerchantTrade == null)
            throw new InvalidOperationException(
                "The selected Core actor-turn stage has no admitted Merchant configuration.");

        if (!DailyV1ActorTurnExecutionStage.TryRunOrdered(
                graph,
                actorRoster,
                actorTurnContext.PersonStore,
                actorTurnContext.ActorChoiceStore,
                scheduledDirectives,
                absoluteDay,
                graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled,
                actorTurnContext.Boundary))
            throw new InvalidOperationException(
                "The selected Core actor-turn stage could not run with the exact admitted graph and runtime roster.");
        if (!boundary.TryCreateSelectedTravelDayContext(
                out IDailyV1SelectedTravelDayExecutionContext travelContext)
            || travelContext == null
            || travelContext.Boundary == null
            || travelContext.ManagedTravelPartyNpcRuntimeIds == null)
            throw new InvalidOperationException(
                "The selected Daily-v1 host could not provide its exact travel projection and P12 context.");
        if (!TryAdvanceTravelDay(graph, travelContext,
                out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
                out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults))
            throw new InvalidOperationException(
                "The selected Core TravelParty/solo-travel stages could not advance the exact owner graph.");
        travelContext.PublishCompletedTravelResults(partyArrivals, soloResults);
        if (!DailyV1SelectedMerchantTradeStateOperation.ObserveCompletedTravelResults(
                graph, partyArrivals, soloResults, absoluteDay))
            throw new InvalidOperationException(
                "The selected Core post-travel market observation could not apply to its exact owner graph.");
        travelContext.ObserveCompletedTravelResults();
    }

    private static bool HasExactSelectedExecutionRoots(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedActorTurnExecutionContext actorTurnContext,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> actorRoster)
    {
        if (!(actorTurnContext.Boundary is IDailyV1SelectedActorActionExecutionBoundary actionBoundary)
            || !executionState.HasExactSelectedRuntimeActorTurnRoots(
                graph,
                actorTurnContext.PersonStore,
                actorTurnContext.ActorChoiceStore,
                actionBoundary.RandomSource)
            || !ReferenceEquals(actionBoundary.RandomSource, executionState.SelectedRandomSource))
            return false;

        foreach (DailyV1ActorTurnRosterEntry entry in actorRoster)
        {
            DailyV1NpcRuntimeOwners actor = entry?.ActorOwner;
            if (actor == null || !actionBoundary.UsesSelectedDailyV1ActionExecution(actor))
                continue;

            if (!actionBoundary.TryGetDecisionRecordOwners(
                actor,
                out RuntimeIdAllocator idAllocator,
                out SimulationRecordSequence recordSequence,
                out NpcDecisionStore decisionStore))
                return false;
            if (!ReferenceEquals(idAllocator, executionState.SelectedRuntimeIdAllocator)
                || !ReferenceEquals(recordSequence, executionState.SelectedRecordSequence)
                || !ReferenceEquals(decisionStore, executionState.SelectedDecisionStore))
                return false;
        }

        return true;
    }

    internal static bool TryAdvanceTravelDay(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedTravelDayExecutionContext context,
        out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
        out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults)
    {
        partyArrivals = Array.Empty<DailyV1SelectedTravelPartyArrivalResult>();
        soloResults = Array.Empty<DailyV1SelectedSoloTravelDayResult>();
        if (graph == null || context == null
            || context.Boundary == null
            || context.ManagedTravelPartyNpcRuntimeIds == null)
            return false;

        return DailyV1SelectedTravelDayStage.TryAdvance(
            graph,
            context.PartyStore,
            context.ManagedTravelPartyNpcRuntimeIds,
            context.Boundary,
            context.ExecuteWithinPartyOwnerOperation,
            context.AdvanceSoloTravel,
            out partyArrivals,
            out soloResults);
    }

    private static string GetCalendarSemanticVersion(DailyV1CalendarDefinition calendar)
    {
        if (calendar == null) throw new InvalidOperationException(
            "The selected Core economy stage requires an admitted calendar definition.");

        CalendarDefinition definition = calendar.monthLengths != null && calendar.monthLengths.Count > 0
            ? new CalendarDefinition(new List<int>(calendar.monthLengths), calendar.daysPerWeek)
            : new CalendarDefinition(calendar.monthsPerYear, calendar.weeksPerMonth, calendar.daysPerWeek);
        return new SimulationCalendar(definition).SemanticVersion;
    }
}
