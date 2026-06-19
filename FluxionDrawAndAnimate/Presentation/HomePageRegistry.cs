using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluxionDrawAndAnimate.Presentation;

/// <summary>
/// Runtime-configurable state for the home page. Everything here can be changed
/// at runtime and the UI updates automatically via data binding.
/// </summary>
public sealed partial class HomePageRegistry : ObservableObject
{
    // ── Welcome header ────────────────────────────────────────────────────

    [ObservableProperty]
    private string _welcomeName = "Alex";

    [ObservableProperty]
    private string _welcomeEmoji = "👋";

    [ObservableProperty]
    private string _welcomeSubtitle = "Ready to bring your ideas to life?";

    // ── Quick action cards ────────────────────────────────────────────────

    public ObservableCollection<QuickActionDefinition> QuickActions { get; } = [];

    public void RegisterQuickAction(QuickActionDefinition action)
    {
        QuickActions.Add(action);
        Sort(QuickActions, q => q.SortOrder);
    }

    // ── Search / filter / sort ────────────────────────────────────────────

    [ObservableProperty]
    private string _projectSearchQuery = "";

    [ObservableProperty]
    private string _projectTypeFilter = "All Types";

    [ObservableProperty]
    private string _projectSortLabel = "Last Modified";

    [ObservableProperty]
    private HomeProjectViewMode _projectViewMode = HomeProjectViewMode.Grid;

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void Sort<T>(ObservableCollection<T> col, Func<T, int> key)
    {
        var sorted = col.OrderBy(key).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var cur = col.IndexOf(sorted[i]);
            if (cur != i) col.Move(cur, i);
        }
    }
}
