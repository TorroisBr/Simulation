using System;
using System.Collections.Generic;

internal static class DailyV1RuntimeProtocolOperationContracts
{
    internal const string NpcMembership = "runtime.npc-membership";
    internal const string PersonParentage = "runtime.person.parentage";
    internal const string BootstrapPublication = "runtime.bootstrap-publication";
    internal const string DailyAdvance = "runtime.advance-day";
    internal const string SoloTravelStart = "runtime.travel.start";
    internal const string TravelPartyStart = "runtime.travel-party.start";
    internal const string TravelPartyAdvance = "runtime.travel-party.advance";
    internal const string NpcTrade = "runtime.economy.npc-trade";
    internal const string NpcMoneyTransfer = "runtime.economy.money-transfer";
    internal const string MarketPurchase = "runtime.economy.market-purchase";
    internal const string MarketSale = "runtime.economy.market-sale";
    internal const string MerchantDailyNpcTrade = "runtime.merchant.advance-npc-trade-state";
    internal const string InstitutionOfficeOwnerCommit = "p12.institution-office.owner-commit";
    internal const string FactionStoreOwnerCommit = "p12.faction.owner-commit";
    internal const string PoliticalClaimOwnerCommit = "p12.political-claim.owner-commit";
    internal const string PoliticalSupportOwnerCommit = "p12.political-support.owner-commit";
    internal const string PropertyOwnerCommit = "p12.property.owner-commit";
    internal const string EstateOwnerCommit = "p12.estate.owner-commit";
    internal const string PopulationImmigration = "runtime.population.immigration";
    internal const string PopulationEmigration = "runtime.population.emigration";
    internal const string PopulationResidentDeath = "runtime.population.resident-death";
    internal const string PopulationResidenceMigration = "runtime.population.residence-migration";
    internal const string PersonDeath = "runtime.person.death";
    internal const string PersonResidenceBind = "runtime.person.residence-bind";

    internal static IReadOnlyList<string> GetExpectedOperationContractIds(
        bool hasRuntimeAdmission,
        bool hasTravelPartySystem,
        bool hasTravelSystem)
    {
        var operationContractIds = new List<string>(24) { NpcMembership };
        if (!hasRuntimeAdmission) return operationContractIds.AsReadOnly();

        operationContractIds.Add(BootstrapPublication);
        operationContractIds.Add(DailyAdvance);
        operationContractIds.Add(PersonParentage);
        if (hasTravelPartySystem)
        {
            operationContractIds.Add(TravelPartyStart);
            operationContractIds.Add(TravelPartyAdvance);
        }
        if (hasTravelSystem) operationContractIds.Add(SoloTravelStart);
        operationContractIds.Add(NpcTrade);
        operationContractIds.Add(NpcMoneyTransfer);
        operationContractIds.Add(MarketPurchase);
        operationContractIds.Add(MarketSale);
        operationContractIds.Add(MerchantDailyNpcTrade);
        operationContractIds.Add(InstitutionOfficeOwnerCommit);
        operationContractIds.Add(FactionStoreOwnerCommit);
        operationContractIds.Add(PoliticalClaimOwnerCommit);
        operationContractIds.Add(PoliticalSupportOwnerCommit);
        operationContractIds.Add(PropertyOwnerCommit);
        operationContractIds.Add(EstateOwnerCommit);
        operationContractIds.Add(PopulationImmigration);
        operationContractIds.Add(PopulationEmigration);
        operationContractIds.Add(PopulationResidentDeath);
        operationContractIds.Add(PopulationResidenceMigration);
        operationContractIds.Add(PersonDeath);
        operationContractIds.Add(PersonResidenceBind);
        return operationContractIds.AsReadOnly();
    }

    /// <summary>
    /// Accepts only a complete selected-runtime inventory emitted by this Core
    /// catalogue. A host may register the IDs during composition, but it cannot
    /// admit a partial or host-invented operation set.
    /// </summary>
    internal static bool IsSupportedSelectedOperationInventory(
        IEnumerable<string> operationContractIds)
    {
        if (operationContractIds == null) return false;

        HashSet<string> supplied = new HashSet<string>(StringComparer.Ordinal);
        foreach (string operationContractId in operationContractIds)
            if (string.IsNullOrWhiteSpace(operationContractId)
                || !supplied.Add(operationContractId)) return false;

        if (supplied.Count == 0) return false;
        for (int travelParty = 0; travelParty <= 1; travelParty++)
        for (int travel = 0; travel <= 1; travel++)
        {
            IReadOnlyList<string> expected = GetExpectedOperationContractIds(
                hasRuntimeAdmission: true,
                hasTravelPartySystem: travelParty == 1,
                hasTravelSystem: travel == 1);
            if (expected.Count != supplied.Count) continue;

            bool matches = true;
            foreach (string operationContractId in expected)
                if (!supplied.Contains(operationContractId))
                {
                    matches = false;
                    break;
                }

            if (matches) return true;
        }

        return false;
    }
}

