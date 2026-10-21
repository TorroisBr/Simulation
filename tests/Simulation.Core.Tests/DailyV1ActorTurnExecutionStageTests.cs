using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1ActorTurnExecutionStageTests
{
    [Test]
    public void SelectedRosterPreservesNullHoleOrdinalAndCoreOwnsTurnPrecedence()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners first, out DailyV1NpcRuntimeOwners second);
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
        ScheduledDirectiveStore directives = CreatePreparedDirectiveStore(second, 5L);
        RecordingBoundary boundary = new RecordingBoundary();

        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph, roster, people, choices, directives, 5L, true, boundary), Is.True);

        Assert.That(boundary.Events, Is.EqualTo(new[]
        {
            "status:" + first.RuntimeId,
            "merchant:" + first.RuntimeId,
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
        ScheduledDirectiveStore directives = CreatePreparedDirectiveStore(actor, 5L);
        RecordingBoundary boundary = new RecordingBoundary { IsSpatialTransit = true };
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster = new[]
        {
            new DailyV1ActorTurnRosterEntry(0, personId, actor)
        };

        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph, roster, people, choices, directives, 5L, true, boundary), Is.True);

        Assert.That(boundary.Events, Is.EqualTo(new[] { "directive:" + actor.RuntimeId }));
        Assert.That(choices.TryGet(input.InputId, out ActorChoiceInput deferred), Is.True);
        Assert.That(deferred.Dispositions.Single().DeferralReason,
            Is.EqualTo(ActorChoiceDeferralReason.Traveling));
        Assert.That(deferred.Dispositions.Single().ActorTurnRosterOrdinal, Is.Zero);
    }

    [Test]
    public void IncompleteSelectedRosterIsRejectedBeforeAnyActorTurnCallback()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners first, out _);
        PersonStore people = new PersonStore();
        ActorChoiceStore choices = new ActorChoiceStore(people);
        RecordingBoundary boundary = new RecordingBoundary();
        IReadOnlyList<DailyV1ActorTurnRosterEntry> incompleteRoster = new[]
        {
            new DailyV1ActorTurnRosterEntry(0, null, first)
        };

        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph, incompleteRoster, people, choices, null, 5L, true, boundary), Is.False);
        Assert.That(boundary.Events, Is.Empty);
    }

    [Test]
    public void CoreSelectedActionStageOwnsDecisionSlotDispatchAndStatusOrder()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary();
        boundary.BindGraph(graph);
        long actionSlotRevision = actor.ActionSlot.Revision;

        Assert.That(DailyV1SelectedActorActionExecutionStage.TryExecuteAutonomousAction(
            graph, actor, new PersonStore(), boundary.RandomSource, 12L, boundary), Is.True);

        Assert.That(actor.ActionSlot.CurrentAction, Is.Not.Null);
        Assert.That(actor.ActionSlot.Revision, Is.EqualTo(actionSlotRevision + 1L),
            "One selected action commits exactly one action-slot revision.");
        Assert.That(boundary.Decisions.Decisions, Has.Count.EqualTo(1));
        Assert.That(actor.ActionSlot.CurrentPayload.OriginDecisionId,
            Is.EqualTo(boundary.Decisions.Decisions[0].DecisionId));
        Assert.That(boundary.Events, Is.EqualTo(new[]
        {
            "record-install",
            "chosen-target",
            "complete-dispatch",
            "status-operation"
        }));
        Assert.That(boundary.Random.Calls, Is.EqualTo(1));
        Assert.That(boundary.Random.LastStreamKey,
            Is.EqualTo("npc-decision|" + actor.RuntimeId + "|12"));
    }

    [Test]
    public void ProjectionRejectionLeavesDecisionOwnersAndActionSlotUnchanged()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary
        {
            CanProjectSelectedAction = false
        };
        boundary.BindGraph(graph);
        long nextDecisionId = boundary.NextDecisionIdSequence;
        long nextRecordSequence = boundary.NextRecordSequence;
        long actionSlotRevision = actor.ActionSlot.Revision;

        Assert.That(DailyV1SelectedActorActionExecutionStage.TryExecuteAutonomousAction(
            graph, actor, new PersonStore(), boundary.RandomSource, 12L, boundary), Is.True);

        Assert.That(boundary.Decisions.Decisions, Is.Empty);
        Assert.That(boundary.NextDecisionIdSequence, Is.EqualTo(nextDecisionId));
        Assert.That(boundary.NextRecordSequence, Is.EqualTo(nextRecordSequence));
        Assert.That(actor.ActionSlot.CurrentAction, Is.Null);
        Assert.That(actor.ActionSlot.CurrentPayload, Is.Null);
        Assert.That(actor.ActionSlot.Revision, Is.EqualTo(actionSlotRevision));
        Assert.That(boundary.Random.Calls, Is.Zero,
            "Adapter projection eligibility is settled before the Core weighted choice draw.");
    }

    [Test]
    public void RejectedScheduledActionReleasesProjectionForNextActor()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator ids = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(ids, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(ids,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph completeGraph), Is.True);
        DailyV1NpcRuntimeOwners first = completeGraph.Npcs.Single(
            owner => owner.Definition.DefinitionId == "JORGE");
        DailyV1NpcRuntimeOwners second = completeGraph.Npcs.First(
            owner => !ReferenceEquals(owner, first));
        DailyV1CityRuntimeOwners city = completeGraph.Cities.Single(
            owner => owner.RuntimeId == first.Travel.CurrentCityRuntimeId);
        DailyV1JusticeRecordsOwner justice = new DailyV1JusticeRecordsOwner();
        DailyV1StatusDefinition arrested = definitions.Statuses[
            definitions.Configuration.arrestedStatus];
        Assert.That(first.Status.TryAdd(arrested), Is.True);
        DailyV1JusticeWantedRecordOwner warrant = new DailyV1JusticeWantedRecordOwner(
            first.RuntimeId, city.RuntimeId, 12f, 4);
        Assert.That(justice.TryAddWantedRecord(warrant), Is.True);
        Assert.That(justice.NotifyMutationCommitted(), Is.True);
        DailyV1JusticeSentenceOwner sentence = new DailyV1JusticeSentenceOwner(
            first.RuntimeId, city.RuntimeId, warrant, 4, 0, wasArrestedToday: false);
        Assert.That(justice.TryAddSentence(sentence), Is.True);
        Assert.That(justice.NotifyMutationCommitted(), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            completeGraph.Cities, new[] { first, second }, completeGraph.SpatialNetwork,
            justice, out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        DailyV1ActionDefinition escape = definitions.Actions.Values.Single(
            value => value.Values.actionType?.Value
                == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(new SimulationTime(12L));
        ScheduledDirective directive = new ScheduledDirective(
            "directive-rejected-projection-cleanup", 12L,
            ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison,
            first.RuntimeId, escape.DefinitionId,
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        Assert.That(directives.Add(directive), Is.True);
        directives.PrepareDay(12L, runtimeId => runtimeId == first.RuntimeId);

        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary();
        boundary.BindGraph(graph);
        boundary.RejectNextActionSlotAdmission = true;
        DailyV1SelectedActorActionExecutionStage.ProcessScheduledRequestAction(
            graph, first, directive, boundary.RandomSource, 12L, boundary);

        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        Assert.That(first.ActionSlot.CurrentAction, Is.Null,
            "A rejected slot admission must not publish the requested action.");
        Assert.That(DailyV1SelectedActorActionExecutionStage.TryExecuteAutonomousAction(
            graph, second, new PersonStore(), boundary.RandomSource, 12L, boundary), Is.True,
            "A rejected projection must not remain cached on the roster-scoped host boundary.");
        Assert.That(second.ActionSlot.CurrentAction, Is.Not.Null);
        Assert.That(boundary.Events, Does.Contain("record-install"));
    }

    [Test]
    public void OrderedActorTurnConsumesForcedEscapeInCoreWithoutDecisionOrRandomDraw()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator ids = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(ids, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(ids,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph authoredGraph), Is.True);
        DailyV1NpcRuntimeOwners actor = authoredGraph.Npcs.Single(
            owner => owner.Definition.DefinitionId == "JORGE");
        DailyV1CityRuntimeOwners city = authoredGraph.Cities.Single(
            owner => owner.Definition.DefinitionId == "CAMPO_VERDE");
        DailyV1JusticeRecordsOwner justice = new DailyV1JusticeRecordsOwner();
        DailyV1StatusDefinition arrested = definitions.Statuses[definitions.Configuration.arrestedStatus];
        Assert.That(actor.Status.TryAdd(arrested), Is.True);
        DailyV1JusticeWantedRecordOwner warrant = new DailyV1JusticeWantedRecordOwner(
            actor.RuntimeId, city.RuntimeId, 12f, 4);
        Assert.That(justice.TryAddWantedRecord(warrant), Is.True);
        Assert.That(justice.NotifyMutationCommitted(), Is.True);
        DailyV1JusticeSentenceOwner sentence = new DailyV1JusticeSentenceOwner(
            actor.RuntimeId, city.RuntimeId, warrant, 4, 0, wasArrestedToday: true);
        Assert.That(justice.TryAddSentence(sentence), Is.True);
        Assert.That(justice.NotifyMutationCommitted(), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            authoredGraph.Cities, new[] { actor }, authoredGraph.SpatialNetwork,
            justice, out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        PersonStore people = new PersonStore();
        PersonId personId = RegisterMaterializedPerson(people, actor, "person-forced-escape");
        ActorChoiceStore choices = new ActorChoiceStore(people);
        DailyV1ActionDefinition escape = definitions.Actions.Values.Single(
            value => value.Values.actionType?.Value
                == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(new SimulationTime(12L));
        ScheduledDirective directive = new ScheduledDirective(
            "directive-forced-escape", 12L, ScheduledDirectiveMode.ForceOutcome,
            ScheduledDirectiveOperation.EscapePrison, actor.RuntimeId,
            escape.DefinitionId, DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        Assert.That(directives.Add(directive), Is.True);
        directives.PrepareDay(12L, runtimeId => runtimeId == actor.RuntimeId);

        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary();
        boundary.BindGraph(graph);
        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph, new[] { new DailyV1ActorTurnRosterEntry(0, personId, actor) },
            people, choices, directives, 12L, merchantTradeEnabled: false, boundary), Is.True);

        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Succeeded), directive.ResultReason);
        Assert.That(justice.ContainsSentence(sentence), Is.False);
        Assert.That(boundary.Decisions.Decisions, Is.Empty);
        Assert.That(boundary.Random.Calls, Is.Zero);
        Assert.That(boundary.Events.Count(value => value == "p12-begin"), Is.EqualTo(1));
        Assert.That(boundary.Events.Count(value => value == "p12-end"), Is.EqualTo(1));
        Assert.That(boundary.Events, Does.Contain("escape-published"));
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(out DailyV1NpcRuntimeOwners actor)
    {
        return CreateGraph(out actor, out _, false);
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        out DailyV1NpcRuntimeOwners first,
        out DailyV1NpcRuntimeOwners second) => CreateGraph(out first, out second, true);

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        out DailyV1NpcRuntimeOwners first,
        out DailyV1NpcRuntimeOwners second,
        bool includeSecond)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph completeGraph), Is.True);
        DailyV1NpcRuntimeOwners firstOwner =
            completeGraph.Npcs.Single(owner => owner.Definition.DefinitionId == "JORGE");
        first = firstOwner;
        second = includeSecond
            ? completeGraph.Npcs.First(owner => !ReferenceEquals(owner, firstOwner))
            : null;
        DailyV1NpcRuntimeOwners[] selectedOwners = includeSecond
            ? new[] { first, second }
            : new[] { first };
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            completeGraph.Cities, selectedOwners, completeGraph.SpatialNetwork,
            out DailyV1SelectedRuntimeOwnerGraph selectedGraph), Is.True);
        return selectedGraph;
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

    private static ScheduledDirectiveStore CreatePreparedDirectiveStore(
        DailyV1NpcRuntimeOwners actor,
        long absoluteDay)
    {
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(new SimulationTime(absoluteDay));
        ScheduledDirective directive = new ScheduledDirective(
            "directive-" + actor.RuntimeId,
            absoluteDay,
            ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison,
            actor.RuntimeId,
            "action/escape-prison",
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        Assert.That(store.Add(directive), Is.True);
        store.PrepareDay(absoluteDay, runtimeId => runtimeId == actor.RuntimeId);
        return store;
    }

    private sealed class RecordingBoundary : IDailyV1ActorTurnExecutionBoundary
    {
        internal readonly List<string> Events = new List<string>();
        internal bool IsSpatialTransit;

        public bool IsInSpatialTransit(DailyV1NpcRuntimeOwners actor, PersonId personId) => IsSpatialTransit;
        public bool IsOnActiveExpedition(DailyV1NpcRuntimeOwners actor) => false;
        public bool IsReservedToday(DailyV1NpcRuntimeOwners actor) => false;
        public void EvaluateStatus(DailyV1NpcRuntimeOwners actor) => Events.Add("status:" + actor.RuntimeId);
        public void ProcessScheduledDirective(DailyV1NpcRuntimeOwners actor, ScheduledDirective directive) =>
            Events.Add("directive:" + actor.RuntimeId);
        public bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal)
        {
            Events.Add("choice:" + actor.RuntimeId + ":" + rosterOrdinal);
            return false;
        }
        public void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor) => Events.Add("merchant:" + actor.RuntimeId);
        public void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor) => Events.Add("action:" + actor.RuntimeId);
    }

    private sealed class CoreSelectedActionBoundary :
        IDailyV1ActorTurnExecutionBoundary,
        IDailyV1SelectedActorActionExecutionBoundary,
        IDailyV1SelectedNpcActionDecisionBoundary,
        IDailyV1SelectedActionDispatchBoundary,
        DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary,
        DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary
    {
        internal readonly List<string> Events = new List<string>();
        internal readonly NpcDecisionStore Decisions = new NpcDecisionStore();
        internal readonly FixedRandom Random = new FixedRandom(0f);
        private readonly RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        private readonly SimulationRecordSequence sequence = new SimulationRecordSequence();
        private DailyV1NpcRuntimeOwners preparedProjectionActor;
        private DailyV1NpcActionPayloadOwner preparedProjectionPayload;
        internal bool RejectNextActionSlotAdmission;
        internal bool CanProjectSelectedAction = true;
        internal long NextDecisionIdSequence => allocator.CaptureSnapshot().Counters
            .Single(counter => counter.FamilyId == "decision").NextSequence;
        internal long NextRecordSequence => sequence.CaptureSnapshot().NextSequence;

        public IAuthoritativeRandomSource RandomSource => Random;
        public bool UsesSelectedDailyV1ActionExecution(DailyV1NpcRuntimeOwners actor) => true;
        public void LogSkippedDirective(ScheduledDirective directive, string reason) =>
            Events.Add("skipped:" + reason);

        public bool TryCreateDecisionBoundary(
            DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedNpcActionDecisionBoundary decisionBoundary)
        {
            decisionBoundary = this;
            return true;
        }

        public bool TryGetConfiguredActionIds(
            DailyV1NpcRuntimeOwners actor,
            out IReadOnlyList<string> actionIds)
        {
            actionIds = actor.Definitions.Configuration.actions;
            return true;
        }

        public bool TryGetDecisionRecordOwners(
            DailyV1NpcRuntimeOwners actor,
            out RuntimeIdAllocator idAllocator,
            out SimulationRecordSequence recordSequence,
            out NpcDecisionStore decisionStore)
        {
            idAllocator = allocator;
            recordSequence = sequence;
            decisionStore = Decisions;
            return true;
        }

        public bool TryPrepareSelectedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate selected,
            IDailyV1SelectedNpcActionDecisionBoundary decisionBoundary,
            DailyV1NpcActionPayloadOwner payload)
        {
            if (preparedProjectionPayload != null
                || !ReferenceEquals(selected?.PreparedPayload, payload)
                || !decisionBoundary.TryProjectSelectedCandidate(actor, selected))
                return false;
            preparedProjectionActor = actor;
            preparedProjectionPayload = payload;
            return true;
        }

        public bool CanCommitSelectedActionSlot(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            if (!ReferenceEquals(preparedProjectionActor, actor)
                || !ReferenceEquals(preparedProjectionPayload, payload))
                return false;
            if (!RejectNextActionSlotAdmission) return true;
            RejectNextActionSlotAdmission = false;
            return false;
        }

        public bool TryPublishSelectedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            if (!ReferenceEquals(preparedProjectionActor, actor)
                || !ReferenceEquals(preparedProjectionPayload, payload)
                || !ReferenceEquals(actor?.ActionSlot?.CurrentPayload, payload)) return false;
            ClearPreparedProjection();
            Events.Add("record-install");
            return true;
        }

        public void CancelPreparedSelectedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            if (ReferenceEquals(preparedProjectionActor, actor)
                && ReferenceEquals(preparedProjectionPayload, payload))
                ClearPreparedProjection();
        }

        private void ClearPreparedProjection()
        {
            preparedProjectionActor = null;
            preparedProjectionPayload = null;
        }

        public void LogSelectedActionDecisionFailure(
            DailyV1NpcRuntimeOwners actor,
            string message) => Events.Add("decision-failure:" + message);

        public bool TryProjectSelectedCandidate(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate) =>
            CurrentGraph?.ContainsExactNpcOwner(actor) == true
            && CanProjectSelectedAction
            && candidate?.Action != null;

        public bool HasRequestedActionProvider(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action) => false;

        public bool TryRecordAndInstallSelectedAction(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate selected,
            IDailyV1SelectedNpcActionDecisionBoundary decisionBoundary,
            NpcDecisionOrigin origin)
        {
            DailyV1NpcActionPayloadOwner payload = selected.PreparedPayload;
            if (payload == null
                && !DailyV1NpcActionPayloadOwner.TryCreate(
                    actor.Definitions, selected.Action, null, null, null, 0,
                    0f, 1f, NpcTravelReason.None, 0f, null, null, null, null,
                    out payload))
                return false;

            bool committed = DailyV1DecisionRecordOperation.TryRecordAndCommitSelectedActionSlot(
                allocator, sequence, Decisions, 12L, CurrentGraph, actor, payload,
                NpcDecisionType.Action, origin,
                candidate => ReferenceEquals(candidate, payload),
                () => true, out _);
            if (!committed) return false;
            Events.Add("record-install");
            return true;
        }

        private DailyV1SelectedRuntimeOwnerGraph CurrentGraph { get; set; }

        public void ClearCurrentAction(DailyV1NpcRuntimeOwners actor)
        {
            if (actor?.ActionSlot?.CurrentAction == null
                && actor?.ActionSlot?.CurrentPayload == null)
                return;
            CurrentGraph.TryCommitCurrentActionSlot(actor, null, null, () => true);
        }

        public bool IsActorAtCurrentCityLocation(DailyV1NpcRuntimeOwners actor) => true;
        public void LogChosenActionTarget(DailyV1NpcRuntimeOwners actor) => Events.Add("chosen-target");
        public bool HasExactCurrentActionProjection(DailyV1NpcRuntimeOwners actor) => true;

        public bool TryGetDispatchBoundary(
            DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedActionDispatchBoundary dispatchBoundary)
        {
            dispatchBoundary = this;
            return true;
        }

        public bool TryCompleteDispatch(
            DailyV1NpcRuntimeOwners actor,
            DailyV1SelectedActionDispatchResult dispatch,
            out bool actionSucceeded,
            out NpcActionResultType? resultType,
            out string resultMessage)
        {
            Events.Add("complete-dispatch");
            actionSucceeded = dispatch.Succeeded;
            resultType = dispatch.Succeeded
                ? NpcActionResultType.Success
                : NpcActionResultType.Failure;
            resultMessage = null;
            return true;
        }

        public bool TryGetActionStatusBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcRuntimeOwners target,
            bool reuseExistingOperation,
            out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary boundary)
        {
            boundary = this;
            return true;
        }

        public bool TryGetForcedEscapeBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        {
            boundary = this;
            return ReferenceEquals(CurrentGraph.Definitions, actor.Definitions)
                && CurrentGraph.ContainsExactNpcOwner(actor)
                && action.Values.actionType?.Value
                    == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue;
        }

        public bool TryInstallForcedOutcomeAction(DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action)
        {
            if (!DailyV1NpcActionPayloadOwner.TryCreate(
                    actor.Definitions, action, null, null, null, 0, 0f, 1f,
                    NpcTravelReason.None, 0f, null, null, null, null,
                    out DailyV1NpcActionPayloadOwner payload)) return false;
            return CurrentGraph.TryCommitCurrentActionSlot(actor, action, payload, () => true);
        }

        public bool IsInSpatialTransit(DailyV1NpcRuntimeOwners actor, PersonId personId) => false;
        public bool IsOnActiveExpedition(DailyV1NpcRuntimeOwners actor) => false;
        public bool IsReservedToday(DailyV1NpcRuntimeOwners actor) => false;
        public void EvaluateStatus(DailyV1NpcRuntimeOwners actor) => Events.Add("status");
        public void ProcessScheduledDirective(DailyV1NpcRuntimeOwners actor, ScheduledDirective directive) =>
            Events.Add("legacy-directive");
        public bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal) => false;
        public void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor) { }
        public void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor) => Events.Add("legacy-action");

        public DailyV1JusticeRecordsOwner JusticeRecordsOwner => CurrentGraph.JusticeRecordsOwner;
        public bool ExecuteWithinOperation(System.Func<bool> operation)
        {
            Events.Add("p12-begin");
            try { return operation?.Invoke() == true; }
            finally { Events.Add("p12-end"); }
        }
        public bool CanProjectStatusChanges(DailyV1NpcRuntimeOwners owner,
            IReadOnlyList<DailyV1StatusDefinition> statusToAdd) => owner?.Status != null;
        public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners owner) { }
        public void NotifyStatusMutationCommitted(DailyV1NpcRuntimeOwners owner,
            DailyV1StatusDefinition status, bool added) { }
        public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners owner) { }
        public bool ApplyHideDuration(DailyV1NpcRuntimeOwners actor, int followingFullDays) => false;
        public bool ClearHidden(DailyV1NpcRuntimeOwners actor) => actor.HiddenState.TrySetRemainingDays(0);
        public bool ApplyStatusChange(DailyV1NpcRuntimeOwners actor, DailyV1StatusDefinition status, bool add)
        {
            if (actor?.Status == null || status == null) return false;
            if (add) return actor.Status.Contains(status) || actor.Status.TryAdd(status);
            return !actor.Status.Contains(status) || actor.Status.TryRemove(status);
        }
        public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
            CurrentGraph.JusticeRecordsOwner.ContainsSentence(sentence);
        public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence) { }
        public void PublishHidden(DailyV1NpcRuntimeOwners actor) { }
        public void PublishFailedEscape(DailyV1NpcRuntimeOwners actor, int sentencePenalty) { }
        public void PublishEscaped(DailyV1NpcRuntimeOwners actor, DailyV1CityRuntimeOwners eventCity,
            bool hadActiveSentence, float bountyAfterPenalty, string originDecisionId) =>
            Events.Add("escape-published");

        public bool HasTravelActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => false;
        public TravelPartyStore GetTravelPartyStore(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => null;
        public bool TryProjectTravelAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool HasMerchantActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => false;
        public bool HasEnabledCrimeActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => false;
        public bool TryProjectStealAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryProjectFleeCityAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryProjectBuyAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryProjectSellAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryPrepareAutonomousProviderAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate, out float utility)
        {
            utility = 0f;
            return false;
        }

        public bool TryGetSoloTravelBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action, out DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary boundary)
        {
            boundary = null;
            return false;
        }
        public bool TryGetMarketBoundaries(DailyV1NpcRuntimeOwners actor, out IDailyV1SelectedOpenMarketSaleBoundary saleBoundary, out IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary)
        {
            saleBoundary = null;
            purchaseBoundary = null;
            return false;
        }
        public bool TryGetNpcTradeBoundary(DailyV1NpcRuntimeOwners seller, DailyV1NpcRuntimeOwners buyer, out IDailyV1SelectedNpcTradeBoundary boundary)
        {
            boundary = null;
            return false;
        }
        public bool TryGetFleeCityActionBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action, out DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary boundary)
        {
            boundary = null;
            return false;
        }
        public bool TryGetGuardArrestActionBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action, out DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary boundary)
        {
            boundary = null;
            return false;
        }
        public bool TryGetStealActionBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action, out DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary boundary)
        {
            boundary = null;
            return false;
        }
        public bool TryGetCrimeActionBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action, out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        {
            boundary = null;
            return false;
        }
        public bool CanAdmitMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) => false;
        public void PublishMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) { }
        public bool CanAdmitTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) => false;
        public void PublishTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) { }
        public void CompleteCrossCityBuy(DailyV1NpcRuntimeOwners actor, DailyV1SelectedCrossCityBuyResult result) { }

        public DailyV1SelectedActionStatusPlanResult ExecuteWithinOperation(DailyV1NpcRuntimeOwners actor, DailyV1NpcRuntimeOwners target, System.Func<DailyV1SelectedActionStatusPlanResult> operation)
        {
            Events.Add("status-operation");
            return operation();
        }
        public bool ApplyActorStatusChanges(DailyV1NpcRuntimeOwners owner, IReadOnlyList<DailyV1StatusDefinition> statusToRemove, IReadOnlyList<DailyV1StatusDefinition> statusToAdd) => owner.Status.TryApplyOrderedChanges(statusToRemove, statusToAdd, () => { }, () => { });
        public bool ApplyTargetStatusChanges(DailyV1NpcRuntimeOwners owner, IReadOnlyList<DailyV1StatusDefinition> statusToRemove, IReadOnlyList<DailyV1StatusDefinition> statusToAdd) => owner.Status.TryApplyOrderedChanges(statusToRemove, statusToAdd, () => { }, () => { });

        internal void BindGraph(DailyV1SelectedRuntimeOwnerGraph graph) => CurrentGraph = graph;
    }

    private sealed class FixedRandom : IAuthoritativeRandomSource
    {
        private readonly float value;
        internal int Calls { get; private set; }
        internal string LastStreamKey { get; private set; }
        internal FixedRandom(float value) => this.value = value;
        public float NextUnit(string streamKey, long drawIndex = 0L)
        {
            Calls++;
            LastStreamKey = streamKey;
            return value;
        }
        public DeterministicRandomStream CreateStream(string streamKey) => null;
    }
}
