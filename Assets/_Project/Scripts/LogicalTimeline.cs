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
    DuplicateInputSequence = 11, DuplicateWorkSequence = 12,
    ContinuationPending = 13, ContinuationFailed = 14, PublicationFailed = 15
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

/// <summary>Optional guard for owners whose facts may be dispatched by exactly one composed timeline.</summary>
public interface ITimelineBoundDueWorkOwner
{
    bool IsBoundToTimeline(SimulationTimeline timeline);
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

/// <summary>One immutable, data-only operation selected when a boundary is activated.</summary>
public sealed class BoundaryContinuationStep
{
    public int Ordinal { get; }
    public string StepId { get; }
    public string OwnerId { get; }
    public string OperationKind { get; }
    public string OperationVersion { get; }
    public string OwnerRevision { get; }
    public string Payload { get; }
    public string PersonId { get; }
    public string Disposition { get; }

    public BoundaryContinuationStep(int ordinal, string stepId, string ownerId, string operationKind,
        string operationVersion, string ownerRevision, string payload, string personId = null,
        string disposition = "included")
    {
        if (ordinal < 0) throw new ArgumentOutOfRangeException(nameof(ordinal));
        if (string.IsNullOrWhiteSpace(stepId) || string.IsNullOrWhiteSpace(ownerId)
            || string.IsNullOrWhiteSpace(operationKind) || string.IsNullOrWhiteSpace(operationVersion))
            throw new ArgumentException("Step, owner, operation kind, and version identities are required.");
        if (string.IsNullOrWhiteSpace(disposition)) throw new ArgumentException("A frozen disposition is required.", nameof(disposition));
        Ordinal = ordinal; StepId = stepId; OwnerId = ownerId; OperationKind = operationKind;
        OperationVersion = operationVersion; OwnerRevision = ownerRevision ?? string.Empty;
        Payload = payload ?? string.Empty; PersonId = personId ?? string.Empty; Disposition = disposition;
    }
}

/// <summary>Frozen activation input; step order and roster cannot be regenerated on retry.</summary>
public sealed class BoundaryContinuationManifest
{
    private readonly IReadOnlyList<BoundaryContinuationStep> steps;
    public string BoundaryOccurrenceId { get; }
    public string ContinuationId { get; }
    public string SubphaseKind { get; }
    public string SubphaseVersion { get; }
    public string WorldId { get; }
    public string ProfileId { get; }
    public long AbsoluteDay { get; }
    public string ConfigurationIdentity { get; }
    public string ContentIdentity { get; }
    public IReadOnlyList<BoundaryContinuationStep> Steps => steps;
    public string GetExecutionStepIdentity(BoundaryContinuationStep step)
    {
        if (step == null || step.Ordinal >= steps.Count || !ReferenceEquals(steps[step.Ordinal], step))
            throw new ArgumentException("Step must be the frozen manifest descriptor at its ordinal.", nameof(step));
        return SpatialStableKey.Encode(ContinuationId, step.Ordinal.ToString(CultureInfo.InvariantCulture), step.StepId);
    }

    public BoundaryContinuationManifest(DailyBoundaryOperation operation, string subphaseKind,
        string subphaseVersion, string configurationIdentity, IReadOnlyList<BoundaryContinuationStep> steps,
        string contentIdentity = null)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        if (string.IsNullOrWhiteSpace(subphaseKind) || string.IsNullOrWhiteSpace(subphaseVersion))
            throw new ArgumentException("Subphase kind and version are required.");
        if (steps == null) throw new ArgumentNullException(nameof(steps));
        List<BoundaryContinuationStep> copy = new List<BoundaryContinuationStep>(steps.Count);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < steps.Count; i++)
        {
            BoundaryContinuationStep step = steps[i] ?? throw new ArgumentException("Manifest steps cannot be null.", nameof(steps));
            if (step.Ordinal != i || !ids.Add(step.StepId)) throw new ArgumentException("Manifest ordinals must be contiguous and step identities unique.", nameof(steps));
            copy.Add(step);
        }
        BoundaryOccurrenceId = operation.OccurrenceId;
        ContinuationId = SpatialStableKey.Encode(BoundaryOccurrenceId, subphaseKind, subphaseVersion);
        SubphaseKind = subphaseKind; SubphaseVersion = subphaseVersion;
        WorldId = operation.WorldId; ProfileId = operation.ProfileId; AbsoluteDay = operation.AbsoluteDay;
        ConfigurationIdentity = configurationIdentity ?? string.Empty;
        ContentIdentity = contentIdentity ?? string.Empty;
        this.steps = copy.AsReadOnly();
    }
}

