namespace LaunchMe.Core.Widgets.Pomodoro;

public enum PomodoroState
{
    Idle,
    Work,
    ShortBreak,
    LongBreak,
    Paused
}

public enum AudioCueType
{
    None,
    WorkStarted,
    WorkCompleted,
    BreakStarted,
    BreakCompleted,
    HalfwayProgress,
    FinalCountdownTick
}

public sealed record PomodoroConfig
{
    public TimeSpan WorkDuration { get; init; } = TimeSpan.FromMinutes(25);
    public TimeSpan ShortBreakDuration { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan LongBreakDuration { get; init; } = TimeSpan.FromMinutes(15);
    public int SessionsUntilLongBreak { get; init; } = 4;
    public bool AutoStartBreaks { get; init; } = false;
    public bool AutoStartWork { get; init; } = false;
}

public sealed class PomodoroTickEventArgs : EventArgs
{
    public required PomodoroState State { get; init; }
    public required TimeSpan RemainingTime { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public required double ProgressPercent { get; init; }
    public required int CompletedSessions { get; init; }
}

public sealed class PomodoroStateChangedEventArgs : EventArgs
{
    public required PomodoroState OldState { get; init; }
    public required PomodoroState NewState { get; init; }
    public required int CompletedSessions { get; init; }
}

public sealed class AudioCueEventArgs : EventArgs
{
    public required AudioCueType CueType { get; init; }
    public required string Description { get; init; }
}
