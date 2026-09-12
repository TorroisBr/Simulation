using System.Collections.Generic;
using NUnit.Framework;

public sealed class TravelScoutingTests
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
    public void CanPlanKnownTravel_UnknownRouteIsUnavailableWithoutRemovingTruth()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        NpcRuntime npc = new NpcRuntime("npc-traveler", SimulationTestFactory.CreateNpc("traveler"), world.A, 100f);
        npc.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        npc.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
        npc.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
        TravelSystem travel = world.CreateTravelSystem(new SimulationTime());

        Assert.That(travel.CanPlanKnownTravel(npc, world.B, out _, out _), Is.True);
        Assert.That(travel.CanPlanKnownTravel(npc, world.C, out _, out _), Is.False);
        Assert.That(world.Network.TryGetRoute(world.RouteAC.RuntimeId, out SpatialRouteRuntime truthRoute), Is.True);
        Assert.That(truthRoute, Is.SameAs(world.RouteAC));
    }

    [Test]
    public void TravelExecution_UsesTruthAndDiscoversRouteOnlyWhenTravelStarts()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        NpcRuntime npc = new NpcRuntime("npc-traveler", SimulationTestFactory.CreateNpc("traveler"), world.A, 100f);
        npc.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        NpcActionData action = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        NpcActionRuntime travelAction = new NpcActionRuntime(action, world.B, null, NpcTravelReason.Trade, 0f, 0f);
        TravelSystem travel = world.CreateTravelSystem(new SimulationTime());

        Assert.That(npc.SpatialKnowledge.KnowsRoute(world.RouteAB.RuntimeId), Is.False);
        Assert.That(npc.SpatialKnowledge.KnowsLocation(world.B.Location.RuntimeId), Is.False);
        Assert.That(travel.TryStartTravel(npc, travelAction), Is.True);
        Assert.That(npc.SpatialKnowledge.KnowsRoute(world.RouteAB.RuntimeId), Is.True);
        Assert.That(npc.SpatialKnowledge.KnowsLocation(world.B.Location.RuntimeId), Is.False);
    }

    [Test]
    public void TravelArrival_DiscoversDestinationAfterTravelCompletes()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        NpcRuntime npc = new NpcRuntime("npc-traveler", SimulationTestFactory.CreateNpc("traveler"), world.A, 100f);
        NpcActionData action = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        TravelSystem travel = world.CreateTravelSystem(new SimulationTime());

        Assert.That(travel.TryStartTravel(npc, new NpcActionRuntime(action, world.B, null, NpcTravelReason.Trade, 0f, 0f)), Is.True);
        Assert.That(travel.AdvanceTravels(new List<NpcRuntime> { npc }), Is.Empty);
        IReadOnlyList<NpcRuntime> arrivals = travel.AdvanceTravels(new List<NpcRuntime> { npc });

        Assert.That(arrivals, Has.Count.EqualTo(1));
        Assert.That(npc.CurrentCity, Is.SameAs(world.B));
        Assert.That(npc.SpatialKnowledge.KnowsLocation(world.B.Location.RuntimeId), Is.True);
    }

    [Test]
    public void DecisionTravelArrival_PreservesCausalOriginAndSequence()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime npc = new NpcRuntime("npc-traveler", SimulationTestFactory.CreateNpc("traveler"), world.A, 100f);
        NpcActionData action = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        NpcActionRuntime actionRuntime = new NpcActionRuntime(action, world.B, null, NpcTravelReason.CommercialScout, 0f, 0f);
        NpcDecisionRecord decision = records.DecisionRecorder.RecordChosenAction(npc, actionRuntime, NpcDecisionOrigin.Autonomous);
        TravelSystem travel = world.CreateTravelSystem(records.Time, records.EventRecorder);

        Assert.That(travel.TryStartTravel(npc, actionRuntime), Is.True);
        travel.AdvanceTravels(new List<NpcRuntime> { npc });
        travel.AdvanceTravels(new List<NpcRuntime> { npc });

        Assert.That(records.Events.Events, Has.Count.EqualTo(2));
        Assert.That(records.Events.Events[0].EventType, Is.EqualTo(DomainEventType.NpcTravelStarted));
        Assert.That(records.Events.Events[1].EventType, Is.EqualTo(DomainEventType.NpcArrived));
        Assert.That(records.Events.Events[0].OriginDecisionId, Is.EqualTo(decision.DecisionId));
        Assert.That(records.Events.Events[1].OriginDecisionId, Is.EqualTo(decision.DecisionId));
        Assert.That(decision.RecordSequence, Is.LessThan(records.Events.Events[0].RecordSequence));
        Assert.That(records.Events.Events[0].RecordSequence, Is.LessThan(records.Events.Events[1].RecordSequence));
    }

    [Test]
    public void CommercialScout_SelectsKnownReachableStaleLocationOverFreshLocation()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        ItemData item = SimulationTestFactory.CreateItem("item-wine");
        NpcData merchantData = SimulationTestFactory.CreateNpc("merchant", NpcJobType.Merchant, MerchantBehavior.Traveling);
        merchantData.job.preferredTradeItems.Add(new TradeItemPreference { item = item });
        NpcRuntime merchant = new NpcRuntime("npc-merchant", merchantData, world.A, 1000f);
        merchant.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.C.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAC.RuntimeId);
        merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            world.B.Location.RuntimeId, item, 60f, 5, 0, 0));
        merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            world.C.Location.RuntimeId, item, 30f, 5, 40, 40));
        SimulationTime time = new SimulationTime(40L);
        MerchantSystem merchantSystem = new MerchantSystem(
            5, 1f, false, world.CreateTravelSystem(time), time, new CommercialKnowledgeSettings(), null);
        float utility = 0f;
        NpcActionData travelAction = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);

        NpcActionRuntime scouting = merchantSystem.CreateMerchantTravelAction(merchant, travelAction, ref utility);

        Assert.That(scouting, Is.Not.Null);
        Assert.That(scouting.TravelReason, Is.EqualTo(NpcTravelReason.CommercialScout));
        Assert.That(scouting.TargetCity, Is.SameAs(world.B));
        Assert.That(scouting.CommercialScoutingEvidence.StaleObservationCount, Is.EqualTo(1));
    }

    [Test]
    public void CommercialScout_UnknownKnownLocationCanGenerateNeed()
    {
        ThreeCityFixture world = new ThreeCityFixture(false);
        ItemData item = SimulationTestFactory.CreateItem("item-grain");
        NpcData merchantData = SimulationTestFactory.CreateNpc("merchant", NpcJobType.Merchant, MerchantBehavior.Traveling);
        merchantData.job.preferredTradeItems.Add(new TradeItemPreference { item = item });
        NpcRuntime merchant = new NpcRuntime("npc-merchant", merchantData, world.A, 1000f);
        merchant.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
        SimulationTime time = new SimulationTime(10L);
        MerchantSystem merchantSystem = new MerchantSystem(
            5, 1f, false, world.CreateTravelSystem(time), time, new CommercialKnowledgeSettings(), null);
        float utility = 0f;

        NpcActionRuntime scouting = merchantSystem.CreateMerchantTravelAction(
            merchant,
            SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel),
            ref utility);

        Assert.That(scouting, Is.Not.Null);
        Assert.That(scouting.TargetCity, Is.SameAs(world.B));
        Assert.That(scouting.CommercialScoutingEvidence.UnknownObservationCount, Is.EqualTo(1));
        Assert.That(scouting.CommercialScoutingEvidence.StaleObservationCount, Is.EqualTo(0));
    }

    [Test]
    public void CommercialScout_RemoteMarketTruthChangesDoNotChangePlanningResult()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        ItemData item = SimulationTestFactory.CreateItem("item-wine", 10f);
        NpcData merchantData = SimulationTestFactory.CreateNpc("merchant", NpcJobType.Merchant, MerchantBehavior.Traveling);
        merchantData.job.preferredTradeItems.Add(new TradeItemPreference { item = item });
        NpcRuntime merchant = new NpcRuntime("npc-merchant", merchantData, world.A, 1000f);
        merchant.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.C.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAC.RuntimeId);
        SimulationTime time = new SimulationTime(40L);
        MerchantSystem merchantSystem = new MerchantSystem(
            5, 1f, false, world.CreateTravelSystem(time), time, new CommercialKnowledgeSettings(), null);
        NpcActionData travelAction = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        float firstUtility = 0f;
        NpcActionRuntime first = merchantSystem.CreateMerchantTravelAction(merchant, travelAction, ref firstUtility);
        world.B.Market.AddStock(item, 10000, 1);
        world.C.Market.AddStock(item, 1, 10000);
        float secondUtility = 0f;
        NpcActionRuntime second = merchantSystem.CreateMerchantTravelAction(merchant, travelAction, ref secondUtility);

        Assert.That(second.TargetCity, Is.SameAs(first.TargetCity));
        Assert.That(second.CommercialScoutingEvidence.ExpectedScore, Is.EqualTo(first.CommercialScoutingEvidence.ExpectedScore));
        Assert.That(second.CommercialScoutingEvidence.UnknownObservationCount, Is.EqualTo(first.CommercialScoutingEvidence.UnknownObservationCount));
    }

    [Test]
    public void CommercialScoutDecision_CapturesStructuredEvidenceAndType()
    {
        ThreeCityFixture world = new ThreeCityFixture(false);
        ItemData item = SimulationTestFactory.CreateItem("item-wine");
        NpcData merchantData = SimulationTestFactory.CreateNpc("merchant", NpcJobType.Merchant, MerchantBehavior.Traveling);
        merchantData.job.preferredTradeItems.Add(new TradeItemPreference { item = item });
        NpcRuntime merchant = new NpcRuntime("npc-merchant", merchantData, world.A, 1000f);
        merchant.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
        SimulationTime time = new SimulationTime(10L);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture(10L);
        MerchantSystem merchantSystem = new MerchantSystem(
            5, 1f, false, world.CreateTravelSystem(time), time, new CommercialKnowledgeSettings(), records.DecisionRecorder);
        float utility = 0f;
        NpcActionRuntime scouting = merchantSystem.CreateMerchantTravelAction(
            merchant,
            SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel),
            ref utility);

        NpcDecisionRecord decision = records.DecisionRecorder.RecordChosenAction(merchant, scouting, NpcDecisionOrigin.Autonomous);

        Assert.That(decision.DecisionType, Is.EqualTo(NpcDecisionType.CommercialScout));
        Assert.That(decision.CommercialScoutingEvidence.TargetLocationRuntimeId, Is.EqualTo(world.B.Location.RuntimeId));
        Assert.That(decision.CommercialScoutingEvidence.KnownRouteRuntimeId, Is.EqualTo(world.RouteAB.RuntimeId));
        Assert.That(decision.CommercialScoutingEvidence.ExpectedTravelDays, Is.EqualTo(1));
        Assert.That(decision.CommercialScoutingEvidence.ExpectedTravelCost, Is.EqualTo(1f));
        Assert.That(scouting.OriginDecisionId, Is.EqualTo(decision.DecisionId));
    }
}
