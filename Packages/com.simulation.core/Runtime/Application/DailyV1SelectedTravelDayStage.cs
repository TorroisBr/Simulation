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
        TravelPartyStore partyStore,
        IReadOnlyCollection<string> managedTravelPartyNpcRuntimeIds,
        IDailyV1SelectedTravelDayBoundary boundary,
        Func<Func<bool>, bool> runPartyOwnerOperation,
        bool advanceSoloTravel,
        out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
        out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults)
    {
        partyArrivals = Array.Empty<DailyV1SelectedTravelPartyArrivalResult>();
        soloResults = Array.Empty<DailyV1SelectedSoloTravelDayResult>();
        if (graph == null || boundary == null || managedTravelPartyNpcRuntimeIds == null)
            return false;

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
            if (runPartyOwnerOperation == null)
                return false;

            IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> completed = null;
            bool executed = runPartyOwnerOperation(() => graph.TryAdvanceTravelParties(
                partyStore,
                TryPrepareTransition,
                PublishCommittedTransition,
                boundary.PublishTravelPartyArrival,
                out completed));
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
}
