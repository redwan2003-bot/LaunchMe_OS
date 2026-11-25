using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Media;

namespace LaunchMe.UI.ViewModels;

public sealed class MusicPlayerViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IMediaSessionService? _mediaService;

    private MediaTrackInfo? _currentTrack;
    private string _trackTitle = "No Track Playing";
    private string _artist = "Unknown Artist";
    private string _album = string.Empty;
    private TimeSpan _position = TimeSpan.Zero;
    private TimeSpan _duration = TimeSpan.Zero;
    private double _progressFraction;
    private bool _isPlaying;
    private MusicVisualizerMode _visualizerMode = MusicVisualizerMode.Vinyl;
    private double _vinylAngle;
    private double _glowPulse = 0.8;
    private bool _disposed;

    private readonly PeriodicTimer _visualizerTimer;
    private readonly CancellationTokenSource _cts = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? DismissRequested;

    public MediaTrackInfo? CurrentTrack
    {
        get => _currentTrack;
        private set { _currentTrack = value; OnPropertyChanged(); }
    }

    public string TrackTitle
    {
        get => _trackTitle;
        set { if (_trackTitle != value) { _trackTitle = value; OnPropertyChanged(); } }
    }

    public string Artist
    {
        get => _artist;
        set { if (_artist != value) { _artist = value; OnPropertyChanged(); } }
    }

    public string Album
    {
        get => _album;
        set { if (_album != value) { _album = value; OnPropertyChanged(); } }
    }

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
            }
        }
    }

    public string PositionFormatted => $"{Position.Minutes:D2}:{Position.Seconds:D2}";
    public string DurationFormatted => $"{Duration.Minutes:D2}:{Duration.Seconds:D2}";

    public double ProgressFraction
    {
        get => _progressFraction;
        set { if (_progressFraction != value) { _progressFraction = value; OnPropertyChanged(); } }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set { if (_isPlaying != value) { _isPlaying = value; OnPropertyChanged(); } }
    }

    public MusicVisualizerMode VisualizerMode
    {
        get => _visualizerMode;
        set { if (_visualizerMode != value) { _visualizerMode = value; OnPropertyChanged(); } }
    }

    public double VinylAngle
    {
        get => _vinylAngle;
        set { if (_vinylAngle != value) { _vinylAngle = value; OnPropertyChanged(); } }
    }

    public double GlowPulse
    {
        get => _glowPulse;
        set { if (_glowPulse != value) { _glowPulse = value; OnPropertyChanged(); } }
    }

    public ICommand PlayPauseCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand CycleModeCommand { get; }
    public ICommand DismissCommand { get; }

    public MusicPlayerViewModel(IMediaSessionService? mediaService = null)
    {
        _mediaService = mediaService;

        PlayPauseCommand = new RelayCommand(async () => await PlayPauseAsync());
        NextCommand = new RelayCommand(async () => await NextAsync());
        PreviousCommand = new RelayCommand(async () => await PreviousAsync());
        CycleModeCommand = new RelayCommand(CycleVisualizerMode);
        DismissCommand = new RelayCommand(() => DismissRequested?.Invoke(this, EventArgs.Empty));

        if (_mediaService != null)
        {
            _mediaService.TrackChanged += OnTrackChanged;
            _ = InitializeTrackAsync();
        }

        // Visualizer tick at 30 fps (33ms)
        _visualizerTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
        _ = RunVisualizerLoopAsync(_cts.Token);
    }

    private async Task InitializeTrackAsync()
    {
        if (_mediaService == null) return;
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
        }
    }

    private void OnTrackChanged(object? sender, MediaTrackInfo track)
    {
        UpdateFromTrack(track);
    }

    public void UpdateFromTrack(MediaTrackInfo track)
    {
        CurrentTrack = track;
        TrackTitle = string.IsNullOrWhiteSpace(track.Title) ? "No Track Playing" : track.Title;
        Artist = track.Artist;
        Album = track.AlbumTitle;
        IsPlaying = track.IsPlaying;
        Position = track.Position;
        Duration = track.Duration;

        ProgressFraction = track.Duration.TotalSeconds > 0
            ? Math.Clamp(track.Position.TotalSeconds / track.Duration.TotalSeconds, 0.0, 1.0)
            : 0.0;
    }

    public async Task PlayPauseAsync()
    {
        if (_mediaService != null)
        {
            await _mediaService.PlayPauseAsync();
        }
        IsPlaying = !IsPlaying;
    }

    public async Task NextAsync()
    {
        if (_mediaService != null)
        {
            await _mediaService.NextAsync();
        }
    }

    public async Task PreviousAsync()
    {
        if (_mediaService != null)
        {
            await _mediaService.PreviousAsync();
        }
    }

    public async Task SeekAsync(double fraction)
    {
        if (_mediaService != null && Duration.TotalSeconds > 0)
        {
            var target = TimeSpan.FromSeconds(Math.Clamp(fraction, 0.0, 1.0) * Duration.TotalSeconds);
            Position = target;
            await _mediaService.SeekAsync(target);
        }
    }

    public void CycleVisualizerMode()
    {
        VisualizerMode = VisualizerMode switch
        {
            MusicVisualizerMode.Vinyl => MusicVisualizerMode.FullScreenArt,
            MusicVisualizerMode.FullScreenArt => MusicVisualizerMode.Particles,
            MusicVisualizerMode.Particles => MusicVisualizerMode.GlowingBeats,
            MusicVisualizerMode.GlowingBeats => MusicVisualizerMode.Vinyl,
            _ => MusicVisualizerMode.Vinyl
        };
    }

    private async Task RunVisualizerLoopAsync(CancellationToken ct)
    {
        try
        {
            while (await _visualizerTimer.WaitForNextTickAsync(ct))
            {
                if (_disposed) break;

                // Respect Pause-on-Hide GPU Law: Only animate if playing!
                if (IsPlaying)
                {
                    VinylAngle = (VinylAngle + 2.5) % 360.0;
                    GlowPulse = 0.5 + 0.5 * Math.Sin(DateTime.UtcNow.TimeOfDay.TotalSeconds * 4.0);

                    if (Duration.TotalSeconds > 0)
                    {
                        var newPos = Position + TimeSpan.FromMilliseconds(33);
                        if (newPos <= Duration)
                        {
                            Position = newPos;
                            ProgressFraction = Math.Clamp(Position.TotalSeconds / Duration.TotalSeconds, 0.0, 1.0);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _visualizerTimer.Dispose();
        _cts.Dispose();

        if (_mediaService != null)
        {
            _mediaService.TrackChanged -= OnTrackChanged;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
