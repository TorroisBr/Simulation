using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;

public sealed class DailyV1ApplicationSessionGateTests
{
    private sealed class TestSession
    {
        internal readonly DailyV1ExecutionBoundaryState ExecutionState;

        internal TestSession(DailyV1ExecutionBoundaryState executionState = null)
        {
            ExecutionState = executionState ?? new DailyV1ExecutionBoundaryState();
        }
    }

    private sealed class ApplicationAdvanceBoundary : IDailyV1RuntimeAdvanceBoundary
    {
        private readonly DailyV1ExecutionBoundaryState state;
        private readonly Action onOperationEntry;

        internal ApplicationAdvanceBoundary(
            DailyV1ExecutionBoundaryState state,
            Action onOperationEntry)
        {
            this.state = state;
            this.onOperationEntry = onOperationEntry;
        }

        internal bool OperationEntered { get; private set; }
        internal bool SelectedBoundaryRequested { get; private set; }
        internal bool Faulted { get; private set; }
        public DailyV1ExecutionBoundaryState ExecutionState => state;
        public bool HasIntradayTimeline => false;

        public bool TryEnterDailyAdvanceOperation(out IDisposable operationScope)
        {
            OperationEntered = true;
            onOperationEntry?.Invoke();
            if (!state.TryBeginRuntimeProtocolOperation())
            {
                operationScope = null;
                return false;
            }

            operationScope = new RuntimeProtocolScope(state);
            return true;
        }

        public bool TryCreateSelectedDailyExecutionBoundary(
            out IDailyV1SelectedDailyExecutionBoundary executionBoundary)
        {
            SelectedBoundaryRequested = true;
            executionBoundary = null;
            return false;
        }

        public bool TryAdvanceDayCore(out SimulationRuntimeAdvanceFailure failure)
        {
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        public bool TryAdvanceIntradayToNextDayBoundary(out SimulationRuntimeAdvanceFailure failure)
        {
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        public bool TryCaptureCompletedDailyCaptureEvidence(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure)
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        public void FaultRuntimeAdmission()
        {
            Faulted = true;
            state.MutationGuard.MarkFaulted(
                AuthoritativeMutationFaultReason.RollbackRestoreFailed);
        }

        private sealed class RuntimeProtocolScope : IDisposable
        {
            private DailyV1ExecutionBoundaryState state;

            internal RuntimeProtocolScope(DailyV1ExecutionBoundaryState state) =>
                this.state = state;

            public void Dispose()
            {
                DailyV1ExecutionBoundaryState owner = state;
                if (owner == null) return;
                if (!owner.TryEndRuntimeProtocolOperation())
                    throw new InvalidOperationException("The test runtime-protocol scope did not close.");
                state = null;
            }
        }
    }

    private static DailyV1ApplicationSessionGate<TestSession> CreateGate() =>
        new DailyV1ApplicationSessionGate<TestSession>(session => session.ExecutionState);

    private static TestSession CreateAdmittedTestSession()
    {
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        return new TestSession(new DailyV1ExecutionBoundaryState(
            new SimulationTime(), admission, SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid())));
    }

