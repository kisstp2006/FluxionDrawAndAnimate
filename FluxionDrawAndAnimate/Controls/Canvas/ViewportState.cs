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
        var targetScale = ClampScale(Math.Min(availableWidth / documentSize.Width, availableHeight / documentSize.Height));

        OffsetX = 0;
        OffsetY = 0;
        Rotation = 0;
        ZoomAt(viewportSize, GetViewportCenter(viewportSize), targetScale / Scale);
    }

    public void Reset(Size viewportSize)
    {
        OffsetX = 0;
        OffsetY = 0;
        Rotation = 0;
        ZoomAt(viewportSize, GetViewportCenter(viewportSize), 1.0 / Scale);
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

    public void ZoomAt(Size viewportSize, Point screenPivot, double factor)
    {
        if (factor <= 0 || double.IsNaN(factor) || double.IsInfinity(factor))
        {
            return;
        }

        var previousScale = Scale;
        var newScale = ClampScale(previousScale * factor);
        if (Math.Abs(newScale - previousScale) < double.Epsilon)
        {
            return;
        }

        var ratio = newScale / previousScale;
        OffsetX = screenPivot.X - viewportSize.Width / 2 - (screenPivot.X - viewportSize.Width / 2 - OffsetX) * ratio;
        OffsetY = screenPivot.Y - viewportSize.Height / 2 - (screenPivot.Y - viewportSize.Height / 2 - OffsetY) * ratio;
        Scale = newScale;
    }

    public void RotateAt(Size viewportSize, Size documentSize, Point screenPivot, double radians)
    {
        if (Math.Abs(radians) < double.Epsilon)
        {
            return;
        }

        if (!TryScreenToDocumentUnclamped(viewportSize, documentSize, screenPivot, out var anchorDocumentPoint))
        {
            RotateBy(radians);
            return;
        }

        Rotation += radians;
        ReanchorDocumentPoint(viewportSize, documentSize, anchorDocumentPoint, screenPivot);
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

    private static Point GetViewportCenter(Size viewportSize) =>
        new(viewportSize.Width / 2.0, viewportSize.Height / 2.0);

    private static double ClampScale(double scale) => Math.Clamp(scale, MinScale, MaxScale);
}
