using LaunchMe.Contracts.Tiling;

namespace LaunchMe.UI.Tiling;

/// <summary>
/// Service managing the Squish neon acrylic ghost drop preview indicator during interactive tiled dragging.
/// Enforces Pause-on-Hide GPU Law: halts all animations and reports false/zero glow when hidden.
/// </summary>
public class TiledDropPreviewService : ITiledDropPreviewService
{
    private bool _isVisible;
    private WindowRect _targetBounds = WindowRect.Empty;

    public bool IsVisible => _isVisible;
    public WindowRect TargetBounds => _targetBounds;

    /// <summary>
    /// Configured Squish neon halo color (default Electric Cyan #00F0FF).
    /// </summary>
    public string NeonColorHex { get; set; } = "#00F0FF";

    /// <summary>
    /// Configured Gaussian halo blur radius in pixels (default 22.0).
    /// </summary>
    public double BlurRadiusPx { get; set; } = 22.0;

    /// <summary>
    /// Configured drop preview corner radius in pixels (default 12.0).
    /// </summary>
    public double CornerRadiusPx { get; set; } = 12.0;

    /// <summary>
    /// Pause-on-Hide GPU Law: true only while visible; false when hidden.
    /// </summary>
    public bool IsAnimationActive => _isVisible;

    /// <summary>
    /// Pause-on-Hide GPU Law: active blur radius is 0.0 when hidden.
    /// </summary>
    public double EffectiveBlurRadiusPx => _isVisible ? BlurRadiusPx : 0.0;

    public void ShowPreview(WindowRect targetBounds)
    {
        if (targetBounds.Width <= 0 || targetBounds.Height <= 0)
        {
            HidePreview();
            return;
        }

        _targetBounds = targetBounds;
        _isVisible = true;
    }

    public void HidePreview()
    {
        _isVisible = false;
        _targetBounds = WindowRect.Empty;
    }
}
