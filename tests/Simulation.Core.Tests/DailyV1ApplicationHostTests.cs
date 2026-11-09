using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

public sealed class DailyV1ApplicationHostTests
{
    private sealed class MissingSlotOperations : IAtomicFileSlotOperations
    {
        public void CreateDirectory(string path) => throw new NotSupportedException();
        public bool Exists(string path) => false;
        public System.IO.Stream CreateNewTemporary(string path) => throw new NotSupportedException();
        public System.IO.Stream OpenRead(string path) => throw new System.IO.FileNotFoundException();
        public void FlushToDisk(System.IO.Stream stream) => throw new NotSupportedException();
        public void Replace(string source, string destination) => throw new NotSupportedException();
        public void Move(string source, string destination) => throw new NotSupportedException();
        public void Delete(string path) => throw new NotSupportedException();
    }

    private sealed class UnavailableSlotOperations : IAtomicFileSlotOperations
    {
        public void CreateDirectory(string path) => throw new NotSupportedException();
        public bool Exists(string path) => throw new System.UnauthorizedAccessException();
        public System.IO.Stream CreateNewTemporary(string path) => throw new NotSupportedException();
        public System.IO.Stream OpenRead(string path) => throw new System.UnauthorizedAccessException();
        public void FlushToDisk(System.IO.Stream stream) => throw new NotSupportedException();
        public void Replace(string source, string destination) => throw new NotSupportedException();
        public void Move(string source, string destination) => throw new NotSupportedException();
        public void Delete(string path) => throw new NotSupportedException();
    }
    private sealed class CorruptSlotOperations : IAtomicFileSlotOperations
    {
        public void CreateDirectory(string path) => throw new NotSupportedException();
        public bool Exists(string path) => true;
        public System.IO.Stream CreateNewTemporary(string path) => throw new NotSupportedException();
        public System.IO.Stream OpenRead(string path) => new System.IO.MemoryStream(new byte[] { 0x53 });
        public void FlushToDisk(System.IO.Stream stream) => throw new NotSupportedException();
        public void Replace(string source, string destination) => throw new NotSupportedException();
        public void Move(string source, string destination) => throw new NotSupportedException();
        public void Delete(string path) => throw new NotSupportedException();
    }

    private sealed class TestHostProjection : IDailyV1ApplicationHostProjection
    {
        public bool TryCreateSelectedTravelPartyStartHostOperation(
            DailyV1SelectedTravelPartyStartPlan plan,
            out IDailyV1SelectedTravelPartyStartHostOperation operation)
        {
            operation = null;
            return false;
        }
    }

    private sealed class ReentrantMissingSlotOperations : IAtomicFileSlotOperations
    {
        private readonly DailyV1Application application;
        private readonly DailyV1EffectiveDefinitions definitions;

        internal ReentrantMissingSlotOperations(
            DailyV1Application application,
            DailyV1EffectiveDefinitions definitions)
        {
            this.application = application;
            this.definitions = definitions;
        }

        internal bool ReentrantGenesisAccepted { get; private set; }
        internal string ReentrantDiagnostic { get; private set; }

        public void CreateDirectory(string path) => throw new NotSupportedException();
        public bool Exists(string path) => false;
        public System.IO.Stream CreateNewTemporary(string path) => throw new NotSupportedException();
        public System.IO.Stream OpenRead(string path)
        {
            string diagnostic;
            ReentrantGenesisAccepted = application.TryPrepareInitialGenesis(
                definitions, allocateWorldId: null, stageCompleted: null,
                out _, out _, out diagnostic);
            ReentrantDiagnostic = diagnostic;
            throw new System.IO.FileNotFoundException();
        }
        public void FlushToDisk(System.IO.Stream stream) => throw new NotSupportedException();
        public void Replace(string source, string destination) => throw new NotSupportedException();
        public void Move(string source, string destination) => throw new NotSupportedException();
        public void Delete(string path) => throw new NotSupportedException();
    }

