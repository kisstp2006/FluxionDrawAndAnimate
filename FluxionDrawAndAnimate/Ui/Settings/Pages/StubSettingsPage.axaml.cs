using Avalonia;
using Avalonia.Controls;

namespace FluxionDrawAndAnimate.Ui.Settings.Pages;

public partial class StubSettingsPage : UserControl
{
    public static readonly StyledProperty<string> PageTitleProperty =
        AvaloniaProperty.Register<StubSettingsPage, string>(nameof(PageTitle), "");

    public static readonly StyledProperty<string> PageDescriptionProperty =
        AvaloniaProperty.Register<StubSettingsPage, string>(nameof(PageDescription), "");

    public string PageTitle { get => GetValue(PageTitleProperty); set => SetValue(PageTitleProperty, value); }
    public string PageDescription { get => GetValue(PageDescriptionProperty); set => SetValue(PageDescriptionProperty, value); }

    public StubSettingsPage() { InitializeComponent(); }

    public StubSettingsPage(string title, string description = "")
    {
        InitializeComponent();
        PageTitle = title;
        PageDescription = description;
    }
}
