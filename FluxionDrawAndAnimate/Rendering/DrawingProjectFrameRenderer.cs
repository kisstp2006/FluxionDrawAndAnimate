using System;
using Avalonia;
using Avalonia.Media;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Rendering;

public sealed class DrawingProjectFrameRenderer
{
    private static readonly IBrush WorkspaceBrush = new SolidColorBrush(Color.FromRgb(21, 22, 25));
    private static readonly IBrush ThumbnailBackgroundBrush = new SolidColorBrush(Color.FromRgb(241, 244, 248));
    private static readonly IBrush PaperBrush = Brushes.White;
    private static readonly Pen PaperPen = new(new SolidColorBrush(Color.FromRgb(48, 54, 61)), 1);
    private static readonly Pen DocumentBorderPen = new(new SolidColorBrush(Color.FromRgb(111, 122, 135)), 1);

    // Chrome helpers used by the raster canvas path (background + paper + border);
    // the strokes themselves are composited from the tile-backed RasterFrameCache.
    public void DrawWorkspaceBackground(DrawingContext context, Rect bounds)
    {
        context.DrawRectangle(WorkspaceBrush, null, bounds);
    }

    public void DrawPaperSurface(DrawingContext context, Rect documentRect)
    {
        DrawPaper(context, documentRect);
    }

    public void DrawBorder(DrawingContext context, Rect documentRect)
    {
        DrawDocumentBorder(context, documentRect);
    }

    public Rect CalculateDocumentRect(DrawingProject project, Rect bounds, double padding)
    {
        var availableWidth = Math.Max(1, bounds.Width - padding * 2);
        var availableHeight = Math.Max(1, bounds.Height - padding * 2);
        var scale = Math.Min(availableWidth / project.Width, availableHeight / project.Height);
        var width = project.Width * scale;
        var height = project.Height * scale;

        return new Rect(
            bounds.X + (bounds.Width - width) / 2,
            bounds.Y + (bounds.Height - height) / 2,
            width,
            height);
    }

    public void DrawWorkspaceFrame(
        DrawingContext context,
        DrawingProject project,
        int frameIndex,
        Rect bounds,
        bool showOnionSkin)
    {
        context.DrawRectangle(WorkspaceBrush, null, bounds);

        var documentRect = CalculateDocumentRect(project, bounds, 28);
        DrawPaper(context, documentRect);

        if (showOnionSkin)
        {
            DrawFrame(context, project, frameIndex - 1, documentRect, 0.22);
            DrawFrame(context, project, frameIndex + 1, documentRect, 0.16);
        }

        DrawFrame(context, project, frameIndex, documentRect, 1);
        DrawDocumentBorder(context, documentRect);
    }

    public void DrawThumbnail(
        DrawingContext context,
        DrawingProject project,
        int preferredFrameIndex,
        Rect bounds)
    {
        using var clip = context.PushClip(bounds);
        context.DrawRectangle(ThumbnailBackgroundBrush, null, bounds);

        var frameIndex = FindPreviewFrameIndex(project, preferredFrameIndex);
        var documentRect = CalculateDocumentRect(project, bounds, 8);
        DrawPaper(context, documentRect);
        DrawFrame(context, project, frameIndex, documentRect, 1);
        DrawDocumentBorder(context, documentRect);
    }

    public int FindPreviewFrameIndex(DrawingProject project, int preferredFrameIndex)
    {
        if (HasVisibleDrawing(project, preferredFrameIndex))
        {
            return preferredFrameIndex;
        }

        for (var frameIndex = 0; frameIndex < project.FrameCount; frameIndex++)
        {
            if (HasVisibleDrawing(project, frameIndex))
            {
                return frameIndex;
            }
        }

        return Math.Clamp(preferredFrameIndex, 0, Math.Max(0, project.FrameCount - 1));
    }

    public void DrawFrame(
        DrawingContext context,
        DrawingProject project,
        int frameIndex,
        Rect documentRect,
        double opacity)
    {
        if (frameIndex < 0)
        {
            return;
        }

        using var opacityLayer = context.PushOpacity(opacity);

        foreach (var layer in project.Layers)
        {
            if (!layer.IsVisible || frameIndex >= layer.Frames.Count)
            {
                continue;
            }

            using var layerOpacity = context.PushOpacity(Math.Clamp(layer.Opacity, 0, 1));
            foreach (var stroke in layer.Frames[frameIndex].Strokes)
            {
                DrawStroke(context, project, documentRect, stroke);
            }
        }
    }

    private static bool HasVisibleDrawing(DrawingProject project, int frameIndex)
    {
        if (frameIndex < 0)
        {
            return false;
        }

        foreach (var layer in project.Layers)
        {
            if (!layer.IsVisible || frameIndex >= layer.Frames.Count)
            {
                continue;
            }

            if (layer.Frames[frameIndex].Strokes.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void DrawPaper(DrawingContext context, Rect documentRect)
    {
        context.DrawRectangle(PaperBrush, PaperPen, documentRect);
    }

    private static void DrawStroke(DrawingContext context, DrawingProject project, Rect documentRect, StrokePath stroke)
    {
        if (stroke.Points.Count == 0)
        {
            return;
        }

        var scale = documentRect.Width / project.Width;
        var brush = new SolidColorBrush(stroke.Color.ToAvaloniaColor());
        var pen = new Pen(brush, Math.Max(1, stroke.Size * scale));

        if (stroke.Points.Count == 1)
        {
            var point = ToScreen(project, documentRect, stroke.Points[0]);
            var radius = Math.Max(1, stroke.Size * scale / 2);
            context.DrawEllipse(brush, null, point, radius, radius);
            return;
        }

        for (var i = 1; i < stroke.Points.Count; i++)
        {
            context.DrawLine(
                pen,
                ToScreen(project, documentRect, stroke.Points[i - 1]),
                ToScreen(project, documentRect, stroke.Points[i]));
        }
    }

    private static Point ToScreen(DrawingProject project, Rect documentRect, PaintInformation point)
    {
        return new Point(
            documentRect.X + point.X / project.Width * documentRect.Width,
            documentRect.Y + point.Y / project.Height * documentRect.Height);
    }

    private static void DrawDocumentBorder(DrawingContext context, Rect documentRect)
    {
        context.DrawRectangle(null, DocumentBorderPen, documentRect);
    }
}
