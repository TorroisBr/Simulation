using System;
using System.Collections.Generic;
using System.Linq;

public enum P20JointCivilTravelState
{
    Proposed = 0, Scheduled = 1, Active = 2, Completed = 3, Interrupted = 4,
    Cancelled = 5, FailedToStart = 6, NotFormed = 7
}

public sealed class P20JointCivilTravelAssent
{
    public string PersonId { get; }
    public bool Accepted { get; }
    public string CausalInputIdentity { get; }
    public P20JointCivilTravelAssent(string personId, bool accepted, string causalInputIdentity)
    {
        if (string.IsNullOrWhiteSpace(personId)) throw new ArgumentException("Person identity is required.", nameof(personId));
        if (string.IsNullOrWhiteSpace(causalInputIdentity)) throw new ArgumentException("Causal input identity is required.", nameof(causalInputIdentity));
        PersonId = personId; Accepted = accepted; CausalInputIdentity = causalInputIdentity;
    }
}

public sealed class P20JointCivilTravelSnapshot
{
    public string ActivityInstanceId { get; }
    public string SharedSegmentStableKey { get; }
    public IReadOnlyList<string> PersonIds { get; }
    public IReadOnlyList<P20JointCivilTravelAssent> Assents { get; }
    public P20JointCivilTravelState State { get; }
    public bool AbortAfterLegRequested { get; }
    public long Revision { get; }
    public string Disposition { get; }
    internal P20JointCivilTravelSnapshot(P20JointCivilTravel operation)
    {
        ActivityInstanceId = operation.ActivityInstanceId;
        SharedSegmentStableKey = operation.SharedSegmentStableKey;
        PersonIds = Array.AsReadOnly(operation.PersonIds.ToArray());
        Assents = Array.AsReadOnly(operation.Assents.Values.OrderBy(x => x.PersonId, StringComparer.Ordinal).ToArray());
        State = operation.State; AbortAfterLegRequested = operation.AbortAfterLegRequested;
        Revision = operation.Revision; Disposition = operation.Disposition;
    }
}

internal sealed class P20JointCivilTravel
{
    internal readonly string ActivityInstanceId;
    internal readonly string SharedSegmentStableKey;
    internal readonly string[] PersonIds;
    internal readonly SortedDictionary<string, P20JointCivilTravelAssent> Assents;
    internal readonly P20JointCivilTravelState State;
    internal readonly bool AbortAfterLegRequested;
    internal readonly long Revision;
    internal readonly long ExpectedLifecycleRevision;
    internal readonly string Disposition;
    internal P20JointCivilTravel(string id, string sharedSegmentStableKey, IEnumerable<string> people,
        SortedDictionary<string, P20JointCivilTravelAssent> assents, P20JointCivilTravelState state,
        bool abortRequested, long revision, long lifecycleRevision, string disposition)
    {
        ActivityInstanceId = id; SharedSegmentStableKey = sharedSegmentStableKey;
        PersonIds = people.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assents = new SortedDictionary<string, P20JointCivilTravelAssent>(assents, StringComparer.Ordinal);
        State = state; AbortAfterLegRequested = abortRequested; Revision = revision;
        ExpectedLifecycleRevision = lifecycleRevision; Disposition = disposition ?? string.Empty;
    }
    internal P20JointCivilTravel With(SortedDictionary<string, P20JointCivilTravelAssent> assents = null,
        P20JointCivilTravelState? state = null, bool? abortRequested = null, long? revision = null,
        long? lifecycleRevision = null, string disposition = null) => new P20JointCivilTravel(ActivityInstanceId,
            SharedSegmentStableKey,
            PersonIds, assents ?? Assents, state ?? State, abortRequested ?? AbortAfterLegRequested,
            revision ?? Revision, lifecycleRevision ?? ExpectedLifecycleRevision, disposition ?? Disposition);
}

/// <summary>Bounded P20-B shared activity for two Persons and one explicit P8 civil leg.</summary>
public sealed class P20JointCivilTravelOwner
{
    public const string ActivityDefinitionId = "p20-joint-civil-travel";
    private readonly ActivityLifecycleComposition composition;
    private readonly P8ETravelTransactionCoordinator travel;
    private readonly Func<PersonId, TraversalCostContext> movementContext;
    private Dictionary<string, P20JointCivilTravel> operations = new Dictionary<string, P20JointCivilTravel>(StringComparer.Ordinal);
    private string terminalArrivalPersonId;

