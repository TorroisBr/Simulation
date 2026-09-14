using System;
using NUnit.Framework;

public sealed class SimulationRuntimeLongRunTests
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
    public void ConstructionDoesNotAdvanceTimeAndAdvanceDayAdvancesExactlyOneTick()
    {
        SimulationTime time = new SimulationTime();
        CityRuntime city = CreatePaidCity("runtime-one-day", 100f, 10, 10, 1, 1);
        SimulationRuntime runtime = new SimulationRuntime(time, new[] { city }, null);

        Assert.That(runtime.CurrentDay, Is.EqualTo(0L));
        Assert.That(time.AbsoluteDay, Is.EqualTo(0L));

        runtime.AdvanceDay();

        Assert.That(runtime.CurrentDay, Is.EqualTo(1L));
        Assert.That(time.AbsoluteDay, Is.EqualTo(1L));
    }

    [Test]
    public void AdvanceDaysZeroIsNoOpAndNegativeCountIsRejected()
    {
        SimulationTime time = new SimulationTime(4L);
        SimulationRuntime runtime = new SimulationRuntime(time, null, null);

        runtime.AdvanceDays(0);

        Assert.That(runtime.CurrentDay, Is.EqualTo(4L));
        Assert.Throws<ArgumentOutOfRangeException>(() => runtime.AdvanceDays(-1));
        Assert.That(runtime.CurrentDay, Is.EqualTo(4L));
    }

    [Test]
    public void AdvanceDaysAdvancesExactlyRequestedNumberOfDays()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null);

        runtime.AdvanceDays(17);

        Assert.That(runtime.CurrentDay, Is.EqualTo(17L));
    }

    [Test]
    public void DailyEconomyOrderIsProductionThenConsumptionThenPriceUpdate()
    {
        ItemData item = SimulationTestFactory.CreateItem("runtime-order", 10f);
        CityData cityData = SimulationTestFactory.CreateCityData(
            "runtime-order-city",
            SimulationTestFactory.CreateMarketItem(item, 0, 1));
        cityData.initialPopulation = 1000;
        cityData.marketItems[0].consumptionPer1000Population = 1f;
        cityData.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = 1 });
        CityRuntime city = new CityRuntime(
            "runtime-order-city",
            cityData,
            new SpatialLocationRuntime("runtime-order-location"));
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), new[] { city }, null);

        runtime.AdvanceDay();

        Assert.That(city.Market.GetAmount(item), Is.EqualTo(0));
        Assert.That(city.Market.GetPrice(item), Is.EqualTo(10f).Within(0.001f));
    }

    [Test]
    public void ClosedPaidEconomyRemainsValidForOneHundredDays()
    {
        CityRuntime city = CreatePaidCity("runtime-100", 1000f, 100, 100, 1, 1);
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), new[] { city }, null);
        float totalMoney = TotalMoney(city);

        runtime.AdvanceDays(100);

        Assert.That(runtime.CurrentDay, Is.EqualTo(100L));
        AssertInvariants(runtime);
        Assert.That(TotalMoney(city), Is.EqualTo(totalMoney).Within(0.01f));
    }

    [Test]
    public void ClosedPaidEconomyRemainsValidForOneThousandDaysAfterPopulationDepletion()
    {
        CityRuntime city = CreatePaidCity("runtime-1000", 50f, 100, 1, 0, 1);
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), new[] { city }, null);
        float totalMoney = TotalMoney(city);

        runtime.AdvanceDays(1000);

        Assert.That(runtime.CurrentDay, Is.EqualTo(1000L));
        Assert.That(city.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(0f).Within(0.01f));
        AssertInvariants(runtime);
        Assert.That(TotalMoney(city), Is.EqualTo(totalMoney).Within(0.01f));
    }

    [Test]
    public void MinimalEconomyRemainsValidForFiveThousandDays()
    {
        CityRuntime city = CreatePaidCity("runtime-5000", 10f, 10, 10, 1, 0);
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), new[] { city }, null);

        runtime.AdvanceDays(5000);

        Assert.That(runtime.CurrentDay, Is.EqualTo(5000L));
        AssertInvariants(runtime);
        Assert.That(city.PopulationEconomy.MoneyAccount.Balance, Is.GreaterThanOrEqualTo(0f));
    }

    private static void AssertInvariants(SimulationRuntime runtime)
    {
        Assert.That(runtime, Is.Not.Null);
        Assert.That(runtime.Cities, Is.Not.Null);
        Assert.That(runtime.NpcRuntimes, Is.Not.Null);

        foreach (CityRuntime city in runtime.Cities)
        {
            if (city == null)
            {
                continue;
            }

            SimulationInvariantValidator.ValidateCityMarketCounterparty(city);
            SimulationInvariantValidator.ValidatePopulationEconomy(city);

            foreach (MarketItemRuntime item in city.Market.Items)
            {
                Assert.That(item, Is.Not.Null);
                Assert.That(item.Amount, Is.GreaterThanOrEqualTo(0));
                Assert.That(item.Amount, Is.LessThanOrEqualTo(int.MaxValue));
                Assert.That(float.IsNaN(item.CurrentPrice), Is.False);
                Assert.That(float.IsInfinity(item.CurrentPrice), Is.False);
            }
        }

        foreach (NpcRuntime npc in runtime.NpcRuntimes)
        {
            if (npc == null)
            {
                continue;
            }

            Assert.That(npc.MoneyAccount.Balance, Is.GreaterThanOrEqualTo(0f));
            Assert.That(float.IsNaN(npc.MoneyAccount.Balance), Is.False);
            Assert.That(float.IsInfinity(npc.MoneyAccount.Balance), Is.False);
            foreach (InventoryItemRuntime item in npc.Inventory.Items)
            {
                if (item != null)
                {
                    Assert.That(item.Amount, Is.GreaterThanOrEqualTo(0));
                }
            }
        }
    }

    private static float TotalMoney(CityRuntime city)
    {
        return city.PopulationEconomy.MoneyAccount.Balance
            + city.MarketCounterparty.MoneyAccount.Balance;
    }

    private static CityRuntime CreatePaidCity(
        string id,
        float populationMoney,
        int initialStock,
        int desiredStock,
        int productionAmount,
        float consumptionPer1000)
    {
        ItemData item = SimulationTestFactory.CreateItem("item-" + id, 10f);
        CityData cityData = SimulationTestFactory.CreateCityData(
            "definition-" + id,
            SimulationTestFactory.CreateMarketItem(item, initialStock, desiredStock));
        cityData.initialPopulation = 1000;
        cityData.marketLiquidity = new MarketLiquidityConfig
        {
            liquidityMode = MarketLiquidityMode.AccountBacked,
            initialPurchasingPower = 0f
        };
        cityData.populationConsumption = new PopulationConsumptionConfig
        {
            paymentMode = ConsumptionPaymentMode.AccountBacked,
            initialPurchasingPower = populationMoney
        };
        cityData.marketItems[0].consumptionPer1000Population = consumptionPer1000;

        if (productionAmount > 0)
        {
            cityData.productionConfigs.Add(new CityProductionConfig
            {
                item = item,
                amountPerDay = productionAmount
            });
        }

        return new CityRuntime(
            "city-" + id,
            cityData,
            new SpatialLocationRuntime("location-" + id));
    }
}
