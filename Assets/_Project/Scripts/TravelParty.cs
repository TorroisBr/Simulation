using System;
using System.Collections.Generic;

public sealed class TravelPartySystem : IAuthoritativeMutationGuardBindable
{
    private static readonly ActionParticipationRequirements DefaultRequirements = new ActionParticipationRequirements(
        minPerformers: 1,
        maxPerformers: ActionParticipationRequirements.Unlimited,
        minSupports: 0,
        maxSupports: ActionParticipationRequirements.Unlimited,
        minTargets: 0,
        maxTargets: 0);

    private readonly TravelPartyStore partyStore;
    private readonly RuntimeIdAllocator idAllocator;
    private readonly RuntimeIdentityRegistry identityRegistry;
    private readonly TravelSystem travelSystem;
    private readonly SimulationTime simulationTime;
    private readonly SimulationRecordSequence recordSequence;
    private readonly DomainEventRecorder domainEventRecorder;
    private readonly SimulationLogger logger;
    private readonly EconomyTransactionService transactionService;
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    [NonSerialized] private Func<TravelPartyStartPreparation, bool> p12StartOperationAdmission;

    public TravelPartyStore Store => partyStore;
    internal RuntimeIdAllocator IdAllocator => idAllocator;
    internal bool HasDomainEventRecorder => domainEventRecorder != null;
    internal DomainEventRecorder SelectedDomainEventRecorder => domainEventRecorder;

    internal bool UsesP12IdentityOwners(RuntimeIdAllocator allocator, SimulationRecordSequence sequence)
    {
        return ReferenceEquals(idAllocator, allocator)
            && ReferenceEquals(recordSequence, sequence)
            && (domainEventRecorder == null
                || domainEventRecorder.UsesIdentityOwners(allocator, simulationTime, sequence));
    }

    internal bool BindP12StartOperationAdmission(Func<TravelPartyStartPreparation, bool> admission)
    {
        if (admission == null) return false;
        if (p12StartOperationAdmission != null)
            return p12StartOperationAdmission.Equals(admission);
        p12StartOperationAdmission = admission;
        return true;
    }

    public TravelPartySystem(
        TravelPartyStore partyStore,
        RuntimeIdAllocator idAllocator,
        RuntimeIdentityRegistry identityRegistry,
        TravelSystem travelSystem,
        SimulationTime simulationTime,
        SimulationRecordSequence recordSequence,
        DomainEventRecorder domainEventRecorder,
        SimulationLogger logger = null,
        EconomyTransactionService transactionService = null)
    {
        this.partyStore = partyStore ?? throw new ArgumentNullException(nameof(partyStore));
        this.idAllocator = idAllocator ?? throw new ArgumentNullException(nameof(idAllocator));
        this.identityRegistry = identityRegistry ?? throw new ArgumentNullException(nameof(identityRegistry));
        this.travelSystem = travelSystem ?? throw new ArgumentNullException(nameof(travelSystem));
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        this.recordSequence = recordSequence ?? throw new ArgumentNullException(nameof(recordSequence));
        this.domainEventRecorder = domainEventRecorder;
        this.logger = logger ?? UnitySimulationLoggerAdapter.Create(null);
        this.transactionService = transactionService ?? new EconomyTransactionService();
        travelSystem.AttachTravelPartyStore(partyStore);
    }

    public bool TryStartTravelParty(ActionExecutionContext context)
    {
        return TryStartTravelParty(context, out _);
    }

