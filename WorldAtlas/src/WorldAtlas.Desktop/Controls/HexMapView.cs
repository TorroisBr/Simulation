using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using WorldAtlas.Observations.Geometry;
using WorldAtlas.Observations.Models;

namespace WorldAtlas.Desktop.Controls;

public sealed class HexMapView : Control
{
    public static readonly StyledProperty<IReadOnlyList<HexFact>?> HexesProperty =
        AvaloniaProperty.Register<HexMapView, IReadOnlyList<HexFact>?>(nameof(Hexes));
    public static readonly StyledProperty<IReadOnlyList<MapMarker>?> MarkersProperty =
        AvaloniaProperty.Register<HexMapView, IReadOnlyList<MapMarker>?>(nameof(Markers));
    public static readonly StyledProperty<string?> SelectedHexIdProperty =
        AvaloniaProperty.Register<HexMapView, string?>(nameof(SelectedHexId));

    private static readonly Brush BackgroundBrush = new SolidColorBrush(Color.Parse("#121A24"));
    private static readonly Brush PlainBrush = new SolidColorBrush(Color.Parse("#405746"));
    private static readonly Brush HillBrush = new SolidColorBrush(Color.Parse("#675945"));
    private static readonly Brush UnknownBrush = new SolidColorBrush(Color.Parse("#3B4756"));
    private static readonly Brush SelectedBrush = new SolidColorBrush(Color.Parse("#426D5B"));
    private static readonly Pen HexBorderPen = new(new SolidColorBrush(Color.Parse("#8593A4")), 1.2d);
    private static readonly Pen SelectedBorderPen = new(new SolidColorBrush(Color.Parse("#B7F0D5")), 3d);
    private static readonly Pen MarkerBorderPen = new(new SolidColorBrush(Color.Parse("#E1EAF4")), 1d);
    private static readonly Typeface LabelTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);

    private HexProjection _projection = new(Array.Empty<HexFact>());
    private bool _panning;
    private Point _previousPointer;

    public HexMapView()
    {
        Focusable = true;
        ClipToBounds = true;
        SizeChanged += (_, _) =>
        {
            if (!_hasUserView)
            {
                FitToView();
            }

            InvalidateVisual();
        };
    }

    private bool _hasUserView;

    public IReadOnlyList<HexFact>? Hexes
    {
        get => GetValue(HexesProperty);
        set => SetValue(HexesProperty, value);
    }

    public IReadOnlyList<MapMarker>? Markers
    {
        get => GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    public string? SelectedHexId
    {
        get => GetValue(SelectedHexIdProperty);
        set => SetValue(SelectedHexIdProperty, value);
    }

    public double Zoom { get; private set; } = 1d;

    public MapPoint Pan { get; private set; } = new(0, 0);

    public event Action<string>? HexSelected;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(BackgroundBrush, new Rect(Bounds.Size));
        DrawGrid(context);

        var viewport = ToMapSize(Bounds.Size);
        var projected = _projection.Project(viewport, Zoom, Pan);
        foreach (var cell in projected)
        {
            var selected = StringComparer.Ordinal.Equals(SelectedHexId, cell.Hex.HexId);
            var fill = selected ? SelectedBrush : TerrainBrush(cell.Hex.TerrainId);
            var geometry = CreateHexGeometry(cell.Vertices);
            context.DrawGeometry(fill, selected ? SelectedBorderPen : HexBorderPen, geometry);
            DrawText(context, cell.Hex.HexId, cell.Center, 11, selected ? Color.Parse("#FFFFFF") : Color.Parse("#DEE6EF"));
        }

        DrawMarkers(context, projected);
        if (projected.IsEmpty)
        {
            DrawCenteredText(context, "No Hex has registered coordinates", new Point(Bounds.Width / 2d, Bounds.Height / 2d),
                Color.Parse("#AEBBCB"), 14);
            DrawCenteredText(context, "Unplaced Hexes remain available in the entity list.",
                new Point(Bounds.Width / 2d, Bounds.Height / 2d + 26d), Color.Parse("#8291A3"), 11);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HexesProperty)
        {
            _projection = new HexProjection(Hexes ?? Array.Empty<HexFact>());
            _hasUserView = false;
            FitToView();
            InvalidateVisual();
        }
        else if (change.Property == MarkersProperty || change.Property == SelectedHexIdProperty)
        {
            InvalidateVisual();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var direction = e.Key switch
        {
            Key.Left => new MapPoint(-1d, 0d),
            Key.Right => new MapPoint(1d, 0d),
            Key.Up => new MapPoint(0d, -1d),
            Key.Down => new MapPoint(0d, 1d),
            _ => (MapPoint?)null
        };

        if (direction is not { } axis)
        {
            return;
        }

        var visible = GetVisibleHexes();
        if (visible.Length == 0)
        {
            e.Handled = true;
            return;
        }

        var current = visible.FirstOrDefault(cell =>
            StringComparer.Ordinal.Equals(SelectedHexId, cell.Hex.HexId));
        if (current is null)
        {
            HexSelected?.Invoke(visible.OrderBy(cell => cell.Hex.HexId, StringComparer.Ordinal).First().Hex.HexId);
            e.Handled = true;
            return;
        }

        var next = visible
            .Where(cell => !StringComparer.Ordinal.Equals(cell.Hex.HexId, current.Hex.HexId))
            .Select(cell =>
            {
                var dx = cell.Center.X - current.Center.X;
                var dy = cell.Center.Y - current.Center.Y;
                return (Cell: cell,
                    Forward: dx * axis.X + dy * axis.Y,
                    Lateral: Math.Abs(dx * axis.Y - dy * axis.X),
                    DistanceSquared: dx * dx + dy * dy);
            })
            .Where(candidate => candidate.Forward > 1e-8d)
            .OrderBy(candidate => candidate.Lateral / candidate.Forward)
            .ThenBy(candidate => candidate.DistanceSquared)
            .ThenBy(candidate => candidate.Cell.Hex.HexId, StringComparer.Ordinal)
            .Select(candidate => candidate.Cell)
            .FirstOrDefault();

        if (next is not null)
        {
            HexSelected?.Invoke(next.Hex.HexId);
        }

        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsMiddleButtonPressed)
        {
            _panning = true;
            _previousPointer = e.GetPosition(this);
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (properties.IsLeftButtonPressed)
        {
            Focus();
            var location = e.GetPosition(this);
            var hit = _projection.HitTest(ToMapSize(Bounds.Size), Zoom, Pan, new MapPoint(location.X, location.Y));
            if (hit is not null)
            {
                HexSelected?.Invoke(hit);
            }

            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_panning)
        {
            return;
        }

        var current = e.GetPosition(this);
        PanBy(new MapPoint(current.X - _previousPointer.X, current.Y - _previousPointer.Y));
        _previousPointer = current;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_panning)
        {
            _panning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var location = e.GetPosition(this);
        ZoomAt(location, e.Delta.Y > 0 ? 1.16d : 1d / 1.16d);
        e.Handled = true;
    }

    public void FitToView()
    {
        var result = _projection.Fit(ToMapSize(Bounds.Size), 58d);
        Zoom = result.Zoom;
        Pan = result.Pan;
        _hasUserView = true;
        InvalidateVisual();
    }

    public void ZoomFromCenter(double factor)
    {
        ZoomAt(new Point(Bounds.Width / 2d, Bounds.Height / 2d), factor);
    }

    public Point PointForHex(string hexId)
    {
        var cell = _projection.Project(ToMapSize(Bounds.Size), Zoom, Pan)
            .FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.Hex.HexId, hexId));
        return cell is null ? new Point(double.NaN, double.NaN) : new Point(cell.Center.X, cell.Center.Y);
    }

    private void ZoomAt(Point pointer, double factor)
    {
        var nextZoom = HexProjection.ClampZoom(Zoom * factor);
        Pan = HexProjection.ZoomAt(Pan, Zoom, nextZoom, new MapPoint(pointer.X, pointer.Y), ToMapSize(Bounds.Size));
        Zoom = nextZoom;
        ClampPan();
        _hasUserView = true;
        InvalidateVisual();
    }

    private void PanBy(MapPoint delta)
    {
        Pan += delta;
        ClampPan();
        _hasUserView = true;
        InvalidateVisual();
    }

    private void ClampPan()
    {
        var worldBounds = _projection.GetBounds(Zoom);
        var centerX = Bounds.Width / 2d;
        var centerY = Bounds.Height / 2d;
        var screenBounds = new MapRect(centerX + worldBounds.Left, centerY + worldBounds.Top,
            centerX + worldBounds.Right, centerY + worldBounds.Bottom);
        Pan = HexProjection.ClampPan(Pan, screenBounds, ToMapSize(Bounds.Size));
    }

    private ProjectedHex[] GetVisibleHexes()
    {
        var viewport = ToMapSize(Bounds.Size);
        return _projection.Project(viewport, Zoom, Pan)
            .Where(cell => cell.Vertices.Max(point => point.X) >= 0d
                && cell.Vertices.Min(point => point.X) <= viewport.Width
                && cell.Vertices.Max(point => point.Y) >= 0d
                && cell.Vertices.Min(point => point.Y) <= viewport.Height)
            .ToArray();
    }

    private void DrawMarkers(DrawingContext context, IReadOnlyList<ProjectedHex> projected)
    {
        if (Markers is null || Markers.Count == 0)
        {
            return;
        }

        foreach (var marker in Markers.OrderBy(marker => marker.AnchorHexId, StringComparer.Ordinal)
                     .ThenBy(marker => marker.OffsetOrder).ThenBy(marker => marker.Id, StringComparer.Ordinal))
        {
            var cell = projected.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.Hex.HexId, marker.AnchorHexId));
            if (cell is null)
            {
                continue;
            }

            var offset = new Point(HexProjection.DefaultHexSize * Zoom * 0.66d,
                -HexProjection.DefaultHexSize * Zoom * 0.67d - marker.OffsetOrder * 17d);
            var point = new Point(cell.Center.X + offset.X, cell.Center.Y + offset.Y);
            var color = marker.Kind == MapMarkerKind.Location ? Color.Parse("#CFD9E8") : Color.Parse("#E7C77B");
            context.DrawEllipse(new SolidColorBrush(color), MarkerBorderPen, point, 8d, 8d);
            DrawText(context, marker.Kind == MapMarkerKind.Location ? "L" : "C", new MapPoint(point.X, point.Y), 9,
                Color.Parse("#17202A"));
        }
    }

    private static Geometry CreateHexGeometry(System.Collections.Immutable.ImmutableArray<MapPoint> vertices)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(vertices[0].X, vertices[0].Y), isFilled: true);
            for (var index = 1; index < vertices.Length; index++)
            {
                context.LineTo(new Point(vertices[index].X, vertices[index].Y));
            }

            context.EndFigure(isClosed: true);
        }

        return geometry;
    }

    private static Brush TerrainBrush(string? terrainId) => terrainId switch
    {
        "plain" => PlainBrush,
        "hill" => HillBrush,
        _ => UnknownBrush
    };

    private static void DrawGrid(DrawingContext context)
    {
        var brush = new SolidColorBrush(Color.Parse("#1D2835"));
        for (var x = 24d; x < 4096d; x += 36d)
        {
            if (x >= 0 && x < 4096)
            {
                context.DrawLine(new Pen(brush, 0.6d), new Point(x, 0), new Point(x, 4096));
            }
        }

        for (var y = 24d; y < 4096d; y += 36d)
        {
            if (y >= 0 && y < 4096)
            {
                context.DrawLine(new Pen(brush, 0.6d), new Point(0, y), new Point(4096, y));
            }
        }
    }

    private static void DrawText(DrawingContext context, string value, MapPoint center, double size, Color color)
    {
        var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, LabelTypeface, size,
            new SolidColorBrush(color));
        context.DrawText(text, new Point(center.X - text.Width / 2d, center.Y - text.Height / 2d));
    }

    private static void DrawCenteredText(DrawingContext context, string value, Point center, Color color, double size)
    {
        var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, LabelTypeface, size,
            new SolidColorBrush(color));
        context.DrawText(text, new Point(center.X - text.Width / 2d, center.Y - text.Height / 2d));
    }

    private static MapSize ToMapSize(Size size) => new(size.Width, size.Height);
}
