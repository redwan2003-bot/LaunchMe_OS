using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace LaunchMe.UI.Widgets.Gif;

public sealed partial class GifWidgetView : UserControl
{
    private BitmapImage? _bitmapImage;

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(GifWidgetViewModel),
            typeof(GifWidgetView),
            new PropertyMetadata(null, OnViewModelChanged));

    public GifWidgetViewModel? ViewModel
    {
        get => (GifWidgetViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public GifWidgetView()
    {
        InitializeComponent();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not GifWidgetView view) return;

        if (e.OldValue is GifWidgetViewModel oldVm)
        {
            oldVm.PropertyChanged -= view.OnViewModelPropertyChanged;
            oldVm.PlaybackStateChanged -= view.OnPlaybackStateChanged;
        }

        if (e.NewValue is GifWidgetViewModel newVm)
        {
            newVm.PropertyChanged += view.OnViewModelPropertyChanged;
            newVm.PlaybackStateChanged += view.OnPlaybackStateChanged;
            view.LoadGifSource();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GifWidgetViewModel.GifPath))
        {
            DispatcherQueue?.TryEnqueue(LoadGifSource);
        }
    }

    private void OnPlaybackStateChanged(object? sender, bool isPlaying)
    {
        DispatcherQueue?.TryEnqueue(() => ApplyPlaybackState(isPlaying));
    }

    private void LoadGifSource()
    {
        _bitmapImage?.Stop();
        _bitmapImage = null;

        string? path = ViewModel?.GifPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            GifImage.Source = null;
            return;
        }

        try
        {
            var bitmap = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute))
            {
                AutoPlay = ViewModel?.IsPlaying ?? false
            };
            _bitmapImage = bitmap;
            GifImage.Source = bitmap;
            ApplyPlaybackState(ViewModel?.IsPlaying ?? false);
        }
        catch
        {
            _bitmapImage = null;
            GifImage.Source = null;
        }
    }

    private void ApplyPlaybackState(bool isPlaying)
    {
        if (_bitmapImage is null) return;

        if (isPlaying)
        {
            _bitmapImage.Play();
        }
        else
        {
            _bitmapImage.Stop();
        }
    }
}
