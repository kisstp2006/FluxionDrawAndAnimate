using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using FluxionDrawAndAnimate.ViewModels;

namespace FluxionDrawAndAnimate.Ui.Settings;

public partial class SettingsDialog : UserControl
{
    private MainViewModel? _vm;
    private readonly Dictionary<string, Control> _pageCache = new();

    public SettingsDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }

        _vm = DataContext as MainViewModel;

        if (_vm is null)
        {
            return;
        }

        _vm.PropertyChanged += OnVmPropertyChanged;
        ShowCategoryPage(_vm.SelectedSettingsCategoryId);
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedSettingsCategoryId) && _vm is not null)
        {
            ShowCategoryPage(_vm.SelectedSettingsCategoryId);
        }
    }

    private void ShowCategoryPage(string categoryId)
    {
        if (_vm is null)
        {
            return;
        }

        if (!_pageCache.TryGetValue(categoryId, out var page))
        {
            var category = _vm.SettingsCategories
                .FirstOrDefault(c => c.Id == categoryId);

            if (category is null)
            {
                return;
            }

            page = category.CreatePage();
            _pageCache[categoryId] = page;
        }

        SettingsPageContent.Content = page;
    }
}

file static class EnumerableExtensions
{
    public static T? FirstOrDefault<T>(this IReadOnlyList<T> list, Func<T, bool> predicate)
    {
        foreach (var item in list)
        {
            if (predicate(item)) return item;
        }

        return default;
    }
}