    internal static void AdmitMissingSlot(DailyV1Application application)
    {
        Assert.That(application.TryInspectInitialContinuationSlotForStart(
            "missing-slot-root", new MissingSlotOperations(),
            out bool exists, out P12AResult result), Is.True);
        Assert.That(exists, Is.False);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.NoSave));
    }

    private static P12AContinuationImage AdmissionOnlyImage()
    {
        P12ACompatibilityDescriptor identity = UnityIdentity();
        var header = new P12AContinuationHeader(
            System.Guid.Parse("f063489a-4fd1-430c-8fe8-51f8d1f6f3bf").ToString("D"),
            0L, 1L, -1L, "admission-fixture-p9-stage",
            identity.P9Fingerprint, "admission-fixture-p8-profile",
            identity.P8Fingerprint, 0L, identity);
        return new P12AContinuationImage(
            header, new P12ACausalRootPayload(null, null),
            Array.Empty<P12AOwnerSection>());
    }

    private static void AssertInitialRestoreAdmissionRejected(
        DailyV1Application application,
        DailyV1ApplicationStartAdmission admission,
        P12AContinuationImage image,
        DailyV1EffectiveDefinitions definitions,
        SimulationRuntimeAdmissionContext bootstrapContext)
    {
        int imageDecoded = 0;
        int stageCallbacks = 0;
        int rootProjections = 0;
        int hostPreparations = 0;
        int ownerProjections = 0;
        bool accepted = application.TryRunRestoredImageCoreOwnerTransaction(
            image, UnityIdentity(), null, definitions, bootstrapContext,
            expectedSession: null, replacementOperation: null,
            initialStartAdmission: admission,
            createDiagnosticLogger: () => new SimulationLogger(
                new SimulationLogSettings(), "restore-admission-test", (_, __) => { }),
            lastEconomySnapshotDay: 0,
            imageDecoded: () => imageDecoded++,
            stageCompleted: _ => stageCallbacks++,
            projectCoreRoots: _ => { rootProjections++; return null; },
            prepareHostProjection: _ => { hostPreparations++; return null; },
            projectCompletedCoreOwnerStage: (_, __) => { ownerProjections++; return null; },
            p12EOwnerStageObserver: null,
            p12FOwnerStageObserver: null,
            out bool admissionPassed, out bool rootsPrepared,
            out P12ADailyV1Census census,
            out P12ADailyV1ImageOwnerSnapshots snapshots,
            out DailyV1RestoredImageCoreOwnerStages stages,
            out DailyV1RestoredImageCoreOwnerStageBoundary failedBoundary,
            out P12AResult result,
            out string diagnostic);

        Assert.That(accepted, Is.False, diagnostic);
        Assert.That(admissionPassed, Is.False);
        Assert.That(rootsPrepared, Is.False);
        Assert.That(census, Is.Null);
        Assert.That(snapshots, Is.Null);
        Assert.That(stages, Is.Null);
        Assert.That(failedBoundary, Is.EqualTo(DailyV1RestoredImageCoreOwnerStageBoundary.None));
        Assert.That(result?.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
        Assert.That(result?.SectionCode, Is.EqualTo("p12.initial-start-admission"));
        Assert.That(imageDecoded, Is.Zero);
        Assert.That(stageCallbacks, Is.Zero);
        Assert.That(rootProjections, Is.Zero);
        Assert.That(hostPreparations, Is.Zero);
        Assert.That(ownerProjections, Is.Zero);
        Assert.That(application.ActiveSession, Is.Null);
    }

    [Test]
    public void HostPortsAndDotNetIdentityAreCapturedWithoutPersistingTheStorageRoot()
    {
        const string storageRoot = "private-test-root";
        DailyV1HostEnvironmentIdentity identity = DotNetIdentity();
        P12ACompatibilityDescriptorV2 descriptor = ToDescriptor(identity);
        var operations = SystemAtomicFileSlotOperations.Instance;
        Action<string> stage = _ => { };
        Action<DailyV1LogSeverity, string> log = (_, __) => { };

        var host = new DailyV1ApplicationHost(storageRoot, operations, stage, log, identity);

        Assert.That(host, Is.Not.Null);
        Assert.That(identity.HostKind, Is.EqualTo(1));
        Assert.That(identity.RuntimeFrameworkDescription, Is.EqualTo(".NET 8.0"));
        Assert.That(identity.RuntimeVersion, Is.EqualTo("8.0.1.0"));
        Assert.That(identity.Assemblies, Has.Count.EqualTo(1));
        Assert.That(ContainsSubsequence(descriptor.CanonicalBytes, Encoding.UTF8.GetBytes(storageRoot)), Is.False,
            "A host storage root must never enter the serialized compatibility identity.");
    }

    [Test]
    public void InitialSlotInspectionAdmitsGenesisOnlyWhenTheCoreSlotIsMissing()
    {
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryInspectInitialContinuationSlotForStart(
            "missing-slot-root", new MissingSlotOperations(), out bool exists, out P12AResult result), Is.True);
        Assert.That(exists, Is.False);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.NoSave));

        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out string diagnostic), Is.True, diagnostic);
        Assert.That(candidate, Is.Not.Null);
    }

    [Test]
    public void RestoredOwnerStageCompositionRejectsDifferentEffectiveConfigurationReference()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        EffectiveSimulationConfiguration differentConfiguration = SimulationConfigurationDefaults.Create();
        Assert.That(ReferenceEquals(definitions.EffectiveConfiguration, differentConfiguration), Is.False);

        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCreateRestoredActiveRuntimeSessionFromOwnerStages(
            definitions,
            differentConfiguration,
            null, null, null, null,
            null, null, null, null,
            0L, null,
            out DailyV1ActiveRuntimeSession candidate,
            out SimulationCalendar calendar,
            out string diagnostic), Is.False);
        Assert.That(candidate, Is.Null);
        Assert.That(calendar, Is.Null);
        Assert.That(diagnostic, Does.Contain("exact admitted definitions and effective configuration"));
    }

    [Test]
    public void InitialGenesisRequiresCompletedNoSaveAdmission()
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        int worldIdAllocations = 0;

        Assert.That(application.TryPrepareInitialGenesis(
            definitions,
            allocateWorldId: () =>
            {
                worldIdAllocations++;
                return new WorldId(System.Guid.Parse("bf5a2935-429c-40b6-9b22-8c2a2bc6b177"));
            },
            stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession rejectedCandidate,
            out string rejectionDiagnostic), Is.False);
        Assert.That(rejectedCandidate, Is.Null);
        Assert.That(rejectionDiagnostic, Does.Contain("NoSave"));
        Assert.That(worldIdAllocations, Is.Zero,
            "Genesis rejection before slot admission cannot allocate a WorldId.");

        AdmitMissingSlot(application);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out string diagnostic), Is.True, diagnostic);
        Assert.That(candidate, Is.Not.Null,
            "A completed NoSave result remains the permitted genesis path.");
    }

    [Test]
    public void InitialSlotReadBlocksReentrantGenesisUntilNoSaveIsKnown()
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        var operations = new ReentrantMissingSlotOperations(application, definitions);
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "reentrant-missing-slot-root", operations, null, null,
            unityV1Identity: UnityIdentity());

        Assert.That(application.TryLoadInitialContinuationImageForStart(
            null, host, out P12AContinuationImage image, out P12AResult loadResult), Is.False);
        Assert.That(image, Is.Null);
        Assert.That(loadResult.Code, Is.EqualTo(P12AResultCode.NoSave));
        Assert.That(operations.ReentrantGenesisAccepted, Is.False);
        Assert.That(operations.ReentrantDiagnostic, Does.Contain("cannot capture a second bootstrap"));

        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out string diagnostic), Is.True, diagnostic);
        Assert.That(candidate, Is.Not.Null,
            "Genesis may proceed after the in-progress slot read completes as NoSave.");
    }

    [Test]
    public void InitialSlotInspectionWithoutCompatibilityIdentityBlocksWhenSlotIsPresent()
    {
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryInspectInitialContinuationSlotForStart(
            "present-slot-root", new CorruptSlotOperations(), out bool exists, out P12AResult result), Is.True);
        Assert.That(exists, Is.True);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.Loaded));

        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out string diagnostic), Is.False);
        Assert.That(candidate, Is.Null);
        Assert.That(diagnostic, Does.Contain("cannot capture a second bootstrap"));
    }

    [Test]
    public void InitialSlotInspectionWithoutCompatibilityIdentityFailsClosedOnStorageError()
    {
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryInspectInitialContinuationSlotForStart(
            "unavailable-slot-root", new UnavailableSlotOperations(), out bool exists, out P12AResult result), Is.False);
        Assert.That(exists, Is.False);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.StorageFailure));

        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out string diagnostic), Is.False);
        Assert.That(candidate, Is.Null);
    }
    [Test]
    public void InitialRestoredImageStagingRejectsMissingAdmissionBeforeDecoding()
    {
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext bootstrapContext), Is.True);

        AssertInitialRestoreAdmissionRejected(application, null, AdmissionOnlyImage(),
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), bootstrapContext);
    }

    [Test]
    public void InitialRestoredImageStagingRejectsForeignGenesisAndWrongImageAdmissionsBeforeDecoding()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        P12AContinuationImage admittedImage = AdmissionOnlyImage();
        P12AContinuationImage wrongImage = AdmissionOnlyImage();

        DailyV1Application target = new DailyV1Application();
        Assert.That(target.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext targetContext), Is.True);
        DailyV1Application foreign = new DailyV1Application();
        Assert.That(foreign.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext foreignContext), Is.True);

        DailyV1ApplicationStartAdmission foreignRestore = new DailyV1ApplicationStartAdmission(
            foreign, DailyV1ApplicationStartAdmissionKind.RestoreRequired,
            admittedImage, new P12AResult(P12AResultCode.Loaded));
        AssertInitialRestoreAdmissionRejected(target, foreignRestore, admittedImage,
            definitions, targetContext);

        DailyV1ApplicationStartAdmission wrongImageRestore = new DailyV1ApplicationStartAdmission(
            target, DailyV1ApplicationStartAdmissionKind.RestoreRequired,
            admittedImage, new P12AResult(P12AResultCode.Loaded));
        AssertInitialRestoreAdmissionRejected(target, wrongImageRestore, wrongImage,
            definitions, targetContext);

        DailyV1Application genesisApplication = new DailyV1Application();
        Assert.That(genesisApplication.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext genesisContext), Is.True);
        DailyV1ApplicationStartAdmission genesisAdmission = new DailyV1ApplicationStartAdmission(
            genesisApplication, DailyV1ApplicationStartAdmissionKind.GenesisRequired,
            null, new P12AResult(P12AResultCode.NoSave));
        AssertInitialRestoreAdmissionRejected(genesisApplication, genesisAdmission,
            admittedImage, definitions, genesisContext);
    }

    [Test]
    public void InitialNoSaveAdmissionTokenIsBoundToOneApplicationAndExactResult()
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "missing-slot-root", new MissingSlotOperations(), null, null,
            unityV1Identity: UnityIdentity());
        Assert.That(application.TryAdmitInitialStart(
            null, host, out DailyV1ApplicationStartAdmission admission,
            out P12AResult loadResult, out string admissionDiagnostic),
            Is.True, admissionDiagnostic);

        Assert.That(admission, Is.Not.Null);
        Assert.That(admission.Application, Is.SameAs(application));
        Assert.That(admission.Kind, Is.EqualTo(DailyV1ApplicationStartAdmissionKind.GenesisRequired));
        Assert.That(admission.RestoreImage, Is.Null);
        Assert.That(admission.PersistenceResult, Is.SameAs(loadResult));
        Assert.That(loadResult.Code, Is.EqualTo(P12AResultCode.NoSave));

        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        SimulationLogger logger = new SimulationLogger(
            new SimulationLogSettings(), "missing-slot-root", (_, __) => { });
        DailyV1Application otherApplication = new DailyV1Application();
        Assert.That(otherApplication.TryPrepareInitialGenesisStart(
            admission, definitions, logger, stageCompleted: null,
            out DailyV1ApplicationStartPreparation wrongPreparation,
            out string wrongDiagnostic), Is.False);
        Assert.That(wrongPreparation, Is.Null);
        Assert.That(wrongDiagnostic, Does.Contain("owned by this application"));

        Assert.That(application.TryPrepareInitialGenesisStart(
            admission, definitions, logger, stageCompleted: null,
            out DailyV1ApplicationStartPreparation preparation,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(preparation, Is.Not.Null);
        Assert.That(preparation.Application, Is.SameAs(application));
        Assert.That(preparation.Kind, Is.EqualTo(DailyV1ApplicationStartPreparationKind.GenesisPrepared));
        Assert.That(preparation.PersistenceResult, Is.SameAs(loadResult),
            "Genesis preparation must carry the exact NoSave object returned by initial slot admission.");
        Assert.That(preparation.CoreSession, Is.Not.Null);
        Assert.That(application.ActiveSession, Is.Null,
            "Core genesis preparation remains unpublished until the host boundary is admitted.");
    }

    [Test]
    public void InspectedNoSaveCanBeBoundToTheSameApplicationGenesisAdmission()
    {
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryInspectInitialContinuationSlotForStart(
            "missing-slot-root", new MissingSlotOperations(),
            out bool exists, out P12AResult result), Is.True);
        Assert.That(exists, Is.False);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.NoSave));

        Assert.That(application.TryCreateInitialStartAdmissionFromNoSave(
            out DailyV1ApplicationStartAdmission admission, out string admissionDiagnostic),
            Is.True, admissionDiagnostic);
        Assert.That(admission.PersistenceResult, Is.SameAs(result));

        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryPrepareInitialGenesisStart(
            admission, definitions,
            new SimulationLogger(new SimulationLogSettings(), "missing-slot-root", (_, __) => { }),
            stageCompleted: null,
            out DailyV1ApplicationStartPreparation preparation,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(preparation.PersistenceResult, Is.SameAs(result));
    }
    [Test]
    public void RestoredAdmissionIdentityIsDerivedByTheApplicationFromItsBootstrapLineage()
    {
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext bootstrap), Is.True);

        Assert.That(application.TryCreateRestoredContinuationAdmissionContext(
            null, null, out SimulationRuntimeAdmissionContext restored, out string diagnostic),
            Is.True, diagnostic);

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored, Is.Not.SameAs(bootstrap));
        Assert.That(restored.IsRestoredContinuation, Is.True);
        Assert.That(restored.ExpectedOwnerThread, Is.SameAs(bootstrap.ExpectedOwnerThread));
        Assert.That(restored.ExpectedOwnerThreadId, Is.EqualTo(bootstrap.ExpectedOwnerThreadId));
        Assert.That(restored.IsRestoredContinuationOf(bootstrap), Is.True);
        Assert.That(restored.IsOwnedByCurrentThread(), Is.True);
    }

    [Test]
    public void RestoredAdmissionIdentityCannotBeCreatedAfterGenesisPreparation()
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        AdmitMissingSlot(application);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out string genesisDiagnostic),
            Is.True, genesisDiagnostic);
        Assert.That(candidate, Is.Not.Null);

        Assert.That(application.TryCreateRestoredContinuationAdmissionContext(
            null, null, out SimulationRuntimeAdmissionContext restored, out string diagnostic),
            Is.False);
        Assert.That(restored, Is.Null);
        Assert.That(diagnostic, Does.Contain("staging authority"));
    }

    [Test]
    public void InvalidInitialHostBlocksGenesisOnTheSameApplication()
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();

        Assert.That(application.TryLoadInitialContinuationImageForStart(
            definitions, null, out P12AContinuationImage image, out P12AResult loadResult), Is.False);
        Assert.That(image, Is.Null);
        Assert.That(loadResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));

        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out string diagnostic), Is.False);
        Assert.That(candidate, Is.Null);
        Assert.That(diagnostic, Does.Contain("cannot capture a second bootstrap"));
    }

    [Test]
    public void CoreStartPreparationOnExistingApplicationRetainsExactApplicationAndAdmission()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "start-existing-application-root", new MissingSlotOperations(),
            stageCompleted: null, logOutput: null, unityV1Identity: UnityIdentity());

        Assert.That(application.TryPrepareStartOnExistingApplication(
            definitions, host,
            out DailyV1ApplicationStartPreparation preparation,
            out P12AResult persistenceResult,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(preparation, Is.Not.Null);
        Assert.That(preparation.Application, Is.SameAs(application));
        Assert.That(preparation.StartAdmission.Application, Is.SameAs(application));
        Assert.That(preparation.StartAdmission.Kind,
            Is.EqualTo(DailyV1ApplicationStartAdmissionKind.GenesisRequired));
        Assert.That(preparation.PersistenceResult, Is.SameAs(persistenceResult));
        Assert.That(preparation.StartAdmission.PersistenceResult, Is.SameAs(persistenceResult));
        Assert.That(preparation.CoreSession, Is.Not.Null);
        Assert.That(application.ActiveSession, Is.Null,
            "Preparing the existing application must not publish its unbound session.");
    }
    [Test]
    public void CoreStartOnExistingApplicationCommitsExactSessionAfterHostProjection()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "start-existing-application-root", new MissingSlotOperations(),
            stageCompleted: null, logOutput: null, unityV1Identity: UnityIdentity());
        TestHostProjection projection = new TestHostProjection();
        int factoryCalls = 0;
        DailyV1ApplicationStartPreparation factoryPreparation = null;

        bool ProjectExactPreparation(
            DailyV1ApplicationStartPreparation candidatePreparation,
            out DailyV1ActiveRuntimeSession candidateSession,
            out IDailyV1ApplicationHostProjection candidateProjection,
            out IDailyV1SelectedRuntimeAdvanceHostPorts candidateHostPorts,
            out string diagnostic)
        {
            factoryCalls++;
            factoryPreparation = candidatePreparation;
            candidateSession = candidatePreparation.CoreSession;
            candidateProjection = projection;
            candidateHostPorts = new MinimalSelectedRuntimeAdvanceHostPorts(
                candidateSession.ExecutionBoundaryState);
            diagnostic = null;
            return true;
        }

        Assert.That(application.TryStartOnExistingApplication(
            definitions, host, ProjectExactPreparation,
            out DailyV1ApplicationStartPreparation preparation,
            out P12AResult persistenceResult,
            out string startDiagnostic), Is.True, startDiagnostic);

        Assert.That(factoryCalls, Is.EqualTo(1));
        Assert.That(preparation, Is.Not.Null);
        Assert.That(factoryPreparation, Is.SameAs(preparation));
        Assert.That(preparation.Application, Is.SameAs(application));
        Assert.That(preparation.StartAdmission.Kind,
            Is.EqualTo(DailyV1ApplicationStartAdmissionKind.GenesisRequired));
        Assert.That(preparation.Kind, Is.EqualTo(DailyV1ApplicationStartPreparationKind.GenesisPrepared));
        Assert.That(preparation.PersistenceResult, Is.SameAs(persistenceResult));
        Assert.That(persistenceResult.Code, Is.EqualTo(P12AResultCode.NoSave));
        Assert.That(preparation.CoreSession.Definitions, Is.SameAs(definitions));
        Assert.That(application.ActiveSession, Is.SameAs(preparation.CoreSession));
        Assert.That(application.TryGetPublishedHostProjection(
            preparation.CoreSession, out IDailyV1ApplicationHostProjection publishedProjection), Is.True);
        Assert.That(publishedProjection, Is.SameAs(projection));
    }

    [Test]
    public void CoreStartOnExistingApplicationLeavesSessionUnpublishedWhenHostProjectionFails()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "start-existing-application-failure-root", new MissingSlotOperations(),
            stageCompleted: null, logOutput: null, unityV1Identity: UnityIdentity());

        bool RejectProjection(
            DailyV1ApplicationStartPreparation preparation,
            out DailyV1ActiveRuntimeSession candidateSession,
            out IDailyV1ApplicationHostProjection candidateProjection,
            out IDailyV1SelectedRuntimeAdvanceHostPorts candidateHostPorts,
            out string diagnostic)
        {
            candidateSession = preparation.CoreSession;
            candidateProjection = null;
            candidateHostPorts = null;
            diagnostic = "Unity rejected the exact host projection.";
            return false;
        }

        Assert.That(application.TryStartOnExistingApplication(
            definitions, host, RejectProjection,
            out DailyV1ApplicationStartPreparation preparation,
            out P12AResult persistenceResult,
            out string startDiagnostic), Is.False);
        Assert.That(startDiagnostic, Does.Contain("Unity rejected"));
        Assert.That(preparation, Is.Not.Null);
        Assert.That(preparation.Application, Is.SameAs(application));
        Assert.That(persistenceResult.Code, Is.EqualTo(P12AResultCode.NoSave));
        Assert.That(application.ActiveSession, Is.Null,
            "A failed Unity projection must not publish the prepared Core session.");
        Assert.That(application.TryGetPublishedHostProjection(
            preparation.CoreSession, out _), Is.False);
    }
    private static OwnerSectionCensusSnapshot ToRequiredSnapshot(
        OwnerSectionCensusWitness witness)
    {
        return new OwnerSectionCensusSnapshot(
            witness.SectionId,
            witness.SchemaVersion,
            OwnerSectionRole.Required,
            witness.OwnerInstanceIdentity,
            witness.Cardinality,
            witness.Revision);
    }

    [Test]
    public void CoreRestoredTargetInvariantGateUsesExactCoreExpeditionOwnerAndAllocator()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "restore-target-invariant-root", new MissingSlotOperations(),
            stageCompleted: null, logOutput: null, unityV1Identity: UnityIdentity());
        Assert.That(DailyV1Application.TryPrepareStart(
            definitions, host,
            out DailyV1ApplicationStartPreparation preparation,
            out _, out string startDiagnostic), Is.True, startDiagnostic);

        DailyV1ActiveRuntimeSession session = preparation.CoreSession;
        OwnerSectionCensusWitness expeditionWitness = new ExpeditionCensusProvider(
            session.OwnerGraph.Expeditions).GetCurrentCensus();
        ActorChoiceStore actorChoices = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        OwnerSectionCensusWitness p11Witness = new ActorChoiceP11CensusProvider(actorChoices).GetCurrentCensus();
        List<OwnerSectionCensusSnapshot> targetSections = new List<OwnerSectionCensusSnapshot>
        {
            ToRequiredSnapshot(expeditionWitness),
            ToRequiredSnapshot(p11Witness)
        };

        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, targetSections, out string diagnostic), Is.True, diagnostic);

        List<OwnerSectionCensusSnapshot> invalidExpeditionSections =
            new List<OwnerSectionCensusSnapshot>(targetSections);
        invalidExpeditionSections[0] = new OwnerSectionCensusSnapshot(
            expeditionWitness.SectionId, expeditionWitness.SchemaVersion, OwnerSectionRole.Required,
            expeditionWitness.OwnerInstanceIdentity, expeditionWitness.Cardinality + 1, expeditionWitness.Revision);
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, invalidExpeditionSections, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("cardinality zero"));

        List<OwnerSectionCensusSnapshot> invalidP11RoleSections =
            new List<OwnerSectionCensusSnapshot>(targetSections);
        invalidP11RoleSections[1] = new OwnerSectionCensusSnapshot(
            p11Witness.SectionId, p11Witness.SchemaVersion, OwnerSectionRole.ExplicitlyEmpty,
            p11Witness.OwnerInstanceIdentity, p11Witness.Cardinality, p11Witness.Revision);
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, invalidP11RoleSections, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("Required P11 ActorChoice owner"));

        List<OwnerSectionCensusSnapshot> invalidP11IdentitySections =
            new List<OwnerSectionCensusSnapshot>(targetSections);
        invalidP11IdentitySections[1] = new OwnerSectionCensusSnapshot(
            p11Witness.SectionId, p11Witness.SchemaVersion, OwnerSectionRole.Required,
            new object(), p11Witness.Cardinality, p11Witness.Revision);
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, invalidP11IdentitySections, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("Required P11 ActorChoice owner"));

        List<OwnerSectionCensusSnapshot> missingP11Sections =
            new List<OwnerSectionCensusSnapshot> { ToRequiredSnapshot(expeditionWitness) };
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, missingP11Sections, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("Required P11 ActorChoice owner"));
    }

    private static OwnerSectionCensusSnapshot ToExplicitlyEmptySnapshot(
        OwnerSectionCensusWitness witness)
    {
        return new OwnerSectionCensusSnapshot(
            witness.SectionId,
            witness.SchemaVersion,
            OwnerSectionRole.ExplicitlyEmpty,
            witness.OwnerInstanceIdentity,
            witness.Cardinality,
            witness.Revision);
    }

    [Test]
    public void CoreRestoredTargetInvariantGateRequiresExactZeroRowsForBoundP8Owners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "restore-p8-target-invariant-root", new MissingSlotOperations(),
            stageCompleted: null, logOutput: null, unityV1Identity: UnityIdentity());
        Assert.That(DailyV1Application.TryPrepareStart(
            definitions, host,
            out DailyV1ApplicationStartPreparation preparation,
            out _, out string startDiagnostic), Is.True, startDiagnostic);

        DailyV1ActiveRuntimeSession session = preparation.CoreSession;
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        PersonSpatialPositionStore positions = new PersonSpatialPositionStore(
            graph.PersonStore,
            spatial,
            new SpatialPassageTraversalOptionResolver(spatial.PassageAuthority));
        LegacySpatialAnchorBindingStore anchors = new LegacySpatialAnchorBindingStore(spatial);
        Assert.That(graph.TryBindPersonSpatialPositionStore(positions), Is.True);
        Assert.That(graph.TryBindLegacySpatialAnchorBindingStore(anchors), Is.True);

        OwnerSectionCensusWitness expedition = new ExpeditionCensusProvider(graph.Expeditions).GetCurrentCensus();
        ActorChoiceStore actorChoices = session.ExecutionBoundaryState.SelectedActorChoiceStore;
        OwnerSectionCensusWitness p11 = new ActorChoiceP11CensusProvider(actorChoices).GetCurrentCensus();
        OwnerSectionCensusWitness position = new PersonSpatialPositionCensusProvider(positions).GetCurrentCensus();
        OwnerSectionCensusWitness anchor = new LegacySpatialAnchorBindingCensusProvider(anchors).GetCurrentCensus();
        List<OwnerSectionCensusSnapshot> exactEmptyTarget = new List<OwnerSectionCensusSnapshot>
        {
            ToRequiredSnapshot(expedition),
            ToRequiredSnapshot(p11),
            ToExplicitlyEmptySnapshot(position),
            ToExplicitlyEmptySnapshot(anchor)
        };

        Assert.That(position.Cardinality, Is.Zero);
        Assert.That(anchor.Cardinality, Is.Zero);
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, exactEmptyTarget, out string diagnostic), Is.True, diagnostic);

        List<OwnerSectionCensusSnapshot> missingPosition =
            new List<OwnerSectionCensusSnapshot>
            {
                ToRequiredSnapshot(expedition),
                ToRequiredSnapshot(p11),
                ToExplicitlyEmptySnapshot(anchor)
            };
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, missingPosition, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain(PersonSpatialPositionCensusProvider.SectionId));

        List<OwnerSectionCensusSnapshot> nonzeroPosition =
            new List<OwnerSectionCensusSnapshot>(exactEmptyTarget);
        nonzeroPosition[2] = new OwnerSectionCensusSnapshot(
            position.SectionId, position.SchemaVersion, OwnerSectionRole.ExplicitlyEmpty,
            position.OwnerInstanceIdentity, 1, position.Revision);
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, nonzeroPosition, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain(PersonSpatialPositionCensusProvider.SectionId));

        List<OwnerSectionCensusSnapshot> wrongPositionIdentity =
            new List<OwnerSectionCensusSnapshot>(exactEmptyTarget);
        wrongPositionIdentity[2] = new OwnerSectionCensusSnapshot(
            position.SectionId, position.SchemaVersion, OwnerSectionRole.ExplicitlyEmpty,
            new object(), position.Cardinality, position.Revision);
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, wrongPositionIdentity, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain(PersonSpatialPositionCensusProvider.SectionId));

        List<OwnerSectionCensusSnapshot> missingAnchor =
            new List<OwnerSectionCensusSnapshot>
            {
                ToRequiredSnapshot(expedition),
                ToRequiredSnapshot(p11),
                ToExplicitlyEmptySnapshot(position)
            };
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, missingAnchor, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain(LegacySpatialAnchorBindingCensusProvider.SectionId));

        List<OwnerSectionCensusSnapshot> nonzeroAnchor =
            new List<OwnerSectionCensusSnapshot>(exactEmptyTarget);
        nonzeroAnchor[3] = new OwnerSectionCensusSnapshot(
            anchor.SectionId, anchor.SchemaVersion, OwnerSectionRole.ExplicitlyEmpty,
            anchor.OwnerInstanceIdentity, 1, anchor.Revision);
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, nonzeroAnchor, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain(LegacySpatialAnchorBindingCensusProvider.SectionId));

        List<OwnerSectionCensusSnapshot> wrongAnchorRole =
            new List<OwnerSectionCensusSnapshot>(exactEmptyTarget);
        wrongAnchorRole[3] = new OwnerSectionCensusSnapshot(
            anchor.SectionId, anchor.SchemaVersion, OwnerSectionRole.Required,
            anchor.OwnerInstanceIdentity, anchor.Cardinality, anchor.Revision);
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session, wrongAnchorRole, out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain(LegacySpatialAnchorBindingCensusProvider.SectionId));
    }

    [Test]
    public void CoreRestoredTargetInvariantGateAcceptsPopulatedRequiredP11Owner()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(out _), Is.True);
        AdmitMissingSlot(application);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession session, out string startDiagnostic),
            Is.True, startDiagnostic);

        MinimalSelectedRuntimeAdvanceHostPorts hostPorts =
            new MinimalSelectedRuntimeAdvanceHostPorts(session.ExecutionBoundaryState);
        Assert.That(application.TryBindRuntimeAdvanceBoundary(session, hostPorts), Is.True);
        Assert.That(application.TryPublishInitial(session), Is.True);
        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        ActorChoiceStore actorChoices = state.SelectedActorChoiceStore;
        PersonRuntime person = new PersonRuntime(new PersonId("restore-populated-p11-person"));
        Assert.That(application.TryBeginOperation(session, DailyV1OperationScopeKind.Application, out DailyV1ActiveRuntimeSession operationSession), Is.True);
        Assert.That(operationSession, Is.SameAs(session));
        try
        {
            Assert.That(session.OwnerGraph.PersonStore.TryRegister(
                person, out PersonStoreFailure personFailure), Is.True, personFailure.ToString());
            Assert.That(actorChoices.TryCapture(
                "restore-populated-p11-command",
                person.PersonId,
                "restore-populated-p11-action",
                WorldCommandOrigin.LocalPlayer,
                WorldCommandAuthorityMode.Request,
                0L,
                out ActorChoiceInput captured,
                out ActorChoiceStoreFailureCode failure), Is.True, failure.ToString());
            Assert.That(captured.Status, Is.EqualTo(ActorChoiceInputStatus.Pending));
        }
        finally
        {
            application.EndOperation(DailyV1OperationScopeKind.Application);
        }

        OwnerSectionCensusWitness expedition = new ExpeditionCensusProvider(
            session.OwnerGraph.Expeditions).GetCurrentCensus();
        OwnerSectionCensusWitness p11 = new ActorChoiceP11CensusProvider(actorChoices).GetCurrentCensus();
        Assert.That(p11.Cardinality, Is.EqualTo(1));
        Assert.That(DailyV1Application.TryValidateRestoredCoreTargetInvariants(
            session,
            new List<OwnerSectionCensusSnapshot>
            {
                ToRequiredSnapshot(expedition),
                ToRequiredSnapshot(p11)
            },
            out string diagnostic), Is.True, diagnostic);
    }

    [Test]
    public void RestoredCoreOwnerGraphRejectsGenealogyEndpointsOutsidePersonStore()
    {
        PersonStore people = new PersonStore();
        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(
            new PersonId("missing-parent"), new PersonId("missing-child"), out _), Is.True);

        Assert.That(
            DailyV1SelectedRuntimeOwnerGraph.TryValidateRestoredPersonNpcGenealogyBindings(
                people, Array.Empty<DailyV1NpcRuntimeOwners>(), genealogy, out string diagnostic),
            Is.False);
        Assert.That(diagnostic, Does.Contain("Genealogy"));
    }

    [Test]
    public void CoreAllocatorHighWaterRejectsASequenceThatWouldReuseRetainedIdentity()
    {
        RuntimeIdAllocatorSnapshot stale = new RuntimeIdAllocatorSnapshot(
            RuntimeIdAllocatorSnapshot.CurrentSchemaId,
            RuntimeIdAllocatorSnapshot.CurrentSchemaVersion,
            new[] { new RuntimeIdAllocatorCounterSnapshot("npc", 1L) });

        Assert.That(DailyV1Application.TryValidateAllocatorHighWater(
            stale,
            new[] { new KeyValuePair<string, string>("npc", "npc-000001") },
            Array.Empty<string>(), out string diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("does not continue past"));

        RuntimeIdAllocatorSnapshot current = new RuntimeIdAllocatorSnapshot(
            RuntimeIdAllocatorSnapshot.CurrentSchemaId,
            RuntimeIdAllocatorSnapshot.CurrentSchemaVersion,
            new[] { new RuntimeIdAllocatorCounterSnapshot("npc", 2L) });
        Assert.That(DailyV1Application.TryValidateAllocatorHighWater(
            current,
            new[] { new KeyValuePair<string, string>("npc", "npc-000001") },
            Array.Empty<string>(), out diagnostic), Is.True, diagnostic);
    }
    [Test]
    public void CoreStartPreparationOwnsNoSaveDecisionAndPreparesOneUnpublishedGenesis()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        List<string> completedStages = new List<string>();
        List<string> logMessages = new List<string>();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "start-preparation-root", new MissingSlotOperations(),
            completedStages.Add,
            (severity, message) => logMessages.Add(message),
            unityV1Identity: UnityIdentity());

        Assert.That(DailyV1Application.TryPrepareStart(
            definitions, host,
            out DailyV1ApplicationStartPreparation preparation,
            out P12AResult persistenceResult,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(preparation, Is.Not.Null);
        Assert.That(preparation.Kind, Is.EqualTo(DailyV1ApplicationStartPreparationKind.GenesisPrepared));
        Assert.That(preparation.PersistenceResult, Is.SameAs(persistenceResult));
        Assert.That(persistenceResult.Code, Is.EqualTo(P12AResultCode.NoSave));
        Assert.That(preparation.RestoreImage, Is.Null);
        Assert.That(preparation.GenesisComposition, Is.Not.Null);
        Assert.That(preparation.CoreSession, Is.Not.Null);
        Assert.That(preparation.Logger, Is.Not.Null);
        Assert.That(preparation.Application.ActiveSession, Is.Null,
            "The start preparation must not publish a session before runtime-boundary admission.");
        Assert.That(ReferenceEquals(preparation.GenesisComposition.Definitions, definitions), Is.True);
        Assert.That(ReferenceEquals(preparation.CoreSession.Definitions, definitions), Is.True);
        Assert.That(completedStages, Is.Not.Empty);
        Assert.That(new HashSet<string>(completedStages, StringComparer.Ordinal).Count,
            Is.EqualTo(completedStages.Count), "Core must preserve the once-only stage callback contract.");
        Assert.That(preparation.Logger.FullLog, Does.Contain(definitions.Configuration.simulationName));
        Assert.That(preparation.Logger.FullLog, Does.Not.Contain("start-preparation-root"),
            "The private storage root must not be emitted through the host log sink.");
    }

    [Test]
    public void InitialStartCommitSelectsOnlyTheExactPreparedGenesisAndKeepsPublicationFailClosed()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "start-transaction-root", new MissingSlotOperations(), null, null,
            unityV1Identity: UnityIdentity());

        Assert.That(DailyV1Application.TryPrepareStart(
            definitions, host,
            out DailyV1ApplicationStartPreparation preparation,
            out P12AResult persistenceResult,
            out string prepareDiagnostic), Is.True, prepareDiagnostic);
        Assert.That(preparation.Kind, Is.EqualTo(DailyV1ApplicationStartPreparationKind.GenesisPrepared));
        Assert.That(persistenceResult.Code, Is.EqualTo(P12AResultCode.NoSave));

        Assert.That(DailyV1Application.TryPrepareStart(
            definitions, host,
            out DailyV1ApplicationStartPreparation foreignPreparation,
            out _, out string foreignDiagnostic), Is.True, foreignDiagnostic);
        Assert.That(foreignPreparation.Kind, Is.EqualTo(DailyV1ApplicationStartPreparationKind.GenesisPrepared));

        var foreignPorts = new MinimalSelectedRuntimeAdvanceHostPorts(
            foreignPreparation.CoreSession.ExecutionBoundaryState);
        Assert.That(preparation.Application.TryCommitInitialStartWithHostProjection(
            preparation, foreignPreparation.CoreSession,
            new TestHostProjection(), foreignPorts, out string foreignCommitDiagnostic), Is.False);
        Assert.That(foreignCommitDiagnostic, Does.Contain("exact Core session"));
        Assert.That(preparation.Application.ActiveSession, Is.Null);

        var exactPorts = new MinimalSelectedRuntimeAdvanceHostPorts(
            preparation.CoreSession.ExecutionBoundaryState);
        Assert.That(preparation.Application.TryCommitInitialStartWithHostProjection(
            preparation, preparation.CoreSession,
            new TestHostProjection(), exactPorts, out string commitDiagnostic), Is.True, commitDiagnostic);
        Assert.That(preparation.Application.ActiveSession, Is.SameAs(preparation.CoreSession),
            "The selected application transaction must publish the exact prepared Core genesis.");
    }

    [Test]
    public void CoreStartPreparationDoesNotFallThroughToGenesisForAnInvalidExistingSlot()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        int completedStageCount = 0;
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "corrupt-start-root", new CorruptSlotOperations(),
            _ => completedStageCount++, null,
            unityV1Identity: UnityIdentity());

        Assert.That(DailyV1Application.TryPrepareStart(
            definitions, host,
            out DailyV1ApplicationStartPreparation preparation,
            out P12AResult persistenceResult,
            out string diagnostic), Is.False);

        Assert.That(preparation, Is.Null);
        Assert.That(persistenceResult.Code, Is.EqualTo(P12AResultCode.CorruptOrTruncated));
        Assert.That(diagnostic, Does.Contain("genesis remains blocked"));
        Assert.That(completedStageCount, Is.Zero,
            "A corrupt existing slot must be rejected before genesis stages begin.");
    }

    [Test]
    public void InvalidInitialSlotBlocksGenesisOnTheSameApplication()
    {
        DailyV1Application application = new DailyV1Application();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "corrupt-slot-root", new CorruptSlotOperations(), null, null,
            unityV1Identity: UnityIdentity());

        Assert.That(application.TryLoadInitialContinuationImageForStart(
            null, host, out P12AContinuationImage image, out P12AResult loadResult), Is.False);
        Assert.That(image, Is.Null);
        Assert.That(loadResult.Code, Is.EqualTo(P12AResultCode.CorruptOrTruncated));

        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out string diagnostic), Is.False);
        Assert.That(candidate, Is.Null);
        Assert.That(diagnostic, Does.Contain("cannot capture a second bootstrap"));
    }

    [Test]
    public void HostRequiresOneIdentityAndExplicitStoragePorts()
    {
        P12ACompatibilityDescriptor unity = new P12ACompatibilityDescriptor(
            "UnityBootstrap-Daily-v1", 1, "config", "6000.3.9f1", "WindowsEditor", "Mono",
            "build", new[] { "00000000-0000-0000-0000-000000000001" }, "numeric", 1,
            "content", "providers", "daily", 1, "calendar", "p9", "p8");

        Assert.Throws<ArgumentException>(() => new DailyV1ApplicationHost(
            "root", SystemAtomicFileSlotOperations.Instance, null, null));
        Assert.Throws<ArgumentException>(() => new DailyV1ApplicationHost(
            "root", SystemAtomicFileSlotOperations.Instance, null, null,
            DotNetIdentity(), unity));
        Assert.Throws<ArgumentException>(() => new DailyV1ApplicationHost(
            " ", SystemAtomicFileSlotOperations.Instance, null, null, unityV1Identity: unity));
        Assert.Throws<ArgumentNullException>(() => new DailyV1ApplicationHost(
            "root", null, null, null, unityV1Identity: unity));
    }

    [Test]
    public void DotNetEnvironmentIdentityRejectsForeignHostKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DotNetIdentity(2));
        Assert.Throws<ArgumentNullException>(() => new DailyV1HostEnvironmentIdentity(1,
            ".NET 8.0", "8.0.1.0", "Windows", "win-x64", "x64", "net8.0",
            null, "numeric", 1));
    }

    [Test]
    public void DotNetV2HeaderAdmitsOnlySupportedDotNetHostKind()
    {
        P12ACompatibilityDescriptorV2 dotNet = ToDescriptor(DotNetIdentity());
        Assert.That(V2Header(dotNet).IsValid, Is.True);

        P12ACompatibilityDescriptorV2 unsupportedHost = new P12ACompatibilityDescriptorV2(
            "UnityBootstrap-Daily-v1", 1, 2,
            ".NET 8.0", "8.0.1.0", "Windows", "win-x64", "x64", "net8.0",
            dotNet.Assemblies, "numeric", 1, "configuration", "content", "providers",
            "calendar", 1, "calendar-fingerprint", "p9", "p8");
        Assert.That(V2Header(unsupportedHost).IsValid, Is.False,
            "Encoding must not accept a descriptor that the .NET-v2 decoder will reject.");

    }

    private static DailyV1HostEnvironmentIdentity DotNetIdentity(ushort hostKind = 1) =>
        new DailyV1HostEnvironmentIdentity(hostKind,
            ".NET 8.0", "8.0.1.0", "Windows", "win-x64", "x64", "net8.0",
            new[] { new P12AAssemblyIdentityV2("Simulation.Core", "1.0.0.0",
                "00000000-0000-0000-0000-000000000001") }, "numeric", 1);

    private sealed class MinimalSelectedRuntimeAdvanceHostPorts : IDailyV1SelectedRuntimeAdvanceHostPorts
    {
        internal MinimalSelectedRuntimeAdvanceHostPorts(DailyV1ExecutionBoundaryState state)
        {
            ExecutionState = state ?? throw new ArgumentNullException(nameof(state));
        }

        public DailyV1ExecutionBoundaryState ExecutionState { get; }

        public bool TryGetExactJusticeRecordsCensusProjectionIdentity(
            DailyV1JusticeRecordsOwner owner,
            out object projectionIdentity,
            out long p18ReceiptRevision)
        {
            projectionIdentity = null;
            p18ReceiptRevision = 0L;
            return false;
        }

        public bool TryCreateSelectedDailyExecutionHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDailyExecutionHostPorts hostPorts)
        {
            hostPorts = null;
            return false;
        }

        public bool TryCaptureOwnerSectionCensus(
            DailyV1CaptureScopeAllowance allowedOuterScope,
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure)
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        public bool TryBindRestoredCandidateMutationGuard(out string diagnostic)
        {
            diagnostic = "This fixture does not publish a restored candidate.";
            return false;
        }

        public bool TryCaptureUnadmittedRestoredDailyOwnerVector(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out string diagnostic)
        {
            ownerSections = null;
            mutationEpoch = 0L;
            diagnostic = "This fixture does not capture a restored target vector.";
            return false;
        }
    }
    private static P12ACompatibilityDescriptor UnityIdentity() =>
        new P12ACompatibilityDescriptor(
            "UnityBootstrap-Daily-v1", 1, "config", "6000.3.9f1", "WindowsEditor", "Mono",
            "build", new[] { "00000000-0000-0000-0000-000000000001" }, "numeric", 1,
            "content", "providers", "daily", 1, "calendar", "p9", "p8");

    private static P12ACompatibilityDescriptorV2 ToDescriptor(DailyV1HostEnvironmentIdentity identity) =>
        new P12ACompatibilityDescriptorV2("UnityBootstrap-Daily-v1", 1, identity.HostKind,
            identity.RuntimeFrameworkDescription, identity.RuntimeVersion,
            identity.OperatingSystemDescription, identity.RuntimeIdentifier,
            identity.ProcessArchitecture, identity.TargetFramework, identity.Assemblies,
            identity.NumericProfileId, identity.NumericProfileVersion,
            "configuration", "content", "providers", "calendar", 1,
            "calendar-fingerprint", "p9", "p8");

    private static P12AContinuationHeader V2Header(P12ACompatibilityDescriptorV2 descriptor) =>
        P12AContinuationHeader.ForDotNetV2("world:00112233445566778899aabbccddeeff", 0, 1, -1,
            "p9-stage", "p9", "P8-A", "p8", 0, descriptor);

    private static bool ContainsSubsequence(byte[] value, byte[] sequence)
    {
        for (int start = 0; start <= value.Length - sequence.Length; start++)
        {
            bool matches = true;
            for (int offset = 0; offset < sequence.Length; offset++)
                if (value[start + offset] != sequence[offset])
                {
                    matches = false;
                    break;
                }
            if (matches) return true;
        }
        return false;
    }
}

