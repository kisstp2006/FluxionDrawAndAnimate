using System.Collections.Generic;

namespace FluxionDrawAndAnimate.Presentation.Studio;

/// <summary>One grouped section (e.g. "Tools", "Transform", "Other") shown with a header.</summary>
public sealed record StudioToolPanelSection(string Name, IReadOnlyList<ToolDefinition> Tools);