    public P20JointCivilTravelOwner(ActivityLifecycleComposition composition,
        P8ETravelTransactionCoordinator travel, Func<PersonId, TraversalCostContext> movementContext)
    {
        this.composition = composition ?? throw new ArgumentNullException(nameof(composition));
        this.travel = travel ?? throw new ArgumentNullException(nameof(travel));
        this.movementContext = movementContext ?? throw new ArgumentNullException(nameof(movementContext));
        composition.Store.BindTransitionParticipant(new TransitionParticipant(this));
    }

    public bool HasOwnerState => operations.Count != 0;
    public int InstanceCount => operations.Count;

    public bool TryCreate(ActivityDefinition definition, string creationIdentity, string sharedSegmentStableKey,
        IReadOnlyList<string> personIds,
        out P20JointCivilTravelSnapshot snapshot, out ActivityFailure failure)
    {
        snapshot = null;
        if (definition == null || definition.Id != ActivityDefinitionId || string.IsNullOrWhiteSpace(sharedSegmentStableKey)
            || personIds == null || personIds.Count != 2 || personIds.Any(string.IsNullOrWhiteSpace)
            || personIds.Distinct(StringComparer.Ordinal).Count() != 2)
        { failure = ActivityFailure.ParticipantConflict; return false; }
        string predictedId = composition.Store.NextProposedInstanceId;
        P20JointCivilTravel operation = new P20JointCivilTravel(predictedId, sharedSegmentStableKey, personIds,
            new SortedDictionary<string, P20JointCivilTravelAssent>(StringComparer.Ordinal),
            P20JointCivilTravelState.Proposed, false, 0L, 0L, string.Empty);
        P20JointCivilTravelSnapshot stagedSnapshot = new P20JointCivilTravelSnapshot(operation);
        Dictionary<string, P20JointCivilTravel> stagedOperations = new Dictionary<string, P20JointCivilTravel>(operations, StringComparer.Ordinal)
        { [predictedId] = operation };
        if (!composition.Store.TryPropose(definition, creationIdentity, out ActivityInstanceSnapshot instance, out failure)) return false;
        if (!string.Equals(instance.Id, predictedId, StringComparison.Ordinal))
        { snapshot = null; failure = ActivityFailure.InvalidState; return false; }
        operations = stagedOperations;
        snapshot = stagedSnapshot;
        return true;
    }

    public bool TryGet(string activityInstanceId, out P20JointCivilTravelSnapshot snapshot)
    {
        snapshot = operations.TryGetValue(activityInstanceId, out P20JointCivilTravel operation)
            ? new P20JointCivilTravelSnapshot(operation) : null;
        return snapshot != null;
    }

    public bool TryRecordAssent(string activityInstanceId, P20JointCivilTravelAssent assent)
    {
        if (assent == null || !operations.TryGetValue(activityInstanceId, out P20JointCivilTravel current)
            || current.State != P20JointCivilTravelState.Proposed
            || !current.PersonIds.Contains(assent.PersonId, StringComparer.Ordinal)
            || current.Assents.ContainsKey(assent.PersonId) || current.Revision == long.MaxValue) return false;
        SortedDictionary<string, P20JointCivilTravelAssent> assents = new SortedDictionary<string, P20JointCivilTravelAssent>(current.Assents, StringComparer.Ordinal)
        { [assent.PersonId] = assent };
        operations[activityInstanceId] = current.With(assents: assents, revision: current.Revision + 1L);
        return true;
    }

