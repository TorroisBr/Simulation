using System.Collections.Generic;
using NUnit.Framework;

public sealed class SettlementStockOwnershipTests
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
    public void CityMarkets_ProjectSettlementOwnershipForOpenAndAccountBackedModes()
    {
        CityRuntime openCity = SimulationTestFactory.CreateCity("city-open", "location-open");
        CityRuntime accountCity = SimulationTestFactory.CreateAccountBackedCity("city-account", "location-account", 10f);

        SimulationInvariantValidator.ValidateCityMarketCounterparty(openCity);
        SimulationInvariantValidator.ValidateCityMarketCounterparty(accountCity);
        Assert.That(openCity.Market.StockOwnerRuntimeId, Is.EqualTo(openCity.RuntimeId));
        Assert.That(accountCity.Market.StockOwnerRuntimeId, Is.EqualTo(accountCity.RuntimeId));
        Assert.That(openCity.Market.Counterparty.CounterpartyRuntimeId, Is.EqualTo(openCity.Market.StockOwnerRuntimeId));
        Assert.That(accountCity.Market.Counterparty.CounterpartyRuntimeId, Is.EqualTo(accountCity.Market.StockOwnerRuntimeId));
    }

    [Test]
    public void StandaloneMarketDoesNotInventStockOwnerIdentity()
    {
        MarketRuntime market = new MarketRuntime();

        Assert.That(market.StockOwnerRuntimeId, Is.Null.Or.Empty);
        Assert.That(market.Counterparty.CounterpartyRuntimeId, Is.Null.Or.Empty);
    }

    [Test]
    public void InitialMarketStockIsSettlementOwned()
    {
        ItemData item = SimulationTestFactory.CreateItem("initial-stock");
        CityRuntime city = SimulationTestFactory.CreateCity(
            "city-initial-stock",
            "location-initial-stock",
            SimulationTestFactory.CreateMarketItem(item, 7, 10));

        Assert.That(city.Market.GetAmount(item), Is.EqualTo(7));
        Assert.That(city.Market.StockOwnerRuntimeId, Is.EqualTo(city.RuntimeId));
    }

    [Test]
    public void ProductionReturnsImmutableSettlementOwnedResultsForMultipleValidConfigs()
    {
        ItemData firstItem = SimulationTestFactory.CreateItem("production-first");
        ItemData secondItem = SimulationTestFactory.CreateItem("production-second");
        CityData definition = SimulationTestFactory.CreateCityData("production-city");
        definition.productionConfigs.Add(new CityProductionConfig { item = firstItem, amountPerDay = 5 });
        definition.productionConfigs.Add(new CityProductionConfig { item = secondItem, amountPerDay = 3 });
        CityRuntime city = new CityRuntime("city-production", definition, new SpatialLocationRuntime("location-production"));

        IReadOnlyList<CityProductionResult> results = city.SimulateProductionDay();

        Assert.That(results.Count, Is.EqualTo(2));
        SimulationInvariantValidator.ValidateCityProductionResult(results[0]);
        SimulationInvariantValidator.ValidateCityProductionResult(results[1]);
        Assert.That(results[0].SettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(results[0].StockOwnerRuntimeId, Is.EqualTo(city.Market.StockOwnerRuntimeId));
        Assert.That(results[0].ItemDefinitionId, Is.EqualTo(firstItem.DefinitionId));
        Assert.That(results[0].QuantityProduced, Is.EqualTo(5));
        Assert.That(results[1].ItemDefinitionId, Is.EqualTo(secondItem.DefinitionId));
        Assert.That(results[1].QuantityProduced, Is.EqualTo(3));
        Assert.That(city.Market.GetAmount(firstItem), Is.EqualTo(5));
        Assert.That(city.Market.GetAmount(secondItem), Is.EqualTo(3));

        city.SimulateProductionDay();

        Assert.That(results[0].QuantityProduced, Is.EqualTo(5));
        Assert.That(results[0].ItemDefinitionId, Is.EqualTo(firstItem.DefinitionId));
    }

    [Test]
    public void InvalidProductionConfigsAreSkipped()
    {
        ItemData validItem = SimulationTestFactory.CreateItem("valid-production");
        CityData definition = SimulationTestFactory.CreateCityData("invalid-production-city");
        definition.productionConfigs.Add(null);
        definition.productionConfigs.Add(new CityProductionConfig { item = null, amountPerDay = 5 });
        definition.productionConfigs.Add(new CityProductionConfig { item = validItem, amountPerDay = 0 });
        definition.productionConfigs.Add(new CityProductionConfig { item = validItem, amountPerDay = -1 });
        definition.productionConfigs.Add(new CityProductionConfig { item = validItem, amountPerDay = 2 });
        CityRuntime city = new CityRuntime("city-invalid-production", definition, new SpatialLocationRuntime("location-invalid-production"));

        IReadOnlyList<CityProductionResult> results = city.SimulateProductionDay();

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].QuantityProduced, Is.EqualTo(2));
        Assert.That(city.Market.GetAmount(validItem), Is.EqualTo(2));
    }

    [Test]
    public void ProductionDoesNotMoveSettlementMoneyEvenWithZeroBalance()
    {
        ItemData item = SimulationTestFactory.CreateItem("zero-money-production");
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity("city-zero-production", "location-zero-production", 0f);
        city.CityData.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = 5 });

        IReadOnlyList<CityProductionResult> results = city.SimulateProductionDay();

        Assert.That(results[0].QuantityProduced, Is.EqualTo(5));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(5));
        Assert.That(city.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(0f));
    }

    [Test]
    public void ProductionProceedsEnterSettlementWhenNpcBuysProducedStock()
    {
        ItemData item = SimulationTestFactory.CreateItem("production-proceeds", 10f);
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity("city-proceeds", "location-proceeds", 0f);
        city.CityData.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = 5 });
        NpcRuntime buyer = CreateNpc("production-buyer", 100f);
        EconomyTransactionService service = new EconomyTransactionService();

        city.SimulateProductionDay();
        EconomyTransactionResult purchase = service.TryExecuteMarketPurchase(buyer, city.Market, item, 2);

        Assert.That(purchase.Success, Is.True);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(3));
        Assert.That(buyer.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(city.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(purchase.TotalPrice).Within(0.001f));
        Assert.That(purchase.ItemSourceRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(purchase.ItemDestinationRuntimeId, Is.EqualTo(buyer.RuntimeId));
    }

    [Test]
    public void NpcTradeReceiptSeparatesMoneyAndItemDirections()
    {
        ItemData item = SimulationTestFactory.CreateItem("npc-item");
        NpcRuntime buyer = CreateNpc("npc-buyer", 50f);
        NpcRuntime seller = CreateNpc("npc-seller", 0f);
        seller.Inventory.AddItem(item, 2, 3f);

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteNpcTrade(buyer, seller, item, 1, 10f);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SourceRuntimeId, Is.EqualTo(buyer.RuntimeId));
        Assert.That(result.DestinationRuntimeId, Is.EqualTo(seller.RuntimeId));
        Assert.That(result.ItemSourceRuntimeId, Is.EqualTo(seller.RuntimeId));
        Assert.That(result.ItemDestinationRuntimeId, Is.EqualTo(buyer.RuntimeId));
    }

    [Test]
    public void AccountBackedMarketReceiptsDeclareSettlementItemFlow()
    {
        ItemData item = SimulationTestFactory.CreateItem("market-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity(
            "city-receipts", "location-receipts", 50f,
            SimulationTestFactory.CreateMarketItem(item, 10, 10));
        NpcRuntime buyer = CreateNpc("market-buyer", 100f);
        NpcRuntime seller = CreateNpc("market-seller", 0f);
        seller.Inventory.AddItem(item, 2, 3f);
        EconomyTransactionService service = new EconomyTransactionService();

        EconomyTransactionResult purchase = service.TryExecuteMarketPurchase(buyer, city.Market, item, 1);
        EconomyTransactionResult sale = service.TryExecuteMarketSale(seller, city.Market, item, 1);

        Assert.That(purchase.ItemSourceRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(purchase.ItemDestinationRuntimeId, Is.EqualTo(buyer.RuntimeId));
        Assert.That(sale.ItemSourceRuntimeId, Is.EqualTo(seller.RuntimeId));
        Assert.That(sale.ItemDestinationRuntimeId, Is.EqualTo(city.RuntimeId));
    }

    [Test]
    public void OpenMarketReceiptsStillDeclareSettlementItemFlow()
    {
        ItemData item = SimulationTestFactory.CreateItem("open-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "city-open-flow", "location-open-flow",
            SimulationTestFactory.CreateMarketItem(item, 5, 5));
        NpcRuntime buyer = CreateNpc("open-buyer", 100f);
        NpcRuntime seller = CreateNpc("open-seller", 0f);
        seller.Inventory.AddItem(item, 2, 3f);
        EconomyTransactionService service = new EconomyTransactionService();

        EconomyTransactionResult purchase = service.TryExecuteMarketPurchase(buyer, city.Market, item, 1);
        EconomyTransactionResult sale = service.TryExecuteMarketSale(seller, city.Market, item, 1);

        Assert.That(purchase.MoneyEffect, Is.EqualTo(MoneyEffect.ExplicitSink));
        Assert.That(purchase.ItemSourceRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(purchase.ItemDestinationRuntimeId, Is.EqualTo(buyer.RuntimeId));
        Assert.That(sale.MoneyEffect, Is.EqualTo(MoneyEffect.ExplicitSource));
        Assert.That(sale.ItemSourceRuntimeId, Is.EqualTo(seller.RuntimeId));
        Assert.That(sale.ItemDestinationRuntimeId, Is.EqualTo(city.RuntimeId));
    }

    [Test]
    public void FailedMarketTransactionDoesNotDeclareItemTransfer()
    {
        ItemData item = SimulationTestFactory.CreateItem("failed-item");
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity("city-failed", "location-failed", 0f);
        NpcRuntime seller = CreateNpc("failed-seller", 0f);
        seller.Inventory.AddItem(item, 1, 3f);

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteMarketSale(seller, city.Market, item, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ItemSourceRuntimeId, Is.Null.Or.Empty);
        Assert.That(result.ItemDestinationRuntimeId, Is.Null.Or.Empty);
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(1));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(0));
    }

    [Test]
    public void SaleToMarketThenResaleMovesItemOwnershipBetweenContainers()
    {
        ItemData item = SimulationTestFactory.CreateItem("resale-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("city-resale", "location-resale");
        NpcRuntime seller = CreateNpc("resale-seller", 0f);
        NpcRuntime buyer = CreateNpc("resale-buyer", 100f);
        seller.Inventory.AddItem(item, 1, 3f);
        EconomyTransactionService service = new EconomyTransactionService();

        EconomyTransactionResult sale = service.TryExecuteMarketSale(seller, city.Market, item, 1);
        EconomyTransactionResult purchase = service.TryExecuteMarketPurchase(buyer, city.Market, item, 1);

        Assert.That(sale.Success, Is.True);
        Assert.That(purchase.Success, Is.True);
        Assert.That(sale.ItemSourceRuntimeId, Is.EqualTo(seller.RuntimeId));
        Assert.That(sale.ItemDestinationRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(purchase.ItemSourceRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(purchase.ItemDestinationRuntimeId, Is.EqualTo(buyer.RuntimeId));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(0));
        Assert.That(buyer.Inventory.GetAmount(item), Is.EqualTo(1));
    }

    [Test]
    public void ConsumptionRemovesSettlementStockWithoutMovingMoney()
    {
        ItemData item = SimulationTestFactory.CreateItem("consumption-item");
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity(
            "city-consumption", "location-consumption", 0f,
            SimulationTestFactory.CreateMarketItem(item, 5, 5));
        city.CityData.marketItems[0].consumptionPer1000Population = 1000f;

        city.SimulateConsumptionDay();

        Assert.That(city.Market.GetAmount(item), Is.EqualTo(0));
        Assert.That(city.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(0f));
    }

    [Test]
    public void NumericStockOverflowDoesNotCorruptMarketStateOrProductionResult()
    {
        ItemData item = SimulationTestFactory.CreateItem("overflow-item");
        MarketRuntime market = new MarketRuntime();

        Assert.That(market.AddStock(item, int.MaxValue), Is.EqualTo(int.MaxValue));
        Assert.That(market.CanAddStock(item, 1), Is.False);
        Assert.That(market.AddStock(item, 1), Is.EqualTo(0));
        Assert.That(market.GetAmount(item), Is.EqualTo(int.MaxValue));

        CityData definition = SimulationTestFactory.CreateCityData("overflow-production");
        definition.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = 1 });
        CityRuntime city = new CityRuntime("city-overflow", definition, new SpatialLocationRuntime("location-overflow"));
        Assert.That(city.Market.AddStock(item, int.MaxValue), Is.EqualTo(int.MaxValue));

        IReadOnlyList<CityProductionResult> results = city.SimulateProductionDay();

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].QuantityProduced, Is.EqualTo(0));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(int.MaxValue));
    }

    private static NpcRuntime CreateNpc(string id, float money)
    {
        return new NpcRuntime(id, SimulationTestFactory.CreateNpc(id), null, money);
    }
}
