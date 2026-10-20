using System;
using System.Collections.Generic;

/// <summary>
/// A single selected Daily-v1 NPC travel/presence change prepared against the
/// existing Core owners. Planning is side-effect free so the host can perform
/// its P12 admission check before any owner or Unity projection is changed.
/// </summary>
internal sealed class DailyV1SelectedTravelTransitionPlan
{
    private sealed class PresenceChange
    {
        internal DailyV1CityRuntimeOwners City { get; }
        internal bool Add { get; }
        internal long ExpectedRevision { get; }

        internal PresenceChange(DailyV1CityRuntimeOwners city, bool add)
        {
            City = city;
            Add = add;
            ExpectedRevision = city.NpcPresence.Revision;
        }
    }

    private readonly DailyV1NpcRuntimeOwners npc;
    private readonly DailyV1NpcTravelOwner travel;
    private readonly string expectedCurrentLocationId;
    private readonly string expectedCurrentCityId;
    private readonly string expectedDestinationLocationId;
    private readonly string expectedDestinationCityId;
    private readonly int expectedTravelDaysRemaining;
    private readonly int expectedTravelDaysTotal;
    private readonly string expectedRouteId;
    private readonly bool expectedTravelStartedToday;
    private readonly string expectedOriginDecisionId;
    private readonly string expectedActiveTravelPartyId;
    private readonly long expectedTravelRevision;
    private readonly string currentLocationId;
    private readonly string currentCityId;
    private readonly string destinationLocationId;
    private readonly string destinationCityId;
    private readonly int travelDaysRemaining;
    private readonly int travelDaysTotal;
    private readonly string routeId;
    private readonly bool travelStartedToday;
    private readonly string originDecisionId;
    private readonly string activeTravelPartyId;
    private readonly bool advanceTravelRevision;
    private readonly List<PresenceChange> presenceChanges;
    private bool committed;

    internal bool TravelRevisionChanges => advanceTravelRevision;
    internal DailyV1NpcRuntimeOwners Npc => npc;
    internal bool IsCommitted => committed;
    internal bool IsNoOp => !HasTravelStateDifference() && presenceChanges.Count == 0;
    internal string CurrentLocationRuntimeId => currentLocationId;
    internal string CurrentCityRuntimeId => currentCityId;
    internal string DestinationLocationRuntimeId => destinationLocationId;
    internal string DestinationCityRuntimeId => destinationCityId;
    internal string RouteRuntimeId => routeId;
    internal int TravelDaysTotal => travelDaysTotal;
    internal string OriginDecisionId => originDecisionId;

    private DailyV1SelectedTravelTransitionPlan(
        DailyV1NpcRuntimeOwners npc,
        string currentLocationId,
        string currentCityId,
        string destinationLocationId,
        string destinationCityId,
        int travelDaysRemaining,
        int travelDaysTotal,
        string routeId,
        bool travelStartedToday,
        string originDecisionId,
        string activeTravelPartyId,
        bool advanceTravelRevision,
        List<PresenceChange> presenceChanges)
    {
        this.npc = npc;
        travel = npc.Travel;
        expectedCurrentLocationId = travel.CurrentLocationRuntimeId;
        expectedCurrentCityId = travel.CurrentCityRuntimeId;
        expectedDestinationLocationId = travel.DestinationLocationRuntimeId;
        expectedDestinationCityId = travel.DestinationCityRuntimeId;
        expectedTravelDaysRemaining = travel.TravelDaysRemaining;
        expectedTravelDaysTotal = travel.TravelDaysTotal;
        expectedRouteId = travel.TravelRouteRuntimeId;
        expectedTravelStartedToday = travel.TravelStartedToday;
        expectedOriginDecisionId = travel.TravelOriginDecisionId;
        expectedActiveTravelPartyId = travel.ActiveTravelPartyId;
        expectedTravelRevision = travel.Revision;
        this.currentLocationId = currentLocationId;
        this.currentCityId = currentCityId;
        this.destinationLocationId = destinationLocationId;
        this.destinationCityId = destinationCityId;
        this.travelDaysRemaining = travelDaysRemaining;
        this.travelDaysTotal = travelDaysTotal;
        this.routeId = routeId;
        this.travelStartedToday = travelStartedToday;
        this.originDecisionId = originDecisionId;
        this.activeTravelPartyId = activeTravelPartyId;
        this.advanceTravelRevision = advanceTravelRevision;
        this.presenceChanges = presenceChanges;
    }

