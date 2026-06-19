using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Core.Animation;

public sealed partial class AnimationLayer : ObservableObject
{
    public AnimationLayer(string name, int frameCount)
        : this(Guid.NewGuid().ToString("N"), name, frameCount, LayerType.Raster)
    {
    }

    public AnimationLayer(string id, string name, int frameCount)
        : this(id, name, frameCount, LayerType.Raster)
    {
    }

    public AnimationLayer(string id, string name, int frameCount, LayerType type)
    {
        Id = id;
        Type = type;
        Name = name;

        for (var i = 0; i < frameCount; i++)
        {
            Frames.Add(new AnimationFrame(i + 1));
        }
    }

    public string Id { get; }
    public LayerType Type { get; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private double _opacity = 1.0;

    [ObservableProperty]
    private BlendMode _blendMode = BlendMode.Normal;

    public ObservableCollection<AnimationFrame> Frames { get; } = new();

    public AnimationFrame EnsureFrame(int zeroBasedIndex)
    {
        while (Frames.Count <= zeroBasedIndex)
        {
            Frames.Add(new AnimationFrame(Frames.Count + 1));
        }

        return Frames[zeroBasedIndex];
    }
}
