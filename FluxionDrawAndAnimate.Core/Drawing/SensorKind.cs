namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// Which input channel drives a brush dynamics binding.
/// B/3 ships Pressure, TiltX, TiltY.
/// B/4+ adds Speed, Distance, Fade, DrawingAngle.
/// </summary>
public enum SensorKind
{
    Pressure,  // 0..1  stylus/touch pressure; 1.0 fallback for mouse
    TiltX,     // -1..1 stylus X-tilt, remapped to 0..1 inside Evaluate
    TiltY,     // -1..1 stylus Y-tilt, remapped to 0..1 inside Evaluate
}
