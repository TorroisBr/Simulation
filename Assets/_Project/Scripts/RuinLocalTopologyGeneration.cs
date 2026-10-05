using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>Pure, versioned P10-B Ruin topology generation; it has no Unity or world-store access.</summary>
public static class RuinLocalTopologyGeneration
{
    public const string GeneratorId = "p10b.ruin-local-topology";
    public const string GeneratorVersion = "1";
    public const string RulesRevision = "p10b.ruin-structure/v1";
    public const string IdentityEncoding = "p10b-id/v1";
    public const string DrawEncoding = "p10b-draw/v1";
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public sealed class Request
    {
        public string P9ProfileFingerprint { get; }
        public long EffectiveGenesisSeed { get; }
        public string StableSiteKey { get; }
        public string SiteInstanceId { get; }
        public string DefinitionId { get; }
        public string LocationId { get; }
        public string GeneratorIdValue { get; }
        public string GeneratorVersionValue { get; }
        public string RulesRevisionValue { get; }
        public string RequestFingerprint { get; }

        public Request(string p9ProfileFingerprint, long effectiveGenesisSeed, string stableSiteKey, string definitionId, string locationId, string generatorId = GeneratorId, string generatorVersion = GeneratorVersion, string rulesRevision = RulesRevision)
        {
            P9ProfileFingerprint = Require(p9ProfileFingerprint, nameof(p9ProfileFingerprint));
            EffectiveGenesisSeed = effectiveGenesisSeed;
            StableSiteKey = Require(stableSiteKey, nameof(stableSiteKey));
            DefinitionId = Require(definitionId, nameof(definitionId));
            LocationId = Require(locationId, nameof(locationId));
            GeneratorIdValue = Require(generatorId, nameof(generatorId));
            GeneratorVersionValue = Require(generatorVersion, nameof(generatorVersion));
            RulesRevisionValue = Require(rulesRevision, nameof(rulesRevision));
            if (GeneratorIdValue != GeneratorId || GeneratorVersionValue != GeneratorVersion || RulesRevisionValue != RulesRevision) throw new ArgumentException("Unsupported P10-B generator or rules version.");
            SiteInstanceId = DeriveSiteInstanceId(StableSiteKey, DefinitionId, LocationId);
            if (SiteInstanceId == DefinitionId || SiteInstanceId == LocationId)
                throw new ArgumentException("Derived SiteInstanceId must differ from the definition and Location IDs.");
            RequestFingerprint = Hex(Hash(Frame("p10b-request/v1", P9ProfileFingerprint, SeedBytes(EffectiveGenesisSeed),
                StableSiteKey, SiteInstanceId, DefinitionId, LocationId, GeneratorIdValue, GeneratorVersionValue, RulesRevisionValue,
                "minPlaces=6", "maxPlaces=10", "maxConnections=19", "maxContainmentDepth=4")));
        }
    }

    public sealed class Place
    {
        public string LocalKey { get; }
        public string RuntimeId { get; }
        public string ParentLocalKey { get; }
        public bool IsEntry { get; }
        internal Place(string key, string id, string parent, bool entry) { LocalKey = key; RuntimeId = id; ParentLocalKey = parent; IsEntry = entry; }
    }

    public sealed class Connection
    {
        public string LocalKey { get; }
        public string RuntimeId { get; }
        public string OriginLocalKey { get; }
        public string DestinationLocalKey { get; }
        public int TraversalCost { get; }
        internal Connection(string key, string id, string origin, string destination, int cost)
        { LocalKey = key; RuntimeId = id; OriginLocalKey = origin; DestinationLocalKey = destination; TraversalCost = cost; }
    }

    public sealed class GeneratedTopology
    {
        public Request Request { get; }
        public IReadOnlyList<Place> Places { get; }
        public IReadOnlyList<Connection> Connections { get; }
        public string GraphFingerprint { get; }
        public string ComposedProfileFingerprint { get; }
        internal GeneratedTopology(Request request, List<Place> places, List<Connection> connections, string fingerprint)
        { Request = request; Places = places.AsReadOnly(); Connections = connections.AsReadOnly(); GraphFingerprint = fingerprint; ComposedProfileFingerprint = Hex(Hash(Frame("p10b.genesis-profile/v1", request.P9ProfileFingerprint, request.RequestFingerprint, fingerprint))); }
    }

