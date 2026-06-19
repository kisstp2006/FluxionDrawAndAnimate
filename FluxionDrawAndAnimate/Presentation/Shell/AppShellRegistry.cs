using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FluxionDrawAndAnimate.Presentation.Shell;

/// <summary>
/// Central registry for all shell extension points: pages, menus, title-bar
/// actions. Holds <see cref="ObservableCollection{T}"/>s so the title bar
/// updates live if items are registered after startup.
///
/// Register once at startup via <see cref="AppShellRegistrar"/>; feature
/// modules can call Register* methods to add their own entries.
/// </summary>
public sealed class AppShellRegistry
{
    public ObservableCollection<PageDefinition> Pages { get; } = [];
    public ObservableCollection<MenuDefinition> Menus { get; } = [];
    public ObservableCollection<TitleBarActionDefinition> Actions { get; } = [];

    public void RegisterPage(PageDefinition page)
    {
        Pages.Add(page);
        Sort(Pages, p => p.SortOrder);
    }

    public void RegisterMenu(MenuDefinition menu)
    {
        Menus.Add(menu);
        Sort(Menus, m => m.SortOrder);
    }

    public void RegisterAction(TitleBarActionDefinition action)
    {
        Actions.Add(action);
        Sort(Actions, a => a.SortOrder);
    }

    public PageDefinition? FindPage(string id) =>
        Pages.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private static void Sort<T>(ObservableCollection<T> collection, Func<T, int> key)
    {
        var sorted = collection.OrderBy(key).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var current = collection.IndexOf(sorted[i]);
            if (current != i)
            {
                collection.Move(current, i);
            }
        }
    }
}
