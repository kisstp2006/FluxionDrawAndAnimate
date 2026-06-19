namespace FluxionDrawAndAnimate.Core.Tiling;

public sealed class DirtyTileSnapshot
{
    public DirtyTileSnapshot(IReadOnlyList<TileCoordinate> tiles)
    {
        Tiles = tiles;
    }

    public IReadOnlyList<TileCoordinate> Tiles { get; }
    public int Count => Tiles.Count;
}
