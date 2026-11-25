namespace LaunchMe.UI.Widgets.Models;

/// <summary>
/// Standard widget dimension presets for Photo and ShapePhoto widgets.
/// </summary>
public enum WidgetPresetSize
{
    Small,
    Medium,
    Wide,
    Large
}

public static class WidgetPresetSizeExtensions
{
    public static (double Width, double Height) GetDimensions(this WidgetPresetSize size) => size switch
    {
        WidgetPresetSize.Small => (160, 160),
        WidgetPresetSize.Medium => (240, 240),
        WidgetPresetSize.Wide => (320, 160),
        WidgetPresetSize.Large => (320, 320),
        _ => (240, 240)
    };
}
