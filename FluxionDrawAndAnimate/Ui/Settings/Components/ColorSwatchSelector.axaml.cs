using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace FluxionDrawAndAnimate.Ui.Settings.Components;

public partial class ColorSwatchSelector : UserControl
{
    public static readonly StyledProperty<IEnumerable<string>?> ColorsProperty =
        AvaloniaProperty.Register<ColorSwatchSelector, IEnumerable<string>?>(nameof(Colors));

    public static readonly StyledProperty<string?> SelectedColorProperty =
        AvaloniaProperty.Register<ColorSwatchSelector, string?>(nameof(SelectedColor),
            defaultBindingMode: BindingMode.TwoWay);

    public IEnumerable<string>? Colors
    {
        get => GetValue(ColorsProperty);
        set => SetValue(ColorsProperty, value);
    }

    public string? SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public ColorSwatchSelector()
    {
        InitializeComponent();
    }
}
