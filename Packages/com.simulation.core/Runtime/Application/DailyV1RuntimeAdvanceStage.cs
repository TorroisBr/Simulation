using System;
using System.Collections.Generic;
using System.Threading;

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
/// Host-owned ports for the selected Daily-v1 session. Census ports return
/// only owner rows that have not yet migrated to the Core owner graph; Core
/// composes the exact P12-F rows itself. The host still supplies quiescence,
/// owner-thread, and shared mutation-epoch evidence for the complete protocol.
/// This intentionally excludes runtime clocks, whole-day advancement, and
/// intraday timeline selection; Core binds those authorities to the exact
/// execution state.
/// </summary>
internal interface IDailyV1SelectedRuntimeAdvanceHostPorts
{
    DailyV1ExecutionBoundaryState ExecutionState { get; }
    bool CanBeginDailyAdvanceOperation { get; }
    bool IsFactualReadWorldPublished { get; }
    bool HasActiveDailyOperationContext { get; }
    bool TryCreateSelectedDailyExecutionBoundary(
        out IDailyV1SelectedDailyExecutionBoundary executionBoundary);
    bool TryCaptureOwnerSectionCensus(
        DailyV1CaptureScopeAllowance allowedOuterScope,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure);
    bool TryBindRestoredCandidateMutationGuard(out string diagnostic);
    bool TryCaptureUnadmittedRestoredDailyOwnerVector(
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out string diagnostic);
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

    public bool TryEnterDailyAdvanceOperation(out IDisposable operationScope)
    {
        operationScope = null;
        if (!hostPorts.CanBeginDailyAdvanceOperation)
        {
            hostPorts.FaultRuntimeAdmission();
            return false;
        }

        if (!executionState.TryBeginRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance))
        {
            hostPorts.FaultRuntimeAdmission();
            return false;
        }

        operationScope = new RuntimeProtocolOperationScope(
            executionState, hostPorts.FaultRuntimeAdmission);
        return true;
    }

    private sealed class RuntimeProtocolOperationScope : IDisposable
    {
        private readonly DailyV1ExecutionBoundaryState executionState;
        private readonly Action faultRuntimeAdmission;
        private int disposed;

        internal RuntimeProtocolOperationScope(
            DailyV1ExecutionBoundaryState executionState,
            Action faultRuntimeAdmission)
        {
            this.executionState = executionState
                ?? throw new ArgumentNullException(nameof(executionState));
            this.faultRuntimeAdmission = faultRuntimeAdmission
                ?? throw new ArgumentNullException(nameof(faultRuntimeAdmission));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            if (executionState.TryEndRuntimeProtocolOperation()) return;

            executionState.InvalidateCurrentCaptureToken();
            faultRuntimeAdmission();
        }
    }

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

        SimulationRuntimeAdmissionContext admissionContext = executionState.AdmissionContext;
        if (admissionContext == null
            || admissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
        {
            failure = DailyCaptureEligibilityFailure.UnsupportedProfile;
            return false;
        }

        if (!admissionContext.IsOwnedByCurrentThread())
        {
            failure = DailyCaptureEligibilityFailure.WrongOwnerThread;
            return false;
        }

        bool dailyAdvanceScope = allowedOuterScope == DailyV1CaptureScopeAllowance.DailyAdvance;
        if (executionState.AdvanceLeaseHeld != dailyAdvanceScope
            || hostPorts.HasActiveDailyOperationContext)
        {
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        if (!hostPorts.IsFactualReadWorldPublished || executionState.WorldId == null)
        {
            failure = DailyCaptureEligibilityFailure.WorldNotPublished;
            return false;
        }

        if (!executionState.CanMutate)
        {
            failure = DailyCaptureEligibilityFailure.RuntimeFaulted;
            return false;
        }

        // Core selects the only scope that may remain active around this
        // snapshot. The second assessment below rejects reentrant scope changes.
        if (!executionState.TryAssessCaptureScope(allowedOuterScope))
        {
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        bool captured = hostPorts.TryCaptureOwnerSectionCensus(
            allowedOuterScope,
            out ownerSections,
            out mutationEpoch,
            out failure);
        if (!captured)
        {
            ownerSections = null;
            mutationEpoch = 0L;
            if (failure == DailyCaptureEligibilityFailure.None)
                failure = DailyCaptureEligibilityFailure.RuntimeFaulted;
            return false;
        }

        if (executionState.AdvanceLeaseHeld != dailyAdvanceScope
            || hostPorts.HasActiveDailyOperationContext
            || !executionState.TryAssessCaptureScope(allowedOuterScope))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryValidateExcludedNpcReceiptSections(
                executionState.SelectedRuntimeOwnerGraph,
                ownerSections,
                out _))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedGenealogySection(
                executionState.SelectedRuntimeOwnerGraph,
                ownerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> coreOwnedGenealogySections,
                out _))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }
        ownerSections = coreOwnedGenealogySections;

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPersonStoreSections(
                executionState.SelectedRuntimeOwnerGraph,
                ownerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> coreOwnedPersonStoreSections,
                out _))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }
        ownerSections = coreOwnedPersonStoreSections;

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                executionState.SelectedRuntimeOwnerGraph,
                executionState.SelectedActorChoiceStore,
                ownerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> coreOwnedP12FSections,
                out _))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }
        ownerSections = coreOwnedP12FSections;

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcSections(
                executionState.SelectedRuntimeOwnerGraph,
                ownerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> coreOwnedNpcSections,
                out _))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }
        ownerSections = coreOwnedNpcSections;

        if (executionState.SelectedRuntimeOwnerGraph.PersonStore != null)
        {
            if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcRuntimeStateSections(
                    executionState.SelectedRuntimeOwnerGraph,
                    ownerSections,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> coreOwnedNpcRuntimeStateSections,
                    out _))
            {
                ownerSections = null;
                mutationEpoch = 0L;
                failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                return false;
            }

            ownerSections = coreOwnedNpcRuntimeStateSections;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCommercialKnowledgeSections(
                executionState.SelectedRuntimeOwnerGraph,
                ownerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> coreOwnedCommercialKnowledgeSections,
                out _))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }
        ownerSections = coreOwnedCommercialKnowledgeSections;

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
                executionState.SelectedRuntimeOwnerGraph,
                ownerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> coreOwnedNpcKnowledgeAndPlanSections,
                out _))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }
        ownerSections = coreOwnedNpcKnowledgeAndPlanSections;

        if (executionState.SelectedRuntimeOwnerGraph.PersonStore != null)
        {
            if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedPersonStoreSections(
                    executionState.SelectedRuntimeOwnerGraph,
                    ownerSections,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> coreOwnedPersonSections,
                    out _))
            {
                ownerSections = null;
                mutationEpoch = 0L;
                failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                return false;
            }

            ownerSections = coreOwnedPersonSections;
        }

        if (executionState.SelectedRuntimeOwnerGraph.PersonStore != null)
        {
            if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCorePersonLifeResidenceSections(
                    executionState.SelectedRuntimeOwnerGraph,
                    ownerSections,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> coreOwnedPersonLifeResidenceSections,
                    out _))
            {
                ownerSections = null;
                mutationEpoch = 0L;
                failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                return false;
            }

            ownerSections = coreOwnedPersonLifeResidenceSections;
        }
        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreSpatialKnowledgeSections(
                executionState.SelectedRuntimeOwnerGraph,
                ownerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> coreOwnedSpatialKnowledgeSections,
                out _))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }
        ownerSections = coreOwnedSpatialKnowledgeSections;

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCitySections(
                executionState.SelectedRuntimeOwnerGraph,
                ownerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> coreOwnedCitySections,
                out _))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }
        ownerSections = coreOwnedCitySections;

        failure = DailyCaptureEligibilityFailure.None;
        return true;
    }


    public void FaultRuntimeAdmission() => hostPorts.FaultRuntimeAdmission();
}

/// <summary>
/// Rebinds selected census rows from the exact Core owner graph after the
/// host captures its complete profile vector. Rebinding requires matching
/// identity, schema, cardinality, role, and revision; stale facts remain intact
/// for P12 classification, and mixed rosters retain the complete host vector.
/// </summary>
internal static class DailyV1SelectedRuntimeOwnerCensusSource
{
    private sealed class ReferenceIdentityComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceIdentityComparer Instance = new ReferenceIdentityComparer();

