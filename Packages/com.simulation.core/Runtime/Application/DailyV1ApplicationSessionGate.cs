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
    private TSession current;
    private int activeOperationCount;

    internal object SyncRoot => syncRoot;

    internal TSession Current => Volatile.Read(ref current);

    internal int ActiveOperationCount
    {
        get
        {
            lock (syncRoot) return activeOperationCount;
        }
    }

    internal bool TryBeginOperation(TSession expectedSession, out TSession session)
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

            if (activeOperationCount == int.MaxValue)
            {
                session = null;
                return false;
            }

            activeOperationCount++;
            return true;
        }
    }

    internal void EndOperation()
    {
        lock (syncRoot)
        {
            if (activeOperationCount <= 0)
                throw new InvalidOperationException("Active-session operation scope underflow.");
            activeOperationCount--;
        }
    }

    internal bool TryPublishInitial(TSession candidateSession)
    {
        if (candidateSession == null) return false;
        lock (syncRoot)
        {
            if (activeOperationCount != 0 || current != null) return false;
            return ReferenceEquals(
                Interlocked.CompareExchange(ref current, candidateSession, null), null);
        }
    }

    internal bool TryPublishReplacement(TSession expectedSession, TSession candidateSession)
    {
        if (expectedSession == null || candidateSession == null) return false;
        lock (syncRoot)
        {
            if (activeOperationCount != 0 || !ReferenceEquals(current, expectedSession))
                return false;
            Interlocked.Exchange(ref current, candidateSession);
            return true;
        }
    }
}
