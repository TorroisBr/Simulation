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
    private const int MaxCatalogAssets = 65536;
    private const int MaxCatalogBytes = 64 * 1024 * 1024;
    private const int MaxValueDepth = 64;

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
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.daily-v1.provider-contract");
            writer.Write(ProviderContractVersion);
            WriteGroup(writer, "required", P12ADailyV1Census.FixedRequiredSectionIds);
            WriteGroup(writer, "empty", P12ADailyV1Census.FixedEmptySectionIds);
            WriteGroup(writer, "npc-prefix", P12ADailyV1Census.NpcSectionPrefixes);
            WriteGroup(writer, "city-prefix", P12ADailyV1Census.CitySectionPrefixes);
            writer.Flush();
            return Sha256(stream.ToArray());
        }
    }

    private static void WriteGroup(BinaryWriter writer, string group, IReadOnlyList<string> values)
    {
        P12AWire.WriteString(writer, group);
        writer.Write(ProviderContractVersion);
        writer.Write(values.Count);
        foreach (string value in values)
        {
            P12AWire.WriteString(writer, value);
            // The selected Daily-v1 manifest currently pins schema v1 for every
            // fixed provider and dynamic provider family. A schema change must
            // update this contract version and the image manifest validation.
            writer.Write(1);
        }
    }

    private static string FingerprintContentCatalog(SimulationConfigData configuration)
    {
        SortedDictionary<string, ScriptableObject> assets =
            new SortedDictionary<string, ScriptableObject>(StringComparer.Ordinal);
        SortedDictionary<string, byte[]> records = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        Queue<ScriptableObject> pending = new Queue<ScriptableObject>();
        byte[] profileRecord = SerializeObjectFields(configuration, assets, pending);
        records.Add("profile-config/SimulationConfigData", profileRecord);
        int discovered = 0;
        while (pending.Count > 0)
        {
            ScriptableObject asset = pending.Dequeue();
            string key = AssetKey(asset);
            if (records.ContainsKey(key)) continue;
            if (++discovered > MaxCatalogAssets)
                throw new InvalidOperationException("The Daily-v1 content catalog exceeds its asset bound.");
            if (!assets.TryGetValue(key, out ScriptableObject registered) || !ReferenceEquals(registered, asset))
                throw new InvalidOperationException("The Daily-v1 content catalog has an ambiguous stable definition identity.");
            records.Add(key, SerializeObjectFields(asset, assets, pending));
        }

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.official-content-catalog/v1");
            writer.Write(records.Count);
            foreach (KeyValuePair<string, byte[]> record in records)
            {
                P12AWire.WriteString(writer, record.Key);
                writer.Write(record.Value.Length);
                writer.Write(record.Value);
                if (stream.Length > MaxCatalogBytes)
                    throw new InvalidOperationException("The Daily-v1 content catalog exceeds its byte bound.");
            }
            writer.Flush();
            return Sha256(stream.ToArray());
        }
    }

    private static byte[] SerializeObjectFields(object value,
        SortedDictionary<string, ScriptableObject> assets, Queue<ScriptableObject> pending)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            Type type = value.GetType();
            P12AWire.WriteString(writer, type.FullName ?? type.Name);
            List<FieldInfo> fields = SerializableFields(type);
            writer.Write(fields.Count);
            foreach (FieldInfo field in fields)
            {
                P12AWire.WriteString(writer, field.DeclaringType?.FullName ?? type.FullName ?? type.Name);
                P12AWire.WriteString(writer, field.Name);
                WriteStableValue(writer, field.GetValue(value), 0, assets, pending);
            }
            writer.Flush();
            if (stream.Length > MaxCatalogBytes)
                throw new InvalidOperationException("A Daily-v1 content definition exceeds its byte bound.");
            return stream.ToArray();
        }
    }

    private static List<FieldInfo> SerializableFields(Type type)
    {
        List<FieldInfo> fields = new List<FieldInfo>();
        for (Type current = type; current != null && current != typeof(object)
            && current != typeof(UnityEngine.Object); current = current.BaseType)
        {
            FieldInfo[] declared = current.GetFields(BindingFlags.Instance | BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (FieldInfo field in declared)
            {
                if (field.IsStatic || field.IsNotSerialized || field.IsInitOnly) continue;
                bool serialized = field.IsPublic
                    || Attribute.IsDefined(field, typeof(SerializeField), true)
                    || Attribute.IsDefined(field, typeof(SerializeReference), true);
                if (serialized) fields.Add(field);
            }
        }
        fields.Sort((left, right) =>
        {
            int typeCompare = StringComparer.Ordinal.Compare(
                left.DeclaringType?.FullName, right.DeclaringType?.FullName);
            return typeCompare != 0 ? typeCompare : StringComparer.Ordinal.Compare(left.Name, right.Name);
        });
        return fields;
    }

    private static void WriteStableValue(BinaryWriter writer, object value, int depth,
        SortedDictionary<string, ScriptableObject> assets, Queue<ScriptableObject> pending)
    {
        if (depth > MaxValueDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        if (value == null)
        {
            writer.Write((byte)0);
            return;
        }

        Type type = value.GetType();
        P12AWire.WriteString(writer, type.FullName ?? type.Name);
        if (value is UnityEngine.Object unityObject)
        {
            if (!(unityObject is ScriptableObject scriptable))
                throw new InvalidOperationException("The Daily-v1 catalog contains an unsupported Unity object reference.");
            string key = AssetKey(scriptable);
            if (assets.TryGetValue(key, out ScriptableObject prior))
            {
                if (!ReferenceEquals(prior, scriptable))
                    throw new InvalidOperationException("The Daily-v1 catalog has duplicate definition identities.");
            }
            else
            {
                assets.Add(key, scriptable);
                pending.Enqueue(scriptable);
            }
            writer.Write((byte)1);
            P12AWire.WriteString(writer, key);
            return;
        }

        if (value is string text)
        {
            writer.Write((byte)2);
            P12AWire.WriteString(writer, text);
            return;
        }
        if (type.IsEnum)
        {
            writer.Write((byte)3);
            P12AWire.WriteString(writer, Convert.ToString(value, CultureInfo.InvariantCulture));
            return;
        }
        if (value is bool boolean) { writer.Write((byte)4); writer.Write(boolean); return; }
        if (value is byte byteValue) { writer.Write((byte)5); writer.Write(byteValue); return; }
        if (value is sbyte sbyteValue) { writer.Write((byte)6); writer.Write(sbyteValue); return; }
        if (value is short shortValue) { writer.Write((byte)7); writer.Write(shortValue); return; }
        if (value is ushort ushortValue) { writer.Write((byte)8); writer.Write(ushortValue); return; }
        if (value is int intValue) { writer.Write((byte)9); writer.Write(intValue); return; }
        if (value is uint uintValue) { writer.Write((byte)10); writer.Write(uintValue); return; }
        if (value is long longValue) { writer.Write((byte)11); writer.Write(longValue); return; }
        if (value is ulong ulongValue) { writer.Write((byte)12); writer.Write(ulongValue); return; }
        if (value is char charValue) { writer.Write((byte)13); writer.Write((ushort)charValue); return; }
        if (value is float singleValue) { writer.Write((byte)14); writer.Write(BitConverter.ToInt32(BitConverter.GetBytes(singleValue), 0)); return; }
        if (value is double doubleValue) { writer.Write((byte)15); writer.Write(BitConverter.ToInt64(BitConverter.GetBytes(doubleValue), 0)); return; }
        if (value is decimal decimalValue)
        {
            writer.Write((byte)16);
            foreach (int part in decimal.GetBits(decimalValue)) writer.Write(part);
            return;
        }
        if (value is Guid guid) { writer.Write((byte)17); P12AWire.WriteString(writer, guid.ToString("N")); return; }
        if (value is DateTime dateTime) { writer.Write((byte)18); writer.Write(dateTime.ToBinary()); return; }
        if (value is TimeSpan timeSpan) { writer.Write((byte)19); writer.Write(timeSpan.Ticks); return; }
        if (value is IDictionary)
            throw new InvalidOperationException("Dictionary values are not admitted in the authored Daily-v1 catalog.");
        if (value is IList list)
        {
            writer.Write((byte)20);
            writer.Write(list.Count);
            foreach (object item in list) WriteStableValue(writer, item, depth + 1, assets, pending);
            return;
        }

        List<FieldInfo> fields = SerializableFields(type);
        if (fields.Count == 0)
            throw new InvalidOperationException("The Daily-v1 catalog contains an unsupported serialized value: " + type.FullName);
        writer.Write((byte)21);
        writer.Write(fields.Count);
        foreach (FieldInfo field in fields)
        {
            P12AWire.WriteString(writer, field.DeclaringType?.FullName ?? type.FullName ?? type.Name);
            P12AWire.WriteString(writer, field.Name);
            WriteStableValue(writer, field.GetValue(value), depth + 1, assets, pending);
        }
    }

    private static string AssetKey(ScriptableObject asset)
    {
        if (asset == null) throw new InvalidOperationException("A Daily-v1 definition reference is missing.");
        Type type = asset.GetType();
        PropertyInfo property = type.GetProperty("DefinitionId", BindingFlags.Public | BindingFlags.Instance);
        string id = property?.GetValue(asset, null) as string;
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException("A Daily-v1 definition lacks a stable DefinitionId.");
        return (type.FullName ?? type.Name) + "/" + id;
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
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            writer.Write(calendar.MonthsPerYear);
            writer.Write(calendar.WeeksPerMonth);
            writer.Write(calendar.DaysPerWeek);
            IReadOnlyList<int> lengths = calendar.monthLengths;
            writer.Write(lengths == null ? 0 : lengths.Count);
            if (lengths != null)
                foreach (int length in lengths) writer.Write(length);
            writer.Flush();
            return Sha256(stream.ToArray());
        }
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