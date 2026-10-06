using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class P10BGeneratedRuinGenesisTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        P10BGeneratedRuinGenesis.PublicationStepCompletedForTests = null;
        P10BGeneratedRuinGenesis.PublicationMutationCompletedForTests = null;
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        P10BGeneratedRuinGenesis.PublicationStepCompletedForTests = null;
        P10BGeneratedRuinGenesis.PublicationMutationCompletedForTests = null;
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
    public void EveryInternalStoreMutationFailureRollsBackThePartialStoreAndEarlierOwners()
    {
        PublicationFixture successful = CreatePublicationFixture();
        var mutationPoints = new List<string>();
        P10BGeneratedRuinGenesis.PublicationMutationCompletedForTests = mutationPoints.Add;
        try
        {
            Assert.That(P10BGeneratedRuinGenesis.TryCommitPreparedRuin(
                successful.Prepared, successful.Identity, successful.Network, successful.Sites,
                successful.Topologies, successful.Authority, successful.Anchors, out string successDiagnostic),
                Is.True, successDiagnostic);
        }
        finally
        {
            P10BGeneratedRuinGenesis.PublicationMutationCompletedForTests = null;
        }
        Assert.That(mutationPoints, Is.Not.Empty);
        Assert.That(new HashSet<string>(mutationPoints, System.StringComparer.Ordinal).Count,
            Is.EqualTo(mutationPoints.Count), "Each mutation boundary must have a unique stable name.");

        foreach (string failedMutation in mutationPoints)
        {
            PublicationFixture fixture = CreatePublicationFixture();
            long identityRevision = fixture.Identity.CensusRevision;
            long networkRevision = fixture.Network.Revision;
            long siteRevision = fixture.Sites.Revision;
            long anchorRevision = fixture.Anchors.Revision;
            long authorityRevision = fixture.Authority.Revision;
            bool injected = false;
            P10BGeneratedRuinGenesis.PublicationMutationCompletedForTests = mutation =>
            {
                if (!injected && string.Equals(mutation, failedMutation, System.StringComparison.Ordinal))
                {
                    injected = true;
                    throw new System.InvalidOperationException("Injected partial store failure at " + failedMutation);
                }
            };
            try
            {
                Assert.That(P10BGeneratedRuinGenesis.TryCommitPreparedRuin(
                    fixture.Prepared, fixture.Identity, fixture.Network, fixture.Sites,
                    fixture.Topologies, fixture.Authority, fixture.Anchors, out string diagnostic),
                    Is.False, failedMutation);
                Assert.That(diagnostic, Does.Contain("atomic publication failed"), failedMutation);
            }
            finally
            {
                P10BGeneratedRuinGenesis.PublicationMutationCompletedForTests = null;
            }

            Assert.That(injected, Is.True, failedMutation);
            AssertPublicationUnchanged(fixture, identityRevision, networkRevision, siteRevision, anchorRevision,
                authorityRevision, failedMutation);
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
        int worldIdentityAllocations = 0;
        SetField(simulation, "worldIdentityAllocator", (System.Func<WorldId>)(() =>
        {
            worldIdentityAllocations++;
            return new WorldId(new System.Guid("12345678-1234-5678-9abc-def012345678"));
        }));

        Assert.Throws<System.InvalidOperationException>(() => simulation.Start());

        Assert.That(worldIdentityAllocations, Is.Zero, "P10-B admission must reject before WorldId allocation.");
        Assert.That(simulation.Bootstrap, Is.Null);
        Assert.That(simulation.Runtime, Is.Null);
        Assert.That(ReadField(simulation, "unpublishedWorldId"), Is.Null);
        Assert.That(ReadField(simulation, "draftComposition"), Is.Null);
        Assert.That(ReadField(simulation, "runtimeIdAllocator"), Is.Null);
        Assert.That(ReadField(simulation, "explorableSiteStore"), Is.Null);
        Assert.That(ReadField(simulation, "runtimeIdentityRegistry"), Is.Null);
        Assert.That(ReadField(simulation, "genesisSpatialAuthority"), Is.Null);
        Assert.That(ReadField(simulation, "spatialNetwork"), Is.Null);
        Assert.That(ReadField(simulation, "p10BPreparedRuin"), Is.Null);
    }

    [Test]
    public void DailyAdmissionRejectsTheSeparateP10ACompatibilityProfileBeforePublication()
    {
        SimulationConfigData config = CreateP10BConfig();
        config.genesisProfileContractIdentity = null;
        config.p10bStableSiteKey = null;
        GameObject gameObject = new GameObject("p10a-daily-admission-compatibility");
        simulationObjects.Add(gameObject);
        TesteSimulacao simulation = gameObject.AddComponent<TesteSimulacao>();
        SetField(simulation, "simulationConfig", config);
        SetField(simulation, "runtimeAdmissionProfile", SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        int worldIdentityAllocations = 0;
        SetField(simulation, "worldIdentityAllocator", (System.Func<WorldId>)(() =>
        {
            worldIdentityAllocations++;
            return new WorldId(new System.Guid("12345678-1234-5678-9abc-def012345678"));
        }));

        Assert.Throws<System.InvalidOperationException>(() => simulation.Start());

        Assert.That(worldIdentityAllocations, Is.Zero, "P10-A admission must reject before WorldId allocation.");
        Assert.That(simulation.Bootstrap, Is.Null);
        Assert.That(simulation.Runtime, Is.Null);
        Assert.That(ReadField(simulation, "unpublishedWorldId"), Is.Null);
        Assert.That(ReadField(simulation, "draftComposition"), Is.Null);
        Assert.That(ReadField(simulation, "runtimeIdAllocator"), Is.Null);
        Assert.That(ReadField(simulation, "explorableSiteStore"), Is.Null);
        Assert.That(ReadField(simulation, "runtimeIdentityRegistry"), Is.Null);
        Assert.That(ReadField(simulation, "genesisSpatialAuthority"), Is.Null);
        Assert.That(ReadField(simulation, "spatialNetwork"), Is.Null);
        Assert.That(ReadField(simulation, "p10BPreparedRuin"), Is.Null);
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
        P10BGeneratedRuinGenesis.PreparedRuin prepared =
            (P10BGeneratedRuinGenesis.PreparedRuin)ReadField(simulation, "p10BPreparedRuin");
        Assert.That(prepared, Is.Not.Null);
        Assert.That(simulation.Bootstrap.Manifest.Fingerprint,
            Is.EqualTo(prepared.GeneratedTopology.ComposedProfileFingerprint),
            "The end-to-end manifest must publish the generator's v1 composed-profile golden value.");
        Assert.That(simulation.Bootstrap.Manifest.Fingerprint,
            Is.EqualTo(FingerprintRecords(new[]
            {
                "p10b.genesis-profile/v1",
                prepared.Request.P9ProfileFingerprint,
                prepared.Request.RequestFingerprint,
                prepared.GeneratedTopology.GraphFingerprint
            })));
        Assert.That(simulation.Bootstrap.Manifest.Fingerprint,
            Is.Not.EqualTo(FingerprintRecords(simulation.Bootstrap.Manifest.CanonicalProvenanceRecords)),
            "The published profile fingerprint uses the designed P9/request/graph composition, not a generic provenance re-hash.");
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
    public void CaptureP10ALegacyCompatibilityGoldenValues()
    {
        SimulationConfigData config = CreateP10BConfig();
        config.genesisProfileContractIdentity = null;
        config.p10bStableSiteKey = null;
        TesteSimulacao simulation = CreateSimulation(config, "p10a-compatibility-golden");
        MethodInfo initialize = typeof(TesteSimulacao).GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
        initialize.Invoke(simulation, new object[] { null });

        SimulationGenesisManifest manifest = simulation.Bootstrap.Manifest;
        ExplorableSiteRuntime site = null;
        foreach (ExplorableSiteRuntime candidate in simulation.Bootstrap.ExplorableSites.Sites)
            if (candidate.DefinitionId == config.authoredP10RuinSite.DefinitionId) site = candidate;
        Assert.That(site, Is.Not.Null);
        Assert.That(simulation.Runtime.LocalTopologyStore.TryGetTopologyForSemanticOwner(
            new LocalTopologySemanticOwnerReference(config.authoredP10RuinSite.DefinitionId,
                new LocationId(config.authoredLocationId)), out LocalTopologyRuntime topology), Is.True);
        Assert.That(manifest.ContractIdentity, Is.EqualTo("unity-authored-bootstrap/p10-ruin-local-topology-v1"));
        Assert.That(manifest.StageOrder, Is.EqualTo(new[]
        {
            "p9.genesis.resolve-profile/v1",
            "p9.genesis.authored-world/v1",
            "p9.genesis.authored-geography/v1",
            "p9.genesis.authored-actors/v1",
            "p10.genesis.ruin-local-topology/v1",
            "p9.genesis.validate-profile/v1",
            "p9.genesis.publish/v1"
        }));
        Assert.That(manifest.CanonicalProvenanceRecords, Has.Count.EqualTo(70));
        Assert.That(FingerprintRecords(manifest.CanonicalProvenanceRecords),
            Is.EqualTo("8a0b535243b0c6e28400e89e9dea920db7c77a9424582adbe6aaa694468cf59a"));
        Assert.That(manifest.Fingerprint,
            Is.EqualTo("8a0b535243b0c6e28400e89e9dea920db7c77a9424582adbe6aaa694468cf59a"));
        Assert.That(site.DefinitionId, Is.EqualTo("p10b-test-ruin"));
        Assert.That(site.RuntimeId, Is.EqualTo("site-000001"));
        Assert.That(site.SiteInstanceId, Is.EqualTo("site-p10a-legacy-p10b-test-ruin"));
        Assert.That(site.Location.RuntimeId, Is.EqualTo("location-000001"));
        Assert.That(topology.Owner.OwnerRuntimeId, Is.EqualTo("site-000001"));
        Assert.That(topology.Owner.SemanticOwner.LegacyDefinitionId, Is.EqualTo("p10b-test-ruin"));
        Assert.That(topology.Owner.SemanticOwner.SiteInstanceId, Is.EqualTo("site-p10a-legacy-p10b-test-ruin"));
        Assert.That(topology.Owner.SemanticOwner.StableKey,
            Is.EqualTo("28:local-topology/site-owner/v114:p10b-test-ruin22:p10b-selected-location"));
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

    private static string FingerprintRecords(System.Collections.Generic.IReadOnlyList<string> records)
    {
        var bytes = new System.Collections.Generic.List<byte>();
        foreach (string record in records)
        {
            byte[] value = new System.Text.UTF8Encoding(false, true).GetBytes(record ?? string.Empty);
            bytes.Add((byte)(value.Length >> 24));
            bytes.Add((byte)(value.Length >> 16));
            bytes.Add((byte)(value.Length >> 8));
            bytes.Add((byte)value.Length);
            bytes.AddRange(value);
        }
        using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(bytes.ToArray());
            var hex = new System.Text.StringBuilder(hash.Length * 2);
            foreach (byte value in hash) hex.Append(value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            return hex.ToString();
        }
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

    private sealed class PublicationFixture
    {
        public SimulationConfigData Config;
        public SpatialAuthorityStore Authority;
        public RuntimeIdentityRegistry Identity;
        public SpatialNetworkRuntime Network;
        public ExplorableSiteStore Sites;
        public LocalTopologyStore Topologies;
        public LegacySpatialAnchorBindingStore Anchors;
        public P10BGeneratedRuinGenesis.PreparedRuin Prepared;
    }

    private static PublicationFixture CreatePublicationFixture()
    {
        var fixture = new PublicationFixture
        {
            Config = CreateP10BConfig(),
            Authority = null,
            Identity = new RuntimeIdentityRegistry()
        };
        fixture.Authority = ComposeGeography(fixture.Config);
        fixture.Network = new SpatialNetworkRuntime(fixture.Identity);
        fixture.Sites = new ExplorableSiteStore();
        fixture.Topologies = new LocalTopologyStore(fixture.Identity);
        fixture.Anchors = new LegacySpatialAnchorBindingStore(fixture.Authority);
        fixture.Prepared = P10BGeneratedRuinGenesis.Prepare(
            fixture.Config, CreateP9Fingerprint(fixture.Config), fixture.Authority, fixture.Identity,
            out string prepareDiagnostic);
        Assert.That(fixture.Prepared, Is.Not.Null, prepareDiagnostic);
        return fixture;
    }

    private static void AssertPublicationUnchanged(
        PublicationFixture fixture,
        long identityRevision,
        long networkRevision,
        long siteRevision,
        long anchorRevision,
        long authorityRevision,
        string context)
    {
        Assert.That(fixture.Network.LocationCount, Is.Zero, context);
        Assert.That(fixture.Network.Revision, Is.EqualTo(networkRevision), context);
        Assert.That(fixture.Sites.Count, Is.Zero, context);
        Assert.That(fixture.Sites.Revision, Is.EqualTo(siteRevision), context);
        Assert.That(fixture.Topologies.Topologies, Is.Empty, context);
        Assert.That(fixture.Prepared.Topology.IsPublished, Is.False, context);
        Assert.That(fixture.Identity.CensusRevision, Is.EqualTo(identityRevision), context);
        Assert.That(GetSiteIdentityCardinality(fixture.Identity), Is.Zero, context);
        Assert.That(fixture.Identity.TryGetLocation(fixture.Prepared.RuntimeLocation.RuntimeId, out _), Is.False, context);
        Assert.That(fixture.Identity.TryGetExplorableSite(fixture.Prepared.Site.RuntimeId, out _), Is.False, context);
        foreach (RuinLocalTopologyGeneration.Place place in fixture.Prepared.GeneratedTopology.Places)
            Assert.That(fixture.Identity.TryGetLocalPlace(place.RuntimeId, out _), Is.False, context);
        foreach (RuinLocalTopologyGeneration.Connection connection in fixture.Prepared.GeneratedTopology.Connections)
            Assert.That(fixture.Identity.TryGetLocalConnection(connection.RuntimeId, out _), Is.False, context);
        Assert.That(fixture.Anchors.Count, Is.Zero, context);
        Assert.That(fixture.Anchors.Revision, Is.EqualTo(anchorRevision), context);
        Assert.That(fixture.Authority.TryGetTopologyBinding(LocalTopologyOwnerKind.ExplorableSite,
            fixture.Prepared.Site.RuntimeId, out _), Is.False, context);
        Assert.That(fixture.Authority.Revision, Is.EqualTo(authorityRevision), context);
    }

    private static void SetField(object target, string name, object value) =>
        typeof(TesteSimulacao).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static object ReadField(object target, string name) =>
        typeof(TesteSimulacao).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
}
