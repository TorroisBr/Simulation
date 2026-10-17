using NUnit.Framework;

[TestFixture]
public sealed class DailyV1CityEconomicOwnerCoreTests
{
    [Test]
    public void MarketCounterpartyOwnsTheAccountInstanceAndStableCityIdentity()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(30f);
        MarketCounterpartyRuntime owner = MarketCounterpartyRuntime.CreateAccountBacked("city-1", account);

        Assert.That(owner.CounterpartyRuntimeId, Is.EqualTo("city-1"));
        Assert.That(owner.LiquidityMode, Is.EqualTo(MarketLiquidityMode.AccountBacked));
        Assert.That(owner.MoneyAccount, Is.SameAs(account));
    }

    [Test]
    public void FreePopulationEconomyHasNoAccountAndPreservesDerivedIdentity()
    {
        PopulationEconomyRuntime owner = new PopulationEconomyRuntime(
            "city-1", ConsumptionPaymentMode.Free, 75f, MarketLiquidityMode.Open);

        Assert.That(owner.CityRuntimeId, Is.EqualTo("city-1"));
        Assert.That(owner.RuntimeId, Is.EqualTo("population-city-1"));
        Assert.That(owner.PaymentMode, Is.EqualTo(ConsumptionPaymentMode.Free));
        Assert.That(owner.MoneyAccount, Is.Null);
    }

    [Test]
    public void AccountBackedPopulationUsesTheSharedMoneyAccountOwner()
    {
        PopulationEconomyRuntime owner = new PopulationEconomyRuntime(
            "city-1", ConsumptionPaymentMode.AccountBacked, 75f, MarketLiquidityMode.AccountBacked);

        Assert.That(owner.MoneyAccount, Is.Not.Null);
        Assert.That(owner.MoneyAccount.Balance, Is.EqualTo(75f));
        Assert.That(owner.MoneyAccount.Revision, Is.Zero);
    }

    [Test]
    public void AccountBackedPopulationRejectsAnOpenMarketAndInvalidStartingBalance()
    {
        Assert.Throws<System.InvalidOperationException>(() => new PopulationEconomyRuntime(
            "city-1", ConsumptionPaymentMode.AccountBacked, 75f, MarketLiquidityMode.Open));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new PopulationEconomyRuntime(
            "city-1", ConsumptionPaymentMode.AccountBacked, float.NaN, MarketLiquidityMode.AccountBacked));
    }
}