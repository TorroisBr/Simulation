using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public enum P20SyntheticOperationDisposition
{
    Proposed = 0, Scheduled = 1, NotFormed = 2, Started = 3,
    FailedToStart = 4, Cancelled = 5
}

public sealed class P20ActorDecision
{
    public string PersonId { get; }
    public bool Accepted { get; }
    public string KnowledgeBoundaryId { get; }
    public string CausalBoundaryId { get; }

    public P20ActorDecision(string personId, bool accepted, string knowledgeBoundaryId, string causalBoundaryId)
    {
        if (string.IsNullOrWhiteSpace(personId)) throw new ArgumentException("Person identity is required.", nameof(personId));
        if (string.IsNullOrWhiteSpace(knowledgeBoundaryId)) throw new ArgumentException("Knowledge boundary identity is required.", nameof(knowledgeBoundaryId));
        if (string.IsNullOrWhiteSpace(causalBoundaryId)) throw new ArgumentException("Causal boundary identity is required.", nameof(causalBoundaryId));
        PersonId = personId; Accepted = accepted; KnowledgeBoundaryId = knowledgeBoundaryId; CausalBoundaryId = causalBoundaryId;
    }
}

public sealed class P20SyntheticParticipantResult
{
    public string PersonId { get; }
    public string Result { get; }
    public string AppliedIdentity { get; }

    internal P20SyntheticParticipantResult(string personId, string result, string appliedIdentity)
    { PersonId = personId; Result = result; AppliedIdentity = appliedIdentity; }
}

public sealed class P20SyntheticOperationSnapshot
{
    public string ActivityInstanceId { get; }
    public IReadOnlyList<string> RequiredPersonIds { get; }
    public IReadOnlyList<P20ActorDecision> Decisions { get; }
    public P20SyntheticOperationDisposition Disposition { get; }
    public string FailureDisposition { get; }
    public long ExpectedLifecycleRevision { get; }
    public IReadOnlyList<P20SyntheticParticipantResult> AppliedResults { get; }

    internal P20SyntheticOperationSnapshot(P20SyntheticOperation source)
    {
        ActivityInstanceId = source.ActivityInstanceId;
        RequiredPersonIds = Array.AsReadOnly(source.RequiredPersonIds.ToArray());
        Decisions = Array.AsReadOnly(source.Decisions.Values.OrderBy(x => x.PersonId, StringComparer.Ordinal).ToArray());
        Disposition = source.Disposition; FailureDisposition = source.FailureDisposition;
        ExpectedLifecycleRevision = source.ExpectedLifecycleRevision;
        AppliedResults = Array.AsReadOnly(source.AppliedResults.ToArray());
    }
}

/// <summary>Read-only current-truth/effect preparation port for the synthetic proving operation.</summary>
public interface IP20SyntheticOperationRules
{
    bool IsCurrentlyEligible(string personId, ActivityInstanceSnapshot activity, LogicalTick instant, out string revision);
    bool TryPrepareResult(string personId, ActivityInstanceSnapshot activity, LogicalTick instant,
        out string result, out string revision);
}

internal sealed class P20SyntheticOperation
{
    public readonly string ActivityInstanceId;
    public readonly string[] RequiredPersonIds;
    public readonly SortedDictionary<string, P20ActorDecision> Decisions;
    public readonly P20SyntheticOperationDisposition Disposition;
    public readonly string FailureDisposition;
    public readonly long ExpectedLifecycleRevision;
    public readonly P20SyntheticParticipantResult[] AppliedResults;

    public P20SyntheticOperation(string id, IEnumerable<string> required, SortedDictionary<string, P20ActorDecision> decisions,
        P20SyntheticOperationDisposition disposition, string failureDisposition, long expectedLifecycleRevision,
        IEnumerable<P20SyntheticParticipantResult> results)
    {
        ActivityInstanceId = id; RequiredPersonIds = required.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Decisions = new SortedDictionary<string, P20ActorDecision>(decisions, StringComparer.Ordinal);
        Disposition = disposition; FailureDisposition = failureDisposition ?? string.Empty;
        ExpectedLifecycleRevision = expectedLifecycleRevision; AppliedResults = results.OrderBy(x => x.PersonId, StringComparer.Ordinal).ToArray();
    }

    public P20SyntheticOperation With(SortedDictionary<string, P20ActorDecision> decisions = null,
        P20SyntheticOperationDisposition? disposition = null, string failure = null, long? revision = null,
        IEnumerable<P20SyntheticParticipantResult> results = null) => new P20SyntheticOperation(ActivityInstanceId,
            RequiredPersonIds, decisions ?? Decisions, disposition ?? Disposition, failure ?? FailureDisposition,
            revision ?? ExpectedLifecycleRevision, results ?? AppliedResults);
}