    /// <summary>Both independent assents authorize one P18 open-ended commitment reservation.</summary>
    public bool TrySchedule(string activityInstanceId, LogicalTick start, out ActivityFailure failure)
    {
        if (!operations.TryGetValue(activityInstanceId, out P20JointCivilTravel current)
            || current.State != P20JointCivilTravelState.Proposed
            || current.PersonIds.Any(id => !current.Assents.TryGetValue(id, out P20JointCivilTravelAssent assent) || !assent.Accepted)
            || !composition.Store.TryGet(activityInstanceId, out ActivityInstanceSnapshot instance)
            || instance.State != ActivityLifecycleState.Proposed || instance.Revision != current.ExpectedLifecycleRevision)
        { failure = ActivityFailure.InvalidState; return false; }
        if (current.Revision > long.MaxValue - 6L || instance.Revision > long.MaxValue - 3L)
        { failure = ActivityFailure.RevisionOverflow; return false; }
        long earliest;
        try { earliest = checked(Math.Max(composition.Timeline.CurrentInstant.Value,
            composition.Timeline.InputsSealedThrough?.Value ?? composition.Timeline.CurrentInstant.Value) + 1L); }
        catch (OverflowException) { failure = ActivityFailure.InvalidInterval; return false; }
        if (start.Value < earliest) { failure = ActivityFailure.InvalidInterval; return false; }
        P20JointCivilTravel scheduled = current.With(state: P20JointCivilTravelState.Scheduled,
            revision: current.Revision + 1L, lifecycleRevision: instance.Revision + 1L);
        return composition.Store.TrySchedule(composition.Timeline, activityInstanceId,
            composition.Timeline.CurrentInstant, start, null, current.PersonIds, () => operations[activityInstanceId] = scheduled, out failure);
    }

    public bool TryCancel(string activityInstanceId, string disposition, out ActivityFailure failure) =>
        composition.Store.TryCancel(composition.Timeline, activityInstanceId, disposition, out failure);

    public bool TryRequestAbortAfterLeg(string activityInstanceId, long expectedRevision)
    {
        if (!operations.TryGetValue(activityInstanceId, out P20JointCivilTravel current)
            || current.State != P20JointCivilTravelState.Active || current.Revision != expectedRevision
            || current.AbortAfterLegRequested || current.Revision == long.MaxValue
            || !composition.Store.TryGet(activityInstanceId, out ActivityInstanceSnapshot activity)
            || activity.State != ActivityLifecycleState.Active || activity.Revision != current.ExpectedLifecycleRevision) return false;
        operations[activityInstanceId] = current.With(abortRequested: true, revision: current.Revision + 1L);
        return true;
    }

    /// <summary>Arrival is explicit. The first Person commits independently; the second shares its P8 arrival with P18 terminal facts.</summary>
    public bool TryArrive(string activityInstanceId, string personId, out ActivityFailure failure)
    {
        if (!operations.TryGetValue(activityInstanceId, out P20JointCivilTravel current)
            || current.State != P20JointCivilTravelState.Active || !current.PersonIds.Contains(personId, StringComparer.Ordinal)
            || !composition.Store.TryGet(activityInstanceId, out ActivityInstanceSnapshot activity)
            || activity.State != ActivityLifecycleState.Active || activity.PlannedEnd.HasValue)
        { failure = ActivityFailure.InvalidState; return false; }
        string otherId = current.PersonIds.Single(x => x != personId);
        PersonId actor = new PersonId(personId);
        if (!travel.TryGetPlanDestination(actor, out HexId destination))
        { failure = ActivityFailure.InvalidState; return false; }
        bool otherArrived = travel.TryGetFinalArrival(new PersonId(otherId), out HexId otherDestination)
            && otherDestination == destination;
        if (!otherArrived)
        {
            if (current.AbortAfterLegRequested && !IsTransitAtFinalArrival(personId))
            { failure = ActivityFailure.InvalidState; return false; }
            if (!travel.TryArriveAtFinalDestination(actor, out P8ETravelFailure travelFailure))
            { failure = ActivityFailure.InvalidState; return false; }
            failure = ActivityFailure.None;
            return true;
        }
        terminalArrivalPersonId = personId;
        try
        {
            ActivityLifecycleState terminal = current.AbortAfterLegRequested
                ? ActivityLifecycleState.Interrupted : ActivityLifecycleState.Completed;
            string disposition = current.AbortAfterLegRequested ? "abort-after-leg" : "all-required-persons-arrived";
            return composition.Store.TryConsumerManagedTerminal(composition.Timeline, activityInstanceId,
                terminal, disposition, out failure);
        }
        finally { terminalArrivalPersonId = null; }
    }

    private bool IsTransitAtFinalArrival(string personId)
    {
        return travel.IsFinalArrivalReady(new PersonId(personId));
    }

