using NUnit.Framework;
using System;
using System.Collections.Generic;
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

    [Test]
    public void SelectedFleeCityRevalidatesCoreOwnersWithoutStartingTravelOrCharging()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, _) = CreateTravelCandidateFixture();
        DailyV1ActionDefinition fleeAction = actor.Definitions.Actions.Values.Single(action =>
            action.Values.actionType?.Value == DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            actor.Definitions, fleeAction, null, destination.RuntimeId, null,
            0, 0f, 1f, NpcTravelReason.None, 0f,
            "decision/flee-city", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, fleeAction, payload, null), Is.True);

        string originCityId = actor.Travel.CurrentCityRuntimeId;
        string originLocationId = actor.Travel.CurrentLocationRuntimeId;
        float balanceBefore = actor.MoneyAccount.Balance;
        long planRevisionBefore = actor.TravelPlan.Revision;
        Assert.That(DailyV1SelectedActionExecutionStage.TryPrepareSelectedFleeCityPlan(
            graph, actor, fleeAction, null,
            out DailyV1SelectedActionExecutionStage.SelectedFleeCityPlan plan), Is.True);

        Assert.That(plan.Destination, Is.SameAs(destination));
        Assert.That(plan.Route.Origin.RuntimeId, Is.EqualTo(originLocationId));
        Assert.That(plan.Route.Destination.RuntimeId, Is.EqualTo(destination.LocationRuntimeId));
        Assert.That(plan.TravelCost, Is.EqualTo(Math.Max(1, plan.Route.TravelDays)
            * (actor.Definitions.EffectiveConfiguration?.Travel?.TravelCostPerDay ?? 0f)));
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balanceBefore));
        Assert.That(actor.Travel.CurrentCityRuntimeId, Is.EqualTo(originCityId));
        Assert.That(actor.Travel.CurrentLocationRuntimeId, Is.EqualTo(originLocationId));
        Assert.That(actor.Travel.IsTraveling, Is.False);
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(planRevisionBefore));
    }

    [Test]
    public void SelectedFleeCityRejectsArrestedPartyManagedAndUnaffordableActors()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, _) = CreateTravelCandidateFixture();
        DailyV1ActionDefinition fleeAction = actor.Definitions.Actions.Values.Single(action =>
            action.Values.actionType?.Value == DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            actor.Definitions, fleeAction, null, destination.RuntimeId, null,
            0, 0f, 1f, NpcTravelReason.None, 0f,
            "decision/flee-city", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, fleeAction, payload, null), Is.True);

        DailyV1EffectiveDefinitions definitions = actor.Definitions;
        Assert.That(definitions.Statuses.TryGetValue(
            definitions.Configuration.arrestedStatus,
            out DailyV1StatusDefinition arrestedStatus), Is.True);
        Assert.That(actor.Status.TryAdd(arrestedStatus), Is.True);
        Assert.That(DailyV1SelectedActionExecutionStage.TryPrepareSelectedFleeCityPlan(
            graph, actor, fleeAction, null, out _), Is.False);
        Assert.That(actor.Status.TryRemove(arrestedStatus), Is.True);

        Assert.That(actor.Travel.TryInstallState(
            actor.Travel.CurrentLocationRuntimeId,
            actor.Travel.CurrentCityRuntimeId,
            null, null, 0, 0, null, false, null,
            "party/active", actor.Travel.Revision + 1L), Is.True);
        Assert.That(DailyV1SelectedActionExecutionStage.TryPrepareSelectedFleeCityPlan(
            graph, actor, fleeAction, null, out _), Is.False);

        Assert.That(actor.Travel.TryInstallState(
            actor.Travel.CurrentLocationRuntimeId,
            actor.Travel.CurrentCityRuntimeId,
            null, null, 0, 0, null, false, null,
            null, actor.Travel.Revision + 1L), Is.True);
        Assert.That(actor.MoneyAccount.TryDebit(actor.MoneyAccount.Balance), Is.True);
        Assert.That(DailyV1SelectedActionExecutionStage.TryPrepareSelectedFleeCityPlan(
            graph, actor, fleeAction, null, out _), Is.False);
    }

    [Test]
    public void SelectedFleeDispatchHandlesMalformedPayloadWithoutProviderFallback()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, _) = CreateTravelCandidateFixture();
        DailyV1ActionDefinition fleeAction = FindFleeCityAction(actor.Definitions);
        DailyV1NpcActionPayloadOwner malformedPayload = CreateFleeCityPayload(
            actor, fleeAction, destination, targetNpcRuntimeId: "npc/not-valid-for-flee-city");
        Assert.That(graph.TryCommitCurrentActionSlot(actor, fleeAction, malformedPayload, null), Is.True);
        FixedActionRandom random = new FixedActionRandom(0f);
        RecordingFleeDispatchBoundary boundary = new RecordingFleeDispatchBoundary(
            new RecordingFleeActionBoundary());

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
            graph, actor, random, 0L, boundary,
            out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.FleeCity));
        Assert.That(result.ActionRollSucceeded, Is.False);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(boundary.FleeBoundaryResolutionCalls, Is.Zero);
        Assert.That(random.Calls, Is.Zero);
    }

    [Test]
    public void SelectedFleeDispatchRejectsMissingBoundaryWithoutProviderFallback()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, _) = CreateTravelCandidateFixture();
        DailyV1ActionDefinition fleeAction = FindFleeCityAction(actor.Definitions);
        DailyV1NpcActionPayloadOwner payload = CreateFleeCityPayload(
            actor, fleeAction, destination, targetNpcRuntimeId: null);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, fleeAction, payload, null), Is.True);
        FixedActionRandom random = new FixedActionRandom(0f);
        RecordingFleeDispatchBoundary boundary = new RecordingFleeDispatchBoundary(null);

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
            graph, actor, random, 0L, boundary,
            out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.FleeCity));
        Assert.That(result.ActionRollSucceeded, Is.False);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(boundary.FleeBoundaryResolutionCalls, Is.EqualTo(1));
        Assert.That(random.Calls, Is.Zero);
    }

    [Test]
    public void SelectedFleeDispatchUsesCorePlanAndOperationBoundary()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, _) = CreateTravelCandidateFixture();
        DailyV1ActionDefinition fleeAction = FindFleeCityAction(actor.Definitions);
        DailyV1NpcActionPayloadOwner payload = CreateFleeCityPayload(
            actor, fleeAction, destination, targetNpcRuntimeId: null);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, fleeAction, payload, null), Is.True);
        RecordingFleeActionBoundary fleeBoundary = new RecordingFleeActionBoundary();
        RecordingFleeDispatchBoundary boundary = new RecordingFleeDispatchBoundary(fleeBoundary);
        FixedActionRandom random = new FixedActionRandom(0f);
        float balanceBefore = actor.MoneyAccount.Balance;
        long planRevisionBefore = actor.TravelPlan.Revision;

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
            graph, actor, random, 0L, boundary,
            out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.FleeCity));
        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.FleeCity.DestinationCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(result.FleeCity.TravelCost, Is.GreaterThan(0f));
        Assert.That(fleeBoundary.OperationCalls, Is.EqualTo(1));
        Assert.That(fleeBoundary.CommitCalls, Is.EqualTo(1));
        Assert.That(fleeBoundary.Actor, Is.SameAs(actor));
        Assert.That(fleeBoundary.Destination, Is.SameAs(destination));
        Assert.That(fleeBoundary.OriginDecisionId, Is.EqualTo("decision/flee-city"));
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balanceBefore));
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(planRevisionBefore));
    }

    private static DailyV1ActionDefinition FindFleeCityAction(DailyV1EffectiveDefinitions definitions) =>
        definitions.Actions.Values.Single(action =>
            action.Values.actionType?.Value == DailyV1NpcDefinitionRules.FleeCityActionTypeValue);

    private static DailyV1NpcActionPayloadOwner CreateFleeCityPayload(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1CityRuntimeOwners destination,
        string targetNpcRuntimeId)
    {
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            actor.Definitions, action, targetNpcRuntimeId, destination.RuntimeId, null,
            0, 0f, 1f, NpcTravelReason.None, 0f,
            "decision/flee-city", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        return payload;
    }

    private sealed class FixedActionRandom : IAuthoritativeRandomSource
    {
        private readonly float value;
        internal int Calls { get; private set; }
        internal FixedActionRandom(float value) => this.value = value;
        public float NextUnit(string streamKey, long drawIndex = 0L)
        { Calls++; return value; }
        public DeterministicRandomStream CreateStream(string streamKey) => null;
    }

    private sealed class RecordingFleeDispatchBoundary : IDailyV1SelectedActionDispatchBoundary
    {
        private readonly DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary fleeBoundary;
        internal int FleeBoundaryResolutionCalls { get; private set; }
        internal RecordingFleeDispatchBoundary(
            DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary fleeBoundary) =>
            this.fleeBoundary = fleeBoundary;

        public bool TryGetSoloTravelBoundary(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action, out TravelPartyStore partyStore,
            out DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary boundary)
        { partyStore = null; boundary = null; return false; }
        public bool TryGetMarketBoundaries(DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedOpenMarketSaleBoundary sale,
            out IDailyV1SelectedOpenMarketPurchaseBoundary purchase)
        { sale = null; purchase = null; return false; }
        public bool TryGetNpcTradeBoundary(DailyV1NpcRuntimeOwners seller,
            DailyV1NpcRuntimeOwners buyer, out IDailyV1SelectedNpcTradeBoundary boundary)
        { boundary = null; return false; }
        public bool TryGetFleeCityActionBoundary(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary boundary)
        {
            FleeBoundaryResolutionCalls++;
            boundary = fleeBoundary;
            return fleeBoundary != null;
        }
        public bool TryGetGuardArrestActionBoundary(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary boundary)
        { boundary = null; return false; }
        public bool TryGetStealActionBoundary(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary boundary)
        { boundary = null; return false; }
        public bool CanAdmitMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
        public void PublishMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) { }
        public bool CanAdmitTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
        public void PublishTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) { }
        public void CompleteCrossCityBuy(DailyV1NpcRuntimeOwners actor,
            DailyV1SelectedCrossCityBuyResult result) { }
    }

    private sealed class RecordingFleeActionBoundary
        : DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary
    {
        internal int OperationCalls { get; private set; }
        internal int CommitCalls { get; private set; }
        internal DailyV1NpcRuntimeOwners Actor { get; private set; }
        internal DailyV1CityRuntimeOwners Destination { get; private set; }
        internal string OriginDecisionId { get; private set; }
        public TravelPartyStore TravelPartyStore => null;
        public bool ExecuteWithinOperation(Func<bool> operation)
        { OperationCalls++; return operation != null && operation(); }
        public bool CommitTravelPlan(DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, float travelCost, string originDecisionId)
        {
            CommitCalls++;
            Actor = actor;
            Destination = destination;
            OriginDecisionId = originDecisionId;
            return travelCost > 0f;
        }
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
