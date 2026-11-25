using System;
using System.Collections.Generic;

/// <summary>
/// Immutable request resolved entirely from the admitted Core owner graph.
/// The Unity host may admit P12 and project mutations, but cannot choose the
/// participating owners, route, roles, or charge values.
/// </summary>
internal sealed class DailyV1SelectedTravelPartyStartPlan
{
    private static readonly ActionParticipationRequirements Requirements =
        new ActionParticipationRequirements(
            minPerformers: 1,
            maxPerformers: ActionParticipationRequirements.Unlimited,
            minSupports: 0,
            maxSupports: ActionParticipationRequirements.Unlimited,
            minTargets: 0,
            maxTargets: 0);

    private readonly IReadOnlyList<DailyV1NpcRuntimeOwners> members;
    private readonly IReadOnlyList<DailyV1NpcRuntimeOwners> travelers;
    private readonly IReadOnlyList<DailyV1NpcRuntimeOwners> escorts;
    private readonly IReadOnlyList<TravelPartyMemberCost> costs;

    internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Members => members;
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Travelers => travelers;
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Escorts => escorts;
    internal SpatialLocationRuntime OriginLocation { get; }
    internal DailyV1CityRuntimeOwners OriginCity { get; }
    internal SpatialLocationRuntime DestinationLocation { get; }
    internal DailyV1CityRuntimeOwners DestinationCity { get; }
    internal SpatialRouteRuntime Route { get; }
    internal int TravelDays => Route.TravelDays;
    internal string OriginDecisionId { get; }
    internal IReadOnlyList<TravelPartyMemberCost> Costs => costs;

    private DailyV1SelectedTravelPartyStartPlan(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1NpcRuntimeOwners> members,
        IReadOnlyList<DailyV1NpcRuntimeOwners> travelers,
        IReadOnlyList<DailyV1NpcRuntimeOwners> escorts,
        SpatialLocationRuntime originLocation,
        DailyV1CityRuntimeOwners originCity,
        SpatialLocationRuntime destinationLocation,
        DailyV1CityRuntimeOwners destinationCity,
        SpatialRouteRuntime route,
        string originDecisionId,
        IReadOnlyList<TravelPartyMemberCost> costs)
    {
        Graph = graph;
        this.members = Array.AsReadOnly(Copy(members));
        this.travelers = Array.AsReadOnly(Copy(travelers));
        this.escorts = Array.AsReadOnly(Copy(escorts));
        OriginLocation = originLocation;
        OriginCity = originCity;
        DestinationLocation = destinationLocation;
        DestinationCity = destinationCity;
        Route = route;
        OriginDecisionId = originDecisionId;
        this.costs = Array.AsReadOnly(Copy(costs));
    }

