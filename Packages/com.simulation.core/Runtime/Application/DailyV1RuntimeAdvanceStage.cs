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
/// Host-owned ports for the selected Daily-v1 session. Census capture returns
/// the complete quiescent host vector and shared mutation epoch. Core selects
/// and validates source ownership against the admitted owner graph. During
/// restoration Core binds
/// its selected owner authorities before asking the host to bind runtime
/// projections. Core owns the owner-thread check and runtime-operation
/// quiescence through its operation registry. This intentionally excludes
/// clocks, whole-day advancement, and intraday timeline selection.
/// </summary>
internal interface IDailyV1SelectedRuntimeAdvanceHostPorts
{
    DailyV1ExecutionBoundaryState ExecutionState { get; }
    // Stable identity for the complete live/restored owner-census reader.
    object SelectedRuntimeOwnerCensusSourceIdentity { get; }
    bool TryGetExactJusticeRecordsCensusProjectionIdentity(
        DailyV1JusticeRecordsOwner owner,
        out object projectionIdentity,
        out long p18ReceiptRevision);
    bool TryCreateSelectedDailyExecutionHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDailyExecutionHostPorts hostPorts);
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
}

/// <summary>
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
    private DailyV1SelectedRuntimeOwnerGraph selectedDailyExecutionOwnerGraph;
    private IDailyV1SelectedDailyExecutionBoundary selectedDailyExecutionBoundary;

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
        if (!IsOwnerCensusProtocolReady())
        {
            executionState.MarkSelectedRuntimeCensusProtocolFaulted();
            return false;
        }

        if (!executionState.TryBeginSelectedRuntimeDailyAdvanceOperation())
        {
            executionState.MarkSelectedRuntimeCensusProtocolFaulted();
            return false;
        }

        operationScope = new RuntimeProtocolOperationScope(executionState);
        return true;
    }

    private bool IsOwnerCensusProtocolReady()
    {
        SimulationRuntimeAdmissionContext admissionContext = executionState.AdmissionContext;
        if (admissionContext == null || !admissionContext.IsOwnedByCurrentThread())
            return false;

        if (!executionState.TryReadSelectedRuntimeCensusOperationCount(out int censusOperationCount))
            return false;

        return censusOperationCount == executionState.ActiveRuntimeOperationCount;
    }

    private sealed class RuntimeProtocolOperationScope : IDisposable
    {
        private readonly DailyV1ExecutionBoundaryState executionState;
        private int disposed;

        internal RuntimeProtocolOperationScope(
            DailyV1ExecutionBoundaryState executionState)
        {
            this.executionState = executionState
                ?? throw new ArgumentNullException(nameof(executionState));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            if (executionState.TryEndRuntimeProtocolOperation()) return;

            executionState.InvalidateCurrentCaptureToken();
            executionState.MarkSelectedRuntimeCensusProtocolFaulted();
        }
    }

    public bool TryCreateSelectedDailyExecutionBoundary(
        out IDailyV1SelectedDailyExecutionBoundary executionBoundary)
    {
        DailyV1SelectedRuntimeOwnerGraph graph = executionState.SelectedRuntimeOwnerGraph;
        if (selectedDailyExecutionBoundary != null)
        {
            if (ReferenceEquals(selectedDailyExecutionOwnerGraph, graph)
                && selectedDailyExecutionBoundary is DailyV1CoreSelectedDailyExecutionBoundary coreBoundary
                && coreBoundary.IsBoundTo(executionState, graph))
            {
                executionBoundary = selectedDailyExecutionBoundary;
                return true;
            }

            // An admitted roster transition may replace the exact owner graph.
            // Never reuse an adapter from that prior graph; bind a fresh one.
            selectedDailyExecutionOwnerGraph = null;
            selectedDailyExecutionBoundary = null;
        }

        executionBoundary = null;
        if (!hostPorts.TryCreateSelectedDailyExecutionHostPorts(
                graph, out IDailyV1SelectedDailyExecutionHostPorts hostExecutionPorts)
            || hostExecutionPorts == null
            || !DailyV1CoreSelectedDailyExecutionBoundary.TryCreate(
                executionState, graph, hostExecutionPorts,
                out IDailyV1SelectedDailyExecutionBoundary createdBoundary))
            return false;

        selectedDailyExecutionOwnerGraph = graph;
        selectedDailyExecutionBoundary = createdBoundary;
        executionBoundary = createdBoundary;
        return true;
    }

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
        if (executionState.AdvanceLeaseHeld != dailyAdvanceScope)
        {
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        if (!executionState.IsFactualReadWorldPublished || executionState.WorldId == null)
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

        bool captured = executionState.TryCaptureBoundOwnerSectionCensus(
            hostPorts,
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
            || !executionState.TryAssessCaptureScope(allowedOuterScope))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        DailyV1SelectedRuntimeOwnerGraph graph = executionState.SelectedRuntimeOwnerGraph;
        DailyV1JusticeRecordsOwner justiceOwner = graph?.JusticeRecordsOwner;
        if (!hostPorts.TryGetExactJusticeRecordsCensusProjectionIdentity(
                justiceOwner, out object justiceProjectionIdentity, out long p18ReceiptRevision)
            || !DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedDailyOwnerVector(
                graph,
                executionState.SelectedActorChoiceStore,
                ownerSections,
                justiceProjectionIdentity,
                p18ReceiptRevision,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeOwnerSections,
                out _))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }
        ownerSections = completeOwnerSections;

        failure = DailyCaptureEligibilityFailure.None;
        return true;
    }


    public void FaultRuntimeAdmission() => executionState.MarkSelectedRuntimeCensusProtocolFaulted();
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

    private static readonly string[] CoreOwnedIdentitySpatialSectionIdValues =
    {
        P12CRuntimeIdentitySectionContract.NpcsSectionId,
        P12CRuntimeIdentitySectionContract.CitiesSectionId,
        P12CRuntimeIdentitySectionContract.LocationsSectionId,
        P12CRuntimeIdentitySectionContract.RoutesSectionId,
        P12CRuntimeIdentitySectionContract.ExplorableSitesSectionId,
        P12CRuntimeIdentitySectionContract.LocalPlacesSectionId,
        P12CRuntimeIdentitySectionContract.LocalConnectionsSectionId,
        P12CRuntimeIdentitySectionContract.NotableItemsSectionId,
        P12ASpatialNetworkOwnerCodec.LocationsSectionId,
        P12ASpatialNetworkOwnerCodec.RoutesSectionId,
        P12P8AImageSectionContract.HexSectionId,
        P12P8AImageSectionContract.LocationSectionId,
        P12P8AImageSectionContract.ScaleContextSectionId,
        DailyV1SelectedOwnerSectionCensusContract.SpatialPassageStateSectionId,
        DailyV1SelectedOwnerSectionCensusContract.SpatialCrossingSectionId,
        LegacySpatialAnchorBindingCensusProvider.SectionId,
        PersonSpatialPositionCensusProvider.SectionId
    };

    internal static IReadOnlyList<string> CoreOwnedIdentitySpatialSectionIds { get; } =
        Array.AsReadOnly(CoreOwnedIdentitySpatialSectionIdValues);

    private static readonly string[] CoreOwnedP12FSectionIdValues =
    {
        ActorChoiceP11CensusProvider.SectionId,
        ScheduledDirectiveCensusProvider.SectionId,
        TravelPartyCensusProvider.SectionId,
        ExpeditionCensusProvider.SectionId,
        PoliticalKnowledgeStoreCensusProvider.SectionId
    };

    private static readonly string[] CoreOwnedRuntimeIdAllocatorSectionIdValues =
    {
        RuntimeIdAllocatorCensusProvider.EventsSectionId,
        RuntimeIdAllocatorCensusProvider.DecisionsSectionId,
        RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId
    };

    private static readonly string[] CoreOwnedNpcKnowledgeAndPlanSectionPrefixValues =
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

    internal static IReadOnlyList<string> CoreOwnedRuntimeIdAllocatorSectionIds { get; } =
        Array.AsReadOnly(CoreOwnedRuntimeIdAllocatorSectionIdValues);

    /// <summary>
    /// Fixed selected-profile rows whose live source is the exact Core session
    /// graph. The Unity census protocol tracks quiescence and mutation epochs;
    /// Core validates these rows against its graph before composing the selected
    /// vector and never accepts host-provided owner identities for them.
    /// </summary>
    internal static IReadOnlyList<string> CoreOwnedP12FSectionIds { get; } =
        Array.AsReadOnly(CoreOwnedP12FSectionIdValues);

    internal static IReadOnlyList<string> CoreOwnedPersonStoreSectionIds { get; } =
        Array.AsReadOnly(CoreOwnedPersonStoreSectionIdValues);

    private static readonly string[] CoreOwnedSelectedRuntimeSectionIdValues =
    {
        P12CRuntimeIdentitySectionContract.NpcsSectionId,
        P12CRuntimeIdentitySectionContract.CitiesSectionId,
        P12CRuntimeIdentitySectionContract.LocationsSectionId,
        P12CRuntimeIdentitySectionContract.RoutesSectionId,
        P12CRuntimeIdentitySectionContract.ExplorableSitesSectionId,
        P12CRuntimeIdentitySectionContract.LocalPlacesSectionId,
        P12CRuntimeIdentitySectionContract.LocalConnectionsSectionId,
        P12CRuntimeIdentitySectionContract.NotableItemsSectionId,
        P12ASpatialNetworkOwnerCodec.LocationsSectionId,
        P12ASpatialNetworkOwnerCodec.RoutesSectionId,
        P12P8AImageSectionContract.HexSectionId,
        P12P8AImageSectionContract.LocationSectionId,
        P12P8AImageSectionContract.ScaleContextSectionId,
        DailyV1SelectedOwnerSectionCensusContract.SpatialPassageStateSectionId,
        DailyV1SelectedOwnerSectionCensusContract.SpatialCrossingSectionId,
        LegacySpatialAnchorBindingCensusProvider.SectionId,
        PersonSpatialPositionCensusProvider.SectionId,
        DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
        DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
        ActorChoiceP11CensusProvider.SectionId,
        ScheduledDirectiveCensusProvider.SectionId,
        TravelPartyCensusProvider.SectionId,
        ExpeditionCensusProvider.SectionId,
        PoliticalKnowledgeStoreCensusProvider.SectionId,
        PoliticalDecisionStoreCensusProvider.SectionId,
        RuntimeIdAllocatorCensusProvider.EventsSectionId,
        RuntimeIdAllocatorCensusProvider.DecisionsSectionId,
        RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId
    };

    internal static IReadOnlyList<string> CoreOwnedSelectedRuntimeSectionIds { get; } =
        Array.AsReadOnly(CoreOwnedSelectedRuntimeSectionIdValues);

    internal static bool TryCreateCoreOwnedRuntimeIdAllocatorCensusSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        out string diagnostic)
    {
        sections = null;
        diagnostic = null;
        RuntimeIdAllocator allocator = graph?.RuntimeIdAllocator;
        if (allocator == null)
        {
            diagnostic = "The Core runtime census requires the exact admitted RuntimeIdAllocator owner.";
            return false;
        }

        OwnerSectionCensusWitness[] witnesses;
        try
        {
            witnesses = new OwnerSectionCensusWitness[]
            {
                RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(allocator).GetCurrentCensus(),
                RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(allocator).GetCurrentCensus(),
                RuntimeIdAllocatorCensusProvider.CreateTravelPartyCounterProvider(allocator).GetCurrentCensus()
            };
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "The Core RuntimeIdAllocator provider could not read its exact owner.";
            return false;
        }

        if (witnesses.Length != CoreOwnedRuntimeIdAllocatorSectionIdValues.Length)
        {
            diagnostic = "The Core RuntimeIdAllocator provider set is incomplete.";
            return false;
        }

        HashSet<string> observedIds = new HashSet<string>(StringComparer.Ordinal);
        OwnerSectionCensusSnapshot[] built =
            new OwnerSectionCensusSnapshot[witnesses.Length];
        RuntimeIdAllocatorCensusCounter[] counters =
        {
            RuntimeIdAllocatorCensusCounter.Events,
            RuntimeIdAllocatorCensusCounter.Decisions,
            RuntimeIdAllocatorCensusCounter.TravelParties
        };
        for (int i = 0; i < witnesses.Length; i++)
        {
            OwnerSectionCensusWitness witness = witnesses[i];
            if (witness == null
                || !string.Equals(witness.SectionId,
                    CoreOwnedRuntimeIdAllocatorSectionIdValues[i], StringComparison.Ordinal)
                || witness.SchemaVersion != RuntimeIdAllocatorCensusProvider.SchemaVersion
                || witness.OwnerInstanceIdentity == null
                || (i > 0 && !ReferenceEquals(
                    witness.OwnerInstanceIdentity, witnesses[0].OwnerInstanceIdentity))
                || witness.Cardinality != 1
                || witness.Revision < 0L
                || !observedIds.Add(witness.SectionId)
                || !RuntimeIdAllocatorOwnerBridge.TryMatchCurrentCensus(
                    allocator, counters[i], witness.OwnerInstanceIdentity,
                    witness.Revision, out _))
            {
                diagnostic = "The Core RuntimeIdAllocator provider returned an invalid selected counter witness.";
                return false;
            }

            built[i] = new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision);
        }

        sections = Array.AsReadOnly(built);
        return true;
    }

    /// <summary>
    /// Replaces the selected event, decision, and travel-party allocator rows
    /// with witnesses read from the allocator bound to the Core owner graph.
    /// The Unity census remains the quiescence/epoch source. A complete host
    /// triplet must match exactly; a fully removed triplet is composed from
    /// Core; partial omission or stale/substituted facts fail closed.
    /// </summary>
    internal static bool TryComposeCoreOwnedRuntimeIdAllocatorSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> composedSections,
        out string diagnostic)
    {
        composedSections = null;
        diagnostic = null;
        if (hostSections == null
            || !TryCreateCoreOwnedRuntimeIdAllocatorCensusSections(
                graph, out IReadOnlyList<OwnerSectionCensusSnapshot> coreRows, out diagnostic))
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The Core allocator census requires its exact owner graph and host rows.";
            return false;
        }

        Dictionary<string, OwnerSectionCensusSnapshot> hostById =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in hostSections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !hostById.TryAdd(section.SectionId, section))
            {
                diagnostic = "The selected census contains a missing or duplicate section identity.";
                return false;
            }
        }

        int presentRows = 0;
        foreach (OwnerSectionCensusSnapshot expected in coreRows)
        {
            if (!hostById.TryGetValue(expected.SectionId,
                    out OwnerSectionCensusSnapshot actual))
                continue;

            presentRows++;
            if (!HasExactOwnerSectionFacts(actual, expected))
            {
                diagnostic = "The host census differs from the exact Core RuntimeIdAllocator owner at "
                    + expected.SectionId + ".";
                return false;
            }
        }

        if (presentRows != 0 && presentRows != coreRows.Count)
        {
            diagnostic = "The host census partially omitted the Core RuntimeIdAllocator counter set.";
            return false;
        }

        List<OwnerSectionCensusSnapshot> composed =
            new List<OwnerSectionCensusSnapshot>(hostSections.Count - presentRows + coreRows.Count);
        foreach (OwnerSectionCensusSnapshot section in hostSections)
            if (Array.IndexOf(CoreOwnedRuntimeIdAllocatorSectionIdValues, section.SectionId) < 0)
                composed.Add(section);
        composed.AddRange(coreRows);
        composed.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));

        composedSections = Array.AsReadOnly(composed.ToArray());
        return true;
    }

    internal static bool TryRemoveCoreOwnedSelectedRuntimeSections(
        IReadOnlyList<OwnerSectionCensusSnapshot> completeHostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedSections,
        out string diagnostic)
    {
        hostOwnedSections = null;
        diagnostic = null;
        if (completeHostSections != null)
        {
            foreach (OwnerSectionCensusSnapshot section in completeHostSections)
            {
                if (section != null
                    && string.Equals(section.SectionId,
                        PoliticalDecisionStoreCensusProvider.SectionId,
                        StringComparison.Ordinal))
                {
                    diagnostic = "Removing the Core PoliticalDecision census row requires its exact owner graph.";
                    return false;
                }
            }
        }

        return TryRemoveCoreOwnedSections(completeHostSections,
            CoreOwnedSelectedRuntimeSectionIdValues,
            "selected Core-runtime",
            out hostOwnedSections,
            out diagnostic);
    }

    /// <summary>
    /// Removes selected Core-owned P12-F and City census rows only after Core
    /// confirms the PoliticalKnowledge row and selected City identities,
    /// schemas, cardinalities, and revisions against the exact session graph.
    /// Unity still captures the full quiescent vector and shared epoch; stale
    /// or mixed-roster City rows remain in the vector.
    /// </summary>
    internal static bool TryRemoveCoreOwnedSelectedRuntimeSections(
        IReadOnlyList<OwnerSectionCensusSnapshot> completeHostSections,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedSections,
        out string diagnostic)
    {
        hostOwnedSections = null;
        diagnostic = null;
        if (graph == null || completeHostSections == null)
        {
            diagnostic = "The selected Core owner census requires a complete host vector and owner graph.";
            return false;
        }
        if (graph.PersonStore == null
            || graph.PoliticalDecisionStore == null
            || !graph.PoliticalDecisionStore.IsBoundToPersonStore(graph.PersonStore)
            || graph.PoliticalKnowledgeStore == null
            || !graph.PoliticalKnowledgeStore.IsBoundToPersonStore(graph.PersonStore))
        {
            diagnostic = "The selected Core census requires its exact PersonStore, PoliticalKnowledge, and PoliticalDecision owners.";
            return false;
        }
        if (!DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(completeHostSections))
        {
            diagnostic = "The host census is not a complete selected Daily-v1 owner vector.";
            return false;
        }
        if (!TryCreateCoreOwnedCityCensusWitnesses(
                graph, out IReadOnlyList<OwnerSectionCensusWitness> cityWitnesses, out diagnostic))
            return false;

        OwnerSectionCensusSnapshot[] copiedHostSections =
            new OwnerSectionCensusSnapshot[completeHostSections.Count];
        for (int i = 0; i < copiedHostSections.Length; i++)
            copiedHostSections[i] = completeHostSections[i];
        Dictionary<string, OwnerSectionCensusSnapshot> hostBySectionId =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in copiedHostSections)
            hostBySectionId.Add(section.SectionId, section);

        if (!TryCreateCoreOwnedPoliticalDecisionSection(
                graph,
                out OwnerSectionCensusSnapshot politicalDecisionSection,
                out diagnostic)
            || !hostBySectionId.TryGetValue(
                PoliticalDecisionStoreCensusProvider.SectionId,
                out OwnerSectionCensusSnapshot hostPoliticalDecisionSection)
            || !HasExactOwnerSectionFacts(hostPoliticalDecisionSection, politicalDecisionSection))
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The host census differs from the exact Core PoliticalDecision owner.";
            return false;
        }

        if (!TryCreateCoreOwnedRuntimeIdAllocatorCensusSections(
                graph, out IReadOnlyList<OwnerSectionCensusSnapshot> allocatorRows, out diagnostic))
            return false;
        foreach (OwnerSectionCensusSnapshot expected in allocatorRows)
        {
            if (!hostBySectionId.TryGetValue(expected.SectionId,
                    out OwnerSectionCensusSnapshot actual)
                || !HasExactOwnerSectionFacts(actual, expected))
            {
                diagnostic = "The host census differs from the exact Core RuntimeIdAllocator owner at "
                    + expected.SectionId + ".";
                return false;
            }
        }

        if (!TryCreateCoreOwnedIdentitySpatialCensusSections(
                graph,
                out IReadOnlyList<OwnerSectionCensusSnapshot> identitySpatialSections,
                out diagnostic))
            return false;
        HashSet<string> activeIdentitySpatialIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot expected in identitySpatialSections)
        {
            if (!activeIdentitySpatialIds.Add(expected.SectionId))
            {
                diagnostic = "The selected Core identity or spatial census contains a duplicate section identity.";
                return false;
            }
            if (!hostBySectionId.TryGetValue(expected.SectionId, out OwnerSectionCensusSnapshot actual)
                || !HasExactOwnerSectionFacts(actual, expected))
            {
                diagnostic = "The host census differs from the exact Core identity or spatial owner at "
                    + expected.SectionId + ".";
                return false;
            }
        }

        if (!TryGetExactCoreOwnedNpcKnowledgeAndPlanSectionIds(
                copiedHostSections,
                graph,
                out IReadOnlyList<string> exactNpcKnowledgeAndPlanSectionIds,
                out diagnostic))
            return false;

        List<string> coreSectionIds = new List<string>(CoreOwnedSelectedRuntimeSectionIdValues.Length);
        HashSet<string> uniqueSectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in CoreOwnedSelectedRuntimeSectionIdValues)
        {
            bool isIdentitySpatial = Array.IndexOf(CoreOwnedIdentitySpatialSectionIdValues, sectionId) >= 0;
            if (!isIdentitySpatial || activeIdentitySpatialIds.Contains(sectionId))
            {
                coreSectionIds.Add(sectionId);
                uniqueSectionIds.Add(sectionId);
            }
        }
        foreach (OwnerSectionCensusWitness witness in cityWitnesses)
        {
            if (witness == null || string.IsNullOrWhiteSpace(witness.SectionId)
                || !uniqueSectionIds.Add(witness.SectionId))
            {
                diagnostic = "The selected Core City census contains a duplicate section identity.";
                return false;
            }
        }
        foreach (string sectionId in exactNpcKnowledgeAndPlanSectionIds)
        {
            if (string.IsNullOrWhiteSpace(sectionId) || !uniqueSectionIds.Add(sectionId))
            {
                diagnostic = "The selected Core NPC Knowledge and Plan census contains a duplicate section identity.";
                return false;
            }
            coreSectionIds.Add(sectionId);
        }

        OwnerSectionCensusWitness politicalKnowledgeWitness;
        try
        {
            politicalKnowledgeWitness =
                new PoliticalKnowledgeStoreCensusProvider(graph.PoliticalKnowledgeStore).GetCurrentCensus();
        }
        catch (InvalidOperationException)
        {
            diagnostic = "The selected Core PoliticalKnowledge census provider could not read its exact owner.";
            return false;
        }
        if (politicalKnowledgeWitness == null
            || !hostBySectionId.TryGetValue(
                PoliticalKnowledgeStoreCensusProvider.SectionId,
                out OwnerSectionCensusSnapshot hostPoliticalKnowledge)
            || hostPoliticalKnowledge.Role != OwnerSectionRole.Required
            || hostPoliticalKnowledge.SchemaVersion != politicalKnowledgeWitness.SchemaVersion
            || !ReferenceEquals(
                hostPoliticalKnowledge.OwnerInstanceIdentity,
                politicalKnowledgeWitness.OwnerInstanceIdentity)
            || hostPoliticalKnowledge.Cardinality != politicalKnowledgeWitness.Cardinality
            || hostPoliticalKnowledge.Revision != politicalKnowledgeWitness.Revision)
        {
            diagnostic = "The host census differs from the exact Core PoliticalKnowledge owner.";
            return false;
        }

        int cityPresenceRows = CountFamilyRows(
            copiedHostSections, DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix);
        int cityMarketRows = CountFamilyRows(
            copiedHostSections, DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix);
        int cityPopulationRows = CountFamilyRows(
            copiedHostSections, DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix);
        int cityReceiptRows = CountFamilyRows(
            copiedHostSections, DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix);
        if (cityPresenceRows != cityMarketRows
            || cityPresenceRows != cityPopulationRows
            || cityPresenceRows != cityReceiptRows
            || cityPresenceRows < graph.Cities.Count)
        {
            diagnostic = "The host census does not cover the exact selected Core City owner families.";
            return false;
        }

        bool hasEverySelectedCitySection = true;
        bool exactSelectedCityWitnesses = cityPresenceRows == graph.Cities.Count;
        foreach (OwnerSectionCensusWitness witness in cityWitnesses)
        {
            if (!hostBySectionId.TryGetValue(witness.SectionId, out OwnerSectionCensusSnapshot hostSection))
            {
                hasEverySelectedCitySection = false;
                break;
            }

            exactSelectedCityWitnesses &= hostSection.Role == OwnerSectionRole.Required
                && hostSection.SchemaVersion == witness.SchemaVersion
                && ReferenceEquals(hostSection.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                && hostSection.Cardinality == witness.Cardinality
                && hostSection.Revision == witness.Revision;
        }

        if (!hasEverySelectedCitySection)
        {
            diagnostic = "The host census is missing a selected Core City owner section.";
            return false;
        }

        // Preserve mixed-roster rows and stale host facts. The later Core
        // composition gate retains the established fallback and lets completed
        // boundary admission classify stale data against its token.
        if (exactSelectedCityWitnesses)
            foreach (OwnerSectionCensusWitness witness in cityWitnesses)
                coreSectionIds.Add(witness.SectionId);

        return TryRemoveCoreOwnedSections(completeHostSections,
            coreSectionIds.AsReadOnly(),
            "selected Core identity/spatial/runtime and City",
            out hostOwnedSections,
            out diagnostic);
    }

    /// <summary>
    /// Reconstructs one complete selected Daily-v1 owner vector from the
    /// captured host vector and the exact Core owner graph. Both live capture
    /// and detached restored-target admission use this path so the Core-owned
    /// row projection, alias binding, and host remainder checks cannot drift.
    /// The host still supplies its actual census and epoch; this method never
    /// fabricates a host owner or revision.
    /// </summary>
    internal static bool TryComposeCoreOwnedDailyOwnerVector(
        DailyV1SelectedRuntimeOwnerGraph graph,
        ActorChoiceStore actorChoiceStore,
        IReadOnlyList<OwnerSectionCensusSnapshot> capturedOwnerSections,
        object justiceProjectionIdentity,
        long p18ReceiptRevision,
        out IReadOnlyList<OwnerSectionCensusSnapshot> completeOwnerSections,
        out string diagnostic)
    {
        completeOwnerSections = null;
        diagnostic = null;
        if (graph == null || actorChoiceStore == null || capturedOwnerSections == null)
        {
            diagnostic = "The complete Core owner-vector composition requires the exact graph, actor-choice store, and captured census.";
            return false;
        }

        if (!TryValidateCoreOwnedJusticeSections(
                graph, capturedOwnerSections, justiceProjectionIdentity,
                p18ReceiptRevision, out diagnostic)
            || !TryAddCoreOwnedCitySections(
                graph, capturedOwnerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> cityRowsBeforeP12F,
                out diagnostic)
            || !TryComposeCoreOwnedPoliticalDecisionSection(
                graph, cityRowsBeforeP12F,
                out IReadOnlyList<OwnerSectionCensusSnapshot> politicalDecisionRowsBeforeP12F,
                out diagnostic)
            || !TryComposeCoreOwnedP12FSections(
                graph, actorChoiceStore, politicalDecisionRowsBeforeP12F,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeCoreP12FSections,
                out diagnostic)
            || !TryRemoveCoreOwnedSelectedRuntimeSections(
                completeCoreP12FSections, graph,
                out IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedSections,
                out diagnostic)
            || !TryValidateExcludedNpcReceiptSections(
                graph, hostOwnedSections, out diagnostic)
            || !TryRebindCoreOwnedGenealogySection(
                graph, hostOwnedSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> genealogySections,
                out diagnostic)
            || !TryComposeCoreOwnedPoliticalDecisionSection(
                graph, genealogySections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> politicalDecisionSections,
                out diagnostic)
            || !TryComposeCoreOwnedPersonStoreSections(
                graph, politicalDecisionSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> personStoreSections,
                out diagnostic)
            || !TryComposeCoreOwnedIdentitySpatialSections(
                graph, personStoreSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> identitySpatialSections,
                out diagnostic)
            || !TryAddCoreOwnedCitySections(
                graph, identitySpatialSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> citySections,
                out diagnostic)
            || !TryComposeCoreOwnedP12FSections(
                graph, actorChoiceStore, citySections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> cityAndP12FSections,
                out diagnostic)
            || !TryRebindCoreOwnedNpcSections(
                graph, cityAndP12FSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> npcSections,
                out diagnostic))
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The Core owner graph could not reproduce its exact selected census rows.";
            return false;
        }

        IReadOnlyList<OwnerSectionCensusSnapshot> composed = npcSections;
        if (graph.PersonStore != null)
        {
            if (!TryRebindCoreOwnedNpcRuntimeStateSections(
                    graph, composed,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> runtimeStateSections,
                    out diagnostic))
                return false;
            composed = runtimeStateSections;
        }

        if (!TryRebindCoreOwnedCommercialKnowledgeSections(
                graph, composed,
                out IReadOnlyList<OwnerSectionCensusSnapshot> commercialKnowledgeSections,
                out diagnostic)
            || !TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
                graph, commercialKnowledgeSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> npcKnowledgeAndPlanSections,
                out diagnostic))
            return false;
        composed = npcKnowledgeAndPlanSections;

        if (graph.PersonStore != null)
        {
            if (!TryRebindCoreOwnedPersonStoreSections(
                    graph, composed,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> boundPersonSections,
                    out diagnostic)
                || !TryRebindCorePersonLifeResidenceSections(
                    graph, boundPersonSections,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> personLifeResidenceSections,
                    out diagnostic))
                return false;
            composed = personLifeResidenceSections;
        }

        if (!TryRebindCoreSpatialKnowledgeSections(
                graph, composed,
                out IReadOnlyList<OwnerSectionCensusSnapshot> spatialKnowledgeSections,
                out diagnostic)
            || !TryComposeCoreOwnedCitySections(
                graph, spatialKnowledgeSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeCitySections,
                out diagnostic)
            || !DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
                completeCitySections))
        {
            completeOwnerSections = null;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The composed selected Daily-v1 owner vector is incomplete.";
            return false;
        }

        completeOwnerSections = completeCitySections;
        return true;
    }

    internal static bool TryComposeCoreOwnedIdentitySpatialSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> composedSections,
        out string diagnostic)
    {
        composedSections = null;
        diagnostic = null;
        if (hostSections == null)
        {
            diagnostic = "The Core identity and spatial census requires a host vector.";
            return false;
        }

        HashSet<string> hostSectionIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, OwnerSectionCensusSnapshot> hostBySectionId =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in hostSections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !hostSectionIds.Add(section.SectionId))
            {
                diagnostic = "The host census contains a missing or duplicate owner section.";
                return false;
            }
            hostBySectionId.Add(section.SectionId, section);
        }

        if (!TryCreateCoreOwnedIdentitySpatialCensusSections(
                graph,
                out IReadOnlyList<OwnerSectionCensusSnapshot> coreSections,
                out diagnostic))
            return false;

        HashSet<string> activeSectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in coreSections)
            activeSectionIds.Add(section.SectionId);
        int presentActiveCount = 0;
        foreach (string sectionId in activeSectionIds)
            if (hostSectionIds.Contains(sectionId)) presentActiveCount++;

        foreach (string sectionId in CoreOwnedIdentitySpatialSectionIdValues)
        {
            if (!activeSectionIds.Contains(sectionId) && !hostSectionIds.Contains(sectionId))
            {
                diagnostic = "The host census omits a fixed identity or spatial row whose source remains host-owned.";
                return false;
            }
        }

        if (presentActiveCount == activeSectionIds.Count)
        {
            // A compatibility host retains the complete source vector. Its
            // active Core rows must still match the exact current Core owners.
            foreach (OwnerSectionCensusSnapshot expected in coreSections)
            {
                if (!hostBySectionId.TryGetValue(expected.SectionId,
                        out OwnerSectionCensusSnapshot actual)
                    || !HasExactOwnerSectionFacts(actual, expected))
                {
                    diagnostic = "The host census differs from the exact Core identity or spatial owner at "
                        + expected.SectionId + ".";
                    return false;
                }
            }

            OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
            for (int i = 0; i < copied.Length; i++) copied[i] = hostSections[i];
            composedSections = Array.AsReadOnly(copied);
            return true;
        }
        if (presentActiveCount != 0)
        {
            diagnostic = "The host census partially omitted rows from the Core-owned identity or spatial source set.";
            return false;
        }

        List<OwnerSectionCensusSnapshot> composed =
            new List<OwnerSectionCensusSnapshot>(hostSections.Count + coreSections.Count);
        composed.AddRange(hostSections);
        foreach (OwnerSectionCensusSnapshot section in coreSections)
            composed.Add(section);
        composed.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        composedSections = Array.AsReadOnly(composed.ToArray());
        return true;
    }

    internal static bool TryCreateCoreOwnedIdentitySpatialCensusSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        out string diagnostic)
    {
        sections = null;
        diagnostic = null;
        PersonStore persons = graph?.PersonStore;
        DailyV1RuntimeIdentityIndex identities = graph?.RuntimeIdentityIndex;
        SpatialNetworkOwner spatialNetwork = graph?.SpatialNetwork;
        PersonSpatialPositionStore positions = graph?.PersonSpatialPositionStore;
        LegacySpatialAnchorBindingStore anchors = graph?.LegacySpatialAnchors;
        SpatialAuthorityStore spatial = positions?.SpatialAuthorityStore;
        if (persons == null)
        {
            diagnostic = "The Core identity and spatial census requires its exact PersonStore.";
            return false;
        }

        List<OwnerSectionCensusSnapshot> built =
            new List<OwnerSectionCensusSnapshot>(CoreOwnedIdentitySpatialSectionIdValues.Length);
        int expectedCoreRowCount = (identities == null ? 0 : 8)
            + (spatialNetwork == null ? 0 : 2)
            + (positions == null ? 0 : 6)
            + (anchors == null || positions == null ? 0 : 1);
        try
        {
            DailyV1RuntimeIdentityKind[] identityKinds =
            {
                DailyV1RuntimeIdentityKind.Npc,
                DailyV1RuntimeIdentityKind.City,
                DailyV1RuntimeIdentityKind.Location,
                DailyV1RuntimeIdentityKind.Route,
                DailyV1RuntimeIdentityKind.ExplorableSite,
                DailyV1RuntimeIdentityKind.LocalPlace,
                DailyV1RuntimeIdentityKind.LocalConnection,
                DailyV1RuntimeIdentityKind.NotableItem
            };
            for (int i = 0; identities != null && i < identityKinds.Length; i++)
            {
                OwnerSectionCensusWitness witness =
                    identities.GetOwnerSectionCensusWitness(identityKinds[i]);
                built.Add(ToOwnerSectionSnapshot(witness,
                    i < 4 ? OwnerSectionRole.Required : OwnerSectionRole.ExplicitlyEmpty));
            }

            if (spatialNetwork != null)
            {
                built.Add(new OwnerSectionCensusSnapshot(
                    P12ASpatialNetworkOwnerCodec.LocationsSectionId,
                    P12ASpatialNetworkOwnerCodec.SchemaVersion,
                    OwnerSectionRole.Required,
                    spatialNetwork,
                    spatialNetwork.LocationCount,
                    spatialNetwork.Revision));
                built.Add(new OwnerSectionCensusSnapshot(
                    P12ASpatialNetworkOwnerCodec.RoutesSectionId,
                    P12ASpatialNetworkOwnerCodec.SchemaVersion,
                    OwnerSectionRole.Required,
                    spatialNetwork,
                    spatialNetwork.RouteCount,
                    spatialNetwork.Revision));
            }

            if (positions != null)
            {
                if (spatial == null || !ReferenceEquals(positions.PersonStore, persons)
                    || spatial.PassageAuthority == null)
                {
                    diagnostic = "The Core position census is not bound to its exact person and spatial owners.";
                    return false;
                }
                built.Add(new OwnerSectionCensusSnapshot(
                    P12P8AImageSectionContract.HexSectionId,
                    P12P8AImageSectionContract.SchemaVersion,
                    OwnerSectionRole.Required,
                    spatial,
                    spatial.HexCount,
                    spatial.Revision));
                built.Add(new OwnerSectionCensusSnapshot(
                    P12P8AImageSectionContract.LocationSectionId,
                    P12P8AImageSectionContract.SchemaVersion,
                    OwnerSectionRole.Required,
                    spatial,
                    spatial.LocationCount,
                    spatial.Revision));
                built.Add(new OwnerSectionCensusSnapshot(
                    P12P8AImageSectionContract.ScaleContextSectionId,
                    P12P8AImageSectionContract.SchemaVersion,
                    OwnerSectionRole.Required,
                    spatial,
                    spatial.HasGeography ? 1 : 0,
                    spatial.Revision));

                SpatialPassageAuthority passage = spatial.PassageAuthority;
                IReadOnlyList<PassageOptionRecord> passageOptions = passage.Options;
                IReadOnlyList<BarrierRecord> passageBarriers = passage.Barriers;
                int passageCardinality;
                checked { passageCardinality = passageOptions.Count + passageBarriers.Count; }
                built.Add(new OwnerSectionCensusSnapshot(
                    DailyV1SelectedOwnerSectionCensusContract.SpatialPassageStateSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.SpatialPassageStateSchemaVersion,
                    OwnerSectionRole.ExplicitlyEmpty,
                    passage,
                    passageCardinality,
                    spatial.Revision));
                built.Add(new OwnerSectionCensusSnapshot(
                    DailyV1SelectedOwnerSectionCensusContract.SpatialCrossingSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.SpatialCrossingSchemaVersion,
                    OwnerSectionRole.ExplicitlyEmpty,
                    spatial,
                    spatial.CrossingCount,
                    spatial.Revision));

                built.Add(ToOwnerSectionSnapshot(
                    new PersonSpatialPositionCensusProvider(positions).GetCurrentCensus(),
                    OwnerSectionRole.ExplicitlyEmpty));
            }

            if (anchors != null && positions != null && spatial != null)
            {
                if (!ReferenceEquals(anchors.SpatialLocationLookup, spatial))
                {
                    diagnostic = "The Core legacy-anchor census is not bound to its exact spatial owner.";
                    return false;
                }
                built.Add(ToOwnerSectionSnapshot(
                    new LegacySpatialAnchorBindingCensusProvider(anchors).GetCurrentCensus(),
                    OwnerSectionRole.ExplicitlyEmpty));
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "The Core identity or spatial provider could not read its exact owner.";
            return false;
        }

        HashSet<string> capturedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in built)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || section.OwnerInstanceIdentity == null
                || section.SchemaVersion <= 0 || section.Cardinality < 0 || section.Revision < 0L
                || !capturedIds.Add(section.SectionId)
                || Array.IndexOf(CoreOwnedIdentitySpatialSectionIdValues, section.SectionId) < 0)
            {
                diagnostic = "The Core identity or spatial providers returned an invalid fixed owner row.";
                return false;
            }
            if (DailyV1SelectedOwnerSectionCensusContract.IsFixedEmptySection(section.SectionId)
                && (section.Role != OwnerSectionRole.ExplicitlyEmpty || section.Cardinality != 0))
            {
                diagnostic = "The Core identity or spatial owner is nonempty where the selected Daily-v1 profile requires an exact-empty row at "
                    + section.SectionId + ".";
                return false;
            }
        }
        if (capturedIds.Count != expectedCoreRowCount)
        {
            diagnostic = "The Core identity or spatial owner row set is incomplete.";
            return false;
        }

        sections = Array.AsReadOnly(built.ToArray());
        return true;
    }

    private static OwnerSectionCensusSnapshot ToOwnerSectionSnapshot(
        OwnerSectionCensusWitness witness,
        OwnerSectionRole role)
    {
        if (witness == null)
            throw new InvalidOperationException("A Core census provider returned no owner witness.");
        return new OwnerSectionCensusSnapshot(
            witness.SectionId,
            witness.SchemaVersion,
            role,
            witness.OwnerInstanceIdentity,
            witness.Cardinality,
            witness.Revision);
    }

    private static bool HasExactOwnerSectionFacts(
        OwnerSectionCensusSnapshot actual,
        OwnerSectionCensusSnapshot expected) =>
        actual != null
        && expected != null
        && string.Equals(actual.SectionId, expected.SectionId, StringComparison.Ordinal)
        && actual.SchemaVersion == expected.SchemaVersion
        && actual.Role == expected.Role
        && ReferenceEquals(actual.OwnerInstanceIdentity, expected.OwnerInstanceIdentity)
        && actual.Cardinality == expected.Cardinality
        && actual.Revision == expected.Revision;

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
    /// Creates the two shared-identity Justice census rows for a host projection.
    /// Unity may pass its exact JusticeSystem projection identity; a detached host
    /// may pass its own opaque identity. In both cases Core derives record facts
    /// from the exact selected owner and the fixed P18 receipt revision remains a
    /// host-supplied witness. The identity is ephemeral and is not serialized.
    /// </summary>
    internal static bool TryCreateCoreOwnedJusticeSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        object projectionIdentity,
        long expectedP18ReceiptRevision,
        out IReadOnlyList<OwnerSectionCensusWitness> witnesses,
        out string diagnostic)
    {
        witnesses = null;
        diagnostic = null;
        DailyV1JusticeRecordsOwner owner = graph?.JusticeRecordsOwner;
        if (owner == null || projectionIdentity == null || expectedP18ReceiptRevision != 0L)
        {
            diagnostic = "The exact selected Justice owner, host census identity, and exact-zero P18 receipt witness are required.";
            return false;
        }

        try
        {
            IReadOnlyList<DailyV1JusticeWantedRecordOwner> wanted = owner.WantedRecords;
            IReadOnlyList<DailyV1JusticeSentenceOwner> sentences = owner.Sentences;
            long revision = owner.Revision;
            if (wanted == null || sentences == null || revision < 0L)
            {
                diagnostic = "The selected Core Justice owner returned invalid census facts.";
                return false;
            }

            int recordCardinality = checked(wanted.Count + sentences.Count);
            witnesses = new OwnerSectionCensusWitness[]
            {
                new OwnerSectionCensusWitness(
                    DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                    projectionIdentity,
                    recordCardinality,
                    revision),
                new OwnerSectionCensusWitness(
                    DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                    projectionIdentity,
                    1,
                    expectedP18ReceiptRevision)
            };
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            witnesses = null;
            diagnostic = "The selected Core Justice owner could not provide a valid census witness.";
            return false;
        }
    }

    /// <summary>
    /// Checks the host-projected Justice census against the exact selected Core owner.
    /// The host supplies one shared projection identity and its receipt revision;
    /// Core derives record cardinality and revision from the authoritative owner.
    /// </summary>
    internal static bool TryValidateCoreOwnedJusticeSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        object projectionIdentity,
        long expectedP18ReceiptRevision,
        out string diagnostic)
    {
        diagnostic = null;
        if (graph?.JusticeRecordsOwner == null || sections == null || projectionIdentity == null
            || expectedP18ReceiptRevision != 0L)
        {
            diagnostic = "The exact selected Justice owner, census projection, and exact-zero P18 receipt witness are required.";
            return false;
        }

        if (!TryCreateCoreOwnedJusticeSections(
                graph, projectionIdentity, expectedP18ReceiptRevision,
                out IReadOnlyList<OwnerSectionCensusWitness> expectedWitnesses,
                out diagnostic)
            || expectedWitnesses == null || expectedWitnesses.Count != 2)
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The exact Core Justice census witnesses could not be composed.";
            return false;
        }

        OwnerSectionCensusSnapshot records = null;
        OwnerSectionCensusSnapshot receipts = null;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (section == null) continue;
            if (string.Equals(section.SectionId,
                    DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId, StringComparison.Ordinal))
            {
                if (records != null)
                {
                    diagnostic = "The host Justice records census contains a duplicate section identity.";
                    return false;
                }
                records = section;
            }
            else if (string.Equals(section.SectionId,
                    DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId, StringComparison.Ordinal))
            {
                if (receipts != null)
                {
                    diagnostic = "The host Justice receipt census contains a duplicate section identity.";
                    return false;
                }
                receipts = section;
            }
        }

        OwnerSectionCensusWitness expectedRecords = expectedWitnesses[0];
        OwnerSectionCensusWitness expectedReceipts = expectedWitnesses[1];
        bool recordsMatch = records != null
            && records.SchemaVersion == expectedRecords.SchemaVersion
            && records.Role == OwnerSectionRole.Required
            && ReferenceEquals(records.OwnerInstanceIdentity, expectedRecords.OwnerInstanceIdentity)
            && records.Cardinality == expectedRecords.Cardinality
            && records.Revision == expectedRecords.Revision;
        bool receiptsMatch = receipts != null
            && receipts.SchemaVersion == expectedReceipts.SchemaVersion
            && receipts.Role == OwnerSectionRole.Required
            && ReferenceEquals(receipts.OwnerInstanceIdentity, expectedReceipts.OwnerInstanceIdentity)
            && receipts.Cardinality == expectedReceipts.Cardinality
            && receipts.Revision == expectedReceipts.Revision;
        if (!recordsMatch || !receiptsMatch)
        {
            diagnostic = "The Justice census does not match its exact Core owner and host projection.";
            return false;
        }

        return true;
    }

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
        if (!DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(completeHostSections))
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

    /// <summary>
    /// Reconstructs the selected NPC owner census fragment from Core owner
    /// instances. Exact omissions are recomposed; mixed rosters and stale
    /// captured rows retain their established host facts for P12 validation.
    /// P12-D receipt sections remain host-owned and are never removed here.
    /// </summary>
    internal static bool TryComposeCoreOwnedNpcOwnerCensusFragment(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (graph?.Npcs == null || hostSections == null)
        {
            diagnostic = "The Core NPC census fragment requires its selected graph and host remainder.";
            return false;
        }

        if (!TryRebindCoreOwnedNpcSections(
                graph, hostSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> inventoryAndMoney,
                out diagnostic))
            return false;

        IReadOnlyList<OwnerSectionCensusSnapshot> runtimeState = inventoryAndMoney;
        if (graph.PersonStore != null
            && !TryRebindCoreOwnedNpcRuntimeStateSections(
                graph, inventoryAndMoney, out runtimeState, out diagnostic))
            return false;

        if (!TryRebindCoreOwnedCommercialKnowledgeSections(
                graph, runtimeState,
                out IReadOnlyList<OwnerSectionCensusSnapshot> commercialKnowledge,
                out diagnostic))
            return false;

        if (!TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
                graph, commercialKnowledge,
                out IReadOnlyList<OwnerSectionCensusSnapshot> knowledgeAndPlan,
                out diagnostic))
            return false;

        if (!TryRebindCoreSpatialKnowledgeSections(
                graph, knowledgeAndPlan,
                out ownerSections, out diagnostic))
            return false;

        return true;
    }

    internal static bool TryCreateCoreOwnedP12FSectionWitnesses(
        DailyV1SelectedRuntimeOwnerGraph graph,
        ActorChoiceStore actorChoiceStore,
        out OwnerSectionCensusWitness[] witnesses,
        out string diagnostic)
    {
        witnesses = null;
        diagnostic = null;
        if (graph?.ScheduledDirectives == null
            || graph.TravelParties == null
            || graph.Expeditions == null
            || graph.PersonStore == null
            || graph.PoliticalKnowledgeStore == null
            || !graph.PoliticalKnowledgeStore.IsBoundToPersonStore(graph.PersonStore)
            || actorChoiceStore == null
            || !ReferenceEquals(actorChoiceStore.PersonStore, graph.PersonStore))
        {
            diagnostic = "The Core P12-F census requires the exact actor-choice, directive, travel-party, expedition, and PersonStore owners.";
            return false;
        }

        try
        {
            witnesses = new OwnerSectionCensusWitness[]
            {
                new ActorChoiceP11CensusProvider(actorChoiceStore).GetCurrentCensus(),
                new ScheduledDirectiveCensusProvider(graph.ScheduledDirectives).GetCurrentCensus(),
                new TravelPartyCensusProvider(graph.TravelParties).GetCurrentCensus(),
                new ExpeditionCensusProvider(graph.Expeditions).GetCurrentCensus(),
                new PoliticalKnowledgeStoreCensusProvider(graph.PoliticalKnowledgeStore).GetCurrentCensus()
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

        HashSet<string> sectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusWitness witness in witnesses)
            if (witness == null
                || string.IsNullOrWhiteSpace(witness.SectionId)
                || witness.OwnerInstanceIdentity == null
                || witness.SchemaVersion <= 0
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || !sectionIds.Add(witness.SectionId)
                || !Array.Exists(CoreOwnedP12FSectionIdValues,
                    sectionId => string.Equals(sectionId, witness.SectionId, StringComparison.Ordinal)))
            {
                diagnostic = "A Core P12-F provider returned a witness outside the fixed selected owner inventory.";
                witnesses = null;
                return false;
            }

        if (witnesses.Length != CoreOwnedP12FSectionIdValues.Length
            || sectionIds.Count != CoreOwnedP12FSectionIdValues.Length)
        {
            diagnostic = "The Core P12-F provider set is incomplete.";
            witnesses = null;
            return false;
        }

        return true;
    }
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
            || graph.PoliticalKnowledgeStore == null
            || !graph.PoliticalKnowledgeStore.IsBoundToPersonStore(graph.PersonStore)
            || actorChoiceStore == null
            || !ReferenceEquals(actorChoiceStore.PersonStore, graph.PersonStore)
            || hostOwnedSections == null)
        {
            diagnostic = "The Core P12-F census requires the exact actor-choice, directive, travel-party, expedition, and PersonStore owners.";
            return false;
        }

        // Core reconstructs the complete selected NPC owner fragment from the
        // exact admitted graph. Unity supplies only residual profile rows when
        // the selected roster is exact; mixed compatibility rosters keep their
        // complete host rows through the existing per-family fallback.
        if (!TryComposeCoreOwnedNpcOwnerCensusFragment(
                graph, hostOwnedSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> hostAndCoreNpcSections,
                out diagnostic))
            return false;

        if (!TryComposeCoreOwnedRuntimeIdAllocatorSections(
                graph, hostAndCoreNpcSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> sectionsWithAllocatorRows,
                out diagnostic))
            return false;

        HashSet<string> hostSectionIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, OwnerSectionCensusSnapshot> hostP12FSections =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        List<OwnerSectionCensusSnapshot> composed =
            new List<OwnerSectionCensusSnapshot>(sectionsWithAllocatorRows.Count + CoreOwnedP12FSectionIdValues.Length);
        foreach (OwnerSectionCensusSnapshot section in sectionsWithAllocatorRows)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !hostSectionIds.Add(section.SectionId))
            {
                diagnostic = "The complete host census is invalid or contains a duplicate section identity.";
                return false;
            }
            if (Array.IndexOf(CoreOwnedP12FSectionIdValues, section.SectionId) >= 0)
                hostP12FSections.Add(section.SectionId, section);
            else
                composed.Add(section);
        }

        if (hostP12FSections.Count != 0
            && hostP12FSections.Count != CoreOwnedP12FSectionIdValues.Length)
        {
            diagnostic = "The host census partially omitted Core-owned P12-F sections.";
            return false;
        }

        if (!TryCreateCoreOwnedP12FSectionWitnesses(
                graph, actorChoiceStore,
                out OwnerSectionCensusWitness[] witnesses,
                out diagnostic))
            return false;

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

            OwnerSectionCensusSnapshot expected = new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision);
            if (hostP12FSections.TryGetValue(
                    expected.SectionId, out OwnerSectionCensusSnapshot actual)
                && !HasExactOwnerSectionFacts(actual, expected))
            {
                diagnostic = "The complete host census differs from the exact Core P12-F owner at "
                    + expected.SectionId + ".";
                return false;
            }

            composed.Add(expected);
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
            || graph.PoliticalKnowledgeStore == null
            || !graph.PoliticalKnowledgeStore.IsBoundToPersonStore(graph.PersonStore)
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

        if (!TryCreateCoreOwnedP12FSectionWitnesses(
                graph, actorChoiceStore,
                out OwnerSectionCensusWitness[] witnesses,
                out diagnostic))
            return false;

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

    internal static bool TryCreateCoreOwnedPoliticalDecisionSection(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out OwnerSectionCensusSnapshot section,
        out string diagnostic)
    {
        section = null;
        diagnostic = null;
        PoliticalDecisionStore owner = graph?.PoliticalDecisionStore;
        PersonStore personStore = graph?.PersonStore;
        if (owner == null || personStore == null || !owner.IsBoundToPersonStore(personStore))
        {
            diagnostic = "The selected Core census requires its exact P12-E decision owner bound to the selected PersonStore.";
            return false;
        }

        OwnerSectionCensusWitness witness;
        try
        {
            witness = new PoliticalDecisionStoreCensusProvider(owner).GetCurrentCensus();
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "The Core PoliticalDecision census provider could not read its exact owner.";
            return false;
        }

        if (witness == null
            || !string.Equals(witness.SectionId, PoliticalDecisionStoreCensusProvider.SectionId, StringComparison.Ordinal)
            || witness.SchemaVersion != PoliticalDecisionStoreCensusProvider.SchemaVersion
            || !ReferenceEquals(witness.OwnerInstanceIdentity, owner)
            || witness.Cardinality < 0
            || witness.Revision < 0L)
        {
            diagnostic = "The Core PoliticalDecision census provider returned an invalid owner witness.";
            return false;
        }

        section = new OwnerSectionCensusSnapshot(
            witness.SectionId,
            witness.SchemaVersion,
            OwnerSectionRole.Required,
            owner,
            witness.Cardinality,
            witness.Revision);
        return true;
    }

    internal static bool TryComposeCoreOwnedPoliticalDecisionSection(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (hostSections == null
            || !TryCreateCoreOwnedPoliticalDecisionSection(
                graph, out OwnerSectionCensusSnapshot expected, out diagnostic))
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The Core PoliticalDecision census requires its owner graph and host remainder.";
            return false;
        }

        List<OwnerSectionCensusSnapshot> copied = new List<OwnerSectionCensusSnapshot>(hostSections.Count + 1);
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < hostSections.Count; i++)
        {
            OwnerSectionCensusSnapshot hostSection = hostSections[i];
            if (hostSection == null || string.IsNullOrWhiteSpace(hostSection.SectionId)
                || !indexes.TryAdd(hostSection.SectionId, i))
            {
                diagnostic = "The host census contains a missing or duplicate owner-section identity.";
                return false;
            }
            copied.Add(hostSection);
        }

        if (indexes.TryGetValue(expected.SectionId, out int index))
        {
            if (!HasExactOwnerSectionFacts(copied[index], expected))
            {
                diagnostic = "The host census differs from the exact Core PoliticalDecision owner.";
                return false;
            }
            copied[index] = expected;
        }
        else
        {
            copied.Add(expected);
        }

        ownerSections = Array.AsReadOnly(copied.ToArray());
        return true;
    }

    internal static bool TryRebindCoreOwnedPoliticalDecisionSection(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        PoliticalDecisionStore owner = graph?.PoliticalDecisionStore;
        if (owner == null || graph.PersonStore == null
            || !owner.IsBoundToPersonStore(graph.PersonStore)
            || hostSections == null)
        {
            diagnostic = "The selected Core census requires its exact P12-E decision owner bound to the selected PersonStore and host census rows.";
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

        if (!indexes.TryGetValue(PoliticalDecisionStoreCensusProvider.SectionId, out int index))
        {
            diagnostic = "The host census is missing the fixed required PoliticalDecision owner section.";
            return false;
        }

        if (!TryCreateCoreOwnedPoliticalDecisionSection(
                graph, out OwnerSectionCensusSnapshot expected, out diagnostic))
            return false;

        OwnerSectionCensusSnapshot hostSection = copied[index];
        if (!HasExactOwnerSectionFacts(hostSection, expected))
        {
            diagnostic = "The host census differs from the exact selected Core PoliticalDecision owner.";
            return false;
        }

        copied[index] = expected;
        ownerSections = Array.AsReadOnly(copied);
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

        bool hostOmittedExactCoreNpcRows = graph.Npcs.Count > 0
            && inventoryRows == 0
            && moneyAccountRows == 0;
        if (!TryCreateCoreOwnedNpcInventoryAndMoneyAccountCensusWitnesses(
                graph, out IReadOnlyList<OwnerSectionCensusWitness> witnesses, out diagnostic))
            return false;

        HashSet<string> selectedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        List<OwnerSectionCensusSnapshot> composed = null;
        foreach (OwnerSectionCensusWitness witness in witnesses)
        {
            if (witness == null || !selectedSectionIds.Add(witness.SectionId))
            {
                diagnostic = "A selected Core NPC census provider returned a missing or duplicate witness.";
                return false;
            }

            if (!indexes.TryGetValue(witness.SectionId, out int index))
            {
                if (!hostOmittedExactCoreNpcRows)
                {
                    diagnostic = "The host census is missing selected Core section " + witness.SectionId + ".";
                    return false;
                }

                composed ??= new List<OwnerSectionCensusSnapshot>(
                    copied.Length + witnesses.Count);
                composed.Add(new OwnerSectionCensusSnapshot(
                    witness.SectionId,
                    witness.SchemaVersion,
                    OwnerSectionRole.Required,
                    witness.OwnerInstanceIdentity,
                    witness.Cardinality,
                    witness.Revision));
                continue;
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

        if (composed != null)
        {
            composed.InsertRange(0, copied);
            composed.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
            ownerSections = Array.AsReadOnly(composed.ToArray());
            diagnostic = null;
            return true;
        }

        ownerSections = Array.AsReadOnly(copied);
        diagnostic = null;
        return true;
    }

    /// <summary>
    /// Returns selected NPC Inventory and MoneyAccount witnesses from the
    /// exact Core owner graph. The dynamic section keys are one-per-NPC and
    /// preserve their canonical runtime identity.
    /// </summary>
    private static bool TryCreateCoreOwnedNpcInventoryAndMoneyAccountCensusWitnesses(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<OwnerSectionCensusWitness> witnesses,
        out string diagnostic)
    {
        witnesses = null;
        diagnostic = null;
        if (graph?.Npcs == null)
        {
            diagnostic = "The selected Core NPC census requires its exact owner graph.";
            return false;
        }

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try
        {
            List<IOwnerSectionCensusProvider> selected = new List<IOwnerSectionCensusProvider>();
            selected.AddRange(DailyV1NpcOwnerCensusProviders.CreateInventoryProviders(graph.Npcs));
            selected.AddRange(DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs));
            providers = selected.AsReadOnly();
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException)
        {
            diagnostic = "The selected Core NPC census providers could not bind to the owner graph.";
            return false;
        }

        int expectedCount;
        try { expectedCount = checked(graph.Npcs.Count * 2); }
        catch (OverflowException)
        {
            diagnostic = "The selected Core NPC census provider count overflowed.";
            return false;
        }

        if (providers.Count != expectedCount)
        {
            diagnostic = "The selected Core NPC census must contain one Inventory and one MoneyAccount provider per owner.";
            return false;
        }

        HashSet<string> expectedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !expectedSectionIds.Add(
                    DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + npc.RuntimeId)
                || !expectedSectionIds.Add(
                    DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + npc.RuntimeId))
            {
                diagnostic = "The selected Core NPC census graph has a missing or duplicate runtime identity.";
                return false;
            }
        }

        OwnerSectionCensusWitness[] built = new OwnerSectionCensusWitness[providers.Count];
        HashSet<string> observedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < providers.Count; index++)
        {
            OwnerSectionCensusWitness witness;
            try { witness = providers[index]?.GetCurrentCensus(); }
            catch (Exception exception) when (exception is ArgumentException
                || exception is InvalidOperationException || exception is OverflowException)
            {
                diagnostic = "A selected Core NPC census provider failed to read its exact owner.";
                return false;
            }

            if (witness == null
                || string.IsNullOrWhiteSpace(witness.SectionId)
                || witness.OwnerInstanceIdentity == null
                || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || !expectedSectionIds.Contains(witness.SectionId)
                || !observedSectionIds.Add(witness.SectionId))
            {
                diagnostic = "A selected Core NPC census provider returned an invalid or duplicate owner witness.";
                return false;
            }
            built[index] = witness;
        }

        if (observedSectionIds.Count != expectedSectionIds.Count)
        {
            diagnostic = "The selected Core NPC census provider set does not cover each exact owner identity.";
            return false;
        }

        Array.Sort(built, (left, right) =>
            StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        witnesses = Array.AsReadOnly(built);
        return true;
    }

    /// <summary>
    /// Selects the complete dynamic NPC owner fragment that Core can source
    /// from the admitted graph. Omission is allowed only when every selected
    /// family row is present and exactly matches its Core owner witness. A
    /// mixed roster, stale row, or substituted owner keeps the full host vector.
    /// The P12-D receipt families intentionally remain in the host remainder.
    /// </summary>
    internal static bool TryGetExactCoreOwnedNpcCensusFragmentSectionIds(
        IReadOnlyList<OwnerSectionCensusSnapshot> completeHostSections,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<string> sectionIds,
        out string diagnostic)
    {
        sectionIds = Array.Empty<string>();
        diagnostic = null;
        if (completeHostSections == null || graph?.Npcs == null)
        {
            diagnostic = "The selected Core NPC census fragment requires the complete host vector and exact owner graph.";
            return false;
        }

        if (graph.Npcs.Count == 0 || graph.PersonStore == null) return true;

        Dictionary<string, OwnerSectionCensusSnapshot> hostById =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in completeHostSections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !hostById.TryAdd(section.SectionId, section))
            {
                diagnostic = "The complete host census contains a missing or duplicate owner-section identity.";
                return false;
            }
        }

        int selectedNpcCount = graph.Npcs.Count;
        int inventoryRows = CountFamilyRows(
            completeHostSections, DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
        int moneyAccountRows = CountFamilyRows(
            completeHostSections, DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
        if (inventoryRows > selectedNpcCount || moneyAccountRows > selectedNpcCount)
            return true;
        if (inventoryRows != selectedNpcCount || moneyAccountRows != selectedNpcCount)
        {
            diagnostic = "The host census does not cover every selected NPC Inventory and MoneyAccount owner.";
            return false;
        }

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try
        {
            List<IOwnerSectionCensusProvider> selected =
                new List<IOwnerSectionCensusProvider>();
            selected.AddRange(DailyV1NpcOwnerCensusProviders.CreateInventoryProviders(graph.Npcs));
            selected.AddRange(DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs));
            selected.AddRange(DailyV1NpcOwnerCensusProviders.CreateRuntimeAuthorityProviders(graph));
            selected.AddRange(DailyV1NpcOwnerCensusProviders.CreateCommercialKnowledgeProviders(graph.Npcs));
            selected.AddRange(DailyV1NpcOwnerCensusProviders.CreateSpatialKnowledgeProviders(graph.Npcs));
            selected.AddRange(DailyV1NpcOwnerCensusProviders.CreateKnowledgeAndPlanProviders(graph));
            providers = selected.AsReadOnly();
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "The selected Core NPC census providers could not bind to the exact owner graph.";
            return false;
        }

        List<string> familyPrefixes = new List<string>
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcLifeStatePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcResidencePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialLiquidityPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix
        };
        familyPrefixes.AddRange(CoreOwnedNpcKnowledgeAndPlanSectionPrefixValues);

        Dictionary<string, int> expectedRowsByPrefix =
            new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string prefix in familyPrefixes)
            if (string.IsNullOrWhiteSpace(prefix)
                || !expectedRowsByPrefix.TryAdd(prefix, 0))
            {
                diagnostic = "The selected Core NPC census family inventory is invalid or duplicated.";
                return false;
            }

        OwnerSectionCensusWitness[] witnesses =
            new OwnerSectionCensusWitness[providers.Count];
        HashSet<string> selectedIds = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < providers.Count; index++)
        {
            OwnerSectionCensusWitness witness;
            try { witness = providers[index]?.GetCurrentCensus(); }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is InvalidOperationException
                || exception is OverflowException)
            {
                diagnostic = "A selected Core NPC census provider failed to read its exact owner.";
                return false;
            }

            if (witness == null
                || string.IsNullOrWhiteSpace(witness.SectionId)
                || witness.OwnerInstanceIdentity == null
                || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || !selectedIds.Add(witness.SectionId))
            {
                diagnostic = "A selected Core NPC census provider returned an invalid or duplicate witness.";
                return false;
            }

            string matchedPrefix = null;
            foreach (string prefix in familyPrefixes)
            {
                if (!witness.SectionId.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (matchedPrefix != null)
                {
                    diagnostic = "A selected Core NPC census witness matched more than one owner family.";
                    return false;
                }
                matchedPrefix = prefix;
            }
            if (matchedPrefix == null)
            {
                diagnostic = "A selected Core NPC census provider returned a section outside the admitted owner families.";
                return false;
            }
            expectedRowsByPrefix[matchedPrefix]++;
            witnesses[index] = witness;
        }

        foreach (string prefix in familyPrefixes)
        {
            int expectedRows = expectedRowsByPrefix[prefix];
            int actualRows = CountFamilyRows(completeHostSections, prefix);
            if (actualRows > expectedRows)
                return true;
            if (actualRows < expectedRows)
            {
                diagnostic = "The host census is missing selected Core NPC rows for " + prefix;
                return false;
            }
        }

        List<string> exactIds = new List<string>(witnesses.Length);
        foreach (OwnerSectionCensusWitness witness in witnesses)
        {
            if (!hostById.TryGetValue(witness.SectionId, out OwnerSectionCensusSnapshot actual))
            {
                diagnostic = "The host census is missing selected Core NPC section " + witness.SectionId + ".";
                return false;
            }

            if (actual.Role != OwnerSectionRole.Required
                || actual.SchemaVersion != witness.SchemaVersion
                || !ReferenceEquals(actual.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                || actual.Cardinality != witness.Cardinality
                || actual.Revision != witness.Revision)
                return true;

            exactIds.Add(witness.SectionId);
        }

        exactIds.Sort(StringComparer.Ordinal);
        sectionIds = Array.AsReadOnly(exactIds.ToArray());
        return true;
    }

    /// <summary>
    /// Produces the host-owned remainder after removing selected NPC, City,
    /// PoliticalDecision, P12-F, and allocator census rows only when their live
    /// values match the exact Core owner graph. Mixed, partial, stale, or
    /// substituted rows remain available to the existing P12 validation path.
    /// </summary>
    internal static bool TryCreateHostOwnedCensusRemainder(
        IReadOnlyList<OwnerSectionCensusSnapshot> completeHostSections,
        DailyV1SelectedRuntimeOwnerGraph graph,
        ActorChoiceStore actorChoiceStore,
        out IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedSections,
        out string diagnostic)
    {
        hostOwnedSections = null;
        diagnostic = null;
        if (completeHostSections == null || graph == null)
        {
            diagnostic = "The selected Core census remainder requires its complete source vector and owner graph.";
            return false;
        }

        if (!TryGetExactCoreOwnedNpcCensusFragmentSectionIds(
                completeHostSections, graph,
                out IReadOnlyList<string> exactNpcSectionIds,
                out diagnostic))
            return false;

        Dictionary<string, OwnerSectionCensusSnapshot> sourceById =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in completeHostSections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !sourceById.TryAdd(section.SectionId, section))
            {
                diagnostic = "The complete selected owner census contains an invalid or duplicate section.";
                return false;
            }
        }

        int capturedCoreP12FRowCount = 0;
        foreach (OwnerSectionCensusSnapshot section in completeHostSections)
            if (Array.IndexOf(CoreOwnedP12FSectionIdValues, section.SectionId) >= 0)
                capturedCoreP12FRowCount++;
        if (capturedCoreP12FRowCount == 0)
        {
            diagnostic = "The source census omitted every Core-owned P12-F row before projection.";
            return false;
        }
        HashSet<string> coreOwnedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in exactNpcSectionIds)
        {
            if (string.IsNullOrWhiteSpace(sectionId)
                || !sourceById.ContainsKey(sectionId)
                || !coreOwnedIds.Add(sectionId))
            {
                diagnostic = "The exact Core NPC census fragment does not match unique source rows.";
                return false;
            }
        }

        if (!TryCreateCoreOwnedPoliticalDecisionSection(
                graph, out OwnerSectionCensusSnapshot politicalDecisionWitness, out diagnostic)
            || !sourceById.TryGetValue(
                PoliticalDecisionStoreCensusProvider.SectionId,
                out OwnerSectionCensusSnapshot sourcePoliticalDecision))
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The complete source census omitted the required Core PoliticalDecision owner row.";
            return false;
        }
        if (HasExactOwnerSectionFacts(sourcePoliticalDecision, politicalDecisionWitness)
            && !coreOwnedIds.Add(politicalDecisionWitness.SectionId))
        {
            diagnostic = "Core NPC and PoliticalDecision census identities overlap.";
            return false;
        }

        // Omit selected City rows only as one complete exact set. A stale row,
        // partial vector, or additional City roster keeps every City row on the
        // host path so existing P12 compatibility and rejection remain intact.
        if (TryCreateCoreOwnedCityCensusWitnesses(
                graph, out IReadOnlyList<OwnerSectionCensusWitness> cityWitnesses, out _))
        {
            int capturedCityRowCount = 0;
            foreach (OwnerSectionCensusSnapshot section in completeHostSections)
            {
                foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.CityPrefixes)
                    if (section.SectionId.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        capturedCityRowCount++;
                        break;
                    }
            }
            if (capturedCityRowCount == cityWitnesses.Count)
            {
                List<string> exactCitySectionIds = new List<string>(cityWitnesses.Count);
                bool exactCityWitnessSet = true;
                foreach (OwnerSectionCensusWitness expected in cityWitnesses)
                {
                    if (!sourceById.TryGetValue(expected.SectionId, out OwnerSectionCensusSnapshot actual)
                        || !HasExactOwnerSectionFacts(
                            actual, ToOwnerSectionSnapshot(expected, OwnerSectionRole.Required)))
                    {
                        exactCityWitnessSet = false;
                        break;
                    }
                    exactCitySectionIds.Add(expected.SectionId);
                }

                if (exactCityWitnessSet)
                    foreach (string sectionId in exactCitySectionIds)
                        if (!coreOwnedIds.Add(sectionId))
                        {
                            diagnostic = "Core NPC, City, and RuntimeIdAllocator census identities overlap.";
                            return false;
                        }
            }
        }

        // Core owns the fixed P12-F census rows only as one exact complete set.
        // Partial, stale, or foreign rows stay in the host remainder so the
        // established P12-F composition gate rejects them without reclassification.
        if (capturedCoreP12FRowCount == CoreOwnedP12FSectionIdValues.Length)
        {
            if (!TryCreateCoreOwnedP12FSectionWitnesses(
                    graph, actorChoiceStore,
                    out OwnerSectionCensusWitness[] p12fWitnesses,
                    out diagnostic))
                return false;

            bool exactP12FSet = true;
            foreach (OwnerSectionCensusWitness expected in p12fWitnesses)
                if (!sourceById.TryGetValue(expected.SectionId, out OwnerSectionCensusSnapshot actual)
                    || !HasExactOwnerSectionFacts(
                        actual, ToOwnerSectionSnapshot(expected, OwnerSectionRole.Required)))
                {
                    exactP12FSet = false;
                    break;
                }

            if (exactP12FSet)
                foreach (OwnerSectionCensusWitness witness in p12fWitnesses)
                    if (!coreOwnedIds.Add(witness.SectionId))
                    {
                        diagnostic = "Core NPC, City, P12-F, and RuntimeIdAllocator census identities overlap.";
                        return false;
                    }
        }
        // Runtime allocator counters are a fixed three-row set. Omit the set
        // only when every row matches its Core witness; stale or partial sets
        // stay intact for the later P12 composition and admission gates.
        if (TryCreateCoreOwnedRuntimeIdAllocatorCensusSections(
                graph, out IReadOnlyList<OwnerSectionCensusSnapshot> allocatorRows, out _)
            && allocatorRows.Count == CoreOwnedRuntimeIdAllocatorSectionIdValues.Length)
        {
            bool exactAllocatorTriplet = true;
            foreach (OwnerSectionCensusSnapshot expected in allocatorRows)
            {
                if (!sourceById.TryGetValue(expected.SectionId, out OwnerSectionCensusSnapshot actual)
                    || !HasExactOwnerSectionFacts(actual, expected))
                {
                    exactAllocatorTriplet = false;
                    break;
                }
            }

            if (exactAllocatorTriplet)
                foreach (OwnerSectionCensusSnapshot allocatorRow in allocatorRows)
                    if (!coreOwnedIds.Add(allocatorRow.SectionId))
                    {
                        diagnostic = "Core NPC and RuntimeIdAllocator census identities overlap.";
                        return false;
                    }
        }

        List<OwnerSectionCensusSnapshot> remainder =
            new List<OwnerSectionCensusSnapshot>(completeHostSections.Count - coreOwnedIds.Count);
        foreach (OwnerSectionCensusSnapshot section in completeHostSections)
            if (!coreOwnedIds.Contains(section.SectionId))
                remainder.Add(section);

        hostOwnedSections = Array.AsReadOnly(remainder.ToArray());
        return true;
    }

    /// <summary>
    /// Identifies exact selected Inventory and MoneyAccount rows that may be
    /// omitted from the Unity remainder. Mixed rosters, stale facts, or
    /// substituted rows remain on the complete host path for P12 to validate.
    /// </summary>
    internal static bool TryGetExactCoreOwnedNpcInventoryAndMoneyAccountSectionIds(
        IReadOnlyList<OwnerSectionCensusSnapshot> completeHostSections,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<string> sectionIds,
        out string diagnostic)
    {
        sectionIds = Array.Empty<string>();
        diagnostic = null;
        if (completeHostSections == null || graph?.Npcs == null
            || !DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
                completeHostSections))
        {
            diagnostic = "Selected NPC owner omission requires a structurally complete source census and exact graph.";
            return false;
        }

        int inventoryRows = CountFamilyRows(
            completeHostSections, DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
        int moneyAccountRows = CountFamilyRows(
            completeHostSections, DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
        if (inventoryRows != moneyAccountRows)
        {
            diagnostic = "The complete source census has inconsistent NPC Inventory and MoneyAccount cardinality.";
            return false;
        }

        // Additional compatibility NPCs keep every NPC row in the host
        // vector. A partial or stale selected set is likewise not removed.
        if (graph.Npcs.Count == 0 || inventoryRows != graph.Npcs.Count)
            return true;

        if (!TryCreateCoreOwnedNpcInventoryAndMoneyAccountCensusWitnesses(
                graph, out IReadOnlyList<OwnerSectionCensusWitness> witnesses, out diagnostic))
            return false;

        Dictionary<string, OwnerSectionCensusSnapshot> hostById =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in completeHostSections)
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !hostById.TryAdd(section.SectionId, section))
            {
                diagnostic = "The complete source census has a missing or duplicate owner-section identity.";
                return false;
            }

        bool exact = true;
        List<string> exactIds = new List<string>(witnesses.Count);
        foreach (OwnerSectionCensusWitness witness in witnesses)
        {
            if (!hostById.TryGetValue(witness.SectionId, out OwnerSectionCensusSnapshot actual))
                return true;

            exact &= actual.Role == OwnerSectionRole.Required
                && actual.SchemaVersion == witness.SchemaVersion
                && ReferenceEquals(actual.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                && actual.Cardinality == witness.Cardinality
                && actual.Revision == witness.Revision;
            exactIds.Add(witness.SectionId);
        }

        if (exact) sectionIds = Array.AsReadOnly(exactIds.ToArray());
        return true;
    }

    /// <summary>
    /// Identifies exact-roster NPC state rows whose authority already lives in
    /// the selected Core owner graph. Mixed rosters and stale or substituted
    /// host rows stay on the complete Unity compatibility path.
    /// </summary>
    internal static bool TryGetExactCoreOwnedNpcRuntimeStateSectionIds(
        IReadOnlyList<OwnerSectionCensusSnapshot> completeHostSections,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<string> sectionIds,
        out string diagnostic)
    {
        sectionIds = Array.Empty<string>();
        diagnostic = null;
        if (completeHostSections == null || graph?.Npcs == null
            || !DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
                completeHostSections))
        {
            diagnostic = "Selected NPC runtime-state omission requires a structurally complete source census and exact graph.";
            return false;
        }

        if (graph.Npcs.Count == 0 || graph.PersonStore == null)
            return true;

        int inventoryRows = CountFamilyRows(
            completeHostSections, DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
        int moneyAccountRows = CountFamilyRows(
            completeHostSections, DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
        if (inventoryRows != moneyAccountRows)
        {
            diagnostic = "The complete source census has inconsistent NPC Inventory and MoneyAccount cardinality.";
            return false;
        }

        // Preserve all rows for additional compatibility NPCs. The complete
        // vector is still validated by the Core runtime-state rebind below.
        if (inventoryRows > graph.Npcs.Count)
            return true;
        if (inventoryRows != graph.Npcs.Count)
        {
            diagnostic = "The complete source census does not cover every selected NPC runtime-state owner.";
            return false;
        }

        if (!TryRebindCoreOwnedNpcRuntimeStateSections(
                graph, completeHostSections,
                out _, out diagnostic))
            return false;

        HashSet<string> expected = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !expected.Add(DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix + npc.RuntimeId)
                || !expected.Add(DailyV1SelectedOwnerSectionCensusContract.NpcLifeStatePrefix + npc.RuntimeId)
                || !expected.Add(DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(npc.RuntimeId))
                || !expected.Add(DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix + npc.RuntimeId))
            {
                diagnostic = "The selected NPC runtime-state graph contains a missing or duplicate identity.";
                return false;
            }

            if (!graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    npc.RuntimeId, out PersonRuntime person))
            {
                if (!expected.Add(
                        DailyV1SelectedOwnerSectionCensusContract.NpcResidenceSectionIdFor(npc.RuntimeId)))
                {
                    diagnostic = "The selected NPC residence graph contains a duplicate identity.";
                    return false;
                }
            }
            else if (person == null
                || !string.Equals(person.MaterializedNpcRuntimeId, npc.RuntimeId,
                    StringComparison.Ordinal))
            {
                diagnostic = "The Core PersonStore returned an inconsistent NPC materialization binding.";
                return false;
            }
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

        Dictionary<string, OwnerSectionCensusSnapshot> hostById =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in completeHostSections)
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !hostById.TryAdd(section.SectionId, section))
            {
                diagnostic = "The complete source census has a missing or duplicate owner-section identity.";
                return false;
            }

        List<string> exactIds = new List<string>(providers.Count);
        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            OwnerSectionCensusWitness witness;
            try { witness = provider?.GetCurrentCensus(); }
            catch (InvalidOperationException)
            {
                diagnostic = "A selected Core NPC runtime-state provider failed to read its owner.";
                return false;
            }

            if (witness == null || string.IsNullOrWhiteSpace(witness.SectionId)
                || !expected.Remove(witness.SectionId)
                || !hostById.TryGetValue(witness.SectionId,
                    out OwnerSectionCensusSnapshot actual)
                || actual.Role != OwnerSectionRole.Required
                || actual.SchemaVersion != witness.SchemaVersion
                || !ReferenceEquals(actual.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                || actual.Cardinality != witness.Cardinality
                || actual.Revision != witness.Revision)
            {
                diagnostic = "The complete source census differs from an exact Core-owned NPC runtime-state witness.";
                return false;
            }

            exactIds.Add(witness.SectionId);
        }

        if (expected.Count != 0)
        {
            diagnostic = "The selected Core NPC runtime-state witness set is incomplete.";
            return false;
        }

        sectionIds = Array.AsReadOnly(exactIds.ToArray());
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
        if (graph?.Npcs == null || hostSections == null)
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
        if (inventoryRows < graph.Npcs.Count)
        {
            diagnostic = "The host census does not cover every NPC in the selected Core graph.";
            return false;
        }

        string[] requiredPrefixes = CoreOwnedNpcKnowledgeAndPlanSectionPrefixValues;
        int[] hostFamilyRows = new int[requiredPrefixes.Length];
        bool allExactSelectedFamilyRowsOmitted = graph.Npcs.Count > 0
            && inventoryRows == graph.Npcs.Count;
        for (int i = 0; i < requiredPrefixes.Length; i++)
        {
            hostFamilyRows[i] = CountFamilyRows(copied, requiredPrefixes[i]);
            if (hostFamilyRows[i] != 0)
                allExactSelectedFamilyRowsOmitted = false;
        }
        if (inventoryRows > graph.Npcs.Count)
            allExactSelectedFamilyRowsOmitted = false;

        if (!allExactSelectedFamilyRowsOmitted)
        {
            for (int i = 0; i < requiredPrefixes.Length; i++)
            {
                if (hostFamilyRows[i] != inventoryRows)
                {
                    diagnostic = "The host census has incomplete selected NPC Knowledge and Plan family coverage for "
                        + requiredPrefixes[i];
                    return false;
                }
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

        int expectedWitnessCount;
        try
        {
            expectedWitnessCount = checked(graph.Npcs.Count * requiredPrefixes.Length);
        }
        catch (OverflowException)
        {
            diagnostic = "The selected Core NPC Knowledge and Plan owner cardinality overflowed.";
            return false;
        }
        if (providers == null || providers.Count != expectedWitnessCount)
        {
            diagnostic = "The selected Core NPC Knowledge and Plan provider set does not contain nine owners per NPC.";
            return false;
        }

        OwnerSectionCensusWitness[] witnesses = new OwnerSectionCensusWitness[providers.Count];
        HashSet<string> selectedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        int[] witnessFamilyRows = new int[requiredPrefixes.Length];
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

            int matchingPrefix = -1;
            for (int prefixIndex = 0; prefixIndex < requiredPrefixes.Length; prefixIndex++)
            {
                if (!witness.SectionId.StartsWith(requiredPrefixes[prefixIndex], StringComparison.Ordinal))
                    continue;
                if (matchingPrefix >= 0)
                {
                    diagnostic = "A selected Core NPC Knowledge and Plan witness matched multiple owner families.";
                    return false;
                }
                matchingPrefix = prefixIndex;
            }
            if (matchingPrefix < 0)
            {
                diagnostic = "A selected Core NPC Knowledge or Plan provider returned a section outside the fixed owner families.";
                return false;
            }
            witnessFamilyRows[matchingPrefix]++;
        }
        for (int i = 0; i < requiredPrefixes.Length; i++)
        {
            if (witnessFamilyRows[i] != graph.Npcs.Count)
            {
                diagnostic = "The selected Core Knowledge and Plan graph does not have exactly one owner per NPC for "
                    + requiredPrefixes[i];
                return false;
            }
        }

        if (inventoryRows > graph.Npcs.Count)
        {
            foreach (OwnerSectionCensusWitness witness in witnesses)
            {
                if (!indexes.ContainsKey(witness.SectionId))
                {
                    diagnostic = "The mixed-roster host census is missing selected Core section "
                        + witness.SectionId + ".";
                    return false;
                }
            }

            // A mixed roster retains the complete host vector, including all
            // compatibility NPC rows, without replacing selected-row snapshots.
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        if (inventoryRows != graph.Npcs.Count)
        {
            diagnostic = "The host census does not contain the exact selected NPC Knowledge and Plan owner cardinality.";
            return false;
        }

        if (allExactSelectedFamilyRowsOmitted)
        {
            List<OwnerSectionCensusSnapshot> recomposed =
                new List<OwnerSectionCensusSnapshot>(copied.Length + witnesses.Length);
            recomposed.AddRange(copied);
            foreach (OwnerSectionCensusWitness witness in witnesses)
            {
                if (indexes.ContainsKey(witness.SectionId))
                {
                    diagnostic = "The selected Core Knowledge and Plan omission overlaps a host owner-section identity.";
                    return false;
                }
                recomposed.Add(new OwnerSectionCensusSnapshot(
                    witness.SectionId,
                    witness.SchemaVersion,
                    OwnerSectionRole.Required,
                    witness.OwnerInstanceIdentity,
                    witness.Cardinality,
                    witness.Revision));
            }
            recomposed.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
            ownerSections = Array.AsReadOnly(recomposed.ToArray());
            return true;
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
    /// <summary>
    /// Returns exact selected NPC Knowledge and Plan rows that may be omitted
    /// from the Unity remainder. Mixed-roster, stale, or substituted rows
    /// remain host-owned for the existing P12 compatibility and stale checks.
    /// </summary>
    internal static bool TryGetExactCoreOwnedNpcKnowledgeAndPlanSectionIds(
        IReadOnlyList<OwnerSectionCensusSnapshot> completeHostSections,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<string> sectionIds,
        out string diagnostic)
    {
        sectionIds = Array.Empty<string>();
        diagnostic = null;
        if (completeHostSections == null || graph?.Npcs == null
            || !DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
                completeHostSections))
        {
            diagnostic = "Selected NPC Knowledge and Plan omission requires a structurally complete source census and exact graph.";
            return false;
        }
        if (graph.Npcs.Count == 0)
            return true;

        int inventoryRows = CountFamilyRows(
            completeHostSections, DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
        int moneyAccountRows = CountFamilyRows(
            completeHostSections, DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
        if (inventoryRows != moneyAccountRows)
        {
            diagnostic = "The complete source census has inconsistent NPC Inventory and MoneyAccount cardinality.";
            return false;
        }
        if (inventoryRows > graph.Npcs.Count)
            return true;
        if (inventoryRows != graph.Npcs.Count)
        {
            diagnostic = "The complete source census does not cover every selected NPC Knowledge and Plan owner.";
            return false;
        }

        if (!TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
                graph, completeHostSections, out _, out diagnostic))
            return false;

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

        int expectedWitnessCount;
        try
        {
            expectedWitnessCount = checked(graph.Npcs.Count * CoreOwnedNpcKnowledgeAndPlanSectionPrefixValues.Length);
        }
        catch (OverflowException)
        {
            diagnostic = "The selected Core NPC Knowledge and Plan owner cardinality overflowed.";
            return false;
        }
        if (providers == null || providers.Count != expectedWitnessCount)
        {
            diagnostic = "The selected Core NPC Knowledge and Plan provider set does not contain nine owners per NPC.";
            return false;
        }

        Dictionary<string, OwnerSectionCensusSnapshot> hostById =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in completeHostSections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !hostById.TryAdd(section.SectionId, section))
            {
                diagnostic = "The complete source census has a missing or duplicate owner-section identity.";
                return false;
            }
        }

        List<string> exactIds = new List<string>(providers.Count);
        HashSet<string> observedIds = new HashSet<string>(StringComparer.Ordinal);
        int[] witnessFamilyRows = new int[CoreOwnedNpcKnowledgeAndPlanSectionPrefixValues.Length];
        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            OwnerSectionCensusWitness witness;
            try
            {
                witness = provider?.GetCurrentCensus();
            }
            catch (InvalidOperationException)
            {
                diagnostic = "A selected Core NPC Knowledge or Plan provider failed to read its owner.";
                return false;
            }

            if (witness == null
                || string.IsNullOrWhiteSpace(witness.SectionId)
                || witness.OwnerInstanceIdentity == null
                || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || !observedIds.Add(witness.SectionId))
            {
                diagnostic = "A selected Core NPC Knowledge or Plan provider returned an invalid census witness.";
                return false;
            }

            int matchingPrefix = -1;
            for (int i = 0; i < CoreOwnedNpcKnowledgeAndPlanSectionPrefixValues.Length; i++)
            {
                if (!witness.SectionId.StartsWith(
                        CoreOwnedNpcKnowledgeAndPlanSectionPrefixValues[i], StringComparison.Ordinal))
                    continue;
                if (matchingPrefix >= 0)
                {
                    diagnostic = "A selected Core NPC Knowledge and Plan witness matched multiple owner families.";
                    return false;
                }
                matchingPrefix = i;
            }
            if (matchingPrefix < 0)
            {
                diagnostic = "A selected Core NPC Knowledge and Plan provider returned a section outside the fixed owner families.";
                return false;
            }
            witnessFamilyRows[matchingPrefix]++;

            if (!hostById.TryGetValue(witness.SectionId, out OwnerSectionCensusSnapshot actual))
                return true;
            if (actual.Role != OwnerSectionRole.Required
                || actual.SchemaVersion != witness.SchemaVersion
                || !ReferenceEquals(actual.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                || actual.Cardinality != witness.Cardinality
                || actual.Revision != witness.Revision)
                return true;

            exactIds.Add(witness.SectionId);
        }

        for (int i = 0; i < witnessFamilyRows.Length; i++)
        {
            if (witnessFamilyRows[i] != graph.Npcs.Count)
            {
                diagnostic = "The selected Core Knowledge and Plan graph does not have exactly one owner per NPC for "
                    + CoreOwnedNpcKnowledgeAndPlanSectionPrefixValues[i];
                return false;
            }
        }

        sectionIds = Array.AsReadOnly(exactIds.ToArray());
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
        bool hostOmittedExactCoreRuntimeStateRows = !mixedRoster && graph.Npcs.Count > 0;
        foreach (string prefix in statePrefixes)
            if (CountFamilyRows(copied, prefix) != 0)
                hostOmittedExactCoreRuntimeStateRows = false;

        foreach (string prefix in statePrefixes)
        {
            int expectedRows = 0;
            for (int i = 0; i < witnesses.Length; i++)
                if (witnesses[i].SectionId.StartsWith(prefix, StringComparison.Ordinal))
                    expectedRows++;

            int actualRows = CountFamilyRows(copied, prefix);
            if (hostOmittedExactCoreRuntimeStateRows && actualRows == 0)
                continue;
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
                if (hostOmittedExactCoreRuntimeStateRows)
                {
                    witnessIndexes[i] = -1;
                    continue;
                }
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

        if (hostOmittedExactCoreRuntimeStateRows)
        {
            List<OwnerSectionCensusSnapshot> recomposed =
                new List<OwnerSectionCensusSnapshot>(copied.Length + witnesses.Length);
            recomposed.AddRange(copied);
            foreach (OwnerSectionCensusWitness witness in witnesses)
                recomposed.Add(new OwnerSectionCensusSnapshot(
                    witness.SectionId,
                    witness.SchemaVersion,
                    OwnerSectionRole.Required,
                    witness.OwnerInstanceIdentity,
                    witness.Cardinality,
                    witness.Revision));
            recomposed.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
            ownerSections = Array.AsReadOnly(recomposed.ToArray());
            diagnostic = null;
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
        bool selectedCommercialRowsOmitted = graph.Npcs.Count > 0
            && inventoryRows == graph.Npcs.Count
            && marketRows == 0
            && liquidityRows == 0
            && receiptRows == 0;
        if (inventoryRows != moneyAccountRows
            || inventoryRows < graph.Npcs.Count
            || (inventoryRows > graph.Npcs.Count
                && (inventoryRows != marketRows
                    || inventoryRows != liquidityRows
                    || inventoryRows != receiptRows))
            || (inventoryRows == graph.Npcs.Count
                && !selectedCommercialRowsOmitted
                && (inventoryRows != marketRows
                    || inventoryRows != liquidityRows
                    || inventoryRows != receiptRows)))
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

        if (selectedCommercialRowsOmitted)
        {
            List<OwnerSectionCensusSnapshot> recomposed =
                new List<OwnerSectionCensusSnapshot>(copied.Length + witnesses.Length);
            recomposed.AddRange(copied);
            foreach (OwnerSectionCensusWitness witness in witnesses)
            {
                if (indexes.ContainsKey(witness.SectionId))
                {
                    diagnostic = "The selected Core CommercialKnowledge omission overlaps a host owner-section identity.";
                    return false;
                }
                recomposed.Add(new OwnerSectionCensusSnapshot(
                    witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                    witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision));
            }
            recomposed.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
            ownerSections = Array.AsReadOnly(recomposed.ToArray());
            return true;
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

        int spatialLocationRows = CountFamilyRows(copied,
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix);
        int spatialRouteRows = CountFamilyRows(copied,
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix);
        bool selectedSpatialRowsOmitted = graph.Npcs.Count > 0
            && inventoryRows == graph.Npcs.Count
            && spatialLocationRows == 0
            && spatialRouteRows == 0;


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

        if (!selectedSpatialRowsOmitted
            && (spatialLocationRows != graph.Npcs.Count
                || spatialRouteRows != graph.Npcs.Count))
        {
            diagnostic = "The host census does not contain one selected SpatialKnowledge locations/routes pair per Core NPC.";
            return false;
        }

        HashSet<string> selectedSectionIdsForCore = new HashSet<string>(StringComparer.Ordinal);
        List<OwnerSectionCensusWitness> witnesses =
            new List<OwnerSectionCensusWitness>(providers.Count);
        if (selectedSpatialRowsOmitted)
        {
            List<OwnerSectionCensusSnapshot> recomposed =
                new List<OwnerSectionCensusSnapshot>(copied.Length + providers.Count);
            recomposed.AddRange(copied);
            foreach (IOwnerSectionCensusProvider provider in providers)
            {
                OwnerSectionCensusWitness witness;
                try { witness = provider?.GetCurrentCensus(); }
                catch (InvalidOperationException)
                {
                    diagnostic = "A selected Core SpatialKnowledge census provider failed to read its owner.";
                    return false;
                }
                if (witness == null || indexes.ContainsKey(witness.SectionId))
                {
                    diagnostic = witness == null
                        ? "A selected Core SpatialKnowledge census provider returned no witness."
                        : "The selected Core SpatialKnowledge omission overlaps a host owner-section identity.";
                    return false;
                }
                recomposed.Add(new OwnerSectionCensusSnapshot(
                    witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                    witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision));
            }
            recomposed.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
            ownerSections = Array.AsReadOnly(recomposed.ToArray());
            return true;
        }

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

    /// <summary>
    /// Composes the four selected City owner families from the exact Core
    /// owners. Matching selected rows may have been omitted after exact host
    /// congruence checking; stale or mixed-roster full vectors retain the
    /// established identity/value and compatibility behavior.
    /// </summary>
    internal static bool TryComposeCoreOwnedCitySections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
        => TryComposeCoreOwnedCitySections(
            graph, hostSections, requireCompleteSnapshot: true, out ownerSections, out diagnostic);

    internal static bool TryAddCoreOwnedCitySections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
        => TryComposeCoreOwnedCitySections(
            graph, hostSections, requireCompleteSnapshot: false, out ownerSections, out diagnostic);

    private static bool TryComposeCoreOwnedCitySections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        bool requireCompleteSnapshot,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (graph == null || hostSections == null
            || !TryCreateCoreOwnedCityCensusWitnesses(
                graph, out IReadOnlyList<OwnerSectionCensusWitness> witnesses, out diagnostic))
            return false;

        HashSet<string> selectedCitySectionIds =
            new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusWitness witness in witnesses)
            if (witness == null || string.IsNullOrWhiteSpace(witness.SectionId)
                || !selectedCitySectionIds.Add(witness.SectionId))
            {
                diagnostic = "The selected Core City census contains a duplicate section identity.";
                return false;
            }

        HashSet<string> hostSectionIds = new HashSet<string>(StringComparer.Ordinal);
        int selectedRowsPresent = 0;
        foreach (OwnerSectionCensusSnapshot section in hostSections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !hostSectionIds.Add(section.SectionId))
            {
                diagnostic = "The host census remainder contains a missing or duplicate owner-section identity.";
                return false;
            }
            if (selectedCitySectionIds.Contains(section.SectionId)) selectedRowsPresent++;
        }

        if (selectedRowsPresent == selectedCitySectionIds.Count)
        {
            // Compatibility hosts still returning the selected rows retain
            // the original exact-instance and stale-evidence classification.
            return TryRebindCoreOwnedCitySections(
                graph, hostSections, out ownerSections, out diagnostic);
        }
        if (selectedRowsPresent != 0)
        {
            diagnostic = "The host census partially omitted the selected Core City owner families.";
            return false;
        }

        List<OwnerSectionCensusSnapshot> composed =
            new List<OwnerSectionCensusSnapshot>(hostSections.Count + witnesses.Count);
        composed.AddRange(hostSections);
        foreach (OwnerSectionCensusWitness witness in witnesses)
            composed.Add(new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision));
        composed.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));

        if (requireCompleteSnapshot
            && !DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(composed))
        {
            diagnostic = "The Core and host City census sections do not form the complete selected owner vector.";
            return false;
        }

        ownerSections = Array.AsReadOnly(composed.ToArray());
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

        if (!TryCreateCoreOwnedCityCensusWitnesses(
                graph, out IReadOnlyList<OwnerSectionCensusWitness> witnesses, out diagnostic))
            return false;

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

    private static bool TryCreateCoreOwnedCityCensusWitnesses(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<OwnerSectionCensusWitness> witnesses,
        out string diagnostic)
    {
        witnesses = null;
        diagnostic = null;
        if (graph?.Cities == null || graph.Definitions == null)
        {
            diagnostic = "The selected Core City census requires its exact definitions and City owners.";
            return false;
        }

        List<OwnerSectionCensusWitness> built =
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
                    built.Add(witness);
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

        witnesses = built.AsReadOnly();
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
    /// execution state. Core first binds its exact selected owner graph; the
    /// host then binds remaining non-graph authorities and runtime projections,
    /// and supplies the complete owner census. Core owns revalidation,
    /// restored-token creation, and factual-read publication.
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
            || AdvanceLeaseHeld
            || !TryAssessRestoredBoundaryQuiescence()
            || !mutationGuard.CanMutate
            || !runtimeIdentityBound
            || !runtimeConfigurationBound
            || CoreMutationGuardAuthoritiesBound)
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.BoundaryAdmissionFailed;
            diagnostic = "The private restored candidate is not at its exact unadmitted Core owner-thread boundary.";
            return false;
        }

        if (!TryBindSelectedCoreOwnerMutationGuardAuthorities(out diagnostic)
            || !hostPorts.TryBindRestoredCandidateMutationGuard(out diagnostic)
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

        if (!IsFactualReadWorldPublished
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
        if (!TryCaptureUnadmittedRestoredOwnerSectionCensus(
                hostPorts, out ownerSections, out mutationEpoch, out string captureDiagnostic))
        {
            diagnostic = "The private target did not produce a complete quiescent owner vector: "
                + (string.IsNullOrWhiteSpace(captureDiagnostic) ? "OwnerCoverageIncomplete" : captureDiagnostic) + ".";
            return false;
        }

        DailyV1JusticeRecordsOwner justiceOwner = selectedRuntimeOwnerGraph?.JusticeRecordsOwner;
        if (!hostPorts.TryGetExactJusticeRecordsCensusProjectionIdentity(
                justiceOwner, out object justiceProjectionIdentity, out long p18ReceiptRevision)
            || !DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedDailyOwnerVector(
                selectedRuntimeOwnerGraph,
                selectedActorChoiceStore,
                ownerSections,
                justiceProjectionIdentity,
                p18ReceiptRevision,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeOwnerSections,
                out diagnostic))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The Core restored boundary could not compose its exact complete owner vector.";
            return false;
        }

        ownerSections = completeOwnerSections;
        return true;
    }
}
