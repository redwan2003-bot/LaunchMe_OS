using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.Widgets.Catalog;

public sealed partial class WidgetCatalogView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(WidgetCatalogViewModel),
            typeof(WidgetCatalogView),
            new PropertyMetadata(null));

    public WidgetCatalogViewModel? ViewModel
    {
        get => (WidgetCatalogViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public WidgetCatalogView()
    {
        InitializeComponent();
    }
}
