using NUnit.Framework;

public sealed class ActorChoiceDiagnosticsTests
{
    [Test]
    public void SnapshotCanonicalExportAndDiffExposeInputAndLifecycle()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        Assert.That(store.TryCapture("command-1", new PersonId("actor-1"), "sell-goods",
            WorldCommandOrigin.System, WorldCommandAuthorityMode.Request, 2L,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode failure), Is.True, failure.ToString());
        Assert.That(store.TryDefer(input.InputId, 3L, 4, ActorChoiceDeferralReason.Traveling, out failure), Is.True);
        WorldStateSnapshot before = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(3L), actorChoiceStore: store));
        Assert.That(store.TryReject(input.InputId, 4L, 1, ActorChoiceFailure.ActorUnavailable, out failure), Is.True);
        WorldStateSnapshot after = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(4L), actorChoiceStore: store));

        string canonical = WorldStateCanonicalWriter.Write(after);
        Assert.That(canonical, Does.Contain("ACTOR_CHOICE"));
        Assert.That(canonical, Does.Contain("command-1"));
        Assert.That(canonical, Does.Contain("sell-goods"));
        Assert.That(canonical, Does.Contain("Traveling"));
        Assert.That(canonical, Does.Contain("ActorUnavailable"));
        Assert.That(WorldStateSnapshotFormatter.Format(after), Does.Contain("roster=4"));
        Assert.That(WorldStateDiff.Compare(before, after).Differences, Has.Some.Property("Section").EqualTo("ActorChoice"));
        Assert.That(WorldStateInvariantValidator.Validate(after).Errors,
            Has.Some.Property("Code").EqualTo("ActorChoicePersonMissing"));
    }
}
