using System.Windows.Input;

namespace FluxionDrawAndAnimate.Presentation.Shell;

/// <summary>One item inside a registered drop-down menu.</summary>
public sealed class MenuItemDefinition
{
    public string Title { get; init; } = "";
    public string? ShortcutText { get; init; }
    public ICommand? Command { get; init; }
    public bool IsSeparator { get; init; }
    public int SortOrder { get; init; }
}
