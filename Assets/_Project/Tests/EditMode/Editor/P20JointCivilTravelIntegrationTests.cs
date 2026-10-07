using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P20JointCivilTravelIntegrationTests
{
    [Test]
    public void DailyProfileExplicitlyAcceptsEmptyP20OwnerInventory()
    {
        Fixture fixture = new Fixture();
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(
            new ActivityLifecycleStore("p20-daily-empty-world"),
            new SimulationCalendar(new CalendarDefinition(2, 2, 3)),
            new LogicalTick(0));
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition, fixture.Travel,
            person => fixture.Contexts[person.Value]);
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            p20JointCivilTravelOwner: owner);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.True,
            failure.ToString());
    }

    [Test]
    public void DailyProfileRejectsNonemptyP20JointTravelOwnerInventory()
    {
        Fixture fixture = new Fixture();
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(
            new ActivityLifecycleStore("p20-daily-nonempty-world"),
            new SimulationCalendar(new CalendarDefinition(2, 2, 3)),
            new LogicalTick(0));
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition, fixture.Travel,
            person => fixture.Contexts[person.Value]);
        Assert.That(owner.TryCreate(new ActivityDefinition(P20JointCivilTravelOwner.ActivityDefinitionId, "v1"),
            "p20-daily-nonempty-proposal", fixture.Segment.StableKey, new LogicalTick(1),
            new[] { "person-a", "person-b" }, out _, out ActivityFailure createFailure), Is.True,
            createFailure.ToString());
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            new SimulationRuntime(new SimulationTime(), null, null,
                runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                p20JointCivilTravelOwner: owner));

        Assert.That(failure.Message, Does.Contain("could not bind"));
    }

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
    public void FailedStartSynchronizesP20RevisionAndRestoresWithoutStartingAgain()
    {
        Fixture fixture = new Fixture();
        ActivityLifecycleStore lifecycle = new ActivityLifecycleStore("joint-travel-failed-start-world");
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle, calendar, new LogicalTick(0));
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition, fixture.Travel,
            person => fixture.Contexts[person.Value]);
        Assert.That(owner.TryCreate(new ActivityDefinition(P20JointCivilTravelOwner.ActivityDefinitionId, "v1"),
            "joint-failed-start-reconstruct", fixture.Segment.StableKey, new LogicalTick(1),
            new[] { "person-a", "person-b" }, out P20JointCivilTravelSnapshot proposal, out ActivityFailure createFailure),
            Is.True, createFailure.ToString());
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-a", true, "failed-start-consent-a")), Is.True);
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-b", true, "failed-start-consent-b")), Is.True);
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, out ActivityFailure scheduleFailure),
            Is.True, scheduleFailure.ToString());
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot scheduledOwner), Is.True);
        long nextReceiptSequence = lifecycle.NextTransitionSequence;
        long positionRevision = fixture.Positions.Revision;
        long planRevision = fixture.Plans.Revision;
        Assert.That(fixture.Spatial.PassageAuthority.TryChangePassageCondition(fixture.Segment.Boundary,
            fixture.Segment.Option, PassageCondition.Closed, out SpatialAuthorityFailure passageFailure), Is.True,
            passageFailure.ToString());

        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(1), out TimelineFailure sealFailure),
            Is.True, sealFailure.ToString());
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(1), out TimelineFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot failedLifecycle), Is.True);
        Assert.That(failedLifecycle.State, Is.EqualTo(ActivityLifecycleState.Cancelled));
        Assert.That(failedLifecycle.Revision, Is.EqualTo(scheduledOwner.ExpectedLifecycleRevision + 1L));
        Assert.That(failedLifecycle.TerminalInstant, Is.EqualTo(new LogicalTick(1)));
        Assert.That(failedLifecycle.Disposition, Is.EqualTo("joint-travel-precondition-failed"));
        Assert.That(lifecycle.GetCommitment("person-a"), Is.Null);
        Assert.That(lifecycle.GetCommitment("person-b"), Is.Null);
        Assert.That(lifecycle.PendingWork, Is.Empty);
        ActivityTransitionReceipt[] failedReceipts = lifecycle.SnapshotTransitionReceipts()
            .Where(receipt => receipt.ActivityInstanceId == proposal.ActivityInstanceId
                && receipt.Kind == ActivityTransitionKind.FailedStart).ToArray();
        Assert.That(failedReceipts, Has.Length.EqualTo(1));
        Assert.That(failedReceipts[0].Sequence, Is.EqualTo(nextReceiptSequence));
        Assert.That(failedReceipts[0].ActivityRevision, Is.EqualTo(failedLifecycle.Revision));
        Assert.That(failedReceipts[0].Instant, Is.EqualTo(new LogicalTick(1)));
        Assert.That(failedReceipts[0].Disposition, Is.EqualTo("joint-travel-precondition-failed"));

        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot failedOwner), Is.True);
        Assert.That(failedOwner.State, Is.EqualTo(P20JointCivilTravelState.FailedToStart));
        Assert.That(failedOwner.ExpectedLifecycleRevision, Is.EqualTo(failedLifecycle.Revision));
        Assert.That(failedOwner.Revision, Is.EqualTo(scheduledOwner.Revision),
            "A failed start must not create a new consent or coordination order.");
        Assert.That(fixture.Positions.Revision, Is.EqualTo(positionRevision));
        Assert.That(fixture.Plans.Revision, Is.EqualTo(planRevision));

        ActivityLifecycleStore restoredLifecycle = lifecycle.Clone();
        ActivityLifecycleComposition restoredComposition = new ActivityLifecycleComposition(
            restoredLifecycle, calendar, new LogicalTick(1));
        P20JointCivilTravelOwner restoredOwner = new P20JointCivilTravelOwner(restoredComposition, fixture.Travel,
            person => fixture.Contexts[person.Value]);
        IReadOnlyList<P20JointCivilTravelSnapshot> savedOwnerState = owner.SnapshotOwnerState();
        Assert.That(restoredOwner.TryRestoreOwnerState(savedOwnerState), Is.True,
            "A committed FailedStart must retain the strict P18/P20 lifecycle token equality needed for reconstruction.");
        Assert.That(restoredOwner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot restored), Is.True);
        Assert.That(restored.State, Is.EqualTo(P20JointCivilTravelState.FailedToStart));
        Assert.That(restored.ExpectedLifecycleRevision, Is.EqualTo(failedLifecycle.Revision));

        Assert.That(restoredComposition.Timeline.TrySealInputsThrough(new LogicalTick(2), out sealFailure),
            Is.True, sealFailure.ToString());
        Assert.That(restoredComposition.Timeline.TryAdvanceTo(new LogicalTick(2), out advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(restoredLifecycle.PendingWork, Is.Empty);
        Assert.That(restoredLifecycle.SnapshotTransitionReceipts()
            .Count(receipt => receipt.ActivityInstanceId == proposal.ActivityInstanceId
                && receipt.Kind == ActivityTransitionKind.Start), Is.EqualTo(0));
        Assert.That(restoredLifecycle.SnapshotTransitionReceipts()
            .Count(receipt => receipt.ActivityInstanceId == proposal.ActivityInstanceId
                && receipt.Kind == ActivityTransitionKind.FailedStart), Is.EqualTo(1));
        Assert.That(fixture.Positions.Revision, Is.EqualTo(positionRevision));
        Assert.That(fixture.Plans.Revision, Is.EqualTo(planRevision));

        Assert.That(restoredLifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot restoredTerminal), Is.True);
        P20JointCivilTravelSnapshot saved = savedOwnerState.Single();
        SortedDictionary<string, P20JointCivilTravelAssent> assents =
            new SortedDictionary<string, P20JointCivilTravelAssent>(StringComparer.Ordinal);
        foreach (P20JointCivilTravelAssent assent in saved.Assents) assents.Add(assent.PersonId, assent);
        P20JointCivilTravel staleOperation = new P20JointCivilTravel(saved.ActivityInstanceId,
            saved.SharedSegmentStableKey, saved.ProposedStart, saved.PersonIds, assents,
            saved.AbortAfterLegRequested, saved.AbortCausalInputIdentity, saved.AbortAcceptedAt,
            saved.AbortAcceptedOrder, saved.Revision, saved.ExpectedLifecycleRevision - 1L);
        P20JointCivilTravelSnapshot staleSnapshot = new P20JointCivilTravelSnapshot(
            staleOperation, restoredTerminal, failedStart: true);
        P20JointCivilTravelOwner staleRestorer = new P20JointCivilTravelOwner(
            new ActivityLifecycleComposition(restoredLifecycle.Clone(), calendar, new LogicalTick(1)), fixture.Travel,
            person => fixture.Contexts[person.Value]);
        Assert.That(staleRestorer.TryRestoreOwnerState(new[] { staleSnapshot }), Is.False,
            "Restore must continue to reject a stale lifecycle token.");
    }

    [Test]
    public void P18StartValidatorRejectionCommitsFailedStartWithoutInstallingP8Travel()
    {
        Fixture fixture = new Fixture();
        ActivityLifecycleStore lifecycle = new ActivityLifecycleStore("joint-travel-validator-failure-world",
            startValidator: new RejectingStartValidator());
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle, calendar, new LogicalTick(0));
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition, fixture.Travel,
            person => fixture.Contexts[person.Value]);
        Assert.That(owner.TryCreate(new ActivityDefinition(P20JointCivilTravelOwner.ActivityDefinitionId, "v1"),
            "joint-validator-failed-start", fixture.Segment.StableKey, new LogicalTick(1),
            new[] { "person-a", "person-b" }, out P20JointCivilTravelSnapshot proposal, out ActivityFailure createFailure),
            Is.True, createFailure.ToString());
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-a", true, "validator-consent-a")), Is.True);
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-b", true, "validator-consent-b")), Is.True);
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, out ActivityFailure scheduleFailure),
            Is.True, scheduleFailure.ToString());
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot scheduledOwner), Is.True);
        long positionRevision = fixture.Positions.Revision;
        long planRevision = fixture.Plans.Revision;

        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(1), out TimelineFailure sealFailure),
            Is.True, sealFailure.ToString());
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(1), out TimelineFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot failedLifecycle), Is.True);
        Assert.That(failedLifecycle.State, Is.EqualTo(ActivityLifecycleState.Cancelled));
        Assert.That(failedLifecycle.Disposition, Is.EqualTo("fixture-start-rejected"));
        Assert.That(lifecycle.SnapshotTransitionReceipts().Any(receipt =>
            receipt.ActivityInstanceId == proposal.ActivityInstanceId
            && receipt.Kind == ActivityTransitionKind.FailedStart
            && receipt.Instant == new LogicalTick(1)
            && receipt.ActivityRevision == failedLifecycle.Revision), Is.True);
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot failedOwner), Is.True);
        Assert.That(failedOwner.State, Is.EqualTo(P20JointCivilTravelState.FailedToStart));
        Assert.That(failedOwner.ExpectedLifecycleRevision, Is.EqualTo(failedLifecycle.Revision));
        Assert.That(failedOwner.Revision, Is.EqualTo(scheduledOwner.Revision));
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
    public void MissingP20CoordinationStateDoesNotConsumeOrTerminalizeScheduledStart()
    {
        ActivityLifecycleStore lifecycle = new ActivityLifecycleStore("joint-travel-missing-owner-world");
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle, calendar, new LogicalTick(0));
        Assert.That(lifecycle.TryPropose(new ActivityDefinition(P20JointCivilTravelOwner.ActivityDefinitionId, "v1"),
            "missing-p20-owner", out ActivityInstanceSnapshot proposed, out ActivityFailure proposalFailure),
            Is.True, proposalFailure.ToString());
        Assert.That(lifecycle.TrySchedule(composition.Timeline, proposed.Id, new LogicalTick(0),
            new LogicalTick(1), null, new[] { "person-a", "person-b" }, out ActivityFailure scheduleFailure),
            Is.True, scheduleFailure.ToString());
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition,
            new Fixture().Travel, _ => new TraversalCostContext("movement", "v1", 1m, 1m, 1m));
        DueWorkReference scheduledStart = lifecycle.PendingWork.Single();
        int receiptCount = lifecycle.SnapshotTransitionReceipts().Count;

        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(1), out TimelineFailure sealFailure),
            Is.True, sealFailure.ToString());
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(1), out _), Is.False,
            "Missing required P20 state must leave the start due for repair/retry.");
        Assert.That(lifecycle.TryGet(proposed.Id, out ActivityInstanceSnapshot stillScheduled), Is.True);
        Assert.That(stillScheduled.State, Is.EqualTo(ActivityLifecycleState.Scheduled));
        Assert.That(stillScheduled.Revision, Is.EqualTo(proposed.Revision + 1L));
        Assert.That(lifecycle.PendingWork, Has.Count.EqualTo(1));
        Assert.That(lifecycle.PendingWork[0], Is.SameAs(scheduledStart));
        Assert.That(lifecycle.GetCommitment("person-a"), Is.Not.Null);
        Assert.That(lifecycle.GetCommitment("person-b"), Is.Not.Null);
        Assert.That(lifecycle.SnapshotTransitionReceipts().Count, Is.EqualTo(receiptCount));
        Assert.That(lifecycle.SnapshotTransitionReceipts().Any(receipt =>
            receipt.ActivityInstanceId == proposed.Id && receipt.Kind == ActivityTransitionKind.FailedStart), Is.False);
        Assert.That(owner.InstanceCount, Is.EqualTo(0));
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

    private sealed class RejectingStartValidator : IActivityStartValidator
    {
        public bool TryValidate(ActivityInstanceSnapshot instance, LogicalTick instant, out string disposition)
        { disposition = "fixture-start-rejected"; return false; }
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
