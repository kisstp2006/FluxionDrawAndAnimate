using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Raster;
using FluxionDrawAndAnimate.Core.Tiling;

namespace FluxionDrawAndAnimate.Rendering;

/// <summary>
/// Bridges the pure-C# raster engine to Avalonia. Keeps one composited
/// <see cref="WriteableBitmap"/> per visited frame and, on each update, copies
/// only the dirty tiles into the bitmap so an in-progress stroke stays cheap.
/// </summary>
public sealed class RasterFrameCache : IDisposable
{
    private sealed class FrameEntry
    {
        public required FrameRasterizer Rasterizer { get; init; }
        public required WriteableBitmap Bitmap { get; init; }
        public bool Initialized { get; set; }
    }

    private readonly Dictionary<int, FrameEntry> _frames = new();
    private readonly List<TileCoordinate> _rebuiltScratch = new();
    private DrawingProject? _project;
    private TileGrid? _grid;
    private TileBufferPool? _pool;
    private byte[] _zeroRow = Array.Empty<byte>();

    public void Configure(DrawingProject project)
    {
        if (ReferenceEquals(_project, project))
        {
            return;
        }

        DisposeFrames();

        var tileSettings = project.TileSettings.Normalize();
        _project = project;
        _grid = new TileGrid(project.Width, project.Height, tileSettings.TileSize);
        _pool = new TileBufferPool(tileSettings.TileSize * tileSettings.TileSize * 4);
        _zeroRow = new byte[tileSettings.TileSize * 4];
    }

    public void InvalidateStroke(int frameIndex, StrokePath stroke)
    {
        if (_frames.TryGetValue(frameIndex, out var entry) && entry.Initialized)
        {
            entry.Rasterizer.MarkDirty(StrokeBounds.Calculate(stroke));
        }
    }

    public void InvalidateFrame(int frameIndex)
    {
        if (_frames.TryGetValue(frameIndex, out var entry))
        {
            entry.Rasterizer.MarkAllDirty();
        }
    }

    /// <summary>Returns a bitmap for the frame, rebuilding any dirty tiles first.</summary>
    public WriteableBitmap? GetFrameBitmap(int frameIndex)
    {
        if (_project is null || _grid is null || _pool is null || frameIndex < 0)
        {
            return null;
        }

        var entry = GetOrCreateEntry(frameIndex);

        if (!entry.Initialized)
        {
            entry.Rasterizer.MarkAllDirty();
            entry.Initialized = true;
        }

        if (entry.Rasterizer.HasDirtyTiles)
        {
            _rebuiltScratch.Clear();
            entry.Rasterizer.Rebuild(_project, frameIndex, _rebuiltScratch);
            BlitTiles(entry, _rebuiltScratch);
        }

        return entry.Bitmap;
    }

    private FrameEntry GetOrCreateEntry(int frameIndex)
    {
        if (_frames.TryGetValue(frameIndex, out var entry))
        {
            return entry;
        }

        entry = new FrameEntry
        {
            Rasterizer = new FrameRasterizer(_grid!, _pool!),
            Bitmap = new WriteableBitmap(
                new PixelSize(_project!.Width, _project.Height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul)
        };

        _frames[frameIndex] = entry;
        return entry;
    }

    private void BlitTiles(FrameEntry entry, IReadOnlyList<TileCoordinate> tiles)
    {
        if (tiles.Count == 0)
        {
            return;
        }

        var grid = _grid!;
        var tileSize = grid.TileSize;
        using var framebuffer = entry.Bitmap.Lock();
        var stride = framebuffer.RowBytes;
        var baseAddress = framebuffer.Address;

        foreach (var coordinate in tiles)
        {
            var originX = coordinate.X * tileSize;
            var originY = coordinate.Y * tileSize;
            var bounds = grid.GetTileBounds(coordinate);
            var tileWidth = (int)bounds.Width;
            var tileHeight = (int)bounds.Height;
            if (tileWidth <= 0 || tileHeight <= 0)
            {
                continue;
            }

            var rowBytes = tileWidth * 4;
            var hasTile = entry.Rasterizer.Surface.TryGetTile(coordinate, out var tile);

            for (var y = 0; y < tileHeight; y++)
            {
                var destination = IntPtr.Add(baseAddress, (originY + y) * stride + originX * 4);
                if (hasTile)
                {
                    Marshal.Copy(tile, y * tileSize * 4, destination, rowBytes);
                }
                else
                {
                    Marshal.Copy(_zeroRow, 0, destination, rowBytes);
                }
            }
        }
    }

    private void DisposeFrames()
    {
        foreach (var entry in _frames.Values)
        {
            entry.Rasterizer.Dispose();
            entry.Bitmap.Dispose();
        }

        _frames.Clear();
    }

    public void Dispose()
    {
        DisposeFrames();
    }
}
