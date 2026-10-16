using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

public enum DailyV1LogSeverity : byte
{
    Log = 0,
    Warning = 1,
    Error = 2
}

/// <summary>Host-captured environment identity for the fixed .NET proving host.</summary>
public sealed class DailyV1HostEnvironmentIdentity
{
    private readonly ReadOnlyCollection<P12AAssemblyIdentityV2> assemblies;

    public DailyV1HostEnvironmentIdentity(ushort hostKind,
        string runtimeFrameworkDescription, string runtimeVersion,
        string operatingSystemDescription, string runtimeIdentifier,
        string processArchitecture, string targetFramework,
        IEnumerable<P12AAssemblyIdentityV2> assemblies,
        string numericProfileId, int numericProfileVersion)
    {
        if (hostKind != 1)
            throw new ArgumentOutOfRangeException(nameof(hostKind), "The Daily-v1 .NET host identity requires HostKind 1.");
        HostKind = hostKind;
        RuntimeFrameworkDescription = NormalizeText(runtimeFrameworkDescription, nameof(runtimeFrameworkDescription));
        RuntimeVersion = CanonicalVersion(runtimeVersion, nameof(runtimeVersion));
        OperatingSystemDescription = NormalizeText(operatingSystemDescription, nameof(operatingSystemDescription));
        RuntimeIdentifier = CanonicalAscii(runtimeIdentifier, nameof(runtimeIdentifier), true);
        ProcessArchitecture = CanonicalAscii(processArchitecture, nameof(processArchitecture), false);
        TargetFramework = NormalizeText(targetFramework, nameof(targetFramework));
        if (!string.Equals(TargetFramework, "net8.0", StringComparison.Ordinal))
            throw new ArgumentException("The selected Daily-v1 host is pinned to net8.0.", nameof(targetFramework));
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));
        List<P12AAssemblyIdentityV2> copy = assemblies.Select(value =>
            value ?? throw new ArgumentException("A host assembly identity is null.", nameof(assemblies))).ToList();
        if (copy.Count < 1 || copy.Count > 4096)
            throw new ArgumentOutOfRangeException(nameof(assemblies));
        copy.Sort(CompareAssembly);
        for (int i = 1; i < copy.Count; i++)
            if (CompareAssembly(copy[i - 1], copy[i]) == 0)
                throw new ArgumentException("The host assembly manifest contains a duplicate identity.", nameof(assemblies));
        this.assemblies = new ReadOnlyCollection<P12AAssemblyIdentityV2>(copy);
        NumericProfileId = Need(numericProfileId, nameof(numericProfileId));
        if (numericProfileVersion <= 0) throw new ArgumentOutOfRangeException(nameof(numericProfileVersion));
        NumericProfileVersion = numericProfileVersion;
    }

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

    private static string Need(string value, string parameter)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A required host identity field is empty.", parameter) : value;

    private static string NormalizeText(string value, string parameter)
        => Need(value, parameter).Trim().Normalize(NormalizationForm.FormC);

    private static string CanonicalVersion(string value, string parameter)
    {
        string[] components = Need(value, parameter).Split('.');
        if (components.Length < 1 || components.Length > 4)
            throw new ArgumentException("The host version must have one to four numeric components.", parameter);
        int[] numbers = new int[4];
        for (int i = 0; i < components.Length; i++)
            if (components[i].Length == 0 || components[i].Any(character => character < '0' || character > '9')
                || !int.TryParse(components[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
                throw new ArgumentException("The host version is not canonical numeric text.", parameter);
        return string.Join(".", numbers.Select(number => number.ToString(CultureInfo.InvariantCulture)));
    }

    private static string CanonicalAscii(string value, string parameter, bool allowHyphens)
    {
        string text = Need(value, parameter).Trim().ToLowerInvariant();
        bool priorHyphen = true;
        foreach (char character in text)
        {
            bool asciiAlphaNumeric = character >= 'a' && character <= 'z'
                || character >= '0' && character <= '9';
            if (asciiAlphaNumeric) { priorHyphen = false; continue; }
            if (!allowHyphens || character != '-' || priorHyphen)
                throw new ArgumentException("The host field must use canonical ASCII text.", parameter);
            priorHyphen = true;
        }
        if (priorHyphen) throw new ArgumentException("The host field must end in an ASCII letter or digit.", parameter);
        return text;
    }

    private static int CompareAssembly(P12AAssemblyIdentityV2 left, P12AAssemblyIdentityV2 right)
    {
        int comparison = StringComparer.Ordinal.Compare(left.SimpleName, right.SimpleName);
        if (comparison != 0) return comparison;
        comparison = Version.Parse(left.Version).CompareTo(Version.Parse(right.Version));
        return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(left.Mvid, right.Mvid);
    }
}

/// <summary>Explicit host ports for one shared Daily-v1 application instance.</summary>
public sealed class DailyV1ApplicationHost
{
    public DailyV1ApplicationHost(
        string persistentDataRoot,
        IAtomicFileSlotOperations fileOperations,
        Action<string> stageCompleted,
        Action<DailyV1LogSeverity, string> logOutput,
        DailyV1HostEnvironmentIdentity dotNetIdentity = null,
        P12ACompatibilityDescriptor unityV1Identity = null)
    {
        if (string.IsNullOrWhiteSpace(persistentDataRoot))
            throw new ArgumentException("An explicit continuation storage root is required.", nameof(persistentDataRoot));
        if (fileOperations == null) throw new ArgumentNullException(nameof(fileOperations));
        if ((dotNetIdentity == null) == (unityV1Identity == null))
            throw new ArgumentException("Exactly one .NET v2 or Unity v1 host identity must be supplied.");

        PersistentDataRoot = persistentDataRoot;
        FileOperations = fileOperations;
        StageCompleted = stageCompleted;
        LogOutput = logOutput;
        DotNetIdentity = dotNetIdentity;
        UnityV1Identity = unityV1Identity;
    }

    internal string PersistentDataRoot { get; }
    internal IAtomicFileSlotOperations FileOperations { get; }
    internal Action<string> StageCompleted { get; }
    internal Action<DailyV1LogSeverity, string> LogOutput { get; }
    internal DailyV1HostEnvironmentIdentity DotNetIdentity { get; }
    internal P12ACompatibilityDescriptor UnityV1Identity { get; }
}
