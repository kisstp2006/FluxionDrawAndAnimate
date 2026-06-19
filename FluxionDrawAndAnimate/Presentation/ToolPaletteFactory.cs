using System.Collections.Generic;
using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Presentation;

/// <summary>
/// Builds the tool palette. Since B/5 the brush descriptions come from the
/// <see cref="IBrushLibrary"/> (embedded JSON resources), so adding or tuning a
/// brush is a data edit, not a code change. The factory maps each preset to a
/// <see cref="ToolPreset"/> with a sensible default color and the vector flag
/// derived from the preset's <see cref="ToolKind"/>.
/// </summary>
public sealed class ToolPaletteFactory
{
    private readonly IBrushLibrary _brushLibrary;

    public ToolPaletteFactory() : this(BrushPresetLibrary.Default)
    {
    }

    public ToolPaletteFactory(IBrushLibrary brushLibrary)
    {
        _brushLibrary = brushLibrary;
    }

    public IReadOnlyList<ToolPreset> CreateDefaultTools()
    {
        var tools = new List<ToolPreset>();

        foreach (var preset in _brushLibrary.BuiltIn)
        {
            tools.Add(BuildToolFromPreset(preset));
        }

        return tools;
    }

    private static ToolPreset BuildToolFromPreset(BrushPreset preset)
    {
        var (color, isVector) = preset.ToolKind switch
        {
            ToolKind.Pencil => (RgbaColor.FromRgb(38, 48, 66), false),
            ToolKind.Ink    => (RgbaColor.FromRgb(20, 22, 28), false),
            ToolKind.Paint  => (RgbaColor.FromRgb(255, 176, 37), false),
            ToolKind.Eraser => (RgbaColor.White, false),
            ToolKind.Vector => (RgbaColor.FromRgb(20, 22, 28), true),
            _               => (RgbaColor.Black, false)
        };

        return new ToolPreset(
            name: preset.Name,
            icon: preset.Icon,
            kind: preset.ToolKind,
            color: color,
            size: preset.DefaultSize,
            isVector: isVector,
            brushPreset: preset);
    }
}
