using System;
using System.Collections.Generic;

internal sealed class DailyV1OwnerSectionContractIdentity
{
    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal OwnerSectionRole Role { get; }

    internal DailyV1OwnerSectionContractIdentity(string sectionId, int schemaVersion, OwnerSectionRole role)
    {
        SectionId = sectionId;
        SchemaVersion = schemaVersion;
        Role = role;
    }
}


internal enum DailyV1OperationScopeKind : byte
{
    Application = 0,
    RuntimeProtocol = 1,
    ContinuationSave = 2,
    DailyAdvance = 3
}

internal enum DailyV1CaptureScopeAllowance : byte
{
    None = 0,
    ContinuationSave = 1,
    DailyAdvance = 2,
    SessionReplacementValidation = 3
}

/// <summary>
/// Owns the per-runtime single-writer advance lease and completed Daily-v1
/// core sequence. This is execution state, not a thread lock or persisted data.
/// </summary>
internal sealed partial class DailyV1ExecutionBoundaryState
{
    private readonly object runtimeInstanceIdentity = new object();
    private readonly object operationSyncRoot = new object();
    private readonly List<DailyV1OperationScopeKind> activeOperationScopes =
        new List<DailyV1OperationScopeKind>();
    private Dictionary<string, DailyV1OwnerSectionContractIdentity> expectedOwnerSectionContracts =
        new Dictionary<string, DailyV1OwnerSectionContractIdentity>(StringComparer.Ordinal);
    private bool expectedOwnerSectionContractInventorySealed;
    private long expectedOwnerSectionContractInventoryRevision;
    private readonly HashSet<string> expectedRuntimeProtocolOperationContracts =
        new HashSet<string>(StringComparer.Ordinal);
    private readonly List<string> activeRuntimeProtocolOperationContracts =
        new List<string>();
    private readonly DailyV1MutationEpochState mutationEpochState =
        new DailyV1MutationEpochState();
    private readonly AuthoritativeMutationGuard mutationGuard =
        new AuthoritativeMutationGuard();
    private bool runtimeIdentityBound;
    private bool runtimeConfigurationBound;
    private bool runtimeProtocolOperationInventorySealed;
    private bool exclusiveSessionReplacementInProgress;
    private DailyV1AdvanceLease advanceLease;
    private long advanceLeaseInitialCompletedSequence;
    private long completedCoreSequence;
    private DailyCaptureEligibilityToken currentCaptureToken;
    private volatile bool factualReadWorldPublished;
    private bool selectedRuntimeOwnerGraphBound;
    private DailyV1SelectedRuntimeOwnerGraph selectedRuntimeOwnerGraph;
    private IReadOnlyList<DailyV1ActorTurnRosterProjection> selectedRuntimeActorRoster;
    private bool selectedRuntimeExecutionRootsBound;
    private RuntimeIdAllocator selectedRuntimeIdAllocator;
    private IAuthoritativeRandomSource selectedRandomSource;
    private SimulationRecordSequence selectedRecordSequence;
    private NpcDecisionStore selectedDecisionStore;
    private ActorChoiceStore selectedActorChoiceStore;
    private DailyV1ActorTurnCoreRoots selectedRuntimeActorTurnCoreRoots;

    internal SimulationTime SimulationTime { get; private set; }
    internal SimulationRuntimeAdmissionContext AdmissionContext { get; private set; }
    internal SimulationRuntimeCompositionProfile CompositionProfile { get; private set; }
    internal WorldId WorldId { get; private set; }
    internal EffectiveSimulationConfiguration ConfigurationIdentity { get; private set; }
    internal SimulationCalendar CalendarIdentity { get; private set; }
    internal AuthoritativeMutationGuard MutationGuard => mutationGuard;
    internal bool CanMutate => mutationGuard.CanMutate;
    internal bool IsFactualReadWorldPublished => factualReadWorldPublished;

    internal DailyV1ExecutionBoundaryState() { }

    /// <summary>
    /// Constructs the one Core execution state used by a selected runtime.
    /// Clock, admission, composition and world identities live beside that
    /// runtime's lease, completed-boundary sequence, graph and capture token;
    /// a Unity adapter must not keep parallel copies as runtime authority.
    /// </summary>
    internal DailyV1ExecutionBoundaryState(
        SimulationTime simulationTime,
        SimulationRuntimeAdmissionContext admissionContext,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId)
    {
        if (simulationTime == null) throw new ArgumentNullException(nameof(simulationTime));
        if (!Enum.IsDefined(typeof(SimulationRuntimeCompositionProfile), compositionProfile))
            throw new ArgumentOutOfRangeException(nameof(compositionProfile));
        SimulationTime = simulationTime;
        AdmissionContext = admissionContext;
        CompositionProfile = compositionProfile;
        WorldId = worldId;
        runtimeIdentityBound = true;
    }

    internal bool TryBindRuntimeConfiguration(
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity)
    {
        if (!runtimeIdentityBound || configurationIdentity == null || calendarIdentity == null)
            return false;
        if (runtimeConfigurationBound)
            return ReferenceEquals(ConfigurationIdentity, configurationIdentity)
                && ReferenceEquals(CalendarIdentity, calendarIdentity);
        ConfigurationIdentity = configurationIdentity;
        CalendarIdentity = calendarIdentity;
        runtimeConfigurationBound = true;
        return true;
    }

    private bool MatchesBoundRuntimeIdentity(
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay)
    {
        return !runtimeIdentityBound
            || (runtimeConfigurationBound
                && ReferenceEquals(AdmissionContext, admissionContext)
                && ReferenceEquals(ConfigurationIdentity, configurationIdentity)
                && ReferenceEquals(CalendarIdentity, calendarIdentity)
                && CompositionProfile == compositionProfile
                && ReferenceEquals(WorldId, worldId)
                && absoluteDay == SimulationTime.AbsoluteDay);
    }

    internal bool AdvanceLeaseHeld => advanceLease != null;

    /// <summary>
    /// The single operation count shared by application admission and the P12
    /// census protocol for this runtime. It is deliberately stored beside the
    /// runtime lease and capture token, rather than mirrored by a host gate.
    /// </summary>
    internal int ActiveOperationCount
    {
        get { lock (operationSyncRoot) return activeOperationCount; }
    }

