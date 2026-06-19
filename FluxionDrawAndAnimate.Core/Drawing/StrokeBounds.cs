using FluxionDrawAndAnimate.Core.Geometry;

namespace FluxionDrawAndAnimate.Core.Drawing;

public static class StrokeBounds
{
    public static DocumentRect Calculate(StrokePath stroke)
    {
        if (stroke.Points.Count == 0)
        {
            return DocumentRect.Empty;
        }

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;

        foreach (var point in stroke.Points)
        {
            if (point.X < minX) minX = point.X;
            if (point.Y < minY) minY = point.Y;
            if (point.X > maxX) maxX = point.X;
            if (point.Y > maxY) maxY = point.Y;
        }

        return new DocumentRect(minX, minY, maxX - minX, maxY - minY)
            .Inflate(Math.Max(1, stroke.Size / 2));
    }
}
