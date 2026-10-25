using System;
using System.Collections.Generic;

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
    /// <summary>
    /// Builds and validates the exact selected stage adapters without mutating
    /// runtime state. Core calls this before advancing the admitted clock so a
    /// missing adapter cannot leave a rejected advance at a later day.
    /// </summary>
    bool TryCreateSelectedDailyExecutionBoundary(
        out IDailyV1SelectedDailyExecutionBoundary executionBoundary);
    bool TryAdvanceDayCore(out SimulationRuntimeAdvanceFailure failure);
    bool TryAdvanceIntradayToNextDayBoundary(out SimulationRuntimeAdvanceFailure failure);
    bool TryCaptureCompletedDailyCaptureEvidence(
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure);
    /// <summary>
    /// Re-captures owner evidence for Core's exact-session continuation
    /// admission. This is distinct from advance-finalization evidence: the
    /// source may be under its exclusive replacement reservation, while a
    /// detached candidate must be idle without that reservation.
    /// </summary>
    bool TryCaptureDailyBoundaryAdmissionEvidence(
        bool sessionReplacement,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure);
    /// <summary>
    /// Captures the live census for the one registered continuation-save
    /// operation. This is distinct from idle admission evidence: P12-G keeps
    /// the save operation registered while it stages detached owners.
    /// </summary>
    bool TryCaptureContinuationSaveEvidence(
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
    internal bool IsHealthyDailyOwnerThreadBoundary()
    {
        return AdmissionContext != null
            && AdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            && AdmissionContext.IsOwnedByCurrentThread()
            && !AdvanceLeaseHeld
            && ActiveOperationCount == 0
            && WorldId != null
            && mutationGuard.CanMutate;
    }

    internal bool HasValidCompletedDailyBoundaryAdmission(
        IDailyV1RuntimeAdvanceBoundary boundary,
        bool sessionReplacement) => HasValidDailyBoundaryAdmission(
            boundary,
            sessionReplacement
                ? (DailyCaptureBoundaryProvenance?)null
                : DailyCaptureBoundaryProvenance.CompletedAdvance,
            sessionReplacement);

    internal bool HasValidRestoredDailyBoundaryAdmission(
        IDailyV1RuntimeAdvanceBoundary boundary) => HasValidDailyBoundaryAdmission(
            boundary, DailyCaptureBoundaryProvenance.RestoredContinuation, sessionReplacement: false);

    private bool HasValidDailyBoundaryAdmission(
        IDailyV1RuntimeAdvanceBoundary boundary,
        DailyCaptureBoundaryProvenance? expectedProvenance,
        bool sessionReplacement)
    {
        DailyCaptureEligibilityToken current = currentCaptureToken;
        if (boundary == null || !ReferenceEquals(boundary.ExecutionState, this)
            || !IsHealthyDailyOwnerThreadBoundary()
            || current == null
            || !HasAllowedDailyBoundaryProvenance(current, expectedProvenance, sessionReplacement)
            || !boundary.TryCaptureDailyBoundaryAdmissionEvidence(
                sessionReplacement,
                out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
                out long mutationEpoch,
                out _))
            return false;

        return IsHealthyDailyOwnerThreadBoundary()
            && ReferenceEquals(currentCaptureToken, current)
            && HasAllowedDailyBoundaryProvenance(current, expectedProvenance, sessionReplacement)
            && SimulationTime != null
            && TryCompareCurrentCaptureToken(
                current,
                AdmissionContext,
                ConfigurationIdentity,
                CalendarIdentity,
                CompositionProfile,
                WorldId,
                SimulationTime.AbsoluteDay,
                ownerSections,
                mutationEpoch);
    }

    private static bool HasAllowedDailyBoundaryProvenance(
        DailyCaptureEligibilityToken token,
        DailyCaptureBoundaryProvenance? expectedProvenance,
        bool sessionReplacement)
    {
        if (token == null) return false;
        if (expectedProvenance.HasValue)
            return token.BoundaryProvenance == expectedProvenance.Value;

        return sessionReplacement
            && (token.BoundaryProvenance == DailyCaptureBoundaryProvenance.CompletedAdvance
                || token.BoundaryProvenance == DailyCaptureBoundaryProvenance.RestoredContinuation);
    }

    internal bool TryAdvanceDay(
        IDailyV1RuntimeAdvanceBoundary boundary,
        out SimulationRuntimeAdvanceFailure failure)
    {
        if (boundary == null) throw new ArgumentNullException(nameof(boundary));
        failure = SimulationRuntimeAdvanceFailure.None;
        if (!ReferenceEquals(boundary.ExecutionState, this))
        {
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        bool scopeRequired = HasRuntimeAdmission;
        if (scopeRequired && !TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance))
        {
            boundary.FaultRuntimeAdmission();
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
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
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }
        return advanced;
    }

    private bool TryAdvanceDayWithinOperationScope(
        IDailyV1RuntimeAdvanceBoundary boundary,
        out SimulationRuntimeAdvanceFailure failure)
    {
        failure = SimulationRuntimeAdvanceFailure.None;

        if (!IsAdmissionOwnerThreadCurrent(boundary))
        {
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        if (IsDailyCaptureProfile)
        {
            if (!CanMutate)
            {
                failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                return false;
            }
            if (!TryPreflightDailyCoreSequenceCapacity(1L, out failure)) return false;
            if (!TryValidateClockAdvance(out failure)) return false;
        }

        if (!TryBeginAdvance(HasRuntimeAdmission, out DailyV1AdvanceLease lease))
        {
            failure = SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress;
            return false;
        }

        using (lease)
        {
            if (HasRuntimeAdmission)
            {
                if (!CanMutate)
                {
                    failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                    return false;
                }

                if (!IsDailyCaptureProfile
                    && !TryValidateClockAdvance(out failure))
                    return false;

                if (!boundary.TryEnterDailyAdvanceOperation(out IDisposable operationScope))
                {
                    failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
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
        out SimulationRuntimeAdvanceFailure failure)
    {
        if (boundary == null) throw new ArgumentNullException(nameof(boundary));
        daysAdvanced = 0;
        failure = SimulationRuntimeAdvanceFailure.None;
        if (!ReferenceEquals(boundary.ExecutionState, this))
        {
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }
        if (dayCount < 0)
        {
            failure = SimulationRuntimeAdvanceFailure.InvalidDayCount;
            return false;
        }
        if (dayCount == 0) return true;

        bool scopeRequired = HasRuntimeAdmission;
        if (scopeRequired && !TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance))
        {
            boundary.FaultRuntimeAdmission();
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
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
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }
        return advanced;
    }

    private bool TryAdvanceDaysWithinOperationScope(
        IDailyV1RuntimeAdvanceBoundary boundary,
        int dayCount,
        out int daysAdvanced,
        out SimulationRuntimeAdvanceFailure failure)
    {
        daysAdvanced = 0;
        failure = SimulationRuntimeAdvanceFailure.None;

        if (!IsAdmissionOwnerThreadCurrent(boundary))
        {
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        if (IsDailyCaptureProfile)
        {
            if (!CanMutate)
            {
                failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                return false;
            }
            if (!TryPreflightDailyCoreSequenceCapacity(dayCount, out failure)) return false;
            if (!TryValidateClockAdvance(out failure)) return false;
        }

        if (!TryBeginAdvance(HasRuntimeAdmission, out DailyV1AdvanceLease lease))
        {
            failure = SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress;
            return false;
        }

        using (lease)
        {
            if (!CanMutate)
            {
                failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                return false;
            }

            if (HasRuntimeAdmission && !IsDailyCaptureProfile
                && !TryValidateClockAdvance(out failure))
                return false;

            IDisposable operationScope = null;
            if (HasRuntimeAdmission
                && !boundary.TryEnterDailyAdvanceOperation(out operationScope))
            {
                failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
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
        out SimulationRuntimeAdvanceFailure failure)
    {
        if (preferIntradayTimeline && boundary.HasIntradayTimeline)
            return boundary.TryAdvanceIntradayToNextDayBoundary(out failure);

        if (HasAdmittedSelectedDailyExecution)
        {
            SimulationTime clock = SimulationTime;
            if (clock == null)
            {
                failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                return false;
            }
            if (!CanMutate)
            {
                failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                return false;
            }
            DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
            if (!boundary.TryCreateSelectedDailyExecutionBoundary(
                    out IDailyV1SelectedDailyExecutionBoundary executionBoundary)
                || graph == null
                || graph.Definitions == null
                || executionBoundary == null)
            {
                failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                boundary.FaultRuntimeAdmission();
                return false;
            }
            if (!SimulationTimeOwnerBridge.TryAdvanceDayFromRuntime(
                    clock, out SimulationTimeAdvanceFailure timeFailure))
            {
                failure = MapClockFailure(timeFailure);
                return false;
            }

            DailyV1SelectedDailyExecutionStage.Advance(this, graph, executionBoundary);
            failure = SimulationRuntimeAdvanceFailure.None;
            return true;
        }

        if (boundary.HasIntradayTimeline)
            return boundary.TryAdvanceIntradayToNextDayBoundary(out failure);

        return boundary.TryAdvanceDayCore(out failure);
    }

    private bool TryRecordCompletedCoreBoundary(
        IDailyV1RuntimeAdvanceBoundary boundary,
        out SimulationRuntimeAdvanceFailure failure)
    {
        failure = SimulationRuntimeAdvanceFailure.None;
        if (!IsDailyCaptureProfile) return true;
        if (TryRecordCompletedCore()) return true;

        boundary.FaultRuntimeAdmission();
        failure = SimulationRuntimeAdvanceFailure.CompletedBoundarySequenceOverflow;
        return false;
    }

    private bool TryPublishCompletedDailyCaptureToken(
        IDailyV1RuntimeAdvanceBoundary boundary,
        out SimulationRuntimeAdvanceFailure failure)
    {
        failure = SimulationRuntimeAdvanceFailure.None;
        if (!boundary.TryCaptureCompletedDailyCaptureEvidence(
                out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
                out long mutationEpoch,
                out _)
            || !TryIssueCompletedAdvanceCaptureToken(
                mutationEpoch, ownerSections, out _))
        {
            boundary.FaultRuntimeAdmission();
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        return true;
    }

    private bool TryValidateClockAdvance(out SimulationRuntimeAdvanceFailure failure)
    {
        SimulationTime clock = SimulationTime;
        if (clock == null)
        {
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        bool valid = SimulationTimeOwnerBridge.TryValidateAdvance(
            clock, out SimulationTimeAdvanceFailure timeFailure);
        failure = valid ? SimulationRuntimeAdvanceFailure.None : MapClockFailure(timeFailure);
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
        out SimulationRuntimeAdvanceFailure failure)
    {
        if (CanRecordCompletedCoreCount(requestedBoundaries))
        {
            failure = SimulationRuntimeAdvanceFailure.None;
            return true;
        }
        failure = SimulationRuntimeAdvanceFailure.CompletedBoundarySequenceOverflow;
        return false;
    }

    private bool HasRuntimeAdmission => AdmissionContext != null;
    private bool IsDailyCaptureProfile => AdmissionContext?.Profile
        == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1;

    private static SimulationRuntimeAdvanceFailure MapClockFailure(
        SimulationTimeAdvanceFailure failure) =>
        failure == SimulationTimeAdvanceFailure.AbsoluteDayOverflow
            ? SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow
            : SimulationRuntimeAdvanceFailure.RuntimeFaulted;
}
