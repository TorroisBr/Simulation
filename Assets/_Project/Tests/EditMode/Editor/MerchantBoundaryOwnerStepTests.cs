using System.Collections.Generic;
using NUnit.Framework;

public sealed class MerchantBoundaryOwnerStepTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void PlanUrgencyStep_CommitsOnceAndResolvesTheSameReceiptOnReplay()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        ItemData item = SimulationTestFactory.CreateItem("merchant-urgency-item");
        NpcRuntime merchant = new NpcRuntime("merchant-urgency-npc",
            SimulationTestFactory.CreateNpc("merchant-urgency", NpcJobType.Merchant, MerchantBehavior.Traveling),
            world.A, 0f);
        BindPersonToNpc(merchant, "person.merchant-urgency");
        merchant.SetMerchantTradePlan(item, world.A, world.B, 4, 2f);

        MerchantSystem system = SimulationTestFactory.CreateMerchantSystem(null, new SimulationTime(1));
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world-1", "intraday-v1", 1L);
        Assert.That(system.TryCreatePlanUrgencyStep(operation, new[] { merchant }, 0,
            out BoundaryContinuationStep step, out TimelineFailure createFailure), Is.True);
        Assert.That(createFailure, Is.EqualTo(TimelineFailure.None));
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily", "1", "merchant-enabled", new[] { step });

        Assert.That(system.TryPreparePlanUrgencyStep(manifest, step, new[] { merchant },
            out IBoundaryContinuationStepCommit prepared, out TimelineFailure prepareFailure), Is.True);
        Assert.That(prepareFailure, Is.EqualTo(TimelineFailure.None));
        Assert.That(prepared.TryCommit(out TimelineFailure commitFailure), Is.True);
        Assert.That(commitFailure, Is.EqualTo(TimelineFailure.None));
        Assert.That(merchant.MerchantTradePlan.PendingTravelDays, Is.EqualTo(1));

        Assert.That(system.TryResolvePlanUrgencyReceipt(manifest, step,
            out MerchantPlanUrgencyReceipt receipt, out TimelineFailure resolveFailure), Is.True);
        Assert.That(resolveFailure, Is.EqualTo(TimelineFailure.None));
        Assert.That(receipt.OwnerRevisionBefore, Is.EqualTo(0L));
        Assert.That(receipt.OwnerRevisionAfter, Is.EqualTo(1L));
        Assert.That(system.TryPreparePlanUrgencyStep(manifest, step, new[] { merchant },
            out IBoundaryContinuationStepCommit replay, out _), Is.True);
        Assert.That(replay.TryCommit(out _), Is.True);
        Assert.That(merchant.MerchantTradePlan.PendingTravelDays, Is.EqualTo(1));
    }

    [Test]
    public void PlanUrgencyStep_RejectsMutationAfterPreparation()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        ItemData item = SimulationTestFactory.CreateItem("merchant-urgency-race-item");
        NpcRuntime merchant = new NpcRuntime("merchant-urgency-race-npc",
            SimulationTestFactory.CreateNpc("merchant-urgency-race", NpcJobType.Merchant, MerchantBehavior.Traveling),
            world.A, 0f);
        BindPersonToNpc(merchant, "person.merchant-urgency-race");
        merchant.SetMerchantTradePlan(item, world.A, world.B, 4, 2f);

        MerchantSystem system = SimulationTestFactory.CreateMerchantSystem(null, new SimulationTime(1));
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world-race", "intraday-v1", 1L);
        Assert.That(system.TryCreatePlanUrgencyStep(operation, new[] { merchant }, 0,
            out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily", "1", "merchant-enabled", new[] { step });
        Assert.That(system.TryPreparePlanUrgencyStep(manifest, step, new[] { merchant },
            out IBoundaryContinuationStepCommit prepared, out _), Is.True);

        merchant.MerchantTradePlan.IncrementPendingTravelDay();
        Assert.That(prepared.TryCommit(out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(merchant.MerchantTradePlan.PendingTravelDays, Is.EqualTo(1));
    }

    [Test]
    public void PlanUrgencyStep_RejectsInPlaceReplacementWithSameTargetAndPendingCount()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        ItemData originalItem = SimulationTestFactory.CreateItem("merchant-plan-original-item");
        ItemData replacementItem = SimulationTestFactory.CreateItem("merchant-plan-replacement-item");
        NpcRuntime merchant = new NpcRuntime("merchant-plan-replacement-npc",
            SimulationTestFactory.CreateNpc("merchant-plan-replacement", NpcJobType.Merchant, MerchantBehavior.Traveling),
            world.A, 0f);
        BindPersonToNpc(merchant, "person.merchant-plan-replacement");
        merchant.SetMerchantTradePlan(originalItem, world.A, world.B, 4, 2f, "decision.original");

        MerchantSystem system = SimulationTestFactory.CreateMerchantSystem(null, new SimulationTime(1));
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world-plan-replacement", "intraday-v1", 1L);
        Assert.That(system.TryCreatePlanUrgencyStep(operation, new[] { merchant }, 0,
            out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily", "1", "merchant-enabled", new[] { step });
        Assert.That(system.TryPreparePlanUrgencyStep(manifest, step, new[] { merchant },
            out IBoundaryContinuationStepCommit prepared, out _), Is.True);

        // Set replaces the active plan in place while preserving the target and pending count.
        merchant.SetMerchantTradePlan(replacementItem, world.A, world.B, 7, 3f, "decision.replacement");
        Assert.That(merchant.MerchantTradePlan.TargetCity, Is.SameAs(world.B));
        Assert.That(merchant.MerchantTradePlan.PendingTravelDays, Is.EqualTo(0));
        Assert.That(merchant.MerchantTradePlan.IsActive, Is.True);

        Assert.That(prepared.TryCommit(out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(merchant.MerchantTradePlan.Item, Is.SameAs(replacementItem));
        Assert.That(merchant.MerchantTradePlan.RemainingAmount, Is.EqualTo(7));
        Assert.That(merchant.MerchantTradePlan.PendingTravelDays, Is.EqualTo(0));
    }

    [Test]
    public void PlanUrgencyStep_RejectsSameTargetRedirectAfterPreparation()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        ItemData item = SimulationTestFactory.CreateItem("merchant-plan-redirect-item");
        NpcRuntime merchant = new NpcRuntime("merchant-plan-redirect-npc",
            SimulationTestFactory.CreateNpc("merchant-plan-redirect", NpcJobType.Merchant, MerchantBehavior.Traveling),
            world.A, 0f);
        BindPersonToNpc(merchant, "person.merchant-plan-redirect");
        merchant.SetMerchantTradePlan(item, world.A, world.B, 4, 2f, "decision.original");

        MerchantSystem system = SimulationTestFactory.CreateMerchantSystem(null, new SimulationTime(1));
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world-plan-redirect", "intraday-v1", 1L);
        Assert.That(system.TryCreatePlanUrgencyStep(operation, new[] { merchant }, 0,
            out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily", "1", "merchant-enabled", new[] { step });
        Assert.That(system.TryPreparePlanUrgencyStep(manifest, step, new[] { merchant },
            out IBoundaryContinuationStepCommit prepared, out _), Is.True);

        merchant.MerchantTradePlan.RedirectTo(world.B, "decision.redirected");
        Assert.That(merchant.MerchantTradePlan.TargetCity, Is.SameAs(world.B));
        Assert.That(merchant.MerchantTradePlan.PendingTravelDays, Is.EqualTo(0));
        Assert.That(merchant.MerchantTradePlan.OriginDecisionId, Is.EqualTo("decision.redirected"));

        Assert.That(prepared.TryCommit(out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(merchant.MerchantTradePlan.PendingTravelDays, Is.EqualTo(0));
    }

    [Test]
    public void PlanUrgencyDescriptor_FreezesRosterOrderAndStablePersonIdentity()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        NpcRuntime first = CreateMerchant("merchant-roster-first", "person.roster-first", world.A);
        NpcRuntime second = CreateMerchant("merchant-roster-second", "person.roster-second", world.A);
        MerchantSystem system = SimulationTestFactory.CreateMerchantSystem(null, new SimulationTime(1));
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world-roster", "intraday-v1", 1L);

        Assert.That(system.TryCreatePlanUrgencyStep(operation, new[] { first, second }, 0,
            out BoundaryContinuationStep firstOrder, out _), Is.True);
        Assert.That(system.TryCreatePlanUrgencyStep(operation, new[] { second, first }, 0,
            out BoundaryContinuationStep reversedOrder, out _), Is.True);
        Assert.That(firstOrder.Payload, Is.Not.EqualTo(reversedOrder.Payload));
    }

    [Test]
    public void P18DTradeStateOccurrenceObservesPostShareMarketAndReplaysWithoutRepeatingEffects()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        ItemData item = SimulationTestFactory.CreateItem("merchant-p18d-state-item", 10f);
        world.A.Market.AddStock(item, 10, 20);
        NpcRuntime merchant = CreateMerchant("merchant-p18d-state-npc",
            "person.merchant-p18d-state", world.A);
        merchant.Inventory.AddItem(item, 4, 1f);
        merchant.SetMerchantTradePlan(item, world.A, world.A, 4, 1f);
        for (int i = 0; i < 3; i++) merchant.MerchantTradePlan.IncrementWaitDayAtDestination();

        SimulationTime time = new SimulationTime(6L);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture(6L);
        MerchantSystem system = SimulationTestFactory.CreateMerchantSystem(
            null, time, records.DecisionRecorder);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("merchant-p18d-world",
            "intraday", 6L);
        BoundaryContinuationStep step = new BoundaryContinuationStep(0,
            "merchant-trade-state:" + merchant.RuntimeId, merchant.RuntimeId,
            "merchant.trade-state", "1", "frozen-roster-v1", "merchant-enabled",
            merchant.PersonId.Value);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "configuration/v1", new[] { step }, "content/v1");

        Assert.That(system.TryAdvanceNpcTradeStateOccurrence(merchant, manifest, step,
            out NpcMerchantTradeStateReceipt receipt, out TimelineFailure failure), Is.True,
            failure.ToString());
        Assert.That(receipt, Is.Not.Null);
        Assert.That(merchant.MerchantTradePlan.WaitDaysAtDestination, Is.Zero);
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(world.A.Location.RuntimeId,
            item.DefinitionId, out CommercialMarketObservation observation), Is.True);
        Assert.That(observation.ObservedDay, Is.EqualTo(6L));
        long commercialRevision = merchant.CommercialKnowledge.Revision;
        long spatialRevision = merchant.SpatialKnowledge.Revision;
        float firstPrice = observation.ObservedPrice;

        item.basePrice += 25f;
        world.A.UpdateMarketPrices();
        Assert.That(system.TryAdvanceNpcTradeStateOccurrence(merchant, manifest, step,
            out NpcMerchantTradeStateReceipt replay, out failure), Is.True, failure.ToString());
        Assert.That(replay, Is.Not.Null);
        Assert.That(merchant.MerchantTradePlan.WaitDaysAtDestination, Is.Zero);
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(world.A.Location.RuntimeId,
            item.DefinitionId, out CommercialMarketObservation replayed), Is.True);
        Assert.That(replayed.ObservedPrice, Is.EqualTo(firstPrice));
        Assert.That(merchant.CommercialKnowledge.Revision, Is.EqualTo(commercialRevision));
        Assert.That(merchant.SpatialKnowledge.Revision, Is.EqualTo(spatialRevision));
        Assert.That(records.Decisions.Decisions, Is.Empty);
    }

    [Test]
    public void P18DTradeStateRedirectRetainsOneDecisionAndOneKeyedDiagnostic()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        ItemData item = SimulationTestFactory.CreateItem("merchant-p18d-redirect-item", 10f);
        world.A.Market.AddStock(item, 10, 20);
        NpcRuntime merchant = CreateMerchant("merchant-p18d-redirect-npc",
            "person.merchant-p18d-redirect", world.A);
        merchant.AddMoney(100f);
        merchant.Inventory.AddItem(item, 4, 1f);
        merchant.SetMerchantTradePlan(item, world.A, world.A, 4, 100f);
        merchant.MerchantTradePlan.IncrementWaitDayAtDestination();
        merchant.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
        merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            world.B.Location.RuntimeId, item, 150f, 10, 6L, 6L));

        SimulationTime time = new SimulationTime(6L);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture(6L);
        SimulationLogger logger = new SimulationLogger(new SimulationLogSettings { trade = true });
        MerchantSystem system = new MerchantSystem(
            new EffectiveMerchantTradeConfiguration(enabled: true),
            new EffectiveCommercialKnowledgeConfiguration(), world.CreateTravelSystem(time),
            time, records.DecisionRecorder, logger);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("merchant-p18d-redirect-world",
            "intraday", 6L);
        BoundaryContinuationStep step = new BoundaryContinuationStep(0,
            "merchant-trade-state:" + merchant.RuntimeId, merchant.RuntimeId,
            "merchant.trade-state", "1", "frozen-roster-v1", "merchant-enabled",
            merchant.PersonId.Value);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "configuration/v1", new[] { step }, "content/v1");

        Assert.That(system.TryAdvanceNpcTradeStateOccurrence(merchant, manifest, step,
            out NpcMerchantTradeStateReceipt receipt, out TimelineFailure failure), Is.True,
            failure.ToString());
        Assert.That(receipt, Is.Not.Null);
        Assert.That(merchant.MerchantTradePlan.TargetCity, Is.SameAs(world.B));
        Assert.That(merchant.MerchantTradePlan.OriginDecisionId, Is.Not.Empty);
        Assert.That(merchant.TravelPlan.TargetCity, Is.SameAs(world.B));
        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(1));
        Assert.That(records.Decisions.Decisions[0].DecisionType, Is.EqualTo(NpcDecisionType.TradeRedirect));
        string firstLog = logger.FullLog;
        Assert.That(firstLog, Does.Contain("mudou o destino"));

        merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            world.B.Location.RuntimeId, item, 1f, 0, 6L, 6L));
        Assert.That(system.TryAdvanceNpcTradeStateOccurrence(merchant, manifest, step,
            out NpcMerchantTradeStateReceipt replay, out failure), Is.True, failure.ToString());
        Assert.That(replay, Is.Not.Null);
        Assert.That(merchant.MerchantTradePlan.TargetCity, Is.SameAs(world.B));
        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(1));
        Assert.That(logger.FullLog, Is.EqualTo(firstLog));
    }

    private static NpcRuntime CreateMerchant(string runtimeId, string personId, CityRuntime city)
    {
        NpcRuntime merchant = new NpcRuntime(runtimeId,
            SimulationTestFactory.CreateNpc(runtimeId, NpcJobType.Merchant, MerchantBehavior.Traveling), city, 0f);
        BindPersonToNpc(merchant, personId);
        return merchant;
    }

    private static void BindPersonToNpc(NpcRuntime npc, string personIdValue)
    {
        PersonId personId = new PersonId(personIdValue);
        SimulationRuntime world = new SimulationRuntime(new SimulationTime(), null, new[] { npc });
        Assert.That(world.TryRegisterPerson(new PersonRuntime(personId), out PersonStoreFailure registrationFailure),
            Is.True, registrationFailure.ToString());
        Assert.That(world.TryBindExistingNpcToPerson(personId, npc.RuntimeId,
            out PersonMaterializationFailure bindingFailure), Is.True, bindingFailure.ToString());
    }
}
