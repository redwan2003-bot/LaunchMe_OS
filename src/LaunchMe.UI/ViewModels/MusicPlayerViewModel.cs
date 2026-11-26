using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.ViewModels;

public sealed class VisualizerParticle : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private double _opacity;
    public int Index { get; init; }
    public double Size { get; init; }
    public string ColorHex { get; init; } = "#FF00F0FF";

    public double X
    {
        get => _x;
        set { if (Math.Abs(_x - value) > 0.05) { _x = value; OnPropertyChanged(); } }
    }

    public double Y
    {
        get => _y;
        set { if (Math.Abs(_y - value) > 0.05) { _y = value; OnPropertyChanged(); } }
    }

    public double Opacity
    {
        get => _opacity;
        set { if (Math.Abs(_opacity - value) > 0.01) { _opacity = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class VisualizerBeatBar : INotifyPropertyChanged
{
    private double _heightPx = 24;
    private double _opacity = 0.7;
    public int Index { get; init; }
    public string ColorHex { get; init; } = "#FF00F0FF";

    public double HeightPx
    {
        get => _heightPx;
        set { if (Math.Abs(_heightPx - value) > 0.2) { _heightPx = value; OnPropertyChanged(); } }
    }

    public double Opacity
    {
        get => _opacity;
        set { if (Math.Abs(_opacity - value) > 0.01) { _opacity = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

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
    private bool _isHidden;
    private MusicVisualizerMode _visualizerMode = MusicVisualizerMode.Vinyl;
    private double _vinylAngle;
    private double _glowPulse = 0.8;
    private double _artScale = 1.0;
    private double _artParallaxX;
    private double _artParallaxY;
    private double _beatRingScale = 1.0;
    private bool _isVerticalLayout = true;
    private bool _syncWallpaperToArt;
    private double _phase;
    private bool _disposed;

    private readonly PeriodicTimer _visualizerTimer;
    private readonly CancellationTokenSource _cts = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? DismissRequested;
    public event EventHandler<MediaTrackInfo>? MusicArtWallpaperSyncRequested;

    public ObservableCollection<VisualizerParticle> ParticleNodes { get; } = [];
    public ObservableCollection<VisualizerBeatBar> BeatBars { get; } = [];

    public MediaTrackInfo? CurrentTrack
    {
        get => _currentTrack;
        private set { _currentTrack = value; OnPropertyChanged(); }
    }

    public string TrackTitle
    {
        get => _trackTitle;
        set
        {
            if (_trackTitle != value)
            {
                _trackTitle = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TrackMonogram));
            }
        }
    }

    public string TrackMonogram =>
        string.IsNullOrWhiteSpace(_trackTitle) || _trackTitle == "No Track Playing"
            ? "LM"
            : _trackTitle.Trim()[..Math.Min(2, _trackTitle.Trim().Length)].ToUpperInvariant();

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

    public bool IsHidden
    {
        get => _isHidden;
        private set { if (_isHidden != value) { _isHidden = value; OnPropertyChanged(); } }
    }

    public MusicVisualizerMode VisualizerMode
    {
        get => _visualizerMode;
        set
        {
            if (_visualizerMode != value)
            {
                _visualizerMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VisualizerModeLabel));
                OnPropertyChanged(nameof(VinylModeVisibility));
                OnPropertyChanged(nameof(FullScreenArtModeVisibility));
                OnPropertyChanged(nameof(ParticlesModeVisibility));
                OnPropertyChanged(nameof(GlowingBeatsModeVisibility));
            }
        }
    }

    public string VisualizerModeLabel => _visualizerMode switch
    {
        MusicVisualizerMode.Vinyl => "Vinyl",
        MusicVisualizerMode.FullScreenArt => "Full Screen Art",
        MusicVisualizerMode.Particles => "Particles",
        MusicVisualizerMode.GlowingBeats => "Glowing Beats",
        _ => "Vinyl"
    };

    public Visibility VinylModeVisibility =>
        _visualizerMode == MusicVisualizerMode.Vinyl ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FullScreenArtModeVisibility =>
        _visualizerMode == MusicVisualizerMode.FullScreenArt ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ParticlesModeVisibility =>
        _visualizerMode == MusicVisualizerMode.Particles ? Visibility.Visible : Visibility.Collapsed;

    public Visibility GlowingBeatsModeVisibility =>
        _visualizerMode == MusicVisualizerMode.GlowingBeats ? Visibility.Visible : Visibility.Collapsed;

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

    public double ArtScale
    {
        get => _artScale;
        set { if (Math.Abs(_artScale - value) > 0.001) { _artScale = value; OnPropertyChanged(); } }
    }

    public double ArtParallaxX
    {
        get => _artParallaxX;
        set { if (Math.Abs(_artParallaxX - value) > 0.05) { _artParallaxX = value; OnPropertyChanged(); } }
    }

    public double ArtParallaxY
    {
        get => _artParallaxY;
        set { if (Math.Abs(_artParallaxY - value) > 0.05) { _artParallaxY = value; OnPropertyChanged(); } }
    }

    public double BeatRingScale
    {
        get => _beatRingScale;
        set { if (Math.Abs(_beatRingScale - value) > 0.001) { _beatRingScale = value; OnPropertyChanged(); } }
    }

    public bool IsVerticalLayout
    {
        get => _isVerticalLayout;
        set
        {
            if (_isVerticalLayout != value)
            {
                _isVerticalLayout = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LayoutOrientation));
                OnPropertyChanged(nameof(LayoutLabel));
            }
        }
    }

    public Orientation LayoutOrientation => _isVerticalLayout ? Orientation.Vertical : Orientation.Horizontal;
    public string LayoutLabel => _isVerticalLayout ? "Vertical" : "Widescreen";

    public bool SyncWallpaperToArt
    {
        get => _syncWallpaperToArt;
        set
        {
            if (_syncWallpaperToArt != value)
            {
                _syncWallpaperToArt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WallpaperSyncLabel));
                if (value && _currentTrack != null)
                {
                    MusicArtWallpaperSyncRequested?.Invoke(this, _currentTrack);
                }
            }
        }
    }

    public string WallpaperSyncLabel => _syncWallpaperToArt ? "Art Sync: ON" : "Art Sync: OFF";

    public ICommand PlayPauseCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand CycleModeCommand { get; }
    public ICommand SelectVinylModeCommand { get; }
    public ICommand SelectFullArtModeCommand { get; }
    public ICommand SelectParticlesModeCommand { get; }
    public ICommand SelectBeatsModeCommand { get; }
    public ICommand ToggleLayoutCommand { get; }
    public ICommand ToggleWallpaperSyncCommand { get; }
    public ICommand DismissCommand { get; }

    public MusicPlayerViewModel(IMediaSessionService? mediaService = null)
    {
        _mediaService = mediaService;

        InitializeVisualizers();

        PlayPauseCommand = new RelayCommand(async () => await PlayPauseAsync());
        NextCommand = new RelayCommand(async () => await NextAsync());
        PreviousCommand = new RelayCommand(async () => await PreviousAsync());
        CycleModeCommand = new RelayCommand(CycleVisualizerMode);
        SelectVinylModeCommand = new RelayCommand(() => VisualizerMode = MusicVisualizerMode.Vinyl);
        SelectFullArtModeCommand = new RelayCommand(() => VisualizerMode = MusicVisualizerMode.FullScreenArt);
        SelectParticlesModeCommand = new RelayCommand(() => VisualizerMode = MusicVisualizerMode.Particles);
        SelectBeatsModeCommand = new RelayCommand(() => VisualizerMode = MusicVisualizerMode.GlowingBeats);
        ToggleLayoutCommand = new RelayCommand(() => IsVerticalLayout = !IsVerticalLayout);
        ToggleWallpaperSyncCommand = new RelayCommand(() => SyncWallpaperToArt = !SyncWallpaperToArt);
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

    private void InitializeVisualizers()
    {
        string[] palette = ["#FF00F0FF", "#FFA855F7", "#FF00FF9D", "#FFF59E0B"];
        for (int i = 0; i < 18; i++)
        {
            double angle = i * (Math.PI * 2.0 / 18.0);
            ParticleNodes.Add(new VisualizerParticle
            {
                Index = i,
                Size = 6 + (i % 4) * 4,
                ColorHex = palette[i % palette.Length],
                X = 145 + Math.Cos(angle) * 90,
                Y = 145 + Math.Sin(angle) * 90,
                Opacity = 0.45 + (i % 3) * 0.2
            });
        }

        for (int i = 0; i < 16; i++)
        {
            BeatBars.Add(new VisualizerBeatBar
            {
                Index = i,
                ColorHex = i % 2 == 0 ? "#FF00F0FF" : "#FFA855F7",
                HeightPx = 28 + (i % 5) * 14,
                Opacity = 0.85
            });
        }
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

        if (_syncWallpaperToArt)
        {
            MusicArtWallpaperSyncRequested?.Invoke(this, track);
        }
    }

    public void OnHidden() => IsHidden = true;
    public void OnShown() => IsHidden = false;

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

    /// <summary>Advances one frame of all visualizer animations (exposed for deterministic unit tests).</summary>
    public void StepVisualizerFrame(double dtSeconds = 0.033)
    {
        if (_disposed || _isHidden || !_isPlaying) return;

        _phase += dtSeconds * 3.0;
        VinylAngle = (VinylAngle + 2.5) % 360.0;
        GlowPulse = 0.5 + 0.5 * Math.Sin(_phase * 1.4);
        ArtScale = 1.0 + 0.035 * Math.Sin(_phase * 1.1);
        ArtParallaxX = Math.Sin(_phase * 0.7) * 8.0;
        ArtParallaxY = Math.Cos(_phase * 0.5) * 6.0;
        BeatRingScale = 0.94 + 0.12 * Math.Abs(Math.Sin(_phase * 2.2));

        for (int i = 0; i < ParticleNodes.Count; i++)
        {
            var p = ParticleNodes[i];
            double orbitAngle = _phase * (0.35 + (i % 3) * 0.12) + i * (Math.PI * 2.0 / ParticleNodes.Count);
            double radius = 60 + 68 * (0.5 + 0.5 * Math.Sin(_phase * 0.8 + i));
            p.X = 145 + Math.Cos(orbitAngle) * radius;
            p.Y = 145 + Math.Sin(orbitAngle) * radius;
            p.Opacity = 0.35 + 0.65 * Math.Abs(Math.Sin(_phase + i * 0.4));
        }

        for (int i = 0; i < BeatBars.Count; i++)
        {
            var b = BeatBars[i];
            double wave = Math.Abs(Math.Sin(_phase * 2.4 + i * 0.45)) * 0.7 + Math.Abs(Math.Cos(_phase * 1.5 - i * 0.3)) * 0.3;
            b.HeightPx = 18.0 + wave * 130.0;
            b.Opacity = 0.45 + wave * 0.55;
        }
    }

    private async Task RunVisualizerLoopAsync(CancellationToken ct)
    {
        try
        {
            while (await _visualizerTimer.WaitForNextTickAsync(ct))
            {
                if (_disposed) break;

                // Respect Pause-on-Hide GPU Law: Only animate if playing AND visible!
                if (IsPlaying && !IsHidden)
                {
                    StepVisualizerFrame(0.033);

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
