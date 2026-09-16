using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WorldObserverReadModelTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void WorldObserverReadModelContainsMacroLocations()
    {
        ObserverFixture fixture = CreateFixture();
        WorldObserverReadModel model = fixture.Query.BuildReadModel();

        Assert.That(model.Locations, Has.Count.EqualTo(4));
        Assert.That(model.Locations, Has.Some.Matches<WorldObserverLocationReadModel>(x => x.RuntimeId == fixture.World.CityA.Location.RuntimeId));
        Assert.That(model.Locations, Has.Some.Matches<WorldObserverLocationReadModel>(x => x.RuntimeId == fixture.World.CityB.Location.RuntimeId));
    }

    [Test]
    public void ReadModelContainsRoutes()
    {
        ObserverFixture fixture = CreateFixture();

        Assert.That(fixture.Query.BuildReadModel().Routes, Has.Count.EqualTo(3));
        Assert.That(fixture.Query.BuildReadModel().Routes, Has.Some.Matches<WorldObserverRouteReadModel>(x => x.OriginLocationRuntimeId == fixture.World.CityA.Location.RuntimeId));
    }

    [Test]
    public void ReadModelContainsTravelingNpcProgress()
    {
        ObserverFixture fixture = CreateFixture();
        NpcRuntime traveler = fixture.World.CreateNpc("observer-traveler", fixture.World.CityA, 100f);
        fixture.Npcs.Add(traveler);
        fixture.EnsurePartySystem();
        fixture.Query = CreateQuery(fixture);
        traveler.SpatialKnowledge.DiscoverLocation(fixture.World.CityA.Location.RuntimeId);
        traveler.SpatialKnowledge.DiscoverLocation(fixture.World.Site.Location.RuntimeId);
        traveler.SpatialKnowledge.DiscoverRoute(fixture.World.SiteRoute.RuntimeId);
        ActionExecutionContext context = new ActionExecutionContext(
            "observer-travel",
            new[] { new ActionExecutionParticipant(traveler.RuntimeId, ActionExecutionParticipantRole.Performer) },
            fixture.World.Site.Location.RuntimeId,
            fixture.World.SiteRoute.RuntimeId);
        Assert.That(fixture.World.TravelPartySystem.TryStartTravelParty(context, out _), Is.True);

        WorldObserverTravelerReadModel travelerModel = FindTraveler(fixture.Query.BuildReadModel(), traveler.RuntimeId);
        Assert.That(travelerModel.IsTraveling, Is.True);
        Assert.That(travelerModel.DestinationLocationRuntimeId, Is.EqualTo(fixture.World.Site.Location.RuntimeId));
        Assert.That(travelerModel.RouteRuntimeId, Is.EqualTo(fixture.World.SiteRoute.RuntimeId));
        Assert.That(travelerModel.RemainingTravelDays, Is.GreaterThan(0));
    }

    [Test]
    public void StationaryNpcAppearsAtCurrentPlace()
    {
        ObserverFixture fixture = CreateFixture();
        NpcRuntime npc = fixture.World.CreateNpc("observer-stationary", fixture.World.CityA, 10f);
        fixture.Npcs.Add(npc);
        fixture.Query = CreateQuery(fixture);

        WorldObserverPlaceReadModel place = fixture.Query.BuildReadModel(fixture.World.CityA.RuntimeId).SelectedPlace;
        Assert.That(place.PresentNpcRuntimeIds, Contains.Item(npc.RuntimeId));
    }

    [Test]
    public void SelectedCityShowsPresentNpc()
    {
        ObserverFixture fixture = CreateFixture();
        NpcRuntime npc = fixture.World.CreateNpc("observer-city-npc", fixture.World.CityA, 10f);
        fixture.Npcs.Add(npc);
        fixture.Query = CreateQuery(fixture);

        WorldObserverReadModel model = fixture.Query.BuildReadModel(fixture.World.CityA.RuntimeId);
        Assert.That(model.SelectedPlace.RuntimeId, Is.EqualTo(fixture.World.CityA.RuntimeId));
        Assert.That(model.SelectedPlace.PresentNpcRuntimeIds, Contains.Item(npc.RuntimeId));
    }

    [Test]
    public void SelectedSiteShowsSiteState()
    {
        ObserverFixture fixture = CreateFixture();
        fixture.ContentStore.GetOrCreate(fixture.World.Site);

        WorldObserverPlaceReadModel place = fixture.Query.BuildReadModel(fixture.World.Site.RuntimeId).SelectedPlace;
        Assert.That(place.HasSiteState, Is.True);
        Assert.That(place.SiteState, Is.EqualTo(PlaceSiteState.Cleared));
    }

    [Test]
    public void SelectedSiteShowsOppositionSummary()
    {
        ObserverFixture fixture = CreateFixture();
        PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("observer-opposition", "Watchers");
        opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("watchers", 5f, 3));
        Assert.That(fixture.ContentStore.TryAddOpposition(fixture.World.Site, opposition, out _), Is.True);

        WorldObserverPlaceReadModel place = fixture.Query.BuildReadModel(fixture.World.Site.RuntimeId).SelectedPlace;
        Assert.That(place.Oppositions, Has.Count.EqualTo(1));
        Assert.That(place.Oppositions[0].DisplayName, Is.EqualTo("Watchers"));
        Assert.That(place.Oppositions[0].AggregateParticipantCount, Is.EqualTo(1));
    }

    [Test]
    public void SelectedPlaceShowsPersistentContentSummary()
    {
        ObserverFixture fixture = CreateFixture();
        ItemData ore = SimulationTestFactory.CreateItem("observer-ore");
        NotableItemRuntime relic = new NotableItemRuntime("observer-relic", SimulationTestFactory.CreateItem("observer-relic-item"));
        fixture.ContentStore.TryAddStack(fixture.World.Site, ore, 4, PlaceContentPersistencePolicy.Durable, out _);
        fixture.ContentStore.TryAddNotable(fixture.World.Site, relic, out _);

        WorldObserverPlaceReadModel place = fixture.Query.BuildReadModel(fixture.World.Site.RuntimeId).SelectedPlace;
        Assert.That(place.Content, Has.Count.EqualTo(2));
        Assert.That(place.Content, Has.Some.Matches<WorldObserverContentReadModel>(x => x.PersistencePolicy == PlaceContentPersistencePolicy.Notable));
        Assert.That(place.Content, Has.Some.Matches<WorldObserverContentReadModel>(x => x.Amount == 4));
    }

    [Test]
    public void OwnerWithTopologyProvidesTopologyNodes()
    {
        ObserverFixture fixture = CreateFixtureWithTopology();

        WorldObserverPlaceReadModel place = fixture.Query.BuildReadModel(fixture.World.Site.RuntimeId).SelectedPlace;
        Assert.That(place.TopologyNodes, Has.Count.EqualTo(2));
        Assert.That(FindNode(place, fixture.Root.RuntimeId), Is.Not.Null);
    }

    [Test]
    public void OwnerWithoutTopologyReturnsNoFabricatedTopology()
    {
        ObserverFixture fixture = CreateFixture();

        WorldObserverPlaceReadModel place = fixture.Query.BuildReadModel(fixture.World.Site.RuntimeId).SelectedPlace;
        Assert.That(place.TopologyNodes, Is.Empty);
        Assert.That(place.TopologyConnections, Is.Empty);
    }

    [Test]
    public void TopologyHierarchyPreservesVariableDepth()
    {
        ObserverFixture fixture = CreateFixtureWithTopology(includeGrandchild: true);
        WorldObserverPlaceReadModel place = fixture.Query.BuildReadModel(fixture.World.Site.RuntimeId).SelectedPlace;

        WorldObserverTopologyNodeReadModel root = FindNode(place, fixture.Root.RuntimeId);
        WorldObserverTopologyNodeReadModel child = FindNode(place, fixture.Child.RuntimeId);
        WorldObserverTopologyNodeReadModel grandchild = FindNode(place, fixture.Grandchild.RuntimeId);
        Assert.That(root.Depth, Is.EqualTo(0));
        Assert.That(child.Depth, Is.EqualTo(1));
        Assert.That(grandchild.Depth, Is.EqualTo(2));
        Assert.That(grandchild.ParentRuntimeId, Is.EqualTo(fixture.Child.RuntimeId));
    }

    [Test]
    public void TopologyConnectionsAreRepresentedSeparatelyFromContainment()
    {
        ObserverFixture fixture = CreateFixtureWithTopology();
        WorldObserverPlaceReadModel place = fixture.Query.BuildReadModel(fixture.World.Site.RuntimeId).SelectedPlace;

        Assert.That(place.TopologyNodes, Has.Count.EqualTo(2));
        Assert.That(place.TopologyConnections, Has.Count.EqualTo(1));
        Assert.That(place.TopologyConnections[0].OriginRuntimeId, Is.EqualTo(fixture.Root.RuntimeId));
        Assert.That(place.TopologyConnections[0].DestinationRuntimeId, Is.EqualTo(fixture.Child.RuntimeId));
    }

    [Test]
    public void ExpeditionExplorationProgressAppearsInReadModel()
    {
        ObserverFixture fixture = CreateFixture();
        NpcRuntime npc = fixture.World.CreateNpc("observer-expedition-npc", fixture.World.CityA, 10f);
        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "observer-expedition",
            fixture.World.Site.RuntimeId,
            fixture.World.CityA.Location.RuntimeId,
            fixture.World.Site.Location.RuntimeId,
            fixture.World.SiteRoute.RuntimeId,
            "observer-party",
            null,
            new[] { npc.RuntimeId },
            new[] { npc.RuntimeId },
            Array.Empty<string>(),
            ExpeditionState.Exploring,
            ExpeditionObjectiveRuntime.Explore(3));
        expedition.TryAdvanceAbstractProgress(2);
        Assert.That(fixture.Expeditions.Add(expedition), Is.True);

        WorldObserverExpeditionReadModel model = fixture.Query.BuildReadModel().Expeditions[0];
        Assert.That(model.Progress, Is.EqualTo(2));
        Assert.That(model.RequiredProgress, Is.EqualTo(3));
        Assert.That(model.State, Is.EqualTo(ExpeditionState.Exploring));
    }

    [Test]
    public void RecentDomainEventsAppearInActivityFeed()
    {
        ObserverFixture fixture = CreateFixture();
        Assert.That(fixture.Records.EventRecorder.Record((eventId, day, sequence) => new NpcArrivedEvent(
            eventId, day, sequence, "observer-event-npc", fixture.World.CityA.Location.RuntimeId)), Is.True);

        WorldObserverReadModel model = fixture.Query.BuildReadModel();
        Assert.That(model.ActivityFeed, Has.Count.EqualTo(1));
        Assert.That(model.ActivityFeed[0].EventType, Is.EqualTo(DomainEventType.NpcArrived));
    }

    [Test]
    public void BuildingReadModelDoesNotMutateWorld()
    {
        ObserverFixture fixture = CreateFixture();
        int contentCountBefore = fixture.ContentStore.Places.Count;
        int topologyCountBefore = fixture.TopologyStore.Topologies.Count;

        fixture.Query.BuildReadModel(fixture.World.Site.RuntimeId);

        Assert.That(fixture.ContentStore.Places.Count, Is.EqualTo(contentCountBefore));
        Assert.That(fixture.TopologyStore.Topologies.Count, Is.EqualTo(topologyCountBefore));
    }

    [Test]
    public void BuildingReadModelDoesNotMutateKnowledge()
    {
        ObserverFixture fixture = CreateFixtureWithTopology();
        int placeObservationsBefore = fixture.Member.LocalTopologyKnowledge.PlaceObservations.Count;
        int connectionObservationsBefore = fixture.Member.LocalTopologyKnowledge.ConnectionObservations.Count;

        fixture.Query.BuildReadModel(fixture.World.Site.RuntimeId);

        Assert.That(fixture.Member.LocalTopologyKnowledge.PlaceObservations.Count, Is.EqualTo(placeObservationsBefore));
        Assert.That(fixture.Member.LocalTopologyKnowledge.ConnectionObservations.Count, Is.EqualTo(connectionObservationsBefore));
    }

    [Test]
    public void BuildingReadModelDoesNotConsumeRng()
    {
        ObserverFixture fixture = CreateFixture();
        CountingRandomSource randomSource = new CountingRandomSource();
        fixture.Query.BuildReadModel(fixture.World.Site.RuntimeId);

        Assert.That(randomSource.Calls, Is.EqualTo(0));
    }

    [Test]
    public void UiUsesTmpTypes()
    {
        Assert.That(typeof(WorldObserverCanvasView).GetField("worldGraphText", BindingFlags.Instance | BindingFlags.NonPublic).FieldType, Is.EqualTo(typeof(TMP_Text)));
        Assert.That(typeof(WorldObserverCanvasView).GetField("selectedPlaceText", BindingFlags.Instance | BindingFlags.NonPublic).FieldType, Is.EqualTo(typeof(TMP_Text)));
        Assert.That(typeof(WorldObserverCanvasView).GetField("topologyText", BindingFlags.Instance | BindingFlags.NonPublic).FieldType, Is.EqualTo(typeof(TMP_Text)));
        Assert.That(typeof(WorldObserverCanvasView).GetField("activityFeedText", BindingFlags.Instance | BindingFlags.NonPublic).FieldType, Is.EqualTo(typeof(TMP_Text)));
        Assert.That(typeof(WorldObserverCanvasView).GetFields(BindingFlags.Instance | BindingFlags.NonPublic), Has.None.Matches<FieldInfo>(field => field.FieldType == typeof(Text)));
    }

    [Test]
    public void ObserverPrefabSmokeInitializesWithoutMissingReference()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/WorldObserver.prefab");
        Assert.That(prefab, Is.Not.Null);

        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        createdObjects.Add(root);
        WorldObserverDemoBootstrap bootstrap = root.GetComponent<WorldObserverDemoBootstrap>();
        Assert.That(bootstrap, Is.Not.Null);
        bootstrap.EnsureReady();

        Assert.That(bootstrap.ObserverView, Is.Not.Null);
        Assert.That(bootstrap.ObserverView.GetComponentsInChildren<TMP_Text>(), Has.Length.GreaterThanOrEqualTo(4));
        Assert.That(root.GetComponentInChildren<WorldObserverTimeController>(), Is.Not.Null);
    }

    [Test]
    public void AdvanceDayControllerAdvancesExplicitlyOnce()
    {
        GameObject root = new GameObject("WorldObserverTimeSmoke");
        createdObjects.Add(root);
        WorldObserverTimeController controller = root.AddComponent<WorldObserverTimeController>();
        int calls = 0;
        int days = 0;
        controller.SetAdvanceDaysCallback(value =>
        {
            calls++;
            days += value;
        });

        Assert.That(controller.AdvanceOneDay(), Is.True);
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(days, Is.EqualTo(1));
    }

    private ObserverFixture CreateFixture()
    {
        ObserverFixture fixture = new ObserverFixture(new SpatialTravelFixture());
        fixture.Query = CreateQuery(fixture);
        return fixture;
    }

    private ObserverFixture CreateFixtureWithTopology(bool includeGrandchild = false)
    {
        ObserverFixture fixture = CreateFixture();
        LocalTopologyRuntime topology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForExplorableSite(fixture.World.Site),
            fixture.World.IdentityRegistry);
        fixture.Root = new LocalPlaceRuntime("observer-root", "Entry");
        fixture.Child = new LocalPlaceRuntime("observer-child", "Chamber");
        Assert.That(topology.AddPlace(fixture.Root, null, true), Is.True);
        Assert.That(topology.AddPlace(fixture.Child, fixture.Root), Is.True);
        fixture.Connection = new LocalTopologyConnectionRuntime(
            "observer-connection", fixture.Root, fixture.Child, 1f);
        Assert.That(topology.AddConnection(fixture.Connection), Is.True);
        if (includeGrandchild)
        {
            fixture.Grandchild = new LocalPlaceRuntime("observer-grandchild", "Vault");
            Assert.That(topology.AddPlace(fixture.Grandchild, fixture.Child), Is.True);
        }

        Assert.That(fixture.TopologyStore.Add(topology), Is.True);
        fixture.Query = CreateQuery(fixture);
        return fixture;
    }

    private static WorldObserverQueryService CreateQuery(ObserverFixture fixture)
    {
        return new WorldObserverQueryService(
            new[] { fixture.World.CityA, fixture.World.CityB },
            fixture.Npcs,
            fixture.World.Network,
            fixture.World.Sites,
            fixture.Expeditions,
            fixture.World.TravelParties,
            fixture.TopologyStore,
            fixture.ContentStore,
            fixture.Records.Events);
    }

    private static WorldObserverTravelerReadModel FindTraveler(WorldObserverReadModel model, string runtimeId)
    {
        foreach (WorldObserverTravelerReadModel traveler in model.Travelers)
        {
            if (traveler.RuntimeId == runtimeId)
            {
                return traveler;
            }
        }

        return null;
    }

    private static WorldObserverTopologyNodeReadModel FindNode(WorldObserverPlaceReadModel place, string runtimeId)
    {
        foreach (WorldObserverTopologyNodeReadModel node in place.TopologyNodes)
        {
            if (node.RuntimeId == runtimeId)
            {
                return node;
            }
        }

        return null;
    }

    private sealed class ObserverFixture
    {
        public SpatialTravelFixture World { get; }
        public List<NpcRuntime> Npcs { get; } = new List<NpcRuntime>();
        public RecordFixture Records => World.Records;
        public ExpeditionStore Expeditions { get; } = new ExpeditionStore();
        public PlaceContentStore ContentStore { get; } = new PlaceContentStore();
        public LocalTopologyStore TopologyStore { get; }
        public WorldObserverQueryService Query { get; set; }
        public LocalPlaceRuntime Root { get; set; }
        public LocalPlaceRuntime Child { get; set; }
        public LocalPlaceRuntime Grandchild { get; set; }
        public LocalTopologyConnectionRuntime Connection { get; set; }
        public NpcRuntime Member { get; private set; }

        public ObserverFixture(SpatialTravelFixture world)
        {
            World = world;
            TopologyStore = new LocalTopologyStore(world.IdentityRegistry);
            Member = world.CreateNpc("observer-member", world.CityA, 10f);
            Npcs.Add(Member);
        }

        public void EnsurePartySystem()
        {
            if (World.TravelPartySystem != null)
            {
                return;
            }

            TravelPartySystem partySystem = new TravelPartySystem(
                new TravelPartyStore(),
                Records.Allocator,
                World.IdentityRegistry,
                World.Travel,
                Records.Time,
                Records.Sequence,
                Records.EventRecorder);
            World.SetTravelPartySystem(partySystem);
        }
    }

    private sealed class CountingRandomSource : IConflictRandomSource
    {
        public int Calls { get; private set; }

        public float NextUnit()
        {
            Calls++;
            return 0.5f;
        }
    }
}
