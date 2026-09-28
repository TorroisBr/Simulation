using System.Collections.Generic;
using NUnit.Framework;

public sealed class SimulationLoggerBoundaryOwnerTests
{
    [Test]
    public void EnabledBeginDayStepCommitsLegacyOutputAndReceiptWithExactReplay()
    {
        SimulationLogger logger = new SimulationLogger(new SimulationLogSettings { showDay = true });
        DailyBoundaryOperation operation = new DailyBoundaryOperation("logger-world", "intraday", 12L);
        BoundaryContinuationManifest manifest = CreateManifest(logger, operation, 0, out BoundaryContinuationStep step);

        Assert.That(logger.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure), Is.True);
        Assert.That(prepared.TryCommit(out failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(logger.FullLog, Is.EqualTo(string.Join(System.Environment.NewLine,
            "====================", "DIA 12", "====================", string.Empty)));
        Assert.That(logger.TryResolveBeginDayReceipt(manifest, step, out LoggerBeginDayReceipt receipt, out failure), Is.True);
        Assert.That(receipt.OwnerRevisionBefore, Is.Zero);
        Assert.That(receipt.OwnerRevisionAfter, Is.EqualTo(1L));
        Assert.That(receipt.Disposition, Is.EqualTo("included"));

        Assert.That(logger.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit replay, out failure), Is.True);
        Assert.That(replay.TryCommit(out failure), Is.True);
        Assert.That(prepared.TryCommit(out failure), Is.True);
        Assert.That(logger.FullLog, Is.EqualTo(string.Join(System.Environment.NewLine,
            "====================", "DIA 12", "====================", string.Empty)));
    }

    [Test]
    public void DisabledBeginDayStepCommitsFrozenNoOutputDisposition()
    {
        SimulationLogger logger = new SimulationLogger(new SimulationLogSettings { showDay = false });
        DailyBoundaryOperation operation = new DailyBoundaryOperation("logger-world", "intraday", 13L);
        BoundaryContinuationManifest manifest = CreateManifest(logger, operation, 0, out BoundaryContinuationStep step);

        Assert.That(step.Disposition, Is.EqualTo("no-output"));
        Assert.That(logger.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure), Is.True);
        Assert.That(prepared.TryCommit(out failure), Is.True);
        Assert.That(logger.FullLog, Is.Empty);
        Assert.That(logger.TryResolveBeginDayReceipt(manifest, step, out LoggerBeginDayReceipt receipt, out failure), Is.True);
        Assert.That(receipt.Disposition, Is.EqualTo("no-output"));
        Assert.That(receipt.OwnerRevisionAfter, Is.EqualTo(1L));
    }

    [Test]
    public void BeginDayStepRejectsDescriptorConflictForSameOccurrence()
    {
        SimulationLogger logger = new SimulationLogger(new SimulationLogSettings { showDay = true });
        DailyBoundaryOperation operation = new DailyBoundaryOperation("logger-world", "intraday", 14L);
        BoundaryContinuationManifest manifest = CreateManifest(logger, operation, 0, out BoundaryContinuationStep step);
        Assert.That(logger.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit prepared, out _), Is.True);
        Assert.That(prepared.TryCommit(out _), Is.True);

        BoundaryContinuationStep conflictingStep = new BoundaryContinuationStep(
            step.Ordinal, step.StepId, step.OwnerId, step.OperationKind, step.OperationVersion,
            step.OwnerRevision, "changed-payload", disposition: step.Disposition);
        BoundaryContinuationManifest conflictingManifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "v1", "configuration", new List<BoundaryContinuationStep> { conflictingStep });
        Assert.That(logger.TryPrepareBeginDayStep(conflictingManifest, conflictingStep, out _, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    [Test]
    public void UncommittedPreparationCanRetryAndCommittedUncertainResponseReplaysOnce()
    {
        SimulationLogger logger = new SimulationLogger(new SimulationLogSettings { showDay = true });
        DailyBoundaryOperation operation = new DailyBoundaryOperation("logger-world", "intraday", 15L);
        BoundaryContinuationManifest manifest = CreateManifest(logger, operation, 0, out BoundaryContinuationStep step);
        Assert.That(logger.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure), Is.True);

        // A retry plan may be prepared while the owner is still unchanged, but only one
        // sibling can commit against that snapshot; the other must observe its stale revision.
        Assert.That(logger.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit retry, out failure), Is.True);
        Assert.That(retry.TryCommit(out failure), Is.True);
        Assert.That(prepared.TryCommit(out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));

        // A lost response after the successful commit is resolved from the owner receipt,
        // then a fresh exact prepare yields replay without a second output.
        Assert.That(logger.TryResolveBeginDayReceipt(manifest, step, out _, out failure), Is.True);
        Assert.That(logger.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit replay, out failure), Is.True);
        Assert.That(replay.TryCommit(out failure), Is.True);
        Assert.That(logger.FullLog.Split(new[] { "DIA 15" }, System.StringSplitOptions.None).Length, Is.EqualTo(2));
    }

    private static BoundaryContinuationManifest CreateManifest(
        SimulationLogger logger,
        DailyBoundaryOperation operation,
        int ordinal,
        out BoundaryContinuationStep step)
    {
        Assert.That(logger.TryCreateBeginDayStep(operation, ordinal, out step, out TimelineFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        List<BoundaryContinuationStep> steps = new List<BoundaryContinuationStep>();
        if (ordinal > 0)
        {
            steps.Add(new BoundaryContinuationStep(0, "prior-step", "test", "test.operation", "1", "", ""));
        }

        steps.Add(step);
        return new BoundaryContinuationManifest(operation, "daily-boundary", "v1", "configuration", steps);
    }
}
