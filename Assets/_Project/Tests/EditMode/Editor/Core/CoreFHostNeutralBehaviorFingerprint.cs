using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Produces the shared Daily-v1 state projection used to compare the Unity and
/// detached .NET hosts. Host/runtime/build identities and the MVID-derived
/// numeric profile ID and independently allocated Genesis WorldId are omitted.
/// Persisted continuation separately asserts exact WorldId retention; common compatibility
/// identities, causal-root, owner-key, alias, revision, exact-empty, and payload data remain bound.
/// </summary>
internal static class CoreFHostNeutralBehaviorFingerprint
{
    internal const string ExpectedUnityDotNetDayOneSha256 = "4d60d9a9e4e2a3f6e53f3ef904819fcf6d74f5981a6f290da9c58646cec994c3";
    internal static string Compute(P12AContinuationImage image)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));

        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("CORE-F-DailyV1-SharedBehavior-v1");

            P12AContinuationHeader header = image.Header;
            writer.Write(P12AContinuationHeader.Profile);
            writer.Write(header.Day);
            writer.Write(header.Sequence);
            writer.Write(header.LastEconomySnapshotDay);
            writer.Write(header.PoliticalWorldRevision);
            writer.Write(header.P9ProfileStage ?? string.Empty);
            writer.Write(header.P9ManifestFingerprint ?? string.Empty);
            writer.Write(header.P8Profile ?? string.Empty);
            writer.Write(header.P8GeographyFingerprint ?? string.Empty);
            P12ACompatibilityDescriptor compatibility = header.Compatibility;
            P12ACompatibilityDescriptorV2 compatibilityV2 = header.CompatibilityV2;
            if ((compatibility == null) == (compatibilityV2 == null))
                throw new InvalidOperationException("The P12 image must carry exactly one compatibility descriptor.");

            writer.Write(compatibility != null ? compatibility.ProfileId : compatibilityV2.ProfileId);
            writer.Write(compatibility != null ? compatibility.ProfileVersion : compatibilityV2.ProfileVersion);
            writer.Write(compatibility != null ? compatibility.ConfigurationFingerprint : compatibilityV2.ConfigurationFingerprint);
            // NumericProfileId includes per-host build/MVID identity and is intentionally not cross-host data.
            writer.Write(compatibility != null ? compatibility.NumericProfileVersion : compatibilityV2.NumericProfileVersion);
            writer.Write(compatibility != null ? compatibility.ContentFingerprint : compatibilityV2.ContentFingerprint);
            writer.Write(compatibility != null ? compatibility.ProviderFingerprint : compatibilityV2.ProviderFingerprint);
            writer.Write(compatibility != null ? compatibility.CalendarId : compatibilityV2.CalendarId);
            writer.Write(compatibility != null ? compatibility.CalendarVersion : compatibilityV2.CalendarVersion);
            writer.Write(compatibility != null ? compatibility.CalendarFingerprint : compatibilityV2.CalendarFingerprint);
            writer.Write(compatibility != null ? compatibility.P9Fingerprint : compatibilityV2.P9Fingerprint);
            writer.Write(compatibility != null ? compatibility.P8Fingerprint : compatibilityV2.P8Fingerprint);

            if (!P12ACP9ManifestCodec.TryEncode(image.CausalRoots.P9Manifest,
                    out byte[] p9Manifest, out _)
                || !P12ACausalRootOwnerCodec.TryEncodeRandomRoot(
                    image.CausalRoots.RandomRoot, out byte[] randomRoot, out _))
                throw new InvalidOperationException(
                    "The selected-profile causal roots could not be encoded for the shared behavior comparison.");

            WriteBytes(writer, p9Manifest);
            WriteBytes(writer, randomRoot);
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
                    WriteBytes(writer, row.Payload);
                }
            }

            writer.Flush();
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] digest = sha256.ComputeHash(stream.ToArray());
                var result = new StringBuilder(digest.Length * 2);
                foreach (byte value in digest)
                    result.Append(value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }

    private static void WriteBytes(BinaryWriter writer, byte[] bytes)
    {
        byte[] value = bytes ?? Array.Empty<byte>();
        writer.Write(value.Length);
        writer.Write(value);
    }
}
