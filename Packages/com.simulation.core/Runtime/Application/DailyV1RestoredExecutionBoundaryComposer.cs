using System;

/// <summary>
/// Builds the Core execution boundary for one already staged Daily-v1
/// restoration. The Unity host supplies exact, unpublished owner instances;
/// Core verifies their graph and binds the shared execution roots before the
/// host constructs its runtime projection.
/// </summary>
internal static class DailyV1RestoredExecutionBoundaryComposer
{
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
}