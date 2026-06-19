using CommunityToolkit.Mvvm.ComponentModel;

namespace FluxionDrawAndAnimate.Presentation;

/// <summary>
/// One entry in the home-page left navigation sidebar.
/// <see cref="IsActive"/> is set by <c>MainViewModel</c> when
/// <c>ActiveSidebarItemId</c> changes so the AXAML binding is straightforward.
/// </summary>
public sealed partial class HomeNavItem : ObservableObject
{
    public HomeNavItem(string id, string icon, string label, string? sectionHeader = null)
    {
        Id = id;
        Icon = icon;
        Label = label;
        SectionHeader = sectionHeader;
    }

    public string Id { get; }
    public string Icon { get; }
    public string Label { get; }

    /// <summary>If set, a non-selectable section header is shown before this item.</summary>
    public string? SectionHeader { get; }
    public bool HasSectionHeader => SectionHeader is not null;

    [ObservableProperty]
    private bool _isActive;
}
