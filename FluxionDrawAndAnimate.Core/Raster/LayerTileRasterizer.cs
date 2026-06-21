using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Geometry;
using FluxionDrawAndAnimate.Core.Tiling;

namespace FluxionDrawAndAnimate.Core.Raster;

/// <summary>
/// Rasterizes one layer/frame pair into a tile-backed surface. The layer remains
/// the source of truth; this class only caches pixels for tiles that have been
/// visited by the renderer.
/// </summary>
public sealed class LayerTileRasterizer : IDisposable
{
    private readonly TileGrid _grid;
    private readonly RasterSurface _surface;
    private readonly HashSet<TileCoordinate> _dirty = new();
    private readonly HashSet<TileCoordinate> _visited = new();

    public LayerTileRasterizer(TileGrid grid, TileBufferPool pool)
    {
        _grid = grid;
        _surface = new RasterSurface(grid, pool);
    }

    public RasterSurface Surface => _surface;
    public bool HasDirtyTiles => _dirty.Count > 0;

    public bool HasVisitedTile(TileCoordinate coordinate) => _visited.Contains(coordinate);

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

    public void MarkDirty(TileCoordinate coordinate)
    {
        _dirty.Add(coordinate);
    }

    public void Rebuild(AnimationLayer layer, int frameIndex, ICollection<TileCoordinate> rebuiltTiles)
    {
        foreach (var coordinate in _dirty)
        {
            RebuildTileCore(layer, frameIndex, coordinate);
            rebuiltTiles.Add(coordinate);
        }

        _dirty.Clear();
    }

    public void RasterizeStrokeRange(
        AnimationLayer layer,
        int frameIndex,
        TileCoordinate coordinate,
        StrokePath stroke,
        int firstPointIndex)
    {
        if (EnsureTileFresh(layer, frameIndex, coordinate))
        {
            return;
        }

        var tileSize = _grid.TileSize;
        var tile = _surface.GetOrCreateTile(coordinate);
        StrokeTileRasterizer.RasterizeRange(
            tile,
            tileSize,
            coordinate.X * tileSize,
            coordinate.Y * tileSize,
            stroke,
            firstPointIndex);

        if (IsTransparent(tile))
        {
            _surface.RemoveTile(coordinate);
        }
    }

    private bool EnsureTileFresh(AnimationLayer layer, int frameIndex, TileCoordinate coordinate)
    {
        if (_visited.Contains(coordinate) && !_dirty.Contains(coordinate))
        {
            return false;
        }

        _dirty.Remove(coordinate);
        RebuildTileCore(layer, frameIndex, coordinate);
        return true;
    }

    private void RebuildTileCore(AnimationLayer layer, int frameIndex, TileCoordinate coordinate)
    {
        _visited.Add(coordinate);

        if (frameIndex < 0 || frameIndex >= layer.Frames.Count)
        {
            _surface.RemoveTile(coordinate);
            return;
        }

        var frame = layer.Frames[frameIndex];
        if (frame.Strokes.Count == 0)
        {
            _surface.RemoveTile(coordinate);
            return;
        }

        var tileSize = _grid.TileSize;
        var originX = coordinate.X * tileSize;
        var originY = coordinate.Y * tileSize;
        var tile = _surface.GetOrCreateTile(coordinate);
        Array.Clear(tile);

        var hasContent = false;
        foreach (var stroke in frame.Strokes)
        {
            var bounds = StrokeBounds.Calculate(stroke);
            if (!IntersectsTile(bounds, originX, originY, tileSize))
            {
                continue;
            }

            StrokeTileRasterizer.Rasterize(tile, tileSize, originX, originY, stroke);
            hasContent = true;
        }

        if (!hasContent || IsTransparent(tile))
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

    private static bool IsTransparent(byte[] tile)
    {
        for (var i = 3; i < tile.Length; i += 4)
        {
            if (tile[i] != 0)
            {
                return false;
            }
        }

        return true;
    }

    public void Dispose()
    {
        _surface.Dispose();
        _dirty.Clear();
        _visited.Clear();
    }
}
