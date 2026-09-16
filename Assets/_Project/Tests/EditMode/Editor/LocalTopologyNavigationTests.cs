using System.Collections.Generic;
using NUnit.Framework;

public sealed class LocalTopologyNavigationTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void ShortestPathUsesConnectionsNotHierarchy()
    {
        LocalTopologyRuntime topology = CreateTopology(out _);
        LocalPlaceRuntime parent = AddPlace(topology, "parent");
        LocalPlaceRuntime child = AddPlace(topology, "child", parent);

        Assert.That(topology.TryFindShortestPath(parent.RuntimeId, child.RuntimeId, out _), Is.False);
    }

    [Test]
    public void DirectedConnectionsAreRespected()
    {
        LocalTopologyRuntime topology = CreateTopology(out _);
        LocalPlaceRuntime first = AddPlace(topology, "first");
        LocalPlaceRuntime second = AddPlace(topology, "second");
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("first-to-second", first, second, 1f)), Is.True);

        Assert.That(topology.TryFindShortestPath(first.RuntimeId, second.RuntimeId, out LocalTopologyPath forward), Is.True);
        Assert.That(forward.LocalConnectionRuntimeIds, Is.EqualTo(new[] { "first-to-second" }));
        Assert.That(topology.TryFindShortestPath(second.RuntimeId, first.RuntimeId, out _), Is.False);
    }

    [Test]
    public void TraversalCostChoosesCheaperPath()
    {
        LocalTopologyRuntime topology = CreateTopology(out _);
        LocalPlaceRuntime start = AddPlace(topology, "start");
        LocalPlaceRuntime expensive = AddPlace(topology, "expensive");
        LocalPlaceRuntime cheap = AddPlace(topology, "cheap");
        LocalPlaceRuntime destination = AddPlace(topology, "destination");
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("start-expensive", start, expensive, 5f)), Is.True);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("expensive-destination", expensive, destination, 5f)), Is.True);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("start-cheap", start, cheap, 1f)), Is.True);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("cheap-destination", cheap, destination, 1f)), Is.True);

        Assert.That(topology.TryFindShortestPath(start.RuntimeId, destination.RuntimeId, out LocalTopologyPath path), Is.True);
        Assert.That(path.LocalPlaceRuntimeIds, Is.EqualTo(new[] { "start", "cheap", "destination" }));
        Assert.That(path.TotalCost, Is.EqualTo(2f));
    }

    [Test]
    public void EqualCostPathsResolveDeterministically()
    {
        LocalTopologyRuntime topology = CreateTopology(out _);
        LocalPlaceRuntime start = AddPlace(topology, "start");
        LocalPlaceRuntime firstBranch = AddPlace(topology, "first-branch");
        LocalPlaceRuntime secondBranch = AddPlace(topology, "second-branch");
        LocalPlaceRuntime destination = AddPlace(topology, "destination");
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("start-first", start, firstBranch, 1f)), Is.True);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("first-destination", firstBranch, destination, 1f)), Is.True);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("start-second", start, secondBranch, 1f)), Is.True);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("second-destination", secondBranch, destination, 1f)), Is.True);

        for (int i = 0; i < 3; i++)
        {
            Assert.That(topology.TryFindShortestPath(start.RuntimeId, destination.RuntimeId, out LocalTopologyPath path), Is.True);
            Assert.That(path.LocalPlaceRuntimeIds, Is.EqualTo(new[] { "start", "first-branch", "destination" }));
            Assert.That(path.LocalConnectionRuntimeIds, Is.EqualTo(new[] { "start-first", "first-destination" }));
        }
    }

    [Test]
    public void DisconnectedNodesHaveNoPath()
    {
        LocalTopologyRuntime topology = CreateTopology(out _);
        LocalPlaceRuntime first = AddPlace(topology, "first");
        LocalPlaceRuntime second = AddPlace(topology, "second");

        Assert.That(topology.TryFindShortestPath(first.RuntimeId, second.RuntimeId, out _), Is.False);
    }

    [Test]
    public void PathQueryDoesNotMutateTopology()
    {
        LocalTopologyRuntime topology = CreateTopology(out _);
        LocalPlaceRuntime first = AddPlace(topology, "first");
        LocalPlaceRuntime second = AddPlace(topology, "second", first);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("first-to-second", first, second, 2f)), Is.True);
        int nodeCount = topology.NodeCount;
        int connectionCount = topology.ConnectionCount;
        int depth = topology.GetDepth(second);

        Assert.That(topology.TryFindShortestPath(first.RuntimeId, second.RuntimeId, out _), Is.True);
        Assert.That(topology.NodeCount, Is.EqualTo(nodeCount));
        Assert.That(topology.ConnectionCount, Is.EqualTo(connectionCount));
        Assert.That(topology.GetDepth(second), Is.EqualTo(depth));
        Assert.That(second.Parent, Is.SameAs(first));
    }

    [Test]
    public void BlueprintBuildsNestedDungeonShape()
    {
        ExplorableSiteRuntime site = CreateSite("dungeon-site", "macro-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterExplorableSite(site), Is.True);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyBlueprint blueprint = new LocalTopologyBlueprint(LocalTopologyOwnerReference.ForExplorableSite(site));
        blueprint.AddNode(new LocalTopologyBlueprintNode("entrance", "Entrance", isEntryPoint: true));
        blueprint.AddNode(new LocalTopologyBlueprintNode("level-1", "Level 1"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("room-a", "Room A", parentLocalKey: "level-1"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("room-b", "Room B", parentLocalKey: "level-1"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("level-2", "Level 2"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("crypt", "Crypt", parentLocalKey: "level-2"));
        blueprint.AddConnection(new LocalTopologyBlueprintConnection("entrance", "level-1", 1f));
        blueprint.AddConnection(new LocalTopologyBlueprintConnection("level-1", "room-a", 1f));
        blueprint.AddConnection(new LocalTopologyBlueprintConnection("level-1", "level-2", 2f));
        blueprint.AddConnection(new LocalTopologyBlueprintConnection("level-2", "crypt", 1f));

        LocalTopologyBuilder builder = new LocalTopologyBuilder(new RuntimeIdAllocator(), registry, store);
        Assert.That(builder.TryBuild(blueprint, out LocalTopologyRuntime topology, out string diagnostic), Is.True, diagnostic);
        Assert.That(topology.NodeCount, Is.EqualTo(6));
        Assert.That(topology.EntryPointCount, Is.EqualTo(1));
        Assert.That(topology.TryGetPlace("local-place-000006", out LocalPlaceRuntime crypt), Is.True);
        Assert.That(topology.GetDepth(crypt), Is.EqualTo(1));
        Assert.That(topology.TryFindShortestPath("local-place-000001", "local-place-000006", out LocalTopologyPath path), Is.True);
        Assert.That(path.TotalCost, Is.EqualTo(4f));
    }

    [Test]
    public void BlueprintBuildsCityShapeUsingSameRuntime()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-owner", "macro-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterCity(city), Is.True);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyBlueprint blueprint = new LocalTopologyBlueprint(LocalTopologyOwnerReference.ForCity(city));
        blueprint.AddNode(new LocalTopologyBlueprintNode("market-district", "Market District"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("noble-district", "Noble District"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("castle", "Castle", parentLocalKey: "noble-district"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("keep", "Keep", parentLocalKey: "castle"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("throne-room", "Throne Room", parentLocalKey: "keep"));

        LocalTopologyBuilder builder = new LocalTopologyBuilder(new RuntimeIdAllocator(), registry, store);
        Assert.That(builder.TryBuild(blueprint, out LocalTopologyRuntime topology, out string diagnostic), Is.True, diagnostic);
        Assert.That(topology.Owner.OwnerKind, Is.EqualTo(LocalTopologyOwnerKind.City));
        Assert.That(topology, Is.TypeOf<LocalTopologyRuntime>());
        Assert.That(topology.GetDepth(topology.Places[4]), Is.EqualTo(3));
    }

    [Test]
    public void InvalidBlueprintDoesNotPartiallyRegisterRuntimeObjects()
    {
        ExplorableSiteRuntime site = CreateSite("site-owner", "macro-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterExplorableSite(site), Is.True);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyBlueprint blueprint = new LocalTopologyBlueprint(LocalTopologyOwnerReference.ForExplorableSite(site));
        blueprint.AddNode(new LocalTopologyBlueprintNode("entrance", "Entrance"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("entrance", "Duplicate"));
        blueprint.AddConnection(new LocalTopologyBlueprintConnection("entrance", "missing", 1f));

        LocalTopologyBuilder builder = new LocalTopologyBuilder(new RuntimeIdAllocator(), registry, store);
        Assert.That(builder.TryBuild(blueprint, out LocalTopologyRuntime topology, out _), Is.False);
        Assert.That(topology, Is.Null);
        Assert.That(store.Topologies, Is.Empty);
        Assert.That(registry.IsRuntimeIdAvailable("local-place-000001"), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable("local-connection-000001"), Is.True);
    }

    [Test]
    public void BlueprintRejectsMissingParent()
    {
        LocalTopologyBlueprint blueprint = CreateBlueprintWithSite();
        blueprint.AddNode(new LocalTopologyBlueprintNode("child", "Child", parentLocalKey: "missing"));

        AssertBuildRejected(blueprint, "missing parent");
    }

    [Test]
    public void BlueprintRejectsHierarchyCycle()
    {
        LocalTopologyBlueprint blueprint = CreateBlueprintWithSite();
        blueprint.AddNode(new LocalTopologyBlueprintNode("first", "First", parentLocalKey: "second"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("second", "Second", parentLocalKey: "first"));

        AssertBuildRejected(blueprint, "cycle");
    }

    [Test]
    public void BlueprintRejectsUnknownConnectionEndpoint()
    {
        LocalTopologyBlueprint blueprint = CreateBlueprintWithSite();
        blueprint.AddNode(new LocalTopologyBlueprintNode("first", "First"));
        blueprint.AddConnection(new LocalTopologyBlueprintConnection("first", "missing", 1f));

        AssertBuildRejected(blueprint, "known");
    }

    [Test]
    public void BlueprintRejectsDuplicateLocalKey()
    {
        LocalTopologyBlueprint blueprint = CreateBlueprintWithSite();
        blueprint.AddNode(new LocalTopologyBlueprintNode("same", "First"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("same", "Second"));

        AssertBuildRejected(blueprint, "duplicate local key");
    }

    [Test]
    public void BlueprintMaterializationAllocatesStableRuntimeIds()
    {
        ExplorableSiteRuntime firstSite = CreateSite("first-site", "macro-one");
        ExplorableSiteRuntime secondSite = CreateSite("second-site", "macro-two");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterExplorableSite(firstSite), Is.True);
        Assert.That(registry.RegisterExplorableSite(secondSite), Is.True);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        LocalTopologyBuilder builder = new LocalTopologyBuilder(allocator, registry, store);

        LocalTopologyBlueprint firstBlueprint = new LocalTopologyBlueprint(LocalTopologyOwnerReference.ForExplorableSite(firstSite));
        firstBlueprint.AddNode(new LocalTopologyBlueprintNode("entrance"));
        firstBlueprint.AddNode(new LocalTopologyBlueprintNode("room"));
        firstBlueprint.AddConnection(new LocalTopologyBlueprintConnection("entrance", "room", 1f));
        Assert.That(builder.TryBuild(firstBlueprint, out LocalTopologyRuntime firstTopology, out string firstDiagnostic), Is.True, firstDiagnostic);

        LocalTopologyBlueprint secondBlueprint = new LocalTopologyBlueprint(LocalTopologyOwnerReference.ForExplorableSite(secondSite));
        secondBlueprint.AddNode(new LocalTopologyBlueprintNode("entrance"));
        Assert.That(builder.TryBuild(secondBlueprint, out LocalTopologyRuntime secondTopology, out string secondDiagnostic), Is.True, secondDiagnostic);

        Assert.That(firstTopology.Places[0].RuntimeId, Is.EqualTo("local-place-000001"));
        Assert.That(firstTopology.Places[1].RuntimeId, Is.EqualTo("local-place-000002"));
        Assert.That(firstTopology.Connections[0].RuntimeId, Is.EqualTo("local-connection-000001"));
        Assert.That(secondTopology.Places[0].RuntimeId, Is.EqualTo("local-place-000003"));
    }

    [Test]
    public void BlueprintMutationAfterBuildDoesNotMutateWorldTruth()
    {
        LocalTopologyBlueprint blueprint = CreateBlueprintWithSite();
        LocalTopologyBlueprintNode node = new LocalTopologyBlueprintNode("room", "Original");
        LocalTopologyBlueprintConnection connection = new LocalTopologyBlueprintConnection("room", "other", 2f);
        blueprint.AddNode(node);
        blueprint.AddNode(new LocalTopologyBlueprintNode("other", "Other"));
        blueprint.AddConnection(connection);
        LocalTopologyStore store = GetStoreForBlueprint(blueprint, out RuntimeIdentityRegistry registry);
        LocalTopologyBuilder builder = new LocalTopologyBuilder(new RuntimeIdAllocator(), registry, store);
        Assert.That(builder.TryBuild(blueprint, out LocalTopologyRuntime topology, out string diagnostic), Is.True, diagnostic);

        node.DisplayName = "Changed";
        node.ParentLocalKey = "other";
        connection.TraversalCost = 99f;
        blueprint.AddNode(new LocalTopologyBlueprintNode("added-later", "Added later"));

        Assert.That(topology.Places.Count, Is.EqualTo(2));
        Assert.That(topology.Places[0].DisplayName, Is.EqualTo("Original"));
        Assert.That(topology.Places[0].Parent, Is.Null);
        Assert.That(topology.Connections[0].TraversalCost, Is.EqualTo(2f));
    }

    [Test]
    public void DerivedNodeConnectionDepthCountsMatchTruth()
    {
        LocalTopologyRuntime topology = CreateTopology(out _);
        LocalPlaceRuntime root = AddPlace(topology, "root", isEntryPoint: true);
        LocalPlaceRuntime child = AddPlace(topology, "child", root);
        LocalPlaceRuntime grandchild = AddPlace(topology, "grandchild", child);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("root-child", root, child, 1f)), Is.True);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime("child-grandchild", child, grandchild, 2f)), Is.True);

        Assert.That(topology.NodeCount, Is.EqualTo(3));
        Assert.That(topology.ConnectionCount, Is.EqualTo(2));
        Assert.That(topology.EntryPointCount, Is.EqualTo(1));
        Assert.That(topology.MaxDepth, Is.EqualTo(2));
    }

    private static void AssertBuildRejected(LocalTopologyBlueprint blueprint, string expectedDiagnostic)
    {
        LocalTopologyStore store = GetStoreForBlueprint(blueprint, out RuntimeIdentityRegistry registry);
        LocalTopologyBuilder builder = new LocalTopologyBuilder(new RuntimeIdAllocator(), registry, store);

        Assert.That(builder.TryBuild(blueprint, out _, out string diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain(expectedDiagnostic));
        Assert.That(store.Topologies, Is.Empty);
        Assert.That(registry.IsRuntimeIdAvailable("local-place-000001"), Is.True);
    }

    private static LocalTopologyBlueprint CreateBlueprintWithSite()
    {
        ExplorableSiteRuntime site = CreateSite("site-owner", "macro-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterExplorableSite(site), Is.True);
        return new LocalTopologyBlueprint(LocalTopologyOwnerReference.ForExplorableSite(site));
    }

    private static LocalTopologyStore GetStoreForBlueprint(
        LocalTopologyBlueprint blueprint,
        out RuntimeIdentityRegistry registry)
    {
        registry = new RuntimeIdentityRegistry();
        if (blueprint.Owner.OwnerKind == LocalTopologyOwnerKind.ExplorableSite)
        {
            ExplorableSiteRuntime site = CreateSite(
                blueprint.Owner.OwnerRuntimeId,
                blueprint.Owner.MacroLocationRuntimeId);
            Assert.That(registry.RegisterExplorableSite(site), Is.True);
        }

        return new LocalTopologyStore(registry);
    }

    private static LocalTopologyRuntime CreateTopology(out CityRuntime city)
    {
        city = SimulationTestFactory.CreateCity("city-owner", "macro-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterCity(city), Is.True);
        return new LocalTopologyRuntime(LocalTopologyOwnerReference.ForCity(city), registry);
    }

    private static LocalPlaceRuntime AddPlace(
        LocalTopologyRuntime topology,
        string runtimeId,
        LocalPlaceRuntime parent = null,
        bool isEntryPoint = false)
    {
        LocalPlaceRuntime place = new LocalPlaceRuntime(runtimeId, runtimeId + " display");
        Assert.That(topology.AddPlace(place, parent, isEntryPoint), Is.True);
        return place;
    }

    private static ExplorableSiteRuntime CreateSite(string runtimeId, string locationRuntimeId)
    {
        return new ExplorableSiteRuntime(
            runtimeId,
            SimulationTestFactory.CreateExplorableSite("definition-" + runtimeId),
            new SpatialLocationRuntime(locationRuntimeId));
    }
}
