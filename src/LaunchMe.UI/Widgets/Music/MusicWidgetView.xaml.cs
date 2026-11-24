using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.Widgets.Music;

public sealed partial class MusicWidgetView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(MusicWidgetViewModel),
            typeof(MusicWidgetView),
            new PropertyMetadata(null));

    public MusicWidgetViewModel? ViewModel
    {
        get => (MusicWidgetViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public MusicWidgetView()
    {
        InitializeComponent();
    }
}
