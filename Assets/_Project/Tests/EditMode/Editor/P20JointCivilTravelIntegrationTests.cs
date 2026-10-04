using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P20JointCivilTravelIntegrationTests
{
    [TestCase(false, ActivityLifecycleState.Completed, P20JointCivilTravelState.Completed)]
    [TestCase(true, ActivityLifecycleState.Interrupted, P20JointCivilTravelState.Interrupted)]
    public void SecondArrivalAtomicallyTerminatesSharedActivityAfterIndividualTravel(
        bool abortAfterLeg, ActivityLifecycleState expectedLifecycle, P20JointCivilTravelState expectedCoordination)
    {
        Fixture fixture = new Fixture();
        ActivityLifecycleStore lifecycle = new ActivityLifecycleStore("joint-travel-integration-world");
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle,
            calendar, new LogicalTick(0));
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition, fixture.Travel,
            person => fixture.Contexts[person.Value]);
        Assert.That(owner.TryCreate(new ActivityDefinition(P20JointCivilTravelOwner.ActivityDefinitionId, "v1"),
            "joint-integration-" + abortAfterLeg, fixture.Segment.StableKey, new LogicalTick(1), new[] { "person-a", "person-b" },
            out P20JointCivilTravelSnapshot proposal, out ActivityFailure createFailure), Is.True, createFailure.ToString());
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-a", true, "consent-a")), Is.True);
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-b", true, "consent-b")), Is.True);
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, out ActivityFailure scheduleFailure),
            Is.True, scheduleFailure.ToString());
        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(1), out TimelineFailure sealFailure), Is.True,
            sealFailure.ToString());
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(1), out TimelineFailure startFailure),
            Is.True, startFailure.ToString());

        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot active), Is.True);
        Assert.That(active.State, Is.EqualTo(ActivityLifecycleState.Active));
        Assert.That(fixture.Positions.TryGetPosition(new PersonId("person-a"), out PersonSpatialPosition transitA), Is.True);
        Assert.That(fixture.Positions.TryGetPosition(new PersonId("person-b"), out PersonSpatialPosition transitB), Is.True);
        Assert.That(transitA.IsInTransit && transitB.IsInTransit, Is.True);
        Assert.That(fixture.Positions.Revision, Is.EqualTo(3L), "The two departures install one replacement root.");
        Assert.That(fixture.Plans.Revision, Is.EqualTo(3L), "The two route activations install one replacement root.");

        if (abortAfterLeg)
        {
            Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot activeOwner), Is.True);
            Assert.That(owner.TryRequestAbortAfterLeg(proposal.ActivityInstanceId, activeOwner.Revision,
                "abort-input-" + abortAfterLeg), Is.True);
            Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot abortRequested), Is.True);
            Assert.That(abortRequested.AbortCausalInputIdentity, Is.EqualTo("abort-input-" + abortAfterLeg));
            Assert.That(abortRequested.AbortAcceptedAt, Is.EqualTo(new LogicalTick(1)));
            Assert.That(abortRequested.AbortAcceptedOrder, Is.EqualTo(abortRequested.Revision));

            ActivityLifecycleStore restoredLifecycle = lifecycle.Clone();
            ActivityLifecycleComposition restoredComposition = new ActivityLifecycleComposition(
                restoredLifecycle, calendar, new LogicalTick(1));
            P20JointCivilTravelOwner restoredOwner = new P20JointCivilTravelOwner(
                restoredComposition, fixture.Travel, person => fixture.Contexts[person.Value]);
            Assert.That(restoredOwner.TryRestoreOwnerState(owner.SnapshotOwnerState()), Is.True);
            P20JointCivilTravelSnapshot restoredAbort = restoredOwner.SnapshotOwnerState().Single();
            Assert.That(restoredAbort.State, Is.EqualTo(P20JointCivilTravelState.Active));
            Assert.That(restoredAbort.ProposedStart, Is.EqualTo(new LogicalTick(1)));
            Assert.That(restoredAbort.AbortAfterLegRequested, Is.True);
            Assert.That(restoredAbort.AbortCausalInputIdentity, Is.EqualTo(abortRequested.AbortCausalInputIdentity));
            Assert.That(restoredAbort.AbortAcceptedAt, Is.EqualTo(abortRequested.AbortAcceptedAt));
            Assert.That(restoredAbort.AbortAcceptedOrder, Is.EqualTo(abortRequested.AbortAcceptedOrder));
        }

        Assert.That(fixture.Travel.TryAdvanceSegment(new PersonId("person-a"), TraversalProgress.CompleteProgressTicks,
            out P8ETravelFailure travelFailure), Is.True, travelFailure.Message);
        Assert.That(owner.TryArrive(proposal.ActivityInstanceId, "person-a", out ActivityFailure firstArrivalFailure),
            Is.True, firstArrivalFailure.ToString());
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot stillActive), Is.True);
        Assert.That(stillActive.State, Is.EqualTo(ActivityLifecycleState.Active));
        Assert.That(lifecycle.GetCommitment("person-a"), Is.Not.Null);
        Assert.That(lifecycle.GetCommitment("person-b"), Is.Not.Null);

        Assert.That(fixture.Travel.TryAdvanceSegment(new PersonId("person-b"), TraversalProgress.CompleteProgressTicks,
            out travelFailure), Is.True, travelFailure.Message);
        Assert.That(owner.TryArrive(proposal.ActivityInstanceId, "person-b", out ActivityFailure secondArrivalFailure),
            Is.True, secondArrivalFailure.ToString());
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot terminal), Is.True);
        Assert.That(terminal.State, Is.EqualTo(expectedLifecycle));
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot terminalOwner), Is.True);
        Assert.That(terminalOwner.State, Is.EqualTo(expectedCoordination));
        Assert.That(fixture.Positions.TryGetPosition(new PersonId("person-b"), out PersonSpatialPosition arrivedB), Is.True);
        Assert.That(arrivedB.IsInTransit, Is.False);
        Assert.That(arrivedB.Position.HexId, Is.EqualTo(new HexId("hex.b")));
        Assert.That(fixture.Plans.TryGetCurrent(new PersonId("person-b"), out PersonRoutePlan completedPlanB), Is.True);
        Assert.That(completedPlanB.Status, Is.EqualTo(PersonRoutePlanStatus.Completed));
        Assert.That(lifecycle.GetCommitment("person-a"), Is.Null);
        Assert.That(lifecycle.GetCommitment("person-b"), Is.Null);
        long positionRevisionAfterTerminal = fixture.Positions.Revision;
        long planRevisionAfterTerminal = fixture.Plans.Revision;
        int terminalReceiptCount = lifecycle.SnapshotTransitionReceipts()
            .Count(x => x.ActivityInstanceId == proposal.ActivityInstanceId
                && (x.Kind == ActivityTransitionKind.Complete || x.Kind == ActivityTransitionKind.Interrupt));
        Assert.That(owner.TryArrive(proposal.ActivityInstanceId, "person-b", out _), Is.False,
            "A retry after the terminal commit must not apply final arrival twice.");
        Assert.That(fixture.Positions.Revision, Is.EqualTo(positionRevisionAfterTerminal));
        Assert.That(fixture.Plans.Revision, Is.EqualTo(planRevisionAfterTerminal));
        Assert.That(lifecycle.SnapshotTransitionReceipts()
            .Count(x => x.ActivityInstanceId == proposal.ActivityInstanceId
                && (x.Kind == ActivityTransitionKind.Complete || x.Kind == ActivityTransitionKind.Interrupt)),
            Is.EqualTo(terminalReceiptCount));
    }

    [Test]
    public void OwnerRestoreRejectsAbortOrderingThatOverlapsConsentOrLiesInTheFuture()
    {
        Fixture fixture = new Fixture();
        ActivityLifecycleStore lifecycle = new ActivityLifecycleStore("joint-travel-restore-order-world");
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle, calendar, new LogicalTick(0));
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition, fixture.Travel,
            person => fixture.Contexts[person.Value]);
        Assert.That(owner.TryCreate(new ActivityDefinition(P20JointCivilTravelOwner.ActivityDefinitionId, "v1"),
            "joint-restore-order", fixture.Segment.StableKey, new LogicalTick(1), new[] { "person-a", "person-b" },
            out P20JointCivilTravelSnapshot proposal, out ActivityFailure createFailure), Is.True, createFailure.ToString());
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-a", true, "restore-order-consent-a")), Is.True);
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-b", true, "restore-order-consent-b")), Is.True);
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, out ActivityFailure scheduleFailure),
            Is.True, scheduleFailure.ToString());
        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(1), out TimelineFailure sealFailure), Is.True,
            sealFailure.ToString());
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(1), out TimelineFailure advanceFailure), Is.True,
            advanceFailure.ToString());
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot active), Is.True);
        Assert.That(owner.TryRequestAbortAfterLeg(proposal.ActivityInstanceId, active.Revision, "restore-order-abort"), Is.True);
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot requested), Is.True);
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot activeLifecycle), Is.True);

        SortedDictionary<string, P20JointCivilTravelAssent> overlappingAssents =
            new SortedDictionary<string, P20JointCivilTravelAssent>(System.StringComparer.Ordinal);
        foreach (P20JointCivilTravelAssent assent in requested.Assents)
        {
            long order = assent.PersonId == "person-a" ? requested.AbortAcceptedOrder : assent.AcceptedOrder;
            overlappingAssents.Add(assent.PersonId, new P20JointCivilTravelAssent(assent.PersonId,
                assent.Accepted, assent.CausalInputIdentity, assent.ProposedStart.Value,
                assent.AcceptedAt.Value, order));
        }
        P20JointCivilTravel overlappingOperation = new P20JointCivilTravel(requested.ActivityInstanceId,
            requested.SharedSegmentStableKey, requested.ProposedStart, requested.PersonIds, overlappingAssents,
            true, requested.AbortCausalInputIdentity, requested.AbortAcceptedAt,
            requested.AbortAcceptedOrder, requested.Revision, requested.ExpectedLifecycleRevision);
        P20JointCivilTravelSnapshot overlappingSnapshot =
            new P20JointCivilTravelSnapshot(overlappingOperation, activeLifecycle, false);
        ActivityLifecycleStore overlappingLifecycle = lifecycle.Clone();
        P20JointCivilTravelOwner overlappingRestorer = new P20JointCivilTravelOwner(
            new ActivityLifecycleComposition(overlappingLifecycle, calendar, new LogicalTick(1)), fixture.Travel,
            person => fixture.Contexts[person.Value]);
        Assert.That(overlappingRestorer.TryRestoreOwnerState(new[] { overlappingSnapshot }), Is.False,
            "Abort order must follow every assent and use the next coordination revision.");

        SortedDictionary<string, P20JointCivilTravelAssent> futureAssents =
            new SortedDictionary<string, P20JointCivilTravelAssent>(System.StringComparer.Ordinal);
        foreach (P20JointCivilTravelAssent assent in requested.Assents)
            futureAssents.Add(assent.PersonId, new P20JointCivilTravelAssent(assent.PersonId, assent.Accepted,
                assent.CausalInputIdentity, assent.ProposedStart.Value, assent.AcceptedAt.Value, assent.AcceptedOrder));
        P20JointCivilTravel futureOperation = new P20JointCivilTravel(requested.ActivityInstanceId,
            requested.SharedSegmentStableKey, requested.ProposedStart, requested.PersonIds, futureAssents,
            true, requested.AbortCausalInputIdentity, new LogicalTick(2),
            requested.AbortAcceptedOrder, requested.Revision, requested.ExpectedLifecycleRevision);
        P20JointCivilTravelSnapshot futureSnapshot = new P20JointCivilTravelSnapshot(futureOperation, activeLifecycle, false);
        ActivityLifecycleStore futureLifecycle = lifecycle.Clone();
        P20JointCivilTravelOwner futureRestorer = new P20JointCivilTravelOwner(
            new ActivityLifecycleComposition(futureLifecycle, calendar, new LogicalTick(1)), fixture.Travel,
            person => fixture.Contexts[person.Value]);
        Assert.That(futureRestorer.TryRestoreOwnerState(new[] { futureSnapshot }), Is.False,
            "Reconstruction must not install an abort fact later than the restored logical instant.");
    }

    [Test]
    public void StalePreparedJointStartCannotInstallEitherParticipantsTravelRoot()
    {
        Fixture fixture = new Fixture();
        P8EJointTravelParticipant[] participants =
        {
            new P8EJointTravelParticipant(new PersonId("person-a"), fixture.Contexts["person-a"]),
            new P8EJointTravelParticipant(new PersonId("person-b"), fixture.Contexts["person-b"])
        };
        Assert.That(fixture.Travel.TryPrepareJointCivilLeg(participants, fixture.Segment.StableKey,
            out PreparedP8EJointCivilLeg prepared, out P8ETravelFailure prepareFailure), Is.True, prepareFailure.Message);
        Assert.That(prepared.CanInstall, Is.True);

        Assert.That(fixture.Positions.TryBeginTransit(new PersonId("person-a"), fixture.Segment.Option,
            fixture.Segment.Boundary, new HexId("hex.a"), new HexId("hex.b"),
            out PersonSpatialPositionFailure mutateFailure), Is.True, mutateFailure.ToString());
        Assert.That(prepared.CanInstall, Is.False);
        Assert.That(fixture.Positions.TryGetPosition(new PersonId("person-a"), out PersonSpatialPosition positionA), Is.True);
        Assert.That(fixture.Positions.TryGetPosition(new PersonId("person-b"), out PersonSpatialPosition positionB), Is.True);
        Assert.That(positionA.IsInTransit, Is.True, "The intervening valid mutation remains authoritative after the stale prepared write is rejected.");
        Assert.That(positionB.IsInTransit, Is.False);
        Assert.That(fixture.Plans.TryGetCurrent(new PersonId("person-a"), out PersonRoutePlan planA), Is.True);
        Assert.That(fixture.Plans.TryGetCurrent(new PersonId("person-b"), out PersonRoutePlan planB), Is.True);
        Assert.That(planA.Status, Is.EqualTo(PersonRoutePlanStatus.Accepted));
        Assert.That(planB.Status, Is.EqualTo(PersonRoutePlanStatus.Accepted));
    }

    [Test]
    public void ChangedCurrentPassageFailsBothParticipantsBeforeJointStart()
    {
        Fixture fixture = new Fixture();
        ActivityLifecycleStore lifecycle = new ActivityLifecycleStore("joint-travel-passage-world");
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle, calendar, new LogicalTick(0));
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition, fixture.Travel,
            person => fixture.Contexts[person.Value]);
        Assert.That(owner.TryCreate(new ActivityDefinition(P20JointCivilTravelOwner.ActivityDefinitionId, "v1"),
            "joint-closed-current-truth", fixture.Segment.StableKey, new LogicalTick(1), new[] { "person-a", "person-b" },
            out P20JointCivilTravelSnapshot proposal, out ActivityFailure createFailure), Is.True, createFailure.ToString());
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-a", true, "passage-consent-a")), Is.True);
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-b", true, "passage-consent-b")), Is.True);
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, out ActivityFailure scheduleFailure),
            Is.True, scheduleFailure.ToString());
        long positionRevision = fixture.Positions.Revision;
        long planRevision = fixture.Plans.Revision;
        Assert.That(fixture.Spatial.PassageAuthority.TryChangePassageCondition(fixture.Segment.Boundary,
            fixture.Segment.Option, PassageCondition.Closed, out SpatialAuthorityFailure passageFailure), Is.True,
            passageFailure.ToString());

        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(1), out TimelineFailure sealFailure), Is.True,
            sealFailure.ToString());
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(1), out TimelineFailure advanceFailure), Is.True,
            advanceFailure.ToString());
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot terminal), Is.True);
        Assert.That(terminal.State, Is.EqualTo(ActivityLifecycleState.Cancelled));
        Assert.That(lifecycle.GetCommitment("person-a"), Is.Null);
        Assert.That(lifecycle.GetCommitment("person-b"), Is.Null);
        Assert.That(fixture.Positions.Revision, Is.EqualTo(positionRevision));
        Assert.That(fixture.Plans.Revision, Is.EqualTo(planRevision));
        Assert.That(fixture.Positions.TryGetPosition(new PersonId("person-a"), out PersonSpatialPosition positionA), Is.True);
        Assert.That(fixture.Positions.TryGetPosition(new PersonId("person-b"), out PersonSpatialPosition positionB), Is.True);
        Assert.That(positionA.IsInTransit, Is.False);
        Assert.That(positionB.IsInTransit, Is.False);
    }

    [Test]
    public void StalePreparedTerminalArrivalCannotCommitASecondOwnerRoot()
    {
        Fixture fixture = new Fixture();
        ActivityLifecycleStore lifecycle = new ActivityLifecycleStore("joint-travel-terminal-stale-world");
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle, calendar, new LogicalTick(0));
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition, fixture.Travel,
            person => fixture.Contexts[person.Value]);
        Assert.That(owner.TryCreate(new ActivityDefinition(P20JointCivilTravelOwner.ActivityDefinitionId, "v1"),
            "joint-stale-terminal", fixture.Segment.StableKey, new LogicalTick(1), new[] { "person-a", "person-b" },
            out P20JointCivilTravelSnapshot proposal, out ActivityFailure createFailure), Is.True, createFailure.ToString());
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-a", true, "terminal-consent-a")), Is.True);
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-b", true, "terminal-consent-b")), Is.True);
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, out ActivityFailure scheduleFailure),
            Is.True, scheduleFailure.ToString());
        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(1), out TimelineFailure sealFailure), Is.True,
            sealFailure.ToString());
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(1), out TimelineFailure startFailure), Is.True,
            startFailure.ToString());
        foreach (string person in new[] { "person-a", "person-b" })
            Assert.That(fixture.Travel.TryAdvanceSegment(new PersonId(person), TraversalProgress.CompleteProgressTicks,
                out P8ETravelFailure progressFailure), Is.True, progressFailure.Message);

        Assert.That(fixture.Travel.TryPrepareFinalArrival(new PersonId("person-b"),
            out PreparedP8EPersonFinalArrival prepared, out P8ETravelFailure arrivalFailure), Is.True,
            arrivalFailure.Message);
        long planRevisionBeforeStaleCommit = fixture.Plans.Revision;
        Assert.That(fixture.Positions.TryArrive(new PersonId("person-b"),
            StablePositionReference.ForHex(new HexId("hex.b")), out PersonSpatialPositionFailure mutateFailure), Is.True,
            mutateFailure.ToString());
        Assert.That(prepared.CanInstall, Is.False);
        Assert.That(fixture.Positions.TryGetPosition(new PersonId("person-b"), out PersonSpatialPosition arrivedB), Is.True);
        Assert.That(arrivedB.IsInTransit, Is.False);
        Assert.That(arrivedB.Position.HexId, Is.EqualTo(new HexId("hex.b")));
        Assert.That(fixture.Plans.Revision, Is.EqualTo(planRevisionBeforeStaleCommit));
        Assert.That(fixture.Plans.TryGetCurrent(new PersonId("person-b"), out PersonRoutePlan planB), Is.True);
        Assert.That(planB.Status, Is.EqualTo(PersonRoutePlanStatus.Active));
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot stillActive), Is.True);
        Assert.That(stillActive.State, Is.EqualTo(ActivityLifecycleState.Active));
    }

    private sealed class Fixture
    {
        private const string Metric = "joint.route.preference";
        private const string Unit = "fixture-units";
        internal readonly PersonSpatialPositionStore Positions;
        internal readonly PersonRoutePlanStore Plans;
        internal readonly SpatialAuthorityStore Spatial;
        internal readonly P8ETravelTransactionCoordinator Travel;
        internal readonly SpatialRouteSegment Segment;
        internal readonly Dictionary<string, TraversalCostContext> Contexts = new Dictionary<string, TraversalCostContext>
        {
            ["person-a"] = new TraversalCostContext("movement.a", "v1", 1m, 1m, 1m),
            ["person-b"] = new TraversalCostContext("movement.b", "v1", 1m, 1m, 1m)
        };

        internal Fixture()
        {
            PersonStore people = new PersonStore();
            foreach (string id in new[] { "person-a", "person-b" })
                Assert.That(people.TryRegister(new PersonRuntime(new PersonId(id)), out PersonStoreFailure personFailure),
                    Is.True, personFailure.ToString());
            SpatialAuthorityStore spatial = BuildSpatial();
            Spatial = spatial;
            HexId from = new HexId("hex.a"), to = new HexId("hex.b");
            Segment = new SpatialRouteSegment(new HexBoundaryKey(from, to), from, to,
                TraversalOptionRef.ForConnection(new ConnectionId("connection.ab")));
            SpatialRouteKnowledgeStore knowledge = new SpatialRouteKnowledgeStore(people);
            SpatialRoutePlanningSystem planner = new SpatialRoutePlanningSystem(people, spatial, knowledge);
            Plans = new PersonRoutePlanStore(people, knowledge);

            foreach (string id in new[] { "person-a", "person-b" })
            {
                PersonId person = new PersonId(id);
                Record(knowledge, person, new SpatialObservation(SpatialSubject.ForTraversalOption(Segment),
                    SpatialObservationValue.ForRouteOptionBelief(SpatialRouteOptionBelief.KnownAvailable),
                    new SpatialObservationProvenance(SpatialObservationSourceKind.ExternalReport, "joint-map", "option." + id),
                    0L, 0L, 1000, "joint-route-report-v1"));
                SpatialRoutePlanningRequest request = new SpatialRoutePlanningRequest(person,
                    StablePositionReference.ForHex(from), StablePositionReference.ForHex(to), 0L);
                SpatialRouteCandidate candidate = planner.BuildKnownCandidates(request).Candidates[0];
                Record(knowledge, person, new SpatialObservation(SpatialSubject.ForRouteEstimate(candidate.Id, Metric),
                    SpatialObservationValue.ForEstimate(1m, Unit),
                    new SpatialObservationProvenance(SpatialObservationSourceKind.InitialScenarioKnowledge, "joint-world", "estimate." + id),
                    0L, 0L, 1000, "joint-route-estimate-v1"));
                SpatialRoutePlanningOutcome selected = planner.SelectKnownRoute(request,
                    new SpatialRouteSelectionPolicy("joint-policy", "v1", Metric, Unit, false, 0L, true));
                Assert.That(selected.IsSuccess, Is.True, selected.FailureMessage);
                Assert.That(Plans.TryAcceptPlan(selected, "route." + id, 0L, 0L, out PersonRoutePlanFailure planFailure),
                    Is.True, planFailure.ToString());
            }

            Positions = new PersonSpatialPositionStore(people, spatial, new Resolver(spatial.PassageAuthority));
            foreach (string id in new[] { "person-a", "person-b" })
                Assert.That(Positions.TrySetAt(new PersonId(id), StablePositionReference.ForHex(from),
                    out PersonSpatialPositionFailure positionFailure), Is.True, positionFailure.ToString());
            Travel = new P8ETravelTransactionCoordinator(Positions, Plans, knowledge,
                spatial.PassageAuthority, () => 0L);
        }

        private static SpatialAuthorityStore BuildSpatial()
        {
            SpatialAuthorityStore spatial = new SpatialAuthorityStore();
            HexRecord[] hexes =
            {
                new HexRecord(new HexId("hex.a"), new HexCoordinate(0, 0), new TerrainReference(new TerrainDefinitionId("terrain.fixture"), "v1")),
                new HexRecord(new HexId("hex.b"), new HexCoordinate(1, 0), new TerrainReference(new TerrainDefinitionId("terrain.fixture"), "v1"))
            };
            SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
                new SpatialWorldScaleContext("joint-scale", "fixture", "v1", 1m, "hex-step"), hexes, Array.Empty<LocationRecord>());
            Assert.That(spatial.TryComposeGeography(geography, out SpatialAuthorityFailure failure), Is.True, failure.ToString());
            HexId from = new HexId("hex.a"), to = new HexId("hex.b");
            Assert.That(spatial.PassageAuthority.TryRegisterConnection(new PassageOptionRecord(
                TraversalOptionRef.ForConnection(new ConnectionId("connection.ab")), new HexBoundaryKey(from, to),
                "joint-connection", "v1"), PassageCondition.Available, out failure), Is.True, failure.ToString());
            return spatial;
        }

        private static void Record(SpatialRouteKnowledgeStore knowledge, PersonId person, SpatialObservation observation)
        { Assert.That(knowledge.TryRecordObservation(person, observation, 0L, out SpatialKnowledgeFailure failure), Is.True, failure.ToString()); }
    }

    private sealed class Resolver : ISpatialTraversalOptionResolver
    {
        private readonly SpatialPassageAuthority passage;
        internal Resolver(SpatialPassageAuthority passage) { this.passage = passage; }
        public bool TryResolveTraversalOption(TraversalOptionRef option, HexBoundaryKey boundary, out string failure)
        {
            if (passage.TryGetTraversalOptions(boundary, out IReadOnlyList<TraversalOptionRef> options,
                out SpatialAuthorityFailure authorityFailure))
            {
                foreach (TraversalOptionRef candidate in options)
                    if (candidate.Equals(option)) { failure = string.Empty; return true; }
                failure = "Traversal option is not registered for the boundary.";
                return false;
            }
            failure = authorityFailure?.Message ?? "Boundary traversal options could not be read.";
            return false;
        }
    }
}
