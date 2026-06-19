using System.Threading;
using System.Threading.Tasks;
using FluxionDrawAndAnimate.Core.Animation;

namespace FluxionDrawAndAnimate.Services;

public sealed class NullProjectFileService : IProjectFileService
{
    public Task<ProjectFileSaveResult?> SaveAsync(DrawingProject project, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ProjectFileSaveResult?>(null);
    }

    public Task<ProjectFileSaveResult?> SaveAsAsync(DrawingProject project, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ProjectFileSaveResult?>(null);
    }

    public Task<ProjectFileLoadResult?> OpenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ProjectFileLoadResult?>(null);
    }

    public Task<ProjectFileLoadResult?> OpenPathAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ProjectFileLoadResult?>(null);
    }
}
