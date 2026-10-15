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
    public void RejectsSelectedCollectionOverItsItemBound()
    {
        Fixture fixture = ReadFixture();
        DailyV1SelectedContentAsset action = fixture.Assets.First(value => value.TypeToken == "NpcActionData");
        List<DailyV1SelectedContentValue> values = new List<DailyV1SelectedContentValue>(65537);
        for (int i = 0; i < 65537; i++) values.Add(DailyV1SelectedContentValue.Null("NpcStatusData"));
        DailyV1SelectedContentAsset oversized = ReplaceField(action, "statusToAdd",
            DailyV1SelectedContentValue.List(TypeToken("List<NpcStatusData>"), values));
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(Replace(fixture.Assets, oversized),
            fixture.Profile, out DailyV1SelectedContentProjection rejected, out _), Is.False);
        Assert.That(rejected, Is.Null);
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
        Dictionary<string, Dictionary<string, string>> schema = ReadSchema();
        using (MemoryStream stream = new MemoryStream(bytes, false))
        using (BinaryReader reader = new BinaryReader(stream, new UTF8Encoding(false, true), true))
        {
            Assert.That(ReadString(reader), Is.EqualTo("p12a.official-content-catalog/v1"));
            int count = reader.ReadInt32();
            List<DailyV1SelectedContentAsset> assets = new List<DailyV1SelectedContentAsset>();
            DailyV1SelectedContentAsset profile = null;
            for (int i = 0; i < count; i++)
            {
                string key = ReadString(reader);
                int length = reader.ReadInt32();
                byte[] recordBytes = reader.ReadBytes(length);
                Assert.That(recordBytes, Has.Length.EqualTo(length));
                using (MemoryStream recordStream = new MemoryStream(recordBytes, false))
                using (BinaryReader record = new BinaryReader(recordStream, new UTF8Encoding(false, true), true))
                {
                    string type = ReadString(record);
                    List<DailyV1SelectedContentField> fields = ReadFields(record, type, schema);
                    Assert.That(recordStream.Position, Is.EqualTo(recordStream.Length));
                    DailyV1SelectedContentAsset asset = new DailyV1SelectedContentAsset(key, type, fields);
                    if (key == DailyV1SelectedContentAsset.ProfileKey) profile = asset;
                    else assets.Add(asset);
                }
            }
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            Assert.That(profile, Is.Not.Null);
            return new Fixture(bytes, fingerprint, profile, assets.ToArray());
        }
    }

    private static List<DailyV1SelectedContentField> ReadFields(BinaryReader reader, string objectType,
        Dictionary<string, Dictionary<string, string>> schema)
    {
        int count = reader.ReadInt32();
        Assert.That(schema.TryGetValue(objectType, out Dictionary<string, string> members), Is.True, objectType);
        Assert.That(count, Is.EqualTo(members.Count), objectType);
        List<DailyV1SelectedContentField> fields = new List<DailyV1SelectedContentField>(count);
        for (int i = 0; i < count; i++)
        {
            string declaringType = ReadString(reader);
            string name = ReadString(reader);
            Assert.That(members.TryGetValue(name, out string sourceType), Is.True, objectType + "." + name);
            fields.Add(new DailyV1SelectedContentField(declaringType, name, ReadValue(reader, sourceType, schema)));
        }
        return fields;
    }

    private static DailyV1SelectedContentValue ReadValue(BinaryReader reader, string sourceType,
        Dictionary<string, Dictionary<string, string>> schema)
    {
        string expected = TypeToken(sourceType);
        long initialPosition = reader.BaseStream.Position;
        int leadingByte = reader.ReadByte();
        reader.BaseStream.Position = initialPosition;
        if (leadingByte == 0)
        {
            reader.ReadByte();
            return DailyV1SelectedContentValue.Null(expected);
        }
        string token = ReadString(reader);
        byte tag = reader.ReadByte();
        switch (tag)
        {
            case 1: return DailyV1SelectedContentValue.Reference(token, ReadString(reader));
            case 2: return DailyV1SelectedContentValue.Scalar(token, ReadString(reader));
            case 3: return DailyV1SelectedContentValue.Enum(token, ReadString(reader));
            case 4: return DailyV1SelectedContentValue.Scalar(token, reader.ReadBoolean());
            case 5: return DailyV1SelectedContentValue.Scalar(token, reader.ReadByte());
            case 6: return DailyV1SelectedContentValue.Scalar(token, reader.ReadSByte());
            case 7: return DailyV1SelectedContentValue.Scalar(token, reader.ReadInt16());
            case 8: return DailyV1SelectedContentValue.Scalar(token, reader.ReadUInt16());
            case 9: return DailyV1SelectedContentValue.Scalar(token, reader.ReadInt32());
            case 10: return DailyV1SelectedContentValue.Scalar(token, reader.ReadUInt32());
            case 11: return DailyV1SelectedContentValue.Scalar(token, reader.ReadInt64());
            case 12: return DailyV1SelectedContentValue.Scalar(token, reader.ReadUInt64());
            case 13: return DailyV1SelectedContentValue.Scalar(token, (char)reader.ReadUInt16());
            case 14: return DailyV1SelectedContentValue.Scalar(token, BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadInt32()), 0));
            case 15: return DailyV1SelectedContentValue.Scalar(token, BitConverter.ToDouble(BitConverter.GetBytes(reader.ReadInt64()), 0));
            case 16: return DailyV1SelectedContentValue.Scalar(token, new decimal(new[] { reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32() }));
            case 17: return DailyV1SelectedContentValue.Scalar(token, Guid.ParseExact(ReadString(reader), "N"));
            case 18: return DailyV1SelectedContentValue.Scalar(token, DateTime.FromBinary(reader.ReadInt64()));
            case 19: return DailyV1SelectedContentValue.Scalar(token, TimeSpan.FromTicks(reader.ReadInt64()));
            case 20:
            {
                int itemCount = reader.ReadInt32();
                string elementType = sourceType.Substring(5, sourceType.Length - 6);
                List<DailyV1SelectedContentValue> values = new List<DailyV1SelectedContentValue>(itemCount);
                for (int i = 0; i < itemCount; i++) values.Add(ReadValue(reader, elementType, schema));
                return DailyV1SelectedContentValue.List(token, values);
            }
            case 21:
                return DailyV1SelectedContentValue.Object(token, ReadFields(reader, sourceType, schema));
            default: throw new InvalidDataException("Unknown content tag " + tag.ToString(CultureInfo.InvariantCulture));
        }
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

    private static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length) throw new EndOfStreamException();
        return new UTF8Encoding(false, true).GetString(bytes);
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
