using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class FiniteSourceProductionTests
{
    [SetUp] public void SetUp() => SimulationTestFactory.CleanupDefinitions();
    [TearDown] public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void FiniteProfileRejectsAdditionalAuthoredCityBeforeRuntimeConstruction()
    {
        CityData finiteCity = SimulationTestFactory.CreateCityData("p14b-single-finite-city");
        finiteCity.materialFlowProfile = LocalMaterialFlowProfile.FiniteReserveDaily;
        CityData additionalCity = SimulationTestFactory.CreateCityData("p14b-additional-city");

        Assert.That(FiniteSourceProfileAdmission.TryValidateAuthoredCityCardinality(
            new List<CityData> { finiteCity }, out string singleCityRejection), Is.True);
        Assert.That(singleCityRejection, Is.Empty);

        Assert.That(FiniteSourceProfileAdmission.TryValidateAuthoredCityCardinality(
            new List<CityData> { finiteCity, additionalCity }, out string multipleCityRejection), Is.False);
        Assert.That(multipleCityRejection,
            Is.EqualTo("FiniteReserveProfileRequiresExactlyOneAuthoredCity"));

        // Repeating one CityData reference still produces two City instances in the authored list.
        Assert.That(FiniteSourceProfileAdmission.TryValidateAuthoredCityCardinality(
            new List<CityData> { finiteCity, finiteCity }, out string repeatedCityRejection), Is.False);
        Assert.That(repeatedCityRejection,
            Is.EqualTo("FiniteReserveProfileRequiresExactlyOneAuthoredCity"));
    }

    [Test]
    public void P14AExogenousSourceRemainsOutsideFiniteProfileCardinalityGate()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14a-exogenous-cardinality-item");
        CityData city = SimulationTestFactory.CreateCityData("p14a-exogenous-cardinality-city");
        city.materialFlowProfile = LocalMaterialFlowProfile.ExogenousDaily;
        city.productionConfigs.Add(new CityProductionConfig
        {
            item = item,
            amountPerDay = 2,
            productionSourceId = "source.p14a.exogenous",
            contentRevision = "1"
        });

        Assert.That(FiniteSourceProfileAdmission.TryValidateAuthoredCityCardinality(
            new List<CityData> { city }, out string rejection), Is.True);
        Assert.That(rejection, Is.Empty);
    }

    [Test]
    public void ProductionIsCappedByReserveAndExhaustionDoesNotChangeRevisions()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-cap-item");
        item.basePrice = 2f;
        FiniteProductionSourceStore source = CreateSource(item, 5, 3);
        MarketRuntime market = CreateMarket(item, 2, 8);
        FiniteSourceProductionService service = new FiniteSourceProductionService();

        FiniteSourceProductionResult applied = service.TryProduceDaily(
            source, market, "settlement.p14b", "store.p14b", "location.p14b", item, 1, 0, 0);

        Assert.That(applied.Status, Is.EqualTo(FiniteSourceProductionStatus.Applied), applied.RejectionReason);
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
            source, market, "settlement.p14b", "store.p14b", "location.p14b", item, 2, 1, 1);

        Assert.That(exhausted.Status, Is.EqualTo(FiniteSourceProductionStatus.Exhausted), exhausted.RejectionReason);
        Assert.That(exhausted.Quantity, Is.Zero);
        Assert.That(source.Source.Revision, Is.EqualTo(1));
        Assert.That(market.Revision, Is.EqualTo(1));
        Assert.That(market.GetAmount(item), Is.EqualTo(5));
    }

    [Test]
    public void UnityP14BAdapterMatchesSharedCoreScenarioForAppliedAndExhaustedDays()
    {
        FiniteSourceScenarioDefinition definition = FiniteSourceScenarioParityVectors.CreateDefinition(
            out FiniteSourceScenarioSnapshot coreSnapshot);
        Assert.That(FiniteSourceScenarioRuntime.TryCreate(definition, coreSnapshot,
            out FiniteSourceScenarioRuntime coreRuntime, out string rejection), Is.True, rejection);

        ItemData item = SimulationTestFactory.CreateItem(FiniteSourceScenarioParityVectors.ItemId, 2f);
        FiniteProductionSourceStore source = new FiniteProductionSourceStore(
            LocalMaterialFlowProfile.FiniteReserveDaily,
            new CityProductionConfig
            {
                item = item,
                amountPerDay = 3,
                initialReserve = 5,
                productionSourceId = FiniteSourceScenarioParityVectors.ProductionSourceId,
                contentRevision = FiniteSourceScenarioParityVectors.ContentRevision
            },
            FiniteSourceScenarioParityVectors.SettlementId,
            FiniteSourceScenarioParityVectors.MarketStoreId);
        MarketRuntime market = CreateMarket(item, 2, 8);

        foreach (long day in FiniteSourceScenarioParityVectors.AdmittedDays)
        {
            Assert.That(coreRuntime.TryPrepareAdvance(coreSnapshot, day,
                out PreparedFiniteSourceAdvance prepared, out rejection), Is.True, rejection);
            Assert.That(coreRuntime.TryApplyPrepared(coreSnapshot, prepared,
                out FiniteSourceScenarioSnapshot nextCoreSnapshot, out rejection), Is.True, rejection);
            FiniteSourceProductionResult unity = new FiniteSourceProductionService().TryProduceDaily(
                source, market, FiniteSourceScenarioParityVectors.SettlementId,
                FiniteSourceScenarioParityVectors.MarketStoreId,
                FiniteSourceScenarioParityVectors.LocationId, item, day,
                source.Source.Revision, market.Revision);

            FiniteSourceProductionStatus expectedStatus = prepared.Status == FiniteSourceOperationStatus.Applied
                ? FiniteSourceProductionStatus.Applied : FiniteSourceProductionStatus.Exhausted;
            Assert.That(unity.Status, Is.EqualTo(expectedStatus), unity.RejectionReason);
            Assert.That(unity.Quantity, Is.EqualTo(prepared.Quantity));
            Assert.That(unity.ReserveAfter, Is.EqualTo(nextCoreSnapshot.Source.RemainingReserve));
            Assert.That(source.Source.Revision, Is.EqualTo(nextCoreSnapshot.Source.Revision));
            Assert.That(market.Revision, Is.EqualTo(nextCoreSnapshot.MarketRevision));
            Assert.That(market.Items[0].Item.DefinitionId,
                Is.EqualTo(nextCoreSnapshot.MarketRows[0].ItemDefinition.DefinitionId));
            Assert.That(market.Items[0].Amount, Is.EqualTo(nextCoreSnapshot.MarketRows[0].Amount));
            Assert.That(market.Items[0].DesiredAmount, Is.EqualTo(nextCoreSnapshot.MarketRows[0].DesiredAmount));
            Assert.That(FloatBits(market.Items[0].CurrentPrice),
                Is.EqualTo(FloatBits(nextCoreSnapshot.MarketRows[0].CurrentPrice)));
            coreSnapshot = nextCoreSnapshot;
        }
    }

    [Test]
    public void IdenticalInputsProduceIdenticalFiniteResultsAndZeroReserveIsExhausted()
    {
        ItemData firstItem = SimulationTestFactory.CreateItem("p14b-deterministic-item");
        ItemData secondItem = SimulationTestFactory.CreateItem("p14b-deterministic-item");
        FiniteProductionSourceStore firstSource = CreateSource(firstItem, 3, 2);
        FiniteProductionSourceStore secondSource = CreateSource(secondItem, 3, 2);
        MarketRuntime firstMarket = CreateMarket(firstItem, 4, 10);
        MarketRuntime secondMarket = CreateMarket(secondItem, 4, 10);

        FiniteSourceProductionResult first = Produce(firstSource, firstMarket, firstItem, 7, 0, 0);
        FiniteSourceProductionResult second = Produce(secondSource, secondMarket, secondItem, 7, 0, 0);

        Assert.That(first.Status, Is.EqualTo(FiniteSourceProductionStatus.Applied));
        Assert.That(second.Status, Is.EqualTo(first.Status));
        Assert.That(second.Quantity, Is.EqualTo(first.Quantity));
        Assert.That(second.ReserveBefore, Is.EqualTo(first.ReserveBefore));
        Assert.That(second.ReserveAfter, Is.EqualTo(first.ReserveAfter));
        Assert.That(second.StockBefore, Is.EqualTo(first.StockBefore));
        Assert.That(second.StockAfter, Is.EqualTo(first.StockAfter));
        Assert.That(firstSource.Source.RemainingReserve, Is.EqualTo(secondSource.Source.RemainingReserve));
        Assert.That(firstMarket.GetAmount(firstItem), Is.EqualTo(secondMarket.GetAmount(secondItem)));

        FiniteProductionSourceStore emptySource = CreateSource(firstItem, 3, 0);
        MarketRuntime emptyMarket = CreateMarket(firstItem, 4, 10);
        FiniteSourceProductionResult exhausted = Produce(emptySource, emptyMarket, firstItem, 7, 0, 0);

        Assert.That(exhausted.Status, Is.EqualTo(FiniteSourceProductionStatus.Exhausted));
        Assert.That(exhausted.Quantity, Is.Zero);
        Assert.That(emptySource.Source.Revision, Is.Zero);
        Assert.That(emptyMarket.Revision, Is.Zero);
        Assert.That(emptyMarket.GetAmount(firstItem), Is.EqualTo(4));
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
            source, market, "settlement.other", "store.p14b", "location.p14b", item, 1, 0, 0);
        FiniteSourceProductionResult wrongCustody = service.TryProduceDaily(
            source, market, "settlement.p14b", "store.other", "location.p14b", item, 1, 0, 0);
        FiniteSourceProductionResult wrongItemResult = service.TryProduceDaily(
            source, market, "settlement.p14b", "store.p14b", "location.p14b", wrongItem, 1, 0, 0);

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
        exogenous.marketItems[0].initialAmount = 10;
        exogenous.marketItems[0].consumptionPer1000Population = 0f;
        CityRuntime exogenousCity = new CityRuntime("runtime.exogenous", exogenous, new SpatialLocationRuntime("legacy-exogenous"));
        Assert.That(exogenousCity.FiniteProductionSources, Is.Null);

        typeof(CityRuntime).GetMethod("SimulateLocalDailyMaterialFlow", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(exogenousCity, new object[] { 1L, "calendar-v1" });

        Assert.That(exogenousCity.FiniteProductionSources, Is.Null);
        Assert.That(exogenousCity.LastMaterialFlow.AppliedSourceQuantity, Is.EqualTo(2));
        Assert.That(exogenousCity.LastMaterialFlow.OpeningStock, Is.EqualTo(10));
        Assert.That(exogenousCity.LastMaterialFlow.ClosingStock, Is.EqualTo(12));

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

    [Test]
    public void DirectProductionPreservesThreeAdmissionCallsAndNotifiesAfterBothInstalls()
    {
        ItemData item = SimulationTestFactory.CreateItem("corec-direct-admission-item");
        FiniteProductionSourceStore source = CreateSource(item, 2, 4);
        MarketRuntime market = CreateMarket(item, 7, 20);
        List<string> observations = new List<string>();
        market.BindP12MutationBoundary(() =>
        {
            observations.Add("admit:" + OwnerState(source, market, item));
            return true;
        }, () => observations.Add("notify:" + OwnerState(source, market, item)));

        FiniteSourceProductionResult result = Produce(source, market, item, 1, 0, 0);

        Assert.That(result.Status, Is.EqualTo(FiniteSourceProductionStatus.Applied));
        Assert.That(observations, Is.EqualTo(new[]
        {
            "admit:reserve=4;sourceRevision=0;stock=7;marketRevision=0",
            "admit:reserve=4;sourceRevision=0;stock=7;marketRevision=0",
            "admit:reserve=4;sourceRevision=0;stock=7;marketRevision=0",
            "notify:reserve=2;sourceRevision=1;stock=9;marketRevision=1"
        }));
    }

    [Test]
    public void DirectProductionRejectsAtEveryAdmissionPositionWithoutPartialInstall()
    {
        for (int rejectedCall = 1; rejectedCall <= 3; rejectedCall++)
        {
            ItemData item = SimulationTestFactory.CreateItem("corec-direct-reject-" + rejectedCall);
            FiniteProductionSourceStore source = CreateSource(item, 2, 4);
            MarketRuntime market = CreateMarket(item, 7, 20);
            int admissionCalls = 0;
            int notifications = 0;
            market.BindP12MutationBoundary(() => ++admissionCalls != rejectedCall,
                () => notifications++);

            FiniteSourceProductionResult result = Produce(source, market, item, 1, 0, 0);

            Assert.That(result.Status, Is.EqualTo(FiniteSourceProductionStatus.Rejected), "call " + rejectedCall);
            Assert.That(result.RejectionReason, Is.EqualTo("MarketMutationRejected"), "call " + rejectedCall);
            Assert.That(admissionCalls, Is.EqualTo(rejectedCall), "call " + rejectedCall);
            Assert.That(notifications, Is.Zero, "call " + rejectedCall);
            Assert.That(source.Source.RemainingReserve, Is.EqualTo(4), "call " + rejectedCall);
            Assert.That(source.Source.Revision, Is.Zero, "call " + rejectedCall);
            Assert.That(market.GetAmount(item), Is.EqualTo(7), "call " + rejectedCall);
            Assert.That(market.Revision, Is.Zero, "call " + rejectedCall);
        }
    }

    [Test]
    public void PreparedCityProductionPreservesFourAdmissionCallsAndPostInstallNotification()
    {
        ItemData item = SimulationTestFactory.CreateItem("corec-city-admission-item");
        CityRuntime city = CreateFiniteCity("corec-city-admission", item, 4);
        List<string> observations = new List<string>();
        city.Market.BindP12MutationBoundary(() =>
        {
            observations.Add("admit:" + OwnerState(city.FiniteProductionSources, city.Market, item));
            return true;
        }, () => observations.Add("notify:" + OwnerState(city.FiniteProductionSources, city.Market, item)));
        BoundaryContinuationManifest manifest = CreateProductionManifest(city, "corec-city-admission", 1);
        BoundaryContinuationStep step = manifest.Steps[0];

        Assert.That(city.TryPrepareDailyEconomyStep(manifest, step,
            out IBoundaryContinuationStepCommit prepared, out TimelineFailure prepareFailure), Is.True,
            prepareFailure.ToString());
        Assert.That(prepared.TryCommit(out TimelineFailure commitFailure), Is.True, commitFailure.ToString());

        Assert.That(observations, Is.EqualTo(new[]
        {
            "admit:reserve=4;sourceRevision=0;stock=10;marketRevision=0",
            "admit:reserve=4;sourceRevision=0;stock=10;marketRevision=0",
            "admit:reserve=4;sourceRevision=0;stock=10;marketRevision=0",
            "admit:reserve=4;sourceRevision=0;stock=10;marketRevision=0",
            "notify:reserve=2;sourceRevision=1;stock=12;marketRevision=1"
        }));
        Assert.That(city.TryResolveDailyEconomyReceipt(manifest, step, out _, out _), Is.True);
    }

    [Test]
    public void PreparedCityProductionRejectsAtEveryAdmissionPositionWithoutPartialInstall()
    {
        for (int rejectedCall = 1; rejectedCall <= 4; rejectedCall++)
        {
            string id = "corec-city-reject-" + rejectedCall;
            ItemData item = SimulationTestFactory.CreateItem(id);
            CityRuntime city = CreateFiniteCity(id, item, 4);
            int admissionCalls = 0;
            int notifications = 0;
            city.Market.BindP12MutationBoundary(() => ++admissionCalls != rejectedCall,
                () => notifications++);
            BoundaryContinuationManifest manifest = CreateProductionManifest(city, id, 1);
            BoundaryContinuationStep step = manifest.Steps[0];

            bool preparedSuccessfully = city.TryPrepareDailyEconomyStep(manifest, step,
                out IBoundaryContinuationStepCommit prepared, out _);
            if (rejectedCall <= 2)
            {
                Assert.That(preparedSuccessfully, Is.False, "call " + rejectedCall);
                Assert.That(prepared, Is.Null, "call " + rejectedCall);
            }
            else
            {
                Assert.That(preparedSuccessfully, Is.True, "call " + rejectedCall);
                Assert.That(prepared.TryCommit(out _), Is.False, "call " + rejectedCall);
            }

            Assert.That(admissionCalls, Is.EqualTo(rejectedCall), "call " + rejectedCall);
            Assert.That(notifications, Is.Zero, "call " + rejectedCall);
            Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(4), "call " + rejectedCall);
            Assert.That(city.FiniteProductionSources.Source.Revision, Is.Zero, "call " + rejectedCall);
            Assert.That(city.Market.GetAmount(item), Is.EqualTo(10), "call " + rejectedCall);
            Assert.That(city.Market.Revision, Is.Zero, "call " + rejectedCall);
            Assert.That(city.TryResolveDailyEconomyReceipt(manifest, step, out _, out _), Is.False,
                "call " + rejectedCall);
        }
    }

    [Test]
    public void AdmissionExceptionRejectsAndNotificationExceptionDoesNotUndoInstalledOwners()
    {
        ItemData rejectedItem = SimulationTestFactory.CreateItem("corec-admission-exception-item");
        FiniteProductionSourceStore rejectedSource = CreateSource(rejectedItem, 2, 4);
        MarketRuntime rejectedMarket = CreateMarket(rejectedItem, 7, 20);
        rejectedMarket.BindP12MutationBoundary(
            () => throw new System.InvalidOperationException("admission"),
            () => Assert.Fail("Rejected mutation sent a notification."));
        FiniteSourceProductionResult rejected = Produce(rejectedSource, rejectedMarket, rejectedItem, 1, 0, 0);
        Assert.That(rejected.Status, Is.EqualTo(FiniteSourceProductionStatus.Rejected));
        Assert.That(rejected.RejectionReason, Is.EqualTo("MarketMutationRejected"));
        Assert.That(rejectedSource.Source.RemainingReserve, Is.EqualTo(4));
        Assert.That(rejectedMarket.GetAmount(rejectedItem), Is.EqualTo(7));

        ItemData committedItem = SimulationTestFactory.CreateItem("corec-notification-exception-item");
        FiniteProductionSourceStore committedSource = CreateSource(committedItem, 2, 4);
        MarketRuntime committedMarket = CreateMarket(committedItem, 7, 20);
        committedMarket.BindP12MutationBoundary(() => true,
            () => throw new System.InvalidOperationException("notification"));
        FiniteSourceProductionResult committed = Produce(committedSource, committedMarket, committedItem, 1, 0, 0);

        Assert.That(committed.Status, Is.EqualTo(FiniteSourceProductionStatus.Applied));
        Assert.That(committedSource.Source.RemainingReserve, Is.EqualTo(2));
        Assert.That(committedSource.Source.Revision, Is.EqualTo(1));
        Assert.That(committedMarket.GetAmount(committedItem), Is.EqualTo(9));
        Assert.That(committedMarket.Revision, Is.EqualTo(1));
    }

    [Test]
    public void FiniteMarketRowAdapterPreservesRowsAndUsesTheExistingTargetItemReference()
    {
        ItemData requestedItem = SimulationTestFactory.CreateItem("corec-row-target");
        requestedItem.basePrice = 2.5f;
        ItemData rowAlias = SimulationTestFactory.CreateItem("corec-row-target");
        rowAlias.basePrice = 9.25f;
        ItemData unrelatedItem = SimulationTestFactory.CreateItem("corec-row-unrelated");

        MarketItemRuntime nullItem = new MarketItemRuntime(null, 1, 1);
        float payloadNaN = System.BitConverter.ToSingle(
            System.BitConverter.GetBytes(unchecked((int)0xFFC00001)), 0);
        float negativeZero = System.BitConverter.ToSingle(
            System.BitConverter.GetBytes(int.MinValue), 0);
        SetPrivate(nullItem, "amount", -17);
        SetPrivate(nullItem, "desiredAmount", 0);
        SetPrivate(nullItem, "currentPrice", payloadNaN);

        MarketItemRuntime unrelated = new MarketItemRuntime(unrelatedItem, 3, 11);
        SetPrivate(unrelated, "currentPrice", negativeZero);
        MarketItemRuntime target = new MarketItemRuntime(rowAlias, 7, 19);
        SetPrivate(target, "currentPrice", payloadNaN);

        MarketRuntime market = new MarketRuntime();
        List<MarketItemRuntime> originalRows = new List<MarketItemRuntime> { null, nullItem, unrelated, target };
        SetPrivate(market, "items", originalRows);
        SetPrivate<object>(market, "readOnlyItems", null);

        FiniteProductionSourceStore source = CreateSource(requestedItem, 2, 4);
        FiniteSourceProductionResult result = Produce(source, market, requestedItem, 1, 0, 0);

        Assert.That(result.Status, Is.EqualTo(FiniteSourceProductionStatus.Applied));
        Assert.That(market.Items.Count, Is.EqualTo(4));
        Assert.That(market.Items[0], Is.Null);
        Assert.That(market.Items[1], Is.Not.Null);
        Assert.That(market.Items[1].Item, Is.Null);
        Assert.That(market.Items[1].Amount, Is.EqualTo(-17));
        Assert.That(market.Items[1].DesiredAmount, Is.Zero);
        Assert.That(FloatBits(market.Items[1].CurrentPrice), Is.EqualTo(FloatBits(payloadNaN)));
        Assert.That(ReferenceEquals(market.Items[2].Item, unrelatedItem), Is.True);
        Assert.That(market.Items[2].Amount, Is.EqualTo(3));
        Assert.That(market.Items[2].DesiredAmount, Is.EqualTo(11));
        Assert.That(FloatBits(market.Items[2].CurrentPrice), Is.EqualTo(FloatBits(negativeZero)));
        Assert.That(ReferenceEquals(market.Items[3].Item, rowAlias), Is.True);
        Assert.That(market.Items[3].Amount, Is.EqualTo(9));
        Assert.That(market.Items[3].DesiredAmount, Is.EqualTo(19));
        Assert.That(FloatBits(market.Items[3].CurrentPrice), Is.EqualTo(FloatBits(
            MarketPriceCalculator.CalculateCurrentPrice(rowAlias.basePrice, 19, 9))));
        Assert.That(source.Source.RemainingReserve, Is.EqualTo(2));
        Assert.That(source.Source.Revision, Is.EqualTo(1));
        Assert.That(market.Revision, Is.EqualTo(1));
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
            source, market, "settlement.p14b", "store.p14b", "location.p14b", item,
            day, sourceRevision, marketRevision);

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

    private static void SetPrivate<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Expected field " + name);
        field.SetValue(target, value);
    }

    private static int FloatBits(float value) =>
        System.BitConverter.ToInt32(System.BitConverter.GetBytes(value), 0);

    private static CityRuntime CreateFiniteCity(string id, ItemData item, int reserve)
    {
        CityData data = CreateCityData(id, item, reserve);
        data.materialFlowProfile = LocalMaterialFlowProfile.FiniteReserveDaily;
        return new CityRuntime("runtime." + id, data, new SpatialLocationRuntime("legacy." + id));
    }

    private static BoundaryContinuationManifest CreateProductionManifest(CityRuntime city, string id, long day)
    {
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world." + id, "profile." + id, day);
        Assert.That(city.TryCreateDailyEconomyStep(operation, 0, CityDailyEconomyStepKind.Production,
            out BoundaryContinuationStep step, out TimelineFailure failure), Is.True, failure.ToString());
        return new BoundaryContinuationManifest(operation, "daily-economy", "1", "config." + id,
            new List<BoundaryContinuationStep> { step });
    }

    private static string OwnerState(FiniteProductionSourceStore source, MarketRuntime market, ItemData item) =>
        "reserve=" + source.Source.RemainingReserve
        + ";sourceRevision=" + source.Source.Revision
        + ";stock=" + market.GetAmount(item)
        + ";marketRevision=" + market.Revision;
}
