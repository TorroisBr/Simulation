using System.Collections.Generic;
using NUnit.Framework;

public sealed class ScenarioTests
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
    public void KnowledgeSharingScenario_PreservesAgeAndReplacesWithNewerInformation()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine");
        CityRuntime city = SimulationTestFactory.CreateCity("city-feira", "location-feira");
        NpcRuntime bruno = new NpcRuntime("npc-bruno", SimulationTestFactory.CreateNpc("bruno", NpcJobType.Merchant), city, 100f);
        NpcRuntime caio = new NpcRuntime("npc-caio", SimulationTestFactory.CreateNpc("caio", NpcJobType.Merchant), city, 100f);
        SimulationTime time = new SimulationTime(20L);
        CommercialKnowledgeSharingSystem sharing = new CommercialKnowledgeSharingSystem(time, null);
        bruno.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            city.Location.RuntimeId, item, 51f, 10, 10, 10));

        sharing.ShareAmongPresentMerchants(new[] { bruno, caio });
        time.AdvanceDay();
        bruno.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            city.Location.RuntimeId, item, 28f, 12, 21, 21));
        sharing.ShareAmongPresentMerchants(new[] { bruno, caio });

        Assert.That(caio.CommercialKnowledge.TryGetObservation(
            city.Location.RuntimeId, item.DefinitionId, out CommercialMarketObservation observation), Is.True);
        Assert.That(observation.ObservedPrice, Is.EqualTo(28f));
        Assert.That(observation.ObservedDay, Is.EqualTo(21L));
        Assert.That(observation.ReceivedDay, Is.EqualTo(21L));
        Assert.That(observation.SourceRuntimeId, Is.EqualTo(bruno.RuntimeId));
    }

    [Test]
    public void DecisionTravelChronicleScenario_PreservesCausalChainAndPerspective()
    {
        ThreeCityFixture world = new ThreeCityFixture(false);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime actor = new NpcRuntime("npc-bruno", SimulationTestFactory.CreateNpc("bruno"), world.A, 100f);
        NpcActionData travelAction = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        NpcActionRuntime actionRuntime = new NpcActionRuntime(travelAction, world.B, null, NpcTravelReason.Trade, 0f, 0f);
        NpcDecisionRecord decision = records.DecisionRecorder.RecordChosenAction(actor, actionRuntime, NpcDecisionOrigin.Autonomous);
        TravelSystem travel = world.CreateTravelSystem(records.Time, records.EventRecorder);

        Assert.That(travel.TryStartTravel(actor, actionRuntime), Is.True);
        travel.AdvanceTravels(new List<NpcRuntime> { actor });
        travel.AdvanceTravels(new List<NpcRuntime> { actor });

        IReadOnlyList<NpcChronicleEntry> chronicle = records.Chronicle.GetChronicle(actor.RuntimeId);

        Assert.That(chronicle.Count, Is.EqualTo(3));
        Assert.That(chronicle[0].Decision, Is.SameAs(decision));
        Assert.That(chronicle[1].DomainEvent.EventType, Is.EqualTo(DomainEventType.NpcTravelStarted));
        Assert.That(chronicle[2].DomainEvent.EventType, Is.EqualTo(DomainEventType.NpcArrived));
        Assert.That(chronicle[1].DomainEvent.OriginDecisionId, Is.EqualTo(decision.DecisionId));
        Assert.That(chronicle[2].DomainEvent.OriginDecisionId, Is.EqualTo(decision.DecisionId));
        SimulationInvariantValidator.ValidateDecisions(records.Decisions.Decisions);
        SimulationInvariantValidator.ValidateDomainEvents(records.Events.Events, null, records.Decisions);
        SimulationInvariantValidator.ValidateChronicle(chronicle);
    }

    [Test]
    public void CommercialScoutingScenario_RefreshesViaArrivalDirectObservation()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine", 10f);
        ThreeCityFixture world = new ThreeCityFixture(false);
        world.B.Market.AddStock(item, 10, 10);
        NpcData merchantData = SimulationTestFactory.CreateNpc("merchant", NpcJobType.Merchant, MerchantBehavior.Traveling);
        merchantData.job.preferredTradeItems.Add(new TradeItemPreference { item = item });
        NpcRuntime merchant = new NpcRuntime("npc-merchant", merchantData, world.A, 1000f);
        merchant.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
        SimulationTime time = new SimulationTime(40L);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture(40L);
        merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            world.B.Location.RuntimeId, item, 90f, 1, 0, 0));
        TravelSystem travel = world.CreateTravelSystem(time, records.EventRecorder);
        MerchantSystem merchantSystem = new MerchantSystem(
            5, 1f, false, travel, time, new CommercialKnowledgeSettings(), records.DecisionRecorder);
        NpcActionData travelAction = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        float utility = 0f;
        NpcActionRuntime scouting = merchantSystem.CreateMerchantTravelAction(merchant, travelAction, ref utility);
        NpcDecisionRecord decision = records.DecisionRecorder.RecordChosenAction(merchant, scouting, NpcDecisionOrigin.Autonomous);

        Assert.That(scouting.TravelReason, Is.EqualTo(NpcTravelReason.CommercialScout));
        Assert.That(travel.TryStartTravel(merchant, scouting), Is.True);
        travel.AdvanceTravels(new List<NpcRuntime> { merchant });
        IReadOnlyList<NpcRuntime> arrivals = travel.AdvanceTravels(new List<NpcRuntime> { merchant });
        merchantSystem.ObserveCurrentMarket(merchant);

        Assert.That(arrivals.Count, Is.EqualTo(1));
        Assert.That(merchant.CurrentCity, Is.SameAs(world.B));
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            world.B.Location.RuntimeId, item.DefinitionId, out CommercialMarketObservation refreshed), Is.True);
        Assert.That(refreshed.Source, Is.EqualTo(CommercialKnowledgeSource.DirectObservation));
        Assert.That(refreshed.ObservedDay, Is.EqualTo(40L));
        Assert.That(refreshed.ReceivedDay, Is.EqualTo(40L));
        Assert.That(refreshed.ObservedPrice, Is.EqualTo(world.B.Market.GetPrice(item)));
        Assert.That(records.Events.Events[0].OriginDecisionId, Is.EqualTo(decision.DecisionId));
    }
}
