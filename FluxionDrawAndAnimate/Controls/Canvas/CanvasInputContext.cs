using System;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Editing;

namespace FluxionDrawAndAnimate.Controls.Canvas;

internal readonly record struct CanvasInputContext(
    DrawingProject? Project,
    int ActiveFrameIndex,
    int ActiveLayerIndex,
    RgbaColor BrushColor,
    double BrushSize,
    ToolKind ActiveToolKind,
    BrushSettings? BrushSettings,
    UndoStack? UndoStack,
    Action<double, double, double>? CursorInfoCallback);
