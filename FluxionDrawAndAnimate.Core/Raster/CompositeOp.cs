namespace FluxionDrawAndAnimate.Core.Raster;

/// <summary>
/// Pixel blending. Phase A ships straight-alpha "source over" (Normal); further
/// modes (Erase, Multiply, ...) are added in later phases behind this same entry
/// point. Channel order agnostic: indices 0/1/2 are colour, index 3 is alpha,
/// so the same code works for BGRA or RGBA tiles.
/// </summary>
public static class CompositeOp
{
    /// <summary>
    /// Blends a source colour over the destination pixel at <paramref name="offset"/>.
    /// <paramref name="coverage"/> (0..1) scales the source alpha — used for
    /// antialiased dab edges and layer opacity.
    /// </summary>
    public static void Over(byte[] dst, int offset, byte c0, byte c1, byte c2, byte srcAlpha, double coverage)
    {
        var sa = srcAlpha / 255.0 * coverage;
        if (sa <= 0)
        {
            return;
        }

        if (sa > 1)
        {
            sa = 1;
        }

        var da = dst[offset + 3] / 255.0;
        var outA = sa + da * (1 - sa);
        if (outA <= 0)
        {
            dst[offset] = dst[offset + 1] = dst[offset + 2] = dst[offset + 3] = 0;
            return;
        }

        var inv = da * (1 - sa) / outA;
        var saOut = sa / outA;

        dst[offset] = (byte)(c0 * saOut + dst[offset] * inv + 0.5);
        dst[offset + 1] = (byte)(c1 * saOut + dst[offset + 1] * inv + 0.5);
        dst[offset + 2] = (byte)(c2 * saOut + dst[offset + 2] * inv + 0.5);
        dst[offset + 3] = (byte)(outA * 255 + 0.5);
    }

    /// <summary>
    /// Destination-out erase: removes destination alpha by <paramref name="coverage"/>
    /// (0..1), leaving colour channels intact. This is a real eraser — it makes
    /// pixels transparent so lower layers / the paper show through.
    /// </summary>
    public static void Erase(byte[] dst, int offset, double coverage)
    {
        if (coverage <= 0)
        {
            return;
        }

        if (coverage > 1)
        {
            coverage = 1;
        }

        dst[offset + 3] = (byte)(dst[offset + 3] * (1 - coverage) + 0.5);
    }
}
