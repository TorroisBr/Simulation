using System;
using System.Collections.Generic;
using System.Reflection;
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
        long spatialRevision = merchant.SpatialKnowledge.Revision;

        item.basePrice += 15f;
        city.UpdateMarketPrices();
        Assert.That(prepared.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(merchant.CommercialKnowledge.Revision, Is.EqualTo(knowledgeRevision));
        Assert.That(merchant.SpatialKnowledge.Revision, Is.EqualTo(spatialRevision));

        Assert.That(provider.TryPrepareStep(manifest, steps[0],
            out IBoundaryContinuationStepCommit replay, out failure), Is.True);
        Assert.That(replay.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(city.Location.RuntimeId,
            item.DefinitionId, out CommercialMarketObservation replayed), Is.True);
        Assert.That(replayed.ObservedStock, Is.EqualTo(7));
        Assert.That(replayed.ObservedPrice, Is.EqualTo(observedPrice));
        Assert.That(merchant.CommercialKnowledge.Revision, Is.EqualTo(knowledgeRevision));
        Assert.That(merchant.SpatialKnowledge.Revision, Is.EqualTo(spatialRevision));
    }

    [Test]
    public void SelectedActorPublishesLocalObservationReceiptToCoreOwnerAndKeepsMixedRosterFallback()
    {
        NpcRuntime selected = CreateSelectedMerchant("p18d-core-local-receipt", out _);
        DailyV1NpcLocalObservationReceiptOwner coreOwner =
            selected.SelectedRuntimeOwners.LocalObservationReceipts;
        NpcRuntime compatibility = new NpcRuntime("p18d-legacy-local-receipt",
            SimulationTestFactory.CreateNpc("p18d-legacy-local-receipt"));
        IReadOnlyList<IOwnerSectionCensusProvider> receiptProviders =
            P12DNpcReceiptOwnerCensusProvider.CreateProviders(new[] { compatibility, selected });
        P12DNpcReceiptOwnerCensusProvider.IReceiptOwnerSectionCensusProvider selectedProvider = null;
        P12DNpcReceiptOwnerCensusProvider.IReceiptOwnerSectionCensusProvider compatibilityProvider = null;
        foreach (IOwnerSectionCensusProvider candidate in receiptProviders)
        {
            var typed = candidate as P12DNpcReceiptOwnerCensusProvider.IReceiptOwnerSectionCensusProvider;
            if (typed == null || typed.Kind != P12DNpcReceiptOwnerCensusProvider.ReceiptOwnerKind.LocalObservation)
                continue;
            if (typed.RuntimeId == selected.RuntimeId) selectedProvider = typed;
            if (typed.RuntimeId == compatibility.RuntimeId) compatibilityProvider = typed;
        }
        Assert.That(selectedProvider, Is.Not.Null);
        Assert.That(selectedProvider.ReceiptOwner, Is.SameAs(coreOwner),
            "Selected receipt census identity must be the exact Core owner used by the owner graph.");
        Assert.That(compatibilityProvider, Is.Not.Null);
        Assert.That(compatibilityProvider.ReceiptOwner,
            Is.SameAs(compatibility.ExistingLocalKnowledgeObservationRuntime),
            "Unselected mixed-roster NPCs retain the Unity compatibility owner.");

        NpcLocalKnowledgeDailyBoundaryStepProvider producer =
            new NpcLocalKnowledgeDailyBoundaryStepProvider(() => new[] { selected }, true);
        DailyBoundaryOperation operation = new DailyBoundaryOperation(
            "p18d-core-local-receipt-world", "intraday", 12L);
        Assert.That(producer.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "configuration/v1", steps);
        Assert.That(producer.TryPrepareStep(manifest, steps[0],
            out IBoundaryContinuationStepCommit prepared, out failure), Is.True, failure.ToString());
        PreparedNpcLocalKnowledgeObservationCommit exactPrepared =
            prepared as PreparedNpcLocalKnowledgeObservationCommit;
        Assert.That(exactPrepared, Is.Not.Null);
        Assert.That(exactPrepared.ExpectedOwnerRevision, Is.EqualTo(coreOwner.Revision),
            "The selected Core receipt install must be prepared from the bound owner's current revision.");
        Assert.That(exactPrepared.IsReplay, Is.False);
        Assert.That(exactPrepared.Owner.Revision, Is.EqualTo(exactPrepared.ExpectedOwnerRevision));
        Assert.That(exactPrepared.Actor.RuntimeId, Is.EqualTo(exactPrepared.Receipt.ActorRuntimeId));
        Assert.That(exactPrepared.Actor.PersonId != null ? exactPrepared.Actor.PersonId.Value : string.Empty,
            Is.EqualTo(exactPrepared.Receipt.PersonId));
        Assert.That(exactPrepared.Owner.TryGetReceipt(exactPrepared.Receipt.OperationIdentity, out _),
            Is.False, "The new operation identity must not already have a published receipt.");
        MethodInfo tryCapture = typeof(NpcLocalKnowledgeObservationRuntime).GetMethod("TryCapture",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(tryCapture, Is.Not.Null);
        object[] captureArguments = { selected, exactPrepared.AbsoluteDay,
            exactPrepared.MerchantKnowledgeEnabled, exactPrepared.IncludeCurrentLocation, null };
        Assert.That((bool)tryCapture.Invoke(null, captureArguments), Is.True,
            "The observation source must remain capturable before installation.");
        NpcLocalKnowledgeObservationSnapshot currentSnapshot =
            captureArguments[4] as NpcLocalKnowledgeObservationSnapshot;
        Assert.That(currentSnapshot, Is.Not.Null);
        Assert.That(currentSnapshot.Fingerprint, Is.EqualTo(exactPrepared.SourceSnapshot.Fingerprint),
            "The observation's source fingerprint must remain stable before install.");
        Assert.That(currentSnapshot.CurrentLocation, Is.SameAs(exactPrepared.SourceSnapshot.CurrentLocation));
        Assert.That(currentSnapshot.CurrentCity, Is.SameAs(exactPrepared.SourceSnapshot.CurrentCity));
        Assert.That(currentSnapshot.CityLocation, Is.SameAs(exactPrepared.SourceSnapshot.CityLocation));
        Assert.That(currentSnapshot.Market, Is.SameAs(exactPrepared.SourceSnapshot.Market));
        Assert.That(currentSnapshot.Counterparty, Is.SameAs(exactPrepared.SourceSnapshot.Counterparty));
        Assert.That(currentSnapshot.ItemReferences.Count,
            Is.EqualTo(exactPrepared.SourceSnapshot.ItemReferences.Count));
        Assert.That(currentSnapshot.MarketItemReferences.Count,
            Is.EqualTo(exactPrepared.SourceSnapshot.MarketItemReferences.Count));
        for (int itemIndex = 0; itemIndex < currentSnapshot.ItemReferences.Count; itemIndex++)
        {
            Assert.That(currentSnapshot.ItemReferences[itemIndex],
                Is.SameAs(exactPrepared.SourceSnapshot.ItemReferences[itemIndex]));
            Assert.That(currentSnapshot.MarketItemReferences[itemIndex],
                Is.Not.SameAs(exactPrepared.SourceSnapshot.MarketItemReferences[itemIndex]),
                "A selected Core market read materializes a new host projection row.");
            Assert.That(currentSnapshot.MarketItemReferences[itemIndex].SelectedDefinition,
                Is.SameAs(exactPrepared.SourceSnapshot.MarketItemReferences[itemIndex].SelectedDefinition));
            Assert.That(currentSnapshot.MarketItemReferences[itemIndex].Amount,
                Is.EqualTo(exactPrepared.SourceSnapshot.MarketItemReferences[itemIndex].Amount));
            Assert.That(currentSnapshot.MarketItemReferences[itemIndex].DesiredAmount,
                Is.EqualTo(exactPrepared.SourceSnapshot.MarketItemReferences[itemIndex].DesiredAmount));
            Assert.That(currentSnapshot.MarketItemReferences[itemIndex].CurrentPrice,
                Is.EqualTo(exactPrepared.SourceSnapshot.MarketItemReferences[itemIndex].CurrentPrice));
        }
        Assert.That(currentSnapshot.Matches(exactPrepared.SourceSnapshot), Is.True,
            "The source facts must remain unchanged between preparation and atomic installation.");
        Assert.That(exactPrepared.SpatialInstall.CanInstall(selected.SpatialKnowledge), Is.True,
            "The observation's spatial install must remain current until the atomic commit.");
        Assert.That(exactPrepared.CommercialInstall.CanInstall(selected.CommercialKnowledge), Is.True,
            "The observation's commercial install must remain current until the atomic commit.");
        Assert.That(coreOwner.CanInstall(exactPrepared.SelectedCoreInstall), Is.True,
            "The preallocated Core receipt install must still target the exact selected owner state.");
        Assert.That(prepared.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(coreOwner.TryReadP12ReceiptCensus(
            out int cardinality, out long revision), Is.True);
        Assert.That(cardinality, Is.EqualTo(1));
        Assert.That(revision, Is.EqualTo(1L));
        Assert.That(selected.LocalKnowledgeObservationRuntime.Revision, Is.EqualTo(1L),
            "The compatibility-facing producer reads the Core owner's revision rather than a second ledger.");

        FieldInfo legacyRevision = typeof(NpcLocalKnowledgeObservationRuntime).GetField(
            "revision", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo legacyReceipts = typeof(NpcLocalKnowledgeObservationRuntime).GetField(
            "receipts", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(legacyRevision.GetValue(selected.ExistingLocalKnowledgeObservationRuntime), Is.EqualTo(0L));
        Assert.That(((System.Collections.ICollection)legacyReceipts.GetValue(
            selected.ExistingLocalKnowledgeObservationRuntime)).Count, Is.Zero,
            "Selected receipt publication leaves the serialized Unity compatibility ledger empty.");

        long spatialRevision = selected.SpatialKnowledge.Revision;
        long commercialRevision = selected.CommercialKnowledge.Revision;
        Assert.That(producer.TryPrepareStep(manifest, steps[0],
            out IBoundaryContinuationStepCommit replay, out failure), Is.True, failure.ToString());
        Assert.That(replay.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(coreOwner.Revision, Is.EqualTo(1L),
            "A matching operation replay resolves the Core receipt without a second publication.");
        Assert.That(selected.SpatialKnowledge.Revision, Is.EqualTo(spatialRevision));
        Assert.That(selected.CommercialKnowledge.Revision, Is.EqualTo(commercialRevision));

        DailyBoundaryOperation staleOperation = new DailyBoundaryOperation(
            "p18d-core-local-receipt-world", "intraday", 13L);
        Assert.That(producer.TryCreateSteps(staleOperation, 0,
            out IReadOnlyList<BoundaryContinuationStep> staleSteps, out failure), Is.True);
        BoundaryContinuationManifest staleManifest = new BoundaryContinuationManifest(
            staleOperation, "daily-boundary", "1", "configuration/v1", staleSteps);
        Assert.That(producer.TryPrepareStep(staleManifest, staleSteps[0],
            out IBoundaryContinuationStepCommit stalePrepared, out failure), Is.True,
            failure.ToString());
        DailyV1MarketOwner marketOwner = selected.CurrentCity.Market.SelectedDailyV1Owner;
        DailyV1ItemDefinition itemDefinition =
            selected.CurrentCity.Market.Items[0].SelectedDefinition;
        Assert.That(marketOwner, Is.Not.Null);
        Assert.That(itemDefinition, Is.Not.Null);
        Assert.That(marketOwner.AddStock(itemDefinition, 1), Is.EqualTo(1),
            "Use the selected authoritative Market owner to stale the prepared observation.");
        Assert.That(stalePrepared.TryCommit(out failure), Is.False,
            "Changed selected market facts must reject a stale observation despite fresh projections.");
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(coreOwner.Revision, Is.EqualTo(1L),
            "A stale observation must not publish its prepared Core receipt.");
        Assert.That(selected.SpatialKnowledge.Revision, Is.EqualTo(spatialRevision),
            "A stale source must not partially install spatial knowledge.");
        Assert.That(selected.CommercialKnowledge.Revision, Is.EqualTo(commercialRevision),
            "A stale source must not partially install commercial knowledge.");
        Assert.Throws<InvalidOperationException>(() => selectedProvider.GetCurrentCensus(),
            "The populated receipt remains outside the accepted exact-empty P12 Daily-v1 profile.");
    }

    private static NpcRuntime CreateSelectedMerchant(
        string prefix, out DailyV1EffectiveDefinitions definitions)
    {
        CityData cityAsset = SimulationTestFactory.CreateCityData(prefix + "-city");
        NpcData npcAsset = SimulationTestFactory.CreateNpc(prefix + "-npc", NpcJobType.Merchant);
        ItemData item = SimulationTestFactory.CreateItem(prefix + "-item", 12f);
        cityAsset.marketItems.Add(new MarketItemConfig
        {
            item = item,
            initialAmount = 7,
            desiredAmount = 10
        });
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(cityAsset);
        config.Npcs.Add(new NpcSimulationConfig { npc = npcAsset, startingCity = cityAsset });
        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: config.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(config.Calendar, out _);
        definitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
            config, effective, calendar, out DailyV1DefinitionAdapter.SelectedBindings bindings);
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        if (!builder.TryCreateConfiguredCities(allocator, _ => true)
            || !builder.TryCreateConfiguredNpcs(allocator,
                cityId => builder.ResolveUniqueStartingCity(cityId)?.RuntimeId)
            || !builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph))
            throw new InvalidOperationException("Could not create the selected LocalObservation fixture graph.");
        DailyV1CityRuntimeOwners cityOwner = graph.Cities[0];
        DailyV1NpcRuntimeOwners npcOwner = graph.Npcs[0];
        if (!graph.SpatialNetwork.TryGetLocation(cityOwner.LocationRuntimeId,
                out SpatialLocationRuntime location))
            throw new InvalidOperationException("The selected LocalObservation fixture has no canonical city location.");
        CityRuntime city = CityRuntime.CreateSelectedDailyV1(
            cityOwner.RuntimeId, cityOwner.Definition, definitions, bindings, cityOwner, location);
        NpcRuntime actor = NpcRuntime.CreateSelectedDailyV1(
            npcOwner.RuntimeId, npcOwner.Definition, definitions, bindings, npcOwner, city);
        if (!actor.TryBindSelectedDailyV1Definition(
                npcOwner.Definition, definitions, bindings,
                runtimeId => string.Equals(runtimeId, city.RuntimeId, StringComparison.Ordinal) ? city : null)
            || !actor.TryBindSelectedRuntimeOwnerGraphProvider(() => graph))
            throw new InvalidOperationException("Could not attach the selected LocalObservation Unity adapter.");
        return actor;
    }

    [Test]
    public void PostShareMarketObservationHasDistinctReceiptAndRetainsItsOwnSnapshot()
    {
        ItemData item = SimulationTestFactory.CreateItem("p18d-postshare-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("p18d-postshare-city",
            "p18d-postshare-location",
            new MarketItemConfig { item = item, initialAmount = 6, desiredAmount = 10 });
        NpcRuntime merchant = new NpcRuntime("p18d-postshare-merchant",
            SimulationTestFactory.CreateNpc("p18d-postshare-merchant", NpcJobType.Merchant),
            city, 20f);
        NpcLocalKnowledgeDailyBoundaryStepProvider provider =
            new NpcLocalKnowledgeDailyBoundaryStepProvider(() => new[] { merchant }, true);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 8L);
        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "configuration", steps);

        Assert.That(provider.TryPrepareStep(manifest, steps[0],
            out IBoundaryContinuationStepCommit firstObservation, out failure), Is.True);
        Assert.That(firstObservation.TryCommit(out failure), Is.True);
        float preSharePrice = city.Market.Items[0].CurrentPrice;

        item.basePrice += 20f;
        city.UpdateMarketPrices();
        float postSharePrice = city.Market.Items[0].CurrentPrice;
        Assert.That(postSharePrice, Is.Not.EqualTo(preSharePrice));
        Assert.That(provider.TryPreparePostShareMarketObservation(
            merchant, manifest, steps[0], out IBoundaryContinuationStepCommit postShare,
            out failure), Is.True);
        Assert.That(postShare.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(city.Location.RuntimeId,
            item.DefinitionId, out CommercialMarketObservation captured), Is.True);
        // Same-day direct observations have equal source priority, so the
        // CommercialKnowledge owner intentionally retains its earlier fact.
        // The post-share occurrence still commits and retains its own source
        // snapshot for deterministic replay without rewriting that fact.
        Assert.That(captured.ObservedPrice, Is.EqualTo(preSharePrice));

        long spatialRevision = merchant.SpatialKnowledge.Revision;
        long commercialRevision = merchant.CommercialKnowledge.Revision;
        item.basePrice += 20f;
        city.UpdateMarketPrices();
        Assert.That(provider.TryPreparePostShareMarketObservation(
            merchant, manifest, steps[0], out IBoundaryContinuationStepCommit replay,
            out failure), Is.True);
        Assert.That(replay.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(city.Location.RuntimeId,
            item.DefinitionId, out CommercialMarketObservation replayed), Is.True);
        Assert.That(replayed.ObservedPrice, Is.EqualTo(preSharePrice));
        Assert.That(merchant.SpatialKnowledge.Revision, Is.EqualTo(spatialRevision));
        Assert.That(merchant.CommercialKnowledge.Revision, Is.EqualTo(commercialRevision));
    }

    [Test]
    public void PostShareMarketObservationRejectsStaleMarketBeforeEitherKnowledgeInstall()
    {
        ItemData item = SimulationTestFactory.CreateItem("p18d-postshare-stale-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("p18d-postshare-stale-city",
            "p18d-postshare-stale-location",
            new MarketItemConfig { item = item, initialAmount = 6, desiredAmount = 10 });
        NpcRuntime merchant = new NpcRuntime("p18d-postshare-stale-merchant",
            SimulationTestFactory.CreateNpc("p18d-postshare-stale-merchant", NpcJobType.Merchant),
            city, 20f);
        NpcLocalKnowledgeDailyBoundaryStepProvider provider =
            new NpcLocalKnowledgeDailyBoundaryStepProvider(() => new[] { merchant }, true);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 9L);
        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "configuration", steps);
        Assert.That(provider.TryPreparePostShareMarketObservation(merchant, manifest,
            steps[0], out IBoundaryContinuationStepCommit prepared, out failure), Is.True);

        item.basePrice += 15f;
        city.UpdateMarketPrices();
        Assert.That(prepared.TryCommit(out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(merchant.SpatialKnowledge.KnowsLocation(city.Location.RuntimeId), Is.False);
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(city.Location.RuntimeId,
            item.DefinitionId, out _), Is.False);
    }

    [Test]
    public void ActorStepRejectsSynthesizedDescriptorEvenWhenManifestContainsIt()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("p18d-local-synthesized-city",
            "p18d-local-synthesized-location");
        NpcRuntime actor = new NpcRuntime("p18d-local-synthesized-actor",
            SimulationTestFactory.CreateNpc("p18d-local-synthesized-actor"), city, 0f);
        NpcLocalKnowledgeDailyBoundaryStepProvider provider =
            new NpcLocalKnowledgeDailyBoundaryStepProvider(() => new[] { actor }, false);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 4L);

        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> frozenSteps, out TimelineFailure failure), Is.True);
        BoundaryContinuationStep frozen = frozenSteps[0];
        BoundaryContinuationStep synthesized = new BoundaryContinuationStep(
            frozen.Ordinal, frozen.StepId, frozen.OwnerId, frozen.OperationKind,
            frozen.OperationVersion, frozen.OwnerRevision, frozen.Payload,
            frozen.PersonId, frozen.Disposition);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "configuration", new[] { synthesized });

        Assert.That(provider.TryPrepareStep(manifest, synthesized,
            out IBoundaryContinuationStepCommit _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(actor.SpatialKnowledge.Revision, Is.Zero);
        Assert.That(actor.CommercialKnowledge.Revision, Is.Zero);
        Assert.That(actor.SpatialKnowledge.KnowsLocation(city.Location.RuntimeId), Is.False);
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
