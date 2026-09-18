using System;

/// <summary>
/// Owner-aware adapter for stateful NPC conflict consequences. ConflictResolver and
/// ConflictResolutionService remain usable as pure-domain services; this boundary is
/// the official path when a world-backed NPC consequence must be applied.
/// </summary>
public sealed class NpcConflictConsequenceSystem
{
    private readonly SimulationRuntime worldRuntime;
    private readonly ConflictResolutionService conflictResolutionService;

    public SimulationRuntime WorldRuntime => worldRuntime;
    public ConflictResolutionService ConflictResolutionService => conflictResolutionService;

    public NpcConflictConsequenceSystem(
        SimulationRuntime worldRuntime,
        ConflictResolutionService conflictResolutionService)
    {
        this.worldRuntime = worldRuntime ?? throw new ArgumentNullException(nameof(worldRuntime));
        this.conflictResolutionService = conflictResolutionService ?? throw new ArgumentNullException(nameof(conflictResolutionService));
    }

    public bool TryResolveAndApply(
        Conflict conflict,
        ConflictResolutionConstraints constraints,
        out ConflictResolutionResult result,
        out string reason)
    {
        return conflictResolutionService.TryResolveAndApply(
            conflict,
            constraints,
            worldRuntime,
            out result,
            out reason);
    }

    public bool TryApply(
        Conflict conflict,
        ConflictResolutionResult result,
        out string reason)
    {
        return conflictResolutionService.TryApply(
            conflict,
            result,
            worldRuntime,
            out reason);
    }
}
