using System.Runtime.InteropServices;
using LaunchMe.Contracts.Triggers;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Gestures;

public sealed class Win32HotCornerService : IDisposable
{
    private readonly object _syncLock = new();
    private readonly HotCornerDwellTracker _dwellTracker;
    private HotCornerConfig _config = new();
    private nint _hookHandle = nint.Zero;
    private Win32Interop.HookProc? _hookProc;
    private bool _disposed;

    public event EventHandler<TriggerEventArgs>? CornerTriggered;

    public Win32HotCornerService()
    {
        _dwellTracker = new HotCornerDwellTracker(150);
    }

    public void SetConfig(HotCornerConfig config)
    {
        lock (_syncLock)
        {
            _config = config;
        }
    }

    public void Start()
    {
        lock (_syncLock)
        {
            if (_hookHandle != nint.Zero || _disposed)
            {
                return;
            }

            _hookProc = HookCallback;
            nint hModule = Win32Interop.GetModuleHandleW(null);
            _hookHandle = Win32Interop.SetWindowsHookExW(Win32Interop.WH_MOUSE_LL, _hookProc, hModule, 0);
        }
    }

    public void Stop()
    {
        lock (_syncLock)
        {
            if (_hookHandle != nint.Zero)
            {
                Win32Interop.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = nint.Zero;
                _hookProc = null;
            }

            _dwellTracker.Reset();
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && wParam == Win32Interop.WM_MOUSEMOVE)
        {
            var hookStruct = Marshal.PtrToStructure<Win32Interop.MSLLHOOKSTRUCT>(lParam);
            ProcessMousePosition(hookStruct.pt.x, hookStruct.pt.y, DateTime.UtcNow);
        }

        return Win32Interop.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void ProcessMousePosition(int x, int y, DateTime timestamp)
    {
        HotCornerAction actionToTrigger = HotCornerAction.None;
        HotCornerLocation triggeredCorner = default;

        lock (_syncLock)
        {
            var monitors = GetMonitors();
            var detected = HotCornerHitTester.FindCornerAcrossMonitors(x, y, monitors, 5);

            if (_dwellTracker.Update(detected, timestamp, out triggeredCorner))
            {
                actionToTrigger = _config.GetActionForCorner(triggeredCorner);
            }
        }

        if (actionToTrigger != HotCornerAction.None)
        {
            CornerTriggered?.Invoke(this, new TriggerEventArgs(TriggerType.HotCorner, triggeredCorner));
        }
    }

    private static List<Win32Interop.RECT> GetMonitors()
    {
        var monitors = new List<Win32Interop.RECT>();
        Win32Interop.EnumDisplayMonitors(nint.Zero, nint.Zero, (nint hMon, nint _, ref Win32Interop.RECT rc, nint _) =>
        {
            var mi = new Win32Interop.MONITORINFO { cbSize = (uint)Marshal.SizeOf<Win32Interop.MONITORINFO>() };
            if (Win32Interop.GetMonitorInfo(hMon, ref mi))
            {
                monitors.Add(mi.rcMonitor);
            }
            return true;
        }, nint.Zero);

        if (monitors.Count == 0)
        {
            monitors.Add(new Win32Interop.RECT
            {
                left = Win32Interop.GetSystemMetrics(Win32Interop.SM_XVIRTUALSCREEN),
                top = Win32Interop.GetSystemMetrics(Win32Interop.SM_YVIRTUALSCREEN),
                right = Win32Interop.GetSystemMetrics(Win32Interop.SM_XVIRTUALSCREEN) + Win32Interop.GetSystemMetrics(Win32Interop.SM_CXVIRTUALSCREEN),
                bottom = Win32Interop.GetSystemMetrics(Win32Interop.SM_YVIRTUALSCREEN) + Win32Interop.GetSystemMetrics(Win32Interop.SM_CYVIRTUALSCREEN)
            });
        }

        return monitors;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }
}
