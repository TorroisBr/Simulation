using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>A nonnegative logical instant in millisecond-sized simulation quanta.</summary>
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
    DailyBoundaryFailed = 8, InputNotSealed = 9, LateInput = 10,
    DuplicateInputSequence = 11, DuplicateWorkSequence = 12
}

/// <summary>Closed descriptor kinds currently understood by the timeline.</summary>
public enum DueWorkKind { DomainOperation = 1 }

/// <summary>Stable identity for an owner's operation at one crossed absolute day boundary.</summary>
public sealed class DailyBoundaryOperation
{
    public string WorldId { get; }
    public string ProfileId { get; }
    public long AbsoluteDay { get; }
    public LogicalTick DueAt { get; }
    public string OccurrenceId => SpatialStableKey.Encode(WorldId, ProfileId,
        AbsoluteDay.ToString(CultureInfo.InvariantCulture));

    public DailyBoundaryOperation(string worldId, string profileId, long absoluteDay)
    {
        if (string.IsNullOrWhiteSpace(worldId)) throw new ArgumentException("World identity is required.", nameof(worldId));
        if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("Profile identity is required.", nameof(profileId));
        if (absoluteDay <= 0L) throw new ArgumentOutOfRangeException(nameof(absoluteDay));
        WorldId = worldId; ProfileId = profileId;
        AbsoluteDay = absoluteDay;
        DueAt = new LogicalTick(checked(absoluteDay * LogicalTick.TicksPerDay));
    }
}

/// <summary>Stable, data-only reference to owner-held causal work.</summary>
public sealed class DueWorkReference
{
    public string OwnerId { get; }
    public string DueWorkId { get; }
    /// <summary>Domain-owned instance identity; independent of any participant identity/cardinality.</summary>
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

/// <summary>Persistable command/input identity captured by the normal game input authority.</summary>
public sealed class TimelineInputReference
{
    public long Sequence { get; }
    public string InputId { get; }
    public string CommandKind { get; }
    public string CommandData { get; }
    public LogicalTick TargetInstant { get; }

    public TimelineInputReference(long sequence, string inputId, string commandKind, string commandData, LogicalTick targetInstant)
    {
        if (sequence < 0L) throw new ArgumentOutOfRangeException(nameof(sequence));
        if (string.IsNullOrWhiteSpace(inputId)) throw new ArgumentException("Input identity is required.", nameof(inputId));
        if (string.IsNullOrWhiteSpace(commandKind)) throw new ArgumentException("Command kind is required.", nameof(commandKind));
        Sequence = sequence; InputId = inputId; CommandKind = commandKind;
        CommandData = commandData ?? string.Empty; TargetInstant = targetInstant;
    }
}

public interface IDueWorkOwner
{
    bool IsCurrent(DueWorkReference reference);
    bool TryPrepare(DueWorkReference reference, out IDueWorkCommit prepared, out TimelineFailure failure);
}

public interface IDueWorkCommit
{
    IReadOnlyList<DueWorkReference> NewOwnerFacts { get; }
    bool TryCommit(out TimelineFailure failure);
}

/// <summary>Input owner applies a captured command through normal action/domain semantics.</summary>
public interface ITimelineInputOwner
{
    bool TryPrepare(TimelineInputReference input, out ITimelineInputCommit prepared, out TimelineFailure failure);
}

/// <summary>Stages command effects and any due facts; commit publishes them atomically.</summary>
public interface ITimelineInputCommit
{
    IReadOnlyList<DueWorkReference> NewOwnerFacts { get; }
    bool TryCommit(out TimelineFailure failure);
}

/// <summary>Typed owner boundary; the owner commits effects and the stable occurrence identity atomically.</summary>
public interface IDayBoundaryOwner
{
    bool TryPrepare(DailyBoundaryOperation operation, out IDayBoundaryCommit prepared, out TimelineFailure failure);
}

public interface IDayBoundaryCommit
{
    bool TryCommit(out TimelineFailure failure);
}

/// <summary>
/// World-local logical clock and rebuildable due-work index. Domain owners and the input authority
/// retain facts; this object only orders stable references and delegates mutation to prepared owners.
/// </summary>
public sealed class SimulationTimeline
{
    public const int DefaultMaxDispatchesPerInstant = 100000;
    private readonly SimulationCalendar calendar;
    private readonly IDueWorkOwner dueWorkOwner;
    private readonly ITimelineInputOwner inputOwner;
    private readonly IDayBoundaryOwner boundaryOwner;
    private readonly string worldId;
    private readonly string profileId;
    private readonly int maxDispatchesPerInstant;
    private readonly SortedDictionary<long, List<ScheduledDueWork>> agenda = new SortedDictionary<long, List<ScheduledDueWork>>();
    private readonly SortedDictionary<long, List<TimelineInputReference>> inputs = new SortedDictionary<long, List<TimelineInputReference>>();
    private readonly HashSet<string> workIdentities = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<long> acceptedInputSequences = new HashSet<long>();
    private long now;
    private long causalSequence;
    private long sealedThrough = -1L;
    private long pendingBoundaryDay = -1L;
    private bool advancing;
    private bool dispatching;

