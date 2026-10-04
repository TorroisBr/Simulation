using NUnit.Framework;

public sealed class P20JointCivilTravelFormationTests
{
    private sealed class SyntheticRules : IP20SyntheticOperationRules
    {
        public bool IsCurrentlyEligible(string personId, ActivityInstanceSnapshot activity, LogicalTick instant, out string revision)
        { revision = "eligible-v1"; return true; }
        public bool TryPrepareResult(string personId, ActivityInstanceSnapshot activity, LogicalTick instant,
            out string result, out string revision)
        { result = "fixture-result"; revision = "result-v1"; return true; }
    }

    private static SimulationCalendar Calendar() => new SimulationCalendar(new CalendarDefinition(2, 2, 3));
    private static ActivityDefinition Definition() =>
        new ActivityDefinition(P20JointCivilTravelOwner.ActivityDefinitionId, "v1");

    private static P20JointCivilTravelOwner CreateOwner(out ActivityLifecycleStore lifecycle)
    {
        return CreateOwner(out lifecycle, out _);
    }

    private static P20JointCivilTravelOwner CreateOwner(out ActivityLifecycleStore lifecycle,
        out ActivityLifecycleComposition composition)
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null, economyEnabled: false);
        lifecycle = new ActivityLifecycleStore("joint-travel-world");
        composition = new ActivityLifecycleComposition(lifecycle, Calendar(), new LogicalTick(0));
        return new P20JointCivilTravelOwner(composition, runtime.P8ETravelTransactionCoordinator,
            _ => null);
    }

    [Test]
    public void FormationNeedsIndependentAssentFromBothPeopleBeforeP18Reservation()
    {
        P20JointCivilTravelOwner owner = CreateOwner(out ActivityLifecycleStore lifecycle);
        Assert.That(owner.TryCreate(Definition(), "joint-1", "shared-segment", new[] { "person-b", "person-a" },
            out P20JointCivilTravelSnapshot proposal, out _), Is.True);
        Assert.That(proposal.PersonIds, Is.EqualTo(new[] { "person-a", "person-b" }));

        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-a", true, "input-a")), Is.True);
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot afterFirstAssent), Is.True);
        Assert.That(afterFirstAssent.Assents[0].AcceptedAt, Is.EqualTo(new LogicalTick(0)));
        Assert.That(afterFirstAssent.Assents[0].AcceptedOrder, Is.EqualTo(1L));
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-b", true, "input-a")), Is.False);
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot afterDuplicateInput), Is.True);
        Assert.That(afterDuplicateInput.Revision, Is.EqualTo(afterFirstAssent.Revision));
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, new LogicalTick(1), out _), Is.False);
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot stillProposed), Is.True);
        Assert.That(stillProposed.State, Is.EqualTo(ActivityLifecycleState.Proposed));
        Assert.That(lifecycle.GetCommitment("person-a"), Is.Null);

        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-b", true, "input-b")), Is.True);
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot afterSecondAssent), Is.True);
        Assert.That(afterSecondAssent.Assents[1].AcceptedAt, Is.EqualTo(new LogicalTick(0)));
        Assert.That(afterSecondAssent.Assents[1].AcceptedOrder, Is.EqualTo(2L));
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, new LogicalTick(1), out ActivityFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(ActivityFailure.None));
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot scheduled), Is.True);
        Assert.That(scheduled.State, Is.EqualTo(ActivityLifecycleState.Scheduled));
        Assert.That(scheduled.Participants, Is.EqualTo(new[] { "person-a", "person-b" }));
        Assert.That(scheduled.PlannedEnd, Is.Null);
        Assert.That(lifecycle.GetCommitment("person-a").ActivityInstanceId, Is.EqualTo(proposal.ActivityInstanceId));
        Assert.That(lifecycle.GetCommitment("person-b").ActivityInstanceId, Is.EqualTo(proposal.ActivityInstanceId));
        Assert.That(lifecycle.PendingWork, Has.Count.EqualTo(1));
    }

    [Test]
    public void DeclineIsRecordedIndividuallyAndNeverReservesEitherPerson()
    {
        P20JointCivilTravelOwner owner = CreateOwner(out ActivityLifecycleStore lifecycle);
        owner.TryCreate(Definition(), "joint-decline", "shared-segment", new[] { "person-a", "person-b" },
            out P20JointCivilTravelSnapshot proposal, out _);
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-a", true, "input-a")), Is.True);
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-b", false, "input-b")), Is.True);
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, new LogicalTick(1), out _), Is.False);
        Assert.That(lifecycle.GetCommitment("person-a"), Is.Null);
        Assert.That(lifecycle.GetCommitment("person-b"), Is.Null);
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot current), Is.True);
        Assert.That(current.Assents, Has.Count.EqualTo(2));
        Assert.That(current.Assents[1].PersonId, Is.EqualTo("person-b"));
        Assert.That(current.Assents[1].Accepted, Is.False);
        Assert.That(current.State, Is.EqualTo(P20JointCivilTravelState.NotFormed));
        Assert.That(current.Disposition, Is.EqualTo("participant-declined"));
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-a", true, "late-acceptance")), Is.False);
    }

    [Test]
    public void CancellationBeforeStartReleasesBothP18Commitments()
    {
        P20JointCivilTravelOwner owner = CreateOwner(out ActivityLifecycleStore lifecycle,
            out ActivityLifecycleComposition composition);
        owner.TryCreate(Definition(), "joint-cancel", "shared-segment", new[] { "person-a", "person-b" },
            out P20JointCivilTravelSnapshot proposal, out _);
        owner.TryRecordAssent(proposal.ActivityInstanceId, new P20JointCivilTravelAssent("person-a", true, "input-a"));
        owner.TryRecordAssent(proposal.ActivityInstanceId, new P20JointCivilTravelAssent("person-b", true, "input-b"));
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, new LogicalTick(2), out _), Is.True);
        Assert.That(owner.TryCancel(proposal.ActivityInstanceId, "user-cancelled", out ActivityFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(ActivityFailure.None));
        Assert.That(lifecycle.GetCommitment("person-a"), Is.Null);
        Assert.That(lifecycle.GetCommitment("person-b"), Is.Null);
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot cancelled), Is.True);
        Assert.That(cancelled.State, Is.EqualTo(ActivityLifecycleState.Cancelled));
        Assert.That(lifecycle.PendingWork, Is.Empty);
        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(2), out TimelineFailure sealFailure), Is.True,
            sealFailure.ToString());
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(2), out TimelineFailure advanceFailure), Is.True,
            advanceFailure.ToString());
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot afterStaleStart), Is.True);
        Assert.That(afterStaleStart.State, Is.EqualTo(ActivityLifecycleState.Cancelled));
    }

    [Test]
    public void DueStartWithoutEitherP8RouteFailsWholeActivityAndReleasesPair()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null, economyEnabled: false);
        ActivityLifecycleStore lifecycle = new ActivityLifecycleStore("joint-travel-world");
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle, Calendar(), new LogicalTick(0));
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition,
            runtime.P8ETravelTransactionCoordinator, _ => null);
        owner.TryCreate(Definition(), "joint-failed-start", "shared-segment", new[] { "person-a", "person-b" },
            out P20JointCivilTravelSnapshot proposal, out _);
        owner.TryRecordAssent(proposal.ActivityInstanceId, new P20JointCivilTravelAssent("person-a", true, "input-a"));
        owner.TryRecordAssent(proposal.ActivityInstanceId, new P20JointCivilTravelAssent("person-b", true, "input-b"));
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, new LogicalTick(1), out _), Is.True);

        Assert.That(composition.Timeline.TrySealInputsThrough(new LogicalTick(1), out TimelineFailure sealFailure), Is.True,
            sealFailure.ToString());
        Assert.That(composition.Timeline.TryAdvanceTo(new LogicalTick(1), out TimelineFailure advanceFailure), Is.True, advanceFailure.ToString());
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot failed), Is.True);
        Assert.That(failed.State, Is.EqualTo(ActivityLifecycleState.Cancelled));
        Assert.That(failed.Disposition, Is.EqualTo("movement-context-unavailable"));
        Assert.That(lifecycle.GetCommitment("person-a"), Is.Null);
        Assert.That(lifecycle.GetCommitment("person-b"), Is.Null);
        Assert.That(runtime.PersonSpatialPositionStore.Count, Is.Zero);
        Assert.That(runtime.PersonRoutePlanStore.PlanCount, Is.Zero);
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot afterStart), Is.True);
        Assert.That(afterStart.State, Is.EqualTo(P20JointCivilTravelState.FailedToStart));
    }

    [Test]
    public void StaleProposalCannotAcceptAssentAfterP18TerminalTransition()
    {
        P20JointCivilTravelOwner owner = CreateOwner(out ActivityLifecycleStore lifecycle);
        Assert.That(owner.TryCreate(Definition(), "joint-stale-proposal", "shared-segment",
            new[] { "person-a", "person-b" }, out P20JointCivilTravelSnapshot proposal, out _), Is.True);
        Assert.That(owner.TryCancel(proposal.ActivityInstanceId, "cancelled-before-assent", out ActivityFailure cancelFailure), Is.True,
            cancelFailure.ToString());

        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-a", true, "late-consent")), Is.False);
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot after), Is.True);
        Assert.That(after.Assents, Is.Empty);
        Assert.That(after.Revision, Is.EqualTo(proposal.Revision));
        Assert.That(after.State, Is.EqualTo(P20JointCivilTravelState.Cancelled),
            "The public P20 state must reflect the P18-owned lifecycle transition.");
    }

    [Test]
    public void JointScheduleConflictLeavesProposalAndBothCommitmentsUnchanged()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null, economyEnabled: false);
        ActivityLifecycleStore lifecycle = new ActivityLifecycleStore("joint-travel-world");
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle, Calendar(), new LogicalTick(0));
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition,
            runtime.P8ETravelTransactionCoordinator, _ => null);
        Assert.That(lifecycle.TryPropose(new ActivityDefinition("existing", "v1"), "existing-commitment",
            out ActivityInstanceSnapshot existing, out ActivityFailure existingFailure), Is.True, existingFailure.ToString());
        Assert.That(lifecycle.TrySchedule(composition.Timeline, existing.Id, composition.Timeline.CurrentInstant,
            new LogicalTick(1), null, new[] { "person-a" }, out existingFailure), Is.True, existingFailure.ToString());

        Assert.That(owner.TryCreate(Definition(), "joint-conflicted", "shared-segment",
            new[] { "person-a", "person-b" }, out P20JointCivilTravelSnapshot proposal, out ActivityFailure createFailure), Is.True,
            createFailure.ToString());
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-a", true, "input-a")), Is.True);
        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-b", true, "input-b")), Is.True);
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot before), Is.True);
        long lifecycleRevision = lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot staged)
            ? staged.Revision : -1L;

        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, new LogicalTick(1), out ActivityFailure scheduleFailure), Is.False);
        Assert.That(scheduleFailure, Is.EqualTo(ActivityFailure.ParticipantConflict));
        Assert.That(owner.TryGet(proposal.ActivityInstanceId, out P20JointCivilTravelSnapshot after), Is.True);
        Assert.That(after.State, Is.EqualTo(P20JointCivilTravelState.Proposed));
        Assert.That(after.Revision, Is.EqualTo(before.Revision));
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot unchanged), Is.True);
        Assert.That(unchanged.Revision, Is.EqualTo(lifecycleRevision));
        Assert.That(unchanged.State, Is.EqualTo(ActivityLifecycleState.Proposed));
        Assert.That(lifecycle.GetCommitment("person-a").ActivityInstanceId, Is.EqualTo(existing.Id));
        Assert.That(lifecycle.GetCommitment("person-b"), Is.Null);
    }

    [Test]
    public void JointTravelIdIsTheP18InstanceIdAndSurvivesLifecycleClone()
    {
        P20JointCivilTravelOwner owner = CreateOwner(out ActivityLifecycleStore lifecycle);
        Assert.That(owner.TryCreate(Definition(), "joint-stable-creation", "shared-segment",
            new[] { "person-a", "person-b" }, out P20JointCivilTravelSnapshot proposal, out _), Is.True);
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot original), Is.True);
        Assert.That(original.CreationIdentity, Is.EqualTo("joint-stable-creation"));

        ActivityLifecycleStore restored = lifecycle.Clone();
        ActivityLifecycleComposition restoredComposition = new ActivityLifecycleComposition(
            restored, Calendar(), new LogicalTick(0));
        Assert.That(restored.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot restoredActivity), Is.True);
        Assert.That(restoredActivity.Id, Is.EqualTo(proposal.ActivityInstanceId));
        Assert.That(restoredActivity.CreationIdentity, Is.EqualTo(original.CreationIdentity));
        P20JointCivilTravelOwner restoredOwner = new P20JointCivilTravelOwner(
            restoredComposition, new SimulationRuntime(new SimulationTime(), null, null, economyEnabled: false)
                .P8ETravelTransactionCoordinator, _ => null);
        Assert.That(restoredOwner.TryCreate(Definition(), "joint-stable-creation", "shared-segment",
            new[] { "person-a", "person-b" }, out _, out ActivityFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure, Is.EqualTo(ActivityFailure.DuplicateCreation));
        Assert.That(restored.SnapshotInstances(), Has.Count.EqualTo(1));
    }

    [Test]
    public void P20BCompositionRejectsTerminalMutationOfAnUnrelatedActivity()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null, economyEnabled: false);
        ActivityLifecycleStore lifecycle = new ActivityLifecycleStore("joint-travel-world");
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle, Calendar(), new LogicalTick(0));
        P20JointCivilTravelOwner owner = new P20JointCivilTravelOwner(composition,
            runtime.P8ETravelTransactionCoordinator, _ => null);
        Assert.That(lifecycle.TryPropose(new ActivityDefinition("unrelated", "v1"), "unrelated-creation",
            out ActivityInstanceSnapshot unrelated, out _), Is.True);

        Assert.That(owner.TryCancel(unrelated.Id, "should-not-cancel", out ActivityFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ActivityFailure.UnknownInstance));
        Assert.That(lifecycle.TryGet(unrelated.Id, out ActivityInstanceSnapshot unchanged), Is.True);
        Assert.That(unchanged.State, Is.EqualTo(ActivityLifecycleState.Proposed));
    }

    [Test]
    public void P20BAndP20ASyntheticOwnersCannotShareOneLifecycleComposition()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null, economyEnabled: false);
        ActivityLifecycleStore lifecycle = new ActivityLifecycleStore("joint-travel-world");
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle, Calendar(), new LogicalTick(0));
        P20JointCivilTravelOwner jointOwner = new P20JointCivilTravelOwner(composition,
            runtime.P8ETravelTransactionCoordinator, _ => null);
        Assert.That(jointOwner, Is.Not.Null);
        Assert.Throws<System.InvalidOperationException>(() =>
            new P20SyntheticOperationOwner(composition, new SyntheticRules()));
    }

}
