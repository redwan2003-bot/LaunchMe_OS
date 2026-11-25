namespace LaunchMe.Contracts.Tiling;

/// <summary>
/// Service providing a visual focus border overlay around the active window.
/// </summary>
public interface IFocusBorderService
{
    void AttachToWindow(nint hwnd, WindowRect bounds);
    void HideBorder();
    bool IsVisible { get; }
}
