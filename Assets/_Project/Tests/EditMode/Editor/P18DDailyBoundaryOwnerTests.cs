using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class P18DDailyBoundaryOwnerTests
{
    private const string WorldId = "p18d-test-world";
    private const string ProfileId = "p18d-test-profile";

    [Test]
    public void TimelineRetriesCommittedOwnerReceiptAndDefersSignalHandoffUntilSuccess()
    {
        StepProvider provider = new StepProvider();
        SignalSink sink = new SignalSink();
        P18DDailyBoundaryOwner boundaryOwner = new P18DDailyBoundaryOwner(
            WorldId, ProfileId, "config/v1", "content/v1", new[] { provider }, sink);
        SimulationTimeline timeline = new SimulationTimeline(
            new SimulationCalendar(new CalendarDefinition(2, 2, 3)),
            new LogicalTick(0), boundaryOwner: boundaryOwner, worldId: WorldId, profileId: ProfileId);
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(timeline.TrySealInputsThrough(boundary, out TimelineFailure sealFailure), Is.True);
        Assert.That(sealFailure, Is.EqualTo(TimelineFailure.None));

        Assert.That(timeline.TryAdvanceTo(boundary, out TimelineFailure firstFailure), Is.False);
        Assert.That(firstFailure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(provider.EffectCount, Is.EqualTo(1));
        Assert.That(provider.PrepareCount, Is.EqualTo(1));
        Assert.That(timeline.CurrentInstant, Is.EqualTo(boundary));

        Assert.That(timeline.TryAdvanceTo(boundary, out TimelineFailure retryFailure), Is.True);
        Assert.That(retryFailure, Is.EqualTo(TimelineFailure.None));
        Assert.That(provider.EffectCount, Is.EqualTo(1), "the owner receipt must prevent reapplying the committed effect");
        Assert.That(provider.PrepareCount, Is.EqualTo(2));
        Assert.That(sink.AttemptCount, Is.Zero, "signals are not handed off from inside timeline advancement");

        Assert.That(timeline.TryCompleteSuccessfulAdvanceHandoffs(out TimelineFailure firstHandoffFailure), Is.False);
        Assert.That(firstHandoffFailure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(sink.AttemptCount, Is.EqualTo(1));
        Assert.That(timeline.TryCompleteSuccessfulAdvanceHandoffs(out TimelineFailure handoffFailure), Is.True);
        Assert.That(handoffFailure, Is.EqualTo(TimelineFailure.None));
        Assert.That(sink.AttemptCount, Is.EqualTo(2));
        Assert.That(sink.LastSignals, Is.EqualTo(new[] { "signal/day-1" }));

        DailyBoundaryOperation operation = new DailyBoundaryOperation(WorldId, ProfileId, 1L);
        Assert.That(boundaryOwner.TryPrepareActivation(operation,
            out IBoundaryActivationCommit repeatedActivation, out TimelineFailure activationFailure), Is.True);
        Assert.That(activationFailure, Is.EqualTo(TimelineFailure.None));
        Assert.That(provider.BuildCount, Is.EqualTo(1), "retry must reuse the frozen activation manifest");
        Assert.That(repeatedActivation.TryCommit(out TimelineFailure repeatedCommitFailure), Is.True);
        Assert.That(repeatedCommitFailure, Is.EqualTo(TimelineFailure.None));

        Assert.That(boundaryOwner.TryResolveContinuation(repeatedActivation.Manifest.ContinuationId,
            out BoundaryContinuationState state, out TimelineFailure stateFailure), Is.True);
        Assert.That(stateFailure, Is.EqualTo(TimelineFailure.None));
        Assert.That(state.IsComplete, Is.True);
        Assert.That(state.TimelineFactsPublished, Is.True);
        Assert.That(state.SignalsHandedOff, Is.True);
        Assert.That(repeatedActivation.Manifest.HasSameFrozenContent(state.Manifest), Is.True);
    }

    [Test]
    public void ActivationRejectsAnEmptyDailyManifest()
    {
        P18DDailyBoundaryOwner owner = new P18DDailyBoundaryOwner(
            WorldId, ProfileId, "config/v1", "content/v1",
            Array.Empty<IP18DDailyBoundaryStepProvider>());

        Assert.That(owner.TryPrepareActivation(new DailyBoundaryOperation(WorldId, ProfileId, 1L),
            out IBoundaryActivationCommit activation, out TimelineFailure failure), Is.False);
        Assert.That(activation, Is.Null);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    private sealed class StepProvider : IP18DDailyBoundaryStepProvider
    {
        private bool hasReceipt;
        public int BuildCount { get; private set; }
        public int PrepareCount { get; private set; }
        public int EffectCount { get; private set; }

        public bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
            out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure)
        {
            BuildCount++;
            steps = new[]
            {
                new BoundaryContinuationStep(firstOrdinal, "test.step", "test.owner",
                    "test.operation", "v1", "owner-revision/v1", string.Empty)
            };
            failure = TimelineFailure.None;
            return true;
        }

        public bool OwnsStep(BoundaryContinuationStep step) =>
            step != null && step.OwnerId == "test.owner" && step.StepId == "test.step";

        public bool TryPrepareStep(BoundaryContinuationManifest manifest,
            BoundaryContinuationStep step, out IBoundaryContinuationStepCommit prepared,
            out TimelineFailure failure)
        {
            PrepareCount++;
            prepared = new StepCommit(this, hasReceipt);
            failure = TimelineFailure.None;
            return true;
        }

        private sealed class StepCommit : IBoundaryContinuationStepCommit
        {
            private readonly StepProvider owner;
            private readonly bool isReplay;
            public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
            public IReadOnlyList<string> RetainedSourceSignals => new[] { "signal/day-1" };

            public StepCommit(StepProvider owner, bool isReplay)
            {
                this.owner = owner;
                this.isReplay = isReplay;
            }

            public bool TryCommit(out TimelineFailure failure)
            {
                if (isReplay)
                {
                    failure = TimelineFailure.None;
                    return true;
                }
                owner.hasReceipt = true;
                owner.EffectCount++;
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }
        }
    }

    private sealed class SignalSink : IP18DDailyBoundarySignalSink
    {
        public int AttemptCount { get; private set; }
        public IReadOnlyList<string> LastSignals { get; private set; } = Array.Empty<string>();

        public bool TryHandoff(BoundaryContinuationManifest manifest,
            IReadOnlyList<string> signals, out TimelineFailure failure)
        {
            AttemptCount++;
            LastSignals = new List<string>(signals).AsReadOnly();
            if (AttemptCount == 1)
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }
            failure = TimelineFailure.None;
            return true;
        }
    }
}
