using System.Runtime.InteropServices;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Shell;

/// <summary>
/// Monitor pixel bounds and work-area bounds for multi-display edge positioning.
/// </summary>
public readonly record struct MonitorBounds(
    int Left,
    int Top,
    int Right,
    int Bottom,
    int WorkLeft,
    int WorkTop,
    int WorkRight,
    int WorkBottom)
{
    public int Width => Math.Max(1, Right - Left);
    public int Height => Math.Max(1, Bottom - Top);
    public int WorkWidth => Math.Max(1, WorkRight - WorkLeft);
    public int WorkHeight => Math.Max(1, WorkBottom - WorkTop);
}

/// <summary>
/// Configures Windows 11 DWM borderless pill edges (rounded corners without the gray 1px HWND outline)
/// and resolves the active monitor's exact bounds under the cursor for multi-display positioning.
/// </summary>
public static class Win32WindowEdgeHelper
{
    /// <summary>
    /// Applies Windows 11 DWMWCP_ROUND corners and DWMWA_COLOR_NONE border suppression
    /// so floating glass pills (Notch, Dock) have clean, unrimmed luxury rounded edges.
    /// </summary>
    public static bool ApplyBorderlessPillDwmChrome(nint hwnd)
    {
        if (hwnd == nint.Zero || !Win32Interop.IsWindow(hwnd))
        {
            return false;
        }

        try
        {
            int cornerPreference = Win32Interop.DWMWCP_ROUND;
            int cornerHr = Win32Interop.DwmSetWindowAttribute(
                hwnd,
                Win32Interop.DWMWA_WINDOW_CORNER_PREFERENCE,
                ref cornerPreference,
                sizeof(int));

            uint borderColor = Win32Interop.DWMWA_COLOR_NONE;
            int borderHr = Win32Interop.DwmSetWindowAttribute(
                hwnd,
                Win32Interop.DWMWA_BORDER_COLOR,
                ref borderColor,
                sizeof(uint));

            return cornerHr >= 0 && borderHr >= 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the full monitor bounds and work-area bounds for the display currently under the cursor,
    /// falling back to primary screen metrics when headless or unavailable.
    /// </summary>
    public static MonitorBounds GetActiveMonitorBounds()
    {
        try
        {
            if (Win32Interop.GetCursorPos(out var pt))
            {
                nint hMon = Win32Interop.MonitorFromPoint(pt, Win32Interop.MONITOR_DEFAULTTONEAREST);
                if (hMon != nint.Zero)
                {
                    var mi = new Win32Interop.MONITORINFO
                    {
                        cbSize = (uint)Marshal.SizeOf<Win32Interop.MONITORINFO>()
                    };

                    if (Win32Interop.GetMonitorInfo(hMon, ref mi))
                    {
                        return new MonitorBounds(
                            mi.rcMonitor.left,
                            mi.rcMonitor.top,
                            mi.rcMonitor.right,
                            mi.rcMonitor.bottom,
                            mi.rcWork.left,
                            mi.rcWork.top,
                            mi.rcWork.right,
                            mi.rcWork.bottom);
                    }
                }
            }
        }
        catch
        {
            // Fall back to primary screen metrics below.
        }

        int screenW = Math.Max(1280, Win32Interop.GetSystemMetrics(0));
        int screenH = Math.Max(720, Win32Interop.GetSystemMetrics(1));
        return new MonitorBounds(0, 0, screenW, screenH, 0, 0, screenW, screenH);
    }
}
