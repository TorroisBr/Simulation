using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// The closed, host-independent wire codec for the frozen P18-D commercial-sharing payloads.
/// It intentionally supports only the two established shapes; it is not a general JSON API.
/// </summary>
public static class P18DCommercialSharingCodec
{
    public sealed class RosterMember
    {
        public string RuntimeId { get; }
        public string PersonId { get; }

        public RosterMember(string runtimeId, string personId)
        {
            RuntimeId = runtimeId;
            PersonId = personId;
        }
    }

    public sealed class Participant
    {
        public string RuntimeId { get; }
        public string PersonId { get; }
        public string CityRuntimeId { get; }
        public string LocationRuntimeId { get; }

        public Participant(string runtimeId, string personId, string cityRuntimeId, string locationRuntimeId)
        {
            RuntimeId = runtimeId;
            PersonId = personId;
            CityRuntimeId = cityRuntimeId;
            LocationRuntimeId = locationRuntimeId;
        }
    }

    public sealed class Edge
    {
        public string SenderRuntimeId { get; }
        public string ReceiverRuntimeId { get; }

        public Edge(string senderRuntimeId, string receiverRuntimeId)
        {
            SenderRuntimeId = senderRuntimeId;
            ReceiverRuntimeId = receiverRuntimeId;
        }
    }

    public sealed class Plan
    {
        public IReadOnlyList<RosterMember> RuntimeRoster { get; }
        public IReadOnlyList<Participant> Participants { get; }
        public IReadOnlyList<Edge> Edges { get; }

        public Plan(IReadOnlyList<RosterMember> runtimeRoster,
            IReadOnlyList<Participant> participants, IReadOnlyList<Edge> edges)
        {
            RuntimeRoster = Copy(runtimeRoster);
            Participants = Copy(participants);
            Edges = Copy(edges);
        }
    }

    public static string EncodePlan(IReadOnlyList<RosterMember> runtimeRoster,
        IReadOnlyList<Participant> participants, IReadOnlyList<Edge> edges)
    {
        var json = new StringBuilder();
        json.Append("{\"runtimeRoster\":");
        AppendRoster(json, runtimeRoster);
        json.Append(",\"participants\":");
        AppendParticipants(json, participants);
        json.Append(",\"edges\":");
        AppendEdges(json, edges);
        json.Append('}');
        return json.ToString();
    }

    public static bool TryDecodePlan(string json, out Plan plan)
    {
        plan = null;
        JsonValue root;
        if (!TryReadObject(json, out root)) return false;

        plan = new Plan(ReadRoster(root), ReadParticipants(root), ReadEdges(root));
        return true;
    }

    public static string EncodeEdge(string senderRuntimeId, string receiverRuntimeId)
    {
        var json = new StringBuilder();
        json.Append("{\"senderRuntimeId\":");
        AppendString(json, senderRuntimeId);
        json.Append(",\"receiverRuntimeId\":");
        AppendString(json, receiverRuntimeId);
        json.Append('}');
        return json.ToString();
    }

    public static bool TryDecodeEdge(string json, out Edge edge)
    {
        edge = null;
        JsonValue root;
        if (!TryReadObject(json, out root)) return false;
        edge = new Edge(ReadString(root, "senderRuntimeId"), ReadString(root, "receiverRuntimeId"));
        return true;
    }

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
    {
        if (source == null) return null;
        var copy = new T[source.Count];
        for (int i = 0; i < source.Count; i++) copy[i] = source[i];
        return Array.AsReadOnly(copy);
    }

    private static void AppendRoster(StringBuilder json, IReadOnlyList<RosterMember> rows)
    {
        if (rows == null) { json.Append("null"); return; }
        json.Append('[');
        for (int i = 0; i < rows.Count; i++)
        {
            if (i != 0) json.Append(',');
            RosterMember row = rows[i];
            if (row == null) { json.Append("null"); continue; }
            json.Append("{\"runtimeId\":"); AppendString(json, row.RuntimeId);
            json.Append(",\"personId\":"); AppendString(json, row.PersonId);
            json.Append('}');
        }
        json.Append(']');
    }