    private bool TryPrepareStart(ActivityInstanceSnapshot instance, LogicalTick instant,
        out IActivityLifecycleTransitionCommit prepared, out string failureDisposition)
    {
        prepared = null;
        failureDisposition = "joint-civil-travel-start-failed";
        if (!operations.TryGetValue(instance.Id, out P20JointCivilTravel current))
        {
            if (instance.DefinitionId != ActivityDefinitionId) { failureDisposition = null; return true; }
            failureDisposition = "joint-owner-state-missing";
            return false;
        }
        if (current.State != P20JointCivilTravelState.Scheduled
            || current.ExpectedLifecycleRevision != instance.Revision
            || instance.State != ActivityLifecycleState.Scheduled
            || !instance.Participants.SequenceEqual(current.PersonIds, StringComparer.Ordinal)
            || current.PersonIds.Any(id => !current.Assents.TryGetValue(id, out P20JointCivilTravelAssent assent) || !assent.Accepted))
        {
            P20JointCivilTravel failed = current.With(state: P20JointCivilTravelState.FailedToStart,
                revision: current.Revision == long.MaxValue ? current.Revision : current.Revision + 1L,
                lifecycleRevision: instance.Revision + 1L, disposition: "scheduled-instance-stale-or-incomplete");
            prepared = new JointTransitionCommit(this, current, failed, false, "scheduled-instance-stale-or-incomplete", null, null);
            failureDisposition = "scheduled-instance-stale-or-incomplete";
            return false;
        }
        List<P8EJointTravelParticipant> participants = new List<P8EJointTravelParticipant>(2);
        foreach (string id in current.PersonIds)
        {
            PersonId person = new PersonId(id);
            TraversalCostContext context = movementContext(person);
            if (context == null)
            {
                P20JointCivilTravel failed = current.With(state: P20JointCivilTravelState.FailedToStart,
                    revision: current.Revision == long.MaxValue ? current.Revision : current.Revision + 1L,
                    lifecycleRevision: instance.Revision + 1L, disposition: "movement-context-unavailable");
                prepared = new JointTransitionCommit(this, current, failed, false, "movement-context-unavailable", null, null);
                failureDisposition = "movement-context-unavailable";
                return false;
            }
            participants.Add(new P8EJointTravelParticipant(person, context));
        }
        if (!travel.TryPrepareJointCivilLeg(participants, current.SharedSegmentStableKey,
            out PreparedP8EJointCivilLeg preparedLeg, out P8ETravelFailure travelFailure))
        {
            P20JointCivilTravel failed = current.With(state: P20JointCivilTravelState.FailedToStart,
                revision: current.Revision == long.MaxValue ? current.Revision : current.Revision + 1L,
                lifecycleRevision: instance.Revision + 1L, disposition: "joint-travel-precondition-failed");
            prepared = new JointTransitionCommit(this, current, failed, false, "joint-travel-precondition-failed", null, null);
            failureDisposition = "joint-travel-precondition-failed";
            return false;
        }
        P20JointCivilTravel active = current.With(state: P20JointCivilTravelState.Active,
            revision: current.Revision + 1L, lifecycleRevision: instance.Revision + 1L, disposition: "started");
        P20JointCivilTravel failedStart = current.With(state: P20JointCivilTravelState.FailedToStart,
            revision: current.Revision + 1L, lifecycleRevision: instance.Revision + 1L, disposition: "failed-to-start");
        prepared = new JointTransitionCommit(this, current, active, true, null, preparedLeg, null, failedStart);
        failureDisposition = null;
        return true;
    }

