using System;
using Android.App;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using FluxionDrawAndAnimate.Services;

namespace FluxionDrawAndAnimate.Android;

/// <summary>
/// Android implementation of <see cref="IPlatformShell"/>.
///
/// Toggles <b>sticky immersive mode</b> on the activity's window: when enabled,
/// the system status bar and navigation bar are hidden and only reappear
/// momentarily on a swipe from the edge. This maximises canvas real estate on
/// phones without breaking access to system gestures.
///
/// Uses two API-level paths so we cover every supported Android version
/// (<c>SupportedOSPlatformVersion = 23</c>):
///  - API 30+ (<c>R</c>): the per-window <c>WindowInsetsController</c> via
///    <c>WindowCompat</c> / <c>WindowInsetsControllerCompat</c>.
///  - API &lt; 30: the legacy <c>View.SystemUiFlags</c> flags with a
///    re-asserting listener that re-applies immersive mode after the user
///    dismisses a transient bar.
///
/// Only depends on <c>Xamarin.AndroidX.Core</c> (already pulled in transitively
/// by Avalonia.Android), so no new package reference is required.
/// </summary>
public sealed class AndroidPlatformShell : IPlatformShell
{
    private readonly Activity _activity;
    private View? _decorView;

    public AndroidPlatformShell(Activity activity)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    public PlatformKind Platform => PlatformKind.Android;

    public bool SupportsImmersive => true;

    public void ApplyImmersive(bool enabled)
    {
        // Marshal onto the UI thread — ApplyImmersive may be invoked from the
        // Avalonia render thread when the layout transitions.
        _activity.RunOnUiThread(() => ApplyImmersiveInternal(enabled));
    }

    private void ApplyImmersiveInternal(bool enabled)
    {
        var window = _activity.Window;
        if (window is null) return;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            // Modern path (API 30+): WindowInsetsController.
            // WindowCompat tells the framework not to fit system insets into the
            // content view, so the canvas can extend edge-to-edge.
            WindowCompat.SetDecorFitsSystemWindows(window, !enabled);

            // GetWindowInsetsController lives on ViewCompat in the .NET Android
            // binding (it accepts the decor view, which is the root of the window).
            // Marked deprecated because the platform prefers the native
            // Window.InsetsController on API 30+, but the compat shim is the
            // safest cross-API surface and we only call it on API 30+ anyway.
            _decorView ??= window.DecorView;
#pragma warning disable CS0618 // ViewCompat.GetWindowInsetsController is deprecated
            var controller = _decorView is null
                ? null
                : ViewCompat.GetWindowInsetsController(_decorView);
#pragma warning restore CS0618
            if (controller is null) return;

            if (enabled)
            {
                controller.Hide(WindowInsetsCompat.Type.SystemBars());
                controller.SystemBarsBehavior =
                    WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
            }
            else
            {
                controller.Show(WindowInsetsCompat.Type.SystemBars());
            }
        }
        else
        {
            // Legacy path (API < 30): SystemUiFlags on the decor view.
            // Resolve and cache the decor view lazily.
            _decorView ??= window.DecorView;
            if (_decorView is null) return;

            // Detach any previous listener before re-applying.
#pragma warning disable CA1422 // SystemUiFlags deprecated on API 30+; this branch only runs on API < 30
            _decorView.SystemUiVisibilityChange -= OnSystemUiVisibilityChange;

            // SystemUiFlags is a [Flags] enum; cast to int for the flag
            // composition, then back to StatusBarVisibility (the property type).
            const SystemUiFlags immersiveSticky =
                SystemUiFlags.LayoutStable
                | SystemUiFlags.LayoutHideNavigation
                | SystemUiFlags.LayoutFullscreen
                | SystemUiFlags.HideNavigation
                | SystemUiFlags.ImmersiveSticky;

            const SystemUiFlags reset =
                SystemUiFlags.LayoutStable
                | SystemUiFlags.LayoutFullscreen;

            _decorView.SystemUiFlags = enabled ? immersiveSticky : reset;

            if (enabled)
            {
                // Re-apply on visibility change so transient bars that the user
                // swiped in are hidden again after a short delay.
                _decorView.SystemUiVisibilityChange += OnSystemUiVisibilityChange;
            }
#pragma warning restore CA1422
        }
    }

    private void OnSystemUiVisibilityChange(object? sender, View.SystemUiVisibilityChangeEventArgs e)
    {
        // Re-assert immersive mode when the user has dismissed a transient bar.
        // e.Visibility is StatusBarVisibility; cast to SystemUiFlags to test flags.
        if (((SystemUiFlags)(int)e.Visibility & SystemUiFlags.HideNavigation) == 0)
        {
            _decorView?.Post(() => ApplyImmersiveInternal(true));
        }
    }
}
