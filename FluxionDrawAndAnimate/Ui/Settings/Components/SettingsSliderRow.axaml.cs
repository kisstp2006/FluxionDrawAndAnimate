using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace FluxionDrawAndAnimate.Ui.Settings.Components;

public partial class SettingsSliderRow : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<SettingsSliderRow, string>(nameof(Label), "");
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingsSliderRow, string?>(nameof(Description));
    public static readonly StyledProperty<double> MinProperty =
        AvaloniaProperty.Register<SettingsSliderRow, double>(nameof(Min), 0);
    public static readonly StyledProperty<double> MaxProperty =
        AvaloniaProperty.Register<SettingsSliderRow, double>(nameof(Max), 100);
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<SettingsSliderRow, double>(nameof(Value),
            defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<string> SuffixProperty =
        AvaloniaProperty.Register<SettingsSliderRow, string>(nameof(Suffix), "%");

    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public double Min { get => GetValue(MinProperty); set => SetValue(MinProperty, value); }
    public double Max { get => GetValue(MaxProperty); set => SetValue(MaxProperty, value); }
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string Suffix { get => GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }

    public SettingsSliderRow() { InitializeComponent(); }
}
