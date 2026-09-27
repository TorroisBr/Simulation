using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public enum ActivityLifecycleState { Proposed = 0, Scheduled = 1, Active = 2, Completed = 3, Cancelled = 4, Interrupted = 5 }
public enum ActivityTransitionKind { Start = 1, Complete = 2 }
public enum ActivityFailure { None = 0, InvalidDefinition = 1, DuplicateCreation = 2, UnknownInstance = 3, InvalidState = 4, NoParticipants = 5, InvalidInterval = 6, ParticipantConflict = 7, StaleWork = 8, RevisionOverflow = 9, TimelinePublicationFailed = 10, InvalidInstant = 11, TimelineMismatch = 12 }

public interface IActivityStartValidator
{
    bool TryValidate(ActivityInstanceSnapshot instance, LogicalTick instant, out string failureDisposition);
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
public sealed class ActivityLifecycleStore : IDueWorkOwner
{
    public const string DueOwnerId = "activity-lifecycle";
    private readonly Dictionary<string, ActivityInstance> instances = new Dictionary<string, ActivityInstance>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> creationIds = new Dictionary<string, string>(StringComparer.Ordinal);
    private Dictionary<string, List<ActivityParticipantCommitment>> commitments = new Dictionary<string, List<ActivityParticipantCommitment>>(StringComparer.Ordinal);
    private List<DueWorkReference> pending = new List<DueWorkReference>();
    private readonly string worldId;
    private readonly IActivityStartValidator startValidator;
    private SimulationTimeline authoritativeTimeline;
    private long nextIdentity;

    public ActivityLifecycleStore(string worldId, long nextIdentity = 0, IActivityStartValidator startValidator = null)
    {
        if (string.IsNullOrWhiteSpace(worldId)) throw new ArgumentException("World identity is required.", nameof(worldId));
        if (nextIdentity < 0) throw new ArgumentOutOfRangeException(nameof(nextIdentity));
        this.worldId = worldId; this.nextIdentity = nextIdentity; this.startValidator = startValidator ?? AcceptActivityStartValidator.Instance;
    }

    public long NextIdentity => nextIdentity;
    public IReadOnlyList<DueWorkReference> PendingWork => pending.AsReadOnly();

