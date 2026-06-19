using System;
using Avalonia.Controls;

namespace FluxionDrawAndAnimate.Presentation.Shell;

/// <summary>
/// Describes one top-level page (Home, Studio, Animate, …).
/// Registered via <see cref="AppShellRegistry"/>; the title bar and shell
/// know nothing about specific pages — they only see this descriptor.
/// </summary>
public sealed class PageDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public int SortOrder { get; init; }

    /// <summary>Factory called once per navigation to create the page control.</summary>
    public required Func<Control> CreateView { get; init; }
}
