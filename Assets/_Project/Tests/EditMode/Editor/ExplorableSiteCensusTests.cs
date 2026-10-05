using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ExplorableSiteCensusTests
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
    public void SelectedP10AProfileHasAnExactWitnessForItsPublishedSiteOwner()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
        Assert.That(config, Is.Not.Null);

        GameObject simulationObject = new GameObject("p12b-site-census-profile-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);

        simulation.Start();

        ExplorableSiteStore owner = simulation.Bootstrap.ExplorableSites;
        ExplorableSiteCensusProvider provider = simulation.Bootstrap.ExplorableSiteCensusProvider;
        OwnerSectionCensusWitness first = provider.GetCurrentCensus();
        OwnerSectionCensusWitness second = provider.GetCurrentCensus();

        Assert.That(first.SectionId, Is.EqualTo(ExplorableSiteCensusProvider.SectionId));
        Assert.That(first.SchemaVersion, Is.EqualTo(ExplorableSiteCensusProvider.SchemaVersion));
        Assert.That(first.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(first.Cardinality, Is.EqualTo(1));
        Assert.That(first.Revision, Is.EqualTo(1L));
        Assert.That(second.OwnerInstanceIdentity, Is.SameAs(first.OwnerInstanceIdentity));
        Assert.That(second.Cardinality, Is.EqualTo(first.Cardinality));
        Assert.That(second.Revision, Is.EqualTo(first.Revision));
    }

    [Test]
    public void SuccessfulAddAdvancesSiteCardinalityAndRevisionExactlyOnce()
    {
        ExplorableSiteStore owner = new ExplorableSiteStore();
        ExplorableSiteCensusProvider provider = new ExplorableSiteCensusProvider(owner);
        ExplorableSiteRuntime site = CreateSite("site-one", "location-one");

        Assert.That(owner.Add(site), Is.True);

        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(1));
        Assert.That(witness.Revision, Is.EqualTo(1L));
        Assert.That(owner.GetByRuntimeId(site.RuntimeId), Is.SameAs(site));
    }

    [Test]
    public void InvalidAndDuplicateAddsLeaveTheSiteWitnessUnchanged()
    {
        ExplorableSiteStore owner = new ExplorableSiteStore();
        ExplorableSiteCensusProvider provider = new ExplorableSiteCensusProvider(owner);
        ExplorableSiteRuntime first = CreateSite("duplicate-site", "location-one");
        ExplorableSiteRuntime duplicate = CreateSite("duplicate-site", "location-two");

        Assert.That(owner.Add(first), Is.True);
        OwnerSectionCensusWitness before = provider.GetCurrentCensus();

        Assert.That(owner.Add(duplicate), Is.False);
        Assert.That(owner.Add(null), Is.False);

        AssertWitnessUnchanged(provider, owner, before);
        Assert.That(owner.GetByRuntimeId(first.RuntimeId), Is.SameAs(first));
    }

    [Test]
    public void FaultedOwnerGuardRejectsAddWithoutChangingTheSiteWitness()
    {
        ExplorableSiteStore owner = new ExplorableSiteStore();
        ExplorableSiteCensusProvider provider = new ExplorableSiteCensusProvider(owner);
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null);
        Assert.That(Bind(owner, GetGuard(runtime)), Is.True);
        MarkFaulted(runtime);
        OwnerSectionCensusWitness before = provider.GetCurrentCensus();

        Assert.That(owner.Add(CreateSite("guarded-site", "guarded-location")), Is.False);

        AssertWitnessUnchanged(provider, owner, before);
        Assert.That(owner.Sites, Is.Empty);
    }

    [Test]
    public void SaturatedRevisionRejectsNewSiteWithoutWrappingOrChangingCardinality()
    {
        ExplorableSiteStore owner = new ExplorableSiteStore();
        ExplorableSiteCensusProvider provider = new ExplorableSiteCensusProvider(owner);
        typeof(ExplorableSiteStore).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(owner, long.MaxValue);

        Assert.That(owner.Add(CreateSite("saturated-site", "saturated-location")), Is.False);

        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.Zero);
        Assert.That(witness.Revision, Is.EqualTo(long.MaxValue));
    }

    private static ExplorableSiteRuntime CreateSite(string runtimeId, string locationRuntimeId)
    {
        return new ExplorableSiteRuntime(
            runtimeId,
            SimulationTestFactory.CreateExplorableSite("definition-" + runtimeId),
            new SpatialLocationRuntime(locationRuntimeId));
    }

    private static void AssertWitnessUnchanged(
        ExplorableSiteCensusProvider provider,
        ExplorableSiteStore owner,
        OwnerSectionCensusWitness expected)
    {
        OwnerSectionCensusWitness actual = provider.GetCurrentCensus();
        Assert.That(actual.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(actual.Cardinality, Is.EqualTo(expected.Cardinality));
        Assert.That(actual.Revision, Is.EqualTo(expected.Revision));
    }

    private static object GetGuard(SimulationRuntime runtime)
    {
        PropertyInfo property = typeof(SimulationRuntime).GetProperty(
            "MutationGuard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null);
        return property.GetValue(runtime);
    }

    private static bool Bind(object authority, object guard)
    {
        MethodInfo method = authority.GetType().GetMethod(
            "TryBindMutationGuard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        return (bool)method.Invoke(authority, new[] { guard });
    }

    private static void MarkFaulted(SimulationRuntime runtime)
    {
        MethodInfo method = typeof(SimulationRuntime).GetMethod(
            "MarkAuthoritativeMutationFaulted",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(runtime, new object[] { AuthoritativeMutationFaultReason.RollbackRestoreFailed });
    }
}
