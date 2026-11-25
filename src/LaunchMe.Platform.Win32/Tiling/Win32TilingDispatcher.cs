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
                }
            }
            return;
        }

        foreach (var (hwnd, rect) in layout)
        {
            if (Win32Interop.IsWindow(hwnd))
            {
                hdwp = Win32Interop.DeferWindowPos(hdwp, hwnd, nint.Zero, rect.X, rect.Y, rect.Width, rect.Height, flags);
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
