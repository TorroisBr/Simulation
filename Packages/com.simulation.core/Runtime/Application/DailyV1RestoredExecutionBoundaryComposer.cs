using System;
using System.Collections.Generic;

/// <summary>
/// Builds the Core execution boundary for one already staged Daily-v1
/// restoration. The Unity host supplies exact, unpublished owner instances;
/// Core verifies their graph and binds the shared execution roots before the
/// host constructs its runtime projection.
/// </summary>
internal static class DailyV1RestoredExecutionBoundaryComposer
{
    /// <summary>
    /// Admits the exact staged P12-D owner instances and composes their shared
    /// execution boundary as one Core-owned operation. The host may decode
    /// persisted values and create compatibility projections, but it does not
    /// choose owner order, validate cross-owner restore relationships, or
    /// assemble the authoritative restored graph.
    /// </summary>
    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        EffectiveSimulationConfiguration effectiveConfiguration,
        CalendarDefinition calendarDefinition,
        SimulationTime simulationTime,
        SimulationRuntimeAdmissionContext admissionContext,
        WorldId worldId,
        IReadOnlyList<DailyV1CityRuntimeOwners> cityOwners,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecords,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        PersonStore personStore,
        ScheduledDirectiveStore directives,
        ExpeditionStore expeditions,
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore,
        out DailyV1ExecutionBoundaryState executionBoundaryState,
        out SimulationCalendar calendar,
        out string diagnostic)
    {
        executionBoundaryState = null;
        calendar = null;
        diagnostic = null;
        if (calendarDefinition == null)
        {
            diagnostic = "The restored Core boundary requires the selected calendar definition.";
            return false;
        }

        if (!DailyV1P12DSelectedOwnerGraphStager.TryCreate(
                definitions,
                cityOwners,
                npcOwners,
                spatialNetwork,
                justiceRecords,
                travelParties,
                runtimeIdentityIndex,
                personStore,
                directives,
                expeditions,
                out DailyV1SelectedRuntimeOwnerGraph ownerGraph,
                out DailyV1P12DSelectedOwnerGraphFailure graphFailure)
            || ownerGraph == null
            || graphFailure != DailyV1P12DSelectedOwnerGraphFailure.None)
        {
            diagnostic = "Core rejected the exact staged P12 City/NPC owners or their cross-owner relationships.";
            return false;
        }

        SimulationCalendar candidateCalendar = new SimulationCalendar(calendarDefinition);
        if (!TryCreate(
                definitions,
                effectiveConfiguration,
                candidateCalendar,
                simulationTime,
                admissionContext,
                worldId,
                ownerGraph,
                runtimeIdAllocator,
                randomSource,
                recordSequence,
                decisionStore,
                actorChoiceStore,
                out DailyV1ExecutionBoundaryState candidateBoundary,
                out diagnostic))
            return false;

        executionBoundaryState = candidateBoundary;
        calendar = candidateCalendar;
        return true;
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        EffectiveSimulationConfiguration effectiveConfiguration,
        SimulationCalendar calendar,
        SimulationTime simulationTime,
        SimulationRuntimeAdmissionContext admissionContext,
        WorldId worldId,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore,
        out DailyV1ExecutionBoundaryState executionBoundaryState,
        out string diagnostic)
    {
        executionBoundaryState = null;
        diagnostic = null;
        if (definitions == null
            || effectiveConfiguration == null
            || !ReferenceEquals(definitions.EffectiveConfiguration, effectiveConfiguration)
            || calendar == null
            || simulationTime == null
            || admissionContext == null
            || admissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !admissionContext.IsRestoredContinuation
            || !admissionContext.IsOwnedByCurrentThread()
            || worldId == null
            || ownerGraph == null
            || !ReferenceEquals(ownerGraph.Definitions, definitions)
            || ownerGraph.Cities == null || ownerGraph.Cities.Count == 0
            || ownerGraph.Npcs == null || ownerGraph.Npcs.Count == 0
            || ownerGraph.SpatialNetwork == null
            || ownerGraph.JusticeRecordsOwner == null
            || ownerGraph.TravelParties == null
            || ownerGraph.RuntimeIdentityIndex == null
            || ownerGraph.PersonStore == null
            || ownerGraph.ScheduledDirectives == null
            || ownerGraph.Expeditions == null
            || runtimeIdAllocator == null
            || randomSource == null
            || recordSequence == null
            || decisionStore == null
            || actorChoiceStore == null
            || !ReferenceEquals(actorChoiceStore.PersonStore, ownerGraph.PersonStore))
        {
            diagnostic = "The restored Core boundary requires exact admitted definitions, selected owners, P12 roots, and owner-thread admission.";
            return false;
        }

        if (!TryBindRestoredP8DRouteOwners(ownerGraph, simulationTime, out diagnostic))
            return false;

        DailyV1ExecutionBoundaryState candidate = new DailyV1ExecutionBoundaryState(
            simulationTime,
            admissionContext,
            SimulationRuntimeCompositionProfile.Standard,
            worldId);
        if (!candidate.TryBindRuntimeConfiguration(effectiveConfiguration, calendar)
            || !candidate.TryBindSelectedRuntimeOwnerGraph(ownerGraph)
            || !candidate.TryBindInitialSelectedRuntimeActorRosterFromOwnerGraph()
            || !candidate.TryBindSelectedRuntimeExecutionRoots(
                runtimeIdAllocator, randomSource, recordSequence, decisionStore, actorChoiceStore)
            || !candidate.HasAdmittedSelectedDailyExecution)
        {
            diagnostic = "Core rejected the restored selected owner graph or one of its exact execution roots.";
            return false;
        }

        executionBoundaryState = candidate;
        return true;
    }

    private static bool TryBindRestoredP8DRouteOwners(
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        SimulationTime simulationTime,
        out string diagnostic)
    {
        diagnostic = null;
        if (ownerGraph.SpatialRouteKnowledgeStore == null
            && ownerGraph.PersonRoutePlanStore == null)
        {
            SpatialRouteKnowledgeStore knowledge =
                new SpatialRouteKnowledgeStore(ownerGraph.PersonStore);
            PersonRoutePlanStore plans = new PersonRoutePlanStore(
                ownerGraph.PersonStore, knowledge,
                () => simulationTime.AbsoluteDay,
                ownerGraph.IsPersonInSpatialTransit);
            if (!ownerGraph.TryBindP8DRouteOwners(knowledge, plans))
            {
                diagnostic = "Core rejected the exact-empty restored P8-D route owners.";
                return false;
            }
        }

        if (ownerGraph.SpatialRouteKnowledgeStore == null
            || ownerGraph.PersonRoutePlanStore == null
            || ownerGraph.SpatialRouteKnowledgeStore.ObservationCount != 0
            || ownerGraph.SpatialRouteKnowledgeStore.Revision != 0L
            || ownerGraph.PersonRoutePlanStore.PlanCount != 0
            || ownerGraph.PersonRoutePlanStore.Revision != 0L)
        {
            diagnostic = "Core requires the selected restored Daily-v1 P8-D route sections to remain exactly empty.";
            return false;
        }

        if (ownerGraph.SpatialAuthority != null
            && ownerGraph.PersonSpatialPositionStore != null
            && ownerGraph.SpatialRoutePlanningSystem == null
            && ownerGraph.P8ETravelCoordinator == null)
        {
            Func<long> currentWorldDayProvider = () => simulationTime.AbsoluteDay;
            SpatialRoutePlanningSystem planner = new SpatialRoutePlanningSystem(
                ownerGraph.PersonStore, ownerGraph.SpatialAuthority,
                ownerGraph.SpatialRouteKnowledgeStore);
            P8ETravelTransactionCoordinator coordinator =
                new P8ETravelTransactionCoordinator(ownerGraph.PersonSpatialPositionStore,
                    ownerGraph.PersonRoutePlanStore, ownerGraph.SpatialRouteKnowledgeStore,
                    ownerGraph.SpatialAuthority.PassageAuthority, currentWorldDayProvider);
            if (!ownerGraph.TryBindP8ETravelComponents(planner, coordinator))
            {
                diagnostic = "Core rejected the restored P8-E components over the exact staged owners.";
                return false;
            }
        }
        else if ((ownerGraph.SpatialRoutePlanningSystem == null)
            != (ownerGraph.P8ETravelCoordinator == null))
        {
            diagnostic = "The restored P8-E planner and transaction coordinator must be present together.";
            return false;
        }

        return true;
    }
}
