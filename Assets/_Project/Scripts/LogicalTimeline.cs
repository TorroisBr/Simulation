using System;
using System.Collections.Generic;

/// <summary>A nonnegative millisecond-sized simulation instant, independent of host time.</summary>
public struct LogicalTick : IEquatable<LogicalTick>, IComparable<LogicalTick>
{
    public const long TicksPerDay = 86400000L;
    public long Value { get; }

    public LogicalTick(long value)
    {
        if (value < 0L) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }

    public long AbsoluteDay => Value / TicksPerDay;
    public long TickOfDay => Value % TicksPerDay;
    public LogicalTick NextDayBoundary => new LogicalTick(checked(checked(AbsoluteDay + 1L) * TicksPerDay));
    public SimulationDate ToDate(SimulationCalendar calendar)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));
        return calendar.GetDate(AbsoluteDay);
    }
    public int CompareTo(LogicalTick other) => Value.CompareTo(other.Value);
    public bool Equals(LogicalTick other) => Value == other.Value;
    public override bool Equals(object obj) => obj is LogicalTick other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
    public static bool operator ==(LogicalTick left, LogicalTick right) => left.Equals(right);
    public static bool operator !=(LogicalTick left, LogicalTick right) => !left.Equals(right);
}

public enum TimelineFailure
{
    None = 0, TargetBeforeNow = 1, Overflow = 2, ReentrantAdvance = 3,
    UnknownWorkKind = 4, StaleWork = 5, DispatchFailed = 6, InstantWorkLimitExceeded = 7,
    DailyBoundaryFailed = 8
}

/// <summary>Closed P18-A descriptor kinds. Instance identity is independent of participant identity.</summary>
public enum DueWorkKind { DomainOperation = 1 }

/// <summary>Derived, stable identity for the legacy daily-pass operation at one crossed boundary.</summary>
public sealed class DailyBoundaryOperation
{
    public long AbsoluteDay { get; }
    public LogicalTick DueAt { get; }
    public string OccurrenceId => "legacy-daily-boundary:" + AbsoluteDay;
    internal DailyBoundaryOperation(long absoluteDay)
    { AbsoluteDay = absoluteDay; DueAt = new LogicalTick(checked(absoluteDay * LogicalTick.TicksPerDay)); }
}

/// <summary>Stable, data-only reference to owner-held causal work; contains no executable object.</summary>
public sealed class DueWorkReference
{
    public string OwnerId { get; }
    public string DueWorkId { get; }
    public string InstanceId { get; }
    public long Revision { get; }
    public long OccurrenceSequence { get; }
    public LogicalTick DueAt { get; }
    public DueWorkKind Kind { get; }

    public DueWorkReference(string ownerId, string dueWorkId, string instanceId, long revision,
        long occurrenceSequence, LogicalTick dueAt, DueWorkKind kind = DueWorkKind.DomainOperation)
    {
        if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner identity is required.", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(dueWorkId)) throw new ArgumentException("Work identity is required.", nameof(dueWorkId));
        if (string.IsNullOrWhiteSpace(instanceId)) throw new ArgumentException("Instance identity is required.", nameof(instanceId));
        if (revision < 0L) throw new ArgumentOutOfRangeException(nameof(revision));
        if (occurrenceSequence < 0L) throw new ArgumentOutOfRangeException(nameof(occurrenceSequence));
        OwnerId = ownerId; DueWorkId = dueWorkId; InstanceId = instanceId;
        Revision = revision; OccurrenceSequence = occurrenceSequence; DueAt = dueAt; Kind = kind;
    }
}

/// <summary>Owner boundary: resolve current truth and atomically commit consumption/effects.</summary>
public interface IDueWorkOwner
{
    bool IsCurrent(DueWorkReference reference);
    /// <summary>Stages a mutation without changing owner truth; commit publishes its staged facts atomically.</summary>
    bool TryPrepare(DueWorkReference reference, out IDueWorkCommit prepared, out TimelineFailure failure);
}

/// <summary>Prepared owner transaction. Facts are published only after its atomic commit succeeds.</summary>
public interface IDueWorkCommit
{
    IReadOnlyList<DueWorkReference> NewOwnerFacts { get; }
    bool TryCommit(out TimelineFailure failure);
}

/// <summary>
/// World-local chronological clock and deterministic due-work index. The owner remains authoritative;
/// references are rebuilt from owner facts and are revalidated immediately before dispatch.
/// </summary>
public sealed class SimulationTimeline
{
    public const int DefaultMaxDispatchesPerInstant = 100000;
    private readonly SimulationCalendar calendar;
    private readonly IDueWorkOwner owner;
    private readonly Action<DailyBoundaryOperation> dailyBoundaryOperation;
    private readonly SortedDictionary<long, List<ScheduledDueWork>> agenda = new SortedDictionary<long, List<ScheduledDueWork>>();
    private readonly HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
    private long now;
    private long lastCompletedBoundaryDay;
    private bool advancing;
    private bool dispatching;
    private long causalSequence;
    private readonly int maxDispatchesPerInstant;

