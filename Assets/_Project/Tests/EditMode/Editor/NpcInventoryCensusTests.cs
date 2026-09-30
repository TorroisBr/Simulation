using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class NpcInventoryCensusTests
{
    [Test]
    public void ProvidersAreOrdinalPerNpcWitnessesAndDoNotMaterializeInventory()
    {
        NpcRuntime z = new NpcRuntime("z", null);
        NpcRuntime a = new NpcRuntime("a", null);
        InventoryRuntime aInventory = a.Inventory;
        aInventory.AddItem(SimulationTestFactory.CreateItem("ore-census", 1f), 3);
        var providers = NpcInventoryCensusProvider.CreateProviders(new[] { z, a });
        Assert.That(providers.Select(p => p.GetCurrentCensus().SectionId), Is.EqualTo(new[] { "p12f.inventory/a", "p12f.inventory/z" }));
        OwnerSectionCensusWitness witness = providers[0].GetCurrentCensus();
        Assert.That(witness.SchemaVersion, Is.EqualTo(1));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(aInventory));
        Assert.That(witness.Cardinality, Is.EqualTo(1));
        Assert.That(witness.Revision, Is.EqualTo(1));
        FieldInfo inventory = typeof(NpcRuntime).GetField("inventory", BindingFlags.Instance | BindingFlags.NonPublic);
        NpcRuntime unmaterialized = new NpcRuntime("lazy", null);
        inventory.SetValue(unmaterialized, null);
        Assert.Throws<InvalidOperationException>(() => NpcInventoryCensusProvider.CreateProviders(new[] { unmaterialized }));
        Assert.That(inventory.GetValue(unmaterialized), Is.Null);
        InventoryRuntime existingOwner = z.Inventory;
        FieldInfo rows = typeof(InventoryRuntime).GetField("items", BindingFlags.Instance | BindingFlags.NonPublic);
        rows.SetValue(existingOwner, null);
        var ownerProvider = NpcInventoryCensusProvider.CreateProviders(new[] { z })[0];
        Assert.Throws<InvalidOperationException>(() => ownerProvider.GetCurrentCensus());
        Assert.That(rows.GetValue(existingOwner), Is.Null, "Passive census must not initialize InventoryRuntime rows.");
    }

    [Test]
    public void RosterMembershipReconcilesInventoryWithOneEpochAndUnnotifiedWritesFailClosed()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null, economyEnabled: false);
        Assert.That(runtime.InventoryCensusProviders, Is.Empty);
        NpcRuntime npc = new NpcRuntime("inventory-roster", null);
        Assert.That(runtime.TryRegisterNpc(npc, out _), Is.True);
        Assert.That(runtime.InventoryCensusProviders, Has.Count.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long epoch, out _), Is.True);
        Assert.That(epoch, Is.EqualTo(1));
        Assert.That(runtime.TryAssessNpcRosterCensus(out _), Is.True);
        npc.Inventory.AddItem(SimulationTestFactory.CreateItem("inventory-drift", 1f), 1);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
    }

    [Test]
    public void UnregisterAndSameIdRegistrationPublishSeparateOwnerSections()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null, economyEnabled: false);
        NpcRuntime original = new NpcRuntime("reused-inventory-id", null);
        Assert.That(runtime.TryRegisterNpc(original, out _), Is.True);
        object originalOwner = runtime.InventoryCensusProviders[0].GetCurrentCensus().OwnerInstanceIdentity;
        Assert.That(runtime.TryUnregisterNpc(original.RuntimeId, out _), Is.True);
        Assert.That(runtime.InventoryCensusProviders, Is.Empty);
        NpcRuntime replacement = new NpcRuntime(original.RuntimeId, null);
        Assert.That(runtime.TryRegisterNpc(replacement, out _), Is.True);
        Assert.That(runtime.InventoryCensusProviders[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(replacement.Inventory));
        Assert.That(runtime.InventoryCensusProviders[0].GetCurrentCensus().OwnerInstanceIdentity, Is.Not.SameAs(originalOwner));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long epoch, out _), Is.True);
        Assert.That(epoch, Is.EqualTo(3));
    }

    [Test]
    public void SameRosterRuntimeIdOwnerReplacementFailsClosedWithoutReseedingInventory()
    {
        NpcRuntime original = new NpcRuntime("same-roster-inventory-id", null);
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, new[] { original }, economyEnabled: false);
        var providersBefore = runtime.InventoryCensusProviders;
        IOwnerSectionCensusProvider publishedBefore = providersBefore[0];
        object originalInventory = publishedBefore.GetCurrentCensus().OwnerInstanceIdentity;
        NpcRuntime replacement = new NpcRuntime(original.RuntimeId, null);
        object roster = typeof(SimulationRuntime).GetField("npcRuntimes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(runtime);
        ((System.Collections.Generic.List<NpcRuntime>)roster)[0] = replacement;

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(runtime.InventoryCensusProviders, Is.SameAs(providersBefore),
            "The protocol must retain its previous published snapshot and fail closed on unannounced owner replacement.");
        Assert.That(runtime.InventoryCensusProviders[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(originalInventory));
    }

    [Test]
    public void RosterInventoryReconciliationDoesNotMaskStaleCityProjection()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("inventory-city", "inventory-city-location");
        NpcRuntime npc = new NpcRuntime("inventory-city-npc", null);
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), new[] { city }, new[] { npc }, economyEnabled: false);
        city.AddImportantNpc(npc);
        typeof(NpcRuntime).GetField("currentCity", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc, null);
        typeof(NpcRuntime).GetField("currentLocation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc, null);
        Assert.That(runtime.TryUnregisterNpc(npc.RuntimeId, out _), Is.True);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure inventoryFailure), Is.True, inventoryFailure.ToString());
        IOwnerSectionCensusProvider cityProvider = CityNpcPresenceCensusProvider.CreateProviders(runtime.Cities, runtime.NpcRuntimes)[0];
        Assert.Throws<InvalidOperationException>(() => cityProvider.GetCurrentCensus(),
            "The separate City witness must continue to reject the retained non-rostered projection member.");
    }

    [Test]
    public void RosteredEmigratedAndDeadNpcRetainsInventorySection()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("inventory-life-city", "inventory-life-location");
        NpcRuntime npc = new NpcRuntime("inventory-life-npc", null);
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), new[] { city }, new[] { npc }, economyEnabled: false);
        OwnerSectionCensusWitness original = runtime.InventoryCensusProviders[0].GetCurrentCensus();
        Assert.That(runtime.TryApplyImmigration(npc, city, out _, out NpcPopulationLifecycleFailure failure), Is.True, failure.ToString());
        Assert.That(runtime.TryApplyEmigration(npc, city, out _, out failure), Is.True, failure.ToString());
        Assert.That(runtime.InventoryCensusProviders[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(original.OwnerInstanceIdentity));
        Assert.That(runtime.TryApplyImmigration(npc, city, out _, out failure), Is.True, failure.ToString());
        Assert.That(runtime.TryApplyResidentDeath(npc, city, out _, out failure), Is.True, failure.ToString());
        Assert.That(npc.IsDead, Is.True);
        Assert.That(runtime.InventoryCensusProviders[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(original.OwnerInstanceIdentity));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.True, censusFailure.ToString());
    }

    [Test]
    public void InventoryRevisionTracksSameRowValueChangesAndRowCardinalityChanges()
    {
        NpcRuntime npc = new NpcRuntime("inventory-revisions", null);
        InventoryRuntime owner = npc.Inventory;
        ItemData first = SimulationTestFactory.CreateItem("inventory-revision-first", 1f);
        ItemData second = SimulationTestFactory.CreateItem("inventory-revision-second", 2f);
        IOwnerSectionCensusProvider provider = NpcInventoryCensusProvider.CreateProviders(new[] { npc })[0];
        OwnerSectionCensusWitness start = provider.GetCurrentCensus();
        owner.AddItem(first, 2, 3f);
        OwnerSectionCensusWitness inserted = provider.GetCurrentCensus();
        Assert.That(inserted.Cardinality, Is.EqualTo(start.Cardinality + 1));
        Assert.That(inserted.Revision, Is.EqualTo(start.Revision + 1));
        owner.AddItem(first, 1, 6f);
        OwnerSectionCensusWitness sameRowChanged = provider.GetCurrentCensus();
        Assert.That(sameRowChanged.Cardinality, Is.EqualTo(inserted.Cardinality));
        Assert.That(sameRowChanged.Revision, Is.EqualTo(inserted.Revision + 1));
        owner.AddItem(second, 1, 2f);
        OwnerSectionCensusWitness secondRow = provider.GetCurrentCensus();
        Assert.That(secondRow.Cardinality, Is.EqualTo(2));
        Assert.That(secondRow.Revision, Is.EqualTo(sameRowChanged.Revision + 1));
        Assert.That(owner.RemoveItem(first, 3), Is.True);
        OwnerSectionCensusWitness removedRow = provider.GetCurrentCensus();
        Assert.That(removedRow.Cardinality, Is.EqualTo(1));
        Assert.That(removedRow.Revision, Is.EqualTo(secondRow.Revision + 1));
        long revisionBeforeNoOps = owner.Revision;
        owner.AddItem(null, 1);
        Assert.That(owner.RemoveItem(first, 99), Is.False);
        Assert.That(owner.Revision, Is.EqualTo(revisionBeforeNoOps));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(revisionBeforeNoOps));
    }
}
