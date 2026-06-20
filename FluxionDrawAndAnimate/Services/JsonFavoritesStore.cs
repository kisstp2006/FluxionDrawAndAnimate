using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FluxionDrawAndAnimate.Services;

public sealed class JsonFavoritesStore : IFavoritesStore
{
    private readonly string _filePath;

    public JsonFavoritesStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Fluxion");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "favorites.json");
    }

    public async Task<HashSet<string>> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return new HashSet<string>(list ?? [], StringComparer.OrdinalIgnoreCase);
        }
        catch { return new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
    }

    public async Task SaveAsync(IEnumerable<string> projectPaths)
    {
        var json = JsonSerializer.Serialize(new List<string>(projectPaths));
        await File.WriteAllTextAsync(_filePath, json);
    }
}
