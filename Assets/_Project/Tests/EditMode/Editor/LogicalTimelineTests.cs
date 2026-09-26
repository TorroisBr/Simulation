using System.Collections.Generic;
using NUnit.Framework;

public sealed class LogicalTimelineTests
{
    private sealed class Owner : IDueWorkOwner
    {
        public readonly List<string> Committed = new List<string>();
        public readonly HashSet<string> Current = new HashSet<string>();
        public readonly Dictionary<string, IReadOnlyList<DueWorkReference>> Generated =
            new Dictionary<string, IReadOnlyList<DueWorkReference>>();
        public SimulationTimeline Timeline;
        public bool CommitSucceeds = true;
        public bool ReenterOnCommit;
        public System.Action<string> OnCommit;
        public bool IsCurrent(DueWorkReference reference) => Current.Contains(reference.DueWorkId);
        public bool TryPrepare(DueWorkReference reference, out IDueWorkCommit prepared, out TimelineFailure failure)
        {
            prepared = new Prepared(this, reference,
                Generated.TryGetValue(reference.DueWorkId, out IReadOnlyList<DueWorkReference> generated)
                    ? generated : new DueWorkReference[0]);
            failure = TimelineFailure.None;
            return true;
        }

        private sealed class Prepared : IDueWorkCommit
        {
            private readonly Owner owner;
            private readonly DueWorkReference reference;
            public IReadOnlyList<DueWorkReference> NewOwnerFacts { get; }
            public Prepared(Owner owner, DueWorkReference reference, IReadOnlyList<DueWorkReference> newFacts)
            { this.owner = owner; this.reference = reference; NewOwnerFacts = newFacts; }
            public bool TryCommit(out TimelineFailure failure)
            {
                if (owner.ReenterOnCommit)
                {
                    Assert.That(owner.Timeline.TryAdvanceTo(reference.DueAt, out failure), Is.False);
                    return false;
                }
                if (!owner.CommitSucceeds) { failure = TimelineFailure.DispatchFailed; return false; }
                owner.Committed.Add(reference.DueWorkId);
                owner.OnCommit?.Invoke(reference.DueWorkId);
                failure = TimelineFailure.None;
                return true;
            }
        }
    }

    private static SimulationCalendar Calendar() => new SimulationCalendar(new CalendarDefinition(2, 2, 3));

