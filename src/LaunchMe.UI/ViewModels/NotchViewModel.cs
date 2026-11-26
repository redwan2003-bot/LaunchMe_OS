using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Agent;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Contracts.Media;
using LaunchMe.Contracts.Notch;
using LaunchMe.UI.Agent;
using Microsoft.UI.Xaml;

namespace LaunchMe.UI.ViewModels;

public enum NotchDrawerTab
{
    Pocket,
    Clipboard,
    Settings
}

public sealed class NotchDrawerWidget : INotifyPropertyChanged
{
    private string _size = "Compact";
    public required string Id { get; init; }
    public required string Title { get; init; }

    public string Size
    {
        get => _size;
        set { if (_size != value) { _size = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Size))); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class NotchViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly INotchWindowService? _notchWindowService;
    private readonly IPocketService? _pocketService;
    private readonly IMediaSessionService? _mediaService;
    private readonly IClipboardMonitor? _clipboardMonitor;
    private readonly IClipboardWriter? _clipboardWriter;

    private bool _isExpanded;
    private double _compactWidthPx = 380;
    private double _compactHeightPx = 36;
    private double _expandedWidthPx = 560;
    private double _expandedHeightPx = 360;
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
    private bool _isFileDragOver;
    private string _bluetoothToastText = string.Empty;
    private int _bluetoothToastTicksRemaining;
    private NotchDrawerTab _activeTab = NotchDrawerTab.Pocket;
    private ClipboardCategory? _selectedClipboardCategory;
    private bool _disposed;

