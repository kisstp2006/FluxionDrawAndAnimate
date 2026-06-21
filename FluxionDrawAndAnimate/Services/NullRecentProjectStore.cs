using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FluxionDrawAndAnimate.Services;

public sealed class NullRecentProjectStore : IRecentProjectStore
{
    public Task<IReadOnlyList<RecentProjectInfo>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<RecentProjectInfo>>([]);
    }

    public Task AddOrUpdateAsync(RecentProjectInfo project, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveAsync(string path, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
