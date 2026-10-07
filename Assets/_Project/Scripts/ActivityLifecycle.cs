using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public enum ActivityLifecycleState { Proposed = 0, Scheduled = 1, Active = 2, Completed = 3, Cancelled = 4, Interrupted = 5 }
public enum ActivityTransitionKind { Start = 1, Complete = 2, Schedule = 3, Cancel = 4, Interrupt = 5, FailedStart = 6 }
public enum ActivityFailure { None = 0, InvalidDefinition = 1, DuplicateCreation = 2, UnknownInstance = 3, InvalidState = 4, NoParticipants = 5, InvalidInterval = 6, ParticipantConflict = 7, StaleWork = 8, RevisionOverflow = 9, TimelinePublicationFailed = 10, InvalidInstant = 11, TimelineMismatch = 12 }

public interface IActivityStartValidator
{
    bool TryValidate(ActivityInstanceSnapshot instance, LogicalTick instant, out string failureDisposition);
}

/// <summary>Bounded owner seam for a domain that must publish facts with lifecycle transitions.</summary>
internal interface IActivityLifecycleTransitionParticipant
{
    bool TryPrepareStart(ActivityInstanceSnapshot instance, LogicalTick instant,
        out IActivityLifecycleTransitionCommit prepared, out string failureDisposition);
    bool TryPrepareTerminal(ActivityInstanceSnapshot instance, ActivityLifecycleState terminal,
        ActivityTransitionKind kind, LogicalTick instant, string disposition,
        out IActivityLifecycleTransitionCommit prepared);
}

internal interface IActivityLifecycleTransitionCommit
{
    bool StartAllowed { get; }
    string FailureDisposition { get; }
    bool CanCommit { get; }
    // Implementations must allocate/validate during preparation and publish prebuilt state only here.
    void CommitStarted();
    void CommitFailedStart();
    void CommitTerminal();
}

/// <summary>Optional preflight for a coordinated start that is being terminalized by a failed-start transition.</summary>
internal interface IActivityLifecycleStartFailureCommit
{
    bool CanCommitFailedStart { get; }
}

internal sealed class AcceptActivityStartValidator : IActivityStartValidator
{
    public static readonly AcceptActivityStartValidator Instance = new AcceptActivityStartValidator();
    private AcceptActivityStartValidator() { }
    public bool TryValidate(ActivityInstanceSnapshot instance, LogicalTick instant, out string failureDisposition)
    { failureDisposition = null; return true; }
}

public sealed class ActivityDefinition
{
    public string Id { get; }
    public string Version { get; }
    public ActivityDefinition(string id, string version)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Definition identity is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Definition version is required.", nameof(version));
        Id = id; Version = version;
    }
}

public sealed class ActivityParticipantCommitment
{
    public string ParticipantId { get; }
    public string ActivityInstanceId { get; }
    public LogicalTick Start { get; }
    public LogicalTick? End { get; }
    public long Revision { get; }
    internal ActivityParticipantCommitment(string participantId, string instanceId, LogicalTick start, LogicalTick? end, long revision)
    { ParticipantId = participantId; ActivityInstanceId = instanceId; Start = start; End = end; Revision = revision; }
}

public sealed class ActivityInstanceSnapshot
{
    public string Id { get; }
    public string CreationIdentity { get; }
    public string DefinitionId { get; }
    public string DefinitionVersion { get; }
    public ActivityLifecycleState State { get; }
    public long Revision { get; }
    public LogicalTick? PlannedStart { get; }
    public LogicalTick? PlannedEnd { get; }
    public LogicalTick? ActualStart { get; }
    public LogicalTick? TerminalInstant { get; }
    public string Disposition { get; }
    public IReadOnlyList<string> Participants { get; }

    internal ActivityInstanceSnapshot(ActivityInstance instance)
    {
        Id = instance.Id; CreationIdentity = instance.CreationIdentity; DefinitionId = instance.Definition.Id;
        DefinitionVersion = instance.Definition.Version; State = instance.State; Revision = instance.Revision;
        PlannedStart = instance.PlannedStart; PlannedEnd = instance.PlannedEnd; ActualStart = instance.ActualStart;
        TerminalInstant = instance.TerminalInstant; Disposition = instance.Disposition;
        Participants = Array.AsReadOnly(instance.Participants.ToArray());
    }
}

/// <summary>Immutable committed lifecycle fact. Receipts trigger reevaluation; lifecycle state remains authoritative.</summary>
public sealed class ActivityTransitionReceipt
{
    public string Id { get; }
    public long Sequence { get; }
    public string ActivityInstanceId { get; }
    public long ActivityRevision { get; }
    public ActivityTransitionKind Kind { get; }
    public LogicalTick Instant { get; }
    public IReadOnlyList<string> ParticipantIds { get; }
    public string Disposition { get; }

