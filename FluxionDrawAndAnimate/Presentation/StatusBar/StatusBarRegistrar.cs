using System;
using System.Reflection;
using FluxionDrawAndAnimate.ViewModels;

namespace FluxionDrawAndAnimate.Presentation.StatusBar;

/// <summary>
/// Registers default status bar items.
///
/// Global items (all pages) — left: app version.
/// Studio items — left: zoom, canvas icons, tool, size, pressure, position, snap, grid.
///               right: memory bar + settings.
/// </summary>
public static class StatusBarRegistrar
{
    public static void RegisterDefaults(StatusBarRegistry registry, MainViewModel vm)
    {
        RegisterGlobal(registry);
        RegisterStudioLeft(registry, vm);
        RegisterStudioRight(registry, vm);
    }

    // ── Global (all pages) ──────────────────────────────────────────────

    private static void RegisterGlobal(StatusBarRegistry r)
    {
        var raw = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0] ?? "1.2.0";

        r.Register(new StatusBarItemDefinition
        {
            Id = "app-version", Section = StatusBarSection.Left,
            Text = $"v{raw}", SortOrder = 0
        });
    }

    // ── Studio page — left section ──────────────────────────────────────

    private static void RegisterStudioLeft(StatusBarRegistry r, MainViewModel vm)
    {
        // Zoom level with dropdown marker
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

        // Zoom-to-fit icon
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-zoom-fit", Section = StatusBarSection.Left,
            Icon = "⊡", Text = "",
            PageId = "studio", SortOrder = 12
        });

        // Export / snapshot icon
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-snapshot", Section = StatusBarSection.Left,
            Icon = "⬜", Text = "",
            PageId = "studio", SortOrder = 13
        });

        // History / rotate icon
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-history", Section = StatusBarSection.Left,
            Icon = "↺", Text = "",
            PageId = "studio", SortOrder = 14
        });

        r.Register(new StatusBarItemDefinition
        {
            Id = "sep-studio-2", Section = StatusBarSection.Left,
            IsSeparator = true, PageId = "studio", SortOrder = 15
        });

        // Active tool
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-tool", Section = StatusBarSection.Left,
            GetLiveText = ds => $"Tool:  {((MainViewModel)ds!).ActiveToolName}",
            ObservedProperties = [nameof(MainViewModel.ActiveToolName)],
            PageId = "studio", SortOrder = 20
        });

        // Brush size
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-size", Section = StatusBarSection.Left,
            GetLiveText = ds => $"Size:  {((MainViewModel)ds!).BrushSize:0}px",
            ObservedProperties = [nameof(MainViewModel.BrushSize)],
            PageId = "studio", SortOrder = 21
        });

        // Last pressure
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-pressure", Section = StatusBarSection.Left,
            GetLiveText = ds => $"Pressure:  {(int)(((MainViewModel)ds!).LastPressure * 100)}%",
            ObservedProperties = [nameof(MainViewModel.LastPressure)],
            PageId = "studio", SortOrder = 22
        });

        // Canvas cursor position
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-position", Section = StatusBarSection.Left,
            GetLiveText = ds =>
            {
                var m = (MainViewModel)ds!;
                return $"Position:  {(int)m.CursorDocumentX}, {(int)m.CursorDocumentY}";
            },
            ObservedProperties = [nameof(MainViewModel.CursorDocumentX)],
            PageId = "studio", SortOrder = 23
        });

        r.Register(new StatusBarItemDefinition
        {
            Id = "sep-studio-3", Section = StatusBarSection.Left,
            IsSeparator = true, PageId = "studio", SortOrder = 24
        });

        // Snap toggle
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-snap", Section = StatusBarSection.Left,
            GetLiveText = ds => $"Snap:  {(((MainViewModel)ds!).SnapEnabled ? "On" : "Off")}",
            ObservedProperties = [nameof(MainViewModel.SnapEnabled)],
            Command = vm.ToggleSnapCommand,
            PageId = "studio", SortOrder = 25
        });

        // Grid toggle icon
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-grid", Section = StatusBarSection.Left,
            Icon = "⊞", Text = "",
            Command = vm.ToggleGridCommand,
            PageId = "studio", SortOrder = 26
        });
    }

    // ── Studio page — right section ─────────────────────────────────────

    private static void RegisterStudioRight(StatusBarRegistry r, MainViewModel vm)
    {
        // Memory usage — text + progress bar
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-memory", Section = StatusBarSection.Right,
            GetLiveText = ds => $"Memory:  {((MainViewModel)ds!).MemoryUsagePercent}%",
            GetProgress = ds => ((MainViewModel)ds!).MemoryUsagePercent / 100.0,
            ObservedProperties = [nameof(MainViewModel.MemoryUsagePercent)],
            PageId = "studio", SortOrder = 0
        });

        // Settings gear
        r.Register(new StatusBarItemDefinition
        {
            Id = "studio-settings", Section = StatusBarSection.Right,
            Icon = "⚙", Text = "",
            Command = vm.ShowSettingsCommand,
            PageId = "studio", SortOrder = 10
        });
    }
}
