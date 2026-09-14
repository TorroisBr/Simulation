using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class PopulationConsumptionTests
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
    public void DefaultPopulationConsumptionRemainsFreeWithoutAnAccount()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("population-default", "location-population-default");

        Assert.That(city.CityData.PopulationConsumption.paymentMode, Is.EqualTo(ConsumptionPaymentMode.Free));
        Assert.That(city.PopulationEconomy.PaymentMode, Is.EqualTo(ConsumptionPaymentMode.Free));
        Assert.That(city.PopulationEconomy.MoneyAccount, Is.Null);
        SimulationInvariantValidator.ValidatePopulationEconomy(city);
    }

    [Test]
    public void AccountBackedPopulationHasStableDistinctEconomicIdentityAndAccount()
    {
        CityData cityData = SimulationTestFactory.CreateCityData("population-account");
        cityData.marketLiquidity = new MarketLiquidityConfig
        {
            liquidityMode = MarketLiquidityMode.AccountBacked,
            initialPurchasingPower = 20f
        };
        cityData.populationConsumption = new PopulationConsumptionConfig
        {
            paymentMode = ConsumptionPaymentMode.AccountBacked,
            initialPurchasingPower = 75f
        };

        CityRuntime first = new CityRuntime(
            "city-population-account",
            cityData,
            new SpatialLocationRuntime("location-population-account"));
        CityRuntime second = new CityRuntime(
            "city-population-account",
            cityData,
            new SpatialLocationRuntime("location-population-account-2"));

        Assert.That(first.PopulationEconomy.PaymentMode, Is.EqualTo(ConsumptionPaymentMode.AccountBacked));
        Assert.That(first.PopulationEconomy.MoneyAccount, Is.Not.Null);
        Assert.That(first.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(75f));
        Assert.That(first.PopulationEconomy.PopulationEconomicRuntimeId, Is.EqualTo("population-city-population-account"));
        Assert.That(first.PopulationEconomy.PopulationEconomicRuntimeId, Is.Not.EqualTo(first.RuntimeId));
        Assert.That(second.PopulationEconomy.PopulationEconomicRuntimeId, Is.EqualTo(first.PopulationEconomy.PopulationEconomicRuntimeId));
    }

    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void InvalidInitialPopulationPurchasingPowerIsRejected(float invalidPower)
    {
        CityData cityData = SimulationTestFactory.CreateCityData("population-invalid");
        cityData.populationConsumption = new PopulationConsumptionConfig
        {
            paymentMode = ConsumptionPaymentMode.Free,
            initialPurchasingPower = invalidPower
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CityRuntime("city-population-invalid", cityData, new SpatialLocationRuntime("location-population-invalid")));
    }

    [Test]
    public void PaidPopulationConsumptionWithOpenMarketIsRejected()
    {
        CityData cityData = SimulationTestFactory.CreateCityData("population-open-incompatible");
        cityData.populationConsumption = new PopulationConsumptionConfig
        {
            paymentMode = ConsumptionPaymentMode.AccountBacked,
            initialPurchasingPower = 10f
        };

        Assert.Throws<InvalidOperationException>(() =>
            new CityRuntime(
                "city-population-open-incompatible",
                cityData,
                new SpatialLocationRuntime("location-population-open-incompatible")));
    }

    [Test]
    public void ZeroPopulationPurchasingPowerIsValid()
    {
        CityData cityData = SimulationTestFactory.CreateCityData("population-zero");
        cityData.marketLiquidity = new MarketLiquidityConfig
        {
            liquidityMode = MarketLiquidityMode.AccountBacked,
            initialPurchasingPower = 0f
        };
        cityData.populationConsumption = new PopulationConsumptionConfig
        {
            paymentMode = ConsumptionPaymentMode.AccountBacked,
            initialPurchasingPower = 0f
        };

        CityRuntime city = new CityRuntime(
            "city-population-zero",
            cityData,
            new SpatialLocationRuntime("location-population-zero"));

        Assert.That(city.PopulationEconomy.MoneyAccount, Is.Not.Null);
        Assert.That(city.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(0f));
    }

    [Test]
    public void FreeConsumptionRemovesStockWithoutMovingMoney()
    {
        ItemData item = SimulationTestFactory.CreateItem("population-free-item", 10f);
        CityData cityData = SimulationTestFactory.CreateCityData(
            "population-free",
            SimulationTestFactory.CreateMarketItem(item, 4, 4));
        cityData.initialPopulation = 1000;
        cityData.marketItems[0].consumptionPer1000Population = 2f;
        CityRuntime city = new CityRuntime(
            "city-population-free",
            cityData,
            new SpatialLocationRuntime("location-population-free"));

        IReadOnlyList<CityConsumptionResult> results = city.SimulateConsumptionDay();

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].PaymentMode, Is.EqualTo(ConsumptionPaymentMode.Free));
        Assert.That(results[0].RequestedQuantity, Is.EqualTo(2));
        Assert.That(results[0].ConsumedQuantity, Is.EqualTo(2));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(2));
        Assert.That(city.PopulationEconomy.MoneyAccount, Is.Null);
        Assert.That(city.MarketCounterparty.MoneyAccount, Is.Null);
    }

    [Test]
    public void PaidConsumptionTransfersMoneyAndConsumesSettlementStock()
    {
        CityRuntime city = CreatePaidCity("paid-basic", 25f, 0f, 10, 10);
        ItemData item = city.CityData.marketItems[0].item;
        city.CityData.marketItems[0].consumptionPer1000Population = 2f;

        IReadOnlyList<CityConsumptionResult> results = city.SimulateConsumptionDay();
        CityConsumptionResult result = results[0];

        Assert.That(result.PaymentMode, Is.EqualTo(ConsumptionPaymentMode.AccountBacked));
        Assert.That(result.SettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(result.PopulationRuntimeId, Is.EqualTo(city.PopulationEconomy.PopulationEconomicRuntimeId));
        Assert.That(result.ItemDefinitionId, Is.EqualTo(item.DefinitionId));
        Assert.That(result.RequestedQuantity, Is.EqualTo(2));
        Assert.That(result.ConsumedQuantity, Is.EqualTo(2));
        Assert.That(result.UnitPrice, Is.EqualTo(10f).Within(0.001f));
        Assert.That(result.TotalPaid, Is.EqualTo(20f).Within(0.001f));
        Assert.That(city.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(5f).Within(0.001f));
        Assert.That(city.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(20f).Within(0.001f));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(8));
    }

    [Test]
    public void PaidConsumptionReceiptSeparatesPopulationMoneyAndItemConsumerFlow()
    {
        CityRuntime city = CreatePaidCity("paid-receipt", 50f, 0f, 5, 5);
        ItemData item = city.CityData.marketItems[0].item;
        EconomyTransactionResult receipt = new EconomyTransactionService().TryExecutePopulationConsumption(
            city.PopulationEconomy,
            city.Market,
            item,
            2);

        Assert.That(receipt.Success, Is.True);
        Assert.That(receipt.TransactionType, Is.EqualTo(EconomyTransactionType.PopulationConsumption));
        Assert.That(receipt.MoneyEffect, Is.EqualTo(MoneyEffect.Transfer));
        Assert.That(receipt.SourceRuntimeId, Is.EqualTo(city.PopulationEconomy.PopulationEconomicRuntimeId));
        Assert.That(receipt.DestinationRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(receipt.ItemSourceRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(receipt.ItemDestinationRuntimeId, Is.EqualTo(city.PopulationEconomy.PopulationEconomicRuntimeId));
        Assert.That(receipt.RequestedQuantity, Is.EqualTo(2));
        Assert.That(receipt.Quantity, Is.EqualTo(2));
        Assert.That(receipt.UnitPrice, Is.EqualTo(10f).Within(0.001f));
        Assert.That(receipt.TotalPrice, Is.EqualTo(20f).Within(0.001f));
    }

    [Test]
    public void PaidConsumptionAllowsPartialQuantityWhenPopulationFundsAreLimited()
    {
        CityRuntime city = CreatePaidCity("paid-partial", 15f, 0f, 10, 10);
        ItemData item = city.CityData.marketItems[0].item;
        city.CityData.marketItems[0].consumptionPer1000Population = 3f;

        CityConsumptionResult result = city.SimulateConsumptionDay()[0];

        Assert.That(result.RequestedQuantity, Is.EqualTo(3));
        Assert.That(result.ConsumedQuantity, Is.EqualTo(1));
        Assert.That(result.TotalPaid, Is.EqualTo(10f).Within(0.001f));
        Assert.That(city.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(5f).Within(0.001f));
        Assert.That(city.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(10f).Within(0.001f));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(9));
    }

    [Test]
    public void FundsBelowOneUnitCauseZeroMutation()
    {
        CityRuntime city = CreatePaidCity("paid-zero-funds", 9f, 0f, 10, 10);
        ItemData item = city.CityData.marketItems[0].item;
        city.CityData.marketItems[0].consumptionPer1000Population = 3f;

        CityConsumptionResult result = city.SimulateConsumptionDay()[0];

        Assert.That(result.ConsumedQuantity, Is.EqualTo(0));
        Assert.That(result.TotalPaid, Is.EqualTo(0f));
        Assert.That(city.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(9f));
        Assert.That(city.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(0f));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(10));
    }

    [Test]
    public void StockShortageLimitsPaidConsumption()
    {
        CityRuntime city = CreatePaidCity("paid-stock-shortage", 100f, 0f, 1, 1);
        ItemData item = city.CityData.marketItems[0].item;
        city.CityData.marketItems[0].consumptionPer1000Population = 3f;

        CityConsumptionResult result = city.SimulateConsumptionDay()[0];

        Assert.That(result.RequestedQuantity, Is.EqualTo(3));
        Assert.That(result.ConsumedQuantity, Is.EqualTo(1));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(0));
        Assert.That(city.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(90f).Within(0.001f));
        Assert.That(city.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(10f).Within(0.001f));
    }

    [Test]
    public void PaidConsumptionConservesPopulationAndSettlementMoney()
    {
        CityRuntime city = CreatePaidCity("paid-conservation", 100f, 25f, 20, 20);
        city.CityData.marketItems[0].consumptionPer1000Population = 3f;
        float totalBefore = city.PopulationEconomy.MoneyAccount.Balance
            + city.MarketCounterparty.MoneyAccount.Balance;

        city.SimulateConsumptionDay();

        float totalAfter = city.PopulationEconomy.MoneyAccount.Balance
            + city.MarketCounterparty.MoneyAccount.Balance;
        Assert.That(totalAfter, Is.EqualTo(totalBefore).Within(0.001f));
    }

    [Test]
    public void PaidConsumptionResultIsASnapshotAndPopulationMoneyDoesNotRefill()
    {
        CityRuntime city = CreatePaidCity("paid-snapshot", 10f, 0f, 10, 10);
        city.CityData.marketItems[0].consumptionPer1000Population = 1f;
        CityConsumptionResult first = city.SimulateConsumptionDay()[0];

        city.SimulateConsumptionDay();

        Assert.That(first.ConsumedQuantity, Is.EqualTo(1));
        Assert.That(first.TotalPaid, Is.EqualTo(10f).Within(0.001f));
        Assert.That(city.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(0f).Within(0.001f));
        Assert.That(city.Market.GetAmount(city.CityData.marketItems[0].item), Is.EqualTo(9));
    }

    [Test]
    public void ProductionDoesNotMovePopulationMoney()
    {
        ItemData item = SimulationTestFactory.CreateItem("population-production", 10f);
        CityData cityData = SimulationTestFactory.CreateCityData("population-production-city");
        cityData.marketLiquidity = new MarketLiquidityConfig
        {
            liquidityMode = MarketLiquidityMode.AccountBacked,
            initialPurchasingPower = 0f
        };
        cityData.populationConsumption = new PopulationConsumptionConfig
        {
            paymentMode = ConsumptionPaymentMode.AccountBacked,
            initialPurchasingPower = 40f
        };
        cityData.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = 5 });
        CityRuntime city = new CityRuntime(
            "city-population-production",
            cityData,
            new SpatialLocationRuntime("location-population-production"));

        city.SimulateProductionDay();

        Assert.That(city.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(40f));
        Assert.That(city.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(0f));
    }

    [Test]
    public void PopulationRevenueFundsLaterSettlementPurchaseFromNpc()
    {
        ItemData item = SimulationTestFactory.CreateItem("population-loop", 10f);
        CityRuntime city = CreatePaidCity("paid-loop", 100f, 0f, 100, 100, item);
        city.CityData.marketItems[0].consumptionPer1000Population = 2f;
        NpcRuntime seller = new NpcRuntime(
            "population-loop-seller",
            SimulationTestFactory.CreateNpc("population-loop-seller"),
            city,
            0f);
        seller.Inventory.AddItem(item, 1, 10f);
        float totalBefore = city.PopulationEconomy.MoneyAccount.Balance
            + city.MarketCounterparty.MoneyAccount.Balance
            + seller.MoneyAccount.Balance;

        CityConsumptionResult consumption = city.SimulateConsumptionDay()[0];
        EconomyTransactionResult sale = new EconomyTransactionService().TryExecuteMarketSale(seller, city.Market, item, 1);

        Assert.That(consumption.ConsumedQuantity, Is.EqualTo(2));
        Assert.That(sale.Success, Is.True);
        Assert.That(city.MarketCounterparty.MoneyAccount.Balance, Is.GreaterThanOrEqualTo(0f));
        Assert.That(city.MarketCounterparty.MoneyAccount.Balance, Is.LessThan(consumption.TotalPaid));
        Assert.That(seller.MoneyAccount.Balance, Is.EqualTo(sale.TotalPrice).Within(0.001f));
        Assert.That(
            city.MarketCounterparty.MoneyAccount.Balance,
            Is.EqualTo(consumption.TotalPaid - sale.TotalPrice).Within(0.001f));
        Assert.That(
            city.PopulationEconomy.MoneyAccount.Balance
            + city.MarketCounterparty.MoneyAccount.Balance
            + seller.MoneyAccount.Balance,
            Is.EqualTo(totalBefore).Within(0.001f));
    }

    private static CityRuntime CreatePaidCity(
        string id,
        float populationMoney,
        float settlementMoney,
        int initialStock,
        int desiredStock,
        ItemData item = null)
    {
        item = item ?? SimulationTestFactory.CreateItem("item-" + id, 10f);
        CityData cityData = SimulationTestFactory.CreateCityData(
            "definition-" + id,
            SimulationTestFactory.CreateMarketItem(item, initialStock, desiredStock));
        cityData.initialPopulation = 1000;
        cityData.marketLiquidity = new MarketLiquidityConfig
        {
            liquidityMode = MarketLiquidityMode.AccountBacked,
            initialPurchasingPower = settlementMoney
        };
        cityData.populationConsumption = new PopulationConsumptionConfig
        {
            paymentMode = ConsumptionPaymentMode.AccountBacked,
            initialPurchasingPower = populationMoney
        };

        return new CityRuntime(
            "city-" + id,
            cityData,
            new SpatialLocationRuntime("location-" + id));
    }
}
