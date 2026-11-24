using LaunchMe.Contracts.Triggers;

namespace LaunchMe.Platform.Win32.Gestures;

public class HotCornerDwellTracker
{
    private HotCornerLocation? _activeCorner;
    private DateTime _dwellStartTime;
    private bool _hasTriggeredForCurrentDwell;
    private readonly int _dwellThresholdMs;

    public HotCornerDwellTracker(int dwellThresholdMs = 150)
    {
        _dwellThresholdMs = dwellThresholdMs;
    }

    public HotCornerLocation? ActiveCorner => _activeCorner;
    public bool HasTriggered => _hasTriggeredForCurrentDwell;

    public bool Update(HotCornerLocation? detectedCorner, DateTime currentTime, out HotCornerLocation triggeredCorner)
    {
        triggeredCorner = default;

        if (!detectedCorner.HasValue)
        {
            _activeCorner = null;
            _hasTriggeredForCurrentDwell = false;
            return false;
        }

        if (_activeCorner != detectedCorner)
        {
            _activeCorner = detectedCorner;
            _dwellStartTime = currentTime;
            _hasTriggeredForCurrentDwell = false;
            return false;
        }

        if (!_hasTriggeredForCurrentDwell)
        {
            double elapsedMs = (currentTime - _dwellStartTime).TotalMilliseconds;
            if (elapsedMs >= _dwellThresholdMs)
            {
                _hasTriggeredForCurrentDwell = true;
                triggeredCorner = _activeCorner.Value;
                return true;
            }
        }

        return false;
    }

    public void Reset()
    {
        _activeCorner = null;
        _hasTriggeredForCurrentDwell = false;
    }
}
