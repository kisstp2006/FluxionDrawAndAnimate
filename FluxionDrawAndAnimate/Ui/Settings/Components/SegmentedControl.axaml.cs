using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace FluxionDrawAndAnimate.Ui.Settings.Components;

public partial class SegmentedControl : UserControl
{
    public static readonly StyledProperty<IEnumerable<string>?> OptionsProperty =
        AvaloniaProperty.Register<SegmentedControl, IEnumerable<string>?>(nameof(Options));

    public static readonly StyledProperty<string?> SelectedOptionProperty =
        AvaloniaProperty.Register<SegmentedControl, string?>(nameof(SelectedOption),
            defaultBindingMode: BindingMode.TwoWay);

    public IEnumerable<string>? Options
    {
        get => GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public string? SelectedOption
    {
        get => GetValue(SelectedOptionProperty);
        set => SetValue(SelectedOptionProperty, value);
    }

    public SegmentedControl()
    {
        InitializeComponent();
    }
}
