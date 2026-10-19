using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedOpenMarketSaleTests
{
    [Test]
    public void PreparedSaleIsSideEffectFreeAndRejectedAdmissionCanRetry()
    {
        CreateFixture(out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners npc,
            out DailyV1CityRuntimeOwners city,
            out DailyV1ItemDefinition item);
        DailyV1InventoryOwner inventory = npc.Inventory;
        MoneyAccountRuntime account = npc.MoneyAccount;
        DailyV1MarketOwner market = city.Market;
        float balanceBefore = account.Balance;
        int inventoryBefore = inventory.GetAmount(item);
        int marketBefore = market.GetAmount(item);
        long accountRevisionBefore = account.Revision;
        long inventoryRevisionBefore = inventory.Revision;
        long marketRevisionBefore = market.Revision;

        Assert.That(graph.TryPrepareOpenMarketSale(npc, market, item, 1,
            out DailyV1SelectedOpenMarketSalePlan plan,
            out DailyV1SelectedOpenMarketSaleFailure preparationFailure), Is.True,
            preparationFailure.ToString());
        Assert.That(account.Balance, Is.EqualTo(balanceBefore));
        Assert.That(inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(market.GetAmount(item), Is.EqualTo(marketBefore));
        Assert.That(account.Revision, Is.EqualTo(accountRevisionBefore));
        Assert.That(inventory.Revision, Is.EqualTo(inventoryRevisionBefore));
        Assert.That(market.Revision, Is.EqualTo(marketRevisionBefore));

        int admissionCalls = 0;
        DailyV1SelectedOpenMarketSaleCommitResult rejected = plan.TryCommit(() =>
        {
            admissionCalls++;
            return false;
        });
        Assert.That(rejected.Succeeded, Is.False);
        Assert.That(rejected.Failure, Is.EqualTo(DailyV1SelectedOpenMarketSaleFailure.AdmissionRejected));
        Assert.That(admissionCalls, Is.EqualTo(1));
        Assert.That(account.Balance, Is.EqualTo(balanceBefore));
        Assert.That(inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(market.GetAmount(item), Is.EqualTo(marketBefore));
        Assert.That(account.Revision, Is.EqualTo(accountRevisionBefore));
        Assert.That(inventory.Revision, Is.EqualTo(inventoryRevisionBefore));
        Assert.That(market.Revision, Is.EqualTo(marketRevisionBefore));

        DailyV1SelectedOpenMarketSaleCommitResult accepted = plan.TryCommit(() => true);
        Assert.That(accepted.Succeeded, Is.True);
        Assert.That(account.Balance, Is.EqualTo(balanceBefore + plan.TotalPrice).Within(0.001f));
        Assert.That(inventory.GetAmount(item), Is.EqualTo(inventoryBefore - plan.Quantity));
        Assert.That(market.GetAmount(item), Is.EqualTo(marketBefore + plan.Quantity));
        Assert.That(account.Revision, Is.EqualTo(accountRevisionBefore + 1));
        Assert.That(inventory.Revision, Is.EqualTo(inventoryRevisionBefore + 1));
        Assert.That(market.Revision, Is.EqualTo(marketRevisionBefore + 1));
    }

    [Test]
    public void InventoryCommitRejectionRefundsAccountAndLeavesMarketUntouched()
    {
        CreateFixture(out _, out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners npc,
            out DailyV1CityRuntimeOwners city,
            out DailyV1ItemDefinition item);
        DailyV1InventoryOwner inventory = npc.Inventory;
        MoneyAccountRuntime account = npc.MoneyAccount;
        DailyV1MarketOwner market = city.Market;
        float balanceBefore = account.Balance;
        int inventoryBefore = inventory.GetAmount(item);
        int marketBefore = market.GetAmount(item);
        long accountRevisionBefore = account.Revision;
        long inventoryRevisionBefore = inventory.Revision;
        long marketRevisionBefore = market.Revision;
        Assert.That(inventory.TryBindMutationBoundary(() => false, () => { }), Is.True);
        Assert.That(graph.TryPrepareOpenMarketSale(npc, market, item, 1,
            out DailyV1SelectedOpenMarketSalePlan plan, out _), Is.True);

        DailyV1SelectedOpenMarketSaleCommitResult result = plan.TryCommit(() => true);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.PartialCommit, Is.False);
        Assert.That(result.AccountMutationCount, Is.EqualTo(2), "Credit and successful refund are both authoritative writes.");
        Assert.That(result.InventoryMutationCount, Is.Zero);
        Assert.That(account.Balance, Is.EqualTo(balanceBefore).Within(0.001f));
        Assert.That(inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(market.GetAmount(item), Is.EqualTo(marketBefore));
        Assert.That(account.Revision, Is.EqualTo(accountRevisionBefore + 2));
        Assert.That(inventory.Revision, Is.EqualTo(inventoryRevisionBefore));
        Assert.That(market.Revision, Is.EqualTo(marketRevisionBefore));
    }

    [Test]
    public void MarketCommitRejectionReportsPartialCommitAfterAccountAndInventoryWrites()
    {
        CreateFixture(out _, out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners npc,
            out DailyV1CityRuntimeOwners city,
            out DailyV1ItemDefinition item);
        DailyV1InventoryOwner inventory = npc.Inventory;
        MoneyAccountRuntime account = npc.MoneyAccount;
        DailyV1MarketOwner market = city.Market;
        float balanceBefore = account.Balance;
        int inventoryBefore = inventory.GetAmount(item);
        int marketBefore = market.GetAmount(item);
        long accountRevisionBefore = account.Revision;
        long inventoryRevisionBefore = inventory.Revision;
        long marketRevisionBefore = market.Revision;
        Assert.That(market.TryBindMutationBoundary(() => false, () => { }), Is.True);
        Assert.That(graph.TryPrepareOpenMarketSale(npc, market, item, 1,
            out DailyV1SelectedOpenMarketSalePlan plan, out _), Is.True);

        DailyV1SelectedOpenMarketSaleCommitResult result = plan.TryCommit(() => true);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.PartialCommit, Is.True);
        Assert.That(result.AccountMutationCount, Is.EqualTo(1));
        Assert.That(result.InventoryMutationCount, Is.EqualTo(1));
        Assert.That(account.Balance, Is.EqualTo(balanceBefore + plan.TotalPrice).Within(0.001f));
        Assert.That(inventory.GetAmount(item), Is.EqualTo(inventoryBefore - plan.Quantity));
        Assert.That(market.GetAmount(item), Is.EqualTo(marketBefore));
        Assert.That(account.Revision, Is.EqualTo(accountRevisionBefore + 1));
        Assert.That(inventory.Revision, Is.EqualTo(inventoryRevisionBefore + 1));
        Assert.That(market.Revision, Is.EqualTo(marketRevisionBefore));
    }

    [Test]
    public void AccountRevisionWithoutCompensationCapacityRejectsBeforeAnyWrite()
    {
        CreateFixture(out _, out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners npc,
            out DailyV1CityRuntimeOwners city,
            out DailyV1ItemDefinition item);
        MoneyAccountRuntime account = npc.MoneyAccount;
        DailyV1InventoryOwner inventory = npc.Inventory;
        DailyV1MarketOwner market = city.Market;
        Assert.That(account.CanInstall(0L, long.MaxValue - 1L, account.Balance), Is.True);
        account.InstallPrepared(0L, long.MaxValue - 1L, account.Balance);
        float balanceBefore = account.Balance;
        int inventoryBefore = inventory.GetAmount(item);
        int marketBefore = market.GetAmount(item);
        long inventoryRevisionBefore = inventory.Revision;
        long marketRevisionBefore = market.Revision;

        Assert.That(graph.TryPrepareOpenMarketSale(npc, market, item, 1,
            out DailyV1SelectedOpenMarketSalePlan plan,
            out DailyV1SelectedOpenMarketSaleFailure failure), Is.False);
        Assert.That(plan, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyV1SelectedOpenMarketSaleFailure.TransactionCommitFailed));
        Assert.That(account.Balance, Is.EqualTo(balanceBefore));
        Assert.That(account.Revision, Is.EqualTo(long.MaxValue - 1L));
        Assert.That(inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(inventory.Revision, Is.EqualTo(inventoryRevisionBefore));
        Assert.That(market.GetAmount(item), Is.EqualTo(marketBefore));
        Assert.That(market.Revision, Is.EqualTo(marketRevisionBefore));
    }

    private static void CreateFixture(
        out DailyV1EffectiveDefinitions definitions,
        out DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1NpcRuntimeOwners npc,
        out DailyV1CityRuntimeOwners city,
        out DailyV1ItemDefinition item)
    {
        definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(
            allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out graph), Is.True);

        city = graph.Cities.Single(owner => string.Equals(
            owner.Definition.DefinitionId, "CAMPO_VERDE", StringComparison.Ordinal));
        npc = graph.Npcs.Single(owner => string.Equals(
            owner.Definition.DefinitionId, "PEDRO", StringComparison.Ordinal));
        item = definitions.Items["TECIDO"];
        Assert.That(ReferenceEquals(npc.Definitions, definitions), Is.True);
        Assert.That(ReferenceEquals(city.Definitions, definitions), Is.True);
        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.EqualTo(city.LocationRuntimeId));
        Assert.That(npc.Inventory.GetAmount(item), Is.GreaterThan(0));
        Assert.That(city.Market.GetItem(item), Is.Not.Null);
    }
}
