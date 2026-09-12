using System;
using System.Collections.Generic;

[Serializable]
public sealed class TravelPartyMemberCost
{
    private readonly string runtimeId;
    private readonly float amount;

    public string RuntimeId => runtimeId;
    public float Amount => amount;

    public TravelPartyMemberCost(string runtimeId, float amount)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("Travel party cost requires a member RuntimeId.", nameof(runtimeId));
        }

        if (amount < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        this.runtimeId = runtimeId;
        this.amount = amount;
    }
}

[Serializable]
public sealed class TravelPartyRuntime
{
    private readonly string travelPartyId;
    private readonly string originLocationRuntimeId;
    private readonly string destinationLocationRuntimeId;
    private readonly string routeRuntimeId;
    private readonly IReadOnlyList<string> travelerRuntimeIds;
    private readonly IReadOnlyList<string> escortRuntimeIds;
    private readonly IReadOnlyList<string> memberRuntimeIds;
    private readonly IReadOnlyList<TravelPartyMemberCost> memberCosts;
    private readonly int travelDaysTotal;
    private readonly string originDecisionId;
    private bool completed;

    public string TravelPartyId => travelPartyId;
    public string OriginLocationRuntimeId => originLocationRuntimeId;
    public string DestinationLocationRuntimeId => destinationLocationRuntimeId;
    public string RouteRuntimeId => routeRuntimeId;
    public IReadOnlyList<string> TravelerRuntimeIds => travelerRuntimeIds;
    public IReadOnlyList<string> EscortRuntimeIds => escortRuntimeIds;
    public IReadOnlyList<string> MemberRuntimeIds => memberRuntimeIds;
    public IReadOnlyList<TravelPartyMemberCost> MemberCosts => memberCosts;
    public int TravelDaysTotal => travelDaysTotal;
    public string OriginDecisionId => originDecisionId;
    public bool IsCompleted => completed;
    public bool IsActive => completed == false;

    public TravelPartyRuntime(
        string travelPartyId,
        string originLocationRuntimeId,
        string destinationLocationRuntimeId,
        string routeRuntimeId,
        IEnumerable<string> travelerRuntimeIds,
        IEnumerable<string> escortRuntimeIds,
        int travelDaysTotal,
        string originDecisionId,
        IEnumerable<TravelPartyMemberCost> memberCosts)
    {
        this.travelPartyId = RequireId(travelPartyId, nameof(travelPartyId));
        this.originLocationRuntimeId = RequireId(originLocationRuntimeId, nameof(originLocationRuntimeId));
        this.destinationLocationRuntimeId = RequireId(destinationLocationRuntimeId, nameof(destinationLocationRuntimeId));
        this.routeRuntimeId = RequireId(routeRuntimeId, nameof(routeRuntimeId));

        if (travelDaysTotal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(travelDaysTotal));
        }

        this.travelDaysTotal = travelDaysTotal;
        this.originDecisionId = NormalizeOptionalId(originDecisionId);
        this.travelerRuntimeIds = CaptureIds(travelerRuntimeIds, nameof(travelerRuntimeIds));
        this.escortRuntimeIds = CaptureIds(escortRuntimeIds, nameof(escortRuntimeIds));

        if (this.travelerRuntimeIds.Count == 0)
        {
            throw new ArgumentException("Travel party requires at least one traveler.", nameof(travelerRuntimeIds));
        }

        HashSet<string> allMemberIds = new HashSet<string>(StringComparer.Ordinal);
        List<string> allMembers = new List<string>();

        foreach (string runtimeId in this.travelerRuntimeIds)
        {
            if (allMemberIds.Add(runtimeId) == false)
            {
                throw new ArgumentException("Travel party members must be distinct.", nameof(travelerRuntimeIds));
            }

            allMembers.Add(runtimeId);
        }

        foreach (string runtimeId in this.escortRuntimeIds)
        {
            if (allMemberIds.Add(runtimeId) == false)
            {
                throw new ArgumentException("Travel party members must be distinct.", nameof(escortRuntimeIds));
            }

            allMembers.Add(runtimeId);
        }

        memberRuntimeIds = allMembers.AsReadOnly();
        memberCosts = CaptureCosts(memberCosts);

        if (memberCosts.Count != memberRuntimeIds.Count)
        {
            throw new ArgumentException("Travel party requires one cost snapshot per member.", nameof(memberCosts));
        }

