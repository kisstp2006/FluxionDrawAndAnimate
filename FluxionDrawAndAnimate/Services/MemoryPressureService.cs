using System;

namespace FluxionDrawAndAnimate.Services;

public enum MemoryPressureLevel
{
    Moderate,
    Critical
}

public sealed class MemoryPressureEventArgs : EventArgs
{
    public MemoryPressureEventArgs(MemoryPressureLevel level)
    {
        Level = level;
    }

    public MemoryPressureLevel Level { get; }
}

public static class MemoryPressureService
{
    public static event EventHandler<MemoryPressureEventArgs>? MemoryPressure;

    public static void Report(MemoryPressureLevel level)
    {
        MemoryPressure?.Invoke(null, new MemoryPressureEventArgs(level));
    }
}