    public SimulationTimeline(SimulationCalendar calendar, LogicalTick initialInstant, IDueWorkOwner owner = null,
        Action<DailyBoundaryOperation> dailyBoundaryOperation = null,
        int maxDispatchesPerInstant = DefaultMaxDispatchesPerInstant)
    {
        this.calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        if (maxDispatchesPerInstant <= 0) throw new ArgumentOutOfRangeException(nameof(maxDispatchesPerInstant));
        this.owner = owner;
        this.dailyBoundaryOperation = dailyBoundaryOperation;
        this.maxDispatchesPerInstant = maxDispatchesPerInstant;
        now = initialInstant.Value;
        lastCompletedBoundaryDay = initialInstant.AbsoluteDay;
        calendar.GetDate(initialInstant.AbsoluteDay);
    }

    public LogicalTick CurrentInstant => new LogicalTick(now);
    public SimulationDate CurrentDate => CurrentInstant.ToDate(calendar);
    public long CausalSequence => causalSequence;
    public LogicalTick? NextDueInstant => agenda.Count == 0 ? (LogicalTick?)null : new LogicalTick(FirstKey());

    public SimulationDate GetCalendarProjection(LogicalTick instant) => instant.ToDate(calendar);

    public IReadOnlyList<DueWorkReference> PreviewDueWork(LogicalTick instant)
    {
        List<DueWorkReference> result = new List<DueWorkReference>();
        if (agenda.TryGetValue(instant.Value, out List<ScheduledDueWork> items))
        {
            foreach (ScheduledDueWork item in items) result.Add(item.Reference);
            result.Sort(CompareReferences);
        }
        return result.AsReadOnly();
    }

    public bool IsDue(LogicalTick instant, string dueWorkId)
    {
        if (dueWorkId == null || !agenda.TryGetValue(instant.Value, out List<ScheduledDueWork> items)) return false;
        foreach (ScheduledDueWork item in items) if (item.Reference.DueWorkId == dueWorkId) return true;
        return false;
    }

    /// <summary>Publishes an owner fact into the rebuildable agenda index.</summary>
    public bool TryIndexOwnerFact(DueWorkReference reference, out TimelineFailure failure)
    {
        if (reference == null) throw new ArgumentNullException(nameof(reference));
        if (dispatching) { failure = TimelineFailure.DispatchFailed; return false; }
        if (!CanIndex(reference, identities, now, out failure)) return false;
        long sequence;
        try { sequence = checked(causalSequence + 1L); }
        catch (OverflowException) { failure = TimelineFailure.Overflow; return false; }
        const int wave = 0;
        string identity = Identity(reference);
        identities.Add(identity);
        AddToAgenda(new ScheduledDueWork(reference, wave, sequence));
        causalSequence = sequence;
        failure = TimelineFailure.None;
        return true;
    }

    public bool TryAdvanceTo(LogicalTick target, out TimelineFailure failure)
    {
        if (advancing) { failure = TimelineFailure.ReentrantAdvance; return false; }
        if (target.Value < now) { failure = TimelineFailure.TargetBeforeNow; return false; }
        advancing = true;
        try
        {
            int dispatchesAtInstant = 0;
            while (true)
            {
                long nextBoundary = dailyBoundaryOperation == null ? long.MaxValue : NextBoundaryAfter(now);
                long nextDue = agenda.Count > 0 ? FirstKey() : long.MaxValue;
                long next = Math.Min(target.Value, Math.Min(nextBoundary, nextDue));
                if (next < now) { failure = TimelineFailure.Overflow; return false; }
                bool crossedBoundary = next == nextBoundary && nextBoundary <= target.Value
                    && next % LogicalTick.TicksPerDay == 0L;
                now = next;
                long currentDay = now / LogicalTick.TicksPerDay;
                bool pendingBoundary = dailyBoundaryOperation != null
                    && now % LogicalTick.TicksPerDay == 0L
                    && currentDay > lastCompletedBoundaryDay;
                if (crossedBoundary || pendingBoundary)
                {
                    try { dailyBoundaryOperation?.Invoke(new DailyBoundaryOperation(currentDay)); }
                    catch (Exception) { failure = TimelineFailure.DailyBoundaryFailed; return false; }
                    lastCompletedBoundaryDay = currentDay;
                    dispatchesAtInstant++;
                }
                if (agenda.TryGetValue(now, out List<ScheduledDueWork> items))
                {
                    items.Sort(CompareWork);
                    while (items.Count > 0)
                    {
                        if (dispatchesAtInstant >= maxDispatchesPerInstant)
                        { failure = TimelineFailure.InstantWorkLimitExceeded; return false; }
                        ScheduledDueWork scheduled = items[0];
                        DueWorkReference item = scheduled.Reference;
                        if (owner == null || !owner.IsCurrent(item))
                        { items.RemoveAt(0); failure = TimelineFailure.StaleWork; return false; }
                        if (!owner.TryPrepare(item, out IDueWorkCommit prepared, out failure) || prepared == null)
                        { if (failure == TimelineFailure.None) failure = TimelineFailure.DispatchFailed; return false; }
                        IReadOnlyList<DueWorkReference> generated = prepared.NewOwnerFacts ?? Array.Empty<DueWorkReference>();
                        List<ScheduledDueWork> staged = new List<ScheduledDueWork>(generated.Count);
                        HashSet<string> stagedIdentities = new HashSet<string>(StringComparer.Ordinal);
                        long nextSequence = causalSequence;
                        try
                        {
                            foreach (DueWorkReference generatedFact in generated)
                            {
                                if (!CanIndex(generatedFact, identities, now, out failure)
                                    || !stagedIdentities.Add(Identity(generatedFact))) return false;
                                nextSequence = checked(nextSequence + 1L);
                                int wave = generatedFact.DueAt.Value == now ? checked(scheduled.Wave + 1) : 0;
                                staged.Add(new ScheduledDueWork(generatedFact, wave, nextSequence));
                            }
                        }
                        catch (OverflowException) { failure = TimelineFailure.Overflow; return false; }
                        dispatching = true;
                        bool committed;
                        try { committed = prepared.TryCommit(out failure); }
                        catch (Exception) { failure = TimelineFailure.DispatchFailed; return false; }
                        finally { dispatching = false; }
                        if (!committed)
                        { if (failure == TimelineFailure.None) failure = TimelineFailure.DispatchFailed; return false; }
                        items.Remove(scheduled);
                        foreach (ScheduledDueWork generatedWork in staged)
                        {
                            identities.Add(Identity(generatedWork.Reference));
                            AddToAgenda(generatedWork);
                        }
                        causalSequence = nextSequence;
                        dispatchesAtInstant++;
                    }
                    agenda.Remove(now);
                }
                if (now == target.Value) break;
                dispatchesAtInstant = 0;
            }
            failure = TimelineFailure.None;
            return true;
        }
        catch (OverflowException) { failure = TimelineFailure.Overflow; return false; }
        finally { advancing = false; }
    }