    internal static bool TrySetCurrentPresence(
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners currentCity,
        string currentLocationId,
        DailyV1CityRuntimeOwners nextCity,
        string nextLocationId,
        out DailyV1SelectedTravelTransitionPlan plan)
    {
        plan = null;
        if (!IsValidNpc(npc)
            || npc.Travel.IsTraveling
            || !IsCompatibleCity(npc.Definitions, currentCity)
            || !MatchesCurrentProjection(npc.Definitions, npc.Travel, currentCity, currentLocationId)
            || !IsCompatibleCity(npc.Definitions, nextCity)
            || (nextCity != null && string.IsNullOrWhiteSpace(nextLocationId)))
            return false;

        List<PresenceChange> changes = new List<PresenceChange>(2);
        if (!ReferenceEquals(currentCity, nextCity)
            && currentCity != null
            && currentCity.NpcPresence.Contains(npc.RuntimeId))
            changes.Add(new PresenceChange(currentCity, false));
        if (nextCity != null && !nextCity.NpcPresence.Contains(npc.RuntimeId))
            changes.Add(new PresenceChange(nextCity, true));

        plan = Create(npc,
            nextLocationId,
            nextCity?.RuntimeId,
            npc.Travel.DestinationLocationRuntimeId,
            npc.Travel.DestinationCityRuntimeId,
            npc.Travel.TravelDaysRemaining,
            npc.Travel.TravelDaysTotal,
            npc.Travel.TravelRouteRuntimeId,
            npc.Travel.TravelStartedToday,
            npc.Travel.TravelOriginDecisionId,
            npc.Travel.ActiveTravelPartyId,
            advanceTravelRevision: false,
            presenceChanges: changes);
        return true;
    }

    internal static bool TryStartTravel(
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners currentCity,
        string currentLocationId,
        string destinationLocationId,
        DailyV1CityRuntimeOwners destinationCity,
        int requestedTravelDays,
        string originDecisionId,
        string routeId,
        out DailyV1SelectedTravelTransitionPlan plan)
    {
        plan = null;
        if (!IsValidNpc(npc)
            || npc.Lifecycle.LifeState != NpcLifeState.Alive
            || string.IsNullOrWhiteSpace(destinationLocationId)
            || string.IsNullOrWhiteSpace(currentLocationId)
            || npc.Travel.IsTraveling
            || !string.IsNullOrWhiteSpace(npc.Travel.ActiveTravelPartyId)
            || !IsCompatibleCity(npc.Definitions, currentCity)
            || !MatchesCurrentProjection(npc.Definitions, npc.Travel, currentCity, currentLocationId)
            || !IsCompatibleCity(npc.Definitions, destinationCity))
            return false;

        List<PresenceChange> changes = new List<PresenceChange>(1);
        if (currentCity != null && currentCity.NpcPresence.Contains(npc.RuntimeId))
            changes.Add(new PresenceChange(currentCity, false));

        int days = Math.Max(1, requestedTravelDays);
        plan = Create(npc,
            null,
            null,
            destinationLocationId,
            destinationCity?.RuntimeId,
            days,
            days,
            Normalize(routeId),
            true,
            Normalize(originDecisionId),
            npc.Travel.ActiveTravelPartyId,
            advanceTravelRevision: true,
            presenceChanges: changes);
        return true;
    }

