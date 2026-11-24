using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// One exact selected actor at its original SimulationRuntime roster position.
/// A null ActorOwner preserves a null Unity roster slot without compacting the
/// ordinals recorded by actor-choice dispositions.
/// </summary>
internal sealed class DailyV1ActorTurnRosterEntry
{
    internal int RosterOrdinal { get; }
    internal PersonId PersonId { get; }
    internal DailyV1NpcRuntimeOwners ActorOwner { get; }

    internal DailyV1ActorTurnRosterEntry(
        int rosterOrdinal,
        PersonId personId,
        DailyV1NpcRuntimeOwners actorOwner)
    {
        if (rosterOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(rosterOrdinal));
        RosterOrdinal = rosterOrdinal;
        PersonId = personId;
        ActorOwner = actorOwner;
    }
}

/// <summary>
/// Host projection of one runtime roster slot. A null projection preserves a
/// null slot; selected runtime identities are joined to the exact Core owner
/// graph by Core before actor execution. Unselected entries retain only their
/// person identity and roster position for choice rejection semantics.
/// </summary>
internal sealed class DailyV1ActorTurnRosterProjection
{
    internal string RuntimeId { get; }
    internal PersonId PersonId { get; }

    internal DailyV1ActorTurnRosterProjection(string runtimeId, PersonId personId)
    {
        RuntimeId = runtimeId;
        PersonId = personId;
    }
}

/// <summary>
/// Core reads spatial-transit truth from the exact P8-C owner bound to its
/// selected owner graph. The host supplies P12 admission/notification, Unity
/// action projection, and presentation ports. For selected Daily-v1 owners,
/// Core executes the admitted actor-turn operations; legacy gameplay callbacks
/// remain only for hosts outside that selected boundary.
/// </summary>
internal interface IDailyV1ActorTurnExecutionBoundary
{
    bool IsReservedToday(DailyV1NpcRuntimeOwners actor);
    void ProcessScheduledDirective(DailyV1NpcRuntimeOwners actor, ScheduledDirective directive);
    bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal);
    void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor);
    void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor);
}

/// <summary>
/// Selected-profile projection/admission ports for the existing Core
/// actor-choice stage. Current-city spatial truth and action eligibility come
/// from the exact Core owner graph; the host supplies only action projection/
/// admission ports.
/// </summary>
internal interface IDailyV1SelectedActorChoiceExecutionContext
{
    bool TryGetActorChoiceExecutionContext(
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1ActorChoiceExecutionBoundary executionBoundary);
}

/// <summary>
/// Optional selected-profile ports for Core to own the complete autonomous
/// action sequence. The host supplies effectful owner-operation boundaries,
/// existing P12 admission adapters, and presentation publication. A Unity
/// host validates its action projection when it creates its dispatch port;
/// projection identity is not part of Core's execution contract.
/// </summary>
internal interface IDailyV1SelectedActorActionExecutionBoundary
    : IDailyV1SelectedActorActionHostPorts
{
    IAuthoritativeRandomSource RandomSource { get; }
    bool UsesSelectedDailyV1ActionExecution(DailyV1NpcRuntimeOwners actor);
}

/// <summary>
/// Optional Core-only P12F commit hook. The hostless selected actor boundary
/// refreshes the live directive census and advances the shared epoch after a
/// terminal directive write; Unity-backed boundaries retain their existing
/// census protocol.
/// </summary>
internal interface IDailyV1SelectedScheduledDirectiveMutationBoundary
{
    bool TryCommitScheduledDirectiveMutation(
        DailyV1NpcRuntimeOwners actor,
        ScheduledDirectiveStore store,
        ScheduledDirective directive,
        long previousRevision);
}

/// <summary>
/// Effect-only callbacks used by the Core-owned selected actor-turn boundary.
/// This contract deliberately excludes action-policy selection and decision
/// roots: those are derived from the admitted Core session and exact graph.
/// </summary>
internal interface IDailyV1SelectedActorActionHostPorts
{
    /// <summary>
    /// True when this host requires an action-object projection to admit and
    /// publish selected Core action slots. A detached Core host may return
    /// false and execute directly against the authoritative owner slot.
    /// </summary>
    bool RequiresActionProjection { get; }
    void LogSkippedDirective(ScheduledDirective directive, string reason);

    bool TryCreateActionProjectionBoundary(
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1SelectedActionProjectionBoundary projectionBoundary);

    // Decision identity roots are supplied by DailyV1ActorTurnCoreRoots for admitted sessions.
    // Legacy actor-choice hosts retain their separate compatibility port.

    bool TryPrepareSelectedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate selected,
        IDailyV1SelectedActionProjectionBoundary projectionBoundary,
        DailyV1NpcActionPayloadOwner payload);

    bool CanCommitSelectedActionSlot(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload);

    bool TryPublishSelectedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload);

    void CancelPreparedSelectedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload);

    void LogSelectedActionDecisionFailure(DailyV1NpcRuntimeOwners actor, string message);

    bool TryPrepareCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor);
    bool CanCommitCurrentActionClear(DailyV1NpcRuntimeOwners actor);
    bool TryPublishCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor);
    void CancelPreparedCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor);
    void LogChosenActionTarget(DailyV1NpcRuntimeOwners actor);
    bool TryGetDispatchBoundary(
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1SelectedActionDispatchBoundary dispatchBoundary);
    bool TryCompleteDispatch(
        DailyV1NpcRuntimeOwners actor,
        DailyV1SelectedActionDispatchResult dispatch,
        out string resultMessage);
    bool TryGetActionStatusBoundary(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcRuntimeOwners target,
        bool reuseExistingOperation,
        out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary boundary);
    bool TryPrepareForcedOutcomeActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload);
    bool TryGetForcedEscapeBoundary(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary);
}

/// <summary>
/// Projection-only callbacks for a captured one-shot actor choice. Owner
/// clearing, decision identity lookup, and action execution remain Core-owned.
/// </summary>
internal interface IDailyV1SelectedActorChoiceHostPorts
{
    bool TryPrepareRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload);
    bool TryRefreshRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload);
    bool CanCommitRequestedActionSlot(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload);
    bool TryPublishRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload);
    void CancelPreparedRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload);
    void LogRequestedActionDecisionFailure(string message);
}

/// <summary>
/// Host-side effect and census ports for the admitted actor-turn stage. Core
/// owns the action-slot P12 admission and mutation publication; the host
/// supplies its census protocol and confirms that a required Unity projection
/// has been prepared before the Core owner is committed.
/// </summary>
internal interface IDailyV1SelectedActorTurnHostPorts
    : IDailyV1SelectedActorActionHostPorts,
      IDailyV1SelectedActorChoiceHostPorts,
      IDailyV1SelectedMerchantTradeStateP12HostPorts,
      IDailyV1SelectedMerchantTradeStateAdmissionBoundary,
      IDailyV1SelectedCrimeJusticeDailyProjectionPorts,
      IDailyV1SelectedCrimeJusticeDailyMutationHostPort
{
    IDailyV1SelectedP12NpcActionOwnerMutationProtocol ActionSlotP12MutationProtocol { get; }

    bool IsSelectedActionSlotProjectionPrepared(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor);
}

/// <summary>
/// Exact census identities needed by the selected Crime/Justice operation.
/// Core validates the exact Core Justice-owner relationships and pins this
/// compatibility binding to the admitted execution session. A host may supply
/// its existing census binding; a Core-only turn derives the same opaque tokens
/// from the sealed Core registry. Operations do not rediscover identities from
/// Unity.
/// </summary>
internal sealed class DailyV1SelectedCrimeJusticeDailyMutationHostBinding
{
    internal IDailyV1P12CrimeJusticeMutationProtocol Protocol { get; }
    internal object CrimeOwnerIdentity { get; }
    internal object CrimeReceiptCensusOwnerIdentity { get; }
    internal DailyV1JusticeRecordsOwner CrimeJusticeRecordsOwner { get; }
    internal object JusticeOwnerIdentity { get; }
    internal object JusticeReceiptCensusOwnerIdentity { get; }
    internal DailyV1JusticeRecordsOwner JusticeRecordsOwner { get; }
    internal bool AdmissionEnabled { get; }

    internal DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
        IDailyV1P12CrimeJusticeMutationProtocol protocol,
        object crimeOwnerIdentity,
        object crimeReceiptCensusOwnerIdentity,
        DailyV1JusticeRecordsOwner crimeJusticeRecordsOwner,
        object justiceOwnerIdentity,
        object justiceReceiptCensusOwnerIdentity,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        bool admissionEnabled)
    {
        Protocol = protocol;
        CrimeOwnerIdentity = crimeOwnerIdentity;
        CrimeReceiptCensusOwnerIdentity = crimeReceiptCensusOwnerIdentity;
        CrimeJusticeRecordsOwner = crimeJusticeRecordsOwner;
        JusticeOwnerIdentity = justiceOwnerIdentity;
        JusticeReceiptCensusOwnerIdentity = justiceReceiptCensusOwnerIdentity;
        JusticeRecordsOwner = justiceRecordsOwner;
        AdmissionEnabled = admissionEnabled;
    }

    internal bool HasExactCoreOwnerRelationships(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (!AdmissionEnabled || graph?.JusticeRecordsOwner == null || Protocol == null)
            return false;

        if ((CrimeOwnerIdentity == null) != (CrimeReceiptCensusOwnerIdentity == null)
            || (CrimeOwnerIdentity != null
                && (!ReferenceEquals(CrimeOwnerIdentity, CrimeReceiptCensusOwnerIdentity)
                    || !ReferenceEquals(CrimeJusticeRecordsOwner, graph.JusticeRecordsOwner))))
            return false;

        if ((JusticeOwnerIdentity == null) != (JusticeReceiptCensusOwnerIdentity == null)
            || (JusticeOwnerIdentity != null
                && (!ReferenceEquals(JusticeOwnerIdentity, JusticeReceiptCensusOwnerIdentity)
                    || !ReferenceEquals(JusticeRecordsOwner, graph.JusticeRecordsOwner))))
            return false;

        return true;
    }

    internal bool HasSameSessionBinding(
        DailyV1SelectedCrimeJusticeDailyMutationHostBinding other) =>
        other != null
        && ReferenceEquals(Protocol, other.Protocol)
        && HasSameOwnerBindings(other);

    internal bool HasSameOwnerBindings(
        DailyV1SelectedCrimeJusticeDailyMutationHostBinding other) =>
        other != null
        && ReferenceEquals(CrimeOwnerIdentity, other.CrimeOwnerIdentity)
        && ReferenceEquals(CrimeReceiptCensusOwnerIdentity, other.CrimeReceiptCensusOwnerIdentity)
        && ReferenceEquals(CrimeJusticeRecordsOwner, other.CrimeJusticeRecordsOwner)
        && ReferenceEquals(JusticeOwnerIdentity, other.JusticeOwnerIdentity)
        && ReferenceEquals(JusticeReceiptCensusOwnerIdentity, other.JusticeReceiptCensusOwnerIdentity)
        && ReferenceEquals(JusticeRecordsOwner, other.JusticeRecordsOwner)
        && AdmissionEnabled == other.AdmissionEnabled;
}

/// <summary>
/// Supplies exact external census bindings for a selected Crime/Justice
/// operation. It does not execute or reserve the operation.
/// </summary>
internal interface IDailyV1SelectedCrimeJusticeDailyMutationHostPort
{
    bool TryGetSelectedCrimeJusticeDailyMutationHostBinding(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding);
}

/// <summary>
/// P12 operation adapter for the Core-only Merchant daily pass. The admitted
/// Core census registry supplies the exact seven-row owner cut and baselines;
/// this adapter only opens the already-inventoried operation scope and keeps
/// the registry synchronized when Core commits plan, spatial, or knowledge
/// rows.
/// </summary>
internal sealed class DailyV1CoreSelectedP12MerchantDailyTradeProtocol
    : IDailyV1SelectedP12MerchantDailyTradeProtocol
{
    private sealed class OperationScope : IDisposable
    {
        private readonly DailyV1ExecutionBoundaryState state;
        private readonly DailyV1CoreSelectedP12MerchantDailyTradeProtocol owner;
        private bool disposed;

        internal OperationScope(
            DailyV1ExecutionBoundaryState state,
            DailyV1CoreSelectedP12MerchantDailyTradeProtocol owner)
        {
            this.state = state;
            this.owner = owner;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (!state.TryEndRuntimeProtocolOperation()) owner.FaultClosed();
        }
    }

    private readonly DailyV1ExecutionBoundaryState state;
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;
    internal string LastFailure { get; private set; }

    internal DailyV1CoreSelectedP12MerchantDailyTradeProtocol(
        DailyV1ExecutionBoundaryState state,
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    public bool TryValidateOwnerSections(
        DailyV1SelectedRuntimeOwnerGraph candidateGraph,
        DailyV1NpcRuntimeOwners actor,
        IReadOnlyList<string> sectionIds)
    {
        LastFailure = null;
        if (!IsBound())
        {
            LastFailure = "ProtocolNotBound:faulted=" + state.IsSelectedRuntimeCensusProtocolFaulted;
            return false;
        }
        if (!ReferenceEquals(candidateGraph, graph))
        {
            LastFailure = "GraphMismatch";
            return false;
        }
        if (!state.TryCreateSelectedP12MerchantDailyTradeSectionIds(
                graph, actor, out IReadOnlyList<string> expected)
            || sectionIds == null || sectionIds.Count != expected.Count)
        {
            LastFailure = "OwnerSectionCut";
            return false;
        }

        HashSet<string> expectedIds = new HashSet<string>(expected, StringComparer.Ordinal);
        HashSet<string> suppliedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in sectionIds)
            if (string.IsNullOrWhiteSpace(sectionId)
                || !expectedIds.Contains(sectionId)
                || !suppliedIds.Add(sectionId))
            {
                LastFailure = "OwnerSectionSet";
                return false;
            }

        bool valid = suppliedIds.Count == expectedIds.Count
            && state.TryValidateCoreSelectedOwnerSectionCensusRows(graph, expected, null);
        if (!valid) LastFailure = "OwnerSectionBaseline";
        return valid;
    }

    public bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds)
    {
        LastFailure = null;
        bool valid = IsBound() && sectionIds != null && sectionIds.Count > 0
            && state.TryValidateCoreSelectedOwnerSectionCensusRows(graph, sectionIds, null);
        if (!valid) LastFailure = "UnchangedOwnerSectionBaseline";
        return valid;
    }

    public bool TryEnterOperation(out IDisposable operationScope)
    {
        operationScope = null;
        if (!IsBound()
            || !state.TryBeginRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.MerchantDailyNpcTrade))
            return false;

        operationScope = new OperationScope(state, this);
        return true;
    }

    public bool TryRefreshCommittedMutationBaselines(
        IReadOnlyCollection<string> sectionIds) =>
        IsBound() && sectionIds != null && sectionIds.Count > 0
        && state.TryRefreshCoreSelectedOwnerSectionCensusRows(graph, sectionIds, null);

    public void FaultClosed()
    {
        state.MarkSelectedRuntimeCensusProtocolFaulted();
        state.InvalidateCurrentCaptureToken();
    }

    private bool IsBound() => state.HasAdmittedSelectedDailyExecution
        && state.AdmissionContext?.IsOwnedByCurrentThread() == true
        && state.HasRuntimeProtocolOperationInventory
        && state.HasSealedSelectedOwnerSectionCensusRegistry
        && !state.IsSelectedRuntimeCensusProtocolFaulted
        && ReferenceEquals(state.SelectedRuntimeOwnerGraph, graph);
}

