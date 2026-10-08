using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class P12DCityRootOwnerSnapshotTests
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
    public void CaptureAndStage_PreserveExactCityMarketPopulationAndOrderedMembers()
    {
        ItemData firstItem = SimulationTestFactory.CreateItem("city-root-first", 10f);
        ItemData secondItem = SimulationTestFactory.CreateItem("city-root-second", 4f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "city-root-runtime",
            "city-root-location",
            SimulationTestFactory.CreateMarketItem(firstItem, 20, 80),
            SimulationTestFactory.CreateMarketItem(secondItem, 3, 12));
        NpcRuntime firstNpc = new NpcRuntime("city-root-npc-b", SimulationTestFactory.CreateNpc("city-root-npc-b"), city, 0f);
        NpcRuntime secondNpc = new NpcRuntime("city-root-npc-a", SimulationTestFactory.CreateNpc("city-root-npc-a"), city, 0f);

        SettlementPopulationRuntime population = city.Population;
        SettlementPopulationTransition transition = new SettlementPopulationTransition(
            city.RuntimeId,
            population.Revision,
            population.CurrentPopulation,
            new PopulationChangeSet(2, 0, 0, 0),
            2L,
            population.CurrentPopulation + 2);
        Assert.That(population.TryApplyTransitionWithReceipt(
            "city-root-operation-1",
            "city-root-fingerprint-1",
            transition,
            out bool newlyApplied,
            out PopulationTransitionFailure failure), Is.True);
        Assert.That(newlyApplied, Is.True);
        Assert.That(failure, Is.EqualTo(PopulationTransitionFailure.None));
        Assert.That(city.Market.AddStock(firstItem, 5), Is.EqualTo(5));

        DailyCaptureEligibilityToken token = CreateToken(city, CreateOwnerSections(city));
        object captureStamp = new object();
        Assert.That(P12DCityRootOwnerSnapshot.TryCapture(
            city,
            token,
            captureStamp,
            token.OwnerSections,
            out P12DCityRootOwnerSnapshot snapshot,
            out P12DCityRootOwnerSnapshotFailure captureFailure), Is.True);
        Assert.That(captureFailure, Is.EqualTo(P12DCityRootOwnerSnapshotFailure.None));

        Assert.That(P12DCityRootOwnerSnapshot.TryCapture(
            city,
            token,
            captureStamp,
            token.OwnerSections,
            out P12DCityRootOwnerSnapshot repeatedSnapshot,
            out _), Is.True);
        Assert.That(repeatedSnapshot.MarketItems.Select(row => row.ItemDefinitionId),
            Is.EqualTo(snapshot.MarketItems.Select(row => row.ItemDefinitionId)));
        Assert.That(repeatedSnapshot.MarketItems.Select(row => row.Amount),
            Is.EqualTo(snapshot.MarketItems.Select(row => row.Amount)));
        Assert.That(repeatedSnapshot.MarketItems.Select(row => row.CurrentPrice),
            Is.EqualTo(snapshot.MarketItems.Select(row => row.CurrentPrice)));
        Assert.That(repeatedSnapshot.PopulationReceipts.Select(receipt => receipt.Identity),
            Is.EqualTo(snapshot.PopulationReceipts.Select(receipt => receipt.Identity)));

        Assert.That(snapshot.ImportantNpcRuntimeIds, Is.EqualTo(new[] { firstNpc.RuntimeId, secondNpc.RuntimeId }));
        Assert.That(snapshot.ImportantNpcRevision, Is.EqualTo(city.ImportantNpcRevision));
        Assert.That(snapshot.MarketItems.Select(row => row.ItemDefinitionId),
            Is.EqualTo(new[] { firstItem.DefinitionId, secondItem.DefinitionId }));
        Assert.That(snapshot.MarketItems[0].Amount, Is.EqualTo(25));
        Assert.That(snapshot.MarketItems[0].DesiredAmount, Is.EqualTo(80));
        Assert.That(snapshot.MarketItems[0].CurrentPrice, Is.EqualTo(city.Market.Items[0].CurrentPrice));
        Assert.That(snapshot.MarketRevision, Is.EqualTo(city.Market.Revision));
        Assert.That(snapshot.MarketCounterpartyRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(snapshot.MarketLiquidityMode, Is.EqualTo(MarketLiquidityMode.Open));
        Assert.That(snapshot.MarketCounterpartyHasAccount, Is.False);
        Assert.That(snapshot.PopulationEconomicRuntimeId, Is.EqualTo("population-" + city.RuntimeId));
        Assert.That(snapshot.PopulationPaymentMode, Is.EqualTo(ConsumptionPaymentMode.Free));
        Assert.That(snapshot.PopulationEconomyHasAccount, Is.False);
        Assert.That(snapshot.PopulationRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(snapshot.CurrentPopulation, Is.EqualTo(1002));
        Assert.That(snapshot.PopulationRevision, Is.EqualTo(1L));
        Assert.That(snapshot.PopulationReceiptRevision, Is.EqualTo(1L));
        Assert.That(snapshot.PopulationReceipts, Has.Count.EqualTo(1));
        Assert.That(snapshot.PopulationReceipts[0].Identity, Is.EqualTo("city-root-operation-1"));
        Assert.That(snapshot.PopulationReceipts[0].Fingerprint, Is.EqualTo("city-root-fingerprint-1"));
        Assert.That(snapshot.PopulationReceipts[0].Transition, Is.EqualTo(transition));

        int capturedAmount = snapshot.MarketItems[0].Amount;
        long capturedMarketRevision = snapshot.MarketRevision;
        Assert.That(city.Market.AddStock(firstItem, 3), Is.EqualTo(3));
        Assert.That(snapshot.MarketItems[0].Amount, Is.EqualTo(capturedAmount));
        Assert.That(snapshot.MarketRevision, Is.EqualTo(capturedMarketRevision));

        Assert.That(snapshot.TryStage(
            new[] { city.CityData },
            new[] { firstItem, secondItem },
            new[] { city.Location },
            out CityRuntime stagedCity,
            out P12DCityMembershipLinker linker,
            out P12DCityRootOwnerSnapshotFailure stageFailure), Is.True);
        Assert.That(stageFailure, Is.EqualTo(P12DCityRootOwnerSnapshotFailure.None));
        Assert.That(stagedCity.RuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(stagedCity.CityData, Is.SameAs(city.CityData));
        Assert.That(stagedCity.Location, Is.SameAs(city.Location));
        Assert.That(stagedCity.MarketCounterparty, Is.SameAs(stagedCity.Market.Counterparty));
        Assert.That(stagedCity.Market.Counterparty.CounterpartyRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(stagedCity.Market.Revision, Is.EqualTo(capturedMarketRevision));
        Assert.That(stagedCity.Market.Items.Select(row => row.Item.DefinitionId),
            Is.EqualTo(new[] { firstItem.DefinitionId, secondItem.DefinitionId }));
        Assert.That(stagedCity.Market.Items[0].Amount, Is.EqualTo(capturedAmount));
        Assert.That(stagedCity.Market.Items[0].CurrentPrice, Is.EqualTo(snapshot.MarketItems[0].CurrentPrice));
        Assert.That(stagedCity.Population.CurrentPopulation, Is.EqualTo(snapshot.CurrentPopulation));
        Assert.That(stagedCity.Population.Revision, Is.EqualTo(snapshot.PopulationRevision));
        stagedCity.Population.GetOperationReceiptCensus(out int stagedReceiptCount, out long stagedReceiptRevision);
        Assert.That(stagedReceiptCount, Is.EqualTo(1));
        Assert.That(stagedReceiptRevision, Is.EqualTo(snapshot.PopulationReceiptRevision));
        Assert.That(stagedCity.ImportantNpcRevision, Is.EqualTo(snapshot.ImportantNpcRevision));
        Assert.That(stagedCity.ImportantNpcs, Is.Empty);
        Assert.That(linker.IsFilled, Is.False);
        Assert.That(linker.TryFillOnce(Array.Empty<NpcRuntime>()), Is.False,
            "the City owner linker must require the exact captured order before filling its private list");
        NpcRuntime stagedFirstNpc = CreateStagedPresenceNpc(firstNpc.RuntimeId, stagedCity);
        NpcRuntime stagedSecondNpc = CreateStagedPresenceNpc(secondNpc.RuntimeId, stagedCity);
        Assert.That(linker.TryFillOnce(new[] { stagedSecondNpc, stagedFirstNpc }), Is.False,
            "the linker must reject a different order before changing the private list");
        Assert.That(stagedCity.ImportantNpcs, Is.Empty);
        Assert.That(linker.TryFillOnce(new[] { stagedFirstNpc, stagedSecondNpc }), Is.True);
        Assert.That(stagedCity.ImportantNpcs, Is.EqualTo(new[] { stagedFirstNpc, stagedSecondNpc }));
        Assert.That(linker.TryFillOnce(new[] { stagedFirstNpc, stagedSecondNpc }), Is.False,
            "the private ordered member list can be filled once only");
        Assert.That(stagedCity.ImportantNpcRevision, Is.EqualTo(snapshot.ImportantNpcRevision));
    }

    [Test]
    public void Capture_RejectsMissingDuplicateWrongOwnerCardinalityAndRevisionSections()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-root-vector", "city-root-vector-location");
        List<OwnerSectionCensusSnapshot> valid = CreateOwnerSections(city);
        OwnerSectionCensusSnapshot marketSection = valid.Single(section =>
            section.SectionId.StartsWith(CityMarketCensusProvider.SectionIdPrefix, StringComparison.Ordinal));

        List<OwnerSectionCensusSnapshot> missing = new List<OwnerSectionCensusSnapshot>(valid);
        missing.Remove(marketSection);
        AssertCaptureRejected(city, CreateToken(city, missing), P12DCityRootOwnerSnapshotFailure.InvalidOwnerSectionVector);

        List<OwnerSectionCensusSnapshot> duplicate = new List<OwnerSectionCensusSnapshot>(valid);
        duplicate.Add(marketSection);
        AssertCaptureRejected(city, CreateToken(city, duplicate), P12DCityRootOwnerSnapshotFailure.InvalidOwnerSectionVector);

        List<OwnerSectionCensusSnapshot> wrongOwner = new List<OwnerSectionCensusSnapshot>(valid);
        wrongOwner[wrongOwner.IndexOf(marketSection)] = new OwnerSectionCensusSnapshot(
            marketSection.SectionId,
            marketSection.SchemaVersion,
            marketSection.Role,
            new object(),
            marketSection.Cardinality,
            marketSection.Revision);
        AssertCaptureRejected(city, CreateToken(city, wrongOwner), P12DCityRootOwnerSnapshotFailure.InvalidOwnerSectionVector);

        List<OwnerSectionCensusSnapshot> wrongCardinality = new List<OwnerSectionCensusSnapshot>(valid);
        wrongCardinality[wrongCardinality.IndexOf(marketSection)] = new OwnerSectionCensusSnapshot(
            marketSection.SectionId,
            marketSection.SchemaVersion,
            marketSection.Role,
            marketSection.OwnerInstanceIdentity,
            marketSection.Cardinality + 1,
            marketSection.Revision);
        AssertCaptureRejected(city, CreateToken(city, wrongCardinality), P12DCityRootOwnerSnapshotFailure.InvalidOwnerSectionVector);

        List<OwnerSectionCensusSnapshot> wrongRevision = new List<OwnerSectionCensusSnapshot>(valid);
        wrongRevision[wrongRevision.IndexOf(marketSection)] = new OwnerSectionCensusSnapshot(
            marketSection.SectionId,
            marketSection.SchemaVersion,
            marketSection.Role,
            marketSection.OwnerInstanceIdentity,
            marketSection.Cardinality,
            marketSection.Revision + 1L);
        AssertCaptureRejected(city, CreateToken(city, wrongRevision), P12DCityRootOwnerSnapshotFailure.InvalidOwnerSectionVector);

        List<OwnerSectionCensusSnapshot> wrongSchema = new List<OwnerSectionCensusSnapshot>(valid);
        wrongSchema[wrongSchema.IndexOf(marketSection)] = new OwnerSectionCensusSnapshot(
            marketSection.SectionId,
            marketSection.SchemaVersion + 1,
            marketSection.Role,
            marketSection.OwnerInstanceIdentity,
            marketSection.Cardinality,
            marketSection.Revision);
        AssertCaptureRejected(city, CreateToken(city, wrongSchema), P12DCityRootOwnerSnapshotFailure.InvalidOwnerSectionVector);
    }

    [Test]
    public void Capture_RequiresCompletedDailyBoundaryAndTheExactSharedSectionVector()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-root-token", "city-root-token-location");
        DailyCaptureEligibilityToken token = CreateToken(
            city,
            CreateOwnerSections(city),
            completedCoreSequence: 0L);

        AssertCaptureRejected(city, token, P12DCityRootOwnerSnapshotFailure.UnsupportedProfile);

        token = CreateToken(city, CreateOwnerSections(city));
        IReadOnlyList<OwnerSectionCensusSnapshot> copiedVector = new ReadOnlyCollection<OwnerSectionCensusSnapshot>(
            token.OwnerSections.ToList());
        Assert.That(P12DCityRootOwnerSnapshot.TryCapture(
            city,
            token,
            new object(),
            copiedVector,
            out _,
            out P12DCityRootOwnerSnapshotFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(P12DCityRootOwnerSnapshotFailure.InvalidCaptureContext));
    }

    [Test]
    public void Capture_RejectsAccountBackedCityOwners()
    {
        CityData accountMarketDefinition = SimulationTestFactory.CreateCityData(
            "city-root-account-market-definition");
        accountMarketDefinition.marketLiquidity.liquidityMode = MarketLiquidityMode.AccountBacked;
        CityRuntime accountMarketCity = new CityRuntime(
            "city-root-account-market",
            accountMarketDefinition,
            new SpatialLocationRuntime("city-root-account-market-location"));
        AssertCaptureRejected(
            accountMarketCity,
            CreateToken(accountMarketCity, CreateOwnerSections(accountMarketCity)),
            P12DCityRootOwnerSnapshotFailure.UnsupportedCityAccountState);

        CityRuntime accountPopulationCity = SimulationTestFactory.CreateCity(
            "city-root-account-population",
            "city-root-account-population-location");
        PopulationEconomyRuntime accountPopulationEconomy = new PopulationEconomyRuntime(
            accountPopulationCity.RuntimeId,
            new PopulationConsumptionConfig { paymentMode = ConsumptionPaymentMode.AccountBacked, initialPurchasingPower = 10f },
            MarketLiquidityMode.AccountBacked);
        typeof(CityRuntime).GetField("populationEconomy", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(accountPopulationCity, accountPopulationEconomy);
        AssertCaptureRejected(
            accountPopulationCity,
            CreateToken(accountPopulationCity, CreateOwnerSections(accountPopulationCity)),
            P12DCityRootOwnerSnapshotFailure.UnsupportedCityAccountState);
    }

    [Test]
    public void Capture_RejectsP18ReceiptsAndP14MaterialFlowState()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-root-p18", "city-root-p18-location");
        typeof(CityRuntime).GetField("dailyEconomyReceiptRevision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(city, 1L);
        AssertCaptureRejected(
            city,
            CreateToken(city, CreateOwnerSections(city)),
            P12DCityRootOwnerSnapshotFailure.ExcludedCityStatePresent);

        city = SimulationTestFactory.CreateCity("city-root-p18-populated", "city-root-p18-populated-location");
        Dictionary<string, CityDailyEconomyReceipt> receipts = new Dictionary<string, CityDailyEconomyReceipt>(StringComparer.Ordinal)
        {
            ["city-root-p18-receipt"] = new CityDailyEconomyReceipt(
                "city-root-p18-receipt",
                "city-root-p18-fingerprint",
                Array.Empty<CityProductionResult>(),
                Array.Empty<CityConsumptionResult>())
        };
        typeof(CityRuntime).GetField("dailyEconomyReceipts", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(city, receipts);
        AssertCaptureRejected(
            city,
            CreateToken(city, CreateOwnerSections(city)),
            P12DCityRootOwnerSnapshotFailure.ExcludedCityStatePresent);

        city = SimulationTestFactory.CreateCity("city-root-p14", "city-root-p14-location");
        city.CityData.materialFlowLocationId = "city-root-p14-anchor";
        AssertCaptureRejected(
            city,
            CreateToken(city, CreateOwnerSections(city)),
            P12DCityRootOwnerSnapshotFailure.ExcludedCityStatePresent);
    }

    [Test]
    public void EmptyStagedMembershipLinker_FillsOnceWithoutAdvancingCityRevision()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-root-empty", "city-root-empty-location");
        DailyCaptureEligibilityToken token = CreateToken(city, CreateOwnerSections(city));
        Assert.That(P12DCityRootOwnerSnapshot.TryCapture(
            city,
            token,
            new object(),
            token.OwnerSections,
            out P12DCityRootOwnerSnapshot snapshot,
            out _), Is.True);
        Assert.That(snapshot.TryStage(
            new[] { city.CityData },
            Array.Empty<ItemData>(),
            new[] { city.Location },
            out CityRuntime stagedCity,
            out P12DCityMembershipLinker linker,
            out _), Is.True);

        long revision = stagedCity.ImportantNpcRevision;
        Assert.That(linker.TryFillOnce(Array.Empty<NpcRuntime>()), Is.True);
        Assert.That(linker.IsFilled, Is.True);
        Assert.That(linker.TryFillOnce(Array.Empty<NpcRuntime>()), Is.False);
        Assert.That(stagedCity.ImportantNpcs, Is.Empty);
        Assert.That(stagedCity.ImportantNpcRevision, Is.EqualTo(revision));
    }

    [Test]
    public void CaptureAndStage_PreservePopulationReceiptsAfterRollbackPruning()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-root-rollback", "city-root-rollback-location");
        SettlementPopulationRuntime population = city.Population;
        SettlementPopulationTransition first = new SettlementPopulationTransition(
            city.RuntimeId,
            0L,
            1000,
            new PopulationChangeSet(5, 0, 0, 0),
            5L,
            1005);
        Assert.That(population.TryApplyTransitionWithReceipt(
            "city-root-retained-receipt",
            "city-root-retained-fingerprint",
            first,
            out bool firstApplied,
            out _), Is.True);
        Assert.That(firstApplied, Is.True);

        SettlementPopulationTransition second = new SettlementPopulationTransition(
            city.RuntimeId,
            1L,
            1005,
            new PopulationChangeSet(3, 0, 0, 0),
            3L,
            1008);
        Assert.That(population.TryApplyTransitionWithReceipt(
            "city-root-pruned-receipt",
            "city-root-pruned-fingerprint",
            second,
            out bool secondApplied,
            out _), Is.True);
        Assert.That(secondApplied, Is.True);

        population.RestoreSnapshot(1005, 1L);
        DailyCaptureEligibilityToken token = CreateToken(city, CreateOwnerSections(city));
        Assert.That(P12DCityRootOwnerSnapshot.TryCapture(
            city,
            token,
            new object(),
            token.OwnerSections,
            out P12DCityRootOwnerSnapshot snapshot,
            out _), Is.True);
        Assert.That(snapshot.CurrentPopulation, Is.EqualTo(1005));
        Assert.That(snapshot.PopulationRevision, Is.EqualTo(1L));
        Assert.That(snapshot.PopulationReceiptRevision, Is.EqualTo(3L));
        Assert.That(snapshot.PopulationReceipts, Has.Count.EqualTo(1));
        Assert.That(snapshot.PopulationReceipts[0].Identity, Is.EqualTo("city-root-retained-receipt"));

        Assert.That(snapshot.TryStage(
            new[] { city.CityData },
            Array.Empty<ItemData>(),
            new[] { city.Location },
            out CityRuntime stagedCity,
            out _,
            out _), Is.True);
        Assert.That(stagedCity.Population.CurrentPopulation, Is.EqualTo(1005));
        Assert.That(stagedCity.Population.Revision, Is.EqualTo(1L));
        stagedCity.Population.GetOperationReceiptCensus(out int stagedReceiptCount, out long stagedReceiptRevision);
        Assert.That(stagedReceiptCount, Is.EqualTo(1));
        Assert.That(stagedReceiptRevision, Is.EqualTo(3L));
    }

    private static void AssertCaptureRejected(
        CityRuntime city,
        DailyCaptureEligibilityToken token,
        P12DCityRootOwnerSnapshotFailure expectedFailure)
    {
        Assert.That(P12DCityRootOwnerSnapshot.TryCapture(
            city,
            token,
            new object(),
            token.OwnerSections,
            out P12DCityRootOwnerSnapshot snapshot,
            out P12DCityRootOwnerSnapshotFailure failure), Is.False);
        Assert.That(snapshot, Is.Null);
        Assert.That(failure, Is.EqualTo(expectedFailure));
    }

    private static List<OwnerSectionCensusSnapshot> CreateOwnerSections(CityRuntime city)
    {
        Assert.That(city.TryCopyOwnerSnapshotMembership(
            out IReadOnlyList<string> memberIds,
            out long membershipRevision), Is.True);
        Assert.That(city.Population.TryCaptureOperationReceiptSnapshot(
            out IReadOnlyList<PopulationOperationReceiptSnapshot> receipts,
            out long receiptRevision), Is.True);

        string encodedRuntimeId = city.RuntimeId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ":" + city.RuntimeId;
        return new List<OwnerSectionCensusSnapshot>
        {
            new OwnerSectionCensusSnapshot(
                CityNpcPresenceCensusProvider.SectionIdFor(city.RuntimeId),
                CityNpcPresenceCensusProvider.SchemaVersion,
                OwnerSectionRole.Required,
                city,
                memberIds.Count,
                membershipRevision),
            new OwnerSectionCensusSnapshot(
                CityMarketCensusProvider.SectionIdPrefix + encodedRuntimeId,
                CityMarketCensusProvider.SchemaVersion,
                OwnerSectionRole.Required,
                city.Market,
                city.Market.Items.Count,
                city.Market.Revision),
            new OwnerSectionCensusSnapshot(
                SettlementPopulationCensusProvider.AggregateSectionPrefix + encodedRuntimeId,
                SettlementPopulationCensusProvider.SchemaVersion,
                OwnerSectionRole.Required,
                city.Population,
                1,
                city.Population.Revision),
            new OwnerSectionCensusSnapshot(
                SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix + encodedRuntimeId,
                SettlementPopulationCensusProvider.SchemaVersion,
                OwnerSectionRole.Required,
                city.Population,
                receipts.Count,
                receiptRevision)
        };
    }

    private static DailyCaptureEligibilityToken CreateToken(
        CityRuntime city,
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        long completedCoreSequence = 1L)
    {
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            null,
            null,
            null,
            null,
            null);
        return new DailyCaptureEligibilityToken(
            new object(),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            configuration,
            new SimulationCalendar(CalendarDefinition.CreateDefault()),
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()),
            0L,
            completedCoreSequence,
            0L,
            sections);
    }

    private static NpcRuntime CreateStagedPresenceNpc(string runtimeId, CityRuntime stagedCity)
    {
        NpcRuntime npc = new NpcRuntime(runtimeId, SimulationTestFactory.CreateNpc("definition-" + runtimeId));
        typeof(NpcRuntime).GetField("currentCity", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(npc, stagedCity);
        typeof(NpcRuntime).GetField("currentLocation", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(npc, stagedCity.Location);
        return npc;
    }
}
