using LaunchMe.Contracts.Triggers;

namespace LaunchMe.Platform.Win32.Gestures;

public sealed record TouchContact(int Id, double X, double Y);

public sealed class TouchpadGestureParser
{
    private readonly double _thresholdFraction;
    private readonly TimeSpan _debounceInterval;
    private DateTime _lastTriggerTime = DateTime.MinValue;

    private int _targetFingerCount = 3;
    private double? _initialAverageDistance;
    private DateTime? _trackingStartTime;

    public int TargetFingerCount
    {
        get => _targetFingerCount;
        set => _targetFingerCount = Math.Clamp(value, 3, 5);
    }

    public event EventHandler<TriggerEventArgs>? GestureTriggered;

    public TouchpadGestureParser(double thresholdFraction = 0.15, int debounceMs = 300)
    {
        _thresholdFraction = thresholdFraction;
        _debounceInterval = TimeSpan.FromMilliseconds(debounceMs);
    }

    public TriggerType? FeedFrame(IReadOnlyList<TouchContact> contacts, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(contacts);

        if (contacts.Count == 0)
        {
            ResetTracking();
            return null;
        }

        if (contacts.Count != _targetFingerCount)
        {
            ResetTracking();
            return null;
        }

        double avgDist = CalculateAverageDistanceToCentroid(contacts);

        if (!_initialAverageDistance.HasValue)
        {
            _initialAverageDistance = avgDist;
            _trackingStartTime = now;
            return null;
        }

        if (_initialAverageDistance.Value <= 0.001)
        {
            _initialAverageDistance = avgDist;
            return null;
        }

        double delta = (avgDist - _initialAverageDistance.Value) / _initialAverageDistance.Value;

        if (now - _lastTriggerTime < _debounceInterval)
        {
            return null;
        }

        TriggerType? triggered = null;
        if (delta <= -_thresholdFraction)
        {
            triggered = TriggerType.TouchpadPinch;
        }
        else if (delta >= _thresholdFraction)
        {
            triggered = TriggerType.TouchpadSpread;
        }

        if (triggered.HasValue)
        {
            _lastTriggerTime = now;
            ResetTracking();
            GestureTriggered?.Invoke(this, new TriggerEventArgs(triggered.Value));
        }

        return triggered;
    }

    public void ResetTracking()
    {
        _initialAverageDistance = null;
        _trackingStartTime = null;
    }

    public static double CalculateAverageDistanceToCentroid(IReadOnlyList<TouchContact> contacts)
    {
        if (contacts.Count == 0) return 0;

        double sumX = 0;
        double sumY = 0;
        for (int i = 0; i < contacts.Count; i++)
        {
            sumX += contacts[i].X;
            sumY += contacts[i].Y;
        }

        double cX = sumX / contacts.Count;
        double cY = sumY / contacts.Count;

        double sumDist = 0;
        for (int i = 0; i < contacts.Count; i++)
        {
            double dx = contacts[i].X - cX;
            double dy = contacts[i].Y - cY;
            sumDist += Math.Sqrt(dx * dx + dy * dy);
        }

        return sumDist / contacts.Count;
    }
}
