using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1RuntimeAdvanceStageTests
{
    [Test]
    public void SharedAdvanceFailureContractPreservesUnityPublicValues()
    {
        Assert.That(Enum.GetUnderlyingType(typeof(SimulationRuntimeAdvanceFailure)), Is.EqualTo(typeof(int)));
        Assert.That((int)SimulationRuntimeAdvanceFailure.None, Is.EqualTo(0));
        Assert.That((int)SimulationRuntimeAdvanceFailure.RuntimeFaulted, Is.EqualTo(1));
        Assert.That((int)SimulationRuntimeAdvanceFailure.InvalidDayCount, Is.EqualTo(2));
        Assert.That((int)SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow, Is.EqualTo(3));
        Assert.That((int)SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress, Is.EqualTo(4));
        Assert.That((int)SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed, Is.EqualTo(5));
        Assert.That((int)SimulationRuntimeAdvanceFailure.CompletedBoundarySequenceOverflow, Is.EqualTo(6));
    }

    [Test]
    public void SingleAdvanceOwnsLeaseOperationCompletionAndCapturePublicationOrder()
    {
        DailyV1ExecutionBoundaryState state = CreateCaptureState();
        FakeBoundary boundary = new FakeBoundary(state);

        Assert.That(state.TryAdvanceDay(boundary, out var failure), Is.True);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.None));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "operation",
            "advance", "operation-end", "capture-evidence"
        }));
        Assert.That(boundary.OperationCountAtP12ScopeBegin, Is.EqualTo(1));
        Assert.That(boundary.OperationCountAtCapture, Is.EqualTo(1),
            "The outer DailyAdvance scope remains registered until capture-token publication has consumed the completed-boundary evidence.");
        Assert.That(state.ActiveOperationCount, Is.Zero,
            "Core closes the outer DailyAdvance scope only after token publication.");
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(1L));
        Assert.That(boundary.SequenceObservedAtEvidenceCapture, Is.EqualTo(1L));
        Assert.That(state.CurrentCaptureToken, Is.Not.Null);
        Assert.That(state.CurrentCaptureToken.OwnerSections, Is.SameAs(boundary.CapturedOwnerSections));
    }

    [Test]
    public void SingleAdvanceExceptionInvalidatesCaptureAndFaultsAdmission()
    {
        DailyV1ExecutionBoundaryState state = CreateCaptureState();
        FakeBoundary boundary = new FakeBoundary(state)
        {
            ThrowDuringAdvance = true
        };

        Assert.Throws<InvalidOperationException>(() =>
            state.TryAdvanceDay(boundary, out _));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
        Assert.That(state.CurrentCaptureToken, Is.Null);
        Assert.That(boundary.Calls, Does.Contain("fault"));
        Assert.That(boundary.Calls, Does.Not.Contain("capture-evidence"));
        Assert.That(state.ActiveOperationCount, Is.Zero,
            "The Core-owned outer DailyAdvance scope closes when the host stage throws.");
    }

    [Test]
    public void SingleLegacyAdvanceDoesNotEnterDailyAdmissionOrPublishCaptureToken()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary(state);

        Assert.That(state.TryAdvanceDay(boundary, out _), Is.True);
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "advance" }));
    }

    [Test]
    public void SingleCapturePreflightFailureDoesNotAcquireLeaseOrAdvance()
    {
        DailyV1ExecutionBoundaryState state = CreateCaptureState(long.MaxValue);
        FakeBoundary boundary = new FakeBoundary(state);

        Assert.That(state.TryAdvanceDay(boundary, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow));
        Assert.That(boundary.Calls, Is.Empty);
        Assert.That(state.AdvanceLeaseHeld, Is.False);
        Assert.That(state.ActiveOperationCount, Is.Zero,
            "A failed Core preflight must close the outer DailyAdvance scope.");
    }

    [Test]
    public void SelectedBoundaryConstructionFailureDoesNotAdvanceAdmittedClock()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        FakeBoundary boundary = new FakeBoundary(state);

        Assert.That(state.TryAdvanceDay(boundary, out SimulationRuntimeAdvanceFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(composition.SimulationTime.AbsoluteDay, Is.Zero,
            "A rejected selected-stage adapter must be discovered before the Core clock mutates.");
        Assert.That(state.CompletedCoreSequence, Is.Zero);
        Assert.That(state.CurrentCaptureToken, Is.Null);
        Assert.That(state.AdvanceLeaseHeld, Is.False);
        Assert.That(state.ActiveOperationCount, Is.Zero);
        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "operation", "execution-boundary", "fault", "operation-end"
        }));
        Assert.That(boundary.Calls, Does.Not.Contain("advance"));
        Assert.That(boundary.Calls, Does.Not.Contain("capture-evidence"));
    }

    [Test]
    public void DailyCaptureAdvanceReadsCoreOwnedMutationGuardBeforeHostWork()
    {
        DailyV1ExecutionBoundaryState state = CreateCaptureState();
        FakeBoundary boundary = new FakeBoundary(state)
        {
            CanMutate = false
        };

        Assert.That(state.TryAdvanceDay(boundary, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(state.MutationGuard.CanMutate, Is.False);
        Assert.That(boundary.Calls, Is.Empty);
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void SingleAdvanceRejectsReentryBeforeAnyHostOperation()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease outerLease), Is.True);
        FakeBoundary boundary = new FakeBoundary(state);

        Assert.That(state.TryAdvanceDay(boundary, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress));
        Assert.That(boundary.Calls, Is.Empty);
        outerLease.Dispose();
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void RuntimeBoundaryMustBelongToTheCoreExecutionStateThatAdvancesIt()
    {
        DailyV1ExecutionBoundaryState requestedState = new DailyV1ExecutionBoundaryState();
        DailyV1ExecutionBoundaryState boundaryState = CreateCaptureState();
        FakeBoundary boundary = new FakeBoundary(boundaryState);

        Assert.That(requestedState.TryAdvanceDay(boundary, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(boundary.Calls, Is.Empty);
        Assert.That(requestedState.AdvanceLeaseHeld, Is.False);
        Assert.That(boundaryState.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void BatchRuntimeBoundaryMustBelongToTheCoreExecutionStateThatAdvancesIt()
    {
        DailyV1ExecutionBoundaryState requestedState = new DailyV1ExecutionBoundaryState();
        DailyV1ExecutionBoundaryState boundaryState = CreateCaptureState();
        FakeBoundary boundary = new FakeBoundary(boundaryState);

        Assert.That(requestedState.TryAdvanceDays(boundary, 2, out int daysAdvanced, out var failure), Is.False);
        Assert.That(daysAdvanced, Is.Zero);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(boundary.Calls, Is.Empty);
        Assert.That(requestedState.AdvanceLeaseHeld, Is.False);
        Assert.That(boundaryState.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void LegacyIntradayAdvanceUsesTheTimelineBoundary()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary(state) { HasIntradayTimeline = true };

        Assert.That(state.TryAdvanceDay(boundary, out _), Is.True);
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "intraday" }));
    }

    [Test]
    public void NonAdmittedSingleAdvanceKeepsIntradayTimelinePrecedence()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary(state)
        {
            HasIntradayTimeline = true
        };

        Assert.That(state.TryAdvanceDay(boundary, out _), Is.True);
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "intraday" }));
        Assert.That(boundary.Calls, Does.Not.Contain("execution-boundary"));
        Assert.That(boundary.Calls, Does.Not.Contain("advance"));
    }

    [Test]
    public void UnadmittedBatchAdvanceKeepsIntradayTimelinePrecedence()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary(state)
        {
            HasIntradayTimeline = true
        };

        Assert.That(state.TryAdvanceDays(
            boundary, 1, out int advanced, out SimulationRuntimeAdvanceFailure failure), Is.True);
        Assert.That(advanced, Is.EqualTo(1));
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.None));
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "intraday" }));
        Assert.That(state.CompletedCoreSequence, Is.Zero,
            "Unadmitted legacy timelines do not publish selected Daily-v1 capture sequence.");
        Assert.That(boundary.Calls, Does.Not.Contain("execution-boundary"));
        Assert.That(boundary.Calls, Does.Not.Contain("selected-clock"));
        Assert.That(boundary.Calls, Does.Not.Contain("advance"));
    }

    [Test]
    public void BatchRejectsNegativeCountAndLeavesZeroCountWithoutLeaseWork()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        FakeBoundary boundary = new FakeBoundary(state);

        Assert.That(state.TryAdvanceDays(
            boundary, -1, out int negativeAdvanced, out var negativeFailure), Is.False);
        Assert.That(negativeAdvanced, Is.Zero);
        Assert.That(negativeFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.InvalidDayCount));
        Assert.That(state.TryAdvanceDays(
            boundary, 0, out int zeroAdvanced, out var zeroFailure), Is.True);
        Assert.That(zeroAdvanced, Is.Zero);
        Assert.That(zeroFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.None));
        Assert.That(boundary.Calls, Is.Empty);
    }

    [Test]
    public void BatchRecordsEachCompletedBoundaryAndPublishesOnlyAfterAllDays()
    {
        DailyV1ExecutionBoundaryState state = CreateCaptureState();
        FakeBoundary boundary = new FakeBoundary(state);

        Assert.That(state.TryAdvanceDays(
            boundary, 3, out int advanced, out var failure), Is.True);
        Assert.That(advanced, Is.EqualTo(3));
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.None));
        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "operation",
            "advance", "advance", "advance", "operation-end", "capture-evidence"
        }));
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(3L));
        Assert.That(boundary.SequenceObservedAtEvidenceCapture, Is.EqualTo(3L));
        Assert.That(state.CurrentCaptureToken, Is.Not.Null);
        Assert.That(state.CurrentCaptureToken.OwnerSections, Is.SameAs(boundary.CapturedOwnerSections));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
        Assert.That(boundary.OperationCountAtP12ScopeBegin, Is.EqualTo(1));
        Assert.That(boundary.OperationCountAtCapture, Is.EqualTo(1));
        Assert.That(state.ActiveOperationCount, Is.Zero,
            "One Core-owned DailyAdvance scope covers the full batch and its final capture publication.");
    }

    [Test]
    public void SingleAdvanceFaultsWhenCoreCannotCloseTheOuterDailyScope()
    {
        DailyV1ExecutionBoundaryState state = CreateCaptureState();
        FakeBoundary boundary = new FakeBoundary(state)
        {
            RemoveOuterScopeDuringCapture = true
        };

        Assert.That(state.TryAdvanceDay(boundary, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(boundary.Calls, Does.Contain("fault"));
        Assert.That(state.ActiveOperationCount, Is.Zero);
        Assert.That(state.CurrentCaptureToken, Is.Null,
            "A token published before an outer-scope close failure must be invalidated when the protocol faults.");
    }

    [Test]
    public void BatchReportsPartialProgressWithoutPublishingAfterLaterAdvanceFailure()
    {
        DailyV1ExecutionBoundaryState state = CreateCaptureState();
        FakeBoundary boundary = new FakeBoundary(state) { FailAdvanceNumber = 2 };

        Assert.That(state.TryAdvanceDays(
            boundary, 3, out int advanced, out var failure), Is.False);
        Assert.That(advanced, Is.EqualTo(1));
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed));
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(1L));
        Assert.That(boundary.Calls, Does.Not.Contain("capture-evidence"));
        Assert.That(boundary.SequenceObservedAtEvidenceCapture, Is.EqualTo(-1L));
        Assert.That(state.CurrentCaptureToken, Is.Null);
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void BatchExceptionFaultsAdmittedRuntime()
    {
        DailyV1ExecutionBoundaryState state = CreateCaptureState();
        FakeBoundary boundary = new FakeBoundary(state)
        {
            HasIntradayTimeline = true,
            ThrowDuringAdvance = true
        };

        Assert.Throws<InvalidOperationException>(() =>
            state.TryAdvanceDays(boundary, 1, out _, out _));
        Assert.That(boundary.Calls, Does.Contain("fault"));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public async Task AdmittedAdvanceUsesCoreOwnerThreadIdentityAndFaultsProtocolOnMismatch()
    {
        DailyV1ExecutionBoundaryState state = CreateCaptureState();
        FakeBoundary boundary = new FakeBoundary(state);

        (bool advanced, SimulationRuntimeAdvanceFailure failure) result =
            await Task.Run(() =>
            {
                bool advanced = state.TryAdvanceDay(
                    boundary, out SimulationRuntimeAdvanceFailure failure);
                return (advanced, failure);
            });

        Assert.That(result.advanced, Is.False);
        Assert.That(result.failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "fault" }));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    private sealed class FakeBoundary : IDailyV1RuntimeAdvanceBoundary
    {
        private readonly DailyV1ExecutionBoundaryState state;
        internal FakeBoundary(DailyV1ExecutionBoundaryState state) => this.state = state;
        internal List<string> Calls { get; } = new List<string>();
        internal int FailAdvanceNumber { get; set; }
        internal bool ThrowDuringAdvance { get; set; }
        internal bool CanMutate
        {
            get => state.CanMutate;
            set
            {
                if (!value)
                    state.MutationGuard.MarkFaulted(AuthoritativeMutationFaultReason.RollbackRestoreFailed);
            }
        }
        internal bool HasIntradayTimeline { get; set; }
        internal bool RemoveOuterScopeDuringCapture { get; set; }
        internal long SequenceObservedAtEvidenceCapture { get; private set; } = -1L;
        internal int OperationCountAtP12ScopeBegin { get; private set; }
        internal int OperationCountAtCapture { get; private set; }
        internal IReadOnlyList<OwnerSectionCensusSnapshot> CapturedOwnerSections { get; private set; }
        DailyV1ExecutionBoundaryState IDailyV1RuntimeAdvanceBoundary.ExecutionState => state;
        bool IDailyV1RuntimeAdvanceBoundary.HasIntradayTimeline => HasIntradayTimeline;

        public bool TryEnterDailyAdvanceOperation(out IDisposable operationScope)
        {
            Calls.Add("operation");
            OperationCountAtP12ScopeBegin = state.ActiveOperationCount;
            operationScope = new FakeOperationScope(() => Calls.Add("operation-end"));
            return true;
        }

        public bool TryCreateSelectedDailyExecutionBoundary(
            out IDailyV1SelectedDailyExecutionBoundary executionBoundary)
        {
            Calls.Add("execution-boundary");
            executionBoundary = null;
            return false;
        }

        public bool TryAdvanceDayCore(out SimulationRuntimeAdvanceFailure failure) => Advance("advance", out failure);

        public bool TryAdvanceIntradayToNextDayBoundary(out SimulationRuntimeAdvanceFailure failure) =>
            Advance("intraday", out failure);

        public bool TryCaptureCompletedDailyCaptureEvidence(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure)
        {
            Calls.Add("capture-evidence");
            OperationCountAtCapture = state.ActiveOperationCount;
            if (RemoveOuterScopeDuringCapture)
                Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
            SequenceObservedAtEvidenceCapture = state.CompletedCoreSequence;
            ownerSections = DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
            CapturedOwnerSections = ownerSections;
            mutationEpoch = 1L;
            failure = DailyCaptureEligibilityFailure.None;
            return true;
        }

        public void FaultRuntimeAdmission() => Calls.Add("fault");

        private bool Advance(string call, out SimulationRuntimeAdvanceFailure failure)
        {
            Calls.Add(call);
            if (ThrowDuringAdvance) throw new InvalidOperationException("fixture advance failure");
            int advances = Calls.FindAll(value => value == call).Count;
            failure = FailAdvanceNumber > 0 && advances == FailAdvanceNumber
                ? SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed
                : SimulationRuntimeAdvanceFailure.None;
            return failure == SimulationRuntimeAdvanceFailure.None;
        }
    }

    private sealed class FakeOperationScope : IDisposable
    {
        private readonly Action onDispose;

        internal FakeOperationScope(Action onDispose) => this.onDispose = onDispose;

        public void Dispose() => onDispose?.Invoke();
    }

    private static DailyV1ExecutionBoundaryState CreateCaptureState(long absoluteDay = 0L)
    {
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow();
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState(
            new SimulationTime(absoluteDay), admission, SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()));
        Assert.That(state.TryBindRuntimeConfiguration(configuration, calendar), Is.True);
        return state;
    }

}
