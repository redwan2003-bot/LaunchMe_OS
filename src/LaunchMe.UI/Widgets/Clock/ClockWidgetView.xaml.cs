using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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
        Loaded += (_, _) => CompositionTarget.Rendering += OnCompositionRendering;
        Unloaded += (_, _) => CompositionTarget.Rendering -= OnCompositionRendering;
    }

    private void OnCompositionRendering(object? sender, object e)
    {
        if (ViewModel is { IsTimerRunning: true, EnableSmoothSweep: true })
        {
            ViewModel.TickSmoothSweep();
        }
    }
}
