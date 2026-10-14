using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public sealed class P12ACompatibilityDescriptor
{
    private readonly ReadOnlyCollection<string> mvids;
    private readonly byte[] canonicalBytes;
    private readonly byte[] canonicalDigest;

    public P12ACompatibilityDescriptor(string profileId, int profileVersion, string configurationFingerprint,
        string unityVersion, string runtimePlatform, string scriptingBackend, string buildGuid,
        IEnumerable<string> assemblyMvids, string numericProfileId, int numericProfileVersion,
        string contentFingerprint, string providerFingerprint, string calendarId, int calendarVersion,
        string calendarFingerprint, string p9Fingerprint, string p8Fingerprint)
    {
        ProfileId = Need(profileId); ProfileVersion = Positive(profileVersion);
        ConfigurationFingerprint = Need(configurationFingerprint); UnityVersion = Need(unityVersion);
        RuntimePlatform = Need(runtimePlatform); ScriptingBackend = Need(scriptingBackend);
        BuildGuid = buildGuid ?? ""; NumericProfileId = Need(numericProfileId);
        NumericProfileVersion = Positive(numericProfileVersion); ContentFingerprint = Need(contentFingerprint);
        ProviderFingerprint = Need(providerFingerprint); CalendarId = Need(calendarId);
        CalendarVersion = Positive(calendarVersion); CalendarFingerprint = Need(calendarFingerprint);
        P9Fingerprint = Need(p9Fingerprint); P8Fingerprint = Need(p8Fingerprint);
        if (assemblyMvids == null) throw new ArgumentNullException(nameof(assemblyMvids));
        List<string> sorted = new List<string>();
        foreach (string item in assemblyMvids)
        {
            if (!Guid.TryParseExact(item, "D", out Guid id)) throw new ArgumentException("Invalid assembly MVID.");
            sorted.Add(id.ToString("D").ToLowerInvariant());
        }
        if (sorted.Count == 0) throw new ArgumentException("At least one project assembly MVID is required.");
        sorted.Sort(StringComparer.Ordinal);
        for (int i = 1; i < sorted.Count; i++)
            if (sorted[i - 1] == sorted[i]) throw new ArgumentException("Duplicate assembly MVID.");
        mvids = new ReadOnlyCollection<string>(sorted);
        canonicalBytes = BuildCanonicalBytes();
        using (SHA256 sha = SHA256.Create()) canonicalDigest = sha.ComputeHash(canonicalBytes);
    }
    public string ProfileId { get; }
    public int ProfileVersion { get; }
    public string ConfigurationFingerprint { get; }
    public string UnityVersion { get; }
    public string RuntimePlatform { get; }
    public string ScriptingBackend { get; }
    public string BuildGuid { get; }
    public IReadOnlyList<string> AssemblyMvids => mvids;
    public string NumericProfileId { get; }
    public int NumericProfileVersion { get; }
    public string ContentFingerprint { get; }
    public string ProviderFingerprint { get; }
    public string CalendarId { get; }
    public int CalendarVersion { get; }
    public string CalendarFingerprint { get; }
    public string P9Fingerprint { get; }
    public string P8Fingerprint { get; }
    public byte[] CanonicalBytes => (byte[])canonicalBytes.Clone();
    public byte[] CanonicalDigest => (byte[])canonicalDigest.Clone();
    public bool Matches(byte[] bytes) => ContinuationWire.BytesEqual(canonicalBytes, bytes);

    public P12AResultCode MismatchCode(P12ACompatibilityDescriptor other)
    {
        if (other == null) return P12AResultCode.IncompatibleContentOrConfiguration;
        bool runtimeMismatch = UnityVersion != other.UnityVersion || RuntimePlatform != other.RuntimePlatform
            || ScriptingBackend != other.ScriptingBackend || BuildGuid != other.BuildGuid
            || NumericProfileId != other.NumericProfileId || NumericProfileVersion != other.NumericProfileVersion
            || !SameStrings(mvids, other.mvids);
        return runtimeMismatch ? P12AResultCode.IncompatibleBuildOrRuntime
            : P12AResultCode.IncompatibleContentOrConfiguration;
    }

    public static bool TryReadCanonical(byte[] bytes, out P12ACompatibilityDescriptor descriptor)
    {
        descriptor = null;
        if (bytes == null || bytes.Length > ContinuationWire.MaxHeaderBytes) return false;
        try
        {
            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (BinaryReader reader = new BinaryReader(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
            {
                if (!ContinuationWire.ReadString(reader, out string profile) || profile == null) return false;
                int profileVersion = reader.ReadInt32();
                if (!ContinuationWire.ReadString(reader, out string config) || config == null
                    || !ContinuationWire.ReadString(reader, out string unity) || unity == null
                    || !ContinuationWire.ReadString(reader, out string platform) || platform == null
                    || !ContinuationWire.ReadString(reader, out string backend) || backend == null
                    || !ContinuationWire.ReadString(reader, out string buildGuid) || buildGuid == null) return false;
                int count = reader.ReadInt32();
                if (count < 0 || count > 4096) return false;
                List<string> assemblies = new List<string>(count);
                for (int i = 0; i < count; i++)
                {
                    if (!ContinuationWire.ReadString(reader, out string mvid) || mvid == null) return false;
                    assemblies.Add(mvid);
                }
                if (!ContinuationWire.ReadString(reader, out string numericId) || numericId == null) return false;
                int numericVersion = reader.ReadInt32();
                if (!ContinuationWire.ReadString(reader, out string content) || content == null
                    || !ContinuationWire.ReadString(reader, out string providers) || providers == null
                    || !ContinuationWire.ReadString(reader, out string calendar) || calendar == null) return false;
                int calendarVersion = reader.ReadInt32();
                if (!ContinuationWire.ReadString(reader, out string calendarHash) || calendarHash == null
                    || !ContinuationWire.ReadString(reader, out string p9) || p9 == null
                    || !ContinuationWire.ReadString(reader, out string p8) || p8 == null
                    || stream.Position != stream.Length) return false;
                descriptor = new P12ACompatibilityDescriptor(profile, profileVersion, config, unity, platform,
                    backend, buildGuid, assemblies, numericId, numericVersion, content, providers,
                    calendar, calendarVersion, calendarHash, p9, p8);
                if (!ContinuationWire.BytesEqual(descriptor.canonicalBytes, bytes))
                { descriptor = null; return false; }
                return true;
            }
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is P12ALimitException
            || exception is ArgumentException || exception is DecoderFallbackException || exception is OverflowException)
        { descriptor = null; return false; }
    }

    private static bool SameStrings(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count) return false;
        for (int i = 0; i < left.Count; i++) if (left[i] != right[i]) return false;
        return true;
    }

    private byte[] BuildCanonicalBytes()
    {
        long encodedLength = 16L
            + StringFieldLength(ProfileId)
            + StringFieldLength(ConfigurationFingerprint)
            + StringFieldLength(UnityVersion)
            + StringFieldLength(RuntimePlatform)
            + StringFieldLength(ScriptingBackend)
            + StringFieldLength(BuildGuid)
            + StringFieldLength(NumericProfileId)
            + StringFieldLength(ContentFingerprint)
            + StringFieldLength(ProviderFingerprint)
            + StringFieldLength(CalendarId)
            + StringFieldLength(CalendarFingerprint)
            + StringFieldLength(P9Fingerprint)
            + StringFieldLength(P8Fingerprint);
        foreach (string mvid in mvids) encodedLength = checked(encodedLength + StringFieldLength(mvid));
        if (encodedLength > ContinuationWire.MaxHeaderBytes) throw new P12ALimitException();
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
        {
            ContinuationWire.WriteString(writer, ProfileId); writer.Write(ProfileVersion);
            ContinuationWire.WriteString(writer, ConfigurationFingerprint);
            ContinuationWire.WriteString(writer, UnityVersion); ContinuationWire.WriteString(writer, RuntimePlatform);
            ContinuationWire.WriteString(writer, ScriptingBackend); ContinuationWire.WriteString(writer, BuildGuid);
            if (mvids.Count > 4096) throw new P12ALimitException();
            writer.Write(mvids.Count); foreach (string id in mvids) ContinuationWire.WriteString(writer, id);
            ContinuationWire.WriteString(writer, NumericProfileId); writer.Write(NumericProfileVersion);
            ContinuationWire.WriteString(writer, ContentFingerprint); ContinuationWire.WriteString(writer, ProviderFingerprint);
            ContinuationWire.WriteString(writer, CalendarId); writer.Write(CalendarVersion);
            ContinuationWire.WriteString(writer, CalendarFingerprint);
            ContinuationWire.WriteString(writer, P9Fingerprint); ContinuationWire.WriteString(writer, P8Fingerprint);
            writer.Flush(); return stream.ToArray();
        }
    }
    private static long StringFieldLength(string value)
        => checked(4L + ContinuationWire.GetEncodedStringByteCount(value));
    private static string Need(string value)
    { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Compatibility value required."); return value; }
    private static int Positive(int value)
    { if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); return value; }
}
