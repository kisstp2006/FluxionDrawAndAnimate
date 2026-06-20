using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluxionDrawAndAnimate.Presentation.Studio;

/// <summary>
/// Describes one collapsible card in the Studio right inspector panel.
/// Register via <see cref="StudioInspectorRegistry"/> — the panel itself
/// has no hardcoded knowledge of which cards it shows.
/// </summary>
public sealed partial class InspectorPanelDefinition : ObservableObject
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required Func<Control> CreateContent { get; init; }
    public int SortOrder { get; init; }

    [ObservableProperty]
    private bool _isExpanded = true;
}