    private long FirstKey()
    {
        foreach (long key in agenda.Keys) return key;
        throw new InvalidOperationException("Agenda is empty.");
    }

    private static long NextBoundaryAfter(long instant)
    {
        long day = instant / LogicalTick.TicksPerDay;
        try { return checked(checked(day + 1L) * LogicalTick.TicksPerDay); }
        catch (OverflowException) { return long.MaxValue; }
    }

    private static bool CanIndex(DueWorkReference reference, HashSet<string> existing, long instant,
        out TimelineFailure failure)
    {
        if (reference == null) throw new ArgumentNullException(nameof(reference));
        if (reference.DueAt.Value < instant) { failure = TimelineFailure.TargetBeforeNow; return false; }
        if (reference.Kind != DueWorkKind.DomainOperation) { failure = TimelineFailure.UnknownWorkKind; return false; }
        if (existing.Contains(Identity(reference))) { failure = TimelineFailure.StaleWork; return false; }
        failure = TimelineFailure.None;
        return true;
    }

    private static string Identity(DueWorkReference reference) => reference.OwnerId + "\n" + reference.DueWorkId
        + "\n" + reference.Revision + "\n" + reference.OccurrenceSequence;

    private void AddToAgenda(ScheduledDueWork item)
    {
        if (!agenda.TryGetValue(item.Reference.DueAt.Value, out List<ScheduledDueWork> list))
            agenda.Add(item.Reference.DueAt.Value, list = new List<ScheduledDueWork>());
        list.Add(item);
        list.Sort(CompareWork);
    }

    private static int CompareWork(ScheduledDueWork left, ScheduledDueWork right)
    {
        int value = left.Wave.CompareTo(right.Wave);
        if (value != 0) return value;
        value = string.CompareOrdinal(left.Reference.OwnerId, right.Reference.OwnerId);
        if (value != 0) return value;
        value = string.CompareOrdinal(left.Reference.DueWorkId, right.Reference.DueWorkId);
        if (value != 0) return value;
        value = left.Reference.OccurrenceSequence.CompareTo(right.Reference.OccurrenceSequence);
        return value != 0 ? value : left.CausalSequence.CompareTo(right.CausalSequence);
    }

    private static int CompareReferences(DueWorkReference left, DueWorkReference right)
    {
        int value = string.CompareOrdinal(left.OwnerId, right.OwnerId);
        if (value != 0) return value;
        value = string.CompareOrdinal(left.DueWorkId, right.DueWorkId);
        return value != 0 ? value : left.OccurrenceSequence.CompareTo(right.OccurrenceSequence);
    }

    private sealed class ScheduledDueWork
    {
        public DueWorkReference Reference { get; }
        public int Wave { get; }
        public long CausalSequence { get; }
        public ScheduledDueWork(DueWorkReference reference, int wave, long causalSequence)
        { Reference = reference; Wave = wave; CausalSequence = causalSequence; }
    }
}
