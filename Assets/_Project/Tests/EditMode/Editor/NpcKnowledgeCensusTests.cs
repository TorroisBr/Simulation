using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class NpcKnowledgeCensusTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void ProviderPublishesTenOrdinalSectionsPerNpcBoundToExactOwners()
    {
        NpcRuntime z = new NpcRuntime("z-knowledge", null);
        NpcRuntime a = new NpcRuntime("a-knowledge", null);
        var providers = NpcKnowledgeCensusProvider.CreateProviders(new[] { z, a });
        Assert.That(providers, Has.Count.EqualTo(20));
        string[] ids = providers.Select(p => p.GetCurrentCensus().SectionId).ToArray();
        Assert.That(ids[0], Does.EndWith("/a-knowledge"));
        Assert.That(ids[10], Does.EndWith("/z-knowledge"));
        Assert.That(ids.Count(x => x.EndsWith("/a-knowledge", StringComparison.Ordinal)), Is.EqualTo(10));
        Assert.That(ids.Count(x => x.EndsWith("/z-knowledge", StringComparison.Ordinal)), Is.EqualTo(10));
        foreach (var group in providers.GroupBy(p => p.GetCurrentCensus().SectionId.Substring(p.GetCurrentCensus().SectionId.LastIndexOf('/') + 1)))
        {
            Assert.That(group.Select(p => p.GetCurrentCensus().SectionId).Distinct().Count(), Is.EqualTo(10));
            var witnesses = group.Select(p => p.GetCurrentCensus()).ToArray();
            Assert.That(witnesses.Select(w => w.OwnerInstanceIdentity).Distinct(ReferenceEqualityComparer.Instance).Count(), Is.EqualTo(4));
            Assert.That(witnesses.All(w => w.SchemaVersion == 1 && w.Cardinality == 0 && w.Revision == 0), Is.True);
        }
    }

    [Test]
    public void SimulationRuntimeReconcilesKnowledgeFamilyThroughNpcMembershipOperations()
    {
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, null, economyEnabled: false);
        Assert.That(runtime.NpcKnowledgeCensusProviders, Is.Empty);

        NpcRuntime npcB = new NpcRuntime("runtime-knowledge-b", null);
        Assert.That(runtime.TryRegisterNpc(npcB, out _), Is.True);
        IReadOnlyList<IOwnerSectionCensusProvider> afterFirstRegistration = runtime.NpcKnowledgeCensusProviders;
        Assert.That(afterFirstRegistration, Has.Count.EqualTo(10));
        IOwnerSectionCensusProvider[] retainedNpcBProviders = afterFirstRegistration.ToArray();
        Assert.That(retainedNpcBProviders.All(provider =>
            provider.GetCurrentCensus().SectionId.EndsWith("/" + npcB.RuntimeId, StringComparison.Ordinal)), Is.True);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure firstAssessment), Is.True,
            firstAssessment.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long firstEpoch, out _), Is.True);
        Assert.That(firstEpoch, Is.EqualTo(1L));

        NpcRuntime npcA = new NpcRuntime("runtime-knowledge-a", null);
        Assert.That(runtime.TryRegisterNpc(npcA, out _), Is.True);
        IReadOnlyList<IOwnerSectionCensusProvider> afterSecondRegistration = runtime.NpcKnowledgeCensusProviders;
        Assert.That(afterSecondRegistration, Has.Count.EqualTo(20));
        Assert.That(afterSecondRegistration.Skip(10).ToArray(), Is.EqualTo(retainedNpcBProviders),
            "A normal roster addition must retain the unchanged NPC's exact Knowledge provider family.");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure secondAssessment), Is.True,
            secondAssessment.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long secondEpoch, out _), Is.True);
        Assert.That(secondEpoch, Is.EqualTo(2L));

        Assert.That(runtime.TryUnregisterNpc(npcA.RuntimeId, out _), Is.True);
        Assert.That(runtime.NpcKnowledgeCensusProviders, Has.Count.EqualTo(10));
        Assert.That(runtime.NpcKnowledgeCensusProviders, Is.EqualTo(retainedNpcBProviders));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure removalAssessment), Is.True,
            removalAssessment.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long finalEpoch, out _), Is.True);
        Assert.That(finalEpoch, Is.EqualTo(3L));
    }

    [Test]
    public void MissingOwnerAndNullCommercialBackingCollectionsFailWithoutMaterialization()
    {
        foreach (string ownerFieldName in new[] { "explorableSiteKnowledge", "localTopologyKnowledge", "adventureSiteIntelKnowledge", "commercialKnowledge" })
        {
            NpcRuntime missingOwnerNpc = new NpcRuntime("missing-" + ownerFieldName, null);
            FieldInfo ownerField = typeof(NpcRuntime).GetField(ownerFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            ownerField.SetValue(missingOwnerNpc, null);
            Assert.Throws<InvalidOperationException>(() => NpcKnowledgeCensusProvider.CreateProviders(new[] { missingOwnerNpc }));
            Assert.That(ownerField.GetValue(missingOwnerNpc), Is.Null);
        }

        NpcRuntime npc = new NpcRuntime("missing-commercial", null);
        FieldInfo commercialField = typeof(NpcRuntime).GetField("commercialKnowledge", BindingFlags.Instance | BindingFlags.NonPublic);
        commercialField.SetValue(npc, null);
        Assert.Throws<InvalidOperationException>(() => NpcKnowledgeCensusProvider.CreateProviders(new[] { npc }));
        Assert.That(commercialField.GetValue(npc), Is.Null);

        foreach (string fieldName in new[] { "observations", "liquidityObservations", "shareReceipts" })
        {
            CommercialKnowledgeRuntime owner = new CommercialKnowledgeRuntime();
            FieldInfo selected = typeof(CommercialKnowledgeRuntime).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            selected.SetValue(owner, null);
            FieldInfo markets = typeof(CommercialKnowledgeRuntime).GetField("observations", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo liquidity = typeof(CommercialKnowledgeRuntime).GetField("liquidityObservations", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo receipts = typeof(CommercialKnowledgeRuntime).GetField("shareReceipts", BindingFlags.Instance | BindingFlags.NonPublic);
            object marketsBefore = markets.GetValue(owner);
            object liquidityBefore = liquidity.GetValue(owner);
            object receiptsBefore = receipts.GetValue(owner);
            FieldInfo revision = typeof(CommercialKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic);
            revision.SetValue(owner, 7L);
            MethodInfo read = typeof(CommercialKnowledgeRuntime).GetMethod("TryReadCensus", BindingFlags.Instance | BindingFlags.NonPublic);
            object[] args = { 0, 0, 0, 0L };
            Assert.That(read.Invoke(owner, args), Is.EqualTo(false));
            Assert.That(markets.GetValue(owner), Is.SameAs(marketsBefore));
            Assert.That(liquidity.GetValue(owner), Is.SameAs(liquidityBefore));
            Assert.That(receipts.GetValue(owner), Is.SameAs(receiptsBefore));
            Assert.That(revision.GetValue(owner), Is.EqualTo(7L));
        }
    }

    [Test]
    public void OwnerRevisionsAdvanceOnlyForSuccessfulKnowledgeChangesAndSaturateClosed()
    {
        ExplorableSiteKnowledgeRuntime explorable = new ExplorableSiteKnowledgeRuntime("owner");
        var first = new ExplorableSiteKnowledgeObservation("site", "loc", 1, 1, ExplorableSiteKnowledgeSource.InitialScenarioKnowledge);
        var replacement = new ExplorableSiteKnowledgeObservation("site", "loc", 2, 2, ExplorableSiteKnowledgeSource.DirectObservation);
        Assert.That(explorable.RecordObservation(first), Is.True);
        Assert.That(explorable.Revision, Is.EqualTo(1));
        Assert.That(explorable.RecordObservation(first), Is.False);
        Assert.That(explorable.RecordObservation(replacement), Is.True);
        Assert.That(explorable.Revision, Is.EqualTo(2));
        typeof(ExplorableSiteKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(explorable, long.MaxValue);
        Assert.That(explorable.RecordObservation(new ExplorableSiteKnowledgeObservation("other", "loc", 1, 1, ExplorableSiteKnowledgeSource.DirectObservation)), Is.False);
        Assert.That(explorable.RecordObservation(new ExplorableSiteKnowledgeObservation("site", "loc", 3, 3, ExplorableSiteKnowledgeSource.DirectObservation)), Is.False);
        Assert.That(explorable.Observations, Has.Count.EqualTo(1));
        Assert.That(explorable.Observations[0].ObservedDay, Is.EqualTo(2L));
        Assert.That(explorable.Revision, Is.EqualTo(long.MaxValue));

        LocalTopologyKnowledgeRuntime local = new LocalTopologyKnowledgeRuntime("owner");
        Assert.That(local.RecordPlaceObservation(new LocalPlaceKnowledgeObservation("site", "place", null, null, "A", false, 1, 1, LocalTopologyKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(local.Revision, Is.EqualTo(1));
        typeof(LocalTopologyKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(local, long.MaxValue);
        Assert.That(local.RecordPlaceObservation(new LocalPlaceKnowledgeObservation("site", "other-place", null, null, "B", false, 1, 1, LocalTopologyKnowledgeSource.DirectObservation)), Is.False);
        Assert.That(local.PlaceObservations, Has.Count.EqualTo(1));

        AdventureSiteIntelKnowledgeRuntime adventure = new AdventureSiteIntelKnowledgeRuntime("owner");
        Assert.That(adventure.RecordObservation(new AdventureOppositionObservation("site", null, "opposition", AdventureOppositionObservedState.Active, 1, 1, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(adventure.Revision, Is.EqualTo(1));
        Assert.That(adventure.RecordObservation(new AdventureNotableItemObservation("site", null, "item", "definition", 1, 1, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(adventure.RecordObservation(new AdventureCommonResourceObservation("site", null, "resource", 1, null, 1, 1, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(adventure.RecordObservation(new AdventureAccessObservation("site", null, default(PlaceAccessState), 1, 1, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(adventure.Revision, Is.EqualTo(4));
        typeof(AdventureSiteIntelKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(adventure, long.MaxValue);
        Assert.That(adventure.RecordObservation(new AdventureOppositionObservation("site", null, "another-opposition", AdventureOppositionObservedState.Active, 1, 1, AdventureIntelSource.DirectObservation)), Is.False);
        Assert.That(adventure.OppositionObservations, Has.Count.EqualTo(1));
    }

    [Test]
    public void NonzeroSectionsMapToExactTypedOwnersAndSharedOwnerRevisions()
    {
        NpcRuntime npc = new NpcRuntime("mapped-knowledge", null);
        ExplorableSiteKnowledgeRuntime explorable = npc.ExplorableSiteKnowledge;
        LocalTopologyKnowledgeRuntime local = npc.LocalTopologyKnowledge;
        AdventureSiteIntelKnowledgeRuntime adventure = npc.AdventureSiteIntelKnowledge;
        CommercialKnowledgeRuntime commercial = npc.CommercialKnowledge;

        Assert.That(explorable.RecordObservation(new ExplorableSiteKnowledgeObservation(
            "site", "location", 1, 1, ExplorableSiteKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(local.RecordPlaceObservation(Place("place-a", 1, "A")), Is.True);
        Assert.That(local.RecordPlaceObservation(Place("place-b", 1, "B")), Is.True);
        Assert.That(local.RecordConnectionObservation(Connection("edge", "place-a", "place-b", 1)), Is.True);
        Assert.That(adventure.RecordObservation(Opposition("opposition", 1)), Is.True);
        Assert.That(adventure.RecordObservation(Notable("item", 1)), Is.True);
        Assert.That(adventure.RecordObservation(Resource("resource", 1)), Is.True);
        Assert.That(adventure.RecordObservation(Access(1)), Is.True);

        ItemData item = SimulationTestFactory.CreateItem("knowledge-census-market-item");
        Assert.That(commercial.RecordObservation(SimulationTestFactory.CreateObservation(
            "market-location", item, 10f, 2, 1, 1)), Is.True);
        Assert.That(commercial.RecordLiquidityObservation(new CommercialLiquidityObservation(
            "market-location", MarketLiquidityMode.AccountBacked, 20f, 1, 1,
            CommercialKnowledgeSource.DirectObservation)), Is.True);
        CommercialKnowledgeShareBatch share = new CommercialKnowledgeShareBatch(
            "boundary", "sender", "sender-person", npc.RuntimeId, "receiver-person", "empty", 1, 1,
            Array.Empty<CommercialKnowledgeShareValue>());
        Assert.That(commercial.TryPrepareShareBatch(share, out CommercialKnowledgeShareBatchCommit shareCommit), Is.True);
        Assert.That(shareCommit.TryCommit(out _), Is.True);

        IReadOnlyList<IOwnerSectionCensusProvider> providers = NpcKnowledgeCensusProvider.CreateProviders(new[] { npc });
        Assert.That(providers, Has.Count.EqualTo(10));
        OwnerSectionCensusWitness[] witnesses = providers.Select(provider => provider.GetCurrentCensus()).ToArray();
        Assert.That(witnesses.Select(witness => witness.SectionId), Is.EqualTo(new[]
        {
            "p12f.explorable-site-knowledge/mapped-knowledge",
            "p12f.local-topology-knowledge.places/mapped-knowledge",
            "p12f.local-topology-knowledge.connections/mapped-knowledge",
            "p12f.adventure-intel.opposition/mapped-knowledge",
            "p12f.adventure-intel.notable-items/mapped-knowledge",
            "p12f.adventure-intel.common-resources/mapped-knowledge",
            "p12f.adventure-intel.access/mapped-knowledge",
            "p12f.commercial-knowledge.markets/mapped-knowledge",
            "p12f.commercial-knowledge.liquidity/mapped-knowledge",
            "p12f.commercial-knowledge.share-receipts/mapped-knowledge"
        }));
        Assert.That(witnesses.Select(witness => witness.Cardinality), Is.EqualTo(new[] { 1, 2, 1, 1, 1, 1, 1, 1, 1, 1 }));
        Assert.That(witnesses.Select(witness => witness.SchemaVersion), Is.All.EqualTo(1));
        Assert.That(witnesses[0].OwnerInstanceIdentity, Is.SameAs(explorable));
        Assert.That(witnesses[1].OwnerInstanceIdentity, Is.SameAs(local));
        Assert.That(witnesses[2].OwnerInstanceIdentity, Is.SameAs(local));
        Assert.That(witnesses[3].OwnerInstanceIdentity, Is.SameAs(adventure));
        Assert.That(witnesses[4].OwnerInstanceIdentity, Is.SameAs(adventure));
        Assert.That(witnesses[5].OwnerInstanceIdentity, Is.SameAs(adventure));
        Assert.That(witnesses[6].OwnerInstanceIdentity, Is.SameAs(adventure));
        Assert.That(witnesses[7].OwnerInstanceIdentity, Is.SameAs(commercial));
        Assert.That(witnesses[8].OwnerInstanceIdentity, Is.SameAs(commercial));
        Assert.That(witnesses[9].OwnerInstanceIdentity, Is.SameAs(commercial));
        Assert.That(witnesses.Skip(1).Take(2).Select(witness => witness.Revision).Distinct().Count(), Is.EqualTo(1));
        Assert.That(witnesses.Skip(3).Take(4).Select(witness => witness.Revision).Distinct().Count(), Is.EqualTo(1));
        Assert.That(witnesses.Skip(7).Select(witness => witness.Revision).Distinct().Count(), Is.EqualTo(1));
    }

    [Test]
    public void LocalTopologyPlaceAndConnectionSectionsShareRevisionAndRejectSaturatedWrites()
    {
        NpcRuntime npc = new NpcRuntime("local-matrix", null);
        LocalTopologyKnowledgeRuntime owner = npc.LocalTopologyKnowledge;
        LocalPlaceKnowledgeObservation firstPlace = Place("a", 1, "A");
        LocalPlaceKnowledgeObservation secondPlace = Place("b", 1, "B");
        Assert.That(owner.RecordPlaceObservation(firstPlace), Is.True);
        Assert.That(owner.RecordPlaceObservation(secondPlace), Is.True);
        LocalConnectionKnowledgeObservation firstConnection = Connection("edge", "a", "b", 1);
        Assert.That(owner.RecordConnectionObservation(firstConnection), Is.True);
        Assert.That(owner.RecordPlaceObservation(Place("a", 2, "A2")), Is.True);
        Assert.That(owner.RecordConnectionObservation(Connection("edge", "a", "b", 2)), Is.True);
        long revisionBeforeNoOps = owner.Revision;
        Assert.That(owner.RecordPlaceObservation(null), Is.False);
        Assert.That(owner.RecordConnectionObservation(null), Is.False);
        Assert.That(owner.RecordPlaceObservation(firstPlace), Is.False, "Older/equal-ranked place knowledge is a no-op.");
        Assert.That(owner.RecordConnectionObservation(firstConnection), Is.False, "Older connection knowledge is a no-op.");
        Assert.That(owner.Revision, Is.EqualTo(revisionBeforeNoOps));

        SetRevision(owner, long.MaxValue);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = NpcKnowledgeCensusProvider.CreateProviders(new[] { npc });
        OwnerSectionCensusWitness placesBefore = providers[1].GetCurrentCensus();
        OwnerSectionCensusWitness connectionsBefore = providers[2].GetCurrentCensus();
        Assert.That(owner.RecordPlaceObservation(Place("new-place", 3, "New")), Is.False);
        Assert.That(owner.RecordConnectionObservation(Connection("new-edge", "a", "b", 3)), Is.False);
        Assert.That(owner.RecordPlaceObservation(Place("a", 3, "A3")), Is.False);
        Assert.That(owner.RecordConnectionObservation(Connection("edge", "a", "b", 3)), Is.False);
        Assert.That(providers[1].GetCurrentCensus().Cardinality, Is.EqualTo(placesBefore.Cardinality));
        Assert.That(providers[2].GetCurrentCensus().Cardinality, Is.EqualTo(connectionsBefore.Cardinality));
        Assert.That(owner.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void EveryAdventureKnowledgeListUsesOneRevisionAndRejectsSaturatedWrites()
    {
        NpcRuntime npc = new NpcRuntime("adventure-matrix", null);
        AdventureSiteIntelKnowledgeRuntime owner = npc.AdventureSiteIntelKnowledge;
        Assert.That(owner.RecordObservation(Opposition("opposition", 1)), Is.True);
        Assert.That(owner.RecordObservation(Notable("item", 1)), Is.True);
        Assert.That(owner.RecordObservation(Resource("resource", 1)), Is.True);
        Assert.That(owner.RecordObservation(Access(1)), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(4L));
        Assert.That(owner.RecordObservation(Opposition("opposition", 1)), Is.False);
        Assert.That(owner.RecordObservation(Notable("item", 1)), Is.False);
        Assert.That(owner.RecordObservation(Resource("resource", 1)), Is.False);
        Assert.That(owner.RecordObservation(Access(1)), Is.False);
        Assert.That(owner.RecordObservation((AdventureOppositionObservation)null), Is.False);

        Assert.That(owner.RecordObservation(Opposition("opposition", 2)), Is.True);
        Assert.That(owner.RecordObservation(Notable("item", 2)), Is.True);
        Assert.That(owner.RecordObservation(Resource("resource", 2)), Is.True);
        Assert.That(owner.RecordObservation(Access(2)), Is.True);
        SetRevision(owner, long.MaxValue);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = NpcKnowledgeCensusProvider.CreateProviders(new[] { npc });
        int[] before = providers.Skip(3).Take(4).Select(provider => provider.GetCurrentCensus().Cardinality).ToArray();
        Assert.That(owner.RecordObservation(Opposition("new-opposition", 3)), Is.False);
        Assert.That(owner.RecordObservation(Notable("new-item", 3)), Is.False);
        Assert.That(owner.RecordObservation(Resource("new-resource", 3)), Is.False);
        Assert.That(owner.RecordObservation(Access(3, "another-place")), Is.False);
        Assert.That(owner.RecordObservation(Opposition("opposition", 3)), Is.False);
        Assert.That(owner.RecordObservation(Notable("item", 3)), Is.False);
        Assert.That(owner.RecordObservation(Resource("resource", 3)), Is.False);
        Assert.That(owner.RecordObservation(Access(3)), Is.False);
        Assert.That(providers.Skip(3).Take(4).Select(provider => provider.GetCurrentCensus().Cardinality), Is.EqualTo(before));
        Assert.That(owner.OppositionObservations[0].ObservedDay, Is.EqualTo(2L));
        Assert.That(owner.NotableItemObservations[0].ObservedDay, Is.EqualTo(2L));
        Assert.That(owner.CommonResourceObservations[0].ObservedDay, Is.EqualTo(2L));
        Assert.That(owner.AccessObservations[0].ObservedDay, Is.EqualTo(2L));
        Assert.That(owner.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void CommercialSectionsCountDirectPreparedAndShareWritesAtOwnerBoundaries()
    {
        NpcRuntime npc = new NpcRuntime("commercial-matrix", null);
        CommercialKnowledgeRuntime owner = npc.CommercialKnowledge;
        ItemData direct = SimulationTestFactory.CreateItem("commercial-direct-census");
        CommercialMarketObservation market = SimulationTestFactory.CreateObservation("market-a", direct, 5f, 2, 1, 1);
        Assert.That(owner.RecordObservation(market), Is.True);
        Assert.That(owner.RecordObservation(null), Is.False);
        Assert.That(owner.RecordLiquidityObservation(new CommercialLiquidityObservation(
            "market-a", MarketLiquidityMode.AccountBacked, 30f, 1, 1, CommercialKnowledgeSource.DirectObservation)), Is.True);

        ItemData preparedItem = SimulationTestFactory.CreateItem("commercial-prepared-census");
        CommercialMarketObservation preparedMarket = SimulationTestFactory.CreateObservation("market-b", preparedItem, 8f, 4, 2, 2);
        MethodInfo prepareDirect = typeof(CommercialKnowledgeRuntime).GetMethod(
            "TryPrepareDirectObservationBatch", BindingFlags.Instance | BindingFlags.NonPublic);
        object[] prepareArgs = { new[] { preparedMarket }, null, null };
        Assert.That((bool)prepareDirect.Invoke(owner, prepareArgs), Is.True);
        object prepared = prepareArgs[2];
        Assert.That((bool)typeof(CommercialKnowledgeRuntime).GetMethod("CanInstall", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(owner, new[] { prepared }), Is.True);
        typeof(CommercialKnowledgeRuntime).GetMethod("InstallPrepared", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(owner, new[] { prepared });

        CommercialKnowledgeShareBatch share = new CommercialKnowledgeShareBatch(
            "boundary-share", "sender", "sender-person", npc.RuntimeId, "receiver-person", "empty", 3, 1,
            Array.Empty<CommercialKnowledgeShareValue>());
        Assert.That(owner.TryPrepareShareBatch(share, out CommercialKnowledgeShareBatchCommit shareCommit), Is.True);
        long beforeShare = owner.Revision;
        Assert.That(shareCommit.TryCommit(out _), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(beforeShare + 1), "A new empty share edge still commits one replay receipt.");
        Assert.That(owner.TryPrepareShareBatch(share, out CommercialKnowledgeShareBatchCommit replay), Is.True);
        long beforeReplay = owner.Revision;
        Assert.That(replay.TryCommit(out _), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(beforeReplay), "A replay does not advance the owner revision.");

        IReadOnlyList<IOwnerSectionCensusProvider> providers = NpcKnowledgeCensusProvider.CreateProviders(new[] { npc });
        Assert.That(providers[7].GetCurrentCensus().Cardinality, Is.EqualTo(2));
        Assert.That(providers[8].GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(providers[9].GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(providers.Skip(7).Select(provider => provider.GetCurrentCensus().Revision).Distinct().Count(), Is.EqualTo(1));

        SetRevision(owner, long.MaxValue);
        OwnerSectionCensusWitness marketBefore = providers[7].GetCurrentCensus();
        OwnerSectionCensusWitness liquidityBefore = providers[8].GetCurrentCensus();
        Assert.That(owner.RecordObservation(SimulationTestFactory.CreateObservation("market-c", direct, 9f, 1, 3, 3)), Is.False);
        Assert.That(owner.RecordObservation(SimulationTestFactory.CreateObservation("market-a", direct, 4f, 1, 3, 3)), Is.False);
        Assert.That(owner.RecordLiquidityObservation(new CommercialLiquidityObservation(
            "market-b", MarketLiquidityMode.Open, 0f, 3, 3, CommercialKnowledgeSource.DirectObservation)), Is.False);
        Assert.That(owner.RecordLiquidityObservation(new CommercialLiquidityObservation(
            "market-a", MarketLiquidityMode.Open, 0f, 3, 3, CommercialKnowledgeSource.DirectObservation)), Is.False);
        object[] saturatedPrepareArgs = { new[] { SimulationTestFactory.CreateObservation("market-d", direct, 3f, 1, 4, 4) }, null, null };
        Assert.That((bool)prepareDirect.Invoke(owner, saturatedPrepareArgs), Is.False);
        CommercialKnowledgeShareBatch saturatedShare = new CommercialKnowledgeShareBatch(
            "boundary-saturated", "sender", "sender-person", npc.RuntimeId, "receiver-person", "empty", 4, 1,
            Array.Empty<CommercialKnowledgeShareValue>());
        Assert.That(owner.TryPrepareShareBatch(saturatedShare, out _), Is.False);
        Assert.That(providers[7].GetCurrentCensus().Cardinality, Is.EqualTo(marketBefore.Cardinality));
        Assert.That(providers[8].GetCurrentCensus().Cardinality, Is.EqualTo(liquidityBefore.Cardinality));
        Assert.That(owner.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void AdventureObservationFansOutLocalTopologyAndKnowledgeWithOwnerScopedSaturation()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        SpatialLocationRuntime location = new SpatialLocationRuntime("fanout-location");
        ExplorableSiteRuntime site = new ExplorableSiteRuntime(
            "fanout-site", SimulationTestFactory.CreateExplorableSite("fanout-site-def"), location);
        Assert.That(registry.RegisterExplorableSite(site), Is.True);
        NpcRuntime npc = new NpcRuntime("fanout-npc", SimulationTestFactory.CreateNpc("fanout-npc-def"));
        npc.SetCurrentPresence(location);
        Assert.That(registry.RegisterNpc(npc), Is.True);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(site));
        LocalPlaceRuntime entrance = new LocalPlaceRuntime("fanout-entrance", "Entrance");
        LocalPlaceRuntime chamber = new LocalPlaceRuntime("fanout-chamber", "Chamber");
        Assert.That(topology.AddPlace(entrance, null, true), Is.True);
        Assert.That(topology.AddPlace(chamber), Is.True);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime(
            "fanout-connection", entrance, chamber, 1f)), Is.True);
        PlaceContentStore contents = new PlaceContentStore(allocator, registry);
        ItemData item = SimulationTestFactory.CreateItem("fanout-resource");
        Assert.That(contents.TryAddStack(entrance, item, 2, PlaceContentPersistencePolicy.Durable, out _), Is.True);

        AdventureSiteIntelKnowledgeSystem intel = new AdventureSiteIntelKnowledgeSystem();
        Assert.That(intel.RecordDirectObservation(npc, site, entrance, topology, contents, 4), Is.True);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = NpcKnowledgeCensusProvider.CreateProviders(new[] { npc });
        Assert.That(providers[1].GetCurrentCensus().Cardinality, Is.EqualTo(2));
        Assert.That(providers[2].GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(providers[1].GetCurrentCensus().Revision, Is.EqualTo(3L));
        Assert.That(providers[2].GetCurrentCensus().Revision, Is.EqualTo(3L));
        Assert.That(providers[6].GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(providers[5].GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(providers[3].GetCurrentCensus().Revision, Is.EqualTo(2L));

        NpcRuntime saturatedNpc = new NpcRuntime("fanout-saturated-npc", SimulationTestFactory.CreateNpc("fanout-saturated-def"));
        saturatedNpc.SetCurrentPresence(location);
        Assert.That(registry.RegisterNpc(saturatedNpc), Is.True);
        typeof(AdventureSiteIntelKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(saturatedNpc.AdventureSiteIntelKnowledge, long.MaxValue);
        Assert.That(intel.RecordDirectObservation(saturatedNpc, site, entrance, topology, contents, 5), Is.True);
        IReadOnlyList<IOwnerSectionCensusProvider> saturatedProviders =
            NpcKnowledgeCensusProvider.CreateProviders(new[] { saturatedNpc });
        Assert.That(saturatedProviders[1].GetCurrentCensus().Cardinality, Is.EqualTo(2));
        Assert.That(saturatedProviders[2].GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(saturatedProviders[3].GetCurrentCensus().Cardinality, Is.Zero);
        Assert.That(saturatedProviders[5].GetCurrentCensus().Cardinality, Is.Zero);
        Assert.That(saturatedProviders[6].GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void KnowledgeFamilyReconcilesRosterChangesAndSameIdReplacement()
    {
        PersonStore personStore = new PersonStore();
        NpcRuntime npcB = new NpcRuntime("npc-b", null);
        List<NpcRuntime> roster = new List<NpcRuntime> { npcB };
        ContinuationCensusProtocol protocol = CreateCompleteNpcFamilyProtocol(personStore, roster);
        IReadOnlyList<IOwnerSectionCensusProvider> initialKnowledge = protocol.NpcKnowledgeFamilyProviders;

        NpcRuntime npcA = new NpcRuntime("npc-a", null);
        roster.Add(npcA);
        ReconcileNpcFamily(protocol, personStore);
        IReadOnlyList<IOwnerSectionCensusProvider> afterAddition = protocol.NpcKnowledgeFamilyProviders;
        Assert.That(afterAddition, Has.Count.EqualTo(20));
        Assert.That(afterAddition[0].GetCurrentCensus().SectionId, Does.EndWith("/npc-a"));
        Assert.That(afterAddition[10].GetCurrentCensus().SectionId, Does.EndWith("/npc-b"));
        Assert.That(afterAddition[10], Is.SameAs(initialKnowledge[0]));
        Assert.That(ReadEpoch(protocol), Is.EqualTo(1L));
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure addAssessment), Is.True,
            addAssessment.ToString());

        roster.Remove(npcA);
        ReconcileNpcFamily(protocol, personStore);
        IReadOnlyList<IOwnerSectionCensusProvider> afterRemoval = protocol.NpcKnowledgeFamilyProviders;
        Assert.That(afterRemoval, Has.Count.EqualTo(10));
        Assert.That(afterRemoval[0].GetCurrentCensus().SectionId, Does.EndWith("/npc-b"));
        Assert.That(ReadEpoch(protocol), Is.EqualTo(2L));
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure removeAssessment), Is.True,
            removeAssessment.ToString());

        NpcRuntime replacement = new NpcRuntime(npcB.RuntimeId, null);
        roster[0] = replacement;
        ReconcileNpcFamily(protocol, personStore);
        IReadOnlyList<IOwnerSectionCensusProvider> afterReplacement = protocol.NpcKnowledgeFamilyProviders;
        Assert.That(afterReplacement, Has.Count.EqualTo(10));
        Assert.That(afterReplacement[0], Is.Not.SameAs(afterRemoval[0]));
        Assert.That(afterReplacement[0].GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(replacement.ExplorableSiteKnowledge));
        Assert.That(ReadEpoch(protocol), Is.EqualTo(3L));
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure replacementAssessment), Is.True,
            replacementAssessment.ToString());
    }

    [Test]
    public void KnowledgeRosterReconciliationPreservesInventoryFamilyCoverageAndProviders()
    {
        PersonStore personStore = new PersonStore();
        NpcRuntime npcB = new NpcRuntime("inventory-npc-b", null);
        InventoryRuntime npcBInventory = npcB.Inventory;
        List<NpcRuntime> roster = new List<NpcRuntime> { npcB };
        ContinuationCensusProtocol protocol = CreateCompleteNpcFamilyProtocol(personStore, roster, includeInventory: true);
        IReadOnlyList<IOwnerSectionCensusProvider> inventoryBefore = protocol.InventoryFamilyProviders;
        Assert.That(inventoryBefore, Has.Count.EqualTo(1));
        Assert.That(inventoryBefore[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(npcBInventory));
        Assert.That(protocol.NpcKnowledgeFamilyProviders, Has.Count.EqualTo(10));

        NpcRuntime npcA = new NpcRuntime("inventory-npc-a", null);
        InventoryRuntime npcAInventory = npcA.Inventory;
        roster.Add(npcA);
        ReconcileNpcFamily(protocol, personStore);

        IReadOnlyList<IOwnerSectionCensusProvider> inventoryAfterAddition = protocol.InventoryFamilyProviders;
        Assert.That(inventoryAfterAddition, Has.Count.EqualTo(2));
        Assert.That(inventoryAfterAddition[0].GetCurrentCensus().SectionId,
            Is.EqualTo(NpcInventoryCensusProvider.SectionPrefix + npcA.RuntimeId));
        Assert.That(inventoryAfterAddition[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(npcAInventory));
        Assert.That(inventoryAfterAddition[1], Is.SameAs(inventoryBefore[0]));
        Assert.That(inventoryAfterAddition[1].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(npcBInventory));
        Assert.That(protocol.NpcKnowledgeFamilyProviders, Has.Count.EqualTo(20));
        Assert.That(protocol.NpcKnowledgeFamilyProviders.Select(p => p.GetCurrentCensus().SectionId)
            .Intersect(inventoryAfterAddition.Select(p => p.GetCurrentCensus().SectionId)), Is.Empty,
            "Inventory and Knowledge section families must remain disjoint during shared roster reconciliation.");
        Assert.That(ReadEpoch(protocol), Is.EqualTo(1L),
            "The Inventory and Knowledge additions must publish under the single outer roster mutation epoch.");
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure addAssessment), Is.True,
            addAssessment.ToString());

        roster.Remove(npcA);
        ReconcileNpcFamily(protocol, personStore);
        IReadOnlyList<IOwnerSectionCensusProvider> inventoryAfterRemoval = protocol.InventoryFamilyProviders;
        Assert.That(inventoryAfterRemoval, Has.Count.EqualTo(1));
        Assert.That(inventoryAfterRemoval[0], Is.SameAs(inventoryBefore[0]));
        Assert.That(inventoryAfterRemoval[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(npcBInventory));
        Assert.That(protocol.NpcKnowledgeFamilyProviders, Has.Count.EqualTo(10));
        Assert.That(ReadEpoch(protocol), Is.EqualTo(2L));
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure removeAssessment), Is.True,
            removeAssessment.ToString());
    }

    [Test]
    public void KnowledgeFamilyReconciliationRebindsEachTypedOwnerAndKeepsOtherProviders()
    {
        PersonStore personStore = new PersonStore();
        NpcRuntime npc = new NpcRuntime("owner-rebind", null);
        List<NpcRuntime> roster = new List<NpcRuntime> { npc };
        ContinuationCensusProtocol protocol = CreateCompleteNpcFamilyProtocol(personStore, roster);
        string[] fields = { "explorableSiteKnowledge", "localTopologyKnowledge", "adventureSiteIntelKnowledge", "commercialKnowledge" };
        object[] replacements =
        {
            new ExplorableSiteKnowledgeRuntime(npc.RuntimeId),
            new LocalTopologyKnowledgeRuntime(npc.RuntimeId),
            new AdventureSiteIntelKnowledgeRuntime(npc.RuntimeId),
            new CommercialKnowledgeRuntime()
        };
        int[][] sectionIndices = { new[] { 0 }, new[] { 1, 2 }, new[] { 3, 4, 5, 6 }, new[] { 7, 8, 9 } };

        for (int ownerIndex = 0; ownerIndex < fields.Length; ownerIndex++)
        {
            IReadOnlyList<IOwnerSectionCensusProvider> before = protocol.NpcKnowledgeFamilyProviders;
            typeof(NpcRuntime).GetField(fields[ownerIndex], BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(npc, replacements[ownerIndex]);
            ReconcileNpcFamily(protocol, personStore);
            IReadOnlyList<IOwnerSectionCensusProvider> after = protocol.NpcKnowledgeFamilyProviders;
            foreach (int sectionIndex in sectionIndices[ownerIndex])
            {
                Assert.That(after[sectionIndex], Is.Not.SameAs(before[sectionIndex]));
                Assert.That(after[sectionIndex].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(replacements[ownerIndex]));
            }
            for (int sectionIndex = 0; sectionIndex < after.Count; sectionIndex++)
            {
                if (!sectionIndices[ownerIndex].Contains(sectionIndex))
                    Assert.That(after[sectionIndex], Is.SameAs(before[sectionIndex]));
            }
            Assert.That(ReadEpoch(protocol), Is.EqualTo(ownerIndex + 1L));
            Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure assessment), Is.True,
                assessment.ToString());
        }
    }

    [Test]
    public void InvalidDynamicKnowledgeRosterFailsBeforePublishingAnyFamilyOrEpoch()
    {
        PersonStore personStore = new PersonStore();
        NpcRuntime first = new NpcRuntime("atomic-a", null);
        NpcRuntime second = new NpcRuntime("atomic-b", null);
        List<NpcRuntime> roster = new List<NpcRuntime> { first, second };
        ContinuationCensusProtocol protocol = CreateCompleteNpcFamilyProtocol(personStore, roster, includeInventory: true);
        IReadOnlyList<IOwnerSectionCensusProvider> spatialBefore = protocol.SpatialKnowledgeFamilyProviders;
        IReadOnlyList<IOwnerSectionCensusProvider> knowledgeBefore = protocol.NpcKnowledgeFamilyProviders;
        IReadOnlyList<IOwnerSectionCensusProvider> inventoryBefore = protocol.InventoryFamilyProviders;

        typeof(NpcRuntime).GetField("commercialKnowledge", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(second, null);

        Assert.That(protocol.TryEnterOperation("runtime.npc-membership", out SimulationOperationScope scope, out _), Is.True);
        Assert.That(protocol.TryReconcileSpatialKnowledgeRosterAndNotifyCommittedMutations(
            new string[0], personStore.Revision, out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(protocol.SpatialKnowledgeFamilyProviders, Is.SameAs(spatialBefore));
        Assert.That(protocol.NpcKnowledgeFamilyProviders, Is.SameAs(knowledgeBefore));
        Assert.That(protocol.InventoryFamilyProviders, Is.SameAs(inventoryBefore),
            "A failed Knowledge reconciliation must not publish a partial Inventory provider snapshot.");
        Assert.That(protocol.InventoryFamilyProviders, Has.Count.EqualTo(2));
        Assert.That(protocol.InventoryFamilyProviders[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(first.Inventory));
        Assert.That(protocol.InventoryFamilyProviders[1].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(second.Inventory));
        Assert.That(protocol.TryReadMutationEpoch(out long epoch, out ContinuationCensusFailure epochFailure), Is.False);
        Assert.That(epoch, Is.Zero);
        Assert.That(epochFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        scope.Dispose();
    }

    [Test]
    public void KnowledgeFamilyRejectsDuplicateEmptyAndMissingOwnerInputsWithoutPartialPublication()
    {
        Assert.Throws<InvalidOperationException>(() => NpcKnowledgeCensusProvider.CreateProviders(new[]
        {
            new NpcRuntime("duplicate", null), new NpcRuntime("duplicate", null)
        }));
        Assert.Throws<InvalidOperationException>(() => NpcKnowledgeCensusProvider.CreateProviders(new[]
        {
            SetRuntimeId(new NpcRuntime("temporarily-valid", null), " ")
        }));

        NpcRuntime valid = new NpcRuntime("valid-owner", null);
        NpcRuntime missingCommercial = new NpcRuntime("missing-owner", null);
        typeof(NpcRuntime).GetField("commercialKnowledge", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(missingCommercial, null);
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterNpcKnowledgeRosterFamily(new[] { valid, missingCommercial }, out _), Is.False);
        Assert.That(protocol.NpcKnowledgeFamilyProviders, Is.Empty);
    }

    [Test]
    public void ProtocolDetectsUnnotifiedDriftInEveryKnowledgeCardinalitySection()
    {
        Action<NpcRuntime>[] changes =
        {
            npc => npc.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
                "site", "location", 1, 1, ExplorableSiteKnowledgeSource.DirectObservation)),
            npc => npc.LocalTopologyKnowledge.RecordPlaceObservation(Place("place", 1, "Place")),
            npc =>
            {
                npc.LocalTopologyKnowledge.RecordPlaceObservation(Place("from", 1, "From"));
                npc.LocalTopologyKnowledge.RecordPlaceObservation(Place("to", 1, "To"));
                npc.LocalTopologyKnowledge.RecordConnectionObservation(Connection("edge", "from", "to", 1));
            },
            npc => npc.AdventureSiteIntelKnowledge.RecordObservation(Opposition("opposition", 1)),
            npc => npc.AdventureSiteIntelKnowledge.RecordObservation(Notable("item", 1)),
            npc => npc.AdventureSiteIntelKnowledge.RecordObservation(Resource("resource", 1)),
            npc => npc.AdventureSiteIntelKnowledge.RecordObservation(Access(1)),
            npc => npc.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
                "market", SimulationTestFactory.CreateItem("drift-item"), 5f, 1, 1, 1)),
            npc => npc.CommercialKnowledge.RecordLiquidityObservation(new CommercialLiquidityObservation(
                "liquidity", MarketLiquidityMode.Open, 0f, 1, 1, CommercialKnowledgeSource.DirectObservation)),
            npc =>
            {
                CommercialKnowledgeShareBatch batch = new CommercialKnowledgeShareBatch(
                    "boundary", "sender", "sender-person", npc.RuntimeId, "receiver-person", "empty", 1, 1,
                    Array.Empty<CommercialKnowledgeShareValue>());
                Assert.That(npc.CommercialKnowledge.TryPrepareShareBatch(batch, out CommercialKnowledgeShareBatchCommit commit), Is.True);
                Assert.That(commit.TryCommit(out _), Is.True);
            }
        };

        foreach (Action<NpcRuntime> change in changes)
        {
            NpcRuntime npc = new NpcRuntime("drift-npc", null);
            ContinuationCensusProtocol protocol = CreateKnowledgeOnlyProtocol(new[] { npc });
            change(npc);
            Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure failure), Is.False);
            Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        }
    }

    [Test]
    public void ProtocolRegistersKnowledgeFamilyAndDetectsUnnotifiedWrite()
    {
        NpcRuntime npc = new NpcRuntime("protocol-knowledge", null);
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterNpcKnowledgeRosterFamily(new[] { npc }, out ContinuationCensusFailure registerFailure), Is.True, registerFailure.ToString());
        Assert.That(protocol.NpcKnowledgeFamilyProviders, Has.Count.EqualTo(10));
        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("runtime.advance", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure baseline), Is.True, baseline.ToString());
        npc.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation("site", "loc", 1, 1, ExplorableSiteKnowledgeSource.DirectObservation));
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure drift), Is.False);
        Assert.That(drift, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
    }

    [Test]
    public void ExactZeroKnowledgeSectionsRemainReadableAtRevisionSaturation()
    {
        NpcRuntime npc = new NpcRuntime("saturated-knowledge", null);
        typeof(ExplorableSiteKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc.ExplorableSiteKnowledge, long.MaxValue);
        typeof(LocalTopologyKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc.LocalTopologyKnowledge, long.MaxValue);
        typeof(AdventureSiteIntelKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc.AdventureSiteIntelKnowledge, long.MaxValue);
        typeof(CommercialKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc.CommercialKnowledge, long.MaxValue);
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterNpcKnowledgeRosterFamily(new[] { npc }, out ContinuationCensusFailure registration), Is.True, registration.ToString());
        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("runtime.advance", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure assessment), Is.True, assessment.ToString());
        Assert.That(protocol.NpcKnowledgeFamilyProviders.All(p => p.GetCurrentCensus().Cardinality == 0), Is.True);
    }

    private static LocalPlaceKnowledgeObservation Place(string placeId, long day, string name)
    {
        return new LocalPlaceKnowledgeObservation("site-owner", placeId, null, null, name, false,
            day, day, LocalTopologyKnowledgeSource.DirectObservation);
    }

    private static LocalConnectionKnowledgeObservation Connection(string connectionId, string from, string to, long day)
    {
        return new LocalConnectionKnowledgeObservation("site-owner", connectionId, from, to, 1f, null,
            day, day, LocalTopologyKnowledgeSource.DirectObservation);
    }

    private static AdventureOppositionObservation Opposition(string id, long day)
    {
        return new AdventureOppositionObservation("site", null, id, AdventureOppositionObservedState.Active,
            day, day, AdventureIntelSource.DirectObservation);
    }

    private static AdventureNotableItemObservation Notable(string id, long day)
    {
        return new AdventureNotableItemObservation("site", null, id, "definition-" + id,
            day, day, AdventureIntelSource.DirectObservation);
    }

    private static AdventureCommonResourceObservation Resource(string id, long day)
    {
        return new AdventureCommonResourceObservation("site", null, id, 1, "resource",
            day, day, AdventureIntelSource.DirectObservation);
    }

    private static AdventureAccessObservation Access(long day, string localPlace = null)
    {
        return new AdventureAccessObservation("site", localPlace, default(PlaceAccessState),
            day, day, AdventureIntelSource.DirectObservation);
    }

    private static void SetRevision(object owner, long revision)
    {
        owner.GetType().GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, revision);
    }

    private static NpcRuntime SetRuntimeId(NpcRuntime npc, string runtimeId)
    {
        typeof(NpcRuntime).GetField("runtimeId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc, runtimeId);
        return npc;
    }

    private static ContinuationCensusProtocol CreateKnowledgeOnlyProtocol(IReadOnlyList<NpcRuntime> roster)
    {
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterNpcKnowledgeRosterFamily(roster, out ContinuationCensusFailure registration), Is.True,
            registration.ToString());
        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("runtime.advance", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure baseline), Is.True,
            baseline.ToString());
        return protocol;
    }

    private static ContinuationCensusProtocol CreateCompleteNpcFamilyProtocol(
        PersonStore personStore,
        IReadOnlyList<NpcRuntime> roster,
        bool includeInventory = false)
    {
        IReadOnlyList<IOwnerSectionCensusProvider> personProviders = PersonStoreCensusProvider.CreateProviders(personStore);
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterExpectedSection(new OwnerSectionContract(
            PersonMembershipCensusProvider.SectionId, PersonMembershipCensusProvider.SchemaVersion,
            OwnerSectionRole.Required), out _), Is.True);
        Assert.That(protocol.RegisterExpectedSection(new OwnerSectionContract(
            PersonMaterializationBindingCensusProvider.SectionId, PersonMaterializationBindingCensusProvider.SchemaVersion,
            OwnerSectionRole.Required), out _), Is.True);
        Assert.That(protocol.RegisterCensusProvider(PersonMembershipCensusProvider.SectionId, personProviders[0], out _), Is.True);
        Assert.That(protocol.RegisterCensusProvider(PersonMaterializationBindingCensusProvider.SectionId, personProviders[1], out _), Is.True);
        Assert.That(protocol.RegisterSpatialKnowledgeRosterFamily(roster, out ContinuationCensusFailure spatialFailure), Is.True,
            spatialFailure.ToString());
        if (includeInventory)
        {
            foreach (NpcRuntime npc in roster) _ = npc.Inventory;
            Assert.That(protocol.RegisterInventoryRosterFamily(roster, out ContinuationCensusFailure inventoryFailure), Is.True,
                inventoryFailure.ToString());
        }
        Assert.That(protocol.RegisterNpcKnowledgeRosterFamily(roster, out ContinuationCensusFailure knowledgeFailure), Is.True,
            knowledgeFailure.ToString());
        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("runtime.npc-membership", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure baseline), Is.True,
            baseline.ToString());
        return protocol;
    }

    private static void ReconcileNpcFamily(ContinuationCensusProtocol protocol, PersonStore personStore)
    {
        Assert.That(protocol.TryEnterOperation("runtime.npc-membership", out SimulationOperationScope scope, out ContinuationCensusFailure enter), Is.True,
            enter.ToString());
        try
        {
            Assert.That(protocol.TryReconcileSpatialKnowledgeRosterAndNotifyCommittedMutations(
                new string[0], personStore.Revision, out ContinuationCensusFailure reconcile), Is.True,
                reconcile.ToString());
        }
        finally
        {
            scope.Dispose();
        }
    }

    private static long ReadEpoch(ContinuationCensusProtocol protocol)
    {
        Assert.That(protocol.TryReadMutationEpoch(out long epoch, out ContinuationCensusFailure failure), Is.True,
            failure.ToString());
        return epoch;
    }
}

internal sealed class ReferenceEqualityComparer : System.Collections.Generic.IEqualityComparer<object>
{
    public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
    public new bool Equals(object x, object y) => ReferenceEquals(x, y);
    public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
