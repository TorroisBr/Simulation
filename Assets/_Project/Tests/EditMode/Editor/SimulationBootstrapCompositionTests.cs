using System.Collections;
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

    [Test]
    public void WorldIdUsesOneCanonicalNonEmptyGuidForm()
    {
        System.Guid guid = new System.Guid("00112233-4455-6677-8899-aabbccddeeff");
        var worldId = new WorldId(guid);

        Assert.That(worldId.Value, Is.EqualTo("world:00112233445566778899aabbccddeeff"));
        Assert.That(WorldId.Parse(worldId.Value), Is.EqualTo(worldId));
        Assert.That(WorldId.TryParse(worldId.Value, out WorldId parsed), Is.True);
        Assert.That(parsed, Is.EqualTo(worldId));
        Assert.That(WorldId.TryParse("WORLD:00112233445566778899aabbccddeeff", out _), Is.False);
        Assert.That(WorldId.TryParse("world:00112233445566778899AABBCCDDEEFF", out _), Is.False);
        Assert.That(WorldId.TryParse("world:00112233445566778899aabbccddeef", out _), Is.False);
        Assert.That(WorldId.TryParse("world:00000000000000000000000000000000", out _), Is.False);
        Assert.Throws<System.FormatException>(() => WorldId.Parse("world:invalid"));
        Assert.Throws<System.ArgumentException>(() => new WorldId(System.Guid.Empty));
    }

    [Test]
    public void GenesisCallbacksCannotObserveWorldBeforeFinalPublicationGate()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        ConfigureGeography(config);
        config.useFixedSimulationSeed = true;
        config.simulationSeed = 4217;
        GameObject simulationObject = new GameObject("world-identity-publication-gate-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        var identity = new WorldId(new System.Guid("12345678-1234-5678-9abc-def012345678"));
        int identityAllocationCount = 0;
        typeof(TesteSimulacao).GetField("worldIdentityAllocator", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, new System.Func<WorldId>(() =>
            {
                identityAllocationCount++;
                return identity;
            }));
        var completedStages = new List<string>();

        InvokeBootstrapInitialize(simulation, stageId =>
        {
            completedStages.Add(stageId);
            Assert.That(simulation.Bootstrap, Is.Null, stageId);
            Assert.That(simulation.Runtime, Is.Null, stageId);
            Assert.That(simulation.SimulationTime, Is.Null, stageId);
            Assert.That(simulation.Calendar, Is.Null, stageId);
            Assert.That(simulation.SpatialNetwork, Is.Null, stageId);
            Assert.That(simulation.DomainEventStore, Is.Null, stageId);
            Assert.That(simulation.History, Is.Null, stageId);
            Assert.That(simulation.ScheduledDirectives, Is.Null, stageId);
            Assert.That(simulation.Decisions, Is.Null, stageId);
            Assert.That(simulation.NpcChronicles, Is.Null, stageId);
            Assert.That(simulation.ChronicleFormatter, Is.Null, stageId);
            Assert.That(simulation.TravelParties, Is.Null, stageId);
            Assert.That(simulation.GroupTravel, Is.Null, stageId);
            Assert.That(simulation.ExplorableSites, Is.Null, stageId);
            Assert.That(simulation.Expeditions, Is.Null, stageId);
            Assert.That(simulation.ExpeditionRuntime, Is.Null, stageId);
            Assert.That(simulation.ExpeditionSystem, Is.Null, stageId);
            Assert.That(simulation.CurrentDay, Is.Zero, stageId);
            Assert.That(simulation.CurrentDate, Is.EqualTo(default(SimulationDate)), stageId);
            Assert.That(simulation.FullLog, Is.Empty, stageId);
            Assert.That(simulation.GetFullLog(), Is.Empty, stageId);
            Assert.That(simulation.TryStartTravelParty(null), Is.False, stageId);
            Assert.That(simulation.TryStartExpedition(null, null, out _), Is.False, stageId);
            Assert.That(simulation.TryGetNpcRuntime("npc-000001", out _), Is.False, stageId);
            Assert.That(simulation.TryGetCityRuntime("city-000001", out _), Is.False, stageId);
            Assert.That(simulation.TryGetSpatialLocation("location-000001", out _), Is.False, stageId);
            Assert.That(simulation.TryGetSpatialRoute("route-000001", out _), Is.False, stageId);
            Assert.That(simulation.TryGetExplorableSiteRuntime("site-000001", out _), Is.False, stageId);
            Assert.That(simulation.GetNpcChronicle("npc-000001"), Is.Empty, stageId);
            Assert.That(identityAllocationCount, Is.EqualTo(1), "identity is allocated before the first genesis stage");
        });

        Assert.That(completedStages, Is.EqualTo(SimulationGenesisPipeline.ResolveStageOrder(includeAuthoredGeography: true)));
        Assert.That(simulation.Bootstrap, Is.Not.Null);
        Assert.That(simulation.Bootstrap.WorldId, Is.SameAs(identity));
        Assert.That(simulation.Runtime.WorldId, Is.SameAs(identity));
        Assert.That(simulation.Bootstrap.Runtime.WorldId, Is.SameAs(identity));
        Assert.That(identityAllocationCount, Is.EqualTo(1));
    }

    [Test]
    public void SameSeedWorldsReceiveDistinctStableIdentitiesWithoutChangingSeededRandomDraws()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        ConfigureGeography(config);
        config.useFixedSimulationSeed = true;
        config.simulationSeed = 8451;
        GameObject firstObject = new GameObject("world-identity-first-same-seed-test");
        GameObject secondObject = new GameObject("world-identity-second-same-seed-test");
        simulationObjects.Add(firstObject);
        simulationObjects.Add(secondObject);
        TesteSimulacao first = firstObject.AddComponent<TesteSimulacao>();
        TesteSimulacao second = secondObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(first, config);
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(second, config);

        first.Start();
        second.Start();

        WorldId firstIdentity = first.Bootstrap.WorldId;
        WorldId secondIdentity = second.Bootstrap.WorldId;
        Assert.That(first.Runtime.WorldId, Is.SameAs(firstIdentity));
        Assert.That(second.Runtime.WorldId, Is.SameAs(secondIdentity));
        Assert.That(firstIdentity, Is.Not.EqualTo(secondIdentity));
        Assert.That(first.Bootstrap.ProfileFingerprint, Is.EqualTo(second.Bootstrap.ProfileFingerprint));
        IAuthoritativeRandomSource firstRandom = ReadPrivateField<IAuthoritativeRandomSource>(first, "authoritativeRandomSource");
        IAuthoritativeRandomSource secondRandom = ReadPrivateField<IAuthoritativeRandomSource>(second, "authoritativeRandomSource");
        Assert.That(firstRandom.NextUnit("world-identity-independent-stream"),
            Is.EqualTo(secondRandom.NextUnit("world-identity-independent-stream")));

        first.Runtime.AdvanceDay();
        Assert.That(first.Bootstrap.WorldId, Is.SameAs(firstIdentity));
        Assert.That(second.Bootstrap.WorldId, Is.SameAs(secondIdentity));
    }

    [Test]
    public void FailedPublishCallbackDiscardsIdentityAndPermanentlyLatchesBootstrap()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        ConfigureGeography(config);
        GameObject simulationObject = new GameObject("world-identity-failed-publication-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        int identityAllocationCount = 0;
        WorldId identity = new WorldId(new System.Guid("87654321-4321-8765-cba9-876543210fed"));

        typeof(TesteSimulacao).GetField("worldIdentityAllocator", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, new System.Func<WorldId>(() =>
            {
                identityAllocationCount++;
                return identity;
            }));
        var retainedPublicAccessors = new List<object>();
        TargetInvocationException failure = Assert.Throws<TargetInvocationException>(() =>
            InvokeBootstrapInitialize(simulation, stageId =>
            {
                Assert.That(simulation.Bootstrap, Is.Null, stageId);
                Assert.That(simulation.Runtime, Is.Null, stageId);
                Assert.That(simulation.SimulationTime, Is.Null, stageId);
                Assert.That(simulation.FullLog, Is.Empty, stageId);
                if (stageId == "p9.genesis.publish/v1")
                {
                    retainedPublicAccessors.AddRange(new object[]
                    {
                        simulation.Bootstrap, simulation.Runtime, simulation.SimulationTime, simulation.Calendar,
                        simulation.SpatialNetwork, simulation.DomainEventStore, simulation.History,
                        simulation.ScheduledDirectives, simulation.Decisions, simulation.NpcChronicles,
                        simulation.ChronicleFormatter, simulation.TravelParties, simulation.GroupTravel,
                        simulation.ExplorableSites, simulation.Expeditions, simulation.ExpeditionRuntime,
                        simulation.ExpeditionSystem, simulation.FullLog, simulation.GetFullLog(),
                        simulation.CurrentDay, simulation.CurrentDate, simulation.GetNpcChronicle("npc-000001")
                    });
                    throw new System.InvalidOperationException("injected publish callback failure");
                }
            }));

        Assert.That(failure.InnerException, Is.TypeOf<System.InvalidOperationException>());
        Assert.That(retainedPublicAccessors.OfType<WorldId>(), Is.Empty);
        Assert.That(retainedPublicAccessors.OfType<SimulationBootstrapComposition>(), Is.Empty);
        Assert.That(retainedPublicAccessors.OfType<SimulationRuntime>(), Is.Empty);
        Assert.That(simulation.Bootstrap, Is.Null);
        Assert.That(simulation.Runtime, Is.Null);
        Assert.That(typeof(TesteSimulacao).GetField("draftComposition", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(simulation), Is.Null);
        Assert.That(identityAllocationCount, Is.EqualTo(1));

        int retryCallbackCount = 0;
        InvokeBootstrapInitialize(simulation, _ => retryCallbackCount++);
        Assert.That(retryCallbackCount, Is.Zero);
        Assert.That(identityAllocationCount, Is.EqualTo(1));
        Assert.That(simulation.Bootstrap, Is.Null);
    }

    [Test]
    public void UnityBootstrapDailyV1PreDraftFailureNeverPublishesIdentityOrRetries()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        ConfigureGeography(config);
        GameObject simulationObject = new GameObject("world-identity-daily-pre-draft-failure-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);

        System.Threading.Thread ownerThread = System.Threading.Thread.CurrentThread;
        typeof(TesteSimulacao).GetField("bootstrapStartThread", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, ownerThread);
        typeof(TesteSimulacao).GetField("bootstrapStartThreadId", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, ownerThread.ManagedThreadId);
        typeof(TesteSimulacao).GetField("runtimeAdmissionContext", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, new SimulationRuntimeAdmissionContext(
                SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                ownerThread, ownerThread.ManagedThreadId));

        int identityAllocationCount = 0;
        WorldId identity = new WorldId(new System.Guid("fedcba98-7654-3210-fedc-ba9876543210"));
        typeof(TesteSimulacao).GetField("worldIdentityAllocator", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, new System.Func<WorldId>(() =>
            {
                identityAllocationCount++;
                return identity;
            }));

        TargetInvocationException failure = Assert.Throws<TargetInvocationException>(() =>
            InvokeBootstrapInitialize(simulation, stageId =>
            {
                AssertPublicWorldAccessorsUnavailable(simulation, stageId);
                if (stageId == "p9.genesis.validate-profile/v1")
                    throw new System.InvalidOperationException("injected selected-profile pre-draft failure");
            }));

        Assert.That(failure.InnerException, Is.TypeOf<System.InvalidOperationException>());
        AssertPublicWorldAccessorsUnavailable(simulation, "after failure");
        Assert.That(identityAllocationCount, Is.EqualTo(1));
        Assert.That(typeof(TesteSimulacao).GetField("unpublishedWorldId", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(simulation), Is.Null);
        Assert.That(typeof(TesteSimulacao).GetField("draftComposition", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(simulation), Is.Null);
        Assert.That(typeof(TesteSimulacao).GetField("publishedComposition", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(simulation), Is.Null);
        Assert.That(typeof(TesteSimulacao).GetField("worldPublished", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(simulation), Is.EqualTo(false));

        simulation.Start();
        int retryCallbackCount = 0;
        InvokeBootstrapInitialize(simulation, _ => retryCallbackCount++);
        Assert.That(retryCallbackCount, Is.Zero);
        Assert.That(identityAllocationCount, Is.EqualTo(1));
        AssertPublicWorldAccessorsUnavailable(simulation, "after retry attempts");
    }

    [Test]
    public void UnsupportedRuntimeProfileExposesUnavailableFactualReadSurface()
    {
        const string factionCapabilityId = "simulation.faction-truth/v1";
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        GameObject simulationObject = new GameObject("frb-unavailable-profile-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);

        simulation.Start();

        Assert.That(simulation.Bootstrap, Is.Not.Null);
        Assert.That(simulation.Bootstrap.FactualReads, Is.Not.Null);
        Assert.That(simulation.Bootstrap.FactualReads.TryCaptureCoherent(
            out FactualReadCapture capture,
            factionCapabilityId), Is.False);
        Assert.That(capture.IsCoherent, Is.False);
        Assert.That(capture.TryGet<FactionTruthFacts>(
            factionCapabilityId,
            out FactReadResult<FactionTruthFacts> result), Is.True);
        Assert.That(result.Status, Is.EqualTo(FactReadStatus.Unavailable));
    }

    [Test]
    public void SelectedDailyProfilePublishesCoherentFactualReadOnlyAfterBootstrapCloses()
    {
        const string factionCapabilityId = "simulation.faction-truth/v1";
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        ConfigureGeography(config);
        GameObject simulationObject = new GameObject("frb-live-publication-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        System.Threading.Thread ownerThread = System.Threading.Thread.CurrentThread;
        typeof(TesteSimulacao).GetField("bootstrapStartThread", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, ownerThread);
        typeof(TesteSimulacao).GetField("bootstrapStartThreadId", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, ownerThread.ManagedThreadId);
        typeof(TesteSimulacao).GetField("runtimeAdmissionContext", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, new SimulationRuntimeAdmissionContext(
                SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                ownerThread,
                ownerThread.ManagedThreadId));

        FactualReadCoordinator draftReads = null;
        InvokeBootstrapInitialize(simulation, stageId =>
        {
            Assert.That(simulation.Bootstrap, Is.Null, stageId);
            if (stageId != "p9.genesis.publish/v1") return;

            SimulationBootstrapComposition draft = (SimulationBootstrapComposition)typeof(TesteSimulacao)
                .GetField("draftComposition", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(simulation);
            Assert.That(draft, Is.Not.Null);
            draftReads = draft.FactualReads;
            Assert.That(draftReads, Is.Not.Null);
            Assert.That(draftReads.TryCaptureCoherent(
                out FactualReadCapture unpublishedCapture,
                factionCapabilityId), Is.False);
            Assert.That(unpublishedCapture.IsCoherent, Is.False);
            Assert.That(unpublishedCapture.TryGet<FactionTruthFacts>(
                factionCapabilityId,
                out FactReadResult<FactionTruthFacts> unpublishedResult), Is.True);
            Assert.That(unpublishedResult.Status, Is.EqualTo(FactReadStatus.Unavailable));
        });

        Assert.That(simulation.Bootstrap, Is.Not.Null);
        Assert.That(simulation.Runtime.StructureStore, Is.Null,
            "UnityBootstrap-Daily-v1 does not compose the P15-A proving owner.");
        Assert.That(simulation.Bootstrap.FactualReads, Is.SameAs(draftReads));
        Assert.That(simulation.Bootstrap.WorldId, Is.SameAs(simulation.Runtime.WorldId));
        Assert.That(simulation.Bootstrap.FactualReads.TryCaptureCoherent(
            out FactualReadCapture initialCapture,
            factionCapabilityId,
            "faction/collection/v1"), Is.True);
        Assert.That(initialCapture.IsCoherent, Is.True);
        Assert.That(initialCapture.LogicalBoundary, Is.EqualTo(simulation.CurrentDay));
        Assert.That(initialCapture.FactionStoreRevision,
            Is.EqualTo(((FactionStore)GetRuntimeOwner(simulation.Runtime, "factionStore")).Revision));
        Assert.That(initialCapture.PersonStoreRevision, Is.EqualTo(simulation.Runtime.PersonStore.Revision));
        Assert.That(initialCapture.TryGet<FactionTruthFacts>(
            factionCapabilityId,
            out FactReadResult<FactionTruthFacts> factionFactsResult), Is.True);
        Assert.That(factionFactsResult.Status, Is.EqualTo(FactReadStatus.Present));
        Assert.That(factionFactsResult.Value, Is.Not.Null);
        Assert.That(initialCapture.TryGet<string>(
            "faction/collection/v1",
            out FactReadResult<string> unsupportedCapability), Is.True);
        Assert.That(unsupportedCapability.Status, Is.EqualTo(FactReadStatus.Unsupported));

        Assert.That(simulation.Runtime.TryAcquireAdvanceLease(out SimulationRuntime.AdvanceLease lease), Is.True);
        using (lease)
        {
            Assert.That(simulation.Bootstrap.FactualReads.TryCaptureCoherent(
                out FactualReadCapture busyCapture,
                factionCapabilityId), Is.False);
            Assert.That(busyCapture.IsCoherent, Is.False);
        }

        FactualReadCapture offThreadCapture = null;
        bool offThreadSucceeded = true;
        System.Threading.Thread captureThread = new System.Threading.Thread(() =>
        {
            offThreadSucceeded = simulation.Bootstrap.FactualReads.TryCaptureCoherent(
                out offThreadCapture,
                factionCapabilityId);
        });
        captureThread.Start();
        Assert.That(captureThread.Join(System.TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(offThreadSucceeded, Is.False);
        Assert.That(offThreadCapture.IsCoherent, Is.False);
        Assert.That(simulation.Bootstrap.FactualReads.TryCaptureCoherent(
            out FactualReadCapture ownerThreadCapture,
            factionCapabilityId), Is.True);
        Assert.That(ownerThreadCapture.IsCoherent, Is.True);

        simulation.Runtime.AdvanceDay();
        Assert.That(simulation.Bootstrap.FactualReads.TryCaptureCoherent(
            out FactualReadCapture advancedCapture,
            factionCapabilityId), Is.True);
        Assert.That(advancedCapture.LogicalBoundary, Is.EqualTo(1L));

        simulation.Runtime.FaultRuntimeAdmission();
        Assert.That(simulation.Bootstrap.FactualReads.TryCaptureCoherent(
            out FactualReadCapture faultedCapture,
            factionCapabilityId), Is.False);
        Assert.That(faultedCapture.IsCoherent, Is.False);
    }

    private static void AssertPublicWorldAccessorsUnavailable(TesteSimulacao simulation, string context)
    {
        Assert.That(simulation.Bootstrap, Is.Null, context);
        Assert.That(simulation.Runtime, Is.Null, context);
        Assert.That(simulation.SimulationTime, Is.Null, context);
        Assert.That(simulation.Calendar, Is.Null, context);
        Assert.That(simulation.SpatialNetwork, Is.Null, context);
        Assert.That(simulation.DomainEventStore, Is.Null, context);
        Assert.That(simulation.History, Is.Null, context);
        Assert.That(simulation.ScheduledDirectives, Is.Null, context);
        Assert.That(simulation.Decisions, Is.Null, context);
        Assert.That(simulation.NpcChronicles, Is.Null, context);
        Assert.That(simulation.ChronicleFormatter, Is.Null, context);
        Assert.That(simulation.TravelParties, Is.Null, context);
        Assert.That(simulation.GroupTravel, Is.Null, context);
        Assert.That(simulation.ExplorableSites, Is.Null, context);
        Assert.That(simulation.Expeditions, Is.Null, context);
        Assert.That(simulation.ExpeditionRuntime, Is.Null, context);
        Assert.That(simulation.ExpeditionSystem, Is.Null, context);
        Assert.That(simulation.FullLog, Is.Empty, context);
        Assert.That(simulation.GetFullLog(), Is.Empty, context);
        Assert.That(simulation.CurrentDay, Is.Zero, context);
        Assert.That(simulation.CurrentDate, Is.EqualTo(default(SimulationDate)), context);
        Assert.That(simulation.TryGetNpcRuntime("npc-000001", out _), Is.False, context);
        Assert.That(simulation.TryGetCityRuntime("city-000001", out _), Is.False, context);
        Assert.That(simulation.TryGetSpatialLocation("location-000001", out _), Is.False, context);
        Assert.That(simulation.TryGetSpatialRoute("route-000001", out _), Is.False, context);
        Assert.That(simulation.TryGetExplorableSiteRuntime("site-000001", out _), Is.False, context);
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
    public void SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        Assert.That(config.useAuthoredGeographyProfile, Is.True);
        Assert.That(config.authoredP10RuinSite, Is.Null);
        GameObject simulationObject = new GameObject("selected-sample-profile-p9b-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);

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

        SimulationRuntime runtime = simulation.Bootstrap.Runtime;
        IReadOnlyList<IOwnerSectionCensusProvider> spatialNetworkProviders =
            simulation.Bootstrap.SpatialNetworkCensusProviders;
        string[] spatialNetworkSectionIds =
        {
            SpatialNetworkCensusProvider.LocationsSectionId,
            SpatialNetworkCensusProvider.RoutesSectionId
        };
        int[] expectedSpatialNetworkCardinalities = { 2, 2 };
        Assert.That(spatialNetworkProviders.Count, Is.EqualTo(spatialNetworkSectionIds.Length));
        for (int i = 0; i < spatialNetworkProviders.Count; i++)
        {
            OwnerSectionCensusWitness witness = spatialNetworkProviders[i].GetCurrentCensus();
            Assert.That(witness.SectionId, Is.EqualTo(spatialNetworkSectionIds[i]));
            Assert.That(witness.SchemaVersion, Is.EqualTo(SpatialNetworkCensusProvider.SchemaVersion));
            Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(simulation.Bootstrap.SpatialNetwork));
            Assert.That(witness.OwnerInstanceIdentity, Is.Not.SameAs(runtimeIdentityOwner));
            Assert.That(witness.Cardinality, Is.EqualTo(expectedSpatialNetworkCardinalities[i]));
            Assert.That(witness.Revision, Is.EqualTo(4L));
        }

        IReadOnlyList<IOwnerSectionCensusProvider> spatialKnowledgeProviders =
            simulation.Bootstrap.SpatialKnowledgeCensusProviders;
        string[] expectedSpatialKnowledgeSectionIds = runtime.NpcRuntimes
            .OrderBy(npc => npc.RuntimeId, System.StringComparer.Ordinal)
            .SelectMany(npc => new[]
            {
                SpatialKnowledgeCensusProvider.LocationsSectionPrefix + npc.RuntimeId,
                SpatialKnowledgeCensusProvider.RoutesSectionPrefix + npc.RuntimeId
            })
            .ToArray();
        Assert.That(spatialKnowledgeProviders.Count, Is.EqualTo(20));
        Assert.That(spatialKnowledgeProviders.Select(provider => provider.GetCurrentCensus().SectionId),
            Is.EqualTo(expectedSpatialKnowledgeSectionIds));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure npcCensusFailure), Is.True,
            npcCensusFailure.ToString());
        ContinuationCensusProtocol censusProtocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);
        HashSet<string> expectedOperations = (HashSet<string>)typeof(ContinuationCensusProtocol)
            .GetField("expectedOperations", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(censusProtocol);
        Assert.That(expectedOperations, Does.Contain("p12.institution-office.owner-commit"));
        Assert.That(expectedOperations, Does.Not.Contain("runtime.institution-office.owner-commit"));
        IDictionary expectedCensusSections = (IDictionary)typeof(ContinuationCensusProtocol)
            .GetField("expectedSections", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(censusProtocol);
        Assert.That(expectedCensusSections.Count, Is.EqualTo(239),
            "The current selected ten-NPC/two-City Daily-v1 composition has 233 sections before its two exact-zero receipt owners and four Institution/Office owner sections.");
        Assert.That(expectedCensusSections.Contains(NpcDecisionRecorder.OccurrenceReceiptSectionId), Is.True);
        Assert.That(expectedCensusSections.Contains(EconomyTransactionService.KeyedSaleReceiptSectionId), Is.True);
        string[] institutionOfficeSectionIds =
        {
            InstitutionOfficeCensusProvider.InstitutionsSectionId,
            InstitutionOfficeCensusProvider.OfficesSectionId,
            InstitutionOfficeCensusProvider.IncumbenciesSectionId,
            InstitutionOfficeCensusProvider.TenuresSectionId
        };
        for (int i = 0; i < institutionOfficeSectionIds.Length; i++)
        {
            Assert.That(expectedCensusSections.Contains(institutionOfficeSectionIds[i]), Is.True);
            Assert.That(((OwnerSectionContract)expectedCensusSections[institutionOfficeSectionIds[i]]).Role,
                Is.EqualTo(OwnerSectionRole.Required));
        }
        Assert.That(((OwnerSectionContract)expectedCensusSections[NpcDecisionRecorder.OccurrenceReceiptSectionId]).Role,
            Is.EqualTo(OwnerSectionRole.ExplicitlyEmpty));
        Assert.That(((OwnerSectionContract)expectedCensusSections[EconomyTransactionService.KeyedSaleReceiptSectionId]).Role,
            Is.EqualTo(OwnerSectionRole.ExplicitlyEmpty));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long npcCensusEpoch, out ContinuationCensusFailure npcEpochFailure), Is.True,
            npcEpochFailure.ToString());
        Assert.That(npcCensusEpoch, Is.Zero);
        IReadOnlyList<IOwnerSectionCensusProvider> moneyAccountProviders =
            simulation.Bootstrap.Runtime.MoneyAccountCensusProviders;
        NpcRuntime[] moneyAccountOwners = runtime.NpcRuntimes
            .OrderBy(npc => npc.RuntimeId, System.StringComparer.Ordinal)
            .ToArray();
        Assert.That(moneyAccountProviders.Count, Is.EqualTo(10));
        Assert.That(moneyAccountProviders.Count, Is.EqualTo(moneyAccountOwners.Length));
        for (int i = 0; i < moneyAccountOwners.Length; i++)
        {
            OwnerSectionCensusWitness witness = moneyAccountProviders[i].GetCurrentCensus();
            Assert.That(witness.SectionId, Is.EqualTo(NpcMoneyAccountCensusProvider.SectionPrefix + moneyAccountOwners[i].RuntimeId));
            Assert.That(witness.SchemaVersion, Is.EqualTo(NpcMoneyAccountCensusProvider.SchemaVersion));
            Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(moneyAccountOwners[i].MoneyAccount));
            Assert.That(witness.Cardinality, Is.EqualTo(1));
            Assert.That(witness.Revision, Is.EqualTo(moneyAccountOwners[i].MoneyAccount.Revision));
        }
        IReadOnlyList<IOwnerSectionCensusProvider> inventoryProviders = simulation.Bootstrap.Runtime.InventoryCensusProviders;
        NpcRuntime[] inventoryOwners = runtime.NpcRuntimes.OrderBy(npc => npc.RuntimeId, System.StringComparer.Ordinal).ToArray();
        Assert.That(inventoryProviders.Count, Is.EqualTo(inventoryOwners.Length));
        for (int i = 0; i < inventoryOwners.Length; i++)
        {
            OwnerSectionCensusWitness inventoryWitness = inventoryProviders[i].GetCurrentCensus();
            Assert.That(inventoryWitness.SectionId, Is.EqualTo(NpcInventoryCensusProvider.SectionPrefix + inventoryOwners[i].RuntimeId));
            Assert.That(inventoryWitness.SchemaVersion, Is.EqualTo(NpcInventoryCensusProvider.SchemaVersion));
            Assert.That(inventoryWitness.OwnerInstanceIdentity, Is.SameAs(inventoryOwners[i].Inventory));
            Assert.That(inventoryWitness.Cardinality, Is.EqualTo(inventoryOwners[i].Inventory.Items.Count));
            Assert.That(inventoryWitness.Revision, Is.EqualTo(inventoryOwners[i].Inventory.Revision));
        }
        IReadOnlyList<IOwnerSectionCensusProvider> npcKnowledgeProviders =
            simulation.Bootstrap.NpcKnowledgeCensusProviders;
        NpcRuntime[] knowledgeOwners = runtime.NpcRuntimes
            .OrderBy(npc => npc.RuntimeId, System.StringComparer.Ordinal)
            .ToArray();
        string[] knowledgeSectionPrefixes =
        {
            "p12f.explorable-site-knowledge/",
            "p12f.local-topology-knowledge.places/",
            "p12f.local-topology-knowledge.connections/",
            "p12f.adventure-intel.opposition/",
            "p12f.adventure-intel.notable-items/",
            "p12f.adventure-intel.common-resources/",
            "p12f.adventure-intel.access/",
            "p12f.commercial-knowledge.markets/",
            "p12f.commercial-knowledge.liquidity/",
            "p12f.commercial-knowledge.share-receipts/"
        };
        Assert.That(npcKnowledgeProviders.Count, Is.EqualTo(knowledgeOwners.Length * knowledgeSectionPrefixes.Length));
        int travelingMerchantCount = 0;
        int travelingMerchantsWithMarketKnowledge = 0;
        for (int npcIndex = 0; npcIndex < knowledgeOwners.Length; npcIndex++)
        {
            NpcRuntime npc = knowledgeOwners[npcIndex];
            CommercialKnowledgeRuntime commercial = ReadPrivateField<CommercialKnowledgeRuntime>(npc, "commercialKnowledge");
            List<CommercialKnowledgeShareReceipt> commercialShareReceipts =
                ReadPrivateField<List<CommercialKnowledgeShareReceipt>>(commercial, "shareReceipts");
            int marketCount = commercial.Observations.Count;
            int liquidityCount = commercial.LiquidityObservations.Count;
            int shareReceiptCount = commercialShareReceipts.Count;
            long commercialRevision = commercial.Revision;

            object[] typedOwners =
            {
                ReadPrivateField<ExplorableSiteKnowledgeRuntime>(npc, "explorableSiteKnowledge"),
                ReadPrivateField<LocalTopologyKnowledgeRuntime>(npc, "localTopologyKnowledge"),
                ReadPrivateField<LocalTopologyKnowledgeRuntime>(npc, "localTopologyKnowledge"),
                ReadPrivateField<AdventureSiteIntelKnowledgeRuntime>(npc, "adventureSiteIntelKnowledge"),
                ReadPrivateField<AdventureSiteIntelKnowledgeRuntime>(npc, "adventureSiteIntelKnowledge"),
                ReadPrivateField<AdventureSiteIntelKnowledgeRuntime>(npc, "adventureSiteIntelKnowledge"),
                ReadPrivateField<AdventureSiteIntelKnowledgeRuntime>(npc, "adventureSiteIntelKnowledge"),
                commercial,
                commercial,
                commercial
            };
            int[] expectedCardinalities =
            {
                ((ExplorableSiteKnowledgeRuntime)typedOwners[0]).Observations.Count,
                ((LocalTopologyKnowledgeRuntime)typedOwners[1]).PlaceObservations.Count,
                ((LocalTopologyKnowledgeRuntime)typedOwners[2]).ConnectionObservations.Count,
                ((AdventureSiteIntelKnowledgeRuntime)typedOwners[3]).OppositionObservations.Count,
                ((AdventureSiteIntelKnowledgeRuntime)typedOwners[4]).NotableItemObservations.Count,
                ((AdventureSiteIntelKnowledgeRuntime)typedOwners[5]).CommonResourceObservations.Count,
                ((AdventureSiteIntelKnowledgeRuntime)typedOwners[6]).AccessObservations.Count,
                marketCount,
                liquidityCount,
                shareReceiptCount
            };
            long[] expectedRevisions =
            {
                ((ExplorableSiteKnowledgeRuntime)typedOwners[0]).Revision,
                ((LocalTopologyKnowledgeRuntime)typedOwners[1]).Revision,
                ((LocalTopologyKnowledgeRuntime)typedOwners[2]).Revision,
                ((AdventureSiteIntelKnowledgeRuntime)typedOwners[3]).Revision,
                ((AdventureSiteIntelKnowledgeRuntime)typedOwners[4]).Revision,
                ((AdventureSiteIntelKnowledgeRuntime)typedOwners[5]).Revision,
                ((AdventureSiteIntelKnowledgeRuntime)typedOwners[6]).Revision,
                commercialRevision,
                commercialRevision,
                commercialRevision
            };
            for (int sectionIndex = 0; sectionIndex < knowledgeSectionPrefixes.Length; sectionIndex++)
            {
                OwnerSectionCensusWitness witness = npcKnowledgeProviders[
                    npcIndex * knowledgeSectionPrefixes.Length + sectionIndex].GetCurrentCensus();
                Assert.That(witness.SectionId,
                    Is.EqualTo(knowledgeSectionPrefixes[sectionIndex] + npc.RuntimeId));
                Assert.That(witness.SchemaVersion, Is.EqualTo(NpcKnowledgeCensusProvider.SchemaVersion));
                Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(typedOwners[sectionIndex]));
                Assert.That(witness.Cardinality, Is.EqualTo(expectedCardinalities[sectionIndex]));
                Assert.That(witness.Revision, Is.EqualTo(expectedRevisions[sectionIndex]));
            }

            bool isTravelingMerchant = npc.NpcData != null
                && npc.NpcData.job != null
                && npc.NpcData.job.jobType == NpcJobType.Merchant
                && npc.NpcData.job.merchantBehavior == MerchantBehavior.Traveling;
            if (isTravelingMerchant)
            {
                travelingMerchantCount++;
                IReadOnlyList<CommercialMarketObservation> observations = commercial.Observations;
                if (observations.Count > 0) travelingMerchantsWithMarketKnowledge++;
                foreach (CommercialMarketObservation observation in observations)
                {
                    Assert.That(observation.Source, Is.EqualTo(CommercialKnowledgeSource.InitialScenarioKnowledge));
                    Assert.That(observation.ObservedDay, Is.Zero);
                    Assert.That(observation.ReceivedDay, Is.Zero);
                    CityRuntime observedCity = runtime.Cities.SingleOrDefault(city =>
                        city.Location != null && city.Location.RuntimeId == observation.LocationRuntimeId);
                    Assert.That(observedCity, Is.Not.Null,
                        "Bootstrap merchant Knowledge must refer to a City in the selected authored profile.");
                    MarketItemRuntime observedItem = observedCity.Market.Items.LastOrDefault(item =>
                        item != null && item.Item != null && item.Item.DefinitionId == observation.ItemDefinitionId);
                    Assert.That(observedItem, Is.Not.Null,
                        "Bootstrap merchant Knowledge must correspond to an authored market item.");
                    Assert.That(observation.ObservedPrice, Is.EqualTo(observedItem.CurrentPrice));
                    Assert.That(observation.ObservedStock, Is.EqualTo(observedItem.Amount));
                }
            }
        }
        Assert.That(travelingMerchantCount, Is.GreaterThan(0),
            "The selected profile contains authored traveling merchants whose Knowledge owners must be covered.");
        Assert.That(travelingMerchantsWithMarketKnowledge, Is.GreaterThan(0),
            "The selected profile must census actual initial market Knowledge on at least one authored traveling merchant.");

        for (int i = 0; i < runtime.NpcRuntimes.Count; i++)
        {
            NpcRuntime npc = runtime.NpcRuntimes[i];
            OwnerSectionCensusWitness locationWitness = spatialKnowledgeProviders[i * 2].GetCurrentCensus();
            OwnerSectionCensusWitness routeWitness = spatialKnowledgeProviders[i * 2 + 1].GetCurrentCensus();
            Assert.That(locationWitness.SchemaVersion, Is.EqualTo(SpatialKnowledgeCensusProvider.SchemaVersion));
            Assert.That(routeWitness.SchemaVersion, Is.EqualTo(SpatialKnowledgeCensusProvider.SchemaVersion));
            Assert.That(locationWitness.OwnerInstanceIdentity, Is.SameAs(npc.SpatialKnowledge));
            Assert.That(routeWitness.OwnerInstanceIdentity, Is.SameAs(npc.SpatialKnowledge));
            Assert.That(locationWitness.Cardinality, Is.EqualTo(2));
            Assert.That(routeWitness.Cardinality, Is.EqualTo(1));
            Assert.That(locationWitness.Revision, Is.EqualTo(3L));
            Assert.That(routeWitness.Revision, Is.EqualTo(locationWitness.Revision));
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
            if (i == 4)
            {
                Assert.That(simulation.Runtime.HasSameRuntimeIdAllocatorEventCounterOwner(
                    runtimeIdAllocatorProviders[i]), Is.True,
                    "the bootstrap composition publishes the exact Event-counter owner bound into its selected runtime");
            }
            if (i == 6)
            {
                Assert.That(simulation.Runtime.HasSameRuntimeIdAllocatorDecisionCounterOwner(
                    runtimeIdAllocatorProviders[i]), Is.True,
                    "the bootstrap composition publishes the exact Decision-counter owner bound into its selected runtime");
            }
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

        OwnerSectionCensusWitness manpowerWitness = simulation.Bootstrap.ContingentManpowerCensusProvider.GetCurrentCensus();
        Assert.That(manpowerWitness.SectionId, Is.EqualTo(ContingentManpowerCensusProvider.SectionId));
        Assert.That(manpowerWitness.SchemaVersion, Is.EqualTo(ContingentManpowerCensusProvider.SchemaVersion));
        Assert.That(manpowerWitness.OwnerInstanceIdentity, Is.SameAs(simulation.Runtime.ContingentManpowerStateStore));
        Assert.That(manpowerWitness.Cardinality, Is.Zero);
        Assert.That(manpowerWitness.Revision, Is.Zero);
        OwnerSectionCensusWitness repeatedManpowerWitness =
            simulation.Bootstrap.ContingentManpowerCensusProvider.GetCurrentCensus();
        Assert.That(repeatedManpowerWitness.OwnerInstanceIdentity, Is.SameAs(manpowerWitness.OwnerInstanceIdentity));
        Assert.That(repeatedManpowerWitness.Cardinality, Is.Zero);
        Assert.That(repeatedManpowerWitness.Revision, Is.Zero);

        OwnerSectionCensusWitness forcePositionWitness = simulation.Bootstrap.ArmedForceSpatialCensusProvider.GetCurrentCensus();
        Assert.That(forcePositionWitness.SectionId, Is.EqualTo(ArmedForceSpatialCensusProvider.SectionId));
        Assert.That(forcePositionWitness.SchemaVersion, Is.EqualTo(ArmedForceSpatialCensusProvider.SchemaVersion));
        Assert.That(forcePositionWitness.OwnerInstanceIdentity, Is.SameAs(simulation.Runtime.ArmedForceSpatialStateStore));
        Assert.That(forcePositionWitness.Cardinality, Is.Zero);
        Assert.That(forcePositionWitness.Revision, Is.Zero);
        OwnerSectionCensusWitness repeatedForcePositionWitness =
            simulation.Bootstrap.ArmedForceSpatialCensusProvider.GetCurrentCensus();
        Assert.That(repeatedForcePositionWitness.OwnerInstanceIdentity, Is.SameAs(forcePositionWitness.OwnerInstanceIdentity));
        Assert.That(repeatedForcePositionWitness.Cardinality, Is.Zero);
        Assert.That(repeatedForcePositionWitness.Revision, Is.Zero);

        OwnerSectionCensusWitness conflictWitness = simulation.Bootstrap.ConflictCensusProvider.GetCurrentCensus();
        Assert.That(conflictWitness.SectionId, Is.EqualTo(PersistentConflictCensusProvider.SectionId));
        Assert.That(conflictWitness.SchemaVersion, Is.EqualTo(PersistentConflictCensusProvider.SchemaVersion));
        Assert.That(conflictWitness.OwnerInstanceIdentity, Is.SameAs(simulation.Runtime.ConflictStore));
        Assert.That(conflictWitness.Cardinality, Is.Zero);
        Assert.That(conflictWitness.Revision, Is.Zero);
        OwnerSectionCensusWitness repeatedConflictWitness = simulation.Bootstrap.ConflictCensusProvider.GetCurrentCensus();
        Assert.That(repeatedConflictWitness.OwnerInstanceIdentity, Is.SameAs(conflictWitness.OwnerInstanceIdentity));
        Assert.That(repeatedConflictWitness.Cardinality, Is.Zero);
        Assert.That(repeatedConflictWitness.Revision, Is.Zero);

        OwnerSectionCensusWitness warWitness = simulation.Bootstrap.WarCensusProvider.GetCurrentCensus();
        Assert.That(warWitness.SectionId, Is.EqualTo(PersistentWarCensusProvider.SectionId));
        Assert.That(warWitness.SchemaVersion, Is.EqualTo(PersistentWarCensusProvider.SchemaVersion));
        Assert.That(warWitness.OwnerInstanceIdentity, Is.SameAs(simulation.Runtime.WarStore));
        Assert.That(warWitness.Cardinality, Is.Zero);
        Assert.That(warWitness.Revision, Is.Zero);
        OwnerSectionCensusWitness repeatedWarWitness = simulation.Bootstrap.WarCensusProvider.GetCurrentCensus();
        Assert.That(repeatedWarWitness.OwnerInstanceIdentity, Is.SameAs(warWitness.OwnerInstanceIdentity));
        Assert.That(repeatedWarWitness.Cardinality, Is.Zero);
        Assert.That(repeatedWarWitness.Revision, Is.Zero);

        OwnerSectionCensusWitness battleWitness = simulation.Bootstrap.BattleCensusProvider.GetCurrentCensus();
        Assert.That(battleWitness.SectionId, Is.EqualTo(PersistentBattleCensusProvider.SectionId));
        Assert.That(battleWitness.SchemaVersion, Is.EqualTo(PersistentBattleCensusProvider.SchemaVersion));
        Assert.That(battleWitness.OwnerInstanceIdentity, Is.SameAs(simulation.Runtime.BattleStore));
        Assert.That(battleWitness.Cardinality, Is.Zero);
        Assert.That(battleWitness.Revision, Is.Zero);
        OwnerSectionCensusWitness repeatedBattleWitness = simulation.Bootstrap.BattleCensusProvider.GetCurrentCensus();
        Assert.That(repeatedBattleWitness.OwnerInstanceIdentity, Is.SameAs(battleWitness.OwnerInstanceIdentity));
        Assert.That(repeatedBattleWitness.Cardinality, Is.Zero);
        Assert.That(repeatedBattleWitness.Revision, Is.Zero);

        OwnerSectionCensusWitness estateWitness = simulation.Bootstrap.EstateCensusProvider.GetCurrentCensus();
        Assert.That(estateWitness.SectionId, Is.EqualTo(EstateCensusProvider.SectionId));
        Assert.That(estateWitness.SchemaVersion, Is.EqualTo(EstateCensusProvider.SchemaVersion));
        Assert.That(estateWitness.OwnerInstanceIdentity, Is.SameAs(simulation.Runtime.EstateStore));
        Assert.That(estateWitness.Cardinality, Is.Zero);
        Assert.That(estateWitness.Revision, Is.Zero);
        OwnerSectionCensusWitness repeatedEstateWitness = simulation.Bootstrap.EstateCensusProvider.GetCurrentCensus();
        Assert.That(repeatedEstateWitness.OwnerInstanceIdentity, Is.SameAs(estateWitness.OwnerInstanceIdentity));
        Assert.That(repeatedEstateWitness.Cardinality, Is.Zero);
        Assert.That(repeatedEstateWitness.Revision, Is.Zero);

        IReadOnlyList<IOwnerSectionCensusProvider> propertyProviders =
            simulation.Bootstrap.PropertyOwnershipCensusProviders;
        Assert.That(propertyProviders, Has.Count.EqualTo(2));
        OwnerSectionCensusWitness propertyOwnershipWitness = propertyProviders[0].GetCurrentCensus();
        OwnerSectionCensusWitness propertyHistoryWitness = propertyProviders[1].GetCurrentCensus();
        Assert.That(propertyOwnershipWitness.SectionId, Is.EqualTo(PropertyOwnershipCensusProvider.OwnershipSectionId));
        Assert.That(propertyOwnershipWitness.SchemaVersion, Is.EqualTo(PropertyOwnershipCensusProvider.SchemaVersion));
        Assert.That(propertyHistoryWitness.SectionId, Is.EqualTo(PropertyOwnershipCensusProvider.TransferHistorySectionId));
        Assert.That(propertyHistoryWitness.SchemaVersion, Is.EqualTo(PropertyOwnershipCensusProvider.SchemaVersion));
        Assert.That(propertyOwnershipWitness.OwnerInstanceIdentity, Is.SameAs(simulation.Runtime.PropertyOwnershipStore));
        Assert.That(propertyHistoryWitness.OwnerInstanceIdentity, Is.SameAs(propertyOwnershipWitness.OwnerInstanceIdentity));
        Assert.That(propertyOwnershipWitness.Cardinality, Is.Zero);
        Assert.That(propertyHistoryWitness.Cardinality, Is.Zero);
        Assert.That(propertyOwnershipWitness.Revision, Is.Zero);
        Assert.That(propertyHistoryWitness.Revision, Is.Zero);
        OwnerSectionCensusWitness[] repeatedPropertyWitnesses = propertyProviders
            .Select(provider => provider.GetCurrentCensus())
            .ToArray();
        Assert.That(repeatedPropertyWitnesses[0].OwnerInstanceIdentity,
            Is.SameAs(propertyOwnershipWitness.OwnerInstanceIdentity));
        Assert.That(repeatedPropertyWitnesses[1].OwnerInstanceIdentity,
            Is.SameAs(propertyOwnershipWitness.OwnerInstanceIdentity));
        Assert.That(repeatedPropertyWitnesses[0].Cardinality, Is.Zero);
        Assert.That(repeatedPropertyWitnesses[1].Cardinality, Is.Zero);
        Assert.That(repeatedPropertyWitnesses[0].Revision, Is.Zero);
        Assert.That(repeatedPropertyWitnesses[1].Revision, Is.Zero);

        IReadOnlyList<IOwnerSectionCensusProvider> institutionOfficeProviders =
            simulation.Bootstrap.InstitutionOfficeCensusProviders;
        Assert.That(institutionOfficeProviders, Has.Count.EqualTo(4));
        for (int i = 0; i < institutionOfficeSectionIds.Length; i++)
        {
            Assert.That(institutionOfficeProviders[i].GetCurrentCensus().SectionId,
                Is.EqualTo(institutionOfficeSectionIds[i]));
            Assert.That(((OwnerSectionContract)expectedCensusSections[institutionOfficeSectionIds[i]]).Role,
                Is.EqualTo(OwnerSectionRole.Required));
        }
        OwnerSectionCensusWitness institutionWitness = institutionOfficeProviders[0].GetCurrentCensus();
        OwnerSectionCensusWitness officeWitness = institutionOfficeProviders[1].GetCurrentCensus();
        OwnerSectionCensusWitness incumbencyWitness = institutionOfficeProviders[2].GetCurrentCensus();
        OwnerSectionCensusWitness tenureWitness = institutionOfficeProviders[3].GetCurrentCensus();
        Assert.That(institutionWitness.SectionId, Is.EqualTo(InstitutionOfficeCensusProvider.InstitutionsSectionId));
        Assert.That(officeWitness.SectionId, Is.EqualTo(InstitutionOfficeCensusProvider.OfficesSectionId));
        Assert.That(incumbencyWitness.SectionId, Is.EqualTo(InstitutionOfficeCensusProvider.IncumbenciesSectionId));
        Assert.That(tenureWitness.SectionId, Is.EqualTo(InstitutionOfficeCensusProvider.TenuresSectionId));
        Assert.That(institutionWitness.SchemaVersion, Is.EqualTo(InstitutionOfficeCensusProvider.SchemaVersion));
        Assert.That(officeWitness.SchemaVersion, Is.EqualTo(InstitutionOfficeCensusProvider.SchemaVersion));
        Assert.That(incumbencyWitness.SchemaVersion, Is.EqualTo(InstitutionOfficeCensusProvider.SchemaVersion));
        Assert.That(tenureWitness.SchemaVersion, Is.EqualTo(InstitutionOfficeCensusProvider.SchemaVersion));
        Assert.That(institutionWitness.OwnerInstanceIdentity,
            Is.SameAs(GetRuntimeOwner(simulation.Runtime, "institutionStore")));
        Assert.That(officeWitness.OwnerInstanceIdentity,
            Is.SameAs(GetRuntimeOwner(simulation.Runtime, "officeStore")));
        Assert.That(incumbencyWitness.OwnerInstanceIdentity, Is.SameAs(officeWitness.OwnerInstanceIdentity));
        Assert.That(tenureWitness.OwnerInstanceIdentity, Is.SameAs(officeWitness.OwnerInstanceIdentity));
        Assert.That(institutionWitness.Cardinality, Is.Zero);
        Assert.That(officeWitness.Cardinality, Is.Zero);
        Assert.That(incumbencyWitness.Cardinality, Is.Zero);
        Assert.That(tenureWitness.Cardinality, Is.Zero);
        Assert.That(institutionWitness.Revision, Is.Zero);
        Assert.That(officeWitness.Revision, Is.Zero);
        Assert.That(incumbencyWitness.Revision, Is.Zero);
        Assert.That(tenureWitness.Revision, Is.Zero);
        OwnerSectionCensusWitness[] repeatedInstitutionOfficeWitnesses = institutionOfficeProviders
            .Select(provider => provider.GetCurrentCensus())
            .ToArray();
        Assert.That(repeatedInstitutionOfficeWitnesses[0].OwnerInstanceIdentity,
            Is.SameAs(institutionWitness.OwnerInstanceIdentity));
        Assert.That(repeatedInstitutionOfficeWitnesses[1].OwnerInstanceIdentity,
            Is.SameAs(officeWitness.OwnerInstanceIdentity));
        Assert.That(repeatedInstitutionOfficeWitnesses[2].OwnerInstanceIdentity,
            Is.SameAs(officeWitness.OwnerInstanceIdentity));
        Assert.That(repeatedInstitutionOfficeWitnesses[3].OwnerInstanceIdentity,
            Is.SameAs(officeWitness.OwnerInstanceIdentity));
        for (int i = 0; i < repeatedInstitutionOfficeWitnesses.Length; i++)
        {
            Assert.That(repeatedInstitutionOfficeWitnesses[i].Cardinality, Is.Zero);
            Assert.That(repeatedInstitutionOfficeWitnesses[i].Revision, Is.Zero);
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

        Assert.That(simulation.Bootstrap.ProfileContractIdentity, Is.EqualTo(SimulationGenesisPipeline.GeographyProfileContractIdentity));
        Assert.That(simulation.Bootstrap.Manifest.SelectedP9ContractIdentity, Is.EqualTo(SimulationGenesisPipeline.GeographyProfileContractIdentity));
        Assert.That(simulation.Bootstrap.Manifest.SelectedP9SchemaVersion, Is.EqualTo(2));
        Assert.That(simulation.Bootstrap.Manifest.SelectedP9ProfileFingerprint, Is.Not.Empty);
        Assert.That(simulation.Bootstrap.Manifest.Fingerprint, Is.EqualTo(simulation.Bootstrap.Manifest.SelectedP9ProfileFingerprint));
        Assert.That(simulation.Bootstrap.Manifest.SchemaVersion, Is.EqualTo(2));
        Assert.That(simulation.Bootstrap.Manifest.StageOrder, Is.EqualTo(new[]
        {
            "p9.genesis.resolve-profile/v1",
            "p9.genesis.authored-world/v1",
            SimulationGenesisPipeline.GeographyStageId,
            "p9.genesis.authored-actors/v1",
            "p9.genesis.validate-profile/v1",
            "p9.genesis.publish/v1"
        }));
        Assert.That(authority.HexCount, Is.EqualTo(1));
        Assert.That(authority.LocationCount, Is.EqualTo(1));
        Assert.That(authority.TryGet(new HexId("hex/sample-origin"), out HexRecord hex), Is.True);
        Assert.That(hex.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
        Assert.That(hex.TerrainDefinitionId.Value, Is.EqualTo("terrain/sample-plains"));
        Assert.That(hex.AuthoredRevisionToken, Is.EqualTo("sample-world-v1"));
        Assert.That(authority.TryGet(new LocationId("location/sample-origin"), out LocationRecord location), Is.True);
        Assert.That(location.AnchorHexId.Value, Is.EqualTo("hex/sample-origin"));
        Assert.That(authority.ScaleContext.ResolvedConventionId, Is.EqualTo("world-scale/Simulation-DailyV1/v1"));
        Assert.That(authority.ScaleContext.SourceIdentity, Is.EqualTo("profile/Simulation-DailyV1"));
        Assert.That(authority.ScaleContext.SourceVersion, Is.EqualTo("1"));
        Assert.That(authority.ScaleContext.DistancePerNeighborStep, Is.EqualTo(1m));
        Assert.That(authority.ScaleContext.Unit, Is.EqualTo("km"));
        Assert.That(runtime.Cities, Has.Count.EqualTo(2));
        Assert.That(runtime.NpcRuntimes, Has.Count.EqualTo(10));
        IReadOnlyList<IOwnerSectionCensusProvider> personProviders =
            simulation.Bootstrap.PersonStoreCensusProviders;
        Assert.That(personProviders, Has.Count.EqualTo(2));
        OwnerSectionCensusWitness personMembership = personProviders[0].GetCurrentCensus();
        OwnerSectionCensusWitness personBindings = personProviders[1].GetCurrentCensus();
        Assert.That(personMembership.SectionId, Is.EqualTo(PersonMembershipCensusProvider.SectionId));
        Assert.That(personBindings.SectionId, Is.EqualTo(PersonMaterializationBindingCensusProvider.SectionId));
        Assert.That(personMembership.OwnerInstanceIdentity, Is.SameAs(runtime.PersonStore));
        Assert.That(personBindings.OwnerInstanceIdentity, Is.SameAs(runtime.PersonStore));
        Assert.That(personMembership.Cardinality, Is.Zero);
        Assert.That(personBindings.Cardinality, Is.Zero);
        Assert.That(personMembership.Revision, Is.Zero);
        Assert.That(personBindings.Revision, Is.Zero);

        IReadOnlyList<IOwnerSectionCensusProvider> cityPresenceProviders =
            simulation.Bootstrap.CityNpcPresenceCensusProviders;
        IReadOnlyList<IOwnerSectionCensusProvider> cityMarketProviders =
            simulation.Bootstrap.CityMarketCensusProviders;
        IReadOnlyList<IOwnerSectionCensusProvider> populationProviders =
            simulation.Bootstrap.SettlementPopulationCensusProviders;
        Assert.That(cityPresenceProviders, Has.Count.EqualTo(runtime.Cities.Count));
        Assert.That(cityMarketProviders, Has.Count.EqualTo(runtime.Cities.Count));
        Assert.That(populationProviders, Has.Count.EqualTo(runtime.Cities.Count * 2));
        CityRuntime[] orderedProfileCities = runtime.Cities
            .OrderBy(city => city.RuntimeId, System.StringComparer.Ordinal)
            .ToArray();
        for (int i = 0; i < runtime.Cities.Count; i++)
        {
            CityRuntime city = orderedProfileCities[i];
            OwnerSectionCensusWitness witness = cityPresenceProviders[i].GetCurrentCensus();
            Assert.That(witness.SectionId, Is.EqualTo(
                CityNpcPresenceCensusProvider.SectionIdPrefix + city.RuntimeId));
            Assert.That(witness.SchemaVersion, Is.EqualTo(CityNpcPresenceCensusProvider.SchemaVersion));
            Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(city));
            Assert.That(witness.Cardinality, Is.EqualTo(city.ImportantNpcs.Count));
            Assert.That(witness.Revision, Is.EqualTo(city.ImportantNpcRevision));
            Assert.That(city.ImportantNpcs.Distinct().Count(), Is.EqualTo(city.ImportantNpcs.Count));
            Assert.That(city.ImportantNpcs.All(npc => npc != null
                && npc.CurrentCity == city
                && npc.CurrentLocation == city.Location
                && runtime.NpcRuntimes.Contains(npc)), Is.True);
            Assert.That(runtime.NpcRuntimes.Count(npc => npc.CurrentCity == city),
                Is.EqualTo(city.ImportantNpcs.Count));

            OwnerSectionCensusWitness marketWitness = cityMarketProviders[i].GetCurrentCensus();
            Assert.That(marketWitness.SectionId, Is.EqualTo(CityMarketCensusProvider.SectionIdPrefix
                + city.RuntimeId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ":" + city.RuntimeId));
            Assert.That(marketWitness.SchemaVersion, Is.EqualTo(CityMarketCensusProvider.SchemaVersion));
            Assert.That(marketWitness.OwnerInstanceIdentity, Is.SameAs(city.Market));
            Assert.That(marketWitness.Cardinality, Is.EqualTo(city.Market.Items.Count));
            Assert.That(marketWitness.Cardinality, Is.EqualTo(5));
            Assert.That(marketWitness.Revision, Is.EqualTo(city.Market.Revision));
            Assert.That(marketWitness.Revision, Is.Zero);

            OwnerSectionCensusWitness population = populationProviders[i * 2].GetCurrentCensus();
            OwnerSectionCensusWitness receipts = populationProviders[i * 2 + 1].GetCurrentCensus();
            string encodedCityId = city.RuntimeId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ":" + city.RuntimeId;
            Assert.That(population.SectionId,
                Is.EqualTo(SettlementPopulationCensusProvider.AggregateSectionPrefix + encodedCityId));
            Assert.That(receipts.SectionId,
                Is.EqualTo(SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix + encodedCityId));
            Assert.That(population.SchemaVersion, Is.EqualTo(SettlementPopulationCensusProvider.SchemaVersion));
            Assert.That(receipts.SchemaVersion, Is.EqualTo(SettlementPopulationCensusProvider.SchemaVersion));
            Assert.That(population.OwnerInstanceIdentity, Is.SameAs(city.Population));
            Assert.That(receipts.OwnerInstanceIdentity, Is.SameAs(city.Population));
            Assert.That(population.Cardinality, Is.EqualTo(1));
            Assert.That(receipts.Cardinality, Is.Zero);
            Assert.That(population.Revision, Is.EqualTo(city.Population.Revision));
            Assert.That(receipts.Revision, Is.Zero);
        }
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
        OwnerSectionCensusWitness scheduledDirectiveWitness =
            simulation.Bootstrap.ScheduledDirectiveCensusProvider.GetCurrentCensus();
        Assert.That(scheduledDirectiveWitness.SectionId, Is.EqualTo(ScheduledDirectiveCensusProvider.SectionId));
        Assert.That(scheduledDirectiveWitness.SchemaVersion, Is.EqualTo(ScheduledDirectiveCensusProvider.SchemaVersion));
        Assert.That(scheduledDirectiveWitness.OwnerInstanceIdentity, Is.SameAs(simulation.Bootstrap.ScheduledDirectives));
        Assert.That(scheduledDirectiveWitness.Cardinality,
            Is.EqualTo(simulation.Bootstrap.ScheduledDirectives.Directives.Count));
        Assert.That(scheduledDirectiveWitness.Revision, Is.EqualTo(simulation.Bootstrap.ScheduledDirectives.Revision));
        Assert.That(simulation.Bootstrap.TravelParties.ActiveParties, Is.Empty);
        OwnerSectionCensusWitness travelPartyWitness = simulation.Bootstrap.TravelPartyCensusProvider.GetCurrentCensus();
        Assert.That(travelPartyWitness.SectionId, Is.EqualTo("p12f.travel-parties"));
        Assert.That(travelPartyWitness.SchemaVersion, Is.EqualTo(1));
        Assert.That(travelPartyWitness.OwnerInstanceIdentity, Is.SameAs(simulation.Bootstrap.TravelParties));
        Assert.That(simulation.Bootstrap.GroupTravel.Store, Is.SameAs(simulation.Bootstrap.TravelParties));
        Assert.That(travelPartyWitness.Cardinality, Is.Zero);
        Assert.That(travelPartyWitness.Revision, Is.Zero);
        OwnerSectionCensusWitness repeatedTravelPartyWitness = simulation.Bootstrap.TravelPartyCensusProvider.GetCurrentCensus();
        Assert.That(repeatedTravelPartyWitness.OwnerInstanceIdentity, Is.SameAs(travelPartyWitness.OwnerInstanceIdentity));
        Assert.That(repeatedTravelPartyWitness.Cardinality, Is.Zero);
        Assert.That(repeatedTravelPartyWitness.Revision, Is.Zero);
        Assert.That(simulation.Bootstrap.Expeditions.ActiveExpeditions, Is.Empty);
        OwnerSectionCensusWitness expeditionWitness = simulation.Bootstrap.ExpeditionCensusProvider.GetCurrentCensus();
        Assert.That(expeditionWitness.SectionId, Is.EqualTo(ExpeditionCensusProvider.SectionId));
        Assert.That(expeditionWitness.SchemaVersion, Is.EqualTo(ExpeditionCensusProvider.SchemaVersion));
        Assert.That(expeditionWitness.OwnerInstanceIdentity, Is.SameAs(simulation.Bootstrap.Expeditions));
        Assert.That(simulation.Bootstrap.ExpeditionSystem.Store, Is.SameAs(simulation.Bootstrap.Expeditions));
        Assert.That(expeditionWitness.Cardinality, Is.EqualTo(simulation.Bootstrap.Expeditions.ActiveExpeditions.Count));
        Assert.That(expeditionWitness.Revision, Is.Zero);
        Assert.That(simulation.Bootstrap.ExplorableSites.Sites, Is.Empty);
        OwnerSectionCensusWitness explorableSiteWitness =
            simulation.Bootstrap.ExplorableSiteCensusProvider.GetCurrentCensus();
        Assert.That(explorableSiteWitness.SectionId, Is.EqualTo(ExplorableSiteCensusProvider.SectionId));
        Assert.That(explorableSiteWitness.SchemaVersion, Is.EqualTo(ExplorableSiteCensusProvider.SchemaVersion));
        Assert.That(explorableSiteWitness.OwnerInstanceIdentity, Is.SameAs(simulation.Bootstrap.ExplorableSites));
        Assert.That(explorableSiteWitness.Cardinality, Is.Zero);
        Assert.That(explorableSiteWitness.Cardinality, Is.EqualTo(simulation.Bootstrap.ExplorableSites.Count));
        Assert.That(explorableSiteWitness.Revision, Is.Zero);
        Assert.That(explorableSiteWitness.Revision, Is.EqualTo(simulation.Bootstrap.ExplorableSites.Revision));
        OwnerSectionCensusWitness repeatedExplorableSiteWitness =
            simulation.Bootstrap.ExplorableSiteCensusProvider.GetCurrentCensus();
        Assert.That(repeatedExplorableSiteWitness.OwnerInstanceIdentity,
            Is.SameAs(explorableSiteWitness.OwnerInstanceIdentity));
        Assert.That(repeatedExplorableSiteWitness.Cardinality, Is.EqualTo(explorableSiteWitness.Cardinality));
        Assert.That(repeatedExplorableSiteWitness.Revision, Is.EqualTo(explorableSiteWitness.Revision));
        Assert.That(runtime.LocalTopologyStore, Is.Null);
        Assert.That(authority.Revision, Is.EqualTo(1));
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

        Assert.That(simulation.ExplorableSites.Sites, Is.Empty);
        Assert.That(runtime.LocalTopologyStore, Is.Null);
        Assert.That(authority.ValidateInvariants().IsValid, Is.True);
        Assert.That(simulation.Bootstrap.Manifest.CanonicalProvenanceRecords,
            Has.Some.Contains("authored-geography-stage"));
        Assert.That(simulation.Bootstrap.Manifest.CanonicalProvenanceRecords,
            Does.Not.Contain("combined-stage-order"));

        int expectedP9RouteCount = config.Cities.Sum(city => city.connections.Count)
            + config.ExplorableSites.Count * 2;
        Assert.That(simulation.SpatialNetwork.Routes.Count, Is.EqualTo(expectedP9RouteCount),
            "The selected P9-B-only profile adds no P10-A topology or routes.");
    }

    [Test]
    public void GeneralTestRemainsASeparateP10RuinProvingProfile()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
        Assert.That(config, Is.Not.Null);
        Assert.That(config.useAuthoredGeographyProfile, Is.True);
        Assert.That(config.authoredP10RuinSite, Is.Not.Null);
        Assert.That(config.GenesisProfileContractIdentity, Is.EqualTo(P10RuinLocalTopologyGenesis.ContractIdentity));
        Assert.That(P10RuinLocalTopologyGenesis.IsEnabled(config), Is.True);
        Assert.That(SimulationGenesisPipeline.ResolveStageOrder(config.useAuthoredGeographyProfile,
            P10RuinLocalTopologyGenesis.IsEnabled(config)), Does.Contain(P10RuinLocalTopologyGenesis.StageId));

        string dailyConfigGuid = AssetDatabase.AssetPathToGUID(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        string sampleScene = System.IO.File.ReadAllText("Assets/Scenes/SampleScene.unity");
        Assert.That(dailyConfigGuid, Is.Not.Empty);
        Assert.That(sampleScene, Does.Contain("simulationConfig: {fileID: 11400000, guid: " + dailyConfigGuid));
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

    private static object GetRuntimeOwner(SimulationRuntime runtime, string fieldName)
    {
        FieldInfo field = typeof(SimulationRuntime).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Expected runtime owner field " + fieldName + ".");
        object owner = field.GetValue(runtime);
        Assert.That(owner, Is.Not.Null, "Expected runtime owner " + fieldName + ".");
        return owner;
    }

    private static T ReadPrivateField<T>(object target, string fieldName) where T : class
    {
        Assert.That(target, Is.Not.Null, "Expected an owner before reading " + fieldName + ".");
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Expected field " + fieldName + " on " + target.GetType().Name + ".");
        T value = field.GetValue(target) as T;
        Assert.That(value, Is.Not.Null, "Expected existing owner field " + fieldName + ".");
        return value;
    }

    private static void InvokeBootstrapInitialize(TesteSimulacao simulation, System.Action<string> stageCompleted)
    {
        MethodInfo initialize = typeof(TesteSimulacao).GetMethod(
            "InitializeSimulation",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(initialize, Is.Not.Null);
        initialize.Invoke(simulation, new object[] { stageCompleted });
    }

}
