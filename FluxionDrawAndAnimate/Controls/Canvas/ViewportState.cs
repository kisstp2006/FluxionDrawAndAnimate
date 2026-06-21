using System;
using Avalonia;

namespace FluxionDrawAndAnimate.Controls.Canvas;

/// <summary>
/// Holds the canvas viewport transform independently from the rendering control.
/// Document pixels stay logical; this object only describes how they are viewed.
/// </summary>
internal sealed class ViewportState
{
    private const double MinScale = 0.05;
    private const double MaxScale = 32.0;

    public double Scale { get; private set; } = 1.0;
    public double OffsetX { get; private set; }
    public double OffsetY { get; private set; }
    public double Rotation { get; private set; }

    public void FitToView(Size viewportSize, Size documentSize, double padding = 40)
    {
        if (viewportSize.Width <= 0 || viewportSize.Height <= 0 ||
            documentSize.Width <= 0 || documentSize.Height <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(1, viewportSize.Width - padding);
        var availableHeight = Math.Max(1, viewportSize.Height - padding);
        Scale = ClampScale(Math.Min(availableWidth / documentSize.Width, availableHeight / documentSize.Height));
        OffsetX = 0;
        OffsetY = 0;
        Rotation = 0;
    }

    public void Reset()
    {
        Scale = 1.0;
        OffsetX = 0;
        OffsetY = 0;
        Rotation = 0;
    }

    public void PanBy(double deltaX, double deltaY)
    {
        OffsetX += deltaX;
        OffsetY += deltaY;
    }

    public void RotateBy(double radians)
    {
        Rotation += radians;
    }

    public void ZoomAt(Size viewportSize, Point pivot, double factor)
    {
        var previousScale = Scale;
        var newScale = ClampScale(previousScale * factor);
        if (Math.Abs(newScale - previousScale) < double.Epsilon)
        {
            return;
        }

        var ratio = newScale / previousScale;
        OffsetX = pivot.X - viewportSize.Width / 2 - (pivot.X - viewportSize.Width / 2 - OffsetX) * ratio;
        OffsetY = pivot.Y - viewportSize.Height / 2 - (pivot.Y - viewportSize.Height / 2 - OffsetY) * ratio;
        Scale = newScale;
    }

    public Matrix BuildMatrix(Size viewportSize, Size documentSize)
    {
        return Matrix.CreateTranslation(-documentSize.Width / 2.0, -documentSize.Height / 2.0)
             * Matrix.CreateScale(Scale, Scale)
             * Matrix.CreateRotation(Rotation)
             * Matrix.CreateTranslation(viewportSize.Width / 2 + OffsetX, viewportSize.Height / 2 + OffsetY);
    }

    public bool TryScreenToDocument(Size viewportSize, Size documentSize, Point screen, out Point document)
    {
        if (!TryScreenToDocumentUnclamped(viewportSize, documentSize, screen, out document))
        {
            return false;
        }

        return document.X >= 0 && document.X <= documentSize.Width &&
               document.Y >= 0 && document.Y <= documentSize.Height;
    }

    public bool TryScreenToDocumentUnclamped(Size viewportSize, Size documentSize, Point screen, out Point document)
    {
        document = default;
        try
        {
            var inverse = BuildMatrix(viewportSize, documentSize).Invert();
            document = ApplyMatrix(inverse, screen);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ViewportSnapshot Capture() => new(Scale, OffsetX, OffsetY, Rotation);

    public void ApplyGesture(
        Size viewportSize,
        Size documentSize,
        Point anchorDocumentPoint,
        Point startCenter,
        Point currentCenter,
        double startDistance,
        double currentDistance,
        double startAngle,
        double currentAngle,
        ViewportSnapshot startViewport,
        double zoomDeadzone,
        double panDeadzone)
    {
        var distanceDelta = Math.Abs(currentDistance - startDistance);
        Scale = distanceDelta > zoomDeadzone && startDistance > 0
            ? ClampScale(startViewport.Scale * currentDistance / startDistance)
            : startViewport.Scale;

        var centerDeltaX = currentCenter.X - startCenter.X;
        var centerDeltaY = currentCenter.Y - startCenter.Y;
        var centerDrift = Math.Sqrt(centerDeltaX * centerDeltaX + centerDeltaY * centerDeltaY);
        var targetCenter = centerDrift > panDeadzone ? currentCenter : startCenter;

        Rotation = startViewport.Rotation + (currentAngle - startAngle);
        ReanchorDocumentPoint(viewportSize, documentSize, anchorDocumentPoint, targetCenter);
    }

    private void ReanchorDocumentPoint(
        Size viewportSize,
        Size documentSize,
        Point documentPoint,
        Point screenPoint)
    {
        var x = (documentPoint.X - documentSize.Width / 2.0) * Scale;
        var y = (documentPoint.Y - documentSize.Height / 2.0) * Scale;
        var cos = Math.Cos(Rotation);
        var sin = Math.Sin(Rotation);
        var rotatedX = x * cos - y * sin;
        var rotatedY = x * sin + y * cos;

        OffsetX = screenPoint.X - viewportSize.Width / 2.0 - rotatedX;
        OffsetY = screenPoint.Y - viewportSize.Height / 2.0 - rotatedY;
    }

    private static Point ApplyMatrix(Matrix matrix, Point point) =>
        new(point.X * matrix.M11 + point.Y * matrix.M21 + matrix.M31,
            point.X * matrix.M12 + point.Y * matrix.M22 + matrix.M32);

    private static double ClampScale(double scale) => Math.Clamp(scale, MinScale, MaxScale);
}

internal readonly record struct ViewportSnapshot(
    double Scale,
    double OffsetX,
    double OffsetY,
    double Rotation);
