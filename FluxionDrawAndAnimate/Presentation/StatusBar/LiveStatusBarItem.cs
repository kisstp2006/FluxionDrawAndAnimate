using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace FluxionDrawAndAnimate.Presentation.StatusBar;

/// <summary>
/// Observable wrapper around <see cref="StatusBarItemDefinition"/>.
/// The bottom bar binds to <see cref="CurrentText"/> and <see cref="Progress"/>;
/// when the ViewModel notifies a property listed in
/// <see cref="StatusBarItemDefinition.ObservedProperties"/> the shell calls
/// <see cref="Refresh"/> to push updated values.
/// </summary>
public sealed class LiveStatusBarItem : INotifyPropertyChanged
{
    private readonly StatusBarItemDefinition _def;
    private string _currentText;
    private double _progress;

    public LiveStatusBarItem(StatusBarItemDefinition def, object? dataSource)
    {
        _def = def;
        _currentText = ComputeText(dataSource);
        _progress = ComputeProgress(dataSource);
    }

    public bool IsSeparator => _def.IsSeparator;
    public string? Icon => _def.Icon;
    public ICommand? Command => _def.Command;
    public bool HasDropdown => _def.HasDropdown;
    public bool HasProgress => _def.GetProgress is not null;
    public IReadOnlyList<string>? ObservedProperties => _def.ObservedProperties;

    public string CurrentText
    {
        get => _currentText;
        private set { if (value == _currentText) return; _currentText = value; Notify(nameof(CurrentText)); }
    }

    public double Progress
    {
        get => _progress;
        private set { if (value == _progress) return; _progress = value; Notify(nameof(Progress)); }
    }

    public void Refresh(object? dataSource)
    {
        CurrentText = ComputeText(dataSource);
        Progress = ComputeProgress(dataSource);
    }

    private string ComputeText(object? ds) => _def.GetLiveText?.Invoke(ds) ?? _def.Text;
    private double ComputeProgress(object? ds) => _def.GetProgress?.Invoke(ds) ?? 0;

    private void Notify(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}
