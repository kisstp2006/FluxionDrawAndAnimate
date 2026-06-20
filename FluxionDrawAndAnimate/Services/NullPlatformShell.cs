namespace FluxionDrawAndAnimate.Services;

/// <summary>
/// Default <see cref="IPlatformShell"/> for platforms that do not have a native
/// immersive mode to toggle: desktop (Windows/Linux/macOS), browser (WASM) and
/// iOS (where fullscreen is handled via Info.plist and is not toggled at runtime).
///
/// Every member is a safe no-op so the shared <c>MainViewModel</c> can call
/// <see cref="ApplyImmersive"/> unconditionally without caring about the host.
///
/// The real immersive implementation lives in
/// <c>FluxionDrawAndAnimate.Android/AndroidPlatformShell.cs</c> and is injected
/// from <c>App.OnFrameworkInitializationCompleted</c> on Android only.
/// </summary>
public sealed class NullPlatformShell : IPlatformShell
{
    public PlatformKind Platform => PlatformKind.Desktop;

    public bool SupportsImmersive => false;

    public void ApplyImmersive(bool enabled)
    {
        // Intentionally empty: desktop / browser / iOS have no runtime-toggled
        // immersive chrome. Desktop keeps window chrome; iOS fullscreen is set
        // via Info.plist at build time; browser is sandboxed.
    }
}
