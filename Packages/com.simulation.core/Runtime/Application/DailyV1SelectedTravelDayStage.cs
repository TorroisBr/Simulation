using System;
using System.Collections.Generic;

/// <summary>
/// Unity's selected-travel adapter may validate and publish projections and
/// record the already registered P12 owner invalidations. It cannot choose a
/// travel result or mutate a selected Core owner.
/// </summary>
internal interface IDailyV1SelectedTravelDayBoundary
{
    bool TryPrepareTransition(
        DailyV1NpcRuntimeOwners npc,
        DailyV1SelectedTravelTransitionPlan plan,
        bool arriving,
        out object projectionReceipt);

    void PublishCommittedTransition(
        DailyV1NpcRuntimeOwners npc,
        DailyV1SelectedTravelTransitionPlan plan,
        bool arriving,
        object projectionReceipt);

    bool PublishTravelPartyArrival(TravelPartyRuntime party);

    void PublishSoloTravelResult(
        DailyV1SelectedSoloTravelDayResult result,
        bool arrivalEventHandledByCore);
}

/// <summary>
/// Host protocol port for the selected party-advance census operation. The
/// Core session owns the operation scope and changed-section set; the host
/// only verifies its registered owner witnesses and exposes the existing
/// census protocol operations.
/// </summary>
internal interface IDailyV1SelectedP12TravelPartyAdvanceProtocol
{
    bool TryValidateOwnerSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<string> expectedSectionIds);

    bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds);
    bool TryRefreshCommittedMutationBaselines(IReadOnlyCollection<string> sectionIds);
    bool NotifyCommittedMutations(IReadOnlyCollection<string> sectionIds);
    void FaultClosed();
}

/// <summary>
/// Core-only P12 protocol for detached selected Daily-v1 travel execution.
/// The operation cut, source identities, live reads, committed baselines, and
/// shared epoch remain owned by the exact admitted Core execution state.
/// </summary>
internal sealed class DailyV1CoreSelectedP12TravelPartyAdvanceProtocol
    : IDailyV1SelectedP12TravelPartyAdvanceProtocol
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;
    private readonly DailyV1CoreSelectedOwnerSectionMutationProtocol mutationProtocol;

    private DailyV1CoreSelectedP12TravelPartyAdvanceProtocol(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        this.executionState = executionState;
        this.graph = graph;
        mutationProtocol = new DailyV1CoreSelectedOwnerSectionMutationProtocol(
            executionState, graph);
    }

    internal static bool TryCreate(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1CoreSelectedP12TravelPartyAdvanceProtocol protocol)
    {
        protocol = null;
        if (executionState == null || graph == null
            || !executionState.HasAdmittedSelectedDailyExecution
            || !executionState.HasSealedSelectedOwnerSectionCensusRegistry
            || !executionState.HasRuntimeProtocolOperationInventory
            || executionState.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !executionState.AdmissionContext.IsOwnedByCurrentThread()
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
            || graph.TravelParties == null)
            return false;

        protocol = new DailyV1CoreSelectedP12TravelPartyAdvanceProtocol(
            executionState, graph);
        return true;
    }

    public bool TryValidateOwnerSections(
        DailyV1SelectedRuntimeOwnerGraph candidateGraph,
        IReadOnlyList<string> expectedSectionIds) =>
        ReferenceEquals(candidateGraph, graph)
        && executionState.TryValidateCoreSelectedTravelPartyAdvanceOwnerSections(
            graph, expectedSectionIds);

    public bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds) =>
        mutationProtocol.TryValidateUnchangedSections(sectionIds);

    public bool TryRefreshCommittedMutationBaselines(
        IReadOnlyCollection<string> sectionIds) =>
        mutationProtocol.TryRefreshCommittedMutationBaselines(sectionIds);

    public bool NotifyCommittedMutations(IReadOnlyCollection<string> sectionIds) =>
        executionState.TryNotifyCoreSelectedTravelMutation(graph, sectionIds);

    public void FaultClosed() => mutationProtocol.FaultClosed();
}

