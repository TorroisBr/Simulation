using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Run on the pre-refactor Unity checkout and retain the emitted values as WP1 parity vectors.
/// This deliberately captures effective configuration and both legacy identity paths separately.
/// </summary>
public sealed class CoreFDailyV1ValueBaselineTests
{
    [Test]
    public void CaptureDailyV1ResolvedValueAndFingerprintBaseline()
    {
        const string profilePath = "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset";
        SimulationConfigData authoring = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(profilePath);
        Assert.That(authoring, Is.Not.Null);
        Assert.That(authoring.simulationName, Is.EqualTo("Daily v1"));

        SimulationConfigurationResolutionResult resolution = SimulationConfigurationResolver.Resolve(
            null, null, authoring.CreateConfigurationOverrides());
        Assert.That(resolution.IsValid, Is.True, string.Join("; ", resolution.Validation.Errors));
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.calendar, out string diagnostic);
        Assert.That(string.IsNullOrEmpty(diagnostic), Is.True, diagnostic);

        string p9 = SimulationGenesisPipeline.CreateFingerprint(
            authoring, resolution.Configuration, calendar, out IReadOnlyList<string> p9Records);
        string[] retained = File.ReadAllLines(Path.Combine("tests", "Simulation.Core.Tests", "Fixtures", "CORE_F_WP1_VALUES_BASELINE.txt"),
            new UTF8Encoding(false, true));
        Assert.That(p9, Is.EqualTo(RetainedValue(retained, "p9")), "Typed P9 fingerprint must preserve the pre-refactor identity.");
        CollectionAssert.AreEqual(RetainedRecords(retained, "p9-record="), p9Records,
            "Typed P9 builder must preserve every ordered pre-refactor record.");
        MethodInfo catalogMethod = typeof(P12ACompatibilityDescriptorFactory).GetMethod(
            "FingerprintContentCatalog", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(catalogMethod, Is.Not.Null);
        string p12 = (string)catalogMethod.Invoke(null, new object[] { authoring });

        byte[] catalogBytes = CaptureCatalogBytes(authoring, catalogMethod);
        Assert.That(Sha256(catalogBytes), Is.EqualTo(p12), "The retained P12 bytes must be the production encoder output.");
        List<string> orderedValues = CaptureSelectedMemberValues(authoring);
        CollectionAssert.AreEqual(RetainedRecords(retained, "member="), orderedValues,
            "The selected 164-field/718-occurrence input vector changed from the retained Unity baseline.");
        string provider = InvokePrivateFingerprint("FingerprintProviders");
        string calendarIdentity = InvokePrivateFingerprint("FingerprintCalendar", calendar);
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library", "CORE_F_WP1_VALUES_BASELINE.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        using (StreamWriter output = new StreamWriter(outputPath, false, new UTF8Encoding(false, true)))
        {
            output.WriteLine("profile=" + profilePath);
            output.WriteLine("p9=" + p9);
            output.WriteLine("p9-record-count=" + p9Records.Count.ToString(CultureInfo.InvariantCulture));
            foreach (string record in p9Records) output.WriteLine("p9-record=" + Escape(record));
            output.WriteLine("p12-content=" + p12);
            output.WriteLine("p12-catalog-byte-count=" + catalogBytes.Length.ToString(CultureInfo.InvariantCulture));
            output.WriteLine("p12-catalog-bytes=" + Convert.ToBase64String(catalogBytes));
            output.WriteLine("p12-catalog-record-count=" + CountCatalogRecords(catalogBytes).ToString(CultureInfo.InvariantCulture));
            output.WriteLine("provider=" + provider);
            output.WriteLine("calendar=" + calendarIdentity);
            output.WriteLine("calendar-values=" + Escape(DescribeProperties(calendar)));
            output.WriteLine("effective=" + Escape(DescribeProperties(resolution.Configuration)));
            foreach (string value in orderedValues) output.WriteLine("member=" + Escape(value));
        }

        Debug.Log("CORE_F_WP1_VALUES_BASELINE|file=" + outputPath
            + "|p9=" + p9 + "|p12Content=" + p12
            + "|cities=" + authoring.Cities.Count
            + "|npcs=" + authoring.Npcs.Count
            + "|actions=" + authoring.Actions.Count
            + "|statuses=" + authoring.Statuses.Count
            + "|jobs=" + authoring.Jobs.Count
            + "|calendar=" + calendar.monthsPerYear + "," + calendar.weeksPerMonth + "," + calendar.daysPerWeek
            + "|effective=" + resolution.Configuration.Population.RepresentationMode + ","
            + resolution.Configuration.Population.DecisionScope + ","
            + resolution.Configuration.Economy.Enabled + ","
            + resolution.Configuration.Travel.TravelCostPerDay.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
    }

    private static byte[] CaptureCatalogBytes(SimulationConfigData root, MethodInfo fingerprintMethod)
    {
        Type factory = fingerprintMethod.DeclaringType;
        MethodInfo keyMethod = factory.GetMethod("AssetKey", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo objectMethod = factory.GetMethod("SerializeObjectFields", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(keyMethod, Is.Not.Null);
        Assert.That(objectMethod, Is.Not.Null);
        SortedDictionary<string, byte[]> records = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        SortedDictionary<string, ScriptableObject> assets = new SortedDictionary<string, ScriptableObject>(StringComparer.Ordinal);
        Queue<ScriptableObject> pending = new Queue<ScriptableObject>();
        records.Add("profile-config/SimulationConfigData", (byte[])objectMethod.Invoke(null,
            new object[] { root, assets, pending }));
        int discovered = 0;
        while (pending.Count != 0)
        {
            ScriptableObject asset = pending.Dequeue();
            string key = (string)keyMethod.Invoke(null, new object[] { asset });
            if (records.ContainsKey(key)) continue;
            Assert.That(++discovered, Is.LessThanOrEqualTo(65536));
            Assert.That(assets.TryGetValue(key, out ScriptableObject registered) && ReferenceEquals(asset, registered), Is.True);
            records.Add(key, (byte[])objectMethod.Invoke(null, new object[] { asset, assets, pending }));
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
            }
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static string InvokePrivateFingerprint(string methodName, params object[] arguments)
    {
        MethodInfo method = typeof(P12ACompatibilityDescriptorFactory).GetMethod(
            methodName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, methodName);
        return (string)method.Invoke(null, arguments);
    }

    private static List<string> CaptureSelectedMemberValues(SimulationConfigData root)
    {
        string[] rows = File.ReadAllLines("docs/architecture/CORE_F_SELECTED_VALUE_MEMBERS.csv");
        Dictionary<string, List<object>> objects = new Dictionary<string, List<object>>(StringComparer.Ordinal);
        HashSet<object> visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Queue<object> pending = new Queue<object>();
        pending.Enqueue(root);
        while (pending.Count != 0)
        {
            object current = pending.Dequeue();
            if (current == null || !visited.Add(current)) continue;
            Type type = current.GetType();
            if (!objects.TryGetValue(type.Name, out List<object> values)) objects.Add(type.Name, values = new List<object>());
            values.Add(current);
            foreach (FieldInfo field in SerializableFields(type))
            {
                object value = field.GetValue(current);
                if (value is IList list)
                {
                    foreach (object item in list) if (item != null) pending.Enqueue(item);
                }
                else if (value != null) pending.Enqueue(value);
            }
        }

        List<string> result = new List<string>();
        Assert.That(rows.Length - 1, Is.EqualTo(164), "The approved source member map changed.");
        foreach (string row in rows.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] columns = row.Split(',');
            string typeName = columns[0];
            string memberName = columns[1];
            if (!objects.TryGetValue(typeName, out List<object> owners))
                owners = new List<object>();
            foreach (object owner in owners)
            {
                FieldInfo field = owner.GetType().GetField(memberName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, typeName + "." + memberName);
                result.Add(typeName + "." + memberName + "=" + Describe(field.GetValue(owner), 0));
            }
        }
        return result;
    }

    private static List<FieldInfo> SerializableFields(Type type)
    {
        List<FieldInfo> fields = new List<FieldInfo>();
        for (Type current = type; current != null && current != typeof(object)
            && current != typeof(UnityEngine.Object); current = current.BaseType)
        {
            foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.IsStatic || field.IsNotSerialized || field.IsInitOnly) continue;
                if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField), true)
                    || Attribute.IsDefined(field, typeof(SerializeReference), true)) fields.Add(field);
            }
        }
        fields.Sort((left, right) =>
        {
            int compare = StringComparer.Ordinal.Compare(left.DeclaringType?.FullName, right.DeclaringType?.FullName);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.Name, right.Name);
        });
        return fields;
    }

    private static string Describe(object value, int depth)
    {
        if (value == null) return "null";
        if (depth > 64) return "<depth-limit>";
        Type type = value.GetType();
        if (value is UnityEngine.Object asset)
        {
            PropertyInfo id = type.GetProperty("DefinitionId", BindingFlags.Public | BindingFlags.Instance);
            return "asset(" + (type.FullName ?? type.Name) + "," + (string)id?.GetValue(asset, null) + ")";
        }
        if (value is string text) return "string:" + Escape(text);
        if (type.IsEnum) return "enum(" + type.FullName + "," + Convert.ToInt64(value, CultureInfo.InvariantCulture)
            + "," + value + ")";
        if (value is float single) return "float32:0x" + BitConverter.ToInt32(BitConverter.GetBytes(single), 0).ToString("x8", CultureInfo.InvariantCulture);
        if (value is double dbl) return "float64:0x" + BitConverter.ToInt64(BitConverter.GetBytes(dbl), 0).ToString("x16", CultureInfo.InvariantCulture);
        if (value is IList list)
        {
            List<string> entries = new List<string>();
            foreach (object item in list) entries.Add(Describe(item, depth + 1));
            return "list<" + type.FullName + ">[" + string.Join(";", entries) + "]";
        }
        if (type.IsPrimitive || value is decimal || value is Guid)
            return type.FullName + ":" + Convert.ToString(value, CultureInfo.InvariantCulture);
        List<string> fields = new List<string>();
        foreach (FieldInfo field in SerializableFields(type))
            fields.Add(field.Name + "=" + Describe(field.GetValue(value), depth + 1));
        return "value<" + type.FullName + ">{" + string.Join(";", fields) + "}";
    }

    private static string DescribeProperties(object value)
    {
        if (value == null) return "null";
        List<string> fields = new List<string>();
        foreach (PropertyInfo property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(item => item.CanRead && item.GetIndexParameters().Length == 0)
            .OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            object child = property.GetValue(value, null);
            fields.Add(property.Name + "=" + (child == null || child.GetType().IsPrimitive || child.GetType().IsEnum
                || child is string || child is float || child is double
                ? Describe(child, 0) : DescribeProperties(child)));
        }
        return "properties<" + value.GetType().FullName + ">{" + string.Join(";", fields) + "}";
    }

    private static string Escape(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
    }

    private static string RetainedValue(string[] lines, string key)
    {
        string prefix = key + "=";
        foreach (string line in lines)
            if (line.StartsWith(prefix, StringComparison.Ordinal))
                return Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(prefix.Length)));
        throw new InvalidDataException("The retained value baseline lacks " + key + ".");
    }

    private static List<string> RetainedRecords(string[] lines, string prefix)
    {
        List<string> result = new List<string>();
        foreach (string line in lines)
            if (line.StartsWith(prefix, StringComparison.Ordinal))
                result.Add(Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(prefix.Length))));
        return result;
    }

    private static int CountCatalogRecords(byte[] bytes)
    {
        using (MemoryStream stream = new MemoryStream(bytes, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            int headerLength = reader.ReadInt32();
            reader.ReadBytes(headerLength);
            return reader.ReadInt32();
        }
    }

    private static string Sha256(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
        public new bool Equals(object left, object right) => ReferenceEquals(left, right);
        public int GetHashCode(object value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
}
