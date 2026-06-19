namespace FluxionDrawAndAnimate.Core.Settings;

public sealed class SettingValueStore
{
    private readonly SettingRegistry _registry;
    private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

    public SettingValueStore(SettingRegistry registry)
    {
        _registry = registry;

        foreach (var definition in registry.Definitions)
        {
            _values[definition.Key] = definition.DefaultValue;
        }
    }

    public SettingRegistry Registry => _registry;

    public IReadOnlyDictionary<string, object?> Values => _values;

    public object? GetValue(SettingDefinition definition)
    {
        return _values.TryGetValue(definition.Key, out var value)
            ? value
            : definition.DefaultValue;
    }

    public T? GetValue<T>(string key)
    {
        var definition = _registry.Get(key);
        var value = GetValue(definition);
        return value is T typedValue ? typedValue : default;
    }

    public object? SetValue(SettingDefinition definition, object? value)
    {
        var normalizedValue = definition.NormalizeValue(value);
        _values[definition.Key] = normalizedValue;
        return normalizedValue;
    }

    public bool SetValue(string key, object? value, out object? normalizedValue)
    {
        normalizedValue = null;
        if (!_registry.TryGet(key, out var definition))
        {
            return false;
        }

        normalizedValue = SetValue(definition, value);
        return true;
    }
}
