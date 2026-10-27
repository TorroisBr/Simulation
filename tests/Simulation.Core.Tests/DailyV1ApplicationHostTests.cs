using System;
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

    private sealed class FailedSlotOperations : IAtomicFileSlotOperations
    {
        public void CreateDirectory(string path) => throw new NotSupportedException();
        public bool Exists(string path) => throw new System.IO.IOException("Injected slot inspection failure.");
        public System.IO.Stream CreateNewTemporary(string path) => throw new NotSupportedException();
        public System.IO.Stream OpenRead(string path) => throw new NotSupportedException();
        public void FlushToDisk(System.IO.Stream stream) => throw new NotSupportedException();
        public void Replace(string source, string destination) => throw new NotSupportedException();
        public void Move(string source, string destination) => throw new NotSupportedException();
        public void Delete(string path) => throw new NotSupportedException();
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
    public void InitialNoSaveAdmissionAllowsCoreGenesisOnTheSameApplication()
    {
        DailyV1Application application = new DailyV1Application();
        MissingSlotOperations operations = new MissingSlotOperations();
        P12ACompatibilityDescriptor unity = UnityIdentity();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "missing-slot-root", operations, null, null,
            unityV1Identity: unity);

        Assert.That(application.TryInspectInitialContinuationSlot(
            host.PersistentDataRoot, operations, out bool exists, out P12AResult inspectResult), Is.True);
        Assert.That(exists, Is.False);
        Assert.That(inspectResult.Code, Is.EqualTo(P12AResultCode.NoSave));
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
    public void InvalidInitialSlotBlocksGenesisOnTheSameApplication()
    {
        DailyV1Application application = new DailyV1Application();
        CorruptSlotOperations operations = new CorruptSlotOperations();
        Assert.That(application.TryInspectInitialContinuationSlot(
            "corrupt-slot-root", operations, out bool exists, out P12AResult inspectResult), Is.True);
        Assert.That(exists, Is.True);
        Assert.That(inspectResult.Code, Is.EqualTo(P12AResultCode.Loaded));
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            "corrupt-slot-root", operations, null, null,
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
    public void InvalidOrUnprobedInitialLoadCannotFallThroughToGenesis()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application invalidHostApplication = new DailyV1Application();

        Assert.That(invalidHostApplication.TryLoadInitialContinuationImageForStart(
            definitions, null, out P12AContinuationImage invalidImage, out P12AResult invalidResult), Is.False);
        Assert.That(invalidImage, Is.Null);
        Assert.That(invalidResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
        Assert.That(invalidHostApplication.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession invalidCandidate, out _), Is.False);
        Assert.That(invalidCandidate, Is.Null);

        DailyV1Application unprobedApplication = new DailyV1Application();
        DailyV1ApplicationHost validHost = new DailyV1ApplicationHost(
            "unprobed-slot-root", new MissingSlotOperations(), null, null,
            unityV1Identity: UnityIdentity());
        Assert.That(unprobedApplication.TryLoadInitialContinuationImageForStart(
            null, validHost, out P12AContinuationImage unprobedImage, out P12AResult unprobedResult), Is.False);
        Assert.That(unprobedImage, Is.Null);
        Assert.That(unprobedResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
        Assert.That(unprobedApplication.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession unprobedCandidate, out _), Is.False);
        Assert.That(unprobedCandidate, Is.Null);
    }

    [Test]
    public void FailedInitialSlotInspectionBlocksGenesis()
    {
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryInspectInitialContinuationSlot(
            "failed-slot-root", new FailedSlotOperations(), out bool exists, out P12AResult result), Is.False);
        Assert.That(exists, Is.False);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.StorageFailure));

        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out _), Is.False);
        Assert.That(candidate, Is.Null);
    }

    [Test]
    public void NoSaveProofIsBoundToTheInspectedRootAndFileOperationsOwner()
    {
        DailyV1Application application = new DailyV1Application();
        MissingSlotOperations operations = new MissingSlotOperations();
        Assert.That(application.TryInspectInitialContinuationSlot(
            "inspected-root", operations, out bool exists, out P12AResult inspectResult), Is.True);
        Assert.That(exists, Is.False);
        Assert.That(inspectResult.Code, Is.EqualTo(P12AResultCode.NoSave));

        DailyV1ApplicationHost otherRootHost = new DailyV1ApplicationHost(
            "different-root", operations, null, null, unityV1Identity: UnityIdentity());
        Assert.That(application.TryLoadInitialContinuationImageForStart(
            null, otherRootHost, out P12AContinuationImage image, out P12AResult loadResult), Is.False);
        Assert.That(image, Is.Null);
        Assert.That(loadResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));

        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted: null,
            out _, out DailyV1ActiveRuntimeSession candidate, out _), Is.False);
        Assert.That(candidate, Is.Null);
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
