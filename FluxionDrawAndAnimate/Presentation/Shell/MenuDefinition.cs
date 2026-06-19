using System.Collections.Generic;

namespace FluxionDrawAndAnimate.Presentation.Shell;

/// <summary>A top-level menu entry (File, Edit, View, Help, …) with its drop-down items.</summary>
public sealed class MenuDefinition
{
    public required string Title { get; init; }
    public int SortOrder { get; init; }
    public List<MenuItemDefinition> Items { get; init; } = [];
}
