namespace FluxionDrawAndAnimate.Core.Tiling;

public readonly record struct TileCoordinate(int X, int Y)
{
    public override string ToString()
    {
        return $"{X},{Y}";
    }
}
