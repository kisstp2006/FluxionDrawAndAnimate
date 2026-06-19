using FluxionDrawAndAnimate.Core.Animation;

namespace FluxionDrawAndAnimate.Core.Persistence;

public interface IProjectArchiveStore
{
    Task SaveAsync(DrawingProject project, Stream destination, CancellationToken cancellationToken = default);

    Task<DrawingProject> LoadAsync(Stream source, CancellationToken cancellationToken = default);
}
