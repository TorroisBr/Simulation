using System;

/// <summary>
/// Owns the selected runtime's mutation epoch and one outstanding reserved
/// commit slot. Thread affinity and the set of admitted operation scopes are
/// enforced by the runtime admission edge before calling this state.
/// </summary>
internal sealed class DailyV1MutationEpochState
{
    private long epoch;
    private DailyV1MutationEpochReservation activeReservation;

    internal long CurrentEpoch => epoch;
    internal bool HasActiveReservation => activeReservation != null;
    internal DailyV1MutationEpochReservation ActiveReservation => activeReservation;
    internal bool CanAdvance => epoch < long.MaxValue;

    internal bool TryReserve(out DailyV1MutationEpochReservation reservation)
    {
        reservation = null;
        if (activeReservation != null || epoch == long.MaxValue) return false;
        DailyV1MutationEpochReservation candidate = new DailyV1MutationEpochReservation(this);
        activeReservation = candidate;
        reservation = candidate;
        return true;
    }

    internal bool Owns(DailyV1MutationEpochReservation reservation) =>
        reservation != null && ReferenceEquals(activeReservation, reservation)
        && reservation.IsOwnedBy(this);

    internal bool TryAdvanceCommittedMutation()
    {
        if (activeReservation != null || epoch == long.MaxValue) return false;
        epoch++;
        return true;
    }

    internal bool TryComplete(
        DailyV1MutationEpochReservation reservation,
        bool committedMutation)
    {
        if (!Owns(reservation) || (committedMutation && epoch == long.MaxValue)) return false;
        if (committedMutation) epoch++;
        activeReservation = null;
        return true;
    }

    internal bool TryRelease(DailyV1MutationEpochReservation reservation)
    {
        if (!Owns(reservation)) return false;
        activeReservation = null;
        return true;
    }
}

/// <summary>Opaque one-shot capacity reservation from one epoch state.</summary>
internal sealed class DailyV1MutationEpochReservation
{
    private readonly DailyV1MutationEpochState owner;
    private readonly object token = new object();

    internal DailyV1MutationEpochReservation(DailyV1MutationEpochState owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    internal object Token => token;
    internal bool IsOwnedBy(DailyV1MutationEpochState candidate) => ReferenceEquals(owner, candidate);
}
