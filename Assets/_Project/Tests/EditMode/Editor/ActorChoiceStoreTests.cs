using System;
using System.Reflection;
using NUnit.Framework;

public sealed class ActorChoiceStoreTests
{
    [Test]
    public void CapturesStableInputsAndReturnsSameActorChoicesInFifoOrder()
    {
        ActorChoiceStore store = CreateStore();
        PersonId actor = new PersonId("person-merchant");
        ActorChoiceInput first = Capture(store, "world-command-1", actor, "sell-goods-a", 3L);
        Capture(store, "world-command-2", new PersonId("person-other"), "sell-goods-b", 3L);
        ActorChoiceInput second = Capture(store, "world-command-3", actor, "sell-goods-c", 4L);

        Assert.That(first.InputId.Value, Is.EqualTo("actor-choice-000001"));
        Assert.That(first.InputSequence, Is.EqualTo(1L));
        Assert.That(first.WorldCommandId, Is.EqualTo("world-command-1"));
        Assert.That(first.PersonId, Is.EqualTo(actor));
        Assert.That(first.ActionDefinitionId, Is.EqualTo("sell-goods-a"));
        Assert.That(first.Origin, Is.EqualTo(WorldCommandOrigin.System));
        Assert.That(first.Authority, Is.EqualTo(WorldCommandAuthorityMode.Request));
        Assert.That(first.CapturedAbsoluteDay, Is.EqualTo(3L));
        Assert.That(store.TryGetNextPendingForActor(actor, out ActorChoiceInput next), Is.True);
        Assert.That(next.InputId, Is.EqualTo(first.InputId));

        Assert.That(store.TryMarkDispatchStarted(
            first.InputId, 5L, 0, "decision-1", out ActorChoiceStoreFailureCode startFailure),
            Is.True, startFailure.ToString());
        Assert.That(store.TryGetNextPendingForActor(actor, out next), Is.True);
        Assert.That(next.InputId, Is.EqualTo(second.InputId));
        Assert.That(store.PendingInputs, Has.Count.EqualTo(2));
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void DeferredAndRejectedChoiceHasOneTerminalDisposition()
    {
        ActorChoiceStore store = CreateStore();
        ActorChoiceInput input = Capture(store, "world-command-1", new PersonId("merchant"));

        Assert.That(store.TryDefer(
            input.InputId, 1L, 2, ActorChoiceDeferralReason.Traveling,
            out ActorChoiceStoreFailureCode deferFailure), Is.True, deferFailure.ToString());
        Assert.That(store.TryReject(
            input.InputId, 2L, 1, ActorChoiceFailure.ActionUnavailable,
            out ActorChoiceStoreFailureCode rejectFailure), Is.True, rejectFailure.ToString());

        Assert.That(store.TryGet(input.InputId, out ActorChoiceInput rejected), Is.True);
        Assert.That(rejected.Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(rejected.Dispositions, Has.Count.EqualTo(2));
        Assert.That(rejected.Dispositions[0].Kind, Is.EqualTo(ActorChoiceDispositionKind.Deferred));
        Assert.That(rejected.Dispositions[0].DeferralReason, Is.EqualTo(ActorChoiceDeferralReason.Traveling));
        Assert.That(rejected.Dispositions[1].Failure, Is.EqualTo(ActorChoiceFailure.ActionUnavailable));

        AssertTransitionRejected(store.TryDefer(
            input.InputId, 3L, 1, ActorChoiceDeferralReason.ScheduledDirective, out ActorChoiceStoreFailureCode deferAgain),
            deferAgain);
        AssertTransitionRejected(store.TryReject(
            input.InputId, 3L, 1, ActorChoiceFailure.ActorUnavailable, out ActorChoiceStoreFailureCode rejectAgain),
            rejectAgain);
        AssertTransitionRejected(store.TryMarkDispatchStarted(
            input.InputId, 3L, 1, "decision-late", out ActorChoiceStoreFailureCode dispatchAfterReject),
            dispatchAfterReject);
        Assert.That(store.TryGet(input.InputId, out ActorChoiceInput stillRejected), Is.True);
        Assert.That(stillRejected.Dispositions, Has.Count.EqualTo(2));
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void DispatchStartIsOneShotAndReturnedOrThrownOutcomeIsTerminal()
    {
        ActorChoiceStore store = CreateStore();
        ActorChoiceInput returned = Capture(store, "world-command-1", new PersonId("merchant-a"));
        ActorChoiceInput threw = Capture(store, "world-command-2", new PersonId("merchant-b"));
        Assert.That(store.TryMarkDispatchStarted(returned.InputId, 1L, 0, "decision-a", out _), Is.True);
        Assert.That(store.TryMarkDispatchStarted(threw.InputId, 1L, 1, "decision-b", out _), Is.True);

        AssertTransitionRejected(store.TryMarkDispatchStarted(
            returned.InputId, 1L, 0, "decision-a-again", out ActorChoiceStoreFailureCode duplicateStart), duplicateStart);
        AssertTransitionRejected(store.TryDefer(
            returned.InputId, 1L, 0, ActorChoiceDeferralReason.Traveling, out ActorChoiceStoreFailureCode deferAfterStart), deferAfterStart);
        AssertTransitionRejected(store.TryReject(
            returned.InputId, 1L, 0, ActorChoiceFailure.ActionUnavailable, out ActorChoiceStoreFailureCode rejectAfterStart), rejectAfterStart);

        Assert.That(store.TryRecordAttemptReturned(
            returned.InputId, 1L, 0, NpcActionResult.Succeeded(), out ActorChoiceStoreFailureCode returnFailure),
            Is.True, returnFailure.ToString());
        Assert.That(store.TryRecordAttemptThrew(threw.InputId, 1L, 1, out ActorChoiceStoreFailureCode threwFailure),
            Is.True, threwFailure.ToString());

        Assert.That(store.TryGet(returned.InputId, out ActorChoiceInput returnedInput), Is.True);
        Assert.That(returnedInput.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        Assert.That(returnedInput.Dispositions[1].AttemptOutcome, Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));
        Assert.That(returnedInput.Dispositions[1].ReturnedResultStatus, Is.EqualTo(NpcActionResultType.Success));
        Assert.That(store.TryGet(threw.InputId, out ActorChoiceInput threwInput), Is.True);
        Assert.That(threwInput.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptThrew));
        Assert.That(threwInput.Dispositions[1].AttemptOutcome, Is.EqualTo(ActorChoiceAttemptOutcome.Threw));
        Assert.That(threwInput.Dispositions[1].ReturnedResultStatus, Is.Null);

        AssertTransitionRejected(store.TryRecordAttemptReturned(
            returned.InputId, 1L, 0, NpcActionResult.Failed(), out ActorChoiceStoreFailureCode secondReturn), secondReturn);
        AssertTransitionRejected(store.TryRecordAttemptThrew(
            threw.InputId, 1L, 1, out ActorChoiceStoreFailureCode secondThrow), secondThrow);
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void NullReturnedResultIsDistinctFromThrownAttempt()
    {
        ActorChoiceStore store = CreateStore();
        ActorChoiceInput input = Capture(store, "world-command-1", new PersonId("merchant"));
        Assert.That(store.TryMarkDispatchStarted(input.InputId, 1L, 0, null, out _), Is.True);
        Assert.That(store.TryRecordAttemptReturned(input.InputId, 1L, 0, null, out _), Is.True);

        Assert.That(store.TryGet(input.InputId, out ActorChoiceInput completed), Is.True);
        Assert.That(completed.Dispositions[1].AttemptOutcome, Is.EqualTo(ActorChoiceAttemptOutcome.ReturnedNoResult));
        Assert.That(completed.Dispositions[1].ReturnedResultStatus, Is.Null);
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void RejectedCaptureAndLifecycleOperationsAreAtomic()
    {
        ActorChoiceStore store = CreateStore();
        ActorChoiceInput first = Capture(store, "world-command-1", new PersonId("merchant"));

        Assert.That(store.TryCapture(
            "world-command-1", new PersonId("merchant"), "sell-goods", WorldCommandOrigin.System,
            WorldCommandAuthorityMode.Request, 0L, out ActorChoiceInput duplicate, out ActorChoiceStoreFailureCode duplicateFailure), Is.False);
        Assert.That(duplicate, Is.Null);
        Assert.That(duplicateFailure, Is.EqualTo(ActorChoiceStoreFailureCode.DuplicateWorldCommandId));
        Assert.That(store.Count, Is.EqualTo(1));

        Assert.That(store.TryCapture(
            "world-command-invalid", new PersonId("merchant"), "sell-goods", WorldCommandOrigin.System,
            WorldCommandAuthorityMode.Request, -1L, out _, out ActorChoiceStoreFailureCode invalidCapture), Is.False);
        Assert.That(invalidCapture, Is.EqualTo(ActorChoiceStoreFailureCode.InvalidInput));

        AssertTransitionRejected(store.TryRecordAttemptReturned(
            first.InputId, 1L, 0, NpcActionResult.Succeeded(), out ActorChoiceStoreFailureCode beforeDispatch), beforeDispatch);
        Assert.That(store.TryDefer(
            first.InputId, -1L, 0, ActorChoiceDeferralReason.Traveling, out ActorChoiceStoreFailureCode invalidBoundary), Is.False);
        Assert.That(invalidBoundary, Is.EqualTo(ActorChoiceStoreFailureCode.InvalidBoundary));

        ActorChoiceInput second = Capture(store, "world-command-2", new PersonId("merchant"));
        Assert.That(second.InputSequence, Is.EqualTo(2L));
        Assert.That(second.InputId.Value, Is.EqualTo("actor-choice-000002"));
        Assert.That(store.TryGet(first.InputId, out ActorChoiceInput unchanged), Is.True);
        Assert.That(unchanged.Status, Is.EqualTo(ActorChoiceInputStatus.Pending));
        Assert.That(unchanged.Dispositions, Is.Empty);
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void CloneCopiesLifecycleAndAllocatorsWithoutSharingMutableState()
    {
        ActorChoiceStore source = CreateStore();
        ActorChoiceInput pending = Capture(source, "world-command-1", new PersonId("merchant-a"));
        ActorChoiceInput consumed = Capture(source, "world-command-2", new PersonId("merchant-b"));
        Assert.That(source.TryDefer(
            pending.InputId, 1L, 0, ActorChoiceDeferralReason.ScheduledDirective, out _), Is.True);
        Assert.That(source.TryMarkDispatchStarted(consumed.InputId, 1L, 1, "decision-b", out _), Is.True);

        ActorChoiceStore clone = CloneStore(source, new PersonStore());
        Assert.That(clone.Count, Is.EqualTo(2));
        Assert.That(clone.TryGet(pending.InputId, out ActorChoiceInput clonedPending), Is.True);
        Assert.That(clonedPending.Dispositions, Has.Count.EqualTo(1));
        Assert.That(clonedPending.Dispositions[0].DeferralReason, Is.EqualTo(ActorChoiceDeferralReason.ScheduledDirective));
        Assert.That(clone.TryGet(consumed.InputId, out ActorChoiceInput clonedConsumed), Is.True);
        Assert.That(clonedConsumed.Status, Is.EqualTo(ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt));
        Assert.That(clone.TryGetNextPendingForActor(consumed.PersonId, out _), Is.False);

        Assert.That(source.TryRecordAttemptThrew(consumed.InputId, 2L, 1, out _), Is.True);
        Assert.That(clone.TryGet(consumed.InputId, out clonedConsumed), Is.True);
        Assert.That(clonedConsumed.Status, Is.EqualTo(ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt));
        Assert.That(clone.TryRecordAttemptReturned(
            consumed.InputId, 2L, 1, NpcActionResult.Failed(), out _), Is.True);
        Assert.That(source.TryGet(consumed.InputId, out ActorChoiceInput sourceConsumed), Is.True);
        Assert.That(sourceConsumed.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptThrew));

        ActorChoiceInput sourceNext = Capture(source, "world-command-3", new PersonId("merchant-c"));
        ActorChoiceInput cloneNext = Capture(clone, "world-command-3", new PersonId("merchant-c"));
        Assert.That(sourceNext.InputSequence, Is.EqualTo(3L));
        Assert.That(cloneNext.InputSequence, Is.EqualTo(3L));
        Assert.That(sourceNext.InputId, Is.EqualTo(cloneNext.InputId));
        Assert.That(source.ValidateInvariants().IsValid, Is.True);
        Assert.That(clone.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void FaultedMutationGuardBlocksWritesWithoutChangingStoreState()
    {
        ActorChoiceStore store = CreateStore();
        ActorChoiceInput input = Capture(store, "world-command-1", new PersonId("merchant"));
        object guard = CreateMutationGuard();
        Assert.That(BindMutationGuard(store, guard), Is.True);
        FaultMutationGuard(guard);

        Assert.That(store.TryCapture(
            "world-command-2", new PersonId("merchant"), "sell-goods", WorldCommandOrigin.System,
            WorldCommandAuthorityMode.Request, 1L, out ActorChoiceInput rejectedCapture, out ActorChoiceStoreFailureCode captureFailure), Is.False);
        Assert.That(rejectedCapture, Is.Null);
        Assert.That(captureFailure, Is.EqualTo(ActorChoiceStoreFailureCode.RuntimeFaulted));
        Assert.That(store.TryDefer(
            input.InputId, 1L, 0, ActorChoiceDeferralReason.Traveling, out ActorChoiceStoreFailureCode transitionFailure), Is.False);
        Assert.That(transitionFailure, Is.EqualTo(ActorChoiceStoreFailureCode.RuntimeFaulted));
        Assert.That(store.Count, Is.EqualTo(1));
        Assert.That(store.TryGet(input.InputId, out ActorChoiceInput unchanged), Is.True);
        Assert.That(unchanged.Status, Is.EqualTo(ActorChoiceInputStatus.Pending));
        Assert.That(unchanged.Dispositions, Is.Empty);
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    private static ActorChoiceStore CreateStore()
    {
        return new ActorChoiceStore(new PersonStore());
    }

    private static ActorChoiceInput Capture(
        ActorChoiceStore store,
        string worldCommandId,
        PersonId personId,
        string actionDefinitionId = "sell-goods",
        long absoluteDay = 0L)
    {
        Assert.That(store.TryCapture(
            worldCommandId,
            personId,
            actionDefinitionId,
            WorldCommandOrigin.System,
            WorldCommandAuthorityMode.Request,
            absoluteDay,
            out ActorChoiceInput input,
            out ActorChoiceStoreFailureCode failure), Is.True, failure.ToString());
        return input;
    }

    private static void AssertTransitionRejected(bool result, ActorChoiceStoreFailureCode failure)
    {
        Assert.That(result, Is.False);
        Assert.That(failure, Is.EqualTo(ActorChoiceStoreFailureCode.InvalidLifecycleTransition));
    }

    private static ActorChoiceStore CloneStore(ActorChoiceStore source, PersonStore targetPersonStore)
    {
        MethodInfo clone = typeof(ActorChoiceStore).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(clone, Is.Not.Null);
        return (ActorChoiceStore)clone.Invoke(source, new object[] { targetPersonStore, null });
    }

    private static object CreateMutationGuard()
    {
        Type guardType = typeof(ActorChoiceStore).Assembly.GetType("AuthoritativeMutationGuard");
        Assert.That(guardType, Is.Not.Null);
        return Activator.CreateInstance(guardType, true);
    }

    private static bool BindMutationGuard(ActorChoiceStore store, object guard)
    {
        MethodInfo bind = typeof(ActorChoiceStore).GetMethod("TryBindMutationGuard", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(bind, Is.Not.Null);
        return (bool)bind.Invoke(store, new[] { guard });
    }

    private static void FaultMutationGuard(object guard)
    {
        Type guardType = guard.GetType();
        Type reasonType = guardType.Assembly.GetType("AuthoritativeMutationFaultReason");
        object reason = Enum.Parse(reasonType, "IntegrityRestoreFailed");
        MethodInfo markFaulted = guardType.GetMethod("MarkFaulted", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(markFaulted, Is.Not.Null);
        markFaulted.Invoke(guard, new[] { reason });
    }
}
