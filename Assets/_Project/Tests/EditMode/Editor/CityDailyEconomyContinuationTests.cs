using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class CityDailyEconomyContinuationTests
{
    [SetUp] public void SetUp() => SimulationTestFactory.CleanupDefinitions();
    [TearDown] public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void ProductionStepReplaysWithoutApplyingStockTwice()
    {
        ItemData item = SimulationTestFactory.CreateItem("daily-production");
        CityData data = SimulationTestFactory.CreateCityData("daily-production-city");
        data.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = 4 });
        CityRuntime city = new CityRuntime("city-daily-production", data, new SpatialLocationRuntime("daily-production-location"));
        BoundaryContinuationManifest manifest = CreateManifest(city, CityDailyEconomyStepKind.Production);

        IBoundaryContinuationStepCommit first = Prepare(city, manifest);
        Assert.That(first.TryCommit(out _), Is.True);
        IBoundaryContinuationStepCommit replay = Prepare(city, manifest);
        Assert.That(replay.TryCommit(out _), Is.True);

        Assert.That(city.Market.GetAmount(item), Is.EqualTo(4));
        Assert.That(city.Market.Revision, Is.EqualTo(1));
    }

    [Test]
    public void SameOccurrenceWithChangedFrozenFingerprintIsRejected()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-daily-fingerprint", "daily-fingerprint-location");
        BoundaryContinuationManifest manifest = CreateManifest(city, CityDailyEconomyStepKind.PriceRefresh);
        Assert.That(Prepare(city, manifest).TryCommit(out _), Is.True);
        BoundaryContinuationStep step = manifest.Steps[0];
        BoundaryContinuationManifest changed = new BoundaryContinuationManifest(
            new DailyBoundaryOperation("world", "profile", 1), "daily-economy", "1", "changed-config",
            new List<BoundaryContinuationStep> { step });
        Assert.That(city.TryPrepareDailyEconomyStep(changed, step, out _, out _), Is.False);
    }

    [Test]
    public void ConsumptionPreservesDuplicateRowsAndConfiguredOrder()
    {
        ItemData item = SimulationTestFactory.CreateItem("daily-duplicate");
        CityData data = SimulationTestFactory.CreateCityData("daily-duplicate-city");
        data.initialPopulation = 1000;
        data.marketItems.Add(new MarketItemConfig { item = item, initialAmount = 10, desiredAmount = 10, consumptionPer1000Population = 2f });
        data.marketItems.Add(new MarketItemConfig { item = item, initialAmount = 10, desiredAmount = 10, consumptionPer1000Population = 3f });
        CityRuntime city = new CityRuntime("city-daily-duplicate", data, new SpatialLocationRuntime("daily-duplicate-location"));
        BoundaryContinuationManifest manifest = CreateManifest(city, CityDailyEconomyStepKind.Consumption);
        Assert.That(Prepare(city, manifest).TryCommit(out _), Is.True);
        Assert.That(city.TryResolveDailyEconomyReceipt(manifest, manifest.Steps[0], out CityDailyEconomyReceipt receipt, out _), Is.True);

        Assert.That(receipt.ConsumptionResults.Count, Is.EqualTo(2));
        Assert.That(receipt.ConsumptionResults[0].RequestedQuantity, Is.EqualTo(2));
        Assert.That(receipt.ConsumptionResults[1].RequestedQuantity, Is.EqualTo(3));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(5));
        Assert.That(city.Market.Revision, Is.EqualTo(2));
    }

    [Test]
    public void FreeConsumptionRemovesAvailableStockAndPaidConsumptionTransfersBalancesAtomically()
    {
        ItemData item = SimulationTestFactory.CreateItem("daily-paid");
        CityData freeData = SimulationTestFactory.CreateCityData("daily-free-city");
        freeData.initialPopulation = 1000;
        freeData.marketItems.Add(new MarketItemConfig { item = item, initialAmount = 1, desiredAmount = 10, consumptionPer1000Population = 3f });
        CityRuntime free = new CityRuntime("city-daily-free", freeData, new SpatialLocationRuntime("daily-free-location"));
        BoundaryContinuationManifest freeManifest = CreateManifest(free, CityDailyEconomyStepKind.Consumption);
        IBoundaryContinuationStepCommit freeCommit = Prepare(free, freeManifest);
        Assert.That(freeCommit.TryCommit(out _), Is.True);
        Assert.That(free.Market.GetAmount(item), Is.Zero);

        CityData paidData = SimulationTestFactory.CreateCityData("daily-paid-city");
        paidData.initialPopulation = 1000;
        paidData.marketLiquidity = new MarketLiquidityConfig { liquidityMode = MarketLiquidityMode.AccountBacked, initialPurchasingPower = 0f };
        paidData.populationConsumption = new PopulationConsumptionConfig { paymentMode = ConsumptionPaymentMode.AccountBacked, initialPurchasingPower = 50f };
        paidData.marketItems.Add(new MarketItemConfig { item = item, initialAmount = 5, desiredAmount = 5, consumptionPer1000Population = 2f });
        CityRuntime paid = new CityRuntime("city-daily-paid", paidData, new SpatialLocationRuntime("daily-paid-location"));
        BoundaryContinuationManifest paidManifest = CreateManifest(paid, CityDailyEconomyStepKind.Consumption);
        Assert.That(Prepare(paid, paidManifest).TryCommit(out _), Is.True);
        Assert.That(paid.Market.GetAmount(item), Is.EqualTo(3));
        Assert.That(paid.PopulationEconomy.MoneyAccount.Balance, Is.LessThan(50f));
        Assert.That(paid.MarketCounterparty.MoneyAccount.Balance, Is.GreaterThan(0f));
        Assert.That(paid.PopulationEconomy.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(paid.MarketCounterparty.MoneyAccount.Revision, Is.EqualTo(1));
    }

    [Test]
    public void ConfigurationAndStalePreparedMarketRevisionAreRejectedBeforeInstall()
    {
        ItemData item = SimulationTestFactory.CreateItem("daily-stale");
        CityData data = SimulationTestFactory.CreateCityData("daily-stale-city");
        data.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = 2 });
        CityRuntime city = new CityRuntime("city-daily-stale", data, new SpatialLocationRuntime("daily-stale-location"));
        BoundaryContinuationManifest manifest = CreateManifest(city, CityDailyEconomyStepKind.Production);
        IBoundaryContinuationStepCommit prepared = Prepare(city, manifest);
        Assert.That(city.Market.AddStock(item, 1), Is.EqualTo(1));
        Assert.That(prepared.TryCommit(out _), Is.False);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(1));

        BoundaryContinuationManifest staleConfig = CreateManifest(city, CityDailyEconomyStepKind.Production);
        data.productionConfigs[0].amountPerDay++;
        Assert.That(city.TryPrepareDailyEconomyStep(staleConfig, staleConfig.Steps[0], out _, out _), Is.False);
        BoundaryContinuationManifest preparedConfigManifest = CreateManifest(city, CityDailyEconomyStepKind.Production);
        IBoundaryContinuationStepCommit configPrepared = Prepare(city, preparedConfigManifest);
        data.productionConfigs[0].amountPerDay++;
        Assert.That(configPrepared.TryCommit(out _), Is.False);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(1));

        CityData paidData = SimulationTestFactory.CreateCityData("daily-stale-account-city");
        paidData.initialPopulation = 1000;
        paidData.marketLiquidity = new MarketLiquidityConfig { liquidityMode = MarketLiquidityMode.AccountBacked, initialPurchasingPower = 0f };
        paidData.populationConsumption = new PopulationConsumptionConfig { paymentMode = ConsumptionPaymentMode.AccountBacked, initialPurchasingPower = 5f };
        paidData.marketItems.Add(new MarketItemConfig { item = item, initialAmount = 5, desiredAmount = 5, consumptionPer1000Population = 1f });
        CityRuntime paidCity = new CityRuntime("city-daily-stale-account", paidData, new SpatialLocationRuntime("daily-stale-account-location"));
        BoundaryContinuationManifest paidManifest = CreateManifest(paidCity, CityDailyEconomyStepKind.Consumption);
        IBoundaryContinuationStepCommit accountPrepared = Prepare(paidCity, paidManifest);
        Assert.That(paidCity.PopulationEconomy.MoneyAccount.TryCredit(1f), Is.True);
        Assert.That(accountPrepared.TryCommit(out _), Is.False);
    }

    [Test]
    public void MarketRevisionOverflowRejectsPreparationWithoutMutation()
    {
        ItemData item = SimulationTestFactory.CreateItem("daily-overflow");
        CityData data = SimulationTestFactory.CreateCityData("daily-overflow-city");
        data.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = 1 });
        CityRuntime city = new CityRuntime("city-daily-overflow", data, new SpatialLocationRuntime("daily-overflow-location"));
        typeof(MarketRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(city.Market, long.MaxValue);
        BoundaryContinuationManifest manifest = CreateManifest(city, CityDailyEconomyStepKind.Production);

        Assert.That(city.TryPrepareDailyEconomyStep(manifest, manifest.Steps[0], out _, out _), Is.False);
        Assert.That(city.Market.GetAmount(item), Is.Zero);
        Assert.That(city.Market.Revision, Is.EqualTo(long.MaxValue));
    }

    private static BoundaryContinuationManifest CreateManifest(CityRuntime city, CityDailyEconomyStepKind kind)
    {
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 1);
        Assert.That(city.TryCreateDailyEconomyStep(operation, 0, kind, out BoundaryContinuationStep step, out _), Is.True);
        return new BoundaryContinuationManifest(operation, "daily-economy", "1", "config",
            new List<BoundaryContinuationStep> { step });
    }

    private static IBoundaryContinuationStepCommit Prepare(CityRuntime city, BoundaryContinuationManifest manifest)
    {
        Assert.That(city.TryPrepareDailyEconomyStep(manifest, manifest.Steps[0], out IBoundaryContinuationStepCommit prepared, out _), Is.True);
        return prepared;
    }

}
