using System.ComponentModel;
using LaunchMe.UI.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace LaunchMe.UI.Widgets.PixelArt;

public sealed partial class PixelArtWidgetView : UserControl
{
    private readonly Border[] _cells = new Border[25];

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PixelArtWidgetViewModel),
            typeof(PixelArtWidgetView),
            new PropertyMetadata(null, OnViewModelChanged));

    public PixelArtWidgetViewModel? ViewModel
    {
        get => (PixelArtWidgetViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public PixelArtWidgetView()
    {
        InitializeComponent();
        InitializePixelGrid();
    }

    private void InitializePixelGrid()
    {
        for (int i = 0; i < 5; i++)
        {
            PixelSpriteGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            PixelSpriteGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 5; c++)
            {
                var cell = new Border
                {
                    Margin = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0))
                };
                Grid.SetRow(cell, r);
                Grid.SetColumn(cell, c);
                PixelSpriteGrid.Children.Add(cell);
                _cells[r * 5 + c] = cell;
            }
        }
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PixelArtWidgetView view) return;

        if (e.OldValue is PixelArtWidgetViewModel oldVm)
        {
            oldVm.PropertyChanged -= view.OnViewModelPropertyChanged;
        }

        if (e.NewValue is PixelArtWidgetViewModel newVm)
        {
            newVm.PropertyChanged += view.OnViewModelPropertyChanged;
            view.RenderCurrentFrame();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PixelArtWidgetViewModel.CurrentFrameIndex) or nameof(PixelArtWidgetViewModel.SelectedPreset))
        {
            DispatcherQueue?.TryEnqueue(RenderCurrentFrame);
        }
    }

    private void RenderCurrentFrame()
    {
        if (ViewModel is null) return;
        var colors = ViewModel.GetCurrentFrameColors();
        var transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        for (int i = 0; i < _cells.Length && i < colors.Count; i++)
        {
            string? hex = colors[i];
            if (string.IsNullOrWhiteSpace(hex))
            {
                _cells[i].Background = transparent;
            }
            else
            {
                var parsed = LiquidGlassTintService.ParseHexColor(hex, Color.FromArgb(255, 56, 189, 248));
                _cells[i].Background = new SolidColorBrush(parsed);
            }
        }
    }
}
