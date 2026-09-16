using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WorldObserverVisualTests
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
    public void ObserverPrefabContainsCanvas()
    {
        GameObject prefab = LoadPrefab();

        Assert.That(prefab.GetComponentInChildren<Canvas>(true), Is.Not.Null);
        Assert.That(prefab.GetComponentInChildren<GraphicRaycaster>(true), Is.Not.Null);
    }

    [Test]
    public void ObserverPrefabContainsActualAdvanceDayButton()
    {
        WorldObserverCanvasView view = LoadPrefab().GetComponentInChildren<WorldObserverCanvasView>(true);

        Assert.That(view, Is.Not.Null);
        Assert.That(view.AdvanceDayButton, Is.Not.Null);
        Assert.That(view.AdvanceDayButton.name, Is.EqualTo("AdvanceDayButton"));
    }

    [Test]
    public void AdvanceDayButtonUsesTmpLabel()
    {
        Button button = LoadPrefab().GetComponentInChildren<WorldObserverCanvasView>(true).AdvanceDayButton;

        Assert.That(button.GetComponentInChildren<TMP_Text>(true), Is.Not.Null);
        Assert.That(button.GetComponentInChildren<Text>(true), Is.Null);
    }

    [Test]
    public void ObserverPrefabContainsWorldGraphPanel()
    {
        WorldObserverCanvasView view = LoadPrefab().GetComponentInChildren<WorldObserverCanvasView>(true);

        Assert.That(view.WorldGraphPanel, Is.Not.Null);
        Assert.That(view.WorldGraphPanel.name, Is.EqualTo("WorldGraphPanel"));
    }

    [Test]
    public void WorldGraphCreatesOneVisualForExpectedLocations()
    {
        VisualFixture fixture = CreateVisualFixture();

        Assert.That(fixture.View.WorldNodes, Has.Count.EqualTo(6));
        Assert.That(CountSiteNodes(fixture.View), Is.EqualTo(2));
    }

    [Test]
    public void WorldGraphCreatesRouteVisuals()
    {
        VisualFixture fixture = CreateVisualFixture();

        Assert.That(fixture.View.RouteViews, Has.Count.EqualTo(3));
        Assert.That(fixture.View.RouteViews, Has.Some.Matches<WorldObserverRouteView>(
            route => route.RouteRuntimeId == fixture.World.SiteRoute.RuntimeId));
    }

    [Test]
    public void ClickingWorldNodeSelectsCorrectRuntimeId()
    {
        VisualFixture fixture = CreateVisualFixture();
        WorldObserverWorldNodeView cityNode = FindWorldNodeBySelection(fixture.View, fixture.World.CityA.RuntimeId);

        cityNode.Button.onClick.Invoke();

        Assert.That(fixture.View.SelectedPlaceRuntimeId, Is.EqualTo(fixture.World.CityA.RuntimeId));
    }

    [Test]
    public void TravelerMarkerPositionUsesProgress01()
    {
        VisualFixture fixture = CreateVisualFixture(withTraveler: true);
        WorldObserverTravelerMarkerView marker = fixture.View.TravelerMarkers[0];
        WorldObserverWorldNodeView origin = FindWorldNode(fixture.View, fixture.World.CityA.Location.RuntimeId);
        WorldObserverWorldNodeView destination = FindWorldNode(fixture.View, fixture.World.Site.Location.RuntimeId);
        Vector2 expected = Vector2.Lerp(origin.AnchoredPosition, destination.AnchoredPosition, marker.Progress01);

        Assert.That(Vector2.Distance(marker.AnchoredPosition, expected), Is.LessThan(0.001f));
    }

    [Test]
    public void TravelerMarkerIsAttachedToCorrectRoute()
    {
        VisualFixture fixture = CreateVisualFixture(withTraveler: true);

        Assert.That(fixture.View.TravelerMarkers, Has.Count.EqualTo(1));
        Assert.That(fixture.View.TravelerMarkers[0].RouteRuntimeId, Is.EqualTo(fixture.World.SiteRoute.RuntimeId));
    }

    [Test]
    public void SelectedPlacePanelRefreshesAfterNodeClick()
    {
        VisualFixture fixture = CreateVisualFixture();
        WorldObserverWorldNodeView siteNode = FindWorldNodeBySelection(fixture.View, fixture.World.Site.RuntimeId);

        siteNode.Button.onClick.Invoke();

        Assert.That(fixture.View.SelectedPlaceTitle.text, Is.EqualTo(fixture.World.Site.Definition.DisplayName));
        Assert.That(fixture.View.DetailRows, Has.Some.Matches<WorldObserverDetailRowView>(
            row => row.Label.text.Contains(fixture.World.Site.RuntimeId)));
    }

    [Test]
    public void TopologyViewCreatesVisualLocalNodes()
    {
        VisualFixture fixture = CreateVisualFixture();

        fixture.View.SelectPlace(fixture.World.Site.RuntimeId);

        Assert.That(fixture.View.TopologyNodes, Has.Count.EqualTo(3));
    }

    [Test]
    public void TopologyHierarchySupportsVariableDepth()
    {
        VisualFixture fixture = CreateVisualFixture();
        fixture.View.SelectPlace(fixture.World.Site.RuntimeId);
        WorldObserverTopologyNodeView root = FindLocalNode(fixture.View, fixture.Root.RuntimeId);
        WorldObserverTopologyNodeView child = FindLocalNode(fixture.View, fixture.Child.RuntimeId);
        WorldObserverTopologyNodeView grandchild = FindLocalNode(fixture.View, fixture.Grandchild.RuntimeId);

        Assert.That(root.Depth, Is.Zero);
        Assert.That(child.Depth, Is.EqualTo(1));
        Assert.That(grandchild.Depth, Is.EqualTo(2));
        Assert.That(root.AnchoredPosition.y, Is.GreaterThan(child.AnchoredPosition.y));
        Assert.That(child.AnchoredPosition.y, Is.GreaterThan(grandchild.AnchoredPosition.y));
    }

    [Test]
    public void TopologyConnectionsHaveVisualEdges()
    {
        VisualFixture fixture = CreateVisualFixture();
        fixture.View.SelectPlace(fixture.World.Site.RuntimeId);

        Assert.That(fixture.View.TopologyEdges, Has.Some.Matches<WorldObserverTopologyEdgeView>(
            edge => edge.EdgeKind == WorldObserverTopologyEdgeKind.Connection
                && edge.OriginRuntimeId == fixture.Root.RuntimeId
                && edge.DestinationRuntimeId == fixture.Child.RuntimeId));
        Assert.That(fixture.View.TopologyEdges, Has.Some.Matches<WorldObserverTopologyEdgeView>(
            edge => edge.EdgeKind == WorldObserverTopologyEdgeKind.Hierarchy));
    }

    [Test]
    public void ClickingLocalNodeSelectsLocalPlace()
    {
        VisualFixture fixture = CreateVisualFixture();
        fixture.View.SelectPlace(fixture.World.Site.RuntimeId);
        WorldObserverTopologyNodeView child = FindLocalNode(fixture.View, fixture.Child.RuntimeId);

        child.Button.onClick.Invoke();

        Assert.That(fixture.View.SelectedPlaceRuntimeId, Is.EqualTo(fixture.Child.RuntimeId));
        Assert.That(fixture.View.SelectedPlaceTitle.text, Is.EqualTo(fixture.Child.DisplayName));
    }

    [Test]
    public void BreadcrumbCanReturnFromTopologyToWorld()
    {
        VisualFixture fixture = CreateVisualFixture();
        fixture.View.SelectPlace(fixture.World.Site.RuntimeId);
        Assert.That(fixture.View.IsTopologyVisible, Is.True);

        fixture.View.TopologyBackButton.onClick.Invoke();

        Assert.That(fixture.View.IsTopologyVisible, Is.False);
        Assert.That(fixture.View.TopologyPanel.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void ActivityFeedCreatesStructuredRows()
    {
        VisualFixture fixture = CreateVisualFixture(addActivity: true);

        Assert.That(fixture.View.ActivityRows, Has.Count.EqualTo(1));
        Assert.That(fixture.View.ActivityRows[0].EventType, Is.EqualTo(DomainEventType.NpcArrived));
        Assert.That(fixture.View.ActivityRows[0].Label, Is.Not.Null);
    }

    [Test]
    public void AdvanceDayButtonAdvancesExactlyOneDayAndRefreshes()
    {
        VisualFixture fixture = CreateVisualFixture();
        long before = fixture.Records.Time.AbsoluteDay;

        fixture.View.AdvanceDayButton.onClick.Invoke();

        Assert.That(fixture.Records.Time.AbsoluteDay, Is.EqualTo(before + 1L));
        Assert.That(fixture.View.DayText.text, Is.EqualTo("Day " + (before + 1L)));
    }

    [Test]
    public void ObserverUiContainsNoUnityEngineUiText()
    {
        GameObject prefab = LoadPrefab();

        Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
    }

    [Test]
    public void ObserverUiUsesTMPForEveryTextualComponent()
    {
        GameObject prefab = LoadPrefab();
        TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);

        Assert.That(texts.Length, Is.GreaterThanOrEqualTo(6));
        Assert.That(prefab.GetComponentInChildren<WorldObserverCanvasView>(true).AdvanceDayButton.GetComponentInChildren<TMP_Text>(true), Is.Not.Null);
        Assert.That(prefab.GetComponentInChildren<WorldObserverCanvasView>(true).TopologyBackButton.GetComponentInChildren<TMP_Text>(true), Is.Not.Null);
    }

    [Test]
    public void ObserverVisualElementsDoNotDefaultToSameRectPosition()
    {
        WorldObserverCanvasView view = LoadPrefab().GetComponentInChildren<WorldObserverCanvasView>(true);
        HashSet<Vector2> panelPositions = new HashSet<Vector2>
        {
            view.WorldGraphPanel.anchoredPosition,
            view.DetailPanel.anchoredPosition,
            view.TopologyPanel.anchoredPosition,
            view.ActivityPanel.anchoredPosition
        };

        Assert.That(panelPositions.Count, Is.GreaterThan(1));
        Assert.That(view.WorldGraphPanel.anchorMin, Is.Not.EqualTo(view.DetailPanel.anchorMin));
    }

    [Test]
    public void ObserverPrefabSmokeHasNoMissingSerializedReferences()
    {
        GameObject prefab = LoadPrefab();
        WorldObserverDemoBootstrap bootstrap = prefab.GetComponent<WorldObserverDemoBootstrap>();
        WorldObserverCanvasView view = prefab.GetComponentInChildren<WorldObserverCanvasView>(true);

        Assert.That(bootstrap, Is.Not.Null);
        Assert.That(view, Is.Not.Null);
        Assert.That(bootstrap.TimeController, Is.Not.Null);
        Assert.That(view.DayText, Is.Not.Null);
        Assert.That(view.AdvanceDayButton, Is.Not.Null);
        Assert.That(view.WorldGraphPanel, Is.Not.Null);
        Assert.That(view.DetailPanel, Is.Not.Null);
        Assert.That(view.TopologyPanel, Is.Not.Null);
        Assert.That(view.ActivityPanel, Is.Not.Null);
    }

    [Test]
    public void ObserverSceneLoadsWithoutMissingReference()
    {
        Scene scene = EditorSceneManager.OpenScene(WorldObserverAssetBuilder.ScenePath, OpenSceneMode.Single);
        WorldObserverDemoBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldObserverDemoBootstrap>();

        Assert.That(scene.IsValid(), Is.True);
        Assert.That(bootstrap, Is.Not.Null);
        Assert.That(bootstrap.ObserverView, Is.Not.Null);
        Assert.That(bootstrap.ObserverView.AdvanceDayButton, Is.Not.Null);
    }

    private GameObject LoadPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldObserverAssetBuilder.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        return prefab;
    }

    private VisualFixture CreateVisualFixture(bool withTraveler = false, bool addActivity = false)
    {
        GameObject prefab = LoadPrefab();
        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        createdObjects.Add(root);
        VisualFixture fixture = new VisualFixture(root);
        if (withTraveler)
        {
            fixture.StartTravelerHalfway();
        }

        if (addActivity)
        {
            fixture.Records.EventRecorder.Record((eventId, day, sequence) => new NpcArrivedEvent(
                eventId,
                day,
                sequence,
                fixture.Member.RuntimeId,
                fixture.World.CityA.Location.RuntimeId));
        }

        fixture.RefreshQuery();
        return fixture;
    }

    private static int CountSiteNodes(WorldObserverCanvasView view)
    {
        int count = 0;
        foreach (WorldObserverWorldNodeView node in view.WorldNodes)
        {
            if (node.IsSiteNode)
            {
                count++;
            }
        }

        return count;
    }

    private static WorldObserverWorldNodeView FindWorldNode(WorldObserverCanvasView view, string runtimeId)
    {
        foreach (WorldObserverWorldNodeView node in view.WorldNodes)
        {
            if (node.RuntimeId == runtimeId)
            {
                return node;
            }
        }

        return null;
    }

    private static WorldObserverWorldNodeView FindWorldNodeBySelection(WorldObserverCanvasView view, string runtimeId)
    {
        foreach (WorldObserverWorldNodeView node in view.WorldNodes)
        {
            if (node.SelectionRuntimeId == runtimeId)
            {
                return node;
            }
        }

        return null;
    }

    private static WorldObserverTopologyNodeView FindLocalNode(WorldObserverCanvasView view, string runtimeId)
    {
        foreach (WorldObserverTopologyNodeView node in view.TopologyNodes)
        {
            if (node.RuntimeId == runtimeId)
            {
                return node;
            }
        }

        return null;
    }

    private sealed class VisualFixture
    {
        public SpatialTravelFixture World { get; }
        public RecordFixture Records => World.Records;
        public List<NpcRuntime> Npcs { get; } = new List<NpcRuntime>();
        public NpcRuntime Member { get; }
        public LocalTopologyStore Topologies { get; }
        public PlaceContentStore Content { get; }
        public ExpeditionStore Expeditions { get; } = new ExpeditionStore();
        public LocalPlaceRuntime Root { get; }
        public LocalPlaceRuntime Child { get; }
        public LocalPlaceRuntime Grandchild { get; }
        public WorldObserverDemoBootstrap Bootstrap { get; }
        public WorldObserverCanvasView View => Bootstrap.ObserverView;

        public VisualFixture(GameObject root)
        {
            World = new SpatialTravelFixture();
            Member = World.CreateNpc("observer-visual-member", World.CityA, 100f);
            Npcs.Add(Member);
            Topologies = new LocalTopologyStore(World.IdentityRegistry);
            LocalTopologyRuntime topology = new LocalTopologyRuntime(
                LocalTopologyOwnerReference.ForExplorableSite(World.Site),
                World.IdentityRegistry);
            Root = new LocalPlaceRuntime("observer-visual-root", "Gate");
            Child = new LocalPlaceRuntime("observer-visual-child", "Crypt");
            Grandchild = new LocalPlaceRuntime("observer-visual-grandchild", "Vault");
            topology.AddPlace(Root, null, true);
            topology.AddPlace(Child, Root);
            topology.AddPlace(Grandchild, Child);
            topology.AddConnection(new LocalTopologyConnectionRuntime("observer-visual-root-child", Root, Child, 1f));
            Topologies.Add(topology);
            Content = new PlaceContentStore(World.Records.Allocator, World.IdentityRegistry);
            Content.GetOrCreate(World.Site);
            Bootstrap = root.GetComponent<WorldObserverDemoBootstrap>();
            Bootstrap.EnsureReady();
            WorldObserverTimeController controller = Bootstrap.TimeController;
            controller.SetAdvanceDaysCallback(days =>
            {
                for (int i = 0; i < days; i++)
                {
                    Records.Time.AdvanceDay();
                }
            });
        }

        public void StartTravelerHalfway()
        {
            NpcRuntime traveler = World.CreateNpc("observer-visual-traveler", World.CityA, 100f);
            Npcs.Add(traveler);
            Assert.That(World.Travel.TryStartTravel(traveler, World.Site.Location, null), Is.True);
            traveler.ClearTravelStartedToday();
            World.Travel.AdvanceTravels(Npcs);
        }

        public void RefreshQuery()
        {
            WorldObserverQueryService query = new WorldObserverQueryService(
                new[] { World.CityA, World.CityB },
                Npcs,
                World.Network,
                World.Sites,
                Expeditions,
                World.TravelParties,
                Topologies,
                Content,
                Records.Events,
                Records.Time);
            View.Initialize(query, Bootstrap.TimeController);
        }
    }
}
