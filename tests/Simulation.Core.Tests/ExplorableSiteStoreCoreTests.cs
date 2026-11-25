using System;
using System.Linq;
using NUnit.Framework;

public sealed class ExplorableSiteStoreCoreTests
{
    [Test]
    public void CapturesAndStagesExactSiteIdentityDefinitionLocationAndRevision()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1SelectedRuntimeOwnerGraphBuilder.TryBuildConfigured(
            definitions, new RuntimeIdAllocator(), out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        SpatialLocationRuntime location = graph.SpatialNetwork.Locations.First();
        DailyV1ExplorableSiteDefinition definition = CreateDefinition("site-definition/core");
        ExplorableSiteRuntime sourceSite = new ExplorableSiteRuntime(
            "site-runtime/core", definition, location, "site-instance/core");
        ExplorableSiteStore source = new ExplorableSiteStore();
        Assert.That(source.Add(sourceSite), Is.True);
        Assert.That(source.Revision, Is.EqualTo(1L));

        Assert.That(source.TryCaptureOwnerSnapshot(
            out ExplorableSiteOwnerSnapshot snapshot,
            out ExplorableSiteSnapshotFailure captureFailure), Is.True, captureFailure?.Code.ToString());

        Assert.That(ExplorableSiteStore.TryCreateFromOwnerSnapshot(
            snapshot, new[] { definition }, new[] { location },
            out ExplorableSiteStore staged, out ExplorableSiteSnapshotFailure stageFailure),
            Is.True, stageFailure?.Code.ToString());

        Assert.That(staged, Is.Not.SameAs(source));
        Assert.That(staged.Revision, Is.EqualTo(source.Revision));
        Assert.That(staged.Count, Is.EqualTo(1));
        Assert.That(staged.GetByRuntimeId(sourceSite.RuntimeId).SiteInstanceId, Is.EqualTo(sourceSite.SiteInstanceId));
        Assert.That(staged.GetByRuntimeId(sourceSite.RuntimeId).Definition, Is.SameAs(definition));
        Assert.That(staged.GetByRuntimeId(sourceSite.RuntimeId).Location, Is.SameAs(location));
    }

    [Test]
    public void RejectsDuplicateSiteRuntimeOrInstanceIdentityInDetachedSnapshot()
    {
        DailyV1ExplorableSiteDefinition definition = CreateDefinition("site-definition/duplicate");
        SpatialLocationRuntime location = new SpatialLocationRuntime("location/duplicate");
        ExplorableSiteOwnerSnapshot snapshot = new ExplorableSiteOwnerSnapshot(
            ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, 2L,
            new[]
            {
                new ExplorableSiteOwnerSnapshotRecord("site/runtime-one", "site/instance", definition.DefinitionId, location.RuntimeId),
                new ExplorableSiteOwnerSnapshotRecord("site/runtime-two", "site/instance", definition.DefinitionId, location.RuntimeId)
            });

        Assert.That(ExplorableSiteStore.TryCreateFromOwnerSnapshot(
            snapshot, new[] { definition }, new[] { location },
            out ExplorableSiteStore staged, out ExplorableSiteSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(ExplorableSiteSnapshotFailureCode.DuplicateSiteIdentity));
    }

    private static DailyV1ExplorableSiteDefinition CreateDefinition(string id) =>
        new DailyV1ExplorableSiteDefinition("ExplorableSiteData", id,
            new DailyV1ExplorableSiteData(id, "Fixture site", new DailyV1EnumValue(0, "Ruin")));
}