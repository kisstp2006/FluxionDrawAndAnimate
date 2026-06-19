namespace FluxionDrawAndAnimate.Core.Settings;

public sealed class SettingRegistry
{
    private readonly Dictionary<string, SettingDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SettingDefinition> Definitions => _definitions.Values.ToArray();

    public SettingDefinition<T> Register<T>(
        string key,
        string title,
        T defaultValue,
        Action<SettingDefinitionBuilder<T>>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Setting key cannot be empty.", nameof(key));
        }

        if (_definitions.ContainsKey(key))
        {
            throw new InvalidOperationException($"A setting with key '{key}' is already registered.");
        }

        var builder = new SettingDefinitionBuilder<T>(key.Trim(), title, defaultValue);
        configure?.Invoke(builder);

        var definition = builder.Build();
        _definitions.Add(definition.Key, definition);
        return definition;
    }

    public bool TryGet(string key, out SettingDefinition definition)
    {
        return _definitions.TryGetValue(key, out definition!);
    }

    public SettingDefinition Get(string key)
    {
        return TryGet(key, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Setting '{key}' is not registered.");
    }
}