    internal static bool TrySetActiveTravelParty(
        DailyV1NpcRuntimeOwners npc,
        string travelPartyId,
        out DailyV1SelectedTravelTransitionPlan plan)
    {
        plan = null;
        if (!IsValidNpc(npc)) return false;
        string next = Normalize(travelPartyId);
        bool changed = !string.Equals(npc.Travel.ActiveTravelPartyId, next, StringComparison.Ordinal);
        plan = Create(npc,
            npc.Travel.CurrentLocationRuntimeId,
            npc.Travel.CurrentCityRuntimeId,
            npc.Travel.DestinationLocationRuntimeId,
            npc.Travel.DestinationCityRuntimeId,
            npc.Travel.TravelDaysRemaining,
            npc.Travel.TravelDaysTotal,
            npc.Travel.TravelRouteRuntimeId,
            npc.Travel.TravelStartedToday,
            npc.Travel.TravelOriginDecisionId,
            next,
            changed,
            new List<PresenceChange>());
        return true;
    }

    internal static bool TryClearTravelStartedToday(
        DailyV1NpcRuntimeOwners npc,
        out DailyV1SelectedTravelTransitionPlan plan)
    {
        plan = null;
        if (!IsValidNpc(npc)) return false;
        bool changed = npc.Travel.TravelStartedToday;
        plan = Create(npc,
            npc.Travel.CurrentLocationRuntimeId,
            npc.Travel.CurrentCityRuntimeId,
            npc.Travel.DestinationLocationRuntimeId,
            npc.Travel.DestinationCityRuntimeId,
            npc.Travel.TravelDaysRemaining,
            npc.Travel.TravelDaysTotal,
            npc.Travel.TravelRouteRuntimeId,
            false,
            npc.Travel.TravelOriginDecisionId,
            npc.Travel.ActiveTravelPartyId,
            changed,
            new List<PresenceChange>());
        return true;
    }

    internal static bool TryAdvanceTravelDay(
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners destinationCity,
        out DailyV1SelectedTravelTransitionPlan plan,
        out bool arriving)
    {
        plan = null;
        arriving = false;
        bool willArrive = npc?.Travel != null && npc.Travel.TravelDaysRemaining <= 1;
        if (!IsValidNpc(npc)
            || npc.Lifecycle.LifeState != NpcLifeState.Alive
            || !npc.Travel.IsTraveling
            || !IsCompatibleCity(npc.Definitions, destinationCity)
            || !string.Equals(npc.Travel.DestinationCityRuntimeId, destinationCity?.RuntimeId, StringComparison.Ordinal)
            || (willArrive && destinationCity != null && !destinationCity.NpcPresence.CanApplyRevisionIncrements(1L)
                && !destinationCity.NpcPresence.Contains(npc.RuntimeId)))
            return false;

        arriving = willArrive;
        List<PresenceChange> changes = new List<PresenceChange>(1);
        if (arriving && destinationCity != null && !destinationCity.NpcPresence.Contains(npc.RuntimeId))
            changes.Add(new PresenceChange(destinationCity, true));

        int remaining = Math.Max(0, npc.Travel.TravelDaysRemaining - 1);
        plan = Create(npc,
            arriving ? npc.Travel.DestinationLocationRuntimeId : npc.Travel.CurrentLocationRuntimeId,
            arriving ? npc.Travel.DestinationCityRuntimeId : npc.Travel.CurrentCityRuntimeId,
            arriving ? null : npc.Travel.DestinationLocationRuntimeId,
            arriving ? null : npc.Travel.DestinationCityRuntimeId,
            remaining,
            arriving ? 0 : npc.Travel.TravelDaysTotal,
            arriving ? null : npc.Travel.TravelRouteRuntimeId,
            npc.Travel.TravelStartedToday,
            arriving ? null : npc.Travel.TravelOriginDecisionId,
            npc.Travel.ActiveTravelPartyId,
            advanceTravelRevision: true,
            presenceChanges: changes);
        return true;
    }

