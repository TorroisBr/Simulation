using System;

public sealed class HexId : IEquatable<HexId>
{
    public string Value { get; }

    public HexId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("HexId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(HexId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as HexId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(HexId left, HexId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(HexId left, HexId right) => (left == right) == false;
}

public sealed class LocationId : IEquatable<LocationId>
{
    public string Value { get; }

    public LocationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("LocationId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(LocationId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as LocationId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(LocationId left, LocationId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(LocationId left, LocationId right) => (left == right) == false;
}

public sealed class HexRecord
{
    public HexId Id { get; }
    public HexCoordinate? Coordinate { get; }
    public TerrainReference TerrainReference { get; }
    public TerrainDefinitionId TerrainDefinitionId => TerrainReference?.TerrainDefinitionId;
    public string AuthoredRevisionToken => TerrainReference?.AuthoredRevisionToken;
    public bool IsGeographic => Coordinate.HasValue && TerrainReference != null;

    public HexRecord(HexId id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public HexRecord(HexId id, HexCoordinate coordinate, TerrainReference terrainReference)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        TerrainReference = terrainReference ?? throw new ArgumentNullException(nameof(terrainReference));
        Coordinate = coordinate;
    }
}

public sealed class LocationRecord
{
    public LocationId Id { get; }
    public HexId AnchorHexId { get; }

    public LocationRecord(LocationId id, HexId anchorHexId)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        AnchorHexId = anchorHexId ?? throw new ArgumentNullException(nameof(anchorHexId));
    }
}