    private static DailyV1Application CreateApplicationWithAdvanceBoundary(
        Action onOperationEntry,
        out DailyV1ActiveRuntimeSession session,
        out ApplicationAdvanceBoundary boundary)
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out session, out string diagnostic), Is.True, diagnostic);
        boundary = new ApplicationAdvanceBoundary(session.ExecutionBoundaryState, onOperationEntry);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, boundary), Is.True);
        Assert.That(application.TryPublishInitial(session), Is.True);
        return application;
    }

    [Test]
    public void SessionReportStateRetainsAndAdvancesEconomySnapshotDay()
    {
        DailyV1SessionReportState state = new DailyV1SessionReportState(0L);
        Assert.That(state.LastEconomySnapshotDay, Is.Zero);
        state.MarkEconomySnapshotDay(4L);
        Assert.That(state.LastEconomySnapshotDay, Is.EqualTo(4L));
    }

    [Test]
    public void ApplicationCapturesBootstrapThreadOnceAndRejectsAnotherApplicationsInitialSession()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        DailyV1Application otherApplication = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext admission), Is.True);
        Assert.That(admission.Profile,
            Is.EqualTo(SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1));
        Assert.That(admission.ExpectedOwnerThread, Is.SameAs(Thread.CurrentThread));
        Assert.That(admission.ExpectedOwnerThreadId,
            Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
        Assert.That(application.TryCaptureBootstrapAdmissionContext(out _), Is.False);
        Assert.That(otherApplication.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext otherAdmission), Is.True);
        Assert.That(otherAdmission, Is.Not.SameAs(admission));

        SimulationRuntimeAdmissionContext otherRestoredAdmission =
            otherAdmission.CreateRestoredContinuationContext();
        Assert.That(otherRestoredAdmission.IsRestoredContinuationOf(admission), Is.False,
            "A restored context from another application must not inherit this application's bootstrap lineage.");

        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions, admission,
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            CreateBoundaryWithAdmission(composition, otherRestoredAdmission),
            definitions, 0L,
            out DailyV1ActiveRuntimeSession foreignRestoredSession, out diagnostic),
            Is.True, diagnostic);
        Assert.That(application.TryPublishInitial(foreignRestoredSession), Is.False,
            "An application cannot publish a restored session admitted by a different bootstrap context on the same thread.");

        SimulationRuntimeAdmissionContext restoredAdmission =
            admission.CreateRestoredContinuationContext();
        Assert.That(restoredAdmission.IsRestoredContinuationOf(admission), Is.True);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            CreateBoundaryWithAdmission(composition, restoredAdmission), definitions, 0L,
            out DailyV1ActiveRuntimeSession session, out diagnostic), Is.True, diagnostic);
        Assert.That(application.TryPublishInitial(session), Is.True);
        Assert.That(application.ActiveSession, Is.SameAs(session));
    }

    [Test]
    public void ApplicationCapturesItsCallingHostThreadWithoutUnityThreadFactory()
    {
        DailyV1Application application = new DailyV1Application();
        SimulationRuntimeAdmissionContext captured = null;
        bool captureSucceeded = false;
        Thread startThread = new Thread(() =>
        {
            captureSucceeded = application.TryCaptureBootstrapAdmissionContext(out captured);
        });

        startThread.Start();
        Assert.That(startThread.Join(TimeSpan.FromSeconds(5)), Is.True,
            "The selected application bootstrap thread did not complete capture.");
        Assert.That(captureSucceeded, Is.True);
        Assert.That(captured.Profile,
            Is.EqualTo(SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1));
        Assert.That(captured.ExpectedOwnerThread, Is.SameAs(startThread));
        Assert.That(captured.ExpectedOwnerThreadId, Is.EqualTo(startThread.ManagedThreadId));
        Assert.That(captured.IsOwnedByCurrentThread(), Is.False,
            "The calling host thread remains the owner after bootstrap capture returns.");
    }

    [Test]
    public void ApplicationPublishesTheExactCoreGenesisOwnerGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        var completedStages = new System.Collections.Generic.List<string>();

        Assert.That(application.TryCreateAndPublishInitialGenesis(
            definitions, completedStages.Add, out string diagnostic), Is.True, diagnostic);

        DailyV1ActiveRuntimeSession session = application.ActiveSession;
        Assert.That(session, Is.Not.Null);
        Assert.That(session.Definitions, Is.SameAs(definitions));
        Assert.That(session.OwnerGraph, Is.SameAs(
            session.ExecutionBoundaryState.SelectedRuntimeOwnerGraph));
        Assert.That(session.OwnerGraph.Definitions, Is.SameAs(definitions));
        Assert.That(session.WorldId, Is.SameAs(session.ExecutionBoundaryState.WorldId));
        Assert.That(session.SimulationTime.AbsoluteDay, Is.Zero);
        Assert.That(session.AdmissionContext.IsOwnedByCurrentThread(), Is.True);
        Assert.That(application.ActiveOperationCount, Is.Zero);
        Assert.That(completedStages, Is.EqualTo(
            definitions.StageOrder.Take(definitions.StageOrder.Count - 2)));
    }

    [Test]
    public void ApplicationKeepsPreparedGenesisPrivateUntilTheExactCoreCandidateIsPublished()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext admission), Is.True);

        Assert.That(application.TryPrepareInitialGenesis(
            definitions,
            allocateWorldId: () => new WorldId(Guid.Parse("6b4d50cc-66d6-4b4c-a0f6-d0f2d9f16a19")),
            stageCompleted: null,
            out DailyV1GenesisComposition composition,
            out DailyV1ActiveRuntimeSession prepared,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(application.ActiveSession, Is.Null,
            "Preparing the Core owner graph must not publish an active application before the host finishes its projection checks.");
        Assert.That(application.ActiveOperationCount, Is.Zero);
        Assert.That(composition.AdmissionContext, Is.SameAs(admission));
        Assert.That(prepared.Definitions, Is.SameAs(definitions));
        Assert.That(prepared.ExecutionBoundaryState, Is.SameAs(composition.ExecutionBoundaryState));
        Assert.That(prepared.OwnerGraph, Is.SameAs(composition.OwnerGraph));
        Assert.That(prepared.WorldId, Is.SameAs(composition.WorldId));

        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions, admission,
            out DailyV1GenesisComposition unrelatedComposition,
            out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            unrelatedComposition.ExecutionBoundaryState,
            definitions,
            0L,
            out DailyV1ActiveRuntimeSession unrelatedSession,
            out diagnostic), Is.True, diagnostic);

        Assert.That(application.TryPublishInitial(unrelatedSession), Is.False,
            "A host cannot publish a different Core candidate after preparing this application's genesis.");
        Assert.That(application.ActiveSession, Is.Null);
        Assert.That(application.TryPublishInitial(prepared), Is.True,
            "The exact Core candidate may publish once its host projection is ready.");
        Assert.That(application.ActiveSession, Is.SameAs(prepared));
        Assert.That(application.ActiveSession.OwnerGraph, Is.SameAs(composition.OwnerGraph));
    }

    [Test]
    public void ApplicationRejectsReentrantPublicationWhileGenesisPreparationIsInProgress()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext admission), Is.True);
        bool callbackInvoked = false;
        bool reentrantPublicationAccepted = false;
        DailyV1ActiveRuntimeSession reentrantCandidate = null;

        Assert.That(application.TryPrepareInitialGenesis(
            definitions,
            allocateWorldId: () => new WorldId(Guid.Parse("d20e3757-9ed0-42ef-bade-3c628f7059bb")),
            stageCompleted: _ =>
            {
                if (callbackInvoked) return;
                callbackInvoked = true;

                Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                    definitions, admission,
                    out DailyV1GenesisComposition reentrantComposition,
                    out string reentrantDiagnostic), Is.True, reentrantDiagnostic);
                Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
                    reentrantComposition.ExecutionBoundaryState, definitions, 0L,
                    out reentrantCandidate, out reentrantDiagnostic), Is.True, reentrantDiagnostic);

                reentrantPublicationAccepted = application.TryPublishInitial(reentrantCandidate);
                Assert.That(application.ActiveSession, Is.Null,
                    "A callback cannot publish an unrelated session while the matching host projection is still being prepared.");
            },
            out DailyV1GenesisComposition composition,
            out DailyV1ActiveRuntimeSession prepared,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(callbackInvoked, Is.True);
        Assert.That(reentrantCandidate, Is.Not.Null);
        Assert.That(reentrantPublicationAccepted, Is.False,
            "Initial publication is reserved as soon as genesis preparation begins.");
        Assert.That(application.ActiveSession, Is.Null,
            "Only the exact candidate returned by preparation may publish after the host projection checks.");
        Assert.That(application.TryPublishInitial(prepared), Is.True);
        Assert.That(application.ActiveSession, Is.SameAs(prepared));
        Assert.That(application.ActiveSession.OwnerGraph, Is.SameAs(composition.OwnerGraph));
    }

    [Test]
    public void ApplicationDoesNotPublishGenesisWhenItsPreparationCallbackThrows()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(out _), Is.True);

        Assert.Throws<System.InvalidOperationException>(() => application.TryPrepareInitialGenesis(
            definitions,
            allocateWorldId: () => new WorldId(Guid.Parse("d74685dd-9cfd-4f4e-967b-307b3622182f")),
            stageCompleted: _ => throw new System.InvalidOperationException("host stage observer failed"),
            out _,
            out _,
            out _));

        Assert.That(application.ActiveSession, Is.Null,
            "A callback failure before application publication must leave the active session empty.");
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void ApplicationAdvanceReservesItsExactSessionAndReleasesAfterRejectedStageConstruction()
    {
        DailyV1Application application = null;
        DailyV1ActiveRuntimeSession session = null;
        bool applicationReentryAccepted = false;
        application = CreateApplicationWithAdvanceBoundary(
            () =>
            {
                applicationReentryAccepted = application.TryBeginOperation(
                    session, DailyV1OperationScopeKind.Application, out _);
                if (applicationReentryAccepted)
                    application.EndOperation(DailyV1OperationScopeKind.Application);
            }, out session, out ApplicationAdvanceBoundary boundary);

        Assert.That(application.TryAdvanceDay(
            session, out SimulationRuntimeAdvanceFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(boundary.OperationEntered, Is.True);
        Assert.That(boundary.SelectedBoundaryRequested, Is.True);
        Assert.That(boundary.Faulted, Is.True);
        Assert.That(applicationReentryAccepted, Is.False,
            "Application queries cannot reenter the active session while Core owns its daily advance reservation.");
        Assert.That(session.SimulationTime.AbsoluteDay, Is.Zero,
            "A missing selected execution boundary is rejected before the admitted clock changes.");
        Assert.That(session.ExecutionBoundaryState.AdvanceLeaseHeld, Is.False);
        Assert.That(application.ActiveOperationCount, Is.Zero);
        Assert.That(application.TryBeginOperation(
            session, DailyV1OperationScopeKind.Application, out DailyV1ActiveRuntimeSession admitted), Is.True,
            "The application reservation must release after a rejected stage construction.");
        Assert.That(admitted, Is.SameAs(session));
        application.EndOperation(DailyV1OperationScopeKind.Application);
    }

    [Test]
    public void ApplicationBatchAdvanceReservesExactSessionAndReleasesAfterRejectedStageConstruction()
    {
        DailyV1Application application = null;
        DailyV1ActiveRuntimeSession session = null;
        bool applicationReentryAccepted = false;
        application = CreateApplicationWithAdvanceBoundary(
            () =>
            {
                applicationReentryAccepted = application.TryBeginOperation(
                    session, DailyV1OperationScopeKind.Application, out _);
                if (applicationReentryAccepted)
                    application.EndOperation(DailyV1OperationScopeKind.Application);
            }, out session, out ApplicationAdvanceBoundary boundary);

        Assert.That(application.TryAdvanceDays(
            session, 3, out int daysAdvanced,
            out SimulationRuntimeAdvanceFailure failure), Is.False);

        Assert.That(daysAdvanced, Is.Zero,
            "A failed first Core stage cannot claim any completed days.");
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(boundary.OperationEntered, Is.True);
        Assert.That(boundary.SelectedBoundaryRequested, Is.True);
        Assert.That(boundary.Faulted, Is.True);
        Assert.That(applicationReentryAccepted, Is.False,
            "Application queries cannot reenter the active session during a batch reservation.");
        Assert.That(session.SimulationTime.AbsoluteDay, Is.Zero,
            "A missing selected execution boundary is rejected before the admitted clock changes.");
        Assert.That(session.ExecutionBoundaryState.AdvanceLeaseHeld, Is.False);
        Assert.That(application.ActiveOperationCount, Is.Zero);
        Assert.That(application.TryBeginOperation(
            session, DailyV1OperationScopeKind.Application,
            out DailyV1ActiveRuntimeSession admitted), Is.True,
            "The application reservation must release after a rejected batch stage.");
        Assert.That(admitted, Is.SameAs(session));
        application.EndOperation(DailyV1OperationScopeKind.Application);
    }

    [Test]
    public void ApplicationBatchAdvanceDelegatesInvalidCountWithoutOpeningDailyScope()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);

        Assert.That(application.TryAdvanceDays(
            session, -1, out int daysAdvanced,
            out SimulationRuntimeAdvanceFailure failure), Is.False);

        Assert.That(daysAdvanced, Is.Zero);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.InvalidDayCount));
        Assert.That(boundary.OperationEntered, Is.False,
            "A negative count is rejected before any DailyAdvance owner operation begins.");
        Assert.That(session.SimulationTime.AbsoluteDay, Is.Zero);
        Assert.That(session.ExecutionBoundaryState.AdvanceLeaseHeld, Is.False);
        Assert.That(application.ActiveOperationCount, Is.Zero);
        Assert.That(application.TryBeginOperation(
            session, DailyV1OperationScopeKind.Application,
            out DailyV1ActiveRuntimeSession admitted), Is.True);
        Assert.That(admitted, Is.SameAs(session));
        application.EndOperation(DailyV1OperationScopeKind.Application);
    }

    [Test]
    public void ApplicationBindsOneExactRuntimeAdvanceBoundaryBeforePublication()
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession session, out string diagnostic), Is.True, diagnostic);

        ApplicationAdvanceBoundary exact = new ApplicationAdvanceBoundary(
            session.ExecutionBoundaryState, null);
        ApplicationAdvanceBoundary replacement = new ApplicationAdvanceBoundary(
            session.ExecutionBoundaryState, null);
        ApplicationAdvanceBoundary foreign = new ApplicationAdvanceBoundary(
            new DailyV1ExecutionBoundaryState(), null);

        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, foreign), Is.False,
            "A host adapter for another Core execution state cannot bind to this session.");
        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, exact), Is.True);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, exact), Is.True,
            "Binding the same adapter is idempotent for retry-safe publication.");
        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, replacement), Is.False,
            "A session cannot switch host mutation and capture authorities after binding.");
        Assert.That(application.TryPublishInitial(session), Is.True);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, replacement), Is.False,
            "A published session cannot replace its exact host adapter.");
        Assert.That(session.RuntimeAdvanceBoundary, Is.SameAs(exact));
    }
    [Test]
    public void ApplicationRejectsAnotherGenesisAfterPublishingItsInitialSession()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCreateAndPublishInitialGenesis(definitions, null, out _), Is.True);
        DailyV1ActiveRuntimeSession published = application.ActiveSession;

        Assert.That(application.TryCreateAndPublishInitialGenesis(definitions, null, out string diagnostic), Is.False);

        Assert.That(diagnostic, Does.Contain("second bootstrap admission context"));
        Assert.That(application.ActiveSession, Is.SameAs(published));
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void ApplicationRejectsInitialOrReplacementSessionsWithoutItsBootstrapLineage()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        DailyV1Application foreignApplication = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext admission), Is.True);
        Assert.That(foreignApplication.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext foreignAdmission), Is.True);

        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions, admission,
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions, foreignAdmission,
            out DailyV1GenesisComposition foreignComposition,
            out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            composition.ExecutionBoundaryState, definitions, 0L,
            out DailyV1ActiveRuntimeSession session, out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            foreignComposition.ExecutionBoundaryState, definitions, 0L,
            out DailyV1ActiveRuntimeSession foreignSession, out diagnostic), Is.True, diagnostic);
        Assert.That(session.IsProfileCompatibleWith(foreignSession), Is.True,
            "Profile compatibility remains independent of application ownership lineage.");

        DailyV1Application unstartedApplication = new DailyV1Application();
        Assert.That(unstartedApplication.TryPublishInitial(session), Is.False,
            "An application must capture its bootstrap lineage before publishing a selected session.");
        Assert.That(application.TryPublishInitial(foreignSession), Is.False,
            "Initial publication must reject another application's session.");
        Assert.That(application.TryPublishInitial(session), Is.True);
        Assert.That(application.TryPublishReplacement(session, foreignSession), Is.False,
            "Replacement publication must reject another application's session even when the profile matches.");

        Assert.That(application.TryBeginExclusiveOperation(session, out var exclusive), Is.True);
        using (exclusive)
            Assert.That(application.TryPublishReplacement(exclusive, foreignSession), Is.False,
                "Exclusive replacement publication must enforce the same application lineage.");
        Assert.That(application.ActiveSession, Is.SameAs(session));
    }

    private static DailyV1ExecutionBoundaryState CreateBoundaryWithAdmission(
        DailyV1GenesisComposition composition,
        SimulationRuntimeAdmissionContext admissionContext)
    {
        DailyV1ExecutionBoundaryState source = composition.ExecutionBoundaryState;
        DailyV1ExecutionBoundaryState boundary = new DailyV1ExecutionBoundaryState(
            source.SimulationTime, admissionContext, source.CompositionProfile, source.WorldId);
        Assert.That(boundary.TryBindRuntimeConfiguration(
            source.ConfigurationIdentity, source.CalendarIdentity), Is.True);
        Assert.That(boundary.TryBindSelectedRuntimeOwnerGraph(composition.OwnerGraph), Is.True);
        Assert.That(boundary.TryBindInitialSelectedRuntimeActorRosterFromOwnerGraph(), Is.True);
        Assert.That(boundary.TryBindSelectedRuntimeExecutionRoots(
            composition.RuntimeIdAllocator,
            composition.RandomSource,
            composition.RecordSequence,
            composition.DecisionStore,
            composition.ActorChoiceStore), Is.True);
        return boundary;
    }

    [Test]
    public void ActiveRuntimeSessionBindsTheExactGenesisBoundaryAndOwnerGraph()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            composition.ExecutionBoundaryState,
            definitions,
            6L,
            out DailyV1ActiveRuntimeSession session,
            out diagnostic), Is.True, diagnostic);

        Assert.That(session.ExecutionBoundaryState, Is.SameAs(composition.ExecutionBoundaryState));
        Assert.That(session.OwnerGraph, Is.SameAs(composition.OwnerGraph));
        Assert.That(session.Definitions, Is.SameAs(composition.Definitions));
        Assert.That(session.AdmissionContext, Is.SameAs(composition.AdmissionContext));
        Assert.That(session.WorldId, Is.SameAs(composition.WorldId));
        Assert.That(session.SimulationTime, Is.SameAs(composition.SimulationTime));
        Assert.That(session.EffectiveConfiguration, Is.SameAs(composition.EffectiveConfiguration));
        Assert.That(session.Calendar, Is.SameAs(composition.Calendar));
        Assert.That(session.ReportState.LastEconomySnapshotDay, Is.EqualTo(6L));
        session.ReportState.MarkEconomySnapshotDay(7L);
        Assert.That(session.ReportState.LastEconomySnapshotDay, Is.EqualTo(7L));
    }

    [Test]
    public void ActiveRuntimeSessionRejectsOtherDefinitionGraphsAndUnadmittedBoundaries()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1EffectiveDefinitions differentDefinitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            composition.ExecutionBoundaryState,
            differentDefinitions,
            0L,
            out DailyV1ActiveRuntimeSession mismatched,
            out diagnostic), Is.False);
        Assert.That(mismatched, Is.Null);
        Assert.That(diagnostic, Does.Contain("exact admitted selected Daily-v1 runtime"));

        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            new DailyV1ExecutionBoundaryState(),
            definitions,
            0L,
            out DailyV1ActiveRuntimeSession unadmitted,
            out diagnostic), Is.False);
        Assert.That(unadmitted, Is.Null);
    }

    [Test]
    public void ActiveRuntimeSessionsRetainSelectedProfileCompatibilityAcrossGenesisInstances()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition firstComposition,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition secondComposition,
            out diagnostic), Is.True, diagnostic);

        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            firstComposition.ExecutionBoundaryState,
            definitions,
            0L,
            out DailyV1ActiveRuntimeSession first,
            out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            secondComposition.ExecutionBoundaryState,
            definitions,
            0L,
            out DailyV1ActiveRuntimeSession second,
            out diagnostic), Is.True, diagnostic);

        Assert.That(first.WorldId, Is.Not.SameAs(second.WorldId));
        Assert.That(first.IsProfileCompatibleWith(second), Is.True,
            "Restore compatibility follows the selected effective profile, while each session keeps its own world and owner identities.");
    }

    [Test]
    public void ApplicationOwnsTheExactCoreSessionAndBoundaryAcrossReplacement()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext admission), Is.True);
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            admission,
            out DailyV1GenesisComposition firstComposition,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            admission,
            out DailyV1GenesisComposition replacementComposition,
            out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            firstComposition.ExecutionBoundaryState, definitions, 0L,
            out DailyV1ActiveRuntimeSession first, out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            replacementComposition.ExecutionBoundaryState, definitions, 0L,
            out DailyV1ActiveRuntimeSession replacement, out diagnostic), Is.True, diagnostic);

        Assert.That(application.TryPublishInitial(first), Is.True);
        Assert.That(application.ActiveSession, Is.SameAs(first));
        Assert.That(application.TryBeginOperation(
            first, DailyV1OperationScopeKind.Application,
            out DailyV1ActiveRuntimeSession admitted), Is.True);
        Assert.That(admitted, Is.SameAs(first));
        Assert.That(first.ExecutionBoundaryState.ActiveOperationCount, Is.EqualTo(1));
        Assert.That(application.TryPublishReplacement(first, replacement), Is.False);
        application.EndOperation(DailyV1OperationScopeKind.Application);

        Assert.That(application.TryBeginExclusiveOperation(first, out var exclusive), Is.True);
        using (exclusive)
        {
            Assert.That(application.ActiveSession, Is.SameAs(first));
            Assert.That(application.TryBeginOperation(
                first, DailyV1OperationScopeKind.Application, out admitted), Is.False);
            Assert.That(first.ExecutionBoundaryState.TryBeginOperation(
                DailyV1OperationScopeKind.Application), Is.False);
            Assert.That(application.TryPublishReplacement(exclusive, replacement), Is.True);
            Assert.That(application.ActiveSession, Is.SameAs(replacement));
            Assert.That(application.TryBeginOperation(
                replacement, DailyV1OperationScopeKind.Application, out admitted), Is.False,
                "The replacement cannot be entered before the exclusive restore reservation closes.");
        }

        Assert.That(application.TryBeginOperation(
            replacement, DailyV1OperationScopeKind.Application, out admitted), Is.True);
        Assert.That(admitted, Is.SameAs(replacement));
        application.EndOperation(DailyV1OperationScopeKind.Application);
    }

    [Test]
    public void InitialPublicationIsSingleAndCannotReplaceAnExistingSession()
    {
        DailyV1ApplicationSessionGate<TestSession> gate = CreateGate();
        TestSession first = new TestSession();
        TestSession second = new TestSession();

        Assert.That(gate.TryPublishInitial(first), Is.True);
        Assert.That(gate.Current, Is.SameAs(first));
        Assert.That(gate.TryPublishInitial(second), Is.False);
        Assert.That(gate.Current, Is.SameAs(first));
    }

    [Test]
    public void ReplacementRequiresIdleGateAndExactExpectedSession()
    {
        DailyV1ApplicationSessionGate<TestSession> gate = CreateGate();
        TestSession first = new TestSession();
        TestSession unrelated = new TestSession();
        TestSession replacement = new TestSession();
        Assert.That(gate.TryPublishInitial(first), Is.True);

        Assert.That(gate.TryBeginOperation(first, out TestSession operationSession), Is.True);
        Assert.That(operationSession, Is.SameAs(first));
        Assert.That(gate.ActiveOperationCount, Is.EqualTo(1));
        Assert.That(gate.TryPublishReplacement(first, replacement), Is.False,
            "A restore publication cannot exchange the active session during an operation.");
        Assert.That(gate.Current, Is.SameAs(first));
        gate.EndOperation();

        Assert.That(gate.TryPublishReplacement(unrelated, replacement), Is.False,
            "A stale restore candidate cannot replace a different active session.");
        Assert.That(gate.Current, Is.SameAs(first));
        Assert.That(gate.TryPublishReplacement(first, replacement), Is.True);
        Assert.That(gate.Current, Is.SameAs(replacement));
    }

    [Test]
    public void ExclusiveRestoreReservationRejectsReentrantOperationsAndPublishesOnce()
    {
        DailyV1ApplicationSessionGate<TestSession> gate = CreateGate();
        TestSession current = CreateAdmittedTestSession();
        TestSession replacement = new TestSession();
        TestSession competingReplacement = new TestSession();
        Assert.That(gate.TryPublishInitial(current), Is.True);

        Assert.That(gate.TryBeginExclusiveOperation(current, out var exclusive), Is.True);
        using (exclusive)
        {
            Assert.That(exclusive.AuthorizesSessionReplacementCapture(current.ExecutionState), Is.True,
                "Only the exact active replacement lease can authorize source-boundary reads for P12-G staging.");
            Assert.That(exclusive.AuthorizesSessionReplacementCapture(replacement.ExecutionState), Is.False,
                "A replacement lease cannot authorize a different runtime's source capture.");
            Assert.That(gate.TryBeginOperation(current, out TestSession reentrant), Is.False,
                "A callback on the restore thread must not enter the old active session during detached staging.");
            Assert.That(reentrant, Is.Null);
            Assert.That(current.ExecutionState.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.False,
                "Direct runtime advance must observe the same exclusive replacement boundary.");
            Assert.That(current.ExecutionState.TryBeginRuntimeProtocolOperation(), Is.False,
                "Direct P12 owner census operations must not enter while the source session is being replaced.");
            Assert.That(current.ExecutionState.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.False,
                "The source session cannot become capture-eligible during detached replacement.");
            Assert.That(current.ExecutionState.TryAssessCaptureScope(
                DailyV1CaptureScopeAllowance.SessionReplacementValidation), Is.True,
                "Read-only completed-boundary validation remains possible without enabling capture publication.");
            Assert.That(gate.TryPublishReplacement(current, competingReplacement), Is.False,
                "The ordinary replacement path cannot bypass the active exclusive reservation.");
            Assert.That(gate.Current, Is.SameAs(current));
            Assert.That(gate.TryPublishReplacement(exclusive, replacement), Is.True);
            Assert.That(exclusive.AuthorizesSessionReplacementCapture(current.ExecutionState), Is.False,
                "After publication the old source is no longer the current runtime authorized by this lease.");
            Assert.That(gate.Current, Is.SameAs(replacement));
            Assert.That(gate.TryBeginOperation(replacement, out _), Is.False,
                "The publication window remains closed until restore staging exits.");
        }

        Assert.That(gate.TryBeginOperation(replacement, out TestSession published), Is.True);
        Assert.That(published, Is.SameAs(replacement));
        gate.EndOperation();
        Assert.That(current.ExecutionState.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True,
            "Disposing the reservation releases the old private session boundary after commit.");
    }

    [Test]
    public void DisposingUncommittedExclusiveRestoreReservationReopensCurrentSession()
    {
        DailyV1ApplicationSessionGate<TestSession> gate = CreateGate();
        TestSession current = new TestSession();
        Assert.That(gate.TryPublishInitial(current), Is.True);

        Assert.That(gate.TryBeginExclusiveOperation(current, out var exclusive), Is.True);
        exclusive.Dispose();

        Assert.That(gate.Current, Is.SameAs(current));
        Assert.That(gate.TryBeginOperation(current, out TestSession operation), Is.True);
        Assert.That(operation, Is.SameAs(current));
        gate.EndOperation();
        Assert.That(gate.TryPublishReplacement(current, new TestSession()), Is.True);
    }

    [Test]
    public void ExclusiveRestoreReservationCannotPublishFromAnotherThread()
    {
        DailyV1ApplicationSessionGate<TestSession> gate = CreateGate();
        TestSession current = new TestSession();
        TestSession replacement = new TestSession();
        Assert.That(gate.TryPublishInitial(current), Is.True);
        Assert.That(gate.TryBeginExclusiveOperation(current, out var exclusive), Is.True);

        bool publishedOffThread = true;
        bool authorizedOffThread = true;
        Thread caller = new Thread(() =>
        {
            publishedOffThread = gate.TryPublishReplacement(exclusive, replacement);
            authorizedOffThread = exclusive.AuthorizesSessionReplacementCapture(current.ExecutionState);
        });
        caller.Start();
        caller.Join();

        Assert.That(publishedOffThread, Is.False);
        Assert.That(authorizedOffThread, Is.False,
            "A replacement lease cannot grant source capture authority from another thread.");
        Assert.That(gate.Current, Is.SameAs(current));
        using (exclusive)
            Assert.That(gate.TryPublishReplacement(exclusive, replacement), Is.True);
        Assert.That(gate.Current, Is.SameAs(replacement));
    }

    [Test]
    public void ExpectedSessionOperationRejectsStaleReferenceAndUnderflowIsRejected()
    {
        DailyV1ApplicationSessionGate<TestSession> gate = CreateGate();
        TestSession current = new TestSession();
        Assert.That(gate.TryPublishInitial(current), Is.True);

        Assert.That(gate.TryBeginOperation(new TestSession(), out TestSession stale), Is.False);
        Assert.That(stale, Is.Null);
        Assert.That(gate.ActiveOperationCount, Is.Zero);
        Assert.Throws<InvalidOperationException>(() => gate.EndOperation());
    }

    [Test]
    public void ApplicationAndP12ScopesUseThePublishedRuntimeBoundaryState()
    {
        DailyV1ApplicationSessionGate<TestSession> gate = CreateGate();
        TestSession session = CreateAdmittedTestSession();
        Assert.That(gate.TryPublishInitial(session), Is.True);

        Assert.That(gate.TryBeginOperation(session, out _), Is.True);
        Assert.That(gate.ActiveOperationCount, Is.EqualTo(1));
        Assert.That(session.ExecutionState.TryBeginRuntimeProtocolOperation(), Is.True);
        Assert.That(gate.ActiveOperationCount, Is.EqualTo(2));

        Assert.That(session.ExecutionState.TryEndRuntimeProtocolOperation(), Is.True);
        Assert.That(gate.ActiveOperationCount, Is.EqualTo(1));
        Assert.That(gate.TryPublishReplacement(session, new TestSession()), Is.False,
            "The Core runtime state remains the single active-operation authority for publication.");
        gate.EndOperation();
        Assert.That(gate.ActiveOperationCount, Is.Zero);
        Assert.That(gate.TryPublishReplacement(session, new TestSession()), Is.True);
    }

    [Test]
    public void CaptureAllowanceRequiresExactlyOneMatchingApplicationScope()
    {
        DailyV1ExecutionBoundaryState state = CreateAdmittedTestSession().ExecutionState;
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.ContinuationSave), Is.False);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.DailyAdvance), Is.False);

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.Application), Is.True);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.False);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.ContinuationSave), Is.False);
        Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.Application), Is.True);

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.ContinuationSave), Is.True);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.False);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.DailyAdvance), Is.False);
        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.Application), Is.True);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.ContinuationSave), Is.False,
            "A nested generic scope is not part of the save allowance.");
        Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.Application), Is.True);
        Assert.That(state.TryBeginRuntimeProtocolOperation(), Is.True);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.ContinuationSave), Is.False,
            "A RuntimeProtocol scope remains forbidden during a P12-G owner snapshot.");
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
        Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.DailyAdvance), Is.True);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.ContinuationSave), Is.False);
        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.DailyAdvance), Is.False,
            "Reentrant advances cannot use the token-finalization allowance.");
        Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);

        Assert.That(state.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void TaggedOperationScopeClosesAfterException()
    {
        DailyV1ApplicationSessionGate<TestSession> gate = CreateGate();
        TestSession session = CreateAdmittedTestSession();
        Assert.That(gate.TryPublishInitial(session), Is.True);

        Assert.Throws<InvalidOperationException>(() =>
        {
            Assert.That(gate.TryBeginOperation(
                session,
                DailyV1OperationScopeKind.ContinuationSave,
                out _), Is.True);
            try
            {
                throw new InvalidOperationException("test failure");
            }
            finally
            {
                gate.EndOperation(DailyV1OperationScopeKind.ContinuationSave);
            }
        });

        Assert.That(gate.ActiveOperationCount, Is.Zero);
        Assert.That(session.ExecutionState.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);
        Assert.That(gate.TryPublishReplacement(session, new TestSession()), Is.True);
    }

    [Test]
    public void DailyAdvanceScopeClosesAfterFailureBeforeCaptureCanResume()
    {
        DailyV1ApplicationSessionGate<TestSession> gate = CreateGate();
        TestSession session = CreateAdmittedTestSession();
        Assert.That(gate.TryPublishInitial(session), Is.True);

        Assert.Throws<InvalidOperationException>(() =>
        {
            Assert.That(gate.TryBeginOperation(
                session,
                DailyV1OperationScopeKind.DailyAdvance,
                out _), Is.True);
            try
            {
                throw new InvalidOperationException("daily failure");
            }
            finally
            {
                gate.EndOperation(DailyV1OperationScopeKind.DailyAdvance);
            }
        });

        Assert.That(gate.ActiveOperationCount, Is.Zero);
        Assert.That(session.ExecutionState.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);
        Assert.That(gate.TryPublishReplacement(session, new TestSession()), Is.True);
    }
}
