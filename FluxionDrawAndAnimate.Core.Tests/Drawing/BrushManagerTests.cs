using FluxionDrawAndAnimate.Core.Drawing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Drawing;

public class BrushManagerTests
{
    [Fact]
    public void GetOrCreate_reuses_state_for_same_brush_id()
    {
        var manager = new BrushManager();
        var defaults = new BrushRuntimeDefaults("pencil-graphite", 10, 1, 0.75, 1, BlendMode.Normal);

        var first = manager.GetOrCreate(defaults);
        first.SetSize(42);
        var second = manager.GetOrCreate(defaults with { Size = 99 });

        Assert.Same(first, second);
        Assert.Equal(42, second.Size);
    }

    [Fact]
    public void Runtime_state_clamps_invalid_values()
    {
        var manager = new BrushManager();
        var state = manager.GetOrCreate(new BrushRuntimeDefaults("paint-soft", -5, 2, -1, 4, BlendMode.Normal));

        Assert.Equal(1, state.Size);
        Assert.Equal(1, state.Opacity);
        Assert.Equal(0, state.Hardness);
        Assert.Equal(1, state.Flow);
    }

    [Fact]
    public void ResolveBrushId_uses_tool_fallback_when_preset_id_is_missing()
    {
        var id = BrushManager.ResolveBrushId("", ToolKind.Eraser);

        Assert.Equal("tool:Eraser", id);
    }
}
