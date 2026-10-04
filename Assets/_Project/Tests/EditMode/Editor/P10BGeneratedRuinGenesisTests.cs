using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class P10BGeneratedRuinGenesisTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject simulationObject in simulationObjects)
            if (simulationObject != null) Object.DestroyImmediate(simulationObject);
        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void PrepareKeepsGeneratedOwnersPrivateUntilSuccessfulBatchPublication()
    {
        SimulationConfigData config = CreateP10BConfig();
        SpatialAuthorityStore authority = ComposeGeography(config);
        string p9Fingerprint = CreateP9Fingerprint(config);
        var identity = new RuntimeIdentityRegistry();
        var network = new SpatialNetworkRuntime(identity);
        var sites = new ExplorableSiteStore();
        var topologies = new LocalTopologyStore(identity);
        var publishedAnchors = new LegacySpatialAnchorBindingStore(authority);

        P10BGeneratedRuinGenesis.PreparedRuin prepared = P10BGeneratedRuinGenesis.Prepare(
            config, p9Fingerprint, authority, identity, out string prepareDiagnostic);

        Assert.That(prepared, Is.Not.Null, prepareDiagnostic);
        Assert.That(prepared.Site.SiteInstanceId, Is.EqualTo(prepared.Request.SiteInstanceId));
        Assert.That(prepared.Site.RuntimeId, Is.EqualTo(prepared.Request.SiteInstanceId));
        Assert.That(prepared.Topology.IsPublished, Is.False);
        Assert.That(network.LocationCount, Is.Zero);
        Assert.That(sites.Count, Is.Zero);
        Assert.That(topologies.Topologies, Is.Empty);
        Assert.That(GetSiteIdentityCardinality(identity), Is.Zero);

        Assert.That(P10BGeneratedRuinGenesis.TryCommitPreparedRuin(
            prepared, identity, network, sites, topologies, authority, publishedAnchors, out string commitDiagnostic),
            Is.True, commitDiagnostic);

        Assert.That(network.LocationCount, Is.EqualTo(1));
        Assert.That(sites.Count, Is.EqualTo(1));
        Assert.That(topologies.Topologies, Has.Count.EqualTo(1));
        Assert.That(prepared.Topology.IsPublished, Is.True);
        Assert.That(identity.TryGetExplorableSite(prepared.Site.RuntimeId, out ExplorableSiteRuntime publishedSite), Is.True);
        Assert.That(publishedSite, Is.SameAs(prepared.Site));
        Assert.That(authority.TryGetTopologyBinding(LocalTopologyOwnerKind.ExplorableSite,
            prepared.Site.RuntimeId, out SpatialLocalTopologyBinding binding), Is.True);
        Assert.That(binding.LocationId, Is.EqualTo(new LocationId(config.authoredLocationId)));
        Assert.That(publishedAnchors.ValidateInvariants().IsValid, Is.True);
        Assert.That(authority.ValidateInvariants(topologies).IsValid, Is.True);
    }

    [Test]
    public void GlobalMemberCollisionRejectsTheWholePreparedBatchBeforePublication()
    {
        SimulationConfigData config = CreateP10BConfig();
        SpatialAuthorityStore authority = ComposeGeography(config);
        var identity = new RuntimeIdentityRegistry();
        P10BGeneratedRuinGenesis.PreparedRuin prepared = P10BGeneratedRuinGenesis.Prepare(
            config, CreateP9Fingerprint(config), authority, identity, out string prepareDiagnostic);
        Assert.That(prepared, Is.Not.Null, prepareDiagnostic);

        LocalPlaceRuntime collision = new LocalPlaceRuntime(prepared.GeneratedTopology.Places[0].RuntimeId, "collision");
        Assert.That(identity.RegisterLocalPlace(collision), Is.True);
        var network = new SpatialNetworkRuntime(identity);
        var sites = new ExplorableSiteStore();
        var topologies = new LocalTopologyStore(identity);
        var publishedAnchors = new LegacySpatialAnchorBindingStore(authority);
        long authorityRevision = authority.Revision;

        Assert.That(P10BGeneratedRuinGenesis.TryCommitPreparedRuin(
            prepared, identity, network, sites, topologies, authority, publishedAnchors, out string diagnostic), Is.False);

        Assert.That(diagnostic, Does.Contain("LocalPlace"));
        Assert.That(network.LocationCount, Is.Zero);
        Assert.That(sites.Count, Is.Zero);
        Assert.That(topologies.Topologies, Is.Empty);
        Assert.That(prepared.Topology.IsPublished, Is.False);
        Assert.That(authority.Revision, Is.EqualTo(authorityRevision));
        Assert.That(authority.TryGetTopologyBinding(LocalTopologyOwnerKind.ExplorableSite,
            prepared.Site.RuntimeId, out _), Is.False);
        Assert.That(GetSiteIdentityCardinality(identity), Is.Zero);
    }

    [Test]
    public void EveryLaterPublicationFailureRollsBackAllOwnersAndStoreRevisions()
    {
        foreach (P10BGeneratedRuinGenesis.PublicationStep failedStep
            in System.Enum.GetValues(typeof(P10BGeneratedRuinGenesis.PublicationStep)))
        {
            SimulationConfigData config = CreateP10BConfig();
            SpatialAuthorityStore authority = ComposeGeography(config);
            var identity = new RuntimeIdentityRegistry();
            var network = new SpatialNetworkRuntime(identity);
            var sites = new ExplorableSiteStore();
            var topologies = new LocalTopologyStore(identity);
            var anchors = new LegacySpatialAnchorBindingStore(authority);
            P10BGeneratedRuinGenesis.PreparedRuin prepared = P10BGeneratedRuinGenesis.Prepare(
                config, CreateP9Fingerprint(config), authority, identity, out string prepareDiagnostic);
            Assert.That(prepared, Is.Not.Null, prepareDiagnostic);
            long identityRevision = identity.CensusRevision;
            long networkRevision = network.Revision;
            long siteRevision = sites.Revision;
            long anchorRevision = anchors.Revision;
            long authorityRevision = authority.Revision;

            P10BGeneratedRuinGenesis.PublicationStepCompletedForTests = step =>
            {
                if (step == failedStep) throw new System.InvalidOperationException("Injected " + step);
            };
            try
            {
                Assert.That(P10BGeneratedRuinGenesis.TryCommitPreparedRuin(
                    prepared, identity, network, sites, topologies, authority, anchors, out string diagnostic),
                    Is.False, failedStep.ToString());
                Assert.That(diagnostic, Does.Contain("rolled back"));
            }
            finally
            {
                P10BGeneratedRuinGenesis.PublicationStepCompletedForTests = null;
            }

            Assert.That(network.LocationCount, Is.Zero, failedStep.ToString());
            Assert.That(network.Revision, Is.EqualTo(networkRevision), failedStep.ToString());
            Assert.That(sites.Count, Is.Zero, failedStep.ToString());
            Assert.That(sites.Revision, Is.EqualTo(siteRevision), failedStep.ToString());
            Assert.That(topologies.Topologies, Is.Empty, failedStep.ToString());
            Assert.That(prepared.Topology.IsPublished, Is.False, failedStep.ToString());
            Assert.That(identity.CensusRevision, Is.EqualTo(identityRevision), failedStep.ToString());
            Assert.That(GetSiteIdentityCardinality(identity), Is.Zero, failedStep.ToString());
            Assert.That(identity.TryGetLocation(prepared.RuntimeLocation.RuntimeId, out _), Is.False, failedStep.ToString());
            Assert.That(identity.TryGetExplorableSite(prepared.Site.RuntimeId, out _), Is.False, failedStep.ToString());
            foreach (RuinLocalTopologyGeneration.Place place in prepared.GeneratedTopology.Places)
                Assert.That(identity.TryGetLocalPlace(place.RuntimeId, out _), Is.False, failedStep.ToString());
            foreach (RuinLocalTopologyGeneration.Connection connection in prepared.GeneratedTopology.Connections)
                Assert.That(identity.TryGetLocalConnection(connection.RuntimeId, out _), Is.False, failedStep.ToString());
            Assert.That(anchors.Count, Is.Zero, failedStep.ToString());
            Assert.That(anchors.Revision, Is.EqualTo(anchorRevision), failedStep.ToString());
            Assert.That(authority.TryGetTopologyBinding(LocalTopologyOwnerKind.ExplorableSite,
                prepared.Site.RuntimeId, out _), Is.False, failedStep.ToString());
            Assert.That(authority.Revision, Is.EqualTo(authorityRevision), failedStep.ToString());
        }
    }

    [Test]
    public void DailyAdmissionRejectsP10BBeforeCreatingRuntimeOrSiteStores()
    {
        SimulationConfigData config = CreateP10BConfig();
        GameObject gameObject = new GameObject("p10b-daily-admission-rejection");
        simulationObjects.Add(gameObject);
        TesteSimulacao simulation = gameObject.AddComponent<TesteSimulacao>();
        SetField(simulation, "simulationConfig", config);
        SetField(simulation, "runtimeAdmissionProfile", SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);

        Assert.Throws<System.InvalidOperationException>(() => simulation.Start());

        Assert.That(simulation.Bootstrap, Is.Null);
        Assert.That(simulation.Runtime, Is.Null);
        Assert.That(ReadField(simulation, "runtimeIdAllocator"), Is.Null);
        Assert.That(ReadField(simulation, "explorableSiteStore"), Is.Null);
        Assert.That(ReadField(simulation, "runtimeIdentityRegistry"), Is.Null);
    }

    [Test]
    public void DailyAdmissionContinuesToAcceptTheP10ACompatibilityProfile()
    {
        SimulationConfigData config = CreateP10BConfig();
        config.genesisProfileContractIdentity = null;
        config.p10bStableSiteKey = null;
        GameObject gameObject = new GameObject("p10a-daily-admission-compatibility");
        simulationObjects.Add(gameObject);
        TesteSimulacao simulation = gameObject.AddComponent<TesteSimulacao>();
        SetField(simulation, "simulationConfig", config);
        SetField(simulation, "runtimeAdmissionProfile", SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);

        simulation.Start();

        Assert.That(simulation.Bootstrap, Is.Not.Null);
        Assert.That(simulation.Bootstrap.ProfileContractIdentity,
            Is.EqualTo(SimulationGenesisPipeline.P10RuinProfileContractIdentity));
        Assert.That(simulation.Runtime.LocalTopologyStore.Topologies, Has.Count.EqualTo(1));
    }

    [Test]
    public void P10BProfilePublishesGeneratedTopologyOnTheSelectedCanonicalLocation()
    {
        SimulationConfigData config = CreateP10BConfig();
        GameObject gameObject = new GameObject("p10b-full-genesis-publication");
        simulationObjects.Add(gameObject);
        TesteSimulacao simulation = gameObject.AddComponent<TesteSimulacao>();
        SetField(simulation, "simulationConfig", config);
        MethodInfo initialize = typeof(TesteSimulacao).GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);

        initialize.Invoke(simulation, new object[] { null });

        Assert.That(simulation.Bootstrap, Is.Not.Null);
        Assert.That(simulation.Bootstrap.ProfileContractIdentity,
            Is.EqualTo(SimulationGenesisPipeline.P10BGeneratedRuinProfileContractIdentity));
        Assert.That(simulation.Bootstrap.Manifest.StageOrder,
            Does.Contain(SimulationGenesisPipeline.P10BGeneratedRuinStageId));
        Assert.That(simulation.Bootstrap.ExplorableSites.Sites, Has.Count.EqualTo(config.ExplorableSites.Count + 1));
        ExplorableSiteRuntime site = simulation.Bootstrap.ExplorableSites.Sites[
            simulation.Bootstrap.ExplorableSites.Sites.Count - 1];
        Assert.That(site.Definition, Is.SameAs(config.authoredP10RuinSite));
        Assert.That(site.SiteInstanceId, Does.StartWith("site-p10b-v1-"));
        Assert.That(simulation.Runtime.LocalTopologyStore, Is.Not.Null);
        Assert.That(simulation.Runtime.LocalTopologyStore.Topologies, Has.Count.EqualTo(1));
        LocalTopologyRuntime topology = simulation.Runtime.LocalTopologyStore.Topologies[0];
        Assert.That(topology.Owner.SemanticOwner.SiteInstanceId, Is.EqualTo(site.SiteInstanceId));
        Assert.That(simulation.Runtime.SpatialAuthorityStore.TryGetTopologyBinding(
            LocalTopologyOwnerKind.ExplorableSite, site.RuntimeId, out SpatialLocalTopologyBinding binding), Is.True);
        Assert.That(binding.LocationId, Is.EqualTo(new LocationId(config.authoredLocationId)));
        Assert.That(simulation.Runtime.SpatialAuthorityStore.ValidateInvariants(simulation.Runtime.LocalTopologyStore).IsValid, Is.True);
    }

    [Test]
    public void RepeatedP10BGenesisProducesTheSameProfileAndSiteIdentity()
    {
        TesteSimulacao first = CreateSimulation(CreateP10BConfig(), "p10b-repeat-first");
        TesteSimulacao second = CreateSimulation(CreateP10BConfig(), "p10b-repeat-second");
        MethodInfo initialize = typeof(TesteSimulacao).GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);

        initialize.Invoke(first, new object[] { null });
        initialize.Invoke(second, new object[] { null });

        Assert.That(second.Bootstrap.Manifest.Fingerprint, Is.EqualTo(first.Bootstrap.Manifest.Fingerprint));
        Assert.That(second.Bootstrap.ExplorableSites.Sites[0].SiteInstanceId,
            Is.EqualTo(first.Bootstrap.ExplorableSites.Sites[0].SiteInstanceId));
        Assert.That(second.Runtime.LocalTopologyStore.Topologies[0].Places.Count,
            Is.EqualTo(first.Runtime.LocalTopologyStore.Topologies[0].Places.Count));
        for (int i = 0; i < first.Runtime.LocalTopologyStore.Topologies[0].Places.Count; i++)
            Assert.That(second.Runtime.LocalTopologyStore.Topologies[0].Places[i].SemanticId,
                Is.EqualTo(first.Runtime.LocalTopologyStore.Topologies[0].Places[i].SemanticId));
    }

    [Test]
    public void TopologySemanticOwnersDistinguishInstancesOfOneDefinition()
    {
        var location = new LocationId("selected-location");
        LocalTopologySemanticOwnerReference first = LocalTopologySemanticOwnerReference.ForSiteInstanceId("site-instance-a", location);
        LocalTopologySemanticOwnerReference second = LocalTopologySemanticOwnerReference.ForSiteInstanceId("site-instance-b", location);

        Assert.That(first.StableKey, Is.Not.EqualTo(second.StableKey));
        Assert.That(first, Is.Not.EqualTo(second));
        Assert.That(LocalTopologySemanticOwnerReference.ForP10ALegacy("same-definition", location).StableKey,
            Is.EqualTo(new LocalTopologySemanticOwnerReference("same-definition", location).StableKey));
    }

    private static SimulationConfigData CreateP10BConfig()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.useAuthoredGeographyProfile = true;
        config.authoredHexId = "p10b-selected-hex";
        config.authoredHexQ = 0;
        config.authoredHexR = 0;
        config.authoredTerrainDefinitionId = "terrain/p10b-test";
        config.authoredTerrainRevisionToken = "p10b-test-v1";
        config.authoredLocationId = "p10b-selected-location";
        config.authoredScaleConventionId = "scale/p10b-test-v1";
        config.authoredScaleSourceIdentity = "profile/p10b-test";
        config.authoredScaleSourceVersion = "1";
        config.authoredDistancePerNeighborStep = "1";
        config.authoredScaleUnit = "km";
        config.authoredP10RuinSite = SimulationTestFactory.CreateExplorableSite("p10b-test-ruin", ExplorableSiteKind.Ruin);
        config.genesisProfileContractIdentity = SimulationGenesisPipeline.P10BGeneratedRuinProfileContractIdentity;
        config.p10bStableSiteKey = "test/site/ruin/one";
        return config;
    }

    private static SpatialAuthorityStore ComposeGeography(SimulationConfigData config)
    {
        var authority = new SpatialAuthorityStore();
        Assert.That(authority.TryComposeGeography(
            new SpatialGeographyDefinition(
                new SpatialWorldScaleContext(config.authoredScaleConventionId, config.authoredScaleSourceIdentity,
                    config.authoredScaleSourceVersion, 1m, config.authoredScaleUnit),
                new[] { new HexRecord(new HexId(config.authoredHexId), new HexCoordinate(0, 0),
                    new TerrainReference(new TerrainDefinitionId(config.authoredTerrainDefinitionId), config.authoredTerrainRevisionToken)) },
                new[] { new LocationRecord(new LocationId(config.authoredLocationId), new HexId(config.authoredHexId)) }),
            out SpatialAuthorityFailure failure), Is.True, failure.ToString());
        return authority;
    }

    private static string CreateP9Fingerprint(SimulationConfigData config)
    {
        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: config.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(config.Calendar, out _);
        return SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _);
    }

    private TesteSimulacao CreateSimulation(SimulationConfigData config, string name)
    {
        GameObject gameObject = new GameObject(name);
        simulationObjects.Add(gameObject);
        TesteSimulacao simulation = gameObject.AddComponent<TesteSimulacao>();
        SetField(simulation, "simulationConfig", config);
        return simulation;
    }

    private static int GetSiteIdentityCardinality(RuntimeIdentityRegistry identity)
    {
        foreach (IOwnerSectionCensusProvider provider in RuntimeIdentityRegistryCensusProvider.CreateProviders(identity))
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (witness.SectionId == RuntimeIdentityRegistryCensusProvider.ExplorableSitesSectionId)
                return witness.Cardinality;
        }
        Assert.Fail("Runtime identity registry omitted its ExplorableSite census section.");
        return -1;
    }

    private static void SetField(object target, string name, object value) =>
        typeof(TesteSimulacao).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static object ReadField(object target, string name) =>
        typeof(TesteSimulacao).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
}
