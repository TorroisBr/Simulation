using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

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

    [Test]
    public void SelectedCityOwnerJoinRetainsExactPopulationMarketAndEconomyInstances()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        string runtimeId = "selected-city";
        MarketCounterpartyRuntime counterparty = MarketCounterpartyRuntime.CreateOpen(runtimeId);
        SettlementPopulationRuntime population = new SettlementPopulationRuntime(
            runtimeId, System.Math.Max(0, city.Values.initialPopulation));
        DailyV1MarketOwner market = new DailyV1MarketOwner(definitions, counterparty);
        Assert.That(market.TryInitializeFromCity(city), Is.True);
        PopulationEconomyRuntime economy = new PopulationEconomyRuntime(
            runtimeId, ConsumptionPaymentMode.Free, 0f, MarketLiquidityMode.Open);
        Assert.That(DailyV1CityNpcPresenceOwner.TryCreateInitial(
            runtimeId, out DailyV1CityNpcPresenceOwner npcPresence), Is.True);

        Assert.That(DailyV1CityRuntimeOwners.TryCreateBound(
            runtimeId, definitions, city, population, market, counterparty, economy,
            npcPresence,
            out DailyV1CityRuntimeOwners owners), Is.True);
        Assert.That(owners.Definitions, Is.SameAs(definitions));
        Assert.That(owners.Definition, Is.SameAs(city));
        Assert.That(owners.Population, Is.SameAs(population));
        Assert.That(owners.Market, Is.SameAs(market));
        Assert.That(owners.Counterparty, Is.SameAs(counterparty));
        Assert.That(owners.PopulationEconomy, Is.SameAs(economy));
        Assert.That(owners.NpcPresence, Is.SameAs(npcPresence));
    }

    [Test]
    public void SelectedCityOwnerJoinRejectsForeignOwnerGraphAndCounterpartyIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions otherDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        string runtimeId = "selected-city";
        MarketCounterpartyRuntime counterparty = MarketCounterpartyRuntime.CreateOpen(runtimeId);
        SettlementPopulationRuntime population = new SettlementPopulationRuntime(
            runtimeId, System.Math.Max(0, city.Values.initialPopulation));
        DailyV1MarketOwner foreignMarket = new DailyV1MarketOwner(otherDefinitions, counterparty);
        Assert.That(foreignMarket.TryInitializeFromCity(otherDefinitions.Cities[city.DefinitionId]), Is.True);
        PopulationEconomyRuntime economy = new PopulationEconomyRuntime(
            runtimeId, ConsumptionPaymentMode.Free, 0f, MarketLiquidityMode.Open);
        Assert.That(DailyV1CityNpcPresenceOwner.TryCreateInitial(
            runtimeId, out DailyV1CityNpcPresenceOwner npcPresence), Is.True);

        Assert.That(DailyV1CityRuntimeOwners.TryCreateBound(
            runtimeId, definitions, city, population, foreignMarket, counterparty, economy,
            npcPresence,
            out DailyV1CityRuntimeOwners owners), Is.False);
        Assert.That(owners, Is.Null);
        Assert.That(DailyV1CityRuntimeOwners.TryCreateBound(
            runtimeId, definitions, city, population, foreignMarket,
            MarketCounterpartyRuntime.CreateOpen("other-city"), economy, npcPresence, out owners), Is.False);
        Assert.That(owners, Is.Null);
    }

    [Test]
    public void SelectedRuntimeOwnerGraphRetainsExactCityAndNpcOwnerCompositions()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        DailyV1NpcDefinition npc = definitions.Npcs.Values.First();
        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial(
            "city-runtime", definitions, city, null, out DailyV1CityRuntimeOwners cityOwners), Is.True);
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, npc, 0f, out DailyV1NpcRuntimeOwners npcOwners), Is.True);

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, new[] { cityOwners }, new[] { npcOwners },
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.Definitions, Is.SameAs(definitions));
        Assert.That(graph.Cities, Has.Count.EqualTo(1));
        Assert.That(graph.Npcs, Has.Count.EqualTo(1));
        Assert.That(graph.Cities[0], Is.SameAs(cityOwners));
        Assert.That(graph.Npcs[0], Is.SameAs(npcOwners));
        Assert.That(cityOwners.NpcPresence.OrderedNpcRuntimeIds, Is.Empty);
        Assert.That(cityOwners.NpcPresence.Revision, Is.Zero);
    }

    [Test]
    public void CityPresenceOwnerPreservesOrderedIdentityAndAdvancesOnlyForMembershipChanges()
    {
        Assert.That(DailyV1CityNpcPresenceOwner.TryCreateInitial(
            "city-runtime", out DailyV1CityNpcPresenceOwner owner), Is.True);

        Assert.That(owner.TryAdd("npc-b"), Is.True);
        Assert.That(owner.TryAdd("npc-a"), Is.True);
        Assert.That(owner.TryAdd("npc-b"), Is.True, "Adding the exact existing identity is idempotent.");
        Assert.That(owner.OrderedNpcRuntimeIds, Is.EqualTo(new[] { "npc-b", "npc-a" }));
        Assert.That(owner.Revision, Is.EqualTo(2L));

        Assert.That(owner.TryRemove("npc-b"), Is.True);
        Assert.That(owner.OrderedNpcRuntimeIds, Is.EqualTo(new[] { "npc-a" }));
        Assert.That(owner.Revision, Is.EqualTo(3L));
        Assert.That(owner.TryRemove("npc-b"), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(3L), "Removing an absent identity is idempotent.");
    }

    [Test]
    public void CityPresenceOwnerRejectsAliasedSnapshotIdentityAndSaturatedMutation()
    {
        Assert.That(DailyV1CityNpcPresenceOwner.TryCreateBound(
            "city-runtime", new[] { "npc-b", "npc-a" }, 17L,
            out DailyV1CityNpcPresenceOwner restored), Is.True);
        Assert.That(restored.CityRuntimeId, Is.EqualTo("city-runtime"));
        Assert.That(restored.OrderedNpcRuntimeIds, Is.EqualTo(new[] { "npc-b", "npc-a" }));
        Assert.That(restored.Revision, Is.EqualTo(17L));

        Assert.That(DailyV1CityNpcPresenceOwner.TryCreateBound(
            "city-runtime", new[] { "npc-a", "npc-a" }, 0L,
            out DailyV1CityNpcPresenceOwner duplicate), Is.False);
        Assert.That(duplicate, Is.Null);

        Assert.That(DailyV1CityNpcPresenceOwner.TryCreateBound(
            "city-runtime", new[] { "npc-a" }, long.MaxValue,
            out DailyV1CityNpcPresenceOwner saturated), Is.True);
        Assert.That(saturated.TryAdd("npc-b"), Is.False);
        Assert.That(saturated.TryRemove("npc-a"), Is.False);
        Assert.That(saturated.OrderedNpcRuntimeIds, Is.EqualTo(new[] { "npc-a" }));
        Assert.That(saturated.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void SelectedRuntimeOwnerGraphRejectsDuplicateRuntimeIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        DailyV1NpcDefinition npc = definitions.Npcs.Values.First();
        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial(
            "shared-runtime", definitions, city, null, out DailyV1CityRuntimeOwners cityOwners), Is.True);
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "shared-runtime", definitions, npc, 0f, out DailyV1NpcRuntimeOwners npcOwners), Is.True);

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, new[] { cityOwners }, new[] { npcOwners },
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.False);
        Assert.That(graph, Is.Null);
    }

    [Test]
    public void SelectedRuntimeOwnerGraphRequiresTheExactOrderedOwnerInstancesAtComposition()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        DailyV1NpcDefinition[] npcDefinitions = definitions.Npcs.Values.Take(2).ToArray();
        Assert.That(npcDefinitions, Has.Length.EqualTo(2));
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            "city-runtime", definitions, city, null,
            out DailyV1CityRuntimeOwners cityOwners), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc-runtime-1", definitions, npcDefinitions[0], 0f,
            out DailyV1NpcRuntimeOwners npcOwner1), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc-runtime-2", definitions, npcDefinitions[1], 0f,
            out DailyV1NpcRuntimeOwners npcOwner2), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, new[] { cityOwners }, new[] { npcOwner1, npcOwner2 },
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        Assert.That(graph.MatchesExactOwnerInstances(
            new[] { cityOwners }, new[] { npcOwner1, npcOwner2 }), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            new[] { cityOwners }, new[] { npcOwner1, npcOwner2 },
            out DailyV1SelectedRuntimeOwnerGraph composedGraph), Is.True);
        Assert.That(composedGraph.MatchesExactOwnerInstances(
            new[] { cityOwners }, new[] { npcOwner1, npcOwner2 }), Is.True);
        Assert.That(graph.MatchesExactOwnerInstances(
            new[] { cityOwners }, new[] { npcOwner2, npcOwner1 }), Is.False);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc-runtime-replacement", definitions, npcDefinitions[1], 0f,
            out DailyV1NpcRuntimeOwners replacementOwner), Is.True);
        Assert.That(graph.MatchesExactOwnerInstances(
            new[] { cityOwners }, new[] { npcOwner1, replacementOwner }), Is.False);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            new[] { cityOwners }, new[] { npcOwner1, replacementOwner },
            out DailyV1SelectedRuntimeOwnerGraph replacementGraph), Is.True);
        Assert.That(replacementGraph.MatchesExactOwnerInstances(
            new[] { cityOwners }, new[] { npcOwner1, replacementOwner }), Is.True);
    }

    [Test]
    public void RuntimeBoundaryOwnsOneExactSelectedOwnerGraphBinding()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial(
            "city-runtime", definitions, definitions.Cities.Values.First(), null,
            out DailyV1CityRuntimeOwners cityOwners), Is.True);
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners npcOwners), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, new[] { cityOwners }, new[] { npcOwners },
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1ExecutionBoundaryState boundary = new DailyV1ExecutionBoundaryState();

        Assert.That(boundary.TryBindSelectedRuntimeOwnerGraph(graph), Is.True);
        Assert.That(boundary.SelectedRuntimeOwnerGraph, Is.SameAs(graph));
        Assert.That(boundary.HasExactSelectedRuntimeOwnerGraph(
            definitions, new[] { cityOwners }, new[] { npcOwners }), Is.True);
        Assert.That(boundary.TryBindSelectedRuntimeOwnerGraph(graph), Is.True,
            "Idempotent binding may retain the original graph instance.");

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, new[] { cityOwners }, new[] { npcOwners },
            out DailyV1SelectedRuntimeOwnerGraph aliasGraph), Is.True);
        Assert.That(boundary.TryBindSelectedRuntimeOwnerGraph(aliasGraph), Is.False,
            "A runtime boundary cannot replace its owner graph with an equal-looking graph.");
        Assert.That(boundary.HasExactSelectedRuntimeOwnerGraph(
            definitions, new[] { cityOwners }, System.Array.Empty<DailyV1NpcRuntimeOwners>()), Is.False);
    }

    [Test]
    public void SelectedExogenousDailyFlowUsesCoreOwnersAndPreservesMarketRevisionOrdering()
    {
        DailyV1EffectiveDefinitions definitions = CreateExogenousFlowDefinitions(
            "TRIGO", "TRIGO", 20, 100, 2f, 5, 1000);
        DailyV1CityDefinition city = definitions.Cities["CAMPO_VERDE"];
        DailyV1ItemDefinition item = definitions.Items["TRIGO"];
        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial(
            "city-runtime", definitions, city, null,
            out DailyV1CityRuntimeOwners owners), Is.True);
        List<string> output = new List<string>();

        LocalDailyMaterialFlowResult result = owners.SimulateSelectedExogenousDailyMaterialFlow(
            "location/1", 7, "calendar/1", (category, message) => output.Add(category + ":" + message));

        Assert.That(result.SettlementSemanticId, Is.EqualTo("settlement/1"));
        Assert.That(result.MarketStoreSemanticId, Is.EqualTo("store/1"));
        Assert.That(result.LocationId, Is.EqualTo("location/1"));
        Assert.That(result.ItemDefinitionId, Is.EqualTo(item.DefinitionId));
        Assert.That(result.OpeningStock, Is.EqualTo(20));
        Assert.That(result.ConfiguredSourceQuantity, Is.EqualTo(5));
        Assert.That(result.AppliedSourceQuantity, Is.EqualTo(5));
        Assert.That(result.RequestedFreeConsumption, Is.EqualTo(2));
        Assert.That(result.ActualFreeConsumption, Is.EqualTo(2));
        Assert.That(result.ClosingStock, Is.EqualTo(23));
        Assert.That(owners.Market.Revision, Is.EqualTo(2));
        Assert.That(owners.Market.GetAmount(item), Is.EqualTo(23));
        Assert.That(owners.Market.Items.Single().Item, Is.SameAs(item));
        Assert.That(output, Has.Count.EqualTo(2));
        Assert.That(output[0], Does.Contain("EconomyProduction"));
        Assert.That(output[1], Does.Contain("EconomyConsumption"));
    }

    [Test]
    public void SelectedExogenousDailyFlowRejectsMissingSourceMarketRowBeforeMutation()
    {
        DailyV1EffectiveDefinitions definitions = CreateExogenousFlowDefinitions(
            "TRIGO", "FERRO", 20, 100, 2f, 5, 1000);
        DailyV1CityDefinition city = definitions.Cities["CAMPO_VERDE"];
        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial(
            "city-runtime", definitions, city, null,
            out DailyV1CityRuntimeOwners owners), Is.True);
        long revision = owners.Market.Revision;
        int stock = owners.Market.Items.Sum(row => row.Amount);

        Assert.Throws<LocalDailyMaterialFlowRejectedException>(() =>
            owners.SimulateSelectedExogenousDailyMaterialFlow("location/1", 7, "calendar/1", null));

        Assert.That(owners.Market.Revision, Is.EqualTo(revision));
        Assert.That(owners.Market.Items.Sum(row => row.Amount), Is.EqualTo(stock));
    }

    private static DailyV1EffectiveDefinitions CreateExogenousFlowDefinitions(
        string sourceItemId, string marketItemId, int openingStock, int desiredStock,
        float consumptionPerThousand, int sourceQuantity, int population)
    {
        DailyV1EffectiveDefinitions baseline = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition baselineCity = baseline.Cities["CAMPO_VERDE"];
        DailyV1CityData city = new DailyV1CityData(
            baselineCity.Values.id, baselineCity.Values.cityName,
            "settlement/1", "location/1", "store/1",
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.ExogenousDaily, "ExogenousDaily"),
            population, baselineCity.Values.marketLiquidity,
            new DailyV1PopulationConsumptionConfig(
                new DailyV1EnumValue((int)ConsumptionPaymentMode.Free, "Free"), 0f),
            new[] { new DailyV1MarketItemConfig(marketItemId, openingStock, desiredStock, consumptionPerThousand) },
            new[] { new DailyV1CityProductionConfig(sourceItemId, sourceQuantity, 0, "source/1", "revision/1",
                new DailyV1EnumValue((int)CityProductionSourceKind.ExogenousDaily, "ExogenousDaily")) },
            baselineCity.Values.connections);

        return DailyV1EffectiveDefinitions.CreateSelectedSchema(
            baseline.Configuration, baseline.EffectiveConfiguration, baseline.Calendar,
            baseline.Cities.Values.Select(node => node.DefinitionId == city.id ? city : node.Values),
            baseline.Items.Values.Select(node => node.Values),
            baseline.Npcs.Values.Select(node => node.Values),
            baseline.Jobs.Values.Select(node => node.Values),
            baseline.Actions.Values.Select(node => node.Values),
            baseline.Statuses.Values.Select(node => node.Values),
            baseline.StageOrder, baseline.ProfileContractIdentity,
            baseline.GeographyProfileContractIdentity, baseline.GeographyStageIdentity,
            baseline.HexCoordinateConvention, baseline.HexCoordinateCanonicalOrder,
            baseline.Traits.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.DisplayName)),
            baseline.CapabilityAttributes.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.DisplayName)),
            baseline.ExplorableSites.Values.Select(node => node.Values));
    }
}