public sealed class BoundaryPublishedFact
{
    public DueWorkReference Fact { get; }
    public long CausalSequence { get; }
    public BoundaryPublishedFact(DueWorkReference fact, long causalSequence)
    {
        Fact = fact ?? throw new ArgumentNullException(nameof(fact));
        if (causalSequence < 0) throw new ArgumentOutOfRangeException(nameof(causalSequence));
        CausalSequence = causalSequence;
    }
}

/// <summary>Owner-held, reconstruction-safe continuation state.</summary>
public sealed class BoundaryContinuationState
{
    public BoundaryContinuationManifest Manifest { get; }
    public int NextStepOrdinal { get; }
    public bool IsComplete { get; }
    public bool TimelineFactsPublished { get; }
    public IReadOnlyList<BoundaryPublishedFact> PublishedFacts { get; }
    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts { get; }
    public IReadOnlyList<string> RetainedSourceSignals { get; }
    public bool SignalsHandedOff { get; }

    public BoundaryContinuationState(BoundaryContinuationManifest manifest, int nextStepOrdinal, bool isComplete,
        bool timelineFactsPublished, IReadOnlyList<BoundaryPublishedFact> publishedFacts,
        IReadOnlyList<DueWorkReference> retainedTimelineFacts, IReadOnlyList<string> retainedSourceSignals,
        bool signalsHandedOff)
    {
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        if (nextStepOrdinal < 0 || nextStepOrdinal > manifest.Steps.Count) throw new ArgumentOutOfRangeException(nameof(nextStepOrdinal));
        if (isComplete && nextStepOrdinal != manifest.Steps.Count) throw new ArgumentException("Completed continuation must have all steps resolved.");
        NextStepOrdinal = nextStepOrdinal; IsComplete = isComplete; TimelineFactsPublished = timelineFactsPublished;
        PublishedFacts = Copy(publishedFacts); RetainedTimelineFacts = Copy(retainedTimelineFacts);
        RetainedSourceSignals = Copy(retainedSourceSignals); SignalsHandedOff = signalsHandedOff;
    }
    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source) =>
        source == null ? Array.Empty<T>() : new List<T>(source).AsReadOnly();
}

public interface IBoundaryActivationCommit
{
    BoundaryContinuationManifest Manifest { get; }
    bool TryCommit(out TimelineFailure failure);
}

public interface IBoundaryContinuationStepCommit
{
    IReadOnlyList<DueWorkReference> RetainedTimelineFacts { get; }
    IReadOnlyList<string> RetainedSourceSignals { get; }
    bool TryCommit(out TimelineFailure failure);
}

public interface IBoundaryTimelinePublicationCommit
{
    bool TryCommit(IReadOnlyList<BoundaryPublishedFact> facts, out TimelineFailure failure);
}

/// <summary>Optional P18-A extension. Implementations retain activation, receipts, and outputs in domain stores.</summary>
public interface IResumableDayBoundaryOwner : IDayBoundaryOwner
{
    bool TryPrepareActivation(DailyBoundaryOperation operation, out IBoundaryActivationCommit prepared, out TimelineFailure failure);
    bool TryResolveContinuation(string continuationId, out BoundaryContinuationState state, out TimelineFailure failure);
    bool TryPrepareStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure);
    bool TryPrepareTimelinePublication(BoundaryContinuationManifest manifest,
        out IBoundaryTimelinePublicationCommit prepared, out TimelineFailure failure);
}

