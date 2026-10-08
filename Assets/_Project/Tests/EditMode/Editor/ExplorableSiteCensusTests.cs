using System.Collections.Generic;
using System.Linq;
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

    [Test]
    public void OwnerSnapshot_EmptyCaptureIsDetachedStableAndStagesAsExactZero()
    {
        ExplorableSiteStore owner = new ExplorableSiteStore();
        ExplorableSiteOwnerSnapshot first = owner.CaptureOwnerSnapshot();
        ExplorableSiteOwnerSnapshot second = owner.CaptureOwnerSnapshot();

        Assert.That(first.SchemaVersion, Is.EqualTo(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion));
        Assert.That(first.Revision, Is.Zero);
        Assert.That(first.Sites, Is.Empty);
        Assert.That(second.Revision, Is.EqualTo(first.Revision));
        Assert.That(second.Sites, Is.Empty);
        Assert.That(second.Sites, Is.Not.SameAs(first.Sites));

        Assert.That(ExplorableSiteStore.TryCreateFromOwnerSnapshot(
            first,
            new ExplorableSiteData[0],
            new SpatialLocationRuntime[0],
            out ExplorableSiteStore staged,
            out ExplorableSiteSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(failure.Code, Is.EqualTo(ExplorableSiteSnapshotFailureCode.None));
        Assert.That(staged, Is.Not.Null);
        Assert.That(staged, Is.Not.SameAs(owner));
        Assert.That(staged.Sites, Is.Empty);
        Assert.That(staged.Revision, Is.Zero);
    }

    [Test]
    public void OwnerSnapshot_PreservesOrderedIdentityDefinitionSharedLocationAndExactRevision()
    {
        ExplorableSiteStore owner = new ExplorableSiteStore();
        ExplorableSiteData firstDefinition = SimulationTestFactory.CreateExplorableSite("snapshot-definition-first");
        ExplorableSiteData secondDefinition = SimulationTestFactory.CreateExplorableSite("snapshot-definition-second");
        SpatialLocationRuntime sharedLocation = new SpatialLocationRuntime("snapshot-shared-location");
        ExplorableSiteRuntime first = new ExplorableSiteRuntime(
            "snapshot-site-first", firstDefinition, sharedLocation, "snapshot-instance-first");
        ExplorableSiteRuntime second = new ExplorableSiteRuntime(
            "snapshot-site-second", secondDefinition, sharedLocation, "snapshot-instance-second");
        Assert.That(owner.Add(first), Is.True);
        Assert.That(owner.Add(second), Is.True);

        ExplorableSiteOwnerSnapshot snapshot = owner.CaptureOwnerSnapshot();
        ExplorableSiteOwnerSnapshot repeated = owner.CaptureOwnerSnapshot();
        Assert.That(snapshot.Revision, Is.EqualTo(2L));
        Assert.That(snapshot.Revision, Is.EqualTo(snapshot.Sites.Count));
        Assert.That(repeated.Revision, Is.EqualTo(snapshot.Revision));
        Assert.That(repeated.Sites.Select(site => site.RuntimeId), Is.EqualTo(snapshot.Sites.Select(site => site.RuntimeId)));
        Assert.That(repeated.Sites.Select(site => site.SiteInstanceId), Is.EqualTo(snapshot.Sites.Select(site => site.SiteInstanceId)));
        Assert.That(repeated.Sites.Select(site => site.DefinitionId), Is.EqualTo(snapshot.Sites.Select(site => site.DefinitionId)));
        Assert.That(repeated.Sites.Select(site => site.LocationRuntimeId), Is.EqualTo(snapshot.Sites.Select(site => site.LocationRuntimeId)));
        Assert.That(snapshot.Sites.Select(site => site.RuntimeId), Is.EqualTo(new[]
        {
            "snapshot-site-first", "snapshot-site-second"
        }));
        Assert.That(snapshot.Sites[0].SiteInstanceId, Is.EqualTo("snapshot-instance-first"));
        Assert.That(snapshot.Sites[0].DefinitionId, Is.EqualTo(firstDefinition.DefinitionId));
        Assert.That(snapshot.Sites[0].LocationRuntimeId, Is.EqualTo(sharedLocation.RuntimeId));
        Assert.That(snapshot.Sites[1].SiteInstanceId, Is.EqualTo("snapshot-instance-second"));
        Assert.That(snapshot.Sites[1].DefinitionId, Is.EqualTo(secondDefinition.DefinitionId));
        Assert.That(snapshot.Sites[1].LocationRuntimeId, Is.EqualTo(sharedLocation.RuntimeId));

        Assert.That(ExplorableSiteStore.TryCreateFromOwnerSnapshot(
            snapshot,
            new[] { firstDefinition, secondDefinition },
            new[] { sharedLocation },
            out ExplorableSiteStore staged,
            out ExplorableSiteSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(staged.Revision, Is.EqualTo(2L));
        Assert.That(staged.Revision, Is.EqualTo(staged.Sites.Count));
        Assert.That(staged.Sites.Select(site => site.RuntimeId), Is.EqualTo(new[]
        {
            "snapshot-site-first", "snapshot-site-second"
        }));
        Assert.That(staged.Sites[0].SiteInstanceId, Is.EqualTo("snapshot-instance-first"));
        Assert.That(staged.Sites[1].SiteInstanceId, Is.EqualTo("snapshot-instance-second"));
        Assert.That(staged.Sites[0].Definition, Is.SameAs(firstDefinition));
        Assert.That(staged.Sites[1].Definition, Is.SameAs(secondDefinition));
        Assert.That(staged.Sites[0].Location, Is.SameAs(sharedLocation));
        Assert.That(staged.Sites[1].Location, Is.SameAs(sharedLocation));
        Assert.That(staged.GetForLocationRuntimeId(sharedLocation.RuntimeId), Has.Count.EqualTo(2));
        Assert.That(staged.GetForLocationRuntimeId(sharedLocation.RuntimeId)[0].RuntimeId,
            Is.EqualTo("snapshot-site-first"));
        Assert.That(staged.GetForLocationRuntimeId(sharedLocation.RuntimeId)[1].RuntimeId,
            Is.EqualTo("snapshot-site-second"));

        Assert.That(staged.Add(new ExplorableSiteRuntime(
            "snapshot-site-after-stage", firstDefinition, sharedLocation, "snapshot-instance-after-stage")), Is.True);
        Assert.That(staged.Revision, Is.EqualTo(3L));
        Assert.That(owner.Revision, Is.EqualTo(2L));
        Assert.That(owner.Count, Is.EqualTo(2));
    }

    [Test]
    public void OwnerSnapshotConstructor_CopiesRowsFromTheInputCollection()
    {
        ExplorableSiteOwnerSnapshotRecord original = new ExplorableSiteOwnerSnapshotRecord(
            "snapshot-copy-site", "snapshot-copy-instance", "snapshot-copy-definition", "snapshot-copy-location");
        List<ExplorableSiteOwnerSnapshotRecord> sourceRows = new List<ExplorableSiteOwnerSnapshotRecord>
        {
            original
        };
        ExplorableSiteOwnerSnapshot snapshot = new ExplorableSiteOwnerSnapshot(
            ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 1L, sourceRows);

        sourceRows[0] = new ExplorableSiteOwnerSnapshotRecord(
            "snapshot-replaced-site", "snapshot-replaced-instance", "snapshot-replaced-definition", "snapshot-replaced-location");
        sourceRows.Add(new ExplorableSiteOwnerSnapshotRecord(
            "snapshot-added-site", "snapshot-added-instance", "snapshot-added-definition", "snapshot-added-location"));

        Assert.That(snapshot.Sites, Has.Count.EqualTo(1));
        Assert.That(snapshot.Sites[0], Is.Not.SameAs(original));
        Assert.That(snapshot.Sites[0].RuntimeId, Is.EqualTo("snapshot-copy-site"));
        Assert.That(snapshot.Sites[0].SiteInstanceId, Is.EqualTo("snapshot-copy-instance"));
        Assert.That(snapshot.Sites[0].DefinitionId, Is.EqualTo("snapshot-copy-definition"));
        Assert.That(snapshot.Sites[0].LocationRuntimeId, Is.EqualTo("snapshot-copy-location"));
    }

    [Test]
    public void OwnerSnapshot_IsDetachedFromLaterSiteMutationsAndExposesReadOnlyRows()
    {
        ExplorableSiteStore owner = new ExplorableSiteStore();
        ExplorableSiteData definition = SimulationTestFactory.CreateExplorableSite("snapshot-detached-definition");
        SpatialLocationRuntime location = new SpatialLocationRuntime("snapshot-detached-location");
        Assert.That(owner.Add(new ExplorableSiteRuntime(
            "snapshot-detached-first", definition, location, "snapshot-detached-instance-first")), Is.True);

        ExplorableSiteOwnerSnapshot snapshot = owner.CaptureOwnerSnapshot();
        System.Collections.Generic.IList<ExplorableSiteOwnerSnapshotRecord> rows =
            snapshot.Sites as System.Collections.Generic.IList<ExplorableSiteOwnerSnapshotRecord>;
        Assert.That(rows, Is.Not.Null);
        Assert.That(rows.IsReadOnly, Is.True);
        Assert.Throws<System.NotSupportedException>(() => rows.Add(
            new ExplorableSiteOwnerSnapshotRecord("illegal", "illegal-instance", definition.DefinitionId, location.RuntimeId)));

        Assert.That(owner.Add(new ExplorableSiteRuntime(
            "snapshot-detached-second", definition, location, "snapshot-detached-instance-second")), Is.True);
        Assert.That(snapshot.Sites, Has.Count.EqualTo(1));
        Assert.That(snapshot.Revision, Is.EqualTo(1L));
        Assert.That(snapshot.Sites[0].RuntimeId, Is.EqualTo("snapshot-detached-first"));
        Assert.That(owner.Count, Is.EqualTo(2));
        Assert.That(owner.Revision, Is.EqualTo(2L));
    }

    [Test]
    public void OwnerSnapshot_RejectsInvalidIdentityDefinitionLocationAndRevisionFacts()
    {
        ExplorableSiteData definition = SimulationTestFactory.CreateExplorableSite("snapshot-valid-definition");
        ExplorableSiteData wrongDefinition = SimulationTestFactory.CreateExplorableSite("snapshot-other-definition");
        SpatialLocationRuntime location = new SpatialLocationRuntime("snapshot-valid-location");
        ExplorableSiteOwnerSnapshotRecord valid = new ExplorableSiteOwnerSnapshotRecord(
            "snapshot-valid-site", "snapshot-valid-instance", definition.DefinitionId, location.RuntimeId);
        ExplorableSiteOwnerSnapshotRecord[] duplicateRuntime =
        {
            valid,
            new ExplorableSiteOwnerSnapshotRecord("snapshot-valid-site", "snapshot-other-instance", definition.DefinitionId, location.RuntimeId)
        };
        ExplorableSiteOwnerSnapshotRecord[] duplicateInstance =
        {
            valid,
            new ExplorableSiteOwnerSnapshotRecord("snapshot-other-site", "snapshot-valid-instance", definition.DefinitionId, location.RuntimeId)
        };
        ExplorableSiteOwnerSnapshot[] invalidSnapshots =
        {
            null,
            new ExplorableSiteOwnerSnapshot(2, 0, new ExplorableSiteOwnerSnapshotRecord[0]),
            new ExplorableSiteOwnerSnapshot(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, -1, new ExplorableSiteOwnerSnapshotRecord[0]),
            new ExplorableSiteOwnerSnapshot(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 0, null),
            new ExplorableSiteOwnerSnapshot(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 0, new[] { valid }),
            new ExplorableSiteOwnerSnapshot(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 2, new[] { valid }),
            new ExplorableSiteOwnerSnapshot(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 1, new ExplorableSiteOwnerSnapshotRecord[] { null }),
            new ExplorableSiteOwnerSnapshot(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 1,
                new[] { new ExplorableSiteOwnerSnapshotRecord(" ", "snapshot-instance", definition.DefinitionId, location.RuntimeId) }),
            new ExplorableSiteOwnerSnapshot(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 2, duplicateRuntime),
            new ExplorableSiteOwnerSnapshot(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 2, duplicateInstance),
            new ExplorableSiteOwnerSnapshot(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 1,
                new[] { new ExplorableSiteOwnerSnapshotRecord("snapshot-site", "snapshot-instance", wrongDefinition.DefinitionId, location.RuntimeId) }),
            new ExplorableSiteOwnerSnapshot(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 1,
                new[] { new ExplorableSiteOwnerSnapshotRecord("snapshot-site", "snapshot-instance", definition.DefinitionId, "snapshot-missing-location") }),
            new ExplorableSiteOwnerSnapshot(ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 1,
                new[] { new ExplorableSiteOwnerSnapshotRecord(location.RuntimeId, "snapshot-instance", definition.DefinitionId, location.RuntimeId) })
        };

        foreach (ExplorableSiteOwnerSnapshot invalid in invalidSnapshots)
        {
            AssertStageRejected(invalid, new[] { definition }, new[] { location });
        }

        ExplorableSiteData duplicateDefinition = SimulationTestFactory.CreateExplorableSite(definition.DefinitionId);
        ExplorableSiteOwnerSnapshot validSnapshot = new ExplorableSiteOwnerSnapshot(
            ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 1, new[] { valid });
        AssertStageRejected(validSnapshot, new[] { definition, duplicateDefinition }, new[] { location });
        AssertStageRejected(validSnapshot, new[] { definition }, new[]
        {
            location, new SpatialLocationRuntime(location.RuntimeId)
        });
    }

    [Test]
    public void OwnerSnapshot_LateStagingFailureDoesNotPublishPartialOwnerOrChangeInputs()
    {
        ExplorableSiteStore activeOwner = new ExplorableSiteStore();
        ExplorableSiteData activeDefinition = SimulationTestFactory.CreateExplorableSite("snapshot-active-definition");
        SpatialLocationRuntime activeLocation = new SpatialLocationRuntime("snapshot-active-location");
        ExplorableSiteRuntime activeSite = new ExplorableSiteRuntime(
            "snapshot-active-site", activeDefinition, activeLocation, "snapshot-active-instance");
        Assert.That(activeOwner.Add(activeSite), Is.True);
        ExplorableSiteOwnerSnapshotRecord[] stagedRows =
        {
            new ExplorableSiteOwnerSnapshotRecord(
                "snapshot-staged-first", "snapshot-staged-instance-first", activeDefinition.DefinitionId, activeLocation.RuntimeId),
            new ExplorableSiteOwnerSnapshotRecord(
                "snapshot-staged-late", "snapshot-staged-instance-late", activeDefinition.DefinitionId, "snapshot-late-missing-location")
        };
        ExplorableSiteOwnerSnapshot invalid = new ExplorableSiteOwnerSnapshot(
            ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 2, stagedRows);
        ExplorableSiteData[] definitions = { activeDefinition };
        SpatialLocationRuntime[] locations = { activeLocation };

        Assert.That(ExplorableSiteStore.TryCreateFromOwnerSnapshot(
            invalid, definitions, locations, out ExplorableSiteStore staged, out ExplorableSiteSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(ExplorableSiteSnapshotFailureCode.MissingLegacyLocation));
        Assert.That(definitions, Has.Length.EqualTo(1));
        Assert.That(definitions[0], Is.SameAs(activeDefinition));
        Assert.That(locations, Has.Length.EqualTo(1));
        Assert.That(locations[0], Is.SameAs(activeLocation));
        Assert.That(activeOwner.Count, Is.EqualTo(1));
        Assert.That(activeOwner.Revision, Is.EqualTo(1L));
        Assert.That(activeOwner.Sites[0], Is.SameAs(activeSite));
        Assert.That(activeOwner.GetByRuntimeId(activeSite.RuntimeId), Is.SameAs(activeSite));
        Assert.That(activeOwner.GetForLocationRuntimeId(activeLocation.RuntimeId), Is.EqualTo(new[] { activeSite }));
    }

    private static void AssertStageRejected(
        ExplorableSiteOwnerSnapshot snapshot,
        ExplorableSiteData[] definitions,
        SpatialLocationRuntime[] locations)
    {
        Assert.That(ExplorableSiteStore.TryCreateFromOwnerSnapshot(
            snapshot, definitions, locations, out ExplorableSiteStore staged, out ExplorableSiteSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure.Code, Is.Not.EqualTo(ExplorableSiteSnapshotFailureCode.None));
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
