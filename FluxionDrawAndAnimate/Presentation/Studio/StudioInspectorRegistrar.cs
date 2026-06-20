using FluxionDrawAndAnimate.Ui.Studio.Inspector;

namespace FluxionDrawAndAnimate.Presentation.Studio;

/// <summary>
/// Registers the default inspector cards.
/// Each card creates its own UserControl which binds to the inherited MainViewModel DataContext.
/// Add a new <see cref="InspectorPanelDefinition"/> here to introduce a new panel —
/// no other file needs to change.
/// </summary>
public static class StudioInspectorRegistrar
{
    public static void RegisterDefaults(StudioInspectorRegistry r)
    {
        r.Register(new InspectorPanelDefinition
        {
            Id = "color", Title = "Color",
            CreateContent = () => new InspectorColorPanel(),
            SortOrder = 0
        });
        r.Register(new InspectorPanelDefinition
        {
            Id = "swatches", Title = "Swatches",
            CreateContent = () => new InspectorSwatchesPanel(),
            SortOrder = 1
        });
        r.Register(new InspectorPanelDefinition
        {
            Id = "tool-properties", Title = "Properties",
            CreateContent = () => new InspectorToolPropertiesPanel(),
            SortOrder = 2
        });
        r.Register(new InspectorPanelDefinition
        {
            Id = "project", Title = "Project",
            CreateContent = () => new InspectorProjectPanel(),
            IsExpanded = false,
            SortOrder = 3
        });
    }
}
