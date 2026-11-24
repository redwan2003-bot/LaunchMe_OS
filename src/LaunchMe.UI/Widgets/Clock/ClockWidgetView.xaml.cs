using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.Widgets.Clock;

public sealed partial class ClockWidgetView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ClockWidgetViewModel),
            typeof(ClockWidgetView),
            new PropertyMetadata(null));

    public ClockWidgetViewModel? ViewModel
    {
        get => (ClockWidgetViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public ClockWidgetView()
    {
        InitializeComponent();
    }
}