/// <summary>
/// Bounded two-Person synthetic proof. This is not a general activity/group/workflow framework.
/// Lifecycle owns temporal facts and commitments; this owner retains independent responses and synthetic results.
/// </summary>
public sealed class P20SyntheticOperationOwner
{
    private readonly ActivityLifecycleStore lifecycle;
    private readonly SimulationTimeline timeline;
    private readonly IP20SyntheticOperationRules rules;
    private Dictionary<string, P20SyntheticOperation> operations = new Dictionary<string, P20SyntheticOperation>(StringComparer.Ordinal);

    public P20SyntheticOperationOwner(ActivityLifecycleComposition composition, IP20SyntheticOperationRules rules)
    {
        if (composition == null) throw new ArgumentNullException(nameof(composition));
        this.lifecycle = composition.Store; timeline = composition.Timeline;
        this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
        lifecycle.BindTransitionParticipant(new TransitionParticipant(this));
    }

    private P20SyntheticOperationOwner(ActivityLifecycleStore lifecycle, SimulationTimeline timeline,
        IP20SyntheticOperationRules rules, Dictionary<string, P20SyntheticOperation> restored)
    {
        this.lifecycle = lifecycle; this.timeline = timeline; this.rules = rules;
        operations = restored; lifecycle.BindTransitionParticipant(new TransitionParticipant(this));
    }

    public P20SyntheticOperationOwner Clone(ActivityLifecycleComposition clonedComposition)
    {
        if (clonedComposition == null) throw new ArgumentNullException(nameof(clonedComposition));
        Dictionary<string, P20SyntheticOperation> copy = new Dictionary<string, P20SyntheticOperation>(operations, StringComparer.Ordinal);
        return new P20SyntheticOperationOwner(clonedComposition.Store, clonedComposition.Timeline, rules, copy);
    }

    public bool TryCreate(ActivityDefinition definition, string creationIdentity, IReadOnlyList<string> requiredPersonIds,
        out P20SyntheticOperationSnapshot snapshot, out ActivityFailure failure)
    {
        snapshot = null;
        if (requiredPersonIds == null || requiredPersonIds.Count != 2
            || requiredPersonIds.Any(string.IsNullOrWhiteSpace)
            || requiredPersonIds.Distinct(StringComparer.Ordinal).Count() != 2)
        { failure = ActivityFailure.ParticipantConflict; return false; }
        if (!lifecycle.TryPropose(definition, creationIdentity, out ActivityInstanceSnapshot proposed, out failure)) return false;
        P20SyntheticOperation operation = new P20SyntheticOperation(proposed.Id, requiredPersonIds,
            new SortedDictionary<string, P20ActorDecision>(StringComparer.Ordinal), P20SyntheticOperationDisposition.Proposed,
            string.Empty, proposed.Revision, Array.Empty<P20SyntheticParticipantResult>());
        operations.Add(proposed.Id, operation); snapshot = new P20SyntheticOperationSnapshot(operation); return true;
    }

    public bool TryGet(string activityInstanceId, out P20SyntheticOperationSnapshot snapshot)
    {
        snapshot = operations.TryGetValue(activityInstanceId, out P20SyntheticOperation operation)
            ? new P20SyntheticOperationSnapshot(operation) : null;
        return snapshot != null;
    }

    /// <summary>Stores one actor's trusted game/UI response with its own Knowledge and causal boundary identities.</summary>
    public bool TryRecordDecision(string activityInstanceId, P20ActorDecision decision)
    {
        if (decision == null || !operations.TryGetValue(activityInstanceId, out P20SyntheticOperation current)
            || current.Disposition != P20SyntheticOperationDisposition.Proposed
            || !current.RequiredPersonIds.Contains(decision.PersonId, StringComparer.Ordinal)
            || current.Decisions.ContainsKey(decision.PersonId)) return false;
        SortedDictionary<string, P20ActorDecision> decisions = new SortedDictionary<string, P20ActorDecision>(current.Decisions, StringComparer.Ordinal)
        { [decision.PersonId] = decision };
        operations[activityInstanceId] = current.With(decisions: decisions); return true;
    }

