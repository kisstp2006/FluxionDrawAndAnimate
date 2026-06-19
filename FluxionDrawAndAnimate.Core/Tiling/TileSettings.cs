namespace FluxionDrawAndAnimate.Core.Tiling;

public sealed class TileSettings
{
    public int TileSize { get; set; } = 256;
    public int MemoryBudgetMegabytes { get; set; } = 128;
    public bool UseProxyPlayback { get; set; } = true;

    public TileSettings Normalize()
    {
        TileSize = Math.Clamp(TileSize, 64, 1024);
        MemoryBudgetMegabytes = Math.Clamp(MemoryBudgetMegabytes, 32, 2048);
        return this;
    }
}
