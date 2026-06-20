using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluxionDrawAndAnimate.Services;

public interface IFavoritesStore
{
    Task<HashSet<string>> LoadAsync();
    Task SaveAsync(IEnumerable<string> projectPaths);
}