/// <summary>
/// Core-created admitted actor-turn boundary. The exact owner graph, roots,
/// PersonStore, ActorChoiceStore, and roster come from the bound execution
/// state; Unity can provide only effect ports and cannot replace actor policy.
/// </summary>
internal sealed class DailyV1CoreSelectedActorTurnExecutionBoundary
    : IDailyV1ActorTurnExecutionBoundary,
      IDailyV1SelectedActorChoiceExecutionContext,
      IDailyV1CoreActorChoiceExecutionBoundary,
      IDailyV1SelectedMerchantTradeStateP12HostPorts,
      IDailyV1SelectedMerchantTradeStateAdmissionBoundary,
      IDailyV1SelectedActionSlotP12MutationBoundary,
      IDailyV1SelectedCrimeJusticeDailyBoundary,
      IDailyV1SelectedScheduledDirectiveMutationBoundary
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;
    private readonly PersonStore personStore;
    private readonly ActorChoiceStore actorChoiceStore;
    private readonly DailyV1ActorTurnCoreRoots coreRoots;
    private readonly IReadOnlyCollection<DailyV1NpcRuntimeOwners> exactRosterOwners;
    private readonly IDailyV1SelectedActorTurnHostPorts hostPorts;
    private readonly DailyV1CoreSelectedP12MerchantDailyTradeProtocol coreMerchantDailyTradeProtocol;
    private readonly IDailyV1SelectedP12NpcActionOwnerMutationProtocol actionSlotP12MutationProtocol;
    private IDailyV1SelectedMerchantTradeStateBoundary merchantBoundary;
    private string lastActionSlotP12Failure;
    internal string LastActionStatusBoundaryRejection { get; private set; }
    internal bool UsesCoreOnlyActorTurnPath => hostPorts == null;
    internal string LastActionSlotP12Failure =>
        lastActionSlotP12Failure
        ?? executionState.LastCoreDecisionMutationAdmissionRejection
        ?? executionState.LastSelectedOwnerSectionCensusRefreshRejection;
    internal string LastMerchantP12Failure =>
        executionState.LastSelectedMerchantDailyTradeRejection;

    private DailyV1CoreSelectedActorTurnExecutionBoundary(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        DailyV1ActorTurnCoreRoots coreRoots,
        IReadOnlyCollection<DailyV1NpcRuntimeOwners> exactRosterOwners,
        IDailyV1SelectedActorTurnHostPorts hostPorts,
        IDailyV1SelectedP12NpcActionOwnerMutationProtocol actionSlotP12MutationProtocol)
    {
        this.executionState = executionState;
        this.graph = graph;
        this.personStore = personStore;
        this.actorChoiceStore = actorChoiceStore;
        this.coreRoots = coreRoots;
        this.exactRosterOwners = exactRosterOwners;
        this.hostPorts = hostPorts;
        coreMerchantDailyTradeProtocol = hostPorts == null
            ? new DailyV1CoreSelectedP12MerchantDailyTradeProtocol(executionState, graph)
            : null;
        this.actionSlotP12MutationProtocol = actionSlotP12MutationProtocol;
    }

    internal static bool TryCreate(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        DailyV1ActorTurnCoreRoots coreRoots,
        IDailyV1SelectedActorTurnHostPorts hostPorts,
        out DailyV1CoreSelectedActorTurnExecutionBoundary boundary)
    {
        return TryCreateCore(
            executionState, graph, personStore, actorChoiceStore, coreRoots,
            hostPorts, out boundary);
    }

    internal static bool TryCreateCoreOnly(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        DailyV1ActorTurnCoreRoots coreRoots,
        out DailyV1CoreSelectedActorTurnExecutionBoundary boundary)
    {
        return TryCreateCore(
            executionState, graph, personStore, actorChoiceStore, coreRoots,
            null, out boundary);
    }

    private static bool TryCreateCore(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        DailyV1ActorTurnCoreRoots coreRoots,
        IDailyV1SelectedActorTurnHostPorts hostPorts,
        out DailyV1CoreSelectedActorTurnExecutionBoundary boundary)
    {
        boundary = null;
        IDailyV1SelectedP12NpcActionOwnerMutationProtocol actionSlotP12MutationProtocol =
            hostPorts?.ActionSlotP12MutationProtocol;
        bool hasSealedCoreCensus = executionState != null
            && executionState.HasSealedSelectedOwnerSectionCensusRegistry;
        if ((actionSlotP12MutationProtocol == null && !hasSealedCoreCensus)
            || executionState == null || graph == null || personStore == null
            || actorChoiceStore == null || coreRoots == null
            || (hostPorts == null && !hasSealedCoreCensus)
            || !executionState.HasAdmittedSelectedDailyExecution
            || !executionState.SelectedRuntimeExecutionRootsBound
            || !executionState.HasRuntimeProtocolOperationInventory
            || executionState.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
            || !ReferenceEquals(graph.PersonStore, personStore)
            || !ReferenceEquals(actorChoiceStore.PersonStore, personStore)
            || !ReferenceEquals(executionState.SelectedActorChoiceStore, actorChoiceStore)
            || !coreRoots.IsBoundTo(executionState, graph, personStore, actorChoiceStore)
            || !executionState.HasExactSelectedRuntimeActorTurnRoots(
                graph, personStore, actorChoiceStore, coreRoots.RandomSource)
            || executionState.SelectedRuntimeActorRoster == null)
            return false;

        if (!DailyV1ActorTurnExecutionStage.TryBuildRoster(
                graph, executionState.SelectedRuntimeActorRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterEntry> roster))
            return false;

        List<DailyV1NpcRuntimeOwners> selectedOwners = new List<DailyV1NpcRuntimeOwners>();
        foreach (DailyV1ActorTurnRosterEntry entry in roster)
            if (entry.ActorOwner != null)
                selectedOwners.Add(entry.ActorOwner);
        if (!graph.MatchesExactOwnerInstances(graph.Cities, selectedOwners))
            return false;

        boundary = new DailyV1CoreSelectedActorTurnExecutionBoundary(
            executionState, graph, personStore, actorChoiceStore, coreRoots,
            selectedOwners.AsReadOnly(), hostPorts,
            actionSlotP12MutationProtocol);
        return true;
    }

    internal bool IsBoundTo(
        DailyV1ExecutionBoundaryState expectedState,
        DailyV1SelectedRuntimeOwnerGraph expectedGraph,
        DailyV1ActorTurnCoreRoots expectedRoots) =>
        ReferenceEquals(executionState, expectedState)
        && ReferenceEquals(graph, expectedGraph)
        && ReferenceEquals(coreRoots, expectedRoots)
        && executionState.HasAdmittedSelectedDailyExecution
        && executionState.SelectedRuntimeExecutionRootsBound
        && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
        && coreRoots.IsBoundTo(executionState, graph, personStore, actorChoiceStore)
        && ReferenceEquals(executionState.SelectedActorChoiceStore, actorChoiceStore)
        && ReferenceEquals(actorChoiceStore.PersonStore, personStore)
        && ReferenceEquals(graph.PersonStore, personStore);

    internal void BindMerchantBoundary(IDailyV1SelectedMerchantTradeStateBoundary selectedBoundary)
    {
        if (selectedBoundary != null && merchantBoundary != null)
            throw new InvalidOperationException("The Core actor-turn Merchant boundary is already bound.");
        merchantBoundary = selectedBoundary;
    }

    private bool TryGetExactActor(DailyV1NpcRuntimeOwners actor)
    {
        if (!IsBoundTo(executionState, graph, coreRoots)
            || actor == null || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions))
            return false;
        foreach (DailyV1NpcRuntimeOwners selected in exactRosterOwners)
            if (ReferenceEquals(selected, actor))
                return true;
        return false;
    }

    private void RequireExactActor(DailyV1NpcRuntimeOwners actor)
    {
        if (!TryGetExactActor(actor))
            throw new InvalidOperationException(
                "The Core actor-turn boundary received an owner outside its exact admitted roster and graph.");
    }

    private void RequireBoundMerchant()
    {
        if (merchantBoundary == null
            || !(merchantBoundary is DailyV1CoreSelectedMerchantTradeStateBoundary coreMerchant)
            || !coreMerchant.IsBoundTo(graph, coreRoots))
            throw new InvalidOperationException(
                "The admitted actor turn has no exact Core Merchant state boundary.");
    }

    internal bool HasHostEffectPorts => hostPorts != null;

    private IDailyV1SelectedActorTurnHostPorts RequireHostPorts() =>
        hostPorts ?? throw new InvalidOperationException(
            "This Core actor-turn boundary has no host effect ports for the requested operation.");

    // Core actor-turn policy. These legacy callbacks are deliberately not
    // delegated to the Unity effect adapter.
    public bool IsReservedToday(DailyV1NpcRuntimeOwners actor)
    {
        RequireExactActor(actor);
        return false;
    }

    public void ProcessScheduledDirective(DailyV1NpcRuntimeOwners actor, ScheduledDirective directive) =>
        throw new InvalidOperationException(
            "Admitted Daily-v1 directives are processed by the Core actor-turn stage.");

    public bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal) =>
        throw new InvalidOperationException(
            "Admitted Daily-v1 choices are processed by the Core actor-turn stage.");

    public void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor)
    {
        RequireExactActor(actor);
        RequireBoundMerchant();
        long absoluteDay = executionState.SimulationTime.AbsoluteDay;
        if (!ExecuteWithinSelectedMerchantDailyOperation(
                actor,
                () => DailyV1SelectedMerchantTradeStateOperation.TryAdvance(
                    graph, actor, absoluteDay, merchantBoundary, coreRoots)))
            throw new InvalidOperationException(
                "The admitted Core Merchant daily operation could not complete.");
    }

    public void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor)
    {
        RequireExactActor(actor);
        if (!DailyV1SelectedActorActionExecutionStage.TryExecuteAutonomousAction(
                graph, actor, personStore, coreRoots.RandomSource,
                executionState.SimulationTime.AbsoluteDay, this, coreRoots))
            throw new InvalidOperationException(
                "The admitted Core actor action could not complete.");
    }

    public IAuthoritativeRandomSource RandomSource => coreRoots.RandomSource;
    public bool RequiresActionProjection => hostPorts?.RequiresActionProjection ?? false;
    public bool UsesSelectedDailyV1ActionExecution(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor);

    public bool TryCommitScheduledDirectiveMutation(
        DailyV1NpcRuntimeOwners actor,
        ScheduledDirectiveStore store,
        ScheduledDirective directive,
        long previousRevision)
    {
        if (!TryGetExactActor(actor)
            || !ReferenceEquals(store, graph.ScheduledDirectives)
            || directive == null || !directive.IsOwnedBy(store)
            || directive.IsPending || previousRevision < 0L
            || previousRevision == long.MaxValue)
            return false;

        long committedRevision = previousRevision + 1L;
        if (hostPorts != null) return store.Revision == committedRevision;
        if (store.Revision != committedRevision) return false;

        DailyV1CoreSelectedOwnerSectionMutationProtocol protocol =
            new DailyV1CoreSelectedOwnerSectionMutationProtocol(executionState, graph);
        if (!protocol.TryRefreshCommittedMutationBaselines(
                new[] { ScheduledDirectiveCensusProvider.SectionId }))
        {
            string rejection = executionState.LastSelectedOwnerSectionCensusRefreshRejection
                ?? "Core census refresh precondition rejected the complete owner vector.";
            protocol.FaultClosed();
            throw new InvalidOperationException(
                "P12F could not publish the terminal directive row: " + rejection);
        }
        if (!executionState.MutationEpochState.TryAdvanceCommittedMutation())
        {
            protocol.FaultClosed();
            throw new InvalidOperationException(
                "P12F refreshed the terminal directive row but could not advance the shared mutation epoch.");
        }
        return true;
    }

    public bool TryGetActorChoiceExecutionContext(
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1ActorChoiceExecutionBoundary executionBoundary)
    {
        executionBoundary = null;
        if (!TryGetExactActor(actor)) return false;
        executionBoundary = this;
        return true;
    }

    public void ClearCurrentAction(DailyV1NpcRuntimeOwners actor)
    {
        RequireExactActor(actor);
        if (!DailyV1SelectedActorActionExecutionStage.TryClearCurrentAction(graph, actor, this))
            throw new InvalidOperationException(
                "The Core actor-turn boundary could not clear its exact action owner and projection.");
    }

    public bool TryGetDecisionRecordOwners(
        DailyV1NpcRuntimeOwners actor,
        out RuntimeIdAllocator idAllocator,
        out SimulationRecordSequence recordSequence,
        out NpcDecisionStore decisionStore)
    {
        idAllocator = null;
        recordSequence = null;
        decisionStore = null;
        if (!TryGetExactActor(actor) || !coreRoots.IsBoundToGraph(graph)) return false;
        idAllocator = coreRoots.IdAllocator;
        recordSequence = coreRoots.RecordSequence;
        decisionStore = coreRoots.DecisionStore;
        return idAllocator != null && recordSequence != null && decisionStore != null;
    }

    public NpcActionResultType? ExecuteCurrentAction(DailyV1NpcRuntimeOwners actor) =>
        throw new InvalidOperationException(
            "Admitted Daily-v1 actor choices execute through the Core action stage, not a host callback.");

    // Narrow host effect ports. Every call is fenced to the exact selected actor.
    public void LogSkippedDirective(ScheduledDirective directive, string reason) =>
        RequireHostPorts().LogSkippedDirective(directive, reason);

    public bool TryCreateActionProjectionBoundary(
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1SelectedActionProjectionBoundary projectionBoundary)
    {
        projectionBoundary = null;
        return TryGetExactActor(actor)
            && RequireHostPorts().TryCreateActionProjectionBoundary(actor, out projectionBoundary)
            && projectionBoundary != null;
    }

    public bool TryPrepareSelectedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate selected,
        IDailyV1SelectedActionProjectionBoundary projectionBoundary,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor)
        && RequireHostPorts().TryPrepareSelectedActionProjection(
            actor, selected, projectionBoundary, payload);

    public bool CanCommitSelectedActionSlot(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && RequireHostPorts().CanCommitSelectedActionSlot(actor, payload);

    public bool TryPublishSelectedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && RequireHostPorts().TryPublishSelectedActionProjection(actor, payload);

    public void CancelPreparedSelectedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload)
    {
        RequireExactActor(actor);
        RequireHostPorts().CancelPreparedSelectedActionProjection(actor, payload);
    }

    public void LogSelectedActionDecisionFailure(DailyV1NpcRuntimeOwners actor, string message)
    {
        RequireExactActor(actor);
        if (hostPorts != null) hostPorts.LogSelectedActionDecisionFailure(actor, message);
    }

    public bool TryPrepareCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor) && RequireHostPorts().TryPrepareCurrentActionClearProjection(actor);

    public bool CanCommitCurrentActionClear(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor) && RequireHostPorts().CanCommitCurrentActionClear(actor);

    public bool TryPublishCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor) && RequireHostPorts().TryPublishCurrentActionClearProjection(actor);

    public void CancelPreparedCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor)
    {
        RequireExactActor(actor);
        RequireHostPorts().CancelPreparedCurrentActionClearProjection(actor);
    }

    public void LogChosenActionTarget(DailyV1NpcRuntimeOwners actor)
    {
        RequireExactActor(actor);
        // Targetless Core actions have no host projection to log. The Unity
        // adapter uses the same no-op when it has no optional projection.
        if (hostPorts != null) hostPorts.LogChosenActionTarget(actor);
    }

    public bool TryGetDispatchBoundary(
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1SelectedActionDispatchBoundary dispatchBoundary)
    {
        dispatchBoundary = null;
        if (!TryGetExactActor(actor)) return false;
        if (hostPorts != null)
            return hostPorts.TryGetDispatchBoundary(actor, out dispatchBoundary)
                && dispatchBoundary != null;
        if (!RequiresActionProjection
            && IsExactCoreOnlyActorDispatchPayload(actor, actor.ActionSlot?.CurrentPayload))
        {
            dispatchBoundary = new DailyV1CoreOnlyActorActionDispatchBoundary(this, actor);
            return true;
        }
        return !RequiresActionProjection
            && DailyV1CoreOnlyCrimeActionBoundary.TryCreate(
                executionState, graph, actor, out dispatchBoundary);
    }

    public bool TryCompleteDispatch(
        DailyV1NpcRuntimeOwners actor,
        DailyV1SelectedActionDispatchResult dispatch,
        out string resultMessage)
    {
        resultMessage = null;
        if (!TryGetExactActor(actor) || dispatch == null) return false;
        if (hostPorts != null
            && hostPorts.TryCompleteDispatch(actor, dispatch, out resultMessage)) return true;
        if (RequiresActionProjection) return false;

        DailyV1ActionDefinition action = actor.ActionSlot?.CurrentAction;
        DailyV1NpcActionPayloadOwner payload = actor.ActionSlot?.CurrentPayload;
        if (dispatch.Kind == DailyV1SelectedActionDispatchKind.OpenMarket)
            return dispatch.OpenMarket != null
                && dispatch.ActionRollSucceeded == dispatch.OpenMarket.ActionRollSucceeded
                && dispatch.Succeeded == (dispatch.ActionRollSucceeded
                    && dispatch.OpenMarket.Succeeded)
                && ((dispatch.OpenMarket.Sale != null
                        && dispatch.OpenMarket.Purchase == null
                        && IsExactCoreLocalSellGoodsPayload(actor, payload))
                    || (dispatch.OpenMarket.Purchase != null
                        && dispatch.OpenMarket.Sale == null
                        && IsExactCoreLocalBuyGoodsPayload(actor, payload))
                    || (!dispatch.ActionRollSucceeded
                        && dispatch.OpenMarket.Sale == null
                        && dispatch.OpenMarket.Purchase == null
                        && (IsExactCoreLocalSellGoodsPayload(actor, payload)
                            || IsExactCoreLocalBuyGoodsPayload(actor, payload))));
        if (dispatch.Kind == DailyV1SelectedActionDispatchKind.CrossCityBuy)
        {
            bool resultPresent = dispatch.CrossCityBuy != null;
            bool rollMatches = resultPresent
                && dispatch.ActionRollSucceeded
                    == dispatch.CrossCityBuy.ActionRollSucceeded;
            if (!resultPresent || !rollMatches) return false;
            if (dispatch.CrossCityBuy.Purchase?.PartialCommit == true)
            {
                executionState.MarkSelectedRuntimeCensusProtocolFaulted();
                executionState.InvalidateCurrentCaptureToken();
                return false;
            }
            bool successMatches = resultPresent
                && dispatch.Succeeded == (dispatch.ActionRollSucceeded
                    && dispatch.CrossCityBuy.Purchase?.Succeeded == true);
            bool exactCrossCity = IsExactCoreCrossCityBuyPayload(actor, payload);
            if (!successMatches || !exactCrossCity)
                return false;
            return true;
        }
        if (dispatch.Kind == DailyV1SelectedActionDispatchKind.GuardArrest)
            return dispatch.GuardArrest != null
                && dispatch.Succeeded == (dispatch.ActionRollSucceeded && dispatch.GuardArrest.Succeeded)
                && DailyV1CoreOnlyCrimeActionBoundary.IsExactGuardArrestActionSlot(
                    graph, actor, action, payload);

        if (dispatch.Kind == DailyV1SelectedActionDispatchKind.FleeCity)
            return dispatch.FleeCity != null
                && dispatch.ActionRollSucceeded
                && dispatch.Succeeded == dispatch.FleeCity.Succeeded
                && string.Equals(dispatch.FleeCity.DestinationCityRuntimeId,
                    payload?.TargetCityRuntimeId, StringComparison.Ordinal)
                && DailyV1CoreOnlyCrimeActionBoundary.IsExactFleeCityActionSlot(
                    graph, actor, action, payload);

        if (dispatch.Kind == DailyV1SelectedActionDispatchKind.Crime)
            return dispatch.Crime != null
                && dispatch.ActionRollSucceeded == dispatch.Crime.ActionRollSucceeded
                && dispatch.Succeeded == dispatch.Crime.Succeeded
                && (DailyV1CoreOnlyCrimeActionBoundary.IsExactTargetlessHideSlot(
                        graph, actor, action, payload)
                    || DailyV1CoreOnlyCrimeActionBoundary.IsExactEscapeActionSlot(
                        graph, actor, action, payload));
        if (dispatch.Kind == DailyV1SelectedActionDispatchKind.Steal)
            return dispatch.Steal != null
                && dispatch.Succeeded == dispatch.Steal.Succeeded
                && string.Equals(dispatch.Steal.TargetRuntimeId,
                    payload?.TargetNpcRuntimeId, StringComparison.Ordinal)
                && string.Equals(dispatch.Steal.CityRuntimeId,
                    actor.Travel?.CurrentCityRuntimeId, StringComparison.Ordinal)
                && DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
                && action.Values.actionType?.Value == DailyV1NpcDefinitionRules.StealActionTypeValue
                && payload?.TargetItem == null
                && payload?.TargetCityRuntimeId == null;

        if (dispatch.Kind == DailyV1SelectedActionDispatchKind.SoloTravel)
            return dispatch.SoloTravel != null
                && dispatch.ActionRollSucceeded
                    == dispatch.SoloTravel.ActionRollSucceeded
                && dispatch.Succeeded
                    == (dispatch.ActionRollSucceeded && dispatch.SoloTravel.Succeeded)
                && DailyV1CoreSelectedActorTurnExecutionBoundary
                    .IsExactCoreOnlySoloTravelDispatchResult(
                        graph, actor, action, payload, dispatch.SoloTravel,
                        requireInstalledSlot: true);

        if (dispatch.Kind != DailyV1SelectedActionDispatchKind.Normal) return false;
        // Normal slot effects are committed separately through the exact Core
        // P12 status-owner boundary.
        if (DailyV1SelectedActorActionExecutionStage.IsProjectionIndependentNormalAction(
                graph, actor, action))
            return true;

        return !string.IsNullOrWhiteSpace(payload?.TargetNpcRuntimeId)
            && graph.TryGetNpcOwner(payload.TargetNpcRuntimeId,
                out DailyV1NpcRuntimeOwners target)
            && DailyV1SelectedActorActionExecutionStage
                .IsProjectionIndependentNormalActionForExactTarget(
                    graph, actor, action, target);
    }

    public bool TryGetActionStatusBoundary(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcRuntimeOwners target,
        bool reuseExistingOperation,
        out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary boundary)
    {
        LastActionStatusBoundaryRejection = null;
        boundary = null;
        if (!TryGetExactActor(actor)
            || (target != null && !graph.ContainsExactNpcOwner(target)))
        {
            LastActionStatusBoundaryRejection = "ExactActorOrTarget";
            return false;
        }
        if (hostPorts != null && hostPorts.TryGetActionStatusBoundary(
                actor, target, reuseExistingOperation, out boundary)
            && boundary != null)
            return true;
        boundary = null;
        if (RequiresActionProjection)
        {
            LastActionStatusBoundaryRejection = "RequiredHostProjectionUnavailable";
            return false;
        }
        if (!reuseExistingOperation)
        {
            bool created = DailyV1SelectedActionExecutionStage
                .CoreSelectedActionStatusOperationBoundary.TryCreate(
                    executionState, graph, null, actor, target, null,
                    out boundary, out string rejection);
            if (!created || boundary == null)
            {
                LastActionStatusBoundaryRejection = rejection
                    ?? "CoreActionStatusBoundaryUnavailable";
                if (executionState.IsSelectedRuntimeCensusProtocolFaulted)
                {
                    string censusRejection =
                        executionState.LastSelectedOwnerSectionCensusRefreshRejection;
                    string mutationRejection =
                        executionState.LastCoreDecisionMutationAdmissionRejection;
                    string soloTravelRejection =
                        executionState.LastSelectedP12SoloTravelStartRejection;
                    if (!string.IsNullOrWhiteSpace(censusRejection))
                        LastActionStatusBoundaryRejection +=
                            "; Census=" + censusRejection;
                    if (!string.IsNullOrWhiteSpace(mutationRejection))
                        LastActionStatusBoundaryRejection +=
                            "; Mutation=" + mutationRejection;
                    if (!string.IsNullOrWhiteSpace(soloTravelRejection))
                        LastActionStatusBoundaryRejection +=
                            "; SoloTravel=" + soloTravelRejection;
                }
                return false;
            }
            return true;
        }

        bool reused = DailyV1SelectedActionExecutionStage
            .CoreSelectedActionStatusOperationBoundary.TryCreateForForcedEscapeScopeReuse(
                executionState, graph, actor, target, out boundary);
        if (!reused || boundary == null)
        {
            LastActionStatusBoundaryRejection =
                "ForcedEscapeScopeReuseUnavailable";
            return false;
        }
        return true;
    }

    public bool TryPrepareForcedOutcomeActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor)
        && RequireHostPorts().TryPrepareForcedOutcomeActionProjection(actor, action, payload);

    public bool TryGetForcedEscapeBoundary(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
    {
        boundary = null;
        if (!TryGetExactActor(actor)) return false;
        if (hostPorts != null)
            return hostPorts.TryGetForcedEscapeBoundary(actor, action, out boundary)
                && boundary != null;
        return !RequiresActionProjection
            && DailyV1CoreOnlyCrimeActionBoundary.TryCreateForcedEscapeBoundary(
                executionState, graph, actor, action, out boundary)
            && boundary != null;
    }

    public bool TryPrepareRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload)
    {
        if (hostPorts != null)
            return TryGetExactActor(actor)
                && hostPorts.TryPrepareRequestedActionProjection(actor, payload);
        return IsExactCoreLocalSellGoodsPayload(actor, payload);
    }

    public bool TryRefreshRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload)
    {
        if (hostPorts != null)
            return TryGetExactActor(actor)
                && hostPorts.TryRefreshRequestedActionProjection(actor, payload);
        return IsExactCoreLocalSellGoodsPayload(actor, payload);
    }

    public bool CanCommitRequestedActionSlot(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload)
    {
        if (hostPorts != null)
            return TryGetExactActor(actor)
                && hostPorts.CanCommitRequestedActionSlot(actor, payload);
        return IsExactCoreLocalSellGoodsPayload(actor, payload)
            && CanCommitSelectedActionSlotP12Mutation(graph, actor);
    }

    public bool TryPublishRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload)
    {
        if (hostPorts != null)
            return TryGetExactActor(actor)
                && hostPorts.TryPublishRequestedActionProjection(actor, payload);
        return IsExactCoreLocalSellGoodsPayload(actor, payload)
            && ReferenceEquals(actor.ActionSlot?.CurrentAction, payload.Action)
            && ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload);
    }

    public void CancelPreparedRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload)
    {
        RequireExactActor(actor);
        if (hostPorts != null)
            hostPorts.CancelPreparedRequestedActionProjection(actor, payload);
    }

    internal static bool IsExactCoreLocalSellGoodsActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action) =>
        action != null
        && ReferenceEquals(actor?.ActionSlot?.CurrentAction, action)
        && IsExactCoreLocalSellGoodsPayload(
            graph, actor, actor?.ActionSlot?.CurrentPayload, requireInstalledSlot: true);

    internal static bool IsExactCoreLocalSellGoodsPayload(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload,
        bool requireInstalledSlot)
    {
        if (graph == null || actor == null || payload?.Action == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || (requireInstalledSlot
                && (!ReferenceEquals(actor.ActionSlot?.CurrentAction, payload.Action)
                    || !ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)))
            || actor.MerchantTradePlan?.IsActive == true
            || payload.TargetItem == null || payload.Amount <= 0
            || !string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
            || payload.TravelReason != NpcTravelReason.None
            || string.IsNullOrWhiteSpace(payload.TargetCityRuntimeId)
            || !string.Equals(payload.TargetCityRuntimeId,
                actor.Travel?.CurrentCityRuntimeId, StringComparison.Ordinal)
            || payload.Action.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            || !graph.Definitions.Actions.TryGetValue(
                payload.Action.DefinitionId,
                out DailyV1ActionDefinition exactAction)
            || !ReferenceEquals(exactAction, payload.Action)
            || !DailyV1SelectedNpcActionDecisionStage.IsEnabledForRuntime(
                graph.Definitions, payload.Action)
            || !graph.Definitions.Items.TryGetValue(
                payload.TargetItem.DefinitionId,
                out DailyV1ItemDefinition exactItem)
            || !ReferenceEquals(exactItem, payload.TargetItem))
            return false;

        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            if (city != null
                && graph.ContainsExactCityOwner(city)
                && string.Equals(city.RuntimeId, payload.TargetCityRuntimeId,
                    StringComparison.Ordinal))
                return graph.IsActorAtCurrentCityLocation(actor);

        return false;
    }

    internal static bool IsExactCoreLocalBuyGoodsActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action) =>
        action != null
        && ReferenceEquals(actor?.ActionSlot?.CurrentAction, action)
        && IsExactCoreLocalBuyGoodsPayload(
            graph, actor, actor?.ActionSlot?.CurrentPayload, requireInstalledSlot: true);

    internal static bool IsExactCoreLocalBuyGoodsPayload(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload,
        bool requireInstalledSlot)
    {
        if (graph == null || actor == null || payload?.Action == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || (requireInstalledSlot
                && (!ReferenceEquals(actor.ActionSlot?.CurrentAction, payload.Action)
                    || !ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)))

            || actor.Lifecycle?.LifeState != NpcLifeState.Alive
            || actor.Travel == null || actor.Travel.IsTraveling
            || actor.MoneyAccount == null || actor.Inventory == null
            || payload.TargetItem == null || payload.Amount <= 0
            || !string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
            || payload.TravelReason != NpcTravelReason.None
            || string.IsNullOrWhiteSpace(payload.TargetCityRuntimeId)
            || !string.Equals(payload.TargetCityRuntimeId,
                actor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)
            || payload.Action.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue
            || !graph.Definitions.Actions.TryGetValue(
                payload.Action.DefinitionId,
                out DailyV1ActionDefinition exactAction)
            || !ReferenceEquals(exactAction, payload.Action)
            || !DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
                graph.Definitions, payload.Action)
            || !graph.Definitions.Items.TryGetValue(
                payload.TargetItem.DefinitionId,
                out DailyV1ItemDefinition exactItem)
            || !ReferenceEquals(exactItem, payload.TargetItem))
            return false;

        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            if (city != null
                && graph.ContainsExactCityOwner(city)
                && ReferenceEquals(city.Definitions, graph.Definitions)
                && ReferenceEquals(city.Market?.Definitions, graph.Definitions)
                && city.Counterparty?.LiquidityMode == MarketLiquidityMode.Open
                && string.Equals(city.RuntimeId, payload.TargetCityRuntimeId,
                    StringComparison.Ordinal))
                return graph.IsActorAtCurrentCityLocation(actor);

        return false;
    }
    internal static bool IsExactCoreOnlySoloTravelActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload,
        bool requireInstalledSlot)
    {
        if (graph == null || actor == null || action == null || payload == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || (requireInstalledSlot
                && (!ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
                    || !ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)))
            || !ReferenceEquals(payload.Action, action)
            || payload.TargetItem != null
            || !string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
            || payload.TravelReason != NpcTravelReason.None
            || string.IsNullOrWhiteSpace(payload.TargetCityRuntimeId)
            || action.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.TravelActionTypeValue
            || !graph.Definitions.Actions.TryGetValue(
                action.DefinitionId, out DailyV1ActionDefinition exactAction)
            || !ReferenceEquals(exactAction, action)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
            || !DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
                graph.Definitions, action)
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, action, actor.Status?.Statuses)
            || actor.Lifecycle?.LifeState != NpcLifeState.Alive
            || actor.Travel == null || actor.Travel.IsTraveling
            || actor.MoneyAccount == null || actor.TravelPlan == null
            || actor.MerchantTradePlan?.IsActive == true
            || graph.TravelParties == null
            || graph.IsManagedByTravelParty(actor, graph.TravelParties)
            || !graph.IsActorAtCurrentCityLocation(actor)
            || string.Equals(payload.TargetCityRuntimeId,
                actor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)
            || !graph.TryGetUniqueCityOwnerForLocation(
                actor.Travel.CurrentLocationRuntimeId,
                out DailyV1CityRuntimeOwners originCity)
            || !ReferenceEquals(originCity, graph.Cities.FirstOrDefault(city =>
                city != null && string.Equals(city.RuntimeId,
                    actor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)))
            || !graph.TryGetCityLocationRuntimeId(
                payload.TargetCityRuntimeId, out string destinationLocationRuntimeId)
            || string.Equals(destinationLocationRuntimeId,
                actor.Travel.CurrentLocationRuntimeId, StringComparison.Ordinal)
            || !graph.TryGetUniqueCityOwnerForLocation(
                destinationLocationRuntimeId,
                out DailyV1CityRuntimeOwners destinationCity)
            || !ReferenceEquals(destinationCity, graph.Cities.FirstOrDefault(city =>
                city != null && string.Equals(city.RuntimeId,
                    payload.TargetCityRuntimeId, StringComparison.Ordinal)))
            || !graph.TryPlanSelectedSoloTravel(
                actor, actor.Travel.CurrentLocationRuntimeId,
                destinationLocationRuntimeId, payload.TargetCityRuntimeId,
                out SpatialRouteRuntime route, out float cost)
            || route == null || !actor.MoneyAccount.CanDebit(cost))
            return false;

        return true;
    }

    internal static bool IsExactCoreOnlySoloTravelDispatchResult(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload,
        DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult result,
        bool requireInstalledSlot)
    {
        if (graph == null || actor == null || action == null || payload == null
            || result == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || (requireInstalledSlot
                && (!ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
                    || !ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)))
            || !ReferenceEquals(payload.Action, action)
            || payload.TargetItem != null
            || !string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
            || payload.TravelReason != NpcTravelReason.None
            || string.IsNullOrWhiteSpace(payload.TargetCityRuntimeId)
            || action.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.TravelActionTypeValue
            || !graph.Definitions.Actions.TryGetValue(
                action.DefinitionId, out DailyV1ActionDefinition exactAction)
            || !ReferenceEquals(exactAction, action)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, action, actor.Status?.Statuses))
            return false;

        if (!result.ActionRollSucceeded)
            return !result.Succeeded && result.Travel == null;
        if (!result.Succeeded)
            return result.Travel == null;

        DailyV1SelectedActionExecutionStage.SoloTravelResult travel = result.Travel;
        DailyV1SelectedTravelTransitionPlan plan = travel?.Transition;
        return travel?.Succeeded == true
            && plan != null && plan.IsCommitted
            && ReferenceEquals(plan.Npc, actor)
            && actor.Travel != null
            && actor.Travel.IsTraveling
            && actor.Travel.CurrentLocationRuntimeId == plan.CurrentLocationRuntimeId
            && actor.Travel.CurrentCityRuntimeId == plan.CurrentCityRuntimeId
            && actor.Travel.DestinationLocationRuntimeId == plan.DestinationLocationRuntimeId
            && actor.Travel.DestinationCityRuntimeId == plan.DestinationCityRuntimeId
            && actor.Travel.TravelDaysRemaining == plan.TravelDaysTotal
            && actor.Travel.TravelDaysTotal == plan.TravelDaysTotal
            && actor.Travel.TravelStartedToday
            && actor.Travel.TravelRouteRuntimeId == plan.RouteRuntimeId
            && actor.Travel.TravelOriginDecisionId == plan.OriginDecisionId
            && string.IsNullOrWhiteSpace(actor.Travel.ActiveTravelPartyId)
            && string.Equals(plan.DestinationCityRuntimeId,
                payload.TargetCityRuntimeId, StringComparison.Ordinal)
            && travel.Route != null
            && string.Equals(plan.RouteRuntimeId, travel.Route.RuntimeId,
                StringComparison.Ordinal)
            && plan.TravelDaysTotal == travel.Route.TravelDays;
    }

    private bool IsExactCoreLocalSellGoodsPayload(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        hostPorts == null && !RequiresActionProjection
        && TryGetExactActor(actor)
        && IsExactCoreLocalSellGoodsPayload(
            graph, actor, payload, requireInstalledSlot: false);

        private bool IsExactCoreLocalBuyGoodsPayload(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        hostPorts == null && !RequiresActionProjection
        && TryGetExactActor(actor)
        && IsExactCoreLocalBuyGoodsPayload(
            graph, actor, payload, requireInstalledSlot: false);

    private bool IsExactCoreCrossCityBuyPayload(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        hostPorts == null && !RequiresActionProjection
        && TryGetExactActor(actor)
        && IsExactCoreCrossCityBuyActionSlot(
            graph, actor, actor.ActionSlot?.CurrentAction);

    internal static bool IsExactCoreCrossCityBuyActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action) =>
        graph != null && actor != null && action != null
        && ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
        && actor.Lifecycle?.LifeState == NpcLifeState.Alive
        && DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
            graph.Definitions, action)
        && DailyV1SelectedCrossCityBuyExecution.IsValidSelectedBuy(
            graph, actor, actor.ActionSlot?.CurrentPayload,
            out _, out _, out _);

    private bool IsExactCoreOnlySoloTravelActionSlot(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload,
        bool requireInstalledSlot) =>
        hostPorts == null && !RequiresActionProjection
        && TryGetExactActor(actor)
        && IsExactCoreOnlySoloTravelActionSlot(
            graph, actor, action, payload, requireInstalledSlot);

    private bool IsExactCoreOnlyActorDispatchPayload(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        IsExactCoreLocalSellGoodsPayload(actor, payload)
        || IsExactCoreLocalBuyGoodsPayload(actor, payload)
        || IsExactCoreCrossCityBuyPayload(actor, payload)
        || IsExactCoreOnlySoloTravelActionSlot(
            actor, actor?.ActionSlot?.CurrentAction, payload, requireInstalledSlot: true);
    /// <summary>
    /// Projection-free dispatch for exact Core market, CrossCityBuy, and ordinary Travel
    /// slots. Each operation constructs its owner-specific boundary and P12 scope
    /// over the shared Core graph.
    /// </summary>
    private sealed class DailyV1CoreOnlyActorActionDispatchBoundary
        : IDailyV1SelectedActionDispatchBoundary,
          IDailyV1SelectedCrossCityBuyP12OperationBoundary
    {
        private readonly DailyV1CoreSelectedActorTurnExecutionBoundary owner;
        private readonly DailyV1NpcRuntimeOwners actor;
        private readonly DailyV1ActionDefinition action;
        private readonly DailyV1NpcActionPayloadOwner payload;

        internal DailyV1CoreOnlyActorActionDispatchBoundary(
            DailyV1CoreSelectedActorTurnExecutionBoundary owner,
            DailyV1NpcRuntimeOwners actor)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
            action = actor.ActionSlot?.CurrentAction;
            payload = actor.ActionSlot?.CurrentPayload;
        }

        private bool IsCurrent(DailyV1NpcRuntimeOwners candidate) =>
            ReferenceEquals(candidate, actor)
            && owner.TryGetExactActor(actor)
            && ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
            && ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
            && owner.IsExactCoreOnlyActorDispatchPayload(actor, payload);

        public bool TryGetMarketBoundaries(
            DailyV1NpcRuntimeOwners candidate,
            out IDailyV1SelectedOpenMarketSaleBoundary saleBoundary,
            out IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary)
        {
            saleBoundary = null;
            purchaseBoundary = null;
            if (!IsCurrent(candidate)
                || !TryFindExactCurrentCity(owner.graph, actor, out DailyV1CityRuntimeOwners city))
                return false;
            if (owner.IsExactCoreLocalSellGoodsPayload(actor, payload))
            {
                if (!DailyV1CoreOnlyLocalSellSaleBoundary.TryCreate(
                        owner.executionState, owner.graph, actor, city,
                        out DailyV1CoreOnlyLocalSellSaleBoundary sale))
                    return false;
                saleBoundary = sale;
                return true;
            }
            if ((owner.IsExactCoreLocalBuyGoodsPayload(actor, payload)
                    || owner.IsExactCoreCrossCityBuyPayload(actor, payload))
                && DailyV1CoreOnlyLocalBuyPurchaseBoundary.TryCreate(
                    owner.executionState, owner.graph, actor, city,
                    out DailyV1CoreOnlyLocalBuyPurchaseBoundary purchase))
            {
                purchaseBoundary = purchase;
                return true;
            }
            return false;
        }
        public bool TryGetSoloTravelBoundary(
            DailyV1NpcRuntimeOwners candidate,
            DailyV1ActionDefinition expectedAction,
            out DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary boundary)
        {
            boundary = null;
            if (!IsCurrent(candidate)
                || !ReferenceEquals(expectedAction, action)
                || !owner.IsExactCoreOnlySoloTravelActionSlot(
                    actor, action, payload, requireInstalledSlot: true))
                return false;
            return DailyV1SelectedActionExecutionStage
                .DailyV1CoreSelectedSoloTravelActionBoundary.TryCreateCoreOnly(
                    owner.executionState, owner.graph, actor, action, out boundary)
                && boundary != null;
        }

        public bool TryGetNpcTradeBoundary(
            DailyV1NpcRuntimeOwners seller,
            DailyV1NpcRuntimeOwners buyer,
            out IDailyV1SelectedNpcTradeBoundary boundary)
        { boundary = null; return false; }

        public bool TryGetFleeCityActionBoundary(
            DailyV1NpcRuntimeOwners candidate,
            DailyV1ActionDefinition expectedAction,
            out DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary boundary)
        { boundary = null; return false; }

        public bool TryGetGuardArrestActionBoundary(
            DailyV1NpcRuntimeOwners candidate,
            DailyV1ActionDefinition expectedAction,
            out DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary boundary)
        { boundary = null; return false; }

        public bool TryGetStealActionBoundary(
            DailyV1NpcRuntimeOwners candidate,
            DailyV1ActionDefinition expectedAction,
            out DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary boundary)
        { boundary = null; return false; }

        public bool TryGetCrimeActionBoundary(
            DailyV1NpcRuntimeOwners candidate,
            DailyV1ActionDefinition expectedAction,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        { boundary = null; return false; }

        public bool TryGetForcedEscapeBoundary(
            DailyV1NpcRuntimeOwners candidate,
            DailyV1ActionDefinition expectedAction,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        { boundary = null; return false; }

        public bool ExecutePlanMutationsWithinCrossCityBuyP12Operation(
            DailyV1NpcRuntimeOwners candidate,
            Func<bool> operation) =>
            IsCurrent(candidate)
            && owner.ExecuteWithinSelectedMerchantDailyOperation(actor, operation);

        public bool CanAdmitMerchantPlanMutation(DailyV1NpcRuntimeOwners candidate) =>
            IsCurrent(candidate)
            && owner.CanAdmitSelectedMerchantPlanMutation(actor);

        public void PublishMerchantPlanMutation(DailyV1NpcRuntimeOwners candidate)
        {
            if (!IsCurrent(candidate)
                || !owner.NotifySelectedMerchantPlanMutationCommitted(
                    actor, actor.MerchantTradePlan?.Revision ?? -1L))
                throw new InvalidOperationException(
                    "The Core-only CrossCityBuy could not publish its exact Merchant-plan census mutation.");
        }

        public bool CanAdmitTradeTravelPlanMutation(DailyV1NpcRuntimeOwners candidate)
        {
            if (!IsCurrent(candidate)
                || !owner.graph.TryGetCityLocationRuntimeId(
                    payload.TargetCityRuntimeId, out string targetLocationRuntimeId))
                return false;
            return owner.CanProjectSelectedMerchantTravelPlanMutation(
                    actor, targetLocationRuntimeId, payload.TargetCityRuntimeId)
                && owner.CanAdmitSelectedMerchantTravelPlanMutation(actor);
        }

        public void PublishTradeTravelPlanMutation(DailyV1NpcRuntimeOwners candidate)
        {
            if (!IsCurrent(candidate)
                || !owner.NotifySelectedMerchantTravelPlanMutationCommitted(
                    actor, actor.TravelPlan?.Revision ?? -1L))
                throw new InvalidOperationException(
                    "The Core-only CrossCityBuy could not publish its exact Travel-plan census mutation.");
        }

        public void CompleteCrossCityBuy(
            DailyV1NpcRuntimeOwners candidate,
            DailyV1SelectedCrossCityBuyResult result)
        {
            if (!IsCurrent(candidate))
                throw new InvalidOperationException(
                    "The Core-only CrossCityBuy lost its exact selected action slot.");
            if (result?.PartialCommit == true)
            {
                owner.executionState.MarkSelectedRuntimeCensusProtocolFaulted();
                owner.executionState.InvalidateCurrentCaptureToken();
                throw new InvalidOperationException(
                    "The Core-only CrossCityBuy partially committed owner state; the admitted Daily-v1 runtime is faulted closed.");
            }
            // Core has no Unity projection to complete. The operation's owner
            // writes were already published through the active Core P12 scope.
        }
    }

    private static bool TryFindExactCurrentCity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        string cityRuntimeId = actor?.Travel?.CurrentCityRuntimeId;
        if (graph == null || actor == null || string.IsNullOrWhiteSpace(cityRuntimeId))
            return false;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            if (candidate != null
                && graph.ContainsExactCityOwner(candidate)
                && ReferenceEquals(candidate.Definitions, graph.Definitions)
                && ReferenceEquals(candidate.Market?.Definitions, graph.Definitions)
                && string.Equals(candidate.RuntimeId, cityRuntimeId, StringComparison.Ordinal))
            {
                city = candidate;
                return true;
            }
        return false;
    }

    /// <summary>
    /// Core-owned equivalent of the former Unity sale adapter. It fences the
    /// exact actor/city owner sections, enters the admitted MarketSale
    /// operation, and publishes every committed owner mutation to the shared
    /// census and epoch before ending that scope.
    /// </summary>
    private sealed class DailyV1CoreOnlyLocalSellSaleBoundary
        : IDailyV1SelectedOpenMarketSaleBoundary
    {
        private readonly DailyV1ExecutionBoundaryState executionState;
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        private readonly DailyV1NpcRuntimeOwners actor;
        private readonly DailyV1CityRuntimeOwners city;
        private readonly DailyV1MarketOwner market;
        private readonly DailyV1CoreSelectedOwnerSectionMutationProtocol protocol;
        private readonly string accountSectionId;
        private readonly string inventorySectionId;
        private readonly string marketSectionId;
        private readonly string[] sectionIds;
        private long accountRevisionBefore;
        private long inventoryRevisionBefore;
        private long marketRevisionBefore;
        private long mutationEpochBefore;
        private bool accountOwnerPublishes;
        private bool inventoryOwnerPublishes;
        private bool marketOwnerPublishes;
        private bool operationBegun;
        private bool disposed;
        private string beginRejection;

        private DailyV1CoreOnlyLocalSellSaleBoundary(
            DailyV1ExecutionBoundaryState executionState,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners city)
        {
            this.executionState = executionState;
            this.graph = graph;
            this.actor = actor;
            this.city = city;
            market = city.Market;
            protocol = new DailyV1CoreSelectedOwnerSectionMutationProtocol(
                executionState, graph);
            accountSectionId = DailyV1SelectedOwnerSectionCensusContract
                .NpcMoneyAccountPrefix + actor.RuntimeId;
            inventorySectionId = DailyV1SelectedOwnerSectionCensusContract
                .NpcInventoryPrefix + actor.RuntimeId;
            string cityKey = P12DCityOwnerImageSectionCodec
                .EncodeCityKeyForSection(city.RuntimeId);
            marketSectionId = DailyV1SelectedOwnerSectionCensusContract
                .CityMarketStockRowsSectionPrefix + cityKey;
            sectionIds = new[] { accountSectionId, inventorySectionId, marketSectionId };
        }

        internal static bool TryCreate(
            DailyV1ExecutionBoundaryState executionState,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners city,
            out DailyV1CoreOnlyLocalSellSaleBoundary boundary)
        {
            boundary = null;
            if (executionState == null || graph == null || actor == null || city == null
                || !executionState.HasAdmittedSelectedDailyExecution
                || !executionState.HasSealedSelectedOwnerSectionCensusRegistry
                || !executionState.HasRuntimeProtocolOperationInventory
                || executionState.AdmissionContext?.Profile
                    != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || !executionState.AdmissionContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
                || !graph.ContainsExactNpcOwner(actor)
                || !graph.ContainsExactCityOwner(city)
                || !ReferenceEquals(actor.Definitions, graph.Definitions)
                || !ReferenceEquals(city.Definitions, graph.Definitions)
                || actor.MoneyAccount == null || actor.Inventory == null
                || city.Market == null
                || !ReferenceEquals(city.Market.Definitions, graph.Definitions)
                || !string.Equals(actor.Travel?.CurrentCityRuntimeId,
                    city.RuntimeId, StringComparison.Ordinal))
                return false;

            DailyV1CoreOnlyLocalSellSaleBoundary candidate =
                new DailyV1CoreOnlyLocalSellSaleBoundary(
                    executionState, graph, actor, city);
            if (!candidate.protocol.TryValidateUnchangedSections(candidate.sectionIds))
                return false;
            boundary = candidate;
            return true;
        }

        public bool TryBegin()
        {
            if (disposed || operationBegun || !IsCurrent())
            {
                beginRejection = "ExactSessionOrOwnerMismatch";
                protocol.FaultClosed();
                return false;
            }
            if (executionState.MutationEpochState.HasActiveReservation
                || !executionState.MutationEpochState.CanAdvanceBy(3))
            {
                beginRejection = "MutationEpochUnavailable";
                protocol.FaultClosed();
                return false;
            }
            if (!protocol.TryValidateUnchangedSections(sectionIds, out string censusFailure))
            {
                beginRejection = "OwnerSectionBaseline:" + (censusFailure ?? "Unknown");
                protocol.FaultClosed();
                return false;
            }
            if (!executionState.TryBeginRuntimeProtocolOperation(
                    DailyV1RuntimeProtocolOperationContracts.MarketSale,
                    requireNoActiveRuntimeProtocolOperations: false))
            {
                beginRejection = "MarketSaleOperationNotAdmitted";
                protocol.FaultClosed();
                return false;
            }

            accountRevisionBefore = actor.MoneyAccount.Revision;
            inventoryRevisionBefore = actor.Inventory.Revision;
            marketRevisionBefore = market.Revision;
            mutationEpochBefore = executionState.MutationEpochState.CurrentEpoch;
            accountOwnerPublishes = MoneyAccountOwnerBridge.HasP12MutationBoundary(
                actor.MoneyAccount);
            inventoryOwnerPublishes = actor.Inventory.HasMutationBoundary;
            marketOwnerPublishes = market.HasMutationBoundary;
            operationBegun = true;
            return true;
        }

        public void Complete(DailyV1SelectedOpenMarketSaleCommitResult result)
        {
            if (!operationBegun || disposed)
            {
                if (!disposed
                    && result?.Failure == DailyV1SelectedOpenMarketSaleFailure.AdmissionRejected
                    && !string.IsNullOrWhiteSpace(beginRejection))
                    throw new InvalidOperationException(
                        "Core P12 MarketSale admission was rejected: " + beginRejection + ".");
                return;
            }
            if (result == null)
            {
                FaultClosedAndThrow("The sale returned no commit result.");
                return;
            }
            if (!IsCurrent())
                FaultClosedAndThrow("The exact selected session or owner graph changed during the sale.");
            if (accountOwnerPublishes != MoneyAccountOwnerBridge.HasP12MutationBoundary(
                    actor.MoneyAccount)
                || inventoryOwnerPublishes != actor.Inventory.HasMutationBoundary
                || marketOwnerPublishes != market.HasMutationBoundary)
                FaultClosedAndThrow("An owner mutation callback changed during the admitted sale.");
            if (!HasExpectedRevision(actor.MoneyAccount.Revision,
                    accountRevisionBefore, result.AccountMutationCount))
                FaultClosedAndThrow("The account revision delta does not match the sale commit result.");
            if (!HasExpectedRevision(actor.Inventory.Revision,
                    inventoryRevisionBefore, result.InventoryMutationCount))
                FaultClosedAndThrow("The Inventory revision delta does not match the sale commit result.");
            if (!HasExpectedRevision(market.Revision, marketRevisionBefore,
                    result.Succeeded ? 1 : 0))
                FaultClosedAndThrow("The Market revision delta does not match the sale commit result.");

            List<string> unnotifiedSections = new List<string>(3);
            int unnotifiedMutationCount = 0;
            if (result.AccountMutationCount > 0 && !accountOwnerPublishes)
            {
                unnotifiedSections.Add(accountSectionId);
                unnotifiedMutationCount += result.AccountMutationCount;
            }
            if (result.InventoryMutationCount > 0 && !inventoryOwnerPublishes)
            {
                unnotifiedSections.Add(inventorySectionId);
                unnotifiedMutationCount += result.InventoryMutationCount;
            }
            if (result.Succeeded && !marketOwnerPublishes)
            {
                unnotifiedSections.Add(marketSectionId);
                unnotifiedMutationCount++;
            }
            object inventoryCardinalityTransition = null;
            if (unnotifiedSections.Contains(inventorySectionId)
                && !executionState.TryBeginCoreSelectedMarketOperationInventoryCardinalityTransition(
                    DailyV1RuntimeProtocolOperationContracts.MarketSale,
                    inventorySectionId, actor.Inventory, out inventoryCardinalityTransition))
                FaultClosedAndThrow(
                    "The exact actor Inventory row could not enter its committed MarketSale transition.");
            object marketCardinalityTransition = null;
            if (unnotifiedSections.Contains(marketSectionId)
                && !executionState.TryBeginCoreSelectedMarketOperationMarketCardinalityTransition(
                    DailyV1RuntimeProtocolOperationContracts.MarketSale,
                    marketSectionId, market, out marketCardinalityTransition))
            {
                executionState.TryEndCoreSelectedMarketOperationInventoryCardinalityTransition(
                    inventoryCardinalityTransition);
                FaultClosedAndThrow(
                    "The exact City Market row could not enter its committed MarketSale transition.");
            }

            bool ownerRowsRefreshed = true;
            bool inventoryTransitionClosed = true;
            bool marketTransitionClosed = true;
            if (unnotifiedSections.Count > 0)
            {
                try
                {
                    ownerRowsRefreshed = executionState.TryRefreshCoreSelectedOwnerSectionCensusRows(
                        graph, unnotifiedSections, reservation: null);
                }
                finally
                {
                    inventoryTransitionClosed = executionState.TryEndCoreSelectedMarketOperationInventoryCardinalityTransition(
                        inventoryCardinalityTransition);
                    marketTransitionClosed = executionState.TryEndCoreSelectedMarketOperationMarketCardinalityTransition(
                        marketCardinalityTransition);
                }
            }
            if (!inventoryTransitionClosed || !marketTransitionClosed)
                FaultClosedAndThrow("The exact MarketSale cardinality transitions did not close.");

            if (unnotifiedSections.Count > 0
                && (!ownerRowsRefreshed
                    || !executionState.MutationEpochState.TryAdvanceCommittedMutations(
                        unnotifiedMutationCount)))
            {
                string changedSections = string.Join(",", unnotifiedSections);
                FaultClosedAndThrow(
                    "Committed P12 owner rows [" + changedSections
                    + "] could not publish their " + unnotifiedMutationCount
                    + " owner mutation epochs (refresh="
                    + (executionState.LastSelectedOwnerSectionCensusRefreshRejection
                        ?? "none") + ").");
            }

            long committedSaleMutationCount = (long)result.AccountMutationCount
                + result.InventoryMutationCount
                + (result.Succeeded ? 1L : 0L);
            if (committedSaleMutationCount < 0L
                || committedSaleMutationCount > 3L
                || mutationEpochBefore > long.MaxValue - committedSaleMutationCount
                || executionState.MutationEpochState.CurrentEpoch
                    != mutationEpochBefore + committedSaleMutationCount)
                FaultClosedAndThrow(
                    "The committed MarketSale owner writes did not publish their exact shared mutation epoch.");

            if (result.PartialCommit)
                FaultClosedAndThrow("The sale reported a partial owner commit.");
            if (!protocol.TryValidateUnchangedSections(sectionIds, out string finalCensusFailure))
                FaultClosedAndThrow("The sale left a P12 owner baseline mismatch: "
                    + (finalCensusFailure ?? "Unknown") + ".");
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (operationBegun && !executionState.TryEndRuntimeProtocolOperation())
                protocol.FaultClosed();
            operationBegun = false;
        }

        private bool IsCurrent() =>
            executionState.HasAdmittedSelectedDailyExecution
            && executionState.AdmissionContext?.IsOwnedByCurrentThread() == true
            && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
            && graph.ContainsExactNpcOwner(actor)
            && graph.ContainsExactCityOwner(city)
            && ReferenceEquals(city.Market, market)
            && ReferenceEquals(actor.Definitions, graph.Definitions)
            && ReferenceEquals(city.Definitions, graph.Definitions)
            && string.Equals(actor.Travel?.CurrentCityRuntimeId,
                city.RuntimeId, StringComparison.Ordinal);

        private static bool HasExpectedRevision(long current, long before, int increments) =>
            increments >= 0 && before >= 0L
            && before <= long.MaxValue - increments
            && current == before + increments;

        private void FaultClosedAndThrow(string message)
        {
            protocol.FaultClosed();
            throw new InvalidOperationException("Core P12 MarketSale faulted closed: " + message);
        }

    }

    /// <summary>
    /// Core-owned P12 boundary for the existing local MarketPurchase operation.
    /// It admits only the exact autonomous local BuyGoods action, fences the
    /// buyer Account, Inventory, and City Market census sections, then publishes
    /// each committed owner mutation into the shared census and epoch.
    /// </summary>
    private sealed class DailyV1CoreOnlyLocalBuyPurchaseBoundary
        : IDailyV1SelectedOpenMarketPurchaseBoundary
    {
        private readonly DailyV1ExecutionBoundaryState executionState;
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        private readonly DailyV1NpcRuntimeOwners actor;
        private readonly DailyV1CityRuntimeOwners city;
        private readonly DailyV1MarketOwner market;
        private readonly DailyV1ActionDefinition action;
        private readonly DailyV1NpcActionPayloadOwner payload;
        private readonly DailyV1CoreSelectedOwnerSectionMutationProtocol protocol;
        private readonly string accountSectionId;
        private readonly string inventorySectionId;
        private readonly string marketSectionId;
        private readonly string[] sectionIds;
        private long accountRevisionBefore;
        private long inventoryRevisionBefore;
        private long marketRevisionBefore;
        private long mutationEpochBefore;
        private bool accountOwnerPublishes;
        private bool inventoryOwnerPublishes;
        private bool marketOwnerPublishes;
        private bool operationBegun;
        private bool disposed;
        private string beginRejection;

        private DailyV1CoreOnlyLocalBuyPurchaseBoundary(
            DailyV1ExecutionBoundaryState executionState,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners city)
        {
            this.executionState = executionState;
            this.graph = graph;
            this.actor = actor;
            this.city = city;
            action = actor.ActionSlot.CurrentAction;
            payload = actor.ActionSlot.CurrentPayload;
            market = city.Market;
            protocol = new DailyV1CoreSelectedOwnerSectionMutationProtocol(
                executionState, graph);
            accountSectionId = DailyV1SelectedOwnerSectionCensusContract
                .NpcMoneyAccountPrefix + actor.RuntimeId;
            inventorySectionId = DailyV1SelectedOwnerSectionCensusContract
                .NpcInventoryPrefix + actor.RuntimeId;
            string cityKey = P12DCityOwnerImageSectionCodec
                .EncodeCityKeyForSection(city.RuntimeId);
            marketSectionId = DailyV1SelectedOwnerSectionCensusContract
                .CityMarketStockRowsSectionPrefix + cityKey;
            sectionIds = new[] { accountSectionId, inventorySectionId, marketSectionId };
        }

        internal static bool TryCreate(
            DailyV1ExecutionBoundaryState executionState,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners city,
            out DailyV1CoreOnlyLocalBuyPurchaseBoundary boundary)
        {
            boundary = null;
            if (executionState == null || graph == null || actor?.ActionSlot == null || city == null
                || (!DailyV1CoreSelectedActorTurnExecutionBoundary
                        .IsExactCoreLocalBuyGoodsActionSlot(
                            graph, actor, actor.ActionSlot.CurrentAction)
                    && !DailyV1CoreSelectedActorTurnExecutionBoundary
                        .IsExactCoreCrossCityBuyActionSlot(
                            graph, actor, actor.ActionSlot.CurrentAction))
                || !executionState.HasAdmittedSelectedDailyExecution
                || !executionState.HasSealedSelectedOwnerSectionCensusRegistry
                || !executionState.HasRuntimeProtocolOperationInventory
                || executionState.AdmissionContext?.Profile
                    != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || !executionState.AdmissionContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
                || !graph.ContainsExactNpcOwner(actor)
                || !graph.ContainsExactCityOwner(city)
                || !ReferenceEquals(actor.Definitions, graph.Definitions)
                || !ReferenceEquals(city.Definitions, graph.Definitions)
                || actor.MoneyAccount == null || actor.Inventory == null
                || city.Market == null || city.Counterparty == null
                || city.Counterparty.LiquidityMode != MarketLiquidityMode.Open
                || !ReferenceEquals(city.Market.Definitions, graph.Definitions)
                || !string.Equals(actor.Travel?.CurrentCityRuntimeId,
                    city.RuntimeId, StringComparison.Ordinal))
                return false;

            DailyV1CoreOnlyLocalBuyPurchaseBoundary candidate =
                new DailyV1CoreOnlyLocalBuyPurchaseBoundary(
                    executionState, graph, actor, city);
            if (!candidate.protocol.TryValidateUnchangedSections(candidate.sectionIds))
                return false;
            boundary = candidate;
            return true;
        }

        public bool CanUseAccountBacked => false;

        public bool TryBegin()
        {
            if (disposed || operationBegun || !IsCurrent())
            {
                beginRejection = "ExactSessionOrOwnerMismatch";
                protocol.FaultClosed();
                return false;
            }
            if (executionState.MutationEpochState.HasActiveReservation
                || !executionState.MutationEpochState.CanAdvanceBy(3))
            {
                beginRejection = "MutationEpochUnavailable";
                protocol.FaultClosed();
                return false;
            }
            if (!protocol.TryValidateUnchangedSections(sectionIds, out string censusFailure))
            {
                beginRejection = "OwnerSectionBaseline:" + (censusFailure ?? "Unknown");
                protocol.FaultClosed();
                return false;
            }
            if (!executionState.TryBeginRuntimeProtocolOperation(
                    DailyV1RuntimeProtocolOperationContracts.MarketPurchase,
                    requireNoActiveRuntimeProtocolOperations: false))
            {
                beginRejection = "MarketPurchaseOperationNotAdmitted";
                protocol.FaultClosed();
                return false;
            }

            accountRevisionBefore = actor.MoneyAccount.Revision;
            inventoryRevisionBefore = actor.Inventory.Revision;
            marketRevisionBefore = market.Revision;
            mutationEpochBefore = executionState.MutationEpochState.CurrentEpoch;
            accountOwnerPublishes = MoneyAccountOwnerBridge.HasP12MutationBoundary(
                actor.MoneyAccount);
            inventoryOwnerPublishes = actor.Inventory.HasMutationBoundary;
            marketOwnerPublishes = market.HasMutationBoundary;
            operationBegun = true;
            return true;
        }

        public void Complete(DailyV1SelectedOpenMarketPurchaseCommitResult result)
        {
            if (!operationBegun || disposed)
            {
                if (!disposed
                    && result?.Failure == DailyV1SelectedOpenMarketPurchaseFailure.AdmissionRejected
                    && !string.IsNullOrWhiteSpace(beginRejection))
                    throw new InvalidOperationException(
                        "Core P12 MarketPurchase admission was rejected: " + beginRejection + ".");
                return;
            }
            if (result == null)
            {
                FaultClosedAndThrow("The purchase returned no commit result.");
                return;
            }
            if (!IsCurrent())
                FaultClosedAndThrow("The exact selected session or owner graph changed during the purchase.");
            if (result.CounterpartyAccountMutationCount != 0
                || accountOwnerPublishes != MoneyAccountOwnerBridge.HasP12MutationBoundary(
                    actor.MoneyAccount)
                || inventoryOwnerPublishes != actor.Inventory.HasMutationBoundary
                || marketOwnerPublishes != market.HasMutationBoundary)
                FaultClosedAndThrow("A purchase used an unsupported counterparty or changed an owner mutation callback.");
            if (!HasExpectedRevision(actor.MoneyAccount.Revision,
                    accountRevisionBefore, result.BuyerAccountMutationCount))
                FaultClosedAndThrow("The buyer Account revision delta does not match the purchase result.");
            if (!HasExpectedRevision(actor.Inventory.Revision,
                    inventoryRevisionBefore, result.InventoryMutationCount))
                FaultClosedAndThrow("The Inventory revision delta does not match the purchase result.");
            if (!HasExpectedRevision(market.Revision,
                    marketRevisionBefore, result.MarketMutationCount))
                FaultClosedAndThrow("The Market revision delta does not match the purchase result.");
            if (result.BuyerAccountMutationCount > 2
                || result.InventoryMutationCount > 1
                || result.MarketMutationCount > 1)
                FaultClosedAndThrow("The purchase reported owner writes outside its bounded mutation shape.");

            List<string> unnotifiedSections = new List<string>(3);
            int unnotifiedMutationCount = 0;
            if (result.BuyerAccountMutationCount > 0 && !accountOwnerPublishes)
            {
                unnotifiedSections.Add(accountSectionId);
                unnotifiedMutationCount += result.BuyerAccountMutationCount;
            }
            if (result.InventoryMutationCount > 0 && !inventoryOwnerPublishes)
            {
                unnotifiedSections.Add(inventorySectionId);
                unnotifiedMutationCount += result.InventoryMutationCount;
            }
            if (result.MarketMutationCount > 0 && !marketOwnerPublishes)
            {
                unnotifiedSections.Add(marketSectionId);
                unnotifiedMutationCount += result.MarketMutationCount;
            }

            object inventoryTransition = null;
            if (unnotifiedSections.Contains(inventorySectionId)
                && !executionState.TryBeginCoreSelectedMarketOperationInventoryCardinalityTransition(
                    DailyV1RuntimeProtocolOperationContracts.MarketPurchase,
                    inventorySectionId, actor.Inventory, out inventoryTransition))
                FaultClosedAndThrow(
                    "The exact actor Inventory row could not enter its committed MarketPurchase transition.");
            object marketTransition = null;
            if (unnotifiedSections.Contains(marketSectionId)
                && !executionState.TryBeginCoreSelectedMarketOperationMarketCardinalityTransition(
                    DailyV1RuntimeProtocolOperationContracts.MarketPurchase,
                    marketSectionId, market, out marketTransition))
            {
                executionState.TryEndCoreSelectedMarketOperationInventoryCardinalityTransition(
                    inventoryTransition);
                FaultClosedAndThrow(
                    "The exact City Market row could not enter its committed MarketPurchase transition.");
            }

            bool ownerRowsRefreshed = true;
            bool inventoryTransitionClosed = true;
            bool marketTransitionClosed = true;
            if (unnotifiedSections.Count > 0)
            {
                try
                {
                    ownerRowsRefreshed = executionState.TryRefreshCoreSelectedOwnerSectionCensusRows(
                        graph, unnotifiedSections, reservation: null);
                }
                finally
                {
                    inventoryTransitionClosed =
                        executionState.TryEndCoreSelectedMarketOperationInventoryCardinalityTransition(
                            inventoryTransition);
                    marketTransitionClosed =
                        executionState.TryEndCoreSelectedMarketOperationMarketCardinalityTransition(
                            marketTransition);
                }
            }
            if (!inventoryTransitionClosed || !marketTransitionClosed)
                FaultClosedAndThrow("The exact MarketPurchase cardinality transitions did not close.");

            if (unnotifiedSections.Count > 0
                && (!ownerRowsRefreshed
                    || !executionState.MutationEpochState.TryAdvanceCommittedMutations(
                        unnotifiedMutationCount)))
            {
                string changedSections = string.Join(",", unnotifiedSections);
                FaultClosedAndThrow(
                    "Committed P12 owner rows [" + changedSections
                    + "] could not publish their " + unnotifiedMutationCount
                    + " owner mutation epochs (refresh="
                    + (executionState.LastSelectedOwnerSectionCensusRefreshRejection
                        ?? "none") + ").");
            }

            long committedMutationCount = (long)result.BuyerAccountMutationCount
                + result.InventoryMutationCount + result.MarketMutationCount;
            if (committedMutationCount < 0L || committedMutationCount > 3L
                || mutationEpochBefore > long.MaxValue - committedMutationCount
                || executionState.MutationEpochState.CurrentEpoch
                    != mutationEpochBefore + committedMutationCount)
                FaultClosedAndThrow(
                    "The committed MarketPurchase writes did not publish their exact shared mutation epoch.");

            if (result.PartialCommit)
                FaultClosedAndThrow("The purchase reported a partial owner commit.");
            if (!protocol.TryValidateUnchangedSections(sectionIds, out string finalCensusFailure))
                FaultClosedAndThrow("The purchase left a P12 owner baseline mismatch: "
                    + (finalCensusFailure ?? "Unknown") + ".");
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (operationBegun && !executionState.TryEndRuntimeProtocolOperation())
                protocol.FaultClosed();
            operationBegun = false;
        }

        private bool IsCurrent() =>
            executionState.HasAdmittedSelectedDailyExecution
            && executionState.AdmissionContext?.IsOwnedByCurrentThread() == true
            && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
            && graph.ContainsExactNpcOwner(actor)
            && graph.ContainsExactCityOwner(city)
            && ReferenceEquals(city.Market, market)
            && ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
            && ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
            && ReferenceEquals(actor.Definitions, graph.Definitions)
            && ReferenceEquals(city.Definitions, graph.Definitions)
            && string.Equals(actor.Travel?.CurrentCityRuntimeId,
                city.RuntimeId, StringComparison.Ordinal);

        private static bool HasExpectedRevision(long current, long before, int increments) =>
            increments >= 0 && before >= 0L
            && before <= long.MaxValue - increments
            && current == before + increments;

        private void FaultClosedAndThrow(string message)
        {
            protocol.FaultClosed();
            throw new InvalidOperationException(
                "Core P12 MarketPurchase faulted closed: " + message);
        }
    }
    public void LogRequestedActionDecisionFailure(string message)
    {
        if (hostPorts != null) hostPorts.LogRequestedActionDecisionFailure(message);
    }

    public bool OwnsSelectedActionSlotP12Mutation =>
        executionState.AdmissionContext != null;

    public bool CanCommitSelectedActionSlotP12Mutation(
        DailyV1SelectedRuntimeOwnerGraph expectedGraph,
        DailyV1NpcRuntimeOwners actor)
    {
        if (!ReferenceEquals(expectedGraph, graph)
            || !TryGetExactActor(actor)
            || (RequiresActionProjection
                && !RequireHostPorts().IsSelectedActionSlotProjectionPrepared(graph, actor)))
            return false;

        return executionState.TryCanCommitSelectedNpcActionSlotMutation(
            graph, actor, actionSlotP12MutationProtocol);
    }

    public bool NotifySelectedActionSlotP12MutationCommitted(
        DailyV1SelectedRuntimeOwnerGraph expectedGraph,
        DailyV1NpcRuntimeOwners actor,
        string sectionId,
        long revision)
    {
        lastActionSlotP12Failure = null;
        if (!ReferenceEquals(expectedGraph, graph))
        {
            lastActionSlotP12Failure = "ActionSlotGraphMismatch";
            return false;
        }
        if (!TryGetExactActor(actor))
        {
            lastActionSlotP12Failure = "ActionSlotActorOutsideBoundRoster";
            return false;
        }
        if (executionState.TryRecordSelectedNpcActionSlotMutation(
                graph, actor, sectionId, revision, actionSlotP12MutationProtocol))
            return true;

        lastActionSlotP12Failure = executionState.LastSelectedOwnerSectionCensusRefreshRejection
            ?? "ActionSlotCoreP12NotificationRejectedWithoutDiagnostic";
        return false;
    }

    public bool CanAdmitSelectedMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor)
        && (hostPorts != null
            ? hostPorts.CanAdmitSelectedMerchantPlanMutation(actor)
            : CanAdmitCoreMerchantSection(actor,
                DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                    DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradePlanKind,
                    actor.RuntimeId)));

    public bool NotifySelectedMerchantPlanMutationCommitted(
        DailyV1NpcRuntimeOwners actor,
        long revision) =>
        TryGetExactActor(actor)
        && actor.MerchantTradePlan?.Revision == revision
        && (hostPorts != null
            ? hostPorts.NotifySelectedMerchantPlanMutationCommitted(actor, revision)
            : RecordCoreMerchantSection(actor,
                DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                    DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradePlanKind,
                    actor.RuntimeId)));

    public bool CanProjectSelectedMerchantTravelPlanMutation(
        DailyV1NpcRuntimeOwners actor,
        string targetLocationRuntimeId,
        string targetCityRuntimeId) =>
        TryGetExactActor(actor)
        && (hostPorts != null
            ? hostPorts.CanProjectSelectedMerchantTravelPlanMutation(
                actor, targetLocationRuntimeId, targetCityRuntimeId)
            : CanProjectCoreMerchantTravelPlan(
                targetLocationRuntimeId, targetCityRuntimeId));

    public bool CanAdmitSelectedMerchantTravelPlanMutation(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor)
        && (hostPorts != null
            ? hostPorts.CanAdmitSelectedMerchantTravelPlanMutation(actor)
            : CanAdmitCoreMerchantSection(actor,
                DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind,
                    actor.RuntimeId)));

    public bool NotifySelectedMerchantTravelPlanMutationCommitted(
        DailyV1NpcRuntimeOwners actor,
        long revision) =>
        TryGetExactActor(actor)
        && actor.TravelPlan?.Revision == revision
        && (hostPorts != null
            ? hostPorts.NotifySelectedMerchantTravelPlanMutationCommitted(actor, revision)
            : RecordCoreMerchantSection(actor,
                DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind,
                    actor.RuntimeId)));

    public NpcDecisionRecord RecordTradeRedirect(
        DailyV1NpcRuntimeOwners actor,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence evidence) =>
        TryGetExactActor(actor)
            ? RequireHostPorts().RecordTradeRedirect(actor, targetLocationRuntimeId, evidence)
            : throw new InvalidOperationException("A Merchant decision referenced an actor outside the Core roster.");

    public void LogTradeDecisionFailure(string message) => hostPorts?.LogTradeDecisionFailure(message);
    public void LogTrade(string message) => hostPorts?.LogTrade(message);

    public bool ExecuteWithinSelectedMerchantDailyOperation(
        DailyV1NpcRuntimeOwners actor,
        Func<bool> operation)
    {
        if (!TryGetExactActor(actor) || operation == null) return false;
        return hostPorts != null
            ? hostPorts.ExecuteWithinSelectedMerchantDailyOperation(actor, operation)
            : executionState.ExecuteSelectedP12MerchantDailyTrade(
                graph, actor, coreMerchantDailyTradeProtocol, operation);
    }

    private bool CanAdmitCoreMerchantSection(
        DailyV1NpcRuntimeOwners actor,
        string sectionId)
    {
        if (!TryGetExactActor(actor) || string.IsNullOrWhiteSpace(sectionId))
            return false;
        if (!executionState.TryCanCommitSelectedP12MerchantDailyTradeMutation(
                new[] { sectionId }, out bool handled))
            return false;
        if (handled) return true;

        return executionState.ActiveRuntimeOperationCount == 1
            && executionState.MutationEpochState.CanAdvanceBy(1)
            && executionState.TryCreateSelectedP12MerchantDailyTradeSectionIds(
                graph, actor, out IReadOnlyList<string> sectionIds)
            && coreMerchantDailyTradeProtocol.TryValidateOwnerSections(
                graph, actor, sectionIds)
            && coreMerchantDailyTradeProtocol.TryValidateUnchangedSections(sectionIds);
    }

    private bool RecordCoreMerchantSection(
        DailyV1NpcRuntimeOwners actor,
        string sectionId)
    {
        if (!TryGetExactActor(actor) || string.IsNullOrWhiteSpace(sectionId)) return false;
        return executionState.TryRecordSelectedP12MerchantDailyTradeMutation(
                new[] { sectionId }, out bool handled)
            && handled;
    }

    private bool CanProjectCoreMerchantTravelPlan(
        string targetLocationRuntimeId,
        string targetCityRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(targetLocationRuntimeId)
            || string.IsNullOrWhiteSpace(targetCityRuntimeId))
            return targetLocationRuntimeId == null && targetCityRuntimeId == null;

        DailyV1CityRuntimeOwners target = null;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
        {
            if (!string.Equals(candidate.RuntimeId, targetCityRuntimeId,
                    StringComparison.Ordinal)
                || !string.Equals(candidate.LocationRuntimeId, targetLocationRuntimeId,
                    StringComparison.Ordinal))
                continue;
            if (target != null || !graph.ContainsExactCityOwner(candidate)) return false;
            target = candidate;
        }
        return target != null;
    }

    public DailyV1JusticeRecordsOwner JusticeRecordsOwner => graph.JusticeRecordsOwner;

    public void ExecuteWithinDailyMutation(
        DailyV1SelectedCrimeJusticeDailyPhase phase,
        Action mutation)
    {
        if (!Enum.IsDefined(typeof(DailyV1SelectedCrimeJusticeDailyPhase), phase)
            || mutation == null)
            throw new ArgumentException("The admitted Core actor turn requires a valid Crime/Justice P12 operation.");
        if (!executionState.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding))
        {
            if (!RequireHostPorts().TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, out binding)
                || binding == null)
            {
                executionState.MarkSelectedRuntimeCensusProtocolFaulted();
                throw new InvalidOperationException(
                    "The selected Crime/Justice operation could not bind its exact host census identities and Core owner relationships to the admitted session.");
            }

            if (!binding.AdmissionEnabled)
            {
                mutation();
                return;
            }

            if (!executionState.TryBindSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, binding)
                || !executionState.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, out binding))
            {
                executionState.MarkSelectedRuntimeCensusProtocolFaulted();
                throw new InvalidOperationException(
                    "The selected Crime/Justice operation could not bind its exact host census identities and Core owner relationships to the admitted session.");
            }
        }

        if (!binding.AdmissionEnabled)
        {
            mutation();
            return;
        }

        if (binding.Protocol == null)
        {
            executionState.MarkSelectedRuntimeCensusProtocolFaulted();
            throw new InvalidOperationException(
                "The selected Crime/Justice operation lost its exact P12 census protocol.");
        }

        executionState.ExecuteSelectedP12CrimeJusticeDailyMutation(
            binding.Protocol,
            graph,
            phase,
            binding.CrimeOwnerIdentity,
            binding.CrimeReceiptCensusOwnerIdentity,
            binding.JusticeOwnerIdentity,
            binding.JusticeReceiptCensusOwnerIdentity,
            includeCrimeReceiptSection: binding.CrimeOwnerIdentity != null,
            includeJusticeReceiptSection: binding.JusticeOwnerIdentity != null,
            mutation);
    }

    public bool CanProjectStatusChanges(
        DailyV1NpcRuntimeOwners owner,
        IReadOnlyList<DailyV1StatusDefinition> statusToAdd)
    {
        if (!TryGetExactActor(owner) || statusToAdd == null) return false;
        if (hostPorts != null)
            return hostPorts.CanProjectStatusChanges(owner, statusToAdd);
        foreach (DailyV1StatusDefinition status in statusToAdd)
            if (!IsExactStatus(status)) return false;
        return true;
    }

    public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners owner)
    {
        RequireExactActor(owner);
        if (hostPorts != null)
        {
            hostPorts.EnsureMutationAllowed(owner);
            return;
        }
        executionState.EnsureSelectedP12CrimeJusticeOwnerMutationAllowed(owner);
    }

    public void NotifyStatusMutationCommitted(
        DailyV1NpcRuntimeOwners owner,
        DailyV1StatusDefinition status,
        bool added)
    {
        RequireExactActor(owner);
        if (!IsExactStatus(status))
            throw new InvalidOperationException(
                "The Core actor-turn boundary received a status outside its admitted definition graph.");
        if (hostPorts != null)
        {
            hostPorts.NotifyStatusMutationCommitted(owner, status, added);
            return;
        }
        executionState.NotifySelectedP12CrimeJusticeOwnerMutationCommitted(owner);
    }

    public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners owner)
    {
        RequireExactActor(owner);
        if (hostPorts != null)
        {
            hostPorts.NotifyHiddenMutationCommitted(owner);
            return;
        }
        executionState.NotifySelectedP12CrimeJusticeOwnerMutationCommitted(owner);
    }

    private bool IsExactStatus(DailyV1StatusDefinition status) =>
        status != null
        && graph.Definitions.Statuses.TryGetValue(
            status.DefinitionId, out DailyV1StatusDefinition admitted)
        && ReferenceEquals(admitted, status);

    public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
        RequireHostPorts().CanRemoveSentenceProjection(sentence);

    public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence) =>
        RequireHostPorts().CommitSentenceProjectionRemoval(sentence);

    public void Log(SimulationLogCategory category, string message) {
        // Core owns the selected Crime/Justice mutation. Logging is a host presentation effect.
        if (hostPorts != null) hostPorts.Log(category, message);
    }
    public void LogWarning(string message) => RequireHostPorts().LogWarning(message);
}

