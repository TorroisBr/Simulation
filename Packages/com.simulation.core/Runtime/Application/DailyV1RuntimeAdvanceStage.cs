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
/// Installs the existing Core operation catalogue for an exact selected
/// application session. The catalogue is derived from live Core capabilities;
/// it does not synthesize host operations or a competing registry.
/// </summary>
internal static class DailyV1CoreRuntimeProtocolOperationInventoryComposition
{
    internal static bool TryInstallForApplicationStart(
        DailyV1ActiveRuntimeSession session,
        out string diagnostic)
    {
        diagnostic = null;
        DailyV1ExecutionBoundaryState state = session?.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = session?.OwnerGraph;
        if (session == null || state == null || graph == null
            || !ReferenceEquals(state.SelectedRuntimeOwnerGraph, graph)
            || !ReferenceEquals(graph.Definitions, session.Definitions)
            || !graph.MatchesExactOwnerInstances(graph.Cities, graph.Npcs)
            || !state.HasAdmittedSelectedDailyExecution
            || !state.SelectedRuntimeExecutionRootsBound
            || state.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !state.AdmissionContext.IsOwnedByCurrentThread())
        {
            diagnostic = "The Core runtime-operation inventory requires the exact admitted selected session on its owner thread.";
            return false;
        }

        IReadOnlyList<string> expected =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true,
                hasTravelPartySystem: graph.TravelParties != null,
                hasTravelSystem: graph.SpatialRoutePlanningSystem != null);

        if (state.HasRuntimeProtocolOperationInventory)
        {
            if (HasExactInventory(state.GetRuntimeProtocolOperationContractIds(), expected))
                return true;
            diagnostic = "The sealed Core runtime-operation inventory differs from the exact selected session capabilities.";
            return false;
        }

        if (state.HasRegisteredRuntimeProtocolOperationContracts)
        {
            if (!HasExactInventory(state.GetRuntimeProtocolOperationContractIds(), expected)
                || !state.TrySealRuntimeProtocolOperationInventory())
            {
                diagnostic = "The existing Core runtime-operation registrations are partial or differ from the selected session capabilities.";
                return false;
            }
            return true;
        }

        if (!state.TryInstallRuntimeProtocolOperationInventory(expected)
            || !state.HasRuntimeProtocolOperationInventory
            || !HasExactInventory(state.GetRuntimeProtocolOperationContractIds(), expected))
        {
            diagnostic = "Core could not atomically install the complete supported runtime-operation inventory for this selected session.";
            return false;
        }
        return true;
    }

    private static bool HasExactInventory(
        IReadOnlyList<string> actual,
        IReadOnlyList<string> expected)
    {
        if (actual == null || expected == null || actual.Count != expected.Count)
            return false;

        HashSet<string> supplied = new HashSet<string>(StringComparer.Ordinal);
        foreach (string operationContractId in actual)
            if (string.IsNullOrWhiteSpace(operationContractId)
                || !supplied.Add(operationContractId))
                return false;

        foreach (string operationContractId in expected)
            if (!supplied.Contains(operationContractId))
                return false;
        return true;
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
/// Host-owned projection and compatibility ports for a selected Daily-v1
/// session. The complete selected owner census is read and validated by the
/// Core execution state's sealed provider registry; the capture methods below
/// remain only for unselected/legacy host paths. During restoration Core binds
/// its selected owner authorities before asking the host to bind runtime
/// projections. This intentionally excludes clocks, whole-day advancement,
/// and intraday timeline selection.
/// </summary>
internal interface IDailyV1SelectedRuntimeAdvanceHostPorts
{
    DailyV1ExecutionBoundaryState ExecutionState { get; }
    // Stable identity for a legacy/unselected host census reader. Selected
    // Daily-v1 identity is the Core execution state and uses its sealed registry.
    object SelectedRuntimeOwnerCensusSourceIdentity { get; }
    bool TryGetExactJusticeRecordsCensusProjectionIdentity(
        DailyV1JusticeRecordsOwner owner,
        out object projectionIdentity,
        out long p18ReceiptRevision);
    bool TryCreateSelectedDailyExecutionHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDailyExecutionHostPorts hostPorts);
    /// <summary>
    /// Captures a legacy/unselected host-owned census source. A selected
    /// Daily-v1 session must use the sealed registry in its Core execution
    /// state and does not call this method for capture.
    /// </summary>
    bool TryCaptureOwnerSectionCensus(
        DailyV1CaptureScopeAllowance allowedOuterScope,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure);
    bool TryBindRestoredCandidateMutationGuard(out string diagnostic);
    /// <summary>
    /// Captures a legacy/unselected private target source. A selected restored
    /// Daily-v1 session is recaptured through the same sealed Core registry
    /// used for live capture.
    /// </summary>
    bool TryCaptureUnadmittedRestoredDailyOwnerVector(
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out string diagnostic);
}

/// <summary>
/// Core-owned sealed census binding. The provider is a live source adapter;
/// the section contract and last committed witness are stored with the Core
/// execution state and participate in the same roster transition transaction.
/// </summary>
internal sealed class DailyV1OwnerSectionCensusProviderRegistration
{
    internal DailyV1OwnerSectionCensusProviderRegistration(
        string sectionId,
        int schemaVersion,
        OwnerSectionRole role,
        IOwnerSectionCensusProvider provider,
        object baselineOwnerInstanceIdentity = null,
        int baselineCardinality = 0,
        long baselineRevision = 0L,
        bool hasBaseline = false)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
            throw new ArgumentException("The owner census section identity is required.", nameof(sectionId));
        if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        if (!Enum.IsDefined(typeof(OwnerSectionRole), role))
            throw new ArgumentOutOfRangeException(nameof(role));
        if (baselineCardinality < 0 || baselineRevision < 0L
            || (hasBaseline && baselineOwnerInstanceIdentity == null))
            throw new ArgumentOutOfRangeException(nameof(baselineCardinality));
        SectionId = sectionId;
        SchemaVersion = schemaVersion;
        Role = role;
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        BaselineOwnerInstanceIdentity = baselineOwnerInstanceIdentity;
        BaselineCardinality = baselineCardinality;
        BaselineRevision = baselineRevision;
        HasBaseline = hasBaseline;
    }

    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal OwnerSectionRole Role { get; }
    internal IOwnerSectionCensusProvider Provider { get; }
    internal object BaselineOwnerInstanceIdentity { get; }
    internal int BaselineCardinality { get; }
    internal long BaselineRevision { get; }
    internal bool HasBaseline { get; }
}
/// <summary>
/// Core reads its sealed provider registry as part of the capture transaction.
/// Owner-thread, quiescence, full 89-family shape, dynamic-key, shared-epoch,
/// and committed-baseline checks are performed here. The host supplies no
/// second owner vector.
/// </summary>
internal static class DailyV1CoreOwnerSectionCensusCapture
{
    internal static bool TryCapture(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1CaptureScopeAllowance allowedOuterScope,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure)
    {
        ownerSections = null;
        mutationEpoch = 0L;
        failure = DailyCaptureEligibilityFailure.RuntimeFaulted;
        if (executionState == null
            || !Enum.IsDefined(typeof(DailyV1CaptureScopeAllowance), allowedOuterScope))
            return false;

        if (!executionState.TryGetSelectedOwnerSectionCensusRegistry(
                allowedOuterScope,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
                out long registryRevision,
                out long startingEpoch,
                out failure))
            return false;
        if (registrations == null || registrations.Count == 0)
        {
            executionState.MarkSelectedRuntimeCensusProtocolFaulted();
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }

        List<OwnerSectionCensusSnapshot> captured =
            new List<OwnerSectionCensusSnapshot>(registrations.Count);
        HashSet<string> registeredIds = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (DailyV1OwnerSectionCensusProviderRegistration registration in registrations)
            {
                if (registration == null
                    || string.IsNullOrWhiteSpace(registration.SectionId)
                    || registration.SchemaVersion <= 0
                    || !Enum.IsDefined(typeof(OwnerSectionRole), registration.Role)
                    || registration.Provider == null
                    || !registration.HasBaseline
                    || registration.BaselineOwnerInstanceIdentity == null
                    || !registeredIds.Add(registration.SectionId))
                {
                    executionState.MarkSelectedRuntimeCensusProtocolFaulted();
                    failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                    return false;
                }

                OwnerSectionCensusWitness witness = registration.Provider.GetCurrentCensus();
                if (witness == null
                    || !string.Equals(witness.SectionId, registration.SectionId, StringComparison.Ordinal)
                    || witness.SchemaVersion != registration.SchemaVersion
                    || witness.OwnerInstanceIdentity == null
                    || witness.Cardinality < 0
                    || witness.Revision < 0L
                    || !ReferenceEquals(witness.OwnerInstanceIdentity,
                        registration.BaselineOwnerInstanceIdentity)
                    || witness.Cardinality != registration.BaselineCardinality
                    || witness.Revision != registration.BaselineRevision)
                {
                    executionState.MarkSelectedRuntimeCensusProtocolFaulted();
                    failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                    return false;
                }

                captured.Add(new OwnerSectionCensusSnapshot(
                    registration.SectionId,
                    registration.SchemaVersion,
                    registration.Role,
                    witness.OwnerInstanceIdentity,
                    witness.Cardinality,
                    witness.Revision));
            }
        }
        catch
        {
            executionState.MarkSelectedRuntimeCensusProtocolFaulted();
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }

