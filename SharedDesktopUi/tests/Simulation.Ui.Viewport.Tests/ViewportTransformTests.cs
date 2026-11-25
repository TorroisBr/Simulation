using NUnit.Framework;
using Simulation.Ui.Viewport;

namespace Simulation.Ui.Viewport.Tests;

[TestFixture]
public sealed class ViewportTransformTests
{
    [TestCase(ViewportYDirection.Down, 310.9375)]
    [TestCase(ViewportYDirection.Up, 306.5625)]
    public void TryProject_UsesOriginCenterPanScaleAndAxisDirection(
        ViewportYDirection direction,
        double expectedY)
    {
        var transform = new ViewportTransform(
            new ViewportPoint(10.25, -4.5),
            new ViewportPoint(500.25, 300),
            new ViewportPoint(-2.5, 8.75),
            1.25,
            direction);

        Assert.That(transform.TryProject(new ViewportPoint(12.5, -2.75), out var projected), Is.True);
        Assert.That(projected.X, Is.EqualTo(500.5625));
        Assert.That(projected.Y, Is.EqualTo(expectedY));
    }

    [TestCase(ViewportYDirection.Down, 310.9375)]
    [TestCase(ViewportYDirection.Up, 306.5625)]
    public void TryUnproject_InvertsBothAxisDirections(
        ViewportYDirection direction,
        double viewportY)
    {
        var transform = new ViewportTransform(
            new ViewportPoint(10.25, -4.5),
            new ViewportPoint(500.25, 300),
            new ViewportPoint(-2.5, 8.75),
            1.25,
            direction);

        Assert.That(transform.TryUnproject(new ViewportPoint(500.5625, viewportY), out var local), Is.True);
        Assert.That(local.X, Is.EqualTo(12.5));
        Assert.That(local.Y, Is.EqualTo(-2.75));
    }

    [TestCase(ViewportYDirection.Down)]
    [TestCase(ViewportYDirection.Up)]
    public void TryProjectAndTryUnproject_RoundTripFractionalCoordinates(ViewportYDirection direction)
    {
        var transform = new ViewportTransform(
            new ViewportPoint(-123.75, 85.125),
            new ViewportPoint(713.5, 409.25),
            new ViewportPoint(-87.125, 15.75),
            0.0375,
            direction);
        var original = new ViewportPoint(-102.375, 96.8125);

        Assert.That(transform.TryProject(original, out var projected), Is.True);
        Assert.That(transform.TryUnproject(projected, out var roundTrip), Is.True);
        Assert.That(transform.TryProject(roundTrip, out var reprojected), Is.True);
        Assert.That(reprojected.X, Is.EqualTo(projected.X).Within(1e-7));
        Assert.That(reprojected.Y, Is.EqualTo(projected.Y).Within(1e-7));
    }

    [TestCase(1e-150, 1e-150, 1e-300)]
    [TestCase(1e150, 1e-150, 1.0)]
    public void TryProjectAndTryUnproject_SupportSmallAndLargeFiniteScales(
        double localOffset,
        double scale,
        double expectedViewportOffset)
    {
        var transform = new ViewportTransform(
            new ViewportPoint(0, 0),
            new ViewportPoint(0, 0),
            new ViewportPoint(0, 0),
            scale,
            ViewportYDirection.Down);

        Assert.That(transform.TryProject(new ViewportPoint(localOffset, localOffset), out var projected), Is.True);
        Assert.That(projected.X, Is.EqualTo(expectedViewportOffset));
        Assert.That(projected.Y, Is.EqualTo(expectedViewportOffset));
        Assert.That(transform.TryUnproject(projected, out var roundTrip), Is.True);
        Assert.That(roundTrip.X, Is.EqualTo(localOffset).Within(Math.Abs(localOffset) * 1e-14));
        Assert.That(roundTrip.Y, Is.EqualTo(localOffset).Within(Math.Abs(localOffset) * 1e-14));
    }

    [TestCase(ViewportYDirection.Down)]
    [TestCase(ViewportYDirection.Up)]
    public void TryProjectAndTryUnproject_SupportExtremeFiniteCoordinates(ViewportYDirection direction)
    {
        var quarterMax = double.MaxValue / 4;
        var transform = new ViewportTransform(
            new ViewportPoint(-quarterMax, -quarterMax),
            new ViewportPoint(0, 0),
            new ViewportPoint(0, 0),
            1e-300,
            direction);

        Assert.That(transform.TryProject(new ViewportPoint(quarterMax, quarterMax), out var projected), Is.True);
        Assert.That(double.IsFinite(projected.X), Is.True);
        Assert.That(double.IsFinite(projected.Y), Is.True);
        Assert.That(transform.TryUnproject(projected, out var local), Is.True);
        Assert.That(local, Is.EqualTo(new ViewportPoint(quarterMax, quarterMax)));
    }

