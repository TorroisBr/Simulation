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
            "merchant:" + first.RuntimeId,
            "choice:" + first.RuntimeId + ":0",
            "action:" + first.RuntimeId,
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
    public void CoreBuildsExactActorRosterFromHostIdentityProjection()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners first, out DailyV1NpcRuntimeOwners second);
        PersonId firstPerson = new PersonId("person-roster-first");
        PersonId dynamicPerson = new PersonId("person-roster-dynamic");
        PersonId secondPerson = new PersonId("person-roster-second");
        IReadOnlyList<DailyV1ActorTurnRosterProjection> projection = new[]
        {
            new DailyV1ActorTurnRosterProjection(
                first.RuntimeId, firstPerson),
            null,
            new DailyV1ActorTurnRosterProjection(
                "npc/unbound-dynamic", dynamicPerson),
            new DailyV1ActorTurnRosterProjection(
                second.RuntimeId, secondPerson)
        };

        Assert.That(DailyV1ActorTurnExecutionStage.TryBuildRoster(
            graph, projection, out IReadOnlyList<DailyV1ActorTurnRosterEntry> roster), Is.True);
        Assert.That(roster, Has.Count.EqualTo(4));
        Assert.That(roster[0].RosterOrdinal, Is.Zero);
        Assert.That(roster[0].PersonId, Is.SameAs(firstPerson));
        Assert.That(roster[0].ActorOwner, Is.SameAs(first));
        Assert.That(roster[1].RosterOrdinal, Is.EqualTo(1));
        Assert.That(roster[1].PersonId, Is.Null);
        Assert.That(roster[1].ActorOwner, Is.Null);
        Assert.That(roster[2].RosterOrdinal, Is.EqualTo(2));
        Assert.That(roster[2].PersonId, Is.SameAs(dynamicPerson));
        Assert.That(roster[2].ActorOwner, Is.Null,
            "A dynamic/unselected actor remains a host identity row, not a selected Core owner.");
        Assert.That(roster[3].RosterOrdinal, Is.EqualTo(3));
        Assert.That(roster[3].PersonId, Is.SameAs(secondPerson));
        Assert.That(roster[3].ActorOwner, Is.SameAs(second),
            "Core joins selected identity projections to the exact owner instances in its graph.");

        Assert.That(DailyV1ActorTurnExecutionStage.TryBuildRoster(
            graph,
            new[]
            {
                new DailyV1ActorTurnRosterProjection(
                    first.RuntimeId, firstPerson),
                new DailyV1ActorTurnRosterProjection(
                    first.RuntimeId, secondPerson)
            },
            out _), Is.False, "Core rejects a duplicated selected RuntimeId before actor execution.");
    }

    [Test]
    public void CoreBoundaryOwnsOrderedRosterAcrossAppendAndRemoval()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners first, out DailyV1NpcRuntimeOwners second);
        PersonId firstPerson = new PersonId("person-core-roster-first");
        PersonId unselectedPerson = new PersonId("person-core-roster-unselected");
        PersonId secondPerson = new PersonId("person-core-roster-second");
        PersonId appendedPerson = new PersonId("person-core-roster-appended");
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        Assert.That(state.TryBindSelectedRuntimeOwnerGraph(graph), Is.True);

        IReadOnlyList<DailyV1ActorTurnRosterProjection> initial = new[]
        {
            new DailyV1ActorTurnRosterProjection(first.RuntimeId, firstPerson),
            new DailyV1ActorTurnRosterProjection("npc/unbound-middle", unselectedPerson),
            null,
            new DailyV1ActorTurnRosterProjection(second.RuntimeId, secondPerson)
        };
        Assert.That(state.TryBindSelectedRuntimeActorRoster(initial), Is.True);
        IReadOnlyList<DailyV1ActorTurnRosterProjection> initialSnapshot =
            state.SelectedRuntimeActorRoster;

        Assert.That(state.TryCreateSelectedRuntimeActorRosterWithAppendedMember(
            new DailyV1ActorTurnRosterProjection("npc/unbound-appended", appendedPerson),
            graph,
            out IReadOnlyList<DailyV1ActorTurnRosterProjection> afterAppend), Is.True);
        Assert.That(state.TryReplaceSelectedRuntimeMembership(
            graph, graph, initialSnapshot, afterAppend), Is.True);
        Assert.That(initialSnapshot, Has.Count.EqualTo(4),
            "A previously published Core roster snapshot remains immutable after membership changes.");
        Assert.That(state.SelectedRuntimeActorRoster, Has.Count.EqualTo(5));

        IReadOnlyList<DailyV1ActorTurnRosterProjection> beforeRemoval =
            state.SelectedRuntimeActorRoster;
        Assert.That(state.TryCreateSelectedRuntimeActorRosterWithoutMember(
            "npc/unbound-middle", graph,
            out IReadOnlyList<DailyV1ActorTurnRosterProjection> afterRemoval), Is.True);
        Assert.That(state.TryReplaceSelectedRuntimeMembership(
            graph, graph, beforeRemoval, afterRemoval), Is.True);

        IReadOnlyList<DailyV1ActorTurnRosterProjection> current =
            state.SelectedRuntimeActorRoster;
        Assert.That(current, Has.Count.EqualTo(4));
        Assert.That(current[0].PersonId, Is.SameAs(firstPerson));
        Assert.That(current[1], Is.Null,
            "Removing an earlier member shifts later ordinals without compacting a null slot.");
        Assert.That(current[2].PersonId, Is.SameAs(secondPerson));
        Assert.That(current[3].PersonId, Is.SameAs(appendedPerson));
        Assert.That(DailyV1ActorTurnExecutionStage.TryBuildRoster(
            graph, current, out IReadOnlyList<DailyV1ActorTurnRosterEntry> entries), Is.True);
        Assert.That(entries.Select(entry => entry.RosterOrdinal), Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(entries[0].ActorOwner, Is.SameAs(first));
        Assert.That(entries[1].ActorOwner, Is.Null);
        Assert.That(entries[2].ActorOwner, Is.SameAs(second));
        Assert.That(entries[3].ActorOwner, Is.Null);
    }

    [Test]
    public void CoreBoundaryRefreshesPersonBindingWithoutChangingActorRosterSlots()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor, out DailyV1NpcRuntimeOwners other);
        PersonStore people = new PersonStore();
        PersonId personId = new PersonId("person-core-roster-late-binding");
        Assert.That(people.TryRegister(new PersonRuntime(personId), out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(graph.TryBindPersonStore(people), Is.True);

        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        Assert.That(state.TryBindSelectedRuntimeOwnerGraph(graph), Is.True);
        Assert.That(state.TryBindSelectedRuntimeActorRoster(new[]
        {
            new DailyV1ActorTurnRosterProjection(actor.RuntimeId, null),
            null,
            new DailyV1ActorTurnRosterProjection("npc/unselected", null),
            new DailyV1ActorTurnRosterProjection(other.RuntimeId, null)
        }), Is.True);
        IReadOnlyList<DailyV1ActorTurnRosterProjection> before =
            state.SelectedRuntimeActorRoster;

        Assert.That(state.TryCreateSelectedRuntimeActorRosterWithPersonBinding(
                actor.RuntimeId, personId,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> expected,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacement), Is.True);
        Assert.That(expected, Is.SameAs(before));
        Assert.That(state.TryCommitSelectedRuntimeActorRoster(expected, replacement), Is.True);
        Assert.That(before[0].PersonId, Is.Null,
            "Published roster snapshots are immutable across a PersonId binding.");
        Assert.That(state.SelectedRuntimeActorRoster, Has.Count.EqualTo(4));
        Assert.That(state.SelectedRuntimeActorRoster[0].RuntimeId, Is.EqualTo(actor.RuntimeId));
        Assert.That(state.SelectedRuntimeActorRoster[0].PersonId, Is.SameAs(personId));
        Assert.That(state.SelectedRuntimeActorRoster[1], Is.Null,
            "Late binding preserves null slots and actor-turn ordinals.");
        Assert.That(state.SelectedRuntimeActorRoster[2].RuntimeId, Is.EqualTo("npc/unselected"));
        Assert.That(state.SelectedRuntimeActorRoster[3].RuntimeId, Is.EqualTo(other.RuntimeId));

        Assert.That(people.TryBindMaterializedNpc(personId, actor.RuntimeId,
            out PersonStoreFailure bindFailure), Is.True, bindFailure.ToString());
        Assert.That(DailyV1ActorTurnExecutionStage.TryBuildRoster(
            graph, state.SelectedRuntimeActorRoster,
            out IReadOnlyList<DailyV1ActorTurnRosterEntry> entries), Is.True);
        Assert.That(entries[0].PersonId, Is.SameAs(personId));
        Assert.That(entries[0].ActorOwner, Is.SameAs(actor));
        Assert.That(entries[1].RosterOrdinal, Is.EqualTo(1));

        Assert.That(state.TryCreateSelectedRuntimeActorRosterWithPersonBinding(
            actor.RuntimeId, personId, out _, out _), Is.False,
            "An already-bound actor cannot publish a second PersonId transition.");
        Assert.That(state.SelectedRuntimeActorRoster, Is.SameAs(replacement));
    }

    [Test]
    public void CoreRuntimeRosterRejectsDuplicateIdentityAndPartialSelectedGraphPublication()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners first, out DailyV1NpcRuntimeOwners second);
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        Assert.That(state.TryBindSelectedRuntimeOwnerGraph(graph), Is.True);
        PersonId firstPerson = new PersonId("person-core-roster-atomic-first");
        PersonId secondPerson = new PersonId("person-core-roster-atomic-second");
        Assert.That(state.TryBindSelectedRuntimeActorRoster(new[]
        {
            new DailyV1ActorTurnRosterProjection(first.RuntimeId, firstPerson),
            new DailyV1ActorTurnRosterProjection(second.RuntimeId, secondPerson)
        }), Is.True);

        IReadOnlyList<DailyV1ActorTurnRosterProjection> prior = state.SelectedRuntimeActorRoster;
        Assert.That(state.TryCreateSelectedRuntimeActorRosterWithAppendedMember(
            new DailyV1ActorTurnRosterProjection(first.RuntimeId, firstPerson), graph, out _), Is.False);
        Assert.That(state.TryReplaceSelectedRuntimeMembership(graph, graph, prior,
            new[] { new DailyV1ActorTurnRosterProjection(first.RuntimeId, firstPerson) }), Is.False,
            "A new roster cannot be published while omitting a selected Core owner.");
        Assert.That(state.SelectedRuntimeOwnerGraph, Is.SameAs(graph));
        Assert.That(state.SelectedRuntimeActorRoster, Is.SameAs(prior),
            "Rejected identity changes leave graph and roster publication unchanged.");
        Assert.That(state.SelectedRuntimeActorRoster[0].PersonId, Is.SameAs(firstPerson));
        Assert.That(state.SelectedRuntimeActorRoster[1].PersonId, Is.SameAs(secondPerson));
    }

    [Test]
    public void CoreRejectsSelectedRosterPersonThatDiffersFromMaterializedStoreBinding()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor);
        PersonStore people = new PersonStore();
        PersonId boundPerson = RegisterMaterializedPerson(people, actor, "person-exact-roster-binding");
        Assert.That(graph.TryBindPersonStore(people), Is.True);

        Assert.That(DailyV1ActorTurnExecutionStage.TryBuildRoster(
            graph,
            new[]
            {
                new DailyV1ActorTurnRosterProjection(
                    actor.RuntimeId, new PersonId(boundPerson.Value))
            },
            out _), Is.False,
            "A selected host projection must retain the exact materialized PersonId instance owned by Core.");
        Assert.That(DailyV1ActorTurnExecutionStage.TryBuildRoster(
            graph,
            new[] { new DailyV1ActorTurnRosterProjection(actor.RuntimeId, boundPerson) },
            out IReadOnlyList<DailyV1ActorTurnRosterEntry> roster), Is.True);
        Assert.That(roster[0].PersonId, Is.SameAs(boundPerson));
    }

    [Test]
    public void CoreRejectsMismatchedMaterializedPersonBeforeActorTurnDispatch()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor);
        PersonStore people = new PersonStore();
        PersonId boundPerson = RegisterMaterializedPerson(
            people, actor, "person-exact-dispatch-binding");
        Assert.That(graph.TryBindPersonStore(people), Is.True);

        ActorChoiceStore choices = new ActorChoiceStore(people);
        RecordingBoundary boundary = new RecordingBoundary();
        IReadOnlyList<DailyV1ActorTurnRosterEntry> mismatchedRoster = new[]
        {
            new DailyV1ActorTurnRosterEntry(
                0, new PersonId(boundPerson.Value), actor)
        };

        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph, mismatchedRoster, people, choices, null, 1L, true, boundary), Is.False);
        Assert.That(boundary.Events, Is.Empty,
            "A host identity projection cannot dispatch any actor work for a different PersonId instance.");
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
    public void CoreExpeditionOwnerDefersActorChoiceWithoutUnityExpeditionQuery()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        PersonStore people = new PersonStore();
        PersonId personId = RegisterMaterializedPerson(people, actor, "person-expedition");
        ActorChoiceStore choices = new ActorChoiceStore(people);
        Assert.That(choices.TryCapture("command-expedition", personId, "action.sell-goods",
            WorldCommandOrigin.LocalPlayer, WorldCommandAuthorityMode.Request, 4L,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode captureFailure),
            Is.True, captureFailure.ToString());

        ExpeditionStore expeditions = new ExpeditionStore();
        ExpeditionRuntime activeExpedition = new ExpeditionRuntime(
            "expedition-active", "site-1", "location-origin", "location-target", "route-outbound",
            null, "decision-expedition", new[] { actor.RuntimeId }, new[] { actor.RuntimeId },
            System.Array.Empty<string>(), ExpeditionState.AtSite);
        Assert.That(expeditions.Add(activeExpedition), Is.True);
        Assert.That(graph.TryBindExpeditionStore(expeditions), Is.True);
        Assert.That(graph.Expeditions, Is.SameAs(expeditions));
        long expeditionRevision = expeditions.Revision;
        RecordingBoundary boundary = new RecordingBoundary();

        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph,
            new[] { new DailyV1ActorTurnRosterEntry(0, personId, actor) },
            people,
            choices,
            null,
            5L,
            false,
            boundary), Is.True);

        Assert.That(boundary.Events, Is.Empty,
            "Expedition membership is read from the Core owner before any host gameplay callback.");
        Assert.That(expeditions.GetById("expedition-active"), Is.SameAs(activeExpedition));
        Assert.That(expeditions.Revision, Is.EqualTo(expeditionRevision));
        Assert.That(choices.TryGet(input.InputId, out ActorChoiceInput deferred), Is.True);
        Assert.That(deferred.Status, Is.EqualTo(ActorChoiceInputStatus.Pending));
        Assert.That(deferred.Dispositions.Single().Kind, Is.EqualTo(ActorChoiceDispositionKind.Deferred));
        Assert.That(deferred.Dispositions.Single().DeferralReason,
            Is.EqualTo(ActorChoiceDeferralReason.ExpeditionParticipant));
        Assert.That(deferred.Dispositions.Single().ActorTurnRosterOrdinal, Is.Zero);
    }

    [Test]
    public void SelectedActorTurnRunsCoreActorChoiceStageWithoutUnityGameplayCallback()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        PersonStore people = new PersonStore();
        PersonId personId = RegisterMaterializedPerson(people, actor, "person-core-choice");
        ActorChoiceStore choices = new ActorChoiceStore(people);
        Assert.That(choices.TryCapture("command-core-choice", personId,
            "action/not-admitted", WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request, 4L,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode captureFailure),
            Is.True, captureFailure.ToString());

        DailyV1ActionDefinition priorAction = actor.Definitions.Actions["VENDER_MERCADORIA"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            actor.Definitions, priorAction, null, null, null, 0,
            0f, 1f, NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner priorPayload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, priorAction,
            priorPayload, () => true), Is.True);

        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary();
        boundary.BindGraph(graph);
        long actionSlotRevisionBefore = actor.ActionSlot.Revision;
        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph,
            new[] { new DailyV1ActorTurnRosterEntry(0, personId, actor) },
            people, choices, null, 5L, merchantTradeEnabled: false, boundary: boundary), Is.True);

        Assert.That(boundary.Events, Does.Not.Contain("legacy-choice-callback"),
            "The selected Core actor-choice path does not invoke the legacy Unity choice callback.");
        Assert.That(boundary.Events, Does.Not.Contain("legacy-action-callback"),
            "A rejected choice does not dispatch through the legacy Unity action callback.");
        Assert.That(actor.ActionSlot.CurrentAction, Is.Null);
        Assert.That(actor.ActionSlot.CurrentPayload, Is.Null);
        Assert.That(actor.ActionSlot.Revision, Is.EqualTo(actionSlotRevisionBefore + 1L),
            "The selected Core path clears the prior action slot before rejecting the unavailable choice.");
        Assert.That(choices.TryGet(input.InputId, out ActorChoiceInput rejected), Is.True);
        Assert.That(rejected.Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(rejected.Dispositions.Single().Failure,
            Is.EqualTo(ActorChoiceFailure.ActionUnavailable));
    }

    [Test]
    public void SelectedActorChoiceDispatchUsesCoreActionExecutionPorts()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        DailyV1CityRuntimeOwners city = graph.Cities.Single(owner =>
            owner.RuntimeId == actor.Travel.CurrentCityRuntimeId);
        DailyV1ItemDefinition iron = graph.Definitions.Items["FERRO"];
        Assert.That(actor.Inventory.TryAddItem(iron, 5, 10f), Is.True);
        float referencePrice = actor.Inventory.GetAverageUnitCost(iron);
        Assert.That(actor.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            city.LocationRuntimeId, iron, referencePrice + 3f, 12, 20L, 20L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);

        PersonStore people = new PersonStore();
        PersonId personId = RegisterMaterializedPerson(people, actor, "person-core-choice-sale");
        ActorChoiceStore choices = new ActorChoiceStore(people);
        int choiceAdmissionCount = 0;
        int choiceCommitCount = 0;
        choices.BindP12MutationBoundary(
            () => { choiceAdmissionCount++; return true; },
            () => choiceCommitCount++);
        Assert.That(choices.TryCapture("command-core-choice-sale", personId,
            "VENDER_MERCADORIA", WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request, 19L,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode captureFailure),
            Is.True, captureFailure.ToString());

        int inventoryBefore = actor.Inventory.GetAmount(iron);
        int marketBefore = city.Market.GetAmount(iron);
        float balanceBefore = actor.MoneyAccount.Balance;
        long accountRevisionBefore = actor.MoneyAccount.Revision;
        long inventoryRevisionBefore = actor.Inventory.Revision;
        long marketRevisionBefore = city.Market.Revision;

        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary();
        boundary.BindGraph(graph);
        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph,
            new[] { new DailyV1ActorTurnRosterEntry(0, personId, actor) },
            people, choices, null, 20L, merchantTradeEnabled: false, boundary: boundary), Is.True);

        Assert.That(boundary.Events, Does.Contain("complete-dispatch"),
            "The committed actor-choice slot was dispatched through the Core selected action stage.");
        Assert.That(boundary.Events, Does.Not.Contain("legacy-choice-callback"));
        Assert.That(boundary.Events, Does.Not.Contain("legacy-action-callback"));
        Assert.That(choices.TryGet(input.InputId, out ActorChoiceInput attempted), Is.True);
        Assert.That(attempted.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        Assert.That(attempted.Dispositions[0].DecisionRecordId,
            Is.EqualTo(boundary.Decisions.Decisions.Single().DecisionId));
        Assert.That(attempted.Dispositions[1].AttemptOutcome,
            Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));
        DailyV1NpcActionPayloadOwner salePayload = actor.ActionSlot.CurrentPayload;
        Assert.That(salePayload, Is.Not.Null);
        Assert.That(boundary.SaleAdmissionCount, Is.EqualTo(1));
        Assert.That(boundary.ActionSlotAdmissionCount, Is.EqualTo(1));
        Assert.That(boundary.SaleReceiptCount, Is.EqualTo(1));
        Assert.That(boundary.SaleDisposeCount, Is.EqualTo(1));
        Assert.That(boundary.LastSaleCommit?.Succeeded, Is.True);
        Assert.That(actor.Inventory.GetAmount(iron), Is.EqualTo(inventoryBefore - salePayload.Amount));
        Assert.That(city.Market.GetAmount(iron), Is.EqualTo(marketBefore + salePayload.Amount));
        Assert.That(actor.MoneyAccount.Balance, Is.GreaterThan(balanceBefore));
        Assert.That(actor.MoneyAccount.Revision, Is.EqualTo(accountRevisionBefore + 1L));
        Assert.That(actor.Inventory.Revision, Is.EqualTo(inventoryRevisionBefore + 1L));
        Assert.That(city.Market.Revision, Is.EqualTo(marketRevisionBefore + 1L));
        Assert.That(choiceAdmissionCount, Is.EqualTo(3),
            "Capture, dispatch-start and returned-attempt transitions each use their existing P12 boundary.");
        Assert.That(choiceCommitCount, Is.EqualTo(3));
    }

    [Test]
    public void SelectedActorChoiceEligibilityUsesAdmittedMerchantConfiguration()
    {
        DailyV1EffectiveDefinitions definitions = CopyWithMerchantTradeEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), false);
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            definitions, out DailyV1NpcRuntimeOwners actor);
        Assert.That(graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled, Is.False);

        PersonStore people = new PersonStore();
        PersonId personId = RegisterMaterializedPerson(people, actor, "person-disabled-merchant-choice");
        ActorChoiceStore choices = new ActorChoiceStore(people);
        Assert.That(choices.TryCapture("command-disabled-merchant-choice", personId,
            "VENDER_MERCADORIA", WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request, 4L,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode captureFailure),
            Is.True, captureFailure.ToString());

        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary();
        boundary.BindGraph(graph);
        DailyV1ActionDefinition priorAction = actor.Definitions.Actions["VENDER_MERCADORIA"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            actor.Definitions, priorAction, null, null, null, 0,
            0f, 1f, NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner priorPayload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, priorAction,
            priorPayload, () => true), Is.True);
        long actionSlotRevisionBefore = actor.ActionSlot.Revision;
        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph,
            new[] { new DailyV1ActorTurnRosterEntry(0, personId, actor) },
            people, choices, null, 5L, merchantTradeEnabled: false, boundary: boundary), Is.True);

        Assert.That(actor.ActionSlot.CurrentAction, Is.Null);
        Assert.That(actor.ActionSlot.CurrentPayload, Is.Null);
        Assert.That(actor.ActionSlot.Revision, Is.EqualTo(actionSlotRevisionBefore + 1L));
        Assert.That(boundary.Events, Is.EqualTo(new[]
        {
            "clear-prepare", "clear-admission", "clear-projection"
        }), "Clearing the prior Core slot is projected, while the disabled Merchant action never reaches sale dispatch.");
        Assert.That(choices.TryGet(input.InputId, out ActorChoiceInput rejected), Is.True);
        Assert.That(rejected.Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(rejected.Dispositions.Single().Failure,
            Is.EqualTo(ActorChoiceFailure.ActionUnavailable));
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
    public void SelectedActionClearCommitsCoreRevisionBetweenAdmissionAndProjection()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        DailyV1ActionDefinition action = graph.Definitions.Actions.Values.First();
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            graph.Definitions, action, null, null, null, 0,
            0f, 1f, NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, payload, () => true), Is.True);
        long revisionBefore = actor.ActionSlot.Revision;

        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary();
        boundary.BindGraph(graph);
        Assert.That(DailyV1SelectedActorActionExecutionStage.TryClearCurrentAction(
            graph, actor, boundary), Is.True);

        Assert.That(actor.ActionSlot.CurrentAction, Is.Null);
        Assert.That(actor.ActionSlot.CurrentPayload, Is.Null);
        Assert.That(actor.ActionSlot.Revision, Is.EqualTo(revisionBefore + 1L));
        Assert.That(boundary.Events, Is.EqualTo(new[]
        {
            "clear-prepare", "clear-admission", "clear-projection"
        }));
    }

    [Test]
    public void RejectedSelectedActionClearLeavesCoreOwnerAndRevisionUnchanged()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        DailyV1ActionDefinition action = graph.Definitions.Actions.Values.First();
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            graph.Definitions, action, null, null, null, 0,
            0f, 1f, NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, payload, () => true), Is.True);
        long revisionBefore = actor.ActionSlot.Revision;

        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary
        {
            RejectNextActionSlotAdmission = true
        };
        boundary.BindGraph(graph);
        Assert.That(DailyV1SelectedActorActionExecutionStage.TryClearCurrentAction(
            graph, actor, boundary), Is.False);

        Assert.That(actor.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(actor.ActionSlot.CurrentPayload, Is.SameAs(payload));
        Assert.That(actor.ActionSlot.Revision, Is.EqualTo(revisionBefore));
        Assert.That(boundary.Events, Is.EqualTo(new[]
        {
            "clear-prepare", "clear-admission", "clear-cancel"
        }));
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
            people, choices, directives, 12L, false, boundary), Is.True);

        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Succeeded), directive.ResultReason);
        Assert.That(justice.ContainsSentence(sentence), Is.False);
        Assert.That(boundary.Decisions.Decisions, Is.Empty);
        Assert.That(boundary.Random.Calls, Is.Zero);
        Assert.That(boundary.Events.Count(value => value == "p12-begin"), Is.EqualTo(1));
        Assert.That(boundary.Events.Count(value => value == "p12-end"), Is.EqualTo(1));
        Assert.That(boundary.Events, Does.Contain("escape-published"));
        Assert.That(boundary.Events, Does.Not.Contain("legacy-directive"),
            "Selected RequestAction/ForceOutcome directives execute through Core, not the Unity gameplay callback.");
        Assert.That(boundary.Events, Does.Not.Contain("reserved-query"),
            "The selected Daily-v1 exact-empty reservation fact is established by the admitted profile and Core owner graph.");
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
        return CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(),
            out first, out second, includeSecond);
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        DailyV1EffectiveDefinitions definitions,
        out DailyV1NpcRuntimeOwners actor)
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            definitions, out DailyV1NpcRuntimeOwners first, out _, includeSecond: false);
        actor = first;
        return graph;
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        DailyV1EffectiveDefinitions definitions,
        out DailyV1NpcRuntimeOwners first,
        out DailyV1NpcRuntimeOwners second,
        bool includeSecond)
    {
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

    private static DailyV1EffectiveDefinitions CopyWithMerchantTradeEnabled(
        DailyV1EffectiveDefinitions definitions,
        bool enabled)
    {
        EffectiveSimulationConfiguration current = definitions.EffectiveConfiguration;
        EffectiveMerchantTradeConfiguration currentMerchant = current.MerchantTrade;
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            current.Population,
            current.Economy,
            current.Travel,
            current.Crime,
            current.GuardCrime,
            current.NaturalMortality,
            current.AggregateDemography,
            new EffectiveMerchantTradeConfiguration(
                enabled,
                currentMerchant.AllowAutonomousTradeRepositioning,
                currentMerchant.MaxTradeAmount,
                currentMerchant.LocalWholesalePriceMultiplier,
                currentMerchant.LocalReserveRatio,
                currentMerchant.MaxUnprofitablePlanWaitDays),
            current.CommercialKnowledge);
        return new DailyV1EffectiveDefinitions(
            definitions.Configuration,
            configuration,
            definitions.Calendar,
            null,
            definitions.Cities.Values,
            definitions.Items.Values,
            definitions.Npcs.Values,
            definitions.Jobs.Values,
            definitions.Actions.Values,
            definitions.Statuses.Values,
            null,
            definitions.StageOrder,
            definitions.ProfileContractIdentity,
            definitions.GeographyProfileContractIdentity,
            definitions.GeographyStageIdentity,
            definitions.HexCoordinateConvention,
            definitions.HexCoordinateCanonicalOrder,
            null,
            definitions.Traits.Values,
            definitions.CapabilityAttributes.Values,
            definitions.ExplorableSites.Values);
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

    private class RecordingBoundary : IDailyV1ActorTurnExecutionBoundary
    {
        internal readonly List<string> Events = new List<string>();
        internal bool IsSpatialTransit;

        public bool IsInSpatialTransit(DailyV1NpcRuntimeOwners actor, PersonId personId) => IsSpatialTransit;
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

    private sealed class CoreActorChoiceTurnBoundary : RecordingBoundary,
        IDailyV1SelectedActorChoiceExecutionContext
    {
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        internal readonly TestActorChoiceBoundary ChoiceBoundary = new TestActorChoiceBoundary();

        internal CoreActorChoiceTurnBoundary(DailyV1SelectedRuntimeOwnerGraph graph) =>
            this.graph = graph;

        public bool TryGetActorChoiceExecutionContext(
            DailyV1NpcRuntimeOwners actor,
            out bool actorAtCurrentCityLocation,
            out bool decisionSystemAvailable,
            out IDailyV1ActorChoiceExecutionBoundary executionBoundary)
        {
            actorAtCurrentCityLocation = false;
            decisionSystemAvailable = true;
            executionBoundary = graph.ContainsExactNpcOwner(actor) ? ChoiceBoundary : null;
            return executionBoundary != null;
        }
    }

    private sealed class TestActorChoiceBoundary : IDailyV1ActorChoiceExecutionBoundary
    {
        internal int ClearCount;
        public void ClearCurrentAction(DailyV1NpcRuntimeOwners actor)
        {
            ClearCount++;
            actor.ActionSlot.TrySetCurrentAction(null, null);
            actor.ActionSlot.NotifyCurrentActionMutationCommitted();
        }
        public bool TryGetDecisionRecordOwners(DailyV1NpcRuntimeOwners actor,
            out RuntimeIdAllocator idAllocator, out SimulationRecordSequence recordSequence,
            out NpcDecisionStore decisionStore)
        {
            idAllocator = null;
            recordSequence = null;
            decisionStore = null;
            return false;
        }
        public bool TryPrepareRequestedActionProjection(DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) => false;
        public bool TryRefreshRequestedActionProjection(DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) => false;
        public bool CanCommitRequestedActionSlot(DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) => false;
        public bool TryPublishRequestedActionProjection(DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) => false;
        public void CancelPreparedRequestedActionProjection(DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) { }
        public void LogRequestedActionDecisionFailure(string message) { }
        public NpcActionResultType? ExecuteCurrentAction(DailyV1NpcRuntimeOwners actor) =>
            throw new AssertionException("The unavailable actor-choice fixture must not dispatch an action.");
    }

    private sealed class CoreSelectedActionBoundary :
        IDailyV1ActorTurnExecutionBoundary,
        IDailyV1SelectedActorChoiceExecutionContext,
        IDailyV1CoreActorChoiceExecutionBoundary,
        IDailyV1SelectedActorActionExecutionBoundary,
        IDailyV1SelectedNpcActionDecisionBoundary,
        IDailyV1SelectedActionDispatchBoundary,
        DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary,
        DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary,
        IDailyV1SelectedOpenMarketSaleBoundary,
        IDailyV1SelectedOpenMarketPurchaseBoundary
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
        internal int SaleAdmissionCount;
        internal int ActionSlotAdmissionCount;
        internal int SaleReceiptCount;
        internal int SaleDisposeCount;
        internal DailyV1SelectedOpenMarketSaleCommitResult LastSaleCommit;
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

        public bool TryGetActorChoiceExecutionContext(
            DailyV1NpcRuntimeOwners actor,
            out bool actorAtCurrentCityLocation,
            out bool decisionSystemAvailable,
            out IDailyV1ActorChoiceExecutionBoundary executionBoundary)
        {
            actorAtCurrentCityLocation = CurrentGraph?.ContainsExactNpcOwner(actor) == true;
            decisionSystemAvailable = true;
            executionBoundary = actorAtCurrentCityLocation ? this : null;
            return executionBoundary != null;
        }

        public bool TryPrepareRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            if (!CurrentGraph.ContainsExactNpcOwner(actor) || payload == null
                || !CanProjectSelectedAction || preparedProjectionPayload != null)
                return false;
            preparedProjectionActor = actor;
            preparedProjectionPayload = payload;
            return true;
        }

        public bool TryRefreshRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) =>
            ReferenceEquals(preparedProjectionActor, actor)
            && ReferenceEquals(preparedProjectionPayload, payload);

        public bool CanCommitRequestedActionSlot(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) =>
            CanCommitSelectedActionSlot(actor, payload);

        public bool TryPublishRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) =>
            TryPublishSelectedActionProjection(actor, payload);

        public void CancelPreparedRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) =>
            CancelPreparedSelectedActionProjection(actor, payload);

        public void LogRequestedActionDecisionFailure(string message) =>
            Events.Add("choice-decision-failure:" + message);

        public NpcActionResultType? ExecuteCurrentAction(DailyV1NpcRuntimeOwners actor) =>
            throw new AssertionException(
                "Selected actor-choice execution must use Core action ports, not the legacy callback.");

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

        public bool TryPrepareCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor)
        {
            if (preparedProjectionPayload != null
                || !CurrentGraph.ContainsExactNpcOwner(actor)
                || actor.ActionSlot?.CurrentPayload == null)
                return false;
            preparedProjectionActor = actor;
            preparedProjectionPayload = actor.ActionSlot.CurrentPayload;
            Events.Add("clear-prepare");
            return true;
        }

        public bool CanCommitCurrentActionClear(DailyV1NpcRuntimeOwners actor)
        {
            if (!ReferenceEquals(preparedProjectionActor, actor)
                || preparedProjectionPayload == null)
                return false;
            Events.Add("clear-admission");
            ActionSlotAdmissionCount++;
            if (!RejectNextActionSlotAdmission) return true;
            RejectNextActionSlotAdmission = false;
            return false;
        }

        public bool TryPublishCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor)
        {
            if (!ReferenceEquals(preparedProjectionActor, actor)
                || preparedProjectionPayload == null
                || actor.ActionSlot?.CurrentAction != null
                || actor.ActionSlot?.CurrentPayload != null)
                return false;
            ClearPreparedProjection();
            Events.Add("clear-projection");
            return true;
        }

        public void CancelPreparedCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor)
        {
            if (!ReferenceEquals(preparedProjectionActor, actor)) return;
            ClearPreparedProjection();
            Events.Add("clear-cancel");
        }

        public bool TryPrepareForcedOutcomeActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            DailyV1NpcActionPayloadOwner payload)
        {
            if (preparedProjectionPayload != null
                || !CurrentGraph.ContainsExactNpcOwner(actor)
                || !ReferenceEquals(payload?.Action, action))
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
            ActionSlotAdmissionCount++;
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
        public bool IsReservedToday(DailyV1NpcRuntimeOwners actor)
        {
            Events.Add("reserved-query");
            return false;
        }
        public void EvaluateStatus(DailyV1NpcRuntimeOwners actor) => Events.Add("status");
        public void ProcessScheduledDirective(DailyV1NpcRuntimeOwners actor, ScheduledDirective directive) =>
            Events.Add("legacy-directive");
        public bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal)
        {
            Events.Add("legacy-choice-callback");
            return false;
        }
        public void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor) { }
        public void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor) => Events.Add("legacy-action-callback");

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
            saleBoundary = this;
            purchaseBoundary = this;
            return CurrentGraph.ContainsExactNpcOwner(actor);
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

        public bool TryBegin()
        {
            SaleAdmissionCount++;
            return true;
        }

        public void Complete(DailyV1SelectedOpenMarketSaleCommitResult result)
        {
            SaleReceiptCount++;
            LastSaleCommit = result;
        }

        public void Complete(DailyV1SelectedOpenMarketPurchaseCommitResult result) { }

        public void Dispose() => SaleDisposeCount++;

        public bool CanUseAccountBacked => true;

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
