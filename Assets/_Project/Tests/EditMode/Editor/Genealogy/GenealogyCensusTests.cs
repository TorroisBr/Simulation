using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GenealogyCensusTests
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
            Object.DestroyImmediate(simulationObject);
            simulationObject = null;
        }

        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void SelectedBootstrapPublishesStableExactZeroInstalledOwnerWitness()
    {
        TesteSimulacao simulation = CreateSelectedSampleSimulation();
        GenealogyStore installed = GetInstalledGenealogyStore(simulation.Runtime);
        IOwnerSectionCensusProvider provider = simulation.Bootstrap.GenealogyCensusProvider;

        AssertWitness(provider.GetCurrentCensus(), installed, 0, 0L);
        AssertWitness(provider.GetCurrentCensus(), installed, 0, 0L);
    }

    [Test]
    public void SelectedDailyCensusRegistersGateOneOwnersAndTracksParentageOperation()
    {
        TesteSimulacao simulation = CreateSelectedSampleSimulation();
        SimulationRuntime runtime = simulation.Runtime;
        GenealogyStore genealogy = GetInstalledGenealogyStore(runtime);
        PoliticalKnowledgeStore politicalKnowledge = GetPrivateField<PoliticalKnowledgeStore>(
            runtime, "politicalKnowledgeStore");
        PoliticalDecisionStore politicalDecisions = GetPrivateField<PoliticalDecisionStore>(
            runtime, "politicalDecisionStore");
        ContinuationCensusProtocol protocol = GetCensusProtocol(runtime);

        IDictionary expectedSections = (IDictionary)typeof(ContinuationCensusProtocol).GetField(
            "expectedSections", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(protocol);
        Assert.That(expectedSections.Count, Is.EqualTo(278));
        HashSet<string> expectedOperations = (HashSet<string>)typeof(ContinuationCensusProtocol).GetField(
            "expectedOperations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(protocol);
        Assert.That(expectedOperations.Count, Is.EqualTo(24));
        Assert.That(expectedOperations, Does.Contain("runtime.person.parentage"));

        AssertRegisteredRequiredOwner(protocol, GenealogyCensusProvider.SectionId,
            GenealogyCensusProvider.SchemaVersion, genealogy, 0, 0L);
        AssertRegisteredRequiredOwner(protocol, PoliticalKnowledgeStoreCensusProvider.SectionId,
            PoliticalKnowledgeStoreCensusProvider.SchemaVersion, politicalKnowledge, 0, 0L);
        AssertRegisteredRequiredOwner(protocol, PoliticalDecisionStoreCensusProvider.SectionId,
            PoliticalDecisionStoreCensusProvider.SchemaVersion, politicalDecisions, 0, 0L);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialCensusFailure),
            Is.True, initialCensusFailure.ToString());

        PersonId parent = new PersonId("selected-genealogy-parent");
        PersonId child = new PersonId("selected-genealogy-child");
        PersonId otherChild = new PersonId("selected-genealogy-other-child");
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(parent), out PersonStoreFailure parentFailure),
            Is.True, parentFailure.ToString());
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(child), out PersonStoreFailure childFailure),
            Is.True, childFailure.ToString());
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(otherChild), out PersonStoreFailure otherFailure),
            Is.True, otherFailure.ToString());
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure registeredCensusFailure),
            Is.True, registeredCensusFailure.ToString());

        long epoch = ReadMutationEpoch(protocol);
        Assert.That(runtime.TryAddParentage(parent, child, out PersonGenealogyFailure runtimeAddFailure),
            Is.True, runtimeAddFailure.ToString());
        Assert.That(ReadMutationEpoch(protocol), Is.EqualTo(++epoch));
        Assert.That(genealogy.Count, Is.EqualTo(1));
        Assert.That(genealogy.Revision, Is.EqualTo(1L));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterRuntimeAdd),
            Is.True, afterRuntimeAdd.ToString());

        Assert.That(runtime.TryAddParentage(new PersonId("selected-genealogy-unregistered"), child,
            out PersonGenealogyFailure missingParentFailure), Is.False);
        Assert.That(missingParentFailure, Is.EqualTo(PersonGenealogyFailure.ParentNotRegistered));
        Assert.That(ReadMutationEpoch(protocol), Is.EqualTo(epoch));
        Assert.That(genealogy.Count, Is.EqualTo(1));
        Assert.That(genealogy.Revision, Is.EqualTo(1L));

        Assert.That(runtime.TryAddParentage(parent, child,
            out PersonGenealogyFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure, Is.EqualTo(PersonGenealogyFailure.DuplicateParentage));
        Assert.That(ReadMutationEpoch(protocol), Is.EqualTo(epoch));
        Assert.That(genealogy.Count, Is.EqualTo(1));
        Assert.That(genealogy.Revision, Is.EqualTo(1L));

        Assert.That(runtime.TryAddParentage(parent, otherChild,
            out PersonGenealogyFailure secondAddFailure), Is.True, secondAddFailure.ToString());
        Assert.That(ReadMutationEpoch(protocol), Is.EqualTo(++epoch));
        Assert.That(genealogy.Count, Is.EqualTo(2));
        Assert.That(genealogy.Revision, Is.EqualTo(2L));

        Assert.That(runtime.TryRemoveParentage(parent, child, out PersonGenealogyFailure runtimeRemoveFailure),
            Is.True, runtimeRemoveFailure.ToString());
        Assert.That(ReadMutationEpoch(protocol), Is.EqualTo(++epoch));
        Assert.That(genealogy.Count, Is.EqualTo(1));
        Assert.That(genealogy.Revision, Is.EqualTo(3L));

        Assert.That(runtime.TryRemoveParentage(parent, otherChild,
            out PersonGenealogyFailure secondRemoveFailure), Is.True, secondRemoveFailure.ToString());
        Assert.That(ReadMutationEpoch(protocol), Is.EqualTo(++epoch));
        Assert.That(genealogy.Count, Is.Zero);
        Assert.That(genealogy.Revision, Is.EqualTo(4L));
        Assert.That(runtime.TryRemoveParentage(parent, otherChild,
            out PersonGenealogyFailure missingFailure), Is.False);
        Assert.That(missingFailure, Is.EqualTo(PersonGenealogyFailure.ParentageNotFound));
        Assert.That(ReadMutationEpoch(protocol), Is.EqualTo(epoch));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure finalCensusFailure),
            Is.True, finalCensusFailure.ToString());
    }

    [Test]
    public void SelectedDailyParentageRejectsWrongThreadBeforeOwnerWrite()
    {
        TesteSimulacao simulation = CreateSelectedSampleSimulation();
        SimulationRuntime runtime = simulation.Runtime;
        GenealogyStore genealogy = GetInstalledGenealogyStore(runtime);
        PersonId parent = new PersonId("selected-genealogy-wrong-thread-parent");
        PersonId child = new PersonId("selected-genealogy-wrong-thread-child");
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(parent), out _), Is.True);
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(child), out _), Is.True);

        bool added = true;
        PersonGenealogyFailure failure = PersonGenealogyFailure.None;
        Thread thread = new Thread(() =>
        {
            added = runtime.TryAddParentage(parent, child, out failure);
        });
        thread.Start();
        thread.Join();

        Assert.That(added, Is.False);
        Assert.That(failure, Is.EqualTo(PersonGenealogyFailure.RuntimeFaulted));
        Assert.That(genealogy.Count, Is.Zero);
        Assert.That(genealogy.Revision, Is.Zero);
    }

    [Test]
    public void SelectedDailyParentageRejectsWhenMutationEpochCapacityIsExhausted()
    {
        TesteSimulacao simulation = CreateSelectedSampleSimulation();
        SimulationRuntime runtime = simulation.Runtime;
        GenealogyStore genealogy = GetInstalledGenealogyStore(runtime);
        ContinuationCensusProtocol protocol = GetCensusProtocol(runtime);
        PersonId parent = new PersonId("selected-genealogy-epoch-parent");
        PersonId child = new PersonId("selected-genealogy-epoch-child");
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(parent), out _), Is.True);
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(child), out _), Is.True);
        SetPrivateField(protocol, "mutationEpoch", long.MaxValue);

        Assert.That(runtime.TryAddParentage(parent, child, out PersonGenealogyFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PersonGenealogyFailure.RuntimeFaulted));
        Assert.That(genealogy.Count, Is.Zero);
        Assert.That(genealogy.Revision, Is.Zero);
    }

    [Test]
    public void RuntimeWitnessTracksSuccessfulAddRemoveAndSameCardinalityReplacement()
    {
        PersonStore persons = new PersonStore();
        Register(persons, "genealogy-census-a");
        Register(persons, "genealogy-census-b");
        Register(persons, "genealogy-census-c");
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, null, economyEnabled: false, personStore: persons);
        GenealogyStore installed = GetInstalledGenealogyStore(runtime);
        GenealogyCensusProvider provider = new GenealogyCensusProvider(installed);
        AssertWitness(provider.GetCurrentCensus(), installed, 0, 0L);

        PersonId a = new PersonId("genealogy-census-a");
        PersonId b = new PersonId("genealogy-census-b");
        PersonId c = new PersonId("genealogy-census-c");
        Assert.That(runtime.TryAddParentage(a, b, out PersonGenealogyFailure firstAdd), Is.True, firstAdd.ToString());
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 1L);

        Assert.That(installed.TryAddParentage(null, b, out GenealogyFailure invalidParent), Is.False);
        Assert.That(invalidParent.Code, Is.EqualTo(GenealogyFailureCode.InvalidParent));
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 1L);
        Assert.That(installed.TryAddParentage(a, null, out GenealogyFailure invalidChild), Is.False);
        Assert.That(invalidChild.Code, Is.EqualTo(GenealogyFailureCode.InvalidChild));
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 1L);

        Assert.That(runtime.TryAddParentage(null, b, out PersonGenealogyFailure runtimeInvalidParent), Is.False);
        Assert.That(runtimeInvalidParent, Is.EqualTo(PersonGenealogyFailure.InvalidParent));
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 1L);
        Assert.That(runtime.TryAddParentage(new PersonId("genealogy-census-unregistered"), b,
            out PersonGenealogyFailure unregisteredParent), Is.False);
        Assert.That(unregisteredParent, Is.EqualTo(PersonGenealogyFailure.ParentNotRegistered));
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 1L);
        Assert.That(runtime.TryRemoveParentage(a, null, out PersonGenealogyFailure runtimeInvalidChild), Is.False);
        Assert.That(runtimeInvalidChild, Is.EqualTo(PersonGenealogyFailure.InvalidChild));
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 1L);

        Assert.That(runtime.TryAddParentage(a, b, out PersonGenealogyFailure duplicate), Is.False);
        Assert.That(duplicate, Is.EqualTo(PersonGenealogyFailure.DuplicateParentage));
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 1L);

        Assert.That(runtime.TryAddParentage(b, b, out PersonGenealogyFailure selfParent), Is.False);
        Assert.That(selfParent, Is.EqualTo(PersonGenealogyFailure.SelfParent));
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 1L);

        Assert.That(runtime.TryAddParentage(b, a, out PersonGenealogyFailure cycle), Is.False);
        Assert.That(cycle, Is.EqualTo(PersonGenealogyFailure.WouldCreateCycle));
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 1L);

        Assert.That(runtime.TryAddParentage(b, c, out PersonGenealogyFailure secondAdd), Is.True, secondAdd.ToString());
        AssertWitness(provider.GetCurrentCensus(), installed, 2, 2L);
        Assert.That(runtime.TryRemoveParentage(a, b, out PersonGenealogyFailure firstRemove), Is.True, firstRemove.ToString());
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 3L);

        Assert.That(runtime.TryRemoveParentage(a, b, out PersonGenealogyFailure missing), Is.False);
        Assert.That(missing, Is.EqualTo(PersonGenealogyFailure.ParentageNotFound));
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 3L);

        Assert.That(runtime.TryRemoveParentage(b, c, out PersonGenealogyFailure secondRemove), Is.True, secondRemove.ToString());
        AssertWitness(provider.GetCurrentCensus(), installed, 0, 4L);
        Assert.That(runtime.TryAddParentage(a, c, out PersonGenealogyFailure replacement), Is.True, replacement.ToString());
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 5L);
        Assert.That(runtime.GenealogyRecords, Has.Count.EqualTo(1));
        Assert.That(runtime.GenealogyRecords[0].ParentId, Is.EqualTo(a));
        Assert.That(runtime.GenealogyRecords[0].ChildId, Is.EqualTo(c));

        Assert.That(installed.TryAddParentage((ParentageRecord)null, out GenealogyFailure nullRecord), Is.False);
        Assert.That(nullRecord.Code, Is.EqualTo(GenealogyFailureCode.InvalidParentageRecord));
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 5L);
    }

    [Test]
    public void RuntimeCloneWitnessUsesInstalledReplayBaselineAndLeavesSourceUnchanged()
    {
        PersonStore persons = new PersonStore();
        Register(persons, "genealogy-clone-parent");
        Register(persons, "genealogy-clone-child");
        Register(persons, "genealogy-clone-other");
        PersonId parent = new PersonId("genealogy-clone-parent");
        PersonId child = new PersonId("genealogy-clone-child");
        PersonId other = new PersonId("genealogy-clone-other");

        GenealogyStore source = new GenealogyStore();
        Assert.That(source.TryAddParentage(parent, child, out _), Is.True);
        Assert.That(source.TryAddParentage(parent, other, out _), Is.True);
        Assert.That(source.TryRemoveParentage(parent, other, out _), Is.True);
        Assert.That(source.Count, Is.EqualTo(1));
        Assert.That(source.Revision, Is.EqualTo(3L));

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, null, economyEnabled: false, personStore: persons, genealogyStore: source);
        GenealogyStore installed = GetInstalledGenealogyStore(runtime);
        GenealogyCensusProvider provider = new GenealogyCensusProvider(installed);

        Assert.That(installed, Is.Not.SameAs(source));
        Assert.That(installed.Records, Is.EqualTo(source.Records));
        Assert.That(source.Count, Is.EqualTo(1));
        Assert.That(source.Revision, Is.EqualTo(3L));
        AssertWitness(provider.GetCurrentCensus(), installed, 1, 1L);
    }

    [Test]
    public void OrdinaryAddAndRemoveRemainClosedAtSaturatedRevision()
    {
        PersonStore persons = new PersonStore();
        Register(persons, "genealogy-saturated-parent");
        Register(persons, "genealogy-saturated-child");
        Register(persons, "genealogy-saturated-other");
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, null, economyEnabled: false, personStore: persons);
        GenealogyStore installed = GetInstalledGenealogyStore(runtime);
        PersonId parent = new PersonId("genealogy-saturated-parent");
        PersonId child = new PersonId("genealogy-saturated-child");
        PersonId other = new PersonId("genealogy-saturated-other");
        Assert.That(runtime.TryAddParentage(parent, child, out _), Is.True);
        SetPrivateField(installed, "revision", long.MaxValue);

        Assert.That(installed.TryAddParentage(parent, other, out GenealogyFailure addFailure), Is.False);
        Assert.That(addFailure.Code, Is.EqualTo(GenealogyFailureCode.RevisionOverflow));
        Assert.That(installed.TryRemoveParentage(parent, child, out GenealogyFailure removeFailure), Is.False);
        Assert.That(removeFailure.Code, Is.EqualTo(GenealogyFailureCode.RevisionOverflow));

        Assert.That(installed.Count, Is.EqualTo(1));
        Assert.That(installed.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(installed.IsDirectParent(parent, child), Is.True);
        Assert.That(installed.IsDirectParent(parent, other), Is.False);
    }

    private TesteSimulacao CreateSelectedSampleSimulation()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        simulationObject = new GameObject("genealogy-census-test");
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField(
            "simulationConfig",
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);
        typeof(TesteSimulacao).GetField(
            "runtimeAdmissionProfile",
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                simulation,
                SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();
        return simulation;
    }

    private static ContinuationCensusProtocol GetCensusProtocol(SimulationRuntime runtime)
    {
        FieldInfo field = typeof(SimulationRuntime).GetField(
            "npcRosterCensusProtocol",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        ContinuationCensusProtocol protocol = field.GetValue(runtime) as ContinuationCensusProtocol;
        Assert.That(protocol, Is.Not.Null);
        return protocol;
    }

    private static T GetPrivateField<T>(object instance, string fieldName) where T : class
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        T value = field.GetValue(instance) as T;
        Assert.That(value, Is.Not.Null, fieldName);
        return value;
    }

    private static long ReadMutationEpoch(ContinuationCensusProtocol protocol)
    {
        Assert.That(protocol.TryReadMutationEpoch(out long epoch, out ContinuationCensusFailure failure),
            Is.True, failure.ToString());
        return epoch;
    }

    private static void AssertRegisteredRequiredOwner(
        ContinuationCensusProtocol protocol,
        string sectionId,
        int schemaVersion,
        object owner,
        int cardinality,
        long revision)
    {
        IDictionary sections = (IDictionary)typeof(ContinuationCensusProtocol).GetField(
            "registeredSections", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(protocol);
        Assert.That(sections.Contains(sectionId), Is.True, sectionId);
        object section = sections[sectionId];
        FieldInfo contractField = section.GetType().GetField(
            "Contract", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo providerField = section.GetType().GetField(
            "Provider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        OwnerSectionContract contract = (OwnerSectionContract)contractField.GetValue(section);
        IOwnerSectionCensusProvider provider = (IOwnerSectionCensusProvider)providerField.GetValue(section);
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();

        Assert.That(contract.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(contract.SchemaVersion, Is.EqualTo(schemaVersion));
        Assert.That(witness.SectionId, Is.EqualTo(sectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(schemaVersion));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(cardinality));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }

    private static GenealogyStore GetInstalledGenealogyStore(SimulationRuntime runtime)
    {
        FieldInfo field = typeof(SimulationRuntime).GetField(
            "genealogyStore",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        GenealogyStore store = field.GetValue(runtime) as GenealogyStore;
        Assert.That(store, Is.Not.Null);
        return store;
    }

    private static void Register(PersonStore store, string id)
    {
        Assert.That(store.TryRegister(
            new PersonRuntime(new PersonId(id)),
            out PersonStoreFailure failure), Is.True, failure.ToString());
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(instance, value);
    }

    private static void AssertWitness(
        OwnerSectionCensusWitness witness,
        GenealogyStore owner,
        int cardinality,
        long revision)
    {
        Assert.That(witness.SectionId, Is.EqualTo(GenealogyCensusProvider.SectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(GenealogyCensusProvider.SchemaVersion));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(cardinality));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }
}