/// <summary>
/// Common P12 scope, projection, census and fault ports used by Core-owned
/// selected execution and legacy runtime advancement.
/// </summary>
internal interface IDailyV1RuntimeAdvanceEvidenceBoundary
{
    DailyV1ExecutionBoundaryState ExecutionState { get; }
    bool TryEnterDailyAdvanceOperation(out IDisposable operationScope);
    /// <summary>
    /// Builds and validates the exact selected stage adapters without mutating
    /// runtime state. Core calls this before advancing the admitted clock so a
    /// missing adapter cannot leave a rejected advance at a later day.
    /// </summary>
    bool TryCreateSelectedDailyExecutionBoundary(
        out IDailyV1SelectedDailyExecutionBoundary executionBoundary);
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
/// Host-owned ports for the selected Daily-v1 session. This intentionally
/// excludes runtime clocks, whole-day advancement, and intraday timeline
/// selection; Core binds those authorities to the exact execution state.
/// </summary>
internal interface IDailyV1SelectedRuntimeAdvanceHostPorts
{
    DailyV1ExecutionBoundaryState ExecutionState { get; }
    bool TryBindApplicationAdvanceAuthority(
        DailyV1Application application,
        DailyV1ActiveRuntimeSession session);
    bool TryEnterRuntimeProtocolOperation(
        string operationContractId,
        out IDisposable operationScope);
    bool TryCreateSelectedDailyExecutionBoundary(
        out IDailyV1SelectedDailyExecutionBoundary executionBoundary);
    bool TryCaptureOwnerSectionCensus(
        DailyV1CaptureScopeAllowance allowedOuterScope,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure);
    void FaultRuntimeAdmission();
}

/// <summary>
/// Legacy boundary for callers that still own their own day or intraday
/// implementation. Selected Daily-v1 application sessions use the Core
/// selected boundary and cannot reach these host advancement members.
/// </summary>
internal interface IDailyV1RuntimeAdvanceBoundary
    : IDailyV1RuntimeAdvanceEvidenceBoundary
{
    bool HasIntradayTimeline { get; }
    bool TryAdvanceDayCore(out SimulationRuntimeAdvanceFailure failure);
    bool TryAdvanceIntradayToNextDayBoundary(out SimulationRuntimeAdvanceFailure failure);
}

/// <summary>
/// Core's binding between one selected session's exact execution state and
/// the host's P12/projection ports. It implements only the evidence boundary;
/// the Core state performs the selected advance and has no host-day fallback.
/// </summary>
internal sealed class DailyV1CoreSelectedRuntimeAdvanceBoundary
    : IDailyV1RuntimeAdvanceEvidenceBoundary
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts;

    internal DailyV1CoreSelectedRuntimeAdvanceBoundary(
        DailyV1ExecutionBoundaryState executionState,
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts)
    {
        this.executionState = executionState
            ?? throw new ArgumentNullException(nameof(executionState));
        this.hostPorts = hostPorts
            ?? throw new ArgumentNullException(nameof(hostPorts));
        if (!executionState.HasAdmittedSelectedDailyExecution
            || executionState.SelectedRuntimeOwnerGraph == null
            || executionState.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !ReferenceEquals(hostPorts.ExecutionState, executionState))
            throw new ArgumentException(
                "The Core selected runtime boundary requires the admitted Daily-v1 graph and profile.",
                nameof(executionState));
    }

    internal bool IsBoundTo(
        DailyV1ExecutionBoundaryState candidateState,
        IDailyV1SelectedRuntimeAdvanceHostPorts candidateHostPorts) =>
        ReferenceEquals(executionState, candidateState)
        && ReferenceEquals(hostPorts, candidateHostPorts);

    public DailyV1ExecutionBoundaryState ExecutionState => executionState;

