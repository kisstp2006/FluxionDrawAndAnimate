using System.Threading;
using System.Threading.Tasks;
using FluxionDrawAndAnimate.Core.Settings;

namespace FluxionDrawAndAnimate.Services;

public interface IUserSettingsStore
{
    Task LoadAsync(SettingValueStore values, CancellationToken cancellationToken = default);

    Task SaveAsync(SettingValueStore values, CancellationToken cancellationToken = default);
}
