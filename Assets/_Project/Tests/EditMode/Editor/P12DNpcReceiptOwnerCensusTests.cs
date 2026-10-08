using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class P12DNpcReceiptOwnerCensusTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void ProviderFamilyHasExactlyTwoRequiredReadyWitnessesPerNpcInStableOrder()
    {
        NpcRuntime second = CreateNpc("p12d-receipt-z");
        NpcRuntime first = CreateNpc("p12d-receipt-a");
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            P12DNpcReceiptOwnerCensusProvider.CreateProviders(new[] { second, first });

        Assert.That(providers.Count, Is.EqualTo(4));
        Assert.That(P12DNpcReceiptOwnerCensusProvider.IsExactCoverage(
            new[] { second, first }, providers), Is.True);
        AssertWitness(providers[0], first,
            P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(first.RuntimeId),
            first.ExistingLocalKnowledgeObservationRuntime);
        AssertWitness(providers[1], first,
            P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(first.RuntimeId),
            first.ExistingMerchantTradeStateRuntime);
        AssertWitness(providers[2], second,
            P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(second.RuntimeId),
            second.ExistingLocalKnowledgeObservationRuntime);
        AssertWitness(providers[3], second,
            P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(second.RuntimeId),
            second.ExistingMerchantTradeStateRuntime);

        Assert.Throws<ArgumentException>(() =>
            P12DNpcReceiptOwnerCensusProvider.CreateProviders(new[] { first, first }));
        Assert.Throws<ArgumentException>(() =>
            P12DNpcReceiptOwnerCensusProvider.CreateProviders(new[]
            {
                first,
                CreateNpc(first.RuntimeId)
            }));

        NpcRuntime aliased = CreateNpc("p12d-receipt-alias");
        SetPrivateField(aliased, "localKnowledgeObservationRuntime",
            first.ExistingLocalKnowledgeObservationRuntime);
        Assert.Throws<InvalidOperationException>(() =>
            P12DNpcReceiptOwnerCensusProvider.CreateProviders(new[] { first, aliased }));
    }

    [TestCase("local")]
    [TestCase("merchant")]
    public void SelectedRuntimeVectorIncludesRequiredExactReceiptRowsAndEachReceiptMutationStalesToken(
        string ownerKind)
    {
        NpcRuntime first = CreateNpc("p12d-vector-a-" + ownerKind);
        NpcRuntime second = CreateNpc("p12d-vector-b-" + ownerKind);
        SimulationRuntime runtime = CreateDailyRuntime(new[] { second, first });

        Assert.That(runtime.NpcReceiptOwnerCensusProviders.Count, Is.EqualTo(4));
        Assert.That(runtime.HasSameP12DNpcReceiptOwnerCensusProviders(
            runtime.NpcReceiptOwnerCensusProviders), Is.True);
        long initialEpoch = ReadEpoch(runtime);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment),
            Is.True, assessment.ToString());
        Assert.That(ReadEpoch(runtime), Is.EqualTo(initialEpoch),
            "read-only admission assessment must not create a mutation epoch");

        ContinuationCensusProtocol protocol = ReadPrivateField<ContinuationCensusProtocol>(
            runtime, "npcRosterCensusProtocol");
        Assert.That(protocol.TryCaptureQuiescentOwnerSectionSnapshot(
            out IReadOnlyList<OwnerSectionCensusSnapshot> snapshots,
            out _, out ContinuationCensusFailure captureFailure),
            Is.True, captureFailure.ToString());
        Assert.That(ReadEpoch(runtime), Is.EqualTo(initialEpoch),
            "read-only owner-vector capture must not create a mutation epoch");

        List<OwnerSectionCensusSnapshot> receiptRows = new List<OwnerSectionCensusSnapshot>();
        foreach (OwnerSectionCensusSnapshot snapshot in snapshots)
        {
            if (snapshot.SectionId.StartsWith("p12d.npc-", StringComparison.Ordinal))
                receiptRows.Add(snapshot);
        }
        Assert.That(receiptRows.Count, Is.EqualTo(4));
        foreach (OwnerSectionCensusSnapshot row in receiptRows)
        {
            Assert.That(row.SchemaVersion, Is.EqualTo(1));
            Assert.That(row.Role, Is.EqualTo(OwnerSectionRole.Required));
            Assert.That(row.Cardinality, Is.Zero);
            Assert.That(row.Revision, Is.Zero);
        }

        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());
        foreach (NpcRuntime npc in new[] { first, second })
        {
            AssertTokenRow(token, P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(npc.RuntimeId),
                npc.ExistingLocalKnowledgeObservationRuntime);
            AssertTokenRow(token, P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(npc.RuntimeId),
                npc.ExistingMerchantTradeStateRuntime);
        }

        if (ownerKind == "local")
            CommitLocalObservationReceipt(first, establishPresence: false);
        else
            CommitMerchantReceipt(first);
        object receiptOwner = GetReceiptOwner(first, ownerKind);
        Assert.That(TryReadOwner(receiptOwner, ownerKind,
            out int populatedCount, out long populatedRevision), Is.True);
        Assert.That(populatedCount, Is.EqualTo(1));
        Assert.That(populatedRevision, Is.EqualTo(1L));
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(
            token, out DailyCaptureEligibilityFailure staleFailure), Is.False);
        Assert.That(staleFailure, Is.Not.EqualTo(DailyCaptureEligibilityFailure.None));
    }

    [TestCase("local")]
    [TestCase("merchant")]
    public void EveryProviderReadRejectsACommittedReceipt(string ownerKind)
    {
        NpcRuntime npc = CreateNpc("p12d-populated-" + ownerKind);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            P12DNpcReceiptOwnerCensusProvider.CreateProviders(new[] { npc });

        if (ownerKind == "local") CommitLocalObservationReceipt(npc);
        else CommitMerchantReceipt(npc);

        IOwnerSectionCensusProvider populatedProvider = providers[
            ownerKind == "local" ? 0 : 1];
        for (int read = 0; read < 2; read++)
            Assert.Throws<InvalidOperationException>(() => populatedProvider.GetCurrentCensus());
    }

    [TestCase("local")]
    [TestCase("merchant")]
    public void NonzeroRevisionWithEmptyLedgerIsNotAdmittedAndNullLedgerIsNeverInitialized(
        string ownerKind)
    {
        NpcRuntime npc = CreateNpc("p12d-malformed-" + ownerKind);
        object owner = GetReceiptOwner(npc, ownerKind);
        string revisionField = "revision";
        IReadOnlyList<IOwnerSectionCensusProvider> providers = CreateProvidersFromOwner(npc);
        foreach (long invalidRevision in new[] { 1L, long.MaxValue, -1L })
        {
            SetPrivateField(owner, revisionField, invalidRevision);
            Assert.Throws<InvalidOperationException>(() => providers[
                ownerKind == "local" ? 0 : 1].GetCurrentCensus());
        }

        SetPrivateField(owner, revisionField, 0L);
        SetPrivateField(owner, "receipts", null);
        Assert.That(TryReadOwner(owner, ownerKind, out int cardinality, out long revision), Is.False);
        Assert.That(cardinality, Is.Zero);
        Assert.That(revision, Is.Zero);
        Assert.That(GetPrivateField(owner, "receipts"), Is.Null,
            "the raw census read must not repair or allocate a missing ledger");
        Assert.Throws<InvalidOperationException>(() => CreateProvidersFromOwner(npc));
        Assert.That(GetPrivateField(owner, "receipts"), Is.Null);
    }

    [Test]
    public void NullEmbeddedOwnerFailsSelectedAdmissionWithoutInvokingLazyAccessor()
    {
        NpcRuntime npc = CreateNpc("p12d-missing-child");
        SetPrivateField(npc, "localKnowledgeObservationRuntime", null);

        Assert.Throws<InvalidOperationException>(() => CreateDailyRuntime(new[] { npc }));
        Assert.That(npc.ExistingLocalKnowledgeObservationRuntime, Is.Null);
    }

    [Test]
    public void ReplacedEmbeddedOwnerFaultsTheExistingCensusBoundary()
    {
        NpcRuntime npc = CreateNpc("p12d-replaced-child");
        SimulationRuntime runtime = CreateDailyRuntime(new[] { npc });
        NpcLocalKnowledgeObservationRuntime replacement =
            new NpcLocalKnowledgeObservationRuntime();
        SetPrivateField(npc, "localKnowledgeObservationRuntime", replacement);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(npc.ExistingLocalKnowledgeObservationRuntime, Is.SameAs(replacement));
    }

    [Test]
    public void CommittedRosterAddRemoveAndSameObjectReAddReconcileBothRowsOnce()
    {
        SimulationRuntime runtime = CreateIdentityBoundDailyRuntime(out _);
        Assert.That(runtime.NpcRuntimes.Count, Is.EqualTo(10));
        Assert.That(runtime.NpcReceiptOwnerCensusProviders.Count, Is.EqualTo(20));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initial),
            Is.True, initial.ToString());

        NpcRuntime added = CreateNpc("p12d-roster-added");
        NpcLocalKnowledgeObservationRuntime localOwner = added.ExistingLocalKnowledgeObservationRuntime;
        NpcMerchantTradeStateRuntime merchantOwner = added.ExistingMerchantTradeStateRuntime;
        long epoch = ReadEpoch(runtime);

        Assert.That(runtime.TryRegisterNpc(added, out WorldNpcRegistryFailure addFailure),
            Is.True, addFailure.ToString());
        Assert.That(runtime.NpcRuntimes.Count, Is.EqualTo(11));
        Assert.That(runtime.NpcReceiptOwnerCensusProviders.Count, Is.EqualTo(22));
        Assert.That(ReadEpoch(runtime), Is.EqualTo(epoch + 1L));
        AssertProviderOwner(runtime, P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(added.RuntimeId),
            added, localOwner);
        AssertProviderOwner(runtime, P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(added.RuntimeId),
            added, merchantOwner);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterAdd),
            Is.True, afterAdd.ToString());
        Assert.That(runtime.HasSameP12DNpcReceiptOwnerCensusProviders(
            runtime.NpcReceiptOwnerCensusProviders), Is.True);

        epoch = ReadEpoch(runtime);
        Assert.That(runtime.TryUnregisterNpc(added.RuntimeId, out WorldNpcRegistryFailure removeFailure),
            Is.True, removeFailure.ToString());
        Assert.That(runtime.NpcRuntimes.Count, Is.EqualTo(10));
        Assert.That(runtime.NpcReceiptOwnerCensusProviders.Count, Is.EqualTo(20));
        Assert.That(ReadEpoch(runtime), Is.EqualTo(epoch + 1L));
        AssertNoProvider(runtime, P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(added.RuntimeId));
        AssertNoProvider(runtime, P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(added.RuntimeId));
        Assert.That(runtime.HasSameP12DNpcReceiptOwnerCensusProviders(
            runtime.NpcReceiptOwnerCensusProviders), Is.True);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterRemove),
            Is.True, afterRemove.ToString());

        epoch = ReadEpoch(runtime);
        Assert.That(runtime.TryRegisterNpc(added, out WorldNpcRegistryFailure readdFailure),
            Is.True, readdFailure.ToString());
        Assert.That(runtime.NpcReceiptOwnerCensusProviders.Count, Is.EqualTo(22));
        Assert.That(ReadEpoch(runtime), Is.EqualTo(epoch + 1L));
        AssertProviderOwner(runtime, P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(added.RuntimeId),
            added, localOwner);
        AssertProviderOwner(runtime, P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(added.RuntimeId),
            added, merchantOwner);

        epoch = ReadEpoch(runtime);
        Assert.That(runtime.TryUnregisterNpc(added.RuntimeId, out _), Is.True);
        long afterExactRemoval = ReadEpoch(runtime);
        Assert.That(afterExactRemoval, Is.EqualTo(epoch + 1L));
        NpcRuntime replacement = CreateNpc(added.RuntimeId);
        Assert.That(runtime.TryRegisterNpc(replacement, out WorldNpcRegistryFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure, Is.EqualTo(WorldNpcRegistryFailure.DuplicateRuntimeId));
        Assert.That(ReadEpoch(runtime), Is.EqualTo(afterExactRemoval));
        Assert.That(runtime.NpcReceiptOwnerCensusProviders.Count, Is.EqualTo(20));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterRejectedAdd),
            Is.True, afterRejectedAdd.ToString());
    }

    [TestCase("local")]
    [TestCase("merchant")]
    public void CommittedRosterAddWithPopulatedReceiptFaultsWithoutPartialRowsOrEpochAdvance(
        string ownerKind)
    {
        SimulationRuntime runtime = CreateIdentityBoundDailyRuntime(out _);
        NpcRuntime added = CreateNpc("p12d-populated-roster-add-" + ownerKind);
        if (ownerKind == "local")
            CommitLocalObservationReceipt(added, establishPresence: false);
        else
            CommitMerchantReceipt(added);

        object receiptOwner = GetReceiptOwner(added, ownerKind);
        Assert.That(TryReadOwner(receiptOwner, ownerKind,
            out int cardinality, out long revision), Is.True);
        Assert.That(cardinality, Is.EqualTo(1));
        Assert.That(revision, Is.EqualTo(1L));

        ContinuationCensusProtocol protocol = ReadPrivateField<ContinuationCensusProtocol>(
            runtime, "npcRosterCensusProtocol");
        long startingEpoch = ReadPrivateField<long>(protocol, "mutationEpoch");
        Assert.That(runtime.NpcReceiptOwnerCensusProviders.Count, Is.EqualTo(20));

        Assert.That(runtime.TryRegisterNpc(added, out WorldNpcRegistryFailure addFailure),
            Is.True, addFailure.ToString());
        Assert.That(addFailure, Is.EqualTo(WorldNpcRegistryFailure.None));
        Assert.That(runtime.NpcRuntimes, Does.Contain(added),
            "the membership write commits before the census reconciliation observes the malformed new owner");

        Assert.That(runtime.NpcReceiptOwnerCensusProviders.Count, Is.EqualTo(20),
            "failed family reconciliation must not publish either new receipt row");
        AssertNoProvider(runtime,
            P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(added.RuntimeId));
        AssertNoProvider(runtime,
            P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(added.RuntimeId));
        Assert.That(ReadPrivateField<long>(protocol, "mutationEpoch"), Is.EqualTo(startingEpoch),
            "failed family reconciliation must not advance the shared epoch");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    private static void AssertWitness(
        IOwnerSectionCensusProvider provider,
        NpcRuntime npc,
        string sectionId,
        object exactOwner)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.SectionId, Is.EqualTo(sectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(P12DNpcReceiptOwnerCensusProvider.SchemaVersion));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(exactOwner));
        Assert.That(witness.Cardinality, Is.Zero);
        Assert.That(witness.Revision, Is.Zero);
        Assert.That(provider,
            Is.InstanceOf<P12DNpcReceiptOwnerCensusProvider.IReceiptOwnerSectionCensusProvider>());
        P12DNpcReceiptOwnerCensusProvider.IReceiptOwnerSectionCensusProvider typed =
            (P12DNpcReceiptOwnerCensusProvider.IReceiptOwnerSectionCensusProvider)provider;
        Assert.That(typed.NpcOwner, Is.SameAs(npc));
        Assert.That(typed.ReceiptOwner, Is.SameAs(exactOwner));

    }

    private static void AssertTokenRow(
        DailyCaptureEligibilityToken token,
        string sectionId,
        object exactOwner)
    {
        int matches = 0;
        foreach (OwnerSectionCensusSnapshot row in token.OwnerSections)
        {
            if (!string.Equals(row.SectionId, sectionId, StringComparison.Ordinal)) continue;
            matches++;
            Assert.That(row.SchemaVersion, Is.EqualTo(1));
            Assert.That(row.Role, Is.EqualTo(OwnerSectionRole.Required));
            Assert.That(row.OwnerInstanceIdentity, Is.SameAs(exactOwner));
            Assert.That(row.Cardinality, Is.Zero);
            Assert.That(row.Revision, Is.Zero);
        }
        Assert.That(matches, Is.EqualTo(1));
    }

    private static void AssertProviderOwner(
        SimulationRuntime runtime,
        string sectionId,
        NpcRuntime npc,
        object exactOwner)
    {
        int matches = 0;
        foreach (IOwnerSectionCensusProvider provider in runtime.NpcReceiptOwnerCensusProviders)
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (!string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal)) continue;
            matches++;
            Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(exactOwner));
            Assert.That(P12DNpcReceiptOwnerCensusProvider.IsExactCoverage(
                runtime.NpcRuntimes,
                runtime.NpcReceiptOwnerCensusProviders), Is.True);
        }
        Assert.That(matches, Is.EqualTo(1));
    }

    private static void AssertNoProvider(SimulationRuntime runtime, string sectionId)
    {
        foreach (IOwnerSectionCensusProvider provider in runtime.NpcReceiptOwnerCensusProviders)
            Assert.That(provider.GetCurrentCensus().SectionId, Is.Not.EqualTo(sectionId));
    }

    private static long ReadEpoch(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epoch, out ContinuationCensusFailure failure), Is.True, failure.ToString());
        return epoch;
    }

    private static SimulationRuntime CreateDailyRuntime(IEnumerable<NpcRuntime> npcs)
    {
        return new SimulationRuntime(
            new SimulationTime(), null, npcs,
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: new WorldId(Guid.NewGuid()));
    }

    private static SimulationRuntime CreateIdentityBoundDailyRuntime(
        out RuntimeIdentityRegistry identities)
    {
        CityRuntime[] cities =
        {
            SimulationTestFactory.CreateCity("p12d-roster-city-a", "p12d-roster-city-location-a"),
            SimulationTestFactory.CreateCity("p12d-roster-city-b", "p12d-roster-city-location-b")
        };
        identities = new RuntimeIdentityRegistry();
        foreach (CityRuntime city in cities)
            Assert.That(identities.RegisterCity(city), Is.True);

        NpcRuntime[] npcs = new NpcRuntime[10];
        for (int i = 0; i < npcs.Length; i++)
        {
            npcs[i] = CreateNpc("p12d-roster-npc-" + i);
            Assert.That(identities.RegisterNpc(npcs[i]), Is.True);
        }

        SpatialLocationRuntime[] locations =
        {
            new SpatialLocationRuntime("p12d-roster-network-location-a"),
            new SpatialLocationRuntime("p12d-roster-network-location-b")
        };
        SpatialNetworkRuntime network = new SpatialNetworkRuntime(identities);
        foreach (SpatialLocationRuntime location in locations)
            Assert.That(network.RegisterLocation(location), Is.True);
        Assert.That(network.RegisterRoute(new SpatialRouteRuntime(
            "p12d-roster-route-a", locations[0], locations[1], 1)), Is.True);
        Assert.That(network.RegisterRoute(new SpatialRouteRuntime(
            "p12d-roster-route-b", locations[1], locations[0], 1)), Is.True);

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), cities, npcs,
            economyEnabled: false,
            explorableSiteStore: new ExplorableSiteStore(),
            spatialAuthorityStore: CreateSpatialAuthority(),
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            runtimeIdentityRegistry: identities,
            spatialNetworkRuntime: network,
            requireP12RuntimeIdentitySpatialCensusOwners: true);
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
        return runtime;
    }

    private static SpatialAuthorityStore CreateSpatialAuthority()
    {
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("p12d-roster-scale", "fixture", "v1", 1m, "step"),
            new[]
            {
                new HexRecord(
                    new HexId("p12d-roster-hex"),
                    new HexCoordinate(0, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain.p12d-roster"), "v1"))
            },
            new[]
            {
                new LocationRecord(new LocationId("p12d-roster-location"),
                    new HexId("p12d-roster-hex"))
            });
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        if (!authority.TryComposeGeography(geography, out SpatialAuthorityFailure failure))
            throw new InvalidOperationException("Could not compose P12-D roster test geography: " + failure);
        return authority;
    }

    private static NpcRuntime CreateNpc(string runtimeId) =>
        new NpcRuntime(runtimeId, SimulationTestFactory.CreateNpc(runtimeId));

    private static void CommitLocalObservationReceipt(NpcRuntime npc, bool establishPresence = true)
    {
        if (establishPresence)
        {
            ItemData item = SimulationTestFactory.CreateItem("p12d-local-receipt-item", 12f);
            CityRuntime city = SimulationTestFactory.CreateCity(
                "p12d-local-receipt-city",
                "p12d-local-receipt-location",
                new MarketItemConfig { item = item, initialAmount = 3, desiredAmount = 5 });
            npc.SetCurrentPresence(city.Location, city);
        }
        NpcLocalKnowledgeDailyBoundaryStepProvider provider =
            new NpcLocalKnowledgeDailyBoundaryStepProvider(() => new[] { npc }, true);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("p12d-world", "intraday", 1L);
        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure createFailure),
            Is.True, createFailure.ToString());
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "configuration/v1", steps);
        Assert.That(provider.TryPrepareStep(manifest, steps[0],
            out IBoundaryContinuationStepCommit prepared, out TimelineFailure prepareFailure),
            Is.True, prepareFailure.ToString());
        Assert.That(prepared.TryCommit(out TimelineFailure commitFailure), Is.True,
            commitFailure.ToString());
    }

    private static void CommitMerchantReceipt(NpcRuntime npc)
    {
        NpcMerchantTradeStateRuntime owner = npc.ExistingMerchantTradeStateRuntime;
        MerchantTradePlanState expectedPlan = npc.MerchantTradePlan.CaptureOwnerState();
        NpcTravelPlanState expectedTravelPlan = npc.TravelPlan.CaptureOwnerState();
        Assert.That(owner.TryPrepareInstall(
            npc,
            "p12d-receipt-test-operation",
            "p12d-receipt-test-fingerprint",
            expectedPlan,
            expectedTravelPlan,
            expectedPlan,
            expectedTravelPlan,
            out IBoundaryContinuationStepCommit prepared), Is.True);
        Assert.That(prepared.TryCommit(out TimelineFailure failure), Is.True, failure.ToString());
    }

    private static object GetReceiptOwner(NpcRuntime npc, string ownerKind) =>
        ownerKind == "local"
            ? (object)npc.ExistingLocalKnowledgeObservationRuntime
            : npc.ExistingMerchantTradeStateRuntime;

    private static bool TryReadOwner(
        object owner,
        string ownerKind,
        out int cardinality,
        out long revision)
    {
        if (ownerKind == "local")
            return ((NpcLocalKnowledgeObservationRuntime)owner)
                .TryReadP12ReceiptCensus(out cardinality, out revision);
        return ((NpcMerchantTradeStateRuntime)owner)
            .TryReadP12ReceiptCensus(out cardinality, out revision);
    }

    private static IReadOnlyList<IOwnerSectionCensusProvider> CreateProvidersFromOwner(NpcRuntime npc) =>
        P12DNpcReceiptOwnerCensusProvider.CreateProviders(new[] { npc });

    private static object GetPrivateField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return field.GetValue(target);
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static T ReadPrivateField<T>(object target, string name) =>
        (T)GetPrivateField(target, name);
}
