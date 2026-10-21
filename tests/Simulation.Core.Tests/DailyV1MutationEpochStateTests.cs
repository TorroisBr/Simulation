using NUnit.Framework;

public sealed class DailyV1MutationEpochStateTests
{
    [Test]
    public void DirectCommittedMutationAdvancesExactlyOnceAndNoOpReservationDoesNot()
    {
        DailyV1MutationEpochState state = new DailyV1MutationEpochState();

        Assert.That(state.CurrentEpoch, Is.Zero);
        Assert.That(state.TryAdvanceCommittedMutation(), Is.True,
            "A direct committed owner write advances the shared epoch once.");
        Assert.That(state.CurrentEpoch, Is.EqualTo(1L));

        Assert.That(state.TryReserve(out DailyV1MutationEpochReservation noOp), Is.True);
        Assert.That(state.TryComplete(noOp, committedMutation: false), Is.True);
        Assert.That(state.CurrentEpoch, Is.EqualTo(1L),
            "A successfully admitted no-op operation must not advance the shared epoch.");
        Assert.That(state.HasActiveReservation, Is.False);
    }

    [Test]
    public void ReservedCommittedMutationCompletesOnceAndInvalidatesStaleHandles()
    {
        DailyV1MutationEpochState state = new DailyV1MutationEpochState();
        Assert.That(state.TryReserve(out DailyV1MutationEpochReservation reservation), Is.True);

        Assert.That(state.TryComplete(reservation, committedMutation: true), Is.True);
        Assert.That(state.CurrentEpoch, Is.EqualTo(1L));
        Assert.That(state.HasActiveReservation, Is.False);
        Assert.That(state.TryComplete(reservation, committedMutation: true), Is.False,
            "The same reservation cannot publish a second committed mutation.");
        Assert.That(state.CurrentEpoch, Is.EqualTo(1L));
    }

    [Test]
    public void ReservationExcludesConcurrentReservationsAndDirectCommits()
    {
        DailyV1MutationEpochState state = new DailyV1MutationEpochState();
        Assert.That(state.TryReserve(out DailyV1MutationEpochReservation first), Is.True);
        Assert.That(state.TryReserve(out DailyV1MutationEpochReservation rejected), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(state.TryAdvanceCommittedMutation(), Is.False,
            "An unreserved owner write cannot interleave with a prepared mutation.");
        Assert.That(state.CurrentEpoch, Is.Zero);

        Assert.That(state.TryRelease(first), Is.True);
        Assert.That(state.HasActiveReservation, Is.False);
        Assert.That(state.TryRelease(first), Is.False,
            "The state rejects a stale/double release; the protocol wrapper preserves its prior idempotent no-active behavior.");
        Assert.That(state.CurrentEpoch, Is.Zero);
    }

    [Test]
    public void ReservationFromAnotherStateCannotAdvanceOrReleaseThisState()
    {
        DailyV1MutationEpochState owner = new DailyV1MutationEpochState();
        DailyV1MutationEpochState other = new DailyV1MutationEpochState();
        Assert.That(owner.TryReserve(out DailyV1MutationEpochReservation reservation), Is.True);

        Assert.That(other.TryComplete(reservation, committedMutation: true), Is.False);
        Assert.That(other.TryRelease(reservation), Is.False);
        Assert.That(owner.CurrentEpoch, Is.Zero);
        Assert.That(owner.HasActiveReservation, Is.True);
        Assert.That(owner.TryRelease(reservation), Is.True);
    }
}