    /// <summary>Explicit close only: no timer or implicit timeout is owned by this proof.</summary>
    public bool TryCloseFormation(string activityInstanceId, LogicalTick start, LogicalTick? duration,
        out P20SyntheticOperationSnapshot snapshot, out ActivityFailure failure)
    {
        snapshot = null;
        if (!operations.TryGetValue(activityInstanceId, out P20SyntheticOperation current)
            || current.Disposition != P20SyntheticOperationDisposition.Proposed
            || !lifecycle.TryGet(activityInstanceId, out ActivityInstanceSnapshot activity)
            || activity.State != ActivityLifecycleState.Proposed || activity.Revision != current.ExpectedLifecycleRevision)
        { failure = ActivityFailure.InvalidState; return false; }

        string reason = null;
        if (current.RequiredPersonIds.Any(person => !current.Decisions.TryGetValue(person, out P20ActorDecision d) || !d.Accepted))
            reason = "formation-incomplete-or-declined";
        long earliest;
        try { earliest = checked(Math.Max(timeline.CurrentInstant.Value, timeline.InputsSealedThrough?.Value ?? timeline.CurrentInstant.Value) + 1L); }
        catch (OverflowException) { reason = "start-tick-overflow"; earliest = long.MaxValue; }
        if (reason == null && start.Value < earliest) reason = "start-not-after-current-and-sealed-inputs";
        if (reason == null)
        {
            foreach (string person in current.RequiredPersonIds)
            {
                if (!rules.IsCurrentlyEligible(person, activity, start, out _))
                { reason = "participant-stale-or-unavailable"; break; }
            }
        }
        if (reason != null)
        {
            P20SyntheticOperation notFormed = current.With(disposition: P20SyntheticOperationDisposition.NotFormed, failure: reason);
            operations[activityInstanceId] = notFormed; snapshot = new P20SyntheticOperationSnapshot(notFormed);
            failure = reason == "start-tick-overflow" ? ActivityFailure.InvalidInterval : ActivityFailure.None;
            return reason != "start-tick-overflow";
        }

        long scheduledRevision;
        try { scheduledRevision = checked(activity.Revision + 1); }
        catch (OverflowException)
        {
            P20SyntheticOperation notFormed = current.With(disposition: P20SyntheticOperationDisposition.NotFormed, failure: "activity-revision-overflow");
            operations[activityInstanceId] = notFormed; snapshot = new P20SyntheticOperationSnapshot(notFormed);
            failure = ActivityFailure.RevisionOverflow; return false;
        }
        P20SyntheticOperation scheduledOperation = current.With(
            disposition: P20SyntheticOperationDisposition.Scheduled, revision: scheduledRevision);
        bool scheduled = lifecycle.TrySchedule(timeline, activityInstanceId, timeline.CurrentInstant, start, duration,
            current.RequiredPersonIds, () => operations[activityInstanceId] = scheduledOperation, out failure);
        if (!scheduled)
        {
            P20SyntheticOperation notFormed = current.With(disposition: P20SyntheticOperationDisposition.NotFormed,
                failure: failure == ActivityFailure.ParticipantConflict ? "participant-conflict" : "schedule-rejected");
            operations[activityInstanceId] = notFormed; snapshot = new P20SyntheticOperationSnapshot(notFormed); return false;
        }
        snapshot = new P20SyntheticOperationSnapshot(operations[activityInstanceId]); return true;
    }

    public bool TryCancel(string activityInstanceId, string disposition, out ActivityFailure failure) =>
        lifecycle.TryCancel(timeline, activityInstanceId, disposition, out failure);

