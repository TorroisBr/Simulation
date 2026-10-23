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
    bool HasIntradayTimeline { get; }
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

        bool scopeRequired = HasRuntimeAdmission;
        if (scopeRequired && !TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance))
        {
            boundary.FaultRuntimeAdmission();
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }

        bool advanced;
        bool scopeClosed = true;
        try
        {
            advanced = TryAdvanceDayWithinOperationScope(boundary, out failure);
        }
        finally
        {
            if (scopeRequired)
            {
                scopeClosed = TryEndOperation(DailyV1OperationScopeKind.DailyAdvance);
                if (!scopeClosed)
                {
                    InvalidateCurrentCaptureToken();
                    boundary.FaultRuntimeAdmission();
                }
            }
        }

        if (!scopeClosed)
        {
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }
        return advanced;
    }

    private bool TryAdvanceDayWithinOperationScope(
        IDailyV1RuntimeAdvanceBoundary boundary,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        failure = DailyV1RuntimeAdvanceFailureCode.None;

        if (!IsAdmissionOwnerThreadCurrent(boundary))
        {
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }

        if (IsDailyCaptureProfile)
        {
            if (!CanMutate)
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                return false;
            }
            if (!TryPreflightDailyCoreSequenceCapacity(1L, out failure)) return false;
            if (!TryValidateClockAdvance(out failure)) return false;
        }

        if (!TryBeginAdvance(HasRuntimeAdmission, out DailyV1AdvanceLease lease))
        {
            failure = DailyV1RuntimeAdvanceFailureCode.AdvanceAlreadyInProgress;
            return false;
        }

        using (lease)
        {
            if (HasRuntimeAdmission)
            {
                if (!CanMutate)
                {
                    failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                    return false;
                }

                if (!IsDailyCaptureProfile
                    && !TryValidateClockAdvance(out failure))
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
                if (IsDailyCaptureProfile
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

        bool scopeRequired = HasRuntimeAdmission;
        if (scopeRequired && !TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance))
        {
            boundary.FaultRuntimeAdmission();
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }

        bool advanced;
        bool scopeClosed = true;
        try
        {
            advanced = TryAdvanceDaysWithinOperationScope(
                boundary, dayCount, out daysAdvanced, out failure);
        }
        finally
        {
            if (scopeRequired)
            {
                scopeClosed = TryEndOperation(DailyV1OperationScopeKind.DailyAdvance);
                if (!scopeClosed)
                {
                    InvalidateCurrentCaptureToken();
                    boundary.FaultRuntimeAdmission();
                }
            }
        }

        if (!scopeClosed)
        {
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }
        return advanced;
    }

    private bool TryAdvanceDaysWithinOperationScope(
        IDailyV1RuntimeAdvanceBoundary boundary,
        int dayCount,
        out int daysAdvanced,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        daysAdvanced = 0;
        failure = DailyV1RuntimeAdvanceFailureCode.None;

        if (!IsAdmissionOwnerThreadCurrent(boundary))
        {
            failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
            return false;
        }

        if (IsDailyCaptureProfile)
        {
            if (!CanMutate)
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                return false;
            }
            if (!TryPreflightDailyCoreSequenceCapacity(dayCount, out failure)) return false;
            if (!TryValidateClockAdvance(out failure)) return false;
        }

        if (!TryBeginAdvance(HasRuntimeAdmission, out DailyV1AdvanceLease lease))
        {
            failure = DailyV1RuntimeAdvanceFailureCode.AdvanceAlreadyInProgress;
            return false;
        }

        using (lease)
        {
            if (!CanMutate)
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                return false;
            }

            if (HasRuntimeAdmission && !IsDailyCaptureProfile
                && !TryValidateClockAdvance(out failure))
                return false;

            IDisposable operationScope = null;
            if (HasRuntimeAdmission
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
                    if (HasRuntimeAdmission)
                        boundary.FaultRuntimeAdmission();
                    throw;
                }
            }

            if (IsDailyCaptureProfile
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

        if (HasAdmittedSelectedDailyExecution)
        {
            SimulationTime clock = SimulationTime;
            if (clock == null)
            {
                failure = DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
                return false;
            }
            if (!CanMutate)
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
                this, SimulationTime.AbsoluteDay, graph, graph.ScheduledDirectives, executionBoundary);
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
        if (!IsDailyCaptureProfile) return true;
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

    private bool TryValidateClockAdvance(out DailyV1RuntimeAdvanceFailureCode failure)
    {
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

    private bool IsAdmissionOwnerThreadCurrent(IDailyV1RuntimeAdvanceBoundary boundary)
    {
        if (AdmissionContext == null || AdmissionContext.IsOwnedByCurrentThread())
            return true;
        boundary.FaultRuntimeAdmission();
        return false;
    }

    private bool TryPreflightDailyCoreSequenceCapacity(
        long requestedBoundaries,
        out DailyV1RuntimeAdvanceFailureCode failure)
    {
        if (CanRecordCompletedCoreCount(requestedBoundaries))
        {
            failure = DailyV1RuntimeAdvanceFailureCode.None;
            return true;
        }
        failure = DailyV1RuntimeAdvanceFailureCode.CompletedBoundarySequenceOverflow;
        return false;
    }

    private bool HasRuntimeAdmission => AdmissionContext != null;
    private bool IsDailyCaptureProfile => AdmissionContext?.Profile
        == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1;

    private static DailyV1RuntimeAdvanceFailureCode MapClockFailure(
        SimulationTimeAdvanceFailure failure) =>
        failure == SimulationTimeAdvanceFailure.AbsoluteDayOverflow
            ? DailyV1RuntimeAdvanceFailureCode.AbsoluteDayOverflow
            : DailyV1RuntimeAdvanceFailureCode.RuntimeFaulted;
}
