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

    internal bool UsesP12IdentityOwners(RuntimeIdAllocator allocator, SimulationRecordSequence sequence)
    {
        return ReferenceEquals(idAllocator, allocator)
            && ReferenceEquals(recordSequence, sequence)
            && (domainEventRecorder == null || domainEventRecorder.UsesIdentityOwners(allocator, sequence));
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
        EconomyTransactionResult groupCharge = transactionService.TryChargeTravelGroup(preparation.Members, travelCost);

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
            party => domainEventRecorder == null || domainEventRecorder.Record(
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
                    party.OriginDecisionId)),
            out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> completed);
        if (!advanced) return false;

        List<NpcRuntime> arrived = new List<NpcRuntime>();
        foreach (DailyV1SelectedTravelPartyArrivalResult result in completed)
        {
            foreach (DailyV1NpcRuntimeOwners owner in result.Members)
                arrived.Add(adapters[owner]);
            if (!result.EventRecorded)
                logger.LogWarning("Travel party arrival event could not be recorded for TravelPartyId '"
                    + result.Party.TravelPartyId + "'.");
        }
        arrivals = arrived.AsReadOnly();
        return true;
    }

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
