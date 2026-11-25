namespace LaunchMe.Contracts.Indexing;

/// <summary>
/// Extracts ultra-high-resolution (256px Jumbo) Windows Shell icons and Explorer file thumbnails via IShellItemImageFactory.
/// </summary>
public interface IShellIconExtractor
{
    /// <summary>
    /// Attempts to extract a PNG-encoded BGRA bitmap up to <paramref name="targetSize"/> pixels from Windows Shell/Explorer.
    /// Returns null safely if unavailable or running headless.
    /// </summary>
    byte[]? ExtractHighResPng(string filePath, int targetSize = 256, bool thumbnailIfAvailable = true);
}
