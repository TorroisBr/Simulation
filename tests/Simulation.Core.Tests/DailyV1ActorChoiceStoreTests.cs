using NUnit.Framework;

[TestFixture]
public sealed class DailyV1ActorChoiceStoreTests
{
    [Test]
    public void CapturesStableInputAndReturnsFifoForTheSamePerson()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        PersonId actor = new PersonId("person-merchant");

        Assert.That(store.TryCapture("command-1", actor, "action.sell-goods",
            WorldCommandOrigin.LocalPlayer, WorldCommandAuthorityMode.Request, 4L,
            out ActorChoiceInput first, out ActorChoiceStoreFailureCode firstFailure),
            Is.True, firstFailure.ToString());
        Assert.That(store.TryCapture("command-2", new PersonId("person-other"), "action.sell-goods",
            WorldCommandOrigin.LocalPlayer, WorldCommandAuthorityMode.Request, 4L,
            out _, out ActorChoiceStoreFailureCode otherFailure), Is.True, otherFailure.ToString());
        Assert.That(store.TryCapture("command-3", actor, "action.sell-goods",
            WorldCommandOrigin.LocalPlayer, WorldCommandAuthorityMode.Request, 4L,
            out ActorChoiceInput second, out ActorChoiceStoreFailureCode secondFailure),
            Is.True, secondFailure.ToString());

        Assert.That(first.InputId.Value, Is.EqualTo("actor-choice-000001"));
        Assert.That(first.InputSequence, Is.EqualTo(1L));
        Assert.That(first.Origin, Is.EqualTo(WorldCommandOrigin.LocalPlayer));
        Assert.That(first.Authority, Is.EqualTo(WorldCommandAuthorityMode.Request));
        Assert.That(store.P11InputCount, Is.EqualTo(3));
        Assert.That(store.CensusRevision, Is.EqualTo(3L));
        Assert.That(store.TryGetNextPendingForActor(actor, out ActorChoiceInput pending), Is.True);
        Assert.That(pending.InputId, Is.EqualTo(first.InputId));

        Assert.That(store.TryMarkDispatchStarted(first.InputId, 5L, 0, "decision-1", out _), Is.True);
        Assert.That(store.TryGetNextPendingForActor(actor, out pending), Is.True);
        Assert.That(pending.InputId, Is.EqualTo(second.InputId));
    }

    [Test]
    public void RecordsReturnedStatusesWithoutDependingOnUnityActionResult()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput succeeded = Capture(store, "command-success", "person-success");
        ActorChoiceInput noResult = Capture(store, "command-null", "person-null");
        ActorChoiceInput threw = Capture(store, "command-throw", "person-throw");

        Assert.That(store.TryMarkDispatchStarted(succeeded.InputId, 1L, 0, "decision-success", out _), Is.True);
        Assert.That(store.TryRecordAttemptReturned(succeeded.InputId, 1L, 0,
            NpcActionResultType.Success, out ActorChoiceStoreFailureCode successFailure),
            Is.True, successFailure.ToString());
        Assert.That(store.TryMarkDispatchStarted(noResult.InputId, 1L, 1, "decision-null", out _), Is.True);
        Assert.That(store.TryRecordAttemptReturned(noResult.InputId, 1L, 1,
            null, out ActorChoiceStoreFailureCode nullFailure), Is.True, nullFailure.ToString());
        Assert.That(store.TryMarkDispatchStarted(threw.InputId, 1L, 2, "decision-throw", out _), Is.True);
        Assert.That(store.TryRecordAttemptThrew(threw.InputId, 1L, 2,
            out ActorChoiceStoreFailureCode throwFailure), Is.True, throwFailure.ToString());

        Assert.That(store.TryGet(succeeded.InputId, out ActorChoiceInput succeededValue), Is.True);
        Assert.That(succeededValue.Dispositions[1].AttemptOutcome,
            Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));
        Assert.That(succeededValue.Dispositions[1].ReturnedResultStatus,
            Is.EqualTo(NpcActionResultType.Success));
        Assert.That(store.TryGet(noResult.InputId, out ActorChoiceInput noResultValue), Is.True);
        Assert.That(noResultValue.Dispositions[1].AttemptOutcome,
            Is.EqualTo(ActorChoiceAttemptOutcome.ReturnedNoResult));
        Assert.That(noResultValue.Dispositions[1].ReturnedResultStatus, Is.Null);
        Assert.That(store.TryGet(threw.InputId, out ActorChoiceInput threwValue), Is.True);
        Assert.That(threwValue.Dispositions[1].AttemptOutcome,
            Is.EqualTo(ActorChoiceAttemptOutcome.Threw));
        Assert.That(threwValue.Dispositions[1].ReturnedResultStatus, Is.Null);
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void P12MutationBoundaryReceivesOneAdmissionAndCommitPerOwnerWrite()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        int admissions = 0;
        int commits = 0;
        store.BindP12MutationBoundary(() => { admissions++; return true; }, () => commits++);

        ActorChoiceInput input = Capture(store, "command-p12", "person-p12");
        Assert.That(store.TryDefer(input.InputId, 1L, 0,
            ActorChoiceDeferralReason.ScheduledDirective,
            out ActorChoiceStoreFailureCode deferFailure), Is.True, deferFailure.ToString());

        Assert.That(admissions, Is.EqualTo(2));
        Assert.That(commits, Is.EqualTo(2));
        Assert.That(store.CensusRevision, Is.EqualTo(2L));
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    private static ActorChoiceInput Capture(ActorChoiceStore store, string commandId, string personId)
    {
        Assert.That(store.TryCapture(commandId, new PersonId(personId), "action.sell-goods",
            WorldCommandOrigin.LocalPlayer, WorldCommandAuthorityMode.Request, 0L,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode failure),
            Is.True, failure.ToString());
        return input;
    }
}