    internal int ActiveRuntimeOperationCount
    {
        get
        {
            lock (operationSyncRoot)
            {
                int count = 0;
                foreach (DailyV1OperationScopeKind scope in activeOperationScopes)
                    if (scope == DailyV1OperationScopeKind.RuntimeProtocol) count++;
                return count;
            }
        }
    }

    internal bool HasRegisteredRuntimeProtocolOperationContracts
    {
        get
        {
            lock (operationSyncRoot)
                return expectedRuntimeProtocolOperationContracts.Count > 0;
        }
    }

    internal bool HasRuntimeProtocolOperationInventory
    {
        get
        {
            lock (operationSyncRoot)
                return runtimeProtocolOperationInventorySealed
                    && expectedRuntimeProtocolOperationContracts.Count > 0;
        }
    }

    internal bool HasRuntimeProtocolOperationContract(string operationContractId)
    {
        if (string.IsNullOrWhiteSpace(operationContractId)) return false;
        lock (operationSyncRoot)
            return runtimeProtocolOperationInventorySealed
                && expectedRuntimeProtocolOperationContracts.Contains(operationContractId);
    }

    internal IReadOnlyList<string> GetRuntimeProtocolOperationContractIds()
    {
        lock (operationSyncRoot)
        {
            List<string> operationContractIds =
                new List<string>(expectedRuntimeProtocolOperationContracts);
            operationContractIds.Sort(StringComparer.Ordinal);
            return operationContractIds.AsReadOnly();
        }
    }

    /// <summary>
    /// Owns the logical P12 section contracts attached to this runtime.
    /// This is only the declared inventory; provider registration and exact
    /// selected-profile completeness remain separately assessed by the census
    /// adapter and its composition evidence.
    /// </summary>
    internal bool ExpectedOwnerSectionContractInventorySealed
    {
        get { lock (operationSyncRoot) return expectedOwnerSectionContractInventorySealed; }
    }

    internal long ExpectedOwnerSectionContractInventoryRevision
    {
        get { lock (operationSyncRoot) return expectedOwnerSectionContractInventoryRevision; }
    }

    internal IReadOnlyList<DailyV1OwnerSectionContractIdentity> GetExpectedOwnerSectionContractInventory(
        out long revision)
    {
        lock (operationSyncRoot)
        {
            List<DailyV1OwnerSectionContractIdentity> contracts =
                new List<DailyV1OwnerSectionContractIdentity>(expectedOwnerSectionContracts.Values);
            contracts.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
            revision = expectedOwnerSectionContractInventoryRevision;
            return contracts.AsReadOnly();
        }
    }

    internal bool TryRegisterExpectedOwnerSectionContract(
        string sectionId,
        int schemaVersion,
        OwnerSectionRole role)
    {
        if (string.IsNullOrWhiteSpace(sectionId)
            || schemaVersion <= 0
            || !Enum.IsDefined(typeof(OwnerSectionRole), role))
            return false;

        lock (operationSyncRoot)
        {
            if (expectedOwnerSectionContractInventorySealed
                || activeOperationScopes.Count != 0
                || exclusiveSessionReplacementInProgress
                || expectedOwnerSectionContractInventoryRevision == long.MaxValue
                || expectedOwnerSectionContracts.ContainsKey(sectionId))
                return false;

            Dictionary<string, DailyV1OwnerSectionContractIdentity> staged =
                new Dictionary<string, DailyV1OwnerSectionContractIdentity>(
                    expectedOwnerSectionContracts, StringComparer.Ordinal);
            staged.Add(sectionId,
                new DailyV1OwnerSectionContractIdentity(sectionId, schemaVersion, role));
            expectedOwnerSectionContracts = staged;
            expectedOwnerSectionContractInventoryRevision++;
            return true;
        }
    }

    internal bool TryReplaceExpectedOwnerSectionContractInventoryBeforeSeal(
        IEnumerable<DailyV1OwnerSectionContractIdentity> contracts)
    {
        if (!TryCreateExpectedOwnerSectionContractMap(contracts, out var staged))
            return false;

        lock (operationSyncRoot)
        {
            if (expectedOwnerSectionContractInventorySealed
                || activeOperationScopes.Count != 0
                || exclusiveSessionReplacementInProgress
                || expectedOwnerSectionContractInventoryRevision == long.MaxValue)
                return false;

            expectedOwnerSectionContracts = staged;
            expectedOwnerSectionContractInventoryRevision++;
            return true;
        }
    }

    /// <summary>
    /// Applies the validated census roster transition's section identity set
    /// while the runtime is inside an owner-thread operation. This does not
    /// advance the mutation epoch; the owning operation retains that commit.
    /// </summary>
    internal bool TryReplaceExpectedOwnerSectionContractInventoryDuringRuntimeOperation(
        IEnumerable<DailyV1OwnerSectionContractIdentity> contracts)
    {
        if (!TryCreateExpectedOwnerSectionContractMap(contracts, out var staged))
            return false;

        lock (operationSyncRoot)
        {
            if (!expectedOwnerSectionContractInventorySealed
                || exclusiveSessionReplacementInProgress
                || !activeRuntimeProtocolOperationContracts.Contains(
                    DailyV1RuntimeProtocolOperationContracts.NpcMembership)
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || expectedOwnerSectionContractInventoryRevision == long.MaxValue)
                return false;

            expectedOwnerSectionContracts = staged;
            expectedOwnerSectionContractInventoryRevision++;
            return true;
        }
    }

    internal bool TrySealExpectedOwnerSectionContractInventory()
    {
        lock (operationSyncRoot)
        {
            if (expectedOwnerSectionContractInventorySealed
                || expectedOwnerSectionContracts.Count == 0
                || activeOperationScopes.Count != 0
                || exclusiveSessionReplacementInProgress)
                return false;

            expectedOwnerSectionContractInventorySealed = true;
            return true;
        }
    }