    [TestCase(ViewportYDirection.Down)]
    [TestCase(ViewportYDirection.Up)]
    public void TryReanchor_PreservesThePointBeneathTheAnchor(ViewportYDirection direction)
    {
        var original = new ViewportTransform(
            new ViewportPoint(17.25, -38.5),
            new ViewportPoint(640.25, 360.5),
            new ViewportPoint(-34.75, 12.125),
            0.002,
            direction);
        var anchor = new ViewportPoint(89d / 3, 211.75);

        Assert.That(original.TryUnproject(anchor, out var anchoredWorld), Is.True);
        Assert.That(original.TryReanchor(1.75, anchor, out var reanchored), Is.True);
        Assert.That(reanchored.Scale, Is.EqualTo(1.75));
        Assert.That(reanchored.LocalOrigin, Is.EqualTo(original.LocalOrigin));
        Assert.That(reanchored.ViewportCenter, Is.EqualTo(original.ViewportCenter));
        Assert.That(reanchored.YDirection, Is.EqualTo(original.YDirection));
        Assert.That(reanchored.TryProject(anchoredWorld, out var projectedAnchor), Is.True);
        Assert.That(projectedAnchor.X, Is.EqualTo(anchor.X).Within(1e-7));
        Assert.That(projectedAnchor.Y, Is.EqualTo(anchor.Y).Within(1e-7));
    }

    [Test]
    public void TryReanchor_RecalculatesPanAtEqualScaleAndPreservesHalfCentimetreSnap()
    {
        var transform = new ViewportTransform(
            new ViewportPoint(0, 0),
            new ViewportPoint(500, 0),
            new ViewportPoint(200.0005, 0),
            0.002,
            ViewportYDirection.Down);
        var anchor = new ViewportPoint(89d / 3, 0);

        Assert.That(transform.TryReanchor(0.002, anchor, out var reanchored), Is.True);
        Assert.That(reanchored.Scale, Is.EqualTo(transform.Scale));
        Assert.That(reanchored.Pan.X, Is.Not.EqualTo(transform.Pan.X));

        Assert.That(reanchored.TryUnproject(new ViewportPoint(700, 0), out var clickWorld), Is.True);
        Assert.That(Math.Round(clickWorld.X / 0.5), Is.EqualTo(-1));

        Assert.That(transform.TryUnproject(new ViewportPoint(700, 0), out var skippedReanchorWorld), Is.True);
        Assert.That(Math.Round(skippedReanchorWorld.X / 0.5), Is.EqualTo(0));
    }

    [Test]
    public void TryReanchor_PreservesTheOtherCameraComponents()
    {
        var original = new ViewportTransform(
            new ViewportPoint(-7, 12),
            new ViewportPoint(500, 400),
            new ViewportPoint(3, -5),
            0.25,
            ViewportYDirection.Up);

        Assert.That(original.TryReanchor(0.5, new ViewportPoint(215.75, 120.125), out var updated), Is.True);
        Assert.That(updated.LocalOrigin, Is.EqualTo(original.LocalOrigin));
        Assert.That(updated.ViewportCenter, Is.EqualTo(original.ViewportCenter));
        Assert.That(updated.YDirection, Is.EqualTo(original.YDirection));
        Assert.That(updated.Scale, Is.EqualTo(0.5));
        Assert.That(updated.Pan, Is.Not.EqualTo(original.Pan));
    }

    [Test]
    public void TryProjectAndTryUnproject_RejectNonfiniteCoordinatesAndInvalidState()
    {
        var valid = Transform();
        var nonfinitePoints = new[]
        {
            new ViewportPoint(double.NaN, 0),
            new ViewportPoint(0, double.NaN),
            new ViewportPoint(double.PositiveInfinity, 0),
            new ViewportPoint(double.NegativeInfinity, 0),
            new ViewportPoint(0, double.PositiveInfinity),
            new ViewportPoint(0, double.NegativeInfinity)
        };

        foreach (var point in nonfinitePoints)
        {
            Assert.That(valid.TryProject(point, out var projected), Is.False);
            Assert.That(projected, Is.EqualTo(default(ViewportPoint)));
            Assert.That(valid.TryUnproject(point, out var local), Is.False);
            Assert.That(local, Is.EqualTo(default(ViewportPoint)));
        }

        var invalidTransforms = new[]
        {
            Transform(scale: 0),
            Transform(scale: -1),
            Transform(scale: double.NaN),
            Transform(scale: double.PositiveInfinity),
            Transform(scale: double.NegativeInfinity),
            Transform(origin: new ViewportPoint(double.NegativeInfinity, 0)),
            Transform(center: new ViewportPoint(0, double.NaN)),
            Transform(pan: new ViewportPoint(0, double.PositiveInfinity)),
            Transform(direction: (ViewportYDirection)42)
        };

        foreach (var invalid in invalidTransforms)
        {
            Assert.That(invalid.TryProject(new ViewportPoint(1, 2), out var projected), Is.False);
            Assert.That(projected, Is.EqualTo(default(ViewportPoint)));
            Assert.That(invalid.TryUnproject(new ViewportPoint(1, 2), out var local), Is.False);
            Assert.That(local, Is.EqualTo(default(ViewportPoint)));
            Assert.That(invalid.TryReanchor(1, new ViewportPoint(1, 2), out var updated), Is.False);
            Assert.That(updated, Is.EqualTo(default(ViewportTransform)));
        }
    }

