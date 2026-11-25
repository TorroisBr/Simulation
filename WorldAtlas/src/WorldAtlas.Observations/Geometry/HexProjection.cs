using System.Collections.Immutable;
using WorldAtlas.Observations.Models;

namespace WorldAtlas.Observations.Geometry;

public readonly record struct MapPoint(double X, double Y)
{
    public static MapPoint operator +(MapPoint left, MapPoint right) => new(left.X + right.X, left.Y + right.Y);
    public static MapPoint operator -(MapPoint left, MapPoint right) => new(left.X - right.X, left.Y - right.Y);
    public static MapPoint operator *(MapPoint point, double scale) => new(point.X * scale, point.Y * scale);
}

public readonly record struct MapSize(double Width, double Height);

public readonly record struct HexFitResult(double Zoom, MapPoint Pan);

public readonly record struct MapRect(double Left, double Top, double Right, double Bottom)
{
    public double Width => Right - Left;
    public double Height => Bottom - Top;
    public bool Contains(MapPoint point) => point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
}

public sealed record ProjectedHex(HexFact Hex, MapPoint Center, ImmutableArray<MapPoint> Vertices);

/// <summary>Projection-local axial geometry with long recentering before double screen transforms.</summary>
public sealed class HexProjection
{
    public const double DefaultHexSize = 48d;
    public const double MinimumZoom = 1e-9d;
    public const double MaximumZoom = 5.0d;
    public const double PanVisibilityPixels = 54d;

    private readonly ImmutableArray<HexFact> _hexes;
    private readonly long _originQ;
    private readonly long _originR;

