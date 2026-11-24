using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using LaunchMe.Contracts.Triggers;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Gestures;

public sealed class Win32HotkeyService : IDisposable
{
    private readonly ConcurrentDictionary<int, (uint Modifiers, uint VirtualKey)> _registrations = new();
    private readonly Thread _pumpThread;
    private readonly ManualResetEventSlim _windowReadyEvent = new(false);
    private nint _hwnd = nint.Zero;
    private Win32Interop.WndProc? _wndProc;
    private int _nextId = 1;
    private bool _disposed;

    public event EventHandler<TriggerEventArgs>? HotkeyTriggered;

    public Win32HotkeyService()
    {
        _pumpThread = new Thread(RunMessagePump)
        {
            IsBackground = true,
            Name = "LaunchMe_HotkeyPump"
        };
        _pumpThread.Start();
        _windowReadyEvent.Wait();
    }

    private void RunMessagePump()
    {
        string className = "LaunchMe_HotkeyListener_" + Guid.NewGuid().ToString("N");
        _wndProc = WndProcHandler;

        var wc = new Win32Interop.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<Win32Interop.WNDCLASSEX>(),
            lpfnWndProc = _wndProc,
            hInstance = Win32Interop.GetModuleHandleW(null),
            lpszClassName = className
        };

        Win32Interop.RegisterClassExW(ref wc);

        _hwnd = Win32Interop.CreateWindowExW(
            0,
            className,
            "LaunchMeHotkeyWindow",
            0,
            0, 0, 0, 0,
            Win32Interop.HWND_MESSAGE,
            nint.Zero,
            wc.hInstance,
            nint.Zero);

        _windowReadyEvent.Set();

        while (Win32Interop.GetMessageW(out var msg, nint.Zero, 0, 0) > 0)
        {
            Win32Interop.TranslateMessage(ref msg);
            Win32Interop.DispatchMessageW(ref msg);
        }

        if (_hwnd != nint.Zero)
        {
            Win32Interop.DestroyWindow(_hwnd);
            _hwnd = nint.Zero;
        }

        Win32Interop.UnregisterClassW(className, wc.hInstance);
    }

    private nint WndProcHandler(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == Win32Interop.WM_HOTKEY)
        {
            int id = (int)wParam;
            if (_registrations.TryGetValue(id, out var _))
            {
                HotkeyTriggered?.Invoke(this, new TriggerEventArgs(TriggerType.Hotkey));
            }
            return nint.Zero;
        }

        return Win32Interop.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    public bool Register(uint modifiers, uint virtualKey)
    {
        if (_disposed || _hwnd == nint.Zero)
        {
            return false;
        }

        foreach (var entry in _registrations)
        {
            if (entry.Value.Modifiers == modifiers && entry.Value.VirtualKey == virtualKey)
            {
                return true;
            }
        }

        int id = Interlocked.Increment(ref _nextId);
        bool success = Win32Interop.RegisterHotKey(_hwnd, id, modifiers | Win32Interop.MOD_NOREPEAT, virtualKey);
        if (success)
        {
            _registrations[id] = (modifiers, virtualKey);
        }

        return success;
    }

    public bool Unregister(uint modifiers, uint virtualKey)
    {
        if (_disposed || _hwnd == nint.Zero)
        {
            return false;
        }

        foreach (var entry in _registrations)
        {
            if (entry.Value.Modifiers == modifiers && entry.Value.VirtualKey == virtualKey)
            {
                if (_registrations.TryRemove(entry.Key, out _))
                {
                    return Win32Interop.UnregisterHotKey(_hwnd, entry.Key);
                }
            }
        }

        return false;
    }

    public void UnregisterAll()
    {
        if (_hwnd != nint.Zero)
        {
            foreach (var id in _registrations.Keys)
            {
                Win32Interop.UnregisterHotKey(_hwnd, id);
            }
            _registrations.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_hwnd != nint.Zero)
        {
            UnregisterAll();
            Win32Interop.PostMessageW(_hwnd, Win32Interop.WM_CLOSE, nint.Zero, nint.Zero);
        }

        _windowReadyEvent.Dispose();
    }
}
