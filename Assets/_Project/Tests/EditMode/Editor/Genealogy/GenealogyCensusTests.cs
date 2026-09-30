using System.Reflection;
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

    private TesteSimulacao CreateSelectedSampleSimulation()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
        Assert.That(config, Is.Not.Null);
        simulationObject = new GameObject("genealogy-census-test");
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField(
            "simulationConfig",
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);
        simulation.Start();
        return simulation;
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
