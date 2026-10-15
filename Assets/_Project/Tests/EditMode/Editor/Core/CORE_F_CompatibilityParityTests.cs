using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;

public sealed class CORE_F_CompatibilityParityTests
{
    private const string ValidationDirectory = "docs/validation/CORE_F_SharedRuntimeSource";

    [Test]
    public void NumericHelpersMatchEveryCapturedUnityVector()
    {
        string path = FindRepositoryFile(ValidationDirectory + "/NUMERIC_ORACLE_VECTORS.csv");
        List<string[]> records = ReadCsv(path);
        int checkedCount = 0;
        for (int i = 1; i < records.Count; i++)
        {
            string operation = records[i][0];
            string[] arguments = records[i][1].Split('|');
            switch (operation)
            {
                case "MAXFLOAT":
                    AssertBits(ParseBits(arguments[3]), UnityMathfCompatibility.Max(ParseBits(arguments[1]), ParseBits(arguments[2])), operation + " " + arguments[0]);
                    break;
                case "MINFLOAT":
                    AssertBits(ParseBits(arguments[3]), UnityMathfCompatibility.Min(ParseBits(arguments[1]), ParseBits(arguments[2])), operation + " " + arguments[0]);
                    break;
                case "CLAMPFLOAT":
                    AssertBits(ParseBits(arguments[4]), UnityMathfCompatibility.Clamp(ParseBits(arguments[1]), ParseBits(arguments[2]), ParseBits(arguments[3])), operation + " " + arguments[0]);
                    break;
                case "CLAMP01":
                    AssertBits(ParseBits(arguments[2]), UnityMathfCompatibility.Clamp01(ParseBits(arguments[1])), operation + " " + arguments[0]);
                    break;
                case "ROUND":
                    Assert.That(UnityMathfCompatibility.RoundToInt(ParseBits(arguments[0])), Is.EqualTo(ParseInt(arguments[1])), operation + " " + arguments[0]);
                    break;
                case "FLOOR":
                    Assert.That(UnityMathfCompatibility.FloorToInt(ParseBits(arguments[0])), Is.EqualTo(ParseInt(arguments[1])), operation + " " + arguments[0]);
                    break;
                case "APPROX":
                    Assert.That(UnityMathfCompatibility.Approximately(ParseBits(arguments[1]), ParseBits(arguments[2])),
                        Is.EqualTo(bool.Parse(arguments[3])), operation + " " + arguments[0]);
                    break;
                case "MAXINT":
                    Assert.That(UnityMathfCompatibility.Max(ParseInt(arguments[0]), ParseInt(arguments[1])), Is.EqualTo(ParseInt(arguments[2])), operation);
                    break;
                case "MININT":
                    Assert.That(UnityMathfCompatibility.Min(ParseInt(arguments[0]), ParseInt(arguments[1])), Is.EqualTo(ParseInt(arguments[2])), operation);
                    break;
                case "MAXINT3":
                    Assert.That(UnityMathfCompatibility.Max(ParseInt(arguments[0]), ParseInt(arguments[1]), ParseInt(arguments[2])), Is.EqualTo(ParseInt(arguments[3])), operation);
                    break;
                case "MININT3":
                    Assert.That(UnityMathfCompatibility.Min(ParseInt(arguments[0]), ParseInt(arguments[1]), ParseInt(arguments[2])), Is.EqualTo(ParseInt(arguments[3])), operation);
                    break;
                case "CLAMPINT":
                    Assert.That(UnityMathfCompatibility.Clamp(ParseInt(arguments[0]), ParseInt(arguments[1]), ParseInt(arguments[2])), Is.EqualTo(ParseInt(arguments[3])), operation);
                    break;
                case "POW":
                    AssertBits(ParseBits(arguments[2]), UnityMathfCompatibility.Pow(ParseBits(arguments[0]), ParseBits(arguments[1])), operation);
                    break;
                case "POW_ESCAPE_INT":
                    AssertBits(ParseBits(arguments[2]), UnityMathfCompatibility.Pow(0.8f, (float)ParseInt(arguments[0])), operation + " " + arguments[0]);
                    break;
                case "POW_ESCAPECONTEXT":
                    AssertBits(ParseBits(arguments[2]), UnityMathfCompatibility.Pow(
                        float.Parse(arguments[0], CultureInfo.InvariantCulture), ParseBits(arguments[1])), operation);
                    break;
                default:
                    Assert.Fail("Unexpected oracle operation " + operation);
                    break;
            }
            checkedCount++;
        }
        Assert.That(checkedCount, Is.EqualTo(172), "The full reviewed Unity numeric vector set must be exercised.");
    }