        foreach (TravelPartyMemberCost cost in memberCosts)
        {
            if (allMemberIds.Contains(cost.RuntimeId) == false)
            {
                throw new ArgumentException("Travel party cost references an unknown member.", nameof(memberCosts));
            }
        }
    }

    public TravelPartyMemberCost GetCostForMember(string runtimeId)
    {
        foreach (TravelPartyMemberCost cost in memberCosts)
        {
            if (cost != null && string.Equals(cost.RuntimeId, runtimeId, StringComparison.Ordinal) == true)
            {
                return cost;
            }
        }

        return null;
    }

    internal void MarkCompleted()
    {
        completed = true;
    }

    private static IReadOnlyList<string> CaptureIds(IEnumerable<string> source, string parameterName)
    {
        List<string> snapshot = new List<string>();
        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);

        if (source != null)
        {
            foreach (string runtimeId in source)
            {
                if (string.IsNullOrWhiteSpace(runtimeId) == true || uniqueIds.Add(runtimeId) == false)
                {
                    throw new ArgumentException("Travel party member RuntimeIds must be non-empty and unique.", parameterName);
                }

                snapshot.Add(runtimeId);
            }
        }

        return snapshot.AsReadOnly();
    }

    private static IReadOnlyList<TravelPartyMemberCost> CaptureCosts(IEnumerable<TravelPartyMemberCost> source)
    {
        List<TravelPartyMemberCost> snapshot = new List<TravelPartyMemberCost>();
        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);

        if (source != null)
        {
            foreach (TravelPartyMemberCost cost in source)
            {
                if (cost == null || uniqueIds.Add(cost.RuntimeId) == false)
                {
                    throw new ArgumentException("Travel party member costs must be unique.", nameof(source));
                }

                snapshot.Add(cost);
            }
        }

        return snapshot.AsReadOnly();
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Travel party requires stable IDs.", parameterName);
        }

        return value;
    }

    private static string NormalizeOptionalId(string value)
    {
        return string.IsNullOrWhiteSpace(value) == true ? null : value;
    }
}

public sealed class TravelPartyStore
{
    private readonly List<TravelPartyRuntime> activeParties = new List<TravelPartyRuntime>();
    private readonly Dictionary<string, TravelPartyRuntime> partiesById = new Dictionary<string, TravelPartyRuntime>(StringComparer.Ordinal);
    private readonly IReadOnlyList<TravelPartyRuntime> readOnlyActiveParties;

    public IReadOnlyList<TravelPartyRuntime> ActiveParties => readOnlyActiveParties;

    public TravelPartyStore()
    {
        readOnlyActiveParties = activeParties.AsReadOnly();
    }

    public TravelPartyRuntime GetById(string travelPartyId)
    {
        return travelPartyId != null && partiesById.TryGetValue(travelPartyId, out TravelPartyRuntime party) == true
            ? party
            : null;
    }

    public bool Add(TravelPartyRuntime party)
    {
        if (party == null || party.IsActive == false || partiesById.ContainsKey(party.TravelPartyId) == true)
        {
            return false;
        }

        foreach (string memberRuntimeId in party.MemberRuntimeIds)
        {
            if (TryGetPartyForNpc(memberRuntimeId, out _))
            {
                return false;
            }
        }

        partiesById.Add(party.TravelPartyId, party);
        activeParties.Add(party);
        return true;
    }

    public bool TryGetPartyForNpc(string npcRuntimeId, out TravelPartyRuntime party)
    {
        party = null;

        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            return false;
        }

