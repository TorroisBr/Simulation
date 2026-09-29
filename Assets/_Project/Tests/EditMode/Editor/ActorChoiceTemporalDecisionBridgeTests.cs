using System.Linq;
using NUnit.Framework;

public sealed class ActorChoiceTemporalDecisionBridgeTests
{
    private const string ProfileId = "intraday-sellgoods-v1";

    [Test]
    public void SuccessfulYieldBindsExactInputToCSequenceWithoutUsingTimelineSequence()
    {
        PersonStore people = new PersonStore();
        ActorChoiceStore choices = new ActorChoiceStore(people);
        ActorChoiceInput input = Capture(choices, "one", new PersonId("person-a"), 91L, 50L);
        ActorDecisionRequestState requests = new ActorDecisionRequestState();
        ActorChoiceTemporalDecisionBridge bridge = new ActorChoiceTemporalDecisionBridge(choices, requests, ProfileId);

        Assert.That(bridge.AfterSuccessfulAdvance(new LogicalTick(50L), out var admitted, out string failure), Is.True, failure);
        Assert.That(admitted, Has.Count.EqualTo(1));
        Assert.That(admitted[0].Input.InputId, Is.EqualTo(input.InputId));
        Assert.That(admitted[0].Request.Instant, Is.EqualTo(new LogicalTick(50L)));
        Assert.That(admitted[0].Request.BoundarySequence, Is.EqualTo(1L));
        Assert.That(admitted[0].OriginatingBoundarySequence, Is.EqualTo(1L));
        Assert.That(admitted[0].Request.Id, Is.Not.EqualTo(input.TemporalCapture.AcceptedInput.Sequence.ToString()));

        ActorDecisionRequestReceipt bound = requests.Snapshot().Single();
        Assert.That(bound.Kind, Is.EqualTo(ActorDecisionRequestReceiptKind.RequestBound));
        Assert.That(bound.InputId, Is.EqualTo(input.InputId.Value));
        Assert.That(bound.SourceSequence, Is.EqualTo(input.InputSequence));
        Assert.That(bound.Outcome, Is.EqualTo(input.TemporalCapture.AcceptedInput.InputId));

        Assert.That(bridge.AfterSuccessfulAdvance(new LogicalTick(50L), out var replay, out failure), Is.True, failure);
        Assert.That(replay.Single().Request.Id, Is.EqualTo(admitted[0].Request.Id));
        Assert.That(requests.Count, Is.EqualTo(1));
    }

    [Test]
    public void SelectsOldestDueInputAndDefersOtherSameActorInputsOnce()
    {
        ActorChoiceStore choices = new ActorChoiceStore(new PersonStore());
        PersonId actor = new PersonId("person-a");
        ActorChoiceInput first = Capture(choices, "first", actor, 12L, 10L);
        ActorChoiceInput second = Capture(choices, "second", actor, 13L, 10L);
        ActorDecisionRequestState requests = new ActorDecisionRequestState();
        ActorChoiceTemporalDecisionBridge bridge = new ActorChoiceTemporalDecisionBridge(choices, requests, ProfileId);

        Assert.That(bridge.AfterSuccessfulAdvance(new LogicalTick(10L), out var admitted, out string failure), Is.True, failure);
        Assert.That(admitted, Has.Count.EqualTo(1));
        Assert.That(admitted[0].Input.InputId, Is.EqualTo(first.InputId));
        Assert.That(admitted[0].OriginatingBoundarySequence, Is.LessThan(
            requests.Snapshot().Single(receipt => receipt.Kind == ActorDecisionRequestReceiptKind.RequestBound
                && receipt.InputId == second.InputId.Value).BoundarySequence));
        ActorDecisionRequestReceipt deferred = requests.Snapshot().Single(receipt =>
            receipt.Kind == ActorDecisionRequestReceiptKind.Deferred && receipt.InputId == second.InputId.Value);
        Assert.That(deferred.Instant, Is.EqualTo(new LogicalTick(10L)));
        Assert.That(deferred.DeferralReason, Is.EqualTo(ActorDecisionDeferralReason.DecisionBoundaryAlreadyUsed));
        Assert.That(choices.TryGet(second.InputId, out ActorChoiceInput stillPending), Is.True);
        Assert.That(stillPending.Status, Is.EqualTo(ActorChoiceInputStatus.Pending));

        Assert.That(bridge.AfterSuccessfulAdvance(new LogicalTick(10L), out var replay, out failure), Is.True, failure);
        Assert.That(replay.Single().Input.InputId, Is.EqualTo(first.InputId));
        Assert.That(requests.Count, Is.EqualTo(3));
    }

