namespace FluxionDrawAndAnimate.Ui.Core;

public sealed class LayoutProfile
{
    public LayoutProfile(
        ResponsiveProfileKind kind,
        string name,
        bool usesCompactChrome,
        bool showsSidePanels,
        bool showsAdvancedTimeline,
        double timelineHeight,
        double sidePanelWidth,
        double toolRailWidth)
    {
        Kind = kind;
        Name = name;
        UsesCompactChrome = usesCompactChrome;
        ShowsSidePanels = showsSidePanels;
        ShowsAdvancedTimeline = showsAdvancedTimeline;
        TimelineHeight = timelineHeight;
        SidePanelWidth = sidePanelWidth;
        ToolRailWidth = toolRailWidth;
    }

    public ResponsiveProfileKind Kind { get; }
    public string Name { get; }
    public bool UsesCompactChrome { get; }
    public bool ShowsSidePanels { get; }
    public bool ShowsAdvancedTimeline { get; }
    public double TimelineHeight { get; }
    public double SidePanelWidth { get; }
    public double ToolRailWidth { get; }
}
