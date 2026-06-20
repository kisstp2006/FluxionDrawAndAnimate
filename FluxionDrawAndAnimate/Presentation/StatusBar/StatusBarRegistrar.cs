using FluxionDrawAndAnimate.ViewModels;

namespace FluxionDrawAndAnimate.Presentation.StatusBar;

public static class StatusBarRegistrar
{
    public static void RegisterDefaults(StatusBarRegistry registry, MainViewModel vm)
    {
        RegisterGlobal(registry);
        RegisterStudioLeft(registry, vm);
        RegisterStudioRight(registry, vm);
    }

    private static void RegisterGlobal(StatusBarRegistry r)
    {
        r.Register(new StatusBarItemDefinition
        {
            Id = "app-version", Section = StatusBarSection.Left,
            Text = $"v{AppInfo.Version}", SortOrder = 0
        });
    }

    private static void RegisterStudioLeft(StatusBarRegistry r, MainViewModel vm)
    {
        // Zoom display
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-zoom", Section = StatusBarSection.Left,
            HasDropdown = true,
            GetLiveText = ds => $"{(int)(((MainViewModel)ds!).CanvasZoom * 100)}%",
            ObservedProperties = [nameof(MainViewModel.CanvasZoom)],
            PageId = "studio", SortOrder = 10
        });

        r.Register(new StatusBarItemDefinition
        {
            Id = "sep-studio-1", Section = StatusBarSection.Left,
            IsSeparator = true, PageId = "studio", SortOrder = 11
        });

        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-zoom-fit", Section = StatusBarSection.Left,
            Icon = "mdi-fit-to-page-outline", Text = "",
            PageId = "studio", SortOrder = 12
        });
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-snapshot", Section = StatusBarSection.Left,
            Icon = "mdi-camera-outline", Text = "",
            PageId = "studio", SortOrder = 13
        });
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-history", Section = StatusBarSection.Left,
            Icon = "mdi-history", Text = "",
            PageId = "studio", SortOrder = 14
        });

        r.Register(new StatusBarItemDefinition
        {
            Id = "sep-studio-2", Section = StatusBarSection.Left,
            IsSeparator = true, PageId = "studio", SortOrder = 15
        });

        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-tool", Section = StatusBarSection.Left,
            GetLiveText = ds => $"Tool:  {((MainViewModel)ds!).ActiveToolName}",
            ObservedProperties = [nameof(MainViewModel.ActiveToolName)],
            PageId = "studio", SortOrder = 20
        });
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-size", Section = StatusBarSection.Left,
            GetLiveText = ds => $"Size:  {((MainViewModel)ds!).BrushSize:0}px",
            ObservedProperties = [nameof(MainViewModel.BrushSize)],
            PageId = "studio", SortOrder = 21
        });
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-pressure", Section = StatusBarSection.Left,
            GetLiveText = ds => $"Pressure:  {(int)(((MainViewModel)ds!).LastPressure * 100)}%",
            ObservedProperties = [nameof(MainViewModel.LastPressure)],
            PageId = "studio", SortOrder = 22
        });
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-position", Section = StatusBarSection.Left,
            GetLiveText = ds =>
            {
                var m = (MainViewModel)ds!;
                return $"Position:  {(int)m.CursorDocumentX}, {(int)m.CursorDocumentY}";
            },
            ObservedProperties = [nameof(MainViewModel.CursorDocumentX)],
            PageId = "studio", SortOrder = 23
        });

        r.Register(new StatusBarItemDefinition
        {
            Id = "sep-studio-3", Section = StatusBarSection.Left,
            IsSeparator = true, PageId = "studio", SortOrder = 24
        });

        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-snap", Section = StatusBarSection.Left,
            GetLiveText = ds => $"Snap:  {(((MainViewModel)ds!).SnapEnabled ? "On" : "Off")}",
            ObservedProperties = [nameof(MainViewModel.SnapEnabled)],
            Command = vm.ToggleSnapCommand,
            PageId = "studio", SortOrder = 25
        });
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-grid", Section = StatusBarSection.Left,
            Icon = "mdi-grid", Text = "",
            Command = vm.ToggleGridCommand,
            PageId = "studio", SortOrder = 26
        });
    }

    private static void RegisterStudioRight(StatusBarRegistry r, MainViewModel vm)
    {
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-memory", Section = StatusBarSection.Right,
            GetLiveText = ds => $"Memory:  {((MainViewModel)ds!).MemoryUsagePercent}%",
            GetProgress = ds => ((MainViewModel)ds!).MemoryUsagePercent / 100.0,
            ObservedProperties = [nameof(MainViewModel.MemoryUsagePercent)],
            PageId = "studio", SortOrder = 0
        });
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-settings", Section = StatusBarSection.Right,
            Icon = "mdi-cog-outline", Text = "",
            Command = vm.ShowSettingsCommand,
            PageId = "studio", SortOrder = 10
        });
    }
}
