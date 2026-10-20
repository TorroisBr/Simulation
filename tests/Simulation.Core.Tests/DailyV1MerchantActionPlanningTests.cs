using System.Collections.Generic;
using System;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1MerchantActionPlanningTests
{
    [Test]
    public void SelectedAutonomousSellCandidateUsesCorePlanAndRejectsMissingOpportunity()
    {
        CreateRuntimeFixture(out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners npc,
            out DailyV1CityRuntimeOwners city);
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        Assert.That(npc.Inventory.TryAddItem(iron, 5, 10f), Is.True);
        Assert.That(npc.CommercialKnowledge.RecordObservation(
            new DailyV1CommercialMarketObservation(
                city.LocationRuntimeId, iron, 13f, 12, 20L, 20L,
                DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(
            definitions.Actions["VENDER_MERCADORIA"], 18f);

        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedSellGoodsCandidate(
            graph, npc, candidate, 20L), Is.True);
        Assert.That(candidate.PreparedPayload, Is.Not.Null);
        Assert.That(candidate.PreparedPayload.Action, Is.SameAs(candidate.Action));
        Assert.That(candidate.PreparedPayload.TargetItem, Is.SameAs(iron));
        Assert.That(candidate.PreparedPayload.Amount, Is.GreaterThan(0));
        Assert.That(candidate.Utility, Is.GreaterThan(18f));

        DailyV1NpcActionCandidate unavailable = new DailyV1NpcActionCandidate(
            definitions.Actions["VENDER_MERCADORIA"], 18f);
        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedSellGoodsCandidate(
            graph, npc, unavailable, 1000L), Is.True);
        Assert.That(unavailable.Utility, Is.Zero);
        Assert.That(unavailable.PreparedPayload, Is.Null);
    }

    [Test]
    public void SelectedLocalSaleReadsExactOwnersAndReturnsExistingCommercialEvidence()
    {
        CreateRuntimeFixture(out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners npc,
            out DailyV1CityRuntimeOwners city);
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        Assert.That(npc.Inventory.TryAddItem(iron, 5, 10f), Is.True);
        float referencePrice = npc.Inventory.GetAverageUnitCost(iron);
        Assert.That(npc.CommercialKnowledge.RecordObservation(
            new DailyV1CommercialMarketObservation(
                city.LocationRuntimeId, iron, referencePrice + 3f, 12, 20L, 20L,
                DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        DailyV1ActionDefinition action = definitions.Actions["VENDER_MERCADORIA"];

        Assert.That(DailyV1MerchantActionPlanning.TryPlanSelectedLocalSale(
            graph, npc, action, 20L, 78f,
            out DailyV1MerchantLocalSalePlan plan), Is.True);

        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.Item, Is.SameAs(iron));
        Assert.That(plan.Amount, Is.GreaterThan(0));
        Assert.That(plan.ReferencePrice, Is.EqualTo(referencePrice));
        Assert.That(plan.LocalPrice, Is.EqualTo(referencePrice + 3f));
        Assert.That(plan.Utility, Is.EqualTo(Math.Max(78f, 25f + plan.Score)));
        Assert.That(plan.Evidence.ItemDefinitionId, Is.EqualTo(iron.DefinitionId));
        Assert.That(plan.Evidence.CurrentLocationRuntimeId, Is.EqualTo(city.LocationRuntimeId));
        Assert.That(plan.Evidence.TradeOriginLocationRuntimeId, Is.EqualTo(city.LocationRuntimeId));
        Assert.That(plan.Evidence.TradeDestinationLocationRuntimeId, Is.EqualTo(city.LocationRuntimeId));
        Assert.That(plan.Evidence.ExpectedQuantity, Is.EqualTo(plan.Amount));
        Assert.That(plan.Evidence.ExpectedPurchaseUnitPrice, Is.EqualTo(referencePrice),
            "The existing selected Trade profit log uses the exact inventory average cost carried by Core evidence.");
        Assert.That(plan.Evidence.ExpectedSaleUnitPrice, Is.EqualTo(referencePrice + 3f));
        Assert.That(plan.Evidence.DestinationObservation.LocationRuntimeId, Is.EqualTo(city.LocationRuntimeId));
        Assert.That(plan.SelectedPayload, Is.Not.Null);
        Assert.That(plan.SelectedPayload.Action, Is.SameAs(action));
        Assert.That(plan.SelectedPayload.TargetCityRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(plan.SelectedPayload.TargetItem, Is.SameAs(iron));
        Assert.That(plan.SelectedPayload.Amount, Is.EqualTo(plan.Amount));
        Assert.That(plan.SelectedPayload.ExpectedUnitPrice, Is.EqualTo(plan.LocalPrice));
        Assert.That(plan.SelectedPayload.CommercialDecisionEvidence, Is.SameAs(plan.Evidence));
    }

    [Test]
    public void SelectedPlannedSaleChoosesOrderedLocalBuyerAndUsesFreshnessReserveAndPlanOwners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder = new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            id => builder.ResolveUniqueStartingCity(id)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners seller = graph.Npcs.Single(owner => owner.Definition.DefinitionId == "JORGE");
        DailyV1CityRuntimeOwners city = graph.Cities.Single(owner => owner.Definition.DefinitionId == "CAMPO_VERDE");
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        DailyV1ActionDefinition sell = definitions.Actions["VENDER_MERCADORIA"];
        Assert.That(seller.Inventory.TryAddItem(item, 5, 10f), Is.True);
        Assert.That(graph.TryInstallMerchantTradePlan(seller, item, city.RuntimeId,
            city.RuntimeId, 5, 10f, "decision-plan"), Is.True);
        Assert.That(seller.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            city.LocationRuntimeId, item, 100f, 20, 20L, 20L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);

        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(sell, 75f);
        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedSellGoodsCandidate(
            graph, seller, candidate, 20L), Is.True);
        Assert.That(candidate.PreparedPayload, Is.Not.Null);
        Assert.That(candidate.PreparedPayload.TargetNpcRuntimeId,
            Is.EqualTo(graph.Npcs.Single(owner => owner.Definition.DefinitionId == "PEDRO").RuntimeId));

        Assert.That(DailyV1MerchantActionPlanning.TryPlanSelectedPlannedLocalBuyer(
            graph, seller, sell, 20L, 75f,
            out DailyV1MerchantLocalSalePlan plan), Is.True);

        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.TargetNpcRuntimeId, Is.EqualTo(graph.Npcs.Single(owner =>
            owner.Definition.DefinitionId == "PEDRO").RuntimeId));
        Assert.That(plan.Utility, Is.EqualTo(Math.Max(75f, 85f + plan.Score)));
        Assert.That(candidate.Utility, Is.EqualTo(plan.Utility));
        Assert.That(plan.SelectedPayload.TargetCityRuntimeId, Is.Null,
            "NPC-targeted selected actions retain the existing null target-city identity.");
        Assert.That(plan.SelectedPayload.TargetNpcRuntimeId, Is.EqualTo(plan.TargetNpcRuntimeId));
        Assert.That(plan.SelectedPayload.TargetItem, Is.SameAs(item));
        Assert.That(plan.SelectedPayload.OriginDecisionId, Is.Null);
        Assert.That(plan.Evidence.TradeOriginLocationRuntimeId, Is.EqualTo(city.LocationRuntimeId));
        Assert.That(plan.Evidence.TradeDestinationLocationRuntimeId, Is.EqualTo(city.LocationRuntimeId));
        Assert.That(plan.Evidence.ExpectedQuantity, Is.EqualTo(plan.Amount));
        Assert.That(plan.Evidence.ExpectedPurchaseUnitPrice, Is.EqualTo(10f));
        Assert.That(plan.Evidence.ExpectedSaleUnitPrice, Is.EqualTo(plan.LocalPrice));

        Assert.That(DailyV1MerchantActionPlanning.TryPlanSelectedPlannedLocalBuyer(
            graph, seller, sell, 20L + definitions.EffectiveConfiguration.CommercialKnowledge.MaxUsefulAgeDays + 2,
            75f, out DailyV1MerchantLocalSalePlan stalePlan), Is.True);
        Assert.That(stalePlan, Is.Null, "A stale market observation cannot select a local merchant buyer.");
    }

    [Test]
    public void SelectedPlannedMarketFallbackPreservesWaitThresholdAndExactCityPayload()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder = new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            id => builder.ResolveUniqueStartingCity(id)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners seller = graph.Npcs.Single(owner => owner.Definition.DefinitionId == "JORGE");
        DailyV1CityRuntimeOwners city = graph.Cities.Single(owner => owner.Definition.DefinitionId == "CAMPO_VERDE");
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        DailyV1ActionDefinition sell = definitions.Actions["VENDER_MERCADORIA"];
        Assert.That(seller.Inventory.TryAddItem(item, 5, 10f), Is.True);
        int waitThreshold = definitions.EffectiveConfiguration.MerchantTrade.MaxUnprofitablePlanWaitDays;
        Assert.That(seller.MerchantTradePlan.TryInstallState(item, city.RuntimeId, city.RuntimeId,
            5, 5, 100f, waitThreshold, 0, "decision-plan"), Is.True);
        Assert.That(seller.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            city.LocationRuntimeId, item, 20f, 20, 20L, 20L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);

        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(sell, 75f);
        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedSellGoodsCandidate(
            graph, seller, candidate, 20L), Is.True);
        Assert.That(candidate.PreparedPayload, Is.Not.Null);
        Assert.That(candidate.PreparedPayload.TargetNpcRuntimeId, Is.Null);
        Assert.That(candidate.PreparedPayload.TargetCityRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(candidate.Utility, Is.EqualTo(75f));

        Assert.That(DailyV1MerchantActionPlanning.TryPlanSelectedPlannedMarketSale(
            graph, seller, sell, 20L, 75f, out DailyV1MerchantLocalSalePlan liquidation), Is.True);
        Assert.That(liquidation, Is.Not.Null);
        Assert.That(liquidation.Amount, Is.EqualTo(5));
        Assert.That(liquidation.SelectedPayload.TargetCityRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(liquidation.SelectedPayload.TargetNpcRuntimeId, Is.Null);
        Assert.That(liquidation.SelectedPayload.TargetItem, Is.SameAs(item));
        Assert.That(liquidation.Evidence.ExpectedPurchaseUnitPrice, Is.EqualTo(100f));
        Assert.That(liquidation.Evidence.ExpectedSaleUnitPrice, Is.EqualTo(20f));
        Assert.That(liquidation.Utility, Is.EqualTo(75f));

        Assert.That(seller.MerchantTradePlan.TryInstallState(item, city.RuntimeId, city.RuntimeId,
            5, 5, 100f, Math.Max(0, waitThreshold - 1), 0, "decision-plan"), Is.True);
        Assert.That(DailyV1MerchantActionPlanning.TryPlanSelectedPlannedMarketSale(
            graph, seller, sell, 20L, 0f, out DailyV1MerchantLocalSalePlan premature), Is.True);
        Assert.That(premature, Is.Null, "Loss liquidation waits until the configured destination threshold.");
    }

    [Test]
    public void SelectedBuyPlanningUsesKnownDirectRouteAndExactOwnerObservations()
    {
        CreateRuntimeFixture(out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners npc,
            out DailyV1CityRuntimeOwners origin);
        DailyV1CityRuntimeOwners destination = graph.Cities.First(value => !ReferenceEquals(value, origin));
        Assert.That(graph.SpatialNetwork.TryGetLocation(origin.LocationRuntimeId,
            out SpatialLocationRuntime originLocation), Is.True);
        Assert.That(graph.SpatialNetwork.TryGetLocation(destination.LocationRuntimeId,
            out SpatialLocationRuntime destinationLocation), Is.True);
        SpatialRouteRuntime route = new SpatialRouteRuntime(
            "route/daily-v1-merchant-test", originLocation, destinationLocation, 2);
        Assert.That(graph.SpatialNetwork.TryRegisterRoute(route, () => true), Is.True);

        Assert.That(SpatialKnowledgeRuntime.TryCreateFromOwnerSnapshot(
            npc.RuntimeId,
            new[] { origin.LocationRuntimeId, destination.LocationRuntimeId },
            new[] { route.RuntimeId }, 0L,
            out SpatialKnowledgeRuntime spatialKnowledge), Is.True);
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateBound(
            npc.RuntimeId, definitions, npc.Definition, npc.Status.Statuses,
            npc.Inventory, npc.MoneyAccount, npc.CommercialKnowledge,
            npc.MerchantTradePlan, spatialKnowledge,
            out DailyV1NpcRuntimeOwners detachedMerchant), Is.True);
        Assert.That(detachedMerchant.Lifecycle.TryInstallState(
            npc.Lifecycle.LifeState, npc.Lifecycle.InjurySeverity,
            npc.Lifecycle.LifeStateRevision), Is.True);
        Assert.That(detachedMerchant.Travel.TryInstallState(
            npc.Travel.CurrentLocationRuntimeId, npc.Travel.CurrentCityRuntimeId,
            npc.Travel.DestinationLocationRuntimeId, npc.Travel.DestinationCityRuntimeId,
            npc.Travel.TravelDaysRemaining, npc.Travel.TravelDaysTotal,
            npc.Travel.TravelRouteRuntimeId, npc.Travel.TravelStartedToday,
            npc.Travel.TravelOriginDecisionId, npc.Travel.ActiveTravelPartyId,
            npc.Travel.Revision), Is.True);
        List<DailyV1NpcRuntimeOwners> detachedNpcs = graph.Npcs
            .Select(value => ReferenceEquals(value, npc) ? detachedMerchant : value).ToList();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, graph.Cities, detachedNpcs, graph.SpatialNetwork,
            out DailyV1SelectedRuntimeOwnerGraph detachedGraph), Is.True);
        npc = detachedMerchant;
        graph = detachedGraph;

        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        Assert.That(npc.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            origin.LocationRuntimeId, iron, 5f, 50, 20L, 20L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(npc.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            destination.LocationRuntimeId, iron, 15f, 10, 20L, 20L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(npc.MoneyAccount.TryCredit(1000f), Is.True);

        Assert.That(DailyV1MerchantActionPlanning.TryPlanSelectedTradeOpportunity(
            graph, npc, definitions.Actions["COMPRAR_MERCADORIA"],
            origin.RuntimeId, 20L, 0f, 0,
            out DailyV1MerchantTradePlan plan), Is.True);
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.Item, Is.SameAs(iron));
        Assert.That(plan.TargetRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(plan.Amount, Is.GreaterThan(0));
        Assert.That(plan.BuyPrice, Is.EqualTo(5f));
        Assert.That(plan.SellPrice, Is.EqualTo(15f));
        Assert.That(plan.Evidence, Is.Not.Null);
        Assert.That(plan.Evidence.OriginObservation.KnownPrice, Is.EqualTo(5f));
        Assert.That(plan.Evidence.DestinationObservation.KnownPrice, Is.EqualTo(15f));
        Assert.That(plan.SelectedPayload, Is.Not.Null);
        Assert.That(plan.SelectedPayload.Action, Is.SameAs(definitions.Actions["COMPRAR_MERCADORIA"]));
        Assert.That(plan.SelectedPayload.TargetCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(plan.SelectedPayload.TargetItem, Is.SameAs(iron));
        Assert.That(plan.SelectedPayload.Amount, Is.EqualTo(plan.Amount));
        Assert.That(plan.SelectedPayload.ExpectedUnitPrice, Is.EqualTo(plan.BuyPrice));
        Assert.That(plan.SelectedPayload.CommercialDecisionEvidence, Is.SameAs(plan.Evidence));

        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(
            definitions.Actions["COMPRAR_MERCADORIA"], 11f);
        Assert.That(DailyV1NpcActionDecision.TryPrepareSelectedBuyGoodsCandidate(
            graph, npc, candidate, 20L), Is.True);
        Assert.That(candidate.PreparedPayload, Is.Not.Null);
        Assert.That(candidate.PreparedPayload.TargetCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(candidate.PreparedPayload.TargetItem, Is.SameAs(iron));
        Assert.That(candidate.Utility, Is.EqualTo(Math.Max(11f, 30f + plan.Score)));
    }

    [TestCase(20L, 20L, 1f)]
    [TestCase(20L, 27L, 1f)]
    [TestCase(20L, 28L, 1f - 1f / 23f)]
    [TestCase(20L, 50L, 0f)]
    [TestCase(20L, -1L, 1f)]
    public void SelectedCommercialFreshnessMatchesExistingPolicyBoundaries(
        long observedDay, long currentDay, float expected)
    {
        float actual = DailyV1CommercialKnowledgeFreshness.GetFreshness(
            observedDay, currentDay, new EffectiveCommercialKnowledgeConfiguration());
        Assert.That(actual, Is.EqualTo(expected).Within(0.000001f));
    }

    [Test]
    public void SelectedLocalSaleRejectsStaleAndBelowThresholdOwnerFacts()
    {
        CreateRuntimeFixture(out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners npc,
            out DailyV1CityRuntimeOwners city);
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        Assert.That(npc.Inventory.TryAddItem(iron, 5, 10f), Is.True);
        float referencePrice = npc.Inventory.GetAverageUnitCost(iron);
        DailyV1ActionDefinition action = definitions.Actions["VENDER_MERCADORIA"];

        Assert.That(npc.CommercialKnowledge.RecordObservation(
            new DailyV1CommercialMarketObservation(
                city.LocationRuntimeId, iron, referencePrice + 10f, 12, 0L, 0L,
                DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(DailyV1MerchantActionPlanning.TryPlanSelectedLocalSale(
            graph, npc, action, 30L, 78f, out DailyV1MerchantLocalSalePlan stalePlan), Is.True);
        Assert.That(stalePlan, Is.Null);

        CreateRuntimeFixture(out definitions, out graph, out npc, out city);
        iron = definitions.Items["FERRO"];
        Assert.That(npc.Inventory.TryAddItem(iron, 5, 10f), Is.True);
        referencePrice = npc.Inventory.GetAverageUnitCost(iron);
        Assert.That(npc.CommercialKnowledge.RecordObservation(
            new DailyV1CommercialMarketObservation(
                city.LocationRuntimeId, iron, referencePrice + 0.5f, 12, 20L, 20L,
                DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(DailyV1MerchantActionPlanning.TryPlanSelectedLocalSale(
            graph, npc, definitions.Actions["VENDER_MERCADORIA"],
            20L, 78f, out DailyV1MerchantLocalSalePlan belowThreshold), Is.True);
        Assert.That(belowThreshold, Is.Null);
    }

    [Test]
    public void LocalSalePreservesSelectedItemIdentityAndExistingUtilityOrdering()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["AFONSO"];
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        DailyV1ItemDefinition grain = definitions.Items["TRIGO"];
        DailyV1InventoryOwner inventory = new DailyV1InventoryOwner(definitions);
        Assert.That(inventory.TryAddItem(iron, 5, 10f), Is.True);
        Assert.That(inventory.TryAddItem(grain, 5, 1f), Is.True);
        List<DailyV1MerchantLocalSaleFact> marketFacts = new List<DailyV1MerchantLocalSaleFact>
        {
            new DailyV1MerchantLocalSaleFact(iron, 15f, 1f, 4),
            new DailyV1MerchantLocalSaleFact(grain, 9f, 1f, 4)
        };

        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, merchant, inventory, marketFacts, 5, 1f, out DailyV1MerchantLocalSalePlan plan), Is.True);

        // The authored iron preference is 2x. Existing formula scores iron at 40
        // and grain at 32, despite grain's greater per-unit margin.
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.MarketFactIndex, Is.EqualTo(0));
        Assert.That(plan.Item, Is.SameAs(iron));
        Assert.That(plan.Amount, Is.EqualTo(4));
        Assert.That(plan.ReferencePrice, Is.EqualTo(10f));
        Assert.That(plan.ProfitPerItem, Is.EqualTo(5f));
        Assert.That(plan.Score, Is.EqualTo(40f));
    }

    [Test]
    public void LocalSaleUsesTypedBasePriceAndOrdinalTieBreak()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["JORGE"];
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        DailyV1ItemDefinition grain = definitions.Items["TRIGO"];
        DailyV1InventoryOwner inventory = new DailyV1InventoryOwner(definitions);
        Assert.That(inventory.TryAddItem(grain, 2), Is.True);
        Assert.That(inventory.TryAddItem(iron, 2), Is.True);
        List<DailyV1MerchantLocalSaleFact> marketFacts = new List<DailyV1MerchantLocalSaleFact>
        {
            new DailyV1MerchantLocalSaleFact(grain, grain.Values.basePrice + 4f, 1f, 1),
            new DailyV1MerchantLocalSaleFact(iron, iron.Values.basePrice + 4f, 1f, 1)
        };

        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, merchant, inventory, marketFacts, 5, 1f, out DailyV1MerchantLocalSalePlan plan), Is.True);

        Assert.That(plan.Item, Is.SameAs(iron),
            "Equal scores retain the existing ordinal DefinitionId tie-break rather than inventory or market-fact order.");
        Assert.That(plan.MarketFactIndex, Is.EqualTo(1));
        Assert.That(plan.ReferencePrice, Is.EqualTo(iron.Values.basePrice),
            "Selected planning reads the captured effective item value, not a Unity ItemData projection.");
    }

    [Test]
    public void LocalSaleRejectsForeignMerchantAndItemNodes()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["JORGE"];
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        DailyV1InventoryOwner inventory = new DailyV1InventoryOwner(definitions);
        Assert.That(inventory.TryAddItem(item, 3, 1f), Is.True);
        DailyV1MerchantLocalSaleFact foreignFact = new DailyV1MerchantLocalSaleFact(
            foreign.Items[item.DefinitionId], 5f, 1f, 2);

        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, foreign.Npcs[merchant.DefinitionId], inventory, new[] { foreignFact }, 3, 0f, out _), Is.False);
        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, merchant, inventory, new[] { foreignFact }, 3, 0f, out _), Is.False);
        DailyV1InventoryOwner foreignInventory = new DailyV1InventoryOwner(foreign);
        Assert.That(foreignInventory.TryAddItem(foreign.Items[item.DefinitionId], 3), Is.True);
        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, merchant, foreignInventory,
            new[] { new DailyV1MerchantLocalSaleFact(item, 5f, 1f, 2) }, 3, 0f, out _), Is.False);
    }

    [Test]
    public void LocalSaleSkipsStaleEmptyAndBelowThresholdCandidates()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["JORGE"];
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        DailyV1InventoryOwner inventory = new DailyV1InventoryOwner(definitions);
        Assert.That(inventory.TryAddItem(iron, 5, 10f), Is.True);

        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, merchant, inventory,
            new[] { new DailyV1MerchantLocalSaleFact(iron, 20f, 0f, 5) },
            5, 1f, out DailyV1MerchantLocalSalePlan plan), Is.True);
        Assert.That(plan, Is.Null, "Stale market facts are skipped.");

        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, merchant, inventory,
            new[] { new DailyV1MerchantLocalSaleFact(iron, 20f, 1f, 0) },
            5, 1f, out plan), Is.True);
        Assert.That(plan, Is.Null, "Market liquidity can make the effective available amount empty.");

        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, merchant, inventory,
            new[] { new DailyV1MerchantLocalSaleFact(iron, 10.5f, 1f, 5) },
            5, 1f, out plan), Is.True);
        Assert.That(plan, Is.Null);
    }

    [Test]
    public void TradeOpportunityAppliesTypedPreferenceAndExactProfitScoring()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["AFONSO"];
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        DailyV1ItemDefinition grain = definitions.Items["TRIGO"];
        List<DailyV1MerchantTradeCandidate> candidates = new List<DailyV1MerchantTradeCandidate>
        {
            new DailyV1MerchantTradeCandidate(iron, "city-iron", 2, 10f, 16f, 4f, 2, 0.5f, 0.8f),
            new DailyV1MerchantTradeCandidate(grain, "city-grain", 1, 1f, 11f, 0f, 1, 1f, 1f)
        };

        Assert.That(DailyV1MerchantActionPlanning.TryPlanTradeOpportunity(
            definitions, merchant, candidates, 1f, out DailyV1MerchantTradePlan plan), Is.True);

        // Iron's net profit is 8; 8 / 2 travel days * min(.5,.8) * authored 2x preference = 4.
        // Grain's score is 10, so it wins despite the Iron preference.
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.CandidateIndex, Is.EqualTo(1));
        Assert.That(plan.Item, Is.SameAs(grain));
        Assert.That(plan.NetProfit, Is.EqualTo(10f));
        Assert.That(plan.Score, Is.EqualTo(10f));
    }

    [Test]
    public void TradeOpportunityTieBreaksByDestinationBeforeItemIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["AFONSO"];
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        DailyV1ItemDefinition grain = definitions.Items["TRIGO"];
        List<DailyV1MerchantTradeCandidate> candidates = new List<DailyV1MerchantTradeCandidate>
        {
            new DailyV1MerchantTradeCandidate(iron, "city-z", 1, 10f, 15f, 0f, 1, 1f, 1f),
            new DailyV1MerchantTradeCandidate(grain, "city-a", 1, 1f, 11f, 0f, 1, 1f, 1f),
            new DailyV1MerchantTradeCandidate(iron, "city-a", 1, 10f, 15f, 0f, 1, 1f, 1f)
        };

        Assert.That(DailyV1MerchantActionPlanning.TryPlanTradeOpportunity(
            definitions, merchant, candidates, 1f, out DailyV1MerchantTradePlan plan), Is.True);

        Assert.That(plan.CandidateIndex, Is.EqualTo(2),
            "Target city ordering wins first; equal city and score then use the ordinal item DefinitionId.");
        Assert.That(plan.TargetRuntimeId, Is.EqualTo("city-a"));
        Assert.That(plan.Item, Is.SameAs(iron));
    }

    [Test]
    public void TradeOpportunityRejectsForeignNodesAndSkipsIneligibleCandidates()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["JORGE"];
        DailyV1ItemDefinition item = definitions.Items["FERRO"];

        Assert.That(DailyV1MerchantActionPlanning.TryPlanTradeOpportunity(
            definitions,
            foreign.Npcs[merchant.DefinitionId],
            new[] { new DailyV1MerchantTradeCandidate(item, "city-a", 1, 1f, 5f, 0f, 1, 1f, 1f) },
            0f,
            out _), Is.False);
        Assert.That(DailyV1MerchantActionPlanning.TryPlanTradeOpportunity(
            definitions,
            merchant,
            new[] { new DailyV1MerchantTradeCandidate(foreign.Items[item.DefinitionId], "city-a", 1, 1f, 5f, 0f, 1, 1f, 1f) },
            0f,
            out _), Is.False);

        Assert.That(DailyV1MerchantActionPlanning.TryPlanTradeOpportunity(
            definitions,
            merchant,
            new[]
            {
                new DailyV1MerchantTradeCandidate(item, "city-a", 1, 1f, 10f, 0f, 0, 1f, 1f),
                new DailyV1MerchantTradeCandidate(item, "city-b", 0, 1f, 10f, 0f, 1, 1f, 1f)
            },
            0f,
            out DailyV1MerchantTradePlan plan), Is.True);
        Assert.That(plan, Is.Null);
    }

    private static void CreateRuntimeFixture(
        out DailyV1EffectiveDefinitions definitions,
        out DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1NpcRuntimeOwners npc,
        out DailyV1CityRuntimeOwners city)
    {
        definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out graph), Is.True);
        npc = graph.Npcs.Single(value => value.Definition.DefinitionId == "AFONSO");
        string cityRuntimeId = npc.Travel.CurrentCityRuntimeId;
        city = graph.Cities.Single(value => value.RuntimeId == cityRuntimeId);
        Assert.That(DailyV1NpcDefinitionRules.HasJobType(npc.Definition, definitions,
            DailyV1NpcDefinitionRules.MerchantJobTypeValue), Is.True);
    }
}
