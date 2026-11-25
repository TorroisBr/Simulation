using System;

/// <summary>Host-side callbacks for the shared logical day clock.</summary>
public delegate bool SimulationTimeRuntimeAdvanceDispatcher(out SimulationTimeAdvanceFailure failure);

/// <summary>
/// Narrow Unity/runtime integration bridge for clock ownership, completed-boundary
/// dispatch and intraday timeline projection. Clock state and day-advance rules
/// remain in the shared Core implementation.
/// </summary>
public static class SimulationTimeOwnerBridge
{
    public static bool CanBindMutationStatus(SimulationTime owner, IAuthoritativeMutationStatus status)
        => owner != null && owner.CanBindMutationStatus(status);

    public static bool TryBindMutationStatus(SimulationTime owner, IAuthoritativeMutationStatus status)
        => owner != null && owner.TryBindMutationStatus(status);

    public static bool TryValidateAdvance(SimulationTime owner, out SimulationTimeAdvanceFailure failure)
    {
        if (owner == null)
        {
            failure = SimulationTimeAdvanceFailure.RuntimeFaulted;
            return false;
        }
        return owner.TryValidateAdvance(out failure);
    }

    public static bool TryAdvanceDayFromRuntime(SimulationTime owner, out SimulationTimeAdvanceFailure failure)
    {
        if (owner == null)
        {
            failure = SimulationTimeAdvanceFailure.RuntimeFaulted;
            return false;
        }
        return owner.TryAdvanceDayFromRuntime(out failure);
    }

    public static bool TryBindRuntimeAdvanceDispatcher(
        SimulationTime owner,
        SimulationTimeRuntimeAdvanceDispatcher dispatcher)
    {
        if (owner == null || dispatcher == null) return false;
        return owner.TryBindRuntimeAdvanceDispatcher((out SimulationTimeAdvanceFailure failure) =>
            dispatcher(out failure));
    }

    public static bool TryBindAbsoluteDayProjection(SimulationTime owner, Func<long> projection)
        => owner != null && owner.TryBindAbsoluteDayProjection(projection);
}
