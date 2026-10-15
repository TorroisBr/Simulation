using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

public sealed class P12AAssemblyIdentityV2
{
    public P12AAssemblyIdentityV2(string simpleName, string version, string mvid)
    { SimpleName = NormalizeText(simpleName); Version = CanonicalVersion(version); Mvid = Guid.ParseExact(Need(mvid), "D").ToString("D").ToLowerInvariant(); }
    public string SimpleName { get; }
    public string Version { get; }
    public string Mvid { get; }
    private static string Need(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Required value.") : value;
    private static string NormalizeText(string value) => Need(value).Trim().Normalize(NormalizationForm.FormC);
    private static string CanonicalVersion(string value)
    {
        string[] parts = Need(value).Split('.');
        if (parts.Length < 1 || parts.Length > 4)
            throw new ArgumentException("Invalid version.");
        int[] numbers = new int[4];
        for (int i = 0; i < parts.Length; i++)
            if (parts[i].Length == 0 || parts[i].Any(c => c < '0' || c > '9')
                || !int.TryParse(parts[i], System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out numbers[i]))
                throw new ArgumentException("Invalid version.");
        return string.Join(".", numbers.Select(n => n.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }
}

public sealed class P12ACompatibilityDescriptorV2
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("P12ACDV2");
    private readonly ReadOnlyCollection<P12AAssemblyIdentityV2> assemblies;
    private readonly byte[] canonicalBytes;
    public P12ACompatibilityDescriptorV2(string profileId, int profileVersion, ushort hostKind,
        string runtimeFrameworkDescription, string runtimeVersion, string operatingSystemDescription,
        string runtimeIdentifier, string processArchitecture, string targetFramework,
        IEnumerable<P12AAssemblyIdentityV2> assemblies, string numericProfileId, int numericProfileVersion,
        string configurationFingerprint, string contentFingerprint, string providerFingerprint,
        string calendarId, int calendarVersion, string calendarFingerprint, string p9Fingerprint, string p8Fingerprint)
    {
        ProfileId = Need(profileId);
        ProfileVersion = Positive(profileVersion);
        HostKind = hostKind;
        RuntimeFrameworkDescription = NormalizeText(runtimeFrameworkDescription);
        RuntimeVersion = CanonicalVersion(runtimeVersion);
        OperatingSystemDescription = NormalizeText(operatingSystemDescription);
        RuntimeIdentifier = CanonicalAscii(runtimeIdentifier, true);
        ProcessArchitecture = CanonicalAscii(processArchitecture, false);
        TargetFramework = NormalizeText(targetFramework);
        if (assemblies == null)
            throw new ArgumentNullException(nameof(assemblies));
        List<P12AAssemblyIdentityV2> list = assemblies.Select(a => a ?? throw new ArgumentException("Null assembly identity.")).ToList();
        list.Sort(CompareAssembly);
        if (list.Count < 1 || list.Count > 4096)
            throw new ArgumentException("Assembly count out of range.");
        for (int i = 1; i < list.Count; i++)
            if (CompareAssembly(list[i - 1], list[i]) == 0)
                throw new ArgumentException("Duplicate assembly identity.");
        this.assemblies = new ReadOnlyCollection<P12AAssemblyIdentityV2>(list);
        NumericProfileId = Need(numericProfileId);
        NumericProfileVersion = Positive(numericProfileVersion);
        ConfigurationFingerprint = Need(configurationFingerprint);
        ContentFingerprint = Need(contentFingerprint);
        ProviderFingerprint = Need(providerFingerprint);
        CalendarId = Need(calendarId);
        CalendarVersion = Positive(calendarVersion);
        CalendarFingerprint = Need(calendarFingerprint);
        P9Fingerprint = Need(p9Fingerprint);
        P8Fingerprint = Need(p8Fingerprint);
        if (hostKind != 1 && hostKind != 2)
            throw new ArgumentOutOfRangeException(nameof(hostKind));
        canonicalBytes = Encode();
    }
    public string ProfileId { get; }
    public int ProfileVersion { get; }
    public ushort HostKind { get; }
    public string RuntimeFrameworkDescription { get; }
    public string RuntimeVersion { get; }
    public string OperatingSystemDescription { get; }
    public string RuntimeIdentifier { get; }
    public string ProcessArchitecture { get; }
    public string TargetFramework { get; }
    public IReadOnlyList<P12AAssemblyIdentityV2> Assemblies => assemblies;
    public string NumericProfileId { get; }
    public int NumericProfileVersion { get; }
    public string ConfigurationFingerprint { get; }
    public string ContentFingerprint { get; }
    public string ProviderFingerprint { get; }
    public string CalendarId { get; }
    public int CalendarVersion { get; }
    public string CalendarFingerprint { get; }
    public string P9Fingerprint { get; }
    public string P8Fingerprint { get; }
    public byte[] CanonicalBytes => (byte[])canonicalBytes.Clone();
    public P12AResultCode MismatchCode(P12ACompatibilityDescriptorV2 other)
    {
        if (other == null || HostKind != other.HostKind || RuntimeFrameworkDescription != other.RuntimeFrameworkDescription || RuntimeVersion != other.RuntimeVersion || OperatingSystemDescription != other.OperatingSystemDescription || RuntimeIdentifier != other.RuntimeIdentifier || ProcessArchitecture != other.ProcessArchitecture || TargetFramework != other.TargetFramework || NumericProfileId != other.NumericProfileId || NumericProfileVersion != other.NumericProfileVersion || !SameAssemblies(other))
            return P12AResultCode.IncompatibleBuildOrRuntime;
        if (ProfileId != other.ProfileId)
            return P12AResultCode.UnsupportedProfile;
        if (ProfileVersion != other.ProfileVersion)
            return P12AResultCode.UnsupportedSchema;
        return ConfigurationFingerprint != other.ConfigurationFingerprint || ContentFingerprint != other.ContentFingerprint || ProviderFingerprint != other.ProviderFingerprint || CalendarId != other.CalendarId || CalendarVersion != other.CalendarVersion || CalendarFingerprint != other.CalendarFingerprint || P9Fingerprint != other.P9Fingerprint || P8Fingerprint != other.P8Fingerprint ? P12AResultCode.IncompatibleContentOrConfiguration : P12AResultCode.Decoded;
    }
    public static bool TryReadCanonical(byte[] bytes, out P12ACompatibilityDescriptorV2 descriptor, out P12AResultCode result)
    {
        if (!TryReadStructuralCanonical(bytes, out descriptor, out result))
            return false;
        if (descriptor.ProfileId != "UnityBootstrap-Daily-v1")
        {
            descriptor = null;
            result = P12AResultCode.UnsupportedProfile;
            return false;
        }
        if (descriptor.ProfileVersion != 1)
        {
            descriptor = null;
            result = P12AResultCode.UnsupportedSchema;
            return false;
        }
        return true;
    }

    // One structural parser serves both profile-aware reading and host admission.
    internal static bool TryReadStructuralCanonical(byte[] bytes, out P12ACompatibilityDescriptorV2 descriptor, out P12AResultCode result)
    {
        descriptor = null;
        result = P12AResultCode.CorruptOrTruncated;
        if (bytes == null || bytes.Length > ContinuationWire.MaxHeaderBytes)
            return false;
        try
        {
            using (var ms = new MemoryStream(bytes, false))
            using (var r = new BinaryReader(ms, ContinuationWire.CreateStrictUtf8Encoding(), true))
            {
                if (r.ReadInt32() != -2)
                    return false;
                byte[] magic = r.ReadBytes(8);
                if (magic.Length != 8 || !ContinuationWire.BytesEqual(magic, Magic))
                    return false;
                ushort format = r.ReadUInt16(), flags = r.ReadUInt16();
                if (format != 2 || flags != 0)
                { result = P12AResultCode.UnsupportedSchema; return false; }
                if (!ReadRequired(r, out var profile))
                    return false;
                int pv = r.ReadInt32();
                ushort host = r.ReadUInt16(), hostflags = r.ReadUInt16();
                if (hostflags != 0)
                { result = P12AResultCode.UnsupportedSchema; return false; }
                if (host != 1 && host != 2)
                { result = P12AResultCode.UnsupportedSchema; return false; }
                if (!ReadRequired(r, out var framework) || !ReadRequired(r, out var rv) || !ReadRequired(r, out var os) || !ReadRequired(r, out var rid) || !ReadRequired(r, out var arch) || !ReadRequired(r, out var tfm))
                    return false;
                int count = r.ReadInt32();
                if (count < 1 || count > 4096)
                    return false;
                var aa = new List<P12AAssemblyIdentityV2>(count);
                for (int i = 0; i < count; i++)
                { if (!ReadRequired(r, out var n) || !ReadRequired(r, out var v) || !ReadRequired(r, out var m)) return false; aa.Add(new P12AAssemblyIdentityV2(n, v, m)); }
                if (!ReadRequired(r, out var numeric))
                    return false;
                int nv = r.ReadInt32();
                if (!ReadRequired(r, out var config) || !ReadRequired(r, out var content) || !ReadRequired(r, out var provider) || !ReadRequired(r, out var calendar))
                    return false;
                int cv = r.ReadInt32();
                if (!ReadRequired(r, out var ch) || !ReadRequired(r, out var p9) || !ReadRequired(r, out var p8) || ms.Position != ms.Length)
                    return false;
                var parsedDescriptor = new P12ACompatibilityDescriptorV2(
                    profile, pv, host, framework, rv, os, rid, arch, tfm, aa,
                    numeric, nv, config, content, provider, calendar, cv, ch, p9, p8);
                if (!ContinuationWire.BytesEqual(bytes, parsedDescriptor.canonicalBytes))
                    return false;

                descriptor = parsedDescriptor;
                result = P12AResultCode.Decoded;
                return true;
            }
        }
        catch (Exception e) when (e is EndOfStreamException || e is DecoderFallbackException || e is ArgumentException || e is P12ALimitException || e is OverflowException) { descriptor = null; return false; }
    }
    private byte[] Encode()
    {
        using (var m = new MemoryStream())
        using (var w = new BinaryWriter(m, ContinuationWire.CreateStrictUtf8Encoding(), true))
        {
            w.Write(-2);
            w.Write(Magic);
            w.Write((ushort)2);
            w.Write((ushort)0);
            S(w, ProfileId);
            w.Write(ProfileVersion);
            w.Write(HostKind);
            w.Write((ushort)0);
            S(w, RuntimeFrameworkDescription);
            S(w, RuntimeVersion);
            S(w, OperatingSystemDescription);
            S(w, RuntimeIdentifier);
            S(w, ProcessArchitecture);
            S(w, TargetFramework);
            w.Write(assemblies.Count);
            foreach (var a in assemblies)
            { S(w, a.SimpleName); S(w, a.Version); S(w, a.Mvid); }
            S(w, NumericProfileId);
            w.Write(NumericProfileVersion);
            S(w, ConfigurationFingerprint);
            S(w, ContentFingerprint);
            S(w, ProviderFingerprint);
            S(w, CalendarId);
            w.Write(CalendarVersion);
            S(w, CalendarFingerprint);
            S(w, P9Fingerprint);
            S(w, P8Fingerprint);
            w.Flush();
            if (m.Length > ContinuationWire.MaxHeaderBytes)
                throw new P12ALimitException();
            return m.ToArray();
        }
    }
    private bool SameAssemblies(P12ACompatibilityDescriptorV2 o) => assemblies.Count == o.assemblies.Count && !assemblies.Where((a, i) => CompareAssembly(a, o.assemblies[i]) != 0).Any();
    private static int CompareAssembly(P12AAssemblyIdentityV2 a, P12AAssemblyIdentityV2 b) { int c = StringComparer.Ordinal.Compare(a.SimpleName, b.SimpleName); if (c != 0) return c; c = CompareVersion(a.Version, b.Version); return c != 0 ? c : StringComparer.Ordinal.Compare(a.Mvid, b.Mvid); }
    private static int CompareVersion(string a, string b) { if (Version.TryParse(a, out var av) && Version.TryParse(b, out var bv)) return av.CompareTo(bv); return StringComparer.Ordinal.Compare(a, b); }
    private static void S(BinaryWriter w, string s) { if (s == null) throw new ArgumentNullException(nameof(s)); ContinuationWire.WriteString(w, s); }
    private static bool ReadRequired(BinaryReader r, out string s) => ContinuationWire.ReadString(r, out s) && s != null;
    private static string Need(string s) => string.IsNullOrWhiteSpace(s) ? throw new ArgumentException("Required value.") : s;
    private static string NormalizeText(string s) => Need(s).Trim().Normalize(NormalizationForm.FormC);
    private static string CanonicalAscii(string s, bool hyphenated)
    {
        s = Need(s).ToLowerInvariant();
        if (s.Any(c => c > 127 || (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && (!hyphenated || c != '-'))))
            throw new ArgumentException("Expected canonical ASCII identity.");
        if (hyphenated && (s.StartsWith("-") || s.EndsWith("-") || s.Contains("--")))
            throw new ArgumentException("Invalid runtime identifier.");
        return s;
    }
    private static string CanonicalVersion(string s)
    {
        string[] parts = Need(s).Split('.');
        if (parts.Length < 1 || parts.Length > 4)
            throw new ArgumentException("Invalid version.");
        int[] n = new int[4];
        for (int i = 0; i < parts.Length; i++)
            if (parts[i].Length == 0 || parts[i].Any(c => c < '0' || c > '9') || !int.TryParse(parts[i], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out n[i]))
                throw new ArgumentException("Invalid version.");
        return string.Join(".", n.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }
    private static int Positive(int n) => n <= 0 ? throw new ArgumentOutOfRangeException(nameof(n)) : n;
}

public static class P12ACompatibilityAdmission
{
    public static bool TryReadV2(byte[] bytes, out P12ACompatibilityDescriptorV2 descriptor, out P12AResultCode result) => P12ACompatibilityDescriptorV2.TryReadCanonical(bytes, out descriptor, out result);
    public static P12AResultCode AdmitUnityV1(byte[] bytes, P12ACompatibilityDescriptor expected)
    {
        if (bytes == null || expected == null)
            return P12AResultCode.InvalidOwnerValue;
        if (IsV2(bytes))
            return ClassifyUnityV2(bytes);
        if (!P12ACompatibilityDescriptor.TryReadCanonical(bytes, out var actual))
            return P12AResultCode.CorruptOrTruncated;
        return expected.Matches(bytes) ? P12AResultCode.Decoded : expected.MismatchCode(actual);
    }
    public static P12AResultCode AdmitDotNetV2(byte[] bytes, P12ACompatibilityDescriptorV2 expected, out P12ACompatibilityDescriptorV2 actual)
    {
        actual = null;
        if (bytes == null || expected == null)
            return P12AResultCode.InvalidOwnerValue;
        if (!IsV2(bytes))
            return P12ACompatibilityDescriptor.TryReadCanonical(bytes, out _)
            ? P12AResultCode.IncompatibleBuildOrRuntime : P12AResultCode.CorruptOrTruncated;
        if (!P12ACompatibilityDescriptorV2.TryReadStructuralCanonical(bytes, out actual, out var parsed))
            return parsed;
        if (actual.HostKind != 1)
            return P12AResultCode.IncompatibleBuildOrRuntime;
        if (actual.ProfileId != "UnityBootstrap-Daily-v1")
            return P12AResultCode.UnsupportedProfile;
        if (actual.ProfileVersion != 1)
            return P12AResultCode.UnsupportedSchema;
        if (!string.Equals(expected.TargetFramework, "net8.0", StringComparison.Ordinal)
            || !string.Equals(actual.TargetFramework, "net8.0", StringComparison.Ordinal))
            return P12AResultCode.IncompatibleBuildOrRuntime;
        return actual.MismatchCode(expected);
    }
    public static P12AResultCode ClassifyUnityV2(byte[] bytes)
    {
        if (!P12ACompatibilityDescriptorV2.TryReadStructuralCanonical(bytes, out _, out var result))
            return result;

        return P12AResultCode.UnsupportedSchema;
    }
    private static bool IsV2(byte[] bytes) => bytes.Length >= 4 && bytes[0] == 0xFE && bytes[1] == 0xFF && bytes[2] == 0xFF && bytes[3] == 0xFF;
}
