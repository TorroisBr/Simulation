using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class DailyV1ApplicationSessionGateTests
{
    private sealed class TestHostProjection : IDailyV1ApplicationHostProjection
    {
        public DailyV1ActiveRuntimeSession CoreRuntimeSession => null;

        public bool TryCreateSelectedTravelPartyStartHostOperation(
            DailyV1SelectedTravelPartyStartPlan plan,
            out IDailyV1SelectedTravelPartyStartHostOperation operation)
        {
            operation = null;
            return false;
        }
    }

    private sealed class CensusIdentityApplicationAdvanceBoundary
        : ApplicationAdvanceBoundary
    {
        internal CensusIdentityApplicationAdvanceBoundary(
            DailyV1ExecutionBoundaryState state,
            object censusSourceIdentity)
            : base(state, null)
        {
            SelectedRuntimeOwnerCensusSourceIdentity = censusSourceIdentity;
        }

        public override object SelectedRuntimeOwnerCensusSourceIdentity { get; }
    }

    private static P12ACompatibilityDescriptor UnityIdentity() =>
        new P12ACompatibilityDescriptor(
            "UnityBootstrap-Daily-v1", 1, "config", "6000.3.9f1", "WindowsEditor", "Mono",
            "build", new[] { "00000000-0000-0000-0000-000000000001" }, "numeric", 1,
            "content", "providers", "daily", 1, "calendar", "p9", "p8");

    private sealed class MissingContinuationSlotOperations : IAtomicFileSlotOperations
    {
        internal int OpenReadCount { get; private set; }

        public void CreateDirectory(string path) => throw new NotSupportedException();
        public bool Exists(string path) => false;
        public System.IO.Stream CreateNewTemporary(string path) => throw new NotSupportedException();
        public System.IO.Stream OpenRead(string path)
        {
            OpenReadCount++;
            throw new System.IO.FileNotFoundException();
        }
        public void FlushToDisk(System.IO.Stream stream) => throw new NotSupportedException();
        public void Replace(string source, string destination) => throw new NotSupportedException();
        public void Move(string source, string destination) => throw new NotSupportedException();
        public void Delete(string path) => throw new NotSupportedException();
    }

    private sealed class TestSession
    {
        internal readonly DailyV1ExecutionBoundaryState ExecutionState;

        internal TestSession(DailyV1ExecutionBoundaryState executionState = null)
        {
            ExecutionState = executionState ?? new DailyV1ExecutionBoundaryState();
        }
    }

    private sealed class SelectedDailyExecutionHostPorts : IDailyV1SelectedDailyExecutionHostPorts
    {
        public void BeginLoggerDay(long absoluteDay) { }
        public bool HasExactCrimeJusticeProjectionOwnerBindings(DailyV1SelectedRuntimeOwnerGraph graph) => true;
        public bool TryCreateSelectedDemographyHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDemographyHostPorts hostPorts)
        {
            hostPorts = null;
            return false;
        }
        public void PublishSelectedDemographyResult(DailyV1SelectedDemographyResult result) { }
        public void BeginSimulationDay() { }
        public void EnsureSelectedEconomyProjectionCanRun(DailyV1SelectedRuntimeOwnerGraph graph) { }
        public void LogSelectedDailyEconomy(
            DailyV1CityRuntimeOwners owner, SimulationLogCategory category, string message) { }
        public void EnsureSelectedLocalKnowledgeProjectionCanRun(
            DailyV1SelectedRuntimeOwnerGraph graph, bool merchantTradeEnabled) { }
        public bool TryCreateSelectedActorTurnHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedActorTurnHostPorts hostPorts)
        {
            hostPorts = null;
            return false;
        }
        public bool TryCreateSelectedTravelDayHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedTravelDayHostPorts hostPorts)
        {
            hostPorts = null;
            return false;
        }
    }
    private class ApplicationAdvanceBoundary : IDailyV1RuntimeAdvanceBoundary,
        IDailyV1SelectedRuntimeAdvanceHostPorts
    {
        private readonly DailyV1ExecutionBoundaryState state;
        private readonly Action onOperationEntry;
        private IReadOnlyList<OwnerSectionCensusSnapshot> admissionOwnerSections;
        private object justiceProjectionIdentity;
        private long justiceP18ReceiptRevision;
        private long admissionMutationEpoch;
        private Exception continuationSaveCaptureException;
        private Action<int> onContinuationSaveEvidenceCapture;
        private Action<DailyV1CaptureScopeAllowance> onOwnerSectionCensusCapture;

        internal ApplicationAdvanceBoundary(
            DailyV1ExecutionBoundaryState state,
            Action onOperationEntry,
            IReadOnlyList<OwnerSectionCensusSnapshot> admissionOwnerSections = null,
            long admissionMutationEpoch = 0L)
        {
            this.state = state;
            this.onOperationEntry = onOperationEntry;
            this.admissionOwnerSections = SortHostOwnerSections(admissionOwnerSections);
            CaptureJusticeProjectionIdentity(this.admissionOwnerSections);
            this.admissionMutationEpoch = admissionMutationEpoch;
        }

        internal bool OperationEntered { get; private set; }
        public virtual object SelectedRuntimeOwnerCensusSourceIdentity => this;
        internal int ActiveOperationCountAtStageCreation { get; private set; }
        internal bool SelectedBoundaryRequested { get; private set; }
        internal bool ReturnSelectedExecutionHostPorts { get; set; }
        internal int SelectedExecutionHostPortCreationCount { get; private set; }
        internal int LegacyAdvanceInvocationCount { get; private set; }
        internal bool Faulted { get; private set; }
        internal string RequestedRuntimeProtocolOperationId { get; private set; }
        internal bool? RequestedSessionReplacement { get; private set; }
        internal DailyV1CaptureScopeAllowance? RequestedCaptureScopeAllowance { get; private set; }
        internal int OwnerCensusCaptureCount { get; private set; }
        internal IReadOnlyList<OwnerSectionCensusSnapshot> LastCapturedOwnerSections { get; private set; }
        internal bool ReturnPartialOwnerEvidenceThenFail { get; set; }
        internal int ContinuationSaveEvidenceCaptureCount { get; private set; }
        internal void ThrowOnContinuationSaveCapture(Exception exception) =>
            continuationSaveCaptureException = exception;
        internal void SetContinuationSaveEvidenceCaptureCallback(Action<int> callback) =>
            onContinuationSaveEvidenceCapture = callback;
        internal void SetOwnerSectionCensusCaptureCallback(
            Action<DailyV1CaptureScopeAllowance> callback) =>
            onOwnerSectionCensusCapture = callback;
        internal void SetAdmissionEvidence(
            IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            long mutationEpoch)
        {
            admissionOwnerSections = SortHostOwnerSections(ownerSections);
            CaptureJusticeProjectionIdentity(admissionOwnerSections);
            admissionMutationEpoch = mutationEpoch;
        }

        private static IReadOnlyList<OwnerSectionCensusSnapshot> SortHostOwnerSections(
            IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections)
        {
            if (ownerSections is OwnerSectionCensusSnapshot[] array)
                Array.Sort(array, (left, right) =>
                    StringComparer.Ordinal.Compare(left?.SectionId, right?.SectionId));
            return ownerSections;
        }
        private void CaptureJusticeProjectionIdentity(
            IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections)
        {
            if (justiceProjectionIdentity != null || ownerSections == null) return;
            OwnerSectionCensusSnapshot records = ownerSections.FirstOrDefault(section =>
                string.Equals(section?.SectionId,
                    DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId, StringComparison.Ordinal));
            OwnerSectionCensusSnapshot receipts = ownerSections.FirstOrDefault(section =>
                string.Equals(section?.SectionId,
                    DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId, StringComparison.Ordinal));
            if (records == null || receipts == null
                || !ReferenceEquals(records.OwnerInstanceIdentity, receipts.OwnerInstanceIdentity)) return;
            justiceProjectionIdentity = records.OwnerInstanceIdentity;
            justiceP18ReceiptRevision = receipts.Revision;
        }

        public DailyV1ExecutionBoundaryState ExecutionState => state;
        public bool TryGetExactJusticeRecordsCensusProjectionIdentity(
            DailyV1JusticeRecordsOwner owner,
            out object projectionIdentity,
            out long p18ReceiptRevision)
        {
            projectionIdentity = null;
            p18ReceiptRevision = 0L;
            if (owner == null || justiceProjectionIdentity == null
                || !ReferenceEquals(state.SelectedRuntimeOwnerGraph?.JusticeRecordsOwner, owner))
                return false;
            projectionIdentity = justiceProjectionIdentity;
            p18ReceiptRevision = justiceP18ReceiptRevision;
            return true;
        }

        public DailyCaptureEligibilityFailure CensusFailure { get; set; }
        public bool HasIntradayTimeline => false;

        public bool TryEnterDailyAdvanceOperation(out IDisposable operationScope)
            => TryEnterRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance,
                out operationScope);

        private bool TryEnterRuntimeProtocolOperation(
            string operationContractId,
            out IDisposable operationScope)
        {
            RequestedRuntimeProtocolOperationId = operationContractId;
            OperationEntered = true;
            onOperationEntry?.Invoke();
            if (!state.TryBeginRuntimeProtocolOperation(operationContractId))
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
            ActiveOperationCountAtStageCreation = state.ActiveOperationCount;
            OperationEntered = ActiveOperationCountAtStageCreation == 2;
            RequestedRuntimeProtocolOperationId =
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance;
            onOperationEntry?.Invoke();
            SelectedBoundaryRequested = true;
            executionBoundary = null;
            return false;
        }

        public bool TryCreateSelectedDailyExecutionHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDailyExecutionHostPorts hostPorts)
        {
            ActiveOperationCountAtStageCreation = state.ActiveOperationCount;
            OperationEntered = ActiveOperationCountAtStageCreation == 2;
            RequestedRuntimeProtocolOperationId =
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance;
            onOperationEntry?.Invoke();
            SelectedBoundaryRequested = true;
            SelectedExecutionHostPortCreationCount++;
            hostPorts = ReturnSelectedExecutionHostPorts
                ? new SelectedDailyExecutionHostPorts()
                : null;
            return hostPorts != null;
        }
        public bool TryAdvanceDayCore(out SimulationRuntimeAdvanceFailure failure)
        {
            LegacyAdvanceInvocationCount++;
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        public bool TryAdvanceIntradayToNextDayBoundary(out SimulationRuntimeAdvanceFailure failure)
        {
            LegacyAdvanceInvocationCount++;
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
            out DailyCaptureEligibilityFailure failure)
        {
            if (continuationSaveCaptureException != null)
                throw continuationSaveCaptureException;
            ContinuationSaveEvidenceCaptureCount++;
            onContinuationSaveEvidenceCapture?.Invoke(ContinuationSaveEvidenceCaptureCount);
            return TryCaptureDailyBoundaryAdmissionEvidence(
                sessionReplacement: false, out ownerSections, out mutationEpoch, out failure);
        }

        public bool TryCaptureOwnerSectionCensus(
            DailyV1CaptureScopeAllowance allowedOuterScope,
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure)
        {
            RequestedCaptureScopeAllowance = allowedOuterScope;
            RequestedSessionReplacement =
                allowedOuterScope == DailyV1CaptureScopeAllowance.SessionReplacementValidation;
            OwnerCensusCaptureCount++;
            if (allowedOuterScope == DailyV1CaptureScopeAllowance.ContinuationSave)
            {
                if (continuationSaveCaptureException != null)
                    throw continuationSaveCaptureException;
                ContinuationSaveEvidenceCaptureCount++;
                onContinuationSaveEvidenceCapture?.Invoke(ContinuationSaveEvidenceCaptureCount);
            }

            ownerSections = admissionOwnerSections;
            mutationEpoch = admissionMutationEpoch;
            onOwnerSectionCensusCapture?.Invoke(allowedOuterScope);
            if (ReturnPartialOwnerEvidenceThenFail)
            {
                failure = DailyCaptureEligibilityFailure.RuntimeFaulted;
                return false;
            }
            failure = CensusFailure != DailyCaptureEligibilityFailure.None
                ? CensusFailure
                : admissionOwnerSections == null
                    ? DailyCaptureEligibilityFailure.OperationInProgress
                    : DailyCaptureEligibilityFailure.None;
            bool captured = admissionOwnerSections != null
                && CensusFailure == DailyCaptureEligibilityFailure.None
                && !ReturnPartialOwnerEvidenceThenFail;
            if (!captured) return false;
            LastCapturedOwnerSections = ownerSections;
            return true;
        }

        public bool TryBindRestoredCandidateMutationGuard(out string diagnostic)
        {
            diagnostic = "The session-gate test boundary does not compose restored candidates.";
            return false;
        }

        public bool TryCaptureUnadmittedRestoredDailyOwnerVector(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out string diagnostic)
        {
            ownerSections = null;
            mutationEpoch = 0L;
            diagnostic = "The session-gate test boundary does not compose restored candidates.";
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
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState(
            new SimulationTime(), admission, SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()));
        Assert.That(state.TryBindRuntimeConfiguration(
            SimulationConfigurationResolver.ResolveOrThrow(),
            new SimulationCalendar(new CalendarDefinition(1, 1, 1))), Is.True);
        EnsureRuntimeProtocolInventory(state);
        return new TestSession(state);
    }

    private static void EnsureRuntimeProtocolInventory(DailyV1ExecutionBoundaryState state)
    {
        if (state.HasRuntimeProtocolOperationInventory) return;
        Assert.That(state.HasRegisteredRuntimeProtocolOperationContracts, Is.False,
            "The test state cannot already own a partial operation inventory.");
        IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true,
                hasTravelPartySystem: false,
                hasTravelSystem: false);
        foreach (string operationContractId in operationContractIds)
            Assert.That(state.TryRegisterRuntimeProtocolOperationContract(operationContractId), Is.True);
        Assert.That(state.TrySealRuntimeProtocolOperationInventory(), Is.True);
    }

    private static DailyV1Application CreateApplicationWithAdvanceBoundary(
        Action onOperationEntry,
        out DailyV1ActiveRuntimeSession session,
        out ApplicationAdvanceBoundary boundary,
        DailyV1EffectiveDefinitions definitions = null,
        bool publishWorld = true,
        bool markCensusProtocolReady = true,
        bool bindAdvanceBoundary = true)
    {
        DailyV1Application application = new DailyV1Application();
        definitions ??= DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out session, out string diagnostic), Is.True, diagnostic);
        if (publishWorld)
            Assert.That(session.ExecutionBoundaryState.TryMarkFactualReadWorldPublished(), Is.True);
        EnsureRuntimeProtocolInventory(session.ExecutionBoundaryState);
        if (markCensusProtocolReady)
            Assert.That(session.ExecutionBoundaryState.TryMarkSelectedRuntimeCensusProtocolReady(), Is.True,
                "The Core boundary fixture models a successfully bound census protocol.");
        boundary = new ApplicationAdvanceBoundary(session.ExecutionBoundaryState, onOperationEntry);
        if (bindAdvanceBoundary)
        {
            Assert.That(application.TryBindRuntimeAdvanceBoundary(session, boundary), Is.True);
            Assert.That(application.TryPublishInitial(session), Is.True);
        }
        return application;
    }

    private static DailyCaptureEligibilityToken InstallCompletedBoundaryToken(
        DailyV1ActiveRuntimeSession session,
        ApplicationAdvanceBoundary boundary,
        OwnerSectionCensusSnapshot[] sections = null,
        long mutationEpoch = 0L)
    {
        sections ??= CreateCoreBoundCompleteOwnerSectionVector(session);
        Array.Sort(sections, (left, right) =>
            StringComparer.Ordinal.Compare(left?.SectionId, right?.SectionId));
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

    private static void ReplaceOwnerSectionWithCurrentCensus(
        OwnerSectionCensusSnapshot[] sections,
        OwnerSectionCensusWitness census)
    {
        int index = Array.FindIndex(sections,
            section => string.Equals(section.SectionId, census.SectionId, StringComparison.Ordinal));
        Assert.That(index, Is.GreaterThanOrEqualTo(0), census.SectionId);
        OwnerSectionCensusSnapshot previous = sections[index];
        sections[index] = new OwnerSectionCensusSnapshot(
            previous.SectionId, previous.SchemaVersion, previous.Role,
            census.OwnerInstanceIdentity, census.Cardinality, census.Revision);
    }

    private static void ReplaceSelectedSpatialOwnerCensus(
        OwnerSectionCensusSnapshot[] sections,
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        Assert.That(sections, Is.Not.Null);
        Assert.That(graph?.SpatialNetwork, Is.Not.Null);
        if (graph.RuntimeIdentityIndex != null)
        {
            DailyV1RuntimeIdentityKind[] identityKinds =
            {
                DailyV1RuntimeIdentityKind.Npc,
                DailyV1RuntimeIdentityKind.City,
                DailyV1RuntimeIdentityKind.Location,
                DailyV1RuntimeIdentityKind.Route,
                DailyV1RuntimeIdentityKind.ExplorableSite,
                DailyV1RuntimeIdentityKind.LocalPlace,
                DailyV1RuntimeIdentityKind.LocalConnection,
                DailyV1RuntimeIdentityKind.NotableItem
            };
            foreach (DailyV1RuntimeIdentityKind identityKind in identityKinds)
                ReplaceOwnerSectionWithCurrentCensus(sections,
                    graph.RuntimeIdentityIndex.GetOwnerSectionCensusWitness(identityKind));
        }

        ReplaceOwnerSectionWithCurrentCensus(sections, new OwnerSectionCensusWitness(
            P12ASpatialNetworkOwnerCodec.LocationsSectionId,
            P12ASpatialNetworkOwnerCodec.SchemaVersion,
            graph.SpatialNetwork,
            graph.SpatialNetwork.LocationCount,
            graph.SpatialNetwork.Revision));
        ReplaceOwnerSectionWithCurrentCensus(sections, new OwnerSectionCensusWitness(
            P12ASpatialNetworkOwnerCodec.RoutesSectionId,
            P12ASpatialNetworkOwnerCodec.SchemaVersion,
            graph.SpatialNetwork,
            graph.SpatialNetwork.RouteCount,
            graph.SpatialNetwork.Revision));

        SpatialAuthorityStore spatial = graph.PersonSpatialPositionStore?.SpatialAuthorityStore;
        if (spatial != null)
        {
            ReplaceOwnerSectionWithCurrentCensus(sections, new OwnerSectionCensusWitness(
                P12P8AImageSectionContract.HexSectionId,
                P12P8AImageSectionContract.SchemaVersion,
                spatial, spatial.HexCount, spatial.Revision));
            ReplaceOwnerSectionWithCurrentCensus(sections, new OwnerSectionCensusWitness(
                P12P8AImageSectionContract.LocationSectionId,
                P12P8AImageSectionContract.SchemaVersion,
                spatial, spatial.LocationCount, spatial.Revision));
            ReplaceOwnerSectionWithCurrentCensus(sections, new OwnerSectionCensusWitness(
                P12P8AImageSectionContract.ScaleContextSectionId,
                P12P8AImageSectionContract.SchemaVersion,
                spatial, spatial.HasGeography ? 1 : 0, spatial.Revision));
            ReplaceOwnerSectionWithCurrentCensus(sections, new OwnerSectionCensusWitness(
                DailyV1SelectedOwnerSectionCensusContract.SpatialPassageStateSectionId,
                DailyV1SelectedOwnerSectionCensusContract.SpatialPassageStateSchemaVersion,
                spatial.PassageAuthority,
                spatial.PassageAuthority.Options.Count + spatial.PassageAuthority.Barriers.Count,
                spatial.Revision));
            ReplaceOwnerSectionWithCurrentCensus(sections, new OwnerSectionCensusWitness(
                DailyV1SelectedOwnerSectionCensusContract.SpatialCrossingSectionId,
                DailyV1SelectedOwnerSectionCensusContract.SpatialCrossingSchemaVersion,
                spatial, spatial.CrossingCount, spatial.Revision));
        }
        if (graph.LegacySpatialAnchors != null)
            ReplaceOwnerSectionWithCurrentCensus(sections,
                new LegacySpatialAnchorBindingCensusProvider(graph.LegacySpatialAnchors).GetCurrentCensus());
        if (graph.PersonSpatialPositionStore != null)
            ReplaceOwnerSectionWithCurrentCensus(sections,
                new PersonSpatialPositionCensusProvider(graph.PersonSpatialPositionStore).GetCurrentCensus());
    }

    private static OwnerSectionCensusSnapshot[] CreateCoreBoundP12FSectionVector(
        DailyV1ActiveRuntimeSession session)
        => CreateCoreBoundCityAndP12FSectionVector(session);

    private static OwnerSectionCensusSnapshot[] CreateCoreBoundP12FSectionVectorWithoutCity(
        DailyV1ActiveRuntimeSession session)
    {
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        PoliticalKnowledgeStore politicalKnowledge =
            graph.PoliticalKnowledgeStore ?? CreateAndBindCorePoliticalKnowledgeStore(graph);
        ActorChoiceStore actorChoices = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        OwnerSectionCensusSnapshot[] sections =
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        Assert.That(graph.PersonStore, Is.Not.Null);
        Assert.That(graph.GenealogyStore, Is.Not.Null);
        ReplaceSelectedSpatialOwnerCensus(sections, graph);
        ReplaceOwnerSectionWithCurrentCensus(sections,
            graph.PersonStore.GetMembershipCensusWitness());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new SimulationRecordSequenceCensusProvider(graph.RecordSequence).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            graph.PersonStore.GetMaterializationBindingCensusWitness());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new GenealogyCensusProvider(graph.GenealogyStore).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new PoliticalKnowledgeStoreCensusProvider(politicalKnowledge).GetCurrentCensus());
        Assert.That(graph.PoliticalDecisionStore, Is.Not.Null);
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new PoliticalDecisionStoreCensusProvider(graph.PoliticalDecisionStore).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new ActorChoiceP11CensusProvider(actorChoices).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new ScheduledDirectiveCensusProvider(graph.ScheduledDirectives).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new TravelPartyCensusProvider(graph.TravelParties).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new ExpeditionCensusProvider(graph.Expeditions).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(graph.RuntimeIdAllocator)
                .GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(graph.RuntimeIdAllocator)
                .GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            RuntimeIdAllocatorCensusProvider.CreateTravelPartyCounterProvider(graph.RuntimeIdAllocator)
                .GetCurrentCensus());
        sections = ReplaceSelectedNpcOwnerSectionVector(graph, sections);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcSections(
            graph, sections, out _, out string diagnostic), Is.True, diagnostic);
        if (graph.P12EOwners != null)
        {
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedP12EOwnerSections(
                graph, Array.Empty<OwnerSectionCensusSnapshot>(),
                out IReadOnlyList<OwnerSectionCensusSnapshot> exactP12EOwnerRows,
                out diagnostic), Is.True, diagnostic);
            HashSet<string> coreP12EOwnerSectionIds = new HashSet<string>(
                exactP12EOwnerRows.Select(section => section.SectionId), StringComparer.Ordinal);
            sections = sections.Where(section => !coreP12EOwnerSectionIds.Contains(section.SectionId))
                .Concat(exactP12EOwnerRows).ToArray();
        }
        return RebindCoreJusticeOwnerSectionVector(graph, sections);
    }

    private static PoliticalKnowledgeStore CreateAndBindCorePoliticalKnowledgeStore(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        Assert.That(graph, Is.Not.Null);
        Assert.That(graph.PersonStore, Is.Not.Null);
        InstitutionStore institutions = new InstitutionStore();
        PoliticalKnowledgeStore store = new PoliticalKnowledgeStore(
            graph.PersonStore,
            institutions,
            new PoliticalClaimStore(),
            new FactionStore(graph.PersonStore),
            new OfficeStore(institutions),
            new PropertyOwnershipStore(graph.PersonStore));
        Assert.That(graph.TryBindPoliticalKnowledgeStore(store), Is.True);
        return store;
    }

    private static OwnerSectionCensusSnapshot[] CreateCoreBoundCityAndP12FSectionVector(
        DailyV1ActiveRuntimeSession session)
    {
        OwnerSectionCensusSnapshot[] existing = CreateCoreBoundP12FSectionVectorWithoutCity(session);
        return RebindCoreJusticeOwnerSectionVector(session.OwnerGraph, ReplaceCityOwnerSectionVector(session, existing));
    }

    private static OwnerSectionCensusSnapshot[] RebindCoreJusticeOwnerSectionVector(
        DailyV1SelectedRuntimeOwnerGraph graph, OwnerSectionCensusSnapshot[] sections)
    {
        DailyV1JusticeRecordsOwner justiceOwner = graph?.JusticeRecordsOwner;
        Assert.That(justiceOwner, Is.Not.Null);
        int recordCardinality = checked(justiceOwner.WantedRecords.Count + justiceOwner.Sentences.Count);
        int recordsIndex = Array.FindIndex(sections, section => string.Equals(section.SectionId,
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId, StringComparison.Ordinal));
        int receiptsIndex = Array.FindIndex(sections, section => string.Equals(section.SectionId,
            DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId, StringComparison.Ordinal));
        Assert.That(recordsIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(receiptsIndex, Is.GreaterThanOrEqualTo(0));
        object justiceProjectionIdentity = sections[recordsIndex].OwnerInstanceIdentity;
        sections[recordsIndex] = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, justiceProjectionIdentity, recordCardinality, justiceOwner.Revision);
        sections[receiptsIndex] = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, justiceProjectionIdentity, 1, sections[receiptsIndex].Revision);
        return sections;
    }

    private static OwnerSectionCensusSnapshot[] CreateCoreBoundCompleteOwnerSectionVector(
        DailyV1ActiveRuntimeSession session)
        => CreateCoreBoundCityAndP12FSectionVector(session);
    private static OwnerSectionCensusSnapshot[] CreateCoreBoundCompleteOwnerSectionVector(
        DailyV1SelectedRuntimeOwnerGraph graph,
        ActorChoiceStore actorChoices)
    {
        OwnerSectionCensusSnapshot[] sections =
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        Assert.That(graph.PersonStore, Is.Not.Null);
        Assert.That(graph.GenealogyStore, Is.Not.Null);
        Assert.That(actorChoices, Is.Not.Null);
        Assert.That(actorChoices.PersonStore, Is.SameAs(graph.PersonStore));
        PoliticalKnowledgeStore politicalKnowledge =
            graph.PoliticalKnowledgeStore ?? CreateAndBindCorePoliticalKnowledgeStore(graph);
        ReplaceSelectedSpatialOwnerCensus(sections, graph);
        ReplaceOwnerSectionWithCurrentCensus(sections,
            graph.PersonStore.GetMembershipCensusWitness());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new SimulationRecordSequenceCensusProvider(graph.RecordSequence).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            graph.PersonStore.GetMaterializationBindingCensusWitness());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new GenealogyCensusProvider(graph.GenealogyStore).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new PoliticalKnowledgeStoreCensusProvider(politicalKnowledge).GetCurrentCensus());
        Assert.That(graph.PoliticalDecisionStore, Is.Not.Null);
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new PoliticalDecisionStoreCensusProvider(graph.PoliticalDecisionStore).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new ActorChoiceP11CensusProvider(actorChoices).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new ScheduledDirectiveCensusProvider(graph.ScheduledDirectives).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new TravelPartyCensusProvider(graph.TravelParties).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new ExpeditionCensusProvider(graph.Expeditions).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(graph.RuntimeIdAllocator)
                .GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(graph.RuntimeIdAllocator)
                .GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            RuntimeIdAllocatorCensusProvider.CreateTravelPartyCounterProvider(graph.RuntimeIdAllocator)
                .GetCurrentCensus());
        sections = ReplaceCityOwnerSectionVector(graph, sections);
        sections = ReplaceSelectedNpcOwnerSectionVector(graph, sections);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcSections(
            graph, sections, out _, out string diagnostic), Is.True, diagnostic);
        if (graph.P12EOwners != null)
        {
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedP12EOwnerSections(
                graph, Array.Empty<OwnerSectionCensusSnapshot>(),
                out IReadOnlyList<OwnerSectionCensusSnapshot> exactP12EOwnerRows,
                out diagnostic), Is.True, diagnostic);
            HashSet<string> coreP12EOwnerSectionIds = new HashSet<string>(
                exactP12EOwnerRows.Select(section => section.SectionId), StringComparer.Ordinal);
            sections = sections.Where(section => !coreP12EOwnerSectionIds.Contains(section.SectionId))
                .Concat(exactP12EOwnerRows).ToArray();
        }
        return RebindCoreJusticeOwnerSectionVector(graph, sections);
    }

    private static OwnerSectionCensusSnapshot[] ReplaceSelectedNpcOwnerSectionVector(
        DailyV1ActiveRuntimeSession session,
        OwnerSectionCensusSnapshot[] existing)
        => ReplaceSelectedNpcOwnerSectionVector(session.OwnerGraph, existing);

    private static OwnerSectionCensusSnapshot[] ReplaceSelectedNpcOwnerSectionVector(
        DailyV1SelectedRuntimeOwnerGraph graph,
        OwnerSectionCensusSnapshot[] existing)
    {
        List<OwnerSectionCensusSnapshot> sections = new List<OwnerSectionCensusSnapshot>();
        foreach (OwnerSectionCensusSnapshot section in existing ?? Array.Empty<OwnerSectionCensusSnapshot>())
        {
            bool remove = false;
            foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes)
                if (section.SectionId.StartsWith(prefix, StringComparison.Ordinal))
                {
                    remove = true;
                    break;
                }
            remove |= section.SectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.NpcResidencePrefix, StringComparison.Ordinal)
                || section.SectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.PersonLifeResidencePrefix, StringComparison.Ordinal);
            if (!remove) sections.Add(section);
        }

        foreach (DailyV1NpcRuntimeOwners owner in graph.Npcs)
        {
            Assert.That(owner, Is.Not.Null);
            Assert.That(owner.Inventory.TryGetCensusCardinality(out int inventoryCardinality), Is.True);
            Assert.That(owner.CommercialKnowledge.TryReadCensus(
                out int markets, out int liquidity, out int receipts, out long commercialRevision), Is.True);
            object actorLifecycleAndResidenceIdentity = owner;
            Add(0, owner.ExplorableSiteKnowledge, owner.ExplorableSiteKnowledge.Observations.Count,
                owner.ExplorableSiteKnowledge.Revision);
            Add(1, owner.LocalTopologyKnowledgeEmptyOwner,
                owner.LocalTopologyKnowledgeEmptyOwner.PlaceObservationCount,
                owner.LocalTopologyKnowledgeEmptyOwner.Revision);
            Add(2, owner.LocalTopologyKnowledgeEmptyOwner,
                owner.LocalTopologyKnowledgeEmptyOwner.ConnectionObservationCount,
                owner.LocalTopologyKnowledgeEmptyOwner.Revision);
            Add(3, owner.AdventureSiteIntelKnowledge,
                owner.AdventureSiteIntelKnowledge.OppositionObservations.Count,
                owner.AdventureSiteIntelKnowledge.Revision);
            Add(4, owner.AdventureSiteIntelKnowledge,
                owner.AdventureSiteIntelKnowledge.NotableItemObservations.Count,
                owner.AdventureSiteIntelKnowledge.Revision);
            Add(5, owner.AdventureSiteIntelKnowledge,
                owner.AdventureSiteIntelKnowledge.CommonResourceObservations.Count,
                owner.AdventureSiteIntelKnowledge.Revision);
            Add(6, owner.AdventureSiteIntelKnowledge,
                owner.AdventureSiteIntelKnowledge.AccessObservations.Count,
                owner.AdventureSiteIntelKnowledge.Revision);
            Add(7, owner.CommercialKnowledge, markets, commercialRevision);
            Add(8, owner.CommercialKnowledge, liquidity, commercialRevision);
            Add(9, owner.CommercialKnowledge, receipts, commercialRevision);
            Add(10, owner.SpatialKnowledge,
                owner.SpatialKnowledge.KnownLocationCount, owner.SpatialKnowledge.Revision);
            Add(11, owner.SpatialKnowledge,
                owner.SpatialKnowledge.KnownRouteCount, owner.SpatialKnowledge.Revision);
            Add(12, actorLifecycleAndResidenceIdentity, 1, owner.Travel.Revision);
            Add(13, owner.Inventory, inventoryCardinality, owner.Inventory.Revision);
            Add(14, owner.MoneyAccount, 1, owner.MoneyAccount.Revision);
            Add(15, owner.MerchantTradePlan, 1, owner.MerchantTradePlan.Revision);
            Add(16, owner.TravelPlan, 1, owner.TravelPlan.Revision);
            Add(17, new object(), 0, 0L);
            Add(18, new object(), 0, 0L);
            Add(19, actorLifecycleAndResidenceIdentity, 1, owner.CrimeJusticeRevision);
            Add(20, actorLifecycleAndResidenceIdentity,
                owner.ActionSlot.CurrentAction == null ? 0 : 1, owner.ActionSlot.Revision);
            Add(21, actorLifecycleAndResidenceIdentity, 1, owner.Lifecycle.LifeStateRevision);

            if (graph.PersonStore.TryGetByMaterializedNpcRuntimeId(owner.RuntimeId,
                    out PersonRuntime person))
                sections.Add(new OwnerSectionCensusSnapshot(
                    DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(person.PersonId),
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                    OwnerSectionRole.Required, person, 1,
                    PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person)));
            else
                sections.Add(new OwnerSectionCensusSnapshot(
                    DailyV1SelectedOwnerSectionCensusContract.NpcResidenceSectionIdFor(owner.RuntimeId),
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                    OwnerSectionRole.Required, actorLifecycleAndResidenceIdentity, 1,
                    owner.Residence.Revision));

            void Add(int family, object identity, int cardinality, long revision) =>
                sections.Add(new OwnerSectionCensusSnapshot(
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(family, owner.RuntimeId),
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                    OwnerSectionRole.Required, identity, cardinality, revision));
        }
        return sections.ToArray();
    }

    private static OwnerSectionCensusSnapshot[] ReplaceCityOwnerSectionVector(
        DailyV1ActiveRuntimeSession session,
        OwnerSectionCensusSnapshot[] existing)
        => ReplaceCityOwnerSectionVector(session.OwnerGraph, existing);

    private static OwnerSectionCensusSnapshot[] ReplaceCityOwnerSectionVector(
        DailyV1SelectedRuntimeOwnerGraph graph,
        OwnerSectionCensusSnapshot[] existing)
    {
        List<OwnerSectionCensusSnapshot> sections = new List<OwnerSectionCensusSnapshot>();
        foreach (OwnerSectionCensusSnapshot section in existing)
        {
            bool citySection = false;
            foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.CityPrefixes)
                if (section.SectionId.StartsWith(prefix, StringComparison.Ordinal))
                {
                    citySection = true;
                    break;
                }
            if (!citySection) sections.Add(section);
        }

        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            string cityKey = P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(city.RuntimeId);
            Add(DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId),
                city.NpcPresence, city.NpcPresence.OrderedNpcRuntimeIds.Count, city.NpcPresence.Revision);
            Add(DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix + cityKey,
                city.Market, city.Market.Items.Count, city.Market.Revision);
            string populationId = DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix + cityKey;
            string receiptsId = DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix + cityKey;
            Assert.That(city.Population.TryCaptureOperationReceiptSnapshot(
                out IReadOnlyList<PopulationOperationReceiptSnapshot> receipts, out long receiptRevision), Is.True);
            Add(populationId, city.Population, 1, city.Population.Revision);
            Add(receiptsId, city.Population, receipts.Count, receiptRevision);
        }
        return sections.ToArray();

        void Add(string id, object identity, int cardinality, long revision)
        {
            sections.Add(new OwnerSectionCensusSnapshot(id,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, identity, cardinality, revision));
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
    public void CoreBoundP12CCausalRootsCaptureFromTheExactGenesisSession()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        DailyCaptureEligibilityToken sourceToken = InstallCompletedBoundaryToken(session, boundary);

        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation, out _), Is.True);
        try
        {
            Assert.That(DailyV1P12GCoreBoundCausalRootSources.TryCapture(
                operation, out DailyV1P12GCoreBoundCausalRootSources sources,
                out DailyV1P12GCoreBoundCausalRootCaptureFailure failure,
                out string diagnostic), Is.True, diagnostic);

            Assert.That(failure, Is.EqualTo(DailyV1P12GCoreBoundCausalRootCaptureFailure.None));
            Assert.That(sources.IsFor(operation), Is.True);
            Assert.That(operation.SourceWitness.Token, Is.SameAs(sourceToken));
            Assert.That(sources.Snapshots.WorldIdentity.WorldIdValue, Is.EqualTo(session.WorldId.Value));
            Assert.That(sources.Snapshots.P9Manifest, Is.SameAs(session.GenesisComposition.P9ManifestSnapshot));
            Assert.That(sources.Snapshots.P9Manifest.Fingerprint,
                Is.EqualTo(session.GenesisComposition.P9ProfileFingerprint));
            Assert.That(sources.Snapshots.SpatialAuthority.OwnerRevision,
                Is.EqualTo(session.GenesisComposition.SpatialAuthority.Revision));
            Assert.That(sources.Snapshots.RandomRoot.Seed,
                Is.EqualTo(sources.Snapshots.P9Manifest.Seed));
            Assert.That(sources.Snapshots.Allocator, Is.Not.Null);
            Assert.That(sources.Snapshots.RecordSequence, Is.Not.Null);
            Assert.That(operation.TryRevalidate(out DailyCaptureEligibilityFailure staleFailure), Is.True,
                staleFailure.ToString());
        }
        finally
        {
            operation.Dispose();
        }
    }

    [Test]
    public void CoreBoundP12CCausalRootsRejectAStaleOwnerVector()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundCompleteOwnerSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);
        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation, out _), Is.True);
        try
        {
            OwnerSectionCensusSnapshot original = sections[0];
            sections[0] = new OwnerSectionCensusSnapshot(
                original.SectionId, original.SchemaVersion, original.Role,
                original.OwnerInstanceIdentity, original.Cardinality, original.Revision + 1L);
            boundary.SetAdmissionEvidence(sections, 0L);

            Assert.That(DailyV1P12GCoreBoundCausalRootSources.TryCapture(
                operation, out DailyV1P12GCoreBoundCausalRootSources sources,
                out DailyV1P12GCoreBoundCausalRootCaptureFailure failure,
                out _), Is.False);
            Assert.That(sources, Is.Null);
            Assert.That(failure, Is.EqualTo(DailyV1P12GCoreBoundCausalRootCaptureFailure.StaleSourceBoundary));
        }
        finally
        {
            operation.Dispose();
        }
    }

    [Test]
    public void ContinuationSaveRevalidationRejectsChangedOwnerCensusAndBalancesScope()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundCompleteOwnerSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);
        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation, out _), Is.True);

        const string hostOwnedSectionId = "p8d.spatial-route-observations";
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedSelectedRuntimeSectionIds
                .Contains(hostOwnedSectionId, StringComparer.Ordinal),
            Is.False, "The changed-revision probe must mutate the host-owned exact-empty row, not a Core-bound identity.");
        int hostOwnedSectionIndex = Array.FindIndex(sections,
            section => section.SectionId == hostOwnedSectionId);
        Assert.That(hostOwnedSectionIndex, Is.GreaterThanOrEqualTo(0));
        OwnerSectionCensusSnapshot original = sections[hostOwnedSectionIndex];
        sections[hostOwnedSectionIndex] = new OwnerSectionCensusSnapshot(
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
    public void ContinuationSaveCaptureExceptionClosesRegisteredScopeAndPreservesOriginalException()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        InstallCompletedBoundaryToken(session, boundary);
        InvalidOperationException expected = new InvalidOperationException(
            "Synthetic continuation-save evidence failure.");
        boundary.ThrowOnContinuationSaveCapture(expected);

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
            application.TryBeginContinuationSaveOperation(
                session, out DailyV1P12GContinuationSaveOperation _, out _));

        Assert.That(actual, Is.SameAs(expected));
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void CoreBoundP12FSourcesCaptureFromExactActiveSessionAndSaveWitness()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        PoliticalKnowledgeStore politicalKnowledge = CreateAndBindCorePoliticalKnowledgeStore(graph);
        ActorChoiceStore actorChoices = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        Assert.That(graph.PoliticalKnowledgeStore, Is.SameAs(politicalKnowledge));
        Assert.That(graph.Expeditions, Is.Not.Null,
            "Core genesis must bind its ExpeditionStore into the selected owner graph.");
        Assert.That(graph.ScheduledDirectives, Is.Not.Null);

        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundP12FSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);

        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation, out _), Is.True);
        Assert.That(DailyV1P12GCoreBoundOwnerSources.TryCapture(
            operation, out DailyV1P12GCoreBoundOwnerSources sources,
            out DailyV1P12GCoreBoundOwnerSourceCaptureFailure failure,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(failure, Is.EqualTo(DailyV1P12GCoreBoundOwnerSourceCaptureFailure.None));
        Assert.That(sources.IsFor(operation), Is.True);
        Assert.That(sources.PoliticalKnowledge.Cardinality, Is.EqualTo(politicalKnowledge.Count));
        Assert.That(sources.PoliticalKnowledge.Revision, Is.EqualTo(politicalKnowledge.Revision));
        Assert.That(sources.ActorChoices.Inputs.Count, Is.EqualTo(actorChoices.P11InputCount));
        Assert.That(sources.ActorChoices.NextInputSequence, Is.EqualTo(actorChoices.NextInputSequence));
        Assert.That(sources.ActorChoices.CensusRevision, Is.EqualTo(actorChoices.CensusRevision));
        Assert.That(sources.Directives.Rows.Count,
            Is.EqualTo(graph.ScheduledDirectives.GetCurrentCensus().Cardinality));
        Assert.That(sources.Directives.Revision, Is.EqualTo(graph.ScheduledDirectives.Revision));
        Assert.That(sources.TravelParties.Parties.Count, Is.EqualTo(graph.TravelParties.ActiveParties.Count));
        Assert.That(sources.TravelParties.Revision, Is.EqualTo(graph.TravelParties.Revision));
        Assert.That(sources.Expeditions.Expeditions.Count,
            Is.EqualTo(new ExpeditionCensusProvider(graph.Expeditions).GetCurrentCensus().Cardinality));
        Assert.That(sources.Expeditions.Revision, Is.EqualTo(graph.Expeditions.Revision));
        Assert.That(operation.TryRevalidate(out DailyCaptureEligibilityFailure revalidationFailure), Is.True);
        Assert.That(revalidationFailure, Is.EqualTo(DailyCaptureEligibilityFailure.None));
        Assert.That(application.ActiveOperationCount, Is.EqualTo(1));

        operation.Dispose();
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void CoreBoundP12FSourcesRejectMissingOwnerGraphBindings()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        DailyV1SelectedRuntimeOwnerGraph current = session.OwnerGraph;
        CreateAndBindCorePoliticalKnowledgeStore(current);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            session.Definitions, current.Cities, current.Npcs, current.SpatialNetwork,
            current.JusticeRecordsOwner, current.TravelParties, current.RuntimeIdentityIndex,
            current.PersonStore, out DailyV1SelectedRuntimeOwnerGraph unboundGraph), Is.True);
        Assert.That(unboundGraph.ScheduledDirectives, Is.Null);
        Assert.That(unboundGraph.Expeditions, Is.Null);
        Assert.That(DailyV1P12GCoreBoundOwnerSources.HasRequiredCoreOwnerBindings(
            unboundGraph, session.ExecutionBoundaryState.SelectedActorChoiceStore), Is.False);
        Assert.That(DailyV1P12GCoreBoundOwnerSources.HasRequiredCoreOwnerBindings(
            current, session.ExecutionBoundaryState.SelectedActorChoiceStore), Is.True);
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void CoreBoundP12FSourcesRejectWitnessChangedDuringRecapture()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundP12FSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);
        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation, out _), Is.True);

        int invalidateAt = boundary.ContinuationSaveEvidenceCaptureCount + 1;
        boundary.SetContinuationSaveEvidenceCaptureCallback(captureCount =>
        {
            if (captureCount != invalidateAt) return;
            OwnerSectionCensusSnapshot current = sections[0];
            sections[0] = new OwnerSectionCensusSnapshot(
                current.SectionId, current.SchemaVersion, current.Role,
                current.OwnerInstanceIdentity, current.Cardinality, current.Revision + 1L);
        });

        Assert.That(DailyV1P12GCoreBoundOwnerSources.TryCapture(
            operation, out DailyV1P12GCoreBoundOwnerSources sources,
            out DailyV1P12GCoreBoundOwnerSourceCaptureFailure failure,
            out string diagnostic), Is.False, diagnostic);
        Assert.That(sources, Is.Null);
        Assert.That(failure, Is.EqualTo(
            DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary));
        Assert.That(application.ActiveOperationCount, Is.EqualTo(1),
            "A rejected capture keeps its enclosing save lease active until disposal.");
        operation.Dispose();
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void CoreBoundCitySourcesCaptureExactCityOwnerValuesAndRevisionVector()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundCityAndP12FSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);
        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation, out _), Is.True);

        Assert.That(DailyV1P12GCoreBoundCityOwnerSources.TryCapture(
            operation, out DailyV1P12GCoreBoundCityOwnerSources sources,
            out DailyV1P12GCoreCitySourceCaptureFailure failure,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(failure, Is.EqualTo(DailyV1P12GCoreCitySourceCaptureFailure.None));
        Assert.That(sources.IsFor(operation), Is.True);
        Assert.That(operation.CoreCityOwnerSources, Is.SameAs(sources),
            "The save operation must retain the Core City sources captured at admission.");
        DailyV1P12GCoreBoundDemographicOwnerSources demographic =
            operation.CoreDemographicOwnerSources;
        Assert.That(demographic, Is.Not.Null);
        Assert.That(demographic.IsFor(operation), Is.True);
        Assert.That(demographic.Persons, Is.SameAs(session.OwnerGraph.PersonStore));
        Assert.That(demographic.Genealogy, Is.SameAs(session.OwnerGraph.GenealogyStore));
        OwnerSectionCensusSnapshot membership = operation.SourceWitness.TokenOwnerSections.Single(
            section => section.SectionId == DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId);
        OwnerSectionCensusSnapshot bindings = operation.SourceWitness.TokenOwnerSections.Single(
            section => section.SectionId == DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId);
        OwnerSectionCensusSnapshot genealogy = operation.SourceWitness.TokenOwnerSections.Single(
            section => section.SectionId == "p12d.genealogy.parentage");
        Assert.That(membership.OwnerInstanceIdentity, Is.SameAs(demographic.Persons));
        Assert.That(membership.Cardinality, Is.EqualTo(demographic.PersonSnapshot.MembershipCount));
        Assert.That(bindings.OwnerInstanceIdentity, Is.SameAs(demographic.Persons));
        Assert.That(bindings.Cardinality, Is.EqualTo(demographic.PersonSnapshot.BindingCount));
        Assert.That(genealogy.OwnerInstanceIdentity, Is.SameAs(demographic.Genealogy));
        Assert.That(genealogy.Cardinality, Is.EqualTo(demographic.GenealogySnapshot.Records.Count));
        Assert.That(demographic.PersonSnapshot.Revision, Is.EqualTo(membership.Revision));
        Assert.That(demographic.PersonSnapshot.Revision, Is.EqualTo(bindings.Revision));
        Assert.That(demographic.GenealogySnapshot.Revision, Is.EqualTo(genealogy.Revision));
        DailyV1P12GCoreBoundSpatialOwnerSources spatial = operation.CoreSpatialOwnerSources;
        Assert.That(spatial, Is.Not.Null);
        Assert.That(spatial.IsFor(operation), Is.True);
        Assert.That(spatial.Owner, Is.SameAs(session.OwnerGraph.SpatialNetwork));
        OwnerSectionCensusSnapshot locations = operation.SourceWitness.TokenOwnerSections.Single(
            section => section.SectionId == P12ASpatialNetworkOwnerCodec.LocationsSectionId);
        OwnerSectionCensusSnapshot routes = operation.SourceWitness.TokenOwnerSections.Single(
            section => section.SectionId == P12ASpatialNetworkOwnerCodec.RoutesSectionId);
        Assert.That(locations.OwnerInstanceIdentity, Is.SameAs(routes.OwnerInstanceIdentity));
        Assert.That(locations.Cardinality, Is.EqualTo(spatial.Snapshot.LocationRuntimeIds.Count));
        Assert.That(routes.Cardinality, Is.EqualTo(spatial.Snapshot.Routes.Count));
        Assert.That(spatial.Snapshot.Revision, Is.EqualTo(locations.Revision));
        Assert.That(spatial.Snapshot.Revision, Is.EqualTo(routes.Revision));
        Assert.That(sources.Cities.Count, Is.EqualTo(session.OwnerGraph.Cities.Count));
        for (int index = 0; index < sources.Cities.Count; index++)
        {
            DailyV1P12GCoreCityOwnerSource source = sources.Cities[index];
            DailyV1CityRuntimeOwners owner = session.OwnerGraph.Cities[index];
            Assert.That(source.Owner, Is.SameAs(owner));
            Assert.That(source.Input.CityRuntimeId, Is.EqualTo(owner.RuntimeId));
            Assert.That(source.Input.CityDefinitionId, Is.EqualTo(owner.Definition.DefinitionId));
            Assert.That(source.Input.ResolvedLocationRuntimeId, Is.EqualTo(owner.LocationRuntimeId));
            Assert.That(source.Input.ImportantNpcRuntimeIds,
                Is.EqualTo(owner.NpcPresence.OrderedNpcRuntimeIds));
            Assert.That(source.Input.ImportantNpcRevision, Is.EqualTo(owner.NpcPresence.Revision));
            Assert.That(source.Input.MarketRevision, Is.EqualTo(owner.Market.Revision));
            Assert.That(source.Input.MarketItems.Count, Is.EqualTo(owner.Market.Items.Count));
            Assert.That(source.Input.CurrentPopulation, Is.EqualTo(owner.Population.CurrentPopulation));
            Assert.That(source.Input.PopulationRevision, Is.EqualTo(owner.Population.Revision));
            Assert.That(source.Sections.Count, Is.EqualTo(4));

            string cityKey = P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(owner.RuntimeId);
            P12AOwnerSection presence = source.Sections.Single(section => section.Id ==
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(owner.RuntimeId));
            P12AOwnerSection market = source.Sections.Single(section => section.Id ==
                DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix + cityKey);
            P12AOwnerSection population = source.Sections.Single(section => section.Id ==
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix + cityKey);
            P12AOwnerSection receipts = source.Sections.Single(section => section.Id ==
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix + cityKey);
            Assert.That(presence.Rows.Count, Is.EqualTo(owner.NpcPresence.OrderedNpcRuntimeIds.Count));
            Assert.That(presence.Revision, Is.EqualTo(owner.NpcPresence.Revision));
            Assert.That(market.Rows.Count, Is.EqualTo(owner.Market.Items.Count));
            Assert.That(market.Revision, Is.EqualTo(owner.Market.Revision));
            Assert.That(population.Rows.Count, Is.EqualTo(1));
            Assert.That(population.Revision, Is.EqualTo(owner.Population.Revision));
            Assert.That(receipts.Rows.Count, Is.EqualTo(source.Input.PopulationReceipts.Count));
            Assert.That(receipts.Revision, Is.EqualTo(source.Input.PopulationReceiptRevision));
        }
        Assert.That(operation.TryRevalidate(out DailyCaptureEligibilityFailure revalidationFailure), Is.True);
        Assert.That(revalidationFailure, Is.EqualTo(DailyCaptureEligibilityFailure.None));
        operation.Dispose();
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void CoreBoundNpcRowsAreBoundInGraphOrderAndRejectAStaleCensus()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundCompleteOwnerSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);

        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation,
            out DailyCaptureEligibilityFailure beginFailure), Is.True, beginFailure.ToString());
        try
        {
            DailyV1P12GCoreBoundNpcOwnerSources sources = operation.CoreNpcOwnerSources;
            Assert.That(sources, Is.Not.Null,
                "The complete admitted roster must bind operation-owned Core D/F rows.");
            Assert.That(sources.IsFor(operation), Is.True);
            Assert.That(sources.Owners.Count, Is.EqualTo(session.OwnerGraph.Npcs.Count));
            Assert.That(sources.DRows.Count, Is.EqualTo(session.OwnerGraph.Npcs.Count));
            Assert.That(sources.FRows.Count, Is.EqualTo(session.OwnerGraph.Npcs.Count));
            for (int index = 0; index < session.OwnerGraph.Npcs.Count; index++)
            {
                DailyV1NpcRuntimeOwners owner = session.OwnerGraph.Npcs[index];
                Assert.That(sources.Owners[index], Is.SameAs(owner));
                Assert.That(sources.DRows[index].RuntimeId, Is.EqualTo(owner.RuntimeId));
                Assert.That(sources.FRows[index].RuntimeId, Is.EqualTo(owner.RuntimeId));
                Assert.That(sources.DRows[index].InventoryRevision,
                    Is.EqualTo(owner.Inventory.Revision));
                Assert.That(sources.DRows[index].MoneyRevision,
                    Is.EqualTo(owner.MoneyAccount.Revision));
                Assert.That(sources.DRows[index].LifeStateRevision,
                    Is.EqualTo(owner.Lifecycle.LifeStateRevision));
                Assert.That(sources.DRows[index].CrimeJusticeRevision,
                    Is.EqualTo(owner.CrimeJusticeRevision));
                Assert.That(sources.DRows[index].CurrentActionRevision,
                    Is.EqualTo(owner.ActionSlot.Revision));
                Assert.That(sources.FRows[index].TravelStateRevision,
                    Is.EqualTo(owner.Travel.Revision));
                Assert.That(sources.FRows[index].TravelPlan.Revision,
                    Is.EqualTo(owner.TravelPlan.Revision));
                Assert.That(sources.FRows[index].MerchantPlan.Revision,
                    Is.EqualTo(owner.MerchantTradePlan.Revision));
                Assert.That(sources.FRows[index].SpatialKnowledgeRevision,
                    Is.EqualTo(owner.SpatialKnowledge.Revision));
                Assert.That(sources.FRows[index].CommercialKnowledgeRevision,
                    Is.EqualTo(owner.CommercialKnowledge.Revision));
            }

            int npcInventorySection = Array.FindIndex(sections, section =>
                string.Equals(section.SectionId,
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
                        13, session.OwnerGraph.Npcs[0].RuntimeId), StringComparison.Ordinal));
            Assert.That(npcInventorySection, Is.GreaterThanOrEqualTo(0));
            OwnerSectionCensusSnapshot original = sections[npcInventorySection];
            sections[npcInventorySection] = new OwnerSectionCensusSnapshot(
                original.SectionId, original.SchemaVersion, original.Role,
                original.OwnerInstanceIdentity, original.Cardinality, original.Revision + 1L);
            boundary.SetAdmissionEvidence(sections, 0L);

            Assert.That(DailyV1P12GCoreBoundNpcOwnerSources.TryCapture(
                operation, out DailyV1P12GCoreBoundNpcOwnerSources staleSources,
                out DailyV1P12GCoreBoundOwnerSourceCaptureFailure staleFailure,
                out _), Is.False);
            Assert.That(staleSources, Is.Null);
            Assert.That(staleFailure,
                Is.EqualTo(DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary));
            Assert.That(operation.CoreNpcOwnerSources, Is.SameAs(sources),
                "A stale recapture must not replace the operation's original immutable rows.");
            Assert.That(application.ActiveSession, Is.SameAs(session));
            Assert.That(application.ActiveOperationCount, Is.EqualTo(1));
        }
        finally
        {
            operation.Dispose();
        }
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void CoreBoundSpatialSourcesRejectStaleOwnerWitnessAfterLeaseAdmission()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundCompleteOwnerSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);
        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation, out _), Is.True);

        int invalidateAt = boundary.ContinuationSaveEvidenceCaptureCount + 1;
        boundary.SetContinuationSaveEvidenceCaptureCallback(captureCount =>
        {
            if (captureCount != invalidateAt) return;
            int index = Array.FindIndex(sections, section =>
                section.SectionId == P12ASpatialNetworkOwnerCodec.LocationsSectionId);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            OwnerSectionCensusSnapshot current = sections[index];
            sections[index] = new OwnerSectionCensusSnapshot(
                current.SectionId, current.SchemaVersion, current.Role,
                current.OwnerInstanceIdentity, current.Cardinality, current.Revision + 1L);
        });

        Assert.That(DailyV1P12GCoreBoundSpatialOwnerSources.TryCapture(
            operation, out DailyV1P12GCoreBoundSpatialOwnerSources sources,
            out DailyV1P12GCoreBoundOwnerSourceCaptureFailure failure,
            out string diagnostic), Is.False, diagnostic);
        Assert.That(sources, Is.Null);
        Assert.That(failure, Is.EqualTo(
            DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary));
        Assert.That(operation.CoreSpatialOwnerSources, Is.Not.Null,
            "The initial exact source remains bound until the active save lease is disposed.");
        Assert.That(application.ActiveOperationCount, Is.EqualTo(1));
        operation.Dispose();
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void ContinuationSaveRejectsMismatchedDemographicOwnerIdentityBeforeOperationPublication()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundCityAndP12FSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);

        int membershipIndex = Array.FindIndex(sections,
            section => section.SectionId == DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId);
        Assert.That(membershipIndex, Is.GreaterThanOrEqualTo(0));
        OwnerSectionCensusSnapshot membership = sections[membershipIndex];
        sections[membershipIndex] = new OwnerSectionCensusSnapshot(
            membership.SectionId, membership.SchemaVersion, membership.Role,
            new object(), membership.Cardinality, membership.Revision);

        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation,
            out DailyCaptureEligibilityFailure failure), Is.False);
        Assert.That(operation, Is.Null,
            "A token vector bound to another PersonStore must not publish a Core save operation.");
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));
        Assert.That(application.ActiveSession, Is.SameAs(session));
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void SaveAdmissionRejectsCoreCityDailyEconomyReceipt()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        DailyV1CityRuntimeOwners city = session.OwnerGraph.Cities[0];
        RecordNoOpDailyEconomyReceipt(city);
        city.GetDailyEconomyReceiptExclusionProof(out int receiptCount, out long receiptRevision);
        Assert.That(receiptCount, Is.EqualTo(1));
        Assert.That(receiptRevision, Is.EqualTo(1L));

        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundCityAndP12FSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);

        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation,
            out DailyCaptureEligibilityFailure failure), Is.False);
        Assert.That(operation, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OwnerCoverageIncomplete));
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void SaveAdmissionRejectsEmptyButAdvancedCoreCityDailyEconomyReceiptRevision()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        DailyV1CityRuntimeOwners city = session.OwnerGraph.Cities[0];
        FieldInfo revisionField = typeof(DailyV1CityRuntimeOwners).GetField(
            "dailyEconomyReceiptRevision", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(revisionField, Is.Not.Null);
        revisionField.SetValue(city, 1L);
        city.GetDailyEconomyReceiptExclusionProof(out int receiptCount, out long receiptRevision);
        Assert.That(receiptCount, Is.Zero);
        Assert.That(receiptRevision, Is.EqualTo(1L),
            "A receipt-free but advanced local ledger is not the accepted exact-zero state.");

        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundCityAndP12FSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);

        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation,
            out DailyCaptureEligibilityFailure failure), Is.False);
        Assert.That(operation, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OwnerCoverageIncomplete));
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void SaveAdmissionRejectsCoreCityLocalMaterialFlowState()
    {
        DailyV1EffectiveDefinitions definitions = CreateP14LocalFlowDefinitions();
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary, definitions);
        DailyV1CityRuntimeOwners city = session.OwnerGraph.Cities[0];
        Assert.That(city.HasLocalDailyMaterialFlow, Is.True);

        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundCityAndP12FSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);

        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation,
            out DailyCaptureEligibilityFailure failure), Is.False);
        Assert.That(operation, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OwnerCoverageIncomplete));
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    private static void RecordNoOpDailyEconomyReceipt(DailyV1CityRuntimeOwners city)
    {
        DailyBoundaryOperation operation = new DailyBoundaryOperation(
            "city-p12-capture-exclusion", "daily-v1", 1);
        Assert.That(city.TryCreateDailyEconomyStep(operation, 0,
            CityDailyEconomyStepKind.PriceRefresh, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "city-economy", "1", "p12-capture-exclusion", new[] { step }, "fixture");
        Assert.That(city.TryPrepareDailyEconomyStep(manifest, step,
            out IBoundaryContinuationStepCommit prepared, out _), Is.True);
        Assert.That(prepared.TryCommit(out _), Is.True);
    }

    private static DailyV1EffectiveDefinitions CreateP14LocalFlowDefinitions()
    {
        DailyV1EffectiveDefinitions baseline = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition baselineCity = baseline.Cities["CAMPO_VERDE"];
        DailyV1CityData city = new DailyV1CityData(
            baselineCity.Values.id, baselineCity.Values.cityName,
            "settlement/p12-exclusion", baseline.Configuration.authoredLocationId,
            "store/p12-exclusion",
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.ExogenousDaily, "ExogenousDaily"),
            baselineCity.Values.initialPopulation, baselineCity.Values.marketLiquidity,
            baselineCity.Values.populationConsumption, baselineCity.Values.marketItems,
            new[] { new DailyV1CityProductionConfig(
                "TRIGO", 1, 0, "source/p12-exclusion", "revision/1",
                new DailyV1EnumValue((int)CityProductionSourceKind.ExogenousDaily, "ExogenousDaily")) },
            baselineCity.Values.connections);

        return DailyV1EffectiveDefinitions.CreateSelectedSchema(
            baseline.Configuration, baseline.EffectiveConfiguration, baseline.Calendar,
            baseline.Cities.Values.Select(value => value.DefinitionId == city.id ? city : value.Values),
            baseline.Items.Values.Select(value => value.Values),
            baseline.Npcs.Values.Select(value => value.Values),
            baseline.Jobs.Values.Select(value => value.Values),
            baseline.Actions.Values.Select(value => value.Values),
            baseline.Statuses.Values.Select(value => value.Values),
            baseline.StageOrder, baseline.ProfileContractIdentity,
            baseline.GeographyProfileContractIdentity, baseline.GeographyStageIdentity,
            baseline.HexCoordinateConvention, baseline.HexCoordinateCanonicalOrder,
            baseline.Traits.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.DisplayName)),
            baseline.CapabilityAttributes.Select(pair =>
                new KeyValuePair<string, string>(pair.Key, pair.Value.DisplayName)),
            baseline.ExplorableSites.Values.Select(value => value.Values));
    }

    [Test]
    public void SaveOperationRejectsMismatchedCoreCityOwnerIdentityBeforeIssuingLease()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundCityAndP12FSectionVector(session);
        string presenceId = DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(
            session.OwnerGraph.Cities[0].RuntimeId);
        int index = Array.FindIndex(sections, section => section.SectionId == presenceId);
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        OwnerSectionCensusSnapshot current = sections[index];
        sections[index] = new OwnerSectionCensusSnapshot(current.SectionId,
            current.SchemaVersion, current.Role, new object(),
            current.Cardinality, current.Revision);
        InstallCompletedBoundaryToken(session, boundary, sections);

        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation,
            out DailyCaptureEligibilityFailure failure), Is.False);
        Assert.That(operation, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OwnerCoverageIncomplete));
        Assert.That(application.ActiveOperationCount, Is.Zero);
    }

    [Test]
    public void CoreBoundCitySourcesDiscardAllStagedRowsWhenWitnessChangesDuringRecapture()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            onOperationEntry: null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        OwnerSectionCensusSnapshot[] sections = CreateCoreBoundCityAndP12FSectionVector(session);
        InstallCompletedBoundaryToken(session, boundary, sections);
        Assert.That(application.TryBeginContinuationSaveOperation(
            session, out DailyV1P12GContinuationSaveOperation operation, out _), Is.True);
        int invalidateAt = boundary.ContinuationSaveEvidenceCaptureCount + 1;
        boundary.SetContinuationSaveEvidenceCaptureCallback(captureCount =>
        {
            if (captureCount != invalidateAt) return;
            OwnerSectionCensusSnapshot current = sections[0];
            sections[0] = new OwnerSectionCensusSnapshot(current.SectionId,
                current.SchemaVersion, current.Role, current.OwnerInstanceIdentity,
                current.Cardinality, current.Revision + 1L);
        });

        Assert.That(DailyV1P12GCoreBoundCityOwnerSources.TryCapture(
            operation, out DailyV1P12GCoreBoundCityOwnerSources sources,
            out DailyV1P12GCoreCitySourceCaptureFailure failure,
            out string diagnostic), Is.False, diagnostic);
        Assert.That(sources, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyV1P12GCoreCitySourceCaptureFailure.StaleSourceBoundary));
        Assert.That(application.ActiveOperationCount, Is.EqualTo(1));
        operation.Dispose();
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
        Assert.That(foreignRestoredSession.TryBindSelectedRuntimeAdvanceHostPorts(
            new ApplicationAdvanceBoundary(foreignRestoredSession.ExecutionBoundaryState, null)), Is.True);
        Assert.That(application.TryPublishInitial(foreignRestoredSession), Is.False,
            "An application cannot publish a restored session admitted by a different bootstrap context on the same thread.");

        SimulationRuntimeAdmissionContext restoredAdmission =
            admission.CreateRestoredContinuationContext();
        Assert.That(restoredAdmission.IsRestoredContinuationOf(admission), Is.True);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            CreateBoundaryWithAdmission(composition, restoredAdmission), definitions, 0L,
            out DailyV1ActiveRuntimeSession session, out diagnostic), Is.True, diagnostic);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(
            session, new ApplicationAdvanceBoundary(session.ExecutionBoundaryState, null)), Is.True);
        Assert.That(application.TryPublishInitial(session), Is.False,
            "A restored descendant cannot use the generic genesis publication surface.");
        Assert.That(application.ActiveSession, Is.Null,
            "Restored publication requires the exact initial slot-admission token.");
    }

    [Test]
    public void CoreRestoreGenericInitialPublicationRequiresExactSlotAdmission()
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
            CreateCoreBoundCompleteOwnerSectionVector(
                composition.OwnerGraph, composition.ActorChoiceStore);
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
        Assert.That(restoredState.TryMarkFactualReadWorldPublished(), Is.True);

        FieldInfo decisionStoreField = typeof(DailyV1ExecutionBoundaryState).GetField(
            "selectedDecisionStore", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(decisionStoreField, Is.Not.Null);
        NpcDecisionStore exactDecisionStore = restoredState.SelectedDecisionStore;
        decisionStoreField.SetValue(restoredState, null);
        Assert.That(application.TryCreateRestoredActiveRuntimeSession(
            restoredState, definitions, 0L, null,
            out DailyV1ActiveRuntimeSession missingDecisionStoreCandidate,
            out diagnostic), Is.False);
        Assert.That(missingDecisionStoreCandidate, Is.Null);
        Assert.That(diagnostic, Does.Contain("decision-store roots"));
        decisionStoreField.SetValue(restoredState, exactDecisionStore);

        Assert.That(application.TryCreateRestoredActiveRuntimeSession(
            restoredState, definitions, 0L, null,
            out DailyV1ActiveRuntimeSession candidate, out diagnostic), Is.True, diagnostic);

        ApplicationAdvanceBoundary boundary = new ApplicationAdvanceBoundary(
            restoredState, null, ownerSections, 23L);
        TestHostProjection projection = new TestHostProjection();

        // The generic commit surface is for a replacement. Initial restored
        // publication must use the separate exact slot-admission token path.
        Assert.That(application.TryCommitRestoredCandidate(
            null, candidate, null, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("exact initial slot admission"));
        Assert.That(application.TryPublishInitial(candidate), Is.False);
        Assert.That(application.TryPublishInitialWithHostProjection(
            candidate, projection, boundary), Is.False,
            "A tokenless initial candidate must be rejected before binding its host boundary.");
        Assert.That(application.TryGetPublishedHostProjection(candidate, out _), Is.False);
        Assert.That(application.TryCommitRestoredCandidateWithHostProjection(
            null, candidate, null, projection, boundary, null, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("exact initial slot admission"));
        Assert.That(application.ActiveSession, Is.Null,
            "A restored candidate without the initial slot token remains private.");
        Assert.That(application.TryGetPublishedHostProjection(candidate, out _), Is.False,
            "A rejected candidate cannot leave a host projection registered.");
    }

    [Test]
    public void CoreRestoreReplacementAcceptsRestoredCandidateTokenAndRejectsStaleSourceCensus()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext bootstrapAdmission), Is.True);
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out DailyV1GenesisComposition sourceComposition,
            out DailyV1ActiveRuntimeSession sourceSession,
            out string diagnostic), Is.True, diagnostic);
        DailyV1ExecutionBoundaryState sourceState = sourceSession.ExecutionBoundaryState;
        EnsureRuntimeProtocolInventory(sourceState);
        Assert.That(sourceState.TryMarkSelectedRuntimeCensusProtocolReady(), Is.True);
        Assert.That(sourceState.TryMarkFactualReadWorldPublished(), Is.True);
        OwnerSectionCensusSnapshot[] sourceOwners =
            CreateCoreBoundCompleteOwnerSectionVector(sourceSession);
        long sourceEpoch = sourceState.MutationEpochState.CurrentEpoch;
        ApplicationAdvanceBoundary sourceBoundary = new ApplicationAdvanceBoundary(
            sourceState, null, sourceOwners, sourceEpoch);
        TestHostProjection sourceProjection = new TestHostProjection();
        Assert.That(application.TryPublishInitialWithHostProjection(
            sourceSession, sourceProjection, sourceBoundary), Is.True);
        Assert.That(application.ActiveSession, Is.SameAs(sourceSession));
        Assert.That(application.TryGetPublishedHostProjection(sourceSession,
            out IDailyV1ApplicationHostProjection publishedSourceProjection), Is.True);
        Assert.That(publishedSourceProjection, Is.SameAs(sourceProjection));
        Assert.That(InstallCompletedBoundaryToken(
            sourceSession, sourceBoundary, sourceOwners, sourceEpoch), Is.Not.Null);

        Assert.That(application.TryCreateRestoredContinuationAdmissionContext(
            sourceSession, null, out SimulationRuntimeAdmissionContext unreservedReplacementAdmission,
            out diagnostic), Is.False);
        Assert.That(unreservedReplacementAdmission, Is.Null);

        Assert.That(application.TryBeginExclusiveOperation(sourceSession, out var replacementOperation), Is.True);
        using (replacementOperation)
        {
            Assert.That(application.TryCreateRestoredContinuationAdmissionContext(
                sourceSession, replacementOperation,
                out SimulationRuntimeAdmissionContext candidateAdmission,
                out diagnostic), Is.True, diagnostic);
            Assert.That(candidateAdmission.IsRestoredContinuation, Is.True);

            Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions, bootstrapAdmission,
                out DailyV1GenesisComposition candidateComposition,
                out diagnostic), Is.True, diagnostic);

            DailyV1ExecutionBoundaryState candidateState =
                CreateBoundaryWithAdmission(candidateComposition, candidateAdmission);
            OwnerSectionCensusSnapshot[] candidateOwners =
            CreateCoreBoundCompleteOwnerSectionVector(
                candidateComposition.OwnerGraph, candidateComposition.ActorChoiceStore);
            Assert.That(candidateOwners.First(section => section.SectionId
                == PoliticalDecisionStoreCensusProvider.SectionId).OwnerInstanceIdentity,
                Is.SameAs(candidateComposition.OwnerGraph.PoliticalDecisionStore),
                "The restored token fixture starts with the exact P12-E Core owner identity.");
            long candidateEpoch = candidateState.MutationEpochState.CurrentEpoch;
            ApplicationAdvanceBoundary candidateBoundary = new ApplicationAdvanceBoundary(
                candidateState, null, candidateOwners, candidateEpoch);
            Assert.That(candidateState.TryBindSelectedRuntimeOwnerCensusSource(
                candidateBoundary.SelectedRuntimeOwnerCensusSourceIdentity,
                allowedOuterScope => candidateBoundary.TryCaptureOwnerSectionCensus(
                        allowedOuterScope,
                        out IReadOnlyList<OwnerSectionCensusSnapshot> capturedOwnerSections,
                        out long capturedEpoch,
                        out DailyCaptureEligibilityFailure captureFailure)
                    ? DailyV1OwnerSectionCensusCaptureResult.Captured(
                        capturedOwnerSections, capturedEpoch)
                    : DailyV1OwnerSectionCensusCaptureResult.Rejected(
                        captureFailure, "The restore fixture census source rejected capture.")), Is.True,
                "The restored candidate binds its exact host census before issuing a capture token.");
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
            Assert.That(candidateState.TryMarkFactualReadWorldPublished(), Is.True);

            Assert.That(application.TryCreateRestoredActiveRuntimeSession(
                candidateState, definitions, 0L, replacementOperation,
                out DailyV1ActiveRuntimeSession candidate, out diagnostic), Is.True, diagnostic);

            Assert.That(bootstrapAdmission.IsOwnedByCurrentThread(), Is.True,
                "The restore commit runs on the application's bootstrap owner thread.");
            Assert.That(candidateAdmission.IsOwnedByCurrentThread(), Is.True,
                "The restored continuation admission belongs to the current owner thread.");
            Assert.That(candidateState.IsHealthyDailyOwnerThreadBoundary(), Is.True,
                "The candidate is healthy and idle before replacement validation.");
            Assert.That(candidateState.HasValidRestoredDailyBoundaryAdmission(candidateBoundary), Is.True,
                "The exact restored continuation token still matches the candidate's owner census.");
            Assert.That(candidateState.ActiveOperationCount, Is.EqualTo(0));
            TestHostProjection candidateProjection = new TestHostProjection();

            List<OwnerSectionCensusSnapshot> staleSourceOwners = new List<OwnerSectionCensusSnapshot>(sourceOwners);
            OwnerSectionCensusSnapshot first = staleSourceOwners[0];
            staleSourceOwners[0] = new OwnerSectionCensusSnapshot(
                first.SectionId, first.SchemaVersion, first.Role, new object(),
                first.Cardinality, first.Revision);
            sourceBoundary.SetAdmissionEvidence(staleSourceOwners, sourceEpoch);
            Assert.That(application.TryCommitRestoredCandidateWithHostProjection(
                sourceSession, candidate, sourceProjection, candidateProjection,
                candidateBoundary, replacementOperation, out diagnostic), Is.False);
            Assert.That(application.ActiveSession, Is.SameAs(sourceSession),
                "A replacement must retain the current restored session when its fresh source census is stale.");
            Assert.That(application.TryGetPublishedHostProjection(sourceSession,
                out IDailyV1ApplicationHostProjection retainedSourceProjection), Is.True);
            Assert.That(retainedSourceProjection, Is.SameAs(sourceProjection));
            Assert.That(application.TryGetPublishedHostProjection(candidate, out _), Is.False);

            sourceBoundary.SetAdmissionEvidence(sourceOwners, sourceEpoch);
            Assert.That(candidateState.IsHealthyDailyOwnerThreadBoundary(), Is.True,
                "Rejecting stale source evidence must leave the private candidate healthy and idle.");
            Assert.That(candidateState.HasValidRestoredDailyBoundaryAdmission(candidateBoundary), Is.True,
                "Rejecting stale source evidence must leave the candidate continuation token and census unchanged.");
            Assert.That(candidateState.ActiveOperationCount, Is.EqualTo(0));
            Assert.That(application.TryCommitRestoredCandidateWithHostProjection(
                sourceSession, candidate, sourceProjection, candidateProjection,
                candidateBoundary, replacementOperation, out diagnostic), Is.True, diagnostic);
            Assert.That(application.ActiveSession, Is.SameAs(candidate));
            Assert.That(application.TryGetPublishedHostProjection(candidate,
                out IDailyV1ApplicationHostProjection publishedCandidateProjection), Is.True);
            Assert.That(publishedCandidateProjection, Is.SameAs(candidateProjection));
            Assert.That(application.TryGetPublishedHostProjection(sourceSession, out _), Is.False,
                "A successful replacement must retire the former host projection with its Core session.");
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
    public void ProductionGenesisPreparationAllocatesWorldIdentityInsideCore()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        var completedStages = new System.Collections.Generic.List<string>();
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);

        Assert.That(application.TryPrepareInitialGenesis(
            definitions, completedStages.Add,
            out DailyV1GenesisComposition composition,
            out DailyV1ActiveRuntimeSession candidate,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(composition, Is.Not.Null);
        Assert.That(candidate, Is.Not.Null);
        Assert.That(composition.WorldId, Is.SameAs(candidate.WorldId));
        Assert.That(composition.ExecutionBoundaryState.WorldId, Is.SameAs(composition.WorldId));
        Assert.That(completedStages, Is.Not.Empty);
        Assert.That(application.ActiveSession, Is.Null,
            "Core-owned genesis remains private until a real runtime boundary is bound and published.");
    }

    [Test]
    public void ApplicationPublishesGenesisAndExactHostProjectionTogether()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate,
            out string diagnostic), Is.True, diagnostic);
        EnsureRuntimeProtocolInventory(candidate.ExecutionBoundaryState);

        TestHostProjection projection = new TestHostProjection();
        ApplicationAdvanceBoundary hostBoundary = new ApplicationAdvanceBoundary(
            candidate.ExecutionBoundaryState, null);
        Assert.That(application.TryGetPublishedHostProjection(candidate, out _), Is.False,
            "A private prepared session cannot expose its host projection before publication.");
        Assert.That(application.TryPublishInitialWithHostProjection(
            candidate, projection, hostBoundary), Is.True);
        Assert.That(application.ActiveSession, Is.SameAs(candidate));
        Assert.That(application.TryGetPublishedHostProjection(candidate,
            out IDailyV1ApplicationHostProjection publishedProjection), Is.True);
        Assert.That(publishedProjection, Is.SameAs(projection));
    }

    [Test]
    public void PreparedNoSaveGenesisPublishesExactSessionOnceAndRejectsForeignBoundaryAtomically()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "prepared-genesis-publication-root",
            new MissingContinuationSlotOperations(),
            stageCompleted: null,
            logOutput: null,
            unityV1Identity: UnityIdentity());

        Assert.That(DailyV1Application.TryPrepareStart(
            definitions, host,
            out DailyV1ApplicationStartPreparation preparation,
            out P12AResult persistenceResult,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(preparation.Kind,
            Is.EqualTo(DailyV1ApplicationStartPreparationKind.GenesisPrepared));
        Assert.That(persistenceResult.Code, Is.EqualTo(P12AResultCode.NoSave));

        DailyV1Application application = preparation.Application;
        DailyV1ActiveRuntimeSession candidate = preparation.CoreSession;
        Assert.That(application.ActiveSession, Is.Null);
        EnsureRuntimeProtocolInventory(candidate.ExecutionBoundaryState);
        TestHostProjection projection = new TestHostProjection();
        ApplicationAdvanceBoundary foreignBoundary = new ApplicationAdvanceBoundary(
            new DailyV1ExecutionBoundaryState(), null);

        Assert.That(application.TryPublishPreparedInitialWithHostProjection(
            preparation, projection, foreignBoundary, out diagnostic), Is.False);
        Assert.That(application.ActiveSession, Is.Null,
            "A host boundary bound to a different execution state must not publish the private genesis.");
        Assert.That(application.TryGetPublishedHostProjection(candidate, out _), Is.False,
            "A rejected bind must not leave a registered projection behind.");

        ApplicationAdvanceBoundary exactBoundary = new ApplicationAdvanceBoundary(
            candidate.ExecutionBoundaryState, null);
        Assert.That(application.TryPublishPreparedInitialWithHostProjection(
            preparation, projection, exactBoundary, out diagnostic), Is.True, diagnostic);
        Assert.That(application.ActiveSession, Is.SameAs(candidate));
        Assert.That(application.TryGetPublishedHostProjection(candidate,
            out IDailyV1ApplicationHostProjection publishedProjection), Is.True);
        Assert.That(publishedProjection, Is.SameAs(projection));

        DailyV1Application otherApplication = new DailyV1Application();
        Assert.That(otherApplication.TryPublishPreparedInitialWithHostProjection(
            preparation, projection, exactBoundary, out diagnostic), Is.False,
            "A NoSave preparation cannot publish through another application instance.");
        Assert.That(application.TryPublishPreparedInitialWithHostProjection(
            preparation, projection, exactBoundary, out diagnostic), Is.False,
            "The exact start preparation can publish only once.");
        Assert.That(application.ActiveSession, Is.SameAs(candidate),
            "A duplicate publication attempt must preserve the already published Core session.");
    }

    [Test]
    public void ApplicationPublishesTheExactCoreGenesisOwnerGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        var completedStages = new System.Collections.Generic.List<string>();
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);

        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: completedStages.Add,
            out DailyV1GenesisComposition composition, out DailyV1ActiveRuntimeSession prepared, out string diagnostic), Is.True, diagnostic);
        Assert.That(application.ActiveSession, Is.Null,
            "Core genesis remains unpublished until its exact runtime advance authority is bound.");
        Assert.That(application.TryPublishInitial(prepared), Is.False,
            "A prepared Core session without a runtime boundary cannot become active.");
        var boundary = new ApplicationAdvanceBoundary(prepared.ExecutionBoundaryState, null);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(prepared, boundary), Is.True);
        Assert.That(application.TryPublishInitial(prepared), Is.True);

        DailyV1ActiveRuntimeSession session = application.ActiveSession;
        Assert.That(session, Is.Not.Null);
        Assert.That(session.Definitions, Is.SameAs(definitions));
        Assert.That(session.GenesisComposition, Is.SameAs(composition),
            "The published Core session retains the exact genesis roots rather than dropping or rebuilding them.");
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
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);

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
        Assert.That(application.TryBindRuntimeAdvanceBoundary(
            prepared, new ApplicationAdvanceBoundary(prepared.ExecutionBoundaryState, null)), Is.True);
        Assert.That(application.TryPublishInitial(prepared), Is.True,
            "The exact Core candidate may publish only after its runtime boundary is bound.");
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
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);
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
        Assert.That(application.TryBindRuntimeAdvanceBoundary(
            prepared, new ApplicationAdvanceBoundary(prepared.ExecutionBoundaryState, null)), Is.True);
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
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);

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
    public void CoreSelectedDailyExecutionAdapterIsCreatedOnceAndRetainedByItsSession()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts);
        hostPorts.ReturnSelectedExecutionHostPorts = true;

        Assert.That(session.RuntimeAdvanceBoundary.TryCreateSelectedDailyExecutionBoundary(
            out IDailyV1SelectedDailyExecutionBoundary first), Is.True);
        Assert.That(session.RuntimeAdvanceBoundary.TryCreateSelectedDailyExecutionBoundary(
            out IDailyV1SelectedDailyExecutionBoundary second), Is.True);

        Assert.That(second, Is.SameAs(first),
            "The exact session retains one Core-created execution adapter rather than rebuilding host authority for each day.");
        Assert.That(hostPorts.SelectedExecutionHostPortCreationCount, Is.EqualTo(1),
            "The host adapter factory is consulted once for the admitted session.");
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
            out SimulationRuntimeAdvanceFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(boundary.OperationEntered, Is.True);
        Assert.That(boundary.ActiveOperationCountAtStageCreation, Is.EqualTo(2),
            "Core owns both the outer DailyAdvance scope and its registered runtime operation while constructing the selected execution stage.");
        Assert.That(boundary.RequestedRuntimeProtocolOperationId,
            Is.EqualTo(DailyV1RuntimeProtocolOperationContracts.DailyAdvance),
            "The selected Core runtime names the registered P12 operation it owns.");
        Assert.That(boundary.SelectedBoundaryRequested, Is.True);
        Assert.That(boundary.LegacyAdvanceInvocationCount, Is.Zero,
            "The selected Core session cannot delegate a failed stage to the host day or intraday advance.");
        Assert.That(session.ExecutionBoundaryState.IsSelectedRuntimeCensusProtocolFaulted, Is.True);
        Assert.That(boundary.Faulted, Is.False,
            "Selected Core failures are latched without a host callback.");
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
    public void ApplicationCannotAdvanceBeforePublishingItsCoreSession()
    {
        DailyV1Application application = new DailyV1Application();

        Assert.That(application.TryAdvanceDay(
            out SimulationRuntimeAdvanceFailure dayFailure), Is.False);
        Assert.That(dayFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));

        Assert.That(application.TryAdvanceDays(
            1, out int daysAdvanced,
            out SimulationRuntimeAdvanceFailure batchFailure), Is.False);
        Assert.That(daysAdvanced, Is.Zero);
        Assert.That(batchFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(application.ActiveSession, Is.Null);
        Assert.That(application.ActiveOperationCount, Is.Zero);
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
            3, out int daysAdvanced,
            out SimulationRuntimeAdvanceFailure failure), Is.False);

        Assert.That(daysAdvanced, Is.Zero,
            "A failed first Core stage cannot claim any completed days.");
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(boundary.OperationEntered, Is.True);
        Assert.That(boundary.ActiveOperationCountAtStageCreation, Is.EqualTo(2));
        Assert.That(boundary.SelectedBoundaryRequested, Is.True);
        Assert.That(boundary.LegacyAdvanceInvocationCount, Is.Zero,
            "A selected multi-day Core session cannot delegate to the host day or intraday advance.");
        Assert.That(session.ExecutionBoundaryState.IsSelectedRuntimeCensusProtocolFaulted, Is.True);
        Assert.That(boundary.Faulted, Is.False,
            "Selected Core failures are latched without a host callback.");
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
    public void ApplicationBindsCoreBoundaryToOneExactSelectedHostPortSetBeforePublication()
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);
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
        DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary =
            session.RuntimeAdvanceBoundary as DailyV1CoreSelectedRuntimeAdvanceBoundary;
        Assert.That(coreBoundary, Is.Not.Null,
            "The session publishes its Core-owned boundary rather than the Unity host runtime.");
        Assert.That(coreBoundary.ExecutionState, Is.SameAs(session.ExecutionBoundaryState));
        Assert.That(coreBoundary.IsBoundTo(session.ExecutionBoundaryState, exact), Is.True);
        Assert.That(session.ExecutionBoundaryState.TryGetApplicationAdvanceAuthority(
            out DailyV1Application boundApplication,
            out DailyV1ActiveRuntimeSession boundSession), Is.True);
        Assert.That(boundApplication, Is.SameAs(application),
            "Application routing identity is owned by the Core execution state.");
        Assert.That(boundSession, Is.SameAs(session));
    }

    [Test]
    public void ApplicationRejectsHostPortsWhoseCensusSourceIsNotTheExactSessionBinding()
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession session, out string diagnostic), Is.True, diagnostic);

        object boundCensusSource = new object();
        Assert.That(session.ExecutionBoundaryState.TryBindSelectedRuntimeOwnerCensusSource(
            boundCensusSource,
            _ => DailyV1OwnerSectionCensusCaptureResult.Captured(
                Array.Empty<OwnerSectionCensusSnapshot>(),
                session.ExecutionBoundaryState.MutationEpochState.CurrentEpoch)), Is.True,
            "The candidate session first binds its exact census source under its Core state.");

        CensusIdentityApplicationAdvanceBoundary missingSource =
            new CensusIdentityApplicationAdvanceBoundary(
                session.ExecutionBoundaryState, null);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, missingSource), Is.False,
            "A selected host boundary must expose the stable owner-census source identity.");

        CensusIdentityApplicationAdvanceBoundary wrongSource =
            new CensusIdentityApplicationAdvanceBoundary(
                session.ExecutionBoundaryState, new object());
        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, wrongSource), Is.False,
            "A host adapter cannot publish a runtime while naming a different census source.");

        CensusIdentityApplicationAdvanceBoundary exactSource =
            new CensusIdentityApplicationAdvanceBoundary(
                session.ExecutionBoundaryState, boundCensusSource);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, exactSource), Is.True,
            "The shared application binds only the exact source that owns this session's P12 census.");
    }

    [Test]
    public void ApplicationBindsTheExactHostCensusSourceDuringRuntimeComposition()
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession session, out string diagnostic), Is.True, diagnostic);

        object censusSourceIdentity = new object();
        CensusIdentityApplicationAdvanceBoundary hostPorts =
            new CensusIdentityApplicationAdvanceBoundary(
                session.ExecutionBoundaryState, censusSourceIdentity);

        Assert.That(session.ExecutionBoundaryState.SelectedRuntimeOwnerCensusSourceIdentity, Is.Null,
            "The candidate is not pre-bound by a host during runtime initialization.");
        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, hostPorts), Is.True);
        Assert.That(session.ExecutionBoundaryState.SelectedRuntimeOwnerCensusSourceIdentity,
            Is.SameAs(censusSourceIdentity),
            "The Core application binds the exact host census source while composing the session boundary.");
        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, hostPorts), Is.True,
            "Retrying the same boundary retains the original exact census-source binding.");
    }

    [Test]
    public void CoreOwnedDailyAdvanceProtocolScopeClosesIdempotently()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts);
        DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary =
            session.RuntimeAdvanceBoundary as DailyV1CoreSelectedRuntimeAdvanceBoundary;

        Assert.That(coreBoundary.TryEnterDailyAdvanceOperation(
            out IDisposable operationScope), Is.True);
        Assert.That(session.ExecutionBoundaryState.ActiveOperationCount, Is.EqualTo(1));

        operationScope.Dispose();
        operationScope.Dispose();

        Assert.That(session.ExecutionBoundaryState.ActiveOperationCount, Is.Zero);
        Assert.That(hostPorts.Faulted, Is.False);
    }

    [Test]
    public void CoreOwnedDailyAdvanceProtocolScopeFaultsAndRemainsActiveWhenDisposedOffOwnerThread()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts);
        DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary =
            session.RuntimeAdvanceBoundary as DailyV1CoreSelectedRuntimeAdvanceBoundary;

        Assert.That(coreBoundary.TryEnterDailyAdvanceOperation(
            out IDisposable operationScope), Is.True);
        System.Threading.Thread disposer = new System.Threading.Thread(operationScope.Dispose);
        disposer.Start();
        disposer.Join();

        Assert.That(session.ExecutionBoundaryState.ActiveOperationCount, Is.EqualTo(1),
            "A scope disposed by another thread cannot make the owner-thread operation registry appear idle.");
        Assert.That(session.ExecutionBoundaryState.CurrentCaptureToken, Is.Null);
        Assert.That(session.ExecutionBoundaryState.IsSelectedRuntimeCensusProtocolFaulted, Is.True);
        Assert.That(hostPorts.Faulted, Is.False,
            "The selected Core boundary latches protocol faults without a Unity host callback.");
    }

    [Test]
    public void CoreAdvanceRequiresHealthyIdleP12OperationAdmissionBeforeOpeningItsScope()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts);
        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
        DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary =
            session.RuntimeAdvanceBoundary as DailyV1CoreSelectedRuntimeAdvanceBoundary;

        try
        {
            Assert.That(coreBoundary.TryEnterDailyAdvanceOperation(
                out IDisposable rejectedScope), Is.False);
            Assert.That(rejectedScope, Is.Null);
            Assert.That(state.ActiveOperationCount, Is.EqualTo(1));
            Assert.That(state.ActiveRuntimeOperationCount, Is.EqualTo(1));
            Assert.That(hostPorts.SelectedBoundaryRequested, Is.False);
            Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.True);
            Assert.That(hostPorts.Faulted, Is.False,
                "Core owns failure latching for the selected advance boundary.");
        }
        finally
        {
            Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
        }

        Assert.That(state.ActiveOperationCount, Is.Zero);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
    }

    [Test]
    public void CoreAdvanceRequiresBoundCensusProtocolBeforeOpeningItsScope()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts,
            markCensusProtocolReady: false);
        DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary =
            session.RuntimeAdvanceBoundary as DailyV1CoreSelectedRuntimeAdvanceBoundary;

        Assert.That(coreBoundary.TryEnterDailyAdvanceOperation(
            out IDisposable operationScope), Is.False);

        Assert.That(operationScope, Is.Null);
        Assert.That(session.ExecutionBoundaryState.ActiveOperationCount, Is.Zero);
        Assert.That(session.ExecutionBoundaryState.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(hostPorts.SelectedBoundaryRequested, Is.False);
        Assert.That(session.ExecutionBoundaryState.IsSelectedRuntimeCensusProtocolFaulted, Is.True);
        Assert.That(hostPorts.Faulted, Is.False,
            "Core owns failure latching for an unready selected census protocol.");
    }

    [Test]
    public void CoreAdvanceRejectsFaultedCensusProtocolBeforeOpeningItsScope()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts);
        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        state.MarkSelectedRuntimeCensusProtocolFaulted();
        DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary =
            session.RuntimeAdvanceBoundary as DailyV1CoreSelectedRuntimeAdvanceBoundary;

        Assert.That(coreBoundary.TryEnterDailyAdvanceOperation(
            out IDisposable operationScope), Is.False);

        Assert.That(operationScope, Is.Null);
        Assert.That(state.ActiveOperationCount, Is.Zero);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(hostPorts.SelectedBoundaryRequested, Is.False);
        Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.True);
        Assert.That(hostPorts.Faulted, Is.False,
            "A preexisting Core fault does not call into the selected host ports.");
    }

    [Test]
    public void FaultedSelectedCensusProtocolCannotPassCaptureScopeAssessment()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out _);
        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;

        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);
        Assert.That(state.TryAssessCaptureScope(
            DailyV1CaptureScopeAllowance.SessionReplacementValidation), Is.True);
        state.MarkSelectedRuntimeCensusProtocolFaulted();

        Assert.That(state.ActiveOperationCount, Is.Zero);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.False,
            "An idle but faulted Core census protocol cannot authorize admission evidence.");
        Assert.That(state.TryAssessCaptureScope(
            DailyV1CaptureScopeAllowance.SessionReplacementValidation), Is.False,
            "Detached restore cannot source a capture vector from a faulted active runtime.");
        Assert.That(state.TryAssessCaptureScope(
            DailyV1CaptureScopeAllowance.ContinuationSave), Is.False,
            "A faulted protocol cannot publish continuation bytes.");
        Assert.That(state.TryAssessCaptureScope(
            DailyV1CaptureScopeAllowance.DailyAdvance), Is.False,
            "A faulted protocol cannot authorize advance-finalization capture.");
    }

    [Test]
    public void ApplicationDoesNotAdvanceWhileRuntimeProtocolOperationIsActive()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts);
        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);

        try
        {
            Assert.That(application.TryAdvanceDay(
                out SimulationRuntimeAdvanceFailure failure), Is.False);
            Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress));
            Assert.That(session.SimulationTime.AbsoluteDay, Is.Zero,
                "The application session gate rejects overlapping runtime work before the day clock advances.");
            Assert.That(state.ActiveRuntimeOperationCount, Is.EqualTo(1));
            Assert.That(hostPorts.SelectedBoundaryRequested, Is.False);
            Assert.That(hostPorts.Faulted, Is.False);
        }
        finally
        {
            Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
        }

        Assert.That(state.ActiveOperationCount, Is.Zero);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
    }

    [Test]
    public void CoreSelectsAndRechecksExactP12CaptureScopeBeforeUsingHostCensusPort()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts);
        DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary =
            session.RuntimeAdvanceBoundary as DailyV1CoreSelectedRuntimeAdvanceBoundary;
        Assert.That(coreBoundary, Is.Not.Null);
        IReadOnlyList<OwnerSectionCensusSnapshot> completeHostVector =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        hostPorts.SetAdmissionEvidence(completeHostVector,
            mutationEpoch: session.ExecutionBoundaryState.MutationEpochState.CurrentEpoch);

        Assert.That(coreBoundary.TryCaptureCompletedDailyCaptureEvidence(
            out _, out _, out DailyCaptureEligibilityFailure failure), Is.False,
            "Core must reject completed-advance evidence unless its exact DailyAdvance scope is active.");
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress));
        Assert.That(hostPorts.OwnerCensusCaptureCount, Is.Zero,
            "A rejected Core scope cannot ask the host to read or reserve the P12 census.");

        Assert.That(coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
            sessionReplacement: false,
            out IReadOnlyList<OwnerSectionCensusSnapshot> admittedSections,
            out _, out failure), Is.True);
        Assert.That(hostPorts.LastCapturedOwnerSections, Has.Count.EqualTo(completeHostVector.Count),
            "The host port supplies the complete census; Core owns exact selected-source validation and filtering.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedP12FSectionIds.All(coreOwnedId =>
                hostPorts.LastCapturedOwnerSections.Any(section => section.SectionId == coreOwnedId)), Is.True,
            "The Unity host cannot omit rows before Core checks the bound owner identities.");
        OwnerSectionCensusSnapshot genealogySection =
            admittedSections.Single(section => section.SectionId == GenealogyCensusProvider.SectionId);
        Assert.That(genealogySection.OwnerInstanceIdentity,
            Is.SameAs(session.OwnerGraph.GenealogyStore),
            "The integrated Core recapture retains the exact Genealogy owner bound to the session graph.");
        Assert.That(admittedSections.Single(section => section.SectionId
                == ActorChoiceP11CensusProvider.SectionId).OwnerInstanceIdentity,
            Is.SameAs(new ActorChoiceP11CensusProvider(
                session.ExecutionBoundaryState.SelectedActorChoiceStore).GetCurrentCensus().OwnerInstanceIdentity),
            "The census row retains ActorChoiceStore's stable owner identity object.");
        Assert.That(admittedSections.Single(section => section.SectionId
                == ScheduledDirectiveCensusProvider.SectionId).OwnerInstanceIdentity,
            Is.SameAs(session.OwnerGraph.ScheduledDirectives));
        Assert.That(admittedSections.Single(section => section.SectionId
                == TravelPartyCensusProvider.SectionId).OwnerInstanceIdentity,
            Is.SameAs(session.OwnerGraph.TravelParties));
        Assert.That(admittedSections.Single(section => section.SectionId
                == ExpeditionCensusProvider.SectionId).OwnerInstanceIdentity,
            Is.SameAs(session.OwnerGraph.Expeditions));
        Assert.That(hostPorts.RequestedCaptureScopeAllowance,
            Is.EqualTo(DailyV1CaptureScopeAllowance.None));

        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        using (lease)
        {
            Assert.That(coreBoundary.TryCaptureCompletedDailyCaptureEvidence(
                out _, out _, out failure), Is.True);
            Assert.That(hostPorts.RequestedCaptureScopeAllowance,
                Is.EqualTo(DailyV1CaptureScopeAllowance.DailyAdvance));
        }
        Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
        Assert.That(coreBoundary.TryCaptureContinuationSaveEvidence(
            out _, out _, out failure), Is.True);
        Assert.That(hostPorts.RequestedCaptureScopeAllowance,
            Is.EqualTo(DailyV1CaptureScopeAllowance.ContinuationSave));
        Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);

        Assert.That(application.TryBeginExclusiveOperation(session, out var replacement), Is.True);
        using (replacement)
        {
            Assert.That(coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
                sessionReplacement: true, out _, out _, out failure), Is.True);
            Assert.That(hostPorts.RequestedCaptureScopeAllowance,
                Is.EqualTo(DailyV1CaptureScopeAllowance.SessionReplacementValidation));
        }

        Assert.That(hostPorts.OwnerCensusCaptureCount, Is.EqualTo(4));
    }

    [Test]
    public void CoreRejectsJusticeCensusThatDoesNotMatchItsExactOwnerAndProjection()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts);
        DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary =
            session.RuntimeAdvanceBoundary as DailyV1CoreSelectedRuntimeAdvanceBoundary;
        Assert.That(coreBoundary, Is.Not.Null);
        OwnerSectionCensusSnapshot[] baseline = CreateCoreBoundCompleteOwnerSectionVector(session);
        OwnerSectionCensusSnapshot baselineRecords = baseline.Single(section => string.Equals(
            section.SectionId, DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId, StringComparison.Ordinal));
        OwnerSectionCensusSnapshot baselineReceipts = baseline.Single(section => string.Equals(
            section.SectionId, DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId, StringComparison.Ordinal));
        object hostNeutralProjectionIdentity = new object();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateCoreOwnedJusticeSections(
                session.OwnerGraph, hostNeutralProjectionIdentity, 1L,
                out IReadOnlyList<OwnerSectionCensusWitness> nonzeroReceiptWitnesses,
                out string nonzeroReceiptDiagnostic),
            Is.False,
            "The selected UnityBootstrap-Daily-v1 profile admits only the exact-empty Justice P18 receipt sentinel.");
        Assert.That(nonzeroReceiptWitnesses, Is.Null);
        Assert.That(nonzeroReceiptDiagnostic, Does.Contain("exact-zero P18 receipt"));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateCoreOwnedJusticeSections(
                session.OwnerGraph, hostNeutralProjectionIdentity, baselineReceipts.Revision,
                out IReadOnlyList<OwnerSectionCensusWitness> coreJusticeWitnesses, out string witnessDiagnostic),
            Is.True, witnessDiagnostic);
        Assert.That(coreJusticeWitnesses, Has.Count.EqualTo(2));
        int baselineRecordsIndex = Array.FindIndex(baseline, section => string.Equals(
            section.SectionId, DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId, StringComparison.Ordinal));
        int baselineReceiptsIndex = Array.FindIndex(baseline, section => string.Equals(
            section.SectionId, DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId, StringComparison.Ordinal));
        Assert.That(baselineRecordsIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(baselineReceiptsIndex, Is.GreaterThanOrEqualTo(0));
        baseline[baselineRecordsIndex] = new OwnerSectionCensusSnapshot(
            coreJusticeWitnesses[0].SectionId,
            coreJusticeWitnesses[0].SchemaVersion,
            OwnerSectionRole.Required,
            coreJusticeWitnesses[0].OwnerInstanceIdentity,
            coreJusticeWitnesses[0].Cardinality,
            coreJusticeWitnesses[0].Revision);
        baseline[baselineReceiptsIndex] = new OwnerSectionCensusSnapshot(
            coreJusticeWitnesses[1].SectionId,
            coreJusticeWitnesses[1].SchemaVersion,
            OwnerSectionRole.Required,
            coreJusticeWitnesses[1].OwnerInstanceIdentity,
            coreJusticeWitnesses[1].Cardinality,
            coreJusticeWitnesses[1].Revision);
        baselineRecords = baseline[baselineRecordsIndex];
        baselineReceipts = baseline[baselineReceiptsIndex];
        Assert.That(baselineRecords.OwnerInstanceIdentity, Is.SameAs(baselineReceipts.OwnerInstanceIdentity));
        Assert.That(baselineRecords.OwnerInstanceIdentity, Is.SameAs(hostNeutralProjectionIdentity));
        Assert.That(baselineRecords.OwnerInstanceIdentity,
            Is.Not.SameAs(session.OwnerGraph.JusticeRecordsOwner),
            "The Core validator accepts one host-local opaque identity while deriving owner facts from the exact Core owner.");
        long sharedEpoch = session.ExecutionBoundaryState.MutationEpochState.CurrentEpoch;
        hostPorts.SetAdmissionEvidence(baseline, mutationEpoch: sharedEpoch);

        Assert.That(coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(false,
            out IReadOnlyList<OwnerSectionCensusSnapshot> admitted, out long admittedEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True);
        Assert.That(admittedEpoch, Is.EqualTo(sharedEpoch));
        Assert.That(admitted.Single(section => section.SectionId
                == DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId).Cardinality,
            Is.EqualTo(session.OwnerGraph.JusticeRecordsOwner.WantedRecords.Count
                + session.OwnerGraph.JusticeRecordsOwner.Sentences.Count));
        Assert.That(admitted.Single(section => section.SectionId
                == DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId).Revision,
            Is.EqualTo(session.OwnerGraph.JusticeRecordsOwner.Revision));

        void Reject(Action<List<OwnerSectionCensusSnapshot>> mutate, string reason)
        {
            List<OwnerSectionCensusSnapshot> candidate = baseline.ToList();
            mutate(candidate);
            hostPorts.SetAdmissionEvidence(candidate.ToArray(), mutationEpoch: sharedEpoch);
            bool captured = coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(false,
                out IReadOnlyList<OwnerSectionCensusSnapshot> rejected, out long rejectedEpoch, out failure);
            Assert.That(captured, Is.False, reason);
            Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OwnerCoverageIncomplete), reason);
            Assert.That(rejected, Is.Null, reason);
            Assert.That(rejectedEpoch, Is.Zero, reason);
        }

        void Replace(List<OwnerSectionCensusSnapshot> rows, string id,
            Func<OwnerSectionCensusSnapshot, OwnerSectionCensusSnapshot> rewrite)
        {
            int index = rows.FindIndex(section => string.Equals(section.SectionId, id, StringComparison.Ordinal));
            Assert.That(index, Is.GreaterThanOrEqualTo(0), id);
            rows[index] = rewrite(rows[index]);
        }

        OwnerSectionCensusSnapshot Change(OwnerSectionCensusSnapshot row,
            int? schema = null, OwnerSectionRole? role = null, object identity = null,
            bool replaceIdentity = false, int? cardinality = null, long? revision = null) =>
            new OwnerSectionCensusSnapshot(row.SectionId, schema ?? row.SchemaVersion,
                role ?? row.Role, replaceIdentity ? identity : row.OwnerInstanceIdentity,
                cardinality ?? row.Cardinality, revision ?? row.Revision);

        string recordsId = DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId;
        string receiptsId = DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId;
        Reject(rows => rows.RemoveAll(section => section.SectionId == recordsId), "Missing Justice records row.");
        Reject(rows => rows.Add(rows.Single(section => section.SectionId == recordsId)), "Duplicate Justice records row.");
        Reject(rows => Replace(rows, recordsId, row => Change(row, schema: row.SchemaVersion + 1)), "Wrong Justice schema.");
        Reject(rows => Replace(rows, recordsId, row => Change(row, role: OwnerSectionRole.ExplicitlyEmpty)), "Wrong Justice role.");
        Reject(rows => Replace(rows, recordsId, row => Change(row, identity: new object(), replaceIdentity: true)), "Wrong Justice owner identity.");
        Reject(rows => Replace(rows, recordsId, row => Change(row, cardinality: row.Cardinality + 1)), "Wrong Justice cardinality.");
        Reject(rows => Replace(rows, recordsId, row => Change(row, revision: row.Revision + 1L)), "Stale Justice revision.");
        Reject(rows => rows.RemoveAll(section => section.SectionId == receiptsId), "Missing Justice receipt row.");
        Reject(rows => rows.Add(rows.Single(section => section.SectionId == receiptsId)), "Duplicate Justice receipt row.");
        Reject(rows => Replace(rows, receiptsId, row => Change(row, identity: new object(), replaceIdentity: true)), "Wrong Justice receipt alias.");
        Reject(rows => Replace(rows, receiptsId, row => Change(row, cardinality: row.Cardinality + 1)), "Wrong Justice receipt cardinality.");
        Reject(rows => Replace(rows, receiptsId, row => Change(row, revision: row.Revision + 1L)), "Stale Justice receipt revision.");
    }

    [Test]
    public void CoreP12FCensusRejectsMissingDuplicateForeignAndStaleOwnerRows()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out _);
        OwnerSectionCensusSnapshot[] captured =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        ActorChoiceStore actorChoices = session.ExecutionBoundaryState.SelectedActorChoiceStore;

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedP12FSections(
            graph, actorChoices, captured, out IReadOnlyList<OwnerSectionCensusSnapshot> rebound,
            out string diagnostic), Is.True, diagnostic);
        foreach (string sectionId in new[]
        {
            ActorChoiceP11CensusProvider.SectionId,
            ScheduledDirectiveCensusProvider.SectionId,
            TravelPartyCensusProvider.SectionId,
            ExpeditionCensusProvider.SectionId,
            PoliticalKnowledgeStoreCensusProvider.SectionId
        })
        {
            OwnerSectionCensusSnapshot current = captured.Single(section => section.SectionId == sectionId);
            OwnerSectionCensusSnapshot exact = rebound.Single(section => section.SectionId == sectionId);
            Assert.That(exact.OwnerInstanceIdentity, Is.SameAs(current.OwnerInstanceIdentity), sectionId);
            Assert.That(exact.Cardinality, Is.EqualTo(current.Cardinality), sectionId);
            Assert.That(exact.Revision, Is.EqualTo(current.Revision), sectionId);
        }

        OwnerSectionCensusSnapshot politicalKnowledgeSection = captured.Single(section =>
            section.SectionId == PoliticalKnowledgeStoreCensusProvider.SectionId);
        Assert.That(politicalKnowledgeSection.OwnerInstanceIdentity,
            Is.SameAs(graph.PoliticalKnowledgeStore),
            "The P12-F census row is bound to the exact Core PoliticalKnowledge owner.");

        string targetSectionId = ActorChoiceP11CensusProvider.SectionId;
        OwnerSectionCensusSnapshot target = captured.Single(section => section.SectionId == targetSectionId);
        Assert.That(target.Cardinality, Is.Zero, "The empty ActorChoice owner remains a required exact witness.");

        OwnerSectionCensusSnapshot[] missing = captured
            .Where(section => section.SectionId != targetSectionId).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedP12FSections(
            graph, actorChoices, missing, out _, out _), Is.False,
            "A required Core P12-F row cannot be omitted.");

        OwnerSectionCensusSnapshot[] duplicate = captured.Concat(new[] { target }).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedP12FSections(
            graph, actorChoices, duplicate, out _, out _), Is.False,
            "A Core P12-F section identity cannot occur more than once.");

        ActorChoiceStore foreignStore = new ActorChoiceStore(new PersonStore());
        OwnerSectionCensusWitness foreignWitness =
            new ActorChoiceP11CensusProvider(foreignStore).GetCurrentCensus();
        OwnerSectionCensusSnapshot[] foreign = (OwnerSectionCensusSnapshot[])captured.Clone();
        int targetIndex = Array.FindIndex(foreign,
            section => section.SectionId == targetSectionId);
        foreign[targetIndex] = new OwnerSectionCensusSnapshot(
            target.SectionId, target.SchemaVersion, target.Role,
            foreignWitness.OwnerInstanceIdentity, foreignWitness.Cardinality, foreignWitness.Revision);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedP12FSections(
            graph, actorChoices, foreign, out _, out _), Is.False,
            "Matching empty values cannot substitute a foreign ActorChoice store.");

        OwnerSectionCensusSnapshot[] stale = (OwnerSectionCensusSnapshot[])captured.Clone();
        stale[targetIndex] = new OwnerSectionCensusSnapshot(
            target.SectionId, target.SchemaVersion, target.Role,
            target.OwnerInstanceIdentity, target.Cardinality, target.Revision + 1L);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedP12FSections(
            graph, actorChoices, stale, out _, out _), Is.False,
            "Stale revision is rejected even when the owner identity and cardinality match.");

        OwnerSectionCensusSnapshot[] foreignPoliticalKnowledge =
            (OwnerSectionCensusSnapshot[])captured.Clone();
        int politicalKnowledgeIndex = Array.FindIndex(foreignPoliticalKnowledge,
            section => section.SectionId == PoliticalKnowledgeStoreCensusProvider.SectionId);
        foreignPoliticalKnowledge[politicalKnowledgeIndex] = new OwnerSectionCensusSnapshot(
            politicalKnowledgeSection.SectionId,
            politicalKnowledgeSection.SchemaVersion,
            politicalKnowledgeSection.Role,
            new object(),
            politicalKnowledgeSection.Cardinality,
            politicalKnowledgeSection.Revision);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedP12FSections(
            graph, actorChoices, foreignPoliticalKnowledge, out _, out _), Is.False,
            "Matching PoliticalKnowledge census values cannot substitute a foreign owner identity.");

        OwnerSectionCensusSnapshot[] stalePoliticalKnowledge =
            (OwnerSectionCensusSnapshot[])captured.Clone();
        stalePoliticalKnowledge[politicalKnowledgeIndex] = new OwnerSectionCensusSnapshot(
            politicalKnowledgeSection.SectionId,
            politicalKnowledgeSection.SchemaVersion,
            politicalKnowledgeSection.Role,
            politicalKnowledgeSection.OwnerInstanceIdentity,
            politicalKnowledgeSection.Cardinality,
            politicalKnowledgeSection.Revision + 1L);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedP12FSections(
            graph, actorChoices, stalePoliticalKnowledge, out _, out _), Is.False,
            "A stale PoliticalKnowledge revision cannot be rebound as current Core owner evidence.");
    }

    [Test]
    public void CorePersonStoreCensusAliasesRequireSharedOwnerRevision()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out _);
        PersonStore personStore = session.OwnerGraph.PersonStore;
        OwnerSectionCensusWitness membership = new OwnerSectionCensusWitness(
            DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
            1, personStore, 0, 8L);
        OwnerSectionCensusWitness materializationBinding = new OwnerSectionCensusWitness(
            DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
            1, personStore, 0, 9L);

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.HasExactCorePersonStoreCensusAliasPair(
            personStore, membership, materializationBinding), Is.False,
            "The two PersonStore aliases cannot publish different local revisions.");

        OwnerSectionCensusWitness matchedMaterializationBinding = new OwnerSectionCensusWitness(
            DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
            1, personStore, 0, 8L);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.HasExactCorePersonStoreCensusAliasPair(
            personStore, membership, matchedMaterializationBinding), Is.True);

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.HasExactCorePersonStoreCensusAliasPair(
            new PersonStore(), membership, matchedMaterializationBinding), Is.False,
            "Matching revision values cannot substitute a different PersonStore instance.");
    }

    [Test]
    public void CoreP12FCensusComposesItsOwnRowsWithTheHostRemainder()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out _);
        OwnerSectionCensusSnapshot[] completeHostVector =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                completeHostVector, out _, out string noGraphDiagnostic), Is.False,
            "The legacy no-graph remover cannot discard a PoliticalDecision row without validating the exact owner.");
        Assert.That(noGraphDiagnostic, Does.Contain("exact owner graph"));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryGetExactCoreOwnedNpcKnowledgeAndPlanSectionIds(
                    completeHostVector, graph,
                    out IReadOnlyList<string> exactNpcKnowledgeAndPlanIds,
                    out string knowledgeAndPlanDiagnostic),
            Is.True, knowledgeAndPlanDiagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateCoreOwnedIdentitySpatialCensusSections(
                graph, out IReadOnlyList<OwnerSectionCensusSnapshot> activeIdentitySpatialRows,
                out string activeIdentitySpatialDiagnostic), Is.True, activeIdentitySpatialDiagnostic);
        string[] activeIdentitySpatialIds = activeIdentitySpatialRows
            .Select(section => section.SectionId).ToArray();
        HashSet<string> activeIdentitySpatialIdSet = new HashSet<string>(
            activeIdentitySpatialIds, StringComparer.Ordinal);
        int coreOwnedNonIdentitySpatialCount =
            DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedSelectedRuntimeSectionIds.Count
            - DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedIdentitySpatialSectionIds.Count
            + activeIdentitySpatialIds.Length;

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                completeHostVector,
                graph,
                out IReadOnlyList<OwnerSectionCensusSnapshot> hostRemainder,
                out string diagnostic), Is.True, diagnostic);
        Assert.That(hostRemainder.Count,
            Is.EqualTo(completeHostVector.Length
                - coreOwnedNonIdentitySpatialCount
                - exactNpcKnowledgeAndPlanIds.Count
                - graph.Cities.Count * 4));
        foreach (string sectionId in DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedSelectedRuntimeSectionIds)
        {
            bool hostRetainsUnavailableCoreSource =
                Array.IndexOf(DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedIdentitySpatialSectionIds.ToArray(),
                    sectionId) >= 0
                && !activeIdentitySpatialIdSet.Contains(sectionId);
            Assert.That(hostRemainder.Any(section => section.SectionId == sectionId),
                Is.EqualTo(hostRetainsUnavailableCoreSource), sectionId);
        }
        foreach (string sectionId in exactNpcKnowledgeAndPlanIds)
            Assert.That(hostRemainder.Any(section => section.SectionId == sectionId), Is.False, sectionId);
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        foreach (string sectionId in new[]
        {
            DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix
                + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(city.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix
                + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(city.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix
                + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(city.RuntimeId)
        })
            Assert.That(hostRemainder.Any(section => section.SectionId == sectionId), Is.False, sectionId);

        ActorChoiceStore actorChoices = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        OwnerSectionCensusSnapshot politicalKnowledgeRow = completeHostVector.Single(section =>
            section.SectionId == PoliticalKnowledgeStoreCensusProvider.SectionId);
        Assert.That(politicalKnowledgeRow.OwnerInstanceIdentity,
            Is.SameAs(graph.PoliticalKnowledgeStore));

        OwnerSectionCensusSnapshot[] wrongPoliticalKnowledgeIdentity = completeHostVector.Select(section =>
            section.SectionId == PoliticalKnowledgeStoreCensusProvider.SectionId
                ? new OwnerSectionCensusSnapshot(
                    section.SectionId, section.SchemaVersion, section.Role, new object(),
                    section.Cardinality, section.Revision)
                : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                wrongPoliticalKnowledgeIdentity, graph, out _, out _), Is.False,
            "The Core census transfer rejects a host row that substitutes a PoliticalKnowledge owner.");
        OwnerSectionCensusSnapshot[] stalePoliticalKnowledgeRevision = completeHostVector.Select(section =>
            section.SectionId == PoliticalKnowledgeStoreCensusProvider.SectionId
                ? new OwnerSectionCensusSnapshot(
                    section.SectionId, section.SchemaVersion, section.Role,
                    section.OwnerInstanceIdentity, section.Cardinality, section.Revision + 1L)
                : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                stalePoliticalKnowledgeRevision, graph, out _, out _), Is.False,
            "The Core census transfer rejects a stale PoliticalKnowledge revision.");

        OwnerSectionCensusSnapshot politicalDecision = completeHostVector.Single(section =>
            section.SectionId == PoliticalDecisionStoreCensusProvider.SectionId);
        Assert.That(politicalDecision.OwnerInstanceIdentity, Is.SameAs(graph.PoliticalDecisionStore));
        OwnerSectionCensusSnapshot[] wrongDecisionIdentity = completeHostVector.Select(section =>
            section.SectionId == PoliticalDecisionStoreCensusProvider.SectionId
                ? new OwnerSectionCensusSnapshot(section.SectionId, section.SchemaVersion,
                    section.Role, new object(), section.Cardinality, section.Revision)
                : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
            wrongDecisionIdentity, graph, out _, out _), Is.False,
            "The Core census transfer rejects a substitute PoliticalDecision owner.");
        OwnerSectionCensusSnapshot[] staleDecisionRevision = completeHostVector.Select(section =>
            section.SectionId == PoliticalDecisionStoreCensusProvider.SectionId
                ? new OwnerSectionCensusSnapshot(section.SectionId, section.SchemaVersion,
                    section.Role, section.OwnerInstanceIdentity, section.Cardinality, section.Revision + 1L)
                : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
            staleDecisionRevision, graph, out _, out _), Is.False,
            "The Core census transfer rejects a stale PoliticalDecision revision.");

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPersonStoreSections(
                graph, hostRemainder,
                out IReadOnlyList<OwnerSectionCensusSnapshot> personComposed,
                out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedIdentitySpatialSections(
                graph, personComposed,
                out IReadOnlyList<OwnerSectionCensusSnapshot> identitySpatialComposed,
                out diagnostic), Is.True, diagnostic);
        Assert.That(identitySpatialComposed.Count - personComposed.Count,
            Is.EqualTo(activeIdentitySpatialIds.Length));
        foreach (string sectionId in DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedIdentitySpatialSectionIds)
            Assert.That(identitySpatialComposed.Any(section => section.SectionId == sectionId), Is.True, sectionId);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryAddCoreOwnedCitySections(
                graph, identitySpatialComposed,
                out IReadOnlyList<OwnerSectionCensusSnapshot> cityComposed,
                out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPoliticalDecisionSection(
                graph, cityComposed,
                out IReadOnlyList<OwnerSectionCensusSnapshot> decisionComposed,
                out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoices, decisionComposed,
                out IReadOnlyList<OwnerSectionCensusSnapshot> p12fComposed,
                out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedCitySections(
                graph, p12fComposed,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeComposed,
                out diagnostic), Is.True, diagnostic);
        Assert.That(completeComposed.Select(section => section.SectionId),
            Is.EqualTo(completeHostVector.Select(section => section.SectionId)
                .OrderBy(sectionId => sectionId, StringComparer.Ordinal)));
        foreach (OwnerSectionCensusSnapshot original in completeHostVector)
        {
            OwnerSectionCensusSnapshot rebuilt = completeComposed.Single(section =>
                section.SectionId == original.SectionId);
            Assert.That(rebuilt.Role, Is.EqualTo(original.Role), original.SectionId);
            Assert.That(rebuilt.OwnerInstanceIdentity, Is.SameAs(original.OwnerInstanceIdentity), original.SectionId);
            Assert.That(rebuilt.Cardinality, Is.EqualTo(original.Cardinality), original.SectionId);
            Assert.That(rebuilt.Revision, Is.EqualTo(original.Revision), original.SectionId);
        }

        string hostOwnedIdentitySpatialId = DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedIdentitySpatialSectionIds
            .First(sectionId => !activeIdentitySpatialIdSet.Contains(sectionId));
        Assert.That(hostRemainder.Any(section => section.SectionId == hostOwnedIdentitySpatialId), Is.True,
            hostOwnedIdentitySpatialId);
        OwnerSectionCensusSnapshot[] missingHostIdentitySpatialRow = hostRemainder
            .Where(section => section.SectionId != hostOwnedIdentitySpatialId).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedIdentitySpatialSections(
                graph, missingHostIdentitySpatialRow,
                out _, out _), Is.False,
            "Core must fail closed when a fixed identity or spatial row remains host-owned but is omitted.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPersonStoreSections(
                graph, hostRemainder.Concat(hostRemainder.Take(1)).ToArray(), out _, out _), Is.False,
            "Core must fail closed when the host remainder duplicates a section.");

        foreach (string sectionId in activeIdentitySpatialIds)
        {
            OwnerSectionCensusSnapshot original = completeHostVector.Single(section =>
                section.SectionId == sectionId);
            OwnerSectionCensusSnapshot[] substitutedIdentity = completeHostVector.Select(section =>
                section.SectionId == sectionId
                    ? new OwnerSectionCensusSnapshot(section.SectionId, section.SchemaVersion,
                        section.Role, new object(), section.Cardinality, section.Revision)
                    : section).ToArray();
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                    substitutedIdentity, graph, out _, out _), Is.False,
                "Core rejects a substitute identity for " + sectionId + ".");
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedIdentitySpatialSections(
                    graph, substitutedIdentity, out _, out _), Is.False,
                "Core composition rejects a substitute identity for " + sectionId + ".");

            OwnerSectionCensusSnapshot[] staleRevision = completeHostVector.Select(section =>
                section.SectionId == sectionId
                    ? new OwnerSectionCensusSnapshot(section.SectionId, section.SchemaVersion,
                        section.Role, section.OwnerInstanceIdentity, section.Cardinality, section.Revision + 1L)
                    : section).ToArray();
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                    staleRevision, graph, out _, out _), Is.False,
                "Core rejects a stale local revision for " + sectionId + ".");
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedIdentitySpatialSections(
                    graph, staleRevision, out _, out _), Is.False,
                "Core composition rejects a stale local revision for " + sectionId + ".");

            OwnerSectionCensusSnapshot[] wrongSchema = completeHostVector.Select(section =>
                section.SectionId == sectionId
                    ? new OwnerSectionCensusSnapshot(section.SectionId, section.SchemaVersion + 1,
                        section.Role, section.OwnerInstanceIdentity, section.Cardinality, section.Revision)
                    : section).ToArray();
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                    wrongSchema, graph, out _, out _), Is.False,
                "Core rejects a substituted schema for " + sectionId + ".");
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedIdentitySpatialSections(
                    graph, wrongSchema, out _, out _), Is.False,
                "Core composition rejects a substituted schema for " + sectionId + ".");

            OwnerSectionCensusSnapshot[] wrongCardinality = completeHostVector.Select(section =>
                section.SectionId == sectionId
                    ? new OwnerSectionCensusSnapshot(section.SectionId, section.SchemaVersion,
                        section.Role, section.OwnerInstanceIdentity, section.Cardinality + 1, section.Revision)
                    : section).ToArray();
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                    wrongCardinality, graph, out _, out _), Is.False,
                "Core rejects a substituted cardinality for " + sectionId + ".");
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedIdentitySpatialSections(
                    graph, wrongCardinality, out _, out _), Is.False,
                "Core composition rejects a substituted cardinality for " + sectionId + ".");

            OwnerSectionRole wrongRole = original.Role == OwnerSectionRole.Required
                ? OwnerSectionRole.ExplicitlyEmpty
                : OwnerSectionRole.Required;
            OwnerSectionCensusSnapshot[] wrongRoleRows = completeHostVector.Select(section =>
                section.SectionId == sectionId
                    ? new OwnerSectionCensusSnapshot(section.SectionId, section.SchemaVersion,
                        wrongRole, section.OwnerInstanceIdentity, section.Cardinality, section.Revision)
                    : section).ToArray();
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                    wrongRoleRows, graph, out _, out _), Is.False,
                "Core rejects a substituted role for " + sectionId + ".");
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedIdentitySpatialSections(
                    graph, wrongRoleRows, out _, out _), Is.False,
                "Core composition rejects a substituted role for " + sectionId + ".");
        }

        OwnerSectionCensusSnapshot[] partialIdentitySpatial = completeHostVector.Where(section =>
            section.SectionId != activeIdentitySpatialIds[0]).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedIdentitySpatialSections(
                graph, partialIdentitySpatial, out _, out _), Is.False,
            "The Core composer rejects a host that omits only part of the identity/geography family.");

        OwnerSectionCensusSnapshot selectedCitySection = completeHostVector.First(section =>
            section.SectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                StringComparison.Ordinal));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedCitySections(
                graph, personComposed.Concat(new[] { selectedCitySection }).ToArray(), out _, out _), Is.False,
            "A host that returns only part of the selected City owner rows must be rejected.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedCitySections(
                graph, personComposed.Concat(completeHostVector.Where(section =>
                    section.SectionId.StartsWith(
                        DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                        StringComparison.Ordinal))).ToArray(), out _, out _), Is.False,
            "A host that returns only one selected City family must be rejected.");

        OwnerSectionCensusSnapshot originalCityPresence = completeHostVector.First(section =>
            section.SectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                StringComparison.Ordinal));
        OwnerSectionCensusSnapshot staleCityPresence = new OwnerSectionCensusSnapshot(
            originalCityPresence.SectionId,
            originalCityPresence.SchemaVersion,
            originalCityPresence.Role,
            originalCityPresence.OwnerInstanceIdentity,
            originalCityPresence.Cardinality,
            originalCityPresence.Revision + 1L);
        OwnerSectionCensusSnapshot[] staleCityHostVector = completeHostVector.Select(section =>
            section.SectionId == originalCityPresence.SectionId ? staleCityPresence : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                staleCityHostVector, graph,
                out IReadOnlyList<OwnerSectionCensusSnapshot> staleCityHostRemainder,
                out diagnostic), Is.True, diagnostic);
        Assert.That(staleCityHostRemainder.Any(section =>
            section.SectionId == staleCityPresence.SectionId), Is.True,
            "A stale captured City fact must remain available for completed-boundary rejection.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedCitySections(
                graph, staleCityHostRemainder,
                out IReadOnlyList<OwnerSectionCensusSnapshot> staleCityComposed,
                out diagnostic), Is.True, diagnostic);
        Assert.That(staleCityComposed.Single(section =>
            section.SectionId == staleCityPresence.SectionId), Is.SameAs(staleCityPresence),
            "Core must not replace stale host evidence with a fresh witness.");

        string compatibilityCityId = "compatibility-city";
        string compatibilityCityKey =
            P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(compatibilityCityId);
        object compatibilityPopulationOwner = new object();
        OwnerSectionCensusSnapshot[] compatibilityCityRows =
        {
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(compatibilityCityId),
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, new object(), 0, 0L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix + compatibilityCityKey,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, new object(), 0, 0L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix + compatibilityCityKey,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, compatibilityPopulationOwner, 0, 0L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix + compatibilityCityKey,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, compatibilityPopulationOwner, 0, 0L)
        };
        OwnerSectionCensusSnapshot[] mixedCityHostVector =
            completeHostVector.Concat(compatibilityCityRows).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                mixedCityHostVector, graph,
                out IReadOnlyList<OwnerSectionCensusSnapshot> mixedCityHostRemainder,
                out diagnostic), Is.True, diagnostic);
        foreach (OwnerSectionCensusSnapshot cityRow in mixedCityHostVector.Where(section =>
            DailyV1SelectedOwnerSectionCensusContract.CityPrefixes.Any(prefix =>
                section.SectionId.StartsWith(prefix, StringComparison.Ordinal))))
            Assert.That(mixedCityHostRemainder.Any(section =>
                section.SectionId == cityRow.SectionId), Is.True, cityRow.SectionId);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedCitySections(
                graph, mixedCityHostRemainder,
                out IReadOnlyList<OwnerSectionCensusSnapshot> mixedCityComposed,
                out diagnostic), Is.True, diagnostic);
        Assert.That(mixedCityComposed.Count,
            Is.EqualTo(mixedCityHostVector.Length
                - coreOwnedNonIdentitySpatialCount
                - exactNpcKnowledgeAndPlanIds.Count),
            "A mixed City roster keeps the complete host vector after the unrelated Core rows are removed.");

        OwnerSectionCensusSnapshot actorChoice = completeHostVector.Single(section =>
            section.SectionId == ActorChoiceP11CensusProvider.SectionId);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoices, cityComposed.Concat(new[] { actorChoice }).ToArray(), out _, out _), Is.False,
            "The host cannot reintroduce a P12-F section whose authority has moved to Core.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoices, cityComposed.Concat(new[] { politicalKnowledgeRow }).ToArray(), out _, out _), Is.False,
            "The host cannot reintroduce the Core-owned PoliticalKnowledge census row.");
        OwnerSectionCensusSnapshot personMembership = completeHostVector.Single(section =>
            section.SectionId == DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPersonStoreSections(
                graph, hostRemainder.Concat(new[] { personMembership }).ToArray(), out _, out _), Is.False,
            "The host cannot reintroduce a PersonStore alias section whose authority has moved to Core.");
    }

    [Test]
    public void CoreRuntimeAllocatorCensusIsBoundToTheExactExecutionRoot()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out _);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        RuntimeIdAllocator allocator = session.ExecutionBoundaryState.SelectedRuntimeIdAllocator;
        Assert.That(graph.RuntimeIdAllocator, Is.SameAs(allocator));

        OwnerSectionCensusSnapshot[] completeVector =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedRuntimeIdAllocatorSectionIds,
            Has.Count.EqualTo(3));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateCoreOwnedRuntimeIdAllocatorCensusSections(
                graph, out IReadOnlyList<OwnerSectionCensusSnapshot> coreRows,
                out string diagnostic), Is.True, diagnostic);
        object allocatorOwnerIdentity = coreRows[0].OwnerInstanceIdentity;
        Assert.That(allocatorOwnerIdentity, Is.Not.Null);

        foreach (OwnerSectionCensusSnapshot coreRow in coreRows)
        {
            Assert.That(coreRow.Role, Is.EqualTo(OwnerSectionRole.Required), coreRow.SectionId);
            Assert.That(coreRow.OwnerInstanceIdentity, Is.SameAs(allocatorOwnerIdentity), coreRow.SectionId);
            Assert.That(coreRow.Cardinality, Is.EqualTo(1), coreRow.SectionId);
            OwnerSectionCensusSnapshot captured = completeVector.Single(section =>
                section.SectionId == coreRow.SectionId);
            Assert.That(captured.OwnerInstanceIdentity, Is.SameAs(coreRow.OwnerInstanceIdentity), coreRow.SectionId);
            Assert.That(captured.Cardinality, Is.EqualTo(coreRow.Cardinality), coreRow.SectionId);
            Assert.That(captured.Revision, Is.EqualTo(coreRow.Revision), coreRow.SectionId);
        }

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedRuntimeIdAllocatorSections(
                graph, completeVector,
                out IReadOnlyList<OwnerSectionCensusSnapshot> composed,
                out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(composed), Is.True);

        HashSet<string> allocatorIds = new HashSet<string>(
            DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedRuntimeIdAllocatorSectionIds,
            StringComparer.Ordinal);
        OwnerSectionCensusSnapshot[] hostWithoutAllocatorRows = completeVector
            .Where(section => !allocatorIds.Contains(section.SectionId)).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedRuntimeIdAllocatorSections(
                graph, hostWithoutAllocatorRows, out IReadOnlyList<OwnerSectionCensusSnapshot> recomposed,
                out diagnostic), Is.True, diagnostic,
            "The Core application can rebuild the complete vector after removing all allocator facts from the host boundary.");
        Assert.That(recomposed.Select(section => section.SectionId),
            Is.EqualTo(composed.Select(section => section.SectionId)));

        OwnerSectionCensusSnapshot[] partialAllocatorRows = completeVector
            .Where(section => section.SectionId != RuntimeIdAllocatorCensusProvider.EventsSectionId)
            .ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedRuntimeIdAllocatorSections(
                graph, partialAllocatorRows, out _, out _), Is.False,
            "Partial host omission of the fixed allocator triplet fails closed.");

        foreach (OwnerSectionCensusSnapshot coreRow in coreRows)
        {
            OwnerSectionCensusSnapshot[] wrongOwner = completeVector.Select(section =>
                section.SectionId == coreRow.SectionId
                    ? new OwnerSectionCensusSnapshot(section.SectionId, section.SchemaVersion,
                        section.Role, new object(), section.Cardinality, section.Revision)
                    : section).ToArray();
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedRuntimeIdAllocatorSections(
                    graph, wrongOwner, out _, out _), Is.False,
                "A substitute allocator identity is rejected for " + coreRow.SectionId + ".");

            OwnerSectionCensusSnapshot[] staleRevision = completeVector.Select(section =>
                section.SectionId == coreRow.SectionId
                    ? new OwnerSectionCensusSnapshot(section.SectionId, section.SchemaVersion,
                        section.Role, section.OwnerInstanceIdentity, section.Cardinality, section.Revision + 1L)
                    : section).ToArray();
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedRuntimeIdAllocatorSections(
                    graph, staleRevision, out _, out _), Is.False,
                "A stale allocator counter revision is rejected for " + coreRow.SectionId + ".");
        }

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                completeVector, graph, out IReadOnlyList<OwnerSectionCensusSnapshot> hostRemainder,
                out diagnostic), Is.True, diagnostic);
        Assert.That(hostRemainder.Any(section => allocatorIds.Contains(section.SectionId)), Is.False);

        OwnerSectionCensusSnapshot[] beforeWrites = composed.Where(section =>
            allocatorIds.Contains(section.SectionId)).ToArray();
        allocator.AllocateEventId();
        allocator.AllocateDecisionId();
        allocator.AllocateTravelPartyId();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedRuntimeIdAllocatorSections(
                graph, hostWithoutAllocatorRows,
                out IReadOnlyList<OwnerSectionCensusSnapshot> afterWrites,
                out diagnostic), Is.True, diagnostic);
        foreach (OwnerSectionCensusSnapshot before in beforeWrites)
        {
            OwnerSectionCensusSnapshot after = afterWrites.Single(section =>
                section.SectionId == before.SectionId);
            Assert.That(after.OwnerInstanceIdentity, Is.SameAs(allocatorOwnerIdentity), before.SectionId);
            Assert.That(after.Revision, Is.GreaterThan(before.Revision), before.SectionId);
        }

        Assert.That(graph.TryCreateWithoutNpc(graph.Npcs[0],
            out DailyV1SelectedRuntimeOwnerGraph replacementGraph), Is.True);
        Assert.That(replacementGraph.RuntimeIdAllocator, Is.Null,
            "A newly composed membership graph starts without an execution-root binding.");
        DailyV1ExecutionBoundaryState executionState = session.ExecutionBoundaryState;
        IReadOnlyList<DailyV1ActorTurnRosterProjection> expectedRoster = executionState.SelectedRuntimeActorRoster;
        Assert.That(executionState.TryCreateSelectedRuntimeActorRosterWithoutMember(
            graph.Npcs[0].RuntimeId, replacementGraph,
            out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster), Is.True);
        Assert.That(executionState.TryReplaceSelectedRuntimeMembership(
            graph, replacementGraph, expectedRoster, replacementRoster), Is.True);
        Assert.That(replacementGraph.RuntimeIdAllocator, Is.SameAs(allocator));
        Assert.That(executionState.HasExactSelectedRuntimeExecutionRoots(
            replacementGraph,
            replacementGraph.PersonStore,
            executionState.SelectedActorChoiceStore,
            executionState.SelectedRandomSource,
            allocator,
            executionState.SelectedRecordSequence,
            executionState.SelectedDecisionStore), Is.True);
        Assert.That(executionState.HasExactSelectedRuntimeActorTurnRoots(
            replacementGraph,
            replacementGraph.PersonStore,
            executionState.SelectedActorChoiceStore,
            executionState.SelectedRandomSource), Is.True);
    }

    [Test]
    public void CoreP12FCensusRecomposesExactSelectedNpcInventoryAndMoneyAccountHostOmission()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out _);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        ActorChoiceStore actorChoices = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        OwnerSectionCensusSnapshot[] completeVector =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        Assert.That(graph.Npcs.Count, Is.GreaterThan(0));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryGetExactCoreOwnedNpcInventoryAndMoneyAccountSectionIds(
                    completeVector, graph,
                    out IReadOnlyList<string> coreOwnedNpcSectionIds,
                    out string npcOmissionDiagnostic),
            Is.True, npcOmissionDiagnostic);
        Assert.That(coreOwnedNpcSectionIds, Has.Count.EqualTo(graph.Npcs.Count * 2));
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            Assert.That(coreOwnedNpcSectionIds, Does.Contain(
                DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + npc.RuntimeId));
            Assert.That(coreOwnedNpcSectionIds, Does.Contain(
                DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + npc.RuntimeId));
        }
        string[] coreOwnedIds = DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedP12FSectionIds
            .Concat(DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedRuntimeIdAllocatorSectionIds)
            .Concat(coreOwnedNpcSectionIds)
            .ToArray();
        HashSet<string> coreOwnedIdSet = new HashSet<string>(coreOwnedIds, StringComparer.Ordinal);
        Assert.That(coreOwnedIds, Has.Length.EqualTo(8 + graph.Npcs.Count * 2));
        Assert.That(coreOwnedIdSet, Has.Count.EqualTo(coreOwnedIds.Length),
            "Core-owned P12-F, allocator, Inventory, and MoneyAccount rows have disjoint exact identities.");

        OwnerSectionCensusSnapshot[] hostRemainder = completeVector
            .Where(section => !coreOwnedIdSet.Contains(section.SectionId)).ToArray();
        Assert.That(hostRemainder, Has.Length.EqualTo(completeVector.Length - coreOwnedIds.Length));
        Assert.That(hostRemainder.Any(section => coreOwnedIdSet.Contains(section.SectionId)), Is.False);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoices, hostRemainder,
                out IReadOnlyList<OwnerSectionCensusSnapshot> composed,
                out string diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(composed), Is.True);
        Assert.That(composed.Select(section => section.SectionId),
            Is.EqualTo(completeVector.Select(section => section.SectionId)
                .OrderBy(sectionId => sectionId, StringComparer.Ordinal)));

        OwnerSectionCensusWitness[] currentCoreWitnesses =
        {
            new ActorChoiceP11CensusProvider(actorChoices).GetCurrentCensus(),
            new ScheduledDirectiveCensusProvider(graph.ScheduledDirectives).GetCurrentCensus(),
            new TravelPartyCensusProvider(graph.TravelParties).GetCurrentCensus(),
            new ExpeditionCensusProvider(graph.Expeditions).GetCurrentCensus(),
            new PoliticalKnowledgeStoreCensusProvider(graph.PoliticalKnowledgeStore).GetCurrentCensus()
        };
        foreach (OwnerSectionCensusWitness witness in currentCoreWitnesses)
        {
            OwnerSectionCensusSnapshot actual = composed.Single(section =>
                section.SectionId == witness.SectionId);
            Assert.That(actual.Role, Is.EqualTo(OwnerSectionRole.Required), witness.SectionId);
            Assert.That(actual.SchemaVersion, Is.EqualTo(witness.SchemaVersion), witness.SectionId);
            Assert.That(actual.OwnerInstanceIdentity, Is.SameAs(witness.OwnerInstanceIdentity), witness.SectionId);
            Assert.That(actual.Cardinality, Is.EqualTo(witness.Cardinality), witness.SectionId);
            Assert.That(actual.Revision, Is.EqualTo(witness.Revision), witness.SectionId);
        }

        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            OwnerSectionCensusWitness inventory = DailyV1NpcOwnerCensusProviders
                .CreateInventoryProviders(new[] { npc }).Single().GetCurrentCensus();
            OwnerSectionCensusWitness moneyAccount = DailyV1NpcOwnerCensusProviders
                .CreateMoneyAccountProviders(new[] { npc }).Single().GetCurrentCensus();
            foreach (OwnerSectionCensusWitness witness in new[] { inventory, moneyAccount })
            {
                OwnerSectionCensusSnapshot actual = composed.Single(section =>
                    section.SectionId == witness.SectionId);
                Assert.That(actual.Role, Is.EqualTo(OwnerSectionRole.Required), witness.SectionId);
                Assert.That(actual.SchemaVersion, Is.EqualTo(witness.SchemaVersion), witness.SectionId);
                Assert.That(actual.OwnerInstanceIdentity, Is.SameAs(witness.OwnerInstanceIdentity), witness.SectionId);
                Assert.That(actual.Cardinality, Is.EqualTo(witness.Cardinality), witness.SectionId);
                Assert.That(actual.Revision, Is.EqualTo(witness.Revision), witness.SectionId);
            }
        }

        OwnerSectionCensusSnapshot staleNpcInventory = completeVector
            .Where(section => section.SectionId == coreOwnedNpcSectionIds[0])
            .Select(section => new OwnerSectionCensusSnapshot(
                section.SectionId, section.SchemaVersion, section.Role,
                section.OwnerInstanceIdentity, section.Cardinality, section.Revision + 1L))
            .Single();
        OwnerSectionCensusSnapshot[] staleNpcVector = completeVector.Select(section =>
            section.SectionId == staleNpcInventory.SectionId ? staleNpcInventory : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryGetExactCoreOwnedNpcInventoryAndMoneyAccountSectionIds(
                    staleNpcVector, graph,
                    out IReadOnlyList<string> staleNpcOmissionIds,
                    out string staleNpcDiagnostic),
            Is.True, staleNpcDiagnostic);
        Assert.That(staleNpcOmissionIds, Is.Empty,
            "Stale owner evidence must stay in the host vector for the established P12 boundary checks.");

        foreach (string coreOwnedId in coreOwnedIds)
        {
            OwnerSectionCensusSnapshot hostSuppliedRow = completeVector.Single(section =>
                section.SectionId == coreOwnedId);
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                    graph, actorChoices, hostRemainder.Concat(new[] { hostSuppliedRow }).ToArray(),
                    out _, out _), Is.False,
                "A partial omission that leaves a host-supplied Core row is rejected: " + coreOwnedId);

            OwnerSectionCensusSnapshot substitutedRow = new OwnerSectionCensusSnapshot(
                hostSuppliedRow.SectionId, hostSuppliedRow.SchemaVersion, hostSuppliedRow.Role,
                new object(), hostSuppliedRow.Cardinality, hostSuppliedRow.Revision);
            Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                    graph, actorChoices, hostRemainder.Concat(new[] { substitutedRow }).ToArray(),
                    out _, out _), Is.False,
                "A substituted Core owner fact is rejected: " + coreOwnedId);
        }

        OwnerSectionCensusSnapshot omittedHostRow = hostRemainder[0];
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoices,
                hostRemainder.Where(section => section.SectionId != omittedHostRow.SectionId).ToArray(),
                out _, out _), Is.False,
            "Omitting any additional host-owned section fails the complete 89-family gate.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoices, hostRemainder.Concat(new[] { hostRemainder[0] }).ToArray(),
                out _, out _), Is.False,
            "Duplicating a host-owned section is rejected.");

        OwnerSectionCensusSnapshot beforeActorChoice = composed.Single(section =>
            section.SectionId == ActorChoiceP11CensusProvider.SectionId);
        Assert.That(actorChoices.TryCapture(
                "census-live-owner-input",
                new PersonId("person-census-live-owner"),
                "action.sell-goods",
                WorldCommandOrigin.LocalPlayer,
                WorldCommandAuthorityMode.Request,
                0L,
                out _,
                out ActorChoiceStoreFailureCode captureFailure), Is.True, captureFailure.ToString());
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoices, hostRemainder,
                out IReadOnlyList<OwnerSectionCensusSnapshot> afterMutation,
                out diagnostic), Is.True, diagnostic);
        OwnerSectionCensusSnapshot afterActorChoice = afterMutation.Single(section =>
            section.SectionId == ActorChoiceP11CensusProvider.SectionId);
        Assert.That(afterActorChoice.OwnerInstanceIdentity, Is.SameAs(beforeActorChoice.OwnerInstanceIdentity));
        Assert.That(afterActorChoice.Cardinality, Is.EqualTo(beforeActorChoice.Cardinality + 1));
        Assert.That(afterActorChoice.Revision, Is.EqualTo(beforeActorChoice.Revision + 1L));
    }

    [Test]
    public void CoreHostCensusRemainderOmitsOnlyExactNpcCityAndAllocatorRows()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session, out _);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        ActorChoiceStore actorChoiceStore = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        OwnerSectionCensusSnapshot[] completeVector =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
            completeVector), Is.True);

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryGetExactCoreOwnedNpcCensusFragmentSectionIds(
                completeVector, graph, out IReadOnlyList<string> exactNpcSectionIds, out string diagnostic),
            Is.True, diagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateCoreOwnedRuntimeIdAllocatorCensusSections(
                graph, out IReadOnlyList<OwnerSectionCensusSnapshot> allocatorRows, out diagnostic),
            Is.True, diagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryAddCoreOwnedCitySections(
                graph, Array.Empty<OwnerSectionCensusSnapshot>(),
                out IReadOnlyList<OwnerSectionCensusSnapshot> coreCityRows, out diagnostic),
            Is.True, diagnostic);
        Assert.That(graph.Cities, Is.Not.Empty);
        Assert.That(coreCityRows.Count, Is.EqualTo(graph.Cities.Count * 4),
            "The selected City census comprises all four bound owner families per City.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                completeVector, graph, actorChoiceStore, out IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedRemainder,
                out diagnostic),
            Is.True, diagnostic);

        HashSet<string> exactCoreIds = new HashSet<string>(exactNpcSectionIds, StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot allocatorRow in allocatorRows)
            Assert.That(exactCoreIds.Add(allocatorRow.SectionId), Is.True, allocatorRow.SectionId);
        foreach (OwnerSectionCensusSnapshot cityRow in coreCityRows)
            Assert.That(exactCoreIds.Add(cityRow.SectionId), Is.True, cityRow.SectionId);
        OwnerSectionCensusSnapshot politicalDecisionRow = completeVector.Single(section =>
            section.SectionId == PoliticalDecisionStoreCensusProvider.SectionId);
        Assert.That(exactCoreIds.Add(politicalDecisionRow.SectionId), Is.True);
        foreach (string p12fSectionId in DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedP12FSectionIds)
            Assert.That(exactCoreIds.Add(p12fSectionId), Is.True, p12fSectionId);
        OwnerSectionCensusSnapshot[] expectedRemainder = completeVector
            .Where(section => !exactCoreIds.Contains(section.SectionId)).ToArray();
        Assert.That(hostOwnedRemainder.Select(section => section.SectionId),
            Is.EqualTo(expectedRemainder.Select(section => section.SectionId)));
        Assert.That(hostOwnedRemainder.Any(section =>
            section.SectionId == PoliticalDecisionStoreCensusProvider.SectionId), Is.False);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPoliticalDecisionSection(
            graph, hostOwnedRemainder, out IReadOnlyList<OwnerSectionCensusSnapshot> recomposedDecision,
            out diagnostic), Is.True, diagnostic);
        OwnerSectionCensusSnapshot recomposedDecisionRow = recomposedDecision.Single(section =>
            section.SectionId == PoliticalDecisionStoreCensusProvider.SectionId);
        Assert.That(recomposedDecisionRow.OwnerInstanceIdentity, Is.SameAs(graph.PoliticalDecisionStore));
        Assert.That(recomposedDecisionRow.Cardinality, Is.EqualTo(graph.PoliticalDecisionStore.Count));
        Assert.That(recomposedDecisionRow.Revision, Is.EqualTo(graph.PoliticalDecisionStore.Revision));
        for (int index = 0; index < expectedRemainder.Length; index++)
            Assert.That(hostOwnedRemainder[index], Is.SameAs(expectedRemainder[index]));

        string staleInventoryId = DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix
            + graph.Npcs[0].RuntimeId;
        OwnerSectionCensusSnapshot staleInventory = completeVector.Single(section =>
            section.SectionId == staleInventoryId);
        OwnerSectionCensusSnapshot staleVectorEntry = new OwnerSectionCensusSnapshot(
            staleInventory.SectionId, staleInventory.SchemaVersion, staleInventory.Role,
            staleInventory.OwnerInstanceIdentity, staleInventory.Cardinality, staleInventory.Revision + 1L);
        OwnerSectionCensusSnapshot[] staleVector = completeVector.Select(section =>
            section.SectionId == staleInventoryId ? staleVectorEntry : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                staleVector, graph, actorChoiceStore, out IReadOnlyList<OwnerSectionCensusSnapshot> staleHostRemainder,
                out diagnostic),
            Is.True, diagnostic);
        Assert.That(staleHostRemainder.Single(section => section.SectionId == staleInventoryId),
            Is.SameAs(staleVectorEntry),
            "Stale NPC facts remain host-owned for the established P12 validation path.");

        OwnerSectionCensusSnapshot staleAllocator = allocatorRows[0];
        OwnerSectionCensusSnapshot staleAllocatorEntry = new OwnerSectionCensusSnapshot(
            staleAllocator.SectionId, staleAllocator.SchemaVersion, staleAllocator.Role,
            staleAllocator.OwnerInstanceIdentity, staleAllocator.Cardinality, staleAllocator.Revision + 1L);
        OwnerSectionCensusSnapshot[] staleAllocatorVector = completeVector.Select(section =>
            section.SectionId == staleAllocator.SectionId ? staleAllocatorEntry : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                staleAllocatorVector, graph, actorChoiceStore, out IReadOnlyList<OwnerSectionCensusSnapshot> staleAllocatorRemainder,
                out diagnostic),
            Is.True, diagnostic);
        foreach (OwnerSectionCensusSnapshot allocatorRow in allocatorRows)
            Assert.That(staleAllocatorRemainder.Single(section =>
                section.SectionId == allocatorRow.SectionId), Is.Not.Null, allocatorRow.SectionId);

        OwnerSectionCensusSnapshot staleDecisionRow = new OwnerSectionCensusSnapshot(
            politicalDecisionRow.SectionId, politicalDecisionRow.SchemaVersion, politicalDecisionRow.Role,
            politicalDecisionRow.OwnerInstanceIdentity, politicalDecisionRow.Cardinality,
            politicalDecisionRow.Revision + 1L);
        OwnerSectionCensusSnapshot[] staleDecisionVector = completeVector.Select(section =>
            section.SectionId == politicalDecisionRow.SectionId ? staleDecisionRow : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
            staleDecisionVector, graph, actorChoiceStore,
            out IReadOnlyList<OwnerSectionCensusSnapshot> staleDecisionRemainder, out diagnostic),
            Is.True, diagnostic);
        Assert.That(staleDecisionRemainder.Single(section =>
            section.SectionId == politicalDecisionRow.SectionId), Is.SameAs(staleDecisionRow));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPoliticalDecisionSection(
            graph, staleDecisionRemainder, out _, out _), Is.False,
            "A stale host row cannot be replaced by the Core owner witness.");

        OwnerSectionCensusSnapshot compatibilityInventory = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "compatibility-npc",
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, new object(), 0, 0L);
        OwnerSectionCensusSnapshot compatibilityMoneyAccount = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + "compatibility-npc",
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, new object(), 1, 0L);
        OwnerSectionCensusSnapshot[] mixedRosterVector = completeVector.Concat(new[]
        {
            compatibilityInventory,
            compatibilityMoneyAccount
        }).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                mixedRosterVector, graph, actorChoiceStore, out IReadOnlyList<OwnerSectionCensusSnapshot> mixedRosterRemainder,
                out diagnostic),
            Is.True, diagnostic);
        Assert.That(mixedRosterRemainder.Any(section =>
                ReferenceEquals(section, compatibilityInventory)), Is.True);
        Assert.That(mixedRosterRemainder.Any(section =>
                ReferenceEquals(section, compatibilityMoneyAccount)), Is.True);
        foreach (OwnerSectionCensusSnapshot coreNpcRow in completeVector.Where(section =>
            section.SectionId.StartsWith(DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix,
                StringComparison.Ordinal)
            || section.SectionId.StartsWith(DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix,
                StringComparison.Ordinal)))
            Assert.That(mixedRosterRemainder.Any(section => ReferenceEquals(section, coreNpcRow)), Is.True,
                "A mixed roster keeps every host NPC row on the compatibility path.");

        OwnerSectionCensusSnapshot missingCityRow = coreCityRows[0];
        OwnerSectionCensusSnapshot[] partialCityVector = completeVector
            .Where(section => section.SectionId != missingCityRow.SectionId).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                partialCityVector, graph, actorChoiceStore, out IReadOnlyList<OwnerSectionCensusSnapshot> partialCityRemainder,
                out diagnostic),
            Is.True, diagnostic);
        foreach (OwnerSectionCensusSnapshot cityRow in partialCityVector.Where(section =>
            DailyV1SelectedOwnerSectionCensusContract.CityPrefixes.Any(prefix =>
                section.SectionId.StartsWith(prefix, StringComparison.Ordinal))))
            Assert.That(partialCityRemainder.Single(section => section.SectionId == cityRow.SectionId),
                Is.SameAs(cityRow),
                "A partial selected City vector retains each captured City row for downstream P12 classification.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryAddCoreOwnedCitySections(
                graph, partialCityRemainder, out _, out diagnostic),
            Is.False, diagnostic,
            "The next composition gate rejects a partial selected City owner set.");

        OwnerSectionCensusSnapshot staleCityRow = coreCityRows[0];
        OwnerSectionCensusSnapshot staleCityEntry = new OwnerSectionCensusSnapshot(
            staleCityRow.SectionId, staleCityRow.SchemaVersion, staleCityRow.Role,
            staleCityRow.OwnerInstanceIdentity, staleCityRow.Cardinality, staleCityRow.Revision + 1L);
        OwnerSectionCensusSnapshot[] staleCityVector = completeVector.Select(section =>
            section.SectionId == staleCityRow.SectionId ? staleCityEntry : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                staleCityVector, graph, actorChoiceStore, out IReadOnlyList<OwnerSectionCensusSnapshot> staleCityRemainder,
                out diagnostic),
            Is.True, diagnostic);
        foreach (OwnerSectionCensusSnapshot cityRow in staleCityVector.Where(section =>
            DailyV1SelectedOwnerSectionCensusContract.CityPrefixes.Any(prefix =>
                section.SectionId.StartsWith(prefix, StringComparison.Ordinal))))
            Assert.That(staleCityRemainder.Single(section => section.SectionId == cityRow.SectionId),
                Is.SameAs(cityRow),
                "A stale selected City witness keeps every City row on the established P12 validation path.");

        const string compatibilityCityId = "compatibility-city";
        string compatibilityCityKey =
            P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(compatibilityCityId);
        object compatibilityPopulationOwner = new object();
        OwnerSectionCensusSnapshot[] compatibilityCityRows =
        {
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(compatibilityCityId),
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, new object(), 0, 0L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix + compatibilityCityKey,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, new object(), 0, 0L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix + compatibilityCityKey,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, compatibilityPopulationOwner, 0, 0L),
            new OwnerSectionCensusSnapshot(
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix + compatibilityCityKey,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                OwnerSectionRole.Required, compatibilityPopulationOwner, 0, 0L)
        };
        OwnerSectionCensusSnapshot[] mixedCityVector = completeVector.Concat(compatibilityCityRows).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                mixedCityVector, graph, actorChoiceStore, out IReadOnlyList<OwnerSectionCensusSnapshot> mixedCityRemainder,
                out diagnostic),
            Is.True, diagnostic);
        foreach (OwnerSectionCensusSnapshot cityRow in mixedCityVector.Where(section =>
            DailyV1SelectedOwnerSectionCensusContract.CityPrefixes.Any(prefix =>
                section.SectionId.StartsWith(prefix, StringComparison.Ordinal))))
            Assert.That(mixedCityRemainder.Single(section => section.SectionId == cityRow.SectionId),
                Is.SameAs(cityRow),
                "An additional City roster preserves every selected and compatibility City witness.");
    }

    [Test]
    public void CoreHostCensusRemainderOmitsP12FOnlyAsExactCompleteSet()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session, out _);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        ActorChoiceStore actorChoiceStore = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        OwnerSectionCensusSnapshot[] completeVector =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        OwnerSectionCensusSnapshot[] p12fRows = completeVector
            .Where(section => DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedP12FSectionIds
                .Contains(section.SectionId))
            .ToArray();
        Assert.That(p12fRows, Has.Length.EqualTo(5));

        OwnerSectionCensusSnapshot actorChoiceRow = p12fRows.Single(section =>
            section.SectionId == ActorChoiceP11CensusProvider.SectionId);
        Assert.That(actorChoiceRow.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(actorChoiceRow.Cardinality, Is.EqualTo(0),
            "A valid zero-cardinality ActorChoice owner remains a required census row.");
        Assert.That(actorChoiceRow.OwnerInstanceIdentity,
            Is.SameAs(new ActorChoiceP11CensusProvider(actorChoiceStore).GetCurrentCensus().OwnerInstanceIdentity));

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                completeVector, graph, actorChoiceStore,
                out IReadOnlyList<OwnerSectionCensusSnapshot> remainder, out string diagnostic),
            Is.True, diagnostic);
        foreach (OwnerSectionCensusSnapshot row in p12fRows)
            Assert.That(remainder.Any(section => section.SectionId == row.SectionId), Is.False, row.SectionId);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryAddCoreOwnedCitySections(
                graph, remainder,
                out IReadOnlyList<OwnerSectionCensusSnapshot> remainderWithCities,
                out diagnostic),
            Is.True, diagnostic);

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPoliticalDecisionSection(
                graph, remainderWithCities,
                out IReadOnlyList<OwnerSectionCensusSnapshot> remainderWithPoliticalDecision,
                out diagnostic), Is.True, diagnostic);

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoiceStore, remainderWithPoliticalDecision,
                out IReadOnlyList<OwnerSectionCensusSnapshot> composed, out diagnostic),
            Is.True, diagnostic);
        OwnerSectionCensusSnapshot recomposedActorChoice = composed.Single(section =>
            section.SectionId == ActorChoiceP11CensusProvider.SectionId);
        Assert.That(recomposedActorChoice.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(recomposedActorChoice.Cardinality, Is.EqualTo(0));
        Assert.That(recomposedActorChoice.OwnerInstanceIdentity,
            Is.SameAs(actorChoiceRow.OwnerInstanceIdentity));
    }

    [Test]
    public void CoreHostCensusRemainderPreservesPartialStaleAndForeignP12FRows()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session, out _);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        ActorChoiceStore actorChoiceStore = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        OwnerSectionCensusSnapshot[] completeVector =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        OwnerSectionCensusSnapshot[] p12fRows = completeVector
            .Where(section => DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedP12FSectionIds
                .Contains(section.SectionId))
            .ToArray();
        Assert.That(p12fRows, Has.Length.EqualTo(5));

        OwnerSectionCensusSnapshot[] partialSource = completeVector
            .Where(section => section.SectionId != p12fRows[0].SectionId).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                partialSource, graph, actorChoiceStore,
                out IReadOnlyList<OwnerSectionCensusSnapshot> partialRemainder, out string diagnostic),
            Is.True, diagnostic);
        foreach (OwnerSectionCensusSnapshot row in p12fRows.Where(section =>
            section.SectionId != p12fRows[0].SectionId))
            Assert.That(partialRemainder.Single(section => section.SectionId == row.SectionId),
                Is.SameAs(row));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPoliticalDecisionSection(
                graph, partialRemainder,
                out IReadOnlyList<OwnerSectionCensusSnapshot> partialWithPoliticalDecision,
                out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoiceStore, partialWithPoliticalDecision, out _, out diagnostic),
            Is.False, diagnostic,
            "The existing composition gate rejects a partial P12-F set.");

        OwnerSectionCensusSnapshot[] absentP12FSource = completeVector
            .Where(section => !DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedP12FSectionIds
                .Contains(section.SectionId)).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                absentP12FSource, graph, actorChoiceStore, out _, out diagnostic),
            Is.False, diagnostic,
            "Projection fails closed when the source omitted every P12-F row.");

        OwnerSectionCensusSnapshot staleRow = p12fRows[0];
        OwnerSectionCensusSnapshot staleEntry = new OwnerSectionCensusSnapshot(
            staleRow.SectionId, staleRow.SchemaVersion, staleRow.Role,
            staleRow.OwnerInstanceIdentity, staleRow.Cardinality, staleRow.Revision + 1L);
        OwnerSectionCensusSnapshot[] staleSource = completeVector.Select(section =>
            section.SectionId == staleRow.SectionId ? staleEntry : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                staleSource, graph, actorChoiceStore,
                out IReadOnlyList<OwnerSectionCensusSnapshot> staleRemainder, out diagnostic),
            Is.True, diagnostic);
        foreach (OwnerSectionCensusSnapshot row in staleSource.Where(section =>
            DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedP12FSectionIds.Contains(section.SectionId)))
            Assert.That(staleRemainder.Single(section => section.SectionId == row.SectionId),
                Is.SameAs(row));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPoliticalDecisionSection(
                graph, staleRemainder,
                out IReadOnlyList<OwnerSectionCensusSnapshot> staleWithPoliticalDecision,
                out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoiceStore, staleWithPoliticalDecision, out _, out diagnostic),
            Is.False, diagnostic,
            "The existing exact-witness gate rejects a stale P12-F revision.");

        OwnerSectionCensusSnapshot foreignRow = p12fRows.Single(section =>
            section.SectionId == TravelPartyCensusProvider.SectionId);
        OwnerSectionCensusSnapshot foreignEntry = new OwnerSectionCensusSnapshot(
            foreignRow.SectionId, foreignRow.SchemaVersion, foreignRow.Role,
            new object(), foreignRow.Cardinality, foreignRow.Revision);
        OwnerSectionCensusSnapshot[] foreignSource = completeVector.Select(section =>
            section.SectionId == foreignRow.SectionId ? foreignEntry : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                foreignSource, graph, actorChoiceStore,
                out IReadOnlyList<OwnerSectionCensusSnapshot> foreignRemainder, out diagnostic),
            Is.True, diagnostic);
        foreach (OwnerSectionCensusSnapshot row in foreignSource.Where(section =>
            DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedP12FSectionIds.Contains(section.SectionId)))
            Assert.That(foreignRemainder.Single(section => section.SectionId == row.SectionId),
                Is.SameAs(row));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPoliticalDecisionSection(
                graph, foreignRemainder,
                out IReadOnlyList<OwnerSectionCensusSnapshot> foreignWithPoliticalDecision,
                out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoiceStore, foreignWithPoliticalDecision, out _, out diagnostic),
            Is.False, diagnostic,
            "The existing exact-witness gate rejects a foreign P12-F owner identity.");
    }
    [Test]
    public void CoreBoundCensusCaptureOwnsHostRemainderSelectionAfterEpochValidation()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session, out _, bindAdvanceBoundary: false);
        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        OwnerSectionCensusSnapshot[] completeHostVector =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryGetExactCoreOwnedNpcCensusFragmentSectionIds(
                completeHostVector, graph, out IReadOnlyList<string> exactNpcIds, out string diagnostic),
            Is.True, diagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryCreateCoreOwnedRuntimeIdAllocatorCensusSections(
                graph, out IReadOnlyList<OwnerSectionCensusSnapshot> allocatorRows, out diagnostic),
            Is.True, diagnostic);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryAddCoreOwnedCitySections(
                graph, Array.Empty<OwnerSectionCensusSnapshot>(),
                out IReadOnlyList<OwnerSectionCensusSnapshot> coreCityRows, out diagnostic),
            Is.True, diagnostic);

        HashSet<string> exactCoreIds = new HashSet<string>(exactNpcIds, StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot allocatorRow in allocatorRows)
            Assert.That(exactCoreIds.Add(allocatorRow.SectionId), Is.True, allocatorRow.SectionId);
        foreach (OwnerSectionCensusSnapshot cityRow in coreCityRows)
            Assert.That(exactCoreIds.Add(cityRow.SectionId), Is.True, cityRow.SectionId);
        Assert.That(exactCoreIds.Add(PoliticalDecisionStoreCensusProvider.SectionId), Is.True);
        foreach (string p12fSectionId in DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedP12FSectionIds)
            Assert.That(exactCoreIds.Add(p12fSectionId), Is.True, p12fSectionId);
        OwnerSectionCensusSnapshot[] expectedHostRemainder = completeHostVector
            .Where(section => !exactCoreIds.Contains(section.SectionId)).ToArray();

        bool sourceCalled = false;
        long currentEpoch = state.MutationEpochState.CurrentEpoch;
        Assert.That(state.TryBindSelectedRuntimeOwnerCensusSource(new object(), allowedOuterScope =>
        {
            sourceCalled = true;
            Assert.That(allowedOuterScope, Is.EqualTo(DailyV1CaptureScopeAllowance.None));
            return DailyV1OwnerSectionCensusCaptureResult.Captured(
                Array.AsReadOnly(completeHostVector), currentEpoch);
        }), Is.True);

        Assert.That(state.TryCaptureBoundOwnerSectionCensus(
                null, DailyV1CaptureScopeAllowance.None,
                out IReadOnlyList<OwnerSectionCensusSnapshot> hostRemainder,
                out long capturedEpoch, out DailyCaptureEligibilityFailure failure),
            Is.True, failure.ToString());
        Assert.That(sourceCalled, Is.True);
        Assert.That(capturedEpoch, Is.EqualTo(currentEpoch));
        Assert.That(hostRemainder.Select(section => section.SectionId),
            Is.EqualTo(expectedHostRemainder.Select(section => section.SectionId)));
        for (int index = 0; index < expectedHostRemainder.Length; index++)
            Assert.That(hostRemainder[index], Is.SameAs(expectedHostRemainder[index]));
    }

    [Test]
    public void CoreBoundCensusCaptureRejectsEpochDriftBeforeRemainderProjection()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session, out _, bindAdvanceBoundary: false);
        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        OwnerSectionCensusSnapshot[] completeHostVector =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        OwnerSectionCensusSnapshot[] duplicateHostVector = completeHostVector
            .Concat(new[] { completeHostVector[0] }).ToArray();
        long wrongEpoch = checked(state.MutationEpochState.CurrentEpoch + 1L);
        Assert.That(state.TryBindSelectedRuntimeOwnerCensusSource(new object(), _ =>
            DailyV1OwnerSectionCensusCaptureResult.Captured(
                Array.AsReadOnly(duplicateHostVector), wrongEpoch)), Is.True);

        // The duplicate row makes remainder projection fail with OwnerCoverageIncomplete.
        // The epoch mismatch must be classified first as OperationInProgress.
        Assert.That(state.TryCaptureBoundOwnerSectionCensus(
                null, DailyV1CaptureScopeAllowance.None,
                out IReadOnlyList<OwnerSectionCensusSnapshot> hostRemainder,
                out long capturedEpoch, out DailyCaptureEligibilityFailure failure),
            Is.False);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress));
        Assert.That(hostRemainder, Is.Null);
        Assert.That(capturedEpoch, Is.EqualTo(0L));
    }

    [Test]
    public void CoreSharedDailyOwnerVectorComposerPreservesCompleteHostCensusAndJusticeAlias()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session, out _);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        ActorChoiceStore actorChoices = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        Assert.That(graph.RecordSequence,
            Is.SameAs(session.ExecutionBoundaryState.SelectedRecordSequence));
        CrimeSocialAppraisalWorldState crimeSocial = graph.P12EOwners?.CrimeSocialAppraisal;
        Assert.That(crimeSocial, Is.Not.Null);
        Assert.That(crimeSocial.SimulationTime,
            Is.SameAs(session.ExecutionBoundaryState.SimulationTime));
        OwnerSectionCensusSnapshot[] captured =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        OwnerSectionCensusSnapshot records = captured.Single(section =>
            section.SectionId == DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
        object justiceProjectionIdentity = records.OwnerInstanceIdentity;
        string hostOwnedRequiredId = DailyV1SelectedOwnerSectionCensusContract.FixedRequired
            .First(sectionId =>
                !DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedSelectedRuntimeSectionIds
                    .Contains(sectionId, StringComparer.Ordinal)
                && sectionId != P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId
                && sectionId != P12CrimeSocialAppraisalOwnerContract.KnowledgeSectionId
                && sectionId != P12CrimeSocialAppraisalOwnerContract.ReactionsSectionId
                && sectionId != DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId
                && sectionId != DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId);
        OwnerSectionCensusSnapshot hostOwnedBefore = captured.Single(section =>
            section.SectionId == hostOwnedRequiredId);

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedDailyOwnerVector(
                graph, actorChoices, captured, justiceProjectionIdentity, 0L,
                out IReadOnlyList<OwnerSectionCensusSnapshot> composed,
                out string diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
            composed), Is.True);
        Assert.That(composed.Select(section => section.SectionId),
            Is.EquivalentTo(captured.Select(section => section.SectionId)));
        OwnerSectionCensusSnapshot composedSequence = composed.Single(section =>
            section.SectionId == SimulationRecordSequenceCensusProvider.SectionId);
        OwnerSectionCensusWitness sequenceWitness =
            new SimulationRecordSequenceCensusProvider(graph.RecordSequence).GetCurrentCensus();
        Assert.That(composedSequence.OwnerInstanceIdentity,
            Is.SameAs(sequenceWitness.OwnerInstanceIdentity));
        Assert.That(SimulationRecordSequenceOwnerBridge.TryMatchCurrentCensus(
            graph.RecordSequence, composedSequence.OwnerInstanceIdentity,
            composedSequence.Revision, out _), Is.True);
        Assert.That(composedSequence.Cardinality, Is.EqualTo(1));
        Assert.That(composedSequence.Revision, Is.EqualTo(sequenceWitness.Revision));
        Assert.That(composed.Single(section => section.SectionId
                == P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId).OwnerInstanceIdentity,
            Is.SameAs(crimeSocial.TheftOutcomes));
        Assert.That(composed.Single(section => section.SectionId
                == P12CrimeSocialAppraisalOwnerContract.KnowledgeSectionId).OwnerInstanceIdentity,
            Is.SameAs(crimeSocial.CrimeKnowledge));
        Assert.That(composed.Single(section => section.SectionId
                == P12CrimeSocialAppraisalOwnerContract.ReactionsSectionId).OwnerInstanceIdentity,
            Is.SameAs(crimeSocial.SocialReactions));

        OwnerSectionCensusSnapshot composedRecords = composed.Single(section =>
            section.SectionId == DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
        OwnerSectionCensusSnapshot composedReceipts = composed.Single(section =>
            section.SectionId == DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId);
        Assert.That(composedRecords.OwnerInstanceIdentity, Is.SameAs(justiceProjectionIdentity));
        Assert.That(composedReceipts.OwnerInstanceIdentity, Is.SameAs(justiceProjectionIdentity));
        Assert.That(composedRecords.Cardinality,
            Is.EqualTo(graph.JusticeRecordsOwner.WantedRecords.Count
                + graph.JusticeRecordsOwner.Sentences.Count));
        Assert.That(composedRecords.Revision, Is.EqualTo(graph.JusticeRecordsOwner.Revision));
        Assert.That(composedReceipts.Cardinality, Is.EqualTo(1));
        Assert.That(composedReceipts.Revision, Is.Zero,
            "The P18 receipt census keeps its established exact-zero sentinel.");

        OwnerSectionCensusSnapshot hostOwnedAfter = composed.Single(section =>
            section.SectionId == hostOwnedRequiredId);
        Assert.That(hostOwnedAfter.OwnerInstanceIdentity,
            Is.SameAs(hostOwnedBefore.OwnerInstanceIdentity));
        Assert.That(hostOwnedAfter.Cardinality, Is.EqualTo(hostOwnedBefore.Cardinality));
        Assert.That(hostOwnedAfter.Revision, Is.EqualTo(hostOwnedBefore.Revision));

        OwnerSectionCensusSnapshot[] nonzeroReceiptRevision = captured.Select(section =>
            section.SectionId == DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId
                ? new OwnerSectionCensusSnapshot(
                    section.SectionId, section.SchemaVersion, section.Role,
                    section.OwnerInstanceIdentity, section.Cardinality, 1L)
                : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedDailyOwnerVector(
                graph, actorChoices, nonzeroReceiptRevision, justiceProjectionIdentity, 1L,
                out IReadOnlyList<OwnerSectionCensusSnapshot> rejected,
                out string rejectionDiagnostic), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(rejectionDiagnostic, Does.Contain("exact-zero"));

        OwnerSectionCensusSnapshot[] staleRecordSequence = captured.Select(section =>
            section.SectionId == SimulationRecordSequenceCensusProvider.SectionId
                ? new OwnerSectionCensusSnapshot(section.SectionId, section.SchemaVersion,
                    section.Role, section.OwnerInstanceIdentity, section.Cardinality,
                    checked(section.Revision + 1L))
                : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedDailyOwnerVector(
                graph, actorChoices, staleRecordSequence, justiceProjectionIdentity, 0L,
                out IReadOnlyList<OwnerSectionCensusSnapshot> staleRejected,
                out string staleDiagnostic), Is.False);
        Assert.That(staleRejected, Is.Null);
        Assert.That(staleDiagnostic, Does.Contain("record-sequence"));
    }

    [Test]
    public void CoreRecomposesTheWholeSelectedNpcCensusFragmentFromItsExactOwnerGraph()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out _);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        OwnerSectionCensusSnapshot[] completeVector =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
            completeVector), Is.True);

        List<IOwnerSectionCensusProvider> providers =
            new List<IOwnerSectionCensusProvider>();
        providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateInventoryProviders(graph.Npcs));
        providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs));
        providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateRuntimeAuthorityProviders(graph));
        providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateCommercialKnowledgeProviders(graph.Npcs));
        providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateSpatialKnowledgeProviders(graph.Npcs));
        providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateKnowledgeAndPlanProviders(graph));

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryGetExactCoreOwnedNpcCensusFragmentSectionIds(
                    completeVector, graph,
                    out IReadOnlyList<string> coreNpcSectionIds,
                    out string omissionDiagnostic),
            Is.True, omissionDiagnostic);
        Assert.That(coreNpcSectionIds, Has.Count.EqualTo(providers.Count));
        Assert.That(coreNpcSectionIds.ToHashSet(StringComparer.Ordinal),
            Is.EquivalentTo(providers.Select(provider =>
                provider.GetCurrentCensus().SectionId)));

        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            OwnerSectionCensusSnapshot captured = completeVector.Single(section =>
                section.SectionId == witness.SectionId);
            Assert.That(captured.Role, Is.EqualTo(OwnerSectionRole.Required), witness.SectionId);
            Assert.That(captured.SchemaVersion, Is.EqualTo(witness.SchemaVersion), witness.SectionId);
            Assert.That(captured.OwnerInstanceIdentity, Is.SameAs(witness.OwnerInstanceIdentity), witness.SectionId);
            Assert.That(captured.Cardinality, Is.EqualTo(witness.Cardinality), witness.SectionId);
            Assert.That(captured.Revision, Is.EqualTo(witness.Revision), witness.SectionId);
        }

        DailyV1NpcRuntimeOwners selectedNpc = graph.Npcs[0];
        OwnerSectionCensusSnapshot topologyPlaces = completeVector.Single(section =>
            section.SectionId == DailyV1SelectedOwnerSectionCensusContract.NpcLocalTopologyPlacesPrefix
                + selectedNpc.RuntimeId);
        Assert.That(topologyPlaces.OwnerInstanceIdentity,
            Is.SameAs(selectedNpc.LocalTopologyKnowledgeEmptyOwner));
        Assert.That(topologyPlaces.Cardinality, Is.Zero);
        Assert.That(topologyPlaces.Revision, Is.Zero);
        Assert.That(coreNpcSectionIds.Any(sectionId =>
            sectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsPrefix,
                StringComparison.Ordinal)), Is.False);
        Assert.That(coreNpcSectionIds.Any(sectionId =>
            sectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradeStateReceiptsPrefix,
                StringComparison.Ordinal)), Is.False,
            "P12-D receipt owners remain in the Unity residual source.");

        HashSet<string> omittedIds = new HashSet<string>(
            coreNpcSectionIds, StringComparer.Ordinal);
        OwnerSectionCensusSnapshot[] hostRemainder = completeVector
            .Where(section => !omittedIds.Contains(section.SectionId))
            .ToArray();
        Assert.That(hostRemainder.Any(section => omittedIds.Contains(section.SectionId)), Is.False);

        ActorChoiceStore actorChoices = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoices, hostRemainder,
                out IReadOnlyList<OwnerSectionCensusSnapshot> recomposed,
                out string composeDiagnostic),
            Is.True, composeDiagnostic);
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
            recomposed), Is.True);
        Assert.That(recomposed.Count, Is.EqualTo(completeVector.Length));
        foreach (OwnerSectionCensusSnapshot original in completeVector)
        {
            OwnerSectionCensusSnapshot rebuilt = recomposed.Single(section =>
                section.SectionId == original.SectionId);
            Assert.That(rebuilt.Role, Is.EqualTo(original.Role), original.SectionId);
            Assert.That(rebuilt.SchemaVersion, Is.EqualTo(original.SchemaVersion), original.SectionId);
            Assert.That(rebuilt.OwnerInstanceIdentity, Is.SameAs(original.OwnerInstanceIdentity), original.SectionId);
            Assert.That(rebuilt.Cardinality, Is.EqualTo(original.Cardinality), original.SectionId);
            Assert.That(rebuilt.Revision, Is.EqualTo(original.Revision), original.SectionId);
        }

        OwnerSectionCensusSnapshot compatibilityInventory = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "compatibility-npc",
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, new object(), 0, 0L);
        OwnerSectionCensusSnapshot compatibilityMoney = new OwnerSectionCensusSnapshot(
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + "compatibility-npc",
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required, new object(), 1, 0L);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryGetExactCoreOwnedNpcCensusFragmentSectionIds(
                    completeVector.Concat(new[] { compatibilityInventory, compatibilityMoney }).ToArray(),
                    graph, out IReadOnlyList<string> mixedOmissionIds,
                    out string mixedDiagnostic),
            Is.True, mixedDiagnostic);
        Assert.That(mixedOmissionIds, Is.Empty,
            "A mixed roster keeps the full host census for compatibility validation.");

        string staleId = coreNpcSectionIds[0];
        OwnerSectionCensusSnapshot staleSection = completeVector.Single(section =>
            section.SectionId == staleId);
        OwnerSectionCensusSnapshot staleRevision = new OwnerSectionCensusSnapshot(
            staleSection.SectionId, staleSection.SchemaVersion, staleSection.Role,
            staleSection.OwnerInstanceIdentity, staleSection.Cardinality,
            checked(staleSection.Revision + 1L));
        OwnerSectionCensusSnapshot[] staleVector = completeVector.Select(section =>
            section.SectionId == staleId ? staleRevision : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryGetExactCoreOwnedNpcCensusFragmentSectionIds(
                    staleVector, graph, out IReadOnlyList<string> staleOmissionIds,
                    out string staleDiagnostic),
            Is.True, staleDiagnostic);
        Assert.That(staleOmissionIds, Is.Empty,
            "Stale evidence remains intact for the established P12 token classification.");
    }

    [Test]
    public void CoreP12RecomposesExactSelectedNpcRuntimeStateRowsFromOwnerGraph()
    {
        CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out _);
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        OwnerSectionCensusSnapshot[] completeVector =
            CreateCoreBoundCompleteOwnerSectionVector(session);
        Assert.That(graph.Npcs.Count, Is.GreaterThan(0));

        IOwnerSectionCensusProvider[] knowledgeAndPlanProviders = DailyV1NpcOwnerCensusProviders
            .CreateKnowledgeAndPlanProviders(graph).ToArray();
        Assert.That(knowledgeAndPlanProviders, Has.Length.EqualTo(graph.Npcs.Count * 9));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryGetExactCoreOwnedNpcKnowledgeAndPlanSectionIds(
                    completeVector, graph,
                    out IReadOnlyList<string> coreOwnedKnowledgeAndPlanIds,
                    out string knowledgeAndPlanDiagnostic),
            Is.True, knowledgeAndPlanDiagnostic);
        Assert.That(coreOwnedKnowledgeAndPlanIds, Has.Count.EqualTo(knowledgeAndPlanProviders.Length));
        Assert.That(coreOwnedKnowledgeAndPlanIds.ToHashSet(StringComparer.Ordinal),
            Is.EquivalentTo(knowledgeAndPlanProviders.Select(provider => provider.GetCurrentCensus().SectionId)));

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryGetExactCoreOwnedNpcRuntimeStateSectionIds(
                    completeVector, graph,
                    out IReadOnlyList<string> coreOwnedRuntimeStateIds,
                    out string omissionDiagnostic),
            Is.True, omissionDiagnostic);
        IOwnerSectionCensusProvider[] providers = DailyV1NpcOwnerCensusProviders
            .CreateRuntimeAuthorityProviders(graph).ToArray();
        Assert.That(coreOwnedRuntimeStateIds, Has.Count.EqualTo(providers.Length));
        Assert.That(coreOwnedRuntimeStateIds.ToHashSet(StringComparer.Ordinal),
            Is.EquivalentTo(providers.Select(provider => provider.GetCurrentCensus().SectionId)));

        string currentActionPrefix = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix;
        string[] selectedCurrentActionIds = coreOwnedRuntimeStateIds
            .Where(sectionId => sectionId.StartsWith(currentActionPrefix, StringComparison.Ordinal))
            .ToArray();
        Assert.That(selectedCurrentActionIds, Has.Length.EqualTo(graph.Npcs.Count));
        HashSet<string> selectedCurrentActionIdSet = new HashSet<string>(
            selectedCurrentActionIds, StringComparer.Ordinal);
        OwnerSectionCensusSnapshot[] currentActionOmittedVector = completeVector
            .Where(section => !selectedCurrentActionIdSet.Contains(section.SectionId)).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryRebindCoreOwnedNpcRuntimeStateSections(
                    graph, currentActionOmittedVector,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> currentActionRecomposed,
                    out string currentActionDiagnostic),
            Is.True, currentActionDiagnostic);
        Assert.That(currentActionRecomposed.Select(section => section.SectionId)
                .OrderBy(sectionId => sectionId, StringComparer.Ordinal),
            Is.EqualTo(completeVector.Select(section => section.SectionId)
                .OrderBy(sectionId => sectionId, StringComparer.Ordinal)));
        foreach (IOwnerSectionCensusProvider provider in providers.Where(provider =>
                     provider.GetCurrentCensus().SectionId.StartsWith(
                         currentActionPrefix, StringComparison.Ordinal)))
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            OwnerSectionCensusSnapshot actual = currentActionRecomposed.Single(
                section => section.SectionId == witness.SectionId);
            Assert.That(actual.Role, Is.EqualTo(OwnerSectionRole.Required), witness.SectionId);
            Assert.That(actual.SchemaVersion, Is.EqualTo(witness.SchemaVersion), witness.SectionId);
            Assert.That(actual.OwnerInstanceIdentity, Is.SameAs(witness.OwnerInstanceIdentity), witness.SectionId);
            Assert.That(actual.Cardinality, Is.EqualTo(witness.Cardinality), witness.SectionId);
            Assert.That(actual.Revision, Is.EqualTo(witness.Revision), witness.SectionId);
        }

        OwnerSectionCensusSnapshot[] partialCurrentActionVector = completeVector
            .Where(section => section.SectionId != selectedCurrentActionIds[0]).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryRebindCoreOwnedNpcRuntimeStateSections(
                    graph, partialCurrentActionVector, out _, out _),
            Is.False, "Partial CurrentAction omission cannot be mistaken for Core-owned exact omission.");

        OwnerSectionCensusSnapshot justiceRecords = completeVector.Single(section =>
            section.SectionId == DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedDailyOwnerVector(
                graph,
                session.ExecutionBoundaryState.SelectedActorChoiceStore,
                currentActionOmittedVector,
                justiceRecords.OwnerInstanceIdentity,
                0L,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeCoreVector,
                out string completeCoreVectorDiagnostic),
            Is.True, completeCoreVectorDiagnostic);
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
            completeCoreVector), Is.True);
        foreach (string sectionId in selectedCurrentActionIds)
            Assert.That(completeCoreVector.Any(section => section.SectionId == sectionId), Is.True,
                "Full Core owner-vector composition restores each authoritative ActionSlot census row.");

        HashSet<string> omittedIds = new HashSet<string>(
            coreOwnedRuntimeStateIds, StringComparer.Ordinal);
        OwnerSectionCensusSnapshot[] hostRemainder = completeVector
            .Where(section => !omittedIds.Contains(section.SectionId)).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryRebindCoreOwnedNpcRuntimeStateSections(
                    graph, hostRemainder,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> recomposed,
                    out string recomposeDiagnostic),
            Is.True, recomposeDiagnostic);
        Assert.That(recomposed.Select(section => section.SectionId)
                .OrderBy(sectionId => sectionId, StringComparer.Ordinal),
            Is.EqualTo(completeVector.Select(section => section.SectionId)
                .OrderBy(sectionId => sectionId, StringComparer.Ordinal)));
        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            OwnerSectionCensusSnapshot actual = recomposed.Single(section =>
                section.SectionId == witness.SectionId);
            Assert.That(actual.Role, Is.EqualTo(OwnerSectionRole.Required), witness.SectionId);
            Assert.That(actual.SchemaVersion, Is.EqualTo(witness.SchemaVersion), witness.SectionId);
            Assert.That(actual.OwnerInstanceIdentity, Is.SameAs(witness.OwnerInstanceIdentity), witness.SectionId);
            Assert.That(actual.Cardinality, Is.EqualTo(witness.Cardinality), witness.SectionId);
            Assert.That(actual.Revision, Is.EqualTo(witness.Revision), witness.SectionId);
        }

        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryGetExactCoreOwnedNpcInventoryAndMoneyAccountSectionIds(
                    completeVector, graph,
                    out IReadOnlyList<string> coreOwnedInventoryAndMoneyIds,
                    out string inventoryDiagnostic),
            Is.True, inventoryDiagnostic);
        HashSet<string> completeCoreOwnedIds = new HashSet<string>(
            DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedP12FSectionIds
                .Concat(DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedRuntimeIdAllocatorSectionIds)
                .Concat(coreOwnedInventoryAndMoneyIds)
                .Concat(coreOwnedRuntimeStateIds)
                .Concat(coreOwnedKnowledgeAndPlanIds),
            StringComparer.Ordinal);
        OwnerSectionCensusSnapshot[] exactCoreHostRemainder = completeVector
            .Where(section => !completeCoreOwnedIds.Contains(section.SectionId)).ToArray();
        ActorChoiceStore actorChoices = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                graph, actorChoices, exactCoreHostRemainder,
                out IReadOnlyList<OwnerSectionCensusSnapshot> p12Composed,
                out string p12CompositionDiagnostic),
            Is.True, p12CompositionDiagnostic);
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
            p12Composed), Is.True,
            "Core must recompose NPC state before the P12 89-family completeness gate.");
        Assert.That(p12Composed.Select(section => section.SectionId)
                .OrderBy(sectionId => sectionId, StringComparer.Ordinal),
            Is.EqualTo(completeVector.Select(section => section.SectionId)
                .OrderBy(sectionId => sectionId, StringComparer.Ordinal)));

        string omittedRuntimeStateId = coreOwnedRuntimeStateIds[0];
        OwnerSectionCensusSnapshot[] partialHostVector = completeVector
            .Where(section => section.SectionId != omittedRuntimeStateId).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryRebindCoreOwnedNpcRuntimeStateSections(
                    graph, partialHostVector, out _, out _),
            Is.False, "Partial omission must not silently combine host and Core rows for one alias family.");

        OwnerSectionCensusSnapshot selectedHostRow = completeVector.Single(section =>
            section.SectionId == omittedRuntimeStateId);
        OwnerSectionCensusSnapshot substituted = new OwnerSectionCensusSnapshot(
            selectedHostRow.SectionId, selectedHostRow.SchemaVersion, selectedHostRow.Role,
            new object(), selectedHostRow.Cardinality, selectedHostRow.Revision);
        OwnerSectionCensusSnapshot[] substitutedVector = completeVector.Select(section =>
            section.SectionId == omittedRuntimeStateId ? substituted : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryRebindCoreOwnedNpcRuntimeStateSections(
                    graph, substitutedVector, out _, out _),
            Is.False, "A host row cannot substitute another instance for a selected Core owner.");

        OwnerSectionCensusSnapshot stale = new OwnerSectionCensusSnapshot(
            selectedHostRow.SectionId, selectedHostRow.SchemaVersion, selectedHostRow.Role,
            selectedHostRow.OwnerInstanceIdentity, selectedHostRow.Cardinality,
            checked(selectedHostRow.Revision + 1L));
        OwnerSectionCensusSnapshot[] staleVector = completeVector.Select(section =>
            section.SectionId == omittedRuntimeStateId ? stale : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryRebindCoreOwnedNpcRuntimeStateSections(
                    graph, staleVector, out _, out _),
            Is.False, "A stale revision remains on the host path and cannot be rebound as current Core state.");

        const string compatibilityRuntimeId = "npc-census-compatibility-runtime-state";
        string[] compatibilityPrefixes =
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcLifeStatePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcResidencePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix
        };
        OwnerSectionCensusSnapshot[] compatibilityRows = compatibilityPrefixes
            .Select(prefix => new OwnerSectionCensusSnapshot(
                prefix + compatibilityRuntimeId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, new object(), 1, 0L))
            .ToArray();
        OwnerSectionCensusSnapshot[] mixedHostVector = completeVector
            .Concat(compatibilityRows).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryRebindCoreOwnedNpcRuntimeStateSections(
                    graph, mixedHostVector,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> mixedResult,
                    out string mixedDiagnostic),
            Is.True, mixedDiagnostic);
        foreach (OwnerSectionCensusSnapshot compatibilityRow in compatibilityRows)
            Assert.That(mixedResult.Single(section =>
                    section.SectionId == compatibilityRow.SectionId),
                Is.SameAs(compatibilityRow),
                "Mixed-roster capture preserves every host-owned selected/compatibility census fact.");
        Assert.That(mixedResult.Count(section =>
                section.SectionId.StartsWith(currentActionPrefix, StringComparison.Ordinal)),
            Is.EqualTo(graph.Npcs.Count + 1),
            "Mixed-roster composition keeps every selected and compatibility CurrentAction row.");

        string omittedKnowledgeAndPlanId = coreOwnedKnowledgeAndPlanIds[0];
        OwnerSectionCensusSnapshot[] partialKnowledgeAndPlanVector = completeVector
            .Where(section => section.SectionId != omittedKnowledgeAndPlanId).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
                    graph, partialKnowledgeAndPlanVector, out _, out _),
            Is.False, "A partial Knowledge/Plan family omission cannot pass as an exact owner vector.");

        OwnerSectionCensusSnapshot selectedKnowledgeAndPlanRow = completeVector.Single(section =>
            section.SectionId == omittedKnowledgeAndPlanId);
        OwnerSectionCensusSnapshot substitutedKnowledgeAndPlanRow = new OwnerSectionCensusSnapshot(
            selectedKnowledgeAndPlanRow.SectionId, selectedKnowledgeAndPlanRow.SchemaVersion,
            selectedKnowledgeAndPlanRow.Role, new object(), selectedKnowledgeAndPlanRow.Cardinality,
            selectedKnowledgeAndPlanRow.Revision);
        OwnerSectionCensusSnapshot[] substitutedKnowledgeAndPlanVector = completeVector.Select(section =>
            section.SectionId == omittedKnowledgeAndPlanId ? substitutedKnowledgeAndPlanRow : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
                    graph, substitutedKnowledgeAndPlanVector,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> substitutedResult,
                    out string substitutedDiagnostic),
            Is.True, substitutedDiagnostic);
        Assert.That(substitutedResult.Single(section =>
                section.SectionId == omittedKnowledgeAndPlanId),
            Is.SameAs(substitutedKnowledgeAndPlanRow),
            "A substituted source owner remains available for the existing P12 stale-token rejection.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryGetExactCoreOwnedNpcKnowledgeAndPlanSectionIds(
                    substitutedKnowledgeAndPlanVector, graph,
                    out IReadOnlyList<string> substitutedOmissionIds, out _),
            Is.True);
        Assert.That(substitutedOmissionIds, Is.Empty,
            "A substituted source owner cannot be omitted from the Unity host remainder.");

        OwnerSectionCensusSnapshot staleKnowledgeAndPlanRow = new OwnerSectionCensusSnapshot(
            selectedKnowledgeAndPlanRow.SectionId, selectedKnowledgeAndPlanRow.SchemaVersion,
            selectedKnowledgeAndPlanRow.Role, selectedKnowledgeAndPlanRow.OwnerInstanceIdentity,
            selectedKnowledgeAndPlanRow.Cardinality, checked(selectedKnowledgeAndPlanRow.Revision + 1L));
        OwnerSectionCensusSnapshot[] staleKnowledgeAndPlanVector = completeVector.Select(section =>
            section.SectionId == omittedKnowledgeAndPlanId ? staleKnowledgeAndPlanRow : section).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
                    graph, staleKnowledgeAndPlanVector,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> staleKnowledgeAndPlanResult,
                    out string staleKnowledgeAndPlanDiagnostic),
            Is.True, staleKnowledgeAndPlanDiagnostic);
        Assert.That(staleKnowledgeAndPlanResult.Single(section =>
                section.SectionId == omittedKnowledgeAndPlanId),
            Is.SameAs(staleKnowledgeAndPlanRow),
            "A stale source revision remains visible to the existing P12 token checks.");
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryGetExactCoreOwnedNpcKnowledgeAndPlanSectionIds(
                    staleKnowledgeAndPlanVector, graph,
                    out IReadOnlyList<string> staleOmissionIds, out _),
            Is.True);
        Assert.That(staleOmissionIds, Is.Empty,
            "A stale source revision cannot be omitted from the Unity host remainder.");

        string[] compatibilityKnowledgeAndPlanPrefixes =
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcExplorableSiteKnowledgePrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcLocalTopologyPlacesPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcLocalTopologyConnectionsPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcAdventureOppositionPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcAdventureNotableItemsPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcAdventureCommonResourcesPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcAdventureAccessPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradePlanPrefix,
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanPrefix
        };
        OwnerSectionCensusSnapshot[] compatibilityKnowledgeAndPlanRows =
            compatibilityKnowledgeAndPlanPrefixes.Select(prefix => new OwnerSectionCensusSnapshot(
                prefix + compatibilityRuntimeId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                OwnerSectionRole.Required, new object(), 1, 0L)).ToArray();
        OwnerSectionCensusSnapshot[] mixedKnowledgeAndPlanVector = completeVector
            .Concat(compatibilityRows.Where(row =>
                row.SectionId.StartsWith(DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix,
                    StringComparison.Ordinal)
                || row.SectionId.StartsWith(DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix,
                    StringComparison.Ordinal)))
            .Concat(compatibilityKnowledgeAndPlanRows).ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource
                .TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
                    graph, mixedKnowledgeAndPlanVector,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> mixedKnowledgeAndPlanResult,
                    out string mixedKnowledgeAndPlanDiagnostic),
            Is.True, mixedKnowledgeAndPlanDiagnostic);
        foreach (OwnerSectionCensusSnapshot compatibilityRow in compatibilityKnowledgeAndPlanRows)
            Assert.That(mixedKnowledgeAndPlanResult.Single(section =>
                    section.SectionId == compatibilityRow.SectionId),
                Is.SameAs(compatibilityRow),
                "Mixed-roster composition preserves every host-owned Knowledge/Plan compatibility owner.");
    }

    [Test]
    public void CoreDropsCensusEvidenceWhenHostFailsOrScopeChangesDuringCapture()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts);
        DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary =
            session.RuntimeAdvanceBoundary as DailyV1CoreSelectedRuntimeAdvanceBoundary;
        Assert.That(coreBoundary, Is.Not.Null);
        IReadOnlyList<OwnerSectionCensusSnapshot> expectedSections =
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        hostPorts.SetAdmissionEvidence(expectedSections, mutationEpoch: 23L);

        hostPorts.ReturnPartialOwnerEvidenceThenFail = true;
        Assert.That(coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
            sessionReplacement: false,
            out IReadOnlyList<OwnerSectionCensusSnapshot> rejectedSections,
            out long rejectedEpoch,
            out DailyCaptureEligibilityFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.RuntimeFaulted));
        Assert.That(rejectedSections, Is.Null,
            "A failing host census cannot leak a partially populated owner vector through Core.");
        Assert.That(rejectedEpoch, Is.Zero);

        hostPorts.ReturnPartialOwnerEvidenceThenFail = false;
        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        hostPorts.SetOwnerSectionCensusCaptureCallback(allowance =>
        {
            Assert.That(allowance, Is.EqualTo(DailyV1CaptureScopeAllowance.None));
            Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.Application), Is.True,
                "The fake host simulates a reentrant scope opening during census capture.");
        });

        Assert.That(coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
            sessionReplacement: false,
            out rejectedSections,
            out rejectedEpoch,
            out failure), Is.False,
            "Core must reject a census if its allowed outer scope changes during the host read.");
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress));
        Assert.That(rejectedSections, Is.Null);
        Assert.That(rejectedEpoch, Is.Zero);
        Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.Application), Is.True);
    }

    [Test]
    public void CoreRejectsOffThreadCaptureWithoutFaultingRuntimeAdmission()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts);
        Assert.That(application, Is.Not.Null);
        DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary =
            session.RuntimeAdvanceBoundary as DailyV1CoreSelectedRuntimeAdvanceBoundary;
        Assert.That(coreBoundary, Is.Not.Null);

        bool captured = true;
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections =
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        long mutationEpoch = 41L;
        DailyCaptureEligibilityFailure failure = DailyCaptureEligibilityFailure.None;
        Thread caller = new Thread(() =>
        {
            captured = coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
                sessionReplacement: false,
                out ownerSections,
                out mutationEpoch,
                out failure);
        });
        caller.Start();
        caller.Join();

        Assert.That(captured, Is.False);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.WrongOwnerThread));
        Assert.That(ownerSections, Is.Null);
        Assert.That(mutationEpoch, Is.Zero);
        Assert.That(hostPorts.Faulted, Is.False,
            "A wrong-thread capture is rejected without faulting the admitted runtime.");
        Assert.That(hostPorts.OwnerCensusCaptureCount, Is.Zero);
    }

    [Test]
    public void CoreOwnsSelectedCapturePreconditionsAndMapsCensusFailures()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary hostPorts,
            publishWorld: false);
        DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary =
            session.RuntimeAdvanceBoundary as DailyV1CoreSelectedRuntimeAdvanceBoundary;
        Assert.That(coreBoundary, Is.Not.Null);
        hostPorts.SetAdmissionEvidence(
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot(), mutationEpoch: 29L);

        Assert.That(coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
            sessionReplacement: false, out IReadOnlyList<OwnerSectionCensusSnapshot> sections,
            out long epoch, out DailyCaptureEligibilityFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.WorldNotPublished));
        Assert.That(sections, Is.Null);
        Assert.That(epoch, Is.Zero);
        Assert.That(hostPorts.OwnerCensusCaptureCount, Is.Zero);

        Assert.That(session.ExecutionBoundaryState.TryMarkFactualReadWorldPublished(), Is.True);
        Assert.That(session.ExecutionBoundaryState.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.True);
        Assert.That(coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
            sessionReplacement: false, out sections, out epoch, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress));
        Assert.That(hostPorts.OwnerCensusCaptureCount, Is.Zero);
        Assert.That(session.ExecutionBoundaryState.TryEndRuntimeProtocolOperation(), Is.True);
        hostPorts.CensusFailure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
        Assert.That(coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
            sessionReplacement: false, out sections, out epoch, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OwnerCoverageIncomplete));
        Assert.That(sections, Is.Null);
        Assert.That(epoch, Is.Zero);
        Assert.That(hostPorts.OwnerCensusCaptureCount, Is.EqualTo(1));

        hostPorts.CensusFailure = DailyCaptureEligibilityFailure.None;
        bool operationOpenedDuringCapture = false;
        hostPorts.SetOwnerSectionCensusCaptureCallback(_ =>
            operationOpenedDuringCapture = session.ExecutionBoundaryState.TryBeginRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.NpcMembership));
        Assert.That(coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
            sessionReplacement: false, out sections, out epoch, out failure), Is.False,
            "Core must discard evidence if a P12 runtime operation enters the shared registry during the census read.");
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress));
        Assert.That(sections, Is.Null);
        Assert.That(epoch, Is.Zero);
        Assert.That(hostPorts.OwnerCensusCaptureCount, Is.EqualTo(2));
        Assert.That(operationOpenedDuringCapture, Is.True);
        Assert.That(session.ExecutionBoundaryState.TryEndRuntimeProtocolOperation(), Is.True);
        hostPorts.SetOwnerSectionCensusCaptureCallback(null);
    }

    [Test]
    public void ApplicationRejectsAnotherGenesisAfterPublishingItsInitialSession()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out string diagnostic), Is.True, diagnostic);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(
            candidate, new ApplicationAdvanceBoundary(candidate.ExecutionBoundaryState, null)), Is.True);
        Assert.That(application.TryPublishInitial(candidate), Is.True);
        DailyV1ActiveRuntimeSession published = application.ActiveSession;

        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out _, out diagnostic), Is.False);

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
        Assert.That(application.TryBindRuntimeAdvanceBoundary(
            session, new ApplicationAdvanceBoundary(session.ExecutionBoundaryState, null)), Is.True);
        Assert.That(foreignApplication.TryBindRuntimeAdvanceBoundary(
            foreignSession, new ApplicationAdvanceBoundary(foreignSession.ExecutionBoundaryState, null)), Is.True);
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

        Assert.That(application.TryBindRuntimeAdvanceBoundary(
            first, new ApplicationAdvanceBoundary(first.ExecutionBoundaryState, null)), Is.True);
        Assert.That(application.TryPublishInitial(first), Is.True);
        Assert.That(application.ActiveSession, Is.SameAs(first));
        Assert.That(application.TryPublishReplacement(first, replacement), Is.False,
            "A replacement without its exact runtime advance boundary cannot become active.");
        Assert.That(application.TryBindRuntimeAdvanceBoundary(
            replacement, new ApplicationAdvanceBoundary(replacement.ExecutionBoundaryState, null)), Is.True);
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
            Assert.That(current.ExecutionState.TryBeginRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.False,
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
        Assert.That(session.ExecutionState.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
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
        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
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

    [Test]
    public void ContinuationImageLoadUsesExactReservedSessionForUnityV1AndDotNetV2()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        InstallCompletedBoundaryToken(session, boundary);
        MissingContinuationSlotOperations fileOperations = new MissingContinuationSlotOperations();
        P12ACompatibilityDescriptor unityIdentity = new P12ACompatibilityDescriptor(
            "UnityBootstrap-Daily-v1", 1, "config", "6000.3.9f1", "WindowsEditor", "Mono",
            "build", new[] { "00000000-0000-0000-0000-000000000001" }, "numeric", 1,
            "content", "providers", "daily", 1, "calendar", "p9", "p8");
        P12ACompatibilityDescriptorV2 dotNetIdentity = new P12ACompatibilityDescriptorV2(
            "UnityBootstrap-Daily-v1", 1, 1,
            ".NET 8.0", "8.0.1.0", "Windows", "win-x64", "x64", "net8.0",
            new[] { new P12AAssemblyIdentityV2("Simulation.Core", "1.0.0.0",
                "00000000-0000-0000-0000-000000000001") }, "numeric", 1,
            "configuration", "content", "providers", "calendar", 1,
            "calendar-fingerprint", "p9", "p8");

        Assert.That(application.TryLoadContinuationImageForReplacement(
            session, null, unityIdentity, null, "private-root", fileOperations,
            out P12AContinuationImage unreservedImage, out P12AResult unreservedResult), Is.False);
        Assert.That(unreservedImage, Is.Null);
        Assert.That(unreservedResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
        Assert.That(fileOperations.OpenReadCount, Is.Zero,
            "A missing replacement reservation must not touch the continuation slot.");

        Assert.That(application.TryBeginExclusiveOperation(session, out var replacement), Is.True);
        using (replacement)
        {
            Assert.That(application.TryLoadContinuationImageForReplacement(
                session, replacement, unityIdentity, null,
                "private-root", fileOperations,
                out P12AContinuationImage unityImage, out P12AResult unityResult), Is.False);
            Assert.That(unityImage, Is.Null);
            Assert.That(unityResult.Code, Is.EqualTo(P12AResultCode.NoSave));

            Assert.That(application.TryLoadContinuationImageForReplacement(
                session, replacement, null, dotNetIdentity,
                "private-root", fileOperations,
                out P12AContinuationImage dotNetImage, out P12AResult dotNetResult), Is.False);
            Assert.That(dotNetImage, Is.Null);
            Assert.That(dotNetResult.Code, Is.EqualTo(P12AResultCode.NoSave));
        }

        Assert.That(fileOperations.OpenReadCount, Is.EqualTo(2),
            "Both descriptor variants must use the exact reserved application session and the same read-only slot operation.");
    }

    [Test]
    public void ContinuationRootStagingRequiresExactReservedSessionBeforeInspectingImage()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        InstallCompletedBoundaryToken(session, boundary);
        int observedStages = 0;

        Assert.That(application.TryStageRestoredContinuationRoots(
                null, session, null,
                out P12CStagedCoreContinuationRoot unreservedRoots,
                out P12AResult unreservedResult,
                _ => observedStages++), Is.False);
        Assert.That(unreservedRoots, Is.Null);
        Assert.That(unreservedResult.Code, Is.EqualTo(P12AResultCode.Busy),
            "Without the exact replacement reservation, Core must reject before inspecting the image.");
        Assert.That(observedStages, Is.Zero);

        Assert.That(application.TryStageRestoredP12DOwnersFromImageRows(
                null, session.Definitions, null, null, null, null, null,
                session, null, out DailyV1P12DSelectedCoreOwnerStage unreservedRowOwners,
                out P12AResult unreservedRowResult), Is.False);
        Assert.That(unreservedRowOwners, Is.Null);
        Assert.That(unreservedRowResult.Code, Is.EqualTo(P12AResultCode.Busy),
            "The row-based entry rejects before inspecting detached image rows without the exact source reservation.");
        Assert.That(application.TryBeginExclusiveOperation(session, out var replacement), Is.True);
        using (replacement)
        {
            Assert.That(application.TryStageRestoredContinuationRoots(
                    null, session, replacement,
                    out P12CStagedCoreContinuationRoot reservedRoots,
                    out P12AResult reservedResult,
                    _ => observedStages++), Is.False);
            Assert.That(reservedRoots, Is.Null);
            Assert.That(reservedResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue),
                "Only after source authorization may Core reject a missing detached image as malformed input.");
            Assert.That(observedStages, Is.Zero);
        }
    }

    [Test]
    public void CoreImageRestoreTransactionRequiresReservationAndPreservesPublishedSessionOnFailure()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        InstallCompletedBoundaryToken(session, boundary);
        P12ACompatibilityDescriptor identity = new P12ACompatibilityDescriptor(
            "UnityBootstrap-Daily-v1", 1, "config", "6000.3.9f1", "WindowsEditor", "Mono",
            "build", new[] { "00000000-0000-0000-0000-000000000001" }, "numeric", 1,
            "content", "providers", "daily", 1, "calendar", "p9", "p8");
        int observedStages = 0;

        Assert.That(application.TryAdmitDecodeAndStageRestoredContinuationImage(
                null, identity, null, session.AdmissionContext,
                session, null,
                out bool unreservedAdmissionPassed,
                out P12ADailyV1Census unreservedCensus,
                out P12ADailyV1ImageOwnerSnapshots unreservedSnapshots,
                out P12CStagedCoreContinuationRoot unreservedRoots,
                out P12AResult unreservedResult,
                () => observedStages++,
                _ => observedStages++), Is.False);
        Assert.That(unreservedAdmissionPassed, Is.False);
        Assert.That(unreservedCensus, Is.Null);
        Assert.That(unreservedSnapshots, Is.Null);
        Assert.That(unreservedRoots, Is.Null);
        Assert.That(unreservedResult.Code, Is.EqualTo(P12AResultCode.Busy),
            "The Core application must reject before inspecting an image without the exact replacement reservation.");
        Assert.That(observedStages, Is.Zero);

        Assert.That(application.TryBeginExclusiveOperation(session, out var replacement), Is.True);
        using (replacement)
        {
            Assert.That(application.TryAdmitDecodeAndStageRestoredContinuationImage(
                    null, identity, null, session.AdmissionContext,
                    session, replacement,
                    out bool reservedAdmissionPassed,
                    out P12ADailyV1Census reservedCensus,
                    out P12ADailyV1ImageOwnerSnapshots reservedSnapshots,
                    out P12CStagedCoreContinuationRoot reservedRoots,
                    out P12AResult reservedResult,
                    () => observedStages++,
                    _ => observedStages++), Is.False);
            Assert.That(reservedAdmissionPassed, Is.False);
            Assert.That(reservedCensus, Is.Null);
            Assert.That(reservedSnapshots, Is.Null);
            Assert.That(reservedRoots, Is.Null);
            Assert.That(reservedResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue),
                "With the exact reservation, Core may inspect the malformed image but must fail before detached staging.");
            Assert.That(observedStages, Is.Zero);
        }

        Assert.That(application.ActiveSession, Is.SameAs(session),
            "A rejected restore transaction must leave the exact published Core session intact.");
        Assert.That(application.ActiveOperationCount, Is.Zero);
        Assert.That(application.TryBeginExclusiveOperation(session, out var retry), Is.True,
            "A failed private restore must release no authority beyond its own reservation.");
        retry.Dispose();
    }

    [Test]
    public void CoreP12DRestoreGraphRequiresExactReservationAndRetainsPublishedSessionOnFailure()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        InstallCompletedBoundaryToken(session, boundary);

        Assert.That(application.TryStageRestoredP12DCoreOwnerGraphFromImageSnapshots(
                null, null, null, session, null,
                out DailyV1P12DCoreRestoreOwnerStage unreservedOwners,
                out P12AResult unreservedResult), Is.False);
        Assert.That(unreservedOwners, Is.Null);
        Assert.That(unreservedResult.Code, Is.EqualTo(P12AResultCode.Busy));
        Assert.That(application.ActiveSession, Is.SameAs(session));

        Assert.That(application.TryBeginExclusiveOperation(session, out var replacement), Is.True);
        using (replacement)
        {
            Assert.That(application.TryStageRestoredP12DCoreOwnerGraphFromImageSnapshots(
                    null, null, null, session, replacement,
                    out DailyV1P12DCoreRestoreOwnerStage failedOwners,
                    out P12AResult failedResult), Is.False);
            Assert.That(failedOwners, Is.Null);
            Assert.That(failedResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
            Assert.That(application.ActiveSession, Is.SameAs(session),
                "A failed private P12-D stage must not replace the exact published session.");
        }

        Assert.That(application.ActiveOperationCount, Is.Zero);
        Assert.That(application.ActiveSession, Is.SameAs(session));
        Assert.That(application.TryBeginExclusiveOperation(session, out var retry), Is.True,
            "A failed D graph stage must not retain its exact-session reservation.");
        retry.Dispose();
    }
    [Test]
    public void SelectedP12DOwnerStageRequiresTheSameRestoreAuthorityAsItsRoots()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        InstallCompletedBoundaryToken(session, boundary);

        Assert.That(application.TryStageRestoredP12DOwners(
                null, session.Definitions, null, null, null, null,
                session, null, out DailyV1P12DSelectedCoreOwnerStage unreservedOwners,
                out P12AResult unreservedResult), Is.False);
        Assert.That(unreservedOwners, Is.Null);
        Assert.That(unreservedResult.Code, Is.EqualTo(P12AResultCode.Busy),
            "P12-D must reject before inspecting owner inputs unless the exact source session is reserved.");
        Assert.That(application.TryStageRestoredP12DOwnersFromImageRows(
                null, session.Definitions, null, null, null, null, null,
                session, null, out DailyV1P12DSelectedCoreOwnerStage unreservedRowOwners,
                out P12AResult unreservedRowResult), Is.False);
        Assert.That(unreservedRowOwners, Is.Null);
        Assert.That(unreservedRowResult.Code, Is.EqualTo(P12AResultCode.Busy),
            "The row-based entry rejects before inspecting detached image rows without the exact source reservation.");

        Assert.That(application.TryBeginExclusiveOperation(session, out var replacement), Is.True);
        using (replacement)
        {
            Assert.That(application.TryStageRestoredP12DOwners(
                    null, session.Definitions, null, null, null, null,
                    session, replacement, out DailyV1P12DSelectedCoreOwnerStage reservedOwners,
                    out P12AResult reservedResult), Is.False);
            Assert.That(reservedOwners, Is.Null);
            Assert.That(reservedResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue),
                "With the exact replacement reservation, Core proceeds to validate the detached P12-C roots first.");
            Assert.That(application.TryStageRestoredP12DOwnersFromImageRows(
                    null, session.Definitions, null, null, null, null, null,
                    session, replacement, out DailyV1P12DSelectedCoreOwnerStage reservedRowOwners,
                    out P12AResult reservedRowResult), Is.False);
            Assert.That(reservedRowOwners, Is.Null);
            Assert.That(reservedRowResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue),
                "After source authorization, the row-based path validates detached roots before row hydration.");
        }

        DailyV1Application initialApplication = new DailyV1Application();
        Assert.That(initialApplication.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        Assert.That(initialApplication.TryStageRestoredP12DOwners(
                null, session.Definitions, null, null, null, null,
                null, null, out DailyV1P12DSelectedCoreOwnerStage initialOwners,
                out P12AResult initialResult), Is.False);
        Assert.That(initialOwners, Is.Null);
        Assert.That(initialResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue),
            "Initial P12-D staging is admitted only on the unused app's captured bootstrap thread, then validates its roots.");
        Assert.That(initialApplication.TryStageRestoredP12DOwnersFromImageRows(
                null, session.Definitions, null, null, null, null, null,
                null, null, out DailyV1P12DSelectedCoreOwnerStage initialRowOwners,
                out P12AResult initialRowResult), Is.False);
        Assert.That(initialRowOwners, Is.Null);
        Assert.That(initialRowResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue),
            "The row-based path is admitted only on the unused app's captured bootstrap thread.");
    }
    [Test]
    public void SelectedP12EOwnerStageRequiresTheSameRestoreAuthorityAsItsRoots()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        InstallCompletedBoundaryToken(session, boundary);

        Assert.That(application.TryStageRestoredP12EOwners(
                null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, false, null, null, null, 0L, session, null,
                out P12ECoreOwnerRestoreStage unreservedOwners,
                out P12AResult unreservedResult), Is.False);
        Assert.That(unreservedOwners, Is.Null);
        Assert.That(unreservedResult.Code, Is.EqualTo(P12AResultCode.Busy),
            "P12-E must reject before inspecting owner inputs unless the exact source session is reserved.");

        Assert.That(application.TryBeginExclusiveOperation(session, out var replacement), Is.True);
        using (replacement)
        {
            Assert.That(application.TryStageRestoredP12EOwners(
                    null, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, false, null, null, null, 0L, session, replacement,
                    out P12ECoreOwnerRestoreStage reservedOwners,
                    out P12AResult reservedResult), Is.False);
            Assert.That(reservedOwners, Is.Null);
            Assert.That(reservedResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue),
                "With the exact replacement reservation, Core proceeds to validate the detached P12-C roots first.");
        }

        DailyV1Application initialApplication = new DailyV1Application();
        Assert.That(initialApplication.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        Assert.That(initialApplication.TryStageRestoredP12EOwners(
                null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, false, null, null, null, 0L, null, null,
                out P12ECoreOwnerRestoreStage initialOwners,
                out P12AResult initialResult), Is.False);
        Assert.That(initialOwners, Is.Null);
        Assert.That(initialResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue),
            "Initial P12-E staging is admitted only on the unused app's captured bootstrap thread, then validates its roots.");
    }

    [Test]
    public void SelectedP12FOwnerStageRequiresTheSameRestoreAuthorityAsItsRoots()
    {
        DailyV1Application application = CreateApplicationWithAdvanceBoundary(
            null, out DailyV1ActiveRuntimeSession session,
            out ApplicationAdvanceBoundary boundary);
        InstallCompletedBoundaryToken(session, boundary);

        Assert.That(application.TryStageRestoredP12FOwners(
                null, null, null, null, null, null,
                null, null, null, null, null, null, 0L, null, null,
                null, null, null, null, null, session, null,
                out DailyV1P12FCoreOwnerRestoreStage unreservedOwners,
                out P12AResult unreservedResult), Is.False);
        Assert.That(unreservedOwners, Is.Null);
        Assert.That(unreservedResult.Code, Is.EqualTo(P12AResultCode.Busy),
            "P12-F must reject before inspecting owner inputs unless the exact source session is reserved.");

        Assert.That(application.TryBeginExclusiveOperation(session, out var replacement), Is.True);
        using (replacement)
        {
            Assert.That(application.TryStageRestoredP12FOwners(
                    null, null, null, null, null, null,
                    null, null, null, null, null, null, 0L, null, null,
                    null, null, null, null, null, session, replacement,
                    out DailyV1P12FCoreOwnerRestoreStage reservedOwners,
                    out P12AResult reservedResult), Is.False);
            Assert.That(reservedOwners, Is.Null);
            Assert.That(reservedResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue),
                "With the exact replacement reservation, Core proceeds to validate the detached P12-C roots first.");
        }

        DailyV1Application initialApplication = new DailyV1Application();
        Assert.That(initialApplication.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        Assert.That(initialApplication.TryStageRestoredP12FOwners(
                null, null, null, null, null, null,
                null, null, null, null, null, null, 0L, null, null,
                null, null, null, null, null, null, null,
                out DailyV1P12FCoreOwnerRestoreStage initialOwners,
                out P12AResult initialResult), Is.False);
        Assert.That(initialOwners, Is.Null);
        Assert.That(initialResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue),
            "Initial P12-F staging is admitted only on the unused app's captured bootstrap thread, then validates its roots.");
    }
}
