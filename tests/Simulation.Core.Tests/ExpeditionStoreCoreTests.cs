using System;
using NUnit.Framework;

public sealed class ExpeditionStoreCoreTests
{
    [Test]
    public void CoreOwnerRevisionCensusAndP12SnapshotRoundTripRemainBoundToExactStore()
    {
        ExpeditionStore owner = new ExpeditionStore();
        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "expedition-1",
            "site-1",
            "location-origin",
            "location-target",
            "route-outbound",
            null,
            "decision-1",
            new[] { "npc-1" },
            new[] { "npc-1" },
            Array.Empty<string>(),
            ExpeditionState.AtSite,
            ExpeditionObjectiveRuntime.Explore(requiredProgress: 2));

        Assert.That(owner.Add(expedition), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(1));
        Assert.That(owner.ActiveExpeditions.Count, Is.EqualTo(1));
        Assert.That(owner.GetById("expedition-1"), Is.SameAs(expedition));

        OwnerSectionCensusWitness initial = new ExpeditionCensusProvider(owner).GetCurrentCensus();
        Assert.That(initial.SectionId, Is.EqualTo("p12f.expeditions"));
        Assert.That(initial.SchemaVersion, Is.EqualTo(1));
        Assert.That(initial.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(initial.Cardinality, Is.EqualTo(1));
        Assert.That(initial.Revision, Is.EqualTo(1));

        Assert.That(expedition.TryBeginExploration(), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(2));
        Assert.That(expedition.TryAdvanceAbstractProgress(), Is.True);
        Assert.That(expedition.ExplorationProgress, Is.EqualTo(1));
        Assert.That(owner.Revision, Is.EqualTo(3));

        Assert.That(owner.TryCaptureOwnerSnapshot(out P12FExpeditionOwnerSnapshot snapshot), Is.True);
        Assert.That(snapshot.Revision, Is.EqualTo(3));
        Assert.That(snapshot.Expeditions.Count, Is.EqualTo(1));
        Assert.That(snapshot.Expeditions[0].ExpeditionId, Is.EqualTo("expedition-1"));
        Assert.That(snapshot.Expeditions[0].State, Is.EqualTo(ExpeditionState.Exploring));
        Assert.That(snapshot.Expeditions[0].ObjectiveType, Is.EqualTo(ExpeditionObjectiveType.Explore));
        Assert.That(snapshot.Expeditions[0].Progress, Is.EqualTo(1));

        Assert.That(ExpeditionStore.TryCreateFromOwnerSnapshot(snapshot, out ExpeditionStore restored), Is.True);
        Assert.That(restored, Is.Not.Null);
        Assert.That(restored.Revision, Is.EqualTo(3));
        Assert.That(restored.ActiveExpeditions.Count, Is.EqualTo(1));
        ExpeditionRuntime restoredExpedition = restored.GetById("expedition-1");
        Assert.That(restoredExpedition, Is.Not.Null);
        Assert.That(restoredExpedition, Is.Not.SameAs(expedition));
        Assert.That(restoredExpedition.State, Is.EqualTo(ExpeditionState.Exploring));
        Assert.That(restoredExpedition.MemberRuntimeIds, Is.EqualTo(new[] { "npc-1" }));
        Assert.That(restoredExpedition.PerformerRuntimeIds, Is.EqualTo(new[] { "npc-1" }));
        Assert.That(restoredExpedition.SupportRuntimeIds, Is.Empty);
        Assert.That(restoredExpedition.ExplorationProgress, Is.EqualTo(1));

        OwnerSectionCensusWitness restoredWitness = new ExpeditionCensusProvider(restored).GetCurrentCensus();
        Assert.That(restoredWitness.OwnerInstanceIdentity, Is.SameAs(restored));
        Assert.That(restoredWitness.Cardinality, Is.EqualTo(1));
        Assert.That(restoredWitness.Revision, Is.EqualTo(3));
    }
}