    [Test]
    public void FutureInputCannotMaskDueInputAndNewLaterInputRetriggersOldestDeferredInput()
    {
        ActorChoiceStore choices = new ActorChoiceStore(new PersonStore());
        PersonId actor = new PersonId("person-a");
        ActorChoiceInput future = Capture(choices, "future", actor, 2L, 20L);
        ActorChoiceInput first = Capture(choices, "first", actor, 9L, 10L);
        ActorDecisionRequestState requests = new ActorDecisionRequestState();
        ActorChoiceTemporalDecisionBridge bridge = new ActorChoiceTemporalDecisionBridge(choices, requests, ProfileId);

        Assert.That(bridge.AfterSuccessfulAdvance(new LogicalTick(10L), out var admitted, out string failure), Is.True, failure);
        Assert.That(admitted.Select(entry => entry.Input.InputId), Is.EqualTo(new[] { first.InputId }));
        Assert.That(requests.Snapshot().All(receipt => receipt.InputId != future.InputId.Value), Is.True);

        ActorChoiceInput sameBoundarySecond = Capture(choices, "second", actor, 10L, 10L);
        Assert.That(bridge.AfterSuccessfulAdvance(new LogicalTick(10L), out var sameBoundary, out failure), Is.True, failure);
        Assert.That(sameBoundary.Single().Input.InputId, Is.EqualTo(first.InputId));
        Assert.That(requests.Snapshot().Any(receipt => receipt.Kind == ActorDecisionRequestReceiptKind.Deferred
            && receipt.InputId == sameBoundarySecond.InputId.Value), Is.True);

        Assert.That(choices.TryRecordTemporalRejected(first.InputId,
            new ActorChoiceTemporalBoundaryReference(ProfileId, new LogicalTick(10L), "source-boundary-10", 0L),
            "terminal-first", ActorChoiceFailure.ActionUnavailable, out ActorChoiceStoreFailureCode storeFailure), Is.True, storeFailure.ToString());
        ActorChoiceInput later = Capture(choices, "later", actor, 31L, 20L);

        Assert.That(bridge.AfterSuccessfulAdvance(new LogicalTick(20L), out var laterRequests, out failure), Is.True, failure);
        Assert.That(laterRequests, Has.Count.EqualTo(1));
        Assert.That(laterRequests[0].Input.InputId, Is.EqualTo(sameBoundarySecond.InputId));
        Assert.That(laterRequests[0].Request.Instant, Is.EqualTo(new LogicalTick(20L)));
        Assert.That(laterRequests[0].Input.TemporalCapture.TargetInstant, Is.EqualTo(new LogicalTick(10L)));
        Assert.That(laterRequests[0].OriginatingBoundarySequence, Is.EqualTo(2L));

        ActorDecisionRequestReceipt observed = requests.Snapshot().Single(receipt =>
            receipt.Kind == ActorDecisionRequestReceiptKind.TriggerObserved && receipt.InputId == sameBoundarySecond.InputId.Value);
        Assert.That(observed.TriggerId, Is.EqualTo(future.TemporalCapture.AcceptedInput.InputId));
        Assert.That(observed.SourceSequence, Is.EqualTo(future.TemporalCapture.AcceptedInput.Sequence));
        Assert.That(observed.BoundarySequence, Is.Not.EqualTo(future.TemporalCapture.AcceptedInput.Sequence));
    }

