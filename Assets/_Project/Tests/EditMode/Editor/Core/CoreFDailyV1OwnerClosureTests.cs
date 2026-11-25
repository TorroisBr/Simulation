using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CoreFDailyV1OwnerClosureTests
{
    private sealed class ThrowAfterPersonDeathProjectionPublicationBoundary
        : IDailyV1SelectedPersonDeathBoundary
    {
        private readonly IDailyV1SelectedPersonDeathBoundary inner;

        internal ThrowAfterPersonDeathProjectionPublicationBoundary(
            IDailyV1SelectedPersonDeathBoundary inner)
        {
            this.inner = inner ?? throw new System.ArgumentNullException(nameof(inner));
        }

        public bool TryBeginPersonDeathOperation(
            DailyV1SelectedPersonDeathPlan plan,
            bool currentActionChanged,
            string operationId,
            IReadOnlyList<string> sectionIds,
            System.Func<bool> localRevisionCapacity,
            out IDailyV1SelectedPersonDeathP12Scope scope) =>
            inner.TryBeginPersonDeathOperation(
                plan, currentActionChanged, operationId, sectionIds,
                localRevisionCapacity, out scope);

        public bool TryBeginPersonDeathProjectionMutation(
            DailyV1SelectedPersonDeathPlan plan,
            string operationId,
            bool currentActionChanged,
            IReadOnlyList<string> sectionIds) =>
            inner.TryBeginPersonDeathProjectionMutation(
                plan, operationId, currentActionChanged, sectionIds);

        public bool TryCompletePersonDeathProjectionMutation(
            DailyV1SelectedPersonDeathPlan plan,
            string operationId,
            bool currentActionChanged,
            IReadOnlyList<string> sectionIds,
            bool operationSucceeded) =>
            inner.TryCompletePersonDeathProjectionMutation(
                plan, operationId, currentActionChanged, sectionIds, operationSucceeded);

        public bool TryValidatePersonDeathProjection(
            DailyV1SelectedPersonDeathPlan plan,
            out bool currentActionChanged,
            out string failure) =>
            inner.TryValidatePersonDeathProjection(plan, out currentActionChanged, out failure);

        public bool TryPublishPersonDeathProjection(
            DailyV1SelectedPersonDeathPlan plan,
            bool currentActionChanged)
        {
            if (!inner.TryPublishPersonDeathProjection(plan, currentActionChanged)) return false;
            throw new System.InvalidOperationException(
                "Injected exception after the compatibility projection was published.");
        }

        public void FaultPersonDeathRuntime() => inner.FaultPersonDeathRuntime();
        public void AdvancePersonDeathWorldRevision() => inner.AdvancePersonDeathWorldRevision();
    }

    private GameObject simulationObject;

    [TearDown]
    public void TearDown()
    {
        if (simulationObject != null) Object.DestroyImmediate(simulationObject);
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void SelectedDailyV1OwnerVectorAndSupportedNpcTransitionStayBoundToTheLiveProtocol()
    {
        SimulationConfigData configuration = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(configuration, Is.Not.Null);
        Assert.That(configuration.useAuthoredGeographyProfile, Is.True);
        Assert.That(configuration.authoredP10RuinSite, Is.Null);

        simulationObject = new GameObject("core-f-daily-v1-owner-closure");
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, configuration);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();

        SimulationRuntime runtime = simulation.Bootstrap.Runtime;
        Assert.That(runtime, Is.Not.Null);
        Assert.That(P12CSpatialAuthoritySnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            SimulationGenesisPipeline.GeographyProfileContractIdentity,
            2,
            runtime.SpatialAuthorityStore,
            out P12CSpatialAuthoritySnapshot p8Snapshot,
            out P12CSpatialAuthoritySnapshotFailure p8Failure), Is.True, p8Failure.ToString());
        Assert.That(P12ACSpatialAuthorityCodec.TryComputeFingerprint(p8Snapshot, out string p8Fingerprint), Is.True);
        Assert.That(p8Fingerprint, Is.Not.Empty);
        TestContext.WriteLine("Selected Daily-v1 live P8-A fingerprint: " + p8Fingerprint);
        Assert.That(runtime.Cities, Has.Count.EqualTo(2));
        Assert.That(runtime.NpcRuntimes, Has.Count.EqualTo(10));
        ContinuationCensusProtocol protocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);
        Assert.That(protocol, Is.Not.Null);

        IReadOnlyCollection<string> operations = CoreFDailyV1OwnerClosureVectors.ReadSealedOperationIds(protocol);
        Assert.That(operations, Is.EquivalentTo(CoreFDailyV1OwnerClosureVectors.ExpectedConditionalOperationIds),
            "The selected composition includes the solo and group travel services, so all 24 source-registered operations apply.");

        IReadOnlyList<OwnerSectionCensusSnapshot> initial =
            CoreFDailyV1OwnerClosureVectors.Capture(protocol, out P12ADailyV1Census initialCensus);
        Assert.That(initial, Has.Count.EqualTo(CoreFDailyV1OwnerClosureVectors.ExpectedSectionCount(
            runtime.NpcRuntimes.Count, 0, runtime.PersonStore.Persons.Count, runtime.Cities.Count)));
        Assert.That(initial, Has.Count.EqualTo(299), "61 + 22*N + U + P + 4*C, with N=10, U=10, P=0, C=2.");
        Assert.That(initial.Select(row => row.SectionId), Is.Ordered.Using<string>(System.StringComparer.Ordinal));
        Assert.That(initial.Select(row => row.SectionId).Distinct(System.StringComparer.Ordinal).Count(), Is.EqualTo(initial.Count));
        Assert.That(initial.All(row => row.Role == OwnerSectionRole.Required
            || row.Role == OwnerSectionRole.ExplicitlyEmpty), Is.True);
        CoreFDailyV1OwnerClosureVectors.AssertAliasGroupsMatchReferences(initial, initialCensus);

        DailyV1Application application = (DailyV1Application)typeof(TesteSimulacao)
            .GetField("dailyV1Application", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(simulation);
        DailyV1ActiveRuntimeSession coreSession = application.ActiveSession;
        Assert.That(coreSession, Is.Not.Null);
        Assert.That(application.TryBeginExclusiveOperation(coreSession, out var exclusiveOperation), Is.True);
        using (exclusiveOperation)
        {
            long heldDay = runtime.CurrentDay;
            Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure heldAdvanceFailure), Is.False);
            Assert.That(heldAdvanceFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress));
            Assert.That(runtime.TryAdvanceDays(
                2,
                out int heldDaysAdvanced,
                out SimulationRuntimeAdvanceFailure heldMultiAdvanceFailure), Is.False);
            Assert.That(heldMultiAdvanceFailure,
                Is.EqualTo(SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress));
            Assert.That(heldDaysAdvanced, Is.Zero);
            Assert.That(runtime.CurrentDay, Is.EqualTo(heldDay),
                "The Unity runtime projection must pass through the Core application session gate.");
        }

        AssertOwnerAlias(initial,
            RuntimeIdentityRegistryCensusProvider.NpcsSectionId,
            RuntimeIdentityRegistryCensusProvider.CitiesSectionId,
            RuntimeIdentityRegistryCensusProvider.LocationsSectionId,
            RuntimeIdentityRegistryCensusProvider.RoutesSectionId,
            RuntimeIdentityRegistryCensusProvider.ExplorableSitesSectionId,
            RuntimeIdentityRegistryCensusProvider.LocalPlacesSectionId,
            RuntimeIdentityRegistryCensusProvider.LocalConnectionsSectionId,
            RuntimeIdentityRegistryCensusProvider.NotableItemsSectionId);

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken completedToken,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());
        MethodInfo captureProjection = typeof(TesteSimulacao).GetMethod(
            "CaptureActiveSession", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(captureProjection, Is.Not.Null);
        SimulationActiveSession unityProjection = (SimulationActiveSession)captureProjection.Invoke(simulation, null);
        Assert.That(unityProjection, Is.Not.Null);
        Assert.That(unityProjection.CoreRuntimeSession, Is.SameAs(coreSession));
        Assert.That(application.TryBeginContinuationSaveOperation(
            coreSession,
            out DailyV1P12GContinuationSaveOperation continuationSaveOperation,
            out DailyCaptureEligibilityFailure continuationSaveFailure), Is.True,
            continuationSaveFailure.ToString());
        P12AContinuationImage unityImage;
        using (continuationSaveOperation)
        {
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionAndCaptureImage(
                unityProjection,
                unityProjection.AdmissionContext,
                continuationSaveOperation,
                out unityImage,
                out _,
                out P12GDailyV1RestoreFailure parityCaptureFailure,
                out string parityCaptureDiagnostic), Is.True,
                parityCaptureFailure + ": " + parityCaptureDiagnostic);
        }
        Assert.That(unityImage.Header.Day, Is.EqualTo(runtime.CurrentDay));
        string expectedContentFingerprint =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions().ContentFingerprint;
        Assert.That(unityImage.Header.Compatibility.ContentFingerprint,
            Is.EqualTo(expectedContentFingerprint),
            "The Unity authored profile and detached .NET host must bind the same captured effective-definition content.");
        string commonBehaviorSha = CoreFHostNeutralBehaviorFingerprint.Compute(unityImage);
        Assert.That(commonBehaviorSha, Is.EqualTo(CoreFHostNeutralBehaviorFingerprint.ExpectedUnityDotNetDayOneSha256),
            "Unity and detached .NET must produce the same accepted Daily-v1 day-one projection.");
        TestContext.WriteLine(
            "CORE_F_HOST_PARITY_V1|host=Unity|contentSha=" + unityImage.Header.Compatibility.ContentFingerprint
            + "|world=" + unityImage.Header.WorldId
            + "|day=" + unityImage.Header.Day
            + "|sequence=" + unityImage.Header.Sequence
            + "|commonSha=" + commonBehaviorSha);
        IReadOnlyList<OwnerSectionCensusSnapshot> afterAdvance =
            CoreFDailyV1OwnerClosureVectors.Capture(protocol, out P12ADailyV1Census afterAdvanceCensus);
        CoreFDailyV1OwnerClosureVectors.AssertAliasGroupsMatchReferences(afterAdvance, afterAdvanceCensus);
        CoreFDailyV1OwnerClosureVectors.AssertSameWitnesses(completedToken.OwnerSections, afterAdvance);

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long beforeRegistrationEpoch, out _), Is.True);
        var initialInventoryProviders = runtime.InventoryCensusProviders
            .Cast<NpcInventoryCensusProvider.INpcInventorySectionCensusProvider>()
            .ToArray();
        NpcRuntime misboundUnselected = new NpcRuntime("core-f-owner-closure-misbound-npc", null);
        DailyV1InventoryOwner selectedInventoryOwner = runtime.NpcRuntimes[0].SelectedRuntimeOwners.Inventory;
        ConstructorInfo inventoryProjectionConstructor = typeof(InventoryRuntime).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { selectedInventoryOwner.GetType() },
            modifiers: null);
        Assert.That(inventoryProjectionConstructor, Is.Not.Null);
        InventoryRuntime borrowedInventoryProjection = (InventoryRuntime)inventoryProjectionConstructor.Invoke(
            new object[] { selectedInventoryOwner });
        typeof(NpcRuntime).GetField("inventory", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(misboundUnselected, borrowedInventoryProjection);
        Assert.Throws<System.InvalidOperationException>(() =>
            NpcInventoryCensusProvider.CreateProviders(new[] { misboundUnselected }));

        NpcRuntime added = new NpcRuntime("core-f-owner-closure-added-npc", null);
        Assert.That(runtime.TryRegisterNpc(added, out WorldNpcRegistryFailure registerFailure), Is.True,
            registerFailure.ToString());
        int selectedInventoryOwnerCount = 0;
        int compatibilityInventoryOwnerCount = 0;
        foreach (IOwnerSectionCensusProvider provider in runtime.InventoryCensusProviders)
        {
            Assert.That(provider, Is.InstanceOf<NpcInventoryCensusProvider.INpcInventorySectionCensusProvider>());
            var inventoryProvider = (NpcInventoryCensusProvider.INpcInventorySectionCensusProvider)provider;
            if (ReferenceEquals(inventoryProvider.NpcOwner, added))
            {
                compatibilityInventoryOwnerCount++;
                Assert.That(inventoryProvider.UsesSelectedCoreOwner, Is.False,
                    "The unselected NPC contributes its required Unity compatibility owner row.");
                Assert.That(provider.GetCurrentCensus().OwnerInstanceIdentity,
                    Is.SameAs(inventoryProvider.InventoryOwner));
            }
            else
            {
                selectedInventoryOwnerCount++;
                Assert.That(inventoryProvider.UsesSelectedCoreOwner, Is.True,
                    "Existing selected rows retain their exact Core census provider across the roster transition.");
                Assert.That(inventoryProvider, Is.SameAs(initialInventoryProviders.Single(prior =>
                    string.Equals(prior.RuntimeId, inventoryProvider.RuntimeId, System.StringComparison.Ordinal))),
                    "A stable selected section keeps its exact registered provider object during dynamic membership reconciliation.");
                Assert.That(provider.GetCurrentCensus().OwnerInstanceIdentity,
                    Is.SameAs(inventoryProvider.NpcOwner.SelectedRuntimeOwners.Inventory));
            }
        }
        Assert.That(selectedInventoryOwnerCount, Is.EqualTo(10));
        Assert.That(compatibilityInventoryOwnerCount, Is.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterRegistrationEpoch, out _), Is.True);
        Assert.That(afterRegistrationEpoch, Is.EqualTo(beforeRegistrationEpoch + 1L),
            "The reserved NPC lifecycle mutation publishes one shared mutation epoch for its registered owner batch.");
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(completedToken, out tokenFailure), Is.False,
            "A supported owner mutation makes the previous completed-boundary token stale.");
        Assert.That(tokenFailure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));

        IReadOnlyList<OwnerSectionCensusSnapshot> evolved =
            CoreFDailyV1OwnerClosureVectors.Capture(protocol, out P12ADailyV1Census evolvedCensus);
        Assert.That(evolved, Has.Count.EqualTo(CoreFDailyV1OwnerClosureVectors.ExpectedSectionCount(
            runtime.NpcRuntimes.Count, 0, runtime.PersonStore.Persons.Count, runtime.Cities.Count)));
        Assert.That(evolved, Has.Count.EqualTo(322), "Adding one unbound NPC contributes 22 per-NPC rows and one residence row.");
        Assert.That(evolved.Select(row => row.SectionId), Is.Not.EqualTo(initial.Select(row => row.SectionId)));
        CoreFDailyV1OwnerClosureVectors.AssertAliasGroupsMatchReferences(evolved, evolvedCensus);

        Assert.That(runtime.TryAdvanceDay(out advanceFailure), Is.True, advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken refreshedToken, out tokenFailure),
            Is.True, tokenFailure.ToString());
        IReadOnlyList<OwnerSectionCensusSnapshot> refreshed =
            CoreFDailyV1OwnerClosureVectors.Capture(protocol, out P12ADailyV1Census refreshedCensus);
        CoreFDailyV1OwnerClosureVectors.AssertSameWitnesses(refreshedToken.OwnerSections, refreshed);
        CoreFDailyV1OwnerClosureVectors.AssertAliasGroupsMatchReferences(refreshed, refreshedCensus);
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(refreshedToken, out tokenFailure), Is.True,
            tokenFailure.ToString());
    }

    [Test]
    public void AdmittedSelectedPersonDeathUsesCoreP12BoundaryThroughUnityRuntimeDispatch()
    {
        UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(314159);
        try
        {
            RunAdmittedSelectedPersonDeathUsesCoreP12BoundaryThroughUnityRuntimeDispatch();
        }
        finally
        {
            UnityEngine.Random.state = previousRandomState;
        }
    }

    [Test]
    public void AdmittedSelectedPersonDeathRejectsCrossThreadWriterPermission()
    {
        UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(314159);
        try
        {
            RunAdmittedSelectedPersonDeathUsesCoreP12BoundaryThroughUnityRuntimeDispatch(
                probeCrossThreadWriters: true);
        }
        finally
        {
            UnityEngine.Random.state = previousRandomState;
        }
    }

    [Test]
    public void AdmittedSelectedPersonDeathRollsBackOwnersAndProjectionWhenPublishThrows()
    {
        UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(314160);
        try
        {
            RunAdmittedSelectedPersonDeathUsesCoreP12BoundaryThroughUnityRuntimeDispatch(
                throwAfterProjectionPublication: true);
        }
        finally
        {
            UnityEngine.Random.state = previousRandomState;
        }
    }

    private void RunAdmittedSelectedPersonDeathUsesCoreP12BoundaryThroughUnityRuntimeDispatch(
        bool probeCrossThreadWriters = false,
        bool throwAfterProjectionPublication = false)
    {
        SimulationConfigData configuration = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(configuration, Is.Not.Null);

        simulationObject = new GameObject("core-f-admitted-person-death-dispatch");
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, configuration);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();

        SimulationRuntime runtime = simulation.Bootstrap.Runtime;
        Assert.That(runtime, Is.Not.Null);
        DailyV1ExecutionBoundaryState executionState = runtime.ExecutionBoundaryState;
        PropertyInfo captureProfileProperty = typeof(SimulationRuntime).GetProperty(
            "IsDailyCaptureProfile", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(captureProfileProperty, Is.Not.Null);
        Assert.That((bool)captureProfileProperty.GetValue(runtime), Is.True,
            "The public selected-death dispatcher requires the admitted Daily-v1 profile.");
        Assert.That(executionState.HasAdmittedSelectedDailyExecution, Is.True);
        DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
        Assert.That(graph, Is.Not.Null);
        Assert.That(ReferenceEquals(graph.PersonStore, runtime.PersonStore), Is.True);
        Assert.That(runtime.NpcRuntimes, Has.Count.EqualTo(10));
        Assert.That(runtime.NpcRuntimes.All(candidate =>
            candidate != null && candidate.UsesSelectedDailyV1Definitions), Is.True,
            "This fixture must enter the all-selected Core death route; mixed-roster compatibility is covered separately.");

        NpcRuntime npc = runtime.NpcRuntimes.First(candidate =>
            candidate?.SelectedRuntimeOwners != null
            && graph.ContainsExactNpcOwner(candidate.SelectedRuntimeOwners));
        CityRuntime city = runtime.Cities.First(candidate =>
            candidate?.SelectedRuntimeOwners != null
            && graph.ContainsExactCityOwner(candidate.SelectedRuntimeOwners));
        Assert.That(city.CurrentPopulation, Is.GreaterThan(0));

        PersonRuntime person = new PersonRuntime(
            new PersonId("core-f-admitted-person-death-dispatch-person"), runtime.CurrentDay);
        Assert.That(runtime.TryRegisterPerson(person, out PersonStoreFailure registerFailure), Is.True,
            registerFailure.ToString());
        Assert.That(runtime.TryBindExistingNpcToPerson(
            person.PersonId, npc.RuntimeId, out PersonMaterializationFailure bindingFailure), Is.True,
            bindingFailure.ToString());
        Assert.That(runtime.TryBindExistingPersonResident(
            person.PersonId, city, out PersonResidenceMembershipFailure residenceFailure), Is.True,
            residenceFailure.ToString());

        PersonRuntime unrelatedPerson = new PersonRuntime(
            new PersonId("core-f-admitted-person-death-unrelated"), runtime.CurrentDay);
        Assert.That(runtime.TryRegisterPerson(unrelatedPerson, out PersonStoreFailure unrelatedRegisterFailure), Is.True,
            unrelatedRegisterFailure.ToString());

        Assert.That(graph.PersonStore.TryGet(person.PersonId, out PersonRuntime registeredPerson), Is.True);
        Assert.That(registeredPerson, Is.SameAs(person));
        Assert.That(graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
            npc.RuntimeId, out PersonRuntime materializedPerson), Is.True);
        Assert.That(materializedPerson, Is.SameAs(person));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));

        long epochBeforeDeath = executionState.MutationEpochState.CurrentEpoch;
        long personRevisionBeforeDeath = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
        long npcLifeRevisionBeforeDeath = npc.SelectedRuntimeOwners.Lifecycle.LifeStateRevision;
        long populationRevisionBeforeDeath = city.Population.Revision;
        int populationBeforeDeath = city.CurrentPopulation;
        Assert.That(runtime.TryProposePersonDeath(
            person.PersonId, out PersonDeathTransition transition,
            out PersonDeathLifecycleFailure proposalFailure), Is.True,
            proposalFailure.ToString());
        Assert.That(transition.ExpectsResident, Is.True);
        Assert.That(transition.ExpectsMaterializedNpc, Is.True);
        Assert.That(DailyV1SelectedPersonDeathOperation.TryPrepare(
            graph, runtime.PersonStore, person,
            transition.ExpectedAbsoluteDay,
            out DailyV1SelectedPersonDeathPlan deathPlan,
            out string deathPlanFailure), Is.True, deathPlanFailure);
        Assert.That(deathPlan.Person, Is.SameAs(transition.ExpectedPerson));
        Assert.That(deathPlan.AbsoluteDay, Is.EqualTo(transition.ExpectedAbsoluteDay));
        Assert.That(deathPlan.MaterializedNpc, Is.SameAs(npc.SelectedRuntimeOwners));
        Assert.That(deathPlan.MaterializedNpc.RuntimeId,
            Is.EqualTo(transition.ExpectedMaterializedNpcRuntimeId));
        Assert.That(deathPlan.IsResident, Is.EqualTo(transition.ExpectsResident));
        Assert.That(deathPlan.ResidenceCity.RuntimeId,
            Is.EqualTo(transition.ExpectedResidenceSettlementRuntimeId));
        Assert.That(deathPlan.ExpectedPopulationRevision,
            Is.EqualTo(transition.ExpectedResidencePopulationRevision));
        Assert.That(deathPlan.ExpectedPopulation,
            Is.EqualTo(transition.ExpectedResidencePopulation));
        Assert.That(PersonDeathLifecycleSystem.TryValidateDeath(
            runtime, transition, requireMaterializedNpc: false,
            out NpcRuntime validatedNpc,
            out PersonDeathLifecycleFailure validationFailure), Is.True, validationFailure.ToString());
        Assert.That(validatedNpc, Is.SameAs(npc));
        MethodInfo boundaryFactory = typeof(SimulationRuntime).GetMethod(
            "CreateSelectedPersonDeathBoundary",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(DailyV1SelectedPersonDeathPlan) },
            modifiers: null);
        Assert.That(boundaryFactory, Is.Not.Null);
        IDailyV1SelectedPersonDeathBoundary boundary =
            (IDailyV1SelectedPersonDeathBoundary)boundaryFactory.Invoke(
                runtime, new object[] { deathPlan });
        Assert.That(boundary, Is.TypeOf<DailyV1CoreSelectedPersonDeathBoundary>(),
            "An admitted all-selected Daily-v1 runtime must dispatch through the Core P12 boundary.");
        Assert.That(boundary.TryValidatePersonDeathProjection(
            deathPlan, out bool actionChanged, out string boundaryFailure), Is.True, boundaryFailure);
        Assert.That(actionChanged, Is.EqualTo(deathPlan.ExpectedAction != null));

        Assert.That(DailyV1SelectedPersonDeathOperation.TryCreateP12SectionIds(
            deathPlan, out IReadOnlyList<string> deathSections), Is.True);
        CityRuntime unrelatedCity = runtime.Cities.First(candidate =>
            candidate?.Population != null && !ReferenceEquals(candidate, city));
        Assert.That(boundary.TryBeginPersonDeathOperation(
            deathPlan, actionChanged, "runtime.person.death.unapproved",
            deathSections, () => true,
            out IDailyV1SelectedPersonDeathP12Scope wrongOperationScope), Is.False,
            "A different operation identity must not acquire the death writer permit.");
        Assert.That(wrongOperationScope, Is.Null);
        IReadOnlyList<string> expandedDeathSections = deathSections.Concat(new[]
        {
            DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(
                unrelatedCity.RuntimeId)
        }).ToArray();
        Assert.That(boundary.TryBeginPersonDeathOperation(
            deathPlan, actionChanged,
            DailyV1RuntimeProtocolOperationContracts.PersonDeath,
            expandedDeathSections, () => true,
            out IDailyV1SelectedPersonDeathP12Scope expandedScope), Is.False,
            "The permit must reject sections outside the exact selected death operation cut.");
        Assert.That(expandedScope, Is.Null);
        Assert.That(boundary.TryBeginPersonDeathOperation(
            deathPlan, actionChanged,
            DailyV1RuntimeProtocolOperationContracts.PersonDeath,
            deathSections, () =>
                PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(
                    deathPlan.Person, deathPlan.IsResident ? 2 : 1)
                && (deathPlan.MaterializedNpc == null
                    || (deathPlan.MaterializedNpc.Lifecycle.CanAdvanceRevision(1)
                        && (!actionChanged || deathPlan.MaterializedNpc.ActionSlot.CanAdvanceRevision(1))
                        && deathPlan.MaterializedNpc.CanCommitPersonDeathAfterValidation(
                            NpcInjurySeverity.None, deathPlan.ExpectedAction, deathPlan.ExpectedPayload)))
                && (!deathPlan.IsResident || PopulationOwnerBridge.CanAdvanceP12AggregateRevision(
                    deathPlan.ResidenceCity?.Population)),
            out IDailyV1SelectedPersonDeathP12Scope preflightScope), Is.True,
            "The exact selected death sections must be admitted before the commit path.");
        using (preflightScope)
        {
            Assert.That(PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(deathPlan.Person), Is.True,
                "The registered Person writer must be admitted by the exact P12 scope before commit.");
            Assert.That(PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(unrelatedPerson), Is.False,
                "The Core operation must not authorize a different Person in the same owner family.");

            Assert.That(boundary.TryBeginPersonDeathOperation(
                deathPlan, actionChanged,
                DailyV1RuntimeProtocolOperationContracts.PersonDeath,
                deathSections, () => true,
                out IDailyV1SelectedPersonDeathP12Scope nestedScope), Is.False,
                "A second Core death operation must not nest inside the active scope.");
            Assert.That(nestedScope, Is.Null);
            Assert.That(executionState.ActiveRuntimeOperationCount, Is.EqualTo(1));

            MethodInfo npcWriterAdmission = typeof(SimulationRuntime).GetMethod(
                "CanCommitP12NpcLifecycleMutation", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(npcWriterAdmission, Is.Not.Null);
            Assert.That((bool)npcWriterAdmission.Invoke(runtime, new object[]
            {
                npc, true, false, actionChanged, null
            }), Is.True,
                "The exact materialized NPC owner must be admitted for its selected death sections.");
            NpcRuntime unrelatedNpc = runtime.NpcRuntimes.First(candidate =>
                candidate != null && !ReferenceEquals(candidate, npc)
                && candidate.SelectedRuntimeOwners != null
                && graph.ContainsExactNpcOwner(candidate.SelectedRuntimeOwners));
            Assert.That((bool)npcWriterAdmission.Invoke(runtime, new object[]
            {
                unrelatedNpc, true, false, false, null
            }), Is.False, "The scoped permit must reject a different NPC owner.");

            MethodInfo populationWriterAdmission = typeof(SimulationRuntime).GetMethod(
                "CanCommitP12PopulationMutation", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(populationWriterAdmission, Is.Not.Null);
            Assert.That((bool)populationWriterAdmission.Invoke(runtime, new object[]
            {
                city.Population, true, false
            }), Is.True,
                "The exact City aggregate owner must be admitted for resident death.");
            Assert.That((bool)populationWriterAdmission.Invoke(runtime, new object[]
            {
                unrelatedCity.Population, true, false
            }), Is.False, "The scoped permit must reject a different City population owner.");
            Assert.That((bool)populationWriterAdmission.Invoke(runtime, new object[]
            {
                city.Population, true, true
            }), Is.False, "A receipt section is outside this selected death operation.");

            if (probeCrossThreadWriters)
            {
                bool crossThreadPersonAllowed = true;
                bool crossThreadNpcAllowed = true;
                bool crossThreadPopulationAllowed = true;
                Thread crossThreadProbe = new Thread(() =>
                {
                    crossThreadPersonAllowed =
                        PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(person);
                    crossThreadNpcAllowed = (bool)npcWriterAdmission.Invoke(runtime, new object[]
                    {
                        npc, true, false, actionChanged, null
                    });
                    crossThreadPopulationAllowed = (bool)populationWriterAdmission.Invoke(runtime,
                        new object[] { city.Population, true, false });
                }) { IsBackground = true };
                crossThreadProbe.Start();
                Assert.That(crossThreadProbe.Join(5000), Is.True,
                    "The cross-thread writer probe must finish without mutating the simulation.");
                Assert.That(crossThreadPersonAllowed, Is.False,
                    "The exact Person permit is bound to the active operation thread.");
                Assert.That(crossThreadNpcAllowed, Is.False,
                    "The exact NPC permit is bound to the active operation thread.");
                Assert.That(crossThreadPopulationAllowed, Is.False,
                    "The exact City permit is bound to the active operation thread.");
            }

            Assert.That(preflightScope.Complete(false), Is.EqualTo(!probeCrossThreadWriters),
                "The bounded death-operation preflight scope should abort cleanly after all unauthorized writer probes are rejected. " +
                "Projection=" + runtime.LastCoreSelectedPersonDeathProjectionFailure +
                "; Boundary=" + ((DailyV1CoreSelectedPersonDeathBoundary)boundary).LastPersonDeathCommitFailure +
                "; Census=" + executionState.LastSelectedOwnerSectionCensusRefreshRejection +
                "; ProtocolFaulted=" + executionState.IsSelectedRuntimeCensusProtocolFaulted);
        }
        Assert.That(executionState.MutationEpochState.CurrentEpoch, Is.EqualTo(epochBeforeDeath),
            "A rejected/no-op preflight releases its reservation without publishing a mutation.");
        Assert.That(executionState.ActiveRuntimeOperationCount, Is.Zero);
        if (probeCrossThreadWriters)
        {
            Assert.That(executionState.IsSelectedRuntimeCensusProtocolFaulted, Is.True,
                "The existing owner-thread contract fault-closes the P12 protocol after cross-thread use.");
            Assert.That(person.DeathAbsoluteDay.HasValue, Is.False);
            Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
            Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person), Is.EqualTo(personRevisionBeforeDeath));
            Assert.That(npc.SelectedRuntimeOwners.Lifecycle.LifeStateRevision, Is.EqualTo(npcLifeRevisionBeforeDeath));
            Assert.That(city.Population.Revision, Is.EqualTo(populationRevisionBeforeDeath));
            Assert.That(city.CurrentPopulation, Is.EqualTo(populationBeforeDeath));
            Assert.That(npc.SelectedRuntimeOwners.Lifecycle.LifeState, Is.Not.EqualTo(NpcLifeState.Dead));
            return;
        }
        Assert.That(executionState.IsSelectedRuntimeCensusProtocolFaulted, Is.False);
        Assert.That(PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(person), Is.False,
            "The compatibility writer permit must be revoked when the Core scope closes.");
        MethodInfo closedNpcWriterAdmission = typeof(SimulationRuntime).GetMethod(
            "CanCommitP12NpcLifecycleMutation", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo closedPopulationWriterAdmission = typeof(SimulationRuntime).GetMethod(
            "CanCommitP12PopulationMutation", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That((bool)closedNpcWriterAdmission.Invoke(runtime, new object[]
        {
            npc, true, false, actionChanged, null
        }), Is.False, "The NPC writer permit is revoked when the Core scope closes.");
        Assert.That((bool)closedPopulationWriterAdmission.Invoke(runtime, new object[]
        {
            city.Population, true, false
        }), Is.False, "The City writer permit is revoked when the Core scope closes.");

        Assert.That(PersonDeathLifecycleSystem.TryValidateDeath(
            runtime, transition, requireMaterializedNpc: false,
            out NpcRuntime revalidatedNpc,
            out PersonDeathLifecycleFailure revalidationFailure), Is.True,
            "The public death transition must remain valid after a clean Core preflight abort: "
                + revalidationFailure);
        Assert.That(revalidatedNpc, Is.SameAs(npc));
        Assert.That(string.IsNullOrWhiteSpace(transition.OperationIdentity), Is.True,
            "Core runtime death dispatch accepts only the non-replayed writer path.");
        Assert.That(runtime.NpcRuntimes.Any(candidate => candidate != null
            && !candidate.UsesSelectedDailyV1Definitions), Is.False,
            "This fixture must continue to exercise the all-selected Core dispatch path.");
        Assert.That(DailyV1SelectedPersonDeathOperation.TryPrepare(
            graph, runtime.PersonStore, transition.ExpectedPerson,
            transition.ExpectedAbsoluteDay,
            out DailyV1SelectedPersonDeathPlan retryPlan,
            out string retryPlanFailure), Is.True, retryPlanFailure);
        Assert.That(ReferenceEquals(retryPlan.Person, transition.ExpectedPerson), Is.True);
        Assert.That(retryPlan.AbsoluteDay, Is.EqualTo(transition.ExpectedAbsoluteDay));
        Assert.That(retryPlan.MaterializedNpc.RuntimeId,
            Is.EqualTo(transition.ExpectedMaterializedNpcRuntimeId));
        Assert.That(retryPlan.IsResident, Is.EqualTo(transition.ExpectsResident));
        Assert.That(retryPlan.ResidenceCity.RuntimeId,
            Is.EqualTo(transition.ExpectedResidenceSettlementRuntimeId));
        Assert.That(retryPlan.ExpectedPopulationRevision,
            Is.EqualTo(transition.ExpectedResidencePopulationRevision));
        Assert.That(retryPlan.ExpectedPopulation,
            Is.EqualTo(transition.ExpectedResidencePopulation));
        Assert.That(boundary.TryValidatePersonDeathProjection(
            deathPlan, out bool actionChangedAfterAbort, out string projectionFailure), Is.True,
            projectionFailure);
        Assert.That(actionChangedAfterAbort, Is.EqualTo(actionChanged));
        Assert.That(boundary.TryBeginPersonDeathOperation(
            deathPlan, actionChangedAfterAbort,
            DailyV1RuntimeProtocolOperationContracts.PersonDeath,
            deathSections, () => true,
            out IDailyV1SelectedPersonDeathP12Scope retryPreflightScope), Is.True,
            "The same exact Core P12 operation should remain admissible after a clean abort.");
        Assert.That(retryPreflightScope.Complete(false), Is.True);
        retryPreflightScope.Dispose();

        if (throwAfterProjectionPublication)
        {
            long personRevisionBeforeInjectedFailure =
                PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
            string residenceBeforeInjectedFailure = person.ResidenceSettlementRuntimeId;
            int populationBeforeInjectedFailure = city.CurrentPopulation;
            long populationRevisionBeforeInjectedFailure = city.Population.Revision;
            NpcLifeState npcLifeBeforeInjectedFailure = npc.SelectedRuntimeOwners.Lifecycle.LifeState;
            long npcLifeRevisionBeforeInjectedFailure =
                npc.SelectedRuntimeOwners.Lifecycle.LifeStateRevision;
            DailyV1ActionDefinition actionBeforeInjectedFailure =
                npc.SelectedRuntimeOwners.ActionSlot.CurrentAction;
            DailyV1NpcActionPayloadOwner payloadBeforeInjectedFailure =
                npc.SelectedRuntimeOwners.ActionSlot.CurrentPayload;
            long actionRevisionBeforeInjectedFailure = npc.SelectedRuntimeOwners.ActionSlot.Revision;
            bool projectedDeadBeforeInjectedFailure = npc.IsDead;
            long epochBeforeInjectedFailure = executionState.MutationEpochState.CurrentEpoch;
            Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
                executionState, DailyV1CaptureScopeAllowance.None,
                out IReadOnlyList<OwnerSectionCensusSnapshot> censusBeforeInjectedFailure,
                out long capturedEpochBeforeInjectedFailure,
                out DailyCaptureEligibilityFailure captureBeforeInjectedFailure), Is.True,
                captureBeforeInjectedFailure.ToString());
            Assert.That(capturedEpochBeforeInjectedFailure, Is.EqualTo(epochBeforeInjectedFailure));

            bool appliedAfterInjectedException = DailyV1SelectedPersonDeathOperation.TryExecute(
                graph,
                runtime.PersonStore,
                deathPlan,
                DailyV1RuntimeProtocolOperationContracts.PersonDeath,
                new ThrowAfterPersonDeathProjectionPublicationBoundary(boundary),
                out string injectedFailure);

            Assert.That(appliedAfterInjectedException, Is.False);
            Assert.That(injectedFailure, Does.Contain("compatibility projection rejected"));
            Assert.That(person.DeathAbsoluteDay, Is.Null);
            Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(residenceBeforeInjectedFailure));
            Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person),
                Is.EqualTo(personRevisionBeforeInjectedFailure));
            Assert.That(city.CurrentPopulation, Is.EqualTo(populationBeforeInjectedFailure));
            Assert.That(city.Population.Revision, Is.EqualTo(populationRevisionBeforeInjectedFailure));
            Assert.That(npc.SelectedRuntimeOwners.Lifecycle.LifeState,
                Is.EqualTo(npcLifeBeforeInjectedFailure));
            Assert.That(npc.SelectedRuntimeOwners.Lifecycle.LifeStateRevision,
                Is.EqualTo(npcLifeRevisionBeforeInjectedFailure));
            Assert.That(npc.SelectedRuntimeOwners.ActionSlot.CurrentAction,
                Is.SameAs(actionBeforeInjectedFailure));
            Assert.That(npc.SelectedRuntimeOwners.ActionSlot.CurrentPayload,
                Is.SameAs(payloadBeforeInjectedFailure));
            Assert.That(npc.SelectedRuntimeOwners.ActionSlot.Revision,
                Is.EqualTo(actionRevisionBeforeInjectedFailure));
            Assert.That(npc.IsDead, Is.EqualTo(projectedDeadBeforeInjectedFailure));
            Assert.That(executionState.MutationEpochState.CurrentEpoch,
                Is.EqualTo(epochBeforeInjectedFailure));
            Assert.That(executionState.MutationEpochState.HasActiveReservation, Is.False);
            Assert.That(executionState.ActiveRuntimeOperationCount, Is.Zero);
            Assert.That(executionState.IsSelectedRuntimeCensusProtocolFaulted, Is.False);
            MethodInfo rollbackNpcWriterAdmission = typeof(SimulationRuntime).GetMethod(
                "CanCommitP12NpcLifecycleMutation", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo rollbackPopulationWriterAdmission = typeof(SimulationRuntime).GetMethod(
                "CanCommitP12PopulationMutation", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(rollbackNpcWriterAdmission, Is.Not.Null);
            Assert.That(rollbackPopulationWriterAdmission, Is.Not.Null);
            Assert.That(PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(person), Is.False,
                "The Person compatibility permit is revoked after rollback.");
            Assert.That((bool)rollbackNpcWriterAdmission.Invoke(runtime, new object[]
            {
                npc, true, false, actionChanged, null
            }), Is.False, "The NPC compatibility permit is revoked after rollback.");
            Assert.That((bool)rollbackPopulationWriterAdmission.Invoke(runtime, new object[]
            {
                city.Population, true, false
            }), Is.False, "The City compatibility permit is revoked after rollback.");
            Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
                executionState, DailyV1CaptureScopeAllowance.None,
                out IReadOnlyList<OwnerSectionCensusSnapshot> censusAfterInjectedFailure,
                out long capturedEpochAfterInjectedFailure,
                out DailyCaptureEligibilityFailure captureAfterInjectedFailure), Is.True,
                captureAfterInjectedFailure.ToString());
            Assert.That(capturedEpochAfterInjectedFailure, Is.EqualTo(epochBeforeInjectedFailure));
            Assert.That(censusAfterInjectedFailure, Has.Count.EqualTo(censusBeforeInjectedFailure.Count));
            foreach (OwnerSectionCensusSnapshot before in censusBeforeInjectedFailure)
            {
                OwnerSectionCensusSnapshot after = censusAfterInjectedFailure.Single(
                    candidate => candidate.SectionId == before.SectionId);
                Assert.That(after.SchemaVersion, Is.EqualTo(before.SchemaVersion), before.SectionId);
                Assert.That(after.OwnerInstanceIdentity, Is.SameAs(before.OwnerInstanceIdentity), before.SectionId);
                Assert.That(after.Cardinality, Is.EqualTo(before.Cardinality), before.SectionId);
                Assert.That(after.Revision, Is.EqualTo(before.Revision), before.SectionId);
            }
        }

        Assert.That(runtime.TryApplyPersonDeath(
            transition, out PersonDeathLifecycleFailure failure), Is.True,
            failure + "; census=" + executionState.LastSelectedOwnerSectionCensusRefreshRejection
                + "; deathBoundary=" + runtime.LastCoreSelectedPersonDeathFailure
                + "; censusFaulted=" + executionState.IsSelectedRuntimeCensusProtocolFaulted
                + "; epoch=" + executionState.MutationEpochState.CurrentEpoch
                + "; activeOperations=" + executionState.ActiveRuntimeOperationCount
                + "; actionPresent=" + (npc.CurrentActionRuntime != null)
                + "; actionRevision=" + npc.SelectedRuntimeOwners.ActionSlot.Revision
                + "; personDead=" + person.DeathAbsoluteDay.HasValue
                + "; personResidence=" + person.ResidenceSettlementRuntimeId
                + "; npcLife=" + npc.SelectedRuntimeOwners.Lifecycle.LifeState
                + "; cityPopulation=" + city.CurrentPopulation
                + "; cityPopulationRevision=" + city.Population.Revision);

        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(runtime.CurrentDay));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person),
            Is.EqualTo(personRevisionBeforeDeath + 2L));
        Assert.That(npc.SelectedRuntimeOwners.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Dead));
        Assert.That(npc.SelectedRuntimeOwners.Lifecycle.LifeStateRevision,
            Is.EqualTo(npcLifeRevisionBeforeDeath + 1L));
        Assert.That(npc.IsDead, Is.True);
        Assert.That(npc.BoundPersonRuntime, Is.SameAs(person));
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBeforeDeath - 1));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevisionBeforeDeath + 1L));
        Assert.That(executionState.MutationEpochState.CurrentEpoch, Is.EqualTo(epochBeforeDeath + 1L));
        Assert.That(executionState.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(executionState.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(executionState.IsSelectedRuntimeCensusProtocolFaulted, Is.False);
        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            executionState, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long capturedEpoch,
            out DailyCaptureEligibilityFailure captureFailure), Is.True,
            captureFailure.ToString());
        Assert.That(capturedEpoch, Is.EqualTo(epochBeforeDeath + 1L));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);

        Assert.That(DailyV1SelectedPersonDeathOperation.TryPrepare(
            graph, runtime.PersonStore, unrelatedPerson, runtime.CurrentDay,
            out DailyV1SelectedPersonDeathPlan crossThreadPlan,
            out string crossThreadPlanFailure), Is.True, crossThreadPlanFailure);
        Assert.That(DailyV1SelectedPersonDeathOperation.TryCreateP12SectionIds(
            crossThreadPlan, out IReadOnlyList<string> crossThreadSections), Is.True);
        Assert.That(boundary.TryBeginPersonDeathOperation(
            crossThreadPlan, currentActionChanged: false,
            DailyV1RuntimeProtocolOperationContracts.PersonDeath,
            crossThreadSections,
            () => PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(unrelatedPerson),
            out IDailyV1SelectedPersonDeathP12Scope crossThreadScope), Is.True);
        long epochBeforeCrossThreadAttempt = executionState.MutationEpochState.CurrentEpoch;
        bool crossThreadWriteAdmitted = System.Threading.Tasks.Task.Run(
            () => PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(unrelatedPerson))
            .GetAwaiter().GetResult();
        Assert.That(crossThreadWriteAdmitted, Is.False,
            "A live compatibility permit must reject use from a different thread.");
        crossThreadScope.Complete(false);
        crossThreadScope.Dispose();
        Assert.That(unrelatedPerson.DeathAbsoluteDay, Is.Null);
        Assert.That(executionState.MutationEpochState.CurrentEpoch,
            Is.EqualTo(epochBeforeCrossThreadAttempt));
        Assert.That(executionState.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(executionState.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(unrelatedPerson), Is.False,
            "The cross-thread-rejected permit must be revoked when the scope closes.");
    }

    private static void AssertOwnerAlias(
        IReadOnlyList<OwnerSectionCensusSnapshot> rows,
        params string[] sectionIds)
    {
        object owner = rows.Single(row => row.SectionId == sectionIds[0]).OwnerInstanceIdentity;
        foreach (string sectionId in sectionIds)
            Assert.That(rows.Single(row => row.SectionId == sectionId).OwnerInstanceIdentity, Is.SameAs(owner), sectionId);
    }
}
