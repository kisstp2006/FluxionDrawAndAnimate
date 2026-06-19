namespace FluxionDrawAndAnimate.Core.Drawing;

public enum BrushShape
{
    Round,      // soft/hard circular dab — default for pencil and paint
    HardRound,  // near-aliased circle with 1-px AA — ink, markers, pixel art
    Chisel,     // angled ellipse (45°, 4:1 ratio) — calligraphy, brush pen
}
