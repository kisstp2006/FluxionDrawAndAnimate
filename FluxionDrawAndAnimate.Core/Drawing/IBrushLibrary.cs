namespace FluxionDrawAndAnimate.Core.Drawing;

public interface IBrushLibrary
{
    IReadOnlyList<BrushPreset> BuiltIn { get; }
    BrushPreset? FindById(string id);
}
