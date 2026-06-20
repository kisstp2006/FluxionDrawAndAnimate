using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Optris.Icons.Avalonia;
using Optris.Icons.Avalonia.MaterialDesign;
using FluxionDrawAndAnimate.Core.Persistence;
using FluxionDrawAndAnimate.Core.Editing;
using FluxionDrawAndAnimate.Core.Projects;
using FluxionDrawAndAnimate.Presentation;
using FluxionDrawAndAnimate.Services;
using FluxionDrawAndAnimate.ViewModels;
using FluxionDrawAndAnimate.Views;

namespace FluxionDrawAndAnimate;

public partial class App : Application
{
    public override void Initialize()
    {
        IconProvider.Current.Register<MaterialDesignIconProvider>();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            window.DataContext = CreateMainViewModel(() => window);
            desktop.MainWindow = window;
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () =>
            {
                var view = new MainView();
                view.DataContext = CreateMainViewModel(() => TopLevel.GetTopLevel(view));
                return view;
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var view = new MainView();
            view.DataContext = CreateMainViewModel(() => TopLevel.GetTopLevel(view));
            singleViewPlatform.MainView = view;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainViewModel CreateMainViewModel(Func<TopLevel?> getTopLevel)
    {
        var archiveStore = new ProjectArchiveStore();
        var fileService = new AvaloniaProjectFileService(getTopLevel, archiveStore);
        var settingsRegistry = new AppSettingsRegistryFactory().CreateDefaultRegistry();

        var vm = new MainViewModel(
            new DefaultProjectFactory(),
            new TimelineEditingService(),
            new ToolPaletteFactory(),
            new ProjectPresetFactory(),
            new ProjectCardFactory(),
            fileService,
            new JsonRecentProjectStore(),
            new AvaloniaProjectThumbnailService(),
            settingsRegistry,
            new SettingEditorResolver(),
            new JsonUserSettingsStore());

        vm.SetFavoritesStore(new JsonFavoritesStore());

        // Inject the platform-specific shell (immersive / fullscreen controller).
        // On Android, MainActivity.OnCreate has already installed a real
        // AndroidPlatformShell into PlatformShellProvider.Current before this
        // method runs. On every other platform the provider lazily returns a
        // NullPlatformShell that no-ops every call.
        vm.SetPlatformShell(PlatformShellProvider.Current);

        return vm;
    }
}
