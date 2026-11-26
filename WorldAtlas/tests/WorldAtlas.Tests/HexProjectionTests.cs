using WorldAtlas.Observations.Geometry;
using WorldAtlas.Observations.Models;

namespace WorldAtlas.Tests;

[TestFixture]
public sealed class HexProjectionTests
{
    [Test]
    public void AxialNeighborsProjectAsAdjacentCellsAndHitTestingUsesStableIdentityOrder()
    {
        var hexes = new[]
        {
            new HexFact("hex-b", 1, 0, "hill", "1"),
            new HexFact("hex-a", 0, 0, "plain", "1")
        };
        var projection = new HexProjection(hexes);
        var viewport = new MapSize(800, 600);
        var cells = projection.Project(viewport, 1d, new MapPoint(0, 0));
        var first = cells.Single(cell => cell.Hex.HexId == "hex-a");
        var second = cells.Single(cell => cell.Hex.HexId == "hex-b");
        var sharedEdgeMidpoint = new MapPoint((first.Center.X + second.Center.X) / 2d, first.Center.Y);

        Assert.That(Math.Abs(second.Center.X - first.Center.X), Is.EqualTo(Math.Sqrt(3d) * HexProjection.DefaultHexSize).Within(1e-8));
        Assert.That(projection.HitTest(viewport, 1d, new MapPoint(0, 0), first.Center), Is.EqualTo("hex-a"));
        Assert.That(projection.HitTest(viewport, 1d, new MapPoint(0, 0), sharedEdgeMidpoint), Is.EqualTo("hex-a"));
    }

    [Test]
    public void PointerAnchoredZoomPanBoundsAndFitRemainFinite()
    {
        var projection = new HexProjection([
            new HexFact("hex-1", -1, 0, "plain", "1"),
            new HexFact("hex-2", 0, 0, "hill", "1")
        ]);
        var viewport = new MapSize(700, 460);
        var pointer = new MapPoint(204, 171);
        var pan = new MapPoint(42, -18);
        const double oldZoom = 1.25d;
        const double newZoom = 2.5d;
        var worldBefore = WorldAt(pointer, viewport, pan, oldZoom);
        var zoomedPan = HexProjection.ZoomAt(pan, oldZoom, newZoom, pointer, viewport);
        var worldAfter = WorldAt(pointer, viewport, zoomedPan, newZoom);
        var fit = projection.Fit(viewport);
        var fitted = projection.Project(viewport, fit.Zoom, fit.Pan);

        Assert.That(worldAfter.X, Is.EqualTo(worldBefore.X).Within(1e-10));
        Assert.That(worldAfter.Y, Is.EqualTo(worldBefore.Y).Within(1e-10));
        Assert.That(fit.Zoom, Is.InRange(HexProjection.MinimumZoom, HexProjection.MaximumZoom));
        Assert.That(fitted.SelectMany(cell => cell.Vertices).All(point => double.IsFinite(point.X) && double.IsFinite(point.Y)), Is.True);

        var content = projection.GetBounds(1d);
        var screenBounds = new MapRect(viewport.Width / 2d + content.Left, viewport.Height / 2d + content.Top,
            viewport.Width / 2d + content.Right, viewport.Height / 2d + content.Bottom);
        var clamped = HexProjection.ClampPan(new MapPoint(1e9, -1e9), screenBounds, viewport);
        var moved = projection.Project(viewport, 1d, clamped).SelectMany(cell => cell.Vertices).ToArray();
        Assert.That(moved.Max(point => point.X), Is.GreaterThanOrEqualTo(HexProjection.PanVisibilityPixels - 1e-7));
        Assert.That(moved.Min(point => point.X), Is.LessThanOrEqualTo(viewport.Width - HexProjection.PanVisibilityPixels + 1e-7));
        Assert.That(moved.Max(point => point.Y), Is.GreaterThanOrEqualTo(HexProjection.PanVisibilityPixels - 1e-7));
        Assert.That(moved.Min(point => point.Y), Is.LessThanOrEqualTo(viewport.Height - HexProjection.PanVisibilityPixels + 1e-7));
    }

