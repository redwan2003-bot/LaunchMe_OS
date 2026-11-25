using LaunchMe.Contracts.Tiling;

namespace LaunchMe.UI.Tiling;

/// <summary>
/// Service managing the acrylic ghost drop preview indicator during interactive tiled dragging.
/// Enforces Pause-on-Hide GPU Law: halts all animations and hides when drag ends.
/// </summary>
public class TiledDropPreviewService : ITiledDropPreviewService
{
    private bool _isVisible;
    private WindowRect _targetBounds = WindowRect.Empty;

    public bool IsVisible => _isVisible;
    public WindowRect TargetBounds => _targetBounds;

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
