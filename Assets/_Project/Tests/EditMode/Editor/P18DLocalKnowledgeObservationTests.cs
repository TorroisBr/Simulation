using System.Collections.Generic;
using NUnit.Framework;

public sealed class P18DLocalKnowledgeObservationTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void ActorStepAtomicallyRecordsSpatialAndMarketKnowledgeAndReplaysCapturedValues()
    {
        ItemData item = SimulationTestFactory.CreateItem("p18d-local-observation-item", 12f);
        CityRuntime city = SimulationTestFactory.CreateCity("p18d-local-observation-city",
            "p18d-local-observation-location",
            new MarketItemConfig { item = item, initialAmount = 7, desiredAmount = 10 });
        NpcRuntime merchant = new NpcRuntime("p18d-local-observation-merchant",
            SimulationTestFactory.CreateNpc("p18d-local-observation-merchant", NpcJobType.Merchant),
            city, 20f);
        List<NpcRuntime> liveRoster = new List<NpcRuntime> { merchant };
        NpcLocalKnowledgeDailyBoundaryStepProvider provider =
            new NpcLocalKnowledgeDailyBoundaryStepProvider(() => liveRoster, true);
        DailyBoundaryOperation operation = new DailyBoundaryOperation(
            "p18d-local-observation-world", "intraday", 1L);

        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(steps.Count, Is.EqualTo(1));
        Assert.That(steps[0].Ordinal, Is.Zero);
        Assert.That(steps[0].OwnerId, Is.EqualTo(merchant.RuntimeId));
        Assert.That(steps[0].PersonId, Is.Empty);
        Assert.That(provider.OwnsStep(steps[0]), Is.True);

        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "configuration/v1", steps, "content/v1");
        liveRoster.Clear();
        Assert.That(provider.TryPrepareStep(manifest, steps[0],
            out IBoundaryContinuationStepCommit prepared, out failure), Is.True);
        Assert.That(prepared.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));

        Assert.That(merchant.SpatialKnowledge.KnowsLocation(city.Location.RuntimeId), Is.True);
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(city.Location.RuntimeId,
            item.DefinitionId, out CommercialMarketObservation observed), Is.True);
        Assert.That(observed.ObservedStock, Is.EqualTo(7));
        Assert.That(observed.ObservedDay, Is.EqualTo(1L));
        float observedPrice = observed.ObservedPrice;
        long knowledgeRevision = merchant.CommercialKnowledge.Revision;

        item.basePrice += 15f;
        city.UpdateMarketPrices();
        Assert.That(provider.TryPrepareStep(manifest, steps[0],
            out IBoundaryContinuationStepCommit replay, out failure), Is.True);
        Assert.That(replay.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(city.Location.RuntimeId,
            item.DefinitionId, out CommercialMarketObservation replayed), Is.True);
        Assert.That(replayed.ObservedStock, Is.EqualTo(7));
        Assert.That(replayed.ObservedPrice, Is.EqualTo(observedPrice));
        Assert.That(merchant.CommercialKnowledge.Revision, Is.EqualTo(knowledgeRevision));
    }

    [Test]
    public void ActorStepFreezesRosterOrderAndRejectsAmbiguousRuntimeIdentity()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("p18d-local-roster-city",
            "p18d-local-roster-location");
        NpcRuntime first = new NpcRuntime("p18d-local-roster-first",
            SimulationTestFactory.CreateNpc("p18d-local-roster-first"), city, 0f);
        NpcRuntime second = new NpcRuntime("p18d-local-roster-second",
            SimulationTestFactory.CreateNpc("p18d-local-roster-second"), city, 0f);
        List<NpcRuntime> liveRoster = new List<NpcRuntime> { second, first };
        NpcLocalKnowledgeDailyBoundaryStepProvider provider =
            new NpcLocalKnowledgeDailyBoundaryStepProvider(() => liveRoster, false);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 2L);

        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure), Is.True);
        Assert.That(steps.Count, Is.EqualTo(2));
        Assert.That(steps[0].OwnerId, Is.EqualTo(second.RuntimeId));
        Assert.That(steps[1].OwnerId, Is.EqualTo(first.RuntimeId));

        liveRoster.Reverse();
        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> replayedSteps, out failure), Is.True);
        Assert.That(replayedSteps[0].OwnerId, Is.EqualTo(second.RuntimeId));
        Assert.That(replayedSteps[1].OwnerId, Is.EqualTo(first.RuntimeId));

        NpcLocalKnowledgeDailyBoundaryStepProvider duplicateProvider =
            new NpcLocalKnowledgeDailyBoundaryStepProvider(() => new[] { first,
                new NpcRuntime(first.RuntimeId, SimulationTestFactory.CreateNpc("p18d-local-duplicate")) }, false);
        Assert.That(duplicateProvider.TryCreateSteps(operation, 0, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    [Test]
    public void ActorStepRejectsChangedObservationSourceBeforeCombinedInstall()
    {
        ItemData item = SimulationTestFactory.CreateItem("p18d-local-stale-item", 8f);
        CityRuntime city = SimulationTestFactory.CreateCity("p18d-local-stale-city",
            "p18d-local-stale-location",
            new MarketItemConfig { item = item, initialAmount = 5, desiredAmount = 10 });
        NpcRuntime merchant = new NpcRuntime("p18d-local-stale-merchant",
            SimulationTestFactory.CreateNpc("p18d-local-stale-merchant", NpcJobType.Merchant),
            city, 0f);
        NpcLocalKnowledgeDailyBoundaryStepProvider provider =
            new NpcLocalKnowledgeDailyBoundaryStepProvider(() => new[] { merchant }, true);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 3L);
        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "configuration", steps);
        Assert.That(provider.TryPrepareStep(manifest, steps[0],
            out IBoundaryContinuationStepCommit prepared, out failure), Is.True);

        item.basePrice += 10f;
        city.UpdateMarketPrices();
        Assert.That(prepared.TryCommit(out failure), Is.False);
        Assert.That(merchant.SpatialKnowledge.KnowsLocation(city.Location.RuntimeId), Is.False);
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(city.Location.RuntimeId,
            item.DefinitionId, out _), Is.False);
    }
}
