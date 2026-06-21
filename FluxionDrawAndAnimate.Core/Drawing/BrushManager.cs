using System;
using System.Collections.Generic;

namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// Central runtime-state store for brush presets. Each brush id gets its own
/// mutable state so tool switching does not overwrite size/opacity/hardness.
/// </summary>
public sealed class BrushManager
{
    private readonly Dictionary<string, BrushRuntimeState> _states = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, BrushRuntimeState> States => _states;

    public BrushRuntimeState GetOrCreate(BrushRuntimeDefaults defaults)
    {
        var brushId = ResolveBrushId(defaults.BrushId, ToolKind.Pencil);
        if (_states.TryGetValue(brushId, out var state))
        {
            return state;
        }

        state = new BrushRuntimeState(defaults with { BrushId = brushId });
        _states[brushId] = state;
        return state;
    }

    public BrushRuntimeState GetOrCreate(
        BrushPreset? preset,
        ToolKind fallbackToolKind,
        double fallbackSize,
        double defaultOpacity = 1.0,
        double defaultFlow = 1.0)
    {
        return GetOrCreate(BrushRuntimeDefaults.FromPreset(
            preset,
            fallbackToolKind,
            fallbackSize,
            defaultOpacity,
            defaultFlow));
    }

    public bool TryGet(string brushId, out BrushRuntimeState state) =>
        _states.TryGetValue(ResolveBrushId(brushId, ToolKind.Pencil), out state!);

    public static string ResolveBrushId(string? brushId, ToolKind fallbackToolKind)
    {
        return string.IsNullOrWhiteSpace(brushId)
            ? $"tool:{fallbackToolKind}"
            : brushId.Trim();
    }
}
