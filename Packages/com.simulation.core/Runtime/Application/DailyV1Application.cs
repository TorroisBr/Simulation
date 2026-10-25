using System;
using System.Collections.Generic;
using System.Threading;

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
    /// Composes and publishes the Core-owned initial session after the caller
    /// has established that no continuation slot exists. This is the genesis
    /// fallback used by the eventual public TryStart path; it intentionally
    /// does not inspect host storage or construct a Unity runtime projection.
    /// </summary>
    internal bool TryCreateAndPublishInitialGenesis(
        DailyV1EffectiveDefinitions definitions,
        Action<string> stageCompleted,
        out string diagnostic)
    {
        if (!TryPrepareInitialGenesis(
                definitions,
                allocateWorldId: null,
                stageCompleted,
                out _,
                out DailyV1ActiveRuntimeSession candidate,
                out diagnostic))
            return false;

        if (!TryPublishInitial(candidate))
        {
            diagnostic = "The Core application rejected its exact initial genesis session.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds one unpublished selected Daily-v1 genesis candidate under the
    /// application-captured bootstrap context. A host may finish creating its
    /// projections and validate its publication boundary before calling
    /// TryPublishInitial with the returned exact Core session.
    /// </summary>
    internal bool TryPrepareInitialGenesis(
        DailyV1EffectiveDefinitions definitions,
        Func<WorldId> allocateWorldId,
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

        if (!DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                admissionContext,
                allocateWorldId,
                stageCompleted,
                out composition,
                out diagnostic))
        {
            composition = null;
            return false;
        }

        if (!DailyV1ActiveRuntimeSession.TryCreate(
                composition.ExecutionBoundaryState,
                definitions,
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
            || !ReferenceEquals(ownerGraph.Definitions, expectedDefinitions))
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

            if (!DailyV1ActiveRuntimeSession.TryCreate(
                    executionBoundaryState,
                    expectedDefinitions,
                    lastEconomySnapshotDay,
                    out candidate,
                    out diagnostic))
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
        out string diagnostic)
    {
        candidate = null;
        calendar = null;
        diagnostic = null;
        if (definitions == null
            || !ReferenceEquals(definitions.EffectiveConfiguration, effectiveConfiguration))
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

    internal void EndOperation(DailyV1OperationScopeKind scopeKind) =>
        activeSessionGate.EndOperation(scopeKind);

    internal bool TryPublishInitial(DailyV1ActiveRuntimeSession candidate)
    {
        if (candidate == null) return false;
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
    /// Associates an unpublished or replacement Core session with the exact
    /// host boundary built for its execution state. Once published, the
    /// session's boundary is immutable; advances resolve it from the session
    /// rather than accepting a host-selected boundary on each call.
    /// </summary>
    internal bool TryBindRuntimeAdvanceBoundary(
        DailyV1ActiveRuntimeSession candidateSession,
        IDailyV1RuntimeAdvanceBoundary runtimeBoundary)
    {
        if (candidateSession == null || runtimeBoundary == null) return false;
        lock (activeSessionGate.SyncRoot)
        {
            DailyV1ActiveRuntimeSession current = activeSessionGate.Current;
            if (ReferenceEquals(current, candidateSession))
                return ReferenceEquals(candidateSession.RuntimeAdvanceBoundary, runtimeBoundary);
            if (!IsBootstrapOrRestoredDescendant(candidateSession.AdmissionContext)
                || activeSessionGate.ActiveOperationCount != 0
                || (current != null && !current.IsProfileCompatibleWith(candidateSession)))
                return false;
            return candidateSession.TryBindRuntimeAdvanceBoundary(runtimeBoundary);
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
        IDailyV1RuntimeAdvanceBoundary runtimeBoundary = expectedSession?.RuntimeAdvanceBoundary;
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

            return expectedSession.ExecutionBoundaryState.TryAdvanceDay(
                runtimeBoundary, out failure);
        }
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
        IDailyV1RuntimeAdvanceBoundary runtimeBoundary = expectedSession?.RuntimeAdvanceBoundary;
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

            return expectedSession.ExecutionBoundaryState.TryAdvanceDays(
                runtimeBoundary, dayCount, out daysAdvanced, out failure);
        }
    }

    internal bool TryPublishReplacement(
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation operation,
        DailyV1ActiveRuntimeSession candidate)
    {
        if (candidate == null) return false;
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
        if (candidate == null) return false;
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
