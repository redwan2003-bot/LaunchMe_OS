namespace LaunchMe.Core.Widgets.Pomodoro;

public interface IPomodoroStateMachine
{
    PomodoroState CurrentState { get; }
    PomodoroState PreviousState { get; }
    TimeSpan RemainingTime { get; }
    TimeSpan TotalDuration { get; }
    double Progress { get; }
    int CompletedWorkSessions { get; }
    PomodoroConfig Config { get; set; }

    void Start();
    void Pause();
    void Resume();
    void Reset(bool resetCompletedSessions = false);
    void Skip();
    void Tick(TimeSpan delta);

    event EventHandler<PomodoroTickEventArgs>? Ticked;
    event EventHandler<PomodoroStateChangedEventArgs>? StateChanged;
    event EventHandler<AudioCueEventArgs>? AudioCueTriggered;
}

public sealed class PomodoroStateMachine : IPomodoroStateMachine
{
    private PomodoroConfig _config;
    private PomodoroState _currentState = PomodoroState.Idle;
    private PomodoroState _previousState = PomodoroState.Idle;
    private TimeSpan _remainingTime = TimeSpan.Zero;
    private TimeSpan _totalDuration = TimeSpan.Zero;
    private int _completedWorkSessions;
    private bool _halfwayTriggered;
    private bool _finalCountdownTriggered;

    public PomodoroStateMachine(PomodoroConfig? config = null)
    {
        _config = config ?? new PomodoroConfig();
    }

    public PomodoroState CurrentState => _currentState;
    public PomodoroState PreviousState => _previousState;
    public TimeSpan RemainingTime => _remainingTime;
    public TimeSpan TotalDuration => _totalDuration;
    public int CompletedWorkSessions => _completedWorkSessions;

    public double Progress
    {
        get
        {
            if (_totalDuration <= TimeSpan.Zero) return 0.0;
            var elapsed = _totalDuration - _remainingTime;
            return Math.Clamp(elapsed.TotalMilliseconds / _totalDuration.TotalMilliseconds, 0.0, 1.0);
        }
    }

    public PomodoroConfig Config
    {
        get => _config;
        set => _config = value ?? throw new ArgumentNullException(nameof(value));
    }

    public event EventHandler<PomodoroTickEventArgs>? Ticked;
    public event EventHandler<PomodoroStateChangedEventArgs>? StateChanged;
    public event EventHandler<AudioCueEventArgs>? AudioCueTriggered;

    public void Start()
    {
        if (_currentState == PomodoroState.Paused)
        {
            Resume();
            return;
        }

        TransitionTo(PomodoroState.Work, _config.WorkDuration);
        TriggerCue(AudioCueType.WorkStarted, "Focus session started.");
    }

    public void Pause()
    {
        if (_currentState is PomodoroState.Idle or PomodoroState.Paused)
        {
            return;
        }

        _previousState = _currentState;
        _currentState = PomodoroState.Paused;
        FireStateChanged(_previousState, _currentState);
    }

    public void Resume()
    {
        if (_currentState != PomodoroState.Paused)
        {
            return;
        }

        var resumeState = _previousState == PomodoroState.Idle ? PomodoroState.Work : _previousState;
        _currentState = resumeState;
        FireStateChanged(PomodoroState.Paused, _currentState);
    }

    public void Reset(bool resetCompletedSessions = false)
    {
        var old = _currentState;
        _currentState = PomodoroState.Idle;
        _previousState = PomodoroState.Idle;
        _remainingTime = TimeSpan.Zero;
        _totalDuration = TimeSpan.Zero;
        _halfwayTriggered = false;
        _finalCountdownTriggered = false;

        if (resetCompletedSessions)
        {
            _completedWorkSessions = 0;
        }

        FireStateChanged(old, _currentState);
    }

    public void Skip()
    {
        if (_currentState == PomodoroState.Idle)
        {
            Start();
            return;
        }

        if (_currentState == PomodoroState.Work || (_currentState == PomodoroState.Paused && _previousState == PomodoroState.Work))
        {
            CompleteWorkSession();
        }
        else if (_currentState is PomodoroState.ShortBreak or PomodoroState.LongBreak ||
                 (_currentState == PomodoroState.Paused && _previousState is PomodoroState.ShortBreak or PomodoroState.LongBreak))
        {
            CompleteBreakSession();
        }
    }