    private static void AppendParticipants(StringBuilder json, IReadOnlyList<Participant> rows)
    {
        if (rows == null) { json.Append("null"); return; }
        json.Append('[');
        for (int i = 0; i < rows.Count; i++)
        {
            if (i != 0) json.Append(',');
            Participant row = rows[i];
            if (row == null) { json.Append("null"); continue; }
            json.Append("{\"runtimeId\":"); AppendString(json, row.RuntimeId);
            json.Append(",\"personId\":"); AppendString(json, row.PersonId);
            json.Append(",\"cityRuntimeId\":"); AppendString(json, row.CityRuntimeId);
            json.Append(",\"locationRuntimeId\":"); AppendString(json, row.LocationRuntimeId);
            json.Append('}');
        }
        json.Append(']');
    }

    private static void AppendEdges(StringBuilder json, IReadOnlyList<Edge> rows)
    {
        if (rows == null) { json.Append("null"); return; }
        json.Append('[');
        for (int i = 0; i < rows.Count; i++)
        {
            if (i != 0) json.Append(',');
            Edge row = rows[i];
            if (row == null) { json.Append("null"); continue; }
            AppendEdgeFields(json, row);
        }
        json.Append(']');
    }

    private static void AppendEdgeFields(StringBuilder json, Edge edge)
    {
        json.Append("{\"senderRuntimeId\":"); AppendString(json, edge.SenderRuntimeId);
        json.Append(",\"receiverRuntimeId\":"); AppendString(json, edge.ReceiverRuntimeId);
        json.Append('}');
    }

