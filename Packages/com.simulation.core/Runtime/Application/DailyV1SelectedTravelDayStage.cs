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

    void PublishSoloTravelResult(DailyV1SelectedSoloTravelDayResult result);
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
    bool TryEnterOperation(out IDisposable operationScope);
    bool NotifyCommittedMutations(IReadOnlyCollection<string> sectionIds);
    void FaultClosed();
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

        bool TryPrepareTransition(
            DailyV1NpcRuntimeOwners npc,
            DailyV1SelectedTravelTransitionPlan plan,
            bool arriving)
        {
            if (!boundary.TryPrepareTransition(npc, plan, arriving, out object receipt))
                return false;

            projectionReceipts.Add(plan, receipt);
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

            projectionReceipts.Remove(plan);
            boundary.PublishCommittedTransition(npc, plan, arriving, receipt);
        }

        if (partyStore != null)
        {
            IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> completed = null;
            Func<bool> advanceParties = () => graph.TryAdvanceTravelParties(
                    partyStore,
                    TryPrepareTransition,
                    PublishCommittedTransition,
                    boundary.PublishTravelPartyArrival,
                    out completed);
            bool executed = executionState != null
                ? executionState.ExecuteSelectedP12TravelPartyAdvance(
                    graph, p12Protocol, advanceParties)
                : legacyPartyOperation != null
                    ? legacyPartyOperation(advanceParties)
                    : advanceParties();
            if (!executed || completed == null)
                return false;
            partyArrivals = completed;
        }

        if (!advanceSoloTravel)
            return true;

        projectionReceipts.Clear();

        return graph.TryAdvanceSoloTravelDay(
            graph.Npcs,
            managedTravelPartyNpcRuntimeIds,
            TryPrepareTransition,
            PublishCommittedTransition,
            boundary.PublishSoloTravelResult,
            out soloResults);
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
