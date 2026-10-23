using System;
using System.Collections.Generic;

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
    DailyV1ExecutionBoundaryState ExecutionState { get; }
    bool HasRuntimeAdmission { get; }
    bool IsAdmissionOwnerThreadCurrent { get; }
    bool IsDailyCaptureProfile { get; }
    bool HasCoreSelectedDailyExecution { get; }
    bool CanMutate { get; }
    bool HasIntradayTimeline { get; }

    bool TryPreflightDailyCoreSequenceCapacity(
        long requestedBoundaries,
        out DailyV1RuntimeAdvanceFailureCode failure);

    bool TryValidateClockAdvance(out DailyV1RuntimeAdvanceFailureCode failure);
    bool TryEnterDailyAdvanceOperation(out IDisposable operationScope);
    bool TryCreateSelectedDailyExecutionBoundary(
        out IDailyV1SelectedDailyExecutionBoundary executionBoundary);
    bool TryAdvanceDayCore(out DailyV1RuntimeAdvanceFailureCode failure);
    bool TryAdvanceIntradayToNextDayBoundary(out DailyV1RuntimeAdvanceFailureCode failure);
    bool TryCaptureCompletedDailyCaptureEvidence(
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure);
    void FaultRuntimeAdmission();
}

/// <summary>
/// Adds the selected Daily-v1 outer advance protocol to the Core-owned runtime
/// state: lease acquisition, P12 operation lifetime, completed-boundary
/// accounting, exception faulting, and the sole completed-token publication
/// point. Clock and host-specific mutation work are narrow adapters and must
/// identify this exact state instance.
/// </summary>
internal sealed partial class DailyV1ExecutionBoundaryState
{
    internal bool TryAdvanceDay(
        IDailyV1RuntimeAdvanceBoundary boundary,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        if (boundary == null) throw new ArgumentNullException(nameof(boundary));
        failure = DailyV1RuntimeAdvanceFailureCode.None;
        if (!ReferenceEquals(boundary.ExecutionState, this))
        {
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }

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

        if (!TryBeginAdvance(boundary.HasRuntimeAdmission, out DailyV1AdvanceLease lease))
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
                        if (advanced && !TryRecordCompletedCoreBoundary(boundary, out failure))
                            advanced = false;
                    }
                }
                catch
                {
                    InvalidateCurrentCaptureToken();
                    boundary.FaultRuntimeAdmission();
                    throw;
                }

                if (!advanced) return false;
                if (boundary.IsDailyCaptureProfile
                    && !TryPublishCompletedDailyCaptureToken(boundary, out failure))
                {
                    return false;
                }
                return true;
            }

            if (boundary.HasIntradayTimeline)
                return boundary.TryAdvanceIntradayToNextDayBoundary(out failure);

            return boundary.TryAdvanceDayCore(out failure);
        }
    }

    internal bool TryAdvanceDays(
        IDailyV1RuntimeAdvanceBoundary boundary,
        int dayCount,
        out int daysAdvanced,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        if (boundary == null) throw new ArgumentNullException(nameof(boundary));
        daysAdvanced = 0;
        failure = DailyV1RuntimeAdvanceFailureCode.None;
        if (!ReferenceEquals(boundary.ExecutionState, this))
        {
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }
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

        if (!TryBeginAdvance(boundary.HasRuntimeAdmission, out DailyV1AdvanceLease lease))
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
                        if (!TryRecordCompletedCoreBoundary(boundary, out failure)) return false;
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
                && !TryPublishCompletedDailyCaptureToken(boundary, out failure))
            {
                return false;
            }

        }

        return true;
    }

    private bool TryAdvanceOneCore(
        IDailyV1RuntimeAdvanceBoundary boundary,
        bool preferIntradayTimeline,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        if (preferIntradayTimeline && boundary.HasIntradayTimeline)
            return boundary.TryAdvanceIntradayToNextDayBoundary(out failure);

        if (boundary.HasCoreSelectedDailyExecution)
        {
            SimulationTime clock = SimulationTime;
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
            DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
            if (!boundary.TryCreateSelectedDailyExecutionBoundary(
                    out IDailyV1SelectedDailyExecutionBoundary executionBoundary)
                || graph == null
                || graph.Definitions == null
                || executionBoundary == null)
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                boundary.FaultRuntimeAdmission();
                return false;
            }

            DailyV1SelectedDailyExecutionStage.Advance(
                SimulationTime.AbsoluteDay, graph, graph.ScheduledDirectives, executionBoundary);
            failure = DailyV1RuntimeAdvanceFailureCode.None;
            return true;
        }

        if (boundary.HasIntradayTimeline)
            return boundary.TryAdvanceIntradayToNextDayBoundary(out failure);

        return boundary.TryAdvanceDayCore(out failure);
    }

    private bool TryRecordCompletedCoreBoundary(
        IDailyV1RuntimeAdvanceBoundary boundary,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        failure = DailyV1RuntimeAdvanceFailureCode.None;
        if (!boundary.IsDailyCaptureProfile) return true;
        if (TryRecordCompletedCore()) return true;

        boundary.FaultRuntimeAdmission();
        failure = DailyV1RuntimeAdvanceFailureCode.CompletedBoundarySequenceOverflow;
        return false;
    }

    private bool TryPublishCompletedDailyCaptureToken(
        IDailyV1RuntimeAdvanceBoundary boundary,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        failure = DailyV1RuntimeAdvanceFailureCode.None;
        if (!boundary.TryCaptureCompletedDailyCaptureEvidence(
                out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
                out long mutationEpoch,
                out _)
            || !TryIssueCompletedAdvanceCaptureToken(
                mutationEpoch, ownerSections, out _))
        {
            boundary.FaultRuntimeAdmission();
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }

        return true;
    }

    private bool TryValidateClockAdvance(
        IDailyV1RuntimeAdvanceBoundary boundary,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        if (boundary.HasIntradayTimeline || !boundary.HasCoreSelectedDailyExecution)
            return boundary.TryValidateClockAdvance(out failure);

        SimulationTime clock = SimulationTime;
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
