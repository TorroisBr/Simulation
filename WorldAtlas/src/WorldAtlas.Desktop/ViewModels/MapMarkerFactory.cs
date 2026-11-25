using System.Collections.Immutable;
using WorldAtlas.Observations.Geometry;
using WorldAtlas.Observations.Models;
using WorldAtlas.Observations.Navigation;

namespace WorldAtlas.Desktop.ViewModels;

public static class MapMarkerFactory
{
    public static ImmutableArray<MapMarker> Build(ObservationDocument observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        var markers = new List<MapMarker>();
        foreach (var location in observation.Geography.Locations)
        {
            markers.Add(new MapMarker(location.LocationId, $"L · {location.LocationId}", location.AnchorHexId,
                MapMarkerKind.Location, 0));
        }

        foreach (var city in observation.Cities.Rows.Where(item => item.P8LocationId is not null))
        {
            var anchor = WorldNavigator.ResolveAnchorHexId(observation, new AtlasEntityKey(AtlasEntityKind.City, city.CityId));
            if (anchor is not null)
            {
                markers.Add(new MapMarker(city.CityId, $"C · {city.Label ?? city.CityId}", anchor,
                    MapMarkerKind.City, 0));
            }
        }

        var offsetsByAnchor = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = ImmutableArray.CreateBuilder<MapMarker>(markers.Count);
        foreach (var marker in markers.OrderBy(item => item.AnchorHexId, StringComparer.Ordinal)
                     .ThenBy(item => item.Kind).ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            offsetsByAnchor.TryGetValue(marker.AnchorHexId, out var offset);
            result.Add(marker with { OffsetOrder = offset });
            offsetsByAnchor[marker.AnchorHexId] = checked(offset + 1);
        }

        return result.MoveToImmutable();
    }
}
