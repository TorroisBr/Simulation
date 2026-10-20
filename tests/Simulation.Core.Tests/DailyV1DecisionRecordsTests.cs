using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1DecisionRecordsTests
{
    [Test]
    public void CoreDecisionStorePreservesAppendOrderAndActorParticipantTargetIndexes()
    {
        NpcDecisionStore store = new NpcDecisionStore();
        NpcDecisionRecord first = Create("decision/1", 1L, "npc/actor", "npc/target");
        NpcDecisionRecord second = Create("decision/2", 2L, "npc/actor", "npc/other");

        Assert.That(store.Record(first), Is.True);
        Assert.That(store.Record(second), Is.True);
        Assert.That(store.Decisions.Count, Is.EqualTo(2));
        Assert.That(store.Decisions[0], Is.SameAs(first));
        Assert.That(store.Decisions[1], Is.SameAs(second));
        Assert.That(store.GetDecisionsForActor("npc/actor"), Is.EqualTo(new[] { first, second }));
        Assert.That(store.GetDecisionsForParticipant("npc/target"), Is.EqualTo(new[] { first }));
        Assert.That(store.GetDecisionsTargeting("npc/target"), Is.EqualTo(new[] { first }));
        Assert.That(store.TryGetDecision("decision/2", out NpcDecisionRecord found), Is.True);
        Assert.That(found, Is.SameAs(second));
    }

    [Test]
    public void CoreDecisionStoreRejectsDuplicateIdentityWithoutChangingIndexes()
    {
        NpcDecisionStore store = new NpcDecisionStore();
        NpcDecisionRecord first = Create("decision/1", 1L, "npc/actor", "npc/target");
        NpcDecisionRecord duplicate = Create("decision/1", 2L, "npc/other", "npc/target");
        Assert.That(store.Record(first), Is.True);
        Assert.That(store.Record(duplicate), Is.False);
        Assert.That(store.Decisions.Count, Is.EqualTo(1));
        Assert.That(store.GetDecisionsForActor("npc/other"), Is.Empty);
        Assert.That(store.GetDecisionsTargeting("npc/target"), Is.EqualTo(new[] { first }));
    }

    [Test]
    public void CoreDecisionRecordOperationPreservesIdThenSequenceThenConstructionOrder()
    {
        RuntimeIdAllocator ids = new RuntimeIdAllocator();
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        NpcDecisionStore store = new NpcDecisionStore();

        Assert.Throws<ArgumentException>(() => DailyV1DecisionRecordOperation.Record(
            ids, sequence, store, 1L, "", NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous, "action/one", null, null, null, null, null));
        Assert.That(store.Decisions, Is.Empty);

        NpcDecisionRecord record = DailyV1DecisionRecordOperation.Record(
            ids, sequence, store, 1L, "npc/actor", NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous, "action/one", null, null, null, null, null);
        Assert.That(record.DecisionId, Is.EqualTo("decision-000002"));
        Assert.That(record.RecordSequence, Is.EqualTo(2L));
        Assert.That(store.Decisions, Is.EqualTo(new[] { record }));
        Assert.That(sequence.CaptureSnapshot().NextSequence, Is.EqualTo(3L));
    }

    [Test]
    public void SelectedActionRecordUsesExactCorePayloadAndCityLocationOwners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator setupIds = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(setupIds, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(setupIds,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        DailyV1CityRuntimeOwners targetCity = graph.Cities[0];
        DailyV1ActionDefinition action = definitions.Actions.Values.First();
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, action,
            null, targetCity.RuntimeId, null, 0, 0f, 1f, NpcTravelReason.None,
            0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);

        NpcDecisionRecord record = DailyV1DecisionRecordOperation.RecordSelectedAction(
            new RuntimeIdAllocator(), new SimulationRecordSequence(), new NpcDecisionStore(),
            1L, graph, actor, payload, NpcDecisionType.Action, NpcDecisionOrigin.Autonomous);
        Assert.That(record, Is.Not.Null);
        Assert.That(record.ActorRuntimeId, Is.EqualTo(actor.RuntimeId));
        Assert.That(record.ActionDefinitionId, Is.EqualTo(action.DefinitionId));
        Assert.That(record.TargetLocationRuntimeId, Is.EqualTo(targetCity.LocationRuntimeId));
        Assert.That(record.TargetRuntimeIds, Is.Empty);
    }

    [Test]
    public void SelectedActionDecisionIsRecordedBeforeExactSlotCommitAndOriginBinding()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator setupIds = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(setupIds, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(setupIds,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        DailyV1ActionDefinition action = definitions.Actions.Values.First();
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, action,
            null, null, null, 0, 0f, 1f, NpcTravelReason.None,
            0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        RuntimeIdAllocator ids = new RuntimeIdAllocator();
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        NpcDecisionStore store = new NpcDecisionStore();
        NpcDecisionRecord recordedBeforeAdmission = null;

        Assert.That(DailyV1DecisionRecordOperation.TryRecordAndCommitSelectedActionSlot(
            ids, sequence, store, 1L, graph, actor, payload, NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            candidate =>
            {
                Assert.That(candidate.OriginDecisionId, Is.EqualTo(store.Decisions[0].DecisionId));
                Assert.That(actor.ActionSlot.CurrentAction, Is.Null);
                return true;
            },
            () =>
            {
                Assert.That(store.Decisions.Count, Is.EqualTo(1));
                recordedBeforeAdmission = store.Decisions[0];
                Assert.That(actor.ActionSlot.Revision, Is.Zero);
                Assert.That(actor.ActionSlot.CurrentAction, Is.Null);
                return true;
            }, out NpcDecisionRecord decision), Is.True);

        Assert.That(decision, Is.SameAs(recordedBeforeAdmission));
        Assert.That(decision.DecisionId, Is.EqualTo("decision-000001"));
        Assert.That(decision.RecordSequence, Is.EqualTo(1L));
        Assert.That(payload.OriginDecisionId, Is.EqualTo(decision.DecisionId));
        Assert.That(actor.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(actor.ActionSlot.CurrentPayload, Is.SameAs(payload));
        Assert.That(actor.ActionSlot.Revision, Is.EqualTo(1L));

        DailyV1NpcRuntimeOwners actorChoiceActor = graph.Npcs[1];
        DailyV1NpcActionPayloadOwner actorChoicePayload = CreatePayload(definitions, action);
        int actorChoiceProjectionCalls = 0;
        Assert.That(DailyV1DecisionRecordOperation.TryRecordAndCommitSelectedActionSlot(
            ids, sequence, store, 1L, graph, actorChoiceActor, actorChoicePayload,
            NpcDecisionType.Action, NpcDecisionOrigin.ActorChoice,
            candidate =>
            {
                actorChoiceProjectionCalls++;
                Assert.That(candidate.OriginDecisionId, Is.EqualTo(store.Decisions[1].DecisionId));
                Assert.That(actorChoiceActor.ActionSlot.CurrentAction, Is.Null);
                return true;
            }, () => true, out NpcDecisionRecord actorChoiceDecision), Is.True);
        Assert.That(actorChoiceDecision.Origin, Is.EqualTo(NpcDecisionOrigin.ActorChoice));
        Assert.That(actorChoiceActor.ActionSlot.CurrentPayload, Is.SameAs(actorChoicePayload));
        Assert.That(actorChoiceActor.ActionSlot.Revision, Is.EqualTo(1L));
        Assert.That(actorChoiceProjectionCalls, Is.EqualTo(1));

        DailyV1NpcRuntimeOwners directiveActor = graph.Npcs[2];
        DailyV1NpcActionPayloadOwner directivePayload = CreatePayload(definitions, action);
        int directiveProjectionCalls = 0;
        Assert.That(DailyV1DecisionRecordOperation.TryRecordAndCommitSelectedActionSlot(
            ids, sequence, store, 1L, graph, directiveActor, directivePayload,
            NpcDecisionType.Action, NpcDecisionOrigin.ScheduledDirective,
            candidate =>
            {
                directiveProjectionCalls++;
                Assert.That(candidate.OriginDecisionId, Is.EqualTo(store.Decisions[2].DecisionId));
                Assert.That(directiveActor.ActionSlot.CurrentAction, Is.Null);
                return true;
            }, () => true, out NpcDecisionRecord directiveDecision), Is.True);
        Assert.That(directiveDecision.Origin, Is.EqualTo(NpcDecisionOrigin.ScheduledDirective));
        Assert.That(directiveActor.ActionSlot.CurrentPayload, Is.SameAs(directivePayload));
        Assert.That(directiveActor.ActionSlot.Revision, Is.EqualTo(1L));
        Assert.That(directiveProjectionCalls, Is.EqualTo(1));

        DailyV1NpcRuntimeOwners rejectedActor = graph.Npcs[3];
        DailyV1NpcActionPayloadOwner rejectedPayload = CreatePayload(definitions, action);
        Assert.That(DailyV1DecisionRecordOperation.TryRecordAndCommitSelectedActionSlot(
            ids, sequence, store, 1L, graph, rejectedActor, rejectedPayload,
            NpcDecisionType.Action, NpcDecisionOrigin.Autonomous,
            candidate =>
            {
                Assert.That(store.Decisions.Count, Is.EqualTo(4));
                Assert.That(candidate.OriginDecisionId, Is.EqualTo(store.Decisions[3].DecisionId));
                return true;
            },
            () =>
            {
                Assert.That(rejectedPayload.OriginDecisionId, Is.EqualTo(store.Decisions[3].DecisionId));
                Assert.That(rejectedActor.ActionSlot.Revision, Is.Zero);
                return false;
            }, out NpcDecisionRecord rejectedDecision), Is.False);
        Assert.That(rejectedDecision, Is.Not.Null,
            "Existing behavior keeps the record even when slot admission rejects.");
        Assert.That(rejectedActor.ActionSlot.CurrentAction, Is.Null);
        Assert.That(rejectedActor.ActionSlot.Revision, Is.Zero);
    }

    private static DailyV1NpcActionPayloadOwner CreatePayload(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActionDefinition action)
    {
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, action,
            null, null, null, 0, 0f, 1f, NpcTravelReason.None,
            0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        return payload;
    }

    private static NpcDecisionRecord Create(
        string decisionId,
        long sequence,
        string actorId,
        string targetId) => new NpcDecisionRecord(
            decisionId, sequence, sequence, actorId, NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous, "action/one",
            new[] { new NpcDecisionParticipant(targetId, NpcDecisionParticipantRole.Performer) },
            new[] { targetId }, "location/target", null);
}
