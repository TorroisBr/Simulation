using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class LocalTopologyPublicationAtomicityTests
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
    public void UncommittedTopologyDoesNotPublishRuntimeIdentities()
    {
        CityRuntime city = CreateRegisteredCity("uncommitted-city", "uncommitted-location", out RuntimeIdentityRegistry registry);
        LocalTopologyRuntime topology = CreateTopology(city, registry);
        LocalPlaceRuntime first = new LocalPlaceRuntime("uncommitted-place-a");
        LocalPlaceRuntime second = new LocalPlaceRuntime("uncommitted-place-b");
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            "uncommitted-connection",
            first,
            second,
            1f);

        Assert.That(topology.AddPlace(first), Is.True);
        Assert.That(topology.AddPlace(second), Is.True);
        Assert.That(topology.AddConnection(connection), Is.True);

        Assert.That(registry.IsRuntimeIdAvailable(first.RuntimeId), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable(second.RuntimeId), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable(connection.RuntimeId), Is.True);
        LogAssert.Expect(
            LogType.Warning,
            "LocalPlace runtime resolution failed: RuntimeId 'uncommitted-place-a' is not registered.");
        Assert.That(registry.TryGetLocalPlace(first.RuntimeId, out _), Is.False);
        LogAssert.Expect(
            LogType.Warning,
            "LocalConnection runtime resolution failed: RuntimeId 'uncommitted-connection' is not registered.");
        Assert.That(registry.TryGetLocalConnection(connection.RuntimeId, out _), Is.False);
    }

    [Test]
    public void CommittedTopologyPublishesAllRuntimeIdentities()
    {
        CityRuntime city = CreateRegisteredCity("committed-city", "committed-location", out RuntimeIdentityRegistry registry);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyRuntime topology = CreateTopology(city, registry);
        LocalPlaceRuntime first = new LocalPlaceRuntime("committed-place-a");
        LocalPlaceRuntime second = new LocalPlaceRuntime("committed-place-b");
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            "committed-connection",
            first,
            second,
            1f);

        Assert.That(topology.AddPlace(first), Is.True);
        Assert.That(topology.AddPlace(second), Is.True);
        Assert.That(topology.AddConnection(connection), Is.True);
        Assert.That(store.TryAddTopology(topology, out string diagnostic), Is.True, diagnostic);

        Assert.That(registry.TryGetLocalPlace(first.RuntimeId, out LocalPlaceRuntime resolvedFirst), Is.True);
        Assert.That(resolvedFirst, Is.SameAs(first));
        Assert.That(registry.TryGetLocalPlace(second.RuntimeId, out LocalPlaceRuntime resolvedSecond), Is.True);
        Assert.That(resolvedSecond, Is.SameAs(second));
        Assert.That(registry.TryGetLocalConnection(connection.RuntimeId, out LocalTopologyConnectionRuntime resolvedConnection), Is.True);
        Assert.That(resolvedConnection, Is.SameAs(connection));
        SimulationInvariantValidator.ValidateLocalTopology(topology, registry, requirePublishedMembers: true);
    }

    [Test]
    public void RejectedTopologyDoesNotLeaveRegisteredPlacesOrConnections()
    {
        CityRuntime city = CreateRegisteredCity("rejected-city", "rejected-location", out RuntimeIdentityRegistry registry);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyRuntime acceptedTopology = CreateTopology(city, registry);
        Assert.That(acceptedTopology.AddPlace(new LocalPlaceRuntime("accepted-place")), Is.True);
        Assert.That(store.Add(acceptedTopology), Is.True);

        LocalTopologyRuntime rejectedTopology = CreateTopology(city, registry);
        LocalPlaceRuntime rejectedFirst = new LocalPlaceRuntime("rejected-place-a");
        LocalPlaceRuntime rejectedSecond = new LocalPlaceRuntime("rejected-place-b");
        LocalTopologyConnectionRuntime rejectedConnection = new LocalTopologyConnectionRuntime(
            "rejected-connection",
            rejectedFirst,
            rejectedSecond,
            1f);
        Assert.That(rejectedTopology.AddPlace(rejectedFirst), Is.True);
        Assert.That(rejectedTopology.AddPlace(rejectedSecond), Is.True);
        Assert.That(rejectedTopology.AddConnection(rejectedConnection), Is.True);

        Assert.That(store.TryAddTopology(rejectedTopology, out _), Is.False);
        Assert.That(registry.IsRuntimeIdAvailable(rejectedFirst.RuntimeId), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable(rejectedSecond.RuntimeId), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable(rejectedConnection.RuntimeId), Is.True);
        Assert.That(store.Topologies.Count, Is.EqualTo(1));
    }

    [Test]
    public void CrossTypeConflictRejectsEntireTopologyAtomically()
    {
        CityRuntime owner = CreateRegisteredCity("cross-type-owner", "cross-type-location", out RuntimeIdentityRegistry registry);
        CityRuntime conflictingCity = CreateRegisteredCity("cross-type-conflict", "other-location", registry);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyRuntime topology = CreateTopology(owner, registry);
        LocalPlaceRuntime conflictingPlace = new LocalPlaceRuntime(conflictingCity.RuntimeId);
        LocalPlaceRuntime otherPlace = new LocalPlaceRuntime("cross-type-other-place");
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            "cross-type-connection",
            conflictingPlace,
            otherPlace,
            1f);

        Assert.That(topology.AddPlace(conflictingPlace), Is.True);
        Assert.That(topology.AddPlace(otherPlace), Is.True);
        Assert.That(topology.AddConnection(connection), Is.True);

        Assert.That(store.TryAddTopology(topology, out _), Is.False);
        Assert.That(registry.IsRuntimeIdAvailable(otherPlace.RuntimeId), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable(connection.RuntimeId), Is.True);
        Assert.That(store.Topologies, Is.Empty);
    }

    [Test]
    public void ConnectionIdConflictRejectsEntireTopologyAtomically()
    {
        CityRuntime city = CreateRegisteredCity("connection-conflict-city", "connection-conflict-location", out RuntimeIdentityRegistry registry);
        LocalPlaceRuntime existingOrigin = new LocalPlaceRuntime("existing-origin");
        LocalPlaceRuntime existingDestination = new LocalPlaceRuntime("existing-destination");
        LocalTopologyConnectionRuntime existingConnection = new LocalTopologyConnectionRuntime(
            "conflicting-connection",
            existingOrigin,
            existingDestination,
            1f);
        Assert.That(registry.RegisterLocalConnection(existingConnection), Is.True);

        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyRuntime topology = CreateTopology(city, registry);
        LocalPlaceRuntime first = new LocalPlaceRuntime("connection-conflict-place-a");
        LocalPlaceRuntime second = new LocalPlaceRuntime("connection-conflict-place-b");
        LocalTopologyConnectionRuntime conflictingConnection = new LocalTopologyConnectionRuntime(
            existingConnection.RuntimeId,
            first,
            second,
            2f);
        Assert.That(topology.AddPlace(first), Is.True);
        Assert.That(topology.AddPlace(second), Is.True);
        Assert.That(topology.AddConnection(conflictingConnection), Is.True);

        Assert.That(store.TryAddTopology(topology, out _), Is.False);
        Assert.That(registry.IsRuntimeIdAvailable(first.RuntimeId), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable(second.RuntimeId), Is.True);
        Assert.That(registry.TryGetLocalConnection(existingConnection.RuntimeId, out LocalTopologyConnectionRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(existingConnection));
        Assert.That(store.Topologies, Is.Empty);
    }

    [Test]
    public void DuplicateRuntimeIdsInsideTopologyRejectEntireCommit()
    {
        CityRuntime city = CreateRegisteredCity("duplicate-member-city", "duplicate-member-location", out RuntimeIdentityRegistry registry);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyRuntime topology = CreateTopology(city, registry);
        LocalPlaceRuntime sharedIdPlace = new LocalPlaceRuntime("shared-member-id");
        LocalPlaceRuntime otherPlace = new LocalPlaceRuntime("other-member-place");
        LocalTopologyConnectionRuntime duplicateIdConnection = new LocalTopologyConnectionRuntime(
            sharedIdPlace.RuntimeId,
            sharedIdPlace,
            otherPlace,
            1f);
        Assert.That(topology.AddPlace(sharedIdPlace), Is.True);
        Assert.That(topology.AddPlace(otherPlace), Is.True);
        Assert.That(topology.AddConnection(duplicateIdConnection), Is.True);

        Assert.That(store.TryAddTopology(topology, out _), Is.False);
        Assert.That(registry.IsRuntimeIdAvailable(sharedIdPlace.RuntimeId), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable(otherPlace.RuntimeId), Is.True);
        Assert.That(store.Topologies, Is.Empty);
    }

    [Test]
    public void BlueprintBuildStillPublishesTopologyAndMembersOnSuccess()
    {
        ExplorableSiteRuntime site = CreateRegisteredSite("blueprint-success-site", "blueprint-success-location", out RuntimeIdentityRegistry registry);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyBlueprint blueprint = new LocalTopologyBlueprint(LocalTopologyOwnerReference.ForExplorableSite(site));
        blueprint.AddNode(new LocalTopologyBlueprintNode("entrance", "Entrance"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("room", "Room"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("vault", "Vault"));
        blueprint.AddConnection(new LocalTopologyBlueprintConnection("entrance", "room", 1f));
        blueprint.AddConnection(new LocalTopologyBlueprintConnection("room", "vault", 2f));

        LocalTopologyBuilder builder = new LocalTopologyBuilder(new RuntimeIdAllocator(), registry, store);
        Assert.That(builder.TryBuild(blueprint, out LocalTopologyRuntime topology, out string diagnostic), Is.True, diagnostic);
        Assert.That(store.TryGetTopologyForOwner(site.RuntimeId, out LocalTopologyRuntime resolvedTopology), Is.True);
        Assert.That(resolvedTopology, Is.SameAs(topology));
        Assert.That(topology.Places.Count, Is.EqualTo(3));
        Assert.That(topology.Connections.Count, Is.EqualTo(2));
        SimulationInvariantValidator.ValidateLocalTopology(topology, registry, requirePublishedMembers: true);
    }

    [Test]
    public void InvalidBlueprintStillPublishesNothing()
    {
        ExplorableSiteRuntime site = CreateRegisteredSite("invalid-blueprint-site", "invalid-blueprint-location", out RuntimeIdentityRegistry registry);
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
        Assert.That(registry.IsRuntimeIdAvailable("local-place-000002"), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable("local-connection-000001"), Is.True);
    }

    [Test]
    public void FailedBlueprintCommitDoesNotPublishPartialMembers()
    {
        ExplorableSiteRuntime site = CreateRegisteredSite("failed-blueprint-site", "failed-blueprint-location", out RuntimeIdentityRegistry registry);
        LocalPlaceRuntime existingOrigin = new LocalPlaceRuntime("existing-blueprint-origin");
        LocalPlaceRuntime existingDestination = new LocalPlaceRuntime("existing-blueprint-destination");
        LocalTopologyConnectionRuntime existingConnection = new LocalTopologyConnectionRuntime(
            "local-connection-000001",
            existingOrigin,
            existingDestination,
            1f);
        Assert.That(registry.RegisterLocalConnection(existingConnection), Is.True);

        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyBlueprint blueprint = new LocalTopologyBlueprint(LocalTopologyOwnerReference.ForExplorableSite(site));
        blueprint.AddNode(new LocalTopologyBlueprintNode("entrance", "Entrance"));
        blueprint.AddNode(new LocalTopologyBlueprintNode("room", "Room"));
        blueprint.AddConnection(new LocalTopologyBlueprintConnection("entrance", "room", 1f));

        LocalTopologyBuilder builder = new LocalTopologyBuilder(new RuntimeIdAllocator(), registry, store);
        Assert.That(builder.TryBuild(blueprint, out LocalTopologyRuntime topology, out _), Is.False);
        Assert.That(topology, Is.Null);
        Assert.That(store.Topologies, Is.Empty);
        Assert.That(registry.IsRuntimeIdAvailable("local-place-000001"), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable("local-place-000002"), Is.True);
        Assert.That(registry.TryGetLocalConnection(existingConnection.RuntimeId, out LocalTopologyConnectionRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(existingConnection));
    }

    [Test]
    public void TwoSitesSameMacroLocationCommitIndependently()
    {
        ExplorableSiteRuntime firstSite = CreateRegisteredSite("same-macro-first-site", "shared-macro", out RuntimeIdentityRegistry registry);
        ExplorableSiteRuntime secondSite = CreateRegisteredSite("same-macro-second-site", "shared-macro", registry);
        LocalTopologyStore store = new LocalTopologyStore(registry);

        LocalTopologyRuntime firstTopology = CreateTopology(firstSite, registry);
        LocalTopologyRuntime secondTopology = CreateTopology(secondSite, registry);
        LocalPlaceRuntime firstPlace = new LocalPlaceRuntime("same-macro-first-place");
        LocalPlaceRuntime secondPlace = new LocalPlaceRuntime("same-macro-second-place");
        Assert.That(firstTopology.AddPlace(firstPlace), Is.True);
        Assert.That(secondTopology.AddPlace(secondPlace), Is.True);

        Assert.That(store.Add(firstTopology), Is.True);
        Assert.That(store.Add(secondTopology), Is.True);
        Assert.That(registry.TryGetLocalPlace(firstPlace.RuntimeId, out LocalPlaceRuntime resolvedFirst), Is.True);
        Assert.That(registry.TryGetLocalPlace(secondPlace.RuntimeId, out LocalPlaceRuntime resolvedSecond), Is.True);
        Assert.That(resolvedFirst, Is.SameAs(firstPlace));
        Assert.That(resolvedSecond, Is.SameAs(secondPlace));
        Assert.That(store.Topologies.Count, Is.EqualTo(2));
    }

    [Test]
    public void OwnerDuplicateDoesNotRegisterSecondTopologyMembers()
    {
        CityRuntime city = CreateRegisteredCity("duplicate-owner-city", "duplicate-owner-location", out RuntimeIdentityRegistry registry);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyRuntime firstTopology = CreateTopology(city, registry);
        LocalPlaceRuntime firstPlace = new LocalPlaceRuntime("duplicate-owner-first-place");
        Assert.That(firstTopology.AddPlace(firstPlace), Is.True);
        Assert.That(store.Add(firstTopology), Is.True);

        LocalTopologyRuntime secondTopology = CreateTopology(city, registry);
        LocalPlaceRuntime secondFirst = new LocalPlaceRuntime("duplicate-owner-second-place-a");
        LocalPlaceRuntime secondSecond = new LocalPlaceRuntime("duplicate-owner-second-place-b");
        LocalTopologyConnectionRuntime secondConnection = new LocalTopologyConnectionRuntime(
            "duplicate-owner-second-connection",
            secondFirst,
            secondSecond,
            1f);
        Assert.That(secondTopology.AddPlace(secondFirst), Is.True);
        Assert.That(secondTopology.AddPlace(secondSecond), Is.True);
        Assert.That(secondTopology.AddConnection(secondConnection), Is.True);

        Assert.That(store.TryAddTopology(secondTopology, out _), Is.False);
        Assert.That(registry.IsRuntimeIdAvailable(secondFirst.RuntimeId), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable(secondSecond.RuntimeId), Is.True);
        Assert.That(registry.IsRuntimeIdAvailable(secondConnection.RuntimeId), Is.True);
        Assert.That(store.Topologies.Count, Is.EqualTo(1));
    }

    [Test]
    public void CommittedTopologySupportsShortestPath()
    {
        ExplorableSiteRuntime site = CreateRegisteredSite("committed-path-site", "committed-path-location", out RuntimeIdentityRegistry registry);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyRuntime topology = CreateTopology(site, registry);
        LocalPlaceRuntime start = new LocalPlaceRuntime("committed-path-start");
        LocalPlaceRuntime middle = new LocalPlaceRuntime("committed-path-middle");
        LocalPlaceRuntime destination = new LocalPlaceRuntime("committed-path-destination");
        Assert.That(topology.AddPlace(start), Is.True);
        Assert.That(topology.AddPlace(middle), Is.True);
        Assert.That(topology.AddPlace(destination), Is.True);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime(
            "committed-path-first",
            start,
            middle,
            1f)), Is.True);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime(
            "committed-path-second",
            middle,
            destination,
            2f)), Is.True);
        Assert.That(store.Add(topology), Is.True);

        Assert.That(topology.TryFindShortestPath(start.RuntimeId, destination.RuntimeId, out LocalTopologyPath path), Is.True);
        Assert.That(path.LocalPlaceRuntimeIds, Is.EqualTo(new[]
        {
            start.RuntimeId,
            middle.RuntimeId,
            destination.RuntimeId
        }));
        Assert.That(path.LocalConnectionRuntimeIds, Is.EqualTo(new[]
        {
            "committed-path-first",
            "committed-path-second"
        }));
        Assert.That(path.TotalCost, Is.EqualTo(3f));
    }

    [Test]
    public void CommittedTopologyCanBeObservedByKnowledge()
    {
        ExplorableSiteRuntime site = CreateRegisteredSite("committed-knowledge-site", "committed-knowledge-location", out RuntimeIdentityRegistry registry);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LocalTopologyRuntime topology = CreateTopology(site, registry);
        LocalPlaceRuntime first = new LocalPlaceRuntime("committed-knowledge-first");
        LocalPlaceRuntime second = new LocalPlaceRuntime("committed-knowledge-second");
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            "committed-knowledge-connection",
            first,
            second,
            1f);
        Assert.That(topology.AddPlace(first), Is.True);
        Assert.That(topology.AddPlace(second), Is.True);
        Assert.That(topology.AddConnection(connection), Is.True);
        Assert.That(store.Add(topology), Is.True);

        NpcRuntime npc = new NpcRuntime("committed-knowledge-npc", SimulationTestFactory.CreateNpc("committed-knowledge-npc"));
        LocalTopologyKnowledgeSystem knowledgeSystem = new LocalTopologyKnowledgeSystem();
        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, first, 1L), Is.True);
        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, second, 1L), Is.True);
        Assert.That(knowledgeSystem.RecordDirectObservation(npc, topology, connection, 1L), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsLocalPlace(site.RuntimeId, first.RuntimeId), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsLocalPlace(site.RuntimeId, second.RuntimeId), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.KnowsConnection(site.RuntimeId, connection.RuntimeId), Is.True);
    }

    private static LocalTopologyRuntime CreateTopology(
        CityRuntime city,
        RuntimeIdentityRegistry registry)
    {
        return new LocalTopologyRuntime(LocalTopologyOwnerReference.ForCity(city), registry);
    }

    private static LocalTopologyRuntime CreateTopology(
        ExplorableSiteRuntime site,
        RuntimeIdentityRegistry registry)
    {
        return new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(site), registry);
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

    private static ExplorableSiteRuntime CreateRegisteredSite(
        string runtimeId,
        string locationRuntimeId,
        out RuntimeIdentityRegistry registry)
    {
        registry = new RuntimeIdentityRegistry();
        return CreateRegisteredSite(runtimeId, locationRuntimeId, registry);
    }

    private static ExplorableSiteRuntime CreateRegisteredSite(
        string runtimeId,
        string locationRuntimeId,
        RuntimeIdentityRegistry registry)
    {
        ExplorableSiteRuntime site = new ExplorableSiteRuntime(
            runtimeId,
            SimulationTestFactory.CreateExplorableSite("definition-" + runtimeId),
            new SpatialLocationRuntime(locationRuntimeId));
        Assert.That(registry.RegisterExplorableSite(site), Is.True);
        return site;
    }
}
