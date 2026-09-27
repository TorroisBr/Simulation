using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public enum ActorDecisionProposalKind { Instantaneous = 1, TimedActivity = 2 }

/// <summary>Stable causal request keyed by PersonId and a committed semantic boundary.</summary>
public sealed class ActorDecisionRequest
{
    public string Id { get; }
    public PersonId Actor { get; }
    public LogicalTick Instant { get; }
    public string BoundaryId { get; }
    public long BoundaryRevision { get; }
    internal ActorDecisionRequest(PersonId actor, LogicalTick instant, string boundaryId, long revision)
    {
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        if (string.IsNullOrWhiteSpace(boundaryId)) throw new ArgumentException("Boundary identity is required.", nameof(boundaryId));
        Instant = instant; BoundaryId = boundaryId; BoundaryRevision = revision;
        Id = SpatialStableKey.Encode(actor.Value, instant.Value.ToString(CultureInfo.InvariantCulture), boundaryId, revision.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>Read-only decision inputs. Implementations must expose only facts permitted by the actor's Knowledge.</summary>
public sealed class KnowledgeDecisionSnapshot
{
    public PersonId Actor { get; }
    public LogicalTick Instant { get; }
    public long KnowledgeRevision { get; }
    public IReadOnlyDictionary<string, string> KnownFacts { get; }
    public IReadOnlyList<string> AvailableActionIds { get; }
    public KnowledgeDecisionSnapshot(PersonId actor, LogicalTick instant, long knowledgeRevision,
        IDictionary<string, string> knownFacts, IEnumerable<string> availableActionIds)
    {
        Actor = actor ?? throw new ArgumentNullException(nameof(actor)); Instant = instant; KnowledgeRevision = knowledgeRevision;
        KnownFacts = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
            new SortedDictionary<string, string>(knownFacts ?? new Dictionary<string, string>(), StringComparer.Ordinal));
        AvailableActionIds = Array.AsReadOnly((availableActionIds ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }
}

/// <summary>Semantic proposal data; it carries no runtime/Unity object as its identity.</summary>
public sealed class ActorDecisionProposal
{
    public string Id { get; }
    public string RequestId { get; }
    public PersonId Actor { get; }
    public string ActionId { get; }
    public string TargetId { get; }
    public ActorDecisionProposalKind Kind { get; }
    public LogicalTick DecisionInstant { get; }
    public LogicalTick? ScheduledStart { get; }
    public long KnowledgeRevision { get; }
    public long? DurationTicks { get; }

    public ActorDecisionProposal(string id, string requestId, PersonId actor, string actionId, string targetId,
        ActorDecisionProposalKind kind, LogicalTick decisionInstant, LogicalTick? scheduledStart,
        long knowledgeRevision, long? durationTicks)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(actionId))
            throw new ArgumentException("Proposal, request and action semantic identities are required.");
        Id = id; RequestId = requestId; Actor = actor ?? throw new ArgumentNullException(nameof(actor)); ActionId = actionId;
        TargetId = targetId ?? string.Empty; Kind = kind; DecisionInstant = decisionInstant; ScheduledStart = scheduledStart;
        KnowledgeRevision = knowledgeRevision; DurationTicks = durationTicks;
        if (kind == ActorDecisionProposalKind.TimedActivity && (!durationTicks.HasValue || durationTicks.Value < 0))
            throw new ArgumentException("Timed proposals require a nonnegative duration.");
        if (kind == ActorDecisionProposalKind.Instantaneous && scheduledStart.HasValue)
            throw new ArgumentException("Instantaneous proposals execute at their decision instant.");
    }
}

public interface IKnowledgeDecisionSnapshotPort
{
    bool TryRead(PersonId actor, LogicalTick instant, out KnowledgeDecisionSnapshot snapshot);
}

public interface IActorDecisionPlanner
{
    bool TryPlan(ActorDecisionRequest request, KnowledgeDecisionSnapshot snapshot, out ActorDecisionProposal proposal);
}

/// <summary>Owning domain resolves semantic IDs and revalidates current truth before mutation.</summary>
public interface ICurrentTruthProposalExecutor
{
    bool TryExecute(ActorDecisionProposal proposal, out string disposition);
}

public sealed class ActorDecisionCoordinator
{
    private readonly ActivityLifecycleStore lifecycle;
    private readonly SimulationTimeline timeline;
    private readonly SortedDictionary<string, ActorDecisionRequest> pending = new SortedDictionary<string, ActorDecisionRequest>(StringComparer.Ordinal);
    private readonly HashSet<string> processedBoundaries = new HashSet<string>(StringComparer.Ordinal);
    private long receiptCursor;

    public ActorDecisionCoordinator(ActivityLifecycleStore lifecycle, SimulationTimeline timeline, long receiptCursor = 0)
    {
        this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        this.timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        if (!lifecycle.IsBoundToTimeline(timeline)) throw new ArgumentException("Coordinator must use the lifecycle's authoritative timeline.");
        if (receiptCursor < 0 || receiptCursor >= lifecycle.NextTransitionSequence) throw new ArgumentOutOfRangeException(nameof(receiptCursor));
        this.receiptCursor = receiptCursor;
    }

    public long ReceiptCursor => receiptCursor;
    public int PendingRequestCount => pending.Count;

    /// <summary>Call only after TryAdvanceTo returned true; failures must leave receipts and requests unconsumed.</summary>
    public bool AfterSuccessfulAdvance(IKnowledgeDecisionSnapshotPort knowledge, IActorDecisionPlanner planner,
        ICurrentTruthProposalExecutor executor, out IReadOnlyList<ActorDecisionProposal> proposals)
    {
        if (knowledge == null) throw new ArgumentNullException(nameof(knowledge));
        if (planner == null) throw new ArgumentNullException(nameof(planner));
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        proposals = Array.Empty<ActorDecisionProposal>();
        IReadOnlyList<ActivityTransitionReceipt> receipts = lifecycle.SnapshotTransitionReceipts(receiptCursor);
        foreach (ActivityTransitionReceipt receipt in receipts)
            foreach (string participant in receipt.ParticipantIds)
            {
                PersonId actor;
                try { actor = new PersonId(participant); } catch (ArgumentException) { continue; }
                var request = new ActorDecisionRequest(actor, receipt.Instant, receipt.Id, receipt.ActivityRevision);
                if (!pending.ContainsKey(request.Id)) pending.Add(request.Id, request);
            }

        List<ActorDecisionProposal> emitted = new List<ActorDecisionProposal>();
        LogicalTick now = timeline.CurrentInstant;
        LogicalTick sealedThrough = timeline.InputsSealedThrough ?? now;
        long boundary = Math.Max(now.Value, sealedThrough.Value);
        foreach (ActorDecisionRequest request in pending.Values.OrderBy(r => r.Actor.Value, StringComparer.Ordinal).ThenBy(r => r.Instant.Value).ThenBy(r => r.Id, StringComparer.Ordinal).ToArray())
        {
            string actorBoundary = SpatialStableKey.Encode(request.Actor.Value, now.Value.ToString(CultureInfo.InvariantCulture));
            if (processedBoundaries.Contains(actorBoundary)) { pending.Remove(request.Id); continue; }
            if (request.Instant.Value > now.Value) continue;
            if (!lifecycle.IsAvailable(request.Actor.Value, now)) { pending.Remove(request.Id); continue; }
            if (!knowledge.TryRead(request.Actor, now, out KnowledgeDecisionSnapshot snapshot) || snapshot == null)
            { pending.Remove(request.Id); processedBoundaries.Add(actorBoundary); continue; }
            if (!planner.TryPlan(request, snapshot, out ActorDecisionProposal candidate) || candidate == null)
            { pending.Remove(request.Id); processedBoundaries.Add(actorBoundary); continue; }
            LogicalTick? scheduledStart = null;
            if (candidate.Kind == ActorDecisionProposalKind.TimedActivity)
            {
                try { scheduledStart = new LogicalTick(checked(boundary + 1L)); }
                catch (OverflowException) { pending.Remove(request.Id); processedBoundaries.Add(actorBoundary); continue; }
            }
            ActorDecisionProposal proposal = new ActorDecisionProposal(candidate.Id, request.Id, request.Actor, candidate.ActionId,
                candidate.TargetId, candidate.Kind, now, scheduledStart, snapshot.KnowledgeRevision, candidate.DurationTicks);
            executor.TryExecute(proposal, out _);
            emitted.Add(proposal);
            pending.Remove(request.Id);
            processedBoundaries.Add(actorBoundary);
        }
        if (receipts.Count > 0) receiptCursor = receipts[receipts.Count - 1].Sequence;
        proposals = Array.AsReadOnly(emitted.ToArray());
        return true;
    }
}
