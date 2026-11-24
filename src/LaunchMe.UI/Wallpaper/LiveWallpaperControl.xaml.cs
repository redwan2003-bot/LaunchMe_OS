using Windows.Media.Core;
using Windows.Media.Playback;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using LaunchMe.Contracts.Spaces;

namespace LaunchMe.UI.Wallpaper;

public sealed partial class LiveWallpaperControl : UserControl, IDisposable
{
    private LiveWallpaperViewModel? _viewModel;
    private MediaPlayer? _mediaPlayer;
    private bool _disposed;

    public LiveWallpaperViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel != value)
            {
                if (_viewModel != null)
                {
                    _viewModel.PlaybackStateChanged -= OnPlaybackStateChanged;
                }

                _viewModel = value;

                if (_viewModel != null)
                {
                    _viewModel.PlaybackStateChanged += OnPlaybackStateChanged;
                    ApplyCurrentState();
                }
            }
        }
    }

    public LiveWallpaperControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_mediaPlayer == null)
        {
            _mediaPlayer = new MediaPlayer
            {
                IsLoopingEnabled = true,
                Volume = 0.0, // Silent video wallpaper
                IsMuted = true
            };
            VideoPlayer.SetMediaPlayer(_mediaPlayer);
        }

        ApplyCurrentState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        PausePlayback();
    }

    private void OnPlaybackStateChanged(object? sender, WallpaperPlaybackState state)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (state)
            {
                case WallpaperPlaybackState.Playing:
                    StartPlayback();
                    break;
                case WallpaperPlaybackState.Paused:
                    PausePlayback();
                    break;
                case WallpaperPlaybackState.Stopped:
                    StopPlayback();
                    break;
            }
        });
    }

    private void ApplyCurrentState()
    {
        if (_viewModel == null) return;

        if (_viewModel.Type == WallpaperType.LiveVideo)
        {
            StaticWallpaperImage.Visibility = Visibility.Collapsed;
            VideoPlayer.Visibility = Visibility.Visible;

            if (!string.IsNullOrWhiteSpace(_viewModel.AssetPath) && _mediaPlayer != null)
            {
                try
                {
                    var uri = new Uri(_viewModel.AssetPath, UriKind.RelativeOrAbsolute);
                    _mediaPlayer.Source = MediaSource.CreateFromUri(uri);
                }
                catch
                {
                    // Fallback to static if uri invalid
                }
            }

            if (_viewModel.IsPlaying)
            {
                StartPlayback();
            }
            else
            {
                PausePlayback();
            }
        }
        else
        {
            // Static or Dynamic Image
            VideoPlayer.Visibility = Visibility.Collapsed;
            PausePlayback();

            if (!string.IsNullOrWhiteSpace(_viewModel.AssetPath))
            {
                try
                {
                    StaticWallpaperImage.Source = new BitmapImage(new Uri(_viewModel.AssetPath, UriKind.RelativeOrAbsolute));
                    StaticWallpaperImage.Visibility = Visibility.Visible;
                }
                catch
                {
                    StaticWallpaperImage.Visibility = Visibility.Collapsed;
                }
            }
        }
    }

    private void StartPlayback()
    {
        if (_mediaPlayer != null && _viewModel?.Type == WallpaperType.LiveVideo)
        {
            _mediaPlayer.Play();
        }
    }

    private void PausePlayback()
    {
        // Enforce Pause-on-Hide GPU Law: Immediately stop decoding
        _mediaPlayer?.Pause();
    }

    private void StopPlayback()
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Pause();
            _mediaPlayer.Source = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_viewModel != null)
        {
            _viewModel.PlaybackStateChanged -= OnPlaybackStateChanged;
            _viewModel = null;
        }

        if (_mediaPlayer != null)
        {
            _mediaPlayer.Pause();
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }
    }
}