/// <summary>
/// Hostless Core boundary for the selected Hide, FleeCity and Guard Arrest
/// actions and the existing scheduled ForceOutcome/EscapePrison operation. Domain execution, owner
/// writes and P12 reservation remain in Core; Unity callbacks are omitted
/// presentation effects, while every unsupported action path fails closed.
/// </summary>
internal sealed class DailyV1CoreOnlyCrimeActionBoundary :
    IDailyV1SelectedActionDispatchBoundary,
    DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionProjectionBoundary,
    DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionProjectionBoundary,
    DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionProjectionBoundary,
    IDailyV1SelectedCrimeJusticeDailyMutationHostPort
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;
    private readonly DailyV1NpcRuntimeOwners actor;
    private readonly DailyV1ActionDefinition action;
    private readonly DailyV1NpcActionPayloadOwner payload;
    private readonly bool forcedEscape;
    private DailyV1JusticeSentenceOwner preparedGuardArrestSentence;
    private DailyV1CityRuntimeOwners preparedFleeDestination;
    private float preparedFleeTravelCost;
    private string preparedFleeOriginDecisionId;
    private long preparedFleeTravelPlanRevision;
    private bool preparedFleeTravelPlanChanges;
    private DailyV1P12CrimeJusticeMutationCoordinator preparedFleeCoordinator;

    private DailyV1CoreOnlyCrimeActionBoundary(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload,
        bool forcedEscape)
    {
        this.executionState = executionState;
        this.graph = graph;
        this.actor = actor;
        this.action = action;
        this.payload = payload;
        this.forcedEscape = forcedEscape;
    }

    internal static bool TryCreate(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1SelectedActionDispatchBoundary boundary)
    {
        boundary = null;
        DailyV1ActionDefinition action = actor?.ActionSlot?.CurrentAction;
        DailyV1NpcActionPayloadOwner payload = actor?.ActionSlot?.CurrentPayload;
        if (executionState == null || graph == null || actor == null
            || (!IsExactTargetlessHideSlot(graph, actor, action, payload)
                && !IsExactFleeCityActionSlot(graph, actor, action, payload)
                && !IsExactGuardArrestActionSlot(graph, actor, action, payload)
                && !IsExactStealActionSlot(graph, actor, action, payload)
                && !IsExactEscapeActionSlot(graph, actor, action, payload))
            || !IsCurrentAdmittedCoreSession(executionState, graph, actor))
            return false;

        boundary = new DailyV1CoreOnlyCrimeActionBoundary(
            executionState, graph, actor, action, payload, forcedEscape: false);
        return true;
    }

    internal static bool TryCreateForcedEscapeBoundary(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
    {
        boundary = null;
        DailyV1NpcActionPayloadOwner payload = actor?.ActionSlot?.CurrentPayload;
        if (executionState == null || graph == null || actor == null || action == null
            || !IsExactTargetlessForcedEscapeSlot(graph, actor, action, payload)
            || !IsCurrentAdmittedCoreSession(executionState, graph, actor))
            return false;

        DailyV1CoreOnlyCrimeActionBoundary projection =
            new DailyV1CoreOnlyCrimeActionBoundary(
                executionState, graph, actor, action, payload, forcedEscape: true);
        if (!string.IsNullOrWhiteSpace(graph.Definitions.Configuration?.arrestedStatus)
            && graph.Definitions.Statuses.TryGetValue(
                graph.Definitions.Configuration.arrestedStatus,
                out DailyV1StatusDefinition arrestedStatus)
            && actor.Status?.Contains(arrestedStatus) == true)
            return projection.TryGetForcedEscapeBoundary(actor, action, out boundary)
                && boundary != null;
        return false;
    }

    internal static bool IsExactTargetlessHideSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload) =>
        graph != null && actor != null && action != null && payload != null
        && graph.ContainsExactNpcOwner(actor)
        && ReferenceEquals(actor.Definitions, graph.Definitions)
        && graph.Definitions.Actions.TryGetValue(action.DefinitionId,
            out DailyV1ActionDefinition admittedAction)
        && ReferenceEquals(action, admittedAction)
        && action.Values.actionType?.Value == DailyV1NpcDefinitionRules.HideActionTypeValue
        && DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
            graph.Definitions, action)
        && ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
        && ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
        && ReferenceEquals(payload.Action, action)
        && payload.TargetNpcRuntimeId == null
        && payload.TargetCityRuntimeId == null
        && payload.TargetItem == null
        && payload.Amount == 0
        && payload.ExpectedUnitPrice == 0f
        && payload.TravelReason == NpcTravelReason.None
        && payload.ExpectedNetValue == 0f
        && payload.CommercialDecisionEvidence == null
        && payload.CommercialScoutingEvidence == null;

    internal static bool IsExactEscapeActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload) =>
        graph != null && actor != null && action != null && payload != null
        && graph.ContainsExactNpcOwner(actor)
        && ReferenceEquals(actor.Definitions, graph.Definitions)
        && graph.Definitions.Actions.TryGetValue(action.DefinitionId,
            out DailyV1ActionDefinition admittedAction)
        && ReferenceEquals(action, admittedAction)
        && DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
        && DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
            graph.Definitions, action)
        && action.Values.actionType?.Value == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue
        && ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
        && ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
        && ReferenceEquals(payload.Action, action)
        && payload.TargetNpcRuntimeId == null
        && payload.TargetCityRuntimeId == null
        && payload.TargetItem == null
        && payload.Amount == 0
        && payload.ExpectedUnitPrice == 0f
        && payload.TravelReason == NpcTravelReason.None
        && payload.ExpectedNetValue == 0f
        && payload.CommercialDecisionEvidence == null
        && payload.CommercialScoutingEvidence == null;
    internal static bool IsExactGuardArrestActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload) =>
        graph != null && actor != null && action != null && payload != null
        && graph.ContainsExactNpcOwner(actor)
        && ReferenceEquals(actor.Definitions, graph.Definitions)
        && graph.Definitions.Actions.TryGetValue(action.DefinitionId,
            out DailyV1ActionDefinition admittedAction)
        && ReferenceEquals(action, admittedAction)
        && DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
        && DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
            graph.Definitions, action)
        && action.Values.actionType?.Value == DailyV1NpcDefinitionRules.ArrestActionTypeValue
        && ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
        && ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
        && ReferenceEquals(payload.Action, action)
        && !string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
        && payload.TargetCityRuntimeId == null
        && payload.TargetItem == null
        && payload.Amount == 0
        && payload.ExpectedUnitPrice == 0f
        && payload.TravelReason == NpcTravelReason.None
        && payload.ExpectedNetValue == 0f
        && payload.CommercialDecisionEvidence == null
        && payload.CommercialScoutingEvidence == null;

    internal static bool IsExactFleeCityActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload) =>
        graph != null && actor != null && action != null && payload != null
        && graph.ContainsExactNpcOwner(actor)
        && ReferenceEquals(actor.Definitions, graph.Definitions)
        && graph.Definitions.Actions.TryGetValue(action.DefinitionId,
            out DailyV1ActionDefinition admittedAction)
        && ReferenceEquals(action, admittedAction)
        && DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
        && DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
            graph.Definitions, action)
        && action.Values.actionType?.Value == DailyV1NpcDefinitionRules.FleeCityActionTypeValue
        && ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
        && ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
        && ReferenceEquals(payload.Action, action)
        && !string.IsNullOrWhiteSpace(payload.TargetCityRuntimeId)
        && payload.TargetNpcRuntimeId == null
        && payload.TargetItem == null
        && payload.Amount == 0;

    internal static bool IsExactStealActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload) =>
        graph != null && actor != null && action != null && payload != null
        && graph.ContainsExactNpcOwner(actor)
        && ReferenceEquals(actor.Definitions, graph.Definitions)
        && graph.Definitions.Actions.TryGetValue(action.DefinitionId,
            out DailyV1ActionDefinition admittedAction)
        && ReferenceEquals(action, admittedAction)
        && DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
        && DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
            graph.Definitions, action)
        && action.Values.actionType?.Value == DailyV1NpcDefinitionRules.StealActionTypeValue
        && ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
        && ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
        && ReferenceEquals(payload.Action, action)
        && !string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
        && payload.TargetCityRuntimeId == null
        && payload.TargetItem == null
        && payload.Amount > 0
        && payload.ExpectedUnitPrice == 0f
        && payload.TravelReason == NpcTravelReason.None
        && payload.ExpectedNetValue == 0f
        && payload.CommercialDecisionEvidence == null
        && payload.CommercialScoutingEvidence == null
        && !string.IsNullOrWhiteSpace(payload.StableOccurrenceKey);

    private static bool IsExactTargetlessForcedEscapeSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload) =>
        graph != null && actor != null && action != null && payload != null
        && graph.ContainsExactNpcOwner(actor)
        && ReferenceEquals(actor.Definitions, graph.Definitions)
        && graph.Definitions.Actions.TryGetValue(action.DefinitionId,
            out DailyV1ActionDefinition admittedAction)
        && ReferenceEquals(action, admittedAction)
        && DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
        && action.Values.actionType?.Value == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue
        && ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
        && ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
        && ReferenceEquals(payload.Action, action)
        && HasNeutralActionPayload(payload);

    private static bool HasNeutralActionPayload(DailyV1NpcActionPayloadOwner payload) =>
        payload != null
        && payload.TargetNpcRuntimeId == null
        && payload.TargetCityRuntimeId == null
        && payload.TargetItem == null
        && payload.Amount == 0
        && payload.ExpectedUnitPrice == 0f
        && payload.TravelReason == NpcTravelReason.None
        && payload.ExpectedNetValue == 0f
        && payload.CommercialDecisionEvidence == null
        && payload.CommercialScoutingEvidence == null
        && payload.OriginDecisionId == null;

    private static bool IsCurrentAdmittedCoreSession(
        DailyV1ExecutionBoundaryState state,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor) =>
        state != null && graph != null && actor != null
        && state.HasAdmittedSelectedDailyExecution
        && state.AdmissionContext?.IsOwnedByCurrentThread() == true
        && ReferenceEquals(state.SelectedRuntimeOwnerGraph, graph)
        && graph.ContainsExactNpcOwner(actor)
        && ReferenceEquals(actor.Definitions, graph.Definitions);

    private bool IsCurrent() =>
        IsCurrentAdmittedCoreSession(executionState, graph, actor)
        && (forcedEscape
            ? IsExactTargetlessForcedEscapeSlot(graph, actor, action, payload)
            : action?.Values.actionType?.Value == DailyV1NpcDefinitionRules.ArrestActionTypeValue
                ? IsExactGuardArrestActionSlot(graph, actor, action, payload)
                : action?.Values.actionType?.Value == DailyV1NpcDefinitionRules.FleeCityActionTypeValue
                    ? IsExactFleeCityActionSlot(graph, actor, action, payload)
                    : action?.Values.actionType?.Value == DailyV1NpcDefinitionRules.StealActionTypeValue
                        ? IsExactStealActionSlot(graph, actor, action, payload)
                        : action?.Values.actionType?.Value == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue
                            ? IsExactEscapeActionSlot(graph, actor, action, payload)
                            : IsExactTargetlessHideSlot(graph, actor, action, payload));
    private bool IsExactActor(DailyV1NpcRuntimeOwners candidate) =>
        ReferenceEquals(candidate, actor) && IsCurrent();

    private void EnsureExactActor(DailyV1NpcRuntimeOwners candidate)
    {
        if (!IsExactActor(candidate))
            throw new InvalidOperationException(
                "The Core-only Crime action boundary lost its exact admitted actor and action slot.");
    }

    public bool TryGetSoloTravelBoundary(
        DailyV1NpcRuntimeOwners candidate,
        DailyV1ActionDefinition expectedAction,
        out DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary boundary)
    { boundary = null; return false; }

    public bool TryGetMarketBoundaries(
        DailyV1NpcRuntimeOwners candidate,
        out IDailyV1SelectedOpenMarketSaleBoundary saleBoundary,
        out IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary)
    { saleBoundary = null; purchaseBoundary = null; return false; }

    public bool TryGetNpcTradeBoundary(
        DailyV1NpcRuntimeOwners seller,
        DailyV1NpcRuntimeOwners buyer,
        out IDailyV1SelectedNpcTradeBoundary boundary)
    { boundary = null; return false; }

    public bool TryGetFleeCityActionBoundary(
        DailyV1NpcRuntimeOwners candidate,
        DailyV1ActionDefinition expectedAction,
        out DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary boundary)
    {
        boundary = null;
        if (forcedEscape || !IsExactActor(candidate)
            || !ReferenceEquals(expectedAction, action)
            || !IsExactFleeCityActionSlot(graph, actor, action, payload))
            return false;

        return DailyV1SelectedCrimeActionExecutionStage
            .DailyV1CoreSelectedFleeCityActionBoundary.TryCreate(
                executionState, graph, actor, action, this, this, out boundary)
            && boundary != null;
    }

    public bool TryGetGuardArrestActionBoundary(
        DailyV1NpcRuntimeOwners candidate,
        DailyV1ActionDefinition expectedAction,
        out DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary boundary)
    {
        boundary = null;
        if (forcedEscape || !IsExactActor(candidate)
            || !ReferenceEquals(expectedAction, action)
            || !IsExactGuardArrestActionSlot(graph, actor, action, payload)
            || !graph.TryGetNpcOwner(payload.TargetNpcRuntimeId,
                out DailyV1NpcRuntimeOwners target)
            || target == null || ReferenceEquals(target, actor))
            return false;

        return DailyV1SelectedCrimeActionExecutionStage.DailyV1CoreSelectedGuardArrestActionBoundary.TryCreate(
                executionState, graph, actor, action, target, this, this, out boundary)
            && boundary != null;
    }

    public bool TryGetStealActionBoundary(
        DailyV1NpcRuntimeOwners candidate,
        DailyV1ActionDefinition expectedAction,
        out DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary boundary)
    {
        boundary = null;
        if (forcedEscape || !IsExactActor(candidate)
            || !ReferenceEquals(expectedAction, action)
            || !DailyV1CoreOnlyCrimeActionBoundary.IsExactStealActionSlot(
                graph, actor, action, payload)
            || string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
            || !graph.TryGetNpcOwner(payload.TargetNpcRuntimeId,
                out DailyV1NpcRuntimeOwners victim)
            || victim == null || ReferenceEquals(victim, actor))
            return false;

        DailyV1CityRuntimeOwners city = null;
        foreach (DailyV1CityRuntimeOwners candidateCity in graph.Cities)
            if (candidateCity != null
                && string.Equals(candidateCity.RuntimeId,
                    actor.Travel?.CurrentCityRuntimeId, StringComparison.Ordinal))
            {
                city = candidateCity;
                break;
            }
        if (city == null) return false;

        return DailyV1SelectedCrimeActionExecutionStage
            .DailyV1CoreSelectedStealActionBoundary.TryCreate(
                executionState, graph, actor, victim, city, action,
                null, this, out boundary)
            && boundary != null;
    }

    public bool TryGetCrimeActionBoundary(
        DailyV1NpcRuntimeOwners candidate,
        DailyV1ActionDefinition expectedAction,
        out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
    {
        boundary = null;
        if (forcedEscape || !IsExactActor(candidate)
            || !ReferenceEquals(expectedAction, action)) return false;
        return DailyV1SelectedCrimeActionExecutionStage.DailyV1CoreSelectedCrimeActionBoundary.TryCreate(
            executionState, graph, actor, action, this, this, out boundary)
            && boundary != null;
    }

    public bool TryGetForcedEscapeBoundary(
        DailyV1NpcRuntimeOwners candidate,
        DailyV1ActionDefinition expectedAction,
        out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
    {
        boundary = null;
        return forcedEscape && IsExactActor(candidate)
            && ReferenceEquals(expectedAction, action)
            && DailyV1SelectedCrimeActionExecutionStage.DailyV1CoreSelectedCrimeActionBoundary.TryCreate(
                executionState, graph, actor, action, this, this, out boundary)
            && boundary != null;
    }

    public bool CanPrepareTravelPlanProjection(
        DailyV1NpcRuntimeOwners candidateActor,
        DailyV1CityRuntimeOwners destination,
        float travelCost,
        string originDecisionId)
    {
        preparedFleeDestination = null;
        preparedFleeCoordinator = null;
        preparedFleeTravelPlanChanges = false;
        if (!IsExactActor(candidateActor)
            || !IsExactFleeCityActionSlot(graph, actor, action, payload)
            || destination == null || !graph.ContainsExactCityOwner(destination)
            || !ReferenceEquals(destination.Definitions, graph.Definitions)
            || !string.Equals(destination.RuntimeId, payload.TargetCityRuntimeId,
                StringComparison.Ordinal)
            || actor.TravelPlan == null)
            return false;

        string normalizedDecisionId = string.IsNullOrWhiteSpace(originDecisionId)
            ? null
            : originDecisionId;
        if (!actor.TravelPlan.CanCommit(
                destination.LocationRuntimeId, destination.RuntimeId,
                NpcTravelReason.Flee, 80f, travelCost, normalizedDecisionId)
            || !executionState.TryGetCoreSelectedP12CrimeJusticeMutationBoundary(
                graph,
                out IDailyV1P12CrimeJusticeMutationProtocol protocol,
                out DailyV1P12CrimeJusticeMutationCoordinator coordinator)
            || protocol == null || coordinator == null)
            return false;

        preparedFleeDestination = destination;
        preparedFleeTravelCost = travelCost;
        preparedFleeOriginDecisionId = normalizedDecisionId;
        preparedFleeTravelPlanRevision = actor.TravelPlan.Revision;
        preparedFleeTravelPlanChanges = !actor.TravelPlan.Matches(
            destination.LocationRuntimeId, destination.RuntimeId,
            NpcTravelReason.Flee, 80f, travelCost, normalizedDecisionId);
        preparedFleeCoordinator = coordinator;
        return true;
    }

    public bool CanAdmitTravelPlanMutation()
    {
        if (preparedFleeDestination == null || !preparedFleeTravelPlanChanges
            || !IsCurrent() || actor.TravelPlan == null
            || actor.TravelPlan.Revision != preparedFleeTravelPlanRevision
            || !executionState.TryGetCoreSelectedP12CrimeJusticeMutationBoundary(
                graph,
                out IDailyV1P12CrimeJusticeMutationProtocol protocol,
                out DailyV1P12CrimeJusticeMutationCoordinator coordinator)
            || protocol == null || !ReferenceEquals(coordinator, preparedFleeCoordinator))
            return false;

        string sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, actor.RuntimeId);
        return coordinator.CanCommit(actor.TravelPlan, sectionId);
    }

    public void PublishCommittedTravelPlan(
        DailyV1NpcRuntimeOwners candidateActor,
        DailyV1CityRuntimeOwners destination,
        float travelCost,
        string originDecisionId)
    {
        string normalizedDecisionId = string.IsNullOrWhiteSpace(originDecisionId)
            ? null
            : originDecisionId;
        if (!IsExactActor(candidateActor) || actor.TravelPlan == null
            || !ReferenceEquals(destination, preparedFleeDestination)
            || !preparedFleeTravelPlanChanges
            || !travelCost.Equals(preparedFleeTravelCost)
            || !string.Equals(normalizedDecisionId, preparedFleeOriginDecisionId,
                StringComparison.Ordinal)
            || actor.TravelPlan.Revision != preparedFleeTravelPlanRevision + 1L
            || !actor.TravelPlan.Matches(destination.LocationRuntimeId,
                destination.RuntimeId, NpcTravelReason.Flee, 80f,
                travelCost, normalizedDecisionId)
            || !executionState.TryGetCoreSelectedP12CrimeJusticeMutationBoundary(
                graph,
                out IDailyV1P12CrimeJusticeMutationProtocol protocol,
                out DailyV1P12CrimeJusticeMutationCoordinator coordinator)
            || protocol == null || !ReferenceEquals(coordinator, preparedFleeCoordinator))
            throw new InvalidOperationException(
                "The Core-only FleeCity TravelPlan commit left its exact admitted owner or mutation scope.");

        string sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, actor.RuntimeId);
        coordinator.NotifyCommitted(actor.TravelPlan, sectionId);
        preparedFleeDestination = null;
        preparedFleeOriginDecisionId = null;
        preparedFleeTravelPlanChanges = false;
        preparedFleeCoordinator = null;
    }

    public bool CanAdmitMerchantPlanMutation(DailyV1NpcRuntimeOwners candidate) => false;
    public void PublishMerchantPlanMutation(DailyV1NpcRuntimeOwners candidate) =>
        throw new InvalidOperationException("Merchant mutation is outside the Core-only Hide adapter.");
    public bool CanAdmitTradeTravelPlanMutation(DailyV1NpcRuntimeOwners candidate) => false;
    public void PublishTradeTravelPlanMutation(DailyV1NpcRuntimeOwners candidate) =>
        throw new InvalidOperationException("Trade travel mutation is outside the Core-only Hide adapter.");
    public void CompleteCrossCityBuy(
        DailyV1NpcRuntimeOwners candidate,
        DailyV1SelectedCrossCityBuyResult result) =>
        throw new InvalidOperationException("Market completion is outside the Core-only Hide adapter.");

    public bool TryGetSelectedCrimeJusticeDailyMutationHostBinding(
        DailyV1SelectedRuntimeOwnerGraph candidateGraph,
        out DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding)
    {
        binding = null;
        return IsCurrent() && ReferenceEquals(candidateGraph, graph)
            && executionState.TryBindSelectedCrimeJusticeDailyMutationBindingFromCoreCensus(graph)
            && executionState.TryGetSelectedCrimeJusticeDailyMutationHostBinding(graph, out binding)
            && binding != null && binding.AdmissionEnabled
            && binding.HasExactCoreOwnerRelationships(graph);
    }

    public bool CanProjectStatusChanges(
        DailyV1NpcRuntimeOwners candidate,
        IReadOnlyList<DailyV1StatusDefinition> statusToAdd)
    {
        bool exactWriter = action?.Values.actionType?.Value
                == DailyV1NpcDefinitionRules.ArrestActionTypeValue
            ? IsGuardArrestTarget(candidate)
            : IsExactActor(candidate);
        if (!exactWriter || !IsCurrent() || statusToAdd == null || candidate.Status == null)
            return false;
        foreach (DailyV1StatusDefinition status in statusToAdd)
            if (status == null
                || !graph.Definitions.Statuses.TryGetValue(status.DefinitionId,
                    out DailyV1StatusDefinition admittedStatus)
                || !ReferenceEquals(status, admittedStatus))
                return false;
        return true;
    }

    public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners candidate)
    {
        if (action?.Values.actionType?.Value != DailyV1NpcDefinitionRules.ArrestActionTypeValue
            || !IsGuardArrestTarget(candidate) || !IsCurrent())
            throw new InvalidOperationException(
                "The Core-only Guard Arrest status write left its exact admitted target owner.");
        executionState.EnsureSelectedP12CrimeJusticeOwnerMutationAllowed(candidate);
    }

    public void NotifyStatusMutationCommitted(
        DailyV1NpcRuntimeOwners candidate,
        DailyV1StatusDefinition status,
        bool added)
    {
        if (action?.Values.actionType?.Value != DailyV1NpcDefinitionRules.ArrestActionTypeValue
            || !IsGuardArrestTarget(candidate) || !IsCurrent())
            throw new InvalidOperationException(
                "The Core-only Guard Arrest status commit left its exact admitted target owner.");
        executionState.NotifySelectedP12CrimeJusticeOwnerMutationCommitted(candidate);
    }

    public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners candidate)
    {
        if (action?.Values.actionType?.Value != DailyV1NpcDefinitionRules.ArrestActionTypeValue
            || !IsGuardArrestTarget(candidate) || !IsCurrent())
            throw new InvalidOperationException(
                "The Core-only Guard Arrest hidden-state commit left its exact admitted target owner.");
        executionState.NotifySelectedP12CrimeJusticeOwnerMutationCommitted(candidate);
    }

    public bool TryPrepareSentenceProjection(
        DailyV1NpcRuntimeOwners target,
        DailyV1CityRuntimeOwners city,
        DailyV1JusticeWantedRecordOwner warrant,
        DailyV1JusticeSentenceOwner sentence)
    {
        if (action?.Values.actionType?.Value != DailyV1NpcDefinitionRules.ArrestActionTypeValue
            || !IsGuardArrestTarget(target)
            || !graph.ContainsExactCityOwner(city)
            || !ReferenceEquals(city.Definitions, graph.Definitions)
            || warrant == null || !graph.JusticeRecordsOwner.ContainsWantedRecord(warrant)
            || !string.Equals(warrant.TargetRuntimeId, target.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(warrant.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || sentence == null || !ReferenceEquals(sentence.Warrant, warrant)
            || !string.Equals(sentence.TargetRuntimeId, target.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(sentence.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || preparedGuardArrestSentence != null || !IsCurrent())
            return false;

        preparedGuardArrestSentence = sentence;
        return true;
    }

    public void DiscardPreparedSentenceProjection(DailyV1JusticeSentenceOwner sentence)
    {
        if (!ReferenceEquals(preparedGuardArrestSentence, sentence) || !IsCurrent())
            throw new InvalidOperationException(
                "The Core-only Guard Arrest discarded a sentence outside its exact prepared owner relation.");
        preparedGuardArrestSentence = null;
    }

    public bool CommitPreparedSentenceProjection(DailyV1JusticeSentenceOwner sentence)
    {
        if (!ReferenceEquals(preparedGuardArrestSentence, sentence) || !IsCurrent()
            || !graph.JusticeRecordsOwner.ContainsSentence(sentence))
            return false;
        preparedGuardArrestSentence = null;
        return true;
    }

    public void PublishArrest(
        DailyV1NpcRuntimeOwners guard,
        DailyV1NpcRuntimeOwners target,
        DailyV1CityRuntimeOwners city,
        string originDecisionId)
    {
        if (action?.Values.actionType?.Value != DailyV1NpcDefinitionRules.ArrestActionTypeValue
            || !ReferenceEquals(guard, actor) || !IsGuardArrestTarget(target)
            || !graph.ContainsExactCityOwner(city)
            || !ReferenceEquals(city.Definitions, graph.Definitions)
            || !string.Equals(originDecisionId, payload.OriginDecisionId, StringComparison.Ordinal)
            || !IsCurrent())
            throw new InvalidOperationException(
                "The Core-only Guard Arrest publication left its exact admitted owners.");

        // DomainEventRecorder.Record historically reports failure without
        // rolling back the committed arrest. Keep that post-commit behavior;
        // the session recorder itself retains event identity and sequence.
        if (!executionState.TryRecordSelectedNpcArrestEvent(
                guard, target, city, originDecisionId, out _))
            throw new InvalidOperationException(
                "The Core-only Guard Arrest could not record through its exact selected event authority.");
    }

    public void RefreshStatusProjection(DailyV1NpcRuntimeOwners candidate) => EnsureExactActor(candidate);
    public void SyncHiddenStateProjection(DailyV1NpcRuntimeOwners candidate) => EnsureExactActor(candidate);
    public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
        forcedEscape && IsExactActor(actor) && IsExactActiveSentence(sentence);
    public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence)
    {
        if (!forcedEscape || !IsExactActor(actor) || !IsExactRemovedSentence(sentence))
            throw new InvalidOperationException(
                "The Core-only Escape sentence removal left its exact Justice owner relation.");
    }
    public void PublishHidden(DailyV1NpcRuntimeOwners candidate)
    {
        if (forcedEscape)
            throw new InvalidOperationException("Hide publication is outside the Core-only Escape boundary.");
        EnsureExactActor(candidate);
    }
    public void PublishFailedEscape(DailyV1NpcRuntimeOwners candidate, int sentencePenalty)
    {
        if (!forcedEscape || !IsExactActor(candidate) || sentencePenalty < 0)
            throw new InvalidOperationException(
                "Failed Escape publication left the exact Core-only Crime action boundary.");
    }
    public void PublishEscaped(
        DailyV1NpcRuntimeOwners candidate,
        DailyV1CityRuntimeOwners eventCity,
        bool hadActiveSentence,
        float bountyAfterPenalty,
        string originDecisionId)
    {
        if (!forcedEscape || !IsExactActor(candidate)
            || (eventCity != null && !graph.ContainsExactCityOwner(eventCity))
            || (hadActiveSentence && eventCity == null))
            throw new InvalidOperationException(
                "Escape publication left the exact Core-only Crime action boundary.");
        if (!executionState.TryRecordSelectedNpcEscapeEvent(
                candidate, eventCity, originDecisionId, out _))
            throw new InvalidOperationException(
                "The Core-only Escape could not record through its exact selected event authority.");
    }

    private bool IsGuardArrestTarget(DailyV1NpcRuntimeOwners candidate) =>
        action?.Values.actionType?.Value == DailyV1NpcDefinitionRules.ArrestActionTypeValue
        && candidate != null
        && graph.TryGetNpcOwner(payload?.TargetNpcRuntimeId,
            out DailyV1NpcRuntimeOwners exactTarget)
        && ReferenceEquals(candidate, exactTarget)
        && graph.ContainsExactNpcOwner(candidate)
        && ReferenceEquals(candidate.Definitions, graph.Definitions);

    private bool IsExactActiveSentence(DailyV1JusticeSentenceOwner sentence) =>
        sentence != null && sentence.IsActive
        && graph.JusticeRecordsOwner != null
        && graph.JusticeRecordsOwner.ContainsSentence(sentence)
        && sentence.Warrant != null
        && graph.JusticeRecordsOwner.ContainsWantedRecord(sentence.Warrant)
        && !sentence.Warrant.IsResolved
        && string.Equals(sentence.TargetRuntimeId, actor.RuntimeId, StringComparison.Ordinal)
        && string.Equals(sentence.Warrant.TargetRuntimeId, actor.RuntimeId, StringComparison.Ordinal)
        && string.Equals(sentence.CityRuntimeId, sentence.Warrant.CityRuntimeId, StringComparison.Ordinal)
        && graph.Cities.Any(city => city != null
            && graph.ContainsExactCityOwner(city)
            && string.Equals(city.RuntimeId, sentence.CityRuntimeId, StringComparison.Ordinal));

    private bool IsExactRemovedSentence(DailyV1JusticeSentenceOwner sentence) =>
        sentence != null && graph.JusticeRecordsOwner != null
        && !graph.JusticeRecordsOwner.ContainsSentence(sentence)
        && sentence.Warrant != null
        && graph.JusticeRecordsOwner.ContainsWantedRecord(sentence.Warrant)
        && string.Equals(sentence.TargetRuntimeId, actor.RuntimeId, StringComparison.Ordinal)
        && string.Equals(sentence.Warrant.TargetRuntimeId, actor.RuntimeId, StringComparison.Ordinal)
        && string.Equals(sentence.CityRuntimeId, sentence.Warrant.CityRuntimeId, StringComparison.Ordinal);
}

/// <summary>
/// The Core action-slot stage owns P12 current-action census admission,
/// committed-baseline publication, and the shared mutation epoch. The Unity
/// adapter verifies its exact compatibility projection and synchronizes its
/// legacy census mirror after Core accepts the owner write.
/// </summary>
internal interface IDailyV1SelectedP12NpcActionOwnerMutationProtocol
{
    bool TryValidateOwnerSection(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners owner,
        string sectionId);
    bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds);
    bool TryRefreshCompatibilityBaselines(IReadOnlyCollection<string> sectionIds);
    void FaultClosed();
}

/// <summary>Host protocol bridge for the Core-owned selected action-slot P12 transition.</summary>
internal interface IDailyV1SelectedActionSlotP12MutationBoundary
{
    bool OwnsSelectedActionSlotP12Mutation { get; }
    bool CanCommitSelectedActionSlotP12Mutation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor);
    bool NotifySelectedActionSlotP12MutationCommitted(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        string sectionId,
        long revision);
}

