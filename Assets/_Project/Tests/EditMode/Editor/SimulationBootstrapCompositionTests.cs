using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SimulationBootstrapCompositionTests
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
        {
            if (simulationObject != null)
            {
                Object.DestroyImmediate(simulationObject);
            }
        }

        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void DisabledCrimeBootstrapAdvancesExistingHiddenTimerWithoutRegisteringCrimeProvider()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData city = SimulationTestFactory.CreateCityData("disabled-crime-city");
        NpcData npcData = SimulationTestFactory.CreateNpc("disabled-crime-npc");
        NpcActionData hide = SimulationTestFactory.CreateAction(
            "disabled-crime-hide",
            NpcActionType.Hide,
            NpcActionCategory.Crime);
        NpcStatusData freeStatus = SimulationTestFactory.CreateStatus("disabled-crime-free");
        NpcStatusData wantedStatus = SimulationTestFactory.CreateStatus("disabled-crime-wanted");
        NpcStatusData arrestedStatus = SimulationTestFactory.CreateStatus("disabled-crime-arrested");
        NpcStatusData hiddenStatus = SimulationTestFactory.CreateStatus("disabled-crime-hidden");

        npcData.acoesPadrao.Add(new NPCDefaultAction
        {
            action = hide,
            baseUtility = 100f
        });
        config.Cities.Add(city);
        config.Npcs.Add(new NpcSimulationConfig
        {
            npc = npcData,
            startingCity = city
        });
        config.Actions.Add(hide);
        config.freeStatus = freeStatus;
        config.wantedStatus = wantedStatus;
        config.arrestedStatus = arrestedStatus;
        config.hiddenStatus = hiddenStatus;

        GameObject simulationObject = new GameObject("disabled-crime-bootstrap-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        FieldInfo configField = typeof(TesteSimulacao).GetField(
            "simulationConfig",
            BindingFlags.Instance | BindingFlags.NonPublic);
        configField.SetValue(simulation, config);

        simulation.Start();

        Assert.That(simulation.Runtime.Configuration.Crime.Enabled, Is.False);
        Assert.That(simulation.TryGetNpcRuntime("npc-000001", out NpcRuntime npc), Is.True);

        FieldInfo crimeField = typeof(TesteSimulacao).GetField(
            "crimeSystem",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo decisionField = typeof(TesteSimulacao).GetField(
            "npcDecisionSystem",
            BindingFlags.Instance | BindingFlags.NonPublic);
        CrimeSystem composedCrime = crimeField.GetValue(simulation) as CrimeSystem;
        NpcDecisionSystem decisions = decisionField.GetValue(simulation) as NpcDecisionSystem;

        Assert.That(composedCrime, Is.Not.Null);
        Assert.That(decisions, Is.Not.Null);
        Assert.That(decisions.HasProvider<CrimeSystem>(), Is.False);

        npc.HideForDays(1);
        npc.AddStatus(hiddenStatus);
        simulation.Runtime.AdvanceDays(2);

        Assert.That(npc.IsHidden, Is.False);
        Assert.That(npc.HiddenDaysRemaining, Is.EqualTo(0));
        Assert.That(npc.CurrentStatus.Contains(hiddenStatus), Is.False);
        Assert.That(simulation.Decisions.Decisions, Is.Empty);
    }

    [Test]
    public void AuthoredGenesisFingerprintIsStableAndPreservesCausalListOrder()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData city = SimulationTestFactory.CreateCityData("genesis-city");
        CityData unorderedCity = SimulationTestFactory.CreateCityData("genesis-unordered-city");
        NpcData npc = SimulationTestFactory.CreateNpc("genesis-npc");
        NpcActionData action = SimulationTestFactory.CreateAction("genesis-action", NpcActionType.Hide, NpcActionCategory.Crime);
        ItemData fingerprintItem = SimulationTestFactory.CreateItem("genesis-fingerprint-item", 10f);
        city.marketItems.Add(new MarketItemConfig { item = fingerprintItem, initialAmount = 4, desiredAmount = 7 });
        city.productionConfigs.Add(new CityProductionConfig { item = fingerprintItem, amountPerDay = 2 });
        city.productionConfigs.Add(new CityProductionConfig { item = fingerprintItem, amountPerDay = 5 });
        config.Cities.Add(city);
        config.Cities.Add(unorderedCity);
        config.Npcs.Add(new NpcSimulationConfig { npc = npc, startingCity = city });
        config.Actions.Add(action);
        config.ScheduledDirectives.Add(new ScheduledDirectiveConfig { actor = npc, action = action, absoluteDay = 1 });
        config.ScheduledDirectives.Add(new ScheduledDirectiveConfig { actor = npc, action = action, absoluteDay = 2 });

        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(contentOverrides: config.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(config.Calendar, out _);
        string first = SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _);
        string same = SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _);
        Assert.That(first, Is.EqualTo(same));
        config.Cities.Reverse();
        Assert.That(SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _), Is.EqualTo(first));
        string beforeProductionReorder = SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _);
        city.productionConfigs.Reverse();
        Assert.That(SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _), Is.Not.EqualTo(beforeProductionReorder));
        city.productionConfigs.Reverse();
        Assert.That(SimulationGenesisPipeline.ResolveStageOrder(), Is.EqualTo(new[]
        {
            "p9.genesis.resolve-profile/v1", "p9.genesis.authored-world/v1",
            "p9.genesis.authored-actors/v1", "p9.genesis.validate-profile/v1", "p9.genesis.publish/v1"
        }));

        config.ScheduledDirectives.Reverse();
        Assert.That(SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _), Is.Not.EqualTo(first));
        config.ScheduledDirectives.Reverse();
        config.travelCostPerDay += 1f;
        EffectiveSimulationConfiguration changedEffective = SimulationConfigurationResolver.ResolveOrThrow(contentOverrides: config.CreateConfigurationOverrides());
        Assert.That(SimulationGenesisPipeline.CreateFingerprint(config, changedEffective, calendar, out _), Is.Not.EqualTo(first));
        config.travelCostPerDay -= 1f;
        fingerprintItem.basePrice += 1f;
        EffectiveSimulationConfiguration restoredEffective = SimulationConfigurationResolver.ResolveOrThrow(contentOverrides: config.CreateConfigurationOverrides());
        Assert.That(SimulationGenesisPipeline.CreateFingerprint(config, restoredEffective, calendar, out _), Is.Not.EqualTo(first));
    }

    [Test]
    public void AuthoredGeographyProfilePublishesOneReconstructibleP8AuthorityBeforeSimulation()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        ConfigureGeography(config);
        GameObject simulationObject = new GameObject("p9b-authored-geography-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);

        simulation.Start();

        Assert.That(simulation.Bootstrap.ProfileContractIdentity, Is.EqualTo(SimulationGenesisPipeline.GeographyProfileContractIdentity));
        Assert.That(simulation.Bootstrap.Manifest.StageOrder, Does.Contain(SimulationGenesisPipeline.GeographyStageId));
        Assert.That(simulation.Bootstrap.SpatialAuthority.HexCount, Is.EqualTo(1));
        Assert.That(simulation.Bootstrap.SpatialAuthority.LocationCount, Is.EqualTo(1));
        Assert.That(simulation.Bootstrap.SpatialAuthority.HasGeography, Is.True);
        Assert.That(simulation.Bootstrap.SpatialAuthority.TryGet(new HexId("authored-hex-one"), out HexRecord hex), Is.True);
        Assert.That(hex.Coordinate, Is.EqualTo(new HexCoordinate(4, -2)));
        Assert.That(hex.TerrainDefinitionId.Value, Is.EqualTo("terrain/authored-fixture"));
        Assert.That(hex.AuthoredRevisionToken, Is.EqualTo("rev-7"));
        Assert.That(simulation.Bootstrap.SpatialAuthority.TryGet(new LocationId("authored-location-one"), out LocationRecord location), Is.True);
        Assert.That(location.AnchorHexId.Value, Is.EqualTo("authored-hex-one"));
        Assert.That(simulation.Bootstrap.SpatialAuthority.ScaleContext.DistancePerNeighborStep, Is.EqualTo(3.5m));
        Assert.That(simulation.Bootstrap.Manifest.CanonicalProvenanceRecords, Has.Some.Contains("authored-geography-stage"));
        Assert.That(simulation.Bootstrap.Manifest.AuthoredDefinitionIds, Does.Contain("hex/authored-hex-one"));
        Assert.That(simulation.Bootstrap.Manifest.AuthoredDefinitionIds, Does.Contain("location/authored-location-one"));
        Assert.That(simulation.History.HistoricalEvents, Is.Empty);
    }

    [Test]
    public void SelectedSampleSceneProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
        Assert.That(config, Is.Not.Null);
        Assert.That(config.useAuthoredGeographyProfile, Is.True);
        GameObject simulationObject = new GameObject("selected-sample-profile-p9b-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);

        simulation.Start();

        IReadOnlyList<IOwnerSectionCensusProvider> runtimeIdentityProviders =
            simulation.Bootstrap.RuntimeIdentityCensusProviders;
        string[] runtimeIdentitySectionIds =
        {
            RuntimeIdentityRegistryCensusProvider.NpcsSectionId,
            RuntimeIdentityRegistryCensusProvider.CitiesSectionId,
            RuntimeIdentityRegistryCensusProvider.LocationsSectionId,
            RuntimeIdentityRegistryCensusProvider.RoutesSectionId,
            RuntimeIdentityRegistryCensusProvider.ExplorableSitesSectionId,
            RuntimeIdentityRegistryCensusProvider.LocalPlacesSectionId,
            RuntimeIdentityRegistryCensusProvider.LocalConnectionsSectionId,
            RuntimeIdentityRegistryCensusProvider.NotableItemsSectionId
        };
        int[] expectedRuntimeIdentityCardinalities = { 10, 2, 2, 2, 0, 0, 0, 0 };
        Assert.That(runtimeIdentityProviders.Count, Is.EqualTo(runtimeIdentitySectionIds.Length));
        object runtimeIdentityOwner = null;
        for (int i = 0; i < runtimeIdentityProviders.Count; i++)
        {
            OwnerSectionCensusWitness witness = runtimeIdentityProviders[i].GetCurrentCensus();
            Assert.That(witness.SectionId, Is.EqualTo(runtimeIdentitySectionIds[i]));
            Assert.That(witness.SchemaVersion, Is.EqualTo(RuntimeIdentityRegistryCensusProvider.SchemaVersion));
            Assert.That(witness.Cardinality, Is.EqualTo(expectedRuntimeIdentityCardinalities[i]));
            Assert.That(witness.Revision, Is.EqualTo(16L));
            if (i == 0)
            {
                runtimeIdentityOwner = witness.OwnerInstanceIdentity;
            }
            else
            {
                Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(runtimeIdentityOwner));
            }
        }

        OwnerSectionCensusWitness[] repeatedIdentityWitnesses = runtimeIdentityProviders
            .Select(provider => provider.GetCurrentCensus())
            .ToArray();
        for (int i = 0; i < repeatedIdentityWitnesses.Length; i++)
        {
            Assert.That(repeatedIdentityWitnesses[i].OwnerInstanceIdentity, Is.SameAs(runtimeIdentityOwner));
            Assert.That(repeatedIdentityWitnesses[i].Cardinality, Is.EqualTo(expectedRuntimeIdentityCardinalities[i]));
            Assert.That(repeatedIdentityWitnesses[i].Revision, Is.EqualTo(16L));
        }
        OwnerSectionCensusWitness recordSequenceCensus =
            simulation.Bootstrap.SimulationRecordSequenceCensusProvider.GetCurrentCensus();
        Assert.That(recordSequenceCensus.SectionId, Is.EqualTo(SimulationRecordSequenceCensusProvider.SectionId));
        Assert.That(recordSequenceCensus.SchemaVersion, Is.EqualTo(SimulationRecordSequenceCensusProvider.SchemaVersion));
        Assert.That(recordSequenceCensus.Cardinality, Is.EqualTo(1));
        Assert.That(recordSequenceCensus.Revision, Is.Zero);
        OwnerSectionCensusWitness repeatedRecordSequenceCensus =
            simulation.Bootstrap.SimulationRecordSequenceCensusProvider.GetCurrentCensus();
        Assert.That(repeatedRecordSequenceCensus.OwnerInstanceIdentity,
            Is.SameAs(recordSequenceCensus.OwnerInstanceIdentity));
        Assert.That(repeatedRecordSequenceCensus.Revision, Is.Zero);

        OwnerSectionCensusWitness p11ActorChoices = simulation.Bootstrap.ActorChoiceInputCensusProvider.GetCurrentCensus();
        OwnerSectionCensusWitness temporalActorChoices = simulation.Bootstrap.ActorChoiceTemporalCensusProvider.GetCurrentCensus();
        Assert.That(p11ActorChoices.SectionId, Is.EqualTo(ActorChoiceP11CensusProvider.SectionId));
        Assert.That(p11ActorChoices.SchemaVersion, Is.EqualTo(ActorChoiceP11CensusProvider.SchemaVersion));
        Assert.That(p11ActorChoices.Cardinality, Is.Zero);
        Assert.That(p11ActorChoices.Revision, Is.Zero);
        Assert.That(temporalActorChoices.SectionId, Is.EqualTo(ActorChoiceTemporalCensusProvider.SectionId));
        Assert.That(temporalActorChoices.SchemaVersion, Is.EqualTo(ActorChoiceTemporalCensusProvider.SchemaVersion));
        Assert.That(temporalActorChoices.Cardinality, Is.Zero);
        Assert.That(temporalActorChoices.Revision, Is.Zero);
        Assert.That(temporalActorChoices.OwnerInstanceIdentity, Is.SameAs(p11ActorChoices.OwnerInstanceIdentity));
        Assert.That(simulation.Bootstrap.ActorChoiceInputCensusProvider.GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(p11ActorChoices.OwnerInstanceIdentity));

        IReadOnlyList<IOwnerSectionCensusProvider> runtimeIdAllocatorProviders =
            simulation.Bootstrap.RuntimeIdAllocatorCensusProviders;
        string[] runtimeIdAllocatorSectionIds =
        {
            RuntimeIdAllocatorCensusProvider.NpcsSectionId,
            RuntimeIdAllocatorCensusProvider.CitiesSectionId,
            RuntimeIdAllocatorCensusProvider.LocationsSectionId,
            RuntimeIdAllocatorCensusProvider.RoutesSectionId,
            RuntimeIdAllocatorCensusProvider.EventsSectionId,
            RuntimeIdAllocatorCensusProvider.DirectivesSectionId,
            RuntimeIdAllocatorCensusProvider.DecisionsSectionId,
            RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId,
            RuntimeIdAllocatorCensusProvider.OrganizationsSectionId,
            RuntimeIdAllocatorCensusProvider.ExplorableSitesSectionId,
            RuntimeIdAllocatorCensusProvider.ExpeditionsSectionId,
            RuntimeIdAllocatorCensusProvider.LocalPlacesSectionId,
            RuntimeIdAllocatorCensusProvider.LocalConnectionsSectionId,
            RuntimeIdAllocatorCensusProvider.NotableItemsSectionId
        };
        long[] expectedRuntimeIdAllocatorRevisions = { 10L, 2L, 2L, 2L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L };
        Assert.That(runtimeIdAllocatorProviders.Count, Is.EqualTo(runtimeIdAllocatorSectionIds.Length));
        object runtimeIdAllocatorOwner = null;
        for (int i = 0; i < runtimeIdAllocatorProviders.Count; i++)
        {
            OwnerSectionCensusWitness witness = runtimeIdAllocatorProviders[i].GetCurrentCensus();
            Assert.That(witness.SectionId, Is.EqualTo(runtimeIdAllocatorSectionIds[i]));
            Assert.That(witness.SchemaVersion, Is.EqualTo(RuntimeIdAllocatorCensusProvider.SchemaVersion));
            Assert.That(witness.Cardinality, Is.EqualTo(1));
            Assert.That(witness.Revision, Is.EqualTo(expectedRuntimeIdAllocatorRevisions[i]));
            if (i == 0)
            {
                runtimeIdAllocatorOwner = witness.OwnerInstanceIdentity;
            }
            else
            {
                Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(runtimeIdAllocatorOwner));
            }
        }

        OwnerSectionCensusWitness[] repeatedRuntimeIdAllocatorWitnesses = runtimeIdAllocatorProviders
            .Select(provider => provider.GetCurrentCensus())
            .ToArray();
        for (int i = 0; i < repeatedRuntimeIdAllocatorWitnesses.Length; i++)
        {
            Assert.That(repeatedRuntimeIdAllocatorWitnesses[i].OwnerInstanceIdentity, Is.SameAs(runtimeIdAllocatorOwner));
            Assert.That(repeatedRuntimeIdAllocatorWitnesses[i].Cardinality, Is.EqualTo(1));
            Assert.That(repeatedRuntimeIdAllocatorWitnesses[i].Revision, Is.EqualTo(expectedRuntimeIdAllocatorRevisions[i]));
        }

        IReadOnlyList<IOwnerSectionCensusProvider> armedForceProviders = simulation.Bootstrap.ArmedForceStoreCensusProviders;
        string[] armedForceSectionIds =
        {
            ArmedForceStoreCensusProvider.ForcesSectionId,
            ArmedForceStoreCensusProvider.ContingentsSectionId,
            ArmedForceStoreCensusProvider.RelevantPersonReferencesSectionId
        };
        Assert.That(armedForceProviders.Count, Is.EqualTo(armedForceSectionIds.Length));
        object armedForceOwner = simulation.Runtime.ArmedForceStore;
        for (int i = 0; i < armedForceProviders.Count; i++)
        {
            OwnerSectionCensusWitness witness = armedForceProviders[i].GetCurrentCensus();
            Assert.That(witness.SectionId, Is.EqualTo(armedForceSectionIds[i]));
            Assert.That(witness.SchemaVersion, Is.EqualTo(ArmedForceStoreCensusProvider.SchemaVersion));
            Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(armedForceOwner));
            Assert.That(witness.Cardinality, Is.Zero,
                "The selected daily profile composes the ArmedForceStore but leaves its three sections empty at day zero.");
            Assert.That(witness.Revision, Is.Zero);
        }

        OwnerSectionCensusWitness[] repeatedArmedForceWitnesses = armedForceProviders
            .Select(provider => provider.GetCurrentCensus())
            .ToArray();
        for (int i = 0; i < repeatedArmedForceWitnesses.Length; i++)
        {
            Assert.That(repeatedArmedForceWitnesses[i].OwnerInstanceIdentity, Is.SameAs(armedForceOwner));
            Assert.That(repeatedArmedForceWitnesses[i].Cardinality, Is.Zero);
            Assert.That(repeatedArmedForceWitnesses[i].Revision, Is.Zero);
        }

        SpatialAuthorityStore authority = simulation.Bootstrap.SpatialAuthority;
        SpatialHexCensusProvider hexCensusProvider = new SpatialHexCensusProvider(authority);
        OwnerSectionCensusWitness hexCensus = hexCensusProvider.GetCurrentCensus();
        Assert.That(hexCensus.SectionId, Is.EqualTo(SpatialHexCensusProvider.SectionId));
        Assert.That(hexCensus.SchemaVersion, Is.EqualTo(SpatialHexCensusProvider.SchemaVersion));
        Assert.That(hexCensus.OwnerInstanceIdentity, Is.SameAs(authority));
        Assert.That(hexCensus.Cardinality, Is.EqualTo(1));
        Assert.That(hexCensus.Revision, Is.EqualTo(1L));
        Assert.That(hexCensusProvider.GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(authority));

        SpatialLocationCensusProvider locationCensusProvider = new SpatialLocationCensusProvider(authority);
        OwnerSectionCensusWitness locationCensus = locationCensusProvider.GetCurrentCensus();
        Assert.That(locationCensus.SectionId, Is.EqualTo(SpatialLocationCensusProvider.SectionId));
        Assert.That(locationCensus.SchemaVersion, Is.EqualTo(SpatialLocationCensusProvider.SchemaVersion));
        Assert.That(locationCensus.OwnerInstanceIdentity, Is.SameAs(authority));
        Assert.That(locationCensus.Cardinality, Is.EqualTo(1));
        Assert.That(locationCensus.Revision, Is.EqualTo(1L));
        Assert.That(locationCensusProvider.GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(authority));

        SpatialScaleContextCensusProvider scaleCensusProvider = new SpatialScaleContextCensusProvider(authority);
        OwnerSectionCensusWitness scaleCensus = scaleCensusProvider.GetCurrentCensus();
        Assert.That(scaleCensus.SectionId, Is.EqualTo(SpatialScaleContextCensusProvider.SectionId));
        Assert.That(scaleCensus.SchemaVersion, Is.EqualTo(SpatialScaleContextCensusProvider.SchemaVersion));
        Assert.That(scaleCensus.OwnerInstanceIdentity, Is.SameAs(authority));
        Assert.That(scaleCensus.Cardinality, Is.EqualTo(1));
        Assert.That(scaleCensus.Revision, Is.EqualTo(1L));
        Assert.That(scaleCensusProvider.GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(authority));

        Assert.That(simulation.Bootstrap.ProfileContractIdentity, Is.EqualTo(SimulationGenesisPipeline.GeographyProfileContractIdentity));
        OwnerSectionCensusWitness occurrenceReceipts = simulation.Bootstrap.GetNpcDecisionOccurrenceReceiptCensus();
        Assert.That(occurrenceReceipts.SectionId, Is.EqualTo(NpcDecisionRecorder.OccurrenceReceiptSectionId));
        Assert.That(occurrenceReceipts.SchemaVersion, Is.EqualTo(NpcDecisionRecorder.OccurrenceReceiptSectionSchemaVersion));
        Assert.That(occurrenceReceipts.Cardinality, Is.Zero,
            "The selected daily profile composes the recorder but not the P18-D receipt writer.");
        Assert.That(occurrenceReceipts.Revision, Is.Zero);
        OwnerSectionCensusWitness repeatedOccurrenceReceipts = simulation.Bootstrap.GetNpcDecisionOccurrenceReceiptCensus();
        Assert.That(repeatedOccurrenceReceipts.OwnerInstanceIdentity, Is.SameAs(occurrenceReceipts.OwnerInstanceIdentity));
        Assert.That(repeatedOccurrenceReceipts.Revision, Is.EqualTo(occurrenceReceipts.Revision));
        OwnerSectionCensusWitness keyedSaleReceipts = simulation.Bootstrap.GetEconomyKeyedSaleReceiptCensus();
        Assert.That(keyedSaleReceipts.SectionId, Is.EqualTo(EconomyTransactionService.KeyedSaleReceiptSectionId));
        Assert.That(keyedSaleReceipts.SchemaVersion, Is.EqualTo(EconomyTransactionService.KeyedSaleReceiptSectionSchemaVersion));
        Assert.That(keyedSaleReceipts.Cardinality, Is.Zero,
            "The selected daily profile composes the keyed-sale receipt owner but does not invoke its P18-D consumer.");
        Assert.That(keyedSaleReceipts.Revision, Is.Zero);
        OwnerSectionCensusWitness repeatedKeyedSaleReceipts = simulation.Bootstrap.GetEconomyKeyedSaleReceiptCensus();
        Assert.That(repeatedKeyedSaleReceipts.OwnerInstanceIdentity, Is.SameAs(keyedSaleReceipts.OwnerInstanceIdentity));
        Assert.That(repeatedKeyedSaleReceipts.Revision, Is.EqualTo(keyedSaleReceipts.Revision));
        Assert.That(simulation.Runtime.ActorChoiceStore, Is.Not.Null,
            "The promoted P9-B bootstrap must retain the P11 actor-choice authority in the composed runtime.");
        Assert.That(authority.HexCount, Is.EqualTo(1));
        Assert.That(authority.LocationCount, Is.EqualTo(1));
        Assert.That(authority.TryGet(new HexId("hex/sample-origin"), out HexRecord hex), Is.True);
        Assert.That(hex.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
        Assert.That(hex.TerrainDefinitionId.Value, Is.EqualTo("terrain/sample-plains"));
        Assert.That(hex.AuthoredRevisionToken, Is.EqualTo("sample-world-v1"));
        Assert.That(authority.TryGet(new LocationId("location/sample-origin"), out LocationRecord location), Is.True);
        Assert.That(location.AnchorHexId.Value, Is.EqualTo("hex/sample-origin"));
        Assert.That(authority.ScaleContext.ResolvedConventionId, Is.EqualTo("world-scale/Simulation-GeneralTest/v1"));
        Assert.That(authority.ScaleContext.SourceIdentity, Is.EqualTo("profile/Simulation-GeneralTest"));
        Assert.That(authority.ScaleContext.SourceVersion, Is.EqualTo("1"));
        Assert.That(authority.ScaleContext.DistancePerNeighborStep, Is.EqualTo(1m));
        Assert.That(authority.ScaleContext.Unit, Is.EqualTo("km"));
        SimulationRuntime runtime = simulation.Bootstrap.Runtime;
        Assert.That(runtime.Cities, Has.Count.EqualTo(2));
        Assert.That(runtime.NpcRuntimes, Has.Count.EqualTo(10));
        Assert.That(runtime.PersonStore.Persons, Is.Empty);
        Assert.That(runtime.GenealogyRecords, Is.Empty);
        Assert.That(runtime.Cities.Sum(city => city.CurrentPopulation), Is.EqualTo(1800));
        Assert.That(runtime.Cities.Sum(city => city.Market.Items.Count), Is.EqualTo(10));
        Assert.That(runtime.Cities.Sum(city => city.Market.Items.Sum(item => item.Amount)), Is.EqualTo(1395));
        Assert.That(runtime.NpcRuntimes.Sum(npc => npc.Inventory.Items.Count), Is.EqualTo(2));
        Assert.That(runtime.NpcRuntimes.Sum(npc => npc.Inventory.Items.Sum(item => item.Amount)), Is.EqualTo(8));
        Assert.That(runtime.NpcRuntimes.Sum(npc => npc.SpatialKnowledge.KnownLocationRuntimeIds.Count), Is.EqualTo(20));
        Assert.That(runtime.NpcRuntimes.Sum(npc => npc.SpatialKnowledge.KnownRouteRuntimeIds.Count), Is.EqualTo(10));
        Assert.That(runtime.NpcRuntimes.All(npc => npc.SpatialKnowledge.Revision == 3), Is.True);
        Assert.That(runtime.ActorChoiceStore.Count, Is.Zero);
        Assert.That(simulation.Bootstrap.ScheduledDirectives.Directives, Is.Empty);
        Assert.That(simulation.Bootstrap.TravelParties.ActiveParties, Is.Empty);
        Assert.That(simulation.Bootstrap.Expeditions.ActiveExpeditions, Is.Empty);
        Assert.That(runtime.LocalTopologyStore, Is.Null,
            "The selected profile excludes P10 local topology and must classify it as not composed.");
        Assert.That(authority.Revision, Is.EqualTo(1),
            "The parent spatial revision is the P8-B passage invalidation stamp after the single P8-A geography commit.");
        Assert.That(authority.CrossingCount, Is.Zero);
        Assert.That(authority.PassageAuthority.Options, Is.Empty);
        Assert.That(authority.PassageAuthority.Barriers, Is.Empty);
        Assert.That(authority.PassageAuthority.OptionStates, Is.Empty);
        Assert.That(authority.PassageAuthority.BarrierStates, Is.Empty);
        SpatialPassageStateCensusProvider passageStateProvider =
            new SpatialPassageStateCensusProvider(authority);
        OwnerSectionCensusWitness passageState = passageStateProvider.GetCurrentCensus();
        Assert.That(passageState.SectionId, Is.EqualTo(SpatialPassageStateCensusProvider.SectionId));
        Assert.That(passageState.SchemaVersion, Is.EqualTo(SpatialPassageStateCensusProvider.SchemaVersion));
        Assert.That(passageState.OwnerInstanceIdentity, Is.SameAs(authority.PassageAuthority));
        Assert.That(passageState.Cardinality, Is.Zero);
        Assert.That(passageState.Revision, Is.EqualTo(1));
        Assert.That(passageStateProvider.GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(passageState.OwnerInstanceIdentity));
        SpatialCrossingCensusProvider crossingProvider = new SpatialCrossingCensusProvider(authority);
        OwnerSectionCensusWitness crossings = crossingProvider.GetCurrentCensus();
        Assert.That(crossings.SectionId, Is.EqualTo(SpatialCrossingCensusProvider.SectionId));
        Assert.That(crossings.SchemaVersion, Is.EqualTo(SpatialCrossingCensusProvider.SchemaVersion));
        Assert.That(crossings.OwnerInstanceIdentity, Is.SameAs(authority));
        Assert.That(crossings.Cardinality, Is.Zero);
        Assert.That(crossings.Revision, Is.EqualTo(1));
        Assert.That(crossingProvider.GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(crossings.OwnerInstanceIdentity));
        Assert.That(authority.ValidateInvariants().IsValid, Is.True);
        Assert.That(runtime.LegacySpatialAnchorBindingStore.Count, Is.Zero);
        Assert.That(runtime.LegacySpatialAnchorBindingStore.Revision, Is.Zero);
        LegacySpatialAnchorBindingCensusProvider anchorBindingsProvider =
            new LegacySpatialAnchorBindingCensusProvider(runtime.LegacySpatialAnchorBindingStore);
        OwnerSectionCensusWitness anchorBindings = anchorBindingsProvider.GetCurrentCensus();
        Assert.That(anchorBindings.SectionId, Is.EqualTo(LegacySpatialAnchorBindingCensusProvider.SectionId));
        Assert.That(anchorBindings.SchemaVersion, Is.EqualTo(LegacySpatialAnchorBindingCensusProvider.SchemaVersion));
        Assert.That(anchorBindings.OwnerInstanceIdentity, Is.SameAs(runtime.LegacySpatialAnchorBindingStore));
        Assert.That(anchorBindings.Cardinality, Is.Zero);
        Assert.That(anchorBindings.Revision, Is.Zero);
        Assert.That(anchorBindingsProvider.GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(anchorBindings.OwnerInstanceIdentity));
        Assert.That(runtime.PersonSpatialPositionStore.Count, Is.Zero);
        Assert.That(runtime.PersonSpatialPositionStore.Revision, Is.Zero);
        PersonSpatialPositionCensusProvider personPositionsProvider =
            new PersonSpatialPositionCensusProvider(runtime.PersonSpatialPositionStore);
        OwnerSectionCensusWitness personPositions = personPositionsProvider.GetCurrentCensus();
        Assert.That(personPositions.SectionId, Is.EqualTo(PersonSpatialPositionCensusProvider.SectionId));
        Assert.That(personPositions.SchemaVersion, Is.EqualTo(PersonSpatialPositionCensusProvider.SchemaVersion));
        Assert.That(personPositions.OwnerInstanceIdentity, Is.SameAs(runtime.PersonSpatialPositionStore));
        Assert.That(personPositions.Cardinality, Is.Zero);
        Assert.That(personPositions.Revision, Is.Zero);
        Assert.That(personPositionsProvider.GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(personPositions.OwnerInstanceIdentity));
        Assert.That(runtime.SpatialRouteKnowledgeStore.ObservationCount, Is.Zero);
        Assert.That(runtime.SpatialRouteKnowledgeStore.Revision, Is.Zero);
        Assert.That(runtime.PersonRoutePlanStore.PlanCount, Is.Zero);
        Assert.That(runtime.PersonRoutePlanStore.History, Is.Empty);
        Assert.That(runtime.PersonRoutePlanStore.Revision, Is.Zero);
        SpatialRouteObservationCensusProvider routeObservationsProvider =
            new SpatialRouteObservationCensusProvider(runtime.SpatialRouteKnowledgeStore);
        OwnerSectionCensusWitness routeObservations = routeObservationsProvider.GetCurrentCensus();
        Assert.That(routeObservations.SectionId, Is.EqualTo(SpatialRouteObservationCensusProvider.SectionId));
        Assert.That(routeObservations.SchemaVersion, Is.EqualTo(SpatialRouteObservationCensusProvider.SchemaVersion));
        Assert.That(routeObservations.OwnerInstanceIdentity, Is.SameAs(runtime.SpatialRouteKnowledgeStore));
        Assert.That(routeObservations.Cardinality, Is.Zero);
        Assert.That(routeObservations.Revision, Is.Zero);
        Assert.That(routeObservationsProvider.GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(routeObservations.OwnerInstanceIdentity));
        PersonRoutePlanHistoryCensusProvider routePlanHistoryProvider =
            new PersonRoutePlanHistoryCensusProvider(runtime.PersonRoutePlanStore);
        OwnerSectionCensusWitness routePlanHistory = routePlanHistoryProvider.GetCurrentCensus();
        Assert.That(routePlanHistory.SectionId, Is.EqualTo(PersonRoutePlanHistoryCensusProvider.SectionId));
        Assert.That(routePlanHistory.SchemaVersion, Is.EqualTo(PersonRoutePlanHistoryCensusProvider.SchemaVersion));
        Assert.That(routePlanHistory.OwnerInstanceIdentity, Is.SameAs(runtime.PersonRoutePlanStore));
        Assert.That(routePlanHistory.Cardinality, Is.Zero);
        Assert.That(routePlanHistory.Revision, Is.Zero);
        Assert.That(runtime.PersonRoutePlanStore.PlanCount, Is.EqualTo(runtime.PersonRoutePlanStore.History.Count));
        Assert.That(routePlanHistoryProvider.GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(routePlanHistory.OwnerInstanceIdentity));
        Assert.That(simulation.CurrentDay, Is.Zero);
        Assert.That(simulation.History.HistoricalEvents, Is.Empty);
    }

    [Test]
    public void AuthoredGeographyProfileRejectsIncompleteInputsBeforePublication()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        ConfigureGeography(config);
        config.authoredTerrainRevisionToken = " ";
        GameObject simulationObject = new GameObject("p9b-invalid-geography-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);

        Assert.Throws<System.InvalidOperationException>(() => simulation.Start());
        Assert.That(simulation.Bootstrap, Is.Null);
        Assert.That(simulation.Runtime, Is.Null);
    }

    [Test]
    public void AuthoredGeographyFingerprintIncludesEverySelectedGeographyInput()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        ConfigureGeography(config);
        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(contentOverrides: config.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(config.Calendar, out _);
        string fingerprint = SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out IReadOnlyList<string> records);
        Assert.That(records.Any(record => record.StartsWith("stage-input|", System.StringComparison.Ordinal)), Is.True);
        Assert.That(records.Any(record => record.StartsWith("stage-output|", System.StringComparison.Ordinal)), Is.True);
        Assert.That(records, Has.Some.EqualTo("edge:p9.genesis.resolve-profile/v1>" + SimulationGenesisPipeline.GeographyStageId));
        AssertGeographyFingerprintChanges(config, effective, calendar, fingerprint, value => config.authoredHexId = value, "authored-hex-two");
        AssertGeographyFingerprintChanges(config, effective, calendar, fingerprint, value => config.authoredHexQ = int.Parse(value), "5");
        AssertGeographyFingerprintChanges(config, effective, calendar, fingerprint, value => config.authoredHexR = int.Parse(value), "-1");
        AssertGeographyFingerprintChanges(config, effective, calendar, fingerprint, value => config.authoredTerrainDefinitionId = value, "terrain/other");
        AssertGeographyFingerprintChanges(config, effective, calendar, fingerprint, value => config.authoredTerrainRevisionToken = value, "rev-8");
        AssertGeographyFingerprintChanges(config, effective, calendar, fingerprint, value => config.authoredLocationId = value, "authored-location-two");
        AssertGeographyFingerprintChanges(config, effective, calendar, fingerprint, value => config.authoredScaleConventionId = value, "world-scale/other");
        AssertGeographyFingerprintChanges(config, effective, calendar, fingerprint, value => config.authoredScaleSourceIdentity = value, "profile/other");
        AssertGeographyFingerprintChanges(config, effective, calendar, fingerprint, value => config.authoredScaleSourceVersion = value, "2");
        AssertGeographyFingerprintChanges(config, effective, calendar, fingerprint, value => config.authoredDistancePerNeighborStep = value, "4.5");
        AssertGeographyFingerprintChanges(config, effective, calendar, fingerprint, value => config.authoredScaleUnit = value, "mile");
    }

    private static void AssertGeographyFingerprintChanges(
        SimulationConfigData config,
        EffectiveSimulationConfiguration effective,
        CalendarDefinition calendar,
        string original,
        System.Action<string> setValue,
        string changedValue)
    {
        setValue(changedValue);
        Assert.That(SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _), Is.Not.EqualTo(original));
        ConfigureGeography(config);
    }

    private static void ConfigureGeography(SimulationConfigData config)
    {
        config.useAuthoredGeographyProfile = true;
        config.authoredHexId = "authored-hex-one";
        config.authoredHexQ = 4;
        config.authoredHexR = -2;
        config.authoredTerrainDefinitionId = "terrain/authored-fixture";
        config.authoredTerrainRevisionToken = "rev-7";
        config.authoredLocationId = "authored-location-one";
        config.authoredScaleConventionId = "world-scale/authored-fixture";
        config.authoredScaleSourceIdentity = "profile/authored-fixture";
        config.authoredScaleSourceVersion = "1";
        config.authoredDistancePerNeighborStep = "3.5";
        config.authoredScaleUnit = "league";
    }

    [Test]
    public void RouteIdentityUsesTupleComponentsEvenWhenDefinitionIdsContainSeparator()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData firstOrigin = SimulationTestFactory.CreateCityData("route\u001fleft");
        CityData firstDestination = SimulationTestFactory.CreateCityData("right");
        CityData secondOrigin = SimulationTestFactory.CreateCityData("route");
        CityData secondDestination = SimulationTestFactory.CreateCityData("left\u001fright");
        firstOrigin.connections.Add(new CityConnection { destination = firstDestination, travelDays = 3 });
        secondOrigin.connections.Add(new CityConnection { destination = secondDestination, travelDays = 3 });
        config.Cities.Add(firstOrigin);
        config.Cities.Add(firstDestination);
        config.Cities.Add(secondOrigin);
        config.Cities.Add(secondDestination);

        Assert.DoesNotThrow(() => SimulationGenesisPipeline.ValidateProfile(config));
        secondOrigin.connections.Add(new CityConnection { destination = secondDestination, travelDays = 3 });
        Assert.Throws<System.InvalidOperationException>(() => SimulationGenesisPipeline.ValidateProfile(config));
    }

    [Test]
    public void DefaultAndJobWorkActionsMustReferenceTheExactSelectedDefinitionObject()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData city = SimulationTestFactory.CreateCityData("exact-action-city");
        NpcData npc = SimulationTestFactory.CreateNpc("exact-action-npc");
        NpcActionData selectedAction = SimulationTestFactory.CreateAction("same-action-id", NpcActionType.Hide);
        NpcActionData unselectedTwin = SimulationTestFactory.CreateAction("same-action-id", NpcActionType.Hide);
        config.Cities.Add(city);
        config.Npcs.Add(new NpcSimulationConfig { npc = npc, startingCity = city });
        config.Actions.Add(selectedAction);
        npc.acoesPadrao.Add(new NPCDefaultAction { action = unselectedTwin, baseUtility = 5f });

        Assert.Throws<System.InvalidOperationException>(() => SimulationGenesisPipeline.ValidateProfile(config));

        npc.acoesPadrao.Clear();
        npc.job.workAction = unselectedTwin;
        Assert.Throws<System.InvalidOperationException>(() => SimulationGenesisPipeline.ValidateProfile(config));

        npc.job.workAction = selectedAction;
        Assert.DoesNotThrow(() => SimulationGenesisPipeline.ValidateProfile(config));
    }

    [Test]
    public void SelectedNpcAndItemCapabilityAuthoringUseDomainValidators()
    {
        SimulationConfigData npcConfig = SimulationTestFactory.CreateSimulationConfig();
        CityData city = SimulationTestFactory.CreateCityData("invalid-capability-city");
        NpcData npc = SimulationTestFactory.CreateNpc("invalid-capability-npc");
        npc.capabilityValues.Add(new CapabilityAttributeValue(
            SimulationTestFactory.CreateCapabilityAttribute("invalid-capability-attribute"), -1f));
        npcConfig.Cities.Add(city);
        npcConfig.Npcs.Add(new NpcSimulationConfig { npc = npc, startingCity = city });
        System.InvalidOperationException npcFailure = Assert.Throws<System.InvalidOperationException>(
            () => SimulationGenesisPipeline.ValidateProfile(npcConfig));
        Assert.That(npcFailure.Message, Does.Contain("Selected NPC capability authoring is invalid"));

        SimulationConfigData itemConfig = SimulationTestFactory.CreateSimulationConfig();
        CityData itemCity = SimulationTestFactory.CreateCityData("invalid-item-city");
        ItemData item = SimulationTestFactory.CreateItem("invalid-item-capability");
        item.CapabilityModifiers.Add(new CapabilityAttributeModifier(
            SimulationTestFactory.CreateCapabilityAttribute("invalid-item-attribute"), -1f));
        itemCity.marketItems.Add(new MarketItemConfig { item = item });
        itemConfig.Cities.Add(itemCity);
        System.InvalidOperationException itemFailure = Assert.Throws<System.InvalidOperationException>(
            () => SimulationGenesisPipeline.ValidateProfile(itemConfig));
        Assert.That(itemFailure.Message, Does.Contain("Selected item capability authoring is invalid"));
    }

    [Test]
    public void ActionStatusWeightsAreIncludedInStableStatusIdentityValidation()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        NpcStatusData selectedStatus = SimulationTestFactory.CreateStatus("duplicate-weight-status");
        NpcStatusData unselectedTwin = SimulationTestFactory.CreateStatus("duplicate-weight-status");
        NpcActionData action = SimulationTestFactory.CreateAction("weighted-action", NpcActionType.Hide);
        action.statusModifiers.Add(new StatusWeightModifier { status = unselectedTwin, multiplier = 2f });
        config.Statuses.Add(selectedStatus);
        config.Actions.Add(action);

        Assert.Throws<System.InvalidOperationException>(() => SimulationGenesisPipeline.ValidateProfile(config));
    }

    [Test]
    public void InvalidAuthoredCalendarFailsBeforeHostCompositionIsPublished()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.calendar = new CalendarDefinition(0, 1, 1);
        GameObject simulationObject = new GameObject("invalid-calendar-bootstrap-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);

        Assert.Throws<System.InvalidOperationException>(() => simulation.Start());
        Assert.That(simulation.Bootstrap, Is.Null);
    }

    [Test]
    public void InvalidAuthoredProfileFailsBeforeHostCompositionIsPublished()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(SimulationTestFactory.CreateCityData("genesis-duplicate"));
        config.Cities.Add(SimulationTestFactory.CreateCityData("genesis-duplicate"));
        GameObject simulationObject = new GameObject("invalid-genesis-bootstrap-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);

        Assert.Throws<System.InvalidOperationException>(() => simulation.Start());
        Assert.That(simulation.Bootstrap, Is.Null);
        Assert.That(simulation.Runtime, Is.Null);
        Assert.That(simulation.History, Is.Null);
    }

    [Test]
    public void AuthoredGenesisPreservesPopulationDistinctRouteDurationsAndWarrantAccumulation()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData origin = SimulationTestFactory.CreateCityData("a-genesis-origin");
        CityData destination = SimulationTestFactory.CreateCityData("b-genesis-destination");
        ItemData authoredItem = SimulationTestFactory.CreateItem("genesis-production-item", 12f);
        origin.initialPopulation = 4321;
        origin.marketLiquidity = new MarketLiquidityConfig { liquidityMode = MarketLiquidityMode.AccountBacked, initialPurchasingPower = 91f };
        origin.populationConsumption = new PopulationConsumptionConfig { paymentMode = ConsumptionPaymentMode.AccountBacked, initialPurchasingPower = 37f };
        origin.productionConfigs.Add(new CityProductionConfig { item = authoredItem, amountPerDay = 3 });
        origin.connections.Add(new CityConnection { destination = destination, travelDays = 2 });
        origin.connections.Add(new CityConnection { destination = destination, travelDays = 5 });
        NpcData actor = SimulationTestFactory.CreateNpc("genesis-warrant-target");
        NpcStatusData authoredStatus = SimulationTestFactory.CreateStatus("genesis-warrant-default-status");
        config.wantedStatus = SimulationTestFactory.CreateStatus("genesis-wanted-status");
        actor.statusPadrao.Add(authoredStatus);
        config.Cities.Add(origin);
        config.Cities.Add(destination);
        config.Npcs.Add(new NpcSimulationConfig { npc = actor, startingCity = origin });
        config.InitialWarrants.Add(new InitialWantedRecordConfig { target = actor, city = origin, bounty = 11f, sentenceDays = 2 });
        config.InitialWarrants.Add(new InitialWantedRecordConfig { target = actor, city = origin, bounty = 7f, sentenceDays = 3 });
        GameObject simulationObject = new GameObject("complete-genesis-profile-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);

        simulation.Start();

        Assert.That(simulation.TryGetCityRuntime("city-000001", out CityRuntime originRuntime), Is.True);
        Assert.That(originRuntime.CurrentPopulation, Is.EqualTo(4321));
        Assert.That(originRuntime.MarketCounterparty.LiquidityMode, Is.EqualTo(MarketLiquidityMode.AccountBacked));
        Assert.That(originRuntime.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(91f));
        Assert.That(originRuntime.PopulationEconomy.PaymentMode, Is.EqualTo(ConsumptionPaymentMode.AccountBacked));
        Assert.That(originRuntime.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(37f));
        Assert.That(originRuntime.CityData.productionConfigs.Single().amountPerDay, Is.EqualTo(3));
        Assert.That(simulation.SpatialNetwork.Routes, Has.Count.EqualTo(2));
        Assert.That(simulation.SpatialNetwork.Routes, Has.Some.Property("TravelDays").EqualTo(2));
        Assert.That(simulation.SpatialNetwork.Routes, Has.Some.Property("TravelDays").EqualTo(5));
        Assert.That(simulation.TryGetNpcRuntime("npc-000001", out NpcRuntime actorRuntime), Is.True);
        Assert.That(actorRuntime.CurrentStatus, Is.EqualTo(new[] { authoredStatus, config.wantedStatus }));
        JusticeSystem justice = typeof(TesteSimulacao).GetField("justiceSystem", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(simulation) as JusticeSystem;
        WantedRecordRuntime warrant = justice.GetActiveWarrants(actorRuntime).Single();
        Assert.That(warrant.Bounty, Is.EqualTo(18f));
        Assert.That(warrant.SentenceDays, Is.EqualTo(5));
        Assert.That(simulation.History.HistoricalEvents, Is.Empty);
        Assert.That(simulation.Bootstrap.Manifest.EffectiveConfiguration.Travel.TravelCostPerDay, Is.EqualTo(config.travelCostPerDay));
        Assert.That(simulation.Bootstrap.Manifest.CanonicalProvenanceRecords, Has.Some.Contains("effective.travel"));
        Assert.That(simulation.Bootstrap.Manifest.AuthoredDefinitionIds, Does.Contain("city/a-genesis-origin"));
    }

    [Test]
    public void RuntimeCannotAdvanceAfterValidationFailureBeforePublication()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData city = SimulationTestFactory.CreateCityData("failure-atomic-city");
        NpcData npc = SimulationTestFactory.CreateNpc("failure-atomic-npc");
        config.Cities.Add(city);
        config.Npcs.Add(new NpcSimulationConfig { npc = npc, startingCity = city });
        GameObject simulationObject = new GameObject("post-validation-failure-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);
        System.Action<string> injectFailure = stageId =>
        {
            if (stageId == "p9.genesis.validate-profile/v1") throw new System.InvalidOperationException("injected post-validation failure");
        };
        MethodInfo initialize = typeof(TesteSimulacao).GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => initialize.Invoke(simulation, new object[] { injectFailure }));
        Assert.That(exception.InnerException, Is.TypeOf<System.InvalidOperationException>());
        Assert.That(simulation.Bootstrap, Is.Null);
        MethodInfo simulate = typeof(TesteSimulacao).GetMethod("Simulate", BindingFlags.Instance | BindingFlags.NonPublic);
        simulate.Invoke(simulation, new object[] { 2 });
        SimulationTime candidateTime = typeof(TesteSimulacao).GetField("simulationTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(simulation) as SimulationTime;
        HistoryStore candidateHistory = typeof(TesteSimulacao).GetField("historyStore", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(simulation) as HistoryStore;
        Assert.That(candidateTime.AbsoluteDay, Is.Zero);
        Assert.That(candidateHistory.HistoricalEvents, Is.Empty);
        Assert.That(simulation.CurrentDay, Is.Zero);
        Assert.That(simulation.History, Is.Null);
    }
}
