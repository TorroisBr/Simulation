using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedOpenMarketPurchaseTests
{
    [Test]
    public void CoreExecutionBuysAgainstSelectedOwnersAndCompletesBoundary()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners npc, out DailyV1CityRuntimeOwners city,
            out DailyV1ItemDefinition item);
        float balance = npc.MoneyAccount.Balance;
        int stock = city.Market.GetAmount(item);
        int inventory = npc.Inventory.GetAmount(item);
        PurchaseBoundary boundary = new PurchaseBoundary(true, true);

        DailyV1SelectedOpenMarketPurchaseExecution execution =
            DailyV1SelectedOpenMarketPurchaseExecution.TryExecute(
                graph, npc, city.Market, item, 1, boundary);

        Assert.That(execution.Plan, Is.Not.Null);
        Assert.That(execution.Commit.Succeeded, Is.True);
        Assert.That(boundary.BeginCalls, Is.EqualTo(1));
        Assert.That(boundary.CompleteCalls, Is.EqualTo(1));
        Assert.That(boundary.DisposeCalls, Is.EqualTo(1));
        Assert.That(boundary.LastResult, Is.SameAs(execution.Commit));
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(balance - execution.Plan.TotalPrice).Within(0.001f));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(stock - execution.Plan.Quantity));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(inventory + execution.Plan.Quantity));
    }

    [Test]
    public void CoreExecutionPreservesRejectedAdmissionWithoutMutatingOwners()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners npc, out DailyV1CityRuntimeOwners city,
            out DailyV1ItemDefinition item);
        float balance = npc.MoneyAccount.Balance;
        long accountRevision = npc.MoneyAccount.Revision;
        int stock = city.Market.GetAmount(item);
        int inventory = npc.Inventory.GetAmount(item);
        PurchaseBoundary boundary = new PurchaseBoundary(false, true);

        DailyV1SelectedOpenMarketPurchaseExecution execution =
            DailyV1SelectedOpenMarketPurchaseExecution.TryExecute(
                graph, npc, city.Market, item, 1, boundary);

        Assert.That(execution.Commit.Failure,
            Is.EqualTo(DailyV1SelectedOpenMarketPurchaseFailure.AdmissionRejected));
        Assert.That(boundary.CompleteCalls, Is.EqualTo(1));
        Assert.That(boundary.DisposeCalls, Is.EqualTo(1));
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(balance));
        Assert.That(npc.MoneyAccount.Revision, Is.EqualTo(accountRevision));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(stock));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(inventory));
    }

    [Test]
    public void InventoryRejectionRefundsDebitAndRestoresMarketStock()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners npc, out DailyV1CityRuntimeOwners city,
            out DailyV1ItemDefinition item);
        float balance = npc.MoneyAccount.Balance;
        int stock = city.Market.GetAmount(item);
        long accountRevision = npc.MoneyAccount.Revision;
        Assert.That(npc.Inventory.TryBindMutationBoundary(() => false, () => { }), Is.True);
        PurchaseBoundary boundary = new PurchaseBoundary(true, true);

        DailyV1SelectedOpenMarketPurchaseExecution execution =
            DailyV1SelectedOpenMarketPurchaseExecution.TryExecute(
                graph, npc, city.Market, item, 1, boundary);

        Assert.That(execution.Commit.Succeeded, Is.False);
        Assert.That(execution.Commit.PartialCommit, Is.True);
        Assert.That(execution.Commit.BuyerAccountMutationCount, Is.EqualTo(1));
        Assert.That(execution.Commit.MarketMutationCount, Is.EqualTo(1));
        Assert.That(boundary.LastResult, Is.SameAs(execution.Commit));
        Assert.That(boundary.DisposeCalls, Is.EqualTo(1));
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(balance - execution.Plan.TotalPrice).Within(0.001f));
        Assert.That(npc.MoneyAccount.Revision, Is.EqualTo(accountRevision + 1));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(stock - execution.Plan.Quantity));
    }

    private static void CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1NpcRuntimeOwners npc, out DailyV1CityRuntimeOwners city,
        out DailyV1ItemDefinition item)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder = new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out graph), Is.True);
        city = graph.Cities.Single(owner => owner.Definition.DefinitionId == "CAMPO_VERDE");
        npc = graph.Npcs.Single(owner => owner.Definition.DefinitionId == "PEDRO");
        item = definitions.Items["TECIDO"];
        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(city.Market.GetAmount(item), Is.GreaterThan(0));
    }

    private sealed class PurchaseBoundary : IDailyV1SelectedOpenMarketPurchaseBoundary
    {
        private readonly bool allow;
        public bool CanUseAccountBacked { get; }
        internal int BeginCalls { get; private set; }
        internal int CompleteCalls { get; private set; }
        internal int DisposeCalls { get; private set; }
        internal DailyV1SelectedOpenMarketPurchaseCommitResult LastResult { get; private set; }
        internal PurchaseBoundary(bool allow, bool canUseAccountBacked)
        { this.allow = allow; CanUseAccountBacked = canUseAccountBacked; }
        public bool TryBegin() { BeginCalls++; return allow; }
        public void Complete(DailyV1SelectedOpenMarketPurchaseCommitResult result)
        { CompleteCalls++; LastResult = result; }
        public void Dispose() { DisposeCalls++; }
    }
}
