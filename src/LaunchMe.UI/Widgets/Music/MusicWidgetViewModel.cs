using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Media;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Models;

namespace LaunchMe.UI.Widgets.Music;

public enum MusicWidgetStyle
{
    Minimal,
    FullCover,
    Vinyl,
    DynamicAccent,
    DynamicSplit,
    CompactIdle
}

public sealed class MusicWidgetViewModel : IWidgetViewModel
{
    private readonly IMediaSessionService _mediaService;
    private readonly Timer? _progressTimer;
    private readonly object _lock = new();

    private string _title = "Not Playing";
    private string _artist = "No active media session";
    private string _album = string.Empty;
    private string? _thumbnailBase64;
    private bool _isPlaying;
    private TimeSpan _position = TimeSpan.Zero;
    private TimeSpan _duration = TimeSpan.Zero;
    private MusicWidgetStyle _style = MusicWidgetStyle.Minimal;
    private bool _isActive = true;
    private bool _isTimerRunning;
    private bool _disposed;

    public string WidgetId { get; } = "widget-music";
    public string Title => "Music";
    public WidgetFamily Family => WidgetFamily.Music;

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
    }

    public string TrackTitle
    {
        get => _title;
        private set { if (_title != value) { _title = value; OnPropertyChanged(); } }
    }

    public string Artist
    {
        get => _artist;
        private set { if (_artist != value) { _artist = value; OnPropertyChanged(); } }
    }

    public string Album
    {
        get => _album;
        private set { if (_album != value) { _album = value; OnPropertyChanged(); } }
    }

    public string? ThumbnailBase64
    {
        get => _thumbnailBase64;
        private set { if (_thumbnailBase64 != value) { _thumbnailBase64 = value; OnPropertyChanged(); } }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlayPauseIconGlyph));
                if (_isPlaying) StartProgressTimer();
                else StopProgressTimer();
            }
        }
    }

    public string PlayPauseIconGlyph => IsPlaying ? "\uE769" : "\uE768"; // Pause vs Play

    public TimeSpan Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                _position = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PositionFormatted));
                OnPropertyChanged(nameof(ProgressRatio));
            }
        }
    }

    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            if (_duration != value)
            {
                _duration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurationFormatted));
                OnPropertyChanged(nameof(ProgressRatio));
            }
        }
    }

    public string PositionFormatted => $"{Position.Minutes:D2}:{Position.Seconds:D2}";
    public string DurationFormatted => $"{Duration.Minutes:D2}:{Duration.Seconds:D2}";

    public double ProgressRatio
    {
        get
        {
            if (Duration.TotalSeconds <= 0) return 0.0;
            return Math.Clamp(Position.TotalSeconds / Duration.TotalSeconds, 0.0, 1.0);
        }
    }

    public ICommand PlayPauseCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand CycleStyleCommand { get; }

    public MusicWidgetStyle Style
    {
        get => _style;
        set { if (_style != value) { _style = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MusicWidgetViewModel(IMediaSessionService mediaService, bool startActive = true)
    {
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));

        PlayPauseCommand = new RelayCommand(() => _ = _mediaService.PlayPauseAsync());
        NextCommand = new RelayCommand(() => _ = _mediaService.NextAsync());
        PreviousCommand = new RelayCommand(() => _ = _mediaService.PreviousAsync());
        CycleStyleCommand = new RelayCommand(() =>
        {
            var values = Enum.GetValues<MusicWidgetStyle>();
            Style = values[((int)Style + 1) % values.Length];
        });

        _mediaService.TrackChanged += OnTrackChanged;
        _progressTimer = new Timer(OnProgressTick, null, Timeout.Infinite, Timeout.Infinite);

        _ = RefreshCurrentTrackAsync();

        if (startActive)
        {
            StartProgressTimer();
        }
    }

    private async Task RefreshCurrentTrackAsync()
    {
        try
        {
            var track = await _mediaService.GetCurrentTrackAsync();
            if (track != null)
            {
                UpdateFromTrack(track);
            }
        }
        catch
        {
            // Ignored if GSMTC unavailable
        }
    }

    private void OnTrackChanged(object? sender, MediaTrackInfo track)
    {
        UpdateFromTrack(track);
    }

    public void UpdateFromTrack(MediaTrackInfo track)
    {
        ArgumentNullException.ThrowIfNull(track);

        TrackTitle = string.IsNullOrWhiteSpace(track.Title) ? "Unknown Title" : track.Title;
        Artist = string.IsNullOrWhiteSpace(track.Artist) ? "Unknown Artist" : track.Artist;
        Album = track.AlbumTitle ?? string.Empty;
        ThumbnailBase64 = track.ThumbnailBase64;
        Duration = track.Duration;
        Position = track.Position;
        IsPlaying = track.IsPlaying;
    }

    private void OnProgressTick(object? state)
    {
        if (!_isActive || !_isPlaying || !_isTimerRunning) return;

        if (Position < Duration)
        {
            Position = Position.Add(TimeSpan.FromSeconds(1));
        }
    }

    private void StartProgressTimer()
    {
        lock (_lock)
        {
            if (_disposed || !_isPlaying) return;
            _isTimerRunning = true;
            _progressTimer?.Change(1000, 1000);
        }
    }

    private void StopProgressTimer()
    {
        lock (_lock)
        {
            _isTimerRunning = false;
            _progressTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public void OnLauncherHidden()
    {
        // Pause-on-Hide GPU Law: Suspend playback progress interpolation timer
        StopProgressTimer();
    }

    public void OnLauncherShown()
    {
        _ = RefreshCurrentTrackAsync();
        if (IsPlaying)
        {
            StartProgressTimer();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _mediaService.TrackChanged -= OnTrackChanged;
        StopProgressTimer();
        _progressTimer?.Dispose();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