    public SimulationTimeline(SimulationCalendar calendar, LogicalTick initialInstant,
        IDueWorkOwner dueWorkOwner = null, ITimelineInputOwner inputOwner = null,
        IDayBoundaryOwner boundaryOwner = null, string worldId = null, string profileId = "default",
        int maxDispatchesPerInstant = DefaultMaxDispatchesPerInstant, long? pendingBoundaryDay = null)
    {
        this.calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        if (maxDispatchesPerInstant <= 0) throw new ArgumentOutOfRangeException(nameof(maxDispatchesPerInstant));
        if (boundaryOwner != null && string.IsNullOrWhiteSpace(worldId)) throw new ArgumentException("World identity is required for boundary work.", nameof(worldId));
        if (boundaryOwner != null && string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("Profile identity is required for boundary work.", nameof(profileId));
        this.dueWorkOwner = dueWorkOwner; this.inputOwner = inputOwner; this.boundaryOwner = boundaryOwner;
        this.worldId = worldId; this.profileId = profileId; this.maxDispatchesPerInstant = maxDispatchesPerInstant;
        now = initialInstant.Value;
        if (pendingBoundaryDay.HasValue && (boundaryOwner == null || pendingBoundaryDay.Value <= 0L
            || initialInstant.TickOfDay != 0L || pendingBoundaryDay.Value != initialInstant.AbsoluteDay))
            throw new ArgumentException("A restored pending boundary must match the initial boundary instant and have an owner.", nameof(pendingBoundaryDay));
        this.pendingBoundaryDay = pendingBoundaryDay ?? -1L;
        calendar.GetDate(initialInstant.AbsoluteDay);
    }

    public LogicalTick CurrentInstant => new LogicalTick(now);
    public SimulationDate CurrentDate => CurrentInstant.ToDate(calendar);
    public long CausalSequence => causalSequence;
    public long? PendingBoundaryDay => pendingBoundaryDay < 0L ? (long?)null : pendingBoundaryDay;
    public LogicalTick? NextDueInstant
    {
        get
        {
            long due = agenda.Count == 0 ? long.MaxValue : FirstKey(agenda);
            long input = inputs.Count == 0 ? long.MaxValue : FirstKey(inputs);
            long next = Math.Min(due, input);
            return next == long.MaxValue ? (LogicalTick?)null : new LogicalTick(next);
        }
    }
    public LogicalTick? InputsSealedThrough => sealedThrough < 0L ? (LogicalTick?)null : new LogicalTick(sealedThrough);
    public SimulationDate GetCalendarProjection(LogicalTick instant) => instant.ToDate(calendar);

    public bool TryAcceptInput(TimelineInputReference input, out TimelineFailure failure)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (dispatching || input.TargetInstant.Value < now || input.TargetInstant.Value <= sealedThrough)
        { failure = TimelineFailure.LateInput; return false; }
        if (!acceptedInputSequences.Add(input.Sequence)) { failure = TimelineFailure.DuplicateInputSequence; return false; }
        if (!inputs.TryGetValue(input.TargetInstant.Value, out List<TimelineInputReference> atInstant))
            inputs.Add(input.TargetInstant.Value, atInstant = new List<TimelineInputReference>());
        atInstant.Add(input);
        atInstant.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Closes the input prefix so no later command can rewrite any instant through this tick.</summary>
    public bool TrySealInputsThrough(LogicalTick instant, out TimelineFailure failure)
    {
        if (dispatching || instant.Value < sealedThrough || instant.Value < now)
        { failure = TimelineFailure.LateInput; return false; }
        sealedThrough = instant.Value;
        failure = TimelineFailure.None;
        return true;
    }

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

    public IReadOnlyList<TimelineInputReference> PreviewInputs(LogicalTick instant)
    {
        if (!inputs.TryGetValue(instant.Value, out List<TimelineInputReference> atInstant))
            return Array.Empty<TimelineInputReference>();
        return atInstant.AsReadOnly();
    }

    public bool IsDue(LogicalTick instant, string dueWorkId)
    {
        if (dueWorkId == null || !agenda.TryGetValue(instant.Value, out List<ScheduledDueWork> items)) return false;
        foreach (ScheduledDueWork item in items) if (item.Reference.DueWorkId == dueWorkId) return true;
        return false;
    }

    public bool TryIndexOwnerFact(DueWorkReference reference, out TimelineFailure failure)
    {
        if (reference == null) throw new ArgumentNullException(nameof(reference));
        if (dispatching) { failure = TimelineFailure.DispatchFailed; return false; }
        if (reference.DueAt.Value <= sealedThrough) { failure = TimelineFailure.LateInput; return false; }
        if (!CanIndex(reference, now, out failure)) return false;
        if (agenda.TryGetValue(reference.DueAt.Value, out List<ScheduledDueWork> sameInstant))
        {
            foreach (ScheduledDueWork existing in sameInstant)
                if (SameStableSequence(existing.Reference, reference))
                { failure = TimelineFailure.DuplicateWorkSequence; return false; }
        }
        if (workIdentities.Contains(Identity(reference))) { failure = TimelineFailure.StaleWork; return false; }
        long sequence;
        try { sequence = checked(causalSequence + 1L); }
        catch (OverflowException) { failure = TimelineFailure.Overflow; return false; }
        workIdentities.Add(Identity(reference));
        AddToAgenda(new ScheduledDueWork(reference, 0, sequence));
        causalSequence = sequence;
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Stages an owner's pending facts, commits its authority, then publishes the derived index atomically.</summary>
    public bool TryCommitOwnerFacts(IReadOnlyList<DueWorkReference> facts, Func<TimelineFailure> commitOwner, out TimelineFailure failure)
    {
        if (facts == null) throw new ArgumentNullException(nameof(facts));
        if (commitOwner == null) throw new ArgumentNullException(nameof(commitOwner));
        if (dispatching || advancing) { failure = TimelineFailure.DispatchFailed; return false; }
        List<ScheduledDueWork> staged = new List<ScheduledDueWork>(facts.Count);
        HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
        long nextSequence = causalSequence;
        try
        {
            foreach (DueWorkReference fact in facts)
            {
                if (!CanIndex(fact, now, out failure)) return false;
                if (fact.DueAt.Value <= sealedThrough) { failure = TimelineFailure.LateInput; return false; }
                string identity = Identity(fact);
                if (workIdentities.Contains(identity) || !identities.Add(identity)) { failure = TimelineFailure.StaleWork; return false; }
                if (agenda.TryGetValue(fact.DueAt.Value, out List<ScheduledDueWork> sameInstant))
                    foreach (ScheduledDueWork existing in sameInstant)
                        if (SameStableSequence(existing.Reference, fact)) { failure = TimelineFailure.DuplicateWorkSequence; return false; }
                foreach (ScheduledDueWork other in staged)
                    if (other.Reference.DueAt == fact.DueAt && SameStableSequence(other.Reference, fact))
                    { failure = TimelineFailure.DuplicateWorkSequence; return false; }
                nextSequence = checked(nextSequence + 1L);
                staged.Add(new ScheduledDueWork(fact, 0, nextSequence));
            }
        }
        catch (OverflowException) { failure = TimelineFailure.Overflow; return false; }
        try { failure = commitOwner(); }
        catch (Exception) { failure = TimelineFailure.DispatchFailed; return false; }
        if (failure != TimelineFailure.None) return false;
        PublishOwnerFacts(staged, nextSequence);
        return true;
    }

    public bool TryAdvanceTo(LogicalTick target, out TimelineFailure failure)
    {
        if (advancing) { failure = TimelineFailure.ReentrantAdvance; return false; }
        if (target.Value < now) { failure = TimelineFailure.TargetBeforeNow; return false; }
        if (sealedThrough < target.Value) { failure = TimelineFailure.InputNotSealed; return false; }
        advancing = true;
        try
        {
            long dispatchCountInstant = now;
            int dispatchesAtInstant = 0;
            while (true)
            {
                long nextBoundary = boundaryOwner == null ? long.MaxValue
                    : pendingBoundaryDay >= 0L ? now : NextBoundaryAfter(now);
                long nextWork = agenda.Count == 0 ? long.MaxValue : FirstKey(agenda);
                long nextInput = inputs.Count == 0 ? long.MaxValue : FirstKey(inputs);
                long next = Math.Min(target.Value, Math.Min(nextBoundary, Math.Min(nextWork, nextInput)));
                if (next < now) { failure = TimelineFailure.Overflow; return false; }
                now = next;
                if (now != dispatchCountInstant)
                { dispatchCountInstant = now; dispatchesAtInstant = 0; }
                if (boundaryOwner != null && pendingBoundaryDay < 0L && now == nextBoundary
                    && now > 0L && now % LogicalTick.TicksPerDay == 0L)
                    pendingBoundaryDay = now / LogicalTick.TicksPerDay;

                // Sealed inputs always precede both the boundary owner and ordinary work at this instant.
                if (inputs.TryGetValue(now, out List<TimelineInputReference> atInstant))
                {
                    while (atInstant.Count > 0)
                    {
                        if (dispatchesAtInstant >= maxDispatchesPerInstant)
                        { failure = TimelineFailure.InstantWorkLimitExceeded; return false; }
                        TimelineInputReference input = atInstant[0];
                        if (inputOwner == null)
                        { failure = TimelineFailure.DispatchFailed; return false; }
                        if (!inputOwner.TryPrepare(input, out ITimelineInputCommit inputCommit, out failure) || inputCommit == null)
                        { if (failure == TimelineFailure.None) failure = TimelineFailure.DispatchFailed; return false; }
                        IReadOnlyList<DueWorkReference> inputFacts = inputCommit.NewOwnerFacts ?? Array.Empty<DueWorkReference>();
                        if (!TryStageOwnerFacts(inputFacts, 0, out List<ScheduledDueWork> stagedInputFacts,
                            out long inputSequence, out failure)) return false;
                        dispatching = true;
                        bool inputCommitted;
                        try { inputCommitted = inputCommit.TryCommit(out failure); }
                        catch (Exception) { failure = TimelineFailure.DispatchFailed; return false; }
                        finally { dispatching = false; }
                        if (!inputCommitted) { if (failure == TimelineFailure.None) failure = TimelineFailure.DispatchFailed; return false; }
                        atInstant.RemoveAt(0);
                        PublishOwnerFacts(stagedInputFacts, inputSequence);
                        dispatchesAtInstant++;
                    }
                    inputs.Remove(now);
                }

                // Boundary owners are idempotent by (profile, absolute day), including retry after commit.
                if (boundaryOwner != null && pendingBoundaryDay >= 0L)
                {
                    if (dispatchesAtInstant >= maxDispatchesPerInstant)
                    { failure = TimelineFailure.InstantWorkLimitExceeded; return false; }
                    DailyBoundaryOperation operation = new DailyBoundaryOperation(worldId, profileId, pendingBoundaryDay);
                    if (!boundaryOwner.TryPrepare(operation, out IDayBoundaryCommit boundaryCommit, out failure) || boundaryCommit == null)
                    { if (failure == TimelineFailure.None) failure = TimelineFailure.DailyBoundaryFailed; return false; }
                    dispatching = true;
                    bool boundaryCommitted;
                    try { boundaryCommitted = boundaryCommit.TryCommit(out failure); }
                    catch (Exception) { failure = TimelineFailure.DailyBoundaryFailed; return false; }
                    finally { dispatching = false; }
                    if (!boundaryCommitted) { if (failure == TimelineFailure.None) failure = TimelineFailure.DailyBoundaryFailed; return false; }
                    pendingBoundaryDay = -1L;
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
                        if (dueWorkOwner == null || !dueWorkOwner.IsCurrent(item))
                        {
                            items.RemoveAt(0);
                            if (items.Count == 0) agenda.Remove(now);
                            // Obsolete owner references are inert index nodes; continue to the next causal item.
                            dispatchesAtInstant++;
                            continue;
                        }
                        if (!dueWorkOwner.TryPrepare(item, out IDueWorkCommit prepared, out failure) || prepared == null)
                        { if (failure == TimelineFailure.None) failure = TimelineFailure.DispatchFailed; return false; }
                        IReadOnlyList<DueWorkReference> generated = prepared.NewOwnerFacts ?? Array.Empty<DueWorkReference>();
                        if (!TryStageOwnerFacts(generated, scheduled.Wave + 1,
                            out List<ScheduledDueWork> staged, out long nextSequence, out failure)) return false;
                        dispatching = true;
                        bool committed;
                        try { committed = prepared.TryCommit(out failure); }
                        catch (Exception) { failure = TimelineFailure.DispatchFailed; return false; }
                        finally { dispatching = false; }
                        if (!committed) { if (failure == TimelineFailure.None) failure = TimelineFailure.DispatchFailed; return false; }
                        items.Remove(scheduled);
                        PublishOwnerFacts(staged, nextSequence);
                        dispatchesAtInstant++;
                    }
                    agenda.Remove(now);
                }

                if (now == target.Value) break;
            }
            failure = TimelineFailure.None;
            return true;
        }
        catch (OverflowException) { failure = TimelineFailure.Overflow; return false; }
        finally { advancing = false; }
    }

    private static bool CanIndex(DueWorkReference reference, long instant, out TimelineFailure failure)
    {
        if (reference == null) throw new ArgumentNullException(nameof(reference));
        if (reference.DueAt.Value < instant) { failure = TimelineFailure.TargetBeforeNow; return false; }
        if (reference.Kind != DueWorkKind.DomainOperation) { failure = TimelineFailure.UnknownWorkKind; return false; }
        failure = TimelineFailure.None;
        return true;
    }

    private bool TryStageOwnerFacts(IReadOnlyList<DueWorkReference> facts, int sameInstantWave,
        out List<ScheduledDueWork> staged, out long nextSequence, out TimelineFailure failure)
    {
        staged = new List<ScheduledDueWork>(facts.Count);
        nextSequence = causalSequence;
        HashSet<string> stagedIdentities = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (DueWorkReference fact in facts)
            {
                if (!CanIndex(fact, now, out failure)) return false;
                string identity = Identity(fact);
                if (workIdentities.Contains(identity) || !stagedIdentities.Add(identity))
                { failure = TimelineFailure.StaleWork; return false; }
                if (agenda.TryGetValue(fact.DueAt.Value, out List<ScheduledDueWork> sameInstant))
                    foreach (ScheduledDueWork existing in sameInstant)
                        if (SameStableSequence(existing.Reference, fact))
                        { failure = TimelineFailure.DuplicateWorkSequence; return false; }
                foreach (ScheduledDueWork existing in staged)
                    if (existing.Reference.DueAt == fact.DueAt && SameStableSequence(existing.Reference, fact))
                    { failure = TimelineFailure.DuplicateWorkSequence; return false; }
                nextSequence = checked(nextSequence + 1L);
                int wave = fact.DueAt.Value == now ? sameInstantWave : 0;
                staged.Add(new ScheduledDueWork(fact, wave, nextSequence));
            }
        }
        catch (OverflowException) { failure = TimelineFailure.Overflow; return false; }
        failure = TimelineFailure.None;
        return true;
    }

