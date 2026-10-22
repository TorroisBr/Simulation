using System;
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1RuntimeAdvanceStageTests
{
    [Test]
    public void SingleAdvanceOwnsLeaseOperationCompletionAndCapturePublicationOrder()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary
        {
            HasRuntimeAdmission = true,
            IsDailyCaptureProfile = true
        };

        Assert.That(DailyV1RuntimeAdvanceStage.TryAdvanceDay(state, boundary, out var failure), Is.True);
        Assert.That(failure, Is.EqualTo(DailyV1RuntimeAdvanceFailureCode.None));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "owner", "healthy", "capacity:1", "clock", "healthy", "operation",
            "advance", "completed", "operation-end", "publish"
        }));
    }

    [Test]
    public void SingleAdvanceExceptionInvalidatesCaptureAndFaultsAdmission()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary
        {
            HasRuntimeAdmission = true,
            IsDailyCaptureProfile = true,
            ThrowDuringAdvance = true
        };

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1RuntimeAdvanceStage.TryAdvanceDay(state, boundary, out _));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
        Assert.That(state.CurrentCaptureToken, Is.Null);
        Assert.That(boundary.Calls, Does.Contain("fault"));
        Assert.That(boundary.Calls, Does.Not.Contain("publish"));
    }

    [Test]
    public void SingleLegacyAdvanceDoesNotEnterDailyAdmissionOrPublishCaptureToken()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary();

        Assert.That(DailyV1RuntimeAdvanceStage.TryAdvanceDay(state, boundary, out _), Is.True);
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "advance" }));
    }

    [Test]
    public void SingleCapturePreflightFailureDoesNotAcquireLeaseOrAdvance()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary
        {
            HasRuntimeAdmission = true,
            IsDailyCaptureProfile = true,
            ClockValidationSucceeds = false,
            ClockFailure = DailyV1RuntimeAdvanceFailureCode.AbsoluteDayOverflow
        };

        Assert.That(DailyV1RuntimeAdvanceStage.TryAdvanceDay(state, boundary, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(DailyV1RuntimeAdvanceFailureCode.AbsoluteDayOverflow));
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "owner", "healthy", "capacity:1", "clock" }));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void SingleAdvanceRejectsReentryBeforeAnyHostOperation()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease outerLease), Is.True);
        FakeBoundary boundary = new FakeBoundary();

        Assert.That(DailyV1RuntimeAdvanceStage.TryAdvanceDay(state, boundary, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(DailyV1RuntimeAdvanceFailureCode.AdvanceAlreadyInProgress));
        Assert.That(boundary.Calls, Is.Empty);
        outerLease.Dispose();
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void LegacyIntradayAdvanceUsesTheTimelineBoundary()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary { HasIntradayTimeline = true };

        Assert.That(DailyV1RuntimeAdvanceStage.TryAdvanceDay(state, boundary, out _), Is.True);
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "intraday" }));
    }

    [Test]
    public void NonAdmittedSingleAdvanceKeepsIntradayTimelinePrecedence()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary
        {
            HasIntradayTimeline = true,
            HasCoreSelectedDailyExecution = true
        };

        Assert.That(DailyV1RuntimeAdvanceStage.TryAdvanceDay(state, boundary, out _), Is.True);
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "intraday" }));
        Assert.That(boundary.Calls, Does.Not.Contain("selected-context"));
        Assert.That(boundary.Calls, Does.Not.Contain("advance"));
    }

    [Test]
    public void BatchAdvanceKeepsIntradayTimelinePrecedenceAndHostPreflight()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary
        {
            HasRuntimeAdmission = true,
            HasIntradayTimeline = true,
            HasCoreSelectedDailyExecution = true
        };

        Assert.That(DailyV1RuntimeAdvanceStage.TryAdvanceDays(
            state, boundary, 1, out int advanced, out DailyV1RuntimeAdvanceFailureCode failure), Is.True);
        Assert.That(advanced, Is.EqualTo(1));
        Assert.That(failure, Is.EqualTo(DailyV1RuntimeAdvanceFailureCode.None));
        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "owner", "healthy", "clock", "operation", "intraday", "completed", "operation-end"
        }));
        Assert.That(boundary.Calls, Does.Not.Contain("selected-context"));
        Assert.That(boundary.Calls, Does.Not.Contain("selected-clock"));
        Assert.That(boundary.Calls, Does.Not.Contain("advance"));
    }

    [Test]
    public void BatchRejectsNegativeCountAndLeavesZeroCountWithoutLeaseWork()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary();

        Assert.That(DailyV1RuntimeAdvanceStage.TryAdvanceDays(
            state, boundary, -1, out int negativeAdvanced, out var negativeFailure), Is.False);
        Assert.That(negativeAdvanced, Is.Zero);
        Assert.That(negativeFailure, Is.EqualTo(DailyV1RuntimeAdvanceFailureCode.InvalidDayCount));
        Assert.That(DailyV1RuntimeAdvanceStage.TryAdvanceDays(
            state, boundary, 0, out int zeroAdvanced, out var zeroFailure), Is.True);
        Assert.That(zeroAdvanced, Is.Zero);
        Assert.That(zeroFailure, Is.EqualTo(DailyV1RuntimeAdvanceFailureCode.None));
        Assert.That(boundary.Calls, Is.Empty);
    }

    [Test]
    public void BatchRecordsEachCompletedBoundaryAndPublishesOnlyAfterAllDays()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary
        {
            HasRuntimeAdmission = true,
            IsDailyCaptureProfile = true
        };

        Assert.That(DailyV1RuntimeAdvanceStage.TryAdvanceDays(
            state, boundary, 3, out int advanced, out var failure), Is.True);
        Assert.That(advanced, Is.EqualTo(3));
        Assert.That(failure, Is.EqualTo(DailyV1RuntimeAdvanceFailureCode.None));
        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "owner", "healthy", "capacity:3", "clock", "healthy", "operation",
            "advance", "completed", "advance", "completed", "advance", "completed", "operation-end", "publish"
        }));
        Assert.That(boundary.Calls.FindIndex(value => value == "publish"),
            Is.GreaterThan(boundary.Calls.FindLastIndex(value => value == "completed")));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void BatchReportsPartialProgressWithoutPublishingAfterLaterAdvanceFailure()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary
        {
            HasRuntimeAdmission = true,
            IsDailyCaptureProfile = true,
            FailAdvanceNumber = 2
        };

        Assert.That(DailyV1RuntimeAdvanceStage.TryAdvanceDays(
            state, boundary, 3, out int advanced, out var failure), Is.False);
        Assert.That(advanced, Is.EqualTo(1));
        Assert.That(failure, Is.EqualTo(DailyV1RuntimeAdvanceFailureCode.TemporalAdvanceFailed));
        Assert.That(boundary.Calls, Does.Not.Contain("publish"));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void BatchExceptionFaultsOnlyAnAdmittedRuntime()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary
        {
            HasRuntimeAdmission = false,
            ThrowDuringAdvance = true
        };

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1RuntimeAdvanceStage.TryAdvanceDays(state, boundary, 1, out _, out _));
        Assert.That(boundary.Calls, Does.Not.Contain("fault"));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    private sealed class FakeBoundary : IDailyV1RuntimeAdvanceBoundary
    {
        internal List<string> Calls { get; } = new List<string>();
        internal int FailAdvanceNumber { get; set; }
        internal bool ThrowDuringAdvance { get; set; }
        internal bool HasRuntimeAdmission { get; set; }
        internal bool IsDailyCaptureProfile { get; set; }
        internal bool HasCoreSelectedDailyExecution { get; set; }
        internal bool IsAdmissionOwnerThreadCurrent { get; set; } = true;
        internal bool CanMutate { get; set; } = true;
        internal bool HasIntradayTimeline { get; set; }
        internal bool ClockValidationSucceeds { get; set; } = true;
        internal DailyV1RuntimeAdvanceFailureCode ClockFailure { get; set; }
        bool IDailyV1RuntimeAdvanceBoundary.HasRuntimeAdmission => HasRuntimeAdmission;
        bool IDailyV1RuntimeAdvanceBoundary.IsDailyCaptureProfile => IsDailyCaptureProfile;
        bool IDailyV1RuntimeAdvanceBoundary.HasCoreSelectedDailyExecution => HasCoreSelectedDailyExecution;
        SimulationTime IDailyV1RuntimeAdvanceBoundary.SelectedDailyClock => null;
        bool IDailyV1RuntimeAdvanceBoundary.IsAdmissionOwnerThreadCurrent
        {
            get { Calls.Add("owner"); return IsAdmissionOwnerThreadCurrent; }
        }
        bool IDailyV1RuntimeAdvanceBoundary.CanMutate
        {
            get { Calls.Add("healthy"); return CanMutate; }
        }
        bool IDailyV1RuntimeAdvanceBoundary.HasIntradayTimeline => HasIntradayTimeline;

        public bool TryPreflightDailyCoreSequenceCapacity(long requestedBoundaries,
            out DailyV1RuntimeAdvanceFailureCode failure)
        {
            Calls.Add("capacity:" + requestedBoundaries);
            failure = DailyV1RuntimeAdvanceFailureCode.None;
            return true;
        }

        public bool TryValidateClockAdvance(out DailyV1RuntimeAdvanceFailureCode failure)
        {
            Calls.Add("clock");
            failure = ClockValidationSucceeds
                ? DailyV1RuntimeAdvanceFailureCode.None : ClockFailure;
            return ClockValidationSucceeds;
        }

        public bool TryEnterDailyAdvanceOperation(out IDisposable operationScope)
        {
            Calls.Add("operation");
            operationScope = new FakeOperationScope(() => Calls.Add("operation-end"));
            return true;
        }

        public bool TryCreateSelectedDailyExecutionContext(
            out long absoluteDay,
            out DailyV1SelectedRuntimeOwnerGraph graph,
            out ScheduledDirectiveStore scheduledDirectives,
            out IDailyV1SelectedDailyExecutionBoundary executionBoundary)
        {
            Calls.Add("selected-context");
            absoluteDay = 0L;
            graph = null;
            scheduledDirectives = null;
            executionBoundary = null;
            return false;
        }

        public bool TryAdvanceDayCore(out DailyV1RuntimeAdvanceFailureCode failure) => Advance("advance", out failure);

        public bool TryAdvanceIntradayToNextDayBoundary(out DailyV1RuntimeAdvanceFailureCode failure) =>
            Advance("intraday", out failure);

        public bool TryRecordCompletedDailyCore(out DailyV1RuntimeAdvanceFailureCode failure)
        {
            Calls.Add("completed");
            failure = DailyV1RuntimeAdvanceFailureCode.None;
            return true;
        }

        public bool TryPublishCompletedDailyCaptureToken()
        {
            Calls.Add("publish");
            return true;
        }

        public void FaultRuntimeAdmission() => Calls.Add("fault");

        private bool Advance(string call, out DailyV1RuntimeAdvanceFailureCode failure)
        {
            Calls.Add(call);
            if (ThrowDuringAdvance) throw new InvalidOperationException("fixture advance failure");
            int advances = Calls.FindAll(value => value == call).Count;
            failure = FailAdvanceNumber > 0 && advances == FailAdvanceNumber
                ? DailyV1RuntimeAdvanceFailureCode.TemporalAdvanceFailed
                : DailyV1RuntimeAdvanceFailureCode.None;
            return failure == DailyV1RuntimeAdvanceFailureCode.None;
        }
    }

    private sealed class FakeOperationScope : IDisposable
    {
        private readonly Action onDispose;

        internal FakeOperationScope(Action onDispose) => this.onDispose = onDispose;

        public void Dispose() => onDispose?.Invoke();
    }
}
