using System;
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
            runtimeId, "location-selected-city", definitions, city, population, market, counterparty, economy,
            npcPresence,
            out DailyV1CityRuntimeOwners owners), Is.True);
        Assert.That(owners.LocationRuntimeId, Is.EqualTo("location-selected-city"));
        Assert.That(owners.Definitions, Is.SameAs(definitions));
        Assert.That(owners.Definition, Is.SameAs(city));
        Assert.That(owners.Population, Is.SameAs(population));
        Assert.That(owners.Market, Is.SameAs(market));
        Assert.That(owners.Counterparty, Is.SameAs(counterparty));
        Assert.That(owners.PopulationEconomy, Is.SameAs(economy));
        Assert.That(owners.NpcPresence, Is.SameAs(npcPresence));
    }

    [Test]
    public void RestoredP12DCityOwnersAreHydratedInCoreWithExactDefinitionsAndRevisions()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        DailyV1ItemDefinition item = definitions.Items[city.Values.marketItems[0].item];
        SettlementPopulationTransition transition = PopulationOwnerBridge.CreateDecodedTransition(
            "restored-city",
            2L,
            1190,
            new PopulationChangeSet(10, 0, 0, 0),
            10L,
            1200);
        PopulationOperationReceiptSnapshot receipt = new PopulationOperationReceiptSnapshot(
            "population-operation-1", "population-fingerprint-1", transition);
        Assert.That(DailyV1MarketItemState.TryCreate(item, 17, 60, 4.25f,
            out DailyV1MarketItemState row), Is.True);

        Assert.That(DailyV1CityRuntimeOwners.TryCreateRestoredP12DOwners(
            "restored-city",
            "restored-location",
            "restored-city",
            "population-restored-city",
            definitions,
            city,
            1200,
            3L,
            new[] { receipt },
            7L,
            new[] { row },
            11L,
            new[] { "npc-second", "npc-first" },
            9L,
            out DailyV1CityRuntimeOwners restored), Is.True);

        Assert.That(restored.Definitions, Is.SameAs(definitions));
        Assert.That(restored.Definition, Is.SameAs(city));
        Assert.That(restored.LocationRuntimeId, Is.EqualTo("restored-location"));
        Assert.That(restored.Population.CurrentPopulation, Is.EqualTo(1200));
        Assert.That(restored.Population.Revision, Is.EqualTo(3L));
        PopulationOwnerBridge.GetOperationReceiptCensus(
            restored.Population, out int receiptCount, out long receiptRevision);
        Assert.That(receiptCount, Is.EqualTo(1));
        Assert.That(receiptRevision, Is.EqualTo(7L));
        Assert.That(restored.Population.TryCaptureOperationReceiptSnapshot(
            out IReadOnlyList<PopulationOperationReceiptSnapshot> restoredReceipts, out long restoredReceiptRevision),
            Is.True);
        Assert.That(restoredReceiptRevision, Is.EqualTo(7L));
        Assert.That(restoredReceipts, Has.Count.EqualTo(1));
        Assert.That(restoredReceipts[0].Identity, Is.EqualTo(receipt.Identity));
        Assert.That(restoredReceipts[0].Fingerprint, Is.EqualTo(receipt.Fingerprint));
        Assert.That(restoredReceipts[0].Transition, Is.EqualTo(receipt.Transition));
        Assert.That(restoredReceipts[0].Transition, Is.Not.SameAs(receipt.Transition));
        Assert.That(restored.Market.Revision, Is.EqualTo(11L));
        Assert.That(restored.Market.Items, Has.Count.EqualTo(1));
        Assert.That(restored.Market.Items[0].Item, Is.SameAs(item));
        Assert.That(restored.Market.Items[0].Amount, Is.EqualTo(17));
        Assert.That(restored.Market.Items[0].DesiredAmount, Is.EqualTo(60));
        Assert.That(restored.Market.Items[0].CurrentPrice, Is.EqualTo(4.25f));
        Assert.That(restored.Counterparty.CounterpartyRuntimeId, Is.EqualTo("restored-city"));
        Assert.That(restored.Counterparty.LiquidityMode, Is.EqualTo(MarketLiquidityMode.Open));
        Assert.That(restored.Counterparty.MoneyAccount, Is.Null);
        Assert.That(restored.PopulationEconomy.PopulationEconomicRuntimeId,
            Is.EqualTo("population-restored-city"));
        Assert.That(restored.NpcPresence.OrderedNpcRuntimeIds,
            Is.EqualTo(new[] { "npc-second", "npc-first" }));
        Assert.That(restored.NpcPresence.Revision, Is.EqualTo(9L));
    }

    [Test]
    public void RestoredP12DCityOwnersRejectForeignDefinitionHandlesAndDuplicateMembership()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreignDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        DailyV1ItemDefinition foreignItem = foreignDefinitions.Items[city.Values.marketItems[0].item];
        Assert.That(DailyV1MarketItemState.TryCreate(foreignItem, 1, 1, 1f,
            out DailyV1MarketItemState foreignRow), Is.True);

        Assert.That(DailyV1CityRuntimeOwners.TryCreateRestoredP12DOwners(
            "restored-city", "restored-location", "restored-city", "population-restored-city",
            definitions, city, 1, 0L, System.Array.Empty<PopulationOperationReceiptSnapshot>(), 0L,
            new[] { foreignRow }, 0L, System.Array.Empty<string>(), 0L, out DailyV1CityRuntimeOwners rejectedForeign),
            Is.False);
        Assert.That(rejectedForeign, Is.Null);

        DailyV1ItemDefinition admittedItem = definitions.Items[city.Values.marketItems[0].item];
        Assert.That(DailyV1MarketItemState.TryCreate(admittedItem, 1, 1, 1f,
            out DailyV1MarketItemState admittedRow), Is.True);
        Assert.That(DailyV1CityRuntimeOwners.TryCreateRestoredP12DOwners(
            "restored-city", "restored-location", "restored-city", "population-restored-city",
            definitions, city, 1, 0L, System.Array.Empty<PopulationOperationReceiptSnapshot>(), 0L,
            new[] { admittedRow }, 0L, new[] { "npc-one", "npc-one" }, 1L,
            out DailyV1CityRuntimeOwners rejectedDuplicate), Is.False);
        Assert.That(rejectedDuplicate, Is.Null);
    }

    [Test]
    public void CoreSelectedCityCompositionOwnsFiniteReserveStateByCanonicalItemIdentity()
    {
        DailyV1CityData cityValues = new DailyV1CityData(
            "city/a", "City A", "settlement/a", "location/a", "market/a",
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.MixedSourcesDaily, "MixedSourcesDaily"),
            1000, null,
            new DailyV1PopulationConsumptionConfig(
                new DailyV1EnumValue((int)ConsumptionPaymentMode.Free, "Free"), 0f),
            new[] { new DailyV1MarketItemConfig("item/a", 5, 100, 4f) },
            new[]
            {
                new DailyV1CityProductionConfig("item/a", 2, 0, "source/exogenous", "content-v1",
                    new DailyV1EnumValue((int)CityProductionSourceKind.ExogenousDaily, "ExogenousDaily")),
                new DailyV1CityProductionConfig("item/a", 3, 7, "source/finite", "content-v1",
                    new DailyV1EnumValue((int)CityProductionSourceKind.FiniteReserveDaily, "FiniteReserveDaily"))
            },
            new List<DailyV1CityConnection>());
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            DailyV1TypedDefinitionGraphTests.CreateConfiguration(1f, new[] { "city/a" }),
            SimulationConfigurationDefaults.Create(), new DailyV1CalendarDefinition(1, 1, 1, null),
            new[] { cityValues }, new[] { new DailyV1ItemData("item/a", "Iron", 4f, null) },
            System.Array.Empty<DailyV1NpcData>(), System.Array.Empty<DailyV1NpcJobData>(),
            System.Array.Empty<DailyV1NpcActionData>(), System.Array.Empty<DailyV1NpcStatusData>(),
            System.Array.Empty<string>(), "test.daily-v1", "test.geography-v1",
            "test.geography-stage/v1", "hex-v1", "q-r-s");

        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial(
            "city-runtime", "location-runtime", definitions, definitions.Cities["city/a"], null,
            out DailyV1CityRuntimeOwners owners), Is.True);
        DailyV1FiniteProductionSourceOwner source = owners.FiniteSource;
        Assert.That(source, Is.Not.Null);
        Assert.That(source.Item, Is.SameAs(definitions.Items["item/a"]));
        Assert.That(source.ProductionSourceId, Is.EqualTo("source/finite"));
        Assert.That(source.InitialReserve, Is.EqualTo(7));
        Assert.That(source.RemainingReserve, Is.EqualTo(7));
        Assert.That(source.Revision, Is.Zero);
        Assert.That(source.LastProcessedDay, Is.EqualTo(long.MinValue));

        LocalDailyMaterialFlowResult result = owners.SimulateSelectedLocalDailyMaterialFlow(
            owners.LocationRuntimeId, 1, "calendar-v1", null);
        Assert.That(result.OpeningStock, Is.EqualTo(5));
        Assert.That(result.ConfiguredSourceQuantity, Is.EqualTo(5));
        Assert.That(result.AppliedSourceQuantity, Is.EqualTo(5));
        Assert.That(result.RequestedFreeConsumption, Is.EqualTo(4));
        Assert.That(result.ActualFreeConsumption, Is.EqualTo(4));
        Assert.That(result.ClosingStock, Is.EqualTo(6));
        Assert.That(result.SourceOutcomes.Select(outcome => outcome.ProductionSourceId),
            Is.EqualTo(new[] { "source/exogenous", "source/finite" }));
        Assert.That(owners.Market.GetAmount(source.Item), Is.EqualTo(6));
        Assert.That(source.RemainingReserve, Is.EqualTo(4));
        Assert.That(source.Revision, Is.EqualTo(1));
        Assert.That(source.LastProcessedDay, Is.EqualTo(1));
        Assert.Throws<LocalDailyMaterialFlowRejectedException>(() =>
            owners.SimulateSelectedLocalDailyMaterialFlow(
                owners.LocationRuntimeId, 1, "calendar-v1", null),
            "A finite source cannot install twice for the same boundary.");
        Assert.That(source.RemainingReserve, Is.EqualTo(4));
        Assert.That(source.Revision, Is.EqualTo(1));
        Assert.That(owners.Market.GetAmount(source.Item), Is.EqualTo(6));
    }

    [Test]
    public void SelectedFiniteReserveDailyFlowUsesCoreOwnersAndCanonicalItemIdentity()
    {
        DailyV1CityRuntimeOwners owners = CreateFiniteCityOwner(consumptionPer1000Population: 4f);
        DailyV1FiniteProductionSourceOwner source = owners.FiniteSource;
        DailyV1ItemDefinition item = source.Item;
        List<string> logs = new List<string>();

        LocalDailyMaterialFlowResult result = owners.SimulateSelectedLocalDailyMaterialFlow(
            owners.LocationRuntimeId, 1, "calendar-v1", (_, message) => logs.Add(message));

        Assert.That(result.ItemDefinitionId, Is.EqualTo(item.DefinitionId));
        Assert.That(result.OpeningStock, Is.EqualTo(5));
        Assert.That(result.ConfiguredSourceQuantity, Is.EqualTo(3));
        Assert.That(result.AppliedSourceQuantity, Is.EqualTo(3));
        Assert.That(result.SourceRejectionReason, Is.Empty);
        Assert.That(result.RequestedFreeConsumption, Is.EqualTo(4));
        Assert.That(result.ActualFreeConsumption, Is.EqualTo(4));
        Assert.That(result.ClosingStock, Is.EqualTo(4));
        Assert.That(owners.Market.GetAmount(item), Is.EqualTo(4));
        Assert.That(source.RemainingReserve, Is.EqualTo(4));
        Assert.That(source.Revision, Is.EqualTo(1));
        Assert.That(source.LastProcessedDay, Is.EqualTo(1));
        Assert.That(owners.Market.Revision, Is.EqualTo(2),
            "Production and the existing free-consumption write retain separate Market revisions.");
        Assert.That(logs, Is.EqualTo(new[] { "Finite City source source/finite: 3/3", "Finite City consumed 4 Iron" }));

        LocalDailyMaterialFlowResult second = owners.SimulateSelectedLocalDailyMaterialFlow(
            owners.LocationRuntimeId, 2, "calendar-v1", null);
        Assert.That(second.AppliedSourceQuantity, Is.EqualTo(3));
        Assert.That(second.ClosingStock, Is.EqualTo(3));
        Assert.That(source.RemainingReserve, Is.EqualTo(1));
        Assert.That(source.Revision, Is.EqualTo(2));
        Assert.That(source.LastProcessedDay, Is.EqualTo(2));
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
            runtimeId, "location-selected-city", definitions, city, population, foreignMarket, counterparty, economy,
            npcPresence,
            out DailyV1CityRuntimeOwners owners), Is.False);
        Assert.That(owners, Is.Null);
        Assert.That(DailyV1CityRuntimeOwners.TryCreateBound(
            runtimeId, "location-selected-city", definitions, city, population, foreignMarket,
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
            "city-runtime", "location-city-runtime", definitions, city, null,
            out DailyV1CityRuntimeOwners cityOwners), Is.True);
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
    public void SelectedNpcOwnerCensusProvidersReadCoreIdentityCardinalityAndRevision()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition definition = definitions.Npcs.Values.First();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-zulu", definitions, definition, 8f, out DailyV1NpcRuntimeOwners zulu), Is.True);
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-alpha", definitions, definition, 4f, out DailyV1NpcRuntimeOwners alpha), Is.True);

        IReadOnlyList<IOwnerSectionCensusProvider> inventoryProviders =
            DailyV1NpcOwnerCensusProviders.CreateInventoryProviders(new[] { zulu, alpha });
        Assert.That(inventoryProviders.Select(provider => provider.GetCurrentCensus().SectionId), Is.EqualTo(new[]
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "npc-alpha",
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "npc-zulu"
        }));
        OwnerSectionCensusWitness inventoryBefore = inventoryProviders[0].GetCurrentCensus();
        Assert.That(inventoryBefore.OwnerInstanceIdentity, Is.SameAs(alpha.Inventory));
        Assert.That(inventoryBefore.Cardinality, Is.Zero);
        Assert.That(inventoryBefore.Revision, Is.Zero);

        DailyV1ItemDefinition item = definitions.Items.Values.First();
        Assert.That(alpha.Inventory.TryAddItem(item, 2, 3f), Is.True);
        OwnerSectionCensusWitness inventoryAfter = inventoryProviders[0].GetCurrentCensus();
        Assert.That(inventoryAfter.OwnerInstanceIdentity, Is.SameAs(alpha.Inventory));
        Assert.That(inventoryAfter.Cardinality, Is.EqualTo(1));
        Assert.That(inventoryAfter.Revision, Is.EqualTo(inventoryBefore.Revision + 1));

        IReadOnlyList<IOwnerSectionCensusProvider> accountProviders =
            DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(new[] { zulu, alpha });
        OwnerSectionCensusWitness accountBefore = accountProviders[0].GetCurrentCensus();
        Assert.That(accountBefore.SectionId,
            Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + "npc-alpha"));
        Assert.That(accountBefore.OwnerInstanceIdentity, Is.SameAs(alpha.MoneyAccount));
        Assert.That(accountBefore.Cardinality, Is.EqualTo(1));
        Assert.That(accountBefore.Revision, Is.Zero);
        Assert.That(alpha.MoneyAccount.TryDebit(1f), Is.True);
        Assert.That(alpha.MoneyAccount.TryCredit(2f), Is.True);
        OwnerSectionCensusWitness accountAfter = accountProviders[0].GetCurrentCensus();
        Assert.That(accountAfter.OwnerInstanceIdentity, Is.SameAs(alpha.MoneyAccount));
        Assert.That(accountAfter.Cardinality, Is.EqualTo(1));
        Assert.That(accountAfter.Revision, Is.EqualTo(accountBefore.Revision + 2));
    }

    [Test]
    public void SelectedRuntimeCensusRebindsExactGenealogyOwnerIncludingRequiredEmptyWitness()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(out _);
        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(graph.TryBindGenealogyStore(genealogy), Is.True);
        OwnerSectionCensusSnapshot hostRow = CreateRequiredSnapshot(
            new GenealogyCensusProvider(genealogy).GetCurrentCensus());

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedGenealogySection(
            graph,
            new[] { hostRow },
            out IReadOnlyList<OwnerSectionCensusSnapshot> rebound,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(rebound, Has.Count.EqualTo(1));
        Assert.That(rebound[0], Is.Not.SameAs(hostRow));
        Assert.That(rebound[0].SectionId, Is.EqualTo(GenealogyCensusProvider.SectionId));
        Assert.That(rebound[0].SchemaVersion, Is.EqualTo(GenealogyCensusProvider.SchemaVersion));
        Assert.That(rebound[0].Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(rebound[0].OwnerInstanceIdentity, Is.SameAs(genealogy));
        Assert.That(rebound[0].Cardinality, Is.Zero,
            "The required empty Genealogy owner is represented by an exact Core witness.");
        Assert.That(rebound[0].Revision, Is.Zero);
    }

    [Test]
    public void SelectedRuntimeGenealogyCensusRejectsMissingDuplicateForeignAndStaleRows()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(out _);
        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(graph.TryBindGenealogyStore(genealogy), Is.True);
        OwnerSectionCensusSnapshot current = CreateRequiredSnapshot(
            new GenealogyCensusProvider(genealogy).GetCurrentCensus());

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedGenealogySection(
            graph, Array.Empty<OwnerSectionCensusSnapshot>(), out _, out _), Is.False,
            "The fixed required Genealogy row cannot be omitted.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedGenealogySection(
            graph, new[] { current, current }, out _, out _), Is.False,
            "The fixed Genealogy identity cannot occur more than once.");

        GenealogyStore foreign = new GenealogyStore();
        OwnerSectionCensusSnapshot foreignRow = CreateRequiredSnapshot(
            new GenealogyCensusProvider(foreign).GetCurrentCensus());
        Assert.That(foreignRow.Cardinality, Is.EqualTo(current.Cardinality));
        Assert.That(foreignRow.Revision, Is.EqualTo(current.Revision));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedGenealogySection(
            graph, new[] { foreignRow }, out _, out _), Is.False,
            "Matching empty values cannot substitute a parallel owner instance.");

        PersonId parent = new PersonId("person/genealogy-parent");
        PersonId child = new PersonId("person/genealogy-child");
        Assert.That(genealogy.TryAddParentage(parent, child, out GenealogyFailure addFailure),
            Is.True, addFailure.ToString());
        Assert.That(genealogy.TryRemoveParentage(parent, child, out GenealogyFailure removeFailure),
            Is.True, removeFailure.ToString());
        Assert.That(genealogy.Count, Is.EqualTo(current.Cardinality));
        Assert.That(genealogy.Revision, Is.EqualTo(current.Revision + 2));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedGenealogySection(
            graph, new[] { current }, out _, out _), Is.False,
            "A stale revision is rejected even when cardinality returned to its prior value.");
    }

    [Test]
    public void SelectedRuntimeMembershipGraphRetainsExactGenealogyOwner()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(out _);
        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(graph.TryBindGenealogyStore(genealogy), Is.True);

        DailyV1NpcDefinition definition = graph.Definitions.Npcs.Values.First();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime-late", graph.Definitions, definition, 0f,
            out DailyV1NpcRuntimeOwners addedNpc), Is.True);
        Assert.That(graph.TryCreateWithAddedNpc(
            addedNpc, out DailyV1SelectedRuntimeOwnerGraph addedGraph), Is.True);
        Assert.That(addedGraph.GenealogyStore, Is.SameAs(genealogy));
        Assert.That(new GenealogyCensusProvider(addedGraph.GenealogyStore)
            .GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(genealogy));

        Assert.That(addedGraph.TryCreateWithoutNpc(
            addedNpc, out DailyV1SelectedRuntimeOwnerGraph restoredGraph), Is.True);
        Assert.That(restoredGraph.GenealogyStore, Is.SameAs(genealogy),
            "Roster removal must retain the session's exact Genealogy authority too.");
    }

    [Test]
    public void SelectedRuntimeCensusRebindsInventoryAndMoneyRowsFromItsExactCoreGraph()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(
            out DailyV1NpcRuntimeOwners npc);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = CreateCoreInventoryAndAccountProviders(graph);
        OwnerSectionCensusSnapshot[] hostRows = CreateRequiredSnapshots(providers);

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcSections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> rebound,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(rebound, Has.Count.EqualTo(2));
        Assert.That(rebound[0], Is.Not.SameAs(hostRows[0]));
        Assert.That(rebound[0].OwnerInstanceIdentity, Is.SameAs(npc.Inventory));
        Assert.That(rebound[1], Is.Not.SameAs(hostRows[1]));
        Assert.That(rebound[1].OwnerInstanceIdentity, Is.SameAs(npc.MoneyAccount));
        Assert.That(rebound[0].Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(rebound[1].Role, Is.EqualTo(OwnerSectionRole.Required));
    }

    [Test]
    public void SelectedRuntimeCensusRebindsNpcRuntimeStateToExactCoreAliasOwner()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(
            out DailyV1NpcRuntimeOwners npc);
        PersonStore personStore = new PersonStore();
        Assert.That(graph.TryBindPersonStore(personStore), Is.True);

        Assert.That(npc.ActionSlot.TryInstallRevision(3L), Is.True);
        Assert.That(npc.Lifecycle.TryInstallState(
            NpcLifeState.Alive, NpcInjurySeverity.None, 4L), Is.True);
        Assert.That(npc.TryInstallCrimeJusticeRevision(5L), Is.True);
        Assert.That(npc.Travel.TryInstallState(
            null, null, null, null, 0, 0, null, false, null, null, 6L), Is.True);
        Assert.That(npc.Residence.TryInstallExact(null, 7L), Is.True);

        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            DailyV1NpcOwnerCensusProviders.CreateRuntimeAuthorityProviders(graph);
        Assert.That(providers, Has.Count.EqualTo(5));
        OwnerSectionCensusSnapshot[] hostRows = CreateCoreNpcCensusRows(graph)
            .Concat(CreateRequiredSnapshots(providers))
            .ToArray();

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcRuntimeStateSections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> rebound,
            out string diagnostic), Is.True, diagnostic);

        string[] aliasIds =
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix + npc.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcLifeStatePrefix + npc.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcResidencePrefix + npc.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimePrefix + npc.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix + npc.RuntimeId
        };
        OwnerSectionCensusSnapshot[] aliases = rebound
            .Where(row => aliasIds.Contains(row.SectionId, StringComparer.Ordinal))
            .ToArray();
        Assert.That(aliases, Has.Length.EqualTo(aliasIds.Length));
        Assert.That(aliases.All(row => ReferenceEquals(row.OwnerInstanceIdentity, npc)), Is.True);
        Assert.That(aliases.Single(row => row.SectionId == aliasIds[0]).Cardinality, Is.Zero);
        Assert.That(aliases.Single(row => row.SectionId == aliasIds[0]).Revision, Is.EqualTo(3L));
        Assert.That(aliases.Single(row => row.SectionId == aliasIds[1]).Revision, Is.EqualTo(4L));
        Assert.That(aliases.Single(row => row.SectionId == aliasIds[2]).Revision, Is.EqualTo(7L));
        Assert.That(aliases.Single(row => row.SectionId == aliasIds[3]).Revision, Is.EqualTo(5L));
        Assert.That(aliases.Single(row => row.SectionId == aliasIds[4]).Revision, Is.EqualTo(6L));

        OwnerSectionCensusSnapshot[] staleRows = hostRows.ToArray();
        int travelIndex = Array.FindIndex(staleRows, row => row.SectionId == aliasIds[4]);
        OwnerSectionCensusSnapshot travel = staleRows[travelIndex];
        staleRows[travelIndex] = new OwnerSectionCensusSnapshot(
            travel.SectionId, travel.SchemaVersion, travel.Role,
            travel.OwnerInstanceIdentity, travel.Cardinality, travel.Revision - 1L);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcRuntimeStateSections(
            graph, staleRows, out _, out _), Is.False,
            "A selected Core travel revision mismatch must reject the complete census rather than publish a partial owner vector.");
        OwnerSectionCensusSnapshot[] foreignIdentityRows = hostRows.ToArray();
        int actionIndex = Array.FindIndex(foreignIdentityRows, row => row.SectionId == aliasIds[0]);
        OwnerSectionCensusSnapshot action = foreignIdentityRows[actionIndex];
        foreignIdentityRows[actionIndex] = new OwnerSectionCensusSnapshot(
            action.SectionId, action.SchemaVersion, action.Role,
            new object(), action.Cardinality, action.Revision);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcRuntimeStateSections(
            graph, foreignIdentityRows, out _, out _), Is.False,
            "A selected Core alias row must identify the exact owner instance, even when its revision matches.");

        object legacyNpcOwner = new object();
        OwnerSectionCensusSnapshot[] mixedRows = hostRows.Concat(new[]
        {
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "npc-legacy",
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, legacyNpcOwner, 0, 2L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + "npc-legacy",
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, legacyNpcOwner, 1, 3L),
            new OwnerSectionCensusSnapshot(aliasIds[0].Replace(npc.RuntimeId, "npc-legacy"),
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, legacyNpcOwner, 0, 2L),
            new OwnerSectionCensusSnapshot(aliasIds[1].Replace(npc.RuntimeId, "npc-legacy"),
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, legacyNpcOwner, 1, 3L),
            new OwnerSectionCensusSnapshot(aliasIds[2].Replace(npc.RuntimeId, "npc-legacy"),
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, legacyNpcOwner, 1, 4L),
            new OwnerSectionCensusSnapshot(aliasIds[3].Replace(npc.RuntimeId, "npc-legacy"),
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, legacyNpcOwner, 1, 5L),
            new OwnerSectionCensusSnapshot(aliasIds[4].Replace(npc.RuntimeId, "npc-legacy"),
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, legacyNpcOwner, 1, 6L)
        }).ToArray();

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcRuntimeStateSections(
            graph, mixedRows, out IReadOnlyList<OwnerSectionCensusSnapshot> mixedRebound,
            out diagnostic), Is.True, diagnostic);
        Assert.That(mixedRebound, Has.Count.EqualTo(mixedRows.Length));
        for (int i = 0; i < mixedRows.Length; i++)
            Assert.That(mixedRebound[i], Is.SameAs(mixedRows[i]),
                "A mixed roster retains every host row as a whole vector without splicing selected Core rows.");
    }

    [Test]
    public void SelectedNpcResidenceCensusRemainsOwnedByMaterializedPerson()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(
            out DailyV1NpcRuntimeOwners npc);
        PersonStore personStore = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("person/materialized-npc"));
        Assert.That(personStore.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(personStore.TryBindMaterializedNpc(
            person.PersonId, npc.RuntimeId, out PersonStoreFailure bindFailure),
            Is.True, bindFailure.ToString());
        Assert.That(graph.TryBindPersonStore(personStore), Is.True);

        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            DailyV1NpcOwnerCensusProviders.CreateRuntimeAuthorityProviders(graph);
        Assert.That(providers, Has.Count.EqualTo(4));
        Assert.That(providers.Any(provider => string.Equals(
            provider.GetCurrentCensus().SectionId,
            DailyV1SelectedOwnerSectionCensusContract.NpcResidenceSectionIdFor(npc.RuntimeId),
            StringComparison.Ordinal)), Is.False);
        Assert.That(person.GetLifeResidenceCensusWitness().OwnerInstanceIdentity, Is.SameAs(person));
    }
    [Test]
    public void SelectedRuntimeCensusRebindsCommercialKnowledgeAliasTripleFromItsExactCoreGraph()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(
            out DailyV1NpcRuntimeOwners npc);
        DailyV1CommercialKnowledgeOwner owner = npc.CommercialKnowledge;
        DailyV1ItemDefinition item = npc.Definitions.Items.Values.First();
        Assert.That(owner.RecordObservation(new DailyV1CommercialMarketObservation(
            "market/location", item, 10f, 2, 1L, 1L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(owner.RecordLiquidityObservation(new DailyV1CommercialLiquidityObservation(
            "market/location", MarketLiquidityMode.AccountBacked, 25f, 1L, 1L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);

        OwnerSectionCensusSnapshot[] hostRows = CreateCoreNpcCensusRows(graph);
        string[] hostOrder = hostRows.Select(row => row.SectionId).ToArray();

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCommercialKnowledgeSections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> rebound,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(rebound, Has.Count.EqualTo(5));
        Assert.That(rebound.Select(row => row.SectionId), Is.EqualTo(hostOrder));
        string[] commercialIds =
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix + npc.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialLiquidityPrefix + npc.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix + npc.RuntimeId
        };
        OwnerSectionCensusSnapshot[] commercialRows = commercialIds
            .Select(id => rebound.Single(row => string.Equals(row.SectionId, id, StringComparison.Ordinal)))
            .ToArray();
        Assert.That(commercialRows.All(row => row.Role == OwnerSectionRole.Required), Is.True);
        Assert.That(commercialRows.All(row =>
            row.SchemaVersion == DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion), Is.True);
        Assert.That(commercialRows.All(row => ReferenceEquals(row.OwnerInstanceIdentity, owner)), Is.True);
        Assert.That(commercialRows.Select(row => row.Cardinality), Is.EqualTo(new[] { 1, 1, 0 }));
        Assert.That(commercialRows.Select(row => row.Revision), Is.EqualTo(new[] { 2L, 2L, 2L }));
        Assert.That(ReferenceEquals(
            commercialRows[0].OwnerInstanceIdentity,
            commercialRows[1].OwnerInstanceIdentity), Is.True);
        Assert.That(ReferenceEquals(
            commercialRows[1].OwnerInstanceIdentity,
            commercialRows[2].OwnerInstanceIdentity), Is.True);
        Assert.That(commercialRows[0].Revision, Is.EqualTo(commercialRows[1].Revision));
        Assert.That(commercialRows[1].Revision, Is.EqualTo(commercialRows[2].Revision));
    }

    [Test]
    public void SelectedRuntimeCensusPreservesMixedCommercialRosterAndStaleFacts()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(
            out DailyV1NpcRuntimeOwners npc);
        OwnerSectionCensusSnapshot[] selectedRows = CreateCoreNpcCensusRows(graph);
        object compatibilityCommercialOwner = new object();
        OwnerSectionCensusSnapshot[] compatibilityRows =
        {
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "compatibility-npc",
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, new object(), 0, 1L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + "compatibility-npc",
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, new object(), 1, 2L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix + "compatibility-npc",
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, compatibilityCommercialOwner, 2, 3L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialLiquidityPrefix + "compatibility-npc",
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, compatibilityCommercialOwner, 0, 3L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix + "compatibility-npc",
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, compatibilityCommercialOwner, 0, 3L)
        };
        OwnerSectionCensusSnapshot[] mixedHostRows =
            selectedRows.Concat(compatibilityRows).Reverse().ToArray();

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCommercialKnowledgeSections(
            graph, mixedHostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> mixedPreserved,
            out string mixedDiagnostic), Is.True, mixedDiagnostic);
        Assert.That(mixedPreserved, Has.Count.EqualTo(mixedHostRows.Length));
        for (int i = 0; i < mixedHostRows.Length; i++)
            Assert.That(mixedPreserved[i], Is.SameAs(mixedHostRows[i]),
                "Mixed host composition preserves the entire vector and original order.");

        string selectedMarketId =
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix + npc.RuntimeId;
        OwnerSectionCensusSnapshot[] missingSelectedRow = mixedHostRows
            .Where(row => !string.Equals(row.SectionId, selectedMarketId, StringComparison.Ordinal))
            .ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCommercialKnowledgeSections(
            graph, missingSelectedRow, out _, out _), Is.False,
            "A mixed host vector must contain every selected Core CommercialKnowledge section.");

        OwnerSectionCensusSnapshot[] selectedOnly = (OwnerSectionCensusSnapshot[])selectedRows.Clone();
        int selectedMarketIndex = Array.FindIndex(selectedOnly,
            row => string.Equals(row.SectionId, selectedMarketId, StringComparison.Ordinal));
        OwnerSectionCensusSnapshot market = selectedOnly[selectedMarketIndex];
        selectedOnly[selectedMarketIndex] = new OwnerSectionCensusSnapshot(
            market.SectionId, market.SchemaVersion, market.Role, new object(),
            market.Cardinality, market.Revision);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCommercialKnowledgeSections(
            graph, selectedOnly, out IReadOnlyList<OwnerSectionCensusSnapshot> stalePreserved,
            out string staleDiagnostic), Is.True, staleDiagnostic);
        for (int i = 0; i < selectedOnly.Length; i++)
            Assert.That(stalePreserved[i], Is.SameAs(selectedOnly[i]),
                "Stale captured owner identity is preserved for the existing P12 source classification.");

        OwnerSectionCensusSnapshot[] unevenFamily = mixedHostRows
            .Where(row => !string.Equals(
                row.SectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix
                    + "compatibility-npc",
                StringComparison.Ordinal))
            .ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCommercialKnowledgeSections(
            graph, unevenFamily, out _, out _), Is.False,
            "Mixed fallback cannot accept incomplete CommercialKnowledge alias-family keys.");
    }

    [Test]
    public void SelectedRuntimeCensusRebindsPersonStoreAliasRowsFromItsExactCoreGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition definition = definitions.Npcs.Values.First();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definition, 8f,
            out DailyV1NpcRuntimeOwners npc), Is.True);

        PersonStoreOwnerSnapshot personSnapshot = new PersonStoreOwnerSnapshot(
            PersonStoreOwnerSnapshot.CurrentSchemaId,
            PersonStoreOwnerSnapshot.CurrentSchemaVersion,
            2L,
            1,
            1,
            new[]
            {
                new PersonStoreOwnerSnapshotRow(
                    "person/census", 1L, null, null, "npc-runtime", 0L)
            });
        Assert.That(PersonStore.TryCreateFromOwnerSnapshot(
            personSnapshot,
            out PersonStore personStore,
            out PersonStoreOwnerSnapshotFailureCode personFailure),
            Is.True, personFailure.ToString());
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions,
            Array.Empty<DailyV1CityRuntimeOwners>(),
            new[] { npc },
            null, null, null, null, personStore,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        OwnerSectionCensusWitness membership = personStore.GetMembershipCensusWitness();
        OwnerSectionCensusWitness bindings = personStore.GetMaterializationBindingCensusWitness();
        object unrelatedOwner = new object();
        OwnerSectionCensusSnapshot unrelated = new OwnerSectionCensusSnapshot(
            "p12d.test.unrelated", 1, OwnerSectionRole.Required, unrelatedOwner, 4, 7L);
        OwnerSectionCensusSnapshot[] hostRows =
        {
            ToRequiredSnapshot(membership),
            ToRequiredSnapshot(bindings),
            unrelated
        };

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedPersonStoreSections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> rebound,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(rebound, Has.Count.EqualTo(3));
        Assert.That(rebound[0], Is.Not.SameAs(hostRows[0]));
        Assert.That(rebound[1], Is.Not.SameAs(hostRows[1]));
        Assert.That(rebound[2], Is.SameAs(unrelated));
        Assert.That(rebound[0].SectionId,
            Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId));
        Assert.That(rebound[1].SectionId,
            Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId));
        Assert.That(rebound[0].OwnerInstanceIdentity, Is.SameAs(personStore));
        Assert.That(rebound[1].OwnerInstanceIdentity, Is.SameAs(personStore));
        Assert.That(rebound[0].Cardinality, Is.EqualTo(1));
        Assert.That(rebound[1].Cardinality, Is.EqualTo(1));
        Assert.That(rebound[0].Revision, Is.EqualTo(2L));
        Assert.That(rebound[1].Revision, Is.EqualTo(2L));
        string membershipAlias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(
            DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId);
        string bindingAlias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(
            DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId);
        Assert.That(membershipAlias, Is.Not.Null.And.Not.Empty);
        Assert.That(bindingAlias, Is.EqualTo(membershipAlias),
            "The two PersonStore rows retain their canonical shared-owner alias.");

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedPersonStoreSections(
            graph, new[] { hostRows[0] }, out _, out _), Is.False,
            "The Core capture boundary rejects a missing member of the fixed alias pair.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedPersonStoreSections(
            graph, new[] { hostRows[0], hostRows[0], hostRows[1] }, out _, out _), Is.False,
            "The Core capture boundary rejects duplicate owner-section IDs.");

        OwnerSectionCensusSnapshot substituted = new OwnerSectionCensusSnapshot(
            hostRows[0].SectionId, hostRows[0].SchemaVersion, hostRows[0].Role,
            new object(), hostRows[0].Cardinality, hostRows[0].Revision);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedPersonStoreSections(
            graph, new[] { substituted, hostRows[1] },
            out IReadOnlyList<OwnerSectionCensusSnapshot> preservedSubstitution,
            out string substitutionDiagnostic), Is.True, substitutionDiagnostic);
        Assert.That(preservedSubstitution[0], Is.SameAs(substituted),
            "Changed owner identity remains available to the P12 stale-token check.");

        OwnerSectionCensusSnapshot staleCardinality = new OwnerSectionCensusSnapshot(
            hostRows[0].SectionId, hostRows[0].SchemaVersion, hostRows[0].Role,
            hostRows[0].OwnerInstanceIdentity, hostRows[0].Cardinality - 1, hostRows[0].Revision);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedPersonStoreSections(
            graph, new[] { staleCardinality, hostRows[1] },
            out IReadOnlyList<OwnerSectionCensusSnapshot> preservedCardinality,
            out string cardinalityDiagnostic), Is.True, cardinalityDiagnostic);
        Assert.That(preservedCardinality[0], Is.SameAs(staleCardinality),
            "Changed cardinality remains available to the P12 stale-token check.");

        OwnerSectionCensusSnapshot staleRevision = new OwnerSectionCensusSnapshot(
            hostRows[1].SectionId, hostRows[1].SchemaVersion, hostRows[1].Role,
            hostRows[1].OwnerInstanceIdentity, hostRows[1].Cardinality, hostRows[1].Revision - 1L);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedPersonStoreSections(
            graph, new[] { hostRows[0], staleRevision },
            out IReadOnlyList<OwnerSectionCensusSnapshot> preservedRevision,
            out string revisionDiagnostic), Is.True, revisionDiagnostic);
        Assert.That(preservedRevision[1], Is.SameAs(staleRevision),
            "Changed revision remains available to the P12 stale-token check.");
    }

    [Test]
    public void SelectedRuntimeCensusPreservesPersonStoreHostRowsForMixedRoster()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition definition = definitions.Npcs.Values.First();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definition, 8f,
            out DailyV1NpcRuntimeOwners npc), Is.True);
        PersonStore selectedPersonStore = new PersonStore();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions,
            Array.Empty<DailyV1CityRuntimeOwners>(),
            new[] { npc },
            null, null, null, null, selectedPersonStore,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        OwnerSectionCensusSnapshot selectedInventory = CreateRequiredSnapshots(
            DailyV1NpcOwnerCensusProviders.CreateInventoryProviders(graph.Npcs))[0];
        OwnerSectionCensusSnapshot selectedMoneyAccount = CreateRequiredSnapshots(
            DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs))[0];
        object compatibilityInventory = new object();
        object compatibilityMoneyAccount = new object();
        OwnerSectionCensusSnapshot compatibilityInventoryRow = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "compatibility-npc",
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, compatibilityInventory, 0, 4L);
        OwnerSectionCensusSnapshot compatibilityMoneyAccountRow = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + "compatibility-npc",
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, compatibilityMoneyAccount, 1, 5L);

        PersonStore hostPersonStore = new PersonStore();
        OwnerSectionCensusSnapshot hostMembership =
            ToRequiredSnapshot(hostPersonStore.GetMembershipCensusWitness());
        OwnerSectionCensusSnapshot hostBindings =
            ToRequiredSnapshot(hostPersonStore.GetMaterializationBindingCensusWitness());
        OwnerSectionCensusSnapshot[] hostRows =
        {
            selectedInventory,
            compatibilityInventoryRow,
            selectedMoneyAccount,
            compatibilityMoneyAccountRow,
            hostMembership,
            hostBindings
        };

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedPersonStoreSections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> preserved,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(preserved, Has.Count.EqualTo(hostRows.Length));
        for (int i = 0; i < hostRows.Length; i++)
            Assert.That(preserved[i], Is.SameAs(hostRows[i]),
                "A mixed roster keeps its complete host census, including the host-owned PersonStore pair.");

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedPersonStoreSections(
            graph, hostRows.Take(hostRows.Length - 1).ToArray(), out _, out _), Is.False,
            "A mixed roster with inconsistent Inventory and MoneyAccount cardinality fails closed.");
    }

    [Test]
    public void SelectedNpcKnowledgeAndPlanCensusUsesExactCoreOwnersAndOrdinalRuntimeIdentityOrder()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateNpcGraph("npc-zulu", "npc-alpha");
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            DailyV1NpcOwnerCensusProviders.CreateKnowledgeAndPlanProviders(graph);
        OwnerSectionCensusSnapshot[] rows = CreateRequiredSnapshots(providers);

        Assert.That(rows.Length, Is.EqualTo(18));
        string[] orderedRuntimeIds = { "npc-alpha", "npc-zulu" };
        for (int npcIndex = 0; npcIndex < orderedRuntimeIds.Length; npcIndex++)
        {
            string runtimeId = orderedRuntimeIds[npcIndex];
            DailyV1NpcRuntimeOwners npc = graph.Npcs.Single(value =>
                string.Equals(value.RuntimeId, runtimeId, StringComparison.Ordinal));
            int offset = npcIndex * 9;
            for (int familyIndex = 0; familyIndex <= 6; familyIndex++)
            {
                OwnerSectionCensusSnapshot row = rows[offset + familyIndex];
                Assert.That(row.SectionId,
                    Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
                        familyIndex, runtimeId)));
                Assert.That(row.SchemaVersion,
                    Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion));
                Assert.That(row.Role, Is.EqualTo(OwnerSectionRole.Required));
            }

            OwnerSectionCensusSnapshot explorable = rows[offset];
            OwnerSectionCensusSnapshot topologyPlaces = rows[offset + 1];
            OwnerSectionCensusSnapshot topologyConnections = rows[offset + 2];
            OwnerSectionCensusSnapshot adventureOpposition = rows[offset + 3];
            OwnerSectionCensusSnapshot adventureItems = rows[offset + 4];
            OwnerSectionCensusSnapshot adventureResources = rows[offset + 5];
            OwnerSectionCensusSnapshot adventureAccess = rows[offset + 6];
            OwnerSectionCensusSnapshot merchantPlan = rows[offset + 7];
            OwnerSectionCensusSnapshot travelPlan = rows[offset + 8];

            Assert.That(explorable.OwnerInstanceIdentity, Is.SameAs(npc.ExplorableSiteKnowledge));
            Assert.That(topologyPlaces.OwnerInstanceIdentity,
                Is.SameAs(npc.LocalTopologyKnowledgeEmptyOwner));
            Assert.That(topologyConnections.OwnerInstanceIdentity,
                Is.SameAs(npc.LocalTopologyKnowledgeEmptyOwner));
            Assert.That(topologyPlaces.Cardinality, Is.Zero);
            Assert.That(topologyConnections.Cardinality, Is.Zero);
            Assert.That(topologyPlaces.Revision, Is.Zero);
            Assert.That(topologyConnections.Revision, Is.Zero);
            Assert.That(adventureOpposition.OwnerInstanceIdentity,
                Is.SameAs(npc.AdventureSiteIntelKnowledge));
            Assert.That(adventureItems.OwnerInstanceIdentity,
                Is.SameAs(npc.AdventureSiteIntelKnowledge));
            Assert.That(adventureResources.OwnerInstanceIdentity,
                Is.SameAs(npc.AdventureSiteIntelKnowledge));
            Assert.That(adventureAccess.OwnerInstanceIdentity,
                Is.SameAs(npc.AdventureSiteIntelKnowledge));
            Assert.That(merchantPlan.SectionId,
                Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                    DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradePlanKind, runtimeId)));
            Assert.That(merchantPlan.OwnerInstanceIdentity, Is.SameAs(npc.MerchantTradePlan));
            Assert.That(merchantPlan.Cardinality, Is.EqualTo(1));
            Assert.That(travelPlan.SectionId,
                Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, runtimeId)));
            Assert.That(travelPlan.OwnerInstanceIdentity, Is.SameAs(npc.TravelPlan));
            Assert.That(travelPlan.Cardinality, Is.EqualTo(1));
        }

        DailyV1NpcRuntimeOwners selectedNpc = graph.Npcs.Single(value => value.RuntimeId == "npc-alpha");
        Assert.That(selectedNpc.ExplorableSiteKnowledge.RecordObservation(
            new ExplorableSiteKnowledgeObservation(
                "site/knowledge", "location/knowledge", 1L, 1L,
                ExplorableSiteKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(selectedNpc.AdventureSiteIntelKnowledge.RecordObservation(
            new AdventureAccessObservation(
                "site/adventure", null, PlaceAccessState.Accessible,
                1L, 1L, AdventureIntelSource.DirectObservation)), Is.True);
        DailyV1ItemDefinition item = graph.Definitions.Items.Values.First();
        Assert.That(selectedNpc.MerchantTradePlan.TryInstallState(
            item, "city/origin", "city/target", 4, 4, 2.5f, 0, 0, "decision/merchant"), Is.True);
        Assert.That(selectedNpc.TravelPlan.TryCommit(
            "location/target", "city/target", NpcTravelReason.Trade,
            0.75f, 12.5f, "decision/travel"), Is.True);

        OwnerSectionCensusSnapshot[] updated = CreateRequiredSnapshots(
            DailyV1NpcOwnerCensusProviders.CreateKnowledgeAndPlanProviders(graph));
        Dictionary<string, OwnerSectionCensusSnapshot> byId =
            updated.ToDictionary(value => value.SectionId, StringComparer.Ordinal);
        Assert.That(byId[DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
            0, selectedNpc.RuntimeId)].Cardinality, Is.EqualTo(1));
        Assert.That(byId[DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
            0, selectedNpc.RuntimeId)].Revision, Is.EqualTo(1L));
        for (int familyIndex = 3; familyIndex <= 6; familyIndex++)
        {
            OwnerSectionCensusSnapshot adventure = byId[
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
                    familyIndex, selectedNpc.RuntimeId)];
            Assert.That(adventure.OwnerInstanceIdentity,
                Is.SameAs(selectedNpc.AdventureSiteIntelKnowledge));
            Assert.That(adventure.Revision, Is.EqualTo(1L));
        }
        Assert.That(byId[DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
            DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradePlanKind,
            selectedNpc.RuntimeId)].Revision, Is.EqualTo(1L));
        Assert.That(byId[DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind,
            selectedNpc.RuntimeId)].Revision, Is.EqualTo(1L));
    }

    [Test]
    public void SelectedNpcKnowledgeAndPlanCensusRebindsExactRowsAndPreservesStaleAndMixedVectors()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(out DailyV1NpcRuntimeOwners npc);
        OwnerSectionCensusSnapshot[] knowledgeRows = CreateRequiredSnapshots(
            DailyV1NpcOwnerCensusProviders.CreateKnowledgeAndPlanProviders(graph));
        OwnerSectionCensusSnapshot[] hostRows =
            CreateCoreNpcCensusRows(graph).Concat(knowledgeRows).ToArray();

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> rebound,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(rebound, Has.Count.EqualTo(hostRows.Length));
        foreach (OwnerSectionCensusSnapshot knowledge in knowledgeRows)
        {
            OwnerSectionCensusSnapshot reboundRow = rebound.Single(value =>
                string.Equals(value.SectionId, knowledge.SectionId, StringComparison.Ordinal));
            Assert.That(reboundRow, Is.Not.SameAs(knowledge));
            Assert.That(reboundRow.OwnerInstanceIdentity, Is.SameAs(knowledge.OwnerInstanceIdentity));
        }

        int staleIndex = Array.FindIndex(hostRows, value =>
            value.SectionId == DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, npc.RuntimeId));
        OwnerSectionCensusSnapshot staleTravelPlan = hostRows[staleIndex];
        OwnerSectionCensusSnapshot[] staleRows = hostRows.ToArray();
        staleRows[staleIndex] = new OwnerSectionCensusSnapshot(
            staleTravelPlan.SectionId, staleTravelPlan.SchemaVersion, staleTravelPlan.Role,
            new object(), staleTravelPlan.Cardinality, staleTravelPlan.Revision);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
            graph, staleRows, out IReadOnlyList<OwnerSectionCensusSnapshot> stalePreserved,
            out string staleDiagnostic), Is.True, staleDiagnostic);
        for (int i = 0; i < staleRows.Length; i++)
            Assert.That(stalePreserved[i], Is.SameAs(staleRows[i]),
                "Stale source evidence remains intact for P12 token classification.");

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
            graph, hostRows.Where(value => value.SectionId != knowledgeRows[0].SectionId).ToArray(),
            out _, out _), Is.False, "Every required Knowledge and Plan family row must be present.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
            graph, hostRows.Concat(new[] { knowledgeRows[0] }).ToArray(),
            out _, out _), Is.False, "The host vector cannot duplicate a required family identity.");

        object legacyTopology = new object();
        object legacyAdventure = new object();
        OwnerSectionCensusSnapshot[] legacyRows = new OwnerSectionCensusSnapshot[11];
        legacyRows[0] = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "npc-legacy",
            1, OwnerSectionRole.Required, new object(), 0, 0L);
        legacyRows[1] = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + "npc-legacy",
            1, OwnerSectionRole.Required, new object(), 1, 0L);
        for (int familyIndex = 0; familyIndex <= 6; familyIndex++)
        {
            object identity = familyIndex == 1 || familyIndex == 2
                ? legacyTopology
                : familyIndex >= 3 && familyIndex <= 6
                    ? legacyAdventure
                    : new object();
            legacyRows[familyIndex + 2] = new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
                    familyIndex, "npc-legacy"),
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, identity, 0, 0L);
        }
        legacyRows[9] = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradePlanKind,
                "npc-legacy"),
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, new object(), 1, 0L);
        legacyRows[10] = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind,
                "npc-legacy"),
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, new object(), 1, 0L);
        OwnerSectionCensusSnapshot[] mixedRows = hostRows.Concat(legacyRows).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
            graph, mixedRows, out IReadOnlyList<OwnerSectionCensusSnapshot> mixedPreserved,
            out string mixedDiagnostic), Is.True, mixedDiagnostic);
        Assert.That(mixedPreserved, Has.Count.EqualTo(mixedRows.Length));
        for (int i = 0; i < mixedRows.Length; i++)
            Assert.That(mixedPreserved[i], Is.SameAs(mixedRows[i]),
                "Mixed rosters preserve the full host census, including compatibility NPC rows.");
    }
    [Test]
    public void SelectedRuntimeCensusRebindsSpatialKnowledgePairsFromItsExactCoreGraph()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(
            out DailyV1NpcRuntimeOwners npc);
        Assert.That(npc.SpatialKnowledge.DiscoverLocation("location-known"), Is.True);
        Assert.That(npc.SpatialKnowledge.DiscoverRoute("route-known"), Is.True);

        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            DailyV1NpcOwnerCensusProviders.CreateSpatialKnowledgeProviders(graph.Npcs);
        OwnerSectionCensusSnapshot[] spatialRows = CreateRequiredSnapshots(providers);
        object unrelatedOwner = new object();
        OwnerSectionCensusSnapshot unrelated = new OwnerSectionCensusSnapshot(
            "p12d.test.unrelated", 1, OwnerSectionRole.Required, unrelatedOwner, 4, 7L);
        OwnerSectionCensusSnapshot[] hostRows = spatialRows.Concat(new[] { unrelated }).ToArray();

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreSpatialKnowledgeSections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> rebound,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(rebound, Has.Count.EqualTo(3));
        Assert.That(rebound[0], Is.Not.SameAs(hostRows[0]));
        Assert.That(rebound[1], Is.Not.SameAs(hostRows[1]));
        Assert.That(rebound[2], Is.SameAs(unrelated));
        Assert.That(rebound[0].SectionId,
            Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix
                + npc.RuntimeId));
        Assert.That(rebound[1].SectionId,
            Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix
                + npc.RuntimeId));
        Assert.That(rebound[0].OwnerInstanceIdentity, Is.SameAs(npc.SpatialKnowledge));
        Assert.That(rebound[1].OwnerInstanceIdentity, Is.SameAs(npc.SpatialKnowledge));
        Assert.That(rebound[0].Cardinality, Is.EqualTo(1));
        Assert.That(rebound[1].Cardinality, Is.EqualTo(1));
        Assert.That(rebound[0].Revision, Is.EqualTo(2L));
        Assert.That(rebound[1].Revision, Is.EqualTo(2L));
        Assert.That(rebound[0].Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(rebound[1].Role, Is.EqualTo(OwnerSectionRole.Required));

        string locationsAlias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(
            rebound[0].SectionId);
        string routesAlias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(
            rebound[1].SectionId);
        Assert.That(locationsAlias, Is.Not.Null.And.Not.Empty);
        Assert.That(routesAlias, Is.EqualTo(locationsAlias),
            "SpatialKnowledge locations and routes retain their canonical shared-owner alias.");

        OwnerSectionCensusSnapshot substitutedOwner = new OwnerSectionCensusSnapshot(
            hostRows[0].SectionId, hostRows[0].SchemaVersion, hostRows[0].Role,
            new object(), hostRows[0].Cardinality, hostRows[0].Revision);
        OwnerSectionCensusSnapshot[] staleRows =
        {
            substitutedOwner, hostRows[1], unrelated
        };
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreSpatialKnowledgeSections(
            graph, staleRows, out IReadOnlyList<OwnerSectionCensusSnapshot> preserved,
            out string staleDiagnostic), Is.True, staleDiagnostic);
        Assert.That(preserved[0], Is.SameAs(substitutedOwner));
        Assert.That(preserved[1], Is.SameAs(hostRows[1]),
            "A stale pair remains intact for the existing P12 stale-token classification.");

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreSpatialKnowledgeSections(
            graph, new[] { hostRows[0] }, out _, out _), Is.False,
            "The selected graph requires both spatial census sections.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreSpatialKnowledgeSections(
            graph, new[] { hostRows[0], hostRows[0], hostRows[1] }, out _, out _), Is.False,
            "The host census cannot duplicate an owner-section identity.");
    }

    [Test]
    public void SelectedRuntimeCensusPreservesSpatialKnowledgeHostRowsForMixedRoster()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(out _);
        OwnerSectionCensusSnapshot selectedInventory = CreateRequiredSnapshots(
            DailyV1NpcOwnerCensusProviders.CreateInventoryProviders(graph.Npcs))[0];
        OwnerSectionCensusSnapshot selectedMoneyAccount = CreateRequiredSnapshots(
            DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs))[0];
        OwnerSectionCensusSnapshot[] selectedSpatial = CreateRequiredSnapshots(
            DailyV1NpcOwnerCensusProviders.CreateSpatialKnowledgeProviders(graph.Npcs));

        OwnerSectionCensusSnapshot compatibilityInventory = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "compatibility-npc",
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, new object(), 0, 4L);
        OwnerSectionCensusSnapshot compatibilityMoneyAccount = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + "compatibility-npc",
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, new object(), 1, 5L);
        SpatialKnowledgeRuntime compatibilitySpatialOwner =
            new SpatialKnowledgeRuntime("compatibility-npc");
        OwnerSectionCensusSnapshot compatibilityLocations = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix
                + "compatibility-npc",
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, compatibilitySpatialOwner, 0, 0L);
        OwnerSectionCensusSnapshot compatibilityRoutes = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix
                + "compatibility-npc",
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, compatibilitySpatialOwner, 0, 0L);
        OwnerSectionCensusSnapshot[] hostRows =
        {
            selectedInventory,
            compatibilityInventory,
            selectedMoneyAccount,
            compatibilityMoneyAccount,
            selectedSpatial[0],
            compatibilityLocations,
            selectedSpatial[1],
            compatibilityRoutes
        };

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreSpatialKnowledgeSections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> preserved,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(preserved, Has.Count.EqualTo(hostRows.Length));
        for (int i = 0; i < hostRows.Length; i++)
            Assert.That(preserved[i], Is.SameAs(hostRows[i]),
                "A mixed roster retains the whole host SpatialKnowledge vector.");

        string selectedRoutesId =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix
            + graph.Npcs[0].RuntimeId;
        OwnerSectionCensusSnapshot[] incompleteSelectedPair =
            hostRows.Where(row => !string.Equals(row.SectionId, selectedRoutesId,
                StringComparison.Ordinal)).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreSpatialKnowledgeSections(
            graph, incompleteSelectedPair, out _, out _), Is.False,
            "The mixed host census must still contain both selected Core sections.");
    }

    private static OwnerSectionCensusSnapshot ToRequiredSnapshot(OwnerSectionCensusWitness witness)
    {
        return new OwnerSectionCensusSnapshot(
            witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
            witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision);
    }
    [Test]
    public void SelectedRuntimeCensusKeepsWholeRosterRowsForMixedHostComposition()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(out _);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = CreateCoreInventoryAndAccountProviders(graph);
        OwnerSectionCensusSnapshot[] selectedRows = CreateRequiredSnapshots(providers);
        object compatibilityInventoryOwner = new object();
        object compatibilityMoneyAccountOwner = new object();
        OwnerSectionCensusSnapshot[] hostRows = selectedRows.Concat(new[]
        {
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "compatibility-npc",
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, compatibilityInventoryOwner, 0, 3L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + "compatibility-npc",
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, compatibilityMoneyAccountOwner, 1, 4L)
        }).ToArray();

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcSections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> preserved,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(preserved, Has.Count.EqualTo(hostRows.Length));
        for (int i = 0; i < hostRows.Length; i++)
            Assert.That(preserved[i], Is.SameAs(hostRows[i]),
                "A mixed roster must retain one complete host-owned census for each NPC family.");

        OwnerSectionCensusSnapshot[] incompleteHostRows = selectedRows.Concat(new[]
        {
            hostRows[2]
        }).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcSections(
            graph, incompleteHostRows, out _, out _), Is.False,
            "Inventory and MoneyAccount must cover the same mixed whole roster.");
    }

    [Test]
    public void SelectedRuntimeCensusRejectsMissingDuplicateOrSubstitutedCoreOwnerRows()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(out _);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = CreateCoreInventoryAndAccountProviders(graph);
        OwnerSectionCensusSnapshot[] hostRows = CreateRequiredSnapshots(providers);

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcSections(
            graph, new[] { hostRows[0] }, out _, out _), Is.False);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcSections(
            graph, new[] { hostRows[0], hostRows[0], hostRows[1] }, out _, out _), Is.False);

        OwnerSectionCensusSnapshot substituted = new OwnerSectionCensusSnapshot(
            hostRows[0].SectionId, hostRows[0].SchemaVersion, hostRows[0].Role,
            new object(), hostRows[0].Cardinality, hostRows[0].Revision);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcSections(
            graph, new[] { substituted, hostRows[1] }, out _, out _), Is.False);
    }

    [Test]
    public void SelectedRuntimeCensusRejectsStaleInventoryCardinalityAndRevision()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition definition = definitions.Npcs.Values.First();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definition, 8f, out DailyV1NpcRuntimeOwners npc), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, Array.Empty<DailyV1CityRuntimeOwners>(), new[] { npc },
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = CreateCoreInventoryAndAccountProviders(graph);
        OwnerSectionCensusSnapshot[] staleRows = CreateRequiredSnapshots(providers);

        Assert.That(npc.Inventory.TryAddItem(definitions.Items.Values.First(), 1, 1f), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcSections(
            graph, staleRows, out _, out _), Is.False,
            "The Core capture boundary must reject a host row captured before the selected owner revision changed.");

        OwnerSectionCensusSnapshot[] staleAccountRows = CreateRequiredSnapshots(providers);
        Assert.That(npc.MoneyAccount.TryCredit(1f), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcSections(
            graph, staleAccountRows, out _, out _), Is.False,
            "The Core capture boundary must reject a host row captured before the selected MoneyAccount revision changed.");
    }

    [Test]
    public void SelectedRuntimeCensusRebindsPersonLifeResidenceFromBoundCoreStore()
    {
        PersonStore personStore = new PersonStore();
        PersonRuntime laterPerson = new PersonRuntime(new PersonId("person/z"), 21L);
        PersonRuntime earlierPerson = new PersonRuntime(new PersonId("person/a"), 18L);
        Assert.That(personStore.TryRegister(laterPerson, out PersonStoreFailure laterFailure),
            Is.True, laterFailure.ToString());
        Assert.That(personStore.TryRegister(earlierPerson, out PersonStoreFailure earlierFailure),
            Is.True, earlierFailure.ToString());

        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(out _);
        Assert.That(graph.TryBindPersonStore(personStore), Is.True);

        OwnerSectionCensusSnapshot laterRow = CreateRequiredSnapshot(
            laterPerson.GetLifeResidenceCensusWitness());
        OwnerSectionCensusSnapshot membershipRow = CreateRequiredSnapshot(
            personStore.GetMembershipCensusWitness());
        OwnerSectionCensusSnapshot earlierRow = CreateRequiredSnapshot(
            earlierPerson.GetLifeResidenceCensusWitness());
        OwnerSectionCensusSnapshot bindingRow = CreateRequiredSnapshot(
            personStore.GetMaterializationBindingCensusWitness());
        OwnerSectionCensusSnapshot[] hostRows =
        {
            laterRow,
            membershipRow,
            earlierRow,
            bindingRow
        };

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCorePersonLifeResidenceSections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> rebound,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(rebound, Has.Count.EqualTo(hostRows.Length));
        Assert.That(rebound.Select(row => row.SectionId),
            Is.EqualTo(hostRows.Select(row => row.SectionId)),
            "Core owner rebinding preserves the order of every row supplied in the host vector.");
        Assert.That(rebound[0], Is.Not.SameAs(laterRow));
        Assert.That(rebound[0].SectionId,
            Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(
                laterPerson.PersonId)));
        Assert.That(rebound[0].OwnerInstanceIdentity, Is.SameAs(laterPerson));
        Assert.That(rebound[0].Cardinality, Is.EqualTo(1));
        Assert.That(rebound[0].Revision, Is.EqualTo(laterPerson.LifeResidenceRevision));
        Assert.That(rebound[2], Is.Not.SameAs(earlierRow));
        Assert.That(rebound[2].OwnerInstanceIdentity, Is.SameAs(earlierPerson));
        Assert.That(rebound[1], Is.SameAs(membershipRow));
        Assert.That(rebound[3], Is.SameAs(bindingRow));
    }

    [Test]
    public void SelectedRuntimeCensusPreservesStalePersonLifeResidenceForP12Classification()
    {
        PersonStore personStore = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("person.stale-residence"), 24L);
        Assert.That(personStore.TryRegister(person, out PersonStoreFailure failure),
            Is.True, failure.ToString());
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(out _);
        Assert.That(graph.TryBindPersonStore(personStore), Is.True);

        OwnerSectionCensusSnapshot captured = CreateRequiredSnapshot(
            person.GetLifeResidenceCensusWitness());
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            person, "city.after-capture"), Is.True);
        Assert.That(person.LifeResidenceRevision, Is.EqualTo(captured.Revision + 1L));

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCorePersonLifeResidenceSections(
            graph, new[] { captured }, out IReadOnlyList<OwnerSectionCensusSnapshot> preserved,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(preserved[0], Is.SameAs(captured),
            "The source revision mismatch remains visible to the established P12 stale-token check.");
    }

    [Test]
    public void SelectedRuntimeCensusPreservesCompletePersonLifeResidenceRowsForMixedRoster()
    {
        PersonStore personStore = new PersonStore();
        PersonRuntime selectedPerson = new PersonRuntime(new PersonId("person.selected"), 22L);
        Assert.That(personStore.TryRegister(selectedPerson, out PersonStoreFailure failure),
            Is.True, failure.ToString());
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(out _);
        Assert.That(graph.TryBindPersonStore(personStore), Is.True);

        PersonRuntime compatibilityPerson = new PersonRuntime(new PersonId("person.compatibility"), 19L);
        OwnerSectionCensusSnapshot selected = CreateRequiredSnapshot(
            selectedPerson.GetLifeResidenceCensusWitness());
        OwnerSectionCensusSnapshot compatibility = CreateRequiredSnapshot(
            compatibilityPerson.GetLifeResidenceCensusWitness());
        OwnerSectionCensusSnapshot unrelated = new OwnerSectionCensusSnapshot(
            "p12d.test.person-vector", 1, OwnerSectionRole.Required, new object(), 3, 9L);
        OwnerSectionCensusSnapshot[] hostRows = { compatibility, unrelated, selected };

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCorePersonLifeResidenceSections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> preserved,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(preserved, Has.Count.EqualTo(hostRows.Length));
        for (int i = 0; i < hostRows.Length; i++)
            Assert.That(preserved[i], Is.SameAs(hostRows[i]),
                "Mixed-roster capture preserves the entire host vector without stitching authorities.");
    }

    [Test]
    public void SelectedRuntimeCensusRejectsMissingOrDuplicatePersonLifeResidenceRows()
    {
        PersonStore personStore = new PersonStore();
        PersonRuntime first = new PersonRuntime(new PersonId("person.first"), 20L);
        PersonRuntime second = new PersonRuntime(new PersonId("person.second"), 21L);
        Assert.That(personStore.TryRegister(first, out PersonStoreFailure firstFailure),
            Is.True, firstFailure.ToString());
        Assert.That(personStore.TryRegister(second, out PersonStoreFailure secondFailure),
            Is.True, secondFailure.ToString());
        DailyV1SelectedRuntimeOwnerGraph graph = CreateSingleNpcGraph(out _);
        Assert.That(graph.TryBindPersonStore(personStore), Is.True);
        OwnerSectionCensusSnapshot firstRow = CreateRequiredSnapshot(first.GetLifeResidenceCensusWitness());
        OwnerSectionCensusSnapshot secondRow = CreateRequiredSnapshot(second.GetLifeResidenceCensusWitness());

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCorePersonLifeResidenceSections(
            graph, new[] { firstRow }, out _, out _), Is.False,
            "A selected Core Person cannot be absent from the host census.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCorePersonLifeResidenceSections(
            graph, new[] { firstRow, firstRow }, out _, out _), Is.False,
            "The owner-section identity cannot be duplicated in a census.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCorePersonLifeResidenceSections(
            graph, new[] { firstRow, secondRow }, out _, out _), Is.True,
            "The full selected Person life/residence family is accepted.");
    }
    [Test]
    public void SelectedRuntimeCensusRebindsAllFourCityFamiliesFromTheExactCoreOwners()
    {
        DailyV1CityRuntimeOwners city = CreateMixedCityOwner();
        city.Population.RestoreSnapshot(0, city.Population.Revision);
        Assert.That(city.Population.CurrentPopulation, Is.Zero);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            city.Definitions, new[] { city }, Array.Empty<DailyV1NpcRuntimeOwners>(),
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        OwnerSectionCensusSnapshot[] hostRows = CreateCoreCityOwnerRows(graph);
        System.Array.Reverse(hostRows);
        string[] hostOrder = hostRows.Select(row => row.SectionId).ToArray();

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCitySections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> rebound,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(rebound, Has.Count.EqualTo(DailyV1SelectedOwnerSectionCensusContract.CityPrefixes.Count));
        Assert.That(rebound.Select(row => row.SectionId), Is.EqualTo(hostOrder),
            "Core rebinding preserves the complete host vector order.");
        Assert.That(rebound[0], Is.Not.SameAs(hostRows[0]));

        string cityKey = P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(city.RuntimeId);
        OwnerSectionCensusSnapshot presence = rebound.Single(row => string.Equals(
            row.SectionId, DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId),
            StringComparison.Ordinal));
        OwnerSectionCensusSnapshot market = rebound.Single(row => string.Equals(
            row.SectionId, DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix + cityKey,
            StringComparison.Ordinal));
        OwnerSectionCensusSnapshot population = rebound.Single(row => string.Equals(
            row.SectionId, DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix + cityKey,
            StringComparison.Ordinal));
        OwnerSectionCensusSnapshot receipts = rebound.Single(row => string.Equals(
            row.SectionId, DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix + cityKey,
            StringComparison.Ordinal));

        Assert.That(presence.SchemaVersion, Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion));
        Assert.That(presence.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(presence.OwnerInstanceIdentity, Is.SameAs(city.NpcPresence));
        Assert.That(presence.Cardinality, Is.EqualTo(city.NpcPresence.OrderedNpcRuntimeIds.Count));
        Assert.That(presence.Revision, Is.EqualTo(city.NpcPresence.Revision));

        Assert.That(market.SchemaVersion, Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion));
        Assert.That(market.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(market.OwnerInstanceIdentity, Is.SameAs(city.Market));
        Assert.That(market.Cardinality, Is.EqualTo(city.Market.Items.Count));
        Assert.That(market.Revision, Is.EqualTo(city.Market.Revision));

        city.Population.GetOperationReceiptCensus(out int receiptCardinality, out long receiptRevision);
        Assert.That(population.SchemaVersion, Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion));
        Assert.That(population.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(population.OwnerInstanceIdentity, Is.SameAs(city.Population));
        Assert.That(population.Cardinality, Is.EqualTo(1),
            "A Required aggregate section has one owner row even when current population is zero.");
        Assert.That(population.Revision, Is.EqualTo(city.Population.Revision));

        Assert.That(receipts.SchemaVersion, Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion));
        Assert.That(receipts.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(receipts.OwnerInstanceIdentity, Is.SameAs(city.Population));
        Assert.That(receipts.Cardinality, Is.EqualTo(receiptCardinality));
        Assert.That(receipts.Revision, Is.EqualTo(receiptRevision));
        Assert.That(population.OwnerInstanceIdentity, Is.SameAs(receipts.OwnerInstanceIdentity),
            "The aggregate and operation-receipt rows preserve the required Population owner alias.");
    }

    [Test]
    public void SelectedRuntimeCensusPreservesTheWholeCityRosterForMixedHostComposition()
    {
        DailyV1CityRuntimeOwners city = CreateMixedCityOwner();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            city.Definitions, new[] { city }, Array.Empty<DailyV1NpcRuntimeOwners>(),
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        OwnerSectionCensusSnapshot[] selectedRows = CreateCoreCityOwnerRows(graph);
        OwnerSectionCensusSnapshot[] compatibilityRows = CreateCompatibilityCityRows("compatibility/city");
        OwnerSectionCensusSnapshot[] hostRows = selectedRows.Concat(compatibilityRows).Reverse().ToArray();

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCitySections(
            graph, hostRows, out IReadOnlyList<OwnerSectionCensusSnapshot> preserved,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(preserved, Has.Count.EqualTo(hostRows.Length));
        for (int i = 0; i < hostRows.Length; i++)
            Assert.That(preserved[i], Is.SameAs(hostRows[i]),
                "A mixed City roster retains every complete host row in its original order.");

        string selectedPresenceId =
            DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId);
        OwnerSectionCensusSnapshot[] missingSelectedRow = hostRows
            .Where(row => !string.Equals(row.SectionId, selectedPresenceId, StringComparison.Ordinal))
            .ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCitySections(
            graph, missingSelectedRow, out _, out _), Is.False,
            "A mixed host vector must still include every selected Core City family.");

        string compatibilityMarketId =
            DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix
            + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection("compatibility/city");
        OwnerSectionCensusSnapshot[] unevenFamily = hostRows
            .Where(row => !string.Equals(row.SectionId, compatibilityMarketId, StringComparison.Ordinal))
            .ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCitySections(
            graph, unevenFamily, out _, out _), Is.False,
            "A mixed City fallback cannot stitch together incomplete family key sets.");
    }

    [Test]
    public void SelectedRuntimeCensusRejectsMalformedCityShapeAndPreservesStaleRows()
    {
        DailyV1CityRuntimeOwners city = CreateMixedCityOwner();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            city.Definitions, new[] { city }, Array.Empty<DailyV1NpcRuntimeOwners>(),
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        OwnerSectionCensusSnapshot[] hostRows = CreateCoreCityOwnerRows(graph);

        for (int missingIndex = 0; missingIndex < hostRows.Length; missingIndex++)
        {
            OwnerSectionCensusSnapshot[] missing = hostRows
                .Where((_, index) => index != missingIndex).ToArray();
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCitySections(
                graph, missing, out _, out _), Is.False,
                "Every selected City must retain all four canonical owner families.");
        }

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCitySections(
            graph, hostRows.Concat(new[] { hostRows[0] }).ToArray(), out _, out _), Is.False,
            "A City owner-section identity cannot be duplicated.");

        for (int rowIndex = 0; rowIndex < hostRows.Length; rowIndex++)
        {
            for (int mismatch = 0; mismatch < 5; mismatch++)
            {
                OwnerSectionCensusSnapshot original = hostRows[rowIndex];
                OwnerSectionRole role = original.Role;
                int schema = original.SchemaVersion;
                object owner = original.OwnerInstanceIdentity;
                int cardinality = original.Cardinality;
                long revision = original.Revision;
                switch (mismatch)
                {
                    case 0: role = OwnerSectionRole.Excluded; break;
                    case 1: schema++; break;
                    case 2: owner = new object(); break;
                    case 3: cardinality++; break;
                    case 4: revision++; break;
                }

                OwnerSectionCensusSnapshot[] stale = (OwnerSectionCensusSnapshot[])hostRows.Clone();
                stale[rowIndex] = new OwnerSectionCensusSnapshot(
                    original.SectionId, schema, role, owner, cardinality, revision);
                Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedCitySections(
                    graph, stale, out IReadOnlyList<OwnerSectionCensusSnapshot> preserved,
                    out string diagnostic), Is.True, diagnostic);
                for (int i = 0; i < stale.Length; i++)
                    Assert.That(preserved[i], Is.SameAs(stale[i]),
                        "Stale role/schema/identity/cardinality/revision evidence remains unchanged for P12 classification.");
            }
        }
    }

    private static OwnerSectionCensusSnapshot[] CreateCoreCityOwnerRows(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        List<OwnerSectionCensusSnapshot> rows = new List<OwnerSectionCensusSnapshot>();
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            string cityKey = P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(city.RuntimeId);
            city.Population.GetOperationReceiptCensus(out int receiptCardinality, out long receiptRevision);
            rows.Add(CreateRequiredSnapshot(new OwnerSectionCensusWitness(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId),
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                city.NpcPresence, city.NpcPresence.OrderedNpcRuntimeIds.Count, city.NpcPresence.Revision)));
            rows.Add(CreateRequiredSnapshot(new OwnerSectionCensusWitness(
                DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix + cityKey,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                city.Market, city.Market.Items.Count, city.Market.Revision)));
            rows.Add(CreateRequiredSnapshot(new OwnerSectionCensusWitness(
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix + cityKey,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                city.Population, 1, city.Population.Revision)));
            rows.Add(CreateRequiredSnapshot(new OwnerSectionCensusWitness(
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix + cityKey,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                city.Population, receiptCardinality, receiptRevision)));
        }
        return rows.ToArray();
    }

    private static OwnerSectionCensusSnapshot[] CreateCompatibilityCityRows(string runtimeId)
    {
        object populationOwner = new object();
        return new[]
        {
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(runtimeId),
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, new object(), 0, 1L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix
                    + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(runtimeId),
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, new object(), 0, 2L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix
                    + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(runtimeId),
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, populationOwner, 1, 3L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix
                    + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(runtimeId),
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, populationOwner, 0, 4L)
        };
    }
    private static DailyV1SelectedRuntimeOwnerGraph CreateNpcGraph(params string[] runtimeIds)
    {
        if (runtimeIds == null) throw new ArgumentNullException(nameof(runtimeIds));
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition definition = definitions.Npcs.Values.First();
        List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>();
        foreach (string runtimeId in runtimeIds)
        {
            Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
                runtimeId, definitions, definition, 8f, out DailyV1NpcRuntimeOwners npc), Is.True);
            npcs.Add(npc);
        }
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, Array.Empty<DailyV1CityRuntimeOwners>(), npcs,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        return graph;
    }
    private static DailyV1SelectedRuntimeOwnerGraph CreateSingleNpcGraph(
        out DailyV1NpcRuntimeOwners npc)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition definition = definitions.Npcs.Values.First();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definition, 8f, out npc), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, Array.Empty<DailyV1CityRuntimeOwners>(), new[] { npc },
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        return graph;
    }

    private static IReadOnlyList<IOwnerSectionCensusProvider> CreateCoreInventoryAndAccountProviders(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        List<IOwnerSectionCensusProvider> providers = new List<IOwnerSectionCensusProvider>();
        providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateInventoryProviders(graph.Npcs));
        providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs));
        return providers;
    }

    private static OwnerSectionCensusSnapshot[] CreateCoreNpcCensusRows(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        List<IOwnerSectionCensusProvider> providers =
            new List<IOwnerSectionCensusProvider>();
        providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateInventoryProviders(graph.Npcs));
        providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs));
        providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateCommercialKnowledgeProviders(graph.Npcs));
        return CreateRequiredSnapshots(providers);
    }

    private static OwnerSectionCensusSnapshot CreateRequiredSnapshot(OwnerSectionCensusWitness witness)
    {
        return new OwnerSectionCensusSnapshot(
            witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
            witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision);
    }
    private static OwnerSectionCensusSnapshot[] CreateRequiredSnapshots(
        IReadOnlyList<IOwnerSectionCensusProvider> providers)
    {
        OwnerSectionCensusSnapshot[] rows = new OwnerSectionCensusSnapshot[providers.Count];
        for (int i = 0; i < providers.Count; i++)
        {
            OwnerSectionCensusWitness witness = providers[i].GetCurrentCensus();
            rows[i] = new OwnerSectionCensusSnapshot(
                witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision);
        }
        return rows;
    }

    [Test]
    public void SelectedCityP18StepsUseCoreOwnersAndRetainReplayableReceipts()
    {
        DailyV1CityRuntimeOwners owners = CreateMixedCityOwner();
        int marketCommitted = 0;
        Assert.That(owners.Market.TryBindMutationBoundary(() => true, () => marketCommitted++), Is.True);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 1);
        Assert.That(owners.TryCreateDailyEconomyStep(operation, 0,
            CityDailyEconomyStepKind.Production, out BoundaryContinuationStep production, out _), Is.True);
        Assert.That(owners.TryCreateDailyEconomyStep(operation, 1,
            CityDailyEconomyStepKind.Consumption, out BoundaryContinuationStep consumption, out _), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(operation,
            "city-economy", "1", "configuration", new[] { production, consumption }, "content");

        Assert.That(owners.TryPrepareDailyEconomyStep(manifest, production,
            out IBoundaryContinuationStepCommit productionCommit, out _), Is.True);
        Assert.That(owners.Market.GetAmount(owners.FiniteSource.Item), Is.EqualTo(5),
            "Preparing a selected City operation must not mutate its authoritative Market owner.");
        Assert.That(productionCommit.TryCommit(out _), Is.True);
        Assert.That(owners.Market.GetAmount(owners.FiniteSource.Item), Is.EqualTo(10));
        Assert.That(owners.Market.Revision, Is.EqualTo(2));
        Assert.That(owners.FiniteSource.RemainingReserve, Is.EqualTo(4));
        Assert.That(owners.FiniteSource.Revision, Is.EqualTo(1));
        Assert.That(marketCommitted, Is.EqualTo(1), "A multi-row production install invalidates the Market once.");
        Assert.That(owners.TryResolveDailyEconomyReceipt(manifest, production,
            out CityDailyEconomyReceipt productionReceipt, out _), Is.True);
        Assert.That(productionReceipt.SourceOutcomes.Select(row => row.ProductionSourceId),
            Is.EqualTo(new[] { "source/exogenous", "source/finite" }));
        Assert.That(productionReceipt.OpeningStock, Is.EqualTo(5));
        Assert.That(productionReceipt.PostProductionStock, Is.EqualTo(10));
        Assert.That(productionReceipt.PostProductionMarketRevision, Is.EqualTo(2));

        Assert.That(owners.TryPrepareDailyEconomyStep(manifest, consumption,
            out IBoundaryContinuationStepCommit consumptionCommit, out _), Is.True);
        Assert.That(consumptionCommit.TryCommit(out _), Is.True);
        Assert.That(owners.Market.GetAmount(owners.FiniteSource.Item), Is.EqualTo(6));
        Assert.That(owners.Market.Revision, Is.EqualTo(3));
        Assert.That(marketCommitted, Is.EqualTo(2));
        Assert.That(owners.TryResolveDailyEconomyReceipt(manifest, consumption,
            out CityDailyEconomyReceipt consumptionReceipt, out _), Is.True);
        Assert.That(consumptionReceipt.ActualFreeConsumption, Is.EqualTo(4));
        Assert.That(consumptionReceipt.ClosingStock, Is.EqualTo(6));

        Assert.That(owners.TryPrepareDailyEconomyStep(manifest, production,
            out IBoundaryContinuationStepCommit replay, out _), Is.True);
        Assert.That(replay.TryCommit(out _), Is.True);
        Assert.That(owners.Market.GetAmount(owners.FiniteSource.Item), Is.EqualTo(6));
        owners.GetDailyEconomyReceiptExclusionProof(out int receiptCount, out long receiptRevision);
        Assert.That(receiptCount, Is.EqualTo(2));
        Assert.That(receiptRevision, Is.EqualTo(2));
    }

    [Test]
    public void SelectedCityNoOpP18StepRecordsReceiptWithoutMarketInvalidation()
    {
        DailyV1CityRuntimeOwners owners = CreateMixedCityOwner();
        int marketCommitted = 0;
        Assert.That(owners.Market.TryBindMutationBoundary(() => true, () => marketCommitted++), Is.True);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 1);
        Assert.That(owners.TryCreateDailyEconomyStep(operation, 0,
            CityDailyEconomyStepKind.PriceRefresh, out BoundaryContinuationStep priceRefresh, out _), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(operation,
            "city-economy", "1", "configuration", new[] { priceRefresh }, "content");

        Assert.That(owners.TryPrepareDailyEconomyStep(manifest, priceRefresh,
            out IBoundaryContinuationStepCommit prepared, out _), Is.True);
        Assert.That(prepared.TryCommit(out _), Is.True);
        Assert.That(owners.Market.Revision, Is.Zero);
        Assert.That(marketCommitted, Is.Zero);
        owners.GetDailyEconomyReceiptExclusionProof(out int receiptCount, out long receiptRevision);
        Assert.That(receiptCount, Is.EqualTo(1));
        Assert.That(receiptRevision, Is.EqualTo(1));
    }

    [Test]
    public void SelectedFiniteSourceP18CommitUsesTheValidatedScenarioAndRejectsStaleMarket()
    {
        DailyV1CityRuntimeOwners owners = CreateFiniteCityOwner();
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 1);
        Assert.That(owners.TryCreateDailyEconomyStep(operation, 0,
            CityDailyEconomyStepKind.Production, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(operation,
            "city-economy", "1", "configuration", new[] { step }, "content");

        Assert.That(owners.TryPrepareDailyEconomyStep(manifest, step,
            out IBoundaryContinuationStepCommit prepared, out _), Is.True);
        Assert.That(owners.Market.GetAmount(owners.FiniteSource.Item), Is.EqualTo(5));
        Assert.That(owners.Market.AddStock(owners.FiniteSource.Item, 1), Is.EqualTo(1));
        Assert.That(prepared.TryCommit(out _), Is.False);
        Assert.That(owners.FiniteSource.RemainingReserve, Is.EqualTo(7));
        Assert.That(owners.FiniteSource.Revision, Is.Zero);
        Assert.That(owners.Market.GetAmount(owners.FiniteSource.Item), Is.EqualTo(6));

        Assert.That(owners.TryCreateDailyEconomyStep(new DailyBoundaryOperation("world", "profile", 2), 0,
            CityDailyEconomyStepKind.Production, out BoundaryContinuationStep nextDayStep, out _), Is.True);
        BoundaryContinuationManifest nextDay = new BoundaryContinuationManifest(
            new DailyBoundaryOperation("world", "profile", 2), "city-economy", "1",
            "configuration", new[] { nextDayStep }, "content");
        Assert.That(owners.TryPrepareDailyEconomyStep(nextDay, nextDayStep,
            out IBoundaryContinuationStepCommit nextDayCommit, out _), Is.True);
        Assert.That(nextDayCommit.TryCommit(out _), Is.True);
        Assert.That(owners.FiniteSource.RemainingReserve, Is.EqualTo(4));
        Assert.That(owners.FiniteSource.Revision, Is.EqualTo(1));
        Assert.That(owners.Market.GetAmount(owners.FiniteSource.Item), Is.EqualTo(9));
    }

    [Test]
    public void SelectedCityP18AccountBackedConsumptionTransfersBothBalancesAtCommit()
    {
        DailyV1CityData cityValues = new DailyV1CityData(
            "city/paid", "Paid City", "settlement/paid", "location/paid", "market/paid",
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.ExogenousDaily, "ExogenousDaily"),
            1000, new DailyV1MarketLiquidityConfig(
                new DailyV1EnumValue((int)MarketLiquidityMode.AccountBacked, "AccountBacked"), 0f),
            new DailyV1PopulationConsumptionConfig(
                new DailyV1EnumValue((int)ConsumptionPaymentMode.AccountBacked, "AccountBacked"), 50f),
            new[] { new DailyV1MarketItemConfig("item/paid", 5, 5, 1f) },
            System.Array.Empty<DailyV1CityProductionConfig>(), new List<DailyV1CityConnection>());
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            DailyV1TypedDefinitionGraphTests.CreateConfiguration(1f, new[] { "city/paid" }),
            SimulationConfigurationDefaults.Create(), new DailyV1CalendarDefinition(1, 1, 1, null),
            new[] { cityValues }, new[] { new DailyV1ItemData("item/paid", "Iron", 4f, null) },
            System.Array.Empty<DailyV1NpcData>(), System.Array.Empty<DailyV1NpcJobData>(),
            System.Array.Empty<DailyV1NpcActionData>(), System.Array.Empty<DailyV1NpcStatusData>(),
            System.Array.Empty<string>(), "test.daily-v1", "test.geography-v1",
            "test.geography-stage/v1", "hex-v1", "q-r-s");
        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial("city-runtime-paid", "location-runtime-paid",
            definitions, definitions.Cities["city/paid"], null, out DailyV1CityRuntimeOwners owners), Is.True);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 1);
        Assert.That(owners.TryCreateDailyEconomyStep(operation, 0, CityDailyEconomyStepKind.Consumption,
            out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(operation,
            "city-economy", "1", "configuration", new[] { step }, "content");

        Assert.That(owners.TryPrepareDailyEconomyStep(manifest, step,
            out IBoundaryContinuationStepCommit prepared, out _), Is.True);
        Assert.That(owners.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(50f));
        Assert.That(owners.Counterparty.MoneyAccount.Balance, Is.EqualTo(0f));
        Assert.That(prepared.TryCommit(out _), Is.True);

        Assert.That(owners.Market.GetAmount(definitions.Items["item/paid"]), Is.EqualTo(4));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(46f));
        Assert.That(owners.Counterparty.MoneyAccount.Balance, Is.EqualTo(4f));
        Assert.That(owners.PopulationEconomy.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(owners.Counterparty.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(owners.TryResolveDailyEconomyReceipt(manifest, step,
            out CityDailyEconomyReceipt receipt, out _), Is.True);
        Assert.That(receipt.ConsumptionResults.Single().TotalPaid, Is.EqualTo(4f));
    }

    private static DailyV1CityRuntimeOwners CreateMixedCityOwner()
    {
        DailyV1CityData cityValues = new DailyV1CityData(
            "city/p18", "P18 City", "settlement/p18", "location/p18", "market/p18",
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.MixedSourcesDaily, "MixedSourcesDaily"),
            1000, null,
            new DailyV1PopulationConsumptionConfig(
                new DailyV1EnumValue((int)ConsumptionPaymentMode.Free, "Free"), 0f),
            new[] { new DailyV1MarketItemConfig("item/p18", 5, 100, 4f) },
            new[]
            {
                new DailyV1CityProductionConfig("item/p18", 2, 0, "source/exogenous", "content-v1",
                    new DailyV1EnumValue((int)CityProductionSourceKind.ExogenousDaily, "ExogenousDaily")),
                new DailyV1CityProductionConfig("item/p18", 3, 7, "source/finite", "content-v1",
                    new DailyV1EnumValue((int)CityProductionSourceKind.FiniteReserveDaily, "FiniteReserveDaily"))
            },
            new List<DailyV1CityConnection>());
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            DailyV1TypedDefinitionGraphTests.CreateConfiguration(1f, new[] { "city/p18" }),
            SimulationConfigurationDefaults.Create(), new DailyV1CalendarDefinition(1, 1, 1, null),
            new[] { cityValues }, new[] { new DailyV1ItemData("item/p18", "Iron", 4f, null) },
            System.Array.Empty<DailyV1NpcData>(), System.Array.Empty<DailyV1NpcJobData>(),
            System.Array.Empty<DailyV1NpcActionData>(), System.Array.Empty<DailyV1NpcStatusData>(),
            System.Array.Empty<string>(), "test.daily-v1", "test.geography-v1",
            "test.geography-stage/v1", "hex-v1", "q-r-s");
        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial("city-runtime-p18", "location-runtime-p18",
            definitions, definitions.Cities["city/p18"], null, out DailyV1CityRuntimeOwners owners), Is.True);
        return owners;
    }

    private static DailyV1CityRuntimeOwners CreateFiniteCityOwner(float consumptionPer1000Population = 0f)
    {
        DailyV1CityData cityValues = new DailyV1CityData(
            "city/finite", "Finite City", "settlement/finite", "location/finite", "market/finite",
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.FiniteReserveDaily, "FiniteReserveDaily"),
            1000, null,
            new DailyV1PopulationConsumptionConfig(
                new DailyV1EnumValue((int)ConsumptionPaymentMode.Free, "Free"), 0f),
            new[] { new DailyV1MarketItemConfig("item/finite", 5, 100, consumptionPer1000Population) },
            new[] { new DailyV1CityProductionConfig("item/finite", 3, 7,
                "source/finite", "content-v1",
                new DailyV1EnumValue((int)CityProductionSourceKind.FiniteReserveDaily, "FiniteReserveDaily")) },
            new List<DailyV1CityConnection>());
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            DailyV1TypedDefinitionGraphTests.CreateConfiguration(1f, new[] { "city/finite" }),
            SimulationConfigurationDefaults.Create(), new DailyV1CalendarDefinition(1, 1, 1, null),
            new[] { cityValues }, new[] { new DailyV1ItemData("item/finite", "Iron", 4f, null) },
            System.Array.Empty<DailyV1NpcData>(), System.Array.Empty<DailyV1NpcJobData>(),
            System.Array.Empty<DailyV1NpcActionData>(), System.Array.Empty<DailyV1NpcStatusData>(),
            System.Array.Empty<string>(), "test.daily-v1", "test.geography-v1",
            "test.geography-stage/v1", "hex-v1", "q-r-s");
        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial("city-runtime-finite", "location-runtime-finite",
            definitions, definitions.Cities["city/finite"], null, out DailyV1CityRuntimeOwners owners), Is.True);
        return owners;
    }

    [Test]
    public void SelectedRuntimeOwnerGraphOwnsUniqueP8LocationBindingsAndChecksNpcCityJoins()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition[] cityDefinitions = definitions.Cities.Values.Take(2).ToArray();
        Assert.That(cityDefinitions, Has.Length.EqualTo(2));

        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial(
            "city-runtime-a", "p8-location-shared", definitions, cityDefinitions[0], null,
            out DailyV1CityRuntimeOwners firstCity), Is.True);
        Assert.That(DailyV1CityRuntimeOwners.TryCreateInitial(
            "city-runtime-b", "p8-location-shared", definitions, cityDefinitions[1], null,
            out DailyV1CityRuntimeOwners secondCity), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, new[] { firstCity, secondCity }, System.Array.Empty<DailyV1NpcRuntimeOwners>(),
            out DailyV1SelectedRuntimeOwnerGraph duplicateLocationGraph), Is.False,
            "A selected runtime cannot assign two top-level City owners to one P8 Location.");
        Assert.That(duplicateLocationGraph, Is.Null);

        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners npc), Is.True);
        Assert.That(npc.Travel.TryInstallState(
            "p8-location-other", firstCity.RuntimeId, null, null,
            0, 0, null, false, null, null, 0L), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, new[] { firstCity }, new[] { npc },
            out DailyV1SelectedRuntimeOwnerGraph mismatchedCurrentLocation), Is.False,
            "A current City reference must use that City owner's exact P8 LocationId.");
        Assert.That(mismatchedCurrentLocation, Is.Null);

        Assert.That(npc.Travel.TryInstallState(
            null, null, "p8-location-other", firstCity.RuntimeId,
            1, 1, null, false, null, null, 0L), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, new[] { firstCity }, new[] { npc },
            out DailyV1SelectedRuntimeOwnerGraph mismatchedDestinationLocation), Is.False,
            "A destination City reference must use that City owner's exact P8 LocationId.");
        Assert.That(mismatchedDestinationLocation, Is.Null);
    }

    [Test]
    public void CoreGenesisBuilderConstructsTheCompleteSelectedCityNpcOwnerGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);

        List<string> cityIds = new List<string>(definitions.Configuration.cities);
        cityIds.RemoveAll(string.IsNullOrWhiteSpace);
        cityIds.Sort(System.StringComparer.Ordinal);
        Dictionary<string, DailyV1CityRuntimeOwners> cityOwnersByDefinition =
            new Dictionary<string, DailyV1CityRuntimeOwners>(System.StringComparer.Ordinal);
        for (int index = 0; index < cityIds.Count; index++)
        {
            string definitionId = cityIds[index];
            Assert.That(definitions.Cities.TryGetValue(definitionId, out DailyV1CityDefinition definition), Is.True);
            Assert.That(builder.TryAddInitialCity(
                "city-runtime-" + index, "location-runtime-" + index, definition,
                out DailyV1CityRuntimeOwners cityOwners), Is.True);
            if (!cityOwnersByDefinition.ContainsKey(definitionId))
                cityOwnersByDefinition.Add(definitionId, cityOwners);
            else
                cityOwnersByDefinition[definitionId] = null;
        }

        Dictionary<string, DailyV1NpcSimulationConfig> npcRows =
            new Dictionary<string, DailyV1NpcSimulationConfig>(System.StringComparer.Ordinal);
        foreach (DailyV1NpcSimulationConfig row in definitions.Configuration.npcs)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.npc)) continue;
            Assert.That(npcRows.TryAdd(row.npc, row), Is.True, "The captured fixture must have unique selected NPC rows.");
        }
        List<string> npcIds = new List<string>(npcRows.Keys);
        npcIds.Sort(System.StringComparer.Ordinal);
        Dictionary<string, DailyV1NpcRuntimeOwners> npcOwnersByDefinition =
            new Dictionary<string, DailyV1NpcRuntimeOwners>(System.StringComparer.Ordinal);
        int npcOrdinal = 0;
        foreach (string definitionId in npcIds)
        {
            Assert.That(definitions.Npcs.TryGetValue(definitionId, out DailyV1NpcDefinition definition), Is.True);
            Assert.That(builder.TryAddInitialNpc(
                "npc-runtime-" + npcOrdinal++, definition, npcRows[definitionId],
                out DailyV1NpcRuntimeOwners npcOwners), Is.True);
            npcOwnersByDefinition.Add(definitionId, npcOwners);
        }

        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.Definitions, Is.SameAs(definitions));
        Assert.That(graph.Cities, Has.Count.EqualTo(cityIds.Count));
        Assert.That(graph.Npcs, Has.Count.EqualTo(npcIds.Count));
        Assert.That(graph.MatchesExactOwnerInstances(builder.Cities, builder.Npcs), Is.True);
        Assert.That(graph.RuntimeIdentityIndex, Is.SameAs(builder.RuntimeIdentityIndex));
        Assert.That(graph.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.City),
            Is.EqualTo(graph.Cities.Count));
        Assert.That(graph.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Npc),
            Is.EqualTo(graph.Npcs.Count));
        Assert.That(graph.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Location),
            Is.EqualTo(graph.SpatialNetwork.LocationCount));
        Assert.That(graph.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Route),
            Is.EqualTo(graph.SpatialNetwork.RouteCount));
        Assert.That(graph.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.ExplorableSite), Is.Zero);
        Assert.That(graph.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.LocalPlace), Is.Zero);
        Assert.That(graph.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.LocalConnection), Is.Zero);
        Assert.That(graph.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.NotableItem), Is.Zero);

        foreach (string definitionId in npcIds)
        {
            DailyV1NpcSimulationConfig row = npcRows[definitionId];
            DailyV1NpcRuntimeOwners owners = npcOwnersByDefinition[definitionId];
            Assert.That(owners.Definitions, Is.SameAs(definitions));
            Assert.That(owners.Definition, Is.SameAs(definitions.Npcs[definitionId]));

            int expectedInventoryWrites = 0;
            foreach (DailyV1NpcInitialInventoryItemConfig itemRow in row.initialInventory
                ?? System.Array.Empty<DailyV1NpcInitialInventoryItemConfig>())
            {
                if (itemRow == null || string.IsNullOrWhiteSpace(itemRow.item)) continue;
                expectedInventoryWrites++;
                Assert.That(definitions.Items.ContainsKey(itemRow.item), Is.True);
            }
            Assert.That(owners.Inventory.Revision, Is.EqualTo(expectedInventoryWrites));
            foreach (DailyV1InventoryItemState item in owners.Inventory.Items)
                Assert.That(definitions.Items[item.Item.DefinitionId], Is.SameAs(item.Item));

            DailyV1CityRuntimeOwners expectedCity = null;
            if (!string.IsNullOrWhiteSpace(row.startingCity))
                cityOwnersByDefinition.TryGetValue(row.startingCity, out expectedCity);
            if (expectedCity == null)
            {
                Assert.That(owners.Travel.CurrentCityRuntimeId, Is.Null);
                Assert.That(owners.Travel.CurrentLocationRuntimeId, Is.Null);
            }
            else
            {
                Assert.That(owners.Travel.CurrentCityRuntimeId, Is.EqualTo(expectedCity.RuntimeId));
                Assert.That(builder.TryGetLocationRuntimeId(expectedCity, out string locationId), Is.True);
                Assert.That(owners.Travel.CurrentLocationRuntimeId, Is.EqualTo(locationId));
                Assert.That(owners.Travel.Revision, Is.Zero);
                Assert.That(expectedCity.NpcPresence.Contains(owners.RuntimeId), Is.True);
            }
        }

        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            int expectedMembershipCount = graph.Npcs.Count(owner =>
                string.Equals(owner.Travel.CurrentCityRuntimeId, city.RuntimeId, System.StringComparison.Ordinal));
            Assert.That(city.NpcPresence.OrderedNpcRuntimeIds, Has.Count.EqualTo(expectedMembershipCount));
            Assert.That(city.NpcPresence.Revision, Is.EqualTo(expectedMembershipCount));
        }
    }

    [Test]
    public void CoreGenesisBuilderOwnsSelectedCityAndNpcIdentityAllocationOrder()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        RuntimeIdAllocator runtimeIds = new RuntimeIdAllocator();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        List<SpatialLocationRuntime> registeredLocations = new List<SpatialLocationRuntime>();

        Assert.That(builder.TryCreateConfiguredCities(runtimeIds, location =>
        {
            registeredLocations.Add(location);
            return true;
        }), Is.True);

        List<string> resolvedNpcCityDefinitions = new List<string>();
        Assert.That(builder.TryCreateConfiguredNpcs(runtimeIds, cityDefinitionId =>
        {
            resolvedNpcCityDefinitions.Add(cityDefinitionId);
            return builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId;
        }), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        List<string> expectedCityDefinitions = new List<string>(definitions.Configuration.cities);
        expectedCityDefinitions.RemoveAll(string.IsNullOrWhiteSpace);
        expectedCityDefinitions.Sort(System.StringComparer.Ordinal);
        Assert.That(graph.Cities, Has.Count.EqualTo(expectedCityDefinitions.Count));
        Assert.That(registeredLocations, Has.Count.EqualTo(expectedCityDefinitions.Count));
        for (int index = 0; index < graph.Cities.Count; index++)
        {
            Assert.That(graph.Cities[index].Definition.DefinitionId,
                Is.EqualTo(expectedCityDefinitions[index]));
            Assert.That(graph.Cities[index].RuntimeId,
                Is.EqualTo("city-" + (index + 1).ToString("D6")));
            Assert.That(builder.TryGetLocationRuntimeId(graph.Cities[index], out string locationRuntimeId), Is.True);
            Assert.That(locationRuntimeId, Is.EqualTo("location-" + (index + 1).ToString("D6")));
            Assert.That(registeredLocations[index].RuntimeId, Is.EqualTo(locationRuntimeId));
            Assert.That(graph.SpatialNetwork.TryGetLocation(locationRuntimeId,
                out SpatialLocationRuntime graphLocation), Is.True);
            Assert.That(graphLocation, Is.SameAs(registeredLocations[index]));
        }

        List<DailyV1NpcSimulationConfig> expectedNpcRows = definitions.Configuration.npcs
            .Where(row => row != null && !string.IsNullOrWhiteSpace(row.npc))
            .OrderBy(row => row.npc, System.StringComparer.Ordinal)
            .ToList();
        Assert.That(graph.Npcs, Has.Count.EqualTo(expectedNpcRows.Count));
        Assert.That(resolvedNpcCityDefinitions, Is.EqualTo(expectedNpcRows
            .Where(row => !string.IsNullOrWhiteSpace(row.startingCity))
            .Select(row => row.startingCity)));
        for (int index = 0; index < graph.Npcs.Count; index++)
        {
            Assert.That(graph.Npcs[index].Definition.DefinitionId,
                Is.EqualTo(expectedNpcRows[index].npc));
            Assert.That(graph.Npcs[index].RuntimeId,
                Is.EqualTo("npc-" + (index + 1).ToString("D6")));
        }
    }

    [Test]
    public void CoreGenesisBuilderRejectsIncompleteOrPostBuildMutation()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        List<string> cityIds = new List<string>(definitions.Configuration.cities);
        cityIds.RemoveAll(string.IsNullOrWhiteSpace);
        cityIds.Sort(System.StringComparer.Ordinal);
        for (int index = 0; index < cityIds.Count; index++)
        {
            DailyV1CityDefinition city = definitions.Cities[cityIds[index]];
            Assert.That(builder.TryAddInitialCity(
                "city-runtime-" + index, "location-runtime-" + index, city, out _), Is.True);
        }

        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph incomplete), Is.False);
        Assert.That(incomplete, Is.Null);
        DailyV1NpcSimulationConfig firstRow = definitions.Configuration.npcs.First(
            row => row != null && !string.IsNullOrWhiteSpace(row.npc));
        Assert.That(builder.TryAddInitialNpc(
            "npc-runtime-1", definitions.Npcs[firstRow.npc], firstRow, out _), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph stillIncomplete), Is.False);
        Assert.That(stillIncomplete, Is.Null);
    }

    [Test]
    public void FailedCoreGenesisCityOwnerCreationRollsBackItsSpatialLocation()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);

        Assert.That(builder.TryAddInitialCity("city-runtime-invalid", "location-runtime-orphan", null, out _), Is.False);
        Assert.That(builder.Cities, Is.Empty);
        Assert.That(builder.SpatialNetwork.LocationCount, Is.Zero);
        Assert.That(builder.SpatialNetwork.Revision, Is.Zero);
        Assert.That(builder.SpatialNetwork.ContainsLocationId("location-runtime-orphan"), Is.False);
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
            "shared-runtime", "location-shared-runtime", definitions, city, null,
            out DailyV1CityRuntimeOwners cityOwners), Is.True);
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
            "city-runtime", "location-city-runtime", definitions, city, null,
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
            "city-runtime", "location-city-runtime", definitions,
            definitions.Cities.Values.First(), null,
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
            "city-runtime", "location-city-runtime", definitions, city, null,
            out DailyV1CityRuntimeOwners owners), Is.True);
        List<string> output = new List<string>();

        LocalDailyMaterialFlowResult result = owners.SimulateSelectedLocalDailyMaterialFlow(
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
            "city-runtime", "location-city-runtime", definitions, city, null,
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
