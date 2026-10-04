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
    public LogicalTick? ProposedStart { get; }
    public LogicalTick? AcceptedAt { get; }
    public long AcceptedOrder { get; }
    public P20JointCivilTravelAssent(string personId, bool accepted, string causalInputIdentity)
    {
        if (string.IsNullOrWhiteSpace(personId)) throw new ArgumentException("Person identity is required.", nameof(personId));
        if (string.IsNullOrWhiteSpace(causalInputIdentity)) throw new ArgumentException("Causal input identity is required.", nameof(causalInputIdentity));
        PersonId = personId; Accepted = accepted; CausalInputIdentity = causalInputIdentity;
    }

    internal P20JointCivilTravelAssent(string personId, bool accepted, string causalInputIdentity,
        LogicalTick proposedStart, LogicalTick acceptedAt, long acceptedOrder)
        : this(personId, accepted, causalInputIdentity)
    {
        if (acceptedOrder <= 0L) throw new ArgumentOutOfRangeException(nameof(acceptedOrder));
        ProposedStart = proposedStart;
        AcceptedAt = acceptedAt;
        AcceptedOrder = acceptedOrder;
    }
}

public sealed class P20JointCivilTravelSnapshot
{
    public string ActivityInstanceId { get; }
    public string CreationIdentity { get; }
    public string SharedSegmentStableKey { get; }
    public LogicalTick ProposedStart { get; }
    public IReadOnlyList<string> PersonIds { get; }
    public IReadOnlyList<P20JointCivilTravelAssent> Assents { get; }
    public P20JointCivilTravelState State { get; }
    public bool AbortAfterLegRequested { get; }
    public string AbortCausalInputIdentity { get; }
    public LogicalTick? AbortAcceptedAt { get; }
    public long AbortAcceptedOrder { get; }
    public long Revision { get; }
    public long ExpectedLifecycleRevision { get; }
    public string Disposition { get; }
    internal P20JointCivilTravelSnapshot(P20JointCivilTravel operation, ActivityInstanceSnapshot lifecycle,
        bool failedStart)
    {
        ActivityInstanceId = operation.ActivityInstanceId;
        CreationIdentity = lifecycle.CreationIdentity;
        SharedSegmentStableKey = operation.SharedSegmentStableKey;
        ProposedStart = operation.ProposedStart;
        PersonIds = Array.AsReadOnly(operation.PersonIds.ToArray());
        Assents = Array.AsReadOnly(operation.Assents.Values.OrderBy(x => x.PersonId, StringComparer.Ordinal).ToArray());
        State = operation.Assents.Values.Any(x => !x.Accepted)
            && lifecycle.State == ActivityLifecycleState.Proposed
            ? P20JointCivilTravelState.NotFormed
            : lifecycle.State == ActivityLifecycleState.Cancelled && failedStart
                ? P20JointCivilTravelState.FailedToStart
                : lifecycle.State == ActivityLifecycleState.Proposed ? P20JointCivilTravelState.Proposed
                : lifecycle.State == ActivityLifecycleState.Scheduled ? P20JointCivilTravelState.Scheduled
                : lifecycle.State == ActivityLifecycleState.Active ? P20JointCivilTravelState.Active
                : lifecycle.State == ActivityLifecycleState.Completed ? P20JointCivilTravelState.Completed
                : lifecycle.State == ActivityLifecycleState.Interrupted ? P20JointCivilTravelState.Interrupted
                : P20JointCivilTravelState.Cancelled;
        AbortAfterLegRequested = operation.AbortAfterLegRequested;
        AbortCausalInputIdentity = operation.AbortCausalInputIdentity;
        AbortAcceptedAt = operation.AbortAcceptedAt;
        AbortAcceptedOrder = operation.AbortAcceptedOrder;
        Revision = operation.Revision;
        ExpectedLifecycleRevision = operation.ExpectedLifecycleRevision;
        Disposition = State == P20JointCivilTravelState.NotFormed ? "participant-declined" : lifecycle.Disposition;
    }
}

