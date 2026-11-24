using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.Widgets.Calendar;

public sealed partial class CalendarWidgetView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(CalendarWidgetViewModel),
            typeof(CalendarWidgetView),
            new PropertyMetadata(null));

    public CalendarWidgetViewModel? ViewModel
    {
        get => (CalendarWidgetViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public CalendarWidgetView()
    {
        InitializeComponent();
    }
}
