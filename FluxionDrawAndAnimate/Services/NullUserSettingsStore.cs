using System.Threading;
using System.Threading.Tasks;
using FluxionDrawAndAnimate.Core.Settings;

namespace FluxionDrawAndAnimate.Services;

public sealed class NullUserSettingsStore : IUserSettingsStore
{
    public Task LoadAsync(SettingValueStore values, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveAsync(SettingValueStore values, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
