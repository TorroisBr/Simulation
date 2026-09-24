using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class BattleOutcomeApplicationTests
{
    private const string NumericProfile = "unity-float32-current-host:v1";

    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void Apply_MultipleSourcesCommitsExactDeathsAndSingleStoreRevisions()
    {
        Fixture fixture = CreateFixture(MultiSourceSeeds(), DeathRule(1L), recorder: CreateSuccessfulRecorder());
        long battleRevision = fixture.World.BattleStore.Revision;
        long manpowerRevision = fixture.World.ContingentManpowerStateStore.Revision;
        long forceRevision = fixture.World.ArmedForceStore.Revision;
        Dictionary<string, long> stateRevisions = CaptureStateRevisions(fixture);
        Dictionary<string, long> populationRevisions = CapturePopulationRevisions(fixture);
        Dictionary<string, int> populations = CapturePopulations(fixture);

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(result.Status, Is.EqualTo(BattleOutcomeApplicationStatus.Applied));
        Assert.That(result.Outcome, Is.Not.Null);
        Assert.That(result.Outcome.ResolvedAbsoluteDay, Is.EqualTo(fixture.World.CurrentDay));
        Assert.That(fixture.World.BattleStore.Revision, Is.EqualTo(battleRevision + 1L));
        Assert.That(fixture.World.ContingentManpowerStateStore.Revision, Is.EqualTo(manpowerRevision + 1L));
        Assert.That(fixture.World.ArmedForceStore.Revision, Is.EqualTo(forceRevision + 1L));
        Assert.That(fixture.World.ArmedForceStore.TryGetContingent(new ContingentId("contingent-a1"), out ContingentRecord contingentA1), Is.True);
        Assert.That(fixture.World.ArmedForceStore.TryGetContingent(new ContingentId("contingent-a2"), out ContingentRecord contingentA2), Is.True);
        Assert.That(fixture.World.ArmedForceStore.TryGetContingent(new ContingentId("contingent-b"), out ContingentRecord contingentB), Is.True);
        Assert.That(contingentA1.Amount, Is.EqualTo(4L));
        Assert.That(contingentA2.Amount, Is.EqualTo(3L));
        Assert.That(contingentB.Amount, Is.EqualTo(2L));
        Assert.That(fixture.World.BattleStore.TryGet(fixture.BattleId, out PersistentBattleRecord resolved), Is.True);
        Assert.That(resolved.LifecycleState, Is.EqualTo(BattleLifecycleState.Resolved));
        Assert.That(resolved.TerminalOutcome, Is.SameAs(result.Outcome));
        Assert.That(fixture.World.BattleStore.ValidateInvariants().IsValid, Is.True);
        Assert.That(fixture.World.BattleStore.TryAddParticipantBinding(
            fixture.BattleId,
            new BattleParticipantBinding(new BattleParticipantBindingId("late-binding"), fixture.BattleId,
                new BattleSideId("side-a"), new ArmedForceId("force-a")),
            out PersistentStateFailure lateBindingFailure), Is.False);
        Assert.That(lateBindingFailure.Code, Is.EqualTo(PersistentStateFailureCode.BattleResolutionDeferred));

        AssertStateRevisionAdvancedOnce(fixture, stateRevisions, "contingent-a1", "contingent-a2", "contingent-b");
        Assert.That(fixture.Cities["source-a"].Population.CurrentPopulation, Is.EqualTo(populations["source-a"] - 2));
        Assert.That(fixture.Cities["source-b"].Population.CurrentPopulation, Is.EqualTo(populations["source-b"] - 1));
        Assert.That(fixture.Cities["source-a"].Population.Revision, Is.EqualTo(populationRevisions["source-a"] + 1L));
        Assert.That(fixture.Cities["source-b"].Population.Revision, Is.EqualTo(populationRevisions["source-b"] + 1L));
        Assert.That(fixture.Recorder.CallCount, Is.EqualTo(1));
        Assert.That(fixture.Recorder.Captured, Is.TypeOf<BattleResolvedEvent>());
        Assert.That(fixture.Recorder.EventStore.Events, Has.Count.EqualTo(1));
        Assert.That(fixture.Recorder.History.HistoricalEvents, Has.Count.EqualTo(1));
        Assert.That(((BattleResolvedEvent)fixture.Recorder.Captured).GetParticipants(), Is.Empty);
        Assert.That(WorldStateDiagnostics.Validate(Capture(fixture)).IsValid, Is.True);
        BattleOutcomeApplicationResult retry = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);
        Assert.That(retry.Status, Is.EqualTo(BattleOutcomeApplicationStatus.AlreadyResolved));
        Assert.That(fixture.Recorder.CallCount, Is.EqualTo(1));

        SimulationRuntime clonedRuntime = new SimulationRuntime(
            new SimulationTime(1L), null, null, economyEnabled: false,
            armedForceStore: fixture.World.ArmedForceStore,
            battleStore: fixture.World.BattleStore,
            spatialAuthorityStore: fixture.World.SpatialAuthorityStore);
        Assert.That(clonedRuntime.BattleStore.TryGet(fixture.BattleId, out PersistentBattleRecord cloned), Is.True);
        Assert.That(cloned.LifecycleState, Is.EqualTo(BattleLifecycleState.Resolved));
        Assert.That(cloned.TerminalOutcome.Provenance.D6B2PlanFingerprint,
            Is.EqualTo(resolved.TerminalOutcome.Provenance.D6B2PlanFingerprint));
    }

    [Test]
    public void Apply_SameSourceDeathsAggregateIntoOnePopulationTransition()
    {
        Fixture fixture = CreateFixture(new[]
        {
            Seed("force-a", "contingent-a1", "source-a", 4L),
            Seed("force-a", "contingent-a2", "source-a", 3L),
            Seed("force-b", "contingent-b", "source-b", 2L)
        }, DeathRule(1L));
        long sourceARevision = fixture.Cities["source-a"].Population.Revision;
        int sourceAPopulation = fixture.Cities["source-a"].Population.CurrentPopulation;

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(result.Status, Is.EqualTo(BattleOutcomeApplicationStatus.AppliedWithPostCommitWarning));
        Assert.That(fixture.Cities["source-a"].Population.CurrentPopulation, Is.EqualTo(sourceAPopulation - 2));
        Assert.That(fixture.Cities["source-a"].Population.Revision, Is.EqualTo(sourceARevision + 1L));
    }

    [Test]
    public void Apply_UsesMixedD6B2DeathWoundAndCapturePartitionsExactly()
    {
        Fixture fixture = CreateFixture(StandardSeeds(), MixedConsequenceRule());
        long manpowerRevision = fixture.World.ContingentManpowerStateStore.Revision;
        long forceRevision = fixture.World.ArmedForceStore.Revision;
        long sourceARevision = fixture.Cities["source-a"].Population.Revision;
        long sourceBRevision = fixture.Cities["source-b"].Population.Revision;

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(result.Status, Is.EqualTo(BattleOutcomeApplicationStatus.AppliedWithPostCommitWarning));
        Assert.That(fixture.World.ContingentManpowerStateStore.Revision, Is.EqualTo(manpowerRevision + 1L));
        Assert.That(fixture.World.ArmedForceStore.Revision, Is.EqualTo(forceRevision + 1L));
        Assert.That(fixture.World.ContingentManpowerStateStore.TryGet(new ContingentId("contingent-a"), out ContingentManpowerState sideA), Is.True);
        Assert.That(sideA.LivingRosterAmount, Is.EqualTo(4L));
        Assert.That(sideA.AvailableAmount, Is.EqualTo(3L));
        Assert.That(sideA.Revision, Is.EqualTo(3L));
        Assert.That(sideA.Cohorts, Has.Count.EqualTo(3));
        Assert.That(sideA.Cohorts, Has.Some.Matches<ContingentManpowerCohort>(cohort =>
            cohort.InjuryState == ManpowerInjuryState.Healthy
            && cohort.CustodyState == ManpowerCustodyState.Free
            && cohort.CustodianForceId == null
            && cohort.AvailabilityState == ManpowerAvailabilityState.Available
            && cohort.Amount == 2L));
        Assert.That(sideA.Cohorts, Has.Some.Matches<ContingentManpowerCohort>(cohort =>
            cohort.InjuryState == ManpowerInjuryState.Wounded
            && cohort.CustodyState == ManpowerCustodyState.Free
            && cohort.CustodianForceId == null
            && cohort.AvailabilityState == ManpowerAvailabilityState.Available
            && cohort.Amount == 1L));
        Assert.That(sideA.Cohorts, Has.Some.Matches<ContingentManpowerCohort>(cohort =>
            cohort.InjuryState == ManpowerInjuryState.Healthy
            && cohort.CustodyState == ManpowerCustodyState.Captured
            && cohort.CustodianForceId?.Value == "force-b"
            && cohort.AvailabilityState == ManpowerAvailabilityState.Unavailable
            && cohort.Amount == 1L));
        Assert.That(fixture.World.ContingentManpowerStateStore.TryGet(new ContingentId("contingent-b"), out ContingentManpowerState sideB), Is.True);
        Assert.That(sideB.LivingRosterAmount, Is.EqualTo(3L));
        Assert.That(sideB.AvailableAmount, Is.EqualTo(3L));
        Assert.That(sideB.Revision, Is.EqualTo(3L));
        Assert.That(sideB.Cohorts, Has.Count.EqualTo(1));
        Assert.That(sideB.Cohorts[0].InjuryState, Is.EqualTo(ManpowerInjuryState.Healthy));
        Assert.That(sideB.Cohorts[0].CustodyState, Is.EqualTo(ManpowerCustodyState.Free));
        Assert.That(sideB.Cohorts[0].CustodianForceId, Is.Null);
        Assert.That(sideB.Cohorts[0].AvailabilityState, Is.EqualTo(ManpowerAvailabilityState.Available));
        Assert.That(sideB.Cohorts[0].Amount, Is.EqualTo(3L));
        Assert.That(fixture.World.ArmedForceStore.TryGetContingent(new ContingentId("contingent-a"), out ContingentRecord sideAMirror), Is.True);
        Assert.That(fixture.World.ArmedForceStore.TryGetContingent(new ContingentId("contingent-b"), out ContingentRecord sideBMirror), Is.True);
        Assert.That(sideAMirror.Amount, Is.EqualTo(sideA.LivingRosterAmount));
        Assert.That(sideBMirror.Amount, Is.EqualTo(sideB.LivingRosterAmount));
        Assert.That(fixture.Cities["source-a"].Population.CurrentPopulation, Is.EqualTo(999));
        Assert.That(fixture.Cities["source-b"].Population.CurrentPopulation, Is.EqualTo(999));
        Assert.That(fixture.Cities["source-a"].Population.Revision, Is.EqualTo(sourceARevision + 1L));
        Assert.That(fixture.Cities["source-b"].Population.Revision, Is.EqualTo(sourceBRevision + 1L));
    }

    [Test]
    public void Apply_ZeroConsequencesOnlyChangesBattleTerminalState()
    {
        Fixture fixture = CreateFixture(StandardSeeds(), UnchangedRule());
        string before = WorldStateCanonicalWriter.Write(Capture(fixture));
        long battleRevision = fixture.World.BattleStore.Revision;
        long forceRevision = fixture.World.ArmedForceStore.Revision;
        long manpowerRevision = fixture.World.ContingentManpowerStateStore.Revision;
        Dictionary<string, long> stateRevisions = CaptureStateRevisions(fixture);
        Dictionary<string, long> populationRevisions = CapturePopulationRevisions(fixture);

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(result.Status, Is.EqualTo(BattleOutcomeApplicationStatus.AppliedWithPostCommitWarning));
        Assert.That(fixture.World.BattleStore.Revision, Is.EqualTo(battleRevision + 1L));
        Assert.That(fixture.World.ArmedForceStore.Revision, Is.EqualTo(forceRevision));
        Assert.That(fixture.World.ContingentManpowerStateStore.Revision, Is.EqualTo(manpowerRevision));
        Assert.That(CaptureStateRevisions(fixture), Is.EquivalentTo(stateRevisions));
        Assert.That(CapturePopulationRevisions(fixture), Is.EquivalentTo(populationRevisions));
        Assert.That(WorldStateCanonicalWriter.Write(Capture(fixture)), Is.Not.EqualTo(before));
    }

    [Test]
    public void ExpectedFingerprintMismatchAndResolvedRetryNeverReplan()
    {
        DelegateRule rule = DeathRule(1L);
        Fixture fixture = CreateFixture(StandardSeeds(), rule);
        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            fixture.BattleId, out BattleDirectConsequencePlan plan, out BattleDirectConsequenceFailure planningFailure),
            Is.True, planningFailure?.ToString());
        string acceptedFingerprint = plan.Fingerprint;
        long beforeMismatch = fixture.World.BattleStore.Revision;
        string worldBeforeMismatch = WorldStateCanonicalWriter.Write(Capture(fixture));

        BattleOutcomeApplicationResult mismatch = fixture.World.BattleOutcomeApplicationService.Apply(
            fixture.BattleId, expectedD6B2PlanFingerprint: "not-the-plan");

        Assert.That(mismatch.Status, Is.EqualTo(BattleOutcomeApplicationStatus.Rejected));
        Assert.That(mismatch.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.ExpectedFingerprintMismatch));
        Assert.That(fixture.World.BattleStore.Revision, Is.EqualTo(beforeMismatch));
        Assert.That(WorldStateCanonicalWriter.Write(Capture(fixture)), Is.EqualTo(worldBeforeMismatch));
        BattleOutcomeApplicationResult applied = fixture.World.BattleOutcomeApplicationService.Apply(
            fixture.BattleId, expectedD6B2PlanFingerprint: acceptedFingerprint);
        Assert.That(applied.Status, Is.EqualTo(BattleOutcomeApplicationStatus.AppliedWithPostCommitWarning));
        int callsAfterApply = rule.CallCount;

        rule.RuleKey = "test.changed-after-resolution:v1";
        BattleOutcomeApplicationResult wrongRetry = fixture.World.BattleOutcomeApplicationService.Apply(
            fixture.BattleId, expectedD6B2PlanFingerprint: "different-accepted-plan");
        BattleOutcomeApplicationResult retry = fixture.World.BattleOutcomeApplicationService.Apply(
            fixture.BattleId, expectedD6B2PlanFingerprint: acceptedFingerprint);

        Assert.That(wrongRetry.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.ExpectedFingerprintMismatch));
        Assert.That(retry.Status, Is.EqualTo(BattleOutcomeApplicationStatus.AlreadyResolved));
        Assert.That(retry.Outcome.Provenance.D6B2PlanFingerprint, Is.EqualTo(acceptedFingerprint));
        Assert.That(rule.CallCount, Is.EqualTo(callsAfterApply));
    }

    [Test]
    public void D6B2PlanningThatSeesStaleManpowerIsRejectedWithoutApplyingItsPlan()
    {
        Fixture fixture = null;
        bool externalMutationMade = false;
        string stateAfterExternalMutation = null;
        DelegateRule rule = new DelegateRule("test.d7.stale-manpower:v1", input =>
        {
            if (!externalMutationMade && fixture != null)
            {
                Assert.That(fixture.World.ContingentManpowerStateStore.TryGet(
                    new ContingentId("contingent-a"), out ContingentManpowerState state), Is.True);
                ContingentManpowerCohort wounded = new ContingentManpowerCohort(
                    ManpowerInjuryState.Wounded,
                    ManpowerCustodyState.Free,
                    null,
                    ManpowerAvailabilityState.Available,
                    state.LivingRosterAmount);
                Assert.That(fixture.World.ContingentManpowerStateStore.TryRedistribute(
                    state.ContingentId, new[] { wounded }, state.Revision,
                    out ContingentManpowerFailure mutationFailure), Is.True, mutationFailure.ToString());
                externalMutationMade = true;
                stateAfterExternalMutation = WorldStateCanonicalWriter.Write(Capture(fixture));
            }
            return UnchangedPartitions(input);
        });
        fixture = CreateFixture(StandardSeeds(), rule);

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(externalMutationMade, Is.True);
        Assert.That(result.Status, Is.EqualTo(BattleOutcomeApplicationStatus.Rejected));
        Assert.That(result.Failure.Code == BattleOutcomeApplicationFailureCode.PlanStale
            || result.Failure.Code == BattleOutcomeApplicationFailureCode.PlanningFailed
            || result.Failure.Code == BattleOutcomeApplicationFailureCode.ManpowerPrepareFailed,
            Is.True, result.Failure.Code.ToString());
        Assert.That(WorldStateCanonicalWriter.Write(Capture(fixture)), Is.EqualTo(stateAfterExternalMutation));
    }

    [Test]
    public void PendingBattleIsRejectedBeforePlanning()
    {
        DelegateRule rule = UnchangedRule();
        Fixture fixture = CreateFixture(StandardSeeds(), rule, startBattle: false);
        string before = WorldStateCanonicalWriter.Write(Capture(fixture));

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(result.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.BattleNotActive));
        Assert.That(rule.CallCount, Is.Zero);
        Assert.That(WorldStateCanonicalWriter.Write(Capture(fixture)), Is.EqualTo(before));
    }

    [TestCase("AfterManpowerCommit")]
    [TestCase("AfterFirstSourceCommit")]
    [TestCase("BeforeBattleCommit")]
    [TestCase("AfterBattleCommit")]
    public void UnexpectedMidTransactionFailure_RestoresWorldTruthAndEveryRevision(string injection)
    {
        Fixture fixture = CreateFixture(MultiSourceSeeds(), DeathRule(1L), recorder: new TestRecorder());
        string before = WorldStateCanonicalWriter.Write(Capture(fixture));
        RevisionCapture revisions = CaptureRevisions(fixture);
        SetFailureInjection(fixture, injection);

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(result.Status, Is.EqualTo(BattleOutcomeApplicationStatus.Rejected));
        Assert.That(result.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.CommitFailedRolledBack));
        Assert.That(WorldStateCanonicalWriter.Write(Capture(fixture)), Is.EqualTo(before));
        AssertRevisions(fixture, revisions);
        Assert.That(fixture.World.BattleStore.TryGet(fixture.BattleId, out PersistentBattleRecord battle), Is.True);
        Assert.That(battle.LifecycleState, Is.EqualTo(BattleLifecycleState.Active));
        Assert.That(battle.TerminalOutcome, Is.Null);
        Assert.That(fixture.Recorder.CallCount, Is.Zero, "A failed preterminal transaction must not publish BattleResolved.");
    }

    [Test]
    public void TerminalBattleStoreCommitFailure_RollsBackEarlierDomainWrites()
    {
        Fixture fixture = CreateFixture(MultiSourceSeeds(), DeathRule(1L));
        string before = WorldStateCanonicalWriter.Write(Capture(fixture));
        RevisionCapture revisions = CaptureRevisions(fixture);
        typeof(PersistentBattleStore).GetProperty("FailNextTerminalCommitForTests", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(fixture.World.BattleStore, true);

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(result.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.CommitFailedRolledBack));
        Assert.That(WorldStateCanonicalWriter.Write(Capture(fixture)), Is.EqualTo(before));
        AssertRevisions(fixture, revisions);
    }

    [Test]
    public void TerminalBattleStoreExceptionAfterAssignment_RestoresBattleAndEarlierDomains()
    {
        Fixture fixture = CreateFixture(MultiSourceSeeds(), DeathRule(1L));
        string before = WorldStateCanonicalWriter.Write(Capture(fixture));
        RevisionCapture revisions = CaptureRevisions(fixture);
        SetPersistentBattleFailure(fixture, "ThrowAfterTerminalWriteForTests");

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(result.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.CommitFailedRolledBack));
        Assert.That(WorldStateCanonicalWriter.Write(Capture(fixture)), Is.EqualTo(before));
        AssertRevisions(fixture, revisions);
        Assert.That(fixture.World.BattleStore.TryGet(fixture.BattleId, out PersistentBattleRecord battle), Is.True);
        Assert.That(battle.LifecycleState, Is.EqualTo(BattleLifecycleState.Active));
        Assert.That(battle.TerminalOutcome, Is.Null);
    }

    [Test]
    public void SecondSourcePreparationFailure_HappensBeforeAnyTransactionWrite()
    {
        Fixture fixture = CreateFixture(MultiSourceSeeds(), DeathRule(1L));
        string before = WorldStateCanonicalWriter.Write(Capture(fixture));
        RevisionCapture revisions = CaptureRevisions(fixture);
        SetFailureInjection(fixture, "BeforeSecondSourcePrepare");

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(result.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.SourcePrepareFailed));
        Assert.That(WorldStateCanonicalWriter.Write(Capture(fixture)), Is.EqualTo(before));
        AssertRevisions(fixture, revisions);
    }

    [Test]
    public void RollbackFailureFaultsRuntimeAndNextApplyRejectsBeforePlanning()
    {
        DelegateRule rule = DeathRule(1L);
        Fixture fixture = CreateFixture(StandardSeeds(), rule);
        SetFailureInjection(fixture, "AfterManpowerCommit, RollbackFailure");

        BattleOutcomeApplicationResult first = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);
        int calls = rule.CallCount;
        BattleOutcomeApplicationResult second = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(first.Status, Is.EqualTo(BattleOutcomeApplicationStatus.TransactionFaulted));
        Assert.That(first.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.RollbackFailed));
        Assert.That(fixture.World.IsMutationFaulted, Is.True);
        Assert.That(second.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.RuntimeFaulted));
        Assert.That(rule.CallCount, Is.EqualTo(calls));
    }

    [Test]
    public void FaultedRuntimeRejectsBeforeD5OrD6B2Planning()
    {
        DelegateRule rule = UnchangedRule();
        Fixture fixture = CreateFixture(StandardSeeds(), rule);
        object guard = typeof(SimulationRuntime).GetField("mutationGuard", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(fixture.World);
        MethodInfo markFaulted = guard.GetType().GetMethod("MarkFaulted", BindingFlags.Instance | BindingFlags.NonPublic);
        Type reasonType = markFaulted.GetParameters()[0].ParameterType;
        markFaulted.Invoke(guard, new[] { Enum.Parse(reasonType, "RollbackRestoreFailed") });
        int calls = rule.CallCount;

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(result.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.RuntimeFaulted));
        Assert.That(rule.CallCount, Is.EqualTo(calls));
    }

    [Test]
    public void ReentrantApplicationIsRejectedWithoutBlockingOuterApply()
    {
        BattleOutcomeApplicationResult nested = null;
        Fixture fixture = null;
        DelegateRule rule = new DelegateRule("test.reentrant:v1", input =>
        {
            if (fixture != null && nested == null)
                nested = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);
            return UnchangedPartitions(input);
        });
        fixture = CreateFixture(StandardSeeds(), rule);

        BattleOutcomeApplicationResult outer = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);

        Assert.That(nested.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.ReentrantApplication));
        Assert.That(outer.Status, Is.EqualTo(BattleOutcomeApplicationStatus.AppliedWithPostCommitWarning));
    }

    [Test]
    public void UnsupportedNumericProfileAndMissingPolicyRejectWithoutMutation()
    {
        Fixture unsupported = CreateFixture(StandardSeeds(), UnchangedRule(), numericSupported: false);
        string beforeUnsupported = WorldStateCanonicalWriter.Write(Capture(unsupported));
        BattleOutcomeApplicationResult unsupportedResult = unsupported.World.BattleOutcomeApplicationService.Apply(unsupported.BattleId);
        Assert.That(unsupportedResult.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.NumericProfileUnsupported));
        Assert.That(WorldStateCanonicalWriter.Write(Capture(unsupported)), Is.EqualTo(beforeUnsupported));

        Fixture noPolicy = CreateFixture(StandardSeeds(), null);
        string beforeNoPolicy = WorldStateCanonicalWriter.Write(Capture(noPolicy));
        BattleOutcomeApplicationResult noPolicyResult = noPolicy.World.BattleOutcomeApplicationService.Apply(noPolicy.BattleId);
        Assert.That(noPolicyResult.Failure.Code, Is.EqualTo(BattleOutcomeApplicationFailureCode.PolicyNotConfigured));
        Assert.That(WorldStateCanonicalWriter.Write(Capture(noPolicy)), Is.EqualTo(beforeNoPolicy));
    }

    [Test]
    public void EventFailureAndMissingRecorderReturnPostCommitWarningWithoutRollback()
    {
        Fixture falseRecorder = CreateFixture(StandardSeeds(), UnchangedRule(), recorder: new TestRecorder(returnFalse: true));
        BattleOutcomeApplicationResult failedRecord = falseRecorder.World.BattleOutcomeApplicationService.Apply(falseRecorder.BattleId);
        Assert.That(failedRecord.Status, Is.EqualTo(BattleOutcomeApplicationStatus.AppliedWithPostCommitWarning));
        Assert.That(falseRecorder.World.BattleStore.TryGet(falseRecorder.BattleId, out PersistentBattleRecord first), Is.True);
        Assert.That(first.LifecycleState, Is.EqualTo(BattleLifecycleState.Resolved));

        Fixture throwingRecorder = CreateFixture(StandardSeeds(), UnchangedRule(), recorder: new TestRecorder(throwOnRecord: true));
        BattleOutcomeApplicationResult threw = throwingRecorder.World.BattleOutcomeApplicationService.Apply(throwingRecorder.BattleId);
        Assert.That(threw.Status, Is.EqualTo(BattleOutcomeApplicationStatus.AppliedWithPostCommitWarning));
        Assert.That(throwingRecorder.World.BattleStore.TryGet(throwingRecorder.BattleId, out PersistentBattleRecord second), Is.True);
        Assert.That(second.LifecycleState, Is.EqualTo(BattleLifecycleState.Resolved));

        Fixture noRecorder = CreateFixture(StandardSeeds(), UnchangedRule());
        BattleOutcomeApplicationResult absent = noRecorder.World.BattleOutcomeApplicationService.Apply(noRecorder.BattleId);
        Assert.That(absent.Status, Is.EqualTo(BattleOutcomeApplicationStatus.AppliedWithPostCommitWarning));
        Assert.That(absent.Failure.Message, Does.Contain("no BattleResolved event recorder"));
    }

    [Test]
    public void RuntimeComposesWorldBoundServiceAndResolvedOutcomeDiagnosticsAreDeterministic()
    {
        Fixture fixture = CreateFixture(StandardSeeds(), UnchangedRule());
        object runtimeGuard = typeof(SimulationRuntime).GetField("mutationGuard", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(fixture.World);
        object serviceGuard = typeof(BattleOutcomeApplicationService).GetField("mutationGuard", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(fixture.World.BattleOutcomeApplicationService);
        Assert.That(serviceGuard, Is.SameAs(runtimeGuard));
        Assert.That(fixture.World.BattleOutcomeApplicationService, Is.Not.Null);
        WorldStateSnapshot before = Capture(fixture);

        BattleOutcomeApplicationResult result = fixture.World.BattleOutcomeApplicationService.Apply(fixture.BattleId);
        WorldStateSnapshot after = Capture(fixture);
        string canonical = WorldStateCanonicalWriter.Write(after);

        Assert.That(WorldStateDiagnostics.Validate(after).IsValid, Is.True);
        Assert.That(WorldStateCanonicalWriter.Write(after), Is.EqualTo(canonical));
        Assert.That(canonical, Does.Contain("BATTLE_OUTCOME_PROVENANCE"));
        Assert.That(WorldStateSnapshotFormatter.Format(after), Does.Contain("BATTLE OUTCOME PROVENANCE"));
        WorldStateDiff diff = WorldStateDiagnostics.Compare(before, after);
        List<string> differenceFieldValues = new List<string>();
        foreach (WorldStateDifference difference in diff.Differences)
            differenceFieldValues.Add(difference.Section + "/" + difference.Field);
        string differenceFields = string.Join(",", differenceFieldValues.ToArray());
        Assert.That(diff.Differences, Has.Some.Matches<WorldStateDifference>(value => value.Section == "Battle" && value.Field == "D6B2PlanFingerprint"), differenceFields);
        Assert.That(result.Outcome, Is.Not.Null);
        Assert.That(fixture.World.CurrentDay, Is.EqualTo(1L));
        AssertNoUnrelatedBattleEffects(diff);
    }

    private static Fixture CreateFixture(
        IReadOnlyList<SeedSpec> seeds,
        DelegateRule rule,
        IDomainEventRecorder recorder = null,
        bool numericSupported = true,
        bool startBattle = true)
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        HashSet<string> forceIds = new HashSet<string>(StringComparer.Ordinal) { "force-a", "force-b" };
        foreach (SeedSpec seed in seeds) forceIds.Add(seed.ForceId);
        foreach (string forceId in forceIds)
            Assert.That(forces.TryRegister(new ArmedForceRecord(new ArmedForceId(forceId), forceId, 0L), out ArmedForceFoundationFailure forceFailure), Is.True, forceFailure.ToString());

        Dictionary<string, List<SeedSpec>> byContingent = new Dictionary<string, List<SeedSpec>>(StringComparer.Ordinal);
        foreach (SeedSpec seed in seeds)
        {
            if (!byContingent.TryGetValue(seed.ContingentId, out List<SeedSpec> grouped))
            {
                grouped = new List<SeedSpec>();
                byContingent.Add(seed.ContingentId, grouped);
            }
            grouped.Add(seed);
        }
        foreach (KeyValuePair<string, List<SeedSpec>> entry in byContingent)
        {
            SeedSpec first = entry.Value[0];
            Assert.That(forces.TryRegisterContingent(new ContingentRecord(
                new ContingentId(first.ContingentId), new ArmedForceId(first.ForceId), 0L,
                new ContingentOriginReference("p7-d7-test", first.ContingentId), "test-service"), out ArmedForceFoundationFailure failure), Is.True, failure.ToString());
        }

        Dictionary<string, CityRuntime> cities = new Dictionary<string, CityRuntime>(StringComparer.Ordinal);
        foreach (SeedSpec seed in seeds)
        {
            if (cities.ContainsKey(seed.SourceId)) continue;
            CityData data = SimulationTestFactory.CreateCityData("p7-d7-source-" + seed.SourceId);
            data.initialPopulation = 1000;
            cities.Add(seed.SourceId, new CityRuntime("city-" + seed.SourceId, data,
                new SpatialLocationRuntime("location-" + seed.SourceId)));
        }
        List<SettlementManpowerSourceRegistration> registrations = new List<SettlementManpowerSourceRegistration>();
        foreach (KeyValuePair<string, CityRuntime> city in cities)
            registrations.Add(new SettlementManpowerSourceRegistration(new ManpowerSourceId(city.Key), city.Value, 1000000L));
        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        HexId hex = new HexId("p7-d7-battle-hex");
        Assert.That(spatial.TryRegisterHex(new HexRecord(hex), out _), Is.True);
        SimulationTime time = new SimulationTime(1L);
        BattleNumericExecutionProfileCompatibility compatibility = numericSupported
            ? new BattleNumericExecutionProfileCompatibility(new[] { NumericProfile })
            : new BattleNumericExecutionProfileCompatibility(Array.Empty<string>());
        BattleResolutionPolicy d5 = new BattleResolutionPolicy(
            new TestCapabilityProvider(),
            new DeterministicBattleConflictRandomSource(19, "p7-d7-random:v1"),
            new BattleResolutionResolverSettings(0f, 0f),
            "battle-conflict-projection:v1",
            NumericProfile,
            compatibility);
        SimulationRuntime world = new SimulationRuntime(
            time,
            new List<CityRuntime>(cities.Values),
            null,
            economyEnabled: false,
            personStore: persons,
            armedForceStore: forces,
            spatialAuthorityStore: spatial,
            battleResolutionPolicy: d5,
            settlementManpowerSourceRegistrations: registrations,
            battleDirectConsequencePolicy: rule == null ? null : new BattleDirectConsequencePolicy(rule),
            battleResolvedEventRecorder: recorder);

        foreach (KeyValuePair<string, List<SeedSpec>> entry in byContingent)
        {
            SeedSpec first = entry.Value[0];
            Assert.That(world.ContingentManpowerStateStore.SourceProvider.TryGetSnapshot(new ManpowerSourceId(first.SourceId), out ManpowerSourceCapacitySnapshot source), Is.True);
            Assert.That(world.ContingentManpowerStateStore.TryGet(new ContingentId(first.ContingentId), out ContingentManpowerState state), Is.True);
            Assert.That(world.ContingentManpowerStateStore.TrySetSourceBinding(state.ContingentId, new ManpowerSourceId(first.SourceId), state.Revision, source.Fingerprint, out ContingentManpowerFailure bindingFailure), Is.True, bindingFailure.ToString());
            foreach (SeedSpec seed in entry.Value)
            {
                Assert.That(world.ContingentManpowerStateStore.TryGet(new ContingentId(seed.ContingentId), out state), Is.True);
                Assert.That(world.ContingentManpowerStateStore.TryAllocate(state.ContingentId, seed.Amount,
                    ManpowerInjuryState.Healthy, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Available, state.Revision, source.Fingerprint,
                    out ContingentManpowerFailure allocationFailure), Is.True, allocationFailure.ToString());
            }
        }
        foreach (string forceId in forceIds)
            Assert.That(world.ArmedForceSpatialStateStore.TrySetPosition(new ArmedForceId(forceId), SpatialReference.ForHex(hex), out ArmedForceSpatialFailure positionFailure), Is.True, positionFailure.ToString());

        BattleId battleId = new BattleId("p7-d7-battle");
        Assert.That(world.BattleStore.TryRegister(new PersistentBattleRecord(
            battleId, 0L,
            sides: new[] { new BattleStateSide(battleId, new BattleSideId("side-a")), new BattleStateSide(battleId, new BattleSideId("side-b")) },
            participantBindings: new[]
            {
                new BattleParticipantBinding(new BattleParticipantBindingId("binding-a"), battleId, new BattleSideId("side-a"), new ArmedForceId("force-a")),
                new BattleParticipantBinding(new BattleParticipantBindingId("binding-b"), battleId, new BattleSideId("side-b"), new ArmedForceId("force-b"))
            },
            locationReference: SpatialReference.ForHex(hex)), out PersistentStateFailure registerFailure), Is.True, registerFailure.ToString());
        if (startBattle)
            Assert.That(world.BattleStore.TryStart(battleId, world.CurrentDay, out PersistentStateFailure startFailure), Is.True, startFailure.ToString());
        return new Fixture(world, battleId, cities, recorder as TestRecorder);
    }

    private static SeedSpec[] StandardSeeds()
        => new[] { Seed("force-a", "contingent-a", "source-a", 5L), Seed("force-b", "contingent-b", "source-b", 4L) };

    private static SeedSpec[] MultiSourceSeeds()
        => new[]
        {
            Seed("force-a", "contingent-a1", "source-a", 5L),
            Seed("force-a", "contingent-a2", "source-a", 4L),
            Seed("force-b", "contingent-b", "source-b", 3L)
        };

    private static SeedSpec Seed(string force, string contingent, string source, long amount)
        => new SeedSpec(force, contingent, source, amount);

    private static DelegateRule DeathRule(long amount)
        => new DelegateRule("test.d7.death:v1", input =>
        {
            List<BattleCohortConsequencePartition> result = new List<BattleCohortConsequencePartition>();
            foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            {
                long deaths = Math.Min(amount, cohort.Amount);
                long survivors = cohort.Amount - deaths;
                result.Add(new BattleCohortConsequencePartition(cohort.Identity,
                    survivors == 0L ? Array.Empty<BattleCohortConsequenceLivingDestination>() : new[]
                    {
                        new BattleCohortConsequenceLivingDestination(cohort.Identity.InjuryState,
                            cohort.Identity.CustodyState, cohort.Identity.CustodianForceId,
                            cohort.Identity.AvailabilityState, survivors)
                    }, deaths));
            }
            return result;
        });

    private static DelegateRule UnchangedRule() => new DelegateRule("test.d7.unchanged:v1", UnchangedPartitions);

    private static DelegateRule MixedConsequenceRule()
        => new DelegateRule("test.d7.mixed:v1", input =>
        {
            List<BattleCohortConsequencePartition> partitions = new List<BattleCohortConsequencePartition>();
            foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            {
                if (cohort.Identity.ForceId.Value == "force-a")
                {
                    partitions.Add(new BattleCohortConsequencePartition(cohort.Identity, new[]
                    {
                        new BattleCohortConsequenceLivingDestination(ManpowerInjuryState.Healthy,
                            ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available, 2L),
                        new BattleCohortConsequenceLivingDestination(ManpowerInjuryState.Wounded,
                            ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available, 1L),
                        new BattleCohortConsequenceLivingDestination(ManpowerInjuryState.Healthy,
                            ManpowerCustodyState.Captured, new ArmedForceId("force-b"),
                            ManpowerAvailabilityState.Unavailable, 1L)
                    }, 1L));
                }
                else
                {
                    partitions.Add(new BattleCohortConsequencePartition(cohort.Identity, new[]
                    {
                        new BattleCohortConsequenceLivingDestination(ManpowerInjuryState.Healthy,
                            ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available, 3L)
                    }, 1L));
                }
            }
            return partitions;
        });

    private static IReadOnlyList<BattleCohortConsequencePartition> UnchangedPartitions(BattleDirectConsequenceInput input)
    {
        List<BattleCohortConsequencePartition> result = new List<BattleCohortConsequencePartition>();
        foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            result.Add(new BattleCohortConsequencePartition(cohort.Identity, new[]
            {
                new BattleCohortConsequenceLivingDestination(cohort.Identity.InjuryState,
                    cohort.Identity.CustodyState, cohort.Identity.CustodianForceId,
                    cohort.Identity.AvailabilityState, cohort.Amount)
            }, 0L));
        return result;
    }

    private static WorldStateSnapshot Capture(Fixture fixture)
        => WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: fixture.World.SimulationTime,
            cities: fixture.World.Cities,
            personStore: fixture.World.PersonStore,
            armedForceStore: fixture.World.ArmedForceStore,
            contingentManpowerStateStore: fixture.World.ContingentManpowerStateStore,
            conflictStore: fixture.World.ConflictStore,
            warStore: fixture.World.WarStore,
            battleStore: fixture.World.BattleStore,
            spatialAuthorityStore: fixture.World.SpatialAuthorityStore,
            armedForceSpatialStateStore: fixture.World.ArmedForceSpatialStateStore));

    private static Dictionary<string, long> CaptureStateRevisions(Fixture fixture)
    {
        Dictionary<string, long> result = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (ContingentManpowerState state in fixture.World.ContingentManpowerStateStore.States)
            result.Add(state.ContingentId.Value, state.Revision);
        return result;
    }

    private static Dictionary<string, long> CapturePopulationRevisions(Fixture fixture)
    {
        Dictionary<string, long> result = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, CityRuntime> city in fixture.Cities) result.Add(city.Key, city.Value.Population.Revision);
        return result;
    }

    private static Dictionary<string, int> CapturePopulations(Fixture fixture)
    {
        Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, CityRuntime> city in fixture.Cities) result.Add(city.Key, city.Value.Population.CurrentPopulation);
        return result;
    }

    private static void AssertStateRevisionAdvancedOnce(Fixture fixture, Dictionary<string, long> before, params string[] changedIds)
    {
        HashSet<string> changed = new HashSet<string>(changedIds, StringComparer.Ordinal);
        foreach (KeyValuePair<string, long> entry in before)
        {
            Assert.That(fixture.World.ContingentManpowerStateStore.TryGet(new ContingentId(entry.Key), out ContingentManpowerState after), Is.True);
            Assert.That(after.Revision, Is.EqualTo(entry.Value + (changed.Contains(entry.Key) ? 1L : 0L)), entry.Key);
        }
    }

    private static RevisionCapture CaptureRevisions(Fixture fixture)
        => new RevisionCapture(fixture.World.BattleStore.Revision,
            fixture.World.ContingentManpowerStateStore.Revision,
            fixture.World.ArmedForceStore.Revision,
            CaptureStateRevisions(fixture), CapturePopulationRevisions(fixture));

    private static void AssertRevisions(Fixture fixture, RevisionCapture before)
    {
        Assert.That(fixture.World.BattleStore.Revision, Is.EqualTo(before.Battle));
        Assert.That(fixture.World.ContingentManpowerStateStore.Revision, Is.EqualTo(before.Manpower));
        Assert.That(fixture.World.ArmedForceStore.Revision, Is.EqualTo(before.Forces));
        Assert.That(CaptureStateRevisions(fixture), Is.EquivalentTo(before.Contingents));
        Assert.That(CapturePopulationRevisions(fixture), Is.EquivalentTo(before.Populations));
    }

    private static void AssertNoUnrelatedBattleEffects(WorldStateDiff diff)
    {
        HashSet<string> forbiddenSections = new HashSet<string>(StringComparer.Ordinal)
        {
            "Metadata", "NPC", "Person", "ArmedForcePosition", "SpatialAuthorityStore",
            "ConflictStore", "Conflict", "ConflictSide", "ConflictParticipantBinding",
            "WarStore", "War", "WarSide", "WarParticipantBinding",
            "PoliticalClaim", "PoliticalClaimRecognition", "Faction", "FactionAffiliation",
            "PoliticalSupport", "PoliticalDecision", "PoliticalKnowledge", "PoliticalKnowledgeObservation"
        };
        foreach (WorldStateDifference difference in diff.Differences)
            Assert.That(forbiddenSections.Contains(difference.Section), Is.False,
                "D7 changed out-of-scope state: " + difference.Section + "/" + difference.Field);
    }

    private static void SetFailureInjection(Fixture fixture, string names)
    {
        PropertyInfo property = typeof(BattleOutcomeApplicationService).GetProperty("FailureInjectionForTests", BindingFlags.Instance | BindingFlags.NonPublic);
        object flags = Enum.Parse(property.PropertyType, names);
        property.SetValue(fixture.World.BattleOutcomeApplicationService, flags);
    }

    private static void SetPersistentBattleFailure(Fixture fixture, string propertyName)
    {
        PropertyInfo property = typeof(PersistentBattleStore).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        property.SetValue(fixture.World.BattleStore, true);
    }

    private static TestRecorder CreateSuccessfulRecorder()
    {
        HistoryStore history = new HistoryStore();
        DomainEventStore eventStore = new DomainEventStore(history, new HistoryPolicy());
        return new TestRecorder(eventStore, history);
    }

    private sealed class SeedSpec
    {
        public readonly string ForceId;
        public readonly string ContingentId;
        public readonly string SourceId;
        public readonly long Amount;
        public SeedSpec(string forceId, string contingentId, string sourceId, long amount)
        {
            ForceId = forceId;
            ContingentId = contingentId;
            SourceId = sourceId;
            Amount = amount;
        }
    }

    private sealed class Fixture
    {
        public readonly SimulationRuntime World;
        public readonly BattleId BattleId;
        public readonly Dictionary<string, CityRuntime> Cities;
        public readonly TestRecorder Recorder;
        public Fixture(SimulationRuntime world, BattleId battleId, Dictionary<string, CityRuntime> cities, TestRecorder recorder)
        {
            World = world;
            BattleId = battleId;
            Cities = cities;
            Recorder = recorder;
        }
    }

    private sealed class RevisionCapture
    {
        public readonly long Battle;
        public readonly long Manpower;
        public readonly long Forces;
        public readonly Dictionary<string, long> Contingents;
        public readonly Dictionary<string, long> Populations;
        public RevisionCapture(long battle, long manpower, long forces,
            Dictionary<string, long> contingents, Dictionary<string, long> populations)
        {
            Battle = battle;
            Manpower = manpower;
            Forces = forces;
            Contingents = contingents;
            Populations = populations;
        }
    }

    private sealed class DelegateRule : IBattleDirectConsequenceRule
    {
        private readonly Func<BattleDirectConsequenceInput, IReadOnlyList<BattleCohortConsequencePartition>> evaluate;
        public string RuleKey { get; set; }
        public string ConfigurationIdentity => "p7-d7-test-configuration:v1";
        public int Version => 1;
        public int CallCount { get; private set; }
        public DelegateRule(string ruleKey, Func<BattleDirectConsequenceInput, IReadOnlyList<BattleCohortConsequencePartition>> evaluate)
        {
            RuleKey = ruleKey;
            this.evaluate = evaluate;
        }
        public IReadOnlyList<BattleCohortConsequencePartition> Evaluate(BattleDirectConsequenceInput input)
        {
            CallCount++;
            return evaluate(input);
        }
    }

    private sealed class TestCapabilityProvider : IBattleContingentCapabilityProvider
    {
        public string RuleKey => "p7-d7-capability:v1";
        public bool TryEvaluate(BattleExecutionContingentSnapshot contingent, out float capability, out string failureReason)
        {
            capability = 1f;
            failureReason = null;
            return true;
        }
    }

    private sealed class TestRecorder : IDomainEventRecorder
    {
        private readonly bool returnFalse;
        private readonly bool throwOnRecord;
        public int CallCount { get; private set; }
        public DomainEvent Captured { get; private set; }
        public DomainEventStore EventStore { get; }
        public HistoryStore History { get; }

        public TestRecorder(DomainEventStore eventStore = null, HistoryStore history = null,
            bool returnFalse = false, bool throwOnRecord = false)
        {
            EventStore = eventStore;
            History = history;
            this.returnFalse = returnFalse;
            this.throwOnRecord = throwOnRecord;
        }

        public bool Record(Func<string, long, long, DomainEvent> createEvent)
        {
            CallCount++;
            if (throwOnRecord) throw new InvalidOperationException("Injected postcommit event failure.");
            if (returnFalse) return false;
            Captured = createEvent("battle-resolved-test", 1L, 1L);
            return EventStore == null || EventStore.Record(Captured);
        }
    }
}
