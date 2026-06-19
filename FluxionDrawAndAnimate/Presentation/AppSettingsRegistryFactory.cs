using FluxionDrawAndAnimate.Core.Settings;

namespace FluxionDrawAndAnimate.Presentation;

public sealed class AppSettingsRegistryFactory
{
    public SettingRegistry CreateDefaultRegistry()
    {
        var registry = new SettingRegistry();

        registry.Register(
            AppSettingKeys.CreatorTheme,
            "App theme",
            ProjectCreatorThemeMode.Dark,
            setting => setting
                .InGroup("Interface")
                .WithDescription("Shared theme used by Phone, Tablet, Studio, and the editor workspace.")
                .AddChoice(ProjectCreatorThemeMode.Dark, "Dark")
                .AddChoice(ProjectCreatorThemeMode.Light, "Light"));

        registry.Register(
            AppSettingKeys.ShowOnionSkin,
            "Onion skin",
            true,
            setting => setting
                .InGroup("Canvas")
                .WithDescription("Show neighboring animation frames while drawing."));

        registry.Register(
            AppSettingKeys.DefaultFrameRate,
            "Default FPS",
            24,
            setting => setting
                .InGroup("Timeline")
                .WithDescription("Default frame rate used by new animation projects.")
                .WithRange(1, 120, 1));

        registry.Register(
            AppSettingKeys.TileMemoryBudget,
            "Tile memory budget",
            128,
            setting => setting
                .InGroup("Performance")
                .WithDescription("Memory budget for tile-backed drawing cache on weaker devices.")
                .WithRange(32, 2048, 32));

        return registry;
    }
}
