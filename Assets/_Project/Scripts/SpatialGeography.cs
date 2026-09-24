using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

/// <summary>
/// Semantic integer coordinates in the axial hex convention. These values do
/// not represent a Unity or rendering coordinate.
/// </summary>
public struct HexCoordinate : IEquatable<HexCoordinate>, IComparable<HexCoordinate>
{
    public const string ConventionVersion = "axial-hex-v1";
    public const string CanonicalOrder = "q-then-r";

    public int Q { get; }
    public int R { get; }

    public HexCoordinate(int q, int r)
    {
        Q = q;
        R = r;
    }

    public bool TryOffset(HexCoordinate delta, out HexCoordinate result)
    {
        try
        {
            result = new HexCoordinate(checked(Q + delta.Q), checked(R + delta.R));
            return true;
        }
        catch (OverflowException)
        {
            result = default(HexCoordinate);
            return false;
        }
    }

    public int CompareTo(HexCoordinate other)
    {
        int qComparison = Q.CompareTo(other.Q);
        return qComparison != 0 ? qComparison : R.CompareTo(other.R);
    }

    public bool Equals(HexCoordinate other) => Q == other.Q && R == other.R;
    public override bool Equals(object obj) => obj is HexCoordinate other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            return (Q * 397) ^ R;
        }
    }

    public override string ToString() => "("
        + Q.ToString(CultureInfo.InvariantCulture)
        + ","
        + R.ToString(CultureInfo.InvariantCulture)
        + ")";

    public static bool operator ==(HexCoordinate left, HexCoordinate right) => left.Equals(right);
    public static bool operator !=(HexCoordinate left, HexCoordinate right) => !left.Equals(right);
    public static bool operator <(HexCoordinate left, HexCoordinate right) => left.CompareTo(right) < 0;
    public static bool operator >(HexCoordinate left, HexCoordinate right) => left.CompareTo(right) > 0;
}

/// <summary>A stable reference to content-defined terrain; it is not a terrain catalog.</summary>
public sealed class TerrainDefinitionId : IEquatable<TerrainDefinitionId>
{
    public string Value { get; }

    public TerrainDefinitionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("TerrainDefinitionId requires a non-empty stable value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(TerrainDefinitionId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as TerrainDefinitionId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(TerrainDefinitionId left, TerrainDefinitionId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(TerrainDefinitionId left, TerrainDefinitionId right) => (left == right) == false;
}

/// <summary>
/// One resolved, world-local physical scale convention. It is independent of
/// global simulation and travel configuration.
/// </summary>
public sealed class SpatialWorldScaleContext : IEquatable<SpatialWorldScaleContext>
{
    public string ResolvedConventionId { get; }
    public string SourceIdentity { get; }
    public string SourceVersion { get; }
    public decimal DistancePerNeighborStep { get; }
    public string Unit { get; }

    public SpatialWorldScaleContext(
        string resolvedConventionId,
        string sourceIdentity,
        string sourceVersion,
        decimal distancePerNeighborStep,
        string unit)
    {
        if (string.IsNullOrWhiteSpace(resolvedConventionId))
        {
            throw new ArgumentException("A resolved scale convention requires a stable identity.", nameof(resolvedConventionId));
        }

        if (string.IsNullOrWhiteSpace(sourceIdentity))
        {
            throw new ArgumentException("A resolved scale convention requires source provenance.", nameof(sourceIdentity));
        }

        if (string.IsNullOrWhiteSpace(sourceVersion))
        {
            throw new ArgumentException("A resolved scale convention requires a source version or revision.", nameof(sourceVersion));
        }

        if (distancePerNeighborStep <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(distancePerNeighborStep), "Distance per neighbor step must be positive.");
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("A resolved scale convention requires an authored unit.", nameof(unit));
        }

        ResolvedConventionId = resolvedConventionId;
        SourceIdentity = sourceIdentity;
        SourceVersion = sourceVersion;
        DistancePerNeighborStep = distancePerNeighborStep;
        Unit = unit;
    }

    public bool Equals(SpatialWorldScaleContext other) => other != null
        && string.Equals(ResolvedConventionId, other.ResolvedConventionId, StringComparison.Ordinal)
        && string.Equals(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal)
        && string.Equals(SourceVersion, other.SourceVersion, StringComparison.Ordinal)
        && DistancePerNeighborStep == other.DistancePerNeighborStep
        && string.Equals(Unit, other.Unit, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as SpatialWorldScaleContext);
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = StringComparer.Ordinal.GetHashCode(ResolvedConventionId);
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SourceIdentity);
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SourceVersion);
            hash = (hash * 397) ^ DistancePerNeighborStep.GetHashCode();
            return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Unit);
        }
    }
}

/// <summary>
/// Explicit authored finite geography input. Composition into a spatial
/// authority is validated and applied atomically by SpatialAuthorityStore.
/// </summary>
public sealed class SpatialGeographyDefinition
{
    private readonly ReadOnlyCollection<HexRecord> hexes;
    private readonly ReadOnlyCollection<LocationRecord> locations;

    public string CoordinateConventionVersion => HexCoordinate.ConventionVersion;
    public string CoordinateCanonicalOrder => HexCoordinate.CanonicalOrder;
    public SpatialWorldScaleContext ScaleContext { get; }
    public IReadOnlyList<HexRecord> Hexes => hexes;
    public IReadOnlyList<LocationRecord> Locations => locations;

    public SpatialGeographyDefinition(
        SpatialWorldScaleContext scaleContext,
        IEnumerable<HexRecord> hexes,
        IEnumerable<LocationRecord> locations = null)
    {
        ScaleContext = scaleContext ?? throw new ArgumentNullException(nameof(scaleContext));
        this.hexes = new ReadOnlyCollection<HexRecord>(hexes == null
            ? new List<HexRecord>()
            : new List<HexRecord>(hexes));
        this.locations = new ReadOnlyCollection<LocationRecord>(locations == null
            ? new List<LocationRecord>()
            : new List<LocationRecord>(locations));
    }
}