    public static string DeriveSiteInstanceId(string stableSiteKey, string definitionId, string locationId) =>
        "site-p10b-v1-" + Hex(Hash(Frame("p10b.site-id/v1", Require(stableSiteKey, nameof(stableSiteKey)),
            Require(definitionId, nameof(definitionId)), Require(locationId, nameof(locationId)))));

    public static GeneratedTopology Generate(Request request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        byte[] root = Hash(Frame(DrawEncoding, request.P9ProfileFingerprint, SeedBytes(request.EffectiveGenesisSeed),
            request.SiteInstanceId, request.GeneratorIdValue, request.GeneratorVersionValue, request.RulesRevisionValue));
        int count = 6 + (int)NextBelow(root, "node-count", 5);
        var parents = new int[count];
        parents[0] = -1;
        var depths = new int[count];
        var edges = new SortedSet<long>();

        for (int i = 1; i < count; i++)
        {
            int low = Math.Max(0, i - 3);
            int parent = low + (int)NextBelow(root, "tree-parent/" + i.ToString("D4", CultureInfo.InvariantCulture), (uint)(i - low));
            parents[i] = parent;
            edges.Add(EdgeKey(parent, i));
        }

        for (int i = 1; i < count; i++)
        {
            if (depths[parents[i]] < 4 && NextBelow(root, "containment/" + i.ToString(CultureInfo.InvariantCulture), 4) == 0)
                depths[i] = depths[parents[i]] + 1;
            else
                parents[i] = -1;
        }

        for (int i = 0; i < count && edges.Count < 19; i++)
        {
            for (int j = i + 1; j < count && edges.Count < 19; j++)
            {
                if (!edges.Contains(EdgeKey(i, j))
                    && NextBelow(root, "alternate-route/" + i.ToString(CultureInfo.InvariantCulture) + "/" + j.ToString(CultureInfo.InvariantCulture), 4) == 0)
                    edges.Add(EdgeKey(i, j));
            }
        }

        var places = new List<Place>(count);
        for (int i = 0; i < count; i++)
        {
            string key = PlaceKey(i);
            places.Add(new Place(key, MemberId(request.SiteInstanceId, "place", key),
                parents[i] < 0 ? null : PlaceKey(parents[i]), i == 0));
        }

        var connections = new List<Connection>(edges.Count);
        foreach (long edge in edges)
        {
            int origin = (int)(edge >> 32);
            int destination = (int)(edge & 0xffffffffL);
            string key = "edge/" + origin.ToString(CultureInfo.InvariantCulture) + "/" + destination.ToString(CultureInfo.InvariantCulture);
            int cost = 1 + (int)NextBelow(root, "cost/" + origin.ToString(CultureInfo.InvariantCulture) + "/" + destination.ToString(CultureInfo.InvariantCulture), 3);
            connections.Add(new Connection(key, MemberId(request.SiteInstanceId, "connection", key),
                PlaceKey(origin), PlaceKey(destination), cost));
        }

        Validate(places, connections);
        return new GeneratedTopology(request, places, connections, GraphFingerprint(request.RequestFingerprint, places, connections));
    }

    private static string MemberId(string site, string kind, string key) =>
        (kind == "place" ? "place-p10b-v1-" : "connection-p10b-v1-") + Hex(Hash(Frame(IdentityEncoding, site, RulesRevision, kind, key)));

