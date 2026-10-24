using System;

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

    // Construction remains internal until the complete TryStart path can
    // create and return the application only after genesis or restore commits.
    internal DailyV1Application() { }

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

    internal bool TryPublishInitial(DailyV1ActiveRuntimeSession candidate) =>
        activeSessionGate.TryPublishInitial(candidate);

    internal bool TryBeginExclusiveOperation(
        DailyV1ActiveRuntimeSession expectedSession,
        out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation operation) =>
        activeSessionGate.TryBeginExclusiveOperation(expectedSession, out operation);

    internal bool TryPublishReplacement(
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation operation,
        DailyV1ActiveRuntimeSession candidate) =>
        activeSessionGate.TryPublishReplacement(operation, candidate);

    internal bool TryPublishReplacement(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ActiveRuntimeSession candidate) =>
        activeSessionGate.TryPublishReplacement(expectedSession, candidate);
}
