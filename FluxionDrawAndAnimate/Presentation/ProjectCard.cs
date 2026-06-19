using Avalonia.Media;
using FluxionDrawAndAnimate.Core.Projects;

namespace FluxionDrawAndAnimate.Presentation;

public sealed class ProjectCard
{
    public ProjectCard(
        ProjectCardKind kind,
        string title,
        string subtitle,
        string sizeLabel,
        string timingLabel,
        string thumbnailLabel,
        string? badgeLabel = null,
        string? updatedLabel = null,
        ProjectCreationOptions? creationOptions = null,
        string? projectPath = null,
        string? thumbnailPath = null,
        IImage? thumbnail = null,
        string? badgeColor = null)
    {
        Kind = kind;
        Title = title;
        Subtitle = subtitle;
        SizeLabel = sizeLabel;
        TimingLabel = timingLabel;
        ThumbnailLabel = thumbnailLabel;
        BadgeLabel = badgeLabel;
        UpdatedLabel = updatedLabel;
        CreationOptions = creationOptions;
        ProjectPath = projectPath;
        ThumbnailPath = thumbnailPath;
        Thumbnail = thumbnail;
        BadgeColor = badgeColor ?? "#3B4252";
    }

    public ProjectCardKind Kind { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string SizeLabel { get; }
    public string TimingLabel { get; }
    public string ThumbnailLabel { get; }
    public string? BadgeLabel { get; }
    public string? UpdatedLabel { get; }
    public ProjectCreationOptions? CreationOptions { get; }
    public string? ProjectPath { get; }
    public string? ThumbnailPath { get; }
    public IImage? Thumbnail { get; }
    public string BadgeColor { get; }
    public bool HasThumbnail => Thumbnail is not null;
    public bool HasNoThumbnail => !HasThumbnail;
    public bool HasBadge => !string.IsNullOrWhiteSpace(BadgeLabel);
    public bool HasUpdatedLabel => !string.IsNullOrWhiteSpace(UpdatedLabel);

    public IBrush BadgeBrush
    {
        get
        {
            try { return new SolidColorBrush(Color.Parse(BadgeColor)); }
            catch { return new SolidColorBrush(Color.Parse("#3B4252")); }
        }
    }
}
