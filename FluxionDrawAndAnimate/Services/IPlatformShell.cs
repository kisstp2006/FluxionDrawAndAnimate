namespace FluxionDrawAndAnimate.Services;

/// <summary>
/// Cross-platform abstraction for native "shell" concerns that differ between
/// desktop, browser, iOS and Android — most notably immersive / fullscreen
/// modes that hide the system status bar and navigation bar on mobile.
///
/// The contract is intentionally tiny: the shared <c>MainViewModel</c> calls
/// <see cref="ApplyImmersive"/> whenever the responsive layout transitions to
/// or from the phone profile, and the per-platform implementation decides what
/// (if anything) that means.
///
/// Pattern: follows the existing <c>IProjectFileService</c> /
/// <c>AvaloniaProjectFileService</c> / <c>NullProjectFileService</c> split —
/// interface in the shared project, real implementation in a platform project,
/// no-op fallback in the shared project for platforms without native shell.
/// </summary>
public interface IPlatformShell
{
    /// <summary>
    /// Current platform identifier, used for diagnostics and optional
    /// platform-specific UI tweaks. Never used for branching gameplay logic.
    /// </summary>
    PlatformKind Platform { get; }

    /// <summary>
    /// Whether this platform supports an immersive (chrome-hiding) mode at all.
    /// Desktop, browser and iOS report <c>false</c>; Android reports <c>true</c>.
    /// Lets the caller skip work without a try/catch.
    /// </summary>
    bool SupportsImmersive { get; }

    /// <summary>
    /// Enable or disable immersive mode on the current platform.
    ///
    /// On Android this toggles sticky immersive mode (hides the system status
    /// and navigation bars; they reappear momentarily on swipe). On every other
    /// platform this is a no-op.
    ///
    /// Called by <c>MainViewModel</c> when the layout transitions between phone
    /// and studio profiles so the canvas gets maximum screen real estate on
    /// phones, and desktop keeps its normal window chrome.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enter immersive mode, <c>false</c> to leave it.</param>
    void ApplyImmersive(bool enabled);
}

/// <summary>
/// Coarse platform identifier. Kept minimal — only the granularity the shell
/// actually needs for branching behaviour.
/// </summary>
public enum PlatformKind
{
    Unknown,
    Desktop,
    Browser,
    Android,
    iOS
}
