using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FluxionDrawAndAnimate.Core.Color;
using FluxionDrawAndAnimate.Core.Tiling;

namespace FluxionDrawAndAnimate.Core.Animation;

public sealed partial class DrawingProject : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private DateTimeOffset _modifiedAt = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private string _name = "Untitled";

    [ObservableProperty]
    private int _width = 1920;

    [ObservableProperty]
    private int _height = 1080;

    [ObservableProperty]
    private int _frameCount = 24;

    [ObservableProperty]
    private int _framesPerSecond = 24;

    public ColorProfile ColorProfile { get; set; } = new();
    public TileSettings TileSettings { get; set; } = new();
    public ObservableCollection<AnimationLayer> Layers { get; } = new();
    public ObservableCollection<AudioClip> AudioClips { get; } = new();
}