/// <summary>
/// No-Unity projection/effect ports for the admitted Core travel stage. Core
/// itself records arrival events and owns travel mutations; this adapter only
/// acknowledges the no-projection transition receipt and leaves presentation
/// to the executable host.
/// </summary>
internal sealed class DailyV1CoreSelectedTravelDayHostPorts
    : IDailyV1SelectedTravelDayHostPorts
{
    private sealed class TransitionReceipt
    {
        internal TransitionReceipt(
            DailyV1ExecutionBoundaryState executionState,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners npc,
            DailyV1SelectedTravelTransitionPlan plan,
            bool arriving)
        {
            ExecutionState = executionState;
            Graph = graph;
            Npc = npc;
            Plan = plan;
            Arriving = arriving;
        }

        internal DailyV1ExecutionBoundaryState ExecutionState { get; }
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal DailyV1NpcRuntimeOwners Npc { get; }
        internal DailyV1SelectedTravelTransitionPlan Plan { get; }
        internal bool Arriving { get; }
        internal bool Published { get; set; }
    }

    private sealed class CoreBoundary : IDailyV1SelectedTravelDayBoundary
    {
        private readonly DailyV1ExecutionBoundaryState executionState;
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;

        internal CoreBoundary(
            DailyV1ExecutionBoundaryState executionState,
            DailyV1SelectedRuntimeOwnerGraph graph)
        {
            this.executionState = executionState;
            this.graph = graph;
        }

        public bool TryPrepareTransition(
            DailyV1NpcRuntimeOwners npc,
            DailyV1SelectedTravelTransitionPlan plan,
            bool arriving,
            out object projectionReceipt)
        {
            projectionReceipt = null;
            if (!IsBoundToCurrentCoreSession()
                || npc == null || plan == null
                || !graph.ContainsExactNpcOwner(npc)
                || !ReferenceEquals(npc.Definitions, graph.Definitions)
                || !plan.IsPreparedFor(npc, graph))
                return false;

            projectionReceipt = new TransitionReceipt(
                executionState, graph, npc, plan, arriving);
            return true;
        }

        public void PublishCommittedTransition(
            DailyV1NpcRuntimeOwners npc,
            DailyV1SelectedTravelTransitionPlan plan,
            bool arriving,
            object projectionReceipt)
        {
            if (!(projectionReceipt is TransitionReceipt receipt)
                || receipt.Published
                || !ReferenceEquals(receipt.ExecutionState, executionState)
                || !ReferenceEquals(receipt.Graph, graph)
                || !ReferenceEquals(receipt.Npc, npc)
                || !ReferenceEquals(receipt.Plan, plan)
                || receipt.Arriving != arriving
                || !IsBoundToCurrentCoreSession()
                || !plan.IsPreparedFor(npc, graph))
                throw new InvalidOperationException(
                    "The Core travel transition has no exact unused no-projection receipt.");

            receipt.Published = true;
        }

        public bool PublishTravelPartyArrival(TravelPartyRuntime party) => false;

        public void PublishSoloTravelResult(
            DailyV1SelectedSoloTravelDayResult result,
            bool arrivalEventHandledByCore)
        {
            if (!IsBoundToCurrentCoreSession()
                || result == null
                || (result.Arrived && !arrivalEventHandledByCore))
                throw new InvalidOperationException(
                    "A detached Core travel result is invalid or lacks its Core arrival-event receipt.");
        }

        private bool IsBoundToCurrentCoreSession() =>
            executionState.HasAdmittedSelectedDailyExecution
            && executionState.SelectedRuntimeExecutionRootsBound
            && executionState.AdmissionContext?.IsOwnedByCurrentThread() == true
            && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
            && graph.Definitions != null;
    }

    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;
    private readonly IDailyV1SelectedP12TravelPartyAdvanceProtocol p12Protocol;
    private readonly IDailyV1SelectedTravelDayBoundary boundary;

    private DailyV1CoreSelectedTravelDayHostPorts(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedP12TravelPartyAdvanceProtocol p12Protocol)
    {
        this.executionState = executionState;
        this.graph = graph;
        this.p12Protocol = p12Protocol;
        boundary = new CoreBoundary(executionState, graph);
    }

    internal static bool TryCreate(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1CoreSelectedTravelDayHostPorts hostPorts)
    {
        hostPorts = null;
        if (!DailyV1CoreSelectedP12TravelPartyAdvanceProtocol.TryCreate(
                executionState, graph,
                out DailyV1CoreSelectedP12TravelPartyAdvanceProtocol protocol))
            return false;

        hostPorts = new DailyV1CoreSelectedTravelDayHostPorts(
            executionState, graph, protocol);
        return true;
    }

    public IDailyV1SelectedTravelDayBoundary Boundary => boundary;
    public IDailyV1SelectedP12TravelPartyAdvanceProtocol P12Protocol => p12Protocol;

    public bool ExecuteWithinPartyOwnerOperation(Func<bool> operation) => false;

    public void PublishCompletedTravelResults(
        IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
        IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults)
    {
        if (!IsBoundToCurrentCoreSession() || partyArrivals == null || soloResults == null)
            throw new InvalidOperationException(
                "The detached Core travel result set is not bound to its active session.");
    }

    public void ObserveCompletedTravelResults()
    {
        if (!IsBoundToCurrentCoreSession())
            throw new InvalidOperationException(
                "The detached Core travel observation is not bound to its active session.");
    }

    private bool IsBoundToCurrentCoreSession() =>
        executionState.HasAdmittedSelectedDailyExecution
        && executionState.SelectedRuntimeExecutionRootsBound
        && executionState.AdmissionContext?.IsOwnedByCurrentThread() == true
        && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
        && graph.Definitions != null;
}

