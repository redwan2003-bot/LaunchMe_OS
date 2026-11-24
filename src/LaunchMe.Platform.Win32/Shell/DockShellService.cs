using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using LaunchMe.Contracts.Dock;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Shell;

public sealed class DockShellService : IDockShellService
{
    private readonly ConcurrentDictionary<nint, Win32Interop.APPBARDATA> _registeredBars = new();
    private bool _disposed;

    public void RegisterAppBar(nint hwnd, nint hMonitor, int heightPx, bool reserveWorkArea)
    {
        if (hwnd == nint.Zero || _disposed)
        {
            return;
        }

        var mi = new Win32Interop.MONITORINFO { cbSize = (uint)Marshal.SizeOf<Win32Interop.MONITORINFO>() };
        if (!Win32Interop.GetMonitorInfo(hMonitor, ref mi))
        {
            mi.rcMonitor = new Win32Interop.RECT
            {
                left = Win32Interop.GetSystemMetrics(Win32Interop.SM_XVIRTUALSCREEN),
                top = Win32Interop.GetSystemMetrics(Win32Interop.SM_YVIRTUALSCREEN),
                right = Win32Interop.GetSystemMetrics(Win32Interop.SM_XVIRTUALSCREEN) + Win32Interop.GetSystemMetrics(Win32Interop.SM_CXVIRTUALSCREEN),
                bottom = Win32Interop.GetSystemMetrics(Win32Interop.SM_YVIRTUALSCREEN) + Win32Interop.GetSystemMetrics(Win32Interop.SM_CYVIRTUALSCREEN)
            };
        }

        var abd = new Win32Interop.APPBARDATA
        {
            cbSize = (uint)Marshal.SizeOf<Win32Interop.APPBARDATA>(),
            hWnd = hwnd,
            uEdge = Win32Interop.ABE_BOTTOM,
            rc = new Win32Interop.RECT
            {
                left = mi.rcMonitor.left,
                top = mi.rcMonitor.bottom - heightPx,
                right = mi.rcMonitor.right,
                bottom = mi.rcMonitor.bottom
            }
        };

        Win32Interop.SHAppBarMessage(Win32Interop.ABM_NEW, ref abd);

        if (reserveWorkArea)
        {
            Win32Interop.SHAppBarMessage(Win32Interop.ABM_QUERYPOS, ref abd);
            Win32Interop.SHAppBarMessage(Win32Interop.ABM_SETPOS, ref abd);
        }

        _registeredBars[hwnd] = abd;
    }

    public void UnregisterAppBar(nint hwnd)
    {
        if (_registeredBars.TryRemove(hwnd, out var abd))
        {
            Win32Interop.SHAppBarMessage(Win32Interop.ABM_REMOVE, ref abd);
        }
    }

    public void UpdateAppBarPos(nint hwnd, nint hMonitor, int heightPx, bool reserveWorkArea)
    {
        UnregisterAppBar(hwnd);
        RegisterAppBar(hwnd, hMonitor, heightPx, reserveWorkArea);
    }

    public void SetNativeTaskbarAutoHide(bool autoHide)
    {
        nint taskbarHwnd = Win32Interop.FindWindowW("Shell_TrayWnd", null);
        if (taskbarHwnd == nint.Zero)
        {
            return;
        }

        var abd = new Win32Interop.APPBARDATA
        {
            cbSize = (uint)Marshal.SizeOf<Win32Interop.APPBARDATA>(),
            hWnd = taskbarHwnd,
            lParam = autoHide ? (nint)Win32Interop.ABS_AUTOHIDE : (nint)Win32Interop.ABS_ALWAYSONTOP
        };

        Win32Interop.SHAppBarMessage(Win32Interop.ABM_SETSTATE, ref abd);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var hwnd in _registeredBars.Keys.ToList())
        {
            UnregisterAppBar(hwnd);
        }
    }
}