    private bool TryPrepareStart(ActivityInstanceSnapshot instance, LogicalTick instant,
        out IActivityLifecycleTransitionCommit prepared, out string failureDisposition)
    {
        prepared = null; failureDisposition = "p20-start-validation-failed";
        if (!operations.TryGetValue(instance.Id, out P20SyntheticOperation current))
        { failureDisposition = null; return true; }
        if (current.Disposition != P20SyntheticOperationDisposition.Scheduled
            || current.ExpectedLifecycleRevision != instance.Revision
            || instance.State != ActivityLifecycleState.Scheduled
            || instance.Participants.Count != current.RequiredPersonIds.Length
            || !instance.Participants.SequenceEqual(current.RequiredPersonIds, StringComparer.Ordinal))
        {
            long failedRevision = checked(instance.Revision + 1);
            P20SyntheticOperation failed = current.With(disposition: P20SyntheticOperationDisposition.FailedToStart,
                failure: "scheduled-instance-stale-or-incomplete", revision: failedRevision,
                results: Array.Empty<P20SyntheticParticipantResult>());
            prepared = new P20TransitionCommit(this, current, failed, false, "scheduled-instance-stale-or-incomplete");
            failureDisposition = "scheduled-instance-stale-or-incomplete"; return false;
        }

        List<P20SyntheticParticipantResult> results = new List<P20SyntheticParticipantResult>(current.RequiredPersonIds.Length);
        foreach (string person in current.RequiredPersonIds)
        {
            ActivityParticipantCommitment commitment = lifecycle.GetCommitment(person);
            if (!current.Decisions.TryGetValue(person, out P20ActorDecision decision) || !decision.Accepted
                || commitment == null || commitment.ActivityInstanceId != instance.Id
                || !lifecycle.IsAvailableForActivity(person, instant, instance.Id)
                || !rules.IsCurrentlyEligible(person, instance, instant, out _)
                || !rules.TryPrepareResult(person, instance, instant, out string result, out _))
            {
                const string failure = "participant-missing-stale-or-ineligible";
                P20SyntheticOperation failed = current.With(disposition: P20SyntheticOperationDisposition.FailedToStart,
                    failure: failure, revision: checked(instance.Revision + 1), results: Array.Empty<P20SyntheticParticipantResult>());
                prepared = new P20TransitionCommit(this, current, failed, false, failure);
                failureDisposition = failure; return false;
            }
            string appliedId = SpatialStableKey.Encode(instance.Id, "result", person);
            results.Add(new P20SyntheticParticipantResult(person, result, appliedId));
        }
        long activeRevision;
        try { activeRevision = checked(instance.Revision + 1); }
        catch (OverflowException)
        { prepared = new P20TransitionCommit(this, current, current, false, "activity-revision-overflow"); failureDisposition = "activity-revision-overflow"; return false; }
        P20SyntheticOperation next = current.With(disposition: P20SyntheticOperationDisposition.Started,
            revision: activeRevision, results: results);
        P20SyntheticOperation failedStart = current.With(disposition: P20SyntheticOperationDisposition.FailedToStart,
            failure: "failed-to-start", revision: activeRevision, results: Array.Empty<P20SyntheticParticipantResult>());
        prepared = new P20TransitionCommit(this, next, failedStart, true, null); failureDisposition = null; return true;
    }

    private bool TryPrepareTerminal(ActivityInstanceSnapshot instance,
        ActivityLifecycleState terminal, ActivityTransitionKind kind, LogicalTick instant, string disposition,
        out IActivityLifecycleTransitionCommit prepared)
    {
        prepared = null;
        if (!operations.TryGetValue(instance.Id, out P20SyntheticOperation current)) return true;
        P20SyntheticOperationDisposition target = terminal == ActivityLifecycleState.Cancelled
            ? P20SyntheticOperationDisposition.Cancelled : current.Disposition;
        long terminalRevision;
        try { terminalRevision = checked(instance.Revision + 1); }
        catch (OverflowException) { prepared = null; return true; }
        prepared = new P20TransitionCommit(this, current.With(disposition: target,
            failure: string.IsNullOrEmpty(disposition) ? "cancelled" : disposition,
            revision: terminalRevision), current, false, null);
        return true;
    }

    private sealed class P20TransitionCommit : IActivityLifecycleTransitionCommit
    {
        private readonly P20SyntheticOperationOwner owner;
        private readonly P20SyntheticOperation replacement;
        private readonly P20SyntheticOperation failedReplacement;
        private readonly bool allowed;
        private readonly string failure;
        public bool StartAllowed => allowed;
        public string FailureDisposition => failure;

        public P20TransitionCommit(P20SyntheticOperationOwner owner, P20SyntheticOperation replacement,
            P20SyntheticOperation failedReplacement, bool allowed, string failure)
        {
            this.owner = owner; this.replacement = replacement; this.allowed = allowed; this.failure = failure;
            this.failedReplacement = failedReplacement;
        }

        public void CommitStarted() => owner.operations[replacement.ActivityInstanceId] = replacement;
        public void CommitFailedStart()
        {
            if (failedReplacement != null) owner.operations[failedReplacement.ActivityInstanceId] = failedReplacement;
        }
        public void CommitTerminal() => owner.operations[replacement.ActivityInstanceId] = replacement;
    }

    private sealed class TransitionParticipant : IActivityLifecycleTransitionParticipant
    {
        private readonly P20SyntheticOperationOwner owner;
        public TransitionParticipant(P20SyntheticOperationOwner owner) { this.owner = owner; }
        public bool TryPrepareStart(ActivityInstanceSnapshot instance, LogicalTick instant,
            out IActivityLifecycleTransitionCommit prepared, out string failureDisposition) =>
            owner.TryPrepareStart(instance, instant, out prepared, out failureDisposition);
        public bool TryPrepareTerminal(ActivityInstanceSnapshot instance, ActivityLifecycleState terminal,
            ActivityTransitionKind kind, LogicalTick instant, string disposition,
            out IActivityLifecycleTransitionCommit prepared) =>
            owner.TryPrepareTerminal(instance, terminal, kind, instant, disposition, out prepared);
    }
}
