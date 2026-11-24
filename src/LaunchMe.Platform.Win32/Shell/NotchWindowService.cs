using LaunchMe.Contracts.Notch;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Shell;

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
        int hr = Win32Interop.SHQueryUserNotificationState(out var state);
        if (hr == 0)
        {
            if (state == Win32Interop.QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D_3D_FULL_SCREEN ||
                state == Win32Interop.QUERY_USER_NOTIFICATION_STATE.QUNS_BUSY)
            {
                return true;
            }
        }

        nint foreground = Win32Interop.GetForegroundWindow();
        if (foreground != nint.Zero)
        {
            if (Win32Interop.GetWindowRect(foreground, out var rect))
            {
                int screenW = Win32Interop.GetSystemMetrics(0);
                int screenH = Win32Interop.GetSystemMetrics(1);

                if (rect.left <= 0 && rect.top <= 0 &&
                    rect.right >= screenW && rect.bottom >= screenH)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
