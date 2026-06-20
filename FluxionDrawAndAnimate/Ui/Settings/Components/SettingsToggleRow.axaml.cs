using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace FluxionDrawAndAnimate.Ui.Settings.Components;

public partial class SettingsToggleRow : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<SettingsToggleRow, string>(nameof(Label), "");

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingsToggleRow, string?>(nameof(Description));

    public static readonly StyledProperty<bool> IsOnProperty =
        AvaloniaProperty.Register<SettingsToggleRow, bool>(nameof(IsOn),
            defaultBindingMode: BindingMode.TwoWay);

    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public bool IsOn { get => GetValue(IsOnProperty); set => SetValue(IsOnProperty, value); }

    public SettingsToggleRow() { InitializeComponent(); }
}
