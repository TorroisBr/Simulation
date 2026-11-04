using System;
using System.Collections.Generic;

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
/// The admitted Unity host supplies only effects around the selected actor
/// operations. The Core factory consumes this bundle to construct its own
/// policy boundary, binding roots, roster and exact owners from session state.
/// </summary>
internal interface IDailyV1SelectedActorTurnHostPorts
    : IDailyV1SelectedActorActionHostPorts,
      IDailyV1SelectedActorChoiceHostPorts,
      IDailyV1SelectedMerchantTradeStateP12HostPorts,
      IDailyV1SelectedMerchantTradeStateAdmissionBoundary,
      IDailyV1SelectedActionSlotP12MutationBoundary,
      IDailyV1SelectedCrimeJusticeDailyProjectionPorts,
      IDailyV1SelectedCrimeJusticeDailyMutationHostPort
{
}

/// <summary>
/// Exact host-side census identities needed by the selected Crime/Justice
/// operation. Core retains phase admission, reservation, mutation tracking,
/// and epoch publication; the host only binds its existing census protocol
/// and opaque Unity receipt-owner identities.
/// </summary>
internal sealed class DailyV1SelectedCrimeJusticeDailyMutationHostBinding
{
    internal IDailyV1P12CrimeJusticeMutationProtocol Protocol { get; }
    internal object CrimeOwnerIdentity { get; }
    internal object CrimeReceiptCensusOwnerIdentity { get; }
    internal object JusticeOwnerIdentity { get; }
    internal object JusticeReceiptCensusOwnerIdentity { get; }
    internal bool AdmissionEnabled { get; }

