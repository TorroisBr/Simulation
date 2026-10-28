using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class DailyV1ApplicationSessionGateTests
{
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

    private static void AssertInitialNoSaveWasProven(DailyV1Application application)
    {
        Assert.That(application.TryInspectInitialContinuationSlot(
            "application-session-gate-no-save",
            new MissingContinuationSlotOperations(),
            out bool exists,
            out P12AResult result), Is.True);
        Assert.That(exists, Is.False);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.NoSave));
    }

    private sealed class TestSession
    {
        internal readonly DailyV1ExecutionBoundaryState ExecutionState;

        internal TestSession(DailyV1ExecutionBoundaryState executionState = null)
        {
            ExecutionState = executionState ?? new DailyV1ExecutionBoundaryState();
        }
    }

    private sealed class ApplicationAdvanceBoundary : IDailyV1RuntimeAdvanceBoundary,
        IDailyV1SelectedRuntimeAdvanceHostPorts
    {
        private readonly DailyV1ExecutionBoundaryState state;
        private readonly Action onOperationEntry;
        private IReadOnlyList<OwnerSectionCensusSnapshot> admissionOwnerSections;
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
            this.admissionOwnerSections = admissionOwnerSections;
            this.admissionMutationEpoch = admissionMutationEpoch;
        }

        internal bool OperationEntered { get; private set; }
        internal bool SelectedBoundaryRequested { get; private set; }
        internal int LegacyAdvanceInvocationCount { get; private set; }
        internal bool Faulted { get; private set; }
        internal string RequestedRuntimeProtocolOperationId { get; private set; }
        internal bool? RequestedSessionReplacement { get; private set; }
        internal DailyV1CaptureScopeAllowance? RequestedCaptureScopeAllowance { get; private set; }
        internal int OwnerCensusCaptureCount { get; private set; }
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
            admissionOwnerSections = ownerSections;
            admissionMutationEpoch = mutationEpoch;
        }
        public DailyV1ExecutionBoundaryState ExecutionState => state;
        public bool HasIntradayTimeline => false;

        public bool TryBindApplicationAdvanceAuthority(
            DailyV1Application application,
            DailyV1ActiveRuntimeSession session) =>
            application != null
            && session != null
            && ReferenceEquals(session.ExecutionBoundaryState, state);

        public bool TryEnterDailyAdvanceOperation(out IDisposable operationScope)
            => TryEnterRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance,
                out operationScope);

        public bool TryEnterRuntimeProtocolOperation(
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
            SelectedBoundaryRequested = true;
            executionBoundary = null;
            return false;
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
            failure = admissionOwnerSections == null
                ? DailyCaptureEligibilityFailure.OperationInProgress
                : DailyCaptureEligibilityFailure.None;
            return admissionOwnerSections != null;
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

    private static bool TryPrepareInitialGenesisForTest(
        DailyV1Application targetApplication,
        DailyV1EffectiveDefinitions definitions,
        Action<string> stageCompleted,
        out DailyV1GenesisComposition composition,
        out DailyV1ActiveRuntimeSession session,
        out string diagnostic)
    {
        string absentRoot = System.IO.Path.Combine(
            System.IO.Directory.GetCurrentDirectory(),
            ".core-genesis-no-save-" + System.Guid.NewGuid().ToString("N"));
        if (targetApplication.TryInspectInitialContinuationSlot(
                absentRoot, SystemAtomicFileSlotOperations.Instance,
                out bool exists, out P12AResult result))
        {
            Assert.That(exists, Is.False);
            Assert.That(result.Code, Is.EqualTo(P12AResultCode.NoSave));
        }
        return targetApplication.TryPrepareInitialGenesis(
            definitions, stageCompleted, out composition, out session, out diagnostic);
    }

    private static bool TryPrepareInitialGenesisForTest(
        DailyV1Application targetApplication,
        DailyV1EffectiveDefinitions definitions,
        Func<WorldId> allocateWorldId,
        Action<string> stageCompleted,
        out DailyV1GenesisComposition composition,
        out DailyV1ActiveRuntimeSession session,
        out string diagnostic)
    {
        string absentRoot = System.IO.Path.Combine(
            System.IO.Directory.GetCurrentDirectory(),
            ".core-genesis-no-save-" + System.Guid.NewGuid().ToString("N"));
        if (targetApplication.TryInspectInitialContinuationSlot(
                absentRoot, SystemAtomicFileSlotOperations.Instance,
                out bool exists, out P12AResult result))
        {
            Assert.That(exists, Is.False);
            Assert.That(result.Code, Is.EqualTo(P12AResultCode.NoSave));
        }
        return targetApplication.TryPrepareInitialGenesis(
            definitions, allocateWorldId, stageCompleted,
            out composition, out session, out diagnostic);
    }

    private static DailyV1Application CreateApplicationWithAdvanceBoundary(
        Action onOperationEntry,
        out DailyV1ActiveRuntimeSession session,
        out ApplicationAdvanceBoundary boundary,
        DailyV1EffectiveDefinitions definitions = null)
    {
        DailyV1Application application = new DailyV1Application();
        definitions ??= DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        Assert.That(TryPrepareInitialGenesisForTest(application,
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out session, out string diagnostic), Is.True, diagnostic);
        EnsureRuntimeProtocolInventory(session.ExecutionBoundaryState);
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
        sections ??= CreateCoreBoundCompleteOwnerSectionVector(session);
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
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new PoliticalKnowledgeStoreCensusProvider(politicalKnowledge).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new ActorChoiceP11CensusProvider(actorChoices).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new ScheduledDirectiveCensusProvider(graph.ScheduledDirectives).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new TravelPartyCensusProvider(graph.TravelParties).GetCurrentCensus());
        ReplaceOwnerSectionWithCurrentCensus(sections,
            new ExpeditionCensusProvider(graph.Expeditions).GetCurrentCensus());
        return sections;
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
        return ReplaceCityOwnerSectionVector(session, existing);
    }

    private static OwnerSectionCensusSnapshot[] CreateCoreBoundCompleteOwnerSectionVector(
        DailyV1ActiveRuntimeSession session)
        => ReplaceCityOwnerSectionVector(
            session, DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot());

    private static OwnerSectionCensusSnapshot[] ReplaceCityOwnerSectionVector(
        DailyV1ActiveRuntimeSession session,
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

        foreach (DailyV1CityRuntimeOwners city in session.OwnerGraph.Cities)
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

        boundary.SetContinuationSaveEvidenceCaptureCallback(captureCount =>
        {
            if (captureCount != 5) return;
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
        boundary.SetContinuationSaveEvidenceCaptureCallback(captureCount =>
        {
            if (captureCount != 4) return;
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
        AssertInitialNoSaveWasProven(application);
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
        Assert.That(application.TryPublishInitial(session), Is.True);
        Assert.That(application.ActiveSession, Is.SameAs(session));
    }

    [Test]
    public void CoreRestoreInitialPublicationRequiresTheExactApplicationLoadedImage()
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
            null, candidate, null, out diagnostic), Is.False,
            "Fresh target census alone cannot replace proof that the application loaded this exact initial P12 image.");
        Assert.That(diagnostic, Does.Contain("successfully loaded P12 continuation image"));
        Assert.That(application.ActiveSession, Is.Null);
        Assert.That(boundary.RequestedSessionReplacement, Is.EqualTo(false));
    }

    [Test]
    public void CoreRestoreReplacementAcceptsRestoredSourceTokenAndRejectsStaleSourceCensus()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext bootstrapAdmission), Is.True);
        AssertInitialNoSaveWasProven(application);
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
        Assert.That(application.TryPublishInitial(sourceSession), Is.True,
            "The replacement test publishes its synthetic source boundary after the explicit NoSave fixture proof.");
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
    public void ProductionGenesisPreparationAllocatesWorldIdentityInsideCore()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        var completedStages = new System.Collections.Generic.List<string>();

        Assert.That(TryPrepareInitialGenesisForTest(application,
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
    public void DirectInitialPublicationRequiresSuccessfulNoSaveInspection()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext admission), Is.True);
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions, admission,
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            composition, 0L,
            out DailyV1ActiveRuntimeSession candidate, out diagnostic), Is.True, diagnostic);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(
            candidate, new ApplicationAdvanceBoundary(candidate.ExecutionBoundaryState, null)), Is.True);

        Assert.That(application.TryPublishInitial(candidate), Is.False,
            "A bound session cannot bypass the initial P12 slot inspection by publishing without the prepared genesis path.");
        Assert.That(application.ActiveSession, Is.Null);
    }

    [Test]
    public void ApplicationPublishesTheExactCoreGenesisOwnerGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        var completedStages = new System.Collections.Generic.List<string>();

        Assert.That(TryPrepareInitialGenesisForTest(application,
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

        Assert.That(TryPrepareInitialGenesisForTest(application,
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
        bool callbackInvoked = false;
        bool reentrantPublicationAccepted = false;
        DailyV1ActiveRuntimeSession reentrantCandidate = null;

        Assert.That(TryPrepareInitialGenesisForTest(application,
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

        Assert.Throws<System.InvalidOperationException>(() => TryPrepareInitialGenesisForTest(application,
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
            out SimulationRuntimeAdvanceFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(boundary.OperationEntered, Is.True);
        Assert.That(boundary.RequestedRuntimeProtocolOperationId,
            Is.EqualTo(DailyV1RuntimeProtocolOperationContracts.DailyAdvance),
            "The selected Core runtime names the P12 operation that the host protocol admits.");
        Assert.That(boundary.SelectedBoundaryRequested, Is.True);
        Assert.That(boundary.LegacyAdvanceInvocationCount, Is.Zero,
            "The selected Core session cannot delegate a failed stage to the host day or intraday advance.");
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
        Assert.That(boundary.SelectedBoundaryRequested, Is.True);
        Assert.That(boundary.LegacyAdvanceInvocationCount, Is.Zero,
            "A selected multi-day Core session cannot delegate to the host day or intraday advance.");
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
    public void ApplicationBindsCoreBoundaryToOneExactSelectedHostPortSetBeforePublication()
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        Assert.That(TryPrepareInitialGenesisForTest(application,
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
        hostPorts.SetAdmissionEvidence(
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot(), mutationEpoch: 17L);

        Assert.That(coreBoundary.TryCaptureCompletedDailyCaptureEvidence(
            out _, out _, out DailyCaptureEligibilityFailure failure), Is.False,
            "Core must reject completed-advance evidence unless its exact DailyAdvance scope is active.");
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress));
        Assert.That(hostPorts.OwnerCensusCaptureCount, Is.Zero,
            "A rejected Core scope cannot ask the host to read or reserve the P12 census.");

        Assert.That(coreBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
            sessionReplacement: false, out _, out _, out failure), Is.True);
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
    public void ApplicationRejectsAnotherGenesisAfterPublishingItsInitialSession()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(TryPrepareInitialGenesisForTest(application,
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out string diagnostic), Is.True, diagnostic);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(
            candidate, new ApplicationAdvanceBoundary(candidate.ExecutionBoundaryState, null)), Is.True);
        Assert.That(application.TryPublishInitial(candidate), Is.True);
        DailyV1ActiveRuntimeSession published = application.ActiveSession;

        Assert.That(TryPrepareInitialGenesisForTest(application,
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
        AssertInitialNoSaveWasProven(application);
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
        AssertInitialNoSaveWasProven(application);
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
                null, null, null, null, null, null, null, 0L, session, null,
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
                    null, null, null, null, null, null, null, 0L, session, replacement,
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
                null, null, null, null, null, null, null, 0L, null, null,
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
