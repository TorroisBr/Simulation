using NUnit.Framework;
using System;
using System.Linq;

public sealed class DailyV1TravelActionPlanningTests
{
    [Test]
    public void SelectedTravelPlanUtilityMatchesTradeUrgencyAndClampRules()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition actor = definitions.Npcs["MARCUS"];
        DailyV1ActionDefinition action = FirstAction(definitions);

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, actor, action, 40f, true, true, 2, 12f,
            out float tradeUtility), Is.True);
        Assert.That(tradeUtility, Is.EqualTo(60f));

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, actor, action, 90f, true, true, 12, 15f,
            out float cappedUtility), Is.True);
        Assert.That(cappedUtility, Is.EqualTo(100f),
            "The plan-plus-trade urgency is clamped before maxing against the candidate utility.");

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, actor, action, -20f, true, false, 0, 3f,
            out float floorUtility), Is.True);
        Assert.That(floorUtility, Is.EqualTo(3f));

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, actor, action, 45f, false, true, 9, 12f,
            out float nonTradeUtility), Is.True);
        Assert.That(nonTradeUtility, Is.EqualTo(45f),
            "Merchant urgency applies only to a Trade travel plan that matches the current merchant plan.");
    }

    [Test]
    public void SelectedTravelPlanUtilityRejectsForeignActorAndActionNodes()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition actor = definitions.Npcs["MARCUS"];
        DailyV1ActionDefinition action = FirstAction(definitions);

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, foreign.Npcs[actor.DefinitionId], action, 40f, true, true, 2, 12f,
            out float foreignActorUtility), Is.False);
        Assert.That(foreignActorUtility, Is.Zero);

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, actor, foreign.Actions[action.DefinitionId], 40f, true, true, 2, 12f,
            out float foreignActionUtility), Is.False);
        Assert.That(foreignActionUtility, Is.Zero);
    }

    [Test]
    public void ExistingSelectedTravelCandidateUsesCoreRouteCostAndPreservesPlanIdentity()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, DailyV1ActionDefinition travelAction) =
            CreateTravelCandidateFixture();
        Assert.That(actor.TravelPlan.TryInstallExact(
            destination.LocationRuntimeId, destination.RuntimeId,
            NpcTravelReason.Trade, 40f, 0f, "decision/travel-origin", 1L), Is.True);
        Assert.That(graph.TryPlanSelectedSoloTravel(actor, actor.Travel.CurrentLocationRuntimeId,
            destination.LocationRuntimeId, destination.RuntimeId,
            out SpatialRouteRuntime route, out float cost), Is.True);
        Assert.That(actor.MoneyAccount.Balance, Is.GreaterThanOrEqualTo(cost));
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(travelAction, 5f);

        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedExistingTravelCandidate(
            graph, actor, candidate, partyStore: null), Is.True);

        Assert.That(candidate.Utility, Is.EqualTo(40f));
        Assert.That(candidate.ExpectedTravelCost, Is.EqualTo(30f));
        Assert.That(candidate.PreparedPayload, Is.Not.Null);
        Assert.That(candidate.PreparedPayload.Action, Is.SameAs(travelAction));
        Assert.That(candidate.PreparedPayload.TargetCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(candidate.PreparedPayload.ExpectedUnitPrice, Is.EqualTo(cost),
            "The selected Unity projection stores travel cost in the payload's shared expected-value field.");
        Assert.That(candidate.PreparedPayload.TravelReason, Is.EqualTo(NpcTravelReason.Trade));
        Assert.That(candidate.PreparedPayload.OriginDecisionId, Is.EqualTo("decision/travel-origin"));
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(1L), "Candidate preparation is side-effect free.");
    }

    [Test]
    public void ExistingSelectedTravelCandidateRejectsUnaffordableOrPartyManagedPlan()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, DailyV1ActionDefinition travelAction) =
            CreateTravelCandidateFixture();
        Assert.That(actor.TravelPlan.TryInstallExact(
            destination.LocationRuntimeId, destination.RuntimeId,
            NpcTravelReason.Trade, 40f, 0f, "decision/travel-origin", 1L), Is.True);
        Assert.That(actor.MoneyAccount.TryDebit(actor.MoneyAccount.Balance - 1f), Is.True);
        DailyV1NpcActionCandidate unaffordable = new DailyV1NpcActionCandidate(travelAction, 5f);

        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedExistingTravelCandidate(
            graph, actor, unaffordable, partyStore: null), Is.True);
        Assert.That(unaffordable.Utility, Is.Zero);
        Assert.That(unaffordable.PreparedPayload, Is.Null);

        Assert.That(actor.Travel.TryInstallState(
            actor.Travel.CurrentLocationRuntimeId, actor.Travel.CurrentCityRuntimeId,
            null, null, 0, 0, null, false, null, "party-active", actor.Travel.Revision + 1L), Is.True);
        DailyV1NpcActionCandidate partyManaged = new DailyV1NpcActionCandidate(travelAction, 5f);
        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedExistingTravelCandidate(
            graph, actor, partyManaged, partyStore: null), Is.True);
        Assert.That(partyManaged.Utility, Is.Zero);
        Assert.That(partyManaged.PreparedPayload, Is.Null);
    }

    [Test]
    public void ExistingSelectedTravelCandidateDefersWhenNoPlanExists()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, DailyV1ActionDefinition travelAction) =
            CreateTravelCandidateFixture();
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(travelAction, 5f);

        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedExistingTravelCandidate(
            graph, actor, candidate, partyStore: null), Is.False);

        Assert.That(candidate.Utility, Is.EqualTo(5f));
        Assert.That(candidate.ExpectedTravelCost, Is.Zero);
        Assert.That(candidate.PreparedPayload, Is.Null);
    }

    private static (DailyV1SelectedRuntimeOwnerGraph Graph, DailyV1NpcRuntimeOwners Actor,
        DailyV1CityRuntimeOwners Destination, DailyV1ActionDefinition TravelAction)
        CreateTravelCandidateFixture()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        SpatialNetworkOwner spatial = new SpatialNetworkOwner();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions, spatial);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        DailyV1CityRuntimeOwners[] cities = builder.Cities.ToArray();
        for (int i = 0; i < cities.Length; i++)
        {
            Assert.That(spatial.TryGetLocation(cities[i].LocationRuntimeId,
                out SpatialLocationRuntime origin), Is.True);
            for (int j = i + 1; j < cities.Length; j++)
            {
                Assert.That(spatial.TryGetLocation(cities[j].LocationRuntimeId,
                    out SpatialLocationRuntime targetLocation), Is.True);
                string routeBase = "route/travel-candidate/" + cities[i].RuntimeId + "/" + cities[j].RuntimeId;
                Assert.That(spatial.TryRegisterRoute(new SpatialRouteRuntime(
                    routeBase, origin, targetLocation, 3), () => true), Is.True);
                Assert.That(spatial.TryRegisterRoute(new SpatialRouteRuntime(
                    routeBase + "/return", targetLocation, origin, 3), () => true), Is.True);
            }
        }
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityId => builder.ResolveUniqueStartingCity(cityId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners actor = graph.Npcs.Single(npc => npc.Definition.DefinitionId == "JORGE");
        DailyV1CityRuntimeOwners destination = graph.Cities.Single(city =>
            city.RuntimeId != actor.Travel.CurrentCityRuntimeId);
        DailyV1ActionDefinition travel = definitions.Actions.Values.Single(action =>
            action.Values.actionType?.Value == DailyV1NpcDefinitionRules.TravelActionTypeValue);
        return (graph, actor, destination, travel);
    }

    private static DailyV1ActionDefinition FirstAction(DailyV1EffectiveDefinitions definitions)
    {
        foreach (DailyV1ActionDefinition action in definitions.Actions.Values)
            return action;
        Assert.Fail("The selected fixture must contain an action node.");
        return null;
    }
}
