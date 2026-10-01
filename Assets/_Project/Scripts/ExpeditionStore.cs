using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>Serialized owner for active ExpeditionRuntime truth.</summary>
public sealed class ExpeditionStore
{
    private readonly object sync = new object();
    private readonly List<ExpeditionRuntime> activeExpeditions = new List<ExpeditionRuntime>();
    private readonly Dictionary<string, ExpeditionRuntime> expeditionsById = new Dictionary<string, ExpeditionRuntime>(StringComparer.Ordinal);
    private readonly HashSet<ExpeditionRuntime> fenced = new HashSet<ExpeditionRuntime>();
    private readonly IReadOnlyList<ExpeditionRuntime> readOnlyActiveExpeditions;
    private long revision;
    private long reservedWrites;

    internal sealed class OperationReservation : IDisposable
    {
        internal readonly ExpeditionStore Store; internal readonly ExpeditionRuntime Expected; internal int Remaining; internal bool IsFenced; internal bool Released;
        internal OperationReservation(ExpeditionStore store, ExpeditionRuntime expected, int remaining) { Store = store; Expected = expected; Remaining = remaining; }
        public void Dispose() { Store.Release(this); }
    }

    public IReadOnlyList<ExpeditionRuntime> ActiveExpeditions { get { lock (sync) return readOnlyActiveExpeditions; } }
    public long Revision { get { lock (sync) return revision; } }

    public ExpeditionStore() { readOnlyActiveExpeditions = activeExpeditions.AsReadOnly(); }

    public ExpeditionRuntime GetById(string id)
    {
        lock (sync) return !string.IsNullOrWhiteSpace(id) && expeditionsById.TryGetValue(id, out ExpeditionRuntime value) ? value : null;
    }
    public ExpeditionRuntime GetByExpeditionId(string id) => GetById(id);
    public bool TryGetById(string id, out ExpeditionRuntime value)
    {
        lock (sync) { if (!string.IsNullOrWhiteSpace(id) && expeditionsById.TryGetValue(id, out value)) return true; value = null; return false; }
    }
    public bool TryGetByExpeditionId(string id, out ExpeditionRuntime value) => TryGetById(id, out value);

    public bool Add(ExpeditionRuntime expedition)
    {
        lock (sync)
        {
            if (!CanAdd(expedition) || !CanCommit(1)) return false;
            expeditionsById.Add(expedition.ExpeditionId, expedition);
            activeExpeditions.Add(expedition);
            if (!expedition.TryAttach(this)) { activeExpeditions.Remove(expedition); expeditionsById.Remove(expedition.ExpeditionId); return false; }
            revision++;
            return true;
        }
    }

    internal bool TryReserveNew(ExpeditionRuntime expected, out OperationReservation reservation)
    {
        lock (sync) { reservation = null; if (expected == null || expected.AttachedStore != null || !CanReserve(2)) return false; reservedWrites += 2; reservation = new OperationReservation(this, expected, 2); return true; }
    }
    internal bool TryReserveExisting(ExpeditionRuntime expected, out OperationReservation reservation)
    {
        lock (sync) { reservation = null; if (!IsExactActive(expected) || fenced.Contains(expected) || !CanReserve(2)) return false; reservedWrites += 2; reservation = new OperationReservation(this, expected, 2); return true; }
    }
    internal bool TryReserveObjectiveCompletion(ExpeditionRuntime expected, out OperationReservation reservation)
    {
        lock (sync)
        {
            reservation = null;
            if (!IsExactActive(expected) || fenced.Contains(expected) || expected.IsObjectiveComplete
                || !CanReserve(1)) return false;
            reservedWrites++;
            reservation = new OperationReservation(this, expected, 1);
            fenced.Add(expected); reservation.IsFenced = true;
            return true;
        }
    }
    internal bool AddAndFence(OperationReservation token)
    {
        lock (sync)
        {
            if (!TokenValid(token) || token.Remaining < 2 || !CanAdd(token.Expected)) return false;
            expeditionsById.Add(token.Expected.ExpeditionId, token.Expected); activeExpeditions.Add(token.Expected);
            if (!token.Expected.TryAttach(this)) { activeExpeditions.Remove(token.Expected); expeditionsById.Remove(token.Expected.ExpeditionId); return false; }
            fenced.Add(token.Expected); token.IsFenced = true; CommitReservation(token); return true;
        }
    }
    internal bool CommitReserved(OperationReservation token, Func<bool> action)
    {
        lock (sync)
        {
            if (!TokenValid(token) || token.Remaining == 0 || !IsExactActive(token.Expected)) return false;
            bool newlyFenced = !token.IsFenced;
            if (newlyFenced) { fenced.Add(token.Expected); token.IsFenced = true; }
            if (!action()) { if (newlyFenced) { fenced.Remove(token.Expected); token.IsFenced = false; } return false; }
            CommitReservation(token); return true;
        }
    }
    internal bool RemoveReserved(OperationReservation token)
    {
        lock (sync)
        {
            if (!TokenValid(token) || !token.IsFenced || token.Remaining == 0 || !IsExactActive(token.Expected)
                || token.Expected.State != ExpeditionState.Preparing) return false;
            RemoveExact(token.Expected); CommitReservation(token); return true;
        }
    }
    private bool CanReserve(long count) => count >= 0 && revision <= long.MaxValue - reservedWrites - count;
    private bool TokenValid(OperationReservation token) => token != null && ReferenceEquals(token.Store, this) && !token.Released;
    private void CommitReservation(OperationReservation token) { token.Remaining--; reservedWrites--; revision++; }
    private void Release(OperationReservation token)
    {
        lock (sync)
        {
            if (!TokenValid(token)) return;
            if (token.IsFenced) fenced.Remove(token.Expected);
            reservedWrites -= token.Remaining; token.Remaining = 0; token.Released = true;
        }
    }

