using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.UI.Theming;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace LaunchMe.UI.Widgets.Gif;

public sealed class GifWidgetViewModel : IWidgetViewModel
{
    private static readonly Color DefaultFillColor = Color.FromArgb(255, 30, 41, 59); // #1E293B
    private static readonly Color TransparentColor = Color.FromArgb(0, 0, 0, 0);

    private string? _gifPath;
    private bool _isTransparentBackground;
    private string _fillColorHex = "#1E293B";
    private bool _isPlaying;
    private bool _isLauncherHidden;
    private bool _isActive = true;
    private bool _disposed;

    public string WidgetId { get; } = "widget-gif";
    public string Title => "GIF";
    public WidgetFamily Family => WidgetFamily.Gif;

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
    }

    public string? GifPath
    {
        get => _gifPath;
        set
        {
            if (_gifPath != value)
            {
                _gifPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasGif));
                OnPropertyChanged(nameof(GifVisibility));
                OnPropertyChanged(nameof(PlaceholderVisibility));
                UpdatePlaybackState();
            }
        }
    }

    public bool HasGif => !string.IsNullOrWhiteSpace(GifPath);
    public Visibility GifVisibility => HasGif ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PlaceholderVisibility => HasGif ? Visibility.Collapsed : Visibility.Visible;

    public bool IsTransparentBackground
    {
        get => _isTransparentBackground;
        set
        {
            if (_isTransparentBackground != value)
            {
                _isTransparentBackground = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResolvedBackgroundColor));
                OnPropertyChanged(nameof(BackgroundBrush));
            }
        }
    }

    public string FillColorHex
    {
        get => _fillColorHex;
        set
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? "#1E293B" : value.Trim();
            if (!string.Equals(_fillColorHex, normalized, StringComparison.Ordinal))
            {
                _fillColorHex = normalized;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResolvedBackgroundColor));
                OnPropertyChanged(nameof(BackgroundBrush));
            }
        }
    }

    public Color ResolvedBackgroundColor =>
        IsTransparentBackground
            ? TransparentColor
            : LiquidGlassTintService.ParseHexColor(FillColorHex, DefaultFillColor);

    public SolidColorBrush? BackgroundBrush => ThemeVisualState.TryCreateBrush(ResolvedBackgroundColor);

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaybackStatusText));
                PlaybackStateChanged?.Invoke(this, _isPlaying);
            }
        }
    }

    public string PlaybackStatusText => IsPlaying ? "Playing" : (HasGif ? "Paused" : "No GIF loaded");

    public ICommand ToggleTransparentBackgroundCommand { get; }
    public ICommand ClearGifCommand { get; }

    public event EventHandler<bool>? PlaybackStateChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public GifWidgetViewModel()
    {
        ToggleTransparentBackgroundCommand = new RelayCommand(() => IsTransparentBackground = !IsTransparentBackground);
        ClearGifCommand = new RelayCommand(() => GifPath = null);
    }

    private void UpdatePlaybackState()
    {
        IsPlaying = !_disposed && !_isLauncherHidden && IsActive && HasGif;
    }

    public void OnLauncherHidden()
    {
        // Pause-on-Hide GPU Law: immediately pause animated GIF frame decoding
        _isLauncherHidden = true;
        IsPlaying = false;
    }

    public void OnLauncherShown()
    {
        _isLauncherHidden = false;
        UpdatePlaybackState();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        IsPlaying = false;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
