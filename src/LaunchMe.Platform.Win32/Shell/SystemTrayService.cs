using System.Runtime.InteropServices;
using LaunchMe.Contracts.Triggers;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Shell;

/// <summary>
/// Manages the LaunchMe-Win notification area (system tray) icon and context menu on a dedicated Win32 message pump.
/// </summary>
public sealed class SystemTrayService : ISystemTrayService
{
    public const uint TrayCallbackMessage = Win32Interop.WM_APP + 1;
    public const uint CommandShowLauncher = 1001;
    public const uint CommandOpenSettings = 1002;
    public const uint CommandExit = 1003;

    private const uint TrayIconId = 1;
    private const int MaxTooltipLength = 127;

    private readonly object _lifecycleLock = new();
    private Thread? _pumpThread;
    private ManualResetEventSlim? _readyEvent;
    private Win32Interop.WndProc? _wndProc;
    private nint _hwnd;
    private int _iconAdded;
    private bool _disposed;

    public bool IsInitialized { get; private set; }

    public event EventHandler? ShowLauncherRequested;
    public event EventHandler? OpenSettingsRequested;
    public event EventHandler? ExitRequested;

    public void Initialize(string tooltip = "LaunchMe-Win")
    {
        string normalizedTooltip = NormalizeTooltip(tooltip);

        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (IsInitialized)
            {
                if (_hwnd != nint.Zero && Volatile.Read(ref _iconAdded) == 1)
                {
                    var updateData = CreateNotifyIconData(_hwnd, nint.Zero, normalizedTooltip, includeMessageAndIcon: false);
                    Win32Interop.Shell_NotifyIconW(Win32Interop.NIM_MODIFY, ref updateData);
                }
                return;
            }

            _readyEvent = new ManualResetEventSlim(false);
            _pumpThread = new Thread(() => RunMessagePump(normalizedTooltip))
            {
                IsBackground = true,
                Name = "LaunchMe_SystemTrayPump"
            };
            _pumpThread.Start();
            _readyEvent.Wait();
        }
    }

    /// <summary>
    /// Routes a popup menu command identifier to the corresponding tray service event.
    /// </summary>
    public bool DispatchCommand(uint commandId)
    {
        switch (commandId)
        {
            case CommandShowLauncher:
                ShowLauncherRequested?.Invoke(this, EventArgs.Empty);
                return true;

            case CommandOpenSettings:
                OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
                return true;

            case CommandExit:
                ExitRequested?.Invoke(this, EventArgs.Empty);
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Handles a mouse message delivered to the tray callback message without requiring a modal OS menu.
    /// </summary>
    public bool HandleTrayMouseMessage(uint mouseMessage)
    {
        if (mouseMessage is Win32Interop.WM_LBUTTONUP or Win32Interop.WM_LBUTTONDBLCLK)
        {
            ShowLauncherRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if (mouseMessage == Win32Interop.WM_RBUTTONUP)
        {
            if (_hwnd != nint.Zero)
            {
                ShowContextMenu(_hwnd);
            }
            return true;
        }

        return false;
    }

    private void RunMessagePump(string tooltip)
    {
        string className = "LaunchMe_TrayWindow_" + Guid.NewGuid().ToString("N");
        nint hInstance = Win32Interop.GetModuleHandleW(null);
        _wndProc = WndProcHandler;

        bool classRegistered = false;
        try
        {
            var wc = new Win32Interop.WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<Win32Interop.WNDCLASSEX>(),
                lpfnWndProc = _wndProc,
                hInstance = hInstance,
                lpszClassName = className
            };

            classRegistered = Win32Interop.RegisterClassExW(ref wc) != 0;
            if (classRegistered)
            {
                _hwnd = Win32Interop.CreateWindowExW(
                    0,
                    className,
                    "LaunchMeTrayWindow",
                    Win32Interop.WS_POPUP,
                    0, 0, 0, 0,
                    nint.Zero,
                    nint.Zero,
                    hInstance,
                    nint.Zero);
            }

            if (_hwnd != nint.Zero)
            {
                nint hIcon = Win32Interop.LoadIconW(nint.Zero, (nint)Win32Interop.IDI_APPLICATION);
                var nid = CreateNotifyIconData(_hwnd, hIcon, tooltip, includeMessageAndIcon: true);
                bool added = Win32Interop.Shell_NotifyIconW(Win32Interop.NIM_ADD, ref nid);
                Volatile.Write(ref _iconAdded, added ? 1 : 0);
                IsInitialized = true;
            }
        }
        finally
        {
            _readyEvent?.Set();
        }

        if (_hwnd != nint.Zero)
        {
            while (Win32Interop.GetMessageW(out var msg, nint.Zero, 0, 0) > 0)
            {
                Win32Interop.TranslateMessage(ref msg);
                Win32Interop.DispatchMessageW(ref msg);
            }
        }

        RemoveTrayIcon();

        if (_hwnd != nint.Zero)
        {
            Win32Interop.DestroyWindow(_hwnd);
            _hwnd = nint.Zero;
        }

        if (classRegistered)
        {
            Win32Interop.UnregisterClassW(className, hInstance);
        }
    }

    private nint WndProcHandler(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == TrayCallbackMessage)
        {
            uint mouseMsg = unchecked((uint)(lParam.ToInt64() & 0xFFFF));
            HandleTrayMouseMessage(mouseMsg);
            return nint.Zero;
        }

        if (msg == Win32Interop.WM_CLOSE)
        {
            RemoveTrayIcon();
            Win32Interop.DestroyWindow(hWnd);
            _hwnd = nint.Zero;
            return nint.Zero;
        }

        if (msg == Win32Interop.WM_DESTROY)
        {
            Win32Interop.PostQuitMessage(0);
            return nint.Zero;
        }

        return Win32Interop.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void ShowContextMenu(nint hWnd)
    {
        nint hMenu = Win32Interop.CreatePopupMenu();
        if (hMenu == nint.Zero)
        {
            return;
        }

        try
        {
            Win32Interop.AppendMenuW(hMenu, Win32Interop.MF_STRING, CommandShowLauncher, "Show Launcher");
            Win32Interop.AppendMenuW(hMenu, Win32Interop.MF_STRING, CommandOpenSettings, "Settings");
            Win32Interop.AppendMenuW(hMenu, Win32Interop.MF_SEPARATOR, 0, null);
            Win32Interop.AppendMenuW(hMenu, Win32Interop.MF_STRING, CommandExit, "Exit LaunchMe");

            Win32Interop.GetCursorPos(out var pt);
            Win32Interop.SetForegroundWindow(hWnd);

            int selected = Win32Interop.TrackPopupMenu(
                hMenu,
                Win32Interop.TPM_RETURNCMD | Win32Interop.TPM_RIGHTBUTTON,
                pt.x,
                pt.y,
                0,
                hWnd,
                nint.Zero);

            if (selected > 0)
            {
                DispatchCommand((uint)selected);
            }
        }
        finally
        {
            Win32Interop.DestroyMenu(hMenu);
        }
    }

    private void RemoveTrayIcon()
    {
        nint hwnd = _hwnd;
        if (hwnd == nint.Zero || Interlocked.Exchange(ref _iconAdded, 0) == 0)
        {
            return;
        }

        var nid = new Win32Interop.NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<Win32Interop.NOTIFYICONDATAW>(),
            hWnd = hwnd,
            uID = TrayIconId
        };
        Win32Interop.Shell_NotifyIconW(Win32Interop.NIM_DELETE, ref nid);
    }

    private static Win32Interop.NOTIFYICONDATAW CreateNotifyIconData(
        nint hWnd,
        nint hIcon,
        string tooltip,
        bool includeMessageAndIcon)
    {
        uint flags = Win32Interop.NIF_TIP;
        if (includeMessageAndIcon)
        {
            flags |= Win32Interop.NIF_MESSAGE | Win32Interop.NIF_ICON;
        }

        return new Win32Interop.NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<Win32Interop.NOTIFYICONDATAW>(),
            hWnd = hWnd,
            uID = TrayIconId,
            uFlags = flags,
            uCallbackMessage = TrayCallbackMessage,
            hIcon = hIcon,
            szTip = tooltip
        };
    }

    private static string NormalizeTooltip(string? tooltip)
    {
        if (string.IsNullOrWhiteSpace(tooltip))
        {
            return "LaunchMe-Win";
        }

        return tooltip.Length <= MaxTooltipLength
            ? tooltip
            : tooltip[..MaxTooltipLength];
    }

    public void Dispose()
    {
        Thread? pumpThreadToJoin;
        ManualResetEventSlim? readyEventToDispose;

        lock (_lifecycleLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            IsInitialized = false;

            RemoveTrayIcon();

            if (_hwnd != nint.Zero)
            {
                Win32Interop.PostMessageW(_hwnd, Win32Interop.WM_CLOSE, nint.Zero, nint.Zero);
            }

            pumpThreadToJoin = _pumpThread;
            _pumpThread = null;
            readyEventToDispose = _readyEvent;
            _readyEvent = null;
        }

        if (pumpThreadToJoin is { IsAlive: true } &&
            Environment.CurrentManagedThreadId != pumpThreadToJoin.ManagedThreadId)
        {
            pumpThreadToJoin.Join(TimeSpan.FromSeconds(5));
        }

        readyEventToDispose?.Dispose();
    }
}