public sealed class DailyV1RestoredImageOwnerVectorCoreTests
{
    [Test]
    public void ImageTargetVectorAcceptsTheExactDailyProfileWithNewProcessLocalOwners()
    {
        OwnerSectionCensusSnapshot[] imageSource = CreateProfileRows(false);
        OwnerSectionCensusSnapshot[] restoredTarget = CreateProfileRows(false);
        Assert.That(P12ADailyV1Census.TryCreate(imageSource,
            out P12ADailyV1Census imageCensus, out P12AResult imageResult),
            Is.True, imageResult?.SectionCode ?? imageResult?.Code.ToString());

        Assert.That(DailyV1RestoredOwnerVectorContract.TryValidateRestoredImageCandidate(
            imageCensus, restoredTarget, out P12GDailyV1OwnerVectorFailure failure,
            out string diagnostic), Is.True, diagnostic ?? failure.ToString());
        Assert.That(failure, Is.EqualTo(P12GDailyV1OwnerVectorFailure.None));
    }

    [Test]
    public void ImageTargetVectorRejectsChangedOwnerRevisionAndAliasStructure()
    {
        OwnerSectionCensusSnapshot[] imageSource = CreateProfileRows(false);
        Assert.That(P12ADailyV1Census.TryCreate(imageSource,
            out P12ADailyV1Census imageCensus, out P12AResult imageResult),
            Is.True, imageResult?.SectionCode ?? imageResult?.Code.ToString());

        OwnerSectionCensusSnapshot[] changedRevision = CreateProfileRows(false);
        int inventoryIndex = Find(changedRevision,
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "npc-one");
        OwnerSectionCensusSnapshot inventory = changedRevision[inventoryIndex];
        changedRevision[inventoryIndex] = new OwnerSectionCensusSnapshot(
            inventory.SectionId, inventory.SchemaVersion, inventory.Role,
            inventory.OwnerInstanceIdentity, inventory.Cardinality, inventory.Revision + 1L);
        Assert.That(DailyV1RestoredOwnerVectorContract.TryValidateRestoredImageCandidate(
            imageCensus, changedRevision, out P12GDailyV1OwnerVectorFailure revisionFailure,
            out _), Is.False);
        Assert.That(revisionFailure, Is.EqualTo(P12GDailyV1OwnerVectorFailure.OwnerAliasMismatch));

        OwnerSectionCensusSnapshot[] changedCardinality = CreateProfileRows(false);
        inventoryIndex = Find(changedCardinality,
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "npc-one");
        inventory = changedCardinality[inventoryIndex];
        changedCardinality[inventoryIndex] = new OwnerSectionCensusSnapshot(
            inventory.SectionId, inventory.SchemaVersion, inventory.Role,
            inventory.OwnerInstanceIdentity, inventory.Cardinality + 1, inventory.Revision);
        Assert.That(DailyV1RestoredOwnerVectorContract.TryValidateRestoredImageCandidate(
            imageCensus, changedCardinality, out P12GDailyV1OwnerVectorFailure cardinalityFailure,
            out _), Is.False);
        Assert.That(cardinalityFailure, Is.EqualTo(P12GDailyV1OwnerVectorFailure.OwnerAliasMismatch));

        OwnerSectionCensusSnapshot[] splitAliases = CreateProfileRows(true);
        Assert.That(DailyV1RestoredOwnerVectorContract.TryValidateRestoredImageCandidate(
            imageCensus, splitAliases, out P12GDailyV1OwnerVectorFailure aliasFailure,
            out _), Is.False);
        Assert.That(aliasFailure, Is.EqualTo(P12GDailyV1OwnerVectorFailure.ContractMismatch));
    }

