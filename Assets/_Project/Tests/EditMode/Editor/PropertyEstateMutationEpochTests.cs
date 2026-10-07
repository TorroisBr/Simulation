using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PropertyEstateMutationEpochTests
{
    private readonly System.Collections.Generic.List<GameObject> simulationObjects =
        new System.Collections.Generic.List<GameObject>();

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
                UnityEngine.Object.DestroyImmediate(simulationObject);
        }
        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void DailyV1ProfileAddsPropertyEstateSectionsAlongsideIdentitySpatialOwners()
    {
        TesteSimulacao simulation = CreateSelectedDailyV1Simulation();
        SimulationRuntime runtime = simulation.Runtime;
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        IReadOnlyList<IOwnerSectionCensusProvider> propertyProviders =
            simulation.Bootstrap.PropertyOwnershipCensusProviders;
        IOwnerSectionCensusProvider estateProvider = simulation.Bootstrap.EstateCensusProvider;

        Assert.That(GetExpectedSectionCount(protocol), Is.EqualTo(260),
            "The selected Daily-v1 census also includes bounded political witnesses and exact-zero P8-D route-owner sections.");
        Assert.That(propertyProviders, Has.Count.EqualTo(2));
        Assert.That(propertyProviders[0].GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(runtime.PropertyOwnershipStore));
        Assert.That(propertyProviders[1].GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(runtime.PropertyOwnershipStore));
        Assert.That(estateProvider.GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(runtime.EstateStore));

        SimulationConfigData generalTest = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
        Assert.That(generalTest, Is.Not.Null);
        Assert.That(P10RuinLocalTopologyGenesis.IsEnabled(generalTest), Is.True,
            "The P10-A proving profile remains separate from Daily-v1.");
        Assert.That(P10RuinLocalTopologyGenesis.IsEnabled(
            AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
                "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset")), Is.False);
    }

    [Test]
    public void DailyAdmissionRegistersExactOwnersAndTracksBoundedPropertyEstateCommits()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        IReadOnlyList<IOwnerSectionCensusProvider> propertyProviders =
            PropertyOwnershipCensusProvider.CreateProviders(runtime.PropertyOwnershipStore);
        EstateCensusProvider estateProvider = new EstateCensusProvider(runtime.EstateStore);
        ContinuationCensusProtocol protocol = GetProtocol(runtime);

        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure initialFailure),
            Is.True, initialFailure.ToString());
        AssertPropertyWitnesses(propertyProviders, runtime.PropertyOwnershipStore, 0, 0, 0L);
        AssertEstateWitness(estateProvider, runtime.EstateStore, 0, 0L);
        Assert.That(ReadMutationEpoch(runtime), Is.Zero);

        PersonId firstOwner = new PersonId("p12-property-estate-first-owner");
        PersonId secondOwner = new PersonId("p12-property-estate-second-owner");
        PropertyId firstProperty = new PropertyId("p12-property-estate-first-property");
        Assert.That(runtime.TryRegisterPropertyOwnership(
            new PropertyOwnershipRecord(firstProperty, firstOwner),
            out PropertyFoundationFailure registrationFailure), Is.True, registrationFailure.ToString());
        AssertMutation(runtime, propertyProviders, estateProvider, 1L, 1, 0, 1L, 0, 0L);

        Assert.That(runtime.TryRegisterPropertyOwnership(
            new PropertyOwnershipRecord(firstProperty, secondOwner),
            out PropertyFoundationFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure.Code, Is.EqualTo(PropertyFoundationFailureCode.DuplicatePropertyId));
        AssertMutation(runtime, propertyProviders, estateProvider, 1L, 1, 0, 1L, 0, 0L);

        Assert.That(runtime.TryTransferProperty(
            firstProperty,
            secondOwner,
            runtime.CurrentDay,
            out PropertyTransferFailure transferFailure), Is.True, transferFailure.ToString());
        AssertMutation(runtime, propertyProviders, estateProvider, 2L, 1, 1, 2L, 0, 0L);

        Assert.That(runtime.TryTransferProperty(
            firstProperty,
            secondOwner,
            runtime.CurrentDay,
            out PropertyTransferFailure sameOwnerFailure), Is.False);
        Assert.That(sameOwnerFailure.Code, Is.EqualTo(PropertyTransferFailureCode.SameOwner));
        AssertMutation(runtime, propertyProviders, estateProvider, 2L, 1, 1, 2L, 0, 0L);

        EstateId estateId = new EstateId("p12-property-estate-record");
        PersonId deceased = new PersonId("p12-property-estate-deceased");
        Assert.That(runtime.TryOpenEstate(
            estateId,
            deceased,
            runtime.CurrentDay,
            out EstateRecord openedEstate,
            out EstateFoundationFailure estateFailure), Is.True, estateFailure.ToString());
        Assert.That(openedEstate.EstateId, Is.EqualTo(estateId));
        AssertMutation(runtime, propertyProviders, estateProvider, 3L, 1, 1, 2L, 1, 1L);

        Assert.That(runtime.TryOpenEstate(
            estateId,
            deceased,
            runtime.CurrentDay,
            out _,
            out EstateFoundationFailure duplicateEstateFailure), Is.False);
        Assert.That(duplicateEstateFailure.Code, Is.EqualTo(EstateFoundationFailureCode.DuplicateEstateId));
        AssertMutation(runtime, propertyProviders, estateProvider, 3L, 1, 1, 2L, 1, 1L);

        PropertyId estateProperty = new PropertyId("p12-property-estate-succession-property");
        Assert.That(runtime.TryRegisterPropertyOwnership(
            new PropertyOwnershipRecord(estateProperty, deceased),
            out PropertyFoundationFailure estatePropertyFailure), Is.True, estatePropertyFailure.ToString());
        AssertMutation(runtime, propertyProviders, estateProvider, 4L, 2, 1, 3L, 1, 1L);

        PersonId heir = new PersonId("p12-property-estate-heir");
        Assert.That(runtime.TryProposeEstateSuccession(
            estateId,
            estateProperty,
            heir,
            runtime.CurrentDay,
            out EstateSuccessionTransition staleSuccession,
            out EstateSuccessionFailure successionProposalFailure), Is.True,
            successionProposalFailure.ToString());

        Assert.That(runtime.TryTransferProperty(
            estateProperty,
            secondOwner,
            runtime.CurrentDay,
            out PropertyTransferFailure interveningTransferFailure), Is.True,
            interveningTransferFailure.ToString());
        AssertMutation(runtime, propertyProviders, estateProvider, 5L, 2, 2, 4L, 1, 1L);
        Assert.That(runtime.TryApplyEstateSuccession(
            staleSuccession,
            out EstateSuccessionFailure staleSuccessionFailure), Is.False);
        Assert.That(staleSuccessionFailure.Code,
            Is.EqualTo(EstateSuccessionFailureCode.PropertyTransferFailed));
        AssertMutation(runtime, propertyProviders, estateProvider, 5L, 2, 2, 4L, 1, 1L);

        PropertyId secondEstateProperty = new PropertyId("p12-property-estate-succession-property-2");
        Assert.That(runtime.TryRegisterPropertyOwnership(
            new PropertyOwnershipRecord(secondEstateProperty, deceased),
            out PropertyFoundationFailure secondEstatePropertyFailure), Is.True,
            secondEstatePropertyFailure.ToString());
        AssertMutation(runtime, propertyProviders, estateProvider, 6L, 3, 2, 5L, 1, 1L);
        Assert.That(runtime.TryProposeEstateSuccession(
            estateId,
            secondEstateProperty,
            heir,
            runtime.CurrentDay,
            out EstateSuccessionTransition succession,
            out EstateSuccessionFailure secondSuccessionProposalFailure), Is.True,
            secondSuccessionProposalFailure.ToString());
        Assert.That(runtime.TryApplyEstateSuccession(
            succession,
            out EstateSuccessionFailure successionFailure), Is.True, successionFailure.ToString());
        Assert.That(runtime.PropertyOwnershipStore.TryGet(
            secondEstateProperty,
            out PropertyOwnershipRecord transferredEstateProperty), Is.True);
        Assert.That(transferredEstateProperty.OwnerPersonId, Is.EqualTo(heir));
        AssertMutation(runtime, propertyProviders, estateProvider, 7L, 3, 3, 6L, 1, 1L);
    }

    [Test]
    public void OffOwnerThreadEstateSuccessionRefusalPrecedesAnyOwnerCommit()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        PersonId deceased = new PersonId("p12-property-estate-deceased");
        PersonId heir = new PersonId("p12-property-estate-heir");
        EstateId estateId = new EstateId("p12-property-estate-thread-record");
        PropertyId propertyId = new PropertyId("p12-property-estate-thread-property");
        Assert.That(runtime.TryOpenEstate(
            estateId,
            deceased,
            runtime.CurrentDay,
            out _,
            out EstateFoundationFailure estateFailure), Is.True, estateFailure.ToString());
        Assert.That(runtime.TryRegisterPropertyOwnership(
            new PropertyOwnershipRecord(propertyId, deceased),
            out PropertyFoundationFailure propertyFailure), Is.True, propertyFailure.ToString());
        Assert.That(runtime.TryProposeEstateSuccession(
            estateId,
            propertyId,
            heir,
            runtime.CurrentDay,
            out EstateSuccessionTransition transition,
            out EstateSuccessionFailure proposalFailure), Is.True, proposalFailure.ToString());

        PropertyOwnershipStore properties = runtime.PropertyOwnershipStore;
        EstateStore estates = runtime.EstateStore;
        long propertyRevision = properties.Revision;
        long estateRevision = estates.Revision;
        int propertyCount = properties.Count;
        int transferCount = properties.TransferHistory.Count;
        long epoch = ReadProtocolMutationEpoch(GetProtocol(runtime));
        EstateSuccessionFailure workerFailure = EstateSuccessionFailure.None;

        bool applied = Task.Run(() => runtime.TryApplyEstateSuccession(
            transition,
            out workerFailure)).GetAwaiter().GetResult();

        Assert.That(applied, Is.False);
        Assert.That(workerFailure.Code, Is.EqualTo(EstateSuccessionFailureCode.RuntimeFaulted));
        Assert.That(properties.Count, Is.EqualTo(propertyCount));
        Assert.That(properties.TransferHistory.Count, Is.EqualTo(transferCount));
        Assert.That(properties.Revision, Is.EqualTo(propertyRevision));
        Assert.That(estates.Revision, Is.EqualTo(estateRevision));
        Assert.That(ReadProtocolMutationEpoch(GetProtocol(runtime)), Is.EqualTo(epoch));
        Assert.That(ReadActiveOperationCount(GetProtocol(runtime)), Is.Zero);
    }

    [Test]
    public void DailyV1AdmissionRejectsInitiallyPopulatedPropertyOwnershipAndHistoryBeforePublication()
    {
        PropertyOwnershipStore suppliedProperties = null;
        EstateStore suppliedEstates = null;
        SimulationRuntime publishedRuntime = null;

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            publishedRuntime = CreateDailyRuntime((persons, properties, estates) =>
            {
                PersonId firstOwner = new PersonId("p12-property-estate-first-owner");
                PersonId secondOwner = new PersonId("p12-property-estate-second-owner");
                PropertyId propertyId = new PropertyId("p12-property-estate-initial-property");
                Assert.That(properties.TryRegister(
                    new PropertyOwnershipRecord(propertyId, firstOwner),
                    out PropertyFoundationFailure registerFailure), Is.True, registerFailure.ToString());
                Assert.That(PropertyTransferSystem.TryTransfer(
                    persons,
                    properties,
                    propertyId,
                    secondOwner,
                    50000L,
                    out _,
                    out PropertyTransferFailure transferFailure), Is.True, transferFailure.ToString());
                suppliedProperties = properties;
                suppliedEstates = estates;
            }));

        Assert.That(failure.Message, Does.Contain("P12 runtime-admission adapter"));
        Assert.That(publishedRuntime, Is.Null,
            "A Daily-v1 runtime with initial Property truth must not reach bootstrap publication.");
        Assert.That(suppliedProperties, Is.Not.Null);
        Assert.That(suppliedProperties.Count, Is.EqualTo(1));
        Assert.That(suppliedProperties.TransferHistory, Has.Count.EqualTo(1));
        Assert.That(suppliedEstates, Is.Not.Null);
        Assert.That(suppliedEstates.Count, Is.Zero);
    }

    [Test]
    public void DailyV1AdmissionRejectsInitiallyPopulatedEstateBeforePublication()
    {
        PropertyOwnershipStore suppliedProperties = null;
        EstateStore suppliedEstates = null;
        SimulationRuntime publishedRuntime = null;

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            publishedRuntime = CreateDailyRuntime((persons, properties, estates) =>
            {
                PersonId deceased = new PersonId("p12-property-estate-deceased");
                EstateId estateId = new EstateId("p12-property-estate-initial-estate");
                Assert.That(EstateOpeningSystem.TryOpenEstate(
                    persons,
                    estates,
                    estateId,
                    deceased,
                    50000L,
                    out _,
                    out EstateFoundationFailure openFailure), Is.True, openFailure.ToString());
                suppliedProperties = properties;
                suppliedEstates = estates;
            }));

        Assert.That(failure.Message, Does.Contain("P12 runtime-admission adapter"));
        Assert.That(publishedRuntime, Is.Null,
            "A Daily-v1 runtime with initial Estate truth must not reach bootstrap publication.");
        Assert.That(suppliedProperties, Is.Not.Null);
        Assert.That(suppliedProperties.Count, Is.Zero);
        Assert.That(suppliedEstates, Is.Not.Null);
        Assert.That(suppliedEstates.Count, Is.EqualTo(1));
    }

    private static SimulationRuntime CreateDailyRuntime()
    {
        return CreateDailyRuntime(null);
    }

    private static SimulationRuntime CreateDailyRuntime(
        System.Action<PersonStore, PropertyOwnershipStore, EstateStore> prepareInitialOwners)
    {
        long day = 50000L;
        PersonStore persons = new PersonStore();
        PersonId firstOwner = new PersonId("p12-property-estate-first-owner");
        PersonId secondOwner = new PersonId("p12-property-estate-second-owner");
        PersonId deceased = new PersonId("p12-property-estate-deceased");
        PersonId heir = new PersonId("p12-property-estate-heir");
        Assert.That(persons.TryRegister(new PersonRuntime(firstOwner, 0L), out _), Is.True);
        Assert.That(persons.TryRegister(new PersonRuntime(secondOwner, 0L), out _), Is.True);
        Assert.That(persons.TryRegister(new PersonRuntime(deceased, 0L, day), out _), Is.True);
        Assert.That(persons.TryRegister(new PersonRuntime(heir, 0L), out _), Is.True);

        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(deceased, heir, out GenealogyFailure genealogyFailure),
            Is.True, genealogyFailure.ToString());
        PropertyOwnershipStore properties = new PropertyOwnershipStore(persons);
        EstateStore estates = new EstateStore(persons);
        prepareInitialOwners?.Invoke(persons, properties, estates);

        return new SimulationRuntime(
            simulationTime: new SimulationTime(day),
            cities: System.Array.Empty<CityRuntime>(),
            npcRuntimes: null,
            economyEnabled: false,
            personStore: persons,
            genealogyStore: genealogy,
            propertyOwnershipStore: properties,
            estateStore: estates,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
    }

    private TesteSimulacao CreateSelectedDailyV1Simulation()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject simulationObject = new GameObject("p12-property-estate-daily-v1-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField(
            "simulationConfig",
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);
        typeof(TesteSimulacao).GetField(
            "runtimeAdmissionProfile",
            BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();
        return simulation;
    }

    private static ContinuationCensusProtocol GetProtocol(SimulationRuntime runtime)
    {
        return (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);
    }

    private static int GetExpectedSectionCount(ContinuationCensusProtocol protocol)
    {
        return ((Dictionary<string, OwnerSectionContract>)typeof(ContinuationCensusProtocol)
            .GetField("expectedSections", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(protocol)).Count;
    }

    private static long ReadMutationEpoch(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epoch,
            out ContinuationCensusFailure failure), Is.True, failure.ToString());
        return epoch;
    }

    private static int ReadActiveOperationCount(ContinuationCensusProtocol protocol)
    {
        return (int)typeof(ContinuationCensusProtocol)
            .GetField("activeOperationCount", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(protocol);
    }

    private static long ReadProtocolMutationEpoch(ContinuationCensusProtocol protocol)
    {
        return (long)typeof(ContinuationCensusProtocol)
            .GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(protocol);
    }

    private static void AssertMutation(
        SimulationRuntime runtime,
        IReadOnlyList<IOwnerSectionCensusProvider> propertyProviders,
        EstateCensusProvider estateProvider,
        long expectedEpoch,
        int ownershipCount,
        int transferCount,
        long propertyRevision,
        int estateCount,
        long estateRevision)
    {
        Assert.That(ReadMutationEpoch(runtime), Is.EqualTo(expectedEpoch));
        AssertPropertyWitnesses(
            propertyProviders,
            runtime.PropertyOwnershipStore,
            ownershipCount,
            transferCount,
            propertyRevision);
        AssertEstateWitness(estateProvider, runtime.EstateStore, estateCount, estateRevision);
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        Assert.That(ReadActiveOperationCount(protocol), Is.Zero);
        Assert.That(protocol.TryAssessRegisteredOperationQuiescence(
            out ContinuationCensusFailure quiescenceFailure), Is.True, quiescenceFailure.ToString());
        Assert.That(protocol.TryAssessOwnerSectionInventory(
            out ContinuationCensusFailure inventoryFailure), Is.True, inventoryFailure.ToString());
    }

    private static void AssertPropertyWitnesses(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        PropertyOwnershipStore owner,
        int ownershipCount,
        int transferCount,
        long revision)
    {
        Assert.That(providers, Has.Count.EqualTo(2));
        OwnerSectionCensusWitness ownership = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness transfers = providers[1].GetCurrentCensus();
        Assert.That(ownership.SectionId, Is.EqualTo(PropertyOwnershipCensusProvider.OwnershipSectionId));
        Assert.That(transfers.SectionId, Is.EqualTo(PropertyOwnershipCensusProvider.TransferHistorySectionId));
        Assert.That(ownership.SchemaVersion, Is.EqualTo(PropertyOwnershipCensusProvider.SchemaVersion));
        Assert.That(transfers.SchemaVersion, Is.EqualTo(PropertyOwnershipCensusProvider.SchemaVersion));
        Assert.That(ownership.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(transfers.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(ownership.Cardinality, Is.EqualTo(ownershipCount));
        Assert.That(transfers.Cardinality, Is.EqualTo(transferCount));
        Assert.That(ownership.Revision, Is.EqualTo(revision));
        Assert.That(transfers.Revision, Is.EqualTo(revision));
    }

    private static void AssertEstateWitness(
        IOwnerSectionCensusProvider provider,
        EstateStore owner,
        int count,
        long revision)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.SectionId, Is.EqualTo(EstateCensusProvider.SectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(EstateCensusProvider.SchemaVersion));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(count));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }
}
