using LaunchMe.Contracts.Tiling;

namespace LaunchMe.UI.Tiling;

/// <summary>
/// Service managing the visual focus glow border overlay around the active window.
/// Enforces Pause-on-Hide GPU Law: renders zero frames and halts when hidden.
/// </summary>
public class FocusBorderService : IFocusBorderService
{
    private bool _isVisible;
    private nint _attachedHwnd;
    private WindowRect _currentBounds = WindowRect.Empty;

    public bool IsVisible => _isVisible;
    public nint AttachedHwnd => _attachedHwnd;
    public WindowRect CurrentBounds => _currentBounds;

    public void AttachToWindow(nint hwnd, WindowRect bounds)
    {
        if (hwnd == nint.Zero || bounds.Width <= 0 || bounds.Height <= 0)
        {
            HideBorder();
            return;
        }

        _attachedHwnd = hwnd;
        _currentBounds = bounds;
        _isVisible = true;
    }

    public void HideBorder()
    {
        _isVisible = false;
        _attachedHwnd = nint.Zero;
        _currentBounds = WindowRect.Empty;
    }
}
