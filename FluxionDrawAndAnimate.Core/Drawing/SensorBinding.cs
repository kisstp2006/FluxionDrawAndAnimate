namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>Wires one input sensor to a brush parameter via a response curve.</summary>
public sealed class SensorBinding
{
    public SensorKind Sensor { get; set; } = SensorKind.Pressure;
    public ResponseCurve Curve { get; set; } = ResponseCurve.Linear();

    /// <summary>Extract the normalised (0..1) sensor value from a paint sample.</summary>
    public double ReadSensor(PaintInformation info) => Sensor switch
    {
        SensorKind.Pressure => info.Pressure,
        SensorKind.TiltX    => (info.TiltX + 1.0) / 2.0,
        SensorKind.TiltY    => (info.TiltY + 1.0) / 2.0,
        _                   => 1.0
    };

    public double Evaluate(PaintInformation info) => Curve.Evaluate(ReadSensor(info));
}
