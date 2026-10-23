using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedRuntimeOwnerGraphTests
{
    [Test]
    public void ConfiguredSelectedOwnersCanBeConstructedWithoutUnityProjectionCallbacks()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        Assert.That(DailyV1SelectedRuntimeOwnerGraphBuilder.TryBuildConfigured(
            definitions, allocator, out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.Definitions, Is.SameAs(definitions));
        Assert.That(graph.Cities, Is.Not.Empty);
        Assert.That(graph.Npcs, Is.Not.Empty);
        Assert.That(graph.JusticeRecordsOwner, Is.Not.Null);
        Assert.That(graph.JusticeRecordsOwner.WantedRecords, Is.Empty);
        Assert.That(definitions.Configuration.explorableSites, Is.Empty,
            "The selected Daily-v1 owner composition has no P10 site/topology input.");
        Assert.That(graph.Npcs.Any(npc => npc.CommercialKnowledge.Observations.Count > 0), Is.True,
            "Core configured composition seeds admitted day-zero commercial rows for traveling Merchants.");
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            Assert.That(graph.SpatialNetwork.ContainsLocationId(city.LocationRuntimeId), Is.True);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            Assert.That(npc.Definitions, Is.SameAs(definitions));
            Assert.That(npc.CommercialKnowledge.Definitions, Is.SameAs(definitions));
            Assert.That(npc.SpatialKnowledge.OwnerRuntimeId, Is.EqualTo(npc.RuntimeId));
            Assert.That(npc.SpatialKnowledge.KnownLocationRuntimeIds,
                Does.Contain(npc.Travel.CurrentLocationRuntimeId));
        }
    }

    [Test]
    public void ConfiguredGenesisComposesDirectiveJusticeSpatialAndCommercialOwnersInCore()
    {
        DailyV1EffectiveDefinitions baseline = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions definitions = WithScheduledDirectives(baseline, new[]
        {
            new DailyV1ScheduledDirectiveConfig(4,
                new DailyV1EnumValue(0, "RequestAction"),
                new DailyV1EnumValue(0, "EscapePrison"),
                "JOBSON", "FUGIR_DA_PRISAO")
        });
        definitions = WithInitialWarrants(definitions, new[]
        {
            new DailyV1InitialWantedRecordConfig("JOBSON", "CAMPO_VERDE", 9f, 2)
        });

        Assert.That(DailyV1SelectedRuntimeOwnerGraphBuilder.TryBuildConfigured(
            definitions, new RuntimeIdAllocator(), out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        Assert.That(graph.ScheduledDirectives, Is.Not.Null);
        ScheduledDirective directive = graph.ScheduledDirectives.Directives.Single();
        DailyV1NpcRuntimeOwners actor = graph.Npcs.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "JOBSON", StringComparison.Ordinal));
        Assert.That(directive.DirectiveId, Is.EqualTo("directive-000001"));
        Assert.That(directive.ActorRuntimeId, Is.EqualTo(actor.RuntimeId));
        Assert.That(directive.ActionDefinitionId,
            Is.EqualTo(definitions.Actions["FUGIR_DA_PRISAO"].DefinitionId));
        Assert.That(directive.AbsoluteDay, Is.EqualTo(4));
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(graph.JusticeRecordsOwner.WantedRecords, Has.Count.EqualTo(1));
        DailyV1CityRuntimeOwners city = graph.Cities.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "CAMPO_VERDE", StringComparison.Ordinal));
        DailyV1JusticeWantedRecordOwner wanted = graph.JusticeRecordsOwner.WantedRecords[0];
        Assert.That(wanted.TargetRuntimeId, Is.EqualTo(actor.RuntimeId));
        Assert.That(wanted.CityRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(wanted.Bounty, Is.EqualTo(9f));
        Assert.That(wanted.SentenceDays, Is.EqualTo(2));
        Assert.That(graph.JusticeRecordsOwner.Revision, Is.Zero,
            "Configured genesis remains outside post-admission P12 mutation accounting.");
        Assert.That(actor.Status.Statuses, Does.Contain(definitions.Statuses[
            definitions.Configuration.wantedStatus]));
        Assert.That(actor.SpatialKnowledge.KnowsLocation(city.LocationRuntimeId), Is.True);
        Assert.That(graph.Npcs.Any(owner => owner.CommercialKnowledge.Observations.Count > 0), Is.True);
        Assert.That(definitions.Configuration.explorableSites, Is.Empty,
            "This composition includes no P10 Ruin or LocalTopology rows.");
    }

    [Test]
    public void ConfiguredCityRoutesAreConstructedByCoreAgainstExactOwnersAndBindHostProjection()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);

        long identityRevisionBeforeRoutes = builder.RuntimeIdentityIndex.CensusRevision;
        long spatialRevisionBeforeRoutes = GetSpatialRevision(builder.SpatialNetwork);
        Dictionary<string, SpatialRouteRuntime> routeProjections =
            new Dictionary<string, SpatialRouteRuntime>(StringComparer.Ordinal);
        List<string> expectedRoutePairs = new List<string>();
        foreach (DailyV1CityRuntimeOwners origin in builder.Cities)
        {
            List<DailyV1CityConnection> connections = new List<DailyV1CityConnection>(
                origin.Definition.Values.connections ?? Array.Empty<DailyV1CityConnection>());
            connections.Sort((left, right) =>
            {
                int destination = StringComparer.Ordinal.Compare(left?.destination, right?.destination);
                return destination != 0
                    ? destination
                    : Nullable.Compare(left != null ? (int?)left.travelDays : null,
                        right != null ? (int?)right.travelDays : null);
            });
            foreach (DailyV1CityConnection connection in connections)
            {
                if (connection == null || string.IsNullOrWhiteSpace(connection.destination)) continue;
                List<DailyV1CityRuntimeOwners> destinations = builder.Cities
                    .Where(city => string.Equals(city.Definition.DefinitionId,
                        connection.destination, StringComparison.Ordinal)).ToList();
                if (destinations.Count != 1 || ReferenceEquals(origin, destinations[0])) continue;
                expectedRoutePairs.Add(origin.LocationRuntimeId + "->" + destinations[0].LocationRuntimeId + ":" +
                    Math.Max(1, connection.travelDays).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        Assert.That(builder.TryCreateConfiguredCityRoutes(allocator, route =>
        {
            Assert.That(builder.RuntimeIdentityIndex.HasIdentity(
                route.RuntimeId, DailyV1RuntimeIdentityKind.Route), Is.True,
                "Core stages the exact Route identity before the Unity projection binds it.");
            return routeProjections.TryAdd(route.RuntimeId, route);
        }, route =>
        {
            Assert.That(routeProjections.TryGetValue(route.RuntimeId,
                out SpatialRouteRuntime current), Is.True);
            Assert.That(current, Is.SameAs(route));
            Assert.That(routeProjections.Remove(route.RuntimeId), Is.True);
        }), Is.True);

        IReadOnlyList<SpatialRouteRuntime> routes = builder.SpatialNetwork.Routes;
        Assert.That(routes.Select(route => route.RuntimeId),
            Is.EqualTo(new[] { "route-000001", "route-000002" }));
        Assert.That(routes.Select(route => route.Origin.RuntimeId + "->" +
            route.Destination.RuntimeId + ":" + route.TravelDays), Is.EqualTo(expectedRoutePairs));
        Assert.That(routeProjections.Count, Is.EqualTo(routes.Count));
        Assert.That(builder.RuntimeIdentityIndex.CensusRevision,
            Is.EqualTo(identityRevisionBeforeRoutes + routes.Count));
        Assert.That(GetSpatialRevision(builder.SpatialNetwork),
            Is.EqualTo(spatialRevisionBeforeRoutes + routes.Count));
        foreach (SpatialRouteRuntime route in routes)
        {
            Assert.That(routeProjections[route.RuntimeId], Is.SameAs(route));
            Assert.That(builder.RuntimeIdentityIndex.HasIdentity(
                route.RuntimeId, DailyV1RuntimeIdentityKind.Route), Is.True);
        }

        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.SpatialNetwork.Routes, Is.EqualTo(routes));
        Assert.That(graph.RuntimeIdentityIndex, Is.SameAs(builder.RuntimeIdentityIndex));
    }

    [Test]
    public void RejectedCityRouteProjectionDoesNotPublishCoreIdentityOrSpatialMutation()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        long identityRevisionBeforeRoutes = builder.RuntimeIdentityIndex.CensusRevision;
        long spatialRevisionBeforeRoutes = GetSpatialRevision(builder.SpatialNetwork);
        int rollbackCalls = 0;

        Assert.That(builder.TryCreateConfiguredCityRoutes(allocator, _ => false, _ => rollbackCalls++), Is.True);

        Assert.That(builder.SpatialNetwork.Routes, Is.Empty);
        Assert.That(builder.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Route), Is.Zero);
        Assert.That(builder.RuntimeIdentityIndex.CensusRevision, Is.EqualTo(identityRevisionBeforeRoutes));
        Assert.That(GetSpatialRevision(builder.SpatialNetwork), Is.EqualTo(spatialRevisionBeforeRoutes));
        Assert.That(rollbackCalls, Is.Zero,
            "A host projection that rejects binding has not published an entry to roll back.");
    }

    [Test]
    public void ConfiguredScheduledDirectivesUseCoreActorAndAdmittedActionIdentities()
    {
        DailyV1EffectiveDefinitions baseline = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions definitions = WithScheduledDirectives(baseline, new[]
        {
            new DailyV1ScheduledDirectiveConfig(4,
                new DailyV1EnumValue(0, "RequestAction"),
                new DailyV1EnumValue(0, "EscapePrison"),
                "JOBSON", "FUGIR_DA_PRISAO"),
            new DailyV1ScheduledDirectiveConfig(5,
                new DailyV1EnumValue(0, "RequestAction"),
                new DailyV1EnumValue(0, "EscapePrison"),
                "JOBSON", "VIAJAR"),
            new DailyV1ScheduledDirectiveConfig(6,
                new DailyV1EnumValue(0, "RequestAction"),
                new DailyV1EnumValue(0, "EscapePrison"),
                "UNKNOWN_ACTOR", "FUGIR_DA_PRISAO")
        });
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredCityRoutes(allocator), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);

        ScheduledDirectiveStore directiveStore = new ScheduledDirectiveStore(new SimulationTime());
        List<string> warnings = new List<string>();
        List<string> errors = new List<string>();
        Assert.That(builder.TryCreateConfiguredScheduledDirectives(
            allocator, directiveStore, warnings.Add, errors.Add), Is.True);

        ScheduledDirective directive = directiveStore.Directives.Single();
        DailyV1NpcRuntimeOwners actor = builder.Npcs.Single(owner =>
            owner.Definition.DefinitionId == "JOBSON");
        DailyV1ActionDefinition action = definitions.Actions["FUGIR_DA_PRISAO"];
        Assert.That(directive.DirectiveId, Is.EqualTo("directive-000001"));
        Assert.That(directive.AbsoluteDay, Is.EqualTo(4));
        Assert.That(directive.Mode, Is.EqualTo(ScheduledDirectiveMode.RequestAction));
        Assert.That(directive.Operation, Is.EqualTo(ScheduledDirectiveOperation.EscapePrison));
        Assert.That(directive.ActorRuntimeId, Is.EqualTo(actor.RuntimeId));
        Assert.That(directive.ActionDefinitionId, Is.EqualTo(action.DefinitionId));
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(warnings, Has.Count.EqualTo(1),
            "A non-EscapePrison admitted action is rejected before directive identity allocation.");
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void ConfiguredInitialJusticeStateUsesExactCoreOwnersAndPreservesDefaultsAndOrder()
    {
        DailyV1EffectiveDefinitions baseline = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions definitions = WithInitialWarrants(baseline, new[]
        {
            new DailyV1InitialWantedRecordConfig("JOBSON", "CAMPO_VERDE", -4f, 0),
            new DailyV1InitialWantedRecordConfig("JOBSON", "CAMPO_VERDE", 6f, 2),
            new DailyV1InitialWantedRecordConfig("JORGE", "SERRA_DE_FERRO", 7f, 4),
            new DailyV1InitialWantedRecordConfig("UNKNOWN_NPC", "CAMPO_VERDE", 10f, 5),
            new DailyV1InitialWantedRecordConfig("JORGE", "UNKNOWN_CITY", 10f, 5),
            null
        });
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredCityRoutes(allocator), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);

        DailyV1JusticeRecordsOwner justice = new DailyV1JusticeRecordsOwner();
        Assert.That(builder.TryCreateConfiguredInitialJusticeState(justice), Is.True);
        Assert.That(justice.WantedRecords, Has.Count.EqualTo(2));
        Assert.That(justice.Revision, Is.Zero,
            "Genesis rows do not publish a post-admission P12 owner revision.");
        Assert.That(justice.HasP12MutationBoundary, Is.False);

        DailyV1NpcRuntimeOwners jobson = builder.Npcs.Single(owner =>
            owner.Definition.DefinitionId == "JOBSON");
        DailyV1CityRuntimeOwners campoVerde = builder.Cities.Single(owner =>
            owner.Definition.DefinitionId == "CAMPO_VERDE");
        DailyV1NpcRuntimeOwners jorge = builder.Npcs.Single(owner =>
            owner.Definition.DefinitionId == "JORGE");
        DailyV1CityRuntimeOwners serraDeFerro = builder.Cities.Single(owner =>
            owner.Definition.DefinitionId == "SERRA_DE_FERRO");
        DailyV1JusticeWantedRecordOwner first = justice.WantedRecords[0];
        DailyV1JusticeWantedRecordOwner second = justice.WantedRecords[1];
        Assert.That(first.TargetRuntimeId, Is.EqualTo(jobson.RuntimeId));
        Assert.That(first.CityRuntimeId, Is.EqualTo(campoVerde.RuntimeId));
        Assert.That(first.Bounty, Is.EqualTo(6f));
        Assert.That(first.SentenceDays, Is.EqualTo(3));
        Assert.That(second.TargetRuntimeId, Is.EqualTo(jorge.RuntimeId));
        Assert.That(second.CityRuntimeId, Is.EqualTo(serraDeFerro.RuntimeId));
        Assert.That(second.Bounty, Is.EqualTo(7f));
        Assert.That(second.SentenceDays, Is.EqualTo(4));

        DailyV1StatusDefinition wanted = definitions.Statuses[definitions.Configuration.wantedStatus];
        Assert.That(jobson.Status.Statuses, Does.Contain(wanted));
        Assert.That(jorge.Status.Statuses, Does.Contain(wanted));
        foreach (DailyV1NpcRuntimeOwners npc in builder.Npcs.Where(owner =>
            owner != jobson && owner != jorge))
            Assert.That(npc.Status.Statuses, Does.Not.Contain(wanted));

        Assert.That(builder.TryBuild(justice, out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.JusticeRecordsOwner, Is.SameAs(justice));
        Assert.That(graph.JusticeRecordsOwner.WantedRecords[0], Is.SameAs(first));
        Assert.That(graph.JusticeRecordsOwner.WantedRecords[1], Is.SameAs(second));
    }

    [Test]
    public void InitialSpatialKnowledgeIsSeededFromCoreRouteOrderAndOwnerIdentities()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        Assert.That(builder.TryCreateConfiguredCities(allocator), Is.True);
        Assert.That(builder.TryCreateConfiguredCityRoutes(allocator), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);

        Dictionary<DailyV1NpcRuntimeOwners, string[]> expectedLocations =
            new Dictionary<DailyV1NpcRuntimeOwners, string[]>();
        Dictionary<DailyV1NpcRuntimeOwners, string[]> expectedRoutes =
            new Dictionary<DailyV1NpcRuntimeOwners, string[]>();
        foreach (DailyV1NpcRuntimeOwners npc in builder.Npcs)
        {
            List<string> locations = new List<string>();
            List<string> routes = new List<string>();
            if (builder.SpatialNetwork.TryGetLocation(npc.Travel.CurrentLocationRuntimeId,
                    out SpatialLocationRuntime start))
            {
                locations.Add(start.RuntimeId);
                foreach (SpatialRouteRuntime route in builder.SpatialNetwork.GetOutgoingRoutes(start))
                {
                    if (route == null) continue;
                    routes.Add(route.RuntimeId);
                    if (route.Destination != null && !locations.Contains(route.Destination.RuntimeId))
                        locations.Add(route.Destination.RuntimeId);
                }
            }
            expectedLocations.Add(npc, locations.ToArray());
            expectedRoutes.Add(npc, routes.ToArray());
        }

        Assert.That(builder.TryBootstrapInitialSpatialKnowledge(), Is.True);
        foreach (DailyV1NpcRuntimeOwners npc in builder.Npcs)
        {
            Assert.That(npc.SpatialKnowledge.OwnerRuntimeId, Is.EqualTo(npc.RuntimeId));
            Assert.That(npc.SpatialKnowledge.KnownLocationRuntimeIds, Is.EqualTo(expectedLocations[npc]));
            Assert.That(npc.SpatialKnowledge.KnownRouteRuntimeIds, Is.EqualTo(expectedRoutes[npc]));
            Assert.That(npc.SpatialKnowledge.Revision,
                Is.EqualTo(expectedLocations[npc].Length + expectedRoutes[npc].Length));
        }

        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
            Assert.That(graph.Npcs.Single(owner => owner.RuntimeId == npc.RuntimeId).SpatialKnowledge,
                Is.SameAs(npc.SpatialKnowledge));
    }

    [Test]
    public void InitialCommercialKnowledgeUsesExactSelectedMarketRowsWithoutAddingSiteOrTopologyFacts()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(definitions.Configuration.explorableSites, Is.Empty,
            "The selected Daily-v1 profile proves P12F commercial owners without admitting P10 site content.");

        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        Assert.That(builder.TryCreateConfiguredCities(allocator), Is.True);
        Assert.That(builder.TryCreateConfiguredCityRoutes(allocator), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBootstrapInitialSpatialKnowledge(), Is.True);

        Dictionary<DailyV1NpcRuntimeOwners, List<DailyV1CityRuntimeOwners>> expectedCities =
            new Dictionary<DailyV1NpcRuntimeOwners, List<DailyV1CityRuntimeOwners>>();
        foreach (DailyV1NpcRuntimeOwners npc in builder.Npcs)
        {
            List<DailyV1CityRuntimeOwners> citiesToObserve = new List<DailyV1CityRuntimeOwners>();
            bool isTravelingMerchant = npc.Lifecycle.LifeState == NpcLifeState.Alive
                && DailyV1NpcDefinitionRules.HasJobType(npc.Definition, definitions,
                    DailyV1NpcDefinitionRules.MerchantJobTypeValue)
                && DailyV1NpcDefinitionRules.HasMerchantBehavior(npc.Definition, definitions,
                    DailyV1NpcDefinitionRules.TravelingMerchantBehaviorValue);
            if (isTravelingMerchant)
            {
                DailyV1CityRuntimeOwners origin = builder.Cities.Single(city =>
                    string.Equals(city.RuntimeId, npc.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
                if (npc.SpatialKnowledge.KnowsLocation(origin.LocationRuntimeId))
                    citiesToObserve.Add(origin);

                Assert.That(builder.SpatialNetwork.TryGetLocation(origin.LocationRuntimeId,
                    out SpatialLocationRuntime originLocation), Is.True);
                List<SpatialRouteRuntime> routes = builder.SpatialNetwork.GetOutgoingRoutes(originLocation)
                    .Where(route => route != null
                        && npc.SpatialKnowledge.KnowsRoute(route.RuntimeId)
                        && npc.SpatialKnowledge.KnowsLocation(route.Destination.RuntimeId)
                        && builder.SpatialNetwork.TryGetSingleDirectRoute(originLocation,
                            route.Destination, out SpatialRouteRuntime directRoute, out _)
                        && ReferenceEquals(directRoute, route))
                    .OrderBy(route => route.RuntimeId, StringComparer.Ordinal)
                    .ToList();
                foreach (SpatialRouteRuntime route in routes)
                {
                    DailyV1CityRuntimeOwners destination = builder.Cities.SingleOrDefault(city =>
                        string.Equals(city.LocationRuntimeId, route.Destination.RuntimeId,
                            StringComparison.Ordinal));
                    if (destination != null) citiesToObserve.Add(destination);
                }
            }
            expectedCities.Add(npc, citiesToObserve);
        }

        Dictionary<DailyV1NpcRuntimeOwners, long> spatialRevisions = builder.Npcs
            .ToDictionary(npc => npc, npc => npc.SpatialKnowledge.Revision);
        Dictionary<DailyV1NpcRuntimeOwners, string[]> spatialLocations = builder.Npcs
            .ToDictionary(npc => npc, npc => npc.SpatialKnowledge.KnownLocationRuntimeIds.ToArray());
        Dictionary<DailyV1NpcRuntimeOwners, string[]> spatialRoutes = builder.Npcs
            .ToDictionary(npc => npc, npc => npc.SpatialKnowledge.KnownRouteRuntimeIds.ToArray());
        Dictionary<DailyV1CityRuntimeOwners, long> marketRevisions = builder.Cities
            .ToDictionary(city => city, city => city.Market.Revision);
        Assert.That(builder.TryBootstrapInitialCommercialKnowledge(1L), Is.True,
            "A non-day-zero call is a no-op and does not consume the one-shot day-zero bootstrap.");
        foreach (DailyV1NpcRuntimeOwners npc in builder.Npcs)
        {
            Assert.That(npc.CommercialKnowledge.Observations, Is.Empty);
            Assert.That(npc.CommercialKnowledge.LiquidityObservations, Is.Empty);
        }

        long spatialNetworkRevision = builder.SpatialNetwork.Revision;
        Assert.That(builder.TryBootstrapInitialCommercialKnowledge(0L), Is.True);
        Assert.That(expectedCities.Values.SelectMany(cities => cities).SelectMany(city => city.Market.Items),
            Is.Not.Empty, "The accepted selected profile must exercise actual CommercialKnowledge market rows.");
        Assert.That(expectedCities.Count(pair => pair.Value.Count > 0), Is.GreaterThan(0),
            "At least one alive traveling Merchant has a current or known direct destination market.");
        foreach (DailyV1NpcRuntimeOwners npc in builder.Npcs)
        {
            List<DailyV1CityRuntimeOwners> expected = expectedCities[npc];
            List<DailyV1CommercialMarketObservation> expectedMarkets = new List<DailyV1CommercialMarketObservation>();
            List<DailyV1CommercialLiquidityObservation> expectedLiquidity = new List<DailyV1CommercialLiquidityObservation>();
            foreach (DailyV1CityRuntimeOwners city in expected)
            {
                foreach (DailyV1MarketItemState marketItem in city.Market.Items)
                {
                    if (marketItem == null || marketItem.Item == null
                        || string.IsNullOrWhiteSpace(marketItem.Item.DefinitionId)) continue;
                    expectedMarkets.Add(new DailyV1CommercialMarketObservation(city.LocationRuntimeId,
                        marketItem.Item, marketItem.CurrentPrice, marketItem.Amount, 0L, 0L,
                        DailyV1CommercialKnowledgeSource.InitialScenarioKnowledge));
                }
                MarketCounterpartyRuntime counterparty = city.Counterparty;
                expectedLiquidity.Add(new DailyV1CommercialLiquidityObservation(
                    city.LocationRuntimeId, counterparty.LiquidityMode,
                    counterparty.LiquidityMode == MarketLiquidityMode.AccountBacked
                        ? counterparty.MoneyAccount.Balance : 0f,
                    0L, 0L, DailyV1CommercialKnowledgeSource.InitialScenarioKnowledge));
            }

            IReadOnlyList<DailyV1CommercialMarketObservation> actualMarkets =
                npc.CommercialKnowledge.Observations;
            IReadOnlyList<DailyV1CommercialLiquidityObservation> actualLiquidity =
                npc.CommercialKnowledge.LiquidityObservations;
            Assert.That(actualMarkets, Has.Count.EqualTo(expectedMarkets.Count), npc.Definition.DefinitionId);
            Assert.That(actualLiquidity, Has.Count.EqualTo(expectedLiquidity.Count), npc.Definition.DefinitionId);
            for (int index = 0; index < expectedMarkets.Count; index++)
            {
                DailyV1CommercialMarketObservation actual = actualMarkets[index];
                DailyV1CommercialMarketObservation expectedMarket = expectedMarkets[index];
                Assert.That(actual.LocationRuntimeId, Is.EqualTo(expectedMarket.LocationRuntimeId));
                Assert.That(actual.ItemDefinition, Is.SameAs(expectedMarket.ItemDefinition));
                Assert.That(actual.ItemDefinition, Is.SameAs(definitions.Items[actual.ItemDefinitionId]));
                Assert.That(actual.ObservedPrice, Is.EqualTo(expectedMarket.ObservedPrice));
                Assert.That(actual.ObservedStock, Is.EqualTo(expectedMarket.ObservedStock));
                Assert.That(actual.ObservedDay, Is.Zero);
                Assert.That(actual.ReceivedDay, Is.Zero);
                Assert.That(actual.Source, Is.EqualTo(
                    DailyV1CommercialKnowledgeSource.InitialScenarioKnowledge));
                Assert.That(actual.SourceRuntimeId, Is.Null);
            }
            for (int index = 0; index < expectedLiquidity.Count; index++)
            {
                DailyV1CommercialLiquidityObservation actual = actualLiquidity[index];
                DailyV1CommercialLiquidityObservation expectedRow = expectedLiquidity[index];
                Assert.That(actual.LocationRuntimeId, Is.EqualTo(expectedRow.LocationRuntimeId));
                Assert.That(actual.LiquidityMode, Is.EqualTo(expectedRow.LiquidityMode));
                Assert.That(actual.ObservedPurchasingPower, Is.EqualTo(expectedRow.ObservedPurchasingPower));
                Assert.That(actual.ObservedDay, Is.Zero);
                Assert.That(actual.ReceivedDay, Is.Zero);
                Assert.That(actual.Source, Is.EqualTo(
                    DailyV1CommercialKnowledgeSource.InitialScenarioKnowledge));
                Assert.That(actual.SourceRuntimeId, Is.Null);
            }
            Assert.That(npc.CommercialKnowledge.Revision,
                Is.EqualTo(expectedMarkets.Count + expectedLiquidity.Count),
                "Each accepted P12F owner row keeps the existing local revision semantics.");
            Assert.That(npc.CommercialKnowledge.TryCopyOwnerSnapshot(out _, out _,
                out IReadOnlyList<DailyV1CommercialKnowledgeShareReceipt> receipts), Is.True);
            Assert.That(receipts, Is.Empty);
            Assert.That(npc.SpatialKnowledge.Revision, Is.EqualTo(spatialRevisions[npc]));
            Assert.That(npc.SpatialKnowledge.KnownLocationRuntimeIds,
                Is.EqualTo(spatialLocations[npc]));
            Assert.That(npc.SpatialKnowledge.KnownRouteRuntimeIds,
                Is.EqualTo(spatialRoutes[npc]),
                "Commercial seeding does not discover or imply additional spatial facts.");
        }

        Assert.That(builder.SpatialNetwork.Revision, Is.EqualTo(spatialNetworkRevision));
        foreach (DailyV1CityRuntimeOwners city in builder.Cities)
            Assert.That(city.Market.Revision, Is.EqualTo(marketRevisions[city]),
                "Commercial observations read selected market truth without mutating the market owner.");

        Assert.That(builder.TryBootstrapInitialCommercialKnowledge(0L), Is.False,
            "The day-zero seed is one-shot.");
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            DailyV1NpcRuntimeOwners builderOwner = builder.Npcs.Single(owner =>
                string.Equals(owner.RuntimeId, npc.RuntimeId, StringComparison.Ordinal));
            Assert.That(npc.CommercialKnowledge, Is.SameAs(builderOwner.CommercialKnowledge));
            Assert.That(npc.CommercialKnowledge.Definitions, Is.SameAs(definitions));
        }
        Assert.That(graph.Definitions.Configuration.explorableSites, Is.Empty,
            "The commercial seed does not create or imply P10 site/topology content.");
    }

    private static DailyV1EffectiveDefinitions WithScheduledDirectives(
        DailyV1EffectiveDefinitions baseline,
        IReadOnlyList<DailyV1ScheduledDirectiveConfig> scheduledDirectives)
    {
        DailyV1SimulationConfigData current = baseline.Configuration;
        return WithConfiguredJusticeAndDirectiveInputs(
            baseline, current.initialWarrants, scheduledDirectives);
    }

    private static DailyV1EffectiveDefinitions WithInitialWarrants(
        DailyV1EffectiveDefinitions baseline,
        IReadOnlyList<DailyV1InitialWantedRecordConfig> initialWarrants)
    {
        DailyV1SimulationConfigData current = baseline.Configuration;
        return WithConfiguredJusticeAndDirectiveInputs(
            baseline, initialWarrants, current.scheduledDirectives);
    }

    private static DailyV1EffectiveDefinitions WithConfiguredJusticeAndDirectiveInputs(
        DailyV1EffectiveDefinitions baseline,
        IReadOnlyList<DailyV1InitialWantedRecordConfig> initialWarrants,
        IReadOnlyList<DailyV1ScheduledDirectiveConfig> scheduledDirectives)
    {
        DailyV1SimulationConfigData current = baseline.Configuration;
        DailyV1SimulationConfigData configuration = new DailyV1SimulationConfigData(
            current.simulationName, current.calendar, current.enabledModules, current.cities,
            current.explorableSites, current.npcs, current.actions, current.statuses, current.jobs,
            initialWarrants, scheduledDirectives, current.useAuthoredGeographyProfile,
            current.authoredHexId, current.authoredHexQ, current.authoredHexR,
            current.authoredTerrainDefinitionId, current.authoredTerrainRevisionToken,
            current.authoredLocationId, current.authoredScaleConventionId,
            current.authoredScaleSourceIdentity, current.authoredScaleSourceVersion,
            current.authoredDistancePerNeighborStep, current.authoredScaleUnit,
            current.authoredP10RuinSite, current.genesisProfileContractIdentity,
            current.p10bStableSiteKey, current.travelCostPerDay,
            current.allowMerchantTradeRepositioning, current.maxMerchantTradeAmount,
            current.localMerchantWholesalePriceMultiplier, current.localMerchantReserveRatio,
            current.maxUnprofitablePlanWaitDays, current.crimeAutonomousEnabled,
            current.commercialKnowledge, current.useFixedSimulationSeed, current.simulationSeed,
            current.includeEconomySnapshots, current.economySnapshotIntervalDays,
            current.logSettings, current.naturalMortalityEnabled,
            current.naturalMortalityAnnualProbability, current.aggregateDemographyEnabled,
            current.aggregateAnnualBirthRate, current.aggregateAnnualDeathRate,
            current.freeStatus, current.wantedStatus, current.arrestedStatus, current.hiddenStatus);
        return new DailyV1EffectiveDefinitions(configuration, baseline.EffectiveConfiguration,
            baseline.Calendar, null, baseline.Cities.Values, baseline.Items.Values,
            baseline.Npcs.Values, baseline.Jobs.Values, baseline.Actions.Values,
            baseline.Statuses.Values, null, baseline.StageOrder, baseline.ProfileContractIdentity,
            baseline.GeographyProfileContractIdentity, baseline.GeographyStageIdentity,
            baseline.HexCoordinateConvention, baseline.HexCoordinateCanonicalOrder, null,
            baseline.Traits.Values, baseline.CapabilityAttributes.Values, baseline.ExplorableSites.Values);
    }

    private static long GetSpatialRevision(SpatialNetworkOwner owner)
    {
        return owner.Revision;
    }

    [Test]
    public void ExactGraphCarriesSamePersonStoreAndRejectsMaterializedNpcOutsideRoster()
    {
        Assert.That(DailyV1SelectedRuntimeOwnerGraphBuilder.TryBuildConfigured(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), new RuntimeIdAllocator(),
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        PersonStore personStore = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("person/graph-binding"), 12L);
        Assert.That(personStore.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(personStore.TryBindMaterializedNpc(person.PersonId,
            graph.Npcs[0].RuntimeId, out PersonStoreFailure bindFailure),
            Is.True, bindFailure.ToString());

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            graph.Cities, graph.Npcs, graph.SpatialNetwork, graph.JusticeRecordsOwner,
            graph.TravelParties, graph.RuntimeIdentityIndex, personStore,
            out DailyV1SelectedRuntimeOwnerGraph boundGraph), Is.True);
        Assert.That(boundGraph.PersonStore, Is.SameAs(personStore));
        Assert.That(boundGraph.RuntimeIdentityIndex, Is.SameAs(graph.RuntimeIdentityIndex));
        Assert.That(boundGraph.Npcs.Count, Is.EqualTo(graph.Npcs.Count));
        for (int index = 0; index < graph.Npcs.Count; index++)
            Assert.That(boundGraph.Npcs[index], Is.SameAs(graph.Npcs[index]));

        PersonStore mismatchedPersonStore = new PersonStore();
        PersonRuntime mismatchedPerson = new PersonRuntime(new PersonId("person/outside-graph"), 12L);
        Assert.That(mismatchedPersonStore.TryRegister(mismatchedPerson, out _), Is.True);
        Assert.That(mismatchedPersonStore.TryBindMaterializedNpc(mismatchedPerson.PersonId,
            "npc/not-in-selected-graph", out _), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            graph.Cities, graph.Npcs, graph.SpatialNetwork, graph.JusticeRecordsOwner,
            graph.TravelParties, graph.RuntimeIdentityIndex, mismatchedPersonStore, out _), Is.False);
    }

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
        TravelPartyStore partyStore = new TravelPartyStore();
        Assert.That(builder.TryBuild(null, partyStore,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
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
        long travelRevisionBefore = npc.Travel.Revision;
        TravelPartyStore mismatchedStore = new TravelPartyStore();
        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSoloTravelSlot(
            graph, npc, destination.RuntimeId, mismatchedStore, _ => true,
            out DailyV1SelectedActionExecutionStage.SoloTravelResult mismatchedStoreResult), Is.False);
        Assert.That(mismatchedStoreResult, Is.Null);
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(balanceBeforeTravel).Within(0.001f));
        Assert.That(npc.Travel.Revision, Is.EqualTo(travelRevisionBefore));
        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSoloTravelSlot(
            graph, npc, destination.RuntimeId, partyStore, _ => false,
            out DailyV1SelectedActionExecutionStage.SoloTravelResult rejectedTravel), Is.True);
        Assert.That(rejectedTravel.Succeeded, Is.False);
        Assert.That(rejectedTravel.Execution.Failure,
            Is.EqualTo(DailyV1SelectedSoloTravelFailure.TransitionRejected));
        Assert.That(rejectedTravel.Execution.RefundSucceeded, Is.True);
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(balanceBeforeTravel).Within(0.001f));
        Assert.That(npc.Travel.IsTraveling, Is.False);

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
            graph, npc, destination.RuntimeId, partyStore, transition => transition.TryCommit(),
            out DailyV1SelectedActionExecutionStage.SoloTravelResult stagedTravel), Is.True);
        Assert.That(stagedTravel.Succeeded, Is.True);
        Assert.That(stagedTravel.Route.RuntimeId, Is.EqualTo("route/merchant-test"));
        Assert.That(stagedTravel.Transition.OriginDecisionId, Is.EqualTo("decision/selected-travel"));
        Assert.That(npc.MoneyAccount.Balance,
            Is.EqualTo(balanceBeforeTravel - stagedTravel.TravelCost).Within(0.001f));
        Assert.That(npc.Travel.TravelRouteRuntimeId, Is.EqualTo(stagedTravel.Route.RuntimeId));
    }

    [Test]
    public void SelectedSoloTravelDispatcherPreservesRepositionAndTravelCallbackOrder()
    {
        CreateSelectedTravelFixture(NpcTravelReason.TradeReposition,
            out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor,
            out DailyV1CityRuntimeOwners destination,
            out DailyV1ActionDefinition action,
            out TravelPartyStore partyStore);
        List<string> order = new List<string>();
        RecordingSelectedTravelActionBoundary boundary = new RecordingSelectedTravelActionBoundary(
            order, graph, actor, NpcTravelReason.TradeReposition);
        FixedTravelActionRandomSource random = new FixedTravelActionRandomSource();
        long travelPlanRevisionBefore = actor.TravelPlan.Revision;

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
            graph, actor, action, random, 27L, boundary,
            out DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult result), Is.True);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Travel.Succeeded, Is.True);
        Assert.That(order, Is.EqualTo(new[]
        {
            "operation-enter", "reposition-preflight", "reposition-log",
            "transition-preflight", "transition-projection-preflight", "transition-admission",
            "transition-commit", "travel-publication", "plan-clear-preflight",
            "plan-clear-admission", "plan-clear-published", "operation-exit"
        }));
        Assert.That(boundary.TransitionAdmissionCalls, Is.EqualTo(1));
        Assert.That(boundary.TravelPlanClearAdmissionCalls, Is.EqualTo(1));
        Assert.That(actor.Travel.IsTraveling, Is.True);
        Assert.That(actor.SpatialKnowledge.KnowsLocation(result.Travel.Route.Origin.RuntimeId), Is.True);
        Assert.That(actor.SpatialKnowledge.KnowsRoute(result.Travel.Route.RuntimeId), Is.True);
        Assert.That(actor.TravelPlan.TargetCityRuntimeId, Is.Null);
        Assert.That(actor.TravelPlan.Reason, Is.EqualTo(NpcTravelReason.None));
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(travelPlanRevisionBefore + 1L));
        Assert.That(graph.TryCreateWithoutNpc(actor,
            out DailyV1SelectedRuntimeOwnerGraph rosterAfterRemoval), Is.True);
        Assert.That(rosterAfterRemoval.TravelParties, Is.SameAs(partyStore));

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc/added-travel-party-preservation", graph.Definitions,
            actor.Definition, 0f,
            out DailyV1NpcRuntimeOwners addedNpc), Is.True);
        Assert.That(graph.TryCreateWithAddedNpc(addedNpc,
            out DailyV1SelectedRuntimeOwnerGraph rosterAfterAddition), Is.True);
        Assert.That(rosterAfterAddition.TravelParties, Is.SameAs(partyStore));
    }

    [Test]
    public void SelectedSoloTravelDispatcherPublishesCommercialScoutBeforeClearingCorePlan()
    {
        CreateSelectedTravelFixture(NpcTravelReason.CommercialScout,
            out _, out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor,
            out _, out DailyV1ActionDefinition action,
            out TravelPartyStore partyStore);
        List<string> order = new List<string>();
        RecordingSelectedTravelActionBoundary boundary = new RecordingSelectedTravelActionBoundary(
            order, graph, actor, NpcTravelReason.CommercialScout);
        long travelPlanRevisionBefore = actor.TravelPlan.Revision;

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
            graph, actor, action, new FixedTravelActionRandomSource(), 28L, boundary,
            out DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult result), Is.True);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(order, Is.EqualTo(new[]
        {
            "operation-enter", "transition-preflight", "transition-projection-preflight",
            "transition-admission", "transition-commit", "travel-publication",
            "commercial-scout", "plan-clear-preflight", "plan-clear-admission",
            "plan-clear-published", "operation-exit"
        }));
        Assert.That(boundary.TravelPlanClearAdmissionCalls, Is.EqualTo(1));
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(travelPlanRevisionBefore + 1L));
        Assert.That(actor.TravelPlan.TargetLocationRuntimeId, Is.Null);
        Assert.That(actor.TravelPlan.TargetCityRuntimeId, Is.Null);
        Assert.That(actor.TravelPlan.Reason, Is.EqualTo(NpcTravelReason.None));
    }

    [Test]
    public void SelectedSoloTravelDispatcherRefusesMismatchedSlotBeforeRandomnessAndLetsCoreRejectMissingDestination()
    {
        CreateSelectedTravelFixture(NpcTravelReason.Trade,
            out DailyV1EffectiveDefinitions definitions,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor,
            out DailyV1CityRuntimeOwners destination,
            out DailyV1ActionDefinition action,
            out TravelPartyStore partyStore);
        List<string> order = new List<string>();
        RecordingSelectedTravelActionBoundary boundary = new RecordingSelectedTravelActionBoundary(
            order, graph, actor, NpcTravelReason.Trade);
        FixedTravelActionRandomSource random = new FixedTravelActionRandomSource();
        float balanceBefore = actor.MoneyAccount.Balance;
        long travelRevisionBefore = actor.Travel.Revision;
        long travelPlanRevisionBefore = actor.TravelPlan.Revision;

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            graph.Cities, graph.Npcs, graph.SpatialNetwork, graph.JusticeRecordsOwner,
            null, out DailyV1SelectedRuntimeOwnerGraph graphWithoutTravelPartyOwner), Is.True);
        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
            graphWithoutTravelPartyOwner, actor, action, random, 29L, boundary, out _), Is.False,
            "Core selected Travel requires the graph-bound P8 TravelParty owner.");
        Assert.That(random.Calls, Is.Zero);
        Assert.That(order, Is.Empty);

        DailyV1ActionDefinition mismatchedAction = definitions.Actions.Values.First(candidate =>
            candidate.Values.actionType?.Value != DailyV1NpcDefinitionRules.TravelActionTypeValue);
        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
            graph, actor, mismatchedAction, random, 29L, boundary, out _), Is.False);
        Assert.That(random.Calls, Is.Zero);
        Assert.That(order, Is.Empty);

        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, action,
            null, null, null, 0, 0f, 1f, NpcTravelReason.Trade,
            0f, "decision/selected-travel", null, null, null,
            out DailyV1NpcActionPayloadOwner unsupportedPayload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, unsupportedPayload, null), Is.True);
        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
            graph, actor, action, random, 29L, boundary, out _), Is.False,
            "An unbound target is not dispatched by the Core Travel operation.");
        Assert.That(random.Calls, Is.Zero);
        Assert.That(order, Is.Empty);

        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, action,
            null, "missing-city", null, 0, 0f, 1f, NpcTravelReason.Trade,
            0f, "decision/selected-travel", null, null, null,
            out DailyV1NpcActionPayloadOwner missingDestinationPayload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, missingDestinationPayload, null), Is.True);
        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
            graph, actor, action, random, 29L, boundary,
            out DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult missingDestination), Is.True);
        Assert.That(missingDestination.Succeeded, Is.False);
        Assert.That(missingDestination.ActionRollSucceeded, Is.True,
            "An exact supported Travel slot consumes its action roll and enters the P12 wrapper before route resolution.");
        Assert.That(random.Calls, Is.Zero, "This authored Travel action is non-failing and therefore has no roll draw.");
        Assert.That(order, Is.EqualTo(new[] { "operation-enter", "operation-exit" }));
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balanceBefore));
        Assert.That(actor.Travel.Revision, Is.EqualTo(travelRevisionBefore));
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(travelPlanRevisionBefore));
        Assert.That(actor.Travel.IsTraveling, Is.False);
        Assert.That(destination.RuntimeId, Is.Not.Empty);
    }

    [Test]
    public void SelectedSoloTravelDispatcherStopsAfterFailedActionRoll()
    {
        CreateSelectedTravelFixture(NpcTravelReason.Trade, out _, out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out _, out DailyV1ActionDefinition action,
            out TravelPartyStore partyStore, canFailTravel: true, travelSuccessChance: 0f);
        List<string> order = new List<string>();
        RecordingSelectedTravelActionBoundary boundary = new RecordingSelectedTravelActionBoundary(
            order, graph, actor, NpcTravelReason.Trade);
        FixedTravelActionRandomSource random = new FixedTravelActionRandomSource(0.5f);
        float balanceBefore = actor.MoneyAccount.Balance;
        long travelRevisionBefore = actor.Travel.Revision;

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
            graph, actor, action, random, 30L, boundary,
            out DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult result), Is.True);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ActionRollSucceeded, Is.False);
        Assert.That(random.Calls, Is.EqualTo(1));
        Assert.That(order, Is.Empty, "P12 admission and all travel callbacks follow a successful roll.");
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balanceBefore));
        Assert.That(actor.Travel.Revision, Is.EqualTo(travelRevisionBefore));
        Assert.That(actor.Travel.IsTraveling, Is.False);
    }

    [Test]
    public void SelectedSoloTravelDispatcherP12RefusalPrecedesDebitAndTransition()
    {
        CreateSelectedTravelFixture(NpcTravelReason.Trade, out _, out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out _, out DailyV1ActionDefinition action,
            out TravelPartyStore partyStore);
        List<string> order = new List<string>();
        RecordingSelectedTravelActionBoundary boundary = new RecordingSelectedTravelActionBoundary(
            order, graph, actor, NpcTravelReason.Trade) { RefuseOperation = true };
        float balanceBefore = actor.MoneyAccount.Balance;
        long travelRevisionBefore = actor.Travel.Revision;
        long planRevisionBefore = actor.TravelPlan.Revision;

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
            graph, actor, action, new FixedTravelActionRandomSource(), 31L, boundary,
            out DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult result), Is.True);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(order, Is.EqualTo(new[] { "operation-enter", "operation-refused" }));
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balanceBefore));
        Assert.That(actor.Travel.Revision, Is.EqualTo(travelRevisionBefore));
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(planRevisionBefore));
        Assert.That(actor.Travel.IsTraveling, Is.False);
    }

    [Test]
    public void SelectedSoloTravelDispatcherRejectsImmediateP12AdmissionAndRefundsBeforePublishing()
    {
        CreateSelectedTravelFixture(NpcTravelReason.Trade, out _, out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out _, out DailyV1ActionDefinition action,
            out _, canFailTravel: false);
        List<string> order = new List<string>();
        RecordingSelectedTravelActionBoundary boundary = new RecordingSelectedTravelActionBoundary(
            order, graph, actor, NpcTravelReason.Trade) { RefuseTransitionAdmission = true };
        float balanceBefore = actor.MoneyAccount.Balance;
        long travelRevisionBefore = actor.Travel.Revision;

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
            graph, actor, action, new FixedTravelActionRandomSource(), 32L, boundary,
            out DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult result), Is.True);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Travel, Is.Null);
        Assert.That(order, Is.EqualTo(new[]
        {
            "operation-enter", "transition-preflight", "transition-projection-preflight",
            "transition-admission", "operation-exit"
        }));
        Assert.That(boundary.TransitionAdmissionCalls, Is.EqualTo(1));
        Assert.That(boundary.SawTravelChargeDuringProjectionPreflight, Is.True,
            "The existing Unity projection preflight remains after the charge and is compensated if P12 refuses.");
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balanceBefore));
        Assert.That(actor.Travel.Revision, Is.EqualTo(travelRevisionBefore));
        Assert.That(actor.Travel.IsTraveling, Is.False);
    }

    [Test]
    public void SelectedSoloTravelDispatcherClearsChangedCorePlanAfterTravelPublication()
    {
        CreateSelectedTravelFixture(NpcTravelReason.Trade, out _, out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners destination,
            out DailyV1ActionDefinition action, out _);
        Assert.That(actor.TravelPlan.TryCommit(destination.LocationRuntimeId,
            destination.RuntimeId, NpcTravelReason.Trade, 6f, 2f,
            "decision/prior-travel-plan"), Is.True);
        long travelPlanRevisionBefore = actor.TravelPlan.Revision;
        List<string> order = new List<string>();
        RecordingSelectedTravelActionBoundary boundary = new RecordingSelectedTravelActionBoundary(
            order, graph, actor, NpcTravelReason.Trade);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
            graph, actor, action, new FixedTravelActionRandomSource(), 33L, boundary,
            out DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult result), Is.True);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(order, Is.EqualTo(new[]
        {
            "operation-enter", "transition-preflight", "transition-projection-preflight",
            "transition-admission", "transition-commit", "travel-publication",
            "plan-clear-preflight", "plan-clear-admission", "plan-clear-published", "operation-exit"
        }));
        Assert.That(boundary.TravelPlanClearAdmissionCalls, Is.EqualTo(1));
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(travelPlanRevisionBefore + 1L));
        Assert.That(actor.TravelPlan.TargetLocationRuntimeId, Is.Null);
        Assert.That(actor.TravelPlan.TargetCityRuntimeId, Is.Null);
        Assert.That(actor.TravelPlan.Reason, Is.EqualTo(NpcTravelReason.None));
    }

    [Test]
    public void SelectedTravelPlanClearRejectsForeignOwnerAndRevisionExhaustionWithoutAdmission()
    {
        CreateSelectedTravelFixture(NpcTravelReason.Trade, out _, out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners destination,
            out _, out _);
        int admissions = 0;
        int publications = 0;
        Assert.That(graph.TryCreateWithoutNpc(actor,
            out DailyV1SelectedRuntimeOwnerGraph graphWithoutActor), Is.True);
        Assert.That(graphWithoutActor.TryClearSelectedTravelPlan(actor,
            () => { admissions++; return true; }, () => publications++, out bool foreignChanged), Is.False);
        Assert.That(foreignChanged, Is.False);
        Assert.That(admissions, Is.Zero);
        Assert.That(publications, Is.Zero);

        Assert.That(graph.TryClearSelectedTravelPlan(actor,
            () => { admissions++; return true; }, () => publications++, out bool cleared), Is.True);
        Assert.That(cleared, Is.True);
        Assert.That(admissions, Is.EqualTo(1));
        Assert.That(publications, Is.EqualTo(1));
        Assert.That(graph.TryClearSelectedTravelPlan(actor,
            () => { admissions++; return true; }, () => publications++, out bool noOp), Is.True);
        Assert.That(noOp, Is.False);
        Assert.That(admissions, Is.EqualTo(1), "A no-op clear does not request P12 admission.");
        Assert.That(publications, Is.EqualTo(1), "A no-op clear does not publish another mutation.");

        Assert.That(actor.TravelPlan.TryInstallExact(destination.LocationRuntimeId,
            destination.RuntimeId, NpcTravelReason.Trade, 1f, 1f,
            "decision/exhausted-travel-plan", long.MaxValue), Is.True);
        Assert.That(graph.TryClearSelectedTravelPlan(actor,
            () => { admissions++; return true; }, () => publications++, out bool exhaustedChanged), Is.False);
        Assert.That(exhaustedChanged, Is.False);
        Assert.That(admissions, Is.EqualTo(1));
        Assert.That(publications, Is.EqualTo(1));
        Assert.That(actor.TravelPlan.Matches(destination.LocationRuntimeId,
            destination.RuntimeId, NpcTravelReason.Trade, 1f, 1f,
            "decision/exhausted-travel-plan"), Is.True);
    }

    private static void CreateSelectedTravelFixture(
        NpcTravelReason reason,
        out DailyV1EffectiveDefinitions definitions,
        out DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1NpcRuntimeOwners actor,
        out DailyV1CityRuntimeOwners destination,
        out DailyV1ActionDefinition action,
        out TravelPartyStore partyStore,
        bool canFailTravel = false,
        float travelSuccessChance = 1f)
    {
        definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        if (canFailTravel)
            definitions = CreateTravelRollDefinitions(definitions, travelSuccessChance);
        SpatialNetworkOwner spatial = new SpatialNetworkOwner();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions, spatial);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        DailyV1CityRuntimeOwners origin = builder.Cities.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "CAMPO_VERDE", StringComparison.Ordinal));
        destination = builder.Cities.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "SERRA_DE_FERRO", StringComparison.Ordinal));
        Assert.That(spatial.TryGetLocation(origin.LocationRuntimeId,
            out SpatialLocationRuntime originLocation), Is.True);
        Assert.That(spatial.TryGetLocation(destination.LocationRuntimeId,
            out SpatialLocationRuntime destinationLocation), Is.True);
        Assert.That(spatial.TryRegisterRoute(new SpatialRouteRuntime("route/selected-dispatch",
            originLocation, destinationLocation, 3), () => true), Is.True);
        Assert.That(spatial.TryRegisterRoute(new SpatialRouteRuntime("route/selected-dispatch-return",
            destinationLocation, originLocation, 3), () => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        partyStore = new TravelPartyStore();
        Assert.That(builder.TryBuild(null, partyStore, out graph), Is.True);
        Assert.That(graph.TravelParties, Is.SameAs(partyStore));
        actor = graph.Npcs.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "JORGE", StringComparison.Ordinal));
        string destinationRuntimeId = destination.RuntimeId;
        destination = graph.Cities.Single(owner =>
            string.Equals(owner.RuntimeId, destinationRuntimeId, StringComparison.Ordinal));
        action = definitions.Actions.Values.Single(candidate =>
            candidate.Values.actionType?.Value == DailyV1NpcDefinitionRules.TravelActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, action,
            null, destination.RuntimeId, null, 0, 0f, 1f, reason,
            0f, "decision/selected-travel", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, payload, null), Is.True);
        Assert.That(actor.TravelPlan.TryInstallExact(destination.LocationRuntimeId,
            destination.RuntimeId, reason, 60f, 30f, "decision/selected-travel", 1L), Is.True);
    }

    private static DailyV1EffectiveDefinitions CreateTravelRollDefinitions(
        DailyV1EffectiveDefinitions source, float travelSuccessChance)
    {
        IEnumerable<DailyV1NpcActionData> actions = source.Actions.Values.Select(action =>
        {
            DailyV1NpcActionData values = action.Values;
            if (values.actionType?.Value != DailyV1NpcDefinitionRules.TravelActionTypeValue)
                return values;
            return new DailyV1NpcActionData(values.id, values.statusNecessariosParaFazerAcao,
                values.actionName, values.normalActionLogText, values.actionCategory, values.actionType,
                values.baseUtility, values.statusModifiers, true, travelSuccessChance,
                values.statusToAdd, values.statusToRemove, values.targetStatusToAdd,
                values.targetStatusToRemove, values.crimeSettings);
        });
        return DailyV1EffectiveDefinitions.CreateSelectedSchema(
            source.Configuration, source.EffectiveConfiguration, source.Calendar,
            source.Cities.Values.Select(value => value.Values),
            source.Items.Values.Select(value => value.Values),
            source.Npcs.Values.Select(value => value.Values),
            source.Jobs.Values.Select(value => value.Values), actions,
            source.Statuses.Values.Select(value => value.Values), source.StageOrder,
            source.ProfileContractIdentity, source.GeographyProfileContractIdentity,
            source.GeographyStageIdentity, source.HexCoordinateConvention,
            source.HexCoordinateCanonicalOrder);
    }

    private sealed class FixedTravelActionRandomSource : IAuthoritativeRandomSource
    {
        private readonly float value;
        public int Calls { get; private set; }
        internal FixedTravelActionRandomSource(float value = 0f) => this.value = value;
        public float NextUnit(string streamKey, long drawIndex = 0L) { Calls++; return value; }
        public DeterministicRandomStream CreateStream(string streamKey) => null;
    }

    private sealed class RecordingSelectedTravelActionBoundary
        : DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary
    {
        private readonly List<string> order;
        private readonly DailyV1NpcRuntimeOwners actor;
        private readonly float initialBalance;
        internal bool RefuseOperation { get; set; }
        internal bool RefuseTransitionAdmission { get; set; }
        internal int TransitionAdmissionCalls { get; private set; }
        internal int TravelPlanClearAdmissionCalls { get; private set; }
        internal bool SawTravelChargeDuringProjectionPreflight { get; private set; }

        internal RecordingSelectedTravelActionBoundary(List<string> order,
            DailyV1SelectedRuntimeOwnerGraph graph, DailyV1NpcRuntimeOwners actor,
            NpcTravelReason reason)
        {
            this.order = order;
            this.actor = actor;
            initialBalance = actor.MoneyAccount.Balance;
        }

        public bool ExecuteWithinOperation(Func<bool> operation)
        {
            order.Add("operation-enter");
            if (RefuseOperation)
            {
                order.Add("operation-refused");
                return false;
            }
            bool succeeded = operation();
            order.Add("operation-exit");
            return succeeded;
        }

        public bool CanStartTradeReposition()
        { order.Add("reposition-preflight"); return true; }

        public void LogTradeReposition() => order.Add("reposition-log");

        public bool CanCommitPreparedTransition(DailyV1SelectedTravelTransitionPlan plan,
            SpatialRouteRuntime route, float travelCost)
        { order.Add("transition-preflight"); return true; }

        public bool TryPrepareTransitionProjection(DailyV1SelectedTravelTransitionPlan plan)
        {
            order.Add("transition-projection-preflight");
            SawTravelChargeDuringProjectionPreflight = actor.MoneyAccount.Balance < initialBalance;
            return true;
        }

        public bool CanAdmitPreparedTransition(DailyV1SelectedTravelTransitionPlan plan)
        {
            TransitionAdmissionCalls++;
            order.Add("transition-admission");
            return !RefuseTransitionAdmission;
        }

        public void PublishCommittedTransition(DailyV1SelectedTravelTransitionPlan plan)
        {
            Assert.That(plan.IsCommitted, Is.True);
            order.Add("transition-commit");
        }

        public void PublishTravelStarted(DailyV1SelectedActionExecutionStage.SoloTravelResult result) =>
            order.Add("travel-publication");

        public void LogCommercialScouting() => order.Add("commercial-scout");

        public bool CanProjectTravelPlanClear(DailyV1NpcRuntimeOwners candidate)
        {
            order.Add("plan-clear-preflight");
            return ReferenceEquals(candidate, actor);
        }

        public bool CanAdmitTravelPlanClear(DailyV1NpcRuntimeOwners candidate)
        {
            TravelPlanClearAdmissionCalls++;
            order.Add("plan-clear-admission");
            return ReferenceEquals(candidate, actor);
        }

        public void PublishCommittedTravelPlanClear(DailyV1NpcRuntimeOwners candidate)
        {
            Assert.That(candidate.TravelPlan.Matches(null, null, NpcTravelReason.None,
                0f, 0f, null), Is.True);
            order.Add("plan-clear-published");
        }
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
    public void SelectedTravelPartyArrivalCommitsAllMembersAcrossSharedPresenceRevisionChanges()
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

        DailyV1NpcRuntimeOwners[] members = graph.Npcs
            .GroupBy(npc => npc.Travel.CurrentCityRuntimeId, StringComparer.Ordinal)
            .Where(group => group.Count() >= 2)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .First()
            .OrderBy(npc => npc.RuntimeId, StringComparer.Ordinal)
            .Take(2)
            .ToArray();
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, members[0].Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        DailyV1CityRuntimeOwners destination = graph.Cities.Single(city => !ReferenceEquals(city, origin));
        const string partyId = "party-shared-presence-arrival-1";
        const string decisionId = "party-shared-presence-arrival-decision";
        const string routeId = "party-shared-presence-arrival-route";

        foreach (DailyV1NpcRuntimeOwners member in members)
        {
            Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
                member,
                origin,
                origin.LocationRuntimeId,
                destination.LocationRuntimeId,
                destination,
                requestedTravelDays: 1,
                originDecisionId: decisionId,
                routeId: routeId,
                out DailyV1SelectedTravelTransitionPlan startPlan), Is.True);
            Assert.That(graph.TryCommitTravelTransition(member, startPlan, null), Is.True);
        }

        TravelPartyRuntime party = new TravelPartyRuntime(
            partyId,
            origin.LocationRuntimeId,
            destination.LocationRuntimeId,
            routeId,
            members.Select(member => member.RuntimeId).ToArray(),
            Array.Empty<string>(),
            1,
            decisionId,
            members.Select(member => new TravelPartyMemberCost(member.RuntimeId, 1f)).ToArray());
        TravelPartyStore partyStore = new TravelPartyStore();
        Assert.That(partyStore.Add(party), Is.True);
        foreach (DailyV1NpcRuntimeOwners member in members)
        {
            Assert.That(DailyV1SelectedTravelTransitionPlan.TrySetActiveTravelParty(
                member, partyId, out DailyV1SelectedTravelTransitionPlan setPartyPlan), Is.True);
            Assert.That(graph.TryCommitTravelTransition(member, setPartyPlan, null), Is.True);
        }

        Assert.That(graph.TryAdvanceTravelParties(
            partyStore,
            (_, _, _) => true,
            (_, _, _) => { },
            _ =>
            {
                Assert.Fail("The start-day marker must be cleared before travel advances.");
                return false;
            },
            out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> startDayResults), Is.True);
        Assert.That(startDayResults, Is.Empty);
        Assert.That(members.Select(member => member.Travel.TravelDaysRemaining), Is.All.EqualTo(1));
        Assert.That(members.Select(member => member.Travel.TravelStartedToday), Is.All.False);

        long destinationPresenceRevision = destination.NpcPresence.Revision;
        bool eventObservedBeforePartyCompletion = false;
        Assert.That(graph.TryAdvanceTravelParties(
            partyStore,
            (_, _, _) => true,
            (_, _, _) => { },
            currentParty =>
            {
                Assert.That(currentParty, Is.SameAs(party));
                Assert.That(partyStore.ActiveParties, Does.Contain(party));
                Assert.That(members.Select(member => member.Travel.TravelDaysRemaining), Is.All.Zero);
                Assert.That(members.Select(member => member.Travel.ActiveTravelPartyId),
                    Is.All.EqualTo(partyId));
                Assert.That(members.All(member => destination.NpcPresence.Contains(member.RuntimeId)), Is.True,
                    "Every party member's destination presence must commit before the arrival event is published.");
                eventObservedBeforePartyCompletion = true;
                return true;
            },
            out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> arrivals), Is.True);

        Assert.That(eventObservedBeforePartyCompletion, Is.True);
        Assert.That(arrivals, Has.Count.EqualTo(1));
        Assert.That(arrivals[0].Party, Is.SameAs(party));
        Assert.That(arrivals[0].Members, Is.EqualTo(members));
        Assert.That(partyStore.ActiveParties, Is.Empty);
        Assert.That(destination.NpcPresence.Revision, Is.EqualTo(destinationPresenceRevision + members.Length));
        foreach (DailyV1NpcRuntimeOwners member in members)
        {
            Assert.That(member.Travel.CurrentCityRuntimeId, Is.EqualTo(destination.RuntimeId));
            Assert.That(member.Travel.DestinationCityRuntimeId, Is.Null);
            Assert.That(member.Travel.TravelDaysRemaining, Is.Zero);
            Assert.That(member.Travel.ActiveTravelPartyId, Is.Null);
            Assert.That(destination.NpcPresence.Contains(member.RuntimeId), Is.True);
        }
    }

    [Test]
    public void SelectedDailyExecutionRunsCorePartyThenSoloTravelAfterActorTurns()
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
        Assert.That(partyOrigin.NpcPresence.Contains(partyMember.RuntimeId), Is.False,
            "The committed travel departure already removes the member from origin presence before arrival-day execution.");

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
        long partyTravelRevisionBefore = partyMember.Travel.Revision;
        long soloTravelRevisionBefore = solo.Travel.Revision;
        RecordingTravelExecutionContext context = new RecordingTravelExecutionContext(
            order, store, new[] { partyMember.RuntimeId }, boundary, advanceSoloTravel: true);
        RecordingDailyExecutionBoundary dailyBoundary = new RecordingDailyExecutionBoundary(
            order, context, graph, partyMember, solo);
        Assert.That(graph.TryBindPersonStore(new PersonStore()), Is.True);
        DailyV1SelectedDailyExecutionStage.Advance(
            3L, graph, null, dailyBoundary);

        Assert.That(context.PartyArrivals, Has.Count.EqualTo(1));
        Assert.That(context.SoloResults, Has.Count.EqualTo(1));
        Assert.That(context.PartyArrivals[0].Party, Is.SameAs(party));
        Assert.That(context.PartyArrivals[0].Members, Has.Count.EqualTo(1));
        Assert.That(context.PartyArrivals[0].Members[0], Is.SameAs(partyMember));
        Assert.That(context.SoloResults[0].Npc, Is.SameAs(solo));
        Assert.That(context.SoloResults[0].WasCommitted, Is.True);
        Assert.That(context.SoloResults[0].Arrived, Is.False);
        Assert.That(context.PartyOperationCount, Is.EqualTo(1),
            "The exact TravelParty owner operation is admitted once around the party batch.");
        Assert.That(partyMember.Travel.Revision, Is.EqualTo(partyTravelRevisionBefore + 2),
            "Core commits the arriving travel transition and then clears active-party identity.");
        Assert.That(solo.Travel.Revision, Is.EqualTo(soloTravelRevisionBefore + 1),
            "Core advances the ungrouped solo travel owner once after the party operation.");
        Assert.That(partyMember.Travel.CurrentCityRuntimeId, Is.EqualTo(partyDestination.RuntimeId));
        Assert.That(partyMember.Travel.ActiveTravelPartyId, Is.Null);
        Assert.That(partyMember.SpatialKnowledge.Revision,
            Is.EqualTo(dailyBoundary.PartySpatialKnowledgeRevisionBeforeTravel
                + (dailyBoundary.PartyKnewDestinationBeforeTravel ? 0 : 1)));
        Assert.That(solo.Travel.TravelDaysRemaining, Is.EqualTo(1));
        Assert.That(solo.Travel.CurrentCityRuntimeId, Is.Null,
            "An en-route solo traveler with no current City remains between Cities until arrival.");
        Assert.That(solo.Travel.DestinationCityRuntimeId, Is.EqualTo(soloDestination.RuntimeId));
        Assert.That(solo.SpatialKnowledge.Revision,
            Is.EqualTo(dailyBoundary.SoloSpatialKnowledgeRevisionBeforeTravel),
            "The non-arriving solo result does not discover or mutate spatial knowledge.");
        Assert.That(store.ActiveParties, Is.Empty);
        Assert.That(store.Revision, Is.EqualTo(dailyBoundary.PartyStoreRevisionBeforeTravel + 1));
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            long expectedDelta = ReferenceEquals(city, partyDestination) ? 1L : 0L;
            Assert.That(city.NpcPresence.Revision,
                Is.EqualTo(dailyBoundary.CityPresenceRevisionsBeforeTravel[city.RuntimeId] + expectedDelta),
                "Arrival adds the member only to its exact destination because origin presence was already removed at departure.");
        }
        Assert.That(partyOrigin.NpcPresence.Contains(partyMember.RuntimeId), Is.False);
        Assert.That(partyDestination.NpcPresence.Contains(partyMember.RuntimeId), Is.True);
        Assert.That(order.IndexOf("party-operation-enter"), Is.LessThan(order.IndexOf("party-event")));
        Assert.That(order.IndexOf("party-event"), Is.LessThan(order.IndexOf("party-operation-exit")));
        Assert.That(order.IndexOf("party-operation-exit"), Is.LessThan(order.IndexOf("solo-result")));
        Assert.That(order.IndexOf("actor-turns"), Is.LessThan(order.IndexOf("travel-context")));
        Assert.That(order.IndexOf("travel-context"), Is.LessThan(order.IndexOf("party-operation-enter")));
        Assert.That(order.IndexOf("solo-result"), Is.LessThan(order.IndexOf("results-published")));
        Assert.That(order.IndexOf("results-published"), Is.LessThan(order.IndexOf("travel-observation")));
        Assert.That(order, Does.Not.Contain("travel-and-observe"),
            "The Core daily stage invokes the travel operation directly instead of the former Unity travel callback.");
        Assert.That(boundary.PublishedTransitions, Is.EqualTo(new[]
        {
            $"{partyMember.RuntimeId}:True",
            $"{partyMember.RuntimeId}:False",
            $"{solo.RuntimeId}:False"
        }), "Committed Core transitions publish the existing Unity/P12 notifications in owner order.");
    }

    private sealed class RecordingTravelStageBoundary : IDailyV1SelectedTravelDayBoundary
    {
        private readonly List<string> order;
        internal List<string> PublishedTransitions { get; } = new List<string>();

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
            PublishedTransitions.Add(npc.RuntimeId + ":" + arriving);
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

    private sealed class RecordingDailyExecutionBoundary
        : IDailyV1SelectedDailyExecutionBoundary
    {
        private readonly List<string> order;
        private readonly RecordingTravelExecutionContext travelContext;
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        private readonly DailyV1NpcRuntimeOwners partyMember;
        private readonly DailyV1NpcRuntimeOwners solo;

        internal long PartySpatialKnowledgeRevisionBeforeTravel { get; private set; }
        internal long SoloSpatialKnowledgeRevisionBeforeTravel { get; private set; }
        internal bool PartyKnewDestinationBeforeTravel { get; private set; }
        internal long PartyStoreRevisionBeforeTravel { get; private set; }
        internal Dictionary<string, long> CityPresenceRevisionsBeforeTravel { get; } =
            new Dictionary<string, long>(StringComparer.Ordinal);

        internal RecordingDailyExecutionBoundary(
            List<string> order,
            RecordingTravelExecutionContext travelContext,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners partyMember,
            DailyV1NpcRuntimeOwners solo)
        {
            this.order = order;
            this.travelContext = travelContext;
            this.graph = graph;
            this.partyMember = partyMember;
            this.solo = solo;
        }

        public void BeginLoggerDay(long absoluteDay) => order.Add("logger:" + absoluteDay);
        public bool TryCreateSelectedDemographyContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDemographyExecutionContext context)
        {
            context = null;
            return false;
        }
        public void PublishSelectedDemographyResult(DailyV1SelectedDemographyResult result) { }
        public bool TryCreateSelectedDayStartContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDayStartExecutionContext context)
        {
            context = null;
            return false;
        }
        public void BeginSimulationDay() => order.Add("begin-day");
        public void EnsureSelectedEconomyCanRun(DailyV1SelectedRuntimeOwnerGraph graph) =>
            order.Add("economy-preflight");
        public void LogSelectedDailyEconomy(
            DailyV1CityRuntimeOwners owner,
            SimulationLogCategory category,
            string message) { }
        public void EnsureSelectedLocalKnowledgeCanRun(DailyV1SelectedRuntimeOwnerGraph graph) =>
            order.Add("knowledge-preflight");
        public bool TryCreateSelectedActorTurnContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedActorTurnExecutionContext context)
        {
            PartySpatialKnowledgeRevisionBeforeTravel = partyMember.SpatialKnowledge.Revision;
            SoloSpatialKnowledgeRevisionBeforeTravel = solo.SpatialKnowledge.Revision;
            PartyKnewDestinationBeforeTravel = partyMember.SpatialKnowledge.KnownLocationRuntimeIds
                .Contains(partyMember.Travel.DestinationLocationRuntimeId);
            PartyStoreRevisionBeforeTravel = travelContext.PartyStore.Revision;
            foreach (DailyV1CityRuntimeOwners city in graph.Cities)
                CityPresenceRevisionsBeforeTravel.Add(city.RuntimeId, city.NpcPresence.Revision);
            order.Add("actor-context");
            context = new NoOpActorTurnContext(graph);
            return true;
        }
        public bool TryCreateSelectedActorTurnBoundary(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1ActorTurnExecutionBoundary boundary)
        {
            boundary = new NoOpActorTurnBoundary();
            return graph != null;
        }

        public bool TryCreateSelectedTravelDayContext(
            out IDailyV1SelectedTravelDayExecutionContext context)
        {
            order.Add("travel-context");
            context = travelContext;
            return true;
        }

        private sealed class NoOpActorTurnContext : IDailyV1SelectedActorTurnExecutionContext
        {
            internal NoOpActorTurnContext(DailyV1SelectedRuntimeOwnerGraph graph)
            {
                RuntimeRoster = graph.Npcs.Select(npc => new DailyV1ActorTurnRosterProjection(
                    npc.RuntimeId, null)).ToList();
                PersonStore = graph.PersonStore;
                ActorChoiceStore = new ActorChoiceStore(PersonStore);
                Boundary = new NoOpActorTurnBoundary();
            }

            public IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
            public PersonStore PersonStore { get; }
            public ActorChoiceStore ActorChoiceStore { get; }
            public IDailyV1ActorTurnExecutionBoundary Boundary { get; }
        }

        private sealed class NoOpActorTurnBoundary : IDailyV1ActorTurnExecutionBoundary
        {
            public bool IsInSpatialTransit(DailyV1NpcRuntimeOwners actor, PersonId personId) => false;
            public bool IsReservedToday(DailyV1NpcRuntimeOwners actor) => false;
            public void ProcessScheduledDirective(DailyV1NpcRuntimeOwners actor, ScheduledDirective directive) { }
            public bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal) => false;
            public void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor) { }
            public void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor) { }
        }
    }

    private sealed class RecordingTravelExecutionContext
        : IDailyV1SelectedTravelDayExecutionContext
    {
        private readonly List<string> order;
        private readonly TravelPartyStore partyStore;
        private readonly IReadOnlyCollection<string> managedPartyMembers;
        private readonly RecordingTravelStageBoundary boundary;

        internal int PartyOperationCount { get; private set; }
        internal IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> PartyArrivals { get; private set; }
        internal IReadOnlyList<DailyV1SelectedSoloTravelDayResult> SoloResults { get; private set; }

        internal RecordingTravelExecutionContext(
            List<string> order,
            TravelPartyStore partyStore,
            IReadOnlyCollection<string> managedPartyMembers,
            RecordingTravelStageBoundary boundary,
            bool advanceSoloTravel)
        {
            this.order = order;
            this.partyStore = partyStore;
            this.managedPartyMembers = managedPartyMembers;
            this.boundary = boundary;
            AdvanceSoloTravel = advanceSoloTravel;
        }

        public TravelPartyStore PartyStore => partyStore;
        public IReadOnlyCollection<string> ManagedTravelPartyNpcRuntimeIds => managedPartyMembers;
        public IDailyV1SelectedTravelDayBoundary Boundary => boundary;
        public bool AdvanceSoloTravel { get; }

        public bool ExecuteWithinPartyOwnerOperation(Func<bool> operation)
        {
            PartyOperationCount++;
            order.Add("party-operation-enter");
            bool result = operation();
            order.Add("party-operation-exit");
            return result;
        }

        public void PublishCompletedTravelResults(
            IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
            IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults)
        {
            Assert.That(partyArrivals, Has.Count.EqualTo(1));
            Assert.That(soloResults, Has.Count.EqualTo(1));
            PartyArrivals = partyArrivals;
            SoloResults = soloResults;
            order.Add("results-published");
        }

        public void ObserveCompletedTravelResults() => order.Add("travel-observation");
    }
}
