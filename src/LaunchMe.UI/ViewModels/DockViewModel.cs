using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using LaunchMe.Contracts.Dock;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Platform.Win32.Metrics;

namespace LaunchMe.UI.ViewModels;

public sealed record DockToolBarAction(string Id, string Label, string Glyph, string ActionKey);

public sealed class DockViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IDockShellService? _dockShellService;
    private readonly IDockSpaceService? _dockSpaceService;
    private readonly ISystemMetricsService? _metricsService;
    private readonly IAmbientSoundService? _ambientSoundService;
    private readonly IAppLauncherService? _appLauncherService;

    private bool _isFloating;
    private int _dockHeight = 64;
    private string _currentDisplayId = "PrimaryDisplay";
    private string _activeDockSpaceName = "Default Dock";
    private string _cpuUsage = "CPU: 0%";
    private string _ramUsage = "RAM: 0%";
    private string _netUsage = "NET: 0 KB/s";
    private bool _isAmbientPlaying;
    private double _ambientVolume = 0.7;
    private AmbientSoundPreset _currentAmbientPreset = AmbientSoundPreset.None;
    private bool _disposed;

    private readonly PeriodicTimer? _metricsTimer;
    private readonly CancellationTokenSource _cts = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsFloating
    {
        get => _isFloating;
        set { if (_isFloating != value) { _isFloating = value; OnPropertyChanged(); } }
    }

    public int DockHeight
    {
        get => _dockHeight;
        set { if (_dockHeight != value) { _dockHeight = value; OnPropertyChanged(); } }
    }

    public string CurrentDisplayId
    {
        get => _currentDisplayId;
        set { if (_currentDisplayId != value) { _currentDisplayId = value; OnPropertyChanged(); } }
    }

    public string ActiveDockSpaceName
    {
        get => _activeDockSpaceName;
        set { if (_activeDockSpaceName != value) { _activeDockSpaceName = value; OnPropertyChanged(); } }
    }

    public string CpuUsage
    {
        get => _cpuUsage;
        set { if (_cpuUsage != value) { _cpuUsage = value; OnPropertyChanged(); } }
    }

    public string RamUsage
    {
        get => _ramUsage;
        set { if (_ramUsage != value) { _ramUsage = value; OnPropertyChanged(); } }
    }

    public string NetUsage
    {
        get => _netUsage;
        set { if (_netUsage != value) { _netUsage = value; OnPropertyChanged(); } }
    }

    private bool _workspaceToastVisible;
    public Visibility WorkspaceToastVisibility => _workspaceToastVisible ? Visibility.Visible : Visibility.Collapsed;

    public void SwitchToSpaceIndex(int index)
    {
        if (_dockSpaceService != null)
        {
            var displaySpaces = _dockSpaceService.GetDockSpaces();
            if (displaySpaces.Count > 0)
            {
                int safeIndex = Math.Clamp(index, 0, displaySpaces.Count - 1);
                _dockSpaceService.SetActiveDockSpace(CurrentDisplayId, displaySpaces[safeIndex].Id);
                ActiveDockSpaceName = displaySpaces[safeIndex].Name;
            }
        }
        else
        {
            ActiveDockSpaceName = $"Space {index + 1}";
        }

        _workspaceToastVisible = true;
        OnPropertyChanged(nameof(WorkspaceToastVisibility));
        _ = Task.Delay(1800).ContinueWith(_ =>
        {
            _workspaceToastVisible = false;
            OnPropertyChanged(nameof(WorkspaceToastVisibility));
        }, TaskScheduler.Default);
    }

    public bool IsAmbientPlaying
    {
        get => _isAmbientPlaying;
        set { if (_isAmbientPlaying != value) { _isAmbientPlaying = value; OnPropertyChanged(); } }
    }

    public double AmbientVolume
    {
        get => _ambientVolume;
        set
        {
            if (_ambientVolume != value)
            {
                _ambientVolume = Math.Clamp(value, 0.0, 1.0);
                if (_ambientSoundService != null) _ambientSoundService.Volume = _ambientVolume;
                OnPropertyChanged();
            }
        }
    }

    public AmbientSoundPreset CurrentAmbientPreset
    {
        get => _currentAmbientPreset;
        set { if (_currentAmbientPreset != value) { _currentAmbientPreset = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<DockItem> DockItems { get; } = [];
    public ObservableCollection<DockToolBarAction> ToolBarActions { get; } =
    [
        new("tb-focus", "Focus", "\uE708", "focus"),
        new("tb-shot", "Snap", "\uE722", "screenshot"),
        new("tb-term", "Term", "\uE756", "terminal"),
        new("tb-lock", "Lock", "\uE72E", "lock")
    ];

    public event EventHandler<DockToolBarAction>? ToolBarActionTriggered;

    public ICommand ToggleFloatingCommand { get; }
    public ICommand NextSpaceCommand { get; }
    public ICommand PreviousSpaceCommand { get; }
    public ICommand ToggleAmbientSoundCommand { get; }
    public ICommand CycleAmbientPresetCommand { get; }
    public ICommand LaunchDockItemCommand { get; }
    public ICommand ExecuteToolBarActionCommand { get; }

    public DockViewModel(
        IDockShellService? dockShellService = null,
        IDockSpaceService? dockSpaceService = null,
        ISystemMetricsService? metricsService = null,
        IAmbientSoundService? ambientSoundService = null,
        IAppLauncherService? appLauncherService = null)
    {
        _dockShellService = dockShellService;
        _dockSpaceService = dockSpaceService;
        _metricsService = metricsService;
        _ambientSoundService = ambientSoundService;
        _appLauncherService = appLauncherService;

        ToggleFloatingCommand = new RelayCommand(ToggleFloating);
        NextSpaceCommand = new RelayCommand(NextSpace);
        PreviousSpaceCommand = new RelayCommand(PreviousSpace);
        ToggleAmbientSoundCommand = new RelayCommand(ToggleAmbientSound);
        CycleAmbientPresetCommand = new RelayCommand(CycleAmbientPreset);
        LaunchDockItemCommand = new RelayCommand<DockItem>(LaunchDockItem);
        ExecuteToolBarActionCommand = new RelayCommand<DockToolBarAction>(ExecuteToolBarAction);

        if (_dockSpaceService != null)
        {
            _dockSpaceService.DockSpacesChanged += OnDockSpacesChanged;
            RefreshDockSpace();
        }

        if (_ambientSoundService != null)
        {
            _ambientSoundService.StateChanged += OnAmbientStateChanged;
            _ambientVolume = _ambientSoundService.Volume;
            _isAmbientPlaying = _ambientSoundService.IsPlaying;
            _currentAmbientPreset = _ambientSoundService.CurrentPreset;
        }

        if (_metricsService != null)
        {
            _metricsTimer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            _ = RunMetricsPollingLoopAsync(_cts.Token);
        }
    }

    public void ToggleFloating()
    {
        IsFloating = !IsFloating;
    }

    public void NextSpace()
    {
        _dockSpaceService?.NextDockSpace(CurrentDisplayId);
    }

    public void PreviousSpace()
    {
        _dockSpaceService?.PreviousDockSpace(CurrentDisplayId);
    }

    public void ToggleAmbientSound()
    {
        if (_ambientSoundService == null) return;

        if (_ambientSoundService.IsPlaying)
        {
            _ambientSoundService.Pause();
        }
        else
        {
            var preset = CurrentAmbientPreset == AmbientSoundPreset.None ? AmbientSoundPreset.Rain : CurrentAmbientPreset;
            _ambientSoundService.Play(preset);
        }
    }

    public void CycleAmbientPreset()
    {
        if (_ambientSoundService == null) return;

        var nextPreset = CurrentAmbientPreset switch
        {
            AmbientSoundPreset.None => AmbientSoundPreset.Rain,
            AmbientSoundPreset.Rain => AmbientSoundPreset.OceanWaves,
            AmbientSoundPreset.OceanWaves => AmbientSoundPreset.Forest,
            AmbientSoundPreset.Forest => AmbientSoundPreset.WhiteNoise,
            AmbientSoundPreset.WhiteNoise => AmbientSoundPreset.CafeStream,
            _ => AmbientSoundPreset.Rain
        };

        _ambientSoundService.Play(nextPreset);
    }

    public void LaunchDockItem(DockItem? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.TargetPath)) return;
        _appLauncherService?.LaunchTarget(item.TargetPath);
    }

    public void ExecuteToolBarAction(DockToolBarAction? action)
    {
        if (action == null) return;
        ToolBarActionTriggered?.Invoke(this, action);
    }

    /// <summary>
    /// Computes continuous Gaussian neighbor-wave magnification scales (LaunchMe Floating Capsule Dock)
    /// across all items: 1.42x at cursor center, tapering smoothly across adjacent neighbors.
    /// </summary>
    public static double[] ComputeGaussianWaveScales(
        int itemCount,
        double pointerX,
        double itemSpacing,
        double maxScale = 1.42,
        double sigma = 68.0)
    {
        if (itemCount <= 0) return [];
        var scales = new double[itemCount];
        double boost = Math.Max(0.0, maxScale - 1.0);
        double denom = 2.0 * sigma * sigma;

        for (int i = 0; i < itemCount; i++)
        {
            double centerX = (i * itemSpacing) + (itemSpacing * 0.5);
            double dist = pointerX - centerX;
            scales[i] = 1.0 + boost * Math.Exp(-(dist * dist) / denom);
        }
        return scales;
    }

    private void RefreshDockSpace()
    {
        if (_dockSpaceService == null) return;

        var active = _dockSpaceService.GetActiveDockSpace(CurrentDisplayId);
        if (active != null)
        {
            ActiveDockSpaceName = active.Name;
            DockItems.Clear();
            foreach (var item in active.Items)
            {
                DockItems.Add(item);
            }
        }
    }

    private void OnDockSpacesChanged(object? sender, EventArgs e)
    {
        RefreshDockSpace();
    }

    private void OnAmbientStateChanged(object? sender, EventArgs e)
    {
        if (_ambientSoundService == null) return;
        IsAmbientPlaying = _ambientSoundService.IsPlaying;
        CurrentAmbientPreset = _ambientSoundService.CurrentPreset;
    }

    private async Task RunMetricsPollingLoopAsync(CancellationToken ct)
    {
        if (_metricsTimer == null || _metricsService == null) return;

        try
        {
            while (await _metricsTimer.WaitForNextTickAsync(ct))
            {
                if (_disposed) break;

                var snapshot = _metricsService.GetCurrentSnapshot();
                CpuUsage = $"CPU: {snapshot.Cpu.UsagePercentage:F0}%";
                RamUsage = $"RAM: {snapshot.Memory.MemoryLoadPercentage}%";
                double totalKbps = (snapshot.Network.BytesReceivedPerSecond + snapshot.Network.BytesSentPerSecond) / 1024.0;
                NetUsage = $"NET: {totalKbps:F0} KB/s";
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
        _metricsTimer?.Dispose();
        _cts.Dispose();

        if (_dockSpaceService != null)
        {
            _dockSpaceService.DockSpacesChanged -= OnDockSpacesChanged;
        }

        if (_ambientSoundService != null)
        {
            _ambientSoundService.StateChanged -= OnAmbientStateChanged;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