    internal ActivityTransitionReceipt(string worldId, long sequence, string activityInstanceId, long revision,
        ActivityTransitionKind kind, LogicalTick instant, IEnumerable<string> participants, string disposition)
    {
        Id = SpatialStableKey.Encode(worldId, "activity-transition", sequence.ToString(CultureInfo.InvariantCulture));
        Sequence = sequence; ActivityInstanceId = activityInstanceId; ActivityRevision = revision; Kind = kind;
        Instant = instant; ParticipantIds = Array.AsReadOnly(participants.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Disposition = disposition ?? string.Empty;
    }
}

internal sealed class ActivityInstance
{
    public readonly string Id, CreationIdentity;
    public readonly ActivityDefinition Definition;
    public ActivityLifecycleState State;
    public long Revision;
    public LogicalTick? PlannedStart, PlannedEnd, ActualStart, TerminalInstant;
    public string Disposition;
    public SortedSet<string> Participants = new SortedSet<string>(StringComparer.Ordinal);
    public ActivityInstance(string id, string creation, ActivityDefinition definition)
    { Id = id; CreationIdentity = creation; Definition = definition; State = ActivityLifecycleState.Proposed; }
}

/// <summary>Authoritative activity lifecycle, participant relations, commitments and due facts.</summary>
public sealed class ActivityLifecycleStore : IDueWorkOwner, ITimelineBoundDueWorkOwner
{
    public const string DueOwnerId = "activity-lifecycle";
    private Dictionary<string, ActivityInstance> instances = new Dictionary<string, ActivityInstance>(StringComparer.Ordinal);
    private Dictionary<string, string> creationIds = new Dictionary<string, string>(StringComparer.Ordinal);
    private Dictionary<string, List<ActivityParticipantCommitment>> commitments = new Dictionary<string, List<ActivityParticipantCommitment>>(StringComparer.Ordinal);
    private List<DueWorkReference> pending = new List<DueWorkReference>();
    private readonly string worldId;
    private readonly IActivityStartValidator startValidator;
    private IActivityLifecycleTransitionParticipant transitionParticipant;
    private SimulationTimeline authoritativeTimeline;
    private long nextIdentity;
    private List<ActivityTransitionReceipt> transitionReceipts = new List<ActivityTransitionReceipt>();
    private long nextTransitionSequence = 1;

    public ActivityLifecycleStore(string worldId, long nextIdentity = 0, IActivityStartValidator startValidator = null)
    {
        if (string.IsNullOrWhiteSpace(worldId)) throw new ArgumentException("World identity is required.", nameof(worldId));
        if (nextIdentity < 0) throw new ArgumentOutOfRangeException(nameof(nextIdentity));
        this.worldId = worldId; this.nextIdentity = nextIdentity; this.startValidator = startValidator ?? AcceptActivityStartValidator.Instance;
    }

    public long NextIdentity => nextIdentity;
    internal string NextProposedInstanceId => SpatialStableKey.Encode(worldId, nextIdentity.ToString(CultureInfo.InvariantCulture));
    public long NextTransitionSequence => nextTransitionSequence;
    public IReadOnlyList<DueWorkReference> PendingWork => pending.AsReadOnly();

    internal void BindTimeline(SimulationTimeline timeline)
    {
        if (timeline == null) throw new ArgumentNullException(nameof(timeline));
        if (authoritativeTimeline != null && !ReferenceEquals(authoritativeTimeline, timeline))
            throw new InvalidOperationException("Activity lifecycle is already bound to another timeline.");
        authoritativeTimeline = timeline;
    }

    public bool IsBoundToTimeline(SimulationTimeline timeline) => ReferenceEquals(authoritativeTimeline, timeline);

    internal void BindTransitionParticipant(IActivityLifecycleTransitionParticipant participant)
    {
        if (participant == null) throw new ArgumentNullException(nameof(participant));
        if (transitionParticipant != null && !ReferenceEquals(transitionParticipant, participant))
            throw new InvalidOperationException("Activity lifecycle already has a transition participant.");
        transitionParticipant = participant;
    }

    public ActivityLifecycleStore Clone()
    {
        ActivityLifecycleStore clone = new ActivityLifecycleStore(worldId, nextIdentity, startValidator);
        clone.nextTransitionSequence = nextTransitionSequence;
        clone.transitionReceipts.AddRange(transitionReceipts);
        foreach (ActivityInstance source in instances.Values)
        {
            ActivityInstance copy = new ActivityInstance(source.Id, source.CreationIdentity, source.Definition)
            {
                State = source.State, Revision = source.Revision, PlannedStart = source.PlannedStart,
                PlannedEnd = source.PlannedEnd, ActualStart = source.ActualStart,
                TerminalInstant = source.TerminalInstant, Disposition = source.Disposition
            };
            copy.Participants.UnionWith(source.Participants);
            clone.instances.Add(copy.Id, copy);
            clone.creationIds.Add(copy.CreationIdentity, copy.Id);
        }
        foreach (KeyValuePair<string, List<ActivityParticipantCommitment>> entry in commitments)
            clone.commitments.Add(entry.Key, new List<ActivityParticipantCommitment>(entry.Value));
        clone.pending.AddRange(pending);
        return clone;
    }

