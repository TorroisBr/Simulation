using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1ActorTurnExecutionStageTests
{
    [Test]
    public void AdmittedSelectedLoopRejectsHostOwnedActionFallbackBeforeActorCallbacks()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor);
        PersonStore people = new PersonStore();
        ActorChoiceStore choices = new ActorChoiceStore(people);
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(new SimulationTime(5L));
        RecordingBoundary boundary = new RecordingBoundary();
        DailyV1ExecutionBoundaryState executionState = CreateActorTurnExecutionState(
            graph, choices, new RuntimeIdAllocator(), new DeterministicRandomSource(5),
            new SimulationRecordSequence(), new NpcDecisionStore());
        DailyV1ActorTurnCoreRoots coreRoots = new DailyV1ActorTurnCoreRoots(executionState, graph);
        long actionRevision = actor.ActionSlot.Revision;

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            DailyV1ActorTurnExecutionStage.TryRunOrdered(
                graph,
                new[] { new DailyV1ActorTurnRosterEntry(0, null, actor) },
                people,
                choices,
                directives,
                absoluteDay: 5L,
                merchantTradeEnabled: false,
                requireCoreOwnedSelectedOperations: true,
                coreRoots: coreRoots,
                boundary: boundary));

        Assert.That(failure.Message, Does.Contain("Core-owned action and actor-choice"));
        Assert.That(boundary.Events, Is.Empty,
            "The admitted path rejects the legacy host before invoking any actor callback.");
        Assert.That(actor.ActionSlot.Revision, Is.EqualTo(actionRevision));
        Assert.That(choices.PendingInputs, Is.Empty);
    }

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
    public void CoreBoundaryRebindsActorTurnRootsWithSelectedMembershipGraph()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners first);
        ActorChoiceStore choices = new ActorChoiceStore(new PersonStore());
        DailyV1ExecutionBoundaryState state = CreateActorTurnExecutionState(
            graph, choices, new RuntimeIdAllocator(), new DeterministicRandomSource(5),
            new SimulationRecordSequence(), new NpcDecisionStore());
        DailyV1ActorTurnCoreRoots priorRoots = state.SelectedRuntimeActorTurnCoreRoots;
        IReadOnlyList<DailyV1ActorTurnRosterProjection> priorRoster =
            state.SelectedRuntimeActorRoster;

        DailyV1NpcDefinition addedDefinition = graph.Definitions.Npcs.Values
            .First(definition => !string.Equals(
                definition.DefinitionId, first.Definition.DefinitionId, System.StringComparison.Ordinal));
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc/actor-roots-membership-add", graph.Definitions, addedDefinition, 0f,
            out DailyV1NpcRuntimeOwners addedOwner), Is.True);
        Assert.That(graph.TryCreateWithAddedNpc(
            addedOwner, out DailyV1SelectedRuntimeOwnerGraph replacementGraph), Is.True);
        Assert.That(state.TryCreateSelectedRuntimeActorRosterWithAppendedMember(
            new DailyV1ActorTurnRosterProjection(addedOwner.RuntimeId, null), replacementGraph,
            out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster), Is.True);

        Assert.That(state.TryReplaceSelectedRuntimeMembership(
            graph, replacementGraph, priorRoster, replacementRoster), Is.True);
        Assert.That(state.SelectedRuntimeOwnerGraph, Is.SameAs(replacementGraph));
        Assert.That(state.SelectedRuntimeActorRoster, Is.Not.SameAs(priorRoster));
        Assert.That(state.SelectedRuntimeActorTurnCoreRoots, Is.Not.SameAs(priorRoots));
        Assert.That(state.SelectedRuntimeActorTurnCoreRoots.IsBoundTo(
            state, replacementGraph, choices.PersonStore, choices), Is.True);
        Assert.That(state.SelectedRuntimeActorTurnCoreRoots.IsBoundToGraph(replacementGraph), Is.True);
    }

    [Test]
    public void FailedCoreMembershipReplacementPreservesGraphRosterAndActorTurnRoots()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        ActorChoiceStore choices = new ActorChoiceStore(new PersonStore());
        DailyV1ExecutionBoundaryState state = CreateActorTurnExecutionState(
            graph, choices, new RuntimeIdAllocator(), new DeterministicRandomSource(5),
            new SimulationRecordSequence(), new NpcDecisionStore());
        IReadOnlyList<DailyV1ActorTurnRosterProjection> priorRoster =
            state.SelectedRuntimeActorRoster;
        DailyV1ActorTurnCoreRoots priorRoots = state.SelectedRuntimeActorTurnCoreRoots;

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            graph.Cities, graph.Npcs, graph.SpatialNetwork,
            out DailyV1SelectedRuntimeOwnerGraph replacementGraph), Is.True);
        Assert.That(replacementGraph.PersonStore, Is.Null,
            "The proposed graph has the same owner instances but lacks the exact bound PersonStore.");
        Assert.That(state.TryReplaceSelectedRuntimeMembership(
            graph, replacementGraph, priorRoster, priorRoster), Is.False);

        Assert.That(state.SelectedRuntimeOwnerGraph, Is.SameAs(graph));
        Assert.That(state.SelectedRuntimeActorRoster, Is.SameAs(priorRoster));
        Assert.That(state.SelectedRuntimeActorTurnCoreRoots, Is.SameAs(priorRoots));
        Assert.That(priorRoots.IsBoundTo(
            state, graph, choices.PersonStore, choices), Is.True);
        Assert.That(actor.RuntimeId, Is.EqualTo(priorRoster[0].RuntimeId));
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
    public void SelectedOwnerGraphDerivesCurrentCityLocationFromExactP8Owners()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateFullGraph();
        Assert.That(graph.Npcs.Count, Is.GreaterThanOrEqualTo(4));
        DailyV1NpcRuntimeOwners atCityActor = graph.Npcs[0];
        DailyV1NpcRuntimeOwners elsewhereActor = graph.Npcs[1];
        DailyV1NpcRuntimeOwners transitActor = graph.Npcs[2];
        DailyV1NpcRuntimeOwners noPositionActor = graph.Npcs[3];
        PersonStore people = new PersonStore();
        PersonId atCityPerson = RegisterMaterializedPerson(
            people, atCityActor, "person-at-selected-city");
        PersonId elsewherePerson = RegisterMaterializedPerson(
            people, elsewhereActor, "person-away-from-selected-city");
        PersonId transitPerson = RegisterMaterializedPerson(
            people, transitActor, "person-in-selected-transit");
        PersonId noPositionPerson = RegisterMaterializedPerson(
            people, noPositionActor, "person-without-position");

        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        HexId fromHex = new HexId("hex.selected-city.from");
        HexId toHex = new HexId("hex.selected-city.to");
        HexBoundaryKey boundary = new HexBoundaryKey(fromHex, toHex);
        LocationId wrongLocation = new LocationId("location.selected-city.other");
        List<LocationRecord> locations = new List<LocationRecord>
        {
            new LocationRecord(wrongLocation, toHex)
        };
        Dictionary<string, LocationId> cityLocations =
            new Dictionary<string, LocationId>(System.StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (cityLocations.ContainsKey(city.RuntimeId)) continue;
            LocationId cityLocation = new LocationId("location.city." + city.RuntimeId);
            cityLocations.Add(city.RuntimeId, cityLocation);
            locations.Add(new LocationRecord(cityLocation, fromHex));
        }
        Assert.That(cityLocations.ContainsKey(atCityActor.Travel.CurrentCityRuntimeId), Is.True);
        Assert.That(spatial.TryComposeGeography(new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("selected-city.scale", "test", "v1", 1m, "unit"),
            new[]
            {
                new HexRecord(fromHex, new HexCoordinate(0, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain.selected-city"), "v1")),
                new HexRecord(toHex, new HexCoordinate(1, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain.selected-city"), "v1"))
            }, locations), out SpatialAuthorityFailure geographyFailure),
            Is.True, geographyFailure?.ToString());
        CrossingId crossingId = new CrossingId("crossing.selected-city.transit");
        Assert.That(spatial.TryRegisterCrossing(new CrossingRecord(
            crossingId, boundary, fromHex, "selected-city-crossing", "v1", null),
            out SpatialAuthorityFailure crossingFailure), Is.True, crossingFailure?.ToString());

        LegacySpatialAnchorBindingStore anchors = new LegacySpatialAnchorBindingStore(spatial);
        foreach (KeyValuePair<string, LocationId> binding in cityLocations)
            Assert.That(anchors.TryBindCity(binding.Key, binding.Value,
                out SpatialAnchorBindingFailure anchorFailure), Is.True, anchorFailure?.ToString());
        PersonSpatialPositionStore positions = new PersonSpatialPositionStore(
            people, spatial, new SpatialPassageTraversalOptionResolver(spatial.PassageAuthority));
        Assert.That(positions.TrySetAt(atCityPerson,
            StablePositionReference.ForLocation(cityLocations[atCityActor.Travel.CurrentCityRuntimeId]),
            out PersonSpatialPositionFailure atCityFailure), Is.True, atCityFailure?.ToString());
        Assert.That(positions.TrySetAt(elsewherePerson,
            StablePositionReference.ForLocation(wrongLocation),
            out PersonSpatialPositionFailure elsewhereFailure), Is.True, elsewhereFailure?.ToString());
        Assert.That(positions.TrySetAt(transitPerson,
            StablePositionReference.ForHex(fromHex),
            out PersonSpatialPositionFailure transitPositionFailure), Is.True,
            transitPositionFailure?.ToString());
        Assert.That(positions.TryBeginTransit(transitPerson,
            TraversalOptionRef.ForCrossing(crossingId), boundary, fromHex, toHex,
            out PersonSpatialPositionFailure transitFailure), Is.True, transitFailure?.ToString());

        Assert.That(graph.TryBindPersonStore(people), Is.True);
        Assert.That(graph.TryBindPersonSpatialPositionStore(positions), Is.True);
        Assert.That(graph.TryBindLegacySpatialAnchorBindingStore(anchors), Is.True);

        Assert.That(graph.IsActorAtCurrentCityLocation(noPositionActor), Is.True,
            "An absent optional position retains the existing legacy at-city behavior.");
        Assert.That(graph.IsActorAtCurrentCityLocation(atCityActor), Is.True,
            "The actor at its exact City LocationId is eligible.");
        Assert.That(graph.IsActorAtCurrentCityLocation(elsewhereActor), Is.False,
            "A Location different from the actor's exact City anchor is not at the current City.");
        Assert.That(graph.IsActorAtCurrentCityLocation(transitActor), Is.False,
            "An actor with an active P8 transit position is not at the current City.");
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
        Assert.That(graph.TryBindPersonStore(people), Is.True);
        AssertSpatialTransit(graph, people, personId);
        ScheduledDirectiveStore directives = CreatePreparedDirectiveStore(actor, 5L);
        RecordingBoundary boundary = new RecordingBoundary();
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
        Assert.That(graph.TryBindPersonStore(people), Is.True);
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

        RuntimeIdAllocator coreIdAllocator = new RuntimeIdAllocator();
        IAuthoritativeRandomSource coreRandom = new FixedRandom(0.75f);
        SimulationRecordSequence coreRecordSequence = new SimulationRecordSequence();
        NpcDecisionStore coreDecisionStore = new NpcDecisionStore();
        DailyV1ExecutionBoundaryState executionState = CreateActorTurnExecutionState(
            graph, choices, coreIdAllocator, coreRandom, coreRecordSequence, coreDecisionStore);
        DailyV1ActorTurnCoreRoots coreRoots = new DailyV1ActorTurnCoreRoots(executionState, graph);

        int inventoryBefore = actor.Inventory.GetAmount(iron);
        int marketBefore = city.Market.GetAmount(iron);
        float balanceBefore = actor.MoneyAccount.Balance;
        long accountRevisionBefore = actor.MoneyAccount.Revision;
        long inventoryRevisionBefore = actor.Inventory.Revision;
        long marketRevisionBefore = city.Market.Revision;

        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary();
        boundary.BindGraph(graph);
        boundary.WithdrawSelectedExecutionAfterPreflight = true;
        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph,
            new[] { new DailyV1ActorTurnRosterEntry(0, personId, actor) },
            people, choices, null, 20L, merchantTradeEnabled: false,
            requireCoreOwnedSelectedOperations: true, coreRoots: coreRoots, boundary: boundary), Is.True);

        Assert.That(boundary.Events, Does.Contain("complete-dispatch"),
            "The committed actor-choice slot was dispatched through the Core selected action stage.");
        Assert.That(boundary.Events, Does.Not.Contain("legacy-choice-callback"));
        Assert.That(boundary.Events, Does.Not.Contain("legacy-action-callback"));
        Assert.That(choices.TryGet(input.InputId, out ActorChoiceInput attempted), Is.True);
        Assert.That(attempted.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        Assert.That(attempted.Dispositions[0].DecisionRecordId,
            Is.EqualTo(coreDecisionStore.Decisions.Single().DecisionId));
        Assert.That(boundary.Decisions.Decisions, Is.Empty,
            "The admitted Core roots own decision recording even when the host boundary exposes different decision owners.");
        Assert.That(boundary.DecisionOwnerLookupCount, Is.Zero,
            "The admitted Core path never asks the Unity boundary for allocator, sequence or decision-store roots.");
        Assert.That(boundary.SelectedExecutionChecks, Is.EqualTo(1),
            "A committed Core choice still dispatches after Unity withdraws its selection flag following preflight.");
        Assert.That(coreIdAllocator.CaptureSnapshot().Counters.Single(counter =>
            counter.FamilyId == "decision").NextSequence, Is.EqualTo(2L),
            "The Core decision allocator consumes one identity from its initial next-sequence value.");
        Assert.That(coreRecordSequence.CaptureSnapshot().NextSequence, Is.EqualTo(2L),
            "The Core record sequence owns both the actor-choice and its action result records.");
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
    public void AdmittedActorTurnConsumesCoreRandomAndDecisionRootsInsteadOfHostRoots()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        PersonStore people = new PersonStore();
        PersonId personId = RegisterMaterializedPerson(people, actor, "person-core-roots-autonomous");
        Assert.That(graph.TryBindPersonStore(people), Is.True);
        ActorChoiceStore choices = new ActorChoiceStore(people);
        RuntimeIdAllocator coreIdAllocator = new RuntimeIdAllocator();
        FixedRandom coreRandom = new FixedRandom(0f);
        SimulationRecordSequence coreRecordSequence = new SimulationRecordSequence();
        NpcDecisionStore coreDecisionStore = new NpcDecisionStore();
        DailyV1ExecutionBoundaryState executionState = CreateActorTurnExecutionState(
            graph, choices, coreIdAllocator, coreRandom, coreRecordSequence, coreDecisionStore);
        DailyV1ActorTurnCoreRoots coreRoots = new DailyV1ActorTurnCoreRoots(executionState, graph);
        CoreSelectedActionBoundary host = new CoreSelectedActionBoundary();
        host.BindGraph(graph);
        long coreSequenceBefore = coreRecordSequence.CaptureSnapshot().NextSequence;

        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph, new[] { new DailyV1ActorTurnRosterEntry(0, personId, actor) },
            people, choices, null, 12L, merchantTradeEnabled: false,
            requireCoreOwnedSelectedOperations: true, coreRoots: coreRoots, boundary: host), Is.True);

        Assert.That(coreRandom.Calls, Is.EqualTo(1),
            "The admitted Core action selector consumes the execution-state RNG root.");
        Assert.That(host.Random.Calls, Is.Zero,
            "A different RNG exposed by the Unity host cannot drive admitted action selection.");
        Assert.That(coreDecisionStore.Decisions, Has.Count.EqualTo(1));
        Assert.That(host.Decisions.Decisions, Is.Empty,
            "Admitted action decisions are recorded in the execution-state Core store.");
        Assert.That(host.DecisionOwnerLookupCount, Is.Zero,
            "The admitted turn does not ask Unity for allocator, sequence or decision-store roots.");
        Assert.That(coreRecordSequence.CaptureSnapshot().NextSequence,
            Is.GreaterThan(coreSequenceBefore));
        Assert.That(actor.ActionSlot.CurrentPayload.OriginDecisionId,
            Is.EqualTo(coreDecisionStore.Decisions.Single().DecisionId));
    }

    private static DailyV1ExecutionBoundaryState CreateActorTurnExecutionState(
        DailyV1SelectedRuntimeOwnerGraph graph,
        ActorChoiceStore actorChoiceStore,
        RuntimeIdAllocator idAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore)
    {
        Assert.That(graph.TryBindPersonStore(actorChoiceStore.PersonStore), Is.True);
        DailyV1CalendarDefinition calendarDefinition = graph.Definitions.Calendar;
        CalendarDefinition calendar = calendarDefinition.monthLengths != null
            && calendarDefinition.monthLengths.Count > 0
            ? new CalendarDefinition(new List<int>(calendarDefinition.monthLengths), calendarDefinition.daysPerWeek)
            : new CalendarDefinition(calendarDefinition.monthsPerYear,
                calendarDefinition.weeksPerMonth, calendarDefinition.daysPerWeek);
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState(
            new SimulationTime(20L),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            SimulationRuntimeCompositionProfile.Standard, new WorldId(Guid.NewGuid()));
        Assert.That(state.TryBindRuntimeConfiguration(
            graph.Definitions.EffectiveConfiguration, new SimulationCalendar(calendar)), Is.True);
        Assert.That(state.TryBindSelectedRuntimeOwnerGraph(graph), Is.True);
        Assert.That(state.TryBindInitialSelectedRuntimeActorRosterFromOwnerGraph(), Is.True);
        Assert.That(state.TryBindSelectedRuntimeExecutionRoots(
            idAllocator, randomSource, recordSequence, decisionStore, actorChoiceStore), Is.True);
        return state;
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
    public void CoreSelectedActionStageDispatchesWithoutHostActionProjection()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        // This fixture provides the dispatch port directly from Core owners;
        // it has no Unity NpcRuntime or NpcActionRuntime projection.
        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary();
        boundary.Projectionless = true;
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
            "chosen-target",
            "complete-dispatch",
            "status-operation"
        }), "Core owns the action-slot and dispatch result; no host projection callback is required.");
        Assert.That(boundary.Random.Calls, Is.EqualTo(1));
        Assert.That(boundary.Random.LastStreamKey,
            Is.EqualTo("npc-decision|" + actor.RuntimeId + "|12"));
    }

    [Test]
    public void CoreCompletesEffectFreeNormalActionWithoutHostActionProjection()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        DailyV1ActionDefinition normal = graph.Definitions.Actions["DESCANSAR"];
        Assert.That(normal.Values.actionType?.Value,
            Is.EqualTo(DailyV1NpcDefinitionRules.NormalActionTypeValue));
        Assert.That(DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
            graph.Definitions, normal,
            out IReadOnlyList<DailyV1StatusDefinition> actorRemove,
            out IReadOnlyList<DailyV1StatusDefinition> actorAdd,
            out IReadOnlyList<DailyV1StatusDefinition> targetRemove,
            out IReadOnlyList<DailyV1StatusDefinition> targetAdd), Is.True);
        Assert.That(actorRemove, Is.Empty);
        Assert.That(actorAdd, Is.Empty);
        Assert.That(targetRemove, Is.Empty);
        Assert.That(targetAdd, Is.Empty);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            graph.Definitions, normal, null, null, null, 0,
            0f, 1f, NpcTravelReason.None, 0f, "decision/normal-core",
            null, null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, normal, payload, null), Is.True);

        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary
        {
            RejectDispatchBoundary = true
        };
        boundary.BindGraph(graph);
        long actionRevision = actor.ActionSlot.Revision;
        DailyV1StatusDefinition[] statusBefore = actor.Status.Statuses.ToArray();

        Assert.That(DailyV1SelectedActorActionExecutionStage.TryExecuteInstalledAction(
            graph, actor, boundary.RandomSource, 12L, boundary,
            out bool succeeded, out NpcActionResultType? resultType,
            out string resultMessage), Is.True);

        Assert.That(succeeded, Is.True);
        Assert.That(resultType, Is.EqualTo(NpcActionResultType.Success));
        string expectedResultMessage = !string.IsNullOrEmpty(normal.Values.normalActionLogText)
            ? $"{actor.Definition.Values.name} {normal.Values.normalActionLogText}"
            : $"{actor.Definition.Values.name} realizou {normal.Values.actionName ?? string.Empty}.";
        Assert.That(resultMessage, Is.EqualTo(expectedResultMessage));
        Assert.That(boundary.LastReportedResultMessage, Is.EqualTo(expectedResultMessage),
            "The projectionless host reporting port receives the exact Core-formatted Normal result.");
        Assert.That(boundary.DispatchBoundaryRequests, Is.Zero);
        Assert.That(boundary.ActionStatusBoundaryRequests, Is.EqualTo(1));
        Assert.That(boundary.ChosenActionTargetLogCalls, Is.EqualTo(1));
        Assert.That(boundary.Events, Is.EqualTo(new[]
        {
            "chosen-target", "complete-dispatch", "status-operation"
        }));
        Assert.That(boundary.Random.Calls, Is.Zero);
        Assert.That(actor.ActionSlot.Revision, Is.EqualTo(actionRevision));
        Assert.That(actor.Status.Statuses, Is.EqualTo(statusBefore));
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
    public void CoreNormalActionReportUsesActionNameWhenNoLogTextIsAuthored()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        DailyV1SelectedActionDispatchResult dispatch = new DailyV1SelectedActionDispatchResult(
            DailyV1SelectedActionDispatchKind.Normal,
            actionRollSucceeded: true,
            succeeded: true,
            normalActionName: "Ação de teste",
            normalActionLogText: string.Empty);

        Assert.That(DailyV1SelectedActionDispatchStage.TryFormatNormalActionResultMessage(
            actor, dispatch, out string message), Is.True);
        Assert.That(message, Is.EqualTo(
            $"{actor.Definition.Values.name} realizou Ação de teste."));
    }

    [Test]
    public void ProjectionlessSelectedActionClearUsesCoreAdmissionAndRevision()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(out DailyV1NpcRuntimeOwners actor);
        DailyV1ActionDefinition action = graph.Definitions.Actions.Values.First();
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            graph.Definitions, action, null, null, null, 0,
            0f, 1f, NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, payload, null), Is.True);
        long revisionBefore = actor.ActionSlot.Revision;

        CoreSelectedActionBoundary boundary = new CoreSelectedActionBoundary
        {
            Projectionless = true
        };
        boundary.BindGraph(graph);

        Assert.That(DailyV1SelectedActorActionExecutionStage.TryClearCurrentAction(
            graph, actor, boundary), Is.True);
        Assert.That(actor.ActionSlot.CurrentAction, Is.Null);
        Assert.That(actor.ActionSlot.CurrentPayload, Is.Null);
        Assert.That(actor.ActionSlot.Revision, Is.EqualTo(revisionBefore + 1L));
        Assert.That(boundary.Events, Is.EqualTo(new[] { "clear-admission" }));
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
        DailyV1ExecutionBoundaryState executionState = CreateActorTurnExecutionState(
            graph, choices, new RuntimeIdAllocator(), new DeterministicRandomSource(12),
            new SimulationRecordSequence(), new NpcDecisionStore());
        DailyV1ActorTurnCoreRoots coreRoots = new DailyV1ActorTurnCoreRoots(executionState, graph);
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
        boundary.WithdrawSelectedExecutionAfterPreflight = true;
        Assert.That(DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph, new[] { new DailyV1ActorTurnRosterEntry(0, personId, actor) },
            people, choices, directives, 12L, false,
            requireCoreOwnedSelectedOperations: true, coreRoots: coreRoots,
            boundary: boundary), Is.True);

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
        Assert.That(boundary.SelectedExecutionChecks, Is.EqualTo(1),
            "After the admitted graph proves Core actor execution once, directive and reservation handling must not ask Unity whether Core execution remains selected.");
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

    private static DailyV1SelectedRuntimeOwnerGraph CreateFullGraph()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph completeGraph), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            completeGraph.Cities, completeGraph.Npcs, completeGraph.SpatialNetwork,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        return graph;
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

    private static void AssertSpatialTransit(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore people,
        PersonId personId)
    {
        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        HexId from = new HexId("hex.actor-turn.from");
        HexId to = new HexId("hex.actor-turn.to");
        HexBoundaryKey boundary = new HexBoundaryKey(from, to);
        CrossingId crossingId = new CrossingId("crossing.actor-turn.transit");
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("actor-turn.scale", "test", "v1", 1m, "unit"),
            new[]
            {
                new HexRecord(from, new HexCoordinate(0, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain.actor-turn"), "v1")),
                new HexRecord(to, new HexCoordinate(1, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain.actor-turn"), "v1"))
            },
            new[]
            {
                new LocationRecord(new LocationId("location.actor-turn.from"), from),
                new LocationRecord(new LocationId("location.actor-turn.to"), to)
            });
        Assert.That(spatial.TryComposeGeography(geography,
            out SpatialAuthorityFailure geographyFailure), Is.True, geographyFailure?.ToString());
        Assert.That(spatial.TryRegisterCrossing(new CrossingRecord(
            crossingId, boundary, from, "actor-turn-crossing", "v1", null),
            out SpatialAuthorityFailure crossingFailure), Is.True, crossingFailure?.ToString());

        PersonSpatialPositionStore positions = new PersonSpatialPositionStore(
            people, spatial, new SpatialPassageTraversalOptionResolver(spatial.PassageAuthority));
        Assert.That(positions.TrySetAt(personId, StablePositionReference.ForHex(from),
            out PersonSpatialPositionFailure positionFailure), Is.True, positionFailure?.ToString());
        Assert.That(positions.TryBeginTransit(personId, TraversalOptionRef.ForCrossing(crossingId),
            boundary, from, to, out PersonSpatialPositionFailure transitFailure),
            Is.True, transitFailure?.ToString());
        Assert.That(graph.TryBindPersonSpatialPositionStore(positions), Is.True,
            "The Core owner graph must read the position owner tied to its exact PersonStore.");
        Assert.That(graph.IsPersonInSpatialTransit(personId), Is.True);
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
            out IDailyV1ActorChoiceExecutionBoundary executionBoundary)
        {
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
        IDailyV1SelectedActionProjectionBoundary,
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
        internal bool WithdrawSelectedExecutionAfterPreflight;
        internal int SelectedExecutionChecks;
        internal int DecisionOwnerLookupCount;
        internal bool RejectDispatchBoundary;
        internal bool Projectionless;
        internal bool RejectActionStatusBoundary;
        internal int DispatchBoundaryRequests;
        internal int ActionStatusBoundaryRequests;
        internal int ChosenActionTargetLogCalls;
        internal string LastReportedResultMessage;
        internal int SaleAdmissionCount;
        internal int ActionSlotAdmissionCount;
        internal int SaleReceiptCount;
        internal int SaleDisposeCount;
        internal DailyV1SelectedOpenMarketSaleCommitResult LastSaleCommit;
        internal long NextDecisionIdSequence => allocator.CaptureSnapshot().Counters
            .Single(counter => counter.FamilyId == "decision").NextSequence;
        internal long NextRecordSequence => sequence.CaptureSnapshot().NextSequence;

        public IAuthoritativeRandomSource RandomSource => Random;
        public bool CanExecuteWithoutActionProjection => Projectionless;
        public bool UsesSelectedDailyV1ActionExecution(DailyV1NpcRuntimeOwners actor)
        {
            SelectedExecutionChecks++;
            return !WithdrawSelectedExecutionAfterPreflight || SelectedExecutionChecks == 1;
        }
        public void LogSkippedDirective(ScheduledDirective directive, string reason) =>
            Events.Add("skipped:" + reason);

        public bool TryCreateActionProjectionBoundary(
            DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedActionProjectionBoundary projectionBoundary)
        {
            projectionBoundary = Projectionless ? null : this;
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
            DecisionOwnerLookupCount++;
            idAllocator = allocator;
            recordSequence = sequence;
            decisionStore = Decisions;
            return true;
        }

        public bool TryGetActorChoiceExecutionContext(
            DailyV1NpcRuntimeOwners actor,
            out IDailyV1ActorChoiceExecutionBoundary executionBoundary)
        {
            executionBoundary = CurrentGraph?.ContainsExactNpcOwner(actor) == true ? this : null;
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
            IDailyV1SelectedActionProjectionBoundary decisionBoundary,
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
            if (!CurrentGraph.ContainsExactNpcOwner(actor)
                || actor.ActionSlot?.CurrentPayload == null
                || (!Projectionless && (!ReferenceEquals(preparedProjectionActor, actor)
                    || preparedProjectionPayload == null)))
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
            if (!CurrentGraph.ContainsExactNpcOwner(actor)
                || (!Projectionless && (!ReferenceEquals(preparedProjectionActor, actor)
                    || !ReferenceEquals(preparedProjectionPayload, payload))))
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
            IDailyV1SelectedActionProjectionBoundary decisionBoundary,
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

        public void LogChosenActionTarget(DailyV1NpcRuntimeOwners actor)
        {
            ChosenActionTargetLogCalls++;
            Events.Add("chosen-target");
        }
        public bool TryGetDispatchBoundary(
            DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedActionDispatchBoundary dispatchBoundary)
        {
            DispatchBoundaryRequests++;
            dispatchBoundary = RejectDispatchBoundary ? null : this;
            return !RejectDispatchBoundary;
        }

        public bool TryCompleteDispatch(
            DailyV1NpcRuntimeOwners actor,
            DailyV1SelectedActionDispatchResult dispatch,
            out string resultMessage)
        {
            Events.Add("complete-dispatch");
            if (dispatch?.Kind == DailyV1SelectedActionDispatchKind.Normal)
            {
                if (!DailyV1SelectedActionDispatchStage.TryFormatNormalActionResultMessage(
                        actor, dispatch, out resultMessage))
                    return false;
                LastReportedResultMessage = resultMessage;
                return true;
            }
            resultMessage = null;
            return true;
        }

        public bool TryGetActionStatusBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcRuntimeOwners target,
            bool reuseExistingOperation,
            out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary boundary)
        {
            ActionStatusBoundaryRequests++;
            boundary = RejectActionStatusBoundary ? null : this;
            return !RejectActionStatusBoundary;
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

[TestFixture]
public sealed class DailyV1SelectedActionDispatchOutcomeTests
{
    [Test]
    public void CoreOutcomeResolvesSuccessAndOperationFailureForEveryDispatchKind()
    {
        foreach (DailyV1SelectedActionDispatchKind kind
            in Enum.GetValues(typeof(DailyV1SelectedActionDispatchKind)))
        {
            DailyV1SelectedActionDispatchResult succeeded = CreateDispatch(kind, true, true);
            Assert.That(DailyV1SelectedActorActionExecutionStage.TryResolveDispatchOutcome(
                succeeded, out bool actionSucceeded, out NpcActionResultType resultType),
                Is.True, $"A complete successful {kind} dispatch should resolve.");
            Assert.That(actionSucceeded, Is.True, $"{kind} should preserve Core success.");
            Assert.That(resultType, Is.EqualTo(NpcActionResultType.Success),
                $"{kind} should resolve the Core success result type.");

            DailyV1SelectedActionDispatchResult failedOperation = CreateDispatch(kind, true, false);
            Assert.That(DailyV1SelectedActorActionExecutionStage.TryResolveDispatchOutcome(
                failedOperation, out actionSucceeded, out resultType),
                Is.True, $"A complete failed {kind} operation should resolve.");
            Assert.That(actionSucceeded, Is.False,
                $"{kind} should preserve the failed Core operation result.");
            Assert.That(resultType, Is.EqualTo(NpcActionResultType.Failure),
                $"{kind} should resolve the Core failure result type.");

            DailyV1SelectedActionDispatchResult failedRoll = CreateDispatch(kind, false, false);
            Assert.That(DailyV1SelectedActorActionExecutionStage.TryResolveDispatchOutcome(
                failedRoll, out actionSucceeded, out resultType),
                Is.True, $"A failed-roll {kind} dispatch should resolve.");
            Assert.That(actionSucceeded, Is.False,
                $"{kind} must not report action success after a failed roll.");
            Assert.That(resultType, Is.EqualTo(NpcActionResultType.Failure),
                $"{kind} failed roll should resolve the Core failure result type.");
        }
    }

    [Test]
    public void CoreOutcomeRejectsMalformedSuccessAndUnknownDispatchKind()
    {
        var impossibleRollSuccess = new DailyV1SelectedActionDispatchResult(
            DailyV1SelectedActionDispatchKind.Normal, false, true);
        Assert.That(DailyV1SelectedActorActionExecutionStage.TryResolveDispatchOutcome(
            impossibleRollSuccess, out bool actionSucceeded, out _), Is.False);
        Assert.That(actionSucceeded, Is.False);

        var missingDomainResult = new DailyV1SelectedActionDispatchResult(
            DailyV1SelectedActionDispatchKind.FleeCity, true, true);
        Assert.That(DailyV1SelectedActorActionExecutionStage.TryResolveDispatchOutcome(
            missingDomainResult, out actionSucceeded, out _), Is.False);
        Assert.That(actionSucceeded, Is.False);

        var unknownKind = new DailyV1SelectedActionDispatchResult(
            (DailyV1SelectedActionDispatchKind)byte.MaxValue, true, false);
        Assert.That(DailyV1SelectedActorActionExecutionStage.TryResolveDispatchOutcome(
            unknownKind, out actionSucceeded, out _), Is.False);

        foreach (DailyV1SelectedActionDispatchKind kind
            in Enum.GetValues(typeof(DailyV1SelectedActionDispatchKind)))
        {
            if (kind == DailyV1SelectedActionDispatchKind.Normal) continue;
            var missingDetailedResult = new DailyV1SelectedActionDispatchResult(
                kind, true, true);
            Assert.That(DailyV1SelectedActorActionExecutionStage.TryResolveDispatchOutcome(
                missingDetailedResult, out actionSucceeded, out _), Is.False,
                $"A successful {kind} dispatch without its typed Core result must fail closed.");
            Assert.That(actionSucceeded, Is.False);
        }
    }

    [Test]
    public void SelectedActionBoundaryDoesNotSupplyDecisionIdentityRoots()
    {
        Assert.That(
            typeof(IDailyV1SelectedActorActionExecutionBoundary).GetMethod("TryGetDecisionRecordOwners"),
            Is.Null,
            "An admitted selected action obtains identity roots from its Core execution state, not its host projection boundary.");
        Assert.That(
            typeof(IDailyV1ActorChoiceExecutionBoundary).GetMethod("TryGetDecisionRecordOwners"),
            Is.Not.Null,
            "The legacy actor-choice compatibility port retains its existing root lookup.");
    }

    [Test]
    public void HostCompletionContractCannotReturnOrReplaceActionOutcome()
    {
        var method = typeof(IDailyV1SelectedActorActionExecutionBoundary).GetMethod(
            nameof(IDailyV1SelectedActorActionExecutionBoundary.TryCompleteDispatch));
        Assert.That(method, Is.Not.Null);
        var parameters = method.GetParameters();
        int outputCount = 0;
        foreach (var parameter in parameters)
        {
            if (!parameter.IsOut) continue;
            outputCount++;
            Assert.That(parameter.ParameterType, Is.EqualTo(typeof(string).MakeByRefType()),
                "The host may return presentation text, but not Core action success or result type.");
        }
        Assert.That(outputCount, Is.EqualTo(1));
    }

    private static DailyV1SelectedActionDispatchResult CreateDispatch(
        DailyV1SelectedActionDispatchKind kind,
        bool actionRollSucceeded,
        bool succeeded)
    {
        switch (kind)
        {
            case DailyV1SelectedActionDispatchKind.Normal:
                return new DailyV1SelectedActionDispatchResult(kind,
                    actionRollSucceeded, succeeded);
            case DailyV1SelectedActionDispatchKind.PlannedNpcSale:
            {
                var sale = new DailyV1SelectedActionExecutionStage.PlannedNpcSaleResult(
                    succeeded, false, succeeded ? 1 : 0, 1f, false, false, null, null);
                var result = new DailyV1SelectedActionExecutionStage.SelectedPlannedNpcSaleActionResult(
                    succeeded, actionRollSucceeded, sale);
                return new DailyV1SelectedActionDispatchResult(kind,
                    actionRollSucceeded, succeeded, plannedNpcSale: result);
            }
            case DailyV1SelectedActionDispatchKind.PlannedMarketSale:
            {
                var sale = new DailyV1SelectedActionExecutionStage.PlannedMarketSaleResult(
                    succeeded, false, succeeded ? 1 : 0, 1f, false, false, null);
                var result = new DailyV1SelectedActionExecutionStage.SelectedPlannedMarketSaleActionResult(
                    succeeded, actionRollSucceeded, sale);
                return new DailyV1SelectedActionDispatchResult(kind,
                    actionRollSucceeded, succeeded, plannedMarketSale: result);
            }
            case DailyV1SelectedActionDispatchKind.OpenMarket:
                return new DailyV1SelectedActionDispatchResult(kind,
                    actionRollSucceeded, succeeded,
                    openMarket: new DailyV1SelectedActionExecutionStage.SelectedOpenMarketActionResult(
                        succeeded, actionRollSucceeded, null, null));
            case DailyV1SelectedActionDispatchKind.CrossCityBuy:
                return new DailyV1SelectedActionDispatchResult(kind,
                    actionRollSucceeded, succeeded,
                    crossCityBuy: new DailyV1SelectedActionExecutionStage.SelectedCrossCityBuyActionResult(
                        actionRollSucceeded,
                        new DailyV1SelectedCrossCityBuyResult(succeeded,
                            succeeded ? DailyV1SelectedCrossCityBuyFailure.None
                                : DailyV1SelectedCrossCityBuyFailure.InvalidInput)));
            case DailyV1SelectedActionDispatchKind.SoloTravel:
                return new DailyV1SelectedActionDispatchResult(kind,
                    actionRollSucceeded, succeeded,
                    soloTravel: new DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult(
                        succeeded, actionRollSucceeded, null));
            case DailyV1SelectedActionDispatchKind.FleeCity:
                return new DailyV1SelectedActionDispatchResult(kind,
                    actionRollSucceeded, succeeded,
                    fleeCity: new DailyV1SelectedActionExecutionStage.SelectedFleeCityActionResult(
                        succeeded, succeeded ? "destination" : null, 1f));
            case DailyV1SelectedActionDispatchKind.GuardArrest:
                return new DailyV1SelectedActionDispatchResult(kind,
                    actionRollSucceeded, succeeded,
                    guardArrest: new DailyV1SelectedActionExecutionStage.SelectedGuardArrestActionResult(
                        succeeded, "target", "city", "decision"));
            case DailyV1SelectedActionDispatchKind.Steal:
                return new DailyV1SelectedActionDispatchResult(kind,
                    actionRollSucceeded, succeeded,
                    steal: new DailyV1SelectedStealActionExecutionStage.Result(
                        succeeded, "target", "city", 1));
            case DailyV1SelectedActionDispatchKind.Crime:
                return new DailyV1SelectedActionDispatchResult(kind,
                    actionRollSucceeded, succeeded,
                    crime: new DailyV1SelectedCrimeActionExecutionStage.Result(
                        actionRollSucceeded, succeeded));
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}
