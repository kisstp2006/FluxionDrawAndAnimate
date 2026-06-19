using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Settings;
using FluxionDrawAndAnimate.Presentation;

namespace FluxionDrawAndAnimate.ViewModels;

public sealed class SettingItemViewModel : ViewModelBase
{
    private readonly SettingDefinition _definition;
    private readonly SettingValueStore _values;
    private readonly Action<SettingItemViewModel> _onChanged;
    private object? _value;

    public SettingItemViewModel(
        SettingDefinition definition,
        SettingValueStore values,
        SettingEditorResolver editorResolver,
        Action<SettingItemViewModel> onChanged)
    {
        _definition = definition;
        _values = values;
        _onChanged = onChanged;
        _value = values.GetValue(definition);

        EditorKind = editorResolver.Resolve(definition);
        Choices = new ObservableCollection<SettingChoiceViewModel>(
            BuildChoices(definition).Select(choice => new SettingChoiceViewModel(choice.Value, choice.Label)));
    }

    public string Key => _definition.Key;
    public string Title => _definition.Title;
    public string Description => _definition.Description;
    public string Group => _definition.Group;
    public string CustomEditorKey => _definition.CustomEditorKey ?? _definition.ValueType.Name;
    public SettingEditorKind EditorKind { get; }
    public ObservableCollection<SettingChoiceViewModel> Choices { get; }

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool IsToggleEditor => EditorKind == SettingEditorKind.Toggle;
    public bool IsNumberEditor => EditorKind == SettingEditorKind.Number;
    public bool IsTextEditor => EditorKind == SettingEditorKind.Text;
    public bool IsChoiceEditor => EditorKind == SettingEditorKind.Choice;
    public bool IsCustomEditor => EditorKind == SettingEditorKind.Custom;
    public bool IsRgbaColorEditor => IsCustomEditor && string.Equals(CustomEditorKey, "rgba-color", StringComparison.OrdinalIgnoreCase);
    public bool IsGenericCustomEditor => IsCustomEditor && !IsRgbaColorEditor;

    public object? Value
    {
        get => _value;
        private set
        {
            if (Equals(_value, value))
            {
                return;
            }

            _value = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BoolValue));
            OnPropertyChanged(nameof(NumberValue));
            OnPropertyChanged(nameof(TextValue));
            OnPropertyChanged(nameof(SelectedChoice));
            OnPropertyChanged(nameof(ValuePreview));
            OnPropertyChanged(nameof(RgbaRed));
            OnPropertyChanged(nameof(RgbaGreen));
            OnPropertyChanged(nameof(RgbaBlue));
            OnPropertyChanged(nameof(RgbaAlpha));
            OnPropertyChanged(nameof(RgbaPreviewBrush));
        }
    }

    public bool BoolValue
    {
        get => Value is bool value && value;
        set => SetSettingValue(value);
    }

    public double NumberValue
    {
        get
        {
            if (Value is null)
            {
                return NumberMinimum;
            }

            return Convert.ToDouble(Value, CultureInfo.InvariantCulture);
        }
        set => SetSettingValue(value);
    }

    public double NumberMinimum => _definition.Minimum ?? 0;
    public double NumberMaximum => _definition.Maximum ?? Math.Max(NumberMinimum + 1, NumberValue + 1);
    public double NumberStep => _definition.Step ?? 1;

    public string TextValue
    {
        get => Value?.ToString() ?? string.Empty;
        set => SetSettingValue(value);
    }

    public SettingChoiceViewModel? SelectedChoice
    {
        get => Choices.FirstOrDefault(choice => Equals(choice.Value, Value));
        set
        {
            if (value is not null)
            {
                SetSettingValue(value.Value);
            }
        }
    }

    public double RgbaRed
    {
        get => GetRgbaColor().R;
        set => SetRgbaColor(red: ToByte(value));
    }

    public double RgbaGreen
    {
        get => GetRgbaColor().G;
        set => SetRgbaColor(green: ToByte(value));
    }

    public double RgbaBlue
    {
        get => GetRgbaColor().B;
        set => SetRgbaColor(blue: ToByte(value));
    }

    public double RgbaAlpha
    {
        get => GetRgbaColor().A;
        set => SetRgbaColor(alpha: ToByte(value));
    }

    public IBrush RgbaPreviewBrush
    {
        get
        {
            var color = GetRgbaColor();
            return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }

    public string ValuePreview => Value switch
    {
        null => "None",
        RgbaColor color => $"rgba({color.R}, {color.G}, {color.B}, {color.A})",
        double value => value.ToString("0.##", CultureInfo.InvariantCulture),
        float value => value.ToString("0.##", CultureInfo.InvariantCulture),
        decimal value => value.ToString("0.##", CultureInfo.InvariantCulture),
        _ => Value.ToString() ?? "None"
    };

    public void ReloadFromStore()
    {
        Value = _values.GetValue(_definition);
    }

    private void SetSettingValue(object? value)
    {
        var normalizedValue = _values.SetValue(_definition, value);
        var changed = !Equals(Value, normalizedValue);
        Value = normalizedValue;

        if (changed)
        {
            _onChanged(this);
        }
    }

    private RgbaColor GetRgbaColor()
    {
        return Value is RgbaColor color ? color : RgbaColor.Black;
    }

    private void SetRgbaColor(byte? red = null, byte? green = null, byte? blue = null, byte? alpha = null)
    {
        var color = GetRgbaColor();
        SetSettingValue(new RgbaColor(
            red ?? color.R,
            green ?? color.G,
            blue ?? color.B,
            alpha ?? color.A));
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp(Math.Round(value), byte.MinValue, byte.MaxValue);
    }

    private static IReadOnlyList<SettingChoice> BuildChoices(SettingDefinition definition)
    {
        if (definition.Choices.Count > 0)
        {
            return definition.Choices;
        }

        if (!definition.ValueType.IsEnum)
        {
            return [];
        }

        return Enum.GetValues(definition.ValueType)
            .Cast<object>()
            .Select(value => new SettingChoice(value, value.ToString() ?? string.Empty))
            .ToArray();
    }
}
