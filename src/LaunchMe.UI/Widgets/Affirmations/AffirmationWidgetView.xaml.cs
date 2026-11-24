using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.Widgets.Affirmations;

public sealed partial class AffirmationWidgetView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(AffirmationWidgetViewModel),
            typeof(AffirmationWidgetView),
            new PropertyMetadata(null));

    public AffirmationWidgetViewModel? ViewModel
    {
        get => (AffirmationWidgetViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public AffirmationWidgetView()
    {
        InitializeComponent();
    }
}
