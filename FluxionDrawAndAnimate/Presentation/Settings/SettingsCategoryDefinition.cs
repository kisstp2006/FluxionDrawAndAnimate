using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluxionDrawAndAnimate.Presentation.Settings;

/// <summary>
/// Describes one category in the Settings dialog sidebar.
/// <see cref="IsActive"/> is set by the VM so XAML can style the active row.
/// </summary>
public sealed partial class SettingsCategoryDefinition : ObservableObject
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Icon { get; init; }
    public int SortOrder { get; init; }
    public required Func<Control> CreatePage { get; init; }

    [ObservableProperty]
    private bool _isActive;
}
