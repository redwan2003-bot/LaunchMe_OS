using System.Diagnostics;
using System.Text;
using LaunchMe.Contracts.Tiling;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Tiling;

/// <summary>
/// Listens to Win32 window lifecycle events and updates the tiling engine.
/// </summary>
public class Win32WindowTracker : IDisposable
{
    private readonly ITilingEngine _engine;
    private readonly IWindowRuleService _rules;
    private readonly IWindowTilingDispatcher _dispatcher;
    private readonly IFocusBorderService? _focusBorder;
    private readonly Win32Interop.WinEventDelegate _winEventDelegate;

    private nint _hookCreateDestroy;
    private nint _hookForeground;
    private bool _disposed;

    public Win32WindowTracker(
        ITilingEngine engine,
        IWindowRuleService rules,
        IWindowTilingDispatcher dispatcher,
        IFocusBorderService? focusBorder = null)
    {
        _engine = engine;
        _rules = rules;
        _dispatcher = dispatcher;
        _focusBorder = focusBorder;

        _winEventDelegate = OnWinEvent;
    }

    public void Start()
    {
        if (_hookCreateDestroy != nint.Zero)
        {
            return;
        }

        uint flags = Win32Interop.WINEVENT_OUTOFCONTEXT | Win32Interop.WINEVENT_SKIPOWNPROCESS;

        // Hook window creation, destruction, show, hide
        _hookCreateDestroy = Win32Interop.SetWinEventHook(
            Win32Interop.EVENT_OBJECT_CREATE,
            Win32Interop.EVENT_OBJECT_HIDE,
            nint.Zero,
            _winEventDelegate,
            0,
            0,
            flags);

        // Hook foreground focus change
        _hookForeground = Win32Interop.SetWinEventHook(
            Win32Interop.EVENT_SYSTEM_FOREGROUND,
            Win32Interop.EVENT_SYSTEM_FOREGROUND,
            nint.Zero,
            _winEventDelegate,
            0,
            0,
            flags);
    }

    public void Stop()
    {
        if (_hookCreateDestroy != nint.Zero)
        {
            Win32Interop.UnhookWinEvent(_hookCreateDestroy);
            _hookCreateDestroy = nint.Zero;
        }

        if (_hookForeground != nint.Zero)
        {
            Win32Interop.UnhookWinEvent(_hookForeground);
            _hookForeground = nint.Zero;
        }
    }

    public void HandleWindowCreated(nint hwnd, WindowRect availableArea, int workspaceId)
    {
        if (!IsTileableWindow(hwnd, out var processName, out var title, out var bounds))
        {
            return;
        }

        if (_rules.ShouldFloat(processName, title))
        {
            return;
        }

        int targetWs = _rules.GetTargetWorkspace(processName, title) ?? workspaceId;

        _engine.RegisterWindow(hwnd, processName, title, bounds, targetWs);
        var layout = _engine.ComputeLayout("primary", availableArea, targetWs);
        _dispatcher.ApplyLayout(layout);

        _focusBorder?.AttachToWindow(hwnd, bounds);
    }

    public void HandleWindowDestroyed(nint hwnd, WindowRect availableArea, int workspaceId)
    {
        _engine.UnregisterWindow(hwnd);
        var layout = _engine.ComputeLayout("primary", availableArea, workspaceId);
        _dispatcher.ApplyLayout(layout);

        var focused = _engine.GetFocusedWindow();
        if (focused.HasValue && Win32Interop.IsWindow(focused.Value))
        {
            if (TryGetWindowBounds(focused.Value, out var bounds))
            {
                _focusBorder?.AttachToWindow(focused.Value, bounds);
            }
            Win32TilingDispatcher.ApplyWindows11SquishChrome(focused.Value, isFocused: true);
        }
        else
        {
            _focusBorder?.HideBorder();
        }
    }

    public void HandleForegroundChanged(nint hwnd)
    {
        if (Win32Interop.IsWindow(hwnd) && TryGetWindowBounds(hwnd, out var bounds))
        {
            var previousFocused = _engine.GetFocusedWindow();
            if (previousFocused.HasValue && previousFocused.Value != nint.Zero && previousFocused.Value != hwnd)
            {
                Win32TilingDispatcher.ApplyWindows11SquishChrome(previousFocused.Value, isFocused: false);
            }

            _engine.FocusWindow(hwnd);
            _focusBorder?.AttachToWindow(hwnd, bounds);
            Win32TilingDispatcher.ApplyWindows11SquishChrome(hwnd, isFocused: true);
        }
    }

    private void OnWinEvent(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == nint.Zero || idObject != 0 || idChild != 0)
        {
            return;
        }

        // Available area default to 1920x1080 if not passed
        var defaultArea = new WindowRect(0, 0, 1920, 1080);

        switch (eventType)
        {
            case Win32Interop.EVENT_OBJECT_CREATE:
            case Win32Interop.EVENT_OBJECT_SHOW:
                HandleWindowCreated(hwnd, defaultArea, 1);
                break;

            case Win32Interop.EVENT_OBJECT_DESTROY:
            case Win32Interop.EVENT_OBJECT_HIDE:
                HandleWindowDestroyed(hwnd, defaultArea, 1);
                break;

            case Win32Interop.EVENT_SYSTEM_FOREGROUND:
                HandleForegroundChanged(hwnd);
                break;
        }
    }

    public static bool IsTileableWindow(nint hwnd, out string processName, out string title, out WindowRect bounds)
    {
        processName = string.Empty;
        title = string.Empty;
        bounds = WindowRect.Empty;

        if (!Win32Interop.IsWindow(hwnd) || !Win32Interop.IsWindowVisible(hwnd) || Win32Interop.IsIconic(hwnd))
        {
            return false;
        }

        // Check if extended tool window (tooltips, tray popups)
        long exStyle = Win32Interop.GetWindowLongPtr(hwnd, Win32Interop.GWL_EXSTYLE);
        if ((exStyle & Win32Interop.WS_EX_TOOLWINDOW) != 0)
        {
            return false;
        }

        // Check cloaked state (Windows 10/11 invisible background UWP apps)
        int cloaked = 0;
        uint intSize = (uint)sizeof(int);
        int hrCloaked = Win32Interop.DwmGetWindowAttribute(hwnd, Win32Interop.DWMWA_CLOAKED, out cloaked, intSize);
        if (hrCloaked == 0 && cloaked != 0)
        {
            return false;
        }

        // Extract title
        int length = Win32Interop.GetWindowTextLengthW(hwnd);
        if (length > 0)
        {
            var sb = new StringBuilder(length + 1);
            Win32Interop.GetWindowTextW(hwnd, sb, sb.Capacity);
            title = sb.ToString();
        }

        // Extract process name
        Win32Interop.GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid > 0)
        {
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName;
            }
            catch
            {
                processName = "unknown";
            }
        }

        // Extract bounds
        return TryGetWindowBounds(hwnd, out bounds);
    }

    public static bool TryGetWindowBounds(nint hwnd, out WindowRect bounds)
    {
        bounds = WindowRect.Empty;
        if (!Win32Interop.IsWindow(hwnd))
        {
            return false;
        }

        var rect = new Win32Interop.RECT();
        uint rectSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Win32Interop.RECT>();
        int hr = Win32Interop.DwmGetWindowAttribute(hwnd, Win32Interop.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, rectSize);
        if (hr == 0 && rect.right > rect.left && rect.bottom > rect.top)
        {
            bounds = new WindowRect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
            return true;
        }

        if (Win32Interop.GetWindowRect(hwnd, out rect))
        {
            bounds = new WindowRect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