    public HexProjection(IEnumerable<HexFact> hexes, double hexSize = DefaultHexSize)
    {
        ArgumentNullException.ThrowIfNull(hexes);
        if (!double.IsFinite(hexSize) || hexSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hexSize));
        }

        HexSize = hexSize;
        _hexes = hexes.Where(hex => hex.HasCoordinates)
            .OrderBy(hex => hex.HexId, StringComparer.Ordinal)
            .ToImmutableArray();

        if (!_hexes.IsEmpty)
        {
            var qSum = _hexes.Sum(hex => (long)hex.Q!.Value);
            var rSum = _hexes.Sum(hex => (long)hex.R!.Value);
            _originQ = qSum / _hexes.Length;
            _originR = rSum / _hexes.Length;
        }
    }

    public double HexSize { get; }

    public ImmutableArray<HexFact> PlacedHexes => _hexes;

    public ImmutableArray<ProjectedHex> Project(MapSize viewport, double zoom, MapPoint pan)
    {
        if (viewport.Width <= 0 || viewport.Height <= 0 || _hexes.IsEmpty)
        {
            return ImmutableArray<ProjectedHex>.Empty;
        }

        var scale = ClampZoom(zoom);
        var center = new MapPoint(viewport.Width / 2d + pan.X, viewport.Height / 2d + pan.Y);
        var builder = ImmutableArray.CreateBuilder<ProjectedHex>(_hexes.Length);
        foreach (var hex in _hexes)
        {
            var axialX = (long)hex.Q!.Value - _originQ;
            var axialR = (long)hex.R!.Value - _originR;
            var worldX = HexSize * Math.Sqrt(3d) * (axialX + axialR / 2d);
            var worldY = HexSize * 1.5d * axialR;
            var cellCenter = center + new MapPoint(worldX * scale, worldY * scale);
            var radius = HexSize * scale;
            var vertices = ImmutableArray.CreateBuilder<MapPoint>(6);
            for (var vertex = 0; vertex < 6; vertex++)
            {
                var angle = (Math.PI / 180d) * (30d + 60d * vertex);
                vertices.Add(new MapPoint(cellCenter.X + radius * Math.Cos(angle), cellCenter.Y + radius * Math.Sin(angle)));
            }

            builder.Add(new ProjectedHex(hex, cellCenter, vertices.MoveToImmutable()));
        }

        return builder.MoveToImmutable();
    }

    public string? HitTest(MapSize viewport, double zoom, MapPoint pan, MapPoint point)
    {
        foreach (var projected in Project(viewport, zoom, pan))
        {
            if (Contains(projected.Vertices, point))
            {
                return projected.Hex.HexId;
            }
        }

        return null;
    }

    public MapRect GetBounds(double zoom = 1d)
    {
        if (_hexes.IsEmpty)
        {
            return new MapRect(0, 0, 0, 0);
        }

        var scale = ClampZoom(zoom);
        var centers = new List<MapPoint>(_hexes.Length);
        foreach (var hex in _hexes)
        {
            var dq = (long)hex.Q!.Value - _originQ;
            var dr = (long)hex.R!.Value - _originR;
            centers.Add(new MapPoint(HexSize * Math.Sqrt(3d) * (dq + dr / 2d) * scale,
                HexSize * 1.5d * dr * scale));
        }

        var halfWidth = HexSize * Math.Sqrt(3d) / 2d * scale;
        var halfHeight = HexSize * scale;
        return new MapRect(centers.Min(point => point.X) - halfWidth, centers.Min(point => point.Y) - halfHeight,
            centers.Max(point => point.X) + halfWidth, centers.Max(point => point.Y) + halfHeight);
    }

    public HexFitResult Fit(MapSize viewport, double padding = 40d)
    {
        if (_hexes.IsEmpty || viewport.Width <= 0 || viewport.Height <= 0)
        {
            return new HexFitResult(1d, new MapPoint(0d, 0d));
        }

        var bounds = GetBounds(1d);
        var availableWidth = Math.Max(1d, viewport.Width - padding * 2d);
        var availableHeight = Math.Max(1d, viewport.Height - padding * 2d);
        var zoom = ClampZoom(Math.Min(availableWidth / Math.Max(1d, bounds.Width), availableHeight / Math.Max(1d, bounds.Height)));
        return new HexFitResult(zoom, new MapPoint(0d, 0d));
    }

    public static double ClampZoom(double zoom)
    {
        if (!double.IsFinite(zoom))
        {
            return 1d;
        }

        return Math.Clamp(zoom, MinimumZoom, MaximumZoom);
    }

    public static MapPoint ZoomAt(MapPoint pan, double currentZoom, double requestedZoom, MapPoint pointer, MapSize viewport)
    {
        var oldZoom = ClampZoom(currentZoom);
        var newZoom = ClampZoom(requestedZoom);
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return pan;
        }

        var center = new MapPoint(viewport.Width / 2d, viewport.Height / 2d);
        var worldAnchor = new MapPoint((pointer.X - center.X - pan.X) / oldZoom, (pointer.Y - center.Y - pan.Y) / oldZoom);
        return new MapPoint(pointer.X - center.X - worldAnchor.X * newZoom,
            pointer.Y - center.Y - worldAnchor.Y * newZoom);
    }

    public static MapPoint ClampPan(MapPoint pan, MapRect contentAtOrigin, MapSize viewport,
        double minimumVisiblePixels = PanVisibilityPixels)
    {
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return new MapPoint(0d, 0d);
        }

        var xMin = minimumVisiblePixels - contentAtOrigin.Right;
        var xMax = viewport.Width - minimumVisiblePixels - contentAtOrigin.Left;
        var yMin = minimumVisiblePixels - contentAtOrigin.Bottom;
        var yMax = viewport.Height - minimumVisiblePixels - contentAtOrigin.Top;
        return new MapPoint(Math.Clamp(pan.X, Math.Min(xMin, xMax), Math.Max(xMin, xMax)),
            Math.Clamp(pan.Y, Math.Min(yMin, yMax), Math.Max(yMin, yMax)));
    }

    private static bool Contains(ImmutableArray<MapPoint> polygon, MapPoint point)
    {
        var inside = false;
        for (int current = 0, previous = polygon.Length - 1; current < polygon.Length; previous = current++)
        {
            var a = polygon[current];
            var b = polygon[previous];
            if (DistanceToSegment(point, a, b) < 1e-7d)
            {
                return true;
            }

            var crosses = (a.Y > point.Y) != (b.Y > point.Y) &&
                          point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X;
            if (crosses)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static double DistanceToSegment(MapPoint point, MapPoint start, MapPoint end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0d)
        {
            return Math.Sqrt(Math.Pow(point.X - start.X, 2d) + Math.Pow(point.Y - start.Y, 2d));
        }

        var t = Math.Clamp(((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared, 0d, 1d);
        var nearest = new MapPoint(start.X + t * dx, start.Y + t * dy);
        return Math.Sqrt(Math.Pow(point.X - nearest.X, 2d) + Math.Pow(point.Y - nearest.Y, 2d));
    }
}
