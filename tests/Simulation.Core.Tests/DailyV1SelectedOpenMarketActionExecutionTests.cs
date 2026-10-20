using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedOpenMarketActionExecutionTests
{
    [Test]
    public void UnplannedSaleTradeProfitUsesRawPreSaleAverageCostEvenWhenEvidenceUsesBasePrice()
    {
        float evidencePurchasePrice = 100f;
        float preSaleAverageUnitCost = 0f;
        float projectedProfit = DailyV1SelectedMarketSaleProfit.Calculate(
            preSaleAverageUnitCost, realizedUnitPrice: 12f, quantity: 2);

        Assert.That(projectedProfit, Is.EqualTo(24f));
        Assert.That(projectedProfit, Is.Not.EqualTo((12f - evidencePurchasePrice) * 2));
    }

    [Test]
    public void SelectedLocalSalePayloadCommitsExactOwnersThroughCoreOperation()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder = new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            id => builder.ResolveUniqueStartingCity(id)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(value => value.Definition.DefinitionId == "CAMPO_VERDE");
        DailyV1NpcRuntimeOwners actor = graph.Npcs.Single(value => value.Definition.DefinitionId == "PEDRO");
        DailyV1ActionDefinition sell = definitions.Actions["VENDER_MERCADORIA"];
        DailyV1ItemDefinition item = definitions.Items["TECIDO"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, sell, null,
            city.RuntimeId, item, 1, 1f, 1f, NpcTravelReason.None, 0f,
            "decision/sale", null, null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, sell, payload, null), Is.True);
        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        RecordingSaleBoundary boundary = new RecordingSaleBoundary();

        bool succeeded = DailyV1SelectedActionExecutionStage.TryExecuteOpenMarketSlot(
            graph, actor, boundary, null, out var sale, out var purchase);

        Assert.That(succeeded, Is.True);
        Assert.That(purchase, Is.Null);
        Assert.That(sale.Commit.Succeeded, Is.True);
        Assert.That(boundary.BeginCalls, Is.EqualTo(1));
        Assert.That(boundary.CompleteCalls, Is.EqualTo(1));
        Assert.That(boundary.DisposeCalls, Is.EqualTo(1));
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore - sale.Plan.Quantity));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore + sale.Plan.Quantity));
    }

    [Test]
    public void DecisionRecordThenActionSlotThenSelectedSaleEffectUsesOnePayloadIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder = new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            id => builder.ResolveUniqueStartingCity(id)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(value => value.Definition.DefinitionId == "CAMPO_VERDE");
        DailyV1NpcRuntimeOwners actor = graph.Npcs.Single(value => value.Definition.DefinitionId == "PEDRO");
        DailyV1ActionDefinition action = definitions.Actions["VENDER_MERCADORIA"];
        DailyV1ItemDefinition item = definitions.Items["TECIDO"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, action, null,
            city.RuntimeId, item, 1, 1f, 1f, NpcTravelReason.None, 0f,
            null, null, null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        NpcDecisionStore records = new NpcDecisionStore();
        NpcDecisionRecord record = DailyV1DecisionRecordOperation.RecordSelectedAction(
            allocator, new SimulationRecordSequence(), records, 0L, graph, actor,
            payload, NpcDecisionType.Action, NpcDecisionOrigin.Autonomous);
        Assert.That(record, Is.Not.Null);
        payload.SetOriginDecisionId(record.DecisionId);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, payload, null), Is.True);
        Assert.That(actor.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(actor.ActionSlot.CurrentPayload, Is.SameAs(payload));
        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        RecordingSaleBoundary boundary = new RecordingSaleBoundary();

        bool succeeded = DailyV1SelectedActionExecutionStage.TryExecuteOpenMarketSlot(
            graph, actor, boundary, null, out var sale, out _);

        Assert.That(succeeded, Is.True);
        Assert.That(record.ActionDefinitionId, Is.EqualTo(actor.ActionSlot.CurrentAction.DefinitionId));
        Assert.That(actor.ActionSlot.CurrentPayload.OriginDecisionId, Is.EqualTo(record.DecisionId));
        Assert.That(sale.Commit.Succeeded, Is.True);
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore - sale.Plan.Quantity));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore + sale.Plan.Quantity));
    }

    [Test]
    public void SelectedMarketOperationRejectsPayloadForAnotherCityBeforeAdmission()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder = new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            id => builder.ResolveUniqueStartingCity(id)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(value => value.Definition.DefinitionId == "CAMPO_VERDE");
        DailyV1CityRuntimeOwners otherCity = graph.Cities.First(value => !ReferenceEquals(value, city));
        DailyV1NpcRuntimeOwners actor = graph.Npcs.Single(value => value.Definition.DefinitionId == "PEDRO");
        DailyV1ActionDefinition sell = definitions.Actions["VENDER_MERCADORIA"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, sell, null,
            otherCity.RuntimeId, definitions.Items["TECIDO"], 1, 1f, 1f,
            NpcTravelReason.None, 0f, "decision/sale", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        RecordingSaleBoundary boundary = new RecordingSaleBoundary();

        bool succeeded = DailyV1SelectedActionExecutionStage.TryExecuteOpenMarketSlot(
            graph, actor, boundary, null, out var sale, out _);

        Assert.That(succeeded, Is.False);
        Assert.That(sale, Is.Null);
        Assert.That(boundary.BeginCalls, Is.Zero);
    }

    [Test]
    public void SelectedMarketDispatcherRollsExactSlotBeforeP12BoundaryAndSale()
    {
        CreateActionRollFixture(1f, out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city,
            out DailyV1ActionDefinition sell, out DailyV1ItemDefinition item);
        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.25f);
        RecordingSaleBoundary boundary = new RecordingSaleBoundary(order);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedOpenMarketActionSlot(
            graph, actor, sell, random, 41L, boundary, null,
            out DailyV1SelectedActionExecutionStage.SelectedOpenMarketActionResult result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Sale.Commit.Succeeded, Is.True);
        Assert.That(order, Is.EqualTo(new[] { "roll", "p12-begin", "p12-complete", "p12-dispose" }));
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore - result.Sale.Plan.Quantity));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore + result.Sale.Plan.Quantity));
    }

    [Test]
    public void SelectedMarketDispatcherStopsAfterFailedRollBeforeP12OrOwnerMutation()
    {
        CreateActionRollFixture(0.2f, out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city,
            out DailyV1ActionDefinition sell, out DailyV1ItemDefinition item);
        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.9f);
        RecordingSaleBoundary boundary = new RecordingSaleBoundary(order);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedOpenMarketActionSlot(
            graph, actor, sell, random, 42L, boundary, null,
            out DailyV1SelectedActionExecutionStage.SelectedOpenMarketActionResult result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.False);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(random.Calls, Is.EqualTo(1));
        Assert.That(order, Is.EqualTo(new[] { "roll" }));
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore));
    }

    [Test]
    public void SelectedActionDispatchRoutesOneLocalMarketSaleAndHandlesItsSingleRoll()
    {
        CreateActionRollFixture(1f, out _,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city,
            out DailyV1ActionDefinition sell, out DailyV1ItemDefinition item);
        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.25f);
        RecordingSaleBoundary saleBoundary = new RecordingSaleBoundary(order);
        RecordingActionDispatchBoundary boundary =
            new RecordingActionDispatchBoundary(saleBoundary, null);

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
            graph, actor, random, 41L, boundary,
            out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.OpenMarket));
        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(random.Calls, Is.EqualTo(1));
        Assert.That(order, Is.EqualTo(new[] { "roll", "p12-begin", "p12-complete", "p12-dispose" }));
        Assert.That(saleBoundary.BeginCalls, Is.EqualTo(1));
        Assert.That(saleBoundary.CompleteCalls, Is.EqualTo(1));
        Assert.That(saleBoundary.DisposeCalls, Is.EqualTo(1));
        Assert.That(actor.Inventory.GetAmount(item),
            Is.EqualTo(inventoryBefore - result.OpenMarket.Sale.Plan.Quantity));
        Assert.That(city.Market.GetAmount(item),
            Is.EqualTo(marketBefore + result.OpenMarket.Sale.Plan.Quantity));
    }

    [Test]
    public void SelectedActionDispatchReturnsHandledFailureAfterTheSingleFailedRoll()
    {
        CreateActionRollFixture(0.2f, out _,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city,
            out _, out DailyV1ItemDefinition item);
        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.9f);
        RecordingSaleBoundary saleBoundary = new RecordingSaleBoundary(order);
        RecordingActionDispatchBoundary boundary =
            new RecordingActionDispatchBoundary(saleBoundary, null);

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
            graph, actor, random, 42L, boundary,
            out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.False);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(random.Calls, Is.EqualTo(1));
        Assert.That(order, Is.EqualTo(new[] { "roll" }));
        Assert.That(saleBoundary.BeginCalls, Is.Zero);
        Assert.That(saleBoundary.CompleteCalls, Is.Zero);
        Assert.That(saleBoundary.DisposeCalls, Is.Zero);
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore));
    }

    [Test]
    public void SelectedMerchantDispatchHandlesUnavailableMarketBoundaryWithoutProviderFallback()
    {
        CreateActionRollFixture(1f, out _,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city,
            out _, out DailyV1ItemDefinition item);
        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        float balanceBefore = actor.MoneyAccount.Balance;
        long inventoryRevisionBefore = actor.Inventory.Revision;
        long marketRevisionBefore = city.Market.Revision;
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.25f);
        RecordingSaleBoundary saleBoundary = new RecordingSaleBoundary(order);
        RecordingActionDispatchBoundary boundary =
            new RecordingActionDispatchBoundary(saleBoundary, null)
            { AllowMarketBoundaries = false };

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
            graph, actor, random, 47L, boundary,
            out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.OpenMarket));
        Assert.That(result.ActionRollSucceeded, Is.False);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(random.Calls, Is.Zero);
        Assert.That(boundary.BoundaryResolutionCalls, Is.EqualTo(1));
        Assert.That(saleBoundary.BeginCalls, Is.Zero);
        Assert.That(saleBoundary.CompleteCalls, Is.Zero);
        Assert.That(saleBoundary.DisposeCalls, Is.Zero);
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore));
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balanceBefore));
        Assert.That(actor.Inventory.Revision, Is.EqualTo(inventoryRevisionBefore));
        Assert.That(city.Market.Revision, Is.EqualTo(marketRevisionBefore));
    }

    [Test]
    public void SelectedMerchantDispatchHandlesMissingBoundaryWithoutProviderFallback()
    {
        CreateActionRollFixture(1f, out _,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city,
            out _, out DailyV1ItemDefinition item);
        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        long accountRevisionBefore = actor.MoneyAccount.Revision;
        FixedActionRandom random = new FixedActionRandom(new List<string>(), 0.25f);

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
            graph, actor, random, 49L, null,
            out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.OpenMarket));
        Assert.That(result.ActionRollSucceeded, Is.False);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(random.Calls, Is.Zero);
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore));
        Assert.That(actor.MoneyAccount.Revision, Is.EqualTo(accountRevisionBefore));
    }

    [Test]
    public void SelectedRemoteSellIsHandledFailureInsteadOfUnityProviderFallback()
    {
        CreateActionRollFixture(1f, out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city,
            out DailyV1ActionDefinition sell, out DailyV1ItemDefinition item);
        DailyV1CityRuntimeOwners otherCity = graph.Cities.First(value => !ReferenceEquals(value, city));
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, sell, null,
            otherCity.RuntimeId, item, 1, 1f, 1f, NpcTravelReason.None, 0f,
            "decision/remote-sell", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, sell, payload, null), Is.True);
        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = otherCity.Market.GetAmount(item);
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.25f);
        RecordingSaleBoundary saleBoundary = new RecordingSaleBoundary(order);
        RecordingActionDispatchBoundary boundary =
            new RecordingActionDispatchBoundary(saleBoundary, null);

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
            graph, actor, random, 48L, boundary,
            out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.OpenMarket));
        Assert.That(result.ActionRollSucceeded, Is.False);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(random.Calls, Is.Zero);
        Assert.That(saleBoundary.BeginCalls, Is.Zero);
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(otherCity.Market.GetAmount(item), Is.EqualTo(marketBefore));
    }

    [Test]
    public void SelectedActionDispatchExecutesNormalActionFromTheExactCoreSlot()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder = new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            id => builder.ResolveUniqueStartingCity(id)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners actor = graph.Npcs.Single(value => value.Definition.DefinitionId == "PEDRO");
        DailyV1ActionDefinition rest = definitions.Actions["DESCANSAR"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, rest,
            null, null, null, 0, 0f, 1f, NpcTravelReason.None, 0f,
            "decision/rest", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, rest, payload, null), Is.True);

        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.25f);
        RecordingActionDispatchBoundary boundary =
            new RecordingActionDispatchBoundary(null, null);

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
            graph, actor, random, 44L, boundary,
            out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.Normal));
        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.NormalActionName, Is.EqualTo("DESCANSAR"));
        Assert.That(result.NormalActionLogText, Is.EqualTo("descansou."));
        Assert.That(random.Calls, Is.Zero);
        Assert.That(boundary.BoundaryResolutionCalls, Is.Zero);
    }

    [Test]
    public void SelectedHideActionWithoutHostBoundaryRemainsUnhandledBeforeRandomness()
    {
        CreateActionRollFixture(1f, out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out _, out _, out _);
        DailyV1ActionDefinition unsupported = definitions.Actions["ESCONDER_SE"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, unsupported,
            null, null, null, 0, 0f, 1f, NpcTravelReason.None, 0f,
            null, null, null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, unsupported, payload, null), Is.True);
        FixedActionRandom random = new FixedActionRandom(new List<string>(), 0.1f);
        RecordingActionDispatchBoundary boundary =
            new RecordingActionDispatchBoundary(null, null);

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
            graph, actor, random, 43L, boundary,
            out DailyV1SelectedActionDispatchResult result), Is.False);

        Assert.That(result, Is.Null);
        Assert.That(random.Calls, Is.Zero);
        Assert.That(boundary.BoundaryResolutionCalls, Is.EqualTo(1));
        Assert.That(boundary.CrossCityCompletionCalls, Is.Zero);
    }

    [Test]
    public void SelectedMarketDispatcherCompletesLocalPurchaseAfterTheSameActionRollBoundary()
    {
        CreateActionRollFixture(1f, out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city,
            out _, out DailyV1ItemDefinition item);
        DailyV1ActionDefinition buy = definitions.Actions["COMPRAR_MERCADORIA"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, buy, null,
            city.RuntimeId, item, 1, 1f, 1f, NpcTravelReason.None, 0f,
            "decision/selected-market-buy", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, buy, payload, null), Is.True);
        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.25f);
        RecordingPurchaseBoundary boundary = new RecordingPurchaseBoundary(order);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedOpenMarketActionSlot(
            graph, actor, buy, random, 44L, null, boundary,
            out DailyV1SelectedActionExecutionStage.SelectedOpenMarketActionResult result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Purchase.Commit.Succeeded, Is.True);
        Assert.That(order, Is.EqualTo(new[] { "roll", "p12-begin", "p12-complete", "p12-dispose" }));
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore + result.Purchase.Plan.Quantity));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore - result.Purchase.Plan.Quantity));
    }

    [Test]
    public void SelectedMarketDispatcherP12RefusalPrecedesSaleMutation()
    {
        CreateActionRollFixture(1f, out _,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city,
            out DailyV1ActionDefinition sell, out DailyV1ItemDefinition item);
        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        float balanceBefore = actor.MoneyAccount.Balance;
        long accountRevisionBefore = actor.MoneyAccount.Revision;
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.25f);
        RecordingSaleBoundary boundary = new RecordingSaleBoundary(order) { Allow = false };

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedOpenMarketActionSlot(
            graph, actor, sell, random, 45L, boundary, null,
            out DailyV1SelectedActionExecutionStage.SelectedOpenMarketActionResult result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Sale.Commit.Failure,
            Is.EqualTo(DailyV1SelectedOpenMarketSaleFailure.AdmissionRejected));
        Assert.That(order, Is.EqualTo(new[] { "roll", "p12-begin", "p12-complete", "p12-dispose" }));
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balanceBefore));
        Assert.That(actor.MoneyAccount.Revision, Is.EqualTo(accountRevisionBefore));
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore));
    }

    [Test]
    public void SelectedPlannedMarketSaleDispatcherRollsBeforePlanAdmissionAndMarketCommit()
    {
        CreateActionRollFixture(1f, out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners city,
            out DailyV1ActionDefinition sell, out DailyV1ItemDefinition item);
        int waitThreshold = definitions.EffectiveConfiguration.MerchantTrade.MaxUnprofitablePlanWaitDays;
        Assert.That(actor.MerchantTradePlan.TryInstallState(item, city.RuntimeId,
            city.RuntimeId, 5, 5, 100f, waitThreshold, 0,
            "decision/selected-market-plan"), Is.True);
        float observedPrice = city.Market.GetPrice(item);
        Assert.That(actor.CommercialKnowledge.RecordObservation(
            new DailyV1CommercialMarketObservation(city.LocationRuntimeId, item,
                observedPrice, 20, 20L, 20L,
                DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(sell, 75f);
        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedSellGoodsCandidate(
            graph, actor, candidate, 20L), Is.True);
        DailyV1NpcActionPayloadOwner payload = candidate.PreparedPayload;
        payload.SetOriginDecisionId("decision/selected-market-plan");
        Assert.That(graph.TryCommitCurrentActionSlot(actor, sell, payload, null), Is.True);

        int inventoryBefore = actor.Inventory.GetAmount(item);
        int marketBefore = city.Market.GetAmount(item);
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.25f);
        RecordingSaleBoundary boundary = new RecordingSaleBoundary(order);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedPlannedMarketSaleActionSlot(
            graph, actor, sell, random, 46L, boundary,
            () => { order.Add("plan-admit"); return true; },
            () => order.Add("plan-commit"),
            out DailyV1SelectedActionExecutionStage.SelectedPlannedMarketSaleActionResult result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Sale.PlanTransitioned, Is.True);
        Assert.That(order, Is.EqualTo(new[]
        {
            "roll", "plan-admit", "p12-begin", "p12-complete", "p12-dispose",
            "plan-admit", "plan-commit"
        }));
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore - result.Sale.Quantity));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(marketBefore + result.Sale.Quantity));
    }

    [Test]
    public void SelectedMarketDispatcherRejectsMismatchedActionBeforeRandomness()
    {
        CreateActionRollFixture(1f, out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out _,
            out DailyV1ActionDefinition sell, out _);
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.1f);
        RecordingSaleBoundary boundary = new RecordingSaleBoundary(order);
        DailyV1ActionDefinition wrongAction = definitions.Actions.Values.First(value =>
            value.Values.actionType?.Value == DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedOpenMarketActionSlot(
            graph, actor, wrongAction, random, 43L, boundary, null, out _), Is.False);

        Assert.That(random.Calls, Is.Zero);
        Assert.That(order, Is.Empty);
    }

    private static void CreateActionRollFixture(float successChance,
        out DailyV1EffectiveDefinitions definitions,
        out DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1NpcRuntimeOwners actor,
        out DailyV1CityRuntimeOwners city,
        out DailyV1ActionDefinition sell,
        out DailyV1ItemDefinition item)
    {
        DailyV1EffectiveDefinitions source = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        IEnumerable<DailyV1NpcActionData> actions = source.Actions.Values.Select(action =>
        {
            DailyV1NpcActionData values = action.Values;
            if (values.actionType?.Value != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
                && values.actionType?.Value != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue)
                return values;
            return new DailyV1NpcActionData(values.id, values.statusNecessariosParaFazerAcao,
                values.actionName, values.normalActionLogText, values.actionCategory, values.actionType,
                values.baseUtility, values.statusModifiers, true, successChance,
                values.statusToAdd, values.statusToRemove, values.targetStatusToAdd,
                values.targetStatusToRemove, values.crimeSettings);
        });
        definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            source.Configuration, source.EffectiveConfiguration, source.Calendar,
            source.Cities.Values.Select(value => value.Values),
            source.Items.Values.Select(value => value.Values),
            source.Npcs.Values.Select(value => value.Values),
            source.Jobs.Values.Select(value => value.Values), actions,
            source.Statuses.Values.Select(value => value.Values), source.StageOrder,
            source.ProfileContractIdentity, source.GeographyProfileContractIdentity,
            source.GeographyStageIdentity, source.HexCoordinateConvention,
            source.HexCoordinateCanonicalOrder);
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            id => builder.ResolveUniqueStartingCity(id)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out graph), Is.True);
        city = graph.Cities.Single(value => value.Definition.DefinitionId == "CAMPO_VERDE");
        actor = graph.Npcs.Single(value => value.Definition.DefinitionId == "PEDRO");
        sell = definitions.Actions["VENDER_MERCADORIA"];
        item = definitions.Items["TECIDO"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, sell, null,
            city.RuntimeId, item, 1, 1f, 1f, NpcTravelReason.None, 0f,
            "decision/selected-market", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, sell, payload, null), Is.True);
    }

    private sealed class FixedActionRandom : IAuthoritativeRandomSource
    {
        private readonly IList<string> order;
        private readonly float value;
        internal int Calls { get; private set; }
        internal FixedActionRandom(IList<string> order, float value)
        { this.order = order; this.value = value; }
        public float NextUnit(string streamKey, long drawIndex = 0L)
        { Calls++; order.Add("roll"); return value; }
        public DeterministicRandomStream CreateStream(string streamKey) => null;
    }

    private sealed class RecordingSaleBoundary : IDailyV1SelectedOpenMarketSaleBoundary
    {
        private readonly IList<string> order;
        internal RecordingSaleBoundary(IList<string> order = null) => this.order = order;
        internal int BeginCalls { get; private set; }
        internal int CompleteCalls { get; private set; }
        internal int DisposeCalls { get; private set; }
        internal bool Allow { get; set; } = true;
        public bool TryBegin()
        { BeginCalls++; order?.Add("p12-begin"); return Allow; }
        public void Complete(DailyV1SelectedOpenMarketSaleCommitResult result)
        { CompleteCalls++; order?.Add("p12-complete"); }
        public void Dispose() { DisposeCalls++; order?.Add("p12-dispose"); }
    }

    private sealed class RecordingPurchaseBoundary : IDailyV1SelectedOpenMarketPurchaseBoundary
    {
        private readonly IList<string> order;
        internal RecordingPurchaseBoundary(IList<string> order) => this.order = order;
        public bool CanUseAccountBacked => true;
        public bool TryBegin() { order.Add("p12-begin"); return true; }
        public void Complete(DailyV1SelectedOpenMarketPurchaseCommitResult result)
            => order.Add("p12-complete");
        public void Dispose() => order.Add("p12-dispose");
    }

    private sealed class RecordingActionDispatchBoundary : IDailyV1SelectedActionDispatchBoundary
    {
        private readonly IDailyV1SelectedOpenMarketSaleBoundary saleBoundary;
        private readonly IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary;
        internal int BoundaryResolutionCalls { get; private set; }
        internal int CrossCityCompletionCalls { get; private set; }
        internal bool AllowMarketBoundaries { get; set; } = true;

        internal RecordingActionDispatchBoundary(
            IDailyV1SelectedOpenMarketSaleBoundary saleBoundary,
            IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary)
        {
            this.saleBoundary = saleBoundary;
            this.purchaseBoundary = purchaseBoundary;
        }

        public bool TryGetSoloTravelBoundary(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action, out TravelPartyStore partyStore,
            out DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary boundary)
        {
            BoundaryResolutionCalls++;
            partyStore = null;
            boundary = null;
            return false;
        }

        public bool TryGetMarketBoundaries(DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedOpenMarketSaleBoundary sale,
            out IDailyV1SelectedOpenMarketPurchaseBoundary purchase)
        {
            BoundaryResolutionCalls++;
            sale = saleBoundary;
            purchase = purchaseBoundary;
            return AllowMarketBoundaries;
        }

        public bool TryGetNpcTradeBoundary(DailyV1NpcRuntimeOwners seller,
            DailyV1NpcRuntimeOwners buyer, out IDailyV1SelectedNpcTradeBoundary boundary)
        {
            BoundaryResolutionCalls++;
            boundary = null;
            return false;
        }

        public bool TryGetFleeCityActionBoundary(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary boundary)
        {
            BoundaryResolutionCalls++;
            boundary = null;
            return false;
        }

        public bool TryGetGuardArrestActionBoundary(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary boundary)
        {
            boundary = null;
            return false;
        }

        public bool TryGetStealActionBoundary(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary boundary)
        {
            BoundaryResolutionCalls++;
            boundary = null;
            return false;
        }

        public bool TryGetCrimeActionBoundary(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        {
            BoundaryResolutionCalls++;
            boundary = null;
            return false;
        }

        public bool CanAdmitMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
        public void PublishMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) { }
        public bool CanAdmitTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
        public void PublishTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) { }

        public void CompleteCrossCityBuy(DailyV1NpcRuntimeOwners actor,
            DailyV1SelectedCrossCityBuyResult result) => CrossCityCompletionCalls++;
    }
}