    public void Tick(TimeSpan delta)
    {
        if (_currentState is PomodoroState.Idle or PomodoroState.Paused || delta <= TimeSpan.Zero)
        {
            return;
        }

        _remainingTime -= delta;

        // Halfway cue
        if (!_halfwayTriggered && _totalDuration > TimeSpan.Zero && _remainingTime <= _totalDuration / 2)
        {
            _halfwayTriggered = true;
            TriggerCue(AudioCueType.HalfwayProgress, "Halfway through the session.");
        }

        // Final 5-second countdown warning cue
        if (!_finalCountdownTriggered && _remainingTime <= TimeSpan.FromSeconds(5) && _remainingTime > TimeSpan.Zero)
        {
            _finalCountdownTriggered = true;
            TriggerCue(AudioCueType.FinalCountdownTick, "5 seconds remaining.");
        }

        if (_remainingTime <= TimeSpan.Zero)
        {
            _remainingTime = TimeSpan.Zero;

            if (_currentState == PomodoroState.Work)
            {
                CompleteWorkSession();
            }
            else if (_currentState is PomodoroState.ShortBreak or PomodoroState.LongBreak)
            {
                CompleteBreakSession();
            }
        }
        else
        {
            FireTicked();
        }
    }

    private void CompleteWorkSession()
    {
        _completedWorkSessions++;
        TriggerCue(AudioCueType.WorkCompleted, "Focus session complete. Well done!");

        var isLongBreak = _config.SessionsUntilLongBreak > 0 &&
                          (_completedWorkSessions % _config.SessionsUntilLongBreak == 0);

        var nextState = isLongBreak ? PomodoroState.LongBreak : PomodoroState.ShortBreak;
        var nextDuration = isLongBreak ? _config.LongBreakDuration : _config.ShortBreakDuration;

        if (_config.AutoStartBreaks)
        {
            TransitionTo(nextState, nextDuration);
            TriggerCue(AudioCueType.BreakStarted, isLongBreak ? "Long break started." : "Short break started.");
        }
        else
        {
            TransitionTo(PomodoroState.Paused, nextDuration);
            _previousState = nextState;
        }
    }

    private void CompleteBreakSession()
    {
        TriggerCue(AudioCueType.BreakCompleted, "Break is over. Ready to focus again?");

        if (_config.AutoStartWork)
        {
            TransitionTo(PomodoroState.Work, _config.WorkDuration);
            TriggerCue(AudioCueType.WorkStarted, "Focus session started.");
        }
        else
        {
            TransitionTo(PomodoroState.Idle, TimeSpan.Zero);
        }
    }

    private void TransitionTo(PomodoroState newState, TimeSpan duration)
    {
        var oldState = _currentState;
        _previousState = oldState;
        _currentState = newState;
        _totalDuration = duration;
        _remainingTime = duration;
        _halfwayTriggered = false;
        _finalCountdownTriggered = false;

        FireStateChanged(oldState, newState);
        FireTicked();
    }

    private void TriggerCue(AudioCueType cueType, string description)
    {
        AudioCueTriggered?.Invoke(this, new AudioCueEventArgs
        {
            CueType = cueType,
            Description = description
        });
    }

    private void FireStateChanged(PomodoroState oldState, PomodoroState newState)
    {
        StateChanged?.Invoke(this, new PomodoroStateChangedEventArgs
        {
            OldState = oldState,
            NewState = newState,
            CompletedSessions = _completedWorkSessions
        });
    }

    private void FireTicked()
    {
        Ticked?.Invoke(this, new PomodoroTickEventArgs
        {
            State = _currentState,
            RemainingTime = _remainingTime,
            TotalDuration = _totalDuration,
            ProgressPercent = Math.Round(Progress * 100.0, 1),
            CompletedSessions = _completedWorkSessions
        });
    }
}
