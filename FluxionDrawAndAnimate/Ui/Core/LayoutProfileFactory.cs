using FluxionDrawAndAnimate.Presentation;

namespace FluxionDrawAndAnimate.Ui.Core;

public sealed class LayoutProfileFactory
{
    private static readonly LayoutProfile Phone = new(
        ResponsiveProfileKind.Phone,
        "Phone",
        usesCompactChrome: true,
        showsSidePanels: false,
        showsAdvancedTimeline: false,
        timelineHeight: 136,
        sidePanelWidth: 0,
        toolRailWidth: 64);

    private static readonly LayoutProfile Tablet = new(
        ResponsiveProfileKind.Tablet,
        "Tablet",
        usesCompactChrome: true,
        showsSidePanels: true,
        showsAdvancedTimeline: true,
        timelineHeight: 196,
        sidePanelWidth: 240,
        toolRailWidth: 68);

    private static readonly LayoutProfile Studio = new(
        ResponsiveProfileKind.Studio,
        "Studio",
        usesCompactChrome: false,
        showsSidePanels: true,
        showsAdvancedTimeline: true,
        timelineHeight: 248,
        sidePanelWidth: 292,
        toolRailWidth: 72);

    public LayoutProfile For(ResponsiveProfileKind kind)
    {
        return kind switch
        {
            ResponsiveProfileKind.Phone => Phone,
            ResponsiveProfileKind.Tablet => Tablet,
            _ => Studio
        };
    }

    public LayoutProfile ForWorkspace(WorkspaceMode mode)
    {
        return mode == WorkspaceMode.Studio ? Studio : Phone;
    }
}