        IReadOnlyList<OwnerSectionCensusSnapshot> immutableCapture =
            Array.AsReadOnly(captured.ToArray());
        if (!DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
                immutableCapture))
        {
            executionState.MarkSelectedRuntimeCensusProtocolFaulted();
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }
        if (!executionState.TryValidateSelectedOwnerSectionCensusCapture(
                allowedOuterScope, registryRevision, startingEpoch, out failure))
            return false;

        ownerSections = immutableCapture;
        mutationEpoch = startingEpoch;
        failure = DailyCaptureEligibilityFailure.None;
        return true;
    }
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
    private sealed class P12OwnerSectionCensusProvider<TOwner> : IOwnerSectionCensusProvider
        where TOwner : class
    {
        private readonly string sectionId;
        private readonly int schemaVersion;
        private readonly TOwner owner;
        private readonly Func<TOwner, int> cardinality;
        private readonly Func<TOwner, long> revision;

        internal P12OwnerSectionCensusProvider(
            string sectionId,
            int schemaVersion,
            TOwner owner,
            Func<TOwner, int> cardinality,
            Func<TOwner, long> revision)
        {
            this.sectionId = sectionId ?? throw new ArgumentNullException(nameof(sectionId));
            if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            this.schemaVersion = schemaVersion;
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.cardinality = cardinality ?? throw new ArgumentNullException(nameof(cardinality));
            this.revision = revision ?? throw new ArgumentNullException(nameof(revision));
        }

        public OwnerSectionCensusWitness GetCurrentCensus() =>
            new OwnerSectionCensusWitness(sectionId, schemaVersion, owner,
                cardinality(owner), revision(owner));
    }

    private sealed class CityPopulationOperationReceiptsCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly string sectionId;
        private readonly SettlementPopulationRuntime owner;

        internal CityPopulationOperationReceiptsCensusProvider(
            string sectionId,
            SettlementPopulationRuntime owner)
        {
            this.sectionId = sectionId ?? throw new ArgumentNullException(nameof(sectionId));
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            owner.GetOperationReceiptCensus(out int cardinality, out long revision);
            return new OwnerSectionCensusWitness(
                sectionId,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                owner,
                cardinality,
                revision);
        }
    }

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
        PersonSpatialPositionCensusProvider.SectionId,
        SpatialRouteObservationCensusProvider.SectionId,
        PersonRoutePlanHistoryCensusProvider.SectionId
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
        SpatialRouteObservationCensusProvider.SectionId,
        PersonRoutePlanHistoryCensusProvider.SectionId,
        DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
        DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
        ActorChoiceP11CensusProvider.SectionId,
        ScheduledDirectiveCensusProvider.SectionId,
        TravelPartyCensusProvider.SectionId,
        ExpeditionCensusProvider.SectionId,
        PoliticalKnowledgeStoreCensusProvider.SectionId,
        PoliticalDecisionStoreCensusProvider.SectionId,
        SimulationRecordSequenceCensusProvider.SectionId,
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

        if (!TryRebindCoreOwnedRecordSequenceSection(
                graph,
                copiedHostSections,
                out _,
                out diagnostic)
            || !hostBySectionId.ContainsKey(SimulationRecordSequenceCensusProvider.SectionId))
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The host census differs from the exact Core SimulationRecordSequence owner.";
            return false;
        }

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
            || !TryRebindCoreOwnedNpcRuntimeStateSections(
                graph, completeCoreP12FSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeCoreNpcRuntimeStateRows,
                out diagnostic)
            || !TryRemoveCoreOwnedSelectedRuntimeSections(
                completeCoreNpcRuntimeStateRows, graph,
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
            || !TryRebindCoreOwnedNpcMerchantTradeStateReceiptSections(
                graph, spatialKnowledgeSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> npcMerchantReceiptSections,
                out diagnostic)
            || !TryComposeCoreOwnedCitySections(
                graph, npcMerchantReceiptSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeCitySections,
                out diagnostic)
            || !TryRebindCoreOwnedP12EOwnerSections(
                graph, completeCitySections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeP12EOwnerSections,
                out diagnostic)
            || !TryRebindCoreOwnedRecordSequenceSection(
                graph, completeP12EOwnerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeRecordSequenceSections,
                out diagnostic)
            || !DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
                completeRecordSequenceSections))
        {
            completeOwnerSections = null;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The composed selected Daily-v1 owner vector is incomplete.";
            return false;
        }

        completeOwnerSections = completeRecordSequenceSections;
        return true;
    }

    /// <summary>
    /// Re-reads the record-sequence census from the exact owner bound to the
    /// Core graph and replaces the source row only after its identity, schema,
    /// cardinality, and revision agree. The row remains in the complete
    /// selected P12 vector, but its final facts come from Core's execution root.
    /// </summary>
    internal static bool TryRebindCoreOwnedRecordSequenceSection(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> sourceSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> composedSections,
        out string diagnostic)
    {
        composedSections = null;
        diagnostic = null;
        SimulationRecordSequence sequence = graph?.RecordSequence;
        if (sequence == null || sourceSections == null)
        {
            diagnostic = "The selected Core record-sequence census requires its exact graph-bound owner and complete source vector.";
            return false;
        }

        OwnerSectionCensusWitness witness;
        try
        {
            witness = new SimulationRecordSequenceCensusProvider(sequence).GetCurrentCensus();
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException)
        {
            diagnostic = "The Core record-sequence census provider could not read its exact owner.";
            return false;
        }

        if (witness == null
            || !string.Equals(witness.SectionId, SimulationRecordSequenceCensusProvider.SectionId,
                StringComparison.Ordinal)
            || witness.SchemaVersion != SimulationRecordSequenceCensusProvider.SchemaVersion
            || witness.Cardinality != 1
            || witness.Revision < 0L
            || !SimulationRecordSequenceOwnerBridge.TryMatchCurrentCensus(
                sequence, witness.OwnerInstanceIdentity, witness.Revision, out _))
        {
            diagnostic = "The Core record-sequence provider returned an invalid selected owner witness.";
            return false;
        }

        List<OwnerSectionCensusSnapshot> composed = new List<OwnerSectionCensusSnapshot>(sourceSections.Count + 1);
        int matches = 0;
        for (int index = 0; index < sourceSections.Count; index++)
        {
            OwnerSectionCensusSnapshot section = sourceSections[index];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId))
            {
                diagnostic = "The complete source census contains an invalid owner-section row.";
                return false;
            }
            if (!string.Equals(section.SectionId, witness.SectionId, StringComparison.Ordinal))
            {
                composed.Add(section);
                continue;
            }

            matches++;
            if (section.Role != OwnerSectionRole.Required
                || section.SchemaVersion != witness.SchemaVersion
                || !ReferenceEquals(section.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                || section.Cardinality != witness.Cardinality
                || section.Revision != witness.Revision)
            {
                diagnostic = "The selected source census differs from the exact Core record-sequence owner.";
                return false;
            }
            composed.Add(new OwnerSectionCensusSnapshot(
                witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision));
        }

        if (matches > 1)
        {
            diagnostic = "The source census must not duplicate the record-sequence family.";
            return false;
        }

        if (matches == 0)
        {
            OwnerSectionCensusSnapshot coreSection = new OwnerSectionCensusSnapshot(
                witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision);
            int insertionIndex = composed.FindIndex(section =>
                StringComparer.Ordinal.Compare(section.SectionId, witness.SectionId) > 0);
            if (insertionIndex < 0)
                composed.Add(coreSection);
            else
                composed.Insert(insertionIndex, coreSection);
        }

        composedSections = composed.AsReadOnly();
        return true;
    }

    /// <summary>
    /// Rebinds the fixed P12-E rows to the exact owner instances retained by
    /// the selected Core graph. A host may retain every row or omit the whole
    /// Core-owned subset; partial omission, stale revisions, and parallel
    /// owner instances are rejected.
    /// </summary>
    internal static bool TryRebindCoreOwnedP12EOwnerSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> composedSections,
        out string diagnostic)
    {
        return TryRebindCoreOwnedP12EOwnerSectionsCore(
            graph, hostSections, includeProviderRegistrations: false,
            out composedSections, out _, out diagnostic);
    }

    internal static bool TryRebindCoreOwnedP12EOwnerSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> composedSections,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> providerRegistrations,
        out string diagnostic)
    {
        return TryRebindCoreOwnedP12EOwnerSectionsCore(
            graph, hostSections, includeProviderRegistrations: true,
            out composedSections, out providerRegistrations, out diagnostic);
    }

    private static bool TryRebindCoreOwnedP12EOwnerSectionsCore(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        bool includeProviderRegistrations,
        out IReadOnlyList<OwnerSectionCensusSnapshot> composedSections,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> providerRegistrations,
        out string diagnostic)
    {
        composedSections = null;
        providerRegistrations = null;
        diagnostic = null;
        if (graph == null || hostSections == null)
        {
            diagnostic = "The Core P12-E census requires its exact owner graph and host census vector.";
            return false;
        }

        P12ECoreOwnerRestoreStage owners = graph.P12EOwners;
        if (owners == null || owners.CrimeSocialAppraisal == null)
        {
            OwnerSectionCensusSnapshot[] unchanged = new OwnerSectionCensusSnapshot[hostSections.Count];
            for (int i = 0; i < unchanged.Length; i++) unchanged[i] = hostSections[i];
            composedSections = Array.AsReadOnly(unchanged);
            if (includeProviderRegistrations)
                providerRegistrations = Array.Empty<DailyV1OwnerSectionCensusProviderRegistration>();
            return true;
        }

        List<OwnerSectionCensusSnapshot> expected = new List<OwnerSectionCensusSnapshot>(23);
        List<DailyV1OwnerSectionCensusProviderRegistration> builtRegistrations =
            includeProviderRegistrations
                ? new List<DailyV1OwnerSectionCensusProviderRegistration>(23)
                : null;
        IReadOnlyList<IOwnerSectionCensusProvider> institutionOffice =
            InstitutionOfficeCensusProvider.CreateProviders(owners.Institutions, owners.Offices);
        if (!TryAppendCoreP12EOwnerProviders(expected, builtRegistrations, institutionOffice,
                new[]
                {
                    InstitutionOfficeCensusProvider.InstitutionsSectionId,
                    InstitutionOfficeCensusProvider.OfficesSectionId,
                    InstitutionOfficeCensusProvider.IncumbenciesSectionId,
                    InstitutionOfficeCensusProvider.TenuresSectionId
                },
                new[]
                {
                    InstitutionOfficeCensusProvider.SchemaVersion,
                    InstitutionOfficeCensusProvider.SchemaVersion,
                    InstitutionOfficeCensusProvider.SchemaVersion,
                    InstitutionOfficeCensusProvider.SchemaVersion
                },
                new object[] { owners.Institutions, owners.Offices, owners.Offices, owners.Offices },
                out diagnostic))
            return false;

        IReadOnlyList<IOwnerSectionCensusProvider> propertyProviders =
            PropertyOwnershipCensusProvider.CreateProviders(owners.PropertyOwnership);
        IOwnerSectionCensusProvider[] propertyEstate =
        {
            propertyProviders != null && propertyProviders.Count > 0 ? propertyProviders[0] : null,
            propertyProviders != null && propertyProviders.Count > 1 ? propertyProviders[1] : null,
            new EstateCensusProvider(owners.Estates)
        };
        if (!TryAppendCoreP12EOwnerProviders(expected, builtRegistrations, propertyEstate,
                new[]
                {
                    PropertyOwnershipCensusProvider.OwnershipSectionId,
                    PropertyOwnershipCensusProvider.TransferHistorySectionId,
                    EstateCensusProvider.SectionId
                },
                new[]
                {
                    PropertyOwnershipCensusProvider.SchemaVersion,
                    PropertyOwnershipCensusProvider.SchemaVersion,
                    EstateCensusProvider.SchemaVersion
                },
                new object[] { owners.PropertyOwnership, owners.PropertyOwnership, owners.Estates },
                out diagnostic))
            return false;

        if (!TryAppendCoreP12EOwnerProviders(expected, builtRegistrations,
                FactionStoreCensusProvider.CreateProviders(owners.Factions),
                new[] { FactionStoreCensusProvider.FactionsSectionId, FactionStoreCensusProvider.AffiliationsSectionId },
                new[] { FactionStoreCensusProvider.SchemaVersion, FactionStoreCensusProvider.SchemaVersion },
                new object[] { owners.Factions, owners.Factions }, out diagnostic)
            || !TryAppendCoreP12EOwnerProviders(expected, builtRegistrations,
                PoliticalClaimStoreCensusProvider.CreateProviders(owners.PoliticalClaims),
                new[] { PoliticalClaimStoreCensusProvider.ClaimsSectionId, PoliticalClaimStoreCensusProvider.RecognitionsSectionId },
                new[] { PoliticalClaimStoreCensusProvider.SchemaVersion, PoliticalClaimStoreCensusProvider.SchemaVersion },
                new object[] { owners.PoliticalClaims, owners.PoliticalClaims }, out diagnostic)
            || !TryAppendCoreP12EOwnerProviders(expected, builtRegistrations,
                PoliticalSupportStoreCensusProvider.CreateProviders(owners.PoliticalSupport),
                new[] { PoliticalSupportStoreCensusProvider.RelationsSectionId },
                new[] { PoliticalSupportStoreCensusProvider.SchemaVersion },
                new object[] { owners.PoliticalSupport }, out diagnostic)
            || !TryAppendCoreP12EOwnerProviders(expected, builtRegistrations,
                new IOwnerSectionCensusProvider[]
                {
                    new P12OwnerSectionCensusProvider<TheftOutcomeStore>(
                        P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId,
                        P12CrimeSocialAppraisalOwnerContract.SchemaVersion,
                        owners.CrimeSocialAppraisal.TheftOutcomes,
                        owner => owner.Count,
                        owner => owner.P12CensusRevision),
                    new P12OwnerSectionCensusProvider<CrimeKnowledgeStore>(
                        P12CrimeSocialAppraisalOwnerContract.KnowledgeSectionId,
                        P12CrimeSocialAppraisalOwnerContract.SchemaVersion,
                        owners.CrimeSocialAppraisal.CrimeKnowledge,
                        owner => owner.Count,
                        owner => owner.P12CensusRevision),
                    new P12OwnerSectionCensusProvider<SocialReactionStore>(
                        P12CrimeSocialAppraisalOwnerContract.ReactionsSectionId,
                        P12CrimeSocialAppraisalOwnerContract.SchemaVersion,
                        owners.CrimeSocialAppraisal.SocialReactions,
                        owner => owner.Count,
                        owner => owner.P12CensusRevision)
                },
                new[]
                {
                    P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId,
                    P12CrimeSocialAppraisalOwnerContract.KnowledgeSectionId,
                    P12CrimeSocialAppraisalOwnerContract.ReactionsSectionId
                },
                new[]
                {
                    P12CrimeSocialAppraisalOwnerContract.SchemaVersion,
                    P12CrimeSocialAppraisalOwnerContract.SchemaVersion,
                    P12CrimeSocialAppraisalOwnerContract.SchemaVersion
                },
                new object[]
                {
                    owners.CrimeSocialAppraisal.TheftOutcomes,
                    owners.CrimeSocialAppraisal.CrimeKnowledge,
                    owners.CrimeSocialAppraisal.SocialReactions
                }, out diagnostic)
            || !TryAppendCoreP12EOwnerProviders(expected, builtRegistrations,
                ArmedForceStoreCensusProvider.CreateProviders(owners.ArmedForces),
                new[]
                {
                    ArmedForceStoreCensusProvider.ForcesSectionId,
                    ArmedForceStoreCensusProvider.ContingentsSectionId,
                    ArmedForceStoreCensusProvider.RelevantPersonReferencesSectionId
                },
                new[]
                {
                    ArmedForceStoreCensusProvider.SchemaVersion,
                    ArmedForceStoreCensusProvider.SchemaVersion,
                    ArmedForceStoreCensusProvider.SchemaVersion
                },
                new object[] { owners.ArmedForces, owners.ArmedForces, owners.ArmedForces }, out diagnostic)
            || !TryAppendCoreP12EOwnerProviders(expected, builtRegistrations,
                new[] { (IOwnerSectionCensusProvider)new ContingentManpowerCensusProvider(owners.Manpower) },
                new[] { ContingentManpowerCensusProvider.SectionId },
                new[] { ContingentManpowerCensusProvider.SchemaVersion },
                new object[] { owners.Manpower }, out diagnostic)
            || !TryAppendCoreP12EOwnerProviders(expected, builtRegistrations,
                new[] { (IOwnerSectionCensusProvider)new ArmedForceSpatialCensusProvider(owners.Positions) },
                new[] { ArmedForceSpatialCensusProvider.SectionId },
                new[] { ArmedForceSpatialCensusProvider.SchemaVersion },
                new object[] { owners.Positions }, out diagnostic)
            || !TryAppendCoreP12EOwnerProviders(expected, builtRegistrations,
                new[] { (IOwnerSectionCensusProvider)new PersistentConflictCensusProvider(owners.Conflicts) },
                new[] { PersistentConflictCensusProvider.SectionId },
                new[] { PersistentConflictCensusProvider.SchemaVersion },
                new object[] { owners.Conflicts }, out diagnostic)
            || !TryAppendCoreP12EOwnerProviders(expected, builtRegistrations,
                new[] { (IOwnerSectionCensusProvider)new PersistentWarCensusProvider(owners.Wars) },
                new[] { PersistentWarCensusProvider.SectionId },
                new[] { PersistentWarCensusProvider.SchemaVersion },
                new object[] { owners.Wars }, out diagnostic)
            || !TryAppendCoreP12EOwnerProviders(expected, builtRegistrations,
                new[] { (IOwnerSectionCensusProvider)new PersistentBattleCensusProvider(owners.Battles) },
                new[] { PersistentBattleCensusProvider.SectionId },
                new[] { PersistentBattleCensusProvider.SchemaVersion },
                new object[] { owners.Battles }, out diagnostic))
            return false;

        HashSet<string> expectedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in expected)
            if (!expectedIds.Add(section.SectionId))
            {
                diagnostic = "The Core P12-E provider set contains a duplicate section identity.";
                return false;
            }

        Dictionary<string, OwnerSectionCensusSnapshot> hostById =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in hostSections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || hostById.ContainsKey(section.SectionId))
            {
                diagnostic = "The host P12-E vector contains a missing or duplicate section identity.";
                return false;
            }
            hostById.Add(section.SectionId, section);
        }

        int present = 0;
        foreach (string sectionId in expectedIds)
            if (hostById.ContainsKey(sectionId)) present++;
        if (present != 0 && present != expected.Count)
        {
            diagnostic = "The host vector partially omitted the Core-owned P12-E owner set.";
            return false;
        }

        foreach (OwnerSectionCensusSnapshot wanted in expected)
            if (present != 0
                && (!hostById.TryGetValue(wanted.SectionId, out OwnerSectionCensusSnapshot actual)
                    || !HasExactOwnerSectionFacts(actual, wanted)))
            {
                diagnostic = "The host vector differs from the exact Core P12-E owner at "
                    + wanted.SectionId + ".";
                return false;
            }

        List<OwnerSectionCensusSnapshot> composed =
            new List<OwnerSectionCensusSnapshot>(hostSections.Count + (present == 0 ? expected.Count : 0));
        composed.AddRange(hostSections);
        if (present == 0) composed.AddRange(expected);
        composed.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        composedSections = Array.AsReadOnly(composed.ToArray());
        if (includeProviderRegistrations)
        {
            builtRegistrations.Sort(
                (left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
            providerRegistrations = builtRegistrations.AsReadOnly();
        }
        return true;
    }

    private static bool TryAppendCoreP12EOwnerProviders(
        List<OwnerSectionCensusSnapshot> target,
        List<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        IReadOnlyList<string> sectionIds,
        IReadOnlyList<int> schemaVersions,
        IReadOnlyList<object> ownerIdentities,
        out string diagnostic)
    {
        diagnostic = null;
        if (target == null || providers == null || sectionIds == null
            || schemaVersions == null || ownerIdentities == null
            || providers.Count == 0 || providers.Count != sectionIds.Count
            || providers.Count != schemaVersions.Count || providers.Count != ownerIdentities.Count)
        {
            diagnostic = "The Core P12-E census provider set is incomplete.";
            return false;
        }

        try
        {
            for (int i = 0; i < providers.Count; i++)
            {
                OwnerSectionCensusWitness witness = providers[i]?.GetCurrentCensus();
                if (witness == null
                    || !string.Equals(witness.SectionId, sectionIds[i], StringComparison.Ordinal)
                    || witness.SchemaVersion != schemaVersions[i]
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, ownerIdentities[i])
                    || witness.Cardinality < 0 || witness.Revision < 0L)
                {
                    diagnostic = "A Core P12-E provider returned stale or mismatched owner facts for "
                        + sectionIds[i] + ".";
                    return false;
                }

                target.Add(new OwnerSectionCensusSnapshot(
                    witness.SectionId,
                    witness.SchemaVersion,
                    OwnerSectionRole.Required,
                    witness.OwnerInstanceIdentity,
                    witness.Cardinality,
                    witness.Revision));
                if (registrations != null)
                    registrations.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                        witness.SectionId,
                        witness.SchemaVersion,
                        OwnerSectionRole.Required,
                        providers[i],
                        witness.OwnerInstanceIdentity,
                        witness.Cardinality,
                        witness.Revision,
                        hasBaseline: true));
            }
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "A Core P12-E census provider failed while reading its selected owner facts.";
            return false;
        }
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
        SpatialRouteKnowledgeStore routeKnowledge = graph?.SpatialRouteKnowledgeStore;
        PersonRoutePlanStore routePlans = graph?.PersonRoutePlanStore;
        if (persons == null)
        {
            diagnostic = "The Core identity and spatial census requires its exact PersonStore.";
            return false;
        }

        if ((routeKnowledge == null) != (routePlans == null)
            || !graph.TryValidateP8DTravelOwnerBindings())
        {
            diagnostic = "The Core P8-D census requires the exact paired route owners joined to the selected PersonStore, position store, and clock.";
            return false;
        }
        List<OwnerSectionCensusSnapshot> built =
            new List<OwnerSectionCensusSnapshot>(CoreOwnedIdentitySpatialSectionIdValues.Length);
        int expectedCoreRowCount = (identities == null ? 0 : 8)
            + (spatialNetwork == null ? 0 : 2)
            + (positions == null ? 0 : 6)
            + (anchors == null || positions == null ? 0 : 1)
            + (routeKnowledge == null ? 0 : 1)
            + (routePlans == null ? 0 : 1);
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
            if (routeKnowledge != null && routePlans != null)
            {
                built.Add(ToOwnerSectionSnapshot(
                    new SpatialRouteObservationCensusProvider(routeKnowledge).GetCurrentCensus(),
                    OwnerSectionRole.ExplicitlyEmpty));
                built.Add(ToOwnerSectionSnapshot(
                    new PersonRoutePlanHistoryCensusProvider(routePlans).GetCurrentCensus(),
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

    /// <summary>
    /// Creates live Core providers for the identity/spatial rows already
    /// composed from the exact selected owner graph. Unity may compare its
    /// authoring/runtime projection against these providers during the
    /// compatibility census pass, but the sealed Core registry retains these
    /// Core providers as the source used by later captures.
    /// </summary>
    internal static bool TryCreateCoreOwnedIdentitySpatialCensusProviderRegistrations(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        registrations = null;
        diagnostic = null;
        if (!TryCreateCoreOwnedIdentitySpatialCensusSections(
                graph, out IReadOnlyList<OwnerSectionCensusSnapshot> expected,
                out diagnostic)
            || expected == null)
            return false;

        if (!TryCreateIdentitySpatialCensusReaders(graph,
                out Dictionary<string, Func<OwnerSectionCensusWitness>> readers,
                out diagnostic))
            return false;

        Dictionary<string, OwnerSectionCensusSnapshot> expectedById =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in expected)
        {
            if (section == null || !expectedById.TryAdd(section.SectionId, section))
            {
                diagnostic = "The Core identity/spatial census contains a duplicate expected section.";
                return false;
            }
        }

        if (expectedById.Count != readers.Count)
        {
            diagnostic = "The Core identity/spatial live provider set does not match its exact owner rows.";
            return false;
        }

        List<DailyV1OwnerSectionCensusProviderRegistration> built =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(expected.Count);
        foreach (OwnerSectionCensusSnapshot section in expected)
        {
            if (!readers.TryGetValue(section.SectionId,
                    out Func<OwnerSectionCensusWitness> read)
                || read == null)
            {
                diagnostic = "The Core identity/spatial source has no live reader for "
                    + section.SectionId + ".";
                return false;
            }

            DailyV1CoreIdentitySpatialOwnerSectionCensusProvider provider =
                new DailyV1CoreIdentitySpatialOwnerSectionCensusProvider(
                    section.SectionId, section.SchemaVersion, read);
            OwnerSectionCensusWitness current;
            try
            {
                current = provider.GetCurrentCensus();
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is InvalidOperationException || exception is OverflowException)
            {
                diagnostic = "The Core identity/spatial provider could not read "
                    + section.SectionId + ".";
                return false;
            }

            if (!HasExactIdentitySpatialWitness(current, section))
            {
                diagnostic = "The live Core identity/spatial provider differs from its exact owner row at "
                    + section.SectionId + ".";
                return false;
            }

            built.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                section.SectionId, section.SchemaVersion, section.Role, provider));
        }

        registrations = built.AsReadOnly();
        return true;
    }

    private static bool TryCreateIdentitySpatialCensusReaders(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out Dictionary<string, Func<OwnerSectionCensusWitness>> readers,
        out string diagnostic)
    {
        readers = null;
        diagnostic = null;
        if (graph == null)
        {
            diagnostic = "The Core identity/spatial census requires its exact owner graph.";
            return false;
        }

        Dictionary<string, Func<OwnerSectionCensusWitness>> built =
            new Dictionary<string, Func<OwnerSectionCensusWitness>>(StringComparer.Ordinal);
        string invalidReaderSection = null;
        DailyV1RuntimeIdentityIndex identities = graph.RuntimeIdentityIndex;
        if (identities != null)
        {
            DailyV1RuntimeIdentityKind[] kinds =
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
            foreach (DailyV1RuntimeIdentityKind kind in kinds)
            {
                DailyV1RuntimeIdentityKind capturedKind = kind;
                OwnerSectionCensusWitness identityWitness =
                    identities.GetOwnerSectionCensusWitness(capturedKind);
                Add(identityWitness.SectionId,
                    () => identities.GetOwnerSectionCensusWitness(capturedKind));
            }
        }

        SpatialNetworkOwner network = graph.SpatialNetwork;
        if (network != null)
        {
            Add(P12ASpatialNetworkOwnerCodec.LocationsSectionId,
                () => new OwnerSectionCensusWitness(
                    P12ASpatialNetworkOwnerCodec.LocationsSectionId,
                    P12ASpatialNetworkOwnerCodec.SchemaVersion,
                    network, network.LocationCount, network.Revision));
            Add(P12ASpatialNetworkOwnerCodec.RoutesSectionId,
                () => new OwnerSectionCensusWitness(
                    P12ASpatialNetworkOwnerCodec.RoutesSectionId,
                    P12ASpatialNetworkOwnerCodec.SchemaVersion,
                    network, network.RouteCount, network.Revision));
        }

        PersonSpatialPositionStore positions = graph.PersonSpatialPositionStore;
        SpatialAuthorityStore spatial = positions?.SpatialAuthorityStore;
        if (positions != null && spatial != null && spatial.PassageAuthority != null)
        {
            Add(P12P8AImageSectionContract.HexSectionId,
                () => new OwnerSectionCensusWitness(
                    P12P8AImageSectionContract.HexSectionId,
                    P12P8AImageSectionContract.SchemaVersion,
                    spatial, spatial.HexCount, spatial.Revision));
            Add(P12P8AImageSectionContract.LocationSectionId,
                () => new OwnerSectionCensusWitness(
                    P12P8AImageSectionContract.LocationSectionId,
                    P12P8AImageSectionContract.SchemaVersion,
                    spatial, spatial.LocationCount, spatial.Revision));
            Add(P12P8AImageSectionContract.ScaleContextSectionId,
                () => new OwnerSectionCensusWitness(
                    P12P8AImageSectionContract.ScaleContextSectionId,
                    P12P8AImageSectionContract.SchemaVersion,
                    spatial, spatial.HasGeography ? 1 : 0, spatial.Revision));

            SpatialPassageAuthority passage = spatial.PassageAuthority;
            Add(DailyV1SelectedOwnerSectionCensusContract.SpatialPassageStateSectionId,
                () =>
                {
                    int cardinality;
                    checked { cardinality = passage.Options.Count + passage.Barriers.Count; }
                    return new OwnerSectionCensusWitness(
                        DailyV1SelectedOwnerSectionCensusContract.SpatialPassageStateSectionId,
                        DailyV1SelectedOwnerSectionCensusContract.SpatialPassageStateSchemaVersion,
                        passage, cardinality, spatial.Revision);
                });
            Add(DailyV1SelectedOwnerSectionCensusContract.SpatialCrossingSectionId,
                () => new OwnerSectionCensusWitness(
                    DailyV1SelectedOwnerSectionCensusContract.SpatialCrossingSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.SpatialCrossingSchemaVersion,
                    spatial, spatial.CrossingCount, spatial.Revision));
            PersonSpatialPositionCensusProvider positionProvider =
                new PersonSpatialPositionCensusProvider(positions);
            Add(PersonSpatialPositionCensusProvider.SectionId,
                positionProvider.GetCurrentCensus);
        }

        LegacySpatialAnchorBindingStore anchors = graph.LegacySpatialAnchors;
        if (anchors != null)
        {
            LegacySpatialAnchorBindingCensusProvider provider =
                new LegacySpatialAnchorBindingCensusProvider(anchors);
            Add(LegacySpatialAnchorBindingCensusProvider.SectionId,
                provider.GetCurrentCensus);
        }

        SpatialRouteKnowledgeStore routeKnowledge = graph.SpatialRouteKnowledgeStore;
        if (routeKnowledge != null)
        {
            SpatialRouteObservationCensusProvider provider =
                new SpatialRouteObservationCensusProvider(routeKnowledge);
            Add(SpatialRouteObservationCensusProvider.SectionId,
                provider.GetCurrentCensus);
        }

        PersonRoutePlanStore routePlans = graph.PersonRoutePlanStore;
        if (routePlans != null)
        {
            PersonRoutePlanHistoryCensusProvider provider =
                new PersonRoutePlanHistoryCensusProvider(routePlans);
            Add(PersonRoutePlanHistoryCensusProvider.SectionId,
                provider.GetCurrentCensus);
        }

        if (invalidReaderSection != null)
        {
            diagnostic = "The Core identity/spatial census has an invalid or duplicate live section reader at "
                + invalidReaderSection + ".";
            return false;
        }

        readers = built;
        return true;

        void Add(string sectionId, Func<OwnerSectionCensusWitness> read)
        {
            if (string.IsNullOrWhiteSpace(sectionId) || read == null
                || !built.TryAdd(sectionId, read))
                invalidReaderSection ??= string.IsNullOrWhiteSpace(sectionId) ? "<empty>" : sectionId;
        }
    }

    private static bool HasExactIdentitySpatialWitness(
        OwnerSectionCensusWitness actual,
        OwnerSectionCensusSnapshot expected) =>
        actual != null && expected != null
        && string.Equals(actual.SectionId, expected.SectionId, StringComparison.Ordinal)
        && actual.SchemaVersion == expected.SchemaVersion
        && ReferenceEquals(actual.OwnerInstanceIdentity, expected.OwnerInstanceIdentity)
        && actual.Cardinality == expected.Cardinality
        && actual.Revision == expected.Revision;

    internal static bool IsCoreOwnedIdentitySpatialSection(string sectionId) =>
        !string.IsNullOrEmpty(sectionId)
        && Array.IndexOf(CoreOwnedIdentitySpatialSectionIdValues, sectionId) >= 0;

    private sealed class DailyV1CoreIdentitySpatialOwnerSectionCensusProvider
        : IOwnerSectionCensusProvider
    {
        private readonly string sectionId;
        private readonly int schemaVersion;
        private readonly Func<OwnerSectionCensusWitness> read;

        internal DailyV1CoreIdentitySpatialOwnerSectionCensusProvider(
            string sectionId,
            int schemaVersion,
            Func<OwnerSectionCensusWitness> read)
        {
            this.sectionId = sectionId ?? throw new ArgumentNullException(nameof(sectionId));
            this.schemaVersion = schemaVersion;
            this.read = read ?? throw new ArgumentNullException(nameof(read));
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            OwnerSectionCensusWitness witness = read();
            if (witness == null
                || !string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal)
                || witness.SchemaVersion != schemaVersion)
                throw new InvalidOperationException(
                    "The Core identity/spatial census reader changed its fixed section contract.");
            return witness;
        }
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
    /// Enforces the P12-D receipt boundary: selected LocalObservation and
    /// Merchant receipt rows bind to their exact Core owners. Compatibility
    /// hosts still supply only unselected NPC rows. Both receipt families
    /// remain exact-zero in this profile, without exporting or replaying data.
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
        Dictionary<string, DailyV1NpcRuntimeOwners> selectedNpcOwners =
            new Dictionary<string, DailyV1NpcRuntimeOwners>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || npc.RuntimeId.IndexOf('/') >= 0 || npc.RuntimeId.IndexOf('\0') >= 0
                || !selectedRuntimeIds.Add(npc.RuntimeId)
                || !selectedNpcOwners.TryAdd(npc.RuntimeId, npc))
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
            if (!localRows.TryGetValue(runtimeId, out OwnerSectionCensusSnapshot localRow)
                || !merchantRows.TryGetValue(runtimeId, out OwnerSectionCensusSnapshot merchantRow)
                || !selectedNpcOwners.TryGetValue(runtimeId, out DailyV1NpcRuntimeOwners npcOwner)
                || npcOwner.LocalObservationReceipts == null
                || !npcOwner.LocalObservationReceipts.IsExactEmpty
                || !ReferenceEquals(
                    localRow.OwnerInstanceIdentity,
                    npcOwner.LocalObservationReceipts)
                || npcOwner.MerchantTradeStateReceipts == null
                || !npcOwner.MerchantTradeStateReceipts.IsExactEmpty
                || !ReferenceEquals(
                    merchantRow.OwnerInstanceIdentity,
                    npcOwner.MerchantTradeStateReceipts))
            {
                diagnostic = "The P12-D receipt census is missing the selected NPC host/Core receipt owner pair or its exact-empty Core binding.";
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
    /// Local-observation receipt sections remain host-owned; merchant receipt
    /// sections are rebound to the exact Core owner when the selected roster is complete.
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
                out IReadOnlyList<OwnerSectionCensusSnapshot> spatialKnowledge,
                out diagnostic))
            return false;
        return TryRebindCoreOwnedNpcMerchantTradeStateReceiptSections(
            graph, spatialKnowledge, out ownerSections, out diagnostic);
    }

    internal static bool TryRebindCoreOwnedNpcMerchantTradeStateReceiptSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out string diagnostic)
    {
        ownerSections = null;
        diagnostic = null;
        if (graph?.Npcs == null || hostSections == null)
        {
            diagnostic = "The selected merchant receipt census requires its owner graph and source rows.";
            return false;
        }

        if (graph.Npcs.Count == 0)
        {
            ownerSections = hostSections;
            return true;
        }

        OwnerSectionCensusSnapshot[] copied = new OwnerSectionCensusSnapshot[hostSections.Count];
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < hostSections.Count; index++)
        {
            OwnerSectionCensusSnapshot section = hostSections[index];
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId)
                || !indexes.TryAdd(section.SectionId, index))
            {
                diagnostic = "The selected merchant receipt census contains a missing or duplicate section.";
                return false;
            }
            copied[index] = section;
        }

        int receiptRows = CountFamilyRows(copied,
            DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradeStateReceiptsPrefix);
        if (receiptRows > graph.Npcs.Count)
        {
            // Preserve the complete host facts for mixed NPC rosters. The
            // existing P12 admission path remains responsible for the full vector.
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }
        if (receiptRows != 0 && receiptRows != graph.Npcs.Count)
        {
            diagnostic = "The selected merchant receipt census has partial NPC family coverage.";
            return false;
        }

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try
        {
            providers = DailyV1NpcOwnerCensusProviders
                .CreateMerchantTradeStateReceiptProviders(graph.Npcs);
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException)
        {
            diagnostic = "The selected Core merchant receipt owners could not bind to the exact NPC graph.";
            return false;
        }

        List<OwnerSectionCensusSnapshot> added = null;
        HashSet<string> observed = new HashSet<string>(StringComparer.Ordinal);
        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            OwnerSectionCensusWitness witness;
            try { witness = provider?.GetCurrentCensus(); }
            catch (Exception exception) when (exception is ArgumentException
                || exception is InvalidOperationException || exception is OverflowException)
            {
                diagnostic = "A selected Core merchant receipt owner did not satisfy Daily-v1's exact-empty contract.";
                return false;
            }

            if (witness == null || string.IsNullOrWhiteSpace(witness.SectionId)
                || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || witness.OwnerInstanceIdentity == null || witness.Cardinality != 0
                || witness.Revision != 0L || !observed.Add(witness.SectionId))
            {
                diagnostic = "A selected Core merchant receipt provider returned an invalid exact-empty witness.";
                return false;
            }

            if (!indexes.TryGetValue(witness.SectionId, out int sourceIndex))
            {
                if (receiptRows != 0)
                {
                    diagnostic = "The selected merchant receipt source omitted " + witness.SectionId + ".";
                    return false;
                }
                added ??= new List<OwnerSectionCensusSnapshot>(providers.Count);
                added.Add(new OwnerSectionCensusSnapshot(witness.SectionId,
                    witness.SchemaVersion, OwnerSectionRole.Required,
                    witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision));
                continue;
            }

            OwnerSectionCensusSnapshot source = copied[sourceIndex];
            if (source.Role != OwnerSectionRole.Required
                || source.SchemaVersion != witness.SchemaVersion
                || !ReferenceEquals(source.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                || source.Cardinality != witness.Cardinality
                || source.Revision != witness.Revision)
            {
                diagnostic = "The host merchant receipt row differs from the exact Core owner at "
                    + witness.SectionId + ".";
                return false;
            }

            copied[sourceIndex] = new OwnerSectionCensusSnapshot(witness.SectionId,
                witness.SchemaVersion, OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision);
        }

        if (added == null)
        {
            ownerSections = Array.AsReadOnly(copied);
            return true;
        }

        added.InsertRange(0, copied);
        added.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        ownerSections = Array.AsReadOnly(added.ToArray());
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
        if (!TryRebindCoreOwnedRecordSequenceSection(
                graph,
                composed,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeWithRecordSequence,
                out diagnostic))
            return false;

        // Preserve a structurally complete but stale host alias witness for
        // the existing exact token/image comparison. That later gate applies
        // IsCompleteSnapshot and reports the established stale-boundary
        // failure instead of reclassifying identity drift as missing coverage.
        if (!DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
                completeWithRecordSequence))
        {
            HashSet<string> composedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (OwnerSectionCensusSnapshot section in completeWithRecordSequence)
                if (section != null) composedIds.Add(section.SectionId);
            List<string> missingFixed = new List<string>();
            foreach (string sectionId in DailyV1SelectedOwnerSectionCensusContract.FixedRequired)
                if (!composedIds.Contains(sectionId)) missingFixed.Add(sectionId);
            foreach (string sectionId in DailyV1SelectedOwnerSectionCensusContract.FixedEmpty)
                if (!composedIds.Contains(sectionId)) missingFixed.Add(sectionId);
            int npcCount = CountFamilyRows(
                composed, DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix);
            int cityCount = CountFamilyRows(
                completeWithRecordSequence, DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix);
            int nonPersonNpcCount = CountFamilyRows(
                completeWithRecordSequence, DailyV1SelectedOwnerSectionCensusContract.NpcResidencePrefix);
            int personCount = CountFamilyRows(
                completeWithRecordSequence, DailyV1SelectedOwnerSectionCensusContract.PersonLifeResidencePrefix);
            diagnostic = "The Core and host census sections do not compose the complete selected Daily-v1 owner vector. "
                + "Rows=" + completeWithRecordSequence.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ", NPCs=" + npcCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ", Cities=" + cityCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ", non-Person NPCs=" + nonPersonNpcCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ", Persons=" + personCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ", missing fixed=" + string.Join(",", missingFixed) + ".";
            return false;
        }

        completeOwnerSections = completeWithRecordSequence;
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

                composed ??= new List<OwnerSectionCensusSnapshot>(copied.Length + witnesses.Count);
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
    /// Creates live Core registrations for every selected NPC owner family
    /// currently composed by Core. The registrations bind exact runtime
    /// identities and carry each owner's initial revision/cardinality baseline.
    /// </summary>
    internal static bool TryCreateCoreOwnedSelectedNpcCensusProviderRegistrations(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        registrations = null;
        diagnostic = null;
        if (graph?.Npcs == null)
        {
            diagnostic = "The selected Core NPC census requires its exact owner graph.";
            return false;
        }

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try
        {
            providers = DailyV1NpcOwnerCensusProviders.CreateSelectedCoreOwnerProviders(graph);
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException)
        {
            diagnostic = "The complete Core-owned NPC census provider set could not bind to the owner graph.";
            return false;
        }

        if (providers == null)
        {
            diagnostic = "The selected Core NPC owner graph produced no census provider set.";
            return false;
        }

        List<DailyV1OwnerSectionCensusProviderRegistration> built =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(providers.Count);
        HashSet<string> observedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            OwnerSectionCensusWitness witness;
            try { witness = provider?.GetCurrentCensus(); }
            catch (Exception exception) when (exception is ArgumentException
                || exception is InvalidOperationException || exception is OverflowException)
            {
                diagnostic = "A Core-owned selected NPC provider failed to read its live witness.";
                return false;
            }

            if (witness == null
                || !IsCoreOwnedSelectedNpcCensusSectionId(witness.SectionId)
                || witness.OwnerInstanceIdentity == null
                || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || !observedSectionIds.Add(witness.SectionId)
                || (witness.SectionId.StartsWith(
                        DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix,
                        StringComparison.Ordinal)
                    && witness.Cardinality != 1))
            {
                diagnostic = "A Core-owned selected NPC provider returned an invalid, duplicate, or substituted witness.";
                return false;
            }

            built.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                provider,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision,
                hasBaseline: true));
        }

        built.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        registrations = built.AsReadOnly();
        return true;
    }

    internal static bool IsCoreOwnedSelectedNpcCensusSectionId(string sectionId)
    {
        if (string.IsNullOrWhiteSpace(sectionId)) return false;

        if (sectionId.StartsWith(
            DailyV1SelectedOwnerSectionCensusContract.NpcResidencePrefix,
            StringComparison.Ordinal))
            return true;

        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes)
            if (sectionId.StartsWith(prefix, StringComparison.Ordinal))
                return true;

        return false;
    }
    internal static bool TryCreateCoreOwnedNpcInventoryAndMoneyAccountCensusProviderRegistrations(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        registrations = null;
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

        Dictionary<string, object> expectedOwnerBySection =
            new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || npc.Inventory == null || npc.MoneyAccount == null
                || !expectedOwnerBySection.TryAdd(
                    DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + npc.RuntimeId,
                    npc.Inventory)
                || !expectedOwnerBySection.TryAdd(
                    DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + npc.RuntimeId,
                    npc.MoneyAccount))
            {
                diagnostic = "The selected Core NPC census graph has a missing or duplicate runtime identity or owner.";
                return false;
            }
        }

        List<DailyV1OwnerSectionCensusProviderRegistration> built =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(providers.Count);
        HashSet<string> observedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < providers.Count; index++)
        {
            IOwnerSectionCensusProvider provider = providers[index];
            OwnerSectionCensusWitness witness;
            try { witness = provider?.GetCurrentCensus(); }
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
                || !expectedOwnerBySection.TryGetValue(witness.SectionId, out object expectedOwner)
                || !ReferenceEquals(witness.OwnerInstanceIdentity, expectedOwner)
                || !observedSectionIds.Add(witness.SectionId))
            {
                diagnostic = "A selected Core NPC census provider returned an invalid, substituted, or duplicate owner witness.";
                return false;
            }

            if (witness.SectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix,
                    StringComparison.Ordinal)
                && witness.Cardinality != 1)
            {
                diagnostic = "Each selected Core NPC MoneyAccount census row must have cardinality exactly one.";
                return false;
            }

            built.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                provider,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision,
                hasBaseline: true));
        }

        if (observedSectionIds.Count != expectedOwnerBySection.Count)
        {
            diagnostic = "The selected Core NPC census provider set does not cover each exact owner identity.";
            return false;
        }

        built.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        registrations = built.AsReadOnly();
        return true;
    }

    private static bool TryCreateCoreOwnedNpcInventoryAndMoneyAccountCensusWitnesses(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<OwnerSectionCensusWitness> witnesses,
        out string diagnostic)
    {
        witnesses = null;
        if (!TryCreateCoreOwnedNpcInventoryAndMoneyAccountCensusProviderRegistrations(
                graph,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
                out diagnostic))
            return false;

        OwnerSectionCensusWitness[] built = new OwnerSectionCensusWitness[registrations.Count];
        for (int index = 0; index < registrations.Count; index++)
        {
            DailyV1OwnerSectionCensusProviderRegistration registration = registrations[index];
            if (registration == null || !registration.HasBaseline)
            {
                diagnostic = "A selected Core NPC census registration has no initial owner baseline.";
                return false;
            }
            built[index] = new OwnerSectionCensusWitness(
                registration.SectionId,
                registration.SchemaVersion,
                registration.BaselineOwnerInstanceIdentity,
                registration.BaselineCardinality,
                registration.BaselineRevision);
        }

        witnesses = Array.AsReadOnly(built);
        diagnostic = null;
        return true;
    }

    /// <summary>
    /// Selects the complete dynamic NPC owner fragment that Core can source
    /// from the admitted graph. Omission is allowed only when every selected
    /// family row is present and exactly matches its Core owner witness. A
    /// mixed roster, stale row, or substituted owner keeps the full host vector.
    /// Local-observation receipt rows remain in the host remainder; merchant
    /// receipt rows are sourced from the exact Core owner and remain exact-empty.
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
            selected.AddRange(DailyV1NpcOwnerCensusProviders.CreateMerchantTradeStateReceiptProviders(graph.Npcs));
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
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradeStateReceiptsPrefix
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
        string currentActionPrefix = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix;
        bool hostOmittedExactCoreCurrentActionRows = !mixedRoster
            && graph.Npcs.Count > 0
            && CountFamilyRows(copied, currentActionPrefix) == 0;
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
            bool hostOmittedExactFamily = hostOmittedExactCoreRuntimeStateRows
                || (string.Equals(prefix, currentActionPrefix, StringComparison.Ordinal)
                    && hostOmittedExactCoreCurrentActionRows);
            if (hostOmittedExactFamily && actualRows == 0)
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
                bool omittedCoreOwnedRow = hostOmittedExactCoreRuntimeStateRows
                    || (hostOmittedExactCoreCurrentActionRows
                        && witness.SectionId.StartsWith(currentActionPrefix, StringComparison.Ordinal));
                if (omittedCoreOwnedRow)
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

        if (hostOmittedExactCoreRuntimeStateRows || hostOmittedExactCoreCurrentActionRows)
        {
            List<OwnerSectionCensusSnapshot> recomposed =
                new List<OwnerSectionCensusSnapshot>(copied.Length + witnesses.Length);
            recomposed.AddRange(copied);
            for (int i = 0; i < witnesses.Length; i++)
            {
                if (witnessIndexes[i] >= 0) continue;
                OwnerSectionCensusWitness witness = witnesses[i];
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

    /// <summary>
    /// Creates the selected City census sources directly from the exact Core
    /// City owner graph. Unity may still validate its compatibility projection
    /// against these sources during setup, but the sealed selected-profile
    /// registry retains these live Core providers rather than Unity providers.
    /// </summary>
    internal static bool TryCreateCoreOwnedCityCensusProviderRegistrations(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        registrations = null;
        diagnostic = null;
        if (graph == null
            || !TryCreateCoreOwnedCityCensusWitnesses(
                graph, out IReadOnlyList<OwnerSectionCensusWitness> expectedWitnesses,
                out diagnostic))
            return false;

        Dictionary<string, OwnerSectionCensusWitness> expectedById =
            new Dictionary<string, OwnerSectionCensusWitness>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusWitness witness in expectedWitnesses)
            if (witness == null || !expectedById.TryAdd(witness.SectionId, witness))
            {
                diagnostic = "The selected Core City census contains a duplicate live section identity.";
                return false;
            }

        List<DailyV1OwnerSectionCensusProviderRegistration> built =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(expectedWitnesses.Count);
        HashSet<string> registeredIds = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            {
                string cityKey = P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(city.RuntimeId);
                DailyV1OwnerSectionCensusProviderRegistration[] cityRegistrations =
                {
                    new DailyV1OwnerSectionCensusProviderRegistration(
                        DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId),
                        DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                        OwnerSectionRole.Required,
                        new P12OwnerSectionCensusProvider<DailyV1CityNpcPresenceOwner>(
                            DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId),
                            DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                            city.NpcPresence,
                            owner => owner.OrderedNpcRuntimeIds.Count,
                            owner => owner.Revision)),
                    new DailyV1OwnerSectionCensusProviderRegistration(
                        DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix + cityKey,
                        DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                        OwnerSectionRole.Required,
                        new P12OwnerSectionCensusProvider<DailyV1MarketOwner>(
                            DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix + cityKey,
                            DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                            city.Market,
                            owner => owner.Items.Count,
                            owner => owner.Revision)),
                    new DailyV1OwnerSectionCensusProviderRegistration(
                        DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix + cityKey,
                        DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                        OwnerSectionRole.Required,
                        new P12OwnerSectionCensusProvider<SettlementPopulationRuntime>(
                            DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix + cityKey,
                            DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                            city.Population,
                            _ => 1,
                            owner => owner.Revision)),
                    new DailyV1OwnerSectionCensusProviderRegistration(
                        DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix + cityKey,
                        DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                        OwnerSectionRole.Required,
                        new CityPopulationOperationReceiptsCensusProvider(
                            DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix + cityKey,
                            city.Population))
                };

                foreach (DailyV1OwnerSectionCensusProviderRegistration registration in cityRegistrations)
                {
                    OwnerSectionCensusWitness actual = registration.Provider.GetCurrentCensus();
                    if (registration == null
                        || !registeredIds.Add(registration.SectionId)
                        || registration.Role != OwnerSectionRole.Required
                        || registration.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion
                        || !string.Equals(actual?.SectionId, registration.SectionId, StringComparison.Ordinal)
                        || actual.SchemaVersion != registration.SchemaVersion
                        || actual.Cardinality < 0
                        || actual.Revision < 0L
                        || !expectedById.TryGetValue(registration.SectionId, out OwnerSectionCensusWitness expected)
                        || !ReferenceEquals(actual.OwnerInstanceIdentity, expected.OwnerInstanceIdentity)
                        || actual.Cardinality != expected.Cardinality
                        || actual.Revision != expected.Revision)
                    {
                        diagnostic = "A live Core City census provider differs from its exact selected owner at "
                            + registration?.SectionId + ".";
                        return false;
                    }
                    built.Add(registration);
                }
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "The live Core City census providers could not be bound to the selected owner graph.";
            return false;
        }

        if (registeredIds.Count != expectedById.Count)
        {
            diagnostic = "The live Core City census provider set does not cover every Core City owner row.";
            return false;
        }

        registrations = built.AsReadOnly();
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
        if (IsDailyCaptureProfile && HasAdmittedSelectedDailyExecution
            && HasSealedSelectedOwnerSectionCensusRegistry
            && !TryValidateCurrentSelectedOwnerSectionCensusBaselines())
        {
            boundary.FaultRuntimeAdmission();
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

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
