using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluxionDrawAndAnimate.Services;

public sealed class NullFavoritesStore : IFavoritesStore
{
    public Task<HashSet<string>> LoadAsync() =>
        Task.FromResult(new HashSet<string>());

    public Task SaveAsync(IEnumerable<string> projectPaths) =>
        Task.CompletedTask;
}
