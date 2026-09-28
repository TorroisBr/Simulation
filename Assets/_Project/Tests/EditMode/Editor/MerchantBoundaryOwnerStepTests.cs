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
        Assert.That(merchant.TryAssignPersonId(new PersonId("person.merchant-urgency")), Is.True);
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
        Assert.That(merchant.TryAssignPersonId(new PersonId("person.merchant-urgency-race")), Is.True);
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
        Assert.That(merchant.TryAssignPersonId(new PersonId("person.merchant-plan-replacement")), Is.True);
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
        Assert.That(merchant.TryAssignPersonId(new PersonId("person.merchant-plan-redirect")), Is.True);
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

    private static NpcRuntime CreateMerchant(string runtimeId, string personId, CityRuntime city)
    {
        NpcRuntime merchant = new NpcRuntime(runtimeId,
            SimulationTestFactory.CreateNpc(runtimeId, NpcJobType.Merchant, MerchantBehavior.Traveling), city, 0f);
        Assert.That(merchant.TryAssignPersonId(new PersonId(personId)), Is.True);
        return merchant;
    }
}