    internal static bool TryCancelTravel(
        DailyV1NpcRuntimeOwners npc,
        string originLocationId,
        DailyV1CityRuntimeOwners originCity,
        out DailyV1SelectedTravelTransitionPlan plan)
    {
        plan = null;
        if (!IsValidNpc(npc)
            || !npc.Travel.IsTraveling
            || !IsCompatibleCity(npc.Definitions, originCity)
            || (originCity != null && string.IsNullOrWhiteSpace(originLocationId)))
            return false;

        List<PresenceChange> changes = new List<PresenceChange>(1);
        if (originCity != null && !originCity.NpcPresence.Contains(npc.RuntimeId))
            changes.Add(new PresenceChange(originCity, true));

        plan = Create(npc,
            originLocationId,
            originCity?.RuntimeId,
            null,
            null,
            0,
            0,
            null,
            false,
            null,
            null,
            advanceTravelRevision: true,
            presenceChanges: changes);
        return true;
    }

    internal static bool TryClearCurrentPresence(
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners city,
        out DailyV1SelectedTravelTransitionPlan plan)
    {
        plan = null;
        if (!IsValidNpc(npc)
            || city == null
            || !ReferenceEquals(npc.Definitions, city.Definitions)
            || !string.Equals(npc.Travel.CurrentCityRuntimeId, city.RuntimeId, StringComparison.Ordinal))
            return false;

        List<PresenceChange> changes = new List<PresenceChange>(1);
        if (city.NpcPresence.Contains(npc.RuntimeId))
            changes.Add(new PresenceChange(city, false));

        plan = Create(npc,
            null,
            null,
            npc.Travel.DestinationLocationRuntimeId,
            npc.Travel.DestinationCityRuntimeId,
            npc.Travel.TravelDaysRemaining,
            npc.Travel.TravelDaysTotal,
            npc.Travel.TravelRouteRuntimeId,
            npc.Travel.TravelStartedToday,
            npc.Travel.TravelOriginDecisionId,
            npc.Travel.ActiveTravelPartyId,
            advanceTravelRevision: false,
            presenceChanges: changes);
        return true;
    }

    internal IReadOnlyList<DailyV1CityRuntimeOwners> ChangedCityOwners
    {
        get
        {
            List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>(presenceChanges.Count);
            foreach (PresenceChange change in presenceChanges) cities.Add(change.City);
            return cities.AsReadOnly();
        }
    }

    internal bool IsPreparedFor(
        DailyV1NpcRuntimeOwners candidate,
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (!ReferenceEquals(npc, candidate)
            || graph == null
            || !ReferenceEquals(candidate?.Definitions, graph.Definitions)
            || !graph.ContainsCityRuntimeLocation(expectedCurrentCityId, expectedCurrentLocationId)
            || !graph.ContainsCityRuntimeLocation(expectedDestinationCityId, expectedDestinationLocationId)
            || !graph.ContainsCityRuntimeLocation(currentCityId, currentLocationId)
            || !graph.ContainsCityRuntimeLocation(destinationCityId, destinationLocationId))
            return false;

        foreach (PresenceChange change in presenceChanges)
            if (change == null || !graph.ContainsExactCityOwner(change.City)) return false;
        return true;
    }

    internal bool TryGetPresenceChange(DailyV1CityRuntimeOwners city, out bool add)
    {
        add = false;
        if (city == null) return false;
        foreach (PresenceChange change in presenceChanges)
        {
            if (!ReferenceEquals(change.City, city)) continue;
            add = change.Add;
            return true;
        }
        return false;
    }

    internal bool TryCommit()
    {
        return TryCommitAfterPreparedPresenceAdds(null, 0L);
    }

    internal bool CanCommit() => !committed && IsCurrentAndAdmissible(null, 0L);

