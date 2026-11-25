using System.Runtime.InteropServices;
using LaunchMe.Contracts.Triggers;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Gestures;

public sealed class Win32TouchpadService : IDisposable
{
    private readonly TouchpadGestureParser _parser;
    private readonly Win32Interop.WndProc _wndProc;
    private readonly Thread? _windowThread;
    private nint _hwnd;
    private bool _disposed;
    private bool _isEnabled = true;
    private readonly ManualResetEventSlim _initEvent = new(false);

    public event EventHandler<TriggerEventArgs>? GestureDetected;

    public int TargetFingerCount
    {
        get => _parser.TargetFingerCount;
        set => _parser.TargetFingerCount = value;
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            if (!value) _parser.ResetTracking();
        }
    }

    public Win32TouchpadService(TouchpadGestureParser? parser = null)
    {
        _parser = parser ?? new TouchpadGestureParser();
        _wndProc = WndProc;
        _parser.GestureTriggered += OnParserGestureTriggered;

        _windowThread = new Thread(WindowThreadProc)
        {
            IsBackground = true,
            Name = "LaunchMe.TouchpadRawInput"
        };
        _windowThread.Start();
        _initEvent.Wait(2000);
    }

    private void OnParserGestureTriggered(object? sender, TriggerEventArgs e)
    {
        if (_isEnabled && !_disposed)
        {
            GestureDetected?.Invoke(this, e);
        }
    }

    public void FeedContacts(IReadOnlyList<TouchContact> contacts)
    {
        if (!_isEnabled || _disposed) return;
        _parser.FeedFrame(contacts, DateTime.UtcNow);
    }

    public void SimulateGesture(TriggerType type)
    {
        if (!_isEnabled || _disposed) return;
        GestureDetected?.Invoke(this, new TriggerEventArgs(type));
    }

    private void WindowThreadProc()
    {
        try
        {
            string className = $"LaunchMeTouchpadSink_{Guid.NewGuid():N}";
            var wndClass = new Win32Interop.WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<Win32Interop.WNDCLASSEX>(),
                lpfnWndProc = _wndProc,
                hInstance = Win32Interop.GetModuleHandleW(null),
                lpszClassName = className
            };

            Win32Interop.RegisterClassExW(ref wndClass);

            _hwnd = Win32Interop.CreateWindowExW(
                0, className, "TouchpadSink", 0,
                0, 0, 0, 0,
                Win32Interop.HWND_MESSAGE, nint.Zero, Win32Interop.GetModuleHandleW(null), nint.Zero);

            if (_hwnd != nint.Zero)
            {
                var rid = new Win32Interop.RAWINPUTDEVICE
                {
                    usUsagePage = Win32Interop.HID_USAGE_PAGE_DIGITIZER,
                    usUsage = Win32Interop.HID_USAGE_DIGITIZER_TOUCH_PAD,
                    dwFlags = Win32Interop.RIDEV_INPUTSINK,
                    hwndTarget = _hwnd
                };

                Win32Interop.RegisterRawInputDevices([rid], 1, (uint)Marshal.SizeOf<Win32Interop.RAWINPUTDEVICE>());
            }
        }
        catch
        {
            // Gracefully tolerate lack of raw input / non-interactive desktop
        }
        finally
        {
            _initEvent.Set();
        }

        while (!_disposed)
        {
            int res = Win32Interop.GetMessageW(out var msg, nint.Zero, 0, 0);
            if (res <= 0) break;
            Win32Interop.TranslateMessage(ref msg);
            Win32Interop.DispatchMessageW(ref msg);
        }
    }

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == Win32Interop.WM_INPUT && _isEnabled && !_disposed)
        {
            // Raw input packet arrived
        }
        else if (msg == Win32Interop.WM_CLOSE || msg == Win32Interop.WM_DESTROY)
        {
            Win32Interop.PostQuitMessage(0);
            return nint.Zero;
        }

        return Win32Interop.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _parser.GestureTriggered -= OnParserGestureTriggered;

        if (_hwnd != nint.Zero)
        {
            try
            {
                var rid = new Win32Interop.RAWINPUTDEVICE
                {
                    usUsagePage = Win32Interop.HID_USAGE_PAGE_DIGITIZER,
                    usUsage = Win32Interop.HID_USAGE_DIGITIZER_TOUCH_PAD,
                    dwFlags = Win32Interop.RIDEV_REMOVE,
                    hwndTarget = nint.Zero
                };
                Win32Interop.RegisterRawInputDevices([rid], 1, (uint)Marshal.SizeOf<Win32Interop.RAWINPUTDEVICE>());
                Win32Interop.PostMessageW(_hwnd, Win32Interop.WM_CLOSE, nint.Zero, nint.Zero);
            }
            catch
            {
            }
        }

        _initEvent.Dispose();
        _windowThread?.Join(1000);
    }
}
