namespace LaunchMe.Contracts.Tiling;

/// <summary>
/// Core mathematical and state coordinator for dynamic BSP window tiling.
/// </summary>
public interface ITilingEngine
{
    TilingConfig Config { get; set; }

    void RegisterWindow(nint hwnd, string processName, string title, WindowRect initialBounds, int workspaceId);
    void UnregisterWindow(nint hwnd);
    void FocusWindow(nint hwnd);
    nint? GetFocusedWindow();
    nint? FindDirectionalTarget(nint currentHwnd, SplitDirection direction);
    void SwapWindow(nint hwnd, SplitDirection direction);
    void ToggleSplitAxis(nint hwnd);
    void ToggleFloating(nint hwnd);
    void ToggleScratchpad(nint hwnd);
    bool IsFloating(nint hwnd);
    bool IsScratchpad(nint hwnd);

    IReadOnlyList<(nint Handle, WindowRect TargetRect)> ComputeLayout(string displayId, WindowRect availableArea, int workspaceId);
    IReadOnlyList<TiledWindow> GetWindows(int workspaceId);
}