    [Test]
    public void CodecMatchesCapturedBytesAndLegacyReaderOutcomes()
    {
        string root = FindRepositoryRoot();
        Dictionary<string, string> payloads = ReadPayloadOracle(Path.Combine(root, ValidationDirectory, "JSON_PAYLOAD_ORACLE.csv"));
        var roster = new[]
        {
            new P18DCommercialSharingCodec.RosterMember("npc-b", string.Empty),
            new P18DCommercialSharingCodec.RosterMember("npc-a", string.Empty)
        };
        var participants = new[]
        {
            new P18DCommercialSharingCodec.Participant("npc-a", string.Empty, "city-oracle", "location-oracle"),
            new P18DCommercialSharingCodec.Participant("npc-b", string.Empty, "city-oracle", "location-oracle")
        };
        var edges = new[]
        {
            new P18DCommercialSharingCodec.Edge("npc-a", "npc-b"),
            new P18DCommercialSharingCodec.Edge("npc-b", "npc-a")
        };
        Assert.That(P18DCommercialSharingCodec.EncodePlan(roster, participants, edges), Is.EqualTo(payloads["commercial-sharing.snapshot;commercial-share-snapshot"]));
        Assert.That(P18DCommercialSharingCodec.EncodeEdge("npc-a", "npc-b"), Is.EqualTo(payloads["commercial-sharing.edge;commercial-share-edge:NzplZGdlL3YxNTpucGMtYTU6bnBjLWI="]));

        var specialRoster = new[] { new P18DCommercialSharingCodec.RosterMember("npc-\"α\\id", "person-Ω") };
        var specialParticipants = new[]
        {
            new P18DCommercialSharingCodec.Participant("npc-\"α\\id", "person-Ω", "city-雪", "loc-\nline")
        };
        var specialEdges = new[] { new P18DCommercialSharingCodec.Edge("npc-\"α\\id", "npc-β") };
        Assert.That(P18DCommercialSharingCodec.EncodePlan(specialRoster, specialParticipants, specialEdges), Is.EqualTo(payloads["special-plan"]));
        Assert.That(P18DCommercialSharingCodec.EncodeEdge("npc-\"α\\id", "npc-β"), Is.EqualTo(payloads["special-edge"]));
        Assert.That(P18DCommercialSharingCodec.EncodeEdge(null, string.Empty), Is.EqualTo("{\"senderRuntimeId\":\"\",\"receiverRuntimeId\":\"\"}"));

        AssertCapturedReaderCases(root);
        AssertCapturedStringRoundTrips(root);
    }

    private static void AssertCapturedReaderCases(string root)
    {
        List<string[]> records = ReadCsv(Path.Combine(root, ValidationDirectory, "UNITY_ORACLE_CAPTURE_RESULTS_d8b4c40e.csv"));
        int checkedCount = 0;
        for (int i = 1; i < records.Count; i++)
        {
            string operation = records[i][ColumnIndex(records[0], "operation")];
            bool planCase = operation == "PLAN_READ" || operation.StartsWith("PLAN_", StringComparison.Ordinal);
            bool edgeCase = operation == "EDGE_READ" || operation.StartsWith("EDGE_", StringComparison.Ordinal);
            if (!planCase && !edgeCase) continue;
            string capture = records[i][ColumnIndex(records[0], "payload")];
            int separator = capture.IndexOf('|');
            Assert.That(separator, Is.GreaterThanOrEqualTo(0), operation + " capture delimiter");
            string json = Encoding.UTF8.GetString(Convert.FromBase64String(capture.Substring(0, separator)));
            string expected = capture.Substring(separator + 1);
            string actual;
            if (planCase)
            {
                P18DCommercialSharingCodec.Plan plan;
                bool success = P18DCommercialSharingCodec.TryDecodePlan(json, out plan);
                actual = success ? "true:" + DescribePlan(plan) : "false:" + (plan == null ? "null" : DescribePlan(plan));
            }
            else
            {
                P18DCommercialSharingCodec.Edge edge;
                bool success = P18DCommercialSharingCodec.TryDecodeEdge(json, out edge);
                actual = success ? "true:" + DescribeEdgeRead(edge) : "false:" + (edge == null ? "null" : DescribeEdgeRead(edge));
            }
            Assert.That(actual, Is.EqualTo(expected), operation + " sequence " + records[i][ColumnIndex(records[0], "sequence")]);
            checkedCount++;
        }
        Assert.That(checkedCount, Is.EqualTo(31), "All captured plan/edge reader outcomes must be checked.");
    }

