using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.Widgets.SystemStats;

public sealed partial class SystemStatsWidgetView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(SystemStatsWidgetViewModel),
            typeof(SystemStatsWidgetView),
            new PropertyMetadata(null));

    public SystemStatsWidgetViewModel? ViewModel
    {
        get => (SystemStatsWidgetViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public SystemStatsWidgetView()
    {
        InitializeComponent();
    }
}
