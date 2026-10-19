using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1NpcActionSlotOwnerTests
{
    [Test]
    public void SelectedNpcLifecycleOwnerOwnsLifeInjuryAndP12RevisionValues()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners owners), Is.True);
        DailyV1NpcLifecycleOwner lifecycle = owners.Lifecycle;

        Assert.That(lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(lifecycle.InjurySeverity, Is.EqualTo(NpcInjurySeverity.None));
        Assert.That(lifecycle.LifeStateRevision, Is.Zero);
        Assert.That(lifecycle.TrySetInjurySeverity(NpcInjurySeverity.Injured), Is.True);
        Assert.That(lifecycle.TrySetInjurySeverity(NpcInjurySeverity.Hurt), Is.True);
        Assert.That(lifecycle.InjurySeverity, Is.EqualTo(NpcInjurySeverity.Injured),
            "Existing injury semantics retain the greatest applied severity.");
        Assert.That(lifecycle.TrySetLifeState(NpcLifeState.Dead), Is.True);
        Assert.That(lifecycle.LifeStateRevision, Is.Zero,
            "State staging and mutation remain separate from committed P12 revision notification.");
        lifecycle.NotifyLifeStateMutationCommitted();
        Assert.That(lifecycle.LifeStateRevision, Is.EqualTo(1L));

        Assert.That(lifecycle.TryInstallState(NpcLifeState.Alive, NpcInjurySeverity.None, 19L), Is.True);
        Assert.That(lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(lifecycle.InjurySeverity, Is.EqualTo(NpcInjurySeverity.None));
        Assert.That(lifecycle.LifeStateRevision, Is.EqualTo(19L));
        Assert.That(lifecycle.TryInstallState((NpcLifeState)99, NpcInjurySeverity.None, 20L), Is.False);
        Assert.That(lifecycle.LifeStateRevision, Is.EqualTo(19L));
    }

    [Test]
    public void SelectedActionSlotOwnsCommittedRevisionAndExactHydrationValue()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners owners), Is.True);
        DailyV1NpcActionSlotOwner slot = owners.ActionSlot;
        DailyV1ActionDefinition action = definitions.Actions.Values.First();

        Assert.That(slot.Revision, Is.Zero);
        Assert.That(slot.TrySetCurrentAction(action), Is.True,
            "Staging the exact admitted definition does not itself report a gameplay commit.");
        Assert.That(slot.Revision, Is.Zero);
        Assert.That(slot.CanAdvanceRevision(1), Is.True);

        slot.NotifyCurrentActionMutationCommitted();
        Assert.That(slot.Revision, Is.EqualTo(1));
        Assert.That(slot.TrySetCurrentAction(null), Is.True);
        slot.NotifyCurrentActionMutationCommitted();
        Assert.That(slot.Revision, Is.EqualTo(2));

        Assert.That(slot.TryInstallRevision(17), Is.True,
            "Detached hydration restores the exact owner-local revision without replaying writes.");
        Assert.That(slot.Revision, Is.EqualTo(17));
        Assert.That(slot.TryInstallRevision(-1), Is.False);
        Assert.That(slot.Revision, Is.EqualTo(17));
    }

    [Test]
    public void SelectedActionSlotRejectsForeignDefinitionAndPreservesSaturatedRevision()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners owners), Is.True);
        DailyV1NpcActionSlotOwner slot = owners.ActionSlot;
        DailyV1ActionDefinition admitted = definitions.Actions.Values.First();
        DailyV1ActionDefinition foreignAction = foreign.Actions[admitted.DefinitionId];

        Assert.That(slot.TrySetCurrentAction(admitted), Is.True);
        slot.NotifyCurrentActionMutationCommitted();
        Assert.That(slot.TrySetCurrentAction(foreignAction), Is.False);
        Assert.That(slot.CurrentAction, Is.SameAs(admitted));
        Assert.That(slot.Revision, Is.EqualTo(1));

        Assert.That(slot.TryInstallRevision(long.MaxValue), Is.True);
        Assert.That(slot.CanAdvanceRevision(1), Is.False);
        slot.NotifyCurrentActionMutationCommitted();
        Assert.That(slot.Revision, Is.EqualTo(long.MaxValue));
    }
}