    [Test]
    public void SignedAxialInt32ExtremesRecenterWithoutOverflowOrFabricatedCells()
    {
        var projection = new HexProjection([
            new HexFact("far-a", int.MinValue, int.MaxValue, "plain", "1"),
            new HexFact("far-b", int.MaxValue, int.MinValue, "hill", "1")
        ]);
        var viewport = new MapSize(900, 560);
        var fit = projection.Fit(viewport);
        var cells = projection.Project(viewport, fit.Zoom, fit.Pan);

        Assert.That(cells.Length, Is.EqualTo(2));
        Assert.That(cells.All(cell => double.IsFinite(cell.Center.X) && double.IsFinite(cell.Center.Y)), Is.True);
        Assert.That(cells.Select(cell => cell.Hex.HexId), Is.EqualTo(new[] { "far-a", "far-b" }));
        Assert.That(cells[0].Center, Is.Not.EqualTo(cells[1].Center));
        Assert.That(projection.HitTest(viewport, fit.Zoom, fit.Pan, cells[0].Center), Is.EqualTo("far-a"));
    }

    [Test]
    public void ProjectionCentersMatchAcceptedLegacyFixturesExactly()
    {
        var projection = new HexProjection([
            new HexFact("a", -2, 1, "plain", "1"),
            new HexFact("b", 3, -4, "hill", "1"),
            new HexFact("c", 0, 2, "forest", "1")
        ]);

        var cells = projection.Project(new MapSize(777.25d, 433.5d), 0.75d, new MapPoint(13.125d, -8.75d));

        Assert.That(cells.Select(cell => cell.Center), Is.EqualTo(new[]
        {
            new MapPoint(308.2192563912806d, 262d),
            new MapPoint(464.1038290724796d, -8d),
            new MapPoint(464.1038290724796d, 316d)
        }));
    }

    [Test]
    public void PointerAnchoredZoomMatchesAcceptedLegacyPanExactly()
    {
        var pan = HexProjection.ZoomAt(new MapPoint(42d, -18d), 1.25d, 2.5d,
            new MapPoint(204d, 171d), new MapSize(700d, 460d));

        Assert.That(pan, Is.EqualTo(new MapPoint(230d, 23d)));
    }

    [Test]
    public void ClampedEqualScaleZoomStillRecalculatesPanLikeTheAcceptedBaseline()
    {
        var pan = HexProjection.ZoomAt(new MapPoint(200.0005d, -31.7d), HexProjection.MinimumZoom, 0d,
            new MapPoint(89d / 3d, 71.2d), new MapSize(500d, 400d));

        Assert.That(pan, Is.EqualTo(new MapPoint(200.00050000000002d, -31.700000000000003d)));
    }

    [Test]
    public void SignedAxialInt32ExtremeCentersMatchAcceptedLegacyFixturesExactly()
    {
        var projection = new HexProjection([
            new HexFact("far-a", int.MinValue, int.MaxValue, "plain", "1"),
            new HexFact("far-b", int.MaxValue, int.MinValue, "hill", "1")
        ]);

        var cells = projection.Project(new MapSize(900d, 560d), 1e-9d, new MapPoint(0d, 0d));

        Assert.That(cells.Select(cell => cell.Center), Is.EqualTo(new[]
        {
            new MapPoint(360.7307810762062d, 434.618822584d),
            new MapPoint(539.2692187990862d, 125.38117734399998d)
        }));
        Assert.That(projection.HitTest(new MapSize(900d, 560d), 1e-9d, new MapPoint(0d, 0d), cells[0].Center),
            Is.EqualTo("far-a"));
    }

    [Test]
    public void EmptyAndUnplacedHexesDoNotProducePolygons()
    {
        var projection = new HexProjection([new HexFact("legacy", null, null, null, null)]);
        Assert.That(projection.Project(new MapSize(400, 300), 1d, new MapPoint(0, 0)), Is.Empty);
        Assert.That(projection.HitTest(new MapSize(400, 300), 1d, new MapPoint(0, 0), new MapPoint(200, 150)), Is.Null);
    }

    private static MapPoint WorldAt(MapPoint pointer, MapSize viewport, MapPoint pan, double zoom) =>
        new((pointer.X - viewport.Width / 2d - pan.X) / zoom, (pointer.Y - viewport.Height / 2d - pan.Y) / zoom);
}
