using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedMerchantTradeStateOperationTests
{
    [Test]
    public void ActorTurnStageRunsCoreMerchantPassInsideTheHostAdmissionBoundary()
    {
        DailyV1SelectedRuntimeOwnerGraph authoredGraph = CreateGraph(out _);
        DailyV1NpcRuntimeOwners merchant = authoredGraph.Npcs.First(owner =>
            owner.Lifecycle.LifeState == NpcLifeState.Alive
            && !owner.Travel.IsTraveling
            && DailyV1NpcDefinitionRules.HasJobType(owner.Definition, authoredGraph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue));
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            authoredGraph.Cities, new[] { merchant }, authoredGraph.SpatialNetwork,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(owner =>
            string.Equals(owner.RuntimeId, merchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        Assert.That(city.Market.Items, Is.Not.Empty);

        PersonStore people = new PersonStore();
        PersonId personId = new PersonId("person-selected-merchant-pass");
        Assert.That(people.TryRegister(new PersonRuntime(personId), out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(people.TryBindMaterializedNpc(personId, merchant.RuntimeId,
            out PersonStoreFailure bindFailure), Is.True, bindFailure.ToString());
        Assert.That(graph.TryBindPersonStore(people), Is.True,
            "The actor operation uses the exact PersonStore identity bound to its selected owner graph.");
        ActorChoiceStore choices = new ActorChoiceStore(people);
        CoreActorTurnBoundary boundary = new CoreActorTurnBoundary(graph);
        long priorCommercialRevision = merchant.CommercialKnowledge.Revision;
        IReadOnlyList<DailyV1MarketItemState> marketRows = city.Market.Items;

        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph,
            new[] { new DailyV1ActorTurnRosterEntry(0, personId, merchant) },
            people, choices, null, 37L, merchantTradeEnabled: true,
            boundary: boundary), Is.True);

        Assert.That(boundary.AdmissionScopeCount, Is.EqualTo(1));
        Assert.That(boundary.CompletedScopeCount, Is.EqualTo(1));
        Assert.That(boundary.MerchantOwnerChangedInsideScope, Is.True,
            "The existing admission window encloses the Core operation's actual owner write.");
        Assert.That(boundary.LegacyMerchantCallbackCount, Is.Zero,
            "The selected actor-turn path invokes the Core merchant operation directly.");
        Assert.That(merchant.CommercialKnowledge.Revision, Is.GreaterThan(priorCommercialRevision));
        Assert.That(merchant.SpatialKnowledge.KnowsLocation(city.LocationRuntimeId), Is.True);
        foreach (DailyV1MarketItemState row in marketRows)
        {
            Assert.That(merchant.CommercialKnowledge.TryGetObservation(
                city.LocationRuntimeId, row.Item.DefinitionId,
                out DailyV1CommercialMarketObservation observation), Is.True);
            Assert.That(observation.ItemDefinition, Is.SameAs(row.Item));
            Assert.That(observation.ObservedDay, Is.EqualTo(37L));
        }
        Assert.That(merchant.CommercialKnowledge.TryGetLiquidityObservation(
            city.LocationRuntimeId, out DailyV1CommercialLiquidityObservation liquidity), Is.True);
        Assert.That(liquidity.ObservedDay, Is.EqualTo(37L));
    }

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
    public void CompletedTravelPartyArrivalObservesItsExactCurrentCityMarketInCore()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out _);
        DailyV1NpcRuntimeOwners merchant = graph.Npcs.First(owner =>
            owner.CommercialKnowledge != null
            && DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue));
        DailyV1CityRuntimeOwners city = graph.Cities.Single(owner =>
            string.Equals(owner.RuntimeId, merchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        TravelPartyRuntime party = new TravelPartyRuntime(
            "party/core-arrival-observation", city.LocationRuntimeId,
            city.LocationRuntimeId, "route/core-arrival-observation",
            new[] { merchant.RuntimeId }, Array.Empty<string>(), 1,
            "decision/core-arrival-observation",
            new[] { new TravelPartyMemberCost(merchant.RuntimeId, 0f) });
        long priorRevision = merchant.CommercialKnowledge.Revision;
        const long arrivalDay = 41L;

        Assert.That(DailyV1SelectedMerchantTradeStateOperation.ObserveCompletedTravelResults(
            graph,
            new[] { new DailyV1SelectedTravelPartyArrivalResult(
                party, new[] { merchant }, eventRecorded: true) },
            Array.Empty<DailyV1SelectedSoloTravelDayResult>(),
            arrivalDay), Is.True);

        Assert.That(merchant.CommercialKnowledge.Revision, Is.GreaterThan(priorRevision));
        foreach (DailyV1MarketItemState row in city.Market.Items)
        {
            Assert.That(merchant.CommercialKnowledge.TryGetObservation(
                city.LocationRuntimeId, row.Item.DefinitionId,
                out DailyV1CommercialMarketObservation observation), Is.True);
            Assert.That(observation.ItemDefinition, Is.SameAs(row.Item));
            Assert.That(observation.ObservedDay, Is.EqualTo(arrivalDay));
        }
        Assert.That(merchant.CommercialKnowledge.TryGetLiquidityObservation(
            city.LocationRuntimeId, out DailyV1CommercialLiquidityObservation liquidity), Is.True);
        Assert.That(liquidity.ObservedDay, Is.EqualTo(arrivalDay));
    }

    [Test]
    public void CompletedSoloArrivalObservesItsExactCurrentCityMarketInCore()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out _);
        DailyV1NpcRuntimeOwners merchant = graph.Npcs.First(owner =>
            owner.CommercialKnowledge != null
            && DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue));
        DailyV1CityRuntimeOwners city = graph.Cities.Single(owner =>
            string.Equals(owner.RuntimeId, merchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        long priorRevision = merchant.CommercialKnowledge.Revision;
        const long arrivalDay = 42L;

        Assert.That(DailyV1SelectedMerchantTradeStateOperation.ObserveCompletedTravelResults(
            graph,
            Array.Empty<DailyV1SelectedTravelPartyArrivalResult>(),
            new[] { new DailyV1SelectedSoloTravelDayResult(
                merchant, null, shouldReport: true, wasCommitted: true,
                arrived: true, originDecisionId: null) },
            arrivalDay), Is.True);

        Assert.That(merchant.CommercialKnowledge.Revision, Is.GreaterThan(priorRevision));
        foreach (DailyV1MarketItemState row in city.Market.Items)
        {
            Assert.That(merchant.CommercialKnowledge.TryGetObservation(
                city.LocationRuntimeId, row.Item.DefinitionId,
                out DailyV1CommercialMarketObservation observation), Is.True);
            Assert.That(observation.ItemDefinition, Is.SameAs(row.Item));
            Assert.That(observation.ObservedDay, Is.EqualTo(arrivalDay));
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

    private sealed class CoreActorTurnBoundary :
        IDailyV1ActorTurnExecutionBoundary,
        IDailyV1SelectedActorChoiceExecutionContext,
        IDailyV1CoreActorChoiceExecutionBoundary,
        IDailyV1SelectedMerchantTradeStateBoundary,
        IDailyV1SelectedMerchantTradeStateAdmissionBoundary
    {
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        private readonly DeterministicRandomSource random = new DeterministicRandomSource(7);
        internal int AdmissionScopeCount;
        internal int CompletedScopeCount;
        internal int LegacyMerchantCallbackCount;
        internal bool MerchantOwnerChangedInsideScope;

        internal CoreActorTurnBoundary(DailyV1SelectedRuntimeOwnerGraph graph) =>
            this.graph = graph;

        public bool IsInSpatialTransit(DailyV1NpcRuntimeOwners actor, PersonId personId) => false;
        public bool IsReservedToday(DailyV1NpcRuntimeOwners actor) => false;
        public void ProcessScheduledDirective(DailyV1NpcRuntimeOwners actor, ScheduledDirective directive) { }
        public bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal) => false;
        public void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor) => LegacyMerchantCallbackCount++;
        public void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor) { }

        public IAuthoritativeRandomSource RandomSource => random;
        public bool UsesSelectedDailyV1ActionExecution(DailyV1NpcRuntimeOwners actor) =>
            graph.ContainsExactNpcOwner(actor);
        public void LogSkippedDirective(ScheduledDirective directive, string reason) { }
        public bool TryCreateDecisionBoundary(
            DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedNpcActionDecisionBoundary decisionBoundary)
        {
            decisionBoundary = null;
            return false;
        }
        public bool TryGetDecisionRecordOwners(
            DailyV1NpcRuntimeOwners actor,
            out RuntimeIdAllocator idAllocator,
            out SimulationRecordSequence recordSequence,
            out NpcDecisionStore decisionStore)
        {
            idAllocator = null;
            recordSequence = null;
            decisionStore = null;
            return false;
        }
        public bool TryPrepareSelectedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate selected,
            IDailyV1SelectedNpcActionDecisionBoundary decisionBoundary,
            DailyV1NpcActionPayloadOwner payload) => false;
        public bool CanCommitSelectedActionSlot(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) => false;
        public bool TryPublishSelectedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) => false;
        public void CancelPreparedSelectedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) { }
        public void LogSelectedActionDecisionFailure(DailyV1NpcRuntimeOwners actor, string message) { }
        public bool TryPrepareCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) => false;
        public bool CanCommitCurrentActionClear(DailyV1NpcRuntimeOwners actor) => false;
        public bool TryPublishCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) => false;
        public void CancelPreparedCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) { }
        public void ClearCurrentAction(DailyV1NpcRuntimeOwners actor) =>
            graph.TryCommitCurrentActionSlot(actor, null, null, () => true);
        public bool IsActorAtCurrentCityLocation(DailyV1NpcRuntimeOwners actor) => false;
        public void LogChosenActionTarget(DailyV1NpcRuntimeOwners actor) { }
        public bool HasExactCurrentActionProjection(DailyV1NpcRuntimeOwners actor) => false;
        public bool TryGetDispatchBoundary(
            DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedActionDispatchBoundary dispatchBoundary)
        {
            dispatchBoundary = null;
            return false;
        }
        public bool TryCompleteDispatch(
            DailyV1NpcRuntimeOwners actor,
            DailyV1SelectedActionDispatchResult dispatch,
            out bool actionSucceeded,
            out NpcActionResultType? resultType,
            out string resultMessage)
        {
            actionSucceeded = false;
            resultType = null;
            resultMessage = null;
            return false;
        }
        public bool TryGetActionStatusBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcRuntimeOwners target,
            bool reuseExistingOperation,
            out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary boundary)
        {
            boundary = null;
            return false;
        }
        public bool TryPrepareForcedOutcomeActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            DailyV1NpcActionPayloadOwner payload) => false;
        public bool TryGetForcedEscapeBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        {
            boundary = null;
            return false;
        }

        public bool TryGetActorChoiceExecutionContext(
            DailyV1NpcRuntimeOwners actor,
            out bool actorAtCurrentCityLocation,
            out bool decisionSystemAvailable,
            out IDailyV1ActorChoiceExecutionBoundary executionBoundary)
        {
            actorAtCurrentCityLocation = graph.ContainsExactNpcOwner(actor);
            decisionSystemAvailable = true;
            executionBoundary = actorAtCurrentCityLocation ? this : null;
            return executionBoundary != null;
        }
        public bool TryPrepareRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) => false;
        public bool TryRefreshRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) => false;
        public bool CanCommitRequestedActionSlot(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) => false;
        public bool TryPublishRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) => false;
        public void CancelPreparedRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) { }
        public void LogRequestedActionDecisionFailure(string message) { }
        public NpcActionResultType? ExecuteCurrentAction(DailyV1NpcRuntimeOwners actor) =>
            throw new AssertionException("The selected Core actor-action boundary has no dispatch fixture.");

        public bool ExecuteWithinSelectedMerchantDailyOperation(
            DailyV1NpcRuntimeOwners actor, Func<bool> operation)
        {
            if (!graph.ContainsExactNpcOwner(actor) || operation == null) return false;
            AdmissionScopeCount++;
            long priorRevision = actor.CommercialKnowledge.Revision;
            if (!operation()) return false;
            CompletedScopeCount++;
            MerchantOwnerChangedInsideScope =
                actor.CommercialKnowledge.Revision > priorRevision;
            return true;
        }

        public bool TryInstallPlanState(DailyV1NpcRuntimeOwners actor,
            DailyV1ItemDefinition item, string originCityRuntimeId,
            string targetCityRuntimeId, int plannedAmount, int remainingAmount,
            float purchasePricePerItem, int waitDaysAtDestination,
            int pendingTravelDays, string originDecisionId) =>
            actor.MerchantTradePlan.TryInstallState(item, originCityRuntimeId,
                targetCityRuntimeId, plannedAmount, remainingAmount,
                purchasePricePerItem, waitDaysAtDestination, pendingTravelDays,
                originDecisionId);

        public bool TrySetTradeTravelPlan(DailyV1NpcRuntimeOwners actor,
            string targetCityRuntimeId, float utility, string originDecisionId) =>
            graph.TryCommitMerchantTradeTravelPlan(actor, targetCityRuntimeId,
                utility, originDecisionId, () => true, out _);

        public void ClearTradeTravelPlan(DailyV1NpcRuntimeOwners actor) { }
        public NpcDecisionRecord RecordTradeRedirect(DailyV1NpcRuntimeOwners actor,
            string targetLocationRuntimeId, CommercialDecisionEvidence evidence) => null;
        public void LogTrade(string message) { }
    }
}
