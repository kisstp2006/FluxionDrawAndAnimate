using System.Threading;
using System.Threading.Tasks;
using FluxionDrawAndAnimate.Core.Animation;

namespace FluxionDrawAndAnimate.Services;

public sealed class NullProjectThumbnailService : IProjectThumbnailService
{
    public Task<string?> SaveThumbnailAsync(
        DrawingProject project,
        string projectPath,
        int preferredFrameIndex,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }
}