    private static void AssertCapturedStringRoundTrips(string root)
    {
        List<string[]> records = ReadCsv(Path.Combine(root, ValidationDirectory, "UNITY_ORACLE_CAPTURE_RESULTS_d8b4c40e.csv"));
        int checkedCount = 0;
        for (int i = 1; i < records.Count; i++)
        {
            if (records[i][ColumnIndex(records[0], "operation")] != "JSON_STRING_ROUNDTRIP") continue;
            string[] payload = records[i][ColumnIndex(records[0], "payload")].Split('|');
            string label = payload[0];
            string jsonHex = ValueAfter(payload[1], "jsonUtf16=");
            string expectedDecodedHex = ValueAfter(payload[2], "decodedUtf16=");
            string value = CapturedString(label);
            string actualJson = P18DCommercialSharingCodec.EncodeEdge(value, "receiver");
            Assert.That(ToUtf16Hex(actualJson), Is.EqualTo(jsonHex), label + " JSON UTF-16");

            P18DCommercialSharingCodec.Edge decoded;
            Assert.That(P18DCommercialSharingCodec.TryDecodeEdge(actualJson, out decoded), Is.True, label + " decode");
            Assert.That(decoded.SenderRuntimeId == null ? "~" : ToUtf16Hex(decoded.SenderRuntimeId), Is.EqualTo(expectedDecodedHex), label + " decoded UTF-16");
            checkedCount++;
        }
        Assert.That(checkedCount, Is.EqualTo(38), "All captured string escaping and round-trip cases must be checked.");
    }

    private static string CapturedString(string label)
    {
        if (label.StartsWith("control-U+", StringComparison.Ordinal))
            return new string((char)int.Parse(label.Substring("control-U+".Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture), 1);
        if (label == "all-control-characters")
        {
            var controls = new char[32];
            for (int i = 0; i < controls.Length; i++) controls[i] = (char)i;
            return new string(controls);
        }
        if (label == "null-string") return null;
        if (label == "empty-string") return string.Empty;
        if (label == "supplementary-surrogate-pair") return char.ConvertFromUtf32(0x1F642);
        if (label == "isolated-high-surrogate") return new string(new[] { '\uD800' });
        if (label == "isolated-low-surrogate") return new string(new[] { '\uDC00' });
        Assert.Fail("Unexpected captured string case " + label);
        return null;
    }

    private static string DescribePlan(P18DCommercialSharingCodec.Plan plan) =>
        "roster=" + DescribeRows(plan.RuntimeRoster, row => row == null ? "null" : "runtimeId=" + Base64(row.RuntimeId) + ",personId=" + Base64(row.PersonId))
        + ";participants=" + DescribeRows(plan.Participants, row => row == null ? "null" : "runtimeId=" + Base64(row.RuntimeId)
            + ",personId=" + Base64(row.PersonId) + ",cityRuntimeId=" + Base64(row.CityRuntimeId) + ",locationRuntimeId=" + Base64(row.LocationRuntimeId))
        + ";edges=" + DescribeRows(plan.Edges, row => row == null ? "null"
            : "senderRuntimeId=" + Base64(row.SenderRuntimeId) + ",receiverRuntimeId=" + Base64(row.ReceiverRuntimeId));

    private static string DescribeEdgeRead(P18DCommercialSharingCodec.Edge edge) => edge == null
        ? "null" : "sender=" + (edge.SenderRuntimeId == null ? "~" : ToUtf16Hex(edge.SenderRuntimeId))
            + ";receiver=" + (edge.ReceiverRuntimeId == null ? "~" : ToUtf16Hex(edge.ReceiverRuntimeId));

    private static string DescribeRows<T>(IReadOnlyList<T> rows, Func<T, string> describe)
    {
        if (rows == null) return "null";
        var values = new string[rows.Count];
        for (int i = 0; i < rows.Count; i++) values[i] = describe(rows[i]);
        return "len=" + rows.Count.ToString(CultureInfo.InvariantCulture) + "[" + string.Join(";", values) + "]";
    }

    private static Dictionary<string, string> ReadPayloadOracle(string path)
    {
        List<string[]> records = ReadCsv(path);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 1; i < records.Count; i++)
            result[records[i][0]] = Encoding.UTF8.GetString(Convert.FromBase64String(records[i][3]));
        return result;
    }

