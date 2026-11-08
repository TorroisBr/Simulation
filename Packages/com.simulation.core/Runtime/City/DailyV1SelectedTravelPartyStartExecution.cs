using System;
using System.Collections.Generic;

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
        if (!TryValidateRequest(graph, members, travelers, escorts,
                originLocationRuntimeId, originCity, destinationLocationRuntimeId,
                destinationCity, routeRuntimeId, travelDays, costs,
                prepareAndAdmitProjection, publishProjection, out float costPerMember))
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

    private static bool TryValidateRequest(
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
        Func<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan,
            DailyV1SelectedTravelPartyProjectionTarget, bool> prepareAndAdmitProjection,
        Action<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan,
            DailyV1SelectedTravelPartyProjectionTarget> publishProjection,
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
            || prepareAndAdmitProjection == null || publishProjection == null
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
