namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// All per-sample data from an input device for one point along a stroke.
/// Replaces the earlier flat StrokePoint — carries pressure, tilt and timing so
/// the brush engine can map them to visual parameters (B/3 SensorStack).
/// Mouse / touch fall back: Pressure=1, Tilt=0, Rotation=0.
/// </summary>
public readonly record struct PaintInformation(
    double X,
    double Y,
    double Pressure,     // 0..1   (stylus/touch), 1.0 fallback for mouse
    double TiltX,        // -1..1  (X-tilt in normalised units, from raw degrees/90)
    double TiltY,        // -1..1
    double Rotation,     // 0..2π  stylus barrel rotation in radians
    double TimestampMs)  // milliseconds from app start, used by Speed sensor (B/3)
{
    public static PaintInformation FromMouse(double x, double y) =>
        new(x, y, 1.0, 0, 0, 0, 0);
}
