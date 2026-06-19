using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Raster;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Raster;

public class BrushMaskTests
{
    [Fact]
    public void Create_round_mask_has_opaque_center()
    {
        var mask = BrushMask.Create(BrushShape.Round, brushRadius: 5, hardness: 0.8);

        var center = mask.Data[mask.MaskRadius * mask.Size + mask.MaskRadius];

        Assert.Equal(255, center);
    }

    [Fact]
    public void Create_mask_size_is_twice_radius_plus_one_for_AA()
    {
        var mask = BrushMask.Create(BrushShape.Round, brushRadius: 5, hardness: 0.8);

        // maskRadius = brushRadius + 1; size = 2*maskRadius + 1.
        Assert.Equal(6, mask.MaskRadius);
        Assert.Equal(13, mask.Size);
    }

    [Fact]
    public void Create_hard_round_has_sharp_edge()
    {
        var hard = BrushMask.Create(BrushShape.HardRound, brushRadius: 5, hardness: 1.0);

        // At full hardness the center must be fully opaque.
        var center = hard.Data[hard.MaskRadius * hard.Size + hard.MaskRadius];
        Assert.Equal(255, center);
    }

    [Fact]
    public void Create_round_corners_are_fully_transparent()
    {
        var mask = BrushMask.Create(BrushShape.Round, brushRadius: 5, hardness: 0.8);

        // Corner pixel is well outside the circle → 0.
        Assert.Equal(0, mask.Data[0]);
        Assert.Equal(0, mask.Data[^1]);
    }
}

public class BrushMaskCacheTests
{
    [Fact]
    public void Get_returns_same_instance_for_same_shape_size_and_hardness()
    {
        var a = BrushMaskCache.Get(BrushShape.Round, 10.0, 0.8);
        var b = BrushMaskCache.Get(BrushShape.Round, 10.0, 0.8);

        Assert.Same(a, b);
    }

    [Fact]
    public void Get_buckets_radius_to_nearest_two_pixels()
    {
        // radius 9 and 10 both bucket to radius 10 → same mask.
        var r9 = BrushMaskCache.Get(BrushShape.Round, 9.0, 0.5);
        var r10 = BrushMaskCache.Get(BrushShape.Round, 10.0, 0.5);

        Assert.Same(r9, r10);
    }

    [Fact]
    public void Get_buckets_hardness_to_nearest_zero_dot_zero_five()
    {
        // 0.51 and 0.52 both round to hardness bucket 10 (0.50).
        var a = BrushMaskCache.Get(BrushShape.Round, 8.0, 0.51);
        var b = BrushMaskCache.Get(BrushShape.Round, 8.0, 0.52);

        Assert.Same(a, b);
    }

    [Fact]
    public void Get_returns_different_masks_for_different_shapes()
    {
        var round = BrushMaskCache.Get(BrushShape.Round, 8.0, 0.8);
        var chisel = BrushMaskCache.Get(BrushShape.Chisel, 8.0, 0.8);

        Assert.NotSame(round, chisel);
    }

    [Fact]
    public void Get_clamps_minimum_radius_bucket_to_one()
    {
        // Tiny radius must still produce a valid mask, not crash on bucket 0.
        var mask = BrushMaskCache.Get(BrushShape.Round, 0.3, 0.5);

        Assert.True(mask.Size > 0);
        Assert.True(mask.MaskRadius >= 1);
    }
}
