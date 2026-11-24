using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.Widgets.Pomodoro;

public sealed partial class PomodoroWidgetView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PomodoroWidgetViewModel),
            typeof(PomodoroWidgetView),
            new PropertyMetadata(null));

    public PomodoroWidgetViewModel? ViewModel
    {
        get => (PomodoroWidgetViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public PomodoroWidgetView()
    {
        InitializeComponent();
    }
}
