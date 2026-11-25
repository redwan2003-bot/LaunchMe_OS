namespace LaunchMe.Contracts.Spaces;

/// <summary>
/// Evaluates Space schedules (day of week and time window) and switches active Spaces automatically.
/// </summary>
public interface ISpaceSchedulerService : IDisposable
{
    bool IsRunning { get; }
    void Start();
    void Stop();
    Task<SpaceModel?> EvaluateScheduleAsync(DateTimeOffset currentTime, CancellationToken ct = default);

    event EventHandler<SpaceChangedEventArgs>? ScheduledSpaceActivated;
}
