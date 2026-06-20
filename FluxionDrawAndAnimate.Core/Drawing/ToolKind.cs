namespace FluxionDrawAndAnimate.Core.Drawing;

public enum ToolKind
{
    // ── Drawing tools (paint onto the raster surface) ──────────────────
    Pencil,
    Ink,
    Paint,
    Eraser,

    // ── Geometric / special drawing ─────────────────────────────────────
    Vector,     // Bezier / shape pen
    Fill,       // Flood fill
    Shape,      // Rect / ellipse / line (coming soon)
    Text,

    // ── Selection & transform ────────────────────────────────────────────
    Select,     // Rectangle selection
    Lasso,      // Freehand selection (coming soon)
    Move,       // Move / pan layer content (coming soon)
    Rotate,     // Rotate transform (coming soon)

    // ── Utility ──────────────────────────────────────────────────────────
    Eyedropper, // Pick colour from canvas (coming soon)
    Ruler,      // Straight-line ruler guide (coming soon)
    Symmetry,   // Symmetry axis (coming soon)
}
