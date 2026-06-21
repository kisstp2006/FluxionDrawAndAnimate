using FluxionDrawAndAnimate.Core.Geometry;

namespace FluxionDrawAndAnimate.Core.Drawing;

public static class StrokeBounds
{
    public static DocumentRect Calculate(StrokePath stroke)
    {
        return CalculateRange(stroke, 0);
    }

    public static DocumentRect CalculateRange(StrokePath stroke, int firstPointIndex)
    {
        if (stroke.Points.Count == 0)
        {
            return DocumentRect.Empty;
        }

        var start = Math.Clamp(firstPointIndex, 0, stroke.Points.Count - 1);
        if (start > 0)
        {
            start--;
        }

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;

        for (var i = start; i < stroke.Points.Count; i++)
        {
            var point = stroke.Points[i];
            if (point.X < minX) minX = point.X;
            if (point.Y < minY) minY = point.Y;
            if (point.X > maxX) maxX = point.X;
            if (point.Y > maxY) maxY = point.Y;
        }

        return new DocumentRect(
                minX,
                minY,
                Math.Max(1, maxX - minX),
                Math.Max(1, maxY - minY))
            .Inflate(Math.Max(1, stroke.Size / 2));
    }
}
