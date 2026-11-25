using LaunchMe.Contracts.Tiling;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Tiling;

/// <summary>
/// Atomically applies computed tiling rectangles to Win32 HWNDs using DeferWindowPos.
/// </summary>
public class Win32TilingDispatcher : IWindowTilingDispatcher
{
    public void ApplyLayout(IReadOnlyList<(nint Handle, WindowRect TargetRect)> layout)
    {
        if (layout == null || layout.Count == 0)
        {
            return;
        }

        uint flags = Win32Interop.SWP_NOACTIVATE | Win32Interop.SWP_NOZORDER | Win32Interop.SWP_FRAMECHANGED;

        if (layout.Count == 1)
        {
            var (hwnd, rect) = layout[0];
            if (Win32Interop.IsWindow(hwnd))
            {
                Win32Interop.SetWindowPos(hwnd, nint.Zero, rect.X, rect.Y, rect.Width, rect.Height, flags);
                ApplyWindows11SquishChrome(hwnd, isFocused: false);
            }
            return;
        }

        nint hdwp = Win32Interop.BeginDeferWindowPos(layout.Count);
        if (hdwp == nint.Zero)
        {
            // Fallback to individual SetWindowPos if DeferWindowPos allocation fails
            foreach (var (hwnd, rect) in layout)
            {
                if (Win32Interop.IsWindow(hwnd))
                {
                    Win32Interop.SetWindowPos(hwnd, nint.Zero, rect.X, rect.Y, rect.Width, rect.Height, flags);
                    ApplyWindows11SquishChrome(hwnd, isFocused: false);
                }
            }
            return;
        }

        foreach (var (hwnd, rect) in layout)
        {
            if (Win32Interop.IsWindow(hwnd))
            {
                hdwp = Win32Interop.DeferWindowPos(hdwp, hwnd, nint.Zero, rect.X, rect.Y, rect.Width, rect.Height, flags);
                ApplyWindows11SquishChrome(hwnd, isFocused: false);
                if (hdwp == nint.Zero)
                {
                    break;
                }
            }
        }

        if (hdwp != nint.Zero)
        {
            Win32Interop.EndDeferWindowPos(hdwp);
        }
    }

    /// <summary>
    /// Applies Windows 11 rounded corners and active neon cyan DWM border styling to a tiled window.
    /// </summary>
    public static void ApplyWindows11SquishChrome(nint hwnd, bool isFocused, uint activeBgrColor = 0x00FFF000)
    {
        if (hwnd == nint.Zero || !Win32Interop.IsWindow(hwnd))
        {
            return;
        }

        try
        {
            int cornerPreference = Win32Interop.DWMWCP_ROUND;
            Win32Interop.DwmSetWindowAttribute(
                hwnd,
                Win32Interop.DWMWA_WINDOW_CORNER_PREFERENCE,
                ref cornerPreference,
                sizeof(int));

            uint borderColor = isFocused ? activeBgrColor : Win32Interop.DWMWA_COLOR_DEFAULT;
            Win32Interop.DwmSetWindowAttribute(
                hwnd,
                Win32Interop.DWMWA_BORDER_COLOR,
                ref borderColor,
                sizeof(uint));
        }
        catch
        {
            // Guard against unsupported OS builds, virtual desktops, or protected windows.
        }
    }

    public void HideWindow(nint hwnd)
    {
        if (Win32Interop.IsWindow(hwnd))
        {
            Win32Interop.ShowWindow(hwnd, Win32Interop.SW_HIDE);
        }
    }

    public void RestoreWindow(nint hwnd)
    {
        if (Win32Interop.IsWindow(hwnd))
        {
            Win32Interop.ShowWindow(hwnd, Win32Interop.SW_RESTORE);
        }
    }
}
