namespace FluxionDrawAndAnimate.Core.Drawing;

public sealed class StrokePath
{
    public StrokePath(RgbaColor color, double size, ToolKind toolKind, bool isVector)
        : this(color, size, toolKind, isVector, BrushSettings.ForTool(toolKind))
    {
    }

    public StrokePath(RgbaColor color, double size, ToolKind toolKind, bool isVector, BrushSettings brushSettings)
    {
        Color = color;
        Size = size;
        ToolKind = toolKind;
        IsVector = isVector;
        BrushSettings = brushSettings;
    }

    public RgbaColor Color { get; }
    public double Size { get; }
    public ToolKind ToolKind { get; }
    public bool IsVector { get; }
    public BrushSettings BrushSettings { get; }
    public List<PaintInformation> Points { get; } = new();
}
