using System.Collections.Immutable;
using WorldAtlas.Observations.Models;

namespace WorldAtlas.Observations.Navigation;

public sealed record NavigationItem(AtlasEntityKey Key, string Label, string Detail);

public static class WorldNavigator
{
    public static ImmutableArray<NavigationItem> List(ObservationDocument observation, AtlasEntityKind kind)
    {
        ArgumentNullException.ThrowIfNull(observation);
        return kind switch
        {
            AtlasEntityKind.World => ImmutableArray.Create(new NavigationItem(
                new AtlasEntityKey(AtlasEntityKind.World, observation.WorldId), "Demo World", observation.WorldId)),
            AtlasEntityKind.Hex => observation.Geography.Hexes.OrderBy(hex => hex.HexId, StringComparer.Ordinal)
                .Select(hex => new NavigationItem(new AtlasEntityKey(AtlasEntityKind.Hex, hex.HexId), $"Hex {hex.HexId}",
                    hex.HasCoordinates ? $"q={hex.Q}, r={hex.R} · {hex.TerrainId}" : "Coordinates unknown · unplaced"))
                .ToImmutableArray(),
            AtlasEntityKind.Location => observation.Geography.Locations.OrderBy(location => location.LocationId, StringComparer.Ordinal)
                .Select(location => new NavigationItem(new AtlasEntityKey(AtlasEntityKind.Location, location.LocationId),
                    $"Location {location.LocationId}", $"Anchor {location.AnchorHexId}"))
                .ToImmutableArray(),
            AtlasEntityKind.City => observation.Cities.Rows.OrderBy(city => city.CityId, StringComparer.Ordinal)
                .Select(city => new NavigationItem(new AtlasEntityKey(AtlasEntityKind.City, city.CityId),
                    city.Label is { Length: > 0 } ? city.Label : $"City {city.CityId}",
                    city.P8LocationId is null ? "No binding recorded" : $"Location {city.P8LocationId}"))
                .ToImmutableArray(),
            _ => ImmutableArray<NavigationItem>.Empty
        };
    }

    public static string? ResolveAnchorHexId(ObservationDocument observation, AtlasEntityKey key)
    {
        ArgumentNullException.ThrowIfNull(observation);
        return key.Kind switch
        {
            AtlasEntityKind.Hex when observation.Geography.Hexes.Any(hex => StringComparer.Ordinal.Equals(hex.HexId, key.SourceId)) => key.SourceId,
            AtlasEntityKind.Location => observation.Geography.Locations
                .FirstOrDefault(location => StringComparer.Ordinal.Equals(location.LocationId, key.SourceId))?.AnchorHexId,
            AtlasEntityKind.City => ResolveCityAnchor(observation, key.SourceId),
            _ => null
        };
    }

    public static HexFact? GetHex(ObservationDocument observation, string hexId) =>
        observation.Geography.Hexes.FirstOrDefault(hex => StringComparer.Ordinal.Equals(hex.HexId, hexId));

    public static LocationFact? GetLocation(ObservationDocument observation, string locationId) =>
        observation.Geography.Locations.FirstOrDefault(location => StringComparer.Ordinal.Equals(location.LocationId, locationId));

    public static CityFact? GetCity(ObservationDocument observation, string cityId) =>
        observation.Cities.Rows.FirstOrDefault(city => StringComparer.Ordinal.Equals(city.CityId, cityId));

    private static string? ResolveCityAnchor(ObservationDocument observation, string cityId)
    {
        var city = GetCity(observation, cityId);
        if (city?.P8LocationId is null)
        {
            return null;
        }

        return GetLocation(observation, city.P8LocationId)?.AnchorHexId;
    }
}
