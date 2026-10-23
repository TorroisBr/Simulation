using System;
using System.Threading;

/// <summary>
/// Mutable reporting progress kept separate from the immutable active-session
/// reference set. Hosts may read a snapshot and update it only while operating
/// on the owning session thread.
/// </summary>
internal sealed class DailyV1SessionReportState
{
    internal long LastEconomySnapshotDay { get; private set; }

    internal DailyV1SessionReportState(long lastEconomySnapshotDay)
    {
        LastEconomySnapshotDay = lastEconomySnapshotDay;
    }

    internal void MarkEconomySnapshotDay(long day)
    {
        LastEconomySnapshotDay = day;
    }
}

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
    private object exclusiveOperationToken;
    private DailyV1ExecutionBoundaryState activeExclusiveState;

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
            if (exclusiveOperationToken != null
                || session == null
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
            if (exclusiveOperationToken != null
                || state == null || state.ActiveOperationCount != 0 || current != null) return false;
            return ReferenceEquals(
                Interlocked.CompareExchange(ref current, candidateSession, null), null);
        }
    }

    /// <summary>
    /// Reserves the current idle session while a detached candidate is loaded,
    /// validated, and prepared for publication. Unlike holding SyncRoot over
    /// candidate construction, the reservation rejects same-thread callback
    /// re-entry as well as callers on other threads.
    /// </summary>
    internal bool TryBeginExclusiveOperation(
        TSession expectedSession,
        out ExclusiveOperation operation)
    {
        operation = null;
        if (expectedSession == null) return false;
        lock (syncRoot)
        {
            DailyV1ExecutionBoundaryState state = GetExecutionState(current);
            if (exclusiveOperationToken != null
                || !ReferenceEquals(current, expectedSession)
                || state == null
                || state.ActiveOperationCount != 0
                || !state.TryBeginExclusiveSessionReplacement())
                return false;

            object token = new object();
            exclusiveOperationToken = token;
            activeExclusiveState = state;
            operation = new ExclusiveOperation(this, token, expectedSession);
            return true;
        }
    }

    /// <summary>Publishes only through the exclusive reservation that staged this replacement.</summary>
    internal bool TryPublishReplacement(
        ExclusiveOperation operation,
        TSession candidateSession)
    {
        if (operation == null || candidateSession == null) return false;
        lock (syncRoot)
        {
            if (!operation.IsOwnedBy(this, exclusiveOperationToken)
                || !operation.IsOwnedByCurrentThread()
                || !ReferenceEquals(current, operation.ExpectedSession))
                return false;

            DailyV1ExecutionBoundaryState currentState = GetExecutionState(current);
            DailyV1ExecutionBoundaryState candidateState = GetExecutionState(candidateSession);
            if (currentState == null || candidateState == null
                || currentState.ActiveOperationCount != 0
                || candidateState.ActiveOperationCount != 0)
                return false;

            Interlocked.Exchange(ref current, candidateSession);
            return true;
        }
    }

    internal bool TryPublishReplacement(TSession expectedSession, TSession candidateSession)
    {
        if (expectedSession == null || candidateSession == null) return false;
        lock (syncRoot)
        {
            if (exclusiveOperationToken != null) return false;
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

    private void EndExclusiveOperation(object token)
    {
        lock (syncRoot)
        {
            if (token == null
                || !ReferenceEquals(exclusiveOperationToken, token)
                || activeExclusiveState == null
                || !activeExclusiveState.TryEndExclusiveSessionReplacement())
                throw new InvalidOperationException("The active-session exclusive operation is no longer current.");
            exclusiveOperationToken = null;
            activeExclusiveState = null;
        }
    }

    private bool AuthorizesSessionReplacementCapture(
        ExclusiveOperation operation,
        object token,
        TSession expectedSession,
        DailyV1ExecutionBoundaryState expectedState)
    {
        if (operation == null || expectedSession == null || expectedState == null
            || !operation.IsOwnedBy(this, token)
            || !ReferenceEquals(operation.ExpectedSession, expectedSession)
            || !operation.IsOwnedByCurrentThread())
            return false;

        lock (syncRoot)
        {
            return ReferenceEquals(exclusiveOperationToken, token)
                && ReferenceEquals(current, expectedSession)
                && ReferenceEquals(activeExclusiveState, expectedState)
                && ReferenceEquals(GetExecutionState(expectedSession), expectedState);
        }
    }

    internal sealed class ExclusiveOperation : IDisposable
    {
        private DailyV1ApplicationSessionGate<TSession> gate;
        private readonly object token;
        private readonly Thread ownerThread;
        private readonly int ownerThreadId;

        internal TSession ExpectedSession { get; }

        internal ExclusiveOperation(
            DailyV1ApplicationSessionGate<TSession> gate,
            object token,
            TSession expectedSession)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.token = token ?? throw new ArgumentNullException(nameof(token));
            ExpectedSession = expectedSession ?? throw new ArgumentNullException(nameof(expectedSession));
            ownerThread = Thread.CurrentThread;
            ownerThreadId = ownerThread.ManagedThreadId;
        }

        internal bool IsOwnedBy(
            DailyV1ApplicationSessionGate<TSession> expectedGate,
            object expectedToken) =>
            ReferenceEquals(gate, expectedGate)
            && ReferenceEquals(token, expectedToken);

        internal bool IsOwnedByCurrentThread() =>
            ReferenceEquals(ownerThread, Thread.CurrentThread)
            && ownerThreadId == Thread.CurrentThread.ManagedThreadId;

        /// <summary>
        /// Grants read-only completed-boundary validation to P12-G staging
        /// only while this exact replacement reservation is active on its
        /// owner thread. It does not grant ordinary P12 capture eligibility.
        /// </summary>
        internal bool AuthorizesSessionReplacementCapture(DailyV1ExecutionBoundaryState expectedState)
        {
            DailyV1ApplicationSessionGate<TSession> owner = gate;
            return owner != null
                && IsOwnedByCurrentThread()
                && owner.AuthorizesSessionReplacementCapture(
                    this, token, ExpectedSession, expectedState);
        }

        public void Dispose()
        {
            DailyV1ApplicationSessionGate<TSession> owner = gate;
            if (owner == null) return;
            if (!IsOwnedByCurrentThread())
                throw new InvalidOperationException("The active-session exclusive operation must close on its owner thread.");
            owner.EndExclusiveOperation(token);
            gate = null;
        }
    }
}
