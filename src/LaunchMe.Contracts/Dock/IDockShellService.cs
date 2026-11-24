namespace LaunchMe.Contracts.Dock;

/// <summary>
/// Controls Win32 AppBar registration (SHAppBarMessage) across multi-display setups.
/// </summary>
public interface IDockShellService : IDisposable
{
    void RegisterAppBar(nint hwnd, nint hMonitor, int heightPx, bool reserveWorkArea);
    void UnregisterAppBar(nint hwnd);
    void UpdateAppBarPos(nint hwnd, nint hMonitor, int heightPx, bool reserveWorkArea);
    void SetNativeTaskbarAutoHide(bool autoHide);
}
