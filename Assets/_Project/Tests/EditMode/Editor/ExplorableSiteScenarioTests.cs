using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ExplorableSiteScenarioTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject simulationObject in simulationObjects)
        {
            if (simulationObject != null)
            {
                Object.DestroyImmediate(simulationObject);
            }
        }

        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void Scenario_CreatesConfiguredSiteLocationRegistryEntryAndRoutes()
    {
        CityData anchorDefinition = SimulationTestFactory.CreateCityData("anchor-city");
        ExplorableSiteData siteDefinition = SimulationTestFactory.CreateExplorableSite("ruin-definition", ExplorableSiteKind.Ruin);
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(anchorDefinition);
        config.ExplorableSites.Add(new ExplorableSiteConfig
        {
            site = siteDefinition,
            anchorCity = anchorDefinition,
            travelDaysFromAnchor = 3
        });

        TesteSimulacao simulation = CreateSimulation(config);

        Assert.That(simulation.ExplorableSites, Is.Not.Null);
        Assert.That(simulation.ExplorableSites.Sites.Count, Is.EqualTo(1));
        ExplorableSiteRuntime site = simulation.ExplorableSites.Sites[0];
        Assert.That(simulation.TryGetExplorableSiteRuntime(site.RuntimeId, out ExplorableSiteRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(site));
        Assert.That(simulation.TryGetSpatialLocation(site.Location.RuntimeId, out SpatialLocationRuntime location), Is.True);
        Assert.That(location, Is.SameAs(site.Location));
        Assert.That(simulation.SpatialNetwork.Locations, Has.Member(site.Location));
        Assert.That(site.RuntimeId, Is.Not.EqualTo(site.Location.RuntimeId));

        Assert.That(simulation.TryGetCityRuntime("city-000001", out CityRuntime anchor), Is.True);
        Assert.That(simulation.SpatialNetwork.TryGetSingleDirectRoute(
            anchor.Location,
            site.Location,
            out SpatialRouteRuntime anchorToSite), Is.True);
        Assert.That(anchorToSite.TravelDays, Is.EqualTo(3));
        Assert.That(simulation.SpatialNetwork.TryGetSingleDirectRoute(
            site.Location,
            anchor.Location,
            out SpatialRouteRuntime siteToAnchor), Is.True);
        Assert.That(siteToAnchor.TravelDays, Is.EqualTo(3));
        Assert.That(simulation.SpatialNetwork.GetOutgoingRoutes(anchor.Location).Count, Is.EqualTo(1));
        Assert.That(simulation.SpatialNetwork.GetOutgoingRoutes(site.Location).Count, Is.EqualTo(1));
    }

    [Test]
    public void Scenario_IsolatedSiteWithoutSelectedAnchorRejectsWholeBootstrap()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        ExplorableSiteData siteDefinition = SimulationTestFactory.CreateExplorableSite("isolated-definition", ExplorableSiteKind.Cave);
        config.ExplorableSites.Add(new ExplorableSiteConfig
        {
            site = siteDefinition,
            anchorCity = null,
            travelDaysFromAnchor = 4
        });

        AssertBootstrapRejected(config);
    }

    [Test]
    public void Scenario_DuplicateSiteOutputIdentityRejectsWholeBootstrap()
    {
        CityData anchorDefinition = SimulationTestFactory.CreateCityData("anchor-city");
        ExplorableSiteData sharedDefinition = SimulationTestFactory.CreateExplorableSite("shared-definition");
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(anchorDefinition);
        config.ExplorableSites.Add(new ExplorableSiteConfig
        {
            site = sharedDefinition,
            anchorCity = anchorDefinition,
            travelDaysFromAnchor = 1
        });
        config.ExplorableSites.Add(new ExplorableSiteConfig
        {
            site = sharedDefinition,
            anchorCity = anchorDefinition,
            travelDaysFromAnchor = 2
        });

        AssertBootstrapRejected(config);
    }

    [Test]
    public void Scenario_InvalidConfigurationsDoNotCreateCorruptSiteEntries()
    {
        CityData anchorDefinition = SimulationTestFactory.CreateCityData("anchor-city");
        CityData missingAnchorDefinition = SimulationTestFactory.CreateCityData("missing-anchor");
        ExplorableSiteData validDefinition = SimulationTestFactory.CreateExplorableSite("valid-definition");
        ExplorableSiteData invalidDefinition = SimulationTestFactory.CreateExplorableSite(" ");
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(anchorDefinition);
        config.ExplorableSites.Add(null);
        config.ExplorableSites.Add(new ExplorableSiteConfig());
        config.ExplorableSites.Add(new ExplorableSiteConfig { site = invalidDefinition });
        config.ExplorableSites.Add(new ExplorableSiteConfig
        {
            site = validDefinition,
            anchorCity = missingAnchorDefinition,
            travelDaysFromAnchor = 2
        });

        AssertBootstrapRejected(config);
    }

    [Test]
    public void Scenario_SiteCreationDoesNotAlterEconomyOrNpcState()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-scenario", 12f);
        CityData anchorDefinition = SimulationTestFactory.CreateCityData(
            "anchor-city",
            SimulationTestFactory.CreateMarketItem(item, 8, 10));
        NpcData npcDefinition = SimulationTestFactory.CreateNpc("scenario-npc");
        ExplorableSiteData siteDefinition = SimulationTestFactory.CreateExplorableSite("site-definition");
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(anchorDefinition);
        config.Npcs.Add(new NpcSimulationConfig
        {
            npc = npcDefinition,
            startingCity = anchorDefinition,
            initialMoney = 77f
        });
        config.ExplorableSites.Add(new ExplorableSiteConfig
        {
            site = siteDefinition,
            anchorCity = anchorDefinition,
            travelDaysFromAnchor = 1
        });

        CityRuntime expectedCity = new CityRuntime(
            "expected-city",
            anchorDefinition,
            new SpatialLocationRuntime("expected-location"));
        float expectedPrice = expectedCity.Market.GetPrice(item);

        TesteSimulacao simulation = CreateSimulation(config);

        Assert.That(simulation.TryGetCityRuntime("city-000001", out CityRuntime city), Is.True);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(8));
        Assert.That(city.Market.GetPrice(item), Is.EqualTo(expectedPrice));
        Assert.That(simulation.TryGetNpcRuntime("npc-000001", out NpcRuntime npc), Is.True);
        Assert.That(npc.CurrentCity, Is.SameAs(city));
        Assert.That(npc.Money, Is.EqualTo(77f));
        Assert.That(npc.CurrentActionRuntime, Is.Null);
        Assert.That(npc.CurrentStatus, Is.Empty);
    }

    [Test]
    public void Scenario_SiteCreationDoesNotAutomaticallyAddSiteOrLocationKnowledge()
    {
        CityData anchorDefinition = SimulationTestFactory.CreateCityData("anchor-city");
        NpcData npcDefinition = SimulationTestFactory.CreateNpc("scenario-npc");
        ExplorableSiteData siteDefinition = SimulationTestFactory.CreateExplorableSite("unknown-site");
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(anchorDefinition);
        config.Npcs.Add(new NpcSimulationConfig
        {
            npc = npcDefinition,
            startingCity = anchorDefinition
        });
        config.ExplorableSites.Add(new ExplorableSiteConfig
        {
            site = siteDefinition,
            anchorCity = anchorDefinition
        });

        TesteSimulacao simulation = CreateSimulation(config);
        ExplorableSiteRuntime site = simulation.ExplorableSites.Sites[0];
        Assert.That(simulation.TryGetNpcRuntime("npc-000001", out NpcRuntime npc), Is.True);

        Assert.That(npc.SpatialKnowledge.KnowsLocation(site.Location.RuntimeId), Is.False);
        Assert.That(npc.SpatialKnowledge.KnowsRoute("route-000001"), Is.False);
    }

    [Test]
    public void Scenario_SiteLocationAndRoutesUseGlobalDistinctRuntimeIds()
    {
        CityData anchorDefinition = SimulationTestFactory.CreateCityData("anchor-city");
        ExplorableSiteData siteDefinition = SimulationTestFactory.CreateExplorableSite("site-definition");
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(anchorDefinition);
        config.ExplorableSites.Add(new ExplorableSiteConfig
        {
            site = siteDefinition,
            anchorCity = anchorDefinition
        });

        TesteSimulacao simulation = CreateSimulation(config);
        HashSet<string> runtimeIds = new HashSet<string>();
        foreach (ExplorableSiteRuntime site in simulation.ExplorableSites.Sites)
        {
            Assert.That(runtimeIds.Add(site.RuntimeId), Is.True);
        }

        foreach (SpatialLocationRuntime location in simulation.SpatialNetwork.Locations)
        {
            Assert.That(runtimeIds.Add(location.RuntimeId), Is.True);
        }

        foreach (SpatialRouteRuntime route in simulation.SpatialNetwork.Routes)
        {
            Assert.That(runtimeIds.Add(route.RuntimeId), Is.True);
        }
    }

    private TesteSimulacao CreateSimulation(SimulationConfigData config)
    {
        GameObject simulationObject = new GameObject("explorable-site-scenario-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        FieldInfo configField = typeof(TesteSimulacao).GetField(
            "simulationConfig",
            BindingFlags.Instance | BindingFlags.NonPublic);
        configField.SetValue(simulation, config);
        simulation.Start();
        return simulation;
    }

    private void AssertBootstrapRejected(SimulationConfigData config)
    {
        GameObject simulationObject = new GameObject("invalid-explorable-site-bootstrap");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);
        Assert.Throws<System.InvalidOperationException>(() => simulation.Start());
        Assert.That(simulation.Bootstrap, Is.Null);
        Assert.That(simulation.Runtime, Is.Null);
        Assert.That(simulation.History, Is.Null);
    }
}
