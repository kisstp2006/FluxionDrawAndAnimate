using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using FluxionDrawAndAnimate.Services;

namespace FluxionDrawAndAnimate.Android;

[Activity(
    Label = "FluxionDrawAndAnimate.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    /// <summary>
    /// The platform shell created for this activity. Set in <see cref="OnCreate"/>
    /// so <c>App.OnFrameworkInitializationCompleted</c> can pick it up via
    /// <see cref="PlatformShellProvider.Current"/> before the Avalonia view is built.
    /// </summary>
    public IPlatformShell? PlatformShell { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Register the Android-specific immersive shell before the Avalonia
        // view tree is constructed so App.OnFrameworkInitializationCompleted
        // can inject it into MainViewModel.
        PlatformShell = new AndroidPlatformShell(this);
        PlatformShellProvider.Current = PlatformShell;
    }

    public override void OnTrimMemory(TrimMemory level)
    {
        base.OnTrimMemory(level);

        if (level is TrimMemory.RunningCritical or TrimMemory.Complete)
        {
            MemoryPressureService.Report(MemoryPressureLevel.Critical);
            return;
        }

        if (level is TrimMemory.RunningLow
            or TrimMemory.RunningModerate
            or TrimMemory.Background
            or TrimMemory.Moderate)
        {
            MemoryPressureService.Report(MemoryPressureLevel.Moderate);
        }
    }

    public override void OnLowMemory()
    {
        base.OnLowMemory();
        MemoryPressureService.Report(MemoryPressureLevel.Critical);
    }
}