    public bool TryGetExpeditionForNpc(string npcRuntimeId, out ExpeditionRuntime expedition)
    {
        lock (sync)
        {
            expedition = null;
            if (string.IsNullOrWhiteSpace(npcRuntimeId)) return false;
            foreach (ExpeditionRuntime candidate in activeExpeditions)
                if (ContainsMember(candidate.MemberRuntimeIds, npcRuntimeId)) { expedition = candidate; return true; }
            return false;
        }
    }
    public bool IsNpcOnActiveExpedition(string id) => TryGetExpeditionForNpc(id, out _);

    public bool Remove(string id)
    {
        lock (sync)
        {
            ExpeditionRuntime value = GetById(id);
            if (value == null || value.State != ExpeditionState.Preparing || fenced.Contains(value) || !CanCommit(1)) return false;
            RemoveExact(value); revision++; return true;
        }
    }

    public bool Complete(string id)
    {
        lock (sync)
        {
            ExpeditionRuntime value = GetById(id);
            if (value == null || value.State != ExpeditionState.Completed || fenced.Contains(value) || !CanCommit(1)) return false;
            RemoveExact(value); revision++; return true;
        }
    }

    /// <summary>Exact-object completion and removal as one owner commit.</summary>
    public bool TryFinalizeCompletion(ExpeditionRuntime expected)
    {
        lock (sync)
        {
            if (!IsExactActive(expected) || expected.State != ExpeditionState.Returning || fenced.Contains(expected) || !CanCommit(1)) return false;
            expected.TryCompleteCore(); RemoveExact(expected); revision++; return true;
        }
    }

    internal bool TryMutate(ExpeditionRuntime expected, Func<bool> mutation)
    {
        lock (sync)
        {
            if (!IsExactActive(expected) || fenced.Contains(expected) || !CanCommit(1)) return false;
            ExpeditionRuntime.OwnerSnapshot before = expected.CaptureOwnerSnapshot();
            if (!mutation()) return false;
            if (!before.Equals(expected.CaptureOwnerSnapshot())) revision++;
            return true;
        }
    }

    internal bool IsExactActive(ExpeditionRuntime expected)
    {
        return expected != null && expeditionsById.TryGetValue(expected.ExpeditionId, out ExpeditionRuntime actual)
            && ReferenceEquals(expected, actual) && activeExpeditions.Contains(expected) && expected.IsActive;
    }

    internal IDisposable EnterReadWindow() { Monitor.Enter(sync); return new Window(sync); }
    internal bool ValidateCensus(out int count, out long currentRevision)
    {
        lock (sync)
        {
            count = 0; currentRevision = revision;
            if (revision < 0 || activeExpeditions.Count != expeditionsById.Count) return false;
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (ExpeditionRuntime value in activeExpeditions)
            {
                if (value == null || !value.IsActive || !ReferenceEquals(value.AttachedStore, this)
                    || string.IsNullOrWhiteSpace(value.ExpeditionId) || !ids.Add(value.ExpeditionId)
                    || !expeditionsById.TryGetValue(value.ExpeditionId, out ExpeditionRuntime indexed)
                    || !ReferenceEquals(indexed, value)) return false;
            }
            foreach (KeyValuePair<string, ExpeditionRuntime> pair in expeditionsById)
                if (pair.Value == null || !ids.Contains(pair.Key) || !string.Equals(pair.Key, pair.Value.ExpeditionId, StringComparison.Ordinal)) return false;
            count = activeExpeditions.Count; return true;
        }
    }

    private bool CanAdd(ExpeditionRuntime expedition)
    {
        if (expedition == null || !expedition.IsActive || string.IsNullOrWhiteSpace(expedition.ExpeditionId)
            || expeditionsById.ContainsKey(expedition.ExpeditionId) || expedition.AttachedStore != null) return false;
        foreach (string member in expedition.MemberRuntimeIds) if (TryGetExpeditionForNpc(member, out _)) return false;
        return true;
    }
    private bool CanCommit(long count) => count >= 0 && revision <= long.MaxValue - reservedWrites - count;
    private void RemoveExact(ExpeditionRuntime value) { expeditionsById.Remove(value.ExpeditionId); activeExpeditions.Remove(value); }
    private static bool ContainsMember(IReadOnlyList<string> ids, string expected)
    { if (ids == null) return false; foreach (string id in ids) if (string.Equals(id, expected, StringComparison.Ordinal)) return true; return false; }
    private sealed class Window : IDisposable
    { private object gate; public Window(object gate) { this.gate = gate; } public void Dispose() { object value = Interlocked.Exchange(ref gate, null); if (value != null) Monitor.Exit(value); } }
}