    private static OwnerSectionCensusSnapshot[] CreateProfileRows(bool splitFixedAlias)
    {
        List<OwnerSectionCensusSnapshot> rows = new List<OwnerSectionCensusSnapshot>();
        Dictionary<string, object> ownersByAlias = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (string id in P12ADailyV1Census.FixedRequiredSectionIds)
            Add(id, OwnerSectionRole.Required);
        foreach (string id in P12ADailyV1Census.FixedEmptySectionIds)
            Add(id, OwnerSectionRole.ExplicitlyEmpty);
        foreach (string prefix in P12ADailyV1Census.NpcSectionPrefixes)
            Add(prefix + "npc-one", OwnerSectionRole.Required);
        const string cityKey = "city-one";
        for (int index = 0; index < P12ADailyV1Census.CitySectionPrefixes.Count; index++)
        {
            string prefix = P12ADailyV1Census.CitySectionPrefixes[index];
            string key = index == 0 ? cityKey : cityKey.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ":" + cityKey;
            Add(prefix + key, OwnerSectionRole.Required);
        }
        return rows.ToArray();

        void Add(string id, OwnerSectionRole role)
        {
            string alias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(id);
            if (alias != null && !ownersByAlias.TryGetValue(alias, out _))
                ownersByAlias.Add(alias, new object());
            object owner = alias == null ? new object() : ownersByAlias[alias];
            if (splitFixedAlias && id == "p12c.runtime-id-allocator.decisions")
                owner = new object();
            rows.Add(new OwnerSectionCensusSnapshot(id, 1, role, owner, 0, 0L));
        }
    }

    private static int Find(IReadOnlyList<OwnerSectionCensusSnapshot> rows, string id)
    {
        for (int index = 0; index < rows.Count; index++)
            if (string.Equals(rows[index].SectionId, id, StringComparison.Ordinal)) return index;
        Assert.Fail("Section was not in the canonical Daily-v1 profile: " + id);
        return -1;
    }
}
