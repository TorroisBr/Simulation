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

    [Test]
    public void SelectedNpcTravelOwnerPreservesRuntimeIdentitiesAndExactRevision()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners owners), Is.True);
        DailyV1NpcTravelOwner travel = owners.Travel;

        Assert.That(travel.NpcRuntimeId, Is.EqualTo("npc-runtime"));
        Assert.That(travel.IsTraveling, Is.False);
        Assert.That(travel.Revision, Is.Zero);
        Assert.That(travel.TryInstallState(
            "location-origin", "city-origin", "location-destination", "city-destination",
            2, 2, "route-1", true, "decision-1", "party-1", 23L), Is.True);
        Assert.That(travel.CurrentLocationRuntimeId, Is.EqualTo("location-origin"));
        Assert.That(travel.CurrentCityRuntimeId, Is.EqualTo("city-origin"));
        Assert.That(travel.DestinationLocationRuntimeId, Is.EqualTo("location-destination"));
        Assert.That(travel.DestinationCityRuntimeId, Is.EqualTo("city-destination"));
        Assert.That(travel.TravelDaysRemaining, Is.EqualTo(2));
        Assert.That(travel.TravelDaysTotal, Is.EqualTo(2));
        Assert.That(travel.TravelRouteRuntimeId, Is.EqualTo("route-1"));
        Assert.That(travel.TravelStartedToday, Is.True);
        Assert.That(travel.TravelOriginDecisionId, Is.EqualTo("decision-1"));
        Assert.That(travel.ActiveTravelPartyId, Is.EqualTo("party-1"));
        Assert.That(travel.IsTraveling, Is.True);
        Assert.That(travel.Revision, Is.EqualTo(23L));
        Assert.That(travel.CanAdvanceRevision(1), Is.True);
        Assert.That(travel.TryCommitState(
            "location-origin", "city-origin", "location-destination", "city-destination",
            1, 2, "route-1", true, "decision-1", "party-1", true), Is.True);
        Assert.That(travel.TravelDaysRemaining, Is.EqualTo(1));
        Assert.That(travel.Revision, Is.EqualTo(24L));

        Assert.That(travel.TryInstallState(
            "location-origin", "city-origin", "location-destination", "city-destination",
            2, 2, "route-1", true, "decision-1", "party-1", -1L), Is.False);
        Assert.That(travel.Revision, Is.EqualTo(24L));
        Assert.That(travel.TryInstallState(
            "location-origin", "city-origin", null, null,
            0, 0, null, false, null, null, long.MaxValue), Is.True);
        Assert.That(travel.CanAdvanceRevision(1), Is.False);
        Assert.That(travel.TryCommitState(
            "location-origin", "city-origin", "location-destination", "city-destination",
            1, 2, "route-1", true, "decision-1", "party-1", true), Is.False);
        Assert.That(travel.IsTraveling, Is.False);
    }
}
