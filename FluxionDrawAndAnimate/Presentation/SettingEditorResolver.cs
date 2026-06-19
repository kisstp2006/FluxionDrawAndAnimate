using System;
using FluxionDrawAndAnimate.Core.Settings;

namespace FluxionDrawAndAnimate.Presentation;

public sealed class SettingEditorResolver
{
    public SettingEditorKind Resolve(SettingDefinition definition)
    {
        if (definition.EditorHint != SettingEditorKind.Auto)
        {
            return definition.EditorHint;
        }

        if (!string.IsNullOrWhiteSpace(definition.CustomEditorKey))
        {
            return SettingEditorKind.Custom;
        }

        if (definition.Choices.Count > 0 || definition.ValueType.IsEnum)
        {
            return SettingEditorKind.Choice;
        }

        if (definition.ValueType == typeof(bool))
        {
            return SettingEditorKind.Toggle;
        }

        if (IsNumericType(definition.ValueType))
        {
            return SettingEditorKind.Number;
        }

        if (definition.ValueType == typeof(string))
        {
            return SettingEditorKind.Text;
        }

        return SettingEditorKind.Custom;
    }

    private static bool IsNumericType(Type type)
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
}
