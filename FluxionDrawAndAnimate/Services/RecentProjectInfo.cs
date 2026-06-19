using System;

namespace FluxionDrawAndAnimate.Services;

public sealed record RecentProjectInfo(
    string Path,
    string Name,
    int Width,
    int Height,
    int FramesPerSecond,
    int FrameCount,
    string? ThumbnailPath,
    DateTimeOffset LastOpenedAt);
