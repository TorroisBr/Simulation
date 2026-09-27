using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class ActivityLifecycleTests
{
    private sealed class MutableStartValidator : IActivityStartValidator
    {
        public bool Available = true;
        public bool TryValidate(ActivityInstanceSnapshot instance, LogicalTick instant, out string disposition)
        { disposition = Available ? null : "participant-became-unavailable"; return Available; }
    }
    private static ActivityDefinition Definition() => new ActivityDefinition("work", "v1");
    private static SimulationCalendar Calendar() => new SimulationCalendar(new CalendarDefinition(2, 2, 3));
    private static string Key(string first, string second) => first.Length + ":" + first + second.Length + ":" + second;
    private static ActivityLifecycleComposition Compose(ActivityLifecycleStore store, long initial = 0) =>
        new ActivityLifecycleComposition(store, Calendar(), new LogicalTick(initial));

    [Test]
    public void DefinitionInstanceAndParticipantIdentitiesAreSeparateAndProposedMayBeUnformed()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        Assert.That(store.TryPropose(Definition(), "command:1", out ActivityInstanceSnapshot proposed, out _), Is.True);
        Assert.That(proposed.State, Is.EqualTo(ActivityLifecycleState.Proposed));
        Assert.That(proposed.Participants, Is.Empty);
        Assert.That(proposed.Id, Is.Not.EqualTo(proposed.DefinitionId));
        Assert.That(proposed.Id, Is.Not.EqualTo("person-1"));
        Assert.That(store.TryPropose(Definition(), "command:1", out _, out ActivityFailure duplicate), Is.False);
        Assert.That(duplicate, Is.EqualTo(ActivityFailure.DuplicateCreation));
    }

    [Test]
    public void ScheduleRequiresParticipantsAndPublishesCommitmentAndTypedDueFacts()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        SimulationTimeline timeline = Compose(store).Timeline;
        store.TryPropose(Definition(), "command:1", out ActivityInstanceSnapshot proposed, out _);
        Assert.That(store.TrySchedule(timeline, proposed.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(5),
            Array.Empty<string>(), out ActivityFailure noParticipants), Is.False);
        Assert.That(noParticipants, Is.EqualTo(ActivityFailure.NoParticipants));
        Assert.That(store.TryGet(proposed.Id, out ActivityInstanceSnapshot unchanged), Is.True);
        Assert.That(unchanged.State, Is.EqualTo(ActivityLifecycleState.Proposed));
        Assert.That(store.TrySchedule(timeline, proposed.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(5),
            new[] { "person-1" }, out _), Is.True);
        Assert.That(store.TryGet(proposed.Id, out ActivityInstanceSnapshot scheduled), Is.True);
        Assert.That(scheduled.State, Is.EqualTo(ActivityLifecycleState.Scheduled));
        Assert.That(scheduled.Participants, Is.EqualTo(new[] { "person-1" }));
        Assert.That(store.GetCommitment("person-1").Start.Value, Is.EqualTo(10));
        Assert.That(store.IsAvailable("person-1", new LogicalTick(9)), Is.True);
        Assert.That(store.IsAvailable("person-1", new LogicalTick(10)), Is.False);
        Assert.That(store.IsAvailable("person-1", new LogicalTick(15)), Is.True);
        Assert.That(store.PendingWork.Count, Is.EqualTo(2));
        Assert.That(store.PendingWork[0].DueWorkId, Does.EndWith("5:start"));
        Assert.That(store.PendingWork[1].DueWorkId, Does.EndWith("8:complete"));
    }

    [Test]
    public void SchedulingRejectsConflictingParticipantWithoutPartialState()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        SimulationTimeline timeline = Compose(store).Timeline;
        store.TryPropose(Definition(), "first", out ActivityInstanceSnapshot first, out _);
        store.TryPropose(Definition(), "second", out ActivityInstanceSnapshot second, out _);
        Assert.That(store.TrySchedule(timeline, first.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(10), new[] { "person" }, out _), Is.True);
        int pending = store.PendingWork.Count;
        Assert.That(store.TrySchedule(timeline, second.Id, new LogicalTick(0), new LogicalTick(15), new LogicalTick(2), new[] { "person" }, out ActivityFailure conflict), Is.False);
        Assert.That(conflict, Is.EqualTo(ActivityFailure.ParticipantConflict));
        Assert.That(store.TryGet(second.Id, out ActivityInstanceSnapshot unchanged), Is.True);
        Assert.That(unchanged.State, Is.EqualTo(ActivityLifecycleState.Proposed));
        Assert.That(store.PendingWork.Count, Is.EqualTo(pending));
    }

    [Test]
    public void TimelineIndexValidationFailureLeavesScheduleFactsUnpublished()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        SimulationTimeline timeline = Compose(store).Timeline;
        store.TryPropose(Definition(), "rejected", out ActivityInstanceSnapshot instance, out _);
        DueWorkReference collision = new DueWorkReference(ActivityLifecycleStore.DueOwnerId, Key(instance.Id, "start"), "other-instance", 9, 1, new LogicalTick(10));
        Assert.That(timeline.TryIndexOwnerFact(collision, out _), Is.True);
        long sequenceBefore = timeline.CausalSequence;
        Assert.That(store.TrySchedule(timeline, instance.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(5), new[] { "person" }, out ActivityFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ActivityFailure.TimelinePublicationFailed));
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot unchanged), Is.True);
        Assert.That(unchanged.State, Is.EqualTo(ActivityLifecycleState.Proposed));
        Assert.That(store.GetCommitment("person"), Is.Null);
        Assert.That(store.PendingWork, Is.Empty);
        Assert.That(timeline.CausalSequence, Is.EqualTo(sequenceBefore));
    }

    [Test]
    public void CancellationInvalidatesPendingStartAndReleasesCommitment()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        SimulationTimeline timeline = Compose(store).Timeline;
        store.TryPropose(Definition(), "first", out ActivityInstanceSnapshot instance, out _);
        store.TrySchedule(timeline, instance.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(10), new[] { "person" }, out _);
        DueWorkReference oldWork = store.PendingWork[0];
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(5), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(5), out _), Is.True);
        Assert.That(store.TryCancel(timeline, instance.Id, new LogicalTick(4), "past", out ActivityFailure past), Is.False);
        Assert.That(past, Is.EqualTo(ActivityFailure.InvalidInstant));
        Assert.That(store.TryCancel(timeline, instance.Id, new LogicalTick(6), "future", out ActivityFailure future), Is.False);
        Assert.That(future, Is.EqualTo(ActivityFailure.InvalidInstant));
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot stillScheduled), Is.True);
        Assert.That(stillScheduled.State, Is.EqualTo(ActivityLifecycleState.Scheduled));
        Assert.That(store.GetCommitment("person"), Is.Not.Null);
        Assert.That(store.PendingWork.Count, Is.EqualTo(2));
        Assert.That(store.TryCancel(timeline, instance.Id, "consumer-cancelled", out _), Is.True);
        Assert.That(store.IsCurrent(oldWork), Is.False);
        Assert.That(store.GetCommitment("person"), Is.Null);
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot cancelled), Is.True);
        Assert.That(cancelled.State, Is.EqualTo(ActivityLifecycleState.Cancelled));
        Assert.That(cancelled.Disposition, Is.EqualTo("consumer-cancelled"));
    }

    [Test]
    public void InterruptIsOnlyValidForActiveInstances()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        SimulationTimeline timeline = Compose(store).Timeline;
        store.TryPropose(Definition(), "interrupt", out ActivityInstanceSnapshot instance, out _);
        Assert.That(store.TryInterrupt(timeline, instance.Id, "early", out ActivityFailure proposedFailure), Is.False);
        Assert.That(proposedFailure, Is.EqualTo(ActivityFailure.InvalidState));
        Assert.That(store.TrySchedule(timeline, instance.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(10), new[] { "person" }, out _), Is.True);
        Assert.That(store.TryInterrupt(timeline, instance.Id, "early", out ActivityFailure scheduledFailure), Is.False);
        Assert.That(scheduledFailure, Is.EqualTo(ActivityFailure.InvalidState));
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(10), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(10), out _), Is.True);
        Assert.That(store.TryInterrupt(timeline, instance.Id, "condition-changed", out _), Is.True);
        Assert.That(store.GetCommitment("person"), Is.Null);
        Assert.That(store.PendingWork, Is.Empty);
    }

    [Test]
    public void FailedStartValidationCancelsAndReleasesCommitmentWithoutActivation()
    {
        MutableStartValidator validator = new MutableStartValidator();
        ActivityLifecycleStore store = new ActivityLifecycleStore("world", startValidator: validator);
        SimulationTimeline timeline = Compose(store).Timeline;
        store.TryPropose(Definition(), "invalid-at-start", out ActivityInstanceSnapshot instance, out _);
        Assert.That(store.TrySchedule(timeline, instance.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(5), new[] { "person" }, out _), Is.True);
        validator.Available = false;
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(15), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(15), out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot cancelled), Is.True);
        Assert.That(cancelled.State, Is.EqualTo(ActivityLifecycleState.Cancelled));
        Assert.That(cancelled.ActualStart, Is.Null);
        Assert.That(cancelled.Disposition, Is.EqualTo("participant-became-unavailable"));
        Assert.That(store.GetCommitment("person"), Is.Null);
        Assert.That(store.PendingWork, Is.Empty);
    }

    [Test]
    public void IdentitySequenceAndParticipantRelationsSurviveStoreReconstructionInputs()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world", 7);
        store.TryPropose(Definition(), "stable-command", out ActivityInstanceSnapshot instance, out _);
        Assert.That(instance.Id, Is.EqualTo("5:world1:7"));
        Assert.That(store.NextIdentity, Is.EqualTo(8));
        ActivityLifecycleStore reconstructed = new ActivityLifecycleStore("world", store.NextIdentity);
        Assert.That(reconstructed.NextIdentity, Is.EqualTo(8));
    }

    [Test]
    public void StaleScheduledEndIsSkippedAndReplacementCompletionRunsExactlyOnce()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        SimulationTimeline timeline = Compose(store).Timeline;
        store.TryPropose(Definition(), "start-end", out ActivityInstanceSnapshot instance, out _);
        Assert.That(store.TrySchedule(timeline, instance.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(5), new[] { "person" }, out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(15), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(15), out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot completed), Is.True);
        Assert.That(completed.State, Is.EqualTo(ActivityLifecycleState.Completed));
        Assert.That(completed.ActualStart.Value.Value, Is.EqualTo(10));
        Assert.That(completed.TerminalInstant.Value.Value, Is.EqualTo(15));
        Assert.That(store.GetCommitment("person"), Is.Null);
        Assert.That(store.PendingWork, Is.Empty);
    }

    [Test]
    public void FailedOwnerCommitDoesNotPublishTimelineIndexOrCausalSequence()
    {
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0));
        DueWorkReference fact = new DueWorkReference("owner", "work", "instance", 1, 1, new LogicalTick(10));
        long before = timeline.CausalSequence;
        Assert.That(timeline.TryCommitOwnerFacts(new[] { fact }, () => TimelineFailure.DispatchFailed, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.DispatchFailed));
        Assert.That(timeline.CausalSequence, Is.EqualTo(before));
        Assert.That(timeline.IsDue(new LogicalTick(10), "work"), Is.False);
    }

    [Test]
    public void ReconstructedStoreCloneRebuildsSamePendingOrderAndCompletesIndependently()
    {
        ActivityLifecycleStore original = new ActivityLifecycleStore("world");
        SimulationTimeline originalTimeline = Compose(original).Timeline;
        original.TryPropose(Definition(), "fork-source", out ActivityInstanceSnapshot instance, out _);
        Assert.That(original.TrySchedule(originalTimeline, instance.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(5), new[] { "person" }, out _), Is.True);
        ActivityLifecycleStore fork = original.Clone();
        SimulationTimeline forkTimeline = Compose(fork).Timeline;
        Assert.That(fork.TryRebuildTimelineIndex(forkTimeline, out TimelineFailure rebuildFailure), Is.True, rebuildFailure.ToString());
        Assert.That(forkTimeline.PreviewDueWork(new LogicalTick(15)).Count, Is.EqualTo(1));
        Assert.That(forkTimeline.TrySealInputsThrough(new LogicalTick(15), out _), Is.True);
        Assert.That(forkTimeline.TryAdvanceTo(new LogicalTick(10), out TimelineFailure startFailure), Is.True, startFailure.ToString());
        Assert.That(forkTimeline.NextDueInstant.Value.Value, Is.EqualTo(15));
        Assert.That(forkTimeline.TryAdvanceTo(new LogicalTick(15), out TimelineFailure advanceFailure), Is.True, advanceFailure.ToString());
        Assert.That(fork.TryGet(instance.Id, out ActivityInstanceSnapshot forkResult), Is.True);
        Assert.That(forkResult.State, Is.EqualTo(ActivityLifecycleState.Completed));
        Assert.That(original.TryGet(instance.Id, out ActivityInstanceSnapshot sourceResult), Is.True);
        Assert.That(sourceResult.State, Is.EqualTo(ActivityLifecycleState.Scheduled));
    }

    [Test]
    public void ForeignTimelineCannotClaimFirstBindingForTermination()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        store.TryPropose(Definition(), "unbound", out ActivityInstanceSnapshot instance, out _);
        SimulationTimeline foreign = new SimulationTimeline(Calendar(), new LogicalTick(77), store);
        Assert.That(store.TryCancel(foreign, instance.Id, "foreign", out ActivityFailure cancelFailure), Is.False);
        Assert.That(cancelFailure, Is.EqualTo(ActivityFailure.TimelineMismatch));
        Assert.That(store.TryInterrupt(foreign, instance.Id, "foreign", out ActivityFailure interruptFailure), Is.False);
        Assert.That(interruptFailure, Is.EqualTo(ActivityFailure.TimelineMismatch));
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot unchanged), Is.True);
        Assert.That(unchanged.State, Is.EqualTo(ActivityLifecycleState.Proposed));
        Assert.That(unchanged.Revision, Is.Zero);
        Assert.That(store.GetCommitment("person"), Is.Null);
        Assert.That(store.PendingWork, Is.Empty);

        ActivityLifecycleComposition owner = Compose(store, 2);
        Assert.That(store.TryCancel(owner.Timeline, instance.Id, "owner-cancelled", out _), Is.True);
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot cancelled), Is.True);
        Assert.That(cancelled.TerminalInstant.Value, Is.EqualTo(new LogicalTick(2)));
    }

    [Test]
    public void ForeignTimelineCannotDispatchBoundActivityFacts()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        SimulationTimeline ownerTimeline = Compose(store).Timeline;
        store.TryPropose(Definition(), "owned-dispatch", out ActivityInstanceSnapshot instance, out _);
        Assert.That(store.TrySchedule(ownerTimeline, instance.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(5), new[] { "person" }, out _), Is.True);
        DueWorkReference[] authoritativeBefore = new List<DueWorkReference>(store.PendingWork).ToArray();
        ActivityParticipantCommitment commitmentBefore = store.GetCommitment("person");

        SimulationTimeline foreign = new SimulationTimeline(Calendar(), new LogicalTick(0), store);
        Assert.That(foreign.TryIndexOwnerFacts(authoritativeBefore, out TimelineFailure indexed), Is.True, indexed.ToString());
        Assert.That(foreign.TrySealInputsThrough(new LogicalTick(10), out _), Is.True);
        Assert.That(foreign.TryAdvanceTo(new LogicalTick(10), out TimelineFailure rejected), Is.False);
        Assert.That(rejected, Is.EqualTo(TimelineFailure.DispatchFailed));
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot unchanged), Is.True);
        Assert.That(unchanged.State, Is.EqualTo(ActivityLifecycleState.Scheduled));
        Assert.That(unchanged.Revision, Is.EqualTo(instance.Revision + 1));
        Assert.That(store.GetCommitment("person"), Is.SameAs(commitmentBefore));
        Assert.That(store.PendingWork.Count, Is.EqualTo(authoritativeBefore.Length));
        for (int i = 0; i < authoritativeBefore.Length; i++)
            Assert.That(store.PendingWork[i], Is.SameAs(authoritativeBefore[i]));

        Assert.That(ownerTimeline.TrySealInputsThrough(new LogicalTick(10), out _), Is.True);
        Assert.That(ownerTimeline.TryAdvanceTo(new LogicalTick(10), out TimelineFailure ownerFailure), Is.True, ownerFailure.ToString());
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot active), Is.True);
        Assert.That(active.State, Is.EqualTo(ActivityLifecycleState.Active));
    }

    [Test]
    public void InstanceScopedDueIdsAllowEqualRevisionsAtSameAndDifferentInstantsAndAtomicRebuildRetry()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        SimulationTimeline sourceTimeline = Compose(store).Timeline;
        store.TryPropose(Definition(), "same-a", out ActivityInstanceSnapshot a, out _);
        store.TryPropose(Definition(), "same-b", out ActivityInstanceSnapshot b, out _);
        store.TryPropose(Definition(), "later", out ActivityInstanceSnapshot later, out _);
        Assert.That(store.TrySchedule(sourceTimeline, a.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(1), new[] { "p-a" }, out _), Is.True);
        Assert.That(store.TrySchedule(sourceTimeline, b.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(1), new[] { "p-b" }, out _), Is.True);
        Assert.That(store.TrySchedule(sourceTimeline, later.Id, new LogicalTick(0), new LogicalTick(12), new LogicalTick(1), new[] { "p-c" }, out _), Is.True);
        Assert.That(store.PendingWork[0].DueWorkId, Is.Not.EqualTo(store.PendingWork[2].DueWorkId));

        ActivityLifecycleStore fork = store.Clone();
        DueWorkReference collisionSource = fork.PendingWork[2];
        SimulationTimeline failedRebuild = Compose(fork).Timeline;
        DueWorkReference collision = new DueWorkReference(collisionSource.OwnerId, collisionSource.DueWorkId,
            "unrelated-instance", 99, collisionSource.OccurrenceSequence, collisionSource.DueAt);
        Assert.That(failedRebuild.TryIndexOwnerFact(collision, out _), Is.True);
        long before = failedRebuild.CausalSequence;
        Assert.That(fork.TryRebuildTimelineIndex(failedRebuild, out TimelineFailure rejected), Is.False);
        Assert.That(rejected, Is.EqualTo(TimelineFailure.DuplicateWorkSequence));
        Assert.That(failedRebuild.CausalSequence, Is.EqualTo(before));
        Assert.That(failedRebuild.IsDue(new LogicalTick(10), fork.PendingWork[0].DueWorkId), Is.False);

        ActivityLifecycleStore retryFork = fork.Clone();
        SimulationTimeline retry = Compose(retryFork).Timeline;
        Assert.That(retryFork.TryRebuildTimelineIndex(retry, out TimelineFailure retryFailure), Is.True, retryFailure.ToString());
        Assert.That(retry.PreviewDueWork(new LogicalTick(10)).Count, Is.EqualTo(2));
        Assert.That(retry.TrySealInputsThrough(new LogicalTick(13), out _), Is.True);
        Assert.That(retry.TryAdvanceTo(new LogicalTick(13), out TimelineFailure advanceFailure), Is.True, advanceFailure.ToString());
        Assert.That(retryFork.TryGet(a.Id, out ActivityInstanceSnapshot doneA), Is.True);
        Assert.That(retryFork.TryGet(b.Id, out ActivityInstanceSnapshot doneB), Is.True);
        Assert.That(retryFork.TryGet(later.Id, out ActivityInstanceSnapshot doneLater), Is.True);
        Assert.That(doneA.State, Is.EqualTo(ActivityLifecycleState.Completed));
        Assert.That(doneB.State, Is.EqualTo(ActivityLifecycleState.Completed));
        Assert.That(doneLater.State, Is.EqualTo(ActivityLifecycleState.Completed));
    }

    [Test]
    public void MultipleParticipantsZeroDurationAndNoEndAvailabilityHaveExplicitBounds()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        SimulationTimeline timeline = Compose(store).Timeline;
        store.TryPropose(Definition(), "shared", out ActivityInstanceSnapshot shared, out _);
        Assert.That(store.TrySchedule(timeline, shared.Id, new LogicalTick(0), new LogicalTick(5), new LogicalTick(0), new[] { "p1", "p2" }, out _), Is.True);
        store.TryPropose(Definition(), "open-ended", out ActivityInstanceSnapshot openEnded, out _);
        Assert.That(store.TrySchedule(timeline, openEnded.Id, new LogicalTick(0), new LogicalTick(8), null, new[] { "p3" }, out _), Is.True);
        Assert.That(store.TryGet(shared.Id, out ActivityInstanceSnapshot multi), Is.True);
        Assert.That(multi.Participants, Is.EqualTo(new[] { "p1", "p2" }));
        Assert.That(store.GetCommitment("p3").End, Is.Null);
        Assert.That(store.IsAvailable("p3", new LogicalTick(long.MaxValue)), Is.False);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(8), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(8), out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(store.TryGet(shared.Id, out ActivityInstanceSnapshot zeroDone), Is.True);
        Assert.That(zeroDone.State, Is.EqualTo(ActivityLifecycleState.Completed));
        Assert.That(zeroDone.ActualStart.Value, Is.EqualTo(new LogicalTick(5)));
        Assert.That(zeroDone.TerminalInstant.Value, Is.EqualTo(new LogicalTick(5)));
        Assert.That(store.TryGet(openEnded.Id, out ActivityInstanceSnapshot openActive), Is.True);
        Assert.That(openActive.State, Is.EqualTo(ActivityLifecycleState.Active));
        Assert.That(store.IsAvailable("p3", new LogicalTick(long.MaxValue)), Is.False);
    }

    [Test]
    public void OwnerCommitWindowRejectsReentrantTimelineMutationAndAdvance()
    {
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0));
        DueWorkReference fact = new DueWorkReference("owner", "fact", "instance", 1, 1, new LogicalTick(10));
        bool advanceRejected = false, mutationRejected = false;
        Assert.That(timeline.TryCommitOwnerFacts(new[] { fact }, () =>
        {
            advanceRejected = !timeline.TryAdvanceTo(new LogicalTick(10), out TimelineFailure advanceFailure)
                && advanceFailure == TimelineFailure.ReentrantAdvance;
            mutationRejected = !timeline.TryIndexOwnerFact(new DueWorkReference("other", "nested", "i", 1, 2, new LogicalTick(11)), out _);
            return TimelineFailure.None;
        }, out _), Is.True);
        Assert.That(advanceRejected, Is.True);
        Assert.That(mutationRejected, Is.True);
        Assert.That(timeline.CausalSequence, Is.EqualTo(1));
        Assert.That(timeline.PreviewDueWork(new LogicalTick(11)), Is.Empty);
    }
}
