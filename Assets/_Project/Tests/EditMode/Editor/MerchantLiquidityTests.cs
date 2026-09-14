using NUnit.Framework;

public sealed class MerchantLiquidityTests
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
    public void Planning_KnownFiniteLiquidityLimitsMarketSaleAndEvidenceQuantity()
    {
        Fixture fixture = new Fixture(25f);
        fixture.ObserveLiquidity(MarketLiquidityMode.AccountBacked, 25f, 0L);

        NpcActionRuntime action = fixture.CreateSaleAction();

        Assert.That(action, Is.Not.Null);
        Assert.That(action.Amount, Is.EqualTo(2));
        Assert.That(action.CommercialDecisionEvidence.ExpectedQuantity, Is.EqualTo(2));
        Assert.That(action.CommercialDecisionEvidence.ExpectedGrossProfit, Is.EqualTo(18f));
        Assert.That(action.CommercialDecisionEvidence.DestinationLiquidityObservation.ObservedPurchasingPower, Is.EqualTo(25f));
    }

    [Test]
    public void Planning_KnownZeroLiquidityBlocksMarketSale()
    {
        Fixture fixture = new Fixture(0f);
        fixture.ObserveLiquidity(MarketLiquidityMode.AccountBacked, 0f, 0L);

        Assert.That(fixture.CreateSaleAction(), Is.Null);
    }

    [Test]
    public void Planning_OpenLiquidityDoesNotLimitMarketSale()
    {
        Fixture fixture = new Fixture(0f);
        fixture.ObserveLiquidity(MarketLiquidityMode.Open, 0f, 0L);

        Assert.That(fixture.CreateSaleAction().Amount, Is.EqualTo(5));
    }

    [Test]
    public void Planning_UnknownLiquidityIsNotTreatedAsZero()
    {
        Fixture fixture = new Fixture(0f);

        Assert.That(fixture.CreateSaleAction().Amount, Is.EqualTo(5));
    }

    [Test]
    public void Planning_UsesRememberedLiquidityWithoutReadingChangedWorldTruth()
    {
        Fixture fixture = new Fixture(0f);
        fixture.ObserveLiquidity(MarketLiquidityMode.AccountBacked, 100f, 0L);
        float moneyBefore = fixture.Merchant.Money;
        int inventoryBefore = fixture.Merchant.Inventory.GetAmount(fixture.Item);
        int stockBefore = fixture.City.Market.GetAmount(fixture.Item);

        NpcActionRuntime action = fixture.CreateSaleAction();

        Assert.That(action.Amount, Is.EqualTo(5));
        Assert.That(fixture.Merchant.Money, Is.EqualTo(moneyBefore));
        Assert.That(fixture.Merchant.Inventory.GetAmount(fixture.Item), Is.EqualTo(inventoryBefore));
        Assert.That(fixture.City.Market.GetAmount(fixture.Item), Is.EqualTo(stockBefore));
        Assert.That(fixture.System.TryExecuteAction(fixture.Merchant, action).Success, Is.False);
        Assert.That(fixture.Merchant.Money, Is.EqualTo(moneyBefore));
        Assert.That(fixture.Merchant.Inventory.GetAmount(fixture.Item), Is.EqualTo(inventoryBefore));
    }

    [Test]
    public void Execution_UsesCurrentSettlementTruthAndMayPartiallyFillStalePlan()
    {
        Fixture fixture = new Fixture(20f, 31L);
        fixture.ObserveLiquidity(MarketLiquidityMode.AccountBacked, 100f, 0L);

        NpcActionRuntime action = fixture.CreateSaleAction();
        NpcActionResult result = fixture.System.TryExecuteAction(fixture.Merchant, action);

        Assert.That(action.Amount, Is.EqualTo(5));
        Assert.That(result.Success, Is.True);
        Assert.That(fixture.Merchant.Inventory.GetAmount(fixture.Item), Is.EqualTo(3));
        Assert.That(fixture.City.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(0f));
        Assert.That(fixture.Merchant.Money, Is.EqualTo(20f));
        Assert.That(fixture.Merchant.Money + fixture.City.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(20f));
        Assert.That(fixture.Merchant.Inventory.GetAmount(fixture.Item) + fixture.City.Market.GetAmount(fixture.Item), Is.EqualTo(15));
    }

    [Test]
    public void LocalNpcBuyerUsesNpcMoneyEvenWhenSettlementLiquidityIsKnownZero()
    {
        Fixture fixture = new Fixture(0f);
        NpcRuntime localBuyer = new NpcRuntime(
            "npc-local-buyer",
            SimulationTestFactory.CreateNpc("local-buyer", NpcJobType.Merchant, MerchantBehavior.Local),
            fixture.City,
            100f);
        fixture.ObserveLiquidity(MarketLiquidityMode.AccountBacked, 0f, 0L);
        fixture.Merchant.SetMerchantTradePlan(
            fixture.Item,
            fixture.City,
            fixture.City,
            5,
            1f);

        float sellerMoneyBefore = fixture.Merchant.Money;
        float buyerMoneyBefore = localBuyer.Money;
        int sellerInventoryBefore = fixture.Merchant.Inventory.GetAmount(fixture.Item);
        int buyerInventoryBefore = localBuyer.Inventory.GetAmount(fixture.Item);

        NpcActionRuntime action = fixture.CreateSaleAction();

        Assert.That(action, Is.Not.Null);
        Assert.That(action.TargetNpc, Is.SameAs(localBuyer));
        Assert.That(action.Amount, Is.EqualTo(5));
        Assert.That(fixture.Merchant.CommercialKnowledge.TryGetLiquidityObservation(
            fixture.City.Location.RuntimeId,
            out CommercialLiquidityObservation liquidity), Is.True);
        Assert.That(liquidity.LiquidityMode, Is.EqualTo(MarketLiquidityMode.AccountBacked));
        Assert.That(liquidity.ObservedPurchasingPower, Is.EqualTo(0f));

        NpcActionResult result = fixture.System.TryExecuteAction(fixture.Merchant, action);

        Assert.That(result.Success, Is.True);
        Assert.That(localBuyer.Money, Is.LessThan(buyerMoneyBefore));
        Assert.That(fixture.Merchant.Money, Is.GreaterThan(sellerMoneyBefore));
        Assert.That(fixture.Merchant.Inventory.GetAmount(fixture.Item), Is.LessThan(sellerInventoryBefore));
        Assert.That(localBuyer.Inventory.GetAmount(fixture.Item), Is.GreaterThan(buyerInventoryBefore));
        Assert.That(fixture.City.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(0f));
    }

    [Test]
    public void Planning_KnownLiquidityChangesDestinationChoiceWithoutReadingDestinationTruth()
    {
        ItemData item = SimulationTestFactory.CreateItem("route-item", 5f);
        ThreeCityFixture world = new ThreeCityFixture();
        NpcRuntime merchant = new NpcRuntime(
            "npc-route-merchant",
            SimulationTestFactory.CreateNpc("route-merchant", NpcJobType.Merchant, MerchantBehavior.Traveling),
            world.A,
            100f);
        merchant.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverLocation(world.C.Location.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(world.RouteAC.RuntimeId);
        merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            world.A.Location.RuntimeId, item, 5f, 10, 0L, 0L));
        merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            world.B.Location.RuntimeId, item, 20f, 10, 0L, 0L));
        merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            world.C.Location.RuntimeId, item, 15f, 10, 0L, 0L));
        merchant.CommercialKnowledge.RecordLiquidityObservation(SimulationTestFactory.CreateLiquidityObservation(
            world.B.Location.RuntimeId, MarketLiquidityMode.AccountBacked, 0f, 0L, 0L));
        merchant.CommercialKnowledge.RecordLiquidityObservation(SimulationTestFactory.CreateLiquidityObservation(
            world.C.Location.RuntimeId, MarketLiquidityMode.AccountBacked, 100f, 0L, 0L));
        SimulationTime time = new SimulationTime();
        MerchantSystem system = SimulationTestFactory.CreateMerchantSystem(world.CreateTravelSystem(time), time);
        float utility = 10f;

        NpcActionRuntime action = system.CreateAction(
            merchant,
            SimulationTestFactory.CreateAction("buy-route", NpcActionType.BuyGoods, NpcActionCategory.Commerce),
            ref utility);

        Assert.That(action, Is.Not.Null);
        Assert.That(action.TargetCity, Is.SameAs(world.C));
        Assert.That(action.Amount, Is.EqualTo(5));
        Assert.That(action.CommercialDecisionEvidence.DestinationLiquidityObservation.ObservedPurchasingPower, Is.EqualTo(100f));
    }

    private sealed class Fixture
    {
        public ItemData Item { get; }
        public CityRuntime City { get; }
        public NpcRuntime Merchant { get; }
        public MerchantSystem System { get; }

        private readonly SimulationTime time;

        public Fixture(float settlementMoney, long absoluteDay = 0L)
        {
            Item = SimulationTestFactory.CreateItem("liquidity-item", 10f);
            City = SimulationTestFactory.CreateAccountBackedCity(
                "city-liquidity",
                "location-liquidity",
                settlementMoney,
                SimulationTestFactory.CreateMarketItem(Item, 10, 10));
            Merchant = new NpcRuntime(
                "npc-merchant",
                SimulationTestFactory.CreateNpc("merchant", NpcJobType.Merchant, MerchantBehavior.Traveling),
                City,
                0f);
            Merchant.Inventory.AddItem(Item, 5, 1f);
            time = new SimulationTime(absoluteDay);
            System = SimulationTestFactory.CreateMerchantSystem(null, time);
            Merchant.CommercialKnowledge.RecordObservation(SimulationTestFactory.CreateObservation(
                City.Location.RuntimeId,
                Item,
                10f,
                10,
                absoluteDay,
                absoluteDay));
        }

        public void ObserveLiquidity(MarketLiquidityMode mode, float purchasingPower, long observedDay)
        {
            Merchant.CommercialKnowledge.RecordLiquidityObservation(SimulationTestFactory.CreateLiquidityObservation(
                City.Location.RuntimeId,
                mode,
                purchasingPower,
                observedDay,
                time.AbsoluteDay));
        }

        public NpcActionRuntime CreateSaleAction()
        {
            float utility = 10f;
            return System.CreateAction(
                Merchant,
                SimulationTestFactory.CreateAction("sell-liquidity", NpcActionType.SellGoods, NpcActionCategory.Commerce),
                ref utility);
        }
    }
}
