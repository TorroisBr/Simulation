using System;

/// <summary>Host results used by Core's serialized Daily-v1 advance coordinator.</summary>
internal enum DailyV1RuntimeAdvanceFailureCode : byte
{
    None = 0,
    RuntimeFaulted = 1,
    InvalidDayCount = 2,
    AbsoluteDayOverflow = 3,
    AdvanceAlreadyInProgress = 4,
    TemporalAdvanceFailed = 5,
    CompletedBoundarySequenceOverflow = 6
}

/// <summary>
/// Narrow clock, census, timeline and publication operations required by the
/// Core-owned advance protocol. The host cannot reorder lease, operation,
/// completed-boundary, or capture-token handling.
/// </summary>
internal interface IDailyV1RuntimeAdvanceBoundary
{
    bool HasRuntimeAdmission { get; }
    bool IsAdmissionOwnerThreadCurrent { get; }
    bool IsDailyCaptureProfile { get; }
    bool HasCoreSelectedDailyExecution { get; }
    SimulationTime SelectedDailyClock { get; }
    bool CanMutate { get; }
    bool HasIntradayTimeline { get; }

    bool TryPreflightDailyCoreSequenceCapacity(
        long requestedBoundaries,
        out DailyV1RuntimeAdvanceFailureCode failure);

    bool TryValidateClockAdvance(out DailyV1RuntimeAdvanceFailureCode failure);
    bool TryEnterDailyAdvanceOperation(out IDisposable operationScope);
    bool TryCreateSelectedDailyExecutionContext(
        out long absoluteDay,
        out DailyV1SelectedRuntimeOwnerGraph graph,
        out ScheduledDirectiveStore scheduledDirectives,
        out IDailyV1SelectedDailyExecutionBoundary executionBoundary);
    bool TryAdvanceDayCore(out DailyV1RuntimeAdvanceFailureCode failure);
    bool TryAdvanceIntradayToNextDayBoundary(out DailyV1RuntimeAdvanceFailureCode failure);
    bool TryRecordCompletedDailyCore(out DailyV1RuntimeAdvanceFailureCode failure);
    bool TryPublishCompletedDailyCaptureToken();
    void FaultRuntimeAdmission();
}

/// <summary>
/// Owns the selected Daily-v1 outer advance protocol: lease acquisition,
/// P12 operation lifetime, completed-boundary accounting, exception faulting,
/// and the sole completed-token publication point. Clock and host-specific
/// mutation work are supplied as narrow callbacks.
/// </summary>
internal static class DailyV1RuntimeAdvanceStage
{
    internal static bool TryAdvanceDay(
        DailyV1ExecutionBoundaryState state,
        IDailyV1RuntimeAdvanceBoundary boundary,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        if (state == null || boundary == null) throw new ArgumentNullException();
        failure = DailyV1RuntimeAdvanceFailureCode.None;

        if (boundary.HasRuntimeAdmission && !boundary.IsAdmissionOwnerThreadCurrent)
        {
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }

        if (boundary.IsDailyCaptureProfile)
        {
            if (!boundary.CanMutate)
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                return false;
            }
            if (!boundary.TryPreflightDailyCoreSequenceCapacity(1L, out failure)) return false;
            if (!TryValidateClockAdvance(boundary, out failure)) return false;
        }

        if (!state.TryBeginAdvance(boundary.HasRuntimeAdmission, out DailyV1AdvanceLease lease))
        {
            failure = DailyV1RuntimeAdvanceFailureCode.AdvanceAlreadyInProgress;
            return false;
        }

