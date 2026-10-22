using System;
using System.Threading;

/// <summary>
/// Owns the active-session reference and operation serialization shared by the
/// Unity and detached hosts. The session type stays internal to the owning
/// application assembly; this gate owns its publication lifecycle only.
/// </summary>
internal sealed class DailyV1ApplicationSessionGate<TSession>
    where TSession : class
{
    private readonly object syncRoot = new object();
    private readonly Func<TSession, DailyV1ExecutionBoundaryState> executionState;
    private TSession current;

    internal DailyV1ApplicationSessionGate(
        Func<TSession, DailyV1ExecutionBoundaryState> executionState)
    {
        this.executionState = executionState
            ?? throw new ArgumentNullException(nameof(executionState));
    }

    internal object SyncRoot => syncRoot;

    internal TSession Current => Volatile.Read(ref current);

    internal int ActiveOperationCount
    {
        get { lock (syncRoot) return GetExecutionState(current)?.ActiveOperationCount ?? 0; }
    }

    internal bool TryBeginOperation(TSession expectedSession, out TSession session) =>
        TryBeginOperation(expectedSession, DailyV1OperationScopeKind.Application, out session);

    internal bool TryBeginOperation(
        TSession expectedSession,
        DailyV1OperationScopeKind scopeKind,
        out TSession session)
    {
        lock (syncRoot)
        {
            session = current;
            if (session == null
                || (expectedSession != null && !ReferenceEquals(session, expectedSession)))
            {
                session = null;
                return false;
            }

            DailyV1ExecutionBoundaryState state = GetExecutionState(session);
            if (state == null || !state.TryBeginOperation(scopeKind))
            {
                session = null;
                return false;
            }
            return true;
        }
    }

    internal void EndOperation() => EndOperation(DailyV1OperationScopeKind.Application);

    internal void EndOperation(DailyV1OperationScopeKind scopeKind)
    {
        lock (syncRoot)
        {
            DailyV1ExecutionBoundaryState state = GetExecutionState(current);
            if (state == null || !state.TryEndOperation(scopeKind))
                throw new InvalidOperationException("Active-session operation scope underflow.");
        }
    }

    internal bool TryPublishInitial(TSession candidateSession)
    {
        if (candidateSession == null) return false;
        lock (syncRoot)
        {
            DailyV1ExecutionBoundaryState state = GetExecutionState(candidateSession);
            if (state == null || state.ActiveOperationCount != 0 || current != null) return false;
            return ReferenceEquals(
                Interlocked.CompareExchange(ref current, candidateSession, null), null);
        }
    }

    internal bool TryPublishReplacement(TSession expectedSession, TSession candidateSession)
    {
        if (expectedSession == null || candidateSession == null) return false;
        lock (syncRoot)
        {
            DailyV1ExecutionBoundaryState currentState = GetExecutionState(current);
            DailyV1ExecutionBoundaryState candidateState = GetExecutionState(candidateSession);
            if (currentState == null || candidateState == null
                || currentState.ActiveOperationCount != 0
                || candidateState.ActiveOperationCount != 0
                || !ReferenceEquals(current, expectedSession))
                return false;
            Interlocked.Exchange(ref current, candidateSession);
            return true;
        }
    }

    private DailyV1ExecutionBoundaryState GetExecutionState(TSession session) =>
        session == null ? null : executionState(session);
}
