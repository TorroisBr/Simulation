using System;

/// <summary>A stable identity for a concrete crossing that exists in world truth.</summary>
public sealed class CrossingId : IEquatable<CrossingId>
{
    public string Value { get; }

    public CrossingId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("CrossingId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(CrossingId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as CrossingId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(CrossingId left, CrossingId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(CrossingId left, CrossingId right) => (left == right) == false;
}

/// <summary>A stable identity for an explicit persistent traversal connection.</summary>
public sealed class ConnectionId : IEquatable<ConnectionId>
{
    public string Value { get; }

    public ConnectionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ConnectionId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(ConnectionId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as ConnectionId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(ConnectionId left, ConnectionId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(ConnectionId left, ConnectionId right) => (left == right) == false;
}

/// <summary>
/// The unordered geometric interface between two distinct Hexes. Construction
/// canonicalizes identity; registration in finite geography establishes whether
/// that pair is actually adjacent.
/// </summary>
public sealed class HexBoundaryKey : IEquatable<HexBoundaryKey>
{
    public HexId FirstHexId { get; }
    public HexId SecondHexId { get; }

    public HexBoundaryKey(HexId firstHexId, HexId secondHexId)
    {
        if (firstHexId == null) throw new ArgumentNullException(nameof(firstHexId));
        if (secondHexId == null) throw new ArgumentNullException(nameof(secondHexId));
        if (firstHexId == secondHexId)
        {
            throw new ArgumentException("A Hex boundary requires two distinct HexIds.", nameof(secondHexId));
        }

        if (StringComparer.Ordinal.Compare(firstHexId.Value, secondHexId.Value) <= 0)
        {
            FirstHexId = firstHexId;
            SecondHexId = secondHexId;
        }
        else
        {
            FirstHexId = secondHexId;
            SecondHexId = firstHexId;
        }
    }

    public bool Contains(HexId hexId) => hexId != null
        && (FirstHexId == hexId || SecondHexId == hexId);

    public bool Equals(HexBoundaryKey other) => other != null
        && FirstHexId == other.FirstHexId && SecondHexId == other.SecondHexId;
    public override bool Equals(object obj) => Equals(obj as HexBoundaryKey);
    public override int GetHashCode() => (FirstHexId.GetHashCode() * 397) ^ SecondHexId.GetHashCode();
    public override string ToString() => FirstHexId.Value + "|" + SecondHexId.Value;
}

public enum TraversalOptionKind
{
    Connection = 0,
    Crossing = 1,
    WildernessRule = 2
}

/// <summary>
/// Stable typed identity for an option over one boundary. It contains no
/// condition or availability state; those remain mutable factual/contextual data.
/// </summary>
public sealed class TraversalOptionRef : IEquatable<TraversalOptionRef>, IComparable<TraversalOptionRef>
{
    private TraversalOptionRef(
        TraversalOptionKind kind,
        ConnectionId connectionId,
        CrossingId crossingId,
        string ruleIdentity,
        string ruleVersion)
    {
        Kind = kind;
        ConnectionId = connectionId;
        CrossingId = crossingId;
        RuleIdentity = ruleIdentity;
        RuleVersion = ruleVersion;
    }

    public TraversalOptionKind Kind { get; }
    public ConnectionId ConnectionId { get; }
    public CrossingId CrossingId { get; }
    public string RuleIdentity { get; }
    public string RuleVersion { get; }

    public static TraversalOptionRef ForConnection(ConnectionId id) =>
        new TraversalOptionRef(TraversalOptionKind.Connection, id ?? throw new ArgumentNullException(nameof(id)), null, null, null);

    public static TraversalOptionRef ForCrossing(CrossingId id) =>
        new TraversalOptionRef(TraversalOptionKind.Crossing, null, id ?? throw new ArgumentNullException(nameof(id)), null, null);

    public static TraversalOptionRef ForWildernessRule(string identity, string version)
    {
        if (string.IsNullOrWhiteSpace(identity)) throw new ArgumentException("A wilderness traversal rule requires a stable identity.", nameof(identity));
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("A wilderness traversal rule requires a stable version.", nameof(version));
        return new TraversalOptionRef(TraversalOptionKind.WildernessRule, null, null, identity, version);
    }

    public bool Equals(TraversalOptionRef other) => other != null
        && Kind == other.Kind
        && ConnectionId == other.ConnectionId
        && CrossingId == other.CrossingId
        && string.Equals(RuleIdentity, other.RuleIdentity, StringComparison.Ordinal)
        && string.Equals(RuleVersion, other.RuleVersion, StringComparison.Ordinal);

    public int CompareTo(TraversalOptionRef other)
    {
        if (other == null) return 1;
        int kindComparison = Kind.CompareTo(other.Kind);
        if (kindComparison != 0) return kindComparison;
        switch (Kind)
        {
            case TraversalOptionKind.Connection:
                return StringComparer.Ordinal.Compare(ConnectionId?.Value, other.ConnectionId?.Value);
            case TraversalOptionKind.Crossing:
                return StringComparer.Ordinal.Compare(CrossingId?.Value, other.CrossingId?.Value);
            case TraversalOptionKind.WildernessRule:
                int identityComparison = StringComparer.Ordinal.Compare(RuleIdentity, other.RuleIdentity);
                return identityComparison != 0
                    ? identityComparison
                    : StringComparer.Ordinal.Compare(RuleVersion, other.RuleVersion);
            default:
                return 0;
        }
    }
    public override bool Equals(object obj) => Equals(obj as TraversalOptionRef);
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)Kind;
            hash = (hash * 397) ^ (ConnectionId == null ? 0 : ConnectionId.GetHashCode());
            hash = (hash * 397) ^ (CrossingId == null ? 0 : CrossingId.GetHashCode());
            hash = (hash * 397) ^ (RuleIdentity == null ? 0 : StringComparer.Ordinal.GetHashCode(RuleIdentity));
            return (hash * 397) ^ (RuleVersion == null ? 0 : StringComparer.Ordinal.GetHashCode(RuleVersion));
        }
    }
}

/// <summary>A concrete crossing physically associated with an adjacent boundary.</summary>
public sealed class CrossingRecord
{
    public CrossingId Id { get; }
    public HexBoundaryKey Boundary { get; }
    public HexId AnchorHexId { get; }

    public CrossingRecord(CrossingId id, HexBoundaryKey boundary, HexId anchorHexId)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        AnchorHexId = anchorHexId ?? throw new ArgumentNullException(nameof(anchorHexId));
    }
}
