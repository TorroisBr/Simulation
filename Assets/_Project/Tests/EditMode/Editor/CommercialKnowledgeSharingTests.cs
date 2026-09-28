using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;

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
    public void BoundaryProvider_RetainsSnapshotCapturedAfterPriorObservationAndUsesFrozenPairing()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-frozen-share");
        CityRuntime city = SimulationTestFactory.CreateCity("city-frozen", "location-frozen");
        NpcRuntime first = new NpcRuntime("npc-a", SimulationTestFactory.CreateNpc("first", NpcJobType.Merchant), city, 100f);
        NpcRuntime second = new NpcRuntime("npc-b", SimulationTestFactory.CreateNpc("second", NpcJobType.Merchant), city, 100f);
        Dictionary<string, NpcRuntime> actors = new Dictionary<string, NpcRuntime>
        {
            [first.RuntimeId] = first,
            [second.RuntimeId] = second
        };
        CommercialKnowledgeSharingSystem sharing = new CommercialKnowledgeSharingSystem(
            new SimulationTime(3L), new EffectiveCommercialKnowledgeConfiguration());
        CommercialKnowledgeSharingDailyBoundaryStepProvider provider =
            new CommercialKnowledgeSharingDailyBoundaryStepProvider(() => new[] { first, second },
                id => actors.TryGetValue(id, out NpcRuntime npc) ? npc : null, sharing);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 3L);

        Assert.That(provider.TryCreateSteps(operation, 0, out IReadOnlyList<BoundaryContinuationStep> steps, out _), Is.True);
        Assert.That(steps.Count, Is.EqualTo(3));
        Assert.That(steps[0].OperationKind, Is.EqualTo("commercial-sharing.snapshot"));
        Assert.That(steps[1].StepId, Is.EqualTo(GetEdgeStepId(second.RuntimeId, first.RuntimeId)));

        // This is the prior local-observation step in the boundary sequence.
        first.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            city.Location.RuntimeId, item, 51f, 9, 2, 2));
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(operation,
            "test", "1", "config", steps, "content");
        Assert.That(provider.TryPrepareStep(manifest, steps[0], out IBoundaryContinuationStepCommit snapshot, out _), Is.True);
        Assert.That(snapshot.TryCommit(out _), Is.True);

        // A replay after a recipient mutation must return the retained source snapshot.
        ItemData recipientItem = SimulationTestFactory.CreateItem("item-recipient-local");
        second.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            "location-recipient", recipientItem, 9f, 1, 3, 3));
        Assert.That(provider.TryPrepareStep(manifest, steps[0], out IBoundaryContinuationStepCommit replaySnapshot, out _), Is.True);
        Assert.That(replaySnapshot.TryCommit(out _), Is.True);

        // Later source changes cannot alter this boundary's retained phase snapshot.
        first.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            city.Location.RuntimeId, item, 88f, 3, 3, 3));
        foreach (BoundaryContinuationStep edge in new[] { steps[1], steps[2] })
        {
            Assert.That(provider.TryPrepareStep(manifest, edge, out IBoundaryContinuationStepCommit prepared, out _), Is.True);
            Assert.That(prepared.TryCommit(out _), Is.True);
        }

        Assert.That(second.CommercialKnowledge.TryGetObservation(city.Location.RuntimeId,
            item.DefinitionId, out CommercialMarketObservation received), Is.True);
        Assert.That(received.ObservedPrice, Is.EqualTo(51f));
        Assert.That(received.ReceivedDay, Is.EqualTo(3L));
        Assert.That(received.SourceRuntimeId, Is.EqualTo(first.RuntimeId));
        Assert.That(second.CommercialKnowledge.TryGetObservation("location-recipient",
            recipientItem.DefinitionId, out _), Is.True);
    }

    [Test]
    public void BoundaryProvider_FreezesFullRosterOrderMerchantCardinalityCityIdentityAndRotation()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-shared-id", "location-shared-id");
        CityRuntime sameIdentityDifferentObject = SimulationTestFactory.CreateCity("city-shared-id", "location-shared-id");
        NpcRuntime a = CreateMerchant("npc-a", city);
        NpcRuntime b = CreateMerchant("npc-b", city);
        NpcRuntime c = CreateMerchant("npc-c", city);
        NpcRuntime d = CreateMerchant("npc-d", city);
        NpcRuntime e = CreateMerchant("npc-e", sameIdentityDifferentObject);
        NpcRuntime nonMerchant = new NpcRuntime("npc-non-merchant",
            SimulationTestFactory.CreateNpc("non-merchant", NpcJobType.None), city, 100f);
        NpcRuntime[] roster = { d, nonMerchant, b, e, a, c };
        Dictionary<string, NpcRuntime> actors = new Dictionary<string, NpcRuntime>();
        foreach (NpcRuntime npc in roster) actors.Add(npc.RuntimeId, npc);
        CommercialKnowledgeSharingDailyBoundaryStepProvider provider = CreateProvider(roster, actors);

        Assert.That(provider.TryCreateSteps(new DailyBoundaryOperation("world", "profile", 1L), 5,
            out IReadOnlyList<BoundaryContinuationStep> dayOne, out _), Is.True);
        FrozenSharePlan plan = JsonUtility.FromJson<FrozenSharePlan>(dayOne[0].Payload);
        Assert.That(Array.ConvertAll(plan.runtimeRoster, entry => entry.runtimeId),
            Is.EqualTo(new[] { "npc-d", "npc-non-merchant", "npc-b", "npc-e", "npc-a", "npc-c" }));
        Assert.That(Array.ConvertAll(plan.participants, entry => entry.runtimeId),
            Is.EqualTo(new[] { "npc-a", "npc-b", "npc-c", "npc-d", "npc-e" }));
        Assert.That(plan.edges.Length, Is.EqualTo(4));
        Assert.That(Array.ConvertAll(plan.edges, edge => edge.senderRuntimeId + ">" + edge.receiverRuntimeId),
            Is.EqualTo(new[] { "npc-b>npc-c", "npc-c>npc-b", "npc-d>npc-a", "npc-a>npc-d" }));
        for (int i = 0; i < plan.edges.Length; i++)
            Assert.That(dayOne[i + 1].StepId, Is.EqualTo(GetEdgeStepId(plan.edges[i].senderRuntimeId,
                plan.edges[i].receiverRuntimeId)));

        Assert.That(provider.TryCreateSteps(new DailyBoundaryOperation("world", "profile", 2L), 0,
            out IReadOnlyList<BoundaryContinuationStep> dayTwo, out _), Is.True);
        FrozenSharePlan nextDay = JsonUtility.FromJson<FrozenSharePlan>(dayTwo[0].Payload);
        Assert.That(Array.ConvertAll(nextDay.edges, edge => edge.senderRuntimeId + ">" + edge.receiverRuntimeId),
            Is.EqualTo(new[] { "npc-c>npc-d", "npc-d>npc-c", "npc-a>npc-b", "npc-b>npc-a" }));
    }

    [Test]
    public void BoundaryProvider_RejectsParticipantMovedToReplacementCityWithSameIds()
    {
        CityRuntime original = SimulationTestFactory.CreateCity("city-stable", "location-stable");
        CityRuntime replacement = SimulationTestFactory.CreateCity("city-stable", "location-stable");
        NpcRuntime first = CreateMerchant("npc-first", original);
        NpcRuntime second = CreateMerchant("npc-second", original);
        NpcRuntime[] roster = { first, second };
        Dictionary<string, NpcRuntime> actors = new Dictionary<string, NpcRuntime>
        {
            [first.RuntimeId] = first,
            [second.RuntimeId] = second
        };
        CommercialKnowledgeSharingDailyBoundaryStepProvider provider = CreateProvider(roster, actors);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 4L);
        Assert.That(provider.TryCreateSteps(operation, 0, out IReadOnlyList<BoundaryContinuationStep> steps, out _), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(operation, "test", "1", "config", steps);
        Assert.That(provider.TryPrepareStep(manifest, steps[0], out IBoundaryContinuationStepCommit snapshot, out _), Is.True);
        Assert.That(snapshot.TryCommit(out _), Is.True);

        Assert.That(first.SetCurrentPresence(replacement.Location, replacement), Is.True);
        Assert.That(provider.TryPrepareStep(manifest, steps[1], out _, out _), Is.False);
    }

    private static NpcRuntime CreateMerchant(string id, CityRuntime city) => new NpcRuntime(id,
        SimulationTestFactory.CreateNpc(id, NpcJobType.Merchant), city, 100f);

    private static CommercialKnowledgeSharingDailyBoundaryStepProvider CreateProvider(
        IReadOnlyList<NpcRuntime> roster, IReadOnlyDictionary<string, NpcRuntime> actors)
    {
        CommercialKnowledgeSharingSystem owner = new CommercialKnowledgeSharingSystem(
            new SimulationTime(20L), new EffectiveCommercialKnowledgeConfiguration());
        return new CommercialKnowledgeSharingDailyBoundaryStepProvider(() => roster,
            id => actors.TryGetValue(id, out NpcRuntime npc) ? npc : null, owner);
    }

    private static string GetEdgeStepId(string sender, string receiver)
    {
        string[] parts = { "edge/v1", sender, receiver };
        StringBuilder encoded = new StringBuilder();
        foreach (string part in parts) encoded.Append(part.Length).Append(':').Append(part);
        return "commercial-share-edge:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(encoded.ToString()));
    }

    [Serializable] private sealed class FrozenSharePlan
    {
        public FrozenRosterMember[] runtimeRoster;
        public FrozenParticipant[] participants;
        public FrozenEdge[] edges;
    }
    [Serializable] private sealed class FrozenRosterMember { public string runtimeId; public string personId; }
    [Serializable] private sealed class FrozenParticipant { public string runtimeId; }
    [Serializable] private sealed class FrozenEdge { public string senderRuntimeId; public string receiverRuntimeId; }

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