    public ActivityLifecycleStore Clone()
    {
        ActivityLifecycleStore clone = new ActivityLifecycleStore(worldId, nextIdentity, startValidator);
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
    {
        if (timeline == null) throw new ArgumentNullException(nameof(timeline));
        if (authoritativeTimeline != null && !ReferenceEquals(authoritativeTimeline, timeline))
        { failure = ActivityFailure.TimelineMismatch; return false; }
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
        bool committed = timeline.TryCommitOwnerFacts(work, () =>
        {
            item.Participants = stagedParticipants; item.PlannedStart = start; item.PlannedEnd = end;
            item.Revision = revision; item.State = ActivityLifecycleState.Scheduled;
            commitments = stagedCommitments; pending = stagedPending; return TimelineFailure.None;
        }, out _);
        if (!committed) { failure = ActivityFailure.TimelinePublicationFailed; return false; }
        authoritativeTimeline = timeline;
        failure = ActivityFailure.None; return true;
    }

    public bool TryRebuildTimelineIndex(SimulationTimeline timeline, out TimelineFailure failure)
    {
        if (timeline == null) throw new ArgumentNullException(nameof(timeline));
        if (authoritativeTimeline != null && !ReferenceEquals(authoritativeTimeline, timeline))
        { failure = TimelineFailure.DispatchFailed; return false; }
        List<DueWorkReference> facts = new List<DueWorkReference>(pending);
        facts.Sort((a, b) => { int c = a.DueAt.CompareTo(b.DueAt); if (c != 0) return c; c = string.CompareOrdinal(a.InstanceId, b.InstanceId); return c != 0 ? c : string.CompareOrdinal(a.DueWorkId, b.DueWorkId); });
        facts.RemoveAll(fact => !instances.TryGetValue(fact.InstanceId, out ActivityInstance instance) || instance.Revision != fact.Revision
            || (instance.State != ActivityLifecycleState.Scheduled && instance.State != ActivityLifecycleState.Active));
        if (!timeline.TryIndexOwnerFacts(facts, out failure)) return false;
        authoritativeTimeline = timeline;
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
        if (authoritativeTimeline != null && !ReferenceEquals(authoritativeTimeline, timeline))
        { failure = ActivityFailure.TimelineMismatch; return false; }
        LogicalTick authoritativeInstant = timeline.CurrentInstant;
        if (requestedInstant != authoritativeInstant) { failure = ActivityFailure.InvalidInstant; return false; }
        if (!instances.TryGetValue(id, out ActivityInstance item)) { failure = ActivityFailure.UnknownInstance; return false; }
        if (terminal == ActivityLifecycleState.Interrupted && item.State != ActivityLifecycleState.Active)
        { failure = ActivityFailure.InvalidState; return false; }
        if (item.State == ActivityLifecycleState.Completed || item.State == ActivityLifecycleState.Cancelled || item.State == ActivityLifecycleState.Interrupted)
        { failure = ActivityFailure.InvalidState; return false; }
        long revision; try { revision = checked(item.Revision + 1); } catch (OverflowException) { failure = ActivityFailure.RevisionOverflow; return false; }
        List<DueWorkReference> stagedPending = pending.Where(fact => fact.InstanceId != item.Id).ToList();
        item.Revision = revision; item.State = terminal; item.TerminalInstant = authoritativeInstant; item.Disposition = disposition ?? string.Empty;
        pending = stagedPending;
        ReleaseCommitments(item); authoritativeTimeline = timeline; failure = ActivityFailure.None; return true;
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
        if (kind == ActivityTransitionKind.Start && !startValidator.TryValidate(new ActivityInstanceSnapshot(instances[reference.InstanceId]), reference.DueAt, out disposition))
            prepared = new ActivityCommit(this, reference, kind, false, string.IsNullOrWhiteSpace(disposition) ? "start-precondition-failed" : disposition);
        else prepared = new ActivityCommit(this, reference, kind, true, null);
        failure = TimelineFailure.None; return true;
    }

    public IReadOnlyList<DueWorkReference> RebuildPendingReferences()
    { return pending.AsReadOnly(); }

    private bool CommitTransition(DueWorkReference reference, ActivityTransitionKind kind, bool startAllowed, string failureDisposition, out TimelineFailure failure)
    {
        if (!IsCurrent(reference)) { failure = TimelineFailure.StaleWork; return false; }
        ActivityInstance item = instances[reference.InstanceId];
        long revision; try { revision = checked(item.Revision + 1); } catch (OverflowException) { failure = TimelineFailure.DispatchFailed; return false; }
        if (kind == ActivityTransitionKind.Start && !startAllowed)
        {
            long terminalRevision; try { terminalRevision = checked(item.Revision + 1); }
            catch (OverflowException) { failure = TimelineFailure.DispatchFailed; return false; }
            List<DueWorkReference> terminalPending = pending.Where(fact => fact.InstanceId != item.Id).ToList();
            item.Revision = terminalRevision; item.State = ActivityLifecycleState.Cancelled;
            item.TerminalInstant = reference.DueAt; item.Disposition = string.IsNullOrWhiteSpace(failureDisposition) ? "start-precondition-failed" : failureDisposition;
            pending = terminalPending; ReleaseCommitments(item); failure = TimelineFailure.None; return true;
        }
        DueWorkReference completion = kind == ActivityTransitionKind.Start && item.PlannedEnd.HasValue
            ? Reference(item, revision, ActivityTransitionKind.Complete, item.PlannedEnd.Value) : null;
        List<DueWorkReference> stagedPending = pending.Where(fact => fact.InstanceId != item.Id).ToList();
        if (completion != null) stagedPending.Add(completion);
        item.Revision = revision;
        if (kind == ActivityTransitionKind.Start)
        {
            item.State = ActivityLifecycleState.Active; item.ActualStart = reference.DueAt;
            pending = stagedPending; failure = TimelineFailure.None; return true;
        }
        item.State = ActivityLifecycleState.Completed; item.TerminalInstant = reference.DueAt;
        pending = stagedPending; ReleaseCommitments(item); failure = TimelineFailure.None; return true;
    }

    private void ReleaseCommitments(ActivityInstance item)
    {
        foreach (string participant in item.Participants)
            if (commitments.TryGetValue(participant, out List<ActivityParticipantCommitment> values))
            {
                values.RemoveAll(c => c.ActivityInstanceId == item.Id);
                if (values.Count == 0) commitments.Remove(participant);
            }
    }
    private static string WorkId(string instanceId, ActivityTransitionKind kind) => SpatialStableKey.Encode(instanceId, kind == ActivityTransitionKind.Start ? "start" : "complete");
    private DueWorkReference Reference(ActivityInstance item, long revision, ActivityTransitionKind kind, LogicalTick at) =>
        new DueWorkReference(DueOwnerId, WorkId(item.Id, kind), item.Id, revision, revision, at);

    private sealed class ActivityCommit : IDueWorkCommit
    {
        private readonly ActivityLifecycleStore owner; private readonly DueWorkReference reference; private readonly ActivityTransitionKind kind;
        private readonly bool startAllowed; private readonly string failureDisposition;
        public ActivityCommit(ActivityLifecycleStore owner, DueWorkReference reference, ActivityTransitionKind kind, bool startAllowed, string failureDisposition)
        { this.owner = owner; this.reference = reference; this.kind = kind; this.startAllowed = startAllowed; this.failureDisposition = failureDisposition; }
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
        public bool TryCommit(out TimelineFailure failure) => owner.CommitTransition(reference, kind, startAllowed, failureDisposition, out failure);
    }
}