/// <summary>Optional handoff for retained P18-C source signals after the entire outer advance succeeds.</summary>
public interface IBoundarySourceSignalHandoff
{
    bool TryHandoff(BoundaryContinuationManifest manifest, IReadOnlyList<string> retainedSignals, out TimelineFailure failure);
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
    private string pendingContinuationId;
    private readonly List<string> pendingSignalHandoffIds = new List<string>();
    private bool advancing;
    private bool dispatching;
    private bool ownerCommitWindow;

    public SimulationTimeline(SimulationCalendar calendar, LogicalTick initialInstant,
        IDueWorkOwner dueWorkOwner = null, ITimelineInputOwner inputOwner = null,
        IDayBoundaryOwner boundaryOwner = null, string worldId = null, string profileId = "default",
        int maxDispatchesPerInstant = DefaultMaxDispatchesPerInstant, long? pendingBoundaryDay = null,
        string pendingContinuationId = null, IReadOnlyList<string> pendingSignalHandoffIds = null,
        long initialCausalSequence = 0L)
    {
        this.calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        if (maxDispatchesPerInstant <= 0) throw new ArgumentOutOfRangeException(nameof(maxDispatchesPerInstant));
        if (initialCausalSequence < 0L) throw new ArgumentOutOfRangeException(nameof(initialCausalSequence));
        if (boundaryOwner != null && string.IsNullOrWhiteSpace(worldId)) throw new ArgumentException("World identity is required for boundary work.", nameof(worldId));
        if (boundaryOwner != null && string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("Profile identity is required for boundary work.", nameof(profileId));
        this.dueWorkOwner = dueWorkOwner; this.inputOwner = inputOwner; this.boundaryOwner = boundaryOwner;
        this.worldId = worldId; this.profileId = profileId; this.maxDispatchesPerInstant = maxDispatchesPerInstant;
        causalSequence = initialCausalSequence;
        now = initialInstant.Value;
        if (pendingBoundaryDay.HasValue && (boundaryOwner == null || pendingBoundaryDay.Value <= 0L
            || initialInstant.TickOfDay != 0L || pendingBoundaryDay.Value != initialInstant.AbsoluteDay))
            throw new ArgumentException("A restored pending boundary must match the initial boundary instant and have an owner.", nameof(pendingBoundaryDay));
        this.pendingBoundaryDay = pendingBoundaryDay ?? -1L;
        if (pendingContinuationId != null && !(boundaryOwner is IResumableDayBoundaryOwner))
            throw new ArgumentException("A restored continuation requires a resumable boundary owner.", nameof(pendingContinuationId));
        this.pendingContinuationId = pendingContinuationId;
        if (pendingSignalHandoffIds != null)
        {
            if (!(boundaryOwner is IResumableDayBoundaryOwner)) throw new ArgumentException("Restored continuation handoffs require a resumable boundary owner.", nameof(pendingSignalHandoffIds));
            foreach (string id in pendingSignalHandoffIds)
                if (string.IsNullOrWhiteSpace(id) || this.pendingSignalHandoffIds.Contains(id)) throw new ArgumentException("Restored handoff identities must be nonempty and unique.", nameof(pendingSignalHandoffIds));
                else this.pendingSignalHandoffIds.Add(id);
        }
        calendar.GetDate(initialInstant.AbsoluteDay);
    }

    public LogicalTick CurrentInstant => new LogicalTick(now);
    public SimulationDate CurrentDate => CurrentInstant.ToDate(calendar);
    public long CausalSequence => causalSequence;
    public long? PendingBoundaryDay => pendingBoundaryDay < 0L ? (long?)null : pendingBoundaryDay;
    public string PendingContinuationId => pendingContinuationId;
    public IReadOnlyList<string> PendingSignalHandoffIds => pendingSignalHandoffIds.AsReadOnly();
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
        if (ownerCommitWindow) { failure = TimelineFailure.ReentrantAdvance; return false; }
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
        if (dispatching || ownerCommitWindow || instant.Value < sealedThrough || instant.Value < now)
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
        if (dispatching || ownerCommitWindow) { failure = TimelineFailure.DispatchFailed; return false; }
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
        if (dispatching || advancing || ownerCommitWindow) { failure = TimelineFailure.DispatchFailed; return false; }
        if (!TryStageExternalFacts(facts, out List<ScheduledDueWork> staged, out long nextSequence, out failure)) return false;
        ownerCommitWindow = true;
        try { failure = commitOwner(); }
        catch (Exception) { failure = TimelineFailure.DispatchFailed; return false; }
        finally { ownerCommitWindow = false; }
        if (failure != TimelineFailure.None) return false;
        PublishOwnerFacts(staged, nextSequence);
        return true;
    }

