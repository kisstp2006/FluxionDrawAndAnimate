using System;
using System.Collections.Generic;
using System.Linq;
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
/// Bridges the pure-C# tile raster engine to Avalonia.
///
/// V2 keeps two cache layers:
/// - layer/frame tile surfaces: one cached raster surface per layer/frame pair;
/// - flattened frame previews: one composited WriteableBitmap per visited frame.
///
/// Both levels are evicted by frame using the project's tile memory budget, so
/// long animations do not keep hundreds of full-frame bitmaps resident.
/// </summary>
public sealed class RasterFrameCache : IDisposable
{
    private readonly record struct LayerFrameKey(string LayerId, int FrameIndex);

    private sealed class LayerTileEntry
    {
        public required AnimationLayer Layer { get; init; }
        public required int FrameIndex { get; init; }
        public required LayerTileRasterizer Rasterizer { get; init; }
        public long LastUsed { get; set; }
    }

    private sealed class FramePreviewEntry
    {
        public required WriteableBitmap Bitmap { get; init; }
        public HashSet<TileCoordinate> DirtyTiles { get; } = new();
        public bool Initialized { get; set; }
        public long LastUsed { get; set; }
    }

    private readonly Dictionary<LayerFrameKey, LayerTileEntry> _layerTiles = new();
    private readonly Dictionary<int, FramePreviewEntry> _framePreviews = new();
    private readonly List<TileCoordinate> _tileScratch = new();
    private readonly List<TileCoordinate> _rebuiltScratch = new();

    private DrawingProject? _project;
    private TileGrid? _grid;
    private TileBufferPool? _pool;
    private byte[] _flattenScratch = Array.Empty<byte>();
    private long _memoryBudgetBytes = 128L * 1024 * 1024;
    private long _frameBitmapBytes;
    private long _clock;

    public void Configure(DrawingProject project)
    {
        var tileSettings = project.TileSettings.Normalize();
        _memoryBudgetBytes = (long)tileSettings.MemoryBudgetMegabytes * 1024 * 1024;

        var canReuse =
            ReferenceEquals(_project, project) &&
            _grid is not null &&
            _grid.CanvasWidth == project.Width &&
            _grid.CanvasHeight == project.Height &&
            _grid.TileSize == tileSettings.TileSize;

        if (canReuse)
        {
            EnforceMemoryBudget(null);
            return;
        }

        DisposeCaches();

        _project = project;
        _grid = new TileGrid(project.Width, project.Height, tileSettings.TileSize);
        _pool = new TileBufferPool(tileSettings.TileSize * tileSettings.TileSize * 4);
        _flattenScratch = new byte[tileSettings.TileSize * tileSettings.TileSize * 4];
        _frameBitmapBytes = (long)project.Width * project.Height * 4;
        _clock = 0;
    }

    public void Clear()
    {
        DisposeCacheEntries();
        _project = null;
        _grid = null;
        _pool = null;
        _flattenScratch = Array.Empty<byte>();
        _frameBitmapBytes = 0;
        _clock = 0;
    }

    public void ClearCachedFrames()
    {
        DisposeCacheEntries();
        _clock = 0;
    }

    public void TrimToBudget()
    {
        EnforceMemoryBudget(null);
    }

    public void TrimToBudget(int memoryBudgetMegabytes)
    {
        _memoryBudgetBytes = Math.Max(1L, memoryBudgetMegabytes) * 1024 * 1024;
        EnforceMemoryBudget(null);
    }

    public void HandleMemoryPressure(bool aggressive)
    {
        if (aggressive)
        {
            ClearCachedFrames();
            return;
        }

        var previousBudget = _memoryBudgetBytes;
        _memoryBudgetBytes = Math.Max(1L, _memoryBudgetBytes / 2);
        try
        {
            EnforceMemoryBudget(null);
        }
        finally
        {
            _memoryBudgetBytes = previousBudget;
        }
    }

    public void InvalidateAll()
    {
        foreach (var layerEntry in _layerTiles.Values)
        {
            layerEntry.Rasterizer.MarkAllDirty();
        }

        foreach (var preview in _framePreviews.Values)
        {
            MarkAllDirty(preview.DirtyTiles);
        }
    }

    public void InvalidateLayerVisual(AnimationLayer layer)
    {
        foreach (var preview in _framePreviews.Values)
        {
            MarkAllDirty(preview.DirtyTiles);
        }
    }

