using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FluxionDrawAndAnimate.Presentation.Studio;

/// <summary>
/// Holds all registered tool panel entries. Grouped sections are recomputed
/// whenever items are added so binding to <see cref="Sections"/> stays fresh.
/// Feature modules call <see cref="Register"/> to contribute their own tools.
/// </summary>
public sealed class StudioToolPanelRegistry
{
    public ObservableCollection<ToolDefinition> Items { get; } = [];

    /// <summary>Tools grouped by <see cref="ToolDefinition.Section"/>, ordered by SortOrder.</summary>
    public IReadOnlyList<StudioToolPanelSection> Sections { get; private set; } = [];

    /// <summary>Fired after any registration so the VM can push the new sections to the UI.</summary>
    public event Action? SectionsChanged;

    public void Register(ToolDefinition tool)
    {
        Items.Add(tool);
        RebuildSections();
    }

    private void RebuildSections()
    {
        Sections = Items
            .GroupBy(t => t.Section)
            .Select(g => new StudioToolPanelSection(
                g.Key,
                g.OrderBy(t => t.SortOrder).ToList()))
            .ToList();

        SectionsChanged?.Invoke();
    }
}