    private static void Validate(List<Place> places, List<Connection> connections)
    {
        if (places.Count < 6 || places.Count > 10 || connections.Count < 5 || connections.Count > 19)
            throw new InvalidOperationException("Generated topology is outside the P10-B v1 finite bounds.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        int entries = 0;
        foreach (Place place in places) { if (!ids.Add(place.RuntimeId)) throw new InvalidOperationException("Duplicate generated member ID."); if (place.IsEntry) entries++; }
        foreach (Connection connection in connections) if (!ids.Add(connection.RuntimeId)) throw new InvalidOperationException("Duplicate generated member ID.");
        if (entries != 1 || !places[0].IsEntry) throw new InvalidOperationException("Generated topology must have one entry.");

        var reached = new HashSet<string>(StringComparer.Ordinal) { places[0].LocalKey };
        bool changed;
        do
        {
            changed = false;
            foreach (Connection connection in connections)
                if (reached.Contains(connection.OriginLocalKey) && reached.Add(connection.DestinationLocalKey)) changed = true;
        } while (changed);
        if (reached.Count != places.Count) throw new InvalidOperationException("Generated topology contains an unreachable place.");

        foreach (Place place in places)
        {
            int depth = 0;
            string parent = place.ParentLocalKey;
            while (parent != null)
            {
                if (++depth > 4) throw new InvalidOperationException("Generated containment exceeds v1 depth.");
                parent = Find(places, parent).ParentLocalKey;
            }
        }
    }

    private static string GraphFingerprint(string requestFingerprint, List<Place> places, List<Connection> connections)
    {
        var fields = new List<object> { "p10b-graph/v1", requestFingerprint };
        foreach (Place place in places)
        {
            fields.Add("place"); fields.Add(place.LocalKey); fields.Add(place.RuntimeId); fields.Add(place.ParentLocalKey ?? "");
            fields.Add(place.IsEntry ? "1" : "0");
        }
        foreach (Connection connection in connections)
        {
            fields.Add("connection"); fields.Add(connection.LocalKey); fields.Add(connection.RuntimeId); fields.Add(connection.OriginLocalKey);
            fields.Add(connection.DestinationLocalKey); fields.Add(connection.TraversalCost.ToString(CultureInfo.InvariantCulture));
        }
        return Hex(Hash(Frame(fields.ToArray())));
    }

    private static Place Find(List<Place> places, string key)
    { foreach (Place place in places) if (place.LocalKey == key) return place; throw new InvalidOperationException("Missing generated containment parent."); }
    private static string PlaceKey(int i) => "place/" + i.ToString("D6", CultureInfo.InvariantCulture);
    private static long EdgeKey(int i, int j) => ((long)i << 32) | (uint)j;

    private static uint NextBelow(byte[] root, string key, uint n)
    {
        if (n == 0) throw new ArgumentOutOfRangeException(nameof(n));
        ulong threshold = unchecked(0UL - (ulong)n) % n;
        for (uint counter = 0; ; counter++)
        {
            byte[] digest = Hash(Concat(root, Frame("draw", key), UInt32(counter)));
            ulong value = 0;
            for (int i = 0; i < 8; i++) value = (value << 8) | digest[i];
            if (value >= threshold) return (uint)(value % n);
            if (counter == uint.MaxValue) throw new InvalidOperationException("P10-B draw retry counter exhausted.");
        }
    }

    private static byte[] Frame(params object[] values)
    {
        using (var stream = new MemoryStream())
        {
            foreach (object value in values)
            {
                byte[] bytes = value is byte[] raw ? raw : StrictUtf8.GetBytes((string)value);
                stream.Write(UInt32((uint)bytes.Length), 0, 4);
                stream.Write(bytes, 0, bytes.Length);
            }
            return stream.ToArray();
        }
    }
    private static byte[] SeedBytes(long seed)
    {
        ulong value = unchecked((ulong)seed); var bytes = new byte[8];
        for (int i = 7; i >= 0; i--) { bytes[i] = (byte)value; value >>= 8; }
        return bytes;
    }
    private static byte[] UInt32(uint value) => new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
    private static byte[] Concat(params byte[][] arrays)
    {
        int length = 0; foreach (byte[] array in arrays) length = checked(length + array.Length);
        var result = new byte[length]; int offset = 0;
        foreach (byte[] array in arrays) { Buffer.BlockCopy(array, 0, result, offset, array.Length); offset += array.Length; }
        return result;
    }
    private static byte[] Hash(byte[] bytes) { using (SHA256 sha = SHA256.Create()) return sha.ComputeHash(bytes); }
    private static string Hex(byte[] bytes)
    { var result = new StringBuilder(bytes.Length * 2); foreach (byte value in bytes) result.Append(value.ToString("x2", CultureInfo.InvariantCulture)); return result.ToString(); }
    private static string Require(string value, string name)
    { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name); StrictUtf8.GetByteCount(value); return value; }
}
