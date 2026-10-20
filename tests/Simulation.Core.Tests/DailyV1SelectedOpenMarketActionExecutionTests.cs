using System;
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

    private sealed class RecordingSaleBoundary : IDailyV1SelectedOpenMarketSaleBoundary
    {
        internal int BeginCalls { get; private set; }
        internal int CompleteCalls { get; private set; }
        internal int DisposeCalls { get; private set; }
        public bool TryBegin() { BeginCalls++; return true; }
        public void Complete(DailyV1SelectedOpenMarketSaleCommitResult result) => CompleteCalls++;
        public void Dispose() => DisposeCalls++;
    }
}
