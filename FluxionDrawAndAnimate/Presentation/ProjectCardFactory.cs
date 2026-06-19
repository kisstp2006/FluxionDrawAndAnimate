using System;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Projects;
using FluxionDrawAndAnimate.Services;

namespace FluxionDrawAndAnimate.Presentation;

public sealed class ProjectCardFactory
{
    public ProjectCard CreateTemplateCard(ProjectPreset preset)
    {
        var options = new ProjectCreationOptions
        {
            Name = preset.Name,
            Width = preset.Width,
            Height = preset.Height,
            FrameCount = preset.FrameCount,
            FramesPerSecond = preset.FramesPerSecond
        };

        return new ProjectCard(
            ProjectCardKind.Template,
            preset.Name,
            preset.Subtitle,
            preset.SizeLabel,
            preset.TimingLabel,
            Initials(preset.Name),
            badgeLabel: preset.Name == "Animation HD" ? "Recommended" : null,
            creationOptions: options);
    }

    public ProjectCard CreateRecentCard(RecentProjectInfo recentProject)
    {
        return new ProjectCard(
            ProjectCardKind.Recent,
            recentProject.Name,
            "Recent project",
            $"{recentProject.Width} × {recentProject.Height}",
            $"{recentProject.FrameCount} frames  •  {recentProject.FramesPerSecond} fps",
            Initials(recentProject.Name),
            updatedLabel: RelativeTime(recentProject.LastOpenedAt),
            projectPath: recentProject.Path,
            thumbnailPath: recentProject.ThumbnailPath,
            thumbnail: TryLoadThumbnail(recentProject.ThumbnailPath),
            badgeColor: "#3B82F6");
    }

    public RecentProjectInfo CreateRecentInfo(DrawingProject project, string path, string? thumbnailPath = null)
    {
        return new RecentProjectInfo(
            path,
            project.Name,
            project.Width,
            project.Height,
            project.FramesPerSecond,
            project.FrameCount,
            thumbnailPath,
            DateTimeOffset.UtcNow);
    }

    private static string Initials(string value)
    {
        var words = value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .ToArray();

        if (words.Length == 0)
        {
            return "F";
        }

        return string.Concat(words.Select(word => char.ToUpperInvariant(word[0])));
    }

    private static IImage? TryLoadThumbnail(string? thumbnailPath)
    {
        if (string.IsNullOrWhiteSpace(thumbnailPath) || !File.Exists(thumbnailPath))
        {
            return null;
        }

        try
        {
            return new Bitmap(thumbnailPath);
        }
        catch
        {
            return null;
        }
    }

    private static string RelativeTime(DateTimeOffset timestamp)
    {
        var elapsed = DateTimeOffset.UtcNow - timestamp;

        if (elapsed.TotalMinutes < 1)
        {
            return "Modified just now";
        }

        if (elapsed.TotalHours < 1)
        {
            return $"Modified {(int)elapsed.TotalMinutes}m ago";
        }

        if (elapsed.TotalDays < 1)
        {
            return $"Modified {(int)elapsed.TotalHours}h ago";
        }

        if (elapsed.TotalDays < 30)
        {
            return $"Modified {(int)elapsed.TotalDays}d ago";
        }

        return "Modified earlier";
    }
}
