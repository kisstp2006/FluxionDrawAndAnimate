using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace FluxionDrawAndAnimate.Ui.Shared;

/// <summary>
/// Shared modal frame used by every dialog in the app.
/// Automatically adapts between:
///   • Desktop/Tablet — centred card with max-width, rounded corners, dim overlay.
///   • Phone          — full-screen sheet, no shadow, large touch targets.
///
/// Place body content as the UserControl's Content.
/// Place footer buttons in <see cref="FooterContentProperty"/>.
/// </summary>
public partial class AppDialogFrame : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<AppDialogFrame, string>(nameof(Title), "");

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<AppDialogFrame, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string> DialogIconProperty =
        AvaloniaProperty.Register<AppDialogFrame, string>(nameof(DialogIcon), "mdi-information-outline");

    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<AppDialogFrame, ICommand?>(nameof(CloseCommand));

    public static readonly StyledProperty<double> MaxDialogWidthProperty =
        AvaloniaProperty.Register<AppDialogFrame, double>(nameof(MaxDialogWidth), 480);

    public static readonly StyledProperty<bool> IsPhoneLayoutProperty =
        AvaloniaProperty.Register<AppDialogFrame, bool>(nameof(IsPhoneLayout));

    public static readonly StyledProperty<Control?> FooterContentProperty =
        AvaloniaProperty.Register<AppDialogFrame, Control?>(nameof(FooterContent));

    public string    Title            { get => GetValue(TitleProperty);          set => SetValue(TitleProperty, value); }
    public string?   Subtitle         { get => GetValue(SubtitleProperty);       set => SetValue(SubtitleProperty, value); }
    public string    DialogIcon       { get => GetValue(DialogIconProperty);     set => SetValue(DialogIconProperty, value); }
    public ICommand? CloseCommand     { get => GetValue(CloseCommandProperty);   set => SetValue(CloseCommandProperty, value); }
    public double    MaxDialogWidth   { get => GetValue(MaxDialogWidthProperty); set => SetValue(MaxDialogWidthProperty, value); }
    public bool      IsPhoneLayout    { get => GetValue(IsPhoneLayoutProperty);  set => SetValue(IsPhoneLayoutProperty, value); }
    public Control?  FooterContent    { get => GetValue(FooterContentProperty);  set => SetValue(FooterContentProperty, value); }

    public AppDialogFrame()
    {
        InitializeComponent();
    }

    // ── Android hardware back-button support ─────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TopLevel.GetTopLevel(this)?.BackRequested += OnBackRequested;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        TopLevel.GetTopLevel(this)?.BackRequested -= OnBackRequested;
    }

    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (!IsVisible) return;
        if (CloseCommand?.CanExecute(null) == true)
        {
            CloseCommand.Execute(null);
            e.Handled = true;
        }
    }
}
