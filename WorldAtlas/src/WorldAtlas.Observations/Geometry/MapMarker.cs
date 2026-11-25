namespace WorldAtlas.Observations.Geometry;

public enum MapMarkerKind
{
    Location,
    City
}

public sealed record MapMarker(string Id, string Label, string AnchorHexId, MapMarkerKind Kind, int OffsetOrder);
