using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1NpcMerchantTradeStateReceiptOwnerTests
{
    [Test]
    public void CoreReceiptOperationCommitsSelectedPlanTravelAndReceiptTogetherAndReplaysExactly()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city);
        DailyV1NpcMerchantTradeStateReceiptOwner receipts = actor.MerchantTradeStateReceipts;
        DailyV1MerchantTradePlanReceiptState expectedPlan = CapturePlan(actor);
        DailyV1NpcTravelPlanReceiptState expectedTravel = CaptureTravel(actor);
        DailyV1MerchantTradePlanReceiptState nextPlan = new DailyV1MerchantTradePlanReceiptState(
            actor.Definitions.Items.Values.First(), city.RuntimeId, city.RuntimeId,
            2, 2, 4.5f, 0, 0, "merchant-decision");
        DailyV1NpcTravelPlanReceiptState nextTravel = new DailyV1NpcTravelPlanReceiptState(
            city.LocationRuntimeId, city.RuntimeId, NpcTravelReason.Trade,
            70f, 3f, "merchant-decision");
        int admissionCalls = 0;
        int planPublications = 0;
        int travelPublications = 0;

        Assert.That(receipts.TryPrepareInstall(graph, actor, string.Empty,
            "occurrence-1", "fingerprint-1", expectedPlan, expectedTravel,
            nextPlan, nextTravel, () => { admissionCalls++; return true; },
            (planChanged, travelChanged) =>
            {
                if (planChanged) planPublications++;
                if (travelChanged) travelPublications++;
            }, out DailyV1NpcMerchantTradeStateReceiptCommit prepared), Is.True);
        Assert.That(actor.MerchantTradePlan.Revision, Is.Zero,
            "Preparation must not mutate the Core plan owner.");
        Assert.That(actor.TravelPlan.Revision, Is.Zero,
            "Preparation must not mutate the Core TravelPlan owner.");
        Assert.That(receipts.Revision, Is.Zero,
            "Preparation must not publish a receipt or its revision.");

        Assert.That(prepared.TryCommit(out TimelineFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(admissionCalls, Is.EqualTo(1));
        Assert.That(actor.MerchantTradePlan.MatchesState(nextPlan.Item,
            nextPlan.OriginCityRuntimeId, nextPlan.TargetCityRuntimeId,
            nextPlan.PlannedAmount, nextPlan.RawRemainingAmount,
            nextPlan.PurchasePricePerItem, nextPlan.WaitDaysAtDestination,
            nextPlan.PendingTravelDays, nextPlan.OriginDecisionId), Is.True);
        Assert.That(actor.MerchantTradePlan.Revision, Is.EqualTo(1));
        Assert.That(actor.TravelPlan.Matches(nextTravel.TargetLocationRuntimeId,
            nextTravel.TargetCityRuntimeId, nextTravel.Reason,
            nextTravel.Utility, nextTravel.ExpectedCost,
            nextTravel.OriginDecisionId), Is.True);
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(1));
        Assert.That(receipts.TryReadP12ReceiptCensus(out int cardinality,
            out long revision), Is.True);
        Assert.That(cardinality, Is.EqualTo(1));
        Assert.That(revision, Is.EqualTo(1));
        Assert.That(planPublications, Is.EqualTo(1));
        Assert.That(travelPublications, Is.EqualTo(1));
        Assert.That(receipts.TryResolve("occurrence-1", "fingerprint-1",
            out DailyV1NpcMerchantTradeStateReceipt receipt), Is.True);
        Assert.That(receipt.ActorRuntimeId, Is.EqualTo(actor.RuntimeId));
        Assert.That(receipt.OwnerRevisionBefore, Is.Zero);
        Assert.That(receipt.OwnerRevisionAfter, Is.EqualTo(1));

        Assert.That(receipts.TryPrepareInstall(graph, actor, string.Empty,
            "occurrence-1", "fingerprint-1", expectedPlan, expectedTravel,
            nextPlan, nextTravel, () => { admissionCalls++; return true; },
            (planChanged, travelChanged) => Assert.Fail("A replay must not republish owner writes."),
            out DailyV1NpcMerchantTradeStateReceiptCommit replay), Is.True);
        Assert.That(replay.TryCommit(out failure), Is.True);
        Assert.That(actor.MerchantTradePlan.Revision, Is.EqualTo(1));
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(1));
        Assert.That(receipts.Revision, Is.EqualTo(1));
        Assert.That(admissionCalls, Is.EqualTo(1),
            "An exact replay is resolved from the Core receipt without repeating mutation admission.");
    }

    [Test]
    public void RejectedOrThrowingAdmissionLeavesBothOwnersAndReceiptUnchanged()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city);
        DailyV1NpcMerchantTradeStateReceiptOwner receipts = actor.MerchantTradeStateReceipts;
        DailyV1MerchantTradePlanReceiptState expectedPlan = CapturePlan(actor);
        DailyV1NpcTravelPlanReceiptState expectedTravel = CaptureTravel(actor);
        DailyV1MerchantTradePlanReceiptState nextPlan = new DailyV1MerchantTradePlanReceiptState(
            actor.Definitions.Items.Values.First(), city.RuntimeId, city.RuntimeId,
            1, 1, 2f, 0, 0, "decision");
        DailyV1NpcTravelPlanReceiptState nextTravel = new DailyV1NpcTravelPlanReceiptState(
            city.LocationRuntimeId, city.RuntimeId, NpcTravelReason.Trade,
            10f, 1f, "decision");

        AssertRejectedAdmission(graph, actor, receipts, expectedPlan,
            expectedTravel, nextPlan, nextTravel, () => false);
        AssertRejectedAdmission(graph, actor, receipts, expectedPlan,
            expectedTravel, nextPlan, nextTravel,
            () => throw new InvalidOperationException("injected admission failure"));

        Assert.That(actor.MerchantTradePlan.Revision, Is.Zero);
        Assert.That(actor.MerchantTradePlan.HasData, Is.False);
        Assert.That(actor.TravelPlan.Revision, Is.Zero);
        Assert.That(actor.TravelPlan.TargetLocationRuntimeId, Is.Null);
        Assert.That(receipts.TryReadP12ReceiptCensus(out int cardinality,
            out long revision), Is.True);
        Assert.That(cardinality, Is.Zero);
        Assert.That(revision, Is.Zero);
    }

    [Test]
    public void CoreReceiptRejectsStaleOwnerStateAndFingerprintCollisionWithoutMutation()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city);
        DailyV1NpcMerchantTradeStateReceiptOwner receipts = actor.MerchantTradeStateReceipts;
        DailyV1MerchantTradePlanReceiptState expectedPlan = CapturePlan(actor);
        DailyV1NpcTravelPlanReceiptState expectedTravel = CaptureTravel(actor);
        DailyV1MerchantTradePlanReceiptState nextPlan = new DailyV1MerchantTradePlanReceiptState(
            actor.Definitions.Items.Values.First(), city.RuntimeId, city.RuntimeId,
            1, 1, 2f, 0, 0, "decision");
        DailyV1NpcTravelPlanReceiptState nextTravel = CaptureTravel(actor);

        Assert.That(actor.MerchantTradePlan.TryInstallState(nextPlan.Item,
            nextPlan.OriginCityRuntimeId, nextPlan.TargetCityRuntimeId,
            nextPlan.PlannedAmount, nextPlan.RawRemainingAmount,
            nextPlan.PurchasePricePerItem, nextPlan.WaitDaysAtDestination,
            nextPlan.PendingTravelDays, nextPlan.OriginDecisionId), Is.True);
        Assert.That(receipts.TryPrepareInstall(graph, actor, string.Empty,
            "stale-operation", "fingerprint", expectedPlan, expectedTravel,
            nextPlan, nextTravel, () => true, null, out _), Is.False,
            "A plan changed after the supplied expected state must not prepare a receipt.");
        Assert.That(receipts.TryReadP12ReceiptCensus(out int cardinality,
            out long revision), Is.True);
        Assert.That(cardinality, Is.Zero);
        Assert.That(revision, Is.Zero);

        DailyV1MerchantTradePlanReceiptState collisionExpectedPlan = CapturePlan(actor);
        DailyV1NpcTravelPlanReceiptState collisionExpectedTravel = CaptureTravel(actor);
        DailyV1MerchantTradePlanReceiptState clearPlan =
            new DailyV1MerchantTradePlanReceiptState(null, null, null,
                0, 0, 0f, 0, 0, null);
        Assert.That(receipts.TryPrepareInstall(graph, actor, string.Empty,
            "collision-operation", "first-fingerprint", collisionExpectedPlan,
            collisionExpectedTravel, clearPlan, collisionExpectedTravel,
            () => true, null,
            out DailyV1NpcMerchantTradeStateReceiptCommit firstCollisionCommit), Is.True);
        Assert.That(firstCollisionCommit.TryCommit(out _), Is.True);
        long committedPlanRevision = actor.MerchantTradePlan.Revision;
        long committedTravelRevision = actor.TravelPlan.Revision;
        long committedReceiptRevision = receipts.Revision;
        int collisionAdmissionCalls = 0;
        DailyV1MerchantTradePlanReceiptState committedPlan = CapturePlan(actor);
        DailyV1NpcTravelPlanReceiptState committedTravel = CaptureTravel(actor);

        Assert.That(receipts.TryPrepareInstall(graph, actor, string.Empty,
            "collision-operation", "different-fingerprint", committedPlan,
            committedTravel, committedPlan, committedTravel,
            () => { collisionAdmissionCalls++; return true; },
            null, out _), Is.False);
        Assert.That(collisionAdmissionCalls, Is.Zero,
            "A fingerprint collision must be rejected before mutation admission.");
        Assert.That(actor.MerchantTradePlan.Revision, Is.EqualTo(committedPlanRevision));
        Assert.That(actor.MerchantTradePlan.MatchesState(null, null, null,
            0, 0, 0f, 0, 0, null), Is.True);
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(committedTravelRevision));
        Assert.That(receipts.Revision, Is.EqualTo(committedReceiptRevision));
        Assert.That(receipts.TryReadP12ReceiptCensus(out cardinality, out revision), Is.True);
        Assert.That(cardinality, Is.EqualTo(1));
        Assert.That(revision, Is.EqualTo(1));
    }

    private static void AssertRejectedAdmission(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcMerchantTradeStateReceiptOwner receipts,
        DailyV1MerchantTradePlanReceiptState expectedPlan,
        DailyV1NpcTravelPlanReceiptState expectedTravel,
        DailyV1MerchantTradePlanReceiptState nextPlan,
        DailyV1NpcTravelPlanReceiptState nextTravel,
        Func<bool> admission)
    {
        Assert.That(receipts.TryPrepareInstall(graph, actor, string.Empty,
            "rejected-operation", "fingerprint", expectedPlan, expectedTravel,
            nextPlan, nextTravel, admission, null,
            out DailyV1NpcMerchantTradeStateReceiptCommit prepared), Is.True);
        Assert.That(prepared.TryCommit(out _), Is.False);
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        out DailyV1NpcRuntimeOwners actor,
        out DailyV1CityRuntimeOwners city)
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        SpatialNetworkOwner spatialNetwork = new SpatialNetworkOwner();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions, spatialNetwork);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.Cities.Count, Is.GreaterThan(0));
        Assert.That(graph.Npcs.Count, Is.GreaterThan(0));
        actor = graph.Npcs[0];
        city = graph.Cities[0];
        return graph;
    }

    private static DailyV1MerchantTradePlanReceiptState CapturePlan(
        DailyV1NpcRuntimeOwners actor) => new DailyV1MerchantTradePlanReceiptState(
            actor.MerchantTradePlan.ItemDefinition,
            actor.MerchantTradePlan.OriginCityRuntimeId,
            actor.MerchantTradePlan.TargetCityRuntimeId,
            actor.MerchantTradePlan.PlannedAmount,
            actor.MerchantTradePlan.RawRemainingAmount,
            actor.MerchantTradePlan.PurchasePricePerItem,
            actor.MerchantTradePlan.WaitDaysAtDestination,
            actor.MerchantTradePlan.PendingTravelDays,
            actor.MerchantTradePlan.OriginDecisionId);

    private static DailyV1NpcTravelPlanReceiptState CaptureTravel(
        DailyV1NpcRuntimeOwners actor) => new DailyV1NpcTravelPlanReceiptState(
            actor.TravelPlan.TargetLocationRuntimeId,
            actor.TravelPlan.TargetCityRuntimeId,
            actor.TravelPlan.Reason, actor.TravelPlan.Utility,
            actor.TravelPlan.ExpectedCost, actor.TravelPlan.OriginDecisionId);
}
