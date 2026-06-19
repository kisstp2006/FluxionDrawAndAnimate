using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Core.Raster;

/// <summary>
/// Caches pre-rendered <see cref="BrushMask"/> instances keyed by (shape,
/// size-bucket, hardness-bucket) so each unique combination is rendered once.
///
/// Size bucket:     every 2 px  (radius rounded up to nearest 2)
/// Hardness bucket: every 0.05  (20 discrete steps)
/// </summary>
public static class BrushMaskCache
{
    private static readonly Dictionary<(BrushShape, int, int), BrushMask> Cache = new();

    public static BrushMask Get(BrushShape shape, double radius, double hardness)
    {
        var radiusBucket = Math.Max(1, (int)Math.Ceiling(radius / 2.0) * 2);
        var hardnessBucket = (int)Math.Round(Math.Clamp(hardness, 0, 1) * 20);
        var key = (shape, radiusBucket, hardnessBucket);

        if (!Cache.TryGetValue(key, out var mask))
        {
            mask = BrushMask.Create(shape, radiusBucket, hardnessBucket / 20.0);
            Cache[key] = mask;
        }

        return mask;
    }
}
