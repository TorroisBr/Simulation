/// <summary>
/// Stable result values for the shared Daily-v1 runtime advance boundary.
/// The values preserve the Unity host contract while allowing the Core-owned
/// application and detached host to report the same failures.
/// </summary>
public enum SimulationRuntimeAdvanceFailure
{
    None = 0,
    RuntimeFaulted = 1,
    InvalidDayCount = 2,
    AbsoluteDayOverflow = 3,
    AdvanceAlreadyInProgress = 4,
    TemporalAdvanceFailed = 5,
    CompletedBoundarySequenceOverflow = 6
}