    private readonly List<ClipboardEntry> _allClipboardHistory = [];
    private readonly PeriodicTimer _timerTick;
    private readonly CancellationTokenSource _cts = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? DismissRequested;
    public event EventHandler<IReadOnlyList<string>>? NearbyShareRequested;
    public event EventHandler<bool>? FocusModeChanged;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                SyncActiveDimensions();
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExpandedDrawerVisibility));
            }
        }
    }

    public Visibility ExpandedDrawerVisibility =>
        _isExpanded ? Visibility.Visible : Visibility.Collapsed;

    public double CompactWidthPx
    {
        get => _compactWidthPx;
        set
        {
            double clamped = Math.Clamp(value, 280, 540);
            if (Math.Abs(_compactWidthPx - clamped) > 0.1)
            {
                _compactWidthPx = clamped;
                OnPropertyChanged();
                if (!_isExpanded) SyncActiveDimensions();
            }
        }
    }

    public double CompactHeightPx
    {
        get => _compactHeightPx;
        set
        {
            double clamped = Math.Clamp(value, 28, 56);
            if (Math.Abs(_compactHeightPx - clamped) > 0.1)
            {
                _compactHeightPx = clamped;
                OnPropertyChanged();
                if (!_isExpanded) SyncActiveDimensions();
            }
        }
    }

    public double ExpandedWidthPx
    {
        get => _expandedWidthPx;
        set
        {
            double clamped = Math.Clamp(value, 440, 760);
            if (Math.Abs(_expandedWidthPx - clamped) > 0.1)
            {
                _expandedWidthPx = clamped;
                OnPropertyChanged();
                if (_isExpanded) SyncActiveDimensions();
            }
        }
    }

    public double ExpandedHeightPx
    {
        get => _expandedHeightPx;
        set
        {
            double clamped = Math.Clamp(value, 280, 540);
            if (Math.Abs(_expandedHeightPx - clamped) > 0.1)
            {
                _expandedHeightPx = clamped;
                OnPropertyChanged();
                if (_isExpanded) SyncActiveDimensions();
            }
        }
    }

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
        set
        {
            if (_focusModeActive != value)
            {
                _focusModeActive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FocusModeVisibility));
                OnPropertyChanged(nameof(FocusModeBadgeText));
                FocusModeChanged?.Invoke(this, value);
            }
        }
    }

    public Visibility FocusModeVisibility =>
        _focusModeActive ? Visibility.Visible : Visibility.Collapsed;

    public string FocusModeBadgeText => _focusModeActive ? "FOCUS" : "OFF";

    public string BluetoothToastText
    {
        get => _bluetoothToastText;
        private set
        {
            if (_bluetoothToastText != value)
            {
                _bluetoothToastText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BluetoothToastVisibility));
            }
        }
    }

    public Visibility BluetoothToastVisibility =>
        !string.IsNullOrEmpty(_bluetoothToastText) ? Visibility.Visible : Visibility.Collapsed;

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

    public bool IsFileDragOver
    {
        get => _isFileDragOver;
        set
        {
            if (_isFileDragOver != value)
            {
                _isFileDragOver = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DropZoneVisibility));
            }
        }
    }

    public Visibility DropZoneVisibility =>
        _isFileDragOver ? Visibility.Visible : Visibility.Collapsed;

    public NotchDrawerTab ActiveTab
    {
        get => _activeTab;
        set
        {
            if (_activeTab != value)
            {
                _activeTab = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PocketTabVisibility));
                OnPropertyChanged(nameof(ClipboardTabVisibility));
                OnPropertyChanged(nameof(SettingsTabVisibility));
            }
        }
    }

    public Visibility PocketTabVisibility =>
        _activeTab == NotchDrawerTab.Pocket ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ClipboardTabVisibility =>
        _activeTab == NotchDrawerTab.Clipboard ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SettingsTabVisibility =>
        _activeTab == NotchDrawerTab.Settings ? Visibility.Visible : Visibility.Collapsed;

    public ClipboardCategory? SelectedClipboardCategory
    {
        get => _selectedClipboardCategory;
        set
        {
            if (_selectedClipboardCategory != value)
            {
                _selectedClipboardCategory = value;
                OnPropertyChanged();
                RebuildFilteredClipboardCards();
            }
        }
    }

    public ObservableCollection<PocketItem> PocketItems { get; } = [];
    public ObservableCollection<ClipboardEntry> RecentClipboardCards { get; } = [];
    public ObservableCollection<ClipboardEntry> FilteredClipboardCards { get; } = [];
    public ObservableCollection<NotchDrawerWidget> DrawerWidgets { get; } =
    [
        new NotchDrawerWidget { Id = "music", Title = "Now Playing", Size = "Compact" },
        new NotchDrawerWidget { Id = "timer", Title = "Focus Timer", Size = "Compact" },
        new NotchDrawerWidget { Id = "weather", Title = "Weather", Size = "Compact" }
    ];

    public BlobboCharacterViewModel Blobbo { get; }
    public ApprovalNotificationViewModel Approvals { get; }

    public ICommand ToggleExpandCommand { get; }
    public ICommand StartTimerCommand { get; }
    public ICommand PauseTimerCommand { get; }
    public ICommand ResetTimerCommand { get; }
    public ICommand ClearPocketCommand { get; }
    public ICommand DismissCommand { get; }
    public ICommand ToggleFocusModeCommand { get; }
    public ICommand SelectPocketTabCommand { get; }
    public ICommand SelectClipboardTabCommand { get; }
    public ICommand SelectSettingsTabCommand { get; }
    public ICommand ResetDimensionsCommand { get; }
    public ICommand ClearClipboardCommand { get; }

    public NotchViewModel(
        INotchWindowService? notchWindowService = null,
        IPocketService? pocketService = null,
        IMediaSessionService? mediaService = null,
        IClipboardMonitor? clipboardMonitor = null,
        IBlobboController? blobboController = null,
        IAgentApprovalService? approvalService = null,
        IDiffParserService? diffParser = null,
        IClipboardWriter? clipboardWriter = null)
    {
        _notchWindowService = notchWindowService;
        _pocketService = pocketService;
        _mediaService = mediaService;
        _clipboardMonitor = clipboardMonitor;
        _clipboardWriter = clipboardWriter;

        Blobbo = new BlobboCharacterViewModel(blobboController ?? new LaunchMe.Core.Agent.BlobboController());
        Approvals = new ApprovalNotificationViewModel(
            approvalService ?? new LaunchMe.Core.Agent.AgentApprovalService(),
            diffParser ?? new LaunchMe.Core.Agent.DiffParserService());

        Approvals.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ApprovalNotificationViewModel.HasPendingApproval) && Approvals.HasPendingApproval)
            {
                Blobbo.SetMood(BlobboMood.Alert);
            }
        };

        ToggleExpandCommand = new RelayCommand(ToggleExpand);
        StartTimerCommand = new RelayCommand(() => StartTimer(25));
        PauseTimerCommand = new RelayCommand(PauseTimer);
        ResetTimerCommand = new RelayCommand(ResetTimer);
        ClearPocketCommand = new RelayCommand(ClearPocket);
        DismissCommand = new RelayCommand(() => DismissRequested?.Invoke(this, EventArgs.Empty));
        ToggleFocusModeCommand = new RelayCommand(ToggleFocusMode);
        SelectPocketTabCommand = new RelayCommand(() => ActiveTab = NotchDrawerTab.Pocket);
        SelectClipboardTabCommand = new RelayCommand(() => ActiveTab = NotchDrawerTab.Clipboard);
        SelectSettingsTabCommand = new RelayCommand(() => ActiveTab = NotchDrawerTab.Settings);
        ResetDimensionsCommand = new RelayCommand(ResetDimensions);
        ClearClipboardCommand = new RelayCommand(ClearClipboardHistory);

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

    private void SyncActiveDimensions()
    {
        WidthPx = _isExpanded ? _expandedWidthPx : _compactWidthPx;
        HeightPx = _isExpanded ? _expandedHeightPx : _compactHeightPx;
    }

    public void ResetDimensions()
    {
        _compactWidthPx = 380;
        _compactHeightPx = 36;
        _expandedWidthPx = 560;
        _expandedHeightPx = 360;
        OnPropertyChanged(nameof(CompactWidthPx));
        OnPropertyChanged(nameof(CompactHeightPx));
        OnPropertyChanged(nameof(ExpandedWidthPx));
        OnPropertyChanged(nameof(ExpandedHeightPx));
        SyncActiveDimensions();
    }

    public void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    public void ToggleFocusMode()
    {
        FocusModeActive = !FocusModeActive;
    }

    public void ShowBluetoothToast(string deviceName, int? batteryPercent = null)
    {
        if (string.IsNullOrWhiteSpace(deviceName)) return;
        BluetoothToastText = batteryPercent.HasValue
            ? $"{deviceName} • {Math.Clamp(batteryPercent.Value, 0, 100)}%"
            : $"{deviceName} Connected";
        _bluetoothToastTicksRemaining = 4;
    }

    public void DismissBluetoothToast()
    {
        _bluetoothToastTicksRemaining = 0;
        BluetoothToastText = string.Empty;
    }

    public void MoveDrawerWidget(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= DrawerWidgets.Count || toIndex < 0 || toIndex >= DrawerWidgets.Count || fromIndex == toIndex)
            return;
        DrawerWidgets.Move(fromIndex, toIndex);
    }

    public void ToggleDrawerWidgetSize(string id)
    {
        var w = DrawerWidgets.FirstOrDefault(x => x.Id == id);
        if (w != null)
        {
            w.Size = w.Size == "Compact" ? "Large" : "Compact";
        }
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

    public void BeginFileDragOver()
    {
        IsFileDragOver = true;
        if (!IsExpanded)
        {
            IsExpanded = true;
        }
    }

    public void EndFileDragOver()
    {
        IsFileDragOver = false;
    }

    public void DropFilesToPocket(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                AddPocketFile(path);
            }
        }
        IsFileDragOver = false;
        ActiveTab = NotchDrawerTab.Pocket;
    }

    public void DropFilesToNearbyShare(IEnumerable<string> filePaths)
    {
        var list = filePaths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        IsFileDragOver = false;
        if (list.Count > 0)
        {
            NearbyShareRequested?.Invoke(this, list);
        }
    }

    public void AddClipboardEntry(ClipboardEntry entry)
    {
        OnClipboardChanged(this, entry);
    }

    public void SelectClipboardCategory(ClipboardCategory? category)
    {
        SelectedClipboardCategory = category;
    }

    public void ToggleFavoriteClipboardEntry(string id)
    {
        var entry = _allClipboardHistory.FirstOrDefault(e => e.Id == id);
        if (entry == null) return;
        entry.IsPinned = !entry.IsPinned;
        RebuildFilteredClipboardCards();
    }

    public void CopyClipboardEntry(ClipboardEntry entry)
    {
        if (entry == null) return;
        if (!string.IsNullOrEmpty(entry.TextContent))
        {
            _clipboardWriter?.SetText(entry.TextContent);
        }
    }

    public void ClearClipboardHistory()
    {
        _allClipboardHistory.Clear();
        RecentClipboardCards.Clear();
        FilteredClipboardCards.Clear();
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
        _allClipboardHistory.Insert(0, entry);
        if (_allClipboardHistory.Count > 24)
        {
            _allClipboardHistory.RemoveAt(_allClipboardHistory.Count - 1);
        }

        RecentClipboardCards.Insert(0, entry);
        if (RecentClipboardCards.Count > 10)
        {
            RecentClipboardCards.RemoveAt(RecentClipboardCards.Count - 1);
        }

        RebuildFilteredClipboardCards();
    }

    private void RebuildFilteredClipboardCards()
    {
        FilteredClipboardCards.Clear();
        IEnumerable<ClipboardEntry> source = _selectedClipboardCategory switch
        {
            null or ClipboardCategory.Recent => _allClipboardHistory,
            ClipboardCategory.Favorites => _allClipboardHistory.Where(e => e.IsPinned || e.Category == ClipboardCategory.Favorites),
            var cat => _allClipboardHistory.Where(e => e.Category == cat)
        };

        foreach (var item in source.Take(12))
        {
            FilteredClipboardCards.Add(item);
        }
    }

    private async Task RunPeriodicLoopAsync(CancellationToken ct)
    {
        try
        {
            while (await _timerTick.WaitForNextTickAsync(ct))
            {
                if (_disposed) break;

                if (IsTimerRunning && _timerRemainingSeconds > 0)
                {
                    _timerRemainingSeconds--;
                    UpdateTimerDisplay();
                    if (_timerRemainingSeconds == 0)
                    {
                        IsTimerRunning = false;
                    }
                }

                // Auto-dismiss Bluetooth toast
                if (_bluetoothToastTicksRemaining > 0)
                {
                    _bluetoothToastTicksRemaining--;
                    if (_bluetoothToastTicksRemaining == 0)
                    {
                        BluetoothToastText = string.Empty;
                    }
                }

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
