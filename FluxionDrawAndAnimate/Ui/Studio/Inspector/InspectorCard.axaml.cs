using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FluxionDrawAndAnimate.Ui.Studio.Inspector;

public partial class InspectorCard : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<InspectorCard, string>(nameof(Title), "");

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<InspectorCard, bool>(nameof(IsExpanded), true);

    public static readonly StyledProperty<Control?> PanelContentProperty =
        AvaloniaProperty.Register<InspectorCard, Control?>(nameof(PanelContent));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public Control? PanelContent
    {
        get => GetValue(PanelContentProperty);
        set => SetValue(PanelContentProperty, value);
    }

    public InspectorCard()
    {
        InitializeComponent();
    }

    private void OnHeaderClicked(object? sender, RoutedEventArgs e)
    {
        IsExpanded = !IsExpanded;
    }
}
