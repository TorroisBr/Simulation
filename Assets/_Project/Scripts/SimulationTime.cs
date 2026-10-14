using System;

public enum SimulationTimeAdvanceFailure
{
    None = 0,
    RuntimeFaulted = 1,
    AbsoluteDayOverflow = 2,
    TimelineProjectionOwnsClock = 3
}

internal delegate bool SimulationTimeAdvanceDispatcher(out SimulationTimeAdvanceFailure failure);

[Serializable]
public sealed class SimulationTime : IAuthoritativeMutationGuardBindable
{
    private long absoluteDay;
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    [NonSerialized] private Func<long> absoluteDayProjection;
    [NonSerialized] private SimulationTimeAdvanceDispatcher runtimeAdvanceDispatcher;

    public long AbsoluteDay => absoluteDayProjection != null
        ? absoluteDayProjection()
        : absoluteDay;

    public SimulationTime()
        : this(0L)
    {
    }

    public SimulationTime(long absoluteDay)
    {
        if (absoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteDay), "AbsoluteDay cannot be negative.");
        }

        this.absoluteDay = absoluteDay;
    }

    public void AdvanceDay()
    {
        if (!TryAdvanceDay(out SimulationTimeAdvanceFailure failure))
        {
            throw new InvalidOperationException(failure == SimulationTimeAdvanceFailure.RuntimeFaulted
                ? "A faulted SimulationRuntime cannot advance its SimulationTime."
                : failure == SimulationTimeAdvanceFailure.TimelineProjectionOwnsClock
                    ? "SimulationTime is projected from the SimulationTimeline and cannot advance independently."
                    : "SimulationTime cannot advance beyond the maximum AbsoluteDay.");
        }
    }

    public bool TryAdvanceDay(out SimulationTimeAdvanceFailure failure)
    {
        if (!TryValidateAdvance(out failure)) return false;
        return runtimeAdvanceDispatcher != null
            ? runtimeAdvanceDispatcher(out failure)
            : TryAdvanceDayCore(out failure);
    }

    internal bool TryValidateAdvance(out SimulationTimeAdvanceFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = SimulationTimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        if (absoluteDayProjection != null)
        {
            failure = SimulationTimeAdvanceFailure.TimelineProjectionOwnsClock;
            return false;
        }

        if (absoluteDay == long.MaxValue)
        {
            failure = SimulationTimeAdvanceFailure.AbsoluteDayOverflow;
            return false;
        }

        failure = SimulationTimeAdvanceFailure.None;
        return true;
    }

    internal bool TryAdvanceDayFromRuntime(out SimulationTimeAdvanceFailure failure)
    {
        if (!TryValidateAdvance(out failure)) return false;
        return TryAdvanceDayCore(out failure);
    }

    private bool TryAdvanceDayCore(out SimulationTimeAdvanceFailure failure)
    {
        absoluteDay++;
        failure = SimulationTimeAdvanceFailure.None;
        return true;
    }

    internal bool TryBindRuntimeAdvanceDispatcher(SimulationTimeAdvanceDispatcher dispatcher)
    {
        if (dispatcher == null || runtimeAdvanceDispatcher != null || absoluteDayProjection != null)
            return false;
        runtimeAdvanceDispatcher = dispatcher;
        return true;
    }

    internal bool TryBindAbsoluteDayProjection(Func<long> projection)
    {
        if (projection == null || absoluteDayProjection != null || projection() != absoluteDay)
            return false;
        absoluteDayProjection = projection;
        return true;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.TryBindTo(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }
}