        using (lease)
        {
            if (boundary.HasRuntimeAdmission)
            {
                if (!boundary.CanMutate)
                {
                    failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                    return false;
                }

                if (!boundary.IsDailyCaptureProfile
                    && !TryValidateClockAdvance(boundary, out failure))
                    return false;

                if (!boundary.TryEnterDailyAdvanceOperation(out IDisposable operationScope))
                {
                    failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                    return false;
                }

                bool advanced;
                try
                {
                    using (operationScope)
                    {
                        advanced = TryAdvanceOneCore(
                            boundary, preferIntradayTimeline: false, out failure);
                        if (advanced && !boundary.TryRecordCompletedDailyCore(out failure))
                            advanced = false;
                    }
                }
                catch
                {
                    state.InvalidateCurrentCaptureToken();
                    boundary.FaultRuntimeAdmission();
                    throw;
                }

                if (!advanced) return false;
                if (boundary.IsDailyCaptureProfile
                    && !boundary.TryPublishCompletedDailyCaptureToken())
                {
                    failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                    return false;
                }
                return true;
            }

            if (boundary.HasIntradayTimeline)
                return boundary.TryAdvanceIntradayToNextDayBoundary(out failure);

            return boundary.TryAdvanceDayCore(out failure);
        }
    }

    internal static bool TryAdvanceDays(
        DailyV1ExecutionBoundaryState state,
        IDailyV1RuntimeAdvanceBoundary boundary,
        int dayCount,
        out int daysAdvanced,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        if (state == null || boundary == null) throw new ArgumentNullException();
        daysAdvanced = 0;
        failure = DailyV1RuntimeAdvanceFailureCode.None;
        if (dayCount < 0)
        {
            failure = DailyV1RuntimeAdvanceFailureCode.InvalidDayCount;
            return false;
        }
        if (dayCount == 0) return true;

        if (boundary.HasRuntimeAdmission && !boundary.IsAdmissionOwnerThreadCurrent)
        {
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }

        if (boundary.IsDailyCaptureProfile)
        {
            if (!boundary.CanMutate)
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                return false;
            }
            if (!boundary.TryPreflightDailyCoreSequenceCapacity(dayCount, out failure)) return false;
            if (!TryValidateClockAdvance(boundary, out failure)) return false;
        }

        if (!state.TryBeginAdvance(boundary.HasRuntimeAdmission, out DailyV1AdvanceLease lease))
        {
            failure = DailyV1RuntimeAdvanceFailureCode.AdvanceAlreadyInProgress;
            return false;
        }

        using (lease)
        {
            if (!boundary.CanMutate)
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                return false;
            }

            if (boundary.HasRuntimeAdmission && !boundary.IsDailyCaptureProfile
                && !TryValidateClockAdvance(boundary, out failure))
                return false;

            IDisposable operationScope = null;
            if (boundary.HasRuntimeAdmission
                && !boundary.TryEnterDailyAdvanceOperation(out operationScope))
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                return false;
            }

            using (operationScope)
            {
                try
                {
                    for (int index = 0; index < dayCount; index++)
                    {
                        bool advanced = TryAdvanceOneCore(
                            boundary, preferIntradayTimeline: true, out failure);
                        if (!advanced) return false;
                        if (!boundary.TryRecordCompletedDailyCore(out failure)) return false;
                        daysAdvanced++;
                    }
                }
                catch
                {
                    if (boundary.HasRuntimeAdmission)
                        boundary.FaultRuntimeAdmission();
                    throw;
                }
            }

            if (boundary.IsDailyCaptureProfile
                && !boundary.TryPublishCompletedDailyCaptureToken())
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                return false;
            }

        }

        return true;
    }

    private static bool TryAdvanceOneCore(
        IDailyV1RuntimeAdvanceBoundary boundary,
        bool preferIntradayTimeline,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        if (preferIntradayTimeline && boundary.HasIntradayTimeline)
            return boundary.TryAdvanceIntradayToNextDayBoundary(out failure);

        if (boundary.HasCoreSelectedDailyExecution)
        {
            SimulationTime clock = boundary.SelectedDailyClock;
            if (clock == null)
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                return false;
            }
            if (!boundary.CanMutate)
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                return false;
            }
            if (!SimulationTimeOwnerBridge.TryAdvanceDayFromRuntime(
                    clock, out SimulationTimeAdvanceFailure timeFailure))
            {
                failure = MapClockFailure(timeFailure);
                return false;
            }
            if (!boundary.TryCreateSelectedDailyExecutionContext(
                    out long absoluteDay,
                    out DailyV1SelectedRuntimeOwnerGraph graph,
                    out ScheduledDirectiveStore scheduledDirectives,
                    out IDailyV1SelectedDailyExecutionBoundary executionBoundary)
                || graph == null
                || executionBoundary == null)
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                boundary.FaultRuntimeAdmission();
                return false;
            }

            DailyV1SelectedDailyExecutionStage.Advance(
                absoluteDay, graph, scheduledDirectives, executionBoundary);
            failure = DailyV1RuntimeAdvanceFailureCode.None;
            return true;
        }

        if (boundary.HasIntradayTimeline)
            return boundary.TryAdvanceIntradayToNextDayBoundary(out failure);

        return boundary.TryAdvanceDayCore(out failure);
    }

    private static bool TryValidateClockAdvance(
        IDailyV1RuntimeAdvanceBoundary boundary,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        if (boundary.HasIntradayTimeline || !boundary.HasCoreSelectedDailyExecution)
            return boundary.TryValidateClockAdvance(out failure);

        SimulationTime clock = boundary.SelectedDailyClock;
        if (clock == null)
        {
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }

        bool valid = SimulationTimeOwnerBridge.TryValidateAdvance(
            clock, out SimulationTimeAdvanceFailure timeFailure);
        failure = valid ? DailyV1RuntimeAdvanceFailureCode.None : MapClockFailure(timeFailure);
        return valid;
    }

    private static DailyV1RuntimeAdvanceFailureCode MapClockFailure(
        SimulationTimeAdvanceFailure failure) =>
        failure == SimulationTimeAdvanceFailure.AbsoluteDayOverflow
            ? DailyV1RuntimeAdvanceFailureCode.AbsoluteDayOverflow
            : DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
}
