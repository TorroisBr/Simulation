using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
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
    public void PartialSelectedCrossCityBuyFaultsBoundP12RuntimeAdmission()
    {
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, Array.Empty<NpcRuntime>(), economyEnabled: false);
        typeof(SimulationRuntime).GetField("npcRosterCensusProtocol",
            BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(runtime, new ContinuationCensusProtocol());
        EconomyTransactionService service = new EconomyTransactionService();
        service.BindP12CensusRuntime(runtime);

        service.FaultSelectedCrossCityBuyAdmissionIfPartial(new DailyV1SelectedCrossCityBuyResult(
            succeeded: false,
            failure: DailyV1SelectedCrossCityBuyFailure.PostPurchaseOwnerCommit,
            partialCommit: true));

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [TestCase("origin-city")]
    [TestCase("origin-market")]
    [TestCase("target-city")]
    public void CrossCityBuyAdapterRejectsProjectionMismatchBeforeOwnerWrites(string mismatch)
    {
        SelectedCrossCityAdapterFixture fixture = new SelectedCrossCityAdapterFixture();
        float balanceBefore = fixture.Merchant.Money;
        int inventoryBefore = fixture.Merchant.Inventory.GetSelectedAmount(fixture.ItemNode);
        long inventoryRevisionBefore = fixture.Merchant.Inventory.SelectedDailyV1Owner.Revision;
        long accountRevisionBefore = fixture.Merchant.MoneyAccount.Revision;
        int stockBefore = fixture.OriginOwner.Market.GetAmount(fixture.ItemNode);
        long marketRevisionBefore = fixture.OriginOwner.Market.Revision;
        long merchantPlanRevisionBefore = fixture.MerchantOwner.MerchantTradePlan.Revision;
        long travelPlanRevisionBefore = fixture.MerchantOwner.TravelPlan.Revision;
        CityRuntime targetProjection = fixture.DestinationProjection;
        if (mismatch == "origin-city")
        {
            CityRuntime unboundOriginAlias = CityRuntime.CreateSelectedDailyV1(
                fixture.OriginOwner.RuntimeId, fixture.OriginOwner.Definition,
                fixture.Definitions, fixture.Bindings,
                new SpatialLocationRuntime(fixture.OriginOwner.LocationRuntimeId));
            typeof(NpcRuntime).GetField("currentCity", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(fixture.Merchant, unboundOriginAlias);
        }
        else if (mismatch == "origin-market")
        {
            MarketRuntime unboundMarketAlias = new MarketRuntime(
                fixture.OriginOwner.Definition, fixture.Definitions, null);
            typeof(CityRuntime).GetField("market", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(fixture.OriginProjection, unboundMarketAlias);
        }
        else
        {
            targetProjection = CityRuntime.CreateSelectedDailyV1(
                fixture.DestinationOwner.RuntimeId, fixture.DestinationOwner.Definition,
                fixture.Definitions, fixture.Bindings,
                new SpatialLocationRuntime(fixture.DestinationOwner.LocationRuntimeId));
        }

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteSelectedCrossCityBuy(
            fixture.Merchant, fixture.Payload, targetProjection);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(fixture.Merchant.Money, Is.EqualTo(balanceBefore));
        Assert.That(fixture.Merchant.Inventory.GetSelectedAmount(fixture.ItemNode), Is.EqualTo(inventoryBefore));
        Assert.That(fixture.Merchant.Inventory.SelectedDailyV1Owner.Revision, Is.EqualTo(inventoryRevisionBefore));
        Assert.That(fixture.Merchant.MoneyAccount.Revision, Is.EqualTo(accountRevisionBefore));
        Assert.That(fixture.OriginOwner.Market.GetAmount(fixture.ItemNode), Is.EqualTo(stockBefore));
        Assert.That(fixture.OriginOwner.Market.Revision, Is.EqualTo(marketRevisionBefore));
        Assert.That(fixture.MerchantOwner.MerchantTradePlan.Revision, Is.EqualTo(merchantPlanRevisionBefore));
        Assert.That(fixture.MerchantOwner.TravelPlan.Revision, Is.EqualTo(travelPlanRevisionBefore));
    }

    [Test]
    public void MoneyAccount_CanCreditRejectsOverflowWithoutMutation()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(float.MaxValue);

        Assert.That(account.CanCredit(float.MaxValue), Is.False);
        Assert.That(account.Balance, Is.EqualTo(float.MaxValue));
    }

    [Test]
    public void KeyedMarketSale_ReplaysReceiptAndRejectsFingerprintCollision()
    {
        ItemData item = SimulationTestFactory.CreateItem("keyed-sale-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("keyed-sale-city", "keyed-sale-location");
        NpcRuntime seller = CreateNpc("keyed-sale-seller", 0f, city);
        seller.Inventory.AddItem(item, 3, 2f);
        EconomyTransactionService service = new EconomyTransactionService();

        OwnerSectionCensusWitness emptyCensus = service.GetKeyedSaleReceiptCensus();
        Assert.That(emptyCensus.SectionId, Is.EqualTo(EconomyTransactionService.KeyedSaleReceiptSectionId));
        Assert.That(emptyCensus.SchemaVersion, Is.EqualTo(EconomyTransactionService.KeyedSaleReceiptSectionSchemaVersion));
        Assert.That(emptyCensus.Cardinality, Is.Zero);
        Assert.That(emptyCensus.Revision, Is.Zero);
        Assert.That(service.GetKeyedSaleReceiptCensus().OwnerInstanceIdentity, Is.SameAs(emptyCensus.OwnerInstanceIdentity));

        KeyedSaleReceipt first = service.TryExecuteKeyedMarketSale("proposal-1", "fingerprint-1", "input-1", "request-1", "person-1", "sell", 1, "profile", "tick-1", "site-1", seller, city.Market, item, 2);
        Assert.That(first.Outcome, Is.EqualTo(KeyedSaleOutcome.Committed));
        Assert.That(first.Result.Quantity, Is.EqualTo(2));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(1));
        Assert.That(seller.Money, Is.EqualTo(first.Result.TotalPrice));
        OwnerSectionCensusWitness committedCensus = service.GetKeyedSaleReceiptCensus();
        Assert.That(committedCensus.OwnerInstanceIdentity, Is.SameAs(emptyCensus.OwnerInstanceIdentity));
        Assert.That(committedCensus.Cardinality, Is.EqualTo(1));
        Assert.That(committedCensus.Revision, Is.EqualTo(1));

        seller.Inventory.AddItem(item, 1, 5f);
        CityRuntime movedToCity = SimulationTestFactory.CreateCity("keyed-sale-moved-city", "keyed-sale-moved-location");
        NpcRuntime rematerializedSeller = CreateNpc("keyed-sale-rematerialized-seller", 0f, movedToCity);
        KeyedSaleReceipt replay = service.TryExecuteKeyedMarketSale("proposal-1", "fingerprint-1", "input-1", "request-1", "person-1", "sell", 1, "profile", "tick-1", "site-1", rematerializedSeller, city.Market, item, 2);
        Assert.That(replay, Is.SameAs(first));
        Assert.That(service.FindKeyedSaleReceipt("proposal-1", "fingerprint-1", "input-1", "request-1", "person-1", "sell", 1, "profile", "tick-1", "site-1", seller, city.Market, item, 2), Is.SameAs(first));
        Assert.That(service.FindKeyedSaleReceipt("proposal-1", "fingerprint-1", "input-1", "request-1", "person-1", "sell", 1, "profile", "tick-1", "site-1", null, null, item, 2), Is.SameAs(first));
        Assert.That(service.FindKeyedSaleReceipt("proposal-1", "fingerprint-1", "input-1", "request-1", "person-1", "sell", 1, "profile", "tick-1", "site-1", item.DefinitionId, 2), Is.SameAs(first));
        Assert.That(service.FindKeyedSaleReceipt("proposal-1", "fingerprint-1", "input-1", "request-1", "person-1", "sell", 1, "profile", "tick-1", "site-1", seller, city.Market, item, 1).Outcome, Is.EqualTo(KeyedSaleOutcome.FingerprintCollision));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(rematerializedSeller.Inventory.GetAmount(item), Is.EqualTo(0));
        KeyedSaleReceipt collision = service.TryExecuteKeyedMarketSale("proposal-1", "fingerprint-1", "input-1", "request-1", "person-1", "sell", 1, "profile", "tick-1", "site-1", seller, city.Market, item, 1);
        Assert.That(collision.Outcome, Is.EqualTo(KeyedSaleOutcome.FingerprintCollision));
        OwnerSectionCensusWitness replayCensus = service.GetKeyedSaleReceiptCensus();
        Assert.That(replayCensus.OwnerInstanceIdentity, Is.SameAs(committedCensus.OwnerInstanceIdentity));
        Assert.That(replayCensus.Cardinality, Is.EqualTo(committedCensus.Cardinality));
        Assert.That(replayCensus.Revision, Is.EqualTo(committedCensus.Revision));
        Assert.That(first.Snapshot.SellerInventory, Is.EqualTo(3));
        Assert.That(first.Snapshot.MarketStock, Is.EqualTo(0));
        Assert.That(first.Snapshot.SellerRuntimeId, Is.EqualTo(seller.RuntimeId));
        Assert.That(first.Snapshot.SellerCityRuntimeId, Is.EqualTo(city.RuntimeId));
    }

    [Test]
    public void KeyedMarketSale_CanonicalFingerprintCoversEveryImmutableRequestArgument()
    {
        ItemData item = SimulationTestFactory.CreateItem("fingerprint-item", 10f);
        ItemData otherItem = SimulationTestFactory.CreateItem("fingerprint-other-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("fingerprint-city", "fingerprint-location");
        CityRuntime otherCity = SimulationTestFactory.CreateCity("fingerprint-other-city", "fingerprint-other-location");
        NpcRuntime seller = CreateNpc("fingerprint-seller", 0f, city);
        NpcRuntime otherSeller = CreateNpc("fingerprint-other-seller", 0f, otherCity);
        seller.Inventory.AddItem(item, 3);
        otherSeller.Inventory.AddItem(otherItem, 3);
        EconomyTransactionService service = new EconomyTransactionService();
        KeyedSaleReceipt first = service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 1);
        Assert.That(first.Outcome, Is.EqualTo(KeyedSaleOutcome.Committed));

        Func<KeyedSaleReceipt>[] changed =
        {
            () => service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "other-input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 1),
            () => service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "input", "other-request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 1),
            () => service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "input", "request", "other-person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 1),
            () => service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "input", "request", "person", "other-action", 1, "profile", "tick", "site", seller, city.Market, item, 1),
            () => service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "input", "request", "person", "sell", 2, "profile", "tick", "site", seller, city.Market, item, 1),
            () => service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "input", "request", "person", "sell", 1, "other-profile", "tick", "site", seller, city.Market, item, 1),
            () => service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "input", "request", "person", "sell", 1, "profile", "other-tick", "site", seller, city.Market, item, 1),
            () => service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "input", "request", "person", "sell", 1, "profile", "tick", "other-site", seller, city.Market, item, 1),
            () => service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, otherItem, 1),
            () => service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 2)
        };
        foreach (Func<KeyedSaleReceipt> retry in changed)
            Assert.That(retry().Outcome, Is.EqualTo(KeyedSaleOutcome.FingerprintCollision));
        Assert.That(service.TryExecuteKeyedMarketSale("fingerprint-proposal", "same-caller-fp", "input", "request", "person", "sell", 1, "profile", "tick", "site", otherSeller, otherCity.Market, item, 1), Is.SameAs(first));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(otherSeller.Inventory.GetAmount(otherItem), Is.EqualTo(3));
    }

    [Test]
    public void KeyedMarketSale_RetainsTerminalRejectionAndOwnerRevisionsAdvance()
    {
        ItemData item = SimulationTestFactory.CreateItem("keyed-reject-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("keyed-reject-city", "keyed-reject-location");
        NpcRuntime seller = CreateNpc("keyed-reject-seller", 0f, city);
        EconomyTransactionService service = new EconomyTransactionService();
        OwnerSectionCensusWitness emptyCensus = service.GetKeyedSaleReceiptCensus();
        KeyedSaleReceipt first = service.TryExecuteKeyedMarketSale("proposal-reject", "fp", "input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 1);
        Assert.That(first.Outcome, Is.EqualTo(KeyedSaleOutcome.TerminalRejection));
        OwnerSectionCensusWitness retainedFailureCensus = service.GetKeyedSaleReceiptCensus();
        Assert.That(retainedFailureCensus.OwnerInstanceIdentity, Is.SameAs(emptyCensus.OwnerInstanceIdentity));
        Assert.That(retainedFailureCensus.Cardinality, Is.EqualTo(1));
        Assert.That(retainedFailureCensus.Revision, Is.EqualTo(1));

        long moneyRevision = seller.MoneyAccount.Revision;
        long inventoryRevision = seller.Inventory.Revision;
        seller.MoneyAccount.TryCredit(1f);
        seller.Inventory.AddItem(item, 1);
        Assert.That(seller.MoneyAccount.Revision, Is.EqualTo(moneyRevision + 1));
        Assert.That(seller.Inventory.Revision, Is.EqualTo(inventoryRevision + 1));
        KeyedSaleReceipt retry = service.TryExecuteKeyedMarketSale("proposal-reject", "fp", "input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 1);
        KeyedSaleReceipt replay = retry;
        Assert.That(replay, Is.SameAs(first));
        Assert.That(replay.Result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InsufficientInventory));
        OwnerSectionCensusWitness replayCensus = service.GetKeyedSaleReceiptCensus();
        Assert.That(replayCensus.OwnerInstanceIdentity, Is.SameAs(retainedFailureCensus.OwnerInstanceIdentity));
        Assert.That(replayCensus.Cardinality, Is.EqualTo(retainedFailureCensus.Cardinality));
        Assert.That(replayCensus.Revision, Is.EqualTo(retainedFailureCensus.Revision));
    }

    [Test]
    public void KeyedMarketSale_PreparedAccountBackedInstallTransfersAllEffects()
    {
        ItemData item = SimulationTestFactory.CreateItem("keyed-account-sale-item", 10f);
        CityRuntime city = CreateAccountBackedCity("keyed-account-sale-city", item, 5, 30f);
        NpcRuntime seller = CreateNpc("keyed-account-sale-seller", 2f, city);
        seller.Inventory.AddItem(item, 2, 3f);
        EconomyTransactionService service = new EconomyTransactionService();
        long marketRevision = city.Market.Revision;
        long inventoryRevision = seller.Inventory.Revision;
        long sellerMoneyRevision = seller.MoneyAccount.Revision;
        long counterpartyMoneyRevision = city.Market.Counterparty.MoneyAccount.Revision;

        KeyedSaleReceipt receipt = service.TryExecuteKeyedMarketSale("proposal-account", "fp-account", "input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 2);

        Assert.That(receipt.Outcome, Is.EqualTo(KeyedSaleOutcome.Committed));
        Assert.That(seller.Money, Is.EqualTo(22f));
        Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(10f));
        Assert.That(seller.Inventory.GetAmount(item), Is.Zero);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(7));
        Assert.That(city.Market.Revision, Is.EqualTo(marketRevision + 1));
        Assert.That(seller.Inventory.Revision, Is.EqualTo(inventoryRevision + 1));
        Assert.That(seller.MoneyAccount.Revision, Is.EqualTo(sellerMoneyRevision + 1));
        Assert.That(city.Market.Counterparty.MoneyAccount.Revision, Is.EqualTo(counterpartyMoneyRevision + 1));
    }

    [Test]
    public void KeyedMarketSale_PreservesFirstMatchingDuplicateMarketRowSemantics()
    {
        ItemData item = SimulationTestFactory.CreateItem("duplicate-row-item", 10f);
        MarketRuntime market = new MarketRuntime(new List<MarketItemConfig>
        {
            SimulationTestFactory.CreateMarketItem(item, 5, 5),
            SimulationTestFactory.CreateMarketItem(item, 8, 8)
        });
        NpcRuntime seller = CreateNpc("duplicate-row-seller", 0f);
        seller.Inventory.AddItem(item, 2);

        KeyedSaleReceipt receipt = new EconomyTransactionService().TryExecuteKeyedMarketSale(
            "duplicate-proposal", "duplicate-fp", "input", "request", "person", "sell", 1,
            "profile", "tick", "site", seller, market, item, 2);

        Assert.That(receipt.Outcome, Is.EqualTo(KeyedSaleOutcome.Committed));
        Assert.That(market.Items[0].Amount, Is.EqualTo(7));
        Assert.That(market.Items[1].Amount, Is.EqualTo(8));
        Assert.That(market.GetAmount(item), Is.EqualTo(7));
    }

    [Test]
    public void KeyedMarketSale_PreservesCachedPricesUntilPriceRefresh()
    {
        ItemData soldItem = SimulationTestFactory.CreateItem("cached-price-sold-item", 10f);
        ItemData untouchedItem = SimulationTestFactory.CreateItem("cached-price-untouched-item", 10f);
        MarketRuntime market = new MarketRuntime(new List<MarketItemConfig>
        {
            SimulationTestFactory.CreateMarketItem(soldItem, 5, 5),
            SimulationTestFactory.CreateMarketItem(untouchedItem, 10, 10)
        });
        NpcRuntime seller = CreateNpc("cached-price-seller", 0f);
        seller.Inventory.AddItem(soldItem, 1);
        float cachedSalePrice = market.GetPrice(soldItem);
        float cachedPrice = market.GetPrice(untouchedItem);
        soldItem.basePrice = 50f;
        untouchedItem.basePrice = 50f;

        Assert.That(market.GetItem(soldItem).CurrentPrice, Is.EqualTo(cachedSalePrice));

        KeyedSaleReceipt receipt = new EconomyTransactionService().TryExecuteKeyedMarketSale(
            "cached-price-proposal", "cached-price-fp", "input", "request", "person", "sell", 1,
            "profile", "tick", "site", seller, market, soldItem, 1);

        Assert.That(receipt.Outcome, Is.EqualTo(KeyedSaleOutcome.Committed));
        Assert.That(receipt.Result.UnitPrice, Is.EqualTo(cachedSalePrice));
        Assert.That(market.GetPrice(untouchedItem), Is.EqualTo(cachedPrice));
        Assert.That(market.GetPrice(soldItem), Is.Not.EqualTo(0f));
    }

    [Test]
    public void KeyedMarketSale_PreflightRevisionExhaustionLeavesAllOwnersUnchangedAndCanRetry()
    {
        ItemData item = SimulationTestFactory.CreateItem("preflight-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("preflight-city", "preflight-location");
        NpcRuntime seller = CreateNpc("preflight-seller", 0f, city);
        seller.Inventory.AddItem(item, 2, 2f);
        EconomyTransactionService service = new EconomyTransactionService();
        OwnerSectionCensusWitness emptyCensus = service.GetKeyedSaleReceiptCensus();
        long originalSellerRevision = seller.MoneyAccount.Revision;
        SetRevision(seller.MoneyAccount, long.MaxValue);
        float money = seller.Money;
        int inventory = seller.Inventory.GetAmount(item);
        int stock = city.Market.GetAmount(item);

        KeyedSaleReceipt rejected = service.TryExecuteKeyedMarketSale("preflight-proposal", "preflight-fp", "input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 1);

        Assert.That(rejected.Outcome, Is.EqualTo(KeyedSaleOutcome.ProvenNoInstall));
        Assert.That(rejected.Result.Success, Is.False);
        Assert.That(rejected.Result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(seller.Money, Is.EqualTo(money));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(inventory));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(stock));
        OwnerSectionCensusWitness retainedPreflightCensus = service.GetKeyedSaleReceiptCensus();
        Assert.That(retainedPreflightCensus.OwnerInstanceIdentity, Is.SameAs(emptyCensus.OwnerInstanceIdentity));
        Assert.That(retainedPreflightCensus.Cardinality, Is.EqualTo(1));
        Assert.That(retainedPreflightCensus.Revision, Is.EqualTo(1));
        SetRevision(seller.MoneyAccount, originalSellerRevision);
        KeyedSaleReceipt retried = service.TryExecuteKeyedMarketSale("preflight-proposal", "preflight-fp", "input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 1);
        Assert.That(retried.Outcome, Is.EqualTo(KeyedSaleOutcome.Committed));
        OwnerSectionCensusWitness replacedReceiptCensus = service.GetKeyedSaleReceiptCensus();
        Assert.That(replacedReceiptCensus.OwnerInstanceIdentity, Is.SameAs(retainedPreflightCensus.OwnerInstanceIdentity));
        Assert.That(replacedReceiptCensus.Cardinality, Is.EqualTo(retainedPreflightCensus.Cardinality));
        Assert.That(replacedReceiptCensus.Revision, Is.EqualTo(retainedPreflightCensus.Revision + 1));
    }

    [Test]
    public void KeyedMarketSale_EachParticipatingAuthorityRevisionInvalidatesPreflight()
    {
        string[] owners = { "inventory", "market", "seller-money", "counterparty-money" };
        foreach (string owner in owners)
        {
            ItemData item = SimulationTestFactory.CreateItem("revision-race-item-" + owner, 10f);
            CityRuntime city = owner == "counterparty-money"
                ? CreateAccountBackedCity("revision-race-city-" + owner, item, 4, 50f)
                : SimulationTestFactory.CreateCity("revision-race-city-" + owner, "revision-race-location-" + owner);
            NpcRuntime seller = CreateNpc("revision-race-seller-" + owner, 1f, city);
            seller.Inventory.AddItem(item, 2);
            object revisionOwner = owner == "inventory" ? (object)seller.Inventory
                : owner == "market" ? city.Market
                : owner == "counterparty-money" ? city.Market.Counterparty.MoneyAccount
                : seller.MoneyAccount;
            SetRevision(revisionOwner, long.MaxValue);
            float sellerMoney = seller.Money;
            float counterpartyMoney = city.Market.Counterparty.MoneyAccount != null ? city.Market.Counterparty.MoneyAccount.Balance : 0f;
            int inventory = seller.Inventory.GetAmount(item);
            int stock = city.Market.GetAmount(item);

            KeyedSaleReceipt receipt = new EconomyTransactionService().TryExecuteKeyedMarketSale(
                "revision-race-proposal-" + owner, "fp", "input", "request", "person", "sell", 1,
                "profile", "tick", "site", seller, city.Market, item, 1);

            Assert.That(receipt.Outcome, Is.EqualTo(KeyedSaleOutcome.ProvenNoInstall), owner);
            Assert.That(seller.Money, Is.EqualTo(sellerMoney), owner);
            Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(inventory), owner);
            Assert.That(city.Market.GetAmount(item), Is.EqualTo(stock), owner);
            if (city.Market.Counterparty.MoneyAccount != null)
                Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(counterpartyMoney), owner);
        }
    }

    [Test]
    public void KeyedMarketSale_AccountCapacityOverflowRejectsBeforeAnyInstall()
    {
        ItemData item = SimulationTestFactory.CreateItem("keyed-overflow-item", 10f);
        CityRuntime city = CreateAccountBackedCity("keyed-overflow-city", item, int.MaxValue, 100f);
        NpcRuntime seller = CreateNpc("keyed-overflow-seller", 0f, city);
        seller.Inventory.AddItem(item, 1);
        float balance = seller.Money;
        int inventory = seller.Inventory.GetAmount(item);
        int stock = city.Market.GetAmount(item);

        KeyedSaleReceipt receipt = new EconomyTransactionService().TryExecuteKeyedMarketSale("overflow-proposal", "overflow-fp", "input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 1);

        Assert.That(receipt.Outcome, Is.EqualTo(KeyedSaleOutcome.TerminalRejection));
        Assert.That(receipt.Result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.MarketCapacity));
        Assert.That(seller.Money, Is.EqualTo(balance));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(inventory));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(stock));
        Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(100f));
    }

    [Test]
    public void KeyedMarketSale_SnapshotUsesNormalizedPriceForCounterpartyAffordability()
    {
        ItemData item = SimulationTestFactory.CreateItem("normalized-snapshot-price-item", 0.01f);
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity(
            "normalized-snapshot-price-city", "normalized-snapshot-price-location", 0.007f,
            SimulationTestFactory.CreateMarketItem(item, 20, 10));
        NpcRuntime seller = CreateNpc("normalized-snapshot-price-seller", 0f, city);
        seller.Inventory.AddItem(item, 1);
        Assert.That(city.Market.GetPrice(item), Is.EqualTo(0.005f).Within(0.00001f));

        KeyedSaleReceipt receipt = new EconomyTransactionService().TryExecuteKeyedMarketSale(
            "normalized-price-proposal", "normalized-price-fp", "input", "request", "person", "sell", 1,
            "profile", "tick", "site", seller, city.Market, item, 1);

        Assert.That(receipt.Outcome, Is.EqualTo(KeyedSaleOutcome.TerminalRejection));
        Assert.That(receipt.Result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InsufficientCounterpartyFunds));
        Assert.That(receipt.Snapshot.EffectiveQuantity, Is.Zero);
        Assert.That(receipt.Snapshot.CounterpartyCouldPay, Is.False);
    }

    [Test]
    public void KeyedMarketSale_UnresolvedReceiptReplaysWithoutRetry()
    {
        ItemData item = SimulationTestFactory.CreateItem("unresolved-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("unresolved-city", "unresolved-location");
        NpcRuntime seller = CreateNpc("unresolved-seller", 0f, city);
        seller.Inventory.AddItem(item, 1);
        EconomyTransactionService service = new EconomyTransactionService();
        SetRevision(seller.MoneyAccount, long.MaxValue);
        KeyedSaleReceipt provenNoInstall = service.TryExecuteKeyedMarketSale("unresolved-proposal", "unresolved-fp", "input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 1);
        Assert.That(provenNoInstall.Outcome, Is.EqualTo(KeyedSaleOutcome.ProvenNoInstall));
        FieldInfo tableField = typeof(EconomyTransactionService).GetField("keyedSaleReceipts", BindingFlags.Instance | BindingFlags.NonPublic);
        List<KeyedSaleReceipt> receipts = (List<KeyedSaleReceipt>)tableField.GetValue(service);
        MethodInfo unresolvedFactory = typeof(KeyedSaleReceipt).GetMethod("Unresolved", BindingFlags.Static | BindingFlags.NonPublic);
        KeyedSaleReceipt unresolved = (KeyedSaleReceipt)unresolvedFactory.Invoke(null, new object[] { provenNoInstall.ProposalId, provenNoInstall.Fingerprint, "unresolved-fp", provenNoInstall.Snapshot, provenNoInstall.Result });
        receipts[0] = unresolved;
        float moneyAfterPartialEffect = seller.Money;
        int inventoryAfterPartialEffect = seller.Inventory.GetAmount(item);
        int stockAfterPartialEffect = city.Market.GetAmount(item);

        KeyedSaleReceipt replay = service.TryExecuteKeyedMarketSale("unresolved-proposal", "unresolved-fp", "input", "request", "person", "sell", 1, "profile", "tick", "site", seller, city.Market, item, 1);

        Assert.That(replay, Is.SameAs(unresolved));
        Assert.That(replay.Outcome, Is.EqualTo(KeyedSaleOutcome.Unresolved));
        Assert.That(seller.Money, Is.EqualTo(moneyAfterPartialEffect));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(inventoryAfterPartialEffect));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(stockAfterPartialEffect));
    }

    [Test]
    public void EconomyOwnerViewsAreReadOnlyAndMarketRevisionTracksStockAndPrice()
    {
        ItemData item = SimulationTestFactory.CreateItem("revision-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity("revision-city", "revision-location", SimulationTestFactory.CreateMarketItem(item, 2, 4));
        MarketRuntime market = city.Market;
        Assert.That(market.Items, Is.Not.InstanceOf<System.Collections.Generic.List<MarketItemRuntime>>());

        long marketRevision = market.Revision;
        market.AddStock(item, 1);
        Assert.That(market.Revision, Is.EqualTo(marketRevision + 1));
        marketRevision = market.Revision;
        item.basePrice = 20f;
        market.UpdatePrices();
        Assert.That(market.Revision, Is.EqualTo(marketRevision + 1));

        NpcRuntime npc = CreateNpc("readonly-inventory", 0f);
        npc.Inventory.AddItem(item, 1);
        Assert.That(npc.Inventory.Items, Is.Not.InstanceOf<System.Collections.Generic.List<InventoryItemRuntime>>());
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(1));
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
    public void MoneyTransfer_RejectsUnrepresentablePositiveMutationWithoutSuccess()
    {
        EconomyTransactionService service = new EconomyTransactionService();
        MoneyAccountRuntime sourceWithUnrepresentableDebit = new MoneyAccountRuntime(float.MaxValue);
        MoneyAccountRuntime destinationWithRoom = new MoneyAccountRuntime(0f);

        EconomyTransactionResult debitResult = service.TryTransferMoney(
            sourceWithUnrepresentableDebit,
            destinationWithRoom,
            10f);

        Assert.That(debitResult.Success, Is.False);
        Assert.That(debitResult.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InsufficientFunds));
        Assert.That(sourceWithUnrepresentableDebit.Balance, Is.EqualTo(float.MaxValue));
        Assert.That(destinationWithRoom.Balance, Is.EqualTo(0f));

        MoneyAccountRuntime sourceWithRoom = new MoneyAccountRuntime(100f);
        MoneyAccountRuntime destinationWithUnrepresentableCredit = new MoneyAccountRuntime(float.MaxValue);

        EconomyTransactionResult creditResult = service.TryTransferMoney(
            sourceWithRoom,
            destinationWithUnrepresentableCredit,
            10f);

        Assert.That(creditResult.Success, Is.False);
        Assert.That(creditResult.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.CreditRejected));
        Assert.That(sourceWithRoom.Balance, Is.EqualTo(100f));
        Assert.That(destinationWithUnrepresentableCredit.Balance, Is.EqualTo(float.MaxValue));
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

    [Test]
    public void CityRuntime_DefaultMarketCounterpartyIsOpen()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-open", "location-open");

        Assert.That(city.Market.Counterparty.LiquidityMode, Is.EqualTo(MarketLiquidityMode.Open));
        Assert.That(city.Market.Counterparty.MoneyAccount, Is.Null);
        Assert.That(city.Market.Counterparty.CounterpartyRuntimeId, Is.EqualTo(city.RuntimeId));

        MarketRuntime standaloneMarket = new MarketRuntime();
        Assert.That(standaloneMarket.Counterparty.LiquidityMode, Is.EqualTo(MarketLiquidityMode.Open));
        Assert.That(standaloneMarket.Counterparty.MoneyAccount, Is.Null);
        Assert.That(standaloneMarket.Counterparty.CounterpartyRuntimeId, Is.Null.Or.Empty);
    }

    [Test]
    public void GenericOpenMarketPurchaseAndSalePreserveExplicitEffects()
    {
        ItemData item = SimulationTestFactory.CreateItem("open-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "city-open",
            "location-open",
            SimulationTestFactory.CreateMarketItem(item, 10, 10));
        NpcRuntime buyer = CreateNpc("open-buyer", 20f);
        NpcRuntime seller = CreateNpc("open-seller", 0f);
        seller.Inventory.AddItem(item, 1, 4f);
        EconomyTransactionService service = new EconomyTransactionService();

        EconomyTransactionResult purchase = service.TryExecuteMarketPurchase(buyer, city.Market, item, 1);
        EconomyTransactionResult sale = service.TryExecuteMarketSale(seller, city.Market, item, 1);

        Assert.That(purchase.Success, Is.True);
        Assert.That(purchase.MoneyEffect, Is.EqualTo(MoneyEffect.ExplicitSink));
        Assert.That(sale.Success, Is.True);
        Assert.That(sale.MoneyEffect, Is.EqualTo(MoneyEffect.ExplicitSource));
        Assert.That(city.Market.Counterparty.MoneyAccount, Is.Null);
    }

    [Test]
    public void SelectedDailyV1PurchaseAndSaleUseCanonicalItemNodeWithoutMarketAssetProjection()
    {
        ItemData itemAsset = SimulationTestFactory.CreateItem("selected-typed-trade-item", 10f);
        ItemData itemAbsentFromMarket = SimulationTestFactory.CreateItem("selected-typed-trade-unlisted-item", 12f);
        CityData cityAsset = SimulationTestFactory.CreateCityData("selected-typed-trade-city",
            SimulationTestFactory.CreateMarketItem(itemAsset, 8, 8));
        NpcData npcAsset = SimulationTestFactory.CreateNpc("selected-typed-trade-npc");
        NpcSimulationConfig npcConfig = new NpcSimulationConfig
        {
            npc = npcAsset,
            startingCity = cityAsset,
            initialMoney = 50f
        };
        npcConfig.InitialInventory.Add(new NpcInitialInventoryItemConfig
        {
            item = itemAbsentFromMarket,
            amount = 1,
            averageUnitCost = 4f
        });
        SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
        authoring.simulationName = "Selected typed trade fixture";
        authoring.cities.Add(cityAsset);
        authoring.npcs.Add(npcConfig);

        SimulationConfigurationResolutionResult resolution = SimulationConfigurationResolver.Resolve(
            null, null, authoring.CreateConfigurationOverrides());
        Assert.That(resolution.IsValid, Is.True, string.Join("; ", resolution.Validation.Errors));
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.calendar, out string calendarDiagnostic);
        Assert.That(string.IsNullOrEmpty(calendarDiagnostic), Is.True, calendarDiagnostic);
        Assert.That(DailyV1DefinitionAdapter.TryCreate(authoring, resolution.Configuration, calendar,
            SimulationGenesisPipeline.ResolveStageOrder(includeAuthoredGeography: true), "axial-hex-v1", "q-then-r",
            out DailyV1EffectiveDefinitions definitions,
            out DailyV1DefinitionAdapter.SelectedBindings bindings, out string diagnostic), Is.True, diagnostic);
        Assert.That(definitions.Items.TryGetValue(itemAsset.DefinitionId, out DailyV1ItemDefinition item), Is.True);
        Assert.That(definitions.Items.TryGetValue(itemAbsentFromMarket.DefinitionId,
            out DailyV1ItemDefinition unlistedItem), Is.True);
        Assert.That(definitions.Cities.TryGetValue(cityAsset.DefinitionId, out DailyV1CityDefinition city), Is.True);
        Assert.That(definitions.Npcs.TryGetValue(npcAsset.DefinitionId, out DailyV1NpcDefinition npcDefinition), Is.True);

        MarketRuntime market = new MarketRuntime(city, definitions, counterparty: null);
        NpcRuntime npc = NpcRuntime.CreateSelectedDailyV1(
            "runtime.selected-typed-trade", npcDefinition, definitions, bindings, null, 50f);
        Assert.That(npc.Inventory.TryAddSelectedItem(unlistedItem, 1, 4f), Is.True);
        Assert.That(npc.Inventory.SelectedDailyV1Owner.Definitions, Is.SameAs(definitions));
        Assert.That(market.SelectedDailyV1Owner.Definitions, Is.SameAs(definitions));
        Assert.That(market.Items, Is.Empty,
            "This selected Market has no Unity ItemData projection binding; typed operations must use the Core owner directly.");

        Assert.That(DailyV1DefinitionAdapter.TryCreate(authoring, resolution.Configuration, calendar,
            SimulationGenesisPipeline.ResolveStageOrder(includeAuthoredGeography: true), "axial-hex-v1", "q-then-r",
            out DailyV1EffectiveDefinitions lookalikeGraph, out _, out string lookalikeDiagnostic), Is.True,
            lookalikeDiagnostic);
        DailyV1ItemDefinition lookalikeItem = lookalikeGraph.Items[item.DefinitionId];
        float balanceBeforeLookalike = npc.Money;
        long inventoryRevisionBeforeLookalike = npc.Inventory.Revision;
        Assert.That(market.BuySelectedItem(npc, lookalikeItem, 1).FailureReason,
            Is.EqualTo(EconomyTransactionFailureReason.InvalidInput));
        Assert.That(npc.Money, Is.EqualTo(balanceBeforeLookalike));
        Assert.That(npc.Inventory.Revision, Is.EqualTo(inventoryRevisionBeforeLookalike));
        Assert.That(market.Revision, Is.Zero);

        EconomyTransactionResult purchase = market.BuySelectedItem(npc, item, 2);
        EconomyTransactionResult sale = market.SellSelectedItem(npc, item, 1);
        float expectedUnlistedSalePrice = Math.Max(0.01f, unlistedItem.Values.basePrice);
        float incorrectSynthesizedMarketPrice = MarketPriceCalculator.CalculateCurrentPrice(
            unlistedItem.Values.basePrice, 100, 0);
        Assert.That(Math.Abs(expectedUnlistedSalePrice - incorrectSynthesizedMarketPrice), Is.GreaterThan(0.001f),
            "The fixture must distinguish the Core owner fallback from a synthesized zero-stock market price.");
        EconomyTransactionResult unlistedSale = market.SellSelectedItem(npc, unlistedItem, 1);

        Assert.That(purchase.Success, Is.True, purchase.FailureReason.ToString());
        Assert.That(sale.Success, Is.True, sale.FailureReason.ToString());
        Assert.That(unlistedSale.Success, Is.True, unlistedSale.FailureReason.ToString());
        Assert.That(unlistedSale.UnitPrice, Is.EqualTo(expectedUnlistedSalePrice).Within(0.001f));
        Assert.That(purchase.ItemDefinitionId, Is.EqualTo(item.DefinitionId));
        Assert.That(sale.ItemDefinitionId, Is.EqualTo(item.DefinitionId));
        Assert.That(npc.Inventory.GetSelectedAmount(item), Is.EqualTo(1));
        Assert.That(npc.Inventory.GetSelectedAmount(unlistedItem), Is.Zero);
        Assert.That(market.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(7));
        Assert.That(market.SelectedDailyV1Owner.GetAmount(unlistedItem), Is.EqualTo(1));
        Assert.That(market.SelectedDailyV1Owner.Items[0].Item, Is.SameAs(item));
        Assert.That(market.SelectedDailyV1Owner.Items[1].Item, Is.SameAs(unlistedItem));
        Assert.That(npc.Inventory.Revision, Is.EqualTo(4));
        Assert.That(market.Revision, Is.EqualTo(3));
        Assert.That(npc.Money, Is.EqualTo(50f - purchase.TotalPrice + sale.TotalPrice + unlistedSale.TotalPrice).Within(0.001f));
    }

    [Test]
    public void P12SelectedMarketPurchaseAndSaleNotifyTheBoundRuntimeEpochOncePerOwnerCommit()
    {
        ItemData itemAsset = SimulationTestFactory.CreateItem("p12-selected-market-item", 10f);
        CityData cityAsset = SimulationTestFactory.CreateCityData("p12-selected-market-city",
            SimulationTestFactory.CreateMarketItem(itemAsset, 4, 4));
        NpcData npcAsset = SimulationTestFactory.CreateNpc("p12-selected-market-npc");
        SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
        authoring.Cities.Add(cityAsset);
        authoring.Npcs.Add(new NpcSimulationConfig
        {
            npc = npcAsset,
            startingCity = cityAsset,
            initialMoney = 50f
        });
        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: authoring.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.Calendar, out _);
        DailyV1EffectiveDefinitions definitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
            authoring, effective, calendar, out DailyV1DefinitionAdapter.SelectedBindings bindings);
        DailyV1ItemDefinition item = definitions.Items[itemAsset.DefinitionId];
        CityRuntime city = CityRuntime.CreateSelectedDailyV1(
            "p12-selected-market-city-runtime", definitions.Cities[cityAsset.DefinitionId], definitions,
            bindings, new SpatialLocationRuntime("p12-selected-market-location"));
        NpcRuntime buyer = NpcRuntime.CreateSelectedDailyV1(
            "p12-selected-market-buyer", definitions.Npcs[npcAsset.DefinitionId], definitions, bindings, city, 50f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), new[] { city }, new[] { buyer }, economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialAssessment), Is.True,
            initialAssessment.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long beforePurchase,
            out ContinuationCensusFailure beforePurchaseFailure), Is.True, beforePurchaseFailure.ToString());
        EconomyTransactionResult purchase = service.TryExecuteMarketPurchase(buyer, city.Market, item, 1);
        Assert.That(purchase.Success, Is.True, purchase.FailureReason.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterPurchase,
            out ContinuationCensusFailure afterPurchaseFailure), Is.True, afterPurchaseFailure.ToString());
        Assert.That(afterPurchase, Is.EqualTo(beforePurchase + 3),
            "A selected typed purchase commits the bound MoneyAccount, Market, and Inventory owners once each.");

        EconomyTransactionResult sale = service.TryExecuteMarketSale(buyer, city.Market, item, 1);
        Assert.That(sale.Success, Is.True, sale.FailureReason.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterSale,
            out ContinuationCensusFailure afterSaleFailure), Is.True, afterSaleFailure.ToString());
        Assert.That(afterSale, Is.EqualTo(afterPurchase + 3),
            "A selected typed sale commits the bound MoneyAccount, Inventory, and Market owners once each.");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure finalAssessment), Is.True,
            finalAssessment.ToString());
        Assert.That(buyer.Inventory.SelectedDailyV1Owner.Items, Is.Empty);
        Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(4));
    }

    [Test]
    public void DailySelectedSellGoodsExecutesThroughTheCoreOwnerGraphWithoutKeyedReceipt()
    {
        ItemData itemAsset = SimulationTestFactory.CreateItem("daily-selected-sale-item", 10f);
        CityData cityAsset = SimulationTestFactory.CreateCityData("daily-selected-sale-city",
            SimulationTestFactory.CreateMarketItem(itemAsset, 0, 8));
        NpcData npcAsset = SimulationTestFactory.CreateNpc(
            "daily-selected-sale-merchant", NpcJobType.Merchant, MerchantBehavior.Traveling);
        NpcActionData sellAction = SimulationTestFactory.CreateAction(
            "daily-selected-sale-action", NpcActionType.SellGoods, NpcActionCategory.Commerce);
        npcAsset.acoesPadrao.Add(new NPCDefaultAction { action = sellAction, baseUtility = 10f });
        NpcSimulationConfig npcConfig = new NpcSimulationConfig
        {
            npc = npcAsset,
            startingCity = cityAsset,
            initialMoney = 20f
        };
        SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
        authoring.enabledModules.Add(SimulationModule.Merchant);
        authoring.Cities.Add(cityAsset);
        authoring.Npcs.Add(npcConfig);
        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: authoring.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.Calendar, out _);
        DailyV1EffectiveDefinitions definitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
            authoring, effective, calendar, out DailyV1DefinitionAdapter.SelectedBindings bindings);
        DailyV1ItemDefinition item = definitions.Items[itemAsset.DefinitionId];
        CityRuntime city = CityRuntime.CreateSelectedDailyV1(
            "daily-selected-sale-city-runtime", definitions.Cities[cityAsset.DefinitionId], definitions,
            bindings, new SpatialLocationRuntime("daily-selected-sale-location"));
        NpcRuntime merchant = NpcRuntime.CreateSelectedDailyV1(
            "daily-selected-sale-merchant-runtime", definitions.Npcs[npcAsset.DefinitionId],
            definitions, bindings, city, 20f);
        Assert.That(merchant.Inventory.TryAddSelectedItem(item, 2, 1f), Is.True);
        SimulationTime time = new SimulationTime();
        EconomyTransactionService transactionService = new EconomyTransactionService();
        MerchantSystem merchantSystem = new MerchantSystem(
            effective.MerchantTrade,
            effective.CommercialKnowledge,
            travelSystem: null,
            simulationTime: time,
            decisionRecorder: null,
            transactionService: transactionService);
        Assert.That(effective.MerchantTrade.Enabled, Is.True);
        Assert.That(merchant.HasJobType(NpcJobType.Merchant), Is.True);
        Assert.That(merchant.CurrentCity, Is.SameAs(city));
        merchantSystem.ObserveCurrentMarket(merchant);
        Assert.That(merchant.Inventory.GetSelectedAmount(item), Is.EqualTo(2));
        Assert.That(city.Market.Items.Count, Is.EqualTo(1));
        Assert.That(merchant.CommercialKnowledge.SelectedOwner.Observations.Count, Is.EqualTo(1));
        Assert.That(merchant.CommercialKnowledge.SelectedOwner.TryGetObservation(
            city.Location.RuntimeId, item.DefinitionId,
            out DailyV1CommercialMarketObservation observedMarket), Is.True);
        Assert.That(observedMarket.ObservedPrice, Is.GreaterThan(merchant.Inventory.GetSelectedAverageUnitCost(item)));
        SimulationRuntime runtime = new SimulationRuntime(
            time,
            new[] { city },
            new[] { merchant },
            configuredActions: new[] { sellAction },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider> { merchantSystem }),
            merchantSystem: merchantSystem,
            economyTransactionService: transactionService,
            configuration: effective);
        float plannedUtility = 0f;
        NpcActionRuntime plannedSale = merchantSystem.CreateAction(merchant, sellAction, ref plannedUtility);
        Assert.That(plannedSale, Is.Not.Null,
            $"The selected local market facts must make SellGoods available; price={observedMarket.ObservedPrice}, utility={plannedUtility}.");
        Assert.That(plannedSale.TargetItem, Is.SameAs(itemAsset));

        int inventoryBefore = merchant.Inventory.SelectedDailyV1Owner.GetAmount(item);
        int marketBefore = city.Market.SelectedDailyV1Owner.GetAmount(item);
        float moneyBefore = merchant.Money;
        long accountRevisionBefore = merchant.MoneyAccount.Revision;
        long inventoryRevisionBefore = merchant.Inventory.Revision;
        long marketRevisionBefore = city.Market.Revision;

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure), Is.True, failure.ToString());

        Assert.That(merchant.CurrentAction, Is.SameAs(sellAction));
        Assert.That(merchant.CurrentActionRuntime, Is.Not.Null);
        Assert.That(merchant.CurrentActionRuntime.TargetItem, Is.SameAs(itemAsset));
        Assert.That(merchant.CurrentActionRuntime.Amount, Is.EqualTo(2));
        Assert.That(merchant.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(inventoryBefore - 2));
        Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(marketBefore + 2));
        Assert.That(merchant.Money, Is.GreaterThan(moneyBefore));
        Assert.That(merchant.MoneyAccount.Revision, Is.EqualTo(accountRevisionBefore + 1));
        Assert.That(merchant.Inventory.Revision, Is.EqualTo(inventoryRevisionBefore + 1));
        Assert.That(city.Market.Revision, Is.EqualTo(marketRevisionBefore + 1));
        OwnerSectionCensusWitness receipts = transactionService.GetKeyedSaleReceiptCensus();
        Assert.That(receipts.Cardinality, Is.Zero,
            "The ordinary Daily-v1 sale must not create a P18-D keyed receipt.");
        Assert.That(receipts.Revision, Is.Zero);

        Assert.That(merchant.Inventory.TryAddSelectedItem(item, 2, 1f), Is.True);
        float mismatchMoneyBefore = merchant.MoneyAccount.Balance;
        int mismatchInventoryBefore = merchant.Inventory.SelectedDailyV1Owner.GetAmount(item);
        int mismatchMarketBefore = city.Market.SelectedDailyV1Owner.GetAmount(item);
        long mismatchAccountRevisionBefore = merchant.MoneyAccount.Revision;
        long mismatchInventoryRevisionBefore = merchant.Inventory.Revision;
        long mismatchMarketRevisionBefore = city.Market.Revision;
        float mismatchPlanUtility = 0f;
        Assert.That(merchantSystem.CreateAction(merchant, sellAction, ref mismatchPlanUtility), Is.Not.Null,
            "The actor must still have a valid local SellGoods action before its Unity location projection is corrupted.");
        typeof(NpcRuntime).GetField("currentLocation", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(merchant, new SpatialLocationRuntime("daily-selected-sale-mismatched-location"));

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure mismatchedLocationFailure),
            Is.True, mismatchedLocationFailure.ToString());

        Assert.That(merchant.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(mismatchInventoryBefore));
        Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(mismatchMarketBefore));
        Assert.That(merchant.MoneyAccount.Balance, Is.EqualTo(mismatchMoneyBefore));
        Assert.That(merchant.MoneyAccount.Revision, Is.EqualTo(mismatchAccountRevisionBefore));
        Assert.That(merchant.Inventory.Revision, Is.EqualTo(mismatchInventoryRevisionBefore));
        Assert.That(city.Market.Revision, Is.EqualTo(mismatchMarketRevisionBefore));
    }

    [TestCase(NpcActionType.BuyGoods)]
    [TestCase(NpcActionType.SellGoods)]
    public void LocalMarketDispatchRejectsMismatchedActorLocationBeforeOwnerMutationOnSpatialFastPath(
        NpcActionType actionType)
    {
        ItemData itemAsset = SimulationTestFactory.CreateItem("spatial-local-market-item", 10f);
        CityData cityAsset = SimulationTestFactory.CreateCityData("spatial-local-market-city",
            SimulationTestFactory.CreateMarketItem(itemAsset, 8, 8));
        NpcData npcAsset = SimulationTestFactory.CreateNpc(
            "spatial-local-market-merchant", NpcJobType.Merchant, MerchantBehavior.Traveling);
        NpcActionData actionAsset = SimulationTestFactory.CreateAction(
            "spatial-local-market-action-" + actionType, actionType, NpcActionCategory.Commerce);
        npcAsset.acoesPadrao.Add(new NPCDefaultAction { action = actionAsset, baseUtility = 10f });
        NpcSimulationConfig npcConfig = new NpcSimulationConfig
        {
            npc = npcAsset,
            startingCity = cityAsset,
            initialMoney = 100f
        };
        SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
        authoring.enabledModules.Add(SimulationModule.Merchant);
        authoring.Cities.Add(cityAsset);
        authoring.Npcs.Add(npcConfig);
        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: authoring.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.Calendar, out _);
        DailyV1EffectiveDefinitions definitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
            authoring, effective, calendar, out DailyV1DefinitionAdapter.SelectedBindings bindings);
        DailyV1ItemDefinition item = definitions.Items[itemAsset.DefinitionId];
        SpatialLocationRuntime location = new SpatialLocationRuntime("spatial-local-market-location");
        CityRuntime city = CityRuntime.CreateSelectedDailyV1(
            "spatial-local-market-city-runtime", definitions.Cities[cityAsset.DefinitionId], definitions,
            bindings, location);
        NpcRuntime merchant = NpcRuntime.CreateSelectedDailyV1(
            "spatial-local-market-merchant-runtime", definitions.Npcs[npcAsset.DefinitionId],
            definitions, bindings, city, 100f);
        if (actionType == NpcActionType.SellGoods)
            Assert.That(merchant.Inventory.TryAddSelectedItem(item, 2, 1f), Is.True);

        RuntimeIdentityRegistry identityRegistry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime spatialNetwork = new SpatialNetworkRuntime(identityRegistry);
        Assert.That(spatialNetwork.RegisterLocation(location), Is.True);
        SimulationTime time = new SimulationTime();
        EconomyTransactionService transactionService = new EconomyTransactionService();
        MerchantSystem merchantSystem = new MerchantSystem(
            effective.MerchantTrade,
            effective.CommercialKnowledge,
            travelSystem: null,
            simulationTime: time,
            decisionRecorder: null,
            transactionService: transactionService);
        SimulationRuntime runtime = new SimulationRuntime(
            time,
            new[] { city },
            new[] { merchant },
            configuredActions: new[] { actionAsset },
            merchantSystem: merchantSystem,
            economyTransactionService: transactionService,
            configuration: effective,
            runtimeIdentityRegistry: identityRegistry,
            spatialNetworkRuntime: spatialNetwork);
        NpcActionRuntime actionRuntime = new NpcActionRuntime(
            actionAsset, city, itemAsset, 1, 10f);
        merchant.SetCurrentActionRuntime(actionRuntime);
        Assert.That(merchant.CurrentActionRuntime, Is.SameAs(actionRuntime));

        Type adapterType = typeof(SimulationRuntime).GetNestedType(
            "SelectedActionDispatchBoundaryAdapter", BindingFlags.NonPublic);
        ConstructorInfo adapterConstructor = adapterType?.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(SimulationRuntime), typeof(NpcRuntime), typeof(NpcActionRuntime), typeof(NpcActionData) },
            modifiers: null);
        Assert.That(adapterConstructor, Is.Not.Null);
        object adapter = adapterConstructor.Invoke(new object[] { runtime, merchant, actionRuntime, actionAsset });
        MethodInfo getMarketBoundaries = adapterType.GetMethod(
            "TryGetMarketBoundaries", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(getMarketBoundaries, Is.Not.Null);
        object[] boundaryArguments = { merchant.SelectedRuntimeOwners, null, null };
        Assert.That(getMarketBoundaries.Invoke(adapter, boundaryArguments), Is.EqualTo(true),
            "The exact actor and City Location should be admitted by the spatial fast path before divergence.");

        float moneyBefore = merchant.MoneyAccount.Balance;
        int inventoryBefore = merchant.Inventory.SelectedDailyV1Owner.GetAmount(item);
        int marketBefore = city.Market.SelectedDailyV1Owner.GetAmount(item);
        long accountRevisionBefore = merchant.MoneyAccount.Revision;
        long inventoryRevisionBefore = merchant.Inventory.Revision;
        long marketRevisionBefore = city.Market.Revision;
        typeof(NpcRuntime).GetField("currentLocation", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(merchant, new SpatialLocationRuntime("spatial-local-market-mismatched-location"));

        boundaryArguments = new object[] { merchant.SelectedRuntimeOwners, null, null };
        Assert.That(getMarketBoundaries.Invoke(adapter, boundaryArguments), Is.EqualTo(false),
            "A local Buy/Sell target must be rejected before the transaction boundaries are exposed when the actor's projected Location diverges from its Travel owner.");
        Assert.That(merchant.MoneyAccount.Balance, Is.EqualTo(moneyBefore));
        Assert.That(merchant.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(marketBefore));
        Assert.That(merchant.MoneyAccount.Revision, Is.EqualTo(accountRevisionBefore));
        Assert.That(merchant.Inventory.Revision, Is.EqualTo(inventoryRevisionBefore));
        Assert.That(city.Market.Revision, Is.EqualTo(marketRevisionBefore));
    }

    [Test]
    public void DailySelectedPlannedMarketLiquidationExecutesThroughCoreAndCompletesExactPlanOwner()
    {
        ItemData itemAsset = SimulationTestFactory.CreateItem("daily-selected-plan-sale-item", 10f);
        CityData cityAsset = SimulationTestFactory.CreateCityData("daily-selected-plan-sale-city",
            SimulationTestFactory.CreateMarketItem(itemAsset, 0, 8));
        NpcData npcAsset = SimulationTestFactory.CreateNpc(
            "daily-selected-plan-sale-merchant", NpcJobType.Merchant, MerchantBehavior.Traveling);
        NpcActionData sellAction = SimulationTestFactory.CreateAction(
            "daily-selected-plan-sale-action", NpcActionType.SellGoods, NpcActionCategory.Commerce);
        npcAsset.acoesPadrao.Add(new NPCDefaultAction { action = sellAction, baseUtility = 10f });
        NpcSimulationConfig npcConfig = new NpcSimulationConfig
        {
            npc = npcAsset,
            startingCity = cityAsset,
            initialMoney = 20f
        };
        SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
        authoring.enabledModules.Add(SimulationModule.Merchant);
        authoring.Cities.Add(cityAsset);
        authoring.Npcs.Add(npcConfig);
        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: authoring.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.Calendar, out _);
        DailyV1EffectiveDefinitions definitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
            authoring, effective, calendar, out DailyV1DefinitionAdapter.SelectedBindings bindings);
        DailyV1ItemDefinition item = definitions.Items[itemAsset.DefinitionId];
        CityRuntime city = CityRuntime.CreateSelectedDailyV1(
            "daily-selected-plan-sale-city-runtime", definitions.Cities[cityAsset.DefinitionId], definitions,
            bindings, new SpatialLocationRuntime("daily-selected-plan-sale-location"));
        NpcRuntime merchant = NpcRuntime.CreateSelectedDailyV1(
            "daily-selected-plan-sale-merchant-runtime", definitions.Npcs[npcAsset.DefinitionId],
            definitions, bindings, city, 20f);
        Assert.That(merchant.TryBindSelectedDailyV1Definition(
            definitions.Npcs[npcAsset.DefinitionId], definitions, bindings,
            runtimeId => string.Equals(runtimeId, city.RuntimeId, StringComparison.Ordinal) ? city : null), Is.True);
        Assert.That(merchant.Inventory.TryAddSelectedItem(item, 2, 1f), Is.True);
        Assert.That(merchant.SelectedRuntimeOwners.MerchantTradePlan.TryInstallState(
            item, city.RuntimeId, city.RuntimeId, 2, 2, 1f, 0, 0,
            "daily-selected-plan-sale-origin"), Is.True);

        SimulationTime time = new SimulationTime();
        EconomyTransactionService transactionService = new EconomyTransactionService();
        MerchantSystem merchantSystem = new MerchantSystem(
            effective.MerchantTrade,
            effective.CommercialKnowledge,
            travelSystem: null,
            simulationTime: time,
            decisionRecorder: null,
            transactionService: transactionService);
        merchantSystem.ObserveCurrentMarket(merchant);
        Assert.That(merchant.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(2));
        Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(item), Is.Zero);
        Assert.That(merchant.CommercialKnowledge.SelectedOwner.Observations.Count, Is.EqualTo(1));

        WorldId worldId = new WorldId(Guid.NewGuid());
        SimulationRuntime runtime = new SimulationRuntime(
            time,
            new[] { city },
            new[] { merchant },
            configuredActions: new[] { sellAction },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider> { merchantSystem }),
            merchantSystem: merchantSystem,
            economyTransactionService: transactionService,
            configuration: effective,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: worldId);
        BindP12CensusRuntime(transactionService, runtime);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialAssessment),
            Is.True, initialAssessment.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long beforeAdvance,
            out ContinuationCensusFailure beforeAdvanceFailure), Is.True, beforeAdvanceFailure.ToString());
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True,
            "Daily-v1 capture-profile fixtures must complete publication before advancing.");

        float moneyBefore = merchant.MoneyAccount.Balance;
        long accountRevisionBefore = merchant.MoneyAccount.Revision;
        long inventoryRevisionBefore = merchant.Inventory.Revision;
        long marketRevisionBefore = city.Market.Revision;
        long planRevisionBefore = merchant.SelectedRuntimeOwners.MerchantTradePlan.Revision;
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure), Is.True, failure.ToString());

        Assert.That(merchant.CurrentAction, Is.SameAs(sellAction));
        Assert.That(merchant.CurrentActionRuntime, Is.Not.Null);
        Assert.That(merchant.CurrentActionRuntime.TargetCity, Is.SameAs(city));
        Assert.That(merchant.CurrentActionRuntime.TargetNpc, Is.Null);
        Assert.That(merchant.CurrentActionRuntime.SelectedOwner, Is.Not.Null,
            "The selected runtime action must retain its exact Core payload owner.");
        Assert.That(merchant.CurrentActionRuntime.Amount, Is.EqualTo(2));
        Assert.That(merchant.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.Zero);
        Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(2));
        Assert.That(merchant.MoneyAccount.Balance, Is.GreaterThan(moneyBefore));
        Assert.That(merchant.MoneyAccount.Revision, Is.EqualTo(accountRevisionBefore + 1));
        Assert.That(merchant.Inventory.Revision, Is.EqualTo(inventoryRevisionBefore + 1));
        Assert.That(city.Market.Revision, Is.EqualTo(marketRevisionBefore + 1));
        Assert.That(merchant.SelectedRuntimeOwners.MerchantTradePlan.Revision,
            Is.EqualTo(planRevisionBefore + 1),
            $"Expected one plan transition from revision {planRevisionBefore}; current revision is "
            + $"{merchant.SelectedRuntimeOwners.MerchantTradePlan.Revision}, remaining amount "
            + $"{merchant.SelectedRuntimeOwners.MerchantTradePlan.RemainingAmount}, active "
            + $"{merchant.SelectedRuntimeOwners.MerchantTradePlan.IsActive}.");
        Assert.That(merchant.MerchantTradePlan.IsActive, Is.False);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterAdvance,
            out ContinuationCensusFailure afterAdvanceFailure), Is.True, afterAdvanceFailure.ToString());
        Assert.That(afterAdvance, Is.GreaterThan(beforeAdvance),
            "The selected planned sale and its bound owner commits must advance the admitted P12 census epoch.");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure finalAssessment),
            Is.True, finalAssessment.ToString());
        OwnerSectionCensusWitness receipts = transactionService.GetKeyedSaleReceiptCensus();
        Assert.That(receipts.Cardinality, Is.Zero,
            "The ordinary planned sale must not create a P18-D keyed receipt.");
        Assert.That(receipts.Revision, Is.Zero);
    }

    [Test]
    public void P12SelectedNpcTradeNotifiesBothAccountAndInventoryOwnerCommits()
    {
        ItemData itemAsset = SimulationTestFactory.CreateItem("p12-selected-npc-trade-item", 7f);
        CityData cityAsset = SimulationTestFactory.CreateCityData("p12-selected-npc-trade-city",
            SimulationTestFactory.CreateMarketItem(itemAsset, 4, 4));
        NpcData buyerAsset = SimulationTestFactory.CreateNpc("p12-selected-npc-trade-buyer");
        NpcData sellerAsset = SimulationTestFactory.CreateNpc("p12-selected-npc-trade-seller");
        SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
        authoring.Cities.Add(cityAsset);
        authoring.Npcs.Add(new NpcSimulationConfig
        {
            npc = buyerAsset,
            startingCity = cityAsset,
            initialMoney = 30f
        });
        authoring.Npcs.Add(new NpcSimulationConfig
        {
            npc = sellerAsset,
            startingCity = cityAsset,
            initialMoney = 0f
        });

        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: authoring.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.Calendar, out _);
        DailyV1EffectiveDefinitions definitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
            authoring, effective, calendar, out DailyV1DefinitionAdapter.SelectedBindings bindings);
        DailyV1ItemDefinition item = definitions.Items[itemAsset.DefinitionId];
        CityRuntime city = CityRuntime.CreateSelectedDailyV1(
            "p12-selected-npc-trade-city-runtime", definitions.Cities[cityAsset.DefinitionId], definitions,
            bindings, new SpatialLocationRuntime("p12-selected-npc-trade-location"));
        NpcRuntime buyer = NpcRuntime.CreateSelectedDailyV1(
            "p12-selected-npc-trade-buyer-runtime", definitions.Npcs[buyerAsset.DefinitionId], definitions,
            bindings, city, 30f);
        NpcRuntime seller = NpcRuntime.CreateSelectedDailyV1(
            "p12-selected-npc-trade-seller-runtime", definitions.Npcs[sellerAsset.DefinitionId], definitions,
            bindings, city, 0f);
        Assert.That(seller.Inventory.TryAddSelectedItem(item, 2, item.Values.basePrice), Is.True);

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), new[] { city }, new[] { buyer, seller }, economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialAssessment), Is.True,
            initialAssessment.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long beforeTrade,
            out ContinuationCensusFailure beforeTradeFailure), Is.True, beforeTradeFailure.ToString());

        EconomyTransactionResult trade = service.TryExecuteNpcTrade(buyer, seller, item, 1, 7f);

        Assert.That(trade.Success, Is.True, trade.FailureReason.ToString());
        Assert.That(trade.TransactionType, Is.EqualTo(EconomyTransactionType.NpcToNpcTrade));
        Assert.That(trade.ItemDefinitionId, Is.EqualTo(item.DefinitionId));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterTrade,
            out ContinuationCensusFailure afterTradeFailure), Is.True, afterTradeFailure.ToString());
        Assert.That(afterTrade, Is.EqualTo(beforeTrade + 4),
            "A selected NPC trade commits both MoneyAccount owners and both Inventory owners once each.");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure finalAssessment), Is.True,
            finalAssessment.ToString());
        Assert.That(buyer.Money, Is.EqualTo(23f).Within(0.001f));
        Assert.That(seller.Money, Is.EqualTo(7f).Within(0.001f));
        Assert.That(buyer.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(1));
        Assert.That(seller.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(1));
    }

    [Test]
    public void SelectedMerchantBuyGoodsUsesCoreTradePlannerAndCapturedPreference()
    {
        ItemData itemAsset = SimulationTestFactory.CreateItem("selected-buy-planner-item", 5f);
        CityData originAsset = SimulationTestFactory.CreateCityData(
            "selected-buy-planner-origin",
            SimulationTestFactory.CreateMarketItem(itemAsset, 10, 10));
        CityData destinationAsset = SimulationTestFactory.CreateCityData(
            "selected-buy-planner-destination",
            SimulationTestFactory.CreateMarketItem(itemAsset, 10, 10));
        originAsset.connections.Add(new CityConnection { destination = destinationAsset, travelDays = 2 });

        NpcData merchantAsset = SimulationTestFactory.CreateNpc(
            "selected-buy-planner-merchant", NpcJobType.Merchant, MerchantBehavior.Traveling);
        merchantAsset.job.minimumProfitPerItem = 1f;
        merchantAsset.job.PreferredTradeItems.Add(new TradeItemPreference
        {
            item = itemAsset,
            utilityMultiplier = 2f
        });
        NpcActionData buyAction = SimulationTestFactory.CreateAction(
            "selected-buy-planner-action", NpcActionType.BuyGoods, NpcActionCategory.Commerce);
        merchantAsset.acoesPadrao.Add(new NPCDefaultAction { action = buyAction, baseUtility = 10f });
        NpcSimulationConfig npcConfig = new NpcSimulationConfig
        {
            npc = merchantAsset,
            startingCity = originAsset,
            initialMoney = 100f
        };

        SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
        authoring.travelCostPerDay = 1f;
        authoring.Cities.Add(originAsset);
        authoring.Cities.Add(destinationAsset);
        authoring.Npcs.Add(npcConfig);
        authoring.Actions.Add(buyAction);
        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: authoring.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.Calendar, out _);
        DailyV1EffectiveDefinitions definitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
            authoring, effective, calendar, out DailyV1DefinitionAdapter.SelectedBindings bindings);

        DailyV1CityDefinition originNode = definitions.Cities[originAsset.DefinitionId];
        DailyV1CityDefinition destinationNode = definitions.Cities[destinationAsset.DefinitionId];
        DailyV1NpcDefinition merchantNode = definitions.Npcs[merchantAsset.DefinitionId];
        DailyV1ItemDefinition itemNode = definitions.Items[itemAsset.DefinitionId];
        CityRuntime origin = CityRuntime.CreateSelectedDailyV1(
            "selected-buy-planner-origin-runtime", originNode, definitions, bindings,
            new SpatialLocationRuntime("selected-buy-planner-origin-location"));
        CityRuntime destination = CityRuntime.CreateSelectedDailyV1(
            "selected-buy-planner-destination-runtime", destinationNode, definitions, bindings,
            new SpatialLocationRuntime("selected-buy-planner-destination-location"));
        NpcRuntime merchant = NpcRuntime.CreateSelectedDailyV1(
            "selected-buy-planner-merchant-runtime", merchantNode, definitions, bindings, origin, 100f);

        RuntimeIdentityRegistry identityRegistry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime network = new SpatialNetworkRuntime(identityRegistry);
        Assert.That(network.RegisterLocation(origin.Location), Is.True);
        Assert.That(network.RegisterLocation(destination.Location), Is.True);
        SpatialRouteRuntime route = SimulationTestFactory.CreateRoute(
            "selected-buy-planner-route", origin, destination, 2);
        Assert.That(network.RegisterRoute(route), Is.True);
        Dictionary<SpatialLocationRuntime, CityRuntime> citiesByLocation = new Dictionary<SpatialLocationRuntime, CityRuntime>
        {
            [origin.Location] = origin,
            [destination.Location] = destination
        };
        TravelSystem travel = new TravelSystem(
            network,
            location => citiesByLocation.TryGetValue(location, out CityRuntime city) ? city : null,
            1f);
        Assert.That(merchant.SpatialKnowledge.DiscoverLocation(origin.Location.RuntimeId), Is.True);
        Assert.That(merchant.SpatialKnowledge.DiscoverLocation(destination.Location.RuntimeId), Is.True);
        Assert.That(merchant.SpatialKnowledge.DiscoverRoute(route.RuntimeId), Is.True);
        Assert.That(merchant.CommercialKnowledge.RecordObservation(new CommercialMarketObservation(
            origin.Location.RuntimeId, itemAsset, 5f, 10, 0L, 0L,
            CommercialKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(merchant.CommercialKnowledge.RecordObservation(new CommercialMarketObservation(
            destination.Location.RuntimeId, itemAsset, 20f, 10, 0L, 0L,
            CommercialKnowledgeSource.DirectObservation)), Is.True);

        // Mutation after admission proves the selected decision reads the captured typed preference.
        merchantAsset.job.PreferredTradeItems[0].utilityMultiplier = 1000f;
        SimulationTime time = new SimulationTime();
        MerchantSystem system = SimulationTestFactory.CreateMerchantSystem(travel, time);
        SimulationRuntime runtime = new SimulationRuntime(
            time,
            new[] { origin, destination },
            new[] { merchant },
            configuredActions: new[] { buyAction },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider> { system }),
            travelSystem: travel,
            merchantSystem: system,
            configuration: effective,
            spatialNetworkRuntime: network);
        float utility = 10f;
        NpcActionRuntime action = system.CreateAction(merchant, buyAction, ref utility);

        Assert.That(action, Is.Not.Null);
        Assert.That(action.Action, Is.SameAs(buyAction));
        Assert.That(action.TargetCity, Is.SameAs(destination));
        Assert.That(action.TargetItem, Is.SameAs(itemAsset));
        Assert.That(action.Amount, Is.EqualTo(5));
        Assert.That(action.ExpectedUnitPrice, Is.EqualTo(5f));
        Assert.That(action.CommercialDecisionEvidence.ItemDefinitionId, Is.EqualTo(itemNode.DefinitionId));
        Assert.That(utility, Is.EqualTo(103f).Within(0.001f),
            "Core scores net profit 73 over two travel days, applies the captured 2x preference (73), then the existing 30-point BuyGoods base utility (103 total).");
    }

    [Test]
    public void SelectedMerchantMarketBuyAndSaleUseCanonicalOwnersWithoutLegacyItemProjections()
    {
        ItemData itemAsset = SimulationTestFactory.CreateItem("selected-merchant-typed-item", 10f);
        CityData cityAsset = SimulationTestFactory.CreateCityData("selected-merchant-typed-city",
            SimulationTestFactory.CreateMarketItem(itemAsset, 4, 8));
        NpcData npcAsset = SimulationTestFactory.CreateNpc(
            "selected-merchant-typed-npc", NpcJobType.Merchant, MerchantBehavior.Traveling);
        NpcActionData sellAction = SimulationTestFactory.CreateAction(
            "selected-merchant-typed-sell", NpcActionType.SellGoods, NpcActionCategory.Commerce);
        NpcActionData buyAction = SimulationTestFactory.CreateAction(
            "selected-merchant-typed-buy", NpcActionType.BuyGoods, NpcActionCategory.Commerce);
        npcAsset.acoesPadrao.Add(new NPCDefaultAction { action = sellAction, baseUtility = 10f });
        npcAsset.acoesPadrao.Add(new NPCDefaultAction { action = buyAction, baseUtility = 10f });
        NpcSimulationConfig npcConfig = new NpcSimulationConfig
        {
            npc = npcAsset,
            startingCity = cityAsset,
            initialMoney = 20f
        };
        npcConfig.InitialInventory.Add(new NpcInitialInventoryItemConfig
        {
            item = itemAsset,
            amount = 2,
            averageUnitCost = 0f
        });
        SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
        authoring.Cities.Add(cityAsset);
        authoring.Npcs.Add(npcConfig);

        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: authoring.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.Calendar, out _);
        DailyV1EffectiveDefinitions definitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
            authoring, effective, calendar, out DailyV1DefinitionAdapter.SelectedBindings bindings);
        DailyV1CityDefinition cityDefinition = definitions.Cities[cityAsset.DefinitionId];
        DailyV1NpcDefinition npcDefinition = definitions.Npcs[npcAsset.DefinitionId];
        DailyV1ItemDefinition item = definitions.Items[itemAsset.DefinitionId];
        itemAsset.basePrice = 1000f;
        CityRuntime city = CityRuntime.CreateSelectedDailyV1(
            "selected-merchant-typed-city-runtime", cityDefinition, definitions, bindings,
            new SpatialLocationRuntime("selected-merchant-typed-location"));
        NpcRuntime merchant = NpcRuntime.CreateSelectedDailyV1(
            "selected-merchant-typed-npc-runtime", npcDefinition, definitions, bindings, city, 20f);
        Assert.That(merchant.Inventory.TryAddSelectedItem(item, 2, 0f), Is.True);
        Assert.That(merchant.TryBindSelectedDailyV1Definition(
            npcDefinition, definitions, bindings,
            runtimeId => string.Equals(runtimeId, city.RuntimeId, StringComparison.Ordinal) ? city : null), Is.True);

        // Remove only the legacy projections. The Merchant action still receives the
        // authored ItemData at its Unity edge, while the selected owner transaction
        // must bind it through the NPC's existing SelectedBindings and mutate Core.
        typeof(MarketRuntime).GetField("selectedBindings", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(city.Market, null);
        typeof(InventoryRuntime).GetField("selectedBindings", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(merchant.Inventory, null);

        SimulationTime time = new SimulationTime();
        MerchantSystem merchantSystem = SimulationTestFactory.CreateMerchantSystem(null, time);
        Assert.That(merchant.CommercialKnowledge.RecordObservation(new CommercialMarketObservation(
            city.Location.RuntimeId,
            itemAsset,
            20f,
            4,
            0L,
            0L,
            CommercialKnowledgeSource.DirectObservation)), Is.True);
        SimulationRuntime runtime = new SimulationRuntime(
            time,
            new[] { city },
            new[] { merchant },
            configuredActions: new[] { sellAction, buyAction },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider> { merchantSystem }),
            merchantSystem: merchantSystem,
            configuration: effective);
        float selectedSaleUtility = 0f;
        NpcActionRuntime plannedSelectedSale = merchantSystem.CreateAction(
            merchant, sellAction, ref selectedSaleUtility);
        Assert.That(plannedSelectedSale, Is.Not.Null);
        Assert.That(plannedSelectedSale.TargetItem, Is.SameAs(itemAsset));
        Assert.That(plannedSelectedSale.Amount, Is.EqualTo(2));
        Assert.That(plannedSelectedSale.ExpectedUnitPrice, Is.EqualTo(20f));
        Assert.That(selectedSaleUtility, Is.EqualTo(45f).Within(0.001f),
            "The selected Core plan uses captured basePrice 10, not the later Unity authoring value 1000: (20 - 10) * 2 plus SellGoods base utility 25.");

        NpcActionRuntime action = new NpcActionRuntime(sellAction, city, itemAsset, 1, 10f);
        Assert.That(merchant.IsAlive, Is.True);
        Assert.That(merchant.GetConfiguredActionType(sellAction), Is.EqualTo(NpcActionType.SellGoods));
        Assert.That(merchant.TryGetSelectedDailyV1Item(itemAsset, out DailyV1ItemDefinition boundItem), Is.True);
        Assert.That(boundItem, Is.SameAs(item));
        Assert.That(city.Market.CanAddSelectedStock(item, 1), Is.True);
        int inventoryBefore = merchant.Inventory.SelectedDailyV1Owner.GetAmount(item);
        int stockBefore = city.Market.SelectedDailyV1Owner.GetAmount(item);

        NpcActionResult result = merchantSystem.TryExecuteAction(merchant, action);

        Assert.That(result.Success, Is.True);
        Assert.That(merchant.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(inventoryBefore - 1));
        Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(stockBefore + 1));
        Assert.That(merchant.Inventory.GetAmount(itemAsset), Is.Zero,
            "The test intentionally removed the adapter projection; Core owner state remains authoritative.");

        NpcActionRuntime buy = new NpcActionRuntime(buyAction, city, itemAsset, 1, 10f);
        Assert.That(merchantSystem.TryExecuteAction(merchant, buy).Success, Is.True);
        Assert.That(merchant.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(stockBefore));

        PersonId actor = new PersonId("selected-merchant-typed-actor");
        Assert.That(merchantSystem.TryExecuteKeyedLocalMarketSale(
            merchant, actor, action, "selected-merchant-typed-proposal", "selected-merchant-typed-fingerprint",
            "selected-merchant-typed-input", "selected-merchant-typed-request", "UnityBootstrap-Daily-v1",
            new LogicalTick(1L), out KeyedSaleReceipt firstReceipt), Is.True);
        Assert.That(firstReceipt.Outcome, Is.EqualTo(KeyedSaleOutcome.Committed));
        int inventoryAfterKeyedSale = merchant.Inventory.SelectedDailyV1Owner.GetAmount(item);
        int stockAfterKeyedSale = city.Market.SelectedDailyV1Owner.GetAmount(item);
        Assert.That(inventoryAfterKeyedSale, Is.EqualTo(inventoryBefore - 1));
        Assert.That(stockAfterKeyedSale, Is.EqualTo(stockBefore + 1));

        Assert.That(merchantSystem.TryExecuteKeyedLocalMarketSale(
            merchant, actor, action, "selected-merchant-typed-proposal", "selected-merchant-typed-fingerprint",
            "selected-merchant-typed-input", "selected-merchant-typed-request", "UnityBootstrap-Daily-v1",
            new LogicalTick(1L), out KeyedSaleReceipt replayedReceipt), Is.True);
        Assert.That(replayedReceipt, Is.SameAs(firstReceipt));
        Assert.That(merchant.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(inventoryAfterKeyedSale));
        Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(stockAfterKeyedSale));

        Assert.That(merchant.TrySetSelectedMerchantTradePlan(
            itemAsset, city, city, 4, 10f, "selected-merchant-typed-plan"), Is.True);
        float plannedSaleUtility = 0f;
        NpcActionRuntime plannedSale = merchantSystem.CreateAction(merchant, sellAction, ref plannedSaleUtility);
        Assert.That(plannedSale, Is.Not.Null,
            "Selected trade-plan inventory admission reads the authoritative Core owner even after the Unity inventory projection binding was removed.");
        Assert.That(plannedSale.TargetItem, Is.SameAs(itemAsset));
        Assert.That(plannedSale.Amount, Is.EqualTo(1),
            "Planned sale quantity is capped by the current Core-owned inventory amount.");

        Assert.That(merchant.Inventory.RemoveSelectedItem(item, 1), Is.True);
        merchantSystem.AdvanceNpcTradeState(merchant);
        Assert.That(merchant.MerchantTradePlan.IsActive, Is.False,
            "After the exact owner inventory is depleted, normalization clears the selected merchant plan.");
    }

    [Test]
    public void SelectedMerchantNpcSaleTransfersThroughSharedCanonicalInventoryOwners()
    {
        ItemData itemAsset = SimulationTestFactory.CreateItem("selected-merchant-npc-trade-item", 10f);
        CityData cityAsset = SimulationTestFactory.CreateCityData("selected-merchant-npc-trade-city",
            SimulationTestFactory.CreateMarketItem(itemAsset, 4, 8));
        NpcData sellerAsset = SimulationTestFactory.CreateNpc(
            "selected-merchant-npc-trade-seller", NpcJobType.Merchant, MerchantBehavior.Traveling);
        NpcData buyerAsset = SimulationTestFactory.CreateNpc(
            "selected-merchant-npc-trade-buyer", NpcJobType.Merchant, MerchantBehavior.Local);
        NpcActionData sellAction = SimulationTestFactory.CreateAction(
            "selected-merchant-npc-trade-sell", NpcActionType.SellGoods, NpcActionCategory.Commerce);
        sellerAsset.acoesPadrao.Add(new NPCDefaultAction { action = sellAction, baseUtility = 10f });
        SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
        authoring.Cities.Add(cityAsset);
        authoring.Npcs.Add(new NpcSimulationConfig
        {
            npc = sellerAsset,
            startingCity = cityAsset,
            initialMoney = 20f
        });
        authoring.Npcs.Add(new NpcSimulationConfig
        {
            npc = buyerAsset,
            startingCity = cityAsset,
            initialMoney = 100f
        });

        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: authoring.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.Calendar, out _);
        DailyV1EffectiveDefinitions definitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
            authoring, effective, calendar, out DailyV1DefinitionAdapter.SelectedBindings bindings);
        DailyV1CityDefinition cityDefinition = definitions.Cities[cityAsset.DefinitionId];
        DailyV1NpcDefinition sellerDefinition = definitions.Npcs[sellerAsset.DefinitionId];
        DailyV1NpcDefinition buyerDefinition = definitions.Npcs[buyerAsset.DefinitionId];
        DailyV1ItemDefinition item = definitions.Items[itemAsset.DefinitionId];
        CityRuntime city = CityRuntime.CreateSelectedDailyV1(
            "selected-merchant-npc-trade-city-runtime", cityDefinition, definitions, bindings,
            new SpatialLocationRuntime("selected-merchant-npc-trade-location"));
        NpcRuntime seller = NpcRuntime.CreateSelectedDailyV1(
            "selected-merchant-npc-trade-seller-runtime", sellerDefinition, definitions, bindings, city, 20f);
        NpcRuntime buyer = NpcRuntime.CreateSelectedDailyV1(
            "selected-merchant-npc-trade-buyer-runtime", buyerDefinition, definitions, bindings, city, 100f);
        Assert.That(seller.Inventory.TryAddSelectedItem(item, 2, 3f), Is.True);
        MerchantSystem merchantSystem = SimulationTestFactory.CreateMerchantSystem(null, new SimulationTime());
        NpcActionRuntime action = new NpcActionRuntime(sellAction, buyer, itemAsset, 1, 10f);
        float sellerMoneyBefore = seller.Money;
        float buyerMoneyBefore = buyer.Money;

        NpcActionResult result = merchantSystem.TryExecuteAction(seller, action);

        Assert.That(result.Success, Is.True);
        Assert.That(seller.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(1));
        Assert.That(buyer.Inventory.SelectedDailyV1Owner.GetAmount(item), Is.EqualTo(1));
        Assert.That(seller.Money, Is.GreaterThan(sellerMoneyBefore));
        Assert.That(buyer.Money, Is.LessThan(buyerMoneyBefore));
        Assert.That(seller.Inventory.SelectedDailyV1Owner.Definitions, Is.SameAs(definitions));
        Assert.That(buyer.Inventory.SelectedDailyV1Owner.Definitions, Is.SameAs(definitions));
    }

    [Test]
    public void AccountBackedPurchaseTransfersMoneyAndCapturesSettlementIds()
    {
        ItemData item = SimulationTestFactory.CreateItem("account-purchase-item", 10f);
        CityRuntime city = CreateAccountBackedCity("city-purchase", item, 10, 50f);
        NpcRuntime npc = CreateNpc("account-buyer", 100f);

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteMarketPurchase(npc, city.Market, item, 4);

        Assert.That(result.Success, Is.True);
        Assert.That(result.MoneyEffect, Is.EqualTo(MoneyEffect.Transfer));
        Assert.That(result.SourceRuntimeId, Is.EqualTo(npc.RuntimeId));
        Assert.That(result.DestinationRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(npc.Money, Is.EqualTo(60f).Within(0.001f));
        Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(90f).Within(0.001f));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(6));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(4));
    }

    [Test]
    public void AccountBackedSaleTransfersMoneyAndConservesBalances()
    {
        ItemData item = SimulationTestFactory.CreateItem("account-sale-item", 10f);
        CityRuntime city = CreateAccountBackedCity("city-sale", item, 10, 100f);
        NpcRuntime npc = CreateNpc("account-seller", 20f);
        npc.Inventory.AddItem(item, 5, 4f);

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteMarketSale(npc, city.Market, item, 3);

        Assert.That(result.Success, Is.True);
        Assert.That(result.MoneyEffect, Is.EqualTo(MoneyEffect.Transfer));
        Assert.That(result.SourceRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(result.DestinationRuntimeId, Is.EqualTo(npc.RuntimeId));
        Assert.That(npc.Money, Is.EqualTo(50f).Within(0.001f));
        Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(70f).Within(0.001f));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(13));
    }

    [Test]
    public void AccountBackedSaleFailsWithExplicitCounterpartyFundsReason()
    {
        ItemData item = SimulationTestFactory.CreateItem("insufficient-settlement-item", 10f);
        CityRuntime city = CreateAccountBackedCity("city-insufficient", item, 10, 5f);
        NpcRuntime npc = CreateNpc("insufficient-seller", 20f);
        npc.Inventory.AddItem(item, 1, 4f);

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteMarketSale(npc, city.Market, item, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InsufficientCounterpartyFunds));
        Assert.That(npc.Money, Is.EqualTo(20f));
        Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(5f));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(1));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(10));
    }

    [Test]
    public void AccountBackedSaleUsesPartialQuantityWhenSettlementCannotPayAll()
    {
        ItemData item = SimulationTestFactory.CreateItem("partial-sale-item", 10f);
        CityRuntime city = CreateAccountBackedCity("city-partial", item, 10, 35f);
        NpcRuntime npc = CreateNpc("partial-seller", 0f);
        npc.Inventory.AddItem(item, 10, 4f);

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteMarketSale(npc, city.Market, item, 10);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RequestedQuantity, Is.EqualTo(10));
        Assert.That(result.Quantity, Is.EqualTo(3));
        Assert.That(result.TotalPrice, Is.EqualTo(30f).Within(0.001f));
        Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(5f).Within(0.001f));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(7));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(13));
    }

    [Test]
    public void AccountBackedSettlementDepletesWithoutAutomaticRefill()
    {
        ItemData item = SimulationTestFactory.CreateItem("depleting-settlement-item", 10f);
        CityRuntime city = CreateAccountBackedCity("city-depleting", item, 10, 25f);
        NpcRuntime npc = CreateNpc("depleting-seller", 0f);
        npc.Inventory.AddItem(item, 5, 4f);
        EconomyTransactionService service = new EconomyTransactionService();

        EconomyTransactionResult firstSale = service.TryExecuteMarketSale(npc, city.Market, item, 5);
        float balanceAfterFirstSale = city.Market.Counterparty.MoneyAccount.Balance;
        city.Market.UpdatePrices();
        EconomyTransactionResult secondSale = service.TryExecuteMarketSale(npc, city.Market, item, 2);

        Assert.That(firstSale.Success, Is.True);
        Assert.That(balanceAfterFirstSale, Is.EqualTo(5f).Within(0.001f));
        Assert.That(secondSale.Success, Is.False);
        Assert.That(secondSale.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InsufficientCounterpartyFunds));
        Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(5f).Within(0.001f));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(3));
    }

    [Test]
    public void AccountBackedPurchaseRestoresSettlementPurchasingPowerForLaterSale()
    {
        ItemData item = SimulationTestFactory.CreateItem("restored-liquidity-item", 10f);
        CityRuntime city = CreateAccountBackedCity("city-restored", item, 10, 0f);
        NpcRuntime buyer = CreateNpc("liquidity-buyer", 20f);
        NpcRuntime seller = CreateNpc("liquidity-seller", 0f);
        seller.Inventory.AddItem(item, 2, 4f);
        EconomyTransactionService service = new EconomyTransactionService();

        EconomyTransactionResult beforeRevenue = service.TryExecuteMarketSale(seller, city.Market, item, 1);
        EconomyTransactionResult purchase = service.TryExecuteMarketPurchase(buyer, city.Market, item, 2);
        float balanceAfterPurchase = city.Market.Counterparty.MoneyAccount.Balance;
        EconomyTransactionResult afterRevenue = service.TryExecuteMarketSale(seller, city.Market, item, 2);
        float expectedFinalBalance = balanceAfterPurchase - afterRevenue.TotalPrice;

        Assert.That(beforeRevenue.Success, Is.False);
        Assert.That(beforeRevenue.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InsufficientCounterpartyFunds));
        Assert.That(purchase.Success, Is.True);
        Assert.That(balanceAfterPurchase, Is.EqualTo(purchase.TotalPrice).Within(0.001f));
        Assert.That(afterRevenue.Success, Is.True);
        Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(expectedFinalBalance).Within(0.001f));
    }

    [Test]
    public void AccountBackedProductionAndConsumptionDoNotMoveCounterpartyMoney()
    {
        ItemData item = SimulationTestFactory.CreateItem("production-item", 10f);
        CityRuntime city = CreateAccountBackedCity("city-production", item, 10, 42f);
        int configuredProductionAmount = 5;
        city.CityData.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = configuredProductionAmount });
        city.CityData.marketItems[0].consumptionPer1000Population = 1000f;

        int initialStock = city.Market.GetAmount(item);
        float initialCounterpartyMoney = city.Market.Counterparty.MoneyAccount.Balance;

        city.SimulateProductionDay();
        int stockAfterProduction = city.Market.GetAmount(item);
        float balanceAfterProduction = city.Market.Counterparty.MoneyAccount.Balance;

        Assert.That(stockAfterProduction, Is.EqualTo(initialStock + configuredProductionAmount));
        Assert.That(balanceAfterProduction, Is.EqualTo(initialCounterpartyMoney).Within(0.001f));

        city.SimulateConsumptionDay();
        int stockAfterConsumption = city.Market.GetAmount(item);
        float balanceAfterConsumption = city.Market.Counterparty.MoneyAccount.Balance;

        Assert.That(stockAfterConsumption, Is.LessThan(stockAfterProduction));
        Assert.That(balanceAfterConsumption, Is.EqualTo(initialCounterpartyMoney).Within(0.001f));
    }

    [Test]
    public void AccountBackedPurchaseRejectsCounterpartyCreditOverflowAtomically()
    {
        ItemData item = SimulationTestFactory.CreateItem("counterparty-overflow-item", 10f);
        CityRuntime city = CreateAccountBackedCity("city-counterparty-overflow", item, 5, float.MaxValue);
        NpcRuntime npc = CreateNpc("overflow-buyer", 100f);

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteMarketPurchase(npc, city.Market, item, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.CounterpartyCreditCapacity));
        Assert.That(npc.Money, Is.EqualTo(100f));
        Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(float.MaxValue));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(5));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(0));
    }

    [Test]
    public void AccountBackedSaleRejectsNpcCreditOverflowAtomically()
    {
        ItemData item = SimulationTestFactory.CreateItem("npc-overflow-item", 10f);
        CityRuntime city = CreateAccountBackedCity("city-npc-overflow", item, 5, 100f);
        NpcRuntime npc = CreateNpc("overflow-seller", float.MaxValue);
        npc.Inventory.AddItem(item, 1, 4f);

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteMarketSale(npc, city.Market, item, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.CreditRejected));
        Assert.That(npc.Money, Is.EqualTo(float.MaxValue));
        Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(100f));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(1));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(5));
    }

    [Test]
    public void MarketWrappersRespectAccountBackedCounterparty()
    {
        ItemData item = SimulationTestFactory.CreateItem("wrapper-item", 10f);
        CityRuntime city = CreateAccountBackedCity("city-wrapper", item, 5, 0f);
        NpcRuntime npc = CreateNpc("wrapper-buyer", 10f);

        Assert.That(city.Market.BuyItem(npc, item, 1, out int amountBought, out _, out _), Is.True);
        Assert.That(amountBought, Is.EqualTo(1));
        Assert.That(city.Market.Counterparty.MoneyAccount.Balance, Is.EqualTo(10f).Within(0.001f));
    }

    [Test]
    public void AccountBackedCounterpartyRequiresMoneyAccount()
    {
        Assert.Throws<System.ArgumentNullException>(() => MarketCounterpartyRuntime.CreateAccountBacked("city", null));
    }

    [Test]
    public void AccountBackedReceiptRemainsASnapshot()
    {
        ItemData item = SimulationTestFactory.CreateItem("account-snapshot-item", 10f);
        CityRuntime city = CreateAccountBackedCity("city-snapshot", item, 10, 50f);
        NpcRuntime npc = CreateNpc("snapshot-buyer", 100f);
        EconomyTransactionResult receipt = new EconomyTransactionService().TryExecuteMarketPurchase(npc, city.Market, item, 1);
        float unitPrice = receipt.UnitPrice;
        float totalPrice = receipt.TotalPrice;

        city.Market.Counterparty.MoneyAccount.TryCredit(10f);
        city.Market.AddStock(item, 20);
        city.Market.UpdatePrices();

        Assert.That(receipt.MoneyEffect, Is.EqualTo(MoneyEffect.Transfer));
        Assert.That(receipt.SourceRuntimeId, Is.EqualTo(npc.RuntimeId));
        Assert.That(receipt.DestinationRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(receipt.UnitPrice, Is.EqualTo(unitPrice));
        Assert.That(receipt.TotalPrice, Is.EqualTo(totalPrice));
    }

    [Test]
    public void CityData_DefaultLiquidityConfigurationCreatesOpenMarket()
    {
        CityData definition = SimulationTestFactory.CreateCityData("default-open");
        CityRuntime city = new CityRuntime("city-default-open", definition, new SpatialLocationRuntime("location-default-open"));

        Assert.That(definition.MarketLiquidity.liquidityMode, Is.EqualTo(MarketLiquidityMode.Open));
        SimulationInvariantValidator.ValidateCityMarketCounterparty(city);
        Assert.That(city.MarketCounterparty.LiquidityMode, Is.EqualTo(MarketLiquidityMode.Open));
    }

    [Test]
    public void CityData_AccountBackedConfigurationCreatesSettlementAccountWithCityIdentity()
    {
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity(
            "city-configured", "location-configured", 37f);

        SimulationInvariantValidator.ValidateCityMarketCounterparty(city);
        Assert.That(city.MarketCounterparty.LiquidityMode, Is.EqualTo(MarketLiquidityMode.AccountBacked));
        Assert.That(city.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(37f));
        Assert.That(city.MarketCounterparty.CounterpartyRuntimeId, Is.EqualTo("city-configured"));
    }

    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void CityData_AccountBackedConfigurationRejectsInvalidInitialPurchasingPower(float invalidBalance)
    {
        CityData definition = SimulationTestFactory.CreateCityData("invalid-liquidity");
        definition.marketLiquidity = new MarketLiquidityConfig
        {
            liquidityMode = MarketLiquidityMode.AccountBacked,
            initialPurchasingPower = invalidBalance
        };

        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            new CityRuntime("city-invalid", definition, new SpatialLocationRuntime("location-invalid")));
    }

    [Test]
    public void ConfiguredAccountBackedSettlementDepletesAndRevenueRestoresPurchasingPower()
    {
        ItemData item = SimulationTestFactory.CreateItem("configured-cycle", 10f);
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity(
            "city-cycle", "location-cycle", 30f, SimulationTestFactory.CreateMarketItem(item, 10, 10));
        NpcRuntime seller = CreateNpc("configured-seller", 0f);
        NpcRuntime buyer = CreateNpc("configured-buyer", 20f);
        seller.Inventory.AddItem(item, 5, 4f);
        EconomyTransactionService service = new EconomyTransactionService();

        EconomyTransactionResult depletion = service.TryExecuteMarketSale(seller, city.Market, item, 3);
        EconomyTransactionResult blocked = service.TryExecuteMarketSale(seller, city.Market, item, 1);
        EconomyTransactionResult revenue = service.TryExecuteMarketPurchase(buyer, city.Market, item, 2);
        EconomyTransactionResult restored = service.TryExecuteMarketSale(seller, city.Market, item, 1);

        Assert.That(depletion.Success, Is.True);
        Assert.That(blocked.Success, Is.False);
        Assert.That(revenue.Success, Is.True);
        Assert.That(restored.Success, Is.True);
        Assert.That(seller.Money + buyer.Money + city.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(50f).Within(0.001f));
        Assert.That(seller.Inventory.GetAmount(item) + buyer.Inventory.GetAmount(item) + city.Market.GetAmount(item), Is.EqualTo(15));
    }

    private static NpcRuntime CreateNpc(string id, float money, CityRuntime city = null)
    {
        return new NpcRuntime(id, SimulationTestFactory.CreateNpc(id), city, money);
    }

    private static void BindP12CensusRuntime(EconomyTransactionService service, SimulationRuntime runtime)
    {
        MethodInfo bind = typeof(SimulationRuntime).GetMethod(
            "BindP12EconomyTransactionService", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(bind, Is.Not.Null);
        bind.Invoke(runtime, new object[] { service });
    }

    private static void SetRevision(object owner, long value)
    {
        FieldInfo revision = owner.GetType().GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(revision, Is.Not.Null, owner.GetType().Name);
        revision.SetValue(owner, value);
    }

    private static CityRuntime CreateAccountBackedCity(string runtimeId, ItemData item, int initialStock, float settlementBalance)
    {
        CityData cityData = SimulationTestFactory.CreateCityData(
            "definition-" + runtimeId,
            SimulationTestFactory.CreateMarketItem(item, initialStock, Mathf.Max(1, initialStock)));
        MarketCounterpartyRuntime counterparty = MarketCounterpartyRuntime.CreateAccountBacked(
            runtimeId,
            new MoneyAccountRuntime(settlementBalance));
        return new CityRuntime(
            runtimeId,
            cityData,
            new SpatialLocationRuntime("location-" + runtimeId),
            null,
            counterparty);
    }

    private sealed class SelectedCrossCityAdapterFixture
    {
        internal readonly DailyV1EffectiveDefinitions Definitions;
        internal readonly DailyV1DefinitionAdapter.SelectedBindings Bindings;
        internal readonly DailyV1SelectedRuntimeOwnerGraph Graph;
        internal readonly DailyV1CityRuntimeOwners OriginOwner;
        internal readonly DailyV1CityRuntimeOwners DestinationOwner;
        internal readonly DailyV1NpcRuntimeOwners MerchantOwner;
        internal readonly CityRuntime OriginProjection;
        internal readonly CityRuntime DestinationProjection;
        internal readonly NpcRuntime Merchant;
        internal readonly DailyV1ItemDefinition ItemNode;
        internal readonly DailyV1NpcActionPayloadOwner Payload;

        internal SelectedCrossCityAdapterFixture()
        {
            ItemData itemAsset = SimulationTestFactory.CreateItem("adapter-cross-city-item", 10f);
            CityData originAsset = SimulationTestFactory.CreateCityData("adapter-cross-city-origin",
                SimulationTestFactory.CreateMarketItem(itemAsset, 8, 8));
            CityData destinationAsset = SimulationTestFactory.CreateCityData("adapter-cross-city-destination",
                SimulationTestFactory.CreateMarketItem(itemAsset, 8, 8));
            originAsset.connections.Add(new CityConnection { destination = destinationAsset, travelDays = 2 });
            NpcData merchantAsset = SimulationTestFactory.CreateNpc(
                "adapter-cross-city-merchant", NpcJobType.Merchant, MerchantBehavior.Traveling);
            NpcActionData buyAction = SimulationTestFactory.CreateAction(
                "adapter-cross-city-buy", NpcActionType.BuyGoods, NpcActionCategory.Commerce);
            merchantAsset.acoesPadrao.Add(new NPCDefaultAction { action = buyAction, baseUtility = 1f });
            SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
            authoring.Cities.Add(originAsset);
            authoring.Cities.Add(destinationAsset);
            authoring.Actions.Add(buyAction);
            authoring.Npcs.Add(new NpcSimulationConfig
            {
                npc = merchantAsset,
                startingCity = originAsset,
                initialMoney = 100f
            });
            EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
                contentOverrides: authoring.CreateConfigurationOverrides());
            CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.calendar, out _);
            Assert.That(DailyV1DefinitionAdapter.TryCreate(authoring, effective, calendar,
                SimulationGenesisPipeline.ResolveStageOrder(includeAuthoredGeography: true),
                "axial-hex-v1", "q-then-r", out DailyV1EffectiveDefinitions definitions,
                out DailyV1DefinitionAdapter.SelectedBindings bindings,
                out string diagnostic), Is.True, diagnostic);
            Definitions = definitions;
            Bindings = bindings;
            RuntimeIdAllocator runtimeIds = new RuntimeIdAllocator();
            DailyV1SelectedRuntimeOwnerGraphBuilder builder =
                new DailyV1SelectedRuntimeOwnerGraphBuilder(Definitions);
            Assert.That(builder.TryCreateConfiguredCities(runtimeIds, _ => true), Is.True);
            Assert.That(builder.TryCreateConfiguredNpcs(runtimeIds,
                cityId => builder.ResolveUniqueStartingCity(cityId)?.RuntimeId), Is.True);
            Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
            Graph = graph;
            OriginOwner = FindCity(builder, originAsset.DefinitionId);
            DestinationOwner = FindCity(builder, destinationAsset.DefinitionId);
            MerchantOwner = builder.Npcs[0];
            Assert.That(Graph.SpatialNetwork.TryGetLocation(OriginOwner.LocationRuntimeId,
                out SpatialLocationRuntime originLocation), Is.True);
            Assert.That(Graph.SpatialNetwork.TryGetLocation(DestinationOwner.LocationRuntimeId,
                out SpatialLocationRuntime destinationLocation), Is.True);
            OriginProjection = CityRuntime.CreateSelectedDailyV1(
                OriginOwner.RuntimeId, OriginOwner.Definition, Definitions, Bindings,
                OriginOwner, originLocation);
            DestinationProjection = CityRuntime.CreateSelectedDailyV1(
                DestinationOwner.RuntimeId, DestinationOwner.Definition, Definitions, Bindings,
                DestinationOwner, destinationLocation);
            Merchant = NpcRuntime.CreateSelectedDailyV1(
                MerchantOwner.RuntimeId, MerchantOwner.Definition, Definitions, Bindings,
                MerchantOwner, OriginProjection);
            Assert.That(Merchant.TryBindSelectedRuntimeOwnerGraphProvider(() => Graph), Is.True);
            ItemNode = Definitions.Items[itemAsset.DefinitionId];
            NpcActionRuntime action = new NpcActionRuntime(
                buyAction, DestinationProjection, itemAsset, 1, 10f);
            action.SetOriginDecisionId("decision/adapter-cross-city-buy");
            Assert.That(Merchant.TryPrepareSelectedActionPayload(action,
                out DailyV1NpcActionPayloadOwner payload), Is.True);
            Payload = payload;
            Assert.That(Graph.TryCommitCurrentActionSlot(MerchantOwner, Payload.Action, Payload, () => true), Is.True);
        }

        private static DailyV1CityRuntimeOwners FindCity(
            DailyV1SelectedRuntimeOwnerGraphBuilder builder,
            string definitionId)
        {
            foreach (DailyV1CityRuntimeOwners city in builder.Cities)
                if (string.Equals(city.Definition.DefinitionId, definitionId, StringComparison.Ordinal))
                    return city;
            Assert.Fail("Expected selected graph City owner for " + definitionId);
            return null;
        }
    }
}
