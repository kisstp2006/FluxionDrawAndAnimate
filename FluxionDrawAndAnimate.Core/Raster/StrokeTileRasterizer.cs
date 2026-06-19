using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Core.Raster;

// Strokes are rasterized into the layer scratch buffer in order, so an eraser
// stroke (destination-out) correctly removes pixels painted by earlier strokes
// on the same layer before that layer is composited onto the frame.
//
// B/2: BrushMask gives each tool a distinct shape (Round / HardRound / Chisel).
// B/3: BrushDynamics.Evaluate() maps sensor values through response curves →
//      per-dab radius and opacity, making every stroke feel alive.

public static class StrokeTileRasterizer
{
    public static void Rasterize(byte[] tile, int tileSize, int originX, int originY, StrokePath stroke)
    {
        var pointCount = stroke.Points.Count;
        if (pointCount == 0)
        {
            return;
        }

        var baseRadius = Math.Max(0.5, stroke.Size / 2.0);
        var isEraser  = stroke.ToolKind == ToolKind.Eraser;
        var shape     = stroke.BrushSettings.Shape;
        var hardness  = stroke.BrushSettings.Hardness;
        var dynamics  = stroke.BrushSettings.Dynamics;

        if (pointCount == 1)
        {
            var p   = stroke.Points[0];
            var dab = dynamics.Evaluate(p, baseRadius);
            var msk = BrushMaskCache.Get(shape, dab.Radius, hardness);
            StampMask(tile, tileSize, originX, originY, p.X, p.Y, msk, stroke.Color, isEraser, dab.Opacity);
            return;
        }

        var minSpacing = Math.Max(0.75, baseRadius * 0.4);

        for (var i = 1; i < pointCount; i++)
        {
            var from = stroke.Points[i - 1];
            var to   = stroke.Points[i];
            var dx   = to.X - from.X;
            var dy   = to.Y - from.Y;
            var len  = Math.Sqrt(dx * dx + dy * dy);
            var steps = Math.Max(1, (int)(len / minSpacing));

            for (var step = 0; step <= steps; step++)
            {
                var t = (double)step / steps;

                // Interpolate all sensor channels between consecutive raw samples
                var info = new PaintInformation(
                    from.X + dx * t,
                    from.Y + dy * t,
                    from.Pressure + (to.Pressure - from.Pressure) * t,
                    from.TiltX    + (to.TiltX    - from.TiltX)    * t,
                    from.TiltY    + (to.TiltY    - from.TiltY)    * t,
                    from.Rotation + (to.Rotation - from.Rotation) * t,
                    from.TimestampMs + (to.TimestampMs - from.TimestampMs) * t);

                var dab = dynamics.Evaluate(info, baseRadius);
                var msk = BrushMaskCache.Get(shape, dab.Radius, hardness);
                StampMask(tile, tileSize, originX, originY, info.X, info.Y, msk, stroke.Color, isEraser, dab.Opacity);
            }
        }
    }

    private static void StampMask(
        byte[] tile,
        int tileSize,
        int originX,
        int originY,
        double centerX,
        double centerY,
        BrushMask mask,
        RgbaColor color,
        bool isEraser,
        double opacity)
    {
        var mr       = mask.MaskRadius;
        var maskSize = mask.Size;

        var tileMinX = Math.Max(0, (int)Math.Floor(centerX - mr) - originX);
        var tileMaxX = Math.Min(tileSize - 1, (int)Math.Ceiling(centerX + mr) - originX);
        var tileMinY = Math.Max(0, (int)Math.Floor(centerY - mr) - originY);
        var tileMaxY = Math.Min(tileSize - 1, (int)Math.Ceiling(centerY + mr) - originY);

        for (var py = tileMinY; py <= tileMaxY; py++)
        {
            var maskY = (int)Math.Round(originY + py + 0.5 - centerY) + mr;
            if ((uint)maskY >= (uint)maskSize)
            {
                continue;
            }

            var maskRowBase = maskY * maskSize;
            var tileRowBase = py * tileSize;

            for (var px = tileMinX; px <= tileMaxX; px++)
            {
                var maskX = (int)Math.Round(originX + px + 0.5 - centerX) + mr;
                if ((uint)maskX >= (uint)maskSize)
                {
                    continue;
                }

                var alpha = mask.Data[maskRowBase + maskX];
                if (alpha == 0)
                {
                    continue;
                }

                var coverage   = alpha / 255.0 * opacity;
                var tileOffset = (tileRowBase + px) * 4;

                if (isEraser)
                {
                    CompositeOp.Erase(tile, tileOffset, coverage);
                }
                else
                {
                    CompositeOp.Over(tile, tileOffset, color.B, color.G, color.R, color.A, coverage);
                }
            }
        }
    }
}