    private static List<string[]> ReadCsv(string path)
    {
        string text = File.ReadAllText(path, Encoding.UTF8);
        var rows = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < text.Length; i++)
        {
            char current = text[i];
            if (quoted)
            {
                if (current == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else if (current == '"') quoted = false;
                else field.Append(current);
                continue;
            }
            if (current == '"') { quoted = true; continue; }
            if (current == ',') { fields.Add(field.ToString()); field.Clear(); continue; }
            if (current == '\n')
            {
                fields.Add(field.ToString().TrimEnd('\r'));
                field.Clear();
                rows.Add(fields.ToArray());
                fields.Clear();
                continue;
            }
            field.Append(current);
        }
        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString().TrimEnd('\r'));
            rows.Add(fields.ToArray());
        }
        if (rows.Count > 0 && rows[0].Length > 0) rows[0][0] = rows[0][0].TrimStart('\uFEFF');
        return rows;
    }

    private static int ColumnIndex(string[] header, string name)
    {
        for (int i = 0; i < header.Length; i++) if (header[i] == name) return i;
        Assert.Fail("Missing CSV column " + name);
        return -1;
    }

    private static string FindRepositoryFile(string relativePath) => Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string FindRepositoryRoot()
    {
        string[] starts = { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory };
        foreach (string start in starts)
        {
            DirectoryInfo directory = new DirectoryInfo(start);
            while (directory != null)
            {
                string marker = Path.Combine(directory.FullName, ValidationDirectory.Replace('/', Path.DirectorySeparatorChar), "NUMERIC_ORACLE_VECTORS.csv");
                if (File.Exists(marker)) return directory.FullName;
                directory = directory.Parent;
            }
        }
        throw new FileNotFoundException("Could not locate the checked-in CORE-F oracle fixtures from the test working directory.");
    }

    private static string ValueAfter(string value, string prefix) => value.StartsWith(prefix, StringComparison.Ordinal)
        ? value.Substring(prefix.Length) : throw new FormatException("Expected " + prefix + " in captured value.");

    private static string ToUtf16Hex(string value)
    {
        var result = new StringBuilder(value.Length * 4);
        for (int i = 0; i < value.Length; i++) result.Append(((int)value[i]).ToString("X4", CultureInfo.InvariantCulture));
        return result.ToString();
    }

    private static string Base64(string value) => value == null ? "~" : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static float ParseBits(string value)
    {
        int start = value.LastIndexOf("0x", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), "Expected a captured binary32 bit pattern: " + value);
        int end = value.IndexOf(']', start);
        uint bits = uint.Parse(value.Substring(start + 2, end - start - 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
    }

    private static void AssertBits(float expected, float actual, string context)
    {
        uint expectedBits = BitConverter.ToUInt32(BitConverter.GetBytes(expected), 0);
        uint actualBits = BitConverter.ToUInt32(BitConverter.GetBytes(actual), 0);
        Assert.That(actualBits, Is.EqualTo(expectedBits), context + " expected 0x" + expectedBits.ToString("X8") + " got 0x" + actualBits.ToString("X8"));
    }

    private static int ParseInt(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
}
