namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>Per-dab parameters after sensor evaluation — what the rasterizer actually uses.</summary>
public readonly record struct DabParameters(double Radius, double Opacity);
