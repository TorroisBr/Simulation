using NUnit.Framework;

public sealed class ActorChoiceTemporalInputOwnerTests
{
    [Test]
    public void TimelineInputCommitRetainsExactP11CommandAndAcceptedReference()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        ActorChoiceTemporalInputOwner owner = new ActorChoiceTemporalInputOwner(store, "intraday-sellgoods-v1");
        ActorChoiceTemporalCommand command = new ActorChoiceTemporalCommand(
            "world-command-α:1", new PersonId("person:merchant"), "sell-goods/v1",
            WorldCommandOrigin.System, WorldCommandAuthorityMode.Request);
        TimelineInputReference accepted = new TimelineInputReference(41, "input:41",
            ActorChoiceTemporalInputOwner.CommandKind, command.Encode(), new LogicalTick(123456));

        Assert.That(owner.TryPrepare(accepted, out ITimelineInputCommit prepared, out TimelineFailure prepareFailure),
            Is.True, prepareFailure.ToString());
        Assert.That(prepared.NewOwnerFacts, Is.Empty);
        Assert.That(prepared.TryCommit(out TimelineFailure commitFailure), Is.True, commitFailure.ToString());
        Assert.That(store.Inputs, Has.Count.EqualTo(1));

        ActorChoiceInput captured = store.Inputs[0];
        Assert.That(captured.InputSequence, Is.EqualTo(1L));
        Assert.That(captured.WorldCommandId, Is.EqualTo("world-command-α:1"));
        Assert.That(captured.PersonId.Value, Is.EqualTo("person:merchant"));
        Assert.That(captured.ActionDefinitionId, Is.EqualTo("sell-goods/v1"));
        Assert.That(captured.Origin, Is.EqualTo(WorldCommandOrigin.System));
        Assert.That(captured.Authority, Is.EqualTo(WorldCommandAuthorityMode.Request));
        Assert.That(captured.TemporalCapture.ProfileId, Is.EqualTo("intraday-sellgoods-v1"));
        Assert.That(captured.TemporalCapture.TargetInstant, Is.EqualTo(new LogicalTick(123456)));
        Assert.That(captured.TemporalCapture.AcceptedInput.Sequence, Is.EqualTo(41L));
        Assert.That(captured.TemporalCapture.AcceptedInput.InputId, Is.EqualTo("input:41"));
        Assert.That(captured.TemporalCapture.AcceptedInput.CommandData, Is.EqualTo(command.Encode()));
    }

    [Test]
    public void RepeatedTimelineCommitIsIdempotentForExactAcceptedReference()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        ActorChoiceTemporalInputOwner owner = new ActorChoiceTemporalInputOwner(store, "intraday-sellgoods-v1");
        ActorChoiceTemporalCommand command = new ActorChoiceTemporalCommand(
            "world-command-1", new PersonId("merchant"), "sell-goods/v1");
        TimelineInputReference accepted = new TimelineInputReference(7, "input-7",
            ActorChoiceTemporalInputOwner.CommandKind, command.Encode(), new LogicalTick(900));

        Assert.That(owner.TryPrepare(accepted, out ITimelineInputCommit first, out _), Is.True);
        Assert.That(owner.TryPrepare(accepted, out ITimelineInputCommit retry, out _), Is.True);
        Assert.That(first.TryCommit(out TimelineFailure firstFailure), Is.True, firstFailure.ToString());
        Assert.That(retry.TryCommit(out TimelineFailure retryFailure), Is.True, retryFailure.ToString());
        Assert.That(store.Inputs, Has.Count.EqualTo(1));
        Assert.That(store.Inputs[0].TemporalCapture.AcceptedInput.Sequence, Is.EqualTo(7L));
    }

    [Test]
    public void UnknownKindOrMalformedPayloadIsNotCommitted()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        ActorChoiceTemporalInputOwner owner = new ActorChoiceTemporalInputOwner(store, "intraday-sellgoods-v1");
        TimelineInputReference unknown = new TimelineInputReference(1, "input-1", "other/v1", "", new LogicalTick(1));
        TimelineInputReference malformed = new TimelineInputReference(2, "input-2",
            ActorChoiceTemporalInputOwner.CommandKind, "actor-choice/v1|bad", new LogicalTick(2));

        Assert.That(owner.TryPrepare(unknown, out _, out TimelineFailure unknownFailure), Is.False);
        Assert.That(unknownFailure, Is.EqualTo(TimelineFailure.UnknownWorkKind));
        Assert.That(owner.TryPrepare(malformed, out _, out TimelineFailure malformedFailure), Is.False);
        Assert.That(malformedFailure, Is.EqualTo(TimelineFailure.UnknownWorkKind));
        Assert.That(store.Count, Is.Zero);
    }
}
