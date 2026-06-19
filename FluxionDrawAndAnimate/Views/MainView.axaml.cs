using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using FluxionDrawAndAnimate.Presentation;
using FluxionDrawAndAnimate.Presentation.Shell;
using FluxionDrawAndAnimate.ViewModels;

namespace FluxionDrawAndAnimate.Views;

public partial class MainView : UserControl
{
    private MainViewModel? _viewModel;
    private readonly Dictionary<string, Control> _pageCache = new();

    public MainView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => ReportAvailableWidth();
        AttachedToVisualTree += (_, _) => ReportAvailableWidth();
    }

    private void ReportAvailableWidth()
    {
        if (_viewModel is null)
        {
            return;
        }

        var width = Bounds.Width;
        if (width <= 0)
        {
            width = TopLevel.GetTopLevel(this)?.Bounds.Width ?? 0;
        }

        _viewModel.ApplyAvailableWidth(width);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        HandleDataContextChanged(DataContext);
    }

    private void HandleDataContextChanged(object? dataContext)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
        }

        _viewModel = dataContext as MainViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        ApplyThemeVariant(_viewModel.ProjectCreatorThemeMode);
        ReportAvailableWidth();
        ShowPage(_viewModel.ActivePage);
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ProjectCreatorThemeMode) && _viewModel is not null)
        {
            ApplyThemeVariant(_viewModel.ProjectCreatorThemeMode);
        }

        if (e.PropertyName == nameof(MainViewModel.ActivePage) && _viewModel is not null)
        {
            ShowPage(_viewModel.ActivePage);
        }
    }

    private void ShowPage(PageDefinition? page)
    {
        if (page is null)
        {
            PageContent.Content = null;
            return;
        }

        if (!_pageCache.TryGetValue(page.Id, out var view))
        {
            view = page.CreateView();
            _pageCache[page.Id] = view;
        }

        PageContent.Content = view;
    }

    private static void ApplyThemeVariant(ProjectCreatorThemeMode mode)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = mode == ProjectCreatorThemeMode.Light
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }
}