        foreach (TravelPartyRuntime candidate in activeParties)
        {
            if (candidate != null && ContainsMember(candidate.MemberRuntimeIds, npcRuntimeId) == true)
            {
                party = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool ContainsMember(IReadOnlyList<string> memberRuntimeIds, string npcRuntimeId)
    {
        if (memberRuntimeIds == null)
        {
            return false;
        }

        foreach (string memberRuntimeId in memberRuntimeIds)
        {
            if (string.Equals(memberRuntimeId, npcRuntimeId, StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }

    public bool Complete(string travelPartyId)
    {
        TravelPartyRuntime party = GetById(travelPartyId);

        if (party == null)
        {
            return false;
        }

        party.MarkCompleted();
        partiesById.Remove(travelPartyId);
        activeParties.Remove(party);
        return true;
    }

    public bool Remove(string travelPartyId)
    {
        TravelPartyRuntime party = GetById(travelPartyId);

        if (party == null)
        {
            return false;
        }

        partiesById.Remove(travelPartyId);
        activeParties.Remove(party);
        return true;
    }
}

public sealed class TravelPartySystem
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

    public TravelPartyStore Store => partyStore;

    public TravelPartySystem(
        TravelPartyStore partyStore,
        RuntimeIdAllocator idAllocator,
        RuntimeIdentityRegistry identityRegistry,
        TravelSystem travelSystem,
        SimulationTime simulationTime,
        SimulationRecordSequence recordSequence,
        DomainEventRecorder domainEventRecorder,
        SimulationLogger logger = null)
    {
        this.partyStore = partyStore ?? throw new ArgumentNullException(nameof(partyStore));
        this.idAllocator = idAllocator ?? throw new ArgumentNullException(nameof(idAllocator));
        this.identityRegistry = identityRegistry ?? throw new ArgumentNullException(nameof(identityRegistry));
        this.travelSystem = travelSystem ?? throw new ArgumentNullException(nameof(travelSystem));
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        this.recordSequence = recordSequence ?? throw new ArgumentNullException(nameof(recordSequence));
        this.domainEventRecorder = domainEventRecorder;
        this.logger = logger ?? new SimulationLogger(null);
        travelSystem.AttachTravelPartyStore(partyStore);
    }

    public bool TryStartTravelParty(ActionExecutionContext context)
    {
        return TryStartTravelParty(context, out _);
    }

    public bool TryStartTravelParty(ActionExecutionContext context, out TravelPartyRuntime party)
    {
        party = null;

        if (TryPrepareTravel(context, false, out TravelPreparation preparation, out _) == false)
        {
            return false;
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

        party = new TravelPartyRuntime(
            partyId,
            preparation.Origin.Location.RuntimeId,
            preparation.Destination.Location.RuntimeId,
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
            if (member.StartTravel(preparation.Destination, preparation.Route.TravelDays, context.OriginDecisionId) == false)
            {
                Rollback(startedMembers, paidCosts, preparation.Origin);
                party = null;
                return false;
            }

            startedMembers.Add(member);
        }

        foreach (TravelPartyMemberCost cost in preparation.Costs)
        {
            NpcRuntime member = preparation.MemberById[cost.RuntimeId];

            if (member.TrySpendMoney(cost.Amount) == false)
            {
                Rollback(startedMembers, paidCosts, preparation.Origin);
                party = null;
                return false;
            }

            paidCosts.Add(cost);
        }

        if (partyStore.Add(party) == false)
        {
            Rollback(startedMembers, paidCosts, preparation.Origin);
            party = null;
            return false;
        }

        foreach (NpcRuntime member in preparation.Members)
        {
            member.SetActiveTravelPartyId(party.TravelPartyId);
        }

        bool eventRecorded = domainEventRecorder == null || domainEventRecorder.Record((eventId, absoluteDay, sequence) => new TravelPartyStartedEvent(
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

        if (eventRecorded == false)
        {
            foreach (NpcRuntime member in preparation.Members)
            {
                member.SetActiveTravelPartyId(null);
            }

            partyStore.Remove(party.TravelPartyId);
            Rollback(startedMembers, paidCosts, preparation.Origin);
            party = null;
            return false;
        }

        foreach (NpcRuntime member in preparation.Members)
        {
            member.SpatialKnowledge.DiscoverLocation(preparation.Origin.Location.RuntimeId);
            member.SpatialKnowledge.DiscoverRoute(preparation.Route.RuntimeId);
        }

        return true;
    }

    public bool CanPlanKnownGroupTravel(ActionExecutionContext context, out string reason)
    {
        return TryPrepareTravel(context, true, out _, out reason);
    }

    public IReadOnlyList<NpcRuntime> AdvanceParties()
    {
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
                foreach (NpcRuntime member in members)
                {
                    member.ClearTravelStartedToday();
                }

                continue;
            }

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
                member.SetActiveTravelPartyId(null);
                arrivals.Add(member);
            }

            partyStore.Complete(party.TravelPartyId);

            if (eventRecorded == false)
            {
                logger.LogWarning("Travel party arrival event could not be recorded for TravelPartyId '" + party.TravelPartyId + "'.");
            }
        }

        return arrivals.AsReadOnly();
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

            if (member.IsTraveling == true
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

        CityRuntime origin = members[0].CurrentCity;

        if (origin == null || origin.Location == null)
        {
            reason = "Group travel participants require a common origin city.";
            return false;
        }

        foreach (NpcRuntime member in members)
        {
            if (member.CurrentCity != origin)
            {
                reason = "Group travel participants must share one origin city.";
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

        CityRuntime destination = travelSystem.GetCityRuntime(destinationLocation);

        if (destination == null || destination.Location == null || destination == origin)
        {
            reason = "Group travel destination could not be resolved or equals the origin.";
            return false;
        }

        if (route.Origin != origin.Location
            || route.Destination != destination.Location
            || travelSystem.GetTravelDays(origin, destination) != route.TravelDays)
        {
            reason = "Group travel route does not match the executable World Truth route.";
            return false;
        }

        float cost = travelSystem.GetTravelCost(route.TravelDays);
        List<TravelPartyMemberCost> costs = new List<TravelPartyMemberCost>();

        foreach (NpcRuntime member in members)
        {
            if (requireKnowledge == true
                && (member.SpatialKnowledge.KnowsLocation(origin.Location.RuntimeId) == false
                    || member.SpatialKnowledge.KnowsLocation(destination.Location.RuntimeId) == false
                    || member.SpatialKnowledge.KnowsRoute(route.RuntimeId) == false))
            {
                reason = "A group travel participant lacks the required spatial knowledge.";
                return false;
            }

            if (member.Money < cost)
            {
                reason = "A group travel participant cannot pay the travel cost.";
                return false;
            }

            costs.Add(new TravelPartyMemberCost(member.RuntimeId, cost));
        }

        preparation = new TravelPreparation(
            origin,
            destination,
            route,
            travelers,
            escorts,
            members,
            memberById,
            costs);
        return true;
    }

    private bool TryResolvePartyMembers(TravelPartyRuntime party, out List<NpcRuntime> members)
    {
        members = new List<NpcRuntime>();

        foreach (string runtimeId in party.MemberRuntimeIds)
        {
            if (identityRegistry.TryGetNpc(runtimeId, out NpcRuntime member) == false || member == null)
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
            || members[0].DestinationCity == null
            || members[0].DestinationCity.Location == null
            || members[0].TravelDaysRemaining <= 0
            || string.Equals(members[0].ActiveTravelPartyId, party.TravelPartyId, StringComparison.Ordinal) == false
            || string.Equals(members[0].DestinationCity.Location.RuntimeId, party.DestinationLocationRuntimeId, StringComparison.Ordinal) == false
            || string.Equals(members[0].TravelOriginDecisionId, party.OriginDecisionId, StringComparison.Ordinal) == false)
        {
            return false;
        }

        int remainingDays = members[0].TravelDaysRemaining;
        CityRuntime destination = members[0].DestinationCity;

        foreach (NpcRuntime member in members)
        {
            if (member == null
                || member.IsTraveling == false
                || member.DestinationCity != destination
                || member.TravelDaysRemaining != remainingDays
                || string.Equals(member.ActiveTravelPartyId, party.TravelPartyId, StringComparison.Ordinal) == false
                || string.Equals(member.TravelOriginDecisionId, party.OriginDecisionId, StringComparison.Ordinal) == false)
            {
                return false;
            }
        }

        return true;
    }

    private static void Rollback(
        List<NpcRuntime> startedMembers,
        List<TravelPartyMemberCost> paidCosts,
        CityRuntime origin)
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
                            member.AddMoney(cost.Amount);
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
                member?.CancelTravel(origin);
            }
        }
    }

    private sealed class TravelPreparation
    {
        public CityRuntime Origin { get; }
        public CityRuntime Destination { get; }
        public SpatialRouteRuntime Route { get; }
        public List<NpcRuntime> Travelers { get; }
        public List<NpcRuntime> Escorts { get; }
        public List<NpcRuntime> Members { get; }
        public Dictionary<string, NpcRuntime> MemberById { get; }
        public List<TravelPartyMemberCost> Costs { get; }

        public TravelPreparation(
            CityRuntime origin,
            CityRuntime destination,
            SpatialRouteRuntime route,
            List<NpcRuntime> travelers,
            List<NpcRuntime> escorts,
            List<NpcRuntime> members,
            Dictionary<string, NpcRuntime> memberById,
            List<TravelPartyMemberCost> costs)
        {
            Origin = origin;
            Destination = destination;
            Route = route;
            Travelers = travelers;
            Escorts = escorts;
            Members = members;
            MemberById = memberById;
            Costs = costs;
        }
    }
}