    /// <summary>Returns an immutable, sequence-ordered copy of receipts newer than the caller's cursor.</summary>
    public IReadOnlyList<ActivityTransitionReceipt> SnapshotTransitionReceipts(long afterSequence = 0)
    {
        if (afterSequence < 0) throw new ArgumentOutOfRangeException(nameof(afterSequence));
        return Array.AsReadOnly(transitionReceipts.Where(r => r.Sequence > afterSequence).OrderBy(r => r.Sequence).ToArray());
    }

    public bool TryPropose(ActivityDefinition definition, string creationIdentity, out ActivityInstanceSnapshot snapshot, out ActivityFailure failure)
    {
        snapshot = null;
        if (definition == null || string.IsNullOrWhiteSpace(creationIdentity)) { failure = ActivityFailure.InvalidDefinition; return false; }
        if (creationIds.TryGetValue(creationIdentity, out string existing))
        { snapshot = new ActivityInstanceSnapshot(instances[existing]); failure = ActivityFailure.DuplicateCreation; return false; }
        string id = SpatialStableKey.Encode(worldId, nextIdentity.ToString(CultureInfo.InvariantCulture));
        try { nextIdentity = checked(nextIdentity + 1); }
        catch (OverflowException) { failure = ActivityFailure.RevisionOverflow; return false; }
        ActivityInstance item = new ActivityInstance(id, creationIdentity, definition);
        instances.Add(id, item); creationIds.Add(creationIdentity, id);
        snapshot = new ActivityInstanceSnapshot(item); failure = ActivityFailure.None; return true;
    }

    /// <summary>Prepares one P18 proposal and a single P20-owned root swap in the same timeline mutation boundary.</summary>
    internal bool TryProposeWithParticipant(SimulationTimeline timeline, ActivityDefinition definition,
        string creationIdentity, Func<ActivityInstanceSnapshot, Action> prepareParticipant,
        out ActivityInstanceSnapshot snapshot, out ActivityFailure failure)
    {
        snapshot = null;
        if (timeline == null) throw new ArgumentNullException(nameof(timeline));
        if (!ReferenceEquals(authoritativeTimeline, timeline))
        { failure = ActivityFailure.TimelineMismatch; return false; }
        if (definition == null || string.IsNullOrWhiteSpace(creationIdentity))
        { failure = ActivityFailure.InvalidDefinition; return false; }
        if (creationIds.TryGetValue(creationIdentity, out string existing))
        { snapshot = new ActivityInstanceSnapshot(instances[existing]); failure = ActivityFailure.DuplicateCreation; return false; }
        string id = SpatialStableKey.Encode(worldId, nextIdentity.ToString(CultureInfo.InvariantCulture));
        long next;
        try { next = checked(nextIdentity + 1L); }
        catch (OverflowException) { failure = ActivityFailure.RevisionOverflow; return false; }
        ActivityInstance item = new ActivityInstance(id, creationIdentity, definition);
        Dictionary<string, ActivityInstance> stagedInstances = new Dictionary<string, ActivityInstance>(instances, StringComparer.Ordinal)
        { [id] = item };
        Dictionary<string, string> stagedCreationIds = new Dictionary<string, string>(creationIds, StringComparer.Ordinal)
        { [creationIdentity] = id };
        ActivityInstanceSnapshot stagedSnapshot = new ActivityInstanceSnapshot(item);
        Action commitParticipant = prepareParticipant?.Invoke(stagedSnapshot);
        bool committed = timeline.TryCommitOwnerFacts(Array.Empty<DueWorkReference>(), () =>
        {
            instances = stagedInstances;
            creationIds = stagedCreationIds;
            nextIdentity = next;
            commitParticipant?.Invoke();
            return TimelineFailure.None;
        }, out _);
        if (!committed) { failure = ActivityFailure.TimelinePublicationFailed; return false; }
        snapshot = stagedSnapshot;
        failure = ActivityFailure.None;
        return true;
    }

    public bool TryGet(string instanceId, out ActivityInstanceSnapshot snapshot)
    { snapshot = instances.TryGetValue(instanceId, out ActivityInstance item) ? new ActivityInstanceSnapshot(item) : null; return snapshot != null; }

    public IReadOnlyList<ActivityInstanceSnapshot> SnapshotInstances()
    {
        List<ActivityInstanceSnapshot> result = new List<ActivityInstanceSnapshot>();
        foreach (ActivityInstance instance in instances.Values) result.Add(new ActivityInstanceSnapshot(instance));
        result.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); return result.AsReadOnly();
    }

    public bool TrySchedule(SimulationTimeline timeline, string instanceId, LogicalTick now, LogicalTick start, LogicalTick? duration,
        IReadOnlyList<string> participantIds, out ActivityFailure failure)
        => TrySchedule(timeline, instanceId, now, start, duration, participantIds, null, out failure);

