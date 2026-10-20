using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedMerchantTradeStateOperationTests
{
    [Test]
    public void MerchantDailyPassObservesTheCoreMarketAndSpatialOwners()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out _);
        DailyV1NpcRuntimeOwners merchant = graph.Npcs.FirstOrDefault(owner =>
            DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue));
        Assert.That(merchant, Is.Not.Null);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(owner =>
            string.Equals(owner.RuntimeId, merchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        FakeBoundary boundary = new FakeBoundary();

        Assert.That(DailyV1SelectedMerchantTradeStateOperation.TryAdvance(
            graph, merchant, 37L, boundary), Is.True);

        Assert.That(merchant.SpatialKnowledge.KnowsLocation(city.LocationRuntimeId), Is.True);
        Assert.That(merchant.CommercialKnowledge.TryGetLiquidityObservation(
            city.LocationRuntimeId, out DailyV1CommercialLiquidityObservation liquidity), Is.True);
        Assert.That(liquidity.ObservedDay, Is.EqualTo(37L));
        foreach (DailyV1MarketItemState row in city.Market.Items)
        {
            Assert.That(merchant.CommercialKnowledge.TryGetObservation(city.LocationRuntimeId,
                row.Item.DefinitionId, out DailyV1CommercialMarketObservation observation), Is.True);
            Assert.That(observation.ItemDefinition, Is.SameAs(row.Item));
            Assert.That(observation.ObservedPrice, Is.EqualTo(row.CurrentPrice));
            Assert.That(observation.ObservedStock, Is.EqualTo(row.Amount));
            Assert.That(observation.ObservedDay, Is.EqualTo(37L));
        }
    }

    [Test]
    public void MerchantDailyPassClearsAPlanForLocalMerchantFromTheExactCoreOwner()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out _);
        DailyV1NpcRuntimeOwners merchant = graph.Npcs.FirstOrDefault(owner =>
            DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            && DailyV1NpcDefinitionRules.HasMerchantBehavior(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue));
        Assert.That(merchant, Is.Not.Null);
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(owner =>
            string.Equals(owner.RuntimeId, merchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        DailyV1CityRuntimeOwners target = graph.Cities.First(owner =>
            !ReferenceEquals(owner, origin));
        DailyV1ItemDefinition item = graph.Definitions.Items.Values.First();
        Assert.That(graph.TryInstallMerchantTradePlan(merchant, item,
            origin.RuntimeId, target.RuntimeId, 2, 1f, "decision/local-merchant"), Is.True);
        long priorRevision = merchant.MerchantTradePlan.Revision;

        Assert.That(DailyV1SelectedMerchantTradeStateOperation.TryAdvance(
            graph, merchant, 38L, new FakeBoundary()), Is.True);

        Assert.That(merchant.MerchantTradePlan.HasData, Is.False);
        Assert.That(merchant.MerchantTradePlan.Revision, Is.EqualTo(priorRevision + 1));
    }

    [Test]
    public void MerchantDailyPassAdvancesUnprofitablePlanWaitOnTheCoreOwner()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out _);
        DailyV1NpcRuntimeOwners merchant = graph.Npcs.FirstOrDefault(owner =>
            DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            && DailyV1NpcDefinitionRules.HasMerchantBehavior(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.TravelingMerchantBehaviorValue));
        Assert.That(merchant, Is.Not.Null);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(owner =>
            string.Equals(owner.RuntimeId, merchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        DailyV1MarketItemState marketRow = city.Market.Items.FirstOrDefault();
        Assert.That(marketRow, Is.Not.Null);
        Assert.That(merchant.Inventory.TryAddItem(marketRow.Item, 2), Is.True);
        Assert.That(graph.TryInstallMerchantTradePlan(merchant, marketRow.Item,
            city.RuntimeId, city.RuntimeId, 2, marketRow.CurrentPrice + 100f,
            "decision/unprofitable-plan"), Is.True);
        long priorRevision = merchant.MerchantTradePlan.Revision;

        Assert.That(DailyV1SelectedMerchantTradeStateOperation.TryAdvance(
            graph, merchant, 39L, new FakeBoundary()), Is.True);

        Assert.That(merchant.MerchantTradePlan.WaitDaysAtDestination, Is.EqualTo(1));
        Assert.That(merchant.MerchantTradePlan.Revision, Is.EqualTo(priorRevision + 1));
    }

    [Test]
    public void MerchantDailyPassRedirectsFromKnownCoreRouteAndRecordsCoreEvidence()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out SpatialNetworkOwner spatial);
        DailyV1NpcRuntimeOwners merchant = graph.Npcs.FirstOrDefault(owner =>
            DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            && DailyV1NpcDefinitionRules.HasMerchantBehavior(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.TravelingMerchantBehaviorValue));
        Assert.That(merchant, Is.Not.Null);
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(owner =>
            string.Equals(owner.RuntimeId, merchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        DailyV1CityRuntimeOwners destination = graph.Cities.First(owner =>
            !ReferenceEquals(owner, origin));
        DailyV1ItemDefinition item = graph.Definitions.Items.Values.First();
        Assert.That(merchant.Inventory.TryAddItem(item, 2), Is.True);
        Assert.That(graph.TryInstallMerchantTradePlan(merchant, item,
            origin.RuntimeId, origin.RuntimeId, 2, origin.Market.GetPrice(item) + 10f,
            "decision/original"), Is.True);
        Assert.That(spatial.TryGetLocation(origin.LocationRuntimeId,
            out SpatialLocationRuntime originLocation), Is.True);
        Assert.That(spatial.TryGetLocation(destination.LocationRuntimeId,
            out SpatialLocationRuntime destinationLocation), Is.True);
        SpatialRouteRuntime route = new SpatialRouteRuntime("route/merchant-daily-redirect",
            originLocation, destinationLocation, 2);
        Assert.That(spatial.TryRegisterRoute(route, () => true), Is.True);
        merchant.SpatialKnowledge.DiscoverLocation(destination.LocationRuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
        Assert.That(merchant.CommercialKnowledge.RecordObservation(
            new DailyV1CommercialMarketObservation(destination.LocationRuntimeId,
                item, 10000f, 1000, 39L, 39L,
                DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        FakeBoundary boundary = new FakeBoundary();

        Assert.That(DailyV1SelectedMerchantTradeStateOperation.TryAdvance(
            graph, merchant, 39L, boundary), Is.True);

        Assert.That(merchant.MerchantTradePlan.TargetCityRuntimeId,
            Is.EqualTo(destination.RuntimeId));
        Assert.That(boundary.RecordedRedirectLocationRuntimeId,
            Is.EqualTo(destination.LocationRuntimeId));
        Assert.That(boundary.RedirectEvidence, Is.Not.Null);
        Assert.That(boundary.RedirectEvidence.TradeDestinationLocationRuntimeId,
            Is.EqualTo(destination.LocationRuntimeId));
    }

    [Test]
    public void MissingTravelSectionUsesTheSameCostForPlanFeasibilityAndCommit()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out SpatialNetworkOwner spatial,
            missingTravelConfiguration: true);
        DailyV1NpcRuntimeOwners merchant = graph.Npcs.First(owner =>
            DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            && DailyV1NpcDefinitionRules.HasMerchantBehavior(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.TravelingMerchantBehaviorValue));
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(owner =>
            string.Equals(owner.LocationRuntimeId, merchant.Travel.CurrentLocationRuntimeId,
                StringComparison.Ordinal));
        DailyV1CityRuntimeOwners destination = graph.Cities.First(owner =>
            !ReferenceEquals(owner, origin));
        Assert.That(spatial.TryGetLocation(origin.LocationRuntimeId,
            out SpatialLocationRuntime originLocation), Is.True);
        Assert.That(spatial.TryGetLocation(destination.LocationRuntimeId,
            out SpatialLocationRuntime destinationLocation), Is.True);
        SpatialRouteRuntime route = new SpatialRouteRuntime("route/missing-travel-section",
            originLocation, destinationLocation, 2);
        Assert.That(spatial.TryRegisterRoute(route, () => true), Is.True);
        Assert.That(graph.TryGetMerchantTradeTravelCost(route, out float committedCost), Is.True);
        Assert.That(committedCost, Is.EqualTo(2f));

        DailyV1ItemDefinition item = graph.Definitions.Items.Values.First();
        Assert.That(merchant.Inventory.TryAddItem(item, 2), Is.True);
        Assert.That(graph.TryInstallMerchantTradePlan(merchant, item, origin.RuntimeId,
            destination.RuntimeId, 2, 1f, "decision/missing-travel-section"), Is.True);
        merchant.SpatialKnowledge.DiscoverLocation(destination.LocationRuntimeId);
        merchant.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
        typeof(MoneyAccountRuntime).GetField("balance", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(merchant.MoneyAccount, 0f);
        FakeBoundary boundary = new FakeBoundary();

        Assert.That(DailyV1SelectedMerchantTradeStateOperation.TryAdvance(
            graph, merchant, 40L, boundary), Is.True);

        Assert.That(boundary.TradeTravelPlanAttempts, Is.Zero,
            "travel feasibility uses the same one-unit-per-day fallback as the owner commit");
    }

    [Test]
    public void PendingTravelDayMaintenanceUsesExactPlanOwnerAndSkipsTravelingOrSaturatedPlans()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out _);
        DailyV1NpcRuntimeOwners merchant = graph.Npcs.First(owner =>
            owner.Lifecycle.LifeState == NpcLifeState.Alive
            && !owner.Travel.IsTraveling
            && DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            && graph.Cities.Any(city => string.Equals(city.RuntimeId,
                owner.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)));
        DailyV1CityRuntimeOwners currentCity = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, merchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        DailyV1CityRuntimeOwners targetCity = graph.Cities.First(city =>
            !ReferenceEquals(city, currentCity));
        DailyV1ItemDefinition item = graph.Definitions.Items.Values.First();
        Assert.That(merchant.MerchantTradePlan.TryInstallState(item,
            currentCity.RuntimeId, targetCity.RuntimeId, 4, 3, 6f, 2, 7,
            "decision/pending-day"), Is.True);
        long beforeRevision = merchant.MerchantTradePlan.Revision;
        FakeBoundary boundary = new FakeBoundary();

        Assert.That(DailyV1SelectedMerchantTradeStateOperation.TryAdvancePendingTravelDays(
            graph, graph.Npcs, boundary), Is.True);

        Assert.That(boundary.PendingTravelDayActorIds.Count(id => id == merchant.RuntimeId), Is.EqualTo(1));
        Assert.That(merchant.MerchantTradePlan.PendingTravelDays, Is.EqualTo(8));
        Assert.That(merchant.MerchantTradePlan.WaitDaysAtDestination, Is.EqualTo(2));
        Assert.That(merchant.MerchantTradePlan.RemainingAmount, Is.EqualTo(3));
        Assert.That(merchant.MerchantTradePlan.Revision, Is.EqualTo(beforeRevision + 1));

        Assert.That(merchant.Travel.TryCommitState(currentCity.LocationRuntimeId,
            currentCity.RuntimeId, targetCity.LocationRuntimeId, targetCity.RuntimeId,
            2, 2, null, true, null, null, true), Is.True);
        Assert.That(DailyV1SelectedMerchantTradeStateOperation.TryAdvancePendingTravelDays(
            graph, graph.Npcs, boundary), Is.True);
        Assert.That(boundary.PendingTravelDayActorIds.Count(id => id == merchant.RuntimeId), Is.EqualTo(1));

        Assert.That(merchant.Travel.TryCommitState(currentCity.LocationRuntimeId,
            currentCity.RuntimeId, null, null, 0, 0, null, false, null, null, true), Is.True);
        Assert.That(merchant.MerchantTradePlan.TryInstallState(item,
            currentCity.RuntimeId, targetCity.RuntimeId, 4, 3, 6f, 2, int.MaxValue,
            "decision/pending-day"), Is.True);
        Assert.That(DailyV1SelectedMerchantTradeStateOperation.TryAdvancePendingTravelDays(
            graph, graph.Npcs, boundary), Is.True);
        Assert.That(boundary.PendingTravelDayActorIds.Count(id => id == merchant.RuntimeId), Is.EqualTo(1));
        Assert.That(merchant.MerchantTradePlan.PendingTravelDays, Is.EqualTo(int.MaxValue));
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(out SpatialNetworkOwner spatial,
        bool missingTravelConfiguration = false)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        if (missingTravelConfiguration)
        {
            EffectiveSimulationConfiguration existing = definitions.EffectiveConfiguration;
            EffectiveSimulationConfiguration withoutTravel = new EffectiveSimulationConfiguration(
                existing.Population, existing.Economy, null, existing.Crime, existing.GuardCrime,
                existing.NaturalMortality, existing.AggregateDemography, existing.MerchantTrade,
                existing.CommercialKnowledge);
            definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
                definitions.Configuration, withoutTravel, definitions.Calendar,
                definitions.Cities.Values.Select(value => value.Values),
                definitions.Items.Values.Select(value => value.Values),
                definitions.Npcs.Values.Select(value => value.Values),
                definitions.Jobs.Values.Select(value => value.Values),
                definitions.Actions.Values.Select(value => value.Values),
                definitions.Statuses.Values.Select(value => value.Values), definitions.StageOrder,
                definitions.ProfileContractIdentity, definitions.GeographyProfileContractIdentity,
                definitions.GeographyStageIdentity, definitions.HexCoordinateConvention,
                definitions.HexCoordinateCanonicalOrder,
                definitions.Traits.Values.Select(value => new KeyValuePair<string, string>(
                    value.DefinitionId, value.DisplayName)),
                definitions.CapabilityAttributes.Values.Select(value => new KeyValuePair<string, string>(
                    value.DefinitionId, value.DisplayName)),
                definitions.ExplorableSites.Values.Select(value => value.Values));
        }
        spatial = new SpatialNetworkOwner();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions, spatial);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        return graph;
    }

    private sealed class FakeBoundary : IDailyV1SelectedMerchantTradeStateBoundary
    {
        internal string RecordedRedirectLocationRuntimeId;
        internal CommercialDecisionEvidence RedirectEvidence;
        internal int TradeTravelPlanAttempts;
        internal readonly List<string> PendingTravelDayActorIds = new List<string>();

        public bool TryInstallPlanState(DailyV1NpcRuntimeOwners actor,
            DailyV1ItemDefinition item, string originCityRuntimeId,
            string targetCityRuntimeId, int plannedAmount, int remainingAmount,
            float purchasePricePerItem, int waitDaysAtDestination,
            int pendingTravelDays, string originDecisionId)
        {
            PendingTravelDayActorIds.Add(actor.RuntimeId);
            return actor.MerchantTradePlan.TryInstallState(item, originCityRuntimeId,
                targetCityRuntimeId, plannedAmount, remainingAmount,
                purchasePricePerItem, waitDaysAtDestination, pendingTravelDays,
                originDecisionId);
        }

        public bool TrySetTradeTravelPlan(DailyV1NpcRuntimeOwners actor,
            string targetCityRuntimeId, float utility, string originDecisionId)
        {
            TradeTravelPlanAttempts++;
            return false;
        }

        public void ClearTradeTravelPlan(DailyV1NpcRuntimeOwners actor) { }

        public NpcDecisionRecord RecordTradeRedirect(DailyV1NpcRuntimeOwners actor,
            string targetLocationRuntimeId, CommercialDecisionEvidence evidence)
        {
            RecordedRedirectLocationRuntimeId = targetLocationRuntimeId;
            RedirectEvidence = evidence;
            return null;
        }

        public void LogTrade(string message) { }
    }
}
