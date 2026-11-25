using System.Collections.Immutable;

namespace WorldAtlas.Observations.Models;

public enum ObservationSourceKind
{
    Demo
}

public enum CapabilityCoverage
{
    Complete,
    Unsupported,
    NotIncluded
}

public enum FactAvailability
{
    Known,
    Unknown,
    NotCovered,
    NotApplicable
}

public enum AtlasEntityKind
{
    World,
    Hex,
    Location,
    City
}

public readonly record struct AtlasEntityKey(AtlasEntityKind Kind, string SourceId);

public sealed record ObservationBoundary(long AbsoluteDay, long CompletedSequence);

public sealed record ProducerMetadata(
    string Id,
    int Version,
    string Build,
    string ScenarioId,
    string ScenarioRevision,
    string CompositionProfile,
    string EffectiveInputFingerprint);

public sealed record HexFact(
    string HexId,
    int? Q,
    int? R,
    string? TerrainId,
    string? TerrainRevision)
{
    public bool HasCoordinates => Q.HasValue && R.HasValue;
}

public sealed record LocationFact(string LocationId, string AnchorHexId);

public sealed record CityFact(string CityId, string? Label, string? P8LocationId);

public sealed record StockScope(string CityId, string ItemDefinitionId);

public sealed record StockRow(string CityId, string ItemDefinitionId, long Quantity);

public sealed record GeographyCapability(
    string Scope,
    CapabilityCoverage Coverage,
    string? SourceStamp,
    ImmutableArray<HexFact> Hexes,
    ImmutableArray<LocationFact> Locations);

public sealed record CitiesCapability(
    string Scope,
    CapabilityCoverage Coverage,
    string? SourceStamp,
    CapabilityCoverage BindingCoverage,
    ImmutableArray<CityFact> Rows);

public sealed record CityMarketStockCapability(
    StockScope Scope,
    CapabilityCoverage Coverage,
    string? SourceStamp,
    ImmutableArray<StockRow> Rows);

public sealed record ObservationDocument(
    string Schema,
    int Version,
    string Profile,
    ObservationSourceKind SourceKind,
    string WorldId,
    string RunId,
    long ObservationIndex,
    string? PreviousDigest,
    ObservationBoundary Boundary,
    ProducerMetadata Producer,
    GeographyCapability Geography,
    CitiesCapability Cities,
    CityMarketStockCapability CityMarketStock,
    string RawDigest,
    string? SourcePath)
{
    public ImmutableArray<HexFact> PlacedHexes =>
        Geography.Hexes.Where(hex => hex.HasCoordinates).ToImmutableArray();

    public ImmutableArray<HexFact> UnplacedHexes =>
        Geography.Hexes.Where(hex => !hex.HasCoordinates).ToImmutableArray();

    public bool TryGetStock(string cityId, string itemId, out StockRow? row)
    {
        row = CityMarketStock.Rows.SingleOrDefault(item =>
            StringComparer.Ordinal.Equals(item.CityId, cityId) &&
            StringComparer.Ordinal.Equals(item.ItemDefinitionId, itemId));
        return row is not null;
    }
}

public sealed record FactValue(string Name, object? Value, FactAvailability Availability, string? Detail = null)
{
    public string DisplayValue => Availability switch
    {
        FactAvailability.Known => Value?.ToString() ?? "Known (null)",
        FactAvailability.Unknown => Detail ?? "Unknown",
        FactAvailability.NotCovered => Detail ?? "Not covered by this observation",
        FactAvailability.NotApplicable => Detail ?? "Not applicable",
        _ => "Unknown"
    };
}
