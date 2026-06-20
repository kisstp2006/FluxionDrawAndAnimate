namespace FluxionDrawAndAnimate.Services;

/// <summary>
/// Process-wide holder for the active <see cref="IPlatformShell"/>.
///
/// On Android, <c>MainActivity.OnCreate</c> sets <see cref="Current"/> to a
/// real <c>AndroidPlatformShell</c> before the Avalonia lifetime starts, so
/// <c>App.OnFrameworkInitializationCompleted</c> can inject it into
/// <c>MainViewModel</c>. On every other platform the provider lazily falls
/// back to <see cref="NullPlatformShell"/>, which is a safe no-op.
///
/// This indirection keeps the shared project free of any Android SDK
/// reference: <c>App</c> only ever sees the <see cref="IPlatformShell"/>
/// contract, and the real implementation is swapped in from the platform
/// project at runtime.
/// </summary>
public static class PlatformShellProvider
{
    private static IPlatformShell? _current;

    /// <summary>
    /// The active platform shell. Never <c>null</c>: if no platform project
    /// has registered one, a <see cref="NullPlatformShell"/> is returned.
    /// </summary>
    public static IPlatformShell Current
    {
        get => _current ??= new NullPlatformShell();
        set => _current = value;
    }
}