    private bool TryPrepareTerminal(ActivityInstanceSnapshot instance, ActivityLifecycleState terminal,
        ActivityTransitionKind kind, LogicalTick instant, string disposition,
        out IActivityLifecycleTransitionCommit prepared)
    {
        prepared = null;
        if (!operations.TryGetValue(instance.Id, out P20JointCivilTravel current))
            return instance.DefinitionId != ActivityDefinitionId;
        if (current.ExpectedLifecycleRevision != instance.Revision) return false;
        if (terminal == ActivityLifecycleState.Cancelled
            && (current.State == P20JointCivilTravelState.Proposed || current.State == P20JointCivilTravelState.Scheduled))
        {
            if (current.Revision == long.MaxValue) return false;
            prepared = new JointTransitionCommit(this, current,
                current.With(state: P20JointCivilTravelState.Cancelled, revision: current.Revision + 1L,
                    lifecycleRevision: instance.Revision + 1L, disposition: disposition ?? "cancelled"), true, null, null, null);
            return true;
        }
        if ((terminal != ActivityLifecycleState.Completed && terminal != ActivityLifecycleState.Interrupted)
            || current.State != P20JointCivilTravelState.Active || terminalArrivalPersonId == null
            || (current.AbortAfterLegRequested != (terminal == ActivityLifecycleState.Interrupted))
            || !current.PersonIds.Contains(terminalArrivalPersonId, StringComparer.Ordinal)
            || current.Revision == long.MaxValue
            || !travel.TryPrepareFinalArrival(new PersonId(terminalArrivalPersonId), out PreparedP8EPersonFinalArrival arrival, out _))
            return false;
        string otherId = current.PersonIds.Single(id => id != terminalArrivalPersonId);
        if (!travel.TryGetFinalArrival(new PersonId(otherId), out HexId otherDestination)
            || !travel.TryGetPlanDestination(new PersonId(terminalArrivalPersonId), out HexId destination)
            || otherDestination != destination) return false;
        P20JointCivilTravel next = current.With(state: terminal == ActivityLifecycleState.Completed
                ? P20JointCivilTravelState.Completed : P20JointCivilTravelState.Interrupted,
            revision: current.Revision + 1L, lifecycleRevision: instance.Revision + 1L,
            disposition: disposition ?? string.Empty);
        prepared = new JointTransitionCommit(this, current, next, true, null, null, arrival);
        return true;
    }

    private sealed class JointTransitionCommit : IActivityLifecycleTransitionCommit
    {
        private readonly P20JointCivilTravelOwner owner;
        private readonly P20JointCivilTravel expected;
        private readonly P20JointCivilTravel next;
        private readonly P20JointCivilTravel failed;
        private readonly bool allowed;
        private readonly string failure;
        private readonly PreparedP8EJointCivilLeg jointStart;
        private readonly PreparedP8EPersonFinalArrival finalArrival;
        public bool StartAllowed => allowed;
        public string FailureDisposition => failure;
        public bool CanCommit => owner.operations.TryGetValue(expected.ActivityInstanceId, out P20JointCivilTravel current)
            && ReferenceEquals(current, expected) && (jointStart == null || jointStart.CanInstall)
            && (finalArrival == null || finalArrival.CanInstall);
        internal JointTransitionCommit(P20JointCivilTravelOwner owner, P20JointCivilTravel expected,
            P20JointCivilTravel next, bool allowed, string failure, PreparedP8EJointCivilLeg jointStart,
            PreparedP8EPersonFinalArrival finalArrival, P20JointCivilTravel failed = null)
        { this.owner = owner; this.expected = expected; this.next = next; this.allowed = allowed; this.failure = failure;
            this.jointStart = jointStart; this.finalArrival = finalArrival; this.failed = failed; }
        public void CommitStarted()
        { jointStart.InstallPrepared(); owner.operations[next.ActivityInstanceId] = next; }
        public void CommitFailedStart()
        { if (failed != null) owner.operations[failed.ActivityInstanceId] = failed; else owner.operations[next.ActivityInstanceId] = next; }
        public void CommitTerminal()
        { finalArrival?.InstallPrepared(); owner.operations[next.ActivityInstanceId] = next; }
    }

    private sealed class TransitionParticipant : IActivityLifecycleTransitionParticipant
    {
        private readonly P20JointCivilTravelOwner owner;
        internal TransitionParticipant(P20JointCivilTravelOwner owner) { this.owner = owner; }
        public bool TryPrepareStart(ActivityInstanceSnapshot instance, LogicalTick instant,
            out IActivityLifecycleTransitionCommit prepared, out string failureDisposition) =>
            owner.TryPrepareStart(instance, instant, out prepared, out failureDisposition);
        public bool TryPrepareTerminal(ActivityInstanceSnapshot instance, ActivityLifecycleState terminal,
            ActivityTransitionKind kind, LogicalTick instant, string disposition,
            out IActivityLifecycleTransitionCommit prepared) =>
            owner.TryPrepareTerminal(instance, terminal, kind, instant, disposition, out prepared);
    }
}