    private static bool TryCreateExpectedOwnerSectionContractMap(
        IEnumerable<DailyV1OwnerSectionContractIdentity> contracts,
        out Dictionary<string, DailyV1OwnerSectionContractIdentity> inventory)
    {
        inventory = null;
        if (contracts == null) return false;

        Dictionary<string, DailyV1OwnerSectionContractIdentity> staged =
            new Dictionary<string, DailyV1OwnerSectionContractIdentity>(StringComparer.Ordinal);
        try
        {
            foreach (DailyV1OwnerSectionContractIdentity contract in contracts)
            {
                if (contract == null
                    || string.IsNullOrWhiteSpace(contract.SectionId)
                    || contract.SchemaVersion <= 0
                    || !Enum.IsDefined(typeof(OwnerSectionRole), contract.Role)
                    || staged.ContainsKey(contract.SectionId))
                    return false;
                staged.Add(contract.SectionId, contract);
            }
        }
        catch
        {
            return false;
        }

        inventory = staged;
        return true;
    }
    internal bool TryBeginOperation(DailyV1OperationScopeKind kind = DailyV1OperationScopeKind.Application)
    {
        lock (operationSyncRoot)
        {
            if (exclusiveSessionReplacementInProgress
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            if (kind == DailyV1OperationScopeKind.RuntimeProtocol
                || !Enum.IsDefined(typeof(DailyV1OperationScopeKind), kind))
                return false;
            return TryAddOperationScope(kind);
        }
    }

    internal bool TryEndOperation(DailyV1OperationScopeKind kind = DailyV1OperationScopeKind.Application)
    {
        lock (operationSyncRoot)
        {
            if (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread())
                return false;
            if (kind == DailyV1OperationScopeKind.RuntimeProtocol
                || !Enum.IsDefined(typeof(DailyV1OperationScopeKind), kind))
                return false;
            return TryRemoveOperationScope(kind);
        }
    }

    /// <summary>
    /// Records one runtime operation during selected-profile bootstrap. The
    /// complete supported inventory is sealed before Core will admit any ID.
    /// </summary>
    internal bool TryRegisterRuntimeProtocolOperationContract(string operationContractId)
    {
        if (string.IsNullOrWhiteSpace(operationContractId)) return false;
        lock (operationSyncRoot)
        {
            if (runtimeProtocolOperationInventorySealed
                || activeOperationScopes.Count != 0
                || exclusiveSessionReplacementInProgress
                || AdmissionContext == null
                || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || !runtimeIdentityBound
                || !runtimeConfigurationBound
                || !AdmissionContext.IsOwnedByCurrentThread())
                return false;

            return expectedRuntimeProtocolOperationContracts.Add(operationContractId);
        }
    }

    /// <summary>
    /// Seals the Core-owned allowlist only when it exactly matches one complete
    /// supported selected-runtime catalogue.
    /// </summary>
    internal bool TrySealRuntimeProtocolOperationInventory()
    {
        lock (operationSyncRoot)
        {
            if (runtimeProtocolOperationInventorySealed
                || activeOperationScopes.Count != 0
                || exclusiveSessionReplacementInProgress
                || AdmissionContext == null
                || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || !runtimeIdentityBound
                || !runtimeConfigurationBound
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !DailyV1RuntimeProtocolOperationContracts.IsSupportedSelectedOperationInventory(
                    expectedRuntimeProtocolOperationContracts))
                return false;

            runtimeProtocolOperationInventorySealed = true;
            return true;
        }
    }

    /// <summary>
    /// Prevents both application and direct runtime/P12 operations from
    /// entering the source session while a detached replacement is staged.
    /// This flag shares the same lock and operation registry as daily advance
    /// and P12 census scopes, so same-thread callbacks cannot bypass the app gate.
    /// </summary>
    internal bool TryBeginExclusiveSessionReplacement()
    {
        lock (operationSyncRoot)
        {
            if (exclusiveSessionReplacementInProgress
                || activeOperationScopes.Count != 0
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            exclusiveSessionReplacementInProgress = true;
            return true;
        }
    }

    internal bool TryEndExclusiveSessionReplacement()
    {
        lock (operationSyncRoot)
        {
            if (!exclusiveSessionReplacementInProgress
                || activeOperationScopes.Count != 0
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            exclusiveSessionReplacementInProgress = false;
            return true;
        }
    }

    internal bool TryAssessCaptureScope(DailyV1CaptureScopeAllowance allowance)
    {
        lock (operationSyncRoot)
        {
            if ((exclusiveSessionReplacementInProgress
                    && allowance != DailyV1CaptureScopeAllowance.SessionReplacementValidation)
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            if (allowance == DailyV1CaptureScopeAllowance.None
                || allowance == DailyV1CaptureScopeAllowance.SessionReplacementValidation)
                return activeOperationScopes.Count == 0;
            if (activeOperationScopes.Count != 1)
                return false;

            DailyV1OperationScopeKind expected = allowance == DailyV1CaptureScopeAllowance.ContinuationSave
                ? DailyV1OperationScopeKind.ContinuationSave
                : allowance == DailyV1CaptureScopeAllowance.DailyAdvance
                    ? DailyV1OperationScopeKind.DailyAdvance
                    : (DailyV1OperationScopeKind)byte.MaxValue;
            return activeOperationScopes[0] == expected;
        }
    }

    internal bool TryBeginRuntimeProtocolOperation(string operationContractId)
    {
        lock (operationSyncRoot)
        {
            if (exclusiveSessionReplacementInProgress
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !runtimeProtocolOperationInventorySealed
                || string.IsNullOrWhiteSpace(operationContractId)
                || !expectedRuntimeProtocolOperationContracts.Contains(operationContractId)
                || activeRuntimeProtocolOperationContracts.Count == int.MaxValue)
                return false;
            if (!TryAddOperationScope(DailyV1OperationScopeKind.RuntimeProtocol)) return false;
            activeRuntimeProtocolOperationContracts.Add(operationContractId);
            return true;
        }
    }

    internal bool TryEndRuntimeProtocolOperation()
    {
        lock (operationSyncRoot)
        {
            if (AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || activeRuntimeProtocolOperationContracts.Count == 0
                || !TryRemoveOperationScope(DailyV1OperationScopeKind.RuntimeProtocol))
                return false;
            activeRuntimeProtocolOperationContracts.RemoveAt(
                activeRuntimeProtocolOperationContracts.Count - 1);
            return true;
        }
    }

    private int activeOperationCount => activeOperationScopes.Count;

    private bool TryAddOperationScope(DailyV1OperationScopeKind kind)
    {
        if (activeOperationScopes.Count == int.MaxValue) return false;
        activeOperationScopes.Add(kind);
        return true;
    }

    private bool TryRemoveOperationScope(DailyV1OperationScopeKind kind)
    {
        for (int index = activeOperationScopes.Count - 1; index >= 0; index--)
        {
            if (activeOperationScopes[index] != kind) continue;
            activeOperationScopes.RemoveAt(index);
            return true;
        }
        return false;
    }

    internal long CompletedCoreSequence => completedCoreSequence;
    internal object RuntimeInstanceIdentity => runtimeInstanceIdentity;
    internal DailyCaptureEligibilityToken CurrentCaptureToken => currentCaptureToken;
    /// <summary>
    /// The mutation epoch paired with this runtime's operation registry,
    /// completed-boundary sequence, and capture token. Continuation census
    /// protocols attached to this runtime must share this exact state.
    /// </summary>
    internal DailyV1MutationEpochState MutationEpochState => mutationEpochState;
    internal DailyV1SelectedRuntimeOwnerGraph SelectedRuntimeOwnerGraph => selectedRuntimeOwnerGraph;
    /// <summary>
    /// Core-owned ordered runtime identity slots used by selected Daily-v1
    /// turns. Null holes and unselected host identities remain in this exact
    /// sequence because actor-choice dispositions persist their ordinals.
    /// </summary>
    internal IReadOnlyList<DailyV1ActorTurnRosterProjection> SelectedRuntimeActorRoster =>
        selectedRuntimeActorRoster;
    internal bool SelectedRuntimeExecutionRootsBound => selectedRuntimeExecutionRootsBound;
    /// <summary>
    /// Whether this admitted state has the complete Core owner/configuration
    /// roots needed by the selected Daily-v1 execution path. Path selection is
    /// derived from Core-owned identities; a Unity adapter cannot opt the
    /// authoritative runtime in or out by reporting its own demographic view.
    /// </summary>
    internal bool HasAdmittedSelectedDailyExecution
    {
        get
        {
            DailyV1SelectedRuntimeOwnerGraph graph = selectedRuntimeOwnerGraph;
            return runtimeIdentityBound
                && runtimeConfigurationBound
                && AdmissionContext != null
                && AdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                && selectedRuntimeOwnerGraphBound
                && selectedRuntimeExecutionRootsBound
                && graph != null
                && graph.Definitions != null
                && ReferenceEquals(graph.Definitions.EffectiveConfiguration, ConfigurationIdentity)
                && graph.PersonStore != null
                && graph.ScheduledDirectives != null
                && graph.SpatialNetwork != null
                && graph.JusticeRecordsOwner != null
                && graph.TravelParties != null
                && graph.RuntimeIdentityIndex != null
                && selectedRuntimeIdAllocator != null
                && selectedRandomSource != null
                && selectedRecordSequence != null
                && selectedDecisionStore != null
                && selectedActorChoiceStore != null
                && ReferenceEquals(selectedActorChoiceStore.PersonStore, graph.PersonStore)
                && HasSelectedCityAndNpcOwners(graph);
        }
    }
    internal RuntimeIdAllocator SelectedRuntimeIdAllocator => selectedRuntimeIdAllocator;
    internal IAuthoritativeRandomSource SelectedRandomSource => selectedRandomSource;
    internal SimulationRecordSequence SelectedRecordSequence => selectedRecordSequence;
    internal NpcDecisionStore SelectedDecisionStore => selectedDecisionStore;
    internal ActorChoiceStore SelectedActorChoiceStore => selectedActorChoiceStore;
    /// <summary>
    /// One immutable Core bundle for the exact decision roots bound to this
    /// execution state. Per-day contexts reuse it; a host cannot recreate a
    /// parallel root bundle from projected runtime values.
    /// </summary>
    internal DailyV1ActorTurnCoreRoots SelectedRuntimeActorTurnCoreRoots =>
        selectedRuntimeActorTurnCoreRoots;

    internal bool TryBindSelectedRuntimeOwnerGraph(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (selectedRuntimeOwnerGraphBound)
            return ReferenceEquals(selectedRuntimeOwnerGraph, graph);

        selectedRuntimeOwnerGraph = graph;
        selectedRuntimeOwnerGraphBound = true;
        return true;
    }

    internal bool TryReplaceSelectedRuntimeOwnerGraph(
        DailyV1SelectedRuntimeOwnerGraph expected,
        DailyV1SelectedRuntimeOwnerGraph replacement)
    {
        if (!selectedRuntimeOwnerGraphBound
            || expected == null
            || replacement == null
            || !ReferenceEquals(selectedRuntimeOwnerGraph, expected)
            || !ReferenceEquals(expected.Definitions, replacement.Definitions))
            return false;

        DailyV1ActorTurnCoreRoots replacementActorTurnRoots = null;
        if (selectedRuntimeExecutionRootsBound)
        {
            if (!ReferenceEquals(replacement.PersonStore, selectedActorChoiceStore?.PersonStore))
                return false;
            try
            {
                replacementActorTurnRoots = new DailyV1ActorTurnCoreRoots(this, replacement);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        selectedRuntimeOwnerGraph = replacement;
        if (replacementActorTurnRoots != null)
            selectedRuntimeActorTurnCoreRoots = replacementActorTurnRoots;
        return true;
    }

    /// <summary>
    /// Binds the runtime's exact actor identity/ordinal projection after
    /// constructor membership has been composed. Domain owner state remains
    /// in the selected graph; this ordered identity roster is the single Core
    /// source for actor-turn ordinals and preserves unselected runtime slots.
    /// </summary>
    internal bool TryBindSelectedRuntimeActorRoster(
        IEnumerable<DailyV1ActorTurnRosterProjection> runtimeRoster)
    {
        if (!selectedRuntimeOwnerGraphBound || selectedRuntimeOwnerGraph == null
            || selectedRuntimeActorRoster != null
            || !TryCreateValidatedSelectedRuntimeActorRoster(
                selectedRuntimeOwnerGraph, runtimeRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> candidate))
            return false;

        selectedRuntimeActorRoster = candidate;
        return true;
    }

    /// <summary>
    /// Binds the initial selected actor order from Core's admitted owner graph.
    /// Unity may later compare its runtime projections with this exact sequence,
    /// but does not choose actor-turn ordinals for the shared runtime.
    /// </summary>
    internal bool TryBindInitialSelectedRuntimeActorRosterFromOwnerGraph()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = selectedRuntimeOwnerGraph;
        if (!selectedRuntimeOwnerGraphBound || graph == null
            || graph.PersonStore == null || selectedRuntimeActorRoster != null)
            return false;

        List<DailyV1ActorTurnRosterProjection> initialRoster =
            new List<DailyV1ActorTurnRosterProjection>(graph.Npcs.Count);
        foreach (DailyV1NpcRuntimeOwners owner in graph.Npcs)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId)) return false;
            PersonId personId = null;
            if (graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    owner.RuntimeId, out PersonRuntime person))
            {
                if (person == null || person.PersonId == null
                    || !person.IsMaterialized
                    || !string.Equals(person.MaterializedNpcRuntimeId,
                        owner.RuntimeId, StringComparison.Ordinal))
                    return false;
                personId = person.PersonId;
            }
            initialRoster.Add(new DailyV1ActorTurnRosterProjection(
                owner.RuntimeId, personId));
        }

        return TryBindSelectedRuntimeActorRoster(initialRoster);
    }

    /// <summary>
    /// Binds a legacy/unpublished roster once, or proves that a host projection
    /// exactly matches the Core-created order and PersonId instances. A Unity
    /// host cannot replace the roster that determines persisted actor ordinals.
    /// </summary>
    internal bool TryBindOrValidateSelectedRuntimeActorRoster(
        IEnumerable<DailyV1ActorTurnRosterProjection> runtimeRoster)
    {
        if (!selectedRuntimeOwnerGraphBound || selectedRuntimeOwnerGraph == null
            || !TryCreateValidatedSelectedRuntimeActorRoster(
                selectedRuntimeOwnerGraph, runtimeRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> candidate))
            return false;

        IReadOnlyList<DailyV1ActorTurnRosterProjection> existing = selectedRuntimeActorRoster;
        if (existing == null)
        {
            selectedRuntimeActorRoster = candidate;
            return true;
        }

        if (existing.Count != candidate.Count) return false;
        for (int index = 0; index < existing.Count; index++)
        {
            DailyV1ActorTurnRosterProjection left = existing[index];
            DailyV1ActorTurnRosterProjection right = candidate[index];
            if (left == null || right == null)
            {
                if (left != null || right != null) return false;
                continue;
            }
            if (!string.Equals(left.RuntimeId, right.RuntimeId, StringComparison.Ordinal)
                || !ReferenceEquals(left.PersonId, right.PersonId))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Prepares the same ordered runtime identity roster with one newly
    /// materialized PersonId. The caller publishes this projection as part of
    /// the existing PersonStore/NPC bind transaction; runtime ID order and
    /// holes are unchanged.
    /// </summary>
    internal bool TryCreateSelectedRuntimeActorRosterWithPersonBinding(
        string runtimeId,
        PersonId personId,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> expectedRoster,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        expectedRoster = selectedRuntimeActorRoster;
        replacementRoster = null;
        if (!selectedRuntimeOwnerGraphBound
            || selectedRuntimeOwnerGraph == null
            || selectedRuntimeActorRoster == null
            || string.IsNullOrWhiteSpace(runtimeId)
            || personId == null)
            return false;

        List<DailyV1ActorTurnRosterProjection> candidate =
            new List<DailyV1ActorTurnRosterProjection>(selectedRuntimeActorRoster.Count);
        bool replaced = false;
        foreach (DailyV1ActorTurnRosterProjection member in selectedRuntimeActorRoster)
        {
            if (member != null
                && string.Equals(member.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                if (replaced || member.PersonId != null) return false;
                replaced = true;
                candidate.Add(new DailyV1ActorTurnRosterProjection(member.RuntimeId, personId));
            }
            else
            {
                candidate.Add(member);
            }
        }

        return replaced && TryCreateValidatedSelectedRuntimeActorRoster(
            selectedRuntimeOwnerGraph, candidate, out replacementRoster);
    }

    /// <summary>
    /// Publishes an already validated roster projection with compare-and-swap
    /// semantics so a caller can compensate it if its paired owner bind fails.
    /// </summary>
    internal bool TryCommitSelectedRuntimeActorRoster(
        IReadOnlyList<DailyV1ActorTurnRosterProjection> expectedRoster,
        IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        if (!selectedRuntimeOwnerGraphBound
            || selectedRuntimeOwnerGraph == null
            || expectedRoster == null
            || replacementRoster == null
            || !ReferenceEquals(selectedRuntimeActorRoster, expectedRoster))
            return false;

        selectedRuntimeActorRoster = replacementRoster;
        return true;
    }

    internal bool TryCreateSelectedRuntimeActorRosterWithAppendedMember(
        DailyV1ActorTurnRosterProjection member,
        DailyV1SelectedRuntimeOwnerGraph replacementGraph,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        replacementRoster = null;
        if (selectedRuntimeActorRoster == null || member == null
            || replacementGraph == null)
            return false;

        List<DailyV1ActorTurnRosterProjection> candidate =
            new List<DailyV1ActorTurnRosterProjection>(selectedRuntimeActorRoster.Count + 1);
        candidate.AddRange(selectedRuntimeActorRoster);
        candidate.Add(member);
        return TryCreateValidatedSelectedRuntimeActorRoster(
            replacementGraph, candidate, out replacementRoster);
    }

    internal bool TryCreateSelectedRuntimeActorRosterWithoutMember(
        string runtimeId,
        DailyV1SelectedRuntimeOwnerGraph replacementGraph,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        replacementRoster = null;
        if (selectedRuntimeActorRoster == null
            || string.IsNullOrWhiteSpace(runtimeId)
            || replacementGraph == null)
            return false;

        List<DailyV1ActorTurnRosterProjection> candidate =
            new List<DailyV1ActorTurnRosterProjection>(selectedRuntimeActorRoster.Count);
        bool removed = false;
        foreach (DailyV1ActorTurnRosterProjection member in selectedRuntimeActorRoster)
        {
            if (member != null && string.Equals(member.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                if (removed) return false;
                removed = true;
                continue;
            }
            candidate.Add(member);
        }

        return removed && TryCreateValidatedSelectedRuntimeActorRoster(
            replacementGraph, candidate, out replacementRoster);
    }

    /// <summary>
    /// Atomically publishes a new selected graph together with its exact
    /// ordered actor identity roster. Membership callers prepare and validate
    /// both snapshots before invoking this single validation-and-publication point.
    /// </summary>
    internal bool TryReplaceSelectedRuntimeMembership(
        DailyV1SelectedRuntimeOwnerGraph expectedGraph,
        DailyV1SelectedRuntimeOwnerGraph replacementGraph,
        IReadOnlyList<DailyV1ActorTurnRosterProjection> expectedRoster,
        IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        if (!selectedRuntimeOwnerGraphBound
            || expectedGraph == null
            || replacementGraph == null
            || !ReferenceEquals(selectedRuntimeOwnerGraph, expectedGraph)
            || !ReferenceEquals(selectedRuntimeActorRoster, expectedRoster)
            || !ReferenceEquals(expectedGraph.Definitions, replacementGraph.Definitions)
            || !TryCreateValidatedSelectedRuntimeActorRoster(
                replacementGraph, replacementRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> validatedRoster))
            return false;

        DailyV1ActorTurnCoreRoots replacementActorTurnRoots = null;
        if (selectedRuntimeExecutionRootsBound)
        {
            if (selectedActorChoiceStore == null
                || !ReferenceEquals(replacementGraph.PersonStore, selectedActorChoiceStore.PersonStore))
                return false;
            try
            {
                replacementActorTurnRoots = new DailyV1ActorTurnCoreRoots(this, replacementGraph);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        selectedRuntimeOwnerGraph = replacementGraph;
        selectedRuntimeActorRoster = validatedRoster;
        if (replacementActorTurnRoots != null)
            selectedRuntimeActorTurnCoreRoots = replacementActorTurnRoots;
        return true;
    }

    private static bool TryCreateValidatedSelectedRuntimeActorRoster(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IEnumerable<DailyV1ActorTurnRosterProjection> runtimeRoster,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> roster)
    {
        roster = null;
        if (graph == null || runtimeRoster == null) return false;

        List<DailyV1ActorTurnRosterProjection> candidate =
            new List<DailyV1ActorTurnRosterProjection>();
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1ActorTurnRosterProjection member in runtimeRoster)
        {
            if (member != null
                && (string.IsNullOrWhiteSpace(member.RuntimeId)
                    || !runtimeIds.Add(member.RuntimeId)))
                return false;
            candidate.Add(member);
        }

        if (!DailyV1ActorTurnExecutionStage.TryBuildRoster(
                graph, candidate, out IReadOnlyList<DailyV1ActorTurnRosterEntry> entries))
            return false;
        List<DailyV1NpcRuntimeOwners> selectedOwners =
            new List<DailyV1NpcRuntimeOwners>(graph.Npcs.Count);
        foreach (DailyV1ActorTurnRosterEntry entry in entries)
            if (entry?.ActorOwner != null)
                selectedOwners.Add(entry.ActorOwner);
        if (!graph.MatchesExactOwnerInstances(graph.Cities, selectedOwners))
            return false;

        roster = candidate.AsReadOnly();
        return true;
    }

    /// <summary>
    /// Binds the selected runtime's mutable decision roots beside its clock,
    /// admission, graph and capture state. Unity may project these same
    /// instances, but it cannot substitute a different RNG, choice store,
    /// allocator, sequence or decision store at a Core execution boundary.
    /// </summary>
    internal bool TryBindSelectedRuntimeExecutionRoots(
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore)
    {
        if (!runtimeIdentityBound || !runtimeConfigurationBound
            || !selectedRuntimeOwnerGraphBound || selectedRuntimeOwnerGraph == null
            || runtimeIdAllocator == null || randomSource == null
            || recordSequence == null || decisionStore == null || actorChoiceStore == null
            || selectedRuntimeOwnerGraph.PersonStore == null
            || !ReferenceEquals(actorChoiceStore.PersonStore, selectedRuntimeOwnerGraph.PersonStore))
            return false;

        if (selectedRuntimeExecutionRootsBound)
            return ReferenceEquals(selectedRuntimeIdAllocator, runtimeIdAllocator)
                && ReferenceEquals(selectedRandomSource, randomSource)
                && ReferenceEquals(selectedRecordSequence, recordSequence)
                && ReferenceEquals(selectedDecisionStore, decisionStore)
                && ReferenceEquals(selectedActorChoiceStore, actorChoiceStore);

        selectedRuntimeIdAllocator = runtimeIdAllocator;
        selectedRandomSource = randomSource;
        selectedRecordSequence = recordSequence;
        selectedDecisionStore = decisionStore;
        selectedActorChoiceStore = actorChoiceStore;
        try
        {
            selectedRuntimeActorTurnCoreRoots = new DailyV1ActorTurnCoreRoots(
                this, selectedRuntimeOwnerGraph);
        }
        catch (ArgumentException)
        {
            selectedRuntimeIdAllocator = null;
            selectedRandomSource = null;
            selectedRecordSequence = null;
            selectedDecisionStore = null;
            selectedActorChoiceStore = null;
            selectedRuntimeActorTurnCoreRoots = null;
            return false;
        }
        selectedRuntimeExecutionRootsBound = true;
        return true;
    }

    private static bool HasSelectedCityAndNpcOwners(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph.Cities == null || graph.Npcs == null)
            return false;
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            if (city == null || city.Population == null
                || !ReferenceEquals(city.Definitions, graph.Definitions))
                return false;
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
            if (npc == null || npc.Lifecycle == null || npc.Travel == null
                || npc.ActionSlot == null || !ReferenceEquals(npc.Definitions, graph.Definitions))
                return false;
        return true;
    }

    internal bool HasExactSelectedRuntimeExecutionRoots(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        IAuthoritativeRandomSource randomSource,
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore)
    {
        return selectedRuntimeExecutionRootsBound
            && ReferenceEquals(selectedRuntimeOwnerGraph, graph)
            && ReferenceEquals(graph?.PersonStore, personStore)
            && ReferenceEquals(selectedActorChoiceStore, actorChoiceStore)
            && ReferenceEquals(actorChoiceStore?.PersonStore, personStore)
            && ReferenceEquals(selectedRandomSource, randomSource)
            && ReferenceEquals(selectedRuntimeIdAllocator, runtimeIdAllocator)
            && ReferenceEquals(selectedRecordSequence, recordSequence)
            && ReferenceEquals(selectedDecisionStore, decisionStore);
    }

    internal bool HasExactSelectedRuntimeActorTurnRoots(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        IAuthoritativeRandomSource randomSource)
    {
        return selectedRuntimeExecutionRootsBound
            && selectedRuntimeActorTurnCoreRoots != null
            && selectedRuntimeActorTurnCoreRoots.IsBoundTo(
                this, graph, personStore, actorChoiceStore)
            && ReferenceEquals(selectedRuntimeOwnerGraph, graph)
            && ReferenceEquals(graph?.PersonStore, personStore)
            && ReferenceEquals(selectedActorChoiceStore, actorChoiceStore)
            && ReferenceEquals(actorChoiceStore?.PersonStore, personStore)
            && ReferenceEquals(selectedRandomSource, randomSource);
    }

    internal bool HasExactSelectedRuntimeOwnerGraph(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners) =>
        selectedRuntimeOwnerGraph != null
        && ReferenceEquals(selectedRuntimeOwnerGraph.Definitions, definitions)
        && selectedRuntimeOwnerGraph.MatchesExactOwnerInstances(cityOwners, npcOwners);

    internal void InvalidateCurrentCaptureToken()
    {
        currentCaptureToken = null;
    }

    internal bool TryCompareCurrentCaptureToken(
        DailyCaptureEligibilityToken token,
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay,
        IReadOnlyList<OwnerSectionCensusSnapshot> currentOwnerSections,
        long currentMutationEpoch)
    {
        return MatchesBoundRuntimeIdentity(admissionContext, configurationIdentity,
                calendarIdentity, compositionProfile, worldId, absoluteDay)
            && token != null
            && ReferenceEquals(token.RuntimeInstanceIdentity, runtimeInstanceIdentity)
            && ReferenceEquals(token.AdmissionContext, admissionContext)
            && ReferenceEquals(token.ConfigurationIdentity, configurationIdentity)
            && ReferenceEquals(token.CalendarIdentity, calendarIdentity)
            && token.CompositionProfile == compositionProfile
            && ReferenceEquals(token.WorldId, worldId)
            && token.AbsoluteDay == absoluteDay
            && token.CompletedCoreSequence == completedCoreSequence
            && token.CompletedCoreSequence > 0L
            && (token.BoundaryProvenance == DailyCaptureBoundaryProvenance.CompletedAdvance
                || token.BoundaryProvenance == DailyCaptureBoundaryProvenance.RestoredContinuation)
            && token.MutationEpoch == currentMutationEpoch
            && AreOwnerSectionsEqual(token.OwnerSections, currentOwnerSections);
    }

    /// <summary>
    /// Revalidates the current completed boundary while exactly one P12-G
    /// continuation-save operation is registered. The returned owner list is
    /// the exact census source which must be retained by detached staging.
    /// </summary>
    internal bool TryCaptureContinuationSaveBoundaryEvidence(
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
        out DailyCaptureEligibilityToken token,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure)
    {
        token = null;
        ownerSections = null;
        mutationEpoch = 0L;
        failure = DailyCaptureEligibilityFailure.OperationInProgress;
        if (boundary == null || !ReferenceEquals(boundary.ExecutionState, this)
            || !TryAssessCaptureScope(DailyV1CaptureScopeAllowance.ContinuationSave)
            || AdmissionContext == null
            || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !AdmissionContext.IsOwnedByCurrentThread()
            || AdvanceLeaseHeld || !mutationGuard.CanMutate
            || SimulationTime == null || WorldId == null
            || !runtimeIdentityBound || !runtimeConfigurationBound)
            return false;

        DailyCaptureEligibilityToken current = currentCaptureToken;
        if (current == null)
        {
            failure = DailyCaptureEligibilityFailure.NoCompletedBoundary;
            return false;
        }

        if (current.BoundaryProvenance != DailyCaptureBoundaryProvenance.CompletedAdvance
            && current.BoundaryProvenance != DailyCaptureBoundaryProvenance.RestoredContinuation)
        {
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        if (!boundary.TryCaptureContinuationSaveEvidence(
                out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
                out long capturedMutationEpoch,
                out failure))
            return false;

        if (captured == null || capturedMutationEpoch < 0L
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured)
            || !TryCompareCurrentCaptureToken(
                current, AdmissionContext, ConfigurationIdentity, CalendarIdentity,
                CompositionProfile, WorldId, SimulationTime.AbsoluteDay,
                captured, capturedMutationEpoch)
            || !TryAssessCaptureScope(DailyV1CaptureScopeAllowance.ContinuationSave)
            || !AdmissionContext.IsOwnedByCurrentThread()
            || !mutationGuard.CanMutate)
        {
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        token = current;
        ownerSections = captured;
        mutationEpoch = capturedMutationEpoch;
        failure = DailyCaptureEligibilityFailure.None;
        return true;
    }

    internal static bool AreOwnerSectionsEqual(
        IReadOnlyList<OwnerSectionCensusSnapshot> left,
        IReadOnlyList<OwnerSectionCensusSnapshot> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        for (int index = 0; index < left.Count; index++)
        {
            OwnerSectionCensusSnapshot first = left[index];
            OwnerSectionCensusSnapshot second = right[index];
            if (first == null
                || second == null
                || !string.Equals(first.SectionId, second.SectionId, StringComparison.Ordinal)
                || first.SchemaVersion != second.SchemaVersion
                || first.Role != second.Role
                || !ReferenceEquals(first.OwnerInstanceIdentity, second.OwnerInstanceIdentity)
                || first.Cardinality != second.Cardinality
                || first.Revision != second.Revision)
                return false;
        }

        return true;
    }

    internal bool CanRecordCompletedCoreCount(long requestedCoreCount)
    {
        return requestedCoreCount > 0L
            && completedCoreSequence <= long.MaxValue - requestedCoreCount;
    }

    internal bool TryRecordCompletedCore()
    {
        if (completedCoreSequence == long.MaxValue)
            return false;

        completedCoreSequence++;
        return true;
    }

    internal bool TryAcquireAdvanceLease(out DailyV1AdvanceLease lease)
    {
        lease = null;
        if (advanceLease != null)
            return false;

        DailyV1AdvanceLease candidate = new DailyV1AdvanceLease(this);
        advanceLease = candidate;
        advanceLeaseInitialCompletedSequence = completedCoreSequence;
        lease = candidate;
        return true;
    }

    internal bool TryBeginAdvance(bool invalidateCaptureToken, out DailyV1AdvanceLease lease)
    {
        if (!TryAcquireAdvanceLease(out lease)) return false;
        if (invalidateCaptureToken) InvalidateCurrentCaptureToken();
        return true;
    }
    internal void ReleaseAdvanceLease(DailyV1AdvanceLease lease)
    {
        if (ReferenceEquals(advanceLease, lease))
            advanceLease = null;
    }

    internal bool TryInstallRestoredCompletedCoreSequence(long restoredSequence)
    {
        if (restoredSequence <= 0L
            || completedCoreSequence != 0L
            || AdvanceLeaseHeld)
            return false;

        completedCoreSequence = restoredSequence;
        return true;
    }

    internal bool TryCreateCompletedAdvanceToken(
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay,
        long mutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyCaptureEligibilityToken token)
    {
        token = null;
        if (!MatchesBoundRuntimeIdentity(admissionContext, configurationIdentity,
                calendarIdentity, compositionProfile, worldId, absoluteDay)
            || completedCoreSequence <= 0L
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(ownerSections))
            return false;

        token = new DailyCaptureEligibilityToken(
            runtimeInstanceIdentity,
            admissionContext,
            configurationIdentity,
            calendarIdentity,
            compositionProfile,
            worldId,
            absoluteDay,
            completedCoreSequence,
            DailyCaptureBoundaryProvenance.CompletedAdvance,
            mutationEpoch,
            ownerSections);
        currentCaptureToken = token;
        return true;
    }

    /// <summary>
    /// Issues a completed-advance token only for a positive boundary recorded
    /// during this exact held runtime lease. The P12 census remains the source
    /// of owner snapshots and mutation epoch; this state owns the publication
    /// decision and binds the token to its own admitted runtime identities.
    /// </summary>
    internal bool TryIssueCompletedAdvanceCaptureToken(
        long mutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyCaptureEligibilityToken token)
    {
        token = null;
        if (!runtimeIdentityBound
            || !runtimeConfigurationBound
            || AdmissionContext == null
            || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !AdvanceLeaseHeld
            || currentCaptureToken != null
            || completedCoreSequence <= advanceLeaseInitialCompletedSequence
            || SimulationTime == null
            || mutationEpoch < 0L
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(ownerSections))
            return false;

        return TryCreateCompletedAdvanceToken(
            AdmissionContext,
            ConfigurationIdentity,
            CalendarIdentity,
            CompositionProfile,
            WorldId,
            SimulationTime.AbsoluteDay,
            mutationEpoch,
            ownerSections,
            out token);
    }

    internal bool TryMarkFactualReadWorldPublished()
    {
        lock (operationSyncRoot)
        {
            if (AdmissionContext == null
                || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !mutationGuard.CanMutate
                || AdvanceLeaseHeld
                || activeOperationScopes.Count != 0
                || factualReadWorldPublished)
                return false;

            factualReadWorldPublished = true;
            return true;
        }
    }

    /// <summary>
    /// Atomically admits one detached restored boundary in Core. The exact
    /// owner vector and epoch were captured by the host port and compared by
    /// DailyV1Application before this method; the host never receives the
    /// token or controls the factual-read publication latch.
    /// </summary>
    internal bool TryAdmitRestoredContinuationBoundary(
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay,
        long restoredCoreSequence,
        long mutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyCaptureEligibilityToken token)
    {
        token = null;
        lock (operationSyncRoot)
        {
            if (!runtimeIdentityBound || !runtimeConfigurationBound
                || !MatchesBoundRuntimeIdentity(admissionContext,
                    configurationIdentity, calendarIdentity, compositionProfile,
                    worldId, absoluteDay)
                || admissionContext == null
                || admissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || !admissionContext.IsOwnedByCurrentThread()
                || worldId == null
                || restoredCoreSequence <= 0L
                || mutationEpoch < 0L
                || completedCoreSequence != 0L
                || currentCaptureToken != null
                || factualReadWorldPublished
                || !mutationGuard.CanMutate
                || AdvanceLeaseHeld
                || activeOperationScopes.Count != 0
                || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(ownerSections))
                return false;

            DailyCaptureEligibilityToken candidate = new DailyCaptureEligibilityToken(
                runtimeInstanceIdentity,
                admissionContext,
                configurationIdentity,
                calendarIdentity,
                compositionProfile,
                worldId,
                absoluteDay,
                restoredCoreSequence,
                DailyCaptureBoundaryProvenance.RestoredContinuation,
                mutationEpoch,
                ownerSections);

            if (!TryInstallRestoredCompletedCoreSequence(restoredCoreSequence))
                return false;

            currentCaptureToken = candidate;
            factualReadWorldPublished = true;
            token = candidate;
            return true;
        }
    }
}

/// <summary>
/// Idempotent handle for one runtime advance window. Disposing an older handle
/// cannot release a later lease from the same boundary state.
/// </summary>
internal sealed class DailyV1AdvanceLease : IDisposable
{
    private DailyV1ExecutionBoundaryState owner;

    internal DailyV1AdvanceLease(DailyV1ExecutionBoundaryState owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Dispose()
    {
        DailyV1ExecutionBoundaryState current = owner;
        owner = null;
        current?.ReleaseAdvanceLease(this);
    }
}
