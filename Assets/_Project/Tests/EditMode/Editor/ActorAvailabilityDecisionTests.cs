using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class ActorAvailabilityDecisionTests
{
    private sealed class KnowledgePort : IKnowledgeDecisionSnapshotPort
    {
        public int Reads;
        public bool TryRead(PersonId actor, LogicalTick instant, out KnowledgeDecisionSnapshot snapshot)
        {
            Reads++;
            snapshot = new KnowledgeDecisionSnapshot(actor, instant, 4,
                new Dictionary<string, string> { { "known-target", "target-1" } }, new[] { "act" });
            return true;
        }
    }
    private sealed class Planner : IActorDecisionPlanner
    {
        public readonly List<string> Order = new List<string>();
        public ActorDecisionProposalKind Kind = ActorDecisionProposalKind.Instantaneous;
        public bool TryPlan(ActorDecisionRequest request, KnowledgeDecisionSnapshot snapshot, out ActorDecisionProposal proposal)
        {
            Order.Add(request.Actor.Value);
            Assert.That(snapshot.KnownFacts.ContainsKey("hidden-current-truth"), Is.False);
            proposal = new ActorDecisionProposal("proposal:" + request.Actor.Value, request.Id, request.Actor,
                "act", "target-1", Kind, request.Instant,
                Kind == ActorDecisionProposalKind.TimedActivity ? new LogicalTick(request.Instant.Value) : (LogicalTick?)null,
                snapshot.KnowledgeRevision, Kind == ActorDecisionProposalKind.TimedActivity ? 3L : (long?)null);
            return true;
        }
    }
    private sealed class Executor : ICurrentTruthProposalExecutor
    {
        public readonly List<ActorDecisionProposal> Executed = new List<ActorDecisionProposal>();
        public bool TryExecute(ActorDecisionProposal proposal, out string disposition)
        { Executed.Add(proposal); disposition = "owner-revalidated"; return true; }
    }

    private static SimulationCalendar Calendar() => new SimulationCalendar(new CalendarDefinition(2, 2, 3));
    private static ActivityLifecycleComposition Setup(out ActivityInstanceSnapshot instance)
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("decision-world");
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(store, Calendar(), new LogicalTick(0));
        Assert.That(store.TryPropose(new ActivityDefinition("activity", "v1"), "activity-command", out instance, out _), Is.True);
        return composition;
    }

    [Test]
    public void ReceiptsAreAppendOnlyImmutableSequenceOrderedAndCloneWithCursorState()
    {
        ActivityInstanceSnapshot instance;
        ActivityLifecycleComposition composition = Setup(out instance);
        ActivityLifecycleStore store = composition.Store;
        Assert.That(store.TrySchedule(composition.Timeline, instance.Id, new LogicalTick(0), new LogicalTick(16), new LogicalTick(4),
            new[] { "person-b", "person-a" }, out _), Is.True);
        ActivityLifecycleStore clone = store.Clone();
        IReadOnlyList<ActivityTransitionReceipt> receipts = clone.SnapshotTransitionReceipts();
        Assert.That(receipts.Count, Is.EqualTo(1));
        Assert.That(receipts[0].Sequence, Is.EqualTo(1));
        Assert.That(receipts[0].Id, Is.Not.Empty);
        Assert.That(receipts[0].ActivityInstanceId, Is.EqualTo(instance.Id));
        Assert.That(receipts[0].ActivityInstanceId, Is.Not.EqualTo("person-a"));
        Assert.That(receipts[0].ParticipantIds, Is.EqualTo(new[] { "person-a", "person-b" }));
        Assert.That(clone.NextTransitionSequence, Is.EqualTo(store.NextTransitionSequence));
        Assert.That(clone.SnapshotTransitionReceipts(1), Is.Empty);
        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(24), out _), Is.True);
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(24), out _), Is.True);
        IReadOnlyList<ActivityTransitionReceipt> afterAdvance = store.SnapshotTransitionReceipts(1);
        Assert.That(afterAdvance.Count, Is.EqualTo(2));
        Assert.That(afterAdvance[0].Kind, Is.EqualTo(ActivityTransitionKind.Start));
        Assert.That(afterAdvance[1].Kind, Is.EqualTo(ActivityTransitionKind.Complete));
        Assert.That(afterAdvance[0].Sequence, Is.LessThan(afterAdvance[1].Sequence));
        Assert.That(clone.SnapshotTransitionReceipts(1), Is.Empty);
    }

    [Test]
    public void PostAdvanceHandoffUsesKnowledgeAndOwnerExecutorAndDoesNotRepeatBoundary()
    {
        ActivityInstanceSnapshot instance;
        ActivityLifecycleComposition composition = Setup(out instance);
        Assert.That(composition.Store.TrySchedule(composition.Timeline, instance.Id, new LogicalTick(0), new LogicalTick(100), null,
            new[] { "person-a", "person-b" }, out _), Is.True);
        Assert.That(composition.Store.TryCancel(composition.Timeline, instance.Id, "cancelled", out _), Is.True);
        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(25), out _), Is.True);
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(10), out _), Is.True);

        ActorDecisionCoordinator coordinator = new ActorDecisionCoordinator(composition.Store, composition.Timeline);
        KnowledgePort knowledge = new KnowledgePort(); Planner planner = new Planner(); Executor executor = new Executor();
        planner.Kind = ActorDecisionProposalKind.TimedActivity;
        Assert.That(coordinator.AfterSuccessfulAdvance(knowledge, planner, executor, out IReadOnlyList<ActorDecisionProposal> proposals), Is.True);
        Assert.That(proposals.Count, Is.EqualTo(2));
        Assert.That(planner.Order, Is.EqualTo(new[] { "person-a", "person-b" }));
        Assert.That(proposals[0].ScheduledStart, Is.EqualTo(new LogicalTick(26)));
        Assert.That(proposals[0].DecisionInstant, Is.EqualTo(new LogicalTick(10)));
        Assert.That(proposals[0].Actor.Value, Is.EqualTo("person-a"));
        Assert.That(proposals[0].Id, Is.Not.EqualTo(proposals[0].Actor.Value));
        Assert.That(executor.Executed.Count, Is.EqualTo(2));
        Assert.That(knowledge.Reads, Is.EqualTo(2));
        Assert.That(coordinator.AfterSuccessfulAdvance(knowledge, planner, executor, out IReadOnlyList<ActorDecisionProposal> repeated), Is.True);
        Assert.That(repeated, Is.Empty);
        Assert.That(executor.Executed.Count, Is.EqualTo(2));
    }

    [Test]
    public void FailedAdvanceLeavesReceiptCursorAndQueuedCausalWorkAvailable()
    {
        ActivityInstanceSnapshot instance;
        ActivityLifecycleComposition composition = Setup(out instance);
        Assert.That(composition.Store.TrySchedule(composition.Timeline, instance.Id, new LogicalTick(0), new LogicalTick(10), null,
            new[] { "person-a" }, out _), Is.True);
        ActorDecisionCoordinator coordinator = new ActorDecisionCoordinator(composition.Store, composition.Timeline);
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(10), out TimelineFailure failed), Is.False);
        Assert.That(failed, Is.EqualTo(TimelineFailure.InputNotSealed));
        Assert.That(coordinator.ReceiptCursor, Is.Zero);
        Assert.That(coordinator.PendingRequestCount, Is.Zero);
        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(10), out _), Is.True);
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(10), out _), Is.True);
        KnowledgePort knowledge = new KnowledgePort(); Planner planner = new Planner(); Executor executor = new Executor();
        Assert.That(coordinator.AfterSuccessfulAdvance(knowledge, planner, executor, out _), Is.True);
        Assert.That(coordinator.ReceiptCursor, Is.EqualTo(composition.Store.NextTransitionSequence - 1));
    }
}
