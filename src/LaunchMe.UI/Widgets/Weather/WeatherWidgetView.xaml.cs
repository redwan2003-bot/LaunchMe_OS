using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.Widgets.Weather;

public sealed partial class WeatherWidgetView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(WeatherWidgetViewModel),
            typeof(WeatherWidgetView),
            new PropertyMetadata(null));

    public WeatherWidgetViewModel? ViewModel
    {
        get => (WeatherWidgetViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public WeatherWidgetView()
    {
        InitializeComponent();
    }
}
