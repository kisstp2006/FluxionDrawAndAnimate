using System.Windows.Input;

namespace FluxionDrawAndAnimate.Presentation.Shell;

/// <summary>
/// A button in the right section of the title bar (New Project, Open, …).
/// <see cref="IsPrimary"/> → accent colour; false → secondary/ghost style.
/// </summary>
public sealed class TitleBarActionDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public ICommand? Command { get; init; }
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
}
