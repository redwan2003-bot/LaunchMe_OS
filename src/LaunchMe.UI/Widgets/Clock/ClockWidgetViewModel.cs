using System.ComponentModel;
using System.Runtime.CompilerServices;
using LaunchMe.UI.Widgets.Models;

namespace LaunchMe.UI.Widgets.Clock;

public enum ClockStyle
{
    Digital,
    Analog
}

public sealed class ClockWidgetViewModel : IWidgetViewModel
{
    private readonly Timer? _timer;
    private readonly object _lock = new();
    private bool _is24HourFormat = true;
    private bool _showSeconds = true;
    private ClockStyle _style = ClockStyle.Digital;
    private string _formattedTime = string.Empty;
    private string _formattedDate = string.Empty;
    private string _amPm = string.Empty;
    private double _hourHandAngle;
    private double _minuteHandAngle;
    private double _secondHandAngle;
    private bool _isActive = true;
    private bool _isTimerRunning;
    private DateTime? _overrideTime;
    private bool _disposed;

    public string WidgetId { get; } = "widget-clock";
    public string Title => "Clock";
    public WidgetFamily Family => WidgetFamily.Clock;

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
    }

    public ClockStyle Style
    {
        get => _style;
        set { if (_style != value) { _style = value; OnPropertyChanged(); } }
    }

    public bool Is24HourFormat
    {
        get => _is24HourFormat;
        set
        {
            if (_is24HourFormat != value)
            {
                _is24HourFormat = value;
                OnPropertyChanged();
                UpdateDisplay(GetCurrentTime());
            }
        }
    }

    public bool ShowSeconds
    {
        get => _showSeconds;
        set
        {
            if (_showSeconds != value)
            {
                _showSeconds = value;
                OnPropertyChanged();
                UpdateDisplay(GetCurrentTime());
            }
        }
    }

    public string FormattedTime
    {
        get => _formattedTime;
        private set { if (_formattedTime != value) { _formattedTime = value; OnPropertyChanged(); } }
    }

    public string FormattedDate
    {
        get => _formattedDate;
        private set { if (_formattedDate != value) { _formattedDate = value; OnPropertyChanged(); } }
    }

    public string AmPm
    {
        get => _amPm;
        private set { if (_amPm != value) { _amPm = value; OnPropertyChanged(); } }
    }

    public double HourHandAngle
    {
        get => _hourHandAngle;
        private set { if (Math.Abs(_hourHandAngle - value) > 0.01) { _hourHandAngle = value; OnPropertyChanged(); } }
    }

    public double MinuteHandAngle
    {
        get => _minuteHandAngle;
        private set { if (Math.Abs(_minuteHandAngle - value) > 0.01) { _minuteHandAngle = value; OnPropertyChanged(); } }
    }

    public double SecondHandAngle
    {
        get => _secondHandAngle;
        private set { if (Math.Abs(_secondHandAngle - value) > 0.01) { _secondHandAngle = value; OnPropertyChanged(); } }
    }

    public bool IsTimerRunning => _isTimerRunning;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ClockWidgetViewModel(bool startActive = true)
    {
        UpdateDisplay(GetCurrentTime());

        _timer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);

        if (startActive)
        {
            StartTimer();
        }
    }

    public void SetTime(DateTime time)
    {
        lock (_lock)
        {
            _overrideTime = time;
            UpdateDisplay(time);
        }
    }

    public void ClearOverrideTime()
    {
        lock (_lock)
        {
            _overrideTime = null;
            UpdateDisplay(GetCurrentTime());
        }
    }

    private DateTime GetCurrentTime()
    {
        lock (_lock)
        {
            return _overrideTime ?? DateTime.Now;
        }
    }

    private void OnTimerTick(object? state)
    {
        if (!_isActive || !_isTimerRunning) return;
        UpdateDisplay(GetCurrentTime());
    }

    private void UpdateDisplay(DateTime time)
    {
        // 1. Time string
        string format;
        if (_is24HourFormat)
        {
            format = _showSeconds ? "HH:mm:ss" : "HH:mm";
            AmPm = string.Empty;
        }
        else
        {
            format = _showSeconds ? "hh:mm:ss" : "hh:mm";
            AmPm = time.ToString("tt");
        }

        FormattedTime = time.ToString(format);
        FormattedDate = time.ToString("dddd, MMMM d");

        // 2. Analog angles
        double seconds = time.Second + (time.Millisecond / 1000.0);
        double minutes = time.Minute + (seconds / 60.0);
        double hours = (time.Hour % 12) + (minutes / 60.0);

        SecondHandAngle = seconds * 6.0;        // 360 / 60
        MinuteHandAngle = minutes * 6.0;        // 360 / 60
        HourHandAngle = hours * 30.0;           // 360 / 12
    }

    private void StartTimer()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _isTimerRunning = true;
            _timer?.Change(0, 1000);
        }
    }

    private void StopTimer()
    {
        lock (_lock)
        {
            _isTimerRunning = false;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public void OnLauncherHidden()
    {
        // Pause-on-Hide GPU Law: Suspend clock ticker immediately
        StopTimer();
    }

    public void OnLauncherShown()
    {
        UpdateDisplay(GetCurrentTime());
        StartTimer();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopTimer();
        _timer?.Dispose();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
