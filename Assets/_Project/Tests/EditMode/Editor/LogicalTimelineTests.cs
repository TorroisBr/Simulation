using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class LogicalTimelineTests
{
    private sealed class DueOwner : IDueWorkOwner
    {
        public readonly HashSet<string> Current = new HashSet<string>();
        public readonly List<string> Committed = new List<string>();
        public readonly Dictionary<string, IReadOnlyList<DueWorkReference>> Generated = new Dictionary<string, IReadOnlyList<DueWorkReference>>();
        public Action<string> OnCommitted;
        public bool CommitSucceeds = true;
        public SimulationTimeline Timeline;
        public bool Reenter;
        public bool IsCurrent(DueWorkReference reference) => Current.Contains(reference.DueWorkId);
        public bool TryPrepare(DueWorkReference reference, out IDueWorkCommit prepared, out TimelineFailure failure)
        {
            Generated.TryGetValue(reference.DueWorkId, out IReadOnlyList<DueWorkReference> generated);
            prepared = new Prepared(this, reference, generated ?? Array.Empty<DueWorkReference>());
            failure = TimelineFailure.None;
            return true;
        }
        private sealed class Prepared : IDueWorkCommit
        {
            private readonly DueOwner owner;
            private readonly DueWorkReference reference;
            public IReadOnlyList<DueWorkReference> NewOwnerFacts { get; }
            public Prepared(DueOwner owner, DueWorkReference reference, IReadOnlyList<DueWorkReference> newFacts)
            { this.owner = owner; this.reference = reference; NewOwnerFacts = newFacts; }
            public bool TryCommit(out TimelineFailure failure)
            {
                if (owner.Reenter)
                {
                    Assert.That(owner.Timeline.TryAdvanceTo(reference.DueAt, out failure), Is.False);
                    return false;
                }
                if (!owner.CommitSucceeds) { failure = TimelineFailure.DispatchFailed; return false; }
                owner.Committed.Add(reference.DueWorkId);
                owner.OnCommitted?.Invoke(reference.DueWorkId);
                failure = TimelineFailure.None;
                return true;
            }
        }
    }

    private sealed class InputOwner : ITimelineInputOwner
    {
        public readonly List<string> Applied = new List<string>();
        public Action<string> OnApplied;
        public bool CommitSucceeds = true;
        public readonly Dictionary<string, IReadOnlyList<DueWorkReference>> Generated = new Dictionary<string, IReadOnlyList<DueWorkReference>>();
        public bool TryPrepare(TimelineInputReference input, out ITimelineInputCommit prepared, out TimelineFailure failure)
        { prepared = new InputCommit(this, input); failure = TimelineFailure.None; return true; }
        private sealed class InputCommit : ITimelineInputCommit
        {
            private readonly InputOwner owner;
            private readonly TimelineInputReference input;
            public IReadOnlyList<DueWorkReference> NewOwnerFacts =>
                owner.Generated.TryGetValue(input.InputId, out IReadOnlyList<DueWorkReference> facts)
                    ? facts : Array.Empty<DueWorkReference>();
            public InputCommit(InputOwner owner, TimelineInputReference input) { this.owner = owner; this.input = input; }
            public bool TryCommit(out TimelineFailure failure)
            {
                if (!owner.CommitSucceeds) { failure = TimelineFailure.DispatchFailed; return false; }
                owner.Applied.Add(input.InputId);
                owner.OnApplied?.Invoke(input.InputId);
                failure = TimelineFailure.None;
                return true;
            }
        }
    }

    private sealed class AtomicBoundaryOwner : IDayBoundaryOwner
    {
        public readonly HashSet<string> CommittedIds = new HashSet<string>();
        public readonly List<string> Effects = new List<string>();
        public readonly List<string> Attempts = new List<string>();
        public Action<string> OnEffect;
        public bool FailNext;
        public bool TryPrepare(DailyBoundaryOperation operation, out IDayBoundaryCommit prepared, out TimelineFailure failure)
        {
            Attempts.Add(operation.OccurrenceId);
            prepared = new BoundaryCommit(this, operation);
            failure = TimelineFailure.None;
            return true;
        }
        private sealed class BoundaryCommit : IDayBoundaryCommit
        {
            private readonly AtomicBoundaryOwner owner;
            private readonly DailyBoundaryOperation operation;
            public BoundaryCommit(AtomicBoundaryOwner owner, DailyBoundaryOperation operation)
            { this.owner = owner; this.operation = operation; }
            public bool TryCommit(out TimelineFailure failure)
            {
                if (owner.CommittedIds.Contains(operation.OccurrenceId)) { failure = TimelineFailure.None; return true; }
                if (owner.FailNext) { owner.FailNext = false; failure = TimelineFailure.DailyBoundaryFailed; return false; }
                owner.Effects.Add("effect:" + operation.AbsoluteDay);
                owner.OnEffect?.Invoke(operation.OccurrenceId);
                owner.CommittedIds.Add(operation.OccurrenceId);
                failure = TimelineFailure.None;
                return true;
            }
        }
    }

    private static SimulationCalendar Calendar() => new SimulationCalendar(new CalendarDefinition(2, 2, 3));
    private static DueWorkReference Work(string owner, string id, string instance, long sequence, long tick) =>
        new DueWorkReference(owner, id, instance, 0, sequence, new LogicalTick(tick));

    [Test]
    public void ProjectionUsesLogicalDayAndCustomCalendarAtBoundariesAndMaximumDay()
    {
        SimulationCalendar calendar = Calendar();
        Assert.That(new LogicalTick(0).ToDate(calendar).AbsoluteDay, Is.Zero);
        Assert.That(new LogicalTick(LogicalTick.TicksPerDay - 1).AbsoluteDay, Is.Zero);
        Assert.That(new LogicalTick(LogicalTick.TicksPerDay).TickOfDay, Is.Zero);
        Assert.That(new LogicalTick(12 * LogicalTick.TicksPerDay).ToDate(calendar).Year, Is.EqualTo(1));
        long maxDay = long.MaxValue / LogicalTick.TicksPerDay;
        LogicalTick lastSupportedDay = new LogicalTick(long.MaxValue);
        Assert.That(lastSupportedDay.ToDate(calendar).AbsoluteDay, Is.EqualTo(maxDay));
        Assert.Throws<OverflowException>(() => { LogicalTick ignored = new LogicalTick(long.MaxValue).NextDayBoundary; });
    }

    [Test]
    public void ProjectionRespectsCustomMonthLengths()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(new[] { 2, 5, 1 }, 3));
        Assert.That(new LogicalTick(2 * LogicalTick.TicksPerDay).ToDate(calendar).Month, Is.EqualTo(2));
        Assert.That(new LogicalTick(8 * LogicalTick.TicksPerDay).ToDate(calendar).Year, Is.EqualTo(1));
    }

    [Test]
    public void InputSequenceIsStableSealedAndLateOrDuplicateInputIsRejected()
    {
        InputOwner inputOwner = new InputOwner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), inputOwner: inputOwner);
        Assert.That(timeline.TryAcceptInput(new TimelineInputReference(2, "second", "Action", "b", new LogicalTick(10)), out _), Is.True);
        Assert.That(timeline.TryAcceptInput(new TimelineInputReference(1, "first", "Action", "a", new LogicalTick(10)), out _), Is.True);
        Assert.That(timeline.TryAcceptInput(new TimelineInputReference(1, "collision", "Action", "c", new LogicalTick(10)), out TimelineFailure duplicate), Is.False);
        Assert.That(duplicate, Is.EqualTo(TimelineFailure.DuplicateInputSequence));
        Assert.That(timeline.PreviewInputs(new LogicalTick(10))[0].Sequence, Is.EqualTo(1));
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(10), out _), Is.True);
        Assert.That(timeline.TryAcceptInput(new TimelineInputReference(3, "late", "Action", "x", new LogicalTick(10)), out TimelineFailure late), Is.False);
        Assert.That(late, Is.EqualTo(TimelineFailure.LateInput));
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(10), out TimelineFailure notSealed), Is.True, notSealed.ToString());
        CollectionAssert.AreEqual(new[] { "first", "second" }, inputOwner.Applied);
    }

    [Test]
    public void InitialInstantMayReceiveInputUntilItsFirstSeal()
    {
        InputOwner inputOwner = new InputOwner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), inputOwner: inputOwner);
        Assert.That(timeline.TryAcceptInput(new TimelineInputReference(0, "initial", "Action", "x", new LogicalTick(0)), out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(0), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(0), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "initial" }, inputOwner.Applied);
        Assert.That(timeline.TryAcceptInput(new TimelineInputReference(1, "too-late", "Action", "y", new LogicalTick(0)), out _), Is.False);
    }

    [Test]
    public void InputCommitPublishesTypedDueFactsOnlyWhenTheInputTransactionCommits()
    {
        List<string> order = new List<string>();
        InputOwner input = new InputOwner { OnApplied = id => order.Add("input:" + id) };
        DueOwner due = new DueOwner { OnCommitted = id => order.Add("due:" + id) };
        due.Current.Add("created-by-input");
        input.Generated["choice"] = new[] { Work("owner", "created-by-input", "instance", 1, 12) };
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), due, input);
        due.Timeline = timeline;
        Assert.That(timeline.TryAcceptInput(new TimelineInputReference(0, "choice", "Action", "x", new LogicalTick(12)), out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(12), out _), Is.True);
        input.CommitSucceeds = false;
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(12), out _), Is.False);
        Assert.That(timeline.IsDue(new LogicalTick(12), "created-by-input"), Is.False);
        Assert.That(timeline.CausalSequence, Is.Zero);
        input.CommitSucceeds = true;
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(12), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "input:choice", "due:created-by-input" }, order);
        Assert.That(timeline.CausalSequence, Is.EqualTo(1));
    }

    [Test]
    public void AdvanceRequiresSealedInputPrefixAndRejectsBackwardTargets()
    {
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(4));
        Assert.That(timeline.GetCalendarProjection(new LogicalTick(0)).AbsoluteDay, Is.Zero);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out TimelineFailure unsealed), Is.False);
        Assert.That(unsealed, Is.EqualTo(TimelineFailure.InputNotSealed));
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(5), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(3), out TimelineFailure backwards), Is.False);
        Assert.That(backwards, Is.EqualTo(TimelineFailure.TargetBeforeNow));
        Assert.That(timeline.CurrentInstant.Value, Is.EqualTo(4));
    }

    [Test]
    public void SameInstantDueWorkOrderingIsIndependentOfRegistrationOrderAndRejectsSequenceCollision()
    {
        DueOwner owner = new DueOwner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner);
        owner.Timeline = timeline;
        foreach (string id in new[] { "z", "b", "a" })
        {
            owner.Current.Add(id);
            Assert.That(timeline.TryIndexOwnerFact(Work("owner", id, "activity-instance", 1, 10), out _), Is.True);
        }
        Assert.That(timeline.TryIndexOwnerFact(Work("owner", "a", "other-instance", 1, 10), out TimelineFailure collision), Is.False);
        Assert.That(collision, Is.EqualTo(TimelineFailure.DuplicateWorkSequence));
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(10), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(10), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "a", "b", "z" }, owner.Committed);
    }

    [Test]
    public void LengthPrefixedDueWorkIdentityKeepsDelimiterBearingComponentsDistinct()
    {
        DueOwner owner = new DueOwner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner);
        owner.Current.Add("c"); owner.Current.Add("b\nc");
        DueWorkReference first = new DueWorkReference("a\nb", "c", "instance-1", 1, 2, new LogicalTick(10));
        DueWorkReference second = new DueWorkReference("a", "b\nc", "instance-2", 1, 2, new LogicalTick(10));
        Assert.That(timeline.TryIndexOwnerFact(first, out _), Is.True);
        Assert.That(timeline.TryIndexOwnerFact(second, out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(timeline.PreviewDueWork(new LogicalTick(10)).Count, Is.EqualTo(2));
    }

    [Test]
    public void LengthPrefixedBoundaryIdentityKeepsDelimiterBearingWorldAndProfileDistinct()
    {
        DailyBoundaryOperation first = new DailyBoundaryOperation("world:segment", "profile", 1);
        DailyBoundaryOperation second = new DailyBoundaryOperation("world", "segment:profile", 1);
        Assert.That(first.OccurrenceId, Is.Not.EqualTo(second.OccurrenceId));
        Assert.That(first.OccurrenceId, Is.EqualTo("13:world:segment7:profile1:1"));
    }

    [Test]
    public void SameInstantGeneratedWorkUsesNextCausalWaveAfterCurrentWave()
    {
        DueOwner owner = new DueOwner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner);
        owner.Timeline = timeline;
        foreach (string id in new[] { "parent", "z-peer", "a-child" }) owner.Current.Add(id);
        Assert.That(timeline.TryIndexOwnerFact(Work("owner", "parent", "instance", 0, 5), out _), Is.True);
        Assert.That(timeline.TryIndexOwnerFact(Work("owner", "z-peer", "instance", 1, 5), out _), Is.True);
        owner.Generated["parent"] = new[] { Work("owner", "a-child", "instance", 2, 5) };
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(5), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "parent", "z-peer", "a-child" }, owner.Committed);
    }

    [Test]
    public void FailedOwnerCommitRetainsWorkAndDoesNotAllocateCausalSequence()
    {
        DueOwner owner = new DueOwner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner);
        owner.Timeline = timeline; owner.Current.Add("parent"); owner.Current.Add("child");
        Assert.That(timeline.TryIndexOwnerFact(Work("owner", "parent", "instance", 0, 5), out _), Is.True);
        owner.Generated["parent"] = new[] { Work("owner", "child", "instance", 1, 5) };
        long before = timeline.CausalSequence;
        owner.CommitSucceeds = false;
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(5), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.DispatchFailed));
        Assert.That(timeline.CausalSequence, Is.EqualTo(before));
        Assert.That(timeline.IsDue(new LogicalTick(5), "parent"), Is.True);
        owner.CommitSucceeds = true;
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "parent", "child" }, owner.Committed);
    }

    [Test]
    public void DayBoundaryOwnerCommitsAtomicallyBeforeOrdinaryWorkAndRetriesSameIdentity()
    {
        AtomicBoundaryOwner boundary = new AtomicBoundaryOwner { FailNext = true };
        DueOwner due = new DueOwner();
        due.Current.Add("ordinary");
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), due,
            boundaryOwner: boundary, worldId: "test-world", profileId: "synthetic-intraday");
        due.Timeline = timeline;
        Assert.That(timeline.TryIndexOwnerFact(Work("owner", "ordinary", "instance", 0, LogicalTick.TicksPerDay), out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(LogicalTick.TicksPerDay), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(LogicalTick.TicksPerDay), out TimelineFailure failed), Is.False);
        Assert.That(failed, Is.EqualTo(TimelineFailure.DailyBoundaryFailed));
        Assert.That(timeline.CurrentInstant.Value, Is.EqualTo(LogicalTick.TicksPerDay));
        Assert.That(boundary.Effects, Is.Empty);
        Assert.That(due.Committed, Is.Empty);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(LogicalTick.TicksPerDay), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "effect:1" }, boundary.Effects);
        CollectionAssert.AreEqual(new[] { "ordinary" }, due.Committed);
        Assert.That(boundary.Attempts[0], Is.EqualTo(boundary.Attempts[1]));
    }

    [Test]
    public void FailedBoundaryCanBeReconstructedAsTheSamePendingOwnerOperation()
    {
        AtomicBoundaryOwner boundary = new AtomicBoundaryOwner { FailNext = true };
        LogicalTick dayOne = new LogicalTick(LogicalTick.TicksPerDay);
        SimulationTimeline first = new SimulationTimeline(Calendar(), new LogicalTick(0), boundaryOwner: boundary,
            worldId: "restore-world", profileId: "restore-profile");
        Assert.That(first.TrySealInputsThrough(dayOne, out _), Is.True);
        Assert.That(first.TryAdvanceTo(dayOne, out _), Is.False);
        Assert.That(first.PendingBoundaryDay, Is.EqualTo(1));

        SimulationTimeline restored = new SimulationTimeline(Calendar(), dayOne, boundaryOwner: boundary,
            worldId: "restore-world", profileId: "restore-profile", pendingBoundaryDay: first.PendingBoundaryDay);
        Assert.That(restored.TrySealInputsThrough(dayOne, out _), Is.True);
        Assert.That(restored.TryAdvanceTo(dayOne, out _), Is.True);
        CollectionAssert.AreEqual(new[] { "effect:1" }, boundary.Effects);
        Assert.That(restored.PendingBoundaryDay, Is.Null);
        Assert.That(boundary.Attempts[0], Is.EqualTo(boundary.Attempts[1]));
    }

    [Test]
    public void SealedBoundaryInputsRunBeforeAtomicDayOwnerAndOrdinaryDueWork()
    {
        List<string> order = new List<string>();
        InputOwner input = new InputOwner { OnApplied = id => order.Add("input:" + id) };
        AtomicBoundaryOwner boundary = new AtomicBoundaryOwner { OnEffect = id => order.Add("boundary:" + id) };
        DueOwner due = new DueOwner { OnCommitted = id => order.Add("due:" + id) };
        due.Current.Add("ordinary");
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), due, input,
            boundary, "ordered-world", "ordered-profile");
        due.Timeline = timeline;
        Assert.That(timeline.TryAcceptInput(new TimelineInputReference(0, "choice", "Action", "x",
            new LogicalTick(LogicalTick.TicksPerDay)), out _), Is.True);
        Assert.That(timeline.TryIndexOwnerFact(Work("owner", "ordinary", "instance", 1,
            LogicalTick.TicksPerDay), out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(LogicalTick.TicksPerDay), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(LogicalTick.TicksPerDay), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "input:choice", "boundary:13:ordered-world15:ordered-profile1:1", "due:ordinary" }, order);
    }

    [Test]
    public void CrossedDayBoundariesAreOrderedAndTypedInstanceIdentityHasNoActorCardinality()
    {
        AtomicBoundaryOwner boundary = new AtomicBoundaryOwner();
        DueOwner due = new DueOwner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(1), due,
            boundaryOwner: boundary, worldId: "world", profileId: "intraday");
        due.Timeline = timeline;
        due.Current.Add("participant-a"); due.Current.Add("participant-b");
        Assert.That(timeline.TryIndexOwnerFact(Work("domain", "participant-a", "shared-instance", 1, LogicalTick.TicksPerDay), out _), Is.True);
        Assert.That(timeline.TryIndexOwnerFact(Work("domain", "participant-b", "shared-instance", 2, LogicalTick.TicksPerDay), out _), Is.True);
        Assert.That(timeline.PreviewDueWork(new LogicalTick(LogicalTick.TicksPerDay)).Count, Is.EqualTo(2));
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(2 * LogicalTick.TicksPerDay), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(2 * LogicalTick.TicksPerDay), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "5:world8:intraday1:1", "5:world8:intraday1:2" }, boundary.Attempts);
        CollectionAssert.AreEqual(new[] { "effect:1", "effect:2" }, boundary.Effects);
    }

    [Test]
    public void DispatchLimitStopsAtFailureInstantAndStaleWorkIsDiscardedAsInert()
    {
        DueOwner owner = new DueOwner();
        SimulationTimeline limited = new SimulationTimeline(Calendar(), new LogicalTick(0), owner, maxDispatchesPerInstant: 1);
        owner.Timeline = limited;
        owner.Current.Add("one"); owner.Current.Add("two");
        Assert.That(limited.TryIndexOwnerFact(Work("owner", "one", "i", 1, 4), out _), Is.True);
        Assert.That(limited.TryIndexOwnerFact(Work("owner", "two", "i", 2, 4), out _), Is.True);
        Assert.That(limited.TrySealInputsThrough(new LogicalTick(8), out _), Is.True);
        Assert.That(limited.TryAdvanceTo(new LogicalTick(8), out TimelineFailure limitFailure), Is.False);
        Assert.That(limitFailure, Is.EqualTo(TimelineFailure.InstantWorkLimitExceeded));
        Assert.That(limited.CurrentInstant.Value, Is.EqualTo(4));
        Assert.That(limited.IsDue(new LogicalTick(4), "two"), Is.True);

        DueOwner staleOwner = new DueOwner();
        SimulationTimeline stale = new SimulationTimeline(Calendar(), new LogicalTick(0), staleOwner);
        Assert.That(stale.TryIndexOwnerFact(Work("owner", "stale", "i", 0, 7), out _), Is.True);
        Assert.That(stale.TrySealInputsThrough(new LogicalTick(20), out _), Is.True);
        Assert.That(stale.TryAdvanceTo(new LogicalTick(20), out TimelineFailure staleFailure), Is.True, staleFailure.ToString());
        Assert.That(stale.CurrentInstant.Value, Is.EqualTo(20));
        Assert.That(stale.IsDue(new LogicalTick(7), "stale"), Is.False);
    }

    [Test]
    public void QueriesArePureAndInputFailureRetainsUnappliedCommandForRetry()
    {
        InputOwner input = new InputOwner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), inputOwner: input);
        Assert.That(timeline.TryAcceptInput(new TimelineInputReference(4, "cmd", "Action", "x", new LogicalTick(6)), out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(6), out _), Is.True);
        long sequenceBefore = timeline.CausalSequence;
        Assert.That(timeline.PreviewInputs(new LogicalTick(6)).Count, Is.EqualTo(1));
        Assert.That(timeline.NextDueInstant.Value.Value, Is.EqualTo(6));
        Assert.That(timeline.CurrentInstant.Value, Is.Zero);
        Assert.That(timeline.CausalSequence, Is.EqualTo(sequenceBefore));
        input.CommitSucceeds = false;
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(6), out _), Is.False);
        Assert.That(timeline.PreviewInputs(new LogicalTick(6)).Count, Is.EqualTo(1));
        input.CommitSucceeds = true;
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(6), out _), Is.True);
        CollectionAssert.AreEqual(new[] { "cmd" }, input.Applied);
    }

    [Test]
    public void ReentrantAdvanceIsRejectedAndLegacyDailyRuntimeIsNotModifiedByThisCandidate()
    {
        DueOwner owner = new DueOwner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner);
        owner.Timeline = timeline; owner.Current.Add("work");
        Assert.That(timeline.TryIndexOwnerFact(Work("owner", "work", "i", 0, 5), out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(5), out _), Is.True);
        owner.Reenter = true;
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ReentrantAdvance));
        Assert.That(timeline.IsDue(new LogicalTick(5), "work"), Is.True);
    }
}
