using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

public sealed class DailyV1ContentProjectionTests
{
    [Test]
    public void ReencodesCapturedUnityCatalogByteForByte()
    {
        Fixture fixture = ReadFixture();
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(fixture.Assets, fixture.Profile,
            out DailyV1SelectedContentProjection projection, out string diagnostic), Is.True, diagnostic);
        Assert.That(projection.Bytes, Is.EqualTo(fixture.Bytes));
        Assert.That(projection.Fingerprint, Is.EqualTo(fixture.Fingerprint));
    }

    [Test]
    public void CatalogAndFieldOrderAreCanonicalAndProjectionBytesAreCopied()
    {
        Fixture fixture = ReadFixture();
        DailyV1SelectedContentAsset reversedProfile = ReverseFields(fixture.Profile);
        DailyV1SelectedContentAsset[] reversedAssets = fixture.Assets.Reverse().Select(ReverseFields).ToArray();
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(reversedAssets, reversedProfile,
            out DailyV1SelectedContentProjection projection, out string diagnostic), Is.True, diagnostic);
        byte[] first = projection.Bytes;
        byte[] expected = (byte[])first.Clone();
        first[0] ^= 0xff;
        Assert.That(projection.Bytes, Is.EqualTo(expected));
        Assert.That(expected, Is.EqualTo(fixture.Bytes));
    }

    [Test]
    public void RejectsUnknownMemberAndMismatchedDeclaredType()
    {
        Fixture fixture = ReadFixture();
        List<DailyV1SelectedContentField> fields = new List<DailyV1SelectedContentField>(fixture.Profile.Fields);
        fields[0] = new DailyV1SelectedContentField("SimulationConfigData", "unapprovedExtension",
            DailyV1SelectedContentValue.Scalar("System.String", "x"));
        DailyV1SelectedContentAsset profile = new DailyV1SelectedContentAsset(
            DailyV1SelectedContentAsset.ProfileKey, "SimulationConfigData", fields);
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(fixture.Assets, profile,
            out DailyV1SelectedContentProjection projection, out _), Is.False);
        Assert.That(projection, Is.Null);

        fields = new List<DailyV1SelectedContentField>(fixture.Profile.Fields);
        DailyV1SelectedContentField first = fields[0];
        fields[0] = new DailyV1SelectedContentField(first.DeclaringTypeToken, first.Name,
            DailyV1SelectedContentValue.Scalar("System.Int32", 1));
        profile = new DailyV1SelectedContentAsset(DailyV1SelectedContentAsset.ProfileKey, "SimulationConfigData", fields);
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(fixture.Assets, profile,
            out projection, out _), Is.False);
    }

    [Test]
    public void PreservesNullVersusEmptyCollectionsAndRejectsUnreachableOrMissingReferences()
    {
        Fixture fixture = ReadFixture();
        string actionKey = fixture.Assets.First(value => value.TypeToken == "NpcActionData").Key;
        DailyV1SelectedContentAsset changedAction = ReplaceField(
            fixture.Assets.First(value => value.Key == actionKey), "statusToAdd",
            DailyV1SelectedContentValue.Null(TypeToken("List<NpcStatusData>")));
        DailyV1SelectedContentAsset[] nullAssets = Replace(fixture.Assets, changedAction);
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(nullAssets, fixture.Profile,
            out DailyV1SelectedContentProjection nullProjection, out string diagnostic), Is.True, diagnostic);

        changedAction = ReplaceField(fixture.Assets.First(value => value.Key == actionKey), "statusToAdd",
            DailyV1SelectedContentValue.List(TypeToken("List<NpcStatusData>"), Array.Empty<DailyV1SelectedContentValue>()));
        DailyV1SelectedContentAsset[] emptyAssets = Replace(fixture.Assets, changedAction);
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(emptyAssets, fixture.Profile,
            out DailyV1SelectedContentProjection emptyProjection, out diagnostic), Is.True, diagnostic);
        Assert.That(nullProjection.Bytes, Is.Not.EqualTo(emptyProjection.Bytes));

        List<DailyV1SelectedContentField> profileFields = new List<DailyV1SelectedContentField>(fixture.Profile.Fields);
        int statusField = profileFields.FindIndex(value => value.Name == "freeStatus");
        profileFields[statusField] = new DailyV1SelectedContentField("SimulationConfigData", "freeStatus",
            DailyV1SelectedContentValue.Reference("NpcStatusData", "NpcStatusData/status/missing"));
        DailyV1SelectedContentAsset missingReferenceProfile = new DailyV1SelectedContentAsset(
            DailyV1SelectedContentAsset.ProfileKey, "SimulationConfigData", profileFields);
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(fixture.Assets, missingReferenceProfile,
            out DailyV1SelectedContentProjection rejected, out _), Is.False);
        Assert.That(rejected, Is.Null);

        DailyV1SelectedContentAsset[] extraAsset = fixture.Assets.Concat(new[]
        {
            new DailyV1SelectedContentAsset("CityData/UNREACHABLE", "CityData", fixture.Assets.First(value => value.TypeToken == "CityData").Fields)
        }).ToArray();
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(extraAsset, fixture.Profile,
            out rejected, out _), Is.False);
    }

    [Test]
    public void AcceptsLegacyListBeyondAssetCountWhenEncodedCatalogFitsByteLimit()
    {
        Fixture fixture = ReadFixture();
        DailyV1SelectedContentAsset action = fixture.Assets.First(value => value.TypeToken == "NpcActionData");
        List<DailyV1SelectedContentValue> values = new List<DailyV1SelectedContentValue>(65537);
        for (int i = 0; i < 65537; i++) values.Add(DailyV1SelectedContentValue.Null("NpcStatusData"));
        DailyV1SelectedContentAsset oversized = ReplaceField(action, "statusToAdd",
            DailyV1SelectedContentValue.List(TypeToken("List<NpcStatusData>"), values));
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(Replace(fixture.Assets, oversized),
            fixture.Profile, out DailyV1SelectedContentProjection projection, out string diagnostic), Is.True, diagnostic);
        Assert.That(projection.Bytes, Is.EqualTo(ReplaceLegacyActionList(fixture.Bytes, action.Key, 65537)));
        Assert.That(ReadActionStatusAddCount(projection.Bytes), Is.EqualTo(65537));
        Assert.That(projection.Bytes.Length, Is.LessThan(1024 * 1024));
    }

    [Test]
    public void PreflightsOversizedStringBeforeCopyingEncoderGraph()
    {
        Fixture fixture = ReadFixture();
        DailyV1SelectedContentAsset profile = ReplaceField(fixture.Profile, "simulationName",
            DailyV1SelectedContentValue.Scalar("System.String", new string('x', ContinuationWire.MaxStringBytes + 1)));
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(fixture.Assets, profile,
            out DailyV1SelectedContentProjection projection, out string diagnostic), Is.False);
        Assert.That(projection, Is.Null);
        Assert.That(diagnostic, Does.Contain("string exceeds its byte bound"));
    }

    [Test]
    public void PreflightsOversizedNestedListAgainstCatalogByteBudget()
    {
        Fixture fixture = ReadFixture();
        const int itemCount = 3200000;
        List<DailyV1SelectedContentValue> items = new List<DailyV1SelectedContentValue>(itemCount);
        DailyV1SelectedContentValue one = DailyV1SelectedContentValue.Scalar("System.Int32", 1);
        for (int i = 0; i < itemCount; i++) items.Add(one);
        DailyV1SelectedContentValue calendar = DailyV1SelectedContentValue.Object("CalendarDefinition", new[]
        {
            new DailyV1SelectedContentField("CalendarDefinition", "monthsPerYear", DailyV1SelectedContentValue.Scalar("System.Int32", 12)),
            new DailyV1SelectedContentField("CalendarDefinition", "weeksPerMonth", DailyV1SelectedContentValue.Scalar("System.Int32", 4)),
            new DailyV1SelectedContentField("CalendarDefinition", "daysPerWeek", DailyV1SelectedContentValue.Scalar("System.Int32", 7)),
            new DailyV1SelectedContentField("CalendarDefinition", "monthLengths",
                DailyV1SelectedContentValue.List(TypeToken("List<int>"), items))
        });
        DailyV1SelectedContentAsset profile = ReplaceField(fixture.Profile, "calendar", calendar);
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(fixture.Assets, profile,
            out DailyV1SelectedContentProjection projection, out string diagnostic), Is.False);
        Assert.That(projection, Is.Null);
        Assert.That(diagnostic, Does.Contain("catalog exceeds its byte bound"));
    }

    private static int ReadActionStatusAddCount(byte[] bytes)
    {
        Dictionary<string, Dictionary<string, string>> schema = ReadSchema();
        using (MemoryStream stream = new MemoryStream(bytes, false))
        using (BinaryReader reader = new BinaryReader(stream, new UTF8Encoding(false, true), true))
        {
            Assert.That(ReadString(reader), Is.EqualTo("p12a.official-content-catalog/v1"));
            int recordCount = reader.ReadInt32();
            for (int i = 0; i < recordCount; i++)
            {
                string key = ReadString(reader);
                int length = reader.ReadInt32();
                byte[] recordBytes = reader.ReadBytes(length);
                using (MemoryStream recordStream = new MemoryStream(recordBytes, false))
                using (BinaryReader record = new BinaryReader(recordStream, new UTF8Encoding(false, true), true))
                {
                    string type = ReadString(record);
                    int fieldCount = record.ReadInt32();
                    if (!schema.TryGetValue(type, out Dictionary<string, string> fieldSchema))
                        throw new InvalidDataException("Unknown serialized type " + type);
                    for (int fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
                    {
                        string declaringType = ReadString(record);
                        string name = ReadString(record);
                        if (type == "NpcActionData" && name == "statusToAdd")
                        {
                            Assert.That(declaringType, Is.EqualTo(type));
                            Assert.That(ReadString(record), Is.EqualTo(TypeToken("List<NpcStatusData>")));
                            Assert.That(record.ReadByte(), Is.EqualTo(20));
                            int count = record.ReadInt32();
                            for (int item = 0; item < count; item++) Assert.That(record.ReadByte(), Is.Zero);
                            return count;
                        }
                        Assert.That(fieldSchema.TryGetValue(name, out string sourceType), Is.True);
                        ReadValue(record, sourceType, schema);
                    }
                }
            }
        }
        Assert.Fail("NpcActionData was absent from the encoded catalog.");
        return -1;
    }

    // Build the expected legacy P12 bytes by replacing only the one captured field in the
    // approved original catalog and writing its documented little-endian tagged list shape.
    private static byte[] ReplaceLegacyActionList(byte[] original, string targetKey, int count)
    {
        Dictionary<string, Dictionary<string, string>> schema = ReadSchema();
        List<KeyValuePair<string, byte[]>> records = new List<KeyValuePair<string, byte[]>>();
        using (MemoryStream stream = new MemoryStream(original, false))
        using (BinaryReader reader = new BinaryReader(stream, new UTF8Encoding(false, true), true))
        {
            string identity = ReadString(reader);
            int recordCount = reader.ReadInt32();
            for (int i = 0; i < recordCount; i++)
            {
                string key = ReadString(reader);
                int length = reader.ReadInt32();
                byte[] payload = reader.ReadBytes(length);
                Assert.That(payload, Has.Length.EqualTo(length));
                using (MemoryStream payloadStream = new MemoryStream(payload, false))
                using (BinaryReader record = new BinaryReader(payloadStream, new UTF8Encoding(false, true), true))
                {
                    string type = ReadString(record);
                    int fieldCount = record.ReadInt32();
                    if (key == targetKey && type == "NpcActionData")
                    {
                        byte[] replacement = null;
                        for (int field = 0; field < fieldCount; field++)
                        {
                            ReadString(record);
                            string name = ReadString(record);
                            long start = payloadStream.Position;
                            string sourceType = schema[type][name];
                            ReadValue(record, sourceType, schema);
                            if (name == "statusToAdd")
                            {
                                long end = payloadStream.Position;
                                using (MemoryStream valueStream = new MemoryStream())
                                using (BinaryWriter valueWriter = new BinaryWriter(valueStream, new UTF8Encoding(false, true), true))
                                {
                                    WriteLegacyString(valueWriter, TypeToken("List<NpcStatusData>"));
                                    valueWriter.Write((byte)20);
                                    valueWriter.Write(count);
                                    for (int item = 0; item < count; item++) valueWriter.Write((byte)0);
                                    valueWriter.Flush();
                                    replacement = new byte[(int)(payload.Length - (end - start)) + (int)valueStream.Length];
                                    Buffer.BlockCopy(payload, 0, replacement, 0, (int)start);
                                    Buffer.BlockCopy(valueStream.GetBuffer(), 0, replacement, (int)start, (int)valueStream.Length);
                                    Buffer.BlockCopy(payload, (int)end, replacement, (int)start + (int)valueStream.Length,
                                        payload.Length - (int)end);
                                }
                            }
                        }
                        Assert.That(replacement, Is.Not.Null);
                        payload = replacement;
                    }
                }
                records.Add(new KeyValuePair<string, byte[]>(key, payload));
            }
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            using (MemoryStream expected = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(expected, new UTF8Encoding(false, true), true))
            {
                WriteLegacyString(writer, identity);
                writer.Write(recordCount);
                foreach (KeyValuePair<string, byte[]> record in records)
                {
                    WriteLegacyString(writer, record.Key);
                    writer.Write(record.Value.Length);
                    writer.Write(record.Value);
                }
                writer.Flush();
                return expected.ToArray();
            }
        }
    }

    private static void WriteLegacyString(BinaryWriter writer, string value)
    {
        byte[] bytes = new UTF8Encoding(false, true).GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static DailyV1SelectedContentAsset ReverseFields(DailyV1SelectedContentAsset value) =>
        new DailyV1SelectedContentAsset(value.Key, value.TypeToken, value.Fields.Reverse());

    private static DailyV1SelectedContentAsset ReplaceField(DailyV1SelectedContentAsset asset,
        string fieldName, DailyV1SelectedContentValue replacement)
    {
        List<DailyV1SelectedContentField> fields = new List<DailyV1SelectedContentField>(asset.Fields);
        int index = fields.FindIndex(value => value.Name == fieldName);
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        DailyV1SelectedContentField existing = fields[index];
        fields[index] = new DailyV1SelectedContentField(existing.DeclaringTypeToken, existing.Name, replacement);
        return new DailyV1SelectedContentAsset(asset.Key, asset.TypeToken, fields);
    }

    private static DailyV1SelectedContentAsset[] Replace(DailyV1SelectedContentAsset[] assets,
        DailyV1SelectedContentAsset replacement)
    {
        DailyV1SelectedContentAsset[] result = (DailyV1SelectedContentAsset[])assets.Clone();
        int index = Array.FindIndex(result, value => value.Key == replacement.Key);
        result[index] = replacement;
        return result;
    }

    private static Fixture ReadFixture()
    {
        string[] lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CORE_F_WP1_VALUES_BASELINE.txt"), new UTF8Encoding(false, true));
        string fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CORE_F_WP1_VALUES_BASELINE.txt");
        Assert.That(new FileInfo(fixturePath).Length, Is.EqualTo(212100));
        using (SHA256 sha = SHA256.Create())
            Assert.That(ToHex(sha.ComputeHash(File.ReadAllBytes(fixturePath))),
                Is.EqualTo("422805cbe373cf1410ee6a928d363fa1ef413dbcf50dc062dce7867ed4fdd1b3"));
        Assert.That(lines.Count(value => value.StartsWith("member=", StringComparison.Ordinal)), Is.EqualTo(718));
        Assert.That(Find(lines, "p12-catalog-record-count"), Is.EqualTo("39"));
        string encoded = Find(lines, "p12-catalog-bytes");
        byte[] bytes = Convert.FromBase64String(encoded);
        string fingerprint = Find(lines, "p12-content");
        DailyV1CapturedSelectedContentFixture.Create(out DailyV1SelectedContentAsset capturedProfile,
            out DailyV1SelectedContentAsset[] capturedAssets);
        return new Fixture(bytes, fingerprint, capturedProfile, capturedAssets);
    }

    private static Dictionary<string, Dictionary<string, string>> ReadSchema()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CORE_F_SELECTED_VALUE_MEMBERS.csv");
        Dictionary<string, Dictionary<string, string>> result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        string[] lines = File.ReadAllLines(path, new UTF8Encoding(false, true));
        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(',');
            if (!result.TryGetValue(parts[0], out Dictionary<string, string> fields))
                result.Add(parts[0], fields = new Dictionary<string, string>(StringComparer.Ordinal));
            fields.Add(parts[1], parts[2]);
        }
        Assert.That(lines.Length - 1, Is.EqualTo(164));
        return result;
    }

    private static string TypeToken(string sourceType)
    {
        switch (sourceType)
        {
            case "bool": return "System.Boolean";
            case "int": return "System.Int32";
            case "long": return "System.Int64";
            case "float": return "System.Single";
            case "double": return "System.Double";
            case "string": return "System.String";
        }
        if (sourceType.StartsWith("List<", StringComparison.Ordinal))
        {
            string element = sourceType.Substring(5, sourceType.Length - 6);
            string elementToken = TypeToken(element);
            string assembly = element == "int"
                ? "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
                : "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            return "System.Collections.Generic.List`1[[" + elementToken + ", " + assembly + "]]";
        }
        return sourceType;
    }

    private static string Find(IEnumerable<string> lines, string name)
    {
        string prefix = name + "=";
        foreach (string line in lines)
            if (line.StartsWith(prefix, StringComparison.Ordinal)) return line.Substring(prefix.Length);
        throw new InvalidDataException("Missing fixture field " + name + ".");
    }

    private static string ToHex(byte[] bytes)
    {
        StringBuilder result = new StringBuilder(bytes.Length * 2);
        foreach (byte value in bytes) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return result.ToString();
    }

    private sealed class Fixture
    {
        public byte[] Bytes { get; }
        public string Fingerprint { get; }
        public DailyV1SelectedContentAsset Profile { get; }
        public DailyV1SelectedContentAsset[] Assets { get; }
        public Fixture(byte[] bytes, string fingerprint, DailyV1SelectedContentAsset profile, DailyV1SelectedContentAsset[] assets)
        { Bytes = bytes; Fingerprint = fingerprint; Profile = profile; Assets = assets; }
    }


}
