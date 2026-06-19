namespace FluxionDrawAndAnimate.ViewModels;

public sealed class SettingChoiceViewModel
{
    public SettingChoiceViewModel(object? value, string label)
    {
        Value = value;
        Label = label;
    }

    public object? Value { get; }
    public string Label { get; }
}