    /// <summary>
    /// Preflights and commits a plan after earlier members in the same
    /// serialized TravelParty arrival have added their presence rows to the
    /// shared destination owner. The only revision difference admitted here
    /// is the exact count of those already-preflighted additions.
    /// </summary>
    /// <summary>Commits a party member after earlier prepared members have
    /// added their rows to the same serialized destination presence owner.</summary>
    internal bool TryCommitAfterPreparedPresenceAdds(
        DailyV1CityNpcPresenceOwner sharedPresenceOwner,
        long earlierAdds)
    {
        if (committed
            || (earlierAdds > 0L
                && (!HasPresenceAdditionFor(sharedPresenceOwner)
                    || !IsCurrentAndAdmissible(sharedPresenceOwner, earlierAdds)))
            || (earlierAdds == 0L && (sharedPresenceOwner != null || !IsCurrentAndAdmissible(null, 0L)))
            || earlierAdds < 0L)
            return false;

        if (IsNoOp)
        {
            committed = true;
            return true;
        }

        // All identities, source values, and revision capacity are checked as
        // one preflight above. The selected runtime is owner-thread serialized,
        // so these owner writes cannot become stale between preflight and commit.
        foreach (PresenceChange change in presenceChanges)
        {
            bool applied = change.Add
                ? change.City.NpcPresence.TryAdd(npc.RuntimeId)
                : change.City.NpcPresence.TryRemove(npc.RuntimeId);
            if (!applied)
                throw new InvalidOperationException("A preflighted selected City presence transition could not commit.");
        }

        if (!travel.TryCommitState(
                currentLocationId,
                currentCityId,
                destinationLocationId,
                destinationCityId,
                travelDaysRemaining,
                travelDaysTotal,
                routeId,
                travelStartedToday,
                originDecisionId,
                activeTravelPartyId,
                advanceTravelRevision))
            throw new InvalidOperationException("A preflighted selected NPC travel transition could not commit.");

        committed = true;
        return true;
    }

    /// <summary>
    /// Runs the host's admission check immediately before the prepared Core
    /// owner install. A rejected admission leaves every City and NPC owner
    /// untouched, and a successful admission still goes through the same
    /// stale-state and revision-capacity checks as a direct commit.
    /// </summary>
    internal bool TryCommit(Func<bool> preCommitAdmission)
    {
        if (preCommitAdmission != null && !preCommitAdmission()) return false;
        return TryCommit();
    }

    private bool IsCurrentAndAdmissible(
        DailyV1CityNpcPresenceOwner sharedPresenceOwner,
        long earlierAdds)
    {
        if (!IsValidNpc(npc)
            || !string.Equals(travel.CurrentLocationRuntimeId, expectedCurrentLocationId, StringComparison.Ordinal)
            || !string.Equals(travel.CurrentCityRuntimeId, expectedCurrentCityId, StringComparison.Ordinal)
            || !string.Equals(travel.DestinationLocationRuntimeId, expectedDestinationLocationId, StringComparison.Ordinal)
            || !string.Equals(travel.DestinationCityRuntimeId, expectedDestinationCityId, StringComparison.Ordinal)
            || travel.TravelDaysRemaining != expectedTravelDaysRemaining
            || travel.TravelDaysTotal != expectedTravelDaysTotal
            || !string.Equals(travel.TravelRouteRuntimeId, expectedRouteId, StringComparison.Ordinal)
            || travel.TravelStartedToday != expectedTravelStartedToday
            || !string.Equals(travel.TravelOriginDecisionId, expectedOriginDecisionId, StringComparison.Ordinal)
            || !string.Equals(travel.ActiveTravelPartyId, expectedActiveTravelPartyId, StringComparison.Ordinal)
            || travel.Revision != expectedTravelRevision
            || (advanceTravelRevision && !travel.CanAdvanceRevision(1)))
            return false;

        foreach (PresenceChange change in presenceChanges)
        {
            DailyV1CityNpcPresenceOwner owner = change.City?.NpcPresence;
            long expectedRevision = change.ExpectedRevision;
            if (ReferenceEquals(owner, sharedPresenceOwner))
            {
                if (!change.Add || earlierAdds <= 0L || expectedRevision > long.MaxValue - earlierAdds)
                    return false;
                expectedRevision += earlierAdds;
            }
            if (owner == null
                || owner.Revision != expectedRevision
                || owner.Contains(npc.RuntimeId) == change.Add
                || !owner.CanApplyRevisionIncrements(1L))
                return false;
        }
        return true;
    }

