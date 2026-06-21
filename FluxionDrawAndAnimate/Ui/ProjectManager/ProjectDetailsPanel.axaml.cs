using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FluxionDrawAndAnimate.Ui.ProjectManager;

public partial class ProjectDetailsPanel : UserControl
{
    public static readonly StyledProperty<bool> IsFullScreenProperty =
        AvaloniaProperty.Register<ProjectDetailsPanel, bool>(nameof(IsFullScreen));

    private static readonly BoxShadows DockedShadow = BoxShadows.Parse("0 8 32 0 #44000000");

    public ProjectDetailsPanel()
    {
        InitializeComponent();
        UpdateSurfaceChrome();
    }

    public bool IsFullScreen
    {
        get => GetValue(IsFullScreenProperty);
        set => SetValue(IsFullScreenProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsFullScreenProperty)
        {
            UpdateSurfaceChrome();
        }
    }

    private void UpdateSurfaceChrome()
    {
        if (DetailsSurface is null)
        {
            return;
        }

        DetailsSurface.CornerRadius = IsFullScreen ? new CornerRadius(0) : new CornerRadius(14);
        DetailsSurface.BorderThickness = IsFullScreen ? new Thickness(0) : new Thickness(1);
        DetailsSurface.BoxShadow = IsFullScreen ? default : DockedShadow;
    }
}
