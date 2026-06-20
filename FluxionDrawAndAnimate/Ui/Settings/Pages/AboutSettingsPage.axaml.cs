using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FluxionDrawAndAnimate.Ui.Settings.Pages;

public partial class AboutSettingsPage : UserControl
{
    public AboutSettingsPage()
    {
        InitializeComponent();
    }

    // ── Platform info (bound from AXAML via ElementName=root) ──────────

    public string CurrentPlatform =>
        OperatingSystem.IsWindows() ? "Windows" :
        OperatingSystem.IsLinux()   ? "Linux" :
        OperatingSystem.IsMacOS()   ? "macOS" :
        OperatingSystem.IsAndroid() ? "Android" :
        OperatingSystem.IsIOS()     ? "iOS" :
        OperatingSystem.IsBrowser() ? "WebAssembly / Browser" :
        "Unknown";

    public string CurrentArch => RuntimeInformation.ProcessArchitecture.ToString();

    public string CurrentRuntime => RuntimeInformation.FrameworkDescription;

    public string OsDescription => RuntimeInformation.OSDescription;

    // ── URL opener ───────────────────────────────────────────────────────

    private async void OnWebsiteClicked(object? sender, RoutedEventArgs e)
    {
        var launcher = TopLevel.GetTopLevel(this)?.Launcher;
        if (launcher is not null)
        {
            await launcher.LaunchUriAsync(new Uri(AppInfo.Website));
        }
    }
}
