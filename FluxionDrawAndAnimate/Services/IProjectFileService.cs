using System.Threading;
using System.Threading.Tasks;
using FluxionDrawAndAnimate.Core.Animation;

namespace FluxionDrawAndAnimate.Services;

public interface IProjectFileService
{
    Task<ProjectFileSaveResult?> SaveAsync(DrawingProject project, CancellationToken cancellationToken = default);

    Task<ProjectFileSaveResult?> SaveAsAsync(DrawingProject project, CancellationToken cancellationToken = default);

    Task<ProjectFileLoadResult?> OpenAsync(CancellationToken cancellationToken = default);

    Task<ProjectFileLoadResult?> OpenPathAsync(string path, CancellationToken cancellationToken = default);
}

public sealed record ProjectFileSaveResult(string? DisplayName, string? Path);

public sealed record ProjectFileLoadResult(DrawingProject Project, string? DisplayName, string? Path);
