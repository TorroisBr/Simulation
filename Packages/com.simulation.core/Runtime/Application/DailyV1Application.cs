using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

internal enum DailyV1P12FCoreOwnerRestoreStep
{
    PoliticalKnowledge = 0,
    ScheduledDirectives = 1,
    ActorChoices = 2,
    TravelParties = 3,
    Expeditions = 4
}

/// <summary>Private selected Core owner candidates staged together for P12-F.</summary>
internal sealed class DailyV1P12FCoreOwnerRestoreStage
{
    internal PoliticalKnowledgeStore PoliticalKnowledge { get; }
    internal ScheduledDirectiveStore ScheduledDirectives { get; }
    internal ActorChoiceStore ActorChoices { get; }
    internal TravelPartyStore TravelParties { get; }
    internal ExpeditionStore Expeditions { get; }

    internal DailyV1P12FCoreOwnerRestoreStage(
        PoliticalKnowledgeStore politicalKnowledge,
        ScheduledDirectiveStore scheduledDirectives,
        ActorChoiceStore actorChoices,
        TravelPartyStore travelParties,
        ExpeditionStore expeditions)
    {
        PoliticalKnowledge = politicalKnowledge ?? throw new ArgumentNullException(nameof(politicalKnowledge));
        ScheduledDirectives = scheduledDirectives ?? throw new ArgumentNullException(nameof(scheduledDirectives));
        ActorChoices = actorChoices ?? throw new ArgumentNullException(nameof(actorChoices));
        TravelParties = travelParties ?? throw new ArgumentNullException(nameof(travelParties));
        Expeditions = expeditions ?? throw new ArgumentNullException(nameof(expeditions));
    }
}

/// <summary>
/// Core-owned lifetime and operation gate for one selected Daily-v1
/// application. Host adapters may retain projections, but this instance owns
/// the exact active Core session and its serialized publication boundary.
/// </summary>
internal sealed class DailyV1Application
{
    private readonly DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession> activeSessionGate =
        new DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>(
            session => session.ExecutionBoundaryState);
    private SimulationRuntimeAdmissionContext bootstrapAdmissionContext;
    private bool initialContinuationLoadAttempted;
    private bool initialContinuationLoadBlocksGenesis;
    private bool initialGenesisPreparationAttempted;
    private DailyV1GenesisComposition preparedInitialGenesis;
    private DailyV1ActiveRuntimeSession preparedInitialSession;

    // Construction remains internal until the complete TryStart path can
    // create and return the application only after genesis or restore commits.
    internal DailyV1Application() { }

    /// <summary>
    /// Captures the selected bootstrap owner-thread identity once, inside the
    /// Core application that will own the resulting active session.
    /// </summary>
    internal bool TryCaptureBootstrapAdmissionContext(
        out SimulationRuntimeAdmissionContext admissionContext)
    {
        lock (activeSessionGate.SyncRoot)
        {
            admissionContext = null;
            if (bootstrapAdmissionContext != null
                || activeSessionGate.Current != null
                || activeSessionGate.ActiveOperationCount != 0)
                return false;

            bootstrapAdmissionContext = new SimulationRuntimeAdmissionContext(
                SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                Thread.CurrentThread,
                Thread.CurrentThread.ManagedThreadId);
            admissionContext = bootstrapAdmissionContext;
            return true;
        }
    }

    /// <summary>
    /// Prepares selected Daily-v1 genesis using Core-owned WorldId allocation.
    /// Production host adapters may observe the established stage sequence,
    /// but cannot supply or replace runtime identities. The allocator-injection
    /// overload below remains an internal deterministic test seam.
    /// </summary>
    internal bool TryPrepareInitialGenesis(
        DailyV1EffectiveDefinitions definitions,
        Action<string> stageCompleted,
        out DailyV1GenesisComposition composition,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic) =>
        TryPrepareInitialGenesisCore(
            definitions, null, null, stageCompleted, out composition, out candidate, out diagnostic);

    /// <summary>Selected Unity host supplies its existing diagnostic sink; Core still creates all runtime roots.</summary>
    internal bool TryPrepareInitialGenesisWithHostLogger(
        DailyV1EffectiveDefinitions definitions,
        SimulationLogger diagnosticLogger,
        Action<string> stageCompleted,
        out DailyV1GenesisComposition composition,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic) =>
        TryPrepareInitialGenesisCore(
            definitions, null, diagnosticLogger, stageCompleted,
            out composition, out candidate, out diagnostic);

    /// <summary>
    /// Deterministic identity-injection seam for Core tests. Production hosts
    /// use the overload without an allocator so identity authority remains in
    /// Core.
    /// </summary>
    internal bool TryPrepareInitialGenesis(
        DailyV1EffectiveDefinitions definitions,
        Func<WorldId> allocateWorldId,
        Action<string> stageCompleted,
        out DailyV1GenesisComposition composition,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic) =>
        TryPrepareInitialGenesisCore(
            definitions, allocateWorldId, null, stageCompleted,
            out composition, out candidate, out diagnostic);

    private bool TryPrepareInitialGenesisCore(
        DailyV1EffectiveDefinitions definitions,
        Func<WorldId> allocateWorldId,
        SimulationLogger diagnosticLogger,
        Action<string> stageCompleted,
        out DailyV1GenesisComposition composition,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic)
    {
        composition = null;
        candidate = null;
        diagnostic = null;
        if (definitions == null)
        {
            diagnostic = "The selected Daily-v1 effective definitions are required.";
            return false;
        }

        SimulationRuntimeAdmissionContext admissionContext;
        lock (activeSessionGate.SyncRoot)
        {
            if (initialGenesisPreparationAttempted
                || initialContinuationLoadBlocksGenesis
                || preparedInitialGenesis != null
                || preparedInitialSession != null
                || activeSessionGate.Current != null
                || activeSessionGate.ActiveOperationCount != 0)
            {
                diagnostic = "The Core application cannot capture a second bootstrap admission context or attempt another initial genesis session.";
                return false;
            }

            admissionContext = bootstrapAdmissionContext;
            if (admissionContext == null)
            {
                admissionContext = new SimulationRuntimeAdmissionContext(
                    SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                    Thread.CurrentThread,
                    Thread.CurrentThread.ManagedThreadId);
                bootstrapAdmissionContext = admissionContext;
            }

            initialGenesisPreparationAttempted = true;
        }

        if (!DailyV1GenesisComposer.TryComposeSelectedOwnersWithHostLogger(
                definitions,
                admissionContext,
                allocateWorldId,
                stageCompleted,
                diagnosticLogger,
                out composition,
                out diagnostic))
        {
            composition = null;
            return false;
        }

        if (!DailyV1ActiveRuntimeSession.TryCreate(
                composition,
                composition.SimulationTime.AbsoluteDay,
                out candidate,
                out diagnostic))
        {
            composition = null;
            candidate = null;
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
            if (activeSessionGate.Current != null
                || preparedInitialGenesis != null
                || preparedInitialSession != null)
            {
                composition = null;
                candidate = null;
                diagnostic = "The Core application changed while its initial genesis candidate was being composed.";
                return false;
            }

            preparedInitialGenesis = composition;
            preparedInitialSession = candidate;
        }

        return true;
    }