    internal static bool TryCreate(
        DailyV1SelectedRuntimeOwnerGraph graph,
        ActionExecutionContext context,
        out DailyV1SelectedTravelPartyStartPlan plan)
    {
        plan = null;
        if (graph == null || context == null || graph.SpatialNetwork == null
            || !ActionExecutionValidator.TryValidate(context, Requirements, out _))
            return false;

        List<DailyV1NpcRuntimeOwners> members = new List<DailyV1NpcRuntimeOwners>();
        List<DailyV1NpcRuntimeOwners> travelers = new List<DailyV1NpcRuntimeOwners>();
        List<DailyV1NpcRuntimeOwners> escorts = new List<DailyV1NpcRuntimeOwners>();
        HashSet<string> memberIds = new HashSet<string>(StringComparer.Ordinal);
        string originLocationId = null;
        foreach (ActionExecutionParticipant participant in context.Participants)
        {
            if (participant == null
                || (participant.Role != ActionExecutionParticipantRole.Performer
                    && participant.Role != ActionExecutionParticipantRole.Support)
                || !memberIds.Add(participant.RuntimeId)
                || !graph.TryGetNpcOwner(participant.RuntimeId,
                    out DailyV1NpcRuntimeOwners member)
                || member == null || !graph.ContainsExactNpcOwner(member)
                || (graph.Expeditions != null
                    && graph.Expeditions.IsNpcOnActiveExpedition(member.RuntimeId)))
                return false;

            if (originLocationId == null)
                originLocationId = member.Travel?.CurrentLocationRuntimeId;
            if (string.IsNullOrWhiteSpace(originLocationId)
                || !string.Equals(originLocationId,
                    member.Travel?.CurrentLocationRuntimeId, StringComparison.Ordinal))
                return false;

            members.Add(member);
            if (participant.Role == ActionExecutionParticipantRole.Performer)
                travelers.Add(member);
            else
                escorts.Add(member);
        }

        if (members.Count == 0 || travelers.Count == 0
            || !graph.SpatialNetwork.TryGetLocation(originLocationId,
                out SpatialLocationRuntime originLocation)
            || string.IsNullOrWhiteSpace(context.TargetLocationRuntimeId)
            || !graph.SpatialNetwork.TryGetLocation(context.TargetLocationRuntimeId,
                out SpatialLocationRuntime destinationLocation)
            || ReferenceEquals(originLocation, destinationLocation)
            || string.IsNullOrWhiteSpace(context.TargetRouteRuntimeId)
            || !graph.SpatialNetwork.TryGetRoute(context.TargetRouteRuntimeId,
                out SpatialRouteRuntime route)
            || !ReferenceEquals(route.Origin, originLocation)
            || !ReferenceEquals(route.Destination, destinationLocation)
            || route.TravelDays <= 0
            || !graph.SpatialNetwork.TryGetSingleDirectRoute(originLocation,
                destinationLocation, out SpatialRouteRuntime uniqueRoute, out _)
            || !ReferenceEquals(uniqueRoute, route)
            || !graph.TryGetUniqueCityOwnerForLocation(originLocation.RuntimeId,
                out DailyV1CityRuntimeOwners originCity)
            || !graph.TryGetUniqueCityOwnerForLocation(destinationLocation.RuntimeId,
                out DailyV1CityRuntimeOwners destinationCity)
            || !graph.TryGetMerchantTradeTravelCost(route, out float costPerMember))
            return false;

        List<TravelPartyMemberCost> costs = new List<TravelPartyMemberCost>(members.Count);
        foreach (DailyV1NpcRuntimeOwners member in members)
            costs.Add(new TravelPartyMemberCost(member.RuntimeId, costPerMember));

        if (!DailyV1SelectedTravelPartyStartExecution.TryValidateRequest(
                graph, members, travelers, escorts, originLocation.RuntimeId,
                originCity, destinationLocation.RuntimeId, destinationCity,
                route.RuntimeId, route.TravelDays, costs, out _))
            return false;

        plan = new DailyV1SelectedTravelPartyStartPlan(
            graph, members, travelers, escorts, originLocation, originCity,
            destinationLocation, destinationCity, route, context.OriginDecisionId, costs);
        return true;
    }

    private static T[] Copy<T>(IReadOnlyList<T> source)
    {
        T[] copy = new T[source.Count];
        for (int index = 0; index < copy.Length; index++) copy[index] = source[index];
        return copy;
    }
}

internal interface IDailyV1SelectedTravelPartyStartHostOperation
{
    bool TryPrepareAndAdmitProjection(
        DailyV1NpcRuntimeOwners owner,
        DailyV1SelectedTravelTransitionPlan plan,
        DailyV1SelectedTravelPartyProjectionTarget target);
    void PublishProjection(
        DailyV1NpcRuntimeOwners owner,
        DailyV1SelectedTravelTransitionPlan plan,
        DailyV1SelectedTravelPartyProjectionTarget target);
}
/// <summary>
/// Describes the compatibility projection that must follow one committed Core
/// travel-party transition. The owner graph remains the source of truth.
/// </summary>
internal sealed class DailyV1SelectedTravelPartyProjectionTarget
{
    internal string CurrentLocationRuntimeId { get; }
    internal string CurrentCityRuntimeId { get; }
    internal string DestinationLocationRuntimeId { get; }
    internal string DestinationCityRuntimeId { get; }