    // JsonUtility serializes null string fields in these DTOs as empty strings.
    private static void AppendString(StringBuilder json, string value)
    {
        json.Append('"');
        if (value != null)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (current == '\0') break;
                switch (current)
                {
                    case '"': json.Append("\\\""); break;
                    case '\\': json.Append("\\\\"); break;
                    case '\b': json.Append("\\b"); break;
                    case '\t': json.Append("\\t"); break;
                    case '\n': json.Append("\\n"); break;
                    case '\f': json.Append("\\f"); break;
                    case '\r': json.Append("\\r"); break;
                    default:
                        if (current < 0x20)
                        {
                            json.Append("\\u00");
                            json.Append(((int)current).ToString("X2", CultureInfo.InvariantCulture));
                        }
                        else if (char.IsHighSurrogate(current))
                        {
                            if (i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                            {
                                json.Append(current);
                                json.Append(value[++i]);
                            }
                            else json.Append('\uFFFD');
                        }
                        else if (char.IsLowSurrogate(current)) json.Append('\uFFFD');
                        else json.Append(current);
                        break;
                }
            }
        }
        json.Append('"');
    }

    private static IReadOnlyList<RosterMember> ReadRoster(JsonValue root)
    {
        JsonValue array = ReadArray(root, "runtimeRoster");
        if (array == null) return null;
        var rows = new List<RosterMember>(array.Items.Count);
        foreach (JsonValue item in array.Items)
        {
            if (item.Kind == JsonKind.Null) rows.Add(new RosterMember(null, null));
            else rows.Add(new RosterMember(ReadString(item, "runtimeId"), ReadString(item, "personId")));
        }
        return rows;
    }

    private static IReadOnlyList<Participant> ReadParticipants(JsonValue root)
    {
        JsonValue array = ReadArray(root, "participants");
        if (array == null) return null;
        var rows = new List<Participant>(array.Items.Count);
        foreach (JsonValue item in array.Items)
        {
            if (item.Kind == JsonKind.Null) continue;
            rows.Add(new Participant(ReadString(item, "runtimeId"), ReadString(item, "personId"),
                ReadString(item, "cityRuntimeId"), ReadString(item, "locationRuntimeId")));
        }
        return rows;
    }

    private static IReadOnlyList<Edge> ReadEdges(JsonValue root)
    {
        JsonValue array = ReadArray(root, "edges");
        if (array == null) return null;
        var rows = new List<Edge>(array.Items.Count);
        foreach (JsonValue item in array.Items)
        {
            if (item.Kind == JsonKind.Null) continue;
            rows.Add(new Edge(ReadString(item, "senderRuntimeId"), ReadString(item, "receiverRuntimeId")));
        }
        return rows;
    }

    private static JsonValue ReadArray(JsonValue root, string name)
    {
        JsonValue value;
        if (!root.Properties.TryGetValue(name, out value)) return null;
        if (value.Kind == JsonKind.Null) return JsonValue.Array(new List<JsonValue>());
        return value.Kind == JsonKind.Array ? value : null;
    }

    private static string ReadString(JsonValue root, string name)
    {
        JsonValue value;
        if (root.Kind != JsonKind.Object || !root.Properties.TryGetValue(name, out value)) return null;
        return value.Kind == JsonKind.Null ? string.Empty : ToLegacyString(value);
    }

    private static string ToLegacyString(JsonValue value)
    {
        switch (value.Kind)
        {
            case JsonKind.String: return value.Text;
            case JsonKind.Number: return value.Text;
            case JsonKind.Boolean: return value.Text;
            default: return null;
        }
    }

    private static bool TryReadObject(string json, out JsonValue root)
    {
        root = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            var parser = new ClosedJsonReader(json);
            JsonValue value;
            if (!parser.TryRead(out value) || value.Kind != JsonKind.Object) return false;
            root = value;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private enum JsonKind { Null, Boolean, Number, String, Array, Object }

    private sealed class JsonValue
    {
        public readonly JsonKind Kind;
        public readonly string Text;
        public readonly List<JsonValue> Items;
        public readonly Dictionary<string, JsonValue> Properties;

        private JsonValue(JsonKind kind, string text, List<JsonValue> items, Dictionary<string, JsonValue> properties)
        {
            Kind = kind; Text = text; Items = items; Properties = properties;
        }

        public static JsonValue Scalar(JsonKind kind, string text) => new JsonValue(kind, text, null, null);
        public static JsonValue Array(List<JsonValue> items) => new JsonValue(JsonKind.Array, null, items, null);
        public static JsonValue Object(Dictionary<string, JsonValue> properties) => new JsonValue(JsonKind.Object, null, null, properties);
    }

    /// <summary>A small strict JSON reader for the two frozen DTO shapes and ignored extension values.</summary>
    private sealed class ClosedJsonReader
    {
        private const int MaximumDepth = 128;
        private readonly string _json;
        private int _position;

        public ClosedJsonReader(string json) { _json = json; }

        public bool TryRead(out JsonValue value)
        {
            value = null;
            SkipWhitespace();
            if (!ReadValue(out value, 0)) return false;
            SkipWhitespace();
            return _position == _json.Length;
        }

        private bool ReadValue(out JsonValue value, int depth)
        {
            value = null;
            if (depth > MaximumDepth || _position >= _json.Length) return false;
            char next = _json[_position];
            if (next == '{') return ReadObject(out value, depth + 1);
            if (next == '[') return ReadArray(out value, depth + 1);
            if (next == '"')
            {
                string text;
                if (!ReadString(out text)) return false;
                value = JsonValue.Scalar(JsonKind.String, text);
                return true;
            }
            if (next == 'n' && ReadLiteral("null")) { value = JsonValue.Scalar(JsonKind.Null, null); return true; }
            if (next == 't' && ReadLiteral("true")) { value = JsonValue.Scalar(JsonKind.Boolean, "true"); return true; }
            if (next == 'f' && ReadLiteral("false")) { value = JsonValue.Scalar(JsonKind.Boolean, "false"); return true; }
            string number;
            if (!ReadNumber(out number)) return false;
            value = JsonValue.Scalar(JsonKind.Number, number);
            return true;
        }

        private bool ReadObject(out JsonValue value, int depth)
        {
            value = null;
            _position++;
            SkipWhitespace();
            var properties = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            if (Take('}')) { value = JsonValue.Object(properties); return true; }
            while (true)
            {
                string key;
                if (!ReadString(out key)) return false;
                SkipWhitespace();
                if (!Take(':')) return false;
                SkipWhitespace();
                JsonValue item;
                if (!ReadValue(out item, depth)) return false;
                if (!properties.ContainsKey(key)) properties.Add(key, item);
                SkipWhitespace();
                if (Take('}')) break;
                if (!Take(',')) return false;
                SkipWhitespace();
            }
            value = JsonValue.Object(properties);
            return true;
        }

        private bool ReadArray(out JsonValue value, int depth)
        {
            value = null;
            _position++;
            SkipWhitespace();
            var items = new List<JsonValue>();
            if (Take(']')) { value = JsonValue.Array(items); return true; }
            while (true)
            {
                JsonValue item;
                if (!ReadValue(out item, depth)) return false;
                items.Add(item);
                SkipWhitespace();
                if (Take(']')) break;
                if (!Take(',')) return false;
                SkipWhitespace();
            }
            value = JsonValue.Array(items);
            return true;
        }

        private bool ReadString(out string value)
        {
            value = null;
            if (!Take('"')) return false;
            var result = new StringBuilder();
            while (_position < _json.Length)
            {
                char current = _json[_position++];
                if (current == '"')
                {
                    value = result.ToString();
                    return true;
                }
                if (current < 0x20) return false;
                if (current != '\\')
                {
                    if (char.IsHighSurrogate(current))
                    {
                        if (_position >= _json.Length || !char.IsLowSurrogate(_json[_position])) return false;
                        result.Append(current);
                        result.Append(_json[_position++]);
                    }
                    else if (!char.IsLowSurrogate(current)) result.Append(current);
                    continue;
                }
                if (_position >= _json.Length) return false;
                char escape = _json[_position++];
                switch (escape)
                {
                    case '"': result.Append('"'); break;
                    case '\\': result.Append('\\'); break;
                    case '/': result.Append('/'); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case 'u':
                        if (_position + 4 > _json.Length) return false;
                        int codeUnit = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            int digit = HexDigit(_json[_position++]);
                            if (digit < 0) return false;
                            codeUnit = (codeUnit << 4) | digit;
                        }
                        char decoded = (char)codeUnit;
                        if (char.IsHighSurrogate(decoded))
                        {
                            if (_position + 6 > _json.Length || _json[_position] != '\\' || _json[_position + 1] != 'u') return false;
                            _position += 2;
                            int lowCodeUnit = 0;
                            for (int i = 0; i < 4; i++)
                            {
                                int digit = HexDigit(_json[_position++]);
                                if (digit < 0) return false;
                                lowCodeUnit = (lowCodeUnit << 4) | digit;
                            }
                            char low = (char)lowCodeUnit;
                            if (!char.IsLowSurrogate(low)) return false;
                            result.Append(decoded);
                            result.Append(low);
                        }
                        else if (!char.IsLowSurrogate(decoded)) result.Append(decoded);
                        break;
                    default: return false;
                }
            }
            return false;
        }

        private bool ReadNumber(out string value)
        {
            value = null;
            int start = _position;
            if (Take('-') && _position >= _json.Length) return false;
            if (Take('0'))
            {
                if (_position < _json.Length && IsDigit(_json[_position])) return false;
            }
            else
            {
                if (_position >= _json.Length || _json[_position] < '1' || _json[_position] > '9') { _position = start; return false; }
                while (_position < _json.Length && IsDigit(_json[_position])) _position++;
            }
            if (Take('.'))
            {
                int fraction = _position;
                while (_position < _json.Length && IsDigit(_json[_position])) _position++;
                if (fraction == _position) return false;
            }
            if (Take('e') || Take('E'))
            {
                if (!Take('+')) Take('-');
                int exponent = _position;
                while (_position < _json.Length && IsDigit(_json[_position])) _position++;
                if (exponent == _position) return false;
            }
            value = _json.Substring(start, _position - start);
            return true;
        }

        private bool ReadLiteral(string literal)
        {
            if (_position + literal.Length > _json.Length
                || string.CompareOrdinal(_json, _position, literal, 0, literal.Length) != 0) return false;
            _position += literal.Length;
            return true;
        }

        private void SkipWhitespace()
        {
            while (_position < _json.Length)
            {
                char current = _json[_position];
                if (current == ' ' || current == '\t' || current == '\r' || current == '\n') _position++;
                else break;
            }
        }

        private bool Take(char expected)
        {
            if (_position >= _json.Length || _json[_position] != expected) return false;
            _position++;
            return true;
        }

        private static bool IsDigit(char value) => value >= '0' && value <= '9';

        private static int HexDigit(char value)
        {
            if (value >= '0' && value <= '9') return value - '0';
            if (value >= 'a' && value <= 'f') return value - 'a' + 10;
            if (value >= 'A' && value <= 'F') return value - 'A' + 10;
            return -1;
        }

    }
}