    public bool TryStartTravelParty(ActionExecutionContext context, out TravelPartyRuntime party)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            party = null;
            return false;
        }
        using (partyStore.EnterMutationWindow())
        {
        if (!partyStore.CanCommitMutations(2))
        {
            party = null;
            return false;
        }
        return TryStartTravelPartyInMutationWindow(context, out party);
        }
    }

    private bool TryStartTravelPartyInMutationWindow(ActionExecutionContext context, out TravelPartyRuntime party)
    {
        party = null;

        if (!mutationGuardBinding.CanMutate)
        {
            return false;
        }

        if (TryPrepareTravel(context, false, out TravelPreparation preparation, out _) == false)
        {
            return false;
        }

        if (p12StartOperationAdmission != null)
        {
            bool admitted;
            try
            {
                admitted = p12StartOperationAdmission(new TravelPartyStartPreparation(
                    preparation.OriginLocation,
                    preparation.OriginCity,
                    preparation.Route,
                    preparation.Members,
                    preparation.Costs));
            }
            catch
            {
                admitted = false;
            }
            if (!admitted) return false;
        }

        bool fullySelected = true;
        foreach (NpcRuntime member in preparation.Members)
            if (member?.SelectedRuntimeOwners == null)
            {
                fullySelected = false;
                break;
            }

        if (fullySelected)
        {
            if (!TryResolveExactSelectedCoreMembers(
                    preparation.Members,
                    out DailyV1SelectedRuntimeOwnerGraph partyGraph,
                    out List<DailyV1NpcRuntimeOwners> partyMembers)
                || !ReferenceEquals(partyGraph.TravelParties, partyStore)
                || !ReferenceEquals(partyGraph.RuntimeIdAllocator, idAllocator)
                || !TryResolveSelectedPartyCities(
                    partyGraph, preparation.OriginCity, preparation.DestinationCity,
                    out DailyV1CityRuntimeOwners selectedOriginCity,
                    out DailyV1CityRuntimeOwners selectedDestinationCity)
                || !TryResolveSelectedPartyRoles(
                    partyGraph, preparation, out List<DailyV1NpcRuntimeOwners> selectedTravelers,
                    out List<DailyV1NpcRuntimeOwners> selectedEscorts))
                return false;

            return TryStartSelectedCoreTravelParty(
                partyGraph, preparation, partyMembers,
                selectedTravelers, selectedEscorts,
                selectedOriginCity, selectedDestinationCity,
                context.OriginDecisionId, out party);
        }

        string partyId;

        try
        {
            partyId = idAllocator.AllocateTravelPartyId();
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError("Cannot allocate TravelPartyId: " + exception.Message);
            return false;
        }

        TravelPartyRuntime createdParty = new TravelPartyRuntime(
            partyId,
            preparation.OriginLocation.RuntimeId,
            preparation.DestinationLocation.RuntimeId,
            preparation.Route.RuntimeId,
            preparation.Travelers.ConvertAll(npc => npc.RuntimeId),
            preparation.Escorts.ConvertAll(npc => npc.RuntimeId),
            preparation.Route.TravelDays,
            context.OriginDecisionId,
            preparation.Costs);

        List<NpcRuntime> startedMembers = new List<NpcRuntime>();
        List<TravelPartyMemberCost> paidCosts = new List<TravelPartyMemberCost>();

        foreach (NpcRuntime member in preparation.Members)
        {
            if (member.StartTravel(
                preparation.DestinationLocation,
                preparation.DestinationCity,
                preparation.Route.TravelDays,
                context.OriginDecisionId,
                preparation.Route.RuntimeId) == false)
            {
                Rollback(
                    startedMembers,
                    paidCosts,
                    preparation.OriginLocation,
                    preparation.OriginCity);
                return false;
            }

            startedMembers.Add(member);
        }

        float travelCost = preparation.Costs[0].Amount;
        EconomyTransactionResult groupCharge;
        bool allMembersUseSelectedCoreOwners = true;
        foreach (NpcRuntime member in preparation.Members)
            if (member == null || member.SelectedRuntimeOwners == null)
            {
                allMembersUseSelectedCoreOwners = false;
                break;
            }

        if (allMembersUseSelectedCoreOwners)
        {
            if (!TryResolveExactSelectedCoreMembers(
                    preparation.Members,
                    out DailyV1SelectedRuntimeOwnerGraph selectedGraph,
                    out List<DailyV1NpcRuntimeOwners> selectedMembers))
            {
                Rollback(startedMembers, paidCosts, preparation.OriginLocation, preparation.OriginCity);
                return false;
            }

            groupCharge = DailyV1SelectedTravelPartyChargeExecution.TryCharge(
                selectedGraph, selectedMembers, travelCost);
        }
        else
        {
            // Preserve the existing Unity-authoritative compatibility path for
            // rosters that are not wholly selected into the Core owner graph.
            groupCharge = transactionService.TryChargeTravelGroup(preparation.Members, travelCost);
        }

        if (groupCharge.Success == false)
        {
            Rollback(
                startedMembers,
                paidCosts,
                preparation.OriginLocation,
                preparation.OriginCity);
            return false;
        }

        paidCosts.AddRange(preparation.Costs);

        if (partyStore.Add(createdParty) == false)
        {
            Rollback(
                startedMembers,
                paidCosts,
                preparation.OriginLocation,
                preparation.OriginCity);
            return false;
        }

        foreach (NpcRuntime member in preparation.Members)
        {
            if (!member.SetActiveTravelPartyId(createdParty.TravelPartyId))
                throw new InvalidOperationException(
                    "A preflighted TravelParty member could not commit its active party identity.");
        }

        bool eventRecorded = domainEventRecorder == null || domainEventRecorder.Record((eventId, absoluteDay, sequence) => new TravelPartyStartedEvent(
            eventId,
            absoluteDay,
            sequence,
            createdParty.TravelPartyId,
            createdParty.OriginLocationRuntimeId,
            createdParty.DestinationLocationRuntimeId,
            createdParty.RouteRuntimeId,
            createdParty.TravelDaysTotal,
            createdParty.TravelerRuntimeIds,
            createdParty.EscortRuntimeIds,
            createdParty.OriginDecisionId));

        if (eventRecorded == false)
        {
            foreach (NpcRuntime member in preparation.Members)
            {
                if (!member.SetActiveTravelPartyId(null))
                    throw new InvalidOperationException(
                        "A preflighted TravelParty member could not clear its active party identity during compensation.");
            }

            if (!partyStore.Remove(createdParty.TravelPartyId))
                throw new InvalidOperationException(
                    "A preflighted TravelParty store could not remove a failed start.");
            Rollback(
                startedMembers,
                paidCosts,
                preparation.OriginLocation,
                preparation.OriginCity);
            return false;
        }

        foreach (NpcRuntime member in preparation.Members)
        {
            member.SpatialKnowledge.DiscoverLocation(preparation.OriginLocation.RuntimeId);
            member.SpatialKnowledge.DiscoverRoute(preparation.Route.RuntimeId);
        }

        party = createdParty;
        return true;
    }

    internal bool TryCreateSelectedCoreTravelPartyStartHostOperation(
        DailyV1SelectedTravelPartyStartPlan plan,
        out IDailyV1SelectedTravelPartyStartHostOperation hostOperation)
    {
        hostOperation = null;
        DailyV1SelectedRuntimeOwnerGraph graph = plan?.Graph;
        if (plan == null || graph == null
            || !ReferenceEquals(graph.TravelParties, partyStore)
            || !ReferenceEquals(graph.RuntimeIdAllocator, idAllocator)
            || graph.SpatialNetwork == null
            || identityRegistry == null || travelSystem == null
            || !graph.SpatialNetwork.TryGetLocation(plan.OriginLocation.RuntimeId,
                out SpatialLocationRuntime coreOrigin)
            || !ReferenceEquals(coreOrigin, plan.OriginLocation)
            || !graph.SpatialNetwork.TryGetLocation(plan.DestinationLocation.RuntimeId,
                out SpatialLocationRuntime coreDestination)
            || !ReferenceEquals(coreDestination, plan.DestinationLocation)
            || !identityRegistry.TryGetLocation(plan.OriginLocation.RuntimeId,
                out SpatialLocationRuntime originLocation)
            || !identityRegistry.TryGetLocation(plan.DestinationLocation.RuntimeId,
                out SpatialLocationRuntime destinationLocation)
            || !travelSystem.TryGetDirectRoute(originLocation, destinationLocation,
                out SpatialRouteRuntime projectedRoute)
            || !string.Equals(projectedRoute.RuntimeId, plan.Route.RuntimeId,
                StringComparison.Ordinal)
            || !string.Equals(projectedRoute.Origin.RuntimeId,
                plan.OriginLocation.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(projectedRoute.Destination.RuntimeId,
                plan.DestinationLocation.RuntimeId, StringComparison.Ordinal)
            || projectedRoute.TravelDays != plan.TravelDays)
            return false;

        CityRuntime originCity = travelSystem.GetCityRuntime(originLocation);
        CityRuntime destinationCity = travelSystem.GetCityRuntime(destinationLocation);
        if (!MatchesSelectedCityProjection(plan.OriginCity, originCity, originLocation.RuntimeId)
            || !MatchesSelectedCityProjection(
                plan.DestinationCity, destinationCity, destinationLocation.RuntimeId)
            || plan.Costs.Count != plan.Members.Count
            || plan.Costs.Count == 0)
            return false;
        Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> adapters =
            new Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime>();
        Dictionary<string, NpcRuntime> adaptersById =
            new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        List<NpcRuntime> members = new List<NpcRuntime>(plan.Members.Count);
        List<NpcRuntime> travelers = new List<NpcRuntime>(plan.Travelers.Count);
        List<NpcRuntime> escorts = new List<NpcRuntime>(plan.Escorts.Count);
        Dictionary<string, NpcRuntime> memberById =
            new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners owner in plan.Members)
        {
            if (owner == null
                || !identityRegistry.TryGetNpc(owner.RuntimeId, out NpcRuntime adapter)
                || adapter == null
                || !ReferenceEquals(adapter.SelectedRuntimeOwnerGraph, graph)
                || !ReferenceEquals(adapter.SelectedRuntimeOwners, owner)
                || adapters.ContainsKey(owner)
                || adaptersById.ContainsKey(owner.RuntimeId))
                return false;
            adapters.Add(owner, adapter);
            adaptersById.Add(owner.RuntimeId, adapter);
            memberById.Add(owner.RuntimeId, adapter);
            members.Add(adapter);
        }
        foreach (DailyV1NpcRuntimeOwners owner in plan.Travelers)
        {
            if (owner == null || !adapters.TryGetValue(owner, out NpcRuntime adapter)) return false;
            travelers.Add(adapter);
        }
        foreach (DailyV1NpcRuntimeOwners owner in plan.Escorts)
        {
            if (owner == null || !adapters.TryGetValue(owner, out NpcRuntime adapter)) return false;
            escorts.Add(adapter);
        }

        TravelPreparation preparation = new TravelPreparation(
            originLocation, originCity, destinationLocation, destinationCity, projectedRoute,
            travelers, escorts, members, memberById,
            new List<TravelPartyMemberCost>(plan.Costs));
        TravelPartyStartPreparation p12Preparation = new TravelPartyStartPreparation(
            originLocation, originCity, projectedRoute, members, preparation.Costs);
        if (p12StartOperationAdmission == null)
            return false;
        try
        {
            if (!p12StartOperationAdmission(p12Preparation)) return false;
        }
        catch
        {
            return false;
        }

        CityRuntime[] projectionCandidates = originCity == null
            ? (destinationCity == null ? Array.Empty<CityRuntime>() : new[] { destinationCity })
            : (destinationCity == null ? new[] { originCity } : new[] { originCity, destinationCity });
        hostOperation = new SelectedCoreTravelPartyStartHostOperation(
            this, preparation, adapters, projectionCandidates);
        return true;
    }

    private static bool MatchesSelectedCityProjection(
        DailyV1CityRuntimeOwners owner,
        CityRuntime projection,
        string locationRuntimeId)
    {
        if (owner == null) return projection == null;
        return projection != null
            && ReferenceEquals(projection.SelectedRuntimeOwners, owner)
            && string.Equals(projection.RuntimeId, owner.RuntimeId, StringComparison.Ordinal)
            && string.Equals(owner.LocationRuntimeId, locationRuntimeId, StringComparison.Ordinal)
            && string.Equals(projection.Location?.RuntimeId,
                locationRuntimeId, StringComparison.Ordinal);
    }
    private bool TryStartSelectedCoreTravelParty(
        DailyV1SelectedRuntimeOwnerGraph graph,
        TravelPreparation preparation,
        IReadOnlyList<DailyV1NpcRuntimeOwners> selectedMembers,
        IReadOnlyList<DailyV1NpcRuntimeOwners> selectedTravelers,
        IReadOnlyList<DailyV1NpcRuntimeOwners> selectedEscorts,
        DailyV1CityRuntimeOwners selectedOriginCity,
        DailyV1CityRuntimeOwners selectedDestinationCity,
        string originDecisionId,
        out TravelPartyRuntime party)
    {
        party = null;
        CityRuntime[] projectionCandidates = preparation.OriginCity == null
            ? (preparation.DestinationCity == null
                ? Array.Empty<CityRuntime>()
                : new[] { preparation.DestinationCity })
            : (preparation.DestinationCity == null
                ? new[] { preparation.OriginCity }
                : new[] { preparation.OriginCity, preparation.DestinationCity });
        Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> adapters =
            new Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime>();
        for (int index = 0; index < selectedMembers.Count; index++)
            adapters.Add(selectedMembers[index], preparation.Members[index]);

        Dictionary<DailyV1SelectedTravelTransitionPlan, CityRuntime[]> changedCitiesByPlan =
            new Dictionary<DailyV1SelectedTravelTransitionPlan, CityRuntime[]>();
        Func<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan,
            DailyV1SelectedTravelPartyProjectionTarget, bool> prepareAndAdmit =
            (owner, plan, _) =>
            {
                if (!adapters.TryGetValue(owner, out NpcRuntime adapter)
                    || !adapter.CanPrepareSelectedTravelProjection(
                        plan, projectionCandidates, out CityRuntime[] changedCities)
                    || !adapter.CanAdmitPreparedSelectedTravelDay(plan, changedCities))
                    return false;
                changedCitiesByPlan.Add(plan, changedCities);
                return true;
            };
        Action<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan,
            DailyV1SelectedTravelPartyProjectionTarget> publishProjection =
            (owner, plan, target) =>
            {
                if (!adapters.TryGetValue(owner, out NpcRuntime adapter)
                    || !changedCitiesByPlan.TryGetValue(plan, out CityRuntime[] changedCities)
                    || !TryResolveSelectedProjectionTarget(
                        target, preparation, out SpatialLocationRuntime currentLocation,
                        out CityRuntime currentCity, out SpatialLocationRuntime destinationLocation,
                        out CityRuntime destinationCity)
                    || !adapter.TryProjectCommittedSelectedTravelTransition(
                        plan, changedCities,
                        currentLocation, currentCity, destinationLocation, destinationCity))
                    throw new InvalidOperationException(
                        "A committed Core TravelParty transition could not publish its Unity projection.");
                adapter.RecordSelectedTravelOwnerMutationCommitted(plan, changedCities);
            };

        return DailyV1SelectedTravelPartyStartExecution.TryStart(
            graph,
            selectedMembers,
            selectedTravelers,
            selectedEscorts,
            preparation.OriginLocation.RuntimeId,
            selectedOriginCity,
            preparation.DestinationLocation.RuntimeId,
            selectedDestinationCity,
            preparation.Route.RuntimeId,
            preparation.Route.TravelDays,
            originDecisionId,
            preparation.Costs,
            domainEventRecorder,
            prepareAndAdmit,
            publishProjection,
            out party);
    }

    private bool TryResolveSelectedProjectionTarget(
        DailyV1SelectedTravelPartyProjectionTarget target,
        TravelPreparation preparation,
        out SpatialLocationRuntime currentLocation,
        out CityRuntime currentCity,
        out SpatialLocationRuntime destinationLocation,
        out CityRuntime destinationCity)
    {
        currentLocation = null;
        currentCity = null;
        destinationLocation = null;
        destinationCity = null;
        return target != null
            && TryResolveLocationProjection(
                target.CurrentLocationRuntimeId, preparation, out currentLocation)
            && TryResolveCityProjection(
                target.CurrentCityRuntimeId, preparation, out currentCity)
            && TryResolveLocationProjection(
                target.DestinationLocationRuntimeId, preparation, out destinationLocation)
            && TryResolveCityProjection(
                target.DestinationCityRuntimeId, preparation, out destinationCity);
    }

    private bool TryResolveLocationProjection(
        string runtimeId,
        TravelPreparation preparation,
        out SpatialLocationRuntime location)
    {
        location = null;
        if (runtimeId == null) return true;
        if (string.Equals(runtimeId, preparation.OriginLocation.RuntimeId, StringComparison.Ordinal))
            location = preparation.OriginLocation;
        else if (string.Equals(runtimeId, preparation.DestinationLocation.RuntimeId, StringComparison.Ordinal))
            location = preparation.DestinationLocation;
        else if (!identityRegistry.TryGetLocation(runtimeId, out location))
            return false;
        return location != null && string.Equals(location.RuntimeId, runtimeId, StringComparison.Ordinal);
    }

    private bool TryResolveCityProjection(
        string runtimeId,
        TravelPreparation preparation,
        out CityRuntime city)
    {
        city = null;
        if (runtimeId == null) return true;
        if (string.Equals(runtimeId, preparation.OriginCity?.RuntimeId, StringComparison.Ordinal))
            city = preparation.OriginCity;
        else if (string.Equals(runtimeId, preparation.DestinationCity?.RuntimeId, StringComparison.Ordinal))
            city = preparation.DestinationCity;
        else if (!identityRegistry.TryGetCity(runtimeId, out city))
            return false;
        return city != null && string.Equals(city.RuntimeId, runtimeId, StringComparison.Ordinal);
    }

    private static bool TryResolveSelectedPartyCities(
        DailyV1SelectedRuntimeOwnerGraph graph,
        CityRuntime originProjection,
        CityRuntime destinationProjection,
        out DailyV1CityRuntimeOwners origin,
        out DailyV1CityRuntimeOwners destination)
    {
        origin = null;
        destination = null;
        if (originProjection != null)
        {
            origin = originProjection.SelectedRuntimeOwners;
            if (origin == null || !graph.ContainsExactCityOwner(origin)
                || !string.Equals(origin.RuntimeId, originProjection.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(origin.LocationRuntimeId,
                    originProjection.Location?.RuntimeId, StringComparison.Ordinal))
                return false;
        }
        if (destinationProjection != null)
        {
            destination = destinationProjection.SelectedRuntimeOwners;
            if (destination == null || !graph.ContainsExactCityOwner(destination)
                || !string.Equals(destination.RuntimeId, destinationProjection.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(destination.LocationRuntimeId,
                    destinationProjection.Location?.RuntimeId, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static bool TryResolveSelectedPartyRoles(
        DailyV1SelectedRuntimeOwnerGraph graph,
        TravelPreparation preparation,
        out List<DailyV1NpcRuntimeOwners> travelers,
        out List<DailyV1NpcRuntimeOwners> escorts)
    {
        travelers = new List<DailyV1NpcRuntimeOwners>(preparation.Travelers.Count);
        escorts = new List<DailyV1NpcRuntimeOwners>(preparation.Escorts.Count);
        foreach (NpcRuntime member in preparation.Travelers)
        {
            if (!graph.TryGetNpcOwner(member.RuntimeId, out DailyV1NpcRuntimeOwners owner)
                || !ReferenceEquals(owner, member.SelectedRuntimeOwners)) return false;
            travelers.Add(owner);
        }
        foreach (NpcRuntime member in preparation.Escorts)
        {
            if (!graph.TryGetNpcOwner(member.RuntimeId, out DailyV1NpcRuntimeOwners owner)
                || !ReferenceEquals(owner, member.SelectedRuntimeOwners)) return false;
            escorts.Add(owner);
        }
        return travelers.Count > 0;
    }

    public bool CanPlanKnownGroupTravel(ActionExecutionContext context, out string reason)
    {
        return TryPrepareTravel(context, true, out _, out reason);
    }

    public IReadOnlyList<NpcRuntime> AdvanceParties()
    {
        using (partyStore.EnterMutationWindow())
        {
        return AdvancePartiesInMutationWindow();
        }
    }

    /// <summary>
    /// Runs the selected Daily-v1 party stage in the shared Core owner graph.
    /// Unity only supplies the established P12/projection boundary and
    /// publishes the noncausal arrival event/log output.
    /// </summary>
    internal bool TryAdvanceSelectedParties(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<NpcRuntime> npcRuntimeList,
        out IReadOnlyList<NpcRuntime> arrivals)
    {
        arrivals = null;
        if (graph == null || npcRuntimeList == null) return false;
        ThrowIfFaulted();

        Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> adapters =
            new Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime>();
        foreach (NpcRuntime runtime in npcRuntimeList)
        {
            DailyV1NpcRuntimeOwners owner = runtime?.SelectedRuntimeOwners;
            if (owner == null || !ReferenceEquals(owner.Definitions, graph.Definitions)
                || !adapters.TryAdd(owner, runtime)) return false;
        }
        if (adapters.Count != graph.Npcs.Count) return false;
        foreach (DailyV1NpcRuntimeOwners owner in graph.Npcs)
            if (!adapters.ContainsKey(owner)) return false;

        Dictionary<DailyV1NpcRuntimeOwners, CityRuntime[]> changedCitiesByOwner =
            new Dictionary<DailyV1NpcRuntimeOwners, CityRuntime[]>();
        bool advanced = graph.TryAdvanceTravelParties(
            partyStore,
            (owner, plan, _) =>
            {
                if (!travelSystem.TryGetSelectedTravelProjectionCandidates(
                        graph, plan, out CityRuntime[] candidates)
                    || !adapters[owner].CanAdmitPreparedSelectedTravelDay(
                        plan, candidates, out CityRuntime[] changedCities)) return false;
                changedCitiesByOwner[owner] = changedCities;
                return true;
            },
            (owner, plan, arriving) =>
            {
                NpcRuntime runtime = adapters[owner];
                if (!changedCitiesByOwner.TryGetValue(owner, out CityRuntime[] changedCities)
                    || !runtime.TryProjectCommittedSelectedTravelDay(
                        plan, arriving, changedCities, out _))
                    throw new InvalidOperationException(
                        "A committed selected TravelParty transition could not publish its Unity projection.");
                runtime.RecordSelectedTravelOwnerMutationCommitted(plan, changedCities);
            },
            PublishSelectedTravelPartyArrivalEvent,
            out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> completed);
        if (!advanced) return false;

        List<NpcRuntime> arrived = new List<NpcRuntime>();
        foreach (DailyV1SelectedTravelPartyArrivalResult result in completed)
        {
            foreach (DailyV1NpcRuntimeOwners owner in result.Members)
                arrived.Add(adapters[owner]);
            if (!result.EventRecorded)
                WarnSelectedTravelPartyArrivalEventFailure(result.Party);
        }
        arrivals = arrived.AsReadOnly();
        return true;
    }

    internal bool PublishSelectedTravelPartyArrivalEvent(TravelPartyRuntime party)
    {
        return party != null && (domainEventRecorder == null || domainEventRecorder.Record(
            (eventId, absoluteDay, recordSequence) => new TravelPartyArrivedEvent(
                eventId,
                absoluteDay,
                recordSequence,
                party.TravelPartyId,
                party.OriginLocationRuntimeId,
                party.DestinationLocationRuntimeId,
                party.RouteRuntimeId,
                party.TravelDaysTotal,
                party.TravelerRuntimeIds,
                party.EscortRuntimeIds,
                party.OriginDecisionId)));
    }

    internal void WarnSelectedTravelPartyArrivalEventFailure(TravelPartyRuntime party)
    {
        if (party == null) return;
        logger.LogWarning("Travel party arrival event could not be recorded for TravelPartyId '"
            + party.TravelPartyId + "'.");
    }

    internal void EnsureSelectedDailyTravelAdvanceAvailable() => ThrowIfFaulted();

    private IReadOnlyList<NpcRuntime> AdvancePartiesInMutationWindow()
    {
        ThrowIfFaulted();

        List<NpcRuntime> arrivals = new List<NpcRuntime>();
        List<TravelPartyRuntime> activeParties = new List<TravelPartyRuntime>(partyStore.ActiveParties);

        foreach (TravelPartyRuntime party in activeParties)
        {
            if (party == null || party.IsActive == false)
            {
                continue;
            }

            if (TryResolvePartyMembers(party, out List<NpcRuntime> members) == false || AreMembersSynchronized(party, members) == false)
            {
                continue;
            }

            bool startedToday = false;

            foreach (NpcRuntime member in members)
            {
                startedToday |= member.TravelStartedToday;
            }

            if (startedToday == true)
            {
                bool revisionsAvailable = true;
                foreach (NpcRuntime member in members)
                    revisionsAvailable &= member.CanApplyTravelStateRevisionIncrements(1L);
                if (!revisionsAvailable) continue;

                foreach (NpcRuntime member in members)
                {
                    if (!member.ClearTravelStartedToday())
                        throw new InvalidOperationException(
                            "A preflighted TravelParty member could not clear its travel-start flag.");
                }

                continue;
            }

            if (members[0].TravelDaysRemaining <= 1
                && (!partyStore.CanCommitMutations(1) || !CanCommitTravelPartyArrival(members)))
            {
                continue;
            }

            long requiredTravelRevisions = members[0].TravelDaysRemaining <= 1 ? 2L : 1L;
            bool travelRevisionsAvailable = true;
            foreach (NpcRuntime member in members)
                travelRevisionsAvailable &= member.CanApplyTravelStateRevisionIncrements(requiredTravelRevisions);
            if (!travelRevisionsAvailable) continue;

            bool allArrived = true;

            foreach (NpcRuntime member in members)
            {
                if (member.AdvanceTravelDay(out _) == false)
                {
                    allArrived = false;
                }
            }

            if (allArrived == false)
            {
                continue;
            }

            foreach (NpcRuntime member in members)
            {
                member.SpatialKnowledge.DiscoverLocation(party.DestinationLocationRuntimeId);
            }

            bool eventRecorded = domainEventRecorder == null || domainEventRecorder.Record((eventId, absoluteDay, sequence) => new TravelPartyArrivedEvent(
                eventId,
                absoluteDay,
                sequence,
                party.TravelPartyId,
                party.OriginLocationRuntimeId,
                party.DestinationLocationRuntimeId,
                party.RouteRuntimeId,
                party.TravelDaysTotal,
                party.TravelerRuntimeIds,
                party.EscortRuntimeIds,
                party.OriginDecisionId));

            foreach (NpcRuntime member in members)
            {
                if (!member.SetActiveTravelPartyId(null))
                    throw new InvalidOperationException(
                        "A preflighted arrived TravelParty member could not clear its active party identity.");
                arrivals.Add(member);
            }

            if (!partyStore.Complete(party.TravelPartyId))
                throw new InvalidOperationException(
                    "A preflighted TravelParty arrival could not commit its party-store completion.");

            if (eventRecorded == false)
            {
                logger.LogWarning("Travel party arrival event could not be recorded for TravelPartyId '" + party.TravelPartyId + "'.");
            }
        }

        return arrivals.AsReadOnly();
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard)
            && partyStore.CanBindMutationGuard(guard)
            && travelSystem.CanBindMutationGuard(guard)
            && SimulationTimeOwnerBridge.CanBindMutationStatus(simulationTime, guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        if (!CanBindMutationGuard(guard))
        {
            return false;
        }

        if (!mutationGuardBinding.TryBindTo(guard))
        {
            return false;
        }

        return partyStore.TryBindMutationGuard(guard)
            && travelSystem.TryBindMutationGuard(guard)
            && SimulationTimeOwnerBridge.TryBindMutationStatus(simulationTime, guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }

    private void ThrowIfFaulted()
    {
        if (!mutationGuardBinding.CanMutate)
        {
            throw new InvalidOperationException("A faulted SimulationRuntime cannot advance travel parties.");
        }
    }


    private static bool TryResolveExactSelectedCoreMembers(
        IReadOnlyList<NpcRuntime> members,
        out DailyV1SelectedRuntimeOwnerGraph graph,
        out List<DailyV1NpcRuntimeOwners> selectedOwners)
    {
        graph = null;
        selectedOwners = null;
        if (members == null || members.Count == 0) return false;

        graph = members[0]?.SelectedRuntimeOwnerGraph;
        if (graph == null) return false;

        List<DailyV1NpcRuntimeOwners> resolved =
            new List<DailyV1NpcRuntimeOwners>(members.Count);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcRuntime member in members)
        {
            DailyV1NpcRuntimeOwners bound = member?.SelectedRuntimeOwners;
            if (member == null
                || bound == null
                || !ReferenceEquals(member.SelectedRuntimeOwnerGraph, graph)
                || !runtimeIds.Add(member.RuntimeId)
                || !graph.TryGetNpcOwner(member.RuntimeId, out DailyV1NpcRuntimeOwners exact)
                || !ReferenceEquals(exact, bound))
                return false;
            resolved.Add(bound);
        }

        selectedOwners = resolved;
        return true;
    }

    private bool TryPrepareTravel(
        ActionExecutionContext context,
        bool requireKnowledge,
        out TravelPreparation preparation,
        out string reason)
    {
        preparation = null;

        if (ActionExecutionValidator.TryValidate(context, DefaultRequirements, out reason) == false)
        {
            return false;
        }

        List<NpcRuntime> travelers = new List<NpcRuntime>();
        List<NpcRuntime> escorts = new List<NpcRuntime>();
        List<NpcRuntime> members = new List<NpcRuntime>();
        Dictionary<string, NpcRuntime> memberById = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        HashSet<string> allMemberIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (ActionExecutionParticipant participant in context.Participants)
        {
            if (participant.Role != ActionExecutionParticipantRole.Performer
                && participant.Role != ActionExecutionParticipantRole.Support)
            {
                reason = "Group travel supports Performer and Support participants only.";
                return false;
            }

            if (allMemberIds.Add(participant.RuntimeId) == false)
            {
                reason = "A group travel member cannot appear in more than one travel role.";
                return false;
            }

            if (identityRegistry.TryGetNpc(participant.RuntimeId, out NpcRuntime member) == false || member == null)
            {
                reason = "A group travel participant could not be resolved.";
                return false;
            }

            if (member.IsAlive == false
                || member.IsTraveling == true
                || string.IsNullOrWhiteSpace(member.ActiveTravelPartyId) == false
                || partyStore.TryGetPartyForNpc(member.RuntimeId, out _))
            {
                reason = "A group travel participant is already traveling or belongs to a TravelParty.";
                return false;
            }

            members.Add(member);
            memberById.Add(member.RuntimeId, member);

            if (participant.Role == ActionExecutionParticipantRole.Performer)
            {
                travelers.Add(member);
            }
            else
            {
                escorts.Add(member);
            }
        }

        if (members.Count == 0)
        {
            reason = "Group travel requires at least one member.";
            return false;
        }

        SpatialLocationRuntime originLocation = members[0].CurrentLocation;

        if (originLocation == null)
        {
            reason = "Group travel participants require a common origin location.";
            return false;
        }

        foreach (NpcRuntime member in members)
        {
            if (member.CurrentLocation != originLocation)
            {
                reason = "Group travel participants must share one origin location.";
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(context.TargetLocationRuntimeId) == true
            || string.IsNullOrWhiteSpace(context.TargetRouteRuntimeId) == true
            || identityRegistry.TryGetLocation(context.TargetLocationRuntimeId, out SpatialLocationRuntime destinationLocation) == false
            || identityRegistry.TryGetRoute(context.TargetRouteRuntimeId, out SpatialRouteRuntime route) == false)
        {
            reason = "Group travel requires a valid destination LocationRuntimeId and RouteRuntimeId.";
            return false;
        }

        if (destinationLocation == originLocation)
        {
            reason = "Group travel destination could not be resolved or equals the origin location.";
            return false;
        }

        if (route.Origin != originLocation
            || route.Destination != destinationLocation
            || travelSystem.GetTravelDays(originLocation, destinationLocation) != route.TravelDays)
        {
            reason = "Group travel route does not match the executable World Truth route.";
            return false;
        }

        float cost = travelSystem.GetTravelCost(route.TravelDays);
        List<TravelPartyMemberCost> costs = new List<TravelPartyMemberCost>();

        foreach (NpcRuntime member in members)
        {
            if (requireKnowledge == true
                && (member.SpatialKnowledge.KnowsLocation(originLocation.RuntimeId) == false
                    || member.SpatialKnowledge.KnowsLocation(destinationLocation.RuntimeId) == false
                    || member.SpatialKnowledge.KnowsRoute(route.RuntimeId) == false))
            {
                reason = "A group travel participant lacks the required spatial knowledge.";
                return false;
            }

            if (transactionService.CanChargeTravel(member, cost) == false)
            {
                reason = "A group travel participant cannot pay the travel cost.";
                return false;
            }

            costs.Add(new TravelPartyMemberCost(member.RuntimeId, cost));
        }

        preparation = new TravelPreparation(
            originLocation,
            travelSystem.GetCityRuntime(originLocation),
            destinationLocation,
            travelSystem.GetCityRuntime(destinationLocation),
            route,
            travelers,
            escorts,
            members,
            memberById,
            costs);
        if (!CanStartTravelProjectionTransaction(preparation))
        {
            reason = "City presence census revision capacity is insufficient for group travel and rollback.";
            return false;
        }
        return true;
    }

    private static bool CanStartTravelProjectionTransaction(TravelPreparation preparation)
    {
        if (preparation == null) return false;

        Dictionary<CityRuntime, long> requiredIncrements = new Dictionary<CityRuntime, long>();
        foreach (NpcRuntime member in preparation.Members)
        {
            if (member == null) return false;
            if (!member.CanPreflightTravelStateMutations(4L)) return false;

            CityRuntime currentCity = member.CurrentCity;
            if (currentCity != null && currentCity.ContainsImportantNpc(member))
                AddRevisionIncrements(requiredIncrements, currentCity, 1L);

            CityRuntime rollbackCity = preparation.OriginCity;
            if (rollbackCity != null)
                AddRevisionIncrements(requiredIncrements, rollbackCity, 1L);
        }

        foreach (KeyValuePair<CityRuntime, long> requirement in requiredIncrements)
        {
            if (!requirement.Key.CanApplyImportantNpcRevisionIncrements(requirement.Value))
                return false;
        }

        return true;
    }

    private static bool CanCommitTravelPartyArrival(List<NpcRuntime> members)
    {
        if (members == null || members.Count == 0) return false;

        CityRuntime destinationCity = members[0]?.DestinationCity;
        if (destinationCity == null)
        {
            return members.TrueForAll(member => member != null && member.DestinationCity == null);
        }

        long requiredIncrements = 0L;
        foreach (NpcRuntime member in members)
        {
            if (member == null || member.DestinationCity != destinationCity) return false;
            if (!destinationCity.ContainsImportantNpc(member)) requiredIncrements++;
        }

        return destinationCity.CanApplyImportantNpcRevisionIncrements(requiredIncrements);
    }

    private static void AddRevisionIncrements(
        Dictionary<CityRuntime, long> incrementsByCity,
        CityRuntime city,
        long increments)
    {
        incrementsByCity.TryGetValue(city, out long current);
        incrementsByCity[city] = current + increments;
    }

    private bool TryResolvePartyMembers(TravelPartyRuntime party, out List<NpcRuntime> members)
    {
        members = new List<NpcRuntime>();

        foreach (string runtimeId in party.MemberRuntimeIds)
        {
            if (identityRegistry.TryGetNpc(runtimeId, out NpcRuntime member) == false || member == null || member.IsAlive == false)
            {
                return false;
            }

            members.Add(member);
        }

        return members.Count > 0;
    }

    private static bool AreMembersSynchronized(TravelPartyRuntime party, List<NpcRuntime> members)
    {
        if (party == null
            || members == null
            || members.Count == 0
            || members[0] == null
            || members[0].IsTraveling == false
            || members[0].DestinationLocation == null
            || members[0].TravelDaysRemaining <= 0
            || string.Equals(members[0].ActiveTravelPartyId, party.TravelPartyId, StringComparison.Ordinal) == false
            || string.Equals(members[0].DestinationLocation.RuntimeId, party.DestinationLocationRuntimeId, StringComparison.Ordinal) == false
            || string.Equals(members[0].TravelOriginDecisionId, party.OriginDecisionId, StringComparison.Ordinal) == false)
        {
            return false;
        }

        int remainingDays = members[0].TravelDaysRemaining;
        SpatialLocationRuntime destination = members[0].DestinationLocation;

        foreach (NpcRuntime member in members)
        {
            if (member == null
                || member.IsTraveling == false
                || member.DestinationLocation != destination
                || member.TravelDaysRemaining != remainingDays
                || string.Equals(member.ActiveTravelPartyId, party.TravelPartyId, StringComparison.Ordinal) == false
                || string.Equals(member.TravelOriginDecisionId, party.OriginDecisionId, StringComparison.Ordinal) == false)
            {
                return false;
            }
        }

        return true;
    }

    private void Rollback(
        List<NpcRuntime> startedMembers,
        List<TravelPartyMemberCost> paidCosts,
        SpatialLocationRuntime originLocation,
        CityRuntime originCity)
    {
        if (paidCosts != null)
        {
            foreach (TravelPartyMemberCost cost in paidCosts)
            {
                if (cost != null && cost.Amount > 0f)
                {
                    // The member lookup is intentionally performed by the started list,
                    // keeping rollback concrete and local to this execution.
                    foreach (NpcRuntime member in startedMembers)
                    {
                        if (member != null && member.RuntimeId == cost.RuntimeId)
                        {
                            transactionService.TryRestoreTravelCharge(member, cost.Amount);
                            break;
                        }
                    }
                }
            }
        }

        if (startedMembers != null)
        {
            foreach (NpcRuntime member in startedMembers)
            {
                member?.CancelTravel(originLocation, originCity);
            }
        }
    }

    private sealed class SelectedCoreTravelPartyStartHostOperation :
        IDailyV1SelectedTravelPartyStartHostOperation
    {
        private readonly TravelPartySystem owner;
        private readonly TravelPreparation preparation;
        private readonly IReadOnlyDictionary<DailyV1NpcRuntimeOwners, NpcRuntime> adapters;
        private readonly CityRuntime[] projectionCandidates;
        private readonly Dictionary<DailyV1SelectedTravelTransitionPlan, CityRuntime[]> changedCitiesByPlan =
            new Dictionary<DailyV1SelectedTravelTransitionPlan, CityRuntime[]>();

        internal SelectedCoreTravelPartyStartHostOperation(
            TravelPartySystem owner,
            TravelPreparation preparation,
            IReadOnlyDictionary<DailyV1NpcRuntimeOwners, NpcRuntime> adapters,
            CityRuntime[] projectionCandidates)
        {
            this.owner = owner;
            this.preparation = preparation;
            this.adapters = adapters;
            this.projectionCandidates = projectionCandidates;
        }

        public bool TryPrepareAndAdmitProjection(
            DailyV1NpcRuntimeOwners owner,
            DailyV1SelectedTravelTransitionPlan plan,
            DailyV1SelectedTravelPartyProjectionTarget target)
        {
            if (!adapters.TryGetValue(owner, out NpcRuntime adapter)
                || changedCitiesByPlan.ContainsKey(plan)
                || !adapter.CanPrepareSelectedTravelProjection(
                    plan, projectionCandidates, out CityRuntime[] changedCities)
                || !adapter.CanAdmitPreparedSelectedTravelDay(plan, changedCities))
                return false;
            changedCitiesByPlan.Add(plan, changedCities);
            return true;
        }

        public void PublishProjection(
            DailyV1NpcRuntimeOwners owner,
            DailyV1SelectedTravelTransitionPlan plan,
            DailyV1SelectedTravelPartyProjectionTarget target)
        {
            if (!adapters.TryGetValue(owner, out NpcRuntime adapter))
                throw new InvalidOperationException(
                    "A committed Core TravelParty transition lost its exact Unity adapter.");
            if (!changedCitiesByPlan.TryGetValue(plan, out CityRuntime[] changedCities))
                throw new InvalidOperationException(
                    "A committed Core TravelParty transition has no prepared projection receipt.");
            if (!this.owner.TryResolveSelectedProjectionTarget(
                    target, preparation, out SpatialLocationRuntime currentLocation,
                    out CityRuntime currentCity, out SpatialLocationRuntime destinationLocation,
                    out CityRuntime destinationCity))
                throw new InvalidOperationException(
                    "A committed Core TravelParty transition lost its prepared projection targets.");
            if (!adapter.TryProjectCommittedSelectedTravelTransition(
                    plan, changedCities, currentLocation, currentCity,
                    destinationLocation, destinationCity))
                throw new InvalidOperationException(
                    "A committed Core TravelParty transition could not publish its Unity projection.");
            adapter.RecordSelectedTravelOwnerMutationCommitted(plan, changedCities);
        }
    }
    private sealed class TravelPreparation
    {
        public SpatialLocationRuntime OriginLocation { get; }
        public CityRuntime OriginCity { get; }
        public SpatialLocationRuntime DestinationLocation { get; }
        public CityRuntime DestinationCity { get; }
        public SpatialRouteRuntime Route { get; }
        public List<NpcRuntime> Travelers { get; }
        public List<NpcRuntime> Escorts { get; }
        public List<NpcRuntime> Members { get; }
        public Dictionary<string, NpcRuntime> MemberById { get; }
        public List<TravelPartyMemberCost> Costs { get; }

        public TravelPreparation(
            SpatialLocationRuntime originLocation,
            CityRuntime originCity,
            SpatialLocationRuntime destinationLocation,
            CityRuntime destinationCity,
            SpatialRouteRuntime route,
            List<NpcRuntime> travelers,
            List<NpcRuntime> escorts,
            List<NpcRuntime> members,
            Dictionary<string, NpcRuntime> memberById,
            List<TravelPartyMemberCost> costs)
        {
            OriginLocation = originLocation;
            OriginCity = originCity;
            DestinationLocation = destinationLocation;
            DestinationCity = destinationCity;
            Route = route;
            Travelers = travelers;
            Escorts = escorts;
            Members = members;
            MemberById = memberById;
            Costs = costs;
        }
    }
}

internal sealed class TravelPartyStartPreparation
{
    public SpatialLocationRuntime OriginLocation { get; }
    public CityRuntime OriginCity { get; }
    public SpatialRouteRuntime Route { get; }
    public IReadOnlyList<NpcRuntime> Members { get; }
    public IReadOnlyList<TravelPartyMemberCost> Costs { get; }

    public TravelPartyStartPreparation(
        SpatialLocationRuntime originLocation,
        CityRuntime originCity,
        SpatialRouteRuntime route,
        IReadOnlyList<NpcRuntime> members,
        IReadOnlyList<TravelPartyMemberCost> costs)
    {
        OriginLocation = originLocation;
        OriginCity = originCity;
        Route = route;
        Members = members;
        Costs = costs;
    }
}