/// <summary>
/// Host-only projection, P12 admission, and notification ports for Core's
/// selected travel execution context. Core derives stage availability from
/// its sealed runtime operation inventory and binds these ports to the exact
/// admitted runtime graph.
/// </summary>
internal interface IDailyV1SelectedTravelDayHostPorts
{
    IDailyV1SelectedTravelDayBoundary Boundary { get; }
    IDailyV1SelectedP12TravelPartyAdvanceProtocol P12Protocol { get; }
    bool ExecuteWithinPartyOwnerOperation(Func<bool> operation);
    void PublishCompletedTravelResults(
        IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
        IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults);
    void ObserveCompletedTravelResults();
}

/// <summary>
/// Core-owned travel execution context bound to one exact active session and
/// owner graph. Host ports can project committed results, but cannot replace
/// the graph, travel operation protocol, or Core-selected travel ordering.
/// </summary>
internal sealed class DailyV1CoreSelectedTravelDayExecutionContext
    : IDailyV1SelectedTravelDayExecutionContext
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph ownerGraph;
    private readonly IDailyV1SelectedTravelDayHostPorts hostPorts;
    private readonly bool advanceSoloTravel;

    private DailyV1CoreSelectedTravelDayExecutionContext(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        IDailyV1SelectedTravelDayHostPorts hostPorts,
        bool advanceSoloTravel)
    {
        this.executionState = executionState;
        this.ownerGraph = ownerGraph;
        this.hostPorts = hostPorts;
        this.advanceSoloTravel = advanceSoloTravel;
    }

    public IDailyV1SelectedTravelDayBoundary Boundary => hostPorts.Boundary;
    public IDailyV1SelectedP12TravelPartyAdvanceProtocol P12Protocol => hostPorts.P12Protocol;
    public bool AdvanceSoloTravel => advanceSoloTravel;

    public bool ExecuteWithinPartyOwnerOperation(Func<bool> operation)
    {
        if (!IsStillBoundToSession()) return false;
        return hostPorts.ExecuteWithinPartyOwnerOperation(operation);
    }

    public void PublishCompletedTravelResults(
        IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
        IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults)
    {
        if (!IsStillBoundToSession())
            throw new InvalidOperationException(
                "The selected travel result cannot be projected from a different Core session graph.");
        hostPorts.PublishCompletedTravelResults(partyArrivals, soloResults);
    }

    public void ObserveCompletedTravelResults()
    {
        if (!IsStillBoundToSession())
            throw new InvalidOperationException(
                "The selected travel results cannot be observed from a different Core session graph.");
        hostPorts.ObserveCompletedTravelResults();
    }

    internal static bool TryCreate(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        IDailyV1SelectedTravelDayHostPorts hostPorts,
        out IDailyV1SelectedTravelDayExecutionContext context)
    {
        context = null;
        if (executionState == null || ownerGraph == null || hostPorts == null
            || !executionState.HasAdmittedSelectedDailyExecution
            || !executionState.SelectedRuntimeExecutionRootsBound
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, ownerGraph)
            || !executionState.HasRuntimeProtocolOperationInventory
            || hostPorts.Boundary == null
            || hostPorts.P12Protocol == null)
            return false;

        bool advanceSoloTravel = executionState.HasRuntimeProtocolOperationContract(
            DailyV1RuntimeProtocolOperationContracts.SoloTravelStart);
        context = new DailyV1CoreSelectedTravelDayExecutionContext(
            executionState, ownerGraph, hostPorts, advanceSoloTravel);
        return true;
    }

    private bool IsStillBoundToSession() =>
        executionState.HasAdmittedSelectedDailyExecution
        && executionState.SelectedRuntimeExecutionRootsBound
        && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, ownerGraph)
        && executionState.HasRuntimeProtocolOperationInventory
        && hostPorts.Boundary != null
        && hostPorts.P12Protocol != null;
}

