using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Models;

namespace LaunchMe.UI.Widgets.Pomodoro;

public enum PomodoroState
{
    Work,
    ShortBreak,
    LongBreak
}

public sealed class PomodoroWidgetViewModel : IWidgetViewModel
{
    private const int DefaultWorkSeconds = 25 * 60;
    private const int DefaultShortBreakSeconds = 5 * 60;
    private const int DefaultLongBreakSeconds = 15 * 60;

    private readonly Timer? _tickTimer;
    private readonly object _lock = new();

    private PomodoroState _currentState = PomodoroState.Work;
    private int _remainingSeconds = DefaultWorkSeconds;
    private int _totalSeconds = DefaultWorkSeconds;
    private int _completedCycles;
    private bool _isRunning;
    private bool _isActive = true;
    private bool _isTimerRunning;
    private DateTime? _lastTickUtc;
    private bool _disposed;

    public event EventHandler? AudioCueRequested;
    public event PropertyChangedEventHandler? PropertyChanged;

    public string WidgetId { get; } = "widget-pomodoro";
    public string Title => "Pomodoro";
    public WidgetFamily Family => WidgetFamily.Pomodoro;

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
    }

    public PomodoroState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StateTitle));
                OnPropertyChanged(nameof(StateGlyph));
                OnPropertyChanged(nameof(ProgressRatio));
            }
        }
    }

    public string StateTitle => CurrentState switch
    {
        PomodoroState.Work => "Focus Work",
        PomodoroState.ShortBreak => "Short Break",
        PomodoroState.LongBreak => "Long Break",
        _ => "Pomodoro"
    };

    public string StateGlyph => CurrentState switch
    {
        PomodoroState.Work => "\uEC42",       // Hourglass / work
        PomodoroState.ShortBreak => "\uE7BE", // Coffee / break
        PomodoroState.LongBreak => "\uE734",  // Star / deep break
        _ => "\uEC42"
    };

    public int RemainingSeconds
    {
        get => _remainingSeconds;
        set
        {
            if (_remainingSeconds != value)
            {
                _remainingSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RemainingTimeFormatted));
                OnPropertyChanged(nameof(ProgressRatio));
            }
        }
    }

    public int CompletedCycles
    {
        get => _completedCycles;
        private set { if (_completedCycles != value) { _completedCycles = value; OnPropertyChanged(); } }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StartPauseButtonText));
                OnPropertyChanged(nameof(StartPauseGlyph));
            }
        }
    }

    public string StartPauseButtonText => IsRunning ? "Pause" : "Start";
    public string StartPauseGlyph => IsRunning ? "\uE769" : "\uE768";

    public string RemainingTimeFormatted
    {
        get
        {
            int minutes = RemainingSeconds / 60;
            int seconds = RemainingSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }

    public double ProgressRatio
    {
        get
        {
            if (_totalSeconds <= 0) return 0.0;
            double elapsed = _totalSeconds - RemainingSeconds;
            return Math.Clamp(elapsed / _totalSeconds, 0.0, 1.0);
        }
    }

    public ICommand StartPauseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand SkipCommand { get; }

    public PomodoroWidgetViewModel()
    {
        StartPauseCommand = new RelayCommand(ToggleStartPause);
        ResetCommand = new RelayCommand(Reset);
        SkipCommand = new RelayCommand(TransitionToNextState);

        _tickTimer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void ToggleStartPause()
    {
        if (IsRunning)
        {
            Pause();
        }
        else
        {
            Start();
        }
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_disposed) return;
            IsRunning = true;
            _lastTickUtc = DateTime.UtcNow;
            _isTimerRunning = true;
            _tickTimer?.Change(1000, 1000);
        }
    }

    public void Pause()
    {
        lock (_lock)
        {
            IsRunning = false;
            _isTimerRunning = false;
            _tickTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _lastTickUtc = null;
        }
    }

    public void Reset()
    {
        Pause();
        RemainingSeconds = _totalSeconds;
    }

    public void TransitionToNextState()
    {
        lock (_lock)
        {
            if (CurrentState == PomodoroState.Work)
            {
                CompletedCycles++;
                if (CompletedCycles % 4 == 0)
                {
                    CurrentState = PomodoroState.LongBreak;
                    _totalSeconds = DefaultLongBreakSeconds;
                }
                else
                {
                    CurrentState = PomodoroState.ShortBreak;
                    _totalSeconds = DefaultShortBreakSeconds;
                }
            }
            else
            {
                CurrentState = PomodoroState.Work;
                _totalSeconds = DefaultWorkSeconds;
            }

            RemainingSeconds = _totalSeconds;
            AudioCueRequested?.Invoke(this, EventArgs.Empty);
            Pause();
        }
    }

    private void OnTick(object? state)
    {
        lock (_lock)
        {
            if (!_isActive || !IsRunning || !_isTimerRunning) return;

            DateTime now = DateTime.UtcNow;
            int elapsedSeconds = 1;
            if (_lastTickUtc.HasValue)
            {
                elapsedSeconds = Math.Max(1, (int)(now - _lastTickUtc.Value).TotalSeconds);
            }
            _lastTickUtc = now;

            if (RemainingSeconds > elapsedSeconds)
            {
                RemainingSeconds -= elapsedSeconds;
            }
            else
            {
                RemainingSeconds = 0;
                TransitionToNextState();
            }
        }
    }

    public void OnLauncherHidden()
    {
        // Pause-on-Hide GPU Law: Suspend high-frequency UI tick timer
        lock (_lock)
        {
            _isTimerRunning = false;
            _tickTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public void OnLauncherShown()
    {
        lock (_lock)
        {
            if (IsRunning)
            {
                // Catch up on elapsed time during hidden state
                if (_lastTickUtc.HasValue)
                {
                    int elapsed = (int)(DateTime.UtcNow - _lastTickUtc.Value).TotalSeconds;
                    if (RemainingSeconds > elapsed)
                    {
                        RemainingSeconds -= elapsed;
                    }
                    else
                    {
                        RemainingSeconds = 0;
                        TransitionToNextState();
                    }
                }

                _lastTickUtc = DateTime.UtcNow;
                _isTimerRunning = true;
                _tickTimer?.Change(1000, 1000);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Pause();
        _tickTimer?.Dispose();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
