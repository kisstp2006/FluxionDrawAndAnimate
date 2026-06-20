using System.Windows.Input;

namespace FluxionDrawAndAnimate.Presentation.Studio;

public enum StudioToolbarSection { Left, Right }

/// <summary>
/// One item in the Studio page's secondary toolbar.
/// Separators have <see cref="IsSeparator"/> = true; all other fields are ignored for them.
/// Right-section items can optionally show a text label and a dropdown arrow.
/// Commands must be existing VM commands — no duplicate logic is registered here.
/// </summary>
public sealed class StudioToolbarItemDefinition
{
    public required string Id { get; init; }
    public string Icon { get; init; } = "";
    public string Text { get; init; } = "";
    public string? Tooltip { get; init; }
    public ICommand? Command { get; init; }
    public bool IsSeparator { get; init; }
    public bool IsPrimary { get; init; }
    public bool HasDropdown { get; init; }
    public StudioToolbarSection Section { get; init; } = StudioToolbarSection.Left;
    public int SortOrder { get; init; }
}
