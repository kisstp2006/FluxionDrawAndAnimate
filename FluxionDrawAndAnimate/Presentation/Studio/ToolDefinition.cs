using CommunityToolkit.Mvvm.ComponentModel;
using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Presentation.Studio;

/// <summary>
/// Describes one entry in the Studio tool panel (left sidebar).
/// <see cref="IsActive"/> is updated by the VM when <c>ActiveToolKind</c> changes.
/// <see cref="IsAvailable"/> = false → grayed out, labelled "coming soon".
/// </summary>
public sealed partial class ToolDefinition : ObservableObject
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Icon { get; init; }
    public required ToolKind ToolKind { get; init; }
    public string Section { get; init; } = "Tools";
    public int SortOrder { get; init; }
    public bool IsAvailable { get; init; } = true;

    [ObservableProperty]
    private bool _isActive;
}
