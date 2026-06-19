using System.Windows.Input;

namespace FluxionDrawAndAnimate.Presentation;

/// <summary>A quick-action card on the home page (New Studio Project, Open Existing, …).</summary>
public sealed class QuickActionDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Subtitle { get; init; } = "";
    public string Icon { get; init; } = "";
    public ICommand? Command { get; init; }
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
}