    internal bool TrySchedule(SimulationTimeline timeline, string instanceId, LogicalTick now, LogicalTick start, LogicalTick? duration,
        IReadOnlyList<string> participantIds, Action commitParticipant, out ActivityFailure failure)
    {
        if (timeline == null) throw new ArgumentNullException(nameof(timeline));
        if (!ReferenceEquals(authoritativeTimeline, timeline))
        { failure = ActivityFailure.TimelineMismatch; return false; }
        if (now != timeline.CurrentInstant) { failure = ActivityFailure.InvalidInstant; return false; }
        if (!instances.TryGetValue(instanceId, out ActivityInstance item)) { failure = ActivityFailure.UnknownInstance; return false; }
        if (item.State != ActivityLifecycleState.Proposed) { failure = ActivityFailure.InvalidState; return false; }
        if (participantIds == null || participantIds.Count == 0) { failure = ActivityFailure.NoParticipants; return false; }
        if (start.Value < now.Value || (duration.HasValue && duration.Value.Value < 0)) { failure = ActivityFailure.InvalidInterval; return false; }
        LogicalTick? end = null;
        if (duration.HasValue)
        {
            try { end = new LogicalTick(checked(start.Value + duration.Value.Value)); }
            catch (OverflowException) { failure = ActivityFailure.InvalidInterval; return false; }
        }
        SortedSet<string> participants = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string participant in participantIds)
            if (string.IsNullOrWhiteSpace(participant) || !participants.Add(participant)) { failure = ActivityFailure.ParticipantConflict; return false; }
        if (participants.Count == 0) { failure = ActivityFailure.NoParticipants; return false; }
        foreach (string participant in participants)
            if (commitments.TryGetValue(participant, out List<ActivityParticipantCommitment> current))
                foreach (ActivityParticipantCommitment commitment in current)
                    if ((!commitment.End.HasValue || start.Value < commitment.End.Value.Value)
                        && (!end.HasValue || commitment.Start.Value < end.Value.Value))
                    { failure = ActivityFailure.ParticipantConflict; return false; }
        long revision;
        try { revision = checked(item.Revision + 1); }
        catch (OverflowException) { failure = ActivityFailure.RevisionOverflow; return false; }
        List<DueWorkReference> work = new List<DueWorkReference>();
        work.Add(Reference(item, revision, ActivityTransitionKind.Start, start));
        if (end.HasValue) work.Add(Reference(item, revision, ActivityTransitionKind.Complete, end.Value));
        SortedSet<string> stagedParticipants = new SortedSet<string>(participants, StringComparer.Ordinal);
        Dictionary<string, List<ActivityParticipantCommitment>> stagedCommitments = new Dictionary<string, List<ActivityParticipantCommitment>>(commitments, StringComparer.Ordinal);
        foreach (string participant in participants)
        {
            List<ActivityParticipantCommitment> participantCommitments = stagedCommitments.TryGetValue(participant, out List<ActivityParticipantCommitment> existing)
                ? new List<ActivityParticipantCommitment>(existing) : new List<ActivityParticipantCommitment>();
            participantCommitments.Add(new ActivityParticipantCommitment(participant, item.Id, start, end, revision));
            stagedCommitments[participant] = participantCommitments;
        }
        List<DueWorkReference> stagedPending = new List<DueWorkReference>(pending);
        stagedPending.AddRange(work);
        if (!TryStageReceipt(item, revision, ActivityTransitionKind.Schedule, timeline.CurrentInstant, participants, "scheduled", out ActivityTransitionReceipt scheduledReceipt))
        { failure = ActivityFailure.RevisionOverflow; return false; }
        List<ActivityTransitionReceipt> stagedReceipts = StageReceiptHistory(scheduledReceipt);
        long stagedNextTransitionSequence = scheduledReceipt.Sequence + 1L;
        bool committed = timeline.TryCommitOwnerFacts(work, () =>
        {
            item.Participants = stagedParticipants; item.PlannedStart = start; item.PlannedEnd = end;
            item.Revision = revision; item.State = ActivityLifecycleState.Scheduled;
            commitments = stagedCommitments; pending = stagedPending; transitionReceipts = stagedReceipts;
            nextTransitionSequence = stagedNextTransitionSequence;
            commitParticipant?.Invoke(); return TimelineFailure.None;
        }, out _);
        if (!committed) { failure = ActivityFailure.TimelinePublicationFailed; return false; }
        failure = ActivityFailure.None; return true;
    }

    public bool TryRebuildTimelineIndex(SimulationTimeline timeline, out TimelineFailure failure)
    {
        if (timeline == null) throw new ArgumentNullException(nameof(timeline));
        if (!ReferenceEquals(authoritativeTimeline, timeline))
        { failure = TimelineFailure.DispatchFailed; return false; }
        List<DueWorkReference> facts = new List<DueWorkReference>(pending);
        facts.Sort((a, b) => { int c = a.DueAt.CompareTo(b.DueAt); if (c != 0) return c; c = string.CompareOrdinal(a.InstanceId, b.InstanceId); return c != 0 ? c : string.CompareOrdinal(a.DueWorkId, b.DueWorkId); });
        facts.RemoveAll(fact => !instances.TryGetValue(fact.InstanceId, out ActivityInstance instance) || instance.Revision != fact.Revision
            || (instance.State != ActivityLifecycleState.Scheduled && instance.State != ActivityLifecycleState.Active));
        if (!timeline.TryIndexOwnerFacts(facts, out failure)) return false;
        return true;
    }

    public bool TryCancel(SimulationTimeline timeline, string instanceId, string disposition, out ActivityFailure failure) =>
        TryTerminate(timeline, instanceId, ActivityLifecycleState.Cancelled, disposition, out failure);
    public bool TryInterrupt(SimulationTimeline timeline, string instanceId, string disposition, out ActivityFailure failure) =>
        TryTerminate(timeline, instanceId, ActivityLifecycleState.Interrupted, disposition, out failure);

    /// <summary>Compatibility guard only; the supplied instant is compared and never persisted as authority.</summary>
    public bool TryCancel(SimulationTimeline timeline, string instanceId, LogicalTick requestedInstant, string disposition, out ActivityFailure failure) =>
        TryTerminate(timeline, instanceId, requestedInstant, ActivityLifecycleState.Cancelled, disposition, out failure);

    private bool TryTerminate(SimulationTimeline timeline, string id, ActivityLifecycleState terminal, string disposition, out ActivityFailure failure) =>
        TryTerminate(timeline, id, timeline == null ? default(LogicalTick) : timeline.CurrentInstant, terminal, disposition, out failure);

    private bool TryTerminate(SimulationTimeline timeline, string id, LogicalTick requestedInstant, ActivityLifecycleState terminal, string disposition, out ActivityFailure failure)
    {
        if (timeline == null) throw new ArgumentNullException(nameof(timeline));
        if (!ReferenceEquals(authoritativeTimeline, timeline))
        { failure = ActivityFailure.TimelineMismatch; return false; }
        LogicalTick authoritativeInstant = timeline.CurrentInstant;
        if (requestedInstant != authoritativeInstant) { failure = ActivityFailure.InvalidInstant; return false; }
        if (!instances.TryGetValue(id, out ActivityInstance item)) { failure = ActivityFailure.UnknownInstance; return false; }
        if (terminal == ActivityLifecycleState.Interrupted && item.State != ActivityLifecycleState.Active)
        { failure = ActivityFailure.InvalidState; return false; }
        if (item.State == ActivityLifecycleState.Completed || item.State == ActivityLifecycleState.Cancelled || item.State == ActivityLifecycleState.Interrupted)
        { failure = ActivityFailure.InvalidState; return false; }
        IActivityLifecycleTransitionCommit coordinated = null;
        ActivityTransitionKind kind = terminal == ActivityLifecycleState.Cancelled ? ActivityTransitionKind.Cancel : ActivityTransitionKind.Interrupt;
        if (transitionParticipant != null && !transitionParticipant.TryPrepareTerminal(new ActivityInstanceSnapshot(item), terminal, kind,
            authoritativeInstant, disposition, out coordinated))
        { failure = ActivityFailure.InvalidState; return false; }
        long revision; try { revision = checked(item.Revision + 1); } catch (OverflowException) { failure = ActivityFailure.RevisionOverflow; return false; }
        List<DueWorkReference> stagedPending = pending.Where(fact => fact.InstanceId != item.Id).ToList();
        if (!TryStageReceipt(item, revision, kind, authoritativeInstant, item.Participants, disposition, out ActivityTransitionReceipt receipt))
        { failure = ActivityFailure.RevisionOverflow; return false; }
        List<ActivityTransitionReceipt> stagedReceipts = StageReceiptHistory(receipt);
        long stagedNextTransitionSequence = receipt.Sequence + 1L;
        Dictionary<string, List<ActivityParticipantCommitment>> stagedCommitments = StageCommitmentRelease(item);
        bool committed = timeline.TryCommitOwnerFacts(Array.Empty<DueWorkReference>(), () =>
        {
            if (coordinated != null && !coordinated.CanCommit) return TimelineFailure.StaleWork;
            item.Revision = revision; item.State = terminal; item.TerminalInstant = authoritativeInstant; item.Disposition = disposition ?? string.Empty;
            pending = stagedPending; commitments = stagedCommitments; transitionReceipts = stagedReceipts;
            nextTransitionSequence = stagedNextTransitionSequence; coordinated?.CommitTerminal(); return TimelineFailure.None;
        }, out _);
        failure = committed ? ActivityFailure.None : ActivityFailure.TimelinePublicationFailed; return committed;
    }

    /// <summary>P20-B-only terminal seam for duration-null, consumer-managed activities.</summary>
    internal bool TryConsumerManagedTerminal(SimulationTimeline timeline, string id, ActivityLifecycleState terminal,
        string disposition, out ActivityFailure failure)
    {
        if (timeline == null) throw new ArgumentNullException(nameof(timeline));
        if (!ReferenceEquals(authoritativeTimeline, timeline)) { failure = ActivityFailure.TimelineMismatch; return false; }
        if (terminal != ActivityLifecycleState.Completed && terminal != ActivityLifecycleState.Interrupted)
        { failure = ActivityFailure.InvalidState; return false; }
        if (!instances.TryGetValue(id, out ActivityInstance item)) { failure = ActivityFailure.UnknownInstance; return false; }
        if (item.State != ActivityLifecycleState.Active || item.PlannedEnd.HasValue || transitionParticipant == null)
        { failure = ActivityFailure.InvalidState; return false; }
        LogicalTick instant = timeline.CurrentInstant;
        ActivityTransitionKind kind = terminal == ActivityLifecycleState.Completed ? ActivityTransitionKind.Complete : ActivityTransitionKind.Interrupt;
        if (!transitionParticipant.TryPrepareTerminal(new ActivityInstanceSnapshot(item), terminal, kind, instant,
            disposition, out IActivityLifecycleTransitionCommit coordinated) || coordinated == null)
        { failure = ActivityFailure.InvalidState; return false; }
        long revision;
        try { revision = checked(item.Revision + 1); }
        catch (OverflowException) { failure = ActivityFailure.RevisionOverflow; return false; }
        List<DueWorkReference> stagedPending = pending.Where(fact => fact.InstanceId != item.Id).ToList();
        if (!TryStageReceipt(item, revision, kind, instant, item.Participants, disposition, out ActivityTransitionReceipt receipt))
        { failure = ActivityFailure.RevisionOverflow; return false; }
        List<ActivityTransitionReceipt> stagedReceipts = StageReceiptHistory(receipt);
        long stagedNextTransitionSequence = receipt.Sequence + 1L;
        Dictionary<string, List<ActivityParticipantCommitment>> stagedCommitments = StageCommitmentRelease(item);
        bool committed = timeline.TryCommitOwnerFacts(Array.Empty<DueWorkReference>(), () =>
        {
            if (!coordinated.CanCommit) return TimelineFailure.StaleWork;
            item.Revision = revision; item.State = terminal; item.TerminalInstant = instant; item.Disposition = disposition ?? string.Empty;
            pending = stagedPending; commitments = stagedCommitments; transitionReceipts = stagedReceipts;
            nextTransitionSequence = stagedNextTransitionSequence; coordinated.CommitTerminal();
            return TimelineFailure.None;
        }, out _);
        failure = committed ? ActivityFailure.None : ActivityFailure.TimelinePublicationFailed;
        return committed;
    }

    public ActivityParticipantCommitment GetCommitment(string participantId)
    {
        if (!commitments.TryGetValue(participantId, out List<ActivityParticipantCommitment> values) || values.Count == 0) return null;
        return values.OrderBy(c => c.Start.Value).ThenBy(c => c.ActivityInstanceId, StringComparer.Ordinal).First();
    }
    public bool IsAvailable(string participantId, LogicalTick at)
    {
        if (!commitments.TryGetValue(participantId, out List<ActivityParticipantCommitment> values)) return true;
        foreach (ActivityParticipantCommitment value in values)
            if (instances.TryGetValue(value.ActivityInstanceId, out ActivityInstance instance)
                && (instance.State == ActivityLifecycleState.Scheduled || instance.State == ActivityLifecycleState.Active)
                && at.Value >= value.Start.Value && (!value.End.HasValue || at.Value < value.End.Value.Value)) return false;
        return true;
    }

    /// <summary>Availability check for a scheduled activity, excluding only its own accepted reservation.</summary>
    internal bool IsAvailableForActivity(string participantId, LogicalTick at, string activityInstanceId)
    {
        if (!commitments.TryGetValue(participantId, out List<ActivityParticipantCommitment> values)) return true;
        foreach (ActivityParticipantCommitment value in values)
        {
            if (value.ActivityInstanceId == activityInstanceId) continue;
            if (instances.TryGetValue(value.ActivityInstanceId, out ActivityInstance instance)
                && (instance.State == ActivityLifecycleState.Scheduled || instance.State == ActivityLifecycleState.Active)
                && at.Value >= value.Start.Value && (!value.End.HasValue || at.Value < value.End.Value.Value)) return false;
        }
        return true;
    }

    public bool IsCurrent(DueWorkReference reference)
    {
        if (reference == null || reference.OwnerId != DueOwnerId || !instances.TryGetValue(reference.InstanceId, out ActivityInstance item)) return false;
        if (item.Revision != reference.Revision) return false;
        if (reference.DueWorkId == WorkId(item.Id, ActivityTransitionKind.Start)) return item.State == ActivityLifecycleState.Scheduled && item.PlannedStart == reference.DueAt;
        if (reference.DueWorkId == WorkId(item.Id, ActivityTransitionKind.Complete)) return item.State == ActivityLifecycleState.Active && item.PlannedEnd == reference.DueAt;
        return false;
    }

    public bool TryPrepare(DueWorkReference reference, out IDueWorkCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        if (!IsCurrent(reference)) { failure = TimelineFailure.StaleWork; return false; }
        ActivityTransitionKind kind = reference.DueWorkId == WorkId(reference.InstanceId, ActivityTransitionKind.Start) ? ActivityTransitionKind.Start : ActivityTransitionKind.Complete;
        string disposition = string.Empty;
        IActivityLifecycleTransitionCommit coordinated = null;
        bool allowed = true;
        if (kind == ActivityTransitionKind.Start)
        {
            ActivityInstanceSnapshot snapshot = new ActivityInstanceSnapshot(instances[reference.InstanceId]);
            allowed = startValidator.TryValidate(snapshot, reference.DueAt, out disposition);
            if (transitionParticipant != null)
            {
                if (!transitionParticipant.TryPrepareStart(snapshot, reference.DueAt, out coordinated, out string participantDisposition))
                    allowed = false;
                if (string.IsNullOrWhiteSpace(disposition)) disposition = participantDisposition;
                if (coordinated != null && !coordinated.StartAllowed) allowed = false;
            }
        }
        if (!allowed) disposition = string.IsNullOrWhiteSpace(disposition) ? (coordinated?.FailureDisposition ?? "start-precondition-failed") : disposition;
        prepared = new ActivityCommit(this, reference, kind, allowed, disposition, coordinated);
        failure = TimelineFailure.None; return true;
    }

    public IReadOnlyList<DueWorkReference> RebuildPendingReferences()
    { return pending.AsReadOnly(); }

    private bool CommitTransition(DueWorkReference reference, ActivityTransitionKind kind, bool startAllowed, string failureDisposition,
        IActivityLifecycleTransitionCommit coordinated, out TimelineFailure failure)
    {
        if (!IsCurrent(reference)) { failure = TimelineFailure.StaleWork; return false; }
        if (coordinated != null)
        {
            bool coordinatedCanCommit = kind == ActivityTransitionKind.Start && !startAllowed
                && coordinated is IActivityLifecycleStartFailureCommit failedStartCommit
                ? failedStartCommit.CanCommitFailedStart
                : coordinated.CanCommit;
            if (!coordinatedCanCommit) { failure = TimelineFailure.StaleWork; return false; }
        }
        ActivityInstance item = instances[reference.InstanceId];
        long revision; try { revision = checked(item.Revision + 1); } catch (OverflowException) { failure = TimelineFailure.DispatchFailed; return false; }
        if (kind == ActivityTransitionKind.Start && !startAllowed)
        {
            long terminalRevision; try { terminalRevision = checked(item.Revision + 1); }
            catch (OverflowException) { failure = TimelineFailure.DispatchFailed; return false; }
            List<DueWorkReference> terminalPending = pending.Where(fact => fact.InstanceId != item.Id).ToList();
            string failedDisposition = string.IsNullOrWhiteSpace(failureDisposition) ? "start-precondition-failed" : failureDisposition;
            if (!TryStageReceipt(item, terminalRevision, ActivityTransitionKind.FailedStart, reference.DueAt, item.Participants, failedDisposition, out ActivityTransitionReceipt failedReceipt))
            { failure = TimelineFailure.DispatchFailed; return false; }
            List<ActivityTransitionReceipt> stagedReceipts = StageReceiptHistory(failedReceipt);
            long failedNextTransitionSequence = failedReceipt.Sequence + 1L;
            Dictionary<string, List<ActivityParticipantCommitment>> failedCommitments = StageCommitmentRelease(item);
            item.Revision = terminalRevision; item.State = ActivityLifecycleState.Cancelled;
            item.TerminalInstant = reference.DueAt; item.Disposition = failedDisposition;
            pending = terminalPending; commitments = failedCommitments; transitionReceipts = stagedReceipts;
            nextTransitionSequence = failedNextTransitionSequence;
            coordinated?.CommitFailedStart(); failure = TimelineFailure.None; return true;
        }
        if (!TryStageReceipt(item, revision, kind, reference.DueAt, item.Participants, kind == ActivityTransitionKind.Start ? "started" : "completed", out ActivityTransitionReceipt receipt))
        { failure = TimelineFailure.DispatchFailed; return false; }
        DueWorkReference completion = kind == ActivityTransitionKind.Start && item.PlannedEnd.HasValue
            ? Reference(item, revision, ActivityTransitionKind.Complete, item.PlannedEnd.Value) : null;
        List<DueWorkReference> stagedPending = pending.Where(fact => fact.InstanceId != item.Id).ToList();
        if (completion != null) stagedPending.Add(completion);
        List<ActivityTransitionReceipt> nextReceipts = StageReceiptHistory(receipt);
        long stagedNextTransitionSequence = receipt.Sequence + 1L;
        Dictionary<string, List<ActivityParticipantCommitment>> stagedCommitments = kind == ActivityTransitionKind.Complete
            ? StageCommitmentRelease(item) : null;
        item.Revision = revision;
        if (kind == ActivityTransitionKind.Start)
        {
            item.State = ActivityLifecycleState.Active; item.ActualStart = reference.DueAt;
            pending = stagedPending; transitionReceipts = nextReceipts; nextTransitionSequence = receipt.Sequence + 1L;
            coordinated?.CommitStarted(); failure = TimelineFailure.None; return true;
        }
        item.State = ActivityLifecycleState.Completed; item.TerminalInstant = reference.DueAt;
        pending = stagedPending; commitments = stagedCommitments; transitionReceipts = nextReceipts;
        nextTransitionSequence = stagedNextTransitionSequence; failure = TimelineFailure.None; return true;
    }

    private List<ActivityTransitionReceipt> StageReceiptHistory(ActivityTransitionReceipt receipt)
    {
        List<ActivityTransitionReceipt> staged = new List<ActivityTransitionReceipt>(transitionReceipts.Count + 1);
        staged.AddRange(transitionReceipts);
        staged.Add(receipt);
        return staged;
    }

    private bool TryStageReceipt(ActivityInstance item, long revision, ActivityTransitionKind kind, LogicalTick instant,
        IEnumerable<string> participants, string disposition, out ActivityTransitionReceipt receipt)
    {
        receipt = null;
        try { receipt = new ActivityTransitionReceipt(worldId, nextTransitionSequence, item.Id, revision, kind, instant, participants, disposition); checked { _ = nextTransitionSequence + 1; } return true; }
        catch (OverflowException) { return false; }
    }
    private Dictionary<string, List<ActivityParticipantCommitment>> StageCommitmentRelease(ActivityInstance item)
    {
        Dictionary<string, List<ActivityParticipantCommitment>> staged =
            new Dictionary<string, List<ActivityParticipantCommitment>>(commitments, StringComparer.Ordinal);
        foreach (string participant in item.Participants)
            if (commitments.TryGetValue(participant, out List<ActivityParticipantCommitment> values))
            {
                List<ActivityParticipantCommitment> remaining = new List<ActivityParticipantCommitment>(values.Count);
                foreach (ActivityParticipantCommitment commitment in values)
                    if (commitment.ActivityInstanceId != item.Id) remaining.Add(commitment);
                if (remaining.Count == 0) staged.Remove(participant);
                else if (remaining.Count != values.Count) staged[participant] = remaining;
            }
        return staged;
    }
    private static string WorkId(string instanceId, ActivityTransitionKind kind) => SpatialStableKey.Encode(instanceId, kind == ActivityTransitionKind.Start ? "start" : "complete");
    private DueWorkReference Reference(ActivityInstance item, long revision, ActivityTransitionKind kind, LogicalTick at) =>
        new DueWorkReference(DueOwnerId, WorkId(item.Id, kind), item.Id, revision, revision, at);

    private sealed class ActivityCommit : IDueWorkCommit
    {
        private readonly ActivityLifecycleStore owner; private readonly DueWorkReference reference; private readonly ActivityTransitionKind kind;
        private readonly bool startAllowed; private readonly string failureDisposition;
        private readonly IActivityLifecycleTransitionCommit coordinated;
        public ActivityCommit(ActivityLifecycleStore owner, DueWorkReference reference, ActivityTransitionKind kind, bool startAllowed,
            string failureDisposition, IActivityLifecycleTransitionCommit coordinated)
        { this.owner = owner; this.reference = reference; this.kind = kind; this.startAllowed = startAllowed; this.failureDisposition = failureDisposition; this.coordinated = coordinated; }
        public IReadOnlyList<DueWorkReference> NewOwnerFacts
        {
            get
            {
                if (!startAllowed || kind != ActivityTransitionKind.Start || reference.Revision == long.MaxValue
                    || !owner.instances.TryGetValue(reference.InstanceId, out ActivityInstance item) || !item.PlannedEnd.HasValue)
                    return Array.Empty<DueWorkReference>();
                return new[] { owner.Reference(item, reference.Revision + 1, ActivityTransitionKind.Complete, item.PlannedEnd.Value) };
            }
        }
        public bool TryCommit(out TimelineFailure failure) => owner.CommitTransition(reference, kind, startAllowed, failureDisposition, coordinated, out failure);
    }
}

