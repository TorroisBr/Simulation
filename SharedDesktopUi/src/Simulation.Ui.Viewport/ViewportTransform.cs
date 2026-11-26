namespace Simulation.Ui.Viewport;

/// <summary>Names the direction in which local positive Y is projected on screen.</summary>
public enum ViewportYDirection
{
    /// <summary>Increasing local Y moves toward increasing viewport Y.</summary>
    Down = 0,

    /// <summary>Increasing local Y moves toward decreasing viewport Y.</summary>
    Up = 1
}

/// <summary>A two-dimensional point represented with double-precision coordinates.</summary>
public readonly record struct ViewportPoint(double X, double Y);

/// <summary>
/// An immutable mapping between local world coordinates and viewport DIPs.
/// Scale is measured in DIPs per local unit.
/// </summary>
public readonly record struct ViewportTransform(
    ViewportPoint LocalOrigin,
    ViewportPoint ViewportCenter,
    ViewportPoint Pan,
    double Scale,
    ViewportYDirection YDirection)
{
    /// <summary>Projects a local world point into viewport DIPs.</summary>
    /// <param name="localPoint">The point in local world coordinates.</param>
    /// <param name="viewportPoint">The projected point when this method returns true.</param>
    /// <returns>True when the transform, input, and projected result are finite and valid.</returns>
    public bool TryProject(ViewportPoint localPoint, out ViewportPoint viewportPoint)
    {
        viewportPoint = default;
        if (!IsValid || !IsFinite(localPoint))
        {
            return false;
        }

        var xBase = ViewportCenter.X + Pan.X;
        if (!double.IsFinite(xBase))
        {
            return false;
        }

        var xDelta = localPoint.X - LocalOrigin.X;
        if (!double.IsFinite(xDelta))
        {
            return false;
        }

        var xOffset = xDelta * Scale;
        if (!double.IsFinite(xOffset))
        {
            return false;
        }

        var x = xBase + xOffset;
        if (!double.IsFinite(x))
        {
            return false;
        }

        var yBase = ViewportCenter.Y + Pan.Y;
        if (!double.IsFinite(yBase))
        {
            return false;
        }

        var yDelta = localPoint.Y - LocalOrigin.Y;
        if (!double.IsFinite(yDelta))
        {
            return false;
        }

        var yOffset = yDelta * Scale;
        if (!double.IsFinite(yOffset))
        {
            return false;
        }

        double y;
        if (YDirection == ViewportYDirection.Down)
        {
            y = yBase + yOffset;
        }
        else
        {
            y = yBase - yOffset;
        }

        if (!double.IsFinite(y))
        {
            return false;
        }

        viewportPoint = new ViewportPoint(x, y);
        return true;
    }

    /// <summary>Unprojects a viewport DIP point into local world coordinates.</summary>
    /// <param name="viewportPoint">The point in viewport DIPs.</param>
    /// <param name="localPoint">The unprojected point when this method returns true.</param>
    /// <returns>True when the transform, input, and unprojected result are finite and valid.</returns>
    public bool TryUnproject(ViewportPoint viewportPoint, out ViewportPoint localPoint)
    {
        localPoint = default;
        if (!IsValid || !IsFinite(viewportPoint))
        {
            return false;
        }

        var xFromCenter = viewportPoint.X - ViewportCenter.X;
        if (!double.IsFinite(xFromCenter))
        {
            return false;
        }

        var xFromPan = xFromCenter - Pan.X;
        if (!double.IsFinite(xFromPan))
        {
            return false;
        }

        var xOffset = xFromPan / Scale;
        if (!double.IsFinite(xOffset))
        {
            return false;
        }

        var x = xOffset + LocalOrigin.X;
        if (!double.IsFinite(x))
        {
            return false;
        }

        var yFromCenter = viewportPoint.Y - ViewportCenter.Y;
        if (!double.IsFinite(yFromCenter))
        {
            return false;
        }

        var yFromPan = yFromCenter - Pan.Y;
        if (!double.IsFinite(yFromPan))
        {
            return false;
        }

        var yOffset = yFromPan / Scale;
        if (!double.IsFinite(yOffset))
        {
            return false;
        }

        if (YDirection == ViewportYDirection.Up)
        {
            yOffset = -yOffset;
        }

        var y = yOffset + LocalOrigin.Y;
        if (!double.IsFinite(y))
        {
            return false;
        }

        localPoint = new ViewportPoint(x, y);
        return true;
    }

    /// <summary>
    /// Changes the scale while preserving the local point beneath a viewport DIP anchor.
    /// The pan equations are evaluated even when <paramref name="newScale"/> equals the current scale.
    /// </summary>
    /// <param name="newScale">The requested positive scale in DIPs per local unit.</param>
    /// <param name="anchor">The pointer anchor in viewport DIPs.</param>
    /// <param name="transform">The updated transform when this method returns true.</param>
    /// <returns>True when the input and updated transform are finite and valid.</returns>
    public bool TryReanchor(double newScale, ViewportPoint anchor, out ViewportTransform transform)
    {
        transform = default;
        if (!IsValid || !double.IsFinite(newScale) || newScale <= 0 || !IsFinite(anchor))
        {
            return false;
        }

        if (!TryUnproject(anchor, out var anchoredLocalPoint))
        {
            return false;
        }

        var xDelta = anchoredLocalPoint.X - LocalOrigin.X;
        if (!double.IsFinite(xDelta))
        {
            return false;
        }

        var xOffset = xDelta * newScale;
        if (!double.IsFinite(xOffset))
        {
            return false;
        }

        var anchorXFromCenter = anchor.X - ViewportCenter.X;
        if (!double.IsFinite(anchorXFromCenter))
        {
            return false;
        }

        var panX = anchorXFromCenter - xOffset;
        if (!double.IsFinite(panX))
        {
            return false;
        }

        var yDelta = anchoredLocalPoint.Y - LocalOrigin.Y;
        if (!double.IsFinite(yDelta))
        {
            return false;
        }

        var yOffset = yDelta * newScale;
        if (!double.IsFinite(yOffset))
        {
            return false;
        }

        if (YDirection == ViewportYDirection.Up)
        {
            yOffset = -yOffset;
        }

        var anchorYFromCenter = anchor.Y - ViewportCenter.Y;
        if (!double.IsFinite(anchorYFromCenter))
        {
            return false;
        }

        var panY = anchorYFromCenter - yOffset;
        if (!double.IsFinite(panY))
        {
            return false;
        }

        transform = new ViewportTransform(
            LocalOrigin,
            ViewportCenter,
            new ViewportPoint(panX, panY),
            newScale,
            YDirection);
        return true;
    }

    private bool IsValid =>
        IsFinite(LocalOrigin) &&
        IsFinite(ViewportCenter) &&
        IsFinite(Pan) &&
        double.IsFinite(Scale) &&
        Scale > 0 &&
        YDirection is ViewportYDirection.Down or ViewportYDirection.Up;

    private static bool IsFinite(ViewportPoint point) =>
        double.IsFinite(point.X) && double.IsFinite(point.Y);
}
