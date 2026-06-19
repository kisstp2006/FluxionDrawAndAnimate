using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FluxionDrawAndAnimate.Services;

public sealed class JsonRecentProjectStore : IRecentProjectStore
{
    private const int MaxRecentProjects = 12;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public JsonRecentProjectStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluxionDrawAndAnimate",
            "recent-projects.json");
    }

    public async Task<IReadOnlyList<RecentProjectInfo>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);
        var projects = await JsonSerializer.DeserializeAsync<List<RecentProjectInfo>>(stream, JsonOptions, cancellationToken);
        return projects?
            .Where(project => !string.IsNullOrWhiteSpace(project.Path))
            .OrderByDescending(project => project.LastOpenedAt)
            .Take(MaxRecentProjects)
            .ToArray() ?? [];
    }

    public async Task AddOrUpdateAsync(RecentProjectInfo project, CancellationToken cancellationToken = default)
    {
        var projects = (await LoadAsync(cancellationToken))
            .Where(existing => !string.Equals(existing.Path, project.Path, StringComparison.OrdinalIgnoreCase))
            .Prepend(project with { LastOpenedAt = DateTimeOffset.UtcNow })
            .OrderByDescending(existing => existing.LastOpenedAt)
            .Take(MaxRecentProjects)
            .ToList();

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, projects, JsonOptions, cancellationToken);
    }
}
