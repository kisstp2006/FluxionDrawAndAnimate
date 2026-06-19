using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Presentation;

/// <summary>
/// UI-facing tool entry. Wraps a <see cref="BrushPreset"/> (loaded from JSON in B/5)
/// plus the per-tool color and vector flag that the palette editor exposes.
/// </summary>
public sealed class ToolPreset
{
    public ToolPreset(string name, string icon, ToolKind kind, RgbaColor color, double size, bool isVector = false)
        : this(name, icon, kind, color, size, isVector, brushPreset: null)
    {
    }

    public ToolPreset(
        string name,
        string icon,
        ToolKind kind,
        RgbaColor color,
        double size,
        bool isVector,
        BrushPreset? brushPreset)
    {
        Name = name;
        Icon = icon;
        Kind = kind;
        Color = color;
        Size = size;
        IsVector = isVector;
        BrushPreset = brushPreset;
    }

    public string Name { get; }
    public string Icon { get; }
    public ToolKind Kind { get; }
    public RgbaColor Color { get; }
    public double Size { get; }
    public bool IsVector { get; }

    /// <summary>
    /// The file-described brush configuration (B/5). Null for tools that have no
    /// brush engine backing (e.g. a pure select tool); the canvas falls back to
    /// <see cref="BrushSettings.ForTool"/> in that case.
    /// </summary>
    public BrushPreset? BrushPreset { get; }

    /// <summary>The effective brush settings for this tool: the preset's, or the per-tool fallback.</summary>
    public BrushSettings EffectiveSettings => BrushPreset?.Settings ?? BrushSettings.ForTool(Kind);
}
