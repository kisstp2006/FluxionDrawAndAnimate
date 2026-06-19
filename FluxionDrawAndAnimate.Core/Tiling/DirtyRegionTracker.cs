using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Geometry;

namespace FluxionDrawAndAnimate.Core.Tiling;

public sealed class DirtyRegionTracker
{
    private readonly HashSet<TileCoordinate> _dirtyTiles = new();

    public DirtyRegionTracker(TileGrid grid)
    {
        Grid = grid;
    }

    public TileGrid Grid { get; }
    public int DirtyTileCount => _dirtyTiles.Count;

    public void MarkDirty(DocumentRect bounds)
    {
        foreach (var tile in Grid.GetTilesIntersecting(bounds))
        {
            _dirtyTiles.Add(tile);
        }
    }

    public void MarkDirty(StrokePath stroke)
    {
        MarkDirty(StrokeBounds.Calculate(stroke));
    }

    public DirtyTileSnapshot Snapshot()
    {
        return new DirtyTileSnapshot(_dirtyTiles.OrderBy(tile => tile.Y).ThenBy(tile => tile.X).ToArray());
    }

    public DirtyTileSnapshot SnapshotAndClear()
    {
        var snapshot = Snapshot();
        _dirtyTiles.Clear();
        return snapshot;
    }

    public void Clear()
    {
        _dirtyTiles.Clear();
    }
}
