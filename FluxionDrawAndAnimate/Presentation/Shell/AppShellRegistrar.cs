using FluxionDrawAndAnimate.Ui.Pages;
using FluxionDrawAndAnimate.ViewModels;

namespace FluxionDrawAndAnimate.Presentation.Shell;

/// <summary>
/// Registers the default pages, menus and title-bar actions.
/// Commands are wired to the live <see cref="MainViewModel"/> so they are
/// never stale. Feature modules call Register* on the same registry to add
/// their own entries without touching this file.
/// </summary>
public static class AppShellRegistrar
{
    public static void RegisterDefaults(AppShellRegistry registry, MainViewModel vm)
    {
        RegisterPages(registry);
        RegisterMenus(registry, vm);
        RegisterActions(registry, vm);
    }

    // ------------------------------------------------------------------

    private static void RegisterPages(AppShellRegistry r)
    {
        r.RegisterPage(new PageDefinition
        {
            Id = "home", Title = "Home", SortOrder = 0,
            CreateView = () => new HomePage()
        });
        r.RegisterPage(new PageDefinition
        {
            Id = "studio", Title = "Studio", SortOrder = 1,
            CreateView = () => new StudioPage()
        });
        r.RegisterPage(new PageDefinition
        {
            Id = "animate", Title = "Animate", SortOrder = 2,
            CreateView = () => new AnimatePage()
        });
    }

    private static void RegisterMenus(AppShellRegistry r, MainViewModel vm)
    {
        r.RegisterMenu(new MenuDefinition
        {
            Title = "File", SortOrder = 0,
            Items =
            [
                new() { Title = "New Project",  Command = vm.CreateProjectCommand,  SortOrder = 0 },
                new() { Title = "Open…",        Command = vm.OpenProjectCommand,    SortOrder = 1 },
                new() { IsSeparator = true,                                         SortOrder = 2 },
                new() { Title = "Save",         Command = vm.SaveProjectCommand,    ShortcutText = "Ctrl+S",        SortOrder = 3 },
                new() { Title = "Save As…",     Command = vm.SaveProjectAsCommand,  ShortcutText = "Ctrl+Shift+S",  SortOrder = 4 },
            ]
        });

        r.RegisterMenu(new MenuDefinition
        {
            Title = "Edit", SortOrder = 1,
            Items =
            [
                new() { Title = "Undo",     Command = vm.UndoCommand,   ShortcutText = "Ctrl+Z", SortOrder = 0 },
                new() { Title = "Redo",     Command = vm.RedoCommand,   ShortcutText = "Ctrl+Y", SortOrder = 1 },
                new() { IsSeparator = true,                                                       SortOrder = 2 },
                new() { Title = "Settings", Command = vm.ShowSettingsCommand,                     SortOrder = 3 },
            ]
        });

        r.RegisterMenu(new MenuDefinition
        {
            Title = "View", SortOrder = 2,
            Items =
            [
                new() { Title = "Toggle Layout", Command = vm.ToggleWorkspaceModeCommand, SortOrder = 0 },
            ]
        });

        r.RegisterMenu(new MenuDefinition
        {
            Title = "Help", SortOrder = 3,
            Items =
            [
                new() { Title = "About Fluxion", SortOrder = 0 },
            ]
        });
    }

    private static void RegisterActions(AppShellRegistry r, MainViewModel vm)
    {
        r.RegisterAction(new TitleBarActionDefinition
        {
            Id = "new-project", Title = "+ New Project",
            Command = vm.CreateProjectCommand,
            IsPrimary = true, SortOrder = 0
        });
        r.RegisterAction(new TitleBarActionDefinition
        {
            Id = "open", Title = "Open",
            Command = vm.OpenProjectCommand,
            SortOrder = 1
        });
        r.RegisterAction(new TitleBarActionDefinition
        {
            Id = "settings", Title = "⚙",
            Command = vm.ShowSettingsCommand,
            SortOrder = 10
        });
    }
}
