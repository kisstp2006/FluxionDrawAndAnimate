using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Core.Raster;

/// <summary>
/// Pre-rendered grayscale alpha mask for one (shape, radius, hardness) combination.
/// Created once and reused across all dabs with the same parameters.
/// Data is stored row-major; value 255 = fully opaque, 0 = transparent.
/// </summary>
public sealed class BrushMask
{
    private BrushMask(int maskRadius, byte[] data)
    {
        MaskRadius = maskRadius;
        Data = data;
    }

    /// <summary>Half the mask size; dab centre is at (MaskRadius, MaskRadius).</summary>
    public int MaskRadius { get; }
    public int Size => MaskRadius * 2 + 1;
    public byte[] Data { get; }

    public static BrushMask Create(BrushShape shape, int brushRadius, double hardness)
    {
        // Extend by 1 px beyond the brush edge for AA
        var maskRadius = brushRadius + 1;
        var size = maskRadius * 2 + 1;
        var data = new byte[size * size];

        for (var py = 0; py < size; py++)
        {
            var dy = py - maskRadius + 0.5; // pixel centre relative to dab centre

            for (var px = 0; px < size; px++)
            {
                var dx = px - maskRadius + 0.5;

                var coverage = shape switch
                {
                    BrushShape.HardRound => RoundCoverage(dx, dy, brushRadius, 1.0),
                    BrushShape.Chisel    => ChiselCoverage(dx, dy, brushRadius, hardness),
                    _                    => RoundCoverage(dx, dy, brushRadius, hardness),
                };

                data[py * size + px] = (byte)(Math.Clamp(coverage, 0, 1) * 255 + 0.5);
            }
        }

        return new BrushMask(maskRadius, data);
    }

    // ------------------------------------------------------------------

    private static double RoundCoverage(double dx, double dy, double radius, double hardness)
    {
        var d = Math.Sqrt(dx * dx + dy * dy);
        if (d >= radius + 1) return 0;               // fully outside + 1-px AA zone
        if (d >= radius) return radius + 1 - d;      // 1-px anti-alias fringe

        // Interior: linear ramp from softStart to radius
        var softStart = radius * hardness;
        if (d <= softStart) return 1.0;
        return (radius - d) / Math.Max(0.001, radius - softStart);
    }

    private static double ChiselCoverage(double dx, double dy, double radius, double hardness)
    {
        const double Angle = Math.PI / 4;   // 45°
        const double Aspect = 0.25;         // short/long axis ratio

        var cos = Math.Cos(-Angle);
        var sin = Math.Sin(-Angle);
        var rx = cos * dx - sin * dy;
        var ry = sin * dx + cos * dy;

        // Normalised ellipse distance (0 at centre, 1 at edge)
        var nd = Math.Sqrt((rx / radius) * (rx / radius)
                         + (ry / (radius * Aspect)) * (ry / (radius * Aspect)));

        if (nd >= 1.0 + 1.0 / radius) return 0;
        if (nd >= 1.0) return Math.Max(0, 1.0 + 1.0 / radius - nd) * radius; // AA fringe

        var softStart = hardness;
        if (nd <= softStart) return 1.0;
        return (1.0 - nd) / Math.Max(0.001, 1.0 - softStart);
    }
}
