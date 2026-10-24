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
            if (bootstrapAdmissionContext == null
                || !IsBootstrapOrRestoredDescendant(candidate.AdmissionContext))
                return false;
            return activeSessionGate.TryPublishInitial(candidate);
        }
    }

    internal bool TryBeginExclusiveOperation(
        DailyV1ActiveRuntimeSession expectedSession,
        out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation operation) =>
        activeSessionGate.TryBeginExclusiveOperation(expectedSession, out operation);

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
