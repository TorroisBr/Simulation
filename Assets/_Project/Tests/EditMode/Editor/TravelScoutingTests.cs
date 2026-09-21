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
    public void TravelUsesEffectiveCostInsteadOfRawBootstrapValue()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                travel: new TravelConfigurationOverrides(7f)));
        TravelSystem travel = new TravelSystem(
            world.Network,
            location => world.CitiesByLocation.TryGetValue(location, out CityRuntime city) ? city : null,
            configuration.Travel,
            null,
            null);

        Assert.That(travel.GetTravelCost(2), Is.EqualTo(14f));
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

        Assert.That(arrivals.Count, Is.EqualTo(1));
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

        Assert.That(records.Events.Events.Count, Is.EqualTo(2));
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
            new EffectiveMerchantTradeConfiguration(enabled: true),
            new EffectiveCommercialKnowledgeConfiguration(),
            world.CreateTravelSystem(time),
            time,
            null);
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
            new EffectiveMerchantTradeConfiguration(enabled: true),
            new EffectiveCommercialKnowledgeConfiguration(),
            world.CreateTravelSystem(time),
            time,
            null);
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
    public void MerchantMinimumProfitIsContentSpecificAndDoesNotChangeGlobalTradePolicy()
    {
        ThreeCityFixture world = new ThreeCityFixture(false);
        ItemData item = SimulationTestFactory.CreateItem("item-margin", 10f);
        SimulationTime time = new SimulationTime();
        TravelSystem travel = world.CreateTravelSystem(time);
        EffectiveMerchantTradeConfiguration tradeConfiguration = new EffectiveMerchantTradeConfiguration(
            enabled: true,
            maxTradeAmount: 5);
        EffectiveCommercialKnowledgeConfiguration knowledgeConfiguration = new EffectiveCommercialKnowledgeConfiguration();
        MerchantSystem merchantSystem = new MerchantSystem(
            tradeConfiguration,
            knowledgeConfiguration,
            travel,
            time,
            null);

        NpcData lowMarginData = SimulationTestFactory.CreateNpc("low-margin", NpcJobType.Merchant, MerchantBehavior.Traveling);
        lowMarginData.job.minimumProfitPerItem = 1f;
        NpcRuntime lowMarginMerchant = new NpcRuntime("npc-low-margin", lowMarginData, world.A, 100f);
        NpcData highMarginData = SimulationTestFactory.CreateNpc("high-margin", NpcJobType.Merchant, MerchantBehavior.Traveling);
        highMarginData.job.minimumProfitPerItem = 4f;
        NpcRuntime highMarginMerchant = new NpcRuntime("npc-high-margin", highMarginData, world.A, 100f);

        foreach (NpcRuntime merchant in new[] { lowMarginMerchant, highMarginMerchant })
        {
            merchant.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
            merchant.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
            merchant.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
            merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
                world.A.Location.RuntimeId, item, 10f, 10, 0L, 0L));
            merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
                world.B.Location.RuntimeId, item, 13f, 10, 0L, 0L));
        }

        float lowUtility = 0f;
        float highUtility = 0f;
        NpcActionRuntime lowAction = merchantSystem.CreateAction(
            lowMarginMerchant,
            SimulationTestFactory.CreateAction("buy-low-margin", NpcActionType.BuyGoods, NpcActionCategory.Commerce),
            ref lowUtility);
        NpcActionRuntime highAction = merchantSystem.CreateAction(
            highMarginMerchant,
            SimulationTestFactory.CreateAction("buy-high-margin", NpcActionType.BuyGoods, NpcActionCategory.Commerce),
            ref highUtility);

        Assert.That(lowAction, Is.Not.Null);
        Assert.That(lowAction.Amount, Is.EqualTo(5));
        Assert.That(highAction, Is.Null);
    }

    [Test]
    public void TradeRepositionPolicyControlsOnlyNewAutonomousRepositionIntent()
    {
        ThreeCityFixture world = new ThreeCityFixture(false);
        SpatialRouteRuntime routeBC = SimulationTestFactory.CreateRoute("route-b-c", world.B, world.C, 1);
        Assert.That(world.Network.RegisterRoute(routeBC), Is.True);
        ItemData item = SimulationTestFactory.CreateItem("item-reposition", 10f);
        NpcRuntime merchant = new NpcRuntime(
            "npc-reposition",
            SimulationTestFactory.CreateNpc("reposition", NpcJobType.Merchant, MerchantBehavior.Traveling),
            world.A,
            100f);
        merchant.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.C.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(routeBC.RuntimeId);
        merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            world.B.Location.RuntimeId, item, 10f, 10, 0L, 0L));
        merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            world.C.Location.RuntimeId, item, 20f, 10, 0L, 0L));
        SimulationTime time = new SimulationTime();
        TravelSystem travel = world.CreateTravelSystem(time);
        NpcActionData travelAction = SimulationTestFactory.CreateAction("reposition-travel", NpcActionType.Travel, NpcActionCategory.Travel);
        float disabledUtility = 0f;
        MerchantSystem disabledPolicy = new MerchantSystem(
            new EffectiveMerchantTradeConfiguration(enabled: true, allowAutonomousTradeRepositioning: false),
            new EffectiveCommercialKnowledgeConfiguration(),
            travel,
            time,
            null);
        NpcActionRuntime disabledAction = disabledPolicy.CreateMerchantTravelAction(merchant, travelAction, ref disabledUtility);

        float enabledUtility = 0f;
        MerchantSystem enabledPolicy = new MerchantSystem(
            new EffectiveMerchantTradeConfiguration(enabled: true, allowAutonomousTradeRepositioning: true),
            new EffectiveCommercialKnowledgeConfiguration(),
            travel,
            time,
            null);
        NpcActionRuntime enabledAction = enabledPolicy.CreateMerchantTravelAction(merchant, travelAction, ref enabledUtility);

        Assert.That(disabledAction, Is.Null);
        Assert.That(enabledAction, Is.Not.Null);
        Assert.That(enabledAction.TravelReason, Is.EqualTo(NpcTravelReason.TradeReposition));
        Assert.That(enabledAction.TargetCity, Is.SameAs(world.B));
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
            new EffectiveMerchantTradeConfiguration(enabled: true),
            new EffectiveCommercialKnowledgeConfiguration(),
            world.CreateTravelSystem(time),
            time,
            null);
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
            new EffectiveMerchantTradeConfiguration(enabled: true),
            new EffectiveCommercialKnowledgeConfiguration(),
            world.CreateTravelSystem(time),
            time,
            records.DecisionRecorder);
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
