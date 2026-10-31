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
    public void InitialNoSaveAdmissionAllowsCoreGenesisOnTheSameApplication()
    {
        DailyV1Application application = new DailyV1Application();
        P12ACompatibilityDescriptor unity = UnityIdentity();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "missing-slot-root", new MissingSlotOperations(), null, null,
            unityV1Identity: unity);

        Assert.That(application.TryLoadInitialContinuationImageForStart(
            null, host, out P12AContinuationImage image, out P12AResult loadResult), Is.False);
        Assert.That(image, Is.Null);
        Assert.That(loadResult.Code, Is.EqualTo(P12AResultCode.NoSave));

        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out string diagnostic), Is.True, diagnostic);
        Assert.That(candidate, Is.Not.Null);
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
