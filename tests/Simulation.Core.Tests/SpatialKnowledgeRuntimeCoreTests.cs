using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class SpatialKnowledgeRuntimeCoreTests
{
    [Test]
    public void DiscoveryPreservesOrderedIdentityRevisionAndNoOpSemantics()
    {
        SpatialKnowledgeRuntime owner = new SpatialKnowledgeRuntime("npc/spatial");

        Assert.That(owner.DiscoverLocation("location/b"), Is.True);
        Assert.That(owner.DiscoverLocation("location/a"), Is.True);
        Assert.That(owner.DiscoverLocation("location/b"), Is.False);
        Assert.That(owner.DiscoverLocation(" "), Is.False);
        Assert.That(owner.DiscoverRoute("route/z"), Is.True);
        Assert.That(owner.DiscoverRoute("route/a"), Is.True);
        Assert.That(owner.DiscoverRoute("route/z"), Is.False);
        Assert.That(owner.DiscoverRoute(null), Is.False);

        Assert.That(owner.OwnerRuntimeId, Is.EqualTo("npc/spatial"));
        Assert.That(owner.KnownLocationRuntimeIds, Is.EqualTo(new[] { "location/b", "location/a" }));
        Assert.That(owner.KnownRouteRuntimeIds, Is.EqualTo(new[] { "route/z", "route/a" }));
        Assert.That(owner.Revision, Is.EqualTo(4L));
    }

    [Test]
    public void DiscoverUsesAdmissionBeforeMutationAndCommittedCallbackAfterMutation()
    {
        SpatialKnowledgeRuntime owner = new SpatialKnowledgeRuntime("npc/spatial");
        List<string> events = new List<string>();
        bool admissionAllowed = false;
        Func<bool> admission = () => { events.Add("admit:" + owner.Revision); return admissionAllowed; };
        Action committed = () => events.Add("commit:" + owner.Revision);
        owner.BindP12MutationBoundary(admission, committed);

        Assert.That(owner.DiscoverLocation("location/rejected"), Is.False);
        Assert.That(owner.Revision, Is.Zero);
        Assert.That(owner.KnownLocationRuntimeIds, Is.Empty);
        Assert.That(events, Is.EqualTo(new[] { "admit:0" }));

        admissionAllowed = true;
        Assert.That(owner.DiscoverLocation("location/accepted"), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(1L));
        Assert.That(events, Is.EqualTo(new[] { "admit:0", "admit:0", "commit:1" }),
            "Admission observes the old value while the committed callback observes the installed value.");
        Assert.That(owner.UnbindP12MutationBoundary(() => true, () => { }), Is.False,
            "Only the exact bound callbacks may unbind the owner.");
        Assert.That(owner.DiscoverLocation("location/accepted"), Is.False);
        Assert.That(events, Has.Count.EqualTo(3), "A duplicate no-op does not enter mutation admission.");
    }

    [Test]
    public void PreparedDiscoverInstallsOrderedChangesAtomicallyAndNotifiesOnce()
    {
        SpatialKnowledgeRuntime owner = new SpatialKnowledgeRuntime("npc/spatial");
        Assert.That(owner.DiscoverLocation("location/original"), Is.True);
        List<string> events = new List<string>();
        bool admissionAllowed = false;
        owner.BindP12MutationBoundary(
            () => { events.Add("admit:" + owner.Revision); return admissionAllowed; },
            () => events.Add("commit:" + owner.Revision));

        Assert.That(owner.TryPrepareDiscoverLocations(new[] { "location/rejected" },
            out SpatialKnowledgeDiscoveryInstall rejected), Is.True);
        Assert.That(rejected.CanInstall(owner), Is.False);
        owner.InstallPrepared(rejected);
        Assert.That(owner.KnownLocationRuntimeIds, Is.EqualTo(new[] { "location/original" }));
        Assert.That(owner.Revision, Is.EqualTo(1L));
        Assert.That(events, Is.EqualTo(new[] { "admit:1", "admit:1" }));

        events.Clear();
        admissionAllowed = true;
        Assert.That(owner.TryPrepareDiscoverLocations(new[]
        {
            "location/new-b", "location/original", "location/new-a", "location/new-b", " "
        }, out SpatialKnowledgeDiscoveryInstall prepared), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(1L), "Preparation does not mutate the authority.");
        Assert.That(owner.KnownLocationRuntimeIds, Is.EqualTo(new[] { "location/original" }));
        Assert.That(events, Is.Empty);
        Assert.That(prepared.CanInstall(owner), Is.True);
        owner.InstallPrepared(prepared);

        Assert.That(owner.KnownLocationRuntimeIds,
            Is.EqualTo(new[] { "location/original", "location/new-b", "location/new-a" }));
        Assert.That(owner.Revision, Is.EqualTo(3L));
        Assert.That(events, Is.EqualTo(new[] { "admit:1", "admit:1", "commit:3" }));
    }

    [Test]
    public void OwnerSnapshotHydrationPreservesExactOrderedRowsAndRevision()
    {
        Assert.That(SpatialKnowledgeRuntime.TryCreateFromOwnerSnapshot(
            "npc/snapshot", new[] { "location/b", "location/a" },
            new[] { "route/z", "route/a" }, 17L,
            out SpatialKnowledgeRuntime restored), Is.True);

        Assert.That(restored.OwnerRuntimeId, Is.EqualTo("npc/snapshot"));
        Assert.That(restored.KnownLocationRuntimeIds, Is.EqualTo(new[] { "location/b", "location/a" }));
        Assert.That(restored.KnownRouteRuntimeIds, Is.EqualTo(new[] { "route/z", "route/a" }));
        Assert.That(restored.Revision, Is.EqualTo(17L));
        Assert.That(SpatialKnowledgeRuntime.TryCreateFromOwnerSnapshot(
            "npc/duplicate", new[] { "location/a", "location/a" }, Array.Empty<string>(), 2L, out _), Is.False);
    }

    [Test]
    public void SelectedNpcCompositionRetainsTheInjectedSpatialKnowledgeAuthority()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npc = definitions.Npcs.Values.First();
        SpatialKnowledgeRuntime spatial = new SpatialKnowledgeRuntime("npc/selected");

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc/selected", definitions, npc, 0f, spatial,
            out DailyV1NpcRuntimeOwners initial), Is.True);
        Assert.That(initial.SpatialKnowledge, Is.SameAs(spatial));

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateBoundNpcOwner(
            "npc/selected", definitions, npc, initial.Status.Statuses,
            initial.Inventory, initial.MoneyAccount, initial.CommercialKnowledge,
            initial.MerchantTradePlan, spatial, out DailyV1NpcRuntimeOwners rebound), Is.True);
        Assert.That(rebound.SpatialKnowledge, Is.SameAs(spatial));
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateBoundNpcOwner(
            "npc/selected", definitions, npc, initial.Status.Statuses,
            initial.Inventory, initial.MoneyAccount, initial.CommercialKnowledge,
            initial.MerchantTradePlan, new SpatialKnowledgeRuntime("npc/other"), out _), Is.False);
    }
}
