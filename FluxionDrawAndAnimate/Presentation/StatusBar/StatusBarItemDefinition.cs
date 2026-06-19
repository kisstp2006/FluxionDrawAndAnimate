using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace FluxionDrawAndAnimate.Presentation.StatusBar;

/// <summary>
/// One item in the status bar. Can be a static label, a live-text label,
/// an icon button, a separator, or a progress bar.
/// <para>
/// <see cref="PageId"/> = null  → visible on every page.<br/>
/// <see cref="PageId"/> = "studio" → visible only when the Studio page is active.
/// </para>
/// </summary>
public sealed class StatusBarItemDefinition
{
    public required string Id { get; init; }
    public StatusBarSection Section { get; init; } = StatusBarSection.Left;

    /// <summary>Static label text. Ignored when <see cref="GetLiveText"/> is set.</summary>
    public string Text { get; init; } = "";

    /// <summary>Optional icon / emoji shown left of the label.</summary>
    public string? Icon { get; init; }

    public ICommand? Command { get; init; }
    public bool IsSeparator { get; init; }

    /// <summary>Show a dropdown arrow (∨) next to the label.</summary>
    public bool HasDropdown { get; init; }

    /// <summary>null = all pages; specific page id = that page only.</summary>
    public string? PageId { get; init; }
    public int SortOrder { get; init; }

    // ── Live / dynamic data ────────────────────────────────────────────

    /// <summary>
    /// Factory called with the current ViewModel to produce the displayed string.
    /// Overrides <see cref="Text"/> when non-null.
    /// </summary>
    public Func<object?, string>? GetLiveText { get; init; }

    /// <summary>
    /// VM property names whose change triggers a re-call of <see cref="GetLiveText"/>.
    /// </summary>
    public IReadOnlyList<string>? ObservedProperties { get; init; }

    /// <summary>
    /// When non-null, a progress bar (0..1) is rendered right of the label.
    /// </summary>
    public Func<object?, double>? GetProgress { get; init; }
}
