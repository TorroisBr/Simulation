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

public sealed class BarrierId : IEquatable<BarrierId>
{
    public string Value { get; }
    public BarrierId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("BarrierId requires a non-empty value.", nameof(value));
        Value = value;
    }
    public bool Equals(BarrierId other) => other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as BarrierId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(BarrierId left, BarrierId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(BarrierId left, BarrierId right) => (left == right) == false;
}

/// <summary>
/// The unordered geometric interface between two distinct Hexes. Construction
/// canonicalizes identity; registration in finite geography establishes whether
/// that pair is actually adjacent.
/// </summary>
public sealed class HexBoundaryKey : IEquatable<HexBoundaryKey>, IComparable<HexBoundaryKey>
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
    public int CompareTo(HexBoundaryKey other)
    {
        if (other == null) return 1;
        int firstComparison = StringComparer.Ordinal.Compare(FirstHexId.Value, other.FirstHexId.Value);
        return firstComparison != 0
            ? firstComparison
            : StringComparer.Ordinal.Compare(SecondHexId.Value, other.SecondHexId.Value);
    }
    public override bool Equals(object obj) => Equals(obj as HexBoundaryKey);
    public override int GetHashCode() => (FirstHexId.GetHashCode() * 397) ^ SecondHexId.GetHashCode();
    public override string ToString() => FirstHexId.Value + "|" + SecondHexId.Value;
}