    [Test]
    public void TryReanchor_RejectsInvalidScaleAndAnchorWithoutPublishingTransform()
    {
        var transform = Transform();
        var invalidScales = new[] { 0d, -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity };

        foreach (var scale in invalidScales)
        {
            Assert.That(transform.TryReanchor(scale, new ViewportPoint(4, 5), out var updated), Is.False);
            Assert.That(updated, Is.EqualTo(default(ViewportTransform)));
        }

        Assert.That(transform.TryReanchor(1, new ViewportPoint(double.NaN, 5), out var invalidAnchor), Is.False);
        Assert.That(invalidAnchor, Is.EqualTo(default(ViewportTransform)));
    }

    [Test]
    public void Operations_RejectArithmeticOverflow()
    {
        var overflowProjection = Transform(
            origin: new ViewportPoint(-double.MaxValue, 0),
            scale: double.MaxValue);
        Assert.That(overflowProjection.TryProject(new ViewportPoint(double.MaxValue, 0), out var projected), Is.False);
        Assert.That(projected, Is.EqualTo(default(ViewportPoint)));

        var overflowProduct = Transform(origin: new ViewportPoint(0, 0), scale: double.MaxValue);
        Assert.That(overflowProduct.TryProject(new ViewportPoint(2, 0), out var productResult), Is.False);
        Assert.That(productResult, Is.EqualTo(default(ViewportPoint)));

        var overflowBase = Transform(
            center: new ViewportPoint(double.MaxValue, 0),
            pan: new ViewportPoint(double.MaxValue, 0));
        Assert.That(overflowBase.TryProject(new ViewportPoint(0, 0), out var baseResult), Is.False);
        Assert.That(baseResult, Is.EqualTo(default(ViewportPoint)));

        var overflowUnprojection = Transform(
            center: new ViewportPoint(-double.MaxValue, 0),
            scale: 1);
        Assert.That(overflowUnprojection.TryUnproject(new ViewportPoint(double.MaxValue, 0), out var local), Is.False);
        Assert.That(local, Is.EqualTo(default(ViewportPoint)));

        var overflowDivision = Transform(scale: double.Epsilon);
        Assert.That(overflowDivision.TryUnproject(new ViewportPoint(1, 0), out var divisionResult), Is.False);
        Assert.That(divisionResult, Is.EqualTo(default(ViewportPoint)));

        var overflowReanchor = Transform(
            origin: new ViewportPoint(0, 0),
            center: new ViewportPoint(-double.MaxValue, 0),
            scale: double.MaxValue);
        Assert.That(overflowReanchor.TryReanchor(double.MaxValue, new ViewportPoint(double.MaxValue, 0), out var updated), Is.False);
        Assert.That(updated, Is.EqualTo(default(ViewportTransform)));

        var overflowReanchoredPan = new ViewportTransform(
            new ViewportPoint(0, 0),
            new ViewportPoint(0, 0),
            new ViewportPoint(double.MaxValue * 0.75, 0),
            1,
            ViewportYDirection.Down);
        Assert.That(overflowReanchoredPan.TryReanchor(2, new ViewportPoint(double.MaxValue * 0.25, 0), out var panResult), Is.False);
        Assert.That(panResult, Is.EqualTo(default(ViewportTransform)));
    }

    [Test]
    public void TryProjectAndTryUnproject_DoNotAllocateForOrdinaryOperations()
    {
        var transform = Transform();
        var anchor = new ViewportPoint(450.25, 275.75);

        for (var index = 0; index < 1_000; index++)
        {
            _ = transform.TryProject(new ViewportPoint(index, index + 1), out _);
            _ = transform.TryUnproject(anchor, out _);
            _ = transform.TryReanchor(1.25, anchor, out _);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var sum = 0d;
        for (var index = 0; index < 10_000; index++)
        {
            var projectSucceeded = transform.TryProject(new ViewportPoint(index, index + 1), out var projected);
            var unprojectSucceeded = transform.TryUnproject(anchor, out var local);
            var reanchorSucceeded = transform.TryReanchor(1.25, anchor, out var reanchored);
            if (!projectSucceeded || !unprojectSucceeded || !reanchorSucceeded)
            {
                Assert.Fail("A finite viewport operation unexpectedly failed.");
            }

            sum += projected.X + projected.Y + local.X + local.Y + reanchored.Pan.X + reanchored.Pan.Y;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(double.IsFinite(sum), Is.True);
        Assert.That(allocated, Is.EqualTo(0));
    }

    private static ViewportTransform Transform(
        ViewportPoint? origin = null,
        ViewportPoint? center = null,
        ViewportPoint? pan = null,
        double scale = 1.25,
        ViewportYDirection direction = ViewportYDirection.Down) =>
        new(
            origin ?? new ViewportPoint(-10, 20),
            center ?? new ViewportPoint(640, 360),
            pan ?? new ViewportPoint(15, -12),
            scale,
            direction);
}
