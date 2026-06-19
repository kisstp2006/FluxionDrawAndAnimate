using System.Collections.Generic;

namespace FluxionDrawAndAnimate.Presentation;

public sealed class ProjectPresetFactory
{
    public IReadOnlyList<ProjectPreset> CreateDefaultPresets()
    {
        return
        [
            new("Animation HD", "16:9 scene", 1920, 1080, 24, 48),
            new("Short Flip", "quick loop", 1280, 720, 12, 24),
            new("Square Post", "social canvas", 1080, 1080, 24, 24),
            new("Tablet Sketch", "wide drawing", 2048, 1536, 24, 24),
            new("Phone Story", "vertical scene", 1080, 1920, 24, 24),
            new("Custom Canvas", "start from scratch", 1920, 1080, 24, 24)
        ];
    }
}
