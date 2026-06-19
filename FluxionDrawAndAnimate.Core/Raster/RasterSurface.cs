using FluxionDrawAndAnimate.Core.Tiling;

namespace FluxionDrawAndAnimate.Core.Raster;

/// <summary>
/// A tile-backed RGBA pixel surface for one canvas. Tiles are allocated lazily
/// (only where there is content) and come from a shared <see cref="TileBufferPool"/>.
/// Tiles are stored in BGRA order to match the on-screen bitmap, avoiding a
/// per-pixel channel swap on the hot blit path.
/// </summary>
public sealed class RasterSurface : IDisposable
{
    private readonly TileBufferPool _pool;
    private readonly Dictionary<TileCoordinate, byte[]> _tiles = new();

    public RasterSurface(TileGrid grid, TileBufferPool pool)
    {
        Grid = grid;
        _pool = pool;
    }

    public TileGrid Grid { get; }
    public int TileSize => Grid.TileSize;

    public byte[] GetOrCreateTile(TileCoordinate coordinate)
    {
        if (!_tiles.TryGetValue(coordinate, out var buffer))
        {
            buffer = _pool.Rent();
            _tiles[coordinate] = buffer;
        }

        return buffer;
    }

    public bool TryGetTile(TileCoordinate coordinate, out byte[] buffer)
    {
        return _tiles.TryGetValue(coordinate, out buffer!);
    }

    public void RemoveTile(TileCoordinate coordinate)
    {
        if (_tiles.Remove(coordinate, out var buffer))
        {
            _pool.Return(buffer);
        }
    }

    public void Clear()
    {
        foreach (var buffer in _tiles.Values)
        {
            _pool.Return(buffer);
        }

        _tiles.Clear();
    }

    public void Dispose()
    {
        Clear();
    }
}
