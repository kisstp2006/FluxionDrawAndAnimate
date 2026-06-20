using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FluxionDrawAndAnimate.Presentation.Studio;

/// <summary>
/// Holds all registered Studio toolbar items.
/// Items are sorted by <see cref="StudioToolbarItemDefinition.SortOrder"/> within each section.
/// Feature modules call <see cref="Register"/> to add their own buttons without touching this file.
/// </summary>
public sealed class StudioToolbarRegistry
{
    public ObservableCollection<StudioToolbarItemDefinition> Items { get; } = [];

    public IReadOnlyList<StudioToolbarItemDefinition> LeftItems =>
        Items.Where(i => i.Section == StudioToolbarSection.Left)
             .OrderBy(i => i.SortOrder)
             .ToList();

    public IReadOnlyList<StudioToolbarItemDefinition> RightItems =>
        Items.Where(i => i.Section == StudioToolbarSection.Right)
             .OrderBy(i => i.SortOrder)
             .ToList();

    public void Register(StudioToolbarItemDefinition item)
    {
        Items.Add(item);
        Sort();
    }

    private void Sort()
    {
        var sorted = Items.OrderBy(i => i.Section).ThenBy(i => i.SortOrder).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var cur = Items.IndexOf(sorted[i]);
            if (cur != i) Items.Move(cur, i);
        }
    }
}
