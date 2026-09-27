using NUnit.Framework;
using System.Reflection;
using System;

public sealed class LocalDailyMaterialFlowTests
{
    [SetUp] public void SetUp() => SimulationTestFactory.CleanupDefinitions();
    [TearDown] public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void DailyFlowRecordsStableTitleCustodyAndClosingBalance()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14-item");
        CityData data = SimulationTestFactory.CreateCityData("p14-definition", SimulationTestFactory.CreateMarketItem(item, 2, 20));
        data.initialPopulation = 1000;
        data.settlementSemanticId = "settlement.north";
        data.marketStoreSemanticId = "store.north";
        data.materialFlowLocationId = "location.north";
        data.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = 5, productionSourceId = "source.harvest", contentRevision = "rev-a" });
        data.marketItems[0].consumptionPer1000Population = 4f;
        CityRuntime city = new CityRuntime("runtime.north", data, new SpatialLocationRuntime("location-runtime"));

        Simulate(city, 12);
        LocalDailyMaterialFlowResult flow = city.LastMaterialFlow;

        Assert.That(flow.TitleOwnerSemanticId, Is.EqualTo("settlement.north"));
        Assert.That(flow.StockCustodianSemanticId, Is.EqualTo("store.north"));
        Assert.That(flow.LocationId, Is.EqualTo("location.north"));
        Assert.That(flow.AbsoluteDay, Is.EqualTo(12));
        Assert.That(flow.EconomyEnabled, Is.True);
        Assert.That(flow.CalendarVersion, Is.EqualTo("test-calendar-v1"));
        Assert.That(flow.OpeningStock, Is.EqualTo(2));
        Assert.That(flow.AppliedSourceQuantity, Is.EqualTo(5));
        Assert.That(flow.RequestedFreeConsumption, Is.EqualTo(4));
        Assert.That(flow.ActualFreeConsumption, Is.EqualTo(4));
        Assert.That(flow.ClosingStock, Is.EqualTo(3));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(flow.ClosingStock));
        string canonical = WorldStateCanonicalWriter.Write(WorldStateSnapshotBuilder.BuildSnapshot(
            new WorldStateSnapshotContext(cities: new[] { city })));
        Assert.That(canonical, Does.Contain("CITY_MATERIAL_FLOW|settlement.north|source.harvest|store.north"));
    }

    [Test]
    public void CompleteSourceAdditionRejectsOverflowWithoutPartialMutation()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14-overflow-item");
        CityData data = SimulationTestFactory.CreateCityData("p14-overflow", SimulationTestFactory.CreateMarketItem(item, int.MaxValue - 2, int.MaxValue));
        data.settlementSemanticId = "settlement.overflow";
        data.marketStoreSemanticId = "store.overflow";
        data.materialFlowLocationId = "location.overflow";
        data.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = 5, productionSourceId = "source.overflow", contentRevision = "rev-a" });
        data.marketItems[0].consumptionPer1000Population = 0f;
        CityRuntime city = new CityRuntime("runtime.overflow", data, new SpatialLocationRuntime("location-runtime-overflow"));

        Simulate(city, 3);

        Assert.That(city.LastMaterialFlow.AppliedSourceQuantity, Is.Zero);
        Assert.That(city.LastMaterialFlow.SourceRejectionReason, Is.EqualTo("AggregateStockOverflow"));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(int.MaxValue - 2));
        Assert.That(city.LastMaterialFlow.ClosingStock, Is.EqualTo(city.LastMaterialFlow.OpeningStock));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RuntimeRejectsExtraOrDuplicateMarketItemRows(bool duplicateSourceItem)
    {
        ItemData sourceItem = SimulationTestFactory.CreateItem("p14-cardinality-source");
        ItemData extraItem = duplicateSourceItem ? sourceItem : SimulationTestFactory.CreateItem("p14-cardinality-extra");
        CityData data = ProfileData("p14-cardinality", sourceItem);
        data.marketItems.Add(SimulationTestFactory.CreateMarketItem(extraItem));
        CityRuntime city = new CityRuntime("runtime-cardinality", data, new SpatialLocationRuntime("legacy-cardinality"));
        SpatialAuthorityStore authority = CreateSpatialAuthority("p14-cardinality-location");
        LegacySpatialAnchorBindingStore anchors = BindCity(authority, city, "p14-cardinality-location");

        Assert.Throws<LocalDailyMaterialFlowRejectedException>(() => CreateRuntime(city, authority, anchors, true));
    }

    [Test]
    public void RuntimeRejectsMissingAndMismatchedP8Anchor()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14-anchor-item");
        CityRuntime missingAnchorCity = new CityRuntime("runtime-anchor-missing", ProfileData("p14-anchor-missing", item), new SpatialLocationRuntime("legacy-anchor-missing"));
        SpatialAuthorityStore authority = CreateSpatialAuthority("p14-anchor-missing-location");
        Assert.Throws<LocalDailyMaterialFlowRejectedException>(() => CreateRuntime(missingAnchorCity, authority, null, true));

        CityRuntime mismatchedCity = new CityRuntime("runtime-anchor-mismatch", ProfileData("p14-anchor-mismatch", item), new SpatialLocationRuntime("legacy-anchor-mismatch"));
        SpatialAuthorityStore mismatchAuthority = CreateSpatialAuthority("p14-anchor-actual");
        LegacySpatialAnchorBindingStore mismatchAnchors = BindCity(mismatchAuthority, mismatchedCity, "p14-anchor-actual");
        Assert.Throws<LocalDailyMaterialFlowRejectedException>(() => CreateRuntime(mismatchedCity, mismatchAuthority, mismatchAnchors, true));
    }

    [Test]
    public void RuntimeRejectsMultipleP14SettlementProfiles()
    {
        ItemData firstItem = SimulationTestFactory.CreateItem("p14-duplicate-first");
        ItemData secondItem = SimulationTestFactory.CreateItem("p14-duplicate-second");
        CityRuntime first = new CityRuntime("runtime-profile-first", ProfileData("p14-profile-first", firstItem), new SpatialLocationRuntime("legacy-profile-first"));
        CityRuntime second = new CityRuntime("runtime-profile-second", ProfileData("p14-profile-second", secondItem), new SpatialLocationRuntime("legacy-profile-second"));
        SpatialAuthorityStore authority = CreateSpatialAuthority("p14-profile-location-a", "p14-profile-location-b");
        LegacySpatialAnchorBindingStore anchors = new LegacySpatialAnchorBindingStore(authority);
        Assert.That(anchors.TryBindCity(first.RuntimeId, new LocationId("p14-profile-location-a"), out _), Is.True);
        Assert.That(anchors.TryBindCity(second.RuntimeId, new LocationId("p14-profile-location-b"), out _), Is.True);

        Assert.Throws<LocalDailyMaterialFlowRejectedException>(() => new SimulationRuntime(
            new SimulationTime(0L), new[] { first, second }, null, economyEnabled: true,
            spatialAuthorityStore: authority, legacySpatialAnchorBindingStore: anchors));
    }

    [Test]
    public void DisabledEconomyLeavesProfileStockAndReceiptUnchanged()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14-disabled-item");
        CityRuntime city = new CityRuntime("runtime-disabled", ProfileData("p14-disabled", item), new SpatialLocationRuntime("legacy-disabled"));
        SpatialAuthorityStore authority = CreateSpatialAuthority("p14-disabled-location");
        LegacySpatialAnchorBindingStore anchors = BindCity(authority, city, "p14-disabled-location");
        SimulationRuntime runtime = CreateRuntime(city, authority, anchors, false);
        int opening = city.Market.GetAmount(item);

        runtime.AdvanceDay();

        Assert.That(city.Market.GetAmount(item), Is.EqualTo(opening));
        Assert.That(city.LastMaterialFlow, Is.Null);
    }

    [Test]
    public void DiffReportsReceiptChangeEvenWhenMarketStockIsEqual()
    {
        LocalDailyMaterialFlowResult beforeFlow = Flow(applied: 5, actualConsumption: 0, closing: 9);
        LocalDailyMaterialFlowResult afterFlow = Flow(applied: 6, actualConsumption: 1, closing: 9);
        WorldStateSnapshot before = Snapshot(beforeFlow);
        WorldStateSnapshot after = Snapshot(afterFlow);

        WorldStateDiff diff = WorldStateDiff.Compare(before, after);

        Assert.That(diff.Differences, Has.Some.Matches<WorldStateDifference>(entry =>
            entry.Section == "LocalDailyMaterialFlow" && entry.Field == "AppliedSourceQuantity"
            && entry.BeforeValue == "5" && entry.AfterValue == "6"));
        Assert.That(diff.Differences, Has.None.Matches<WorldStateDifference>(entry => entry.Section == "CityStock"));
    }

    private static CityData ProfileData(string id, ItemData item)
    {
        CityData data = SimulationTestFactory.CreateCityData(id, SimulationTestFactory.CreateMarketItem(item, 10, 20));
        data.settlementSemanticId = "settlement." + id;
        data.marketStoreSemanticId = "store." + id;
        data.materialFlowLocationId = id + "-location";
        data.initialPopulation = 1000;
        data.marketItems[0].consumptionPer1000Population = 2f;
        data.productionConfigs.Add(new CityProductionConfig
        {
            item = item,
            amountPerDay = 3,
            productionSourceId = "source." + id,
            contentRevision = "content-v1"
        });
        return data;
    }

    private static SpatialAuthorityStore CreateSpatialAuthority(params string[] locationIds)
    {
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("p14-test-hex")), out _), Is.True);
        foreach (string locationId in locationIds)
            Assert.That(authority.TryRegisterLocation(new LocationRecord(new LocationId(locationId), new HexId("p14-test-hex")), out _), Is.True);
        return authority;
    }

    private static LegacySpatialAnchorBindingStore BindCity(SpatialAuthorityStore authority, CityRuntime city, string locationId)
    {
        LegacySpatialAnchorBindingStore anchors = new LegacySpatialAnchorBindingStore(authority);
        Assert.That(anchors.TryBindCity(city.RuntimeId, new LocationId(locationId), out _), Is.True);
        return anchors;
    }

    private static SimulationRuntime CreateRuntime(CityRuntime city, SpatialAuthorityStore authority,
        LegacySpatialAnchorBindingStore anchors, bool economyEnabled) => new SimulationRuntime(
            new SimulationTime(0L), new[] { city }, null, economyEnabled: economyEnabled,
            spatialAuthorityStore: authority, legacySpatialAnchorBindingStore: anchors);

    private static WorldStateSnapshot Snapshot(LocalDailyMaterialFlowResult flow) => new WorldStateSnapshot(0L,
        cities: new[] { new WorldStateCitySnapshot("runtime-diff", "definition-diff", "location-diff", 1000,
            "counterparty-diff", MarketLiquidityMode.Open, 0f, Array.Empty<string>(),
            new[] { new WorldStateMarketStackSnapshot("item-diff", 9, 20, 1f) },
            lastMaterialFlow: flow) });

    private static LocalDailyMaterialFlowResult Flow(int applied, int actualConsumption, int closing) => new LocalDailyMaterialFlowResult(
        "settlement.diff", "source.diff", "store.diff", "location.diff", "item-diff", "content-v1",
        "Economy.Enabled=true;PaymentMode=Free", "simulation-calendar", "calendar-v1", true, 1000, 2f,
        1L, 4, applied, applied, string.Empty, actualConsumption == 0 ? 0 : 2, actualConsumption, closing);

    private static void Simulate(CityRuntime city, long day)
    {
        typeof(CityRuntime).GetMethod("SimulateLocalDailyMaterialFlow", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(city, new object[] { day, "test-calendar-v1" });
    }
}
