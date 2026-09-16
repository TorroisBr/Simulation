using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class LocalTopologyPublishedMutationTests
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
    public void DraftTopologyAllowsDirectPlaceAddition()
    {
        LocalTopologyRuntime topology = CreateDraftCityTopology(
            "draft-place-city",
            "draft-place-location",
            out RuntimeIdentityRegistry registry,
            out _);
        LocalPlaceRuntime place = new LocalPlaceRuntime("draft-place");

        Assert.That(topology.IsPublished, Is.False);
        Assert.That(topology.AddPlace(place), Is.True);
        Assert.That(topology.ContainsPlace(place), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable(place.RuntimeId), Is.True);
    }

    [Test]
    public void DraftTopologyAllowsDirectConnectionAddition()
    {
        LocalTopologyRuntime topology = CreateDraftCityTopology(
            "draft-connection-city",
            "draft-connection-location",
            out RuntimeIdentityRegistry registry,
            out _);
        LocalPlaceRuntime origin = new LocalPlaceRuntime("draft-origin");
        LocalPlaceRuntime destination = new LocalPlaceRuntime("draft-destination");
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            "draft-connection",
            origin,
            destination,
            1f);
        Assert.That(topology.AddPlace(origin), Is.True);
        Assert.That(topology.AddPlace(destination), Is.True);

        Assert.That(topology.IsPublished, Is.False);
        Assert.That(topology.AddConnection(connection), Is.True);
        Assert.That(topology.ContainsConnection(connection), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable(connection.RuntimeId), Is.True);
    }

    [Test]
    public void SuccessfulStoreCommitMarksTopologyPublished()
    {
        LocalTopologyRuntime topology = CreateDraftCityTopology(
            "published-state-city",
            "published-state-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime("published-state-place")), Is.True);

        Assert.That(store.TryAddTopology(topology, out string diagnostic), Is.True, diagnostic);
        Assert.That(topology.IsPublished, Is.True);
        Assert.That(topology.PublicationState, Is.EqualTo(LocalTopologyPublicationState.Published));
        SimulationInvariantValidator.ValidateLocalTopology(topology, registry);
    }

    [Test]
    public void FailedStoreCommitDoesNotMarkTopologyPublished()
    {
        LocalTopologyRuntime topology = CreateDraftCityTopology(
            "failed-state-owner",
            "failed-state-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        CityRuntime conflictingCity = CreateRegisteredCity(
            "failed-state-conflict",
            "failed-state-conflict-location",
            registry);
        LocalPlaceRuntime conflictingPlace = new LocalPlaceRuntime(conflictingCity.RuntimeId);
        LocalPlaceRuntime otherPlace = new LocalPlaceRuntime("failed-state-other-place");
        Assert.That(topology.AddPlace(conflictingPlace), Is.True);
        Assert.That(topology.AddPlace(otherPlace), Is.True);

        Assert.That(store.TryAddTopology(topology, out _), Is.False);
        Assert.That(topology.IsPublished, Is.False);
        Assert.That(topology.PublicationState, Is.EqualTo(LocalTopologyPublicationState.Draft));
        Assert.That(registry.IsRuntimeIdAvailable(otherPlace.RuntimeId), Is.True);
        Assert.That(store.Topologies, Is.Empty);
    }

    [Test]
    public void PublishedTopologyDirectAddPlaceIsRejected()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopology(
            "direct-place-city",
            "direct-place-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        LocalPlaceRuntime place = new LocalPlaceRuntime("direct-place-rejected");
        int nodeCount = topology.NodeCount;

        Assert.That(topology.AddPlace(place), Is.False);
        Assert.That(topology.NodeCount, Is.EqualTo(nodeCount));
        AssertPlaceIsNotPublished(registry, topology, place);
        Assert.That(store.Topologies.Count, Is.EqualTo(1));
    }

    [Test]
    public void PublishedTopologyDirectAddConnectionIsRejected()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopologyWithTwoPlaces(
            "direct-connection-city",
            "direct-connection-location",
            out RuntimeIdentityRegistry registry,
            out _);
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            "direct-connection-rejected",
            topology.Places[0],
            topology.Places[1],
            1f);
        int connectionCount = topology.ConnectionCount;

        Assert.That(topology.AddConnection(connection), Is.False);
        Assert.That(topology.ConnectionCount, Is.EqualTo(connectionCount));
        AssertConnectionIsNotPublished(registry, topology, connection);
    }

    [Test]
    public void PublishedTopologyCanAddPlaceThroughStore()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopology(
            "store-place-city",
            "store-place-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        LocalPlaceRuntime place = new LocalPlaceRuntime("store-place");

        Assert.That(store.TryAddPlace(topology, place, out string diagnostic), Is.True, diagnostic);
        Assert.That(topology.ContainsPlace(place), Is.True);
        Assert.That(registry.TryGetLocalPlace(place.RuntimeId, out LocalPlaceRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(place));
        SimulationInvariantValidator.ValidateLocalTopology(topology, registry);
    }

    [Test]
    public void PublishedTopologyCanAddNestedPlaceThroughStore()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopology(
            "nested-place-city",
            "nested-place-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        LocalPlaceRuntime castle = topology.Places[0];
        LocalPlaceRuntime secretRoom = new LocalPlaceRuntime("nested-secret-room");

        Assert.That(store.TryAddPlace(topology, secretRoom, castle, out string diagnostic), Is.True, diagnostic);
        Assert.That(secretRoom.Parent, Is.SameAs(castle));
        Assert.That(topology.GetParent(secretRoom), Is.SameAs(castle));
        Assert.That(topology.GetChildren(castle), Does.Contain(secretRoom));
        Assert.That(registry.TryGetLocalPlace(secretRoom.RuntimeId, out LocalPlaceRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(secretRoom));
    }

    [Test]
    public void PublishedTopologyCanAddEntryPointThroughStore()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopology(
            "entry-point-city",
            "entry-point-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        LocalPlaceRuntime entryPoint = new LocalPlaceRuntime("store-entry-point");

        Assert.That(store.TryAddPlace(topology, entryPoint, true, out string diagnostic), Is.True, diagnostic);
        Assert.That(topology.EntryPoints, Does.Contain(entryPoint));
        Assert.That(registry.TryGetLocalPlace(entryPoint.RuntimeId, out LocalPlaceRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(entryPoint));
    }

    [Test]
    public void PublishedTopologyCanAddConnectionThroughStore()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopologyWithTwoPlaces(
            "store-connection-city",
            "store-connection-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            "store-connection",
            topology.Places[0],
            topology.Places[1],
            2f);

        Assert.That(store.TryAddConnection(topology, connection, out string diagnostic), Is.True, diagnostic);
        Assert.That(topology.ContainsConnection(connection), Is.True);
        Assert.That(registry.TryGetLocalConnection(connection.RuntimeId, out LocalTopologyConnectionRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(connection));
    }

    [Test]
    public void PublishedPlaceRuntimeIdConflictDoesNotMutateTopology()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopology(
            "place-conflict-city",
            "place-conflict-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        CityRuntime conflictingCity = CreateRegisteredCity(
            "published-place-conflict",
            "published-place-conflict-location",
            registry);
        LocalPlaceRuntime place = new LocalPlaceRuntime(conflictingCity.RuntimeId);
        int nodeCount = topology.NodeCount;

        Assert.That(store.TryAddPlace(topology, place, out _), Is.False);
        Assert.That(topology.NodeCount, Is.EqualTo(nodeCount));
        Assert.That(topology.ContainsPlace(place), Is.False);
        Assert.That(registry.TryGetCity(conflictingCity.RuntimeId, out CityRuntime resolvedCity), Is.True);
        Assert.That(resolvedCity, Is.SameAs(conflictingCity));
    }

    [Test]
    public void PublishedConnectionRuntimeIdConflictDoesNotMutateTopology()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopologyWithTwoPlaces(
            "connection-conflict-city",
            "connection-conflict-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        LocalPlaceRuntime existingOrigin = new LocalPlaceRuntime("existing-connection-origin");
        LocalPlaceRuntime existingDestination = new LocalPlaceRuntime("existing-connection-destination");
        LocalTopologyConnectionRuntime existingConnection = new LocalTopologyConnectionRuntime(
            "published-connection-conflict",
            existingOrigin,
            existingDestination,
            1f);
        Assert.That(registry.RegisterLocalConnection(existingConnection), Is.True);
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            existingConnection.RuntimeId,
            topology.Places[0],
            topology.Places[1],
            2f);
        int connectionCount = topology.ConnectionCount;

        Assert.That(store.TryAddConnection(topology, connection, out _), Is.False);
        Assert.That(topology.ConnectionCount, Is.EqualTo(connectionCount));
        Assert.That(topology.ContainsConnection(connection), Is.False);
        Assert.That(registry.TryGetLocalConnection(existingConnection.RuntimeId, out LocalTopologyConnectionRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(existingConnection));
    }

    [Test]
    public void PublishedPlaceWithForeignParentIsRejectedAtomically()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopology(
            "foreign-parent-city",
            "foreign-parent-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        CityRuntime foreignCity = CreateRegisteredCity(
            "foreign-parent-owner",
            "foreign-parent-owner-location",
            registry);
        LocalTopologyRuntime foreignTopology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForCity(foreignCity),
            registry);
        LocalPlaceRuntime foreignParent = new LocalPlaceRuntime("foreign-parent");
        Assert.That(foreignTopology.AddPlace(foreignParent), Is.True);
        LocalPlaceRuntime place = new LocalPlaceRuntime("foreign-parent-candidate");

        Assert.That(store.TryAddPlace(topology, place, foreignParent, out _), Is.False);
        AssertPlaceIsNotPublished(registry, topology, place);
    }

    [Test]
    public void PublishedConnectionWithForeignEndpointIsRejectedAtomically()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopology(
            "foreign-endpoint-city",
            "foreign-endpoint-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        CityRuntime foreignCity = CreateRegisteredCity(
            "foreign-endpoint-owner",
            "foreign-endpoint-owner-location",
            registry);
        LocalTopologyRuntime foreignTopology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForCity(foreignCity),
            registry);
        LocalPlaceRuntime foreignPlace = new LocalPlaceRuntime("foreign-endpoint");
        Assert.That(foreignTopology.AddPlace(foreignPlace), Is.True);
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            "foreign-endpoint-connection",
            topology.Places[0],
            foreignPlace,
            1f);

        Assert.That(store.TryAddConnection(topology, connection, out _), Is.False);
        AssertConnectionIsNotPublished(registry, topology, connection);
    }

    [Test]
    public void WrongStoreCannotMutatePublishedTopology()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopology(
            "wrong-store-city",
            "wrong-store-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore owningStore);
        LocalTopologyStore wrongStore = new LocalTopologyStore(registry);
        LocalPlaceRuntime place = new LocalPlaceRuntime("wrong-store-place");
        int nodeCount = topology.NodeCount;

        Assert.That(wrongStore.TryAddPlace(topology, place, out _), Is.False);
        Assert.That(topology.NodeCount, Is.EqualTo(nodeCount));
        AssertPlaceIsNotPublished(registry, topology, place);
        Assert.That(owningStore.Topologies.Count, Is.EqualTo(1));
        Assert.That(wrongStore.Topologies, Is.Empty);
    }

    [Test]
    public void PublishedTopologyExpansionPreservesShortestPath()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopologyWithTwoPlaces(
            "expansion-path-city",
            "expansion-path-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        LocalPlaceRuntime start = topology.Places[0];
        LocalPlaceRuntime middle = topology.Places[1];
        LocalTopologyConnectionRuntime firstConnection = new LocalTopologyConnectionRuntime(
            "expansion-path-first",
            start,
            middle,
            1f);
        Assert.That(store.TryAddConnection(topology, firstConnection, out string firstDiagnostic), Is.True, firstDiagnostic);

        LocalPlaceRuntime destination = new LocalPlaceRuntime("expansion-path-destination");
        Assert.That(store.TryAddPlace(topology, destination, out string placeDiagnostic), Is.True, placeDiagnostic);
        LocalTopologyConnectionRuntime secondConnection = new LocalTopologyConnectionRuntime(
            "expansion-path-second",
            middle,
            destination,
            2f);
        Assert.That(store.TryAddConnection(topology, secondConnection, out string secondDiagnostic), Is.True, secondDiagnostic);

        Assert.That(topology.TryFindShortestPath(
            start.RuntimeId,
            destination.RuntimeId,
            out LocalTopologyPath path), Is.True);
        Assert.That(path.LocalPlaceRuntimeIds, Is.EqualTo(new[]
        {
            start.RuntimeId,
            middle.RuntimeId,
            destination.RuntimeId
        }));
        Assert.That(path.LocalConnectionRuntimeIds, Is.EqualTo(new[]
        {
            firstConnection.RuntimeId,
            secondConnection.RuntimeId
        }));
        Assert.That(path.TotalCost, Is.EqualTo(3f));
        SimulationInvariantValidator.ValidateLocalTopology(topology, registry);
    }

    [Test]
    public void PublishedTopologyExpansionCanBeObservedByKnowledge()
    {
        LocalTopologyRuntime topology = CreatePublishedCityTopologyWithTwoPlaces(
            "expansion-knowledge-city",
            "expansion-knowledge-location",
            out RuntimeIdentityRegistry registry,
            out LocalTopologyStore store);
        LocalPlaceRuntime knownPlace = topology.Places[1];
        LocalPlaceRuntime newPlace = new LocalPlaceRuntime("expansion-knowledge-place");
        Assert.That(store.TryAddPlace(topology, newPlace, out string placeDiagnostic), Is.True, placeDiagnostic);
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            "expansion-knowledge-connection",
            knownPlace,
            newPlace,
            1f);
        Assert.That(store.TryAddConnection(topology, connection, out string connectionDiagnostic), Is.True, connectionDiagnostic);

        NpcRuntime npc = new NpcRuntime(
            "expansion-knowledge-npc",
            SimulationTestFactory.CreateNpc("expansion-knowledge-npc"));
        LocalTopologyKnowledgeSystem knowledgeSystem = new LocalTopologyKnowledgeSystem();
        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, knownPlace, 1L), Is.True);
        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, newPlace, 1L), Is.True);
        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, connection, 1L), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsLocalPlace(topology.Owner.OwnerRuntimeId, newPlace.RuntimeId), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsConnection(topology.Owner.OwnerRuntimeId, connection.RuntimeId), Is.True);
        SimulationInvariantValidator.ValidateLocalTopology(topology, registry);
    }

    private static LocalTopologyRuntime CreateDraftCityTopology(
        string cityRuntimeId,
        string locationRuntimeId,
        out RuntimeIdentityRegistry registry,
        out LocalTopologyStore store)
    {
        CityRuntime city = CreateRegisteredCity(cityRuntimeId, locationRuntimeId, out registry);
        store = new LocalTopologyStore(registry);
        return new LocalTopologyRuntime(LocalTopologyOwnerReference.ForCity(city), registry);
    }

    private static LocalTopologyRuntime CreatePublishedCityTopology(
        string cityRuntimeId,
        string locationRuntimeId,
        out RuntimeIdentityRegistry registry,
        out LocalTopologyStore store)
    {
        LocalTopologyRuntime topology = CreateDraftCityTopology(
            cityRuntimeId,
            locationRuntimeId,
            out registry,
            out store);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime(cityRuntimeId + "-place")), Is.True);
        Assert.That(store.Add(topology), Is.True);
        Assert.That(topology.IsPublished, Is.True);
        return topology;
    }

    private static LocalTopologyRuntime CreatePublishedCityTopologyWithTwoPlaces(
        string cityRuntimeId,
        string locationRuntimeId,
        out RuntimeIdentityRegistry registry,
        out LocalTopologyStore store)
    {
        LocalTopologyRuntime topology = CreateDraftCityTopology(
            cityRuntimeId,
            locationRuntimeId,
            out registry,
            out store);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime(cityRuntimeId + "-origin")), Is.True);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime(cityRuntimeId + "-destination")), Is.True);
        Assert.That(store.Add(topology), Is.True);
        Assert.That(topology.IsPublished, Is.True);
        return topology;
    }

    private static CityRuntime CreateRegisteredCity(
        string runtimeId,
        string locationRuntimeId,
        out RuntimeIdentityRegistry registry)
    {
        registry = new RuntimeIdentityRegistry();
        return CreateRegisteredCity(runtimeId, locationRuntimeId, registry);
    }

    private static CityRuntime CreateRegisteredCity(
        string runtimeId,
        string locationRuntimeId,
        RuntimeIdentityRegistry registry)
    {
        CityRuntime city = SimulationTestFactory.CreateCity(runtimeId, locationRuntimeId);
        Assert.That(registry.RegisterCity(city), Is.True);
        return city;
    }

    private static void AssertPlaceIsNotPublished(
        RuntimeIdentityRegistry registry,
        LocalTopologyRuntime topology,
        LocalPlaceRuntime place)
    {
        Assert.That(topology.ContainsPlace(place), Is.False);
        Assert.That(registry.IsRuntimeIdAvailable(place.RuntimeId), Is.True);
        LogAssert.Expect(
            LogType.Warning,
            $"LocalPlace runtime resolution failed: RuntimeId '{place.RuntimeId}' is not registered.");
        Assert.That(registry.TryGetLocalPlace(place.RuntimeId, out _), Is.False);
    }

    private static void AssertConnectionIsNotPublished(
        RuntimeIdentityRegistry registry,
        LocalTopologyRuntime topology,
        LocalTopologyConnectionRuntime connection)
    {
        Assert.That(topology.ContainsConnection(connection), Is.False);
        Assert.That(registry.IsRuntimeIdAvailable(connection.RuntimeId), Is.True);
        LogAssert.Expect(
            LogType.Warning,
            $"LocalConnection runtime resolution failed: RuntimeId '{connection.RuntimeId}' is not registered.");
        Assert.That(registry.TryGetLocalConnection(connection.RuntimeId, out _), Is.False);
    }
}
