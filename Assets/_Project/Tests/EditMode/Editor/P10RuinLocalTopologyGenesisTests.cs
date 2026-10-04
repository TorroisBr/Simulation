using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class P10RuinLocalTopologyGenesisTests
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
            if (simulationObject != null) UnityEngine.Object.DestroyImmediate(simulationObject);
        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void P10StageFailureAfterCandidateCompositionLeavesBootstrapUnpublished()
    {
        SimulationConfigData config = CreateP10Config(ExplorableSiteKind.Ruin);
        TesteSimulacao simulation = CreateSimulation(config);
        Action<string> failAfterP10 = stageId =>
        {
            if (stageId == P10RuinLocalTopologyGenesis.StageId)
                throw new InvalidOperationException("injected failure after P10 candidate composition");
        };
        MethodInfo initialize = typeof(TesteSimulacao).GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
            () => initialize.Invoke(simulation, new object[] { failAfterP10 }));

        Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(exception.InnerException.Message, Does.Contain("injected failure"));
        Assert.That(simulation.Bootstrap, Is.Null);
        Assert.That(simulation.Runtime, Is.Null);
        Assert.That(simulation.ExplorableSites, Is.Null);
        Assert.That(simulation.History, Is.Null);
        Assert.That(simulation.CurrentDay, Is.Zero);
    }

    [Test]
    public void P10RejectsNonRuinAndDoesNotAcceptTheP9ProfileLocationByGuessing()
    {
        SimulationConfigData wrongKind = CreateP10Config(ExplorableSiteKind.Cave);
        Assert.Throws<InvalidOperationException>(() => SimulationGenesisPipeline.ValidateProfile(wrongKind));

        SimulationConfigData config = CreateP10Config(ExplorableSiteKind.Ruin);
        var spatialAuthority = new SpatialAuthorityStore();
        Assert.That(spatialAuthority.TryComposeGeography(
            new SpatialGeographyDefinition(
                new SpatialWorldScaleContext("scale/v1", "profile/test", "1", 1m, "km"),
                new[]
                {
                    new HexRecord(new HexId("selected-hex"), new HexCoordinate(0, 0),
                        new TerrainReference(new TerrainDefinitionId("terrain/test"), "test-v1"))
                },
                new[] { new LocationRecord(new LocationId("other-location"), new HexId("selected-hex")) }),
            out SpatialAuthorityFailure geographyFailure), Is.True, geographyFailure.ToString());

        Assert.Throws<InvalidOperationException>(() =>
            P10RuinLocalTopologyGenesis.CreateCandidate(config, spatialAuthority));
    }

    [Test]
    public void SemanticSiteOwnerAndTopologyIdsIgnoreRuntimeHandlesAndRejectDuplicateDefinitionOwners()
    {
        ExplorableSiteData definition = SimulationTestFactory.CreateExplorableSite("p10-semantic-site", ExplorableSiteKind.Ruin);
        ExplorableSiteRuntime firstSite = new ExplorableSiteRuntime(
            "site-runtime-first", definition, new SpatialLocationRuntime("location-runtime-first"), P10RuinLocalTopologyGenesis.CreateLegacySiteInstanceId(definition.DefinitionId));
        ExplorableSiteRuntime secondSite = new ExplorableSiteRuntime(
            "site-runtime-second", definition, new SpatialLocationRuntime("location-runtime-second"), P10RuinLocalTopologyGenesis.CreateLegacySiteInstanceId(definition.DefinitionId));
        LocationId canonicalLocation = new LocationId("p8-location-one");
        LocalTopologyOwnerReference firstOwner = LocalTopologyOwnerReference.ForSemanticExplorableSite(firstSite, canonicalLocation);
        LocalTopologyOwnerReference secondOwner = LocalTopologyOwnerReference.ForSemanticExplorableSite(secondSite, canonicalLocation);
        Assert.That(firstOwner.SemanticOwner.StableKey, Is.EqualTo(secondOwner.SemanticOwner.StableKey));

        var identity = new RuntimeIdentityRegistry();
        Assert.That(identity.RegisterExplorableSite(firstSite), Is.True);
        Assert.That(identity.RegisterExplorableSite(secondSite), Is.True);
        LocalTopologyStore store = new LocalTopologyStore(identity);
        LocalTopologyRuntime firstTopology = CreateTopology(firstOwner, identity, "runtime-place-first", "stable-place-first");
        Assert.That(store.TryAddTopology(firstTopology, out string firstDiagnostic), Is.True, firstDiagnostic);
        Assert.That(store.TryResolveSemanticOwnerRuntimeId(firstOwner.SemanticOwner, out string resolvedRuntimeId), Is.True);
        Assert.That(resolvedRuntimeId, Is.EqualTo(firstSite.RuntimeId));
        Assert.That(store.TryGetTopologyForSemanticOwner(
            new LocalTopologySemanticOwnerReference(definition.DefinitionId, new LocationId("wrong-location")), out _), Is.False);

        LocalTopologyRuntime duplicateDefinitionTopology = CreateTopology(
            LocalTopologyOwnerReference.ForSemanticExplorableSite(secondSite, new LocationId("another-location")),
            identity,
            "runtime-place-second",
            "stable-place-second");
        Assert.That(store.TryAddTopology(duplicateDefinitionTopology, out _), Is.False);
        Assert.That(store.Topologies, Has.Count.EqualTo(1));
        Assert.That(store.TryGetTopologyForOwner(secondSite.RuntimeId, out _), Is.False);
    }

    [Test]
    public void SemanticTopologyRejectsMissingOrDuplicateStableMemberIdsBeforeStorePublication()
    {
        ExplorableSiteData definition = SimulationTestFactory.CreateExplorableSite("p10-invalid-semantic-site", ExplorableSiteKind.Ruin);
        ExplorableSiteRuntime site = new ExplorableSiteRuntime(
            "site-runtime-invalid", definition, new SpatialLocationRuntime("location-runtime-invalid"), P10RuinLocalTopologyGenesis.CreateLegacySiteInstanceId(definition.DefinitionId));
        var identity = new RuntimeIdentityRegistry();
        Assert.That(identity.RegisterExplorableSite(site), Is.True);
        LocalTopologyOwnerReference owner = LocalTopologyOwnerReference.ForSemanticExplorableSite(
            site, new LocationId("p8-location-invalid"));
        LocalTopologyStore store = new LocalTopologyStore(identity);

        LocalTopologyRuntime missingIdTopology = new LocalTopologyRuntime(owner, identity);
        LocalPlaceRuntime missingIdPlace = new LocalPlaceRuntime("runtime-place-missing-semantic-id", "Entrance");
        Assert.That(missingIdTopology.AddPlace(missingIdPlace, isEntryPoint: true), Is.True);
        Assert.That(missingIdTopology.TryValidate(out string missingDiagnostic), Is.False);
        Assert.That(missingDiagnostic, Does.Contain("stable IDs"));
        Assert.That(store.TryAddTopology(missingIdTopology, out _), Is.False);

        LocalTopologyRuntime duplicateIdTopology = new LocalTopologyRuntime(owner, identity);
        const string duplicateStableId = "p10.local-topology/place/v1|same";
        Assert.That(duplicateIdTopology.AddPlace(
            new LocalPlaceRuntime("runtime-place-duplicate-one", "Entrance", semanticId: duplicateStableId), isEntryPoint: true), Is.True);
        Assert.That(duplicateIdTopology.AddPlace(
            new LocalPlaceRuntime("runtime-place-duplicate-two", "Courtyard", semanticId: duplicateStableId)), Is.True);
        Assert.That(duplicateIdTopology.TryValidate(out string duplicateDiagnostic), Is.False);
        Assert.That(duplicateDiagnostic, Does.Contain("stable IDs"));
        Assert.That(store.TryAddTopology(duplicateIdTopology, out _), Is.False);
        Assert.That(store.Topologies, Is.Empty);
    }

    [Test]
    public void P10SemanticTopologyIdentityIsStableWhenRuntimeAllocationChanges()
    {
        SimulationConfigData config = CreateP10Config(ExplorableSiteKind.Ruin);
        var first = ComposeCandidate(config, preallocateRuntimeIds: false);
        var second = ComposeCandidate(config, preallocateRuntimeIds: true);

        Assert.That(first.Site.RuntimeId, Is.Not.EqualTo(second.Site.RuntimeId));
        Assert.That(first.Topology.Owner.SemanticOwner.StableKey,
            Is.EqualTo(second.Topology.Owner.SemanticOwner.StableKey));
        Assert.That(GetPlaceSemanticIds(first.Topology), Is.EqualTo(GetPlaceSemanticIds(second.Topology)));
        Assert.That(GetConnectionSemanticIds(first.Topology), Is.EqualTo(GetConnectionSemanticIds(second.Topology)));
        Assert.That(first.Topology.Owner.OwnerRuntimeId, Is.Not.EqualTo(second.Topology.Owner.OwnerRuntimeId));
    }

    [Test]
    public void PublishedSemanticTopologyRejectsMissingAndGloballyDuplicateMemberIds()
    {
        ExplorableSiteData definition = SimulationTestFactory.CreateExplorableSite("p10-published-site", ExplorableSiteKind.Ruin);
        ExplorableSiteRuntime site = new ExplorableSiteRuntime(
            "site-runtime-published", definition, new SpatialLocationRuntime("location-runtime-published"), P10RuinLocalTopologyGenesis.CreateLegacySiteInstanceId(definition.DefinitionId));
        var identity = new RuntimeIdentityRegistry();
        Assert.That(identity.RegisterExplorableSite(site), Is.True);
        LocalTopologyOwnerReference owner = LocalTopologyOwnerReference.ForSemanticExplorableSite(
            site, new LocationId("p8-location-published"));
        LocalTopologyStore store = new LocalTopologyStore(identity);
        LocalTopologyRuntime topology = CreateTopology(owner, identity, "runtime-place-published", "stable-place-published");
        Assert.That(store.TryAddTopology(topology, out string publishDiagnostic), Is.True, publishDiagnostic);

        Assert.That(store.TryAddPlace(topology,
            new LocalPlaceRuntime("runtime-place-missing-id", "Missing ID"), out string missingDiagnostic), Is.False);
        Assert.That(missingDiagnostic, Does.Contain("stable ID"));
        Assert.That(store.TryAddPlace(topology,
            new LocalPlaceRuntime("runtime-place-duplicate-id", "Duplicate ID", semanticId: "stable-place-published"),
            out string duplicateDiagnostic), Is.False);
        Assert.That(duplicateDiagnostic, Does.Contain("duplicate stable semantic member ID"));
        Assert.That(topology.Places, Has.Count.EqualTo(1));
    }

    private TesteSimulacao CreateSimulation(SimulationConfigData config)
    {
        GameObject gameObject = new GameObject("p10-ruin-genesis-test");
        simulationObjects.Add(gameObject);
        TesteSimulacao simulation = gameObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        return simulation;
    }

    private static SimulationConfigData CreateP10Config(ExplorableSiteKind kind)
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.useAuthoredGeographyProfile = true;
        config.authoredHexId = "selected-hex";
        config.authoredHexQ = 0;
        config.authoredHexR = 0;
        config.authoredTerrainDefinitionId = "terrain/test";
        config.authoredTerrainRevisionToken = "test-v1";
        config.authoredLocationId = "selected-location";
        config.authoredScaleConventionId = "scale/v1";
        config.authoredScaleSourceIdentity = "profile/test";
        config.authoredScaleSourceVersion = "1";
        config.authoredDistancePerNeighborStep = "1";
        config.authoredScaleUnit = "km";
        config.authoredP10RuinSite = SimulationTestFactory.CreateExplorableSite("p10-test-ruin", kind);
        return config;
    }

    private static LocalTopologyRuntime CreateTopology(
        LocalTopologyOwnerReference owner,
        RuntimeIdentityRegistry identity,
        string placeRuntimeId,
        string stableId)
    {
        LocalTopologyRuntime topology = new LocalTopologyRuntime(owner, identity);
        LocalPlaceRuntime place = new LocalPlaceRuntime(placeRuntimeId, "Entrance", semanticId: stableId);
        Assert.That(topology.AddPlace(place, isEntryPoint: true), Is.True);
        return topology;
    }

    private static (ExplorableSiteRuntime Site, LocalTopologyRuntime Topology) ComposeCandidate(
        SimulationConfigData config,
        bool preallocateRuntimeIds)
    {
        var spatialAuthority = new SpatialAuthorityStore();
        Assert.That(spatialAuthority.TryComposeGeography(
            new SpatialGeographyDefinition(
                new SpatialWorldScaleContext("scale/v1", "profile/test", "1", 1m, "km"),
                new[]
                {
                    new HexRecord(new HexId(config.authoredHexId), new HexCoordinate(config.authoredHexQ, config.authoredHexR),
                        new TerrainReference(new TerrainDefinitionId(config.authoredTerrainDefinitionId), config.authoredTerrainRevisionToken))
                },
                new[] { new LocationRecord(new LocationId(config.authoredLocationId), new HexId(config.authoredHexId)) }),
            out SpatialAuthorityFailure geographyFailure), Is.True, geographyFailure.ToString());

        P10RuinLocalTopologyCandidate candidate = P10RuinLocalTopologyGenesis.CreateCandidate(config, spatialAuthority);
        var identity = new RuntimeIdentityRegistry();
        var allocator = new RuntimeIdAllocator();
        if (preallocateRuntimeIds)
        {
            allocator.AllocateLocationId();
            allocator.AllocateExplorableSiteId();
            allocator.AllocateLocalPlaceId();
            allocator.AllocateLocalConnectionId();
        }
        var spatialNetwork = new SpatialNetworkRuntime(identity);
        var siteStore = new ExplorableSiteStore();
        var topologyStore = new LocalTopologyStore(identity);
        Assert.That(P10RuinLocalTopologyGenesis.TryCompose(
            candidate, allocator, identity, spatialNetwork, siteStore, spatialAuthority,
            topologyStore, out ExplorableSiteRuntime site, out LocalTopologyRuntime topology,
            out LegacySpatialAnchorBindingStore bindings, out string diagnostic), Is.True, diagnostic);
        Assert.That(bindings.ValidateInvariants().IsValid, Is.True);
        return (site, topology);
    }

    private static List<string> GetPlaceSemanticIds(LocalTopologyRuntime topology)
    {
        var ids = new List<string>();
        foreach (LocalPlaceRuntime place in topology.Places) ids.Add(place.SemanticId);
        return ids;
    }

    private static List<string> GetConnectionSemanticIds(LocalTopologyRuntime topology)
    {
        var ids = new List<string>();
        foreach (LocalTopologyConnectionRuntime connection in topology.Connections) ids.Add(connection.SemanticId);
        return ids;
    }
}
