using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Raster;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Raster;

public class StrokeTileRasterizerTests
{
    private const int TileSize = 64;

    // Tile is BGRA in memory (matches the Skia-friendly byte layout the compositor uses).
    private static byte[] NewBlankTile() => new byte[TileSize * TileSize * 4];

    private static (int b, int g, int r, int a) GetPixel(byte[] tile, int x, int y)
    {
        var i = (y * TileSize + x) * 4;
        return (tile[i], tile[i + 1], tile[i + 2], tile[i + 3]);
    }

    [Fact]
    public void Rasterize_noop_for_empty_stroke()
    {
        var tile = NewBlankTile();
        var stroke = new StrokePath(RgbaColor.Black, 8, ToolKind.Pencil, isVector: false);

        StrokeTileRasterizer.Rasterize(tile, TileSize, 0, 0, stroke);

        // Every pixel remains transparent black.
        Assert.All(tile, b => Assert.Equal(0, b));
    }

    [Fact]
    public void Rasterize_single_point_stamps_a_dab_at_the_center()
    {
        var tile = NewBlankTile();
        var stroke = new StrokePath(RgbaColor.FromRgb(255, 0, 0), 10, ToolKind.Pencil, false);
        stroke.Points.Add(new PaintInformation(32, 32, 1, 0, 0, 0, 0));

        StrokeTileRasterizer.Rasterize(tile, TileSize, 0, 0, stroke);

        var center = GetPixel(tile, 32, 32);
        // Red stroke, fully opaque at full pressure. Tile stores BGRA → B=0, G=0, R=255, A>0.
        Assert.Equal(0, center.b);
        Assert.Equal(0, center.g);
        Assert.Equal(255, center.r);
        Assert.True(center.a > 0, "Center pixel should be non-transparent after a stamp");
    }

    [Fact]
    public void Rasterize_connects_two_points_with_dabs_along_the_path()
    {
        var tile = NewBlankTile();
        var stroke = new StrokePath(RgbaColor.FromRgb(0, 0, 255), 8, ToolKind.Ink, false);
        stroke.Points.Add(new PaintInformation(10, 32, 1, 0, 0, 0, 0));
        stroke.Points.Add(new PaintInformation(50, 32, 1, 0, 0, 0, 0));

        StrokeTileRasterizer.Rasterize(tile, TileSize, 0, 0, stroke);

        // The midpoint between the two endpoints should also be painted.
        var mid = GetPixel(tile, 30, 32);
        // Blue ink → BGRA B=255, G=0, R=0.
        Assert.Equal(255, mid.b);
        Assert.Equal(0, mid.r);
        Assert.True(mid.a > 0, "Midpoint should be painted between two connected points");
    }

    [Fact]
    public void Rasterize_eraser_clears_existing_paint()
    {
        var tile = NewBlankTile();
        // Pre-paint a red dab.
        var red = new StrokePath(RgbaColor.FromRgb(255, 0, 0), 12, ToolKind.Pencil, false);
        red.Points.Add(new PaintInformation(32, 32, 1, 0, 0, 0, 0));
        StrokeTileRasterizer.Rasterize(tile, TileSize, 0, 0, red);

        // Erase at the same spot.
        var eraser = new StrokePath(RgbaColor.White, 12, ToolKind.Eraser, false);
        eraser.Points.Add(new PaintInformation(32, 32, 1, 0, 0, 0, 0));
        StrokeTileRasterizer.Rasterize(tile, TileSize, 0, 0, eraser);

        var center = GetPixel(tile, 32, 32);
        Assert.Equal(0, center.a); // erased → fully transparent
    }

    [Fact]
    public void Rasterize_zero_pressure_still_paints_at_minimum_radius()
    {
        var tile = NewBlankTile();
        var stroke = new StrokePath(RgbaColor.Black, 20, ToolKind.Pencil, false);
        // Pressure 0 — the dynamics floor keeps a minimum radius (0.25 * baseRadius).
        stroke.Points.Add(new PaintInformation(32, 32, 0, 0, 0, 0, 0));

        StrokeTileRasterizer.Rasterize(tile, TileSize, 0, 0, stroke);

        var center = GetPixel(tile, 32, 32);
        Assert.True(center.a > 0, "Zero-pressure dab should still paint at the minimum radius floor");
    }

    [Fact]
    public void Rasterize_uses_brush_settings_shape_from_preset()
    {
        var tile = NewBlankTile();
        var settings = new BrushSettings
        {
            Shape = BrushShape.HardRound,
            Hardness = 1.0,
            Dynamics = BrushDynamics.Ink()
        };
        var stroke = new StrokePath(RgbaColor.Black, 10, ToolKind.Ink, false, settings);
        stroke.Points.Add(new PaintInformation(32, 32, 1, 0, 0, 0, 0));

        StrokeTileRasterizer.Rasterize(tile, TileSize, 0, 0, stroke);

        // A hard round dab at full pressure should have a sharp, opaque center.
        var center = GetPixel(tile, 32, 32);
        Assert.Equal(255, center.a); // hard + full pressure → fully opaque center
    }

    [Fact]
    public void RasterizeRange_paints_only_from_the_requested_segment()
    {
        var tile = NewBlankTile();
        var stroke = new StrokePath(RgbaColor.FromRgb(0, 0, 255), 4, ToolKind.Ink, false);
        stroke.Points.Add(new PaintInformation(10, 32, 1, 0, 0, 0, 0));
        stroke.Points.Add(new PaintInformation(20, 32, 1, 0, 0, 0, 0));
        stroke.Points.Add(new PaintInformation(50, 32, 1, 0, 0, 0, 0));

        StrokeTileRasterizer.RasterizeRange(tile, TileSize, 0, 0, stroke, firstPointIndex: 2);

        Assert.Equal(0, GetPixel(tile, 10, 32).a);
        Assert.True(GetPixel(tile, 35, 32).a > 0);
    }
}
