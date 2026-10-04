using System.Reflection;
using NUnit.Framework;

public sealed class FiniteSourceProductionTests
{
    [SetUp] public void SetUp() => SimulationTestFactory.CleanupDefinitions();
    [TearDown] public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void ProductionIsCappedByReserveAndExhaustionDoesNotChangeRevisions()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-cap-item");
        item.basePrice = 2f;
        FiniteProductionSourceStore source = CreateSource(item, 5, 3);
        MarketRuntime market = CreateMarket(item, 2, 8);
        FiniteSourceProductionService service = new FiniteSourceProductionService();

        FiniteSourceProductionResult applied = service.TryProduceDaily(
            source, market, "settlement.p14b", "store.p14b", item, 1, 0, 0);

        Assert.That(applied.Status, Is.EqualTo(FiniteSourceProductionStatus.Applied));
        Assert.That(applied.Quantity, Is.EqualTo(3));
        Assert.That(applied.ReserveBefore, Is.EqualTo(3));
        Assert.That(applied.ReserveAfter, Is.Zero);
        Assert.That(applied.StockBefore, Is.EqualTo(2));
        Assert.That(applied.StockAfter, Is.EqualTo(5));
        Assert.That(source.Source.Revision, Is.EqualTo(1));
        Assert.That(market.Revision, Is.EqualTo(1));
        Assert.That(market.GetAmount(item), Is.EqualTo(5));
        Assert.That(market.GetPrice(item), Is.EqualTo(3.2f).Within(0.0001f));

        FiniteSourceProductionResult exhausted = service.TryProduceDaily(
            source, market, "settlement.p14b", "store.p14b", item, 2, 1, 1);

