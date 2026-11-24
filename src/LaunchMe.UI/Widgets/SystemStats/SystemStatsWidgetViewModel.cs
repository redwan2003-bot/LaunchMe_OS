using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using LaunchMe.UI.Theming;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Models;

namespace LaunchMe.UI.Widgets.SystemStats;

public enum MetricStatusLevel
{
    Normal,
    Warning,
    Critical
}

public sealed class SystemStatsWidgetViewModel : IWidgetViewModel
{
    private static readonly Color GreenColor = Color.FromArgb(255, 16, 185, 129); // #10B981
    private static readonly Color AmberColor = Color.FromArgb(255, 245, 158, 11); // #F59E0B
    private static readonly Color RedColor = Color.FromArgb(255, 239, 68, 68);    // #EF4444

    private readonly ISystemStatsProvider _statsProvider;
    private readonly Timer? _pollTimer;
    private readonly object _lock = new();

    private double _cpuUsagePercent;
    private double _memoryUsagePercent;
    private string _cpuUsageFormatted = "0%";
    private string _memoryUsageFormatted = "0 / 0 GB";
    private string _networkFormatted = "0 KB/s";
    private MetricStatusLevel _cpuStatus = MetricStatusLevel.Normal;
    private MetricStatusLevel _memoryStatus = MetricStatusLevel.Normal;
    private Color _cpuColor = GreenColor;
    private Color _memoryColor = GreenColor;
    private bool _isActive = true;
    private bool _isTimerRunning;
    private bool _disposed;

    public string WidgetId { get; } = "widget-system-stats";
    public string Title => "System Stats";
    public WidgetFamily Family => WidgetFamily.SystemStats;

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
    }

    public double CpuUsagePercent
    {
        get => _cpuUsagePercent;
        set
        {
            if (Math.Abs(_cpuUsagePercent - value) > 0.1)
            {
                _cpuUsagePercent = value;
                OnPropertyChanged();
                CpuUsageFormatted = $"{Math.Round(value)}%";
                UpdateCpuStatus(value);
            }
        }
    }

    public double MemoryUsagePercent
    {
        get => _memoryUsagePercent;
        set
        {
            if (Math.Abs(_memoryUsagePercent - value) > 0.1)
            {
                _memoryUsagePercent = value;
                OnPropertyChanged();
                UpdateMemoryStatus(value);
            }
        }
    }

    public string CpuUsageFormatted
    {
        get => _cpuUsageFormatted;
        private set { if (_cpuUsageFormatted != value) { _cpuUsageFormatted = value; OnPropertyChanged(); } }
    }

    public string MemoryUsageFormatted
    {
        get => _memoryUsageFormatted;
        private set { if (_memoryUsageFormatted != value) { _memoryUsageFormatted = value; OnPropertyChanged(); } }
    }

    public string NetworkFormatted
    {
        get => _networkFormatted;
        private set { if (_networkFormatted != value) { _networkFormatted = value; OnPropertyChanged(); } }
    }

    public MetricStatusLevel CpuStatus
    {
        get => _cpuStatus;
        private set { if (_cpuStatus != value) { _cpuStatus = value; OnPropertyChanged(); } }
    }

    public MetricStatusLevel MemoryStatus
    {
        get => _memoryStatus;
        private set { if (_memoryStatus != value) { _memoryStatus = value; OnPropertyChanged(); } }
    }

    public Color CpuColor
    {
        get => _cpuColor;
        private set
        {
            if (_cpuColor != value)
            {
                _cpuColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CpuBrush));
            }
        }
    }

    public Color MemoryColor
    {
        get => _memoryColor;
        private set
        {
            if (_memoryColor != value)
            {
                _memoryColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MemoryBrush));
            }
        }
    }

    public SolidColorBrush? CpuBrush => ThemeVisualState.TryCreateBrush(CpuColor);
    public SolidColorBrush? MemoryBrush => ThemeVisualState.TryCreateBrush(MemoryColor);

    public ICommand RefreshCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SystemStatsWidgetViewModel(ISystemStatsProvider? statsProvider = null, bool startActive = true)
    {
        _statsProvider = statsProvider ?? new DefaultSystemStatsProvider();
        RefreshCommand = new RelayCommand(() => _ = RefreshAsync());

        _ = RefreshAsync();

        // 2-second polling interval
        _pollTimer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);

        if (startActive)
        {
            StartTimer();
        }
    }

    private void OnTimerTick(object? state)
    {
        if (!_isActive || !_isTimerRunning) return;
        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            var snapshot = await _statsProvider.GetSnapshotAsync();
            UpdateFromSnapshot(snapshot);
        }
        catch
        {
            // Silently keep cached data on error
        }
    }

    public void UpdateFromSnapshot(SystemMetricsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        CpuUsagePercent = snapshot.CpuUsagePercent;
        MemoryUsagePercent = snapshot.MemoryUsagePercent;
        MemoryUsageFormatted = $"{snapshot.UsedMemoryGb:0.0} / {snapshot.TotalMemoryGb:0.0} GB ({Math.Round(snapshot.MemoryUsagePercent)}%)";

        string up = snapshot.NetworkUploadKbps >= 1024
            ? $"{snapshot.NetworkUploadKbps / 1024.0:0.0} MB/s"
            : $"{snapshot.NetworkUploadKbps:0} KB/s";
        string down = snapshot.NetworkDownloadKbps >= 1024
            ? $"{snapshot.NetworkDownloadKbps / 1024.0:0.0} MB/s"
            : $"{snapshot.NetworkDownloadKbps:0} KB/s";
        NetworkFormatted = $"\uE896 {up}   \uE898 {down}";
    }

    private void UpdateCpuStatus(double percent)
    {
        if (percent >= 90.0)
        {
            CpuStatus = MetricStatusLevel.Critical;
            CpuColor = RedColor;
        }
        else if (percent >= 70.0)
        {
            CpuStatus = MetricStatusLevel.Warning;
            CpuColor = AmberColor;
        }
        else
        {
            CpuStatus = MetricStatusLevel.Normal;
            CpuColor = GreenColor;
        }
    }

    private void UpdateMemoryStatus(double percent)
    {
        if (percent >= 90.0)
        {
            MemoryStatus = MetricStatusLevel.Critical;
            MemoryColor = RedColor;
        }
        else if (percent >= 75.0)
        {
            MemoryStatus = MetricStatusLevel.Warning;
            MemoryColor = AmberColor;
        }
        else
        {
            MemoryStatus = MetricStatusLevel.Normal;
            MemoryColor = GreenColor;
        }
    }

    private void StartTimer()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _isTimerRunning = true;
            _pollTimer?.Change(2000, 2000);
        }
    }

    private void StopTimer()
    {
        lock (_lock)
        {
            _isTimerRunning = false;
            _pollTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public void OnLauncherHidden()
    {
        // Pause-on-Hide GPU Law: Suspend system stats polling immediately
        StopTimer();
    }

    public void OnLauncherShown()
    {
        _ = RefreshAsync();
        StartTimer();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopTimer();
        _pollTimer?.Dispose();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
