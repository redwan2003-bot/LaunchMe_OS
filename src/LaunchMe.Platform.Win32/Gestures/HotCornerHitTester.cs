using LaunchMe.Contracts.Triggers;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Gestures;

public static class HotCornerHitTester
{
    public static HotCornerLocation? EvaluateCorner(int x, int y, Win32Interop.RECT bounds, int thresholdPx)
    {
        if (thresholdPx <= 0)
        {
            return null;
        }

        bool nearLeft = x >= bounds.left && x < bounds.left + thresholdPx;
        bool nearRight = x >= bounds.right - thresholdPx && x <= bounds.right;
        bool nearTop = y >= bounds.top && y < bounds.top + thresholdPx;
        bool nearBottom = y >= bounds.bottom - thresholdPx && y <= bounds.bottom;

        if (nearTop && nearLeft)
        {
            return HotCornerLocation.TopLeft;
        }

        if (nearTop && nearRight)
        {
            return HotCornerLocation.TopRight;
        }

        if (nearBottom && nearLeft)
        {
            return HotCornerLocation.BottomLeft;
        }

        if (nearBottom && nearRight)
        {
            return HotCornerLocation.BottomRight;
        }

        return null;
    }

    public static HotCornerLocation? FindCornerAcrossMonitors(int x, int y, IEnumerable<Win32Interop.RECT> monitors, int thresholdPx)
    {
        foreach (var monitor in monitors)
        {
            if (x >= monitor.left && x <= monitor.right && y >= monitor.top && y <= monitor.bottom)
            {
                var corner = EvaluateCorner(x, y, monitor, thresholdPx);
                if (corner.HasValue)
                {
                    return corner.Value;
                }
            }
        }

        return null;
    }
}
