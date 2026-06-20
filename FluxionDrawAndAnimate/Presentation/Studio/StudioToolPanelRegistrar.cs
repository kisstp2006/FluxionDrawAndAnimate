using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Presentation.Studio;

/// <summary>
/// Registers the default drawing tools for the Studio panel.
/// Icons use MDI (Material Design Icons) keys.
/// Tools marked <c>IsAvailable = false</c> are displayed as greyed-out
/// "coming soon" items — they exist in the registry for discoverability.
/// </summary>
public static class StudioToolPanelRegistrar
{
    public static void RegisterDefaults(StudioToolPanelRegistry r)
    {
        // ── Tools ─────────────────────────────────────────────────────────
        r.Register(new ToolDefinition { Id = "brush",    Name = "Brush",   Icon = "mdi-brush-outline",       ToolKind = ToolKind.Pencil,     Section = "Tools",     SortOrder = 0 });
        r.Register(new ToolDefinition { Id = "pencil",   Name = "Pencil",  Icon = "mdi-pencil-outline",      ToolKind = ToolKind.Ink,        Section = "Tools",     SortOrder = 1 });
        r.Register(new ToolDefinition { Id = "eraser",   Name = "Eraser",  Icon = "mdi-eraser",              ToolKind = ToolKind.Eraser,     Section = "Tools",     SortOrder = 2 });
        r.Register(new ToolDefinition { Id = "fill",     Name = "Fill",    Icon = "mdi-format-color-fill",   ToolKind = ToolKind.Fill,       Section = "Tools",     SortOrder = 3,  IsAvailable = false });
        r.Register(new ToolDefinition { Id = "shape",    Name = "Shape",   Icon = "mdi-shape-outline",       ToolKind = ToolKind.Shape,      Section = "Tools",     SortOrder = 4,  IsAvailable = false });
        r.Register(new ToolDefinition { Id = "text",     Name = "Text",    Icon = "mdi-format-text",         ToolKind = ToolKind.Text,       Section = "Tools",     SortOrder = 5 });

        // ── Transform ─────────────────────────────────────────────────────
        r.Register(new ToolDefinition { Id = "select",   Name = "Select",  Icon = "mdi-selection",           ToolKind = ToolKind.Select,     Section = "Transform", SortOrder = 0 });
        r.Register(new ToolDefinition { Id = "lasso",    Name = "Lasso",   Icon = "mdi-lasso",               ToolKind = ToolKind.Lasso,      Section = "Transform", SortOrder = 1,  IsAvailable = false });
        r.Register(new ToolDefinition { Id = "move",     Name = "Move",    Icon = "mdi-cursor-move",         ToolKind = ToolKind.Move,       Section = "Transform", SortOrder = 2,  IsAvailable = false });
        r.Register(new ToolDefinition { Id = "rotate",   Name = "Rotate",  Icon = "mdi-rotate-right",        ToolKind = ToolKind.Rotate,     Section = "Transform", SortOrder = 3,  IsAvailable = false });

        // ── Other ─────────────────────────────────────────────────────────
        r.Register(new ToolDefinition { Id = "eyedrop",  Name = "Eyedropper", Icon = "mdi-eyedropper",      ToolKind = ToolKind.Eyedropper, Section = "Other",     SortOrder = 0,  IsAvailable = false });
        r.Register(new ToolDefinition { Id = "ruler",    Name = "Ruler",   Icon = "mdi-ruler",               ToolKind = ToolKind.Ruler,      Section = "Other",     SortOrder = 1,  IsAvailable = false });
        r.Register(new ToolDefinition { Id = "symmetry", Name = "Symmetry",Icon = "mdi-approximately-equal", ToolKind = ToolKind.Symmetry,   Section = "Other",     SortOrder = 2,  IsAvailable = false });
    }
}
