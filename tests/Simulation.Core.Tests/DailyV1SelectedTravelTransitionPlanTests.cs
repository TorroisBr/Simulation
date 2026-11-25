using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedTravelTransitionPlanTests
{
    [Test]
    public void SoloTravelExecutionChargesBeforeTransitionAndRefundsRejectedTransition()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityRuntimeOwners origin = CreateCity(definitions, "city-origin", new[] { "npc-1" }, 1L);
        DailyV1CityRuntimeOwners destination = CreateCity(definitions, "city-destination", System.Array.Empty<string>(), 0L);
        DailyV1NpcRuntimeOwners npc = CreateNpc(definitions, "npc-1", "location-origin", origin.RuntimeId, 0L);
        Assert.That(npc.MoneyAccount.TryCredit(20f), Is.True);
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            npc, origin, "location-origin", "location-destination", destination,
            2, "decision-1", "route-1", out DailyV1SelectedTravelTransitionPlan rejectedPlan), Is.True);

        DailyV1SelectedSoloTravelResult rejected = DailyV1SelectedSoloTravelExecution.TryExecute(
            npc, rejectedPlan, 3f, () => false);
        Assert.That(rejected.Succeeded, Is.False);
        Assert.That(rejected.Failure, Is.EqualTo(DailyV1SelectedSoloTravelFailure.TransitionRejected));
        Assert.That(rejected.RefundSucceeded, Is.True);
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(20f));
        Assert.That(npc.Travel.IsTraveling, Is.False);
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.True);

        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            npc, origin, "location-origin", "location-destination", destination,
            2, "decision-1", "route-1", out DailyV1SelectedTravelTransitionPlan acceptedPlan), Is.True);
        DailyV1SelectedSoloTravelResult accepted = DailyV1SelectedSoloTravelExecution.TryExecute(
            npc, acceptedPlan, 3f, acceptedPlan.TryCommit);
        Assert.That(accepted.Succeeded, Is.True);
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(17f));
        Assert.That(npc.Travel.TravelRouteRuntimeId, Is.EqualTo("route-1"));
        Assert.That(npc.Travel.TravelOriginDecisionId, Is.EqualTo("decision-1"));
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.False);
    }

    [Test]
    public void PresenceTransitionIsSideEffectFreeUntilCommitAndPreservesNpcRevision()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityRuntimeOwners origin = CreateCity(definitions, "city-origin", new[] { "npc-1" }, 1L);
        DailyV1CityRuntimeOwners destination = CreateCity(definitions, "city-destination", System.Array.Empty<string>(), 0L);
        DailyV1NpcRuntimeOwners npc = CreateNpc(definitions, "npc-1", "location-origin", origin.RuntimeId, 4L);

        Assert.That(DailyV1SelectedTravelTransitionPlan.TrySetCurrentPresence(
            npc, origin, "location-origin", origin, "location-origin",
            out DailyV1SelectedTravelTransitionPlan noOp), Is.True);
        Assert.That(noOp.IsNoOp, Is.True);
        Assert.That(noOp.TryCommit(), Is.True);
        Assert.That(origin.NpcPresence.Revision, Is.EqualTo(1L));
        Assert.That(npc.Travel.Revision, Is.EqualTo(4L));

        Assert.That(DailyV1SelectedTravelTransitionPlan.TrySetCurrentPresence(
            npc, origin, "location-origin", destination, "location-destination",
            out DailyV1SelectedTravelTransitionPlan plan), Is.True);

        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.True);
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(origin.NpcPresence.Revision, Is.EqualTo(1L));
        Assert.That(destination.NpcPresence.Revision, Is.Zero);
        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(origin.RuntimeId));
        Assert.That(npc.Travel.Revision, Is.EqualTo(4L));

        Assert.That(plan.TryCommit(), Is.True);
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.True);
        Assert.That(origin.NpcPresence.Revision, Is.EqualTo(2L));
        Assert.That(destination.NpcPresence.Revision, Is.EqualTo(1L));
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.EqualTo("location-destination"));
        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(npc.Travel.Revision, Is.EqualTo(4L));
    }

    [Test]
    public void SameCityPresenceTransitionRepairsMissingMembership()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityRuntimeOwners city = CreateCity(definitions, "city-current", System.Array.Empty<string>(), 0L);
        DailyV1NpcRuntimeOwners npc = CreateNpc(definitions, "npc-1", "location-current", city.RuntimeId, 4L);

        Assert.That(DailyV1SelectedTravelTransitionPlan.TrySetCurrentPresence(
            npc, city, "location-current", city, "location-current",
            out DailyV1SelectedTravelTransitionPlan plan), Is.True);
        Assert.That(plan.IsNoOp, Is.False);
        Assert.That(city.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(city.NpcPresence.Revision, Is.Zero);
        Assert.That(plan.TryCommit(), Is.True);
        Assert.That(city.NpcPresence.Contains(npc.RuntimeId), Is.True);
        Assert.That(city.NpcPresence.Revision, Is.EqualTo(1L));
        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(npc.Travel.Revision, Is.EqualTo(4L));
    }

    [Test]
    public void TravelProgressAndArrivalAdvanceNpcRevisionOncePerCommittedDay()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityRuntimeOwners origin = CreateCity(definitions, "city-origin", new[] { "npc-1" }, 1L);
        DailyV1CityRuntimeOwners destination = CreateCity(definitions, "city-destination", System.Array.Empty<string>(), 0L);
        DailyV1NpcRuntimeOwners npc = CreateNpc(definitions, "npc-1", "location-origin", origin.RuntimeId, 0L);

        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            npc, origin, "location-origin", "location-destination", destination,
            requestedTravelDays: 2, originDecisionId: "decision-1", routeId: "route-1",
            out DailyV1SelectedTravelTransitionPlan start), Is.True);
        Assert.That(start.TryCommit(), Is.True);
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(npc.Travel.Revision, Is.EqualTo(1L));
        Assert.That(npc.Travel.TravelDaysRemaining, Is.EqualTo(2));

        Assert.That(DailyV1SelectedTravelTransitionPlan.TryAdvanceTravelDay(
            npc, destination, out DailyV1SelectedTravelTransitionPlan progress, out bool arriving), Is.True);
        Assert.That(arriving, Is.False);
        Assert.That(progress.TryCommit(), Is.True);
        Assert.That(npc.Travel.TravelDaysRemaining, Is.EqualTo(1));
        Assert.That(npc.Travel.Revision, Is.EqualTo(2L));
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.False);

        Assert.That(DailyV1SelectedTravelTransitionPlan.TryAdvanceTravelDay(
            npc, destination, out DailyV1SelectedTravelTransitionPlan arrival, out arriving), Is.True);
        Assert.That(arriving, Is.True);
        Assert.That(arrival.TryCommit(), Is.True);
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.True);
        Assert.That(destination.NpcPresence.Revision, Is.EqualTo(1L));
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.EqualTo("location-destination"));
        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(npc.Travel.DestinationLocationRuntimeId, Is.Null);
        Assert.That(npc.Travel.TravelDaysRemaining, Is.Zero);
        Assert.That(npc.Travel.TravelDaysTotal, Is.Zero);
        Assert.That(npc.Travel.TravelRouteRuntimeId, Is.Null);
        Assert.That(npc.Travel.TravelOriginDecisionId, Is.Null);
        Assert.That(npc.Travel.TravelStartedToday, Is.True);
        Assert.That(npc.Travel.Revision, Is.EqualTo(3L));
    }

    [Test]
    public void RejectedOrSaturatedStartLeavesTravelAndCityOwnersUnchanged()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityRuntimeOwners origin = CreateCity(
            definitions, "city-origin", new[] { "npc-1" }, long.MaxValue);
        DailyV1CityRuntimeOwners destination = CreateCity(
            definitions, "city-destination", System.Array.Empty<string>(), 0L);
        DailyV1NpcRuntimeOwners npc = CreateNpc(
            definitions, "npc-1", "location-origin", origin.RuntimeId, 0L);

        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            npc, origin, "location-origin", "location-destination", destination,
            requestedTravelDays: 3, originDecisionId: "decision-1", routeId: "route-1",
            out DailyV1SelectedTravelTransitionPlan uncommitted), Is.True);
        // A rejected P12 admission leaves a prepared plan uncommitted.
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.True);
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.EqualTo("location-origin"));
        Assert.That(npc.Travel.Revision, Is.Zero);

        Assert.That(uncommitted.TryCommit(), Is.False, "The saturated City revision must reject before either owner changes.");
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.True);
        Assert.That(origin.NpcPresence.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.EqualTo("location-origin"));
        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(origin.RuntimeId));
        Assert.That(npc.Travel.DestinationLocationRuntimeId, Is.Null);
        Assert.That(npc.Travel.TravelDaysRemaining, Is.Zero);
        Assert.That(npc.Travel.Revision, Is.Zero);

        DailyV1CityRuntimeOwners secondOrigin = CreateCity(
            definitions, "city-second-origin", new[] { "npc-2" }, 1L);
        DailyV1NpcRuntimeOwners saturatedNpc = CreateNpc(
            definitions, "npc-2", "location-second-origin", secondOrigin.RuntimeId, long.MaxValue);
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            saturatedNpc, secondOrigin, "location-second-origin", "location-destination", destination,
            requestedTravelDays: 1, originDecisionId: null, routeId: null,
            out DailyV1SelectedTravelTransitionPlan saturatedPlan), Is.True);
        Assert.That(saturatedPlan.TryCommit(), Is.False,
            "A saturated NPC revision must reject before origin membership is removed.");
        Assert.That(secondOrigin.NpcPresence.Contains(saturatedNpc.RuntimeId), Is.True);
        Assert.That(secondOrigin.NpcPresence.Revision, Is.EqualTo(1L));
        Assert.That(saturatedNpc.Travel.CurrentCityRuntimeId, Is.EqualTo(secondOrigin.RuntimeId));
        Assert.That(saturatedNpc.Travel.TravelDaysRemaining, Is.Zero);
        Assert.That(saturatedNpc.Travel.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void RejectedAdmissionLeavesPreparedCityAndNpcOwnersUntouchedAndCanRetry()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityRuntimeOwners origin = CreateCity(definitions, "city-origin", new[] { "npc-1" }, 2L);
        DailyV1CityRuntimeOwners destination = CreateCity(definitions, "city-destination", System.Array.Empty<string>(), 4L);
        DailyV1NpcRuntimeOwners npc = CreateNpc(definitions, "npc-1", "location-origin", origin.RuntimeId, 6L);
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            npc, origin, "location-origin", "location-destination", destination,
            requestedTravelDays: 2, originDecisionId: "decision-1", routeId: "route-1",
            out DailyV1SelectedTravelTransitionPlan plan), Is.True);

        int admissionCalls = 0;
        Assert.That(plan.TryCommit(() =>
        {
            admissionCalls++;
            return false;
        }), Is.False);
        Assert.That(admissionCalls, Is.EqualTo(1));
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.True);
        Assert.That(origin.NpcPresence.Revision, Is.EqualTo(2L));
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(destination.NpcPresence.Revision, Is.EqualTo(4L));
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.EqualTo("location-origin"));
        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(origin.RuntimeId));
        Assert.That(npc.Travel.DestinationLocationRuntimeId, Is.Null);
        Assert.That(npc.Travel.TravelDaysRemaining, Is.Zero);
        Assert.That(npc.Travel.Revision, Is.EqualTo(6L));

        Assert.That(plan.TryCommit(() =>
        {
            admissionCalls++;
            return true;
        }), Is.True);
        Assert.That(admissionCalls, Is.EqualTo(2));
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(origin.NpcPresence.Revision, Is.EqualTo(3L));
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(destination.NpcPresence.Revision, Is.EqualTo(4L));
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.Null);
        Assert.That(npc.Travel.DestinationLocationRuntimeId, Is.EqualTo("location-destination"));
        Assert.That(npc.Travel.TravelDaysRemaining, Is.EqualTo(2));
        Assert.That(npc.Travel.Revision, Is.EqualTo(7L));
    }

    [Test]
    public void SaturatedDestinationPresenceRevisionBlocksOnlyTheArrivalWrite()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityRuntimeOwners destination = CreateCity(
            definitions, "city-destination", System.Array.Empty<string>(), long.MaxValue);
        DailyV1NpcRuntimeOwners npc = CreateNpc(definitions, "npc-1", null, null, 0L);
        Assert.That(npc.Travel.TryInstallState(
            null, null, "location-destination", destination.RuntimeId,
            2, 2, "route-1", true, "decision-1", null, 0L), Is.True);

        Assert.That(DailyV1SelectedTravelTransitionPlan.TryAdvanceTravelDay(
            npc, destination, out DailyV1SelectedTravelTransitionPlan progress, out bool arriving), Is.True);
        Assert.That(arriving, Is.False);
        Assert.That(progress.TryCommit(), Is.True);
        Assert.That(npc.Travel.TravelDaysRemaining, Is.EqualTo(1));
        Assert.That(npc.Travel.Revision, Is.EqualTo(1L));
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(destination.NpcPresence.Revision, Is.EqualTo(long.MaxValue));

        Assert.That(DailyV1SelectedTravelTransitionPlan.TryAdvanceTravelDay(
            npc, destination, out DailyV1SelectedTravelTransitionPlan _, out bool _), Is.False);
        Assert.That(npc.Travel.TravelDaysRemaining, Is.EqualTo(1));
        Assert.That(npc.Travel.Revision, Is.EqualTo(1L));
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(destination.NpcPresence.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void TravelCancellationRestoresOriginMembershipAndStableIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityRuntimeOwners origin = CreateCity(definitions, "city-origin", new[] { "npc-1" }, 1L);
        DailyV1CityRuntimeOwners destination = CreateCity(definitions, "city-destination", System.Array.Empty<string>(), 0L);
        DailyV1NpcRuntimeOwners npc = CreateNpc(definitions, "npc-1", "location-origin", origin.RuntimeId, 0L);

        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            npc, origin, "location-origin", "location-destination", destination,
            requestedTravelDays: 3, originDecisionId: "decision-1", routeId: "route-1",
            out DailyV1SelectedTravelTransitionPlan start), Is.True);
        Assert.That(start.TryCommit(), Is.True);
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryCancelTravel(
            npc, "location-origin", origin,
            out DailyV1SelectedTravelTransitionPlan cancel), Is.True);
        Assert.That(cancel.TryCommit(), Is.True);

        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.True);
        Assert.That(origin.NpcPresence.Revision, Is.EqualTo(3L));
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.EqualTo("location-origin"));
        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(origin.RuntimeId));
        Assert.That(npc.Travel.DestinationLocationRuntimeId, Is.Null);
        Assert.That(npc.Travel.DestinationCityRuntimeId, Is.Null);
        Assert.That(npc.Travel.TravelDaysRemaining, Is.Zero);
        Assert.That(npc.Travel.TravelDaysTotal, Is.Zero);
        Assert.That(npc.Travel.TravelStartedToday, Is.False);
        Assert.That(npc.Travel.TravelOriginDecisionId, Is.Null);
        Assert.That(npc.Travel.ActiveTravelPartyId, Is.Null);
        Assert.That(npc.Travel.Revision, Is.EqualTo(2L));
    }

    private static DailyV1CityRuntimeOwners CreateCity(
        DailyV1EffectiveDefinitions definitions,
        string runtimeId,
        string[] npcRuntimeIds,
        long presenceRevision)
    {
        DailyV1CityDefinition definition = System.Linq.Enumerable.First(definitions.Cities.Values);
        string locationRuntimeId = runtimeId.StartsWith("city-", System.StringComparison.Ordinal)
            ? "location-" + runtimeId.Substring("city-".Length)
            : "location-" + runtimeId;
        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial(
            runtimeId, locationRuntimeId, definitions, definition, null,
            out DailyV1CityRuntimeOwners initial), Is.True);
        Assert.That(DailyV1CityNpcPresenceOwner.TryCreateBound(
            runtimeId, npcRuntimeIds, presenceRevision,
            out DailyV1CityNpcPresenceOwner presence), Is.True);
        Assert.That(DailyV1CityRuntimeOwners.TryCreateBound(
            runtimeId, locationRuntimeId, definitions, definition,
            initial.Population, initial.Market, initial.Counterparty, initial.PopulationEconomy,
            presence, out DailyV1CityRuntimeOwners bound), Is.True);
        return bound;
    }

    private static DailyV1NpcRuntimeOwners CreateNpc(
        DailyV1EffectiveDefinitions definitions,
        string runtimeId,
        string currentLocationId,
        string currentCityId,
        long revision)
    {
        DailyV1NpcDefinition definition = System.Linq.Enumerable.First(definitions.Npcs.Values);
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            runtimeId, definitions, definition, 0f, out DailyV1NpcRuntimeOwners npc), Is.True);
        Assert.That(npc.Travel.TryInstallState(
            currentLocationId, currentCityId, null, null,
            0, 0, null, false, null, null, revision), Is.True);
        return npc;
    }
}