        public new bool Equals(object left, object right) => ReferenceEquals(left, right);

        public int GetHashCode(object value) =>
            value == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }

    private static readonly string[] CoreOwnedPersonStoreSectionIdValues =
    {
        DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
        DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId
    };

    private static readonly string[] CoreOwnedP12FSectionIdValues =
    {
        ActorChoiceP11CensusProvider.SectionId,
        ScheduledDirectiveCensusProvider.SectionId,
        TravelPartyCensusProvider.SectionId,
        ExpeditionCensusProvider.SectionId
    };

    /// <summary>
    /// Fixed selected-profile rows whose live source is the exact Core session
    /// graph. The Unity census protocol still tracks quiescence and mutation
    /// epochs, but its host port must omit these rows from the vector returned
    /// to Core so it cannot supply their owner identity or values.
    /// </summary>
    internal static IReadOnlyList<string> CoreOwnedP12FSectionIds { get; } =
        Array.AsReadOnly(CoreOwnedP12FSectionIdValues);

    internal static IReadOnlyList<string> CoreOwnedPersonStoreSectionIds { get; } =
        Array.AsReadOnly(CoreOwnedPersonStoreSectionIdValues);

    private static readonly string[] CoreOwnedSelectedRuntimeSectionIdValues =
    {
        DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
        DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
        ActorChoiceP11CensusProvider.SectionId,
        ScheduledDirectiveCensusProvider.SectionId,
        TravelPartyCensusProvider.SectionId,
        ExpeditionCensusProvider.SectionId
    };

    internal static IReadOnlyList<string> CoreOwnedSelectedRuntimeSectionIds { get; } =
        Array.AsReadOnly(CoreOwnedSelectedRuntimeSectionIdValues);

    internal static bool TryRemoveCoreOwnedSelectedRuntimeSections(
        IReadOnlyList<OwnerSectionCensusSnapshot> completeHostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedSections,
        out string diagnostic) =>
        TryRemoveCoreOwnedSections(completeHostSections,
            CoreOwnedSelectedRuntimeSectionIdValues,
            "selected Core-runtime",
            out hostOwnedSections,
            out diagnostic);

    internal static bool TryRemoveCoreOwnedP12FSections(
        IReadOnlyList<OwnerSectionCensusSnapshot> completeHostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedSections,
        out string diagnostic)
        => TryRemoveCoreOwnedSections(completeHostSections,
            CoreOwnedP12FSectionIdValues,
            "fixed Core-owned P12-F",
            out hostOwnedSections,
            out diagnostic);

    /// <summary>
    /// Enforces the two excluded P18 receipt-owner rows in the admitted P12-D
    /// profile. The host census remains the source of their real owner
    /// identities; Core requires a complete per-NPC pair and exact zero
    /// cardinality/revision without exporting or replaying receipt data.
    /// </summary>
    internal static bool TryValidateExcludedNpcReceiptSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        out string diagnostic)
    {
        diagnostic = null;
        if (graph == null || sections == null || graph.Npcs == null)
        {
            diagnostic = "The excluded NPC receipt census requires the admitted Core graph and host rows.";
            return false;
        }

        HashSet<string> selectedRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || npc.RuntimeId.IndexOf('/') >= 0 || npc.RuntimeId.IndexOf('\0') >= 0
                || !selectedRuntimeIds.Add(npc.RuntimeId))
            {
                diagnostic = "The selected Core NPC graph has an invalid or duplicate RuntimeId.";
                return false;
            }
        }
        if (selectedRuntimeIds.Count == 0)
        {
            diagnostic = "The selected Daily-v1 receipt census requires at least one admitted NPC.";
            return false;
        }

        Dictionary<string, OwnerSectionCensusSnapshot> rowsById =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        Dictionary<string, OwnerSectionCensusSnapshot> localRows =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        Dictionary<string, OwnerSectionCensusSnapshot> merchantRows =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        HashSet<object> receiptOwnerIdentities =
            new HashSet<object>(ReferenceIdentityComparer.Instance);

        foreach (OwnerSectionCensusSnapshot row in sections)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.SectionId)
                || !rowsById.TryAdd(row.SectionId, row))
            {
                diagnostic = "The host census contains a missing or duplicate owner section.";
                return false;
            }

            bool local = row.SectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsPrefix,
                StringComparison.Ordinal);
            bool merchant = row.SectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradeStateReceiptsPrefix,
                StringComparison.Ordinal);
            if (!local && !merchant) continue;

            string prefix = local
                ? DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsPrefix
                : DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradeStateReceiptsPrefix;
            string runtimeId = row.SectionId.Substring(prefix.Length);
            if (string.IsNullOrWhiteSpace(runtimeId) || runtimeId.IndexOf('/') >= 0
                || runtimeId.IndexOf('\0') >= 0
                || !string.Equals(row.SectionId, local
                    ? DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsSectionIdFor(runtimeId)
                    : DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradeStateReceiptsSectionIdFor(runtimeId),
                    StringComparison.Ordinal)
                || row.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || row.Role != OwnerSectionRole.Required
                || row.Cardinality != 0 || row.Revision != 0L
                || row.OwnerInstanceIdentity == null
                || !receiptOwnerIdentities.Add(row.OwnerInstanceIdentity)
                || !(local ? localRows : merchantRows).TryAdd(runtimeId, row))
            {
                diagnostic = "A P12-D excluded NPC receipt owner is missing, aliased, populated, or malformed.";
                return false;
            }
        }

        if (localRows.Count < selectedRuntimeIds.Count
            || merchantRows.Count != localRows.Count)
        {
            diagnostic = "The P12-D receipt census does not contain two rows for every admitted NPC.";
            return false;
        }

        foreach (string runtimeId in selectedRuntimeIds)
        {
            if (!localRows.ContainsKey(runtimeId) || !merchantRows.ContainsKey(runtimeId))
            {
                diagnostic = "The P12-D receipt census is missing a selected Core NPC owner pair.";
                return false;
            }
        }

        foreach (string runtimeId in localRows.Keys)
        {
            if (!merchantRows.ContainsKey(runtimeId))
            {
                diagnostic = "The P12-D receipt census has an unmatched compatibility NPC owner.";
                return false;
            }
        }

        return true;
    }

    private static bool TryRemoveCoreOwnedSections(
        IReadOnlyList<OwnerSectionCensusSnapshot> completeHostSections,
        IReadOnlyList<string> coreSectionIds,
        string scope,
        out IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedSections,
        out string diagnostic)
    {
        hostOwnedSections = null;
        diagnostic = null;
        if (!DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(completeHostSections))
        {
            diagnostic = "The host census is not a complete selected Daily-v1 owner vector.";
            return false;
        }

        HashSet<string> remainingCoreSectionIds =
            new HashSet<string>(coreSectionIds, StringComparer.Ordinal);
        List<OwnerSectionCensusSnapshot> remaining =
            new List<OwnerSectionCensusSnapshot>(completeHostSections.Count - coreSectionIds.Count);
        int removed = 0;
        foreach (OwnerSectionCensusSnapshot section in completeHostSections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId))
            {
                diagnostic = "The complete host census contains an invalid section.";
                return false;
            }

            if (remainingCoreSectionIds.Remove(section.SectionId))
            {
                removed++;
                continue;
            }

            remaining.Add(section);
        }

        if (removed != coreSectionIds.Count || remainingCoreSectionIds.Count != 0)
        {
            diagnostic = "The complete host census does not contain each " + scope
                + " section exactly once.";
            return false;
        }

        hostOwnedSections = Array.AsReadOnly(remaining.ToArray());
        return true;
    }

    internal static bool TryComposeCoreOwnedPersonStoreSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> composedSections,
        out string diagnostic)
    {
        composedSections = null;
        diagnostic = null;
        PersonStore personStore = graph?.PersonStore;
        if (personStore == null || hostOwnedSections == null)
        {
            diagnostic = "The Core PersonStore census requires its exact owner graph and host remainder.";
            return false;
        }

        HashSet<string> hostSectionIds = new HashSet<string>(StringComparer.Ordinal);
        List<OwnerSectionCensusSnapshot> composed =
            new List<OwnerSectionCensusSnapshot>(hostOwnedSections.Count + CoreOwnedPersonStoreSectionIdValues.Length);
        foreach (OwnerSectionCensusSnapshot section in hostOwnedSections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !hostSectionIds.Add(section.SectionId)
                || Array.IndexOf(CoreOwnedPersonStoreSectionIdValues, section.SectionId) >= 0)
            {
                diagnostic = "The host census remainder is invalid, duplicated, or contains a Core-owned PersonStore section.";
                return false;
            }
            composed.Add(section);
        }

        OwnerSectionCensusWitness[] witnesses;
        try
        {
            witnesses = new OwnerSectionCensusWitness[]
            {
                personStore.GetMembershipCensusWitness(),
                personStore.GetMaterializationBindingCensusWitness()
            };
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "The Core PersonStore census could not read its exact owner.";
            return false;
        }

        if (witnesses.Length != CoreOwnedPersonStoreSectionIdValues.Length
            || !HasExactCorePersonStoreCensusAliasPair(
                personStore, witnesses[0], witnesses[1]))
        {
            diagnostic = "The Core PersonStore census aliases do not share the exact owner and revision.";
            return false;
        }

        HashSet<string> capturedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusWitness witness in witnesses)
        {
            if (witness == null
                || string.IsNullOrWhiteSpace(witness.SectionId)
                || !ReferenceEquals(witness.OwnerInstanceIdentity, personStore)
                || witness.SchemaVersion <= 0
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || !capturedSectionIds.Add(witness.SectionId)
                || !Array.Exists(CoreOwnedPersonStoreSectionIdValues,
                    sectionId => string.Equals(sectionId, witness.SectionId, StringComparison.Ordinal)))
            {
                diagnostic = "The Core PersonStore provider returned a witness outside its exact alias pair.";
                return false;
            }

            composed.Add(new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision));
        }

        if (capturedSectionIds.Count != CoreOwnedPersonStoreSectionIdValues.Length)
        {
            diagnostic = "The Core PersonStore provider set is incomplete.";
            return false;
        }

        composed.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        composedSections = Array.AsReadOnly(composed.ToArray());
        return true;
    }

    internal static bool HasExactCorePersonStoreCensusAliasPair(
        PersonStore personStore,
        OwnerSectionCensusWitness membership,
        OwnerSectionCensusWitness materializationBinding) =>
        personStore != null
        && membership != null
        && materializationBinding != null
        && string.Equals(membership.SectionId,
            DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
            StringComparison.Ordinal)
        && string.Equals(materializationBinding.SectionId,
            DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
            StringComparison.Ordinal)
        && ReferenceEquals(membership.OwnerInstanceIdentity, personStore)
        && ReferenceEquals(materializationBinding.OwnerInstanceIdentity, personStore)
        && membership.Revision == materializationBinding.Revision;

    internal static bool TryComposeCoreOwnedP12FSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        ActorChoiceStore actorChoiceStore,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> completeOwnerSections,
        out string diagnostic)
    {
        completeOwnerSections = null;
        diagnostic = null;
        if (graph?.ScheduledDirectives == null
            || graph.TravelParties == null
            || graph.Expeditions == null
            || graph.PersonStore == null
            || actorChoiceStore == null
            || !ReferenceEquals(actorChoiceStore.PersonStore, graph.PersonStore)
            || hostOwnedSections == null)
        {
            diagnostic = "The Core P12-F census requires the exact actor-choice, directive, travel-party, expedition, and PersonStore owners.";
            return false;
        }

        HashSet<string> hostSectionIds = new HashSet<string>(StringComparer.Ordinal);
        List<OwnerSectionCensusSnapshot> composed =
            new List<OwnerSectionCensusSnapshot>(hostOwnedSections.Count + CoreOwnedP12FSectionIdValues.Length);
        foreach (OwnerSectionCensusSnapshot section in hostOwnedSections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !hostSectionIds.Add(section.SectionId)
                || Array.IndexOf(CoreOwnedP12FSectionIdValues, section.SectionId) >= 0)
            {
                diagnostic = "The host census remainder is invalid, duplicated, or contains a Core-owned P12-F section.";
                return false;
            }
            composed.Add(section);
        }

        OwnerSectionCensusWitness[] witnesses;
        try
        {
            witnesses = new OwnerSectionCensusWitness[]
            {
                new ActorChoiceP11CensusProvider(actorChoiceStore).GetCurrentCensus(),
                new ScheduledDirectiveCensusProvider(graph.ScheduledDirectives).GetCurrentCensus(),
                new TravelPartyCensusProvider(graph.TravelParties).GetCurrentCensus(),
                new ExpeditionCensusProvider(graph.Expeditions).GetCurrentCensus()
            };
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "A Core P12-F provider could not read its exact owner.";
            return false;
        }

        HashSet<string> capturedCoreSectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusWitness witness in witnesses)
        {
            if (witness == null
                || string.IsNullOrWhiteSpace(witness.SectionId)
                || witness.OwnerInstanceIdentity == null
                || witness.SchemaVersion <= 0
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || !capturedCoreSectionIds.Add(witness.SectionId)
                || !Array.Exists(CoreOwnedP12FSectionIdValues,
                    sectionId => string.Equals(sectionId, witness.SectionId, StringComparison.Ordinal)))
            {
                diagnostic = "A Core P12-F provider returned a witness outside the fixed selected owner inventory.";
                return false;
            }

            composed.Add(new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision));
        }

        if (capturedCoreSectionIds.Count != CoreOwnedP12FSectionIdValues.Length)
        {
            diagnostic = "The Core P12-F provider set is incomplete.";
            return false;
        }

        composed.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        // Preserve a structurally complete but stale host alias witness for
        // the existing exact token/image comparison. That later gate applies
        // IsCompleteSnapshot and reports the established stale-boundary
        // failure instead of reclassifying identity drift as missing coverage.
        if (!DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(composed))
        {
            diagnostic = "The Core and host census sections do not compose the complete selected Daily-v1 owner vector.";
            return false;
        }

        completeOwnerSections = Array.AsReadOnly(composed.ToArray());
        return true;
    }

    internal static bool TryRebindCoreOwnedP12FSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        ActorChoiceStore actorChoiceStore,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (graph?.ScheduledDirectives == null
            || graph.TravelParties == null
            || graph.Expeditions == null
            || graph.PersonStore == null
            || actorChoiceStore == null
            || !ReferenceEquals(actorChoiceStore.PersonStore, graph.PersonStore)
            || hostSections == null)
        {
            diagnostic = "The Core P12-F census requires the exact actor-choice, directive, travel-party, expedition, and PersonStore owners.";
            return false;
        }

        OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < hostSections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = hostSections[i];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !indexes.TryAdd(section.SectionId, i))
            {
                diagnostic = "The host census contains a missing or duplicate owner-section identity.";
                return false;
            }
            copied[i] = section;
        }

        OwnerSectionCensusWitness[] witnesses;
        try
        {
            witnesses = new OwnerSectionCensusWitness[]
            {
                new ActorChoiceP11CensusProvider(actorChoiceStore).GetCurrentCensus(),
                new ScheduledDirectiveCensusProvider(graph.ScheduledDirectives).GetCurrentCensus(),
                new TravelPartyCensusProvider(graph.TravelParties).GetCurrentCensus(),
                new ExpeditionCensusProvider(graph.Expeditions).GetCurrentCensus()
            };
        }
        catch (InvalidOperationException)
        {
            diagnostic = "A Core P12-F census provider could not read its exact owner.";
            return false;
        }

        HashSet<string> witnessSectionIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            if (witness == null
                || string.IsNullOrWhiteSpace(witness.SectionId)
                || witness.OwnerInstanceIdentity == null
                || witness.SchemaVersion <= 0
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || !witnessSectionIds.Add(witness.SectionId)
                || !indexes.TryGetValue(witness.SectionId, out int index))
            {
                diagnostic = witness == null
                    ? "A Core P12-F census provider returned no witness."
                    : "The host census is missing, duplicates, or cannot bind Core P12-F section "
                        + witness.SectionId + ".";
                return false;
            }

            OwnerSectionCensusSnapshot hostSection = copied[index];
            if (hostSection.Role != OwnerSectionRole.Required
                || hostSection.SchemaVersion != witness.SchemaVersion
                || !ReferenceEquals(hostSection.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                || hostSection.Cardinality != witness.Cardinality
                || hostSection.Revision != witness.Revision)
            {
                diagnostic = "The host census differs from the exact Core P12-F owner at "
                    + witness.SectionId + ".";
                return false;
            }

            copied[index] = new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision);
        }

        ownerSections = Array.AsReadOnly(copied);
        return true;
    }

    internal static bool TryRebindCoreOwnedGenealogySection(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (graph?.GenealogyStore == null || hostSections == null)
        {
            diagnostic = "The selected Core census requires its exact Genealogy owner and host census rows.";
            return false;
        }

        OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < hostSections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = hostSections[i];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !indexes.TryAdd(section.SectionId, i))
            {
                diagnostic = "The host census contains a missing or duplicate owner-section identity.";
                return false;
            }
            copied[i] = section;
        }

        if (!indexes.TryGetValue(GenealogyCensusProvider.SectionId, out int index))
        {
            diagnostic = "The host census is missing the fixed required Genealogy owner section.";
            return false;
        }

        OwnerSectionCensusWitness witness =
            new GenealogyCensusProvider(graph.GenealogyStore).GetCurrentCensus();
        OwnerSectionCensusSnapshot hostSection = copied[index];
        if (witness == null
            || !string.Equals(witness.SectionId, GenealogyCensusProvider.SectionId, StringComparison.Ordinal)
            || hostSection.Role != OwnerSectionRole.Required
            || hostSection.SchemaVersion != witness.SchemaVersion
            || !ReferenceEquals(hostSection.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
            || hostSection.Cardinality != witness.Cardinality
            || hostSection.Revision != witness.Revision)
        {
            diagnostic = "The host census differs from the exact selected Core Genealogy owner.";
            return false;
        }

        copied[index] = new OwnerSectionCensusSnapshot(
            witness.SectionId,
            witness.SchemaVersion,
            OwnerSectionRole.Required,
            witness.OwnerInstanceIdentity,
            witness.Cardinality,
            witness.Revision);
        ownerSections = Array.AsReadOnly(copied);
        diagnostic = null;
        return true;
    }

    internal static bool TryRebindCoreOwnedNpcSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (graph == null || hostSections == null)
        {
            diagnostic = "The selected Core census requires an owner graph and host census rows.";
            return false;
        }

        OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < hostSections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = hostSections[i];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !indexes.TryAdd(section.SectionId, i))
            {
                diagnostic = "The host census contains a missing or duplicate owner-section identity.";
                return false;
            }
            copied[i] = section;
        }

        int inventoryRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
        int moneyAccountRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
        if (inventoryRows != moneyAccountRows)
        {
            diagnostic = "The complete host census has inconsistent NPC Inventory and MoneyAccount family cardinality.";
            return false;
        }
        if (inventoryRows > graph.Npcs.Count)
        {
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try
        {
            List<IOwnerSectionCensusProvider> selected = new List<IOwnerSectionCensusProvider>();
            selected.AddRange(DailyV1NpcOwnerCensusProviders.CreateInventoryProviders(graph.Npcs));
            selected.AddRange(DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs));
            providers = selected.AsReadOnly();
        }
        catch (ArgumentException)
        {
            diagnostic = "The selected Core NPC census providers could not bind to the owner graph.";
            return false;
        }
        catch (InvalidOperationException)
        {
            diagnostic = "The selected Core NPC census providers could not bind to the owner graph.";
            return false;
        }

        HashSet<string> selectedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            OwnerSectionCensusWitness witness;
            try
            {
                witness = provider?.GetCurrentCensus();
            }
            catch (InvalidOperationException)
            {
                diagnostic = "A selected Core NPC census provider failed to read its owner.";
                return false;
            }

            if (witness == null || !selectedSectionIds.Add(witness.SectionId)
                || !indexes.TryGetValue(witness.SectionId, out int index))
            {
                diagnostic = witness == null
                    ? "A selected Core NPC census provider returned no witness."
                    : "The host census is missing or duplicates selected Core section " + witness.SectionId + ".";
                return false;
            }

            OwnerSectionCensusSnapshot hostSection = copied[index];
            if (hostSection.Role != OwnerSectionRole.Required
                || hostSection.SchemaVersion != witness.SchemaVersion
                || !ReferenceEquals(hostSection.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                || hostSection.Cardinality != witness.Cardinality
                || hostSection.Revision != witness.Revision)
            {
                diagnostic = "The host census differs from the exact selected Core owner at " + witness.SectionId + ".";
                return false;
            }

            copied[index] = new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision);
        }

        ownerSections = Array.AsReadOnly(copied);
        diagnostic = null;
        return true;
    }

    internal static bool TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (graph == null || hostSections == null)
        {
            diagnostic = "The selected Core NPC Knowledge and Plan census requires an owner graph and host census rows.";
            return false;
        }

        OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < hostSections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = hostSections[i];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !indexes.TryAdd(section.SectionId, i))
            {
                diagnostic = "The host census contains a missing or duplicate owner-section identity.";
                return false;
            }
            copied[i] = section;
        }

        int inventoryRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
        int moneyAccountRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
        if (inventoryRows != moneyAccountRows)
        {
            diagnostic = "The complete host census has inconsistent NPC Inventory and MoneyAccount family cardinality.";
            return false;
        }

        string[] requiredPrefixes =
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcExplorableSiteKnowledgePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcLocalTopologyPlacesPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcLocalTopologyConnectionsPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcAdventureOppositionPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcAdventureNotableItemsPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcAdventureCommonResourcesPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcAdventureAccessPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradePlanPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanPrefix
        };
        foreach (string prefix in requiredPrefixes)
        {
            if (CountFamilyRows(copied, prefix) != inventoryRows)
            {
                diagnostic = "The host census has incomplete selected NPC Knowledge and Plan family coverage for " + prefix;
                return false;
            }
        }

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try
        {
            providers = DailyV1NpcOwnerCensusProviders.CreateKnowledgeAndPlanProviders(graph);
        }
        catch (ArgumentException)
        {
            diagnostic = "The selected Core NPC Knowledge and Plan providers could not bind to the owner graph.";
            return false;
        }
        catch (InvalidOperationException)
        {
            diagnostic = "The selected Core NPC Knowledge and Plan providers could not bind to the owner graph.";
            return false;
        }

        OwnerSectionCensusWitness[] witnesses = new OwnerSectionCensusWitness[providers.Count];
        HashSet<string> selectedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < providers.Count; i++)
        {
            try
            {
                witnesses[i] = providers[i]?.GetCurrentCensus();
            }
            catch (InvalidOperationException)
            {
                diagnostic = "A selected Core NPC Knowledge or Plan provider failed to read its owner.";
                return false;
            }

            OwnerSectionCensusWitness witness = witnesses[i];
            if (witness == null
                || string.IsNullOrWhiteSpace(witness.SectionId)
                || witness.OwnerInstanceIdentity == null
                || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || !selectedSectionIds.Add(witness.SectionId))
            {
                diagnostic = "A selected Core NPC Knowledge or Plan provider returned an invalid census witness.";
                return false;
            }
        }

        if (inventoryRows < graph.Npcs.Count)
        {
            diagnostic = "The host census does not cover every NPC in the selected Core graph.";
            return false;
        }

        if (inventoryRows > graph.Npcs.Count)
        {
            for (int i = 0; i < witnesses.Length; i++)
            {
                if (!indexes.ContainsKey(witnesses[i].SectionId))
                {
                    diagnostic = "The mixed-roster host census is missing selected Core section "
                        + witnesses[i].SectionId + ".";
                    return false;
                }
            }

            // A mixed roster retains the complete host vector, including all
            // compatibility NPC rows, without replacing selected-row snapshots.
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        if (inventoryRows != graph.Npcs.Count || witnesses.Length != graph.Npcs.Count * 9)
        {
            diagnostic = "The host census does not contain all nine selected NPC Knowledge and Plan sections per Core owner.";
            return false;
        }

        int[] indexesByWitness = new int[witnesses.Length];
        bool matchesCoreOwners = true;
        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            if (!indexes.TryGetValue(witness.SectionId, out int index))
            {
                diagnostic = "The host census is missing selected Core Knowledge or Plan section "
                    + witness.SectionId + ".";
                return false;
            }

            indexesByWitness[i] = index;
            OwnerSectionCensusSnapshot hostSection = copied[index];
            matchesCoreOwners &= hostSection.Role == OwnerSectionRole.Required
                && hostSection.SchemaVersion == witness.SchemaVersion
                && ReferenceEquals(hostSection.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                && hostSection.Cardinality == witness.Cardinality
                && hostSection.Revision == witness.Revision;
        }

        if (!matchesCoreOwners)
        {
            // Retain the captured source facts so existing P12 token checks
            // classify stale owner state instead of silently replacing it.
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            copied[indexesByWitness[i]] = new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision);
        }

        ownerSections = Array.AsReadOnly(copied);
        return true;
    }
    internal static bool TryRebindCoreOwnedNpcRuntimeStateSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (graph == null || hostSections == null)
        {
            diagnostic = "The selected Core NPC runtime-state census requires an owner graph and host census rows.";
            return false;
        }
        if (graph.PersonStore == null)
        {
            ownerSections = hostSections;
            return true;
        }

        OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < hostSections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = hostSections[i];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !indexes.TryAdd(section.SectionId, i))
            {
                diagnostic = "The host census contains a missing or duplicate owner-section identity.";
                return false;
            }
            copied[i] = section;
        }

        int inventoryRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
        int moneyAccountRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
        if (inventoryRows != moneyAccountRows)
        {
            diagnostic = "The complete host census has inconsistent NPC Inventory and MoneyAccount family cardinality.";
            return false;
        }

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try
        {
            providers = DailyV1NpcOwnerCensusProviders.CreateRuntimeAuthorityProviders(graph);
        }
        catch (ArgumentException)
        {
            diagnostic = "The selected Core NPC runtime-state providers could not bind to the owner graph.";
            return false;
        }
        catch (InvalidOperationException)
        {
            diagnostic = "The selected Core NPC runtime-state providers could not bind to the owner graph.";
            return false;
        }

        OwnerSectionCensusWitness[] witnesses = new OwnerSectionCensusWitness[providers.Count];
        for (int i = 0; i < providers.Count; i++)
        {
            try
            {
                witnesses[i] = providers[i]?.GetCurrentCensus();
            }
            catch (InvalidOperationException)
            {
                diagnostic = "A selected Core NPC runtime-state provider failed to read its owner.";
                return false;
            }

            OwnerSectionCensusWitness witness = witnesses[i];
            if (witness == null
                || string.IsNullOrWhiteSpace(witness.SectionId)
                || witness.OwnerInstanceIdentity == null
                || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || witness.Cardinality < 0
                || witness.Revision < 0L)
            {
                diagnostic = "A selected Core NPC runtime-state provider returned an invalid census witness.";
                return false;
            }
        }

        bool mixedRoster = inventoryRows > graph.Npcs.Count;
        if (inventoryRows < graph.Npcs.Count)
        {
            diagnostic = "The host census does not cover every NPC in the selected Core graph.";
            return false;
        }

        string[] statePrefixes =
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcLifeStatePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcResidencePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix
        };
        foreach (string prefix in statePrefixes)
        {
            int expectedRows = 0;
            for (int i = 0; i < witnesses.Length; i++)
                if (witnesses[i].SectionId.StartsWith(prefix, StringComparison.Ordinal))
                    expectedRows++;

            int actualRows = CountFamilyRows(copied, prefix);
            bool residenceFamily = string.Equals(
                prefix, DailyV1SelectedOwnerSectionCensusContract.NpcResidencePrefix,
                StringComparison.Ordinal);
            bool invalidFamilyCardinality = mixedRoster
                ? (residenceFamily
                    ? actualRows < expectedRows || actualRows > inventoryRows
                    : actualRows != inventoryRows)
                : actualRows != expectedRows;
            if (invalidFamilyCardinality)
            {
                diagnostic = "The host census does not have the exact selected NPC runtime-state family cardinality for " + prefix;
                return false;
            }
        }

        int[] witnessIndexes = new int[witnesses.Length];
        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            if (!indexes.TryGetValue(witness.SectionId, out int index))
            {
                diagnostic = "The host census is missing selected Core NPC runtime-state section " + witness.SectionId + ".";
                return false;
            }
            witnessIndexes[i] = index;

            OwnerSectionCensusSnapshot hostSection = copied[index];
            if (hostSection.Role != OwnerSectionRole.Required
                || hostSection.SchemaVersion != witness.SchemaVersion
                || !ReferenceEquals(hostSection.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                || hostSection.Cardinality != witness.Cardinality
                || hostSection.Revision != witness.Revision)
            {
                diagnostic = "The host census differs from the exact selected Core NPC runtime state at " + witness.SectionId + ".";
                return false;
            }
        }

        if (mixedRoster)
        {
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            copied[witnessIndexes[i]] = new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision);
        }

        ownerSections = Array.AsReadOnly(copied);
        diagnostic = null;
        return true;
    }

    internal static bool TryRebindCoreOwnedCommercialKnowledgeSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (graph == null || hostSections == null)
        {
            diagnostic = "The selected Core CommercialKnowledge census requires an owner graph and host census rows.";
            return false;
        }

        OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < hostSections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = hostSections[i];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !indexes.TryAdd(section.SectionId, i))
            {
                diagnostic = "The host census contains a missing or duplicate owner-section identity.";
                return false;
            }
            copied[i] = section;
        }

        int inventoryRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
        int moneyAccountRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
        int marketRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix);
        int liquidityRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcCommercialLiquidityPrefix);
        int receiptRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix);
        if (inventoryRows != moneyAccountRows
            || inventoryRows != marketRows
            || inventoryRows != liquidityRows
            || inventoryRows != receiptRows)
        {
            diagnostic = "The complete host census has inconsistent NPC owner-family cardinality.";
            return false;
        }

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try
        {
            providers = DailyV1NpcOwnerCensusProviders.CreateCommercialKnowledgeProviders(graph.Npcs);
        }
        catch (ArgumentException)
        {
            diagnostic = "The selected Core CommercialKnowledge providers could not bind to the owner graph.";
            return false;
        }
        catch (InvalidOperationException)
        {
            diagnostic = "The selected Core CommercialKnowledge providers could not bind to the owner graph.";
            return false;
        }

        OwnerSectionCensusWitness[] witnesses = new OwnerSectionCensusWitness[providers.Count];
        HashSet<string> selectedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < providers.Count; i++)
        {
            try
            {
                witnesses[i] = providers[i]?.GetCurrentCensus();
            }
            catch (InvalidOperationException)
            {
                diagnostic = "A selected Core CommercialKnowledge provider failed to read its owner.";
                return false;
            }

            if (witnesses[i] == null
                || string.IsNullOrWhiteSpace(witnesses[i].SectionId)
                || witnesses[i].OwnerInstanceIdentity == null
                || witnesses[i].SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || witnesses[i].Cardinality < 0
                || witnesses[i].Revision < 0L
                || !selectedSectionIds.Add(witnesses[i].SectionId))
            {
                diagnostic = "A selected Core CommercialKnowledge provider returned an invalid census witness.";
                return false;
            }
        }

        if (inventoryRows > graph.Npcs.Count)
        {
            for (int i = 0; i < witnesses.Length; i++)
            {
                if (!indexes.ContainsKey(witnesses[i].SectionId))
                {
                    diagnostic = "The mixed-roster host census is missing selected Core CommercialKnowledge section "
                        + witnesses[i].SectionId + ".";
                    return false;
                }
            }

            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        if (inventoryRows != graph.Npcs.Count)
        {
            diagnostic = "The host census does not contain all three CommercialKnowledge sections for each Core NPC.";
            return false;
        }

        int[] indexesByWitness = new int[witnesses.Length];
        bool matchesCoreOwners = true;
        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            if (!indexes.TryGetValue(witness.SectionId, out int index))
            {
                diagnostic = "The host census is missing selected Core CommercialKnowledge section "
                    + witness.SectionId + ".";
                return false;
            }

            indexesByWitness[i] = index;
            OwnerSectionCensusSnapshot hostSection = copied[index];
            matchesCoreOwners &= hostSection.Role == OwnerSectionRole.Required
                && hostSection.SchemaVersion == witness.SchemaVersion
                && ReferenceEquals(hostSection.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                && hostSection.Cardinality == witness.Cardinality
                && hostSection.Revision == witness.Revision;
        }

        if (!matchesCoreOwners)
        {
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            copied[indexesByWitness[i]] = new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision);
        }

        ownerSections = Array.AsReadOnly(copied);
        return true;
    }

    internal static bool TryRebindCoreOwnedPersonStoreSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        PersonStore personStore = graph?.PersonStore;
        if (personStore == null || hostSections == null)
        {
            diagnostic = "The Core PersonStore census requires its bound owner graph and host census rows.";
            return false;
        }

        OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < hostSections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = hostSections[i];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !indexes.TryAdd(section.SectionId, i))
            {
                diagnostic = "The host census contains a missing or duplicate owner-section identity.";
                return false;
            }
            copied[i] = section;
        }

        int inventoryRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
        int moneyAccountRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
        if (inventoryRows != moneyAccountRows)
        {
            diagnostic = "The complete host census has inconsistent NPC Inventory and MoneyAccount family cardinality.";
            return false;
        }
        if (inventoryRows > graph.Npcs.Count)
        {
            if (!indexes.ContainsKey(DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId)
                || !indexes.ContainsKey(DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId))
            {
                diagnostic = "The mixed-roster host census is missing a fixed PersonStore alias section.";
                return false;
            }
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        OwnerSectionCensusWitness[] witnesses =
        {
            personStore.GetMembershipCensusWitness(),
            personStore.GetMaterializationBindingCensusWitness()
        };
        if (witnesses[0] == null || witnesses[1] == null
            || !ReferenceEquals(witnesses[0].OwnerInstanceIdentity, personStore)
            || !ReferenceEquals(witnesses[1].OwnerInstanceIdentity, personStore)
            || !ReferenceEquals(witnesses[0].OwnerInstanceIdentity, witnesses[1].OwnerInstanceIdentity)
            || witnesses[0].Revision != witnesses[1].Revision)
        {
            diagnostic = "The Core PersonStore witnesses do not preserve the required shared identity and revision.";
            return false;
        }

        int[] indexesByWitness = new int[witnesses.Length];
        bool matchesCoreOwner = true;
        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            if (!indexes.TryGetValue(witness.SectionId, out int index))
            {
                diagnostic = "The host census is missing selected Core PersonStore section " + witness.SectionId + ".";
                return false;
            }

            indexesByWitness[i] = index;
            OwnerSectionCensusSnapshot hostSection = copied[index];
            matchesCoreOwner &= hostSection.Role == OwnerSectionRole.Required
                && hostSection.SchemaVersion == witness.SchemaVersion
                && ReferenceEquals(hostSection.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                && hostSection.Cardinality == witness.Cardinality
                && hostSection.Revision == witness.Revision;
        }

        if (!matchesCoreOwner)
        {
            // Preserve the host's captured values so the established P12 source
            // witness can report a stale token instead of changing its failure
            // classification at this earlier integration seam.
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            copied[indexesByWitness[i]] = new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision);
        }

        ownerSections = Array.AsReadOnly(copied);
        diagnostic = null;
        return true;
    }

    internal static bool TryRebindCorePersonLifeResidenceSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        PersonStore personStore = graph?.PersonStore;
        if (personStore == null || hostSections == null)
        {
            diagnostic = "The Core Person life/residence census requires its bound owner graph and host census rows.";
            return false;
        }

        OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < hostSections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = hostSections[i];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !indexes.TryAdd(section.SectionId, i))
            {
                diagnostic = "The host census contains a missing or duplicate owner-section identity.";
                return false;
            }
            copied[i] = section;
        }

        List<PersonRuntime> ordered = new List<PersonRuntime>(personStore.Persons);
        HashSet<string> personIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<PersonRuntime> owners = new HashSet<PersonRuntime>();
        foreach (PersonRuntime person in ordered)
        {
            if (person == null || person.PersonId == null
                || string.IsNullOrWhiteSpace(person.PersonId.Value)
                || !personIds.Add(person.PersonId.Value)
                || !owners.Add(person))
            {
                diagnostic = "The Core PersonStore contains a missing or duplicate life/residence owner.";
                return false;
            }
        }
        ordered.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.PersonId.Value, right.PersonId.Value));

        int hostPersonRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.PersonLifeResidencePrefix);
        OwnerSectionCensusWitness[] witnesses = new OwnerSectionCensusWitness[ordered.Count];
        HashSet<string> selectedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < ordered.Count; i++)
        {
            PersonRuntime person = ordered[i];
            OwnerSectionCensusWitness witness = person.GetLifeResidenceCensusWitness();
            string expectedId = DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(
                person.PersonId);
            if (witness == null
                || !string.Equals(witness.SectionId, expectedId, StringComparison.Ordinal)
                || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || !ReferenceEquals(witness.OwnerInstanceIdentity, person)
                || witness.Cardinality != 1
                || witness.Revision < 0L
                || !selectedSectionIds.Add(witness.SectionId))
            {
                diagnostic = "A Core Person life/residence witness differs from its canonical identity or singleton owner contract.";
                return false;
            }
            witnesses[i] = witness;
        }

        if (hostPersonRows > ordered.Count)
        {
            foreach (OwnerSectionCensusWitness witness in witnesses)
            {
                if (!indexes.ContainsKey(witness.SectionId))
                {
                    diagnostic = "The mixed-roster host census is missing selected Core Person section "
                        + witness.SectionId + ".";
                    return false;
                }
            }

            // Keep the complete host vector when it contains Person owners
            // outside this Core graph. P12 validates those compatibility rows.
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        if (hostPersonRows != ordered.Count)
        {
            diagnostic = "The host census does not contain one life/residence section per Core Person.";
            return false;
        }

        int[] indexesByWitness = new int[witnesses.Length];
        bool matchesCoreOwners = true;
        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            if (!indexes.TryGetValue(witness.SectionId, out int index))
            {
                diagnostic = "The host census is missing selected Core Person section "
                    + witness.SectionId + ".";
                return false;
            }

            indexesByWitness[i] = index;
            OwnerSectionCensusSnapshot hostSection = copied[index];
            matchesCoreOwners &= hostSection.Role == OwnerSectionRole.Required
                && hostSection.SchemaVersion == witness.SchemaVersion
                && ReferenceEquals(hostSection.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                && hostSection.Cardinality == witness.Cardinality
                && hostSection.Revision == witness.Revision;
        }

        if (!matchesCoreOwners)
        {
            // Preserve captured facts so the established P12 source witness
            // reports an owner revision change as a stale token.
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            copied[indexesByWitness[i]] = new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision);
        }

        ownerSections = Array.AsReadOnly(copied);
        diagnostic = null;
        return true;
    }
    internal static bool TryRebindCoreSpatialKnowledgeSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (graph == null || hostSections == null)
        {
            diagnostic = "The Core SpatialKnowledge census requires its bound owner graph and host census rows.";
            return false;
        }

        OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < hostSections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = hostSections[i];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !indexes.TryAdd(section.SectionId, i))
            {
                diagnostic = "The host census contains a missing or duplicate owner-section identity.";
                return false;
            }
            copied[i] = section;
        }

        int inventoryRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
        int moneyAccountRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
        if (inventoryRows != moneyAccountRows)
        {
            diagnostic = "The complete host census has inconsistent NPC Inventory and MoneyAccount family cardinality.";
            return false;
        }

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try
        {
            providers = DailyV1NpcOwnerCensusProviders.CreateSpatialKnowledgeProviders(graph.Npcs);
        }
        catch (ArgumentException)
        {
            diagnostic = "The selected Core SpatialKnowledge census providers could not bind to the owner graph.";
            return false;
        }
        catch (InvalidOperationException)
        {
            diagnostic = "The selected Core SpatialKnowledge census providers could not bind to the owner graph.";
            return false;
        }

        if (inventoryRows > graph.Npcs.Count)
        {
            HashSet<string> selectedSectionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (IOwnerSectionCensusProvider provider in providers)
            {
                OwnerSectionCensusWitness witness;
                try
                {
                    witness = provider?.GetCurrentCensus();
                }
                catch (InvalidOperationException)
                {
                    diagnostic = "A selected Core SpatialKnowledge census provider failed to read its owner.";
                    return false;
                }

                if (witness == null || !selectedSectionIds.Add(witness.SectionId)
                    || !indexes.ContainsKey(witness.SectionId))
                {
                    diagnostic = witness == null
                        ? "A selected Core SpatialKnowledge census provider returned no witness."
                        : "The mixed-roster host census is missing selected Core section " + witness.SectionId + ".";
                    return false;
                }
            }

            // Preserve the complete host vector when it includes compatibility
            // NPCs outside the selected Core graph. P12 retains authority to
            // validate every row and its cross-family cardinality.
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        if (CountFamilyRows(copied,
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix) != graph.Npcs.Count
            || CountFamilyRows(copied,
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix) != graph.Npcs.Count)
        {
            diagnostic = "The host census does not contain one selected SpatialKnowledge locations/routes pair per Core NPC.";
            return false;
        }

        HashSet<string> selectedSectionIdsForCore = new HashSet<string>(StringComparer.Ordinal);
        List<OwnerSectionCensusWitness> witnesses =
            new List<OwnerSectionCensusWitness>(providers.Count);
        int[] indexesByWitness = new int[providers.Count];
        bool matchesCoreOwners = true;
        for (int i = 0; i < providers.Count; i++)
        {
            OwnerSectionCensusWitness witness;
            try
            {
                witness = providers[i]?.GetCurrentCensus();
            }
            catch (InvalidOperationException)
            {
                diagnostic = "A selected Core SpatialKnowledge census provider failed to read its owner.";
                return false;
            }

            if (witness == null || !selectedSectionIdsForCore.Add(witness.SectionId)
                || !indexes.TryGetValue(witness.SectionId, out int index))
            {
                diagnostic = witness == null
                    ? "A selected Core SpatialKnowledge census provider returned no witness."
                    : "The host census is missing or duplicates selected Core section " + witness.SectionId + ".";
                return false;
            }

            indexesByWitness[i] = index;
            witnesses.Add(witness);
            OwnerSectionCensusSnapshot hostSection = copied[index];
            matchesCoreOwners &= hostSection.Role == OwnerSectionRole.Required
                && hostSection.SchemaVersion == witness.SchemaVersion
                && ReferenceEquals(hostSection.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                && hostSection.Cardinality == witness.Cardinality
                && hostSection.Revision == witness.Revision;
        }

        if (!matchesCoreOwners)
        {
            // Retain the original facts so P12 classifies a changed captured
            // vector through its established stale-token check.
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        for (int i = 0; i < witnesses.Count; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            copied[indexesByWitness[i]] = new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision);
        }

        ownerSections = Array.AsReadOnly(copied);
        diagnostic = null;
        return true;
    }

    internal static bool TryRebindCoreOwnedCitySections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (graph == null || hostSections == null)
        {
            diagnostic = "The selected Core City census requires an owner graph and host census rows.";
            return false;
        }

        OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < hostSections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = hostSections[i];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !indexes.TryAdd(section.SectionId, i))
            {
                diagnostic = "The host census contains a missing or duplicate owner-section identity.";
                return false;
            }
            copied[i] = section;
        }

        int presenceRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix);
        int marketRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix);
        int populationRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix);
        int receiptRows = CountFamilyRows(
            copied, DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix);
        if (presenceRows != marketRows || presenceRows != populationRows || presenceRows != receiptRows)
        {
            diagnostic = "The complete host census has inconsistent City owner-family cardinality.";
            return false;
        }

        List<OwnerSectionCensusWitness> witnesses =
            new List<OwnerSectionCensusWitness>(graph.Cities.Count * 4);
        HashSet<string> cityRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> witnessSectionIds = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            {
                if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                    || !cityRuntimeIds.Add(city.RuntimeId)
                    || city.Definitions == null || !ReferenceEquals(city.Definitions, graph.Definitions)
                    || city.NpcPresence == null
                    || !string.Equals(city.NpcPresence.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
                    || city.NpcPresence.OrderedNpcRuntimeIds == null
                    || city.NpcPresence.Revision < 0L
                    || city.Market == null || !ReferenceEquals(city.Market.Definitions, graph.Definitions)
                    || city.Market.Items == null || city.Market.Revision < 0L
                    || city.Population == null
                    || !string.Equals(city.Population.SettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal)
                    || city.Population.Revision < 0L)
                {
                    diagnostic = "A selected Core City owner is missing, duplicated, or not bound to its canonical runtime identity.";
                    return false;
                }

                string cityKey = P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(city.RuntimeId);
                city.Population.GetOperationReceiptCensus(
                    out int receiptCardinality,
                    out long receiptRevision);
                OwnerSectionCensusWitness[] cityWitnesses =
                {
                    new OwnerSectionCensusWitness(
                        DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId),
                        DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                        city.NpcPresence,
                        city.NpcPresence.OrderedNpcRuntimeIds.Count,
                        city.NpcPresence.Revision),
                    new OwnerSectionCensusWitness(
                        DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix + cityKey,
                        DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                        city.Market,
                        city.Market.Items.Count,
                        city.Market.Revision),
                    new OwnerSectionCensusWitness(
                        DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix + cityKey,
                        DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                        city.Population,
                        1,
                        city.Population.Revision),
                    new OwnerSectionCensusWitness(
                        DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix + cityKey,
                        DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                        city.Population,
                        receiptCardinality,
                        receiptRevision)
                };

                foreach (OwnerSectionCensusWitness witness in cityWitnesses)
                {
                    if (witness == null || string.IsNullOrWhiteSpace(witness.SectionId)
                        || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion
                        || witness.OwnerInstanceIdentity == null
                        || witness.Cardinality < 0
                        || witness.Revision < 0L
                        || !witnessSectionIds.Add(witness.SectionId))
                    {
                        diagnostic = "A selected Core City census witness violates its fixed identity, schema, or cardinality contract.";
                        return false;
                    }
                    witnesses.Add(witness);
                }
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "A selected Core City census witness could not be read from its bound owner graph.";
            return false;
        }

        if (presenceRows > graph.Cities.Count)
        {
            foreach (OwnerSectionCensusWitness witness in witnesses)
            {
                if (!indexes.ContainsKey(witness.SectionId))
                {
                    diagnostic = "The mixed-roster host census is missing selected Core City section "
                        + witness.SectionId + ".";
                    return false;
                }
            }

            // Keep the complete host vector when it includes compatibility
            // Cities outside the selected Core graph. P12 validates all rows.
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        if (presenceRows != graph.Cities.Count)
        {
            diagnostic = "The host census does not contain all four City owner sections for each Core City.";
            return false;
        }

        int[] indexesByWitness = new int[witnesses.Count];
        bool matchesCoreOwners = true;
        for (int i = 0; i < witnesses.Count; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            if (!indexes.TryGetValue(witness.SectionId, out int index))
            {
                diagnostic = "The host census is missing selected Core City section " + witness.SectionId + ".";
                return false;
            }

            indexesByWitness[i] = index;
            OwnerSectionCensusSnapshot hostSection = copied[index];
            matchesCoreOwners &= hostSection.Role == OwnerSectionRole.Required
                && hostSection.SchemaVersion == witness.SchemaVersion
                && ReferenceEquals(hostSection.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                && hostSection.Cardinality == witness.Cardinality
                && hostSection.Revision == witness.Revision;
        }

        if (!matchesCoreOwners)
        {
            // Preserve captured facts so the established P12 source witness
            // classifies stale City owners without replacing them with new rows.
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        for (int i = 0; i < witnesses.Count; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            copied[indexesByWitness[i]] = new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision);
        }

        ownerSections = Array.AsReadOnly(copied);
        diagnostic = null;
        return true;
    }
    private static int CountFamilyRows(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionPrefix)
    {
        int count = 0;
        for (int i = 0; i < sections.Count; i++)
            if (sections[i].SectionId.StartsWith(sectionPrefix, StringComparison.Ordinal))
                count++;
        return count;
    }
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
            if (clock.AbsoluteDay == long.MaxValue)
            {
                if (!SimulationTimeOwnerBridge.TryAdvanceDayFromRuntime(
                        clock, out SimulationTimeAdvanceFailure limitFailure))
                {
                    failure = MapClockFailure(limitFailure);
                    return false;
                }

                failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                boundary.FaultRuntimeAdmission();
                return false;
            }

            DailyV1SelectedDailyExecutionStage.PreparedDay preparedDay =
                DailyV1SelectedDailyExecutionStage.PrepareForNextDay(this, graph, executionBoundary);
            if (!SimulationTimeOwnerBridge.TryAdvanceDayFromRuntime(
                    clock, out SimulationTimeAdvanceFailure timeFailure))
            {
                failure = MapClockFailure(timeFailure);
                return false;
            }

            DailyV1SelectedDailyExecutionStage.Advance(preparedDay);
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

    /// <summary>
    /// Finalizes an unpublished restored candidate under this exact Core
    /// execution state. The host supplies only the complete owner census and
    /// one-way guard-binding ports; Core owns revalidation, restored-token
    /// creation, and factual-read publication.
    /// </summary>
    internal bool TryFinalizeRestoredDailyBoundary(
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts,
        WorldId expectedWorldId,
        long preservedAbsoluteDay,
        long preservedCompletedCoreSequence,
        IReadOnlyList<OwnerSectionCensusSnapshot> validatedTargetOwnerSections,
        long validatedTargetMutationEpoch,
        Action<DailyV1RestoredBoundaryFinalizationStage> stageObserver,
        out IReadOnlyList<OwnerSectionCensusSnapshot> recapturedOwnerSections,
        out long recapturedMutationEpoch,
        out DailyV1RestoredBoundaryFinalizationFailure failure,
        out string diagnostic)
    {
        recapturedOwnerSections = null;
        recapturedMutationEpoch = 0L;
        failure = DailyV1RestoredBoundaryFinalizationFailure.None;
        diagnostic = null;

        if (boundary == null || !ReferenceEquals(boundary.ExecutionState, this)
            || hostPorts == null || !ReferenceEquals(hostPorts.ExecutionState, this))
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.CandidateHostBoundaryUnavailable;
            diagnostic = "The private restored candidate is not bound to this exact Core execution state.";
            return false;
        }

        if (AdmissionContext == null
            || !AdmissionContext.IsRestoredContinuation
            || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !AdmissionContext.IsOwnedByCurrentThread()
            || expectedWorldId == null
            || !ReferenceEquals(expectedWorldId, WorldId)
            || SimulationTime == null
            || SimulationTime.AbsoluteDay != preservedAbsoluteDay
            || preservedAbsoluteDay < 0L
            || preservedCompletedCoreSequence <= 0L
            || validatedTargetMutationEpoch < 0L
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(
                validatedTargetOwnerSections)
            || currentCaptureToken != null
            || completedCoreSequence != 0L
            || factualReadWorldPublished
            || hostPorts.IsFactualReadWorldPublished
            || AdvanceLeaseHeld
            || ActiveOperationCount != 0
            || hostPorts.HasActiveDailyOperationContext
            || !mutationGuard.CanMutate
            || !runtimeIdentityBound
            || !runtimeConfigurationBound
            || CoreMutationGuardAuthoritiesBound)
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.BoundaryAdmissionFailed;
            diagnostic = "The private restored candidate is not at its exact unadmitted Core owner-thread boundary.";
            return false;
        }

        if (!hostPorts.TryBindRestoredCandidateMutationGuard(out diagnostic)
            || !CoreMutationGuardAuthoritiesBound)
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.MutationGuardBindingFailed;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The private restored candidate mutation guard did not bind to its complete owner graph.";
            return false;
        }
        stageObserver?.Invoke(DailyV1RestoredBoundaryFinalizationStage.MutationGuardBound);

        if (!TryCaptureUnadmittedRestoredOwnerVector(
                hostPorts, out IReadOnlyList<OwnerSectionCensusSnapshot> firstRecapture,
                out long firstRecapturedEpoch, out diagnostic)
            || !DailyV1RestoredOwnerVectorContract.TryMatchCurrentTargetSnapshot(
                validatedTargetOwnerSections, validatedTargetMutationEpoch,
                firstRecapture, firstRecapturedEpoch, out _, out diagnostic))
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.OwnerVectorValidationFailed;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The detached target owner vector changed while binding its mutation guard.";
            return false;
        }

        recapturedOwnerSections = firstRecapture;
        recapturedMutationEpoch = firstRecapturedEpoch;
        stageObserver?.Invoke(DailyV1RestoredBoundaryFinalizationStage.OwnerVectorRecaptured);

        if (!TryCaptureUnadmittedRestoredOwnerVector(
                hostPorts, out IReadOnlyList<OwnerSectionCensusSnapshot> finalOwnerSections,
                out long finalMutationEpoch, out diagnostic)
            || !DailyV1RestoredOwnerVectorContract.TryMatchCurrentTargetSnapshot(
                validatedTargetOwnerSections, validatedTargetMutationEpoch,
                finalOwnerSections, finalMutationEpoch, out _, out diagnostic))
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.OwnerVectorValidationFailed;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The detached target owner vector changed before restored-boundary admission.";
            return false;
        }

        if (!TryCreateRestoredContinuationToken(
                AdmissionContext, ConfigurationIdentity, CalendarIdentity,
                CompositionProfile, expectedWorldId, preservedAbsoluteDay,
                preservedCompletedCoreSequence, finalMutationEpoch,
                validatedTargetOwnerSections, out _)
            || !TryMarkFactualReadWorldPublished())
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.BoundaryAdmissionFailed;
            diagnostic = "The Core execution state could not publish the restored continuation boundary.";
            return false;
        }

        if (!hostPorts.IsFactualReadWorldPublished
            || !IsHealthyDailyOwnerThreadBoundary()
            || !HasValidRestoredDailyBoundaryAdmission(boundary))
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.BoundaryAdmissionFailed;
            diagnostic = "The restored target did not retain a healthy admitted completed boundary.";
            return false;
        }

        stageObserver?.Invoke(DailyV1RestoredBoundaryFinalizationStage.BoundaryAdmitted);
        return true;
    }

    private bool TryCaptureUnadmittedRestoredOwnerVector(
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out string diagnostic)
    {
        ownerSections = null;
        mutationEpoch = 0L;
        diagnostic = null;
        if (!hostPorts.TryCaptureUnadmittedRestoredDailyOwnerVector(
                out ownerSections, out mutationEpoch, out string captureDiagnostic))
        {
            diagnostic = "The private target did not produce a complete quiescent owner vector: "
                + (string.IsNullOrWhiteSpace(captureDiagnostic) ? "OwnerCoverageIncomplete" : captureDiagnostic) + ".";
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryValidateExcludedNpcReceiptSections(
                selectedRuntimeOwnerGraph, ownerSections, out diagnostic))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The Core restored boundary could not validate its exact-empty NPC receipt rows.";
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPersonStoreSections(
                selectedRuntimeOwnerGraph,
                ownerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> personStoreComposedSections,
                out diagnostic))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The Core restored boundary could not compose its exact PersonStore alias rows.";
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
                selectedRuntimeOwnerGraph,
                personStoreComposedSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> npcKnowledgeAndPlanSections,
                out diagnostic))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The Core restored boundary could not bind its exact NPC Knowledge and Plan owners.";
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                selectedRuntimeOwnerGraph,
                selectedActorChoiceStore,
                npcKnowledgeAndPlanSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeOwnerSections,
                out diagnostic))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The Core restored boundary could not compose its exact P12-F owner rows.";
            return false;
        }

        ownerSections = completeOwnerSections;
        return true;
    }
}