/// <summary>
/// Core-owned ordered selected Daily-v1 actor loop. The selected owner graph
/// supplies actor and current-location truth; the host forwards only remaining
/// compatibility, projection and admission operations.
/// </summary>
internal static class DailyV1ActorTurnExecutionStage
{
    internal static bool TryBuildRoster(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRoster,
        out IReadOnlyList<DailyV1ActorTurnRosterEntry> roster)
    {
        roster = null;
        if (graph == null || runtimeRoster == null) return false;

        Dictionary<string, DailyV1NpcRuntimeOwners> ownersByRuntimeId =
            new Dictionary<string, DailyV1NpcRuntimeOwners>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners owner in graph.Npcs)
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || !ownersByRuntimeId.TryAdd(owner.RuntimeId, owner))
                return false;

        List<DailyV1ActorTurnRosterEntry> built =
            new List<DailyV1ActorTurnRosterEntry>(runtimeRoster.Count);
        HashSet<string> selectedRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        for (int ordinal = 0; ordinal < runtimeRoster.Count; ordinal++)
        {
            DailyV1ActorTurnRosterProjection projection = runtimeRoster[ordinal];
            if (projection == null)
            {
                built.Add(new DailyV1ActorTurnRosterEntry(ordinal, null, null));
                continue;
            }

            if (string.IsNullOrWhiteSpace(projection.RuntimeId))
                return false;

            // Selection is determined by Core graph identity, not a host flag.
            // Dynamic/unselected host entries preserve their person identity and
            // original ordinal but do not acquire a Core owner.
            ownersByRuntimeId.TryGetValue(projection.RuntimeId,
                out DailyV1NpcRuntimeOwners owner);
            if (owner != null && !selectedRuntimeIds.Add(projection.RuntimeId))
                return false;
            if (owner != null
                && graph.PersonStore != null
                && graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    owner.RuntimeId, out PersonRuntime materializedPerson)
                && !ReferenceEquals(projection.PersonId, materializedPerson.PersonId))
                return false;
            built.Add(new DailyV1ActorTurnRosterEntry(ordinal, projection.PersonId, owner));
        }

        roster = built.AsReadOnly();
        return true;
    }

    internal static bool TryRunOrdered(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        ScheduledDirectiveStore scheduledDirectiveStore,
        long absoluteDay,
        bool merchantTradeEnabled,
        IDailyV1ActorTurnExecutionBoundary boundary)
    {
        return TryRunOrdered(
            graph,
            roster,
            personStore,
            actorChoiceStore,
            scheduledDirectiveStore,
            absoluteDay,
            merchantTradeEnabled,
            requireCoreOwnedSelectedOperations: false,
            boundary);
    }

    internal static bool TryRunOrdered(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        ScheduledDirectiveStore scheduledDirectiveStore,
        long absoluteDay,
        bool merchantTradeEnabled,
        bool requireCoreOwnedSelectedOperations,
        IDailyV1ActorTurnExecutionBoundary boundary)
    {
        return TryRunOrdered(
            graph, roster, personStore, actorChoiceStore, scheduledDirectiveStore,
            absoluteDay, merchantTradeEnabled, requireCoreOwnedSelectedOperations,
            null, boundary);
    }

    internal static bool TryRunOrdered(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        ScheduledDirectiveStore scheduledDirectiveStore,
        long absoluteDay,
        bool merchantTradeEnabled,
        bool requireCoreOwnedSelectedOperations,
        DailyV1ActorTurnCoreRoots coreRoots,
        IDailyV1ActorTurnExecutionBoundary boundary,
        IDailyV1SelectedMerchantTradeStateBoundary selectedMerchantTradeBoundary = null)
    {
        if (graph == null || roster == null || personStore == null
            || actorChoiceStore == null || boundary == null)
            throw new ArgumentNullException("Selected actor turns require their exact graph, roster, owners and host boundary.");
        if (!ReferenceEquals(actorChoiceStore.PersonStore, personStore))
            throw new InvalidOperationException("Selected actor turns must use the PersonStore owned by their actor-choice store.");
        if (graph.PersonStore != null && !ReferenceEquals(graph.PersonStore, personStore))
            return false;
        if (absoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(absoluteDay));

        List<DailyV1NpcRuntimeOwners> rosterOwners = new List<DailyV1NpcRuntimeOwners>();
        for (int ordinal = 0; ordinal < roster.Count; ordinal++)
        {
            DailyV1ActorTurnRosterEntry entry = roster[ordinal];
            if (entry == null || entry.RosterOrdinal != ordinal)
                return false;
            DailyV1NpcRuntimeOwners actor = entry.ActorOwner;
            if (actor != null
                && graph.PersonStore != null
                && graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    actor.RuntimeId, out PersonRuntime materializedPerson)
                && !ReferenceEquals(entry.PersonId, materializedPerson.PersonId))
                return false;
            if (actor != null
                && (!graph.ContainsExactNpcOwner(actor)
                    || !ReferenceEquals(graph.Definitions, actor.Definitions)
                    || actor.Lifecycle == null
                    || actor.Travel == null))
                return false;
            if (actor != null) rosterOwners.Add(actor);
        }

        // The runtime roster is the actor-turn authority for this boundary.
        // Checking only that each roster actor belongs to the graph would allow
        // a graph-owned actor omitted from the host roster to miss its turn.
        if (!graph.MatchesExactOwnerInstances(graph.Cities, rosterOwners))
            return false;

        if (requireCoreOwnedSelectedOperations
            && (coreRoots == null
                || !coreRoots.IsBoundTo(graph, personStore, actorChoiceStore)))
            throw new InvalidOperationException(
                "The admitted Daily-v1 actor turn requires exact Core roots from its bound execution state and owner graph.");

        if (requireCoreOwnedSelectedOperations)
        {
            if (!(boundary is IDailyV1SelectedActorActionExecutionBoundary selectedActionBoundary)
                || !(boundary is IDailyV1SelectedActorChoiceExecutionContext)
                || !(boundary is IDailyV1CoreActorChoiceExecutionBoundary))
                throw new InvalidOperationException(
                    "An admitted Daily-v1 actor turn requires Core-owned action and actor-choice execution boundaries.");

            foreach (DailyV1ActorTurnRosterEntry entry in roster)
            {
                if (entry.ActorOwner != null
                    && !selectedActionBoundary.UsesSelectedDailyV1ActionExecution(entry.ActorOwner))
                    throw new InvalidOperationException(
                        "An admitted Daily-v1 actor cannot fall back to host-owned action execution.");
            }

            if (merchantTradeEnabled
                && (!(boundary is IDailyV1SelectedMerchantTradeStateP12HostPorts)
                    || !(boundary is IDailyV1SelectedMerchantTradeStateAdmissionBoundary)))
                throw new InvalidOperationException(
                    "An admitted Daily-v1 Merchant turn requires Core trade-state effect ports and its P12 operation admission boundary.");
        }

        IDailyV1SelectedMerchantTradeStateBoundary coreMerchantTradeBoundary =
            selectedMerchantTradeBoundary;
        if (merchantTradeEnabled)
        {
            if (requireCoreOwnedSelectedOperations)
            {
                if (coreMerchantTradeBoundary == null
                    && boundary is IDailyV1SelectedMerchantTradeStateP12HostPorts merchantHostPorts
                    && DailyV1CoreSelectedMerchantTradeStateBoundary.TryCreate(
                        graph, coreRoots, merchantHostPorts,
                        out DailyV1CoreSelectedMerchantTradeStateBoundary selectedCoreMerchantBoundary))
                    coreMerchantTradeBoundary = selectedCoreMerchantBoundary;
                if (!(coreMerchantTradeBoundary is DailyV1CoreSelectedMerchantTradeStateBoundary coreBoundary)
                    || !coreBoundary.IsBoundTo(graph, coreRoots))
                    throw new InvalidOperationException(
                        "The admitted Daily-v1 Merchant operation requires Core owner writes and exact P12 effect ports.");
            }
            else
            {
                coreMerchantTradeBoundary ??= boundary as IDailyV1SelectedMerchantTradeStateBoundary;
            }
        }

        foreach (DailyV1ActorTurnRosterEntry entry in roster)
        {
            DailyV1NpcRuntimeOwners actor = entry.ActorOwner;
            if (actor == null) continue;

            int rosterOrdinal = entry.RosterOrdinal;
            PersonId personId = entry.PersonId;
            if (actor.Lifecycle.LifeState != NpcLifeState.Alive)
            {
                RejectActorChoicesForPerson(actorChoiceStore, personId, absoluteDay, rosterOrdinal);
                continue;
            }

            bool hasPendingChoice = actorChoiceStore.TryGetNextPendingForActor(personId, out _);
            if (hasPendingChoice
                && !IsCurrentMaterializedPersonActor(
                    personStore, personId, actor.RuntimeId, absoluteDay))
            {
                RejectActorChoicesForPerson(actorChoiceStore, personId, absoluteDay, rosterOrdinal);
                continue;
            }

            if (hasPendingChoice && graph.IsPersonInSpatialTransit(personId))
            {
                TryProcessScheduledDirective(graph, actor, scheduledDirectiveStore, absoluteDay, boundary,
                    requireCoreOwnedSelectedOperations, coreRoots);
                DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                    rosterOrdinal, ActorChoiceDeferralReason.Traveling);
                continue;
            }

            if (actor.Travel.IsTraveling)
            {
                TryProcessScheduledDirective(graph, actor, scheduledDirectiveStore, absoluteDay, boundary,
                    requireCoreOwnedSelectedOperations, coreRoots);
                DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                    rosterOrdinal, ActorChoiceDeferralReason.Traveling);
                continue;
            }

            if (graph.Expeditions != null && graph.Expeditions.IsNpcOnActiveExpedition(actor.RuntimeId))
            {
                TryProcessScheduledDirective(graph, actor, scheduledDirectiveStore, absoluteDay, boundary,
                    requireCoreOwnedSelectedOperations, coreRoots);
                DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                    rosterOrdinal, ActorChoiceDeferralReason.ExpeditionParticipant);
                continue;
            }

            if (IsReservedToday(actor, boundary, requireCoreOwnedSelectedOperations))
            {
                DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                    rosterOrdinal, ActorChoiceDeferralReason.ReservedExpeditionActivity);
                continue;
            }

            if (actorChoiceStore.TryGetNextPendingForActor(personId, out _))
            {
                if (TryProcessScheduledDirective(graph, actor, scheduledDirectiveStore, absoluteDay, boundary,
                        requireCoreOwnedSelectedOperations, coreRoots))
                {
                    DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                        rosterOrdinal, ActorChoiceDeferralReason.ScheduledDirective);
                    continue;
                }

                if (TryProcessActorChoice(graph, actor, personId, actorChoiceStore,
                        absoluteDay, rosterOrdinal, boundary,
                        requireCoreOwnedSelectedOperations, coreRoots))
                    continue;
            }

            if (merchantTradeEnabled)
                AdvanceMerchantTradeState(graph, actor, absoluteDay, boundary,
                    requireCoreOwnedSelectedOperations, coreRoots,
                    coreMerchantTradeBoundary);

            if (TryProcessScheduledDirective(graph, actor, scheduledDirectiveStore, absoluteDay, boundary,
                    requireCoreOwnedSelectedOperations, coreRoots))
            {
                DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                    rosterOrdinal, ActorChoiceDeferralReason.ScheduledDirective);
                continue;
            }

            if (TryProcessActorChoice(graph, actor, personId, actorChoiceStore,
                    absoluteDay, rosterOrdinal, boundary,
                    requireCoreOwnedSelectedOperations, coreRoots))
                continue;

            if (requireCoreOwnedSelectedOperations)
            {
                if (!(boundary is IDailyV1SelectedActorActionExecutionBoundary selectedActionBoundary))
                    throw new InvalidOperationException(
                        "The admitted Daily-v1 actor lost its Core-owned action boundary during its turn.");
                if (!DailyV1SelectedActorActionExecutionStage.TryExecuteAutonomousAction(
                        graph, actor, personStore,
                        requireCoreOwnedSelectedOperations
                            ? coreRoots.RandomSource : selectedActionBoundary.RandomSource,
                        absoluteDay, selectedActionBoundary, coreRoots))
                    throw new InvalidOperationException(
                        "The exact selected Daily-v1 actor could not complete its Core-owned action turn.");
            }
            else
            {
                if (boundary is IDailyV1SelectedActorActionExecutionBoundary selectedActionBoundary
                    && selectedActionBoundary.UsesSelectedDailyV1ActionExecution(actor))
                {
                    if (!DailyV1SelectedActorActionExecutionStage.TryExecuteAutonomousAction(
                            graph, actor, personStore, selectedActionBoundary.RandomSource,
                            absoluteDay, selectedActionBoundary))
                        throw new InvalidOperationException(
                            "The exact selected Daily-v1 actor could not complete its Core-owned action turn.");
                }
                else
                {
                    boundary.EvaluateAndExecuteAction(actor);
                }
            }
        }

        RejectActorChoicesWithoutMaterializedTurn(actorChoiceStore, roster, absoluteDay);
        return true;
    }

    private static bool TryProcessScheduledDirective(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        ScheduledDirectiveStore directiveStore,
        long absoluteDay,
        IDailyV1ActorTurnExecutionBoundary boundary,
        bool requireCoreOwnedSelectedOperations,
        DailyV1ActorTurnCoreRoots coreRoots)
    {
        IDailyV1SelectedActorActionExecutionBoundary coreActionBoundary =
            boundary as IDailyV1SelectedActorActionExecutionBoundary;
        if (requireCoreOwnedSelectedOperations
            && !(boundary is IDailyV1SelectedActorActionExecutionBoundary))
            throw new InvalidOperationException(
                "The admitted Daily-v1 actor lost its Core-owned directive boundary during its turn.");
        if (directiveStore == null
            || !directiveStore.TryTakeDirective(actor.RuntimeId, out ScheduledDirective directive))
            return false;

        long directiveRevisionBefore = directiveStore.Revision;

        if (boundary is IDailyV1SelectedActorActionExecutionBoundary selectedActionBoundary
            && (requireCoreOwnedSelectedOperations
                || selectedActionBoundary.UsesSelectedDailyV1ActionExecution(actor)))
        {
            if (directive.Mode == ScheduledDirectiveMode.RequestAction)
            {
                DailyV1SelectedActorActionExecutionStage.ProcessScheduledRequestAction(
                    graph, actor, directive,
                    requireCoreOwnedSelectedOperations
                        ? coreRoots.RandomSource : selectedActionBoundary.RandomSource,
                    absoluteDay, selectedActionBoundary, coreRoots);
            }
            else if (directive.Mode == ScheduledDirectiveMode.ForceOutcome)
            {
                DailyV1SelectedActorActionExecutionStage.ProcessScheduledForceOutcome(
                    graph, actor, directive, absoluteDay, selectedActionBoundary);
            }
            else
            {
                throw new InvalidOperationException(
                    "The selected Daily-v1 directive store contains a mode outside its supported Core contract.");
            }
        }
        else
        {
            if (requireCoreOwnedSelectedOperations)
                throw new InvalidOperationException(
                    "The admitted Daily-v1 actor cannot delegate directive execution to its host.");
            boundary.ProcessScheduledDirective(actor, directive);
        }

        if (directive.State != ScheduledDirectiveState.Pending
            && coreActionBoundary is IDailyV1SelectedScheduledDirectiveMutationBoundary mutationBoundary
            && !mutationBoundary.TryCommitScheduledDirectiveMutation(
                actor, directiveStore, directive, directiveRevisionBefore))
            throw new InvalidOperationException(
                "The selected Daily-v1 directive result could not commit its exact P12F owner census and shared mutation epoch.");
        return true;
    }

    private static bool TryProcessActorChoice(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        PersonId personId,
        ActorChoiceStore store,
        long absoluteDay,
        int rosterOrdinal,
        IDailyV1ActorTurnExecutionBoundary boundary,
        bool requireCoreOwnedSelectedOperations,
        DailyV1ActorTurnCoreRoots coreRoots)
    {
        if (requireCoreOwnedSelectedOperations
            && !(boundary is IDailyV1SelectedActorActionExecutionBoundary))
            throw new InvalidOperationException(
                "The admitted Daily-v1 actor lost its Core-owned choice boundary during its turn.");
        if (boundary is IDailyV1SelectedActorActionExecutionBoundary selectedActionBoundary
            && (requireCoreOwnedSelectedOperations
                || selectedActionBoundary.UsesSelectedDailyV1ActionExecution(actor)))
        {
            // This helper is also reached after the ordinary directive slot
            // when no choice is pending. Avoid entering the strict selected
            // choice boundary unless there is an actual captured input; keep
            // the legacy callback's existing invocation order unchanged.
            if (!store.TryGetNextPendingForActor(personId, out _))
                return false;

            if (!(boundary is IDailyV1SelectedActorChoiceExecutionContext selectedContext))
                throw new InvalidOperationException(
                    "The selected Daily-v1 actor omitted its required Core actor-choice context.");
            if (!selectedContext.TryGetActorChoiceExecutionContext(actor,
                    out IDailyV1ActorChoiceExecutionBoundary executionBoundary))
                throw new InvalidOperationException(
                    "The selected Daily-v1 actor could not provide its exact action projection boundary.");
            if (executionBoundary == null)
                throw new InvalidOperationException(
                    "The selected Core actor-choice context omitted its projection/admission boundary.");
            if (requireCoreOwnedSelectedOperations
                && (!(executionBoundary is IDailyV1CoreActorChoiceExecutionBoundary coreChoiceBoundary)
                    || !ReferenceEquals(coreChoiceBoundary, selectedActionBoundary)))
                throw new InvalidOperationException(
                    "The admitted Daily-v1 actor choice must use the exact actor-turn Core boundary.");
            EffectiveMerchantTradeConfiguration merchantTrade =
                graph.Definitions.EffectiveConfiguration?.MerchantTrade;
            if (merchantTrade == null)
                throw new InvalidOperationException(
                    "The selected Core actor-choice stage requires admitted Merchant configuration.");
            return DailyV1ActorChoiceExecutionStage.TryProcessPending(
                graph, actor, personId, store, absoluteDay, rosterOrdinal,
                graph.IsActorAtCurrentCityLocation(actor), merchantTrade.Enabled,
                executionBoundary, requireCoreOwnedSelectedOperations, coreRoots);
        }

        if (requireCoreOwnedSelectedOperations)
            throw new InvalidOperationException(
                "The admitted Daily-v1 actor cannot delegate actor-choice execution to its host.");
        return boundary.TryProcessActorChoice(actor, rosterOrdinal);
    }

    private static void AdvanceMerchantTradeState(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        long absoluteDay,
        IDailyV1ActorTurnExecutionBoundary boundary,
        bool requireCoreOwnedSelectedOperations,
        DailyV1ActorTurnCoreRoots coreRoots,
        IDailyV1SelectedMerchantTradeStateBoundary selectedMerchantBoundary)
    {
        if (requireCoreOwnedSelectedOperations
            && !(boundary is IDailyV1SelectedActorActionExecutionBoundary))
            throw new InvalidOperationException(
                "The admitted Daily-v1 actor lost its Core-owned Merchant boundary during its turn.");
        if (boundary is IDailyV1SelectedActorActionExecutionBoundary selectedActionBoundary
            && (requireCoreOwnedSelectedOperations
                || selectedActionBoundary.UsesSelectedDailyV1ActionExecution(actor)))
        {
            if (selectedMerchantBoundary == null
                || !(boundary is IDailyV1SelectedMerchantTradeStateAdmissionBoundary admissionBoundary))
                throw new InvalidOperationException(
                    "The selected Daily-v1 actor omitted its Core Merchant operation and P12 admission boundaries.");
            if (!admissionBoundary.ExecuteWithinSelectedMerchantDailyOperation(
                    actor,
                    () => DailyV1SelectedMerchantTradeStateOperation.TryAdvance(
                        graph, actor, absoluteDay, selectedMerchantBoundary, coreRoots)))
                throw new InvalidOperationException(
                    "The selected Core Merchant daily operation could not complete through its admitted owner graph for actor '"
                    + actor.RuntimeId + "': "
                    + ((boundary as DailyV1CoreSelectedActorTurnExecutionBoundary)
                        ?.LastMerchantP12Failure ?? "no rejection detail"));
            return;
        }

        // Legacy hosts outside the selected Core boundary retain their
        // existing per-actor callback path.
        if (requireCoreOwnedSelectedOperations)
            throw new InvalidOperationException(
                "The admitted Daily-v1 actor cannot delegate Merchant state execution to its host.");
        boundary.AdvanceMerchantTradeState(actor);
    }

    private static bool IsReservedToday(
        DailyV1NpcRuntimeOwners actor,
        IDailyV1ActorTurnExecutionBoundary boundary,
        bool requireCoreOwnedSelectedOperations)
    {
        if (requireCoreOwnedSelectedOperations)
        {
            // This admitted profile has no site, place, notable-item, or
            // expedition-autonomy producer. Core owns the exact-empty result;
            // a selected Unity host must not supply reservation truth.
            return false;
        }
        if (boundary is IDailyV1SelectedActorActionExecutionBoundary selectedBoundary
            && selectedBoundary.UsesSelectedDailyV1ActionExecution(actor))
        {
            // The accepted UnityBootstrap-Daily-v1 graph has no admitted site,
            // place, notable-item, or expedition-autonomy producer. Active
            // expedition membership is read from the Core Expeditions owner
            // immediately before this exact-empty reservation result.
            return false;
        }

        return boundary.IsReservedToday(actor);
    }

    private static bool IsCurrentMaterializedPersonActor(
        PersonStore personStore,
        PersonId personId,
        string actorRuntimeId,
        long absoluteDay)
    {
        if (personId == null
            || !personStore.TryGet(personId, out PersonRuntime person)
            || person.IsDeadAt(absoluteDay)
            || !person.IsMaterialized
            || !string.Equals(person.MaterializedNpcRuntimeId, actorRuntimeId, StringComparison.Ordinal)
            || !personStore.TryGetByMaterializedNpcRuntimeId(actorRuntimeId, out PersonRuntime byNpcId)
            || !ReferenceEquals(person, byNpcId))
            return false;
        return true;
    }

    private static void RejectActorChoicesForPerson(
        ActorChoiceStore store,
        PersonId personId,
        long absoluteDay,
        int rosterOrdinal)
    {
        if (personId == null) return;
        foreach (ActorChoiceInput input in store.PendingInputs)
        {
            if (input.PersonId.Equals(personId))
                RejectActorChoice(store, input, absoluteDay, rosterOrdinal,
                    ActorChoiceFailure.ActorUnavailable);
        }
    }

    private static void DeferActorChoiceForPerson(
        ActorChoiceStore store,
        PersonId personId,
        long absoluteDay,
        int rosterOrdinal,
        ActorChoiceDeferralReason reason)
    {
        if (!store.TryGetNextPendingForActor(personId, out ActorChoiceInput input)) return;
        if (!store.TryDefer(input.InputId, absoluteDay, rosterOrdinal, reason,
                out ActorChoiceStoreFailureCode failure))
            throw new InvalidOperationException(
                "Actor choice could not record its required deferral: " + failure + ".");
    }

    private static void RejectActorChoicesWithoutMaterializedTurn(
        ActorChoiceStore store,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster,
        long absoluteDay)
    {
        HashSet<PersonId> presentActors = new HashSet<PersonId>();
        foreach (DailyV1ActorTurnRosterEntry entry in roster)
        {
            if (entry.ActorOwner != null && entry.PersonId != null)
                presentActors.Add(entry.PersonId);
        }

        foreach (ActorChoiceInput input in store.PendingInputs)
        {
            if (!presentActors.Contains(input.PersonId))
                RejectActorChoice(store, input, absoluteDay, roster.Count,
                    ActorChoiceFailure.ActorUnavailable);
        }
    }

    private static void RejectActorChoice(
        ActorChoiceStore store,
        ActorChoiceInput input,
        long absoluteDay,
        int rosterOrdinal,
        ActorChoiceFailure reason)
    {
        if (input == null)
            throw new InvalidOperationException(
                "Actor choice could not record its required rejection: missing input.");
        if (!store.TryReject(input.InputId, absoluteDay, rosterOrdinal, reason,
                out ActorChoiceStoreFailureCode failure))
            throw new InvalidOperationException(
                "Actor choice could not record its required rejection: " + failure + ".");
    }
}
