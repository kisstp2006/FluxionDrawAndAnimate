using System.Collections.Generic;
using FluxionDrawAndAnimate.Ui.Settings.Pages;

namespace FluxionDrawAndAnimate.Presentation.Settings;

public static class SettingsCategoryRegistrar
{
    public static IReadOnlyList<SettingsCategoryDefinition> CreateDefault() =>
    [
        new() { Id = "general",     Title = "General",      Icon = "mdi-tune-variant",
                SortOrder = 0, CreatePage = () => new StubSettingsPage("General",
                    "Startup behaviour, language, autosave and recent files.") },

        new() { Id = "appearance",  Title = "Appearance",   Icon = "mdi-palette-outline",
                SortOrder = 1, CreatePage = () => new AppearanceSettingsPage() },

        new() { Id = "canvas",      Title = "Canvas",       Icon = "mdi-vector-square",
                SortOrder = 2, CreatePage = () => new StubSettingsPage("Canvas",
                    "Default background, grid, rulers, snapping and checkerboard.") },

        new() { Id = "drawing",     Title = "Drawing",      Icon = "mdi-lead-pencil",
                SortOrder = 3, CreatePage = () => new StubSettingsPage("Drawing",
                    "Brush cursor, stabilizer, smoothing, pressure curve and color picker.") },

        new() { Id = "input",       Title = "Input",        Icon = "mdi-gesture-tap",
                SortOrder = 4, CreatePage = () => new StubSettingsPage("Input",
                    "Mouse, touch, stylus, gestures and zoom / pan / rotate sensitivity.") },

        new() { Id = "performance", Title = "Performance",  Icon = "mdi-speedometer",
                SortOrder = 5, CreatePage = () => new StubSettingsPage("Performance",
                    "GPU acceleration, memory / cache limits and large-canvas mode.") },

        new() { Id = "files",       Title = "Files & Sync", Icon = "mdi-folder-sync-outline",
                SortOrder = 6, CreatePage = () => new StubSettingsPage("Files & Sync",
                    "Project folder, autosave, backups and cloud sync.") },

        new() { Id = "shortcuts",   Title = "Shortcuts",    Icon = "mdi-keyboard-outline",
                SortOrder = 7, CreatePage = () => new StubSettingsPage("Shortcuts",
                    "Keyboard shortcuts and shortcut profile management.") },

        new() { Id = "about",       Title = "About",        Icon = "mdi-information-outline",
                SortOrder = 8, CreatePage = () => new StubSettingsPage("About",
                    "Version, credits, open-source licences and build info.") },
    ];
}