    /// <summary>
    /// Creates the Core session binding for a detached, already admitted
    /// restore candidate. The application checks that the restored execution
    /// state descends from this application's bootstrap and, for replacement,
    /// that the exact current session is still reserved by its owner thread.
    /// The Unity restore projection supplies no session authority.
    /// </summary>
    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions expectedDefinitions,
        long lastEconomySnapshotDay,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic) =>
        TryCreateRestoredActiveRuntimeSession(
            executionBoundaryState, expectedDefinitions, lastEconomySnapshotDay,
            null, null, replacementOperation, out candidate, out diagnostic);

    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions expectedDefinitions,
        long lastEconomySnapshotDay,
        P12CP9GenesisManifestCoreSnapshot p9Manifest,
        SpatialAuthorityStore spatialAuthority,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic)
    {
        candidate = null;
        diagnostic = null;
        if (executionBoundaryState == null
            || executionBoundaryState.SelectedRuntimeIdAllocator == null
            || executionBoundaryState.SimulationTime == null
            || executionBoundaryState.SelectedRecordSequence == null
            || executionBoundaryState.SelectedDecisionStore == null)
        {
            diagnostic = "The restored Core session requires its exact execution identity and decision-store roots.";
            return false;
        }

        DailyV1DomainEventAuthority eventAuthority = DailyV1DomainEventAuthority.Create(
            executionBoundaryState.SelectedRuntimeIdAllocator,
            executionBoundaryState.SimulationTime,
            executionBoundaryState.SelectedRecordSequence, executionBoundaryState.SelectedDecisionStore);
        return TryCreateRestoredActiveRuntimeSession(
            executionBoundaryState, expectedDefinitions, lastEconomySnapshotDay,
            p9Manifest, spatialAuthority, eventAuthority, replacementOperation,
            out candidate, out diagnostic);
    }

    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions expectedDefinitions,
        long lastEconomySnapshotDay,
        P12CP9GenesisManifestCoreSnapshot p9Manifest,
        SpatialAuthorityStore spatialAuthority,
        DailyV1DomainEventAuthority eventAuthority,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic)
    {
        candidate = null;
        diagnostic = null;
        if (executionBoundaryState == null || expectedDefinitions == null)
        {
            diagnostic = "The restored Core session requires its exact execution boundary and effective definitions.";
            return false;
        }

        DailyV1SelectedRuntimeOwnerGraph ownerGraph = executionBoundaryState.SelectedRuntimeOwnerGraph;
        if (!executionBoundaryState.HasAdmittedSelectedDailyExecution
            || ownerGraph == null
            || !ReferenceEquals(ownerGraph.Definitions, expectedDefinitions)
            || eventAuthority == null
            || !eventAuthority.IsFor(
                executionBoundaryState.SelectedRuntimeIdAllocator,
                executionBoundaryState.SimulationTime,
                executionBoundaryState.SelectedRecordSequence, executionBoundaryState.SelectedDecisionStore))
        {
            diagnostic = "The restored Core execution boundary is not admitted for the exact selected definitions.";
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
            SimulationRuntimeAdmissionContext admissionContext = bootstrapAdmissionContext;
            DailyV1ActiveRuntimeSession current = activeSessionGate.Current;
            if (admissionContext == null
                || !admissionContext.IsOwnedByCurrentThread()
                || !IsBootstrapOrRestoredDescendant(executionBoundaryState.AdmissionContext)
                || !executionBoundaryState.AdmissionContext.IsOwnedByCurrentThread())
            {
                diagnostic = "The restored Core session does not descend from this application's current owner-thread bootstrap.";
                return false;
            }

            if (current == null)
            {
                if (replacementOperation != null
                    || activeSessionGate.ActiveOperationCount != 0
                    || initialGenesisPreparationAttempted
                    || preparedInitialGenesis != null
                    || preparedInitialSession != null)
                {
                    diagnostic = "Initial restoration requires an unused Core application with no active session or operation.";
                    return false;
                }
            }
            else if (replacementOperation == null
                || !ReferenceEquals(replacementOperation.ExpectedSession, current)
                || !replacementOperation.AuthorizesSessionReplacementCapture(
                    current.ExecutionBoundaryState)
                || activeSessionGate.ActiveOperationCount != 0)
            {
                diagnostic = "Restoration replacement requires the exact current Core session's owner-thread reservation.";
                return false;
            }

            bool sessionCreated = p9Manifest == null && spatialAuthority == null
                ? DailyV1ActiveRuntimeSession.TryCreate(
                    executionBoundaryState, expectedDefinitions, lastEconomySnapshotDay,
                    eventAuthority, out candidate, out diagnostic)
                : DailyV1ActiveRuntimeSession.TryCreateRestored(
                    executionBoundaryState, expectedDefinitions, lastEconomySnapshotDay,
                    p9Manifest, spatialAuthority, eventAuthority,
                    out candidate, out diagnostic);
            if (!sessionCreated)
            {
                candidate = null;
                return false;
            }

            if (current != null && !current.IsProfileCompatibleWith(candidate))
            {
                candidate = null;
                diagnostic = "The restored Core session is incompatible with the published profile.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Composes the selected Core owner graph, restored execution boundary,
    /// and unpublished application session in the owning application. A
    /// persistence host supplies already-staged Core owners and P12 roots; it
    /// cannot assemble or publish a competing Core session.
    /// </summary>
    internal bool TryCreateRestoredActiveRuntimeSession(
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
        long lastEconomySnapshotDay,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out SimulationCalendar calendar,
        out string diagnostic) =>
        TryCreateRestoredActiveRuntimeSession(
            definitions, null, effectiveConfiguration, calendarDefinition,
            simulationTime, admissionContext, worldId, cityOwners, npcOwners,
            spatialNetwork, justiceRecords, travelParties, runtimeIdentityIndex,
            personStore, directives, expeditions, null, runtimeIdAllocator,
            randomSource, recordSequence, decisionStore, actorChoiceStore,
            lastEconomySnapshotDay, replacementOperation, out candidate,
            out calendar, out diagnostic);

    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1EffectiveDefinitions definitions,
        P12CP9GenesisManifestCoreSnapshot p9Manifest,
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
        SpatialAuthorityStore spatialAuthority,
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore,
        long lastEconomySnapshotDay,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out SimulationCalendar calendar,
        out string diagnostic)
    {
        return TryCreateRestoredActiveRuntimeSession(
            definitions, p9Manifest, effectiveConfiguration, calendarDefinition,
            simulationTime, admissionContext, worldId, cityOwners, npcOwners,
            spatialNetwork, justiceRecords, travelParties, runtimeIdentityIndex,
            personStore, null, directives, expeditions, spatialAuthority,
            runtimeIdAllocator, randomSource, recordSequence, decisionStore,
            actorChoiceStore, lastEconomySnapshotDay, replacementOperation,
            out candidate, out calendar, out diagnostic);
    }

    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1EffectiveDefinitions definitions,
        P12CP9GenesisManifestCoreSnapshot p9Manifest,
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
        GenealogyStore genealogyStore,
        ScheduledDirectiveStore directives,
        ExpeditionStore expeditions,
        SpatialAuthorityStore spatialAuthority,
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore,
        long lastEconomySnapshotDay,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out SimulationCalendar calendar,
        out string diagnostic)
    {
        candidate = null;
        calendar = null;
        diagnostic = null;
        if (runtimeIdAllocator == null || simulationTime == null
            || recordSequence == null || decisionStore == null)
        {
            diagnostic = "Restored Core session composition requires its exact runtime identity and decision-store roots.";
            return false;
        }

        DailyV1DomainEventAuthority eventAuthority = DailyV1DomainEventAuthority.Create(
            runtimeIdAllocator, simulationTime, recordSequence, decisionStore);
        return TryCreateRestoredActiveRuntimeSession(
            definitions, p9Manifest, effectiveConfiguration, calendarDefinition,
            simulationTime, admissionContext, worldId, cityOwners, npcOwners, spatialNetwork,
            justiceRecords, travelParties, runtimeIdentityIndex, personStore, genealogyStore, directives,
            expeditions, spatialAuthority, eventAuthority, runtimeIdAllocator, randomSource,
            recordSequence, decisionStore, actorChoiceStore, lastEconomySnapshotDay,
            replacementOperation, out candidate, out calendar, out diagnostic);
    }

    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1EffectiveDefinitions definitions,
        P12CP9GenesisManifestCoreSnapshot p9Manifest,
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
        GenealogyStore genealogyStore,
        ScheduledDirectiveStore directives,
        ExpeditionStore expeditions,
        SpatialAuthorityStore spatialAuthority,
        DailyV1DomainEventAuthority eventAuthority,
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore,
        long lastEconomySnapshotDay,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out SimulationCalendar calendar,
        out string diagnostic)
    {
        candidate = null;
        calendar = null;
        diagnostic = null;
        if (definitions == null
            || !ReferenceEquals(definitions.EffectiveConfiguration, effectiveConfiguration)
            || eventAuthority == null
            || !eventAuthority.IsFor(runtimeIdAllocator, simulationTime, recordSequence, decisionStore))
        {
            diagnostic = "Restored Core session composition requires the exact selected definitions and configuration.";
            return false;
        }

        if (!DailyV1RestoredExecutionBoundaryComposer.TryCreate(
                definitions,
                effectiveConfiguration,
                calendarDefinition,
                simulationTime,
                admissionContext,
                worldId,
                cityOwners,
                npcOwners,
                spatialNetwork,
                justiceRecords,
                travelParties,
                runtimeIdentityIndex,
                personStore,
                genealogyStore,
                directives,
                expeditions,
                runtimeIdAllocator,
                randomSource,
                recordSequence,
                decisionStore,
                actorChoiceStore,
                out DailyV1ExecutionBoundaryState executionBoundaryState,
                out SimulationCalendar candidateCalendar,
                out diagnostic))
            return false;

        if (!TryCreateRestoredActiveRuntimeSession(
                executionBoundaryState,
                definitions,
                lastEconomySnapshotDay,
                p9Manifest,
                spatialAuthority,
                eventAuthority,
                replacementOperation,
                out DailyV1ActiveRuntimeSession candidateSession,
                out diagnostic))
            return false;

        candidate = candidateSession;
        calendar = candidateCalendar;
        return true;
    }

    internal object ActiveSessionSyncRoot => activeSessionGate.SyncRoot;
    internal DailyV1ActiveRuntimeSession ActiveSession => activeSessionGate.Current;
    internal int ActiveOperationCount => activeSessionGate.ActiveOperationCount;

    internal bool TryBeginOperation(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1OperationScopeKind scopeKind,
        out DailyV1ActiveRuntimeSession session) =>
        activeSessionGate.TryBeginOperation(expectedSession, scopeKind, out session);

    /// <summary>
    /// Starts the single Core-owned P12-G save operation and captures the
    /// exact current completed-boundary witness before detached staging. The
    /// returned operation keeps the save scope registered until disposal and
    /// supports repeated source recapture between P12 stages.
    /// </summary>
    internal bool TryBeginContinuationSaveOperation(
        DailyV1ActiveRuntimeSession expectedSession,
        out DailyV1P12GContinuationSaveOperation operation,
        out DailyCaptureEligibilityFailure failure)
    {
        operation = null;
        failure = DailyCaptureEligibilityFailure.OperationInProgress;
        if (expectedSession == null
            || !ReferenceEquals(activeSessionGate.Current, expectedSession)
            || !expectedSession.ExecutionBoundaryState.IsHealthyDailyOwnerThreadBoundary()
            || expectedSession.RuntimeAdvanceBoundary == null
            || !ReferenceEquals(expectedSession.RuntimeAdvanceBoundary.ExecutionState,
                expectedSession.ExecutionBoundaryState))
            return false;

        if (!activeSessionGate.TryBeginOperation(
                expectedSession, DailyV1OperationScopeKind.ContinuationSave,
                out DailyV1ActiveRuntimeSession admittedSession)
            || !ReferenceEquals(admittedSession, expectedSession))
            return false;

        DailyV1P12GContinuationSaveOperation candidateOperation = null;
        bool witnessCaptured;
        try
        {
            witnessCaptured = TryCaptureContinuationSourceWitness(expectedSession,
                out DailyV1P12GSourceBoundaryWitness witness, out failure);
            if (witnessCaptured)
            {
                candidateOperation = new DailyV1P12GContinuationSaveOperation(
                    this, expectedSession, witness);

                // Bind the P12-D City image source to the exact Core owner
                // graph while the continuation-save lease is active. The
                // Unity projection may later stage a detached candidate, but
                // it cannot choose a different City state for this image.
                if (!DailyV1P12GCoreBoundCityOwnerSources.TryCapture(
                        candidateOperation,
                        out DailyV1P12GCoreBoundCityOwnerSources cityOwnerSources,
                        out DailyV1P12GCoreCitySourceCaptureFailure cityCaptureFailure,
                        out _)
                    || !candidateOperation.TryBindCoreCityOwnerSources(cityOwnerSources))
                {
                    witnessCaptured = false;
                    failure = cityCaptureFailure == DailyV1P12GCoreCitySourceCaptureFailure.StaleSourceBoundary
                        ? DailyCaptureEligibilityFailure.StaleToken
                        : DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                }

                if (witnessCaptured
                    && (!DailyV1P12GCoreBoundDemographicOwnerSources.TryCapture(
                            candidateOperation,
                            out DailyV1P12GCoreBoundDemographicOwnerSources demographicOwnerSources,
                            out DailyV1P12GCoreBoundOwnerSourceCaptureFailure demographicCaptureFailure,
                            out _)
                        || !candidateOperation.TryBindCoreDemographicOwnerSources(demographicOwnerSources)))
                {
                    witnessCaptured = false;
                    failure = demographicCaptureFailure ==
                        DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary
                        ? DailyCaptureEligibilityFailure.StaleToken
                        : DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                }
            }
        }
        catch (Exception captureException)
        {
            try
            {
                if (candidateOperation != null && candidateOperation.IsActive)
                    candidateOperation.Dispose();
                else
                    activeSessionGate.EndOperation(DailyV1OperationScopeKind.ContinuationSave);
            }
            catch (Exception cleanupException)
            {
                // Do not replace the original capture/construction failure if
                // cleanup also fails. Keep the cleanup diagnostic attached.
                try
                {
                    captureException.Data["ContinuationSaveScopeCleanupException"] = cleanupException;
                }
                catch
                {
                    // Exception data is diagnostic only; never mask the original failure.
                }
            }

            ExceptionDispatchInfo.Capture(captureException).Throw();
            throw;
        }

        if (!witnessCaptured)
        {
            if (candidateOperation != null && candidateOperation.IsActive)
                candidateOperation.Dispose();
            else
                activeSessionGate.EndOperation(DailyV1OperationScopeKind.ContinuationSave);
            return false;
        }

        operation = candidateOperation;
        return true;
    }

    internal bool TryRevalidateContinuationSaveOperation(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1P12GSourceBoundaryWitness expectedWitness,
        out DailyCaptureEligibilityFailure failure)
    {
        failure = DailyCaptureEligibilityFailure.OperationInProgress;
        if (expectedSession == null || expectedWitness == null
            || !ReferenceEquals(activeSessionGate.Current, expectedSession)
            || !expectedSession.AdmissionContext.IsOwnedByCurrentThread())
            return false;

        if (!TryCaptureContinuationSourceWitness(expectedSession,
                out DailyV1P12GSourceBoundaryWitness currentWitness, out failure))
            return false;

        if (!expectedWitness.Matches(currentWitness))
        {
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Persists a P12-G image only while the exact Core continuation-save
    /// operation that authorized its capture remains active. This is the
    /// Core-owned census/encode/atomic-store half of the Unity migration
    /// route; image capture itself remains in the P12-G owner coordinator
    /// until its selected owner staging closure is moved into Core.
    /// </summary>
    internal bool TryPersistContinuationImage(
        DailyV1P12GContinuationSaveOperation operation,
        P12AContinuationImage image,
        string persistentDataRoot,
        IAtomicFileSlotOperations fileOperations,
        out P12AResult result)
    {
        result = new P12AResult(P12AResultCode.CaptureRejected);
        if (operation == null || !operation.IsActive || !operation.IsOwnedBy(this)
            || !ReferenceEquals(activeSessionGate.Current, operation.Session))
            return false;

        if (!operation.Session.AdmissionContext.IsOwnedByCurrentThread())
        {
            result = new P12AResult(P12AResultCode.WrongThread);
            return false;
        }

        if (image == null || image.Header == null || !image.Header.IsValid
            || string.IsNullOrWhiteSpace(persistentDataRoot) || fileOperations == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!operation.TryRevalidate(out _))
            return false;

        if (!P12ADailyV1Census.TryCreateForImage(
                image, out P12ADailyV1Census census, out result)
            || !P12AContinuationCodec.TryEncode(
                image, census.Manifest, out byte[] bytes, out result))
            return false;

        // Encoding traverses every owner section. Confirm that the same
        // completed boundary still authorizes the bytes immediately before
        // the externally supplied storage operations begin.
        if (!operation.TryRevalidate(out _))
        {
            result = new P12AResult(P12AResultCode.CaptureRejected);
            return false;
        }

        if (image.Header.Compatibility != null && image.Header.CompatibilityV2 == null)
            return P12AContinuationStore.TrySave(bytes, census.Manifest,
                image.Header.Compatibility, persistentDataRoot, fileOperations, out result);

        if (image.Header.Compatibility == null && image.Header.CompatibilityV2 != null)
            return P12AContinuationStore.TrySaveDotNetV2(bytes, census.Manifest,
                image.Header.CompatibilityV2, persistentDataRoot, fileOperations, out result);

        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        return false;
    }

    internal void EndContinuationSaveOperation()
    {
        activeSessionGate.EndOperation(DailyV1OperationScopeKind.ContinuationSave);
    }

    private static bool TryCaptureContinuationSourceWitness(
        DailyV1ActiveRuntimeSession session,
        out DailyV1P12GSourceBoundaryWitness witness,
        out DailyCaptureEligibilityFailure failure)
    {
        witness = null;
        failure = DailyCaptureEligibilityFailure.OperationInProgress;
        if (session == null || session.RuntimeAdvanceBoundary == null
            || !session.ExecutionBoundaryState.TryCaptureContinuationSaveBoundaryEvidence(
                session.RuntimeAdvanceBoundary,
                out DailyCaptureEligibilityToken token,
                out IReadOnlyList<OwnerSectionCensusSnapshot> currentSections,
                out long mutationEpoch,
                out failure))
            return false;

        if (!DailyV1P12GSourceBoundaryWitness.TryCreate(
                token, currentSections, mutationEpoch, out witness))
        {
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }

        failure = DailyCaptureEligibilityFailure.None;
        return true;
    }

    internal void EndOperation(DailyV1OperationScopeKind scopeKind) =>
        activeSessionGate.EndOperation(scopeKind);

    internal bool TryPublishInitial(DailyV1ActiveRuntimeSession candidate)
    {
        // A Core session is not active until its exact execution boundary is
        // bound. Publishing an unbound genesis value creates a session that
        // cannot advance and leaves the application with no valid runtime.
        if (candidate?.RuntimeAdvanceBoundary == null) return false;
        lock (activeSessionGate.SyncRoot)
        {
            if (preparedInitialSession != null)
            {
                if (!ReferenceEquals(preparedInitialSession, candidate)
                    || preparedInitialGenesis == null
                    || !ReferenceEquals(preparedInitialGenesis.ExecutionBoundaryState,
                        candidate.ExecutionBoundaryState)
                    || !ReferenceEquals(preparedInitialGenesis.Definitions,
                        candidate.Definitions))
                    return false;

                if (!IsBootstrapOrRestoredDescendant(candidate.AdmissionContext)
                    || !activeSessionGate.TryPublishInitial(candidate))
                    return false;

                preparedInitialGenesis = null;
                preparedInitialSession = null;
                return true;
            }

            if (initialGenesisPreparationAttempted
                || bootstrapAdmissionContext == null
                || !IsBootstrapOrRestoredDescendant(candidate.AdmissionContext))
                return false;
            return activeSessionGate.TryPublishInitial(candidate);
        }
    }

    internal bool TryBeginExclusiveOperation(
        DailyV1ActiveRuntimeSession expectedSession,
        out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation operation) =>
        activeSessionGate.TryBeginExclusiveOperation(expectedSession, out operation);

    /// <summary>
    /// Reads the continuation image for the exact published session while its
    /// replacement reservation remains held. The host chooses only its
    /// explicit storage root, file operations, and current-host descriptor;
    /// Core revalidates the source completed boundary immediately before the
    /// read so a stale reservation cannot authorize detached staging.
    /// </summary>
    internal bool TryLoadContinuationImageForReplacement(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        P12ACompatibilityDescriptor unityV1Identity,
        P12ACompatibilityDescriptorV2 dotNetV2Identity,
        string persistentDataRoot,
        IAtomicFileSlotOperations fileOperations,
        out P12AContinuationImage image,
        out P12AResult result)
    {
        image = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (expectedSession == null || replacementOperation == null
            || ((unityV1Identity == null) == (dotNetV2Identity == null))
            || string.IsNullOrWhiteSpace(persistentDataRoot) || fileOperations == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        DailyV1ExecutionBoundaryState state = expectedSession.ExecutionBoundaryState;
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary = expectedSession.RuntimeAdvanceBoundary;
        if (!replacementOperation.IsOwnedByCurrentThread()
            || !ReferenceEquals(replacementOperation.ExpectedSession, expectedSession)
            || !ReferenceEquals(activeSessionGate.Current, expectedSession)
            || !replacementOperation.AuthorizesSessionReplacementCapture(state)
            || activeSessionGate.ActiveOperationCount != 0
            || boundary == null
            || !ReferenceEquals(boundary.ExecutionState, state)
            || !state.IsHealthyDailyOwnerThreadBoundary()
            || !state.HasValidCompletedDailyBoundaryAdmission(
                boundary, sessionReplacement: true))
            return false;

        if (unityV1Identity != null)
            return P12AContinuationStore.TryLoad(
                null, unityV1Identity, persistentDataRoot, fileOperations,
                out image, out result);

        return P12AContinuationStore.TryLoadDotNetV2(
            null, dotNetV2Identity, persistentDataRoot, fileOperations,
            out image, out result);
    }

    /// <summary>
    /// Inspects the initial continuation slot when the host cannot provide a
    /// truthful compatibility identity. Only a missing slot admits genesis;
    /// a present or inaccessible slot keeps this application fail-closed.
    /// </summary>
    internal bool TryInspectInitialContinuationSlotForStart(
        string persistentDataRoot,
        IAtomicFileSlotOperations fileOperations,
        out bool exists,
        out P12AResult result)
    {
        exists = false;
        result = new P12AResult(P12AResultCode.Busy);
        if (string.IsNullOrWhiteSpace(persistentDataRoot) || fileOperations == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            lock (activeSessionGate.SyncRoot)
            {
                if (!initialContinuationLoadAttempted
                    && !initialGenesisPreparationAttempted
                    && preparedInitialGenesis == null
                    && preparedInitialSession == null
                    && activeSessionGate.Current == null
                    && activeSessionGate.ActiveOperationCount == 0)
                {
                    initialContinuationLoadAttempted = true;
                    initialContinuationLoadBlocksGenesis = true;
                }
            }
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
            if (initialContinuationLoadAttempted
                || initialGenesisPreparationAttempted
                || preparedInitialGenesis != null
                || preparedInitialSession != null
                || activeSessionGate.Current != null
                || activeSessionGate.ActiveOperationCount != 0)
                return false;

            if (bootstrapAdmissionContext == null)
            {
                bootstrapAdmissionContext = new SimulationRuntimeAdmissionContext(
                    SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                    Thread.CurrentThread,
                    Thread.CurrentThread.ManagedThreadId);
            }

            if (!bootstrapAdmissionContext.IsOwnedByCurrentThread())
            {
                initialContinuationLoadBlocksGenesis = true;
                result = new P12AResult(P12AResultCode.WrongThread);
                return false;
            }

            initialContinuationLoadAttempted = true;
            initialContinuationLoadBlocksGenesis = true;
        }

        bool inspected = P12AContinuationStore.TryHasSlot(
            persistentDataRoot, fileOperations, out exists, out result);
        lock (activeSessionGate.SyncRoot)
        {
            initialContinuationLoadBlocksGenesis = !inspected
                || exists
                || result == null
                || result.Code != P12AResultCode.NoSave;
        }
        return inspected;
    }
    /// <summary>
    /// Admits the initial continuation slot before genesis. A missing slot is
    /// the only outcome that permits the caller to continue into genesis;
    /// every present-but-invalid, incompatible, or inaccessible slot blocks
    /// genesis for this application instance. The selected owner-thread context
    /// is captured by this Core application and no runtime identity is allocated
    /// by this read.
    /// </summary>
    internal bool TryLoadInitialContinuationImageForStart(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ApplicationHost host,
        out P12AContinuationImage image,
        out P12AResult result)
    {
        image = null;
        result = new P12AResult(P12AResultCode.Busy);
        bool invalidHost = host == null
            || string.IsNullOrWhiteSpace(host.PersistentDataRoot)
            || host.FileOperations == null
            || (host.UnityV1Identity == null && definitions == null);
        if (invalidHost)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            lock (activeSessionGate.SyncRoot)
            {
                // Invalid input is still a failed initial-load attempt. Only a
                // completed NoSave result may let this application fall through
                // to genesis.
                if (!initialContinuationLoadAttempted
                    && !initialGenesisPreparationAttempted
                    && preparedInitialGenesis == null
                    && preparedInitialSession == null
                    && activeSessionGate.Current == null
                    && activeSessionGate.ActiveOperationCount == 0)
                {
                    initialContinuationLoadAttempted = true;
                    initialContinuationLoadBlocksGenesis = true;
                }
            }
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
            if (initialContinuationLoadAttempted
                || initialGenesisPreparationAttempted
                || preparedInitialGenesis != null
                || preparedInitialSession != null
                || activeSessionGate.Current != null
                || activeSessionGate.ActiveOperationCount != 0)
                return false;

            if (bootstrapAdmissionContext == null)
            {
                bootstrapAdmissionContext = new SimulationRuntimeAdmissionContext(
                    SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                    Thread.CurrentThread,
                    Thread.CurrentThread.ManagedThreadId);
            }

            if (!bootstrapAdmissionContext.IsOwnedByCurrentThread())
            {
                result = new P12AResult(P12AResultCode.WrongThread);
                return false;
            }

            initialContinuationLoadAttempted = true;
        }

        bool loaded;
        if (host.UnityV1Identity != null)
        {
            loaded = P12AContinuationStore.TryLoad(
                null, host.UnityV1Identity, host.PersistentDataRoot,
                host.FileOperations, out image, out result);
        }
        else if (!DailyV1CompatibilityDescriptorFactory.TryCreateDotNetV2(
                definitions, host.DotNetIdentity,
                out P12ACompatibilityDescriptorV2 dotNetIdentity, out _))
        {
            result = new P12AResult(P12AResultCode.IncompatibleContentOrConfiguration);
            loaded = false;
        }
        else
        {
            loaded = P12AContinuationStore.TryLoadDotNetV2(
                null, dotNetIdentity, host.PersistentDataRoot,
                host.FileOperations, out image, out result);
        }

        lock (activeSessionGate.SyncRoot)
        {
            // No-save is the only safe fall-through to genesis. A loaded image
            // must be restored, and any storage/compatibility failure must fail
            // closed rather than creating a replacement world.
            initialContinuationLoadBlocksGenesis =
                loaded || result == null || result.Code != P12AResultCode.NoSave;
        }

        if (!loaded && (result == null || result.Code != P12AResultCode.NoSave))
            image = null;
        return loaded;
    }

    /// <summary>
    /// Admits a persisted owner image's host identity and complete Daily-v1
    /// census under the exact bootstrap context owned by this application.
    /// The typed decoder remains a separate Core step for callers that need to
    /// distinguish census admission from value decoding.
    /// </summary>
    internal bool TryAdmitRestoredContinuationImage(
        P12AContinuationImage image,
        P12ACompatibilityDescriptor unityV1Identity,
        P12ACompatibilityDescriptorV2 dotNetV2Identity,
        SimulationRuntimeAdmissionContext admissionContext,
        out P12ADailyV1Census census,
        out P12AResult result)
    {
        census = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (image == null || ((unityV1Identity == null) == (dotNetV2Identity == null))
            || admissionContext == null)
            return false;

        lock (activeSessionGate.SyncRoot)
        {
            if (!ReferenceEquals(bootstrapAdmissionContext, admissionContext)
                || admissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || admissionContext.IsRestoredContinuation
                || !admissionContext.IsOwnedByCurrentThread())
            {
                result = new P12AResult(P12AResultCode.WrongThread);
                return false;
            }

            if (image.Header == null || !image.Header.IsValid)
            {
                result = new P12AResult(P12AResultCode.CorruptOrTruncated);
                return false;
            }

            if (unityV1Identity != null)
            {
                if (image.Header.Compatibility == null
                    || !unityV1Identity.Matches(image.Header.Compatibility.CanonicalBytes))
                {
                    result = new P12AResult(P12AResultCode.IncompatibleContentOrConfiguration);
                    return false;
                }
            }
            else if (image.Header.CompatibilityV2 == null
                || P12ACompatibilityAdmission.AdmitDotNetV2(
                    image.Header.CompatibilityV2.CanonicalBytes,
                    dotNetV2Identity, out _) != P12AResultCode.Decoded)
            {
                result = new P12AResult(P12AResultCode.IncompatibleBuildOrRuntime);
                return false;
            }

            if (!P12ADailyV1Census.TryCreateForImage(image, out census, out result))
            {
                census = null;
                return false;
            }
        }

        result = new P12AResult(P12AResultCode.Decoded);
        return true;
    }

    /// <summary>
    /// Admits and decodes one persisted owner image within the Core
    /// application's bootstrap identity. The compatibility adapter may stage
    /// the returned detached values, but cannot bypass Core's host/census gate
    /// or choose a different image decoder.
    /// </summary>
    internal bool TryAdmitAndDecodeRestoredContinuationImage(
        P12AContinuationImage image,
        P12ACompatibilityDescriptor unityV1Identity,
        P12ACompatibilityDescriptorV2 dotNetV2Identity,
        SimulationRuntimeAdmissionContext admissionContext,
        out bool admissionPassed,
        out P12ADailyV1Census census,
        out P12ADailyV1ImageOwnerSnapshots snapshots,
        out P12AResult result)
    {
        admissionPassed = false;
        census = null;
        snapshots = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (!TryAdmitRestoredContinuationImage(
                image, unityV1Identity, dotNetV2Identity, admissionContext,
                out census, out result))
            return false;

        admissionPassed = true;
        if (!P12ADailyV1ImageOwnerDecoder.TryDecode(image, out snapshots, out result)
            || snapshots == null)
        {
            snapshots = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Owns the first detached restore transaction boundary: the application
    /// verifies its exact initial/replacement staging authority, admits the
    /// persisted image against its captured bootstrap identity, decodes the
    /// typed owner values, and privately stages the Core P12-C roots before
    /// returning any restore result to a host adapter. The returned roots are
    /// unpublished and do not replace or mutate the current session.
    /// </summary>
    internal bool TryAdmitDecodeAndStageRestoredContinuationImage(
        P12AContinuationImage image,
        P12ACompatibilityDescriptor unityV1Identity,
        P12ACompatibilityDescriptorV2 dotNetV2Identity,
        SimulationRuntimeAdmissionContext admissionContext,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out bool admissionPassed,
        out P12ADailyV1Census census,
        out P12ADailyV1ImageOwnerSnapshots snapshots,
        out P12CStagedCoreContinuationRoot stagedRoots,
        out P12AResult result,
        Action imageDecoded = null,
        Action<string> stageCompleted = null)
    {
        admissionPassed = false;
        census = null;
        snapshots = null;
        stagedRoots = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (!TryAdmitAndDecodeRestoredContinuationImage(
                image,
                unityV1Identity,
                dotNetV2Identity,
                admissionContext,
                out admissionPassed,
                out census,
                out snapshots,
                out result))
            return false;

        imageDecoded?.Invoke();

        if (!TryStageRestoredContinuationRootsFromSnapshots(
                snapshots.C,
                expectedSession,
                replacementOperation,
                out stagedRoots,
                out result,
                stageCompleted)
            || stagedRoots == null)
        {
            stagedRoots = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Keeps the restored image-to-target owner-vector decision inside the
    /// application that owns the exact initial or replacement staging
    /// reservation. The Unity runtime supplies only its captured census.
    /// </summary>
    internal bool TryValidateRestoredImageOwnerVector(
        P12ADailyV1Census imageCensus,
        IReadOnlyList<OwnerSectionCensusSnapshot> targetOwnerSections,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out P12GDailyV1OwnerVectorFailure failure,
        out string diagnostic)
    {
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
        {
            failure = P12GDailyV1OwnerVectorFailure.OwnerVectorChanged;
            diagnostic = "The application no longer holds the exact restored-continuation staging authority.";
            return false;
        }

        return DailyV1RestoredOwnerVectorContract.TryValidateRestoredImageCandidate(
            imageCensus, targetOwnerSections, out failure, out diagnostic);
    }

    /// <summary>
    /// Finalizes the private restored Daily-v1 boundary while this application
    /// owns the exact initial or replacement staging authority. Core binds and
    /// revalidates the selected execution boundary, publishes the restored
    /// continuation token, and marks factual-read state before the host can
    /// publish its compatibility projection.
    /// </summary>
    internal bool TryFinalizeRestoredCandidateBoundary(
        DailyV1ActiveRuntimeSession candidateSession,
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts,
        WorldId expectedWorldId,
        long preservedAbsoluteDay,
        long preservedCompletedCoreSequence,
        IReadOnlyList<OwnerSectionCensusSnapshot> validatedTargetSections,
        long validatedMutationEpoch,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        Action<DailyV1RestoredBoundaryFinalizationStage> stageObserver,
        out IReadOnlyList<OwnerSectionCensusSnapshot> recapturedOwnerSections,
        out long recapturedMutationEpoch,
        out DailyV1RestoredBoundaryFinalizationFailure failure,
        out string diagnostic)
    {
        recapturedOwnerSections = null;
        recapturedMutationEpoch = 0L;
        failure = DailyV1RestoredBoundaryFinalizationFailure.None;
        diagnostic = null;

        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.StagingAuthorityUnavailable;
            diagnostic = "The application no longer holds the exact restored-continuation staging authority.";
            return false;
        }

        if (candidateSession == null || hostPorts == null
            || !ReferenceEquals(candidateSession.ExecutionBoundaryState, hostPorts.ExecutionState)
            || !IsBootstrapOrRestoredDescendant(candidateSession.AdmissionContext)
            || !TryBindRuntimeAdvanceBoundary(candidateSession, hostPorts))
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.CandidateHostBoundaryUnavailable;
            diagnostic = "The private restored candidate could not bind its exact Core application and host boundary.";
            return false;
        }

        if (candidateSession.RuntimeAdvanceBoundary == null
            || !ReferenceEquals(candidateSession.RuntimeAdvanceBoundary.ExecutionState,
                candidateSession.ExecutionBoundaryState))
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.CandidateHostBoundaryUnavailable;
            diagnostic = "The private restored candidate has no exact Core runtime evidence boundary.";
            return false;
        }

        return candidateSession.ExecutionBoundaryState.TryFinalizeRestoredDailyBoundary(
            candidateSession.RuntimeAdvanceBoundary,
            hostPorts,
            expectedWorldId,
            preservedAbsoluteDay,
            preservedCompletedCoreSequence,
            validatedTargetSections,
            validatedMutationEpoch,
            stageObserver,
            out recapturedOwnerSections,
            out recapturedMutationEpoch,
            out failure,
            out diagnostic);
    }
    /// <summary>
    /// Stages the detached P12-C roots for an image through this application.
    /// Replacement staging is bound to the exact published session and its
    /// owner-thread reservation; initial restoration is accepted only by an
    /// unused application on its captured bootstrap thread. The host receives
    /// only the staged Core root and may create a Unity compatibility
    /// projection, but does not choose or reconstruct the authoritative roots.
    /// </summary>
    internal bool TryStageRestoredContinuationRoots(
        P12AContinuationImage image,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out P12CStagedCoreContinuationRoot stagedRoots,
        out P12AResult result,
        Action<string> stageCompleted = null)
    {
        stagedRoots = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (image == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!P12ACausalRootImageCodec.TryDecode(
                image, out P12ACausalRootImageSnapshots snapshots, out result)
            || snapshots == null)
            return false;

        return TryStageRestoredContinuationRootsFromSnapshots(
            snapshots,
            expectedSession,
            replacementOperation,
            out stagedRoots,
            out result,
            stageCompleted);
    }

    /// <summary>
    /// Stages the Core P12-C roots from the immutable C values already
    /// decoded as part of the complete typed image. This avoids re-decoding
    /// image sections and keeps staging under the exact application
    /// reservation.
    /// </summary>
    internal bool TryStageRestoredContinuationRootsFromSnapshots(
        P12ACausalRootImageSnapshots snapshots,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out P12CStagedCoreContinuationRoot stagedRoots,
        out P12AResult result,
        Action<string> stageCompleted = null)
    {
        stagedRoots = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (snapshots == null || snapshots.WorldIdentity == null
            || snapshots.Allocator == null || snapshots.RecordSequence == null
            || snapshots.SpatialAuthority == null || snapshots.P9Manifest == null
            || snapshots.RandomRoot == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!P12CSelectedCoreRootStager.TryStage(
                snapshots.WorldIdentity,
                snapshots.Allocator,
                snapshots.RecordSequence,
                snapshots.SpatialAuthority,
                snapshots.P9Manifest,
                snapshots.RandomRoot,
                isDetached: true,
                stageObserver: stageCompleted,
                out stagedRoots,
                out _)
            || stagedRoots == null
            || !stagedRoots.IsDetached)
        {
            stagedRoots = null;
            result = new P12AResult(
                P12AResultCode.OwnerGraphRejected,
                "p12c.continuation-roots");
            return false;
        }

        result = null;
        return true;
    }

    /// <summary>
    /// Stages the selected Core-owned P12-D City/NPC owner vectors under the
    /// same bootstrap or exact-session replacement authority as the P12-C
    /// roots. Unity supplies detached value rows and the already staged legacy
    /// spatial owner; Core creates and validates the authoritative selected
    /// City/NPC owners before the host builds compatibility projections.
    /// </summary>
    internal bool TryStageRestoredP12DOwners(
        P12CStagedCoreContinuationRoot stagedRoots,
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> cityInputs,
        IReadOnlyList<DailyV1P12DNpcCoreOwnerHydrationInput> npcInputs,
        SpatialNetworkOwner spatialNetwork,
        IEnumerable<string> travelPartyIds,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1P12DSelectedCoreOwnerStage stagedOwners,
        out P12AResult result)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (stagedRoots == null || !stagedRoots.IsDetached || definitions == null
            || cityInputs == null || npcInputs == null || spatialNetwork == null
            || travelPartyIds == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!DailyV1P12DSelectedCoreOwnerStager.TryCreate(
                definitions, cityInputs, npcInputs, spatialNetwork, travelPartyIds,
                out stagedOwners, out _)
            || stagedOwners == null)
        {
            stagedOwners = null;
            result = new P12AResult(
                P12AResultCode.OwnerGraphRejected,
                "p12d.selected-core-owners");
            return false;
        }

        result = null;
        return true;
    }

    /// <summary>
    /// Stages selected P12-D owners directly from detached image rows under the
    /// same bootstrap or exact-session replacement authority as the P12-C roots.
    /// Core owns D/F row pairing, hydration-input conversion, definition binding,
    /// and selected City/NPC owner creation before host projections are built.
    /// </summary>
    internal bool TryStageRestoredP12DOwnersFromImageRows(
        P12CStagedCoreContinuationRoot stagedRoots,
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> cityInputs,
        IReadOnlyList<P12DNpcDRow> npcDRows,
        IReadOnlyList<P12DNpcFRow> npcFRows,
        SpatialNetworkOwner spatialNetwork,
        IEnumerable<string> travelPartyIds,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1P12DSelectedCoreOwnerStage stagedOwners,
        out P12AResult result)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (stagedRoots == null || !stagedRoots.IsDetached || definitions == null
            || cityInputs == null || npcDRows == null || npcFRows == null
            || spatialNetwork == null || travelPartyIds == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!DailyV1P12DSelectedCoreOwnerStager.TryCreateFromRows(
                definitions, cityInputs, npcDRows, npcFRows, spatialNetwork, travelPartyIds,
                out stagedOwners, out _)
            || stagedOwners == null)
        {
            stagedOwners = null;
            result = new P12AResult(
                P12AResultCode.OwnerGraphRejected,
                "p12d.selected-core-owners");
            return false;
        }

        result = null;
        return true;
    }
    /// <summary>
    /// Stages the selected P12-D spatial and City/NPC Core owner graph from
    /// values already decoded from one image. The application validates its
    /// exact initial or replacement reservation before creating any owners;
    /// Unity receives only the unpublished Core owners for projection.
    /// </summary>
    internal bool TryStageRestoredP12DCoreOwnerGraphFromImageSnapshots(
        P12CStagedCoreContinuationRoot stagedRoots,
        P12ADailyV1ImageOwnerSnapshots snapshots,
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1P12DCoreRestoreOwnerStage stagedOwners,
        out P12AResult result)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (stagedRoots == null || !stagedRoots.IsDetached || snapshots == null
            || definitions == null || snapshots.SpatialNetwork == null
            || snapshots.RuntimeIdentities == null || snapshots.Persons == null
            || snapshots.Genealogy == null || snapshots.Cities == null
            || snapshots.NpcDRows == null || snapshots.NpcFRows == null
            || snapshots.TravelParties == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!DailyV1P12DSelectedCoreOwnerStager.TryCreateFromImageValues(
                definitions,
                snapshots.SpatialNetwork,
                snapshots.RuntimeIdentities,
                snapshots.Persons,
                snapshots.Genealogy,
                snapshots.Cities,
                snapshots.NpcDRows,
                snapshots.NpcFRows,
                snapshots.TravelParties,
                out stagedOwners,
                out _)
            || stagedOwners == null)
        {
            stagedOwners = null;
            result = new P12AResult(
                P12AResultCode.OwnerGraphRejected,
                "p12d.selected-core-owners");
            return false;
        }

        result = null;
        return true;
    }
    /// <summary>
    /// Stages the selected Core-owned P12-E institution, property, political,
    /// justice, and military owners under the same bootstrap or exact-session
    /// replacement authority as the P12-C/D roots. Unity supplies only the
    /// already decoded immutable owner snapshots and staged P12-C/D roots;
    /// Core creates the authoritative P12-E owners.
    /// </summary>
    internal bool TryStageRestoredP12EOwners(
        P12CStagedCoreContinuationRoot stagedRoots,
        P12EInstitutionOfficeOwnerSnapshot institutionOffice,
        PropertyEstateOwnerSnapshot propertyEstate,
        P12EFactionOwnerSnapshot faction,
        P12EPoliticalClaimOwnerSnapshot politicalClaims,
        P12EPoliticalSupportOwnerSnapshot politicalSupport,
        P12EPoliticalDecisionOwnerSnapshot politicalDecisions,
        P12EJusticeRecordsOwnerSnapshot justiceRecords,
        P12EMilitaryOwnerSnapshot military,
        PersistentConflictOwnerSnapshot conflicts,
        PersistentWarOwnerSnapshot wars,
        PersistentBattleOwnerSnapshot battles,
        bool localTopologyComposed,
        PersonStore persons,
        IReadOnlyList<DailyV1CityRuntimeOwners> stagedCities,
        IReadOnlyList<DailyV1NpcRuntimeOwners> stagedNpcs,
        long capturedDay,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out P12ECoreOwnerRestoreStage stagedOwners,
        out P12AResult result,
        Action<P12ECoreOwnerRestoreStep> stageObserver = null)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (stagedRoots == null || !stagedRoots.IsDetached
            || persons == null || stagedCities == null || stagedNpcs == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!DailyV1P12ECoreOwnerRestoreStager.TryStage(
                institutionOffice, propertyEstate, faction, politicalClaims,
                politicalSupport, politicalDecisions, justiceRecords, military, conflicts, wars,
                battles, localTopologyComposed,
                persons, stagedRoots.SpatialAuthority, stagedCities, stagedNpcs,
                capturedDay, stageObserver,
                out stagedOwners, out _)
            || stagedOwners == null)
        {
            stagedOwners = null;
            result = new P12AResult(
                P12AResultCode.OwnerGraphRejected,
                "p12e.core-owners");
            return false;
        }

        result = null;
        return true;
    }

    /// <summary>
    /// Stages the selected Core-owned P12-F knowledge, directive, actor-choice,
    /// travel-party, and expedition owners under the same bootstrap or exact
    /// session-replacement authority as the detached P12-C/D/E graph.
    /// </summary>
    internal bool TryStageRestoredP12FOwners(
        P12CStagedCoreContinuationRoot stagedRoots,
        P12FPoliticalKnowledgeOwnerSnapshot politicalKnowledge,
        P12FScheduledDirectiveOwnerSnapshot directives,
        P12FActorChoiceSnapshot actorChoices,
        P12FTravelPartyOwnerSnapshot travelParties,
        P12FExpeditionOwnerSnapshot expeditions,
        PersonStore persons,
        InstitutionStore institutions,
        PoliticalClaimStore politicalClaims,
        FactionStore factions,
        OfficeStore offices,
        PropertyOwnershipStore properties,
        long capturedDay,
        SimulationTime stagedTime,
        DailyV1EffectiveDefinitions selectedDefinitions,
        IReadOnlyList<P12DNpcFRow> detachedNpcRows,
        IReadOnlyList<DailyV1NpcRuntimeOwners> stagedSelectedNpcOwners,
        DailyV1RuntimeIdentityIndex stagedIdentities,
        SpatialNetworkOwner stagedSpatialNetwork,
        ExplorableSiteStore stagedExplorableSites,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1P12FCoreOwnerRestoreStage stagedOwners,
        out P12AResult result,
        Action<DailyV1P12FCoreOwnerRestoreStep> stageObserver = null)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (stagedRoots == null || !stagedRoots.IsDetached
            || politicalKnowledge == null || directives == null || actorChoices == null
            || travelParties == null || expeditions == null || persons == null
            || institutions == null || politicalClaims == null || factions == null
            || offices == null || properties == null || stagedTime == null
            || capturedDay != stagedTime.AbsoluteDay || capturedDay < 0L
            || selectedDefinitions == null || detachedNpcRows == null
            || stagedSelectedNpcOwners == null || stagedIdentities == null
            || stagedSpatialNetwork == null || stagedExplorableSites == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        try
        {
            List<string> stagedNpcRuntimeIds = new List<string>(detachedNpcRows.Count);
            HashSet<string> seenNpcRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (P12DNpcFRow row in detachedNpcRows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.RuntimeId)
                    || !seenNpcRuntimeIds.Add(row.RuntimeId))
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue);
                    return false;
                }
                stagedNpcRuntimeIds.Add(row.RuntimeId);
            }

            if (!politicalKnowledge.TryStageForImage(persons, institutions, politicalClaims,
                    factions, offices, properties, capturedDay,
                    out PoliticalKnowledgeStore stagedPoliticalKnowledge, out _)
                || stagedPoliticalKnowledge == null)
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12f.political-knowledge");
                return false;
            }
            stageObserver?.Invoke(DailyV1P12FCoreOwnerRestoreStep.PoliticalKnowledge);

            if (!directives.TryStageForImage(stagedTime, selectedDefinitions,
                    out ScheduledDirectiveStore stagedDirectives, out _)
                || stagedDirectives == null)
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12f.scheduled-directives");
                return false;
            }
            stageObserver?.Invoke(DailyV1P12FCoreOwnerRestoreStep.ScheduledDirectives);

            if (!P12FActorChoiceSnapshot.TryStage(actorChoices, persons,
                    out ActorChoiceStore stagedActorChoices, out _)
                || stagedActorChoices == null)
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12f.actor-choices");
                return false;
            }
            stageObserver?.Invoke(DailyV1P12FCoreOwnerRestoreStep.ActorChoices);

            if (!DailyV1P12FTravelPartyOwnerStager.TryStage(travelParties,
                    stagedSelectedNpcOwners, detachedNpcRows, stagedSpatialNetwork,
                    out TravelPartyStore stagedTravelParties, out _)
                || stagedTravelParties == null)
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12f.travel-parties");
                return false;
            }
            stageObserver?.Invoke(DailyV1P12FCoreOwnerRestoreStep.TravelParties);

            if (!P12FExpeditionOwnerSnapshotStager.TryStage(expeditions,
                    stagedNpcRuntimeIds, detachedNpcRows, stagedSelectedNpcOwners,
                    stagedIdentities, stagedSpatialNetwork, stagedExplorableSites,
                    stagedTravelParties, out ExpeditionStore stagedExpeditions, out _)
                || stagedExpeditions == null)
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12f.expeditions");
                return false;
            }
            stageObserver?.Invoke(DailyV1P12FCoreOwnerRestoreStep.Expeditions);

            stagedOwners = new DailyV1P12FCoreOwnerRestoreStage(
                stagedPoliticalKnowledge, stagedDirectives, stagedActorChoices,
                stagedTravelParties, stagedExpeditions);
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException
            || exception is NullReferenceException)
        {
            stagedOwners = null;
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12f.core-owners");
            return false;
        }
    }

    private bool HasRestoredContinuationStagingAuthority(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation)
    {
        bool initialRestore = expectedSession == null && replacementOperation == null;
        if (initialRestore)
        {
            return bootstrapAdmissionContext != null
                && bootstrapAdmissionContext.IsOwnedByCurrentThread()
                && activeSessionGate.Current == null
                && activeSessionGate.ActiveOperationCount == 0
                && !initialGenesisPreparationAttempted
                && preparedInitialGenesis == null
                && preparedInitialSession == null;
        }

        DailyV1ExecutionBoundaryState sourceState = expectedSession?.ExecutionBoundaryState;
        IDailyV1RuntimeAdvanceEvidenceBoundary sourceBoundary = expectedSession?.RuntimeAdvanceBoundary;
        return expectedSession != null && replacementOperation != null
            && replacementOperation.IsOwnedByCurrentThread()
            && ReferenceEquals(replacementOperation.ExpectedSession, expectedSession)
            && ReferenceEquals(activeSessionGate.Current, expectedSession)
            && replacementOperation.AuthorizesSessionReplacementCapture(sourceState)
            && activeSessionGate.ActiveOperationCount == 0
            && sourceBoundary != null
            && ReferenceEquals(sourceBoundary.ExecutionState, sourceState)
            && sourceState.IsHealthyDailyOwnerThreadBoundary()
            && sourceState.HasValidCompletedDailyBoundaryAdmission(
                sourceBoundary, sessionReplacement: true);
    }

    /// <summary>
    /// Associates an unpublished or replacement Core session with the exact
    /// host boundary built for its execution state. Once published, the
    /// session's boundary is immutable; advances resolve it from the session
    /// rather than accepting a host-selected boundary on each call.
    /// </summary>
    internal bool TryBindRuntimeAdvanceBoundary(
        DailyV1ActiveRuntimeSession candidateSession,
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts)
    {
        if (candidateSession == null || hostPorts == null
            || !ReferenceEquals(hostPorts.ExecutionState,
                candidateSession.ExecutionBoundaryState))
            return false;
        lock (activeSessionGate.SyncRoot)
        {
            DailyV1ActiveRuntimeSession current = activeSessionGate.Current;
            if (ReferenceEquals(current, candidateSession))
            {
                return candidateSession.TryBindSelectedRuntimeAdvanceHostPorts(hostPorts)
                    && hostPorts.TryBindApplicationAdvanceAuthority(this, candidateSession);
            }
            if (!IsBootstrapOrRestoredDescendant(candidateSession.AdmissionContext)
                || activeSessionGate.ActiveOperationCount != 0
                || (current != null && !current.IsProfileCompatibleWith(candidateSession)))
                return false;
            return candidateSession.TryBindSelectedRuntimeAdvanceHostPorts(hostPorts)
                && hostPorts.TryBindApplicationAdvanceAuthority(this, candidateSession);
        }
    }

    /// <summary>
    /// Advances only the exact published Core session. The application gate
    /// reserves that session against queries, saves, and replacement while
    /// the existing Core runtime protocol owns its single DailyAdvance scope,
    /// lease, completed-boundary accounting, and P12 token publication.
    /// </summary>
    internal bool TryAdvanceDay(
        DailyV1ActiveRuntimeSession expectedSession,
        out SimulationRuntimeAdvanceFailure failure)
    {
        IDailyV1RuntimeAdvanceEvidenceBoundary runtimeBoundary = expectedSession?.RuntimeAdvanceBoundary;
        failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
        if (expectedSession == null || runtimeBoundary == null
            || !ReferenceEquals(activeSessionGate.Current, expectedSession)
            || !ReferenceEquals(runtimeBoundary.ExecutionState,
                expectedSession.ExecutionBoundaryState))
            return false;

        if (!activeSessionGate.TryBeginRuntimeAdvance(
                expectedSession,
                out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.RuntimeAdvanceOperation operation))
        {
            failure = SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress;
            return false;
        }

        using (operation)
        {
            if (!ReferenceEquals(operation.ExpectedSession, expectedSession)
                || !operation.IsOwnedByCurrentThread()
                || !ReferenceEquals(activeSessionGate.Current, expectedSession))
                return false;

            return runtimeBoundary is DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary
                && expectedSession.ExecutionBoundaryState.TryAdvanceSelectedDailyV1Day(
                    coreBoundary, out failure);
        }
    }

    /// <summary>
    /// Advances the application's current session without requiring a host to
    /// retain or select a Core session reference. The operation gate still
    /// validates that the captured session remains the exact published one.
    /// </summary>
    internal bool TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure)
    {
        DailyV1ActiveRuntimeSession expectedSession = activeSessionGate.Current;
        failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
        return expectedSession != null && TryAdvanceDay(expectedSession, out failure);
    }

    /// <summary>
    /// Advances the exact published session for one serialized multi-day
    /// operation. The execution boundary owns one DailyAdvance scope and lease
    /// for the entire span, including completed-boundary accounting and its
    /// final P12 capture-token publication.
    /// </summary>
    internal bool TryAdvanceDays(
        DailyV1ActiveRuntimeSession expectedSession,
        int dayCount,
        out int daysAdvanced,
        out SimulationRuntimeAdvanceFailure failure)
    {
        IDailyV1RuntimeAdvanceEvidenceBoundary runtimeBoundary = expectedSession?.RuntimeAdvanceBoundary;
        daysAdvanced = 0;
        failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
        if (expectedSession == null || runtimeBoundary == null
            || !ReferenceEquals(activeSessionGate.Current, expectedSession)
            || !ReferenceEquals(runtimeBoundary.ExecutionState,
                expectedSession.ExecutionBoundaryState))
            return false;

        if (!activeSessionGate.TryBeginRuntimeAdvance(
                expectedSession,
                out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.RuntimeAdvanceOperation operation))
        {
            failure = SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress;
            return false;
        }

        using (operation)
        {
            if (!ReferenceEquals(operation.ExpectedSession, expectedSession)
                || !operation.IsOwnedByCurrentThread()
                || !ReferenceEquals(activeSessionGate.Current, expectedSession))
                return false;

            return runtimeBoundary is DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary
                && expectedSession.ExecutionBoundaryState.TryAdvanceSelectedDailyV1Days(
                    coreBoundary, dayCount, out daysAdvanced, out failure);
        }
    }

    /// <summary>Advances the application's current session as one serialized operation.</summary>
    internal bool TryAdvanceDays(
        int dayCount,
        out int daysAdvanced,
        out SimulationRuntimeAdvanceFailure failure)
    {
        DailyV1ActiveRuntimeSession expectedSession = activeSessionGate.Current;
        daysAdvanced = 0;
        failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
        return expectedSession != null
            && TryAdvanceDays(expectedSession, dayCount, out daysAdvanced, out failure);
    }

    internal bool TryPublishReplacement(
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation operation,
        DailyV1ActiveRuntimeSession candidate)
    {
        if (candidate?.RuntimeAdvanceBoundary == null) return false;
        lock (activeSessionGate.SyncRoot)
        {
            return bootstrapAdmissionContext != null
                && IsBootstrapOrRestoredDescendant(candidate.AdmissionContext)
                && activeSessionGate.TryPublishReplacement(operation, candidate);
        }
    }

    internal bool TryPublishReplacement(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ActiveRuntimeSession candidate)
    {
        if (candidate?.RuntimeAdvanceBoundary == null) return false;
        lock (activeSessionGate.SyncRoot)
        {
            return bootstrapAdmissionContext != null
                && IsBootstrapOrRestoredDescendant(candidate.AdmissionContext)
                && activeSessionGate.TryPublishReplacement(expectedSession, candidate);
        }
    }

    /// <summary>
    /// Commits one already staged P12 restored candidate. All admission,
    /// provenance, owner-thread, source-boundary, profile, and reservation
    /// checks are made against the exact Core sessions under the same lock as
    /// the sole active-session exchange. The host boundary supplies only a
    /// fresh owner census; Core verifies the token, identities, revisions,
    /// epoch, provenance, and replacement reservation before publication.
    /// </summary>
    internal bool TryCommitRestoredCandidate(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ActiveRuntimeSession candidate,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out string diagnostic)
    {
        diagnostic = null;
        if (candidate == null)
        {
            diagnostic = "The Core application cannot commit a null restored session.";
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
            SimulationRuntimeAdmissionContext candidateAdmission = candidate.AdmissionContext;
            if (bootstrapAdmissionContext == null
                || !bootstrapAdmissionContext.IsOwnedByCurrentThread()
                || candidateAdmission == null
                || !candidateAdmission.IsRestoredContinuation
                || !candidateAdmission.IsOwnedByCurrentThread()
                || !IsBootstrapOrRestoredDescendant(candidateAdmission)
                || !candidate.ExecutionBoundaryState.IsHealthyDailyOwnerThreadBoundary()
                || !candidate.ExecutionBoundaryState.HasValidRestoredDailyBoundaryAdmission(
                    candidate.RuntimeAdvanceBoundary)
                || candidate.ExecutionBoundaryState.ActiveOperationCount != 0)
            {
                diagnostic = "The restored candidate is not a healthy, idle, exact Core continuation admitted on this application's owner thread.";
                return false;
            }

            DailyV1ActiveRuntimeSession current = activeSessionGate.Current;
            if (current == null)
            {
                if (expectedSession != null
                    || replacementOperation != null
                    || activeSessionGate.ActiveOperationCount != 0
                    || initialGenesisPreparationAttempted
                    || preparedInitialGenesis != null
                    || preparedInitialSession != null
                    || !activeSessionGate.TryPublishInitial(candidate))
                {
                    diagnostic = "Initial restore requires an unused Core application and a candidate with no replacement reservation.";
                    return false;
                }

                return true;
            }

            if (expectedSession == null
                || !ReferenceEquals(expectedSession, current)
                || replacementOperation == null
                || !ReferenceEquals(replacementOperation.ExpectedSession, current)
                || !replacementOperation.AuthorizesSessionReplacementCapture(
                    current.ExecutionBoundaryState)
                || activeSessionGate.ActiveOperationCount != 0
                || !current.ExecutionBoundaryState.IsHealthyDailyOwnerThreadBoundary()
                || !current.ExecutionBoundaryState.HasValidCompletedDailyBoundaryAdmission(
                    current.RuntimeAdvanceBoundary, sessionReplacement: true)
                || !current.IsProfileCompatibleWith(candidate)
                || !activeSessionGate.TryPublishReplacement(
                    replacementOperation, candidate))
            {
                diagnostic = "Restored replacement requires the exact healthy current Core session, its owner-thread reservation, and a compatible admitted candidate.";
                return false;
            }

            return true;
        }
    }

    private bool IsBootstrapOrRestoredDescendant(
        SimulationRuntimeAdmissionContext candidate)
    {
        if (ReferenceEquals(candidate, bootstrapAdmissionContext)) return true;
        return candidate != null
            && candidate.IsRestoredContinuationOf(bootstrapAdmissionContext);
    }
}
