using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FluxionDrawAndAnimate.Services;

public interface IRecentProjectStore
{
    Task<IReadOnlyList<RecentProjectInfo>> LoadAsync(CancellationToken cancellationToken = default);

    Task AddOrUpdateAsync(RecentProjectInfo project, CancellationToken cancellationToken = default);
}
