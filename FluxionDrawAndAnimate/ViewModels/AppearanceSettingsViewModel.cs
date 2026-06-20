using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluxionDrawAndAnimate.ViewModels;

/// <summary>Observable state for the Appearance settings page.</summary>
public sealed partial class AppearanceSettingsViewModel : ObservableObject
{
    // ── Theme ────────────────────────────────────────────────────────────

    public IReadOnlyList<string> ThemeOptions { get; } = ["Dark", "Light", "System"];
    [ObservableProperty] private string _appTheme = "Dark";

    public IReadOnlyList<string> EditorSurfaceOptions { get; } = ["Deep Graphite", "Charcoal", "Midnight", "Obsidian"];
    [ObservableProperty] private string _editorSurface = "Deep Graphite";

    [ObservableProperty] private bool _windowTransparency = true;

    // ── Accent & Color ───────────────────────────────────────────────────

    public IReadOnlyList<string> AccentColorOptions { get; } = ["Purple", "Blue", "Cyan", "Green", "Amber", "Rose"];
    [ObservableProperty] private string _accentColorName = "Purple";

    [ObservableProperty] private double _accentIntensity = 65;

    // Hex values matching label order in AccentColorOptions
    public IReadOnlyList<string> HighlightSwatchColors { get; } =
        ["#7C3AED", "#3B82F6", "#06B6D4", "#10B981", "#F59E0B", "#F43F5E"];
    [ObservableProperty] private string _selectionHighlightHex = "#7C3AED";

    // ── Layout Density ───────────────────────────────────────────────────

    public IReadOnlyList<string> DensityOptions { get; } = ["Comfortable", "Compact", "Spacious"];
    [ObservableProperty] private string _uiDensity = "Comfortable";

    public IReadOnlyList<string> SidebarSizeOptions { get; } = ["Narrow", "Normal", "Wide"];
    [ObservableProperty] private string _sidebarSizeOption = "Normal";

    [ObservableProperty] private double _panelCornerRadius = 14;

    // ── Text & Icons ─────────────────────────────────────────────────────

    [ObservableProperty] private double _fontScalePercent = 100;

    public IReadOnlyList<string> IconStyleOptions { get; } = ["Outlined", "Filled", "Sharp"];
    [ObservableProperty] private string _iconStyle = "Outlined";

    [ObservableProperty] private bool _showToolLabels = true;

    public void ResetToDefaults()
    {
        AppTheme = "Dark";
        EditorSurface = "Deep Graphite";
        WindowTransparency = true;
        AccentColorName = "Purple";
        AccentIntensity = 65;
        SelectionHighlightHex = "#7C3AED";
        UiDensity = "Comfortable";
        SidebarSizeOption = "Normal";
        PanelCornerRadius = 14;
        FontScalePercent = 100;
        IconStyle = "Outlined";
        ShowToolLabels = true;
    }
}