    [Test]
    public void ReplayedTriggerDoesNotReadmitTerminalInput()
    {
        ActorChoiceStore choices = new ActorChoiceStore(new PersonStore());
        PersonId actor = new PersonId("person-a");
        ActorChoiceInput first = Capture(choices, "first", actor, 9L, 10L);
        ActorChoiceInput deferred = Capture(choices, "deferred", actor, 10L, 10L);
        ActorDecisionRequestState requests = new ActorDecisionRequestState();
        ActorChoiceTemporalDecisionBridge bridge = new ActorChoiceTemporalDecisionBridge(choices, requests, ProfileId);

        Assert.That(bridge.AfterSuccessfulAdvance(new LogicalTick(10L), out var bound, out string failure), Is.True, failure);
        ActorChoiceTemporalDecisionRequest boundRequest = bound.Single();
        Assert.That(boundRequest.Input.InputId, Is.EqualTo(first.InputId));
        ActorChoiceInput later = Capture(choices, "later", actor, 31L, 20L);
        Assert.That(bridge.TryObserveMeaningfulTrigger(actor, later.TemporalCapture.AcceptedInput.InputId,
            new LogicalTick(20L), later.TemporalCapture.AcceptedInput.InputId, 0L,
            later.TemporalCapture.AcceptedInput.Sequence, out ActorChoiceTemporalDecisionRequest triggered, out failure), Is.True, failure);
        Assert.That(triggered, Is.Not.Null);
        Assert.That(triggered.Input.InputId, Is.EqualTo(deferred.InputId));
        Assert.That(requests.TryReconcileTerminal("terminal-op", triggered.Request.Id, deferred.InputId.Value,
            actor, new LogicalTick(20L), "proposal-20", "CommittedRejected", out _), Is.True);

        Assert.That(choices.TryRecordTemporalRejected(deferred.InputId,
            new ActorChoiceTemporalBoundaryReference(ProfileId, new LogicalTick(20L),
                later.TemporalCapture.AcceptedInput.InputId, 0L),
            "terminal-p11-op", ActorChoiceFailure.ActionUnavailable, out ActorChoiceStoreFailureCode storeFailure),
            Is.True, storeFailure.ToString());
        Assert.That(bridge.TryObserveMeaningfulTrigger(actor, later.TemporalCapture.AcceptedInput.InputId,
            new LogicalTick(20L), later.TemporalCapture.AcceptedInput.InputId, 0L,
            later.TemporalCapture.AcceptedInput.Sequence, out ActorChoiceTemporalDecisionRequest replay, out failure), Is.True, failure);
        Assert.That(replay, Is.Null);
        Assert.That(requests.Snapshot().Count(receipt => receipt.Kind == ActorDecisionRequestReceiptKind.TriggerObserved), Is.EqualTo(1));
        Assert.That(boundRequest.Request.Id, Is.Not.EqualTo(triggered.Request.Id));
    }

    [Test]
    public void ProvenUncommittedAttemptReplaysTheSameCRequestAndProposal()
    {
        ActorChoiceStore choices = new ActorChoiceStore(new PersonStore());
        PersonId actor = new PersonId("person-a");
        ActorChoiceInput input = Capture(choices, "retry", actor, 7L, 10L);
        ActorDecisionRequestState requests = new ActorDecisionRequestState();
        ActorChoiceTemporalDecisionBridge bridge = new ActorChoiceTemporalDecisionBridge(choices, requests, ProfileId);

        Assert.That(bridge.AfterSuccessfulAdvance(new LogicalTick(10L), out var initial, out string failure), Is.True, failure);
        ActorChoiceTemporalDecisionRequest original = initial.Single();
        Assert.That(choices.TryRecordTemporalDispatchStarted(input.InputId,
            new ActorChoiceTemporalBoundaryReference(ProfileId, new LogicalTick(10L),
                input.TemporalCapture.AcceptedInput.InputId, 0L), "dispatch-once", original.Request.Id,
            out ActorChoiceStoreFailureCode storeFailure), Is.True, storeFailure.ToString());
        Assert.That(requests.TryRecordRetryableProposal("retryable-once", original.Request.Id,
            input.InputId.Value, actor, new LogicalTick(10L), "proposal-stable", "ProvenNoInstall", out _), Is.True);

        Assert.That(bridge.AfterSuccessfulAdvance(new LogicalTick(20L), out var retried, out failure), Is.True, failure);
        Assert.That(retried, Has.Count.EqualTo(1));
        Assert.That(retried[0].Request.Id, Is.EqualTo(original.Request.Id));
        Assert.That(retried[0].RetryableProposalId, Is.EqualTo("proposal-stable"));
        Assert.That(choices.TryGet(input.InputId, out ActorChoiceInput consumed), Is.True);
        Assert.That(consumed.Status, Is.EqualTo(ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt));
        Assert.That(requests.NextBoundarySequence, Is.EqualTo(2L));
    }

