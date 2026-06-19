using FluxionDrawAndAnimate.Core.Geometry;

namespace FluxionDrawAndAnimate.Core.Tiling;

public sealed class TileGrid
{
    public TileGrid(int canvasWidth, int canvasHeight, int tileSize)
    {
        if (canvasWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(canvasWidth));
        }

        if (canvasHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(canvasHeight));
        }

        if (tileSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tileSize));
        }

        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;
        TileSize = tileSize;
    }

    public int CanvasWidth { get; }
    public int CanvasHeight { get; }
    public int TileSize { get; }
    public int Columns => (CanvasWidth + TileSize - 1) / TileSize;
    public int Rows => (CanvasHeight + TileSize - 1) / TileSize;

    public DocumentRect GetTileBounds(TileCoordinate coordinate)
    {
        var x = coordinate.X * TileSize;
        var y = coordinate.Y * TileSize;
        var width = Math.Min(TileSize, CanvasWidth - x);
        var height = Math.Min(TileSize, CanvasHeight - y);
        return new DocumentRect(x, y, width, height);
    }

    public IEnumerable<TileCoordinate> GetTilesIntersecting(DocumentRect bounds)
    {
        var clamped = bounds.Clamp(CanvasWidth, CanvasHeight);
        if (clamped.IsEmpty)
        {
            yield break;
        }

        var minX = Math.Clamp((int)Math.Floor(clamped.X / TileSize), 0, Columns - 1);
        var minY = Math.Clamp((int)Math.Floor(clamped.Y / TileSize), 0, Rows - 1);
        var maxX = Math.Clamp((int)Math.Floor((clamped.Right - 0.001) / TileSize), 0, Columns - 1);
        var maxY = Math.Clamp((int)Math.Floor((clamped.Bottom - 0.001) / TileSize), 0, Rows - 1);

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                yield return new TileCoordinate(x, y);
            }
        }
    }
}
