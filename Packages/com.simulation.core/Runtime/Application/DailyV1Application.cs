using System;
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
    /// Advances only the exact published Core session. The application gate
    /// reserves that session against queries, saves, and replacement while
    /// the existing Core runtime protocol owns its single DailyAdvance scope,
    /// lease, completed-boundary accounting, and P12 token publication.
    /// </summary>
    internal bool TryAdvanceDay(
        DailyV1ActiveRuntimeSession expectedSession,
        IDailyV1RuntimeAdvanceBoundary runtimeBoundary,
        out SimulationRuntimeAdvanceFailure failure)
    {
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
        IDailyV1RuntimeAdvanceBoundary runtimeBoundary,
        int dayCount,
        out int daysAdvanced,
        out SimulationRuntimeAdvanceFailure failure)
    {
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

    private bool IsBootstrapOrRestoredDescendant(
        SimulationRuntimeAdmissionContext candidate)
    {
        if (ReferenceEquals(candidate, bootstrapAdmissionContext)) return true;
        return candidate != null
            && candidate.IsRestoredContinuationOf(bootstrapAdmissionContext);
    }
}
