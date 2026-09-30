using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PersonStoreCensusTests
{
    private GameObject simulationObject;

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        if (simulationObject != null)
        {
            UnityEngine.Object.DestroyImmediate(simulationObject);
            simulationObject = null;
        }

        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void SelectedBootstrapPublishesStableExactZeroInstalledPersonWitnesses()
    {
        TesteSimulacao simulation = CreateSelectedSampleSimulation();
        PersonStore installed = simulation.Runtime.PersonStore;
        IReadOnlyList<IOwnerSectionCensusProvider> providers = simulation.Bootstrap.PersonStoreCensusProviders;

        Assert.That(providers, Has.Count.EqualTo(2));
        AssertCensus(providers[0].GetCurrentCensus(), PersonMembershipCensusProvider.SectionId, installed, 0, 0L);
        AssertCensus(providers[1].GetCurrentCensus(), PersonMaterializationBindingCensusProvider.SectionId, installed, 0, 0L);
        AssertCensus(providers[0].GetCurrentCensus(), PersonMembershipCensusProvider.SectionId, installed, 0, 0L);
        AssertCensus(providers[1].GetCurrentCensus(), PersonMaterializationBindingCensusProvider.SectionId, installed, 0, 0L);

        IList<IOwnerSectionCensusProvider> mutableView = providers as IList<IOwnerSectionCensusProvider>;
        Assert.That(mutableView, Is.Not.Null);
        Assert.That(mutableView.IsReadOnly, Is.True);
    }

    [Test]
    public void RegistryAndBindingWritesAdvanceSharedRevisionAndCompensationsRemainRevisioned()
    {
        PersonStore store = new PersonStore();
        IReadOnlyList<IOwnerSectionCensusProvider> providers = PersonStoreCensusProvider.CreateProviders(store);
        PersonRuntime person = new PersonRuntime(new PersonId("person-census-writes"));

        AssertWitnesses(providers, store, 0, 0, 0L);
        Assert.That(store.TryRegister(person, out PersonStoreFailure registrationFailure), Is.True, registrationFailure.ToString());
        AssertWitnesses(providers, store, 1, 0, 1L);

        Assert.That(store.TryRegister(new PersonRuntime(person.PersonId), out registrationFailure), Is.False);
        Assert.That(registrationFailure, Is.EqualTo(PersonStoreFailure.DuplicatePersonId));
        Assert.That(store.TryRegister(null, out registrationFailure), Is.False);
        Assert.That(registrationFailure, Is.EqualTo(PersonStoreFailure.InvalidPerson));
        AssertWitnesses(providers, store, 1, 0, 1L);

        Assert.That(TryBind(store, person.PersonId, "npc-census-writes", out PersonStoreFailure bindingFailure), Is.True);
        Assert.That(bindingFailure, Is.EqualTo(PersonStoreFailure.None));
        AssertWitnesses(providers, store, 1, 1, 2L);

        Assert.That(TryBind(store, person.PersonId, "npc-census-duplicate", out bindingFailure), Is.False);
        Assert.That(bindingFailure, Is.EqualTo(PersonStoreFailure.AlreadyMaterialized));
        Assert.That(TryBind(store, new PersonId("person-not-registered"), "npc-census-missing", out bindingFailure), Is.False);
        Assert.That(bindingFailure, Is.EqualTo(PersonStoreFailure.PersonNotRegistered));
        AssertWitnesses(providers, store, 1, 1, 2L);

        Assert.That(TryRollbackBinding(store, person.PersonId, "npc-census-writes"), Is.True);
        AssertWitnesses(providers, store, 1, 0, 3L);
        Assert.That(TryRollbackRegistration(store, person), Is.True);
        AssertWitnesses(providers, store, 0, 0, 4L);
    }

    [Test]
    public void RuntimeFailureAndPersonFactsOutsideStructuralScopeDoNotChangeWitness()
    {
        PersonStore source = new PersonStore();
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, null, economyEnabled: false, personStore: source);
        PersonStore installed = runtime.PersonStore;
        IReadOnlyList<IOwnerSectionCensusProvider> providers = PersonStoreCensusProvider.CreateProviders(installed);

        Assert.That(runtime.TryRegisterPerson(
            new PersonRuntime(new PersonId("person-census-future"), 1L),
            out PersonStoreFailure futureFailure), Is.False);
        Assert.That(futureFailure, Is.EqualTo(PersonStoreFailure.BirthAbsoluteDayInFuture));
        AssertWitnesses(providers, installed, 0, 0, 0L);

        PersonRuntime person = new PersonRuntime(new PersonId("person-census-facts"));
        Assert.That(installed.TryRegister(person, out _), Is.True);
        AssertWitnesses(providers, installed, 1, 0, 1L);
        Assert.That(typeof(PersonRuntime).GetMethod(
            "TryRecordDeath", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(person, new object[] { 0L }), Is.EqualTo(true));
        Assert.That(typeof(PersonRuntime).GetMethod(
            "TrySetResidenceSettlementRuntimeId", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(person, new object[] { "settlement-census-facts" }), Is.EqualTo(true));
        AssertWitnesses(providers, installed, 1, 0, 1L);
    }

    [Test]
    public void MutationGuardRejectionsLeavePersonWitnessUnchanged()
    {
        PersonStore store = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("person-census-guard"));
        Assert.That(store.TryRegister(person, out _), Is.True);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = PersonStoreCensusProvider.CreateProviders(store);

        Type guardType = typeof(PersonStore).Assembly.GetType("AuthoritativeMutationGuard");
        object guard = Activator.CreateInstance(guardType, true);
        Assert.That(typeof(PersonStore).GetMethod(
            "TryBindMutationGuard", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(store, new[] { guard }), Is.EqualTo(true));
        guardType.GetMethod("MarkFaulted", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(guard, new[] { Enum.Parse(guardType.Assembly.GetType("AuthoritativeMutationFaultReason"), "RollbackRestoreFailed") });

        Assert.That(store.TryRegister(new PersonRuntime(new PersonId("person-census-guard-rejected")),
            out PersonStoreFailure registrationFailure), Is.False);
        Assert.That(registrationFailure, Is.EqualTo(PersonStoreFailure.RuntimeFaulted));
        Assert.That(TryBind(store, person.PersonId, "npc-census-guard-rejected", out PersonStoreFailure bindingFailure), Is.False);
        Assert.That(bindingFailure, Is.EqualTo(PersonStoreFailure.RuntimeFaulted));
        AssertWitnesses(providers, store, 1, 0, 1L);
    }

    [Test]
    public void ForwardWritesFailClosedAtReservedHeadroomAndOverflowMapsThroughMaterialization()
    {
        PersonStore store = new PersonStore();
        IReadOnlyList<IOwnerSectionCensusProvider> providers = PersonStoreCensusProvider.CreateProviders(store);
        SetRevision(store, long.MaxValue - 1L);

        Assert.That(store.TryRegister(new PersonRuntime(new PersonId("person-census-overflow")), out PersonStoreFailure registrationFailure), Is.False);
        Assert.That(registrationFailure, Is.EqualTo(PersonStoreFailure.RevisionOverflow));
        AssertWitnesses(providers, store, 0, 0, long.MaxValue - 1L);
        SetRevision(store, long.MaxValue);
        Assert.That(store.TryRegister(new PersonRuntime(new PersonId("person-census-overflow-max")), out registrationFailure), Is.False);
        Assert.That(registrationFailure, Is.EqualTo(PersonStoreFailure.RevisionOverflow));
        AssertWitnesses(providers, store, 0, 0, long.MaxValue);

        PersonStore bindingStore = new PersonStore();
        PersonRuntime bindingPerson = new PersonRuntime(new PersonId("person-census-binding-overflow"));
        Assert.That(bindingStore.TryRegister(bindingPerson, out _), Is.True);
        IReadOnlyList<IOwnerSectionCensusProvider> bindingProviders = PersonStoreCensusProvider.CreateProviders(bindingStore);
        SetRevision(bindingStore, long.MaxValue - 1L);
        Assert.That(TryBind(bindingStore, bindingPerson.PersonId, "npc-census-overflow", out PersonStoreFailure bindingFailure), Is.False);
        Assert.That(bindingFailure, Is.EqualTo(PersonStoreFailure.RevisionOverflow));
        AssertWitnesses(bindingProviders, bindingStore, 1, 0, long.MaxValue - 1L);
        SetRevision(bindingStore, long.MaxValue);
        Assert.That(TryBind(bindingStore, bindingPerson.PersonId, "npc-census-overflow-max", out bindingFailure), Is.False);
        Assert.That(bindingFailure, Is.EqualTo(PersonStoreFailure.RevisionOverflow));
        AssertWitnesses(bindingProviders, bindingStore, 1, 0, long.MaxValue);

        PersonStore runtimeSource = new PersonStore();
        PersonRuntime runtimePerson = new PersonRuntime(new PersonId("person-census-mapped-overflow"));
        Assert.That(runtimeSource.TryRegister(runtimePerson, out _), Is.True);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, null, economyEnabled: false, personStore: runtimeSource);
        SetRevision(runtime.PersonStore, long.MaxValue - 1L);
        Assert.That(runtime.TryMaterializePerson(
            runtimePerson.PersonId,
            SimulationTestFactory.CreateNpc("person-census-mapped-overflow"),
            "npc-census-mapped-overflow",
            null,
            0f,
            out _,
            out PersonMaterializationFailure materializationFailure), Is.False);
        Assert.That(materializationFailure, Is.EqualTo(PersonMaterializationFailure.RevisionOverflow));
        Assert.That(runtime.PersonStore.MaterializedBindingCount, Is.Zero);
        Assert.That(runtime.PersonStore.Revision, Is.EqualTo(long.MaxValue - 1L));
    }

    [Test]
    public void ImmediateCompensationUsesReservedRevisionAndSaturatedRollbackRejectsBeforeMutation()
    {
        PersonStore registrationStore = new PersonStore();
        PersonRuntime registration = new PersonRuntime(new PersonId("person-census-reserved-registration"));
        SetRevision(registrationStore, long.MaxValue - 2L);
        Assert.That(registrationStore.TryRegister(registration, out _), Is.True);
        Assert.That(registrationStore.Revision, Is.EqualTo(long.MaxValue - 1L));
        Assert.That(TryRollbackRegistration(registrationStore, registration), Is.True);
        Assert.That(registrationStore.Persons, Is.Empty);
        Assert.That(registrationStore.Revision, Is.EqualTo(long.MaxValue));

        PersonStore bindingStore = new PersonStore();
        PersonRuntime boundPerson = new PersonRuntime(new PersonId("person-census-reserved-binding"));
        Assert.That(bindingStore.TryRegister(boundPerson, out _), Is.True);
        SetRevision(bindingStore, long.MaxValue - 2L);
        Assert.That(TryBind(bindingStore, boundPerson.PersonId, "npc-census-reserved", out _), Is.True);
        Assert.That(bindingStore.Revision, Is.EqualTo(long.MaxValue - 1L));
        Assert.That(TryRollbackBinding(bindingStore, boundPerson.PersonId, "npc-census-reserved"), Is.True);
        Assert.That(boundPerson.IsMaterialized, Is.False);
        Assert.That(bindingStore.MaterializedBindingCount, Is.Zero);
        Assert.That(bindingStore.Revision, Is.EqualTo(long.MaxValue));

        PersonStore saturatedRegistrationStore = new PersonStore();
        PersonRuntime saturatedRegistration = new PersonRuntime(new PersonId("person-census-saturated-registration"));
        Assert.That(saturatedRegistrationStore.TryRegister(saturatedRegistration, out _), Is.True);
        SetRevision(saturatedRegistrationStore, long.MaxValue);
        Assert.That(TryRollbackRegistration(saturatedRegistrationStore, saturatedRegistration), Is.False);
        Assert.That(saturatedRegistrationStore.TryGet(saturatedRegistration.PersonId, out PersonRuntime retained), Is.True);
        Assert.That(retained, Is.SameAs(saturatedRegistration));
        Assert.That(saturatedRegistrationStore.Revision, Is.EqualTo(long.MaxValue));

        PersonStore saturatedBindingStore = new PersonStore();
        PersonRuntime saturatedBoundPerson = new PersonRuntime(new PersonId("person-census-saturated-binding"));
        Assert.That(saturatedBindingStore.TryRegister(saturatedBoundPerson, out _), Is.True);
        Assert.That(TryBind(saturatedBindingStore, saturatedBoundPerson.PersonId, "npc-census-saturated", out _), Is.True);
        SetRevision(saturatedBindingStore, long.MaxValue);
        Assert.That(TryRollbackBinding(saturatedBindingStore, saturatedBoundPerson.PersonId, "npc-census-saturated"), Is.False);
        Assert.That(saturatedBoundPerson.IsMaterialized, Is.True);
        Assert.That(saturatedBindingStore.TryGetByMaterializedNpcRuntimeId("npc-census-saturated", out PersonRuntime indexed), Is.True);
        Assert.That(indexed, Is.SameAs(saturatedBoundPerson));
        Assert.That(saturatedBindingStore.MaterializedBindingCount, Is.EqualTo(1));
        Assert.That(saturatedBindingStore.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void CensusOverflowFailuresAppendWithoutRenumberingExistingValues()
    {
        Assert.That((int)PersonStoreFailure.RuntimeFaulted, Is.EqualTo(9));
        Assert.That((int)PersonStoreFailure.RevisionOverflow, Is.EqualTo(10));
        Assert.That((int)PersonMaterializationFailure.RuntimeFaulted, Is.EqualTo(16));
        Assert.That((int)PersonMaterializationFailure.RevisionOverflow, Is.EqualTo(17));
    }

    private TesteSimulacao CreateSelectedSampleSimulation()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
        Assert.That(config, Is.Not.Null);
        simulationObject = new GameObject("person-store-census-test");
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField(
            "simulationConfig",
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);
        simulation.Start();
        return simulation;
    }

    private static bool TryBind(
        PersonStore store,
        PersonId personId,
        string npcRuntimeId,
        out PersonStoreFailure failure)
    {
        object[] arguments = { personId, npcRuntimeId, PersonStoreFailure.None };
        bool result = (bool)typeof(PersonStore).GetMethod(
            "TryBindMaterializedNpc", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(store, arguments);
        failure = (PersonStoreFailure)arguments[2];
        return result;
    }

    private static bool TryRollbackRegistration(PersonStore store, PersonRuntime person)
    {
        return (bool)typeof(PersonStore).GetMethod(
            "TryRollbackRegistration", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(store, new object[] { person });
    }

    private static bool TryRollbackBinding(PersonStore store, PersonId personId, string npcRuntimeId)
    {
        return (bool)typeof(PersonStore).GetMethod(
            "TryRollbackMaterializedNpcBinding", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(store, new object[] { personId, npcRuntimeId });
    }

    private static void SetRevision(PersonStore store, long revision)
    {
        typeof(PersonStore).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(store, revision);
    }

    private static void AssertWitnesses(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        PersonStore owner,
        int people,
        int bindings,
        long revision)
    {
        AssertCensus(providers[0].GetCurrentCensus(), PersonMembershipCensusProvider.SectionId, owner, people, revision);
        AssertCensus(providers[1].GetCurrentCensus(), PersonMaterializationBindingCensusProvider.SectionId, owner, bindings, revision);
    }

    private static void AssertCensus(
        OwnerSectionCensusWitness witness,
        string sectionId,
        PersonStore owner,
        int cardinality,
        long revision)
    {
        Assert.That(witness.SectionId, Is.EqualTo(sectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(1));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(cardinality));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }

}
