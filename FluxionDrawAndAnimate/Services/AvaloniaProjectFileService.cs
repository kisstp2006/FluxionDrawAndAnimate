using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Persistence;

namespace FluxionDrawAndAnimate.Services;

public sealed class AvaloniaProjectFileService : IProjectFileService
{
    private static readonly FilePickerFileType FluxionProjectFileType = new("Fluxion Project")
    {
        Patterns = new[] { "*.fluxion" },
        MimeTypes = new[] { "application/x-fluxion-project", "application/zip" },
        AppleUniformTypeIdentifiers = new[] { "com.fluxion.project" }
    };

    private readonly Func<TopLevel?> _getTopLevel;
    private readonly IProjectArchiveStore _archiveStore;
    private IStorageFile? _currentFile;

    public AvaloniaProjectFileService(Func<TopLevel?> getTopLevel, IProjectArchiveStore archiveStore)
    {
        _getTopLevel = getTopLevel;
        _archiveStore = archiveStore;
    }

    public async Task<ProjectFileSaveResult?> SaveAsync(DrawingProject project, CancellationToken cancellationToken = default)
    {
        if (_currentFile is null)
        {
            return await SaveAsAsync(project, cancellationToken);
        }

        await WriteProjectAsync(project, _currentFile, cancellationToken);
        return new ProjectFileSaveResult(DisplayName(_currentFile), LocalPath(_currentFile));
    }

    public async Task<ProjectFileSaveResult?> SaveAsAsync(DrawingProject project, CancellationToken cancellationToken = default)
    {
        var storageProvider = _getTopLevel()?.StorageProvider;
        if (storageProvider?.CanSave != true)
        {
            return null;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Fluxion Project",
            SuggestedFileName = SuggestedFileName(project.Name),
            DefaultExtension = "fluxion",
            FileTypeChoices = new List<FilePickerFileType> { FluxionProjectFileType },
            SuggestedFileType = FluxionProjectFileType,
            ShowOverwritePrompt = true
        });

        if (file is null)
        {
            return null;
        }

        await WriteProjectAsync(project, file, cancellationToken);
        _currentFile = file;
        return new ProjectFileSaveResult(DisplayName(file), LocalPath(file));
    }

    public async Task<ProjectFileLoadResult?> OpenAsync(CancellationToken cancellationToken = default)
    {
        var storageProvider = _getTopLevel()?.StorageProvider;
        if (storageProvider?.CanOpen != true)
        {
            return null;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Fluxion Project",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> { FluxionProjectFileType },
            SuggestedFileType = FluxionProjectFileType
        });

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return null;
        }

        await using var stream = await file.OpenReadAsync();
        var project = await _archiveStore.LoadAsync(stream, cancellationToken);
        _currentFile = file;
        return new ProjectFileLoadResult(project, DisplayName(file), LocalPath(file));
    }

    public async Task<ProjectFileLoadResult?> OpenPathAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        var project = await _archiveStore.LoadAsync(stream, cancellationToken);

        var storageProvider = _getTopLevel()?.StorageProvider;
        if (storageProvider is not null)
        {
            _currentFile = await storageProvider.TryGetFileFromPathAsync(path);
        }

        return new ProjectFileLoadResult(project, path, path);
    }

    private async Task WriteProjectAsync(DrawingProject project, IStorageFile file, CancellationToken cancellationToken)
    {
        await using var stream = await file.OpenWriteAsync();
        if (stream.CanSeek)
        {
            stream.SetLength(0);
        }

        await _archiveStore.SaveAsync(project, stream, cancellationToken);
    }

    private static string DisplayName(IStorageItem item)
    {
        return item.TryGetLocalPath() ?? item.Name;
    }

    private static string? LocalPath(IStorageItem item)
    {
        return item.TryGetLocalPath();
    }

    private static string SuggestedFileName(string projectName)
    {
        var name = string.IsNullOrWhiteSpace(projectName) ? "Untitled" : projectName.Trim();
        return name.EndsWith(".fluxion", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.fluxion";
    }
}
