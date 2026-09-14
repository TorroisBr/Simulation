using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ExplorableSiteKnowledgeTests
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
                UnityEngine.Object.DestroyImmediate(simulationObject);
            }
        }

        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void NpcRuntime_StartsWithOwnedEmptyExplorableSiteKnowledge()
    {
        NpcRuntime npc = new NpcRuntime("npc-knowledge", SimulationTestFactory.CreateNpc("knowledge"));

        Assert.That(npc.ExplorableSiteKnowledge, Is.Not.Null);
        Assert.That(npc.ExplorableSiteKnowledge.OwnerRuntimeId, Is.EqualTo(npc.RuntimeId));
        Assert.That(npc.ExplorableSiteKnowledge.Observations, Is.Empty);
        Assert.That(npc.ExplorableSiteKnowledge.KnowsSite("unknown-site"), Is.False);
    }

    [Test]
    public void WorldTruthSiteDoesNotAutomaticallyEnterNpcKnowledge()
    {
        ExplorableSiteRuntime site = CreateSite("site-truth", "location-truth");
        ExplorableSiteStore store = new ExplorableSiteStore();
        NpcRuntime npc = new NpcRuntime("npc-knowledge", SimulationTestFactory.CreateNpc("knowledge"));

        Assert.That(store.Add(site), Is.True);
        Assert.That(npc.ExplorableSiteKnowledge.KnowsSite(site.RuntimeId), Is.False);
        Assert.That(npc.SpatialKnowledge.KnowsLocation(site.Location.RuntimeId), Is.False);
    }

    [Test]
    public void KnowledgeSystemRecordsInitialSiteAndOnlyItsLocation()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ExplorableSiteRuntime site = CreateSite("site-initial", "location-initial");
        NpcRuntime npc = new NpcRuntime("npc-knowledge", SimulationTestFactory.CreateNpc("knowledge"));
        ExplorableSiteKnowledgeSystem system = new ExplorableSiteKnowledgeSystem();

        Assert.That(system.RecordInitialScenarioKnowledge(npc, site, records.Time.AbsoluteDay), Is.True);
        Assert.That(npc.ExplorableSiteKnowledge.TryGetObservation(
            site.RuntimeId,
            out ExplorableSiteKnowledgeObservation observation), Is.True);
        Assert.That(observation.Source, Is.EqualTo(ExplorableSiteKnowledgeSource.InitialScenarioKnowledge));
        Assert.That(observation.SiteRuntimeId, Is.EqualTo(site.RuntimeId));
        Assert.That(observation.LocationRuntimeId, Is.EqualTo(site.Location.RuntimeId));
        Assert.That(npc.SpatialKnowledge.KnowsLocation(site.Location.RuntimeId), Is.True);
        Assert.That(npc.SpatialKnowledge.KnowsRoute("route-to-site"), Is.False);
        SimulationInvariantValidator.ValidateExplorableSiteKnowledgeObservation(observation);
    }

    [Test]
    public void ScenarioBootstrapAddsConfiguredKnowledgeWithoutLeakingRoute()
    {
        CityData anchorDefinition = SimulationTestFactory.CreateCityData("anchor-city");
        ExplorableSiteData siteDefinition = SimulationTestFactory.CreateExplorableSite("known-site");
        NpcData npcDefinition = SimulationTestFactory.CreateNpc("known-npc");
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(anchorDefinition);
        config.Npcs.Add(new NpcSimulationConfig
        {
            npc = npcDefinition,
            startingCity = anchorDefinition,
            initialKnownExplorableSites = new List<ExplorableSiteData> { siteDefinition }
        });
        config.ExplorableSites.Add(new ExplorableSiteConfig
        {
            site = siteDefinition,
            anchorCity = anchorDefinition,
            travelDaysFromAnchor = 2
        });

        TesteSimulacao simulation = CreateSimulation(config);
        ExplorableSiteRuntime site = simulation.ExplorableSites.Sites[0];
        Assert.That(simulation.TryGetNpcRuntime("npc-000001", out NpcRuntime npc), Is.True);
        Assert.That(npc.ExplorableSiteKnowledge.TryGetObservation(
            site.RuntimeId,
            out ExplorableSiteKnowledgeObservation observation), Is.True);
        Assert.That(observation.Source, Is.EqualTo(ExplorableSiteKnowledgeSource.InitialScenarioKnowledge));
        Assert.That(observation.ObservedDay, Is.EqualTo(0L));
        Assert.That(npc.SpatialKnowledge.KnowsLocation(site.Location.RuntimeId), Is.True);

        Assert.That(simulation.TryGetCityRuntime("city-000001", out CityRuntime anchor), Is.True);
        Assert.That(simulation.SpatialNetwork.TryGetSingleDirectRoute(
            anchor.Location,
            site.Location,
            out SpatialRouteRuntime route), Is.True);
        Assert.That(npc.SpatialKnowledge.KnowsRoute(route.RuntimeId), Is.False);
    }

    [Test]
    public void ScenarioAdvanceDayDoesNotPerformOmniscientSiteDiscovery()
    {
        CityData anchorDefinition = SimulationTestFactory.CreateCityData("anchor-city");
        ExplorableSiteData siteDefinition = SimulationTestFactory.CreateExplorableSite("unknown-site");
        NpcData npcDefinition = SimulationTestFactory.CreateNpc("unknown-npc");
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
        simulation.Runtime.AdvanceDay();

        Assert.That(npc.ExplorableSiteKnowledge.Observations, Is.Empty);
        Assert.That(npc.SpatialKnowledge.KnowsLocation(site.Location.RuntimeId), Is.False);
    }

    [Test]
    public void ExplorableSiteKnowledge_QueryUnknownDoesNotMutate()
    {
        ExplorableSiteKnowledgeRuntime knowledge = new ExplorableSiteKnowledgeRuntime("npc-knowledge");

        Assert.That(knowledge.TryGetObservation("missing", out _), Is.False);
        Assert.That(knowledge.KnowsSite("missing"), Is.False);
        Assert.That(knowledge.Observations, Is.Empty);
    }

    [Test]
    public void ExplorableSiteKnowledge_NewerObservationReplacesOlder()
    {
        ExplorableSiteKnowledgeRuntime knowledge = new ExplorableSiteKnowledgeRuntime("npc-knowledge");
        ExplorableSiteKnowledgeObservation older = CreateObservation(
            "site-knowledge",
            "location-old",
            2L,
            2L,
            ExplorableSiteKnowledgeSource.InitialScenarioKnowledge);
        ExplorableSiteKnowledgeObservation newer = CreateObservation(
            "site-knowledge",
            "location-new",
            3L,
            5L,
            ExplorableSiteKnowledgeSource.InitialScenarioKnowledge);

        Assert.That(knowledge.RecordObservation(older), Is.True);
        Assert.That(knowledge.RecordObservation(newer), Is.True);
        Assert.That(knowledge.TryGetObservation("site-knowledge", out ExplorableSiteKnowledgeObservation current), Is.True);
        Assert.That(current, Is.SameAs(newer));
    }

    [Test]
    public void ExplorableSiteKnowledge_OlderObservationDoesNotReplaceNewer()
    {
        ExplorableSiteKnowledgeRuntime knowledge = new ExplorableSiteKnowledgeRuntime("npc-knowledge");
        ExplorableSiteKnowledgeObservation newer = CreateObservation(
            "site-knowledge",
            "location-new",
            3L,
            3L,
            ExplorableSiteKnowledgeSource.InitialScenarioKnowledge);
        ExplorableSiteKnowledgeObservation older = CreateObservation(
            "site-knowledge",
            "location-old",
            2L,
            4L,
            ExplorableSiteKnowledgeSource.DirectObservation);

        Assert.That(knowledge.RecordObservation(newer), Is.True);
        Assert.That(knowledge.RecordObservation(older), Is.False);
        Assert.That(knowledge.TryGetObservation("site-knowledge", out ExplorableSiteKnowledgeObservation current), Is.True);
        Assert.That(current, Is.SameAs(newer));
    }

    [Test]
    public void ExplorableSiteKnowledge_DirectObservationWinsSameDayTie()
    {
        ExplorableSiteKnowledgeRuntime knowledge = new ExplorableSiteKnowledgeRuntime("npc-knowledge");
        ExplorableSiteKnowledgeObservation initial = CreateObservation(
            "site-knowledge",
            "location-initial",
            4L,
            4L,
            ExplorableSiteKnowledgeSource.InitialScenarioKnowledge);
        ExplorableSiteKnowledgeObservation direct = CreateObservation(
            "site-knowledge",
            "location-direct",
            4L,
            6L,
            ExplorableSiteKnowledgeSource.DirectObservation);

        Assert.That(knowledge.RecordObservation(initial), Is.True);
        Assert.That(knowledge.RecordObservation(direct), Is.True);
        Assert.That(knowledge.TryGetObservation("site-knowledge", out ExplorableSiteKnowledgeObservation current), Is.True);
        Assert.That(current, Is.SameAs(direct));
    }

    [Test]
    public void ExplorableSiteKnowledge_EquivalentObservationIsDeterministicNoOp()
    {
        ExplorableSiteKnowledgeRuntime knowledge = new ExplorableSiteKnowledgeRuntime("npc-knowledge");
        ExplorableSiteKnowledgeObservation first = CreateObservation(
            "site-knowledge",
            "location-knowledge",
            4L,
            5L,
            ExplorableSiteKnowledgeSource.DirectObservation);
        ExplorableSiteKnowledgeObservation equivalent = CreateObservation(
            "site-knowledge",
            "location-knowledge",
            4L,
            5L,
            ExplorableSiteKnowledgeSource.DirectObservation);

        Assert.That(knowledge.RecordObservation(first), Is.True);
        Assert.That(knowledge.RecordObservation(equivalent), Is.False);
        Assert.That(knowledge.Observations.Count, Is.EqualTo(1));
        Assert.That(knowledge.Observations[0], Is.SameAs(first));
    }

    [TestCase(null, "location", 0L, 0L, ExplorableSiteKnowledgeSource.DirectObservation)]
    [TestCase("", "location", 0L, 0L, ExplorableSiteKnowledgeSource.DirectObservation)]
    [TestCase(" ", "location", 0L, 0L, ExplorableSiteKnowledgeSource.DirectObservation)]
    [TestCase("site", null, 0L, 0L, ExplorableSiteKnowledgeSource.DirectObservation)]
    [TestCase("site", "", 0L, 0L, ExplorableSiteKnowledgeSource.DirectObservation)]
    [TestCase("site", " ", 0L, 0L, ExplorableSiteKnowledgeSource.DirectObservation)]
    public void ExplorableSiteKnowledgeObservation_RejectsInvalidIds(
        string siteRuntimeId,
        string locationRuntimeId,
        long observedDay,
        long receivedDay,
        ExplorableSiteKnowledgeSource source)
    {
        Assert.Throws<ArgumentException>(() => new ExplorableSiteKnowledgeObservation(
            siteRuntimeId,
            locationRuntimeId,
            observedDay,
            receivedDay,
            source));
    }

    [Test]
    public void ExplorableSiteKnowledgeObservation_RejectsInvalidDays()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateObservation(
            "site",
            "location",
            -1L,
            0L,
            ExplorableSiteKnowledgeSource.DirectObservation));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateObservation(
            "site",
            "location",
            2L,
            1L,
            ExplorableSiteKnowledgeSource.DirectObservation));
    }

    [Test]
    public void Scenario_AmbiguousDefinitionDoesNotSilentlyChooseFirstRuntime()
    {
        CityData anchorDefinition = SimulationTestFactory.CreateCityData("anchor-city");
        ExplorableSiteData sharedDefinition = SimulationTestFactory.CreateExplorableSite("ambiguous-site");
        NpcData npcDefinition = SimulationTestFactory.CreateNpc("ambiguous-npc");
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(anchorDefinition);
        config.Npcs.Add(new NpcSimulationConfig
        {
            npc = npcDefinition,
            startingCity = anchorDefinition,
            initialKnownExplorableSites = new List<ExplorableSiteData> { sharedDefinition }
        });
        config.ExplorableSites.Add(new ExplorableSiteConfig { site = sharedDefinition, anchorCity = anchorDefinition });
        config.ExplorableSites.Add(new ExplorableSiteConfig { site = sharedDefinition, anchorCity = anchorDefinition });
        LogAssert.Expect(
            LogType.Error,
            "Explorable site definition 'ambiguous-site' is ambiguous: 2 runtime instances exist. Resolve by RuntimeId instead.");

        TesteSimulacao simulation = CreateSimulation(config);

        Assert.That(simulation.ExplorableSites.Sites.Count, Is.EqualTo(2));
        Assert.That(simulation.TryGetNpcRuntime("npc-000001", out NpcRuntime npc), Is.True);
        Assert.That(npc.ExplorableSiteKnowledge.Observations, Is.Empty);
    }

    [Test]
    public void Scenario_SiteKnowledgeBelongsToOneNpc()
    {
        CityData anchorDefinition = SimulationTestFactory.CreateCityData("anchor-city");
        ExplorableSiteData siteDefinition = SimulationTestFactory.CreateExplorableSite("private-site");
        NpcData firstNpcDefinition = SimulationTestFactory.CreateNpc("first-npc");
        NpcData secondNpcDefinition = SimulationTestFactory.CreateNpc("second-npc");
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(anchorDefinition);
        config.Npcs.Add(new NpcSimulationConfig
        {
            npc = firstNpcDefinition,
            startingCity = anchorDefinition,
            initialKnownExplorableSites = new List<ExplorableSiteData> { siteDefinition }
        });
        config.Npcs.Add(new NpcSimulationConfig
        {
            npc = secondNpcDefinition,
            startingCity = anchorDefinition
        });
        config.ExplorableSites.Add(new ExplorableSiteConfig { site = siteDefinition, anchorCity = anchorDefinition });

        TesteSimulacao simulation = CreateSimulation(config);
        ExplorableSiteRuntime site = simulation.ExplorableSites.Sites[0];
        Assert.That(simulation.TryGetNpcRuntime("npc-000001", out NpcRuntime firstNpc), Is.True);
        Assert.That(simulation.TryGetNpcRuntime("npc-000002", out NpcRuntime secondNpc), Is.True);

        Assert.That(firstNpc.ExplorableSiteKnowledge.KnowsSite(site.RuntimeId), Is.True);
        Assert.That(secondNpc.ExplorableSiteKnowledge.KnowsSite(site.RuntimeId), Is.False);
        Assert.That(secondNpc.SpatialKnowledge.KnowsLocation(site.Location.RuntimeId), Is.False);
    }

    [Test]
    public void KnowledgeMutationDoesNotMutateExplorableSiteTruth()
    {
        ExplorableSiteData definition = SimulationTestFactory.CreateExplorableSite("truth-definition", ExplorableSiteKind.Dungeon, "Truth Display");
        SpatialLocationRuntime location = new SpatialLocationRuntime("truth-location");
        ExplorableSiteRuntime site = new ExplorableSiteRuntime("truth-site", definition, location);
        NpcRuntime npc = new NpcRuntime("npc-knowledge", SimulationTestFactory.CreateNpc("knowledge"));
        ExplorableSiteKnowledgeSystem system = new ExplorableSiteKnowledgeSystem();

        Assert.That(system.RecordDirectObservation(npc, site, 7L), Is.True);

        Assert.That(site.RuntimeId, Is.EqualTo("truth-site"));
        Assert.That(site.Definition, Is.SameAs(definition));
        Assert.That(site.DefinitionId, Is.EqualTo("truth-definition"));
        Assert.That(site.Location, Is.SameAs(location));
        Assert.That(site.Location.RuntimeId, Is.EqualTo("truth-location"));
    }

    private ExplorableSiteRuntime CreateSite(string runtimeId, string locationRuntimeId)
    {
        return new ExplorableSiteRuntime(
            runtimeId,
            SimulationTestFactory.CreateExplorableSite("definition-" + runtimeId),
            new SpatialLocationRuntime(locationRuntimeId));
    }

    private static ExplorableSiteKnowledgeObservation CreateObservation(
        string siteRuntimeId,
        string locationRuntimeId,
        long observedDay,
        long receivedDay,
        ExplorableSiteKnowledgeSource source)
    {
        return new ExplorableSiteKnowledgeObservation(
            siteRuntimeId,
            locationRuntimeId,
            observedDay,
            receivedDay,
            source);
    }

    private TesteSimulacao CreateSimulation(SimulationConfigData config)
    {
        GameObject simulationObject = new GameObject("explorable-site-knowledge-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        FieldInfo configField = typeof(TesteSimulacao).GetField(
            "simulationConfig",
            BindingFlags.Instance | BindingFlags.NonPublic);
        configField.SetValue(simulation, config);
        simulation.Start();
        return simulation;
    }
}