    [Test]
    public void TickProjectionUsesCalendarAndExactDayBoundaries()
    {
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0));
        Assert.That(timeline.CurrentDate, Is.EqualTo(Calendar().GetDate(0)));
        Assert.That(new LogicalTick(LogicalTick.TicksPerDay - 1).AbsoluteDay, Is.EqualTo(0));
        Assert.That(new LogicalTick(LogicalTick.TicksPerDay).AbsoluteDay, Is.EqualTo(1));
        Assert.That(new LogicalTick(LogicalTick.TicksPerDay).TickOfDay, Is.Zero);
        Assert.That(new LogicalTick(2 * LogicalTick.TicksPerDay).ToDate(Calendar()).Month, Is.EqualTo(1));
    }

    [Test]
    public void AdvanceRejectsBackwardsAndPureQueriesDoNotChangeClock()
    {
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(4));
        Assert.That(timeline.GetCalendarProjection(new LogicalTick(0)).AbsoluteDay, Is.Zero);
        Assert.That(timeline.CurrentInstant.Value, Is.EqualTo(4));
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(3), out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.TargetBeforeNow));
        Assert.That(timeline.CurrentInstant.Value, Is.EqualTo(4));
    }

    [Test]
    public void SameInstantWorkHasStableOwnerAndIdOrdering()
    {
        Owner owner = new Owner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner);
        owner.Timeline = timeline;
        foreach (string id in new[] { "z", "b", "a" })
        {
            owner.Current.Add(id);
            Assert.That(timeline.TryIndexOwnerFact(new DueWorkReference("owner", id, "activity-instance", 0, 0,
                new LogicalTick(10)), out _), Is.True);
        }
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(10), out TimelineFailure failure), Is.True, failure.ToString());
        CollectionAssert.AreEqual(new[] { "a", "b", "z" }, owner.Committed);
    }

    [Test]
    public void WorkGeneratedAtCurrentInstantRunsAfterItsCommittedParent()
    {
        Owner owner = new Owner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner);
        owner.Timeline = timeline;
        owner.Current.Add("parent");
        owner.Current.Add("z-peer");
        owner.Current.Add("a-child");
        Assert.That(timeline.TryIndexOwnerFact(new DueWorkReference("owner", "parent", "instance", 0, 0,
            new LogicalTick(5)), out _), Is.True);
        Assert.That(timeline.TryIndexOwnerFact(new DueWorkReference("owner", "z-peer", "instance", 0, 1,
            new LogicalTick(5)), out _), Is.True);
        owner.Generated["parent"] = new[] { new DueWorkReference("owner", "a-child", "instance", 0, 2,
            new LogicalTick(5)) };
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "parent", "z-peer", "a-child" }, owner.Committed);
    }

    [Test]
    public void FailedPreparedCommitRetainsWorkAndDoesNotAllocateGeneratedSequence()
    {
        Owner owner = new Owner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner);
        owner.Timeline = timeline;
        owner.Current.Add("parent"); owner.Current.Add("child");
        Assert.That(timeline.TryIndexOwnerFact(new DueWorkReference("owner", "parent", "instance", 0, 0,
            new LogicalTick(5)), out _), Is.True);
        long sequenceBefore = timeline.CausalSequence;
        owner.Generated["parent"] = new[] { new DueWorkReference("owner", "child", "instance", 0, 1,
            new LogicalTick(5)) };
        owner.CommitSucceeds = false;
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.DispatchFailed));
        Assert.That(timeline.CausalSequence, Is.EqualTo(sequenceBefore));
        Assert.That(timeline.IsDue(new LogicalTick(5), "parent"), Is.True);
        Assert.That(timeline.IsDue(new LogicalTick(5), "child"), Is.False);
        owner.CommitSucceeds = true;
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "parent", "child" }, owner.Committed);
    }

    [Test]
    public void OwnerCannotReenterTimelineDuringCommit()
    {
        Owner owner = new Owner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner);
        owner.Timeline = timeline;
        owner.Current.Add("work");
        Assert.That(timeline.TryIndexOwnerFact(new DueWorkReference("owner", "work", "instance", 0, 0,
            new LogicalTick(5)), out _), Is.True);
        owner.ReenterOnCommit = true;
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ReentrantAdvance));
        Assert.That(timeline.IsDue(new LogicalTick(5), "work"), Is.True);
    }

    [Test]
    public void InstantLimitLeavesRemainingWorkQueuedAtFailureInstant()
    {
        Owner owner = new Owner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner,
            maxDispatchesPerInstant: 1);
        owner.Timeline = timeline;
        foreach (string id in new[] { "one", "two" })
        {
            owner.Current.Add(id);
            Assert.That(timeline.TryIndexOwnerFact(new DueWorkReference("owner", id, "instance", 0,
                id == "one" ? 0 : 1, new LogicalTick(4)), out _), Is.True);
        }
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(8), out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.InstantWorkLimitExceeded));
        Assert.That(timeline.CurrentInstant.Value, Is.EqualTo(4));
        Assert.That(owner.Committed, Is.EqualTo(new[] { "one" }));
        Assert.That(timeline.IsDue(new LogicalTick(4), "two"), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(8), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "one", "two" }, owner.Committed);
    }

    [Test]
    public void FailedDailyBoundaryRemainsPendingForRetryAtSameInstant()
    {
        int attempts = 0;
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0),
            dailyBoundaryOperation: boundary =>
            {
                Assert.That(boundary.OccurrenceId, Is.EqualTo("legacy-daily-boundary:1"));
                attempts++;
                if (attempts == 1) throw new System.InvalidOperationException("injected failure");
            });
        LogicalTick boundaryTick = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(timeline.TryAdvanceTo(boundaryTick, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.DailyBoundaryFailed));
        Assert.That(timeline.CurrentInstant, Is.EqualTo(boundaryTick));
        Assert.That(timeline.TryAdvanceTo(boundaryTick, out _), Is.True);
        Assert.That(attempts, Is.EqualTo(2));
    }

    [Test]
    public void DailyBoundariesAreVisitedInOrderAndBeforeBoundaryWork()
    {
        List<string> order = new List<string>();
        Owner owner = new Owner();
        owner.OnCommit = id => order.Add(id);
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(1), owner,
            boundary => order.Add("day" + boundary.AbsoluteDay));
        owner.Timeline = timeline;
        owner.Current.Add("ordinary");
        Assert.That(timeline.TryIndexOwnerFact(new DueWorkReference("owner", "ordinary", "instance", 0, 0,
            new LogicalTick(LogicalTick.TicksPerDay)), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(2 * LogicalTick.TicksPerDay), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "day1", "ordinary", "day2" }, order);
    }

    [Test]
    public void OwnerStalenessStopsAtDueInstantAndDoesNotAdvancePastFailure()
    {
        Owner owner = new Owner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner);
        Assert.That(timeline.TryIndexOwnerFact(new DueWorkReference("owner", "stale", "instance", 0, 0,
            new LogicalTick(7)), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(20), out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.StaleWork));
        Assert.That(timeline.CurrentInstant.Value, Is.EqualTo(7));
    }

    [Test]
    public void TimeOverflowAndNegativeTimeAreRejected()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new LogicalTick(-1));
        Assert.Throws<System.OverflowException>(() => { LogicalTick ignored = new LogicalTick(long.MaxValue).NextDayBoundary; });
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0));
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(long.MaxValue), out _), Is.True);
    }

    [Test]
    public void RuntimeAdvanceDayUsesTimelineAndAdvancesOneCalendarBoundary()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null);
        runtime.AdvanceDay();
        Assert.That(runtime.SimulationTime.AbsoluteDay, Is.EqualTo(1));
        Assert.That(runtime.Timeline.CurrentInstant.Value, Is.EqualTo(LogicalTick.TicksPerDay));
        Assert.That(runtime.Timeline.CurrentDate.AbsoluteDay, Is.EqualTo(runtime.SimulationTime.AbsoluteDay));
    }

    [Test]
    public void RuntimeRejectsInitialDayOutsideLogicalTickRange()
    {
        long maxDay = long.MaxValue / LogicalTick.TicksPerDay;
        Assert.DoesNotThrow(() => new SimulationRuntime(new SimulationTime(maxDay), null, null));
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => new SimulationRuntime(new SimulationTime(maxDay + 1L), null, null));
    }

    [Test]
    public void RuntimeAdvanceToVisitsEachDailyBoundaryChronologically()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null);
        Assert.That(runtime.TryAdvanceTo(new LogicalTick(3 * LogicalTick.TicksPerDay + 17),
            out SimulationRuntimeAdvanceFailure failure), Is.True, failure.ToString());
        Assert.That(runtime.SimulationTime.AbsoluteDay, Is.EqualTo(3));
        Assert.That(runtime.Timeline.CurrentInstant.Value, Is.EqualTo(3 * LogicalTick.TicksPerDay + 17));
        Assert.That(runtime.Timeline.CurrentDate.AbsoluteDay, Is.EqualTo(3));
    }

    [Test]
    public void RuntimeChronologicallyDispatchesDueWorkBeforeDailyBoundary()
    {
        Owner owner = new Owner();
        owner.Current.Add("midday");
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null,
            timelineDueWorkOwner: owner);
        Assert.That(runtime.Timeline.TryIndexOwnerFact(new DueWorkReference("domain", "midday", "instance",
            0, 0, new LogicalTick(20)), out _), Is.True);
        Assert.That(runtime.TryAdvanceTo(new LogicalTick(30), out SimulationRuntimeAdvanceFailure failure),
            Is.True, failure.ToString());
        CollectionAssert.AreEqual(new[] { "midday" }, owner.Committed);
        Assert.That(runtime.SimulationTime.AbsoluteDay, Is.EqualTo(0));
        runtime.AdvanceDay();
        Assert.That(runtime.SimulationTime.AbsoluteDay, Is.EqualTo(1));
        CollectionAssert.AreEqual(new[] { "midday" }, owner.Committed);
    }

    [Test]
    public void DueWorkIdentityIsInstanceScopedAndDoesNotImplyOneParticipant()
    {
        Owner owner = new Owner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner);
        owner.Timeline = timeline;
        owner.Current.Add("participant-a");
        owner.Current.Add("participant-b");
        Assert.That(timeline.TryIndexOwnerFact(new DueWorkReference("domain", "participant-a",
            "shared-activity-instance", 1, 0, new LogicalTick(9)), out _), Is.True);
        Assert.That(timeline.TryIndexOwnerFact(new DueWorkReference("domain", "participant-b",
            "shared-activity-instance", 1, 1, new LogicalTick(9)), out _), Is.True);
        Assert.That(timeline.PreviewDueWork(new LogicalTick(9)).Count, Is.EqualTo(2));
    }
}
