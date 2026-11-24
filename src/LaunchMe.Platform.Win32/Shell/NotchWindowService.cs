using System.Runtime.InteropServices;
using System.Text;
using LaunchMe.Contracts.Notch;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Shell;

/// <summary>
/// Service controlling screen capture exclusion and full-screen game detection for Dynamic Notch.
/// </summary>
public sealed class NotchWindowService : INotchWindowService
{
    private bool _disposed;

    public void ApplyScreenCaptureExclusion(nint hwnd, bool exclude)
    {
        if (hwnd == nint.Zero || _disposed)
        {
            return;
        }

        uint affinity = exclude ? Win32Interop.WDA_EXCLUDEFROMCAPTURE : Win32Interop.WDA_NONE;
        Win32Interop.SetWindowDisplayAffinity(hwnd, affinity);
    }

    public bool IsFullscreenGameActive()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // 1. Check SHQueryUserNotificationState for direct DirectX / Direct3D full-screen detection
        int hr = Win32Interop.SHQueryUserNotificationState(out var state);
        if (hr == 0 && state == Win32Interop.QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D_3D_FULL_SCREEN)
        {
            return true;
        }

        // 2. Inspect the current foreground window
        nint foreground = Win32Interop.GetForegroundWindow();
        if (foreground == nint.Zero)
        {
            return false;
        }

        // Exclude system desktop and shell windows
        if (foreground == Win32Interop.GetDesktopWindow() || foreground == Win32Interop.GetShellWindow())
        {
            return false;
        }

        if (Win32Interop.IsIconic(foreground) || !Win32Interop.IsWindowVisible(foreground))
        {
            return false;
        }

        var sb = new StringBuilder(256);
        if (Win32Interop.GetClassNameW(foreground, sb, sb.Capacity) > 0)
        {
            string className = sb.ToString();
            if (IsExcludedShellClass(className))
            {
                return false;
            }
        }

        // 3. Obtain window bounds via DWMWA_EXTENDED_FRAME_BOUNDS, fallback to GetWindowRect
        Win32Interop.RECT windowRect;
        int dwmResult = Win32Interop.DwmGetWindowAttribute(
            foreground,
            Win32Interop.DWMWA_EXTENDED_FRAME_BOUNDS,
            out windowRect,
            (uint)Marshal.SizeOf<Win32Interop.RECT>());

        if (dwmResult != 0)
        {
            if (!Win32Interop.GetWindowRect(foreground, out windowRect))
            {
                return false;
            }
        }

        // 4. Compare with monitor bounds
        nint hMonitor = Win32Interop.MonitorFromWindow(foreground, Win32Interop.MONITOR_DEFAULTTONEAREST);
        if (hMonitor != nint.Zero)
        {
            var mi = new Win32Interop.MONITORINFO
            {
                cbSize = (uint)Marshal.SizeOf<Win32Interop.MONITORINFO>()
            };

            if (Win32Interop.GetMonitorInfo(hMonitor, ref mi))
            {
                return CheckWindowBoundsMatchMonitor(windowRect, mi.rcMonitor);
            }
        }

        // Fallback to primary screen metrics if monitor info is unavailable
        int screenW = Win32Interop.GetSystemMetrics(0);
        int screenH = Win32Interop.GetSystemMetrics(1);
        var primaryMonitorRect = new Win32Interop.RECT { left = 0, top = 0, right = screenW, bottom = screenH };
        return CheckWindowBoundsMatchMonitor(windowRect, primaryMonitorRect);
    }

    public static bool CheckWindowBoundsMatchMonitor(Win32Interop.RECT windowRect, Win32Interop.RECT monitorRect)
    {
        return windowRect.left <= monitorRect.left &&
               windowRect.top <= monitorRect.top &&
               windowRect.right >= monitorRect.right &&
               windowRect.bottom >= monitorRect.bottom;
    }

    public static bool IsExcludedShellClass(string className)
    {
        return className.Equals("Progman", StringComparison.OrdinalIgnoreCase) ||
               className.Equals("WorkerW", StringComparison.OrdinalIgnoreCase) ||
               className.Equals("Shell_TrayWnd", StringComparison.OrdinalIgnoreCase) ||
               className.Equals("Shell_SecondaryTrayWnd", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
