using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Raster;
using FluxionDrawAndAnimate.Core.Tiling;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Raster;

public class LayerTileRasterizerTests
{
    private const int TileSize = 64;

    [Fact]
    public void Rebuild_creates_tile_for_layer_stroke()
    {
        var grid = new TileGrid(128, 128, TileSize);
        var pool = new TileBufferPool(TileSize * TileSize * 4);
        using var rasterizer = new LayerTileRasterizer(grid, pool);
        var layer = new AnimationLayer("Layer", frameCount: 1);
        var stroke = new StrokePath(RgbaColor.FromRgb(255, 0, 0), 10, ToolKind.Pencil, false);
        stroke.Points.Add(new PaintInformation(32, 32, 1, 0, 0, 0, 0));
        layer.Frames[0].Strokes.Add(stroke);
        var rebuilt = new List<TileCoordinate>();

        rasterizer.MarkDirty(StrokeBounds.Calculate(stroke));
        rasterizer.Rebuild(layer, frameIndex: 0, rebuilt);

        Assert.Contains(new TileCoordinate(0, 0), rebuilt);
        Assert.True(rasterizer.Surface.TryGetTile(new TileCoordinate(0, 0), out var tile));
        Assert.True(tile[GetPixelOffset(32, 32) + 3] > 0);
        Assert.False(rasterizer.HasDirtyTiles);
    }

    [Fact]
    public void Rebuild_removes_tile_when_strokes_no_longer_touch_it()
    {
        var grid = new TileGrid(128, 128, TileSize);
        var pool = new TileBufferPool(TileSize * TileSize * 4);
        using var rasterizer = new LayerTileRasterizer(grid, pool);
        var layer = new AnimationLayer("Layer", frameCount: 1);
        var stroke = new StrokePath(RgbaColor.Black, 10, ToolKind.Pencil, false);
        stroke.Points.Add(new PaintInformation(32, 32, 1, 0, 0, 0, 0));
        layer.Frames[0].Strokes.Add(stroke);
        var rebuilt = new List<TileCoordinate>();

        rasterizer.MarkDirty(StrokeBounds.Calculate(stroke));
        rasterizer.Rebuild(layer, 0, rebuilt);
        Assert.Equal(1, rasterizer.Surface.TileCount);

        layer.Frames[0].Strokes.Clear();
        rebuilt.Clear();
        rasterizer.MarkDirty(new Core.Geometry.DocumentRect(0, 0, TileSize, TileSize));
        rasterizer.Rebuild(layer, 0, rebuilt);

        Assert.Contains(new TileCoordinate(0, 0), rebuilt);
        Assert.False(rasterizer.Surface.TryGetTile(new TileCoordinate(0, 0), out _));
        Assert.Equal(0, rasterizer.Surface.TileCount);
        Assert.True(pool.FreeBufferCount > 0);
    }

    private static int GetPixelOffset(int x, int y) => (y * TileSize + x) * 4;
}
