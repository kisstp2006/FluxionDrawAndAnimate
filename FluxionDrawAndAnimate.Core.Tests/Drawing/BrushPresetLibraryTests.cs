using FluxionDrawAndAnimate.Core.Drawing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Drawing;

public class BrushPresetLibraryTests
{
    [Fact]
    public void Default_loads_all_eight_embedded_presets()
    {
        var lib = BrushPresetLibrary.Default;

        Assert.Equal(8, lib.BuiltIn.Count);
    }

    [Fact]
    public void BuiltIn_presets_are_sorted_by_sort_order()
    {
        var lib = BrushPresetLibrary.Default;

        for (var i = 1; i < lib.BuiltIn.Count; i++)
        {
            Assert.True(
                lib.BuiltIn[i - 1].SortOrder <= lib.BuiltIn[i].SortOrder,
                $"Preset '{lib.BuiltIn[i - 1].Name}' (sort {lib.BuiltIn[i - 1].SortOrder}) must come before or equal to '{lib.BuiltIn[i].Name}' (sort {lib.BuiltIn[i].SortOrder})");
        }
    }

    [Fact]
    public void Every_preset_has_unique_id_and_nonempty_name()
    {
        var lib = BrushPresetLibrary.Default;
        var ids = new HashSet<string>();

        foreach (var preset in lib.BuiltIn)
        {
            Assert.False(string.IsNullOrWhiteSpace(preset.Id), $"Preset '{preset.Name}' has empty Id");
            Assert.False(string.IsNullOrWhiteSpace(preset.Name), $"Preset '{preset.Id}' has empty Name");
            Assert.True(ids.Add(preset.Id), $"Duplicate preset Id: {preset.Id}");
        }
    }

    [Fact]
    public void Every_preset_carries_valid_brush_settings()
    {
        var lib = BrushPresetLibrary.Default;

        foreach (var preset in lib.BuiltIn)
        {
            Assert.NotNull(preset.Settings);
            Assert.True(preset.Settings.Hardness is >= 0 and <= 1, $"Preset '{preset.Id}' hardness {preset.Settings.Hardness} out of [0,1]");
            Assert.True(Enum.IsDefined(preset.Settings.Shape), $"Preset '{preset.Id}' has invalid shape");
            Assert.NotNull(preset.Settings.Dynamics);
        }
    }

    [Fact]
    public void Every_preset_default_size_is_positive()
    {
        var lib = BrushPresetLibrary.Default;

        foreach (var preset in lib.BuiltIn)
        {
            Assert.True(preset.DefaultSize > 0, $"Preset '{preset.Id}' has non-positive default size {preset.DefaultSize}");
        }
    }

    [Theory]
    [InlineData("pencil-graphite", ToolKind.Pencil)]
    [InlineData("pencil-sketch", ToolKind.Pencil)]
    [InlineData("ink-brush", ToolKind.Ink)]
    [InlineData("ink-marker", ToolKind.Ink)]
    [InlineData("paint-soft", ToolKind.Paint)]
    [InlineData("paint-opaque", ToolKind.Paint)]
    [InlineData("chisel-calligraphy", ToolKind.Vector)]
    [InlineData("eraser-soft", ToolKind.Eraser)]
    public void FindById_returns_preset_with_expected_tool_kind(string id, ToolKind expectedKind)
    {
        var lib = BrushPresetLibrary.Default;

        var preset = lib.FindById(id);

        Assert.NotNull(preset);
        Assert.Equal(expectedKind, preset!.ToolKind);
    }

    [Fact]
    public void FindById_returns_null_for_unknown_id()
    {
        var lib = BrushPresetLibrary.Default;

        Assert.Null(lib.FindById("does-not-exist"));
    }

    [Fact]
    public void Eraser_preset_has_no_opacity_binding_so_it_erases_at_full_strength()
    {
        var lib = BrushPresetLibrary.Default;
        var eraser = lib.FindById("eraser-soft");

        Assert.NotNull(eraser);
        Assert.Null(eraser!.Settings.Dynamics.OpacityBinding);
    }

    [Fact]
    public void Ink_preset_has_no_opacity_binding_for_clean_line_art()
    {
        var lib = BrushPresetLibrary.Default;
        var ink = lib.FindById("ink-brush");

        Assert.NotNull(ink);
        Assert.Null(ink!.Settings.Dynamics.OpacityBinding);
    }

    [Fact]
    public void Pencil_preset_pressure_curve_needs_strong_pressure_for_full_size()
    {
        var lib = BrushPresetLibrary.Default;
        var pencil = lib.FindById("pencil-graphite");

        Assert.NotNull(pencil);
        var sizeBinding = pencil!.Settings.Dynamics.SizeBinding;
        Assert.NotNull(sizeBinding);

        // EaseIn at 0.5 pressure should stay below 0.3 — graphite needs pressure.
        var sample = PaintInformation.FromMouse(0, 0) with { Pressure = 0.5 };
        Assert.True(sizeBinding!.Evaluate(sample) < 0.3, "Graphite pencil size at 0.5 pressure should be < 0.3");
    }

    [Fact]
    public void Paint_soft_preset_responds_quickly_at_low_pressure()
    {
        var lib = BrushPresetLibrary.Default;
        var paint = lib.FindById("paint-soft");

        Assert.NotNull(paint);
        var sizeBinding = paint!.Settings.Dynamics.SizeBinding;
        Assert.NotNull(sizeBinding);

        // EaseOut at 0.15 pressure should already be above 0.4 — wide and watery.
        var sample = PaintInformation.FromMouse(0, 0) with { Pressure = 0.15 };
        Assert.True(sizeBinding!.Evaluate(sample) > 0.4, "Soft paint size at 0.15 pressure should be > 0.4");
    }

    [Fact]
    public void Chisel_preset_uses_chisel_shape()
    {
        var lib = BrushPresetLibrary.Default;
        var chisel = lib.FindById("chisel-calligraphy");

        Assert.NotNull(chisel);
        Assert.Equal(BrushShape.Chisel, chisel!.Settings.Shape);
    }

    [Fact]
    public void LoadEmbedded_returns_same_instance_content_on_repeated_calls()
    {
        var first = BrushPresetLibrary.LoadEmbedded();
        var second = BrushPresetLibrary.LoadEmbedded();

        Assert.Equal(first.BuiltIn.Count, second.BuiltIn.Count);
        for (var i = 0; i < first.BuiltIn.Count; i++)
        {
            Assert.Equal(first.BuiltIn[i].Id, second.BuiltIn[i].Id);
        }
    }
}
