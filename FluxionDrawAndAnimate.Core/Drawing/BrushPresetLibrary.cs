using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// Loads built-in <see cref="BrushPreset"/> instances from embedded JSON resources
/// (matching the workflow used by Krita / ToonSquid — brushes are data, not code).
///
/// Resources live under <c>Resources/Brushes/*.json</c> in the Core project and are
/// embedded via the csproj <c>&lt;EmbeddedResource&gt;</c> item group. The library
/// loads them once, sorts by <see cref="BrushPreset.SortOrder"/>, and caches the list.
///
/// B/5 will add user presets (loaded from the app data directory and merged in).
/// </summary>
public sealed class BrushPresetLibrary : IBrushLibrary
{
    private const string ResourcePrefix = "FluxionDrawAndAnimate.Core.Resources.Brushes.";
    private const string ResourceSuffix = ".json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static BrushPresetLibrary? _default;

    private readonly List<BrushPreset> _builtIn;

    public BrushPresetLibrary(IEnumerable<BrushPreset> builtIn)
    {
        _builtIn = [..builtIn];
    }

    /// <summary>Shared default instance backed by the embedded built-in presets.</summary>
    public static BrushPresetLibrary Default => _default ??= LoadEmbedded();

    public IReadOnlyList<BrushPreset> BuiltIn => _builtIn;

    public BrushPreset? FindById(string id)
    {
        foreach (var preset in _builtIn)
        {
            if (preset.Id == id)
            {
                return preset;
            }
        }

        return null;
    }

    /// <summary>
    /// Loads every <c>*.json</c> brush preset embedded in the Core assembly.
    /// Missing or malformed files are skipped (a built-in brush must never crash startup).
    /// </summary>
    public static BrushPresetLibrary LoadEmbedded(Assembly? assembly = null)
    {
        assembly ??= typeof(BrushPresetLibrary).Assembly;
        var presets = new List<BrushPreset>();

        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(ResourcePrefix, System.StringComparison.Ordinal) ||
                !name.EndsWith(ResourceSuffix, System.StringComparison.Ordinal))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var preset = TryDeserialize(json);
            if (preset is not null)
            {
                presets.Add(preset);
            }
        }

        presets.Sort((a, b) =>
        {
            var c = a.SortOrder.CompareTo(b.SortOrder);
            return c != 0 ? c : string.CompareOrdinal(a.Name, b.Name);
        });

        return new BrushPresetLibrary(presets);
    }

    private static BrushPreset? TryDeserialize(string json)
    {
        try
        {
            var preset = JsonSerializer.Deserialize<BrushPreset>(json, JsonOptions);
            if (preset is null || string.IsNullOrWhiteSpace(preset.Id))
            {
                return null;
            }

            return preset;
        }
        catch (JsonException)
        {
            // A malformed built-in brush is logged once at startup in the host;
            // the library keeps loading the rest.
            return null;
        }
    }
}
