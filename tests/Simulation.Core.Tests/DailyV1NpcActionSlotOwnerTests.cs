using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1NpcActionSlotOwnerTests
{
    [Test]
    public void P12RestoreFactoryBuildsExactCoreNpcOwnerVectorAndRevisions()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npcDefinition = definitions.Npcs.Values.First();
        DailyV1ActionDefinition action = definitions.Actions.Values.First();
        DailyV1ItemDefinition item = definitions.Items.Values.First();
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, "target-npc", "target-city", item,
            3, 6.25f, 1.5f, NpcTravelReason.Trade, -2f,
            "decision/restore", "occurrence/restore", null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(DailyV1NpcMerchantTradePlanOwner.TryCreateInitial(
            definitions, out DailyV1NpcMerchantTradePlanOwner merchantPlan), Is.True);

        DailyV1InventoryOwner inventory = new DailyV1InventoryOwner(definitions);
        MoneyAccountRuntime money = new MoneyAccountRuntime(17.5f);
        DailyV1CommercialKnowledgeOwner commercial = new DailyV1CommercialKnowledgeOwner(definitions);
        SpatialKnowledgeRuntime spatial = new SpatialKnowledgeRuntime("npc/restored");
        DailyV1StatusDefinition[] statuses = definitions.Statuses.Values.Take(1).ToArray();
        DailyV1NpcRuntimeRestoreState state = new DailyV1NpcRuntimeRestoreState
        {
            RuntimeId = "npc/restored",
            Definitions = definitions,
            Definition = npcDefinition,
            Statuses = statuses,
            Inventory = inventory,
            MoneyAccount = money,
            CommercialKnowledge = commercial,
            MerchantTradePlan = merchantPlan,
            SpatialKnowledge = spatial,
            CurrentAction = action,
            CurrentActionPayload = payload,
            ActionRevision = 17L,
            LifeState = NpcLifeState.Alive,
            InjurySeverity = NpcInjurySeverity.Injured,
            LifeStateRevision = 11L,
            CrimeJusticeRevision = 13L,
            HiddenDaysRemaining = 2,
            CurrentLocationRuntimeId = "location/current",
            CurrentCityRuntimeId = "city/current",
            DestinationLocationRuntimeId = "location/destination",
            DestinationCityRuntimeId = "city/destination",
            TravelDaysRemaining = 3,
            TravelDaysTotal = 5,
            TravelRouteRuntimeId = "route/travel",
            TravelStartedToday = true,
            TravelOriginDecisionId = "decision/travel",
            ActiveTravelPartyId = "party/travel",
            TravelRevision = 19L,
            TravelPlanTargetLocationRuntimeId = "location/plan",
            TravelPlanTargetCityRuntimeId = "city/plan",
            TravelPlanReason = NpcTravelReason.CommercialScout,
            TravelPlanUtility = 0.75f,
            TravelPlanExpectedCost = 8.5f,
            TravelPlanOriginDecisionId = "decision/plan",
            TravelPlanRevision = 23L
        };

        Assert.That(DailyV1NpcRuntimeOwners.TryCreateRestoredP12DOwner(
            state, out DailyV1NpcRuntimeOwners owners), Is.True);
        Assert.That(owners.RuntimeId, Is.EqualTo("npc/restored"));
        Assert.That(owners.Definitions, Is.SameAs(definitions));
        Assert.That(owners.Definition, Is.SameAs(npcDefinition));
        Assert.That(owners.Inventory, Is.SameAs(inventory));
        Assert.That(owners.MoneyAccount, Is.SameAs(money));
        Assert.That(owners.CommercialKnowledge, Is.SameAs(commercial));
        Assert.That(owners.MerchantTradePlan, Is.SameAs(merchantPlan));
        Assert.That(owners.SpatialKnowledge, Is.SameAs(spatial));
        Assert.That(owners.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(owners.ActionSlot.CurrentPayload, Is.SameAs(payload));
        Assert.That(owners.ActionSlot.Revision, Is.EqualTo(17L));
        Assert.That(owners.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(owners.Lifecycle.InjurySeverity, Is.EqualTo(NpcInjurySeverity.Injured));
        Assert.That(owners.Lifecycle.LifeStateRevision, Is.EqualTo(11L));
        Assert.That(owners.CrimeJusticeRevision, Is.EqualTo(13L));
        Assert.That(owners.HiddenState.RemainingDays, Is.EqualTo(2));
        Assert.That(owners.Travel.CurrentLocationRuntimeId, Is.EqualTo("location/current"));
        Assert.That(owners.Travel.CurrentCityRuntimeId, Is.EqualTo("city/current"));
        Assert.That(owners.Travel.DestinationLocationRuntimeId, Is.EqualTo("location/destination"));
        Assert.That(owners.Travel.DestinationCityRuntimeId, Is.EqualTo("city/destination"));
        Assert.That(owners.Travel.TravelDaysRemaining, Is.EqualTo(3));
        Assert.That(owners.Travel.TravelDaysTotal, Is.EqualTo(5));
        Assert.That(owners.Travel.TravelRouteRuntimeId, Is.EqualTo("route/travel"));
        Assert.That(owners.Travel.TravelStartedToday, Is.True);
        Assert.That(owners.Travel.TravelOriginDecisionId, Is.EqualTo("decision/travel"));
        Assert.That(owners.Travel.ActiveTravelPartyId, Is.EqualTo("party/travel"));
        Assert.That(owners.Travel.Revision, Is.EqualTo(19L));
        Assert.That(owners.TravelPlan.TargetLocationRuntimeId, Is.EqualTo("location/plan"));
        Assert.That(owners.TravelPlan.TargetCityRuntimeId, Is.EqualTo("city/plan"));
        Assert.That(owners.TravelPlan.Reason, Is.EqualTo(NpcTravelReason.CommercialScout));
        Assert.That(owners.TravelPlan.Utility, Is.EqualTo(0.75f));
        Assert.That(owners.TravelPlan.ExpectedCost, Is.EqualTo(8.5f));
        Assert.That(owners.TravelPlan.OriginDecisionId, Is.EqualTo("decision/plan"));
        Assert.That(owners.TravelPlan.Revision, Is.EqualTo(23L));
    }

    [Test]
    public void P12RestoreFactoryRejectsInvalidOrForeignOwnerInputWithoutPublishing()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npcDefinition = definitions.Npcs.Values.First();
        DailyV1ActionDefinition foreignAction = foreign.Actions.Values.First();
        DailyV1ItemDefinition foreignItem = foreign.Items.Values.First();
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            foreign, foreignAction, null, null, foreignItem, 0, 0f, 1f,
            NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner foreignPayload), Is.True);
        Assert.That(DailyV1NpcMerchantTradePlanOwner.TryCreateInitial(
            definitions, out DailyV1NpcMerchantTradePlanOwner merchantPlan), Is.True);

        DailyV1NpcRuntimeRestoreState state = new DailyV1NpcRuntimeRestoreState
        {
            RuntimeId = "npc/restored",
            Definitions = definitions,
            Definition = npcDefinition,
            Statuses = Array.Empty<DailyV1StatusDefinition>(),
            Inventory = new DailyV1InventoryOwner(definitions),
            MoneyAccount = new MoneyAccountRuntime(0f),
            CommercialKnowledge = new DailyV1CommercialKnowledgeOwner(definitions),
            MerchantTradePlan = merchantPlan,
            SpatialKnowledge = new SpatialKnowledgeRuntime("npc/restored"),
            CurrentAction = foreignAction,
            CurrentActionPayload = foreignPayload,
            ActionRevision = 4L,
            LifeState = NpcLifeState.Alive,
            InjurySeverity = NpcInjurySeverity.None,
            TravelRevision = -1L,
            TravelPlanReason = NpcTravelReason.None
        };

        Assert.That(DailyV1NpcRuntimeOwners.TryCreateRestoredP12DOwner(state, out _), Is.False,
            "A same-ID foreign action graph must not be admitted to the restored selected owner set.");
        state.CurrentAction = null;
        state.CurrentActionPayload = null;
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateRestoredP12DOwner(
            state, out DailyV1NpcRuntimeOwners rejected), Is.False,
            "A negative travel revision must reject the private owner candidate.");
        Assert.That(rejected, Is.Null,
            "A failed restore never publishes a partially initialized owner graph.");
    }

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
    public void SelectedActionPayloadRetainsExactDefinitionAndEvidenceOwners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ActionDefinition action = definitions.Actions.Values.First();
        DailyV1ItemDefinition item = definitions.Items.Values.First();
        CommercialDecisionEvidence evidence = new CommercialDecisionEvidence(
            item.DefinitionId, "city-a", "city-a", "city-b", null,
            null, null, 2, 3f, 4f, 1f, 2f, 1f, 0.5f);

        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, "npc-a", "city-b", item,
            2, 4f, 1.25f, NpcTravelReason.CommercialScout, 3f,
            "decision-a", "occurrence-a", evidence, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(payload.Action, Is.SameAs(action));
        Assert.That(payload.TargetItem, Is.SameAs(item));
        Assert.That(payload.TargetNpcRuntimeId, Is.EqualTo("npc-a"));
        Assert.That(payload.TargetCityRuntimeId, Is.EqualTo("city-b"));
        Assert.That(payload.Amount, Is.EqualTo(2));
        Assert.That(payload.ExpectedUnitPrice, Is.EqualTo(4f));
        Assert.That(payload.SuccessChanceMultiplier, Is.EqualTo(1.25f));
        Assert.That(payload.TravelReason, Is.EqualTo(NpcTravelReason.CommercialScout));
        Assert.That(payload.ExpectedNetValue, Is.EqualTo(3f));
        Assert.That(payload.OriginDecisionId, Is.EqualTo("decision-a"));
        Assert.That(payload.StableOccurrenceKey, Is.EqualTo("occurrence-a"));
        Assert.That(payload.CommercialDecisionEvidence, Is.SameAs(evidence));

        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-a", definitions, definitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners owners), Is.True);
        Assert.That(owners.ActionSlot.TrySetCurrentAction(action, payload), Is.True);
        payload.SetSuccessChanceMultiplier(0.75f);
        payload.SetOriginDecisionId("decision-b");
        payload.SetStableOccurrenceKey("occurrence-b");
        Assert.That(payload.SuccessChanceMultiplier, Is.EqualTo(0.75f));
        Assert.That(payload.OriginDecisionId, Is.EqualTo("decision-b"));
        Assert.That(payload.StableOccurrenceKey, Is.EqualTo("occurrence-b"));
        Assert.That(owners.ActionSlot.CurrentPayload, Is.SameAs(payload));
        Assert.That(owners.ActionSlot.Revision, Is.Zero,
            "Payload staging stays separate from the existing committed action-slot revision callback.");
        owners.ActionSlot.NotifyCurrentActionMutationCommitted();
        Assert.That(owners.ActionSlot.Revision, Is.EqualTo(1L));

        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, foreign.Actions[action.DefinitionId], "npc-a", "city-b", item,
            2, 4f, 1f, NpcTravelReason.CommercialScout, 3f,
            null, null, null, null, out _), Is.False,
            "An action with a matching ID from a different graph is not admitted.");
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, "npc-a", "city-b", foreign.Items[item.DefinitionId],
            2, 4f, 1f, NpcTravelReason.CommercialScout, 3f,
            null, null, null, null, out _), Is.False,
            "An item with a matching ID from a different graph is not admitted.");
    }

    [Test]
    public void SelectedActionPersistenceSnapshotReadsTheExactCorePayload()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ActionDefinition action = definitions.Actions.Values.First();
        DailyV1ItemDefinition item = definitions.Items.Values.First();
        CommercialDecisionEvidence decisionEvidence = new CommercialDecisionEvidence(
            item.DefinitionId, "location/current", "location/origin", "location/destination", null,
            null, null, 5, 7f, 8f, 2f, 3f, 1.5f, 0.75f);
        CommercialScoutingEvidence scoutingEvidence = new CommercialScoutingEvidence(
            "location/target", "route/known", 2, 1, true, 3L, 5f, 2, 6f);
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners owners), Is.True);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, "target-npc", "target-city", item,
            3, 5.25f, 1.5f, NpcTravelReason.CommercialScout, -2.75f,
            "decision-origin", "occurrence-stable", decisionEvidence, scoutingEvidence,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(owners.ActionSlot.TrySetCurrentAction(action, payload), Is.True);

        Assert.That(owners.ActionSlot.TryCapturePayload(
            out DailyV1NpcActionPayloadSnapshot snapshot), Is.True);
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot.Owner, Is.SameAs(payload));
        Assert.That(snapshot.ActionDefinitionId, Is.EqualTo(action.DefinitionId));
        Assert.That(snapshot.TargetNpcRuntimeId, Is.EqualTo("target-npc"));
        Assert.That(snapshot.TargetCityRuntimeId, Is.EqualTo("target-city"));
        Assert.That(snapshot.TargetItemDefinitionId, Is.EqualTo(item.DefinitionId));
        Assert.That(snapshot.Amount, Is.EqualTo(3));
        Assert.That(snapshot.ExpectedUnitPrice, Is.EqualTo(5.25f));
        Assert.That(snapshot.SuccessChanceMultiplier, Is.EqualTo(1.5f));
        Assert.That(snapshot.TravelReason, Is.EqualTo(NpcTravelReason.CommercialScout));
        Assert.That(snapshot.ExpectedNetValue, Is.EqualTo(-2.75f));
        Assert.That(snapshot.OriginDecisionId, Is.EqualTo("decision-origin"));
        Assert.That(snapshot.StableOccurrenceKey, Is.EqualTo("occurrence-stable"));
        Assert.That(snapshot.CommercialDecisionEvidence, Is.SameAs(decisionEvidence));
        Assert.That(snapshot.CommercialScoutingEvidence, Is.SameAs(scoutingEvidence));

        payload.SetOriginDecisionId("decision-updated");
        Assert.That(owners.ActionSlot.TryCapturePayload(out DailyV1NpcActionPayloadSnapshot updated), Is.True);
        Assert.That(updated.OriginDecisionId, Is.EqualTo("decision-updated"),
            "A later persistence view must read the current canonical owner payload.");
        Assert.That(updated.Owner, Is.SameAs(payload));
    }

    [Test]
    public void SelectedActionPersistenceSnapshotRejectsIncompleteSlotAndPreservesEmptySlot()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, definitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners owners), Is.True);
        DailyV1NpcActionSlotOwner slot = owners.ActionSlot;

        Assert.That(slot.TryCapturePayload(out DailyV1NpcActionPayloadSnapshot empty), Is.True);
        Assert.That(empty, Is.Null);

        Assert.That(slot.TrySetCurrentAction(definitions.Actions.Values.First()), Is.True);
        Assert.That(slot.TryCapturePayload(out DailyV1NpcActionPayloadSnapshot incomplete), Is.False,
            "A selected slot with an action identity but no canonical payload is not persistable.");
        Assert.That(incomplete, Is.Null);
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
