using LaunchMe.Contracts.Tiling;

namespace LaunchMe.Platform.Win32.Tiling;

/// <summary>
/// Dispatches physical window geometry repositioning calls to Win32.
/// </summary>
public interface IWindowTilingDispatcher
{
    void ApplyLayout(IReadOnlyList<(nint Handle, WindowRect TargetRect)> layout);
    void HideWindow(nint hwnd);
    void RestoreWindow(nint hwnd);
}
