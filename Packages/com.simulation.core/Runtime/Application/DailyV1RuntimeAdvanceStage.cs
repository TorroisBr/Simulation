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
    bool CanMutate { get; }
    bool HasIntradayTimeline { get; }

    bool TryPreflightDailyCoreSequenceCapacity(
        long requestedBoundaries,
        out DailyV1RuntimeAdvanceFailureCode failure);

    bool TryValidateClockAdvance(out DailyV1RuntimeAdvanceFailureCode failure);
    bool TryEnterDailyAdvanceOperation(out IDisposable operationScope);
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
            if (!boundary.TryValidateClockAdvance(out failure)) return false;
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
                    && !boundary.TryValidateClockAdvance(out failure))
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
                        advanced = boundary.TryAdvanceDayCore(out failure);
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
            if (!boundary.TryValidateClockAdvance(out failure)) return false;
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
                && !boundary.TryValidateClockAdvance(out failure))
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
                        bool advanced = boundary.HasIntradayTimeline
                            ? boundary.TryAdvanceIntradayToNextDayBoundary(out failure)
                            : boundary.TryAdvanceDayCore(out failure);
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
}
