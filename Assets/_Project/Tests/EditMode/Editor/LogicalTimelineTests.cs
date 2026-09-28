using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class LogicalTimelineTests
{
    private sealed class DueOwner : IDueWorkOwner
    {
        public readonly HashSet<string> Current = new HashSet<string>();
        public readonly List<string> Committed = new List<string>();
        public readonly Dictionary<string, IReadOnlyList<DueWorkReference>> Generated = new Dictionary<string, IReadOnlyList<DueWorkReference>>();
        public Action<string> OnCommitted;
        public Action<string> OnPrepared;
        public bool CommitSucceeds = true;
        public SimulationTimeline Timeline;
        public bool Reenter;
        public bool IsCurrent(DueWorkReference reference) => Current.Contains(reference.DueWorkId);
        public bool TryPrepare(DueWorkReference reference, out IDueWorkCommit prepared, out TimelineFailure failure)
        {
            OnPrepared?.Invoke(reference.DueWorkId);
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

    private sealed class ResumableBoundaryOwner : IResumableDayBoundaryOwner, IBoundarySourceSignalHandoff
    {
        private readonly Dictionary<string, BoundaryContinuationState> states = new Dictionary<string, BoundaryContinuationState>(StringComparer.Ordinal);
        private readonly List<DueWorkReference> retainedFacts = new List<DueWorkReference>();
        private readonly List<string> retainedSignals = new List<string>();
        public IReadOnlyList<BoundaryContinuationStep> SelectedSteps = Array.Empty<BoundaryContinuationStep>();
        public readonly List<string> AppliedSteps = new List<string>();
        public readonly List<string> PreparedOccurrences = new List<string>();
        public readonly List<string> HandedOffSignals = new List<string>();
        public bool FailNextStep;
        public string FailStepId;
        public bool FailNextPublication;
        public bool ChangeManifestAfterStep;
        public Action OnPublished;
        public SimulationTimeline Timeline;

        public bool TryPrepare(DailyBoundaryOperation operation, out IDayBoundaryCommit prepared, out TimelineFailure failure)
        { prepared = null; failure = TimelineFailure.DispatchFailed; return false; }
        public bool TryPrepareActivation(DailyBoundaryOperation operation, out IBoundaryActivationCommit prepared, out TimelineFailure failure)
        {
            PreparedOccurrences.Add(operation.OccurrenceId);
            BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(operation, "test-subphase", "v1", "cfg|1", SelectedSteps);
            prepared = new Activation(this, manifest); failure = TimelineFailure.None; return true;
        }
        public bool TryResolveContinuation(string continuationId, out BoundaryContinuationState state, out TimelineFailure failure)
        { states.TryGetValue(continuationId, out state); failure = state == null ? TimelineFailure.ContinuationFailed : TimelineFailure.None; return state != null; }
        public bool TryPrepareStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
            out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure)
        {
            prepared = new Step(this, manifest, step); failure = TimelineFailure.None; return true;
        }
        public bool TryPrepareTimelinePublication(BoundaryContinuationManifest manifest,
            out IBoundaryTimelinePublicationCommit prepared, out TimelineFailure failure)
        { prepared = new Publication(this, manifest); failure = TimelineFailure.None; return true; }
        public bool TryHandoff(BoundaryContinuationManifest manifest, IReadOnlyList<string> signals, out TimelineFailure failure)
        {
            Assert.That(Timeline.IsAdvanceInProgress, Is.False);
            if (!states.TryGetValue(manifest.ContinuationId, out BoundaryContinuationState state)) { failure = TimelineFailure.ContinuationFailed; return false; }
            foreach (string signal in signals) if (!HandedOffSignals.Contains(signal)) HandedOffSignals.Add(signal);
            states[manifest.ContinuationId] = CopyState(state, signalsHandedOff: true);
            failure = TimelineFailure.None; return true;
        }
        private bool CommitActivation(BoundaryContinuationManifest manifest, out TimelineFailure failure)
        {
            if (!states.ContainsKey(manifest.ContinuationId))
                states.Add(manifest.ContinuationId, new BoundaryContinuationState(manifest, 0, manifest.Steps.Count == 0,
                    false, Array.Empty<BoundaryPublishedFact>(), retainedFacts, retainedSignals, false));
            failure = TimelineFailure.None; return true;
        }
        private bool CommitStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step, out TimelineFailure failure)
        {
            BoundaryContinuationState state = states[manifest.ContinuationId];
            if (state.NextStepOrdinal > step.Ordinal) { failure = TimelineFailure.None; return true; }
            if (state.NextStepOrdinal != step.Ordinal) { failure = TimelineFailure.ContinuationFailed; return false; }
            if (FailNextStep || FailStepId == step.StepId)
            { FailNextStep = false; FailStepId = null; failure = TimelineFailure.ContinuationFailed; return false; }
            AppliedSteps.Add(step.StepId);
            string signal = "signal:" + step.StepId;
            retainedSignals.Add(signal);
            if (step.Ordinal == 0)
                retainedFacts.Add(new DueWorkReference("z-boundary-owner", StableKey(step.StepId, "fact"),
                    "activity-instance|shared", 0, 1, new LogicalTick(manifest.AbsoluteDay * LogicalTick.TicksPerDay)));
            int next = step.Ordinal + 1;
            states[manifest.ContinuationId] = new BoundaryContinuationState(manifest, next, next == manifest.Steps.Count,
                false, Array.Empty<BoundaryPublishedFact>(), retainedFacts, retainedSignals, false);
            if (ChangeManifestAfterStep)
            {
                ChangeManifestAfterStep = false;
                List<BoundaryContinuationStep> altered = new List<BoundaryContinuationStep>(manifest.Steps);
                int changedOrdinal = Math.Min(next, altered.Count - 1);
                if (changedOrdinal >= 0)
                {
                    BoundaryContinuationStep old = altered[changedOrdinal];
                    altered[changedOrdinal] = new BoundaryContinuationStep(old.Ordinal, old.StepId, old.OwnerId,
                        old.OperationKind, old.OperationVersion, old.OwnerRevision, old.Payload + " changed",
                        old.PersonId, old.Disposition);
                }
                BoundaryContinuationManifest changed = new BoundaryContinuationManifest(
                    new DailyBoundaryOperation(manifest.WorldId, manifest.ProfileId, manifest.AbsoluteDay),
                    manifest.SubphaseKind, manifest.SubphaseVersion, manifest.ConfigurationIdentity,
                    altered.AsReadOnly(), manifest.ContentIdentity);
                BoundaryContinuationState current = states[manifest.ContinuationId];
                states[manifest.ContinuationId] = new BoundaryContinuationState(changed, current.NextStepOrdinal,
                    current.IsComplete, current.TimelineFactsPublished, current.PublishedFacts,
                    current.RetainedTimelineFacts, current.RetainedSourceSignals, current.SignalsHandedOff);
            }
            failure = TimelineFailure.None; return true;
        }
        private bool CommitPublication(BoundaryContinuationManifest manifest, IReadOnlyList<BoundaryPublishedFact> facts, out TimelineFailure failure)
        {
            if (FailNextPublication) { FailNextPublication = false; failure = TimelineFailure.PublicationFailed; return false; }
            BoundaryContinuationState state = states[manifest.ContinuationId];
            states[manifest.ContinuationId] = new BoundaryContinuationState(manifest, state.NextStepOrdinal, true,
                true, facts, retainedFacts, retainedSignals, false);
            OnPublished?.Invoke(); failure = TimelineFailure.None; return true;
        }
        private static BoundaryContinuationState CopyState(BoundaryContinuationState state, bool signalsHandedOff) =>
            new BoundaryContinuationState(state.Manifest, state.NextStepOrdinal, state.IsComplete, state.TimelineFactsPublished,
                state.PublishedFacts, state.RetainedTimelineFacts, state.RetainedSourceSignals, signalsHandedOff);

        private sealed class Activation : IBoundaryActivationCommit
        {
            private readonly ResumableBoundaryOwner owner;
            public BoundaryContinuationManifest Manifest { get; }
            public Activation(ResumableBoundaryOwner owner, BoundaryContinuationManifest manifest) { this.owner = owner; Manifest = manifest; }
            public bool TryCommit(out TimelineFailure failure) => owner.CommitActivation(Manifest, out failure);
        }
        private sealed class Step : IBoundaryContinuationStepCommit
        {
            private readonly ResumableBoundaryOwner owner; private readonly BoundaryContinuationManifest manifest; private readonly BoundaryContinuationStep step;
            public Step(ResumableBoundaryOwner owner, BoundaryContinuationManifest manifest, BoundaryContinuationStep step)
            { this.owner = owner; this.manifest = manifest; this.step = step; }
            public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => owner.retainedFacts.AsReadOnly();
            public IReadOnlyList<string> RetainedSourceSignals => owner.retainedSignals.AsReadOnly();
            public bool TryCommit(out TimelineFailure failure) => owner.CommitStep(manifest, step, out failure);
        }
        private sealed class Publication : IBoundaryTimelinePublicationCommit
        {
            private readonly ResumableBoundaryOwner owner; private readonly BoundaryContinuationManifest manifest;
            public Publication(ResumableBoundaryOwner owner, BoundaryContinuationManifest manifest) { this.owner = owner; this.manifest = manifest; }
            public bool TryCommit(IReadOnlyList<BoundaryPublishedFact> facts, out TimelineFailure failure) => owner.CommitPublication(manifest, facts, out failure);
        }
    }

    private static SimulationCalendar Calendar() => new SimulationCalendar(new CalendarDefinition(2, 2, 3));
    private static DueWorkReference Work(string owner, string id, string instance, long sequence, long tick) =>
        new DueWorkReference(owner, id, instance, 0, sequence, new LogicalTick(tick));
    private static BoundaryContinuationStep Step(int ordinal, string personId = null) =>
        new BoundaryContinuationStep(ordinal, "step|" + ordinal.ToString() + "|" + (personId ?? "none"),
            "owner|domain", "daily-operation", "v1", "revision|1", "payload|" + ordinal.ToString(), personId);
    private static string StableKey(params string[] parts)
    {
        System.Text.StringBuilder key = new System.Text.StringBuilder();
        foreach (string part in parts) { string value = part ?? string.Empty; key.Append(value.Length).Append(':').Append(value); }
        return key.ToString();
    }

    private static SimulationTimeline ResumableTimeline(ResumableBoundaryOwner owner, DueOwner due, string restoredId = null,
        IReadOnlyList<string> handoffIds = null, string worldId = "world|one", string profileId = "profile:one")
    {
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), due,
            boundaryOwner: owner, worldId: worldId, profileId: profileId, pendingContinuationId: restoredId,
            pendingSignalHandoffIds: handoffIds);
        due.Timeline = timeline; owner.Timeline = timeline;
        due.Current.Add("ordinary");
        Assert.That(timeline.TryIndexOwnerFact(Work("ordinary-owner", "ordinary", "activity|instance", 0, boundary.Value), out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(boundary, out _), Is.True);
        return timeline;
    }

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

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(3)]
    public void ResumableManifestPreservesZeroOneManyRosterAndInjectiveIdentities(int count)
    {
        ResumableBoundaryOwner owner = new ResumableBoundaryOwner();
        List<BoundaryContinuationStep> steps = new List<BoundaryContinuationStep>();
        for (int i = 0; i < count; i++) steps.Add(Step(i, "person|:" + i.ToString()));
        owner.SelectedSteps = steps.AsReadOnly();
        DueOwner due = new DueOwner();
        SimulationTimeline timeline = ResumableTimeline(owner, due);
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(timeline.TryAdvanceTo(boundary, out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(owner.HandedOffSignals, Is.Empty, "signals remain pending until the host completes post-advance handoff");
        Assert.That(timeline.TryAdvanceTo(boundary, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationPending));
        Assert.That(timeline.TryCompleteSuccessfulAdvanceHandoffs(out failure), Is.True, failure.ToString());
        string occurrence = StableKey("world|one", "profile:one", "1");
        string continuation = StableKey(occurrence, "test-subphase", "v1");
        Assert.That(owner.PreparedOccurrences[0], Is.EqualTo(occurrence));
        Assert.That(owner.HandedOffSignals.Count, Is.EqualTo(count));
        Assert.That(owner.AppliedSteps.Count, Is.EqualTo(count));
        Assert.That(timeline.PendingContinuationId, Is.Null);
        Assert.That(timeline.PendingSignalHandoffIds, Is.Empty);
        Assert.That(continuation, Is.Not.EqualTo(occurrence));
        Assert.That(StableKey("a|b", "c"), Is.Not.EqualTo(StableKey("a", "b|c")));
        Assert.That(owner.AppliedSteps.Distinct().Count(), Is.EqualTo(count));
        Assert.That("activity|instance", Is.Not.EqualTo("person|:0"));
    }

    [Test]
    public void ContinuationBarrierRetainsFrozenProgressAndResumesSameStepAfterPartialCommit()
    {
        ResumableBoundaryOwner owner = new ResumableBoundaryOwner { FailStepId = Step(1, "person|b").StepId };
        owner.SelectedSteps = new[] { Step(0, "person|a"), Step(1, "person|b"), Step(2, "person|c") };
        DueOwner due = new DueOwner();
        SimulationTimeline timeline = ResumableTimeline(owner, due);
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(timeline.TryAdvanceTo(boundary, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(timeline.CurrentInstant, Is.EqualTo(boundary));
        Assert.That(timeline.PendingContinuationId, Is.Not.Null);
        CollectionAssert.AreEqual(new[] { Step(0, "person|a").StepId }, owner.AppliedSteps);
        Assert.That(due.Committed, Is.Empty);

        owner.SelectedSteps = new[] { Step(0, "changed"), Step(1, "changed"), Step(2, "changed") };
        Assert.That(timeline.TryAdvanceTo(boundary, out failure), Is.True, failure.ToString());
        Assert.That(timeline.TryCompleteSuccessfulAdvanceHandoffs(out failure), Is.True, failure.ToString());
        CollectionAssert.AreEqual(new[] { Step(0, "person|a").StepId, Step(1, "person|b").StepId, Step(2, "person|c").StepId }, owner.AppliedSteps);
        CollectionAssert.AreEqual(new[] { "ordinary" }, due.Committed);
        Assert.That(owner.PreparedOccurrences.Count, Is.EqualTo(1));
    }

    [Test]
    public void FailedContinuationFactPublicationHasNoSequenceDriftAndRetryDoesNotRepeatSteps()
    {
        ResumableBoundaryOwner owner = new ResumableBoundaryOwner { FailNextPublication = true };
        owner.SelectedSteps = new[] { Step(0, "person|one") };
        DueOwner due = new DueOwner();
        SimulationTimeline timeline = ResumableTimeline(owner, due);
        long before = timeline.CausalSequence;
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(timeline.TryAdvanceTo(boundary, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.PublicationFailed));
        Assert.That(timeline.CausalSequence, Is.EqualTo(before));
        Assert.That(due.Committed, Is.Empty);
        Assert.That(owner.HandedOffSignals, Is.Empty);

        Assert.That(timeline.TryAdvanceTo(boundary, out failure), Is.True, failure.ToString());
        Assert.That(timeline.CausalSequence, Is.EqualTo(before + 1));
        Assert.That(owner.AppliedSteps.Count, Is.EqualTo(1));
        Assert.That(owner.HandedOffSignals, Is.Empty);
        Assert.That(timeline.TryCompleteSuccessfulAdvanceHandoffs(out failure), Is.True, failure.ToString());
        Assert.That(owner.HandedOffSignals, Has.Count.EqualTo(1));
    }

    [Test]
    public void ContinuationFactsArePublishedBeforeOrdinaryWorkAndSignalsWaitForOuterSuccess()
    {
        ResumableBoundaryOwner owner = new ResumableBoundaryOwner(); owner.SelectedSteps = new[] { Step(0, "person|one") };
        DueOwner due = new DueOwner();
        SimulationTimeline timeline = ResumableTimeline(owner, due);
        bool publicationCommitted = false;
        owner.OnPublished = () => publicationCommitted = true;
        due.OnPrepared = id =>
        {
            if (id == "ordinary") Assert.That(publicationCommitted, Is.True);
        };
        due.CommitSucceeds = false;
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(timeline.TryAdvanceTo(boundary, out TimelineFailure failure), Is.False);
        Assert.That(timeline.CausalSequence, Is.EqualTo(2));
        Assert.That(owner.HandedOffSignals, Is.Empty);
        due.CommitSucceeds = true;
        Assert.That(timeline.TryAdvanceTo(boundary, out failure), Is.True, failure.ToString());
        Assert.That(owner.HandedOffSignals, Is.Empty);
        Assert.That(timeline.TryCompleteSuccessfulAdvanceHandoffs(out failure), Is.True, failure.ToString());
        Assert.That(owner.HandedOffSignals, Has.Count.EqualTo(1));
        Assert.That(owner.AppliedSteps.Count, Is.EqualTo(1));
    }

    [Test]
    public void PublishedFactsAndUnhandedSignalsSurviveReconstructionAfterOuterFailure()
    {
        ResumableBoundaryOwner owner = new ResumableBoundaryOwner(); owner.SelectedSteps = new[] { Step(0, "person|one") };
        DueOwner firstDue = new DueOwner { CommitSucceeds = false };
        SimulationTimeline first = ResumableTimeline(owner, firstDue);
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(first.TryAdvanceTo(boundary, out _), Is.False);
        Assert.That(first.PendingSignalHandoffIds, Has.Count.EqualTo(1));
        string publishedId = StableKey(Step(0, "person|one").StepId, "fact");
        Assert.That(first.IsDue(boundary, publishedId), Is.True);
        Assert.That(owner.HandedOffSignals, Is.Empty);

        DueOwner restoredDue = new DueOwner(); restoredDue.Current.Add("ordinary"); restoredDue.Current.Add(publishedId);
        SimulationTimeline restored = new SimulationTimeline(Calendar(), boundary, restoredDue,
            boundaryOwner: owner, worldId: "world|one", profileId: "profile:one",
            pendingSignalHandoffIds: first.PendingSignalHandoffIds, initialCausalSequence: first.CausalSequence);
        restoredDue.Timeline = restored;
        Assert.That(restored.TryIndexOwnerFact(Work("ordinary-owner", "ordinary", "activity|instance", 0, boundary.Value), out _), Is.True);
        Assert.That(restored.TrySealInputsThrough(boundary, out _), Is.True);
        Assert.That(restored.TryAdvanceTo(boundary, out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(owner.HandedOffSignals, Is.Empty);
        Assert.That(restored.TryCompleteSuccessfulAdvanceHandoffs(out failure), Is.True, failure.ToString());
        CollectionAssert.AreEqual(new[] { "signal:" + Step(0, "person|one").StepId }, owner.HandedOffSignals);
        Assert.That(owner.AppliedSteps.Count, Is.EqualTo(1));
    }

    [Test]
    public void IncompleteContinuationCanBeReconstructedWithoutChangingItsFrozenProgress()
    {
        ResumableBoundaryOwner owner = new ResumableBoundaryOwner { FailStepId = Step(1, "person|b").StepId };
        owner.SelectedSteps = new[] { Step(0, "person|a"), Step(1, "person|b") };
        DueOwner firstDue = new DueOwner();
        SimulationTimeline first = ResumableTimeline(owner, firstDue);
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(first.TryAdvanceTo(boundary, out _), Is.False);
        string continuationId = first.PendingContinuationId;

        owner.FailStepId = null;
        DueOwner restoredDue = new DueOwner();
        SimulationTimeline restored = ResumableTimeline(owner, restoredDue, continuationId);
        Assert.That(restored.TryAdvanceTo(boundary, out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(restored.TryCompleteSuccessfulAdvanceHandoffs(out failure), Is.True, failure.ToString());
        CollectionAssert.AreEqual(new[] { Step(0, "person|a").StepId, Step(1, "person|b").StepId }, owner.AppliedSteps);
    }

    [TestCase("different-world", "profile:one")]
    [TestCase("world|one", "different-profile")]
    public void PendingContinuationReconstructionRejectsWrongWorldOrProfile(string worldId, string profileId)
    {
        ResumableBoundaryOwner owner = new ResumableBoundaryOwner { FailStepId = Step(1, "person|b").StepId };
        owner.SelectedSteps = new[] { Step(0, "person|a"), Step(1, "person|b") };
        SimulationTimeline original = ResumableTimeline(owner, new DueOwner());
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(original.TryAdvanceTo(boundary, out _), Is.False);

        DueOwner restoredDue = new DueOwner();
        SimulationTimeline restored = new SimulationTimeline(Calendar(), boundary, restoredDue, boundaryOwner: owner,
            worldId: worldId, profileId: profileId, pendingContinuationId: original.PendingContinuationId);
        owner.Timeline = restored; restoredDue.Timeline = restored;
        Assert.That(restored.TryIndexOwnerFact(Work("ordinary-owner", "ordinary", "activity|instance", 0, boundary.Value), out _), Is.True);
        Assert.That(restored.TrySealInputsThrough(boundary, out _), Is.True);
        Assert.That(restored.TryAdvanceTo(boundary, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        CollectionAssert.AreEqual(new[] { Step(0, "person|a").StepId }, owner.AppliedSteps);
    }

    [TestCase("different-world", "profile:one")]
    [TestCase("world|one", "different-profile")]
    public void RestoredSignalHandoffRejectsWrongWorldOrProfile(string worldId, string profileId)
    {
        ResumableBoundaryOwner owner = new ResumableBoundaryOwner(); owner.SelectedSteps = new[] { Step(0, "person|one") };
        SimulationTimeline original = ResumableTimeline(owner, new DueOwner { CommitSucceeds = false });
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(original.TryAdvanceTo(boundary, out _), Is.False);
        Assert.That(original.PendingSignalHandoffIds, Has.Count.EqualTo(1));

        DueOwner restoredDue = new DueOwner();
        SimulationTimeline restored = new SimulationTimeline(Calendar(), boundary, restoredDue, boundaryOwner: owner,
            worldId: worldId, profileId: profileId, pendingSignalHandoffIds: original.PendingSignalHandoffIds,
            initialCausalSequence: original.CausalSequence);
        owner.Timeline = restored; restoredDue.Timeline = restored; restoredDue.Current.Add("ordinary");
        Assert.That(restored.TryIndexOwnerFact(Work("ordinary-owner", "ordinary", "activity|instance", 0, boundary.Value), out _), Is.True);
        Assert.That(restored.TrySealInputsThrough(boundary, out _), Is.True);
        Assert.That(restored.TryAdvanceTo(boundary, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(owner.HandedOffSignals, Is.Empty);
    }

    [Test]
    public void ReloadedContinuationRejectsChangedFrozenDescriptorContent()
    {
        ResumableBoundaryOwner owner = new ResumableBoundaryOwner
        { FailStepId = Step(1, "person|b").StepId, ChangeManifestAfterStep = true };
        owner.SelectedSteps = new[] { Step(0, "person|a"), Step(1, "person|b") };
        SimulationTimeline timeline = ResumableTimeline(owner, new DueOwner());
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(LogicalTick.TicksPerDay), out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        CollectionAssert.AreEqual(new[] { Step(0, "person|a").StepId }, owner.AppliedSteps);
    }

    [Test]
    public void FrozenManifestValueComparisonRejectsDescriptorReorderingAndIdentityConflicts()
    {
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world|a", "profile:b", 1);
        BoundaryContinuationStep first = Step(0, "person|a");
        BoundaryContinuationStep second = Step(1, "person|b");
        BoundaryContinuationManifest frozen = new BoundaryContinuationManifest(operation, "phase", "v1", "cfg", new[] { first, second }, "content");
        BoundaryContinuationManifest reordered = new BoundaryContinuationManifest(operation, "phase", "v1", "cfg",
            new[] { new BoundaryContinuationStep(0, second.StepId, second.OwnerId, second.OperationKind, second.OperationVersion,
                second.OwnerRevision, second.Payload, second.PersonId, second.Disposition),
                new BoundaryContinuationStep(1, first.StepId, first.OwnerId, first.OperationKind, first.OperationVersion,
                    first.OwnerRevision, first.Payload, first.PersonId, first.Disposition) }, "content");
        Assert.That(frozen.HasSameFrozenContent(reordered), Is.False);
        Assert.That(frozen.GetExecutionStepIdentity(first), Is.Not.EqualTo(frozen.ContinuationId));
        Assert.That(frozen.GetExecutionStepIdentity(first), Is.Not.EqualTo(frozen.BoundaryOccurrenceId));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PublishedFactReconciliationRejectsIndexedIdentityWithDifferentDueOrCausalSequence(bool causalSequenceConflict)
    {
        ResumableBoundaryOwner owner = new ResumableBoundaryOwner(); owner.SelectedSteps = new[] { Step(0, "person|one") };
        SimulationTimeline first = ResumableTimeline(owner, new DueOwner { CommitSucceeds = false });
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(first.TryAdvanceTo(boundary, out _), Is.False);
        string factId = StableKey(Step(0, "person|one").StepId, "fact");
        long restoredSequence = causalSequenceConflict ? 5L : 1L;
        SimulationTimeline restored = new SimulationTimeline(Calendar(), boundary, new DueOwner(), boundaryOwner: owner,
            worldId: "world|one", profileId: "profile:one", pendingSignalHandoffIds: first.PendingSignalHandoffIds,
            initialCausalSequence: restoredSequence);
        LogicalTick conflictingDueAt = causalSequenceConflict ? boundary : new LogicalTick(boundary.Value + 1L);
        Assert.That(restored.TryIndexOwnerFact(new DueWorkReference("z-boundary-owner", factId, "activity|shared", 0, 1,
            conflictingDueAt), out _), Is.True);
        Assert.That(restored.TrySealInputsThrough(boundary, out _), Is.True);
        Assert.That(restored.TryAdvanceTo(boundary, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.PublicationFailed));
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
    public void StaleReferenceDoesNotConsumeCommittedDispatchLimit()
    {
        DueOwner owner = new DueOwner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), owner, maxDispatchesPerInstant: 1);
        owner.Timeline = timeline;
        owner.Current.Add("valid");
        Assert.That(timeline.TryIndexOwnerFact(Work("owner", "obsolete", "instance-a", 0, 9), out _), Is.True);
        Assert.That(timeline.TryIndexOwnerFact(Work("owner", "valid", "instance-b", 0, 9), out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(9), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(9), out TimelineFailure failure), Is.True, failure.ToString());
        CollectionAssert.AreEqual(new[] { "valid" }, owner.Committed);
        Assert.That(timeline.CurrentInstant.Value, Is.EqualTo(9));
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
    public void NextCausalInstantChoosesEarliestInputWorkBoundaryOrTarget()
    {
        InputOwner input = new InputOwner();
        DueOwner due = new DueOwner();
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), due, input);
        due.Timeline = timeline;
        due.Current.Add("work");
        Assert.That(timeline.TryIndexOwnerFact(Work("owner", "work", "instance", 0, 30), out _), Is.True);
        Assert.That(timeline.TryAcceptInput(new TimelineInputReference(0, "input", "Action", "x", new LogicalTick(20)), out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(100), out _), Is.True);
        Assert.That(timeline.TryGetNextCausalInstant(new LogicalTick(100), out LogicalTick next, out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(next.Value, Is.EqualTo(20));

        InputOwner workInput = new InputOwner();
        DueOwner workOwner = new DueOwner(); workOwner.Current.Add("work");
        SimulationTimeline workTimeline = new SimulationTimeline(Calendar(), new LogicalTick(0), workOwner, workInput);
        Assert.That(workTimeline.TryIndexOwnerFact(Work("owner", "work", "instance", 0, 30), out _), Is.True);
        Assert.That(workTimeline.TryAcceptInput(new TimelineInputReference(0, "later", "Action", "x", new LogicalTick(40)), out _), Is.True);
        Assert.That(workTimeline.TrySealInputsThrough(new LogicalTick(100), out _), Is.True);
        Assert.That(workTimeline.TryGetNextCausalInstant(new LogicalTick(100), out next, out failure), Is.True, failure.ToString());
        Assert.That(next.Value, Is.EqualTo(30));

        AtomicBoundaryOwner boundaryOwner = new AtomicBoundaryOwner();
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        SimulationTimeline boundaryTimeline = new SimulationTimeline(Calendar(), new LogicalTick(1),
            boundaryOwner: boundaryOwner, worldId: "world", profileId: "profile");
        Assert.That(boundaryTimeline.TrySealInputsThrough(new LogicalTick(boundary.Value + 10), out _), Is.True);
        Assert.That(boundaryTimeline.TryGetNextCausalInstant(new LogicalTick(boundary.Value + 10), out next, out failure), Is.True, failure.ToString());
        Assert.That(next, Is.EqualTo(boundary));

        Assert.That(boundaryTimeline.TryGetNextCausalInstant(new LogicalTick(boundary.Value + 10), out next, out failure), Is.True, failure.ToString());
        Assert.That(boundaryTimeline.CurrentInstant.Value, Is.EqualTo(1));
        Assert.That(boundaryTimeline.CausalSequence, Is.Zero);
        Assert.That(boundaryTimeline.PendingBoundaryDay, Is.Null);
        Assert.That(boundaryOwner.CommittedIds, Is.Empty);
        Assert.That(timeline.TryGetNextCausalInstant(new LogicalTick(101), out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.InputNotSealed));
        SimulationTimeline advanced = new SimulationTimeline(Calendar(), new LogicalTick(5));
        Assert.That(advanced.TrySealInputsThrough(new LogicalTick(10), out _), Is.True);
        Assert.That(advanced.TryGetNextCausalInstant(new LogicalTick(4), out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.TargetBeforeNow));
    }

    [Test]
    public void NextCausalInstantRetainsPendingBoundaryAndDoesNotChangeSameTickOrdering()
    {
        AtomicBoundaryOwner boundaryOwner = new AtomicBoundaryOwner();
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        SimulationTimeline pending = new SimulationTimeline(Calendar(), boundary,
            boundaryOwner: boundaryOwner, worldId: "world", profileId: "profile", pendingBoundaryDay: 1);
        Assert.That(pending.TrySealInputsThrough(new LogicalTick(boundary.Value + 5), out _), Is.True);
        Assert.That(pending.TryGetNextCausalInstant(new LogicalTick(boundary.Value + 5), out LogicalTick next, out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(next, Is.EqualTo(boundary));
        Assert.That(pending.PendingBoundaryDay, Is.EqualTo(1L));
        Assert.That(boundaryOwner.CommittedIds, Is.Empty);

        List<string> order = new List<string>();
        InputOwner inputOwner = new InputOwner { OnApplied = id => order.Add("input:" + id) };
        DueOwner dueOwner = new DueOwner { OnCommitted = id => order.Add("work:" + id) };
        dueOwner.Current.Add("same-tick");
        SimulationTimeline sameTick = new SimulationTimeline(Calendar(), new LogicalTick(1), dueOwner, inputOwner,
            boundaryOwner, "world", "profile");
        dueOwner.Timeline = sameTick;
        Assert.That(sameTick.TryIndexOwnerFact(Work("owner", "same-tick", "instance", 0, boundary.Value), out _), Is.True);
        Assert.That(sameTick.TryAcceptInput(new TimelineInputReference(0, "same-tick", "Action", "x", boundary), out _), Is.True);
        Assert.That(sameTick.TrySealInputsThrough(boundary, out _), Is.True);
        Assert.That(sameTick.TryGetNextCausalInstant(boundary, out next, out failure), Is.True, failure.ToString());
        Assert.That(next, Is.EqualTo(boundary));
        Assert.That(sameTick.TryAdvanceTo(boundary, out failure), Is.True, failure.ToString());
        CollectionAssert.AreEqual(new[] { "input:same-tick", "work:same-tick" }, order);
        Assert.That(boundaryOwner.Attempts, Has.Count.EqualTo(1));
        Assert.That(boundaryOwner.Effects, Is.EqualTo(new[] { "effect:1" }));
    }

    [Test]
    public void NextCausalInstantYieldsCurrentForRestoredContinuationAndPendingHandoff()
    {
        ResumableBoundaryOwner continuationOwner = new ResumableBoundaryOwner
        { FailStepId = Step(0, "person|pending").StepId };
        continuationOwner.SelectedSteps = new[] { Step(0, "person|pending") };
        SimulationTimeline original = ResumableTimeline(continuationOwner, new DueOwner());
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(original.TryAdvanceTo(boundary, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));

        DueOwner emptyDue = new DueOwner();
        SimulationTimeline restored = new SimulationTimeline(Calendar(), boundary, emptyDue,
            boundaryOwner: continuationOwner, worldId: "world|one", profileId: "profile:one",
            pendingContinuationId: original.PendingContinuationId);
        continuationOwner.Timeline = restored;
        Assert.That(restored.TrySealInputsThrough(new LogicalTick(boundary.Value + 10), out _), Is.True);
        Assert.That(restored.TryGetNextCausalInstant(new LogicalTick(boundary.Value + 10), out LogicalTick next, out failure), Is.True, failure.ToString());
        Assert.That(next, Is.EqualTo(boundary));
        Assert.That(restored.PendingContinuationId, Is.EqualTo(original.PendingContinuationId));
        Assert.That(restored.PreviewInputs(boundary).Count, Is.Zero);
        Assert.That(restored.PreviewDueWork(boundary).Count, Is.Zero);

        ResumableBoundaryOwner handoffOwner = new ResumableBoundaryOwner();
        handoffOwner.SelectedSteps = new[] { Step(0, "person|handoff") };
        SimulationTimeline handoff = ResumableTimeline(handoffOwner, new DueOwner());
        Assert.That(handoff.TryAdvanceTo(boundary, out failure), Is.True, failure.ToString());
        Assert.That(handoff.SuccessfulAdvanceAwaitingHandoff, Is.True);
        Assert.That(handoff.TrySealInputsThrough(new LogicalTick(boundary.Value + 10), out _), Is.True);
        Assert.That(handoff.TryGetNextCausalInstant(new LogicalTick(boundary.Value + 10), out next, out failure), Is.True, failure.ToString());
        Assert.That(next, Is.EqualTo(boundary));
        Assert.That(handoff.SuccessfulAdvanceAwaitingHandoff, Is.True);
        Assert.That(handoff.PendingSignalHandoffIds, Has.Count.EqualTo(1));
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