    private void PublishOwnerFacts(List<ScheduledDueWork> facts, long nextSequence)
    {
        foreach (ScheduledDueWork fact in facts)
        {
            workIdentities.Add(Identity(fact.Reference));
            AddToAgenda(fact);
        }
        causalSequence = nextSequence;
    }

    private static bool SameStableSequence(DueWorkReference left, DueWorkReference right) =>
        left.OwnerId == right.OwnerId && left.DueWorkId == right.DueWorkId
        && left.OccurrenceSequence == right.OccurrenceSequence;

    private static string Identity(DueWorkReference reference) => SpatialStableKey.Encode(
        reference.OwnerId,
        reference.DueWorkId,
        reference.Revision.ToString(CultureInfo.InvariantCulture),
        reference.OccurrenceSequence.ToString(CultureInfo.InvariantCulture));

    private static long FirstKey<T>(SortedDictionary<long, T> dictionary)
    {
        foreach (long key in dictionary.Keys) return key;
        throw new InvalidOperationException("Ordered collection is empty.");
    }

    private static long NextBoundaryAfter(long instant)
    {
        long day = instant / LogicalTick.TicksPerDay;
        try { return checked(checked(day + 1L) * LogicalTick.TicksPerDay); }
        catch (OverflowException) { return long.MaxValue; }
    }

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
        return value;
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
