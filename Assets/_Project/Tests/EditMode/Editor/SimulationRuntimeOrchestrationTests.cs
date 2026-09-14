using System.Collections.Generic;
using NUnit.Framework;

public sealed class SimulationRuntimeOrchestrationTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void AdvanceDay_ExecutesOneAutonomousDecisionThroughRuntime()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime npc = new NpcRuntime("npc-autonomous", SimulationTestFactory.CreateNpc("autonomous"));
        NpcStatusData completedStatus = SimulationTestFactory.CreateStatus("completed");
        NpcActionData action = SimulationTestFactory.CreateAction("autonomous-action", NpcActionType.Normal);
        action.statusToAdd.Add(completedStatus);
        NpcDecisionSystem decisionSystem = new NpcDecisionSystem(new List<INpcActionProvider>());
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            null,
            new[] { npc },
            economyEnabled: false,
            configuredActions: new[] { action },
            npcDecisionSystem: decisionSystem,
            decisionRecorder: records.DecisionRecorder);

        runtime.AdvanceDay();

        Assert.That(runtime.CurrentDay, Is.EqualTo(1L));
        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(1));
        NpcDecisionRecord decision = records.Decisions.Decisions[0];
        Assert.That(decision.ActorRuntimeId, Is.EqualTo(npc.RuntimeId));
        Assert.That(decision.DecisionType, Is.EqualTo(NpcDecisionType.Action));
        Assert.That(decision.Origin, Is.EqualTo(NpcDecisionOrigin.Autonomous));
        Assert.That(decision.ActionDefinitionId, Is.EqualTo(action.DefinitionId));
        Assert.That(npc.CurrentActionRuntime, Is.Not.Null);
        Assert.That(npc.CurrentActionRuntime.OriginDecisionId, Is.EqualTo(decision.DecisionId));
        Assert.That(npc.CurrentStatus, Has.Member(completedStatus));
    }

    [Test]
    public void AdvanceDay_ProcessesScheduledDirectiveBeforeAutonomousDecision()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        CityRuntime city = SimulationTestFactory.CreateCity("city-directive", "location-directive");
        NpcRuntime guard = new NpcRuntime(
            "npc-directive-guard",
            SimulationTestFactory.CreateNpc("directive-guard"),
            city,
            0f);
        NpcRuntime actor = new NpcRuntime(
            "npc-directive-actor",
            SimulationTestFactory.CreateNpc("directive-actor"),
            city,
            0f);
        NpcStatusData freeStatus = SimulationTestFactory.CreateStatus("free");
        NpcStatusData wantedStatus = SimulationTestFactory.CreateStatus("wanted");
        NpcStatusData arrestedStatus = SimulationTestFactory.CreateStatus("arrested");
        NpcStatusData hiddenStatus = SimulationTestFactory.CreateStatus("hidden");
        JusticeSystem justice = new JusticeSystem(
            freeStatus,
            wantedStatus,
            arrestedStatus,
            hiddenStatus,
            records.EventRecorder,
            null);
        justice.CreateOrIncreaseWarrant(actor, city, 50f, 3);
        Assert.That(justice.Arrest(guard, actor, city), Is.True);

        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterNpc(actor), Is.True);
        NpcActionData escape = SimulationTestFactory.CreateAction(
            "escape-directive",
            NpcActionType.EscapePrison,
            NpcActionCategory.Justice);
        ScheduledDirective directive = new ScheduledDirective(
            "directive-runtime",
            1L,
            ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison,
            actor.RuntimeId,
            escape);
        ScheduledDirectiveStore directiveStore = new ScheduledDirectiveStore(records.Time);
        Assert.That(directiveStore.Add(directive), Is.True);
        ScheduledDirectiveSystem directiveSystem = new ScheduledDirectiveSystem(directiveStore, registry);
        CrimeSystem crime = new CrimeSystem(justice, null, hiddenStatus);
        NpcDecisionSystem decisionSystem = new NpcDecisionSystem(new List<INpcActionProvider> { crime });
        NpcActionData autonomousFallback = SimulationTestFactory.CreateAction("autonomous-fallback", NpcActionType.Normal);
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { city },
            new[] { actor },
            economyEnabled: false,
            configuredActions: new[] { autonomousFallback },
            scheduledDirectiveSystem: directiveSystem,
            justiceSystem: justice,
            crimeSystem: crime,
            npcDecisionSystem: decisionSystem,
            decisionRecorder: records.DecisionRecorder);

        runtime.AdvanceDay();

        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(1));
        Assert.That(records.Decisions.Decisions[0].Origin, Is.EqualTo(NpcDecisionOrigin.ScheduledDirective));
        Assert.That(records.Decisions.Decisions[0].DecisionType, Is.EqualTo(NpcDecisionType.Escape));
        Assert.That(records.Decisions.Decisions[0].ActionDefinitionId, Is.EqualTo(escape.DefinitionId));
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
        Assert.That(actor.CurrentActionRuntime, Is.Not.Null);
        Assert.That(actor.CurrentActionRuntime.Action, Is.SameAs(escape));
        Assert.That(justice.IsArrested(actor), Is.False);
    }

    [Test]
    public void AdvanceDay_TravelingNpcSkipsAutonomousDecisionUntilTravelProgresses()
    {
        ThreeCityFixture world = new ThreeCityFixture(false, routeABTravelDays: 2);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime npc = new NpcRuntime(
            "npc-traveling",
            SimulationTestFactory.CreateNpc("traveling"),
            world.A,
            100f);
        TravelSystem travel = world.CreateTravelSystem(records.Time, records.EventRecorder);
        NpcActionData travelAction = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        Assert.That(travel.TryStartTravel(
            npc,
            new NpcActionRuntime(travelAction, world.B, null, NpcTravelReason.Trade, 0f, 0f)), Is.True);
        npc.ClearTravelStartedToday();

        NpcActionData autonomousAction = SimulationTestFactory.CreateAction("autonomous-action", NpcActionType.Normal);
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { world.A, world.B },
            new[] { npc },
            economyEnabled: false,
            configuredActions: new[] { autonomousAction },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider>()),
            decisionRecorder: records.DecisionRecorder,
            travelSystem: travel);

        runtime.AdvanceDay();

        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(0));
        Assert.That(npc.IsTraveling, Is.True);
        Assert.That(npc.CurrentCity, Is.Null);
        Assert.That(npc.DestinationCity, Is.SameAs(world.B));
        Assert.That(npc.TravelDaysRemaining, Is.EqualTo(1));
    }

    [Test]
    public void AdvanceDay_ArrivalHappensAfterDecisionAndNextDayCanDecideAtDestination()
    {
        ThreeCityFixture world = new ThreeCityFixture(false, routeABTravelDays: 1);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime npc = new NpcRuntime(
            "npc-arrival",
            SimulationTestFactory.CreateNpc("arrival"),
            world.A,
            100f);
        TravelSystem travel = world.CreateTravelSystem(records.Time, records.EventRecorder);
        NpcActionData travelAction = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        Assert.That(travel.TryStartTravel(
            npc,
            new NpcActionRuntime(travelAction, world.B, null, NpcTravelReason.Trade, 0f, 0f)), Is.True);
        npc.ClearTravelStartedToday();

        NpcActionData autonomousAction = SimulationTestFactory.CreateAction("destination-action", NpcActionType.Normal);
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { world.A, world.B },
            new[] { npc },
            economyEnabled: false,
            configuredActions: new[] { autonomousAction },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider>()),
            decisionRecorder: records.DecisionRecorder,
            travelSystem: travel);

        runtime.AdvanceDay();

        Assert.That(runtime.CurrentDay, Is.EqualTo(1L));
        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(0));
        Assert.That(npc.IsTraveling, Is.False);
        Assert.That(npc.CurrentCity, Is.SameAs(world.B));

        runtime.AdvanceDay();

        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(1));
        Assert.That(records.Decisions.Decisions[0].AbsoluteDay, Is.EqualTo(2L));
        Assert.That(records.Decisions.Decisions[0].Origin, Is.EqualTo(NpcDecisionOrigin.Autonomous));
        Assert.That(npc.CurrentActionRuntime.Action, Is.SameAs(autonomousAction));
    }

    [Test]
    public void AdvanceDay_ArrivalRefreshesMerchantKnowledgeAfterTravelProgress()
    {
        ThreeCityFixture world = new ThreeCityFixture(false, routeABTravelDays: 1);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ItemData item = SimulationTestFactory.CreateItem("item-arrival");
        world.B.Market.AddStock(item, 10, 10);
        NpcRuntime merchant = new NpcRuntime(
            "npc-arriving-merchant",
            SimulationTestFactory.CreateNpc("arriving-merchant", NpcJobType.Merchant, MerchantBehavior.Traveling),
            world.A,
            100f);
        TravelSystem travel = world.CreateTravelSystem(records.Time, records.EventRecorder);
        MerchantSystem merchantSystem = SimulationTestFactory.CreateMerchantSystem(
            travel,
            records.Time,
            records.DecisionRecorder);
        merchantSystem.ObserveCurrentMarket(merchant);
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            world.B.Location.RuntimeId,
            item.DefinitionId,
            out _), Is.False);

        NpcActionData travelAction = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        Assert.That(travel.TryStartTravel(
            merchant,
            new NpcActionRuntime(travelAction, world.B, null, NpcTravelReason.Trade, 0f, 0f)), Is.True);
        merchant.ClearTravelStartedToday();

        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { world.A, world.B },
            new[] { merchant },
            economyEnabled: false,
            travelSystem: travel,
            merchantSystem: merchantSystem);

        runtime.AdvanceDay();

        Assert.That(merchant.CurrentCity, Is.SameAs(world.B));
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            world.B.Location.RuntimeId,
            item.DefinitionId,
            out CommercialMarketObservation observation), Is.True);
        Assert.That(observation.ObservedDay, Is.EqualTo(runtime.CurrentDay));
        Assert.That(observation.Source, Is.EqualTo(CommercialKnowledgeSource.DirectObservation));
        Assert.That(merchant.CommercialKnowledge.TryGetLiquidityObservation(
            world.B.Location.RuntimeId,
            out CommercialLiquidityObservation liquidity), Is.True);
        Assert.That(liquidity.ObservedDay, Is.EqualTo(runtime.CurrentDay));
        Assert.That(liquidity.Source, Is.EqualTo(CommercialKnowledgeSource.DirectObservation));
    }

    [Test]
    public void AdvanceDay_GroupTravelProgressesOnceAndKeepsMembersSynchronized()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        Assert.That(fixture.System.TryStartTravelParty(CreateGroupTravelContext(fixture), out TravelPartyRuntime party), Is.True);

        foreach (NpcRuntime member in fixture.Members)
        {
            member.ClearTravelStartedToday();
        }

        SimulationRuntime runtime = new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.World.A, fixture.World.B, fixture.World.C },
            fixture.Members,
            economyEnabled: false,
            travelSystem: fixture.Travel,
            travelPartySystem: fixture.System);

        runtime.AdvanceDay();

        Assert.That(party.IsActive, Is.True);
        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(1));
        foreach (NpcRuntime member in fixture.Members)
        {
            Assert.That(member.IsTraveling, Is.True);
            Assert.That(member.ActiveTravelPartyId, Is.EqualTo(party.TravelPartyId));
            Assert.That(member.TravelDaysRemaining, Is.EqualTo(2));
        }

        SimulationInvariantValidator.ValidateTravelParties(
            fixture.Parties,
            fixture.World.IdentityRegistry,
            fixture.Members);
    }

    private static ActionExecutionContext CreateGroupTravelContext(TravelPartyFixture fixture)
    {
        return new ActionExecutionContext(
            "group-travel-runtime",
            new[]
            {
                new ActionExecutionParticipant(fixture.Bruno.RuntimeId, ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant(fixture.Caio.RuntimeId, ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant(fixture.Marta.RuntimeId, ActionExecutionParticipantRole.Support)
            },
            fixture.World.B.Location.RuntimeId,
            fixture.World.RouteAB.RuntimeId);
    }
}
