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
        private IReadOnlyList<OwnerSectionCensusSnapshot> admissionOwnerSections;
        private long admissionMutationEpoch;

        internal ApplicationAdvanceBoundary(
            DailyV1ExecutionBoundaryState state,
            Action onOperationEntry,
            IReadOnlyList<OwnerSectionCensusSnapshot> admissionOwnerSections = null,
            long admissionMutationEpoch = 0L)
        {
            this.state = state;
            this.onOperationEntry = onOperationEntry;
            this.admissionOwnerSections = admissionOwnerSections;
            this.admissionMutationEpoch = admissionMutationEpoch;
        }

        internal bool OperationEntered { get; private set; }
        internal bool SelectedBoundaryRequested { get; private set; }
        internal bool Faulted { get; private set; }
        internal bool? RequestedSessionReplacement { get; private set; }
        internal void SetAdmissionEvidence(
            IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            long mutationEpoch)
        {
            admissionOwnerSections = ownerSections;
            admissionMutationEpoch = mutationEpoch;
        }
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

        public bool TryCaptureDailyBoundaryAdmissionEvidence(
            bool sessionReplacement,
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure)
        {
            RequestedSessionReplacement = sessionReplacement;
            ownerSections = admissionOwnerSections;
            mutationEpoch = admissionMutationEpoch;
            failure = admissionOwnerSections == null
                ? DailyCaptureEligibilityFailure.OperationInProgress
                : DailyCaptureEligibilityFailure.None;
            return admissionOwnerSections != null;
        }

        public bool TryCaptureContinuationSaveEvidence(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure) =>
            TryCaptureDailyBoundaryAdmissionEvidence(
                sessionReplacement: false, out ownerSections, out mutationEpoch, out failure);

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

    private static DailyCaptureEligibilityToken InstallCompletedBoundaryToken(
        DailyV1ActiveRuntimeSession session,
        ApplicationAdvanceBoundary boundary,
        OwnerSectionCensusSnapshot[] sections = null,
        long mutationEpoch = 0L)
    {
        sections ??= DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        Assert.That(state.TryRecordCompletedCore(), Is.True);
        Assert.That(state.TryCreateCompletedAdvanceToken(
            session.AdmissionContext,
            session.EffectiveConfiguration,
            session.Calendar,
            state.CompositionProfile,
            session.WorldId,
            session.SimulationTime.AbsoluteDay,
            mutationEpoch,
            sections,
            out DailyCaptureEligibilityToken token), Is.True);
        boundary.SetAdmissionEvidence(sections, mutationEpoch);
        return token;
    }

    private static void AssertOwnerSectionSnapshotsMatch(
        IReadOnlyList<OwnerSectionCensusSnapshot> expected,
        IReadOnlyList<OwnerSectionCensusSnapshot> actual)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Count));
        for (int index = 0; index < expected.Count; index++)
        {
            Assert.That(actual[index].SectionId, Is.EqualTo(expected[index].SectionId));
            Assert.That(actual[index].SchemaVersion, Is.EqualTo(expected[index].SchemaVersion));
            Assert.That(actual[index].Role, Is.EqualTo(expected[index].Role));
            Assert.That(actual[index].OwnerInstanceIdentity,
                Is.SameAs(expected[index].OwnerInstanceIdentity));
            Assert.That(actual[index].Cardinality, Is.EqualTo(expected[index].Cardinality));
            Assert.That(actual[index].Revision, Is.EqualTo(expected[index].Revision));
        }
    }

    [Test]
    public void ContinuationSaveOperationRetainsExactCompletedSourceWitnessAndClosesScope()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        DailyCaptureEligibilityToken sourceToken =
            InstallCompletedBoundaryToken(session, boundary);

        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation,
            out DailyCaptureEligibilityFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.None));
        Assert.That(operation, Is.Not.Null);
        Assert.That(operation.SourceWitness.Token, Is.SameAs(sourceToken));
        Assert.That(operation.SourceWitness.TokenOwnerSections,
            Is.SameAs(sourceToken.OwnerSections));
        Assert.That(operation.SourceWitness.TokenOwnerSectionsSnapshot,
            Is.Not.SameAs(sourceToken.OwnerSections));
        AssertOwnerSectionSnapshotsMatch(sourceToken.OwnerSections,
            operation.SourceWitness.TokenOwnerSectionsSnapshot);
        Assert.That(operation.SourceWitness.Manifest, Is.Not.Null);
        Assert.That(application.ActiveOperationCount, Is.EqualTo(1));
        Assert.That(operation.TryRevalidate(out failure), Is.True);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.None));

        operation.Dispose();
        operation.Dispose();
        Assert.That(operation.IsActive, Is.False);
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void ContinuationSaveRevalidationRejectsChangedOwnerCensusAndBalancesScope()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        OwnerSectionCensusSnapshot[] sections =
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        InstallCompletedBoundaryToken(session, boundary, sections);
        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation, out _), Is.True);

        OwnerSectionCensusSnapshot original = sections[0];
        sections[0] = new OwnerSectionCensusSnapshot(
            original.SectionId, original.SchemaVersion, original.Role,
            original.OwnerInstanceIdentity, original.Cardinality, original.Revision + 1L);
        boundary.SetAdmissionEvidence(sections, 0L);

        Assert.That(operation.TryRevalidate(out DailyCaptureEligibilityFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));
        operation.Dispose();
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void ContinuationSaveDoesNotRegisterWhenCompletedBoundaryIsUnavailable()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out _);

        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation, out _), Is.False);
        Assert.That(operation, Is.Null);
        Assert.That(application.ActiveOperationCount, Is.Zero);
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
    public void CoreRestoreCommitRechecksCandidateCensusBeforeInitialPublication()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext bootstrapAdmission), Is.True);
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions, bootstrapAdmission, out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        SimulationRuntimeAdmissionContext restoredAdmission =
            bootstrapAdmission.CreateRestoredContinuationContext();
        DailyV1ExecutionBoundaryState restoredState =
            CreateBoundaryWithAdmission(composition, restoredAdmission);
        OwnerSectionCensusSnapshot[] ownerSections =
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        Assert.That(restoredState.TryCreateRestoredContinuationToken(
            restoredAdmission,
            restoredState.ConfigurationIdentity,
            restoredState.CalendarIdentity,
            restoredState.CompositionProfile,
            restoredState.WorldId,
            restoredState.SimulationTime.AbsoluteDay,
            1L,
            23L,
            ownerSections,
            out _), Is.True);
        Assert.That(application.TryCreateRestoredActiveRuntimeSession(
            restoredState, definitions, 0L, null,
            out DailyV1ActiveRuntimeSession candidate, out diagnostic), Is.True, diagnostic);

        ApplicationAdvanceBoundary boundary = new ApplicationAdvanceBoundary(
            restoredState, null, ownerSections, 23L);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(candidate, boundary), Is.True);

        List<OwnerSectionCensusSnapshot> changedOwners = new List<OwnerSectionCensusSnapshot>(ownerSections);
        OwnerSectionCensusSnapshot original = changedOwners[0];
        changedOwners[0] = new OwnerSectionCensusSnapshot(
            original.SectionId, original.SchemaVersion, original.Role, new object(),
            original.Cardinality, original.Revision);
        // The adapter remains identity-bound; mutate only its fresh read-only
        // census to model an owner change between staging and commit.
        boundary.SetAdmissionEvidence(changedOwners, 23L);
        Assert.That(application.TryCommitRestoredCandidate(
            null, candidate, null, out diagnostic), Is.False);
        Assert.That(application.ActiveSession, Is.Null,
            "A stale private target must not become the initial application session.");
        Assert.That(diagnostic, Does.Contain("healthy, idle, exact Core continuation"));

        boundary.SetAdmissionEvidence(ownerSections, 23L);
        Assert.That(application.TryCommitRestoredCandidate(
            null, candidate, null, out diagnostic), Is.True, diagnostic);
        Assert.That(application.ActiveSession, Is.SameAs(candidate));
        Assert.That(boundary.RequestedSessionReplacement, Is.EqualTo(false));
    }

    [Test]
    public void CoreRestoreReplacementAcceptsRestoredSourceTokenAndRejectsStaleSourceCensus()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext bootstrapAdmission), Is.True);
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions, bootstrapAdmission, out DailyV1GenesisComposition sourceComposition,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions, bootstrapAdmission, out DailyV1GenesisComposition candidateComposition,
            out diagnostic), Is.True, diagnostic);

        SimulationRuntimeAdmissionContext sourceAdmission =
            bootstrapAdmission.CreateRestoredContinuationContext();
        DailyV1ExecutionBoundaryState sourceState =
            CreateBoundaryWithAdmission(sourceComposition, sourceAdmission);
        OwnerSectionCensusSnapshot[] sourceOwners =
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        const long sourceEpoch = 23L;
        Assert.That(sourceState.TryCreateRestoredContinuationToken(
            sourceAdmission,
            sourceState.ConfigurationIdentity,
            sourceState.CalendarIdentity,
            sourceState.CompositionProfile,
            sourceState.WorldId,
            sourceState.SimulationTime.AbsoluteDay,
            1L,
            sourceEpoch,
            sourceOwners,
            out DailyCaptureEligibilityToken sourceToken), Is.True);
        Assert.That(sourceToken.BoundaryProvenance,
            Is.EqualTo(DailyCaptureBoundaryProvenance.RestoredContinuation));
        Assert.That(application.TryCreateRestoredActiveRuntimeSession(
            sourceState, definitions, 0L, null,
            out DailyV1ActiveRuntimeSession sourceSession, out diagnostic), Is.True, diagnostic);

        ApplicationAdvanceBoundary sourceBoundary = new ApplicationAdvanceBoundary(
            sourceState, null, sourceOwners, sourceEpoch);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(sourceSession, sourceBoundary), Is.True);
        Assert.That(application.TryCommitRestoredCandidate(
            null, sourceSession, null, out diagnostic), Is.True, diagnostic);
        Assert.That(application.ActiveSession, Is.SameAs(sourceSession));

        SimulationRuntimeAdmissionContext candidateAdmission =
            bootstrapAdmission.CreateRestoredContinuationContext();
        DailyV1ExecutionBoundaryState candidateState =
            CreateBoundaryWithAdmission(candidateComposition, candidateAdmission);
        OwnerSectionCensusSnapshot[] candidateOwners =
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        const long candidateEpoch = 31L;
        Assert.That(candidateState.TryCreateRestoredContinuationToken(
            candidateAdmission,
            candidateState.ConfigurationIdentity,
            candidateState.CalendarIdentity,
            candidateState.CompositionProfile,
            candidateState.WorldId,
            candidateState.SimulationTime.AbsoluteDay,
            2L,
            candidateEpoch,
            candidateOwners,
            out _), Is.True);

        Assert.That(application.TryBeginExclusiveOperation(sourceSession, out var replacementOperation), Is.True);
        using (replacementOperation)
        {
            Assert.That(application.TryCreateRestoredActiveRuntimeSession(
                candidateState, definitions, 0L, replacementOperation,
                out DailyV1ActiveRuntimeSession candidate, out diagnostic), Is.True, diagnostic);
            ApplicationAdvanceBoundary candidateBoundary = new ApplicationAdvanceBoundary(
                candidateState, null, candidateOwners, candidateEpoch);
            Assert.That(application.TryBindRuntimeAdvanceBoundary(candidate, candidateBoundary), Is.True);

            List<OwnerSectionCensusSnapshot> staleSourceOwners = new List<OwnerSectionCensusSnapshot>(sourceOwners);
            OwnerSectionCensusSnapshot first = staleSourceOwners[0];
            staleSourceOwners[0] = new OwnerSectionCensusSnapshot(
                first.SectionId, first.SchemaVersion, first.Role, new object(),
                first.Cardinality, first.Revision);
            sourceBoundary.SetAdmissionEvidence(staleSourceOwners, sourceEpoch);
            Assert.That(application.TryCommitRestoredCandidate(
                sourceSession, candidate, replacementOperation, out diagnostic), Is.False);
            Assert.That(application.ActiveSession, Is.SameAs(sourceSession),
                "A replacement must retain the current restored session when its fresh source census is stale.");

            sourceBoundary.SetAdmissionEvidence(sourceOwners, sourceEpoch);
            Assert.That(application.TryCommitRestoredCandidate(
                sourceSession, candidate, replacementOperation, out diagnostic), Is.True, diagnostic);
            Assert.That(application.ActiveSession, Is.SameAs(candidate));
            Assert.That(sourceBoundary.RequestedSessionReplacement, Is.EqualTo(true));
        }
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