internal sealed class P20JointCivilTravel
{
    internal readonly string ActivityInstanceId;
    internal readonly LogicalTick ProposedStart;
    internal readonly string SharedSegmentStableKey;
    internal readonly string[] PersonIds;
    internal readonly SortedDictionary<string, P20JointCivilTravelAssent> Assents;
    internal readonly bool AbortAfterLegRequested;
    internal readonly string AbortCausalInputIdentity;
    internal readonly LogicalTick? AbortAcceptedAt;
    internal readonly long AbortAcceptedOrder;
    internal readonly long Revision;
    internal readonly long ExpectedLifecycleRevision;
    internal P20JointCivilTravel(string id, string sharedSegmentStableKey, LogicalTick proposedStart,
        IEnumerable<string> people,
        SortedDictionary<string, P20JointCivilTravelAssent> assents,
        bool abortRequested, string abortCausalInputIdentity, LogicalTick? abortAcceptedAt, long abortAcceptedOrder,
        long revision, long lifecycleRevision)
    {
        ActivityInstanceId = id; SharedSegmentStableKey = sharedSegmentStableKey; ProposedStart = proposedStart;
        PersonIds = people.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assents = new SortedDictionary<string, P20JointCivilTravelAssent>(assents, StringComparer.Ordinal);
        AbortAfterLegRequested = abortRequested;
        AbortCausalInputIdentity = abortCausalInputIdentity ?? string.Empty; Revision = revision;
        AbortAcceptedAt = abortAcceptedAt; AbortAcceptedOrder = abortAcceptedOrder;
        ExpectedLifecycleRevision = lifecycleRevision;
    }
    internal P20JointCivilTravel With(SortedDictionary<string, P20JointCivilTravelAssent> assents = null,
        bool? abortRequested = null,
        string abortCausalInputIdentity = null, LogicalTick? abortAcceptedAt = null, long? abortAcceptedOrder = null,
        long? revision = null,
        long? lifecycleRevision = null) => new P20JointCivilTravel(ActivityInstanceId,
            SharedSegmentStableKey, ProposedStart,
            PersonIds, assents ?? Assents, abortRequested ?? AbortAfterLegRequested,
            abortCausalInputIdentity ?? AbortCausalInputIdentity,
            abortAcceptedAt ?? AbortAcceptedAt, abortAcceptedOrder ?? AbortAcceptedOrder,
            revision ?? Revision, lifecycleRevision ?? ExpectedLifecycleRevision);
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
        LogicalTick proposedStart, IReadOnlyList<string> personIds,
        out P20JointCivilTravelSnapshot snapshot, out ActivityFailure failure)
    {
        snapshot = null;
        if (definition == null || definition.Id != ActivityDefinitionId || string.IsNullOrWhiteSpace(sharedSegmentStableKey)
            || personIds == null || personIds.Count != 2 || personIds.Any(string.IsNullOrWhiteSpace)
            || personIds.Distinct(StringComparer.Ordinal).Count() != 2)
        { failure = ActivityFailure.ParticipantConflict; return false; }
        if (!TryGetEarliestStart(out long earliest) || proposedStart.Value < earliest)
        { failure = ActivityFailure.InvalidInterval; return false; }
        string predictedId = composition.Store.NextProposedInstanceId;
        P20JointCivilTravel operation = new P20JointCivilTravel(predictedId, sharedSegmentStableKey,
            proposedStart, personIds,
            new SortedDictionary<string, P20JointCivilTravelAssent>(StringComparer.Ordinal),
            false, string.Empty, null, 0L, 0L, 0L);
        P20JointCivilTravelSnapshot stagedSnapshot = null;
        Dictionary<string, P20JointCivilTravel> stagedOperations = new Dictionary<string, P20JointCivilTravel>(operations, StringComparer.Ordinal)
        { [predictedId] = operation };
        if (!composition.Store.TryProposeWithParticipant(composition.Timeline, definition, creationIdentity,
            instance =>
            {
                stagedSnapshot = new P20JointCivilTravelSnapshot(operation, instance, false);
                return () => operations = stagedOperations;
            }, out _, out failure)) return false;
        snapshot = stagedSnapshot;
        return true;
    }

    /// <summary>Returns reconstruction-ready P20 owner facts in stable activity identity order.</summary>
    internal IReadOnlyList<P20JointCivilTravelSnapshot> SnapshotOwnerState()
    {
        List<P20JointCivilTravelSnapshot> result = new List<P20JointCivilTravelSnapshot>(operations.Count);
        foreach (string activityId in operations.Keys.OrderBy(x => x, StringComparer.Ordinal))
        {
            if (!TryGet(activityId, out P20JointCivilTravelSnapshot snapshot))
                throw new InvalidOperationException("P20 joint-travel owner references a missing P18 activity instance.");
            result.Add(snapshot);
        }
        return result.AsReadOnly();
    }

    /// <summary>Restores P20-owned proposal, assent, and abort facts against an already restored P18 activity store.</summary>
    internal bool TryRestoreOwnerState(IEnumerable<P20JointCivilTravelSnapshot> snapshots)
    {
        if (snapshots == null || operations.Count != 0) return false;
        Dictionary<string, P20JointCivilTravel> staged = new Dictionary<string, P20JointCivilTravel>(StringComparer.Ordinal);
        foreach (P20JointCivilTravelSnapshot snapshot in snapshots)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.ActivityInstanceId)
                || string.IsNullOrWhiteSpace(snapshot.CreationIdentity)
                || string.IsNullOrWhiteSpace(snapshot.SharedSegmentStableKey)
                || snapshot.PersonIds == null || snapshot.PersonIds.Count != 2
                || snapshot.PersonIds.Any(string.IsNullOrWhiteSpace)
                || snapshot.PersonIds.Distinct(StringComparer.Ordinal).Count() != 2
                || snapshot.Assents == null || snapshot.Assents.Count > 2
                || snapshot.Revision != snapshot.Assents.Count + (snapshot.AbortAfterLegRequested ? 1L : 0L)
                || snapshot.ExpectedLifecycleRevision < 0L
                || !composition.Store.TryGet(snapshot.ActivityInstanceId, out ActivityInstanceSnapshot instance)
                || instance.DefinitionId != ActivityDefinitionId
                || !string.Equals(instance.CreationIdentity, snapshot.CreationIdentity, StringComparison.Ordinal)
                || instance.Revision != snapshot.ExpectedLifecycleRevision
                || !Enum.IsDefined(typeof(P20JointCivilTravelState), snapshot.State)) return false;
            bool hasP18Schedule = instance.PlannedStart.HasValue;
            if ((hasP18Schedule && instance.PlannedStart.Value != snapshot.ProposedStart)
                || (hasP18Schedule && !instance.Participants.SequenceEqual(snapshot.PersonIds, StringComparer.Ordinal))
                || (!hasP18Schedule && instance.Participants.Count != 0)
                || (!hasP18Schedule && instance.State != ActivityLifecycleState.Proposed
                    && instance.State != ActivityLifecycleState.Cancelled)) return false;
            if (staged.ContainsKey(snapshot.ActivityInstanceId)) return false;

            SortedDictionary<string, P20JointCivilTravelAssent> assents =
                new SortedDictionary<string, P20JointCivilTravelAssent>(StringComparer.Ordinal);
            HashSet<string> causalIdentities = new HashSet<string>(StringComparer.Ordinal);
            HashSet<long> assentOrders = new HashSet<long>();
            if (snapshot.Assents.Any(x => x == null)) return false;
            long nextCausalOrder = 1L;
            LogicalTick? lastAcceptedAt = null;
            foreach (P20JointCivilTravelAssent assent in snapshot.Assents.OrderBy(x => x.AcceptedOrder))
            {
                if (assent == null || !snapshot.PersonIds.Contains(assent.PersonId, StringComparer.Ordinal)
                    || !assent.ProposedStart.HasValue || assent.ProposedStart.Value != snapshot.ProposedStart
                    || !assent.AcceptedAt.HasValue || assent.AcceptedOrder <= 0L
                    || assent.AcceptedAt.Value.Value >= snapshot.ProposedStart.Value
                    || assent.AcceptedAt.Value.Value > composition.Timeline.CurrentInstant.Value
                    || assent.AcceptedOrder != nextCausalOrder
                    || assent.AcceptedOrder > snapshot.Revision
                    || !causalIdentities.Add(assent.CausalInputIdentity)
                    || !assentOrders.Add(assent.AcceptedOrder)
                    || (lastAcceptedAt.HasValue && assent.AcceptedAt.Value.Value < lastAcceptedAt.Value.Value)
                    || assents.ContainsKey(assent.PersonId)) return false;
                assents.Add(assent.PersonId, new P20JointCivilTravelAssent(assent.PersonId, assent.Accepted,
                    assent.CausalInputIdentity, snapshot.ProposedStart, assent.AcceptedAt.Value, assent.AcceptedOrder));
                nextCausalOrder++;
                lastAcceptedAt = assent.AcceptedAt.Value;
            }
            if (assents.Values.Any(x => !x.Accepted)
                && (assents.Values.Count(x => !x.Accepted) != 1
                    || assents.Values.OrderBy(x => x.AcceptedOrder).Last().Accepted)) return false;
            if (snapshot.AbortAfterLegRequested)
            {
                if (string.IsNullOrWhiteSpace(snapshot.AbortCausalInputIdentity)
                    || !snapshot.AbortAcceptedAt.HasValue || snapshot.AbortAcceptedOrder <= 0L
                    || snapshot.AbortAcceptedAt.Value.Value < snapshot.ProposedStart.Value
                    || snapshot.AbortAcceptedAt.Value.Value > composition.Timeline.CurrentInstant.Value
                    || snapshot.AbortAcceptedOrder != nextCausalOrder
                    || snapshot.AbortAcceptedOrder != snapshot.Revision
                    || (lastAcceptedAt.HasValue && snapshot.AbortAcceptedAt.Value.Value < lastAcceptedAt.Value.Value)
                    || !causalIdentities.Add(snapshot.AbortCausalInputIdentity)
                    || (instance.State != ActivityLifecycleState.Active && instance.State != ActivityLifecycleState.Interrupted))
                    return false;
            }
            else if (!string.IsNullOrEmpty(snapshot.AbortCausalInputIdentity)
                || snapshot.AbortAcceptedAt.HasValue || snapshot.AbortAcceptedOrder != 0L) return false;
            if (instance.State == ActivityLifecycleState.Interrupted && !snapshot.AbortAfterLegRequested) return false;

            P20JointCivilTravel operation = new P20JointCivilTravel(snapshot.ActivityInstanceId,
                snapshot.SharedSegmentStableKey, snapshot.ProposedStart, snapshot.PersonIds, assents,
                snapshot.AbortAfterLegRequested, snapshot.AbortCausalInputIdentity,
                snapshot.AbortAcceptedAt, snapshot.AbortAcceptedOrder, snapshot.Revision,
                snapshot.ExpectedLifecycleRevision);
            bool failedStart = instance.State == ActivityLifecycleState.Cancelled
                && composition.Store.SnapshotTransitionReceipts().Any(x => x.ActivityInstanceId == snapshot.ActivityInstanceId
                    && x.Kind == ActivityTransitionKind.FailedStart);
            P20JointCivilTravelSnapshot restored = new P20JointCivilTravelSnapshot(operation, instance, failedStart);
            if (restored.State != snapshot.State || !string.Equals(restored.Disposition, snapshot.Disposition, StringComparison.Ordinal))
                return false;
            staged.Add(snapshot.ActivityInstanceId, operation);
        }
        operations = staged;
        return true;
    }

    public bool TryGet(string activityInstanceId, out P20JointCivilTravelSnapshot snapshot)
    {
        if (!operations.TryGetValue(activityInstanceId, out P20JointCivilTravel operation)
            || !composition.Store.TryGet(activityInstanceId, out ActivityInstanceSnapshot instance))
        { snapshot = null; return false; }
        bool failedStart = instance.State == ActivityLifecycleState.Cancelled
            && composition.Store.SnapshotTransitionReceipts().Any(x => x.ActivityInstanceId == activityInstanceId
                && x.Kind == ActivityTransitionKind.FailedStart);
        snapshot = new P20JointCivilTravelSnapshot(operation, instance, failedStart);
        return true;
    }

    public bool TryRecordAssent(string activityInstanceId, P20JointCivilTravelAssent assent)
    {
        if (assent == null || !operations.TryGetValue(activityInstanceId, out P20JointCivilTravel current)
            || !composition.Store.TryGet(activityInstanceId, out ActivityInstanceSnapshot instance)
            || instance.State != ActivityLifecycleState.Proposed
            || instance.Revision != current.ExpectedLifecycleRevision
            || current.Assents.Values.Any(x => !x.Accepted)
            || !current.PersonIds.Contains(assent.PersonId, StringComparer.Ordinal)
            || current.Assents.Values.Any(x => x.CausalInputIdentity == assent.CausalInputIdentity)
            || current.Assents.ContainsKey(assent.PersonId) || current.Revision == long.MaxValue) return false;
        if (!TryGetEarliestStart(out long earliest) || current.ProposedStart.Value < earliest) return false;
        SortedDictionary<string, P20JointCivilTravelAssent> assents = new SortedDictionary<string, P20JointCivilTravelAssent>(current.Assents, StringComparer.Ordinal)
        { [assent.PersonId] = new P20JointCivilTravelAssent(assent.PersonId, assent.Accepted,
            assent.CausalInputIdentity, current.ProposedStart, composition.Timeline.CurrentInstant, current.Revision + 1L) };
        P20JointCivilTravel next = current.With(assents: assents,
            revision: current.Revision + 1L,
            lifecycleRevision: current.ExpectedLifecycleRevision);
        Dictionary<string, P20JointCivilTravel> staged = new Dictionary<string, P20JointCivilTravel>(operations, StringComparer.Ordinal)
        { [activityInstanceId] = next };
        return composition.Timeline.TryCommitOwnerFacts(Array.Empty<DueWorkReference>(), () =>
        {
            if (!operations.TryGetValue(activityInstanceId, out P20JointCivilTravel latest)
                || !ReferenceEquals(latest, current)
                || !composition.Store.TryGet(activityInstanceId, out ActivityInstanceSnapshot latestActivity)
                || latestActivity.State != ActivityLifecycleState.Proposed
                || latestActivity.Revision != current.ExpectedLifecycleRevision)
                return TimelineFailure.StaleWork;
            operations = staged;
            return TimelineFailure.None;
        }, out _);
    }

    /// <summary>Both independent assents authorize one P18 open-ended commitment reservation.</summary>
    public bool TrySchedule(string activityInstanceId, out ActivityFailure failure)
    {
        if (!operations.TryGetValue(activityInstanceId, out P20JointCivilTravel current)
            || current.PersonIds.Any(id => !current.Assents.TryGetValue(id, out P20JointCivilTravelAssent assent) || !assent.Accepted)
            || !composition.Store.TryGet(activityInstanceId, out ActivityInstanceSnapshot instance)
            || instance.State != ActivityLifecycleState.Proposed || instance.Revision != current.ExpectedLifecycleRevision)
        { failure = ActivityFailure.InvalidState; return false; }
        if (instance.Revision == long.MaxValue)
        { failure = ActivityFailure.RevisionOverflow; return false; }
        if (!TryGetEarliestStart(out long earliest) || current.ProposedStart.Value < earliest)
        { failure = ActivityFailure.InvalidInterval; return false; }
        P20JointCivilTravel scheduled = current.With(lifecycleRevision: instance.Revision + 1L);
        return composition.Store.TrySchedule(composition.Timeline, activityInstanceId,
            composition.Timeline.CurrentInstant, current.ProposedStart, null, current.PersonIds,
            () => operations[activityInstanceId] = scheduled, out failure);
    }

    private bool TryGetEarliestStart(out long earliest)
    {
        long latestClosed = Math.Max(composition.Timeline.CurrentInstant.Value,
            composition.Timeline.InputsSealedThrough?.Value ?? composition.Timeline.CurrentInstant.Value);
        try { earliest = checked(latestClosed + 1L); return true; }
        catch (OverflowException) { earliest = long.MaxValue; return false; }
    }

    public bool TryCancel(string activityInstanceId, string disposition, out ActivityFailure failure)
    {
        if (!operations.ContainsKey(activityInstanceId))
        { failure = ActivityFailure.UnknownInstance; return false; }
        return composition.Store.TryCancel(composition.Timeline, activityInstanceId, disposition, out failure);
    }

    public bool TryRequestAbortAfterLeg(string activityInstanceId, long expectedRevision, string causalInputIdentity)
    {
        if (!operations.TryGetValue(activityInstanceId, out P20JointCivilTravel current)
            || current.Revision != expectedRevision
            || current.AbortAfterLegRequested || current.Revision == long.MaxValue
            || string.IsNullOrWhiteSpace(causalInputIdentity)
            || current.Assents.Values.Any(x => x.CausalInputIdentity == causalInputIdentity)
            || !composition.Store.TryGet(activityInstanceId, out ActivityInstanceSnapshot activity)
            || activity.State != ActivityLifecycleState.Active
            || activity.Revision != current.ExpectedLifecycleRevision) return false;
        P20JointCivilTravel next = current.With(abortRequested: true,
            abortCausalInputIdentity: causalInputIdentity,
            abortAcceptedAt: composition.Timeline.CurrentInstant,
            abortAcceptedOrder: current.Revision + 1L, revision: current.Revision + 1L);
        Dictionary<string, P20JointCivilTravel> staged = new Dictionary<string, P20JointCivilTravel>(operations, StringComparer.Ordinal)
        { [activityInstanceId] = next };
        return composition.Timeline.TryCommitOwnerFacts(Array.Empty<DueWorkReference>(), () =>
        {
            if (!operations.TryGetValue(activityInstanceId, out P20JointCivilTravel latest)
                || !ReferenceEquals(latest, current)
                || !composition.Store.TryGet(activityInstanceId, out ActivityInstanceSnapshot latestActivity)
                || latestActivity.State != ActivityLifecycleState.Active
                || latestActivity.Revision != current.ExpectedLifecycleRevision)
                return TimelineFailure.StaleWork;
            operations = staged;
            return TimelineFailure.None;
        }, out _);
    }

    /// <summary>Arrival is explicit. The first Person commits independently; the second shares its P8 arrival with P18 terminal facts.</summary>
    public bool TryArrive(string activityInstanceId, string personId, out ActivityFailure failure)
    {
        if (!operations.TryGetValue(activityInstanceId, out P20JointCivilTravel current)
            || !current.PersonIds.Contains(personId, StringComparer.Ordinal)
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
        if (current.ExpectedLifecycleRevision != instance.Revision
            || instance.Revision == long.MaxValue
            || instance.State != ActivityLifecycleState.Scheduled
            || !instance.Participants.SequenceEqual(current.PersonIds, StringComparer.Ordinal)
            || current.PersonIds.Any(id => !current.Assents.TryGetValue(id, out P20JointCivilTravelAssent assent) || !assent.Accepted))
        {
            prepared = new JointTransitionCommit(this, current, current, false, "scheduled-instance-stale-or-incomplete", null, null);
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
                prepared = new JointTransitionCommit(this, current, current, false, "movement-context-unavailable", null, null);
                failureDisposition = "movement-context-unavailable";
                return false;
            }
            participants.Add(new P8EJointTravelParticipant(person, context));
        }
        if (!travel.TryPrepareJointCivilLeg(participants, current.SharedSegmentStableKey,
            out PreparedP8EJointCivilLeg preparedLeg, out P8ETravelFailure travelFailure))
        {
            prepared = new JointTransitionCommit(this, current, current, false, "joint-travel-precondition-failed", null, null);
            failureDisposition = "joint-travel-precondition-failed";
            return false;
        }
        P20JointCivilTravel next = current.With(lifecycleRevision: instance.Revision + 1L);
        prepared = new JointTransitionCommit(this, current, next, true, null, preparedLeg, null);
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
        if (current.ExpectedLifecycleRevision != instance.Revision || instance.Revision == long.MaxValue) return false;
        if (terminal == ActivityLifecycleState.Cancelled
            && (instance.State == ActivityLifecycleState.Proposed || instance.State == ActivityLifecycleState.Scheduled))
        {
            prepared = new JointTransitionCommit(this, current,
                current.With(lifecycleRevision: instance.Revision + 1L), true, null, null, null);
            return true;
        }
        if ((terminal != ActivityLifecycleState.Completed && terminal != ActivityLifecycleState.Interrupted)
            || instance.State != ActivityLifecycleState.Active || terminalArrivalPersonId == null
            || (current.AbortAfterLegRequested != (terminal == ActivityLifecycleState.Interrupted))
            || !current.PersonIds.Contains(terminalArrivalPersonId, StringComparer.Ordinal)
            || !travel.TryPrepareFinalArrival(new PersonId(terminalArrivalPersonId), out PreparedP8EPersonFinalArrival arrival, out _))
            return false;
        string otherId = current.PersonIds.Single(id => id != terminalArrivalPersonId);
        if (!travel.TryGetFinalArrival(new PersonId(otherId), out HexId otherDestination)
            || !travel.TryGetPlanDestination(new PersonId(terminalArrivalPersonId), out HexId destination)
            || otherDestination != destination) return false;
        P20JointCivilTravel next = current.With(lifecycleRevision: instance.Revision + 1L);
        prepared = new JointTransitionCommit(this, current, next, true, null, null, arrival);
        return true;
    }

    private sealed class JointTransitionCommit : IActivityLifecycleTransitionCommit
    {
        private readonly P20JointCivilTravelOwner owner;
        private readonly P20JointCivilTravel expected;
        private readonly P20JointCivilTravel next;
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
            PreparedP8EPersonFinalArrival finalArrival)
        { this.owner = owner; this.expected = expected; this.next = next; this.allowed = allowed; this.failure = failure;
            this.jointStart = jointStart; this.finalArrival = finalArrival; }
        public void CommitStarted()
        { jointStart.InstallPrepared(); owner.operations[next.ActivityInstanceId] = next; }
        public void CommitFailedStart() { }
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
