using System;
using System.Collections.Generic;

/// <summary>
/// Adapts the existing PlaceContentStore receipt-backed daily-aging operation
/// to the P18-D boundary coordinator. The store remains the sole effect and
/// receipt owner.
/// </summary>
public sealed class PlaceContentDailyBoundaryStepProvider : IP18DDailyBoundaryStepProvider
{
    private readonly PlaceContentStore owner;

    public PlaceContentDailyBoundaryStepProvider(PlaceContentStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure)
    {
        steps = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || firstOrdinal < 0
            || !owner.TryCreateDayAdvanceStep(operation, firstOrdinal,
                out BoundaryContinuationStep step, out failure)
            || step == null)
        {
            if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        steps = Array.AsReadOnly(new[] { step });
        failure = TimelineFailure.None;
        return true;
    }

    public bool OwnsStep(BoundaryContinuationStep step) =>
        step != null
        && step.OwnerId == PlaceContentStore.DayAdvanceOwnerId
        && step.StepId == PlaceContentStore.DayAdvanceStepId;

    public bool TryPrepareStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        if (!OwnsStep(step))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        return owner.TryPrepareDayAdvanceStep(manifest, step, out prepared, out failure);
    }
}

/// <summary>
/// Adapts the existing SimulationLogger begin-day heading receipt to P18-D.
/// The captured include/no-output disposition is frozen by the logger owner.
/// </summary>
public sealed class LoggerDailyBoundaryStepProvider : IP18DDailyBoundaryStepProvider
{
    private const string OwnerId = "logger";
    private const string StepId = "logger-begin-day";
    private readonly SimulationLogger owner;

    public LoggerDailyBoundaryStepProvider(SimulationLogger owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure)
    {
        steps = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || firstOrdinal < 0
            || !owner.TryCreateBeginDayStep(operation, firstOrdinal,
                out BoundaryContinuationStep step, out failure)
            || step == null)
        {
            if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        steps = Array.AsReadOnly(new[] { step });
        failure = TimelineFailure.None;
        return true;
    }

    public bool OwnsStep(BoundaryContinuationStep step) =>
        step != null && step.OwnerId == OwnerId && step.StepId == StepId;

    public bool TryPrepareStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        if (!OwnsStep(step))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        return owner.TryPrepareBeginDayStep(manifest, step, out prepared, out failure);
    }
}
