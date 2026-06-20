using FluxionDrawAndAnimate.ViewModels;

namespace FluxionDrawAndAnimate.Presentation.Studio;

/// <summary>
/// Registers the default Studio toolbar items.
///
/// Rule: NEVER add items whose command already appears in the global
/// menu bar (File / Edit / View / Help). Those menus handle: New, Open,
/// Save, Save As, Undo, Redo, Settings, Layout toggle.
///
/// This toolbar covers STUDIO-SPECIFIC actions only:
///   • Frame / timeline management
///   • Layer operations
///   • Canvas view toggles (onion skin, snap, grid)
///   • Export to raster/video (distinct from "Save project file")
/// </summary>
public static class StudioToolbarRegistrar
{
    public static void RegisterDefaults(StudioToolbarRegistry r, MainViewModel vm)
    {
        RegisterFrameGroup(r, vm);
        RegisterLayerGroup(r);
        RegisterViewGroup(r, vm);
        RegisterRightActions(r);
    }

    // ── Frame / timeline group ─────────────────────────────────────────

    private static void RegisterFrameGroup(StudioToolbarRegistry r, MainViewModel vm)
    {
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "frame-add",
            Icon = "mdi-plus-box-outline",
            Tooltip = "Add frame",
            Command = vm.AddFrameCommand,
            Section = StudioToolbarSection.Left, SortOrder = 0
        });
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "frame-duplicate",
            Icon = "mdi-content-copy",
            Tooltip = "Duplicate frame",
            Command = vm.DuplicateFrameCommand,
            Section = StudioToolbarSection.Left, SortOrder = 1
        });
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "frame-delete",
            Icon = "mdi-delete-outline",
            Tooltip = "Delete frame  (coming soon)",
            Section = StudioToolbarSection.Left, SortOrder = 2
        });
    }

    // ── Layer operations group ─────────────────────────────────────────

    private static void RegisterLayerGroup(StudioToolbarRegistry r)
    {
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "sep-1", IsSeparator = true,
            Section = StudioToolbarSection.Left, SortOrder = 10
        });
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "layer-lock",
            Icon = "mdi-lock-outline",
            Tooltip = "Lock active layer  (coming soon)",
            Section = StudioToolbarSection.Left, SortOrder = 11
        });
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "layer-visibility",
            Icon = "mdi-eye-outline",
            Tooltip = "Toggle layer visibility  (coming soon)",
            Section = StudioToolbarSection.Left, SortOrder = 12
        });
    }

    // ── Canvas view toggles ────────────────────────────────────────────

    private static void RegisterViewGroup(StudioToolbarRegistry r, MainViewModel vm)
    {
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "sep-2", IsSeparator = true,
            Section = StudioToolbarSection.Left, SortOrder = 20
        });
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "view-onion",
            Icon = "mdi-layers-outline",
            Tooltip = "Toggle onion skin",
            Command = vm.ToggleOnionSkinCommand,
            Section = StudioToolbarSection.Left, SortOrder = 21
        });
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "view-snap",
            Icon = "mdi-magnet-on",
            Tooltip = "Toggle snap",
            Command = vm.ToggleSnapCommand,
            Section = StudioToolbarSection.Left, SortOrder = 22
        });
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "view-grid",
            Icon = "mdi-grid",
            Tooltip = "Toggle grid",
            Command = vm.ToggleGridCommand,
            Section = StudioToolbarSection.Left, SortOrder = 23
        });
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "view-capture",
            Icon = "mdi-camera-outline",
            Tooltip = "Capture frame  (coming soon)",
            Section = StudioToolbarSection.Left, SortOrder = 24
        });
    }

    // ── Right: export actions (NOT the same as Save/Save As) ──────────

    private static void RegisterRightActions(StudioToolbarRegistry r)
    {
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "share",
            Icon = "mdi-share-variant-outline",
            Text = "Share",
            Tooltip = "Share project  (coming soon)",
            Section = StudioToolbarSection.Right, SortOrder = 0
        });
        r.Register(new StudioToolbarItemDefinition
        {
            Id = "export",
            Icon = "mdi-export-variant",
            Text = "Export",
            Tooltip = "Export to image / video  (coming soon)",
            IsPrimary = true,
            HasDropdown = true,
            Section = StudioToolbarSection.Right, SortOrder = 1
        });
    }
}
