using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FluxionDrawAndAnimate.Core.Settings;

namespace FluxionDrawAndAnimate.Services;

public sealed class JsonUserSettingsStore : IUserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly string _filePath;

    public JsonUserSettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluxionDrawAndAnimate",
            "settings.json");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public async Task LoadAsync(SettingValueStore values, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        await using var stream = File.OpenRead(_filePath);
        var savedValues = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(
            stream,
            JsonOptions,
            cancellationToken) ?? [];

        foreach (var savedValue in savedValues)
        {
            if (!values.Registry.TryGet(savedValue.Key, out var definition))
            {
                continue;
            }

            try
            {
                var typedValue = JsonSerializer.Deserialize(
                    savedValue.Value.GetRawText(),
                    definition.ValueType,
                    JsonOptions);
                values.SetValue(definition, typedValue);
            }
            catch
            {
                values.SetValue(definition, definition.DefaultValue);
            }
        }
    }

    public async Task SaveAsync(SettingValueStore values, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var savedValues = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in values.Registry.Definitions)
        {
            savedValues[definition.Key] = JsonSerializer.SerializeToElement(
                values.GetValue(definition),
                definition.ValueType,
                JsonOptions);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, savedValues, JsonOptions, cancellationToken);
    }
}
