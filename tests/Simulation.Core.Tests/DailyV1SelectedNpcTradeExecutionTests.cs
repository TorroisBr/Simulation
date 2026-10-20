using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedNpcTradeExecutionTests
{
    [Test]
    public void CoreTradeCommitsInEstablishedOrderAndNotifiesEachChangedOwner()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners buyer, out DailyV1NpcRuntimeOwners seller,
            out DailyV1ItemDefinition item);
        float buyerBalance = buyer.MoneyAccount.Balance;
        float sellerBalance = seller.MoneyAccount.Balance;
        float total = 2 * 15f;
        RecordingBoundary boundary = new RecordingBoundary(true);

        DailyV1SelectedNpcTradeCommitResult result = DailyV1SelectedNpcTradeExecution.TryExecute(
            graph, buyer, seller, item, 2, 15f, boundary);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.BuyerAccountMutationCount, Is.EqualTo(1));
        Assert.That(result.SellerAccountMutationCount, Is.EqualTo(1));
        Assert.That(result.SellerInventoryMutationCount, Is.EqualTo(1));
        Assert.That(result.BuyerInventoryMutationCount, Is.EqualTo(1));
        Assert.That(boundary.Events, Is.EqualTo(new[] { "begin", "buyer-account", "seller-account",
            "seller-inventory", "buyer-inventory", "dispose" }));
        Assert.That(buyer.MoneyAccount.Balance, Is.EqualTo(buyerBalance - total).Within(0.001f));
        Assert.That(seller.MoneyAccount.Balance, Is.EqualTo(sellerBalance + total).Within(0.001f));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(buyer.Inventory.GetAmount(item), Is.EqualTo(2));
    }

    [Test]
    public void SellerInventoryRejectionRefundsBothAccountsAndPublishesCompensationWrites()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners buyer, out DailyV1NpcRuntimeOwners seller,
            out DailyV1ItemDefinition item);
        Assert.That(seller.Inventory.TryBindMutationBoundary(() => false, () => { }), Is.True);
        float buyerBalance = buyer.MoneyAccount.Balance;
        float sellerBalance = seller.MoneyAccount.Balance;
        RecordingBoundary boundary = new RecordingBoundary(true);

        DailyV1SelectedNpcTradeCommitResult result = DailyV1SelectedNpcTradeExecution.TryExecute(
            graph, buyer, seller, item, 2, 15f, boundary);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure, Is.EqualTo(DailyV1SelectedNpcTradeFailure.TransactionCommitFailed));
        Assert.That(result.BuyerAccountMutationCount, Is.EqualTo(2));
        Assert.That(result.SellerAccountMutationCount, Is.EqualTo(2));
        Assert.That(boundary.Events, Is.EqualTo(new[] { "begin", "buyer-account", "seller-account",
            "seller-account", "buyer-account", "dispose" }));
        Assert.That(buyer.MoneyAccount.Balance, Is.EqualTo(buyerBalance).Within(0.001f));
        Assert.That(seller.MoneyAccount.Balance, Is.EqualTo(sellerBalance).Within(0.001f));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(4));
        Assert.That(buyer.Inventory.GetAmount(item), Is.Zero);
    }

    [Test]
    public void BuyerInventoryRejectionRetainsEstablishedPartialCommitAndSectionReceipts()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners buyer, out DailyV1NpcRuntimeOwners seller,
            out DailyV1ItemDefinition item);
        Assert.That(buyer.Inventory.TryBindMutationBoundary(() => false, () => { }), Is.True);
        RecordingBoundary boundary = new RecordingBoundary(true);

        DailyV1SelectedNpcTradeCommitResult result = DailyV1SelectedNpcTradeExecution.TryExecute(
            graph, buyer, seller, item, 2, 15f, boundary);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.PartialCommit, Is.True);
        Assert.That(result.Failure, Is.EqualTo(DailyV1SelectedNpcTradeFailure.PartialCommit));
        Assert.That(boundary.Events, Is.EqualTo(new[] { "begin", "buyer-account", "seller-account",
            "seller-inventory", "dispose" }));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(buyer.Inventory.GetAmount(item), Is.Zero);
    }

    [Test]
    public void P12AdmissionRejectionOccursBeforeAnyOwnerWrite()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners buyer, out DailyV1NpcRuntimeOwners seller,
            out DailyV1ItemDefinition item);
        long buyerRevision = buyer.MoneyAccount.Revision;
        long sellerRevision = seller.MoneyAccount.Revision;
        RecordingBoundary boundary = new RecordingBoundary(false);

        DailyV1SelectedNpcTradeCommitResult result = DailyV1SelectedNpcTradeExecution.TryExecute(
            graph, buyer, seller, item, 2, 15f, boundary);

        Assert.That(result.Failure, Is.EqualTo(DailyV1SelectedNpcTradeFailure.AdmissionRejected));
        Assert.That(boundary.Events, Is.EqualTo(new[] { "begin" }));
        Assert.That(buyer.MoneyAccount.Revision, Is.EqualTo(buyerRevision));
        Assert.That(seller.MoneyAccount.Revision, Is.EqualTo(sellerRevision));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(4));
    }

    [Test]
    public void BuyerDebitRejectionStopsBeforeAnyLaterOwnerWrite()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners buyer, out DailyV1NpcRuntimeOwners seller,
            out DailyV1ItemDefinition item);
        buyer.MoneyAccount.BindP12MutationBoundary(() => false, () => { });
        RecordingBoundary boundary = new RecordingBoundary(true);

        DailyV1SelectedNpcTradeCommitResult result = DailyV1SelectedNpcTradeExecution.TryExecute(
            graph, buyer, seller, item, 2, 15f, boundary);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.BuyerAccountMutationCount, Is.Zero);
        Assert.That(result.SellerAccountMutationCount, Is.Zero);
        Assert.That(boundary.Events, Is.EqualTo(new[] { "begin", "dispose" }));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(4));
        Assert.That(buyer.Inventory.GetAmount(item), Is.Zero);
    }

    [Test]
    public void SellerCreditRejectionRefundsBuyerAndPublishesBothBuyerAccountWrites()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners buyer, out DailyV1NpcRuntimeOwners seller,
            out DailyV1ItemDefinition item);
        seller.MoneyAccount.BindP12MutationBoundary(() => false, () => { });
        float buyerBalance = buyer.MoneyAccount.Balance;
        RecordingBoundary boundary = new RecordingBoundary(true);

        DailyV1SelectedNpcTradeCommitResult result = DailyV1SelectedNpcTradeExecution.TryExecute(
            graph, buyer, seller, item, 2, 15f, boundary);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.BuyerAccountMutationCount, Is.EqualTo(2));
        Assert.That(result.SellerAccountMutationCount, Is.Zero);
        Assert.That(boundary.Events, Is.EqualTo(new[] { "begin", "buyer-account",
            "buyer-account", "dispose" }));
        Assert.That(buyer.MoneyAccount.Balance, Is.EqualTo(buyerBalance).Within(0.001f));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(4));
    }

    [Test]
    public void SelectedPlannedBuyerPayloadExecutesTradeThenCompletesSameMerchantPlanOwner()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners buyer, out DailyV1NpcRuntimeOwners seller,
            out DailyV1ItemDefinition item);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(value =>
            string.Equals(value.RuntimeId, seller.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        DailyV1ActionDefinition sell = graph.Definitions.Actions["VENDER_MERCADORIA"];
        Assert.That(graph.TryInstallMerchantTradePlan(seller, item, city.RuntimeId,
            graph.Cities.First(value => !ReferenceEquals(value, city)).RuntimeId,
            2, 10f, "decision/merchant-buy"), Is.True);
        float currentMarketPrice = city.Market.GetPrice(item);
        Assert.That(seller.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            city.LocationRuntimeId, item, currentMarketPrice, 20, 20L, 20L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(DailyV1MerchantActionPlanning.TryPlanSelectedPlannedLocalBuyer(
            graph, seller, sell, 20L, 75f, out DailyV1MerchantLocalSalePlan plan), Is.True);
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.SelectedPayload.TargetNpcRuntimeId, Is.EqualTo(buyer.RuntimeId));
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        NpcDecisionStore decisions = new NpcDecisionStore();
        NpcDecisionRecord decision = DailyV1DecisionRecordOperation.RecordSelectedAction(
            allocator, new SimulationRecordSequence(), decisions, 20L, graph, seller,
            plan.SelectedPayload, NpcDecisionType.TradeSale, NpcDecisionOrigin.Autonomous);
        Assert.That(decision, Is.Not.Null);
        plan.SelectedPayload.SetOriginDecisionId(decision.DecisionId);
        Assert.That(graph.TryCommitCurrentActionSlot(seller, sell, plan.SelectedPayload, null), Is.True);
        Assert.That(plan.SelectedPayload.TargetCityRuntimeId, Is.Null);
        Assert.That(buyer.Travel.CurrentCityRuntimeId, Is.EqualTo(seller.Travel.CurrentCityRuntimeId));
        Assert.That(buyer.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(seller.MerchantTradePlan.IsActive, Is.True);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecutePlannedNpcSaleSlot(
            graph, seller, buyer, new RecordingBoundary(true), null,
            () => true, () => { }, out var outcome), Is.True);

        Assert.That(outcome.Succeeded, Is.True);
        Assert.That(outcome.SoldToNpc, Is.True);
        Assert.That(outcome.Quantity, Is.EqualTo(plan.Amount));
        Assert.That(outcome.PlanCompleted, Is.True);
        Assert.That(outcome.NpcTrade.Succeeded, Is.True);
        Assert.That(outcome.NpcTrade.BuyerAccountMutationCount, Is.EqualTo(1));
        Assert.That(outcome.NpcTrade.SellerInventoryMutationCount, Is.EqualTo(1));
        Assert.That(seller.MerchantTradePlan.IsActive, Is.False);
        Assert.That(seller.MerchantTradePlan.HasData, Is.False);
        Assert.That(seller.MerchantTradePlan.Revision, Is.EqualTo(2));
        Assert.That(seller.ActionSlot.CurrentPayload.OriginDecisionId, Is.EqualTo(decision.DecisionId));
        Assert.That(decisions.Decisions.Single().TargetRuntimeIds.Single(), Is.EqualTo(buyer.RuntimeId));
    }

    [Test]
    public void PlannedSalePlanAdmissionRejectsBeforeAnyTradeOwnerMutation()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners buyer, out DailyV1NpcRuntimeOwners seller,
            out DailyV1ItemDefinition item);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(value =>
            string.Equals(value.RuntimeId, seller.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        DailyV1ActionDefinition sell = graph.Definitions.Actions["VENDER_MERCADORIA"];
        Assert.That(graph.TryInstallMerchantTradePlan(seller, item, city.RuntimeId,
            graph.Cities.First(value => !ReferenceEquals(value, city)).RuntimeId,
            2, 10f, "decision/merchant-buy"), Is.True);
        Assert.That(seller.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            city.LocationRuntimeId, item, city.Market.GetPrice(item), 20, 20L, 20L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(DailyV1MerchantActionPlanning.TryPlanSelectedPlannedLocalBuyer(
            graph, seller, sell, 20L, 75f, out DailyV1MerchantLocalSalePlan plan), Is.True);
        Assert.That(plan, Is.Not.Null);
        Assert.That(graph.TryCommitCurrentActionSlot(seller, sell, plan.SelectedPayload, null), Is.True);
        float buyerBalance = buyer.MoneyAccount.Balance;
        float sellerBalance = seller.MoneyAccount.Balance;
        int buyerInventory = buyer.Inventory.GetAmount(item);
        int sellerInventory = seller.Inventory.GetAmount(item);
        long planRevision = seller.MerchantTradePlan.Revision;
        RecordingBoundary boundary = new RecordingBoundary(true);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecutePlannedNpcSaleSlot(
            graph, seller, buyer, boundary, null, () => false, () => { }, out var outcome), Is.True);

        Assert.That(outcome.Succeeded, Is.False);
        Assert.That(boundary.Events, Is.Empty);
        Assert.That(buyer.MoneyAccount.Balance, Is.EqualTo(buyerBalance).Within(0.001f));
        Assert.That(seller.MoneyAccount.Balance, Is.EqualTo(sellerBalance).Within(0.001f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.EqualTo(buyerInventory));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(sellerInventory));
        Assert.That(seller.MerchantTradePlan.Revision, Is.EqualTo(planRevision));
        Assert.That(seller.MerchantTradePlan.IsActive, Is.True);
    }

    [Test]
    public void PlannedSaleAtMaxPlanRevisionRejectsBeforeAnyTradeOwnerMutation()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph originalGraph,
            out DailyV1NpcRuntimeOwners buyer, out DailyV1NpcRuntimeOwners originalSeller,
            out DailyV1ItemDefinition item);
        DailyV1CityRuntimeOwners city = originalGraph.Cities.Single(value =>
            string.Equals(value.RuntimeId, originalSeller.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        DailyV1CityRuntimeOwners target = originalGraph.Cities.First(value => !ReferenceEquals(value, city));
        Assert.That(DailyV1NpcMerchantTradePlanOwner.TryCreateFromOwnerSnapshot(
            originalGraph.Definitions, item, city.RuntimeId, target.RuntimeId,
            2, 2, 10f, 0, 0, "decision/max-plan", long.MaxValue,
            out DailyV1NpcMerchantTradePlanOwner maxRevisionPlan), Is.True);
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateBound(
            originalSeller.RuntimeId, originalGraph.Definitions, originalSeller.Definition,
            originalSeller.Status.Statuses, originalSeller.Inventory, originalSeller.MoneyAccount,
            originalSeller.CommercialKnowledge, maxRevisionPlan, originalSeller.SpatialKnowledge,
            out DailyV1NpcRuntimeOwners seller), Is.True);
        Assert.That(seller.Lifecycle.TryInstallState(originalSeller.Lifecycle.LifeState,
            originalSeller.Lifecycle.InjurySeverity, originalSeller.Lifecycle.LifeStateRevision), Is.True);
        Assert.That(seller.Travel.TryInstallState(originalSeller.Travel.CurrentLocationRuntimeId,
            originalSeller.Travel.CurrentCityRuntimeId, originalSeller.Travel.DestinationLocationRuntimeId,
            originalSeller.Travel.DestinationCityRuntimeId, originalSeller.Travel.TravelDaysRemaining,
            originalSeller.Travel.TravelDaysTotal, originalSeller.Travel.TravelRouteRuntimeId,
            originalSeller.Travel.TravelStartedToday, originalSeller.Travel.TravelOriginDecisionId,
            originalSeller.Travel.ActiveTravelPartyId, originalSeller.Travel.Revision), Is.True);
        List<DailyV1NpcRuntimeOwners> owners = originalGraph.Npcs.Select(value =>
            ReferenceEquals(value, originalSeller) ? seller : value).ToList();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(originalGraph.Definitions,
            originalGraph.Cities, owners, originalGraph.SpatialNetwork,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        DailyV1ActionDefinition sell = graph.Definitions.Actions["VENDER_MERCADORIA"];
        EffectiveMerchantTradeConfiguration tradeConfig = graph.Definitions.EffectiveConfiguration.MerchantTrade;
        float preference = DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(
            buyer.Definition, graph.Definitions, item, out float configuredPreference)
            ? UnityMathfCompatibility.Max(0.01f, configuredPreference) : 1f;
        float unitPrice = UnityMathfCompatibility.Max(0.01f, city.Market.GetPrice(item))
            * UnityMathfCompatibility.Clamp(tradeConfig.LocalWholesalePriceMultiplier
                + UnityMathfCompatibility.Min(0.15f,
                    UnityMathfCompatibility.Max(0f, preference - 1f) * 0.1f), 0.5f, 0.85f);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(graph.Definitions, sell,
            buyer.RuntimeId, null, item, 1, unitPrice, 1f, NpcTravelReason.None,
            0f, "decision/max-plan", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(seller, sell, payload, null), Is.True);
        float buyerBalance = buyer.MoneyAccount.Balance;
        float sellerBalance = seller.MoneyAccount.Balance;
        int buyerInventory = buyer.Inventory.GetAmount(item);
        int sellerInventory = seller.Inventory.GetAmount(item);
        RecordingBoundary boundary = new RecordingBoundary(true);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecutePlannedNpcSaleSlot(
            graph, seller, buyer, boundary, null, () => true, () => { }, out var outcome), Is.True);

        Assert.That(outcome.Succeeded, Is.False);
        Assert.That(boundary.Events, Is.Empty);
        Assert.That(buyer.MoneyAccount.Balance, Is.EqualTo(buyerBalance).Within(0.001f));
        Assert.That(seller.MoneyAccount.Balance, Is.EqualTo(sellerBalance).Within(0.001f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.EqualTo(buyerInventory));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(sellerInventory));
        Assert.That(seller.MerchantTradePlan.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(seller.MerchantTradePlan.IsActive, Is.True);
    }

    [Test]
    public void SelectedPlannedMarketSaleCommitsMarketAndPlanOwnersFromExactSlot()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out _, out DailyV1NpcRuntimeOwners seller, out DailyV1ItemDefinition item);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(value =>
            string.Equals(value.RuntimeId, seller.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        DailyV1ActionDefinition sell = graph.Definitions.Actions["VENDER_MERCADORIA"];
        Assert.That(seller.Inventory.TryAddItem(item, 1, 10f), Is.True);
        int waitThreshold = graph.Definitions.EffectiveConfiguration.MerchantTrade.MaxUnprofitablePlanWaitDays;
        Assert.That(seller.MerchantTradePlan.TryInstallState(item, city.RuntimeId,
            city.RuntimeId, 5, 5, 100f, waitThreshold, 0,
            "decision/market-liquidation"), Is.True);
        float observedPrice = city.Market.GetPrice(item);
        Assert.That(seller.CommercialKnowledge.RecordObservation(
            new DailyV1CommercialMarketObservation(city.LocationRuntimeId, item,
                observedPrice, 20, 20L, 20L,
                DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(sell, 75f);
        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedSellGoodsCandidate(
            graph, seller, candidate, 20L), Is.True);
        DailyV1NpcActionPayloadOwner payload = candidate.PreparedPayload;
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload.TargetNpcRuntimeId, Is.Null);
        Assert.That(payload.TargetCityRuntimeId, Is.EqualTo(city.RuntimeId));
        payload.SetOriginDecisionId("decision/market-liquidation");
        Assert.That(graph.TryCommitCurrentActionSlot(seller, sell, payload, null), Is.True);

        float balanceBefore = seller.MoneyAccount.Balance;
        int inventoryBefore = seller.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        long accountRevisionBefore = seller.MoneyAccount.Revision;
        long inventoryRevisionBefore = seller.Inventory.Revision;
        long marketRevisionBefore = city.Market.Revision;
        long planRevisionBefore = seller.MerchantTradePlan.Revision;
        RecordingSaleBoundary boundary = new RecordingSaleBoundary(true);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecutePlannedMarketSaleSlot(
            graph, seller, boundary, () => true, () => { }, out var result), Is.True);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.PartialCommit, Is.False);
        Assert.That(result.Quantity, Is.EqualTo(payload.Amount));
        Assert.That(result.UnitPrice, Is.EqualTo(city.Market.GetPrice(item)));
        Assert.That(result.PlanCompleted, Is.True);
        Assert.That(result.PlanTransitioned, Is.True);
        Assert.That(result.MarketSale.Commit.Succeeded, Is.True);
        Assert.That(boundary.Events, Is.EqualTo(new[] { "begin", "complete", "dispose" }));
        Assert.That(seller.MoneyAccount.Balance,
            Is.EqualTo(balanceBefore + result.UnitPrice * result.Quantity).Within(0.001f));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore - result.Quantity));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore + result.Quantity));
        Assert.That(seller.MoneyAccount.Revision, Is.EqualTo(accountRevisionBefore + 1));
        Assert.That(seller.Inventory.Revision, Is.EqualTo(inventoryRevisionBefore + 1));
        Assert.That(city.Market.Revision, Is.EqualTo(marketRevisionBefore + 1));
        Assert.That(seller.MerchantTradePlan.Revision, Is.EqualTo(planRevisionBefore + 1));
        Assert.That(seller.MerchantTradePlan.IsActive, Is.False);
        Assert.That(seller.ActionSlot.CurrentPayload.OriginDecisionId,
            Is.EqualTo("decision/market-liquidation"));
    }

    [Test]
    public void SelectedPlannedMarketSaleRejectsPlanAdmissionBeforeMarketBoundaryOrWrites()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out _, out DailyV1NpcRuntimeOwners seller, out DailyV1ItemDefinition item);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(value =>
            string.Equals(value.RuntimeId, seller.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        DailyV1ActionDefinition sell = graph.Definitions.Actions["VENDER_MERCADORIA"];
        Assert.That(seller.Inventory.TryAddItem(item, 1, 10f), Is.True);
        int waitThreshold = graph.Definitions.EffectiveConfiguration.MerchantTrade.MaxUnprofitablePlanWaitDays;
        Assert.That(seller.MerchantTradePlan.TryInstallState(item, city.RuntimeId,
            city.RuntimeId, 5, 5, 100f, waitThreshold, 0,
            "decision/market-liquidation-rejected"), Is.True);
        float observedPrice = city.Market.GetPrice(item);
        Assert.That(seller.CommercialKnowledge.RecordObservation(
            new DailyV1CommercialMarketObservation(city.LocationRuntimeId, item,
                observedPrice, 20, 20L, 20L,
                DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(sell, 75f);
        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedSellGoodsCandidate(
            graph, seller, candidate, 20L), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(
            seller, sell, candidate.PreparedPayload, null), Is.True);

        float balanceBefore = seller.MoneyAccount.Balance;
        int inventoryBefore = seller.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        long accountRevisionBefore = seller.MoneyAccount.Revision;
        long inventoryRevisionBefore = seller.Inventory.Revision;
        long marketRevisionBefore = city.Market.Revision;
        long planRevisionBefore = seller.MerchantTradePlan.Revision;
        RecordingSaleBoundary boundary = new RecordingSaleBoundary(true);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecutePlannedMarketSaleSlot(
            graph, seller, boundary, () => false, () => { }, out var result), Is.True);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.PartialCommit, Is.False);
        Assert.That(result.MarketSale, Is.Null);
        Assert.That(boundary.Events, Is.Empty);
        Assert.That(seller.MoneyAccount.Balance, Is.EqualTo(balanceBefore).Within(0.001f));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore));
        Assert.That(seller.MoneyAccount.Revision, Is.EqualTo(accountRevisionBefore));
        Assert.That(seller.Inventory.Revision, Is.EqualTo(inventoryRevisionBefore));
        Assert.That(city.Market.Revision, Is.EqualTo(marketRevisionBefore));
        Assert.That(seller.MerchantTradePlan.Revision, Is.EqualTo(planRevisionBefore));
        Assert.That(seller.MerchantTradePlan.IsActive, Is.True);
    }

    [Test]
    public void SelectedPlannedMarketSaleMarksUnexpectedPostSalePlanRejectionAsPartialCommit()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out _, out DailyV1NpcRuntimeOwners seller, out DailyV1ItemDefinition item);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(value =>
            string.Equals(value.RuntimeId, seller.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        DailyV1ActionDefinition sell = graph.Definitions.Actions["VENDER_MERCADORIA"];
        Assert.That(seller.Inventory.TryAddItem(item, 1, 10f), Is.True);
        int waitThreshold = graph.Definitions.EffectiveConfiguration.MerchantTrade.MaxUnprofitablePlanWaitDays;
        Assert.That(seller.MerchantTradePlan.TryInstallState(item, city.RuntimeId,
            city.RuntimeId, 5, 5, 100f, waitThreshold, 0,
            "decision/market-liquidation-partial"), Is.True);
        float observedPrice = city.Market.GetPrice(item);
        Assert.That(seller.CommercialKnowledge.RecordObservation(
            new DailyV1CommercialMarketObservation(city.LocationRuntimeId, item,
                observedPrice, 20, 20L, 20L,
                DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(sell, 75f);
        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedSellGoodsCandidate(
            graph, seller, candidate, 20L), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(
            seller, sell, candidate.PreparedPayload, null), Is.True);

        int admissionCalls = 0;
        RecordingSaleBoundary boundary = new RecordingSaleBoundary(true);
        Assert.That(DailyV1SelectedActionExecutionStage.TryExecutePlannedMarketSaleSlot(
            graph, seller, boundary,
            () => ++admissionCalls == 1,
            () => { }, out var result), Is.True);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.PartialCommit, Is.True);
        Assert.That(result.MarketSale.Commit.Succeeded, Is.True);
        Assert.That(result.PlanTransitioned, Is.False);
        Assert.That(seller.MerchantTradePlan.IsActive, Is.True);
        Assert.That(seller.Inventory.GetAmount(item), Is.LessThan(4));
        Assert.That(admissionCalls, Is.EqualTo(2));
        Assert.That(boundary.Events, Is.EqualTo(new[] { "begin", "complete", "dispose" }));
    }

    private static void CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1NpcRuntimeOwners buyer, out DailyV1NpcRuntimeOwners seller,
        out DailyV1ItemDefinition item)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder = new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            id => builder.ResolveUniqueStartingCity(id)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out graph), Is.True);
        buyer = graph.Npcs.Single(value => value.Definition.DefinitionId == "PEDRO");
        seller = graph.Npcs.Single(value => value.Definition.DefinitionId == "JORGE");
        item = definitions.Items["FERRO"];
        Assert.That(seller.Inventory.TryAddItem(item, 4, 12f), Is.True);
    }

    private sealed class RecordingBoundary : IDailyV1SelectedNpcTradeBoundary
    {
        private readonly bool admit;
        internal List<string> Events { get; } = new List<string>();
        internal RecordingBoundary(bool admit) => this.admit = admit;
        public bool TryBegin() { Events.Add("begin"); return admit; }
        public void NotifyMutation(DailyV1SelectedNpcTradeMutation mutation) => Events.Add(mutation switch
        {
            DailyV1SelectedNpcTradeMutation.BuyerAccount => "buyer-account",
            DailyV1SelectedNpcTradeMutation.SellerAccount => "seller-account",
            DailyV1SelectedNpcTradeMutation.BuyerInventory => "buyer-inventory",
            DailyV1SelectedNpcTradeMutation.SellerInventory => "seller-inventory",
            _ => "unknown"
        });
        public void Dispose() => Events.Add("dispose");
    }

    private sealed class RecordingSaleBoundary : IDailyV1SelectedOpenMarketSaleBoundary
    {
        private readonly bool admit;
        internal List<string> Events { get; } = new List<string>();
        internal RecordingSaleBoundary(bool admit) => this.admit = admit;
        public bool TryBegin() { Events.Add("begin"); return admit; }
        public void Complete(DailyV1SelectedOpenMarketSaleCommitResult result) => Events.Add("complete");
        public void Dispose() => Events.Add("dispose");
    }
}
