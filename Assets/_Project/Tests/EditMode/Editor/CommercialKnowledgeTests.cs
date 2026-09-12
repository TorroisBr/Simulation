using NUnit.Framework;

public sealed class CommercialKnowledgeTests
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
    public void RecordObservation_NewerObservedDay_ReplacesExisting()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine");
        CommercialKnowledgeRuntime knowledge = new CommercialKnowledgeRuntime();
        knowledge.RecordObservation(SimulationTestFactory.CreateObservation("location-a", item, 50f, 10, 10, 10));

        bool replaced = knowledge.RecordObservation(SimulationTestFactory.CreateObservation("location-a", item, 25f, 20, 11, 11));

        Assert.That(replaced, Is.True);
        knowledge.TryGetObservation("location-a", item.DefinitionId, out CommercialMarketObservation current);
        Assert.That(current.ObservedPrice, Is.EqualTo(25f));
        Assert.That(current.ObservedDay, Is.EqualTo(11L));
    }

    [Test]
    public void RecordObservation_OlderObservedDay_DoesNotReplaceNewer()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-iron");
        CommercialKnowledgeRuntime knowledge = new CommercialKnowledgeRuntime();
        knowledge.RecordObservation(SimulationTestFactory.CreateObservation("location-a", item, 25f, 20, 11, 11));

        bool replaced = knowledge.RecordObservation(SimulationTestFactory.CreateObservation("location-a", item, 50f, 10, 10, 20));

        Assert.That(replaced, Is.False);
        knowledge.TryGetObservation("location-a", item.DefinitionId, out CommercialMarketObservation current);
        Assert.That(current.ObservedPrice, Is.EqualTo(25f));
        Assert.That(current.ObservedDay, Is.EqualTo(11L));
    }

    [Test]
    public void RecordObservation_SameDayDirectObservation_BeatsSharedAndInitialKnowledge()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-grain");
        CommercialKnowledgeRuntime knowledge = new CommercialKnowledgeRuntime();

        knowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            "location-a", item, 60f, 10, 10, 10, CommercialKnowledgeSource.InitialScenarioKnowledge));
        Assert.That(knowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            "location-a", item, 51f, 10, 10, 20, CommercialKnowledgeSource.SharedByNpc, "npc-bruno")), Is.True);
        Assert.That(knowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            "location-a", item, 28f, 10, 10, 10, CommercialKnowledgeSource.DirectObservation)), Is.True);

        knowledge.TryGetObservation("location-a", item.DefinitionId, out CommercialMarketObservation current);
        Assert.That(current.Source, Is.EqualTo(CommercialKnowledgeSource.DirectObservation));
        Assert.That(current.ObservedPrice, Is.EqualTo(28f));
    }

    [Test]
    public void RecordObservation_SameDaySharedKnowledge_BeatsInitialScenarioKnowledge()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-salt");
        CommercialKnowledgeRuntime knowledge = new CommercialKnowledgeRuntime();
        knowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            "location-a", item, 60f, 10, 10, 10, CommercialKnowledgeSource.InitialScenarioKnowledge));

        Assert.That(knowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            "location-a", item, 51f, 10, 10, 20, CommercialKnowledgeSource.SharedByNpc, "npc-bruno")), Is.True);
        knowledge.TryGetObservation("location-a", item.DefinitionId, out CommercialMarketObservation current);
        Assert.That(current.Source, Is.EqualTo(CommercialKnowledgeSource.SharedByNpc));
        Assert.That(current.SourceRuntimeId, Is.EqualTo("npc-bruno"));
    }

    [Test]
    public void RecordObservation_SameDaySamePriority_PreservesExistingSnapshot()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wool");
        CommercialKnowledgeRuntime knowledge = new CommercialKnowledgeRuntime();
        knowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            "location-a", item, 51f, 7, 10, 20, CommercialKnowledgeSource.SharedByNpc, "npc-bruno"));

        Assert.That(knowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            "location-a", item, 28f, 99, 10, 21, CommercialKnowledgeSource.SharedByNpc, "npc-caio")), Is.False);
        knowledge.TryGetObservation("location-a", item.DefinitionId, out CommercialMarketObservation current);
        Assert.That(current.ObservedPrice, Is.EqualTo(51f));
        Assert.That(current.ObservedStock, Is.EqualTo(7));
        Assert.That(current.SourceRuntimeId, Is.EqualTo("npc-bruno"));
    }

    [Test]
    public void Freshness_UsesObservedDayInsteadOfReceivedDay()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine");
        CommercialMarketObservation observation = SimulationTestFactory.CreateObservation(
            "location-a", item, 50f, 10, 10, 20, CommercialKnowledgeSource.SharedByNpc, "npc-bruno");
        CommercialKnowledgePolicy policy = new CommercialKnowledgePolicy(new CommercialKnowledgeSettings
        {
            freshForDays = 7,
            maxUsefulAgeDays = 30
        });

        Assert.That(policy.GetAgeDays(observation, 25L), Is.EqualTo(15L));
        Assert.That(policy.GetFreshness(observation, 25L), Is.LessThan(1f));
        Assert.That(policy.GetFreshness(observation, 25L), Is.GreaterThan(0f));
    }

    [Test]
    public void SharedObservation_RequiresSourceRuntimeId()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine");

        Assert.Throws<System.ArgumentException>(() => SimulationTestFactory.CreateObservation(
            "location-a", item, 50f, 10, 10, 10, CommercialKnowledgeSource.SharedByNpc));
    }

    [Test]
    public void SharedObservation_WithSourceRuntimeId_IsValid()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine");
        CommercialMarketObservation observation = SimulationTestFactory.CreateObservation(
            "location-a", item, 50f, 10, 10, 20, CommercialKnowledgeSource.SharedByNpc, "npc-bruno");

        SimulationInvariantValidator.ValidateCommercialObservation(observation);
        Assert.That(observation.SourceRuntimeId, Is.EqualTo("npc-bruno"));
    }

    [Test]
    public void CommercialObservationEvidence_CapturesKnowledgeSourceAndTransmissionMetadata()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine");
        CommercialMarketObservation observation = SimulationTestFactory.CreateObservation(
            "location-a", item, 51f, 8, 10, 20, CommercialKnowledgeSource.SharedByNpc, "npc-bruno");

        CommercialObservationEvidence evidence = CommercialObservationEvidence.Capture(observation, 0.5f);

        Assert.That(evidence.LocationRuntimeId, Is.EqualTo("location-a"));
        Assert.That(evidence.KnownPrice, Is.EqualTo(51f));
        Assert.That(evidence.ObservedDay, Is.EqualTo(10L));
        Assert.That(evidence.ReceivedDay, Is.EqualTo(20L));
        Assert.That(evidence.Freshness, Is.EqualTo(0.5f));
        Assert.That(evidence.Source, Is.EqualTo(CommercialKnowledgeSource.SharedByNpc));
        Assert.That(evidence.SourceRuntimeId, Is.EqualTo("npc-bruno"));
    }

    [Test]
    public void InitialCommercialBootstrap_UsesInitialSourceOnlyForSpatiallyKnownLocations()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine");
        ThreeCityFixture world = new ThreeCityFixture();
        world.A.Market.AddStock(item, 10, 10);
        world.B.Market.AddStock(item, 20, 20);
        world.C.Market.AddStock(item, 30, 30);
        NpcRuntime merchant = new NpcRuntime(
            "npc-merchant",
            SimulationTestFactory.CreateNpc("merchant", NpcJobType.Merchant, MerchantBehavior.Traveling),
            world.A,
            100f);
        merchant.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
        SimulationTime time = new SimulationTime();
        MerchantSystem system = new MerchantSystem(
            5, 1f, false, world.CreateTravelSystem(time), time, new CommercialKnowledgeSettings(), null);

        system.BootstrapInitialKnowledge(merchant);

        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            world.A.Location.RuntimeId, item.DefinitionId, out CommercialMarketObservation local), Is.True);
        Assert.That(local.Source, Is.EqualTo(CommercialKnowledgeSource.InitialScenarioKnowledge));
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            world.B.Location.RuntimeId, item.DefinitionId, out CommercialMarketObservation neighbor), Is.True);
        Assert.That(neighbor.Source, Is.EqualTo(CommercialKnowledgeSource.InitialScenarioKnowledge));
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            world.C.Location.RuntimeId, item.DefinitionId, out _), Is.False);
    }
}
