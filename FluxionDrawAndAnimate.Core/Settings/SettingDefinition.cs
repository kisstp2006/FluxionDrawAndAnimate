using System.Globalization;

namespace FluxionDrawAndAnimate.Core.Settings;

public abstract class SettingDefinition
{
    protected SettingDefinition(
        string key,
        string group,
        string title,
        string description,
        Type valueType,
        object? defaultValue,
        SettingEditorKind editorHint,
        string? customEditorKey,
        double? minimum,
        double? maximum,
        double? step,
        IReadOnlyList<SettingChoice> choices)
    {
        Key = key;
        Group = group;
        Title = title;
        Description = description;
        ValueType = valueType;
        DefaultValue = defaultValue;
        EditorHint = editorHint;
        CustomEditorKey = customEditorKey;
        Minimum = minimum;
        Maximum = maximum;
        Step = step;
        Choices = choices;
    }

    public string Key { get; }
    public string Group { get; }
    public string Title { get; }
    public string Description { get; }
    public Type ValueType { get; }
    public object? DefaultValue { get; }
    public SettingEditorKind EditorHint { get; }
    public string? CustomEditorKey { get; }
    public double? Minimum { get; }
    public double? Maximum { get; }
    public double? Step { get; }
    public IReadOnlyList<SettingChoice> Choices { get; }

    public abstract object? NormalizeValue(object? value);

    protected static bool IsNumericType(Type type)
    {
        var targetType = Nullable.GetUnderlyingType(type) ?? type;
        return targetType == typeof(byte)
            || targetType == typeof(short)
            || targetType == typeof(int)
            || targetType == typeof(long)
            || targetType == typeof(float)
            || targetType == typeof(double)
            || targetType == typeof(decimal);
    }

    protected object? NormalizeNumericValue(object value, Type targetType)
    {
        var numericValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);

        if (Minimum is not null)
        {
            numericValue = Math.Max(Minimum.Value, numericValue);
        }

        if (Maximum is not null)
        {
            numericValue = Math.Min(Maximum.Value, numericValue);
        }

        var nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return Convert.ChangeType(numericValue, nonNullableType, CultureInfo.InvariantCulture);
    }
}

public sealed class SettingDefinition<T> : SettingDefinition
{
    internal SettingDefinition(
        string key,
        string group,
        string title,
        string description,
        T defaultValue,
        SettingEditorKind editorHint,
        string? customEditorKey,
        double? minimum,
        double? maximum,
        double? step,
        IReadOnlyList<SettingChoice> choices)
        : base(
            key,
            group,
            title,
            description,
            typeof(T),
            defaultValue,
            editorHint,
            customEditorKey,
            minimum,
            maximum,
            step,
            choices)
    {
    }

    public override object? NormalizeValue(object? value)
    {
        if (value is null)
        {
            return default(T);
        }

        if (value is T typedValue)
        {
            if (IsNumericType(typeof(T)))
            {
                return NormalizeNumericValue(typedValue, typeof(T));
            }

            return typedValue;
        }

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (targetType.IsEnum)
        {
            return value is string text
                ? Enum.Parse(targetType, text, ignoreCase: true)
                : Enum.ToObject(targetType, value);
        }

        if (IsNumericType(targetType))
        {
            return NormalizeNumericValue(value, targetType);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}