/// <summary>
/// Executes the selected Daily-v1 post-actor travel group in its established
/// order: P8 TravelParty owner, then ungrouped solo travel. Core selects and
/// commits all travel transitions against the exact admitted graph. The narrow
/// boundary above is limited to projection/P12 reconciliation and existing
/// event/log publication.
/// </summary>
internal static class DailyV1SelectedTravelDayStage
{
    internal static bool TryAdvance(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedTravelDayBoundary boundary,
        DailyV1ExecutionBoundaryState executionState,
        IDailyV1SelectedP12TravelPartyAdvanceProtocol p12Protocol,
        Func<Func<bool>, bool> legacyPartyOperation,
        bool advanceSoloTravel,
        out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
        out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults)
    {
        partyArrivals = Array.Empty<DailyV1SelectedTravelPartyArrivalResult>();
        soloResults = Array.Empty<DailyV1SelectedSoloTravelDayResult>();
        if (graph == null || boundary == null)
            return false;

        // The graph is the sole authority for both the party store and the
        // members managed by active parties. Snapshot before party advancement
        // to preserve the established party-before-solo selection semantics.
        TravelPartyStore partyStore = graph.TravelParties;
        HashSet<string> managedTravelPartyNpcRuntimeIds =
            SnapshotManagedTravelPartyNpcRuntimeIds(partyStore);

        Dictionary<DailyV1SelectedTravelTransitionPlan, object> projectionReceipts =
            new Dictionary<DailyV1SelectedTravelTransitionPlan, object>();
        Dictionary<DailyV1SelectedTravelTransitionPlan, IReadOnlyList<string>> mutationSectionIdsByPlan =
            new Dictionary<DailyV1SelectedTravelTransitionPlan, IReadOnlyList<string>>();

        bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds)
        {
            bool coreAdmissionHandled = false;
            if (executionState != null
                && !executionState.TryCanCommitSelectedP12TravelPartyMutation(
                    sectionIds, out coreAdmissionHandled))
                return false;
            if (coreAdmissionHandled) return true;
            if (executionState?.HasAdmittedSelectedDailyExecution == true && p12Protocol == null)
                return false;
            return p12Protocol == null || p12Protocol.TryValidateUnchangedSections(sectionIds);
        }

        bool TryPrepareSpatialDiscovery(
            DailyV1NpcRuntimeOwners npc,
            DailyV1SelectedTravelTransitionPlan plan,
            bool arriving)
        {
            if (!arriving || npc?.SpatialKnowledge == null || plan == null
                || !plan.TravelRevisionChanges)
                return true;
            string destinationRuntimeId = npc.Travel?.DestinationLocationRuntimeId;
            if (string.IsNullOrWhiteSpace(destinationRuntimeId)
                || npc.SpatialKnowledge.KnowsLocation(destinationRuntimeId))
                return true;
            return TryValidateUnchangedSections(new[]
            {
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix
                    + npc.RuntimeId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix
                    + npc.RuntimeId
            });
        }

        void PublishSpatialDiscovery(DailyV1NpcRuntimeOwners npc, string locationRuntimeId)
        {
            if (executionState?.HasAdmittedSelectedDailyExecution != true)
                return;
            IReadOnlyList<string> sectionIds = new[]
            {
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix
                    + npc.RuntimeId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix
                    + npc.RuntimeId
            };
            if (!npc.SpatialKnowledge.KnowsLocation(locationRuntimeId)
                || !executionState.TryRecordSelectedP12TravelPartyMutation(
                    sectionIds, out bool coreMutationHandled))
                throw new InvalidOperationException(
                    "The Core session rejected an arrived travel SpatialKnowledge mutation.");
            if (coreMutationHandled) return;
            if (p12Protocol == null
                || !p12Protocol.NotifyCommittedMutations(sectionIds))
                throw new InvalidOperationException(
                    "P12 rejected an arrived travel SpatialKnowledge mutation.");
        }