    internal DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
        IDailyV1P12CrimeJusticeMutationProtocol protocol,
        object crimeOwnerIdentity,
        object crimeReceiptCensusOwnerIdentity,
        object justiceOwnerIdentity,
        object justiceReceiptCensusOwnerIdentity,
        bool admissionEnabled)
    {
        Protocol = protocol;
        CrimeOwnerIdentity = crimeOwnerIdentity;
        CrimeReceiptCensusOwnerIdentity = crimeReceiptCensusOwnerIdentity;
        JusticeOwnerIdentity = justiceOwnerIdentity;
        JusticeReceiptCensusOwnerIdentity = justiceReceiptCensusOwnerIdentity;
        AdmissionEnabled = admissionEnabled;
    }
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
      IDailyV1SelectedCrimeJusticeDailyBoundary
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;
    private readonly PersonStore personStore;
    private readonly ActorChoiceStore actorChoiceStore;
    private readonly DailyV1ActorTurnCoreRoots coreRoots;
    private readonly IReadOnlyCollection<DailyV1NpcRuntimeOwners> exactRosterOwners;
    private readonly IDailyV1SelectedActorTurnHostPorts hostPorts;
    private IDailyV1SelectedMerchantTradeStateBoundary merchantBoundary;

    private DailyV1CoreSelectedActorTurnExecutionBoundary(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        DailyV1ActorTurnCoreRoots coreRoots,
        IReadOnlyCollection<DailyV1NpcRuntimeOwners> exactRosterOwners,
        IDailyV1SelectedActorTurnHostPorts hostPorts)
    {
        this.executionState = executionState;
        this.graph = graph;
        this.personStore = personStore;
        this.actorChoiceStore = actorChoiceStore;
        this.coreRoots = coreRoots;
        this.exactRosterOwners = exactRosterOwners;
        this.hostPorts = hostPorts;
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
        boundary = null;
        if (executionState == null || graph == null || personStore == null
            || actorChoiceStore == null || coreRoots == null || hostPorts == null
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
            selectedOwners.AsReadOnly(), hostPorts);
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
    public bool RequiresActionProjection => hostPorts.RequiresActionProjection;
    public bool UsesSelectedDailyV1ActionExecution(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor);

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
        hostPorts.LogSkippedDirective(directive, reason);

    public bool TryCreateActionProjectionBoundary(
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1SelectedActionProjectionBoundary projectionBoundary)
    {
        projectionBoundary = null;
        return TryGetExactActor(actor)
            && hostPorts.TryCreateActionProjectionBoundary(actor, out projectionBoundary)
            && projectionBoundary != null;
    }

    public bool TryPrepareSelectedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate selected,
        IDailyV1SelectedActionProjectionBoundary projectionBoundary,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor)
        && hostPorts.TryPrepareSelectedActionProjection(
            actor, selected, projectionBoundary, payload);

    public bool CanCommitSelectedActionSlot(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && hostPorts.CanCommitSelectedActionSlot(actor, payload);

    public bool TryPublishSelectedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && hostPorts.TryPublishSelectedActionProjection(actor, payload);

    public void CancelPreparedSelectedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload)
    {
        RequireExactActor(actor);
        hostPorts.CancelPreparedSelectedActionProjection(actor, payload);
    }

    public void LogSelectedActionDecisionFailure(DailyV1NpcRuntimeOwners actor, string message)
    {
        RequireExactActor(actor);
        hostPorts.LogSelectedActionDecisionFailure(actor, message);
    }

    public bool TryPrepareCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor) && hostPorts.TryPrepareCurrentActionClearProjection(actor);

    public bool CanCommitCurrentActionClear(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor) && hostPorts.CanCommitCurrentActionClear(actor);

    public bool TryPublishCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor) && hostPorts.TryPublishCurrentActionClearProjection(actor);

    public void CancelPreparedCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor)
    {
        RequireExactActor(actor);
        hostPorts.CancelPreparedCurrentActionClearProjection(actor);
    }

    public void LogChosenActionTarget(DailyV1NpcRuntimeOwners actor)
    {
        RequireExactActor(actor);
        hostPorts.LogChosenActionTarget(actor);
    }

    public bool TryGetDispatchBoundary(
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1SelectedActionDispatchBoundary dispatchBoundary)
    {
        dispatchBoundary = null;
        return TryGetExactActor(actor)
            && hostPorts.TryGetDispatchBoundary(actor, out dispatchBoundary)
            && dispatchBoundary != null;
    }

    public bool TryCompleteDispatch(
        DailyV1NpcRuntimeOwners actor,
        DailyV1SelectedActionDispatchResult dispatch,
        out string resultMessage)
    {
        resultMessage = null;
        return TryGetExactActor(actor)
            && hostPorts.TryCompleteDispatch(actor, dispatch, out resultMessage);
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
        return hostPorts.TryGetActionStatusBoundary(
            actor, target, reuseExistingOperation, out boundary)
            && boundary != null;
    }

    public bool TryPrepareForcedOutcomeActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor)
        && hostPorts.TryPrepareForcedOutcomeActionProjection(actor, action, payload);

    public bool TryGetForcedEscapeBoundary(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
    {
        boundary = null;
        return TryGetExactActor(actor)
            && hostPorts.TryGetForcedEscapeBoundary(actor, action, out boundary)
            && boundary != null;
    }

    public bool TryPrepareRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && hostPorts.TryPrepareRequestedActionProjection(actor, payload);

    public bool TryRefreshRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && hostPorts.TryRefreshRequestedActionProjection(actor, payload);

    public bool CanCommitRequestedActionSlot(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && hostPorts.CanCommitRequestedActionSlot(actor, payload);

    public bool TryPublishRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload) =>
        TryGetExactActor(actor) && hostPorts.TryPublishRequestedActionProjection(actor, payload);

    public void CancelPreparedRequestedActionProjection(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload)
    {
        RequireExactActor(actor);
        hostPorts.CancelPreparedRequestedActionProjection(actor, payload);
    }

    public void LogRequestedActionDecisionFailure(string message) =>
        hostPorts.LogRequestedActionDecisionFailure(message);

    public bool OwnsSelectedActionSlotP12Mutation => hostPorts.OwnsSelectedActionSlotP12Mutation;

    public bool CanCommitSelectedActionSlotP12Mutation(
        DailyV1SelectedRuntimeOwnerGraph expectedGraph,
        DailyV1NpcRuntimeOwners actor) =>
        ReferenceEquals(expectedGraph, graph)
        && TryGetExactActor(actor)
        && hostPorts.CanCommitSelectedActionSlotP12Mutation(graph, actor);

    public bool NotifySelectedActionSlotP12MutationCommitted(
        DailyV1SelectedRuntimeOwnerGraph expectedGraph,
        DailyV1NpcRuntimeOwners actor,
        string sectionId,
        long revision) =>
        ReferenceEquals(expectedGraph, graph)
        && TryGetExactActor(actor)
        && hostPorts.NotifySelectedActionSlotP12MutationCommitted(
            graph, actor, sectionId, revision);

    public bool CanAdmitSelectedMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor) && hostPorts.CanAdmitSelectedMerchantPlanMutation(actor);

    public bool NotifySelectedMerchantPlanMutationCommitted(
        DailyV1NpcRuntimeOwners actor,
        long revision) =>
        TryGetExactActor(actor)
        && hostPorts.NotifySelectedMerchantPlanMutationCommitted(actor, revision);

    public bool CanProjectSelectedMerchantTravelPlanMutation(
        DailyV1NpcRuntimeOwners actor,
        string targetLocationRuntimeId,
        string targetCityRuntimeId) =>
        TryGetExactActor(actor)
        && hostPorts.CanProjectSelectedMerchantTravelPlanMutation(
            actor, targetLocationRuntimeId, targetCityRuntimeId);

    public bool CanAdmitSelectedMerchantTravelPlanMutation(DailyV1NpcRuntimeOwners actor) =>
        TryGetExactActor(actor)
        && hostPorts.CanAdmitSelectedMerchantTravelPlanMutation(actor);

    public bool NotifySelectedMerchantTravelPlanMutationCommitted(
        DailyV1NpcRuntimeOwners actor,
        long revision) =>
        TryGetExactActor(actor)
        && hostPorts.NotifySelectedMerchantTravelPlanMutationCommitted(actor, revision);

    public NpcDecisionRecord RecordTradeRedirect(
        DailyV1NpcRuntimeOwners actor,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence evidence) =>
        TryGetExactActor(actor)
            ? hostPorts.RecordTradeRedirect(actor, targetLocationRuntimeId, evidence)
            : throw new InvalidOperationException("A Merchant decision referenced an actor outside the Core roster.");

    public void LogTradeDecisionFailure(string message) => hostPorts.LogTradeDecisionFailure(message);
    public void LogTrade(string message) => hostPorts.LogTrade(message);

    public bool ExecuteWithinSelectedMerchantDailyOperation(
        DailyV1NpcRuntimeOwners actor,
        Func<bool> operation) =>
        TryGetExactActor(actor)
        && hostPorts.ExecuteWithinSelectedMerchantDailyOperation(actor, operation);

    public DailyV1JusticeRecordsOwner JusticeRecordsOwner => graph.JusticeRecordsOwner;

    public void ExecuteWithinDailyMutation(
        DailyV1SelectedCrimeJusticeDailyPhase phase,
        Action mutation)
    {
        if (!Enum.IsDefined(typeof(DailyV1SelectedCrimeJusticeDailyPhase), phase)
            || mutation == null)
            throw new ArgumentException("The admitted Core actor turn requires a valid Crime/Justice P12 operation.");
        if (!hostPorts.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding)
            || binding == null)
        {
            executionState.MarkSelectedRuntimeCensusProtocolFaulted();
            throw new InvalidOperationException(
                "The selected Crime/Justice operation could not bind its exact host census identities to the admitted Core session.");
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
        IReadOnlyList<DailyV1StatusDefinition> statusToAdd) =>
        TryGetExactActor(owner) && hostPorts.CanProjectStatusChanges(owner, statusToAdd);

    public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners owner)
    {
        RequireExactActor(owner);
        hostPorts.EnsureMutationAllowed(owner);
    }

    public void NotifyStatusMutationCommitted(
        DailyV1NpcRuntimeOwners owner,
        DailyV1StatusDefinition status,
        bool added)
    {
        RequireExactActor(owner);
        hostPorts.NotifyStatusMutationCommitted(owner, status, added);
    }

    public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners owner)
    {
        RequireExactActor(owner);
        hostPorts.NotifyHiddenMutationCommitted(owner);
    }

    public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
        hostPorts.CanRemoveSentenceProjection(sentence);

    public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence) =>
        hostPorts.CommitSentenceProjectionRemoval(sentence);

    public void Log(SimulationLogCategory category, string message) => hostPorts.Log(category, message);
    public void LogWarning(string message) => hostPorts.LogWarning(message);
}

/// <summary>
/// The Core action-slot stage owns the P12 current-action section transition.
/// The host adapter supplies the registered census protocol, while Core
/// decides when the exact section is admitted and when its committed write is
/// published after the Unity projection succeeds.
/// </summary>
internal interface IDailyV1SelectedP12NpcActionOwnerMutationProtocol
{
    bool TryValidateOwnerSection(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners owner,
        string sectionId);
    bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds);
    bool NotifyCommittedMutations(IReadOnlyCollection<string> sectionIds);
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
        if (requireCoreOwnedSelectedOperations
            && !(boundary is IDailyV1SelectedActorActionExecutionBoundary))
            throw new InvalidOperationException(
                "The admitted Daily-v1 actor lost its Core-owned directive boundary during its turn.");
        if (directiveStore == null
            || !directiveStore.TryTakeDirective(actor.RuntimeId, out ScheduledDirective directive))
            return false;

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
