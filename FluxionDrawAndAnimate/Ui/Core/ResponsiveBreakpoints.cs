namespace FluxionDrawAndAnimate.Ui.Core;

/// <summary>
/// Single source of truth for the width thresholds that drive responsive
/// layout across the whole app (project manager and editor alike).
/// </summary>
public static class ResponsiveBreakpoints
{
    /// <summary>Below this width the UI uses the touch-first Phone layout.</summary>
    public const double PhoneMaxWidth = 600;

    /// <summary>Below this width the UI uses the compact Tablet layout.</summary>
    public const double TabletMaxWidth = 1024;

    public static ResponsiveProfileKind FromWidth(double width)
    {
        if (width <= 0)
        {
            return ResponsiveProfileKind.Studio;
        }

        if (width < PhoneMaxWidth)
        {
            return ResponsiveProfileKind.Phone;
        }

        if (width < TabletMaxWidth)
        {
            return ResponsiveProfileKind.Tablet;
        }

        return ResponsiveProfileKind.Studio;
    }
}