    private bool HasPresenceAdditionFor(DailyV1CityNpcPresenceOwner owner)
    {
        if (owner == null) return false;
        foreach (PresenceChange change in presenceChanges)
            if (ReferenceEquals(change.City?.NpcPresence, owner) && change.Add)
                return true;
        return false;
    }

    private bool HasTravelStateDifference() =>
        !string.Equals(expectedCurrentLocationId, currentLocationId, StringComparison.Ordinal)
        || !string.Equals(expectedCurrentCityId, currentCityId, StringComparison.Ordinal)
        || !string.Equals(expectedDestinationLocationId, destinationLocationId, StringComparison.Ordinal)
        || !string.Equals(expectedDestinationCityId, destinationCityId, StringComparison.Ordinal)
        || expectedTravelDaysRemaining != travelDaysRemaining
        || expectedTravelDaysTotal != travelDaysTotal
        || !string.Equals(expectedRouteId, routeId, StringComparison.Ordinal)
        || expectedTravelStartedToday != travelStartedToday
        || !string.Equals(expectedOriginDecisionId, originDecisionId, StringComparison.Ordinal)
        || !string.Equals(expectedActiveTravelPartyId, activeTravelPartyId, StringComparison.Ordinal);

    private static DailyV1SelectedTravelTransitionPlan Create(
        DailyV1NpcRuntimeOwners npc,
        string currentLocationId,
        string currentCityId,
        string destinationLocationId,
        string destinationCityId,
        int travelDaysRemaining,
        int travelDaysTotal,
        string routeId,
        bool travelStartedToday,
        string originDecisionId,
        string activeTravelPartyId,
        bool advanceTravelRevision,
        List<PresenceChange> presenceChanges) =>
        new DailyV1SelectedTravelTransitionPlan(
            npc,
            currentLocationId,
            currentCityId,
            destinationLocationId,
            destinationCityId,
            travelDaysRemaining,
            travelDaysTotal,
            routeId,
            travelStartedToday,
            originDecisionId,
            activeTravelPartyId,
            advanceTravelRevision,
            presenceChanges);

    private static bool IsValidNpc(DailyV1NpcRuntimeOwners npc) =>
        npc != null
        && npc.Definitions != null
        && npc.Travel != null
        && string.Equals(npc.RuntimeId, npc.Travel.NpcRuntimeId, StringComparison.Ordinal);

    private static bool MatchesCurrentProjection(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcTravelOwner travel,
        DailyV1CityRuntimeOwners currentCity,
        string currentLocationId) =>
        currentCity != null
            ? ReferenceEquals(definitions, currentCity.Definitions)
                && string.Equals(travel.CurrentCityRuntimeId, currentCity.RuntimeId, StringComparison.Ordinal)
                && string.Equals(travel.CurrentLocationRuntimeId, currentLocationId, StringComparison.Ordinal)
            : travel.CurrentCityRuntimeId == null
                && string.Equals(travel.CurrentLocationRuntimeId, currentLocationId, StringComparison.Ordinal);

    private static bool IsCompatibleCity(
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityRuntimeOwners city) =>
        city == null || (ReferenceEquals(definitions, city.Definitions)
            && !string.IsNullOrWhiteSpace(city.RuntimeId)
            && city.Definition != null
            && definitions.Cities.TryGetValue(city.Definition.DefinitionId, out DailyV1CityDefinition admitted)
            && ReferenceEquals(city.Definition, admitted)
            && city.NpcPresence != null
            && string.Equals(city.RuntimeId, city.NpcPresence.CityRuntimeId, StringComparison.Ordinal));

    private static string Normalize(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