        Assert.That(exhausted.Status, Is.EqualTo(FiniteSourceProductionStatus.Exhausted));
        Assert.That(exhausted.Quantity, Is.Zero);
        Assert.That(source.Source.Revision, Is.EqualTo(1));
        Assert.That(market.Revision, Is.EqualTo(1));
        Assert.That(market.GetAmount(item), Is.EqualTo(5));
    }

    [Test]
    public void StockOverflowRejectsWithoutChangingReserveMarketOrRevision()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-stock-overflow-item");
        FiniteProductionSourceStore source = CreateSource(item, 5, 5);
        MarketRuntime market = CreateMarket(item, int.MaxValue - 2, 100);

        FiniteSourceProductionResult result = Produce(source, market, item, 1, 0, 0);

        Assert.That(result.Status, Is.EqualTo(FiniteSourceProductionStatus.Rejected));
        Assert.That(result.RejectionReason, Is.EqualTo("MarketStockOverflow"));
        Assert.That(source.Source.RemainingReserve, Is.EqualTo(5));
        Assert.That(source.Source.Revision, Is.Zero);
        Assert.That(market.GetAmount(item), Is.EqualTo(int.MaxValue - 2));
        Assert.That(market.Revision, Is.Zero);
    }

    [Test]
    public void DuplicateMarketRowsRejectWithoutPartialCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-duplicate-item");
        FiniteProductionSourceStore source = CreateSource(item, 2, 4);
        MarketRuntime market = new MarketRuntime(new System.Collections.Generic.List<MarketItemConfig>
        {
            SimulationTestFactory.CreateMarketItem(item, 7, 20),
            SimulationTestFactory.CreateMarketItem(item, 9, 20)
        });

        FiniteSourceProductionResult result = Produce(source, market, item, 1, 0, 0);

        Assert.That(result.Status, Is.EqualTo(FiniteSourceProductionStatus.Rejected));
        Assert.That(result.RejectionReason, Is.EqualTo("MarketRowCardinality"));
        Assert.That(source.Source.RemainingReserve, Is.EqualTo(4));
        Assert.That(source.Source.Revision, Is.Zero);
        Assert.That(market.Items[0].Amount, Is.EqualTo(7));
        Assert.That(market.Items[1].Amount, Is.EqualTo(9));
        Assert.That(market.Revision, Is.Zero);
    }

    [Test]
    public void StaleSourceOrMarketRevisionRejectsWithUnchangedOwners()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-stale-item");
        FiniteProductionSourceStore source = CreateSource(item, 2, 4);
        MarketRuntime market = CreateMarket(item, 7, 20);

        FiniteSourceProductionResult staleSource = Produce(source, market, item, 1, 1, 0);
        FiniteSourceProductionResult staleMarket = Produce(source, market, item, 1, 0, 1);

        Assert.That(staleSource.RejectionReason, Is.EqualTo("StaleSourceRevision"));
        Assert.That(staleMarket.RejectionReason, Is.EqualTo("StaleMarketRevision"));
        Assert.That(source.Source.RemainingReserve, Is.EqualTo(4));
        Assert.That(source.Source.Revision, Is.Zero);
        Assert.That(market.GetAmount(item), Is.EqualTo(7));
        Assert.That(market.Revision, Is.Zero);
    }

    [Test]
    public void TitleCustodyOrItemIdentityMismatchRejectsWithoutMutation()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-identity-item");
        ItemData wrongItem = SimulationTestFactory.CreateItem("p14b-wrong-item");
        FiniteProductionSourceStore source = CreateSource(item, 2, 4);
        MarketRuntime market = CreateMarket(item, 7, 20);
        FiniteSourceProductionService service = new FiniteSourceProductionService();

        FiniteSourceProductionResult wrongTitle = service.TryProduceDaily(
            source, market, "settlement.other", "store.p14b", item, 1, 0, 0);
        FiniteSourceProductionResult wrongCustody = service.TryProduceDaily(
            source, market, "settlement.p14b", "store.other", item, 1, 0, 0);
        FiniteSourceProductionResult wrongItemResult = service.TryProduceDaily(
            source, market, "settlement.p14b", "store.p14b", wrongItem, 1, 0, 0);

        Assert.That(wrongTitle.RejectionReason, Is.EqualTo("IdentityMismatch"));
        Assert.That(wrongCustody.RejectionReason, Is.EqualTo("IdentityMismatch"));
        Assert.That(wrongItemResult.RejectionReason, Is.EqualTo("IdentityMismatch"));
        Assert.That(source.Source.RemainingReserve, Is.EqualTo(4));
        Assert.That(source.Source.Revision, Is.Zero);
        Assert.That(market.GetAmount(item), Is.EqualTo(7));
        Assert.That(market.Revision, Is.Zero);
    }

    [Test]
    public void MarketAdmissionRejectsBeforeReserveDebit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-guard-item");
        FiniteProductionSourceStore source = CreateSource(item, 2, 4);
        MarketRuntime market = CreateMarket(item, 7, 20);
        market.BindP12MutationBoundary(() => false, () => Assert.Fail("Rejected mutation notified P12."));

        FiniteSourceProductionResult result = Produce(source, market, item, 1, 0, 0);

        Assert.That(result.Status, Is.EqualTo(FiniteSourceProductionStatus.Rejected));
        Assert.That(result.RejectionReason, Is.EqualTo("MarketMutationRejected"));
        Assert.That(source.Source.RemainingReserve, Is.EqualTo(4));
        Assert.That(source.Source.Revision, Is.Zero);
        Assert.That(market.GetAmount(item), Is.EqualTo(7));
        Assert.That(market.Revision, Is.Zero);
    }

    [Test]
    public void OwnerNotificationObservesBothInstalledRootsAndSameDayCannotRepeat()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-notification-item");
        FiniteProductionSourceStore source = CreateSource(item, 2, 4);
        MarketRuntime market = CreateMarket(item, 7, 20);
        int observedReserve = -1;
        int observedStock = -1;
        market.BindP12MutationBoundary(() => true, () =>
        {
            observedReserve = source.Source.RemainingReserve;
            observedStock = market.GetAmount(item);
        });

        FiniteSourceProductionResult applied = Produce(source, market, item, 8, 0, 0);
        FiniteSourceProductionResult duplicate = Produce(source, market, item, 8, 1, 1);

        Assert.That(applied.Status, Is.EqualTo(FiniteSourceProductionStatus.Applied));
        Assert.That(observedReserve, Is.EqualTo(2));
        Assert.That(observedStock, Is.EqualTo(9));
        Assert.That(duplicate.RejectionReason, Is.EqualTo("BoundaryAlreadyProcessed"));
        Assert.That(source.Source.RemainingReserve, Is.EqualTo(2));
        Assert.That(market.GetAmount(item), Is.EqualTo(9));
        Assert.That(source.Source.Revision, Is.EqualTo(1));
        Assert.That(market.Revision, Is.EqualTo(1));
    }

    [Test]
    public void RevisionExhaustionRejectsWithoutChangingEitherOwner()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-revision-item");
        FiniteProductionSourceStore source = CreateSource(item, 2, 4);
        MarketRuntime market = CreateMarket(item, 7, 20);
        SetPrivateLong(source.Source, "revision", long.MaxValue);

        FiniteSourceProductionResult sourceExhausted = Produce(source, market, item, 1, long.MaxValue, 0);
        SetPrivateLong(source.Source, "revision", 0);
        SetPrivateLong(market, "revision", long.MaxValue);
        FiniteSourceProductionResult marketExhausted = Produce(source, market, item, 1, 0, long.MaxValue);

        Assert.That(sourceExhausted.RejectionReason, Is.EqualTo("SourceRevisionExhausted"));
        Assert.That(marketExhausted.RejectionReason, Is.EqualTo("MarketRevisionExhausted"));
        Assert.That(source.Source.RemainingReserve, Is.EqualTo(4));
        Assert.That(market.GetAmount(item), Is.EqualTo(7));
        Assert.That(market.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void CityRuntimeCreatesFiniteOwnerOnlyForExplicitFiniteProfile()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-profile-item");
        CityData exogenous = CreateCityData("p14b-exogenous", item, 0);
        CityRuntime exogenousCity = new CityRuntime("runtime.exogenous", exogenous, new SpatialLocationRuntime("legacy-exogenous"));
        Assert.That(exogenousCity.FiniteProductionSources, Is.Null);

        CityData finite = CreateCityData("p14b-finite", item, 5);
        finite.materialFlowProfile = LocalMaterialFlowProfile.FiniteReserveDaily;
        CityRuntime finiteCity = new CityRuntime("runtime.finite", finite, new SpatialLocationRuntime("legacy-finite"));
        Assert.That(finiteCity.FiniteProductionSources, Is.Not.Null);
        Assert.That(finiteCity.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(5));
    }

    [Test]
    public void FiniteCityFlowDebitsReserveByAppliedQuantityBeforeFreeConsumption()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-city-flow-item");
        CityData data = CreateCityData("p14b-city-flow", item, 3);
        data.materialFlowProfile = LocalMaterialFlowProfile.FiniteReserveDaily;
        data.initialPopulation = 1000;
        data.marketItems[0].initialAmount = 2;
        data.marketItems[0].consumptionPer1000Population = 4f;
        CityRuntime city = new CityRuntime("runtime.p14b-city-flow", data, new SpatialLocationRuntime("legacy-p14b-city-flow"));

        typeof(CityRuntime).GetMethod("SimulateLocalDailyMaterialFlow", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(city, new object[] { 1L, "calendar-v1" });

        Assert.That(city.LastMaterialFlow.OpeningStock, Is.EqualTo(2));
        Assert.That(city.LastMaterialFlow.AppliedSourceQuantity, Is.EqualTo(2));
        Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(1));
        Assert.That(city.LastMaterialFlow.ActualFreeConsumption, Is.EqualTo(4));
        Assert.That(city.LastMaterialFlow.ClosingStock, Is.EqualTo(0));
        Assert.That(city.LastMaterialFlow.OpeningStock + city.LastMaterialFlow.AppliedSourceQuantity
            - city.LastMaterialFlow.ActualFreeConsumption, Is.EqualTo(city.LastMaterialFlow.ClosingStock));
        Assert.That(3 - city.FiniteProductionSources.Source.RemainingReserve,
            Is.EqualTo(city.LastMaterialFlow.AppliedSourceQuantity));
    }

    private static FiniteProductionSourceStore CreateSource(ItemData item, int dailyLimit, int reserve)
    {
        return new FiniteProductionSourceStore(LocalMaterialFlowProfile.FiniteReserveDaily, new CityProductionConfig
        {
            item = item,
            amountPerDay = dailyLimit,
            initialReserve = reserve,
            productionSourceId = "source.p14b",
            contentRevision = "content-v1"
        }, "settlement.p14b", "store.p14b");
    }

    private static MarketRuntime CreateMarket(ItemData item, int stock, int desired) =>
        new MarketRuntime(new System.Collections.Generic.List<MarketItemConfig>
        {
            SimulationTestFactory.CreateMarketItem(item, stock, desired)
        });

    private static FiniteSourceProductionResult Produce(
        FiniteProductionSourceStore source,
        MarketRuntime market,
        ItemData item,
        long day,
        long sourceRevision,
        long marketRevision) => new FiniteSourceProductionService().TryProduceDaily(
            source, market, "settlement.p14b", "store.p14b", item, day, sourceRevision, marketRevision);

    private static CityData CreateCityData(string id, ItemData item, int reserve)
    {
        CityData data = SimulationTestFactory.CreateCityData(id, SimulationTestFactory.CreateMarketItem(item, 10, 20));
        data.settlementSemanticId = "settlement." + id;
        data.marketStoreSemanticId = "store." + id;
        data.materialFlowLocationId = "location." + id;
        data.productionConfigs.Add(new CityProductionConfig
        {
            item = item,
            amountPerDay = 2,
            initialReserve = reserve,
            productionSourceId = "source." + id,
            contentRevision = "content-v1"
        });
        return data;
    }

    private static void SetPrivateLong(object target, string name, long value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Expected field " + name);
        field.SetValue(target, value);
    }
}
