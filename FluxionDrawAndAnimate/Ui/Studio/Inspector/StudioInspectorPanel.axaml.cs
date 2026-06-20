using System;
using Avalonia.Controls;
using FluxionDrawAndAnimate.ViewModels;

namespace FluxionDrawAndAnimate.Ui.Studio.Inspector;

public partial class StudioInspectorPanel : UserControl
{
    private MainViewModel? _vm;

    public StudioInspectorPanel()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm is not null)
            _vm.StudioInspectorRegistry.PanelsChanged -= Rebuild;

        _vm = DataContext as MainViewModel;

        if (_vm is not null)
        {
            _vm.StudioInspectorRegistry.PanelsChanged += Rebuild;
            Rebuild();
        }
    }

    private void Rebuild()
    {
        CardsContainer.Children.Clear();
        if (_vm is null) return;

        foreach (var def in _vm.StudioInspectorRegistry.Panels)
        {
            var content = def.CreateContent();
            var card = new InspectorCard
            {
                Title = def.Title,
                IsExpanded = def.IsExpanded,
                PanelContent = content,
            };
            CardsContainer.Children.Add(card);
        }
    }
}
