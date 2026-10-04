using System;
using System.Linq;
using NUnit.Framework;

public sealed class P20SyntheticOperationTests
{
    private sealed class Rules : IP20SyntheticOperationRules
    {
        public bool Eligible = true;
        public bool ResultsAllowed = true;
        public string FailPersonId;
        public System.Collections.Generic.List<string> Events;
        public bool IsCurrentlyEligible(string personId, ActivityInstanceSnapshot activity, LogicalTick instant, out string revision)
        { revision = "truth:" + personId; return Eligible; }
        public bool TryPrepareResult(string personId, ActivityInstanceSnapshot activity, LogicalTick instant, out string result, out string revision)
        { Events?.Add("p20-start-prepare"); revision = "result:" + personId; result = "distinct:" + personId; return ResultsAllowed && personId != FailPersonId; }
    }

    private sealed class Boundary : IResumableDayBoundaryOwner, IBoundarySourceSignalHandoff
    {
        private readonly System.Collections.Generic.Dictionary<string, BoundaryContinuationState> states =
            new System.Collections.Generic.Dictionary<string, BoundaryContinuationState>(StringComparer.Ordinal);
        public readonly System.Collections.Generic.List<string> Events = new System.Collections.Generic.List<string>();
        public SimulationTimeline Timeline;
        public bool FailFirstStep = true;

        public bool TryPrepare(DailyBoundaryOperation operation, out IDayBoundaryCommit prepared, out TimelineFailure failure)
        { prepared = null; failure = TimelineFailure.DispatchFailed; return false; }
        public bool TryPrepareActivation(DailyBoundaryOperation operation, out IBoundaryActivationCommit prepared, out TimelineFailure failure)
        {
            BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(operation, "p20-test", "v1", "cfg",
                new[] { new BoundaryContinuationStep(0, "step-0", "test-owner", "test", "v1", "r1", "payload") });
            prepared = new Activation(this, manifest); failure = TimelineFailure.None; return true;
        }
        public bool TryResolveContinuation(string continuationId, out BoundaryContinuationState state, out TimelineFailure failure)
        { states.TryGetValue(continuationId, out state); failure = state == null ? TimelineFailure.ContinuationFailed : TimelineFailure.None; return state != null; }
        public bool TryPrepareStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
            out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure)
        { prepared = new Step(this, manifest, step); failure = TimelineFailure.None; return true; }
        public bool TryPrepareTimelinePublication(BoundaryContinuationManifest manifest,
            out IBoundaryTimelinePublicationCommit prepared, out TimelineFailure failure)
        { prepared = new Publication(this, manifest); failure = TimelineFailure.None; return true; }
        public bool TryHandoff(BoundaryContinuationManifest manifest, System.Collections.Generic.IReadOnlyList<string> signals,
            out TimelineFailure failure)
        { Assert.That(Timeline.IsAdvanceInProgress, Is.False); Events.Add("signal-handoff"); failure = TimelineFailure.None; return true; }

