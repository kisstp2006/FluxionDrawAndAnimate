using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace FluxionDrawAndAnimate.Ui.Settings.Components;

public partial class SettingsSegmentedRow : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<SettingsSegmentedRow, string>(nameof(Label), "");
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingsSegmentedRow, string?>(nameof(Description));
    public static readonly StyledProperty<IEnumerable<string>?> OptionsProperty =
        AvaloniaProperty.Register<SettingsSegmentedRow, IEnumerable<string>?>(nameof(Options));
    public static readonly StyledProperty<string?> SelectedOptionProperty =
        AvaloniaProperty.Register<SettingsSegmentedRow, string?>(nameof(SelectedOption),
            defaultBindingMode: BindingMode.TwoWay);

    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public IEnumerable<string>? Options { get => GetValue(OptionsProperty); set => SetValue(OptionsProperty, value); }
    public string? SelectedOption { get => GetValue(SelectedOptionProperty); set => SetValue(SelectedOptionProperty, value); }

    public SettingsSegmentedRow() { InitializeComponent(); }
}
