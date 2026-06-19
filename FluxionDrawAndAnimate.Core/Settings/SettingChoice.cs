namespace FluxionDrawAndAnimate.Core.Settings;

public sealed class SettingChoice
{
    public SettingChoice(object? value, string label)
    {
        Value = value;
        Label = label;
    }

    public object? Value { get; }
    public string Label { get; }
}