        bool TryPreparePartyCompletion(TravelPartyRuntime party) =>
            party != null && TryValidateUnchangedSections(
                new[] { TravelPartyCensusProvider.SectionId });

        void PublishPartyCompletion(TravelPartyRuntime party)
        {
            if (executionState?.HasAdmittedSelectedDailyExecution != true) return;
            if (party == null
                || !executionState.TryRecordSelectedP12TravelPartyMutation(
                    new[] { TravelPartyCensusProvider.SectionId }, out bool coreMutationHandled))
                throw new InvalidOperationException(
                    "The Core session rejected a completed TravelParty owner mutation.");
            if (coreMutationHandled) return;
            if (p12Protocol == null
                || !p12Protocol.NotifyCommittedMutations(
                    new[] { TravelPartyCensusProvider.SectionId }))
                throw new InvalidOperationException(
                    "P12 rejected a completed TravelParty owner mutation.");
        }

        bool TryPrepareTransition(
            DailyV1NpcRuntimeOwners npc,
            DailyV1SelectedTravelTransitionPlan plan,
            bool arriving)
        {
            IReadOnlyList<string> sectionIds = CreateTravelMutationSectionIds(graph, npc, plan);
            if (sectionIds == null) return false;

            bool coreAdmissionHandled = false;
            if (executionState != null
                && !executionState.TryCanCommitSelectedP12TravelPartyMutation(
                    sectionIds, out coreAdmissionHandled))
                return false;
            if (sectionIds.Count != 0 && !coreAdmissionHandled)
            {
                if (executionState?.HasAdmittedSelectedDailyExecution == true && p12Protocol == null)
                    return false;
                if (p12Protocol != null && !p12Protocol.TryValidateUnchangedSections(sectionIds))
                    return false;
            }
            if (!TryPrepareSpatialDiscovery(npc, plan, arriving)) return false;

            if (!boundary.TryPrepareTransition(npc, plan, arriving, out object receipt))
                return false;

            projectionReceipts.Add(plan, receipt);
            mutationSectionIdsByPlan.Add(plan, sectionIds);
            return true;
        }

        void PublishCommittedTransition(
            DailyV1NpcRuntimeOwners npc,
            DailyV1SelectedTravelTransitionPlan plan,
            bool arriving)
        {
            if (!projectionReceipts.TryGetValue(plan, out object receipt))
                throw new InvalidOperationException(
                    "A committed selected travel transition has no prepared projection receipt.");
            if (!mutationSectionIdsByPlan.TryGetValue(plan, out IReadOnlyList<string> sectionIds))
                throw new InvalidOperationException(
                    "A committed selected travel transition has no prepared P12 section receipt.");

            projectionReceipts.Remove(plan);
            mutationSectionIdsByPlan.Remove(plan);
            boundary.PublishCommittedTransition(npc, plan, arriving, receipt);

            if (sectionIds.Count == 0) return;
            bool coreMutationHandled = false;
            if (executionState != null
                && !executionState.TryRecordSelectedP12TravelPartyMutation(
                    sectionIds, out coreMutationHandled))
                throw new InvalidOperationException(
                    "The Core session rejected a committed selected travel owner mutation.");
            if (coreMutationHandled) return;
            if (executionState?.HasAdmittedSelectedDailyExecution == true && p12Protocol == null)
                throw new InvalidOperationException(
                    "The admitted Core travel transition has no P12 mutation protocol.");
            if (p12Protocol != null && !p12Protocol.NotifyCommittedMutations(sectionIds))
                throw new InvalidOperationException(
                    "P12 rejected a committed selected travel owner mutation. Core: "
                    + (executionState?.LastCoreSelectedTravelMutationRejection ?? "unspecified"));
        }

        bool PublishTravelPartyArrival(TravelPartyRuntime party)
        {
            if (executionState?.HasAdmittedSelectedDailyExecution != true)
                return boundary.PublishTravelPartyArrival(party);

            // Core publishes the actual arrival after the nested TravelParty
            // owner operation closes. EventId and record-sequence allocations
            // have their own selected P12 writer boundaries and are admitted
            // by the enclosing DailyAdvance turn, not the nested party scope.
            return party != null;
        }

