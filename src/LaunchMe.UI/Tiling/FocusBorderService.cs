using LaunchMe.Contracts.Tiling;

namespace LaunchMe.UI.Tiling;

/// <summary>
/// Service managing the Squish neon visual focus glow border overlay around the active window.
/// Enforces Pause-on-Hide GPU Law: renders zero frames and reports false/zero active glow when hidden.
/// </summary>
public class FocusBorderService : IFocusBorderService
{
    private bool _isVisible;
    private nint _attachedHwnd;
    private WindowRect _currentBounds = WindowRect.Empty;

    public bool IsVisible => _isVisible;
    public nint AttachedHwnd => _attachedHwnd;
    public WindowRect CurrentBounds => _currentBounds;

    /// <summary>
    /// Configured Squish neon halo color (default Electric Cyan #00F0FF).
    /// </summary>
    public string NeonColorHex { get; set; } = "#00F0FF";

    /// <summary>
    /// Configured Gaussian halo blur radius in pixels (default 22.0).
    /// </summary>
    public double BlurRadiusPx { get; set; } = 22.0;

    /// <summary>
    /// Configured focus halo corner radius in pixels (default 12.0).
    /// </summary>
    public double CornerRadiusPx { get; set; } = 12.0;

    /// <summary>
    /// Pause-on-Hide GPU Law: true only while visible; false when hidden.
    /// </summary>
    public bool IsGlowActive => _isVisible;

    /// <summary>
    /// Pause-on-Hide GPU Law: active blur radius is 0.0 when hidden.
    /// </summary>
    public double EffectiveBlurRadiusPx => _isVisible ? BlurRadiusPx : 0.0;

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
