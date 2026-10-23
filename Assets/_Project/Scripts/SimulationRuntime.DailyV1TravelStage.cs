using System;
using System.Collections.Generic;

public sealed partial class SimulationRuntime
{
    private bool TryCreateSelectedTravelDayExecutionContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedTravelDayExecutionContext context)
    {
        if (graph == null || !ReferenceEquals(graph, SelectedRuntimeOwnerGraph))
        {
            context = null;
            return false;
        }
        if (runtimeAdmissionContext != null
            && (!ReferenceEquals(graph.TravelParties, travelPartySystem?.Store)
                || (travelSystem != null
                    && !ReferenceEquals(graph.TravelParties,
                        travelSystem.SelectedTravelPartyStore))))
        {
            context = null;
            return false;
        }
        return TryCreateSelectedTravelDayExecutionContext(out context);
    }

    private bool TryCreateSelectedTravelDayExecutionContext(
        out IDailyV1SelectedTravelDayExecutionContext context)
    {
        context = null;
        DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
        if (graph == null) return false;
        travelPartySystem?.EnsureSelectedDailyTravelAdvanceAvailable();
        travelSystem?.EnsureSelectedDailyTravelAdvanceAvailable();

        if (!SelectedTravelDayBoundary.TryCreate(this, graph, npcRuntimes,
                out SelectedTravelDayBoundary boundary))
            return false;

        context = new SelectedDailyV1TravelDayExecutionContext(
            this, boundary,
            advanceSoloTravel: travelSystem != null);
        return true;
    }

    private bool TryAdvanceSelectedTravelDayStage(out IReadOnlyList<NpcRuntime> arrivedNpcs)
    {
        arrivedNpcs = null;
        if (!TryCreateSelectedTravelDayExecutionContext(
                out IDailyV1SelectedTravelDayExecutionContext context)
            || !(context is SelectedDailyV1TravelDayExecutionContext selectedContext)
            || !DailyV1SelectedDailyExecutionStage.TryAdvanceTravelDay(
                SelectedRuntimeOwnerGraph, selectedContext,
                out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
                out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults))
            return false;

        selectedContext.PublishCompletedTravelResults(partyArrivals, soloResults);
        arrivedNpcs = selectedContext.Arrivals;
        return true;
    }

    private sealed class SelectedDailyV1TravelDayExecutionContext
        : IDailyV1SelectedTravelDayExecutionContext
    {
        private readonly SimulationRuntime runtime;
        private readonly SelectedTravelDayBoundary boundary;

        internal SelectedDailyV1TravelDayExecutionContext(
            SimulationRuntime runtime,
            SelectedTravelDayBoundary boundary,
            bool advanceSoloTravel)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
            AdvanceSoloTravel = advanceSoloTravel;
        }

        public IDailyV1SelectedTravelDayBoundary Boundary => boundary;
        internal IReadOnlyList<NpcRuntime> Arrivals => boundary.Arrivals;
        public bool AdvanceSoloTravel { get; }

        public bool ExecuteWithinPartyOwnerOperation(Func<bool> operation)
        {
            if (operation == null) return false;
            if (runtime.runtimeAdmissionContext == null) return operation();
            if (!runtime.TryBeginP12TravelPartyAdvanceOperation(
                    out P12TravelPartyAdvanceOperationScope operationScope))
                throw new InvalidOperationException(
                    "The P12 TravelParty advance owner operation could not be admitted.");
            using (operationScope) return operation();
        }

        public void PublishCompletedTravelResults(
            IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
            IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults)
        {
            foreach (DailyV1SelectedTravelPartyArrivalResult partyArrival in partyArrivals)
            {
                foreach (DailyV1NpcRuntimeOwners owner in partyArrival.Members)
                    boundary.AddPartyArrival(owner);
                if (!partyArrival.EventRecorded)
                    runtime.travelPartySystem?.WarnSelectedTravelPartyArrivalEventFailure(partyArrival.Party);
            }

            // Core publishes solo event/log outputs at their original per-result
            // callback positions, but the shared arrival list consumed by later
            // stages has always been party-first, then solo RuntimeId order.
            foreach (DailyV1SelectedSoloTravelDayResult soloResult in soloResults)
            {
                if (soloResult?.ShouldReport == true && soloResult.Arrived)
                    boundary.AddSoloArrival(soloResult.Npc);
            }
        }

        public void ObserveCompletedTravelResults() =>
            runtime.ObserveTravelArrivals(boundary.Arrivals, selectedMarketAlreadyObservedByCore: true);
    }

    private void ObserveTravelArrivals(IReadOnlyList<NpcRuntime> arrivedNpcs)
    {
        ObserveTravelArrivals(arrivedNpcs, selectedMarketAlreadyObservedByCore: false);
    }

    private void ObserveTravelArrivals(
        IReadOnlyList<NpcRuntime> arrivedNpcs,
        bool selectedMarketAlreadyObservedByCore)
    {
        foreach (NpcRuntime arrivedNpc in arrivedNpcs ?? Array.Empty<NpcRuntime>())
        {
            ObserveArrivedExplorableSites(arrivedNpc);
            if (!selectedMarketAlreadyObservedByCore
                && arrivedNpc?.CurrentCity != null && configuration.MerchantTrade.Enabled)
                merchantSystem?.ObserveCurrentMarket(arrivedNpc);
        }

        expeditionSystem?.ReconcileAfterTravel(arrivedNpcs ?? Array.Empty<NpcRuntime>());
    }

    private sealed class SelectedTravelDayBoundary : IDailyV1SelectedTravelDayBoundary
    {
        private readonly SimulationRuntime runtime;
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        private readonly Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> adapters;
        private readonly Dictionary<DailyV1NpcRuntimeOwners, CityRuntime[]> changedCitiesByOwner =
            new Dictionary<DailyV1NpcRuntimeOwners, CityRuntime[]>();
        private readonly List<NpcRuntime> arrivals = new List<NpcRuntime>();

        internal IReadOnlyList<NpcRuntime> Arrivals => arrivals.AsReadOnly();

        private SelectedTravelDayBoundary(
            SimulationRuntime runtime,
            DailyV1SelectedRuntimeOwnerGraph graph,
            Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> adapters)
        {
            this.runtime = runtime;
            this.graph = graph;
            this.adapters = adapters;
        }

        internal static bool TryCreate(
            SimulationRuntime runtime,
            DailyV1SelectedRuntimeOwnerGraph graph,
            IReadOnlyList<NpcRuntime> npcRuntimes,
            out SelectedTravelDayBoundary boundary)
        {
            boundary = null;
            if (runtime == null || graph == null || npcRuntimes == null) return false;

            Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> adapters =
                new Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime>();
            foreach (NpcRuntime npcRuntime in npcRuntimes)
            {
                if (npcRuntime == null || !npcRuntime.UsesSelectedDailyV1Definitions)
                    continue;

                DailyV1NpcRuntimeOwners owner = npcRuntime?.SelectedRuntimeOwners;
                if (owner == null
                    || !ReferenceEquals(owner.Definitions, graph.Definitions)
                    || !adapters.TryAdd(owner, npcRuntime))
                    return false;
            }
            if (adapters.Count != graph.Npcs.Count) return false;
            foreach (DailyV1NpcRuntimeOwners owner in graph.Npcs)
                if (!adapters.ContainsKey(owner)) return false;

            boundary = new SelectedTravelDayBoundary(runtime, graph, adapters);
            return true;
        }

        public bool TryPrepareTransition(
            DailyV1NpcRuntimeOwners npc,
            DailyV1SelectedTravelTransitionPlan plan,
            bool arriving,
            out object projectionReceipt)
        {
            projectionReceipt = null;
            if (runtime.travelSystem == null
                || npc == null
                || plan == null
                || !adapters.TryGetValue(npc, out NpcRuntime adapter)
                || !runtime.travelSystem.TryGetSelectedTravelProjectionCandidates(
                    graph, plan, out CityRuntime[] candidates)
                || !adapter.CanAdmitPreparedSelectedTravelDay(
                    plan, candidates, out CityRuntime[] changedCities))
                return false;

            changedCitiesByOwner[npc] = changedCities;
            projectionReceipt = changedCities;
            return true;
        }

        public void PublishCommittedTransition(
            DailyV1NpcRuntimeOwners npc,
            DailyV1SelectedTravelTransitionPlan plan,
            bool arriving,
            object projectionReceipt)
        {
            if (npc == null
                || !adapters.TryGetValue(npc, out NpcRuntime adapter)
                || !(projectionReceipt is CityRuntime[] changedCities)
                || !changedCitiesByOwner.TryGetValue(npc, out CityRuntime[] expected)
                || !ReferenceEquals(changedCities, expected)
                || !adapter.TryProjectCommittedSelectedTravelDay(
                    plan, arriving, changedCities, out _))
                throw new InvalidOperationException(
                    "A committed selected travel transition could not publish its Unity projection.");

            // Projection precedes the P12 mutation notification so the owner
            // census can observe the reciprocal Core/Unity presence join.
            adapter.RecordSelectedTravelOwnerMutationCommitted(plan, changedCities);
        }

        public bool PublishTravelPartyArrival(TravelPartyRuntime party)
        {
            return runtime.travelPartySystem == null
                || runtime.travelPartySystem.PublishSelectedTravelPartyArrivalEvent(party);
        }

        public void PublishSoloTravelResult(DailyV1SelectedSoloTravelDayResult result)
        {
            if (result == null || !adapters.TryGetValue(result.Npc, out NpcRuntime adapter))
                throw new InvalidOperationException(
                    "A selected Core travel result did not resolve to its exact Unity adapter.");
            runtime.travelSystem?.PublishSelectedSoloTravelResult(adapter, result);
        }

        internal void AddPartyArrival(DailyV1NpcRuntimeOwners owner)
        {
            if (owner == null || !adapters.TryGetValue(owner, out NpcRuntime adapter))
                throw new InvalidOperationException(
                    "A completed Core TravelParty member did not resolve to its exact Unity adapter.");
            arrivals.Add(adapter);
        }

        internal void AddSoloArrival(DailyV1NpcRuntimeOwners owner)
        {
            if (owner == null || !adapters.TryGetValue(owner, out NpcRuntime adapter))
                throw new InvalidOperationException(
                    "A completed Core solo-travel arrival did not resolve to its exact Unity adapter.");
            arrivals.Add(adapter);
        }
    }
}
