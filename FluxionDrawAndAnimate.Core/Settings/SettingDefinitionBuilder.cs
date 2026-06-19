namespace FluxionDrawAndAnimate.Core.Settings;

public sealed class SettingDefinitionBuilder<T>
{
    private readonly string _key;
    private readonly string _title;
    private readonly T _defaultValue;
    private readonly List<SettingChoice> _choices = [];

    private string _group = "General";
    private string _description = string.Empty;
    private SettingEditorKind _editorHint = SettingEditorKind.Auto;
    private string? _customEditorKey;
    private double? _minimum;
    private double? _maximum;
    private double? _step;

    internal SettingDefinitionBuilder(string key, string title, T defaultValue)
    {
        _key = key;
        _title = title;
        _defaultValue = defaultValue;
    }

    public SettingDefinitionBuilder<T> InGroup(string group)
    {
        _group = string.IsNullOrWhiteSpace(group) ? "General" : group.Trim();
        return this;
    }

    public SettingDefinitionBuilder<T> WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public SettingDefinitionBuilder<T> WithEditor(SettingEditorKind editorKind)
    {
        _editorHint = editorKind;
        return this;
    }

    public SettingDefinitionBuilder<T> WithRange(double minimum, double maximum, double step = 1)
    {
        _minimum = minimum;
        _maximum = maximum;
        _step = step;
        return this;
    }

    public SettingDefinitionBuilder<T> AddChoice(T value, string label)
    {
        _choices.Add(new SettingChoice(value, label));
        return this;
    }

    public SettingDefinitionBuilder<T> WithCustomEditor(string editorKey)
    {
        _editorHint = SettingEditorKind.Custom;
        _customEditorKey = editorKey;
        return this;
    }

    internal SettingDefinition<T> Build()
    {
        return new SettingDefinition<T>(
            _key,
            _group,
            _title,
            _description,
            _defaultValue,
            _editorHint,
            _customEditorKey,
            _minimum,
            _maximum,
            _step,
            _choices.ToArray());
    }
}
