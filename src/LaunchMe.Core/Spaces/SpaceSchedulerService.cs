using System.Globalization;
using LaunchMe.Contracts.Spaces;

namespace LaunchMe.Core.Spaces;

/// <summary>
/// Evaluates Space schedules (day of week and time window) on a polling interval and switches active Spaces automatically.
/// </summary>
public sealed class SpaceSchedulerService : ISpaceSchedulerService
{
    private static readonly string[] TimeFormats = ["HH:mm", "H:mm"];

    private readonly ISpaceStore _spaceStore;
    private readonly TimeSpan _pollInterval;
    private readonly Func<DateTimeOffset> _clock;
    private readonly object _gate = new();

    private CancellationTokenSource? _cts;
    private int _disposed;

    public event EventHandler<SpaceChangedEventArgs>? ScheduledSpaceActivated;

    public SpaceSchedulerService(
        ISpaceStore spaceStore,
        TimeSpan? pollInterval = null,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentNullException.ThrowIfNull(spaceStore);

        if (pollInterval.HasValue && pollInterval.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollInterval), pollInterval, "Poll interval must be positive.");
        }

        _spaceStore = spaceStore;
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(30);
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return Volatile.Read(ref _disposed) == 0 && _cts is { IsCancellationRequested: false };
            }
        }
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

            if (_cts is { IsCancellationRequested: false })
            {
                return;
            }

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _ = Task.Run(() => RunPollingLoopAsync(token));
        }
    }

    public void Stop()
    {
        CancellationTokenSource? ctsToCancel;
        lock (_gate)
        {
            ctsToCancel = _cts;
            _cts = null;
        }

        if (ctsToCancel is not null)
        {
            try
            {
                ctsToCancel.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed concurrently.
            }
            finally
            {
                ctsToCancel.Dispose();
            }
        }
    }

    public async Task<SpaceModel?> EvaluateScheduleAsync(DateTimeOffset currentTime, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        ct.ThrowIfCancellationRequested();

        var spaces = await _spaceStore.GetAllSpacesAsync(ct).ConfigureAwait(false);
        if (spaces.Count == 0)
        {
            return null;
        }

        var matchingSpaces = spaces
            .Where(s => IsScheduleActive(s.Schedule, currentTime))
            .ToList();

        if (matchingSpaces.Count == 0)
        {
            return null;
        }

        // Prioritize explicit non-default scheduled spaces over fallback default spaces.
        var target = matchingSpaces.FirstOrDefault(s => !s.IsDefault)
            ?? matchingSpaces.FirstOrDefault();

        if (target is null)
        {
            return null;
        }

        var activeSpace = await _spaceStore.GetActiveSpaceAsync(ct).ConfigureAwait(false);
        if (activeSpace is not null &&
            string.Equals(activeSpace.SpaceId, target.SpaceId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var previousSpaceId = activeSpace?.SpaceId ?? string.Empty;
        await _spaceStore.SwitchSpaceAsync(target.SpaceId, ct).ConfigureAwait(false);
        ScheduledSpaceActivated?.Invoke(this, new SpaceChangedEventArgs(previousSpaceId, target.SpaceId, target));
        return target;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Stop();
    }

    private async Task RunPollingLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_pollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    await EvaluateScheduleAsync(_clock(), ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch
                {
                    // Keep background scheduler resilient across transient store failures.
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on Stop/Dispose.
        }
    }

    private static bool IsScheduleActive(SpaceSchedule? schedule, DateTimeOffset currentTime)
    {
        if (schedule is null || schedule.Days is not { Count: > 0 })
        {
            return false;
        }

        if (!TryParseTime(schedule.Start, out var start) || !TryParseTime(schedule.End, out var end))
        {
            return false;
        }

        if (start == end)
        {
            return false;
        }

        var currentClock = TimeOnly.FromTimeSpan(currentTime.TimeOfDay);

        if (start < end)
        {
            return currentClock >= start &&
                   currentClock < end &&
                   IsDayScheduled(schedule.Days, currentTime.DayOfWeek);
        }

        // Overnight schedule (e.g., 22:00 - 06:00): post-midnight belongs to yesterday's start day.
        if (currentClock >= start)
        {
            return IsDayScheduled(schedule.Days, currentTime.DayOfWeek);
        }

        if (currentClock < end)
        {
            var previousDay = currentTime.AddDays(-1).DayOfWeek;
            return IsDayScheduled(schedule.Days, previousDay);
        }

        return false;
    }

    private static bool TryParseTime(string? value, out TimeOnly time)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            time = default;
            return false;
        }

        return TimeOnly.TryParseExact(
            value.Trim(),
            TimeFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);
    }

    private static bool IsDayScheduled(IEnumerable<string> configuredDays, DayOfWeek targetDay)
    {
        foreach (var entry in configuredDays)
        {
            if (TryParseDayOfWeek(entry, out var parsed) && parsed == targetDay)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseDayOfWeek(string? value, out DayOfWeek day)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            day = default;
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "mon":
            case "monday":
                day = DayOfWeek.Monday;
                return true;
            case "tue":
            case "tuesday":
                day = DayOfWeek.Tuesday;
                return true;
            case "wed":
            case "wednesday":
                day = DayOfWeek.Wednesday;
                return true;
            case "thu":
            case "thursday":
                day = DayOfWeek.Thursday;
                return true;
            case "fri":
            case "friday":
                day = DayOfWeek.Friday;
                return true;
            case "sat":
            case "saturday":
                day = DayOfWeek.Saturday;
                return true;
            case "sun":
            case "sunday":
                day = DayOfWeek.Sunday;
                return true;
            default:
                day = default;
                return false;
        }
    }
}
