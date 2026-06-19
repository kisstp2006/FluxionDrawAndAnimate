using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Geometry;
using FluxionDrawAndAnimate.Core.Tiling;

namespace FluxionDrawAndAnimate.Core.Raster;

/// <summary>
/// Composites one animation frame (all visible layers) into a tile-backed
/// <see cref="RasterSurface"/>. Only tiles marked dirty are rebuilt, so an
/// in-progress stroke re-rasterizes a handful of tiles instead of the canvas.
/// The vector strokes remain the source of truth; this surface is a cache.
/// </summary>
public sealed class FrameRasterizer : IDisposable
{
    private readonly TileGrid _grid;
    private readonly RasterSurface _surface;
    private readonly byte[] _layerScratch;
    private readonly HashSet<TileCoordinate> _dirty = new();

    public FrameRasterizer(TileGrid grid, TileBufferPool pool)
    {
        _grid = grid;
        _surface = new RasterSurface(grid, pool);
        _layerScratch = new byte[grid.TileSize * grid.TileSize * 4];
    }

    public RasterSurface Surface => _surface;
    public bool HasDirtyTiles => _dirty.Count > 0;

    public void MarkAllDirty()
    {
        for (var y = 0; y < _grid.Rows; y++)
        {
            for (var x = 0; x < _grid.Columns; x++)
            {
                _dirty.Add(new TileCoordinate(x, y));
            }
        }
    }

    public void MarkDirty(DocumentRect bounds)
    {
        foreach (var coordinate in _grid.GetTilesIntersecting(bounds))
        {
            _dirty.Add(coordinate);
        }
    }

    /// <summary>Rebuilds dirty tiles and reports which tiles changed (for blitting).</summary>
    public void Rebuild(DrawingProject project, int frameIndex, ICollection<TileCoordinate> rebuiltTiles)
    {
        foreach (var coordinate in _dirty)
        {
            RebuildTile(project, frameIndex, coordinate);
            rebuiltTiles.Add(coordinate);
        }

        _dirty.Clear();
    }

    private void RebuildTile(DrawingProject project, int frameIndex, TileCoordinate coordinate)
    {
        var tileSize = _grid.TileSize;
        var originX = coordinate.X * tileSize;
        var originY = coordinate.Y * tileSize;

        var composite = _surface.GetOrCreateTile(coordinate);
        Array.Clear(composite);
        var hasContent = false;

        foreach (var layer in project.Layers)
        {
            if (!layer.IsVisible || layer.Opacity <= 0 || frameIndex >= layer.Frames.Count)
            {
                continue;
            }

            var frame = layer.Frames[frameIndex];
            if (frame.Strokes.Count == 0)
            {
                continue;
            }

            Array.Clear(_layerScratch);
            var layerHasContent = false;

            foreach (var stroke in frame.Strokes)
            {
                var bounds = StrokeBounds.Calculate(stroke);
                if (!IntersectsTile(bounds, originX, originY, tileSize))
                {
                    continue;
                }

                StrokeTileRasterizer.Rasterize(_layerScratch, tileSize, originX, originY, stroke);
                layerHasContent = true;
            }

            if (!layerHasContent)
            {
                continue;
            }

            hasContent = true;
            var opacity = Math.Clamp(layer.Opacity, 0, 1);
            for (var i = 0; i < _layerScratch.Length; i += 4)
            {
                var alpha = _layerScratch[i + 3];
                if (alpha == 0)
                {
                    continue;
                }

                CompositeOp.Over(composite, i, _layerScratch[i], _layerScratch[i + 1], _layerScratch[i + 2], alpha, opacity);
            }
        }

        if (!hasContent)
        {
            _surface.RemoveTile(coordinate);
        }
    }

    private static bool IntersectsTile(DocumentRect bounds, int originX, int originY, int tileSize)
    {
        if (bounds.IsEmpty)
        {
            return false;
        }

        return !(bounds.Right < originX
                 || bounds.X > originX + tileSize
                 || bounds.Bottom < originY
                 || bounds.Y > originY + tileSize);
    }

    public void Dispose()
    {
        _surface.Dispose();
    }
}
