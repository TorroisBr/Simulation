using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class LocalTopologyFoundationTests
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
    public void CityCanOwnOptionalLocalTopology()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-owner", "macro-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterCity(city), Is.True);

        LocalTopologyStore store = new LocalTopologyStore(registry);
        Assert.That(store.TryGetTopologyForOwner(city.RuntimeId, out _), Is.False);

        LocalTopologyRuntime topology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForCity(city),
            registry);
        LocalPlaceRuntime market = new LocalPlaceRuntime("local-place-market", "Market");
        Assert.That(topology.AddPlace(market, isEntryPoint: true), Is.True);

        Assert.That(store.Add(topology), Is.True);
        Assert.That(store.TryGetTopologyForOwner(city.RuntimeId, out LocalTopologyRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(topology));
        Assert.That(resolved.Owner.MacroLocationRuntimeId, Is.EqualTo(city.Location.RuntimeId));
    }

    [Test]
    public void ExplorableSiteCanOwnOptionalLocalTopology()
    {
        ExplorableSiteRuntime site = CreateSite("site-owner", "macro-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterExplorableSite(site), Is.True);

        LocalTopologyRuntime topology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForExplorableSite(site),
            registry);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime("local-place-entrance"), isEntryPoint: true), Is.True);

        LocalTopologyStore store = new LocalTopologyStore(registry);
        Assert.That(store.Add(topology), Is.True);
        Assert.That(store.TryGetTopologyForOwner(site.RuntimeId, out LocalTopologyRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(topology));
    }

    [Test]
    public void TwoSitesAtSameMacroLocationCanHaveIndependentTopologies()
    {
        ExplorableSiteRuntime firstSite = CreateSite("site-one", "shared-macro-location");
        ExplorableSiteRuntime secondSite = CreateSite("site-two", "shared-macro-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterExplorableSite(firstSite), Is.True);
        Assert.That(registry.RegisterExplorableSite(secondSite), Is.True);
        LocalTopologyStore store = new LocalTopologyStore(registry);

        LocalTopologyRuntime firstTopology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForExplorableSite(firstSite), registry);
        LocalTopologyRuntime secondTopology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForExplorableSite(secondSite), registry);
        Assert.That(firstTopology.AddPlace(new LocalPlaceRuntime("first-entry")), Is.True);
        Assert.That(secondTopology.AddPlace(new LocalPlaceRuntime("second-entry")), Is.True);

        Assert.That(store.Add(firstTopology), Is.True);
        Assert.That(store.Add(secondTopology), Is.True);
        Assert.That(store.TryGetTopologyForOwner(firstSite.RuntimeId, out LocalTopologyRuntime resolvedFirst), Is.True);
        Assert.That(store.TryGetTopologyForOwner(secondSite.RuntimeId, out LocalTopologyRuntime resolvedSecond), Is.True);
        Assert.That(resolvedFirst, Is.Not.SameAs(resolvedSecond));
        Assert.That(resolvedFirst.Owner.MacroLocationRuntimeId, Is.EqualTo(resolvedSecond.Owner.MacroLocationRuntimeId));
        Assert.That(resolvedFirst.Places[0], Is.Not.SameAs(resolvedSecond.Places[0]));
    }

    [Test]
    public void LocalPlacesDoNotBecomeSpatialLocations()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-owner", "macro-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime network = new SpatialNetworkRuntime(registry);
        Assert.That(network.RegisterLocation(city.Location), Is.True);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForCity(city), registry);

        List<LocalPlaceRuntime> rooms = new List<LocalPlaceRuntime>();
        for (int i = 0; i < 20; i++)
        {
            LocalPlaceRuntime room = new LocalPlaceRuntime("room-" + i, "Room " + i);
            Assert.That(topology.AddPlace(room), Is.True);
            rooms.Add(room);
        }

        for (int i = 0; i < 10; i++)
        {
            Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime(
                "connection-" + i,
                rooms[i],
                rooms[i + 1],
                i + 1f)), Is.True);
        }

        Assert.That(network.Locations.Count(), Is.EqualTo(1));
        Assert.That(network.Routes.Count, Is.EqualTo(0));
        Assert.That(topology.NodeCount, Is.EqualTo(20));
        Assert.That(topology.ConnectionCount, Is.EqualTo(10));
    }

    [Test]
    public void LocalPlaceIdsAreStableAndIndependent()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        Assert.That(allocator.AllocateLocalPlaceId(), Is.EqualTo("local-place-000001"));
        Assert.That(allocator.AllocateLocalPlaceId(), Is.EqualTo("local-place-000002"));
        Assert.That(allocator.AllocateLocalConnectionId(), Is.EqualTo("local-connection-000001"));
        Assert.That(allocator.AllocateLocalPlaceId(), Is.EqualTo("local-place-000003"));
    }

    [Test]
    public void LocalConnectionIdsAreStableAndIndependent()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        Assert.That(allocator.AllocateLocalConnectionId(), Is.EqualTo("local-connection-000001"));
        Assert.That(allocator.AllocateLocalConnectionId(), Is.EqualTo("local-connection-000002"));
        Assert.That(allocator.AllocateLocalPlaceId(), Is.EqualTo("local-place-000001"));
        Assert.That(allocator.AllocateLocalConnectionId(), Is.EqualTo("local-connection-000003"));
    }

    [Test]
    public void RuntimeIdentityRejectsCrossTypeDuplicateLocalPlaceId()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        CityRuntime city = SimulationTestFactory.CreateCity("shared-runtime-id", "macro-location");
        Assert.That(registry.RegisterCity(city), Is.True);
        LogAssert.Expect(
            LogType.Error,
            "Duplicate RuntimeId 'shared-runtime-id' while registering LocalPlace; it is already registered as City.");

        Assert.That(registry.RegisterLocalPlace(new LocalPlaceRuntime("shared-runtime-id")), Is.False);
    }

    [Test]
    public void RuntimeIdentityRejectsCrossTypeDuplicateLocalConnectionId()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterLocalPlace(new LocalPlaceRuntime("shared-runtime-id")), Is.True);
        LogAssert.Expect(
            LogType.Error,
            "Duplicate RuntimeId 'shared-runtime-id' while registering LocalConnection; it is already registered as LocalPlace.");

        Assert.That(registry.RegisterLocalConnection(new LocalTopologyConnectionRuntime(
            "shared-runtime-id",
            new LocalPlaceRuntime("origin"),
            new LocalPlaceRuntime("destination"),
            1f)), Is.False);
    }

    [Test]
    public void HierarchySupportsArbitraryDepth()
    {
        LocalTopologyRuntime topology = CreateCityTopology(out _);
        LocalPlaceRuntime district = AddPlace(topology, "district");
        LocalPlaceRuntime castle = AddPlace(topology, "castle", district);
        LocalPlaceRuntime keep = AddPlace(topology, "keep", castle);
        LocalPlaceRuntime floor = AddPlace(topology, "floor", keep);
        LocalPlaceRuntime room = AddPlace(topology, "room", floor);

        Assert.That(topology.GetParent(room), Is.SameAs(floor));
        Assert.That(topology.GetAncestors(room).Select(place => place.RuntimeId), Is.EqualTo(new[] { "floor", "keep", "castle", "district" }));
        Assert.That(topology.GetDepth(room), Is.EqualTo(4));
        Assert.That(topology.MaxDepth, Is.EqualTo(4));
        Assert.That(topology.IsDescendantOf(room, district), Is.True);
        Assert.That(topology.GetChildren(district), Is.EqualTo(new[] { castle }));
    }

    [Test]
    public void HierarchyRejectsSelfParent()
    {
        LocalTopologyRuntime topology = CreateCityTopology(out _);
        LocalPlaceRuntime place = AddPlace(topology, "place");

        Assert.That(topology.TrySetParent(place, place, out string diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("own parent"));
        Assert.That(place.Parent, Is.Null);
    }

    [Test]
    public void HierarchyRejectsCycles()
    {
        LocalTopologyRuntime topology = CreateCityTopology(out _);
        LocalPlaceRuntime first = AddPlace(topology, "first");
        LocalPlaceRuntime second = AddPlace(topology, "second", first);
        LocalPlaceRuntime third = AddPlace(topology, "third", second);

        Assert.That(topology.TrySetParent(first, third, out string diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("cycle"));
        Assert.That(first.Parent, Is.Null);
        Assert.That(topology.TryValidate(out diagnostic), Is.True, diagnostic);
    }

    [Test]
    public void HierarchyRejectsForeignParent()
    {
        LocalTopologyRuntime firstTopology = CreateCityTopology(out _);
        LocalTopologyRuntime secondTopology = CreateCityTopology(out _);
        LocalPlaceRuntime foreignParent = AddPlace(firstTopology, "foreign-parent");
        LocalPlaceRuntime child = AddPlace(secondTopology, "child");

        Assert.That(secondTopology.TrySetParent(child, foreignParent, out string diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("belong to this topology"));
        Assert.That(child.Parent, Is.Null);
    }

    [Test]
    public void ContainmentDoesNotCreateTraversalConnection()
    {
        LocalTopologyRuntime topology = CreateCityTopology(out _);
        LocalPlaceRuntime parent = AddPlace(topology, "parent");
        LocalPlaceRuntime child = AddPlace(topology, "child", parent);

        Assert.That(topology.GetChildren(parent), Is.EqualTo(new[] { child }));
        Assert.That(topology.ConnectionCount, Is.EqualTo(0));
        Assert.That(topology.GetOutgoingConnections(parent), Is.Empty);
        Assert.That(topology.GetOutgoingConnections(child), Is.Empty);
    }

    [Test]
    public void MultipleEntryPointsAreSupported()
    {
        LocalTopologyRuntime topology = CreateCityTopology(out _);
        LocalPlaceRuntime northGate = AddPlace(topology, "north-gate");
        LocalPlaceRuntime harbor = AddPlace(topology, "harbor");

        Assert.That(topology.AddEntryPoint(northGate), Is.True);
        Assert.That(topology.AddEntryPoint(harbor), Is.True);
        Assert.That(topology.AddEntryPoint(harbor), Is.False);
        Assert.That(topology.EntryPoints, Is.EqualTo(new[] { northGate, harbor }));
        Assert.That(topology.EntryPointCount, Is.EqualTo(2));
    }

    [Test]
    public void OneOwnerCannotReceiveTwoDifferentActiveTopologies()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-owner", "macro-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterCity(city), Is.True);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyRuntime first = CreateTopology(city, registry, "first-place");
        LocalTopologyRuntime second = CreateTopology(city, registry, "second-place");

        Assert.That(store.Add(first), Is.True);
        Assert.That(store.Add(second), Is.False);
        Assert.That(store.Topologies.Count, Is.EqualTo(1));
        Assert.That(registry.IsRuntimeIdAvailable(second.Places[0].RuntimeId), Is.True);
    }

    [Test]
    public void DifferentOwnersCanReuseSameSemanticPlaceDefinition()
    {
        LocalPlaceTypeData roomType = SimulationTestFactory.CreateLocalPlaceType("room");
        ExplorableSiteRuntime firstSite = CreateSite("site-one", "macro-one");
        ExplorableSiteRuntime secondSite = CreateSite("site-two", "macro-two");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterExplorableSite(firstSite), Is.True);
        Assert.That(registry.RegisterExplorableSite(secondSite), Is.True);

        LocalTopologyRuntime first = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(firstSite), registry);
        LocalTopologyRuntime second = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(secondSite), registry);
        LocalPlaceRuntime firstRoom = new LocalPlaceRuntime("first-room", "Room", roomType);
        LocalPlaceRuntime secondRoom = new LocalPlaceRuntime("second-room", "Room", roomType);
        Assert.That(first.AddPlace(firstRoom), Is.True);
        Assert.That(second.AddPlace(secondRoom), Is.True);

        Assert.That(firstRoom.TypeDefinition, Is.SameAs(secondRoom.TypeDefinition));
        Assert.That(firstRoom.RuntimeId, Is.Not.EqualTo(secondRoom.RuntimeId));
    }

    [Test]
    public void LocalPlaceDisplayNameDoesNotBecomeSemanticIdentity()
    {
        LocalTopologyRuntime topology = CreateCityTopology(out _);
        LocalPlaceRuntime first = new LocalPlaceRuntime("place-one", "Same Display");
        LocalPlaceRuntime second = new LocalPlaceRuntime("place-two", "Same Display");

        Assert.That(topology.AddPlace(first), Is.True);
        Assert.That(topology.AddPlace(second), Is.True);
        Assert.That(first.DisplayName, Is.EqualTo(second.DisplayName));
        Assert.That(first.RuntimeId, Is.Not.EqualTo(second.RuntimeId));
        Assert.That(topology.TryGetPlace("place-one", out LocalPlaceRuntime resolvedFirst), Is.True);
        Assert.That(topology.TryGetPlace("place-two", out LocalPlaceRuntime resolvedSecond), Is.True);
        Assert.That(resolvedFirst, Is.Not.SameAs(resolvedSecond));
    }

    private static LocalTopologyRuntime CreateCityTopology(out CityRuntime city)
    {
        city = SimulationTestFactory.CreateCity("city-owner-" + System.Guid.NewGuid().ToString("N"), "macro-location-" + System.Guid.NewGuid().ToString("N"));
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterCity(city), Is.True);
        return new LocalTopologyRuntime(LocalTopologyOwnerReference.ForCity(city), registry);
    }

    private static LocalTopologyRuntime CreateTopology(CityRuntime city, RuntimeIdentityRegistry registry, string placeId)
    {
        LocalTopologyRuntime topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForCity(city), registry);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime(placeId)), Is.True);
        return topology;
    }

    private static LocalPlaceRuntime AddPlace(
        LocalTopologyRuntime topology,
        string runtimeId,
        LocalPlaceRuntime parent = null)
    {
        LocalPlaceRuntime place = new LocalPlaceRuntime(runtimeId, runtimeId + " display");
        Assert.That(topology.AddPlace(place, parent), Is.True);
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
