using System.Threading;
using System.Threading.Tasks;
using FluxionDrawAndAnimate.Core.Animation;

namespace FluxionDrawAndAnimate.Services;

public interface IProjectThumbnailService
{
    Task<string?> SaveThumbnailAsync(
        DrawingProject project,
        string projectPath,
        int preferredFrameIndex,
        CancellationToken cancellationToken = default);
}