    public void InvalidateStroke(int frameIndex, int layerIndex, StrokePath stroke)
    {
        if (_project is null || _grid is null ||
            layerIndex < 0 || layerIndex >= _project.Layers.Count)
        {
            return;
        }

        var bounds = StrokeBounds.Calculate(stroke);
        if (bounds.IsEmpty)
        {
            return;
        }

        var layer = _project.Layers[layerIndex];
        var key = new LayerFrameKey(layer.Id, frameIndex);
        if (_layerTiles.TryGetValue(key, out var layerEntry))
        {
            layerEntry.Rasterizer.MarkDirty(bounds);
        }

        MarkFrameDirty(frameIndex, bounds);
    }

    public void AppendStrokeRange(int frameIndex, int layerIndex, StrokePath stroke, int firstPointIndex)
    {
        if (_project is null || _grid is null ||
            layerIndex < 0 || layerIndex >= _project.Layers.Count)
        {
            return;
        }

        var bounds = StrokeBounds.CalculateRange(stroke, firstPointIndex);
        if (bounds.IsEmpty)
        {
            return;
        }

        var layer = _project.Layers[layerIndex];
        var layerEntry = GetOrCreateLayerTileEntry(layer, frameIndex);
        foreach (var coordinate in _grid.GetTilesIntersecting(bounds))
        {
            layerEntry.Rasterizer.RasterizeStrokeRange(
                layer,
                frameIndex,
                coordinate,
                stroke,
                firstPointIndex);
        }

        MarkFrameDirty(frameIndex, bounds);
    }

    public void InvalidateFrame(int frameIndex)
    {
        if (_grid is null)
        {
            return;
        }

        foreach (var entry in _layerTiles.Where(pair => pair.Key.FrameIndex == frameIndex))
        {
            entry.Value.Rasterizer.MarkAllDirty();
        }

        if (_framePreviews.TryGetValue(frameIndex, out var preview))
        {
            MarkAllDirty(preview.DirtyTiles);
        }
    }

    /// <summary>Returns a composited bitmap for the frame, rebuilding dirty tiles first.</summary>
    public WriteableBitmap? GetFrameBitmap(int frameIndex)
    {
        if (_project is null || _grid is null || _pool is null ||
            frameIndex < 0 || frameIndex >= _project.FrameCount)
        {
            return null;
        }

        var entry = GetOrCreateFramePreview(frameIndex);
        Touch(entry);

        if (!entry.Initialized)
        {
            MarkAllDirty(entry.DirtyTiles);
            entry.Initialized = true;
        }

        if (entry.DirtyTiles.Count > 0)
        {
            _tileScratch.Clear();
            _tileScratch.AddRange(entry.DirtyTiles);
            entry.DirtyTiles.Clear();
            RebuildFlattenedTiles(frameIndex, entry, _tileScratch);
        }

        EnforceMemoryBudget(frameIndex);
        return entry.Bitmap;
    }

