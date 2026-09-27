using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class ActivityLifecycleTests
{
    private static ActivityDefinition Definition() => new ActivityDefinition("work", "v1");
    private static SimulationCalendar Calendar() => new SimulationCalendar(new CalendarDefinition(2, 2, 3));

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
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), store);
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
        Assert.That(store.PendingWork[0].DueWorkId, Is.EqualTo("start"));
        Assert.That(store.PendingWork[1].DueWorkId, Is.EqualTo("complete"));
    }

    [Test]
    public void SchedulingRejectsConflictingParticipantWithoutPartialState()
    {
        ActivityLifecycleStore store = new ActivityLifecycleStore("world");
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), store);
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
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), store);
        store.TryPropose(Definition(), "rejected", out ActivityInstanceSnapshot instance, out _);
        DueWorkReference collision = new DueWorkReference(ActivityLifecycleStore.DueOwnerId, "start", "other-instance", 9, 1, new LogicalTick(10));
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
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), store);
        store.TryPropose(Definition(), "first", out ActivityInstanceSnapshot instance, out _);
        store.TrySchedule(timeline, instance.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(10), new[] { "person" }, out _);
        DueWorkReference oldWork = store.PendingWork[0];
        Assert.That(store.TryCancel(instance.Id, new LogicalTick(5), "consumer-cancelled", out _), Is.True);
        Assert.That(store.IsCurrent(oldWork), Is.False);
        Assert.That(store.GetCommitment("person"), Is.Null);
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot cancelled), Is.True);
        Assert.That(cancelled.State, Is.EqualTo(ActivityLifecycleState.Cancelled));
        Assert.That(cancelled.Disposition, Is.EqualTo("consumer-cancelled"));
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
        SimulationTimeline timeline = new SimulationTimeline(Calendar(), new LogicalTick(0), store);
        store.TryPropose(Definition(), "start-end", out ActivityInstanceSnapshot instance, out _);
        Assert.That(store.TrySchedule(timeline, instance.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(5), new[] { "person" }, out _), Is.True);
        Assert.That(timeline.TrySealInputsThrough(new LogicalTick(15), out _), Is.True);
        Assert.That(timeline.TryAdvanceTo(new LogicalTick(15), out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(store.TryGet(instance.Id, out ActivityInstanceSnapshot completed), Is.True);
        Assert.That(completed.State, Is.EqualTo(ActivityLifecycleState.Completed));
        Assert.That(completed.ActualStart.Value.Value, Is.EqualTo(10));
        Assert.That(completed.TerminalInstant.Value.Value, Is.EqualTo(15));
        Assert.That(store.GetCommitment("person"), Is.Null);
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
        SimulationTimeline originalTimeline = new SimulationTimeline(Calendar(), new LogicalTick(0), original);
        original.TryPropose(Definition(), "fork-source", out ActivityInstanceSnapshot instance, out _);
        Assert.That(original.TrySchedule(originalTimeline, instance.Id, new LogicalTick(0), new LogicalTick(10), new LogicalTick(5), new[] { "person" }, out _), Is.True);
        ActivityLifecycleStore fork = original.Clone();
        SimulationTimeline forkTimeline = new SimulationTimeline(Calendar(), new LogicalTick(0), fork);
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
}
