using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using FluxionDrawAndAnimate.ViewModels;

namespace FluxionDrawAndAnimate.Services;

/// <summary>
/// Single source of truth for applying <see cref="AppearanceSettingsViewModel"/>
/// to the live Avalonia resource dictionary.  All screens (Studio, Phone, Home)
/// inherit the same resource tokens — no per-layout duplicates needed.
///
/// Call <see cref="ApplyAll"/> once on startup and subscribe to
/// AppearanceSettings.PropertyChanged to call it again on any change.
/// </summary>
public static class AppearanceApplier
{
    // ── Accent colour presets ─────────────────────────────────────────────
    private static readonly Dictionary<string, Color> AccentColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Purple"] = Color.Parse("#7C3AED"),
        ["Blue"]   = Color.Parse("#3B82F6"),
        ["Cyan"]   = Color.Parse("#06B6D4"),
        ["Green"]  = Color.Parse("#10B981"),
        ["Amber"]  = Color.Parse("#F59E0B"),
        ["Rose"]   = Color.Parse("#F43F5E"),
    };

    // ── Editor surface presets (dark-mode background overrides) ──────────
    private static readonly Dictionary<string, (string Page, string Panel, string Card, string Elevated, string Input, string Border)>
        Surfaces = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Deep Graphite"] = ("#111419", "#171B22", "#1E2530", "#252B35", "#141A21", "#333C49"),
        ["Charcoal"]      = ("#141618", "#1C1E21", "#23262B", "#2A2D34", "#161A1D", "#3A3D44"),
        ["Midnight"]      = ("#0D0F14", "#141720", "#1B1F2D", "#22273A", "#0F1219", "#2E3449"),
        ["Obsidian"]      = ("#13131A", "#1A1A25", "#222232", "#2A2A3F", "#10101A", "#333344"),
    };

    // ── Entry point ───────────────────────────────────────────────────────

    public static void ApplyAll(AppearanceSettingsViewModel s)
    {
        ApplyTheme(s.AppTheme);
        ApplyAccent(s.AccentColorName, s.AccentIntensity / 100.0);
        ApplyEditorSurface(s.EditorSurface, s.WindowTransparency, s.AppTheme);
        ApplyCornerRadius(s.PanelCornerRadius);
        ApplySidebarSize(s.SidebarSizeOption);
        ApplyDensity(s.UiDensity);
        ApplyFontScale(s.FontScalePercent / 100.0);
    }

    // ── Per-setting appliers ──────────────────────────────────────────────

    public static void ApplyTheme(string theme)
    {
        if (Application.Current is null) return;
        Application.Current.RequestedThemeVariant = theme switch
        {
            "Light"  => ThemeVariant.Light,
            "System" => ThemeVariant.Default,
            _        => ThemeVariant.Dark,
        };
    }

    public static void ApplyAccent(string colorName, double intensity)
    {
        if (!AccentColors.TryGetValue(colorName, out var color))
        {
            color = Color.Parse("#7C3AED");
        }

        var adjusted = AdjustIntensity(color, Math.Clamp(intensity, 0.2, 1.0));
        Set("FluxAccent", new SolidColorBrush(adjusted));
        Set("FluxAccentText", new SolidColorBrush(Colors.White));
    }

    public static void ApplyEditorSurface(string surface, bool transparent, string theme)
    {
        // Surface overrides only make sense in Dark mode; in Light/System let
        // the ThemeDictionary define the background colours.
        if (!string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            RemoveOverride("FluxBgPage");
            RemoveOverride("FluxBgPanel");
            RemoveOverride("FluxBgCard");
            RemoveOverride("FluxBgElevated");
            RemoveOverride("FluxBgInput");
            RemoveOverride("FluxBorder");
            return;
        }

        if (!Surfaces.TryGetValue(surface, out var s))
        {
            s = Surfaces["Deep Graphite"];
        }

        var panelAlpha = transparent ? (byte)224 : (byte)255;
        Set("FluxBgPage",     Brush(s.Page,     255));
        Set("FluxBgPanel",    Brush(s.Panel,    panelAlpha));
        Set("FluxBgCard",     Brush(s.Card,     panelAlpha));
        Set("FluxBgElevated", Brush(s.Elevated, 255));
        Set("FluxBgInput",    Brush(s.Input,    255));
        Set("FluxBorder",     Brush(s.Border,   255));
    }

    public static void ApplyCornerRadius(double px)
    {
        var r = Math.Clamp(px, 0, 28);
        Set("FluxCornerRadius",   new CornerRadius(r));
        Set("FluxCornerRadiusMd", new CornerRadius(Math.Max(0, r - 2)));
        Set("FluxCornerRadiusSm", new CornerRadius(Math.Max(0, r - 4)));
    }

    public static void ApplySidebarSize(string size)
    {
        double w = size switch
        {
            "Narrow" => 180.0,
            "Wide"   => 260.0,
            _        => 220.0,
        };
        Set("FluxHomeSidebarWidth", w);
    }

    public static void ApplyDensity(string density)
    {
        double sp = density switch
        {
            "Compact"  => 8.0,
            "Spacious" => 18.0,
            _          => 12.0,
        };
        Set("FluxDensitySpacing", sp);
    }

    public static void ApplyFontScale(double scale)
    {
        scale = Math.Clamp(scale, 0.75, 1.5);
        Set("FluxFontSizeXS", Math.Round(11.0 * scale, 1));
        Set("FluxFontSizeSM", Math.Round(12.0 * scale, 1));
        Set("FluxFontSizeMD", Math.Round(14.0 * scale, 1));
        Set("FluxFontSizeLG", Math.Round(16.0 * scale, 1));
        Set("FluxFontSizeXL", Math.Round(20.0 * scale, 1));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void Set(string key, object? value)
    {
        if (Application.Current?.Resources is not { } res) return;
        res[key] = value;
    }

    private static void RemoveOverride(string key)
    {
        Application.Current?.Resources.Remove(key);
    }

    private static IBrush Brush(string hex, byte alpha)
    {
        var c = Color.Parse(hex);
        return new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
    }

    // Blend colour toward gray to reduce apparent intensity without changing hue.
    private static Color AdjustIntensity(Color color, double intensity)
    {
        if (intensity >= 1.0) return color;
        var gray = (byte)(color.R * 0.299 + color.G * 0.587 + color.B * 0.114);
        return Color.FromArgb(
            color.A,
            (byte)(color.R * intensity + gray * (1 - intensity)),
            (byte)(color.G * intensity + gray * (1 - intensity)),
            (byte)(color.B * intensity + gray * (1 - intensity)));
    }
}