        void PublishSoloTravelResult(DailyV1SelectedSoloTravelDayResult result)
        {
            bool arrivalEventHandledByCore =
                executionState?.HasAdmittedSelectedDailyExecution == true;
            if (arrivalEventHandledByCore
                && result?.Arrived == true
                && !executionState.TryRecordSelectedSoloTravelArrival(result, out _))
                throw new InvalidOperationException(
                    "The Core session rejected a committed selected solo-travel arrival event.");

            boundary.PublishSoloTravelResult(result, arrivalEventHandledByCore);
        }

        if (partyStore != null)
        {
            IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> completed = null;
            Func<bool> advanceParties = () => graph.TryAdvanceTravelParties(
                    partyStore,
                    TryPrepareTransition,
                    PublishCommittedTransition,
                    PublishTravelPartyArrival,
                    TryPreparePartyCompletion,
                    PublishPartyCompletion,
                    PublishSpatialDiscovery,
                    out completed);
            bool executed = executionState != null
                ? executionState.ExecuteSelectedP12TravelPartyAdvance(
                    graph, p12Protocol, advanceParties)
                : legacyPartyOperation != null
                    ? legacyPartyOperation(advanceParties)
                    : advanceParties();
            if (!executed || completed == null)
                return false;
            if (executionState?.HasAdmittedSelectedDailyExecution == true)
            {
                List<DailyV1SelectedTravelPartyArrivalResult> recordedArrivals =
                    new List<DailyV1SelectedTravelPartyArrivalResult>(completed.Count);
                foreach (DailyV1SelectedTravelPartyArrivalResult arrival in completed)
                {
                    bool eventRecorded = false;
                    bool eventHandled = arrival != null
                        && executionState.TryRecordSelectedTravelPartyArrival(
                            arrival.Party, out eventRecorded);
                    recordedArrivals.Add(new DailyV1SelectedTravelPartyArrivalResult(
                        arrival.Party, arrival.Members, eventHandled && eventRecorded));
                }
                partyArrivals = recordedArrivals.AsReadOnly();
            }
            else
            {
                partyArrivals = completed;
            }
        }

        if (!advanceSoloTravel)
            return true;

        projectionReceipts.Clear();
        mutationSectionIdsByPlan.Clear();

        return graph.TryAdvanceSoloTravelDay(
            graph.Npcs,
            managedTravelPartyNpcRuntimeIds,
            TryPrepareTransition,
            PublishCommittedTransition,
            PublishSoloTravelResult,
            PublishSpatialDiscovery,
            out soloResults);
    }

    private static IReadOnlyList<string> CreateTravelMutationSectionIds(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        DailyV1SelectedTravelTransitionPlan plan)
    {
        if (graph == null || npc == null || plan == null
            || !ReferenceEquals(plan.Npc, npc)
            || !graph.ContainsExactNpcOwner(npc)
            || !plan.IsPreparedFor(npc, graph))
            return null;

        List<string> sectionIds = new List<string>();
        if (plan.TravelRevisionChanges)
            sectionIds.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, npc.RuntimeId));
        foreach (DailyV1CityRuntimeOwners city in plan.ChangedCityOwners)
        {
            if (city == null || city.NpcPresence == null || !graph.ContainsExactCityOwner(city))
                return null;
            sectionIds.Add(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId));
        }

        if (new HashSet<string>(sectionIds, StringComparer.Ordinal).Count != sectionIds.Count)
            return null;
        return Array.AsReadOnly(sectionIds.ToArray());
    }

    private static HashSet<string> SnapshotManagedTravelPartyNpcRuntimeIds(
        TravelPartyStore partyStore)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        if (partyStore == null) return ids;
        using (partyStore.EnterReadWindow())
        {
            foreach (TravelPartyRuntime party in partyStore.ActiveParties)
            {
                if (party == null || !party.IsActive || party.MemberRuntimeIds == null) continue;
                foreach (string memberRuntimeId in party.MemberRuntimeIds)
                    if (!string.IsNullOrWhiteSpace(memberRuntimeId))
                        ids.Add(memberRuntimeId);
            }
        }
        return ids;
    }
}