    [Test]
    public void RejectsRequestBoundReceiptForDifferentActor()
    {
        ActorChoiceStore choices = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput input = Capture(choices, "mismatched-actor", new PersonId("person-a"), 4L, 5L);
        ActorDecisionRequestState requests = new ActorDecisionRequestState();
        Assert.That(requests.TryBindInput("wrong-actor", input.InputId.Value, new PersonId("person-b"),
            input.TemporalCapture.TargetInstant, ProfileId, input.InputSequence,
            input.TemporalCapture.AcceptedInput.InputId, 0L, out _), Is.True);
        ActorChoiceTemporalDecisionBridge bridge = new ActorChoiceTemporalDecisionBridge(choices, requests, ProfileId);

        Assert.That(bridge.AfterSuccessfulAdvance(input.TemporalCapture.TargetInstant,
            out _, out string failure), Is.False);
        Assert.That(failure, Is.EqualTo("P18-C request receipt did not reconstruct its exact request identity."));
    }

    [Test]
    public void RejectsRequestBoundReceiptWithWrongProfileOrTargetInstant()
    {
        ActorChoiceStore choices = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput input = Capture(choices, "mismatched-profile", new PersonId("person-a"), 7L, 5L);
        ActorDecisionRequestState requests = new ActorDecisionRequestState();
        Assert.That(requests.TryBindInput("wrong-profile", input.InputId.Value, input.PersonId,
            input.TemporalCapture.TargetInstant, "other-profile", input.InputSequence,
            input.TemporalCapture.AcceptedInput.InputId, 0L, out _), Is.True);
        ActorChoiceTemporalDecisionBridge bridge = new ActorChoiceTemporalDecisionBridge(choices, requests, ProfileId);

        Assert.That(bridge.AfterSuccessfulAdvance(input.TemporalCapture.TargetInstant,
            out _, out string failure), Is.False);
        Assert.That(failure, Is.EqualTo("P18-C request receipt did not reconstruct its exact request identity."));

        choices = new ActorChoiceStore(new PersonStore());
        input = Capture(choices, "mismatched-instant", new PersonId("person-a"), 8L, 5L);
        requests = new ActorDecisionRequestState();
        Assert.That(requests.TryBindInput("wrong-instant", input.InputId.Value, input.PersonId,
            new LogicalTick(6L), ProfileId, input.InputSequence,
            input.TemporalCapture.AcceptedInput.InputId, 0L, out _), Is.True);
        bridge = new ActorChoiceTemporalDecisionBridge(choices, requests, ProfileId);

        Assert.That(bridge.AfterSuccessfulAdvance(new LogicalTick(6L),
            out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo("P18-C request receipt did not reconstruct its exact request identity."));
    }

    private static ActorChoiceInput Capture(ActorChoiceStore store, string suffix, PersonId actor,
        long timelineSequence, long targetTick)
    {
        ActorChoiceTemporalInputOwner owner = new ActorChoiceTemporalInputOwner(store, ProfileId);
        ActorChoiceTemporalCommand command = new ActorChoiceTemporalCommand(
            "world-command-" + suffix, actor, "sell-goods/v1");
        TimelineInputReference reference = new TimelineInputReference(timelineSequence,
            "timeline-input-" + suffix, ActorChoiceTemporalInputOwner.CommandKind, command.Encode(),
            new LogicalTick(targetTick));
        Assert.That(owner.TryPrepare(reference, out ITimelineInputCommit prepared,
            out TimelineFailure prepareFailure), Is.True, prepareFailure.ToString());
        Assert.That(prepared.TryCommit(out TimelineFailure commitFailure), Is.True, commitFailure.ToString());
        return store.PendingInputs.Single(input => input.WorldCommandId == "world-command-" + suffix);
    }
}
