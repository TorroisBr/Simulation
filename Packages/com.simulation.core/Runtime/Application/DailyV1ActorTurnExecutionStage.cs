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
    private readonly IDailyV1SelectedP12NpcActionOwnerMutationProtocol actionSlotP12MutationProtocol;
    private IDailyV1SelectedMerchantTradeStateBoundary merchantBoundary;

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
                new[] { ScheduledDirectiveCensusProvider.SectionId })
            || !executionState.MutationEpochState.TryAdvanceCommittedMutation())
        {
            protocol.FaultClosed();
            return false;
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
        if (dispatch.Kind == DailyV1SelectedActionDispatchKind.Crime)
            return dispatch.Crime != null
                && dispatch.ActionRollSucceeded == dispatch.Crime.ActionRollSucceeded
                && dispatch.Succeeded == dispatch.Crime.Succeeded
                && DailyV1CoreOnlyCrimeActionBoundary.IsExactTargetlessHideSlot(
                    graph, actor, action, payload);

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
        boundary = null;
        if (!TryGetExactActor(actor)
            || (target != null && !graph.ContainsExactNpcOwner(target)))
            return false;
        if (hostPorts != null && hostPorts.TryGetActionStatusBoundary(
                actor, target, reuseExistingOperation, out boundary)
            && boundary != null)
            return true;
        boundary = null;
        return !reuseExistingOperation
            && !RequiresActionProjection
            ? DailyV1SelectedActionExecutionStage.CoreSelectedActionStatusOperationBoundary.TryCreate(
                executionState, graph, null, actor, target, null, out boundary)
                && boundary != null
            : reuseExistingOperation && !RequiresActionProjection
                && DailyV1SelectedActionExecutionStage.CoreSelectedActionStatusOperationBoundary
                    .TryCreateForForcedEscapeScopeReuse(
                        executionState, graph, actor, target, out boundary)
                && boundary != null;
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
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && RequireHostPorts().TryPrepareRequestedActionProjection(actor, payload);

    public bool TryRefreshRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && RequireHostPorts().TryRefreshRequestedActionProjection(actor, payload);

    public bool CanCommitRequestedActionSlot(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && RequireHostPorts().CanCommitRequestedActionSlot(actor, payload);

    public bool TryPublishRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && RequireHostPorts().TryPublishRequestedActionProjection(actor, payload);

    public void CancelPreparedRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload)
    {
        RequireExactActor(actor);
        RequireHostPorts().CancelPreparedRequestedActionProjection(actor, payload);
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
        long revision) =>
        ReferenceEquals(expectedGraph, graph)
        && TryGetExactActor(actor)
        && executionState.TryRecordSelectedNpcActionSlotMutation(
            graph, actor, sectionId, revision, actionSlotP12MutationProtocol);

    public bool CanAdmitSelectedMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor) && RequireHostPorts().CanAdmitSelectedMerchantPlanMutation(actor);

    public bool NotifySelectedMerchantPlanMutationCommitted(
        DailyV1NpcRuntimeOwners actor,
        long revision) =>
        TryGetExactActor(actor)
        && RequireHostPorts().NotifySelectedMerchantPlanMutationCommitted(actor, revision);

    public bool CanProjectSelectedMerchantTravelPlanMutation(
        DailyV1NpcRuntimeOwners actor,
        string targetLocationRuntimeId,
        string targetCityRuntimeId) =>
        TryGetExactActor(actor)
        && RequireHostPorts().CanProjectSelectedMerchantTravelPlanMutation(
            actor, targetLocationRuntimeId, targetCityRuntimeId);

    public bool CanAdmitSelectedMerchantTravelPlanMutation(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor)
        && RequireHostPorts().CanAdmitSelectedMerchantTravelPlanMutation(actor);

    public bool NotifySelectedMerchantTravelPlanMutationCommitted(
        DailyV1NpcRuntimeOwners actor,
        long revision) =>
        TryGetExactActor(actor)
        && RequireHostPorts().NotifySelectedMerchantTravelPlanMutationCommitted(actor, revision);

    public NpcDecisionRecord RecordTradeRedirect(
        DailyV1NpcRuntimeOwners actor,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence evidence) =>
        TryGetExactActor(actor)
            ? RequireHostPorts().RecordTradeRedirect(actor, targetLocationRuntimeId, evidence)
            : throw new InvalidOperationException("A Merchant decision referenced an actor outside the Core roster.");

    public void LogTradeDecisionFailure(string message) => RequireHostPorts().LogTradeDecisionFailure(message);
    public void LogTrade(string message) => RequireHostPorts().LogTrade(message);

    public bool ExecuteWithinSelectedMerchantDailyOperation(
        DailyV1NpcRuntimeOwners actor,
        Func<bool> operation) =>
        TryGetExactActor(actor)
        && RequireHostPorts().ExecuteWithinSelectedMerchantDailyOperation(actor, operation);

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
/// Hostless Core boundary for the selected Hide action and the existing
/// scheduled ForceOutcome/EscapePrison operation. Domain execution, owner
/// writes and P12 reservation remain in Core; Unity callbacks are omitted
/// presentation effects, while every unsupported action path fails closed.
/// </summary>
internal sealed class DailyV1CoreOnlyCrimeActionBoundary :
    IDailyV1SelectedActionDispatchBoundary,
    DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionProjectionBoundary,
    IDailyV1SelectedCrimeJusticeDailyMutationHostPort
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;
    private readonly DailyV1NpcRuntimeOwners actor;
    private readonly DailyV1ActionDefinition action;
    private readonly DailyV1NpcActionPayloadOwner payload;
    private readonly bool forcedEscape;

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
            || !IsExactTargetlessHideSlot(graph, actor, action, payload)
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
        if (!IsExactActor(candidate) || statusToAdd == null || candidate.Status == null)
            return false;
        foreach (DailyV1StatusDefinition status in statusToAdd)
            if (status == null
                || !graph.Definitions.Statuses.TryGetValue(status.DefinitionId,
                    out DailyV1StatusDefinition admittedStatus)
                || !ReferenceEquals(status, admittedStatus))
                return false;
        return true;
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
        // Publication is a Unity presentation effect; the Core Justice/NPC
        // owners already contain the authoritative transition.
    }

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
                    "The selected Core Merchant daily operation could not complete through its admitted owner graph.");
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
