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
        CommercialKnowledgePolicy policy = new CommercialKnowledgePolicy(
            new EffectiveCommercialKnowledgeConfiguration(7, 30, 2));

        Assert.That(policy.GetAgeDays(observation, 25L), Is.EqualTo(15L));
        Assert.That(policy.GetFreshness(observation, 25L), Is.LessThan(1f));
        Assert.That(policy.GetFreshness(observation, 25L), Is.GreaterThan(0f));
    }

    [Test]
    public void ShareBatch_ReceiptReplaysWithoutReapplyingAndRevisionConflictIsAtomic()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-share-receipt");
        CommercialKnowledgeRuntime source = new CommercialKnowledgeRuntime();
        source.RecordObservation(SimulationTestFactory.CreateObservation("location-a", item, 20f, 4, 2, 2));
        CommercialKnowledgeShareValue value = CommercialKnowledgeShareValue.Capture(source.Observations[0]);
        CommercialKnowledgeShareBatch batch = new CommercialKnowledgeShareBatch("boundary", "sender", "person-s",
            "receiver", "person-r", "snapshot", 3L, 1, new[] { value });
        CommercialKnowledgeRuntime receiver = new CommercialKnowledgeRuntime();
        Assert.That(receiver.TryPrepareShareBatch(batch, out CommercialKnowledgeShareBatchCommit stale), Is.True);
        Assert.That(receiver.RecordObservation(SimulationTestFactory.CreateObservation("location-b", item, 10f, 1, 1, 1)), Is.True);
        long afterLocalObservation = receiver.Revision;
        Assert.That(stale.TryCommit(out _), Is.False);
        Assert.That(receiver.Revision, Is.EqualTo(afterLocalObservation));
        Assert.That(receiver.TryGetObservation("location-a", item.DefinitionId, out _), Is.False);

        Assert.That(receiver.TryPrepareShareBatch(batch, out CommercialKnowledgeShareBatchCommit first), Is.True);
        Assert.That(first.TryCommit(out CommercialKnowledgeShareReceipt committed), Is.True);
        long afterCommit = receiver.Revision;
        Assert.That(receiver.TryPrepareShareBatch(batch, out CommercialKnowledgeShareBatchCommit replay), Is.True);
        Assert.That(replay.TryCommit(out CommercialKnowledgeShareReceipt replayed), Is.True);
        Assert.That(replayed, Is.SameAs(committed));
        Assert.That(receiver.Revision, Is.EqualTo(afterCommit));
        Assert.That(committed.AppliedObservationCount, Is.EqualTo(1));
    }

    [Test]
    public void ShareBatch_RejectsConflictingReplayForSameEdgeIdentity()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-share-conflict");
        CommercialKnowledgeRuntime sender = new CommercialKnowledgeRuntime();
        sender.RecordObservation(SimulationTestFactory.CreateObservation("location-a", item, 20f, 4, 2, 2));
        CommercialKnowledgeShareValue firstValue = CommercialKnowledgeShareValue.Capture(sender.Observations[0]);
        CommercialKnowledgeShareBatch first = new CommercialKnowledgeShareBatch("boundary", "sender", "person-s",
            "receiver", "person-r", "snapshot-one", 3L, 1, new[] { firstValue });
        CommercialKnowledgeRuntime receiver = new CommercialKnowledgeRuntime();
        Assert.That(receiver.TryPrepareShareBatch(first, out CommercialKnowledgeShareBatchCommit commit), Is.True);
        Assert.That(commit.TryCommit(out _), Is.True);

        CommercialKnowledgeShareBatch conflict = new CommercialKnowledgeShareBatch("boundary", "sender", "person-s",
            "receiver", "person-r", "snapshot-two", 3L, 1, new[] { firstValue });
        Assert.That(receiver.TryPrepareShareBatch(conflict, out _), Is.False);
    }

    [Test]
    public void ShareBatch_InvalidValueLeavesRecipientAndSourceOwnersUnchanged()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-share-invalid");
        CommercialKnowledgeRuntime source = new CommercialKnowledgeRuntime();
        CommercialMarketObservation sourceObservation = SimulationTestFactory.CreateObservation(
            "location-a", item, 20f, 4, 2, 2);
        source.RecordObservation(sourceObservation);
        long sourceRevision = source.Revision;
        CommercialKnowledgeShareValue value = CommercialKnowledgeShareValue.Capture(sourceObservation);
        CommercialKnowledgeShareBatch batch = new CommercialKnowledgeShareBatch("boundary", "sender", "person-s",
            "receiver", "person-r", "snapshot", 3L, 1, new[] { value });
        CommercialKnowledgeRuntime receiver = new CommercialKnowledgeRuntime();
        item.id = "item-mutated-after-capture";

        Assert.That(receiver.TryPrepareShareBatch(batch, out _), Is.False);
        Assert.That(receiver.Observations, Is.Empty);
        Assert.That(receiver.Revision, Is.Zero);
        Assert.That(receiver.TryGetShareReceipt(batch.OperationIdentity, out _), Is.False);
        Assert.That(source.Revision, Is.EqualTo(sourceRevision));
        Assert.That(source.Observations, Has.Count.EqualTo(1));
        Assert.That(source.Observations[0], Is.SameAs(sourceObservation));
        Assert.That(sourceObservation.ObservedPrice, Is.EqualTo(20f));
        Assert.That(value.ItemDefinitionId, Is.EqualTo("item-share-invalid"));
    }

    [Test]
    public void ShareBatch_NoOpEdgeStillRetainsReplayableReceipt()
    {
        CommercialKnowledgeShareBatch noOp = new CommercialKnowledgeShareBatch("boundary-noop", "sender", "person-s",
            "receiver", "person-r", "empty-snapshot", 3L, 2, System.Array.Empty<CommercialKnowledgeShareValue>());
        CommercialKnowledgeRuntime receiver = new CommercialKnowledgeRuntime();
        Assert.That(receiver.TryPrepareShareBatch(noOp, out CommercialKnowledgeShareBatchCommit prepared), Is.True);
        Assert.That(prepared.TryCommit(out CommercialKnowledgeShareReceipt committed), Is.True);
        Assert.That(committed.AppliedObservationCount, Is.Zero);
        long afterReceipt = receiver.Revision;

        ItemData localItem = SimulationTestFactory.CreateItem("item-local-after-noop");
        receiver.RecordObservation(SimulationTestFactory.CreateObservation("location-local", localItem, 5f, 1, 3, 3));
        long afterLocalChange = receiver.Revision;
        Assert.That(receiver.TryPrepareShareBatch(noOp, out CommercialKnowledgeShareBatchCommit replay), Is.True);
        Assert.That(replay.TryCommit(out CommercialKnowledgeShareReceipt replayed), Is.True);
        Assert.That(replayed, Is.SameAs(committed));
        Assert.That(receiver.Revision, Is.EqualTo(afterLocalChange));
        Assert.That(afterLocalChange, Is.GreaterThan(afterReceipt));
    }

    [Test]
    public void ShareBatch_CountsOnlySuccessfulUpdatesAndPreservesFrozenSourceOrder()
    {
        ItemData newest = SimulationTestFactory.CreateItem("item-newest");
        ItemData second = SimulationTestFactory.CreateItem("item-second");
        ItemData third = SimulationTestFactory.CreateItem("item-third");
        CommercialKnowledgeRuntime source = new CommercialKnowledgeRuntime();
        CommercialMarketObservation sourceNewest = SimulationTestFactory.CreateObservation("location-a", newest, 30f, 3, 3, 3);
        CommercialMarketObservation sourceSecond = SimulationTestFactory.CreateObservation("location-b", second, 20f, 2, 2, 2);
        CommercialMarketObservation sourceThird = SimulationTestFactory.CreateObservation("location-c", third, 10f, 1, 1, 1);
        source.RecordObservation(sourceNewest);
        source.RecordObservation(sourceSecond);
        source.RecordObservation(sourceThird);
        long sourceRevision = source.Revision;
        CommercialKnowledgeShareBatch batch = new CommercialKnowledgeShareBatch("boundary-order", "sender", "person-s",
            "receiver", "person-r", "snapshot-order", 4L, 1, new[]
            {
                CommercialKnowledgeShareValue.Capture(sourceNewest),
                CommercialKnowledgeShareValue.Capture(sourceSecond),
                CommercialKnowledgeShareValue.Capture(sourceThird)
            });
        CommercialKnowledgeRuntime receiver = new CommercialKnowledgeRuntime();
        receiver.RecordObservation(SimulationTestFactory.CreateObservation("location-a", newest, 99f, 9, 4, 4));

        Assert.That(receiver.TryPrepareShareBatch(batch, out CommercialKnowledgeShareBatchCommit prepared), Is.True);
        Assert.That(prepared.TryCommit(out CommercialKnowledgeShareReceipt receipt), Is.True);
        Assert.That(receipt.AppliedObservationCount, Is.EqualTo(1));
        Assert.That(receiver.TryGetObservation("location-b", second.DefinitionId, out CommercialMarketObservation applied), Is.True);
        Assert.That(applied.ObservedPrice, Is.EqualTo(20f));
        Assert.That(receiver.TryGetObservation("location-c", third.DefinitionId, out _), Is.False);
        Assert.That(receiver.TryGetObservation("location-a", newest.DefinitionId, out CommercialMarketObservation retained), Is.True);
        Assert.That(retained.ObservedPrice, Is.EqualTo(99f));
        Assert.That(source.Revision, Is.EqualTo(sourceRevision));
        Assert.That(source.Observations[0], Is.SameAs(sourceNewest));
        Assert.That(sourceNewest.Source, Is.EqualTo(CommercialKnowledgeSource.DirectObservation));
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
            new EffectiveMerchantTradeConfiguration(enabled: true),
            new EffectiveCommercialKnowledgeConfiguration(),
            world.CreateTravelSystem(time),
            time,
            null);

        system.BootstrapInitialKnowledge(merchant);

        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            world.A.Location.RuntimeId, item.DefinitionId, out CommercialMarketObservation local), Is.True);
        Assert.That(local.Source, Is.EqualTo(CommercialKnowledgeSource.InitialScenarioKnowledge));
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            world.B.Location.RuntimeId, item.DefinitionId, out CommercialMarketObservation neighbor), Is.True);
        Assert.That(neighbor.Source, Is.EqualTo(CommercialKnowledgeSource.InitialScenarioKnowledge));
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            world.C.Location.RuntimeId, item.DefinitionId, out _), Is.False);
        Assert.That(merchant.CommercialKnowledge.TryGetLiquidityObservation(
            world.A.Location.RuntimeId, out CommercialLiquidityObservation localLiquidity), Is.True);
        Assert.That(localLiquidity.Source, Is.EqualTo(CommercialKnowledgeSource.InitialScenarioKnowledge));
        Assert.That(merchant.CommercialKnowledge.TryGetLiquidityObservation(
            world.B.Location.RuntimeId, out CommercialLiquidityObservation neighborLiquidity), Is.True);
        Assert.That(neighborLiquidity.Source, Is.EqualTo(CommercialKnowledgeSource.InitialScenarioKnowledge));
        Assert.That(merchant.CommercialKnowledge.TryGetLiquidityObservation(
            world.C.Location.RuntimeId, out _), Is.False);
    }

    [Test]
    public void LiquidityObservation_OpenModeUsesFiniteSentinelInsteadOfInfinity()
    {
        CommercialLiquidityObservation observation = SimulationTestFactory.CreateLiquidityObservation(
            "location-open", MarketLiquidityMode.Open, float.PositiveInfinity, 3L, 3L);

        SimulationInvariantValidator.ValidateCommercialLiquidityObservation(observation);
        Assert.That(observation.ObservedPurchasingPower, Is.EqualTo(0f));
        Assert.That(float.IsInfinity(observation.ObservedPurchasingPower), Is.False);
    }

    [Test]
    public void LiquidityObservation_SameDayDirectObservationBeatsSharedKnowledge()
    {
        CommercialKnowledgeRuntime knowledge = new CommercialKnowledgeRuntime();
        knowledge.RecordLiquidityObservation(SimulationTestFactory.CreateLiquidityObservation(
            "location-a", MarketLiquidityMode.AccountBacked, 90f, 10L, 20L,
            CommercialKnowledgeSource.SharedByNpc, "npc-source"));

        bool replaced = knowledge.RecordLiquidityObservation(SimulationTestFactory.CreateLiquidityObservation(
            "location-a", MarketLiquidityMode.AccountBacked, 25f, 10L, 10L,
            CommercialKnowledgeSource.DirectObservation));

        Assert.That(replaced, Is.True);
        knowledge.TryGetLiquidityObservation("location-a", out CommercialLiquidityObservation current);
        Assert.That(current.ObservedPurchasingPower, Is.EqualTo(25f));
        Assert.That(current.Source, Is.EqualTo(CommercialKnowledgeSource.DirectObservation));
    }

    [Test]
    public void LiquidityFreshness_UsesObservedDayInsteadOfReceivedDay()
    {
        CommercialLiquidityObservation observation = SimulationTestFactory.CreateLiquidityObservation(
            "location-a", MarketLiquidityMode.AccountBacked, 50f, 10L, 20L,
            CommercialKnowledgeSource.SharedByNpc, "npc-source");
        CommercialKnowledgePolicy policy = new CommercialKnowledgePolicy(
            new EffectiveCommercialKnowledgeConfiguration(7, 30, 2));

        Assert.That(policy.GetAgeDays(observation, 25L), Is.EqualTo(15L));
        Assert.That(policy.GetFreshness(observation, 25L), Is.InRange(0.01f, 0.99f));
    }

    [Test]
    public void DirectLiquidityObservation_IsSnapshotUntilMerchantObservesAgain()
    {
        ItemData item = SimulationTestFactory.CreateItem("snapshot-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity(
            "city-snapshot", "location-snapshot", 75f, SimulationTestFactory.CreateMarketItem(item, 10, 10));
        NpcRuntime merchant = new NpcRuntime(
            "npc-merchant", SimulationTestFactory.CreateNpc("merchant", NpcJobType.Merchant), city, 100f);
        NpcRuntime buyer = new NpcRuntime("npc-buyer", SimulationTestFactory.CreateNpc("buyer"), city, 20f);
        SimulationTime time = new SimulationTime();
        MerchantSystem system = SimulationTestFactory.CreateMerchantSystem(null, time);

        system.ObserveCurrentMarket(merchant);
        Assert.That(new EconomyTransactionService().TryExecuteMarketPurchase(buyer, city.Market, item, 1).Success, Is.True);
        merchant.CommercialKnowledge.TryGetLiquidityObservation(city.Location.RuntimeId, out CommercialLiquidityObservation oldSnapshot);

        Assert.That(oldSnapshot.ObservedPurchasingPower, Is.EqualTo(75f));
        time.AdvanceDay();
        system.ObserveCurrentMarket(merchant);
        merchant.CommercialKnowledge.TryGetLiquidityObservation(city.Location.RuntimeId, out CommercialLiquidityObservation refreshed);
        Assert.That(refreshed.ObservedPurchasingPower, Is.EqualTo(85f));
        Assert.That(refreshed.ObservedDay, Is.EqualTo(1L));
    }
}
