namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// A fully described, serialisable brush configuration.
/// Loaded from JSON resources; replaces the hardcoded BrushSettings.ForTool() defaults.
/// B/5 adds user-created presets and a thumbnail.
/// </summary>
public sealed class BrushPreset
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public string Description { get; init; } = "";
    public string Icon { get; init; } = "●";
    public ToolKind ToolKind { get; init; } = ToolKind.Pencil;
    public double DefaultSize { get; init; } = 10.0;
    public int SortOrder { get; init; } = 0;
    public BrushSettings Settings { get; init; } = new();
}
