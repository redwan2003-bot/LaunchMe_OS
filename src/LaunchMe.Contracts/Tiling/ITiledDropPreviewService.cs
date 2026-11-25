namespace LaunchMe.Contracts.Tiling;

/// <summary>
/// Service providing visual semi-transparent acrylic ghost preview during drag-to-tile operations.
/// </summary>
public interface ITiledDropPreviewService
{
    void ShowPreview(WindowRect targetBounds);
    void HidePreview();
    bool IsVisible { get; }
}
