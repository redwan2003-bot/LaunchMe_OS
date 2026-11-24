using System.ComponentModel;
using System.Runtime.CompilerServices;
using LaunchMe.Contracts.Spaces;
using LaunchMe.UI.ViewModels;

namespace LaunchMe.UI.Wallpaper;

/// <summary>
/// ViewModel managing wallpaper playback state and enforcing the Pause-on-Hide GPU Law.
/// </summary>
public sealed class LiveWallpaperViewModel : INotifyPropertyChanged, IDisposable
{
    private WallpaperSettings _settings = new();
    private WallpaperPlaybackState _currentState = WallpaperPlaybackState.Stopped;
    private string _assetPath = string.Empty;
    private WallpaperType _type = WallpaperType.Static;
    private double _blurRadius;
    private bool _pauseOnHide = true;
    private double _activeGpuEstimatePercent;
    private bool _wasPlayingBeforeHide;
    private LauncherViewModel? _attachedLauncher;
    private bool _disposed;

    public event EventHandler<WallpaperPlaybackState>? PlaybackStateChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public WallpaperSettings Settings
    {
        get => _settings;
        private set
        {
            _settings = value;
            OnPropertyChanged();
        }
    }

    public WallpaperPlaybackState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(IsPaused));
                PlaybackStateChanged?.Invoke(this, _currentState);
            }
        }
    }

    public bool IsPlaying => CurrentState == WallpaperPlaybackState.Playing;
    public bool IsPaused => CurrentState == WallpaperPlaybackState.Paused;

    public string AssetPath
    {
        get => _assetPath;
        private set { if (_assetPath != value) { _assetPath = value; OnPropertyChanged(); } }
    }

    public WallpaperType Type
    {
        get => _type;
        private set { if (_type != value) { _type = value; OnPropertyChanged(); } }
    }

    public double BlurRadius
    {
        get => _blurRadius;
        private set { if (Math.Abs(_blurRadius - value) > 0.001) { _blurRadius = value; OnPropertyChanged(); } }
    }

    public bool PauseOnHide
    {
        get => _pauseOnHide;
        set { if (_pauseOnHide != value) { _pauseOnHide = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Estimated GPU load. Must strictly reach 0.0% when hidden/paused.
    /// </summary>
    public double ActiveGpuEstimatePercent
    {
        get => _activeGpuEstimatePercent;
        private set { if (Math.Abs(_activeGpuEstimatePercent - value) > 0.001) { _activeGpuEstimatePercent = value; OnPropertyChanged(); } }
    }

    public LiveWallpaperViewModel(WallpaperSettings? initialSettings = null)
    {
        if (initialSettings != null)
        {
            ApplySettings(initialSettings);
        }
    }

    public void AttachToLauncher(LauncherViewModel launcher)
    {
        ArgumentNullException.ThrowIfNull(launcher);
        DetachFromLauncher();

        _attachedLauncher = launcher;
        _attachedLauncher.LauncherHidden += OnLauncherHiddenHandler;
        _attachedLauncher.LauncherShown += OnLauncherShownHandler;
    }

    public void DetachFromLauncher()
    {
        if (_attachedLauncher != null)
        {
            _attachedLauncher.LauncherHidden -= OnLauncherHiddenHandler;
            _attachedLauncher.LauncherShown -= OnLauncherShownHandler;
            _attachedLauncher = null;
        }
    }

    private void OnLauncherHiddenHandler(object? sender, EventArgs e) => OnLauncherHidden();
    private void OnLauncherShownHandler(object? sender, EventArgs e) => OnLauncherShown();

    public void ApplySettings(WallpaperSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Settings = settings;
        Type = settings.Type;
        AssetPath = settings.Asset ?? string.Empty;
        BlurRadius = Math.Clamp(settings.Blur * 40.0, 0.0, 50.0);
        PauseOnHide = settings.PauseOnHide;

        if (Type == WallpaperType.LiveVideo || Type == WallpaperType.Dynamic)
        {
            Play();
        }
        else
        {
            Stop();
        }
    }

    public void Play()
    {
        CurrentState = WallpaperPlaybackState.Playing;
        _wasPlayingBeforeHide = false;
        ActiveGpuEstimatePercent = (Type == WallpaperType.LiveVideo) ? 1.5 : (Type == WallpaperType.Dynamic ? 0.8 : 0.0);
    }

    public void Pause()
    {
        CurrentState = WallpaperPlaybackState.Paused;
        ActiveGpuEstimatePercent = 0.0; // GPU decoding / shader halts immediately
    }

    public void Stop()
    {
        CurrentState = WallpaperPlaybackState.Stopped;
        _wasPlayingBeforeHide = false;
        ActiveGpuEstimatePercent = 0.0;
    }

    /// <summary>
    /// Enforces Pause-on-Hide GPU Law: Immediately pauses video decoding and shader timers.
    /// </summary>
    public void OnLauncherHidden()
    {
        if (PauseOnHide)
        {
            if (IsPlaying)
            {
                _wasPlayingBeforeHide = true;
                Pause();
            }
        }
    }

    /// <summary>
    /// Resumes playback when the launcher becomes visible again.
    /// </summary>
    public void OnLauncherShown()
    {
        if (PauseOnHide && _wasPlayingBeforeHide)
        {
            _wasPlayingBeforeHide = false;
            Play();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DetachFromLauncher();
        Stop();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
