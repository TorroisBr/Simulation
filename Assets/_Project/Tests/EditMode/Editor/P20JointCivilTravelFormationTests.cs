using NUnit.Framework;

public sealed class P20JointCivilTravelFormationTests
{
    private static SimulationCalendar Calendar() => new SimulationCalendar(new CalendarDefinition(2, 2, 3));
    private static ActivityDefinition Definition() => new ActivityDefinition("joint-civil-travel", "v1");

    private static P20JointCivilTravelOwner CreateOwner(out ActivityLifecycleStore lifecycle)
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null, economyEnabled: false);
        lifecycle = new ActivityLifecycleStore("joint-travel-world");
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(lifecycle, Calendar(), new LogicalTick(0));
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
        Assert.That(owner.TrySchedule(proposal.ActivityInstanceId, new LogicalTick(1), out _), Is.False);
        Assert.That(lifecycle.TryGet(proposal.ActivityInstanceId, out ActivityInstanceSnapshot stillProposed), Is.True);
        Assert.That(stillProposed.State, Is.EqualTo(ActivityLifecycleState.Proposed));
        Assert.That(lifecycle.GetCommitment("person-a"), Is.Null);

        Assert.That(owner.TryRecordAssent(proposal.ActivityInstanceId,
            new P20JointCivilTravelAssent("person-b", true, "input-b")), Is.True);
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
    }

    [Test]
    public void CancellationBeforeStartReleasesBothP18Commitments()
    {
        P20JointCivilTravelOwner owner = CreateOwner(out ActivityLifecycleStore lifecycle);
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
}
