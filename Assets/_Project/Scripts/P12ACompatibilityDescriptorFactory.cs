using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>Creates the current-host Daily-v1 compatibility identity without domain allocation.</summary>
internal static class P12ACompatibilityDescriptorFactory
{
    private const int ProviderContractVersion = 1;

    internal static bool TryCreate(SimulationConfigData configuration,
        EffectiveSimulationConfiguration effectiveConfiguration, CalendarDefinition calendar,
        string configurationFingerprint, P12CP9GenesisManifestSnapshot p9Manifest,
        string p8Fingerprint, out P12ACompatibilityDescriptor descriptor)
    {
        descriptor = null;
        if (configuration == null || effectiveConfiguration == null || calendar == null
            || string.IsNullOrWhiteSpace(configurationFingerprint)
            || p9Manifest == null || !p9Manifest.TryValidate(out _)
            || string.IsNullOrWhiteSpace(p8Fingerprint)) return false;

        try
        {
            string providerFingerprint = FingerprintProviders();
            string calendarFingerprint = FingerprintCalendar(calendar);
            if (!TryGetProjectAssemblyMvids(out string[] assemblyIds)) return false;
            string contentFingerprint = FingerprintContentCatalog(configuration);
#if ENABLE_IL2CPP
            const string backend = "IL2CPP";
#else
            const string backend = "Mono";
#endif
            string numericProfileId = CreateNumericProfileIdentity(backend,
                UnityEngine.Application.buildGUID, assemblyIds);
            descriptor = new P12ACompatibilityDescriptor(
                P12AContinuationHeader.Profile,
                P12AContinuationHeader.ProfileVersion,
                configurationFingerprint,
                UnityEngine.Application.unityVersion,
                UnityEngine.Application.platform.ToString(),
                backend,
                UnityEngine.Application.buildGUID,
                assemblyIds,
                numericProfileId,
                1,
                contentFingerprint,
                providerFingerprint,
                "simulation-calendar/v1",
                1,
                calendarFingerprint,
                p9Manifest.Fingerprint,
                p8Fingerprint);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException
            || exception is P12ALimitException || exception is CryptographicException
            || exception is IOException || exception is NotSupportedException
            || exception is TargetInvocationException || exception is UnauthorizedAccessException)
        {
            descriptor = null;
            return false;
        }
    }

    private static string FingerprintProviders()
    {
        return DailyV1DefinitionIdentity.CreateProviderFingerprint(
            P12ADailyV1Census.FixedRequiredSectionIds,
            P12ADailyV1Census.FixedEmptySectionIds,
            P12ADailyV1Census.NpcSectionPrefixes,
            P12ADailyV1Census.CitySectionPrefixes);
    }

    private static string FingerprintContentCatalog(SimulationConfigData configuration)
    {
        if (!P12ADailyV1ContentAdapter.TryCapture(configuration,
            out DailyV1SelectedContentProjection projection, out string diagnostic))
            throw new InvalidOperationException(diagnostic ?? "The Daily-v1 content projection is invalid.");
        return projection.Fingerprint;
    }

    private static bool TryGetProjectAssemblyMvids(out string[] mvids)
    {
        SortedSet<string> values = new SortedSet<string>(StringComparer.Ordinal);
        try
        {
            AddAssemblyMvid(typeof(SimulationRuntime).Assembly, values);
            string dataPath = Path.GetFullPath(UnityEngine.Application.dataPath);
            DirectoryInfo dataDirectory = new DirectoryInfo(dataPath);
            string projectRoot = string.Equals(dataDirectory.Name, "Assets", StringComparison.OrdinalIgnoreCase)
                ? dataDirectory.Parent?.FullName : null;
            List<string> roots = new List<string> { dataPath };
            if (!string.IsNullOrWhiteSpace(projectRoot))
                roots.Add(Path.Combine(projectRoot, "Library", "ScriptAssemblies"));
            string managedRoot = Path.Combine(dataPath, "Managed");
            roots.Add(managedRoot);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string location;
                try { location = assembly.Location; }
                catch (NotSupportedException) { continue; }
                if (string.IsNullOrWhiteSpace(location)) continue;
                string fullPath = Path.GetFullPath(location);
                if (roots.Any(root => IsUnderRoot(fullPath, root))) AddAssemblyMvid(assembly, values);
            }
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is IOException || exception is NotSupportedException
            || exception is UnauthorizedAccessException)
        {
            mvids = Array.Empty<string>();
            return false;
        }
        mvids = values.ToArray();
        return mvids.Length > 0;
    }

    private static bool IsUnderRoot(string path, string root)
    {
        string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddAssemblyMvid(Assembly assembly, SortedSet<string> values)
    {
        if (assembly == null) throw new InvalidOperationException("A project assembly is unavailable.");
        Guid mvid = assembly.ManifestModule.ModuleVersionId;
        if (mvid == Guid.Empty) throw new InvalidOperationException("A project assembly has no module identity.");
        values.Add(mvid.ToString("D").ToLowerInvariant());
    }

    private static string CreateNumericProfileIdentity(string backend, string buildGuid,
        IReadOnlyList<string> assemblyIds)
    {
        int singleOne = BitConverter.ToInt32(BitConverter.GetBytes(1.0f), 0);
        int singleNegativeZero = BitConverter.ToInt32(BitConverter.GetBytes(-0.0f), 0);
        long doubleOne = BitConverter.ToInt64(BitConverter.GetBytes(1.0d), 0);
        long doubleNegativeZero = BitConverter.ToInt64(BitConverter.GetBytes(-0.0d), 0);
        if (singleOne != unchecked((int)0x3f800000)
            || singleNegativeZero != unchecked((int)0x80000000)
            || doubleOne != 0x3ff0000000000000L
            || doubleNegativeZero != unchecked((long)0x8000000000000000UL))
            throw new InvalidOperationException("The host numeric representation is unsupported.");

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12.numeric.ieee754-binary32-binary64/canonical-le-wire");
            P12AWire.WriteString(writer, backend);
            P12AWire.WriteString(writer, buildGuid ?? string.Empty);
            writer.Write(assemblyIds.Count);
            foreach (string mvid in assemblyIds) P12AWire.WriteString(writer, mvid);
            writer.Flush();
            return "p12.numeric.ieee754-binary32-binary64/canonical-le-wire-v1/" + Sha256(stream.ToArray());
        }
    }

    private static string FingerprintCalendar(CalendarDefinition calendar)
    {
        if (!calendar.TryValidate(out _)) throw new ArgumentException(nameof(calendar));
        return DailyV1DefinitionIdentity.CreateCalendarFingerprint(new DailyV1CalendarDefinition(
            calendar.MonthsPerYear, calendar.WeeksPerMonth, calendar.DaysPerWeek, calendar.monthLengths));
    }

    private static string Sha256(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(bytes);
            char[] digits = new char[hash.Length * 2];
            const string hex = "0123456789abcdef";
            for (int i = 0; i < hash.Length; i++)
            { digits[i * 2] = hex[hash[i] >> 4]; digits[i * 2 + 1] = hex[hash[i] & 15]; }
            return new string(digits);
        }
    }
}
