using Avalonia.Controls;
using Avalonia.Input;
using FluxionDrawAndAnimate.ViewModels;

namespace FluxionDrawAndAnimate.Ui.ProjectManager;

public partial class RecentProjectsSection : UserControl
{
    public RecentProjectsSection()
    {
        InitializeComponent();
        ProjectListBox.DoubleTapped += OnProjectDoubleTapped;
    }

    private void OnProjectDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.OpenSelectedRecentProjectCommand.CanExecute(null))
            vm.OpenSelectedRecentProjectCommand.Execute(null);
    }
}
