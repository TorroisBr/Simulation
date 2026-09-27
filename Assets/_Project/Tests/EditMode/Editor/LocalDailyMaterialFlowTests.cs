using NUnit.Framework;
using System.Reflection;

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

    private static void Simulate(CityRuntime city, long day)
    {
        typeof(CityRuntime).GetMethod("SimulateLocalDailyMaterialFlow", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(city, new object[] { day, "test-calendar-v1" });
    }
}
