using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace FluxionDrawAndAnimate.ViewModels;

public sealed class SettingsGroupViewModel
{
    public SettingsGroupViewModel(string name, IEnumerable<SettingItemViewModel> settings)
    {
        Name = name;
        Settings = new ObservableCollection<SettingItemViewModel>(settings);
    }

    public string Name { get; }
    public ObservableCollection<SettingItemViewModel> Settings { get; }
}
