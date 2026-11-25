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
            graph, actor, candidate), Is.True);

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
            graph, actor, unaffordable), Is.True);
        Assert.That(unaffordable.Utility, Is.Zero);
        Assert.That(unaffordable.PreparedPayload, Is.Null);

        Assert.That(actor.Travel.TryInstallState(
            actor.Travel.CurrentLocationRuntimeId, actor.Travel.CurrentCityRuntimeId,
            null, null, 0, 0, null, false, null, "party-active", actor.Travel.Revision + 1L), Is.True);
        DailyV1NpcActionCandidate partyManaged = new DailyV1NpcActionCandidate(travelAction, 5f);
        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedExistingTravelCandidate(
            graph, actor, partyManaged), Is.True);
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
            graph, actor, candidate), Is.False);

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
            graph, actor, fleeAction,
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
    public void SelectedFleeCityFailsClosedWhenExactOwnerGraphHasNoTravelPartyStore()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, _) = CreateTravelCandidateFixture();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            graph.Cities, graph.Npcs, graph.SpatialNetwork, graph.JusticeRecordsOwner,
            null, out DailyV1SelectedRuntimeOwnerGraph graphWithoutTravelParties), Is.True);
        DailyV1ActionDefinition fleeAction = actor.Definitions.Actions.Values.Single(action =>
            action.Values.actionType?.Value == DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            actor.Definitions, fleeAction, null, destination.RuntimeId, null,
            0, 0f, 1f, NpcTravelReason.None, 0f,
            "decision/flee-city-no-party-store", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, fleeAction, payload, null), Is.True);

        float balanceBefore = actor.MoneyAccount.Balance;
        long travelRevisionBefore = actor.Travel.Revision;
        Assert.That(DailyV1SelectedActionExecutionStage.TryPrepareSelectedFleeCityPlan(
            graphWithoutTravelParties, actor, fleeAction, out _), Is.False);
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balanceBefore));
        Assert.That(actor.Travel.Revision, Is.EqualTo(travelRevisionBefore));
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
            graph, actor, fleeAction, out _), Is.False);
        Assert.That(actor.Status.TryRemove(arrestedStatus), Is.True);

        Assert.That(actor.Travel.TryInstallState(
            actor.Travel.CurrentLocationRuntimeId,
            actor.Travel.CurrentCityRuntimeId,
            null, null, 0, 0, null, false, null,
            "party/active", actor.Travel.Revision + 1L), Is.True);
        Assert.That(DailyV1SelectedActionExecutionStage.TryPrepareSelectedFleeCityPlan(
            graph, actor, fleeAction, out _), Is.False);

        Assert.That(actor.Travel.TryInstallState(
            actor.Travel.CurrentLocationRuntimeId,
            actor.Travel.CurrentCityRuntimeId,
            null, null, 0, 0, null, false, null,
            null, actor.Travel.Revision + 1L), Is.True);
        Assert.That(actor.MoneyAccount.TryDebit(actor.MoneyAccount.Balance), Is.True);
        Assert.That(DailyV1SelectedActionExecutionStage.TryPrepareSelectedFleeCityPlan(
            graph, actor, fleeAction, out _), Is.False);
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
        Assert.That(fleeBoundary.ProjectionPreflightCalls, Is.EqualTo(1));
        Assert.That(fleeBoundary.AdmissionCalls, Is.EqualTo(1));
        Assert.That(fleeBoundary.NotificationCalls, Is.EqualTo(1));
        Assert.That(fleeBoundary.ProjectionCalls, Is.EqualTo(1));
        Assert.That(fleeBoundary.PublicationOrder,
            Is.EqualTo(new[] { "projection", "notification" }));
        Assert.That(fleeBoundary.Actor, Is.SameAs(actor));
        Assert.That(fleeBoundary.Destination, Is.SameAs(destination));
        Assert.That(fleeBoundary.OriginDecisionId, Is.EqualTo("decision/flee-city"));
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balanceBefore));
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(planRevisionBefore + 1L));
        Assert.That(actor.TravelPlan.Matches(
            destination.LocationRuntimeId, destination.RuntimeId,
            NpcTravelReason.Flee, 80f, result.FleeCity.TravelCost,
            "decision/flee-city"), Is.True);
    }

    [Test]
    public void SelectedFleeOwnerCommitRejectsNonMemberActorOrDestinationBeforeAdmission()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, _) = CreateTravelCandidateFixture();
        (DailyV1SelectedRuntimeOwnerGraph _, DailyV1NpcRuntimeOwners foreignActor,
            DailyV1CityRuntimeOwners foreignDestination, _) = CreateTravelCandidateFixture();
        long revisionBefore = actor.TravelPlan.Revision;
        int admissions = 0;
        int notifications = 0;

        Assert.That(graph.TryCommitSelectedFleeCityTravelPlan(
            foreignActor, destination, 12f, "decision/foreign-actor",
            () => { admissions++; return true; },
            () => notifications++, out bool actorChanged), Is.False);
        Assert.That(actorChanged, Is.False);
        Assert.That(graph.TryCommitSelectedFleeCityTravelPlan(
            actor, foreignDestination, 12f, "decision/foreign-city",
            () => { admissions++; return true; },
            () => notifications++, out bool cityChanged), Is.False);
        Assert.That(cityChanged, Is.False);
        Assert.That(admissions, Is.Zero);
        Assert.That(notifications, Is.Zero);
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(revisionBefore));
    }

    [Test]
    public void SelectedFleeOwnerCommitRejectsP12AdmissionWithoutChangingOwner()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, _) = CreateTravelCandidateFixture();
        long revisionBefore = actor.TravelPlan.Revision;
        NpcTravelPlanOwnerSnapshot stateBefore = CaptureTravelPlan(actor.TravelPlan);
        int admissions = 0;
        int notifications = 0;

        Assert.That(graph.TryCommitSelectedFleeCityTravelPlan(
            actor, destination, 12f, "decision/admission-rejected",
            () => { admissions++; return false; },
            () => notifications++, out bool changed), Is.False);

        Assert.That(changed, Is.False);
        Assert.That(admissions, Is.EqualTo(1));
        Assert.That(notifications, Is.Zero);
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(revisionBefore));
        Assert.That(CaptureTravelPlan(actor.TravelPlan), Is.EqualTo(stateBefore));
    }

    [Test]
    public void SelectedFleeOwnerCommitTreatsExactExistingPlanAsNoOpWithoutP12Callbacks()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, _) = CreateTravelCandidateFixture();
        Assert.That(actor.TravelPlan.TryInstallExact(
            destination.LocationRuntimeId, destination.RuntimeId,
            NpcTravelReason.Flee, 80f, 12f, "decision/already-fleeing", 4L), Is.True);
        int admissions = 0;
        int notifications = 0;

        Assert.That(graph.TryCommitSelectedFleeCityTravelPlan(
            actor, destination, 12f, "decision/already-fleeing",
            () => { admissions++; return true; },
            () => notifications++, out bool changed), Is.True);

        Assert.That(changed, Is.False);
        Assert.That(admissions, Is.Zero);
        Assert.That(notifications, Is.Zero);
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(4L));
        Assert.That(actor.TravelPlan.Matches(
            destination.LocationRuntimeId, destination.RuntimeId,
            NpcTravelReason.Flee, 80f, 12f, "decision/already-fleeing"), Is.True);
    }

    [Test]
    public void SelectedFleeOwnerCommitNormalizesBlankOriginDecisionId()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, _) = CreateTravelCandidateFixture();
        int admissions = 0;
        int notifications = 0;

        Assert.That(graph.TryCommitSelectedFleeCityTravelPlan(
            actor, destination, 12f, "  ",
            () => { admissions++; return true; },
            () => notifications++, out bool changed), Is.True);

        Assert.That(changed, Is.True);
        Assert.That(admissions, Is.EqualTo(1));
        Assert.That(notifications, Is.EqualTo(1));
        Assert.That(actor.TravelPlan.Matches(
            destination.LocationRuntimeId, destination.RuntimeId,
            NpcTravelReason.Flee, 80f, 12f, null), Is.True);
    }

    [Test]
    public void SelectedFleeOwnerCommitRejectsRevisionExhaustionBeforeP12Admission()
    {
        (DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, _) = CreateTravelCandidateFixture();
        Assert.That(actor.TravelPlan.TryInstallExact(
            destination.LocationRuntimeId, destination.RuntimeId,
            NpcTravelReason.Trade, 80f, 1f, "decision/previous-plan", long.MaxValue), Is.True);
        NpcTravelPlanOwnerSnapshot stateBefore = CaptureTravelPlan(actor.TravelPlan);
        int admissions = 0;
        int notifications = 0;

        Assert.That(graph.TryCommitSelectedFleeCityTravelPlan(
            actor, destination, 12f, "decision/exhausted",
            () => { admissions++; return true; },
            () => notifications++, out bool changed), Is.False);

        Assert.That(changed, Is.False);
        Assert.That(admissions, Is.Zero);
        Assert.That(notifications, Is.Zero);
        Assert.That(CaptureTravelPlan(actor.TravelPlan), Is.EqualTo(stateBefore));
    }

    private static NpcTravelPlanOwnerSnapshot CaptureTravelPlan(DailyV1NpcTravelPlanOwner owner) =>
        new NpcTravelPlanOwnerSnapshot(owner.TargetLocationRuntimeId,
            owner.TargetCityRuntimeId, owner.Reason, owner.Utility,
            owner.ExpectedCost, owner.OriginDecisionId, owner.Revision);

    private sealed class NpcTravelPlanOwnerSnapshot : IEquatable<NpcTravelPlanOwnerSnapshot>
    {
        private readonly string locationId;
        private readonly string cityId;
        private readonly NpcTravelReason reason;
        private readonly float utility;
        private readonly float cost;
        private readonly string decisionId;
        private readonly long revision;

        internal NpcTravelPlanOwnerSnapshot(string locationId, string cityId,
            NpcTravelReason reason, float utility, float cost, string decisionId, long revision)
        {
            this.locationId = locationId;
            this.cityId = cityId;
            this.reason = reason;
            this.utility = utility;
            this.cost = cost;
            this.decisionId = decisionId;
            this.revision = revision;
        }

        public bool Equals(NpcTravelPlanOwnerSnapshot other) => other != null
            && string.Equals(locationId, other.locationId, StringComparison.Ordinal)
            && string.Equals(cityId, other.cityId, StringComparison.Ordinal)
            && reason == other.reason
            && utility.Equals(other.utility)
            && cost.Equals(other.cost)
            && string.Equals(decisionId, other.decisionId, StringComparison.Ordinal)
            && revision == other.revision;

        public override bool Equals(object obj) => Equals(obj as NpcTravelPlanOwnerSnapshot);
        public override int GetHashCode() => HashCode.Combine(locationId, cityId,
            reason, utility, cost, decisionId, revision);
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
            DailyV1ActionDefinition action,
            out DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary boundary)
        { boundary = null; return false; }
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
        public bool TryGetCrimeActionBoundary(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        { boundary = null; return false; }
        public bool TryGetForcedEscapeBoundary(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
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
        internal int ProjectionPreflightCalls { get; private set; }
        internal int AdmissionCalls { get; private set; }
        internal int NotificationCalls { get; private set; }
        internal int ProjectionCalls { get; private set; }
        internal List<string> PublicationOrder { get; } = new List<string>();
        internal DailyV1NpcRuntimeOwners Actor { get; private set; }
        internal DailyV1CityRuntimeOwners Destination { get; private set; }
        internal string OriginDecisionId { get; private set; }
        public TravelPartyStore TravelPartyStore => null;
        public bool ExecuteWithinOperation(Func<bool> operation)
        { OperationCalls++; return operation != null && operation(); }
        public bool CanPrepareTravelPlanProjection(DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, float travelCost, string originDecisionId)
        {
            ProjectionPreflightCalls++;
            Actor = actor;
            Destination = destination;
            OriginDecisionId = originDecisionId;
            return travelCost > 0f;
        }
        public bool CanAdmitTravelPlanMutation()
        { AdmissionCalls++; return true; }
        public void PublishCommittedTravelPlan(DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination, float travelCost, string originDecisionId)
        {
            ProjectionCalls++;
            PublicationOrder.Add("projection");
            NotificationCalls++;
            PublicationOrder.Add("notification");
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
        TravelPartyStore travelParties = new TravelPartyStore();
        Assert.That(builder.TryBuild(null, travelParties,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.TravelParties, Is.SameAs(travelParties));
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
