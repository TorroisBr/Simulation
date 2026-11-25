using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class Program
{
    public static int Main(string[] args)
    {
        string storageRoot = null;
        try
        {
            if (args == null || args.Length != 3)
                return Fail("arguments", "Expected: <genesis|restore> <storage-root> <advance-count>.", null);

            string mode = args[0];
            bool genesis = string.Equals(mode, "genesis", StringComparison.Ordinal);
            bool restore = string.Equals(mode, "restore", StringComparison.Ordinal);
            if (!genesis && !restore)
                return Fail("mode", "Only the fixed genesis and restore process modes are supported.", null);
            if (!int.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out int advanceCount)
                || advanceCount < 1 || advanceCount > 3)
                return Fail("advance-count", "The fixed process host accepts one to three daily advances.", null);

            storageRoot = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(storageRoot);
            DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
            DailyV1HostEnvironmentIdentity identity =
                DailyV1DotNetHostEnvironmentIdentityFactory.Capture(typeof(Program));
            var host = new DailyV1ApplicationHost(
                storageRoot, SystemAtomicFileSlotOperations.Instance,
                stageCompleted: null, logOutput: null, dotNetIdentity: identity);
            var application = new DailyV1Application();
            if (!application.TryStartDotNetOnExistingApplication(
                    definitions, host,
                    out DailyV1ApplicationStartPreparation preparation,
                    out P12AResult startResult,
                    out string startDiagnostic))
                return Fail("start", startDiagnostic, storageRoot);

            P12AResultCode expectedStartCode = genesis ? P12AResultCode.NoSave : P12AResultCode.Loaded;
            DailyV1ActiveRuntimeSession publishedSession = application.ActiveSession;
            long epochAtStart = publishedSession.ExecutionBoundaryState.MutationEpochState.CurrentEpoch;
            if (preparation == null || publishedSession == null
                || (genesis && !ReferenceEquals(publishedSession, preparation.CoreSession))
                || startResult == null || startResult.Code != expectedStartCode)
                return Fail("start-contract", "Core did not publish the expected exact session/start result: "
                    + (startResult == null ? "<null>" : startResult.Code.ToString()), storageRoot);

            for (int index = 0; index < advanceCount; index++)
            {
                if (!application.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure))
                    return Fail("advance", failure.ToString(), storageRoot);
            }

            if (!application.TrySaveDotNetContinuation(
                    out P12AResult saveResult, out string saveDiagnostic)
                || saveResult == null || saveResult.Code != P12AResultCode.Saved)
                return Fail("save", saveDiagnostic ?? (saveResult == null ? "<null>" : saveResult.Code.ToString()), storageRoot);

            DailyV1ActiveRuntimeSession session = application.ActiveSession;
            string slotPath = P12AContinuationStore.SlotPath(storageRoot);
            byte[] persistedBytes = File.ReadAllBytes(slotPath);
            if (!DailyV1CompatibilityDescriptorFactory.TryCreateDotNetV2(
                    definitions, identity,
                    out P12ACompatibilityDescriptorV2 descriptor,
                    out string descriptorDiagnostic)
                || descriptor == null)
                return Fail("descriptor", descriptorDiagnostic, storageRoot);
            if (!P12AContinuationStore.TryLoadDotNetV2(
                    manifest: null, descriptor, storageRoot, SystemAtomicFileSlotOperations.Instance,
                    out P12AContinuationImage image, out P12AResult decodedResult)
                || image == null || decodedResult == null || decodedResult.Code != P12AResultCode.Loaded)
                return Fail("decode", decodedResult == null ? "<null>" : decodedResult.Code.ToString(), storageRoot);
            if (image.Header.Day != session.SimulationTime.AbsoluteDay
                || image.Header.Sequence != session.ExecutionBoundaryState.CompletedCoreSequence
                || !string.Equals(image.Header.WorldId, session.WorldId.Value, StringComparison.Ordinal))
                return Fail("persisted-state", "The production P12 image does not identify the published Core session boundary.", storageRoot);

            int ownerRows = 0;
            int emptyOwnerSections = 0;
            foreach (P12AOwnerSection section in image.Sections)
            {
                ownerRows += section.Rows.Count;
                if (section.Rows.Count == 0) emptyOwnerSections++;
            }

            Console.WriteLine(
                "RESULT|mode=" + mode
                + "|pid=" + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture)
                + "|start=" + startResult.Code
                + "|profile=" + P12AContinuationHeader.Profile
                + "|world=" + image.Header.WorldId
                + "|day=" + image.Header.Day.ToString(CultureInfo.InvariantCulture)
                + "|sequence=" + image.Header.Sequence.ToString(CultureInfo.InvariantCulture)
                + "|epochStart=" + epochAtStart.ToString(CultureInfo.InvariantCulture)
                + "|epoch=" + session.ExecutionBoundaryState.MutationEpochState.CurrentEpoch.ToString(CultureInfo.InvariantCulture)
                + "|sections=" + image.Sections.Count.ToString(CultureInfo.InvariantCulture)
                + "|owners=" + ownerRows.ToString(CultureInfo.InvariantCulture)
                + "|emptySections=" + emptyOwnerSections.ToString(CultureInfo.InvariantCulture)
                + "|stateSha=" + ComputeComparablePersistedStateSha256(image)
                + "|p12sha=" + Sha256(persistedBytes));
            return 0;
        }
        catch (Exception exception)
        {
            return Fail("exception", exception.GetType().FullName + ": " + exception.Message, storageRoot);
        }
    }

    private static int Fail(string stage, string diagnostic, string storageRoot)
    {
        string safeDiagnostic = diagnostic ?? string.Empty;
        if (!string.IsNullOrEmpty(storageRoot))
            safeDiagnostic = safeDiagnostic.Replace(storageRoot, "<storage-root>", StringComparison.OrdinalIgnoreCase);
        Console.Error.WriteLine("ERROR|stage=" + stage + "|diagnostic=" + safeDiagnostic);
        return 1;
    }

    private static string ComputeComparablePersistedStateSha256(P12AContinuationImage image)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            P12AContinuationHeader header = image.Header;
            writer.Write(header.Day);
            writer.Write(header.Sequence);
            writer.Write(header.LastEconomySnapshotDay);
            writer.Write(header.PoliticalWorldRevision);
            writer.Write(header.P9ProfileStage ?? string.Empty);
            writer.Write(header.P9ManifestFingerprint ?? string.Empty);
            writer.Write(header.P8Profile ?? string.Empty);
            writer.Write(header.P8GeographyFingerprint ?? string.Empty);
            byte[] compatibility = header.CompatibilityBytes ?? Array.Empty<byte>();
            writer.Write(compatibility.Length);
            writer.Write(compatibility);
            if (!P12ACP9ManifestCodec.TryEncode(image.CausalRoots.P9Manifest,
                    out byte[] p9Manifest, out _)
                || !P12ACausalRootOwnerCodec.TryEncodeRandomRoot(image.CausalRoots.RandomRoot,
                    out byte[] randomRoot, out _))
                throw new InvalidOperationException("The decoded production causal roots could not be re-encoded.");
            writer.Write(p9Manifest.Length);
            writer.Write(p9Manifest);
            writer.Write(randomRoot.Length);
            writer.Write(randomRoot);
            writer.Write(image.Sections.Count);
            foreach (P12AOwnerSection section in image.Sections)
            {
                writer.Write(section.Id ?? string.Empty);
                writer.Write(section.Major);
                writer.Write(section.Minor);
                writer.Write((int)section.Role);
                writer.Write(section.AliasGroup ?? string.Empty);
                writer.Write(section.Revision);
                writer.Write(section.Rows.Count);
                foreach (P12AOwnerRow row in section.Rows)
                {
                    writer.Write(row.OwnerKey ?? string.Empty);
                    writer.Write(row.Revision);
                    byte[] payload = row.Payload ?? Array.Empty<byte>();
                    writer.Write(payload.Length);
                    writer.Write(payload);
                }
            }
            writer.Flush();
            return Sha256(stream.ToArray());
        }
    }

    private static string Sha256(byte[] bytes)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] digest = sha256.ComputeHash(bytes ?? Array.Empty<byte>());
            var text = new StringBuilder(digest.Length * 2);
            for (int index = 0; index < digest.Length; index++)
                text.Append(digest[index].ToString("x2", CultureInfo.InvariantCulture));
            return text.ToString();
        }
    }
}