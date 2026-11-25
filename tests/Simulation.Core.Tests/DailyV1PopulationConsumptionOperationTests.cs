using System;
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1PopulationConsumptionOperationTests
{
    [Test]
    public void SelectedCityConsumptionCommitsThroughCoreOwnersAndPreservesOwnerCallbacks()
    {
        DailyV1CityRuntimeOwners owners = CreateAccountBackedCity(50f, 5, 1f);
        int populationCallbacks = 0;
        int counterpartyCallbacks = 0;
        int marketCallbacks = 0;
        Func<bool> admitAccountMutation = () => true;
        Action populationCommitted = () => populationCallbacks++;
        Action counterpartyCommitted = () => counterpartyCallbacks++;
        Action marketCommitted = () => marketCallbacks++;
        MoneyAccountOwnerBridge.BindP12MutationBoundary(
            owners.PopulationEconomy.MoneyAccount, admitAccountMutation, populationCommitted);
        MoneyAccountOwnerBridge.BindP12MutationBoundary(
            owners.Counterparty.MoneyAccount, admitAccountMutation, counterpartyCommitted);
        Assert.That(owners.Market.TryBindMutationBoundary(() => true, marketCommitted), Is.True);

        owners.SimulateSelectedConsumptionDay(null);

        DailyV1ItemDefinition item = owners.Definitions.Items["item/paid"];
        Assert.That(owners.Market.GetAmount(item), Is.EqualTo(4));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(46f));
        Assert.That(owners.Counterparty.MoneyAccount.Balance, Is.EqualTo(4f));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(owners.Counterparty.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(owners.Market.Revision, Is.EqualTo(1));
        Assert.That(populationCallbacks, Is.EqualTo(1));
        Assert.That(counterpartyCallbacks, Is.EqualTo(1));
        Assert.That(marketCallbacks, Is.EqualTo(1));
    }

    [Test]
    public void RejectedMarketInstallUsesExistingTransferRollbackAndAccountCallbacks()
    {
        DailyV1CityRuntimeOwners owners = CreateAccountBackedCity(50f, 5, 1f);
        int populationCallbacks = 0;
        int counterpartyCallbacks = 0;
        int marketCallbacks = 0;
        Func<bool> admitAccountMutation = () => true;
        MoneyAccountOwnerBridge.BindP12MutationBoundary(
            owners.PopulationEconomy.MoneyAccount, admitAccountMutation, () => populationCallbacks++);
        MoneyAccountOwnerBridge.BindP12MutationBoundary(
            owners.Counterparty.MoneyAccount, admitAccountMutation, () => counterpartyCallbacks++);
        Assert.That(owners.Market.TryBindMutationBoundary(() => false, () => marketCallbacks++), Is.True);

        DailyV1PopulationConsumptionOutcome result = DailyV1PopulationConsumptionOperation.Execute(
            owners.PopulationEconomy, owners.Counterparty, owners.Market,
            owners.Definitions.Items["item/paid"], 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Failure, Is.EqualTo(DailyV1PopulationConsumptionFailure.TransactionCommitFailed));
        Assert.That(owners.Market.GetAmount(owners.Definitions.Items["item/paid"]), Is.EqualTo(5));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(50f));
        Assert.That(owners.Counterparty.MoneyAccount.Balance, Is.EqualTo(0f));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Revision, Is.EqualTo(2));
        Assert.That(owners.Counterparty.MoneyAccount.Revision, Is.EqualTo(2));
        Assert.That(owners.Market.Revision, Is.Zero);
        Assert.That(populationCallbacks, Is.EqualTo(2));
        Assert.That(counterpartyCallbacks, Is.EqualTo(2));
        Assert.That(marketCallbacks, Is.Zero);
    }

    [Test]
    public void MissingSelectedStockRowRejectsWithoutMutation()
    {
        DailyV1CityRuntimeOwners owners = CreateAccountBackedCity(50f, 5, 1f);
        DailyV1EffectiveDefinitions definitions = owners.Definitions;
        DailyV1ItemDefinition otherItem = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            DailyV1TypedDefinitionGraphTests.CreateConfiguration(1f, new[] { "city/paid" }),
            SimulationConfigurationDefaults.Create(), new DailyV1CalendarDefinition(1, 1, 1, null),
            new[] { CreatePaidCityValues() },
            new[]
            {
                new DailyV1ItemData("item/paid", "Iron", 4f, null),
                new DailyV1ItemData("item/other", "Coal", 3f, null)
            },
            Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(),
            Array.Empty<DailyV1NpcActionData>(), Array.Empty<DailyV1NpcStatusData>(),
            Array.Empty<string>(), "test.daily-v1", "test.geography-v1",
            "test.geography-stage/v1", "hex-v1", "q-r-s").Items["item/other"];

        DailyV1PopulationConsumptionOutcome result = DailyV1PopulationConsumptionOperation.Execute(
            owners.PopulationEconomy, owners.Counterparty, owners.Market, otherItem, 1);

        Assert.That(definitions.Items.ContainsKey(otherItem.DefinitionId), Is.False);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Failure, Is.EqualTo(DailyV1PopulationConsumptionFailure.InsufficientStock));
        Assert.That(owners.Market.GetAmount(definitions.Items["item/paid"]), Is.EqualTo(5));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(50f));
        Assert.That(owners.Counterparty.MoneyAccount.Balance, Is.EqualTo(0f));
    }

    [Test]
    public void InvalidInputAndQuantityKeepTheExistingFailureCategories()
    {
        DailyV1CityRuntimeOwners owners = CreateAccountBackedCity(50f, 5, 1f);
        DailyV1ItemDefinition item = owners.Definitions.Items["item/paid"];

        Assert.That(DailyV1PopulationConsumptionOperation.Execute(
            null, owners.Counterparty, owners.Market, item, 1).Failure,
            Is.EqualTo(DailyV1PopulationConsumptionFailure.InvalidInput));
        Assert.That(DailyV1PopulationConsumptionOperation.Execute(
            owners.PopulationEconomy, null, owners.Market, item, 1).Failure,
            Is.EqualTo(DailyV1PopulationConsumptionFailure.InvalidInput));
        Assert.That(DailyV1PopulationConsumptionOperation.Execute(
            owners.PopulationEconomy, owners.Counterparty, owners.Market, item, 0).Failure,
            Is.EqualTo(DailyV1PopulationConsumptionFailure.InvalidQuantity));
        Assert.That(DailyV1PopulationConsumptionOperation.Execute(
            owners.PopulationEconomy, owners.Counterparty, owners.Market, item, -1).Failure,
            Is.EqualTo(DailyV1PopulationConsumptionFailure.InvalidQuantity));

        Assert.That(owners.Market.GetAmount(item), Is.EqualTo(5));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(50f));
        Assert.That(owners.Counterparty.MoneyAccount.Balance, Is.EqualTo(0f));
    }

    [Test]
    public void ZeroBasePriceRetainsTheSelectedMarketMinimumAndUnaffordableRequestDoesNotMutate()
    {
        DailyV1CityRuntimeOwners owners = CreateAccountBackedCity(0.01f, 5, 1f, 0f);
        DailyV1ItemDefinition item = owners.Definitions.Items["item/paid"];

        Assert.That(owners.Market.GetPrice(item), Is.EqualTo(0.01f));
        DailyV1PopulationConsumptionOutcome result = DailyV1PopulationConsumptionOperation.Execute(
            owners.PopulationEconomy, owners.Counterparty, owners.Market, item, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UnitPrice, Is.EqualTo(0.01f));
        Assert.That(result.TotalPrice, Is.EqualTo(0.01f));
        Assert.That(owners.Market.GetAmount(item), Is.EqualTo(4));

        DailyV1CityRuntimeOwners poor = CreateAccountBackedCity(0.005f, 5, 1f, 0f);
        DailyV1PopulationConsumptionOutcome rejected = DailyV1PopulationConsumptionOperation.Execute(
            poor.PopulationEconomy, poor.Counterparty, poor.Market,
            poor.Definitions.Items["item/paid"], 1);
        Assert.That(rejected.Success, Is.False);
        Assert.That(rejected.Failure, Is.EqualTo(DailyV1PopulationConsumptionFailure.InsufficientFunds));
        Assert.That(poor.Market.GetAmount(poor.Definitions.Items["item/paid"]), Is.EqualTo(5));
        Assert.That(poor.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(0.005f));
    }

    [Test]
    public void InvalidSelectedPriceIsRejectedBeforeAnyOwnerMutation()
    {
        DailyV1CityRuntimeOwners owners = CreateAccountBackedCity(50f, 5, 1f, float.NaN);

        DailyV1PopulationConsumptionOutcome result = DailyV1PopulationConsumptionOperation.Execute(
            owners.PopulationEconomy, owners.Counterparty, owners.Market,
            owners.Definitions.Items["item/paid"], 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Failure, Is.EqualTo(DailyV1PopulationConsumptionFailure.InvalidPrice));
        Assert.That(owners.Market.GetAmount(owners.Definitions.Items["item/paid"]), Is.EqualTo(5));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(50f));
        Assert.That(owners.Counterparty.MoneyAccount.Balance, Is.EqualTo(0f));
    }

    [Test]
    public void CounterpartyCreditCapacityRejectsBeforeAnyOwnerMutation()
    {
        DailyV1CityRuntimeOwners owners = CreateAccountBackedCity(50f, 5, 1f);
        Assert.That(owners.Counterparty.MoneyAccount.TryCredit(float.MaxValue), Is.True);

        DailyV1PopulationConsumptionOutcome result = DailyV1PopulationConsumptionOperation.Execute(
            owners.PopulationEconomy, owners.Counterparty, owners.Market,
            owners.Definitions.Items["item/paid"], 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Failure, Is.EqualTo(DailyV1PopulationConsumptionFailure.CounterpartyCreditCapacity));
        Assert.That(owners.Market.GetAmount(owners.Definitions.Items["item/paid"]), Is.EqualTo(5));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(50f));
        Assert.That(owners.Counterparty.MoneyAccount.Balance, Is.EqualTo(float.MaxValue));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Revision, Is.Zero);
        Assert.That(owners.Counterparty.MoneyAccount.Revision, Is.EqualTo(1));
    }

    [Test]
    public void AccountMutationAdmissionRejectsBeforeAnyOwnerMutation()
    {
        DailyV1CityRuntimeOwners owners = CreateAccountBackedCity(50f, 5, 1f);
        int populationCallbacks = 0;
        int counterpartyCallbacks = 0;
        Func<bool> rejectAccountMutation = () => false;
        MoneyAccountOwnerBridge.BindP12MutationBoundary(
            owners.PopulationEconomy.MoneyAccount, rejectAccountMutation, () => populationCallbacks++);
        MoneyAccountOwnerBridge.BindP12MutationBoundary(
            owners.Counterparty.MoneyAccount, () => true, () => counterpartyCallbacks++);

        DailyV1PopulationConsumptionOutcome result = DailyV1PopulationConsumptionOperation.Execute(
            owners.PopulationEconomy, owners.Counterparty, owners.Market,
            owners.Definitions.Items["item/paid"], 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Failure, Is.EqualTo(DailyV1PopulationConsumptionFailure.TransactionCommitFailed));
        Assert.That(owners.Market.GetAmount(owners.Definitions.Items["item/paid"]), Is.EqualTo(5));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(50f));
        Assert.That(owners.Counterparty.MoneyAccount.Balance, Is.EqualTo(0f));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Revision, Is.Zero);
        Assert.That(owners.Counterparty.MoneyAccount.Revision, Is.Zero);
        Assert.That(populationCallbacks, Is.Zero);
        Assert.That(counterpartyCallbacks, Is.Zero);
    }

    private static DailyV1CityRuntimeOwners CreateAccountBackedCity(
        float balance, int stock, float consumptionPer1000, float basePrice = 4f)
    {
        DailyV1CityData cityValues = CreatePaidCityValues(balance, stock, consumptionPer1000);
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            DailyV1TypedDefinitionGraphTests.CreateConfiguration(1f, new[] { "city/paid" }),
            SimulationConfigurationDefaults.Create(), new DailyV1CalendarDefinition(1, 1, 1, null),
            new[] { cityValues }, new[] { new DailyV1ItemData("item/paid", "Iron", basePrice, null) },
            Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(),
            Array.Empty<DailyV1NpcActionData>(), Array.Empty<DailyV1NpcStatusData>(),
            Array.Empty<string>(), "test.daily-v1", "test.geography-v1",
            "test.geography-stage/v1", "hex-v1", "q-r-s");
        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial("city-runtime-paid", "location-runtime-paid",
            definitions, definitions.Cities["city/paid"], null,
            out DailyV1CityRuntimeOwners owners), Is.True);
        return owners;
    }

    private static DailyV1CityData CreatePaidCityValues(float balance = 50f, int stock = 5, float consumptionPer1000 = 1f) =>
        new DailyV1CityData(
            "city/paid", "Paid City", "settlement/paid", "location/paid", "market/paid",
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.ExogenousDaily, "ExogenousDaily"),
            1000, new DailyV1MarketLiquidityConfig(
                new DailyV1EnumValue((int)MarketLiquidityMode.AccountBacked, "AccountBacked"), 0f),
            new DailyV1PopulationConsumptionConfig(
                new DailyV1EnumValue((int)ConsumptionPaymentMode.AccountBacked, "AccountBacked"), balance),
            new[] { new DailyV1MarketItemConfig("item/paid", stock, stock, consumptionPer1000) },
            Array.Empty<DailyV1CityProductionConfig>(), new List<DailyV1CityConnection>());
}