    public bool TryEnterDailyAdvanceOperation(out IDisposable operationScope) =>
        hostPorts.TryEnterRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance,
            out operationScope);

    public bool TryCreateSelectedDailyExecutionBoundary(
        out IDailyV1SelectedDailyExecutionBoundary executionBoundary) =>
        hostPorts.TryCreateSelectedDailyExecutionBoundary(out executionBoundary);

    public bool TryCaptureCompletedDailyCaptureEvidence(
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure) =>
        TryCaptureOwnerSectionCensus(
            DailyV1CaptureScopeAllowance.DailyAdvance,
            out ownerSections, out mutationEpoch, out failure);

    public bool TryCaptureDailyBoundaryAdmissionEvidence(
        bool sessionReplacement,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure) =>
        TryCaptureOwnerSectionCensus(
            sessionReplacement
                ? DailyV1CaptureScopeAllowance.SessionReplacementValidation
                : DailyV1CaptureScopeAllowance.None,
            out ownerSections, out mutationEpoch, out failure);

    public bool TryCaptureContinuationSaveEvidence(
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure) =>
        TryCaptureOwnerSectionCensus(
            DailyV1CaptureScopeAllowance.ContinuationSave,
            out ownerSections, out mutationEpoch, out failure);

    private bool TryCaptureOwnerSectionCensus(
        DailyV1CaptureScopeAllowance allowedOuterScope,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure)
    {
        ownerSections = null;
        mutationEpoch = 0L;
        if (!Enum.IsDefined(typeof(DailyV1CaptureScopeAllowance), allowedOuterScope))
        {
            failure = DailyCaptureEligibilityFailure.RuntimeFaulted;
            return false;
        }

        // Core selects the only scope that may remain active around this
        // snapshot. Recheck after the host reads the owner census so a
        // reentrant scope change cannot authorize a stale capture.
        if (!executionState.TryAssessSelectedCaptureBoundary(allowedOuterScope, out failure))
            return false;

        if (!hostPorts.TryCaptureOwnerSectionCensus(
                allowedOuterScope, out ownerSections, out mutationEpoch, out failure))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            return false;
        }

        if (failure != DailyCaptureEligibilityFailure.None)
        {
            ownerSections = null;
            mutationEpoch = 0L;
            return false;
        }
        if (mutationEpoch < 0L)
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.RuntimeFaulted;
            return false;
        }
        if (ownerSections == null
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(ownerSections))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (!executionState.TryAssessSelectedCaptureBoundary(allowedOuterScope, out failure))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            return false;
        }

        return true;
    }

    public void FaultRuntimeAdmission() => hostPorts.FaultRuntimeAdmission();
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
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
        bool sessionReplacement) => HasValidDailyBoundaryAdmission(
            boundary,
            sessionReplacement
                ? (DailyCaptureBoundaryProvenance?)null
                : DailyCaptureBoundaryProvenance.CompletedAdvance,
            sessionReplacement);

    internal bool HasValidRestoredDailyBoundaryAdmission(
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary) => HasValidDailyBoundaryAdmission(
            boundary, DailyCaptureBoundaryProvenance.RestoredContinuation, sessionReplacement: false);

    internal bool TryValidateRestoredDailyBoundaryAdmission(
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
        out string diagnostic)
    {
        diagnostic = null;
        if (boundary == null || !ReferenceEquals(boundary.ExecutionState, this))
        {
            diagnostic = "The restored boundary is not bound to this Core execution state.";
            return false;
        }
        if (!IsHealthyDailyOwnerThreadBoundary())
        {
            diagnostic = "The Core execution state is not at a healthy owner-thread boundary.";
            return false;
        }

        DailyCaptureEligibilityToken current = currentCaptureToken;
        if (current == null
            || current.BoundaryProvenance != DailyCaptureBoundaryProvenance.RestoredContinuation)
        {
            diagnostic = "The Core execution state has no restored-continuation token.";
            return false;
        }
        if (!boundary.TryCaptureDailyBoundaryAdmissionEvidence(
                sessionReplacement: false,
                out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
                out long mutationEpoch,
                out DailyCaptureEligibilityFailure captureFailure))
        {
            diagnostic = "The restored owner census could not be recaptured: " + captureFailure + ".";
            return false;
        }
        if (!IsHealthyDailyOwnerThreadBoundary()
            || !ReferenceEquals(currentCaptureToken, current)
            || current.BoundaryProvenance != DailyCaptureBoundaryProvenance.RestoredContinuation)
        {
            diagnostic = "The Core boundary changed during restored owner census recapture.";
            return false;
        }
        if (SimulationTime == null
            || !TryCompareCurrentCaptureToken(
                current,
                AdmissionContext,
                ConfigurationIdentity,
                CalendarIdentity,
                CompositionProfile,
                WorldId,
                SimulationTime.AbsoluteDay,
                ownerSections,
                mutationEpoch))
        {
            diagnostic = "The restored token no longer matches the exact owner vector, epoch, or runtime identities.";
            return false;
        }

        return true;
    }

    private bool HasValidDailyBoundaryAdmission(
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
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
        out SimulationRuntimeAdvanceFailure failure) =>
        TryAdvanceDayCore(boundary, allowLegacyAdvance: true, out failure);

    /// <summary>
    /// Advances a selected Daily-v1 execution state through the Core-owned
    /// boundary bound to its active session. This path cannot select a Unity
    /// day or intraday implementation.
    /// </summary>
    internal bool TryAdvanceSelectedDailyV1Day(
        DailyV1CoreSelectedRuntimeAdvanceBoundary boundary,
        out SimulationRuntimeAdvanceFailure failure) =>
        TryAdvanceDayCore(boundary, allowLegacyAdvance: false, out failure);

    private bool TryAdvanceDayCore(
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
        bool allowLegacyAdvance,
        out SimulationRuntimeAdvanceFailure failure)
    {
        if (boundary == null) throw new ArgumentNullException(nameof(boundary));
        failure = SimulationRuntimeAdvanceFailure.None;
        if (!ReferenceEquals(boundary.ExecutionState, this)
            || (!allowLegacyAdvance
                && (!IsDailyCaptureProfile || !HasAdmittedSelectedDailyExecution)))
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
            advanced = TryAdvanceDayWithinOperationScope(
                boundary, allowLegacyAdvance, out failure);
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
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
        bool allowLegacyAdvance,
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
                            boundary, preferIntradayTimeline: false,
                            allowLegacyAdvance, out failure);
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

            return TryAdvanceLegacyBoundary(boundary, out failure);
        }
    }

    internal bool TryAdvanceDays(
        IDailyV1RuntimeAdvanceBoundary boundary,
        int dayCount,
        out int daysAdvanced,
        out SimulationRuntimeAdvanceFailure failure) =>
        TryAdvanceDaysCore(boundary, dayCount, allowLegacyAdvance: true,
            out daysAdvanced, out failure);

    internal bool TryAdvanceSelectedDailyV1Days(
        DailyV1CoreSelectedRuntimeAdvanceBoundary boundary,
        int dayCount,
        out int daysAdvanced,
        out SimulationRuntimeAdvanceFailure failure) =>
        TryAdvanceDaysCore(boundary, dayCount, allowLegacyAdvance: false,
            out daysAdvanced, out failure);

    private bool TryAdvanceDaysCore(
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
        int dayCount,
        bool allowLegacyAdvance,
        out int daysAdvanced,
        out SimulationRuntimeAdvanceFailure failure)
    {
        if (boundary == null) throw new ArgumentNullException(nameof(boundary));
        daysAdvanced = 0;
        failure = SimulationRuntimeAdvanceFailure.None;
        if (!ReferenceEquals(boundary.ExecutionState, this)
            || (!allowLegacyAdvance
                && (!IsDailyCaptureProfile || !HasAdmittedSelectedDailyExecution)))
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
                boundary, dayCount, allowLegacyAdvance, out daysAdvanced, out failure);
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
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
        int dayCount,
        bool allowLegacyAdvance,
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
                            boundary, preferIntradayTimeline: true,
                            allowLegacyAdvance, out failure);
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
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
        bool preferIntradayTimeline,
        bool allowLegacyAdvance,
        out SimulationRuntimeAdvanceFailure failure)
    {
        IDailyV1RuntimeAdvanceBoundary legacyBoundary =
            allowLegacyAdvance ? boundary as IDailyV1RuntimeAdvanceBoundary : null;
        if (preferIntradayTimeline && legacyBoundary?.HasIntradayTimeline == true)
            return legacyBoundary.TryAdvanceIntradayToNextDayBoundary(out failure);

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

        return TryAdvanceLegacyBoundary(boundary, out failure);
    }

    private bool TryAdvanceLegacyBoundary(
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
        out SimulationRuntimeAdvanceFailure failure)
    {
        if (boundary is IDailyV1RuntimeAdvanceBoundary legacyBoundary)
        {
            if (legacyBoundary.HasIntradayTimeline)
                return legacyBoundary.TryAdvanceIntradayToNextDayBoundary(out failure);
            return legacyBoundary.TryAdvanceDayCore(out failure);
        }

        failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
        return false;
    }

    private bool TryRecordCompletedCoreBoundary(
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
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
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
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

    private bool IsAdmissionOwnerThreadCurrent(IDailyV1RuntimeAdvanceEvidenceBoundary boundary)
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
