using System.Collections.Generic;
using NUnit.Framework;

public sealed class LocalTopologyKnowledgeTests
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
    public void KnowingSiteDoesNotRevealSiteTopology()
    {
        ExplorableSiteRuntime site = CreateSite("site-owner", "macro-location");
        LocalTopologyRuntime topology = CreateSiteTopology(site, out _);
        NpcRuntime npc = CreateNpc("npc-one");
        ExplorableSiteKnowledgeSystem siteKnowledge = new ExplorableSiteKnowledgeSystem();

        Assert.That(siteKnowledge.RecordInitialScenarioKnowledge(npc, site), Is.True);
        Assert.That(npc.ExplorableSiteKnowledge.KnowsSite(site.RuntimeId), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsLocalPlace(
            topology.Owner.OwnerRuntimeId,
            topology.Places[0].RuntimeId), Is.False);
    }

    [Test]
    public void KnowingMacroLocationDoesNotRevealLocalTopology()
    {
        ExplorableSiteRuntime site = CreateSite("site-owner", "macro-location");
        LocalTopologyRuntime topology = CreateSiteTopology(site, out _);
        NpcRuntime npc = CreateNpc("npc-one");

        Assert.That(npc.SpatialKnowledge.DiscoverLocation(site.Location.RuntimeId), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.PlaceObservations, Is.Empty);
        Assert.That(npc.LocalTopologyKnowledge.KnowsLocalPlace(
            topology.Owner.OwnerRuntimeId,
            topology.Places[0].RuntimeId), Is.False);
    }

    [Test]
    public void NpcCanKnowOnlySubsetOfTopology()
    {
        LocalTopologyRuntime topology = CreateSiteTopology(CreateSite("site-owner", "macro-location"), out _);
        NpcRuntime npc = CreateNpc("npc-one");
        LocalTopologyKnowledgeSystem knowledgeSystem = new LocalTopologyKnowledgeSystem();

        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, topology.Places[0], 3L), Is.True);
        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, topology.Places[1], 3L), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.PlaceObservations.Count, Is.EqualTo(2));
        Assert.That(npc.LocalTopologyKnowledge.KnowsLocalPlace(topology.Owner.OwnerRuntimeId, topology.Places[2].RuntimeId), Is.False);
    }

    [Test]
    public void DirectlyObservedNodeCreatesKnowledgeSnapshot()
    {
        LocalPlaceTypeData roomType = SimulationTestFactory.CreateLocalPlaceType("room");
        ExplorableSiteRuntime site = CreateSite("site-owner", "macro-location");
        LocalTopologyRuntime topology = CreateSiteTopology(site, out _);
        LocalPlaceRuntime child = new LocalPlaceRuntime("child", "Throne Room", roomType);
        Assert.That(topology.AddPlace(child, topology.Places[0]), Is.True);
        NpcRuntime npc = CreateNpc("npc-one");

        Assert.That(new LocalTopologyKnowledgeSystem().RecordDirectObservation(npc, topology, child, 7L), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.TryGetPlaceObservation(
            site.RuntimeId,
            child.RuntimeId,
            out LocalPlaceKnowledgeObservation observation), Is.True);
        Assert.That(observation.ParentLocalPlaceRuntimeId, Is.EqualTo(topology.Places[0].RuntimeId));
        Assert.That(observation.PlaceTypeDefinitionId, Is.EqualTo(roomType.DefinitionId));
        Assert.That(observation.DisplayName, Is.EqualTo("Throne Room"));
        Assert.That(observation.IsEntryPoint, Is.False);
        Assert.That(observation.ObservedDay, Is.EqualTo(7L));
        Assert.That(observation.ReceivedDay, Is.EqualTo(7L));
        Assert.That(observation.Source, Is.EqualTo(LocalTopologyKnowledgeSource.DirectObservation));
    }

    [Test]
    public void ObservedConnectionAlsoRevealsItsEndpointsExplicitly()
    {
        LocalTopologyRuntime topology = CreateSiteTopology(CreateSite("site-owner", "macro-location"), out _);
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            "connection-one",
            topology.Places[0],
            topology.Places[1],
            2f);
        Assert.That(topology.AddConnection(connection), Is.True);
        NpcRuntime npc = CreateNpc("npc-one");

        Assert.That(new LocalTopologyKnowledgeSystem().RecordDirectObservation(npc, topology, connection, 4L), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsLocalPlace(topology.Owner.OwnerRuntimeId, topology.Places[0].RuntimeId), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsLocalPlace(topology.Owner.OwnerRuntimeId, topology.Places[1].RuntimeId), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsConnection(topology.Owner.OwnerRuntimeId, connection.RuntimeId), Is.True);
        SimulationInvariantValidator.ValidateLocalTopology(topology);
        SimulationInvariantValidator.ValidateLocalTopologyKnowledge(npc.LocalTopologyKnowledge);
    }

    [Test]
    public void KnownPathUsesOnlyKnownConnections()
    {
        LocalTopologyRuntime topology = CreateLinearTopology(out LocalTopologyConnectionRuntime firstConnection, out _);
        NpcRuntime npc = CreateNpc("npc-one");
        LocalTopologyKnowledgeSystem knowledgeSystem = new LocalTopologyKnowledgeSystem();

        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, firstConnection, 1L), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.TryFindKnownPath(
            topology.Owner.OwnerRuntimeId,
            topology.Places[0].RuntimeId,
            topology.Places[2].RuntimeId,
            out _), Is.False);
    }

    [Test]
    public void KnownPathSucceedsAfterMissingConnectionIsObserved()
    {
        LocalTopologyRuntime topology = CreateLinearTopology(out LocalTopologyConnectionRuntime firstConnection, out LocalTopologyConnectionRuntime secondConnection);
        NpcRuntime npc = CreateNpc("npc-one");
        LocalTopologyKnowledgeSystem knowledgeSystem = new LocalTopologyKnowledgeSystem();

        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, firstConnection, 1L), Is.True);
        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, secondConnection, 2L), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.TryFindKnownPath(
            topology.Owner.OwnerRuntimeId,
            topology.Places[0].RuntimeId,
            topology.Places[2].RuntimeId,
            out LocalTopologyPath path), Is.True);
        Assert.That(path.LocalConnectionRuntimeIds, Is.EqualTo(new[] { firstConnection.RuntimeId, secondConnection.RuntimeId }));
        Assert.That(path.TotalCost, Is.EqualTo(3f));
    }

    [Test]
    public void KnownPathDoesNotReadTruthBehindKnowledgeBoundary()
    {
        LocalTopologyRuntime topology = CreateLinearTopology(out _, out _);
        NpcRuntime npc = CreateNpc("npc-one");
        LocalTopologyKnowledgeSystem knowledgeSystem = new LocalTopologyKnowledgeSystem();

        foreach (LocalPlaceRuntime place in topology.Places)
        {
            Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, place, 1L), Is.True);
        }

        Assert.That(topology.TryFindShortestPath(
            topology.Places[0].RuntimeId,
            topology.Places[2].RuntimeId,
            out _), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.TryFindKnownPath(
            topology.Owner.OwnerRuntimeId,
            topology.Places[0].RuntimeId,
            topology.Places[2].RuntimeId,
            out _), Is.False);
    }

    [Test]
    public void DifferentNpcsCanKnowDifferentTopologySubsets()
    {
        LocalTopologyRuntime topology = CreateSiteTopology(CreateSite("site-owner", "macro-location"), out _);
        NpcRuntime firstNpc = CreateNpc("npc-one");
        NpcRuntime secondNpc = CreateNpc("npc-two");
        LocalTopologyKnowledgeSystem knowledgeSystem = new LocalTopologyKnowledgeSystem();

        Assert.That(knowledgeSystem.RecordDirectObservation(firstNpc, topology, topology.Places[0], 1L), Is.True);
        Assert.That(knowledgeSystem.RecordDirectObservation(secondNpc, topology, topology.Places[1], 1L), Is.True);
        Assert.That(firstNpc.LocalTopologyKnowledge.KnowsLocalPlace(topology.Owner.OwnerRuntimeId, topology.Places[0].RuntimeId), Is.True);
        Assert.That(firstNpc.LocalTopologyKnowledge.KnowsLocalPlace(topology.Owner.OwnerRuntimeId, topology.Places[1].RuntimeId), Is.False);
        Assert.That(secondNpc.LocalTopologyKnowledge.KnowsLocalPlace(topology.Owner.OwnerRuntimeId, topology.Places[0].RuntimeId), Is.False);
        Assert.That(secondNpc.LocalTopologyKnowledge.KnowsLocalPlace(topology.Owner.OwnerRuntimeId, topology.Places[1].RuntimeId), Is.True);
    }

    [Test]
    public void NewerObservationReplacesOlderObservation()
    {
        NpcRuntime npc = CreateNpc("npc-one");
        LocalPlaceKnowledgeObservation older = new LocalPlaceKnowledgeObservation(
            "site-owner", "place-one", null, null, "Old", false, 2L, 2L,
            LocalTopologyKnowledgeSource.DirectObservation);
        LocalPlaceKnowledgeObservation newer = new LocalPlaceKnowledgeObservation(
            "site-owner", "place-one", null, null, "New", false, 3L, 3L,
            LocalTopologyKnowledgeSource.DirectObservation);

        Assert.That(npc.LocalTopologyKnowledge.RecordPlaceObservation(older), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.RecordPlaceObservation(newer), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.TryGetPlaceObservation("site-owner", "place-one", out LocalPlaceKnowledgeObservation current), Is.True);
        Assert.That(current.DisplayName, Is.EqualTo("New"));
        Assert.That(npc.LocalTopologyKnowledge.RecordPlaceObservation(older), Is.False);
    }

    [Test]
    public void SameDayDirectObservationBeatsSharedKnowledge()
    {
        NpcRuntime npc = CreateNpc("npc-one");
        LocalPlaceKnowledgeObservation shared = new LocalPlaceKnowledgeObservation(
            "site-owner", "place-one", null, null, "Shared", false, 5L, 20L,
            LocalTopologyKnowledgeSource.SharedByNpc, "npc-source");
        LocalPlaceKnowledgeObservation direct = new LocalPlaceKnowledgeObservation(
            "site-owner", "place-one", null, null, "Direct", false, 5L, 5L,
            LocalTopologyKnowledgeSource.DirectObservation);

        Assert.That(npc.LocalTopologyKnowledge.RecordPlaceObservation(shared), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.RecordPlaceObservation(direct), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.TryGetPlaceObservation("site-owner", "place-one", out LocalPlaceKnowledgeObservation current), Is.True);
        Assert.That(current.DisplayName, Is.EqualTo("Direct"));
        Assert.That(current.Source, Is.EqualTo(LocalTopologyKnowledgeSource.DirectObservation));
    }

    [Test]
    public void KnowledgeQueryDoesNotMutateKnowledge()
    {
        LocalTopologyRuntime topology = CreateLinearTopology(out LocalTopologyConnectionRuntime firstConnection, out _);
        NpcRuntime npc = CreateNpc("npc-one");
        LocalTopologyKnowledgeSystem knowledgeSystem = new LocalTopologyKnowledgeSystem();
        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, firstConnection, 1L), Is.True);
        int placeCount = npc.LocalTopologyKnowledge.PlaceObservations.Count;
        int connectionCount = npc.LocalTopologyKnowledge.ConnectionObservations.Count;

        Assert.That(npc.LocalTopologyKnowledge.KnowsLocalPlace(topology.Owner.OwnerRuntimeId, topology.Places[0].RuntimeId), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsConnection(topology.Owner.OwnerRuntimeId, firstConnection.RuntimeId), Is.True);
        npc.LocalTopologyKnowledge.TryGetPlaceObservation(topology.Owner.OwnerRuntimeId, topology.Places[0].RuntimeId, out _);
        npc.LocalTopologyKnowledge.GetKnownChildren(topology.Owner.OwnerRuntimeId, null);
        npc.LocalTopologyKnowledge.TryFindKnownPath(topology.Owner.OwnerRuntimeId, topology.Places[0].RuntimeId, topology.Places[1].RuntimeId, out _);

        Assert.That(npc.LocalTopologyKnowledge.PlaceObservations.Count, Is.EqualTo(placeCount));
        Assert.That(npc.LocalTopologyKnowledge.ConnectionObservations.Count, Is.EqualTo(connectionCount));
    }

    [Test]
    public void KnowledgeQueryDoesNotMutateWorldTruth()
    {
        LocalTopologyRuntime topology = CreateLinearTopology(out LocalTopologyConnectionRuntime firstConnection, out _);
        NpcRuntime npc = CreateNpc("npc-one");
        Assert.That(new LocalTopologyKnowledgeSystem().RecordDirectObservation(npc, topology, firstConnection, 1L), Is.True);
        int nodeCount = topology.NodeCount;
        int connectionCount = topology.ConnectionCount;
        int depth = topology.GetDepth(topology.Places[2]);

        npc.LocalTopologyKnowledge.TryFindKnownPath(
            topology.Owner.OwnerRuntimeId,
            topology.Places[0].RuntimeId,
            topology.Places[1].RuntimeId,
            out _);

        Assert.That(topology.NodeCount, Is.EqualTo(nodeCount));
        Assert.That(topology.ConnectionCount, Is.EqualTo(connectionCount));
        Assert.That(topology.GetDepth(topology.Places[2]), Is.EqualTo(depth));
    }

    [Test]
    public void TopologyKnowledgeIsScopedByOwner()
    {
        LocalPlaceTypeData roomType = SimulationTestFactory.CreateLocalPlaceType("room");
        ExplorableSiteRuntime firstSite = CreateSite("site-one", "shared-macro");
        ExplorableSiteRuntime secondSite = CreateSite("site-two", "shared-macro");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterExplorableSite(firstSite), Is.True);
        Assert.That(registry.RegisterExplorableSite(secondSite), Is.True);
        LocalTopologyRuntime firstTopology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(firstSite), registry);
        LocalTopologyRuntime secondTopology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(secondSite), registry);
        LocalPlaceRuntime firstRoom = new LocalPlaceRuntime("first-room", "Room", roomType);
        LocalPlaceRuntime secondRoom = new LocalPlaceRuntime("second-room", "Room", roomType);
        Assert.That(firstTopology.AddPlace(firstRoom), Is.True);
        Assert.That(secondTopology.AddPlace(secondRoom), Is.True);
        NpcRuntime npc = CreateNpc("npc-one");

        Assert.That(new LocalTopologyKnowledgeSystem().RecordDirectObservation(npc, firstTopology, firstRoom, 1L), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsLocalPlace(firstSite.RuntimeId, firstRoom.RuntimeId), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsLocalPlace(secondSite.RuntimeId, secondRoom.RuntimeId), Is.False);
    }

    [Test]
    public void ArrivalAtExplorableSiteDoesNotRevealEntireTopology()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-origin", "origin-location");
        ExplorableSiteRuntime site = CreateSite("site-target", "site-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterCity(city), Is.True);
        Assert.That(registry.RegisterExplorableSite(site), Is.True);
        SpatialNetworkRuntime network = new SpatialNetworkRuntime(registry);
        Assert.That(network.RegisterLocation(city.Location), Is.True);
        Assert.That(network.RegisterLocation(site.Location), Is.True);
        SpatialRouteRuntime route = new SpatialRouteRuntime("route-to-site", city.Location, site.Location, 1);
        Assert.That(network.RegisterRoute(route), Is.True);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(site), registry);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime("entrance")), Is.True);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime("crypt")), Is.True);
        NpcRuntime npc = new NpcRuntime("npc-one", SimulationTestFactory.CreateNpc("npc-one"), city, 0f);
        Assert.That(registry.RegisterNpc(npc), Is.True);
        ExplorableSiteStore siteStore = new ExplorableSiteStore();
        Assert.That(siteStore.Add(site), Is.True);
        ExplorableSiteKnowledgeSystem siteKnowledge = new ExplorableSiteKnowledgeSystem();
        TravelSystem travel = new TravelSystem(
            network,
            location => location == city.Location ? city : null,
            0f);
        SimulationRuntime simulation = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new[] { npc },
            economyEnabled: false,
            travelSystem: travel,
            explorableSiteStore: siteStore,
            explorableSiteKnowledgeSystem: siteKnowledge);

        Assert.That(travel.TryStartTravel(npc, site.Location, 1), Is.True);
        simulation.AdvanceDays(2);

        Assert.That(npc.CurrentLocation, Is.SameAs(site.Location));
        Assert.That(npc.ExplorableSiteKnowledge.KnowsSite(site.RuntimeId), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.PlaceObservations, Is.Empty);
        Assert.That(npc.LocalTopologyKnowledge.ConnectionObservations, Is.Empty);
        SimulationInvariantValidator.ValidateLocalTopology(topology, registry);
        SimulationInvariantValidator.ValidateLocalTopologyKnowledge(npc.LocalTopologyKnowledge);
        Assert.That(topology.Places.Count, Is.EqualTo(2));
    }

    [Test]
    public void ExistingSpatialKnowledgeBehaviorRemainsUnchanged()
    {
        SpatialKnowledgeRuntime knowledge = new SpatialKnowledgeRuntime("npc-one");

        Assert.That(knowledge.DiscoverLocation("location-one"), Is.True);
        Assert.That(knowledge.DiscoverRoute("route-one"), Is.True);
        Assert.That(knowledge.KnowsLocation("location-one"), Is.True);
        Assert.That(knowledge.KnowsRoute("route-one"), Is.True);
        Assert.That(knowledge.KnownLocationRuntimeIds, Is.EqualTo(new[] { "location-one" }));
        Assert.That(knowledge.KnownRouteRuntimeIds, Is.EqualTo(new[] { "route-one" }));
    }

    private static LocalTopologyRuntime CreateSiteTopology(
        ExplorableSiteRuntime site,
        out RuntimeIdentityRegistry registry)
    {
        registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterExplorableSite(site), Is.True);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForExplorableSite(site),
            registry);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime("entrance"), isEntryPoint: true), Is.True);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime("room-a")), Is.True);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime("room-b")), Is.True);
        return topology;
    }

    private static LocalTopologyRuntime CreateLinearTopology(
        out LocalTopologyConnectionRuntime firstConnection,
        out LocalTopologyConnectionRuntime secondConnection)
    {
        LocalTopologyRuntime topology = CreateSiteTopology(CreateSite("site-owner", "macro-location"), out _);
        firstConnection = new LocalTopologyConnectionRuntime(
            "connection-a-b",
            topology.Places[0],
            topology.Places[1],
            1f);
        secondConnection = new LocalTopologyConnectionRuntime(
            "connection-b-c",
            topology.Places[1],
            topology.Places[2],
            2f);
        Assert.That(topology.AddConnection(firstConnection), Is.True);
        Assert.That(topology.AddConnection(secondConnection), Is.True);
        return topology;
    }

    private static NpcRuntime CreateNpc(string runtimeId)
    {
        return new NpcRuntime(runtimeId, SimulationTestFactory.CreateNpc(runtimeId));
    }

    private static ExplorableSiteRuntime CreateSite(string runtimeId, string locationRuntimeId)
    {
        return new ExplorableSiteRuntime(
            runtimeId,
            SimulationTestFactory.CreateExplorableSite("definition-" + runtimeId),
            new SpatialLocationRuntime(locationRuntimeId));
    }
}
