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
        List<string> registeredLocationIds = new List<string>();

        Assert.That(builder.TryCreateConfiguredCities(runtimeIds, locationRuntimeId =>
        {
            registeredLocationIds.Add(locationRuntimeId);
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
        Assert.That(registeredLocationIds, Has.Count.EqualTo(expectedCityDefinitions.Count));
        for (int index = 0; index < graph.Cities.Count; index++)
        {
            Assert.That(graph.Cities[index].Definition.DefinitionId,
                Is.EqualTo(expectedCityDefinitions[index]));
            Assert.That(graph.Cities[index].RuntimeId,
                Is.EqualTo("city-" + (index + 1).ToString("D6")));
            Assert.That(builder.TryGetLocationRuntimeId(graph.Cities[index], out string locationRuntimeId), Is.True);
            Assert.That(locationRuntimeId, Is.EqualTo("location-" + (index + 1).ToString("D6")));
            Assert.That(registeredLocationIds[index], Is.EqualTo(locationRuntimeId));
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
