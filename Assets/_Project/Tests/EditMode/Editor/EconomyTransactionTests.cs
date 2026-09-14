using NUnit.Framework;
using UnityEngine;

public sealed class EconomyTransactionTests
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
    public void MoneyAccount_CanCreditRejectsOverflowWithoutMutation()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(float.MaxValue);

        Assert.That(account.CanCredit(float.MaxValue), Is.False);
        Assert.That(account.Balance, Is.EqualTo(float.MaxValue));
    }

    [Test]
    public void MoneyTransfer_PrevalidatesDestinationBeforeDebitingSource()
    {
        EconomyTransactionService service = new EconomyTransactionService();
        MoneyAccountRuntime source = new MoneyAccountRuntime(float.MaxValue);
        MoneyAccountRuntime destination = new MoneyAccountRuntime(float.MaxValue);

        EconomyTransactionResult result = service.TryTransferMoney(source, destination, float.MaxValue);

        Assert.That(result.Success, Is.False);
        Assert.That(result.TransactionType, Is.EqualTo(EconomyTransactionType.MoneyTransfer));
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.CreditRejected));
        Assert.That(source.Balance, Is.EqualTo(float.MaxValue));
        Assert.That(destination.Balance, Is.EqualTo(float.MaxValue));
    }

    [Test]
    public void MoneyTransfer_SuccessConservesMoneyAndCapturesRuntimeIds()
    {
        NpcRuntime source = CreateNpc("source", 75f);
        NpcRuntime destination = CreateNpc("destination", 10f);
        EconomyTransactionResult result = new EconomyTransactionService().TryTransferMoney(source, destination, 25f);

        Assert.That(result.Success, Is.True);
        Assert.That(result.MoneyEffect, Is.EqualTo(MoneyEffect.Transfer));
        Assert.That(result.Amount, Is.EqualTo(25f));
        Assert.That(result.SourceRuntimeId, Is.EqualTo(source.RuntimeId));
        Assert.That(result.DestinationRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(source.Money + destination.Money, Is.EqualTo(85f));
    }

    [Test]
    public void NpcTrade_FailedPaymentDoesNotMoveItemsOrMoney()
    {
        ItemData item = SimulationTestFactory.CreateItem("trade-item", 10f);
        NpcRuntime buyer = CreateNpc("buyer", 10f);
        NpcRuntime seller = CreateNpc("seller", 0f);
        seller.Inventory.AddItem(item, 2, 4f);

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteNpcTrade(buyer, seller, item, 2, 10f);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InsufficientFunds));
        Assert.That(buyer.Money, Is.EqualTo(10f));
        Assert.That(seller.Money, Is.EqualTo(0f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.EqualTo(0));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(2));
    }

    [Test]
    public void NpcTrade_SuccessMovesItemsAndMoneyAtomically()
    {
        ItemData item = SimulationTestFactory.CreateItem("trade-item", 10f);
        NpcRuntime buyer = CreateNpc("buyer", 100f);
        NpcRuntime seller = CreateNpc("seller", 20f);
        seller.Inventory.AddItem(item, 5, 4f);

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteNpcTrade(buyer, seller, item, 2, 12f);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TransactionType, Is.EqualTo(EconomyTransactionType.NpcToNpcTrade));
        Assert.That(result.ItemDefinitionId, Is.EqualTo(item.DefinitionId));
        Assert.That(result.Quantity, Is.EqualTo(2));
        Assert.That(result.UnitPrice, Is.EqualTo(12f));
        Assert.That(result.TotalPrice, Is.EqualTo(24f));
        Assert.That(buyer.Money, Is.EqualTo(76f));
        Assert.That(seller.Money, Is.EqualTo(44f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(3));
        Assert.That(buyer.Money + seller.Money, Is.EqualTo(120f));
        Assert.That(buyer.Inventory.GetAmount(item) + seller.Inventory.GetAmount(item), Is.EqualTo(5));
    }

    [Test]
    public void OpenMarketPurchaseUsesExplicitSinkAndFailedPaymentLeavesStock()
    {
        ItemData item = SimulationTestFactory.CreateItem("market-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("market-city", "market-location", SimulationTestFactory.CreateMarketItem(item, 5, 5));
        NpcRuntime npc = CreateNpc("buyer", 5f);
        EconomyTransactionService service = new EconomyTransactionService();

        EconomyTransactionResult failed = service.TryExecuteOpenMarketPurchase(npc, city.Market, item, 1);

        Assert.That(failed.Success, Is.False);
        Assert.That(failed.MoneyEffect, Is.EqualTo(MoneyEffect.ExplicitSink));
        Assert.That(npc.Money, Is.EqualTo(5f));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(5));

        npc.MoneyAccount.TryCredit(5f);
        EconomyTransactionResult success = service.TryExecuteOpenMarketPurchase(npc, city.Market, item, 1);

        Assert.That(success.Success, Is.True);
        Assert.That(success.TransactionType, Is.EqualTo(EconomyTransactionType.OpenMarketPurchase));
        Assert.That(success.Quantity, Is.EqualTo(1));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(1));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(4));
        Assert.That(npc.Money, Is.EqualTo(0f));
    }

    [Test]
    public void OpenMarketSaleUsesExplicitSourceAndInvalidSaleDoesNotCreateEntry()
    {
        ItemData item = SimulationTestFactory.CreateItem("market-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("market-city", "market-location");
        NpcRuntime npc = CreateNpc("seller", 0f);
        EconomyTransactionService service = new EconomyTransactionService();

        EconomyTransactionResult invalid = service.TryExecuteOpenMarketSale(npc, city.Market, item, 1);

        Assert.That(invalid.Success, Is.False);
        Assert.That(city.Market.GetItem(item), Is.Null);
        Assert.That(npc.Money, Is.EqualTo(0f));

        npc.Inventory.AddItem(item, 2, 3f);
        EconomyTransactionResult success = service.TryExecuteOpenMarketSale(npc, city.Market, item, 1);

        Assert.That(success.Success, Is.True);
        Assert.That(success.TransactionType, Is.EqualTo(EconomyTransactionType.OpenMarketSale));
        Assert.That(success.MoneyEffect, Is.EqualTo(MoneyEffect.ExplicitSource));
        Assert.That(success.Quantity, Is.EqualTo(1));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(1));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(1));
        Assert.That(npc.Money, Is.EqualTo(success.TotalPrice));
    }

    [Test]
    public void TransactionReceiptIsASnapshotAndNotAReferenceToMarketState()
    {
        ItemData item = SimulationTestFactory.CreateItem("snapshot-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("market-city", "market-location", SimulationTestFactory.CreateMarketItem(item, 10, 10));
        NpcRuntime npc = CreateNpc("buyer", 100f);

        EconomyTransactionResult receipt = new EconomyTransactionService().TryExecuteOpenMarketPurchase(npc, city.Market, item, 1);
        float capturedUnitPrice = receipt.UnitPrice;
        float capturedTotalPrice = receipt.TotalPrice;

        city.Market.UpdatePrices();
        city.Market.AddStock(item, 100);

        Assert.That(receipt.ItemDefinitionId, Is.EqualTo("snapshot-item"));
        Assert.That(receipt.UnitPrice, Is.EqualTo(capturedUnitPrice));
        Assert.That(receipt.TotalPrice, Is.EqualTo(capturedTotalPrice));
        Assert.That(receipt.Quantity, Is.EqualTo(1));
    }

    [Test]
    public void CrimeStealTransfersMoneyThroughTheTransactionBoundary()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("crime-city", "crime-location");
        NpcRuntime thief = CreateNpc("thief", 0f, city);
        NpcRuntime target = CreateNpc("target", 50f, city);
        NpcStatusData free = SimulationTestFactory.CreateStatus("free");
        NpcStatusData wanted = SimulationTestFactory.CreateStatus("wanted");
        NpcStatusData arrested = SimulationTestFactory.CreateStatus("arrested");
        NpcStatusData hidden = SimulationTestFactory.CreateStatus("hidden");
        JusticeSystem justice = new JusticeSystem(free, wanted, arrested, hidden);
        CrimeSystem crime = new CrimeSystem(justice, null, hidden);
        NpcActionData action = SimulationTestFactory.CreateAction("steal", NpcActionType.Steal, NpcActionCategory.Crime);
        action.crimeSettings.amount = 20;

        NpcActionResult result = crime.TryExecuteAction(thief, new NpcActionRuntime(action, target, 20));

        Assert.That(result.Success, Is.True);
        Assert.That(thief.Money, Is.EqualTo(20f));
        Assert.That(target.Money, Is.EqualTo(30f));
        Assert.That(thief.Money + target.Money, Is.EqualTo(50f));
    }

    [Test]
    public void TravelChargeUsesExplicitSinkAndPreservesFailureState()
    {
        NpcRuntime npc = CreateNpc("traveler", 5f);
        EconomyTransactionService service = new EconomyTransactionService();

        EconomyTransactionResult failed = service.TryChargeTravel(npc, 10f);

        Assert.That(failed.Success, Is.False);
        Assert.That(failed.MoneyEffect, Is.EqualTo(MoneyEffect.ExplicitSink));
        Assert.That(npc.Money, Is.EqualTo(5f));

        EconomyTransactionResult success = service.TryChargeTravel(npc, 5f);

        Assert.That(success.Success, Is.True);
        Assert.That(success.TransactionType, Is.EqualTo(EconomyTransactionType.TravelCharge));
        Assert.That(npc.Money, Is.EqualTo(0f));
    }

    [Test]
    public void GroupTravelChargePrevalidatesAllMembersBeforeAnyDebit()
    {
        NpcRuntime first = CreateNpc("traveler-one", 20f);
        NpcRuntime second = CreateNpc("traveler-two", 5f);
        EconomyTransactionResult result = new EconomyTransactionService().TryChargeTravelGroup(new[] { first, second }, 10f);

        Assert.That(result.Success, Is.False);
        Assert.That(first.Money, Is.EqualTo(20f));
        Assert.That(second.Money, Is.EqualTo(5f));
        Assert.That(result.ParticipantRuntimeIds, Has.Count.EqualTo(2));
    }

    private static NpcRuntime CreateNpc(string id, float money, CityRuntime city = null)
    {
        return new NpcRuntime(id, SimulationTestFactory.CreateNpc(id), city, money);
    }
}