    /// <summary>Atomically validates and indexes an owner's complete authoritative fact set.</summary>
    public bool TryIndexOwnerFacts(IReadOnlyList<DueWorkReference> facts, out TimelineFailure failure)
    {
        if (facts == null) throw new ArgumentNullException(nameof(facts));
        if (dispatching || advancing || ownerCommitWindow) { failure = TimelineFailure.DispatchFailed; return false; }
        if (!TryStageExternalFacts(facts, out List<ScheduledDueWork> staged, out long nextSequence, out failure)) return false;
        PublishOwnerFacts(staged, nextSequence);
        return true;
    }

    private bool TryStageExternalFacts(IReadOnlyList<DueWorkReference> facts, out List<ScheduledDueWork> staged,
        out long nextSequence, out TimelineFailure failure)
    {
        staged = new List<ScheduledDueWork>(facts.Count);
        HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
        nextSequence = causalSequence;
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
        failure = TimelineFailure.None;
        return true;
    }

    public bool TryAdvanceTo(LogicalTick target, out TimelineFailure failure)
    {
        if (advancing || ownerCommitWindow) { failure = TimelineFailure.ReentrantAdvance; return false; }
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
                    if (boundaryOwner is IResumableDayBoundaryOwner resumableOwner)
                    {
                        if (!resumableOwner.TryPrepareActivation(operation, out IBoundaryActivationCommit activation, out failure) || activation == null)
                        { if (failure == TimelineFailure.None) failure = TimelineFailure.DailyBoundaryFailed; return false; }
                        if (activation.Manifest == null || activation.Manifest.BoundaryOccurrenceId != operation.OccurrenceId
                            || activation.Manifest.WorldId != worldId || activation.Manifest.ProfileId != profileId
                            || activation.Manifest.AbsoluteDay != operation.AbsoluteDay
                            || activation.Manifest.ContinuationId != SpatialStableKey.Encode(operation.OccurrenceId,
                                activation.Manifest.SubphaseKind, activation.Manifest.SubphaseVersion))
                        { failure = TimelineFailure.ContinuationFailed; return false; }
                        dispatching = true;
                        bool activationCommitted;
                        try { activationCommitted = activation.TryCommit(out failure); }
                        catch (Exception) { failure = TimelineFailure.DailyBoundaryFailed; return false; }
                        finally { dispatching = false; }
                        if (!activationCommitted || activation.Manifest == null)
                        { if (failure == TimelineFailure.None) failure = TimelineFailure.DailyBoundaryFailed; return false; }
                        pendingBoundaryDay = -1L;
                        pendingContinuationId = activation.Manifest.ContinuationId;
                        dispatchesAtInstant++;
                    }
                    else
                    {
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
                }

                // A resumable subphase is a barrier: finish and publish it before ordinary work at this instant.
                if (pendingContinuationId != null)
                {
                    if (dispatchesAtInstant >= maxDispatchesPerInstant)
                    { failure = TimelineFailure.InstantWorkLimitExceeded; return false; }
                    if (!TryResumeBoundaryContinuation(out BoundaryContinuationManifest completedManifest, out failure)) return false;
                    pendingContinuationId = null;
                    if (!pendingSignalHandoffIds.Contains(completedManifest.ContinuationId))
                        pendingSignalHandoffIds.Add(completedManifest.ContinuationId);
                    dispatchesAtInstant++;
                }

                if (!TryRestorePendingBoundaryFacts(out failure)) return false;

                if (agenda.TryGetValue(now, out List<ScheduledDueWork> items))
                {
                    items.Sort(CompareWork);
                    while (items.Count > 0)
                    {
                        if (dispatchesAtInstant >= maxDispatchesPerInstant)
                        { failure = TimelineFailure.InstantWorkLimitExceeded; return false; }
                        ScheduledDueWork scheduled = items[0];
                        DueWorkReference item = scheduled.Reference;
                        if (dueWorkOwner is ITimelineBoundDueWorkOwner boundOwner && !boundOwner.IsBoundToTimeline(this))
                        { failure = TimelineFailure.DispatchFailed; return false; }
                        if (dueWorkOwner == null || !dueWorkOwner.IsCurrent(item))
                        {
                            items.RemoveAt(0);
                            if (items.Count == 0) agenda.Remove(now);
                            // Obsolete owner references are inert index nodes; continue to the next causal item.
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
            if (!TryHandoffBoundarySignals(out failure)) return false;
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

    private bool TryResumeBoundaryContinuation(out BoundaryContinuationManifest completedManifest, out TimelineFailure failure)
    {
        completedManifest = null;
        failure = TimelineFailure.None;
        if (!(boundaryOwner is IResumableDayBoundaryOwner owner)
            || !owner.TryResolveContinuation(pendingContinuationId, out BoundaryContinuationState state, out failure)
            || state == null)
        { if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed; return false; }
        BoundaryContinuationManifest manifest = state.Manifest;
        if (manifest.ContinuationId != pendingContinuationId
            || manifest.ContinuationId != SpatialStableKey.Encode(manifest.BoundaryOccurrenceId, manifest.SubphaseKind, manifest.SubphaseVersion)
            || manifest.AbsoluteDay != now / LogicalTick.TicksPerDay || now % LogicalTick.TicksPerDay != 0L)
        { failure = TimelineFailure.ContinuationFailed; return false; }

        while (!state.IsComplete)
        {
            int ordinal = state.NextStepOrdinal;
            if (ordinal < 0 || ordinal >= manifest.Steps.Count)
            { failure = TimelineFailure.ContinuationFailed; return false; }
            BoundaryContinuationStep step = manifest.Steps[ordinal];
            if (!owner.TryPrepareStep(manifest, step, out IBoundaryContinuationStepCommit stepCommit, out failure)
                || stepCommit == null)
            { if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed; return false; }
            // Outputs must be retained with the step receipt; publication is a later timeline boundary.
            if (stepCommit.RetainedTimelineFacts == null || stepCommit.RetainedSourceSignals == null)
            { failure = TimelineFailure.ContinuationFailed; return false; }
            dispatching = true;
            bool committed;
            try { committed = stepCommit.TryCommit(out failure); }
            catch (Exception) { failure = TimelineFailure.ContinuationFailed; return false; }
            finally { dispatching = false; }
            if (!committed)
            { if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed; return false; }
            if (!owner.TryResolveContinuation(pendingContinuationId, out state, out failure) || state == null)
            { if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed; return false; }
            if (!ReferenceEquals(state.Manifest, manifest) && state.Manifest.ContinuationId != manifest.ContinuationId)
            { failure = TimelineFailure.ContinuationFailed; return false; }
        }

        if (!state.TimelineFactsPublished)
        {
            List<DueWorkReference> facts = new List<DueWorkReference>(state.RetainedTimelineFacts);
            HashSet<string> factIdentities = new HashSet<string>(StringComparer.Ordinal);
            foreach (DueWorkReference fact in facts)
                if (fact == null || !factIdentities.Add(Identity(fact)))
                { failure = TimelineFailure.PublicationFailed; return false; }
            if (!TryStageOwnerFacts(facts, 0, out List<ScheduledDueWork> staged, out long nextSequence, out failure))
            { if (failure == TimelineFailure.None) failure = TimelineFailure.PublicationFailed; return false; }
            List<BoundaryPublishedFact> receipts = staged.ConvertAll(item => new BoundaryPublishedFact(item.Reference, item.CausalSequence));
            if (!owner.TryPrepareTimelinePublication(manifest, out IBoundaryTimelinePublicationCommit publication, out failure)
                || publication == null)
            { if (failure == TimelineFailure.None) failure = TimelineFailure.PublicationFailed; return false; }
            dispatching = true;
            bool published;
            try { published = publication.TryCommit(receipts.AsReadOnly(), out failure); }
            catch (Exception) { failure = TimelineFailure.PublicationFailed; return false; }
            finally { dispatching = false; }
            if (!published)
            { if (failure == TimelineFailure.None) failure = TimelineFailure.PublicationFailed; return false; }
            PublishOwnerFacts(staged, nextSequence);
        }
        else if (!TryRestorePublishedBoundaryFacts(state.PublishedFacts, out failure)) return false;

        completedManifest = manifest;
        failure = TimelineFailure.None;
        return true;
    }

    private bool TryRestorePublishedBoundaryFacts(IReadOnlyList<BoundaryPublishedFact> facts, out TimelineFailure failure)
    {
        if (facts == null) { failure = TimelineFailure.PublicationFailed; return false; }
        HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (BoundaryPublishedFact published in facts)
        {
            if (published == null || published.Fact == null || !identities.Add(Identity(published.Fact)))
            { failure = TimelineFailure.PublicationFailed; return false; }
            if (published.Fact.DueAt.Value < now) continue;
            string identity = Identity(published.Fact);
            if (workIdentities.Contains(identity)) continue;
            if (!CanIndex(published.Fact, now, out failure)) return false;
            foreach (List<ScheduledDueWork> indexed in agenda.Values)
                foreach (ScheduledDueWork existing in indexed)
                    if (existing.CausalSequence == published.CausalSequence)
                    { failure = TimelineFailure.PublicationFailed; return false; }
            workIdentities.Add(identity);
            AddToAgenda(new ScheduledDueWork(published.Fact, 0, published.CausalSequence));
            if (published.CausalSequence > causalSequence) causalSequence = published.CausalSequence;
        }
        failure = TimelineFailure.None;
        return true;
    }

    private bool TryRestorePendingBoundaryFacts(out TimelineFailure failure)
    {
        if (pendingSignalHandoffIds.Count == 0) { failure = TimelineFailure.None; return true; }
        if (!(boundaryOwner is IResumableDayBoundaryOwner owner))
        { failure = TimelineFailure.ContinuationFailed; return false; }
        foreach (string id in pendingSignalHandoffIds)
        {
            if (!owner.TryResolveContinuation(id, out BoundaryContinuationState state, out failure)
                || state == null || !state.IsComplete || !state.TimelineFactsPublished)
            { if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed; return false; }
            if (!TryRestorePublishedBoundaryFacts(state.PublishedFacts, out failure)) return false;
        }
        failure = TimelineFailure.None;
        return true;
    }

    private bool TryHandoffBoundarySignals(out TimelineFailure failure)
    {
        failure = TimelineFailure.None;
        if (pendingSignalHandoffIds.Count == 0) { failure = TimelineFailure.None; return true; }
        if (!(boundaryOwner is IResumableDayBoundaryOwner owner))
        { failure = TimelineFailure.ContinuationFailed; return false; }
        if (!(boundaryOwner is IBoundarySourceSignalHandoff handoff))
        {
            foreach (string id in pendingSignalHandoffIds)
                if (!owner.TryResolveContinuation(id, out BoundaryContinuationState state, out failure) || state == null || state.RetainedSourceSignals.Count != 0)
                { if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed; return false; }
            pendingSignalHandoffIds.Clear(); failure = TimelineFailure.None; return true;
        }
        for (int i = 0; i < pendingSignalHandoffIds.Count;)
        {
            string id = pendingSignalHandoffIds[i];
            if (!owner.TryResolveContinuation(id, out BoundaryContinuationState state, out failure) || state == null || !state.IsComplete)
            { if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed; return false; }
            if (!state.SignalsHandedOff && !handoff.TryHandoff(state.Manifest, state.RetainedSourceSignals, out failure))
            { if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed; return false; }
            pendingSignalHandoffIds.RemoveAt(i);
        }
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