    private FramePreviewEntry GetOrCreateFramePreview(int frameIndex)
    {
        if (_framePreviews.TryGetValue(frameIndex, out var entry))
        {
            return entry;
        }

        entry = new FramePreviewEntry
        {
            Bitmap = new WriteableBitmap(
                new PixelSize(_project!.Width, _project.Height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul)
        };

        _framePreviews[frameIndex] = entry;
        return entry;
    }

    private LayerTileEntry GetOrCreateLayerTileEntry(AnimationLayer layer, int frameIndex)
    {
        var key = new LayerFrameKey(layer.Id, frameIndex);
        if (_layerTiles.TryGetValue(key, out var entry))
        {
            Touch(entry);
            return entry;
        }

        entry = new LayerTileEntry
        {
            Layer = layer,
            FrameIndex = frameIndex,
            Rasterizer = new LayerTileRasterizer(_grid!, _pool!)
        };

        _layerTiles[key] = entry;
        Touch(entry);
        return entry;
    }

    private void RebuildFlattenedTiles(
        int frameIndex,
        FramePreviewEntry preview,
        IReadOnlyList<TileCoordinate> tiles)
    {
        if (tiles.Count == 0)
        {
            return;
        }

        var grid = _grid!;
        var tileSize = grid.TileSize;
        using var framebuffer = preview.Bitmap.Lock();
        var stride = framebuffer.RowBytes;
        var baseAddress = framebuffer.Address;

        foreach (var coordinate in tiles)
        {
            Array.Clear(_flattenScratch);

            foreach (var layer in _project!.Layers)
            {
                if (!layer.IsVisible || layer.Opacity <= 0 || frameIndex >= layer.Frames.Count)
                {
                    continue;
                }

                var layerEntry = GetOrCreateLayerTileEntry(layer, frameIndex);
                if (!layerEntry.Rasterizer.HasVisitedTile(coordinate))
                {
                    layerEntry.Rasterizer.MarkDirty(coordinate);
                }

                RebuildLayerIfNeeded(layerEntry);

                if (!layerEntry.Rasterizer.Surface.TryGetTile(coordinate, out var layerTile))
                {
                    continue;
                }

                CompositeLayerTile(_flattenScratch, layerTile, Math.Clamp(layer.Opacity, 0, 1));
            }

            BlitScratchTile(baseAddress, stride, coordinate, tileSize);
        }
    }

    private void RebuildLayerIfNeeded(LayerTileEntry entry)
    {
        if (!entry.Rasterizer.HasDirtyTiles)
        {
            return;
        }

        _rebuiltScratch.Clear();
        entry.Rasterizer.Rebuild(entry.Layer, entry.FrameIndex, _rebuiltScratch);
    }

    private static void CompositeLayerTile(byte[] destination, byte[] source, double opacity)
    {
        for (var i = 0; i < source.Length; i += 4)
        {
            var alpha = source[i + 3];
            if (alpha == 0)
            {
                continue;
            }

            CompositeOp.Over(destination, i, source[i], source[i + 1], source[i + 2], alpha, opacity);
        }
    }

    private void BlitScratchTile(IntPtr baseAddress, int stride, TileCoordinate coordinate, int tileSize)
    {
        var grid = _grid!;
        var originX = coordinate.X * tileSize;
        var originY = coordinate.Y * tileSize;
        var bounds = grid.GetTileBounds(coordinate);
        var tileWidth = (int)bounds.Width;
        var tileHeight = (int)bounds.Height;
        if (tileWidth <= 0 || tileHeight <= 0)
        {
            return;
        }

        var rowBytes = tileWidth * 4;
        for (var y = 0; y < tileHeight; y++)
        {
            var destination = IntPtr.Add(baseAddress, (originY + y) * stride + originX * 4);
            Marshal.Copy(_flattenScratch, y * tileSize * 4, destination, rowBytes);
        }
    }

    private void MarkFrameDirty(int frameIndex, Core.Geometry.DocumentRect bounds)
    {
        if (_grid is null)
        {
            return;
        }

        if (!_framePreviews.TryGetValue(frameIndex, out var preview))
        {
            return;
        }

        foreach (var coordinate in _grid.GetTilesIntersecting(bounds))
        {
            preview.DirtyTiles.Add(coordinate);
        }
    }

    private void MarkAllDirty(HashSet<TileCoordinate> target)
    {
        var grid = _grid!;
        for (var y = 0; y < grid.Rows; y++)
        {
            for (var x = 0; x < grid.Columns; x++)
            {
                target.Add(new TileCoordinate(x, y));
            }
        }
    }

    private void Touch(FramePreviewEntry entry)
    {
        entry.LastUsed = ++_clock;
    }

    private void Touch(LayerTileEntry entry)
    {
        entry.LastUsed = ++_clock;
    }

    private long EstimateCacheBytes()
    {
        var layerTileBytes = _pool is null
            ? 0
            : _layerTiles.Values.Sum(entry =>
                (long)entry.Rasterizer.Surface.TileCount * _pool.BufferLength);
        var previewBytes = (long)_framePreviews.Count * _frameBitmapBytes;
        return layerTileBytes + previewBytes;
    }

    private void EnforceMemoryBudget(int? frameToKeep)
    {
        if (_memoryBudgetBytes <= 0)
        {
            return;
        }

        while (EstimateCacheBytes() > _memoryBudgetBytes && _framePreviews.Count > 1)
        {
            var victim = _framePreviews
                .Where(pair => pair.Key != frameToKeep)
                .OrderBy(pair => pair.Value.LastUsed)
                .FirstOrDefault();

            if (victim.Value is null)
            {
                break;
            }

            EvictFrame(victim.Key);
        }

        _pool?.TrimFreeBuffers(0);
    }

    private void EvictFrame(int frameIndex)
    {
        if (_framePreviews.Remove(frameIndex, out var preview))
        {
            preview.Bitmap.Dispose();
        }

        var layersToRemove = _layerTiles.Keys
            .Where(key => key.FrameIndex == frameIndex)
            .ToArray();

        foreach (var key in layersToRemove)
        {
            _layerTiles[key].Rasterizer.Dispose();
            _layerTiles.Remove(key);
        }
    }

    private void DisposeCaches()
    {
        DisposeCacheEntries();
        _pool?.TrimFreeBuffers(0);
    }

    private void DisposeCacheEntries()
    {
        foreach (var preview in _framePreviews.Values)
        {
            preview.Bitmap.Dispose();
        }

        foreach (var entry in _layerTiles.Values)
        {
            entry.Rasterizer.Dispose();
        }

        _framePreviews.Clear();
        _layerTiles.Clear();
        _pool?.TrimFreeBuffers(0);
    }

    public void Dispose()
    {
        DisposeCaches();
    }
}
