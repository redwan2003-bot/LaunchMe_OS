using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.Widgets.Weather;

/// <summary>Selects one of the 9 Weather widget visual skins.</summary>
public sealed class WeatherStyleTemplateSelector : DataTemplateSelector
{
    public DataTemplate? MinimalTemplate { get; set; }
    public DataTemplate? CompactTemplate { get; set; }
    public DataTemplate? ForecastTemplate { get; set; }
    public DataTemplate? PanelTemplate { get; set; }
    public DataTemplate? AlmanacTemplate { get; set; }
    public DataTemplate? HorizonTemplate { get; set; }
    public DataTemplate? BoldTemplate { get; set; }
    public DataTemplate? StoryTemplate { get; set; }
    public DataTemplate? EditorialGlassTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item) =>
        item is WeatherWidgetViewModel vm ? Pick(vm.Style) : base.SelectTemplateCore(item);

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) =>
        item is WeatherWidgetViewModel vm ? Pick(vm.Style) : base.SelectTemplateCore(item, container);

    private DataTemplate Pick(WeatherStyle style) => style switch
    {
        WeatherStyle.Minimal        => MinimalTemplate        ?? Fallback,
        WeatherStyle.Compact        => CompactTemplate        ?? Fallback,
        WeatherStyle.Forecast       => ForecastTemplate       ?? Fallback,
        WeatherStyle.Panel          => PanelTemplate          ?? Fallback,
        WeatherStyle.Almanac        => AlmanacTemplate        ?? Fallback,
        WeatherStyle.Horizon        => HorizonTemplate        ?? Fallback,
        WeatherStyle.Bold           => BoldTemplate           ?? Fallback,
        WeatherStyle.Story          => StoryTemplate          ?? Fallback,
        WeatherStyle.EditorialGlass => EditorialGlassTemplate ?? Fallback,
        _                           => ForecastTemplate       ?? Fallback
    };

    private static DataTemplate? _fallback;
    private static DataTemplate Fallback => _fallback ??= new DataTemplate();
}
