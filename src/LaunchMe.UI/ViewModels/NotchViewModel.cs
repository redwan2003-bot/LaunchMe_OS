using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Contracts.Media;
using LaunchMe.Contracts.Notch;

namespace LaunchMe.UI.ViewModels;

public sealed class NotchViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly INotchWindowService? _notchWindowService;
    private readonly IPocketService? _pocketService;
    private readonly IMediaSessionService? _mediaService;
    private readonly IClipboardMonitor? _clipboardMonitor;

    private bool _isExpanded;
    private double _widthPx = 380;
    private double _heightPx = 36;
    private string _weatherSummary = "22°C Clear";
    private string _nowPlayingTitle = "Not Playing";
    private string _nowPlayingArtist = string.Empty;
    private double _nowPlayingProgress;
    private string _batteryPercentage = "100%";
    private bool _focusModeActive;
    private string _timerDisplay = "25:00";
    private bool _isTimerRunning;
    private int _timerRemainingSeconds = 25 * 60;
    private bool _isHiddenByFullscreenGame;
    private bool _disposed;

    private readonly PeriodicTimer _timerTick;
    private readonly CancellationTokenSource _cts = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? DismissRequested;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                WidthPx = value ? 560 : 380;
                HeightPx = value ? 360 : 36;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExpandedDrawerVisibility));
            }
        }
    }

    public Microsoft.UI.Xaml.Visibility ExpandedDrawerVisibility =>
        _isExpanded ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public double WidthPx
    {
        get => _widthPx;
        set { if (_widthPx != value) { _widthPx = value; OnPropertyChanged(); } }
    }

    public double HeightPx
    {
        get => _heightPx;
        set { if (_heightPx != value) { _heightPx = value; OnPropertyChanged(); } }
    }

    public string WeatherSummary
    {
        get => _weatherSummary;
        set { if (_weatherSummary != value) { _weatherSummary = value; OnPropertyChanged(); } }
    }

    public string NowPlayingTitle
    {
        get => _nowPlayingTitle;
        set { if (_nowPlayingTitle != value) { _nowPlayingTitle = value; OnPropertyChanged(); } }
    }

    public string NowPlayingArtist
    {
        get => _nowPlayingArtist;
        set { if (_nowPlayingArtist != value) { _nowPlayingArtist = value; OnPropertyChanged(); } }
    }

    public double NowPlayingProgress
    {
        get => _nowPlayingProgress;
        set { if (_nowPlayingProgress != value) { _nowPlayingProgress = value; OnPropertyChanged(); } }
    }

    public string BatteryPercentage
    {
        get => _batteryPercentage;
        set { if (_batteryPercentage != value) { _batteryPercentage = value; OnPropertyChanged(); } }
    }

    public bool FocusModeActive
    {
        get => _focusModeActive;
        set { if (_focusModeActive != value) { _focusModeActive = value; OnPropertyChanged(); } }
    }

    public string TimerDisplay
    {
        get => _timerDisplay;
        set { if (_timerDisplay != value) { _timerDisplay = value; OnPropertyChanged(); } }
    }

    public bool IsTimerRunning
    {
        get => _isTimerRunning;
        set { if (_isTimerRunning != value) { _isTimerRunning = value; OnPropertyChanged(); } }
    }

    public bool IsHiddenByFullscreenGame
    {
        get => _isHiddenByFullscreenGame;
        set { if (_isHiddenByFullscreenGame != value) { _isHiddenByFullscreenGame = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<PocketItem> PocketItems { get; } = [];
    public ObservableCollection<ClipboardEntry> RecentClipboardCards { get; } = [];

    public ICommand ToggleExpandCommand { get; }
    public ICommand StartTimerCommand { get; }
    public ICommand PauseTimerCommand { get; }
    public ICommand ResetTimerCommand { get; }
    public ICommand ClearPocketCommand { get; }
    public ICommand DismissCommand { get; }

    public NotchViewModel(
        INotchWindowService? notchWindowService = null,
        IPocketService? pocketService = null,
        IMediaSessionService? mediaService = null,
        IClipboardMonitor? clipboardMonitor = null)
    {
        _notchWindowService = notchWindowService;
        _pocketService = pocketService;
        _mediaService = mediaService;
        _clipboardMonitor = clipboardMonitor;

        ToggleExpandCommand = new RelayCommand(ToggleExpand);
        StartTimerCommand = new RelayCommand(() => StartTimer(25));
        PauseTimerCommand = new RelayCommand(PauseTimer);
        ResetTimerCommand = new RelayCommand(ResetTimer);
        ClearPocketCommand = new RelayCommand(ClearPocket);
        DismissCommand = new RelayCommand(() => DismissRequested?.Invoke(this, EventArgs.Empty));

        if (_pocketService != null)
        {
            _pocketService.ItemsChanged += OnPocketItemsChanged;
            RefreshPocketItems();
        }

        if (_mediaService != null)
        {
            _mediaService.TrackChanged += OnMediaTrackChanged;
        }

        if (_clipboardMonitor != null)
        {
            _clipboardMonitor.ItemCaptured += OnClipboardChanged;
        }

        _timerTick = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = RunPeriodicLoopAsync(_cts.Token);
    }

    public void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    public void StartTimer(int minutes = 25)
    {
        _timerRemainingSeconds = Math.Max(1, minutes * 60);
        UpdateTimerDisplay();
        IsTimerRunning = true;
    }

    public void PauseTimer()
    {
        IsTimerRunning = false;
    }

    public void ResetTimer()
    {
        IsTimerRunning = false;
        _timerRemainingSeconds = 25 * 60;
        UpdateTimerDisplay();
    }

    public void AddPocketFile(string filePath)
    {
        if (_pocketService == null || string.IsNullOrWhiteSpace(filePath)) return;
        _pocketService.AddFile(filePath);
    }

    public void RemovePocketFile(string id)
    {
        if (_pocketService == null || string.IsNullOrWhiteSpace(id)) return;
        _pocketService.RemoveFile(id);
    }

    public void ClearPocket()
    {
        _pocketService?.Clear();
    }

    public void CheckFullscreenGame()
    {
        if (_notchWindowService == null) return;
        IsHiddenByFullscreenGame = _notchWindowService.IsFullscreenGameActive();
    }

    public void ApplyScreenCaptureExclusion(nint hwnd)
    {
        _notchWindowService?.ApplyScreenCaptureExclusion(hwnd, true);
    }

    private void UpdateTimerDisplay()
    {
        int minutes = _timerRemainingSeconds / 60;
        int seconds = _timerRemainingSeconds % 60;
        TimerDisplay = $"{minutes:D2}:{seconds:D2}";
    }

    private void RefreshPocketItems()
    {
        if (_pocketService == null) return;
        PocketItems.Clear();
        foreach (var item in _pocketService.GetItems())
        {
            PocketItems.Add(item);
        }
    }

    private void OnPocketItemsChanged(object? sender, EventArgs e)
    {
        RefreshPocketItems();
    }

    private void OnMediaTrackChanged(object? sender, MediaTrackInfo track)
    {
        NowPlayingTitle = string.IsNullOrWhiteSpace(track.Title) ? "Not Playing" : track.Title;
        NowPlayingArtist = track.Artist;
        NowPlayingProgress = track.Duration.TotalSeconds > 0
            ? Math.Clamp(track.Position.TotalSeconds / track.Duration.TotalSeconds, 0.0, 1.0)
            : 0.0;
    }

    private void OnClipboardChanged(object? sender, ClipboardEntry entry)
    {
        RecentClipboardCards.Insert(0, entry);
        if (RecentClipboardCards.Count > 10)
        {
            RecentClipboardCards.RemoveAt(RecentClipboardCards.Count - 1);
        }
    }

    private async Task RunPeriodicLoopAsync(CancellationToken ct)
    {
        try
        {
            while (await _timerTick.WaitForNextTickAsync(ct))
            {
                if (_disposed) break;

                // 1. Tick Countdown
                if (IsTimerRunning && _timerRemainingSeconds > 0)
                {
                    _timerRemainingSeconds--;
                    UpdateTimerDisplay();
                    if (_timerRemainingSeconds == 0)
                    {
                        IsTimerRunning = false;
                    }
                }

                // 2. Periodic Game check
                CheckFullscreenGame();
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
        _timerTick.Dispose();
        _cts.Dispose();

        if (_pocketService != null)
        {
            _pocketService.ItemsChanged -= OnPocketItemsChanged;
        }

        if (_mediaService != null)
        {
            _mediaService.TrackChanged -= OnMediaTrackChanged;
        }

        if (_clipboardMonitor != null)
        {
            _clipboardMonitor.ItemCaptured -= OnClipboardChanged;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
