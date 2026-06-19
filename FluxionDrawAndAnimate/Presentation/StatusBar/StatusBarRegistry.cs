using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FluxionDrawAndAnimate.Presentation.StatusBar;

/// <summary>
/// Holds all registered status-bar items. ObservableCollection so the bar
/// updates live when items are added after startup (plugin scenario).
/// </summary>
public sealed class StatusBarRegistry
{
    public ObservableCollection<StatusBarItemDefinition> Items { get; } = [];

    public void Register(StatusBarItemDefinition item)
    {
        Items.Add(item);
        Sort();
    }

    /// <summary>Returns items matching the current page and section, sorted.</summary>
    public IReadOnlyList<StatusBarItemDefinition> GetForPage(string? pageId, StatusBarSection section)
    {
        return Items
            .Where(i => i.Section == section
                        && (i.PageId is null
                            || i.PageId.Equals(pageId, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(i => i.SortOrder)
            .ToList();
    }

    private void Sort()
    {
        var sorted = Items.OrderBy(i => i.SortOrder).ToList();
        for (var idx = 0; idx < sorted.Count; idx++)
        {
            var cur = Items.IndexOf(sorted[idx]);
            if (cur != idx)
            {
                Items.Move(cur, idx);
            }
        }
    }
}
