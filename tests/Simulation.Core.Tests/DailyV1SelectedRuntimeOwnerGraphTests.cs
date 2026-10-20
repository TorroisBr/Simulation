using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedRuntimeOwnerGraphTests
{
    [Test]
    public void MerchantTradePlanTransitionRequiresExactGraphMembersAndPreservesDecisionIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1CityRuntimeOwners origin = graph.Cities[0];
        DailyV1CityRuntimeOwners target = graph.Cities[1];
        DailyV1ItemDefinition item = definitions.Items.Values.First();

        Assert.That(graph.TryInstallMerchantTradePlan(npc, item, origin.RuntimeId,
            target.RuntimeId, 3, 2.5f, "decision-merchant-buy"), Is.True);
        Assert.That(npc.MerchantTradePlan.Revision, Is.EqualTo(1));
        Assert.That(npc.MerchantTradePlan.RawRemainingAmount, Is.EqualTo(3));
        Assert.That(npc.MerchantTradePlan.ItemDefinition, Is.SameAs(item));
        Assert.That(npc.MerchantTradePlan.OriginDecisionId, Is.EqualTo("decision-merchant-buy"));
        Assert.That(graph.TryInstallMerchantTradePlan(npc, item, origin.RuntimeId,
            target.RuntimeId, 3, 2.5f, "decision-merchant-buy"), Is.True,
            "An exact repeated transition is a no-op.");
        Assert.That(npc.MerchantTradePlan.Revision, Is.EqualTo(1));

        Assert.That(graph.TryInstallMerchantTradePlan(npc, item, origin.RuntimeId,
            "outside-city", 4, 2.5f, "decision-next"), Is.False);
        Assert.That(npc.MerchantTradePlan.Revision, Is.EqualTo(1));
        Assert.That(npc.MerchantTradePlan.OriginDecisionId, Is.EqualTo("decision-merchant-buy"));
    }

    [Test]
    public void MerchantTradeTravelPlanUsesCoreDirectRouteAndAdmissionBeforeOwnerCommit()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        SpatialNetworkOwner spatial = new SpatialNetworkOwner();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions, spatial);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        DailyV1CityRuntimeOwners routeOrigin = builder.Cities.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "CAMPO_VERDE", StringComparison.Ordinal));
        DailyV1CityRuntimeOwners routeDestination = builder.Cities.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "SERRA_DE_FERRO", StringComparison.Ordinal));
        Assert.That(spatial.TryGetLocation(routeOrigin.LocationRuntimeId,
            out SpatialLocationRuntime originLocation), Is.True);
        Assert.That(spatial.TryGetLocation(routeDestination.LocationRuntimeId,
            out SpatialLocationRuntime destinationLocation), Is.True);
        Assert.That(spatial.TryRegisterRoute(new SpatialRouteRuntime("route/merchant-test",
            originLocation, destinationLocation, 3), () => true), Is.True);
        Assert.That(spatial.TryRegisterRoute(new SpatialRouteRuntime("route/merchant-test-return",
            destinationLocation, originLocation, 3), () => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners npc = graph.Npcs.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "JORGE", StringComparison.Ordinal));
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(owner =>
            string.Equals(owner.RuntimeId, npc.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        DailyV1CityRuntimeOwners destination = graph.Cities.Single(owner =>
            !string.Equals(owner.RuntimeId, origin.RuntimeId, StringComparison.Ordinal));
        Assert.That(graph.TryPlanSelectedSoloTravel(npc, origin.LocationRuntimeId,
            destination.LocationRuntimeId, destination.RuntimeId,
            out SpatialRouteRuntime selectedRoute, out float selectedCost), Is.True);
        Assert.That(selectedRoute.RuntimeId, Is.EqualTo("route/merchant-test"));
        Assert.That(selectedCost, Is.EqualTo(Math.Max(1, selectedRoute.TravelDays)
            * (definitions.EffectiveConfiguration?.Travel?.TravelCostPerDay ?? 0f)));
        Assert.That(graph.TryPlanSelectedSoloTravel(npc, "outside-current-location",
            destination.LocationRuntimeId, destination.RuntimeId, out _, out _), Is.False);
        Assert.That(graph.TryPlanSelectedSoloTravel(npc, origin.LocationRuntimeId,
            destination.LocationRuntimeId, origin.RuntimeId, out _, out _), Is.False);

        Assert.That(graph.TryInstallMerchantTradePlan(npc, definitions.Items.Values.First(),
            origin.RuntimeId, destination.RuntimeId, 2, 1f, "decision-buy"), Is.True);

        int admissionCalls = 0;
        long priorRevision = npc.TravelPlan.Revision;
        Assert.That(graph.TryCommitMerchantTradeTravelPlan(npc, destination.RuntimeId,
            70f, "decision-buy", () => { admissionCalls++; return false; }, out _), Is.False);
        Assert.That(admissionCalls, Is.EqualTo(1));
        Assert.That(npc.TravelPlan.Revision, Is.EqualTo(priorRevision));

        Assert.That(graph.TryCommitMerchantTradeTravelPlan(npc, destination.RuntimeId,
            70f, "decision-buy", () => { admissionCalls++; return true; },
            out float expectedCost), Is.True);
        Assert.That(expectedCost, Is.GreaterThan(0f));
        Assert.That(npc.TravelPlan.Revision, Is.EqualTo(priorRevision + 1));
        Assert.That(npc.TravelPlan.TargetCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(npc.TravelPlan.OriginDecisionId, Is.EqualTo("decision-buy"));

        Assert.That(graph.TryCommitMerchantTradeTravelPlan(npc, destination.RuntimeId,
            70f, "decision-buy", () => { admissionCalls++; return true; }, out _), Is.True);
        Assert.That(admissionCalls, Is.EqualTo(2), "An exact no-op skips P12 admission.");
        Assert.That(npc.TravelPlan.Revision, Is.EqualTo(priorRevision + 1));

        DailyV1ActionDefinition travelAction = definitions.Actions.Values.Single(action =>
            action.Values.actionType?.Value == DailyV1NpcDefinitionRules.TravelActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, travelAction,
            null, destination.RuntimeId, null, 0, 0f, 1f, NpcTravelReason.Trade,
            0f, "decision/selected-travel", null, null, null,
            out DailyV1NpcActionPayloadOwner travelPayload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(npc, travelAction, travelPayload, null), Is.True);
        float balanceBeforeTravel = npc.MoneyAccount.Balance;
        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSoloTravelSlot(
            graph, npc, destination.RuntimeId, null, _ => false,
            out DailyV1SelectedActionExecutionStage.SoloTravelResult rejectedTravel), Is.True);
        Assert.That(rejectedTravel.Succeeded, Is.False);
        Assert.That(rejectedTravel.Execution.Failure,
            Is.EqualTo(DailyV1SelectedSoloTravelFailure.TransitionRejected));
        Assert.That(rejectedTravel.Execution.RefundSucceeded, Is.True);
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(balanceBeforeTravel).Within(0.001f));
        Assert.That(npc.Travel.IsTraveling, Is.False);

        TravelPartyStore partyStore = new TravelPartyStore();
        Assert.That(graph.IsManagedByTravelParty(npc, partyStore), Is.False,
            "Neither owner nor store membership must leave solo travel eligible.");
        TravelPartyRuntime party = new TravelPartyRuntime("party/travel-guard",
            origin.LocationRuntimeId, destination.LocationRuntimeId, selectedRoute.RuntimeId,
            new[] { npc.RuntimeId }, Array.Empty<string>(), selectedRoute.TravelDays,
            "decision/party", new[] { new TravelPartyMemberCost(npc.RuntimeId, 0f) });
        Assert.That(partyStore.Add(party), Is.True);
        Assert.That(npc.Travel.ActiveTravelPartyId, Is.Null,
            "This fixture exercises authoritative store membership without an owner ID projection.");
        Assert.That(graph.IsManagedByTravelParty(npc, partyStore), Is.True,
            "Store-only membership must fail closed.");
        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSoloTravelSlot(
            graph, npc, destination.RuntimeId, partyStore, transition => transition.TryCommit(),
            out DailyV1SelectedActionExecutionStage.SoloTravelResult partyRejectedTravel), Is.False);
        Assert.That(partyRejectedTravel, Is.Null);
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(balanceBeforeTravel).Within(0.001f));
        Assert.That(npc.Travel.TryInstallState(
            npc.Travel.CurrentLocationRuntimeId, npc.Travel.CurrentCityRuntimeId,
            npc.Travel.DestinationLocationRuntimeId, npc.Travel.DestinationCityRuntimeId,
            npc.Travel.TravelDaysRemaining, npc.Travel.TravelDaysTotal,
            npc.Travel.TravelRouteRuntimeId, npc.Travel.TravelStartedToday,
            npc.Travel.TravelOriginDecisionId, party.TravelPartyId,
            npc.Travel.Revision), Is.True);
        Assert.That(graph.IsManagedByTravelParty(npc, partyStore), Is.True,
            "Matching owner and store membership must be excluded from solo travel.");
        Assert.That(partyStore.Complete(party.TravelPartyId), Is.True);
        TravelPartyRuntime mismatchedParty = new TravelPartyRuntime("party/travel-guard-mismatch",
            origin.LocationRuntimeId, destination.LocationRuntimeId, selectedRoute.RuntimeId,
            new[] { npc.RuntimeId }, Array.Empty<string>(), selectedRoute.TravelDays,
            "decision/party-mismatch", new[] { new TravelPartyMemberCost(npc.RuntimeId, 0f) });
        Assert.That(partyStore.Add(mismatchedParty), Is.True);
        Assert.That(graph.IsManagedByTravelParty(npc, partyStore), Is.True,
            "Different owner and store party IDs must fail closed.");
        Assert.That(partyStore.Complete(mismatchedParty.TravelPartyId), Is.True);
        Assert.That(graph.IsManagedByTravelParty(npc, partyStore), Is.True,
            "Owner-only membership after store removal must fail closed.");
        Assert.That(npc.Travel.TryInstallState(
            npc.Travel.CurrentLocationRuntimeId, npc.Travel.CurrentCityRuntimeId,
            npc.Travel.DestinationLocationRuntimeId, npc.Travel.DestinationCityRuntimeId,
            npc.Travel.TravelDaysRemaining, npc.Travel.TravelDaysTotal,
            npc.Travel.TravelRouteRuntimeId, npc.Travel.TravelStartedToday,
            npc.Travel.TravelOriginDecisionId, null, npc.Travel.Revision), Is.True);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSoloTravelSlot(
            graph, npc, destination.RuntimeId, null, transition => transition.TryCommit(),
            out DailyV1SelectedActionExecutionStage.SoloTravelResult stagedTravel), Is.True);
        Assert.That(stagedTravel.Succeeded, Is.True);
        Assert.That(stagedTravel.Route.RuntimeId, Is.EqualTo("route/merchant-test"));
        Assert.That(stagedTravel.Transition.OriginDecisionId, Is.EqualTo("decision/selected-travel"));
        Assert.That(npc.MoneyAccount.Balance,
            Is.EqualTo(balanceBeforeTravel - stagedTravel.TravelCost).Within(0.001f));
        Assert.That(npc.Travel.TravelRouteRuntimeId, Is.EqualTo(stagedTravel.Route.RuntimeId));
    }

    [Test]
    public void GraphRejectsRuntimeIdentityCollisionsAcrossSelectedOwnerAndSpatialFamilies()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition cityDefinition = definitions.Cities.Values.First();
        DailyV1NpcDefinition npcDefinition = definitions.Npcs.Values.First();

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            "city-runtime", "city-location", definitions, cityDefinition, null,
            out DailyV1CityRuntimeOwners city), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc-runtime", definitions, npcDefinition, 0f,
            out DailyV1NpcRuntimeOwners npc), Is.True);

        SpatialNetworkOwner locationCollision = new SpatialNetworkOwner();
        Assert.That(locationCollision.TryRegisterLocation(
            new SpatialLocationRuntime("city-runtime"), () => true, () => { }, null), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, new[] { city }, Array.Empty<DailyV1NpcRuntimeOwners>(), locationCollision,
            out _), Is.False, "A City identity cannot alias a Location identity.");

        SpatialNetworkOwner routeCollision = new SpatialNetworkOwner();
        SpatialLocationRuntime origin = new SpatialLocationRuntime("route-origin");
        SpatialLocationRuntime destination = new SpatialLocationRuntime("route-destination");
        Assert.That(routeCollision.TryRegisterLocation(origin, () => true, () => { }, null), Is.True);
        Assert.That(routeCollision.TryRegisterLocation(destination, () => true, () => { }, null), Is.True);
        Assert.That(routeCollision.TryRegisterRoute(
            new SpatialRouteRuntime("npc-runtime", origin, destination, 1), () => true), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, Array.Empty<DailyV1CityRuntimeOwners>(), new[] { npc }, routeCollision,
            out _), Is.False, "An NPC identity cannot alias a Route identity.");

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            "same-as-location", "same-as-location", definitions, cityDefinition, null,
            out _), Is.False, "A detached graph still rejects an owner/location alias without a spatial network.");
    }

    [Test]
    public void CurrentActionSlotCommitUsesExactGraphAdmissionBeforeCoreOwnerMutation()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(
            allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1ActionDefinition action = definitions.Actions.Values.First();
        DailyV1EffectiveDefinitions foreignDefinitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ActionDefinition foreignAction = foreignDefinitions.Actions[action.DefinitionId];
        int admissionCalls = 0;

        Assert.That(graph.TryCommitCurrentActionSlot(npc, foreignAction, null, () =>
        {
            admissionCalls++;
            return true;
        }), Is.False, "A foreign action definition is rejected before host admission.");
        Assert.That(admissionCalls, Is.Zero);
        Assert.That(npc.ActionSlot.CurrentAction, Is.Null);
        Assert.That(npc.ActionSlot.Revision, Is.Zero);

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "outside-runtime", definitions, npc.Definition, 0f,
            out DailyV1NpcRuntimeOwners outsideNpc), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(outsideNpc, action, null, () =>
        {
            admissionCalls++;
            return true;
        }), Is.False, "An owner outside this exact graph is rejected before host admission.");
        Assert.That(admissionCalls, Is.Zero);

        Assert.That(graph.TryCommitCurrentActionSlot(npc, action, null, () =>
        {
            admissionCalls++;
            Assert.That(npc.ActionSlot.CurrentAction, Is.Null,
                "Host admission runs while the prior authoritative action remains installed.");
            Assert.That(npc.ActionSlot.Revision, Is.Zero,
                "Host admission observes the pre-commit local revision.");
            return false;
        }), Is.False);
        Assert.That(admissionCalls, Is.EqualTo(1));
        Assert.That(npc.ActionSlot.CurrentAction, Is.Null);
        Assert.That(npc.ActionSlot.Revision, Is.Zero,
            "Rejected admission preserves both authoritative action value and revision.");

        Assert.That(graph.TryCommitCurrentActionSlot(npc, action, null, () =>
        {
            admissionCalls++;
            Assert.That(npc.ActionSlot.CurrentAction, Is.Null);
            Assert.That(npc.ActionSlot.Revision, Is.Zero);
            return true;
        }), Is.True);
        Assert.That(admissionCalls, Is.EqualTo(2));
        Assert.That(npc.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(npc.ActionSlot.Revision, Is.EqualTo(1));
    }

    [Test]
    public void SelectedDailyEconomyPreflightsExactCityOwnersAndPreservesGlobalPassOrder()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(
            allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        long[] originalRevisions = graph.Cities.Select(city => city.Market.Revision).ToArray();
        int[][] originalStock = graph.Cities.Select(city => city.Market.Items.Select(row => row.Amount).ToArray()).ToArray();
        LocalDailyMaterialFlowResult[] originalMaterialFlow = graph.Cities.Select(city => city.LastMaterialFlow).ToArray();
        List<string> events = new List<string>();

        Assert.That(graph.TrySimulateDailyEconomyDay(
            graph.Cities.Reverse(), 1, "calendar-v1",
            (owner, category, _) => events.Add(owner.Definition.DefinitionId + ":" + category)), Is.False);
        Assert.That(events, Is.Empty, "A mismatched City order is rejected before any writes or callbacks.");
        Assert.That(graph.Cities.Select(city => city.Market.Revision), Is.EqualTo(originalRevisions));
        Assert.That(graph.Cities.Select(city => city.Market.Items.Select(row => row.Amount).ToArray()),
            Is.EqualTo(originalStock));
        Assert.That(graph.Cities.Select(city => city.LastMaterialFlow), Is.EqualTo(originalMaterialFlow),
            "A rejected owner sequence cannot publish a partial Core material-flow report.");

        Assert.That(graph.TrySimulateDailyEconomyDay(
            graph.Cities, 1, "calendar-v1",
            (owner, category, _) => events.Add(owner.Definition.DefinitionId + ":" + category)), Is.True);

        string[] productionEvents = events.Where(value => value.EndsWith(":" + SimulationLogCategory.EconomyProduction,
            StringComparison.Ordinal)).ToArray();
        string[] consumptionEvents = events.Where(value => value.EndsWith(":" + SimulationLogCategory.EconomyConsumption,
            StringComparison.Ordinal)).ToArray();
        string[] marketEvents = events.Where(value => value.EndsWith(":" + SimulationLogCategory.Market,
            StringComparison.Ordinal)).ToArray();
        Assert.That(productionEvents, Is.Not.Empty);
        Assert.That(marketEvents, Has.Length.EqualTo(graph.Cities.Count));
        Assert.That(marketEvents.Select(value => value.Split(':')[0]),
            Is.EqualTo(graph.Cities.Select(city => city.Definition.DefinitionId)));

        int firstConsumption = events.FindIndex(value => value.EndsWith(":" + SimulationLogCategory.EconomyConsumption,
            StringComparison.Ordinal));
        int firstMarket = events.FindIndex(value => value.EndsWith(":" + SimulationLogCategory.Market,
            StringComparison.Ordinal));
        int lastProduction = events.FindLastIndex(value => value.EndsWith(":" + SimulationLogCategory.EconomyProduction,
            StringComparison.Ordinal));
        Assert.That(lastProduction, Is.LessThan(firstMarket), "All production remains ahead of the final City pass.");
        if (consumptionEvents.Length > 0)
            Assert.That(firstConsumption, Is.GreaterThan(lastProduction),
                "All City production completes before any City consumes.");
    }

    [Test]
    public void AccountBackedDailyEconomyRunsThroughCoreOwnerGraph()
    {
        DailyV1SimulationConfigData configuration = DailyV1TypedDefinitionGraphTests.CreateConfiguration(
            1f, new[] { "city/paid" });
        DailyV1CityData cityValues = new DailyV1CityData(
            "city/paid", "Paid City", string.Empty, string.Empty, string.Empty,
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.ExogenousDaily, "ExogenousDaily"),
            1000,
            new DailyV1MarketLiquidityConfig(
                new DailyV1EnumValue((int)MarketLiquidityMode.AccountBacked, "AccountBacked"), 0f),
            new DailyV1PopulationConsumptionConfig(
                new DailyV1EnumValue((int)ConsumptionPaymentMode.AccountBacked, "AccountBacked"), 50f),
            new[] { new DailyV1MarketItemConfig("item/paid", 5, 5, 1f) },
            Array.Empty<DailyV1CityProductionConfig>(), Array.Empty<DailyV1CityConnection>());
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            configuration, SimulationConfigurationDefaults.Create(),
            new DailyV1CalendarDefinition(1, 1, 1, null), new[] { cityValues },
            new[] { new DailyV1ItemData("item/paid", "Iron", 4f, null) },
            Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(),
            Array.Empty<DailyV1NpcActionData>(), Array.Empty<DailyV1NpcStatusData>(),
            Array.Empty<string>(), "test.daily-v1", "test.geography-v1",
            "test.geography-stage/v1", "hex-v1", "q-r-s");
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            "city-runtime-paid", "location-runtime-paid", definitions,
            definitions.Cities["city/paid"], null, out DailyV1CityRuntimeOwners city), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, new[] { city }, Array.Empty<DailyV1NpcRuntimeOwners>(), out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        int consumptionLogs = 0;
        Assert.That(graph.TrySimulateDailyEconomyDay(graph.Cities, 1, "calendar-v1",
            (_, category, _) => { if (category == SimulationLogCategory.EconomyConsumption) consumptionLogs++; }), Is.True);

        DailyV1ItemDefinition item = definitions.Items["item/paid"];
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(4));
        Assert.That(city.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(46f));
        Assert.That(city.Counterparty.MoneyAccount.Balance, Is.EqualTo(4f));
        Assert.That(city.PopulationEconomy.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(city.Counterparty.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(city.Market.Revision, Is.EqualTo(1), "The selected consumption commits one Market owner write.");
        Assert.That(consumptionLogs, Is.EqualTo(1));
    }

    [Test]
    public void SelectedDailyEconomyKeepsMaterialFlowResultOnTheCoreOwnerWithoutProjectionCallback()
    {
        DailyV1CityData cityValues = new DailyV1CityData(
            "city/finite", "Finite City", "settlement/finite", "location/finite", "market/finite",
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.FiniteReserveDaily, "FiniteReserveDaily"),
            1000, null,
            new DailyV1PopulationConsumptionConfig(
                new DailyV1EnumValue((int)ConsumptionPaymentMode.Free, "Free"), 0f),
            new[] { new DailyV1MarketItemConfig("item/finite", 5, 100, 0f) },
            new[] { new DailyV1CityProductionConfig("item/finite", 3, 7,
                "source/finite", "content-v1",
                new DailyV1EnumValue((int)CityProductionSourceKind.FiniteReserveDaily, "FiniteReserveDaily")) },
            Array.Empty<DailyV1CityConnection>());
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            DailyV1TypedDefinitionGraphTests.CreateConfiguration(1f, new[] { "city/finite" }),
            SimulationConfigurationDefaults.Create(), new DailyV1CalendarDefinition(1, 1, 1, null),
            new[] { cityValues }, new[] { new DailyV1ItemData("item/finite", "Iron", 4f, null) },
            Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(),
            Array.Empty<DailyV1NpcActionData>(), Array.Empty<DailyV1NpcStatusData>(), Array.Empty<string>(),
            "test.daily-v1", "test.geography-v1", "test.geography-stage/v1", "hex-v1", "q-r-s");
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            "city-runtime-finite", "location-runtime-finite", definitions,
            definitions.Cities["city/finite"], null, out DailyV1CityRuntimeOwners city), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, new[] { city }, Array.Empty<DailyV1NpcRuntimeOwners>(),
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        Assert.That(graph.TrySimulateDailyEconomyDay(graph.Cities, 1, "calendar-v1", null), Is.True);

        Assert.That(city.LastMaterialFlow, Is.Not.Null,
            "The Core stage stores the completed result on its selected owner instead of calling Unity to publish it.");
        Assert.That(city.LastMaterialFlow.AbsoluteDay, Is.EqualTo(1L));
        Assert.That(city.LastMaterialFlow.OpeningStock, Is.EqualTo(5));
        Assert.That(city.LastMaterialFlow.AppliedSourceQuantity, Is.EqualTo(3));
        Assert.That(city.LastMaterialFlow.ClosingStock, Is.EqualTo(8));
        Assert.That(city.FiniteSource.RemainingReserve, Is.EqualTo(4));
    }

    [Test]
    public void AdmittedDefinitionsConstructAndOperatePairedCityNpcOwnersInCore()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(
            allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.Definitions, Is.SameAs(definitions));
        Assert.That(graph.Cities.Count, Is.EqualTo(2));
        Assert.That(graph.Npcs.Count, Is.EqualTo(10));

        DailyV1NpcRuntimeOwners npc = graph.Npcs.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "JORGE", StringComparison.Ordinal));
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "CAMPO_VERDE", StringComparison.Ordinal));
        DailyV1CityRuntimeOwners destination = graph.Cities.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "SERRA_DE_FERRO", StringComparison.Ordinal));

        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(origin.RuntimeId));
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.EqualTo(origin.LocationRuntimeId));
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.True);
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(npc.Definitions, Is.SameAs(graph.Definitions));
        Assert.That(origin.Definitions, Is.SameAs(graph.Definitions));
        Assert.That(destination.Definitions, Is.SameAs(graph.Definitions));

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc-outside-graph", definitions, npc.Definition, 0f,
            out DailyV1NpcRuntimeOwners outsideNpc), Is.True);
        Assert.That(outsideNpc.Travel.TryInstallState(
            origin.LocationRuntimeId, origin.RuntimeId,
            null, null, 0, 0, null, false, null, null, 0L), Is.True);
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            outsideNpc, origin, origin.LocationRuntimeId,
            destination.LocationRuntimeId, destination,
            requestedTravelDays: 2, originDecisionId: "unregistered", routeId: "route-core-host",
            out DailyV1SelectedTravelTransitionPlan outsidePlan), Is.True);
        int rejectedAdmissionCalls = 0;
        Assert.That(graph.TryCommitTravelTransition(outsideNpc, outsidePlan, () =>
        {
            rejectedAdmissionCalls++;
            return true;
        }), Is.False);
        Assert.That(rejectedAdmissionCalls, Is.Zero,
            "A prepared plan for an NPC outside this runtime is rejected before host admission.");
        Assert.That(outsideNpc.Travel.CurrentCityRuntimeId, Is.EqualTo(origin.RuntimeId));
        Assert.That(outsideNpc.Travel.DestinationCityRuntimeId, Is.Null);
        Assert.That(outsideNpc.Travel.Revision, Is.Zero);

        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            npc, origin, origin.LocationRuntimeId, destination.LocationRuntimeId, destination,
            requestedTravelDays: 2, originDecisionId: "decision-core-host", routeId: "route-core-host",
            out DailyV1SelectedTravelTransitionPlan plan), Is.True);

        int admissionCalls = 0;
        Assert.That(graph.TryCommitTravelTransition(npc, plan, () =>
        {
            admissionCalls++;
            Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.True,
                "Admission runs before the paired City and NPC owner write.");
            Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(origin.RuntimeId));
            return true;
        }), Is.True);

        Assert.That(admissionCalls, Is.EqualTo(1));
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.False,
            "Travel start removes current presence without prematurely adding destination presence.");
        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.Null);
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.Null);
        Assert.That(npc.Travel.DestinationCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(npc.Travel.DestinationLocationRuntimeId, Is.EqualTo(destination.LocationRuntimeId));
        Assert.That(npc.Travel.TravelDaysRemaining, Is.EqualTo(2));
        Assert.That(npc.Travel.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void SelectedSoloTravelDayUsesExactSortedOwnersAndPreservesAdmissionAndPartyRules()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(
            allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        List<DailyV1NpcRuntimeOwners> selected = graph.Npcs
            .OrderBy(owner => owner.RuntimeId, StringComparer.Ordinal)
            .Take(4)
            .ToList();
        DailyV1NpcRuntimeOwners startedToday = selected[0];
        DailyV1NpcRuntimeOwners rejectedAdvance = selected[1];
        DailyV1NpcRuntimeOwners arriving = selected[2];
        DailyV1NpcRuntimeOwners partyMember = selected[3];
        foreach (DailyV1NpcRuntimeOwners npc in selected)
        {
            DailyV1CityRuntimeOwners origin = graph.Cities.Single(city =>
                string.Equals(city.RuntimeId, npc.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
            DailyV1CityRuntimeOwners destination = graph.Cities.Single(city => !ReferenceEquals(city, origin));
            Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
                npc, origin, origin.LocationRuntimeId,
                destination.LocationRuntimeId, destination,
                requestedTravelDays: 3,
                originDecisionId: "decision-" + npc.RuntimeId,
                routeId: "route-" + npc.RuntimeId,
                out DailyV1SelectedTravelTransitionPlan startPlan), Is.True);
            Assert.That(graph.TryCommitTravelTransition(npc, startPlan, () => true), Is.True);
        }

        DailyV1CityRuntimeOwners arrivingDestination = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, arriving.Travel.DestinationCityRuntimeId, StringComparison.Ordinal));
        Assert.That(startedToday.Travel.TravelStartedToday, Is.True);
        Assert.That(startedToday.Travel.TryInstallState(
            null, null, startedToday.Travel.DestinationLocationRuntimeId,
            startedToday.Travel.DestinationCityRuntimeId, 3, 3,
            startedToday.Travel.TravelRouteRuntimeId, true,
            startedToday.Travel.TravelOriginDecisionId, null, startedToday.Travel.Revision), Is.True);
        Assert.That(rejectedAdvance.Travel.TryInstallState(
            null, null, rejectedAdvance.Travel.DestinationLocationRuntimeId,
            rejectedAdvance.Travel.DestinationCityRuntimeId, 2, 3,
            rejectedAdvance.Travel.TravelRouteRuntimeId, false,
            rejectedAdvance.Travel.TravelOriginDecisionId, null, rejectedAdvance.Travel.Revision), Is.True);
        Assert.That(arriving.Travel.TryInstallState(
            null, null, arriving.Travel.DestinationLocationRuntimeId,
            arriving.Travel.DestinationCityRuntimeId, 1, 3,
            arriving.Travel.TravelRouteRuntimeId, false,
            arriving.Travel.TravelOriginDecisionId, null, arriving.Travel.Revision), Is.True);
        Assert.That(partyMember.Travel.TryInstallState(
            null, null, partyMember.Travel.DestinationLocationRuntimeId,
            partyMember.Travel.DestinationCityRuntimeId, 2, 3,
            partyMember.Travel.TravelRouteRuntimeId, false,
            partyMember.Travel.TravelOriginDecisionId, null, partyMember.Travel.Revision), Is.True);

        long[] revisionsBeforeRejectedRoster = graph.Npcs.Select(npc => npc.Travel.Revision).ToArray();
        int preflightCommitCalls = 0;
        Assert.That(graph.TryAdvanceSoloTravelDay(
            graph.Npcs.Reverse(),
            Array.Empty<string>(),
            (_, _, _) => { preflightCommitCalls++; return true; },
            (_, _, _) => Assert.Fail("An inexact roster must not commit travel."),
            _ => Assert.Fail("An inexact roster must not publish travel results."),
            out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> rejectedRosterResults), Is.False);
        Assert.That(preflightCommitCalls, Is.Zero);
        Assert.That(rejectedRosterResults, Is.Null);
        Assert.That(graph.Npcs.Select(npc => npc.Travel.Revision), Is.EqualTo(revisionsBeforeRejectedRoster));

        List<string> transitionOrder = new List<string>();
        Assert.That(graph.TryAdvanceSoloTravelDay(
            graph.Npcs,
            new[] { partyMember.RuntimeId },
            (npc, _, _) =>
            {
                transitionOrder.Add("pre:" + npc.RuntimeId);
                return !ReferenceEquals(npc, rejectedAdvance);
            },
            (npc, plan, _) =>
            {
                Assert.That(plan.IsCommitted, Is.True);
                transitionOrder.Add("publish:" + npc.RuntimeId);
            },
            result =>
            {
                if (result.Arrived)
                {
                    Assert.That(result.Npc.SpatialKnowledge.KnowsLocation(
                        result.Npc.Travel.CurrentLocationRuntimeId), Is.True,
                        "The committed SpatialKnowledge owner is current before the adapter result is published.");
                    transitionOrder.Add("knowledge-current:" + result.Npc.RuntimeId);
                }
                transitionOrder.Add("result:" + result.Npc.RuntimeId);
            },
            out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> travelResults), Is.True);

        foreach (DailyV1NpcRuntimeOwners committedOwner in new[] { startedToday, arriving })
        {
            int publishIndex = transitionOrder.IndexOf("publish:" + committedOwner.RuntimeId);
            Assert.That(publishIndex, Is.GreaterThanOrEqualTo(0));
            int nextPreflightIndex = transitionOrder
                .Select((value, index) => new { value, index })
                .Where(entry => entry.index > publishIndex && entry.value.StartsWith("pre:", StringComparison.Ordinal))
                .Select(entry => entry.index)
                .DefaultIfEmpty(int.MaxValue)
                .First();
            Assert.That(publishIndex, Is.LessThan(nextPreflightIndex),
                "Each committed owner must publish its projection/P12 boundary before Core evaluates the next owner.");
            int resultIndex = transitionOrder.IndexOf("result:" + committedOwner.RuntimeId);
            Assert.That(resultIndex, Is.GreaterThan(publishIndex),
                "Each owner result must be consumed after commit publication.");
            Assert.That(resultIndex, Is.LessThan(nextPreflightIndex),
                "Per-NPC event/report adapters must retain deterministic owner order after Core commits arrival knowledge.");
            if (ReferenceEquals(committedOwner, arriving))
            {
                int knowledgeIndex = transitionOrder.IndexOf("knowledge-current:" + committedOwner.RuntimeId);
                Assert.That(knowledgeIndex, Is.GreaterThan(publishIndex),
                    "Arrival discovery follows the travel projection/P12 notification.");
                Assert.That(knowledgeIndex, Is.LessThan(resultIndex),
                    "Arrival discovery is committed before its event/report adapter runs.");
            }
        }

        Assert.That(arriving.SpatialKnowledge.KnowsLocation(
            arriving.Travel.CurrentLocationRuntimeId), Is.True,
            "The Core travel stage applies arrival discovery to the exact selected SpatialKnowledge owner before publishing its report.");
        Assert.That(arriving.SpatialKnowledge.Revision, Is.EqualTo(1L));
        Assert.That(startedToday.SpatialKnowledge.Revision, Is.Zero,
            "Clearing the start-day marker is not an arrival and must not create discovery state.");
        Assert.That(rejectedAdvance.SpatialKnowledge.Revision, Is.Zero,
            "A rejected travel advance must not create arrival discovery state.");

        Assert.That(startedToday.Travel.TravelDaysRemaining, Is.EqualTo(3),
            "Travel started today is cleared without consuming a travel day.");
        Assert.That(startedToday.Travel.TravelStartedToday, Is.False);
        Assert.That(startedToday.Travel.Revision, Is.EqualTo(2L));
        Assert.That(rejectedAdvance.Travel.TravelDaysRemaining, Is.EqualTo(2),
            "Rejected host admission leaves authoritative travel state unchanged.");
        Assert.That(rejectedAdvance.Travel.Revision, Is.EqualTo(1L));
        Assert.That(arriving.Travel.CurrentCityRuntimeId, Is.EqualTo(arrivingDestination.RuntimeId));
        Assert.That(arriving.Travel.CurrentLocationRuntimeId, Is.EqualTo(arrivingDestination.LocationRuntimeId));
        Assert.That(arriving.Travel.DestinationCityRuntimeId, Is.Null);
        Assert.That(arrivingDestination.NpcPresence.Contains(arriving.RuntimeId), Is.True);
        Assert.That(arriving.Travel.Revision, Is.EqualTo(2L));
        Assert.That(partyMember.Travel.TravelDaysRemaining, Is.EqualTo(2),
            "TravelParty membership keeps the owner out of solo advancement.");
        Assert.That(partyMember.Travel.Revision, Is.EqualTo(1L));

        Assert.That(travelResults.Where(result => result.ShouldReport).Select(result =>
            result.Npc.RuntimeId + ":" + result.Arrived + ":" + result.OriginDecisionId), Is.EqualTo(new[]
        {
            rejectedAdvance.RuntimeId + ":False:" + rejectedAdvance.Travel.TravelOriginDecisionId,
            arriving.RuntimeId + ":True:" + "decision-" + arriving.RuntimeId
        }.OrderBy(value => value.Split(':')[0], StringComparer.Ordinal)));
        Assert.That(travelResults.Single(result => ReferenceEquals(result.Npc, startedToday)).WasCommitted, Is.True);
        Assert.That(travelResults.Any(result => ReferenceEquals(result.Npc, partyMember)), Is.False);
    }

    [Test]
    public void SelectedTravelPartyDayRunsCoreOwnerTransitionsInPartyMemberAndP12Order()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(
            allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        DailyV1NpcRuntimeOwners member = graph.Npcs[0];
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, member.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        DailyV1CityRuntimeOwners destination = graph.Cities.Single(city => !ReferenceEquals(city, origin));
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            member, origin, origin.LocationRuntimeId, destination.LocationRuntimeId, destination,
            requestedTravelDays: 2, originDecisionId: "party-day-decision",
            routeId: "party-day-route", out DailyV1SelectedTravelTransitionPlan startPlan), Is.True);
        Assert.That(graph.TryCommitTravelTransition(member, startPlan, () => true), Is.True);

        TravelPartyRuntime party = new TravelPartyRuntime(
            "party-day-1", origin.LocationRuntimeId, destination.LocationRuntimeId,
            "party-day-route", new[] { member.RuntimeId }, Array.Empty<string>(), 2,
            "party-day-decision", new[] { new TravelPartyMemberCost(member.RuntimeId, 1f) });
        TravelPartyStore partyStore = new TravelPartyStore();
        Assert.That(partyStore.Add(party), Is.True);
        Assert.That(DailyV1SelectedTravelTransitionPlan.TrySetActiveTravelParty(
            member, party.TravelPartyId, out DailyV1SelectedTravelTransitionPlan setPartyPlan), Is.True);
        Assert.That(graph.TryCommitTravelTransition(member, setPartyPlan, () => true), Is.True);

        List<string> ownerEvents = new List<string>();
        IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> completed = null;
        Assert.That(graph.TryAdvanceTravelParties(
            partyStore,
            (owner, plan, arriving) =>
            {
                ownerEvents.Add("admit:" + owner.RuntimeId + ":" + arriving);
                return true;
            },
            (owner, plan, arriving) =>
            {
                Assert.That(plan.IsCommitted, Is.True);
                ownerEvents.Add("publish:" + owner.RuntimeId + ":" + arriving);
            },
            _ =>
            {
                Assert.Fail("The travel start-day flag must be cleared without arrival.");
                return false;
            },
            out completed), Is.True);
        Assert.That(completed, Is.Empty);
        Assert.That(member.Travel.TravelStartedToday, Is.False);
        Assert.That(member.Travel.TravelDaysRemaining, Is.EqualTo(2));
        Assert.That(partyStore.ActiveParties, Is.EqualTo(new[] { party }));

        Assert.That(member.Travel.TryInstallState(
            null, null, destination.LocationRuntimeId, destination.RuntimeId, 1, 2,
            "party-day-route", false, "party-day-decision", party.TravelPartyId,
            member.Travel.Revision), Is.True);
        long storeRevisionBeforeArrival = partyStore.Revision;
        bool eventObservedBeforePartyClear = false;
        ownerEvents.Clear();

        Assert.That(graph.TryAdvanceTravelParties(
            partyStore,
            (owner, plan, arriving) =>
            {
                ownerEvents.Add("admit:" + owner.RuntimeId + ":" + arriving);
                return true;
            },
            (owner, plan, arriving) =>
            {
                Assert.That(plan.IsCommitted, Is.True);
                ownerEvents.Add("publish:" + owner.RuntimeId + ":" + arriving);
            },
            currentParty =>
            {
                Assert.That(currentParty, Is.SameAs(party));
                Assert.That(partyStore.ActiveParties, Does.Contain(party),
                    "The established arrival event is published before the party owner completes.");
                Assert.That(member.Travel.ActiveTravelPartyId, Is.EqualTo(party.TravelPartyId),
                    "The established arrival event precedes member party-identity clearing.");
                Assert.That(destination.NpcPresence.Contains(member.RuntimeId), Is.True);
                Assert.That(member.SpatialKnowledge.KnowsLocation(destination.LocationRuntimeId), Is.True,
                    "Arrival discovery is committed to the exact selected Core owner before event publication.");
                ownerEvents.Add("event:" + currentParty.TravelPartyId);
                eventObservedBeforePartyClear = true;
                return true;
            },
            out completed), Is.True);

        Assert.That(eventObservedBeforePartyClear, Is.True);
        Assert.That(completed, Has.Count.EqualTo(1));
        Assert.That(completed[0].Party, Is.SameAs(party));
        Assert.That(completed[0].Members, Is.EqualTo(new[] { member }));
        Assert.That(completed[0].EventRecorded, Is.True);
        Assert.That(partyStore.ActiveParties, Is.Empty);
        Assert.That(partyStore.Revision, Is.EqualTo(storeRevisionBeforeArrival + 1));
        Assert.That(member.Travel.ActiveTravelPartyId, Is.Null);
        Assert.That(member.Travel.CurrentCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(member.Travel.Revision, Is.EqualTo(5L));
        Assert.That(member.SpatialKnowledge.Revision, Is.EqualTo(1L));
        Assert.That(destination.NpcPresence.Contains(member.RuntimeId), Is.True);
        Assert.That(ownerEvents, Is.EqualTo(new[]
        {
            "admit:" + member.RuntimeId + ":True",
            "publish:" + member.RuntimeId + ":True",
            "event:party-day-1",
            "admit:" + member.RuntimeId + ":False",
            "publish:" + member.RuntimeId + ":False"
        }));
    }

    [Test]
    public void SelectedTravelDayStageRunsPartyOperationBeforeSoloTravel()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(
            allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.Npcs.Count, Is.GreaterThanOrEqualTo(2));

        DailyV1NpcRuntimeOwners partyMember = graph.Npcs[0];
        DailyV1CityRuntimeOwners partyOrigin = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, partyMember.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        DailyV1CityRuntimeOwners partyDestination = graph.Cities.Single(city =>
            !ReferenceEquals(city, partyOrigin));
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            partyMember, partyOrigin, partyOrigin.LocationRuntimeId, partyDestination.LocationRuntimeId,
            partyDestination, 1, "ordered-party", "ordered-party-route",
            out DailyV1SelectedTravelTransitionPlan partyStart), Is.True);
        Assert.That(graph.TryCommitTravelTransition(partyMember, partyStart, null), Is.True);

        TravelPartyRuntime party = new TravelPartyRuntime(
            "ordered-party-1", partyOrigin.LocationRuntimeId, partyDestination.LocationRuntimeId,
            "ordered-party-route", new[] { partyMember.RuntimeId }, Array.Empty<string>(), 1,
            "ordered-party", new[] { new TravelPartyMemberCost(partyMember.RuntimeId, 1f) });
        TravelPartyStore store = new TravelPartyStore();
        Assert.That(store.Add(party), Is.True);
        Assert.That(DailyV1SelectedTravelTransitionPlan.TrySetActiveTravelParty(
            partyMember, party.TravelPartyId, out DailyV1SelectedTravelTransitionPlan setParty), Is.True);
        Assert.That(graph.TryCommitTravelTransition(partyMember, setParty, null), Is.True);
        Assert.That(partyMember.Travel.TravelStartedToday, Is.True);
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryClearTravelStartedToday(
            partyMember, out DailyV1SelectedTravelTransitionPlan clearPartyStartDay), Is.True);
        Assert.That(graph.TryCommitTravelTransition(partyMember, clearPartyStartDay, null), Is.True);

        DailyV1NpcRuntimeOwners solo = graph.Npcs[1];
        DailyV1CityRuntimeOwners soloOrigin = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, solo.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        DailyV1CityRuntimeOwners soloDestination = graph.Cities.Single(city =>
            !ReferenceEquals(city, soloOrigin));
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            solo, soloOrigin, soloOrigin.LocationRuntimeId, soloDestination.LocationRuntimeId,
            soloDestination, 2, "ordered-solo", "ordered-solo-route",
            out DailyV1SelectedTravelTransitionPlan soloStart), Is.True);
        Assert.That(graph.TryCommitTravelTransition(solo, soloStart, null), Is.True);
        Assert.That(solo.Travel.TryInstallState(
            null, null, soloDestination.LocationRuntimeId, soloDestination.RuntimeId, 2, 2,
            "ordered-solo-route", false, "ordered-solo", null, solo.Travel.Revision), Is.True);

        List<string> order = new List<string>();
        RecordingTravelStageBoundary boundary = new RecordingTravelStageBoundary(order);
        Assert.That(DailyV1SelectedTravelDayStage.TryAdvance(
            graph,
            store,
            new[] { partyMember.RuntimeId },
            boundary,
            operation =>
            {
                order.Add("party-operation-enter");
                bool committed = operation();
                order.Add("party-operation-exit");
                return committed;
            },
            advanceSoloTravel: true,
            out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
            out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults), Is.True);

        Assert.That(partyArrivals, Has.Count.EqualTo(1));
        Assert.That(soloResults, Has.Count.EqualTo(1));
        Assert.That(soloResults[0].Npc, Is.SameAs(solo));
        Assert.That(order.IndexOf("party-operation-enter"), Is.LessThan(order.IndexOf("party-event")));
        Assert.That(order.IndexOf("party-event"), Is.LessThan(order.IndexOf("party-operation-exit")));
        Assert.That(order.IndexOf("party-operation-exit"), Is.LessThan(order.IndexOf("solo-result")));
    }

    private sealed class RecordingTravelStageBoundary : IDailyV1SelectedTravelDayBoundary
    {
        private readonly List<string> order;

        internal RecordingTravelStageBoundary(List<string> order) => this.order = order;

        public bool TryPrepareTransition(
            DailyV1NpcRuntimeOwners npc,
            DailyV1SelectedTravelTransitionPlan plan,
            bool arriving,
            out object projectionReceipt)
        {
            order.Add("prepare:" + npc.RuntimeId + ":" + arriving);
            projectionReceipt = plan;
            return true;
        }

        public void PublishCommittedTransition(
            DailyV1NpcRuntimeOwners npc,
            DailyV1SelectedTravelTransitionPlan plan,
            bool arriving,
            object projectionReceipt)
        {
            Assert.That(plan, Is.SameAs(projectionReceipt));
            Assert.That(plan.IsCommitted, Is.True);
            order.Add("publish:" + npc.RuntimeId + ":" + arriving);
        }

        public bool PublishTravelPartyArrival(TravelPartyRuntime party)
        {
            order.Add("party-event");
            return true;
        }

        public void PublishSoloTravelResult(DailyV1SelectedSoloTravelDayResult result)
        {
            order.Add("solo-result");
        }
    }
}
