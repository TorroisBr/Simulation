using System.Collections.Generic;
using NUnit.Framework;

public sealed class CommercialKnowledgeSharingTests
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
    public void Sharing_CopiesSendersRememberedObservation_NotCurrentMarketTruth()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine", 28f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "city-feira",
            "location-feira",
            SimulationTestFactory.CreateMarketItem(item, 100, 100));
        NpcRuntime bruno = new NpcRuntime("npc-bruno", SimulationTestFactory.CreateNpc("bruno", NpcJobType.Merchant), city, 100f);
        NpcRuntime caio = new NpcRuntime("npc-caio", SimulationTestFactory.CreateNpc("caio", NpcJobType.Merchant), city, 100f);
        bruno.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            city.Location.RuntimeId, item, 51f, 12, 10, 10));
        CommercialKnowledgeSharingSystem sharing = new CommercialKnowledgeSharingSystem(
            new SimulationTime(20L),
            new EffectiveCommercialKnowledgeConfiguration());

        sharing.ShareAmongPresentMerchants(new[] { bruno, caio });

        Assert.That(city.Market.GetPrice(item), Is.EqualTo(28f));
        Assert.That(caio.CommercialKnowledge.TryGetObservation(
            city.Location.RuntimeId, item.DefinitionId, out CommercialMarketObservation received), Is.True);
        Assert.That(received.ObservedPrice, Is.EqualTo(51f));
        Assert.That(received.ObservedDay, Is.EqualTo(10L));
        Assert.That(received.ReceivedDay, Is.EqualTo(20L));
        Assert.That(received.Source, Is.EqualTo(CommercialKnowledgeSource.SharedByNpc));
        Assert.That(received.SourceRuntimeId, Is.EqualTo(bruno.RuntimeId));
    }

    [Test]
    public void Sharing_DoesNotDowngradeReceiverWithNewerObservation()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-iron");
        CityRuntime city = SimulationTestFactory.CreateCity("city-a", "location-a");
        NpcRuntime bruno = new NpcRuntime("npc-bruno", SimulationTestFactory.CreateNpc("bruno", NpcJobType.Merchant), city, 100f);
        NpcRuntime caio = new NpcRuntime("npc-caio", SimulationTestFactory.CreateNpc("caio", NpcJobType.Merchant), city, 100f);
        bruno.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            city.Location.RuntimeId, item, 51f, 10, 10, 10));
        caio.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            city.Location.RuntimeId, item, 35f, 8, 15, 15));

        new CommercialKnowledgeSharingSystem(
            new SimulationTime(20L),
            new EffectiveCommercialKnowledgeConfiguration())
            .ShareAmongPresentMerchants(new[] { bruno, caio });

        caio.CommercialKnowledge.TryGetObservation(city.Location.RuntimeId, item.DefinitionId, out CommercialMarketObservation current);
        Assert.That(current.ObservedDay, Is.EqualTo(15L));
        Assert.That(current.ObservedPrice, Is.EqualTo(35f));
    }

    [Test]
    public void Sharing_UsesNewestUsefulObservationsFirstAndHonorsLimit()
    {
        ItemData olderItem = SimulationTestFactory.CreateItem("item-older");
        ItemData newerItem = SimulationTestFactory.CreateItem("item-newer");
        CityRuntime city = SimulationTestFactory.CreateCity("city-a", "location-a");
        NpcRuntime sender = new NpcRuntime("npc-sender", SimulationTestFactory.CreateNpc("sender", NpcJobType.Merchant), city, 100f);
        NpcRuntime receiver = new NpcRuntime("npc-receiver", SimulationTestFactory.CreateNpc("receiver", NpcJobType.Merchant), city, 100f);
        sender.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            city.Location.RuntimeId, olderItem, 10f, 1, 5, 5));
        sender.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            city.Location.RuntimeId, newerItem, 20f, 1, 8, 8));
        CommercialKnowledgeSharingSystem sharing = new CommercialKnowledgeSharingSystem(
            new SimulationTime(10L),
            new EffectiveCommercialKnowledgeConfiguration(maxSharedObservationsPerInteraction: 1));

        sharing.ShareAmongPresentMerchants(new[] { sender, receiver });

        Assert.That(receiver.CommercialKnowledge.Observations.Count, Is.EqualTo(1));
        Assert.That(receiver.CommercialKnowledge.TryGetObservation(
            city.Location.RuntimeId, newerItem.DefinitionId, out _), Is.True);
    }

    [Test]
    public void Sharing_ReceivedKnowledgeCannotCascadeWithinSameDay()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine");
        CityRuntime city = SimulationTestFactory.CreateCity("city-a", "location-a");
        NpcRuntime bruno = new NpcRuntime("npc-bruno", SimulationTestFactory.CreateNpc("bruno", NpcJobType.Merchant), city, 100f);
        NpcRuntime caio = new NpcRuntime("npc-caio", SimulationTestFactory.CreateNpc("caio", NpcJobType.Merchant), city, 100f);
        NpcRuntime marta = new NpcRuntime("npc-marta", SimulationTestFactory.CreateNpc("marta", NpcJobType.Merchant), city, 100f);
        bruno.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            city.Location.RuntimeId, item, 51f, 10, 10, 10));
        CommercialKnowledgeSharingSystem sharing = new CommercialKnowledgeSharingSystem(
            new SimulationTime(20L),
            new EffectiveCommercialKnowledgeConfiguration());

        sharing.ShareAmongPresentMerchants(new[] { bruno, caio });
        sharing.ShareAmongPresentMerchants(new[] { caio, marta });

        Assert.That(caio.CommercialKnowledge.TryGetObservation(
            city.Location.RuntimeId, item.DefinitionId, out CommercialMarketObservation received), Is.True);
        Assert.That(received.SourceRuntimeId, Is.EqualTo(bruno.RuntimeId));
        Assert.That(marta.CommercialKnowledge.TryGetObservation(
            city.Location.RuntimeId, item.DefinitionId, out _), Is.False);
    }

    [Test]
    public void Sharing_RequiresSameCityAndStationaryMerchants()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine");
        CityRuntime firstCity = SimulationTestFactory.CreateCity("city-a", "location-a");
        CityRuntime secondCity = SimulationTestFactory.CreateCity("city-b", "location-b");
        NpcRuntime sender = new NpcRuntime("npc-sender", SimulationTestFactory.CreateNpc("sender", NpcJobType.Merchant), firstCity, 100f);
        NpcRuntime differentCity = new NpcRuntime("npc-different", SimulationTestFactory.CreateNpc("different", NpcJobType.Merchant), secondCity, 100f);
        NpcRuntime traveler = new NpcRuntime("npc-traveler", SimulationTestFactory.CreateNpc("traveler", NpcJobType.Merchant), firstCity, 100f);
        traveler.StartTravel(secondCity, 2);
        sender.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            firstCity.Location.RuntimeId, item, 51f, 10, 10, 10));

        new CommercialKnowledgeSharingSystem(
            new SimulationTime(20L),
            new EffectiveCommercialKnowledgeConfiguration())
            .ShareAmongPresentMerchants(new List<NpcRuntime> { sender, differentCity, traveler });

        Assert.That(differentCity.CommercialKnowledge.Observations, Is.Empty);
        Assert.That(traveler.CommercialKnowledge.Observations, Is.Empty);
    }

    [Test]
    public void Sharing_CopiesLiquiditySnapshotWithProvenanceAndPreservedObservedDay()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-a", "location-a");
        NpcRuntime sender = new NpcRuntime("npc-sender", SimulationTestFactory.CreateNpc("sender", NpcJobType.Merchant), city, 100f);
        NpcRuntime receiver = new NpcRuntime("npc-receiver", SimulationTestFactory.CreateNpc("receiver", NpcJobType.Merchant), city, 100f);
        sender.CommercialKnowledge.RecordLiquidityObservation(SimulationTestFactory.CreateLiquidityObservation(
            "location-market", MarketLiquidityMode.AccountBacked, 42f, 10L, 10L));

        new CommercialKnowledgeSharingSystem(
            new SimulationTime(20L),
            new EffectiveCommercialKnowledgeConfiguration())
            .ShareAmongPresentMerchants(new[] { sender, receiver });

        Assert.That(receiver.CommercialKnowledge.TryGetLiquidityObservation(
            "location-market", out CommercialLiquidityObservation received), Is.True);
        SimulationInvariantValidator.ValidateCommercialLiquidityObservation(received);
        Assert.That(received.ObservedPurchasingPower, Is.EqualTo(42f));
        Assert.That(received.ObservedDay, Is.EqualTo(10L));
        Assert.That(received.ReceivedDay, Is.EqualTo(20L));
        Assert.That(received.Source, Is.EqualTo(CommercialKnowledgeSource.SharedByNpc));
        Assert.That(received.SourceRuntimeId, Is.EqualTo(sender.RuntimeId));
    }

    [Test]
    public void Sharing_ReceivedLiquidityCannotCascadeWithinSameDay()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-a", "location-a");
        NpcRuntime first = new NpcRuntime("npc-first", SimulationTestFactory.CreateNpc("first", NpcJobType.Merchant), city, 100f);
        NpcRuntime second = new NpcRuntime("npc-second", SimulationTestFactory.CreateNpc("second", NpcJobType.Merchant), city, 100f);
        NpcRuntime third = new NpcRuntime("npc-third", SimulationTestFactory.CreateNpc("third", NpcJobType.Merchant), city, 100f);
        first.CommercialKnowledge.RecordLiquidityObservation(SimulationTestFactory.CreateLiquidityObservation(
            "location-market", MarketLiquidityMode.AccountBacked, 42f, 10L, 10L));
        CommercialKnowledgeSharingSystem sharing = new CommercialKnowledgeSharingSystem(
            new SimulationTime(20L),
            new EffectiveCommercialKnowledgeConfiguration());

        sharing.ShareAmongPresentMerchants(new[] { first, second });
        sharing.ShareAmongPresentMerchants(new[] { second, third });

        Assert.That(second.CommercialKnowledge.TryGetLiquidityObservation("location-market", out _), Is.True);
        Assert.That(third.CommercialKnowledge.TryGetLiquidityObservation("location-market", out _), Is.False);
    }
}
