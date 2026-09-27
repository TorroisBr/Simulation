using System;
using System.Collections.Generic;
using System.Reflection;
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
        public readonly List<string> RequestIds = new List<string>();
        public ActorDecisionProposalKind Kind = ActorDecisionProposalKind.Instantaneous;
        public bool TryPlan(ActorDecisionRequest request, KnowledgeDecisionSnapshot snapshot, out ActorDecisionProposal proposal)
        {
            Order.Add(request.Actor.Value);
            RequestIds.Add(request.Id);
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
        public readonly Queue<ProposalExecutionResult> Results = new Queue<ProposalExecutionResult>();
        public ProposalExecutionResult Execute(ActorDecisionProposal proposal)
        {
            Executed.Add(proposal);
            return Results.Count > 0 ? Results.Dequeue() : new ProposalExecutionResult(ProposalExecutionStatus.CommittedAccepted, "owner-revalidated");
        }
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
        Assert.That(proposals.Count, Is.EqualTo(4));
        Assert.That(planner.Order, Is.EqualTo(new[] { "person-a", "person-a", "person-b", "person-b" }));
        Assert.That(planner.RequestIds[0], Is.Not.EqualTo(planner.RequestIds[1]));
        Assert.That(proposals[0].ScheduledStart, Is.EqualTo(new LogicalTick(26)));
        Assert.That(proposals[0].DecisionInstant, Is.EqualTo(new LogicalTick(10)));
        Assert.That(proposals[0].Actor.Value, Is.EqualTo("person-a"));
        Assert.That(proposals[0].Id, Is.Not.EqualTo(proposals[0].Actor.Value));
        Assert.That(executor.Executed.Count, Is.EqualTo(4));
        Assert.That(knowledge.Reads, Is.EqualTo(4));
        Assert.That(coordinator.AfterSuccessfulAdvance(knowledge, planner, executor, out IReadOnlyList<ActorDecisionProposal> repeated), Is.True);
        Assert.That(repeated, Is.Empty);
        Assert.That(executor.Executed.Count, Is.EqualTo(4));
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
        Assert.That(knowledge.Reads, Is.Zero);
        Assert.That(executor.Executed, Is.Empty);
    }

    [Test]
    public void SameActorSameInstantReceiptsRemainDistinctRequestsAndAreBothHandled()
    {
        ActivityInstanceSnapshot instance;
        ActivityLifecycleComposition composition = Setup(out instance);
        Assert.That(composition.Store.TrySchedule(composition.Timeline, instance.Id, new LogicalTick(0), new LogicalTick(100), null,
            new[] { "person-a" }, out _), Is.True);
        Assert.That(composition.Store.TryCancel(composition.Timeline, instance.Id, "cancelled", out _), Is.True);
        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(10), out _), Is.True);
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(0), out _), Is.True);

        ActorDecisionCoordinator coordinator = new ActorDecisionCoordinator(composition.Store, composition.Timeline);
        KnowledgePort knowledge = new KnowledgePort(); Planner planner = new Planner(); Executor executor = new Executor();
        Assert.That(coordinator.AfterSuccessfulAdvance(knowledge, planner, executor, out IReadOnlyList<ActorDecisionProposal> proposals), Is.True);
        Assert.That(proposals.Count, Is.EqualTo(2));
        Assert.That(planner.Order, Is.EqualTo(new[] { "person-a", "person-a" }));
        Assert.That(planner.RequestIds[0], Is.Not.EqualTo(planner.RequestIds[1]));
        Assert.That(executor.Executed.Count, Is.EqualTo(2));
        Assert.That(coordinator.AfterSuccessfulAdvance(knowledge, planner, executor, out IReadOnlyList<ActorDecisionProposal> repeated), Is.True);
        Assert.That(repeated, Is.Empty);
        Assert.That(executor.Executed.Count, Is.EqualTo(2));
    }

    [Test]
    public void UncommittedStaleExecutionRetainsRequestAndRetriesSameProposalIdWhileCommittedRejectionConsumesIt()
    {
        ActivityInstanceSnapshot instance;
        ActivityLifecycleComposition composition = Setup(out instance);
        Assert.That(composition.Store.TrySchedule(composition.Timeline, instance.Id, new LogicalTick(0), new LogicalTick(100), null,
            new[] { "person-a" }, out _), Is.True);
        Assert.That(composition.Store.TryCancel(composition.Timeline, instance.Id, "cancelled", out _), Is.True);
        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(10), out _), Is.True);
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(0), out _), Is.True);

        ActorDecisionCoordinator coordinator = new ActorDecisionCoordinator(composition.Store, composition.Timeline);
        KnowledgePort knowledge = new KnowledgePort(); Planner planner = new Planner(); Executor executor = new Executor();
        executor.Results.Enqueue(new ProposalExecutionResult(ProposalExecutionStatus.UncommittedRetryable, "stale-source-uncommitted"));
        executor.Results.Enqueue(new ProposalExecutionResult(ProposalExecutionStatus.CommittedRejected, "stale-target-rejected"));
        executor.Results.Enqueue(new ProposalExecutionResult(ProposalExecutionStatus.UncommittedRetryable, "still-uncommitted"));

        Assert.That(coordinator.AfterSuccessfulAdvance(knowledge, planner, executor, out IReadOnlyList<ActorDecisionProposal> first), Is.True);
        Assert.That(first, Is.Empty);
        Assert.That(coordinator.PendingRequestCount, Is.EqualTo(2));
        string retryId = executor.Executed[0].Id;

        Assert.That(coordinator.AfterSuccessfulAdvance(knowledge, planner, executor, out IReadOnlyList<ActorDecisionProposal> second), Is.True);
        Assert.That(second.Count, Is.EqualTo(1));
        Assert.That(second[0].Id, Is.EqualTo(retryId));
        Assert.That(coordinator.PendingRequestCount, Is.EqualTo(1));
        Assert.That(executor.Executed[1].Id, Is.EqualTo(retryId));

        Assert.That(coordinator.AfterSuccessfulAdvance(knowledge, planner, executor, out IReadOnlyList<ActorDecisionProposal> third), Is.True);
        Assert.That(third.Count, Is.EqualTo(1));
        Assert.That(coordinator.PendingRequestCount, Is.Zero);
        Assert.That(executor.Executed[2].Id, Is.Not.EqualTo(retryId));
        Assert.That(executor.Executed[3].Id, Is.EqualTo(executor.Executed[2].Id));
    }

    [Test]
    public void RequestStateAllocatesIndependentCSequenceAndIdempotentlyRecordsCorrelations()
    {
        ActorDecisionRequestState state = new ActorDecisionRequestState();
        PersonId actor = new PersonId("person-a");
        Assert.That(state.TryBindInput("bind-op", "actor-choice-1", actor, new LogicalTick(20), "profile-x", 91,
            "input-receipt", 4, out ActorDecisionRequestReceipt bound), Is.True);
        Assert.That(bound.BoundarySequence, Is.EqualTo(1));
        Assert.That(bound.RequestId, Is.Not.EqualTo("actor-choice-1"));
        Assert.That(state.TryBindInput("bind-op", "actor-choice-1", actor, new LogicalTick(20), "profile-x", 91,
            "input-receipt", 4, out ActorDecisionRequestReceipt duplicate), Is.True);
        Assert.That(duplicate.RequestId, Is.EqualTo(bound.RequestId));
        string triggerRequestId = new ActorDecisionRequest(actor, new LogicalTick(25), "activity-boundary", 2, 8).Id;
        Assert.That(state.TryObserveTrigger("trigger-op", triggerRequestId, "lifecycle-receipt", actor,
            new LogicalTick(25), "activity-boundary", 8, 123, "actor-choice-1", out ActorDecisionRequestReceipt trigger), Is.True);
        Assert.That(trigger.BoundarySequence, Is.EqualTo(2));
        Assert.That(trigger.SourceSequence, Is.EqualTo(123));
        Assert.That(trigger.BoundarySequence, Is.Not.EqualTo(trigger.SourceSequence));
        Assert.That(state.TryObserveTrigger("trigger-redelivery", triggerRequestId, "lifecycle-receipt", actor,
            new LogicalTick(25), "activity-boundary", 8, 123, "actor-choice-1", out ActorDecisionRequestReceipt redelivered), Is.True);
        Assert.That(redelivered.BoundarySequence, Is.EqualTo(trigger.BoundarySequence));
        Assert.That(state.NextBoundarySequence, Is.EqualTo(3));
        Assert.That(state.TryDefer("defer-op", trigger.RequestId, trigger.InputId, actor, trigger.Instant,
            trigger.BoundaryId, trigger.BoundaryRevision, trigger.SourceSequence,
            ActorDecisionDeferralReason.DecisionBoundaryAlreadyUsed, out ActorDecisionRequestReceipt deferred), Is.True);
        Assert.That(deferred.BoundarySequence, Is.EqualTo(trigger.BoundarySequence));
        Assert.That(state.TryDefer("defer-op", trigger.RequestId, trigger.InputId, actor, trigger.Instant,
            trigger.BoundaryId, trigger.BoundaryRevision, trigger.SourceSequence,
            ActorDecisionDeferralReason.DecisionBoundaryAlreadyUsed, out _), Is.True);
        Assert.That(state.TryDefer("defer-op", trigger.RequestId, trigger.InputId, actor, trigger.Instant,
            trigger.BoundaryId, trigger.BoundaryRevision, trigger.SourceSequence,
            ActorDecisionDeferralReason.TemporarilyUnavailable, out _), Is.False);
        ActorDecisionRequestState clone = state.Clone();
        Assert.That(clone.NextBoundarySequence, Is.EqualTo(state.NextBoundarySequence));
        Assert.That(clone.Snapshot().Count, Is.EqualTo(state.Snapshot().Count));
        Assert.That(state.ValidateInvariants(), Is.Empty);
        Assert.That(clone.ValidateInvariants(), Is.Empty);
        Assert.That(ActorDecisionRequestState.TryRestore(state.Snapshot(), state.NextBoundarySequence,
            out ActorDecisionRequestState restored), Is.True);
        Assert.That(restored.Snapshot().Count, Is.EqualTo(state.Snapshot().Count));
        Assert.That(restored.ValidateInvariants(), Is.Empty);
    }

    [Test]
    public void RequestStateRejectsOrphanAndMismatchedCausalReceipts()
    {
        ActorDecisionRequestState state = new ActorDecisionRequestState();
        PersonId actor = new PersonId("person-a");
        Assert.That(state.TryDefer("orphan-defer", "missing-request", "input-a", actor, new LogicalTick(20),
            "boundary", 0, 0, ActorDecisionDeferralReason.DecisionBoundaryAlreadyUsed, out _), Is.False);
        Assert.That(state.TryRecordRetryableProposal("orphan-retry", "missing-request", "input-a", actor,
            new LogicalTick(20), "proposal", "uncommitted", out _), Is.False);

        Assert.That(state.TryBindInput("bind", "input-a", actor, new LogicalTick(20), "profile", 7,
            "accepted-input-receipt", 2, out ActorDecisionRequestReceipt bound), Is.True);
        Assert.That(state.TryDefer("wrong-actor", bound.RequestId, "input-a", new PersonId("person-b"),
            new LogicalTick(20), "input-a", 2, 7, ActorDecisionDeferralReason.DecisionBoundaryAlreadyUsed, out _), Is.False);
        Assert.That(state.TryDefer("wrong-input", bound.RequestId, "input-b", actor,
            new LogicalTick(20), "input-a", 2, 7, ActorDecisionDeferralReason.DecisionBoundaryAlreadyUsed, out _), Is.False);
        Assert.That(state.TryDefer("wrong-instant", bound.RequestId, "input-a", actor,
            new LogicalTick(21), "input-a", 2, 7, ActorDecisionDeferralReason.DecisionBoundaryAlreadyUsed, out _), Is.False);
        Assert.That(state.TryRecordRetryableProposal("wrong-retry", bound.RequestId, "input-a", new PersonId("person-b"),
            new LogicalTick(20), "proposal", "uncommitted", out _), Is.False);

        ActorDecisionRequestReceipt orphan = (ActorDecisionRequestReceipt)Activator.CreateInstance(
            typeof(ActorDecisionRequestReceipt), BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { 2L, bound.BoundarySequence, ActorDecisionRequestReceiptKind.Deferred, "orphan", bound.RequestId,
                "other-input", null, actor, new LogicalTick(20), "input-a", 2L, 7L,
                (ActorDecisionDeferralReason?)ActorDecisionDeferralReason.DecisionBoundaryAlreadyUsed, null, null, null }, null);
        Assert.That(ActorDecisionRequestState.TryRestore(new[] { bound, orphan }, state.NextBoundarySequence, out _), Is.False);
    }

    [Test]
    public void TerminalReconciliationIsIdempotentAndCannotConflictForOneProposal()
    {
        ActorDecisionRequestState state = new ActorDecisionRequestState();
        PersonId actor = new PersonId("person-a");
        Assert.That(state.TryBindInput("bind", "input-a", actor, new LogicalTick(20), "profile", 7,
            "accepted-input-receipt", 2, out ActorDecisionRequestReceipt bound), Is.True);
        Assert.That(state.TryRecordRetryableProposal("retry", bound.RequestId, "input-a", actor,
            new LogicalTick(20), "proposal-a", "proven-uncommitted", out _), Is.True);
        Assert.That(state.TryReconcileTerminal("terminal", bound.RequestId, "input-a", actor,
            new LogicalTick(20), "proposal-a", "committed", out ActorDecisionRequestReceipt terminal), Is.True);
        Assert.That(state.TryReconcileTerminal("terminal-replay", bound.RequestId, "input-a", actor,
            new LogicalTick(20), "proposal-a", "committed", out ActorDecisionRequestReceipt replay), Is.True);
        Assert.That(replay.ReceiptSequence, Is.EqualTo(terminal.ReceiptSequence));
        Assert.That(state.TryReconcileTerminal("terminal-conflict", bound.RequestId, "input-a", actor,
            new LogicalTick(20), "proposal-a", "rejected", out _), Is.False);
        Assert.That(state.TryRecordRetryableProposal("retry-after-terminal", bound.RequestId, "input-a", actor,
            new LogicalTick(20), "proposal-a", "uncommitted", out _), Is.False);
        Assert.That(state.ValidateInvariants(), Is.Empty);
    }

    [Test]
    public void OneLifecycleReceiptCanTriggerDistinctParticipantRequests()
    {
        ActorDecisionRequestState state = new ActorDecisionRequestState();
        PersonId firstActor = new PersonId("person-a");
        PersonId secondActor = new PersonId("person-b");
        Assert.That(state.TryBindInput("bind-a", "input-a", firstActor, new LogicalTick(5), "profile", 1,
            "capture-a", 1, out _), Is.True);
        Assert.That(state.TryBindInput("bind-b", "input-b", secondActor, new LogicalTick(5), "profile", 2,
            "capture-b", 1, out _), Is.True);
        string firstRequestId = new ActorDecisionRequest(firstActor, new LogicalTick(10), "lifecycle-receipt", 3, 4).Id;
        string secondRequestId = new ActorDecisionRequest(secondActor, new LogicalTick(10), "lifecycle-receipt", 4, 4).Id;
        Assert.That(state.TryObserveTrigger("trigger-a", firstRequestId, "lifecycle-receipt", firstActor,
            new LogicalTick(10), "lifecycle-receipt", 4, 22, "input-a", out ActorDecisionRequestReceipt first), Is.True);
        Assert.That(state.TryObserveTrigger("trigger-b", secondRequestId, "lifecycle-receipt", secondActor,
            new LogicalTick(10), "lifecycle-receipt", 4, 22, "input-b", out ActorDecisionRequestReceipt second), Is.True);
        Assert.That(first.BoundarySequence, Is.Not.EqualTo(second.BoundarySequence));
        Assert.That(state.ValidateInvariants(), Is.Empty);
    }

    [Test]
    public void DeferredRequestCannotRetryUntilFreshTriggerCreatesNewRequestIdentity()
    {
        ActorDecisionRequestState state = new ActorDecisionRequestState();
        PersonId actor = new PersonId("person-a");
        Assert.That(state.TryBindInput("bind", "input-a", actor, new LogicalTick(10), "profile", 5,
            "accepted-input", 1, out ActorDecisionRequestReceipt bound), Is.True);
        Assert.That(state.TryDefer("defer", bound.RequestId, bound.InputId, actor, bound.Instant,
            bound.BoundaryId, bound.BoundaryRevision, bound.SourceSequence,
            ActorDecisionDeferralReason.DecisionBoundaryAlreadyUsed, out _), Is.True);
        Assert.That(state.TryRecordRetryableProposal("premature-retry", bound.RequestId, bound.InputId, actor,
            bound.Instant, "proposal-1", "proven-uncommitted", out _), Is.False);

        string laterRequestId = new ActorDecisionRequest(actor, new LogicalTick(20), "later-trigger", 2, 3).Id;
        Assert.That(state.TryObserveTrigger("later-trigger-op", laterRequestId, "later-trigger-receipt", actor,
            new LogicalTick(20), "later-trigger", 3, 9, bound.InputId, out ActorDecisionRequestReceipt later), Is.True);
        Assert.That(later.RequestId, Is.Not.EqualTo(bound.RequestId));
        Assert.That(state.TryRecordRetryableProposal("retry-new-request", later.RequestId, later.InputId, actor,
            later.Instant, "proposal-2", "proven-uncommitted", out _), Is.True);
        Assert.That(state.ValidateInvariants(), Is.Empty);

        ActorDecisionRequestReceipt malformedRetry = (ActorDecisionRequestReceipt)Activator.CreateInstance(
            typeof(ActorDecisionRequestReceipt), BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { 3L, bound.BoundarySequence, ActorDecisionRequestReceiptKind.RetryableProposal,
                "malformed-retry", bound.RequestId, bound.InputId, null, actor, bound.Instant, bound.RequestId,
                0L, 0L, null, "proposal-1", "proven-uncommitted", null }, null);
        Assert.That(ActorDecisionRequestState.TryRestore(new[] { bound,
            (ActorDecisionRequestReceipt)Activator.CreateInstance(typeof(ActorDecisionRequestReceipt),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { 2L, bound.BoundarySequence, ActorDecisionRequestReceiptKind.Deferred, "defer", bound.RequestId,
                    bound.InputId, null, actor, bound.Instant, bound.BoundaryId, bound.BoundaryRevision, bound.SourceSequence,
                    (ActorDecisionDeferralReason?)ActorDecisionDeferralReason.DecisionBoundaryAlreadyUsed, null, null, null }, null),
            malformedRetry }, 2L, out _), Is.False);
    }
}
