using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Rendering;

namespace FluxionDrawAndAnimate.Services;

public sealed class AvaloniaProjectThumbnailService : IProjectThumbnailService
{
    private const int ThumbnailWidth = 360;
    private const int ThumbnailHeight = 240;

    private readonly DrawingProjectFrameRenderer _renderer;
    private readonly string _thumbnailDirectory;

    public AvaloniaProjectThumbnailService(DrawingProjectFrameRenderer? renderer = null, string? thumbnailDirectory = null)
    {
        _renderer = renderer ?? new DrawingProjectFrameRenderer();
        _thumbnailDirectory = thumbnailDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluxionDrawAndAnimate",
            "thumbnails");
    }

    public Task<string?> SaveThumbnailAsync(
        DrawingProject project,
        string projectPath,
        int preferredFrameIndex,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return Task.FromResult<string?>(null);
        }

        Directory.CreateDirectory(_thumbnailDirectory);
        var thumbnailPath = Path.Combine(_thumbnailDirectory, $"{SafeFileName(project.Id)}-{HashPath(projectPath)}.png");

        using var bitmap = new RenderTargetBitmap(new PixelSize(ThumbnailWidth, ThumbnailHeight), new Vector(96, 96));
        using (var context = bitmap.CreateDrawingContext())
        {
            _renderer.DrawThumbnail(
                context,
                project,
                preferredFrameIndex,
                new Rect(0, 0, ThumbnailWidth, ThumbnailHeight));
        }

        cancellationToken.ThrowIfCancellationRequested();
        bitmap.Save(thumbnailPath);
        return Task.FromResult<string?>(thumbnailPath);
    }

    private static string SafeFileName(string value)
    {
        var name = string.IsNullOrWhiteSpace(value) ? "project" : value.Trim();
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }

        return name;
    }

    private static string HashPath(string path)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(path));
        return Convert.ToHexString(bytes, 0, 6).ToLowerInvariant();
    }
}