    internal DailyV1SelectedTravelPartyProjectionTarget(
        string currentLocationRuntimeId,
        string currentCityRuntimeId,
        string destinationLocationRuntimeId,
        string destinationCityRuntimeId)
    {
        CurrentLocationRuntimeId = currentLocationRuntimeId;
        CurrentCityRuntimeId = currentCityRuntimeId;
        DestinationLocationRuntimeId = destinationLocationRuntimeId;
        DestinationCityRuntimeId = destinationCityRuntimeId;
    }
}

/// <summary>
/// Executes a fully selected group-travel start against the existing Core
/// owner graph. Unity supplies only its established P12 admission and
/// compatibility-projection callbacks; it does not commit authoritative
/// travel, account, party, identity, event, or knowledge state.
/// </summary>
internal static class DailyV1SelectedTravelPartyStartExecution
{
    internal static bool TryStart(
        DailyV1SelectedTravelPartyStartPlan plan,
        DomainEventRecorder eventRecorder,
        IDailyV1SelectedTravelPartyStartHostOperation hostOperation,
        out TravelPartyRuntime party)
    {
        party = null;

        if (plan == null || hostOperation == null) return false;
        return TryStart(
            plan.Graph, plan.Members, plan.Travelers, plan.Escorts,
            plan.OriginLocation.RuntimeId, plan.OriginCity,
            plan.DestinationLocation.RuntimeId, plan.DestinationCity,
            plan.Route.RuntimeId, plan.TravelDays, plan.OriginDecisionId,
            plan.Costs, eventRecorder,
            hostOperation.TryPrepareAndAdmitProjection,
            hostOperation.PublishProjection, out party);
    }
    internal static bool TryStart(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1NpcRuntimeOwners> members,
        IReadOnlyList<DailyV1NpcRuntimeOwners> travelers,
        IReadOnlyList<DailyV1NpcRuntimeOwners> escorts,
        string originLocationRuntimeId,
        DailyV1CityRuntimeOwners originCity,
        string destinationLocationRuntimeId,
        DailyV1CityRuntimeOwners destinationCity,
        string routeRuntimeId,
        int travelDays,
        string originDecisionId,
        IReadOnlyList<TravelPartyMemberCost> costs,
        DomainEventRecorder eventRecorder,
        Func<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan,
            DailyV1SelectedTravelPartyProjectionTarget, bool> prepareAndAdmitProjection,
        Action<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan,
            DailyV1SelectedTravelPartyProjectionTarget> publishProjection,
        out TravelPartyRuntime party)
    {
        party = null;
        if (prepareAndAdmitProjection == null || publishProjection == null) return false;
        if (!TryValidateRequest(graph, members, travelers, escorts,
                originLocationRuntimeId, originCity, destinationLocationRuntimeId,
                destinationCity, routeRuntimeId, travelDays, costs,
                out float costPerMember))
            return false;

        using (graph.TravelParties.EnterMutationWindow())
        {
            if (!graph.TravelParties.CanCommitMutations(2)) return false;

            string partyId;
            try
            {
                partyId = graph.RuntimeIdAllocator.AllocateTravelPartyId();
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            TravelPartyRuntime created = new TravelPartyRuntime(
                partyId,
                originLocationRuntimeId,
                destinationLocationRuntimeId,
                routeRuntimeId,
                RuntimeIds(travelers),
                RuntimeIds(escorts),
                travelDays,
                originDecisionId,
                costs);

            List<DailyV1NpcRuntimeOwners> started = new List<DailyV1NpcRuntimeOwners>(members.Count);
            List<TravelPartyMemberCost> paid = new List<TravelPartyMemberCost>();
            DailyV1SelectedTravelPartyProjectionTarget travelingTarget =
                new DailyV1SelectedTravelPartyProjectionTarget(
                    null, null, destinationLocationRuntimeId, destinationCity?.RuntimeId);

            foreach (DailyV1NpcRuntimeOwners member in members)
            {
                if (!DailyV1SelectedTravelTransitionPlan.TryStartTravel(
                        member, originCity, originLocationRuntimeId,
                        destinationLocationRuntimeId, destinationCity, travelDays,
                        originDecisionId, routeRuntimeId,
                        out DailyV1SelectedTravelTransitionPlan plan)
                    || !TryCommitTransition(graph, member, plan, travelingTarget,
                        prepareAndAdmitProjection, publishProjection))
                {
                    Rollback(graph, started, paid, originLocationRuntimeId, originCity,
                        destinationLocationRuntimeId, destinationCity,
                        prepareAndAdmitProjection, publishProjection);
                    return false;
                }
                started.Add(member);
            }

            EconomyTransactionResult charge = DailyV1SelectedTravelPartyChargeExecution.TryCharge(
                graph, members, costPerMember);
            if (charge == null || !charge.Success)
            {
                Rollback(graph, started, paid, originLocationRuntimeId, originCity,
                    destinationLocationRuntimeId, destinationCity,
                    prepareAndAdmitProjection, publishProjection);
                return false;
            }
            foreach (TravelPartyMemberCost cost in costs) paid.Add(cost);

            if (!graph.TravelParties.Add(created))
            {
                Rollback(graph, started, paid, originLocationRuntimeId, originCity,
                    destinationLocationRuntimeId, destinationCity,
                    prepareAndAdmitProjection, publishProjection);
                return false;
            }

            foreach (DailyV1NpcRuntimeOwners member in members)
            {
                if (!DailyV1SelectedTravelTransitionPlan.TrySetActiveTravelParty(
                        member, created.TravelPartyId,
                        out DailyV1SelectedTravelTransitionPlan plan)
                    || !TryCommitTransition(graph, member, plan, travelingTarget,
                        prepareAndAdmitProjection, publishProjection))
                    throw new InvalidOperationException(
                        "A preflighted selected TravelParty member could not commit its active party identity.");
            }

            bool eventRecorded = eventRecorder == null || eventRecorder.Record(
                (eventId, absoluteDay, sequence) => new TravelPartyStartedEvent(
                    eventId, absoluteDay, sequence, created.TravelPartyId,
                    created.OriginLocationRuntimeId, created.DestinationLocationRuntimeId,
                    created.RouteRuntimeId, created.TravelDaysTotal,
                    created.TravelerRuntimeIds, created.EscortRuntimeIds,
                    created.OriginDecisionId));

            if (!eventRecorded)
            {
                DailyV1SelectedTravelPartyProjectionTarget clearedTarget =
                    new DailyV1SelectedTravelPartyProjectionTarget(
                        null, null, destinationLocationRuntimeId, destinationCity?.RuntimeId);
                foreach (DailyV1NpcRuntimeOwners member in members)
                {
                    if (!DailyV1SelectedTravelTransitionPlan.TrySetActiveTravelParty(
                            member, null,
                            out DailyV1SelectedTravelTransitionPlan plan)
                        || !TryCommitTransition(graph, member, plan, clearedTarget,
                            prepareAndAdmitProjection, publishProjection))
                        throw new InvalidOperationException(
                            "A preflighted selected TravelParty member could not clear its active party identity during compensation.");
                }

                if (!graph.TravelParties.Remove(created.TravelPartyId))
                    throw new InvalidOperationException(
                        "A preflighted selected TravelParty store could not remove a failed start.");
                Rollback(graph, started, paid, originLocationRuntimeId, originCity,
                    destinationLocationRuntimeId, destinationCity,
                    prepareAndAdmitProjection, publishProjection);
                return false;
            }

            foreach (DailyV1NpcRuntimeOwners member in members)
            {
                member.SpatialKnowledge?.DiscoverLocation(originLocationRuntimeId);
                member.SpatialKnowledge?.DiscoverRoute(routeRuntimeId);
            }

            party = created;
            return true;
        }
    }

    internal static bool TryValidateRequest(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1NpcRuntimeOwners> members,
        IReadOnlyList<DailyV1NpcRuntimeOwners> travelers,
        IReadOnlyList<DailyV1NpcRuntimeOwners> escorts,
        string originLocationRuntimeId,
        DailyV1CityRuntimeOwners originCity,
        string destinationLocationRuntimeId,
        DailyV1CityRuntimeOwners destinationCity,
        string routeRuntimeId,
        int travelDays,
        IReadOnlyList<TravelPartyMemberCost> costs,
        out float costPerMember)
    {
        costPerMember = -1f;
        if (graph == null || graph.TravelParties == null || graph.RuntimeIdAllocator == null
            || graph.SpatialNetwork == null || members == null || members.Count == 0
            || travelers == null || travelers.Count == 0 || escorts == null || costs == null
            || costs.Count != members.Count || travelDays <= 0
            || string.IsNullOrWhiteSpace(originLocationRuntimeId)
            || string.IsNullOrWhiteSpace(destinationLocationRuntimeId)
            || string.Equals(originLocationRuntimeId, destinationLocationRuntimeId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(routeRuntimeId)
            || (originCity != null && (!graph.ContainsExactCityOwner(originCity)
                || !string.Equals(originCity.LocationRuntimeId, originLocationRuntimeId, StringComparison.Ordinal)))
            || (destinationCity != null && (!graph.ContainsExactCityOwner(destinationCity)
                || !string.Equals(destinationCity.LocationRuntimeId, destinationLocationRuntimeId, StringComparison.Ordinal)))
            || !graph.SpatialNetwork.TryGetLocation(originLocationRuntimeId, out SpatialLocationRuntime origin)
            || !graph.SpatialNetwork.TryGetLocation(destinationLocationRuntimeId, out SpatialLocationRuntime destination)
            || !graph.SpatialNetwork.TryGetRoute(routeRuntimeId, out SpatialRouteRuntime route)
            || !ReferenceEquals(route.Origin, origin) || !ReferenceEquals(route.Destination, destination)
            || route.TravelDays != travelDays)
            return false;

        HashSet<DailyV1NpcRuntimeOwners> requestedMembers = new HashSet<DailyV1NpcRuntimeOwners>();
        HashSet<string> memberIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, TravelPartyMemberCost> costsById =
            new Dictionary<string, TravelPartyMemberCost>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners member in members)
        {
            if (!graph.ContainsExactNpcOwner(member) || member.Lifecycle == null
                || member.Lifecycle.LifeState != NpcLifeState.Alive || member.Travel == null
                || member.Travel.IsTraveling || !string.IsNullOrWhiteSpace(member.Travel.ActiveTravelPartyId)
                || !string.Equals(member.Travel.CurrentLocationRuntimeId, originLocationRuntimeId, StringComparison.Ordinal)
                || !string.Equals(member.Travel.CurrentCityRuntimeId, originCity?.RuntimeId, StringComparison.Ordinal)
                || !member.Travel.CanAdvanceRevision(4)
                || member.MoneyAccount == null
                || !requestedMembers.Add(member) || !memberIds.Add(member.RuntimeId)
                || graph.TravelParties.TryGetPartyForNpc(member.RuntimeId, out _))
                return false;
        }

        foreach (DailyV1NpcRuntimeOwners member in travelers)
            if (!requestedMembers.Contains(member)) return false;
        HashSet<DailyV1NpcRuntimeOwners> roleMembers = new HashSet<DailyV1NpcRuntimeOwners>();
        foreach (DailyV1NpcRuntimeOwners member in travelers)
            if (!roleMembers.Add(member)) return false;
        foreach (DailyV1NpcRuntimeOwners member in escorts)
            if (!requestedMembers.Contains(member) || !roleMembers.Add(member)) return false;
        if (roleMembers.Count != requestedMembers.Count) return false;

        for (int index = 0; index < costs.Count; index++)
        {
            TravelPartyMemberCost cost = costs[index];
            if (cost == null || !string.Equals(cost.RuntimeId, members[index].RuntimeId, StringComparison.Ordinal)
                || costsById.ContainsKey(cost.RuntimeId)) return false;
            costsById.Add(cost.RuntimeId, cost);
            if (index == 0) costPerMember = cost.Amount;
            else if (cost.Amount != costPerMember) return false;
        }
        if (!IsValidNonNegativeFiniteAmount(costPerMember)) return false;

        long originPresenceChanges = members.Count;
        foreach (DailyV1NpcRuntimeOwners member in members)
        {
            if (!member.MoneyAccount.CanDebit(costPerMember)
                || !CanReserveChargeAndRefundRevisionCapacity(member.MoneyAccount, costPerMember))
                return false;
            if (originCity != null && originCity.NpcPresence.Contains(member.RuntimeId))
                originPresenceChanges++;
        }
        return originCity == null || originCity.NpcPresence.CanApplyRevisionIncrements(originPresenceChanges);
    }

    internal static bool CanReserveChargeAndRefundRevisionCapacity(
        MoneyAccountRuntime account,
        float amountPerMember) =>
        account != null
        && IsValidNonNegativeFiniteAmount(amountPerMember)
        && (amountPerMember == 0f || account.Revision <= long.MaxValue - 2L);

    private static bool TryCommitTransition(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners member,
        DailyV1SelectedTravelTransitionPlan plan,
        DailyV1SelectedTravelPartyProjectionTarget target,
        Func<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan,
            DailyV1SelectedTravelPartyProjectionTarget, bool> prepareAndAdmitProjection,
        Action<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan,
            DailyV1SelectedTravelPartyProjectionTarget> publishProjection)
    {
        if (plan == null || !plan.IsPreparedFor(member, graph)) return false;
        if (plan.IsNoOp) return true;
        if (!prepareAndAdmitProjection(member, plan, target)
            || !graph.TryCommitTravelTransition(member, plan, null)) return false;
        publishProjection(member, plan, target);
        return true;
    }

    private static void Rollback(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1NpcRuntimeOwners> started,
        IReadOnlyList<TravelPartyMemberCost> paid,
        string originLocationRuntimeId,
        DailyV1CityRuntimeOwners originCity,
        string destinationLocationRuntimeId,
        DailyV1CityRuntimeOwners destinationCity,
        Func<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan,
            DailyV1SelectedTravelPartyProjectionTarget, bool> prepareAndAdmitProjection,
        Action<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan,
            DailyV1SelectedTravelPartyProjectionTarget> publishProjection)
    {
        if (paid != null)
        {
            foreach (TravelPartyMemberCost cost in paid)
            {
                if (cost == null || cost.Amount <= 0f) continue;
                if (!graph.TryGetNpcOwner(cost.RuntimeId, out DailyV1NpcRuntimeOwners member)
                    || !member.MoneyAccount.TryCredit(cost.Amount))
                    throw new InvalidOperationException(
                        "A preflighted selected TravelParty account could not restore its committed charge.");
            }
        }

        if (started == null) return;
        DailyV1SelectedTravelPartyProjectionTarget originTarget =
            new DailyV1SelectedTravelPartyProjectionTarget(
                originLocationRuntimeId, originCity?.RuntimeId, null, null);
        foreach (DailyV1NpcRuntimeOwners member in started)
        {
            if (!DailyV1SelectedTravelTransitionPlan.TryCancelTravel(
                    member, originLocationRuntimeId, originCity,
                    out DailyV1SelectedTravelTransitionPlan plan)
                || !TryCommitTransition(graph, member, plan, originTarget,
                    prepareAndAdmitProjection, publishProjection))
                throw new InvalidOperationException(
                    "A preflighted selected TravelParty member could not cancel its started travel during compensation.");
        }
    }

    private static List<string> RuntimeIds(IReadOnlyList<DailyV1NpcRuntimeOwners> owners)
    {
        List<string> ids = new List<string>(owners.Count);
        foreach (DailyV1NpcRuntimeOwners owner in owners) ids.Add(owner.RuntimeId);
        return ids;
    }

    private static bool IsValidNonNegativeFiniteAmount(float amount) =>
        amount >= 0f && !float.IsNaN(amount) && !float.IsInfinity(amount);
}
