using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FluxionDrawAndAnimate.Presentation.Studio;

/// <summary>
/// Holds all registered inspector panel cards for the Studio right sidebar.
/// Feature modules call <see cref="Register"/> to add their own panels.
/// </summary>
public sealed class StudioInspectorRegistry
{
    public ObservableCollection<InspectorPanelDefinition> Panels { get; } = [];

    /// <summary>Fired when panels are added/removed — lets the UI rebuild.</summary>
    public event Action? PanelsChanged;

    public void Register(InspectorPanelDefinition panel)
    {
        Panels.Add(panel);
        Sort();
        PanelsChanged?.Invoke();
    }

    private void Sort()
    {
        var sorted = Panels.OrderBy(p => p.SortOrder).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var cur = Panels.IndexOf(sorted[i]);
            if (cur != i) Panels.Move(cur, i);
        }
    }
}