/// <summary>Trusted host composition that creates/binds the one lifecycle timeline before exposing either object.</summary>
public sealed class ActivityLifecycleComposition
{
    public ActivityLifecycleStore Store { get; }
    public SimulationTimeline Timeline { get; }

    public ActivityLifecycleComposition(string worldId, SimulationCalendar calendar, LogicalTick initialInstant,
        long nextIdentity = 0, IActivityStartValidator startValidator = null)
        : this(new ActivityLifecycleStore(worldId, nextIdentity, startValidator), calendar, initialInstant) { }

    /// <summary>Recompose a cloned/restored store with its new authoritative timeline before lifecycle operations.</summary>
    public ActivityLifecycleComposition(ActivityLifecycleStore store, SimulationCalendar calendar, LogicalTick initialInstant)
    {
        Store = store ?? throw new ArgumentNullException(nameof(store));
        Timeline = new SimulationTimeline(calendar, initialInstant, Store);
        Store.BindTimeline(Timeline);
    }

    /// <summary>Trusted composition for owners that also coordinate a timeline boundary authority.</summary>
    public ActivityLifecycleComposition(ActivityLifecycleStore store, SimulationCalendar calendar, LogicalTick initialInstant,
        IDayBoundaryOwner boundaryOwner, string timelineWorldId, string timelineProfileId = "default")
    {
        Store = store ?? throw new ArgumentNullException(nameof(store));
        Timeline = new SimulationTimeline(calendar, initialInstant, Store, boundaryOwner: boundaryOwner,
            worldId: timelineWorldId, profileId: timelineProfileId);
        Store.BindTimeline(Timeline);
    }
}
