using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1ActorTurnExecutionStageTests
{
    [Test]
    public void SelectedRosterPreservesNullHoleOrdinalAndCoreOwnsTurnPrecedence()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners first);
        DailyV1NpcRuntimeOwners second = graph.Npcs.First(owner => !ReferenceEquals(owner, first));
        PersonStore people = new PersonStore();
        PersonId secondPersonId = RegisterMaterializedPerson(people, second, "person-second");
        ActorChoiceStore choices = new ActorChoiceStore(people);
        Assert.That(choices.TryCapture("command-second", secondPersonId, "action.sell-goods",
            WorldCommandOrigin.LocalPlayer, WorldCommandAuthorityMode.Request, 4L,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode captureFailure),
            Is.True, captureFailure.ToString());

        List<DailyV1ActorTurnRosterEntry> roster = new List<DailyV1ActorTurnRosterEntry>
        {
            new DailyV1ActorTurnRosterEntry(0, null, first),
            new DailyV1ActorTurnRosterEntry(1, null, null),
            new DailyV1ActorTurnRosterEntry(2, secondPersonId, second)
        };
        RecordingBoundary boundary = new RecordingBoundary { ScheduledDirectiveRuntimeId = second.RuntimeId };

        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph, roster, people, choices, 5L, true, boundary), Is.True);

        Assert.That(boundary.Events, Is.EqualTo(new[]
        {
            "status:" + first.RuntimeId,
            "merchant:" + first.RuntimeId,
            "directive:" + first.RuntimeId,
            "choice:" + first.RuntimeId + ":0",
            "action:" + first.RuntimeId,
            "status:" + second.RuntimeId,
            "directive:" + second.RuntimeId
        }));
        Assert.That(choices.TryGet(input.InputId, out ActorChoiceInput deferred), Is.True);
        Assert.That(deferred.Status, Is.EqualTo(ActorChoiceInputStatus.Pending));
        Assert.That(deferred.Dispositions.Single().Kind, Is.EqualTo(ActorChoiceDispositionKind.Deferred));
        Assert.That(deferred.Dispositions.Single().DeferralReason,
            Is.EqualTo(ActorChoiceDeferralReason.ScheduledDirective));
        Assert.That(deferred.Dispositions.Single().ActorTurnRosterOrdinal, Is.EqualTo(2));
    }

    [Test]
    public void SpatialTransitDeferralRunsDirectiveBeforeStatusAndRetainsRosterOrdinal()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        PersonStore people = new PersonStore();
        PersonId personId = RegisterMaterializedPerson(people, actor, "person-transit");
        ActorChoiceStore choices = new ActorChoiceStore(people);
        Assert.That(choices.TryCapture("command-transit", personId, "action.sell-goods",
            WorldCommandOrigin.LocalPlayer, WorldCommandAuthorityMode.Request, 4L,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode captureFailure),
            Is.True, captureFailure.ToString());
        RecordingBoundary boundary = new RecordingBoundary
        {
            IsSpatialTransit = true,
            ScheduledDirectiveRuntimeId = actor.RuntimeId
        };
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster = new[]
        {
            new DailyV1ActorTurnRosterEntry(0, personId, actor)
        };

        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph, roster, people, choices, 5L, true, boundary), Is.True);

        Assert.That(boundary.Events, Is.EqualTo(new[] { "directive:" + actor.RuntimeId }));
        Assert.That(choices.TryGet(input.InputId, out ActorChoiceInput deferred), Is.True);
        Assert.That(deferred.Dispositions.Single().DeferralReason,
            Is.EqualTo(ActorChoiceDeferralReason.Traveling));
        Assert.That(deferred.Dispositions.Single().ActorTurnRosterOrdinal, Is.Zero);
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(out DailyV1NpcRuntimeOwners actor)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        actor = graph.Npcs.Single(owner => owner.Definition.DefinitionId == "JORGE");
        return graph;
    }

    private static PersonId RegisterMaterializedPerson(
        PersonStore people,
        DailyV1NpcRuntimeOwners actor,
        string personIdValue)
    {
        PersonId personId = new PersonId(personIdValue);
        Assert.That(people.TryRegister(new PersonRuntime(personId), out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(people.TryBindMaterializedNpc(personId, actor.RuntimeId,
            out PersonStoreFailure bindFailure), Is.True, bindFailure.ToString());
        return personId;
    }

    private sealed class RecordingBoundary : IDailyV1ActorTurnExecutionBoundary
    {
        internal readonly List<string> Events = new List<string>();
        internal string ScheduledDirectiveRuntimeId;
        internal bool IsSpatialTransit;

        public bool IsInSpatialTransit(DailyV1NpcRuntimeOwners actor, PersonId personId) => IsSpatialTransit;
        public bool IsOnActiveExpedition(DailyV1NpcRuntimeOwners actor) => false;
        public bool IsReservedToday(DailyV1NpcRuntimeOwners actor) => false;
        public void EvaluateStatus(DailyV1NpcRuntimeOwners actor) => Events.Add("status:" + actor.RuntimeId);
        public bool TryProcessScheduledDirective(DailyV1NpcRuntimeOwners actor)
        {
            Events.Add("directive:" + actor.RuntimeId);
            return string.Equals(ScheduledDirectiveRuntimeId, actor.RuntimeId);
        }
        public bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal)
        {
            Events.Add("choice:" + actor.RuntimeId + ":" + rosterOrdinal);
            return false;
        }
        public void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor) => Events.Add("merchant:" + actor.RuntimeId);
        public void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor) => Events.Add("action:" + actor.RuntimeId);
    }
}