        private bool Activate(BoundaryContinuationManifest manifest, out TimelineFailure failure)
        {
            states[manifest.ContinuationId] = new BoundaryContinuationState(manifest, 0, false, false, null, null, null, false);
            Events.Add("activated"); failure = TimelineFailure.None; return true;
        }
        private bool CommitStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step, out TimelineFailure failure)
        {
            if (FailFirstStep) { FailFirstStep = false; failure = TimelineFailure.ContinuationFailed; return false; }
            states[manifest.ContinuationId] = new BoundaryContinuationState(manifest, 1, true, false, null,
                Array.Empty<DueWorkReference>(), new[] { "signal-1" }, false);
            Events.Add("continuation-step"); failure = TimelineFailure.None; return true;
        }
        private bool Publish(BoundaryContinuationManifest manifest, out TimelineFailure failure)
        {
            BoundaryContinuationState current = states[manifest.ContinuationId];
            states[manifest.ContinuationId] = new BoundaryContinuationState(manifest, current.NextStepOrdinal, true,
                true, Array.Empty<BoundaryPublishedFact>(), current.RetainedTimelineFacts, current.RetainedSourceSignals, false);
            Events.Add("timeline-facts-published"); failure = TimelineFailure.None; return true;
        }
        private sealed class Activation : IBoundaryActivationCommit
        {
            private readonly Boundary owner;
            public BoundaryContinuationManifest Manifest { get; }
            public Activation(Boundary owner, BoundaryContinuationManifest manifest) { this.owner = owner; Manifest = manifest; }
            public bool TryCommit(out TimelineFailure failure) => owner.Activate(Manifest, out failure);
        }
        private sealed class Step : IBoundaryContinuationStepCommit
        {
            private readonly Boundary owner; private readonly BoundaryContinuationManifest manifest; private readonly BoundaryContinuationStep step;
            public Step(Boundary owner, BoundaryContinuationManifest manifest, BoundaryContinuationStep step)
            { this.owner = owner; this.manifest = manifest; this.step = step; }
            public System.Collections.Generic.IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
            public System.Collections.Generic.IReadOnlyList<string> RetainedSourceSignals => new[] { "signal-1" };
            public bool TryCommit(out TimelineFailure failure) => owner.CommitStep(manifest, step, out failure);
        }
        private sealed class Publication : IBoundaryTimelinePublicationCommit
        {
            private readonly Boundary owner; private readonly BoundaryContinuationManifest manifest;
            public Publication(Boundary owner, BoundaryContinuationManifest manifest) { this.owner = owner; this.manifest = manifest; }
            public bool TryCommit(System.Collections.Generic.IReadOnlyList<BoundaryPublishedFact> facts, out TimelineFailure failure) =>
                owner.Publish(manifest, out failure);
        }
    }

    private static SimulationCalendar Calendar() => new SimulationCalendar(new CalendarDefinition(2, 2, 3));
    private static ActivityDefinition Definition() => new ActivityDefinition("synthetic-op", "v1");
    private static ActivityLifecycleComposition Compose(ActivityLifecycleStore store, long initial = 0) =>
        new ActivityLifecycleComposition(store, Calendar(), new LogicalTick(initial));
    private static P20SyntheticOperationOwner NewOwner(out ActivityLifecycleStore store, out SimulationTimeline timeline,
        Rules rules = null, long initial = 0)
    {
        store = new ActivityLifecycleStore("p20-world");
        ActivityLifecycleComposition composition = Compose(store, initial);
        timeline = composition.Timeline;
        return new P20SyntheticOperationOwner(composition, rules ?? new Rules());
    }
    private static bool CreateAccepted(P20SyntheticOperationOwner owner, string key, out P20SyntheticOperationSnapshot operation)
    {
        Assert.That(owner.TryCreate(Definition(), key, new[] { "person-b", "person-a" }, out operation, out _), Is.True);
        Assert.That(owner.TryRecordDecision(operation.ActivityInstanceId, new P20ActorDecision("person-a", true, "knowledge-a", "boundary-a")), Is.True);
        Assert.That(owner.TryRecordDecision(operation.ActivityInstanceId, new P20ActorDecision("person-b", true, "knowledge-b", "boundary-b")), Is.True);
        return true;
    }

    [Test]
    public void FixtureRequiresTwoDistinctSemanticPersonsBeforePublishingProposedInstance()
    {
        P20SyntheticOperationOwner owner = NewOwner(out ActivityLifecycleStore store, out _);
        Assert.That(owner.TryCreate(Definition(), "duplicate-ids", new[] { "person-a", "person-a" }, out _, out _), Is.False);
        Assert.That(store.SnapshotInstances(), Is.Empty);
        Assert.That(owner.TryCreate(Definition(), "two", new[] { "person-b", "person-a" }, out P20SyntheticOperationSnapshot created, out _), Is.True);
        Assert.That(created.Disposition, Is.EqualTo(P20SyntheticOperationDisposition.Proposed));
        Assert.That(created.RequiredPersonIds, Is.EqualTo(new[] { "person-a", "person-b" }));
        Assert.That(created.ActivityInstanceId, Is.Not.EqualTo("person-a"));
        Assert.That(created.Decisions, Is.Empty);
    }

    [Test]
    public void IndependentPartialDecisionRemainsProposedUntilExplicitCloseThenMissingResponseFormsNothing()
    {
        P20SyntheticOperationOwner owner = NewOwner(out ActivityLifecycleStore store, out SimulationTimeline timeline);
        owner.TryCreate(Definition(), "partial", new[] { "a", "b" }, out P20SyntheticOperationSnapshot proposed, out _);
        Assert.That(owner.TryRecordDecision(proposed.ActivityInstanceId, new P20ActorDecision("a", true, "ka", "ca")), Is.True);
        Assert.That(owner.TryRecordDecision(proposed.ActivityInstanceId, new P20ActorDecision("a", false, "ka2", "ca2")), Is.False);
        Assert.That(owner.TryGet(proposed.ActivityInstanceId, out P20SyntheticOperationSnapshot partial), Is.True);
        Assert.That(partial.Disposition, Is.EqualTo(P20SyntheticOperationDisposition.Proposed));
        Assert.That(partial.Decisions.Single().KnowledgeBoundaryId, Is.EqualTo("ka"));
        Assert.That(owner.TryCloseFormation(proposed.ActivityInstanceId, new LogicalTick(5), new LogicalTick(3), out P20SyntheticOperationSnapshot closed, out _), Is.True);
        Assert.That(closed.Disposition, Is.EqualTo(P20SyntheticOperationDisposition.NotFormed));
        Assert.That(store.TryGet(proposed.ActivityInstanceId, out ActivityInstanceSnapshot instance), Is.True);
        Assert.That(instance.State, Is.EqualTo(ActivityLifecycleState.Proposed));
        Assert.That(store.PendingWork, Is.Empty);
        Assert.That(store.SnapshotTransitionReceipts().Select(x => x.Kind), Is.Empty);
        Assert.That(timeline.CurrentInstant.Value, Is.EqualTo(0));
    }

    [Test]
    public void DeclineLateSealedAndStaleEligibilityCloseWithoutPartialSchedule()
    {
        foreach (string mode in new[] { "decline", "late", "stale" })
        {
            Rules rules = new Rules { Eligible = mode != "stale" };
            P20SyntheticOperationOwner owner = NewOwner(out ActivityLifecycleStore store, out SimulationTimeline timeline, rules);
            owner.TryCreate(Definition(), mode, new[] { "a", "b" }, out P20SyntheticOperationSnapshot op, out _);
            owner.TryRecordDecision(op.ActivityInstanceId, new P20ActorDecision("a", true, "ka", "ca"));
            owner.TryRecordDecision(op.ActivityInstanceId, new P20ActorDecision("b", mode != "decline", "kb", "cb"));
            LogicalTick requested = mode == "late" ? new LogicalTick(0) : new LogicalTick(5);
            owner.TryCloseFormation(op.ActivityInstanceId, requested, new LogicalTick(3), out P20SyntheticOperationSnapshot result, out _);
            Assert.That(result.Disposition, Is.EqualTo(P20SyntheticOperationDisposition.NotFormed), mode);
            Assert.That(store.PendingWork, Is.Empty, mode);
            Assert.That(store.SnapshotTransitionReceipts().Select(x => x.Kind), Is.Empty, mode);
            Assert.That(timeline.CurrentInstant.Value, Is.EqualTo(0), mode);
        }
    }

    [Test]
    public void CompleteSetUsesOneScheduleAndStableIdentityAndOrder()
    {
        P20SyntheticOperationOwner owner = NewOwner(out ActivityLifecycleStore store, out _);
        CreateAccepted(owner, "complete-set", out P20SyntheticOperationSnapshot op);
        Assert.That(owner.TryCloseFormation(op.ActivityInstanceId, new LogicalTick(5), new LogicalTick(3), out P20SyntheticOperationSnapshot scheduled, out _), Is.True);
        Assert.That(scheduled.Disposition, Is.EqualTo(P20SyntheticOperationDisposition.Scheduled));
        Assert.That(store.TryGet(op.ActivityInstanceId, out ActivityInstanceSnapshot instance), Is.True);
        Assert.That(instance.State, Is.EqualTo(ActivityLifecycleState.Scheduled));
        Assert.That(instance.Participants, Is.EqualTo(new[] { "person-a", "person-b" }));
        Assert.That(store.SnapshotTransitionReceipts().Select(x => x.Kind), Is.EqualTo(new[] { ActivityTransitionKind.Schedule }));
        Assert.That(store.GetCommitment("person-a").ActivityInstanceId, Is.EqualTo(op.ActivityInstanceId));
        Assert.That(store.GetCommitment("person-b").ActivityInstanceId, Is.EqualTo(op.ActivityInstanceId));
        Assert.That(store.PendingWork.Count, Is.EqualTo(2));
    }

    [Test]
    public void ReservationConflictAndSealedStartDoNotPublishEitherParticipantCommitment()
    {
        foreach (bool conflict in new[] { true, false })
        {
            P20SyntheticOperationOwner owner = NewOwner(out ActivityLifecycleStore store, out SimulationTimeline timeline);
            if (conflict)
            {
                store.TryPropose(Definition(), "existing", out ActivityInstanceSnapshot existing, out _);
                store.TrySchedule(timeline, existing.Id, timeline.CurrentInstant, new LogicalTick(5), new LogicalTick(3), new[] { "a" }, out _);
            }
            owner.TryCreate(Definition(), "candidate" + conflict, new[] { "a", "b" }, out P20SyntheticOperationSnapshot op, out _);
            owner.TryRecordDecision(op.ActivityInstanceId, new P20ActorDecision("a", true, "ka", "ca"));
            owner.TryRecordDecision(op.ActivityInstanceId, new P20ActorDecision("b", true, "kb", "cb"));
            if (!conflict) timeline.TrySealInputsThrough(new LogicalTick(5), out _) ;
            owner.TryCloseFormation(op.ActivityInstanceId, new LogicalTick(5), new LogicalTick(3), out P20SyntheticOperationSnapshot result, out _);
            Assert.That(result.Disposition, Is.EqualTo(P20SyntheticOperationDisposition.NotFormed));
            Assert.That(store.TryGet(op.ActivityInstanceId, out ActivityInstanceSnapshot candidate), Is.True);
            Assert.That(candidate.State, Is.EqualTo(ActivityLifecycleState.Proposed));
            Assert.That(store.GetCommitment("b"), Is.Null);
            Assert.That(store.PendingWork.Count, Is.EqualTo(conflict ? 2 : 0));
        }
    }

    [Test]
    public void CheckedNextTickOverflowLeavesProposedOperationWithoutTimelineFacts()
    {
        P20SyntheticOperationOwner owner = NewOwner(out ActivityLifecycleStore store, out _, initial: long.MaxValue);
        owner.TryCreate(Definition(), "overflow", new[] { "a", "b" }, out P20SyntheticOperationSnapshot op, out _);
        owner.TryRecordDecision(op.ActivityInstanceId, new P20ActorDecision("a", true, "ka", "ca"));
        owner.TryRecordDecision(op.ActivityInstanceId, new P20ActorDecision("b", true, "kb", "cb"));
        Assert.That(owner.TryCloseFormation(op.ActivityInstanceId, new LogicalTick(long.MaxValue), new LogicalTick(1), out P20SyntheticOperationSnapshot result, out ActivityFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ActivityFailure.InvalidInterval));
        Assert.That(result.Disposition, Is.EqualTo(P20SyntheticOperationDisposition.NotFormed));
        Assert.That(store.PendingWork, Is.Empty);
    }

    [Test]
    public void SuccessfulStartCommitsDistinctOrderedResultsTogetherAndRetryCannotReapply()
    {
        P20SyntheticOperationOwner owner = NewOwner(out ActivityLifecycleStore store, out SimulationTimeline timeline);
        CreateAccepted(owner, "start-success", out P20SyntheticOperationSnapshot op);
        owner.TryCloseFormation(op.ActivityInstanceId, new LogicalTick(5), new LogicalTick(3), out _, out _);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(5), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(owner.TryGet(op.ActivityInstanceId, out P20SyntheticOperationSnapshot active), Is.True);
        Assert.That(active.Disposition, Is.EqualTo(P20SyntheticOperationDisposition.Started));
        Assert.That(active.AppliedResults.Select(x => x.PersonId), Is.EqualTo(new[] { "person-a", "person-b" }));
        Assert.That(active.AppliedResults.Select(x => x.Result), Is.EqualTo(new[] { "distinct:person-a", "distinct:person-b" }));
        Assert.That(active.AppliedResults.Select(x => x.AppliedIdentity).Distinct().Count(), Is.EqualTo(2));
        Assert.That(store.TryGet(op.ActivityInstanceId, out ActivityInstanceSnapshot instance), Is.True);
        Assert.That(instance.State, Is.EqualTo(ActivityLifecycleState.Active));
        Assert.That(store.SnapshotTransitionReceipts().Select(x => x.Kind), Is.EqualTo(new[] { ActivityTransitionKind.Schedule, ActivityTransitionKind.Start }));
        Assert.That(store.GetCommitment("person-a"), Is.Not.Null);
        Assert.That(store.GetCommitment("person-b"), Is.Not.Null);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out TimelineFailure retryFailure), Is.True, retryFailure.ToString());
        Assert.That(owner.TryGet(op.ActivityInstanceId, out P20SyntheticOperationSnapshot retried), Is.True);
        Assert.That(retried.AppliedResults.Count, Is.EqualTo(2));
        ActivityLifecycleStore clonedStore = store.Clone();
        ActivityLifecycleComposition clonedComposition = Compose(clonedStore, 5);
        P20SyntheticOperationOwner clonedOwner = owner.Clone(clonedComposition);
        Assert.That(clonedStore.TryGet(op.ActivityInstanceId, out ActivityInstanceSnapshot clonedActivity), Is.True);
        Assert.That(clonedActivity.State, Is.EqualTo(ActivityLifecycleState.Active));
        Assert.That(clonedOwner.TryGet(op.ActivityInstanceId, out P20SyntheticOperationSnapshot reconstructed), Is.True);
        Assert.That(reconstructed.AppliedResults.Select(x => x.AppliedIdentity), Is.EqualTo(retried.AppliedResults.Select(x => x.AppliedIdentity)));
        Assert.That(reconstructed.Decisions.Select(x => x.KnowledgeBoundaryId), Is.EqualTo(new[] { "knowledge-a", "knowledge-b" }));
    }

    [Test]
    public void FailedStartAndExplicitCancelReleaseBothCommitmentsAndInvalidateDueWorkCoherently()
    {
        foreach (bool cancel in new[] { true, false })
        {
            Rules rules = new Rules();
            P20SyntheticOperationOwner owner = NewOwner(out ActivityLifecycleStore store, out SimulationTimeline timeline, rules);
            CreateAccepted(owner, "terminal" + cancel, out P20SyntheticOperationSnapshot op);
            owner.TryCloseFormation(op.ActivityInstanceId, new LogicalTick(5), new LogicalTick(3), out _, out _);
            if (cancel) Assert.That(owner.TryCancel(op.ActivityInstanceId, "test-cancel", out _), Is.True);
            else
            {
                rules.Eligible = false;
                Assert.That(timeline.TrySealInputsThrough(new LogicalTick(5), out _), Is.True);
                Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out TimelineFailure failure), Is.True, failure.ToString());
            }
            Assert.That(owner.TryGet(op.ActivityInstanceId, out P20SyntheticOperationSnapshot terminal), Is.True);
            Assert.That(terminal.Disposition, Is.EqualTo(cancel ? P20SyntheticOperationDisposition.Cancelled : P20SyntheticOperationDisposition.FailedToStart));
            Assert.That(terminal.AppliedResults, Is.Empty);
            Assert.That(store.TryGet(op.ActivityInstanceId, out ActivityInstanceSnapshot instance), Is.True);
            Assert.That(instance.State, Is.EqualTo(ActivityLifecycleState.Cancelled));
            Assert.That(store.GetCommitment("person-a"), Is.Null);
            Assert.That(store.GetCommitment("person-b"), Is.Null);
            Assert.That(store.PendingWork, Is.Empty);
            Assert.That(store.SnapshotTransitionReceipts().Last().Kind,
                Is.EqualTo(cancel ? ActivityTransitionKind.Cancel : ActivityTransitionKind.FailedStart));
        }
    }

    [Test]
    public void AttachedP20OwnerDoesNotChangeExistingSingleParticipantP18LifecycleBehavior()
    {
        P20SyntheticOperationOwner owner = NewOwner(out ActivityLifecycleStore store, out SimulationTimeline timeline);
        store.TryPropose(Definition(), "ordinary-p18", out ActivityInstanceSnapshot instance, out _);
        Assert.That(store.TrySchedule(timeline, instance.Id, timeline.CurrentInstant, new LogicalTick(5), new LogicalTick(2),
            new[] { "single-person" }, out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(5), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot active), Is.True);
        Assert.That(active.State, Is.EqualTo(ActivityLifecycleState.Active));
        Assert.That(owner.TryGet(instance.Id, out _), Is.False);
    }

    [Test]
    public void FailurePreparingSecondParticipantResultPublishesNeitherEffectAndUsesFailedStartRelease()
    {
        Rules rules = new Rules();
        P20SyntheticOperationOwner owner = NewOwner(out ActivityLifecycleStore store, out SimulationTimeline timeline, rules);
        CreateAccepted(owner, "second-result-failure", out P20SyntheticOperationSnapshot op);
        owner.TryCloseFormation(op.ActivityInstanceId, new LogicalTick(5), new LogicalTick(3), out _, out _);
        rules.FailPersonId = "person-b";
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(5), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(owner.TryGet(op.ActivityInstanceId, out P20SyntheticOperationSnapshot terminal), Is.True);
        Assert.That(terminal.Disposition, Is.EqualTo(P20SyntheticOperationDisposition.FailedToStart));
        Assert.That(terminal.AppliedResults, Is.Empty);
        Assert.That(store.GetCommitment("person-a"), Is.Null);
        Assert.That(store.GetCommitment("person-b"), Is.Null);
        Assert.That(store.SnapshotTransitionReceipts().Last().Kind, Is.EqualTo(ActivityTransitionKind.FailedStart));
    }

    [Test]
    public void BoundaryContinuationBlocksP20StartPublishesBeforeStartAndDefersSignalsUntilSuccessfulReturn()
    {
        Boundary boundary = new Boundary();
        ActivityLifecycleStore store = new ActivityLifecycleStore("p20-world");
        ActivityLifecycleComposition composition = new ActivityLifecycleComposition(store, Calendar(), new LogicalTick(0), boundary,
            "p20-world", "profile");
        boundary.Timeline = composition.Timeline;
        Rules rules = new Rules { Events = boundary.Events };
        P20SyntheticOperationOwner owner = new P20SyntheticOperationOwner(composition, rules);
        CreateAccepted(owner, "barrier", out P20SyntheticOperationSnapshot operation);
        LogicalTick boundaryTick = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(owner.TryCloseFormation(operation.ActivityInstanceId, boundaryTick, null, out _, out _), Is.True);
        Assert.That(composition.Timeline.TrySealInputsThrough(boundaryTick, out _), Is.True);

        Assert.That(composition.Timeline.TryAdvanceTo(boundaryTick, out TimelineFailure firstFailure), Is.False);
        Assert.That(firstFailure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(owner.TryGet(operation.ActivityInstanceId, out P20SyntheticOperationSnapshot waiting), Is.True);
        Assert.That(waiting.Disposition, Is.EqualTo(P20SyntheticOperationDisposition.Scheduled));
        Assert.That(waiting.AppliedResults, Is.Empty);
        Assert.That(boundary.Events, Does.Not.Contain("p20-start-prepare"));
        Assert.That(boundary.Events, Does.Not.Contain("signal-handoff"));

        Assert.That(composition.Timeline.TryAdvanceTo(boundaryTick, out TimelineFailure retryFailure), Is.True, retryFailure.ToString());
        Assert.That(owner.TryGet(operation.ActivityInstanceId, out P20SyntheticOperationSnapshot started), Is.True);
        Assert.That(started.Disposition, Is.EqualTo(P20SyntheticOperationDisposition.Started));
        Assert.That(boundary.Events.IndexOf("timeline-facts-published"), Is.LessThan(boundary.Events.IndexOf("p20-start-prepare")));
        Assert.That(boundary.Events, Does.Not.Contain("signal-handoff"));
        Assert.That(composition.Timeline.TryCompleteSuccessfulAdvanceHandoffs(out TimelineFailure handoffFailure), Is.True, handoffFailure.ToString());
        Assert.That(boundary.Events.Last(), Is.EqualTo("signal-handoff"));
    }
}
