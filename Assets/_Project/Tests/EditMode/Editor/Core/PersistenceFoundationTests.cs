using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class PersistenceFoundationTests
{
    private static P12ACompatibilityDescriptorV2 V2(ushort host = 1, string framework = ".NET 8", string version = "8.0.1.0", string os = "Windows", string rid = "win-x64", string arch = "x64", string tfm = "net8.0", string assemblyName = "Simulation.Core", string assemblyVersion = "1.0.0.0", string mvid = "4b37a0ac-246b-47c5-9bbc-fd79704e0666", string numeric = "numeric", int numericVersion = 1, string config = "config", string content = "content", string providers = "providers", string calendar = "calendar", int calendarVersion = 1, string calendarHash = "calendar-hash", string p9 = "p9", string p8 = "p8", string profileId = "UnityBootstrap-Daily-v1", int profileVersion = 1)
        => new P12ACompatibilityDescriptorV2(profileId, profileVersion, host, framework, version, os, rid, arch, tfm,
            new[] { new P12AAssemblyIdentityV2(assemblyName, assemblyVersion, mvid) }, numeric, numericVersion, config, content, providers, calendar, calendarVersion, calendarHash, p9, p8);
    [Test]
    public void V1DescriptorMatchesIndependentPreExtractionCanonicalVector()
    {
        var source = new P12ACompatibilityDescriptor("UnityBootstrap-Daily-v1", 1, "config", "6000.3.9f1", "WindowsEditor", "Mono", "build", new[] { "00000000-0000-0000-0000-000000000001" }, "numeric", 1, "content", "providers", "daily", 1, "calendar", "p9", "p8");
        const string expected = "17000000556E697479426F6F7473747261702D4461696C792D76310100000006000000636F6E6669670A000000363030302E332E3966310D00000057696E646F7773456469746F72040000004D6F6E6F050000006275696C64010000002400000030303030303030302D303030302D303030302D303030302D303030303030303030303031070000006E756D657269630100000007000000636F6E74656E740900000070726F766964657273050000006461696C79010000000800000063616C656E646172020000007039020000007038";
        Assert.That(BitConverter.ToString(source.CanonicalBytes).Replace("-", string.Empty), Is.EqualTo(expected));
        Assert.That(P12ACompatibilityDescriptor.TryReadCanonical(source.CanonicalBytes, out var parsed), Is.True);
        Assert.That(parsed.MismatchCode(new P12ACompatibilityDescriptor("UnityBootstrap-Daily-v1", 1, "changed", "6000.3.9f1", "WindowsEditor", "Mono", "build", new[] { "00000000-0000-0000-0000-000000000001" }, "numeric", 1, "content", "providers", "daily", 1, "calendar", "p9", "p8")), Is.EqualTo(P12AResultCode.IncompatibleContentOrConfiguration));
        Assert.That(parsed.MismatchCode(new P12ACompatibilityDescriptor("UnityBootstrap-Daily-v1", 1, "config", "6000.3.9f1", "LinuxPlayer", "Mono", "build", new[] { "00000000-0000-0000-0000-000000000001" }, "numeric", 1, "content", "providers", "daily", 1, "calendar", "p9", "p8")), Is.EqualTo(P12AResultCode.IncompatibleBuildOrRuntime));
    }
    [Test]
    public void V2RoundTripsCanonicalBytesAndClassifiesContentAndRuntime()
    {
        var expected = V2();
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(expected.CanonicalBytes, out var actual, out var result), Is.True);
        Assert.That(result, Is.EqualTo(P12AResultCode.Decoded));
        Assert.That(actual.CanonicalBytes, Is.EqualTo(expected.CanonicalBytes));
        Assert.That(P12ACompatibilityAdmission.AdmitDotNetV2(expected.CanonicalBytes, expected, out _), Is.EqualTo(P12AResultCode.Decoded));
        Assert.That(expected.MismatchCode(V2(2)), Is.EqualTo(P12AResultCode.IncompatibleBuildOrRuntime));
        Assert.That(P12ACompatibilityAdmission.AdmitDotNetV2(V2(2).CanonicalBytes, expected, out _), Is.EqualTo(P12AResultCode.IncompatibleBuildOrRuntime));
        var b = V2().CanonicalBytes;
        var content = V2(content: "other");
        Assert.That(expected.MismatchCode(content), Is.EqualTo(P12AResultCode.IncompatibleContentOrConfiguration));
        Assert.That(b, Is.EqualTo(expected.CanonicalBytes));
    }
    [Test]
    public void V2AdmissionIsPinnedToTheApprovedNet8TargetFramework()
    {
        var expected = V2();
        Assert.That(P12ACompatibilityAdmission.AdmitDotNetV2(V2(tfm: "net9.0").CanonicalBytes, expected, out _), Is.EqualTo(P12AResultCode.IncompatibleBuildOrRuntime));
        Assert.That(P12ACompatibilityAdmission.AdmitDotNetV2(V2().CanonicalBytes, V2(tfm: "net9.0"), out _), Is.EqualTo(P12AResultCode.IncompatibleBuildOrRuntime));
    }
    [Test]
    public void NumericProfileCoreClrHashMatchesIndependentCanonicalVector()
    {
        string[] ids = { "00000000-0000-0000-0000-000000000002", "00000000-0000-0000-0000-000000000001" };
        Assert.That(P12ANumericProfileIdentity.TryCreateCoreClr(ids, out string id, out int version), Is.True);
        Assert.That(version, Is.EqualTo(1));
        Assert.That(id, Is.EqualTo("p12.numeric.ieee754-binary32-binary64/canonical-le-wire-v1/70edafa4db068c283e8975b7029a62d59360746aa078013f9616c0507dd0f4d7"));
        Assert.That(P12ANumericProfileIdentity.TryCreateCoreClr(new[] { ids[0], ids[0] }, out _, out _), Is.False);
    }
    [Test]
    public void V2ExactWireFieldOrderStrictUtf8BoundsAndCanonicality()
    {
        byte[] bytes = V2().CanonicalBytes;
        using (var stream = new MemoryStream(bytes, false))
        using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
        {
            Assert.That(reader.ReadInt32(), Is.EqualTo(-2));
            Assert.That(System.Text.Encoding.ASCII.GetString(reader.ReadBytes(8)), Is.EqualTo("P12ACDV2"));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(2));
            Assert.That(reader.ReadUInt16(), Is.Zero);
            Assert.That(ReadIndependentString(reader), Is.EqualTo("UnityBootstrap-Daily-v1"));
            Assert.That(reader.ReadInt32(), Is.EqualTo(1));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(1));
            Assert.That(reader.ReadUInt16(), Is.Zero);
            Assert.That(ReadIndependentString(reader), Is.EqualTo(".NET 8"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("8.0.1.0"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("Windows"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("win-x64"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("x64"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("net8.0"));
            Assert.That(reader.ReadInt32(), Is.EqualTo(1));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("Simulation.Core"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("1.0.0.0"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("4b37a0ac-246b-47c5-9bbc-fd79704e0666"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("numeric"));
            Assert.That(reader.ReadInt32(), Is.EqualTo(1));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("config"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("content"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("providers"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("calendar"));
            Assert.That(reader.ReadInt32(), Is.EqualTo(1));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("calendar-hash"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("p9"));
            Assert.That(ReadIndependentString(reader), Is.EqualTo("p8"));
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
        }
        int frameworkLengthOffset = FindEncodedStringOffset(bytes, ".NET 8");
        byte[] malformed = (byte[])bytes.Clone();
        malformed[frameworkLengthOffset + sizeof(int)] = 0xFF;
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(malformed, out _, out var result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.CorruptOrTruncated));
        byte[] noncanonical = (byte[])bytes.Clone();
        noncanonical[FindEncodedStringOffset(bytes, "win-x64") + sizeof(int)] = (byte)'W';
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(noncanonical, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.CorruptOrTruncated));
        var normalized = V2(framework: " .NET 8 ", rid: "WIN-x64", arch: "X64", assemblyName: " Simulation.Core ", assemblyVersion: "1.0", mvid: "4B37A0AC-246B-47C5-9BBC-FD79704E0666");
        Assert.That(normalized.CanonicalBytes, Is.EqualTo(bytes));
        byte[] reserved = (byte[])bytes.Clone();
        reserved[14] = 1;
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(reserved, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.UnsupportedSchema));
        int profileOffset = FindEncodedStringOffset(bytes, "UnityBootstrap-Daily-v1");
        int hostKindOffset = profileOffset + sizeof(int) + System.Text.Encoding.UTF8.GetByteCount("UnityBootstrap-Daily-v1") + sizeof(int);
        byte[] zeroProfileVersion = (byte[])bytes.Clone();
        Array.Clear(zeroProfileVersion, hostKindOffset - sizeof(int), sizeof(int));
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(zeroProfileVersion, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.CorruptOrTruncated),
            "A mandatory positive profile version of zero is malformed, while positive unknown versions are schema rejections.");
        byte[] hostReserved = (byte[])bytes.Clone();
        hostReserved[hostKindOffset + sizeof(ushort)] = 1;
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(hostReserved, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.UnsupportedSchema));
        byte[] unknownHost = (byte[])bytes.Clone();
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)3), 0, unknownHost, hostKindOffset, sizeof(ushort));
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(unknownHost, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.UnsupportedSchema));
        byte[] unsupportedProfile = (byte[])bytes.Clone();
        unsupportedProfile[profileOffset + sizeof(int)] = (byte)'X';
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(unsupportedProfile, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.UnsupportedProfile));
        byte[] unsupportedProfileVersion = (byte[])bytes.Clone();
        Buffer.BlockCopy(BitConverter.GetBytes(2), 0, unsupportedProfileVersion, profileOffset + sizeof(int) + System.Text.Encoding.UTF8.GetByteCount("UnityBootstrap-Daily-v1"), sizeof(int));
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(unsupportedProfileVersion, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.UnsupportedSchema));
        byte[] tooLarge = new byte[ContinuationWire.MaxHeaderBytes + 1];
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(tooLarge, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.CorruptOrTruncated));
        byte[] oversizedString = (byte[])bytes.Clone();
        Buffer.BlockCopy(BitConverter.GetBytes(ContinuationWire.MaxStringBytes + 1), 0, oversizedString, frameworkLengthOffset, sizeof(int));
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(oversizedString, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.CorruptOrTruncated));
        int assemblyCountOffset = FindAssemblyCountOffset(bytes);
        byte[] noAssemblies = (byte[])bytes.Clone();
        Buffer.BlockCopy(BitConverter.GetBytes(0), 0, noAssemblies, assemblyCountOffset, sizeof(int));
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(noAssemblies, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.CorruptOrTruncated));
        byte[] tooMany = (byte[])bytes.Clone();
        Buffer.BlockCopy(BitConverter.GetBytes(4097), 0, tooMany, assemblyCountOffset, sizeof(int));
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(tooMany, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.CorruptOrTruncated));
        byte[] duplicate = DuplicateSingleAssembly(bytes, assemblyCountOffset);
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(duplicate, out _, out result), Is.False);
        Assert.That(result, Is.EqualTo(P12AResultCode.CorruptOrTruncated));
    }
    [Test]
    public void V2MismatchTaxonomyChecksEveryRuntimeAndSemanticIdentityGroup()
    {
        var expected = V2();
        P12ACompatibilityDescriptorV2[] runtime = { V2(framework: ".NET 7"), V2(version: "8.0.2.0"), V2(os: "Linux"), V2(rid: "linux-x64"), V2(arch: "arm64"), V2(tfm: "net9.0"), V2(assemblyName: "Other.Core"), V2(assemblyVersion: "2.0.0.0"), V2(mvid: "5b37a0ac-246b-47c5-9bbc-fd79704e0666"), V2(numeric: "other"), V2(numericVersion: 2) };
        foreach (var value in runtime)
            Assert.That(expected.MismatchCode(value), Is.EqualTo(P12AResultCode.IncompatibleBuildOrRuntime));
        P12ACompatibilityDescriptorV2[] content = { V2(config: "other"), V2(content: "other"), V2(providers: "other"), V2(calendar: "other"), V2(calendarVersion: 2), V2(calendarHash: "other"), V2(p9: "other"), V2(p8: "other") };
        foreach (var value in content)
            Assert.That(expected.MismatchCode(value), Is.EqualTo(P12AResultCode.IncompatibleContentOrConfiguration));
    }
    [Test]
    public void V2DiscriminatorNeverFallsBackAndUnityRejectsRecognizedV2()
    {
        byte[] valid = V2(2).CanonicalBytes;
        Assert.That(P12ACompatibilityAdmission.ClassifyUnityV2(valid), Is.EqualTo(P12AResultCode.UnsupportedSchema));
        byte[] bad = (byte[])valid.Clone();
        bad[4] = (byte)'X';
        Assert.That(P12ACompatibilityAdmission.ClassifyUnityV2(bad), Is.EqualTo(P12AResultCode.CorruptOrTruncated));

        byte[] unknownProfile = (byte[])V2().CanonicalBytes.Clone();
        unknownProfile[FindEncodedStringOffset(unknownProfile, "UnityBootstrap-Daily-v1") + sizeof(int)] = (byte)'X';
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(unknownProfile, out _, out var code), Is.False);
        Assert.That(code, Is.EqualTo(P12AResultCode.UnsupportedProfile));
        Assert.That(P12ACompatibilityAdmission.ClassifyUnityV2(unknownProfile), Is.EqualTo(P12AResultCode.UnsupportedSchema));
        Assert.That(P12ACompatibilityAdmission.AdmitDotNetV2(unknownProfile, V2(), out _), Is.EqualTo(P12AResultCode.UnsupportedProfile));

        byte[] malformedUnknownProfile = (byte[])unknownProfile.Clone();
        malformedUnknownProfile[FindEncodedStringOffset(malformedUnknownProfile, ".NET 8") + sizeof(int)] = 0xFF;
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(malformedUnknownProfile, out _, out code), Is.False);
        Assert.That(code, Is.EqualTo(P12AResultCode.CorruptOrTruncated));
        Assert.That(P12ACompatibilityAdmission.ClassifyUnityV2(malformedUnknownProfile), Is.EqualTo(P12AResultCode.CorruptOrTruncated));
    }
    [Test]
    public void HostAdmissionPrecedesProfilePolicyOnlyAfterFullCanonicalStructureValidation()
    {
        var expected = V2();
        foreach (ushort host in new ushort[] { 1, 2 })
        foreach (bool unknownProfile in new[] { false, true })
        foreach (int profileVersion in new[] { 1, 2 })
        {
            var descriptor = V2(host: host, profileId: unknownProfile ? "unknown-profile" : "UnityBootstrap-Daily-v1",
                profileVersion: profileVersion);
            byte[] bytes = descriptor.CanonicalBytes;
            P12AResultCode expectedResult = host == 2 ? P12AResultCode.IncompatibleBuildOrRuntime
                : unknownProfile ? P12AResultCode.UnsupportedProfile
                : profileVersion != 1 ? P12AResultCode.UnsupportedSchema : P12AResultCode.Decoded;
            Assert.That(P12ACompatibilityAdmission.AdmitDotNetV2(bytes, expected, out _), Is.EqualTo(expectedResult));
            Assert.That(P12ACompatibilityAdmission.ClassifyUnityV2(bytes), Is.EqualTo(P12AResultCode.UnsupportedSchema));
            byte[] malformed = (byte[])bytes.Clone();
            malformed[FindEncodedStringOffset(malformed, ".NET 8") + sizeof(int)] = 0xFF;
            Assert.That(P12ACompatibilityAdmission.AdmitDotNetV2(malformed, expected, out _),
                Is.EqualTo(P12AResultCode.CorruptOrTruncated));
            Assert.That(P12ACompatibilityAdmission.ClassifyUnityV2(malformed),
                Is.EqualTo(P12AResultCode.CorruptOrTruncated));
        }
    }

    [Test]
    public void AtomicSlotUsesDefaultFilesystemAndValidatorRunsOnceBeforeReplacement()
    {
        string root = Path.Combine(Path.GetTempPath(), "core-p12-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "slot.bin");
        var ops = SystemAtomicFileSlotOperations.Instance;
        try
        {
            byte[] original = { 1, 2, 3 };
            int calls = 0;
            Assert.That(AtomicFileSlot.Write(path, original, 32, ops, b => { calls++; return true; }), Is.EqualTo(AtomicSlotResult.Success));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(AtomicFileSlot.Read(path, 32, ops, out var read), Is.EqualTo(AtomicSlotResult.Success));
            Assert.That(read, Is.EqualTo(original));
            byte[] replacement = { 4, 5 };
            Assert.That(AtomicFileSlot.Write(path, replacement, 32, ops, b => { calls++; return false; }), Is.EqualTo(AtomicSlotResult.StorageFailure));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(Directory.GetFiles(root, "*.tmp").Length, Is.Zero);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Test]
    public void InspectDistinguishesMissingWithoutCreatingDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), "core-p12-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "nested", "slot");
        Assert.That(AtomicFileSlot.Inspect(path, SystemAtomicFileSlotOperations.Instance), Is.EqualTo(AtomicSlotResult.Missing));
        Assert.That(Directory.Exists(root), Is.False);
    }
    [TestCase("create-directory")]
    [TestCase("create-temporary")]
    [TestCase("write")]
    [TestCase("flush")]
    [TestCase("read")]
    [TestCase("replace")]
    public void StorageFaultsPreserveDestinationAndCleanOnlyOwnedTemporary(string fault)
    {
        string root = Path.Combine(Path.GetTempPath(), "core-p12-fault-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "slot.bin");
        Directory.CreateDirectory(root);
        byte[] prior = { 9, 8, 7 };
        File.WriteAllBytes(path, prior);
        var ops = new FaultOperations(fault);
        try
        { Assert.That(AtomicFileSlot.Write(path, new byte[] { 1, 2, 3 }, 32, ops, _ => true), Is.EqualTo(AtomicSlotResult.StorageFailure)); Assert.That(File.ReadAllBytes(path), Is.EqualTo(prior)); Assert.That(Directory.GetFiles(root, "*.tmp").Length, Is.Zero); }
        finally { Directory.Delete(root, true); }
    }
    [Test]
    public void FailedCreateNewDoesNotDeleteForeignTemporaryCollision()
    {
        string root = Path.Combine(Path.GetTempPath(), "core-p12-collision-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "slot.bin");
        Directory.CreateDirectory(root);
        byte[] prior = { 6 };
        File.WriteAllBytes(path, prior);
        string collision = null;
        var ops = new CollisionOperations(p => collision = p);
        try
        { Assert.That(AtomicFileSlot.Write(path, new byte[] { 1 }, 32, ops, _ => true), Is.EqualTo(AtomicSlotResult.StorageFailure)); Assert.That(File.ReadAllBytes(path), Is.EqualTo(prior)); Assert.That(File.ReadAllBytes(collision), Is.EqualTo(new byte[] { 4, 5 })); }
        finally { Directory.Delete(root, true); }
    }
    private static string ReadIndependentString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length < 0)
            throw new InvalidDataException("V2 strings are mandatory.");
        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
            throw new EndOfStreamException();
        return new System.Text.UTF8Encoding(false, true).GetString(bytes);
    }

    private static int FindEncodedStringOffset(byte[] bytes, string value)
    {
        byte[] encoded = System.Text.Encoding.UTF8.GetBytes(value);
        byte[] length = BitConverter.GetBytes(encoded.Length);
        for (int i = 0; i <= bytes.Length - encoded.Length - sizeof(int); i++)
        {
            bool match = true;
            for (int j = 0; j < sizeof(int); j++)
                if (bytes[i + j] != length[j])
                { match = false; break; }
            for (int j = 0; match && j < encoded.Length; j++)
                if (bytes[i + sizeof(int) + j] != encoded[j])
                    match = false;
            if (match)
                return i;
        }
        Assert.Fail("Expected field not present in the v2 vector.");
        return -1;
    }

    private static int FindAssemblyCountOffset(byte[] bytes)
    {
        using (var stream = new MemoryStream(bytes, false))
        using (var reader = new BinaryReader(stream, new System.Text.UTF8Encoding(false, true), true))
        {
            reader.ReadInt32();
            reader.ReadBytes(8);
            reader.ReadUInt16();
            reader.ReadUInt16();
            ReadIndependentString(reader);
            reader.ReadInt32();
            reader.ReadUInt16();
            reader.ReadUInt16();
            for (int i = 0; i < 6; i++)
                ReadIndependentString(reader);
            return checked((int)stream.Position);
        }
    }

    private static byte[] DuplicateSingleAssembly(byte[] bytes, int countOffset)
    {
        using (var stream = new MemoryStream(bytes, false))
        using (var reader = new BinaryReader(stream, new System.Text.UTF8Encoding(false, true), true))
        {
            stream.Position = countOffset;
            Assert.That(reader.ReadInt32(), Is.EqualTo(1));
            int tupleStart = checked((int)stream.Position);
            ReadIndependentString(reader);
            ReadIndependentString(reader);
            ReadIndependentString(reader);
            int tupleEnd = checked((int)stream.Position);
            int semanticStart = tupleEnd;
            byte[] tuple = new byte[tupleEnd - tupleStart];
            Buffer.BlockCopy(bytes, tupleStart, tuple, 0, tuple.Length);
            using (var output = new MemoryStream())
            using (var writer = new BinaryWriter(output))
            {
                writer.Write(bytes, 0, countOffset);
                writer.Write(2);
                writer.Write(tuple);
                writer.Write(tuple);
                writer.Write(bytes, semanticStart, bytes.Length - semanticStart);
                return output.ToArray();
            }
        }
    }

    private sealed class FaultOperations : IAtomicFileSlotOperations
    {
        private readonly string fault; private readonly IAtomicFileSlotOperations inner = SystemAtomicFileSlotOperations.Instance;
        internal FaultOperations(string fault) { this.fault = fault; }
        public void CreateDirectory(string p) { if (fault == "create-directory") throw new IOException(); inner.CreateDirectory(p); }
        public bool Exists(string p) => inner.Exists(p);
        public Stream CreateNewTemporary(string p) { if (fault == "create-temporary") throw new IOException(); Stream s = inner.CreateNewTemporary(p); return fault == "write" ? new ThrowingWriteStream(s) : s; }
        public Stream OpenRead(string p) { if (fault == "read") throw new IOException(); return inner.OpenRead(p); }
        public void FlushToDisk(Stream s) { if (fault == "flush") throw new IOException(); inner.FlushToDisk(s); }
        public void Replace(string s, string d) { if (fault == "replace") throw new IOException(); inner.Replace(s, d); }
        public void Move(string s, string d) => inner.Move(s, d); public void Delete(string p) => inner.Delete(p);
    }
    private sealed class ThrowingWriteStream : Stream
    {
        private readonly Stream inner; internal ThrowingWriteStream(Stream inner) { this.inner = inner; }
        public override bool CanRead => inner.CanRead; public override bool CanSeek => inner.CanSeek; public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length; public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] b, int o, int c) => inner.Read(b, o, c); public override long Seek(long o, SeekOrigin so) => inner.Seek(o, so); public override void SetLength(long v) => inner.SetLength(v);
        public override void Write(byte[] b, int o, int c) => throw new IOException(); protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
    }
    private sealed class CollisionOperations : IAtomicFileSlotOperations
    {
        private readonly Action<string> observe; private readonly IAtomicFileSlotOperations inner = SystemAtomicFileSlotOperations.Instance;
        internal CollisionOperations(Action<string> observe) { this.observe = observe; }
        public void CreateDirectory(string p) => inner.CreateDirectory(p); public bool Exists(string p) => inner.Exists(p);
        public Stream CreateNewTemporary(string p) { File.WriteAllBytes(p, new byte[] { 4, 5 }); observe(p); throw new IOException(); }
        public Stream OpenRead(string p) => inner.OpenRead(p); public void FlushToDisk(Stream s) => inner.FlushToDisk(s); public void Replace(string s, string d) => inner.Replace(s, d);
        public void Move(string s, string d) => inner.Move(s, d); public void Delete(string p) => inner.Delete(p);
    }
}
